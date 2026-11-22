// node web/test/group40_check.mjs — area $40's enemy group vs tools.emu (session 186): the area's own spawn list
// is one h0 type-$6A enemy in slot $12 (p=$1A, AI $27 = 27:A9DB, AiShooterChase that idles while offscreen,
// hp $20, atk $2C, def 4, elem 2 = immune to sword 2), the h3 p=$02 record in slot $18 and an h2 record in $19.
// The default entry point is a cave-mouth ledge far from the enemy, so the scenario puts the player + camera at
// (480, 720) in the field above it (`place_player`, player y reads 688). Level 16, sword 1 poked (`$0711` = 1,
// `$03E1` = 17). The player steps RIGHT to x=504, faces DOWN and swings every 12 frames while the chaser walks
// up: sword hits on $03D2 at f2680 / f2746 (`--watch 03D2`, pc 26:9144: HP 32 -> 18 -> 4, each knocks it back
// down), HP -> $F6 at f2824 = the kill (EnemyKilled, puff AI $7D), coin AI $7B in slot $0E (~f2840) picked up
// by the standing player. Before that (~f2674 / ~f2698) the area's respawn entries fill slots $0F / $0D with
// two more AI $27 type-$6A chasers, which stay idle offscreen.
// No contact hit on the player and no shot in this run.
// State: python3 -m tools.emu.scenario_state group_40 12 --out web/test/states/group40.json
//          --save-state tools/emu/states/group40.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group40.state --web web/test/states/group40.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group40 --keys 2528:RIGHT:12 2540:DOWN:2
//          2542:B:4 2554:B:4 .. (every 12, 26 presses) .. 2842:B:4, cut by tools/csv_trim.py to f2509-2985 (the state is at f2508).
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
const LAST = 2985;
const KEYS = parseKeys(['2528:RIGHT:12', '2540:DOWN:3',
  ...Array.from({ length: 26 }, (_, i) => `${2543 + 12 * i}:B:4`)]);
const KILL = 2824;                        // slot $12 AI -> $7D (puff), ref o12_ai

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group40.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group40_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group40_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
let kill;
const ai = m.rd(0x04B2) & 0x7F;
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (kill === undefined && (m.rd(0x04B2) & 0x7F) === 0x7D) kill = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai !== 0x27) fail(`slot $12 AI $${ai.toString(16)}, want $27`);
if (kill !== KILL) fail(`slot $12 kill f${kill}, want f${KILL}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group40: ${n} rows vs tools.emu at shift 0 (snd incl.), kill slot $12 f${kill}`);
process.exit(fails ? 1 : 0);
