// node web/test/group84_check.mjs — area $84's enemy group vs tools.emu (session 190): the area's own spawn list
// has three h0 type-$55 enemies in slots $12/$13/$15 (p=$05 b2=$80, AI $40 = 27:B39E AiWander16: every $D0
// frames DirToTarget(Y=1) + Wander16AimJitter aim, 80 frames aimed, then Wander16DirPerm drift; hp 2, atk 6, def 0,
// elem 0; flies over walls) and an h0 type-$85 (p=$35, AI $21, hp $4A) in slot $11, + h3 $16.
// (Area $2B's single type $55 floats over the solid block between its corridors, out of sword reach, so the
// AI-$40 group check uses area $84 instead.)
// Level 16, sword 1 (`$0711` = 1, `$03E1` = 16 + 1), the stale level-1 defences (atk 6 contact hits for 5).
// Run from the entry point (128, 687): walk UP 40 frames (the bottom exit is right behind the spawn, a knockback
// there leaves the area), wait, swing every 12 frames: slot $12 homes in and dies on the first hit f2715
// ($03D2 2 -> $F0, puff AI $7D, slot clear f2763), slot $13 contact-hits the player f2852 (HP $60 -> $5B,
// knockback to (96, 575)), dies f2877 (clear f2925); slot $15 drifts down from (112, 256), never reached.
// State: python3 -m tools.emu.scenario_state group_84 12 --out web/test/states/group84.json
//          --save-state tools/emu/states/group84.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group84.state --web web/test/states/group84.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group84 --keys 2525:UP:40
//          2595:B:4 .. (every 12, 32 presses) .. 2967:B:4, cut by tools/csv_trim.py to f2506-2984
//          (the state is at f2505 L252).
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes (`--watch 0043`, +1 when line
// >= 241), press and release each (as in group08_check.mjs): presses up to f2847 are read at L261, from f2860
// at L4-5 (the contact hit's lag), so the press of f2859 spans only 3 port frames.
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
const every = (f0, n, len = 4) => Array.from({ length: n }, (_, i) => `${f0 + 12 * i}:B:${len}`);
const KEYS = parseKeys(['2525:UP:40', ...every(2596, 10), '2716:B:4', ...every(2728, 11), '2860:B:3',
  ...every(2871, 9)]);
const KILLS = [[0x12, 2715], [0x13, 2877]];   // slot AI -> $7D (puff), ref o12_ai / o13_ai
const PHP = [0x5B];                            // player HP $03C1 steps

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group84.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group84_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group84_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
const kill = new Map(), php = [];
const ais = [0x04B2, 0x04B3, 0x04B5].map((a) => m.rd(a) & 0x7F);
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  const pBefore = m.rd(0x03C1);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  for (const [s] of KILLS) if (!kill.has(s) && (m.rd(0x04A0 + s) & 0x7F) === 0x7D) kill.set(s, f);
  if (m.rd(0x03C1) !== pBefore) php.push(m.rd(0x03C1));
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ais.some((a) => a !== 0x40)) fail(`slots $12/$13/$15 AI ${ais.map((a) => a.toString(16))}, want $40`);
for (const [s, want] of KILLS) if (kill.get(s) !== want) fail(`slot $${s.toString(16)} kill f${kill.get(s)}, want f${want}`);
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
console.log(`${fails ? 'FAIL' : 'ok  '} group84: ${n} rows vs tools.emu at shift 0 (snd incl.), kills ${KILLS.map(([s]) => `$${s.toString(16)} f${kill.get(s)}`).join(', ')}, player HP ${php.map((v) => '$' + v.toString(16)).join(' ')}`);
process.exit(fails ? 1 : 0);
