#!/usr/bin/env python3
"""Run Rogue Waves' mechanical checks through the Message Batches API (half the normal price, results within 24 h).

Checks:
  text   every player-facing string of the six parts, read for spelling, grammar, the naming rule and stale facts
         (the recipe of docs/reports/TEXT_AUDIT_2026-11-05.md section 1)
  scope  one scope file at a time, every line mapped to code file:line with a mark: MATCH, DIFFERENT, MISSING,
         UNBUILT-QUEUED or UNBUILT-BY-DESIGN (the recipe of docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md)

From the repository root (README.md beside this file has the key, the steps and the costs):
  python3 TDS_Mod/Tools/batch/run_checks.py plan    [--check text|scope|all] [--model sonnet|haiku] [--exact]
  python3 TDS_Mod/Tools/batch/run_checks.py submit  [--check ...] [--model ...] [--effort ...] [--no-wait]
  python3 TDS_Mod/Tools/batch/run_checks.py collect BATCH_ID

plan sends nothing and costs nothing (with --exact it calls the free token-count endpoint, which needs the key).
submit sends one batch, waits for it and writes docs/reports/BATCH_CHECKS_<date>.md; collect does the second half
for a batch submitted with --no-wait or interrupted. The key is read from ANTHROPIC_API_KEY, or asked for once
(hidden, never stored) when the variable is unset. Only the mod's own code, the scope and PLAN Part C are sent.
"""
import argparse, datetime, getpass, json, os, re, subprocess, sys, time
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
SRC = ROOT / "TDS_Mod" / "ModSource"
OUT = Path(__file__).resolve().parent / "out"

MODELS = {"sonnet": "claude-sonnet-5-5", "haiku": "claude-haiku-5-5"}
# Standard $ per million tokens (2026-11-05); a batch costs half. Haiku 5.5 charges the higher row for a prompt over
# 100K tokens. Cache writes cost 1.25x input and reads 0.1x; inside a batch, cache hits are best effort.
PRICES = {
    "claude-sonnet-5-5": [(None, 2.00, 10.00)],
    "claude-haiku-5-5": [(100_000, 0.10, 0.50), (None, 0.50, 2.50)],
}
BATCH = 0.5

PARTS = ["Yossele", "Spool", "Argo", "Uloe", "RogueWavesMode", "RogueWavesFoes"]
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
TEXT_CHUNK = 250          # strings per text request
GUESS_OUT = {"text": 3_000, "scope": 14_000}   # output tokens per request, thinking included: a guess for `plan`


# ---------------------------------------------------------------- reading the repository

def game_files(part):
    """A part's game code: Code/NN_*.cs with NN below 90 (90-99 are self-tests, reviews, shots and play builds).
    Shared is the live kit, every file."""
    if part == "Shared":
        return sorted((SRC / "Shared").glob("*.cs"))
    return [f for f in sorted((SRC / part / "Code").glob("*.cs")) if f.name[:2].isdigit() and int(f.name[:2]) < 90]


def rel(f):
    return f.relative_to(SRC).as_posix()


def numbered(f):
    lines = f.read_text(encoding="utf-8-sig").split("\n")
    return f"=== {rel(f)} ===\n" + "\n".join(f"{i}\t{l.rstrip()}" for i, l in enumerate(lines, 1))


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


def player_strings():
    """Strings of two or more words from the six parts' game code, each text once with every place it appears."""
    seen = {}
    for part in PARTS:
        for f in game_files(part):
            for line, s in cs_strings(f.read_text(encoding="utf-8-sig")):
                if re.search(r"[A-Za-z]{2,}[^\S\n]+[A-Za-z]{2,}", s) and "/" not in s.split(" ")[0]:
                    seen.setdefault(s, []).append(f"{rel(f)}:{line}")
    return seen


