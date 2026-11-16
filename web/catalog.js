// catalog.js — re-c asset browser. Serve the repo root (python3 -m http.server) and open
// /web/catalog.html. Data: data/catalog_manifest.json (tools/catalog_export.py), data/chr.bin,
// ../gfx/sprites.json, ../gfx/palettes.json, data/sound.json, data/text.json, rendered PNGs.
import { renderTrack, trackFrames, FRAME_HZ } from './apu_preview.js';

const $ = (id) => document.getElementById(id);
const hex2 = (n) => n.toString(16).toUpperCase().padStart(2, '0');
const status = (s) => { $('status').textContent = s; };
const D = {}; // loaded data

async function getJSON(p) { const r = await fetch(p); if (!r.ok) throw new Error(p + ' ' + r.status); return r.json(); }

// ---------- tabs ----------
const inited = {};
const tabInit = { chr: initChr, pal: initPal, spr: initSpr, area: initArea, scr: initScr, snd: initSnd, txt: initTxt };
for (const b of document.querySelectorAll('header button')) {
  b.onclick = () => showTab(b.dataset.tab);
}
async function showTab(t) {
  for (const b of document.querySelectorAll('header button')) b.classList.toggle('on', b.dataset.tab === t);
  for (const s of document.querySelectorAll('main section')) s.classList.toggle('on', s.id === t);
  if (!inited[t]) {
    inited[t] = true;
    status('loading ' + t + '…');
    try { await tabInit[t](); status(''); } catch (e) { status(String(e)); console.error(e); }
  }
}

// ---------- shared: NES colours, CHR tiles ----------
function rgb(c) { // NES colour index -> [r,g,b]
  const h = D.pal.master_palette[c & 0x3F];
  return [parseInt(h.slice(0, 2), 16), parseInt(h.slice(2, 4), 16), parseInt(h.slice(4, 6), 16)];
}
const GRAY = [[0, 0, 0], [85, 85, 85], [170, 170, 170], [255, 255, 255]];

// draw 8x8 tile (bank 0-127, tile 0-63) into ImageData at (x,y); cols = 4 rgb or null (transparent)
function drawTile(img, x, y, bank, tile, cols, hflip = false, vflip = false) {
  const base = bank * 1024 + tile * 16, chr = D.chr, W = img.width, d = img.data;
  for (let r = 0; r < 8; r++) {
    const lo = chr[base + r], hi = chr[base + 8 + r];
    const py = y + (vflip ? 7 - r : r);
    if (py < 0 || py >= img.height) continue;
    for (let b = 0; b < 8; b++) {
      const v = ((lo >> (7 - b)) & 1) | (((hi >> (7 - b)) & 1) << 1);
      const c = cols[v];
      if (!c) continue;
      const px = x + (hflip ? 7 - b : b);
      if (px < 0 || px >= W) continue;
      const o = (py * W + px) * 4;
      d[o] = c[0]; d[o + 1] = c[1]; d[o + 2] = c[2]; d[o + 3] = 255;
    }
  }
}
function scaledCanvas(img, scale) {
  const src = document.createElement('canvas');
  src.width = img.width; src.height = img.height;
  src.getContext('2d').putImageData(img, 0, 0);
  const c = document.createElement('canvas');
  c.width = img.width * scale; c.height = img.height * scale;
  const g = c.getContext('2d'); g.imageSmoothingEnabled = false;
  g.drawImage(src, 0, 0, c.width, c.height);
  return c;
}
function slotColours(slot) {
  if (slot < 0) return GRAY;
  return D.pal.slots[slot].colors.map((c, i) => (c === 0xFF || c === undefined ? (i ? [255, 0, 255] : [40, 40, 48]) : rgb(c)));
}

// ---------- CHR ----------
async function initChr() {
  const sel = $('chrPal');
  sel.innerHTML = '<option value="-1">grayscale</option>' +
    D.pal.slots.map((s) => `<option value="${s.slot}">$${hex2(s.slot)} (${s.bank}:${s.addr})</option>`).join('');
  sel.onchange = drawChrGrid; $('chrScale').onchange = drawChrGrid;
  drawChrGrid();
}
let chrSel = 0;
function chrBankImage(bank, cols) {
  const img = new ImageData(128, 32);
  for (let t = 0; t < 64; t++) drawTile(img, (t & 15) * 8, (t >> 4) * 8, bank, t, cols);
  return img;
}
function drawChrGrid() {
  const cols = slotColours(+$('chrPal').value), grid = $('chrGrid');
  grid.innerHTML = '';
  for (let b = 0; b < 128; b++) {
    const cell = document.createElement('div');
    cell.className = 'cell' + (b === chrSel ? ' sel' : '');
    cell.appendChild(scaledCanvas(chrBankImage(b, cols), 1));
    cell.appendChild(document.createElement('br'));
    cell.append(`$${hex2(b)} ${b < 64 ? 'BG' : 'SPR'}`);
    cell.onclick = () => { chrSel = b; drawChrGrid(); };
    grid.appendChild(cell);
  }
  const big = scaledCanvas(chrBankImage(chrSel, cols), +$('chrScale').value);
  const tgt = $('chrBig'); tgt.width = big.width; tgt.height = big.height;
  tgt.getContext('2d').drawImage(big, 0, 0);
  $('chrInfo').textContent = `CHR bank $${hex2(chrSel)} (${chrSel})\nfile offset 0x${(0x40010 + chrSel * 1024).toString(16)}\n` +
    `tiles $00-$3F; as a sprite bank it serves tiles $${hex2((chrSel & 3) * 64)}-$${hex2((chrSel & 3) * 64 + 63)} when mapped to R${2 + (chrSel & 3)}-slot ${chrSel & 3}`;
}

