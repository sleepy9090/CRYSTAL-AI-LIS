// node web/test/group7f_check.mjs — area $7F's enemy group vs tools.emu (session 189): the area's own spawn list
// has two h0 type-$84 enemies in slots $0E/$0F (p=$34 b2=$80, AI $2E = 27:A9B4: once (obj_param2 = 0) a linked
// SpawnShot child, then AI $2A AiLunger 27:ABF8; hp $40, atk $4E, def 6, elem 3 = immune to swords 1 and 2) and an
// h0 type-$6D (p=$1D, AI $20, hp $14, elem 2) in slot $10.
// Level 16, sword 3 (`$0711` = 3, `$03E1` = 16 + SwordAttackTable[3] = $14), max defences `$0401` = $40 /
// `$0400` = $30 (atk $4E contact hits for $0E).
// Run from the entry point (384, 463): walk UP into slot $0E (contact hit f2623, HP $60 -> $52, knockback),
// swings every 12 frames (the first 10 miss while it is out of reach), step up, swing: $03CE $40 -> $2E f2765,
// $1C f2821, step up, $0A f2897, $F8 at f2935 = the kill (puff AI $7D, slot clears f2983; coin AI $7B in slot
// $13 from f2951, not picked up). Slot $0F stays idle at (488, 124) with an AI-$70 object in slot $11 at
// (496, 102) from the load (its linked child); slot $10 (AI $20) walks about.
// State: python3 -m tools.emu.scenario_state group_7F 12 --out web/test/states/group7f.json
//          --save-state tools/emu/states/group7f.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group7f.state --web web/test/states/group7f.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group7f --keys 2525:UP:100
//          2645:B:4 .. (every 12, 10 presses) .. 2753:B:4 2765:UP:10 2775:B:4 .. (10) .. 2883:B:4 2895:UP:10
//          2905:B:4 .. (7) .. 2977:B:4, cut by tools/csv_trim.py to f2506-2984 (the state is at f2505 L252).
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
const every = (f0, n) => Array.from({ length: n }, (_, i) => `${f0 + 12 * i}:B:4`);
const KEYS = parseKeys(['2525:UP:101', ...every(2645, 10), '2765:UP:11', ...every(2776, 10), '2896:UP:9',
  ...every(2905, 7)]);
const KILL = 2935;                        // slot $0E AI -> $7D (puff), ref o0E_ai
const HITS = [0x2E, 0x1C, 0x0A];          // $03CE after each non-lethal sword hit
const PHP = [0x52];                       // player HP $03C1 steps

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group7f.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group7f_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group7f_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
let kill;
const hp = [], php = [];
const ais = [0x04AE, 0x04AF].map((a) => m.rd(a) & 0x7F);
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  const before = m.rd(0x03CE), pBefore = m.rd(0x03C1);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (kill === undefined && m.rd(0x03CE) !== before && (m.rd(0x04AE) & 0x7F) === 0x2E) hp.push(m.rd(0x03CE));
  if (kill === undefined && (m.rd(0x04AE) & 0x7F) === 0x7D) kill = f;
  if (m.rd(0x03C1) !== pBefore) php.push(m.rd(0x03C1));
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ais.some((a) => a !== 0x2E)) fail(`slots $0E/$0F AI ${ais.map((a) => a.toString(16))}, want $2E`);
if (hp.join() !== HITS.join()) fail(`slot $0E HP steps ${hp.map((v) => v.toString(16))}, want ${HITS.map((v) => v.toString(16))}`);
if (kill !== KILL) fail(`slot $0E kill f${kill}, want f${KILL}`);
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
console.log(`${fails ? 'FAIL' : 'ok  '} group7f: ${n} rows vs tools.emu at shift 0 (snd incl.), ${hp.length + 1} hits, kill slot $0E f${kill}, player HP ${php.map((v) => '$' + v.toString(16)).join(' ')}`);
process.exit(fails ? 1 : 0);
