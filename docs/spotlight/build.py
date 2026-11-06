"""Build the Rogue Waves Spotlight page.

Reads `spotlight.src.html` (the page, with `{{img:<key>}}` placeholders) and the repository's own art, and writes
`spotlight.html`, one self-contained file with every picture inlined as a data URI. Every picture comes from the
repository (the pack's icons and sprites, the Uloe art, the Yossele sheet, model previews, the sea chart view and
one screenshot of Argo); nothing comes from the project files or the base game.

    python3 docs/spotlight/build.py            # from the repository root
"""
import base64
import io
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", ".."))
SRC = os.path.join(ROOT, "TDS_Mod", "ModSource")
ICONS = os.path.join(SRC, "RogueWavesMode", "Art", "Temporary", "game_size")
MODEL_BG = (219, 214, 204)  # the model previews' flat backdrop


def path(*parts):
    return os.path.join(ROOT, *parts)


def png(im):
    buf = io.BytesIO()
    im.save(buf, "PNG", optimize=True)
    return "data:image/png;base64," + base64.b64encode(buf.getvalue()).decode()


def key_out(im, colours, tol=0):
    im = im.convert("RGBA")
    px = im.load()
    for y in range(im.height):
        for x in range(im.width):
            r, g, b, a = px[x, y]
            for c in colours:
                if abs(r - c[0]) <= tol and abs(g - c[1]) <= tol and abs(b - c[2]) <= tol:
                    px[x, y] = (0, 0, 0, 0)
                    break
    return im


def model_cell(rel, col, row, size=256, label=22):
    im = Image.open(path(rel)).convert("RGBA")
    x, y = col * size, row * size
    cell = im.crop((x, y + label, x + size, y + size))
    return key_out(cell, [MODEL_BG], tol=3)


def images():
    out = {}
    # Vessels
    sheet = Image.open(os.path.join(SRC, "Yossele", "Art", "Yossele-Sheet.png")).convert("RGBA")
    out["yossele_idle"] = png(sheet.crop((0, 0, 192, 32)))  # idle, front right: six 32 px cells
    out["spool"] = png(Image.open(os.path.join(
        SRC, "Spool", "Art", "Temporary", "pastel_green_2026-11-05", "game_size", "Spool_Pastel.png")).convert("RGBA"))
    uloe = key_out(Image.open(path("docs", "concepts", "art", "uloe_8x.png")), [(214, 218, 224), (232, 234, 238)])
    out["uloe"] = png(uloe.resize((32, 32), Image.NEAREST))
    out["argo"] = png(Image.open(os.path.join(SRC, "Argo", "Previews", "round5", "close_argo.png"))
                      .convert("RGB").crop((392, 128, 588, 372)))
    # One tower each, from the model previews
    out["tower_yossele"] = png(model_cell("TDS_Mod/ModSource/Yossele/Previews/0.2/models_Dandelion.png", 3, 0))
    out["tower_spool"] = png(model_cell("TDS_Mod/ModSource/Spool/Previews/0.1/models_Tin.png", 3, 0))
    out["tower_uloe"] = png(model_cell("TDS_Mod/ModSource/Uloe/Previews/0.1/models_Glass.png", 1, 0))
    out["tower_argo"] = png(model_cell("TDS_Mod/ModSource/Argo/Previews/2.0/harpoon.png", 0, 2))
    # The mode
    out["vendcint"] = png(Image.open(os.path.join(ICONS, "Vendcint-Sheet.png")).convert("RGBA").crop((0, 0, 192, 32)))
    chart = Image.open(path("docs", "concepts", "art", "chart_look_vote.png")).convert("RGB")
    out["chart"] = png(chart.resize((1120, 630), Image.LANCZOS))  # the chart view at a vote (CHART_NAVIGATION.md)
    for name in os.listdir(ICONS):
        stem, ext = os.path.splitext(name)
        if ext == ".png" and stem != "Vendcint-Sheet":
            out["icon_" + stem] = png(Image.open(os.path.join(ICONS, name)).convert("RGBA"))
    return out


def main():
    src = open(os.path.join(HERE, "spotlight.src.html"), encoding="utf-8").read()
    imgs = images()
    missing = []

    def sub(text):
        res, i = [], 0
        while True:
            j = text.find("{{img:", i)
            if j < 0:
                res.append(text[i:])
                return "".join(res)
            k = text.find("}}", j)
            key = text[j + 6:k]
            res.append(text[i:j])
            if key in imgs:
                res.append(imgs[key])
            else:
                missing.append(key)
            i = k + 2

    html = sub(src)
    if missing:
        sys.exit("missing pictures: " + ", ".join(sorted(set(missing))))
    dst = os.path.join(HERE, "spotlight.html")
    open(dst, "w", encoding="utf-8").write(html)
    print(f"wrote {os.path.relpath(dst, ROOT)}, {len(html) // 1024} KB")


if __name__ == "__main__":
    main()
