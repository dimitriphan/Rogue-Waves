#!/usr/bin/env python3
"""The mechanical half of AGENTS.md "Code Review Rules", run for free before Codex or a Claude reviewer reads a PR.

    python3 TDS_Mod/Tools/cloud-check/check_rules.py [--base origin/claude/phase-1-6lfnar]

Run from the repository root. Whole-tree checks look at the tree as it is; diff checks look only at what the branch
adds since its merge base with --base, so old text that dated reports keep on purpose never trips them. Exit 1 lists
each finding as `P0|P1 path:line: what`, exit 0 prints "rules: clean". GitHub runs it on every PR
(.github/workflows/rules.yml). It never judges design, wording or game numbers: those stay with the reviewer.
"""
import argparse
import colorsys
import glob
import json
import os
import re
import subprocess
import sys

NOW_LIMIT = 4096            # Part C "Now" stays under about 4 KB (Dimitri 2026-11-12)
BINARY = re.compile(r'\.(dll|bundle|unitypackage|zip)$', re.I)
BLOCKED_API = re.compile(r'\bSystem\.(IO|Reflection)\b')
OLD_NAMES = re.compile(r'\b(Vessel|Team) Cardinals?\b')
# Where the old names and pre-scope history may stay (AGENTS.md P1): dated reports, the Log, version histories,
# his scope notes. Code keeps them in save keys, so .cs files are left to the reviewer.
OLD_NAMES_OK = ('docs/reports/', 'docs/log/', 'docs/scope/02_Progress.txt', 'AGENTS.md', 'CLAUDE.md',
                'TDS_Mod/Tools/batch/', 'TDS_Mod/Tools/cloud-check/check_rules.py')
ROOTS = ('docs/', 'TDS_Mod/', 'TDS_Vessel_Creator/', 'concept-tools/', 'StatusWidget/', '.claude/', '.github/')


def git(*args):
    return subprocess.run(('git',) + args, capture_output=True, text=True, check=True).stdout


def added_lines(base):
    """{path: [(line number, text)]} for every line the branch adds."""
    out, path, n = {}, None, 0
    for line in git('diff', '-U0', '--no-color', base + '...HEAD').splitlines():
        if line.startswith('+++ '):
            path = line[6:] if line.startswith('+++ b/') else None
        elif line.startswith('@@'):
            n = int(re.search(r'\+(\d+)', line).group(1))
        elif path and line.startswith('+'):
            out.setdefault(path, []).append((n, line[1:]))
            n += 1
    return out


def changed_files(base):
    return [p for p in git('diff', '--name-only', '--diff-filter=AMR', base + '...HEAD').splitlines() if p]


def cited_path_exists(p):
    p = re.sub(r'[:#§].*$', '', p).rstrip('/.,')
    if not p:
        return True
    for root in ('', 'TDS_Vessel_Creator/', 'TDS_Mod/', 'TDS_Mod/ModSource/'):
        if os.path.exists(root + p) or glob.glob(glob.escape(root + p) + '*'):
            return True
    return False


def art_problems(path, foe):
    from PIL import Image
    im = Image.open(path).convert('RGBA')
    soft = black = magenta = 0
    raw = im.tobytes()
    for r, g, b, a in zip(raw[0::4], raw[1::4], raw[2::4], raw[3::4]):
        if 0 < a < 255:
            soft += 1
        if not a:
            continue
        if (r, g, b) == (0, 0, 0):
            black += 1
        h, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        if not foe and s > 0.2 and v > 0.2 and 270 <= h * 360 <= 320:
            magenta += 1
    found = []
    if soft:
        found.append('%d half-transparent pixels' % soft)
    if black:
        found.append('%d pure black 000000 pixels' % black)
    if magenta:
        found.append("%d pixels in the foes' hue band 270-320 on a non-foe" % magenta)
    return found


def main():
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument('--base', default='origin/claude/phase-1-6lfnar')
    base = ap.parse_args().base
    found = []

    def flag(level, where, what):
        found.append('%s %s: %s' % (level, where, what))

    # Whole tree
    for p in git('ls-files').splitlines():
        if BINARY.search(p):
            flag('P0', p, 'a binary that is never committed (the game\'s binaries, packages, archives)')
    for p in glob.glob('.claude/**/*.json', recursive=True):
        try:
            json.load(open(p))
        except ValueError as e:
            flag('P0', p, 'not valid JSON, so Claude Code ignores it (%s)' % e)
    if subprocess.run(['diff', '-rq', 'TDS_Mod/ModSource/Shared', 'TDS_Vessel_Creator/kit/Shared'],
                      capture_output=True).returncode:
        flag('P0', 'TDS_Mod/ModSource/Shared', 'differs from TDS_Vessel_Creator/kit/Shared')
    plan = open('docs/PLAN.md').read()
    now = re.search(r'^### Now\n(.*?)^### ', plan, re.S | re.M)
    if now and len(now.group(1).encode()) > NOW_LIMIT:
        flag('P1', 'docs/PLAN.md', 'Part C "Now" is %d bytes, over %d' % (len(now.group(1).encode()), NOW_LIMIT))
    log = re.search(r'^### Log\n(.*?)^### ', plan, re.S | re.M)
    if log and re.search(r'^- \*\*\d{4}-\d\d-\d\d', log.group(1), re.M):
        flag('P1', 'docs/PLAN.md', 'a dated entry in the Log section: it goes in its own docs/log/ file')

    # What the branch adds
    try:
        added, changed = added_lines(base), changed_files(base)
    except subprocess.CalledProcessError as e:
        sys.exit('rules: cannot diff against %s (%s); fetch it first' % (base, e.stderr.strip()))
    kit_readme = 'TDS_Vessel_Creator/kit/README.md'
    if any(p.startswith('TDS_Vessel_Creator/kit/') and p != kit_readme for p in changed) and \
            not any(re.match(r'\| \d+\.\d+', text) for _, text in added.get(kit_readme, [])):
        flag('P0', 'TDS_Vessel_Creator/kit', 'changed without a new row in kit/README.md "Versions"')
    for p, lines in added.items():
        for n, text in lines:
            where = '%s:%d' % (p, n)
            if p.startswith('TDS_Mod/ModSource/') and p.endswith('.cs'):
                code = text.split('//', 1)[0]
                if BLOCKED_API.search(code):
                    flag('P0', where, 'System.IO / System.Reflection: the game refuses to load the mod')
            if p.endswith(('.md', '.txt')) and not p.startswith(OLD_NAMES_OK) and OLD_NAMES.search(text):
                flag('P1', where, 'old name "%s" (now Trinkets / Cardinals)' % OLD_NAMES.search(text).group(0))
            if p.endswith('.md') and not p.startswith(('docs/reports/', 'docs/log/')):
                for cite in re.findall(r'`([^`\s]+)`', text):
                    if cite.startswith(ROOTS) and not re.search(r'[<>*{}$~\\|]', cite) \
                            and not cited_path_exists(cite):
                        flag('P1', where, 'cites `%s`, which does not exist' % cite)
    for p in changed:
        if p.startswith('TDS_Mod/ModSource/') and p.lower().endswith('.png') and os.path.exists(p):
            for what in art_problems(p, foe='Foes' in p):
                flag('P1', p, what)

    if found:
        print('\n'.join(found))
        print('rules: %d finding(s)' % len(found))
        sys.exit(1)
    print('rules: clean')


if __name__ == '__main__':
    main()
