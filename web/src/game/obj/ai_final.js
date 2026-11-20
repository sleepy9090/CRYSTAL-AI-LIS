// re-c web port — AI $70/$F0 Ai70PhaseDispatch 27:BA2E, the final-scene object (session 97, "boss / final-scene
// AI port" (a)). REVERSE.md "AI $70 final-scene phases (session 97)" + jump table row `27:BA40`.
//
//   ai70PhaseDispatch(m, x)  generator: JMP (JmpTable27_BA40[obj_phase]) with $10/$11 = the handler address
//
//   phase 0  BA5A  once slot $0D is free: sound $6E, the 8-pass window wipe (BA71: $6000-$60DB box image, $7C ->
//                  $7E per column group BA9A, block $2E each pass), visited_map $62F0 = $FF, slot off
//   phase 1  BAF5  boss_cam_lock = $7F; obj_behav b7 gate; every 16th frame one type-$31 object from table BBCE
//                  (x y tile_ofs meta, 4 bytes; $FF ends) into slot X + anim>>4, slot 0 parked at ($80,$A0)
//                  frozen; at the table end orbit_level b7 gate, then messages $1B.05-07 (ShowMessage, NOT
//                  ported), full HP / MP, player AI = 2, slot off
//   phase 2  BBE7  $6060-$609F = $FF, attr_shadow -> $6060-$608F, block $04, sub_state $1F, phase+1
//   phase 3  BC0C  phase+1; pal ids $07E0-2 = $A2-$A4 one by one, each FadeIn(9) with $07E8 = BC34[i]; sound $14
//   phase 4  BC37  hit flash, overlay $FC / meta $FB, anim 0, timer2+1 -> obj_behav = BC61[timer2 & $0F], phase+1
//   phase 5  BC71  hit flash; obj_behav countdown, then coll $34 and anim+1 up to $1F -> phase 6, obj_behav $20
//   phase 6  BC94  hit flash; shot $29 (dir 4, +4/+$12) on $064E & 3 and $08 & 7 = 0; countdown, anim-1 to 0 -> 4
//   phase 8  BD29  shots: $034D b7 clear -> $2A from BD58 every 8th frame per slot; set -> $28 from BD89 by
//                  player x
//   phase 9  BCF6  phase+1; bank 11 $9E00 -> $6000 (256 bytes), block $44 (REVERSE.md "third copy site")
//   phase 10 BD19  timer2+1; wrap -> sub_state $1E (SubstFinalSceneReset 19:BDF2), slot off
//   phase 12 BDA9  follower of the leader slot obj_param2: off unless the leader runs AI $2E; pose / coll / offset
//                  BE30 by the leader's meta & 3 and obj_act_timer; anim -4 per frame while the leader is not
//                  frozen (sound $6F at anim & $7F = 0 when onscreen)
//   phases 7, 11 (null) and >= 13 jump through garbage in the ROM: logged as unported here.
import { cpuRd, setR6 } from '../prg.js';
import { playSound } from '../playsound.js';
import { paletteBuildRom, fadeIn, hudRedraw, hudHpBar } from '../states/death.js';
import { ppuQueueBlock, waitPpuQueue } from '../states/pause.js';
import { farObjInit } from './objinit.js';
import { objPosToTemp, tempToObjPos, tempAddOffset, spawnShot } from './util.js';

const META = 0x0300, TILE_OFS = 0x0320, META_DIR = 0x0360, FLAGS = 0x0380, COLL = 0x03A0, ACTIVE = 0x04A0;
const ANIM = 0x04E0, OVERLAY = 0x0580, FREEZE = 0x05A0, PHASE = 0x0600, TIMER2 = 0x0620, ACT_TIMER = 0x0640;
const PARAM2 = 0x06A0, ORBIT = 0x06C0, BEHAV = 0x06E0, FLAGS2 = 0x0340;
const BUF = 0x6000;

function unported(m, what) {
  if (!m.unported.has(what)) { m.unported.add(what); m.log(`${what} not ported yet`); }
}
const inc = (m, a) => { const v = (m.rd(a) + 1) & 0xFF; m.wr(a, v); return v; };
const dec = (m, a) => { const v = (m.rd(a) - 1) & 0xFF; m.wr(a, v); return v; };

/** 26:9B07 (A = palette mask): obj_flags2 b7 clear -> PaletteBuild; else on odd frames every pal_buf entry
 *  whose group bit is set in A (entry 0 of each group always) turns $30, except $0F in the BG half. */
export function hitFlashPal(m, x, a) {
  m.wr(0x10, a);
  if (!(m.rd(FLAGS2 + x) & 0x80)) { paletteBuildRom(m, 0); return; }
  if (!(m.rd(0x08) & 1)) return;
  let y = 0;
  while (y !== 0x20) {
    if ((y & 3) === 0) {                                     // 9B34: next group's bit
      const b = m.rd(0x10);
      m.wr(0x10, b >> 1);
      if (!(b & 1)) { y += 4; continue; }
    }
    if (y >= 0x10 || m.rd(0x6140 + y) !== 0x0F) m.wr(0x6140 + y, 0x30);
    y++;
  }
}

