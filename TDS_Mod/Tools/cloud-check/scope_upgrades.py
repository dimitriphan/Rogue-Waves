"""Map every `% N:{Name}` upgrade in a vessel scope to its U(...) entry in the part's 02_Data.cs.

Usage, from the repository root: python3 TDS_Mod/Tools/cloud-check/scope_upgrades.py docs/scope/07_Vessel_Yossele.txt TDS_Mod/ModSource/Yossele/Code/02_Data.cs
V=1 in the environment also prints each upgrade's scope Numbers line and code parameters, to read by hand.
Prints one row per scope upgrade: scope line, name, code file:line, and what differs (level, cost, in-game line).
"""
import os, re, sys

COL = {"Redaura": "Red", "Blueaura": "Blue", "Yellowaura": "Yellow", "Greenaura": "Green", "Thread": "Thread"}

def norm(s):
    s = s.replace("~", "").replace("{", "").replace("}", "").strip().rstrip(".").strip()
    s = re.sub(r"\s+", " ", s)
    return s.lower()

def scope_costs(s):
    s = s.split("/")[0].replace("~", "")
    out = {}
    for m in re.finditer(r"(Red|Blue|Yellow|Green|Thread)\s+(\d+)", s):
        out[m.group(1)] = int(m.group(2))
    return out

def code_costs(s):
    out = {}
    for part in s.split(";"):
        if "=" in part:
            k, v = part.split("=", 1)
            out[COL.get(k.strip(), k.strip())] = int(float(v))
    return out

def read_scope(path):
    lines = open(path, encoding="utf-8").read().split("\n")
    ups = []
    cur = None
    building = ""
    for i, l in enumerate(lines, 1):
        b = re.match(r"\s*\([^)]*\):\{([^}]+)\}", l)
        if b:
            building = b.group(1)
        m = re.match(r"\s*% (\d):\{([^}]+)\}", l)
        if m:
            cur = {"line": i, "level": int(m.group(1)), "name": m.group(2).replace("~", ""), "building": building, "ingame": None, "cost": None}
            ups.append(cur)
            continue
        if cur is None:
            continue
        if re.match(r"\s*% |\s*\(|\s*\[", l):
            cur = None
            continue
        g = re.match(r"\s*- In-game line:\s*(.*)", l)
        if g:
            cur["ingame"] = (i, g.group(1).split(" / ")[0].split(" /~")[0])
        n = re.match(r"\s*- Numbers:\s*(.*)", l)
        if n:
            cur["numbers"] = n.group(1)
        lt = re.match(r"\s*- Leads to:\s*(.*)", l)
        if lt:
            t = lt.group(1).split(" /")[0]
            cur["leads"] = [x.replace("~","") for x in re.findall(r"\{([^}]+)\}", t)] if "nothing" not in t else []
        c = re.match(r"\s*- Cost:\s*(.*)", l)
        if c:
            cur["cost"] = (i, c.group(1))
    return ups

def read_code(paths):
    found = {}
    pat = re.compile(r'U\(\s*(\w+)\s*,\s*"(\w+)"\s*,\s*(\d+)\s*,\s*"([^"]*)"\s*,\s*"([^"]*)"\s*,\s*"([^"]*)"\s*,\s*"([^"]*)"')
    for p in paths:
        for i, l in enumerate(open(p, encoding="utf-8").read().split("\n"), 1):
            m = pat.search(l)
            if m:
                found.setdefault(norm(m.group(4)), []).append({"file": p, "line": i, "tier": int(m.group(3)), "display": m.group(4), "desc": m.group(5), "costs": m.group(7), "pars": m.group(6), "key": m.group(2), "next": l[m.end():].split('"')[1] if l[m.end():].count('"')>=2 else ""})
    return found

VERBOSE=os.environ.get('V')=='1'
def main():
    scope, codes = sys.argv[1], sys.argv[2:]
    ups = read_scope(scope)
    code = read_code(codes)
    short = scope.split("/")[-1][:2]
    for u in ups:
        c = code.get(norm(u["name"]))
        if not c:
            print(f"{short}:{u['line']}\t{u['building']}\t{u['name']}\tL{u['level']}\tMISSING\t")
            continue
        c = c[0]
        diffs = []
        if c["tier"] != u["level"] + 1:
            diffs.append(f"level {u['level']} vs tier {c['tier']}")
        if u["cost"]:
            sc, cc = scope_costs(u["cost"][1]), code_costs(c["costs"])
            if sc and sc != cc:
                diffs.append(f"cost {sc} vs {cc}")
        if u["ingame"] and u["ingame"][1].strip():
            if norm(u["ingame"][1]) != norm(c["desc"]):
                diffs.append(f"line '{u['ingame'][1].strip()}' vs '{c['desc']}'")
        if "leads" in u:
            keymap = {}
            for lst in code.values():
                for e in lst: keymap[e["key"].lower()] = norm(e["display"])
            nxt = set(keymap.get(re.sub(r"\d+$","",k).lower(), "?"+k) for k in c["next"].split(",") if k)
            want = set(norm(x) for x in u["leads"])
            if nxt != want:
                diffs.append(f"leads {sorted(want)} vs {sorted(nxt)}")
        loc = c["file"].split("ModSource/")[-1] + ":" + str(c["line"])
        print(f"{short}:{u['line']}\t{u['building']}\t{u['name']}\tL{u['level']}\t{loc}\t{'; '.join(diffs) if diffs else 'MATCH'}")
        if VERBOSE: print("    S:", u.get("numbers","").replace("~","")[:260]); print("    C:", c["pars"])

main()
