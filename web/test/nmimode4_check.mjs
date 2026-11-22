// node web/test/nmimode4_check.mjs — irq_mode 4 (session 208; mainloop.js nmiMode4 / irqRowStretch / irqOffSound):
// NmiMode4 31:F782 + IRQ handler 4 IrqRowStretch 31:F5E1 + handler 5 IrqOffSound 31:F60E. Dead in the ROM (only the
// RTS-stubbed shop-door routines 31:FE01/FE18 set irq_mode 4), so the states force it at the main-loop top:
//   python3 -m tools.emu.web_state --load-state tools/emu/states/gameplay_first.state --frames 1
//     --force 30:C900:0051=04 --force 30:C900:0058=NN --until 30:C900
//     --out web/test/states/nmimode4_NN.json --save-state tools/emu/states/nmimode4_NN.state      (NN = 08, 01, 00)
// Refs web/test/nmimode4_NN_ref.csv = tools/web_state_compare.py --state .. --web .. --frames 240 --keys 2353:DOWN:30
// --web-key-shift 1 (tools.emu's restored pad latch is one frame ahead of the port's keys; the same in a run without
// the force), cut by tools/csv_trim.py (core,pal,obj,snd,oam_crc) to f2354-2592. Every cell matches at shift 0.
// SCREENS = CRC32 of the tools.emu shot's RGB (`python3 tools/png_crc.py emu_FFFF.png`). SPLITS = the lines where
// the port's row changes show; tools/emu/irq_split_probe.py puts the 2nd PPUADDR write one line earlier
// ($58 = 8: lines 4, 14 .. 234 ($59 = $17: 24 IRQs), IRQOFF + SoundUpdate at 235; $58 = 1: 4, 7 .. 88 ($1C: 29), at 89).
// Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import { crc32 } from 'node:zlib';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { compareColumns, compareRow, parseKeys, fbToRgb } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
let fails = 0;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const range = (a, b, s) => { const o = []; for (let i = a; i <= b; i += s) o.push(i); return o; };

const RUNS = [
  { nn: '08', screens: { 2356: 0xad374283, 2450: 0x9830c920 }, splits: range(5, 235, 10) },
  { nn: '01', screens: { 2356: 0x10040fd9, 2450: 0x1c01bde9 }, splits: range(5, 89, 3) },
  { nn: '00', screens: { 2356: 0xb77d18ab, 2450: 0x85ea3b4e }, splits: [] },
];
const LAST = 2592;

for (const { nn, screens, splits } of RUNS) {
  const before = fails;
  const state = JSON.parse(readFileSync(path.join(WEB, `test/states/nmimode4_${nn}.json`), 'utf8'));
  const [head, ...lines] = readFileSync(path.join(WEB, `test/nmimode4_${nn}_ref.csv`), 'utf8').trim().split('\n');
  const refCols = head.split(',');
  const webCols = compareColumns(), idx = refCols.map((c) => webCols.indexOf(c));
  if (idx.some((i) => i < 0)) throw new Error(`nmimode4_${nn}_ref.csv has a column the port does not write`);
  const m = new Machine(data, { log: () => {} });
  m.reset();
  m.loadState(state);
  const f0 = state.frame0 ?? state.frame;
  const keys = parseKeys(['2354:DOWN:30']);
  const rows = new Map();
  let splitsSeen = null, h5 = 0;
  const snd = m.soundUpdate;
  m.soundUpdate = (mm) => { if (mm.rd(0x56) === 4 && mm.ppu.splits.size) h5++; return snd(mm); };
  while (f0 + m.frame <= LAST) {
    const f = f0 + m.frame;
    m.runFrame(keys.get(f) || 0);
    rows.set(f, compareRow(m, f));
    if (f === 2400) splitsSeen = [...m.ppu.splits.keys()].sort((a, b) => a - b);
    if (screens[f] !== undefined) {
      const got = crc32(fbToRgb(m.ppu.fb, data.palettes.nesRgb).rgb) >>> 0;
      if (got !== screens[f]) fail(`$58=${nn} f${f}: screen crc ${got.toString(16)}, want ${screens[f].toString(16)}`);
    }
  }
  m.soundUpdate = snd;
  if (m.unported.size) fail(`$58=${nn}: ${[...m.unported].join(', ')}`);
  if (m.rd(0x51) !== 4) fail(`$58=${nn}: irq_mode ${m.rd(0x51)} at the end, want 4`);
  if (JSON.stringify(splitsSeen) !== JSON.stringify(splits)) fail(`$58=${nn}: split lines ${splitsSeen}, want ${splits}`);
  const wantVec = nn === '00' ? null : [0xE1, 0xF5];   // irq_vector $54/$55 = IrqHandlerTable[4] = F5E1
  if (wantVec && (m.rd(0x54) !== wantVec[0] || m.rd(0x55) !== wantVec[1])) fail(`$58=${nn}: irq_vector $54/$55`);
  if (m.rd(0x52) > 3 || m.rd(0x53) !== 0xF7) fail(`$58=${nn}: $52/$53 = ${m.rd(0x52)}/${m.rd(0x53)}`);
  // handler 5's SoundUpdate (from F5FE) once per frame after the last row; none with $58 = 0 (IRQ off)
  if (h5 !== (nn === '00' ? 0 : lines.length)) fail(`$58=${nn}: IrqOffSound ran ${h5} times`);
  let n = 0;
  for (const l of lines) {
    const w = l.split(',').map(Number), f = w[0], row = rows.get(f);
    if (!row) { fail(`$58=${nn} f${f}: no port row`); continue; }
    n++;
    const d = refCols.filter((c, i) => i && row[idx[i]] !== w[i]);
    if (d.length) fail(`$58=${nn} f${f}: ${d.map((c) => `${c} want ${w[refCols.indexOf(c)]} got ${row[webCols.indexOf(c)]}`).join(' ')}`);
  }
  console.log(`${fails > before ? 'FAIL' : 'ok  '} nmimode4 $58=${nn}: ${n} rows vs tools.emu, ${Object.keys(screens).length} screens, ${splitsSeen.length} splits, H5 ${h5}`);
}
process.exit(fails ? 1 : 0);
