// re-c web port — main states 4 / 5: the ending, bank 17 (session 90, "Ph7 (low priority) web ending").
// REVERSE.md "Ending sequence, bank 17" + Verification Log row "Ending (web port)".
//
//   mainState4(m)   30:C944 SwitchBank16(8) -> EndingInit 17:A000 (INC main_state -> 5)
//   mainState5(m)   30:C94C SwitchBank16(8), ReadPadsMenu, EndingTick 17:A100
//
// Ported instruction by instruction. EndingTick dispatches on $0602 (EndingModeTable 17:A11A: 0 = scene,
// 1 = EndingFadeIn AA74, 2 = EndingFadeOut AAAD). The scene runs the sub-effect $060E (EndingSubTable 17:A140)
// every 4 frames while it is non-zero, else the step obj_phase $0600 (EndingPhaseTable 17:A166, 30 steps).
// The in-step waits (EndingWaitFrame 17:A744: one frame per queued row in EndingRect / EndingPicture /
// EndingTextLines, the step-3 flash, the name rows) are `yield`s (death.js waitFrame = the same spin).
// Direct PPU writes (NtFill_b17 17:A751) go through m.ppu.writeReg like the ROM's $2000-$2007.
import { R } from '../ram.js';
import { cpuRd, indY, switchBank16 } from '../prg.js';
import { readPadsMenu } from '../pads.js';
import { waitFrame } from './death.js';
import { oamHideAll, palBlack } from './title.js';

const Q = 0x6200;                     // PPU queue ring (world/ppuqueue.js): hi, lo, len, src; write index $0B
const OAM = 0x0200;
const TGT = 0x0618;                   // $0618-$0637 fade target
const PHASE = 0x0600, MODE = 0x0602, WAIT_LO = 0x0604, WAIT_HI = 0x0605, SUB = 0x060E, CNT = 0x0638;
const COLS = 0x0639, CLIP_Y = 0x063D, OAM_FROM = 0x063E, LINES = 0x063A;

const inc = (m, a) => m.wr(a, m.rd(a) + 1);
const dec = (m, a) => { const v = (m.rd(a) - 1) & 0xFF; m.wr(a, v); return v; };
const ptr16 = (m, a) => cpuRd(m, a) | cpuRd(m, a + 1) << 8;
/** zp pointer <- the ptr16 variable at `a` (LDA a / STA zp / LDA a+1 / STA zp+1) */
const setPtr = (m, zp, a) => { m.wr(zp, cpuRd(m, a)); m.wr(zp + 1, cpuRd(m, a + 1)); };
/** 16-bit zp pointer += n (CLC / ADC / LDA #0 / ADC) */
const addPtr = (m, zp, n) => { const v = m.rd(zp) + (n & 0xFF); m.wr(zp, v & 0xFF); m.wr(zp + 1, m.rd(zp + 1) + (v >> 8)); };
const nextStep = (m) => inc(m, PHASE);

// ---------------------------------------------------------------- bank 17 PPU / queue helpers
/** PpuNmiOff_b17 17:A720 / PpuNmiOn_b17 17:A728 */
function nmiOff(m) { m.ppu.writeReg(0, m.rd(R.ppuctrl_shadow) & 0x7F); }
function nmiOn(m) { m.ppu.writeReg(0, m.rd(R.ppuctrl_shadow) | 0x80); }
/** 17:A730 / 17:A73A */
function maskOff(m) { const v = m.rd(R.ppumask_shadow) & 0xE1; m.ppu.writeReg(1, v); m.wr(R.ppumask_shadow, v); }
function maskOn(m) { const v = m.rd(R.ppumask_shadow) | 0x1E; m.ppu.writeReg(1, v); m.wr(R.ppumask_shadow, v); }

/** NtFill_b17 17:A751: PPU hi:00 <- $3C0 x tile, then $40 x attr (NMI + rendering off meanwhile) */
function ntFill(m, hi, tile, attr) {
  m.wr(0x80, tile); m.wr(0x81, attr);
  nmiOff(m); maskOff(m);
  m.ppu.writeReg(6, hi); m.ppu.writeReg(6, 0);
  for (let i = 0; i < 0x3C0; i++) m.ppu.writeReg(7, tile);
  m.wr(0x82, 0);                                    // A773 DEC zp_82 loop ends at 0
  for (let i = 0; i < 0x40; i++) m.ppu.writeReg(7, attr);
  maskOn(m); nmiOn(m);
}

