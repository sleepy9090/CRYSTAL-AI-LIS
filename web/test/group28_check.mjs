// node web/test/group28_check.mjs — area $28's enemy group vs tools.emu (session 188): the area's own spawn list
// has three h0 type-$64 enemies in slots $11/$12/$16 (p=$14, AI $2A = 27:ABF8 AiLunger, hp $16, atk $30, def 6,
// elem 6 = immune to swords 2 and 3), the boss record h1 p=$C2 in $0D, an h4 flag item in $0E, h1 records in
// $0F/$10/$17 and h2 records in $18-$1C. Frozen while offscreen; near the player it faces it, sets $0640 = $0F and
// at $0640 = 8 spawns its melee shot (AI $10 in a free slot, here slot $12 after the kill).
// The default entry point is ~800 px away, so the scenario puts the player + camera at (1160, 400) between $11 and
// $12 (`place_player`). Level 16, sword 1 (`$0711` = 1, `$03E1` = 17), mid-gear defences `$0401` = $28 /
// `$0400` = $20 (at $40 every hit does 0, at the level-1 values atk $30 kills in 3).
// Run: idle (contact hit f2531, HP $60 -> $58), tap RIGHT to face $12, step right at f2658 and swing: $03D2
// $16 -> $0A at f2670 (pc 26:9144), step right again at f2790 and swing: $FE at f2807 = the kill (puff AI $7D in
// that row, slot clears f2855, no drop); swings every 12 frames after that. Slot $11's contact / melee shots hit
// the player at f2805 / f2837 / f2920 (HP $50 / $48 / $40); its shots (AI $10) reuse slot $12 from f2876.
// State: python3 -m tools.emu.scenario_state group_28 12 --out web/test/states/group28.json
//          --save-state tools/emu/states/group28.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group28.state --web web/test/states/group28.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group28 --keys 2546:RIGHT:2 2658:RIGHT:6
//          2664:B:4 2789:RIGHT:10 2799:B:4 2811:B:4 .. (every 12, 16 presses) .. 2979:B:4,
//          cut by tools/csv_trim.py to f2507-2984 (the state is at f2506).
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes (`--watch 0043`, +1 when line
// >= 241), press and release each (as in group08_check.mjs).
// Exit code 1 on any difference.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { compareColumns, compareRow, parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const verbose = process.argv.includes('-v');
let fails = 0;
const fail = (msg) => { fails++; if (verbose || fails <= 30) console.log('  FAIL ' + msg); };
const LAST = 2984;
const KEYS = parseKeys(['2546:RIGHT:3', '2658:RIGHT:7', '2665:B:4', '2790:RIGHT:10', '2800:B:4', '2812:B:3',
  '2823:B:4', '2835:B:5', '2847:B:4', '2859:B:4', '2871:B:4', '2883:B:4', '2895:B:4', '2907:B:4', '2919:B:5',
  '2931:B:4', '2943:B:4', '2955:B:4', '2967:B:4', '2979:B:4']);
const KILL = 2807;                        // slot $12 AI -> $7D (puff), ref o12_ai
const HITS = [0x0A];                      // $03D2 after each non-lethal sword hit
const PHP = [0x58, 0x50, 0x48, 0x40];     // player HP $03C1 steps

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group28.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group28_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group28_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
let kill;
const hp = [], php = [];
const ais = [0x04B1, 0x04B2, 0x04B6].map((a) => m.rd(a) & 0x7F);
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  const before = m.rd(0x03D2), pBefore = m.rd(0x03C1);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (kill === undefined && m.rd(0x03D2) !== before && (m.rd(0x04B2) & 0x7F) === 0x2A) hp.push(m.rd(0x03D2));
  if (kill === undefined && (m.rd(0x04B2) & 0x7F) === 0x7D) kill = f;
  if (m.rd(0x03C1) !== pBefore) php.push(m.rd(0x03C1));
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ais.some((a) => a !== 0x2A)) fail(`slots $11/$12/$16 AI ${ais.map((a) => a.toString(16))}, want $2A`);
if (hp.join() !== HITS.join()) fail(`slot $12 HP steps ${hp.map((v) => v.toString(16))}, want ${HITS.map((v) => v.toString(16))}`);
if (kill !== KILL) fail(`slot $12 kill f${kill}, want f${KILL}`);
if (php.join() !== PHP.join()) fail(`player HP steps ${php.map((v) => v.toString(16))}, want ${PHP.map((v) => v.toString(16))}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group28: ${n} rows vs tools.emu at shift 0 (snd incl.), ${hp.length + 1} hits, kill slot $12 f${kill}, player HP ${php.map((v) => '$' + v.toString(16)).join(' ')}`);
process.exit(fails ? 1 : 0);
