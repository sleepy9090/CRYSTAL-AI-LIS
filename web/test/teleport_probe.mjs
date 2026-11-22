// node web/test/teleport_probe.mjs STATE.json LO-HI KEY.. — print teleport-menu RAM per frame (session 143 debug aid)
// columns: frame sub area $20 $21 OAM0(y tile attr x) $07FF pad_new pad_prev
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const [file, range, ...keyArgs] = process.argv.slice(2);
const [lo, hi] = range.split('-').map(Number);
const m = new Machine(data, { log: (s) => console.log('  log: ' + s) });
m.reset();
const s = JSON.parse(readFileSync(file, 'utf8'));
m.loadState(s);
const f0 = s.frame0 ?? s.frame;
const keys = parseKeys(keyArgs);
const h = (a) => m.rd(a).toString(16).padStart(2, '0');
while (f0 + m.frame <= hi) {
  const f = f0 + m.frame;
  m.runFrame(keys.get(f) || 0);
  if (f >= lo) console.log(`f${f} sub ${h(0x41)} area ${h(0x6C)} c ${h(0x20)} r ${h(0x21)} oam ${h(0x200)} ${h(0x201)} ${h(0x202)} ${h(0x203)} rot ${h(0x7FF)} new ${h(0x4B)} prev ${h(0x43)} anim1 ${h(0x4E1)} inv ${h(0x6440)} save ${h(0x7D40)} sel ${h(0x715)} 08 ${h(0x08)} fade ${h(0x11)} n ${h(0x17)} fr ${h(0x09)}`);
}
