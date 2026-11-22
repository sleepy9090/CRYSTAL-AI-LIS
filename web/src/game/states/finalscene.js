// re-c web port — final scene (session 152; checked s153 by web/test/finalscene_check.mjs). REVERSE.md
// substate table row 30.
//
//   substFinalSceneReset(m)  SubstFinalSceneReset 19:BDF2 (sub_state $1E, stored only by AI $70 phase 10
//                            27:BD19 when its obj_timer2 wraps):
//     slots 4-$1E inactive; irq_mode / $2E / obj_phase / obj_timer2 / orbit_level = 0; chr_r4_play $47, sprite
//     pal ids $06 / $1C, SetR6($1A) + PaletteBuild 26:8C0E, obj1_flash = 0, StatRecalc 30:C008 (X = $1E from the
//     dispatch); $6000-$60FF = $FF, PpuQueueBlock $39-$3C (both nametables blank), attr_shadow = 0 +
//     PpuQueueAttrAll 30:C739, SetR6($0B) and $6000 <- 11:9E00 (256 bytes), PpuQueueBlock $45, WaitPpuQueue.
//     Then the walk (SetR6($1A)):
//       BE74  (DEC anim s01, WaitFrame, slot 1 meta 1 / ovl 9 (x < $80, x+1) or meta 3 / ovl $0B (x-1), slot 0 x
//             = slot 1 x, sprite pass) until x = $80
//       BEA7  meta 0 / ovl 8; (WaitFrame, DEC anim, y-1 (slot 0 + 1), sprite pass) until y = $78
//       BEC6  meta 4 / ovl 8 / anim $17; (WaitFrame, sprite pass, DEC anim) until anim = 8; meta 0, SFX $5C
//       BEEF  ObjInit(slot $0D, type $C2) at slot 0's position, y - $10, flags 9, meta $FE, anim 8, sprite
//             pass, message 0, 16 frames, anim $10, sprite pass, message 1, 16 frames
//       BF33  slot $0E at ($0068, $00C8): ObjInit(type $C2), meta $2C, flags 9; (WaitFrame, DEC anim, y-1,
//             sprite pass) until y = $78; meta $2D, sprite pass, message 2, 16 frames, messages 3 and 4
//       BF7D  slot $0E meta $2E, slot 1 meta 2 / ovl $0A; ($08-1; ($08 & $18) = 0 -> SFX $76 + MesiaPalFlash
//             19:BDDC (A = $16, 2 frames) else WaitFrame; sprite pass, DEC anim s01 / s0E, y+1 (slots 0, 1),
//             slot $0E y+1 unless slot 1 y & 7 = 0) until slot $0E y = $E0
//       BFBF  FadeOut 26:8C3D (A = $20), FinalSceneToEnding 19:BFC4: main_state = 4, RTS
//   finalMsg(m)  19:BFC9: ($07DF, $07DE) = EventMsgTable19 19:BFE8[$2E] (32.29, 27.15-27.18), SFX $29 (Y = $2E*2),
//                FarCall_D354 ShowMessage (waits for A / B; leaves sub_state 8), $2E + 1.
import { R } from '../ram.js';
import { cpuRd, setR6, farRestore } from '../prg.js';
import { playSound } from '../playsound.js';
import { waitFrame, waitFrames, statRecalc, paletteBuildRom, fadeOut } from './death.js';
import { farSpriteDraw } from './formrevert.js';
import { ppuQueueBlock, waitPpuQueue } from './pause.js';
import { showMessage } from './talk.js';
import { palFlash } from './mesia.js';
import { farObjInit } from '../obj/objinit.js';
import { objPosToTemp, tempToObjPos } from '../obj/util.js';
import { queueAttrAll } from '../world/ppuqueue.js';

const MSG_TABLE = 0xBFE8;                               // bank 19 at $A000 (SwitchBank16(9))
const dec = (m, a) => { const v = (m.rd(a) - 1) & 0xFF; m.wr(a, v); return v; };
const inc = (m, a) => { const v = (m.rd(a) + 1) & 0xFF; m.wr(a, v); return v; };

/** 19:BFC9: message $2E of EventMsgTable19, then $2E + 1 */
function* finalMsg(m) {
  const n = m.rd(0x2E), y = (n << 1) & 0xFF;
  m.wr(0x07DF, cpuRd(m, MSG_TABLE + y));
  m.wr(0x07DE, cpuRd(m, MSG_TABLE + y + 1));
  playSound(m, 0x29, y);
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);              // FarCall_D354
  yield* showMessage(m);
  farRestore(m, r6, r7);
  m.wr(0x2E, (n + 1) & 0xFF);
}

