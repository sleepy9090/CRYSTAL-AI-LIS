// re-c web port — main states 2 / 3 (title side, banks 18/19), Ph7 web 7/8 (a), session 82.
//
//   mainState2(m)   30:C931 SwitchBank16(9) -> MainState2Init 18:9FCC -> 19:A000 (INC main_state)
//   mainState3(m)   30:C939 SwitchBank16(9), ReadPadsMenu, MainState3 19:A015
//
// Ported line by line from REVERSE.md "Main states 2/3" + "State-3 modes named". MainState3 dispatches on
// state3_mode $06F3 (0 = State3SubDispatch on title_mode $06F0, 1 = TitleFadeIn, 2 = TitleFadeOut).
// Ported title modes: 0 TitleOpening (25 steps), 5 TitleMenuInit, 6 TitleMenu (s82); 1 TitleStoryInit,
// 2 TitleIntroStory (28 steps), 3 TitleStatusInit, 4 TitleStatusScreen (7 steps incl. name entry) -> main_state 0
// (s107). PpuQueueDrainWait 19:A092 is a `yield SPIN` loop: the status text and name tiles go through the
// $6200 queue, which the NMI flushes.
//
// Routines that spin on frame_ready (WaitFrameReady 18:8198) are generators: the caller `yield*`s and
// mainloop.js resumes them on the next frame, as the ROM continues after the next NMI.
// Direct PPU writes (NtFill 18:81A1, NtRectBlit 18:81F0) go through m.ppu.writeReg like the ROM's $2000-$2007.
import { R } from '../ram.js';
import { cpuRd, indY, switchBank16 } from '../prg.js';
import { readPadsMenu } from '../pads.js';
import { SPIN, NO_NMI } from './death.js';

// title RAM (labels.csv)
const T = Object.freeze({
  title_mode: 0x06F0, title_step: 0x06F1, ram_06F2: 0x06F2, state3_mode: 0x06F3,
  jmp_lo: 0x06FC, jmp_hi: 0x06FD, wait: 0x06FE, ctr: 0x06FF, snd: 0x06B2,
  pal_target: 0x06D0,          // $06D0-$06EF MenuPalLoad result / fade target
  match_cnt: 0x06CF,
});
const OAM = 0x0200;

const inc = (m, a) => m.wr(a, m.rd(a) + 1);
const dec = (m, a) => { const v = (m.rd(a) - 1) & 0xFF; m.wr(a, v); return v; };
const ptr16 = (m, a) => cpuRd(m, a) | cpuRd(m, a + 1) << 8;

// ---------------------------------------------------------------- bank 18 screen-setup library
/** PpuNmiOff 18:8174 / PpuNmiOn 18:817C: PPUCTRL <- ppuctrl_shadow with b7 clear / set */
function ppuNmiOff(m) { m.ppu.writeReg(0, m.rd(R.ppuctrl_shadow) & 0x7F); }
function ppuNmiOn(m) { m.ppu.writeReg(0, m.rd(R.ppuctrl_shadow) | 0x80); }
/** 18:8184 / 18:818E: ppumask_shadow &= $E1 (BG + sprites off) / |= $1E, also written to PPUMASK */
function maskOff(m) { const v = m.rd(R.ppumask_shadow) & 0xE1; m.ppu.writeReg(1, v); m.wr(R.ppumask_shadow, v); }
function maskOn(m) { const v = m.rd(R.ppumask_shadow) | 0x1E; m.ppu.writeReg(1, v); m.wr(R.ppumask_shadow, v); }

/** WaitFrameReady 18:8198: spin until frame_ready != 0, then clear it */
export function* waitFrameReady(m) {
  while (m.rd(R.frame_ready) === 0) yield;
  m.wr(R.frame_ready, 0);
}

/** NtFill 18:81A1: A = PPU address hi; $3C0 x tile X, then $40 x attribute Y (rendering off meanwhile) */
export function ntFill(m, hi, tile, attr) {
  m.wr(0x72, tile); m.wr(0x73, attr);
  ppuNmiOff(m); maskOff(m);
  m.ppu.writeReg(6, hi); m.ppu.writeReg(6, 0);
  for (let i = 0; i < 0x3C0; i++) m.ppu.writeReg(7, tile);
  m.wr(0x88, 0);                                   // 81C3 DEC zp_88 loop ends at 0
  for (let i = 0; i < 0x40; i++) m.ppu.writeReg(7, attr);
  maskOn(m); ppuNmiOn(m);
}

/** OamHideAll18 18:81D8: oam_y = $F0 for every sprite (from sprite 1 unless irq_mode is 0 or 3) */
export function oamHideAll(m) {
  const im = m.rd(R.irq_mode);
  for (let x = (im === 0 || im === 3) ? 0 : 4; x < 256; x += 4) m.wr(OAM + x, 0xF0);
}

/** NtRectBlit 18:81F0: stream ($8E): W H, then H rows of W tiles (+ $06FE) to PPU ($8C/$8D) stride $20 */
export function ntRectBlit(m) {
  ppuNmiOff(m); maskOff(m);
  let src = m.rd(0x8E) | m.rd(0x8F) << 8;
  const w = cpuRd(m, src), h = cpuRd(m, src + 1);
  m.wr(0x72, w); m.wr(0x73, h);
  src += 2;
  let dst = m.rd(0x8C) | m.rd(0x8D) << 8;
  const bias = m.rd(T.wait);
  for (let r = 0; r < h; r++) {
    m.ppu.writeReg(6, dst >> 8); m.ppu.writeReg(6, dst & 0xFF);
    dst = (dst + 0x20) & 0xFFFF;
    for (let x = 0; x < w; x++) m.ppu.writeReg(7, cpuRd(m, src + x) + bias);
    src = (src + w) & 0xFFFF;
  }
  m.wr(0x8E, src & 0xFF); m.wr(0x8F, src >> 8);
  m.wr(0x8C, dst & 0xFF); m.wr(0x8D, dst >> 8);
  m.wr(0x73, 0);
  maskOn(m); ppuNmiOn(m);
}

/** blit helper: the opening/menu steps all do $8E/$8F <- word at `tbl`, $8C/$8D <- ppu, JSR NtRectBlit */
function blitFrom(m, tbl, ppu) {
  m.wr(0x8E, cpuRd(m, tbl)); m.wr(0x8F, cpuRd(m, tbl + 1));
  m.wr(0x8C, ppu & 0xFF); m.wr(0x8D, ppu >> 8);
  ntRectBlit(m);
}

/** MenuPalBlack 18:82B3 */
export function palBlack(m) { for (let x = 0; x < 32; x++) m.wr(R.pal_buf + x, 0x0F); }

/** MenuPalLoad 18:82BE: A b7 = sprite half; MenuPalSets 18:838B[A & 15] -> $06D0/$06E0; then the BG backdrop
 *  ($06D0) into the four sprite-palette backdrop entries $06E0/E4/E8/EC */
export function palLoad(m, a) {
  let x = (a & 0x80) ? 0x10 : 0;
  let y = (a & 0x0F) << 4;
  for (let i = 0; i < 16; i++) m.wr(T.pal_target + x++, cpuRd(m, 0x838B + y++));
  m.wr(0x72, 0);
  const bd = m.rd(T.pal_target);
  for (let i = 0; i < 4; i++) m.wr(T.pal_target + 0x10 + 4 * i, bd);
}

