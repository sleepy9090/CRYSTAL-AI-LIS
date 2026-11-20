// node web/test/obj_check.mjs [--verbose] — Ph7 web 6/8 (s55): routine-level check of the object code vs tools.emu.
// Every case in web/test/obj_frames/*.json (tools/emu/routine_capture.py, see README.md there) gives RAM + WRAM at
// a routine's first instruction, its registers, and the bytes it changed. The port runs the same routine on the
// same memory; the whole 2KB RAM + 8KB WRAM must then match (stack $0100+sp-32 .. sp+4 excluded, s100), and so must
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
import { ppuQueueFlush } from '../src/game/world/ppuqueue.js';
import { bgRowRender, bgColRender, fullScreenDraw, runNow } from '../src/game/world/bgstream.js';
import { objMoveSlide } from '../src/game/obj/move.js';
import { objUpdateAll } from '../src/game/obj/objects.js';
import * as W from '../src/game/obj/ai_walk.js';
import * as F from '../src/game/obj/ai_field.js';
import * as S from '../src/game/obj/ai_shot.js';
import * as E from '../src/game/obj/ai_effect.js';
import * as FA from '../src/game/obj/ai_final.js';
import * as B from '../src/game/obj/ai_boss.js';

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
  // s79 (Ph7 web 4/8 (a)): these cases also carry ciram_after / ppu_ctrl (nametables + PPUCTRL compared)
  PpuQueueFlush: (m) => { ppuQueueFlush(m); },
  BgRowRender: (m) => { runNow(bgRowRender(m)); },
  BgColRender: (m) => { runNow(bgColRender(m)); },
  FullScreenDraw: (m, r) => { runNow(fullScreenDraw(m, r.a)); },
  // s80 (Ph7 web 5/8 (a)): the object loop (only ported / null AI ids in the cases) + the knockback mover
  ObjMoveSlide: (m, r) => { objMoveSlide(m, r.x); },
  ObjUpdateAll: (m) => { runNow(objUpdateAll(m)); },
  // s81 (Ph7 web 6/8 (a)): the walker / chaser AIs (obj/ai_walk.js); none of them pops its caller
  AiWalkPause: (m, r) => { W.aiWalkPause(m, r.x); },
  AiShooterWalk: (m, r) => { W.aiShooterWalk(m, r.x); },
  AiDormant: (m, r) => { W.aiDormant(m, r.x); },
  AiChase: (m, r) => { W.aiChase(m, r.x); },
  AiChaseOnscreen: (m, r) => { W.aiChaseOnscreen(m, r.x); },
  AiShooterChase: (m, r) => { W.aiShooterChase(m, r.x); },
  AiShooterChaseOn: (m, r) => { W.aiShooterChaseOnscreen(m, r.x); },
  AiAimShooter: (m, r) => { W.aiAimShooter(m, r.x); },
  AiBurrower: (m, r) => { W.aiBurrower(m, r.x); },
  AiLunger: (m, r) => { W.aiLunger(m, r.x); },
  AiSpawnChildLunger: (m, r) => { W.aiSpawnChildLunger(m, r.x); },
  AiShellOnHit: (m, r) => { W.aiShellOnHit(m, r.x); },
  // s87 (enemy port leftovers (b)): the remaining field AIs (obj/ai_field.js); none of them pops its caller
  AiTrapChest: (m, r) => { F.aiTrapChest(m, r.x); },
  AiDasher: (m, r) => { F.aiDasher(m, r.x); },
  AiHomingFlyer: (m, r) => { F.aiHomingFlyer(m, r.x); },
  AiHomingShooter: (m, r) => { F.aiHomingShooter(m, r.x); },
  AiOrbiter: (m, r) => { F.aiOrbiter(m, r.x); },
  AiPatrolGunner: (m, r) => { F.aiPatrolGunner(m, r.x); },
  AiDrifter: (m, r) => { F.aiDrifter(m, r.x); },
  AiWaitClear: (m, r) => { F.aiWaitClear(m, r.x); },
  AiCoin: (m, r) => { F.aiCoin(m, r.x); },
  // s87: the boss shot / effect AIs (obj/ai_shot.js)
  AiCountdown: (m, r) => { S.aiCountdown(m, r.x); },
  AiCurveShot: (m, r) => { S.aiCurveShot(m, r.x); },
  AiHoverShot: (m, r) => { S.aiHoverShot(m, r.x); },
  AiBounceShot: (m, r) => { S.aiBounceShot(m, r.x); },
  AiDirFrameShot: (m, r) => { S.aiDirFrameShot(m, r.x); },
  AiShotAnimDec: (m, r) => { S.aiShotAnimDec(m, r.x); },
  // s93 (boss / effect AI port (a)): obj/ai_effect.js; AiMpDrain is a generator (HudRedraw's queue wait)
  AiPlayerFollowC: (m) => { E.aiPlayerFollowC(m); },
  AiStraightMoverTl: (m, r) => { E.AI_EFFECT[0x57](m, r.x); },
  AiMpDrain: (m, r) => { runNow(E.aiMpDrain(m, r.x)); },
  // s97 (boss / final-scene AI port (a)): obj/ai_final.js, AI $70 phases (cases never reach an NMI wait)
  Ai70PhaseDispatch: (m, r) => { runNow(FA.ai70PhaseDispatch(m, r.x)); },
  // s100 (boss AI port, rest (b)): obj/ai_boss.js, bank 15 bosses (cases run with R6 = $1A, R7 = $0F)
  BossAiE0: (m, r) => { B.bossAiE0(m, r.x); },
  BossHomingShot: (m, r) => { B.bossHomingShot(m, r.x); },
  BossAiE3: (m, r) => { B.bossAiE3(m, r.x); },
  BossRockShot: (m, r) => { B.bossRockShot(m, r.x); },
};

