"""The chart view drawn in the game's own UI look (docs/mode/CHART_NAVIGATION.md §3.6), second draft after Dimitri's
"more in line with the current TDS art style and theme" (2026-11-06). Same chart, stops and moment as
sea_chart_view.py (the layout draft); only the look changes, taken from the stock dialog, VENDCINT and the HUD as the
PC round 4 pictures show them:

- no panel: the chart floats on the dialog's dim, like the stock choice dialog's cards;
- a thin uppercase title with the stock corner rule (a small glyph, a line under the title that turns down the right
  side), and one short caption line with "•" between its parts, as under the stock cards;
- square tiles for islands, as the stock cards are squares: Rogue Teal, a Breaker in Storm Coral (the coral tile
  VENDCINT uses), a sailed one dark; white one-pixel line icons on them (the game's line-drawing icons);
- the choices in white corner brackets, the game's selection mark; lanes as thin rules, dashed ahead, solid sailed.

    python3 concept-tools/sea_chart_look.py                  -> docs/concepts/art/chart_look_vote.png, chart_look_boat.png
    python3 concept-tools/sea_chart_look.py --over SHOT.png  -> the vote view over a real map screenshot (1600 x 900 crop
                                                                of SHOT), written next to SHOT; never committed

Fonts: Inter Light and Regular stand in for the game's thin sans. Not game art; the pixel icons are drawn here as
string rows in the kit's way (TDS_Vessel_Creator/docs/03 §2) and would become the mode's temporary art.
"""
import os, sys
from PIL import Image, ImageDraw, ImageFont, ImageFilter

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sea_chart as sc
from sea_chart_view import stops, forward, nice, SEED, ROUTE, HOVER, RETRIES, ICONS, OUT, WEATHER_ICON

TEAL, SWELL, FOAM, CORAL, SAND = (0x3F, 0x8F, 0x96), (0x2E, 0x4A, 0x62), (0xE9, 0xF1, 0xEE), (0xE2, 0x72, 0x5B), (0xD8, 0xC4, 0x9A)
DARK = (0x2A, 0x33, 0x3D)          # the dark tile (VENDCINT's back button)
INK = (0xF2, 0xF4, 0xF3)
S = 2                              # supersampling for the vector layer

FONT_DIR = "/usr/share/fonts/opentype/inter/"


def font(size, weight="Light"):
    return ImageFont.truetype(FONT_DIR + "Inter-%s.otf" % weight, size * S)


# 12 x 12 line icons, '#' = ink, '+' = accent (Wet Sand), '.' = clear
PIX = {
    "harbour": ["....####....", "....#..#....", "....####....", ".....##.....", "...######...", ".....##.....",
                ".....##.....", "#....##....#", "##...##...##", ".##..##..##.", "..###..###..", "....####...."],
    "cove": ["............", ".....#......", ".....##.....", "....#.#.....", "....#..#....", "...#.++.#...",
             "...#.++.#...", "....#..#....", ".....##.....", ".##......##.", "...######...", ".##......##."],
    "wreck": ["............", "............", "..########..", ".#........#.", ".#........#.", ".####++####.",
              ".#...++...#.", ".#........#.", ".#........#.", ".##########.", "............", "............"],
    "check": ["............", "............", "..........#.", ".........#..", "........#...", "#......#....",
              ".#....#.....", "..#..#......", "...##.......", "............", "............", "............"],
    "boat": [".....#......", ".....##.....", ".....#+#....", ".....#++#...", ".....#+++#..", ".....#####..",
             ".....#......", "############", ".#........#.", "..########..", "............", "............"],
}