/** one queue record at $0B (X = $0B, ... TXA / ADC #4 / AND #$1F / STA $0B) */
function push(m, hi, lo, len, src) {
  const x = m.rd(0x0B);
  m.wr(Q + x, hi & 0xFF); m.wr(Q + 1 + x, lo & 0xFF); m.wr(Q + 2 + x, len & 0xFF); m.wr(Q + 3 + x, src);
  m.wr(0x0B, (x + 4) & 0x1F);
}

/** EndingSnd 17:A1AB: A -> snd_queue $0103, count $0100 = 1 under snd_queue_lock $0101 */
function endingSnd(m, a) {
  m.wr(0x0103, a);
  m.wr(0x0101, 1); m.wr(0x0100, 1);
  m.wr(0x0101, 0);
}

// ---------------------------------------------------------------- palettes
/** EndingPalLoad 17:AA22: A b7 = sprite half, low nibble = palette n (EndingPalettes 17:AAED) -> target */
function palLoad(m, a) {
  let x = (a & 0x80) ? 0x10 : 0;
  let y = (a & 0x0F) << 4;
  for (let i = 0; i < 16; i++) m.wr(TGT + x++, cpuRd(m, 0xAAED + y++));
  m.wr(0x80, 0);
  const bd = m.rd(TGT);
  for (let i = 0; i < 4; i++) m.wr(TGT + 0x10 + 4 * i, bd);
}
/** EndingPalApply 17:AA55 */
function palApply(m) { for (let x = 0x1F; x >= 0; x--) m.wr(R.pal_buf + x, m.rd(TGT + x)); }
/** EndingFadeInStart 17:AA61 */
function fadeInStart(m) {
  for (let x = 0x1F; x >= 0; x--) m.wr(R.pal_buf + x, m.rd(TGT + x) & 0x0F);
  m.wr(MODE, 1);
}
/** EndingFadeOutStart 17:AAA7 */
function fadeOutStart(m) { m.wr(MODE, 2); }

/** EndingFadeIn 17:AA74 (mode 1): every 8 frames pal_buf += $10 until it equals the target */
function fadeIn(m) {
  if (m.rd(0x8C) & 7) return;
  m.wr(CNT, 0);
  for (let x = 0x1F; x >= 0; x--) {
    const a = m.rd(R.pal_buf + x);
    if (a === m.rd(TGT + x)) inc(m, CNT);
    else m.wr(R.pal_buf + x, a + 0x10);
  }
  if (m.rd(CNT) === 0x20) m.wr(MODE, 0);
}

/** EndingFadeOut 17:AAAD (mode 2): every 4 frames pal_buf -= $10, floor $0F */
function fadeOut(m) {
  if (m.rd(0x8C) & 3) return;
  m.wr(CNT, 0);
  for (let x = 0x1F; x >= 0; x--) {
    const a = m.rd(R.pal_buf + x);
    if (a === 0x0F) inc(m, CNT);
    else if ((a & 0xF0) === 0) m.wr(R.pal_buf + x, 0x0F);
    else m.wr(R.pal_buf + x, a - 0x10);
  }
  if (m.rd(CNT) === 0x20) m.wr(MODE, 0);
}

