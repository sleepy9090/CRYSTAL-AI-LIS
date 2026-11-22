// node web/test/pass_count.mjs STATE.json LAST [--out FILE] [keys..] — s210 (gap (c) lag model study): per frame of a
// load-state run, the number of Machine rd / wr calls made by the port (main-loop pass + NMI + IRQ work), as
// `frame rd wr` lines (tools.emu frame numbers). Compare with `python3 -m tools.emu.pass_cost` totals
// (tools/pass_fit.py) to see how well the port's RAM-access count predicts a pass's CPU cycles.
import { readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const argv = process.argv.slice(2);
const oi = argv.indexOf('--out'), outFile = oi >= 0 ? argv.splice(oi, 2)[1] : null;
const [stateFile, last, ...keyArgs] = argv;
if (!last) { console.log('usage: node web/test/pass_count.mjs STATE.json LAST [keys..]'); process.exit(1); }
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const state = JSON.parse(readFileSync(stateFile, 'utf8'));
const keys = parseKeys(keyArgs);
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
let rd = 0, wr = 0;
const r0 = m.rd.bind(m), w0 = m.wr.bind(m);
m.rd = (a) => { rd++; return r0(a); };
m.wr = (a, v) => { wr++; w0(a, v); };
const f0 = state.frame0 ?? state.frame;
const out = [];
while (f0 + m.frame <= Number(last)) {
  const f = f0 + m.frame;
  rd = 0; wr = 0;
  m.runFrame(keys.get(f) || 0);
  out.push(`${f} ${rd} ${wr}`);
}
if (outFile) writeFileSync(outFile, out.join('\n') + '\n');
else console.log(out.join('\n'));
