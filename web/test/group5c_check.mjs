// node web/test/group5c_check.mjs — AI $5C (27:B626 AiPatrolGunner) vs tools.emu (session 195): the type-$A2
// gunner (h0 p=$52 b2=$00, hp $50 atk 0 def 0) in slot $16 of areas $59 / $5A / $5B. It patrols a ledge 58 px
// above the walkway (x 585-737 in $5A, 841-1001 in $59; reverses at screen x < $36 / >= $E8) and, on an odd
// Rand64 every 32 obj_timer steps, stops: `$0620` = $20, obj_elem_drop 0, 4 aimed shots (AI $10) 8 frames apart.
// The ledge is out of sword reach from the walkway, so the runs only take its shots (no hit on it).
// Same groups: type $A0 AI $2A lungers $13-$15 (hp $C8 atk $64) and six invisible h0 p=$51 b2=$C0 AI-$5E records
// $0D-$12 (droppers: contact + falling AI-$10 shots), area $5A also AI $5D in $17.
// Level 16, sword 4, HP / max HP $C0, max defences `$0401` = $40 / `$0400` = $30.
//   group_59: `place_player` (880, 846) on the walkway right of the gunner; UP 40, then B 4 every 16.
//   group_5A: `place_player` (720, 598) (x 656 sits under a dropper); UP 40, then B 4 every 16.
// s215: **the whole refs are compared** (479 rows each, every cell at shift 0 incl. snd): the port's lag model
// (web/src/game/lagmodel.js) reproduces all 11 / 6 overruns ($5A was in no fit: out-of-sample run). Before s215
// only the frames before tools.emu's first CPU overrun were compared (as group81_check.mjs): with the gunner's
// and droppers' shots up to 20 slots are active, ObjUpdateAll takes ~108 lines and ObjCollideAll starts at
// L226-236, so the NMI finds frame_ready 1 and the next frame has no pass (tools/emu/pass_probe.py: area $59
// 11 such frames in 480, first f2611; area $5A 6, first f2600; the idle area-$5B run has none, but its gunner
// sits the same way). The port has no general cycle model (gap (c)), so it runs one pass ahead from there.
// tools.emu's f2579 frame_ready 1 in area $5A is not an overrun (ObjCollideAll at L145): the player-hit frame,
// reproduced by the port. The full refs (f2507-2985 / f2517-2995) stay as test cases for a future lag model.
// State: python3 -m tools.emu.scenario_state group_59 12 --out web/test/states/group59.json
//          --save-state tools/emu/states/group59.state   (f2506 L252; group_5A: group5a.json, f2516)
// Ref:   tools/web_state_compare.py --state tools/emu/states/group59.state --web web/test/states/group59.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group59 --keys $(python3 tools/keyseq.py
//          2510:UP:40 '2550:B:4*27/16')   (group_5A: 2520:UP:40 '2560:B:4*27/16'), cut by tools/csv_trim.py.
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes, press and release each
// (`tools.emu ... --watch 0043 | python3 tools/pad_watch_keys.py - --last F`).
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
const hx = (v) => '$' + v.toString(16);

const RUNS = [
  // s215: the whole refs with the lag model (web/src/game/lagmodel.js; was to f2611 / f2600, the first lag frames)
  { name: '59', last: 2985, php: [0xA7, 0x8E, 0x75, 0x5C, 0x43, 0x2A], attack: 'f2589,f2688,f2851,f2917,f2984',
    keys: '2510:UP:40 2550:B:4 2566:B:4 2582:B:4 2598:B:5 2615:B:4 2631:B:4 2647:B:4 2663:B:4 2679:B:3 2695:B:3 '
      + '2711:B:4 2727:B:4 2743:B:3 2758:B:4 2774:B:4 2790:B:4 2806:B:4 2823:B:4 2839:B:4 2855:B:4 2870:B:5 2886:B:4 '
      + '2902:B:4 2918:B:4 2934:B:4 2951:B:4 2967:B:4' },
  { name: '5a', last: 2995, php: [0x92, 0x83, 0x4B], attack: 'f2567,f2633,f2734,f2894,f2958',
    keys: '2520:UP:40 2560:B:4 2576:B:5 2593:B:4 2609:B:4 2625:B:4 2641:B:4 2656:B:5 2673:B:4 2688:B:4 2705:B:3 '
      + '2721:B:4 2737:B:4 2753:B:4 2769:B:4 2785:B:4 2801:B:4 2817:B:4 2833:B:4 2849:B:4 2865:B:4 2881:B:4 2897:B:4 '
      + '2913:B:4 2929:B:3 2944:B:5 2961:B:4 2977:B:4' },
];
const out = [];
const li = process.argv.indexOf('--last');                   // s215: --last F = compare to F (lag-model study)
if (li > 0) for (const run of RUNS) { run.last = Number(process.argv[li + 1]) + (run.name === '5a' ? 10 : 0); run.php = run.attack = null; }
for (const run of RUNS) {
  const state = JSON.parse(readFileSync(path.join(WEB, `test/states/group${run.name}.json`), 'utf8'));
  const [head, ...lines] = readFileSync(path.join(WEB, `test/group${run.name}_ref.csv`), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
  if (idx.some((i) => i < 0)) throw new Error(`group${run.name}_ref.csv has a column the port does not write`);
  const KEYS = parseKeys(run.keys.split(' '));
  const m = new Machine(data, { log: () => {} });
  m.reset();
  m.loadState(state);
  const f0 = state.frame0 ?? state.frame;
  const rows = new Map(), php = [], attack = [];
  const ai = m.rd(0x04B6);
  while (f0 + m.frame <= run.last) {
    const f = f0 + m.frame;
    const pBefore = m.rd(0x03C1), aBefore = m.rd(0x0636);
    m.runFrame(KEYS.get(f) || 0);
    rows.set(f, compareRow(m, f));
    if (m.rd(0x03C1) !== pBefore) php.push(m.rd(0x03C1));
    if (m.rd(0x0636) !== aBefore && m.rd(0x0636) === 0x20) attack.push(`f${f}`);   // attack start (counts down)
  }
  if (m.unported.size) fail(`${run.name}: unported: ${[...m.unported].join(', ')}`);
  if (ai !== 0xDC) fail(`${run.name}: slot $16 obj_ai ${hx(ai)}, want $dc (AI $5C active)`);
  if (run.php && php.join() !== run.php.join()) fail(`${run.name}: player HP steps ${php.map(hx)}, want ${run.php.map(hx)}`);
  if (run.php && attack.join() !== run.attack) fail(`${run.name}: attack starts ${attack}, want ${run.attack}`);
  let n = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
    if (f > run.last) break;
    if (!row) { fail(`${run.name} f${f}: no port row`); continue; }
    n++;
    for (let i = 1; i < refCols.length; i++)
      if (row[idx[i]] !== w[i]) fail(`${run.name} f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
  out.push(`area $${run.name.toUpperCase()} ${n} rows to f${run.last} (player HP ${php.map(hx).join(' ')}, attack start $0636 = $20 ${attack.join(' ')})`);
}
console.log(`${fails ? 'FAIL' : 'ok  '} group5c: AI $5C vs tools.emu at shift 0 (snd incl.): ${out.join('; ')}`);
process.exit(fails ? 1 : 0);