// ---------------------------------------------------------------- $060E sub-effects (17:A056-A0EA)
/** 17:A07C: Y entries from X: one step up towards the target (low nibble kept), $0638 counts the done ones */
function subUp(m, x, n) {
  do {
    const t = m.rd(TGT + x);
    m.wr(0x060F, t & 0x0F);
    let a = (m.rd(R.pal_buf + x) & 0xF0) | m.rd(0x060F);
    if (a === t) inc(m, CNT);
    else a = (a + 0x10) & 0xFF;
    m.wr(R.pal_buf + x, a);
    x++;
  } while (--n);
}
/** 17:A0CF: Y entries from X: one step down to $0F */
function subDown(m, x, n) {
  do {
    const a = m.rd(R.pal_buf + x);
    if (a === 0x0F) inc(m, CNT);
    else {
      let v = (a - 0x10) & 0xFF;
      if (v & 0x80) v = 0x0F;
      m.wr(R.pal_buf + x, v);
    }
    x++;
  } while (--n);
}
const subDone = (m, n) => { if (m.rd(CNT) === n) m.wr(SUB, 0); };
const SUBS = new Map([
  [0xA056, (m) => { subUp(m, 0, 0x20); subDone(m, 0x20); }],                          // 1 fade all in
  [0xA069, (m) => { subUp(m, 9, 3); subDone(m, 3); }],                                // 2 BG pal 2 in
  [0xA0A2, (m) => { subDown(m, 0, 0x0C); subDown(m, 0x10, 0x10); subDone(m, 0x1C); }], // 3 all but BG 3 out
  [0xA0BC, (m) => { subDown(m, 9, 3); subDone(m, 3); }],                              // 4 BG pal 2 out
]);

// ---------------------------------------------------------------- metasprites (EndingSprites 17:B45E)
/** clear OAM bytes $063E..$FF to $F0 (every byte, not just y), then $80/$81 = metasprite pointer */
function sprBegin(m, a) {
  const y = (a << 1) & 0xFF;
  let x = m.rd(OAM_FROM);
  do { m.wr(OAM + x, 0xF0); x = (x + 1) & 0xFF; } while (x !== 0);
  m.wr(0x80, cpuRd(m, 0xB45E + y)); m.wr(0x81, cpuRd(m, 0xB45F + y));
}

/** EndingSprRel 17:A94B: records attr, dx, dy, tile relative to x $71 / y $70; off-screen or y >= $063D dropped */
function sprRel(m, a) {
  sprBegin(m, a);
  let y = 0, x = m.rd(OAM_FROM);
  const rel = (d, base) => {                        // -1 = wrapped off-screen (JMP A9C9)
    if (!(d & 0x80)) { const v = d + m.rd(base); return v > 0xFF ? -1 : v; }
    m.wr(0x72, ((d ^ 0xFF) + 1) & 0xFF);
    const v = m.rd(base) - m.rd(0x72);
    return v < 0 ? -1 : v;
  };
  for (;;) {
    m.wr(0x73, y);
    const attr = indY(m, 0x80, y);
    if (attr === 0xFF) return;
    m.wr(OAM + 2 + x, attr);
    y = (y + 1) & 0xFF;
    let skip = true;
    const sx = rel(indY(m, 0x80, y), 0x71);
    if (sx >= 0) {
      m.wr(OAM + 3 + x, sx);
      y = (y + 1) & 0xFF;
      const sy = rel(indY(m, 0x80, y), 0x70);
      if (sy >= 0) {
        m.wr(OAM + x, sy);
        y = (y + 1) & 0xFF;
        if (sy < m.rd(CLIP_Y)) {
          m.wr(OAM + 1 + x, indY(m, 0x80, y));
          y = (y + 1) & 0xFF;
          x = (x + 4) & 0xFF;
          skip = false;
        }
      }
    }
    if (skip) {                                     // A9C9
      y = (m.rd(0x73) + 4) & 0xFF;
      m.wr(OAM + x, 0xF0);
    }
  }
}

/** EndingSprAbs 17:A9D7: records attr, x, y, tile as is */
function sprAbs(m, a) {
  sprBegin(m, a);
  let y = 0, x = m.rd(OAM_FROM);
  for (;;) {
    const attr = indY(m, 0x80, y);
    if (attr === 0xFF) return;
    m.wr(OAM + 2 + x, attr);
    m.wr(OAM + 3 + x, indY(m, 0x80, y + 1));
    m.wr(OAM + x, indY(m, 0x80, y + 2));
    m.wr(OAM + 1 + x, indY(m, 0x80, y + 3));
    y = (y + 4) & 0xFF;
    x = (x + 4) & 0xFF;
  }
}

