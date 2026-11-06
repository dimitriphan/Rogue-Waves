/* ================================================================ the editor (Vessel Editor page only)
   The sheet's lines are the record: the outline edits them one line at a time, the Text view edits them all at
   once, and Export writes them back out exactly, so an untouched sheet exports byte for byte. Saves go to the
   artifact's shared store (db, collection "sheets", one document per vessel), or to this browser when there is none. */
const ED = { rec: null, mode: 'outline', act: -1, fresh: -1, tools: -1, undo: [], redo: [], pane: 'sheet', bar: null, lint: false, hintKey: null };
let DB = null, STORE = 'pending', CANWRITE = true, DLS;
const TPL = textToDoc(D.vesselTemplate).lines;

/* ------------------------------------------------ line kinds and the syntax view */
const CONTAINER = new Set(['title', 'vessel', 'section', 'building', 'sub', 'upgrade']);
function kindOf(t, d) {
  if (!t.trim()) return 'blank';
  if (/^\|.*\|\s*$/.test(t)) return 'title';
  if (/^\/~/.test(t)) return 'cnote';
  if (/^\//.test(t)) return 'note';
  if (/^#/.test(t)) return 'design';
  if (/^!/.test(t)) return 'needs';
  if (/^</.test(t)) return 'answer';
  if (/^%/.test(t)) return 'upgrade';
  if (/^\[/.test(t)) return 'section';
  if (/^\([^)]*\):\{/.test(t)) return 'building';
  if (/^\{/.test(t)) return 'sub';
  if (/^- [^:]{1,40}:/.test(t)) return 'field';
  if (/^- /.test(t)) return 'detail';
  if (d === 1 && t.includes(',')) return 'vessel';
  return 'detail';
}
/* where a trailing / note \ starts, skipping a " / " that sits inside a ~default~ */
function noteStart(t) {
  for (const m of t.matchAll(/(^|\s)\/~?(?=\s|$)/g)) {
    if (((t.slice(0, m.index).match(/~/g) || []).length) % 2 === 0) return m.index;
  }
  return -1;
}
function fmtInline(s) {
  let out = '', last = 0;
  const re = /(~[^~]*~)|(\{[^}]*\})|(<[^>]*>)|(\[[^\]]*\])|(![^!]*!)/g; let m;
  const e = x => x.replace(/[&<>"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));
  while ((m = re.exec(s))) {
    out += e(s.slice(last, m.index)); last = re.lastIndex;
    if (m[1]) out += m[1].length > 2 ? `<span class="tk-def" title="Claude's default">${fmtInline(m[1].slice(1, -1))}</span>` : `<span class="tk-def empty" title="An empty slot">blank</span>`;
    else if (m[2]) out += `<b class="tk-name">${e(m[2])}</b>`;
    else if (m[3]) out += `<span class="tk-ans">${e(m[3])}</span>`;
    else if (m[4]) out += `<span class="tk-sys">${e(m[4])}</span>`;
    else out += `<span class="tk-needs">${e(m[5])}</span>`;
  }
  return out + e(s.slice(last));
}
function fmtLine(t, k) {
  if (k === 'cnote') return `<span class="tk-who">Claude</span> <span class="tk-cnote">${fmtInline(t.replace(/^\/~\s*|\s*\\\s*$/g, ''))}</span>`;
  if (k === 'note') return `<span class="tk-note">${fmtInline(t.replace(/^\/\s*|\s*\\\s*$/g, ''))}</span>`;
  let body = t, note = '';
  const ns = noteStart(t);
  if (ns > 0) {
    body = t.slice(0, ns);
    const n = t.slice(ns).trim();
    note = /^\/~/.test(n)
      ? ` <span class="tk-who">Claude</span> <span class="tk-cnote">${fmtInline(n.replace(/^\/~\s*|\s*\\\s*!?\s*$/g, ''))}</span>`
      : ` <span class="tk-note">${fmtInline(n.replace(/^\/\s*|\s*\\\s*!?\s*$/g, ''))}</span>`;
  }
  if (k === 'field') {
    const c = body.indexOf(':');
    return `<span class="tk-key">${fmtInline(body.slice(2, c))}</span><span class="tk-val">${fmtInline(body.slice(c + 1).trim()) || '<span class="tk-empty">empty</span>'}</span>${note}`;
  }
  if (k === 'detail') return `<span class="tk-dash" aria-hidden="true">–</span>${fmtInline(body.replace(/^- /, ''))}${note}`;
  if (k === 'design') return `<span class="tk-hash" aria-hidden="true">#</span>${fmtInline(body.replace(/^#\s?/, ''))}${note}`;
  if (k === 'upgrade') {
    const m = /^%\s?(~?\d~?):/.exec(body);
    return `${m ? `<span class="tk-lvl">L${m[1].replace(/~/g, '')}</span>` : ''}${fmtInline(m ? body.slice(m[0].length) : body)}${note}`;
  }
  return fmtInline(body) + note;
}
/* every ~default~ on the line (outside its note) becomes Dimitri's own */
function keepDefaults(raw) {
  const d = depthOf(raw), t = raw.slice(d), ns = noteStart(t);
  const body = ns < 0 ? t : t.slice(0, ns), rest = ns < 0 ? '' : t.slice(ns);
  return '\t'.repeat(d) + body.replace(/~([^~]*)~/g, '$1').replace(/\s+$/, rest ? '' : '') + rest;
}

/* ------------------------------------------------ the template: hints and blocks to insert */
function ctxOf(lines, i) {
  let d = depthOf(lines[i]);
  for (let j = i - 1; j >= 0; j--) {
    if (isBlank(lines[j])) continue;
    const dj = depthOf(lines[j]);
    if (dj < d) {
      const t = lines[j].trim();
      if (/^\[([^\]]+)\]/.test(t)) return t.match(/^\[([^\]]+)\]/)[1];
      if (/^\(Minion\)/.test(t)) return 'Minion';
      if (/^\(/.test(t)) return 'Building';
      if (/^%/.test(t)) return 'Upgrade';
      if (/^\{/.test(t)) return 'Minion';
      d = dj;
    }
  }
  return '';
}
const HINTS = (() => {
  const h = {};
  TPL.forEach((raw, i) => {
    const t = raw.trim(), m = /^- ([^:]+):/.exec(t);
    if (!m) return;
    const ns = noteStart(t);
    if (ns < 0 || /^\s*\/~/.test(t.slice(ns))) return;
    const note = t.slice(ns).replace(/^\s*\/\s*|\s*\\\s*$/g, '');
    const def = (/~[^~]*~/.exec(t.slice(0, ns)) || [''])[0];
    h[ctxOf(TPL, i) + '|' + m[1]] = { note, def };
    if (!h['*|' + m[1]]) h['*|' + m[1]] = { note, def };
  });
  return h;
})();
function hintFor(lines, i) {
  const t = lines[i].trim(), m = /^- ([^:]+):/.exec(t);
  if (!m) return null;
  return HINTS[ctxOf(lines, i) + '|' + m[1]] || HINTS['*|' + m[1]] || null;
}
/* strip the template's own / \ notes (they explain the template, not the vessel) */
function stripTplNote(raw) {
  const d = depthOf(raw), t = raw.slice(d), ns = noteStart(t);
  if (ns < 0 || /^\s*\/~/.test(t.slice(ns))) return raw;
  return '\t'.repeat(d) + t.slice(0, ns).replace(/\s+$/, '');
}
function tplSubtree(test) {
  const i = TPL.findIndex(l => test(l.trim()));
  if (i < 0) return [];
  const d = depthOf(TPL[i]); let j = i + 1;
  while (j < TPL.length && (isBlank(TPL[j]) || depthOf(TPL[j]) > d)) j++;
  while (j > i + 1 && isBlank(TPL[j - 1])) j--;
  return TPL.slice(i, j).map(l => isBlank(l) ? '' : stripTplNote(l)).map(l => l.slice(d)).map(l => l === '' ? '' : l);
}
const rebase = (block, d) => block.map(l => l === '' ? '' : '\t'.repeat(d) + blankNames(l));
/* the template's placeholder names become empty slots, so the page shows them as not named yet */
const blankNames = l => l.replace(/\{Name of [^}]*\}|\{Name\}/g, '{~~}').replace(/\[Name of Mechanic\]/g, '[~~]');
function newSheet(name, title) {
  const body = tplSubtree(t => t === 'Name, Title').map((l, i) => i === 0 ? `${name}, ${title}` : l)
    .map(l => /^\t- Title:\s*$/.test(l) ? `\t- Title: ${title}` : l);
  return ['| Vessel: ' + name + ' |', ...rebase(body, 1)].join('\n') + '\n';
}

/* ------------------------------------------------ edits and undo */
const L = () => ED.rec.doc.lines;
function snapshot() { ED.undo.push(L().slice()); if (ED.undo.length > 200) ED.undo.shift(); ED.redo = []; }
function touched(structural) {
  const rec = ED.rec;
  rec.ver++; rec.dirty = true;
  scheduleSave(rec); schedulePreview();
  if (structural) renderLines(); else lintBadge();
  status();
}
function undo(redo) {
  const from = redo ? ED.redo : ED.undo, to = redo ? ED.undo : ED.redo;
  if (!from.length) return;
  to.push(L().slice());
  ED.rec.doc.lines = from.pop();
  ED.act = -1;
  touched(true);
  if (ED.mode === 'text') renderPane();
}
function blockEnd(lines, i) {
  const d = depthOf(lines[i]); let j = i + 1;
  while (j < lines.length && (isBlank(lines[j]) || depthOf(lines[j]) > d)) j++;
  while (j > i + 1 && isBlank(lines[j - 1])) j--;
  return j;
}
function rowAction(act, i) {
  const lines = L();
  const raw = lines[i], d = depthOf(raw), t = raw.slice(d), k = kindOf(t, d);
  snapshot();
  let focus = -1;
  if (act === 'below') {
    const j = CONTAINER.has(k) ? blockEnd(lines, i) : i + 1;
    lines.splice(j, 0, '\t'.repeat(d) + (/^- /.test(t) || k === 'field' ? '- ' : '')); focus = j;
  } else if (act === 'child') {
    const j = blockEnd(lines, i);
    lines.splice(j, 0, '\t'.repeat(d + 1) + '- '); focus = j;
  } else if (act === 'upg') {
    const j = blockEnd(lines, i);
    const lv = Math.min(4, Math.max(1, ...lines.slice(i + 1, j).map(l => +((/^%\s?~?(\d)/.exec(l.trim()) || [])[1] || 0))));
    const blk = rebase(tplSubtree(x => /^% 1:\{Name\} What it does/.test(x)).map((l, n) => n === 0 ? `% ${lv}:{New Upgrade} What it does, in words.` : l), d + 1);
    lines.splice(j, 0, ...blk); focus = j;
  } else if (act === 'up' || act === 'down') {
    const j = blockEnd(lines, i), blk = lines.slice(i, j);
    if (act === 'up') {
      let p = i - 1;
      while (p >= 0 && (isBlank(lines[p]) || depthOf(lines[p]) > d)) p--;
      if (p < 0 || depthOf(lines[p]) < d) { ED.undo.pop(); return msg('This block is already first in its group.'); }
      lines.splice(i, blk.length); lines.splice(p, 0, ...blk); focus = p;
    } else {
      let n = j;
      while (n < lines.length && isBlank(lines[n])) n++;
      if (n >= lines.length || depthOf(lines[n]) < d) { ED.undo.pop(); return msg('This block is already last in its group.'); }
      const ne = blockEnd(lines, n), gap = n - j;
      const next = lines.slice(j, ne);
      lines.splice(i, ne - i, ...next.slice(gap), ...lines.slice(j, n).slice(0, gap), ...blk);
      focus = i + (ne - n) + gap;
    }
  } else if (act === 'dup') {
    const j = blockEnd(lines, i);
    lines.splice(j, 0, ...lines.slice(i, j)); focus = j;
  } else if (act === 'del') {
    const j = blockEnd(lines, i);
    lines.splice(i, j - i);
    if (!lines.length) lines.push('');
    focus = Math.min(i, lines.length - 1);
    ED.act = -1; touched(true); msg(`Deleted ${j - i} line${j - i === 1 ? '' : 's'}. Undo brings ${j - i === 1 ? 'it' : 'them'} back.`, 'undo');
    return;
  } else if (act === 'mine') {
    lines[i] = keepDefaults(raw); focus = -1;
  } else if (act === 'indent' || act === 'outdent') {
    const nd = act === 'indent' ? d + 1 : Math.max(0, d - 1);
    lines[i] = '\t'.repeat(nd) + t;
  }
  ED.tools = -1;
  touched(true);
  if (focus >= 0 && ['below', 'child', 'upg'].includes(act)) { startEdit(focus, 'end'); ED.fresh = focus; }
  else if (focus >= 0) flashRow(focus);
}
function insertBlock(kind) {
  const lines = L();
  let at = lines.findIndex(l => depthOf(l) === 2 && /^\[(Directions|Questions)\]/.test(l.trim()));
  if (at < 0) at = lines.length;
  const src = kind === 'minion'
    ? tplSubtree(t => /^\(Minion\):\{Name of Minion\}/.test(t)).map((l, n) => n === 0 ? '(Minion):{New Minion}' : l)
    : tplSubtree(t => /^\(Slot\):\{Name of Building\}/.test(t)).map((l, n) => n === 0 ? '(Offense):{New Building}' : l);
  snapshot();
  const blk = rebase(src, 2);
  lines.splice(at, 0, ...blk, '');
  touched(true);
  startEdit(at, 'end');
}

/* ------------------------------------------------ the pane */
const IC = {
  below: '<path d="M8 3v10M3 8h10"/>', child: '<path d="M4 3v6h8M9 6l3 3-3 3"/>', up: '<path d="M8 13V3M4 7l4-4 4 4"/>',
  down: '<path d="M8 3v10M4 9l4 4 4-4"/>', dup: '<path d="M5 5h8v8H5zM3 11V3h8"/>', del: '<path d="M3 4h10M6 4V2.5h4V4M4.5 4l.8 9.5h5.4l.8-9.5"/>',
  mine: '<path d="M3 8.5l3 3 7-7"/>', upg: '<path d="M8 13V5M5 8l3-3 3 3M3 3h10"/>',
};
const svgI = k => `<svg viewBox="0 0 16 16" width="14" height="14" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">${IC[k]}</svg>`;
const RB_LABEL = { below: 'Add a line below', child: 'Add a line inside', upg: 'Add an upgrade', up: 'Move up', down: 'Move down', dup: 'Duplicate', del: 'Delete', mine: 'Keep the defaults as yours' };
function rowHTML(i) {
  const raw = L()[i], d = depthOf(raw), t = raw.slice(d), k = kindOf(t, d);
  const acts = [];
  if (/~/.test(t.slice(0, noteStart(t) < 0 ? t.length : noteStart(t)))) acts.push('mine');
  if (k === 'section' && /^\[Upgrades\]/.test(t)) acts.push('upg');
  if (CONTAINER.has(k)) acts.push('child');
  acts.push('below');
  if (k !== 'blank') acts.push('up', 'down', 'dup');
  acts.push('del');
  const blk = CONTAINER.has(k) && k !== 'title' ? ' (and everything inside it)' : '';
  return `<div class="ln k-${k}${i === ED.act ? ' act' : ''}${i === ED.tools ? ' tools' : ''}" data-i="${i}" style="--d:${Math.min(d, 8)}">
    <span class="gut num" aria-hidden="true">${i + 1}</span>
    <div class="lc">${k === 'blank' ? `<button class="disp blank" aria-label="Empty line ${i + 1}, click to write"></button>` : `<div class="disp" tabindex="0" role="button" aria-label="Line ${i + 1}: ${att(t)}">${fmtLine(t, k)}</div>`}</div>
    ${CANWRITE ? `<button class="rt" data-rt="${i}" aria-expanded="${i === ED.tools}" title="Line tools" aria-label="Tools for line ${i + 1}">⋯</button><div class="ra">${acts.map(a => `<button class="rb${a === 'del' ? ' danger' : ''}" data-ra="${a}" title="${RB_LABEL[a]}${['up', 'down', 'dup', 'del'].includes(a) ? blk : ''}" aria-label="${RB_LABEL[a]}, line ${i + 1}">${svgI(a)}</button>`).join('')}</div>` : ''}
  </div>`;
}
function renderLines() {
  if (ED.mode !== 'outline') { lintBadge(); return; }
  const box = document.getElementById('lines');
  if (!box) return;
  box.innerHTML = L().map((_, i) => rowHTML(i)).join('');
  renderJumps(); lintBadge();
}
function renderJumps() {
  const el = document.getElementById('jumps');
  if (!el) return;
  const lines = L(), out = [];
  lines.forEach((raw, i) => {
    const d = depthOf(raw), t = raw.slice(d);
    if (d !== 2) return;
    const b = /^\([^)]*\):\{([^}]*)\}/.exec(t), s = /^\[([^\]]+)\]/.exec(t);
    if (b) out.push([i, b[1].replace(/~/g, ''), 'b']);
    else if (s) out.push([i, s[1], 's']);
  });
  el.innerHTML = out.map(([i, n, c]) => `<button class="jump ${c}" data-jump="${i}">${esc(n)}</button>`).join('');
}
function flashRow(i) {
  const r = document.querySelector(`.ln[data-i="${i}"]`);
  if (!r) return;
  r.scrollIntoView({ block: 'center', behavior: RM ? 'auto' : 'smooth' });
  r.classList.remove('flash'); void r.offsetWidth; r.classList.add('flash');
}
function autosize(ta) { ta.style.height = 'auto'; ta.style.height = ta.scrollHeight + 'px'; }
function startEdit(i, caret) {
  if (!CANWRITE) return;
  if (ED.act >= 0 && ED.act !== i) finishEdit();
  const row = document.querySelector(`.ln[data-i="${i}"]`);
  if (!row) return;
  const raw = L()[i], d = depthOf(raw), t = raw.slice(d);
  if (ED.tools >= 0 && ED.tools !== i) { const j = ED.tools; ED.tools = -1; const r = document.querySelector(`.ln[data-i="${j}"]`); if (r) r.outerHTML = rowHTML(j); }
  ED.tools = -1;
  ED.act = i;
  document.querySelectorAll('.ln.act').forEach(x => x.classList.remove('act'));
  row.classList.add('act');
  const h = hintFor(L(), i);
  const lc = row.querySelector('.lc');
  lc.innerHTML = `<textarea class="lin" id="lin" rows="1" spellcheck="true" aria-label="Line ${i + 1}">${esc(t).replace(/<span class="inwip">WIP<\/span>/g, '')}</textarea>${h ? `<div class="hint"><span class="lab">Template</span> ${h.note ? esc(h.note) : ''}${h.def ? ` <span class="tk-def">${esc(h.def.slice(1, -1) || 'blank')}</span>` : ''}</div>` : ''}`;
  const ta = lc.querySelector('textarea');
  ta.value = t;
  autosize(ta);
  ta.focus({ preventScroll: true });
  const p = caret === 'start' ? 0 : typeof caret === 'number' ? Math.min(caret, t.length) : t.length;
  ta.setSelectionRange(p, p);
  const r = row.getBoundingClientRect(), pane = document.getElementById('lines').getBoundingClientRect();
  if (r.top < pane.top || r.bottom > pane.bottom) row.scrollIntoView({ block: 'nearest' });
  ta.addEventListener('input', () => autosize(ta));
  ta.addEventListener('keydown', e => lineKey(e, ta));
  ta.addEventListener('blur', () => setTimeout(() => { if (ED.act === i && document.activeElement !== ta) finishEdit(); }, 0));
}
function finishEdit(keepFocus) {
  const i = ED.act;
  if (i < 0) return;
  const ta = document.getElementById('lin');
  ED.act = -1;
  if (!ta) return;
  const lines = L();
  const d = depthOf(lines[i]);
  const val = ta.value.replace(/\r?\n/g, ' ').replace(/^\t+/, '');
  const nraw = (val.trim() ? '\t'.repeat(d) : (isBlank(lines[i]) ? lines[i] : '')) + (val.trim() ? val : '');
  const changed = nraw !== lines[i];
  if (changed) { if (ED.fresh !== i) snapshot(); lines[i] = nraw; }
  ED.fresh = -1;
  const row = ta.closest('.ln');
  if (row) { row.outerHTML = rowHTML(i); }
  if (changed) touched(false);
  if (keepFocus) { const disp = document.querySelector(`.ln[data-i="${i}"] .disp`); if (disp) disp.focus({ preventScroll: true }); }
}
function lineKey(e, ta) {
  const i = ED.act, lines = L();
  const at0 = ta.selectionStart === 0 && ta.selectionEnd === 0, atEnd = ta.selectionStart === ta.value.length;
  if (e.key === 'Enter' && !e.shiftKey && !e.isComposing) {
    e.preventDefault();
    const t = ta.value, d = depthOf(lines[i]);
    const k = kindOf(t, d);
    const before = ED.undo.length;
    finishEdit();
    if (ED.undo.length === before) snapshot();
    let nd = d, pre = '';
    if (k === 'section' || k === 'building' || k === 'sub' || k === 'vessel' || k === 'upgrade') { nd = d + 1; pre = '- '; }
    else if (/^- /.test(t)) pre = '- ';
    lines.splice(i + 1, 0, '\t'.repeat(nd) + pre);
    touched(true);
    startEdit(i + 1, 'end');
    ED.fresh = i + 1;
  } else if (e.key === 'Tab') {
    e.preventDefault();
    const pos = ta.selectionStart, t = ta.value, d = depthOf(lines[i]);
    let prevD = -1;
    for (let j = i - 1; j >= 0; j--) if (!isBlank(lines[j])) { prevD = depthOf(lines[j]); break; }
    const nd = e.shiftKey ? Math.max(0, d - 1) : Math.min(d + 1, prevD + 1);
    if (nd === d) return;
    snapshot();
    lines[i] = '\t'.repeat(nd) + t;
    ED.act = -1;
    touched(true);
    startEdit(i, pos);
  } else if (e.key === 'Backspace' && ta.value === '' && i > 0) {
    e.preventDefault();
    ED.act = -1;
    snapshot(); lines.splice(i, 1);
    touched(true);
    let p = i - 1;
    startEdit(p, 'end');
  } else if (e.key === 'ArrowUp' && at0 && i > 0) { e.preventDefault(); finishEdit(); startEdit(i - 1, 'end'); }
  else if (e.key === 'ArrowDown' && atEnd && i < lines.length - 1) { e.preventDefault(); finishEdit(); startEdit(i + 1, 'start'); }
  else if (e.key === 'Escape') { e.preventDefault(); finishEdit(true); }
}

