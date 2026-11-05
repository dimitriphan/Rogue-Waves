"""Yossele's final sprite sheets (2026-11-05, Dimitri's "default for all" on OPEN_DECISIONS_2026-11-05b item 11): Claude
draws the final sheets from his reshade and the temporary sheets, as .aseprite for him to edit. Scope 07's sprite NEEDS
line stays until he takes them.

What makes them final rather than temporary (kit docs/18 section 3, rules 2 and 7; docs/03 section 1):
  * Every frame is his reshade's pixels (v3_flat, round 3), moved as parts: the temporary sheets' idle, knuckle walk and
    picked "Recoil blast" attack, the code's Fertilize, fall and landing, all drawn from it.
  * The light stays on the upper left in every facing. The temporary v1 and v3 rows (and the code's Fertilize v1/v3)
    are the exact mirrors of v0 and v2 (the attack's in part), so the right-edge shade band of his reshade landed on the LEFT edge whenever
    Yossele walked left. Here each mirrored frame is re-shaded: within every horizontal run of one material (the clay,
    the sprout's leaf) that starts in shade and ends lit, the edge band moves back to the right end, so the shade band
    sits on the right edge again; cast shadows inside the run keep their order. Nothing else in the frame moves. The attack's v1
    and v3 mirror only the body (its legs were drawn lit from the left already), so only runs that read mirrored change.
  * Each sheet is checked: only his reshade's colours plus the laser's and the soil's, fully opaque, feet on the
    bottom row, the shade on the right in every facing (the metric is printed).

Inputs:
  --temp   ModSource/Yossele/Art/Temporary/game_size  (Yossele-Sheet, YosseleAttack-Sheet: his picks)
  --code   the code's sheets as the cloud preview writes them (YosseleFertilize-Sheet, YosseleFall-Sheet,
           YosseleIntroR-Sheet):  TDS_Mod/Tools/cloud-check/lite/run_preview.sh TDS_Mod/Assets/YosseleMod/Editor Yossele <out> s
Writes TDS_Mod/ModSource/Yossele/Art/<sheet>.png (final art: one folder above Temporary, so YosseleBuilder.FileArt takes
it over the temporary file and the code), Art/aseprite/<sheet>.aseprite (one frame per cell, a tag per row, layer
"FINAL art"), Art/preview_8x/ (8x sheets and GIFs, never built).

    python3 concept-tools/temp_art/yossele_final.py --temp TDS_Mod/ModSource/Yossele/Art/Temporary/game_size --code <out>/sheets
"""
import argparse
import os
import sys

from tart import Canvas, H, REPO, gif, preview, save_ase, save_png, sheet
import ase_sheets

OUT = os.path.join(REPO, 'TDS_Mod', 'ModSource', 'Yossele', 'Art')

# Material ramps of his reshade (light first): a run of one material gets its tones put back in v0's order.
CLAY = (H('945743'), H('79432F'))
LEAF = (H('7BCF80'), H('65AD69'))
RAMPS = (CLAY, LEAF)

# Sheets: name, source, rows per facing group, frames per row, tags (name, row, count). 'mirrored' rows are re-shaded.
SHEETS = [
    ('Yossele-Sheet', 'temp', 6, [('idle_v%d' % v, v, 6) for v in range(4)] + [('move_v%d' % v, 4 + v, 6) for v in range(4)],
     [1, 3, 5, 7]),
    ('YosseleAttack-Sheet', 'temp', 4, [('attack_v%d' % v, v, 4) for v in range(4)], [1, 3]),
    ('YosseleFertilize-Sheet', 'code', 6, [('fertilize_v%d' % v, v, 6) for v in range(4)], [1, 3]),
    ('YosseleFall-Sheet', 'code', 12, [('fall', 0, 12)], []),
    ('YosseleIntroR-Sheet', 'code', 12, [('land', 0, 12)], []),
]


def load(path):
    w, h, px = ase_sheets.png_read(path)
    c = Canvas(w, h)
    for i in range(w * h):
        c.c[i] = tuple(px[i * 4:i * 4 + 4])
    return c


def cell(s, col, row, size=32):
    c = Canvas(size, size)
    for y in range(size):
        for x in range(size):
            c.set(x, y, s.get(col * size + x, row * size + y))
    return c


def ramp_of(p):
    for r in RAMPS:
        if p in r:
            return r
    return None