// ---------------------------------------------------------------- blitters (queue rows, one frame each)
/** $80 = W, $81 = H from ($88), $88 += 2, $84/$85 = $8A/$8B */
function blitHead(m) {
  m.wr(0x84, m.rd(0x8A)); m.wr(0x85, m.rd(0x8B));
  m.wr(0x80, indY(m, 0x88, 0)); m.wr(0x81, indY(m, 0x88, 1));
  addPtr(m, 0x88, 2);
}

/** EndingRect 17:A7A0: H lines of W tiles; rows (+$20) or, with $0639 = $40, columns (+1) */
function* rect(m) {
  blitHead(m);
  do {
    let n = m.rd(0x80), y = 0;
    do { m.wr(0x6000 + y, indY(m, 0x88, y)); y = (y + 1) & 0xFF; } while (--n & 0xFF);
    addPtr(m, 0x88, y);
    nmiOff(m);
    push(m, m.rd(0x85) | m.rd(COLS), m.rd(0x84), m.rd(0x80), 0);
    addPtr(m, 0x84, m.rd(COLS) ? 1 : 0x20);
    nmiOn(m);
    yield* waitFrame(m);
  } while (dec(m, 0x81) !== 0);
}

/** EndingTextLines 17:AB1D: H lines of W tiles + 1 address-advance byte */
function* textLines(m) {
  blitHead(m);
  do {
    let n = m.rd(0x80), y = 0;
    do { m.wr(0x6000 + y, indY(m, 0x88, y)); y = (y + 1) & 0xFF; } while (--n & 0xFF);
    const adv = indY(m, 0x88, y);
    y = (y + 1) & 0xFF;
    addPtr(m, 0x88, y);
    nmiOff(m);
    push(m, m.rd(0x85) | m.rd(COLS), m.rd(0x84), m.rd(0x80), 0);
    addPtr(m, 0x84, adv);
    nmiOn(m);
    yield* waitFrame(m);
  } while (dec(m, 0x81) !== 0);
}

/** EndingPicture 17:A81C: W x H metatiles (TL/TR/BL/BR tables B004/B0C4/B184/B244), 2 chained rows per frame,
 *  then 64 attribute bytes (1 frame), 32 bytes fade target, 2 -> chr_r0/r1 $07F0, 4 -> $07F2-$07F5 */
function* picture(m) {
  blitHead(m);
  do {
    m.wr(0x87, m.rd(0x80));
    m.wr(0x82, 0); m.wr(0x83, 0);
    do {
      const t = indY(m, 0x88, m.rd(0x82));
      inc(m, 0x82);
      const y = m.rd(0x83);
      m.wr(0x6000 + y, cpuRd(m, 0xB004 + t));
      m.wr(0x6001 + y, cpuRd(m, 0xB0C4 + t));
      m.wr(0x6040 + y, cpuRd(m, 0xB184 + t));
      m.wr(0x6041 + y, cpuRd(m, 0xB244 + t));
      m.wr(0x83, y + 2);
    } while (dec(m, 0x87) !== 0);
    addPtr(m, 0x88, m.rd(0x80));
    nmiOff(m);
    push(m, m.rd(0x85) | 0x80, m.rd(0x84), m.rd(0x83), 0x00);
    addPtr(m, 0x84, 0x20);
    push(m, m.rd(0x85), m.rd(0x84), m.rd(0x83), 0x40);
    addPtr(m, 0x84, 0x20);
    nmiOn(m);
    yield* waitFrame(m);
  } while (dec(m, 0x81) !== 0);
  let y = 0;
  for (let x = 0; x < 0x40; x++, y++) m.wr(0x6000 + y, indY(m, 0x88, y));
  nmiOff(m);
  push(m, m.rd(0x8B) | 0x03, 0xC0, 0x40, 0x00);
  nmiOn(m);
  yield* waitFrame(m);
  for (let x = 0; x < 0x20; x++, y++) m.wr(TGT + x, indY(m, 0x88, y));
  for (let x = 0; x < 2; x++, y++) m.wr(R.chr_r0_play + x, indY(m, 0x88, y));
  for (let x = 0; x < 4; x++, y++) m.wr(R.chr_r0_play + 2 + x, indY(m, 0x88, y));
  addPtr(m, 0x88, y);
}

