// node web/test/group05_check.mjs — enemy group of area $05 vs tools.emu (session 181): the area's own spawn list
// (no ObjInit pokes): slots $0D/$13 type $52 (hp 5, def 2, xp 2, drop class 2) = AI $20 AiWalkPause 27:A915. The
// player (level 16, sword 1, attack $03E1 = $11 poked since the pokes skip StatRecalc) climbs to the enemy's row,
// walks left into it (contact hit f2579, HP $60 -> $55) and swings B; the sword kills slot $0D at f2662
// (26:9144 HP 5 -> $F5, EnemyKilled -> AI $7D puff, AddXp 2), its coin hops (ChestHop 27:B79F) and is picked up
// at f2706 (PickupGold 26:946D, gold 2). The area's respawn entries then refill slots $10/$0F/$0E with AI $20
// walkers (f2651-2683), which the player walks into and swings at.
// State: python3 -m tools.emu.scenario_state group_05 12 --out web/test/states/group05.json
//          --save-state tools/emu/states/group05.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group05.state --web web/test/states/group05.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group05 --keys 2527:UP:25 2552:LEFT:34
//          2586:B:4 2600:B:4 2614:B:4 2628:B:4 2642:B:4 2656:B:4 2700:LEFT:20 2765:LEFT:30 2805:B:4 2825:B:4
//          2845:B:4, cut by tools/csv_trim.py to f2507-2985.
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
const KEYS = parseKeys(['2528:UP:24', '2552:LEFT:35', '2587:B:3', '2601:B:4', '2614:B:4', '2629:B:4', '2642:B:4',
  '2657:B:4', '2701:LEFT:19', '2766:LEFT:30', '2806:B:4', '2826:B:4', '2846:B:4']);
const HITS = [[2579, 0x55]];          // tools.emu --watch 03C1 (pc 26:9413)
const KILL = 2662, GOLD = [[2706, 2]]; // --watch 03CD (26:9144 -> $F5) / scenario watch $0702 (26:9480)

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group05.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group05_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group05_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), hits = [], gold = [];
let hp = m.rd(0x03C1), g = m.rd(0x0702), kill = -1;
const ai0 = m.rd(0x04AD);
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (m.rd(0x03C1) !== hp) { hp = m.rd(0x03C1); hits.push([f, hp]); }
  if (m.rd(0x0702) !== g) { g = m.rd(0x0702); gold.push([f, g]); }
  if (kill < 0 && m.rd(0x03CD) === 0xF5) kill = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai0 !== 0x20) fail(`slot $0D AI $${ai0.toString(16)}, want $20`);
if (JSON.stringify(hits) !== JSON.stringify(HITS)) fail(`HP changes ${JSON.stringify(hits)}, want ${JSON.stringify(HITS)}`);
if (kill !== KILL) fail(`slot $0D killed at f${kill}, want f${KILL}`);
if (JSON.stringify(gold) !== JSON.stringify(GOLD)) fail(`gold changes ${JSON.stringify(gold)}, want ${JSON.stringify(GOLD)}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++)
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
}
console.log(`${fails ? 'FAIL' : 'ok  '} group05: ${n} rows vs tools.emu at shift 0 (snd incl.), contact hit `
  + `f${hits.map((h) => h[0]).join('/f')}, kill f${kill}, gold f${gold.map((x) => x[0]).join('/f')}`);
process.exit(fails ? 1 : 0);