def plan_now():
    text = (ROOT / "docs" / "PLAN.md").read_text(encoding="utf-8")
    m = re.search(r"^### Now\n(.*?)^### Log", text, re.S | re.M)
    return m.group(1).strip() if m else ""


def base_commit():
    try:
        return subprocess.run(["git", "-C", str(ROOT), "rev-parse", "--short", "HEAD"],
                              capture_output=True, text=True).stdout.strip()
    except OSError:
        return "?"


# ---------------------------------------------------------------- the requests

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

TEXT_SCHEMA = {
    "type": "object",
    "properties": {"findings": {"type": "array", "items": {
        "type": "object",
        "properties": {
            "id": {"type": "string"},
            "kind": {"type": "string", "enum": ["spelling", "grammar", "naming", "stale", "unclear"]},
            "problem": {"type": "string"},
            "suggestion": {"type": "string"},
        },
        "required": ["id", "kind", "problem", "suggestion"], "additionalProperties": False}}},
    "required": ["findings"], "additionalProperties": False,
}

SCOPE_SYSTEM = """You audit Rogue Waves, a fan expansion pack for the game TDS - Tower Defense Strategy, against its \
scope. The scope file (each line numbered) says what to build; the code (each line numbered, file by file) is what is \
built. Go through the scope file from top to bottom and map every line that states a behaviour, a number, a name or \
a text to the code that does it. Group neighbouring lines that one code location covers. Skip headings, comments \
(lines starting with #) and blank lines.

Marks:
- MATCH: the code does what the line says, at the location you give
- DIFFERENT: built, and the code does something else (a number, a rule, a name or a text differs): say what differs
- MISSING: the scope asks for it, nothing in the code builds it, and nothing in the plan queues it
- UNBUILT-QUEUED: not built, and the plan below queues it: name the queue item
- UNBUILT-BY-DESIGN: not built, and the scope itself says so (on hold, a ! NEEDS ! slot, "later")

Scope symbols: | Title |, [System], {Thing}, (Kind), % Level:{Upgrade}; a value between tildes ~like this~ is a \
default Claude picked, which still counts. Code locations are file:line or file:first-last, with the file as \
given in the === headers. Be exact: a MATCH needs the line that holds the number or the rule, and a DIFFERENT or \
MISSING row must say what you looked for. Report every scope line with content; do not stop early."""

SCOPE_SCHEMA = {
    "type": "object",
    "properties": {"rows": {"type": "array", "items": {
        "type": "object",
        "properties": {
            "lines": {"type": "string"},
            "says": {"type": "string"},
            "code": {"type": "string"},
            "mark": {"type": "string",
                     "enum": ["MATCH", "DIFFERENT", "MISSING", "UNBUILT-QUEUED", "UNBUILT-BY-DESIGN"]},
            "note": {"type": "string"},
        },
        "required": ["lines", "says", "code", "mark", "note"], "additionalProperties": False}}},
    "required": ["rows"], "additionalProperties": False,
}


def params(model, effort, max_tokens, system, user, schema):
    return {
        "model": model, "max_tokens": max_tokens, "system": system,
        "messages": [{"role": "user", "content": user}],
        "output_config": {"effort": effort, "format": {"type": "json_schema", "schema": schema}},
    }


def text_requests(model, effort):
    strings = player_strings()
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
                     params(model, effort, 16_000, TEXT_SYSTEM, user, TEXT_SCHEMA)))
    return reqs


def scope_requests(model, effort, which):
    now = plan_now()
    reqs = []
    for num in which:
        path = next((ROOT / "docs" / "scope").glob(f"{num}_*.txt"))
        system, files = [{"type": "text", "text": SCOPE_SYSTEM}], []
        for part in SCOPE_PARTS[num]:      # one cached block per part, so requests share the mode's block
            part_files = game_files(part)
            files += part_files
            system.append({"type": "text", "cache_control": {"type": "ephemeral"},
                           "text": f"The code of {part} (game code only; self-tests left out):\n\n"
                                   + "\n\n".join(numbered(f) for f in part_files)})
        scope = path.read_text(encoding="utf-8-sig").split("\n")
        user = (f"The plan's current state (what is queued, what is done), docs/PLAN.md Part C:\n\n{now}\n\n")
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
                     params(model, effort, 64_000, system, user, SCOPE_SCHEMA)))
    return reqs