// ---------------------------------------------------------------- steps (EndingPhaseTable 17:A166)
/** EndingWait 17:A0EB: 60 frames x ($0605 + 1), then the next step */
function stepWait(m) {
  if (!(dec(m, WAIT_LO) & 0x80)) return;
  m.wr(WAIT_LO, 0x3B);
  if (!(dec(m, WAIT_HI) & 0x80)) return;
  nextStep(m);
}
const setWait = (m, lo, hi) => { m.wr(WAIT_LO, lo); m.wr(WAIT_HI, hi); };
const sndStep = (id) => (m) => { endingSnd(m, id); nextStep(m); };
const fadeOutStep = (m) => { fadeOutStart(m); nextStep(m); };

/** step 0 EndingStep0Setup 17:A1BC */
function* step0(m) {
  m.wr(0x8A, 0x00); m.wr(0x8B, 0x20);
  setPtr(m, 0x88, 0xABD9);
  yield* picture(m);
  setPtr(m, 0x88, 0xB854);
  m.wr(0x78, 0xC0); m.wr(0x79, 0x20);
  m.wr(0x7A, 0x00); m.wr(0x7B, 0x28);
  m.wr(LINES, 0);
  m.wr(0x70, 0xF0); m.wr(0x71, 0x5C);
  setWait(m, 0, 1);
  fadeInStart(m);
  nextStep(m);
}

/** 17:A26B / A2BA: row pointer at zp += $20; $x3C0 of the 1st / 2nd nametable wraps to the other one */
function rowAdvance(m, zp) {
  addPtr(m, zp, 0x20);
  if (m.rd(zp) === 0xC0 && (m.rd(zp + 1) & 3) === 3) {
    m.wr(zp, 0);
    m.wr(zp + 1, (m.rd(zp + 1) & 0x28) ^ 0x08);
  }
}

/** step 3 EndingStep3Scroll 17:A212: one tile-column row (+ a blank row 30 lines below) per frame */
function* step3(m) {
  for (;;) {
    const col = indY(m, 0x88, 0);
    if (col !== 0) break;
    // A337: end of the list -> rewind; with C = 0 here the test is $063A - $2D >= 0 (so 64 rows in all)
    setPtr(m, 0x88, 0xB854);
    if (((m.rd(LINES) - 0x2D) & 0xFF) & 0x80) continue;   // JMP EndingStep3Scroll
    m.wr(0x8D, 0x3C);
    fadeOutStart(m);
    m.wr(MODE, 0);
    setWait(m, 0, 1);
    m.wr(0x7F, 8);
    do {                                              // A363: flashing palette, 40 frames
      palLoad(m, 1); palApply(m);
      yield* waitFrame(m); yield* waitFrame(m);
      palLoad(m, 2); palApply(m);
      yield* waitFrame(m); yield* waitFrame(m); yield* waitFrame(m);
    } while (dec(m, 0x7F) !== 0);
    endingSnd(m, 0x5B);
    nextStep(m);
    return;
  }
  m.wr(0x7C, indY(m, 0x88, 0));
  let y = 1, x = 0;
  for (let a; (a = indY(m, 0x88, y)) !== 0; y++, x++) m.wr(0x6000 + x, a);
  y++;
  addPtr(m, 0x88, y);
  nmiOff(m);
  push(m, m.rd(0x79) | 0x80, m.rd(0x78) + m.rd(0x7C), x, 0x00);
  rowAdvance(m, 0x78);
  for (let i = 0x1F; i >= 0; i--) m.wr(0x6020 + i, 0xFF);
  push(m, m.rd(0x7B) | 0x80, m.rd(0x7A), 0x20, 0x20);
  rowAdvance(m, 0x7A);
  if (m.rd(LINES) === 0x18) {                       // A2D9: attribute row $23C0 = $55
    for (let i = 0x0F; i >= 0; i--) m.wr(0x6040 + i, 0x55);
    push(m, 0x23, 0xC0, 0x10, 0x40);
  }
  nmiOn(m);
  const n = m.rd(LINES);
  inc(m, LINES);
  if (n >= 0x14) {                                  // A30E: scroll down 10 lines per row
    const v = (m.rd(R.cam_y_lo) + 0x0A) & 0xFF;
    m.wr(R.cam_y_lo, v);
    if (v === 0xF0) { m.wr(R.cam_y_lo, 0); m.wr(R.cam_y_hi, m.rd(R.cam_y_hi) ^ 1); }
  }
  if (m.rd(LINES) < 0x20) return;
  m.wr(0x70, m.rd(0x70) - 4);
  sprRel(m, 8);
}