/* ------------------------------------------------ check panel */
function lintBadge() {
  const b = document.getElementById('lintn');
  if (!b) return;
  const all = lintSheet(L()), hard = all.filter(x => !x.soft);
  b.textContent = all.length ? String(all.length) : '';
  b.hidden = !all.length;
  b.className = 'badge' + (hard.length ? ' warn' : '');
  if (ED.lint) renderLint(all);
}
function renderLint(all) {
  const el = document.getElementById('lintbox');
  if (!el) return;
  all = all || lintSheet(L());
  el.hidden = !ED.lint;
  el.innerHTML = all.length
    ? `<p class="lab">${all.length} thing${all.length === 1 ? '' : 's'} to look at, against the rules in 00_Read_Me.txt</p><ul>${all.map(x => `<li><button class="linkish" data-jump="${x.i}">Line ${x.i + 1}</button> ${esc(x.m)}</li>`).join('')}</ul>`
    : '<p class="lab">The sheet follows the format rules.</p>';
}

/* ------------------------------------------------ messages and confirmations, inside the page */
let msgT;
function msg(text, kind) {
  const el = document.getElementById('edmsg');
  if (!el) return;
  el.hidden = false;
  el.className = 'edmsg' + (kind === 'err' ? ' err' : '');
  el.innerHTML = `<span>${esc(text)}</span>${kind === 'undo' ? '<button class="tb-btn sm" data-ed="undo">Undo</button>' : ''}<button class="tb-btn sm ghost" data-ed="msg-x" aria-label="Dismiss">✕</button>`;
  clearTimeout(msgT);
  if (kind !== 'err') msgT = setTimeout(() => { el.hidden = true; }, 7000);
}
function confirmBar(text, yes, onYes, danger) {
  const el = document.getElementById('edmsg');
  if (!el) return;
  clearTimeout(msgT);
  ED.bar = onYes;
  el.hidden = false; el.className = 'edmsg ask';
  el.innerHTML = `<span>${esc(text)}</span><button class="tb-btn sm ${danger ? 'danger' : 'primary'}" data-ed="bar-yes">${esc(yes)}</button><button class="tb-btn sm" data-ed="bar-no">Cancel</button>`;
}

