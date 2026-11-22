// node web/test/outofmp_check.mjs — out of MP (session 151; states/play.js substOutOfMp): SubstOutOfMp 30:CC17.
// State (town $02, magic 2 = shot, MP 0; SpendMp 26:9AE8 has set sub_state $1C at the export):
//   python3 -m tools.emu.web_state --load-state tools/emu/states/town02_enter.state --frames 10 --poke 64DE=E0
//     --poke 64DF=FF --poke 03C1=30 --poke 0712=02 --poke 0708=00 --keys 2500:UP:30 2545:A
//     --until 30:CC17 --out web/test/states/outofmp.json --save-state tools/emu/states/outofmp.state
// Reference web/test/outofmp_ref.csv = tools/web_state_compare.py --state tools/emu/states/outofmp.state
// --web web/test/states/outofmp.json --frames 120 --keys 2600:A 2640:A, cut by tools/csv_trim.py (core,pal,obj)
// to f2546-2607 (message 32.14 "Your magic power is too low." via ShowPendingMessage, closed by A at 2600).
// Every column of every row matches at shift 0. Not compared: oam_crc at f2601-2607 (the window close) and play
// after f2607: the town trigger re-runs GiveItem one frame later in the port (the no-cycle-model gap (c), s140).
// Part 2 runs the handler once on the loaded state and checks its writes, part 3 the area $5F irq_mode clear.
// Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { substOutOfMp } from '../src/game/states/play.js';
import { compareColumns, compareRow, parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const hex = (v) => v.toString(16).padStart(2, '0');
const want = (m, a, v, tag) => { if (m.rd(a) !== v) fail(`${tag}: $${a.toString(16)} = ${hex(m.rd(a))}, want ${hex(v)}`); };
const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/outofmp.json'), 'utf8'));

function load(poke = {}) {
  const m = new Machine(data, { log: () => {} });
  m.reset();
  m.loadState(state);
  for (const [a, v] of Object.entries(poke)) m.wr(Number(a), v);
  return m;
}

// ---- frame by frame vs tools.emu
{
  const before = fails;
  const [head, ...lines] = readFileSync(path.join(WEB, 'test/outofmp_ref.csv'), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error('outofmp_ref.csv has a column the port does not write');
  const m = load();
  const f0 = state.frame0 ?? state.frame;
  const keys = parseKeys(['2600:A']);
  const rows = new Map();
  while (f0 + m.frame <= 2607) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    rows.set(f, compareRow(m, f));
  }
  if (m.unported.size) fail(`${[...m.unported].join(', ')}`);
  let n = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
    if (!row) { fail(`f${f}: no port row`); continue; }
    n++;
    const d = refCols.filter((c, i) => i && row[idx[i]] !== w[i]);
    if (d.length) fail(`f${f}: ${d.map((c) => `${c} want ${w[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} outofmp: ${n} rows vs tools.emu`);
}

// ---- the handler's writes (one pass, no wait)
function direct(tag, poke, irqWant) {
  const before = fails;
  const m = load(poke);
  const q = m.rd(0x0100);
  substOutOfMp(m, 0x5A);
  want(m, 0x41, 0x10, 'sub_state');
  want(m, 0x06C3, 0x0E, 'pending_msg_id');
  want(m, 0x06E3, 0x20, 'pending_msg_group');
  want(m, 0x0103 + Math.min(q, 5), 0x4E, 'SFX $4E queued');
  want(m, 0x010F, 0x5A, 'snd_saved_y');
  want(m, 0x51, irqWant, 'irq_mode');
  console.log(`${fails > before ? 'FAIL' : 'ok  '} outofmp ${tag}`);
}
direct('town $02 (irq_mode kept)', { 0x51: 0x02 }, 0x02);
direct('area $5F (irq_mode 0)', { 0x6C: 0x5F, 0x51: 0x02 }, 0x00);
process.exit(fails ? 1 : 0);
