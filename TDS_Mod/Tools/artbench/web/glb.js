// RWGlb: reads a .glb (ours from mesh_glb.py, or Blender's export of it) into flat triangles in Unity coordinates, with
// one palette swatch per triangle, by the same rules as mesh_glb.py from_glb: glTF (x, y, z) -> Unity (-x, y, z) with
// the winding flipped back; the model node (our extras, else the top-most non-part node holding a mesh or a part) is the
// origin; parts are nodes named Turret, Spinner, ... (Blender's ".001" suffix ignored). Colour: our 8x8 palette texture
// -> the swatch under the triangle's UV; another texture -> its pixel x baseColorFactor, snapped; no texture ->
// COLOR_0 x baseColorFactor, else baseColorFactor, snapped (linear -> sRGB first).
//   RWGlb.parse(arrayBuffer, palette[, decoded])  sync; a texture that is not 8x8 needs decoded[image] = {w, h, px}
//   RWGlb.parseAsync(arrayBuffer, palette)        decodes the textures with createImageBitmap first (browsers)
// -> {name, tris, parts: [names], triangles: Float32Array 9 per tri, colours: Uint8Array, partOf: Int8Array (-1 body),
//     bounds: {min, max}, sources: {texture|COLOR_0|baseColorFactor: n}}
(function (root) {
  'use strict';
  var KNOWN = ['Turret', 'Spinner', 'Swell', 'Facing', 'Lens', 'Lamp', 'Pivot', 'Gun'];
  var CT = { 5120: ['getInt8', 1, 127], 5121: ['getUint8', 1, 255], 5122: ['getInt16', 2, 32767], 5123: ['getUint16', 2, 65535],
    5125: ['getUint32', 4, 4294967295], 5126: ['getFloat32', 4, 0] };
  var NC = { SCALAR: 1, VEC2: 2, VEC3: 3, VEC4: 4, MAT4: 16 };

  function hexRgb(h) { h = h.replace('#', ''); return [0, 2, 4].map(function (i) { return parseInt(h.substr(i, 2), 16); }); }
  function linToSrgb(c) { c = Math.min(1, Math.max(0, c)); return 255 * (c <= 0.0031308 ? 12.92 * c : 1.055 * Math.pow(c, 1 / 2.4) - 0.055); }
  function nearest(rgb, pal) {
    var best = 0, bd = Infinity;
    for (var i = 0; i < pal.length; i++) {
      var d = 0; for (var k = 0; k < 3; k++) d += (rgb[k] - pal[i][k]) * (rgb[k] - pal[i][k]);
      if (d < bd) { bd = d; best = i; }
    }
    return best;
  }
  function baseName(n) { return (n || '').replace(/\.\d{3}$/, ''); }

  function parseGlb(buf) {
    var dv = new DataView(buf), gl = null, bin = null, pos = 12;
    if (dv.getUint32(0, true) !== 0x46546C67 || dv.getUint32(4, true) !== 2) throw new Error('not a glTF 2.0 binary');
    var total = Math.min(dv.getUint32(8, true), buf.byteLength);
    while (pos < total) {
      var n = dv.getUint32(pos, true), t = dv.getUint32(pos + 4, true);
      if (t === 0x4E4F534A) gl = JSON.parse(new TextDecoder().decode(new Uint8Array(buf, pos + 8, n)));
      else if (t === 0x004E4942 && !bin) bin = new DataView(buf, pos + 8, n);
      pos += 8 + n;
    }
    var bufs = (gl.buffers || []).map(function (b) {
      if (b.uri === undefined) return bin;
      if (b.uri.indexOf('data:') === 0) { var s = atob(b.uri.split(',')[1]), u = new Uint8Array(s.length); for (var i = 0; i < s.length; i++) u[i] = s.charCodeAt(i); return new DataView(u.buffer); }
      throw new Error('external buffer ' + b.uri + ' not supported; export as .glb');
    });
    return { gl: gl, bufs: bufs };
  }

  function readAcc(g, i) {
    var gl = g.gl, a = gl.accessors[i], ct = CT[a.componentType], n = NC[a.type], out = [], k, c;
    for (k = 0; k < a.count; k++) out.push(new Array(n).fill(0));
    function fill(view, off, idx) {
      var dv = g.bufs[view.buffer || 0], base = (view.byteOffset || 0) + off, stride = view.byteStride || ct[1] * n;
      for (k = 0; k < idx.length; k++) for (c = 0; c < n; c++) out[idx[k]][c] = dv[ct[0]](base + k * stride + c * ct[1], true);
    }
    if (a.bufferView !== undefined) fill(gl.bufferViews[a.bufferView], a.byteOffset || 0, out.map(function (_, j) { return j; }));
    if (a.sparse) {
      var si = a.sparse.indices, sv = a.sparse.values, ict = CT[si.componentType], iv = gl.bufferViews[si.bufferView], idx = [];
      for (k = 0; k < a.sparse.count; k++) idx.push(g.bufs[iv.buffer || 0][ict[0]]((iv.byteOffset || 0) + (si.byteOffset || 0) + k * ict[1], true));
      var vv = gl.bufferViews[sv.bufferView], save = vv.byteStride; vv.byteStride = 0; fill(vv, sv.byteOffset || 0, idx); vv.byteStride = save;
    }
    if (ct[2] && a.normalized) out = out.map(function (r) { return r.map(function (x) { return Math.max(x / ct[2], -1); }); });
    return out;
  }

  // 4x4 row-major helpers
  var I4 = [[1, 0, 0, 0], [0, 1, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]];
  function mul(a, b) { return a.map(function (r) { return [0, 1, 2, 3].map(function (c) { return r[0] * b[0][c] + r[1] * b[1][c] + r[2] * b[2][c] + r[3] * b[3][c]; }); }); }
  function nodeMatrix(nd) {
    if (nd.matrix) { var m = nd.matrix; return [0, 1, 2, 3].map(function (r) { return [0, 1, 2, 3].map(function (c) { return m[c * 4 + r]; }); }); }
    var t = nd.translation || [0, 0, 0], q = nd.rotation || [0, 0, 0, 1], s = nd.scale || [1, 1, 1], x = q[0], y = q[1], z = q[2], w = q[3];
    var r = [[1 - 2 * (y * y + z * z), 2 * (x * y - z * w), 2 * (x * z + y * w)], [2 * (x * y + z * w), 1 - 2 * (x * x + z * z), 2 * (y * z - x * w)],
      [2 * (x * z - y * w), 2 * (y * z + x * w), 1 - 2 * (x * x + y * y)]];
    return [0, 1, 2].map(function (i) { return [r[i][0] * s[0], r[i][1] * s[1], r[i][2] * s[2], t[i]]; }).concat([[0, 0, 0, 1]]);
  }
  function det3(m) { return m[0][0] * (m[1][1] * m[2][2] - m[1][2] * m[2][1]) - m[0][1] * (m[1][0] * m[2][2] - m[1][2] * m[2][0]) + m[0][2] * (m[1][0] * m[2][1] - m[1][1] * m[2][0]); }
  function inverse(m) {
    var d = det3(m), inv = [0, 1, 2].map(function (r) { return [0, 1, 2].map(function (c) {
      return (m[(c + 1) % 3][(r + 1) % 3] * m[(c + 2) % 3][(r + 2) % 3] - m[(c + 1) % 3][(r + 2) % 3] * m[(c + 2) % 3][(r + 1) % 3]) / d; }); });
    return inv.map(function (row, r) { return row.concat([-(row[0] * m[0][3] + row[1] * m[1][3] + row[2] * m[2][3])]); }).concat([[0, 0, 0, 1]]);
  }
  function apply(m, p) { return [0, 1, 2].map(function (r) { return m[r][0] * p[0] + m[r][1] * p[1] + m[r][2] * p[2] + m[r][3]; }); }

  function walk(gl) {
    var nodes = gl.nodes || [], roots;
    if (gl.scenes && gl.scenes.length) roots = gl.scenes[gl.scene || 0].nodes || [];
    else { var kids = {}; nodes.forEach(function (n) { (n.children || []).forEach(function (c) { kids[c] = 1; }); }); roots = nodes.map(function (_, i) { return i; }).filter(function (i) { return !kids[i]; }); }
    var out = [], stack = roots.slice().reverse().map(function (r) { return [r, I4, []]; });
    while (stack.length) {
      var e = stack.pop(), w = mul(e[1], nodeMatrix(nodes[e[0]])), ch = e[2].concat([e[0]]);
      out.push({ i: e[0], w: w, chain: e[2] });
      (nodes[e[0]].children || []).slice().reverse().forEach(function (c) { stack.push([c, w, ch]); });
    }
    return out;
  }

  function triangles(g, prim) {
    var count = g.gl.accessors[prim.attributes.POSITION].count, idx = [], out = [], i;
    if (prim.indices !== undefined) idx = readAcc(g, prim.indices).map(function (r) { return r[0]; }); else for (i = 0; i < count; i++) idx.push(i);
    var mode = prim.mode === undefined ? 4 : prim.mode;
    if (mode === 4) for (i = 0; i + 2 < idx.length; i += 3) out.push([idx[i], idx[i + 1], idx[i + 2]]);
    if (mode === 5) for (i = 0; i + 2 < idx.length; i++) out.push(i % 2 ? [idx[i + 1], idx[i], idx[i + 2]] : [idx[i], idx[i + 1], idx[i + 2]]);
    if (mode === 6) for (i = 1; i + 1 < idx.length; i++) out.push([idx[0], idx[i], idx[i + 1]]);
    return out;
  }

  function pngSize(bytes) { // IHDR width, height
    if (bytes[0] !== 0x89 || bytes[1] !== 0x50) return null;
    var dv = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    return { w: dv.getUint32(16), h: dv.getUint32(20) };
  }
  function imageBytes(g, im) {
    if (im.bufferView !== undefined) { var v = g.gl.bufferViews[im.bufferView], dv = g.bufs[v.buffer || 0]; return new Uint8Array(dv.buffer, dv.byteOffset + (v.byteOffset || 0), v.byteLength); }
    if (im.uri && im.uri.indexOf('data:') === 0) { var s = atob(im.uri.split(',')[1]), u = new Uint8Array(s.length); for (var i = 0; i < s.length; i++) u[i] = s.charCodeAt(i); return u; }
    return null;
  }

  function colourFn(g, prim, decoded, palRgb) {
    var mats = g.gl.materials || [], mat = prim.material !== undefined ? mats[prim.material] || {} : {};
    var pbr = mat.pbrMetallicRoughness || {}, f = pbr.baseColorFactor || [1, 1, 1, 1], tex = pbr.baseColorTexture;
    var white = Math.abs(f[0] - 1) < 1e-3 && Math.abs(f[1] - 1) < 1e-3 && Math.abs(f[2] - 1) < 1e-3;
    if (tex) {
      var src = g.gl.textures[tex.index].source, im = src !== undefined ? g.gl.images[src] : null, bytes = im ? imageBytes(g, im) : null;
      var dec = decoded && decoded[src], size = dec ? { w: dec.w, h: dec.h } : bytes ? pngSize(bytes) : null;
      if (size) {
        var ours;
        if (dec) { ours = dec.w === 8 && dec.h === 8; for (var i = 0; ours && i < 64; i++) { var p = dec.px[7 - (i >> 3)][i & 7]; ours = p[0] === palRgb[i][0] && p[1] === palRgb[i][1] && p[2] === palRgb[i][2]; } }
        else ours = size.w === 8 && size.h === 8; // undecoded: an 8x8 texture is taken to be our palette
        if (ours || dec) return { tc: tex.texCoord || 0, pick: function (uvs) {
          var u = (uvs[0][0] + uvs[1][0] + uvs[2][0]) / 3, v = (uvs[0][1] + uvs[1][1] + uvs[2][1]) / 3;
          var x = ((Math.floor(u * size.w) % size.w) + size.w) % size.w, y = ((Math.floor(v * size.h) % size.h) + size.h) % size.h;
          if (ours && white) return [(7 - y) * 8 + x, 'texture'];
          var px = dec.px[y][x];
          return [nearest([0, 1, 2].map(function (k) { return px[k] * linToSrgb(f[k]) / 255; }), palRgb), 'texture'];
        } };
      }
    }
    return { tc: null, pick: function (uvs, cols) {
      if (cols) return [nearest([0, 1, 2].map(function (k) { return linToSrgb((cols[0][k] + cols[1][k] + cols[2][k]) / 3 * f[k]); }), palRgb), 'COLOR_0'];
      return [nearest([linToSrgb(f[0]), linToSrgb(f[1]), linToSrgb(f[2])], palRgb), 'baseColorFactor'];
    } };
  }

  function parse(buf, palette, decoded) {
    var g = parseGlb(buf), gl = g.gl, nodes = gl.nodes || [], palRgb = palette.map(hexRgb);
    var isPart = function (i) { return KNOWN.indexOf(baseName(nodes[i].name)) >= 0 || !!(nodes[i].extras && nodes[i].extras.rw_part); };
    var flat = walk(gl), world = {}, model = null, cand = [];
    flat.forEach(function (e) { world[e.i] = e.w; if (model === null && nodes[e.i].extras && nodes[e.i].extras.rw_model) model = e.i; });
    var free = function (e) { return !isPart(e.i) && !e.chain.some(isPart); };
    if (model === null) {
      flat.forEach(function (e) { if (free(e) && (nodes[e.i].mesh !== undefined || (nodes[e.i].children || []).some(isPart))) cand.push(e); });
      if (!cand.length) cand = flat.filter(free);
      cand.sort(function (a, b) { return a.chain.length - b.chain.length || a.i - b.i; });
      model = cand.length ? cand[0].i : null;
    }
    var toModel = model !== null ? inverse(world[model]) : I4, parts = [], tri = [], col = [], partOf = [], sources = {};
    flat.forEach(function (e) {
      var nd = nodes[e.i];
      if (nd.mesh === undefined) return;
      var owner = null;
      for (var k = e.chain.length; k >= 0; k--) { var c = k === e.chain.length ? e.i : e.chain[k]; if (isPart(c)) { owner = c; break; } }
      var pi = -1;
      if (owner !== null) { var nm = baseName(nodes[owner].name); pi = parts.indexOf(nm); if (pi < 0) { parts.push(nm); pi = parts.length - 1; } }
      var m = mul(toModel, e.w), flip = det3(m) < 0;
      gl.meshes[nd.mesh].primitives.forEach(function (prim) {
        var mode = prim.mode === undefined ? 4 : prim.mode;
        if ([4, 5, 6].indexOf(mode) < 0 || prim.attributes.POSITION === undefined) return;
        var P = readAcc(g, prim.attributes.POSITION), cf = colourFn(g, prim, decoded, palRgb);
        var uvKey = cf.tc !== null ? 'TEXCOORD_' + cf.tc : null, UV = uvKey && prim.attributes[uvKey] !== undefined ? readAcc(g, prim.attributes[uvKey]) : null;
        var C = prim.attributes.COLOR_0 !== undefined ? readAcc(g, prim.attributes.COLOR_0) : null;
        triangles(g, prim).forEach(function (t) {
          if (flip) t = [t[0], t[2], t[1]];
          var r = cf.pick(UV ? t.map(function (k) { return UV[k]; }) : [[0, 0], [0, 0], [0, 0]], C ? t.map(function (k) { return C[k]; }) : null);
          sources[r[1]] = (sources[r[1]] || 0) + 1;
          [t[0], t[2], t[1]].forEach(function (k) { var p = apply(m, P[k]); tri.push(0 - p[0], p[1], p[2]); });
          col.push(r[0]); partOf.push(pi);
        });
      });
    });
    var mn = [Infinity, Infinity, Infinity], mx = [-Infinity, -Infinity, -Infinity];
    for (var i = 0; i < tri.length; i++) { mn[i % 3] = Math.min(mn[i % 3], tri[i]); mx[i % 3] = Math.max(mx[i % 3], tri[i]); }
    return { name: model !== null ? baseName(nodes[model].name) : 'Imported', tris: col.length, parts: parts, triangles: new Float32Array(tri),
      colours: new Uint8Array(col), partOf: new Int8Array(partOf), bounds: col.length ? { min: mn, max: mx } : null, sources: sources };
  }

  function parseAsync(buf, palette) {
    var g = parseGlb(buf), decoded = {};
    var jobs = (g.gl.images || []).map(function (im, i) {
      var bytes = imageBytes(g, im);
      if (!bytes || typeof createImageBitmap === 'undefined') return Promise.resolve();
      return createImageBitmap(new Blob([bytes], { type: im.mimeType || 'image/png' })).then(function (bmp) {
        var cv = typeof OffscreenCanvas !== 'undefined' ? new OffscreenCanvas(bmp.width, bmp.height) : Object.assign(document.createElement('canvas'), { width: bmp.width, height: bmp.height });
        var cx = cv.getContext('2d'); cx.drawImage(bmp, 0, 0);
        var d = cx.getImageData(0, 0, bmp.width, bmp.height).data, px = [];
        for (var y = 0; y < bmp.height; y++) { var row = []; for (var x = 0; x < bmp.width; x++) { var o = (y * bmp.width + x) * 4; row.push([d[o], d[o + 1], d[o + 2]]); } px.push(row); }
        decoded[i] = { w: bmp.width, h: bmp.height, px: px };
      }).catch(function () {});
    });
    return Promise.all(jobs).then(function () { return parse(buf, palette, decoded); });
  }

  root.RWGlb = { parse: parse, parseAsync: parseAsync };
})(typeof window !== 'undefined' ? window : globalThis);
