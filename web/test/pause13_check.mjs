// node web/test/pause13_check.mjs — SubstateHandlerTable entry 13 = PauseWait 15:BDDB (session 142, states/pause.js
// pauseWaitEntry). The entry is unreachable in the ROM (no code stores 12/13 into sub_state, and a dispatched PauseWait
// would PLA MainLoop's return address), so there is no tools.emu reference: this only checks that a forced sub_state 13
// waits for START, then leaves with sub_state 8 and the unchanged irq_mode instead of hanging the port.
// State: web/test/states/town02_enter.json (s139). Exit code 1 on failure.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const logs = [];
const m = new Machine(data, { log: (s) => logs.push(s) });
m.reset();
const s = JSON.parse(readFileSync(path.join(WEB, 'test/states/town02_enter.json'), 'utf8'));
m.loadState(s);
const f0 = s.frame0 ?? s.frame;
let fails = 0;
const fail = (msg) => { fails++; console.log('  FAIL ' + msg); };

// let the town trigger's GiveItem finish: poke only between frames with no pending generator and sub_state 8
for (let i = 0; i < 600 && !(m.pending === null && m.rd(0x41) === 8); i++) m.runFrame(0);
if (m.pending !== null || m.rd(0x41) !== 8) fail('no idle SubstPlay frame to poke from');
const irq = m.rd(0x51);
m.wr(0x41, 13);                                      // force the dispatch to entry 13
const start = f0 + m.frame + 40;
const keys = parseKeys([`${start}:START`]);
let seen8 = -1;
for (let i = 0; i < 60; i++) {
  const f = f0 + m.frame;
  m.runFrame(keys.get(f) || 0);
  const sub = m.rd(0x41);
  if (f < start && sub !== 13) { fail(`f${f}: sub_state ${sub} before START, want 13`); break; }
  if (f >= start && sub === 8 && seen8 < 0) seen8 = f;
}
if (seen8 < 0) fail('sub_state never returned to 8 after START');
if (m.rd(0x51) !== irq) fail(`irq_mode ${m.rd(0x51)}, want ${irq} (unchanged)`);
if (!logs.some((l) => l.includes('sub_state 13'))) fail('no unreachable-entry log line');
if ([...m.unported].some((x) => x === 'sub_state 13')) fail('sub_state 13 still logged as not ported');
console.log(`${fails ? 'FAIL' : 'ok  '} sub_state 13 PauseWait: waits for START (f${start}), sub_state 8 at f${seen8}`);
process.exit(fails ? 1 : 0);
