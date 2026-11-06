/* Scope sheet <-> vessel model, in the browser. A port of parse.py (Dimitri's sheets, "mine" mode) plus the
   vessel shaping build.py did for Spool and Yossele. Also the line store the editor round-trips byte for byte.
   Claude's ~defaults~ never reach the page: a whole-default value reads as unset, a default inside one of
   Dimitri's values becomes WIPMARK, which the page draws as a WIP chip. */
const WIPMARK = '';

/* ------------------------------------------------ the text, as lines (exact round trip) */
function textToDoc(text) {
  const eol = /\r\n/.test(text) ? '\r\n' : '\n';
  const finalNL = /\r?\n$/.test(text);
  const body = finalNL ? text.replace(/\r?\n$/, '') : text;
  return { lines: body.split(/\r?\n/), eol, finalNL };
}
function docToText(doc) {
  return doc.lines.join(doc.eol) + (doc.finalNL ? doc.eol : '');
}
const depthOf = raw => raw.length - raw.replace(/^\t+/, '').length;
const isBlank = raw => !raw.trim();

/* ------------------------------------------------ tree */
function tree(lines) {
  const root = { t: '', k: [], d: -1, i: -1 };
  const stack = [root];
  lines.forEach((raw, i) => {
    if (isBlank(raw)) return;
    const d = depthOf(raw);
    const n = { t: raw.trim(), k: [], d, i };
    while (stack[stack.length - 1].d >= d) stack.pop();
    stack[stack.length - 1].k.push(n);
    stack.push(n);
  });
  return root;
}

const NOTE = /^(.*?)\s*\/(~?)\s*(.*?)\s*\\\s*$/;
const FIELD = /^- ((?:In-game|[A-Z][A-Za-z0-9]*)(?: [a-z0-9'%]+){0,3}): ?(.*)$/;
const UPG = /^%\s?~?(\d)~?:\{(~?)([^}~]*)~?\}\s*(.*)$/;
const BLD = /^\(([^)]+)\):\{([^}]*)\}\s*(.*)$/;
const SEC = /^\[([^\]]+)\]\s*(.*)$/;

