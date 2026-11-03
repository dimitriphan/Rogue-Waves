"""Temporary animations for Yossele (2026-10-27): the knuckle walk Dimitri asked for on 2026-10-12 goes into the Move rows,
and the mouth-laser attack is redrawn with body motion (a squat as it charges, a lean back as the hole lights, a kick
and a muzzle flash as it fires, an afterglow wisp). Every other frame is the code's own art, unchanged.

Inputs (both made from Dimitri's reshade, not committed here):
  --code  a folder with the code's sheets as the cloud preview writes them:
          TDS_Mod/Tools/cloud-check/lite/run_preview.sh TDS_Mod/Assets/YosseleMod/Editor Yossele <out> s  -> <out>/sheets
  --walk  the walk concept, /mnt/project-files/concepts/walk_2026-10-12/game_size/YosseleWalk-Sheet.png

    python3 concept-tools/temp_art/yossele_anims.py --code <out>/sheets --walk <YosseleWalk-Sheet.png>
Writes Yossele-Sheet.png and YosseleAttack-Sheet.png (+ .aseprite, 8x previews, GIFs) to ModSource/Yossele/Art/Temporary/;
the builder takes them over the code's sheets (YosseleBuilder.FileArt).
"""
import argparse
import os
from tart import gif, Canvas, H, write_sheet, REPO

LIME, PALE, DIM, WHITE = H('99E550'), H('D9F7B5'), H('6ABE30'), H('FFFFFF')
SMOKE = (H('7BCF80'), H('65AD69'))
CUT = 27            # rows above this are the body (head, collar, arms); rows from it are the root legs


def load(path):
    w, h, px = __import__('ase_sheets').png_read(path)
    c = Canvas(w, h)
    for i in range(w * h):
        c.c[i] = tuple(px[i * 4:i * 4 + 4])
    return c


def cell(sheet, col, row, size=32):
    c = Canvas(size, size)
    for y in range(size):
        for x in range(size):
            c.set(x, y, sheet.get(col * size + x, row * size + y))
    return c


def hole(c):
    """The see-through hole: transparent pixels the head closes in on all four sides (rows 12-24)."""
    out = []
    for y in range(12, 25):
        for x in range(1, 31):
            if c.get(x, y)[3]:
                continue
            l = any(c.get(i, y)[3] for i in range(x - 4, x))
            r = any(c.get(i, y)[3] for i in range(x + 1, x + 5))
            u = any(c.get(x, j)[3] for j in range(y - 3, y))
            d = any(c.get(x, j)[3] for j in range(y + 1, y + 4))
            if l and r and u and d:
                out.append((x, y))
    return out


def moved(base, dx, dy, top_dx=0):
    """The body shifted by (dx, dy) over the legs; the sprout (the top four opaque rows) moves top_dx further."""
    c = Canvas(32, 32)
    for y in range(CUT, 32):
        for x in range(32):
            c.set(x, y, base.get(x, y))
    rows = [y for y in range(CUT) if any(base.get(x, y)[3] for x in range(32))]
    top = rows[0] if rows else 0
    for y in range(CUT):
        for x in range(32):
            p = base.get(x, y)
            if p[3]:
                ex = top_dx if y < top + 4 else 0
                if y + dy < CUT or dy > 0:
                    c.set(x + dx + ex, y + dy, p)
    return c


def fill_hole(c, pts, dx, dy, core):
    pts = [(x + dx, y + dy) for x, y in pts]
    if not pts:
        return None
    cx = sum(x for x, _ in pts) / len(pts)
    cy = sum(y for _, y in pts) / len(pts)
    for x, y in pts:
        edge = ((x + 1, y) not in pts or (x, y + 1) not in pts)
        c.set(x, y, DIM if core == 'dim' else (LIME if edge or core == 'lime' else WHITE))
    if core == 'white':
        c.set(int(cx), int(cy), WHITE)
    return cx, cy


