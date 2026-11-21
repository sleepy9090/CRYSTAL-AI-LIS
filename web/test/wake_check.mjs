// node web/test/wake_check.mjs — new game WITHOUT a held button after name entry (session 138).
// The canonical keys hold DOWN at f2340, so NewGameSkipCheck 30:CAA2 skips the wake-up cut-scene; this run
// (keys 1250/1400/1600/1700 START + 2301 START, = tools.emu's 2300 START, see REVERSE.md Verification Log
// "Title side") goes through sub_state $13 SubstWakeCutscene 30:D9B6 -> WakeCutscene 20:9400 (states/wake.js)
// and on into SubstPlay.
// Reference: web/test/wake_ref.csv = tools.emu rows f2350-3399, made with
//   python3 -m tools.emu --frames 3400 --keys 1250:START 1400:START 1600:START 1700:START 2300:START --compare emu.csv
//   python3 tools/csv_trim.py emu.csv web/test/wake_ref.csv --cols core,snd,pal,obj,oam_crc --frames 2350-3399
// Each column of port row f must equal tools.emu row f or row f-1: the cut-scene writes pal_buf / OAM right after the
// NMI, and tools.emu's row point can fall before, inside or after that work (the port finishes it in one go).
// Rows from the first SubstPlay frame on must match exactly. Exit code 1 on any mismatch.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys, compareColumns, compareRow } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const [head, ...lines] = readFileSync(path.join(WEB, 'test/wake_ref.csv'), 'utf8').trim().split('\n');
const refCols = head.split(',');
const ref = new Map(lines.map((l) => { const v = l.split(',').map(Number); return [v[0], v]; }));
const webCols = compareColumns();
const idx = refCols.map((c) => webCols.indexOf(c));
if (idx.some((i) => i < 0)) throw new Error('wake_ref.csv has a column the port does not write');

const FRAME0 = 214, LAST = Math.max(...ref.keys());
const keys = parseKeys(['1250:START', '1400:START', '1600:START', '1700:START', '2301:START']);
const logs = [];
const m = new Machine(data, { log: (s) => logs.push(s) });
m.reset();
let fails = 0, exact = 0, late = 0, seen19 = false, playFrom = null;
const fail = (msg) => { fails++; if (fails <= 30) console.log('  FAIL ' + msg); };
const subCol = refCols.indexOf('sub'), stCol = refCols.indexOf('state');
while (FRAME0 + m.frame <= LAST) {
  const f = FRAME0 + m.frame;
  m.runFrame(keys.get(f) || 0);
  const want = ref.get(f);
  if (!want) continue;
  const row = compareRow(m, f), got = idx.map((i) => row[i]);
  if (got[subCol] === 0x13) seen19 = true;
  if (seen19 && playFrom === null && got[subCol] === 8 && m.pending === null) playFrom = f + 1;
  const prev = ref.get(f - 1);
  const diff = (w) => refCols.filter((c, i) => i && got[i] !== w[i]);
  const d0 = diff(want);
  if (!d0.length) { exact++; continue; }
  if (playFrom !== null && f >= playFrom) { fail(`f${f} (SubstPlay) ${d0.map((c) => `${c} want ${want[refCols.indexOf(c)]} got ${got[refCols.indexOf(c)]}`).join(' ')}`); continue; }
  // per column: pal_buf work often lands before tools.emu's row point and the OAM clear+draw after it
  if (prev && d0.every((c) => got[refCols.indexOf(c)] === prev[refCols.indexOf(c)])) { late++; continue; }
  fail(`f${f}: ${d0.join(' ')} differ from tools.emu rows ${f} and ${f - 1}`);
}
if (!seen19) fail('sub_state $13 never reached (the cut-scene was skipped)');
if (playFrom === null) fail('the cut-scene never returned to SubstPlay');
if (m.unported.size) fail(`unported: ${[...m.unported].join(', ')}`);
if (m.rd(0x40) !== 1 || m.rd(0x41) !== 8 || ref.get(LAST)[stCol] !== 1) fail(`end state ${m.rd(0x40)}/${m.rd(0x41)}`);
console.log(`${fails ? 'FAIL' : 'ok  '} wake-up cut-scene f${Math.min(...ref.keys())}-${LAST}: ${exact} rows exact, ` +
  `${late} one row after tools.emu, SubstPlay from f${playFrom}, ${fails} failing`);
process.exit(fails ? 1 : 0);
