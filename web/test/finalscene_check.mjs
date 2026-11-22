// node web/test/finalscene_check.mjs — sub_state 30 SubstFinalSceneReset 19:BDF2 (sessions 152-153;
// states/finalscene.js).
// State (town $02; sub_state $1E poked, as AI $70 phase 10 27:BD19 stores it):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 100 --poke 0041=1E
//     --until 19:BDF2 --out web/test/states/finalscene.json --save-state tools/emu/states/finalscene.state
//   (stops at frame 2550 line 256)
// Reference web/test/finalscene_ref.csv = tools/web_state_compare.py --state tools/emu/states/finalscene.state
// --web web/test/states/finalscene.json --frames 1080 --keys <KEYS below>, cut by tools/csv_trim.py
// (core,pal,obj) to the frames of the scene (the five messages are closed by the A taps every 40 frames).
// Rule for differing cells: each walk loop moves slots 0 / 1 / $0E right after its WaitFrame. tools.emu samples
// its row at L255, after that work; the port does it in the next frame (the no-cycle-model gap (c)). So a cell
// that differs at shift 0 is accepted only if it is in LATE and the port's NEXT row has the tools.emu value.
// Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys, compareColumns, compareRow } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const hex = (v) => v.toString(16).padStart(2, '0');
const want = (m, a, v, tag) => { if (m.rd(a) !== v) fail(`${tag}: $${a.toString(16)} = ${hex(m.rd(a))}, want ${hex(v)}`); };

export const KEYS = [];
for (let f = 2640; f <= 3520; f += 40) KEYS.push(`${f}:A:2`);
const LATE = new Set(['sub', 'pos_x', 'pos_y', 'o00_x', 'o00_y', 'o01_x', 'o01_y', 'o0E_y', 'pal_crc']);
// The other way round: the last loop (BF8C, from f3289) runs the sprite pass BEFORE its INCs, and the first
// ShowMessage stores sub_state 8 late in its frame (f2928). Both land after tools.emu's L255 sample, so there
// tools.emu's row F has the value of the port's row F-1 (EARLY). On the 2-frame white flashes (MesiaPalFlash,
// A = $16) tools.emu samples inside PaletteBuild (FLASH_HALF, a half-rebuilt pal_buf no port row has); the port's
// previous row must then show the all-$16 buffer (FLASH_CRC).
const EARLY = new Set(['sub', 'o00_y', 'o01_y', 'o0E_y', 'pal_crc']);
const isEarly = (f) => f >= 3289 || f === 2928;
const FLASH_HALF = 2323950862, FLASH_CRC = 110842302;
// FadeOut 26:8C3D steps (every 33 frames): tools.emu's row is again sampled inside the rebuild; the port's row
// must show the finished step = tools.emu's NEXT row.
const FADE = new Set([3504, 3537, 3570, 3603]);
const LAST = 3620;

const m = new Machine(data, { log: () => {} });
m.reset();
const s = JSON.parse(readFileSync(path.join(WEB, 'test/states/finalscene.json'), 'utf8'));
m.loadState(s);
const f0 = s.frame0 ?? s.frame, keys = parseKeys(KEYS);
const rows = new Map();
let entry = false, ending = null;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(keys.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (f === 2560) {                                     // entry block done, first walk steps
    entry = true;
    for (let i = 4; i < 0x1F; i++) if (m.rd(0x04A0 + i)) fail(`slot ${hex(i)} still active`);
    for (const a of [0x51, 0x0600, 0x0620, 0x06C0, 0x0710]) want(m, a, 0, 'cleared at entry');
    want(m, 0x07F4, 0x47, 'chr_r4_play');
    want(m, 0x07E6, 0x06, 'pal_id_spr2');
    want(m, 0x07E7, 0x1C, 'pal_id_spr3');
  }
  if (!ending && m.rd(0x40) === 0x04) ending = { f, msgs: m.rd(0x2E), yE: m.rd(0xBE) };
}
if (m.unported.size) fail(`${[...m.unported].join(', ')}`);
if (!entry) fail('entry frame not reached');

// ---- frame by frame vs tools.emu
{
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/finalscene_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('finalscene_ref.csv has a column the port does not write');
  const before = fails;
  let n = 0, late = 0, early = 0, flash = 0;
  const ref = new Map(lines.map((l) => { const w = l.split(',').map(Number); return [w[0], w]; }));
  for (const [f, w] of ref) {
    const row = rows.get(f), next = rows.get(f + 1), prev = rows.get(f - 1);
    if (!row) { fail(`f${f}: no port row`); continue; }
    n++;
    const d = [];
    refCols.forEach((c, i) => {
      if (c === 'pal_crc' && FADE.has(f)) {
        if (row[idx[i]] === ref.get(f + 1)?.[i]) flash++;
        else d.push(`fade step: pal_crc ${row[idx[i]]}, want tools.emu f${f + 1} ${ref.get(f + 1)?.[i]}`);
        return;
      }
      if (!i || row[idx[i]] === w[i]) return;
      if (c === 'pal_crc' && w[i] === FLASH_HALF) {
        if (prev && prev[idx[i]] === FLASH_CRC) flash++;
        else d.push(`flash row: previous pal_crc ${prev?.[idx[i]]}, want ${FLASH_CRC}`);
        return;
      }
      if (isEarly(f) && EARLY.has(c) && prev && prev[idx[i]] === w[i]) { early++; return; }
      if (LATE.has(c) && next && next[idx[i]] === w[i]) { late++; return; }
      d.push(`${c} want ${w[i]} got ${row[idx[i]]}`);
    });
    if (d.length) fail(`f${f}: ${d.join(' ')}`);
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} finalscene: ${n} rows vs tools.emu (${late} cells one frame late, ${early} one frame early, ${flash} flash / fade rows)`);
}

// ---- end: FinalSceneToEnding 19:BFC4 (main_state 4), then MainStateEndingInit -> 5
{
  const before = fails;
  if (!ending) fail('main_state 4 never stored');
  else {
    if (ending.msgs !== 5) fail(`$2E = ${ending.msgs} messages, want 5`);
    if (ending.yE !== 0xE0) fail(`slot $0E y = ${hex(ending.yE)}, want e0`);
  }
  want(m, 0x40, 0x05, 'main_state after EndingInit');
  console.log(`${fails > before ? 'FAIL' : 'ok  '} finalscene end state${ending ? ` (main_state 4 at f${ending.f})` : ''}`);
}
process.exit(fails ? 1 : 0);
