"""Seeded sea chart for the Rogue Wave campaign: a Slay the Spire style node map the boat sails across, one row per
map (docs/SEA_CHART.md), with each node's base map and its sea weather. This is the reference for the mod's C# port
(TDS_Mod/ModSource/RogueWavesMode/Code/03_Chart.cs): same PRNG, same steps, same order of draws, so the host, every
client and this script build the same chart from one seed. `--check` prints the lines the port prints, for a diff.

    python3 concept-tools/sea_chart.py [seed ...]           -> docs/concepts/art/sea_chart_<seed>.png and the node list
    python3 concept-tools/sea_chart.py --check [seed ...]   -> CHART lines only (compare with RWChart.Dump)

Rules: a harbour (the start, no map), then ROWS rows of 3-4 map nodes; each node links to 1-3 neighbouring nodes in
the next row (2 most often), every node has a parent, and links never cross. Rows 5 and 10 close a stretch (Breaker maps,
docs/SCOPE_VS_LOOP.md #20). The PRNG is xorshift32, not System.Random, so the C# port is exact on every machine.

Maps: every node gets a base map from POOL, never one twice. Row r aims at the label 2 + (r-1)/2 (labels are the
game's Mapentryobject.Difficulty, 1-9; the final map's 0 counts as 10), a Breaker row two labels higher; the node draws
one of the five maps nearest that aim. Weather: rows 1, 5 and 10 are calm; elsewhere one node in three carries weather
in the first stretch and one in two in the second, and a map never gets the weather it already has (NATIVE).
"""
import os, sys

ROWS = 10
STRETCH = 5
NEAREST = 5                                       # a node draws among this many maps nearest its row's aim

CALM, RISING_WATER, QUICKENING_CURRENT, NESTING_GROUND = 0, 1, 2, 3
WEATHERS = ["Calm", "Rising Water", "Quickening Current", "Nesting Ground"]
# Maps that already carry a weather's object (its stock map): that weather is never rolled there.
NATIVE = {RISING_WATER: {"Map_Manafold"}, QUICKENING_CURRENT: {"Map_Chrono", "Map_Chrono_Alt"},
          NESTING_GROUND: {"Map_Vultures", "Map_Vultures_Alt"}}

# The 45 base Gameplans a vote may offer (every stock plan but Test_ and Special_), sorted by name as the mod sorts
# them (ordinal), with their map and label, from RogueLab round 2 (TDS_Vessel_Creator/reference/roguelab_round2.txt).
POOL = [
    ("0B_Hydragen", "Map_Hydraden", 7), ("1_Skyhook", "Map_Skyhook", 2), ("2B_Maybeth", "Map_Maybeth", 5),
    ("2B_Monogrove", "Map_Monogrove", 5), ("2C_Blasthead", "Map_Blasthead", 4), ("2D_Mercs", "Map_Mercs", 4),
    ("2_Galbass", "Map_Galbass", 3), ("2_Gridhead", "Map_Gridhead", 3), ("2_Ulu", "Map_Ulu", 3),
    ("3BH_Kong", "Map_Kong", 5), ("3B_Thehunt", "Map_Thehunt", 7), ("3B_Woozy", "Map_Woozy", 5),
    ("3_Manafold", "Map_Manafold", 5), ("3_Opal", "Map_Purlest", 4), ("3_Vultures", "Map_Vultures", 5),
    ("4B_Brittle", "Map_Brittle", 7), ("4B_Crimson", "Map_Crimson", 4), ("4C_Oroboro", "Map_Oroboro", 6),
    ("4_Artrio", "Map_Artrio", 5), ("4_Hydroplane", "Map_Hydroplane", 5), ("5B_Crescent", "Map_Crescent", 5),
    ("5B_Final", "Map_Final", 0), ("5B_Hold", "Map_Hold", 7), ("5B_Split", "Map_Split", 6),
    ("5_Chrono", "Map_Chrono", 6), ("5_Mini", "Map_Mini", 5), ("5_Snobound", "Map_Snobound", 4),
    ("Alt_0_Tutorial", "Map_Tutorial_Alt", 9), ("Alt_1_Skyhook", "Map_Skyhook_Alt", 4),
    ("Alt_2B_Monogrove", "Map_Monogrove_Alt", 5), ("Alt_2C_Blasthead", "Map_Blasthead_Alt", 5),
    ("Alt_2D_Mercs", "Map_Mercs_Alt", 5), ("Alt_2_Galbass", "Map_Galbass_Alt", 5),
    ("Alt_2_Gridhead", "Map_Gridhead_Alt", 4), ("Alt_2_Ulu", "Map_Ulu_Alt", 4), ("Alt_3B_Woozy", "Map_Woozy_Alt", 6),
    ("Alt_3_Opal", "Map_Purlest_Alt", 4), ("Alt_3_Vultures", "Map_Vultures_Alt", 5),
    ("Alt_4B_Brittle", "Map_Brittle_Alt", 8), ("Alt_4B_Crimson", "Map_Crimson_Alt", 5),
    ("Alt_4_Artrio", "Map_Artrio_Alt", 5), ("Alt_5B_Crescent", "Map_Crescent_Alt", 6),
    ("Alt_5B_Split", "Map_Split_Alt", 6), ("Alt_5_Chrono", "Map_Chrono_Alt", 6), ("Alt_5_Mini", "Map_Mini_Alt", 6),
]