/** MenuPalApply 18:82F1 */
export function palApply(m) { for (let x = 0; x < 32; x++) m.wr(R.pal_buf + x, m.rd(T.pal_target + x)); }

/** MenuPalFadeIn 18:82FD: pal_buf = target & $0F, state3_mode = 1 */
export function palFadeIn(m) {
  for (let x = 0; x < 32; x++) m.wr(R.pal_buf + x, m.rd(T.pal_target + x) & 0x0F);
  m.wr(T.state3_mode, 1);
}

/** TitleFadeIn 18:8310 (state3_mode 1) */
function titleFadeIn(m) {
  if (m.rd(T.ctr) & 7) return;
  let n = 0;
  for (let x = 31; x >= 0; x--) {
    const a = m.rd(R.pal_buf + x);
    if (a === m.rd(T.pal_target + x)) n++;
    else m.wr(R.pal_buf + x, a + 0x10);
  }
  m.wr(T.match_cnt, n);
  if (n === 0x20) m.wr(T.state3_mode, 0);
}

/** TitleFadeOutStart 18:8344 */
function fadeOutStart(m) { m.wr(T.state3_mode, 2); }

/** TitleFadeOut 18:834A (state3_mode 2) */
function titleFadeOut(m) {
  if (m.rd(T.ctr) & 3) return;
  let n = 0;
  for (let x = 31; x >= 0; x--) {
    const a = m.rd(R.pal_buf + x);
    if (a === 0x0F) n++;
    else if ((a & 0xF0) === 0) m.wr(R.pal_buf + x, 0x0F);
    else m.wr(R.pal_buf + x, a - 0x10);
  }
  m.wr(T.match_cnt, n);
  if (n === 0x20) m.wr(T.state3_mode, 0);
}

/** SceneMetaDraw 18:80E7: metasprite $72 (SceneMetaPtrs 18:8F3B), frame $73, at x $88 / y $89 into OAM from 4 */
export function sceneMetaDraw(m) {
  m.wr(0x06C7, 0); m.wr(0x06C5, 4);
  sceneMetaBody(m);
}

/** 18:80DF: the clipped variant: $06C7 = $FF (hide records with x outside $06C6..$06C4-1), OAM from $06C5 on */
export function sceneMetaDrawClip(m) {
  m.wr(0x06C7, 0xFF);
  sceneMetaBody(m);
}

/** 18:80F1: the shared body (Y keeps counting across records: 8-bit, as the ROM's INY) */
function sceneMetaBody(m) {
  const y0 = (m.rd(0x72) << 1) & 0xFF;
  m.wr(0x86, cpuRd(m, 0x8F3B + y0)); m.wr(0x87, cpuRd(m, 0x8F3C + y0));
  m.wr(0x06C0, indY(m, 0x86, 0));
  m.wr(0x06C1, indY(m, 0x86, 1));
  let y = (m.rd(0x73) * 3 + 2) & 0xFF;
  const lo = indY(m, 0x86, y), hi = indY(m, 0x86, y + 1);
  m.wr(0x06C2, indY(m, 0x86, y + 2));
  m.wr(0x87, hi); m.wr(0x86, lo);
  let x = m.rd(0x06C5);
  for (let i = 0; ; i = (i + 4) & 0xFF) {
    const attr = indY(m, 0x86, i);
    if (attr === 0xFF) return;
    m.wr(OAM + 2 + x, attr);
    m.wr(OAM + 1 + x, indY(m, 0x86, i + 1));
    m.wr(OAM + 3 + x, indY(m, 0x86, i + 2) + m.rd(0x88));
    m.wr(OAM + x, indY(m, 0x86, i + 3) + m.rd(0x89));
    if (m.rd(0x06C7) & 0x80) {                       // 8155: clip window $06C6..$06C4 (never set here: $06C7 = 0)
      const ox = m.rd(OAM + 3 + x);
      if (ox < m.rd(0x06C6) || ox >= m.rd(0x06C4)) { m.wr(OAM + x, 0xF0); m.wr(0x06C5, x); continue; }
    }
    x = (x + 4) & 0xFF;
    m.wr(0x06C5, x);
  }
}

/** 19:A07C: queue sound A (snd_queue $0103 = A, count 1) and clear the pending title sound $06B2 */
export function titleSound(m, a) {
  m.wr(0x0103, a);
  m.wr(0x0101, 1); m.wr(0x0100, 1);
  m.wr(0x0101, 0);
  m.wr(T.snd, 0xFF);
}

/** TitleStepWait 18:9FC2: DEC $06FE, on 0 INC title_step */
function stepWait(m) { if (dec(m, T.wait) === 0) inc(m, T.title_step); }

// ---------------------------------------------------------------- main state 2
/** 30:C931 + MainState2Init 18:9FCC -> 19:A000 */
export function* mainState2(m) {
  switchBank16(m, 9);
  palBlack(m);
  oamHideAll(m);
  yield* waitFrameReady(m);
  m.wr(R.irq_mode, 3);
  for (let x = 0x28; x >= 0; x--) { m.wr(0x70 + x, 0); m.wr(0x0600 + x, 0); }
  for (let x = 0x80; x < 0x100; x++) m.wr(0x0600 + x, 0);
  for (const a of [T.title_mode, T.title_step, T.ram_06F2, T.state3_mode]) m.wr(a, 0);
  m.wr(R.frame_ready, 0);
  m.wr(R.scroll_shadow0, 0); m.wr(R.cam_y_lo, 0); m.wr(R.cam_x_hi, 0); m.wr(R.cam_y_hi, 0);  // 9FFD-A003
  titleSound(m, 0);                                  // A005 (A = 0)
  m.wr(T.snd, 0xFF);
  inc(m, R.main_state);                              // A00D -> 3
  titleSound(m, 0);
}

// ---------------------------------------------------------------- main state 3
/** 30:C939 + MainState3 19:A015 */
export function mainState3(m) {
  switchBank16(m, 9);
  readPadsMenu(m);
  const s = m.rd(T.snd);
  if (!(s & 0x80)) titleSound(m, s);
  m.wr(R.frame_ready, 0);
  dec(m, T.ctr);
  const mode = m.rd(T.state3_mode);
  setJmp(m, 0xA038 + mode * 2);
  if (mode === 0) return subDispatch(m);
  if (mode === 1) return titleFadeIn(m);
  if (mode === 2) return titleFadeOut(m);
  throw new Error(`state3_mode ${mode} out of range`);
}

/** the ROM's JMP ($06FC) dispatch: store the table word in $06FC/$06FD (kept for RAM fidelity), return it */
function setJmp(m, tblAddr) {
  m.wr(T.jmp_lo, cpuRd(m, tblAddr)); m.wr(T.jmp_hi, cpuRd(m, tblAddr + 1));
  return ptr16(m, tblAddr);
}

function unported(m, addr) {
  const k = `19:${addr.toString(16).toUpperCase()}`;
  if (!m.unported.has(k)) { m.unported.add(k); m.log(`title routine ${k} not ported yet`); }
}

/** State3SubDispatch 19:A03E: State3SubTable 19:A052[title_mode] */
function subDispatch(m) {
  const a = setJmp(m, 0xA052 + m.rd(T.title_mode) * 2);
  const fn = MODES.get(a);
  if (!fn) return unported(m, a);
  return fn(m);
}

