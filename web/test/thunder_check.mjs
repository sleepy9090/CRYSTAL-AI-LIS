// node web/test/thunder_check.mjs — AI $1A (ObjUpdateAi1A 30:CD7F -> 30:CD8A), the sword-4 level-3 charged shot,
// vs tools.emu (session 197). Scenario thunder_B2 = group_B2 (area $B2, level 16, sword 4, MP $C0) + ball 8, so
// SwordPowerLevel gives sword_power 2 and SwordChargeCap lets orbit_level reach $18. B held 120 frames, released
// at f2632: shot id $0F (type $1F, slot 4, obj_active $1A), SpendMp($28) (MP $C0 -> $98). 106 steps (table 30:CF47),
// one per ObjUpdateAll pass: a type-$1B bolt on slot $0D + (phase & $0F) each step, nibble-tile PPU blocks
// $14-$1F with a white flash on even $08 passes, irq_mode 2 / CHR alt $34 palette ramps from banks 0/1,
// PlaySound($43) three times (Y = 3 * phase = $24 / $7E / $DE), slot 4 cleared at the end (f2893).
// State: python3 -m tools.emu.scenario_state thunder_B2 12 --out web/test/states/thunder.json
//          --save-state tools/emu/states/thunder.state
// Ref:   tools/web_state_compare.py --state tools/emu/states/thunder.state --web web/test/states/thunder.json
//          --frames 440 --cols core,snd,pal,obj,oam_crc --out-dir gfx/thunder --keys 2512:B:120 --web-key-shift 1,
//          cut by tools/csv_trim.py to f2508-2945.
// Accepted cells (tools/cell_class.py), only inside the AI $1A window f2633-2893:
// - LATE: the port's NEXT row has tools.emu's value. After a SPIN wait (PpuQueueBlock / WaitPpuQueue) the NMI
//   is short and tools.emu's pass goes on in vblank (writes at L251/L252) before its L261 row sample, so the
//   start of the next step (bolt spawn, obj_phase, pal_buf) is already in its row f; the port's row is the
//   pass end. Count fixed below.
// - PARTIAL pal_crc: tools.emu's row is sampled mid-PaletteBuild / mid-palette copy: its $6140-$615F = the
//   port's row f+1 for the bytes written so far and row f for the rest (sequential, or the type-0 copy's
//   interleaved $6140+i / $6150+i order); checked by rebuilding that mix.
// Exit code 1 on any other difference.
import { readFileSync } from 'node:fs';
import { crc32 } from 'node:zlib';
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
const LAST = 2945, WIN = [2633, 2893];
const KEYS = parseKeys(['2513:B:120']);
const LATE_N = 240, PARTIAL_N = 29;
const SND_Y = [0x24, 0x7E, 0xDE];

const state = JSON.parse(readFileSync(path.join(WEB, 'test/states/thunder.json'), 'utf8'));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/thunder_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const webCols = compareColumns(), idx = refCols.map((x) => webCols.indexOf(x));
if (idx.some((i) => i < 0)) throw new Error('thunder_ref.csv has a column the port does not write');
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const rows = new Map(), pal = new Map(), sndY = [];
let start = 0, end = 0, maxPhase = 0;
const wr = m.mem.wr.bind(m.mem);
m.mem.wr = (a, v) => {                                   // PlaySound($43): its Y is in $010F when the id is queued
  if (a >= 0x0103 && a <= 0x0107 && v === 0x43) sndY.push(m.rd(0x010F));
  wr(a, v);
};
while (f0 + m.frame <= LAST + 1) {
  const f = f0 + m.frame;
  const before = m.rd(0x04A4);
  m.runFrame(KEYS.get(f) || 0);
  rows.set(f, compareRow(m, f));
  pal.set(f, Uint8Array.from({ length: 32 }, (_, i) => m.rd(0x6140 + i)));
  if (before !== 0x1A && m.rd(0x04A4) === 0x1A) start = f;
  if (before === 0x1A && m.rd(0x04A4) !== 0x1A) end = f;
  if (m.rd(0x04A4) === 0x1A) maxPhase = Math.max(maxPhase, m.rd(0x0604));
}
const hx = (v) => '$' + v.toString(16);
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (start !== 2633 || end !== 2893) fail(`AI $1A in slot 4 f${start}-f${end}, want f2633-f2893`);
if (maxPhase !== 0x69) fail(`last obj_phase ${hx(maxPhase)}, want $69`);
if (m.rd(0x0604) !== 0x6A) fail(`obj_phase at the end ${hx(m.rd(0x0604))}, want $6A`);
if (sndY.join() !== SND_Y.join()) fail(`PlaySound($43) Y ${sndY.map(hx)}, want ${SND_Y.map(hx)}`);
const midWrite = (f, want) => {
  const a = pal.get(f), b = pal.get(f + 1);
  if (!a || !b) return false;
  for (let k = 1; k < 32; k++) {
    const seq = Uint8Array.from(a); seq.set(b.subarray(0, k));
    if ((crc32(seq) >>> 0) === want) return true;
    const il = Uint8Array.from(a);                       // type-0 copy: $6140+i then $6150+i
    for (let j = 0; j < k; j++) il[(j >> 1) + (j & 1) * 16] = b[(j >> 1) + (j & 1) * 16];
    if ((crc32(il) >>> 0) === want) return true;
  }
  return false;
};
let n = 0, late = 0, partial = 0;
for (const l of lines) {
  const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
  if (!row) { fail(`f${f}: no port row`); continue; }
  n++;
  const inWin = f >= WIN[0] && f <= WIN[1];
  for (let i = 1; i < refCols.length; i++) {
    if (row[idx[i]] === w[i]) continue;
    if (inWin && rows.get(f + 1)?.[idx[i]] === w[i]) { late++; continue; }
    if (inWin && refCols[i] === 'pal_crc' && midWrite(f, w[i])) { partial++; continue; }
    fail(`f${f}: ${refCols[i]} want ${w[i]} got ${row[idx[i]]}`);
  }
}
if (late !== LATE_N) fail(`${late} LATE cells, want ${LATE_N}`);
if (partial !== PARTIAL_N) fail(`${partial} PARTIAL pal cells, want ${PARTIAL_N}`);
console.log(`${fails ? 'FAIL' : 'ok  '} thunder: ${n} rows vs tools.emu at shift 0 (snd incl., ${late} LATE + ${partial} PARTIAL pal cells in f${WIN[0]}-${WIN[1]}), AI $1A slot 4 f${start}-f${end}, PlaySound($43) Y ${sndY.map(hx).join(' ')}`);
process.exit(fails ? 1 : 0);