function splitNote(s) {
  const m = NOTE.exec(s);
  if (m) return [m[1].trim(), { text: m[3].trim(), claude: m[2] === '~' }];
  return [s.trim(), null];
}
function clean(s) {
  return String(s).replace(/[{}\[\]]/g, '').replace(/->/g, '→').trim();
}
/* Dimitri's value with Claude's parts taken out: null when nothing of his is left */
function mineVal(v) {
  if (v == null) return null;
  if (/^\s*\{?~/.test(v)) return null; /* a value that opens with a default is Claude's, unit and all */
  let s = v.replace(/~[^~]*~/g, WIPMARK).replace(/~/g, '');
  const left = s.replace(new RegExp(WIPMARK, 'g'), '').replace(/[\s,.;:+\-–→()%]/g, '');
  if (!left) return null;
  s = s.replace(/\s+/g, ' ').trim();
  return s;
}
function val(s) {
  let [v, note] = splitNote(s);
  let n = null;
  if (note && !note.claude) n = clean(note.text) || null;
  const mv = mineVal(v);
  return [mv ? clean(mv) : null, n];
}

function fields(node) {
  const out = {}, desc = [], notes = [];
  for (const k of node.k) {
    const t = k.t;
    if (t.startsWith('#')) { notes.push(clean(t.slice(1).trim())); continue; }
    if (t.startsWith('/')) {
      if (t.startsWith('/~')) continue;
      notes.push(clean(t.replace(/^\/+|[\\\s]+$/g, '').trim()));
      continue;
    }
    const m = FIELD.exec(t);
    if (m) {
      const [v, n] = val(m[2]);
      out[m[1]] = { v, n, r: m[2] };
    } else if (t.startsWith('- ')) {
      const [v, n] = val(t.slice(2));
      if (v) desc.push({ v, n });
    } else if (t.startsWith('!')) {
      desc.push({ needs: clean(t.replace(/^!\s*|\s*!$/g, '')) });
    }
  }
  return [out, desc, notes];
}

function upgrades(node) {
  const ups = [];
  for (const k of node.k) {
    const m = UPG.exec(k.t);
    if (m) {
      const lvl = +m[1], tilde = m[2], name = m[3];
      const [d, note] = splitNote(m[4]);
      const [f] = fields(k);
      const g = key => (f[key] || {});
      ups.push({
        lvl, name: tilde || !name.trim() ? null : clean(name), desc: clean(mineVal(d) || ''),
        note: note && !note.claude ? clean(note.text) : null,
        line: g('In-game line').v || null, num: g('Numbers').v || null, numnote: g('Numbers').n || null,
        leads: g('Leads to').v || null, cost: g('Cost').v || null,
      });
    } else if (k.t.startsWith('!')) {
      ups.push({ needs: clean(k.t.replace(/^!\s*|\s*!$/g, '')) });
    }
  }
  return ups;
}

function vesselNode(node) {
  const comma = node.t.indexOf(',');
  const v = {
    name: (comma < 0 ? node.t : node.t.slice(0, comma)).trim(), title: comma < 0 ? '' : node.t.slice(comma + 1).trim(),
    notes: [], identity: {}, stats: {}, ability: null, resource: null, mechanic: null,
    buildings: [], minions: [], directions: [],
  };
  for (const k of node.k) {
    const t = k.t;
    if (t.startsWith('#')) { v.notes.push(clean(t.slice(1).trim())); continue; }
    if (t.startsWith('/')) continue;
    const b = BLD.exec(t);
    if (b) {
      const [slot, bname, rest] = [b[1], b[2], b[3]];
      const bnote = rest ? splitNote(rest)[1] : null;
      const [f, desc, notes] = fields(k);
      const item = {
        slot, name: clean(bname), note: bnote && !bnote.claude ? bnote.text : null,
        fields: f, desc, notes, stats: {}, growth: null, growthnote: null, upgrades: [], subs: [],
      };
      for (const s of k.k) {
        const st = s.t;
        if (st.startsWith('[Stats]')) item.stats = fields(s)[0];
        else if (st.startsWith('[Growth]')) {
          const gn = splitNote(st)[1];
          item.growth = fields(s)[0];
          item.growthnote = gn && !gn.claude ? gn.text : null;
        } else if (st.startsWith('[Upgrades]')) item.upgrades = upgrades(s);
        else if (st.startsWith('{')) {
          const end = st.indexOf('}');
          const nm = st.slice(0, end + 1), rest2 = st.slice(end + 1);
          const [sf, sd, sn] = fields(s);
          const n2 = splitNote(rest2)[1];
          item.subs.push({ name: clean(nm), note: n2 && !n2.claude ? n2.text : null, fields: sf, desc: sd, notes: sn });
        }
      }
      (slot === 'Minion' ? v.minions : v.buildings).push(item);
      continue;
    }
    const s = SEC.exec(t);
    if (s) {
      const sec = s[1], rest = s[2];
      const [f] = fields(k);
      if (sec === 'Identity') v.identity = f;
      else if (sec === 'Vessel Stats') v.stats = f;
      else if (sec === 'Ability' || sec === 'Resource' || sec === 'Vessel Mechanic') {
        const key = { Ability: 'ability', Resource: 'resource', 'Vessel Mechanic': 'mechanic' }[sec];
        const nm = mineVal(splitNote(rest)[0]);
        v[key] = { name: nm ? clean(nm) : null, fields: f };
      } else if (sec === 'Directions') {
        for (const d of k.k) if (d.t.startsWith('- ')) {
          const names = [...splitNote(d.t)[0].matchAll(/\{([^}]*)\}/g)].map(x => clean(x[1])).filter(x => x && !x.includes('~'));
          v.directions.push(...names);
        }
      }
    }
  }
  return v;
}

