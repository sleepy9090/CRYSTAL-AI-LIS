// re-c web port — magic 7, change form (session 146). REVERSE.md substate table row 23 and the s146 note there.
//
//   substChangeForm(m)  SubstChangeForm 30:DEC1 (sub_state $17, set by MagicChangeForm 27:A161):
//     open                 orbit_level = 0, msg 33.1 "What form will you take?", TelepathyOpen 30:DF8F (window, black BG,
//                          cursor OAM 0, irq_mode 1, $21 = 0), CHR R4 alt $4D, R2 alt / R3 play $5C, WaitPpuQueue
//     loop 30:DEE4         TelepathyCursorBlink 30:DDCD, $1F = 0, ChangeFormCasterRecord 31:E038, the 4 form icons
//                          (30:DFD4 = records 30:E088, no WaitPpuQueue), OamAttrPalClear 31:E077, WaitFrame,
//                          ReadPadsMenu, PadNewButtonIndex -> JmpTable30_DF0E: A = ChangeFormApply, B/SEL/START = exit
//                          30:DE51, U/D ignored, L/R = $21 -/+ 1 (& 3, cursor x 30:DDED) + SFX $4B
//   ChangeFormApply 30:DF2F  SFX $39, OAM 0 hidden, 31 frames with R4 play $4D: the caster shows the form preview
//                          (meta $21*4 + $2E, tile ofs $D0, anim = $07FF counting $1F -> 1); then the plain caster,
//                          32 frames, OamClipTail($F0), WaitFrame, R4 play restored, obj1_flash |= $21 << 4 | $80,
//                          SpendMp($14), JMP 30:DE51 (the exit pulls the irq_mode pushed at 30:DEC3)
import { R } from '../ram.js';
import { setR6 } from '../prg.js';
import { playSound } from '../playsound.js';
import { readPadsMenu } from '../pads.js';
import { waitFrame, oamClip } from './death.js';
import { waitPpuQueue, objSlot1FDraw } from './pause.js';
import { padNewButtonIndex } from './teleport.js';
import { cpuRd } from '../prg.js';
import { hideOam0, selectOpen, cursorX, selectExit } from './telepathy.js';
import { spendMp } from '../obj/player.js';

const OAM = 0x0200, ROT = 0x07FF, R4_PLAY = 0x07F4, FLASH = 0x0710;

/** ChangeFormCasterRecord 31:E038: record 0 at $6000 = the player at (x $84, y $A0), meta $02, tile ofs $4C,
 *  flags $0B, anim 0; while $1F != 0 the form preview: anim = $07FF, meta = $21*4 + $2E, tile ofs $D0 */
function casterRecord(m) {
  [0x84, 0xA0, 0x02, 0x4C, 0x0B, 0x00, 0x00, 0x00].forEach((v, i) => m.wr(0x6000 + i, v));
  if (!m.rd(0x1F)) return;
  m.wr(0x6007, m.rd(ROT));
  m.wr(0x6002, ((m.rd(0x21) << 2) + 0x2E) & 0xFF);
  m.wr(0x6003, 0xD0);
}

/** 30:DFD4: the 4 icons = records 30:E088 at $6008-$6027 (y = $4C - (cam_y & 7)), $20 = 4, ObjSlot1FDraw */
function drawIcons(m) {
  for (let x = 0x1F; x >= 0; x--) m.wr(0x6008 + x, cpuRd(m, 0xE088 + x));
  m.wr(0x10, m.rd(R.cam_y_lo) & 7);
  const y = (0x4C - m.rd(0x10)) & 0xFF;
  for (const a of [0x6009, 0x6011, 0x6019, 0x6021]) m.wr(a, y);
  m.wr(0x20, 4);
  objSlot1FDraw(m);
}

/** OamAttrPalClear 31:E077: every OAM attribute byte &= $FC (sprite palette 0) */
function oamAttrPalClear(m) {
  for (let x = 0; x < 0x100; x += 4) m.wr(OAM + 2 + x, m.rd(OAM + 2 + x) & 0xFC);
}

/** SubstChangeForm 30:DEC1 */
export function* substChangeForm(m) {
  const savedIrq = m.rd(R.irq_mode);                   // PHA, pulled at 30:DE6A
  m.wr(0x06C0, 0);                                      // orbit_level
  m.wr(0x07DE, 0x01);                                   // msg 33.1
  m.wr(0x07DF, 0x21);
  yield* selectOpen(m);                                 // TelepathyOpen 30:DF8F
  m.wr(0x5C, 0x4D);                                     // chr_r4_alt
  m.wr(0x5A, 0x5C);                                     // chr_r2_alt
  m.wr(0x07F3, 0x5C);                                   // chr_r3_play
  yield* waitPpuQueue(m);
  for (;;) {                                            // DEE4
    const b = (m.rd(ROT) - 1) & 0xFF;                   // TelepathyCursorBlink 30:DDCD
    m.wr(ROT, b);
    m.wr(OAM + 1, (b & 0x08) ? 0x3F : 0x3D);
    m.wr(0x1F, 0);
    casterRecord(m);                                    // 31:E038
    drawIcons(m);                                       // 30:DFD4
    oamAttrPalClear(m);                                 // 31:E077
    yield* waitFrame(m);
    readPadsMenu(m);
    const k = padNewButtonIndex(m);
    if (k < 0 || k === 4 || k === 5) continue;          // BCC / U/D -> DEE4
    if (k === 0) { yield* changeFormApply(m); break; }
    if (k <= 3) break;                                  // B / SELECT / START -> 30:DE51
    m.wr(0x21, (m.rd(0x21) + (k === 7 ? 1 : -1)) & 0xFF);   // DF1E INC INC DEC / DF22 DEC
    cursorX(m);                                         // 30:DDED
    playSound(m, 0x4B, k * 2);                          // Y = the jump-table index (TAY at 30:DF00)
  }
  yield* selectExit(m, savedIrq);                       // 30:DE51
}

/** ChangeFormApply 30:DF2F (Y = 0 from the A-key index) */
function* changeFormApply(m) {
  playSound(m, 0x39, 0);
  hideOam0(m);
  m.wr(ROT, 0x1F);
  m.wr(0x1F, 0x1F);
  const r4 = m.rd(R4_PLAY);
  m.wr(R4_PLAY, 0x4D);
  do {                                                  // DF47
    yield* waitFrame(m);
    casterRecord(m);
    drawIcons(m);
    oamAttrPalClear(m);
    m.wr(ROT, (m.rd(ROT) - 1) & 0xFF);
  } while (m.rd(ROT));
  m.wr(0x6003, 0x4C);
  drawIcons(m);
  oamAttrPalClear(m);
  for (let y = 0x20; y; y--) yield* waitFrame(m);      // 30:C176 (Y = $20)
  oamClip(m, 0xF0);                                     // OamClipTail
  yield* waitFrame(m);
  m.wr(R4_PLAY, r4);
  m.wr(FLASH, ((m.rd(0x21) << 4) | m.rd(FLASH) | 0x80) & 0xFF);
  setR6(m, 0x1A);
  // MagicChangeForm 27:A161 already checked MP >= $14; a short MP here would PLA PLA into the PHA'd irq_mode
  if (!(yield* spendMp(m, 0x14, 0))) m.log?.('ChangeFormApply: SpendMp short (stack would break in the ROM)');
}
