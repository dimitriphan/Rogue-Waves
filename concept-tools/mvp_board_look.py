"""The multiplayer MVP board ("SHIP'S LOG") drawn in the game's own UI look, as the chart view is
(docs/mode/MVP_BOARD.md §5, docs/mode/CHART_NAVIGATION.md §3.6, docs/themes/rogue-tide.md): no panel, the stock title
with its rule and knot, one stock-built card per player with the one title that stands out most for them (an accolade row
marked in Rogue Teal or a failure row in slate),
the MVP in white corner brackets under the view's one Storm Coral banner.

    python3 concept-tools/mvp_board_look.py   -> docs/concepts/art/mvp_board_map.png

A made-up three-player board after map 4 (an odd crew on an even map: one failure, docs/mode/MVP_BOARD.md §4). The vessel icons are the pack's own (Yossele, Spool, Argo); the 12 x 12
title icons are drawn here as string rows, like the chart's. Inter stands in for the game's TMP font.
"""
import os, sys
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sea_chart_look as L
from sea_chart_look import Canvas, brackets, ruled_title, framed, font, pix, stand_in, dim, INK, TEAL, SWELL, CORAL, SAND, DARK

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "docs", "concepts", "art")
SRC = os.path.join(ROOT, "TDS_Mod", "ModSource")
MIST = (0xB4, 0xC8, 0xCF)
WHITE = (0xFF, 0xFF, 0xFF)
SWELL2 = (0x35, 0x54, 0x6D)

L.PAL.update({"l": (0x9C, 0xD3, 0xDB)})   # no Storm Coral in icons: the MVP banner is the one coral piece
L.PIX.update({
    "wave": ["............", ".....llll...", "...llwwwwl..", "..lwwl..lwl.", ".lwl.....wl.", ".lw...l..l..",
             "lwl..lwl....", "lw..lwwwl..l", "lwllwwwwwllw", "lwwwwwwwwwww", "llllllllllll", "............"],
    "hammer": ["............", "..gggggg....", ".gwwwwwwgG..", ".gggggggGG..", "..GGGGGGG...", ".....bB.....",
               ".....bB.....", ".....bB.....", ".....bB.....", ".....bB.....", ".....bb.....", "............"],
    "ring": ["............", "....owwo....", "..ooowwooo..", ".ooo....ooo.", ".oo......oo.", "ww........ww",
             "ww........ww", ".oo......oo.", ".ooo....ooo.", "..ooowwooo..", "....owwo....", "............"],
    "hoard": ["............", "...y.yy.y...", "..yyyyyyyy..", ".bbbbbbbbbb.", ".bBBBBBBBBb.", ".bbbbyybbbb.",
              ".bBBByyBBBb.", ".bBBBBBBBBb.", ".bBBBBBBBBb.", ".bbbbbbbbbb.", "............", "............"],
    "hourglass": ["............", "..bbbbbbbb..", "...g....g...", "...gssssg...", "....gssg....", ".....gg.....",
                  ".....gg.....", "....gssg....", "...gssssg...", "..bbbbbbbb..", "............", "............"],
})


