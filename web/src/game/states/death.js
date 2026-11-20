// re-c web port — PlayerDeath 19:B900 (sub_state 3) + OpelRevive 19:B9B0, and the shared helpers they need
// (session 88, "Ph7 menu / death screens" (d)). REVERSE.md "Death / game over sequence" (under "Opel Statue
// auto-revive") + Verification Log row "Death / game over" (s89: 0/557 frames differ outside the HUD rows).
//
//   playerDeath(m)        generator: stat recalc, fade out, death pose, GAME OVER sprites, main_state = 2
//   statRecalc(m)         30:C008 (attack / defence, player CHR + pose, HUD MP digits + HP bar)
//   hudRedraw(m, e)       26:8E46 (HudElemTable 26:8EC5 record e -> digits -> PPU queue, src $7B)
//   hudHpBar(m)           26:8CC0 (HP bar $6060-$6070 -> PPU queue, src $60)
//   fadeOut / fadeIn      26:8C3D / 26:8C49 (5 PaletteBuild steps, A+1 frames apart, then 1 frame)
//   waitFrame / waitFrames  WaitFrame 30:C169 / 30:C176
//
// A `yield SPIN` marks a ROM spin loop that does NOT clear frame_ready (PpuQueueWaitSrc 30:C72B,
// WaitPpuQueue 30:C676): mainStep then leaves frame_ready set, so the NMI skips its OAM DMA / scroll copy
// exactly as on hardware. A plain `yield` = WaitFrame (frame_ready cleared before the NMI).
import { R } from '../ram.js';
import { cpuRd, setR6, setR7 } from '../prg.js';
import { paletteBuild } from '../palette.js';
import { playSound } from '../playsound.js';
import { spriteDraw } from '../obj/sprites.js';
import { readPads } from '../pads.js';
import { queueHasSrc, queueEmpty, queuePush, ppuQueueFlush, ppuCtrlNmiOn, ppuCtrlNmiOff } from '../world/ppuqueue.js';

export const SPIN = 'spin';
// s107: `yield NO_NMI` = the frame's NMI is lost (a PpuNmiOff18 section ran through the whole vblank and
// PpuNmiOn18 came after it). runFrame skips nmiWork once, and the next mainStep resumes the routine even
// though frame_ready is 0 (the ROM just keeps running in the next frame).
export const NO_NMI = 'no_nmi';

const dec = (m, a) => { const v = (m.rd(a) - 1) & 0xFF; m.wr(a, v); return v; };

/** WaitFrame 30:C169 */
export function* waitFrame(m) {
  m.wr(R.frame_ready, 0);
  yield;
  m.wr(R.frame_ready, 0);
}

/** 30:C176: Y x WaitFrame (Y = 0 -> 256) */
export function* waitFrames(m, y) {
  y &= 0xFF;
  do { yield* waitFrame(m); y = (y - 1) & 0xFF; } while (y !== 0);
}

/** PaletteBuild 26:8C0E / 8C10 (A = fade) / 8C12 (fade = $11 as is), with the zero-page temps it leaves:
 *  $11 = fade, $1A-$1C = last group's pointer / id, $1D = mask shifted out, then SetR7(saved $6F). */
export function paletteBuildRom(m, fade = m.rd(0x11)) {
  m.wr(0x11, fade);
  const r7 = m.rd(0x6F);
  paletteBuild(m, fade);
  const id = (m.rd(0x07E7) + 0xB0) & 0xFF;          // last group (X = $1C)
  m.wr(0x1C, id);
  m.wr(0x1A, ((id & 3) << 2) | 0xF0);
  m.wr(0x1B, ((id >> 2) & 0x1F) | 0xA0);
  m.wr(0x1D, 0);
  setR7(m, r7);
}

