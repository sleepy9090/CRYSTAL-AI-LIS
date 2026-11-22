// node web/test/swordraise_check.mjs — sub_state 26 SwordRaisePose 19:BC04 (session 149; states/swordraise.js).
// State (town $02; sub_state $1A poked, as event act 22 EvActSwordState 30:D78C stores it):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 100 --poke 0041=1A
//     --until 19:BC04 --out web/test/states/swordraise.json --save-state tools/emu/states/swordraise.state
//   (stops at frame 2550 line 256; --frames 10 misses it: the town-entry GiveItem is still running and stores 8)
// Reference web/test/swordraise_ref.csv = tools/web_state_compare.py --state tools/emu/states/swordraise.state
// --web web/test/states/swordraise.json --frames 133, cut by tools/csv_trim.py (core,pal,obj) to f2551-2682.
// Skipped: `music` at f2551 and `sub` at f2682. Both are writes made right after an NMI (`snd_cur_music` |= $80 after
// the first WaitFrame, sub_state 8 after the last); tools.emu samples its row at L255, after that vblank work, and
// the port makes them one frame later (the no-cycle-model gap (c)). `--ram-diff` at f2682 = only $09 / $41 for the
// same reason. After the handler the player stands on the town trigger (GiveItem 7/8 one frame apart), so play is
// not compared. Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { compareColumns, compareRow } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const hex = (v) => v.toString(16).padStart(2, '0');
const want = (m, a, v, tag) => { if (m.rd(a) !== v) fail(`${tag}: $${a.toString(16)} = ${hex(m.rd(a))}, want ${hex(v)}`); };
const SKIP = { 2551: 'music', 2682: 'sub' };

const m = new Machine(data, { log: () => {} });
m.reset();
const s = JSON.parse(readFileSync(path.join(WEB, 'test/states/swordraise.json'), 'utf8'));
m.loadState(s);
const f0 = s.frame0 ?? s.frame, meta1 = m.rd(0x0301), over1 = m.rd(0x0581);
const rows = new Map();
let mid = false;
while (f0 + m.frame <= 2683) {
  const f = f0 + m.frame;
  m.runFrame(0);
  rows.set(f, compareRow(m, f));
  if (f === 2600) {                                     // mid-pose (tools.emu RAM)
    mid = true;
    want(m, 0x0301, 0xB8, 'slot 1 meta = raise pose');
    want(m, 0x0581, 0x4C, 'slot 1 overlay');
    want(m, 0x07F2, 0x5C, 'chr_r2_play');
    want(m, 0x07F3, 0x42, 'chr_r3_play');
    want(m, 0x06C0, 0x00, 'orbit_level');
    for (let i = 4; i < 0x0C; i++) want(m, 0x04A0 + i, 0, `obj_active slot ${i}`);
    if (!(m.rd(0x0102) & 0x80)) fail('snd_cur_music b7 not set');
  }
}
if (m.unported.size) fail(`${[...m.unported].join(', ')}`);
if (!mid) fail('f2600 not reached');

// ---- frame by frame vs tools.emu
{
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/swordraise_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('swordraise_ref.csv has a column the port does not write');
  const before = fails;
  let n = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
    if (!row) { fail(`f${f}: no port row`); continue; }
    n++;
    const d = refCols.filter((c, i) => i && SKIP[f] !== c && row[idx[i]] !== w[i]);
    if (d.length) fail(`f${f}: ${d.map((c) => `${c} want ${w[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} swordraise: ${n} rows vs tools.emu`);
}

// ---- end state (f2683: the handler has returned)
{
  const before = fails;
  want(m, 0x41, 0x08, 'sub_state');
  want(m, 0x0301, meta1, 'slot 1 meta restored');
  want(m, 0x0581, over1, 'slot 1 overlay restored');
  want(m, 0x04E1, 0x00, 'obj_anim_s01');
  console.log(`${fails > before ? 'FAIL' : 'ok  '} swordraise end state`);
}
process.exit(fails ? 1 : 0);