/** step 5 EndingStep5Sink 17:A38F: hero sprite 8 down 1 px / 4 frames; after $8D runs out, fade out */
function step5(m) {
  sprRel(m, 8);
  if (!(m.rd(0x8C) & 3)) inc(m, 0x70);
  if (!(dec(m, 0x8D) & 0x80)) return;
  if (m.rd(0x8C) & 7) return;
  m.wr(MODE, 2);
  fadeOut(m);
  const done = m.rd(MODE) === 0;
  m.wr(MODE, 0);
  if (!done) return;
  setWait(m, 0, 1);
  nextStep(m);
}

/** step 7 EndingStep7Epilogue 17:A3CB: text page 1 + the player name centred on rows $2208 / $2228 */
function* step7(m) {
  m.wr(R.chr_r0_play, 0x3C);
  m.wr(R.cam_y_lo, 0); m.wr(R.cam_y_hi, 0);
  ntFill(m, 0x20, 0x10, 0x00);
  oamHideAll(m);
  palBlack(m);
  m.wr(0x0619, 0x30);
  setPtr(m, 0x88, 0xBE37);
  m.wr(0x8A, 0xC4); m.wr(0x8B, 0x20);
  yield* textLines(m);
  const len = m.rd(0x6420);
  m.wr(0x7E, (6 - len) & 0xFF);
  let x = 0, y = len;
  do {
    m.wr(0x6000 + x, m.rd(0x6410 + x));
    m.wr(0x6020 + x, m.rd(0x6418 + x));
    x = (x + 1) & 0xFF;
    y = (y - 1) & 0xFF;
  } while (y !== 0);
  yield* waitFrame(m);
  nmiOff(m);
  push(m, 0xA2, 0x08 + m.rd(0x7E), len, 0x00);
  push(m, 0x22, 0x28 + m.rd(0x7E), len, 0x20);
  nmiOn(m);
  yield* waitFrame(m);
  fadeInStart(m);
  setWait(m, 2, 0x0C);
  nextStep(m);
}

/** step 11 EndingStep11Page2 17:A483 */
function* step11(m) {
  ntFill(m, 0x20, 0x10, 0x00);
  m.wr(0x8A, 0xC4); m.wr(0x8B, 0x20);
  yield* textLines(m);
  fadeInStart(m);
  setWait(m, 0, 5);
  nextStep(m);
}

/** step 14 EndingStep14StaffCard 17:A4AF: "STAFF" ($A4E3) at $21CE */
function* step14(m) {
  m.wr(R.chr_r0_play, 0x1C);
  ntFill(m, 0x20, 0x20, 0x00);
  setPtr(m, 0x88, 0xA4E3);
  m.wr(0x8A, 0xCE); m.wr(0x8B, 0x21);
  yield* rect(m);
  fadeInStart(m);
  setWait(m, 0, 2);
  nextStep(m);
}