def pix(name, scale, ink=INK, accent=SAND, alpha=255):
    rows = PIX[name]
    im = Image.new("RGBA", (12 * scale, 12 * scale), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    for y, row in enumerate(rows):
        assert len(row) == 12, (name, y)
        for x, ch in enumerate(row):
            if ch != ".":
                c = ink if ch == "#" else accent
                d.rectangle([x * scale, y * scale, x * scale + scale - 1, y * scale + scale - 1], fill=c + (alpha,))
    return im


def wicon(name, scale, alpha=255):
    im = Image.open(os.path.join(ICONS, "RogueWaves_%s.png" % name)).convert("RGBA")
    im = im.resize((im.width * scale, im.height * scale), Image.NEAREST)
    if alpha < 255:
        im.putalpha(im.getchannel("A").point(lambda v: v * alpha // 255))
    return im


class Canvas:
    """A supersampled vector layer plus crisp pixel stamps pasted after the downsample."""

    def __init__(self, w, h):
        self.w, self.h = w, h
        self.v = Image.new("RGBA", (w * S, h * S), (0, 0, 0, 0))
        self.d = ImageDraw.Draw(self.v)
        self.stamps = []

    def line(self, pts, col, a=255, w=1):
        self.d.line([(x * S, y * S) for x, y in pts], fill=col + (a,), width=max(1, int(w * S)))

    def dashed(self, p, q, col, a=255, w=1, on=5, off=4):
        import math
        L = math.hypot(q[0] - p[0], q[1] - p[1])
        ux, uy = (q[0] - p[0]) / L, (q[1] - p[1]) / L
        t = 0
        while t < L:
            e = min(L, t + on)
            self.line([(p[0] + ux * t, p[1] + uy * t), (p[0] + ux * e, p[1] + uy * e)], col, a, w)
            t += on + off

    def rect(self, box, fill=None, outline=None, a=255, oa=255, w=1):
        x0, y0, x1, y1 = box
        self.d.rectangle([x0 * S, y0 * S, x1 * S - 1, y1 * S - 1], fill=fill + (a,) if fill else None,
                         outline=outline + (oa,) if outline else None, width=int(w * S))

    def text(self, xy, s, f, col=INK, a=255, anchor="la", spacing=0):
        if spacing:
            x, y = xy
            total = sum(self.d.textlength(ch, font=f) for ch in s) / S + spacing * (len(s) - 1)
            if anchor[0] == "m":
                x -= total / 2
            elif anchor[0] == "r":
                x -= total
            for ch in s:
                self.d.text((x * S, y * S), ch, font=f, fill=col + (a,), anchor="l" + anchor[1])
                x += self.d.textlength(ch, font=f) / S + spacing
            return
        self.d.text((xy[0] * S, xy[1] * S), s, font=f, fill=col + (a,), anchor=anchor)

    def stamp(self, im, x, y):
        self.stamps.append((im, int(round(x)), int(round(y))))

    def onto(self, base):
        small = self.v.resize((self.w, self.h), Image.LANCZOS)
        out = base.convert("RGBA")
        out.alpha_composite(small)
        for im, x, y in self.stamps:
            out.alpha_composite(im, (x, y))
        return out


def brackets(c, box, a=255, arm=6, w=1.5):
    """The game's selection mark: white corner brackets."""
    x0, y0, x1, y1 = box
    for (x, y, dx, dy) in ((x0, y0, 1, 1), (x1, y0, -1, 1), (x0, y1, 1, -1), (x1, y1, -1, -1)):
        c.line([(x, y + dy * arm), (x, y), (x + dx * arm, y)], INK, a, w)


def ruled_title(c, x, y, text, size, right, bottom, a=235):
    """The stock dialog's title: a small glyph, the title in thin capitals, a rule under it that turns down the right."""
    f = font(size)
    c.stamp(wicon("Crest", 1, a), x - 16, y + size * 0.85)
    c.text((x, y), text, f, INK, a, spacing=size * 0.06)
    ry = y + size * 1.45
    c.line([(x - 18, ry), (right, ry), (right, bottom)], INK, 150, 1)


def tile(c, n, key, x, y, size, state, alpha):
    """An island as a square tile. state: 'sailed', 'harbour', 'open'."""
    stop = n.get("stop")
    half = size / 2
    if state == "harbour":
        fill = TEAL
    elif state == "sailed":
        fill = DARK
    elif n["breaker"]:
        fill = CORAL
    else:
        fill = TEAL
    a = int(255 * alpha)
    c.rect((x - half, y - half, x + half, y + half), fill, (min(255, fill[0] + 60), min(255, fill[1] + 60), min(255, fill[2] + 60)), a, a, 1)
    sc_ = 2 if size >= 26 else 1
    if state == "harbour":
        c.stamp(pix("harbour", sc_, alpha=a), x - 6 * sc_, y - 6 * sc_)
    elif state == "sailed":
        c.stamp(pix("check", sc_, alpha=a), x - 6 * sc_, y - 6 * sc_)
    elif stop == "cove":
        c.stamp(pix("cove", sc_, alpha=a), x - 6 * sc_, y - 6 * sc_)
    elif stop == "wreck":
        c.stamp(pix("wreck", sc_, alpha=a), x - 6 * sc_, y - 6 * sc_)
    else:
        c.text((x, y + 0.5), str(n["label"]), font(int(size * 0.5), "Regular"), INK, a, anchor="mm")
    if n["weather"] and state == "open" and size >= 20:
        c.stamp(wicon(WEATHER_ICON[n["weather"]], 1, a), x + half - 5, y - half - 8)


def chart(c, box, rows, route, hover, mini=False, sail=None):
    x0, y0, x1, y1 = box
    here_row = len(route) - 1
    title_size = 15 if mini else 26
    ruled_title(c, x0 + 20, y0, "SEA CHART", title_size, x1, y1)
    cap = font(9 if mini else 12)
    retries = "%d retries left" % RETRIES
    c.text((x0 + 20, y0 + title_size * 1.45 + 6), ("Row %d of 10  •  %s" if mini else "Row %d of 10  •  Stretch 1  •  %s  •  Seed %d")
           % ((here_row + 1, retries) if mini else (here_row + 1, retries, SEED)), cap, INK, 190)
    rail = 0 if mini else 46
    legend_h = 0 if mini else 58
    ax0, ax1 = x0 + rail + (18 if mini else 30), x1 - (18 if mini else 34)
    ay0, ay1 = y0 + title_size * 1.45 + (34 if mini else 70), y1 - legend_h - (14 if mini else 30)
    xs = [n["x"] for r in rows for n in r]
    zs = [n["z"] for r in rows for n in r]
    P = lambda n: (ax0 + (n["x"] - min(xs)) / (max(xs) - min(xs)) * (ax1 - ax0),
                   ay1 - (n["z"] - min(zs)) / (max(zs) - min(zs)) * (ay1 - ay0))
    rowy = lambda r: sum(P(n)[1] for n in rows[r]) / len(rows[r])

    if not mini:   # the rail: row numbers and the stretches, small and thin
        for r in range(len(rows)):
            lab = "H" if r == 0 else str(r)
            col, a = (CORAL, 230) if r % 5 == 0 and r else (INK, 235 if r == here_row + 1 else 120)
            c.text((x0 + 30, rowy(r)), lab, font(12, "Regular"), col, a, anchor="mm")
        for k, (a_, b_) in enumerate([(1, 5), (6, 10)]):
            ya, yb = rowy(a_) + 6, rowy(b_) - 6
            c.line([(x0 + 14, ya), (x0 + 14, yb)], INK, 90, 1)
        c.text((ax0 + (ax1 - ax0) / 2, ay0 - 30), "THE LAST MAP", font(10, "Regular"), CORAL, 220, anchor="mm", spacing=1.5)

    here = (here_row, route[-1])
    reach = forward(rows, *here)
    choices = [(here_row + 1, k) for k in rows[here_row][route[-1]]["links"]]
    voted = choices[hover]
    cone = forward(rows, *voted)
    sailed = set(enumerate(route))
    T = 11 if mini else 26           # tile size

    for r in range(len(rows) - 1):
        for n in rows[r]:
            for k in n["links"]:
                a_, b_ = (r, n["col"]), (r + 1, k)
                p, q = P(n), P(rows[r + 1][k])
                if a_ in sailed and b_ in sailed:
                    c.line([p, q], INK, 235, 2.5 if not mini else 1.5)
                elif a_ in reach and b_ in reach:
                    on = (a_ in cone and b_ in cone) or (a_ == here and b_ == voted)
                    if sail is not None and a_ == here and b_ == voted:
                        m = (p[0] + (q[0] - p[0]) * sail, p[1] + (q[1] - p[1]) * sail)
                        c.line([p, m], INK, 235, 1.5)
                        c.dashed(m, q, INK, 200, 1, 3, 3)
                    else:
                        c.dashed(p, q, INK, 230 if on else 110, 1.5 if on else 1, 5 if not mini else 3, 4 if not mini else 3)
                else:
                    c.line([p, q], INK, 35, 1)

    for row in rows:
        for n in row:
            key = (n["row"], n["col"])
            x, y = P(n)
            state = "harbour" if n["row"] == 0 else ("sailed" if key in sailed else "open")
            alpha = 1.0 if key in reach or key in sailed else 0.3
            size = T + (4 if key in choices and sail is None else 0)
            tile(c, n, key, x, y, size, state, alpha)
            if key in choices and sail is None:
                s = size / 2 + (7 if key == voted else 5)
                brackets(c, (x - s, y - s, x + s, y + s), 255 if key == voted else 170, 6 if key == voted else 4)
                letter = "ABC"[choices.index(key)]
                c.text((x - s - 9, y), letter, font(13, "Regular"), INK, 235, anchor="mm")

    hx, hy = P(rows[here_row][route[-1]])
    if sail is None:
        c.stamp(pix("boat", 2 if not mini else 1), hx + T / 2 + 6, hy - 12 if not mini else hy - 6)
        if not mini:
            c.text((hx + T / 2 + 36, hy), "YOU ARE HERE", font(10, "Regular"), INK, 215, anchor="lm", spacing=1)
    else:
        vx, vy = P(rows[voted[0]][voted[1]])
        bx, by = hx + (vx - hx) * sail, hy + (vy - hy) * sail
        c.stamp(pix("boat", 1), bx - 6, by - 10)
        s = T / 2 + 4
        brackets(c, (vx - s, vy - s, vx + s, vy + s), 255, 4)

    if not mini:   # the legend: tiles and words on one caption line, the stock "•" between
        ly = y1 - legend_h + 10
        items = [("tile", TEAL, "Map, its Danger"), ("tile", CORAL, "Breaker"), ("cove", TEAL, "Cove: rest"),
                 ("wreck", TEAL, "Wreck: harder, a Trinket"), ("tile", DARK, "Sailed")]
        x = x0 + 20
        f = font(11)
        for i, (kind, col, text) in enumerate(items):
            if i == 3:
                x, ly = x0 + 20, ly + 26
            c.rect((x, ly - 8, x + 16, ly + 8), col, (min(255, col[0] + 60), min(255, col[1] + 60), min(255, col[2] + 60)))
            if kind in ("cove", "wreck"):
                c.stamp(pix(kind, 1), x + 2, ly - 6)
            c.text((x + 22, ly), text, f, INK, 205, anchor="lm")
            x += 22 + c.d.textlength(text, font=f) / S + 22
        x += 4
        for w in (sc.RISING_WATER, sc.QUICKENING_CURRENT, sc.NESTING_GROUND):
            c.stamp(wicon(WEATHER_ICON[w], 1), x, ly - 6)
            x += 16
        c.text((x + 4, ly), "Weather", f, INK, 205, anchor="lm")
    return P, choices


def card(c, x, y, size, n, letter, focus):
    stop = n.get("stop")
    fill = CORAL if n["breaker"] else TEAL
    c.rect((x, y, x + size, y + size), fill, (min(255, fill[0] + 60), min(255, fill[1] + 60), min(255, fill[2] + 60)), 255, 255, 1)
    if stop in ("cove", "wreck"):
        c.stamp(pix(stop, 2), x + size / 2 - 12, y + size / 2 - 12)
    elif n["weather"]:
        c.stamp(wicon(WEATHER_ICON[n["weather"]], 2), x + size / 2 - 12, y + size / 2 - 12)
    else:
        c.text((x + size / 2, y + size / 2), str(n["label"]), font(20, "Regular"), INK, 255, anchor="mm")
    c.text((x + 5, y + 4), letter, font(10, "Regular"), INK, 220)
    if focus:
        brackets(c, (x - 5, y - 5, x + size + 5, y + size + 5), 255, 7)


def caption_for(n, rows):
    if n.get("stop") == "cove":
        parts = ["The Cove", "No map", "A retry back (%d to %d)" % (RETRIES, min(3, RETRIES + 1))]
    else:
        parts = [nice(n["plan"]) + (" (Wreck)" if n.get("stop") == "wreck" else ""), "Danger %d" % n["label"]]
        if n.get("stop") == "wreck":
            parts.append("Plays one row harder, pays a Trinket")
        elif n["weather"]:
            parts.append(sc.WEATHERS[n["weather"]])
    ahead = [rows[n["row"] + 1][k] for k in n["links"]]
    parts.append("Leads on to " + ", ".join(nice(a["plan"]) for a in ahead))
    return "  •  ".join(parts)


def stand_in(w, h, tint=(0x4C, 0x58, 0x66)):
    """A blurred low-poly stand-in for the game's 3D scene (the real one would be the map or the boat's world)."""
    import random
    rnd = random.Random(7)
    im = Image.new("RGB", (w, h), tint)
    d = ImageDraw.Draw(im)
    for _ in range(140):
        x, y = rnd.randrange(-100, w), rnd.randrange(-100, h)
        s = rnd.randrange(60, 260)
        k = rnd.randrange(-18, 22)
        col = tuple(max(0, min(255, t + k)) for t in tint)
        d.polygon([(x, y), (x + s, y + rnd.randrange(-40, 40)), (x + rnd.randrange(0, s), y + s * 0.6)], fill=col)
    return im.filter(ImageFilter.GaussianBlur(3))


def dim(im, a=150):
    """The stock dialog's Fade: a dark wash over the scene."""
    wash = Image.new("RGBA", im.size, (0x1C, 0x24, 0x2C, a))
    out = im.convert("RGBA")
    out.alpha_composite(wash)
    return out


def vote_view(rows, background=None):
    W, H = 1600, 900
    base = dim(background if background is not None else stand_in(W, H), 165)
    # a little more dim behind the chart, fading out toward the dialog, so lanes read over a busy map
    grad = Image.new("L", (W, H), 0)
    gd = ImageDraw.Draw(grad)
    for x in range(0, 820):
        gd.line([(x, 0), (x, H)], fill=int(95 * min(1.0, (820 - x) / 260)))
    wash = Image.new("RGBA", (W, H), (0x1C, 0x24, 0x2C, 0))
    wash.putalpha(grad)
    base.alpha_composite(wash)
    c = Canvas(W, H)
    P, choices = chart(c, (70, 46, 690, 860), rows, ROUTE, HOVER)
    # the stock dialog, moved to the right half: title and rule, the square cards, one caption line
    ruled_title(c, 860, 300, "WHERE NEXT?", 28, 1440, 470)
    size, gap = 64, 18
    x = 860
    for i, (r, k) in enumerate(choices):
        card(c, x, 370, size, rows[r][k], "ABC"[i], i == HOVER)
        x += size + gap
    n = rows[choices[HOVER][0]][choices[HOVER][1]]
    c.text((860, 470), caption_for(n, rows), font(13), INK, 225)
    c.text((860, 494), "Pick a card or a lit island  •  A tie goes to A  •  18 s", font(12), INK, 150)
    out = c.onto(base)
    return out


def boat_view(rows):
    W, H = 1600, 900
    base = stand_in(W, H, (0x5E, 0x7E, 0x92)).convert("RGBA")
    c = Canvas(W, H)
    # the 3D chart as built: islands, the lane sailed, the boat (stand-ins), a ring of brackets on where it sails to
    for (x, y, w) in [(330, 650, 150), (760, 390, 130), (1060, 430, 120), (520, 370, 120)]:
        c.d.ellipse([(x - w) * S, (y - w * 0.32) * S, (x + w) * S, (y + w * 0.32) * S], fill=(0xC9, 0xC5, 0xB8, 255),
                    outline=(0xE6, 0xE2, 0xD6, 255), width=2 * S)
    c.dashed((360, 610), (740, 410), INK, 230, 2.5, 14, 10)
    brackets(c, (600, 320, 920, 460), 240, 18, 2)
    # a banner in the HUD's own shape (TIDE APPROACHING): a filled bar, a lighter rim, thin capitals
    c.rect((520, 30, 1080, 82), TEAL, (0x8F, 0xC9, 0xCC), 235, 255, 2)
    c.text((800, 56), "SAILING TO THE COVE", font(26), INK, 255, anchor="mm", spacing=1.5)
    c.text((800, 100), "Row 4 of 10  •  A retry back  •  Then on to the party's second pick", font(13), INK, 230, anchor="mm")
    chart(c, (1250, 140, 1560, 660), rows, ROUTE, HOVER, mini=True, sail=0.45)
    c.text((1405, 680), "M  •  the whole chart", font(12, "Regular"), INK, 220, anchor="mm")
    out = c.onto(base)
    d = ImageDraw.Draw(out)
    # the boat itself, a stand-in
    bx, by = 440, 590
    d.polygon([(bx - 30, by), (bx + 30, by), (bx + 20, by + 14), (bx - 20, by + 14)], fill=(0xEF, 0xEF, 0xE8, 255))
    d.line([(bx, by), (bx, by - 44)], fill=(0x6B, 0x55, 0x40, 255), width=3)
    d.polygon([(bx + 2, by - 42), (bx + 2, by - 6), (bx + 26, by - 6)], fill=(0xE2, 0x72, 0x5B, 255))
    d.text((40, 860), "stand-in for the boat's 3D chart as built today", font=ImageFont.truetype(FONT_DIR + "Inter-Light.otf", 12), fill=(0xFF, 0xFF, 0xFF, 140))
    return out


if __name__ == "__main__":
    rows = stops(sc.chart(SEED), SEED)
    args = sys.argv[1:]
    if args[:1] == ["--over"]:
        shot = Image.open(args[1]).convert("RGB")
        shot = shot.resize((1600, int(shot.height * 1600 / shot.width)), Image.LANCZOS).crop((0, 0, 1600, 900))
        path = os.path.splitext(args[1])[0] + "_chart_look.png"
        vote_view(rows, shot).convert("RGB").save(path)
        print("wrote", path)
    else:
        vote_view(rows).convert("RGB").save(os.path.join(OUT, "chart_look_vote.png"))
        boat_view(rows).convert("RGB").save(os.path.join(OUT, "chart_look_boat.png"))
        print("wrote chart_look_vote.png, chart_look_boat.png in", OUT)