/** 26:8C3D (A = frames-1 per step; $11 0 -> $40: darker) / 26:8C49 ($11 $40 -> 0: back to full) */
function* fade(m, a, start, step) {
  m.wr(0x18, a); m.wr(0x11, start); m.wr(0x19, step); m.wr(0x17, 5);
  do {
    yield* waitFrames(m, m.rd(0x18) + 1);
    paletteBuildRom(m);                               // 8C5D JSR 8C12
    m.wr(0x11, (m.rd(0x11) + m.rd(0x19)) & 0xFF);
  } while (dec(m, 0x17) !== 0);
  yield* waitFrame(m);                                // 8C6B JMP WaitFrame
}
export const fadeOut = (m, a) => fade(m, a, 0x00, 0x10);
export const fadeIn = (m, a) => fade(m, a, 0x40, 0xF0);

/** 30:C8B2: PpuQueueFlush right away (rendering off), with ppuctrl_shadow b7 clear meanwhile */
export function queueFlushNow(m) {
  const s = m.rd(R.ppuctrl_shadow);
  m.wr(R.ppuctrl_shadow, s & 0x7F);
  const y = ppuQueueFlush(m);
  m.wr(R.ppuctrl_shadow, s);
  return y;                                           // s113: Y as PpuQueueFlush leaves it (undefined = untouched)
}

/** tail shared by the HUD writers: PpuNmiOff, queue record, flush now if rendering is off, PpuNmiOn */
export function hudQueue(m, hi, lo, len, src) {
  ppuCtrlNmiOff(m);
  queuePush(m, hi, lo, len, src);
  const y = (m.rd(R.ppumask_shadow) & 0x18) ? undefined : queueFlushNow(m);
  ppuCtrlNmiOn(m);
  return y;
}

/** 26:8F2B: 16-bit $10/$11 -> 5 decimal digits $20 (10^4) .. $24 (10^0), table 26:8F64 */
function toDecimal(m) {
  for (let i = 0; i < 5; i++) m.wr(0x20 + i, 0);
  m.wr(0x12, 8);
  for (;;) {
    const x = m.rd(0x12);
    const p = cpuRd(m, 0x8F64 + x) | cpuRd(m, 0x8F65 + x) << 8;
    const v = (m.rd(0x10) | m.rd(0x11) << 8) - p;
    if (v < 0) {                                      // BCC 8F5D: next power down
      const n = (x - 2) & 0xFF;
      m.wr(0x12, n);
      if (n & 0x80) return;
      continue;
    }
    m.wr(0x10, v & 0xFF); m.wr(0x11, v >> 8);
    m.wr(0x13, x);
    const d = (8 - x) >> 1;
    m.wr(0x20 + d, m.rd(0x20 + d) + 1);
  }
}

/** HudRedraw 26:8E46: element e (0-6) of HudElemTable 26:8EC5 (value ptr, PPU lo/hi, skip, 16-bit flag) */
export function* hudRedraw(m, e, xReg = 0) {
  m.wr(0x14, xReg);
  while (queueHasSrc(m, 0x7B)) yield SPIN;          // PpuQueueWaitSrc($7B)
  const x = e * 6;
  m.wr(0x10, (e << 1) & 0xFF);
  m.wr(0x18, x);
  const ptr = cpuRd(m, 0x8EC5 + x) | cpuRd(m, 0x8EC6 + x) << 8;
  m.wr(0x12, ptr & 0xFF); m.wr(0x13, ptr >> 8);
  m.wr(0x10, cpuRd(m, ptr)); m.wr(0x11, cpuRd(m, ptr + 1));
  if (cpuRd(m, 0x8ECA + x) === 0) m.wr(0x11, 0);     // 8-bit value
  toDecimal(m);
  const skip = cpuRd(m, 0x8EC9 + x);
  for (let y = 4, s = skip + 4; y >= 0; y--, s--) m.wr(0x607B + y, m.rd((0x20 + s) & 0xFF));
  const y = hudQueue(m, cpuRd(m, 0x8EC8 + x), cpuRd(m, 0x8EC7 + x), (5 - skip) & 0xFF, 0x7B);
  return y ?? x;                                      // s113: the Y register (LDY $18, or the flush's Y)
}