def build(args):
    model = MODELS[args.model]
    reqs = []
    if args.check in ("text", "all"):
        reqs += text_requests(model, args.effort)
    if args.check in ("scope", "all"):
        which = DEFAULT_SCOPE
        if args.scope_files:
            which = sorted({n.strip().zfill(2) for n in args.scope_files.split(",") if n.strip()})
            bad = [n for n in which if n not in SCOPE_PARTS]
            if bad or not which:
                sys.exit(f"--scope-files: no audit for {', '.join(bad) or 'nothing'}; "
                         f"pick from {','.join(DEFAULT_SCOPE)}")
        reqs += scope_requests(model, args.effort, which)
    return model, reqs


# ---------------------------------------------------------------- cost

def rate(model, prompt_tokens):
    for limit, i, o in PRICES[model]:
        if limit is None or prompt_tokens <= limit:
            return i, o


def est_tokens(p):
    """About 3.5 characters a token for code and scope text; `plan --exact` counts instead."""
    sys_text = p["system"] if isinstance(p["system"], str) else "".join(b["text"] for b in p["system"])
    return int((len(sys_text) + len(p["messages"][0]["content"])) / 3.5)


def cached_tokens(p):
    if isinstance(p["system"], str):
        return 0
    return int(sum(len(b["text"]) for b in p["system"] if "cache_control" in b) / 3.5)


def cost(model, inp, out, cache_write=0, cache_read=0):
    i, o = rate(model, inp + cache_write + cache_read)
    return BATCH * (inp * i + cache_write * i * 1.25 + cache_read * i * 0.1 + out * o) / 1e6


# ---------------------------------------------------------------- the API

def client():
    try:
        import anthropic
    except ImportError:
        sys.exit("The Anthropic SDK is missing: python -m pip install anthropic")
    key = os.environ.get("ANTHROPIC_API_KEY")
    if not key:
        if not sys.stdin.isatty():
            sys.exit("ANTHROPIC_API_KEY is not set (README.md beside this script says how).")
        key = getpass.getpass("API key (hidden, not stored): ").strip()
    return anthropic, anthropic.Anthropic(api_key=key)


def cmd_plan(args):
    model, reqs = build(args)
    OUT.mkdir(exist_ok=True)
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
                sys.exit(f"Token count failed on {cid}: {e}")
    total_in = total_cost = extra = 0
    print(f"{'request':<12} {'input tok':>10} {'out (guess)':>11} {'batch $':>9}")
    for cid, meta, p in reqs:
        n = counted[cid] if counted else est_tokens(p)
        out = GUESS_OUT[meta["check"]]
        c_ = cost(model, n, out)
        total_in += n; total_cost += c_
        cw = cached_tokens(p)        # worst case: every cached block is written, never read
        extra += cost(model, n - cw, 0, cache_write=cw) - cost(model, n, 0)
        print(f"{cid:<12} {n:>10,} {out:>11,} {c_:>9.3f}")
    print(f"{len(reqs)} requests, {total_in:,} input tokens ({'counted' if counted else 'estimated'}), "
          f"{model}: about ${total_cost:.2f} at batch prices (cache writes can add up to "
          f"${extra:.2f}; cache reads lower it).")
    path = OUT / f"plan_{args.check}_{args.model}.jsonl"
    with path.open("w", encoding="utf-8") as f:
        for cid, _, p in reqs:
            f.write(json.dumps({"custom_id": cid, "params": p}) + "\n")
    print(f"Requests written to {path.relative_to(ROOT)} (nothing sent).")


