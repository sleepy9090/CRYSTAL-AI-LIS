// node web/test/bossa6_check.mjs — BossHookA6 15:B8FD (sessions 161-162; obj/ai_boss.js bossHookA6), the
// BossDefeated 15:B807 record hook of area $A6 (BossRecs index 11): irq_mode 0, slots $0D-$13 off, sprite pass,
// player pos <- camera, slots $14/$15 re-inited, rendering off, WaitFrame, FullScreenDraw(1), rendering on.
// State (tools.emu scenario `final_locked`, area $A6):
//   python3 -m tools.emu.scenario_state final_locked 9 --out web/test/states/finallocked.json
//     --save-state tools/emu/states/finallocked.state
// Reference web/test/bossa6_ref.csv = tools/web_state_compare.py --state tools/emu/states/finallocked.state
// --web web/test/states/finallocked.json --frames 40, cut by tools/csv_trim.py (core,pal,obj,oam_crc) to
// f2505-2535 (the hook's draw f2505-2510 with the NMIs after rows 9/15/21/27 = FSD_LAG_WF, then AI $70).
// Every cell must match at shift 0 (no keys). Exit code 1 on any mismatch.
// Not covered: from f2536 (AI $70 phase 1's spawn sequence) the port runs ~1 frame behind (lag model (c)).
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { compareColumns, compareRow } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const hex = (v) => v.toString(16).padStart(2, '0');
const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/finallocked.json'), 'utf8'));
const LAST = 2535;

const [head, ...lines] = readFileSync(path.join(WEB, 'test/bossa6_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
if (idx.some((i) => i < 0)) throw new Error('bossa6_ref.csv has a column the port does not write');

const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), mask = new Map();
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(0);
  rows.set(f, compareRow(m, f));
  mask.set(f, m.rd(0x01) & 0x18);
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
console.log(`${fails ? 'FAIL' : 'ok  '} bossa6: ${n} rows vs tools.emu at shift 0`);

const before = fails;
const off = [...mask].filter(([, v]) => v === 0).map(([f]) => f);
if (!off.length) fail('rendering ($01 & $18) never off during the hook');
if (mask.get(LAST) !== 0x18) fail(`$01 & $18 = ${hex(mask.get(LAST))} at f${LAST}, want 18`);
if (m.rd(0x51) !== 0) fail(`irq_mode $51 = ${hex(m.rd(0x51))}, want 00`);
console.log(`${fails > before ? 'FAIL' : 'ok  '} bossa6: rendering off f${off[0]}-f${off.at(-1)}, back on, irq_mode 0`);
process.exit(fails ? 1 : 0);