/** HudHpBar 26:8CC0: 17 tiles at $2B26: $8C/$8D frame up to max HP, $88 full cells, partial cell 26:8D49 */
export function* hudHpBar(m) {
  while (queueHasSrc(m, 0x60)) yield SPIN;          // PpuQueueWaitSrc($60)
  for (let x = 0; x <= 0x10; x++) m.wr(0x6060 + x, 0x20);
  const less1 = (v) => (v === 0 ? 0 : v - 1);
  let x = less1(m.rd(0x03C0)) >> 4;
  m.wr(0x6061 + x, 0x8D);
  for (; x >= 0; x--) m.wr(0x6060 + x, 0x8C);
  const hp = less1(m.rd(0x03C1));
  const y = hp >> 4;
  for (x = y; x > 0; x--) m.wr(0x605F + x, 0x88);
  m.wr(0x6060 + y, cpuRd(m, 0x8D49 + (hp & 0x0F)));
  hudQueue(m, 0x2B, 0x26, 0x11, 0x60);
}

/** 30:C008 StatRecalc: $03E1 attack, $0401 / obj_defense $0400 from level (x2 with ring $0E / $10 / $14) +
 *  26:8BD2 / 8BC0 / 8BC9; player palette / pose / CHR from obj1_flash $0710; then HUD MP digits + HP bar. */
export function* statRecalc(m, xReg = 0) {
  const r6 = m.rd(0x6E);
  setR6(m, 0x1A);
  const lvl = m.rd(0x0421), acc = m.rd(0x0716);
  const stat = (ring, tbl, eq) => (((acc === ring ? lvl << 1 : lvl) + cpuRd(m, tbl + m.rd(eq))) & 0xFF);
  m.wr(0x03E1, stat(0x0E, 0x8BD2, 0x0711));
  m.wr(0x0401, stat(0x10, 0x8BC0, 0x0713));
  m.wr(0x0400, stat(0x14, 0x8BC9, 0x0714));
  setR6(m, r6);
  const flash = m.rd(0x0710);
  m.wr(0x07E4, (flash & 0x0F) === 2 ? 0x4D : 0x00);
  const t = m.rd(0x6485) & 0xF0;
  m.wr(0x10, t);
  m.wr(0x6485, ((flash & 0x80) ? cpuRd(m, 0xC121 + ((flash >> 4) & 3)) : 0) | t);
  let special = true;
  if (flash & 0x80) m.wr(0x07F2, 0x4D);               // C107
  else if ((flash & 0x0F) === 4) m.wr(0x07F2, 0x64);  // C100
  else special = false;
  if (special) {                                      // C10C: stone / flash pose
    m.wr(0x0381, m.rd(0x0381) & 0xFD);
    m.wr(0x07F3, 0x42);
    m.wr(0x0321, 0x80);
  } else {                                            // C090: normal pose from facing
    m.wr(0x0321, 0);
    m.wr(0x0381, m.rd(0x0381) | 0x02);
    const d = (m.rd(0x0360) >> 1) & 3;
    m.wr(0x10, d);
    m.wr(0x0301, d);
    m.wr(0x0581, (m.rd(0x0714) ? 0x0C : 0x08) | d);
    m.wr(0x07F2, m.rd(0x0713) ? 0x41 : 0x40);
    let y = Math.min(m.rd(0x0711), 5);
    if (y === 0) y = 1;
    m.wr(0x07E5, y);
    m.wr(0x07F3, y + 0x41);
  }
  const s6 = m.rd(0x6E), s7 = m.rd(0x6F);            // C0DA
  setR6(m, 0x1A);
  if (!(m.rd(0x07E8) & 0x80)) paletteBuildRom(m, 0);
  yield* hudRedraw(m, 4, xReg);                       // MP digits
  yield* hudHpBar(m);
  while (!queueEmpty(m)) yield SPIN;                  // WaitPpuQueue 30:C676
  setR7(m, s7);
  setR6(m, s6);
}

/** 30:C188 (OamClipTop tail, A = limit): $10 = A; sprites 1-63 with y <= A -> y = $F0 */
function oamClip(m, a) {
  m.wr(0x10, a);
  for (let x = 4; x < 0x100; x += 4) if (a >= m.rd(0x0200 + x)) m.wr(0x0200 + x, 0xF0);
}

