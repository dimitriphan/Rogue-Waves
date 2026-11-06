const D = /*__DATA__*/null;
const SH = D.sheets;
const BASE = D.vessels;
const EDIT = !!D.edit;
const DIR_LINES = textToDoc(D.dirText).lines;
/* Rogue Waves vessels live as sheet text (their scope .txt); the page model is parsed from it.
   rec: {id, file, doc, baseText, frameUrl, accent, accent2, added, ver} */
const RECS = D.rw.map(r => ({ id: r.id, file: r.file, doc: textToDoc(r.text), baseText: r.text, baseFrame: r.frameUrl || null,
  frameUrl: r.frameUrl || null, accent: r.accent, accent2: r.accent2, baseAccent: [r.accent, r.accent2], added: false, ver: 0 }));
const MODELS = new Map();
function modelOf(rec) {
  const c = MODELS.get(rec.id);
  if (c && c.ver === rec.ver && c.rec === rec) return c.v;
  let v = null;
  try { v = vesselModel(rec, DIR_LINES); } catch (e) { console.error(e); }
  if (v) v.id = rec.id;
  MODELS.set(rec.id, { ver: rec.ver, rec, v });
  return v;
}
const NEWV = () => RECS.map(modelOf).filter(Boolean);
const findV = id => BASE.find(x => x.id === id) || NEWV().find(x => x.id === id);
const RM = window.matchMedia && matchMedia('(prefers-reduced-motion: reduce)').matches;
const esc = s => String(s ?? '').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]))
  .replace(//g, '<span class="inwip">WIP</span>');
/* for attributes: a WIP mark can't hold markup there */
const att = s => String(s ?? '').replace(//g, '…').replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

/* ---------------------------------------------------------------- sprites */
const REG = []; let live = [];
function spr(sp, scale, face = 0, extra = '') {
  if (!sp) return '';
  REG.push(sp);
  const c = sp.c * scale;
  return `<div class="spr ${extra}" data-i="${REG.length - 1}" data-sc="${scale}" data-face="${face}" style="width:${c}px;height:${c}px" role="img" aria-label="sprite"></div>`;
}
function frameImg(key, scale, label) {
  const s = SH[key];
  return `<img class="px" src="${s.u}" width="${s.w * scale}" height="${s.h * scale}" alt="${esc(label)}">`;
}
function mount(root) {
  root.querySelectorAll('.spr[data-i]').forEach(el => {
    const sp = REG[+el.dataset.i]; const sc = +el.dataset.sc; const sh = SH[sp.s];
    el.style.backgroundImage = `url(${sh.u})`;
    el.style.backgroundSize = `${sh.w * sc}px ${sh.h * sc}px`;
    const it = { el, sp, sc, face: +el.dataset.face, fr: -1 };
    paint(it, 0);
    live.push(it);
  });
}
function paint(it, fr) {
  if (fr === it.fr) return;
  it.fr = fr;
  const row = it.sp.rows[Math.min(it.face, it.sp.rows.length - 1)];
  const c = it.sp.c * it.sc;
  it.el.style.backgroundPosition = `${-fr * c}px ${-row * c}px`;
}
let t0 = performance.now();
function tick(now) {
  const t = (now - t0) / 1000;
  live = live.filter(it => it.el.isConnected);
  for (const it of live) paint(it, it.sp.f > 1 ? Math.floor(t * it.sp.fps) % it.sp.f : 0);
  requestAnimationFrame(tick);
}
if (!RM) requestAnimationFrame(tick);

/* ---------------------------------------------------------------- icons (12x12, 1 px line, like the game's) */
const ICONS = {
  offense: ['..........##', '.........#.#', '........#.#.', '.......#.#..', '..#...#.#...', '...#.#.#....', '....#.#.....', '...#.#......', '..#.#.#.....', '.##...#.....', '#.#.........', '##..........'],
  defense: ['............', '.##########.', '.#........#.', '.#........#.', '.#...##...#.', '.#...##...#.', '.#........#.', '..#......#..', '..#......#..', '...#....#...', '....#..#....', '.....##.....'],
  economy: ['............', '....####....', '...#.#..#...', '..#.#....#..', '.#..#.....#.', '############', '.#..#.....#.', '..#..#...#..', '...#.#..#...', '....#.##....', '.....##.....', '............'],
  units: ['..#.........', '..######....', '..#.....##..', '..#.......#.', '..#.....##..', '..######....', '..#.........', '..#.........', '..#.........', '..#.........', '.###........', '............'],
  support: ['............', '..##....##..', '.#..#..#..#.', '#....##....#', '#..........#', '#..........#', '.#........#.', '..#......#..', '...#....#...', '....#..#....', '.....##.....', '............'],
  trap: ['............', '............', '..#...#.....', '...#...#....', '....#...#...', '.....#...#..', '....#...#...', '...#...#....', '..#...#.....', '............', '............', '............'],
  minion: ['............', '..##....##..', '.#..#..#..#.', '.#..#..#..#.', '..##....##..', '....####....', '...#....#...', '..#......#..', '..#......#..', '...######...', '............', '............'],
  summoner: ['............', '..########..', '.#........#.', '############', '#..........#', '#.#.#..#.#.#', '#..........#', '#..........#', '#.#.#..#.#.#', '############', '............', '............'],
};
function icon(name, size = 24) {
  const rows = ICONS[name] || ICONS.support; let d = '';
  rows.forEach((r, y) => { r.padEnd(12, '.').slice(0, 12).split('').forEach((ch, x) => { if (ch === '#') d += `M${x} ${y}h1v1h-1z`; }); });
  return `<svg width="${size}" height="${size}" viewBox="0 0 12 12" shape-rendering="crispEdges" aria-hidden="true"><path d="${d}" fill="currentColor"/></svg>`;
}
function slotIcon(slot) {
  const s = slot.toLowerCase();
  if (s.includes('summoner')) return 'summoner';
  if (s.includes('offense')) return 'offense';
  if (s.includes('defense')) return 'defense';
  if (s.includes('economy')) return 'economy';
  if (s.includes('units')) return 'units';
  if (s.includes('trap')) return 'trap';
  if (s.includes('minion')) return 'minion';
  return 'support';
}

/* ---------------------------------------------------------------- small parts */
const wipTag = (t = 'WIP') => `<span class="inwip">${esc(t)}</span>`;
function wipCover(ghost, title, line, extra = '') {
  return `<div class="wipc"><div class="ghost" aria-hidden="true">${ghost}</div><div class="plate"><b>${esc(title || 'WORK IN PROGRESS')}</b>${line ? `<span>${esc(line)}</span>` : ''}${extra}</div></div>`;
}
function costChips(c) {
  if (!c) return '';
  const parts = []; let rest = c;
  const re = /(Red|Blue|Yellow|Thread)\s+(\d+)/g; let m;
  while ((m = re.exec(c))) parts.push(`<span class="au ${m[1]} num" title="${m[1]}">${m[2]}</span>`);
  rest = c.replace(re, '').replace(/^[\s+,]+|[\s+,]+$/g, '').replace(/\s*\+\s*/g, ' ');
  if (!parts.length) return `<span class="au Free">${esc(c.charAt(0).toUpperCase() + c.slice(1))}</span>`;
  return `<span class="cost">${parts.join('')}</span>${rest ? `<span class="costtxt">${esc(rest)}</span>` : ''}`;
}
function pips(diff) {
  const n = { Basic: 1, Intermediate: 2, Advanced: 3 }[diff] || 0;
  return `<span class="pips" aria-label="${att(diff)}">${[1, 2, 3].map(i => `<i class="${i <= n ? 'on' : ''}"></i>`).join('')}</span>`;
}
const cap = s => s ? String(s).charAt(0).toUpperCase() + String(s).slice(1) : s;
const xref = s => String(s).replace(/→\s*\d+_[A-Za-z_]+\.txt\s*/g, 'see ').replace(/→\s*/g, 'see ').replace(/([.!?]) see /g, '$1 See ');
const val = (v, n) => v ? `${esc(cap(xref(v)))}${n ? `<span class="note">${esc(cap(n))}</span>` : ''}` : wipTag();
/* set rows first; a new vessel's unset rows fold into one "Not set yet" line instead of a column of WIP tags */
function rowsHTML(rows) {
  const set = rows.filter(r => r.v), open = rows.filter(r => !r.v);
  return set.map(r => `<div class="r"><span>${esc(r.k)}</span><span>${val(r.v, r.n)}</span></div>`).join('')
    + (open.length ? `<div class="r"><span>Not set yet</span><span>${wipTag()} ${esc(open.map(r => r.k).join(', '))}</span></div>` : '');
}
function vesselStyle(v) { return `--vc:${v.accent};--vc2:${v.accent2 || v.accent}`; }

