// node web/test/group6e_check.mjs — AI $3A (27:A555 AiNpcBossReveal) vs tools.emu (session 193): area $6E's
// h4 type-$2A in slot $0E (invisible, hittable, HP $FF) on the slot-$0D NPC (h1 p=$84) at (128, 364).
// Each sword hit while the player is on screen row 1 (y lo $28-$AF): HP back to $FF, obj_phase++, message
// $10.$10/$11/$12 (sub_state $10), closed by the next B; the frame after the third, slots $0D/$0F go off and
// slot $0E becomes type $7D (AI $E6 BossAiE6, HP $94), whose intro runs to the end of the window.
// Scenario group_6E: level 16, sword 1 (`$0711` = 1, `$03E1` = 16 + 1), HP / max HP $C0, max defences,
// `place_player(0x0080, 0x0178, cam=True)` below the NPC.
// Run (tools.emu keys): B:4 every 24 frames from f2512 (`keyseq.py '2512:B:4*19/24'`): hits f2543 / f2616 /
// f2712, reveal f2858. tools.emu's 9 lag frames (pass_probe: f2577 2584 2650 2657 2746 2753 2778 2802 2826,
// at the message open / close redraws) are reproduced by the port, no lag model needed.
// State: python3 -m tools.emu.scenario_state group_6E 12 --out web/test/states/group6e.json
//          --save-state tools/emu/states/group6e.state   (f2508 L255)
// Ref:   tools/web_state_compare.py --state tools/emu/states/group6e.state --web web/test/states/group6e.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group6e --keys <the B list above>,
//          cut by tools/csv_trim.py to f2509-2987.
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes, press and release each
// (`tools.emu ... --watch 0043 | python3 tools/pad_watch_keys.py - --last 2987`).
// Exit code 1 on any difference.
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
const LAST = 2987;
const KEYS = parseKeys(('2513:B:3 2536:B:4 2587:B:9 2609:B:4 2681:B:9 2705:B:4 2777:B:4 2801:B:4 2825:B:4 '
  + '2849:B:9 2873:B:4 2896:B:5 2921:B:4 2945:B:4').split(' '));
const MSGS = [0x10, 0x11, 0x12];       // msg ids on the hits (slot $0E obj_phase 1-3)
const REVEAL = 2858;                   // slot $0E AI -> $66 (type $7D, BossAiE6 id $E6 & $7F); group_probe said 2859 (+1)
// PARTIAL oam_crc cells (tools/cell_class.py): ShowMessage's closing frames (30:D3AA loop, B held) run the next
// sprite pass in vblank right after the NMI, so tools.emu's L261 row = port row f with entries 1..k-1 from row
// f+1, sampled before that pass's OamClipTop 30:C17D: entries 1-2 still hold the sword tiles $3A/$3B at Y $4C
// (<= $58, hidden after the clip) = the port's entries 1-2 on the first row after the message closes.
const PARTIAL_OAM = [2587, 2588, 2589, 2591, 2592, 2593, 2681, 2682, 2683, 2685, 2686, 2687,
  2849, 2850, 2852, 2853, 2854, 2856];

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group6e.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group6e_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group6e_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), oam = new Map();
const phases = [];
let reveal = 0;
const ai = m.rd(0x04AE) & 0x7F;
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  const ph = m.rd(0x060E);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  oam.set(f, Uint8Array.from({ length: 256 }, (_, i) => m.rd(0x200 + i)));  if (!reveal && m.rd(0x060E) !== ph && m.rd(0x060E) !== 0) phases.push(m.rd(0x060E) + 0x0F);
  if (!reveal && (m.rd(0x04AE) & 0x7F) === 0x66) reveal = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai !== 0x3A) fail(`slot $0E AI $${ai.toString(16)}, want $3A`);
if (phases.join() !== MSGS.join()) fail(`msgs ${phases.map((v) => v.toString(16))}, want ${MSGS.map((v) => v.toString(16))}`);
if (reveal !== REVEAL) fail(`reveal f${reveal}, want f${REVEAL}`);
if (m.rd(0x04AD) || m.rd(0x04AF)) fail('slots $0D/$0F still active after the reveal');
const midPass = (f, want) => {
  const a = oam.get(f), b = oam.get(f + 1);
  if (!a || !b) return false;
  let g = f + 1;                       // first later row with entry 1 unclipped (the closed message's next pass)
  while (oam.has(g) && oam.get(g)[4] >= 0xF0) g++;
  const pre = Uint8Array.from(b);
  if (oam.has(g)) pre.set(oam.get(g).subarray(4, 12), 4);
  for (const src of [b, pre])
    for (let k = 2; k <= 64; k++) {
      const mix = Uint8Array.from(a);
      mix.set(src.subarray(4, k * 4), 4);
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
    fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
if (partial !== PARTIAL_OAM.length) fail(`${partial} PARTIAL cells, want ${PARTIAL_OAM.length}`);
console.log(`${fails ? 'FAIL' : 'ok  '} group6e: ${n} rows vs tools.emu at shift 0 (snd incl., ${partial} PARTIAL), `
  + `AI $3A msgs ${phases.map((v) => '$' + v.toString(16)).join(' ')}, reveal f${reveal}`);
process.exit(fails ? 1 : 0);
