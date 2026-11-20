// node web/test/case_trace.mjs FILE CASE ADDR [ADDR..] (s104): replay one obj_frames boss/AI case (ai_boss.js by
// routine name, called as fn(m, x)) and print a JS stack each time the port writes one of the RAM addresses (hex).
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import * as B from '../src/game/obj/ai_boss.js';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const [file, name, ...addrs] = process.argv.slice(2);
const watch = new Set(addrs.map((a) => parseInt(a, 16)));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const m = new Machine(data);
const j = JSON.parse(readFileSync(file, 'utf8'));
const c = j.cases.find((k) => k.name === name);
const wram = Uint8Array.from(Buffer.from(j.wram_base, 'hex'));
for (const [a, v] of c.wram) wram[a - 0x6000] = v;
m.reset();
const ram = Uint8Array.from(Buffer.from(c.ram, 'hex'));
m.mem.ram.set(ram); m.mem.wram.set(wram);
m.prgR6 = ram[0x6E]; m.prgR7 = ram[0x6F];
const wr = m.wr.bind(m);
m.wr = (a, v) => {
  if (watch.has(a)) console.log(`wr $${a.toString(16)} = ${v.toString(16)}\n${new Error().stack.split('\n').slice(2, 7).join('\n')}`);
  return wr(a, v);
};
const fn = Object.entries(B).find(([k]) => k.toLowerCase() === c.routine.toLowerCase())[1];
fn(m, c.regs.x);
