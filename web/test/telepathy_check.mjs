// node web/test/telepathy_check.mjs — magic 3 telepathy (session 145; states/telepathy.js): SubstTelepathySelect
// 30:DD6B + SubstTelepathyReply 30:DE04 + Telepathy 14:816F.
// States (town $02, MP 03C1=30 0712=03 0708=60; `hint` adds 0421=10 = level 16):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 10 --poke 64DE=E0
//     --poke 64DF=FF --poke 03C1=30 --poke 0712=03 --poke 0708=60 [--poke 0421=10] --keys 2500:UP:30 2545:A
//     --until 30:DD6B --out web/test/states/telepathy_NAME.json --save-state tools/emu/states/telepathy_NAME.state
// Reference web/test/telepathy_ref.csv = tools/web_state_compare.py --state tools/emu/states/telepathy_magic.state
// --frames 300 --keys 2600:RIGHT 2630:A 2720:A, cut by tools/csv_trim.py (core,pal,obj) to f2546-2843.
// The port and tools.emu run the same frames, but a tools.emu CSV row also holds ROM work done in the vblank
// right after that frame's NMI, which the port does in its next frame. So the ref is compared per phase with a
// shift: window open f2546-2554 (0), select + reply f2555-2718 (port 1 frame later), exit f2719-2721 (0),
// play f2723-2843 (0, without `sub`: the town trigger under the player re-runs GiveItem every frame, 7 -> 8 inside
// one tools.emu frame, and tools.emu samples it after GiveItem, the port before). Slot $1F (o1F_*) is skipped:
// tools.emu samples it in the middle of ObjSlot1FDraw (the reply's draw runs from line 252 to line 26).
// The message / MP / RNG bytes of the Kensu and hint cases match tools.emu (web_state_compare --ram-diff at
// f2700, only mid-draw scratch differs); their values are checked here. Exit code 1 on any mismatch.
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

function machine(name) {
  const m = new Machine(data, { log: () => {} });
  m.reset();
  const s = JSON.parse(readFileSync(path.join(WEB, `test/states/telepathy_${name}.json`), 'utf8'));
  m.loadState(s);
  return { m, f0: s.frame0 ?? s.frame };
}

/** run a state with keys up to frame `last`; onFrame(f, m) after each frame */
function run(name, keyArgs, last, onFrame = () => {}) {
  const { m, f0 } = machine(name);
  const keys = parseKeys(keyArgs);
  while (f0 + m.frame <= last) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    onFrame(f, m);
  }
  if (m.unported.size) fail(`${name}: ${[...m.unported].join(', ')}`);
  return m;
}

// ---- frame by frame vs tools.emu (Zebu)
{
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/telepathy_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const ref = new Map(lines.map((l) => { const v = l.split(',').map(Number); return [v[0], v]; }));
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('telepathy_ref.csv has a column the port does not write');
  const PHASES = [[2546, 2554, 0], [2555, 2718, 1], [2719, 2721, 0], [2723, 2843, 0, ['sub']]];
  const rows = new Map();
  run('magic', ['2600:RIGHT', '2630:A', '2720:A'], 2844, (f, m) => rows.set(f, compareRow(m, f)));
  let n = 0;
  for (const [lo, hi, shift, skip = []] of PHASES) {
    for (let f = lo; f <= hi; f++) {
      const w = ref.get(f), row = rows.get(f + shift);
      if (!w || !row) continue;
      n++;
      const d = refCols.filter((c, i) => i && !c.startsWith('o1F_') && !skip.includes(c) && row[idx[i]] !== w[i]);
      if (d.length) fail(`f${f}(+${shift}): ${d.map((c) => `${c} want ${w[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
    }
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} telepathy Zebu: ${n} rows vs tools.emu`);
}

// [group $07DF, msg $07DE, MP $0708, RNG $07C0] at f2700 (the tools.emu values)
// level 1: row 3 = the blessing (Zebu 19.19, Kensu 19.4); level 16: a hint from TelepathyHintLists (29.21)
const EXPECT = { zebu: [0x13, 0x13, 0x58, 0x01], kensu: [0x13, 0x04, 0x58], hint: [0x1D, 0x15, 0x58] };

/** end-state case at frame `at` */
function check(tag, name, keyArgs, at, checks) {
  const before = fails;
  const m = run(name, keyArgs, at);
  checks(m);
  console.log(`${fails > before ? 'FAIL' : 'ok  '} telepathy ${tag}`);
}
// reply message ids (TextIdUnpack'd group $07DF / msg $07DE), MP after the 8-MP cost, RNG $07C0 + 1
check('Zebu reply', 'magic', ['2600:RIGHT', '2630:A'], 2700, (m) => {
  want(m, 0x41, 0x15, 'still in the reply');
  want(m, 0x07DF, EXPECT.zebu[0], 'group'); want(m, 0x07DE, EXPECT.zebu[1], 'msg');
  want(m, 0x0708, EXPECT.zebu[2], 'MP'); want(m, 0x07C0, EXPECT.zebu[3], 'RNG');
});
check('Kensu reply', 'magic', ['2600:LEFT', '2630:A'], 2700, (m) => {
  want(m, 0x07DF, EXPECT.kensu[0], 'group'); want(m, 0x07DE, EXPECT.kensu[1], 'msg');
  want(m, 0x0708, EXPECT.kensu[2], 'MP');
});
check('hint (level 16) Kensu', 'hint', ['2600:LEFT', '2630:A'], 2700, (m) => {
  want(m, 0x07DF, EXPECT.hint[0], 'group'); want(m, 0x07DE, EXPECT.hint[1], 'msg');
  want(m, 0x0708, EXPECT.hint[2], 'MP');
});
// B in the select = 30:DE51 without a reply: no MP spent, irq_mode restored, sub_state 8
check('cancel (B)', 'magic', ['2600:B'], 2603, (m) => {
  want(m, 0x41, 0x08, 'cancel sub_state');
  want(m, 0x0708, 0x60, 'cancel MP kept');
  want(m, 0x51, 0x00, 'cancel irq_mode restored');
  want(m, 0x0200, 0xF0, 'cancel OAM 0');
});
process.exit(fails ? 1 : 0);
