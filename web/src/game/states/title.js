// re-c web port — main states 2 / 3 (title side, banks 18/19), Ph7 web 7/8 (a), session 82.
//
//   mainState2(m)   30:C931 SwitchBank16(9) -> MainState2Init 18:9FCC -> 19:A000 (INC main_state)
//   mainState3(m)   30:C939 SwitchBank16(9), ReadPadsMenu, MainState3 19:A015
//
// Ported line by line from REVERSE.md "Main states 2/3" + "State-3 modes named". MainState3 dispatches on
// state3_mode $06F3 (0 = State3SubDispatch on title_mode $06F0, 1 = TitleFadeIn, 2 = TitleFadeOut).
// Ported title modes: 0 TitleOpening (25 steps), 5 TitleMenuInit, 6 TitleMenu. Modes 1-4 (story, status /
// name entry) log "not ported" once and do nothing (leftover task in REVERSE.md).
//
// Routines that spin on frame_ready (WaitFrameReady 18:8198) are generators: the caller `yield*`s and
// mainloop.js resumes them on the next frame, as the ROM continues after the next NMI.
// Direct PPU writes (NtFill 18:81A1, NtRectBlit 18:81F0) go through m.ppu.writeReg like the ROM's $2000-$2007.
import { R } from '../ram.js';
import { cpuRd, indY, switchBank16 } from '../prg.js';
import { readPadsMenu } from '../pads.js';

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
  const y0 = (m.rd(0x72) << 1) & 0xFF;
  m.wr(0x86, cpuRd(m, 0x8F3B + y0)); m.wr(0x87, cpuRd(m, 0x8F3C + y0));
  m.wr(0x06C0, indY(m, 0x86, 0));
  m.wr(0x06C1, indY(m, 0x86, 1));
  let y = (m.rd(0x73) * 3 + 2) & 0xFF;
  const lo = indY(m, 0x86, y), hi = indY(m, 0x86, y + 1);
  m.wr(0x06C2, indY(m, 0x86, y + 2));
  m.wr(0x87, hi); m.wr(0x86, lo);
  let x = m.rd(0x06C5);
  for (let i = 0; ; i += 4) {
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

// ---------------------------------------------------------------- dispatch maps (ROM address -> port)
const MODES = new Map([
  [0xA79F, titleOpening], [0xA653, titleMenuInit], [0xA6DF, titleMenu],
]);
const STEPS = new Map([
  [0xA070, stepBlack], [0x9FC2, stepWait], [0xA064, stepFadeOut], [0xABAB, titleOpeningEnd],
  [0xA7F6, openingDateCard], [0xA843, openingPage1], [0xA8D2, openingPage1Anim], [0xA92D, openingPage1Flash],
  [0xA953, openingPage2], [0xAA16, openingPage2Flip], [0xAA30, openingPage3], [0xAAC2, openingPage3Anim],
  [0xAAF1, openingPage4], [0xAB81, openingPage4Anim],
  [0xA6FE, titleMenuSprites], [0xA749, titleMenuInput], [0xA796, titleMenuRestart],
]);
