// node web/test/group11_check.mjs — enemy group of area $11 vs tools.emu (session 182): the area's own spawn list
// (no ObjInit pokes): slot $0D type $61 (hp $0C, AI $20 AiWalkPause 27:A915) and slot $11 type $63 (hp $12, def 3,
// atk $25, hitflags $85 = poisonous + level 5, AI $24 AiChase 27:AA57). The player (level 16, sword 1, $03E1 = $11)
// walks down and left between them: contact hit f2694 (HP $60 -> $3C), then PlayerPoisonRoll 26:927E poisons
// (status 3, sub_state $10 + msg $19 at f2726); A closes the message (f2797), a second hit f2836 ($3C -> $18) knocks
// the player right, B swings, UP walks off and the poison tick takes HP $18 -> $14 at f2901.
// State: python3 -m tools.emu.scenario_state group_11 12 --out web/test/states/group11.json
//          --save-state tools/emu/states/group11.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group11.state --web web/test/states/group11.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group11 --keys 2526:DOWN:50 2576:LEFT:70
//          2796:A:4 2840:B:4 2864:UP:30, cut by tools/csv_trim.py to f2507-2984.
// Keys: the port frames are the pass frames of tools.emu's pad_prev $43 writes (`--watch 0043`, +1 when line
// >= 241), press and release each (as in group08_check.mjs).
// PARTIAL cells (tools.emu's row matches neither port neighbour, tools/cell_class.py):
// - oam_crc f2797/98/2800/01/02/04: ShowMessage's closing frames (30:D3AA loop) run the next sprite pass in vblank
//   right after the NMI, so tools.emu's L261 row has OAM entries 1..k-1 from row f+1 (as in kill_check.mjs);
//   accepted only if such a mix of the port's rows f / f+1 has the reference crc.
// - pos_x/pos_y ($34-$37 zp scratch) f2842 (pos_y) and f2900 (both): mid-routine scratch at the row sample
//   (f2899's poison tick write lands at line 1 of the next frame, PlayerStatusTick 31:EF9C); listed by frame + column.
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
const LAST = 2984;
const KEYS = parseKeys(['2527:DOWN:50', '2577:LEFT:69', '2797:A:9', '2841:B:3', '2864:UP:30']);
const HITS = [[2695, 0x3C], [2836, 0x18], [2899, 0x14]]; // tools.emu --watch 03C1 (26:9413 x2, tick 31:EF9C)
const POISON = 2726;                                     // --watch 0041 = $10 (pc 26:92AA)
const PARTIAL_OAM = [2797, 2798, 2800, 2801, 2802, 2804];
const PARTIAL = new Map([[2842, ['pos_y']], [2900, ['pos_x', 'pos_y']]]);

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group11.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group11_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group11_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), oam = new Map(), hits = [];
let hp = m.rd(0x03C1), poison = -1;
const ai = [m.rd(0x04AD), m.rd(0x04B1)];
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  oam.set(f, Uint8Array.from({ length: 256 }, (_, i) => m.rd(0x200 + i)));
  if (m.rd(0x03C1) !== hp) { hp = m.rd(0x03C1); hits.push([f, hp]); }
  if (poison < 0 && m.rd(0x0041) === 0x10) poison = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai[0] !== 0x20 || ai[1] !== 0x24) fail(`slot $0D/$11 AI $${ai[0].toString(16)}/$${ai[1].toString(16)}, want $20/$24`);
if (JSON.stringify(hits) !== JSON.stringify(HITS)) fail(`HP changes ${JSON.stringify(hits)}, want ${JSON.stringify(HITS)}`);
if (poison !== POISON) fail(`poison sub_state $10 at f${poison}, want f${POISON}`);
// tools.emu's crc = OAM of row f with entries 1..k-1 already rewritten by the pass of row f+1
// f2797/f2801: sampled before that pass's OamClipTop 30:C17D, so entries 1-2 still hold an object's tiles CC/CD at
// Y $53 (<= $58, hidden after the clip); the frozen object's unclipped entries = port row f2806's 1-2
const midPass = (f, want) => {
  const a = oam.get(f), b = oam.get(f + 1);
  if (!a || !b) return false;
  const pre = Uint8Array.from(b);
  if (f === 2797 || f === 2801) pre.set(oam.get(2806).subarray(4, 12), 4);
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
    if (PARTIAL.get(f)?.includes(refCols[i])) { partial++; continue; }
    fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
if (partial !== 9) fail(`${partial} PARTIAL cells, want 9`);
console.log(`${fails ? 'FAIL' : 'ok  '} group11: ${n} rows vs tools.emu at shift 0 (snd incl., ${partial} PARTIAL), `
  + `hits f${hits.map((h) => h[0]).join('/f')}, poison f${poison}`);
process.exit(fails ? 1 : 0);