def cmd_submit(args):
    model, reqs = build(args)
    anthropic, c = client()
    try:
        batch = c.messages.batches.create(requests=[{"custom_id": cid, "params": p} for cid, _, p in reqs])
    except anthropic.AuthenticationError:
        sys.exit("The API key was refused (401). Check it on the Console's API keys page.")
    except anthropic.APIStatusError as e:
        sys.exit(f"The batch was refused: {e.status_code} {e.message}")
    except anthropic.APIConnectionError as e:
        sys.exit(f"Could not reach the API: {e}")
    OUT.mkdir(exist_ok=True)
    (OUT / f"{batch.id}.json").write_text(json.dumps({
        "model": model, "effort": args.effort, "check": args.check, "base": base_commit(),
        "submitted": datetime.datetime.now(datetime.timezone.utc).isoformat(timespec="seconds"),
        "requests": {cid: meta for cid, meta, _ in reqs}}), encoding="utf-8")
    print(f"Batch {batch.id}: {len(reqs)} requests, {model}, effort {args.effort}.")
    if args.no_wait:
        print(f"Collect it later: python3 TDS_Mod/Tools/batch/run_checks.py collect {batch.id}")
        return
    collect(c, anthropic, batch.id)


def collect(c, anthropic, batch_id):
    meta_path = OUT / f"{batch_id}.json"
    if not meta_path.exists():
        sys.exit(f"No record of {batch_id} in {OUT.relative_to(ROOT)} (it is written by submit on this machine).")
    meta = json.loads(meta_path.read_text(encoding="utf-8"))
    try:
        while True:
            b = c.messages.batches.retrieve(batch_id)
            rc = b.request_counts
            print(f"{datetime.datetime.now():%H:%M} {b.processing_status}: {rc.processing} running, "
                  f"{rc.succeeded} done, {rc.errored} errored", flush=True)
            if b.processing_status == "ended":
                break
            time.sleep(60)
    except KeyboardInterrupt:
        sys.exit(f"\nStopped waiting; the batch keeps running. Collect it later: "
                 f"python3 TDS_Mod/Tools/batch/run_checks.py collect {batch_id}")
    results = {}
    for r in c.messages.batches.results(batch_id):
        results[r.custom_id] = r.result
    write_report(batch_id, meta, results)


def cmd_collect(args):
    anthropic, c = client()
    collect(c, anthropic, args.batch_id)


# ---------------------------------------------------------------- the report

def cell(s):
    return str(s).replace("|", "\\|").replace("\n", " ").strip()


