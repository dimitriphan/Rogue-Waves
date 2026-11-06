#!/usr/bin/env python3
"""Run Rogue Waves' mechanical checks through the Message Batches API (half the normal price, results within 24 h).

Checks:
  text    every player-facing string of the six parts, read for spelling, grammar, the naming rule and stale facts
          (the recipe of docs/reports/TEXT_AUDIT_2026-11-05.md section 1)
  scope   one scope file at a time, every line mapped to code file:line with a mark: MATCH, DIFFERENT, MISSING,
          UNBUILT-QUEUED or UNBUILT-BY-DESIGN (the recipe of docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md)
  design  each vessel's DESIGN.md, every number and rule mapped to the code: MATCH, DIFFERENT or NOT-IN-CODE
  docs    CLAUDE.md, PLAN Part C "Now", the reports' README and the project skills read against each other for
          contradictions and stale facts, after a free local pass that lists every cited path that does not exist
  art     sprite sheets and icons, each shown on Deep Swell and on a light ground, read against docs/STYLE.md, with
          concept-tools/temp_art/art_check.py's findings beside each

From the repository root (README.md beside this file has the key, the steps and the costs):
  python3 TDS_Mod/Tools/batch/run_checks.py plan    [--check LIST] [--model sonnet|haiku] [--since REF|last] [--exact]
  python3 TDS_Mod/Tools/batch/run_checks.py submit  [--check ...] [--model ...] [--since ...] [--max-cost USD] [--no-wait]
  python3 TDS_Mod/Tools/batch/run_checks.py collect BATCH_ID
  python3 TDS_Mod/Tools/batch/run_checks.py spend   [--month YYYY-MM]

plan sends nothing and costs nothing (with --exact it calls the free token-count endpoint, which needs the key).
submit sends one batch, waits for it and writes docs/reports/BATCH_CHECKS_<date>.md; collect does the second half
for a batch submitted with --no-wait or interrupted. spend adds up what this month's reports cost. The key is read
from RW_BATCH_API_KEY (a name Claude Code never reads), else ANTHROPIC_API_KEY, else asked for once (hidden, never
stored) when a terminal is there.
Only the mod's own code and art, the scope, and the project's docs are sent; never the game's binaries or sprites.

Exit codes: 0 done (or nothing to check), 2 bad arguments, 3 no key, 4 over --max-cost, 1 any other failure.
"""
import argparse, base64, datetime, getpass, importlib.util, io, json, os, re, subprocess, sys, time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SRC = ROOT / "TDS_Mod" / "ModSource"
REPORTS = ROOT / "docs" / "reports"
OUT = Path(os.environ.get("RW_BATCH_OUT") or Path(__file__).resolve().parent / "out")

MODELS = {"sonnet": "claude-sonnet-5-5", "haiku": "claude-haiku-5-5"}
# Standard $ per million tokens (2026-11-05); a batch costs half. Haiku 5.5 charges the higher row for a prompt over
# 100K tokens. Cache writes cost 1.25x input and reads 0.1x; inside a batch, cache hits are best effort.
PRICES = {
    "claude-sonnet-5-5": [(None, 2.00, 10.00)],
    "claude-haiku-5-5": [(100_000, 0.10, 0.50), (None, 0.50, 2.50)],
}
BATCH = 0.5
MONTHLY_CREDIT = 100.0

CHECKS = ["text", "scope", "design", "docs", "art"]
PARTS = ["Yossele", "Spool", "Argo", "Uloe", "RogueWavesMode", "RogueWavesFoes"]
VESSELS = ["Yossele", "Spool", "Argo", "Uloe"]          # the parts with a DESIGN.md
# Scope file -> the parts whose game code it is checked against, the mode first so its cached block is shared.
# 00 (rules), 02 (progress log) and 15 (templates) describe no game behaviour and have no entry.
SCOPE_PARTS = {
    "01": ["RogueWavesMode"], "03": ["RogueWavesMode", "Shared"],
    "04": ["RogueWavesMode"], "05": ["RogueWavesMode"], "06": ["RogueWavesMode"],
    "07": ["Yossele"], "08": ["Spool"], "09": ["Argo"], "10": ["Uloe"],
    "11": ["RogueWavesMode", "Yossele", "Spool", "Argo", "Uloe"],   # each vessel's Directions span its files
    "12": ["RogueWavesMode"], "13": ["RogueWavesMode", "RogueWavesFoes"], "14": ["RogueWavesMode"],
    "16": ["RogueWavesMode"],
}
DEFAULT_SCOPE = sorted(SCOPE_PARTS)
VESSEL_DATA = {"07": "Yossele", "08": "Spool", "09": "Argo", "10": "Uloe"}
# The docs check: whole files, plus PLAN's "Now" only (the Log is history and stays out).
DOC_FILES = ["CLAUDE.md", "docs/reports/README.md", "TDS_Mod/Tools/batch/README.md"]
DOC_GLOBS = ["docs/skills/*.md"]
TEXT_CHUNK = 250          # strings per text request
ART_CHUNK = 8             # images per art request
GUESS_OUT = {"text": 3_000, "scope": 14_000, "design": 10_000, "docs": 4_000, "art": 3_000}
# Each request's max_tokens (thinking included): the most it can write, so the ceiling --max-cost holds a run to.
MAX_OUT = {"text": 8_000, "scope": 40_000, "design": 24_000, "docs": 8_000, "art": 8_000}
DEEP_SWELL, LIGHT = (0x2E, 0x4A, 0x62, 255), (0xE9, 0xF1, 0xEE, 255)   # docs/STYLE.md: Deep Swell and Foam


# ---------------------------------------------------------------- reading the repository

def game_files(part):
    """A part's game code: Code/NN_*.cs with NN below 90 (90-99 are self-tests, reviews, shots and play builds).
    Shared is the live kit, every file."""
    if part == "Shared":
        return sorted((SRC / "Shared").glob("*.cs"))
    return [f for f in sorted((SRC / part / "Code").glob("*.cs")) if f.name[:2].isdigit() and int(f.name[:2]) < 90]


def is_game_path(path, part):
    """True when a repository path (from git) is, or was, one of the part's game files."""
    if part == "Shared":
        return path.startswith("TDS_Mod/ModSource/Shared/") and path.endswith(".cs")
    name = path.rpartition("/")[2]
    return (path.startswith(f"TDS_Mod/ModSource/{part}/Code/") and path.endswith(".cs")
            and name[:2].isdigit() and int(name[:2]) < 90)


