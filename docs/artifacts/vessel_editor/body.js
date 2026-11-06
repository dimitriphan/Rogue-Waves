/* ---------------------------------------------------------------- a Rogue Waves vessel's own frame, or a taped gap */
function frameArt(v, size, cls = '', style = '') {
  if (v.frameUrl) return `<img class="px ${cls}" src="${v.frameUrl}" width="${size}" height="${size}" alt="${att(v.name)}, front frame drawn by Dimitri"${style ? ` style="${style}"` : ''}>`;
  return `<span class="noframe ${cls}" style="width:${size}px;height:${size}px;${style}" role="img" aria-label="${att(v.name)}: no sprite yet"><span class="inwip">Sprite WIP</span></span>`;
}

/* ---------------------------------------------------------------- asset roll (what a vessel ships with) */
function roll(v) {
  const base = v.kind === 'base', a = v.anims || {};
  const G = (s, label, meta, prev, extra = {}) => Object.assign({ s, label, meta, prev }, extra);
  const anim = (k, face = 0, sc = 2) => a[k] ? spr(a[k], sc, face) : '';
  const unitsN = v.units.length;
  const upN = v.buildings.reduce((n, b) => n + b.upgrades.length, 0);
  const special = { Boutman: ['Fishing, KO', anim('Fishing')], Buster: ['Dash run', anim('Dash')] }[v.name];
  const R = [];
  if (base) {
    R.push(['Sprites', [
      G('got', 'Front frame', 'Idle, facing the camera', anim('Idle')),
      G('got', 'Idle', '4 facings × 6 frames, 32 px', anim('Idle', 1)),
      G('got', 'Move', '4 facings × 6 frames', anim('Move')),
      G('got', 'Back view', 'Facings 3 and 4, drawn, never mirrored', anim('Idle', 2)),
      a.Attack ? G('got', 'Attack', `${a.Attack.f} frames × 4 facings`, anim('Attack')) : G('none', 'Attack', v.attackNote || 'Swings in Idle', ''),
      special ? G('got', 'Special animation', special[0], special[1]) : G('none', 'Special animation', 'No ability of its own', ''),
      G('got', 'Loading-screen fall', `${a.Fall.f} frames`, anim('Fall')),
      G('got', 'Match-start landing', `${a.Land.f} frames`, anim('Land')),
      G('got', 'Boat mini', '16 px, 4 facings × 12 frames', anim('Mini', 0, 4)),
    ]]);
    R.push(['Menu and UI art', [
      a.Readyup ? G('got', 'Ready-up icon', '12 × 12', spr(a.Readyup, 5)) : G('game', 'Ready-up icon', '12 × 12, in game', ''),
      G('game', 'Vessel panel splash', '128 × 128 line art, in game', ''),
      G('game', 'End-screen faces', '32 × 32, happy and defeated, in game', ''),
      G('game', 'Journal tab icon', '32 × 32 white line icon, in game', ''),
      (v.sig.length ? G('game', 'Ability and resource icons', '12 × 12, in game', '') : G('none', 'Ability and resource icons', 'No ability or resource', '')),
    ]]);
    R.push(['World', [
      G('game', 'Tower models', `${v.buildings.filter(b => !b.unit).length} low-poly towers, in game`, ''),
      G('game', 'Upgrade models', 'One added feature per upgrade, in game', ''),
      G('got', 'Unit sprites', `${unitsN} unit${unitsN === 1 ? '' : 's'}${v.units.some(u => u.variants && u.variants.length) ? ', plus upgrade looks' : ''}`, v.units[0] && v.units[0].sprite ? spr(v.units[0].sprite, v.units[0].sprite.c > 16 ? 2 : 4) : ''),
      G('none', 'Direction icons', 'Directions are Rogue Waves content', ''),
    ]]);
    R.push(['Design and words', [
      G('got', 'Look', v.look, ''), G('got', 'Archetype', 'One line on what the player does', ''),
      G('got', 'Vessel stats', 'Every number set', ''), G('got', 'Buildings and upgrades', `${v.buildings.filter(b => !b.unit).length} buildings, ${upN} upgrades, every cost set`, ''),
      G('got', 'Select-screen line', v.line, ''), G(v.rule && !v.rule.none ? 'got' : 'none', 'Special rule tooltip', v.rule && !v.rule.none ? v.rule.name : 'None', ''),
      G('got', 'Lore', 'Journal text', ''), G('game', 'Sounds', 'Attacks, footsteps, death, in game', ''),
    ]]);
  } else {
    const frame = v.frameUrl ? frameArt(v, 64) : '';
    const dirsN = v.directions.length;
    const towersN = v.buildings.filter(b => !b.unit).length;
    const setBars = v.bars.filter(b => b.v && !b.v.includes('')).length;
    const costsOpen = v.buildings.reduce((n, b) => n + (b.cost ? 0 : 1) + b.upgrades.filter(u => !u.cost).length, 0);
    const icons = [v.ability, v.resource, v.mechanic].filter(Boolean);
    R.push(['Sprites', [
      v.frameUrl ? G('mine', 'Front frame', 'Drawn by Dimitri: one 32 px frame', frame) : G('wip', 'Front frame', 'Not drawn', ''),
      G('wip', 'Idle', '4 facings × 6 frames', ''),
      G('wip', 'Move', 'Not drawn', ''),
      G('wip', 'Back view', 'Facings 3 and 4 not drawn', ''),
      G('wip', 'Attack', v.attack ? cap(v.attack) : 'Not drawn', ''),
      v.ability ? G('wip', 'Special animation', v.ability, '') : G('none', 'Special animation', 'No ability of its own', ''),
      G('wip', 'Loading-screen fall', '12 frames', ''),
      G('wip', 'Match-start landing', '12 frames', ''),
      G('wip', 'Boat mini', '16 px, 4 facings', ''),
    ]]);
    R.push(['Menu and UI art', [
      G('wip', 'Ready-up icon', '12 × 12', ''), G('wip', 'Vessel panel splash', '128 × 128 line art', ''),
      G('wip', 'End-screen faces', '32 × 32, happy and defeated', ''), G('wip', 'Journal tab icon', '32 × 32 white line icon', ''),
      icons.length ? G('wip', 'Ability and resource icons', icons.join(', '), '') : G('none', 'Ability and resource icons', 'No ability or resource', ''),
    ]]);
    R.push(['World', [
      G(v.towersLook ? 'part' : 'wip', 'Tower models', v.towersLook ? 'Motif written, models not made' : `${towersN} towers, motif not set`, ''),
      G('wip', 'Upgrade models', 'One added feature per upgrade', ''),
      unitsN ? G('wip', 'Unit sprites', `${unitsN} unit${unitsN === 1 ? '' : 's'} described, none drawn`, '') : G('none', 'Unit sprites', 'No units', ''),
      dirsN ? G('part', 'Direction icons', `${dirsN} icon${dirsN === 1 ? '' : 's'} described by Dimitri, none drawn`, '') : G('wip', 'Direction icons', 'No Directions yet', ''),
    ]]);
    R.push(['Design and words', [
      v.look ? G('mine', 'Look', 'Written by Dimitri', '') : G('wip', 'Look', 'Not written', ''),
      v.archetype ? G('mine', 'Archetype', 'Written by Dimitri', '') : G('wip', 'Archetype', 'Not written', ''),
      G(setBars === v.bars.length ? 'mine' : setBars ? 'part' : 'wip', 'Vessel stats', `${setBars} of ${v.bars.length} set by Dimitri`, ''),
      G(towersN && !costsOpen ? 'mine' : towersN ? 'part' : 'wip', 'Buildings and upgrades', `${towersN} buildings, ${upN} upgrades${costsOpen ? `; ${costsOpen} cost${costsOpen === 1 ? '' : 's'} open` : ''}`, ''),
      v.line ? G('mine', 'Select-screen line', v.line, '') : G('wip', 'Select-screen line', 'Not written', ''),
      v.rule && v.rule.name && v.rule.text ? G('mine', 'Special rule tooltip', v.rule.name, '') : G('wip', 'Special rule tooltip', 'Not written', ''),
      v.lore ? G('mine', 'Lore', 'Written by Dimitri', '') : G('wip', 'Lore', v.loreNote || 'Comes later', ''),
      G('wip', 'Sounds', 'None yet', ''),
    ]]);
  }
  return R;
}
function readiness(v) {
  let have = 0, part = 0, need = 0;
  roll(v).forEach(([, items]) => items.forEach(it => {
    if (it.s === 'none') return; need++;
    if (it.s === 'got' || it.s === 'game' || it.s === 'mine') have++; else if (it.s === 'part') part++;
  }));
  return { have, part, need };
}
const ST = { got: 'Shown here', game: 'In game', none: 'Not needed', mine: "Dimitri's", part: 'Partly done', wip: 'WIP' };

