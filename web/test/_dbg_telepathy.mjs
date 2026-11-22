// temporary debug: port run from telepathy_magic.json, print slot 3 / trigger state around the exit
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const m = new Machine(data, { log: (s) => console.log('LOG', s) });
m.reset();
const s = JSON.parse(readFileSync(path.join(WEB, 'test/states/telepathy_magic.json'), 'utf8'));
m.loadState(s);
const f0 = s.frame0 ?? s.frame;
const keys = parseKeys(process.argv.slice(3).length ? process.argv.slice(3) : ['2600:RIGHT', '2630:A', '2720:A']);
const last = Number(process.argv[2] || 2726);
const h = (v) => v.toString(16).padStart(2, '0');
while (f0 + m.frame <= last) {
  const f = f0 + m.frame;
  m.runFrame(keys.get(f) || 0);
  if (f < last - 8 && f !== 2545) continue;
  const act = [];
  for (let i = 0; i < 0x20; i++) if (m.rd(0x04A0 + i)) act.push(`${h(i)}:${h(m.rd(0x04A0 + i))}@${h(m.rd(0x0380 + i))}${h(m.rd(0x0400 + i) || 0)}`);
  console.log(f, 'sub', h(m.rd(0x41)), '$08', h(m.rd(8)), 'pad', h(m.rd(0x4B)), h(m.rd(0x43)), h(m.rd(0x49)), 'slots', act.join(' '));
}
