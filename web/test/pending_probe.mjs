// node web/test/pending_probe.mjs STATE.json LAST [F:BTN ..] — load a port state (ENTRY_POINTS start the routine as
// the pending generator) and print the frame where m.pending is first not the entry generator (s204: when an
// ENTRY_POINTS routine such as an event act returns), plus sub_state then.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const [file, last, ...keys] = process.argv.slice(2);
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const m = new Machine(data, { log: () => {} });
m.reset();
const s = JSON.parse(readFileSync(file, 'utf8'));
m.loadState(s);
const g = m.pending, f0 = s.frame0 ?? s.frame, pad = parseKeys(keys);
if (!g) { console.log(`${file}: no ENTRY_POINTS generator for ${s.until}`); process.exit(1); }
while (f0 + m.frame <= +last) {
  const f = f0 + m.frame;
  m.runFrame(pad.get(f) ?? 0);
  if (m.pending !== g) { console.log(`entry ${s.until} returned in frame ${f}, sub_state ${m.rd(0x41)}`); process.exit(0); }
}
console.log(`entry ${s.until} still running after frame ${last}`);