/* ---------------------------------------------------------------- lineup page */
function renderLineup() {
  const NEW = NEWV();
  const all = [...BASE, ...NEW];
  const cards = all.map(v => {
    const r = readiness(v); const pa = Math.round(100 * r.have / r.need), pb = Math.round(100 * r.part / r.need);
    const art = v.kind === 'base' ? spr(v.anims.Idle, 3) : frameArt(v, 96);
    return `<button class="vcard" style="--cc:${v.accent}" data-go="${v.id}">
      <div class="vart">${art}</div>
      <div class="vb"><span class="lab">${v.kind === 'base' ? 'Base game' : 'Rogue Waves'}${v.difficulty ? ' · ' + esc(v.difficulty) : ''}</span><b>${esc(v.name)}</b><span class="tt">${esc(v.title)}</span>
      <span class="mini-meter" title="${r.have} done, ${r.part} partly, of ${r.need}"><i style="width:${pa}%"></i><i style="width:${pb}%;opacity:.4"></i></span>
      <span class="tt num">${r.have + r.part} of ${r.need} started</span></div></button>`;
  }).join('') + (EDIT ? `<button class="vcard addcard" data-act="new-vessel"><div class="vart">${icon('units', 40)}</div><div class="vb"><span class="lab">From the template</span><b>New vessel</b><span class="tt">Start a sheet, or import a .txt</span></div></button>` : '');
  const rolls = all.map(v => roll(v));
  const groups = rolls[0].map(g => g[0]);
  let rows = '';
  groups.forEach((gname, gi) => {
    rows += `<tr class="gh"><td colspan="${all.length + 1}">${esc(gname)}</td></tr>`;
    rolls[0][gi][1].forEach((it, ii) => {
      rows += `<tr><td>${esc(it.label)}</td>${all.map((v, vi) => { const x = rolls[vi][gi][1][ii]; return `<td style="--cc:${v.accent}"><span class="cell ${x.s}" title="${att(x.meta)}">${esc(ST[x.s])}</span></td>`; }).join('')}</tr>`;
    });
  });
  const head = all.map(v => `<th style="--cc:${v.accent}">${v.kind === 'base' ? spr(v.anims.Mini, 2) : frameArt(v, 32, '', 'display:block;margin:0 auto 2px')}${esc(v.name)}</th>`).join('');
  const words = ['', 'One', 'Two', 'Three', 'Four', 'Five', 'Six', 'Seven', 'Eight', 'Nine', 'Ten', 'Eleven', 'Twelve'];
  const names = NEW.map(v => v.name);
  const nameList = names.length > 1 ? names.slice(0, -1).join(', ') + ' and ' + names[names.length - 1] : names[0] || 'The new vessels';
  return `<div class="page">
    <section class="intro">
      <span class="lab">${words[all.length] || all.length} vessels</span>
      <h1>The base five set the bar. ${esc(nameList)} ${names.length === 1 ? 'is' : 'are'} catching up.</h1>
      <p>Each base vessel's page shows what a finished vessel ships with: every sprite, its stats, its four buildings and their upgrade trees, its units and its lore. ${esc(nameList)} use${names.length === 1 ? 's' : ''} the same pages with only Dimitri's own art and design in them. Everything still missing sits under a work-in-progress cover.${EDIT ? ' Open a Rogue Waves vessel to edit its sheet beside its page.' : ''}</p>
    </section>
    <section class="cards">${cards}</section>
    <section>
      <div class="sec-h"><h2>Asset sheet</h2><span class="lab">What each vessel has, side by side</span></div>
      <div class="matrix-wrap"><table class="matrix"><thead><tr><th>Asset</th>${head}</tr></thead><tbody>${rows}</tbody></table></div>
      <p class="scale-note">"In game" marks base assets that ship with the game but aren't pulled onto this page (splash art, tower models, sounds). "Shown here" means the sheet itself is on the vessel's page.</p>
    </section>
  </div>`;
}

