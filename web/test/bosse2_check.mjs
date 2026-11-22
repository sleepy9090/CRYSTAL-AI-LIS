// node web/test/bosse2_check.mjs — AI $62 BossAiE2 15:A675 (session 163; obj/ai_boss.js bossAiE2), the area $1A
// boss woken by the Insect Flute: phase 0 (camera aligned: music $12, CHR / palettes, BossBuildParts, fades,
// WRAM image + PPU blocks $2C/$2D, obj_coll $13) and phase 1 (walk / attack, BossEBMove, shots + minion).
// State (tools.emu scenario `boss_e2`, flute used at +20):
//   python3 -m tools.emu.scenario_state boss_e2 30 --out web/test/states/bosse2.json
//     --save-state tools/emu/states/bosse2.state
// Reference web/test/bosse2_ref.csv = tools/web_state_compare.py --state tools/emu/states/bosse2.state
// --web web/test/states/bosse2.json --frames 385, cut by tools/csv_trim.py (core,pal,obj,oam_crc) to f2524-2907.
// Every cell must match at shift 0 (no keys), except the PARTIAL cells: tools.emu's frame ended in the middle of a
// PaletteBuild (fade steps) or of BossBuildParts($13), so its row shows a half-done value; the port's cell must
// then equal the ref's previous frame (the routine has not run yet) and the next row matches as usual.
// The intro's first frame overruns in the ROM (NMI at f2535 L241 inside the sprite pass, frame_ready 1): the
// port yields SPIN there (tools/emu/e2_intro_probe.py). Not covered: from f2909 the fight's shots give tools.emu
// lag frames the port does not model (gap (c)). Exit code 1 on any mismatch.
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
const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/bosse2.json'), 'utf8'));
const PARTIAL = {
  2552: ['pal_crc'], 2786: ['pal_crc'], 2819: ['pal_crc'], 2852: ['pal_crc'], 2885: ['pal_crc'],
  2707: ['o0E_y', 'o0F_x', 'o0F_y'],
};

const [head, ...lines] = readFileSync(path.join(WEB, 'test/bosse2_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
if (idx.some((i) => i < 0)) throw new Error('bosse2_ref.csv has a column the port does not write');
const ref = new Map(lines.map((l) => { const w = l.split(',').map(Number); return [w[0], w]; }));
const LAST = Math.max(...ref.keys());

const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
let phase1 = 0;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(0);
  rows.set(f, compareRow(m, f));
  if (!phase1 && m.rd(0x060D) === 1) phase1 = f;
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
console.log(`${fails ? 'FAIL' : 'ok  '} bosse2: ${n} rows vs tools.emu at shift 0 (${np} PARTIAL cells = previous frame)`);

const before = fails;
const want = (what, got, exp) => { if (got !== exp) fail(`${what} = ${hex(got)}, want ${hex(exp)}`); };
want('obj_phase_s0D', m.rd(0x060D), 1);
want('obj_coll_s0D', m.rd(0x03AD), 0x13);
want('irq_mode', m.rd(0x51), 7);
for (let s = 0x0E; s < 0x14; s++) want(`obj_active slot ${hex(s)}`, m.rd(0x04A0 + s) & 0x7F, 0x3F);
console.log(`${fails > before ? 'FAIL' : 'ok  '} bosse2: phase 1 from f${phase1}, coll $13, irq_mode 7, parts AI $3F`);
process.exit(fails ? 1 : 0);