/** 27:BCD7: hit flash (mask 7), irq_mode 0, or 2 with chr_r0_alt $58-$5D = chr_r0_play $07F0-$07F5 when $08 b2 clear */
function flashIrq(m, x) {
  hitFlashPal(m, x, 7);
  m.wr(0x51, 0);
  if (m.rd(0x08) & 4) return;
  m.wr(0x51, 2);
  for (let y = 5; y >= 0; y--) m.wr(0x58 + y, m.rd(0x07F0 + y));
}

/** 27:BAB8: the window image in $6000-$60DB (10 rows of $16: $1A ... $6F/$72/$75 ... $1B, corners $7F) */
function windowImage(m) {
  for (let x = 0xDC; x > 0; x--) m.wr(0x5FFF + x, 0x7C);
  for (let x = 0x16; x >= 0; x--) {
    m.wr(0x6001 + x, 0x6F); m.wr(0x6017 + x, 0x72); m.wr(0x60C7 + x, 0x75);
  }
  for (let x = 0, y = 10; y; y--, x += 0x16) { m.wr(BUF + x, 0x1A); m.wr(0x6015 + x, 0x1B); }
  m.wr(0x60C6, 0x7F); m.wr(0x60DB, 0x7F);
}

function* phase0(m, x) {
  if (m.rd(0x04AD)) return;
  playSound(m, 0x6E, 0);                                     // Y = obj_phase*2 = 0
  m.wr(0x2F, x);
  yield* waitPpuQueue(m);                                    // BA71
  windowImage(m);
  m.wr(0x1F, 7);
  do {
    yield* waitPpuQueue(m);
    m.wr(0x09, 0);
    for (let i = cpuRd(m, 0xBA9A + m.rd(0x1F)); i < 0xDC; i += 8) {   // BAA2
      if (m.rd(BUF + i) === 0x7C) m.wr(BUF + i, 0x7E);
    }
    yield* ppuQueueBlock(m, 0x2E);
  } while (!(dec(m, 0x1F) & 0x80));
  m.wr(0x62F0, 0xFF);
  m.wr(ACTIVE + x, 0);
}

function* phase1(m, x) {
  m.wr(0x07D7, 0x7F);
  if (!(m.rd(BEHAV + x) & 0x80)) { inc(m, BEHAV + x); return; }
  const anim = inc(m, ANIM + x);
  if (anim & 0x0F) return;
  const y = anim >> 2;
  if (cpuRd(m, 0xBBCE + y) !== 0xFF) {                       // BB79: one type-$31 object from table BBCE
    const n = (x + (anim >> 4)) & 0xFF;
    m.wr(0x10, n); m.wr(0x11, 0x31);
    farObjInit(m);
    m.wr(0x70 + n, cpuRd(m, 0xBBCE + y)); m.wr(0x90 + n, 0);
    m.wr(0xB0 + n, cpuRd(m, 0xBBCF + y)); m.wr(0xD0 + n, 0);
    m.wr(TILE_OFS + n, cpuRd(m, 0xBBD0 + y));
    m.wr(META + n, cpuRd(m, 0xBBD1 + y));
    m.wr(PHASE, 0); m.wr(TIMER2, 0); m.wr(ORBIT, 0); m.wr(META_DIR, 0);
    m.wr(FREEZE, 0xFF);
    m.wr(0x70, 0x80); m.wr(0xB0, 0xA0);
    return;
  }
  dec(m, ANIM + x);                                          // BB1A: table end
  if (!(m.rd(ORBIT + x) & 0x80)) { inc(m, ORBIT + x); return; }
  m.wr(0x07DF, 0x1B); m.wr(0x07DE, 5);
  do {
    playSound(m, 0x29, y);
    unported(m, 'AI $70 phase 1 ShowMessage 30:D354');   // BB37 JSR FarCall_D354: Y / X after it unknown
  } while (inc(m, 0x07DE) !== 8);
  m.wr(0x0676, 1); m.wr(0x04B6, 0x53); m.wr(0x05B6, 0);
  playSound(m, 0x25, y);
  m.wr(0x03C1, m.rd(0x03C0));
  yield* hudHpBar(m);
  m.wr(0x0708, m.rd(0x0709));
  yield* hudRedraw(m, 4, x);
  m.wr(ACTIVE, 2);
  m.wr(ACTIVE + x, 0);
}

function* phase2(m, x) {
  yield* waitPpuQueue(m);
  for (let y = 0x3F; y >= 0; y--) m.wr(0x6060 + y, 0xFF);
  for (let y = 0x2F; y >= 0; y--) m.wr(0x6060 + y, m.rd(0x6100 + y));
  yield* ppuQueueBlock(m, 4);
  m.wr(0x41, 0x1F);
  inc(m, PHASE + x);
}

function* phase3(m, x) {
  inc(m, PHASE + x);
  m.wr(0x2F, x);
  for (let i = 2; i >= 0; i--) {
    m.wr(0x07E8, cpuRd(m, 0xBC34 + i));
    m.wr(0x07E0 + i, i + 0xA2);
    yield* fadeIn(m, 9);
  }
  m.wr(0x07E8, 0);
  playSound(m, 0x14, 4);                                     // Y = 4 from PaletteLoadOne's last group
}