def write_report(batch_id, meta, results):
    model, reqs = meta["model"], meta["requests"]
    texts, scopes, runs, spent = [], {}, [], 0.0
    for cid in sorted(reqs):
        info, res = reqs[cid], results.get(cid)
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
        else:
            scopes[info["file"]] = data["rows"]

    day = datetime.date.today().isoformat()
    for suffix in [""] + list("bcdefghijklmnopqrstuvwxyz"):
        path = ROOT / "docs" / "reports" / f"BATCH_CHECKS_{day}{suffix}.md"
        if not path.exists():
            break
    else:
        sys.exit(f"Every BATCH_CHECKS_{day}*.md name is taken; results stay in the batch for 29 days.")
    L = [f"# Batch checks ({day})", "",
         f"Machine-read by `{model}` (effort {meta['effort']}) through the Message Batches API, batch `{batch_id}`, "
         f"on base `{meta['base']}`, by `TDS_Mod/Tools/batch/run_checks.py`. Cost: ${spent:.2f} at batch prices. "
         "Every row is a lead to confirm by hand before it changes code or the scope: the model reads, it does not "
         "run the game. Nothing ran on the PC.", ""]
    n = 1
    if any(i["check"] == "text" for i in reqs.values()):
        L += [f"## {n}. Text", "", f"{len(texts)} finding{'' if len(texts) == 1 else 's'}. Recipe: `docs/reports/TEXT_AUDIT_2026-11-05.md` §1.", ""]
        if texts:
            L += ["| Where | Text | Kind | Problem | Suggestion |", "| --- | --- | --- | --- | --- |"]
            for where, s, f in texts:
                more = f" (+{len(where) - 1})" if len(where) > 1 else ""
                L.append(f"| `{where[0]}`{more} | {cell(s)} | {f['kind']} | {cell(f['problem'])} | "
                         f"{cell(f['suggestion'])} |")
            L.append("")
        n += 1
    if scopes:
        marks = ["MATCH", "DIFFERENT", "MISSING", "UNBUILT-QUEUED", "UNBUILT-BY-DESIGN"]
        L += [f"## {n}. Scope against code", "",
              "Recipe: `docs/reports/SCOPE_CODE_AUDIT_2026-11-05.md` (marks as there). Code paths are relative to "
              "`TDS_Mod/ModSource`.", "", "| File | " + " | ".join(marks) + " |",
              "| --- |" + " --- |" * len(marks)]
        for name, rows in sorted(scopes.items()):
            L.append(f"| {name} | " + " | ".join(str(sum(r["mark"] == m for r in rows)) for m in marks) + " |")
        work = [(name, r) for name, rows in sorted(scopes.items()) for r in rows
                if r["mark"] in ("DIFFERENT", "MISSING")]
        L += ["", f"### {n}.1 DIFFERENT and MISSING (to confirm by hand)", ""]
        if work:
            L += ["| Lines | Says | Code | Mark | Note |", "| --- | --- | --- | --- | --- |"]
            L += [f"| {cell(r['lines'])} | {cell(r['says'])} | `{cell(r['code'])}` | {r['mark']} | {cell(r['note'])} |"
                  for _, r in work]
        else:
            L.append("None.")
        L.append("")
        for k, (name, rows) in enumerate(sorted(scopes.items()), 2):
            L += [f"### {n}.{k} {name}", "", "| Lines | Says | Code | Mark | Note |", "| --- | --- | --- | --- | --- |"]
            L += [f"| {cell(r['lines'])} | {cell(r['says'])} | `{cell(r['code'])}` | {r['mark']} | {cell(r['note'])} |"
                  for r in rows]
            L.append("")
        n += 1
    L += [f"## {n}. Requests", "", "| Request | Result | Input tokens | Output tokens | $ |", "| --- | --- | --- | --- | --- |"]
    L += [f"| {cid} | {st} | {i:,} | {o:,} | {c_:.3f} |" for cid, st, i, o, c_ in runs]
    L += ["", "A result other than `ok`: `max_tokens` (answer cut, rerun that file with a lower effort), `refusal`, "
          "`unreadable answer` or `errored` (rerun with `--scope-files`)."]
    path.write_text("\n".join(L) + "\n", encoding="utf-8")
    print(f"Report: {path.relative_to(ROOT)}  (${spent:.2f})")


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    sub = ap.add_subparsers(dest="cmd", required=True)
    for name in ("plan", "submit"):
        p = sub.add_parser(name)
        p.add_argument("--check", choices=["text", "scope", "all"], default="all")
        p.add_argument("--model", choices=sorted(MODELS), default="sonnet")
        p.add_argument("--effort", choices=["low", "medium", "high"], default="medium")
        p.add_argument("--scope-files", help="comma list of scope numbers, e.g. 07,11 (default: 01,03-14,16)")
        if name == "plan":
            p.add_argument("--exact", action="store_true", help="count tokens with the free endpoint (needs the key)")
        else:
            p.add_argument("--no-wait", action="store_true", help="submit and return; run collect later")
    p = sub.add_parser("collect")
    p.add_argument("batch_id")
    args = ap.parse_args()
    {"plan": cmd_plan, "submit": cmd_submit, "collect": cmd_collect}[args.cmd](args)


if __name__ == "__main__":
    main()
