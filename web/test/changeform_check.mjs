// node web/test/changeform_check.mjs — magic 7 change form (session 146; states/changeform.js): SubstChangeForm
// 30:DEC1 + ChangeFormApply 30:DF2F.
// State (town $02, magic 7, MP $60):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 10 --poke 64DE=E0
//     --poke 64DF=FF --poke 03C1=30 --poke 0712=07 --poke 0708=60 --keys 2500:UP:30 2545:A
//     --until 30:DEC1 --out web/test/states/changeform.json --save-state tools/emu/states/changeform.state
// Reference web/test/changeform_ref.csv = tools/web_state_compare.py --state tools/emu/states/changeform.state
// --web web/test/states/changeform.json --frames 300 --keys 2600:RIGHT 2630:RIGHT 2660:A, cut by tools/csv_trim.py
// (core,pal,obj) to f2546-2728 (window, menu, the 31-frame preview, the 32-frame wait, the exit).
// Unlike the telepathy select, the whole menu lines up with no shift. Skipped: slot $1F (o1F_*: tools.emu samples
// it in the middle of ObjSlot1FDraw) and pal_crc at f2555 / f2724 (the palette upload of the window open / exit
// lands one frame apart: vblank work right after the NMI, no cycle model). After the exit the town trigger re-runs
// GiveItem and the NPCs are a step apart (the s140 lag frames), so play is not compared here.
// Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys, compareColumns, compareRow } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const hex = (v) => v.toString(16).padStart(2, '0');
const want = (m, a, v, tag) => { if (m.rd(a) !== v) fail(`${tag}: $${a.toString(16)} = ${hex(m.rd(a))}, want ${hex(v)}`); };

function machine() {
  const m = new Machine(data, { log: () => {} });
  m.reset();
  const s = JSON.parse(readFileSync(path.join(WEB, 'test/states/changeform.json'), 'utf8'));
  m.loadState(s);
  return { m, f0: s.frame0 ?? s.frame };
}

function run(keyArgs, last, onFrame = () => {}) {
  const { m, f0 } = machine();
  const flash0 = m.rd(0x0710);
  const keys = parseKeys(keyArgs);
  while (f0 + m.frame <= last) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    onFrame(f, m);
  }
  if (m.unported.size) fail(`${[...m.unported].join(', ')}`);
  return { m, flash0 };
}

// ---- frame by frame vs tools.emu (RIGHT RIGHT A = form 2)
{
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/changeform_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('changeform_ref.csv has a column the port does not write');
  const rows = new Map();
  run(['2600:RIGHT', '2630:RIGHT', '2660:A'], 2728, (f, m) => rows.set(f, compareRow(m, f)));
  let n = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
    if (!row) { fail(`f${f}: no port row`); continue; }
    n++;
    const d = refCols.filter((c, i) => i && !c.startsWith('o1F_') && !(c === 'pal_crc' && (f === 2555 || f === 2724))
      && row[idx[i]] !== w[i]);
    if (d.length) fail(`f${f}: ${d.map((c) => `${c} want ${w[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} change form 2: ${n} rows vs tools.emu`);
}

/** end-state case at frame `at` */
function check(tag, keyArgs, at, checks) {
  const before = fails;
  const { m, flash0 } = run(keyArgs, at);
  checks(m, flash0);
  console.log(`${fails > before ? 'FAIL' : 'ok  '} change form ${tag}`);
}
// form 2: obj1_flash |= $20 | $80 (b5-4 = form, b7 = changed), MP $60 -> $4C (tools.emu f2727 RAM diff: equal)
check('2 applied', ['2600:RIGHT', '2630:RIGHT', '2660:A'], 2728, (m, f0) => {
  want(m, 0x0710, f0 | 0xA0, 'obj1_flash');
  want(m, 0x0708, 0x4C, 'MP');
  want(m, 0x51, 0x00, 'irq_mode restored');
  want(m, 0x07F4, machine().m.rd(0x07F4), 'R4 play restored');
});
// LEFT from form 0 wraps to 3 (30:DDED & 3): cursor x = 3*48 + $38, then A -> b5-4 = 3
check('LEFT wraps to 3', ['2600:LEFT'], 2605, (m) => {
  want(m, 0x21, 0x03, 'form'); want(m, 0x0203, 0xC8, 'cursor x');
});
check('3 applied', ['2600:LEFT', '2630:A'], 2700, (m, f0) => {
  want(m, 0x0710, f0 | 0xB0, 'obj1_flash');
  want(m, 0x0708, 0x4C, 'MP');
});
// B = 30:DE51 without a change: MP kept, obj1_flash kept, irq_mode restored, sub_state 8
check('cancel (B)', ['2600:B'], 2603, (m, f0) => {
  want(m, 0x41, 0x08, 'cancel sub_state');
  want(m, 0x0708, 0x60, 'cancel MP kept');
  want(m, 0x0710, f0, 'cancel obj1_flash kept');
  want(m, 0x51, 0x00, 'cancel irq_mode restored');
  want(m, 0x0200, 0xF0, 'cancel OAM 0');
});
process.exit(fails ? 1 : 0);
