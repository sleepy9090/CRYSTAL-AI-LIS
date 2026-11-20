// re-c web port — the lasting effects of the cold-boot path between Reset 31:F2A4 and the first MainLoop pass
// (session 106).
//
//   introLeftovers(m)   the RAM / PPU state the bank-18 cold-boot intro (18:8028, f0-213, not ported) leaves
//                       behind at the first 30:C900 (tools.emu, power-on, no input; see REVERSE.md "Boot")
//   coldSaveInit(m)     ColdSaveInit 31:F1CD (cold path only: $0182-$0184 != "USO")
//   bootSaveCheck(m)    BootSaveCheck 31:F0A4 + the slot checks / resets / repair copies behind it
//   copySramBlock(m)    CopySramBlock 31:F3A2 ($7D00 -> $6480, $7E00 -> $6580: the one-page-off boot copy)
//
// Order in Reset: (intro) -> ColdSaveInit -> BootSaveCheck -> CopySramBlock -> JMP MainLoop. ColdSaveInit runs
// before the intro, but the intro does not touch WRAM $6480+ / $6D00+ / $7000+, so the port can run all three
// after introLeftovers.

import { setR7 } from './prg.js';

const TEMPLATE = 0xBE00;          // new-save template page, bank 23 at $A000 ($17)
const TAG = [0x41, 0x4D, 0x49];   // "AMI" at page + $FD

const T = (m, x) => m.data.prg[0x17 * 0x2000 + ((TEMPLATE + x) & 0x1FFF)];

/** Intro end state (tools.emu RAM at the first 30:C900, frame 214 line 222). The ROM has frame_ready $09 = 1
 *  there, so MainState2 starts at line 222 of f214 — but it runs ~3000 cycles and the f214 NMI (line 241)
 *  interrupts it: the f214 row still shows main_state 2 / title_ctr $F0, and the pass ends early in f215.
 *  The port runs a state in one block, so it keeps $09 = 0: frame 214 runs no state, its NMI does the OAM DMA +
 *  scroll copy (the ROM skips them with $09 != 0, but OAM is all hidden and $02-$05 are 0 either way) and sets
 *  $09 = 1, and MainState2 runs whole in f215. irq_mode 5 stays until MainState2's IRQ write, so the f214 NMI
 *  is NmiMode5 (IRQ handler 6 at latch $58 = $89) in both. */
const INTRO_RAM = {
  0x01: 0x1E,                              // ppumask_shadow (BG + sprites on)
  0x49: 0xFF, 0x4A: 0xFF,
  0x51: 0x05,                              // irq_mode 5 (NmiMode5 31:F7C5)
  0x53: 0xF7, 0x54: 0x91, 0x55: 0xF6,      // irq_vector = IrqHandler6 31:F691 (the port uses m.irqSplits)
  0x56: 0x06,                              // SetIrqHandler(6)
  0x58: 0x89, 0x59: 0x28,                  // chr_r0_alt $58-$5A: IRQ6 latch $89 (line 137), PPUADDR $2800
  0x8C: 0x2A, 0x8D: 0x22, 0x8E: 0x28, 0x8F: 0x80,
  0x06FF: 0xF0,                            // title_ctr (cleared by MainState2's second pass)
  0x07F0: 0x28, 0x07F1: 0x0E,              // chr_r0_play: R0 $28, R1 $0E, R2-R5 0
};
// $06CF-$06EF: the intro's last fade target (pal_target $06D0-$06EF is cleared by MainState2's second pass)
const INTRO_06CF = [0x20, 0x20, 0x0F, 0x1C, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F,
  0x0F, 0x20, 0x0F, 0x0F, 0x0F, 0x20, 0x0F, 0x0F, 0x0F, 0x20, 0x0F, 0x0F, 0x0F, 0x20, 0x0F, 0x0F, 0x0F];

export function introLeftovers(m) {
  for (const [a, v] of Object.entries(INTRO_RAM)) m.wr(+a, v);
  INTRO_06CF.forEach((v, i) => m.wr(0x06CF + i, v));
  for (let i = 0; i < 256; i += 4) m.wr(0x0200 + i, 0xF0);    // OAM shadow: every sprite hidden (y = $F0)
  for (let a = 0x6140; a < 0x6160; a++) m.wr(a, 0x0F);       // pal_buf: the intro ends faded to black
  m.ppu.pal.fill(0x0F);                                      // ... and its NMIs uploaded that
  m.ppu.writeReg(1, 0x1E);
  for (let r = 0; r < 6; r++) m.ppu.setChrReg(r, m.rd(0x07F0 + r));   // ApplyChrPlay as the intro left it
  m.wr(0x6E, 0x12); m.wr(0x6F, 0x13);                        // SwitchBank16(9) 31:F399: banks 18/19
  m.prgR6 = 0x12; m.prgR7 = 0x13;
}

/** CurSaveReset 31:F219: $7D00/$6D00 <- template, $7E00/$7F00/$6E00/$6F00 <- 0, save_reset_flag $7001 = $FF */
function curSaveReset(m) {
  for (let x = 0; x < 256; x++) {
    m.wr(0x6D00 + x, T(m, x)); m.wr(0x7D00 + x, T(m, x));
    for (const a of [0x6E00, 0x7E00, 0x6F00, 0x7F00]) m.wr(a + x, 0);
  }
  m.wr(0x7001, 0xFF);
}

