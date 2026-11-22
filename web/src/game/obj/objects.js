// re-c web port — the per-frame object loop (Ph7 web 5/8 (a), s80). REVERSE.md "Object update + boss defeat"
// and "Per-frame order inside ObjUpdateAll" (session 30).
//
//   objUpdateAll(m)   ObjUpdateAll 30:CCCC, a generator (AI $1A's palette/IRQ sequence 30:CD8A, obj/ai_thunder.js,
//                     waits on the PPU queue and on frames). For slots 0-30, each with SwitchBank16($0D) first:
//                     knockback (obj_flags2 hi nibble: += $10; slots != 1 with obj_active b7 clear slide one
//                     step in obj_meta_dir>>4 at speed 8 and skip the AI), obj_freeze (DEC, skip while != 0;
//                     reaching 0 on spawn_b2 = 1 clears the hit flag), then JSR $0010 = JMP ObjAiTable 27:A314
//                     [obj_active & $7F] (ids $6x run with SetR7($0F)). A handler that returns true popped
//                     ObjUpdateAll's return address (AiShootCheck reached by a JMP): the loop ends there.
//   hitSetFlag(m)     HitSetFlag 26:9011: param $13 in HitFlagParams 26:9045 -> story flag HitFlagIds 26:904F[i]
//                     = $12 & bit (only the bit is touched)
//   AI_TABLE          id -> handler(m, x); ids not in it log "AI $xx not ported" once and do nothing
import { cpuRd, setR6, setR7, switchBank16 } from '../prg.js';
import { AI_HANDLERS } from './ai_static.js';
import { AI_WALK } from './ai_walk.js';
import { AI_FIELD } from './ai_field.js';
import { AI_SHOT } from './ai_shot.js';
import { AI_EFFECT } from './ai_effect.js';
import { AI_FINAL } from './ai_final.js';
import { AI_BOSS } from './ai_boss.js';
import { AI_PLAYER } from './player.js';
import { objMoveSlide } from './move.js';
import { thunderStep } from './ai_thunder.js';

const FLAGS2 = 0x0340, META_DIR = 0x0360, TIMER = 0x0480, ACTIVE = 0x04A0, SPAWN_B2 = 0x0540, SPAWN_PARAM = 0x0560;
const FREEZE = 0x05A0;

export const AI_TABLE = { ...AI_HANDLERS, ...AI_WALK, ...AI_FIELD, ...AI_SHOT, ...AI_EFFECT, ...AI_FINAL, ...AI_BOSS, ...AI_PLAYER };

export function hitSetFlag(m) {
  let y = 0;
  for (;;) {
    const p = cpuRd(m, 0x9045 + y);
    if (p === 0) return;
    if (p === m.rd(0x13)) break;
    y = (y + 1) & 0xFF;
  }
  const id = cpuRd(m, 0x904F + y), bit = cpuRd(m, 0xC000 + (id & 7));
  m.wr(0x11, bit ^ 0xFF);
  m.wr(0x10, bit & m.rd(0x12));
  const a = 0x6480 + (id >> 3);
  m.wr(a, (m.rd(a) & m.rd(0x11)) | m.rd(0x10));
}

function unported(m, what) {
  if (!m.unported.has(what)) { m.unported.add(what); m.log(`${what} not ported yet`); }
}

/** 30:CD31 knockback; returns true when the slot's AI is skipped this frame */
function knockback(m, x) {
  const f = (m.rd(FLAGS2 + x) + 0x10) & 0xFF;
  m.wr(FLAGS2 + x, f);
  if (f & 0xF0) {
    if (x === 1) return false;                              // CD42: the player handles its own knockback
    if (!(m.rd(ACTIVE + x) & 0x80)) {
      const sf = m.rd(FLAGS2 + x), sd = m.rd(META_DIR + x);
      m.wr(META_DIR + x, sd >> 4);
      m.wr(FLAGS2 + x, 8);
      setR6(m, 0x1A);
      objMoveSlide(m, x);
      m.wr(TIMER + x, m.rd(TIMER + x) - 1);
      m.wr(META_DIR + x, sd);
      m.wr(FLAGS2 + x, sf);
      return true;
    }
  }
  m.wr(META_DIR + x, m.rd(META_DIR + x) & 0x0F);            // CD74: counter wrapped (or b7 object)
  return false;
}

export function* objUpdateAll(m) {
  m.wr(0x04BF, 0);                                          // CCCE STX obj_active slot $1F (X = 0)
  let rewinds = 0;
  for (let x = 0; x < 0x1F; x++) {
    switchBank16(m, 0x0D);                                  // CCD1
    if ((m.rd(FLAGS2 + x) & 0xF0) && knockback(m, x)) continue;
    if (m.rd(FREEZE + x)) {                                 // CCDD
      m.wr(FREEZE + x, m.rd(FREEZE + x) - 1);
      if (m.rd(FREEZE + x)) continue;
      if (m.rd(SPAWN_B2 + x) === 1) {
        m.wr(0x12, 0);
        m.wr(0x13, m.rd(SPAWN_PARAM + x));
        hitSetFlag(m);
      }
    }
    const act = m.rd(ACTIVE + x), id = act & 0x7F;          // CCFA
    // s212: per-dispatch bucket for web/test/pass_count.mjs (= tools.emu.pass_cost --slots spans, CCFA to CCFA)
    m.passStage = act ? `s${x}:${id}` : 'upd';
    if (id === 0x1A) {                                      // CD03 JMP ObjUpdateAi1A 30:CD7F
      yield* thunderStep(m, x);                             // CD7F JSR 30:CD8A (waits on frames; s197)
      switchBank16(m, 0x0D);                                // CD82
      continue;
    }
    m.wr(0x10, 0x4C);
    m.wr(0x11, cpuRd(m, 0xA314 + id * 2));
    m.wr(0x12, cpuRd(m, 0xA315 + id * 2));
    if ((m.rd(0x11) | m.rd(0x12)) === 0) continue;
    if ((act & 0x70) === 0x60) setR7(m, 0x0F);
    const h = AI_TABLE[id];
    if (!h) { unported(m, `AI $${id.toString(16).toUpperCase().padStart(2, '0')}`); continue; }
    let r = h(m, x);
    if (r && typeof r.next === 'function') r = yield* r;    // s93: generator AIs (HudRedraw's queue spin)
    if (r === true) return;                                 // popped ObjUpdateAll's return address
    if (typeof r === 'number') {                            // s93: the AI left X changed (AI $04: X = $0C)
      // s165: X below the slot re-runs the slots after X in this pass (final boss setup 15:B301 returns X = 0)
      if (r < x && ++rewinds > 0x40) {                       // ROM: an AI that always does it loops forever
        unported(m, `AI at slot $${x.toString(16)} leaving X = $${r.toString(16)} (the ROM loops forever)`);
        return;
      }
      x = r;
    }
  }
}