class Rng:                                        # xorshift32: trivial to port, identical everywhere
    def __init__(self, seed):
        self.s = (seed & 0xFFFFFFFF) or 0x9E3779B9

    def next(self):
        s = self.s
        s ^= (s << 13) & 0xFFFFFFFF
        s ^= s >> 17
        s ^= (s << 5) & 0xFFFFFFFF
        self.s = s
        return s

    def below(self, n):
        return self.next() % n


def links_between(rng, w, nw):
    """Non-crossing links from a row of w nodes to a row of nw: a staircase walk from (first, first) to (last, last).
    Each step adds a child to the current node (k+1), hands the current child on to the next node (i+1), or moves both
    on (i+1, k+1); every cell it visits is a link. So links never cross and every node in both rows is linked. A node
    with one child so far takes a second five times in six; one with two moves on, sharing its last child with the next
    node unless the next row has spare nodes to reach. Without crossings, a node can't
    always have two children (rows of equal width need some single links), so a vote offers 1-3 maps."""
    i = k = 0
    kids = [[0]] + [[] for _ in range(w - 1)]
    while (i, k) != (w - 1, nw - 1):
        n = len(kids[i])
        spare = (nw - 1 - k) - (w - 1 - i)        # child steps beyond one new child for each node still to come
        if i == w - 1:
            move = "k"
        elif k == nw - 1:
            move = "i"
        elif n == 1:
            move = "k" if rng.below(6) else "i"
        elif n == 2 and spare > 0:
            move = ("ik", "ik", "k", "i")[rng.below(4)]
        else:
            move = "ik" if spare > 0 else "i"
        if "i" in move: i += 1
        if "k" in move: k += 1
        kids[i].append(k)
    return kids


def label(entry):
    return 10 if entry[2] == 0 else entry[2]


def aim2(row):                                    # twice the row's aimed label, so every comparison stays an integer
    return 4 + (row - 1) + (4 if row % STRETCH == 0 else 0)


def chart(seed, pool=POOL):
    rng = Rng(seed)
    widths = [1] + [3 + rng.below(2) for _ in range(ROWS)]      # row 0 is the harbour
    rows = [[{"row": r, "col": c, "links": []} for c in range(widths[r])] for r in range(ROWS + 1)]
    for r in range(ROWS):
        for i, kids in enumerate(links_between(rng, widths[r], widths[r + 1]) if r else [list(range(widths[1]))]):
            rows[r][i]["links"] = kids
    for r in range(1, ROWS + 1):                                # sea positions: rows up the chart, a little wander
        for c, n in enumerate(rows[r]):
            w = widths[r]
            n["x"] = (c - (w - 1) / 2) * 34 + (rng.below(9) - 4) * 2
            n["z"] = r * 30 + (rng.below(7) - 3) * 2
            n["breaker"] = r % STRETCH == 0
    rows[0][0].update(x=0, z=0, breaker=False, plan="", map="", label=0, weather=CALM)
    left = sorted(pool)                                         # maps: nearest the row's aim, none twice
    for r in range(1, ROWS + 1):
        for n in rows[r]:
            a = aim2(r)
            near = sorted(left, key=lambda e: (abs(2 * label(e) - a), e[0]))[:NEAREST]
            pick = near[rng.below(len(near))]
            left.remove(pick)
            n["plan"], n["map"], n["label"] = pick[0], pick[1], label(pick)
    for r in range(1, ROWS + 1):                                # weather: calm rows 1, 5, 10; 1 in 3, then 1 in 2
        for n in rows[r]:
            n["weather"] = CALM
            if r == 1 or n["breaker"]:
                continue
            if rng.below(3 if r <= STRETCH else 2) != 0:
                continue
            first = 1 + rng.below(3)
            for k in range(3):
                w = 1 + (first - 1 + k) % 3
                if n["map"] not in NATIVE[w]:
                    n["weather"] = w
                    break
    return rows