def rel(f):
    return f.relative_to(SRC).as_posix()


def repo_rel(f):
    return f.resolve().relative_to(ROOT).as_posix()


def numbered(f, label=None):
    lines = f.read_text(encoding="utf-8-sig").split("\n")
    return f"=== {label or rel(f)} ===\n" + "\n".join(f"{i}\t{l.rstrip()}" for i, l in enumerate(lines, 1))


def cs_strings(text):
    """Yield (line, literal) for each C# string literal outside comments. Interpolations read as {...}."""
    i, n, line = 0, len(text), 1
    while i < n:
        c = text[i]
        if c == "\n":
            line += 1; i += 1; continue
        if text.startswith("//", i):
            j = text.find("\n", i); i = n if j < 0 else j; continue
        if text.startswith("/*", i):
            j = text.find("*/", i + 2); j = n if j < 0 else j + 2
            line += text.count("\n", i, j); i = j; continue
        if c == "'":
            j = i + 1
            while j < n and text[j] not in "'\n":
                j += 2 if text[j] == "\\" else 1
            i = j + 1; continue
        m = re.match(r'(\$@|@\$|\$|@)?"', text[i:i + 3])
        if not m or (i > 0 and (text[i - 1].isalnum() or text[i - 1] == "_") and m.group(1)):
            i += 1; continue
        verbatim, interp = "@" in (m.group(1) or ""), "$" in (m.group(1) or "")
        j, start, buf = i + len(m.group(0)), line, []
        while j < n:
            ch = text[j]
            if ch == "\n":
                line += 1
            if verbatim and text.startswith('""', j):
                buf.append('"'); j += 2; continue
            if not verbatim and ch == "\\":
                buf.append(text[j:j + 2]); j += 2; continue
            if interp and ch == "{":
                if text.startswith("{{", j):
                    buf.append("{"); j += 2; continue
                depth, k = 1, j + 1
                while k < n and depth:          # skip the expression, nested strings included
                    if text[k] == '"':
                        k = text.find('"', k + 1)
                        if k < 0: k = n
                    elif text[k] == "{": depth += 1
                    elif text[k] == "}": depth -= 1
                    k += 1
                buf.append("{...}"); j = k; continue
            if ch == '"':
                j += 1; break
            buf.append(ch); j += 1
        yield start, "".join(buf)
        i = j


def player_strings(parts=PARTS):
    """Strings of two or more words from the parts' game code, each text once with every place it appears."""
    seen = {}
    for part in parts:
        for f in game_files(part):
            for line, s in cs_strings(f.read_text(encoding="utf-8-sig")):
                if re.search(r"[A-Za-z]{2,}[^\S\n]+[A-Za-z]{2,}", s) and "/" not in s.split(" ")[0]:
                    seen.setdefault(s, []).append(f"{rel(f)}:{line}")
    return seen


def plan_now():
    text = (ROOT / "docs" / "PLAN.md").read_text(encoding="utf-8")
    m = re.search(r"^### Now\n(.*?)^### Log", text, re.S | re.M)
    return m.group(1).strip() if m else ""


def git(*args):
    return subprocess.run(["git", "-C", str(ROOT), *args], capture_output=True, text=True)


def base_commit(short=True):
    try:
        return git("rev-parse", *(["--short"] if short else []), "HEAD").stdout.strip() or "?"
    except OSError:
        return "?"


BASES_LINE = re.compile(r"<!-- batch-bases: (\{.*?\}) -->")


def prior_bases():
    """Each check's base from the newest report that records them: the commit up to which that check last ran in
    full with every request answered. A narrowed run (--scope-files, --art), a failed request or an explicit
    --since REF leaves the check's old base, so `--since last` never skips what such a run left out."""
    for p in sorted(REPORTS.glob("BATCH_CHECKS_*.md"), reverse=True):
        m = BASES_LINE.search(p.read_text(encoding="utf-8"))
        if m:
            return json.loads(m.group(1))
    return {}


def changed_since(ref):
    """Repository paths changed between ref and HEAD (committed changes only), or None for 'everything'."""
    if ref is None:
        return None
    r = git("diff", "--name-only", ref, "HEAD")
    if r.returncode:
        print(f"--since {ref}: {r.stderr.strip() or 'git diff failed'}", file=sys.stderr)
        sys.exit(2)
    print(f"--since {ref}: {len(r.stdout.split())} changed files.")
    return set(r.stdout.split())


def doc_paths():
    files = [ROOT / f for f in DOC_FILES]
    for g in DOC_GLOBS:
        files += sorted(ROOT.glob(g))
    return [f for f in files if f.exists()]


PATH_TOKEN = re.compile(r"`([A-Za-z0-9_.\-]+(?:/[A-Za-z0-9_.\-<>]*)+)`")


def missing_paths():
    """The free local half of the docs check: every `path/like/this` cited in the docs files that names nothing in
    the repository (tried from the root, from TDS_Mod/ModSource, from the kit, and as a prefix such as docs/02)."""
    texts = [(repo_rel(f), f.parent, f.read_text(encoding="utf-8")) for f in doc_paths()]
    texts.append(("docs/PLAN.md (Now)", ROOT / "docs", plan_now()))
    found = []
    for name, here, text in texts:
        bases = [here, ROOT, SRC, ROOT / "TDS_Vessel_Creator", ROOT / "TDS_Mod", ROOT / "TDS_Mod" / "Tools"]
        for n, line in enumerate(text.split("\n"), 1):
            for tok in PATH_TOKEN.findall(line):
                if "<" in tok or tok.startswith(("http", "mnt/", "claude/", "origin/", "Code/")):
                    continue
                t = tok.rstrip("/.")
                if any((b / t).exists() or any(b.glob(t + "*")) for b in bases):
                    continue
                found.append(f"{name}:{n}: `{tok}`")
    return found


