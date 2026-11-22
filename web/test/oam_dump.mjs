// node web/test/oam_dump.mjs STATE.json FRAME[,FRAME..] [--keys SPEC ..] — session 175, port counterpart of
// `python3 -m tools.emu.oam_dump`: the OAM shadow $0200-$02FF after each listed frame's row (sprites with Y < $F0)
// plus $07FF (OBJ_DRAW_ROT) and $08.
import { readFileSync } from 'node:fs';
import { crc32 } from 'node:zlib';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const [stateF, frameStr, ...rest] = process.argv.slice(2);
if (!frameStr) { console.log('usage: oam_dump.mjs STATE.json FRAME[,FRAME..] [--keys SPEC ..]'); process.exit(2); }
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const all = rest.includes('--all');   // s193: also the hidden (Y >= $F0) entries; the line also prints the crc
const keys = parseKeys(rest.includes('--keys') ? rest.slice(rest.indexOf('--keys') + 1).filter((a) => a !== '--all') : []);
const frames = frameStr.split(',').map(Number), last = Math.max(...frames);
const state = JSON.parse(readFileSync(stateF, 'utf8'));
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
const h = (v) => v.toString(16).toUpperCase().padStart(2, '0');
while (f0 + m.frame <= last) {
  const f = f0 + m.frame;
  m.runFrame(keys.get(f) || 0);
  if (!frames.includes(f)) continue;
  const spr = [];
  for (let i = 0; i < 64; i++) {
    const o = 0x200 + i * 4;
    if (m.rd(o) < 0xF0 || all) spr.push(`${h(i)}:${h(m.rd(o))} ${h(m.rd(o + 1))} ${h(m.rd(o + 2))} ${h(m.rd(o + 3))}`);
  }
  const crc = crc32(Uint8Array.from({ length: 256 }, (_, i) => m.rd(0x200 + i))) >>> 0;
  console.log(`f${f} crc ${crc} rot ${h(m.rd(0x07FF))} $08 ${h(m.rd(0x08))}  ${spr.join('  ')}`);
}