function parseSheet(lines) {
  const r = tree(lines);
  const out = [];
  for (const top of r.k) {
    if (!/Vessel:/.test(top.t)) continue;
    for (const k of top.k) {
      if (k.t.includes(',') && !/^[#\/\-]/.test(k.t)) out.push(vesselNode(k));
    }
  }
  return out;
}
/* The vessel's name from the file's own header, for imports */
function sheetName(lines) {
  for (const raw of lines) {
    const m = /^\|\s*(?:New\s+)?Vessel:\s*([^|]+?)\s*\|/.exec(raw.trim());
    if (m) return m[1];
  }
  return null;
}

/* ------------------------------------------------ the page's vessel model (was build.py's dimitri()) */
const num = v => { const m = /^-?\d*\.?\d+/.exec(v || ''); return m ? parseFloat(m[0]) : null; };
const kv = (d, skip = []) => Object.entries(d || {}).filter(([k]) => !skip.includes(k)).map(([k, x]) => ({ k, v: x.v, n: x.n }));
const STAT_KEYS = [['Health', 65], ['Speed', 3.5], ['Damage', 5], ['Range', 3], ['Attack speed', 0.75], ['Build range', 3], ['Sell rate', 75]];
const UNIT_KEYS = ['Made by', 'Attack', 'Health', 'Damage', 'Range', 'Attack speed', 'Speed', 'Engage range', 'Carries',
  'Collects from', 'How many', 'Respawn', 'Leash', 'Orders', 'Cost', 'Look'];

function statsBlock(v) {
  const s = v.stats;
  const bars = STAT_KEYS.map(([k, mx]) => {
    const val_ = (s[k] || {}).v || null;
    const main = val_ ? val_.split(',')[0].trim() : null;
    const extra = val_ && val_.includes(',') ? val_.slice(val_.indexOf(',') + 1).trim() : null;
    return { k, v: main, x: extra, num: num(main), max: mx };
  });
  return { bars, regen: (s['Health regen'] || {}).v || null };
}
function ruleOf(v) {
  const r = v.stats['Special rule'];
  if (!r || !r.r) return { wip: true };
  const raw = splitNote(r.r)[0];
  if (raw.trim() === 'none') return { none: true };
  const m = /^\{([^}]*)\}\s*(.*)$/.exec(raw);
  const name = m ? mineVal(m[1]) : null, text = mineVal(m ? m[2] : raw);
  if (!name && !text) return { wip: true };
  return { name: name ? clean(name) : null, text: text ? clean(text) : null };
}
function splitNotes(lines) {
  return lines.map(t => {
    const m = /^(.*?)\s*\/\s*(.*?)\s*\\?\s*$/.exec(t);
    if (m && t.includes(' / ')) return { v: m[1].trim(), n: m[2].replace(/[\\\s]+$/, '').trim() };
    return { v: t, n: null };
  });
}
function buildingOut(b) {
  const st = b.stats;
  const ups = [], needs = [];
  for (const u of b.upgrades) {
    if (u.needs) { needs.push(u.needs); continue; }
    const c = Object.assign({}, u);
    if (c.leads === 'nothing, the branch ends here') c.leads = 'end';
    ups.push(c);
  }
  const unit = b.slot === 'Minion';
  return {
    slot: b.slot, name: b.name, alt: b.note, line: (st['In-game line'] || {}).v || null, cost: (st.Cost || {}).v || (unit ? (b.fields.Cost || {}).v : null) || null,
    desc: b.desc, notes: splitNotes(b.notes), stats: unit ? kv(b.fields, ['Cost']) : kv(st, ['In-game line', 'Cost']),
    growth: b.growth ? { rows: kv(b.growth), note: b.growthnote } : null, upgrades: ups, needs, unit,
  };
}
function unitOut(m, sub) {
  const f = sub ? sub.fields : m.fields;
  return {
    name: sub ? sub.name : m.name, note: sub ? sub.note : m.note,
    rows: UNIT_KEYS.filter(k => k in f).map(k => ({ k, v: f[k].v, n: f[k].n })),
    desc: sub ? sub.desc : m.desc, notes: sub ? sub.notes : m.notes,
  };
}
function directionsFor(names, dirLines) {
  if (!names.length || !dirLines) return [];
  const r = tree(dirLines);
  const found = {};
  const walk = node => {
    for (const k of node.k) {
      const m = /^\{([^}]*)\}/.exec(k.t);
      if (m && names.includes(m[1])) {
        const [f] = fields(k);
        const rows = kv(f);
        for (const c of k.k) {
          const rm = /^- (Rank I{1,3}): ?(.*)$/.exec(c.t);
          if (rm) { const [v_, n_] = val(rm[2]); rows.push({ k: rm[1], v: v_, n: n_ }); }
        }
        found[m[1]] = { name: m[1], rows };
      }
      walk(k);
    }
  };
  walk(r);
  return names.map(n => found[n] || { name: n, rows: [], missing: true });
}
const slug = s => String(s || 'vessel').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '') || 'vessel';