/** SubstFinalSceneReset 19:BDF2. `y` = the Y register PlaySound $5C / $76 sees (after a sprite pass). */
export function* substFinalSceneReset(m, y = 0) {
  for (let s = 4; s < 0x1F; s++) m.wr(0x04A0 + s, 0);
  m.wr(R.irq_mode, 0);
  m.wr(0x2E, 0); m.wr(0x0600, 0); m.wr(0x0620, 0); m.wr(0x06C0, 0);
  m.wr(0x07F4, 0x47);                                   // chr_r4_play
  m.wr(0x07E6, 0x06); m.wr(0x07E7, 0x1C);               // pal_id_spr2 / spr3
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);                                // 26:8C0E (keeps X)
  m.wr(0x0710, 0);                                      // obj1_flash
  yield* statRecalc(m, 0x1E);                           // X = sub_state from the CAB6 dispatch
  for (let i = 0; i < 0x100; i++) m.wr(0x6000 + i, 0xFF);
  for (let b = 0x39; b <= 0x3C; b++) yield* ppuQueueBlock(m, b);
  for (let i = 0; i < 0x40; i++) m.wr(0x6100 + i, 0);   // attr_shadow
  queueAttrAll(m);                                      // 30:C739
  setR6(m, 0x0B);
  for (let i = 0; i < 0x100; i++) m.wr(0x6000 + i, cpuRd(m, 0x9E00 + i));
  yield* ppuQueueBlock(m, 0x45);
  yield* waitPpuQueue(m);
  setR6(m, 0x1A);

  do {                                                  // BE74: walk to x = $80
    dec(m, 0x04E1);
    yield* waitFrame(m);
    let d = 1;
    m.wr(0x0301, 0x01); m.wr(0x0581, 0x09);
    if (m.rd(0x70) & 0x80) { d = 0xFF; m.wr(0x0301, 0x03); m.wr(0x0581, 0x0B); }
    const x = (m.rd(0x70) + d) & 0xFF;
    m.wr(0x70, x); m.wr(0x71, x);
    farSpriteDraw(m);
  } while (m.rd(0x70) !== 0x80);
  m.wr(0x0301, 0x00); m.wr(0x0581, 0x08);
  do {                                                  // BEB1: walk up to y = $78
    yield* waitFrame(m);
    dec(m, 0x04E1);
    m.wr(0xB1, dec(m, 0xB0));
    farSpriteDraw(m);
  } while (m.rd(0xB0) !== 0x78);
  m.wr(0x0301, 0x04); m.wr(0x0581, 0x08); m.wr(0x04E1, 0x17);
  do {                                                  // BED5
    yield* waitFrame(m);
    farSpriteDraw(m);
  } while (dec(m, 0x04E1) !== 0x08);
  m.wr(0x0301, 0x00);
  playSound(m, 0x5C, y);

  objPosToTemp(m, 0);                                   // BEEF: 26:98A8 X = 0
  m.wr(0x10, 0x0D); m.wr(0x11, 0xC2);
  farObjInit(m);                                        // FarCall_C25D
  let s = m.rd(0x10);
  tempToObjPos(m, s);                                   // 26:9897
  m.wr(0xB0 + s, (m.rd(0xB0 + s) - 0x10) & 0xFF);
  m.wr(0x038D, 0x09); m.wr(0x030D, 0xFE); m.wr(0x04ED, 0x08);
  farSpriteDraw(m);
  yield* finalMsg(m);
  yield* waitFrames(m, 0x10);
  m.wr(0x04ED, 0x10);
  farSpriteDraw(m);
  yield* finalMsg(m);
  yield* waitFrames(m, 0x10);

  m.wr(0x7E, 0x68); m.wr(0xBE, 0xC8); m.wr(0x9E, 0); m.wr(0xDE, 0);   // BF33: slot $0E position
  m.wr(0x10, 0x0E); m.wr(0x11, 0xC2);
  farObjInit(m);
  m.wr(0x030E, 0x2C); m.wr(0x038E, 0x09);
  do {                                                  // BF56
    yield* waitFrame(m);
    dec(m, 0x04EE);
    dec(m, 0xBE);
    farSpriteDraw(m);
  } while (m.rd(0xBE) !== 0x78);
  m.wr(0x030E, 0x2D);
  farSpriteDraw(m);
  yield* finalMsg(m);
  yield* waitFrames(m, 0x10);
  yield* finalMsg(m);
  yield* finalMsg(m);

  m.wr(0x030E, 0x2E); m.wr(0x0301, 0x02); m.wr(0x0581, 0x0A);
  do {                                                  // BF8C
    if ((dec(m, 0x08) & 0x18) === 0) {
      playSound(m, 0x76, y);
      yield* palFlash(m, 0x16);                         // MesiaPalFlash 19:BDDC, then JMP BFA4
    } else yield* waitFrame(m);
    farSpriteDraw(m);                                   // BFA4
    dec(m, 0x04E1); dec(m, 0x04EE);
    inc(m, 0xB0);
    if (inc(m, 0xB1) & 7) inc(m, 0xBE);
  } while (m.rd(0xBE) !== 0xE0);
  yield* fadeOut(m, 0x20);                              // 26:8C3D
  m.wr(R.main_state, 0x04);                             // FinalSceneToEnding 19:BFC4
}
