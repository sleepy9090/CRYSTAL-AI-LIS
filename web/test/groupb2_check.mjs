// node web/test/groupb2_check.mjs — AI $41 (27:B339 AiShellOnHit) vs tools.emu (session 194): area $B2's h0
// type-$92 in slot $0D (p=$42 b2=$80, hp $8A atk $5E def 0, elem 7 = immune to swords 1-3), + the two type-$91
// AI-$27 walkers $14/$15 (p=$41, hp $3F, elem 5).
// Scenario group_B2: level 16, sword 4 (`$0711` = 4, `$03E1` = 16 + 8), HP / max HP $C0, max defences
// `$0401` = $40 / `$0400` = $30, from the area's entry stairs (464, 175). The run walks UP 40, RIGHT 76 to the top
// of the x-608 corridor, DOWN 20 into it, then swings B every 16 frames. The $92 coming up the corridor is hit at
// f2666 ($03CD $8A -> $6A): obj_active $C1 -> $41 (shell closed, `obj_coll` group cleared) for 63 frames until it
// reopens at f2743 ($41 -> $C1); no later swing reaches it. The $91 in slot $14 is killed at f2871 (puff AI $7D,
// slot clear f2921); the player takes two hits.
// tools.emu has 2 lag frames in the run (f2832, f2839, tools/emu/pass_probe.py).
// State: python3 -m tools.emu.scenario_state group_B2 12 --out web/test/states/groupb2.json
//          --save-state tools/emu/states/groupb2.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/groupb2.state --web web/test/states/groupb2.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/groupb2 --keys $(python3 tools/keyseq.py
//          2512:UP:40 2552:RIGHT:76 2628:DOWN:20 '2648:B:4*22/16'), cut by tools/csv_trim.py to f2508-2986
//          (the state is at f2507 L252).
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes, press and release each
// (`tools.emu ... --watch 0043 | python3 tools/pad_watch_keys.py - --last 2986`).
// PARTIAL cells (tools/cell_class.py; tools.emu's row matches neither port neighbour):
// - oam_crc f2842-44 / f2846-48 (the B held 9 frames after the lag frames f2832 / f2839): tools.emu's L261 row
//   has OAM entries 1..k-1 of row f+1 already written and entry k partly (byte-granular), as in kill_check.mjs;
//   accepted only if such a mix of the port's rows f / f+1 has the reference crc.
// - pos_x/pos_y ($34-$37 zp scratch) f2874, the player-hit frame: tools.emu's row holds the player position
//   (581, 139) mid-routine, the port the pass-end value (465, 27); listed by frame + column.
// Exit code 1 on any other difference.
import { readFileSync } from 'node:fs';
import { crc32 } from 'node:zlib';
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
const LAST = 2986;
const KEYS = parseKeys(('2513:UP:40 2553:RIGHT:76 2629:DOWN:19 2648:B:5 2665:B:4 2681:B:4 2697:B:4 2713:B:4 '
  + '2729:B:4 2745:B:4 2761:B:4 2777:B:4 2793:B:4 2842:B:9 2857:B:4 2873:B:3 2888:B:4 2904:B:4 2920:B:4 2936:B:4 '
  + '2952:B:4 2968:B:4 2984:B:3').split(' '));
const KILLS = [[0x14, 2870]];          // slot AI -> $7D (puff), ref o14_ai
const PHP = [0xB4, 0xB0];              // player HP $03C1 steps
const EHP = [0x6A];                    // slot $0D HP $03CD steps (sword 4 hits for $20)
const SHELL = [[2666, 0x41], [2742, 0xC1]];   // slot $0D obj_active $04AD changes (closed / reopened)
const PARTIAL_OAM = [2842, 2843, 2844, 2846, 2847, 2848];
const PARTIAL = new Map([[2874, ['pos_x', 'pos_y']]]);

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/groupb2.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/groupb2_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('groupb2_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
const kill = new Map(), php = [], ehp = [], shell = [];
const ai = m.rd(0x04AD) & 0x7F;
const oam = new Map();
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  const pBefore = m.rd(0x03C1), eBefore = m.rd(0x03CD), aBefore = m.rd(0x04AD);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  oam.set(f, Uint8Array.from({ length: 256 }, (_, i) => m.rd(0x200 + i)));
  for (const [s] of KILLS) if (!kill.has(s) && (m.rd(0x04A0 + s) & 0x7F) === 0x7D) kill.set(s, f);
  if (m.rd(0x03C1) !== pBefore) php.push(m.rd(0x03C1));
  if (m.rd(0x03CD) !== eBefore) ehp.push(m.rd(0x03CD));
  if (m.rd(0x04AD) !== aBefore) shell.push([f, m.rd(0x04AD)]);
}
const hx = (v) => '$' + v.toString(16);
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai !== 0x41) fail(`slot $0D AI ${hx(ai)}, want $41`);
for (const [s, want] of KILLS) if (kill.get(s) !== want) fail(`slot ${hx(s)} kill f${kill.get(s)}, want f${want}`);
if (php.join() !== PHP.join()) fail(`player HP steps ${php.map(hx)}, want ${PHP.map(hx)}`);
if (ehp.join() !== EHP.join()) fail(`slot $0D HP steps ${ehp.map(hx)}, want ${EHP.map(hx)}`);
if (shell.join() !== SHELL.join()) fail(`slot $0D obj_active ${shell.map(([f, v]) => `f${f}:${hx(v)}`)}, want ${SHELL.map(([f, v]) => `f${f}:${hx(v)}`)}`);
// tools.emu's crc = OAM of row f with entries 1..k-1 rewritten by the pass of row f+1 and entry k partly
// (f2842: entry 3 has the new Y / attr / X and the old tile)
const midPass = (f, want) => {
  const a = oam.get(f), b = oam.get(f + 1);
  if (!a || !b) return false;
  for (let k = 1; k < 64; k++)
    for (let mask = 0; mask < 16; mask++) {
      const mix = Uint8Array.from(a);
      mix.set(b.subarray(4, k * 4), 4);
      for (let j = 0; j < 4; j++) if (mask >> j & 1) mix[k * 4 + j] = b[k * 4 + j];
      if ((crc32(mix) >>> 0) === want) return true;
    }
  return false;
};
let n = 0, partial = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] === w[i]) continue;
    if (refCols[i] === 'oam_crc' && PARTIAL_OAM.includes(f) && midPass(f, w[i])) { partial++; continue; }
    if (PARTIAL.get(f)?.includes(refCols[i])) { partial++; continue; }
    fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
if (partial !== 8) fail(`${partial} PARTIAL cells, want 8`);
console.log(`${fails ? 'FAIL' : 'ok  '} groupb2: ${n} rows vs tools.emu at shift 0 (snd incl., ${partial} PARTIAL), AI $41 slot $0D HP ${ehp.map(hx).join(' ')}, shell ${shell.map(([f, v]) => `f${f}:${hx(v)}`).join(' ')}, slot $14 kill f${kill.get(0x14)}, player HP ${php.map(hx).join(' ')}`);
process.exit(fails ? 1 : 0);