/** step-table dispatch (TitleOpening JmpTable19_A7BC, TitleMenu JmpTable19_A6F3) */
function stepDispatch(m, tbl) {
  const a = setJmp(m, tbl + m.rd(T.title_step) * 2);
  const fn = STEPS.get(a);
  if (!fn) return unported(m, a);
  return fn(m);
}

// ---------------------------------------------------------------- title mode 0: TitleOpening 19:A79F
function titleOpening(m) {
  if (m.rd(R.pad_new) & 0x30) {                      // START/SELECT -> 19:A7EE
    titleSound(m, 0);
    return titleOpeningEnd(m);
  }
  return stepDispatch(m, 0xA7BC);
}

/** TitleOpeningEnd 19:ABAB */
function titleOpeningEnd(m) { fadeOutStart(m); m.wr(T.title_mode, 5); }

/** 19:A064 TitleStepFadeOut */
function stepFadeOut(m) { fadeOutStart(m); m.wr(T.wait, 0x1E); inc(m, T.title_step); }
/** 19:A070 */
function stepBlack(m) { palBlack(m); m.wr(T.wait, 0x3C); inc(m, T.title_step); }

/** OpeningDateCard 19:A7F6 (step 2) */
function openingDateCard(m) {
  palBlack(m); oamHideAll(m);
  m.wr(R.chr_r0_play, 0x3C); m.wr(R.chr_r0_play + 1, 0x34);
  ntFill(m, 0x20, 0x10, 0xFF);
  m.wr(T.wait, 0); m.wr(R.cam_y_lo, 0); m.wr(R.cam_y_hi, 0);
  blitFrom(m, 0xB162, 0x21A0);
  palLoad(m, 0); palFadeIn(m);
  m.wr(T.wait, 0x78); m.wr(T.snd, 0x0A);
  inc(m, T.title_step);
}

/** OpeningPage1 19:A843 (step 6) */
function openingPage1(m) {
  palBlack(m);
  for (let r = 2; r < 6; r++) m.wr(R.chr_r0_play + r, 0x7C);
  m.wr(R.chr_r0_play + 1, 0x16);
  ntFill(m, 0x20, 0x10, 0xFF);
  m.wr(T.wait, 0);
  blitFrom(m, 0x847B, 0x2042);
  blitFrom(m, 0x8C37, 0x23C0);
  blitFrom(m, 0xB1C0, 0x2240);
  palLoad(m, 0); palLoad(m, 0x80); palFadeIn(m);
  for (const a of [0x0600, 0x0618, 0x0620, 0x0628, 0x06B3]) m.wr(a, 0);
  inc(m, T.title_step);
}

/** OpeningPage1Anim 19:A8D2 (step 7): frames $0620 of metasprite $0618, frame time $06C2; tables 19:A8C8/A8CD */
function openingPage1Anim(m) {
  oamHideAll(m);
  m.wr(0x72, m.rd(0x0618)); m.wr(0x73, m.rd(0x0620));
  m.wr(0x88, 0x80); m.wr(0x89, 0x10);
  sceneMetaDraw(m);
  if (m.rd(0x0600) === 0) {
    m.wr(0x0628, m.rd(0x06C2));
    inc(m, 0x0600);
    return;
  }
  if (!(dec(m, 0x0628) & 0x80)) return;
  m.wr(0x0600, 0);
  const y = m.rd(0x06B3);
  if (cpuRd(m, 0xA8C8 + y) === m.rd(0x0620)) {
    inc(m, 0x06B3);
    m.wr(T.snd, cpuRd(m, 0xA8CD + y));
  }
  inc(m, 0x0620);
  if (m.rd(0x0620) === m.rd(0x06C0)) {
    m.wr(T.wait, 0x78);
    inc(m, T.title_step);
  }
}

/** OpeningPage1Flash 19:A92D (step 8) */
function openingPage1Flash(m) {
  const a = (m.rd(T.ctr) >> 1) & 1;
  palLoad(m, a); palLoad(m, a + 0x80); palApply(m);
  if (dec(m, T.wait) !== 0) return;
  palLoad(m, 0); palApply(m);
  inc(m, T.title_step);
}

/** OpeningPage2 19:A953 (step 11): two pages, $2000 and $2800 */
function* openingPage2(m) {
  palBlack(m); oamHideAll(m);
  m.wr(R.chr_r0_play + 1, 0x10);
  ntFill(m, 0x20, 0x10, 0xFF);
  m.wr(T.wait, 0x80);
  blitFrom(m, 0x8607, 0x2042);
  m.wr(T.wait, 0);
  blitFrom(m, 0x8C37, 0x23C0);
  blitFrom(m, 0xB25A, 0x2240);
  yield* waitFrameReady(m);                          // A9B0
  ntFill(m, 0x28, 0x10, 0xFF);
  m.wr(T.wait, 0x80);
  blitFrom(m, 0x8793, 0x2842);
  m.wr(T.wait, 0);
  blitFrom(m, 0x8C37, 0x2BC0);
  blitFrom(m, 0xB25A, 0x2A40);
  palLoad(m, 2); palFadeIn(m);
  m.wr(T.wait, 0xF0);
  inc(m, T.title_step);
}

/** OpeningPage2Flip 19:AA16 (steps 12, 13) */
function openingPage2Flip(m) {
  m.wr(R.cam_y_hi, (m.rd(T.ctr) >> 4) & 1);         // ROR x4 AND 1 (carry rotates in at b7: no effect on b0)
  if (dec(m, T.wait) !== 0) return;
  m.wr(T.wait, 0x3C);
  inc(m, T.title_step);
}

/** OpeningPage3 19:AA30 (step 16). Lag frame: the step runs past the frame end, and the NMI fires when the
 *  second blit's PpuNmiOn (18:817C) re-enables NMI inside vblank (tools.emu trace s82: f1239 line 241, after
 *  19:AA8F). The `yield` after that blit is that NMI; the rest of the step runs in the next frame. */
function* openingPage3(m) {
  palBlack(m);
  m.wr(R.chr_r0_play + 3, 0x11);
  m.wr(0x72, 1); m.wr(0x73, 0);
  m.wr(0x0610, 0x28); m.wr(0x88, 0x28);
  m.wr(0x0608, 0x20); m.wr(0x89, 0x20);
  sceneMetaDraw(m);
  ntFill(m, 0x20, 0x10, 0xFF);
  m.wr(R.cam_y_hi, 0);
  m.wr(T.wait, 0x80);
  blitFrom(m, 0x891F, 0x2042);
  m.wr(T.wait, 0);
  blitFrom(m, 0x8C37, 0x23C0);
  yield;                                             // NMI (lag frame, see above)
  blitFrom(m, 0xB337, 0x2240);
  palLoad(m, 3); palLoad(m, 0x88); palFadeIn(m);
  m.wr(0x0628, 2);
  m.wr(T.wait, 0xF0);
  inc(m, T.title_step);
}

/** OpeningPage3Anim 19:AAC2 (step 17) */
function openingPage3Anim(m) {
  if ((m.rd(T.ctr) & 3) === 0) inc(m, 0x0610);
  m.wr(0x72, 1); m.wr(0x73, 0);
  m.wr(0x88, m.rd(0x0610)); m.wr(0x89, m.rd(0x0608));
  sceneMetaDraw(m);
  if (dec(m, T.wait) !== 0) return;
  if (dec(m, 0x0628) !== 0) return;
  inc(m, T.title_step);
}