/* ------------------------------------------------ save status */
const fmtTime = iso => { try { return new Date(iso).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' }); } catch (e) { return ''; } };
function status() {
  const el = document.getElementById('edstat'), rec = ED.rec;
  if (!el || !rec) return;
  let s, c = '';
  if (!CANWRITE) { s = 'View only: export the .txt to keep changes'; c = 'warn'; }
  else if (rec.err) { s = rec.err; c = 'warn'; }
  else if (rec.dirty || rec.saving) s = STORE === 'pending' ? 'Waiting for storage…' : 'Saving…';
  else if (STORE === 'local' && rec.savedAt) { s = 'Saved in this browser only'; c = 'warn'; }
  else if (rec.savedAt) s = 'Saved ' + fmtTime(rec.savedAt);
  else s = rec.added ? 'New sheet' : 'Published version';
  const edited = !rec.added && docToText(rec.doc) !== rec.baseText;
  el.className = 'edstat ' + c;
  el.textContent = s + (edited && !rec.dirty && !rec.saving && !rec.err ? ' · edited' : '') + sendLabel(rec);
  const sb = document.getElementById('edsend');
  if (sb) sb.hidden = !(STORE === 'db' && CANWRITE && !rec.dirty && !rec.saving && rec.remote && (rec.remote.status || 'draft') === 'draft');
  const rv = document.getElementById('edrevert');
  if (rv) { rv.hidden = rec.added ? false : !edited && rec.frameUrl === rec.baseFrame && rec.accent === rec.baseAccent[0] && rec.accent2 === rec.baseAccent[1]; rv.textContent = rec.added ? 'Delete vessel' : 'Back to published'; }
}

/* ------------------------------------------------ send to the project
   Send marks the stored sheet "waiting". A thread reads every waiting doc ("bring in my vessel edits"), folds the
   sheet into docs/scope through a reviewed PR, and writes back status "pr" (with pr) and then "merged". */
const SENDST = { waiting: 'sent, waiting for a thread', pr: 'in a pull request', merged: 'merged into the scope' };
function sendLabel(rec) {
  const r = rec.remote, s = r && r.status;
  if (!s || !SENDST[s]) return '';
  return ' · ' + SENDST[s] + (s === 'pr' && r.pr ? ' #' + r.pr : '');
}
function askSend() {
  const rec = ED.rec, el = document.getElementById('edmsg');
  if (!rec || !el) return;
  finishEdit();
  el.hidden = false; el.className = 'edmsg ask';
  el.innerHTML = `<span>Send ${esc(rec.file)} to the project? A thread folds it into the scope through a pull request once you say “bring in my vessel edits”.</span>
    <input class="fname" id="ednote" placeholder="Note for Claude (optional)" aria-label="Note for Claude" value="${att((rec.remote && rec.remote.note) || '')}">
    <button class="tb-btn sm primary" data-ed="send-yes">Send</button><button class="tb-btn sm" data-ed="msg-x">Cancel</button>`;
  document.getElementById('ednote').focus();
}
async function sendRec(rec, note) {
  if (STORE !== 'db' || !CANWRITE) return;
  clearTimeout(saveTimers[rec.id]);
  while (rec.saving) await new Promise(r => setTimeout(r, 150));
  const data = { ...recData(rec, rec.remote), status: 'waiting', sentAt: new Date().toISOString(), note: note || '', builtFrom: D.built || '' };
  rec.saving = true; status();
  try {
    await DB.doc('sheets/' + rec.id).set(data);
    rec.remote = data; rec.savedAt = data.updatedAt; rec.dirty = false;
    msg(`Sent ${rec.file}. Say “bring in my vessel edits” in the project when you are done.`);
  } catch (e) { msg('Not sent: the storage did not answer. Try again in a moment.', 'err'); }
  rec.saving = false; status();
}

/* ------------------------------------------------ the layout */
let pvT;
function schedulePreview() { clearTimeout(pvT); pvT = setTimeout(renderPreview, 260); }
function renderPreview() {
  const pv = document.getElementById('pv');
  if (!pv || !ED.rec) return;
  const v = modelOf(ED.rec);
  REG.length = 0;
  if (!v) {
    pv.innerHTML = `<div class="band pad" style="margin-top:22px"><h2 style="font-family:var(--f-display)">No vessel found in this sheet</h2><p>The page reads a vessel from a <b>| Vessel: Name |</b> line, then a <b>Name, Title</b> line one tab in. Add those and the page comes back.</p></div>`;
    return;
  }
  const y = window.scrollY;
  pv.innerHTML = renderVessel(v);
  mount(pv); wire(v, pv);
  document.getElementById('edl').setAttribute('style', vesselStyle(v));
  if (Math.abs(window.scrollY - y) > 2) window.scrollTo(0, y);
  // the dock shows the name and sprite, which an edit can change
  const slot = document.querySelector(`.slot[data-go="${ED.rec.id}"] .nm`);
  if (slot && slot.textContent !== v.name) renderDock(CUR);
}
function renderPane() {
  const box = document.getElementById('edbody');
  if (!box) return;
  document.querySelectorAll('[data-mode]').forEach(b => b.setAttribute('aria-pressed', String(b.dataset.mode === ED.mode)));
  if (ED.mode === 'outline') {
    box.innerHTML = `<div class="jumps" id="jumps" aria-label="Jump to a section"></div><div class="lines" id="lines" role="list" aria-label="Sheet lines"></div>`;
    renderLines();
  } else {
    box.innerHTML = `<div class="txtwrap"><textarea id="txt" class="txt" spellcheck="false" aria-label="The sheet as text"${CANWRITE ? '' : ' readonly'}></textarea><p class="frame-note">Tab indents one level, as in the scope files. Changes show on the page as you type.</p></div>`;
    const ta = document.getElementById('txt');
    ta.value = L().join('\n') + (ED.rec.doc.finalNL ? '\n' : '');
    let snapped = false, t;
    ta.addEventListener('keydown', e => {
      if (e.key === 'Tab') {
        e.preventDefault();
        const s = ta.selectionStart, en = ta.selectionEnd;
        if (s !== en && ta.value.slice(s, en).includes('\n')) {
          // indent or outdent every selected line
          const a = ta.value.lastIndexOf('\n', s - 1) + 1, blk = ta.value.slice(a, en);
          const nb = e.shiftKey ? blk.replace(/^\t/gm, '') : blk.replace(/^/gm, '\t');
          ta.setRangeText(nb, a, en, 'select');
        } else if (!e.shiftKey) ta.setRangeText('\t', s, en, 'end');
        ta.dispatchEvent(new Event('input'));
      }
    });
    ta.addEventListener('input', () => {
      if (!snapped) { snapshot(); snapped = true; }
      clearTimeout(t);
      t = setTimeout(() => {
        const v = ta.value, fin = v.endsWith('\n');
        ED.rec.doc.lines = (fin ? v.slice(0, -1) : v).split('\n');
        ED.rec.doc.finalNL = fin;
        touched(false);
      }, 350);
    });
    lintBadge();
  }
}
function editorShow(main, rec, changed) {
  if (ED.rec !== rec) { ED.rec = rec; ED.undo = []; ED.redo = []; ED.act = -1; }
  main.classList.add('wide');
  const v = modelOf(rec);
  main.innerHTML = `<div class="edl" id="edl" data-pane="${ED.pane}" style="${v ? vesselStyle(v) : vesselStyle(rec)}">
    <div class="edtabs" role="tablist" aria-label="Sheet or page">
      <button role="tab" data-pane="sheet" aria-selected="${ED.pane === 'sheet'}">Sheet</button><button role="tab" data-pane="page" aria-selected="${ED.pane === 'page'}">Page</button>
    </div>
    <aside class="edpane" aria-label="Sheet editor">
      <div class="edhead">
        <div class="edfile">
          <label class="lab" for="edfname">File</label>
          <input id="edfname" class="fname" value="${att(rec.file)}" spellcheck="false"${CANWRITE ? '' : ' readonly'}>
          <span class="edstat" id="edstat" role="status"></span>
        </div>
        <div class="edtools">
          <button class="tb-btn primary" data-ed="export" title="Save the sheet as a .txt in the scope format">${svgI('down')} Export .txt</button>
          <button class="tb-btn primary" id="edsend" data-ed="send" title="Pass this sheet to the project, where a thread folds it into the scope" hidden>Send to project</button>
          <button class="tb-btn" data-ed="copy" title="Copy the whole sheet as text">${svgI('dup')} Copy</button>
          <button class="tb-btn" data-ed="import" title="Load a .txt sheet">${svgI('upg')} Import</button>
          <span class="seg" role="group" aria-label="View"><button class="tb-btn" data-mode="outline" aria-pressed="true">Outline</button><button class="tb-btn" data-mode="text" aria-pressed="false">Text</button></span>
          <button class="tb-btn" data-ed="undo" title="Undo (Ctrl+Z)" aria-label="Undo">↶</button><button class="tb-btn" data-ed="redo" title="Redo (Ctrl+Shift+Z)" aria-label="Redo">↷</button>
          <button class="tb-btn" data-ed="lint" aria-pressed="${ED.lint}" title="Check the format">Check <span id="lintn" class="badge" hidden></span></button>
          <details class="more"><summary class="tb-btn">More</summary><div class="menu">
            ${CANWRITE ? `<button class="tb-btn" data-ed="add-building">Add a building</button>
            <button class="tb-btn" data-ed="add-minion">Add a minion</button>
            <button class="tb-btn" data-ed="sprite">Set the sprite (PNG)</button>
            ${rec.frameUrl ? '<button class="tb-btn" data-ed="sprite-x">Remove the sprite</button>' : ''}
            <label class="tb-btn colour">Page colour <input type="color" id="edc1" value="${att(rec.accent)}"></label>
            <label class="tb-btn colour">Second colour <input type="color" id="edc2" value="${att(rec.accent2 || rec.accent)}"></label>
            <button class="tb-btn danger" id="edrevert" data-ed="revert" hidden>Back to published</button>` : '<p class="frame-note">View only.</p>'}
          </div></details>
        </div>
        <div class="edmsg" id="edmsg" hidden></div>
        <div class="lintbox" id="lintbox" hidden></div>
      </div>
      <div class="edbody" id="edbody"></div>
      <p class="edkeys">Click a line to edit it. <kbd>Enter</kbd> new line · <kbd>Tab</kbd> indent · <kbd>Esc</kbd> done · dashed = Claude's default</p>
    </aside>
    <div class="pv" id="pv"></div>
  </div>`;
  renderPane();
  renderPreview();
  status();
  if (ED.lint) renderLint();
  if (changed) window.scrollTo(0, 0);
}
function editorHide() {
  const main = document.getElementById('main');
  if (main) main.classList.remove('wide');
  ED.act = -1;
}

/* ------------------------------------------------ storage */
let saveTimers = {};
function scheduleSave(rec) {
  clearTimeout(saveTimers[rec.id]);
  saveTimers[rec.id] = setTimeout(() => save(rec), 1200);
}
function recData(rec, prev) {
  const text = docToText(rec.doc);
  let history = (prev && prev.history) || [];
  if (prev && prev.text && prev.text !== text) {
    const last = history[0] ? Date.parse(history[0].at) : 0;
    if (!last || Date.parse(prev.updatedAt || 0) - last > 10 * 60e3) history = [{ at: prev.updatedAt, text: prev.text }, ...history].slice(0, 5);
  }
  const v = modelOf(rec);
  const data = { id: rec.id, name: v ? v.name : rec.id, file: rec.file, text, frameUrl: rec.frameUrl || null, accent: rec.accent, accent2: rec.accent2 || rec.accent, added: !!rec.added, updatedAt: new Date().toISOString(), history, status: 'draft', note: (prev && prev.note) || '' };
  while (JSON.stringify(data).length > 240000 && data.history.length) data.history.pop();
  return data;
}
async function save(rec) {
  if (!CANWRITE) return;
  if (STORE === 'pending') { scheduleSave(rec); return; }
  if (rec.saving) { scheduleSave(rec); return; }
  rec.saving = true; rec.dirty = false; rec.err = null; status();
  const data = recData(rec, rec.remote);
  try {
    if (STORE === 'db') {
      try { await DB.doc('sheets/' + rec.id).set(data); }
      catch (e) {
        if (e && e.code === 'unavailable') { await new Promise(r => setTimeout(r, 900 + Math.random() * 900)); await DB.doc('sheets/' + rec.id).set(data); }
        else throw e;
      }
    } else {
      const all = localAll(); all[rec.id] = data; localPut(all);
    }
    rec.remote = data; rec.savedAt = data.updatedAt;
  } catch (e) {
    rec.dirty = true;
    const code = e && e.code;
    if (code === 'invalid_argument') { CANWRITE = false; rec.err = null; msg('This page is view only for you, so edits are not saved here. Export the .txt to keep them.', 'err'); }
    else if (code === 'quota_exceeded') rec.err = 'Not saved: the page\'s storage is full';
    else rec.err = 'Not saved yet: the storage did not answer';
  }
  rec.saving = false;
  if (ED.rec === rec) status();
}
function localAll() { try { return JSON.parse(localStorage.getItem('rw-vessel-sheets-v1') || '{}') || {}; } catch (e) { return {}; } }
function localPut(all) { try { localStorage.setItem('rw-vessel-sheets-v1', JSON.stringify(all)); } catch (e) { throw { code: 'local' }; } }
async function removeStored(rec) {
  try {
    if (STORE === 'db') await DB.doc('sheets/' + rec.id).delete();
    else { const all = localAll(); delete all[rec.id]; localPut(all); }
  } catch (e) { msg('Could not remove the saved copy. Try again in a moment.', 'err'); return false; }
  rec.remote = null; rec.savedAt = null;
  return true;
}
/* a stored sheet (db or this browser) into the page's records */
function applyStored(data, removed) {
  if (!data || !data.id) return false;
  let rec = RECS.find(r => r.id === data.id);
  if (rec && (rec.dirty || rec.saving)) return false;
  if (removed) {
    if (!rec) return false;
    if (rec.added) { RECS.splice(RECS.indexOf(rec), 1); MODELS.delete(rec.id); return true; }
    rec.doc = textToDoc(rec.baseText); rec.frameUrl = rec.baseFrame; [rec.accent, rec.accent2] = rec.baseAccent; rec.remote = null; rec.savedAt = null; rec.ver++;
    return true;
  }
  if (!rec) {
    rec = { id: data.id, file: data.file, doc: textToDoc(data.text), baseText: null, baseFrame: null, frameUrl: data.frameUrl || null,
      accent: data.accent || '#3f8f96', accent2: data.accent2 || data.accent || '#3f8f96', baseAccent: ['#3f8f96', '#3f8f96'], added: true, ver: 0 };
    RECS.push(rec);
  } else {
    if (rec.remote && rec.remote.updatedAt === data.updatedAt) { const st = (rec.remote.status || '') + (rec.remote.pr || ''); rec.remote = data; return st !== (data.status || '') + (data.pr || '') ? 'status' : false; }
    if (docToText(rec.doc) === data.text && rec.frameUrl === (data.frameUrl || null) && rec.accent === data.accent && rec.file === data.file) { rec.remote = data; rec.savedAt = data.updatedAt; return 'status'; }
    rec.doc = textToDoc(data.text); rec.file = data.file || rec.file; rec.frameUrl = data.frameUrl || null;
    rec.accent = data.accent || rec.accent; rec.accent2 = data.accent2 || rec.accent2; rec.ver++;
  }
  rec.remote = data; rec.savedAt = data.updatedAt;
  return true;
}
function afterRemote(changed) {
  if (!changed || changed === 'status') { if (ED.rec) status(); return; }   // a status-only change keeps the open bar and undo
  if (ED.rec && !RECS.includes(ED.rec)) { ED.rec = null; show('lineup', true); return; }
  if (ED.rec && CUR === ED.rec.id) {
    if (ED.act >= 0) return; // never pull a line out from under the cursor
    ED.undo = []; ED.redo = [];
    show(CUR, false);
  } else show(CUR, false);
}
async function initStore() {
  const c = window.claude;
  let db = null;
  try { db = c && c.use ? await c.use('db') : null; } catch (e) { db = null; }
  if (!db) {
    STORE = 'local';
    let ch = false;
    Object.values(localAll()).forEach(d => { ch = applyStored(d) || ch; });
    afterRemote(ch);
    RECS.forEach(r => { if (r.dirty) scheduleSave(r); });
    return;
  }
  DB = db; STORE = 'db';
  try {
    const user = await c.use('user');
    if (user && user.can) { const w = await user.can('data.write'); if (w === false) { CANWRITE = false; show(CUR, false); } }
  } catch (e) { /* not told: keep the inputs */ }
  db.collection('sheets').onSnapshot(snap => {
    let ch = false;
    snap.docChanges().forEach(x => { const r = applyStored(x.doc.data ? x.doc.data() : null, x.type === 'removed'); ch = r === true || ch === true ? true : (r || ch); });
    afterRemote(ch);
  }, e => { if (e && e.code !== 'revoked') msg('Live updates stopped. Reload the page to see edits made elsewhere.', 'err'); });
  RECS.forEach(r => { if (r.dirty) scheduleSave(r); });
}

/* ------------------------------------------------ export, copy, import */
async function exportTxt() {
  finishEdit();
  const rec = ED.rec, text = docToText(rec.doc);
  if (DLS === undefined) { try { DLS = window.claude && window.claude.use ? await window.claude.use('downloads') : null; } catch (e) { DLS = null; } }
  if (DLS) {
    try { await DLS.save({ filename: rec.file, data: text }); msg(`Saved ${rec.file}.`); return; }
    catch (e) {
      if (e && e.code === 'declined') return;
      if (e && e.code === 'rate_limited') return msg('A save is already waiting for your answer.');
    }
  }
  exportFallback(text);
}
function exportFallback(text) {
  const el = document.getElementById('lintbox');
  ED.lint = false;
  el.hidden = false;
  el.innerHTML = `<p class="lab">Saving files isn't available here. Copy the text into ${esc(ED.rec.file)} instead.</p><textarea class="txt small" id="exptxt" readonly aria-label="The sheet as text">${esc(text).replace(/<span class="inwip">WIP<\/span>/g, '')}</textarea><div class="edtools"><button class="tb-btn primary" data-ed="copy">Copy</button><button class="tb-btn" data-ed="lint-x">Close</button></div>`;
  document.getElementById('exptxt').value = text;
}
async function copyTxt() {
  finishEdit();
  const text = docToText(ED.rec.doc);
  try { await navigator.clipboard.writeText(text); msg('Copied the sheet.'); }
  catch (e) {
    exportFallback(text);
    const ta = document.getElementById('exptxt'); ta.focus(); ta.select();
    msg('Copying was blocked here, so the text is selected below: press Ctrl+C.');
  }
}
function fileFor(name, upload) {
  const m = /^(\d\d_Vessel_[A-Za-z0-9_]+?)\d*\.txt$/i.exec(upload || '');
  if (m) return m[1] + '.txt';
  const used = RECS.map(r => +(/^(\d\d)_/.exec(r.file) || [])[1] || 0);
  const n = Math.max(16, ...used) + 1;
  return `${n}_Vessel_${name.replace(/[^A-Za-z0-9]+/g, '_')}.txt`;
}
function importText(text, upload) {
  const doc = textToDoc(text);
  const name = sheetName(doc.lines) || ((parseSheet(doc.lines)[0] || {}).name);
  if (!name) return msgAnywhere(`${upload || 'That file'} has no | Vessel: Name | line, so it isn't a vessel sheet.`, 'err');
  const id = slug(name);
  if (BASE.some(b => b.id === id)) return msgAnywhere(`${name} is a base game vessel. Its page reads the game's own data, so it can't be replaced here.`, 'err');
  const rec = RECS.find(r => r.id === id);
  if (rec) {
    show(id, true);
    confirmBar(`Replace ${name}'s sheet with ${upload || 'the imported text'}? Undo brings the current one back.`, 'Replace', () => {
      snapshot();
      rec.doc = doc; rec.ver++; rec.dirty = true;
      ED.act = -1; renderPane(); touched(true);
      msg(`Imported ${upload || 'the sheet'}.`, 'undo');
    });
    return;
  }
  const v = parseSheet(doc.lines)[0];
  RECS.push({ id, file: fileFor(name, upload), doc, baseText: null, baseFrame: null, frameUrl: null, accent: '#3f8f96', accent2: '#3f8f96', baseAccent: ['#3f8f96', '#3f8f96'], added: true, ver: 0, dirty: true });
  scheduleSave(RECS[RECS.length - 1]);
  show(id, true);
  msg(`Added ${v ? v.name : name} from ${upload || 'the imported text'}.`);
}
function msgAnywhere(text, kind) {
  if (document.getElementById('edmsg')) return msg(text, kind);
  const el = document.getElementById('newerr');
  if (el) { el.hidden = false; el.textContent = text; }
}
function pickFile(accept, cb, asData) {
  const inp = document.createElement('input');
  inp.type = 'file'; inp.accept = accept; inp.hidden = true;
  document.body.appendChild(inp);
  inp.addEventListener('change', () => {
    const f = inp.files && inp.files[0];
    inp.remove();
    if (!f) return;
    const r = new FileReader();
    r.onload = () => cb(r.result, f.name);
    r.onerror = () => msgAnywhere('That file could not be read.', 'err');
    if (asData) r.readAsDataURL(f); else r.readAsText(f);
  });
  inp.click();
}
function setSprite(url) {
  const img = new Image();
  img.onload = () => {
    if (img.width > 256 || img.height > 256) return msg('Use one frame (32 × 32), not a whole sheet: this image is ' + img.width + ' × ' + img.height + '.', 'err');
    const cv = document.createElement('canvas'); cv.width = img.width; cv.height = img.height;
    cv.getContext('2d').drawImage(img, 0, 0);
    ED.rec.frameUrl = cv.toDataURL('image/png'); ED.rec.ver++; ED.rec.dirty = true;
    scheduleSave(ED.rec); renderDock(CUR); renderPreview(); status();
  };
  img.onerror = () => msg('That image could not be read. Use a PNG.', 'err');
  img.src = url;
}

/* ------------------------------------------------ the New vessel panel */
function newVesselPanel() {
  const ov = document.createElement('div');
  ov.className = 'overlay';
  ov.innerHTML = `<form class="newv band pad" id="newv" role="dialog" aria-modal="true" aria-labelledby="newvh">
    <h2 id="newvh">New vessel</h2>
    <p class="frame-note">Starts a sheet from the Vessel template in 15_Templates.txt, with Claude's usual defaults marked ~like this~.</p>
    <label class="lab" for="nvname">Name</label><input id="nvname" class="fname" required autocomplete="off" placeholder="Uloe">
    <label class="lab" for="nvtitle">Title</label><input id="nvtitle" class="fname" autocomplete="off" placeholder="The Lighthouse Keeper">
    <p class="err" id="newerr" hidden></p>
    <div class="edtools"><button class="tb-btn primary" type="submit">Start the sheet</button><button class="tb-btn" type="button" data-nv="import">Import a .txt instead</button><button class="tb-btn" type="button" data-nv="x">Cancel</button></div>
  </form>`;
  document.body.appendChild(ov);
  const close = () => ov.remove();
  ov.addEventListener('click', e => { if (e.target === ov || e.target.closest('[data-nv="x"]')) close(); });
  ov.querySelector('[data-nv="import"]').addEventListener('click', () => pickFile('.txt,text/plain', (t, n) => { close(); importText(t, n); }));
  ov.addEventListener('keydown', e => { if (e.key === 'Escape') close(); });
  ov.querySelector('form').addEventListener('submit', e => {
    e.preventDefault();
    const name = document.getElementById('nvname').value.trim().replace(/[|,{}\[\]~]/g, '');
    const title = document.getElementById('nvtitle').value.trim().replace(/[|~]/g, '') || 'Title';
    const err = document.getElementById('newerr');
    if (!name) { err.hidden = false; err.textContent = 'Give the vessel a name.'; return; }
    const id = slug(name);
    if (BASE.some(b => b.id === id) || RECS.some(r => r.id === id)) { err.hidden = false; err.textContent = `There is already a vessel called ${name}.`; return; }
    const rec = { id, file: fileFor(name), doc: textToDoc(newSheet(name, title)), baseText: null, baseFrame: null, frameUrl: null, accent: '#3f8f96', accent2: '#3f8f96', baseAccent: ['#3f8f96', '#3f8f96'], added: true, ver: 0, dirty: true };
    RECS.push(rec);
    close();
    scheduleSave(rec);
    show(id, true);
  });
  document.getElementById('nvname').focus();
}

/* ------------------------------------------------ events */
document.addEventListener('click', e => {
  const t = e.target;
  if (t.closest('[data-act="new-vessel"]')) { if (CANWRITE) newVesselPanel(); return; }
  const pane = t.closest('.edtabs [data-pane]');
  if (pane) { ED.pane = pane.dataset.pane; const l = document.getElementById('edl'); l.dataset.pane = ED.pane; document.querySelectorAll('.edtabs [data-pane]').forEach(b => b.setAttribute('aria-selected', String(b === pane))); if (ED.pane === 'page') renderPreview(); return; }
  if (!ED.rec || !document.getElementById('edl')) return;
  const jump = t.closest('[data-jump]');
  if (jump) { if (ED.mode !== 'outline') { ED.mode = 'outline'; renderPane(); } flashRow(+jump.dataset.jump); return; }
  const md = t.closest('[data-mode]');
  if (md) { finishEdit(); ED.mode = md.dataset.mode; renderPane(); return; }
  const rt = t.closest('[data-rt]');
  if (rt) {
    const i = +rt.dataset.rt, was = ED.tools;
    if (ED.act >= 0) finishEdit();
    ED.tools = was === i ? -1 : i;
    [was, ED.tools].forEach(j => { const r = document.querySelector(`.ln[data-i="${j}"]`); if (j >= 0 && r) r.outerHTML = rowHTML(j); });
    const nb = document.querySelector(`.ln[data-i="${i}"] .rt`); if (nb) nb.focus({ preventScroll: true });
    return;
  }
  const rb = t.closest('[data-ra]');
  if (rb) { const i = +rb.closest('.ln').dataset.i; if (ED.act >= 0 && ED.act !== i) finishEdit(); else if (ED.act === i) finishEdit(); rowAction(rb.dataset.ra, i); return; }
  const disp = t.closest('.ln .disp');
  if (disp && CANWRITE) {
    const i = +disp.closest('.ln').dataset.i;
    let caret = 'end';
    try {
      const raw = L()[i].trim();
      const sel = window.getSelection && window.getSelection();
      if (sel && sel.anchorNode && disp.contains(sel.anchorNode)) {
        const r = document.createRange(); r.setStart(disp, 0); r.setEnd(sel.anchorNode, sel.anchorOffset);
        const shown = r.toString();
        const guess = raw.indexOf(shown.slice(-12));
        if (shown.length > 3 && guess >= 0) caret = guess + Math.min(12, shown.length);
      }
    } catch (x) { /* end of line, then */ }
    startEdit(i, caret);
    return;
  }
  const ed = t.closest('[data-ed]');
  if (!ed) return;
  const a = ed.dataset.ed;
  if (a !== 'bar-yes' && a !== 'bar-no') document.querySelectorAll('details.more[open]').forEach(d => { if (!d.contains(ed) || ed.tagName === 'BUTTON') d.open = false; });
  if (a === 'export') exportTxt();
  else if (a === 'send') askSend();
  else if (a === 'send-yes') { const n = document.getElementById('ednote'); const note = n ? n.value.trim() : ''; document.getElementById('edmsg').hidden = true; sendRec(ED.rec, note); }
  else if (a === 'copy') copyTxt();
  else if (a === 'import') pickFile('.txt,text/plain', (txt, n) => importText(txt, n));
  else if (a === 'undo') { finishEdit(); undo(false); }
  else if (a === 'redo') { finishEdit(); undo(true); }
  else if (a === 'lint') { ED.lint = !ED.lint; ed.setAttribute('aria-pressed', String(ED.lint)); renderLint(); }
  else if (a === 'lint-x') { ED.lint = false; document.getElementById('lintbox').hidden = true; }
  else if (a === 'msg-x') document.getElementById('edmsg').hidden = true;
  else if (a === 'add-building') { finishEdit(); insertBlock('building'); }
  else if (a === 'add-minion') { finishEdit(); insertBlock('minion'); }
  else if (a === 'sprite') pickFile('image/png,image/gif,image/webp', (u) => setSprite(u), true);
  else if (a === 'sprite-x') { ED.rec.frameUrl = null; ED.rec.ver++; ED.rec.dirty = true; scheduleSave(ED.rec); show(CUR, false); }
  else if (a === 'revert') {
    const rec = ED.rec;
    if (rec.added) confirmBar(`Delete ${modelOf(rec) ? modelOf(rec).name : rec.id} and its sheet from this page? Export the .txt first if you want to keep it.`, 'Delete', async () => {
      clearTimeout(saveTimers[rec.id]); rec.dirty = false;
      if (await removeStored(rec)) { RECS.splice(RECS.indexOf(rec), 1); MODELS.delete(rec.id); ED.rec = null; show('lineup', true); }
    }, true);
    else confirmBar('Go back to the published sheet? Your edits here are removed; Undo keeps them for this visit.', 'Go back', async () => {
      clearTimeout(saveTimers[rec.id]);
      snapshot();
      rec.doc = textToDoc(rec.baseText); rec.frameUrl = rec.baseFrame; [rec.accent, rec.accent2] = rec.baseAccent; rec.ver++; rec.dirty = false;
      await removeStored(rec);
      show(CUR, false);
    }, true);
  }
  else if (a === 'bar-yes') { const f = ED.bar; ED.bar = null; document.getElementById('edmsg').hidden = true; if (f) f(); }
  else if (a === 'bar-no') { ED.bar = null; document.getElementById('edmsg').hidden = true; }
});
document.addEventListener('keydown', e => {
  if (!ED.rec || !document.getElementById('edl')) return;
  const disp = e.target.closest && e.target.closest('.ln .disp');
  if (disp && (e.key === 'Enter' || e.key === 'F2')) { e.preventDefault(); startEdit(+disp.closest('.ln').dataset.i, 'end'); return; }
  const inField = /^(TEXTAREA|INPUT)$/.test(e.target.tagName);
  if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === 'z' && !inField) { e.preventDefault(); undo(e.shiftKey); }
});
document.addEventListener('change', e => {
  if (!ED.rec) return;
  if (e.target.id === 'edfname') {
    let f = e.target.value.trim().replace(/[\\/:*?"<>|]+/g, '_');
    if (!f) f = ED.rec.file; if (!/\.txt$/i.test(f)) f += '.txt';
    e.target.value = f; ED.rec.file = f; ED.rec.dirty = true; scheduleSave(ED.rec); status();
  }
});
document.addEventListener('input', e => {
  if (!ED.rec || !/^edc[12]$/.test(e.target.id)) return;
  if (e.target.id === 'edc1') ED.rec.accent = e.target.value; else ED.rec.accent2 = e.target.value;
  ED.rec.ver++; ED.rec.dirty = true; scheduleSave(ED.rec); schedulePreview(); status();
});
document.addEventListener('paste', e => {
  // a whole sheet pasted on the lineup page becomes an import
  if (CUR !== 'lineup' || /^(TEXTAREA|INPUT)$/.test(e.target.tagName)) return;
  const t = e.clipboardData && e.clipboardData.getData('text');
  if (t && /\|\s*Vessel:/.test(t)) { e.preventDefault(); importText(t, 'the pasted sheet'); }
});
