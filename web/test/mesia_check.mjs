// node web/test/mesia_check.mjs — sub_state 27 SubstMesiaReunion 19:BC6B (session 150; states/mesia.js).
// State (town $02; sub_state $1B poked, as event act 30 EvActEnding 30:D827 stores it):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 100 --poke 0041=1B
//     --until 19:BC6B --out web/test/states/mesia.json --save-state tools/emu/states/mesia.state
//   (stops at frame 2550 line 256)
// Reference web/test/mesia_ref.csv = tools/web_state_compare.py --state tools/emu/states/mesia.state
// --web web/test/states/mesia.json --frames 320 --cols core,pal,obj, cut by tools/csv_trim.py (core,pal,obj)
// to f2551-2863.
// Rule for differing cells: the scene does short work right after each WaitFrame (the slot 0/1 step through
// TempAddStep, the slot $15-$18 x/y moves, sub_state 7 at the end).
// tools.emu samples its row at L255, after that work; the port does it in the next frame (the no-cycle-model gap
// (c)). So a cell that differs at shift 0 is accepted only if it is in LATE and the port's NEXT row has the
// tools.emu value. Everything else (spawns of type $C2 in slots $15-$18, meta/AI, the white rows) must be equal at
// shift 0. RAM at f2861 (web_state_compare --ram-diff --ram-ignore 0100-01FF) = only $09, $10-$1C scratch,
// $6E/$6F (tools.emu is inside the sprite pass), $C8 and $07FF (post-NMI). Exit code 1 on any mismatch.
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
const want = (m, a, v, tag, mask = 0xFF) => {
  if ((m.rd(a) & mask) !== v) fail(`${tag}: $${a.toString(16)} = ${hex(m.rd(a))}, want ${hex(v)}`);
};
const LATE = new Set(['sub', 'pos_x', 'pos_y', 'o00_x', 'o00_y', 'o01_x', 'o01_y',
  'o15_x', 'o16_x', 'o17_x', 'o18_x', 'o18_y']);
// The last move of a loop and the slot clear after it (BDAF $04B5-$04B7 = 0, BDD6 $04B8 = 0) run in the same
// post-NMI slice, so the port's next row already shows the slot inactive: the late value is never sampled there.
const SKIP = { 2837: ['o15_x', 'o16_x', 'o17_x'], 2861: ['o18_y'] };
// White flash rows (BDDC fill -> WaitFrame -> PaletteBuild): tools.emu's L255 sample lands inside PaletteBuild
// (a half-rebuilt pal_buf CRC that no port row has). The port must show the all-$30 buffer on these rows.
const WHITE = new Set([2838, 2841, 2844, 2847, 2850, 2853, 2856, 2859]);
const WHITE_CRC = 2700251856;

const m = new Machine(data, { log: () => {} });
m.reset();
const s = JSON.parse(readFileSync(path.join(WEB, 'test/states/mesia.json'), 'utf8'));
m.loadState(s);
const f0 = s.frame0 ?? s.frame;
const rows = new Map();
let mid = false, spawned = false, end = null;
while (f0 + m.frame <= 2863) {
  const f = f0 + m.frame;
  m.runFrame(0);
  rows.set(f, compareRow(m, f));
  if (!end && m.rd(0x41) === 0x07) {                    // the handler's last frame (GiveItem runs in the next one)
    end = { f, ram: [0x0623, 0x057F, 0x04B5, 0x04B6, 0x04B7, 0x04B8].map((a) => m.rd(a)) };
  }
  if (f === 2560) {                                     // first walk step (up)
    mid = true;
    for (const a of [0x0600, 0x0620, 0x06C0, 0x0710]) want(m, a, 0, 'cleared at entry');
    if (m.rd(0x0381) & 0x04) fail('slot 1 flags b2 not cleared');
    want(m, 0x0301, 0x00, 'slot 1 meta (step 0)');
    want(m, 0x0581, 0x08, 'slot 1 overlay (step 0)');
  }
  if (f === 2820) {                                     // all four $C2 spawns placed, fly-out not started
    spawned = true;
    for (let i = 0x15; i <= 0x18; i++) {
      want(m, 0x0300 + i, 0xFE, `slot ${hex(i)} meta`);
      want(m, 0x0380 + i, 0x0D, `slot ${hex(i)} flags`, 0x7F);   // b7 = the sprite pass's off-screen bit
    }
    want(m, 0x22, 0x19, 'next spawn slot');
  }
}
if (m.unported.size) fail(`${[...m.unported].join(', ')}`);
if (!mid || !spawned) fail('check frames not reached');

// ---- frame by frame vs tools.emu
{
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/mesia_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('mesia_ref.csv has a column the port does not write');
  const before = fails;
  let n = 0, late = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f), next = rows.get(f + 1);
    if (!row) { fail(`f${f}: no port row`); continue; }
    n++;
    const d = [];
    refCols.forEach((c, i) => {
      if (c === 'pal_crc' && WHITE.has(f)) {
        if (row[idx[i]] !== WHITE_CRC) d.push(`pal_crc not white (${row[idx[i]]})`);
        return;
      }
      if (!i || row[idx[i]] === w[i] || SKIP[f]?.includes(c)) return;
      if (LATE.has(c) && next && next[idx[i]] === w[i]) { late++; return; }
      d.push(`${c} want ${w[i]} got ${row[idx[i]]}`);
    });
    if (d.length) fail(`f${f}: ${d.join(' ')}`);
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} mesia: ${n} rows vs tools.emu (${late} post-NMI cells one frame late)`);
}

// ---- end state (the frame the handler stored sub_state 7; GiveItem then runs and returns to 8)
{
  const before = fails;
  if (!end) fail('sub_state 7 never stored');
  else {
    if (end.f !== 2862) fail(`sub_state 7 at f${end.f}, want f2862 (tools.emu row f2861: post-NMI)`);
    const exp = [0x1F, 0x04, 0, 0, 0, 0];
    ['$0623 GiveItem slot', '$057F param of slot $1F', 'obj_active $15', 'obj_active $16', 'obj_active $17',
      'obj_active $18'].forEach((t, i) => { if (end.ram[i] !== exp[i]) fail(`${t} = ${hex(end.ram[i])}, want ${hex(exp[i])}`); });
  }
  want(m, 0x41, 0x08, 'sub_state after GiveItem');
  console.log(`${fails > before ? 'FAIL' : 'ok  '} mesia end state`);
}
process.exit(fails ? 1 : 0);
