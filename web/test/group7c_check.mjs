// node web/test/group7c_check.mjs — area $7C's enemy group vs tools.emu (session 187): the area's own spawn list
// has one h0 type-$82 enemy in slot $14 (p=$32 b2=$80, AI $29 = 27:AB9E AiBurrower, hp $2E, atk $44, def 8,
// elem 2 = immune to sword 2), an h4 flag item in $0E, h3 p=$02 records in $16/$17 and h2 records in $18-$1B.
// The burrower walks underground with obj_coll $41 (group $40: touches the player, but the sword's group misses
// it) and surfaces with obj_coll $11 only while obj_act_timer runs (here f2789-2909, sitting at x=1108 y=842).
// The default entry point is ~950 px away, so the scenario puts the player + camera at (1216, 848) in the
// enemy's pocket (`place_player`). Level 16, sword 1 (`$0711` = 1, `$03E1` = 17) and the StatRecalc defences
// for level 16 + best armour/shield + accessory $10 (`$0401` = $40, `$0400` = $30): with the level-1 values the
// atk-$44 contact hits for $43 and kill the player in two.
// Run: LEFT, DOWN, a tap LEFT to face it; contact hit f2602 (HP $60 -> $5C, body defence $0401); at f2752 the
// player walks UP + LEFT to x=1134 and swings every 12 frames while it is surfaced: sword hits on $03D4 at
// f2799 / 2811 / 2823 / 2835 (`--watch 03D4`, pc 26:9144: 46 -> 36 -> 26 -> 16 -> 6), HP -> $FC at f2847 = the
// kill (EnemyKilled, puff AI $7D in that frame's row, then the slot clears; no drop). No shot hit in this run.
// State: python3 -m tools.emu.scenario_state group_7C 12 --out web/test/states/group7c.json
//          --save-state tools/emu/states/group7c.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group7c.state --web web/test/states/group7c.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group7c --keys 2525:LEFT:14 2539:DOWN:8
//          2547:LEFT:2 2751:UP:9 2760:LEFT:26 2786:B:4 2798:B:4 .. (every 12, 13 presses) .. 2930:B:4,
//          cut by tools/csv_trim.py to f2506-2984 (the state is at f2505).
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
const KEYS = parseKeys(['2525:LEFT:14', '2539:DOWN:9', '2548:LEFT:1', '2752:UP:9', '2761:LEFT:26', '2787:B:4',
  ...Array.from({ length: 12 }, (_, i) => `${2799 + 12 * i}:B:4`)]);
const KILL = 2847;                       // slot $14 AI -> $7D (puff), ref o14_ai
const HITS = [0x24, 0x1A, 0x10, 0x06];    // $03D4 after each sword hit

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group7c.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group7c_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group7c_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
let kill;
const hp = [];
const ai = m.rd(0x04B4) & 0x7F;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  const before = m.rd(0x03D4);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (kill === undefined && m.rd(0x03D4) !== before && (m.rd(0x04B4) & 0x7F) === 0x29) hp.push(m.rd(0x03D4));
  if (kill === undefined && (m.rd(0x04B4) & 0x7F) === 0x7D) kill = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai !== 0x29) fail(`slot $14 AI $${ai.toString(16)}, want $29`);
if (hp.join() !== HITS.join()) fail(`slot $14 HP steps ${hp.map((v) => v.toString(16))}, want ${HITS.map((v) => v.toString(16))}`);
if (kill !== KILL) fail(`slot $14 kill f${kill}, want f${KILL}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group7c: ${n} rows vs tools.emu at shift 0 (snd incl.), ${hp.length} hits, kill slot $14 f${kill}`);
process.exit(fails ? 1 : 0);