// ---------- Palettes ----------
async function initPal() {
  $('master').innerHTML = D.pal.master_palette.map((h, i) => `<span class="sw" title="$${hex2(i)} #${h}" style="background:#${h};width:12px;height:14px"></span>`).join('');
  const g = $('palGrid');
  g.innerHTML = D.pal.slots.map((s) => {
    const sw = s.colors.map((c) => c === 0xFF
      ? '<span class="sw" style="background:repeating-conic-gradient(#555 0 25%,#999 0 50%) 0/8px 8px"></span>'
      : `<span class="sw" style="background:#${D.pal.master_palette[c & 0x3F]}" title="$${hex2(c)}"></span>`).join('');
    const tag = (s.bg_areas.length ? 'BG ' : '') + (s.spr_areas.length ? 'SPR' : '');
    const tip = `slot $${hex2(s.slot)} @ ${s.bank}:${s.addr}\ncolours ${s.colors.map(hex2).join(' ')}\nBG areas: ${s.bg_areas.map(hex2).join(' ')}\nsprite areas: ${s.spr_areas.map(hex2).join(' ')}`;
    return `<div class="cell" title="${tip}">$${hex2(s.slot)}<br>${sw}<br><small>${tag || '&nbsp;'}</small></div>`;
  }).join('');
}

// ---------- Sprites ----------
let sprTimer = null, sprCounter = 0, sprCells = [];
async function initSpr() {
  const ids = Object.keys(D.spr.areas);
  $('sprArea').innerHTML = ids.map((a) => `<option value="${a}">$${a} (${D.spr.areas[a].objects.length} obj)</option>`).join('');
  $('sprArea').value = ids.includes('03') ? '03' : ids[0];
  $('sprArea').onchange = buildSpr; $('sprScale').onchange = buildSpr; $('sprAll').onchange = buildSpr;
  buildSpr();
  if (sprTimer) clearInterval(sprTimer);
  sprTimer = setInterval(() => {
    if (!$('sprAnim').checked || !$('spr').classList.contains('on')) return;
    sprCounter = (sprCounter - 1) & 0xFF; // $04E0 counts down; frame changes every 8 counts
    for (const c of sprCells) drawSprCell(c);
  }, 1000 / FRAME_HZ);
}
function buildSpr() {
  const a = D.spr.areas[$('sprArea').value];
  const scale = +$('sprScale').value;
  const pals = a.pals.map((p) => p.map((h, i) => (i === 0 ? null : rgb(parseInt(h, 16)))));
  let rows = [];
  if ($('sprAll').checked) {
    for (let i = 0; i < 256; i++) rows.push({ label: `meta $${hex2(i)}`, meta: i, tadd: 0 });
  } else {
    for (let i = 0; i < 8; i++) rows.push({ label: `player $${hex2(i)}`, meta: i, tadd: 0 });
    const seen = new Set();
    for (const o of a.objects) {
      const k = o.meta + ':' + o.tile_add;
      if (seen.has(k) || o.meta === null) continue;
      seen.add(k);
      rows.push({ label: `type $${o.type} m$${o.meta}${o.tile_add !== '00' ? '+' + o.tile_add : ''}`, meta: parseInt(o.meta, 16), tadd: parseInt(o.tile_add, 16), obj: o });
    }
  }
  $('sprInfo').textContent = `area $${$('sprArea').value}: CHR R2-R5 = ${a.chr.join(' ')}, sprite palette ids ${a.pal_ids.join(' ')} → slots ${a.pal_slots.join(' ')}; ${a.objects.length} placed objects`;
  $('sprSheet').src = '../' + a.png;
  const g = $('sprGrid'); g.innerHTML = ''; sprCells = [];
  for (const r of rows) {
    const m = D.spr.metasprites[hex2(r.meta)];
    const cell = document.createElement('div'); cell.className = 'cell';
    const cv = document.createElement('canvas'); cv.width = 64 * scale; cv.height = 64 * scale;
    cell.appendChild(cv); cell.appendChild(document.createElement('br'));
    const nfr = m && m.frames ? m.frames.length : 0;
    cell.append(r.label + (nfr ? ` (${nfr}f)` : ' (empty)'));
    if (r.obj) cell.title = JSON.stringify(r.obj);
    g.appendChild(cell);
    const c = { cv, m, tadd: r.tadd, chr: a.chr.map((h) => parseInt(h, 16)), pals, scale, last: -1 };
    sprCells.push(c); drawSprCell(c);
  }
}
function drawSprCell(c) {
  const m = c.m;
  const g = c.cv.getContext('2d');
  if (!m || !m.frames || !m.frames.length) { if (c.last !== -2) { g.fillStyle = '#111'; g.fillRect(0, 0, c.cv.width, c.cv.height); c.last = -2; } return; }
  const f = ((sprCounter >> 3) & m.mask) % m.frames.length;
  if (f === c.last) return;
  c.last = f;
  const img = new ImageData(64, 64);
  for (let i = 0; i < img.data.length; i += 4) { img.data[i] = 17; img.data[i + 1] = 17; img.data[i + 2] = 22; img.data[i + 3] = 255; }
  // OAM order: first sprite has priority -> draw back to front
  const spr = m.frames[f];
  for (let i = spr.length - 1; i >= 0; i--) {
    const [dx, dy, attr, tile0] = spr[i];
    const tile = (tile0 + c.tadd) & 0xFF;
    const bank = c.chr[tile >> 6];
    if (bank === 0xFF) continue; // unused register
    drawTile(img, 32 + dx, 32 + dy + 1, bank & 0x7F, tile & 63, c.pals[attr & 3], !!(attr & 0x40), !!(attr & 0x80));
  }
  const s = scaledCanvas(img, c.scale);
  g.drawImage(s, 0, 0);
}

