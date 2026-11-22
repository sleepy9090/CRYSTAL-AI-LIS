// node web/test/formrevert_check.mjs — form revert (session 148; states/formrevert.js): SubstFormRevert 19:BB9D.
// State (town $02, changed form from changeform.state: RIGHT, RIGHT, A picks the form, then A = magic again):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/changeform.state --frames 0
//     --keys 2600:RIGHT 2630:RIGHT 2660:A 2760:A --until 19:BB9D --out web/test/states/formrevert.json
//     --save-state tools/emu/states/formrevert.state
// Reference web/test/formrevert_ref.csv = tools/web_state_compare.py --state tools/emu/states/formrevert.state
// --web web/test/states/formrevert.json --frames 60 (no keys), cut by tools/csv_trim.py (core,pal,obj) to
// f2761-2797 (black BG, the 31-frame revert animation, the tail with sub_state 8). Every column of every row
// matches at shift 0; RAM + WRAM are identical at the end of f2795 apart from the stack page and $10-$1F.
// Not compared: oam_crc (the same 8-frame sampling rows as the cure run), snd*_period at f2773/f2787 (the
// tools.emu note-change row timing), and play after f2797: the player stands on the town trigger, so
// TalkProbeItemUse 26:94AB sets sub_state 7 every frame; tools.emu runs that GiveItem pass at L253 of the same
// frame (sub = 8 again at the L255 sample), the port one frame later (the no-cycle-model gap (c)).
// Exit code 1 on any mismatch.
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

function run(last, poke = {}, onFrame = () => {}) {
  const m = new Machine(data, { log: () => {} });
  m.reset();
  const s = JSON.parse(readFileSync(path.join(WEB, 'test/states/formrevert.json'), 'utf8'));
  m.loadState(s);
  for (const [a, v] of Object.entries(poke)) m.wr(Number(a), v);
  const f0 = s.frame0 ?? s.frame;
  const start = { flash: m.rd(0x0710), flags1: m.rd(0x0381) };
  while (f0 + m.frame <= last) {
    const f = f0 + m.frame;
    m.runFrame(0);
    onFrame(f, m);
  }
  if (m.unported.size) fail(`${[...m.unported].join(', ')}`);
  return { m, start };
}

// ---- frame by frame vs tools.emu
{
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/formrevert_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('formrevert_ref.csv has a column the port does not write');
  const rows = new Map();
  run(2797, {}, (f, m) => rows.set(f, compareRow(m, f)));
  let n = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
    if (!row) { fail(`f${f}: no port row`); continue; }
    n++;
    const d = refCols.filter((c, i) => i && row[idx[i]] !== w[i]);
    if (d.length) fail(`f${f}: ${d.map((c) => `${c} want ${w[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} formrevert: ${n} rows vs tools.emu`);
}

/** during the animation (f2790; the countdown ends at f2794, then StatRecalc rewrites R3 / slot 1) and after
 *  the tail (f2797: sub_state 8) */
function check(tag, poke, r2) {
  const before = fails;
  let mid = null;
  const { m, start } = run(2797, poke, (f, mm) => {
    if (f === 2790) mid = { r2: mm.rd(0x07F2), r3: mm.rd(0x07F3), ov: mm.rd(0x0581), meta1: mm.rd(0x0301),
      flash: mm.rd(0x0710), flags1: mm.rd(0x0381), ofs: mm.rd(0x0321), sub: mm.rd(0x41) };
  });
  const w2 = (k, v, t) => { if (mid[k] !== v) fail(`${t} at f2790 =${hex(mid[k])}, want ${hex(v)}`); };
  w2('r2', r2, 'chr_r2_play');
  w2('r3', 0x5C, 'chr_r3_play');
  w2('ov', 0xD0, 'slot 1 overlay');
  w2('meta1', 0x4C, 'slot 1 meta');
  w2('ofs', 0x00, 'slot 1 tile ofs');
  w2('flash', start.flash & 0x4F, 'obj1_flash (form bits cleared)');
  w2('flags1', start.flags1 | 0x02, 'slot 1 flags b1');
  w2('sub', 0x19, 'sub_state during the animation');
  want(m, 0x41, 0x08, 'sub_state after the tail');
  want(m, 0x04E1, 0x00, 'obj_anim_s01');
  if (start.flash & 0x80) { /* ok */ } else fail('state is not in a changed form (obj1_flash b7 clear)');
  console.log(`${fails > before ? 'FAIL' : 'ok  '} formrevert ${tag}`);
}
check('no shield', { 0x0714: 0 }, 0x40);
check('shield', { 0x0714: 1 }, 0x41);
process.exit(fails ? 1 : 0);
