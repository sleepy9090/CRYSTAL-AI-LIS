// re-c web port — SubstWakeCutscene 30:D9B6 (sub_state $13) -> WakeCutscene 20:9400 (session 138).
// The new-game wake-up cut-scene: a self-timed loop in bank 20 with its own `LSR frame_ready / BCC` waits
// (lsrWait, states/talk.js). About 857 frames in all; sub_state = 8 is stored after ~505 of them
// (20:948A, inside the 120-frame wait) but the routine runs on to its own RTS before SubstPlay starts.
// Tail 30:D91F: StatRecalc, FarCall_B14_800E (sprite pass), PaletteBuild(0), 30:D8BE (pad_new 0, sub 8).
// REVERSE.md "New game (session 38)" + "bank 20 tables" (s72) + the s138 note on the task.
// 20:9685/9693 (a 4-sprite walk, frames $10-$17 at $96F6/$96FE) has no caller and is not ported.
import { R } from '../ram.js';
import { cpuRd, setR6, switchBank16, farRestore } from '../prg.js';
import { spriteDraw } from '../obj/sprites.js';
import { statRecalc, paletteBuildRom } from './death.js';
import { lsrWait } from './talk.js';

const T20 = 0x20, T21 = 0x21, T22 = 0x22, T26 = 0x26, T2A = 0x2A, T2C = 0x2C;
const CNT = 0x60F8, BASE_Y = 0x60FC, BASE_X = 0x60FF;

/** 20:9756: snd_queue = id, lock, count 1, unlock (a raw queue write, not PlaySound) */
function wakeSound(m, id) {
  m.wr(0x0103, id);
  m.wr(0x0101, 1);
  m.wr(0x0100, 1);
  m.wr(0x0101, 0);
}

/** 20:974C: tmp_22 x LSR-wait (X = 0 after it) */
function* waitT22(m) {
  let x = m.rd(T22);
  do { yield* lsrWait(m); x = (x - 1) & 0xFF; } while (x !== 0);
}

/** 20:94F5: the whole OAM page <- $F0 */
function oamClear(m) {
  for (let i = 0; i < 0x100; i++) m.wr(0x0200 + i, 0xF0);
}

/** 20:948F: pal_buf <- 16 bytes at $9B0E + (wram_60F8 & $0C) * 4 */
function palCycle(m) {
  const o = (m.rd(CNT) & 0x0C) << 2;
  for (let y = 0; y < 0x10; y++) m.wr(R.pal_buf + y, cpuRd(m, 0x9B0E + o + y));
}

/** 20:94A6 (A = set group): 4 x (tmp_21 LSR-waits, then WakePalPtrs[tmp_20] -> pal_buf), tmp_20 = A*8, +2 */
function* palFade(m, a) {
  m.wr(T20, (a << 3) & 0xFF);
  for (let k = 0; k < 4; k++) {
    if (k) m.wr(T20, (m.rd(T20) + 2) & 0xFF);
    let x = m.rd(T21);                                  // 94C0
    do { yield* lsrWait(m); x = (x - 1) & 0xFF; } while (x !== 0);
    const i = m.rd(T20);                                // 94C9
    m.wr(T26, cpuRd(m, 0x9A6A + i));
    m.wr(T26 + 1, cpuRd(m, 0x9A6B + i));
    const p = m.rd(T26) | m.rd(T26 + 1) << 8;
    for (let y = 0; y < 0x10; y++) m.wr(R.pal_buf + y, cpuRd(m, p + y));
    m.wr(0x23, 0);
    const c = m.rd(R.pal_buf);
    for (const a2 of [0x6150, 0x6154, 0x6158, 0x615C]) m.wr(a2, c);
  }
}

/** WakeSprDraw 20:9500: ($26) = count, then per sprite dy tile attr dx; OAM from tmp_2A */
function wakeSprDraw(m) {
  const p = m.rd(T26) | m.rd(T26 + 1) << 8;
  const rd = (y) => cpuRd(m, (p + (y & 0xFF)) & 0xFFFF);
  let y = 0;
  m.wr(T2C, rd(y));
  y++;
  let x = m.rd(T2A);
  do {
    const oam = (o) => 0x0200 + ((x + o) & 0xFF);
    const dy = rd(y);
    if (dy === 0x80) {                                  // 950F hidden: all 4 bytes $F8, Y += 4 (+1 at 9557)
      for (let o = 0; o < 4; o++) m.wr(oam(o), 0xF8);
      y += 4;
    } else {
      m.wr(oam(0), (dy + m.rd(BASE_Y)) & 0xFF);
      y++; m.wr(oam(1), rd(y));
      y++; m.wr(oam(2), rd(y));
      y++;
      const dx = rd(y), s = dx + m.rd(BASE_X);
      const keep = (dx & 0x80) ? s > 0xFF : s <= 0xFF;   // 953A BMI: negative dx needs the carry
      if (keep) m.wr(oam(3), s & 0xFF);
      else m.wr(oam(0), 0xF8);                          // 955D
    }
    x = (x + 4) & 0xFF;                                 // 9551
    m.wr(T2A, x);
    y++;
    m.wr(T2C, (m.rd(T2C) - 1) & 0xFF);
  } while (m.rd(T2C) !== 0);
}