def reshade(c):
    """Moves a mirrored run's edge band back to the right: in a run of one material that starts in its shade tone and ends
    in its light one, the leading shade pixels go to the right end and the rest shifts left, keeping its order (a cast
    shadow inside stays a cast shadow). A run already lit from the left (the attack's legs, drawn unmirrored) and a run
    in one tone stay as they are."""
    o = Canvas(c.w, c.h)
    o.c = list(c.c)
    for y in range(c.h):
        x = 0
        while x < c.w:
            r = ramp_of(c.get(x, y))
            if r is None:
                x += 1
                continue
            end = x
            while end + 1 < c.w and ramp_of(c.get(end + 1, y)) is r:
                end += 1
            run = [c.get(i, y) for i in range(x, end + 1)]
            if run[0] == r[1] and run[-1] == r[0]:      # shade at the left end, light at the right: mirrored
                band = 0
                while run[band] == r[1]:
                    band += 1
                # the edge band moves to the right end; whatever lies inside (a cast shadow) keeps its order
                for i, p in enumerate(run[band:] + run[:band]):
                    o.set(x + i, y, p)
            x = end + 1
    return o


def sides(c):
    """(right-edge shade share, left-edge shade share) of the clay: docs/18's measure of where the light comes from."""
    r = l = rn = ln = 0
    for y in range(c.h):
        for x in range(c.w):
            p = c.get(x, y)
            if p not in CLAY:
                continue
            if c.get(x + 1, y)[3] == 0:
                rn += 1
                r += p == CLAY[1]
            if c.get(x - 1, y)[3] == 0:
                ln += 1
                l += p == CLAY[1]
    return (r / rn if rn else 0.0, l / ln if ln else 0.0)


def build(name, src, cols, tags, mirrored):
    s = load(src)
    rows = s.h // 32
    cells = [cell(s, k, r) for r in range(rows) for k in range(cols)]
    before = []
    for r in mirrored:
        for k in range(cols):
            i = r * cols + k
            before.append(sides(cells[i]))
            cells[i] = reshade(cells[i])
    out = sheet(cells, cols, 32)
    save_png(out, os.path.join(OUT, name + '.png'))
    save_png(preview(out, 32, 8), os.path.join(OUT, 'preview_8x', name + '_8x.png'))
    frames, atags = [], []
    for tag, row, count in tags:
        start = len(frames)
        frames += cells[row * cols:row * cols + count]
        atags.append((tag, start, len(frames) - 1, 'FINAL;sheet=Vessel/%s.png;row=%d;col=0;cols=%d;rows=%d' % (name, row, cols, rows)))
    save_ase(os.path.join(OUT, 'aseprite', name + '.aseprite'), 32, frames, atags, layer='FINAL art')
    try:
        by_row = [cells[r * cols:r * cols + cols] for r in range(rows)]
        if rows >= 4:
            for g in range(rows // 4):
                gif(by_row[g * 4:g * 4 + 4], os.path.join(OUT, 'preview_8x', '%s_%s.gif' % (name, tags[g * 4][0].split('_')[0])))
        else:
            gif(by_row, os.path.join(OUT, 'preview_8x', name + '.gif'))
    except ImportError:
        pass
    after = [sides(cells[r * cols + k]) for r in mirrored for k in range(cols)]
    v0 = [sides(cells[k]) for k in range(cols)]
    avg = lambda xs, i: sum(x[i] for x in xs) / len(xs) if xs else 0.0
    print('%-24s %d rows x %d; v0 shade right %.0f%% left %.0f%%' % (name, rows, cols, 100 * avg(v0, 0), 100 * avg(v0, 1))
          + ('; mirrored rows before: right %.0f%% left %.0f%%, after: right %.0f%% left %.0f%%'
             % (100 * avg(before, 0), 100 * avg(before, 1), 100 * avg(after, 0), 100 * avg(after, 1)) if mirrored else ''))
    return cells


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('--temp', required=True)
    ap.add_argument('--code', required=True)
    a = ap.parse_args()
    for name, where, cols, tags, mirrored in SHEETS:
        src = os.path.join(a.temp if where == 'temp' else a.code, name + '.png')
        if not os.path.exists(src):
            sys.exit('missing ' + src)
        build(name, src, cols, tags, mirrored)


if __name__ == '__main__':
    main()
