// re-c web port — AI $1A, the sword-4 level-3 charged shot (session 197). REVERSE.md "AI $1A ThunderStorm".
// Shot id $0F = (sword 4 - 1) * 4 | charge level 3 (26:9CA7) spawns type $1F in slot 4 with obj_active $1A;
// ObjUpdateAll sends id $1A to ObjUpdateAi1A 30:CD7F instead of ObjAiTable (whose $1A entry 27:B2F4 is an RTS).
//
//   thunderStep(m, x)  generator = 30:CD8A, one step per ObjUpdateAll pass (it waits on frames inside the pass):
//     30:CDD5  SetR6($1A), SpawnShot($1E, dir 0) (type $1B bolt, slots 5-$0B) placed on slot $0D + (phase & $0F)
//     WaitPpuQueue, 30:CE07 ($6000-$60BF = $7F), irq_mode saved, 30:CE12 step [obj_phase], irq_mode restored,
//     obj_phase++; at $6A: obj_active = 0, WaitFrame, irq_mode = 0, PaletteBuild. Else if irq_mode != 0:
//     obj_flags slot 1 |= $10 (the slot-0 ORA at CDCC is dead: its result is overwritten by LDA, no STA).
//   Step record 30:CF47 + 3 * phase (Y = 3p & $FF, the ADC carry picks the $D047 copy = the same table):
//     t20 b7   tile rows: X = t20; t21 = 0 -> nothing; else 2 x 8 bytes at (t21:$F0) -> nibble tiles $70+n at
//              $6000 + X (two 16-byte rows $20 apart), PpuQueueBlock(t22); odd $08 -> PaletteBuild (no wait),
//              even -> pal_buf 1-$1F = $0F with BG colours 1-3 of each group = $30, WaitFrame
//     t20 = 0  [t22 != 0 -> 30:CF28] pal_buf $6140-$614F <- (t21:$F0), $6150-$615F <- (t21+1:$F0), WaitFrame
//     t20 = 1  WaitFrame, PlaySound($43) (Y = 3p), 30:CF28, $6060-$609F pattern, PpuQueueBlock(3), WaitFrame
//     else     irq_mode 2 -> 0, PaletteBuild, WaitFrame
//   30:CF28: irq_mode = 2, CHR alt R0/R1 = $34, R2-R5 alt = the play set $07F2-$07F5.
import { R } from '../ram.js';
import { cpuRd, setR6, switchBank16 } from '../prg.js';
import { playSound } from '../playsound.js';
import { spawnShot } from './util.js';
import { paletteBuildRom, waitFrame } from '../states/death.js';
import { ppuQueueBlock, waitPpuQueue } from '../states/pause.js';

const PHASE = 0x0600, ACTIVE = 0x04A0, FLAGS = 0x0380;

/** 30:CF28 */
function irqAlt(m) {
  m.wr(R.irq_mode, 2);
  m.wr(R.chr_r0_alt, 0x34);
  m.wr(R.chr_r0_alt + 1, 0x34);
  for (let i = 2; i < 6; i++) m.wr(R.chr_r0_alt + i, m.rd(0x07F0 + i));
}

/** 30:CDD5: a bolt on enemy slot $0D + (phase & $0F) (copied even when that slot is empty) */
function spawnBolt(m, x) {
  setR6(m, 0x1A);
  if (!spawnShot(m, x, 0x1E, 0)) return;
  m.wr(0x11, x);
  const src = (m.rd(PHASE + x) & 0x0F) + 0x0D, dst = m.rd(0x10);
  for (const a of [0x70, 0x90, 0xB0, 0xD0]) m.wr(a + dst, m.rd(a + src));
}