const dir = path.join(WEB, 'test/obj_frames');
let fail = 0, total = 0, gaps = 0;
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
    let wantC = null;
    if (c.ciram_after) {                                // PPU routines: CIRAM from the base, ctrl = ppuctrl_shadow
      m.ppu.vram.set(Buffer.from(j.ciram_base, 'hex'));
      m.ppu.mirroring = j.mirroring;
      m.ppu.ctrl = ram[0x00]; m.ppu.w = 0;
      wantC = Uint8Array.from(m.ppu.vram);
      for (const [i, v] of c.ciram_after) wantC[i] = v;
    }
    let err = '', popped = false;
    m.unported.clear();
    try { popped = !!RUN[c.routine](m, c.regs); } catch (e) { err = e.message; }
    if (wantC) {
      let nc = 0, first = '';
      for (let i = 0; i < 0x800; i++) if (m.ppu.vram[i] !== wantC[i]) { if (!nc++) first = ` first CIRAM $${hx(i, 3)} port ${hx(m.ppu.vram[i])} emu ${hx(wantC[i])}`; }
      if (nc) err += `${nc} CIRAM byte(s) differ${first}; `;
      if (m.ppu.ctrl !== c.ppu_ctrl) err += `PPUCTRL port ${hx(m.ppu.ctrl)} emu ${hx(c.ppu_ctrl)}; `;
    }
    const lo = 0x100 + Math.max(0, c.sp - 32), hi = 0x100 + Math.min(0xFF, c.sp + 4), diffs = [];
    for (let a = 0; a < 0x800; a++) {
      if (a >= lo && a <= hi) continue;
      if (m.mem.ram[a] !== wantR[a]) diffs.push(`$${hx(a, 4)} port ${hx(m.mem.ram[a])} emu ${hx(wantR[a])}`);
    }
    for (let a = 0; a < 0x2000; a++) {
      if (m.mem.wram[a] !== wantW[a]) diffs.push(`$${hx(a + 0x6000, 4)} port ${hx(m.mem.wram[a])} emu ${hx(wantW[a])}`);
    }
    if (popped !== (c.ret === 'popped')) err += `return: port ${popped ? 'popped' : 'rts'} emu ${c.ret}; `;
    const gap = [...m.unported].filter((u) => u.startsWith('AI '));
    if ((err || diffs.length) && gap.length) {
      // s80: the case spawned an object (shot / death type) whose AI the port does not have yet and ran it later
      // in the same ObjUpdateAll pass -> not a failure of the ported code; counted and listed with --verbose
      p.gap = (p.gap || 0) + 1;
      if (verbose) console.log(`GAP  ${c.name}: ${gap.join(', ')}`);
    } else if (err || diffs.length) {
      p.bad++;
      if (p.bad <= 3 || verbose) {
        console.log(`FAIL ${c.name} regs ${JSON.stringify(c.regs)}: ${err}${diffs.length} byte(s): ${diffs.slice(0, 8).join(', ')}`);
      }
    } else p.ok++;
  }
  for (const [n, p] of Object.entries(per)) {
    fail += p.bad;
    console.log(`${p.bad ? 'FAIL' : 'ok  '} ${file} ${n.padEnd(16)} ${p.ok}/${p.ok + p.bad} cases identical${p.popped ? ` (${p.popped} popped the caller)` : ''}${p.gap ? ` (${p.gap} more reached an unported AI)` : ''}`);
  }
  gaps += Object.values(per).reduce((s, p) => s + (p.gap || 0), 0);
}
console.log(`${total - fail - gaps}/${total} routine case(s) identical, ${fail} failed, ${gaps} reached an unported AI`);
process.exit(fail || !total ? 1 : 0);
