// node web/test/group6b_check.mjs — area $6B's enemy group vs tools.emu (session 185): the area's own spawn list
// is one h0 type-$78 enemy in slot $0D (p=$28, AI $26 = AiShooterChase 27:A9E4, hp $2D, atk $34, def 8,
// elem 2 = immune to sword 2), two h2 records (slots $0E/$0F, AI $78) and the h3 p=$02 record in slot $10.
// The player (level 16, sword 1 poked: `$0711` = 1, `$03E1` = 17) walks down and left along the corridor to
// x=148, faces DOWN and swings every 12 frames while the chaser walks up the column: one contact hit (~f2660,
// player HP $60 -> $2D), sword hits on $03CD at f2661 / f2701 / f2783 / f2857 (`--watch 03CD`, pc 26:9144:
// HP 45 -> 35 -> 25 -> 15 -> 5, each knocks it back down), then DOWN + B: HP -> $FB at f2897 = the kill
// (EnemyKilled, puff AI $7D in the f2897 row, slot cleared ~f2945; no drop).
// The chaser never reaches its attack/shot branch in this run (no shot object appears).
// State: python3 -m tools.emu.scenario_state group_6B 12 --out web/test/states/group6b.json
//          --save-state tools/emu/states/group6b.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group6b.state --web web/test/states/group6b.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group6b --keys 2525:DOWN:24 2549:LEFT:80
//          2629:DOWN:12 2641:B:4 2653:B:4 .. (every 12) .. 2869:B:4 2881:DOWN:10 2891:B:4 2903:B:4 2915:B:4,
//          cut by tools/csv_trim.py to f2508-2984.
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes (`--watch 0043`, +1 when line
// >= 241), press and release each (as in group08_check.mjs). Planning note: group_probe.py holds a segment's
// keys from L252 of the frame before its first printed row, so a probe row F maps to tools.emu `--keys` F-1
// whenever ReadPadsNew runs at L261 (probe frames fed as-is gave 1 hit instead of 5 here).
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
const KEYS = parseKeys(['2525:DOWN:24', '2549:LEFT:80', '2629:DOWN:12', '2641:B:4', '2653:B:4', '2666:B:3',
  ...Array.from({ length: 17 }, (_, i) => `${2677 + 12 * i}:B:4`),
  '2881:DOWN:10', '2891:B:5', '2904:B:4', '2916:B:4']);
const KILL = 2897;                        // slot $0D AI -> $7D (puff), ref o0D_ai

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group6b.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group6b_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group6b_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
let kill;
const ai = m.rd(0x04AD) & 0x7F;
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (kill === undefined && (m.rd(0x04AD) & 0x7F) === 0x7D) kill = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai !== 0x26) fail(`slot $0D AI $${ai.toString(16)}, want $26`);
if (kill !== KILL) fail(`slot $0D kill f${kill}, want f${KILL}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group6b: ${n} rows vs tools.emu at shift 0 (snd incl.), kill slot $0D f${kill}`);
process.exit(fails ? 1 : 0);