/* ---------------------------------------------------------------- vessel page */
function hero(v) {
  const base = v.kind === 'base';
  const a = v.anims || {};
  const ANIMS = ['Idle', 'Move', 'Attack', 'Fishing', 'KO', 'Dash', 'Land', 'Fall'];
  const label = { Land: 'Landing', Fall: 'Falling' };
  let chips, faces, sprite, note;
  if (base) {
    chips = ANIMS.filter(k => a[k]).map((k, i) => `<button class="chip" aria-pressed="${i === 0}" data-anim="${k}">${label[k] || k}</button>`).join('');
    faces = ['Front ▸', '◂ Front', 'Back ▸', '◂ Back'].map((f, i) => `<button class="chip" aria-pressed="${i === 0}" data-facing="${i}">${f}</button>`).join('');
    sprite = spr(a.Idle, 6, 0, 'hero-spr');
    note = `The game's own sheet at 6× zoom, ${a.Idle.fps} frames a second.`;
  } else {
    const ks = ['Move', 'Attack', v.ability, 'Landing', 'Falling'].filter(Boolean);
    chips = `<button class="chip" aria-pressed="true">Idle</button>` + ks.map(k => `<button class="chip" disabled>${esc(k)}<span class="w">WIP</span></button>`).join('');
    faces = `<button class="chip" aria-pressed="true">Front ▸</button>` + ['◂ Front', 'Back ▸', '◂ Back'].map(f => `<button class="chip" disabled>${f}<span class="w">WIP</span></button>`).join('');
    sprite = frameArt(v, 192, 'hero-spr', 'position:absolute');
    note = v.frameUrl ? "Dimitri's own frame at 6× zoom. One frame of the 48 a vessel's main sheet needs." : 'No sprite yet. A vessel\'s main sheet needs 48 frames.';
  }
  const isle = `<svg class="isle" viewBox="0 0 460 190" aria-hidden="true">
      <g class="waves"><path d="M-20 150 Q 20 142 60 150 T 140 150 T 220 150 T 300 150 T 380 150 T 460 150 T 540 150 V 190 H -20 Z" fill="var(--water)" opacity=".75"/><path d="M-20 162 Q 25 154 70 162 T 160 162 T 250 162 T 340 162 T 430 162 T 520 162 V 190 H -20 Z" fill="var(--water2)" opacity=".8"/></g>
      <polygon points="110,96 150,78 310,78 350,96 312,116 148,116" fill="var(--grass)"/>
      <polygon points="350,96 312,116 296,150 340,128" fill="var(--dirt2)"/>
      <polygon points="148,116 312,116 296,150 244,172 170,156" fill="var(--dirt)"/>
      <polygon points="110,96 148,116 170,156 126,128" fill="var(--dirt3)"/>
      <polygon points="296,150 244,172 236,186 286,162" fill="var(--dirt3)"/>
      <polygon points="148,116 312,116 314,112 146,112" fill="var(--grass2)"/>
      <path d="M100 152 Q 140 142 180 150" stroke="var(--foam)" stroke-width="3" fill="none" stroke-linecap="round" opacity=".7"/>
      <path d="M282 156 Q 320 146 360 152" stroke="var(--foam)" stroke-width="3" fill="none" stroke-linecap="round" opacity=".7"/>
    </svg>`;
  let rule = '';
  if (v.rule && v.rule.wip) rule = `<div class="rulebanner" style="background:none"><span class="lab">Special rule</span><span>${wipTag()} <span class="frame-note">The tooltip on the select screen isn't written yet.</span></span></div>`;
  else if (v.rule && !v.rule.none) rule = `<div class="rulebanner"><b>${v.rule.name ? esc(v.rule.name) : wipTag('Name WIP')}</b><span>${v.rule.text ? esc(v.rule.text) : wipTag('Text WIP')}</span></div>`;
  const line = v.line ? `<p class="selectline">${esc(v.line)}</p>` : `<p class="selectline" style="border-color:var(--tape)">${wipTag('Select-screen line WIP')}</p>`;
  const melee = /ranged/i.test(v.attack || '') ? 'Ranged' : /melee/i.test(v.attack || '') ? 'Melee' : null;
  const lookTxt = v.look ? `${esc(cap(v.look.replace(/\.$/, '')))}.${v.lookNote ? ` <i style="color:var(--ink2)">${esc(cap(v.lookNote.replace(/\.$/, '')))}.</i>` : ''}` : wipTag('Not written');
  const colour = base ? `<span class="swatch"></span> ${esc(v.accent)}`
    : v.accentName ? `<span class="swatch"></span> ${esc(cap(v.accentName))}` : `<span class="swatch"></span> ${wipTag('Not chosen')} <span class="frame-note">page tinted from the sprite</span>`;
  return `<section class="band hero">
    <div class="stage">
      <div class="world">${isle}${sprite}</div>
      <div class="controls">
        <div class="ctl-row"><span class="lab">Animation</span><div class="chips" data-ctl="anim">${chips}</div></div>
        <div class="ctl-row"><span class="lab">Facing</span><div class="chips" data-ctl="face">${faces}</div></div>
        <p class="frame-note">${note}</p>
      </div>
    </div>
    <div class="ident">
      <div class="eyebrow">
        <span class="pill ${base ? '' : 'solid'}">${base ? 'Base vessel' : 'Rogue Waves vessel'}</span>
        ${v.difficulty ? `<span class="pill">${pips(v.difficulty)} ${esc(v.difficulty)}</span>` : `<span class="pill">${wipTag('Difficulty')}</span>`}
        ${melee ? `<span class="pill">${melee}</span>` : ''}
        ${base ? '' : '<span class="pill tape">In development</span>'}
      </div>
      <div><h1 class="vname">${esc(v.name)}</h1><p class="vtitle">${esc(v.title)}</p></div>
      ${line}
      <p class="archetype">${v.archetype ? esc(cap(v.archetype.replace(/\.?$/, '.'))) : wipTag('Archetype WIP')}</p>
      ${rule}
      <div class="facts">
        <div class="wide"><span class="lab">Look</span><p class="v">${lookTxt}</p></div>
        <div><span class="lab">Attack</span><p class="v">${v.attack ? esc(cap(v.attack)) : wipTag()}</p></div>
        <div><span class="lab">Colour</span><p class="v">${colour}</p></div>
        ${v.towersLook ? `<div><span class="lab">Towers look</span><p class="v">${esc(v.towersLook)}</p></div>` : ''}
        ${!base && v.status ? `<div class="wide"><span class="lab">Status</span><p class="v">${esc(cap(v.status))}</p></div>` : ''}
      </div>
    </div>
  </section>`;
}

