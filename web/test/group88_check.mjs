// node web/test/group88_check.mjs — AI $4E (27:B5D9 AiTurret) vs tools.emu (session 192): area $88's h0
// type-$87 in slot $0D (p=$37 b2=$C0, hp $64 atk $50 def 6, elem 2 = immune to sword 2), at (384, 60) in the
// screen-1 top room; static, every 16 obj_anim steps it spawns its aimed shot (`obj_shot_id` $26 -> AI $10).
// The 7 h0 p=$3F records ($0F-$15, AI $34) sit in the corridor below; a 2nd type-$87 turret (screen 3,
// x 896) refills slot $0E after the kill and fires its own (offscreen, 1-frame) shots.
// Scenario group_88: level 16, sword 1 (`$0711` = 1, `$03E1` = 16 + 1), HP / max HP $C0, max defences
// `$0401` = $40 / `$0400` = $30, `place_player` at (352, 60) left of the turret.
// Run: RIGHT 7 to (366, 60), B every 12 frames ×9: hits for 12 ($03CD $64 -> $58 ... $04 -> $F8), kill f2617
// (puff AI $7D, slot clear f2665; the 2nd turret enters slot $0E f2676), then RIGHT 2 + DOWN 150 out of the room through the bottom door into the
// corridor (vertical scroll; stops at y 358). The turret's shots never hit the player (HP stays $C0).
// tools.emu has no lag frame in the run (tools/emu/pass_probe.py).
// State: python3 -m tools.emu.scenario_state group_88 12 --out web/test/states/group88.json
//          --save-state tools/emu/states/group88.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group88.state --web web/test/states/group88.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group88 --keys 2509:RIGHT:6
//          $(python3 tools/keyseq.py '2515:B:4*9/12') 2640:RIGHT:2 2642:DOWN:150,
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
const KEYS = parseKeys(('2509:RIGHT:7 2516:B:4 2528:B:4 2540:B:4 2552:B:4 2564:B:4 2576:B:4 2588:B:4 2600:B:4 '
  + '2612:B:4 2641:RIGHT:2 2643:DOWN:150').split(' '));
const KILLS = [[0x0D, 2617]];          // slot AI -> $7D (puff), ref o0D_ai
const PHP = [];                        // player HP $03C1 steps (no hit)
const EHP = [0x58, 0x4C, 0x40, 0x34, 0x28, 0x1C, 0x10, 0x04, 0xF8];  // slot $0D HP $03CD (sword 1 hits for 12)
const REFILL = [0x0E, 0x4E];           // slot $0E gets the 2nd turret after the kill

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group88.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group88_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group88_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
const kill = new Map(), php = [], ehp = [];
let refill = 0;
const ai = m.rd(0x04AD) & 0x7F;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  const pBefore = m.rd(0x03C1), eBefore = m.rd(0x03CD);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  for (const [s] of KILLS) if (!kill.has(s) && (m.rd(0x04A0 + s) & 0x7F) === 0x7D) kill.set(s, f);
  if (m.rd(0x03C1) !== pBefore) php.push(m.rd(0x03C1));
  if (m.rd(0x03CD) !== eBefore && ehp.length < EHP.length) ehp.push(m.rd(0x03CD));
  if (!refill && (m.rd(0x04A0 + REFILL[0]) & 0x7F) === REFILL[1]) refill = f;
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (ai !== 0x4E) fail(`slot $0D AI $${ai.toString(16)}, want $4E`);
for (const [s, want] of KILLS) if (kill.get(s) !== want) fail(`slot $${s.toString(16)} kill f${kill.get(s)}, want f${want}`);
if (php.join() !== PHP.join()) fail(`player HP steps ${php.map((v) => v.toString(16))}, want ${PHP.map((v) => v.toString(16))}`);
if (ehp.join() !== EHP.join()) fail(`slot $0D HP steps ${ehp.map((v) => v.toString(16))}, want ${EHP.map((v) => v.toString(16))}`);
if (!refill) fail('slot $0E never got AI $4E');
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
console.log(`${fails ? 'FAIL' : 'ok  '} group88: ${n} rows vs tools.emu at shift 0 (snd incl.), AI $4E slot $0D HP ${ehp.map((v) => '$' + v.toString(16)).join(' ')}, kill f${kill.get(0x0D)}, slot $0E turret f${refill}`);
process.exit(fails ? 1 : 0);