/** SaveSlot0Reset 31:F11F / SaveSlot1Reset 31:F1A3: primary + backup first page <- template, other pages 0 */
function slotReset(m, pri, bak) {
  for (let x = 0; x < 256; x++) {
    m.wr(pri + x, T(m, x)); m.wr(bak + x, T(m, x));
    for (const p of [0x100, 0x200]) { m.wr(pri + p + x, 0); m.wr(bak + p + x, 0); }
  }
  m.wr(0x7001, 0xFF);
}

/** SaveSumSlot 31:F23D: tmp_16 += 3 pages from `page` (8-bit; $16 is NOT cleared first, the seed quirk).
 *  Leaves the ROM's pointers in $10-$15. */
function saveSumSlot(m, page) {
  let s = m.rd(0x16);
  for (let y = 0; y < 256; y++) {
    for (let p = 0; p < 3; p++) s = (s + m.rd(((page + p) << 8) + y)) & 0xFF;
  }
  m.wr(0x16, s);
  m.wr(0x10, 0); m.wr(0x11, page); m.wr(0x12, 0); m.wr(0x13, page + 1); m.wr(0x14, 0); m.wr(0x15, page + 2);
}

/** SlotCopy 31:F273: 3 pages src -> dst (pairs at SlotCopyPages 31:F298) */
function slotCopy(m, src, dst) {
  for (let i = 0; i < 0x300; i++) m.wr((dst << 8) + i, m.rd((src << 8) + i));
  m.wr(0x10, 0); m.wr(0x11, src + 3); m.wr(0x12, 0); m.wr(0x13, dst + 3);
}

function same3(m, a, b) {
  for (let i = 0; i < 0x300; i++) if (m.rd(a + i) !== m.rd(b + i)) return false;
  return true;
}
const tagOk = (m, page) => TAG.every((v, i) => m.rd(page + 0xFD + i) === v);

/** ColdSaveInit 31:F1CD: $700E/$700F = 0, boot_r7_save $7005 = R7, SetR7($17), JMP CurSaveReset (-> R7 back) */
export function coldSaveInit(m) {
  m.wr(0x700E, 0); m.wr(0x700F, 0);
  m.wr(0x7005, m.rd(0x6F));
  curSaveReset(m);
}

/** BootSaveCheck 31:F0A4 (REVERSE.md "Boot save check"). Ends with SetR7($7005) (BootSaveRestoreR7 31:F1C7). */
export function bootSaveCheck(m) {
  m.wr(0x7001, 0);
  m.wr(0x7005, m.rd(0x6F));
  // slot 0: $7100 primary, $7400 backup, checksums $70F0/$70F1
  if (same3(m, 0x7100, 0x7400)) {
    if (!tagOk(m, 0x7100)) slotReset(m, 0x7100, 0x7400);
  } else {
    saveSumSlot(m, 0x71);
    if (m.rd(0x70F0) === m.rd(0x16)) {
      slotCopy(m, 0x71, 0x74); m.wr(0x70F0, m.rd(0x16)); m.wr(0x70F1, m.rd(0x16));
    } else {
      saveSumSlot(m, 0x74);                       // continues from the failed primary sum
      if (m.rd(0x70F1) === m.rd(0x16)) {
        slotCopy(m, 0x74, 0x71); m.wr(0x70F0, m.rd(0x16)); m.wr(0x70F1, m.rd(0x16));
      } else slotReset(m, 0x7100, 0x7400);
    }
  }
  // slot 1: $7700 / $7A00, $70F2/$70F3; a primary match copies, sets only $70F2, then falls into the backup check
  if (same3(m, 0x7700, 0x7A00)) {
    if (!tagOk(m, 0x7700)) slotReset(m, 0x7700, 0x7A00);
  } else {
    saveSumSlot(m, 0x77);
    if (m.rd(0x70F2) === m.rd(0x16)) { slotCopy(m, 0x77, 0x7A); m.wr(0x70F2, m.rd(0x16)); }
    saveSumSlot(m, 0x7A);                         // 31:F18A
    if (m.rd(0x70F3) === m.rd(0x16)) { slotCopy(m, 0x7A, 0x77); m.wr(0x70F3, m.rd(0x16)); }
    else slotReset(m, 0x7700, 0x7A00);
  }
  // BootCheckCurrent 31:F1E2: $7D00-$7FFF == $6D00-$6FFF and the AMI tag at $7DFD
  if (!same3(m, 0x6D00, 0x7D00) || !tagOk(m, 0x7D00)) curSaveReset(m);
  setR7(m, m.rd(0x7005));
}

/** CopySramBlock 31:F3A2 */
export function copySramBlock(m) {
  for (let x = 0; x < 256; x++) { m.wr(0x6480 + x, m.rd(0x7D00 + x)); m.wr(0x6580 + x, m.rd(0x7E00 + x)); }
}