function statsCard(v) {
  const bars = v.bars.map(b => {
    if (b.v == null || b.num == null) return `<div class="bar"><span class="k">${esc(b.k)}</span><span class="track"><span class="fill" style="width:0"></span></span><span class="v">${b.v ? esc(b.v) : (v.kind === 'rw' ? wipTag() : '?')}</span></div>`;
    const pct = Math.max(3, Math.min(100, 100 * b.num / b.max));
    const shown = b.k === 'Sell rate' ? b.v : b.v.replace(' tiles', '');
    const unit = /tiles/.test(b.v) ? 'tiles' : '';
    return `<div class="bar"><span class="k">${esc(b.k)}</span><span class="track" title="0 to ${b.max}"><span class="fill" style="width:${pct}%"></span></span><span class="v num">${esc(shown)}${unit || b.x ? `<small>${esc([unit, b.x].filter(Boolean).join(', '))}</small>` : ''}</span></div>`;
  }).join('');
  const extras = v.extras.length ? `<dl class="kvs">${v.extras.map(e => `<dt>${esc(e.k)}</dt><dd>${esc(e.v)}</dd>`).join('')}</dl>` : '';
  const body = `<div class="bars">${bars}</div><dl class="kvs"><dt>Health regen</dt><dd>${v.regen ? esc(v.regen) : (v.kind === 'rw' ? wipTag() : '?')}</dd></dl>${extras}`;
  if (v.kind === 'base') {
    return `<section class="band pad c7"><div class="sec-h"><h2>Vessel stats</h2><span class="lab">Bars run from 0 to the base game's top</span></div>${body}
      <p class="scale-note">Bar ends: health 65, speed 3.5, damage 5, range 3 tiles, attack speed 0.75, build range 3 tiles, sell rate 75%.</p></section>`;
  }
  const set = v.bars.filter(b => b.v).length;
  const inner = set ? body : wipCover(body, 'WORK IN PROGRESS', 'No numbers set yet.');
  return `<section class="band pad c7"><div class="sec-h"><h2>Vessel stats</h2><span class="lab">${set} of ${v.bars.length} set · bars to the base game's top</span></div>
    ${inner}<p class="scale-note">Bar ends: health 65, speed 3.5, damage 5, range 3 tiles, attack speed 0.75, build range 3 tiles, sell rate 75%. A bar past the end fills it.</p></section>`;
}

