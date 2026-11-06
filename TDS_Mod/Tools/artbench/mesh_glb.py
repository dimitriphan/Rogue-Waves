#!/usr/bin/env python3
"""Rogue Waves' low-poly models to binary glTF (.glb) for Blender, and back.

The cloud preview (TDS_Mod/Tools/cloud-check/lite/run_preview.sh, flag m) writes one mesh JSON per model:
  {"name", "group", "projectile", "tris", ["scale"], "body": {"v","n","uv","t"},
   "parts": [{"name", "pivot", "v","n","uv","t"}], "palette": [64 hex]}
in Unity's coordinates (left-handed, Y up, 1 = one tile, ground at y = -0.5); a part's vertices are relative to its
pivot. Colour is a palette swatch: index = row * 8 + column of the 8x8 point-filtered texture, UV on its centre.

Conversion: Unity (x, y, z) <-> glTF (-x, y, z). Mirroring X turns the left-handed frame right-handed with Y still up
and the model's front still -z; a mirror flips every triangle's winding, so (a, b, c) is written (a, c, b). Normals and
pivots are mirrored the same way. glTF's V runs down from the top of the image, so v_gltf = 1 - v_unity, and the
embedded PNG has palette row r at image row 7 - r.

  python3 mesh_glb.py export <meshes_dir> <out_dir>          every .json -> .glb
  python3 mesh_glb.py import <file.glb> <palette_from.json> <out.json>
  python3 mesh_glb.py check  <file.glb>                       triangles, parts, bounds
"""
import base64, json, math, os, re, struct, sys, zlib

FLOAT, U8, U16, U32 = 5126, 5121, 5123, 5125
NEAREST, CLAMP = 9728, 33071
KNOWN_PARTS = {'Turret', 'Spinner', 'Swell', 'Facing', 'Lens', 'Lamp', 'Pivot', 'Gun'}
CTYPES = {5120: ('b', 1, 127.0), 5121: ('B', 1, 255.0), 5122: ('h', 2, 32767.0), 5123: ('H', 2, 65535.0),
          5125: ('I', 4, 4294967295.0), 5126: ('f', 4, None)}
NCOMP = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}


def hex_rgb(h):
    h = h.lstrip('#')
    return tuple(int(h[i:i + 2], 16) for i in (0, 2, 4))


def lin_to_srgb(c):
    c = min(1.0, max(0.0, c))
    return 255.0 * (12.92 * c if c <= 0.0031308 else 1.055 * c ** (1 / 2.4) - 0.055)


