// node web/test/group84b_check.mjs — AI $21 (27:A969 AiShooterWalk) vs tools.emu (session 191): area $84's
// h0 type-$85 in slot $11 (p=$35, hp $4A atk $52 def 8, elem 5 = immune to swords 1 and 3), which s190's
// group_84 run never reached; + the three type-$55 AI-$40 wanderers $12/$13/$15 and h3 $16.
// (Area $81, the other type-$85 area, was tried first: its pass of 2 AI $21 + 2 AI $2E + 2 AI-$70 followers runs
// ~240 lines and tools.emu loses 18 frames out of 480 to CPU overruns (CameraTick +40 lines on a vertical
// scroll), which the port does not model, gap (c).)
// Scenario group_84b: level 16, sword 4 (`$0711` = 4, `$03E1` = 16 + 8; elem bit 8 is not in mask 5), HP / max
// HP $C0, max defences `$0401` = $40 / `$0400` = $30, `place_player` at (152, 380) in the y-380 corridor.
// The $85 walks right to x 230, then patrols vertically there (y 375-412, below the corridor line), so the run
// walks RIGHT 20, UP 14, RIGHT 18 to stand above it at (224, 360), taps DOWN, swings B every 12 frames ×5 (hit
// f2617: $03D1 $4A -> $32), then (DOWN 6, B 4, idle 6) every 16 frames following it down: hits f2635 / f2647,
// kill f2655 ($03D1 2 -> $EA, puff AI $7D, slot clear f2703) and the player's one contact hit f2655 ($C0 -> $AE).
// tools.emu has no lag frame in the run (tools/emu/pass_probe.py).
// State: python3 -m tools.emu.scenario_state group_84b 12 --out web/test/states/group84b.json
//          --save-state tools/emu/states/group84b.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group84b.state --web web/test/states/group84b.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group84b --keys $(python3 tools/keyseq.py
//          2505:RIGHT:20 2525:UP:14 2539:RIGHT:18 2557:DOWN:1 '2558:B:4*5/12' '2618:DOWN:6,+6:B:4*23/16'),
//          cut by tools/csv_trim.py to f2506-2984 (the state is at f2505 L252).
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes, press and release each
// (`tools.emu ... --watch 0043 | python3 tools/pad_watch_keys.py - --last 2984`).
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
const KEYS = parseKeys(('2506:RIGHT:19 2525:UP:15 2540:RIGHT:17 2557:DOWN:1 2558:B:5 2571:B:3 2583:B:4 2595:B:4 '
  + '2607:B:4 2619:DOWN:6 2625:B:4 2635:DOWN:6 2641:B:4 2651:DOWN:5 2656:B:4 2666:DOWN:7 2673:B:4 2683:DOWN:6 '
  + '2689:B:4 2699:DOWN:5 2704:B:5 2715:DOWN:6 2721:B:4 2731:DOWN:6 2737:B:4 2747:DOWN:6 2753:B:4 2763:DOWN:6 '
  + '2769:B:4 2779:DOWN:6 2785:B:4 2795:DOWN:6 2801:B:4 2811:DOWN:6 2817:B:4 2827:DOWN:6 2833:B:4 2843:DOWN:6 '
  + '2849:B:4 2859:DOWN:6 2865:B:4 2875:DOWN:6 2881:B:4 2891:DOWN:6 2897:B:4 2907:DOWN:6 2913:B:4 2923:DOWN:6 '
  + '2929:B:4 2939:DOWN:6 2945:B:4 2955:DOWN:6 2961:B:4 2971:DOWN:6 2977:B:4').split(' '));
const KILLS = [[0x11, 2655]];          // slot AI -> $7D (puff), ref o11_ai
const PHP = [0xAE];                    // player HP $03C1 steps
const EHP = [0x32, 0x1A, 0x02, 0xEA];  // slot $11 HP $03D1 steps (sword 4 hits for $18)

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group84b.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group84b_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group84b_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
const kill = new Map(), php = [], ehp = [];
const ai = m.rd(0x04B1) & 0x7F;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  const pBefore = m.rd(0x03C1), eBefore = m.rd(0x03D1);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  for (const [s] of KILLS) if (!kill.has(s) && (m.rd(0x04A0 + s) & 0x7F) === 0x7D) kill.set(s, f);
  if (m.rd(0x03C1) !== pBefore) php.push(m.rd(0x03C1));
  if (m.rd(0x03D1) !== eBefore && ehp.length < EHP.length) ehp.push(m.rd(0x03D1));
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai !== 0x21) fail(`slot $11 AI $${ai.toString(16)}, want $21`);
for (const [s, want] of KILLS) if (kill.get(s) !== want) fail(`slot $${s.toString(16)} kill f${kill.get(s)}, want f${want}`);
if (php.join() !== PHP.join()) fail(`player HP steps ${php.map((v) => v.toString(16))}, want ${PHP.map((v) => v.toString(16))}`);
if (ehp.join() !== EHP.join()) fail(`slot $11 HP steps ${ehp.map((v) => v.toString(16))}, want ${EHP.map((v) => v.toString(16))}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group84b: ${n} rows vs tools.emu at shift 0 (snd incl.), AI $21 slot $11 HP ${ehp.map((v) => '$' + v.toString(16)).join(' ')}, kill f${kill.get(0x11)}, player HP ${php.map((v) => '$' + v.toString(16)).join(' ')}`);
process.exit(fails ? 1 : 0);
