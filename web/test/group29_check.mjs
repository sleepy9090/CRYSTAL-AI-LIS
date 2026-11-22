// node web/test/group29_check.mjs — area $29's enemy group vs tools.emu (session 184): the area's own spawn list
// is four h0 type-$65 enemies (p=$15, AI $25 = AiChase that idles while offscreen, hp $0F, atk $24, def 4,
// hitflags 6, elem 1 = immune to sword 1) in slots $0E/$14/$15/$17.
// The player (level 16, sword 2 poked: `$0711` = 2, `$03E1` = 17) walks down, right and up into the chaser in slot
// $15 (2 contact hits, ~f2589 / ~f2621, player HP $60 -> $1A), turns LEFT after the knockback and swings: one hit
// (HP $0F -> 0), then the kill at f2665 (borrow -> EnemyKilled, puff AI $7D, coin AI $7B in slot $0D ~f2684-2696).
// Meanwhile the area's respawn entries fill slots $16 $13 $12 $11 $10 $0F (f2548-2660) and $0D (~f2948) with
// more AI $25 chasers. Slot $14 comes up from below: DOWN + B swings, one hit then the kill at f2933.
// (Hit frames are group_probe.py samples, kill frames are the ref's o14_ai / o15_ai = $7D.)
// State: python3 -m tools.emu.scenario_state group_29 12 --out web/test/states/group29.json
//          --save-state tools/emu/states/group29.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group29.state --web web/test/states/group29.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group29 --keys 2526:DOWN:20 2546:RIGHT:40
//          2586:UP:20 2606:RIGHT:2 2628:LEFT:2 2630:B:4 2642:B:4 2654:B:4 2666:B:4 2700:LEFT:30 2830:DOWN:2
//          2832:B:4 2844:B:4 2856:B:4 2868:B:4 2880:B:4 2927:B:4, cut by tools/csv_trim.py to f2508-2984.
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
const KEYS = parseKeys(['2527:DOWN:20', '2547:RIGHT:40', '2587:UP:20', '2607:RIGHT:2', '2629:LEFT:1', '2630:B:4',
  '2642:B:5', '2655:B:4', '2667:B:4', '2701:LEFT:30', '2831:DOWN:2', '2833:B:4', '2845:B:4', '2857:B:4',
  '2869:B:4', '2881:B:4', '2928:B:4']);
const KILLS = { 0x15: 2665, 0x14: 2933 };    // slot AI -> $7D (puff), ref o15_ai / o14_ai

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group29.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group29_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group29_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
const kill = {};
const ai = [0x0E, 0x14, 0x15, 0x17].map((s) => m.rd(0x04A0 + s) & 0x7F);
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  for (const s of Object.keys(KILLS)) if (kill[s] === undefined && (m.rd(0x04A0 + +s) & 0x7F) === 0x7D) kill[s] = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai.some((a) => a !== 0x25)) fail(`slots $0E/$14/$15/$17 AI ${ai.map((a) => a.toString(16))}, want all $25`);
for (const [s, want] of Object.entries(KILLS)) if (kill[s] !== want) fail(`slot $${(+s).toString(16)} kill f${kill[s]}, want f${want}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group29: ${n} rows vs tools.emu at shift 0 (snd incl.), `
  + `kills slot $15 f${kill[0x15]}, slot $14 f${kill[0x14]}`);
process.exit(fails ? 1 : 0);