def attack(base, v):
    front = v in (0, 1)
    d = 1 if v in (0, 2) else -1
    pts = hole(base)
    frames = []
    # 0: charge, a squat, the hole lit dim
    f = moved(base, 0, 1)
    fill_hole(f, pts, 0, 1, 'dim')
    frames.append(f)
    # 1: aim, a lean back, the hole bright, two sparks drawn in
    f = moved(base, -d, 0, -d)
    c = fill_hole(f, pts, -d, 0, 'lime')
    if c:
        for sx, sy in ((d * 5, -3), (d * 6, 2)):
            f.set(int(c[0] + sx), int(c[1] + sy), PALE)
    frames.append(f)
    # 2: fire, kicked back, the sprout flung, a white core and a flash out of the hole toward the heading
    f = moved(base, -d, 0, -2 * d)
    c = fill_hole(f, pts, -d, 0, 'white')
    if c:
        ox, oy = int(round(c[0])), int(round(c[1]))
        if front:
            fx = ox + 4 * d
            for k, col in ((0, WHITE), (1, PALE), (2, LIME)):
                for px, py in ((fx + k * d, oy), (fx, oy - k), (fx, oy + k)):
                    f.set(px, py, col)
            for px, py in ((fx + 2 * d, oy - 2), (fx + 2 * d, oy + 2), (fx + 4 * d, oy), (fx - d, oy - 3), (fx - d, oy + 3)):
                f.set(px, py, LIME)
            for x in range(min(ox, fx), max(ox, fx) + 1):
                if not f.get(x, oy)[3] or f.get(x, oy) in (LIME, DIM):
                    f.set(x, oy, PALE)
        else:
            for px, py in ((ox + 6 * d, oy - 1), (ox + 7 * d, oy + 1), (ox - 6 * d, oy)):
                f.set(px, py, LIME)
    frames.append(f)
    # 3: recover, the hole fading, a wisp rising off the head
    f = moved(base, 0, 0, d)
    c = fill_hole(f, pts, 0, 0, 'dim')
    rows = [y for y in range(CUT) if any(base.get(x, y)[3] for x in range(32))]
    if c and rows:
        top = rows[0]
        for i, (sx, sy) in enumerate(((-d, -1), (-2 * d, -3), (-d, -4))):
            f.set(int(c[0]) + sx, top + sy + 2, SMOKE[i % 2])
    frames.append(f)
    return frames


def build(code, walk, out=None):
    out = out or os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Yossele', 'Art', 'Temporary')
    main = load(os.path.join(code, 'Yossele-Sheet.png'))
    wk = load(walk)
    assert (main.w, main.h) == (192, 256) and (wk.w, wk.h) == (192, 128), 'unexpected sheet sizes'
    cells = [cell(main, c, r) for r in range(4) for c in range(6)] + [cell(wk, c, r) for r in range(4) for c in range(6)]
    tags = [('idle_v%d' % v, v, 0, 6) for v in range(4)] + [('move_v%d' % v, 4 + v, 0, 6) for v in range(4)]
    write_sheet(out, 'Yossele-Sheet', cells, 6, 32, tags)
    atk = []
    for v in range(4):
        atk += attack(cells[v * 6], v)
    write_sheet(out, 'YosseleAttack-Sheet', atk, 4, 32, [('attack_v%d' % v, v, 0, 4) for v in range(4)])
    gdir = os.path.join(out, 'preview_8x')
    gif([cells[v * 6:(v + 1) * 6] for v in range(4)], os.path.join(gdir, 'Yossele_idle.gif'), ms=125)
    gif([cells[24 + v * 6:24 + (v + 1) * 6] for v in range(4)], os.path.join(gdir, 'Yossele_move.gif'), ms=110)
    gif([atk[v * 4:(v + 1) * 4] for v in range(4)], os.path.join(gdir, 'Yossele_attack.gif'), ms=110)


if __name__ == '__main__':
    a = argparse.ArgumentParser()
    a.add_argument('--code', required=True)
    a.add_argument('--walk', required=True)
    o = a.parse_args()
    build(o.code, o.walk)
    print('Yossele-Sheet and YosseleAttack-Sheet written')
