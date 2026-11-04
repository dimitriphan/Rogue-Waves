"""Builds the Argo diver picks page: template.html with every image inlined as a data URI.

    python3 concept-tools/argo_divers/argo_divers.py            # the art first
    python3 concept-tools/argo_divers/build_page.py [out.html]

Default out: /mnt/project-files/concepts/argo_divers_2026-11-04/argo_divers_picks.html (the page is published as an
Artifact from there; it stores Dimitri's pick in its db collection `picks`, doc `argo`).
"""
import base64
import io
import os
import sys

from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import argo_divers as A  # noqa: E402

ART = os.path.join(A.REPO, 'docs', 'concepts', 'argo_divers_2026-11-04', 'art')
BG = (140, 155, 140)
K = 8


def uri(im, fmt='PNG', **kw):
    b = io.BytesIO()
    im.save(b, fmt, **kw)
    return 'data:image/%s;base64,%s' % (fmt.lower(), base64.b64encode(b.getvalue()).decode())


def tile(c, k=K):
    im = Image.frombytes('RGBA', (c.w, c.h), c.rgba()).resize((c.w * k, c.h * k), Image.NEAREST)
    o = Image.new('RGB', im.size, BG)
    o.paste(im, (0, 0), im)
    return o


def idle_gif(frames, k=K):
    ims = [tile(f, k) for f in frames]
    b = io.BytesIO()
    ims[0].save(b, 'GIF', save_all=True, append_images=ims[1:], duration=125, loop=0)
    return 'data:image/gif;base64,' + base64.b64encode(b.getvalue()).decode()


def main():
    out = sys.argv[1] if len(sys.argv) > 1 else '/mnt/project-files/concepts/argo_divers_2026-11-04/argo_divers_picks.html'
    html = open(os.path.join(HERE, 'template.html')).read()
    takes = [('Shipped', A.shipped)] + A.TAKES
    for name, fn in takes:
        idle = [fn(i) for i in range(6)]
        html = html.replace('{{%s_IDLE}}' % name, idle_gif(idle))
        html = html.replace('{{%s_BACK}}' % name, uri(tile(fn(0, True))))
        html = html.replace('{{%s_1X}}' % name, uri(tile(idle[0], 2)))
    one = Image.open(os.path.join(ART, 'argo_divers_lineup_1x_x3.png'))
    html = html.replace('{{LINEUP_1X}}', uri(one))
    os.makedirs(os.path.dirname(out), exist_ok=True)
    open(out, 'w').write(html)
    print(out, len(html) // 1024, 'KB')


if __name__ == '__main__':
    main()