def name(n):                                                    # the node's GameObject name in the mod's world
    return "RW_Harbour" if n["row"] == 0 else "RW_Node_%d_%d" % (n["row"], n["col"])


def check_lines(seed, rows):
    """The lines RWChart.Dump logs in game ('CHART ...'), so a PC log can be diffed against this script."""
    out = ["CHART seed %d widths %s" % (seed, ",".join(str(len(r)) for r in rows))]
    for r in rows:
        for n in r:
            out.append("CHART %s %d,%d %s %s w%d -> %s" % (
                name(n), int(round(n["x"] * 10)), int(round(n["z"] * 10)), n["plan"] or "-",
                "B" if n["breaker"] else "-", n["weather"], ",".join(str(k) for k in n["links"]) or "-"))
    return out


def draw(rows, path):
    from PIL import Image, ImageDraw
    S, M = 3, 60
    xs = [n["x"] for r in rows for n in r]; zs = [n["z"] for r in rows for n in r]
    W = int((max(xs) - min(xs)) * S + 2 * M + 260); H = int((max(zs) - min(zs)) * S + 2 * M)
    im = Image.new("RGB", (W, H), (0x2E, 0x4A, 0x62)); d = ImageDraw.Draw(im)   # Deep Swell (docs/STYLE.md)
    P = lambda n: ((n["x"] - min(xs)) * S + M, H - ((n["z"] - min(zs)) * S + M))
    for r in range(len(rows) - 1):
        for n in rows[r]:
            for k in n["links"]:
                d.line([P(n), P(rows[r + 1][k])], fill=(0xE9, 0xF1, 0xEE), width=3)
    tint = {RISING_WATER: (0x3F, 0x8F, 0x96), QUICKENING_CURRENT: (0xF4, 0xEE, 0xF8), NESTING_GROUND: (0xE2, 0x72, 0x5B)}
    for r in rows:
        for n in r:
            x, y = P(n); rad = 16 if n["row"] == 0 else 12
            fill = (0x3F, 0x8F, 0x96) if n["row"] == 0 else (0xE2, 0x72, 0x5B) if n["breaker"] else (0xD8, 0xC4, 0x9A)
            d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=fill, outline=(0x1E, 0x33, 0x45), width=3)
            if n.get("weather"):
                d.rectangle([x + 9, y - 18, x + 19, y - 8], fill=tint[n["weather"]], outline=(0x1E, 0x33, 0x45))
            d.text((x - 6, y - 6), "H" if n["row"] == 0 else str(n["row"]), fill=(0x1E, 0x33, 0x45))
            if n["row"]:
                d.text((x + 16, y + 2), n["plan"], fill=(0xE9, 0xF1, 0xEE))
    im.save(path)


if __name__ == "__main__":
    args = sys.argv[1:]
    check = "--check" in args
    seeds = [int(a) for a in args if a != "--check"] or [20261009]
    out = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "docs", "concepts", "art")
    for seed in seeds:
        rows = chart(seed)
        if check:
            print("\n".join(check_lines(seed, rows)))
            continue
        print("seed", seed, "widths", [len(r) for r in rows])
        for r in rows:
            for n in r:
                nxt = rows[n["row"] + 1] if n["row"] < ROWS else []
                print("  %-14s (%6.1f, %6.1f) %-17s L%-2s %-18s%s -> %s" % (
                    name(n), n["x"], n["z"], n["plan"] or "-", n["label"] or "", WEATHERS[n["weather"]],
                    " Breaker" if n["breaker"] else "", ", ".join(name(nxt[k]) for k in n["links"])))
        draw(rows, os.path.join(out, "sea_chart_%d.png" % seed))