/** 30:CE89: 8 bytes at ($10),Y -> 16 nibble tiles $70+n at $6000 + x; returns X + $20 */
function nibbleRow(m, x, y) {
  const ptr = (m.rd(0x11) << 8) | m.rd(0x10);
  m.wr(0x12, 8);
  do {
    const b = cpuRd(m, ptr + y);
    m.wr(0x6000 + x, (b >> 4) + 0x70);
    m.wr(0x6001 + x, (b & 0x0F) + 0x70);
    x += 2; y++;
    m.wr(0x12, m.rd(0x12) - 1);
  } while (m.rd(0x12));
  return [x + 0x10, y];
}

/** 30:CE12: the step for phase a */
function* step(m, a) {
  m.wr(0x10, a);
  const t = ((a << 1) & 0xFF) + a, y = t & 0xFF;
  const base = t > 0xFF ? 0xD047 : 0xCF47;
  for (let i = 0; i < 3; i++) m.wr(0x20 + i, cpuRd(m, base + y + i));
  switchBank16(m, 0);                                       // CE39
  const t20 = m.rd(0x20);
  if (t20 & 0x80) {
    m.wr(0x11, m.rd(0x21));
    if (!m.rd(0x21)) return;                                // CE47: RTS, no frame wait
    m.wr(0x10, 0xF0);
    let x = t20, yy = 0;
    [x, yy] = nibbleRow(m, x, yy);
    [x, yy] = nibbleRow(m, x, yy);
    yield* ppuQueueBlock(m, m.rd(0x22));
    if (m.rd(0x08) & 1) { setR6(m, 0x1A); paletteBuildRom(m, 0); return; }   // CE61 JMP $8C0E
    for (let i = 0x1F; i > 0; i--) m.wr(R.pal_buf + i, 0x0F);
    for (let i = 0x0C; i >= 0; i -= 4) for (let j = 1; j <= 3; j++) m.wr(R.pal_buf + i + j, 0x30);
    yield* waitFrame(m);
    return;
  }
  if (t20 === 0) {
    switchBank16(m, 0);
    if (m.rd(0x22)) irqAlt(m);
    m.wr(0x11, m.rd(0x21));
    m.wr(0x10, 0xF0);
    const ptr = (m.rd(0x21) << 8) | 0xF0;
    for (let i = 0; i < 0x10; i++) {
      m.wr(R.pal_buf + i, cpuRd(m, ptr + i));
      m.wr(R.pal_buf + 0x10 + i, cpuRd(m, ptr + 0x100 + i));
    }
    yield* waitFrame(m);
    return;
  }
  if (t20 === 1) {
    yield* waitFrame(m);
    playSound(m, 0x43, y);                                  // Y = 3p (WaitFrame keeps Y)
    irqAlt(m);
    for (let i = 0x0F; i >= 0; i--) {
      m.wr(0x6060 + i, 0); m.wr(0x6070 + i, 0x55); m.wr(0x6078 + i, 0xAA);
      m.wr(0x6088 + i, 0xAA); m.wr(0x6090 + i, 0xFF);
    }
    yield* ppuQueueBlock(m, 3);
    yield* waitFrame(m);
    return;
  }
  if (m.rd(R.irq_mode) === 2) m.wr(R.irq_mode, 0);          // CF13
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);
  yield* waitFrame(m);
}

/** 30:CD8A */
export function* thunderStep(m, x) {
  spawnBolt(m, x);
  yield* waitPpuQueue(m);                                   // CD8F
  for (let i = 0; i < 0xC0; i++) m.wr(0x6000 + i, 0x7F);    // CE07
  const irq = m.rd(R.irq_mode);
  yield* step(m, m.rd(PHASE + x));
  m.wr(R.irq_mode, irq);
  const p = (m.rd(PHASE + x) + 1) & 0xFF;
  m.wr(PHASE + x, p);
  if (p === 0x6A) {
    m.wr(ACTIVE + x, 0);
    yield* waitFrame(m);
    m.wr(R.irq_mode, 0);
    setR6(m, 0x1A);
    paletteBuildRom(m, 0);
    return;
  }
  if (m.rd(R.irq_mode)) m.wr(FLAGS + 1, m.rd(FLAGS + 1) | 0x10);   // CDC0 (slot 0's ORA has no STA)
}