def art_files(paths, changed):
    """The PNGs for the art check: the paths given (files or folders, inside the repository), or the PNGs under a
    part's Art folder changed since --since, or by default the art in use (Art/*.png, Art/Temporary/game_size)."""
    out = []
    if paths:
        for p in paths:
            p = (Path.cwd() / p).resolve()
            try:
                p.relative_to(ROOT)
            except ValueError:
                print(f"--art {p}: outside the repository; only the pack's own art is sent.", file=sys.stderr)
                sys.exit(2)
            out += sorted(p.rglob("*.png")) if p.is_dir() else [p]
    elif changed is not None:
        out = [ROOT / c for c in sorted(changed)
               if re.match(r"TDS_Mod/ModSource/[^/]+/Art/.*\.png$", c) and (ROOT / c).exists()]
    else:
        for part in PARTS:
            out += sorted((SRC / part / "Art").glob("*.png"))
            out += sorted((SRC / part / "Art" / "Temporary" / "game_size").glob("*.png"))
    tracked = set(git("ls-files", "*.png").stdout.split())
    keep = []
    for f in out:                 # only files git tracks: nothing untracked, ignored or linked from outside is sent
        try:
            name = f.resolve().relative_to(ROOT).as_posix()
        except ValueError:
            continue
        if name in tracked and "preview_8x" not in f.parts:
            keep.append(ROOT / name)
    return keep


def art_check_module():
    spec = importlib.util.spec_from_file_location("art_check", ROOT / "concept-tools" / "temp_art" / "art_check.py")
    mod = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(mod)
    return mod