function loreCard(v) {
  if (v.kind === 'base') {
    return `<section class="band pad c5"><div class="sec-h"><h2>Lore</h2><span class="lab">Journal entry</span></div><p class="lore">${esc(v.lore)}</p><p class="quote-src">From ${esc(v.name)}'s page in the game's journal.</p></section>`;
  }
  const ghost = `<p class="lore">${esc(v.name)} lore will sit here, a few lines in the journal's voice, the way the base vessels each have theirs.</p>`;
  const notes = (v.notes || []).map(n => `<div class="dnote">${esc(n.v)}${n.n ? `<small>${esc(n.n)}</small>` : ''}</div>`).join('');
  const lore = v.lore ? `<p class="lore">${esc(v.lore)}</p>` : wipCover(ghost, 'WORK IN PROGRESS', v.loreNote);
  return `<section class="band pad c5" style="display:grid;gap:16px;align-content:start">
    <div><div class="sec-h"><h2>Lore</h2><span class="lab">Journal entry</span></div>${lore}</div>
    ${notes ? `<div><div class="sec-h"><h2 style="font-size:19px">Designer's notes</h2><span class="lab">Dimitri's words</span></div><div class="dnotes">${notes}</div></div>` : ''}
  </section>`;
}

function sigSection(v) {
  if (!v.sig.length) {
    return `<section class="band pad c12"><div class="sec-h"><h2>Signature</h2><span class="lab">Ability, resource, rule</span></div><p>No ability, resource or special rule. Everything ${esc(v.name)} does goes through the buildings below.</p></section>`;
  }
  const cards = v.sig.map(s => {
    const rows = rowsHTML(s.rows.filter(r => !['Keys'].includes(r.k) || r.v));
    return `<div class="sig"><span class="lab">${esc(s.type)}</span><h3>${s.name ? esc(s.name) : wipTag('Name WIP')}</h3><div class="rows">${rows}</div></div>`;
  }).join('');
  return `<section class="band pad c12"><div class="sec-h"><h2>Signature</h2><span class="lab">${v.kind === 'base' ? 'Ability, resource, rule' : 'WIP = not set by Dimitri yet'}</span></div><div class="sigs">${cards}</div></section>`;
}

function upTile(u, lvl, mine) {
  const name = u.name ? `<b>${esc(u.name)}</b>` : `<b style="color:var(--ink2)">Unnamed</b>`;
  const s = u.sprite ? spr(u.sprite, u.sprite.c > 16 ? 1 : 2, 0, 'uspr') : '';
  const num = u.num ? `<span class="n">${esc(u.num.replace(/^→ Crafted /, 'See the '))}</span>` : '';
  const lead = u.leads ? `<span class="lt">${u.leads === 'end' ? 'Branch ends here' : 'Leads to ' + esc(u.leads)}</span>` : '';
  const cost = u.cost ? costChips(u.cost) : (mine ? wipTag('Cost WIP') : '');
  const line = mine && u.line ? `<span class="lt">“${esc(u.line)}”</span>` : '';
  return `<div class="up ${lvl === 4 ? 'cap4' : ''}"><div class="top"><div style="display:grid;gap:2px">${name}${!u.name ? wipTag('Name WIP') : ''}</div>${s}</div>
    <span class="d">${esc(u.desc)}</span>${u.note ? `<span class="note">${esc(u.note)}</span>` : ''}${num}${u.numnote ? `<span class="note">${esc(u.numnote)}</span>` : ''}${line}${lead}<div>${cost}</div></div>`;
}

