// node web/test/ai70msg_check.mjs — AI $70 phase 1's table end 27:BB1A (session 173; obj/ai_final.js phase1):
// messages $1B.05-07 (PlaySound($29) + FarCall_D354 ShowMessage each), $0676 = 1, slot $16 = AI $53,
// PlaySound($25), full HP / MP (HudHpBar, HudRedraw(4) with X = 0), player obj_active 2, slot off.
// ShowMessage leaves X = 0 / Y = $0A (tools/emu/ai70_msg_probe.py): sounds 2-4 get snd_saved_y $0A.
// State (tools.emu scenario `final_locked`, area $A6, 4 frames before the table end at f2864):
//   python3 -m tools.emu.scenario_state final_locked 365 --out web/test/states/ai70msg.json
//     --save-state tools/emu/states/ai70msg.state
// Reference web/test/ai70msg_ref.csv = tools/web_state_compare.py --state tools/emu/states/ai70msg.state
// --web web/test/states/ai70msg.json --frames 320 --keys 2865:A 2895:A ... 3165:A (every 30 f) --web-key-shift 1
// --out-dir gfx/compare/ai70msg, cut by tools/csv_trim.py (core,snd,pal,obj,oam_crc) to f2861-3179.
// The port gets each A one frame later (ShowMessage reads the pad right after the vblank wait, = --web-key-shift 1).
// Every cell must match at shift 0. Exit code 1 on any mismatch.
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
const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/ai70msg.json'), 'utf8'));
const LAST = 3179;
const KEYS = new Set(Array.from({ length: 11 }, (_, i) => 2866 + 30 * i));   // emu 2865 + 30 i, shifted 1

const [head, ...lines] = readFileSync(path.join(WEB, 'test/ai70msg_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
if (idx.some((i) => i < 0)) throw new Error('ai70msg_ref.csv has a column the port does not write');

const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map();
const snd = [];                                          // snd_saved_y $010F after each change
let msgSeen = 0;
while (f0 + m.frame <= LAST) {
  const f = f0 + m.frame;
  m.runFrame(KEYS.has(f) ? 0x80 : 0);
  rows.set(f, compareRow(m, f));
  if (m.rd(0x010F) !== snd.at(-1)) snd.push(m.rd(0x010F));
  if (m.rd(0x07DF) === 0x1B) msgSeen = Math.max(msgSeen, m.rd(0x07DE));
}
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
let n = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  for (let i = 1; i < refCols.length; i++)
    if (row[idx[i]] !== w[i]) fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
}
console.log(`${fails ? 'FAIL' : 'ok  '} ai70msg: ${n} rows vs tools.emu at shift 0`);

const before = fails;
if (msgSeen !== 8) fail(`text_msg_id ended at ${hex(msgSeen)}, want 08 (messages $1B.05-07)`);
if (m.rd(0x04A0) !== 2) fail(`player obj_active = ${hex(m.rd(0x04A0))}, want 02`);
if (m.rd(0x04B5) !== 0) fail(`slot $15 obj_active = ${hex(m.rd(0x04B5))}, want 00`);
if (m.rd(0x03C1) !== m.rd(0x03C0) || m.rd(0x0708) !== m.rd(0x0709)) fail('HP / MP not at max');
if (!snd.includes(0x0A)) fail(`snd_saved_y never $0A (got ${snd.map(hex).join(' ')})`);
if (m.rd(0x51) !== 0 || m.rd(0x41) !== 8) fail(`irq_mode ${hex(m.rd(0x51))} sub_state ${hex(m.rd(0x41))}`);
console.log(`${fails > before ? 'FAIL' : 'ok  '} ai70msg: 3 messages, snd_saved_y ${snd.map(hex).join(' ')}, `
  + 'player AI 2, slot $15 off, HP / MP full');
process.exit(fails ? 1 : 0);