/* rec: {lines, frameUrl, accent, accent2}. Returns the page's vessel object, or null if the sheet holds no vessel. */
function vesselModel(rec, dirLines) {
  const vs = parseSheet(rec.doc ? rec.doc.lines : rec.lines);
  if (!vs.length) return null;
  const v = vs[0];
  const id = v.identity, g = k => (id[k] || {});
  const { bars, regen } = statsBlock(v);
  const sig = [['ability', 'Ability'], ['resource', 'Resource'], ['mechanic', 'Vessel rule']]
    .filter(([key]) => v[key]).map(([key, label]) => ({ type: label, name: v[key].name, rows: kv(v[key].fields) }));
  const out = {
    id: rec.id, kind: 'rw', name: v.name, title: v.title || g('Title').v,
    difficulty: g('Difficulty').v, line: g('In-game line').v, look: g('Look').v, lookNote: g('Look').n,
    towersLook: g('Towers look').v, accent: rec.accent, accent2: rec.accent2 || rec.accent, accentName: g('Accent colour').v,
    archetype: g('Archetype').v, lore: g('Lore').v, loreNote: g('Lore').n, spritesNote: g('Sprites').v, status: g('Status').v,
    attack: ((v.stats.Attack || {}).v || '').replace(/\.$/, '') || null, rule: ruleOf(v),
    bars, extras: [], regen, notes: splitNotes(v.notes), sig, ability: v.ability ? v.ability.name : null,
    resource: v.resource ? v.resource.name : null, mechanic: v.mechanic ? v.mechanic.name : null,
    buildings: v.buildings.map(buildingOut), units: [], frameUrl: rec.frameUrl || null,
    directions: directionsFor(v.directions, dirLines),
  };
  for (const m of v.minions) {
    if (m.subs.length) m.subs.forEach(sb => out.units.push(unitOut(m, sb)));
    else out.units.push(unitOut(m));
    if (m.upgrades.length) out.buildings.push(buildingOut(m));
  }
  return out;
}

/* ------------------------------------------------ format check (00_Read_Me rules a slip can break) */
function lintSheet(lines) {
  const out = [];
  let prev = -1, first = lines.findIndex(l => !isBlank(l));
  if (first >= 0 && !/^\|.*\|\s*$/.test(lines[first].trim())) out.push({ i: first, m: 'The file should open with a | Title | line, like | Vessel: Spool |' });
  lines.forEach((raw, i) => {
    if (isBlank(raw)) return;
    if (/^ +/.test(raw) || /^\t+ +\S/.test(raw)) out.push({ i, m: 'Indented with spaces: use one tab per level' });
    const d = depthOf(raw);
    if (prev >= 0 && d > prev + 1) out.push({ i, m: `Jumps ${d - prev} levels deeper than the line above` });
    prev = d;
    const t = raw.trim();
    let ns = -1;
    for (const m of t.matchAll(/(^|\s)\/~?(?=\s|$)/g)) {
      if (((t.slice(0, m.index).match(/~/g) || []).length) % 2 === 0) { ns = m.index; break; }
    }
    const body = ns < 0 ? t : t.slice(0, ns);
    if (((body.match(/~/g) || []).length) % 2) out.push({ i, m: 'A ~default~ is missing its closing ~' });
    if (ns >= 0 && !/\\\s*!?\s*$/.test(t) && /\S+\s+\S+\s+\S+/.test(t.slice(ns + 2))) out.push({ i, m: 'A / note is missing its closing \\' });
    if ((t.match(/\{/g) || []).length !== (t.match(/\}/g) || []).length) out.push({ i, m: 'A {Name} is missing a brace' });
    if (/^%/.test(t) && !UPG.test(t)) out.push({ i, m: 'An upgrade reads % 1:{Name} What it does' });
    if (/^</.test(t) || /<[^>]*>/.test(t)) out.push({ i, m: 'An < answer > is waiting to be folded in', soft: true });
  });
  return out;
}

if (typeof module !== 'undefined') module.exports = { WIPMARK, textToDoc, docToText, depthOf, isBlank, tree, parseSheet, sheetName, vesselModel, lintSheet, slug, splitNote, mineVal };