// ---------- Areas ----------
async function initArea() {
  const areas = D.man.areas;
  $('areaSel').innerHTML = areas.map((a, i) => `<option value="${i}">$${a.id} — ${a.info.replace(/^\s*\d+ \$\w\w\s+/, '').slice(0, 40)}</option>`).join('');
  $('areaSel').onchange = drawArea; $('areaZoom').onchange = drawArea;
  drawArea();
}
function drawArea() {
  const a = D.man.areas[+$('areaSel').value];
  const img = $('areaImg');
  img.onload = () => { img.style.width = img.naturalWidth * +$('areaZoom').value + 'px'; };
  img.src = a.png;
  const sheet = D.man.sprite_sheets.find((s) => s.id === a.id);
  $('areaInfo').textContent = a.info + (sheet ? '\nsprites: ' + sheet.info : '');
}

// ---------- Screens ----------
async function initScr() {
  $('rectGrid').innerHTML = D.man.rects.map((r) =>
    `<div class="cell" title="${r.info}"><img class="px" src="${r.png}" loading="lazy" style="height:240px"><br>${r.png.split('/').pop()}<br><small>${r.info.split(':').slice(1).join(':')}</small></div>`).join('');
  $('scrFilter').oninput = drawScr;
  drawScr();
}
function drawScr() {
  const q = $('scrFilter').value.trim();
  const list = D.man.screens.filter((s) => !q || s.includes(q));
  $('scrGrid').innerHTML = list.map((s) =>
    `<div class="cell"><img class="px" src="${s}" loading="lazy" width="256" height="240"><br>${s.split('/').pop()}</div>`).join('');
}