def art_image(path):
    """The sprite scaled up (nearest neighbour, at most 8x, the long side at most 1568 px) and shown twice, on Deep
    Swell above and on Foam below. Returns (base64 png, width, height, scale)."""
    from PIL import Image
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    k = max(1, min(8, 1568 // w, 784 // h))
    big = im.resize((w * k, h * k), Image.NEAREST)
    pad = 4 * k
    sheet = Image.new("RGBA", (w * k + 2 * pad, 2 * (h * k + 2 * pad)), LIGHT)
    sheet.paste(Image.new("RGBA", (w * k + 2 * pad, h * k + 2 * pad), DEEP_SWELL), (0, 0))
    sheet.alpha_composite(big, (pad, pad))
    sheet.alpha_composite(big, (pad, h * k + 3 * pad))
    buf = io.BytesIO()
    sheet.convert("RGB").save(buf, "PNG")
    return base64.b64encode(buf.getvalue()).decode(), sheet.size[0], sheet.size[1], k


# ---------------------------------------------------------------- the requests

PREAMBLE = ("You check Rogue Waves, a fan expansion pack for the game TDS - Tower Defense Strategy. The pack's own "
            "files follow; the task comes after them.")

TEXT_SYSTEM = """You proofread the player-facing text of Rogue Waves, a fan expansion pack for the game TDS - Tower \
Defense Strategy. Each string below comes from the pack's C# code; {...} marks a value filled in at run time and \\n a \
line break. Report only real problems a player would notice; most strings are fine and should not be listed.

Look for:
- spelling: the pack writes British English (colour, centre, armour); a US spelling in player text is a finding
- grammar: wrong word, missing word, doubled word, agreement, a sentence that does not parse
- naming: the old names "Vessel Cardinal(s)" (now Trinkets) and "Team Cardinal(s)" (now Cardinals) must not appear in \
player text; Rogue Waves is plural ("Rogue Wave" alone is wrong)
- stale: "15 maps" (a campaign is 10 maps, won by leaving map 10 after Tide 15), "Swell" or "ascension" for the \
difficulty ladder (now Depth)
- unclear: text a player cannot act on or that contradicts itself

Not problems: invented names of the pack and the base game (vessels Yossele, Spool, Argo, Uloe, Smacky, Sno Sno, \
Boutman, Boutbro, Fie, Buster; Mushboom, Cordyfriend, VENDCINT, Snoble, and every building, unit, Direction, \
Trinket, Cardinal, objective, foe, weather and resource name); terse labels, key names and log lines; capitalised \
game terms (Tide, Aura, Pure Aura, Directions); deliberate words such as "Tanky" and "offcuts".

Answer with the findings only, each by the string's id."""

FINDINGS_SCHEMA = {
    "type": "object",
    "properties": {"findings": {"type": "array", "items": {
        "type": "object",
        "properties": {
            "id": {"type": "string"},
            "kind": {"type": "string"},
            "problem": {"type": "string"},
            "suggestion": {"type": "string"},
        },
        "required": ["id", "kind", "problem", "suggestion"], "additionalProperties": False}}},
    "required": ["findings"], "additionalProperties": False,
}


def findings_schema(kinds):
    s = json.loads(json.dumps(FINDINGS_SCHEMA))
    s["properties"]["findings"]["items"]["properties"]["kind"]["enum"] = kinds
    return s


TEXT_SCHEMA = findings_schema(["spelling", "grammar", "naming", "stale", "unclear"])

SCOPE_TASK = """Audit the pack against its scope. The scope file (each line numbered) says what to build; the code \
above (each line numbered, file by file) is what is built. Go through the scope file from top to bottom and map every \
line that states a behaviour, a number, a name or a text to the code that does it. Group neighbouring lines that one \
code location covers. Skip headings, comments (lines starting with #) and blank lines.

Marks:
- MATCH: the code does what the line says, at the location you give
- DIFFERENT: built, and the code does something else (a number, a rule, a name or a text differs): say what differs
- MISSING: the scope asks for it, nothing in the code builds it, and nothing in the plan queues it
- UNBUILT-QUEUED: not built, and the plan queues it: name the queue item
- UNBUILT-BY-DESIGN: not built, and the scope itself says so (on hold, a ! NEEDS ! slot, "later")

Scope symbols: | Title |, [System], {Thing}, (Kind), % Level:{Upgrade}; a value between tildes ~like this~ is a \
default Claude picked, which still counts. Code locations are file:line or file:first-last, with the file as \
given in the === headers. Be exact: a MATCH needs the line that holds the number or the rule, and a DIFFERENT or \
MISSING row must say what you looked for. Report every scope line with content; do not stop early."""

DESIGN_TASK = """Check the vessel's design page against its code. The page (each line numbered) is the vessel's \
written design: its numbers, upgrade trees, units and rules. Go through it from top to bottom and map every line that \
states a number, a rule or a name the game uses to the code above that does it. Group neighbouring lines that one \
code location covers. Skip headings, history notes, art notes, open questions and blank lines.

Marks:
- MATCH: the code does what the line says, at the location you give
- DIFFERENT: the code does something else (a number, a rule or a name differs): give both values
- NOT-IN-CODE: nothing in the code does it (say what you looked for; the page may describe a later plan)

Code locations are file:line or file:first-last, with the file as given in the === headers. A MATCH needs the line \
that holds the number or the rule. Report every line with content; do not stop early."""


def rows_schema(marks):
    return {
        "type": "object",
        "properties": {"rows": {"type": "array", "items": {
            "type": "object",
            "properties": {
                "lines": {"type": "string"},
                "says": {"type": "string"},
                "code": {"type": "string"},
                "mark": {"type": "string", "enum": marks},
                "note": {"type": "string"},
            },
            "required": ["lines", "says", "code", "mark", "note"], "additionalProperties": False}}},
        "required": ["rows"], "additionalProperties": False,
    }


SCOPE_MARKS = ["MATCH", "DIFFERENT", "MISSING", "UNBUILT-QUEUED", "UNBUILT-BY-DESIGN"]
DESIGN_MARKS = ["MATCH", "DIFFERENT", "NOT-IN-CODE"]

DOCS_TASK = """Read the project's guiding documents above against each other. They tell every working session what \
the state is, which rules hold and where things live; the newest dated entry wins when two disagree. Report only \
real problems a reader would trip on; most lines are fine and should not be listed.

Look for:
- contradiction: two places state different values for the same thing (a version, a next free number, a PR's state, \
a rule, a count, a model, a path)
- stale: a fact a newer entry in these documents replaced (for example a campaign of 15 maps, "Swell" for Depth, \
"Vessel Cardinals" or "Team Cardinals", a kit version or pitfall number older than the "Next free numbers" line)
- missing-path: a cited path from the local list below that a reader needs (ignore ones that are clearly examples)
- unclear: an instruction a session could not follow as written

Give each finding's id as file:line (with the file as in the === headers) and say what the fix is."""

DOCS_SCHEMA = findings_schema(["contradiction", "stale", "missing-path", "unclear"])

ART_TASK = """Review the pack's art against the style sheet above. Each image shows one file, scaled up with no \
smoothing, twice: on Deep Swell (the mode's dark UI colour) above and on Foam (a light ground) below. The text before \
each image gives its id, path, real size, scale and what the automatic script found.

Read each piece against the sheet: its palette (the vessel's row of the roster, the pack colours), opaque pixels, no \
outline, lit from the upper left, shade that keeps the hue, no pure black, magenta only on foes, the vessel's motif on \
its towers, and whether it reads at game size on both grounds. A sheet's frames should keep one size and baseline. \
Report only real problems; a piece that follows the sheet gets no finding. Give each finding the image's id."""

ART_SCHEMA = findings_schema(["palette", "outline", "light", "black", "magenta", "motif", "readability",
                              "frames", "other"])


def params(model, effort, max_tokens, system, user, schema):
    return {
        "model": model, "max_tokens": max_tokens, "system": system,
        "messages": [{"role": "user", "content": user}],
        "output_config": {"effort": effort, "format": {"type": "json_schema", "schema": schema}},
    }


def code_system(parts):
    """The preamble, then one block per part. The same part gives the same block in every check, so a scope request
    and a design request for one vessel can share the cache. Only the first and the last block carry a cache mark:
    the API takes at most 4 a request (scope 11 has five parts), and only the first part's prefix (the mode's) is
    shared between requests."""
    system, files = [{"type": "text", "text": PREAMBLE}], []
    for k, part in enumerate(parts):
        part_files = game_files(part)
        files += part_files
        block = {"type": "text", "text": f"The code of {part} (game code only; self-tests left out):\n\n"
                                         + "\n\n".join(numbered(f) for f in part_files)}
        if k in (0, len(parts) - 1):
            block["cache_control"] = {"type": "ephemeral"}
        system.append(block)
    return system, files


def text_requests(model, effort, changed):
    parts = PARTS if changed is None else [p for p in PARTS if any(is_game_path(c, p) for c in changed)]
    if not parts:
        return []
    strings = player_strings(parts)
    def place(kv):
        f, _, line = kv[1][0].rpartition(":")
        return f, int(line)
    items = sorted(strings.items(), key=place)
    reqs = []
    for c in range(0, len(items), TEXT_CHUNK):
        chunk, meta, rows = items[c:c + TEXT_CHUNK], {}, []
        for k, (s, where) in enumerate(chunk, c + 1):
            sid = f"S{k}"
            meta[sid] = {"text": s, "where": where}
            rows.append(f"{sid} | {where[0]} | {s}")
        cid = f"text-{c // TEXT_CHUNK + 1:02d}"
        user = "Proofread these strings (id | first place | text):\n\n" + "\n".join(rows)
        reqs.append((cid, {"check": "text", "strings": meta},
                     params(model, effort, MAX_OUT["text"], TEXT_SYSTEM, user, TEXT_SCHEMA)))
    return reqs


def scope_requests(model, effort, which, changed):
    if changed is not None:
        which = [n for n in which
                 if any(is_game_path(c, p) for c in changed for p in SCOPE_PARTS[n])
                 or any(re.match(rf"docs/scope/{n}_.*\.txt$", c) for c in changed)]
    now = plan_now()
    reqs = []
    for num in which:
        path = next((ROOT / "docs" / "scope").glob(f"{num}_*.txt"))
        system, files = code_system(SCOPE_PARTS[num])
        scope = path.read_text(encoding="utf-8-sig").split("\n")
        user = f"{SCOPE_TASK}\n\nThe plan's current state (what is queued, what is done), docs/PLAN.md Part C:\n\n{now}\n\n"
        if num in VESSEL_DATA:
            helper = ROOT / "TDS_Mod" / "Tools" / "cloud-check" / "scope_upgrades.py"
            data = SRC / VESSEL_DATA[num] / "Code" / "02_Data.cs"
            r = subprocess.run([sys.executable, str(helper), str(path), str(data)],
                               capture_output=True, text=True, cwd=ROOT)
            if r.returncode == 0 and r.stdout.strip():
                user += ("A script already matched each upgrade line to its entry in 02_Data.cs (scope line, name, "
                         "code line, what differs); check its leads and use them:\n\n" + r.stdout.strip() + "\n\n")
        user += (f"The scope file docs/scope/{path.name}, lines numbered:\n\n"
                 + "\n".join(f"{i}\t{l.rstrip()}" for i, l in enumerate(scope, 1))
                 + f"\n\nAudit every line of {path.name} against the code. Give lines as {num}:N or {num}:N-M.")
        reqs.append((f"scope-{num}", {"check": "scope", "file": path.name, "code_files": [rel(f) for f in files]},
                     params(model, effort, MAX_OUT["scope"], system, user, rows_schema(SCOPE_MARKS))))
    return reqs


def design_requests(model, effort, changed):
    reqs = []
    for part in VESSELS:
        page = SRC / part / "DESIGN.md"
        if not page.exists():
            continue
        if changed is not None and not (f"TDS_Mod/ModSource/{part}/DESIGN.md" in changed
                                        or any(is_game_path(c, part) for c in changed)):
            continue
        system, files = code_system([part])
        user = (f"{DESIGN_TASK}\n\nThe design page {rel(page)}, lines numbered:\n\n"
                + "\n".join(f"{i}\t{l.rstrip()}" for i, l in enumerate(page.read_text(encoding='utf-8-sig').split('\n'), 1))
                + "\n\nCheck every line of the page against the code. Give lines as D:N or D:N-M.")
        reqs.append((f"design-{part}", {"check": "design", "file": rel(page), "code_files": [rel(f) for f in files]},
                     params(model, effort, MAX_OUT["design"], system, user, rows_schema(DESIGN_MARKS))))
    return reqs


def docs_requests(model, effort, changed):
    files = doc_paths()
    names = {repo_rel(f) for f in files} | {"docs/PLAN.md"}
    if changed is not None and not (names & changed):
        return []
    missing = missing_paths()
    blocks = [numbered(f, repo_rel(f)) for f in files]
    now = plan_now().split("\n")
    blocks.append("=== docs/PLAN.md (Part C, Now) ===\n" + "\n".join(f"{i}\t{l}" for i, l in enumerate(now, 1)))
    system = [{"type": "text", "text": PREAMBLE},
              {"type": "text", "cache_control": {"type": "ephemeral"}, "text": "\n\n".join(blocks)}]
    user = (f"{DOCS_TASK}\n\nCited paths the local pass could not find ({len(missing)}):\n"
            + ("\n".join(missing) if missing else "none"))
    return [("docs-01", {"check": "docs", "missing": missing},
             params(model, effort, MAX_OUT["docs"], system, user, DOCS_SCHEMA))]


def art_requests(model, effort, files):
    if not files:
        return []
    ac = art_check_module()
    style = (ROOT / "docs" / "STYLE.md").read_text(encoding="utf-8")
    system = [{"type": "text", "text": PREAMBLE},
              {"type": "text", "cache_control": {"type": "ephemeral"}, "text": "=== docs/STYLE.md ===\n" + style}]
    reqs = []
    for c in range(0, len(files), ART_CHUNK):
        content, meta, img_tokens = [{"type": "text", "text": ART_TASK}], {}, 0
        for k, f in enumerate(files[c:c + ART_CHUNK], c + 1):
            aid = f"A{k}"
            data, w, h, scale = art_image(f)
            problems, notes = ac.check(str(f), None, None, None, False)
            from PIL import Image
            real = Image.open(f).size
            found = "; ".join(problems + [f"note, {n}" for n in notes]) or "nothing"
            meta[aid] = {"path": repo_rel(f), "script": found}
            content.append({"type": "text", "text": f"{aid}: {repo_rel(f)}, {real[0]}x{real[1]} px, shown at "
                                                    f"{scale}x. Script: {found}."})
            content.append({"type": "image", "source": {"type": "base64", "media_type": "image/png", "data": data}})
            img_tokens += w * h // 750
        cid = f"art-{c // ART_CHUNK + 1:02d}"
        reqs.append((cid, {"check": "art", "images": meta, "image_tokens": img_tokens},
                     params(model, effort, MAX_OUT["art"], system, content, ART_SCHEMA)))
    return reqs


def parse_checks(spec):
    which = list(CHECKS) if spec == "all" else [c.strip() for c in spec.split(",") if c.strip()]
    bad = [c for c in which if c not in CHECKS]
    if bad or not which:
        print(f"--check: unknown {', '.join(bad) or 'nothing'}; pick from all,{','.join(CHECKS)}", file=sys.stderr)
        sys.exit(2)
    return which


def changes_per_check(since, checks):
    """check -> the paths changed since its base (None = check everything)."""
    if since is None:
        return {c: None for c in checks}
    if since != "last":
        changed = changed_since(since)
        return {c: changed for c in checks}
    bases, diffs, out = prior_bases(), {}, {}
    for c in checks:
        ref = bases.get(c)
        if ref is None:
            print(f"--since last: {c} has no earlier full run, so all of it is checked this time.")
            out[c] = None
            continue
        if ref not in diffs:
            diffs[ref] = changed_since(ref)
        out[c] = diffs[ref]
    return out


def build(args):
    model = MODELS[args.model]
    checks = parse_checks(args.check)
    if "art" in checks and args.check == "all" and not (args.art or args.since):
        # "all" without --since or --art leaves art out: the default set is the art in use, about a hundred images.
        checks.remove("art")
    ch = changes_per_check(args.since, checks)
    reqs = []
    if "text" in checks:
        reqs += text_requests(model, args.effort, ch["text"])
    if "scope" in checks:
        which = DEFAULT_SCOPE
        if args.scope_files:
            which = sorted({n.strip().zfill(2) for n in args.scope_files.split(",") if n.strip()})
            bad = [n for n in which if n not in SCOPE_PARTS]
            if bad or not which:
                print(f"--scope-files: no audit for {', '.join(bad) or 'nothing'}; "
                      f"pick from {','.join(DEFAULT_SCOPE)}", file=sys.stderr)
                sys.exit(2)
        reqs += scope_requests(model, args.effort, which, ch["scope"])
    if "design" in checks:
        reqs += design_requests(model, args.effort, ch["design"])
    if "docs" in checks:
        reqs += docs_requests(model, args.effort, ch["docs"])
    if "art" in checks:
        reqs += art_requests(model, args.effort, art_files(args.art, ch["art"]))
    # The checks this run covers in full, whose base may move to HEAD once every request of theirs is answered.
    narrowed = {"scope": bool(args.scope_files), "art": bool(args.art)}
    covered = [c for c in checks if not narrowed.get(c) and args.since in (None, "last")]
    built = [c for c in CHECKS if any(meta["check"] == c for _, meta, _ in reqs)]
    return model, built, reqs, covered


# ---------------------------------------------------------------- cost

def rate(model, prompt_tokens):
    for limit, i, o in PRICES[model]:
        if limit is None or prompt_tokens <= limit:
            return i, o


def block_chars(blocks):
    if isinstance(blocks, str):
        return len(blocks)
    return sum(len(b.get("text", "")) for b in blocks)


def est_tokens(meta, p):
    """About 3.5 characters a token for code and text, and width x height / 750 for an image; `plan --exact`
    counts instead."""
    chars = block_chars(p["system"]) + block_chars(p["messages"][0]["content"])
    return int(chars / 3.5) + meta.get("image_tokens", 0)


def cached_tokens(p):
    if isinstance(p["system"], str):
        return 0
    return int(sum(len(b["text"]) for b in p["system"] if "cache_control" in b) / 3.5)


def cost(model, inp, out, cache_write=0, cache_read=0):
    i, o = rate(model, inp + cache_write + cache_read)
    return BATCH * (inp * i + cache_write * i * 1.25 + cache_read * i * 0.1 + out * o) / 1e6


def estimate(model, reqs, counted=None):
    """Rows (id, input tokens, guessed output, $), total input, the estimate in $, and the ceiling in $: every
    cached block written and never read, every request writing its whole max_tokens."""
    rows, total_in, total, ceiling = [], 0, 0.0, 0.0
    for cid, meta, p in reqs:
        n = counted[cid] if counted else est_tokens(meta, p)
        out = GUESS_OUT[meta["check"]]
        c_ = cost(model, n, out)
        cw = min(cached_tokens(p), n)
        ceiling += cost(model, n - cw, p["max_tokens"], cache_write=cw)
        rows.append((cid, n, out, c_)); total_in += n; total += c_
    return rows, total_in, total, ceiling


# ---------------------------------------------------------------- the API

def client():
    try:
        import anthropic
    except ImportError:
        print("The Anthropic SDK is missing: python3 -m pip install anthropic", file=sys.stderr)
        sys.exit(1)
    key = os.environ.get("RW_BATCH_API_KEY") or os.environ.get("ANTHROPIC_API_KEY")
    if not key:
        if not sys.stdin.isatty():
            print("RW_BATCH_API_KEY is not set (README.md beside this script says how). Nothing was sent.",
                  file=sys.stderr)
            sys.exit(3)
        key = getpass.getpass("API key (hidden, not stored): ").strip()
    return anthropic, anthropic.Anthropic(api_key=key)


def cmd_plan(args):
    model, checks, reqs, covered = build(args)
    if not reqs:
        print("Nothing to check: no request was built (nothing changed for these checks).")
        return
    OUT.mkdir(parents=True, exist_ok=True)
    counted = None
    if args.exact:
        anthropic, c = client()
        counted = {}
        for cid, _, p in reqs:
            try:
                counted[cid] = c.messages.count_tokens(model=p["model"], system=p["system"],
                                                       messages=p["messages"],
                                                       output_config=p["output_config"]).input_tokens
            except anthropic.APIError as e:
                print(f"Token count failed on {cid}: {e}", file=sys.stderr)
                sys.exit(1)
    rows, total_in, total, ceiling = estimate(model, reqs, counted)
    print(f"{'request':<16} {'input tok':>10} {'out (guess)':>11} {'batch $':>9}")
    for cid, n, out, c_ in rows:
        print(f"{cid:<16} {n:>10,} {out:>11,} {c_:>9.3f}")
    print(f"{len(reqs)} requests ({', '.join(checks)}), {total_in:,} input tokens "
          f"({'counted' if counted else 'estimated'}), {model}: about ${total:.2f} at batch prices; at most "
          f"${ceiling:.2f} (every request at its max_tokens, every cache block written), the figure --max-cost "
          f"checks.")
    path = OUT / f"plan_{args.model}.jsonl"
    with path.open("w", encoding="utf-8") as f:
        for cid, _, p in reqs:
            f.write(json.dumps({"custom_id": cid, "params": p}) + "\n")
    print(f"Requests written to {path} (nothing sent).")


def cmd_submit(args):
    model, checks, reqs, covered = build(args)
    if not reqs:
        print("Nothing to check: no request was built (nothing changed for these checks). Nothing was sent.")
        return
    _, _, total, ceiling = estimate(model, reqs)
    if ceiling > args.max_cost:
        print(f"About ${total:.2f}, at most ${ceiling:.2f}: the most is over --max-cost ${args.max_cost:.2f}. Nothing "
              f"was sent; narrow it (--check, --scope-files, --since, --model haiku) or raise --max-cost.",
              file=sys.stderr)
        sys.exit(4)
    anthropic, c = client()
    try:
        batch = c.messages.batches.create(requests=[{"custom_id": cid, "params": p} for cid, _, p in reqs])
    except anthropic.AuthenticationError:
        print("The API key was refused (401). Check it on the Console's API keys page.", file=sys.stderr)
        sys.exit(1)
    except anthropic.APIStatusError as e:
        print(f"The batch was refused: {e.status_code} {e.message}", file=sys.stderr)
        sys.exit(1)
    except anthropic.APIConnectionError as e:
        print(f"Could not reach the API: {e}", file=sys.stderr)
        sys.exit(1)
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / f"{batch.id}.json").write_text(json.dumps({
        "model": model, "effort": args.effort, "checks": checks, "base": base_commit(),
        "head": base_commit(short=False), "covered": covered, "prev_bases": prior_bases(),
        "since": args.since, "estimate": round(total, 2), "ceiling": round(ceiling, 2),
        "submitted": datetime.datetime.now(datetime.timezone.utc).isoformat(timespec="seconds"),
        "requests": {cid: meta for cid, meta, _ in reqs}}), encoding="utf-8")
    print(f"Batch {batch.id}: {len(reqs)} requests ({', '.join(checks)}), {model}, effort {args.effort}, "
          f"about ${total:.2f} (at most ${ceiling:.2f}). Record: {OUT / (batch.id + '.json')}")
    if args.no_wait:
        print(f"Collect it later, on a machine that has the record: "
              f"python3 TDS_Mod/Tools/batch/run_checks.py collect {batch.id}")
        return
    collect(c, batch.id, args.max_wait)


def collect(c, batch_id, max_wait=0):
    meta_path = OUT / f"{batch_id}.json"
    if not meta_path.exists():
        print(f"No record of {batch_id} in {OUT} (submit writes it; set RW_BATCH_OUT to the folder it used).",
              file=sys.stderr)
        sys.exit(1)
    meta = json.loads(meta_path.read_text(encoding="utf-8"))
    start = time.monotonic()
    try:
        while True:
            b = c.messages.batches.retrieve(batch_id)
            rc = b.request_counts
            print(f"{datetime.datetime.now():%H:%M} {b.processing_status}: {rc.processing} running, "
                  f"{rc.succeeded} done, {rc.errored} errored", flush=True)
            if b.processing_status == "ended":
                break
            if max_wait and time.monotonic() - start > max_wait * 60:
                print(f"Stopped waiting after {max_wait} minutes; the batch keeps running. Collect it later: "
                      f"python3 TDS_Mod/Tools/batch/run_checks.py collect {batch_id}")
                return
            time.sleep(60)
    except KeyboardInterrupt:
        print(f"\nStopped waiting; the batch keeps running. Collect it later: "
              f"python3 TDS_Mod/Tools/batch/run_checks.py collect {batch_id}")
        sys.exit(1)
    results = {}
    for r in c.messages.batches.results(batch_id):
        results[r.custom_id] = r.result
    write_report(batch_id, meta, results)


def cmd_collect(args):
    _, c = client()
    collect(c, args.batch_id, args.max_wait)


def report_costs(month):
    rows = []
    for p in sorted(REPORTS.glob(f"BATCH_CHECKS_{month}-*.md")):
        m = re.search(r"Cost: \$([0-9]+\.[0-9]+)", p.read_text(encoding="utf-8"))
        if m:
            rows.append((p.name, float(m.group(1))))
    return rows


def cmd_spend(args):
    month = args.month or datetime.date.today().strftime("%Y-%m")
    rows = report_costs(month)
    for name, c_ in rows:
        print(f"{name:<34} ${c_:.2f}")
    total = sum(c_ for _, c_ in rows)
    print(f"{len(rows)} batch report{'' if len(rows) == 1 else 's'} in {month}: ${total:.2f} of the "
          f"${MONTHLY_CREDIT:.0f} monthly API credit. Other use of the key is not counted; the Console's Billing "
          f"page has the real figure.")


# ---------------------------------------------------------------- the report

def cell(s):
    return str(s).replace("|", "\\|").replace("\n", " ").strip()


def row_table(rows, name=None):
    """A table of audit rows; with name (a dict row -> file), a File column first."""
    head = "| File " if name else ""
    L = [head + "| Lines | Says | Code | Mark | Note |", ("| --- " if name else "") + "| --- | --- | --- | --- | --- |"]
    L += [(f"| {name[id(r)]} " if name else "")
          + f"| {cell(r['lines'])} | {cell(r['says'])} | `{cell(r['code'])}` | {r['mark']} | {cell(r['note'])} |"
          for r in rows]
    return L


def mark_section(L, n, title, recipe, results, marks, work_marks):
    L += [f"## {n}. {title}", "", recipe, "", "| File | " + " | ".join(marks) + " |",
          "| --- |" + " --- |" * len(marks)]
    for name, rows in sorted(results.items()):
        L.append(f"| {name} | " + " | ".join(str(sum(r["mark"] == m for r in rows)) for m in marks) + " |")
    work = [(f, r) for f, rows in sorted(results.items()) for r in rows if r["mark"] in work_marks]
    L += ["", f"### {n}.1 {' and '.join(work_marks)} (to confirm by hand)", ""]
    L += row_table([r for _, r in work], {id(r): f for f, r in work}) if work else ["None."]
    L.append("")
    for k, (name, rows) in enumerate(sorted(results.items()), 2):
        L += [f"### {n}.{k} {name}", ""] + row_table(rows) + [""]


def write_report(batch_id, meta, results):
    model, reqs = meta["model"], meta["requests"]
    checks = meta.get("checks") or sorted({i["check"] for i in reqs.values()})
    texts, scopes, designs, docs, arts, missing, runs, spent = [], {}, {}, [], [], None, [], 0.0
    for cid in sorted(reqs):
        info, res = reqs[cid], results.get(cid)
        if info["check"] == "docs":
            missing = info.get("missing", [])
        status, data = (res.type if res else "missing"), None
        if res and res.type == "succeeded":
            msg = res.message
            u = msg.usage
            c_ = cost(model, u.input_tokens, u.output_tokens,
                      u.cache_creation_input_tokens or 0, u.cache_read_input_tokens or 0)
            spent += c_
            status = msg.stop_reason
            if msg.stop_reason in ("end_turn", "stop_sequence"):
                text = next((b.text for b in msg.content if b.type == "text"), "")
                try:
                    data = json.loads(text)
                    status = "ok"
                except json.JSONDecodeError:
                    status = "unreadable answer"
            runs.append((cid, status, u.input_tokens + (u.cache_creation_input_tokens or 0)
                         + (u.cache_read_input_tokens or 0), u.output_tokens, c_))
        else:
            if res and res.type == "errored":
                err = getattr(res.error, "error", res.error)
                status = f"errored: {getattr(err, 'type', '?')} {cell(getattr(err, 'message', ''))}".strip()
            runs.append((cid, status, 0, 0, 0.0))
        if data is None:
            continue
        if info["check"] == "text":
            for f in data["findings"]:
                s = info["strings"].get(f["id"])
                if s:
                    texts.append((s["where"], s["text"], f))
        elif info["check"] == "scope":
            scopes[info["file"]] = data["rows"]
        elif info["check"] == "design":
            designs[info["file"]] = data["rows"]
        elif info["check"] == "docs":
            docs += data["findings"]
        elif info["check"] == "art":
            for f in data["findings"]:
                img = info["images"].get(f["id"])
                if img:
                    arts.append((img, f))

    day = datetime.date.today().isoformat()
    for suffix in [""] + list("bcdefghijklmnopqrstuvwxyz"):
        path = REPORTS / f"BATCH_CHECKS_{day}{suffix}.md"
        if not path.exists():
            break
    else:
        print(f"Every BATCH_CHECKS_{day}*.md name is taken; results stay in the batch for 29 days.", file=sys.stderr)
        sys.exit(1)
    since = f", changes since `{meta['since']}`" if meta.get("since") else ""
    L = [f"# Batch checks ({day})", "",
         f"Machine-read by `{model}` (effort {meta['effort']}) through the Message Batches API, batch `{batch_id}`, "
         f"on base `{meta['base']}`{since}, checks {', '.join(checks)}, by `TDS_Mod/Tools/batch/run_checks.py`. "
         f"Cost: ${spent:.2f} at batch prices. Every row is a lead to confirm by hand before it changes code, docs, "
         "art or the scope: the model reads, it does not run the game. Nothing ran on the PC.", ""]
    n = 1
    if "text" in checks and any(i["check"] == "text" for i in reqs.values()):
        L += [f"## {n}. Text", "",
              f"{len(texts)} finding{'' if len(texts) == 1 else 's'}. Recipe: `docs/reports/TEXT_AUDIT_2026-11-05.md` §1.", ""]
        if texts:
            L += ["| Where | Text | Kind | Problem | Suggestion |", "| --- | --- | --- | --- | --- |"]
            for where, s, f in texts:
                more = f" (+{len(where) - 1})" if len(where) > 1 else ""
                L.append(f"| `{where[0]}`{more} | {cell(s)} | {f['kind']} | {cell(f['problem'])} | "
                         f"{cell(f['suggestion'])} |")
            L.append("")
        n += 1
    if scopes:
        mark_section(L, n, "Scope against code",
                     "Recipe: `docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md` (marks as there). Code paths are relative "
                     "to `TDS_Mod/ModSource`.", scopes, SCOPE_MARKS, ["DIFFERENT", "MISSING"])
        n += 1
    if designs:
        mark_section(L, n, "Design pages against code",
                     "Each vessel's `DESIGN.md` (lines as D:N), every number and rule mapped to its code. The scope "
                     "wins over a design page: a DIFFERENT row is fixed in whichever of the two the scope agrees with "
                     "least.", designs, DESIGN_MARKS, ["DIFFERENT", "NOT-IN-CODE"])
        n += 1
    if missing is not None:
        L += [f"## {n}. Docs", "",
              f"{len(docs)} finding{'' if len(docs) == 1 else 's'} from reading `CLAUDE.md`, PLAN Part C \"Now\", "
              "the reports' README, the batch README and `docs/skills/` against each other.", ""]
        if docs:
            L += ["| Where | Kind | Problem | Fix |", "| --- | --- | --- | --- |"]
            L += [f"| `{cell(f['id'])}` | {f['kind']} | {cell(f['problem'])} | {cell(f['suggestion'])} |" for f in docs]
            L.append("")
        L += [f"Cited paths the free local pass could not find: {len(missing)}.", ""]
        L += [f"- {m}" for m in missing]
        L.append("")
        n += 1
    if any(i["check"] == "art" for i in reqs.values()):
        imgs = sum(len(i["images"]) for i in reqs.values() if i["check"] == "art")
        L += [f"## {n}. Art", "",
              f"{len(arts)} finding{'' if len(arts) == 1 else 's'} on {imgs} image{'' if imgs == 1 else 's'}, read "
              "against `docs/STYLE.md` (the art review's step 1 by machine; step 2 is the script's column). The "
              "contact sheets for his eye still come from the art review.", ""]
        if arts:
            L += ["| File | Kind | Problem | Suggestion | Script |", "| --- | --- | --- | --- | --- |"]
            L += [f"| `{img['path']}` | {f['kind']} | {cell(f['problem'])} | {cell(f['suggestion'])} | "
                  f"{cell(img['script'])} |" for img, f in arts]
            L.append("")
        n += 1
    L += [f"## {n}. Requests", "", "| Request | Result | Input tokens | Output tokens | $ |", "| --- | --- | --- | --- | --- |"]
    L += [f"| {cid} | {st} | {i:,} | {o:,} | {c_:.3f} |" for cid, st, i, o, c_ in runs]
    L += ["", "A result other than `ok`: `max_tokens` (answer cut, rerun that request with a lower effort), `refusal`, "
          "`unreadable answer` or `errored` (rerun it alone with `--check` and `--scope-files`)."]
    month = day[:7]
    total = sum(c_ for _, c_ in report_costs(month)) + spent
    L += ["", f"Spent on batch checks in {month}, this run included: ${total:.2f} of the ${MONTHLY_CREDIT:.0f} "
          "monthly API credit."]
    bases = dict(meta.get("prev_bases") or {})
    for c in meta.get("covered", []):
        if all(st == "ok" for cid, st, *_ in runs if reqs[cid]["check"] == c):
            bases[c] = meta.get("head") or meta["base"]
    L += ["", f"<!-- batch-bases: {json.dumps(bases, sort_keys=True)} -->"]
    path.write_text("\n".join(L) + "\n", encoding="utf-8")
    print(f"Report: {os.path.relpath(path, ROOT)}  (${spent:.2f}; ${total:.2f} this month)")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    for name in ("plan", "submit"):
        p = sub.add_parser(name)
        p.add_argument("--check", default="all",
                       help=f"all, or a comma list of {','.join(CHECKS)} (all leaves art out unless --since or --art)")
        p.add_argument("--model", choices=sorted(MODELS), default="sonnet")
        p.add_argument("--effort", choices=["low", "medium", "high"], default="medium")
        p.add_argument("--scope-files", help="comma list of scope numbers, e.g. 07,11 (default: 01,03-14,16)")
        p.add_argument("--since", metavar="REF",
                       help="only what changed between REF and HEAD; 'last' = each check since its last full run")
        p.add_argument("--art", nargs="+", metavar="PATH", help="PNG files or folders for the art check")
        if name == "plan":
            p.add_argument("--exact", action="store_true", help="count tokens with the free endpoint (needs the key)")
        else:
            p.add_argument("--max-cost", type=float, default=10.0,
                           help="refuse to send when the most the run can cost (every request at its max_tokens, "
                                "every cache block written) is over this many $ (10)")
            p.add_argument("--no-wait", action="store_true", help="submit and return; run collect later")
            p.add_argument("--max-wait", type=int, default=0, metavar="MIN",
                           help="stop waiting after MIN minutes (0 = until the batch ends)")
    p = sub.add_parser("collect")
    p.add_argument("batch_id")
    p.add_argument("--max-wait", type=int, default=0, metavar="MIN")
    p = sub.add_parser("spend")
    p.add_argument("--month", metavar="YYYY-MM")
    args = ap.parse_args()
    {"plan": cmd_plan, "submit": cmd_submit, "collect": cmd_collect, "spend": cmd_spend}[args.cmd](args)


if __name__ == "__main__":
    main()
