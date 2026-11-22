import { readFileSync } from 'node:fs';
const W = '/home/sleepy9090/Downloads/nes-port/RE-C-NES-ROM/web/';
const { loadData } = await import(W + 'src/data.js');
const { Machine } = await import(W + 'src/game/machine.js');
const data = await loadData((n) => readFileSync(W + 'data/' + n));
const m = new Machine(data, { log: (s) => console.log('LOG', s) });
m.reset();
const s = JSON.parse(readFileSync(W + 'test/states/formrevert.json', 'utf8'));
m.loadState(s);
const f0 = s.frame0 ?? s.frame;
const h = (v) => v.toString(16).padStart(2, '0');
const A = [0x41, 0x07F2, 0x07F3, 0x0581, 0x0301, 0x0710, 0x0714, 0x04E1];
console.log('load', A.map((a) => h(m.rd(a))).join(' '));
while (f0 + m.frame <= 2797) {
  const f = f0 + m.frame;
  m.runFrame(0);
  console.log(f, A.map((a) => h(m.rd(a))).join(' '));
}
