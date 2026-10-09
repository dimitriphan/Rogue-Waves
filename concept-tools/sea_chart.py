"""Seeded sea chart for the Rogue Wave campaign: a Slay the Spire style node map the boat sails across, one row per
map (docs/SEA_CHART.md). This is the generator the mod would port to C#: same PRNG, same steps, so the host and every
client build the same chart from one seed.

    python3 concept-tools/sea_chart.py [seed ...]      -> docs/concepts/art/sea_chart_<seed>.png and the node list

Rules: a harbour (the start, no map), then ROWS rows of 3-4 map nodes; each node links to 1-3 neighbouring nodes in
the next row (2 most often), every node has a parent, and links never cross. Rows 5 and 10 close a stretch (Breaker maps,
docs/SCOPE_VS_LOOP.md #20). The PRNG is xorshift32, not System.Random, so the C# port is exact on every machine.
"""
import os, sys

ROWS = 10
STRETCH = 5


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


def chart(seed):
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
    rows[0][0].update(x=0, z=0, breaker=False)
    return rows


def name(n):                                                    # the node's GameObject name in the mod's world
    return "Harbour" if n["row"] == 0 else "RW_Node_%d_%d" % (n["row"], n["col"])


def draw(rows, path):
    from PIL import Image, ImageDraw
    S, M = 3, 60
    xs = [n["x"] for r in rows for n in r]; zs = [n["z"] for r in rows for n in r]
    W = int((max(xs) - min(xs)) * S + 2 * M); H = int((max(zs) - min(zs)) * S + 2 * M)
    im = Image.new("RGB", (W, H), (22, 48, 74)); d = ImageDraw.Draw(im)
    P = lambda n: ((n["x"] - min(xs)) * S + M, H - ((n["z"] - min(zs)) * S + M))
    for r in range(len(rows) - 1):
        for n in rows[r]:
            for k in n["links"]:
                d.line([P(n), P(rows[r + 1][k])], fill=(120, 170, 200), width=3)
    for r in rows:
        for n in r:
            x, y = P(n); rad = 16 if n["row"] == 0 else 12
            fill = (226, 200, 120) if n["row"] == 0 else (200, 80, 70) if n["breaker"] else (235, 235, 225)
            d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=fill, outline=(10, 20, 30), width=3)
            d.text((x - 6, y - 6), "H" if n["row"] == 0 else str(n["row"]), fill=(10, 20, 30))
    im.save(path)


if __name__ == "__main__":
    out = os.path.join(os.path.dirname(os.path.dirname(os.path.abspath(__file__))), "docs", "concepts", "art")
    for seed in [int(a) for a in sys.argv[1:]] or [20261009]:
        rows = chart(seed)
        print("seed", seed, "widths", [len(r) for r in rows])
        for r in rows:
            for n in r:
                nxt = rows[n["row"] + 1] if n["row"] < ROWS else []
                print("  %-14s (%6.1f, %6.1f)%s -> %s" % (name(n), n["x"], n["z"], " Breaker" if n["breaker"] else "",
                                                        ", ".join(name(nxt[k]) for k in n["links"])))
        draw(rows, os.path.join(out, "sea_chart_%d.png" % seed))