/** step 18 EndingStep18RollFrame 17:A4F3: border box (column + row blocks of ($AB95)), blank card area */
function* step18(m) {
  ntFill(m, 0x20, 0x2A, 0xFF);
  setPtr(m, 0x88, 0xAB95);
  m.wr(0x8A, 0x03); m.wr(0x8B, 0x20); m.wr(COLS, 0x40);
  yield* rect(m);
  m.wr(0x8A, 0x20); m.wr(0x8B, 0x22); m.wr(COLS, 0x00);
  yield* rect(m);
  setPtr(m, 0x88, 0xAB95);
  m.wr(0x8A, 0x0E); m.wr(0x8B, 0x20); m.wr(COLS, 0x40);
  yield* rect(m);
  m.wr(0x8A, 0x40); m.wr(0x8B, 0x23); m.wr(COLS, 0x00);
  yield* rect(m);
  setPtr(m, 0x88, 0xA57A);
  m.wr(0x8A, 0x40); m.wr(0x8B, 0x22);
  yield* rect(m);
  setPtr(m, 0x88, 0xAC83);
  m.wr(0x8D, 0);
  oamHideAll(m);
  nextStep(m);
}

/** step 19 EndingScript 17:A597 on EndingScriptBytes 17:A65E[$8D] */
function* step19(m) {
  const next = () => { const y = m.rd(0x8D); inc(m, 0x8D); return cpuRd(m, 0xA65E + y); };
  for (;;) {
    const b = next();
    if (b & 0x80) { fadeOutStart(m); m.wr(PHASE, 0x16); return; }
    if (b !== 0) {
      const y = (b << 1) & 0xFF;
      const s88 = m.rd(0x88), s89 = m.rd(0x89);   // PHA x2 (stack bytes not modelled)
      m.wr(0x88, cpuRd(m, 0xB8D2 + y)); m.wr(0x89, cpuRd(m, 0xB8D3 + y));
      m.wr(0x8A, 0x50); m.wr(0x8B, 0x22);
      yield* rect(m);
      m.wr(0x89, s89); m.wr(0x88, s88);
      const i = m.rd(0x8D);
      m.wr(SUB, cpuRd(m, 0xA65E + i));
      m.wr(WAIT_HI, cpuRd(m, 0xA65E + ((i + 1) & 0xFF)));
      m.wr(WAIT_LO, cpuRd(m, 0xA65E + ((i + 2) & 0xFF)));
      m.wr(0x8D, (i + 3) & 0xFF);
      nextStep(m);
      return;
    }
    m.wr(0x8A, 0x04); m.wr(0x8B, 0x20);            // 0: next portrait of the ($88) stream + its metasprite
    yield* picture(m);
    sprAbs(m, next());
  }
}

/** step 21 EndingScriptNext 17:A603 */
function step21(m) {
  const y = m.rd(0x8D);
  inc(m, 0x8D);
  m.wr(SUB, cpuRd(m, 0xA65E + y));
  m.wr(PHASE, 0x13);
}

/** step 22 EndingStep22Thanks 17:A613: ($BD78) at $2128 */
function* step22(m) {
  palBlack(m);
  for (let x = 0x1F; x >= 0; x--) m.wr(TGT + x, 0x0F);
  oamHideAll(m);
  ntFill(m, 0x20, 0x20, 0x00);
  m.wr(0x0619, 0x20);
  m.wr(0x8A, 0x28); m.wr(0x8B, 0x21);
  setPtr(m, 0x88, 0xBD78);
  yield* rect(m);
  fadeInStart(m);
  setWait(m, 0x14, 5);
  nextStep(m);
}

/** step 25 EndingStep25Final 17:A696: final picture ($B304), metasprites 5 / 6 (abs), 9 (rel, clip y $78) */
function* step25(m) {
  setPtr(m, 0x88, 0xB304);
  m.wr(0x8A, 0x00); m.wr(0x8B, 0x20);
  yield* picture(m);
  sprAbs(m, 5);
  m.wr(OAM_FROM, 0x8C);
  sprAbs(m, 6);
  m.wr(OAM_FROM, 0xAC);
  m.wr(0x71, 0x74); m.wr(0x70, 0x14);
  m.wr(CLIP_Y, 0x78);
  sprRel(m, 9);
  fadeInStart(m);
  nextStep(m);
}

/** step 26 EndingStep26Sink 17:A6D8 */
function step26(m) {
  sprRel(m, 9);
  if (!(m.rd(0x8C) & 3)) inc(m, 0x70);
  if (m.rd(0x70) < 0x8C) return;
  setWait(m, 0x28, 2);
  nextStep(m);
}

