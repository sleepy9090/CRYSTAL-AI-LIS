// node web/test/cure_check.mjs — magic 5 cure (session 147; states/cure.js): SubstCureMagic 19:BB39.
// State (town $02, magic 5, status 3 = poisoned, MP $60; MagicCure 27:A17D has already spent $18 at the export):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 10 --poke 64DE=E0
//     --poke 64DF=FF --poke 03C1=30 --poke 0712=05 --poke 0708=60 --poke 0710=03 --keys 2500:UP:30 2545:A
//     --until 19:BB39 --out web/test/states/cure.json --save-state tools/emu/states/cure.state
// Reference web/test/cure_ref.csv = tools/web_state_compare.py --state tools/emu/states/cure.state
// --web web/test/states/cure.json --frames 260 --keys 2720:A 2760:A, cut by tools/csv_trim.py (core,pal,obj) to
// f2546-2727 (black BG, the 63-frame cure animation, message 32.2 via ShowPendingMessage, closed by A at 2720).
// Skipped: `sub` at f2612. tools.emu's row is sampled at L255, after the vblank work that follows the NMI ending
// StatRecalc's WaitPpuQueue, so its row already shows sub_state $10; the port finishes the tail one frame later
// (the no-cycle-model gap (c)). Every other column of every row matches at shift 0. After the message the town
// trigger re-runs GiveItem and the NPCs are a step apart (the s140 lag frames), so play is not compared here.
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

function run(keyArgs, last, poke = {}, onFrame = () => {}) {
  const m = new Machine(data, { log: () => {} });
  m.reset();
  const s = JSON.parse(readFileSync(path.join(WEB, 'test/states/cure.json'), 'utf8'));
  m.loadState(s);
  for (const [a, v] of Object.entries(poke)) m.wr(Number(a), v);
  const f0 = s.frame0 ?? s.frame, meta1 = m.rd(0x0301), keys = parseKeys(keyArgs);
  while (f0 + m.frame <= last) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    onFrame(f, m);
  }
  if (m.unported.size) fail(`${[...m.unported].join(', ')}`);
  return { m, meta1 };
}

// ---- frame by frame vs tools.emu
{
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/cure_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('cure_ref.csv has a column the port does not write');
  const rows = new Map();
  run(['2720:A', '2760:A'], 2727, {}, (f, m) => rows.set(f, compareRow(m, f)));
  let n = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
    if (!row) { fail(`f${f}: no port row`); continue; }
    n++;
    const d = refCols.filter((c, i) => i && !(c === 'sub' && f === 2612) && row[idx[i]] !== w[i]);
    if (d.length) fail(`f${f}: ${d.map((c) => `${c} want ${w[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} cure: ${n} rows vs tools.emu`);
}

/** end-state case after the tail (f2613: sub_state $10 is set, the message has not started) */
function check(tag, poke, checks) {
  const before = fails;
  const { m, meta1 } = run([], 2613, poke);
  checks(m, meta1);
  console.log(`${fails > before ? 'FAIL' : 'ok  '} cure ${tag}`);
}
// values = tools.emu RAM at f2613 (web_state_compare --frames 69 --ram-diff: only the scratch / bank bytes of the
// next frame's ShowPendingMessage dispatch differ)
check('poison', {}, (m, meta1) => {
  want(m, 0x0710, 0x00, 'obj1_flash status cleared');
  want(m, 0x05A0, 0x00, 'obj_freeze');
  want(m, 0x41, 0x10, 'sub_state');
  want(m, 0x06C3, 0x02, 'pending_msg_id');
  want(m, 0x06E3, 0x20, 'pending_msg_group');
  want(m, 0x0300, 0xA7, 'slot 0 meta');
  want(m, 0x0301, meta1, 'slot 1 meta restored');
  want(m, 0x0708, 0x48, 'MP (spent before the handler)');
  want(m, 0x04E1, 0x00, 'obj_anim_s01');
});
// stone (31:F019 path) with a changed form: only the low nibble is cleared, b7 / form bits stay
check('stone + form bits kept', { 0x0710: 0xA2, 0x05A0: 0x40 }, (m) => {
  want(m, 0x0710, 0xA0, 'obj1_flash');
  want(m, 0x05A0, 0x00, 'obj_freeze');
  want(m, 0x41, 0x10, 'sub_state');
});
process.exit(fails ? 1 : 0);
