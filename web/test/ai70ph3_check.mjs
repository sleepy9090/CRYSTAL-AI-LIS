// node web/test/ai70ph3_check.mjs — AI $70 phases 3-6 (27:BC0C / BC37 / BC71 / BC94) in area $5F, session 174
// (obj/ai_final.js). Type $A4 (AI $F0, starts at phase 2) in slot $0D: phase 2 -> SubstRestoreScreen, phase 3 =
// three FadeIn(9) with pal ids $07E2/1/0 = $A4/$A3/$A2 + PlaySound($14), then phases 4-6 (flash IRQ, shot $29
// bursts) until the AI-7 shots kill the player (PlayerDeath at f2733).
// Two lag models are checked here (no CPU cycle count in the port):
//  * f2670: the HUD IRQ's SoundUpdate that starts music $14 runs L191-L241, so the NMI finds nmi_busy_flag set
//    and the frame is lost (mainloop.js irqHud / nmiLostToHud, MUSIC_START_CYCLES vs HUD_SOUND_BUDGET);
//  * phases 4-6: a pass with >= 14 active slots on an even $08 or >= 17 overruns (ai_final.js flashIrq).
// State: web/test/states/restore5f.json (session 120, tools/emu/states/restore5f_s120.state, f2507).
// Reference web/test/ai70ph3_ref.csv = tools/web_state_compare.py --state tools/emu/states/restore5f_s120.state
// --web web/test/states/restore5f.json --frames 400 --out-dir gfx/compare/ai70ph3, cut by tools/csv_trim.py
// (core,pal,obj,oam_crc) to f2507-2757 (snd left out: the 1-frame snd*_period offset, as in bossa6_check).
// Every cell must match at shift 0 except the listed ones, each checked for its 1-frame relation:
//   PARTIAL: tools.emu samples its row in the middle of a FadeIn step's PaletteBuild; the port has the
//            previous row's value (and the next row matches again);
//   EARLY:   the port has tools.emu's next-row value (work done after line 0 of a lagging / long pass);
//   LATE:    the port has the value one row later (a music start in the HUD IRQ, as finalboss_check f2590).
// Exit code 1 on any mismatch.
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
const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/restore5f.json'), 'utf8'));
const LAST = 2758;

const PARTIAL = new Map([2536, 2546, 2556, 2566, 2597, 2607, 2617, 2648, 2658, 2668].map((f) => [f, ['pal_crc']]));
const EARLY = new Map([[2732, ['o10_ai', 'o10_x', 'o10_y']]]);     // the hit pass f2732 L253 -> f2733 L12
// f2515: SubstRestoreScreen's end (s120 row timing); f2669 / f2736: music $14 / the death music start
const LATE = new Map([[2515, ['sub']], [2669, ['music']], [2736, ['music']]]);

const [head, ...lines] = readFileSync(path.join(WEB, 'test/ai70ph3_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
if (idx.some((i) => i < 0)) throw new Error('ai70ph3_ref.csv has a column the port does not write');
const ref = new Map(lines.map((l) => { const w = l.split(',').map(Number); return [w[0], w]; }));

const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
let hitFrame = -1, deathFrame = -1, pal3 = null;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(0);
  rows.set(f, compareRow(m, f));
  if (hitFrame < 0 && m.rd(0x03C1) === 0) hitFrame = f;
  if (deathFrame < 0 && m.rd(0x41) === 3) deathFrame = f;
  // phase 3 INCs the phase before its fades, so read the ids once phase 4 has run
  if (!pal3 && m.rd(0x060D) >= 5) pal3 = [m.rd(0x07E0), m.rd(0x07E1), m.rd(0x07E2), m.rd(0x07E8)];
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);

const get = (f, c) => rows.get(f)?.[webCols.indexOf(c)];
const want = (f, c) => ref.get(f)?.[refCols.indexOf(c)];
let n = 0, special = 0;
for (const [f, w] of ref) {
  const row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    const c = refCols[i];
    if (row[idx[i]] === w[i]) continue;
    if (PARTIAL.get(f)?.includes(c) && row[idx[i]] === want(f - 1, c) && get(f + 1, c) === want(f + 1, c)) { special++; continue; }
    if (EARLY.get(f)?.includes(c) && row[idx[i]] === want(f + 1, c)) { special++; continue; }
    if (LATE.get(f)?.includes(c) && get(f + 1, c) === w[i]) { special++; continue; }
    fail(`f${f}: ${c} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} ai70ph3: ${n} rows vs tools.emu at shift 0 (${special} listed 1-frame cells)`);

const before = fails;
if (!pal3 || pal3.join() !== [0xA2, 0xA3, 0xA4, 0].join()) fail(`after phase 3: $07E0-2 / $07E8 = ${pal3?.map(hex).join(' ')}, want a2 a3 a4 00`);
if (hitFrame !== 2732) fail(`player HP 0 at f${hitFrame}, want f2732 (tools.emu f2733 L1, the overrunning pass)`);
if (deathFrame !== 2734) fail(`sub_state 3 (PlayerDeath) at f${deathFrame}, want f2734`);
console.log(`${fails > before ? 'FAIL' : 'ok  '} ai70ph3: phase-3 palette ids ${pal3?.map(hex).join(' ')}, `
  + `fatal hit f${hitFrame}, PlayerDeath f${deathFrame}`);
process.exit(fails ? 1 : 0);
