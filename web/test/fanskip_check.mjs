// node web/test/fanskip_check.mjs — the fan-turret frame vs tools.emu (session 177): AiFanTurretIdle 27:B5D3
// tail-JMPs to AiShootCheck, so when the check starts an attack its PLA PLA pops ObjUpdateAll's return
// (JSR $0010 at 30:CD28) and slots $10-$1E get no AI update on that frame.
//   scenario fan_skip (area 03): fan turret $5D (AI $4C) in slot $0F, dasher $5B (AI $44) in slot $1C at +10;
//   attack start ($064F = $1F) at f2679, where the dasher's obj_phase $061C (DEC every frame at 27:B448) skips
//   one step in tools.emu too (`--watch 061C`: $0A at f2678, $09 at f2680). No keys; the shot's hit knocks the
//   player into area $02's exit (sub_state 1 at f2713).
// State: python3 -m tools.emu.scenario_state fan_skip 12 --out web/test/states/fanskip.json
//          --save-state tools/emu/states/fanskip.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/fanskip.state --web web/test/states/fanskip.json
//          --frames 300 --cols core,snd,pal,obj,oam_crc --out-dir gfx/fanskip, cut by tools/csv_trim.py to
//          f2509-2807 -> web/test/fanskip_ref.csv (s178: extended from f2718 over the area-$02 entry).
// f2509-2718 all cells equal at shift 0, snd included. s178, the area-$02 entry f2719-2807, listed cells only
// (tools.emu pass_probe --pc 26:8C10 --write 6140 615F 0036 0102 04AD; all are row-sample timing, the screen
// and RAM match again from f2737):
//   PARTIAL pal_crc f2719/22/25/28 (FadeOut) + f2744/47/50/53 (FadeIn): each PaletteBuild runs L254 -> L9 of the
//     next frame, so tools.emu's L261 row holds a half-built buffer. Accepted when port f == ref f-1, f+1 equal.
//   LATE f2729: the object clear (L253) + music $16 (L258) run after the last fade WaitFrame in vblank, before
//     the sample; the port runs them in its next frame. Accepted when port f == ref f-1 and port f+1 == ref f+1.
//   FSD pos_y f2730/32/33/34/36: $36 = FullScreenDraw's row cursor (8 px per row). tools.emu starts row 1 at
//     f2730 L258 (after the NMI, before the sample) and its area-$02 rows cost ~44-45 lines, so its frame ends
//     fall in rows 7/12/18/24/30 where the port's FSD_LAG (area 0, ~43 lines) pauses after rows 7/13/19/25/30:
//     the port is 0 / one row (-8) ahead there; f2736 pos_y = 08 / pos_x = $A1 written by AreaEnter at L261.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { compareColumns, compareRow } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const verbose = process.argv.includes('-v');
let fails = 0;
const fail = (msg) => { fails++; if (verbose || fails <= 30) console.log('  FAIL ' + msg); };
const LAST = 2807, ATTACK = 2679, KNOCK = 2718;
const PARTIAL = new Set([2719, 2722, 2725, 2728, 2744, 2747, 2750, 2753]);
const LATE = 2729;
// FullScreenDraw row cursor: port value per listed frame (tools.emu's is in the ref)
const FSD_POSY = new Map([[2730, 0], [2732, 136], [2733, 88], [2734, 40], [2736, 0]]);

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/fanskip.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/fanskip_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('fanskip_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), phase = new Map();
let attackF = -1;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(0);
  rows.set(f, compareRow(m, f));
  phase.set(f, m.rd(0x061C));
  if (attackF < 0 && m.rd(0x064F) === 0x1F) attackF = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
const ref = new Map(lines.map((l) => { const w = l.split(',').map(Number); return [w[0], w]; }));
const port = (f, i) => rows.get(f)?.[idx[i]];
let n = 0, special = 0;
for (const [f, w] of ref) {
  const row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    const c = refCols[i];
    if (row[idx[i]] === w[i]) continue;
    const prevOk = row[idx[i]] === ref.get(f - 1)?.[i];
    if (PARTIAL.has(f) && c === 'pal_crc' && prevOk && port(f + 1, i) === ref.get(f + 1)?.[i]) { special++; continue; }
    if (f === LATE && prevOk && port(f + 1, i) === ref.get(f + 1)?.[i]) { special++; continue; }
    if (c === 'pos_y' && FSD_POSY.get(f) === row[idx[i]]) { special++; continue; }
    if (c === 'pos_x' && f === 2736 && row[idx[i]] === 0xA0 && port(f + 1, i) === ref.get(f + 1)?.[i]) { special++; continue; }
    fail(`f${f}: ${c} want ${w[i]} got ${row[idx[i]]}`);
  }
}
if (attackF !== ATTACK) fail(`fan attack start ($064F = $1F) at f${attackF}, want f${ATTACK}`);
// the skipped frame: slot $1C's obj_phase holds on f2679 and steps on the frames around it
const want = [[ATTACK - 1, 0x0A], [ATTACK, 0x0A], [ATTACK + 1, 0x09]];
for (const [f, v] of want)
  if (phase.get(f) !== v) fail(`f${f}: slot $1C obj_phase $061C = ${phase.get(f)}, want ${v}`);
if (rows.get(KNOCK)?.[webCols.indexOf('sub')] !== 1) fail(`f${KNOCK}: sub_state != 1 (area $02 entry)`);
if (rows.get(LAST)?.[webCols.indexOf('area')] !== 2) fail(`f${LAST}: area != $02`);
console.log(`${fails ? 'FAIL' : 'ok  '} fanskip: ${n} rows vs tools.emu at shift 0 (${special} listed cells), attack f${attackF}, `
  + `slot $1C phase ${want.map(([f]) => phase.get(f)).join('/')} at f${ATTACK - 1}-${ATTACK + 1}`);
process.exit(fails ? 1 : 0);