function buildingDetail(v, b) {
  if (!b) return '<p class="frame-note">No buildings yet.</p>';
  const mine = v.kind === 'rw';
  const lv = [1, 2, 3, 4];
  const has4 = b.upgrades.some(u => u.lvl === 4);
  const cols = lv.filter(l => l < 4 || has4 || mine).map(l => {
    const ups = b.upgrades.filter(u => u.lvl === l);
    let inner = ups.map(u => upTile(u, l, mine)).join('');
    if (!ups.length && mine) {
      const nd = b.needs.find(n => new RegExp(`LEVEL[^!]*${l}`).test(n)) || (l < 4 ? b.needs[0] : null);
      inner = l === 4 ? `<div class="needs">Optional capstone. ${esc(b.name)} has none yet.</div>` : `<div class="needs">${esc(nd ? nd.replace(/^NEEDS /, '').toLowerCase().replace(/^./, c => c.toUpperCase()) + ': not designed yet.' : 'Not designed yet.')}</div>`;
    }
    const tier = l + 1;
    return `<div class="lvl"><span class="lab"><span>Level ${l}${l === 4 ? ' · capstone' : ''}</span><span>Tier ${tier}</span></span>${inner}</div>`;
  }).join('');
  const bst = b.stats.filter(s => !(b.unit && ['Made by'].includes(s.k)));
  const openSt = bst.filter(s => !s.v);
  const stats = bst.filter(s => s.v).map(s => `<div class="st"><span class="lab">${esc(s.k)}</span><div class="v">${esc(cap(s.v))}${s.n ? `<small>${esc(cap(s.n))}</small>` : ''}</div></div>`).join('')
    + (openSt.length ? `<div class="st wipst"><span class="lab">Not set yet</span><div class="v">${wipTag()} ${esc(openSt.map(s => s.k).join(', '))}</div></div>` : '');
  const desc = [...b.notes.map(n => ({ v: n.v, n: n.n })), ...b.desc.filter(d => d.v)].map(d => `<li>${esc(d.v)}${d.n ? `<span class="note">${esc(d.n)}</span>` : ''}</li>`).join('');
  const g = b.growth ? `<div class="growth"><h4>Growth <span class="gsteps">${Array.from({ length: Math.min(12, +((b.growth.rows.find(r => r.k === 'Max') || {}).v || 0) || 0) }, () => '<i></i>').join('')}</span></h4>
      ${b.growth.note ? `<p class="frame-note">${esc(b.growth.note)}</p>` : ''}
      <div class="rows">${rowsHTML(b.growth.rows)}</div></div>` : '';
  const model = b.sprite ? `<div class="bmodel">${spr(b.sprite, b.sprite.c > 16 ? 2 : 4)}</div>`
    : mine ? `<div class="bmodel wipm">${icon(slotIcon(b.slot), 36)}<span class="cap">${b.unit ? 'Sprite WIP' : 'Model WIP'}</span></div>`
    : `<div class="bmodel">${icon(slotIcon(b.slot), 40)}<span class="cap">3D in game</span></div>`;
  const cost = b.cost ? costChips(b.cost) : (mine ? wipTag('Cost WIP') : '');
  const line = b.line ? `<p class="il">${esc(b.line)}</p>` : (mine ? `<p class="il">${wipTag('In-game line WIP')}</p>` : '');
  return `<div class="bdetail">
    <div class="bhead">${model}<div style="min-width:0"><span class="lab">${esc(b.slot)}${b.alt ? ' · ' + esc(b.alt) : ''}</span><h3>${esc(b.name)}</h3>${line}</div><div class="costbox"><span class="lab">Cost</span>${cost}</div></div>
    <div class="bbody">
      <div style="display:grid;gap:14px;align-content:start">${desc ? `<ul class="blist">${desc}</ul>` : ''}${g}</div>
      <div><span class="lab" style="display:block;margin-bottom:8px">Stats</span><div class="statgrid">${stats || '<p class="frame-note">No stats of its own.</p>'}</div></div>
    </div>
    <div><div class="sec-h" style="margin-bottom:10px"><h2 style="font-size:20px">Upgrade path</h2><span class="lab">${b.upgrades.length} upgrades${mine ? '' : ' · text as it reads in game'}</span></div><div class="tree">${cols}</div></div>
  </div>`;
}

const BTAB = {};  // remembered building tab per vessel, so a re-render keeps it
function towers(v) {
  if (!v.buildings.length) return `<section class="band pad c12"><div class="sec-h"><h2>Towers and units</h2></div><p class="frame-note">No buildings on the sheet yet.</p></section>`;
  const cur = Math.min(BTAB[v.id] || 0, v.buildings.length - 1);
  const tabs = v.buildings.map((b, i) => `<button class="btab" role="tab" aria-selected="${i === cur}" data-b="${i}"><span class="ico">${icon(slotIcon(b.slot), 18)}</span><span class="t"><span>${esc(b.slot)}</span><b>${esc(b.name)}</b></span></button>`).join('');
  return `<section class="band pad c12"><div class="sec-h"><h2>Towers and units</h2><span class="lab">${v.kind === 'base' ? 'Pick a building to read its page' : 'Dimitri\'s designs; WIP marks what is still open'}</span></div>
    <div class="btabs" role="tablist">${tabs}</div><div id="bdetail">${buildingDetail(v, v.buildings[cur])}</div></section>`;
}