def swatch_uv(i):  # Unity UV of swatch i's centre
    return ((i % 8 + 0.5) / 8, (i // 8 + 0.5) / 8)


def nearest(rgb, pal):
    return min(range(len(pal)), key=lambda i: sum((a - b) ** 2 for a, b in zip(rgb, pal[i])))


# ------------------------------------------------------------ PNG (stdlib; PIL used for reading when present)

def png_bytes(pal_hex):
    rows = b''
    for y in range(8):  # image row y (top first) holds palette row 7 - y
        rows += b'\0' + bytes(c for x in range(8) for c in hex_rgb(pal_hex[(7 - y) * 8 + x]))
    chunk = lambda t, d: struct.pack('>I', len(d)) + t + d + struct.pack('>I', zlib.crc32(t + d) & 0xffffffff)
    return (b'\x89PNG\r\n\x1a\n' + chunk(b'IHDR', struct.pack('>IIBBBBB', 8, 8, 8, 2, 0, 0, 0))
            + chunk(b'IDAT', zlib.compress(rows)) + chunk(b'IEND', b''))


def decode_image(data):
    """-> (w, h, [[(r, g, b)] per row, top first]) or None."""
    try:
        from PIL import Image
        import io
        im = Image.open(io.BytesIO(data)).convert('RGB')
        px = im.load()
        return im.width, im.height, [[px[x, y] for x in range(im.width)] for y in range(im.height)]
    except ImportError:
        pass
    if data[:8] != b'\x89PNG\r\n\x1a\n':
        return None
    pos, idat, hdr = 8, b'', None
    while pos < len(data):
        n, t = struct.unpack('>I4s', data[pos:pos + 8])
        if t == b'IHDR': hdr = struct.unpack('>IIBBBBB', data[pos + 8:pos + 21])
        if t == b'IDAT': idat += data[pos + 8:pos + 8 + n]
        pos += 12 + n
    w, h, depth, ct, _, _, il = hdr
    bpp = {2: 3, 6: 4}.get(ct)
    if depth != 8 or not bpp or il:
        return None
    raw, prev, out, s = zlib.decompress(idat), bytearray(w * bpp), [], w * bpp
    for y in range(h):
        f, line = raw[y * (s + 1)], bytearray(raw[y * (s + 1) + 1:(y + 1) * (s + 1)])
        for i in range(s):
            a = line[i - bpp] if i >= bpp else 0
            b, c = prev[i], prev[i - bpp] if i >= bpp else 0
            p = a + b - c
            pr = a if abs(p - a) <= abs(p - b) and abs(p - a) <= abs(p - c) else b if abs(p - b) <= abs(p - c) else c
            line[i] = (line[i] + (0, a, b, (a + b) // 2, pr)[f]) & 255
        out.append([tuple(line[x * bpp:x * bpp + 3]) for x in range(w)])
        prev = line
    return w, h, out


# ------------------------------------------------------------ export

def face_normals(g):
    """g's normals, with any that is not unit length (Lp writes zero for a sliver whose cross product is under 1e-5,
    as Unity's Normalize does) replaced by its triangle's face normal, Lp's normalize(cross(b - a, c - a)): glTF
    requires unit normals. -> (normals, number repaired)."""
    n, v, t, bad = list(g['n']), g['v'], g['t'], 0
    for i in range(0, len(t), 3):
        ks = [t[i + k] for k in range(3)]
        if all(abs(sum(n[3 * k + c] ** 2 for c in range(3)) - 1) < 1e-3 for k in ks):
            continue
        a, b, c = [v[3 * k:3 * k + 3] for k in ks]
        e1, e2 = [b[j] - a[j] for j in range(3)], [c[j] - a[j] for j in range(3)]
        f = [e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0]]
        ln = math.sqrt(sum(x * x for x in f))
        f = [x / ln for x in f] if ln > 0 else [0.0, 1.0, 0.0]
        for k in ks:
            if abs(sum(n[3 * k + c] ** 2 for c in range(3)) - 1) >= 1e-3:
                n[3 * k:3 * k + 3] = f
                bad += 1
    return n, bad


def to_glb(m):
    """Mesh JSON (dict or str) -> .glb bytes: a node named the model holding the body, a child node per part at its pivot."""
    if isinstance(m, (str, bytes)):
        m = json.loads(m)
    bin_, views, accs = bytearray(), [], []

    def view(data, target=None):
        while len(bin_) % 4: bin_.append(0)
        views.append(dict(buffer=0, byteOffset=len(bin_), byteLength=len(data), **({'target': target} if target else {})))
        bin_.extend(data)
        return len(views) - 1

    def acc(vals, n, typ, ctype=FLOAT, target=34962, minmax=False):
        fmt = {FLOAT: 'f', U16: 'H', U32: 'I'}[ctype]
        a = dict(bufferView=view(struct.pack('<%d%s' % (len(vals), fmt), *vals), target), componentType=ctype,
                 count=len(vals) // n, type=typ)
        if minmax:
            a['min'] = [min(vals[i::n]) for i in range(n)]
            a['max'] = [max(vals[i::n]) for i in range(n)]
        accs.append(a)
        return len(accs) - 1

    def f32(x):  # min/max must equal the stored float32 values exactly
        return struct.unpack('<f', struct.pack('<f', x))[0]

    meshes = []

    def mesh(name, g):
        if not g['t']:
            return None
        v = [f32(-x if i % 3 == 0 else x) for i, x in enumerate(g['v'])]
        n = [-x if i % 3 == 0 else x for i, x in enumerate(face_normals(g)[0])]
        uv = [1.0 - x if i % 2 else x for i, x in enumerate(g['uv'])]
        t = g['t']
        idx = [t[i + k] for i in range(0, len(t), 3) for k in (0, 2, 1)]
        attrs = dict(POSITION=acc(v, 3, 'VEC3', minmax=True), NORMAL=acc(n, 3, 'VEC3'), TEXCOORD_0=acc(uv, 2, 'VEC2'))
        ind = acc(idx, 1, 'SCALAR', U16 if len(v) // 3 < 65536 else U32, 34963)
        meshes.append(dict(name=name, primitives=[dict(attributes=attrs, indices=ind, material=0, mode=4)]))
        return len(meshes) - 1

    extras = {k: m[k] for k in ('group', 'projectile', 'scale') if k in m}
    extras.update(rw_model=True, palette=m['palette'])
    nodes = [dict(name=m['name'], extras=extras)]
    bm = mesh(m['name'], m['body'])
    if bm is not None: nodes[0]['mesh'] = bm
    for p in m.get('parts', []):
        px, py, pz = p['pivot']
        node = dict(name=p['name'], translation=[-px, py, pz], extras={'rw_part': True})
        pm = mesh(p['name'], p)
        if pm is not None: node['mesh'] = pm
        nodes.append(node)
        nodes[0].setdefault('children', []).append(len(nodes) - 1)
    img = view(png_bytes(m['palette']))
    gl = dict(asset=dict(version='2.0', generator='Rogue Waves mesh_glb.py'), scene=0,
              scenes=[dict(name=m['name'], nodes=[0])], nodes=nodes, bufferViews=views,
              materials=[dict(name='RW_Palette', extras={'palette': m['palette']},
                              pbrMetallicRoughness=dict(baseColorTexture=dict(index=0), metallicFactor=0.0, roughnessFactor=1.0))],
              textures=[dict(sampler=0, source=0)], samplers=[dict(magFilter=NEAREST, minFilter=NEAREST, wrapS=CLAMP, wrapT=CLAMP)],
              images=[dict(name='RW_Palette', bufferView=img, mimeType='image/png')])
    if meshes: gl['meshes'] = meshes
    if accs: gl['accessors'] = accs
    while len(bin_) % 4: bin_.append(0)
    gl['buffers'] = [dict(byteLength=len(bin_))]
    js = json.dumps(gl, separators=(',', ':')).encode()
    js += b' ' * (-len(js) % 4)
    out = struct.pack('<4sII', b'glTF', 2, 12 + 8 + len(js) + 8 + len(bin_))
    return out + struct.pack('<I4s', len(js), b'JSON') + js + struct.pack('<I4s', len(bin_), b'BIN\0') + bytes(bin_)


# ------------------------------------------------------------ import

def parse_glb(data):
    magic, ver, total = struct.unpack('<4sII', data[:12])
    if magic != b'glTF' or ver != 2:
        raise ValueError('not a glTF 2.0 binary')
    pos, gl, bin_ = 12, None, None
    while pos < min(total, len(data)):
        n, t = struct.unpack('<I4s', data[pos:pos + 8])
        if t == b'JSON': gl = json.loads(data[pos + 8:pos + 8 + n])
        elif t == b'BIN\0' and bin_ is None: bin_ = data[pos + 8:pos + 8 + n]
        pos += 8 + n
    bufs = []
    for i, b in enumerate(gl.get('buffers', [])):
        uri = b.get('uri')
        if uri is None: bufs.append(bin_)
        elif uri.startswith('data:'): bufs.append(base64.b64decode(uri.split(',', 1)[1]))
        else: raise ValueError('external buffer %r not supported; export as .glb' % uri)
    return gl, bufs


def read_acc(gl, bufs, i):
    """Accessor -> list of tuples (normalized ints become floats), honouring byteStride and sparse."""
    a = gl['accessors'][i]
    fmt, size, norm = CTYPES[a['componentType']]
    n, count = NCOMP[a['type']], a['count']
    out = [[0.0] * n for _ in range(count)]
    if 'bufferView' in a:
        v = gl['bufferViews'][a['bufferView']]
        buf, stride = bufs[v.get('buffer', 0)], v.get('byteStride') or size * n
        base = v.get('byteOffset', 0) + a.get('byteOffset', 0)
        for k in range(count):
            out[k] = list(struct.unpack_from('<%d%s' % (n, fmt), buf, base + k * stride))
    sp = a.get('sparse')
    if sp:
        iv, vv = sp['indices'], sp['values']
        ifmt, isz, _ = CTYPES[iv['componentType']]
        ib = gl['bufferViews'][iv['bufferView']]
        vb = gl['bufferViews'][vv['bufferView']]
        for k in range(sp['count']):
            j = struct.unpack_from('<' + ifmt, bufs[ib.get('buffer', 0)], ib.get('byteOffset', 0) + iv.get('byteOffset', 0) + k * isz)[0]
            out[j] = list(struct.unpack_from('<%d%s' % (n, fmt), bufs[vb.get('buffer', 0)], vb.get('byteOffset', 0) + vv.get('byteOffset', 0) + k * size * n))
    if norm and a.get('normalized'):
        out = [[max(c / norm, -1.0) for c in row] for row in out]
    return [tuple(r) for r in out]


def mat_mul(a, b):  # 4x4, row-major lists
    return [[sum(a[r][k] * b[k][c] for k in range(4)) for c in range(4)] for r in range(4)]


def node_matrix(nd):
    if 'matrix' in nd:
        m = nd['matrix']  # column-major
        return [[m[c * 4 + r] for c in range(4)] for r in range(4)]
    tx, ty, tz = nd.get('translation', (0, 0, 0))
    x, y, z, w = nd.get('rotation', (0, 0, 0, 1))
    sx, sy, sz = nd.get('scale', (1, 1, 1))
    r = [[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)],
         [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
         [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]]
    return [[r[0][0] * sx, r[0][1] * sy, r[0][2] * sz, tx], [r[1][0] * sx, r[1][1] * sy, r[1][2] * sz, ty],
            [r[2][0] * sx, r[2][1] * sy, r[2][2] * sz, tz], [0, 0, 0, 1]]


def det3(a):  # of the upper-left 3x3
    return (a[0][0] * (a[1][1] * a[2][2] - a[1][2] * a[2][1]) - a[0][1] * (a[1][0] * a[2][2] - a[1][2] * a[2][0])
            + a[0][2] * (a[1][0] * a[2][1] - a[1][1] * a[2][0]))


def inverse(m):  # affine 4x4
    a, det = m, det3(m)
    inv = [[(a[(c + 1) % 3][(r + 1) % 3] * a[(c + 2) % 3][(r + 2) % 3] - a[(c + 1) % 3][(r + 2) % 3] * a[(c + 2) % 3][(r + 1) % 3]) / det
            for c in range(3)] for r in range(3)]
    t = [-sum(inv[r][k] * m[k][3] for k in range(3)) for r in range(3)]
    return [inv[0] + [t[0]], inv[1] + [t[1]], inv[2] + [t[2]], [0, 0, 0, 1]]


def apply(m, p):
    return tuple(sum(m[r][k] * p[k] for k in range(3)) + m[r][3] for r in range(3))


def base_name(n):
    return re.sub(r'\.\d{3}$', '', n or '')


def walk(gl):
    """-> [(node index, world matrix, parent chain)] for the default scene (or every root node)."""
    nodes = gl.get('nodes', [])
    if gl.get('scenes'):
        roots = gl['scenes'][gl.get('scene', 0)].get('nodes', [])
    else:
        kids = {c for nd in nodes for c in nd.get('children', [])}
        roots = [i for i in range(len(nodes)) if i not in kids]
    out, stack = [], [(r, [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]], ()) for r in reversed(roots)]
    while stack:
        i, parent, chain = stack.pop()
        w = mat_mul(parent, node_matrix(nodes[i]))
        out.append((i, w, chain))
        stack.extend((c, w, chain + (i,)) for c in reversed(nodes[i].get('children', [])))
    return out


def triangles(gl, bufs, prim):
    """-> list of (i0, i1, i2) in the file's own winding."""
    pos = gl['accessors'][prim['attributes']['POSITION']]['count']
    idx = [r[0] for r in read_acc(gl, bufs, prim['indices'])] if 'indices' in prim else list(range(pos))
    mode = prim.get('mode', 4)
    if mode == 4: return [tuple(idx[i:i + 3]) for i in range(0, len(idx) - 2, 3)]
    if mode == 5: return [(idx[i], idx[i + 1], idx[i + 2]) if i % 2 == 0 else (idx[i + 1], idx[i], idx[i + 2]) for i in range(len(idx) - 2)]
    if mode == 6: return [(idx[0], idx[i], idx[i + 1]) for i in range(1, len(idx) - 1)]
    return []


def material_colour(gl, prim, images, pal_rgb):
    """-> f(uvs of the triangle, colours of its vertices) -> (swatch, source)."""
    mat = gl['materials'][prim['material']] if 'material' in prim and gl.get('materials') else {}
    pbr = mat.get('pbrMetallicRoughness', {})
    factor = pbr.get('baseColorFactor', [1, 1, 1, 1])
    tex = pbr.get('baseColorTexture')
    img = None
    if tex is not None:
        src = gl['textures'][tex['index']].get('source')
        img = images.get(src) if src is not None else None
    if img:
        w, h, px = img
        ours = w == 8 and h == 8 and all(px[7 - i // 8][i % 8] == pal_rgb[i] for i in range(64))

        def by_tex(uvs, cols):
            u, v = sum(t[0] for t in uvs) / 3, sum(t[1] for t in uvs) / 3
            x, y = int(math.floor(u * w)) % w, int(math.floor(v * h)) % h
            if ours and all(abs(f - 1) < 1e-3 for f in factor[:3]):
                return (7 - y) * 8 + x, 'texture'
            return nearest([c * lin_to_srgb(f) / 255 for c, f in zip(px[y][x], factor)], pal_rgb), 'texture'
        return by_tex, tex.get('texCoord', 0)

    def by_colour(uvs, cols):
        if cols:
            lin = [sum(c[k] for c in cols) / 3 * factor[k] for k in range(3)]
            return nearest([lin_to_srgb(c) for c in lin], pal_rgb), 'COLOR_0'
        return nearest([lin_to_srgb(f) for f in factor[:3]], pal_rgb), 'baseColorFactor'
    return by_colour, None


def from_glb(data, palette, part_names=()):
    """.glb bytes -> mesh JSON (Unity coordinates). Each triangle snaps to one swatch; its UVs go to the swatch centre."""
    gl, bufs = parse_glb(data)
    pal_rgb = [hex_rgb(h) for h in palette]
    images = {}
    for i, im in enumerate(gl.get('images', [])):
        if 'bufferView' in im:
            v = gl['bufferViews'][im['bufferView']]
            raw = bufs[v.get('buffer', 0)][v.get('byteOffset', 0):v.get('byteOffset', 0) + v['byteLength']]
        elif im.get('uri', '').startswith('data:'):
            raw = base64.b64decode(im['uri'].split(',', 1)[1])
        else:
            continue
        images[i] = decode_image(bytes(raw))
    nodes = gl.get('nodes', [])
    names = KNOWN_PARTS | set(part_names)
    is_part = lambda i: base_name(nodes[i].get('name')) in names or nodes[i].get('extras', {}).get('rw_part')
    flat = walk(gl)
    world = {i: w for i, w, _ in flat}
    # the model node: one marked by our extras, else the top-most non-part node with a mesh or a part below it, else
    # the top-most non-part node
    model = next((i for i, _, _ in flat if nodes[i].get('extras', {}).get('rw_model')), None)
    if model is None:
        cand = [(len(ch), i) for i, _, ch in flat if not is_part(i) and not any(is_part(c) for c in ch)
                and ('mesh' in nodes[i] or any(is_part(c) for c in nodes[i].get('children', [])))]
        cand = cand or [(len(ch), i) for i, _, ch in flat if not is_part(i) and not any(is_part(c) for c in ch)]
        model = min(cand)[1] if cand else None
    to_model = inverse(world[model]) if model is not None else [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]]
    ex = nodes[model].get('extras', {}) if model is not None else {}
    out = dict(name=base_name(nodes[model]['name']) if model is not None else 'Imported', group=ex.get('group', ''),
               projectile=ex.get('projectile', False), tris=0)
    if 'scale' in ex: out['scale'] = ex['scale']
    groups, order = {}, []  # part node (None = body) -> geometry
    report = dict(primitives=0, sources={}, skipped_primitives=0, extras='group' in ex)  # Blender drops extras by default
    for i, w, chain in flat:
        nd = nodes[i]
        if 'mesh' not in nd:
            continue
        owner = next((c for c in reversed(chain + (i,)) if is_part(c)), None)
        if owner not in groups:
            groups[owner] = dict(v=[], n=[], uv=[], t=[])
            order.append(owner)
        g = groups[owner]
        m = mat_mul(to_model, w)
        piv = apply(mat_mul(to_model, world[owner]), (0, 0, 0)) if owner is not None else (0, 0, 0)
        det = det3(m)
        ninv = inverse(m) if abs(det) > 1e-12 else None  # normals go through the inverse transpose
        for prim in gl['meshes'][nd['mesh']]['primitives']:
            if prim.get('mode', 4) not in (4, 5, 6) or 'POSITION' not in prim['attributes']:
                report['skipped_primitives'] += 1
                continue
            report['primitives'] += 1
            P = read_acc(gl, bufs, prim['attributes']['POSITION'])
            pick, tc = material_colour(gl, prim, images, pal_rgb)
            UV = read_acc(gl, bufs, prim['attributes']['TEXCOORD_%d' % tc]) if tc is not None and 'TEXCOORD_%d' % tc in prim['attributes'] else None
            C = read_acc(gl, bufs, prim['attributes']['COLOR_0']) if 'COLOR_0' in prim['attributes'] else None
            N = read_acc(gl, bufs, prim['attributes']['NORMAL']) if 'NORMAL' in prim['attributes'] and ninv else None
            for tri in triangles(gl, bufs, prim):
                if det < 0: tri = (tri[0], tri[2], tri[1])
                sw, src = pick([UV[k] for k in tri] if UV else [(0, 0)] * 3, [C[k] for k in tri] if C else None)
                report['sources'][src] = report['sources'].get(src, 0) + 1
                # glTF -> Unity: mirror X and flip the winding back
                tri = (tri[0], tri[2], tri[1])
                q = [apply(m, P[k]) for k in tri]
                q = [(piv[0] - p[0] + 0.0, p[1] - piv[1], p[2] - piv[2]) for p in q]
                if N:  # the file's normals (Blender splits flat faces too), else the face's own, as Lp makes them
                    ns = [[sum(ninv[k][r] * N[j][k] for k in range(3)) for r in range(3)] for j in tri]
                    ns = [(-a, b, c) for a, b, c in ns]
                else:
                    e1, e2 = [q[1][k] - q[0][k] for k in range(3)], [q[2][k] - q[0][k] for k in range(3)]
                    ns = [(e1[1] * e2[2] - e1[2] * e2[1], e1[2] * e2[0] - e1[0] * e2[2], e1[0] * e2[1] - e1[1] * e2[0])] * 3
                s = len(g['v']) // 3
                for p, nv in zip(q, ns):
                    ln = math.sqrt(sum(c * c for c in nv)) or 1.0
                    g['v'].extend(p)
                    g['n'].extend(c / ln + 0.0 for c in nv)
                    g['uv'].extend(swatch_uv(sw))
                g['t'].extend((s, s + 1, s + 2))
    out['body'] = groups.get(None, dict(v=[], n=[], uv=[], t=[]))
    out['parts'] = []
    for owner in order:
        if owner is None: continue
        p = apply(mat_mul(to_model, world[owner]), (0, 0, 0))
        out['parts'].append(dict(name=base_name(nodes[owner]['name']), pivot=[0.0 - p[0], p[1], p[2]], **groups[owner]))
    out['tris'] = (len(out['body']['t']) + sum(len(p['t']) for p in out['parts'])) // 3
    out['palette'] = list(palette)
    report.update(body_tris=len(out['body']['t']) // 3, parts={p['name']: len(p['t']) // 3 for p in out['parts']})
    out['report'] = report
    return out


# ------------------------------------------------------------ CLI

def bounds(v):
    if not v: return None
    return [min(v[k::3]) for k in range(3)], [max(v[k::3]) for k in range(3)]


def check(data):
    gl, _ = parse_glb(data)
    pal = next((m.get('extras', {}).get('palette') for m in gl.get('materials', []) if m.get('extras', {}).get('palette')), None)
    pal = pal or next((n.get('extras', {}).get('palette') for n in gl.get('nodes', []) if n.get('extras', {}).get('palette')), None)
    m = from_glb(data, pal or ['000000'] * 64)
    allv = list(m['body']['v'])
    for p in m['parts']:
        allv += [c + p['pivot'][i % 3] for i, c in enumerate(p['v'])]
    b = bounds(allv)
    print('%s: %d triangles (body %d), parts %s, primitives %d, colour from %s%s' % (
        m['name'], m['tris'], m['report']['body_tris'], m['report']['parts'] or 'none', m['report']['primitives'],
        m['report']['sources'] or '-', '' if pal else ' (no palette in the file; swatches not meaningful)'))
    for p in m['parts']:
        print('  part %s pivot (Unity) %s' % (p['name'], ['%.4f' % c for c in p['pivot']]))
    if b: print('  bounds (Unity) min %s max %s' % (['%.3f' % c for c in b[0]], ['%.3f' % c for c in b[1]]))
    return m


def main(a):
    if len(a) == 3 and a[0] == 'export':
        os.makedirs(a[2], exist_ok=True)
        n = 0
        for f in sorted(os.listdir(a[1])):
            if f.endswith('.json'):
                with open(os.path.join(a[1], f)) as fh:
                    src = json.load(fh)
                glb = to_glb(src)
                fixed = sum(face_normals(g)[1] for g in [src['body']] + src.get('parts', []))
                if fixed: print('  %s: %d zero-length normal(s) written as face normals' % (src['name'], fixed))
                with open(os.path.join(a[2], f[:-5] + '.glb'), 'wb') as fh:
                    fh.write(glb)
                n += 1
        print('exported %d model(s) to %s' % (n, a[2]))
    elif len(a) == 4 and a[0] == 'import':
        with open(a[2]) as fh:
            src = json.load(fh)
        with open(a[1], 'rb') as fh:
            m = from_glb(fh.read(), src['palette'], [p['name'] for p in src.get('parts', [])])
        r = m.pop('report')
        if not r['extras']:  # keep the source's group, projectile and scale
            m.update({k: src[k] for k in ('group', 'projectile', 'scale') if k in src})
        with open(a[3], 'w') as fh:
            json.dump(m, fh, separators=(',', ':'))
        print('%s: %d triangles (body %d), parts %s, colour from %s, primitives %d%s' % (
            m['name'], m['tris'], r['body_tris'], r['parts'] or 'none', r['sources'], r['primitives'],
            ', %d non-triangle primitive(s) skipped' % r['skipped_primitives'] if r['skipped_primitives'] else ''))
    elif len(a) == 2 and a[0] == 'check':
        with open(a[1], 'rb') as fh:
            check(fh.read())
    else:
        sys.exit(__doc__)


if __name__ == '__main__':
    main(sys.argv[1:])
