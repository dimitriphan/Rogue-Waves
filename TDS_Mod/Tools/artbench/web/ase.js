/* ase.js - .aseprite read/write, PNG encode and a STORED .zip writer for the Rogue Waves art bench page.
 * Plain browser script, no build step: defines one global, RWAse.
 * Reference: TDS_Mod/Tools/aseprite/ase_sheets.py (write_ase, read_ase, visible_layers, flatten). writeAse lays out
 * files byte for byte like write_ase (32-bit RGBA, one layer, one cropped zlib cel per non-empty frame, a tags chunk
 * followed by one user data chunk per tag), so the vessel builders' C# merge (YosseleAseprite.cs Read) reads them.
 * Only the zlib streams differ (CompressionStream picks its own level). readAse also reads 16-bit grayscale and
 * 8-bit indexed files, which the Python and C# readers refuse.
 * Format: https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md
 */
globalThis.RWAse = (() => {
  'use strict';
  const enc = new TextEncoder(), dec = new TextDecoder();

  // ---------------------------------------------------------------- CRC-32 and zlib streams
  const CRC = new Uint32Array(256);
  for (let n = 0; n < 256; n++) {
    let c = n;
    for (let k = 0; k < 8; k++) c = c & 1 ? 0xEDB88320 ^ (c >>> 1) : c >>> 1;
    CRC[n] = c >>> 0;
  }
  function crc32(bytes, crc = 0) {
    let c = ~crc >>> 0;
    for (let i = 0; i < bytes.length; i++) c = CRC[(c ^ bytes[i]) & 255] ^ (c >>> 8);
    return ~c >>> 0;
  }
  const frameCrc = rgba => crc32(rgba).toString(16).padStart(8, '0');

  async function pipe(bytes, stream) {
    const out = new Blob([bytes]).stream().pipeThrough(stream);
    return new Uint8Array(await new Response(out).arrayBuffer());
  }
  const deflate = bytes => pipe(bytes, new CompressionStream('deflate'));
  const inflate = bytes => pipe(bytes, new DecompressionStream('deflate'));

  // ---------------------------------------------------------------- little byte writer
  class Out {
    constructor() { this.parts = []; this.length = 0; }
    raw(b) { this.parts.push(b); this.length += b.length; return this; }
    num(n, size, be) {
      const b = new Uint8Array(size), v = new DataView(b.buffer);
      if (size === 1) v.setUint8(0, n);
      else if (size === 2) v.setUint16(0, n & 0xFFFF, !be);
      else v.setUint32(0, n >>> 0, !be);
      return this.raw(b);
    }
    u8(...ns) { ns.forEach(n => this.num(n, 1)); return this; }
    u16(...ns) { ns.forEach(n => this.num(n, 2)); return this; }
    u32(...ns) { ns.forEach(n => this.num(n, 4)); return this; }
    be32(n) { return this.num(n, 4, true); }
    zero(n) { return this.raw(new Uint8Array(n)); }
    str(s) { const b = enc.encode(s); return this.u16(b.length).raw(b); }
    bytes() {
      const r = new Uint8Array(this.length);
      let o = 0;
      for (const p of this.parts) { r.set(p, o); o += p.length; }
      return r;
    }
  }

  // ---------------------------------------------------------------- .aseprite writer (= write_ase)
  function bbox(w, h, px) {
    let x0 = w, y0 = h, x1 = -1, y1 = -1;
    for (let y = 0; y < h; y++)
      for (let x = 0; x < w; x++)
        if (px[(y * w + x) * 4 + 3]) {
          if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
        }
    return x1 < 0 ? null : [x0, y0, x1 + 1, y1 + 1];
  }
  function crop(w, px, x, y, cw, ch) {
    const r = new Uint8Array(cw * ch * 4);
    for (let j = 0; j < ch; j++) r.set(px.subarray(((y + j) * w + x) * 4, ((y + j) * w + x + cw) * 4), j * cw * 4);
    return r;
  }
  const chunk = (type, body) => new Out().u32(body.length + 6).u16(type).raw(body).bytes();

  async function writeAse({ w, h, frames, tags = [], palette = [], layerName = 'Layer 1', duration = 100 }) {
    if (!palette.length) palette = [[0, 0, 0, 0]];
    const file = new Out();
    for (let i = 0; i < frames.length; i++) {
      const px = frames[i], ch = [];
      if (i === 0) {
        ch.push(chunk(0x2007, new Out().u16(1, 0).u32(0).zero(8).bytes()));  // sRGB
        const pal = new Out().u32(palette.length, 0, palette.length - 1).zero(8);
        for (const c of palette) pal.u16(0).u8(c[0], c[1], c[2], c[3] ?? 255);
        ch.push(chunk(0x2019, pal.bytes()));
        ch.push(chunk(0x2004, new Out().u16(3, 0, 0, 0, 0, 0).u8(255).zero(3).str(layerName).bytes()));
        if (tags.length) {
          const t = new Out().u16(tags.length).zero(8);
          for (const g of tags) t.u16(g.from, g.to).u8(0).u16(0).zero(6).u8(...g.color, 0).str(g.name);
          ch.push(chunk(0x2018, t.bytes()));
          for (const g of tags)  // one user data chunk per tag, in tag order
            ch.push(chunk(0x2020, new Out().u32(3).str(g.data || '').u8(...g.color, 255).bytes()));
        }
      }
      const bb = bbox(w, h, px);
      if (bb) {
        const [x0, y0, x1, y1] = bb, z = await deflate(crop(w, px, x0, y0, x1 - x0, y1 - y0));
        ch.push(chunk(0x2005, new Out().u16(0, x0, y0).u8(255).u16(2, 0).zero(5).u16(x1 - x0, y1 - y0).raw(z).bytes()));
      }
      const body = new Out();
      ch.forEach(c => body.raw(c));
      file.u32(16 + body.length).u16(0xF1FA, Math.min(ch.length, 0xFFFF), duration).zero(2).u32(ch.length).raw(body.bytes());
    }
    const head = new Out().u32(128 + file.length).u16(0xA5E0, frames.length, w, h, 32).u32(1).u16(duration).u32(0, 0)
      .u8(0).zero(3).u16(palette.length).u8(1, 1).u16(0, 0, 16, 16).zero(84);
    return head.raw(file.bytes()).bytes();
  }

  // ---------------------------------------------------------------- .aseprite reader (= read_ase + flatten)
  async function readAse(bytes) {
    const d = new DataView(bytes.buffer, bytes.byteOffset, bytes.byteLength);
    const u8 = o => d.getUint8(o), u16 = o => d.getUint16(o, true), i16 = o => d.getInt16(o, true);
    const u32 = o => d.getUint32(o, true);
    const str = o => dec.decode(bytes.subarray(o + 2, o + 2 + u16(o)));
    if (u16(4) !== 0xA5E0) throw new Error('not an .aseprite file');
    const nframes = u16(6), w = u16(8), h = u16(10), depth = u16(12), opacityValid = u32(14) & 1;
    const transparent = u8(28);
    if (![8, 16, 32].includes(depth)) throw new Error('unknown colour depth ' + depth);
    const layers = [], cels = [], tags = [], durations = [];
    let palette = null, oldPalette = null, pending = [];
    let pos = 128;
    for (let fi = 0; fi < nframes; fi++) {
      const flen = u32(pos), n = u32(pos + 12) || u16(pos + 6), frame = {};
      durations.push(u16(pos + 8));
      let p = pos + 16;
      for (let k = 0; k < n; k++) {
        const clen = u32(p), type = u16(p + 4), c = p + 6;
        if (type === 0x2004) {
          layers.push({ visible: !!(u16(c) & 1), background: !!(u16(c) & 4), type: u16(c + 2), level: u16(c + 4),
                        blend: u16(c + 10), opacity: opacityValid ? u8(c + 12) : 255, name: str(c + 16) });
          pending = [];
        } else if (type === 0x2005) {
          const li = u16(c), celType = u16(c + 7);
          if (celType === 1) {
            const linked = cels[u16(c + 16)] && cels[u16(c + 16)][li];
            if (linked) frame[li] = linked;
          } else if (celType === 0 || celType === 2) {
            const cw = u16(c + 16), chh = u16(c + 18), data = bytes.subarray(c + 20, p + clen);
            frame[li] = { x: i16(c + 2), y: i16(c + 4), w: cw, h: chh, opacity: u8(c + 6),
                          px: celType === 0 ? data.slice() : await inflate(data) };
          }  // 3 = tilemap cel: skipped
          pending = [];
        } else if (type === 0x2018) {
          let q = c + 10;
          pending = [];
          for (let t = u16(c); t > 0; t--) {
            const tag = { name: str(q + 17), from: u16(q), to: u16(q + 2), color: [u8(q + 13), u8(q + 14), u8(q + 15)],
                          data: '' };
            q += 19 + u16(q + 17);
            tags.push(tag);
            pending.push(tag);
          }
        } else if (type === 0x2020) {
          const text = u32(c) & 1 ? str(c + 4) : '';
          if (pending.length) pending.shift().data = text;
        } else if (type === 0x2019) {
          palette = palette || [];
          let q = c + 20;
          for (let i = u32(c + 4), last = u32(c + 8); i <= last; i++) {
            const fl = u16(q);
            palette[i] = [u8(q + 2), u8(q + 3), u8(q + 4), u8(q + 5)];
            q += 6 + (fl & 1 ? 2 + u16(q + 6) : 0);
          }
          pending = [];  // a user data chunk right after the palette belongs to the sprite
        } else if (type === 0x0004 || type === 0x0011) {
          oldPalette = oldPalette || [];
          let q = c + 2, i = 0;
          for (let pk = u16(c); pk > 0; pk--) {
            i += u8(q);
            const count = u8(q + 1) || 256, scale = type === 0x0011 ? v => (v << 2) | (v >> 4) : v => v;
            q += 2;
            for (let j = 0; j < count; j++, i++, q += 3) oldPalette[i] = [scale(u8(q)), scale(u8(q + 1)), scale(u8(q + 2)), 255];
          }
        } else if (type === 0x2022 || type === 0x2023) {
          pending = [];  // slice or tileset: the next user data is theirs
        }
        p += clen;
      }
      cels.push(frame);
      pos += flen;
    }
    palette = palette || oldPalette || [];
    // Cel pixels -> RGBA
    if (depth !== 32) {
      const done = new Set();
      for (const frame of cels)
        for (const li in frame) {
          const cel = frame[li];
          if (done.has(cel)) continue;
          done.add(cel);
          const n = cel.w * cel.h, src = cel.px, px = new Uint8Array(n * 4), bg = layers[li] && layers[li].background;
          for (let i = 0; i < n; i++) {
            if (depth === 16) px.set([src[i * 2], src[i * 2], src[i * 2], src[i * 2 + 1]], i * 4);
            else if (src[i] !== transparent || bg) px.set(palette[src[i]] || [0, 0, 0, 255], i * 4);
          }
          cel.px = px;
        }
    }
    const ase = { w, h, depth, layers, cels, tags, palette, durations };
    ase.frames = cels.map((_, fi) => flatten(ase, fi));
    return ase;
  }

  function visibleLayers(layers) {
    const out = [];
    let stack = [];
    layers.forEach((l, i) => {
      stack = stack.slice(0, l.level);
      const shown = l.visible && stack.every(Boolean);
      stack.push(shown);
      if (l.type === 0 && shown) out.push(i);
    });
    return out;
  }

  function flatten(ase, fi) {  // normal blend for every layer, layer and cel opacity, integer maths of flatten()
    const { w, h } = ase, img = new Uint8Array(w * h * 4);
    for (const li of visibleLayers(ase.layers)) {
      const cel = ase.cels[fi][li];
      if (!cel) continue;
      const lop = ase.layers[li].opacity, px = cel.px;
      for (let y = 0; y < cel.h; y++)
        for (let x = 0; x < cel.w; x++) {
          const tx = cel.x + x, ty = cel.y + y;
          if (tx < 0 || ty < 0 || tx >= w || ty >= h) continue;
          const s = (y * cel.w + x) * 4, sa = Math.floor(px[s + 3] * cel.opacity * lop / 65025);
          if (sa === 0) continue;
          const t = (ty * w + tx) * 4, da = img[t + 3];
          if (da === 0 || sa === 255) { img.set([px[s], px[s + 1], px[s + 2], sa], t); continue; }
          const oa = sa + Math.floor(da * (255 - sa) / 255);
          for (let k = 0; k < 3; k++)
            img[t + k] = Math.floor((px[s + k] * sa + Math.floor(img[t + k] * da * (255 - sa) / 255)) / oa);
          img[t + 3] = oa;
        }
    }
    return img;
  }

  // ---------------------------------------------------------------- .zip (STORED) and PNG
  function zipStore(files) {
    const body = new Out(), dir = new Out(), now = new Date();
    const time = (now.getHours() << 11) | (now.getMinutes() << 5) | (now.getSeconds() >> 1);
    const date = ((Math.max(now.getFullYear(), 1980) - 1980) << 9) | ((now.getMonth() + 1) << 5) | now.getDate();
    for (const f of files) {
      const name = enc.encode(f.name), crc = crc32(f.bytes), size = f.bytes.length, offset = body.length;
      // version 2.0, flag 0x0800 = UTF-8 names, method 0 = stored
      body.u32(0x04034B50).u16(20, 0x0800, 0, time, date).u32(crc, size, size).u16(name.length, 0).raw(name).raw(f.bytes);
      dir.u32(0x02014B50).u16(20, 20, 0x0800, 0, time, date).u32(crc, size, size).u16(name.length, 0, 0, 0, 0)
        .u32(0, offset).raw(name);
    }
    const end = new Out().u32(0x06054B50).u16(0, 0, files.length, files.length).u32(dir.length, body.length).u16(0);
    return body.raw(dir.bytes()).raw(end.bytes()).bytes();
  }

  async function pngEncode(w, h, rgba) {
    const raw = new Uint8Array(h * (w * 4 + 1));
    for (let y = 0; y < h; y++) raw.set(rgba.subarray(y * w * 4, (y + 1) * w * 4), y * (w * 4 + 1) + 1);  // filter 0
    const png = new Out().raw(new Uint8Array([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]));
    const add = (type, data) => {
      const td = new Out().raw(enc.encode(type)).raw(data).bytes();
      png.be32(data.length).raw(td).be32(crc32(td));
    };
    const ihdr = new Out().be32(w).be32(h).u8(8, 6, 0, 0, 0).bytes();
    add('IHDR', ihdr);
    add('IDAT', await deflate(raw));
    add('IEND', new Uint8Array(0));
    return png.bytes();
  }

  return { crc32, frameCrc, writeAse, readAse, flatten, visibleLayers, zipStore, pngEncode };
})();