function unitsSection(v) {
  if (!v.units.length) {
    return v.kind === 'rw' ? `<section class="band pad c12"><div class="sec-h"><h2>Units</h2></div><p>${esc(v.name)} has no units yet.</p></section>` : '';
  }
  const cards = v.units.map(u => {
    const art = u.sprite ? spr(u.sprite, u.sprite.c <= 12 ? 6 : u.sprite.c <= 16 ? 5 : 3)
      : `<div class="wipc" style="min-height:0;width:120px;height:84px;display:grid;place-items:center;border:1px dashed color-mix(in srgb,var(--tape) 80%,var(--ink2))"><span class="inwip" style="position:relative;z-index:2">Sprite WIP</span></div>`;
    const rows = rowsHTML(u.rows);
    const vars = u.variants && u.variants.length ? `<div><span class="lab">Upgrade looks</span><div class="variants">${u.variants.map(x => `<div class="var">${spr(x.sprite, x.sprite.c > 16 ? 2 : 3)}<span>${esc(x.label)}</span></div>`).join('')}</div></div>` : '';
    const weap = u.weapons ? `<div><span class="lab">Guns from Silo Pipes</span><div class="weap">${u.weapons.map(w => { const [c, rest] = w.split(':'); return `<span class="au ${['Red', 'Blue', 'Yellow'].includes(c) ? c : 'Free'}" title="${att(c)}">${esc(rest.trim().replace(/ .*/, '') ? rest.trim().split(' ').slice(0, 2).join(' ') : c)}</span>`; }).join('')}</div></div>` : '';
    const notes = [...(u.notes || []).map(n => ({ v: n })), ...(u.desc || [])].filter(d => d.v).map(d => `<li>${esc(d.v)}${d.n ? `<span class="note">${esc(d.n)}</span>` : ''}</li>`).join('');
    return `<article class="unit"><div class="uart">${art}</div><div class="ubody"><h3>${esc(u.name)}</h3>${u.note ? `<span class="alt">${esc(u.note)}</span>` : ''}${notes ? `<ul class="blist" style="font-size:13.5px">${notes}</ul>` : ''}<div class="rows">${rows}</div>${weap}${vars}</div></article>`;
  }).join('');
  return `<section class="band pad c12"><div class="sec-h"><h2>Units</h2><span class="lab">${v.kind === 'base' ? 'Sprites from the game, at zoom' : 'Described by Dimitri, not drawn yet'}</span></div><div class="units">${cards}</div></section>`;
}

function directionsSection(v) {
  if (!v.directions || !v.directions.length) return '';
  const cards = v.directions.map(d => {
    const g = k => (d.rows.find(r => r.k === k) || {});
    const ranks = ['Rank I', 'Rank II', 'Rank III'].map((k, i) => g(k).v ? `<div class="rk"><b>${['I', 'II', 'III'][i]}</b><span>${esc(g(k).v)}${g(k).n ? ` <i class="frame-note">(${esc(g(k).n)})</i>` : ''}</span></div>` : '').join('');
    const body = d.missing ? `<p class="frame-note">Not written in 11_Directions.txt yet.</p>`
      : `<div class="ranks">${ranks}</div>
      <div class="rows"><div class="r"><span>Numbers</span><span>${val(g('Numbers').v)}</span></div><div class="r"><span>Icon brief</span><span>${g('Look').v ? esc(g('Look').v) : wipTag()}</span></div>${g('Clashes').v ? `<div class="r"><span>Clashes</span><span>${esc(g('Clashes').v)}</span></div>` : ''}</div>`;
    return `<article class="dir"><div class="dh"><div class="dico">ICON<br>WIP</div><div><span class="lab">${esc(g('Kind').v || '')}${g('Touches').v ? ' · ' + esc(g('Touches').v) : ''}</span><h3>${esc(d.name)}</h3></div></div>${body}</article>`;
  }).join('');
  return `<section class="band pad c12"><div class="sec-h"><h2>Directions</h2><span class="lab">Rogue Waves run upgrades, picked between maps · from 11_Directions.txt</span></div><div class="dirs">${cards}</div></section>`;
}

function rollSection(v) {
  const R = roll(v);
  const r = readiness(v);
  const pa = 100 * r.have / r.need, pb = 100 * r.part / r.need;
  const strip = v.kind === 'rw'
    ? `<div class="ready" style="margin-bottom:18px"><span class="lab num">${r.have} done · ${r.part} partly · ${r.need - r.have - r.part} to go</span><div class="meter" role="img" aria-label="${r.have} done, ${r.part} partly done, of ${r.need}"><span class="a" style="width:${pa}%"></span><span class="b" style="width:${pb}%"></span></div>
       <span></span><div class="legend"><span><i style="background:var(--vc)"></i>Dimitri's own</span><span><i style="background:color-mix(in srgb,var(--vc) 40%,var(--panel))"></i>Partly done</span><span><i style="background:var(--tape)"></i>Work in progress</span></div></div>` : '';
  const groups = R.map(([g, items]) => `<div class="rgroup"><h3>${esc(g)}</h3><div class="tiles">${items.map(it => `<div class="tile s-${it.s}"><div class="prev">${it.prev || `<span class="gl">${it.s === 'wip' ? 'WORK IN PROGRESS' : it.s === 'none' ? '—' : it.s === 'game' ? 'In the game' : it.s === 'part' ? 'PARTLY DONE' : it.s === 'mine' ? "DIMITRI'S" : ''}</span>`}</div><div class="tb"><span class="st-chip ${it.s}">${ST[it.s]}</span><b>${esc(it.label)}</b><span class="m">${esc(it.meta || '')}</span></div></div>`).join('')}</div></div>`).join('');
  const foot = v.kind === 'rw' && v.spritesNote ? `<p class="scale-note">${esc('From the sheet: "' + v.spritesNote + '"')}</p>` : '';
  return `<section class="band pad c12"><div class="sec-h"><h2>Asset sheet</h2><span class="lab">${v.kind === 'base' ? 'Everything the vessel ships with' : 'Against what a base vessel ships with'}</span></div>${strip}<div class="roll">${groups}</div>${foot}</section>`;
}

