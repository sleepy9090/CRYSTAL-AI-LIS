// node web/test/group08_check.mjs — enemy group of area $08 vs tools.emu (session 179): the area's own spawn
// list (no ObjInit pokes): slot $0D type $54 = AI $28 AiAimShooter 27:AB61 (start delay, AiChase, aimed shot type
// $45 via DirToTarget at obj_act_timer $0F), slot $0E = AI $78 (the area's h2 p=$24 entry). The player walks up to the shooter
// and stands in its line; its shots hit 3 times (HitPlayerShot 26:92BF -> hitPlayer, HP $60 -> $33).
// State: python3 -m tools.emu.scenario_state group_08 12 --out web/test/states/group08.json
//          --save-state tools/emu/states/group08.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/group08.state --web web/test/states/group08.json
//          --frames 480 --cols core,snd,pal,obj,oam_crc --out-dir gfx/group08
//          --keys 2525:UP:85 2695:LEFT:16 2755:RIGHT:32 2825:DOWN:20, cut by tools/csv_trim.py to f2507-2985.
// Keys: ReadPadsNew 31:FE97 runs near the frame boundary here (L2-6 of frame F, or L261 of F-1), so the port key
// frames are the pass frames of tools.emu's pad_prev $43 writes (`--watch 0043`, +1 when line >= 241), press and
// release each: UP 2526 x84, LEFT 2696 x16, RIGHT 2756 x31, DOWN 2826 x19. Feeding the tools.emu frames with a
// global --web-key-shift leaves 1-frame walk-counter ($04E1) offsets = oam_crc rows every 8 frames.
// All 479 rows equal at shift 0, snd included; exit code 1 on any difference.
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
const KEYS = parseKeys(['2526:UP:84', '2696:LEFT:16', '2756:RIGHT:31', '2826:DOWN:19']);
const HITS = [[2761, 0x51], [2793, 0x42], [2930, 0x33]];   // tools.emu --watch 03C1 (pc 26:9413)

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/group08.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/group08_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('group08_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), hits = [];
let hp = m.rd(0x03C1), shooterAi = -1;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  if (m.rd(0x03C1) !== hp) { hp = m.rd(0x03C1); hits.push([f, hp]); }
  if (shooterAi < 0) shooterAi = m.rd(0x04AD);
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (shooterAi !== 0x28) fail(`slot $0D AI $${shooterAi.toString(16)}, want $28`);
if (JSON.stringify(hits) !== JSON.stringify(HITS))
  fail(`HP changes ${JSON.stringify(hits)}, want ${JSON.stringify(HITS)}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++)
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
}
console.log(`${fails ? 'FAIL' : 'ok  '} group08: ${n} rows vs tools.emu at shift 0 (snd incl.), `
  + `${hits.length} shot hits at f${hits.map((h) => h[0]).join('/f')}`);
process.exit(fails ? 1 : 0);
