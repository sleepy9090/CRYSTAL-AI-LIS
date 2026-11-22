// re-c web port — Mesia reunion scene (session 150). REVERSE.md substate table row 27.
//
//   substMesiaReunion(m)  SubstMesiaReunion 19:BC6B (sub_state $1B, stored only by EventActionTable act 30
//                         EvActEnding 30:D827, which also drops its caller's return address):
//     obj_phase / obj_timer2 / orbit_level / obj1_flash = 0, slot 1 flags &= $FB, SetR6($1A), StatRecalc 30:C008
//     (X = $1B = sub_state from the dispatch), then with X = 1, $20 = 0 (step), $22 = $15 (next spawn slot):
//     MesiaScript 19:BCB0 over the 5-byte records MesiaStepTable 19:BD59 (index $20 * 5):
//       dx dy n meta ovl  -> meta/overlay of slot 1, n x (WaitFrame, slot 1 pos += signed (dx, dy) via
//                            TempAddStep 26:9861, slot 0 pos = slot 1 pos, DEC obj_anim_s01, sprite pass)
//       $80 - a meta ovl  -> obj_anim_s01 = a, meta/overlay, (WaitFrame, sprite pass, DEC anim) until anim = 8,
//                            then ObjInit(slot $22, type $C2) at slot 1's position, y - $18, flags $0D, meta $FE,
//                            $22++ (slots $15-$18)
//       $81               -> end
//     MesiaSpawnFly 19:BD91: 16 x (WaitFrame, slot $15 x+3, $16 x+1, $17 x-1, $18 x-3, sprite pass), slots
//     $15-$17 inactive, slot $18 flags 9, then 8 x (pal_buf = $30 (white), WaitFrame, PaletteBuild, WaitFrame,
//     WaitFrame, slot $18 y+1, sprite pass), slot $18 inactive. Then SetR6($1A), PaletteBuild, $0623 = $1F,
//     obj_spawn_param slot $1F = 4, sub_state 7 (GiveItem reads that slot's param), RTS.
import { R } from '../ram.js';
import { cpuRd, setR6 } from '../prg.js';
import { waitFrame, statRecalc, paletteBuildRom } from './death.js';
import { farSpriteDraw } from './formrevert.js';
import { farObjInit } from '../obj/objinit.js';
import { objPosToTemp, tempToObjPos, tempAddStep } from '../obj/util.js';

const STEP_TABLE = 0xBD59;                              // bank 19 at $A000 (SwitchBank16(9))

/** 19:BDDC: pal_buf $6140-$615F = A, WaitFrame, SetR6($1A) + PaletteBuild 26:8C0E, WaitFrame */
export function* palFlash(m, a) {
  for (let y = 0x1F; y >= 0; y--) m.wr(0x6140 + y, a);
  yield* waitFrame(m);
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);
  yield* waitFrame(m);
}

/** MesiaScript 19:BCB0 (X = 1) */
function* mesiaScript(m) {
  const x = 1;
  for (;;) {
    const y = (m.rd(0x20) * 5) & 0xFF, b = cpuRd(m, STEP_TABLE + y);
    if (b === 0x81) return;
    if (b !== 0x80) {
      m.wr(0x30, b);
      m.wr(0x31, cpuRd(m, STEP_TABLE + y + 1));
      m.wr(0x21, cpuRd(m, STEP_TABLE + y + 2));
      m.wr(0x0300 + x, cpuRd(m, STEP_TABLE + y + 3));
      m.wr(0x0580 + x, cpuRd(m, STEP_TABLE + y + 4));
      do {                                              // BCDC
        yield* waitFrame(m);
        objPosToTemp(m, x);
        tempAddStep(m);
        tempToObjPos(m, x);
        tempToObjPos(m, x - 1);                         // BCE8: X = 1 -> slot 0 follows
        m.wr(0x04E0 + x, (m.rd(0x04E0 + x) - 1) & 0xFF);
        farSpriteDraw(m);
        m.wr(0x21, (m.rd(0x21) - 1) & 0xFF);
      } while (m.rd(0x21));
      m.wr(0x20, (m.rd(0x20) + 1) & 0xFF);
      continue;
    }
    m.wr(0x04E0 + x, cpuRd(m, STEP_TABLE + y + 2));     // BD00
    m.wr(0x0300 + x, cpuRd(m, STEP_TABLE + y + 3));
    m.wr(0x0580 + x, cpuRd(m, STEP_TABLE + y + 4));
    do {                                                // BD12
      yield* waitFrame(m);
      farSpriteDraw(m);
      m.wr(0x04E0 + x, (m.rd(0x04E0 + x) - 1) & 0xFF);
    } while (m.rd(0x04E0 + x) !== 0x08);
    m.wr(0x20, (m.rd(0x20) + 1) & 0xFF);
    objPosToTemp(m, x);                                 // BD26
    m.wr(0x10, m.rd(0x22));
    m.wr(0x11, 0xC2);
    farObjInit(m);                                      // FarCall_C25D
    const s = m.rd(0x10);
    tempToObjPos(m, s);
    m.wr(0xB0 + s, (m.rd(0xB0 + s) - 0x18) & 0xFF);
    m.wr(0x0380 + s, 0x0D);
    m.wr(0x0300 + s, 0xFE);
    m.wr(0x22, (m.rd(0x22) + 1) & 0xFF);
  }
}

/** MesiaSpawnFly 19:BD91 */
function* mesiaSpawnFly(m) {
  m.wr(0x21, 0x10);
  do {                                                  // BD95
    yield* waitFrame(m);
    m.wr(0x85, (m.rd(0x85) + 3) & 0xFF);
    m.wr(0x86, (m.rd(0x86) + 1) & 0xFF);
    m.wr(0x87, (m.rd(0x87) - 1) & 0xFF);
    m.wr(0x88, (m.rd(0x88) - 3) & 0xFF);
    farSpriteDraw(m);
    m.wr(0x21, (m.rd(0x21) - 1) & 0xFF);
  } while (m.rd(0x21));
  m.wr(0x04B5, 0); m.wr(0x04B6, 0); m.wr(0x04B7, 0);    // BDAF
  m.wr(0x0398, 0x09);
  m.wr(0x21, 0x08);
  do {                                                  // BDC3
    yield* palFlash(m, 0x30);
    yield* waitFrame(m);
    m.wr(0xC8, (m.rd(0xC8) + 1) & 0xFF);                // obj_y_lo slot $18
    farSpriteDraw(m);
    m.wr(0x21, (m.rd(0x21) - 1) & 0xFF);
  } while (m.rd(0x21));
  m.wr(0x04B8, 0);
}

/** SubstMesiaReunion 19:BC6B */
export function* substMesiaReunion(m) {
  m.wr(0x0600, 0); m.wr(0x0620, 0); m.wr(0x06C0, 0); m.wr(0x0710, 0);
  m.wr(0x0381, m.rd(0x0381) & 0xFB);
  setR6(m, 0x1A);
  yield* statRecalc(m, 0x1B);                           // X = sub_state from the CAB6 dispatch
  m.wr(0x20, 0x00);
  m.wr(0x22, 0x15);
  yield* mesiaScript(m);
  yield* mesiaSpawnFly(m);
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);                                // 26:8C0E
  m.wr(0x0623, 0x1F);
  m.wr(0x057F, 0x04);
  m.wr(R.sub_state, 0x07);
}
