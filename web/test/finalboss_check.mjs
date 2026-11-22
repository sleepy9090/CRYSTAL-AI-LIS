// node web/test/finalboss_check.mjs — AI $6B AiBowOfTruth9E 15:B2DA, the final boss (session 165; obj/ai_boss.js
// ebSetup ... ebRain): phase 0 setup 15:B301 (sprite pass, FadeOut($10), SFX $0C, CHR / palettes, blocks $39-$3C,
// BG strips 4 ... 0, BossBuildParts($26), FadeIn($10)), then sway / hand attacks (phase 1), rain (6) and the
// start of the dive (2).
// State (tools.emu scenario `final_boss_idle`, no keys, type $9E spawned in slot $0D at +10):
//   python3 -m tools.emu.scenario_state final_boss_idle 9 --out web/test/states/finalboss.json
//     --save-state tools/emu/states/finalboss.state
// Reference web/test/finalboss_ref.csv = tools/web_state_compare.py --state tools/emu/states/finalboss.state
// --web web/test/states/finalboss.json --frames 710, cut by tools/csv_trim.py (core,pal,obj,oam_crc) to f2505-3147.
// Every cell must match at shift 0, except the PARTIAL cells (tools.emu's frame ended inside a fade step's
// PaletteBuild: the port's cell = the ref's previous frame) and the LATE music cell at f2590 (the SFX $0C after
// FadeOut shows one frame later in the port; also = the previous frame).
// The setup RTSes with X = 0 (left by the sprite pass), so ObjUpdateAll's INX re-runs slots 1-$1E in the same
// pass and the first sway step lands at f2697 (tools/emu/eb_setup_probe.py). Not covered: from f3148 the dive's
// BG strips + the player hit overrun the frame in tools.emu (lag frames, gap (c)). Exit code 1 on any mismatch.
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
const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/finalboss.json'), 'utf8'));
const PARTIAL = {
  2538: ['pal_crc'], 2555: ['pal_crc'], 2572: ['pal_crc'], 2589: ['pal_crc'],
  2644: ['pal_crc'], 2661: ['pal_crc'], 2678: ['pal_crc'], 2695: ['pal_crc'],
  2590: ['music'],                                     // LATE
};

const [head, ...lines] = readFileSync(path.join(WEB, 'test/finalboss_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
if (idx.some((i) => i < 0)) throw new Error('finalboss_ref.csv has a column the port does not write');
const ref = new Map(lines.map((l) => { const w = l.split(',').map(Number); return [w[0], w]; }));
const LAST = Math.max(...ref.keys());

const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), phases = new Map();
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(0);
  rows.set(f, compareRow(m, f));
  const p = m.rd(0x060D);
  if (!phases.has(p)) phases.set(p, f);
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
let n = 0, np = 0;
for (const [f, w] of ref) {
  const row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  const part = PARTIAL[f] || [];
  for (let i = 1; i < refCols.length; i++) {
    const want = part.includes(refCols[i]) ? ref.get(f - 1)[i] : w[i];
    if (part.includes(refCols[i])) np++;
    if (row[idx[i]] !== want) fail(`f${f}: ${refCols[i]} want ${want} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} finalboss: ${n} rows vs tools.emu at shift 0 (${np} PARTIAL / LATE cells = previous frame)`);

const before = fails;
const want = (what, got, exp) => { if (got !== exp) fail(`${what} = ${hex(got)}, want ${hex(exp)}`); };
const wantF = (what, got, exp) => { if (got !== exp) fail(`${what} = f${got}, want f${exp}`); };
wantF('phase 1 (INC in the setup) first frame', phases.get(1), 2611);   // tools.emu watch: scenario +116 / +297 / +651
wantF('phase 6 (rain) first frame', phases.get(6), 2792);
wantF('phase 2 (dive) first frame', phases.get(2), 3146);
want('obj_phase_s0D', m.rd(0x060D), 2);
want('irq_mode', m.rd(0x51), 7);
want('cam lock $07D7 ($10, one countdown step)', m.rd(0x07D7), 0x0F);
for (let s = 0x0E; s < 0x14; s++) want(`obj_active slot ${hex(s)}`, m.rd(0x04A0 + s) & 0x7F, 0x3F);
console.log(`${fails > before ? 'FAIL' : 'ok  '} finalboss: phases ${[...phases].map(([p, f]) => `${p}@f${f}`).join(' ')}, ` +
  'irq_mode 7, cam lock $0F, parts AI $3F');
process.exit(fails ? 1 : 0);