/** OpeningPage4 19:AAF1 (step 20) */
function openingPage4(m) {
  palBlack(m);
  for (let r = 2; r < 6; r++) m.wr(R.chr_r0_play + r, 0x7D);
  oamHideAll(m);
  m.wr(0x72, 2); m.wr(0x73, 0);
  m.wr(0x0610, 0x70); m.wr(0x88, 0x70);
  m.wr(0x0608, 0x38); m.wr(0x89, 0x38);
  sceneMetaDraw(m);
  ntFill(m, 0x20, 0x10, 0xFF);
  m.wr(T.wait, 0x80);
  blitFrom(m, 0x8AAB, 0x2042);
  m.wr(T.wait, 0);
  blitFrom(m, 0x8C5B, 0x23C0);
  blitFrom(m, 0xB40D, 0x2240);
  palLoad(m, 4); palFadeIn(m);
  m.wr(T.wait, 0x28);
  inc(m, T.title_step);
}

/** OpeningPage4Anim 19:AB81 (step 21) */
function openingPage4Anim(m) {
  if (m.rd(T.ctr) & 3) return;
  dec(m, 0x0608);
  m.wr(0x72, 2); m.wr(0x73, 0);
  m.wr(0x88, m.rd(0x0610)); m.wr(0x89, m.rd(0x0608));
  sceneMetaDraw(m);
  if (dec(m, T.wait) !== 0) return;
  inc(m, T.title_step);
}

// ---------------------------------------------------------------- title modes 5 / 6: the title menu
/** TitleMenuInit 19:A653 (mode 5), falls into TitleMenu */
function* titleMenuInit(m) {
  palBlack(m);
  yield* waitFrameReady(m);
  ntFill(m, 0x28, 0xFF, 0x00);
  m.wr(R.chr_r0_play, 0x1C); m.wr(R.chr_r0_play + 1, 0x1E);
  m.wr(R.chr_r0_play + 3, 0x73); m.wr(R.chr_r0_play + 5, 0x7B);
  for (const a of [T.title_step, T.ram_06F2, T.state3_mode, R.scroll_shadow0, R.cam_y_lo, R.cam_x_hi, R.cam_y_hi]) m.wr(a, 0);
  m.wr(R.cam_y_lo, 8);
  oamHideAll(m);
  yield* waitFrameReady(m);
  m.wr(T.wait, 0);
  blitFrom(m, 0x94BE, 0x2000);
  palLoad(m, 0x0C); palLoad(m, 0x8E); palFadeIn(m);
  m.wr(0x82, cpuRd(m, 0xA6DD)); m.wr(0x83, cpuRd(m, 0xA6DE));
  m.wr(0x84, 0x0E);
  m.wr(0x06B4, 0x96); m.wr(0x06B5, 2);
  m.wr(T.snd, 0x0B);
  inc(m, T.title_mode);
  const r = titleMenu(m);                            // A6DA JMP TitleMenu
  if (r && typeof r.next === 'function') yield* r;
}

/** TitleMenu 19:A6DF (mode 6): JmpTable19_A6F3[title_step] */
function titleMenu(m) { return stepDispatch(m, 0xA6F3); }

/** title-menu step 0 19:A6FE: $84 OAM records (index, y, tile, attr, x) from ($82); cursor sprite $02F0 */
function titleMenuSprites(m) {
  do {
    const x = indY(m, 0x82, 0);
    m.wr(OAM + x, indY(m, 0x82, 1));
    m.wr(OAM + 1 + x, indY(m, 0x82, 2));
    m.wr(OAM + 2 + x, indY(m, 0x82, 3));
    m.wr(OAM + 3 + x, indY(m, 0x82, 4));
    const p = (m.rd(0x82) | m.rd(0x83) << 8) + 5;
    m.wr(0x82, p & 0xFF); m.wr(0x83, (p >> 8) & 0xFF);
  } while (dec(m, 0x84) !== 0);
  inc(m, T.title_step);
  m.wr(0x82, 0);
  m.wr(0x02F0, 0xAF); m.wr(0x02F1, 0x7E); m.wr(0x02F2, 0); m.wr(0x02F3, cpuRd(m, 0xA77B));
}

/** TitleMenuInput 19:A749 (step 1): START -> 19:A77D, SELECT toggles title_menu_sel $82, idle timer $06B4/$06B5 */
function titleMenuInput(m) {
  if (m.rd(R.pad_new) & 0x10) {                      // TitleMenuStart 19:A77D
    titleSound(m, 0);
    m.wr(T.snd, 9);
    if (m.rd(0x82) !== 0) { m.wr(R.main_state, 8); return; }
    m.wr(T.title_mode, 1);
    return;
  }
  if (m.rd(R.pad_new) & 0x20) {
    m.wr(T.snd, 0x4B);
    const s = m.rd(0x82) ^ 1;
    m.wr(0x82, s);
    m.wr(0x02F3, cpuRd(m, 0xA77B + s));
    return;
  }
  if (dec(m, 0x06B4) !== 0) return;
  if (dec(m, 0x06B5) !== 0) return;
  fadeOutStart(m);
  inc(m, T.title_step);
}

/** TitleMenuRestart 19:A796 (step 2): DEC main_state (-> 2, MainState2Init again) */
function* titleMenuRestart(m) {
  m.wr(R.main_state, m.rd(R.main_state) - 1);
  palBlack(m);
  yield* waitFrameReady(m);
}

// ---------------------------------------------------------------- shared helpers of modes 2-4 (s107)
/** PpuQueueDrainWait 19:A092: spin until the NMI has drained the PPU queue ($0A = $0B), then reset both */
function* ppuQueueDrainWait(m) {
  while (m.rd(0x0A) !== m.rd(0x0B)) yield SPIN;
  ppuNmiOff(m);
  m.wr(0x0A, 0); m.wr(0x0B, 0);
  ppuNmiOn(m);
}

/** two $6200 records at $0B (hi, lo, len, src each) under PpuNmiOff18 / PpuNmiOn18, $0B += 8 */
function queuePair(m, hi1, lo1, hi2, lo2, len, src) {
  ppuNmiOff(m);
  const x = m.rd(0x0B);
  m.wr(0x6200 + x, hi1); m.wr(0x6204 + x, hi2);
  m.wr(0x6201 + x, lo1); m.wr(0x6205 + x, lo2);
  m.wr(0x6202 + x, len); m.wr(0x6206 + x, len);
  m.wr(0x6203 + x, src); m.wr(0x6207 + x, src);
  m.wr(0x0B, (x + 8) & 0x1F);
}

/** the cut-scene slot animation (19:AD85 / AF3B / A1D8): phase $0600,X = 0 -> timer $0628,X = $06C2, phase 1;
 *  else DEC timer, on < 0 phase 0 + next frame $0620,X (wrap to $06C1 at $06C0) */
function slotAnim(m, x) {
  if (m.rd(0x0600 + x) === 0) {
    m.wr(0x0628 + x, m.rd(0x06C2));
    inc(m, 0x0600 + x);
    return;
  }
  if (!(dec(m, 0x0628 + x) & 0x80)) return;
  m.wr(0x0600 + x, 0);
  inc(m, 0x0620 + x);
  if (m.rd(0x0620 + x) === m.rd(0x06C0)) m.wr(0x0620 + x, m.rd(0x06C1));
}

