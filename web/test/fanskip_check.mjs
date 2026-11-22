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
//          f2509-2718 -> web/test/fanskip_ref.csv (all cells equal at shift 0, snd included).
// Not covered (from f2719, the area-$02 entry): mid-fade pal_crc rows f2719/22/25/28, music + object clear at
// f2729 one frame late in the port, pos_y f2730-2736 (see the task text in REVERSE.md).
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
const LAST = 2718, ATTACK = 2679;

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
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++)
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
}
if (attackF !== ATTACK) fail(`fan attack start ($064F = $1F) at f${attackF}, want f${ATTACK}`);
// the skipped frame: slot $1C's obj_phase holds on f2679 and steps on the frames around it
const want = [[ATTACK - 1, 0x0A], [ATTACK, 0x0A], [ATTACK + 1, 0x09]];
for (const [f, v] of want)
  if (phase.get(f) !== v) fail(`f${f}: slot $1C obj_phase $061C = ${phase.get(f)}, want ${v}`);
if (rows.get(LAST)?.[webCols.indexOf('sub')] !== 1) fail(`f${LAST}: sub_state != 1 (area $02 entry)`);
console.log(`${fails ? 'FAIL' : 'ok  '} fanskip: ${n} rows vs tools.emu at shift 0, attack f${attackF}, `
  + `slot $1C phase ${want.map(([f]) => phase.get(f)).join('/')} at f${ATTACK - 1}-${ATTACK + 1}`);
process.exit(fails ? 1 : 0);
