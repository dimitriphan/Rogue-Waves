"""make_handoff.py: packs a PC handoff from git, for Apply-Handoff.ps1 on Dimitri's PC.

  python3 TDS_Mod/Tools/pc/make_handoff.py <Name> (--manifest <pc_manifest.txt> | --since <git ref>)
        [--ref HEAD] [--only Argo,Shared,...] [--force <path>...] [--note "line for the README"] [--zip <out.zip>]

Writes TDS_Mod/Handoff/<Name>/ (HASHES.txt, README.md) in the work tree: commit and push it, and Get-Handoff.ps1 on
the PC fetches the handoff from the branch. --zip also writes the same handoff as one zip (files, HASHES.txt,
README.md and the PC scripts), for a PC without the token: Dimitri saves it into Desktop\\ClaudeProjects\\handoff and
unpacks it.

What goes in: every file under TDS_Mod/ModSource and TDS_Mod/Assets/*/Editor at --ref (or only the vessels and
folders --only names) whose hash differs from what the PC has.
  --manifest: the PC's own pc_manifest.txt (Get-PcManifest.ps1, written again by every Apply-Handoff -Apply). Exact:
              each file's base is the hash the PC has, "new" where it has none.
  --since:    a git ref the PC is known to match (the last handoff's commit). Bases are that ref's hashes.
  --force:    paths (under TDS_Mod) to replace whatever the PC has (base "*"); use only when the thread knows why.
With --manifest, a PC copy that matches no version git has ever had is a PC edit: it is left out and listed.
Hashes are HandoffHash.ps1's: the first 16 hex digits of sha256, text with any BOM and every CR removed.
"""
import argparse, hashlib, os, subprocess, sys, zipfile, datetime

TEXT = {'.cs', '.ps1', '.md', '.txt', '.json', '.lua', '.py', '.sh', '.html', '.csv', '.xml', '.asmdef'}
HERE = os.path.dirname(os.path.abspath(__file__))
SKIP = ('/Build/', '/Research/', '/Previews/', '/bin/', '/obj/', '/Aseprite/generated/')

def h16(path, data):
    if os.path.splitext(path)[1].lower() in TEXT:
        data = data.decode('utf-8', errors='replace').lstrip('\ufeff').replace('\r', '').encode('utf-8')
    return hashlib.sha256(data).hexdigest()[:16]

def git(*args):
    return subprocess.run(['git'] + list(args), capture_output=True, check=True).stdout

def tree(ref, only):
    names = git('ls-tree', '-r', '--name-only', ref, '--', 'TDS_Mod/ModSource', 'TDS_Mod/Assets').decode().split('\n')
    out = []
    for n in names:
        if not n:
            continue
        rel = n[len('TDS_Mod/'):]
        if any(s in '/' + rel for s in SKIP):
            continue
        parts = rel.split('/')
        if parts[0] == 'Assets' and (len(parts) < 4 or parts[2] != 'Editor'):
            continue
        if only:
            vessel = parts[1][:-3] if parts[0] == 'Assets' and parts[1].endswith('Mod') else parts[1]
            if vessel not in only:
                continue
        out.append(rel)
    return out

def blob(ref, rel):
    try:
        return git('show', '%s:TDS_Mod/%s' % (ref, rel))
    except subprocess.CalledProcessError:
        return None

def history(rel):
    # Hashes of every version of the file git has ever seen (any branch), to tell a PC copy that is merely old from a
    # PC edit that never reached GitHub.
    seen = set()
    for c in git('log', '--all', '--format=%H', '--', 'TDS_Mod/' + rel).decode().split():
        data = blob(c, rel)
        if data is not None:
            seen.add(h16(rel, data))
    return seen