def vessel(path, size):
    im = Image.open(os.path.join(SRC, path)).convert("RGBA")
    box = im.getbbox()
    if box:
        im = im.crop(box)
    k = max(1, size // max(im.size)) if max(im.size) < size else 0
    if k:
        return im.resize((im.width * k, im.height * k), Image.NEAREST)
    return im.resize((size, int(size * im.height / im.width)), Image.NEAREST)


def wrap(c, s, f, width):
    words, lines, cur = s.split(), [], ""
    for w in words:
        t = (cur + " " + w).strip()
        if c.d.textlength(t, font=f) / L.S > width and cur:
            lines.append(cur)
            cur = w
        else:
            cur = t
    return lines + [cur]


PLAYERS = [
    {"name": "Dimitri", "vessel": "Yossele", "icon": "Yossele/Previews/0.1/sprite_Yossele_Icon.png", "you": True,
     "mvp": True,
     "title": (True, "wave", "TIDEBREAKER", "Sent 214 foes to the bottom."),
     "nums": (("214", "SUNK"), ("19", "BUILT"), ("1,240", "AURA"), ("0", "DROWNED"))},
    {"name": "Wren", "vessel": "Spool", "icon": "Spool/Previews/0.1/sprite_Spool_Icon.png",
     "title": (False, "harbour", "ANCHOR", "Spent more time on the seabed than on deck."),
     "nums": (("131", "SUNK"), ("31", "BUILT"), ("980", "AURA"), ("3", "DROWNED"))},
    {"name": "Tobi", "vessel": "Argo", "icon": "Argo/Concept/assets/Hero_Argo_Icon.png",
     "title": (True, "ring", "UNSINKABLE", "Never touched the seabed."),
     "nums": (("158", "SUNK"), ("22", "BUILT"), ("1,105", "AURA"), ("0", "DROWNED"))},
]


def title_tile(c, x, y, w, h, fill, icon, title, line, line_col):
    """A raised Deep Swell row; the icon on a small stock-built square tile in the title's colour (teal for an
    accolade, slate for a failure). Body text stays on Deep Swell: Foam on Rogue Teal is for icons and big headers only."""
    c.rect((x, y, x + w, y + h), SWELL2)
    c.rect((x, y, x + 3, y + h), fill)
    framed(c, (x + 14, y + (h - 48) / 2, x + 62, y + (h + 48) / 2), fill)
    c.stamp(pix(icon, 3), x + 20, y + (h - 36) / 2)
    tx = x + 78
    c.text((tx, y + 14), title, font(15, "SemiBold"), INK, 255, spacing=1.2)
    f = font(13)
    for i, ln in enumerate(wrap(c, line, f, w - 90)):
        c.text((tx, y + 38 + i * 18), ln, f, line_col, 245)


def card(c, x, y, w, h, p):
    framed(c, (x, y, x + w, y + h), SWELL)
    if p.get("mvp"):
        brackets(c, (x - 12, y - 12, x + w + 12, y + h + 12), 255, arm=14, w=2)
        bw = 92
        c.rect((x + w / 2 - bw / 2 - 2, y - 30, x + w / 2 + bw / 2 + 2, y - 2), None, INK, 0, 230, 1)
        c.rect((x + w / 2 - bw / 2, y - 28, x + w / 2 + bw / 2, y - 4), CORAL)
        c.text((x + w / 2, y - 16), "MVP", font(18, "Bold"), WHITE, 255, anchor="mm", spacing=3)
    icon = vessel(p["icon"], 64)
    c.stamp(icon, x + w / 2 - icon.width / 2, y + 26)
    c.text((x + w / 2, y + 104), p["name"], font(20, "SemiBold"), INK, 255, anchor="ma")
    c.text((x + w / 2, y + 132), p["vessel"].upper(), font(12), MIST, 255, anchor="ma", spacing=2)
    if p.get("you"):
        c.rect((x + w / 2 - 34, y + 152, x + w / 2 + 34, y + 154), SAND)
    good, icon, title, line = p["title"]
    title_tile(c, x + 16, y + 170, w - 32, 92, TEAL if good else DARK, icon, title, line, line_col=INK if good else MIST)
    cw = (w - 32) / 4
    for i, (n, lab) in enumerate(p["nums"]):
        cx = x + 16 + cw * i + cw / 2
        c.text((cx, y + 282), n, font(18, "Bold"), INK, 255, anchor="ma")
        c.text((cx, y + 308), lab, font(10), MIST, 255, anchor="ma", spacing=1.2)


def board(background=None):
    W, H = 1600, 900
    base = dim(background if background is not None else stand_in(W, H), 175)
    c = Canvas(W, H)
    cw, ch, gap = 320, 334, 48
    left = (W - (3 * cw + 2 * gap)) / 2
    ruled_title(c, left, 88, "SHIP'S LOG", 30, left + 3 * cw + 2 * gap, 200)
    c.text((W / 2, 150), "MAP 4 WON  •  THE NARROWS", font(13), MIST, 255, anchor="ma", spacing=2)
    for i, p in enumerate(PLAYERS):
        card(c, left + i * (cw + gap), 236, cw, ch, p)
    c.text((W / 2, 602), "Tide 15  •  2 retries left  •  38 foes slipped through  •  Next: Direction picks",
           font(14), INK, 225, anchor="ma")
    bx, by, bw, bh = W / 2 - 90, 642, 180, 46
    framed(c, (bx, by, bx + bw, by + bh), TEAL)
    c.text((W / 2, by + bh / 2), "CONTINUE", font(16, "SemiBold"), INK, 255, anchor="mm", spacing=2)
    c.text((bx + bw + 20, by + bh / 2), "9 s", font(13), MIST, 255, anchor="lm")
    return c.onto(base)


if __name__ == "__main__":
    path = os.path.join(OUT, "mvp_board_map.png")
    board().convert("RGB").save(path)
    print("wrote", path)