/** $72/$73/$88/$89 <- slot X ($0618 id, $0620 frame, $0610 x, $0608 y), then 18:80DF */
function slotDraw(m, x) {
  m.wr(0x72, m.rd(0x0618 + x)); m.wr(0x73, m.rd(0x0620 + x));
  m.wr(0x88, m.rd(0x0610 + x)); m.wr(0x89, m.rd(0x0608 + x));
  sceneMetaDrawClip(m);
}

/** slot tables: $0618/$0610/$0608,X <- tbls[0..2][X], $0600/$0620/$0628,X <- 0 (or extra tables) */
function slotInit(m, n, ids, xs, ys, frames = null, timers = null) {
  for (let x = 0; x < n; x++) {
    m.wr(0x0618 + x, cpuRd(m, ids + x)); m.wr(0x0610 + x, cpuRd(m, xs + x)); m.wr(0x0608 + x, cpuRd(m, ys + x));
    m.wr(0x0620 + x, frames ? cpuRd(m, frames + x) : 0);
    m.wr(0x0628 + x, timers ? cpuRd(m, timers + x) : 0);
    m.wr(0x0600 + x, 0);
  }
}

function camZero(m) {
  for (const a of [T.title_step, T.ram_06F2, T.state3_mode, R.frame_ready, R.scroll_shadow0, R.cam_y_lo, R.cam_x_hi, R.cam_y_hi]) m.wr(a, 0);
}

// ---------------------------------------------------------------- title mode 1: TitleStoryInit 19:ABB4
function* titleStoryInit(m) {
  palBlack(m);
  yield* waitFrameReady(m);
  m.wr(R.irq_mode, 3);
  m.wr(T.title_mode, 2);
  camZero(m);
  for (let r = 2; r < 6; r++) m.wr(R.chr_r0_play + r, 0x7E);
  oamHideAll(m);
}

// ---------------------------------------------------------------- title mode 2: TitleIntroStory 19:ABEA (28 steps)
function titleIntroStory(m) {
  if (m.rd(R.pad_new) & 0x30) return titleStoryEnd(m);
  return stepDispatch(m, 0xAC07);
}

/** TitleStoryEnd 19:B15C */
function titleStoryEnd(m) { m.wr(T.title_mode, 3); }

/** 19:AC3F (step 0): the scroll page, NT $2000 + $2800. Lag frame: the NMI fires at the 2nd blit's PpuNmiOn18
 *  (tools.emu trace s107: f1603 line 252, after 19:ACA4), the rest of the step runs in the next frame. */
function* storyScrollPage(m) {
  m.wr(R.chr_r0_play, 0x3C); m.wr(R.chr_r0_play + 1, 0x12);
  m.wr(R.chr_r0_play + 2, 0x7C); m.wr(R.chr_r0_play + 4, 0x7C);
  m.wr(R.chr_r0_play + 3, 0x7D); m.wr(R.chr_r0_play + 5, 0x7D);
  m.wr(R.cam_y_hi, 1); m.wr(R.cam_y_lo, 0xE8);
  ntFill(m, 0x20, 0x80, 0x00);
  ntFill(m, 0x28, 0x80, 0x00);
  m.wr(T.wait, 0x80);
  blitFrom(m, 0x8C7F, 0x2814);
  m.wr(T.wait, 0);
  blitFrom(m, 0x8EAF, 0x2BC0);
  yield;                                             // NMI (lag frame, see above)
  palLoad(m, 5); palLoad(m, 0x89); palApply(m);
  inc(m, T.title_step);
  for (let x = 5; x >= 0; x--) {
    m.wr(0x0618 + x, cpuRd(m, 0xACDA + x)); m.wr(0x0610 + x, cpuRd(m, 0xACDF + x)); m.wr(0x0608 + x, cpuRd(m, 0xACE4 + x));
    m.wr(0x0600 + x, 0); m.wr(0x0620 + x, 0); m.wr(0x0628 + x, 0);
  }
}

/** 19:ACE9 (step 1): cam_y_lo -= 8 until $68 */
function storyScrollUp(m) {
  const v = (m.rd(R.cam_y_lo) - 8) & 0xFF;
  m.wr(R.cam_y_lo, v);
  if (v === 0x68) inc(m, T.title_step);
}

/** 19:ACF9 (step 2): slot 0 drawn, its y += 8 while cam_y_lo -= 8 down to 0 */
function storyScrollSprite(m) {
  m.wr(0x72, m.rd(0x0618)); m.wr(0x73, m.rd(0x0620));
  m.wr(0x88, m.rd(0x0610)); m.wr(0x89, m.rd(0x0608));
  sceneMetaDraw(m);
  m.wr(0x0608, m.rd(0x0608) + 8);
  const v = (m.rd(R.cam_y_lo) - 8) & 0xFF;
  m.wr(R.cam_y_lo, v);
  if (v === 0) inc(m, T.title_step);
}

/** 19:AD27 (step 3): clip window $98..$EF, slot 0 y -= 8, $0609 = $68 */
function storyBirdsInit(m) {
  m.wr(0x06C6, 0x98); m.wr(0x06C4, 0xF0);
  m.wr(T.wait, 0);
  m.wr(0x0608, m.rd(0x0608) - 8);
  m.wr(0x0609, 0x68);
  m.wr(0x72, m.rd(0x0618)); m.wr(0x73, 0);
  m.wr(0x88, m.rd(0x0610)); m.wr(0x89, m.rd(0x0608));
  sceneMetaDraw(m);
  inc(m, T.title_step);
}

/** 19:AD5E (step 4) / 19:AF14 (step 12): n slots drawn clipped + animated, moving every 2nd frame by
 *  dx/dy tables; a slot that leaves the box restarts from its init x/y */
function storySlots(m, n, dxT, dyT, x0T, y0T, out) {
  oamHideAll(m);
  m.wr(0x06C5, 4);
  for (let x = 0; x < n; x++) {
    m.wr(0x06C3, x);
    slotDraw(m, x);
    slotAnim(m, x);
    if (m.rd(T.ctr) & 1) {
      const nx = (m.rd(0x0610 + x) - cpuRd(m, dxT + x)) & 0xFF;
      m.wr(0x0610 + x, nx);
      const ny = (m.rd(0x0608 + x) - cpuRd(m, dyT + x)) & 0xFF;
      m.wr(0x0608 + x, ny);
      if (out(nx, ny)) { m.wr(0x0610 + x, cpuRd(m, x0T + x)); m.wr(0x0608 + x, cpuRd(m, y0T + x)); }
    }
  }
  m.wr(0x06C3, n);
}
function storyBirds(m) {
  storySlots(m, 5, 0xADF5, 0xADFA, 0xACDF, 0xACE4, (nx) => nx < 0x70);
  if (dec(m, T.wait) === 0) inc(m, T.title_step);
}

/** 19:ADFF / 19:AFCB: TitleFadeOutStart, next step */
function storyFadeOut(m) { fadeOutStart(m); inc(m, T.title_step); }

/** 19:AE06 (step 6) / 19:AFD2 (step 14): a text page on NT $2800 at $29A0, BG set 0 fade-in */
function storyTextPage(m, ptr) {
  oamHideAll(m);
  ntFill(m, 0x28, 0x10, 0xFF);
  m.wr(T.wait, 0);
  blitFrom(m, ptr, 0x29A0);
  palLoad(m, 0); palFadeIn(m);
  m.wr(T.wait, 0);
  inc(m, T.title_step);
}