def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('name')
    ap.add_argument('--manifest')
    ap.add_argument('--since')
    ap.add_argument('--ref', default='HEAD')
    ap.add_argument('--only', default='')
    ap.add_argument('--force', nargs='*', default=[])
    ap.add_argument('--note', action='append', default=[])
    ap.add_argument('--zip')
    a = ap.parse_args()
    if bool(a.manifest) == bool(a.since):
        sys.exit('give exactly one of --manifest or --since')
    os.chdir(git('rev-parse', '--show-toplevel').decode().strip())
    sha = git('rev-parse', a.ref).decode().strip()
    only = set(x for x in a.only.split(',') if x)
    pc, pc_note = {}, ''
    if a.manifest:
        for line in open(a.manifest, encoding='utf-8-sig'):
            line = line.rstrip('\r\n')
            if line.startswith('#'):
                pc_note = line.lstrip('# ')
                continue
            if '  ' in line:
                hsh, rel = line.split('  ', 1)
                pc[rel.replace('\\', '/')] = hsh
    force = set(f.replace('\\', '/') for f in a.force)
    rows, files, edits = [], {}, []
    for rel in tree(a.ref, only):
        data = blob(a.ref, rel)
        new = h16(rel, data)
        if a.manifest:
            base = pc.get(rel, 'new')
            if base not in ('new', new) and rel not in force and base not in history(rel):
                edits.append(rel)   # the PC's copy was never on GitHub: don't overwrite it unless told to
                continue
        else:
            old = blob(a.since, rel)
            base = h16(rel, old) if old is not None else 'new'
        if rel in force:
            base = '*'
        if base == new:
            continue
        rows.append((base, new, rel))
        files[rel] = data
    pc_only = sorted(r for r in pc if r not in set(tree(a.ref, only)) and (not only or any(
        ('/' + v + '/') in ('/' + r) or ('/' + v + 'Mod/') in ('/' + r) for v in only))) if a.manifest else []
    out = os.path.join('TDS_Mod', 'Handoff', a.name)
    os.makedirs(out, exist_ok=True)
    with open(os.path.join(out, 'HASHES.txt'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('# Handoff %s: commit %s of %s. base[,older]  new  path under TDS_Mod (Apply-Handoff.ps1)\n' % (a.name, sha[:7], a.ref))
        f.write('# Bases from %s.\n' % ('the PC manifest (%s)' % pc_note if a.manifest else 'commit ' + git('rev-parse', a.since).decode().strip()[:7]))
        for base, new, rel in rows:
            f.write('%s  %s  %s\n' % (base, new, rel.replace('/', '\\')))
    expect = {'new': 'will add', '*': 'will replace'}
    lines = ['# Handoff %s (%s)' % (a.name, datetime.date.today().isoformat()), '',
             'Commit `%s` (`%s`). Made by `TDS_Mod/Tools/pc/make_handoff.py`; the folders mirror `C:\\Users\\Dimitri\\TDS_Mod`.' % (sha[:7], a.ref), '']
    lines += a.note + ([''] if a.note else [])
    lines += ['`Apply-Handoff.ps1` (check only) should list:', '']
    lines += ['- %s: `%s`' % (expect.get(b, 'will copy'), r.replace('/', '\\')) for b, n, r in rows] or ['- nothing: the PC is up to date']
    if edits:
        lines += ['', 'Left out, edited on the PC and never on GitHub (bring them to the cloud first, or pack again with --force): '
                  + ', '.join('`%s`' % r.replace('/', '\\') for r in edits)]
    if pc_only:
        lines += ['', 'On the PC only (left alone): ' + ', '.join('`%s`' % r.replace('/', '\\') for r in pc_only[:30])
                  + (' and %d more' % (len(pc_only) - 30) if len(pc_only) > 30 else '')]
    lines += ['', '## Steps', '',
              '1. Only one session works in TDS_Mod at a time (`pc/QUEUE.md` in the Claude Project\'s files).',
              '2. `Apply-Handoff.ps1` (check only). Anything "DIFFERS" means the PC has edits that aren\'t on GitHub: stop and report them.',
              '3. `Apply-Handoff.ps1 -Apply`, once Dimitri has OK\'d it in the thread in his own words. It backs up what it replaces',
              '   and writes a fresh `handoff\\pc_manifest.txt`.',
              '4. Build, test and release as the thread\'s brief says (`TDS_Mod/Tools/pc/README.md`).', '']
    with open(os.path.join(out, 'README.md'), 'w', encoding='utf-8', newline='\n') as f:
        f.write('\n'.join(lines))
    if a.zip:
        with zipfile.ZipFile(a.zip, 'w', zipfile.ZIP_DEFLATED) as z:
            for rel, data in files.items():
                z.writestr(rel, data)
            for n in ('HASHES.txt', 'README.md'):
                z.write(os.path.join(out, n), n)
            for n in sorted(os.listdir(HERE)):
                if n.endswith('.ps1'):
                    z.write(os.path.join(HERE, n), n)
    print('%s: %d file(s) for the PC from %s (%s)%s' % (a.name, len(rows), sha[:7], a.ref, '; zip ' + a.zip if a.zip else ''))
    for b, n, r in rows:
        print('  %-12s %s' % (expect.get(b, 'will copy'), r))
    for r in edits:
        print('  LEFT OUT     %s: the PC copy was never on GitHub (a PC edit); --force %s replaces it' % (r, r))

if __name__ == '__main__':
    main()