/** 19:BA03: WaitFrame + sprite pass until obj_anim_s01 counts down to 0 */
function* poseHold(m) {
  do {
    yield* waitFrame(m);
    spriteDraw(m);                                    // FarCall_B14_800E
  } while (dec(m, 0x04E1) !== 0);
}

/** 19:BA0F GameOverScreen: hide sprites, fade in to the GAME OVER palette, 8 sprites from 19:BA45,
 *  wait 80 frames or a new press, fade out */
function* gameOver(m) {
  oamClip(m, 0xF0);
  m.wr(0x07E4, 0x48);
  yield* fadeIn(m, 4);
  for (let x = 0x1F; x >= 0; x--) m.wr(0x0200 + x, cpuRd(m, 0xBA45 + x));
  m.wr(0x07F3, 0x35);
  m.wr(0x2F, 0x50);
  for (;;) {
    yield* waitFrame(m);
    readPads(m);
    if (dec(m, 0x2F) === 0) break;
    if (m.rd(R.pad_new) !== 0) break;
  }
  yield* fadeOut(m, 2);
}

/** 19:B9FB: SetR6($1A) + HudHpBar */
function* hpBarBank(m) { setR6(m, 0x1A); yield* hudHpBar(m); }

/** OpelRevive 19:B9B0: 64 frames, SFX $68, HP +4 per frame up to max, HUD, invulnerable, sub_state 6 */
function* opelRevive(m) {
  m.wr(0x0743, 0x40);
  yield* waitFrames(m, 0x40);
  playSound(m, 0x68, 0);
  for (;;) {
    yield* waitFrame(m);
    spriteDraw(m);
    m.wr(0x10, 4);
    let full = false;
    do {
      m.wr(0x03C1, m.rd(0x03C1) + 1);
      if (m.rd(0x03C1) === m.rd(0x03C0)) { full = true; break; }
    } while (dec(m, 0x10) !== 0);
    yield* hpBarBank(m);
    if (full) break;
  }
  yield* hudRedraw(m, 4, 3);                          // B9E1 JSR 8E46 (bank 26 still in from B9FB)
  m.wr(0x0381, m.rd(0x0381) | 0x04);
  m.wr(0x071A, 0xFF);
  yield* waitFrames(m, 0x40);
  m.wr(R.sub_state, 6);
}

/** PlayerDeath 19:B900 (sub_state 3; X = 3 from the dispatcher) */
export function* playerDeath(m) {
  m.wr(0x0710, m.rd(0x0710) & 0x0F);
  yield* statRecalc(m, 3);
  m.wr(0x06C0 + 3, 0);                                // B90D STA orbit_level,X (X = sub_state 3)
  m.wr(R.irq_mode, 0);
  if (m.rd(0x0715) === 0x0A) { yield* opelRevive(m); return; }   // Opel Statue selected
  playSound(m, 0x00, 0);                              // music off
  m.wr(0x06C0, 0);
  m.wr(0x07E8, 0x10);                                 // sprite group 0 (player) ignores the fade
  setR6(m, 0x1A);
  yield* fadeOut(m, 0x0A);
  playSound(m, 0x3E, 0);
  for (let y = 2; y < 0x1F; y++) m.wr(0x04A0 + y, 0); // every object but the player
  yield* waitFrame(m);
  m.wr(0x0381, m.rd(0x0381) & 0xFB);
  m.wr(0x07F2, 0x5C);
  m.wr(0x0301, 0xBC); m.wr(0x04E1, 0x40); m.wr(0x0581, 0x4C); m.wr(0x0321, 0);
  yield* poseHold(m);                                 // 64 frames, collapse pose
  m.wr(0x0301, 0xBD); m.wr(0x04E1, 0x40);
  yield* poseHold(m);                                 // 64 frames, lying pose
  for (let i = 0; i < 4; i++) m.wr(0x07E0 + i, 0xAF);
  for (let i = 5; i < 8; i++) m.wr(0x07E0 + i, 0x4F);
  m.wr(0x07E8, 0);
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);
  yield* waitFrame(m);
  yield* fadeOut(m, 0x10);
  yield* gameOver(m);
  m.wr(R.main_state, 2);                              // -> MainStateTitleInit (states/title.js)
}
