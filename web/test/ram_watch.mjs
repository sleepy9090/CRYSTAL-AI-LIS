// node web/test/ram_watch.mjs STATE.json FRAMES ADDR[,ADDR..] [--keys SPEC ..] — port counterpart of
// `python3 -m tools.emu --load-state S --frames N --watch ADDR` (session 174): logs every write to the
// watched addresses (hex) with the tools.emu frame number (state frame0 + port frame).
// A JS stack line is printed per write so the writing port routine is visible.
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const [stateF, nStr, addrStr, ...rest] = process.argv.slice(2);
if (!addrStr) { console.log('usage: ram_watch.mjs STATE.json FRAMES ADDR[,ADDR..] [--keys SPEC ..]'); process.exit(2); }
const keyArgs = rest[0] === '--keys' ? rest.slice(1) : [];
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const state = JSON.parse(readFileSync(stateF, 'utf8'));
const watch = new Set(addrStr.split(',').map((a) => parseInt(a, 16)));
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const keys = keyArgs.length ? parseKeys(keyArgs) : null;
const wr = m.mem.wr.bind(m.mem);
m.mem.wr = (a, v) => {
  if (watch.has(a)) {
    const site = (new Error().stack.split('\n')[3] || '').trim().replace(/^at /, '').replace(/\(?file:\/\/.*\/src\//, '(');
    console.log(`write $${a.toString(16).toUpperCase().padStart(4, '0')}=${v.toString(16).toUpperCase().padStart(2, '0')}`
      + `  frame=${f0 + m.frame}  ${site}`);
  }
  wr(a, v);
};
const n = Number(nStr);
for (let i = 0; i < n; i++) {
  const f = f0 + m.frame;
  m.runFrame(keys?.get(f) ?? 0);
}
console.log(`frames=${n} (last frame_no=${f0 + m.frame - 1})`);
