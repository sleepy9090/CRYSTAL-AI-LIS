// node web/test/bossflee_check.mjs — BossFleeSpeech 15:BA98 (session 160; obj/ai_boss.js bossFleeSpeech), the
// BossDefeated 15:B807 record hook of areas $28 / $6E / $F2 (here area $28, BossFleeRecs index 2: msg 6.1).
// State (tools.emu scenario `boss_flee`: area $28, type $CF (AI $6F) spawned into slot $0F; the export applies
// the spawn at the main-loop top of the pass that runs BossDefeated):
//   python3 -m tools.emu.scenario_state boss_flee 9 --out web/test/states/bossflee.json
//     --save-state tools/emu/states/bossflee.state          (frame 2503 line 258)
// Reference web/test/bossflee_ref.csv = tools/web_state_compare.py --state tools/emu/states/bossflee.state
// --web web/test/states/bossflee.json --frames 300 --keys 2694:A 2734:A 2774:A --web-key-shift 1, cut by
// tools/csv_trim.py (core,pal,obj,oam_crc) to f2504-2760 (WaitFrames, StatRecalc, slot $1F type $2B, message,
// walk-off until f2752). The port gets each key one frame later (ShowMessage's pads are read in vblank).
// Rule for differing cells: only f2537 (the work after StatRecalc's WaitPpuQueue: ObjInit $1F, ClearEnemySlots)
// may differ, and then the port's NEXT row must have the tools.emu value (tools.emu samples at L255, after that
// work; the port does it in the next frame: the no-cycle-model gap (c)). Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { compareColumns, compareRow, parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const hex = (v) => v.toString(16).padStart(2, '0');
const want = (m, a, v, tag) => { if (m.rd(a) !== v) fail(`${tag}: $${a.toString(16)} = ${hex(m.rd(a))}, want ${hex(v)}`); };
const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/bossflee.json'), 'utf8'));
const LATE_FRAME = 2537, LAST = 2760;

const [head, ...lines] = readFileSync(path.join(WEB, 'test/bossflee_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
if (idx.some((i) => i < 0)) throw new Error('bossflee_ref.csv has a column the port does not write');

const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const keys = parseKeys(['2695:A', '2735:A', '2775:A']);   // --web-key-shift 1
const rows = new Map();
let msg = null;
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  m.runFrame(keys.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (f === 2600) msg = { id: m.rd(0x07DE), group: m.rd(0x07DF), meta: m.rd(0x031F), tile: m.rd(0x033F) };
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
let n = 0, late = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f), next = rows.get(f + 1);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] === w[i]) continue;
    if (f === LATE_FRAME && next && next[idx[i]] === w[i]) { late++; continue; }
    fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} bossflee: ${n} rows vs tools.emu (${late} f${LATE_FRAME} cells 1 frame late)`);

const before = fails;
if (!msg) fail('no f2600 sample');
else {
  if (msg.group !== 0x06 || msg.id !== 0x01) fail(`message ${hex(msg.group)}.${hex(msg.id)}, want 06.01`);
  if (msg.meta !== 0xD3 || msg.tile !== 0x00) fail(`slot $1F meta/tile ${hex(msg.meta)}/${hex(msg.tile)}, want d3/00`);
}
want(m, 0x04BF, 0x00, 'slot $1F off after the walk-off');
want(m, 0x04AF, 0x6E, 'slot $0F = the reward drop (AI $6E)');
// ClearEnemySlots keeps AI $78/$7F/$30/$39 (here $0E = $7F, $10 = $30, the guards' idle ids)
for (const s of [0x0D, 0x11, 0x12]) want(m, 0x04A0 + s, 0, `slot ${hex(s)} cleared`);
want(m, 0x04AE, 0x7F, 'slot $0E kept');
want(m, 0x04B0, 0x30, 'slot $10 kept');
console.log(`${fails > before ? 'FAIL' : 'ok  '} bossflee: record 15:BB1A (msg 6.1, meta $D3), slots after`);
process.exit(fails ? 1 : 0);