function renderVessel(v) {
  return `<div class="page" style="${vesselStyle(v)}">
    ${hero(v)}
    <div class="cols">${statsCard(v)}${loreCard(v)}${sigSection(v)}${towers(v)}${unitsSection(v)}${directionsSection(v)}${rollSection(v)}</div>
  </div>`;
}

/* ---------------------------------------------------------------- dock + routing */
let CUR = 'lineup';
function renderDock(cur) {
  const slot = v => `<button class="slot" role="tab" aria-selected="${cur === v.id}" data-go="${v.id}" title="${att(v.name)}, ${att(v.title)}">
      ${v.kind === 'base' ? spr(v.anims.Mini, 3) : frameArt(v, 48)}
      <span class="nm">${esc(v.name)}</span>${v.kind === 'rw' ? '<span class="wipdot">WIP</span>' : ''}</button>`;
  const dock = document.getElementById('dock');
  const add = EDIT ? `<button class="slot addslot" data-act="new-vessel" title="Start a new vessel sheet"><span class="lineup-ico">${icon('units', 30)}</span><span class="nm">+ New</span></button>` : '';
  dock.innerHTML = `<div class="grp"><button class="slot lineup" role="tab" aria-selected="${cur === 'lineup'}" data-go="lineup"><span class="lineup-ico">${icon('units', 30)}</span><span class="nm">Lineup</span></button></div>
    <span class="sep"></span><span class="glab">BASE GAME</span><div class="grp">${BASE.map(slot).join('')}</div>
    <span class="sep"></span><span class="glab">ROGUE WAVES</span><div class="grp">${NEWV().map(slot).join('')}${add}</div>`;
  mount(dock);
}
function pageHTML(v) { return v ? renderVessel(v) : renderLineup(); }
function show(id, push) {
  const v = findV(id);
  const key = v ? id : 'lineup';
  const changed = key !== CUR;
  CUR = key;
  REG.length = 0; live = [];
  renderDock(key);
  const main = document.getElementById('main');
  if (EDIT && v && v.kind === 'rw') {
    editorShow(main, RECS.find(r => r.id === key), changed);
  } else {
    if (EDIT) editorHide();
    main.innerHTML = pageHTML(v);
    mount(main);
    if (v) wire(v, main);
  }
  if (push) { try { history.replaceState(null, '', '#' + key); } catch (e) { location.hash = key; } }
  const sel = document.querySelector('.slot[aria-selected="true"]');
  if (sel && sel.scrollIntoView && changed) sel.scrollIntoView({ block: 'nearest', inline: 'center' });
}
function wire(v, main) {
  const heroEl = main.querySelector('.hero-spr.spr');
  if (heroEl) {
    const it = live.find(x => x.el === heroEl);
    const st = { anim: 'Idle', face: 0 };
    const apply = () => {
      const sp = v.anims[st.anim];
      it.sp = sp; it.face = Math.min(st.face, sp.rows.length - 1); it.fr = -1;
      const sh = SH[sp.s]; const sc = it.sc;
      heroEl.style.width = heroEl.style.height = sp.c * sc + 'px';
      heroEl.style.backgroundImage = `url(${sh.u})`;
      heroEl.style.backgroundSize = `${sh.w * sc}px ${sh.h * sc}px`;
      paint(it, 0);
      main.querySelectorAll('[data-ctl="face"] .chip').forEach((c, i) => { c.disabled = sp.rows.length === 1 && i > 0; c.setAttribute('aria-pressed', String(i === it.face)); });
    };
    main.querySelectorAll('[data-anim]').forEach(b => b.addEventListener('click', () => {
      st.anim = b.dataset.anim; main.querySelectorAll('[data-anim]').forEach(x => x.setAttribute('aria-pressed', String(x === b))); apply();
    }));
    main.querySelectorAll('[data-facing]').forEach(b => b.addEventListener('click', () => { st.face = +b.dataset.facing; apply(); }));
  }
  main.querySelectorAll('.btab').forEach(t => t.addEventListener('click', () => {
    main.querySelectorAll('.btab').forEach(x => x.setAttribute('aria-selected', String(x === t)));
    BTAB[v.id] = +t.dataset.b;
    const d = main.querySelector('#bdetail');
    d.innerHTML = buildingDetail(v, v.buildings[+t.dataset.b]);
    mount(d);
  }));
}
document.addEventListener('click', e => {
  const g = e.target.closest('[data-go]');
  if (g) { show(g.dataset.go, true); if (g.classList.contains('vcard')) window.scrollTo({ top: 0, behavior: RM ? 'auto' : 'smooth' }); }
});
window.addEventListener('hashchange', () => { if (location.hash.slice(1) !== CUR) show(location.hash.slice(1), false); });
