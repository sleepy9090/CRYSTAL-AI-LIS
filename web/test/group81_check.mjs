// node web/test/group81_check.mjs — area $81's enemy group vs tools.emu (session 191): the area's spawn list
// has two h0 type-$85 enemies in slots $0D (392, 124) / $0E (376, 300) (p=$35 b2=$00, AI $21 = 27:A969
// AiShooterWalk; hp $4A atk $52 def 8, elem 5 = immune to swords 1 and 3) and two type-$84 (p=$34 b2=$80,
// AI $2E, elem 3) in $0F / $10.
// Level 16, sword 4 (`$0711` = 4, `$03E1` = 16 + 8: elem bit 8 passes both elem masks), HP / max HP $C0, max
// defences `$0401` = $40 / `$0400` = $30 (atk $52 still hits for $12).
// The entry shaft (560, 415) only leads into the lower corridor past the $84 in slot $10, so `place_player`
// drops the player into the vertical shaft at (384, 200) above slot $0E, which walks up it.
// Run: DOWN 2, wait, then (DOWN 6, B 4, idle 6) every 16 frames: $0E is hit f2597 / f2862 / f2891 and dies
// f2923 ($03CE 2 -> $EA, puff AI $7D, slot clear f2975); the player takes 7 hits ($C0 -> $42, contact + shots);
// the $84 in $10 climbs into reach and takes sword-4 hits too ($40 -> $12, its AI $2E phase 1).
// **Only f2506-2604 are compared.** This area runs at the frame budget: ObjUpdateAll (2 AI $21, 2 AI $2E, 2 AI-$70
// phase-12 followers) ~100 lines + ObjSpriteDraw ~65-80 + ObjCollideAll ~60, and CameraTick costs ~50 lines
// instead of ~11 on a vertical scroll. At f2604 ObjCollideAll starts at L235, the NMI finds frame_ready 1 and
// f2605 has no pass (tools/emu/pass_probe.py: 18 such frames in the 480). The port has no general cycle model
// (gap (c)), so from f2605 it runs one pass ahead. The full ref stays as a test case for a future lag model;
// AI $21 itself is checked over a whole run in group84b_check.mjs.
// State: python3 -m tools.emu.scenario_state group_81 12 --out web/test/states/group81.json
//          --save-state tools/emu/states/group81.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group81.state --web web/test/states/group81.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group81 --keys 2505:DOWN:2 2569:DOWN:6
//          2575:B:4 .. (python3 tools/keyseq.py 2505:DOWN:2 '2569:DOWN:6,+6:B:4*26/16'), cut by
//          tools/csv_trim.py to f2506-2984 (the state is at f2505 L252).
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes, press and release each
// (`tools.emu ... --watch 0043 | python3 tools/pad_watch_keys.py -`): many reads land at L2-5 (lag of the
// two shooters' work), so the press lengths vary 3-8 frames.
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
const LAST = 2604;   // f2605 = tools.emu's first lag frame (see above); the ref runs to f2984
const KEYS = parseKeys(('2506:DOWN:2 2569:DOWN:7 2576:B:4 2585:DOWN:7 2592:B:4 2601:DOWN:6 2607:B:5 2618:DOWN:6 '
  + '2624:B:4 2634:DOWN:5 2639:B:4 2650:DOWN:6 2656:B:5 2666:DOWN:6 2672:B:4 2681:DOWN:6 2687:B:5 2698:DOWN:6 '
  + '2704:B:4 2713:DOWN:6 2719:B:5 2729:DOWN:8 2737:B:3 2745:DOWN:7 2752:B:4 2761:DOWN:7 2768:B:4 2777:DOWN:7 '
  + '2784:B:3 2794:DOWN:5 2799:B:5 2810:DOWN:6 2816:B:4 2825:DOWN:6 2831:B:4 2842:DOWN:6 2848:B:4 2858:DOWN:6 '
  + '2864:B:4 2873:DOWN:7 2880:B:4 2889:DOWN:7 2896:B:4 2906:DOWN:5 2911:B:4 2922:DOWN:6 2928:B:4 2937:DOWN:7 '
  + '2944:B:3 2954:DOWN:5 2959:B:4 2970:DOWN:5 2975:B:4').split(' '));
// to f2604 (the whole tools.emu run: player $AE $9C $8A $78 $66 $54 $42, slot $0E $32 $1A $02 $EA, kill f2923)
const KILLS = [];                                            // slot AI -> $7D (puff), ref o0E_ai
const PHP = [0xAE];                                          // player HP $03C1 steps
const EHP = [0x32];                                          // slot $0E HP $03CE steps

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group81.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group81_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group81_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
const kill = new Map(), php = [], ehp = [];
const ais = [0x04AD, 0x04AE].map((a) => m.rd(a) & 0x7F);
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  const pBefore = m.rd(0x03C1), eBefore = m.rd(0x03CE);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  for (const [s] of KILLS) if (!kill.has(s) && (m.rd(0x04A0 + s) & 0x7F) === 0x7D) kill.set(s, f);
  if (m.rd(0x03C1) !== pBefore) php.push(m.rd(0x03C1));
  if (m.rd(0x03CE) !== eBefore && !kill.has(0x0E) || f === kill.get(0x0E)) ehp.push(m.rd(0x03CE));
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ais.some((a) => a !== 0x21)) fail(`slots $0D/$0E AI ${ais.map((a) => a.toString(16))}, want $21`);
for (const [s, want] of KILLS) if (kill.get(s) !== want) fail(`slot $${s.toString(16)} kill f${kill.get(s)}, want f${want}`);
if (php.join() !== PHP.join()) fail(`player HP steps ${php.map((v) => v.toString(16))}, want ${PHP.map((v) => v.toString(16))}`);
if (ehp.join() !== EHP.join()) fail(`slot $0E HP steps ${ehp.map((v) => v.toString(16))}, want ${EHP.map((v) => v.toString(16))}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (f > LAST) break;
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group81: ${n} rows vs tools.emu at shift 0 (snd incl.), kills ${KILLS.map(([s]) => `$${s.toString(16)} f${kill.get(s)}`).join(', ')}, player HP ${php.map((v) => '$' + v.toString(16)).join(' ')}`);
process.exit(fails ? 1 : 0);