// ---------- Sound ----------
let audioCtx = null, audioSrc = null;
const CHN = ['SQ1', 'SQ2', 'TRI', 'NOISE', 'DMC', 'SFX-SQ2', 'SFX-NOISE'];
async function initSnd() {
  if (!D.snd) { status('loading sound.json (2.7 MB)…'); D.snd = await getJSON(D.man.json.sound); }
  $('sndMute').innerHTML = CHN.map((c) => `<label><input type="checkbox" data-chn="${c}"> ${c}</label>`).join(' ');
  const rows = [];
  for (const kind of ['music', 'sfx']) {
    for (const t of D.snd[kind]) {
      const ch = t.channels.map((c) => `${c.chn}:${c.frames}f${c.loop !== null ? '↻' : ''}`).join(' ');
      const last = t.channels.map((c) => c.events[c.events.length - 1]).find((e) => e && e.k === 'end' && e.next);
      rows.push(`<tr><td><button data-kind="${kind}" data-id="${t.id}">▶</button></td><td>${kind} $${hex2(t.id)}</td>` +
        `<td>${(trackFrames(t, 1e9) / FRAME_HZ).toFixed(1)}s</td><td><small>${ch}${last ? ' → $' + hex2(last.next) : ''}</small></td></tr>`);
    }
  }
  $('sndTable').innerHTML = '<tr><th></th><th>id</th><th>len</th><th>channels (frames, ↻ loops)</th></tr>' + rows.join('');
  $('sndTable').onclick = (e) => {
    const b = e.target.closest('button'); if (!b) return;
    playTrack(b.dataset.kind, +b.dataset.id);
  };
  $('sndStop').onclick = stopSnd;
}
function stopSnd() { if (audioSrc) { try { audioSrc.stop(); } catch { /* already stopped */ } audioSrc = null; } }
function playTrack(kind, id) {
  stopSnd();
  const t = D.snd[kind].find((x) => x.id === id);
  if (!audioCtx) audioCtx = new AudioContext();
  const mute = new Set([...document.querySelectorAll('#sndMute input:checked')].map((i) => i.dataset.chn));
  const t0 = performance.now();
  const pcm = renderTrack(t, D.snd, audioCtx.sampleRate, { maxSeconds: +$('sndMax').value, mute });
  const buf = audioCtx.createBuffer(1, pcm.length, audioCtx.sampleRate);
  buf.copyToChannel(pcm, 0);
  audioSrc = audioCtx.createBufferSource(); audioSrc.buffer = buf;
  audioSrc.connect(audioCtx.destination); audioSrc.start();
  const lines = [`${kind} $${hex2(id)}  header $${t.hdr.toString(16).toUpperCase()}  ${kind === 'music' ? 'b0' : 'prio'} ${t.b0 ?? t.prio}`,
    `rendered ${(pcm.length / audioCtx.sampleRate).toFixed(1)} s in ${(performance.now() - t0).toFixed(0)} ms`, ''];
  for (const c of t.channels) {
    lines.push(`${c.chn}  start $${c.start.toString(16).toUpperCase()}  ${c.events.length} events  ${c.frames} frames  loop→${c.loop ?? '-'}`);
    for (const e of c.events.slice(0, 40)) {
      lines.push(`  f${String(e.f).padStart(5)} ${e.k.padEnd(6)} len ${String(e.len ?? '').padStart(3)} ` +
        (e.k === 'note' || e.k === 'legato' ? `o${e.oct} x${hex2(e.x)} P=${e.period} ${e.hz ?? ''}Hz ` : '') +
        `vol ${e.vol ?? ''} env ${e.env ?? '-'} vib ${e.vib ?? '-'}${e.dmc ? ' DMC#' + e.dmc : ''}${e.holdVol !== undefined ? ' hold ' + e.holdVol : ''}${e.next !== undefined ? ' next $' + hex2(e.next) : ''}`);
    }
    if (c.events.length > 40) lines.push(`  … ${c.events.length - 40} more`);
  }
  $('sndInfo').textContent = lines.join('\n');
}

// ---------- Text ----------
async function initTxt() {
  if (!D.txt) D.txt = await getJSON(D.man.json.text);
  $('txtQ').oninput = drawTxt; $('txtTab').onchange = drawTxt;
  drawTxt();
}
function esc(s) { return s.replace(/[&<>]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;' }[c])); }
function drawTxt() {
  const q = $('txtQ').value.toLowerCase(), tab = $('txtTab').value;
  let html;
  if (tab === 'messages') {
    const m = D.txt.messages.filter((x) => !q || x.text.toLowerCase().includes(q));
    $('txtCount').textContent = `${m.length} / ${D.txt.messages.length}`;
    html = '<tr><th>grp</th><th>msg</th><th>addr</th><th>text</th></tr>' + m.map((x) =>
      `<tr><td>${x.group}</td><td>${x.msg}</td><td>${x.bank}:${x.addr}</td><td style="white-space:pre-wrap">${esc(x.text)}</td></tr>`).join('');
  } else {
    const l = D.txt[tab].map((s, i) => [i, s]).filter(([, s]) => !q || s.toLowerCase().includes(q));
    $('txtCount').textContent = `${l.length} / ${D.txt[tab].length}`;
    html = '<tr><th>#</th><th>string</th></tr>' + l.map(([i, s]) => `<tr><td>$${hex2(i)}</td><td>${esc(s)}</td></tr>`).join('');
  }
  $('txtTable').innerHTML = html;
}

// ---------- boot ----------
(async () => {
  try {
    D.man = await getJSON('data/catalog_manifest.json');
    const [chr, pal, spr] = await Promise.all([
      fetch(D.man.chr).then((r) => r.arrayBuffer()),
      getJSON(D.man.json.palettes), getJSON(D.man.json.sprites)]);
    D.chr = new Uint8Array(chr); D.pal = pal; D.spr = spr;
    inited.chr = true; await initChr(); status('');
  } catch (e) {
    status('load failed: ' + e + ' — serve the repo root: python3 -m http.server, open /web/catalog.html');
    console.error(e);
  }
})();
