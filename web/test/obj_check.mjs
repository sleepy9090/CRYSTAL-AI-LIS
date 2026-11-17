// node web/test/obj_check.mjs [--verbose] — Ph7 web 6/8 (s55): routine-level check of the object code vs tools.emu.
// Every case in web/test/obj_frames/*.json (tools/emu/routine_capture.py, see README.md there) gives RAM + WRAM at
// a routine's first instruction, its registers, and the bytes it changed. The port runs the same routine on the
// same memory; the whole 2KB RAM + 8KB WRAM must then match (stack $0100+sp-15 .. sp+4 excluded), and so must
// the way it returned ("popped" = AiShootCheck pulled the caller's return address). Exit 1 on any difference.
import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { objInit } from '../src/game/obj/objinit.js';
import { playSound } from '../src/game/playsound.js';
import { dirToTarget, spawnShot, objBgProbe } from '../src/game/obj/util.js';
import { aiStraightMover, aiDirShot, aiBurst, aiFanTurret, aiTurret } from '../src/game/obj/ai_static.js';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const verbose = process.argv.includes('--verbose');
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const m = new Machine(data);
const hx = (v, n = 2) => v.toString(16).toUpperCase().padStart(n, '0');

// routine name -> (m, regs) => true if the routine popped its caller
const RUN = {
  ObjInit: (m) => { objInit(m); },
  PlaySound: (m, r) => { playSound(m, r.a, r.y); },
  DirToTarget: (m, r) => { dirToTarget(m, r.x, r.y); },
  SpawnShot: (m, r) => { spawnShot(m, r.x, r.a, r.y); },
  ObjBgProbe: (m, r) => { objBgProbe(m, r.x, r.a); },
  AiStraightMover: (m, r) => { aiStraightMover(m, r.x); },
  AiDirShot: (m, r) => { aiDirShot(m, r.x); },
  AiBurst: (m, r) => { aiBurst(m, r.x); },
  AiFanTurret: (m, r) => aiFanTurret(m, r.x),
  AiTurret: (m, r) => { aiTurret(m, r.x); },
};

const dir = path.join(WEB, 'test/obj_frames');
let fail = 0, total = 0;
for (const file of readdirSync(dir).filter((f) => f.endsWith('.json')).sort()) {
  const j = JSON.parse(readFileSync(path.join(dir, file), 'utf8'));
  const wbase = Buffer.from(j.wram_base, 'hex');
  const per = {};
  for (const c of j.cases) {
    total++;
    const p = (per[c.routine] ||= { ok: 0, bad: 0, popped: 0 });
    if (c.ret === 'popped') p.popped++;
    m.reset();
    const ram = Uint8Array.from(Buffer.from(c.ram, 'hex')), wram = Uint8Array.from(wbase);
    for (const [a, v] of c.wram) wram[a - 0x6000] = v;
    m.mem.ram.set(ram); m.mem.wram.set(wram);
    m.prgR6 = ram[0x6E]; m.prgR7 = ram[0x6F];        // capture sets them = the mapping nes.call used (bank calls)
    const wantR = Uint8Array.from(ram), wantW = Uint8Array.from(wram);
    for (const [a, v] of c.after) { if (a < 0x800) wantR[a] = v; else wantW[a - 0x6000] = v; }
    let err = '', popped = false;
    try { popped = !!RUN[c.routine](m, c.regs); } catch (e) { err = e.message; }
    const lo = 0x100 + Math.max(0, c.sp - 15), hi = 0x100 + Math.min(0xFF, c.sp + 4), diffs = [];
    for (let a = 0; a < 0x800; a++) {
      if (a >= lo && a <= hi) continue;
      if (m.mem.ram[a] !== wantR[a]) diffs.push(`$${hx(a, 4)} port ${hx(m.mem.ram[a])} emu ${hx(wantR[a])}`);
    }
    for (let a = 0; a < 0x2000; a++) {
      if (m.mem.wram[a] !== wantW[a]) diffs.push(`$${hx(a + 0x6000, 4)} port ${hx(m.mem.wram[a])} emu ${hx(wantW[a])}`);
    }
    if (popped !== (c.ret === 'popped')) err += `return: port ${popped ? 'popped' : 'rts'} emu ${c.ret}; `;
    if (err || diffs.length) {
      p.bad++;
      if (p.bad <= 3 || verbose) {
        console.log(`FAIL ${c.name} regs ${JSON.stringify(c.regs)}: ${err}${diffs.length} byte(s): ${diffs.slice(0, 8).join(', ')}`);
      }
    } else p.ok++;
  }
  for (const [n, p] of Object.entries(per)) {
    fail += p.bad;
    console.log(`${p.bad ? 'FAIL' : 'ok  '} ${file} ${n.padEnd(16)} ${p.ok}/${p.ok + p.bad} cases identical${p.popped ? ` (${p.popped} popped the caller)` : ''}`);
  }
}
console.log(`${total - fail}/${total} routine case(s) identical`);
process.exit(fail || !total ? 1 : 0);