/** 19:AE3D (step 10): the second scroll page (8 slots, tables 19:AEC2-AEE2) */
function storyPage2(m) {
  ntFill(m, 0x20, 0x80, 0x00);
  ntFill(m, 0x28, 0x80, 0x00);
  m.wr(R.cam_y_hi, 0); m.wr(R.cam_y_lo, 0);
  m.wr(T.wait, 0x80);
  blitFrom(m, 0x8D73, 0x28C2);
  m.wr(T.wait, 0);
  blitFrom(m, 0x8EAF, 0x2BC0);
  palLoad(m, 7); palLoad(m, 0x89); palApply(m);
  inc(m, T.title_step);
  for (let x = 7; x >= 0; x--) {
    m.wr(0x0618 + x, cpuRd(m, 0xAEC2 + x)); m.wr(0x0610 + x, cpuRd(m, 0xAECA + x));
    m.wr(0x0608 + x, cpuRd(m, 0xAED2 + x)); m.wr(0x0620 + x, cpuRd(m, 0xAEDA + x));
    m.wr(0x0628 + x, cpuRd(m, 0xAEE2 + x)); m.wr(0x0600 + x, 0);
  }
}

/** 19:AEEA (step 11): cam_y_lo += 8 to $F0, then the next NT; clip window $08..$5F, timer $06CC/$06CD = $80/2 */
function storyScrollDown(m) {
  const v = (m.rd(R.cam_y_lo) + 8) & 0xFF;
  m.wr(R.cam_y_lo, v);
  if (v !== 0xF0) return;
  m.wr(R.cam_y_lo, 0);
  inc(m, R.cam_y_hi);
  m.wr(0x06C6, 0x08); m.wr(0x06C4, 0x60);
  m.wr(0x06CC, 0x80); m.wr(0x06CD, 2);
  inc(m, T.title_step);
}

/** 19:AF14 (step 12) */
function storyPage2Anim(m) {
  storySlots(m, 8, 0xAFBB, 0xAFC3, 0xAECA, 0xAED2, (nx, ny) => nx < 0x08 || ny < 0x20);
  if (dec(m, 0x06CC) !== 0) return;
  if (dec(m, 0x06CD) !== 0) return;
  inc(m, T.title_step);
}

/** 19:B009 (step 18): picture page on NT $2000 with one animated metasprite (id 7) */
function storyPage3(m) {
  oamHideAll(m);
  ntFill(m, 0x20, 0x10, 0xFF);
  m.wr(R.chr_r0_play + 1, 0x16);
  m.wr(T.wait, 0); m.wr(R.cam_y_lo, 0); m.wr(R.cam_y_hi, 0);
  blitFrom(m, 0x8E67, 0x20CC);
  blitFrom(m, 0x8EF3, 0x23C0);
  blitFrom(m, 0xB5DB, 0x21C0);
  palLoad(m, 6); palLoad(m, 0x86); palFadeIn(m);
  m.wr(0x06C8, 2); m.wr(0x06C9, 0xA0); m.wr(0x06CA, 8);
  m.wr(0x0618, 7);
  m.wr(0x0600, 0); m.wr(0x0620, 0); m.wr(0x0628, 0);
  inc(m, T.title_step);
}

/** 19:B092 (step 19): the metasprite at (60, 30); its frame time $06CA shrinks by 2 per loop; $06C9/$06C8 timer */
function storyPage3Anim(m) {
  oamHideAll(m);
  m.wr(0x72, m.rd(0x0618)); m.wr(0x73, m.rd(0x0620));
  m.wr(0x88, 0x60); m.wr(0x89, 0x30);
  sceneMetaDraw(m);
  if (m.rd(0x0600) === 0) {
    m.wr(0x0628, m.rd(0x06CA));
    inc(m, 0x0600);
  } else if (dec(m, 0x0628) & 0x80) {
    m.wr(0x0600, 0);
    inc(m, 0x0620);
    if (m.rd(0x0620) === m.rd(0x06C0)) {
      m.wr(0x0620, 0); m.wr(0x0628, 0);
      dec(m, 0x06CA);
      if (dec(m, 0x06CA) & 0x80) m.wr(0x06CA, 0);
    }
  }
  if (dec(m, 0x06C9) !== 0) return;
  if (dec(m, 0x06C8) !== 0) return;
  inc(m, T.title_step);
}

/** 19:B0FB (step 22): the last page, NT $2000, wait $B4 */
function storyLastPage(m) {
  oamHideAll(m);
  ntFill(m, 0x20, 0x10, 0xFF);
  m.wr(T.wait, 0);
  blitFrom(m, 0x8E8B, 0x20CC);
  blitFrom(m, 0x8F17, 0x23C0);
  blitFrom(m, 0xB6ED, 0x2200);
  palLoad(m, 6); palFadeIn(m);
  m.wr(T.wait, 0xB4);
  inc(m, T.title_step);
}

// ---------------------------------------------------------------- title mode 3: TitleStatusInit 19:A0B5
function* titleStatusInit(m) {
  m.wr(T.snd, 0);
  palBlack(m); oamHideAll(m);
  yield* waitFrameReady(m);
  m.wr(R.cam_y_lo, 0); m.wr(R.cam_y_hi, 0);
  m.wr(R.chr_r0_play, 0x3C); m.wr(R.chr_r0_play + 1, 0x36);
  m.wr(R.chr_r0_play + 2, 0xC7); m.wr(R.chr_r0_play + 3, 0xC7);
  palLoad(m, 9); palLoad(m, 0x8B); palApply(m);
  palLoad(m, 0x0D);
  m.wr(0x06DD, 0x0F);
  m.wr(T.wait, 0);
  blitFrom(m, 0x99D0, 0x2000);
  blitFrom(m, 0x9DD4, 0x2203);
  // Lost NMI (tools.emu trace s107): the WaitFrameReady above returns in f1702, the full-screen blit plus the
  // name-entry blit hold NMI off through that frame's vblank, and PpuNmiOn18 comes at f1703 line 27, so f1702
  // has no NMI at all; the rest runs in f1703 before its NMI.
  yield NO_NMI;
  m.wr(T.title_step, 0);
  inc(m, T.title_mode);
  m.wr(0x0617, 0x0D); m.wr(0x0627, 0);
  slotInit(m, 4, 0xA16A, 0xA16E, 0xA172);
  m.wr(0x06B4, 0x78); m.wr(0x06BB, 0x78);
  m.wr(0x8B, 0x10); m.wr(0x8A, 3);
}

// ---------------------------------------------------------------- title mode 4: TitleStatusScreen 19:A176
function titleStatusScreen(m) {
  oamHideAll(m);
  statusAnimTick(m);
  statusSlotAnim(m);
  return stepDispatch(m, 0xA193);
}

/** StatusAnimTick 19:A212: a 7-sprite strip at OAM $58 (y $58, x $18 + 8i) from 19:9908[frame * 8]; every 8th
 *  pass status_anim_ctr $0617 -= 1 and the next frame (byte 7 of the record = jump target unless $FF) */