/** WakeSprFrame 20:973D: A = frame -> WakeSprFramePtrs 20:9767 -> WakeSprDraw */
function wakeSprFrame(m, a) {
  const i = (a << 1) & 0xFF;
  m.wr(T26, cpuRd(m, 0x9767 + i));
  m.wr(T26 + 1, cpuRd(m, 0x9768 + i));
  wakeSprDraw(m);
}

const at = (m, x, y) => { m.wr(BASE_X, x); m.wr(BASE_Y, y); };

/** 20:95FA: the main scene (frames $1B, $1C+n, $19, $18 when b3, $20/$21 by b2), then tmp_22 waits */
function* sceneMain(m) {
  m.wr(T2A, 4);
  const c = m.rd(CNT);
  let i = (c & 3) << 1;
  at(m, cpuRd(m, 0x9675 + i), cpuRd(m, 0x9676 + i));
  wakeSprFrame(m, 0x1B);
  at(m, 0x60, 0x6B);
  wakeSprFrame(m, (c & 3) + 0x1C);
  i = (c & 0x0C) >> 1;
  at(m, cpuRd(m, 0x967D + i), cpuRd(m, 0x967E + i));
  wakeSprFrame(m, 0x19);
  if (c & 8) { at(m, 0x58, 0x53); wakeSprFrame(m, 0x18); }
  at(m, 0x80, 0x43);
  wakeSprFrame(m, 0x20 + ((c >> 2) & 1));              // 9659: b2 ends in the carry, LDA #0 / ADC #$20
  yield* waitT22(m);
}

/** 20:95B8 (rise) / 20:95D5 (with frame $1A): one 8-step frame at (80,73), 3 waits */
function* sceneRise(m, second) {
  at(m, 0x80, 0x73);
  m.wr(T2A, 4);
  if (second) wakeSprFrame(m, 0x1A);
  wakeSprFrame(m, (m.rd(CNT) & 7) + (second ? 8 : 0));
  m.wr(T22, 3);
  yield* waitT22(m);
}

/** 20:9706: sprite palette 0 ($6150-$6153) <- $972D + 0/4/8/$C, 8 waits each */
function* sprPalFade(m) {
  for (let x = 0; x < 0x10; x += 4) {
    for (let y = 0; y < 4; y++) m.wr(0x6150 + y, cpuRd(m, 0x972D + x + y));
    m.wr(T22, 8);
    yield* waitT22(m);
  }
}

/** WakeCutscene 20:9400 */
function* wakeCutscene(m) {
  yield* lsrWait(m);
  for (let x = 0; x < 0x0C; x++) m.wr(0x6154 + x, cpuRd(m, 0x9B02 + x));
  oamClear(m);
  m.wr(0x07F5, 0x73);                                   // chr_r5_play
  m.wr(CNT, 0);
  m.wr(T21, 8);
  yield* palFade(m, 2);
  m.wr(CNT, 0xC0);
  m.wr(T22, 3);
  wakeSound(m, 0x60);
  for (;;) {                                            // 9435
    palCycle(m);
    oamClear(m);
    yield* sceneMain(m);
    const c = (m.rd(CNT) - 1) & 0xFF;
    m.wr(CNT, c);
    if (c === 0) break;
    if (c === 0x60) m.wr(T22, 1);
    else if (c === 0xA0) m.wr(T22, 2);
  }
  wakeSound(m, 0x61);                                   // 945F
  oamClear(m);
  yield* palFade(m, 1);
  for (let x = 0x78; x; x--) yield* lsrWait(m);         // 9481
  m.wr(R.sub_state, 8);
  yield* palFade(m, 0);
  m.wr(T22, 0x60);
  yield* waitT22(m);
  oamClear(m);
  m.wr(CNT, 7);                                         // 9565
  wakeSound(m, 0x5D);
  do {
    yield* sceneRise(m, false);
    m.wr(CNT, (m.rd(CNT) - 1) & 0xFF);
  } while (m.rd(CNT) !== 0xFF);
  wakeSound(m, 0x5C);
  m.wr(T22, 0x30);
  yield* waitT22(m);
  m.wr(T2A, 4);
  wakeSprFrame(m, 0x1A);
  wakeSprFrame(m, 0);
  yield* sprPalFade(m);
  m.wr(T22, 0x60);
  yield* waitT22(m);
  wakeSound(m, 0x5E);
  m.wr(CNT, 7);
  do {
    yield* sceneRise(m, true);
    m.wr(CNT, (m.rd(CNT) - 1) & 0xFF);
  } while (m.rd(CNT) !== 0xFF);
}

/** SubstWakeCutscene 30:D9B6 + its tail 30:D91F */
export function* substWakeCutscene(m) {
  switchBank16(m, 0x0A);
  yield* wakeCutscene(m);
  yield* statRecalc(m, 0);                              // X = 0 after the last 974C wait
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);              // FarCall_B14_800E
  switchBank16(m, 0x0E);
  spriteDraw(m);
  farRestore(m, r6, r7);
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);                                // 26:8C0E
  m.wr(R.pad_new, 0);                                   // 30:D8BE
  m.wr(R.sub_state, 8);
}