function phase4(m, x) {
  m.wr(COLL + x, 0);
  flashIrq(m, x);
  m.wr(OVERLAY + x, 0xFC);
  m.wr(META + x, 0xFB);
  m.wr(ANIM + x, 0);
  const t = inc(m, TIMER2 + x);
  m.wr(BEHAV + x, cpuRd(m, 0xBC61 + (t & 0x0F)));
  inc(m, PHASE + x);
}

function phase5(m, x) {
  flashIrq(m, x);
  if (dec(m, BEHAV + x)) return;
  inc(m, BEHAV + x);
  m.wr(COLL + x, 0x34);
  if (inc(m, ANIM + x) !== 0x1F) return;
  inc(m, PHASE + x);
  m.wr(BEHAV + x, 0x20);
}

function phase6(m, x) {
  flashIrq(m, x);
  if ((m.rd(0x064E) & 3) && !(m.rd(0x08) & 7) && spawnShot(m, x, 0x29, 4)) {
    const n = m.rd(0x10);
    m.wr(0x70 + n, (m.rd(0x70 + n) + 4) & 0xFF);
    m.wr(0xB0 + n, (m.rd(0xB0 + n) + 0x12) & 0xFF);
  }
  if (dec(m, BEHAV + x)) return;
  inc(m, BEHAV + x);
  if (dec(m, ANIM + x)) return;
  m.wr(PHASE + x, 4);
}

function phase8(m, x) {
  const f = m.rd(0x08);
  if (m.rd(0x034D) & 0x80) {                                 // BD68
    if (f & 7) return;
    m.wr(0x10, m.rd(0x70) >> 4);
    spawnShot(m, x, 0x28, cpuRd(m, 0xBD89 + (((x & 1) << 4) | m.rd(0x10))));
    return;
  }
  if (f === 0) inc(m, ACT_TIMER + x);
  if ((((x << 2) + m.rd(ACT_TIMER + x)) & 7) || (f & 1)) return;
  const d = cpuRd(m, 0xBD58 + ((f & 0x1E) >> 1));
  if (!(d & 0x80)) spawnShot(m, x, 0x2A, d);
}

function* phase9(m, x) {
  inc(m, PHASE + x);
  yield* waitPpuQueue(m);
  setR6(m, 0x0B);
  for (let y = 0; y < 0x100; y++) m.wr(BUF + y, cpuRd(m, 0x9E00 + y));
  yield* ppuQueueBlock(m, 0x44);
  yield* waitPpuQueue(m);
  setR6(m, 0x1A);
}

function phase10(m, x) {
  if (inc(m, TIMER2 + x)) return;
  m.wr(0x41, 0x1E);
  m.wr(ACTIVE + x, 0);
}

function phase12(m, x) {
  const l = m.rd(PARAM2 + x);                                // BDA9 LDY obj_param2,X (the leader slot)
  if (m.rd(ACTIVE + l) !== 0x2E) { m.wr(ACTIVE + x, 0); return; }
  if (!m.rd(FREEZE + l)) {
    m.wr(COLL + x, 0);
    m.wr(META + x, 0xF4);
    const a = (m.rd(ANIM + x) - 4) & 0xFF;
    m.wr(ANIM + x, a);
    if (!(a & 0x7F) && !(m.rd(FLAGS + x) & 0x80)) playSound(m, 0x6F, l);
  }
  m.wr(0x1F, x);                                             // BDDF
  objPosToTemp(m, l);
  m.wr(0x10, m.rd(ACT_TIMER + l));
  const k = m.rd(META + l) & 3;
  m.wr(0x11, k);
  let y = k << 1;
  if (m.rd(0x10)) {
    y |= 8;
    m.wr(META + x, k + 0xF5);
    m.wr(ANIM + x, k << 4);
    m.wr(COLL + x, (k & 1) | 0x46);
    m.wr(META_DIR + x, k << 1);
  }
  m.wr(0x30, cpuRd(m, 0xBE30 + y)); m.wr(0x31, cpuRd(m, 0xBE31 + y));
  tempAddOffset(m);
  tempToObjPos(m, x);
}

const PHASES = [phase0, phase1, phase2, phase3, phase4, phase5, phase6, null, phase8, phase9, phase10, null, phase12];

/** AI $70/$F0 27:BA2E */
export function* ai70PhaseDispatch(m, x) {
  const p = m.rd(PHASE + x), y = (p << 1) & 0xFF;
  m.wr(0x10, cpuRd(m, 0xBA40 + y));
  m.wr(0x11, cpuRd(m, 0xBA41 + y));
  const h = PHASES[p];
  if (!h) { unported(m, `AI $70 phase ${p} (JMP through $${(m.rd(0x11) << 8 | m.rd(0x10)).toString(16)})`); return; }
  const r = h(m, x);
  if (r && typeof r.next === 'function') yield* r;
}

export const AI_FINAL = { 0x70: ai70PhaseDispatch };
