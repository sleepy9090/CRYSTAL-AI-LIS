// node web/test/sprite_check.mjs [--verbose] — Ph7 web 5/8 (s54): obj/sprites.js spriteDraw() vs tools.emu.
// Every case in web/test/sprite_frames/*.json (tools/emu/sprite_capture.py, see README.md there) gives the RAM at
// the first instruction of ObjSpriteDraw 28:800E and the bytes changed when it returned. The port runs on the
// same RAM; the whole 2KB must then match (OAM shadow, obj columns, $07EF/$07FF, the temps $10-$1E), except the
// stack $0100+sp-15 .. $0100+sp+2 (JSR/PHA scratch + return address). Exit 1 on any difference.
import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { spriteDraw } from '../src/game/obj/sprites.js';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const verbose = process.argv.includes('--verbose');
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const m = new Machine(data);
const dir = path.join(WEB, 'test/sprite_frames');
const hx = (v, n = 2) => v.toString(16).toUpperCase().padStart(n, '0');
let fail = 0, total = 0;
for (const file of readdirSync(dir).filter((f) => f.endsWith('.json')).sort()) {
  const j = JSON.parse(readFileSync(path.join(dir, file), 'utf8'));
  let ok = 0, bad = 0;
  for (const c of j.cases) {
    total++;
    m.reset();
    const before = Buffer.from(c.before, 'hex');
    m.mem.ram.set(before);
    const want = Uint8Array.from(before);
    for (const [a, v] of c.after) want[a] = v;
    let err = '';
    try { spriteDraw(m); } catch (e) { err = e.message; }
    const lo = 0x100 + Math.max(0, c.sp - 15), hi = 0x100 + Math.min(0xFF, c.sp + 2), diffs = [];
    for (let a = 0; a < 0x800; a++) {
      if (a >= lo && a <= hi) continue;
      if (m.mem.ram[a] !== want[a]) diffs.push(`$${hx(a, 4)} port ${hx(m.mem.ram[a])} emu ${hx(want[a])}`);
    }
    if (err || diffs.length) {
      bad++;
      if (bad <= 5 || verbose) console.log(`FAIL ${file} ${c.name}: ${err || ''}${diffs.length} byte(s): ${diffs.slice(0, 8).join(', ')}`);
    } else ok++;
  }
  fail += bad;
  console.log(`${bad ? 'FAIL' : 'ok  '} ${file.padEnd(22)} ${ok}/${j.cases.length} cases identical  (${j.source})`);
}
console.log(`${total - fail}/${total} sprite-pass case(s) identical`);
process.exit(fail || !total ? 1 : 0);
