"""Mockups of the proposed chart view (docs/mode/CHART_NAVIGATION.md): the whole sea chart as a flat panel, the way a
roguelike map screen reads, drawn from the same generator the mod uses (sea_chart.py), so the layout is a real chart.

    python3 concept-tools/sea_chart_view.py   -> docs/concepts/art/chart_view_vote.png, chart_view_boat.png

Not game art: plain shapes in the docs/STYLE.md palette and the mode's 12 x 12 temporary icons, to agree the layout on.
Map names are the plan names tidied (the game shows each map's own display name).
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sea_chart as sc

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ICONS = os.path.join(ROOT, "TDS_Mod", "ModSource", "RogueWavesMode", "Art", "Temporary", "variants", "v1", "game_size")
OUT = os.path.join(ROOT, "docs", "concepts", "art")

SWELL, DEEP, TEAL, FOAM, CORAL, SAND = (0x2E, 0x4A, 0x62), (0x1E, 0x33, 0x45), (0x3F, 0x8F, 0x96), (0xE9, 0xF1, 0xEE), \
    (0xE2, 0x72, 0x5B), (0xD8, 0xC4, 0x9A)
MUTED = (0x8F, 0xA5, 0xB5)
WEATHER_ICON = {sc.RISING_WATER: "RisingWater", sc.QUICKENING_CURRENT: "Quickening", sc.NESTING_GROUND: "Nesting"}
WEATHER_LINE = {sc.RISING_WATER: "the water climbs each Tide", sc.QUICKENING_CURRENT: "the game runs faster from Tide 5",
                sc.NESTING_GROUND: "a halberd nest grows near the foes"}

SEED = 20261009
ROUTE = [0, 1, 1]          # harbour, row 1 col 1, row 2 col 1: two maps won, the party picks map 3
HOVER = 1                  # the choice under the pointer (or the card with pad focus)


def font(size, bold=False):
    return ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans%s.ttf" % ("-Bold" if bold else ""), size)


def icon(name, scale):
    im = Image.open(os.path.join(ICONS, "RogueWaves_%s.png" % name)).convert("RGBA")
    return im.resize((im.width * scale, im.height * scale), Image.NEAREST)


def nice(plan):
    alt = plan.startswith("Alt_")
    core = plan[4:] if alt else plan
    core = core.split("_", 1)[1] if "_" in core else core
    return core + (" (alt)" if alt else "")


def forward(rows, r, c):
    """Every node reachable from (r, c), itself included."""
    seen, todo = {(r, c)}, [(r, c)]
    while todo:
        a, b = todo.pop()
        if a + 1 < len(rows):
            for k in rows[a][b]["links"]:
                if (a + 1, k) not in seen:
                    seen.add((a + 1, k)); todo.append((a + 1, k))
    return seen


def blend(c, a, bg=SWELL):
    return tuple(int(bg[i] + (c[i] - bg[i]) * a) for i in range(3))


def boat(d, x, y, s=1.0):
    d.polygon([(x - 14 * s, y), (x + 14 * s, y), (x + 9 * s, y + 7 * s), (x - 9 * s, y + 7 * s)], fill=FOAM, outline=DEEP)
    d.line([(x, y), (x, y - 20 * s)], fill=DEEP, width=max(1, int(2 * s)))
    d.polygon([(x + 1, y - 19 * s), (x + 1, y - 3 * s), (x + 12 * s, y - 3 * s)], fill=TEAL, outline=DEEP)


def chart_panel(im, box, rows, route, hover, mini=False, sail=None):
    """Draws the chart into box (x0, y0, x1, y1). sail: fraction of the voted lane already sailed (boat view)."""
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = box
    d.rounded_rectangle(box, 14, fill=SWELL, outline=FOAM, width=2)
    f_h, f_s, f_n, f_t = (font(13, True), font(10), font(9, True), font(9)) if mini else (font(22, True), font(14), font(13, True), font(12))
    head = 34 if mini else 64
    legend_h = 0 if mini else 128
    d.text((x0 + 16, y0 + 10), "SEA CHART", font=f_h, fill=FOAM)
    here_row = len(route) - 1
    d.text((x0 + (120 if mini else 170), y0 + (13 if mini else 16)),
           ("Map %d of 10" % (here_row + 1)) if mini else
           "Map %d of 10   Stretch %d   Retries " % (here_row + 1, 1 if here_row < 5 else 2), font=f_s, fill=FOAM)
    rx = x1 - 50 if mini else x0 + 418
    for i in range(3):
        cx = rx + i * (12 if mini else 18)
        cy = y0 + (19 if mini else 24)
        rr = 4 if mini else 6
        d.ellipse([cx - rr, cy - rr, cx + rr, cy + rr], fill=TEAL, outline=FOAM)
    if not mini:
        d.text((x0 + 16, y0 + 40), "Seed %d" % SEED, font=f_t, fill=MUTED)

    rail = 0 if mini else 64
    ax0, ax1 = x0 + rail + (20 if mini else 30), x1 - (20 if mini else 30)
    ay0, ay1 = y0 + head + (14 if mini else 52), y1 - legend_h - (18 if mini else 34)
    xs = [n["x"] for r in rows for n in r]
    zs = [n["z"] for r in rows for n in r]
    P = lambda n: (ax0 + (n["x"] - min(xs)) / (max(xs) - min(xs)) * (ax1 - ax0),
                   ay1 - (n["z"] - min(zs)) / (max(zs) - min(zs)) * (ay1 - ay0))
    rowy = lambda r: ay1 - ((sum(n["z"] for n in rows[r]) / len(rows[r])) - min(zs)) / (max(zs) - min(zs)) * (ay1 - ay0)

    if not mini:   # the rail: row numbers, the two stretches, the last map
        for r in range(len(rows)):
            y = rowy(r)
            lab = "H" if r == 0 else str(r)
            col = CORAL if r % 5 == 0 and r else (FOAM if r == here_row + 1 else MUTED)
            d.text((x0 + 40, y - 8), lab, font=font(14, True), fill=col, anchor="ma")
        for k, (a, b) in enumerate([(1, 5), (6, 10)]):
            ya, yb = rowy(a) + 8, rowy(b) - 8
            d.line([(x0 + 18, ya), (x0 + 18, yb)], fill=MUTED, width=2)
            d.line([(x0 + 18, ya), (x0 + 24, ya)], fill=MUTED, width=2)
            d.line([(x0 + 18, yb), (x0 + 24, yb)], fill=MUTED, width=2)
            t = Image.new("RGBA", (120, 16), (0, 0, 0, 0))
            ImageDraw.Draw(t).text((60, 0), "STRETCH %d" % (k + 1), font=font(10, True), fill=MUTED, anchor="ma")
            t = t.rotate(90, expand=True)
            im.paste(t, (int(x0 + 2), int((ya + yb) / 2 - 60)), t)
        d.text(((ax0 + ax1) / 2, ay0 - 40), "ROW 10: THE LAST MAP", font=font(11, True), fill=CORAL, anchor="ma")

    here = (here_row, route[-1])
    reach = forward(rows, *here)
    cone = forward(rows, here_row + 1, hover) if hover is not None else set()
    sailed = {(r, c) for r, c in enumerate(route)}
    choices = [(here_row + 1, k) for k in rows[here_row][route[-1]]["links"]]
    voted = choices[hover] if hover is not None else None

    for r in range(len(rows) - 1):   # lanes: faint off the route, plain ahead, bright on the hovered choice's way, teal sailed
        for n in rows[r]:
            for k in n["links"]:
                a, b = (r, n["col"]), (r + 1, k)
                pa, pb = P(n), P(rows[r + 1][k])
                if a in sailed and b in sailed:
                    d.line([pa, pb], fill=TEAL, width=7 if not mini else 4)
                elif a in reach and b in reach:
                    on = (a in cone and b in cone) or (a == here and b == voted)
                    if sail is not None and a == here and b == voted:
                        mx = (pa[0] + (pb[0] - pa[0]) * sail, pa[1] + (pb[1] - pa[1]) * sail)
                        d.line([pa, mx], fill=TEAL, width=4)
                        dash(d, mx, pb, FOAM, 3)
                    else:
                        d.line([pa, pb], fill=FOAM if on else blend(FOAM, 0.55), width=(4 if on else 2) if not mini else (2 if on else 1))
                else:
                    d.line([pa, pb], fill=blend(FOAM, 0.16), width=1 if mini else 2)

    for row in rows:
        for n in row:
            key = (n["row"], n["col"])
            x, y = P(n)
            rad = (7 if mini else 15) if n["row"] else (9 if mini else 18)
            a = 1.0 if key in reach or key in sailed else 0.28
            if n["row"] == 0:
                fill = TEAL
            elif key in sailed:
                fill = TEAL
            elif n["breaker"]:
                fill = CORAL
            else:
                fill = SAND
            fill = blend(fill, a)
            if key in choices and sail is None:
                for g in (10, 6):
                    d.ellipse([x - rad - g, y - rad - g, x + rad + g, y + rad + g], outline=blend(FOAM, 0.35 if g == 10 else 0.8), width=2)
            if n["breaker"]:
                d.ellipse([x - rad - 4, y - rad - 4, x + rad + 4, y + rad + 4], outline=blend(CORAL, a), width=2)
            d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=fill, outline=blend(DEEP, a), width=2)
            if not mini:
                if n["row"] == 0:
                    d.text((x, y - 8), "H", font=font(14, True), fill=FOAM, anchor="ma")
                elif key in sailed:
                    d.line([(x - 6, y), (x - 2, y + 5), (x + 7, y - 6)], fill=FOAM, width=3)
                else:
                    d.text((x, y - 7), str(n["label"]), font=f_n, fill=blend(DEEP, a), anchor="ma")
                if n["weather"] and key not in sailed:
                    ic = icon(WEATHER_ICON[n["weather"]], 2)
                    if a < 1:
                        ic.putalpha(ic.getchannel("A").point(lambda v: int(v * 0.35)))
                    im.paste(ic, (int(x + rad - 6), int(y - rad - 14)), ic)
            elif n["weather"] and key not in sailed and a == 1:
                d.rectangle([x + 5, y - 10, x + 10, y - 5], fill=TEAL if n["weather"] == 1 else FOAM if n["weather"] == 2 else CORAL, outline=DEEP)
            if key in choices and sail is None and not mini:
                letter = "ABC"[choices.index(key)]
                tx, ty = x - rad - 24, y - 10
                d.rounded_rectangle([tx - 10, ty - 1, tx + 10, ty + 19], 4, fill=FOAM if key == voted else TEAL, outline=DEEP)
                d.text((tx, ty + 1), letter, font=font(13, True), fill=DEEP, anchor="ma")

    hx, hy = P(rows[here_row][route[-1]])
    if sail is None:
        boat(d, hx + (16 if mini else 26), hy + (4 if mini else 8), 0.6 if mini else 1.0)
        if not mini:
            d.text((hx + 46, hy + 2), "YOU ARE HERE", font=font(10, True), fill=FOAM)
    else:
        vx, vy = P(rows[voted[0]][voted[1]])
        bx, by = hx + (vx - hx) * sail, hy + (vy - hy) * sail
        boat(d, bx + 2, by - 2, 0.6 if mini else 1.0)
        d.ellipse([vx - 13, vy - 13, vx + 13, vy + 13], outline=FOAM, width=2)

    if not mini:   # the legend
        ly = y1 - legend_h
        d.line([(x0 + 16, ly), (x1 - 16, ly)], fill=blend(FOAM, 0.3), width=1)
        items = [("disc", SAND, "Map (number = Danger)"), ("breaker", CORAL, "Breaker: closes a stretch"),
                 ("disc", TEAL, "Sailed / harbour"), ("ring", FOAM, "Can sail to next"),
                 ("icon", "RisingWater", "Rising Water"), ("icon", "Quickening", "Quickening Current"),
                 ("icon", "Nesting", "Nesting Ground"), ("fade", SAND, "Out of reach from here")]
        for i, (kind, c, text) in enumerate(items):
            cx = x0 + 34 + (i % 2) * 270
            cy = ly + 22 + (i // 2) * 26
            if kind == "disc":
                d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], fill=c, outline=DEEP, width=2)
            elif kind == "breaker":
                d.ellipse([cx - 11, cy - 11, cx + 11, cy + 11], outline=c, width=2)
                d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], fill=c, outline=DEEP, width=2)
            elif kind == "ring":
                d.ellipse([cx - 13, cy - 13, cx + 13, cy + 13], outline=c, width=2)
                d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], fill=SAND, outline=DEEP, width=2)
            elif kind == "fade":
                d.ellipse([cx - 8, cy - 8, cx + 8, cy + 8], fill=blend(c, 0.28), outline=blend(DEEP, 0.28), width=2)
            else:
                ic = icon(c, 2)
                im.paste(ic, (cx - 12, cy - 12), ic)
            d.text((cx + 22, cy - 8), text, font=f_t, fill=FOAM)
    return P, choices


def dash(d, a, b, col, w, on=8, off=6):
    import math
    L = math.hypot(b[0] - a[0], b[1] - a[1])
    if L == 0:
        return
    ux, uy = (b[0] - a[0]) / L, (b[1] - a[1]) / L
    t = 0
    while t < L:
        e = min(L, t + on)
        d.line([(a[0] + ux * t, a[1] + uy * t), (a[0] + ux * e, a[1] + uy * e)], fill=col, width=w)
        t += on + off


def card(im, box, letter, n, focus):
    d = ImageDraw.Draw(im)
    x0, y0, x1, y1 = box
    d.rounded_rectangle(box, 10, fill=SWELL, outline=FOAM if focus else (CORAL if n["breaker"] else TEAL), width=4 if focus else 3)
    d.rounded_rectangle([x0 + 12, y0 + 12, x0 + 40, y0 + 40], 6, fill=FOAM if focus else TEAL, outline=DEEP)
    d.text((x0 + 26, y0 + 15), letter, font=font(17, True), fill=DEEP, anchor="ma")
    d.rectangle([x0 + 54, y0 + 14, x0 + 102, y0 + 62], fill=DEEP, outline=MUTED)
    if n["weather"]:
        ic = icon(WEATHER_ICON[n["weather"]], 3)
        im.paste(ic, (x0 + 60, y0 + 20), ic)
    else:
        d.text((x0 + 78, y0 + 30), "map", font=font(10), fill=MUTED, anchor="ma")
    d.text((x0 + 116, y0 + 14), nice(n["plan"]), font=font(17, True), fill=FOAM)
    d.text((x0 + 116, y0 + 40), "Danger %d" % n["label"], font=font(13), fill=SAND)
    if n["weather"]:
        d.text((x0 + 14, y0 + 74), "%s: %s." % (sc.WEATHERS[n["weather"]], WEATHER_LINE[n["weather"]]), font=font(12), fill=FOAM)
        d.text((x0 + 14, y0 + 92), "+1 Pure Aura each Tide.", font=font(12), fill=FOAM)
    else:
        d.text((x0 + 14, y0 + 74), "Calm waters.", font=font(12), fill=FOAM)


def vote_view(rows):
    W, H = 1600, 900
    im = Image.new("RGB", (W, H), (0x26, 0x33, 0x2C))
    d = ImageDraw.Draw(im)
    for i in range(0, W, 40):                       # a stand-in for the map scene, dimmed under the vote
        for j in range(0, H, 40):
            if (i // 40 + j // 40) % 2:
                d.rectangle([i, j, i + 39, j + 39], fill=(0x2A, 0x38, 0x30))
    d.text((W - 20, H - 24), "the map scene, dimmed under the vote (stand-in)", font=font(11), fill=(0x55, 0x66, 0x5C), anchor="ra")
    P, choices = chart_panel(im, (40, 30, 650, 870), rows, ROUTE, HOVER)
    # the hovered choice's card: what the player sees before choosing
    n = rows[choices[HOVER][0]][choices[HOVER][1]]
    x, y = P(n)
    tip = (int(x) + 40, int(y) + 34, int(x) + 40 + 250, int(y) + 34 + 108)
    d.rounded_rectangle(tip, 8, fill=DEEP, outline=FOAM, width=2)
    d.text((tip[0] + 12, tip[1] + 10), nice(n["plan"]), font=font(14, True), fill=FOAM)
    d.text((tip[0] + 12, tip[1] + 32), "Danger %d   Row %d" % (n["label"], n["row"]), font=font(12), fill=SAND)
    d.text((tip[0] + 12, tip[1] + 52), sc.WEATHERS[n["weather"]] if n["weather"] else "Calm waters", font=font(12), fill=FOAM)
    ahead = [rows[n["row"] + 1][k] for k in n["links"]]
    d.text((tip[0] + 12, tip[1] + 74), "Leads on to: " + ", ".join("%s (%d)" % (nice(a["plan"]), a["label"]) for a in ahead),
           font=font(11), fill=MUTED)
    # the stock choice dialog, beside the chart, cards lettered to match
    d.text((1100, 70), "WHERE NEXT?", font=font(30, True), fill=FOAM, anchor="ma")
    d.text((1100, 112), "Pick a lit island or a card. 18 s left", font=font(15), fill=FOAM, anchor="ma")
    for i, (r, c) in enumerate(choices):
        card(im, (760, 160 + i * 150, 1440, 160 + i * 150 + 128), "ABC"[i], rows[r][c], i == HOVER)
    d.text((1100, 640), "Your vote: B.  Votes so far: A 1, B 1  (host's tally, option)", font=font(14), fill=FOAM, anchor="ma")
    d.text((1100, 668), "The highlighted lanes on the chart are where B leads.", font=font(13), fill=MUTED, anchor="ma")
    im.save(os.path.join(OUT, "chart_view_vote.png"))


def boat_view(rows):
    W, H = 1600, 900
    im = Image.new("RGB", (W, H), (0x2B, 0x5C, 0x7A))
    d = ImageDraw.Draw(im)
    d.text((40, H - 40), "the 3D chart on the boat, as built today (stand-in): the boat sails the voted lane by itself",
           font=font(13), fill=(0x9F, 0xC4, 0xD6))
    for (x, y, w) in [(300, 640, 150), (700, 380, 130), (1000, 420, 120), (470, 360, 120)]:
        d.ellipse([x - w, y - w * 0.35, x + w, y + w * 0.35], fill=(0x8E, 0x9E, 0x6A), outline=(0xD8, 0xC4, 0x9A), width=4)
    d.ellipse([700 - 160, 380 - 60, 700 + 160, 380 + 60], outline=FOAM, width=4)        # the voted island's ring
    dash(d, (330, 600), (690, 400), FOAM, 6, 18, 12)                                    # the voted lane, bright
    boat(d, 420, 560, 2.2)
    d.rounded_rectangle([480, 26, 1120, 86], 12, fill=SWELL, outline=FOAM, width=2)
    d.text((800, 34), "SAILING TO GRIDHEAD (ALT), AS VOTED", font=font(20, True), fill=FOAM, anchor="ma")
    d.text((800, 62), "Map 3 of 10  ·  Danger 4  ·  Quickening Current", font=font(13), fill=SAND, anchor="ma")
    chart_panel(im, (1250, 110, 1570, 640), rows, ROUTE, HOVER, mini=True, sail=0.45)
    d.text((1410, 652), "M  open the whole chart", font=font(13, True), fill=FOAM, anchor="ma")
    im.save(os.path.join(OUT, "chart_view_boat.png"))


if __name__ == "__main__":
    rows = sc.chart(SEED)
    vote_view(rows)
    boat_view(rows)
    print("wrote chart_view_vote.png, chart_view_boat.png in", OUT)