function statusAnimTick(m) {
  if (m.rd(0x06BB) === 0) return;
  let y = (m.rd(0x0627) << 3) & 0xFF;
  m.wr(0x061F, 0x18);
  for (let i = 0, x = 0x58; i < 7; i++, x += 4, y++) {
    m.wr(OAM + x, 0x58);
    m.wr(OAM + 1 + x, cpuRd(m, 0x9908 + y));
    m.wr(OAM + 2 + x, 0);
    m.wr(OAM + 3 + x, m.rd(0x061F));
    m.wr(0x061F, m.rd(0x061F) + 8);
  }
  m.wr(0x062F, 0);
  if (m.rd(T.ctr) & 7) return;
  dec(m, 0x0617);
  inc(m, 0x0627);
  const a = cpuRd(m, 0x9908 + y);
  if (a !== 0xFF) m.wr(0x0627, a);
}

/** StatusSlotAnim 19:A1AA: 4 slots, OAM from $80, no clipping window ($00..$FE) */
function statusSlotAnim(m) {
  m.wr(0x06C5, 0x80); m.wr(0x06C6, 0); m.wr(0x06C4, 0xFF);
  for (let x = 0; x < 4; x++) {
    m.wr(0x06C8, x);
    slotDraw(m, x);
    slotAnim(m, x);
  }
  m.wr(0x06C8, 4);
}

/** status step 0 19:A1A1: sound $57 */
function statusSound(m) { m.wr(T.snd, 0x57); inc(m, T.title_step); }

/** status step 1 19:A26B: at status_anim_ctr 5, sound $1F */
function statusWait5(m) {
  if (m.rd(0x0617) !== 5) return;
  m.wr(T.snd, 0x1F);
  inc(m, T.title_step);
}

/** status step 2 19:A27C: at status_anim_ctr 0, fade in and set up the first text line */
function statusTextStart(m) {
  if (m.rd(0x0617) !== 0) return;
  palFadeIn(m);
  inc(m, T.title_step);
  m.wr(0x92, 0);
  return statusLineSetup(m);
}

/** 19:A28C: text line $92 (count 19:B7C3, text ptr 19:B7C7), typed at $2084 with cursor sprite $0220; the
 *  two rows at $2084/$20A4 and $2884/$28A4 are blanked from $6018 ($10 x $18), one queue pair per frame */
function* statusLineSetup(m) {
  const x = m.rd(0x92);
  m.wr(0x92, x + 1);
  m.wr(0x06CB, cpuRd(m, 0xB7C3 + x));
  m.wr(0x90, cpuRd(m, 0xB7C7 + 2 * x)); m.wr(0x91, cpuRd(m, 0xB7C8 + 2 * x));
  m.wr(0x93, 0);
  m.wr(0x84, 0x84); m.wr(0x85, 0x20);
  m.wr(0x06B4, cpuRd(m, 0xA349 + m.rd(0x92)));
  m.wr(0x0220, 0x27); m.wr(0x0221, 0x22); m.wr(0x0222, 0x03); m.wr(0x0223, 0x20);
  for (let i = 0x17; i >= 0; i--) m.wr(0x6018 + i, 0x10);
  m.wr(T.snd, 0x5A);
  yield* ppuQueueDrainWait(m);
  queuePair(m, m.rd(0x85), m.rd(0x84), m.rd(0x85), m.rd(0x84) + 0x20, 0x18, 0x18);
  ppuNmiOn(m);
  yield* waitFrameReady(m);
  yield* ppuQueueDrainWait(m);
  const hi = m.rd(0x85) + 8;
  queuePair(m, hi, m.rd(0x84), hi, m.rd(0x84) + 0x20, 0x18, 0x18);
  ppuNmiOn(m);
}

/** StatusTypeStep 19:A34E (step 3): every 2nd pass one character (2 tiles: $6002 upper, $6003 glyph) at $84,
 *  written vertically to NT $2000 and $2800; when the line is done, a pause $06B4, then the next line (19:A28C)
 *  or, after line 4, the name-entry palette ($06DD = $20), fade-in and step 4 */
function statusTypeStep(m) {
  if (m.rd(T.ctr) & 1) return;
  if (m.rd(0x06CB) & 0x80) {
    if (dec(m, 0x06B4) !== 0) return;
    if (m.rd(0x92) !== 4) return statusLineSetup(m);
    m.wr(0x06DD, 0x20);
    m.wr(0x614D, 0);
    inc(m, T.state3_mode);
    inc(m, T.title_step);
    return;
  }
  return statusTypeChar(m);
}
function* statusTypeChar(m) {
  let y = m.rd(0x93);
  m.wr(0x6002, indY(m, 0x90, y)); m.wr(0x6003, indY(m, 0x90, y + 1));
  m.wr(0x93, y + 2);
  yield* ppuQueueDrainWait(m);
  const hi = m.rd(0x85) | 0xC0;
  queuePair(m, hi, m.rd(0x84), hi + 8, m.rd(0x84), 2, 2);
  inc(m, 0x84);
  ppuNmiOn(m);
  m.wr(0x0220, 0x27);
  m.wr(0x0223, m.rd(0x0223) + 8);
  if (dec(m, 0x06CB) & 0x80) m.wr(0x0220, 0xF0);
}

/** status step 4 19:A3DD: blank name ($6410/$6418 = $10 x 6), cursor $6420 = 0, name_entry_frames = 0 */
function nameEntryInit(m) {
  inc(m, T.title_step);
  for (let x = 5; x >= 0; x--) { m.wr(0x6410 + x, 0x10); m.wr(0x6418 + x, 0x10); }
  m.wr(0x6420, 0); m.wr(0x6423, 0);
}

/** NameEntryInput 19:A3F6 (step 5) */
function nameEntryInput(m) {
  inc(m, 0x6423);
  m.wr(0x0220, 0x49); m.wr(0x0221, 0x22); m.wr(0x0222, 0x03);
  m.wr(0x0223, (m.rd(0x6420) * 8 + 0x68) & 0xFF);
  nameEntryCursor(m);
  const p = m.rd(R.pad_new);
  if (p & 0x40) return nameCursorLeft(m);              // B
  if (p & 0x10) return nameEntryFinish(m);             // START
  if (!(p & 0x80)) return;                             // A
  const c = m.rd(0x06CC);
  if (c === 0xF2) return nameCursorLeft(m);
  if (c === 0xF1) return nameCursorRight(m);
  if (c === 0xF0) return nameEntryFinish(m);
  return nameEntryType(m);
}

/** 19:A448: type the cell under the cursor at $6420 (both tile bytes), advance (wrap at 6), queue the two
 *  tiles vertically at $210D + position in NT $2000 and $2800 */
function* nameEntryType(m) {
  m.wr(T.snd, 0x58);
  let x = m.rd(0x6420);
  m.wr(0x6002, m.rd(0x06CC)); m.wr(0x6410 + x, m.rd(0x06CC));
  m.wr(0x6003, m.rd(0x06CD)); m.wr(0x6418 + x, m.rd(0x06CD));
  const pos = x;
  x++;
  if (x === 6) x = 0;
  m.wr(0x6420, x);
  const a = pos + cpuRd(m, 0xA4B1);
  m.wr(0x72, a & 0xFF);
  m.wr(0x73, (cpuRd(m, 0xA4B2) + (a >> 8)) & 0xFF);
  yield* ppuQueueDrainWait(m);
  const hi = m.rd(0x73) | 0xC0;
  queuePair(m, hi, m.rd(0x72), hi + 8, m.rd(0x72), 2, 2);
  ppuNmiOn(m);
}