/** step 28 EndingStep28Last 17:A6FB: target $0635-$0637 = 20 24 04, applied at once; metasprite 7 */
function step28(m) {
  m.wr(0x0635, 0x20); m.wr(0x0636, 0x24); m.wr(0x0637, 0x04);
  palApply(m);
  m.wr(OAM_FROM, 0x8C);
  sprAbs(m, 7);
  nextStep(m);
}

const STEPS = new Map([
  [0xA1BC, step0], [0xA0EB, stepWait], [0xA209, sndStep(0x4A)], [0xA212, step3], [0xA38F, step5],
  [0xA3CB, step7], [0xA47A, sndStep(0x1D)], [0xA4A8, fadeOutStep], [0xA483, step11], [0xA4AF, step14],
  [0xA58E, sndStep(0x1C)], [0xA4EC, fadeOutStep], [0xA4F3, step18], [0xA597, step19], [0xA603, step21],
  [0xA613, step22], [0xA657, fadeOutStep], [0xA696, step25], [0xA6D8, step26], [0xA6FB, step28],
  [0xA71B, () => {}],                               // step 29: bare RTS, forever
]);

/** JMP ($008E) dispatch: $8E/$8F <- table word (kept for RAM fidelity) */
function jmp(m, tbl, i) {
  m.wr(0x8E, cpuRd(m, tbl + 2 * i)); m.wr(0x8F, cpuRd(m, tbl + 2 * i + 1));
  return ptr16(m, tbl + 2 * i);
}

/** run fn: plain function (one frame) or generator (spans frames) */
function* call(fn, m) {
  const r = fn(m);
  if (r && typeof r.next === 'function') yield* r;
}

/** scene 17:A14A: sub-effect 17:A120 every 4 frames while $060E != 0, else step obj_phase */
function* scene(m) {
  if (m.rd(SUB) !== 0) {
    if (m.rd(0x8C) & 3) return;
    m.wr(CNT, 0);
    const a = jmp(m, 0xA140, m.rd(SUB));
    const fn = SUBS.get(a);
    if (!fn) throw new Error(`ending sub-effect ${m.rd(SUB)} -> ${a.toString(16)}`);
    fn(m);
    return;
  }
  const a = jmp(m, 0xA166, m.rd(PHASE));
  const fn = STEPS.get(a);
  if (!fn) throw new Error(`ending step ${m.rd(PHASE)} -> ${a.toString(16)}`);
  yield* call(fn, m);
}

/** EndingTick 17:A100 */
function* endingTick(m) {
  m.wr(R.frame_ready, 0);
  dec(m, 0x8C);
  const mode = m.rd(MODE);
  jmp(m, 0xA11A, mode);
  if (mode === 0) yield* scene(m);
  else if (mode === 1) fadeIn(m);
  else if (mode === 2) fadeOut(m);
  else throw new Error(`ending mode ${mode}`);
}

// ---------------------------------------------------------------- main states
/** 30:C944 + EndingInit 17:A000 */
export function* mainState4(m) {
  switchBank16(m, 8);
  endingSnd(m, 0);                                  // EndingSilence 17:A1A2
  palBlack(m);
  yield* waitFrame(m);
  ntFill(m, 0x20, 0xFF, 0x55);
  ntFill(m, 0x28, 0xFF, 0x55);
  oamHideAll(m);
  m.wr(R.irq_mode, 3);
  m.wr(R.frame_ready, 0);
  m.wr(R.scroll_shadow0, 0); m.wr(R.cam_y_lo, 0); m.wr(R.cam_x_hi, 0); m.wr(R.cam_y_hi, 0);
  for (const a of [SUB, PHASE, 0x0601, MODE, COLS]) m.wr(a, 0);
  palLoad(m, 0x00);
  palLoad(m, 0x80);
  m.wr(CLIP_Y, 0xF0);
  m.wr(OAM_FROM, 0x04);
  inc(m, R.main_state);
  m.wr(0x700E, 0xFF);
}

/** 30:C94C + EndingTick 17:A100 */
export function* mainState5(m) {
  switchBank16(m, 8);
  readPadsMenu(m);
  yield* endingTick(m);
}
