// Tests for ase.js against the Python reference (TDS_Mod/Tools/aseprite/ase_sheets.py). Node 22+, python3.
//   node test_ase.mjs            (scratch files go to $ARTBENCH_TMP, else a new folder in the OS temp dir)
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import vm from 'node:vm';
import zlib from 'node:zlib';
import { execFileSync, spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const here = path.dirname(fileURLToPath(import.meta.url));
const tools = path.resolve(here, '../..');
const art = path.resolve(tools, '../ModSource/Yossele/Art');
vm.runInThisContext(fs.readFileSync(path.join(here, 'ase.js'), 'utf8'), { filename: 'ase.js' });
const A = globalThis.RWAse;
const tmp = fs.mkdtempSync(path.join(process.env.ARTBENCH_TMP || os.tmpdir(), 'artbench-'));

const PY = `
import base64, json, sys, zlib
sys.path.insert(0, ${JSON.stringify(path.join(tools, 'aseprite'))})
import ase_sheets as S
cmd, a, b = sys.argv[1:4]
if cmd == 'png':
    w, h, px = S.png_read(a)
    open(b, 'wb').write(px)
    print(w, h)
elif cmd == 'read':
    x = S.read_ase(a)
    fr = [S.flatten(x, i) for i in range(len(x['frames']))]
    json.dump({'w': x['w'], 'h': x['h'], 'tags': x['tags'], 'layers': x['layers'], 'durations': x['durations'],
               'frames': [base64.b64encode(f).decode() for f in fr], 'crcs': ['%08x' % zlib.crc32(f) for f in fr]},
              open(b, 'w'))
elif cmd == 'write':
    s = json.load(open(a))
    S.write_ase(b, s['w'], s['h'], [base64.b64decode(f) for f in s['frames']],
                [(t['name'], t['from'], t['to'], tuple(t['color']), t['data']) for t in s['tags']],
                [tuple(c) for c in s['palette']], s['layerName'])
`;
const py = (...args) => execFileSync('python3', ['-I', '-c', PY, ...args], { encoding: 'utf8' });
const pyRead = file => { const o = path.join(tmp, 'read.json'); py('read', file, o); return JSON.parse(fs.readFileSync(o)); };
const b64 = u => Buffer.from(u).toString('base64');

let pass = 0, fail = 0;
function check(name, ok, detail = '') {
  ok ? pass++ : fail++;
  console.log((ok ? 'ok   ' : 'FAIL ') + name + (ok || !detail ? '' : '  -- ' + detail));
}
function sameAse(label, js, ref) {
  check(`${label}: size ${ref.w}x${ref.h}, ${ref.frames.length} frames`,
        js.w === ref.w && js.h === ref.h && js.frames.length === ref.frames.length);
  const bad = ref.frames.map((f, i) => b64(js.frames[i] || []) === f ? -1 : i).filter(i => i >= 0);
  check(`${label}: every frame pixel-identical`, bad.length === 0, 'frames ' + bad.join(','));
  const tj = JSON.stringify(js.tags.map(t => [t.name, t.from, t.to, t.data]));
  const tr = JSON.stringify(ref.tags.map(t => [t.name, t.from, t.to, t.data]));
  check(`${label}: ${ref.tags.length} tags, names/ranges/user data identical`, tj === tr, tj + ' vs ' + tr);
  const lj = JSON.stringify(js.layers.map(l => [l.name, +l.visible, l.type, l.level, l.opacity]));
  const lr = JSON.stringify(ref.layers.map(l => [l.name, l.visible, l.type, l.level, l.opacity]));
  check(`${label}: ${ref.layers.length} layers identical`, lj === lr, lj + ' vs ' + lr);
}

// Chunk listing with sizes dropped and zlib cels inflated: equal listings = byte-identical apart from zlib streams.
function canon(buf) {
  const d = Buffer.from(buf), out = [d.subarray(4, 128).toString('hex')];
  for (let f = 0, pos = 128; f < d.readUInt16LE(6); f++) {
    out.push('F' + d.subarray(pos + 4, pos + 16).toString('hex'));
    for (let k = 0, p = pos + 16, n = d.readUInt32LE(pos + 12); k < n; k++) {
      const len = d.readUInt32LE(p), type = d.readUInt16LE(p + 4), c = d.subarray(p + 6, p + len);
      out.push(type.toString(16) + ':' + (type === 0x2005 && c.readUInt16LE(7) === 2
        ? c.subarray(0, 20).toString('hex') + zlib.inflateSync(c.subarray(20)).toString('hex') : c.toString('hex')));
      p += len;
    }
    pos += d.readUInt32LE(pos);
  }
  return out;
}

// ---- the real sheet, decoded by Python
const sheetPng = path.join(art, 'Yossele-Sheet.png'), sheetRaw = path.join(tmp, 'sheet.rgba');
const [SW, SH] = py('png', sheetPng, sheetRaw).trim().split(' ').map(Number);
const sheet = new Uint8Array(fs.readFileSync(sheetRaw));
const cell = (col, row) => {
  const r = new Uint8Array(32 * 32 * 4);
  for (let y = 0; y < 32; y++) r.set(sheet.subarray(((row * 32 + y) * SW + col * 32) * 4, ((row * 32 + y) * SW + col * 32 + 32) * 4), y * 128);
  return r;
};
const paletteOf = frames => {
  const seen = new Map([['0,0,0,0', [0, 0, 0, 0]]]);
  for (const f of frames) for (let i = 0; i < f.length; i += 4)
    if (f[i + 3]) seen.set(`${f[i]},${f[i + 1]},${f[i + 2]},${f[i + 3]}`, Array.from(f.subarray(i, i + 4)));
  return [...seen.values()].slice(0, 256);
};
const tagFor = (name, row, from, frames, color, extra = '') => ({
  name, from, to: from + frames.length - 1, color,
  data: `sheet=Vessel/Yossele-Sheet.png;row=${row};col=0;cols=6;rows=8;gen=${frames.map(A.frameCrc).join(',')}${extra}`,
});

// 1. writeAse -> Python read_ase + flatten
{
  check(`sheet decoded ${SW}x${SH}`, SW === 192 && SH === 256);
  const frames = [0, 1, 2, 3, 4, 5].map(c => cell(c, 0));
  const tag = tagFor('idle_v0', 0, 0, frames, [106, 190, 48]);
  const file = path.join(tmp, 'js_row.aseprite');
  const spec = { w: 32, h: 32, frames, tags: [tag], palette: paletteOf(frames), layerName: 'Yossele 0.1' };
  const bytes = await A.writeAse(spec);
  fs.writeFileSync(file, bytes);
  const ref = pyRead(file);
  sameAse('1 writeAse -> read_ase', { ...ref, frames: frames, tags: [tag], layers: ref.layers }, ref);
  check('1 frameCrc equals Python zlib.crc32 for every frame', JSON.stringify(frames.map(A.frameCrc)) === JSON.stringify(ref.crcs));
  // Same input through Python write_ase: identical bytes apart from the zlib streams.
  const pyFile = path.join(tmp, 'py_row.aseprite'), specFile = path.join(tmp, 'spec.json');
  fs.writeFileSync(specFile, JSON.stringify({ ...spec, frames: frames.map(b64) }));
  py('write', specFile, pyFile);
  const cj = canon(bytes), cp = canon(fs.readFileSync(pyFile));
  const diff = cj.findIndex((x, i) => x !== cp[i]);
  check('1 writeAse bytes match write_ase (zlib streams inflated)', cj.length === cp.length && diff < 0, 'first diff at item ' + diff);
  // Empty frame: no cel chunk, like write_ase
  const withEmpty = await A.writeAse({ ...spec, frames: [frames[0], new Uint8Array(32 * 32 * 4)], tags: [] });
  const ce = canon(withEmpty);
  check('1 empty frame written without a cel', ce[ce.length - 1].startsWith('F') && ce.filter(x => x.startsWith('2005:')).length === 1);
  const back = await A.readAse(withEmpty);
  check('1 readAse(writeAse) round trip', b64(back.frames[0]) === b64(frames[0]) && back.frames[1].every(v => v === 0));
}

// 2. Python write_ase (multi-tag) -> readAse
{
  const frames = [], tags = [], colours = [[106, 190, 48], [106, 190, 48], [91, 110, 225], [178, 102, 216]];
  for (let row = 0; row < 4; row++) {
    const fr = [0, 1, 2, 3, 4, 5].map(c => cell(c, row * 2));
    tags.push(tagFor(['idle_v0', 'idle_v1', 'move_v0', 'ability_v0'][row], row * 2, frames.length, fr, colours[row],
                     row === 1 ? ';mirror=idle_v0' : ''));
    frames.push(...fr);
  }
  frames.push(new Uint8Array(32 * 32 * 4));  // an empty frame, untagged
  tags.push({ name: 'ünïcode tag', from: 24, to: 24, color: [1, 2, 3], data: '' });
  const specFile = path.join(tmp, 'spec2.json'), file = path.join(tmp, 'py_multi.aseprite');
  fs.writeFileSync(specFile, JSON.stringify({ w: 32, h: 32, frames: frames.map(b64), tags, palette: paletteOf(frames), layerName: 'Yossele' }));
  py('write', specFile, file);
  const js = await A.readAse(new Uint8Array(fs.readFileSync(file)));
  sameAse('2 write_ase -> readAse', js, pyRead(file));
  check('2 frames equal the sheet cells', frames.every((f, i) => b64(f) === b64(js.frames[i])));
  check('2 tag colours read', JSON.stringify(js.tags.map(t => t.color)) === JSON.stringify(tags.map(t => t.color)));
}

// 3. Real files: readAse vs read_ase + flatten
for (const name of fs.readdirSync(path.join(art, 'aseprite')).filter(n => n.endsWith('.aseprite')).sort()) {
  const file = path.join(art, 'aseprite', name);
  sameAse('3 ' + name, await A.readAse(new Uint8Array(fs.readFileSync(file))), pyRead(file));
}

// 4. zipStore
{
  const files = [{ name: 'sheets/Vessel/Yossele-Sheet.png', bytes: new Uint8Array(fs.readFileSync(sheetPng)) },
                 { name: 'ünï/notes.txt', bytes: new TextEncoder().encode('hello\n') },
                 { name: 'empty.bin', bytes: new Uint8Array(0) }];
  const zip = path.join(tmp, 'test.zip');
  fs.writeFileSync(zip, A.zipStore(files));
  const t = spawnSync('python3', ['-I', '-m', 'zipfile', '-t', zip], { encoding: 'utf8' });
  check('4 python3 -m zipfile -t', t.status === 0, t.stdout + t.stderr);
  const names = spawnSync('python3', ['-I', '-c', 'import sys,zipfile; z=zipfile.ZipFile(sys.argv[1]); print("\\n".join("%s %d %d" % (i.filename, i.compress_type, len(z.read(i))) for i in z.infolist()))', zip], { encoding: 'utf8' }).stdout.trim();
  check('4 names (UTF-8), method 0, sizes', names === files.map(f => `${f.name} 0 ${f.bytes.length}`).join('\n'), names);
  const u = spawnSync('unzip', ['-t', zip], { encoding: 'utf8' });
  if (u.error) console.log('skip unzip -t (not installed)');
  else check('4 unzip -t', u.status === 0 && /No errors/.test(u.stdout), u.stdout + u.stderr);
  check('4 crc32 matches zlib', A.crc32(files[0].bytes) === zlib.crc32(files[0].bytes) && A.crc32(new Uint8Array(0)) === 0);
}

// 5. pngEncode -> Python png_read
{
  const png = path.join(tmp, 'enc.png'), raw = path.join(tmp, 'enc.rgba');
  fs.writeFileSync(png, await A.pngEncode(SW, SH, sheet));
  const [w, h] = py('png', png, raw).trim().split(' ').map(Number);
  check('5 pngEncode -> png_read same pixels', w === SW && h === SH && Buffer.compare(fs.readFileSync(raw), Buffer.from(sheet)) === 0);
}

// 6. Hand-built files for what Python can't read: 8-bit indexed, 16-bit grayscale, groups, opacity, links, tilemaps
{
  const le = (n, s) => { const b = Buffer.alloc(s); s === 1 ? b.writeUInt8(n) : s === 2 ? b.writeUInt16LE(n & 0xFFFF) : b.writeUInt32LE(n); return b; };
  const str = s => Buffer.concat([le(Buffer.byteLength(s), 2), Buffer.from(s)]);
  const ch = (t, body) => Buffer.concat([le(body.length + 6, 4), le(t, 2), body]);
  const layer = (flags, type, level, opacity, name) => ch(0x2004, Buffer.concat([le(flags, 2), le(type, 2), le(level, 2), Buffer.alloc(6), le(opacity, 1), Buffer.alloc(3), str(name)]));
  const cel = (li, x, y, op, type, rest) => ch(0x2005, Buffer.concat([le(li, 2), le(x, 2), le(y, 2), le(op, 1), le(type, 2), Buffer.alloc(7), rest]));
  const file = (depth, w, h, trans, frames) => {
    const body = Buffer.concat(frames.map(cs => { const b = Buffer.concat(cs); return Buffer.concat([le(16 + b.length, 4), le(0xF1FA, 2), le(cs.length, 2), le(100, 2), Buffer.alloc(2), le(cs.length, 4), b]); }));
    const head = Buffer.alloc(128);
    head.writeUInt32LE(128 + body.length, 0); head.writeUInt16LE(0xA5E0, 4); head.writeUInt16LE(frames.length, 6);
    head.writeUInt16LE(w, 8); head.writeUInt16LE(h, 10); head.writeUInt16LE(depth, 12); head.writeUInt32LE(1, 14); head[28] = trans;
    return new Uint8Array(Buffer.concat([head, body]));
  };
  const px = (...v) => Buffer.from(v);
  const newPal = ch(0x2019, Buffer.concat([le(3, 4), le(0, 4), le(2, 4), Buffer.alloc(8), px(0, 0, 0, 0, 0, 0), px(0, 0, 255, 0, 0, 255), px(1, 0, 0, 255, 0, 128), str('green')]));
  const oldPal = ch(0x0004, Buffer.concat([le(1, 2), px(0, 3), px(0, 0, 0, 255, 0, 0, 0, 0, 255)]));
  // indexed 2x1, transparent index 0: layer 0 normal, group 1 hidden with child layer 2, tilemap layer 3
  const idx = file(8, 2, 1, 0, [
    [newPal, layer(1, 0, 0, 255, 'base'), layer(0, 1, 0, 255, 'hidden group'), layer(1, 0, 1, 255, 'in hidden'), layer(1, 2, 0, 255, 'tiles'),
     cel(0, 0, 0, 255, 0, Buffer.concat([le(2, 2), le(1, 2), px(0, 1)])), cel(2, 0, 0, 255, 0, Buffer.concat([le(2, 2), le(1, 2), px(1, 1)])),
     cel(3, 0, 0, 255, 3, Buffer.alloc(32))],
    [cel(0, 0, 0, 0, 1, le(0, 2)), cel(0, 1, 0, 255, 0, Buffer.concat([le(1, 2), le(1, 2), px(2)]))],  // link, then a second cel replaces it
  ]);
  const ri = await A.readAse(idx);
  check('6 indexed: transparent index, palette, hidden group, tilemap skipped',
        ri.depth === 8 && b64(ri.frames[0]) === b64(px(0, 0, 0, 0, 255, 0, 0, 255)));
  check('6 indexed: cel moved in frame 2', b64(ri.frames[1]) === b64(px(0, 0, 0, 0, 0, 255, 0, 128)));
  const ri2 = await A.readAse(file(8, 2, 1, 1, [[oldPal, layer(1, 0, 0, 255, 'a'), cel(0, 0, 0, 255, 0, Buffer.concat([le(2, 2), le(1, 2), px(1, 2)]))]]));
  check('6 indexed: old palette chunk fallback', b64(ri2.frames[0]) === b64(px(0, 0, 0, 0, 0, 0, 255, 255)));
  // grayscale 2x1: layer opacity 128 under a full layer, linked cel in frame 2, zlib cel
  const gz = zlib.deflateSync(px(200, 255, 100, 255));
  const gray = file(16, 2, 1, 0, [
    [layer(1, 0, 0, 255, 'under'), layer(1, 0, 0, 128, 'over'),
     cel(0, 0, 0, 255, 2, Buffer.concat([le(2, 2), le(1, 2), gz])), cel(1, 1, 0, 255, 0, Buffer.concat([le(1, 2), le(1, 2), px(0, 255)]))],
    [cel(0, 0, 0, 0, 1, le(0, 2))],
  ]);
  const rg = await A.readAse(gray);
  // over pixel: sa = 255*255*128/65025 = 128 onto da 255 -> oa = 128 + 255*127/255 = 255; rgb = (0 + 100*255*127/255)/255 = 49
  check('6 grayscale: layer opacity blend', b64(rg.frames[0]) === b64(px(200, 200, 200, 255, 49, 49, 49, 255)));
  check('6 grayscale: linked cel', b64(rg.frames[1]) === b64(px(200, 200, 200, 255, 100, 100, 100, 255)));
}

console.log(`\n${pass} passed, ${fail} failed  (scratch: ${tmp})`);
process.exit(fail ? 1 : 0);