/** 19:A4BF / 19:A4D0: name cursor left / right (wrap 0..5), name_entry_frames = 0 */
function nameCursorLeft(m) {
  m.wr(0x6423, 0);
  let x = m.rd(0x6420) - 1;
  if (x < 0) x = 5;
  m.wr(0x6420, x);
}
function nameCursorRight(m) {
  m.wr(0x6423, 0);
  let x = m.rd(0x6420) + 1;
  if (x === 6) x = 0;
  m.wr(0x6420, x);
}

/** 19:A4E3: finish. Length = last non-blank of $6418; all blank -> the default name (19:A56C ptr: glyph row,
 *  0, upper row, 0) and again. Builds player_name $6400: upper-row code $76 -> glyph + $F8, $75 -> glyph | $80,
 *  else the glyph; 0-terminated. Then fade out, step 6. */
function nameEntryFinish(m) {
  for (;;) {
    let x = 5;
    while (x >= 0 && m.rd(0x6418 + x) === 0x10) x--;
    if (x >= 0) { x++; m.wr(0x6420, x); m.wr(0x6421, x); break; }
    const p = cpuRd(m, 0xA56C) | cpuRd(m, 0xA56D) << 8;   // 19:A53B
    m.wr(0x92, p & 0xFF); m.wr(0x93, p >> 8);
    let y = 0, a;
    for (x = 0; (a = cpuRd(m, p + y++)) !== 0; x++) m.wr(0x6418 + x, a);
    for (x = 0; (a = cpuRd(m, p + y++)) !== 0; x++) m.wr(0x6410 + x, a);
  }
  let x = 0;
  for (let y = 0; ; y++) {
    const hi = m.rd(0x6410 + y), g = m.rd(0x6418 + y);
    if (hi === 0x76) { m.wr(0x6400 + x, g); x++; m.wr(0x6400 + x, 0xF8); }
    else if (hi === 0x75) m.wr(0x6400 + x, g | 0x80);
    else m.wr(0x6400 + x, g);
    x++;
    if (dec(m, 0x6421) === 0) break;
  }
  m.wr(0x6400 + x, 0);
  fadeOutStart(m);
  inc(m, T.title_step);
}

/** NameEntryCursor 19:A585: pad_new b0-b3 = right / left / down / up (first set bit wins), skipping $FF cells
 *  of NameCellGrid 18:9EC2; cursor sprite $0208; $06CC/$06CD <- the cell code / glyph (19:A63A alt set if $05) */
function nameEntryCursor(m) {
  const p = m.rd(R.pad_new);
  if (p & 0x0F) m.wr(T.snd, 0x4B);
  const cell = () => { const y = cellIndex(m); return cpuRd(m, 0x9EC2 + y); };
  if (p & 1) {
    do { let a = m.rd(0x8A) + 1; if (a === 0x20) a = 0; m.wr(0x8A, a); } while (cell() === 0xFF);
  } else if (p & 2) {
    do { let a = (m.rd(0x8A) - 1) & 0xFF; if (a & 0x80) a = 0x1F; m.wr(0x8A, a); } while (cell() === 0xFF);
  } else if (p & 4) {
    do { let a = m.rd(0x8B) + 2; if (a === 0x18) a = 0x10; m.wr(0x8B, a); } while (cell() === 0xFF);
  } else if (p & 8) {
    do { let a = m.rd(0x8B) - 2; if (a === 0x0E) a = 0x16; m.wr(0x8B, a); } while (cell() === 0xFF);
  }
  m.wr(0x06B6, rorChain(p, p & 1 ? 1 : p & 2 ? 2 : p & 4 ? 3 : 4));
  m.wr(0x0208, (((m.rd(0x8B) + 1) << 3) + 1) & 0xFF);
  m.wr(0x0209, 0x22); m.wr(0x020A, 0x03);
  m.wr(0x020B, (m.rd(0x8A) << 3) & 0xFF);
  const y = cellIndex(m);
  m.wr(0x06CC, cpuRd(m, 0x9EC2 + y));
  m.wr(0x06CD, cpuRd(m, 0x9F42 + y));
  if (m.rd(R.cam_y_hi) === 0) return;
  const g = m.rd(0x06CD);
  if (g === 0x10 || g === 0x0C) return;
  m.wr(0x06CD, g + 0x32);
}

/** $06B6 = pad_new after k ROR's (1 for RIGHT, 2 LEFT, 3 DOWN, else 4). Carry in = 0: the caller's last carry
 *  op is 19:A40E CLC / ADC #$68 on $6420 * 8 (<= $90, no carry); LDA/AND/STA keep it. */
function rorChain(p, k) {
  let v = p, c = 0;
  for (let i = 0; i < k; i++) { const nc = v & 1; v = (v >> 1) | (c << 7); c = nc; }
  return v;
}

/** 19:A0A5: cell index ($8B - $10) * 16 + $8A -> $88 */
function cellIndex(m) {
  const y = ((((m.rd(0x8B) - 0x10) << 4) + m.rd(0x8A)) & 0xFF);
  m.wr(0x88, y);
  return y;
}

/** NameEntryDone 19:A57C (step 6) -> StartNewGame 19:A6F9 */
function* nameEntryDone(m) {
  palBlack(m);
  yield* waitFrameReady(m);
  m.wr(R.main_state, 0);
}

// ---------------------------------------------------------------- dispatch maps (ROM address -> port)
const MODES = new Map([
  [0xA79F, titleOpening], [0xA653, titleMenuInit], [0xA6DF, titleMenu],
  [0xABB4, titleStoryInit], [0xABEA, titleIntroStory], [0xA0B5, titleStatusInit], [0xA176, titleStatusScreen],
]);
const STEPS = new Map([
  [0xA070, stepBlack], [0x9FC2, stepWait], [0xA064, stepFadeOut], [0xABAB, titleOpeningEnd],
  [0xA7F6, openingDateCard], [0xA843, openingPage1], [0xA8D2, openingPage1Anim], [0xA92D, openingPage1Flash],
  [0xA953, openingPage2], [0xAA16, openingPage2Flip], [0xAA30, openingPage3], [0xAAC2, openingPage3Anim],
  [0xAAF1, openingPage4], [0xAB81, openingPage4Anim],
  [0xA6FE, titleMenuSprites], [0xA749, titleMenuInput], [0xA796, titleMenuRestart],
  // story steps (JmpTable19_AC07)
  [0xAC3F, storyScrollPage], [0xACE9, storyScrollUp], [0xACF9, storyScrollSprite], [0xAD27, storyBirdsInit],
  [0xAD5E, storyBirds], [0xADFF, storyFadeOut], [0xAE06, (m) => storyTextPage(m, 0xB42F)], [0xAE3D, storyPage2],
  [0xAEEA, storyScrollDown], [0xAF14, storyPage2Anim], [0xAFCB, storyFadeOut],
  [0xAFD2, (m) => storyTextPage(m, 0xB505)], [0xB009, storyPage3], [0xB092, storyPage3Anim],
  [0xB0FB, storyLastPage], [0xB15C, titleStoryEnd],
  // status / name-entry steps (JmpTable19_A193)
  [0xA1A1, statusSound], [0xA26B, statusWait5], [0xA27C, statusTextStart], [0xA34E, statusTypeStep],
  [0xA3DD, nameEntryInit], [0xA3F6, nameEntryInput], [0xA57C, nameEntryDone],
]);
