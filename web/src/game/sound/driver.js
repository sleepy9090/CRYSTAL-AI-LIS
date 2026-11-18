// re-c web port — sound driver, bank 24 (Ph7 web 7/8, s56). Line-by-line port of SndInit 24:8020, SndCommand
// 24:8048, SndTick 24:8186, SndChannelTick 24:8276, SndReadOp 24:8347 + the 16 SndOpcodeTable handlers,
// SndEnvelopeTick 24:84AF, SndVibratoTick 24:86AA, SndFadeStep 24:8609 (REVERSE.md "Sound driver",
// "Sound data format"). All state is the ROM's RAM ($0100-$017F, zp $F0-$FF); all tables and stream bytes are
// read from PRG through cpuRd with the mapping the driver sets (R6 = 24, R7 = 25, or 12 for SFX).
// APU access goes through m.apuWrite(addr, v) / m.apuRead(addr) (Machine -> nes/apu_core.js).
//
// Checked by web/test/sound_check.mjs against tools/emu/sound_capture.py: every APU write and every RAM byte,
// per call, over 35 940 SoundUpdate calls (all music, all SFX over music, pause, fade, random queue traffic).
import { cpuRd } from '../prg.js';

// RAM fields (6-wide arrays, X = channel 0-5; flow ops use X+6 for the loop-2 slots)
const FLAGS = 0x0114, PTR_LO = 0x011A, PTR_HI = 0x0120, L1_LO = 0x0126, L1_HI = 0x0132, L1_CNT = 0x013E;
const WAIT = 0x014A, LEN = 0x0150, OCT = 0x0156, ENV = 0x015C, VIB = 0x0162, REG0 = 0x0168, SWEEP = 0x016E;
const PER_LO = 0x0174, PER_HI = 0x017A;
const L2_LO = 0x012C, L2_HI = 0x0138, L2_CNT = 0x0144;
const MUSIC_ID = 0x0111, SFX_PRI = 0x0112, NEXT_MUSIC = 0x0113, CMD = 0x0110;
// zero page
const F = 0xF0, OFS = 0xF1, FEND = 0xF2, OP = 0xF3, PTR = 0xF5, TMP = 0xF7, SAVEX = 0xFA, CHN = 0xFB, DIRTY = 0xFC,
  OPCNT = 0xFD, FADE = 0xFE, DRUM = 0xFF;
// ROM tables (bank 24, $8000 window)
const OPCODE_TABLE = 0x8000, CHN_TYPE = 0x87A6, CHN_REG = 0x87AC, CHN_MASK = 0x87B2, PERIOD_LO = 0x87B8,
  PERIOD_HI = 0x8838, ENV_TONAL = 0x88B8, ENV_NOISE = 0x89B8, DURATION = 0x8AB8, VOLUME = 0x8AD8, DMC = 0x8BD8,
  MUSIC_TABLE = 0x8C0C, DETUNE = 0x83DA, OCTAVE = 0x842D, VIB_STEP = 0x8714, VIB_OFS = 0x8724;

const rom = (m, a) => cpuRd(m, a & 0xFFFF);
const ptrY = (m, y) => cpuRd(m, (m.rd(PTR) | m.rd(PTR + 1) << 8) + (y & 0xFF));
const or = (m, a, v) => m.wr(a, m.rd(a) | v);
const and = (m, a, v) => m.wr(a, m.rd(a) & v);
const inc = (m, a) => m.wr(a, (m.rd(a) + 1) & 0xFF);
const dec = (m, a) => m.wr(a, (m.rd(a) - 1) & 0xFF);
const type = (m, x) => rom(m, CHN_TYPE + x);
const regOf = (m, x) => rom(m, CHN_REG + x);

/** SndInit 24:8020 */
export function sndInit(m) {
  m.wr(F, 1);
  m.wr(MUSIC_ID, 0xFF); m.wr(SFX_PRI, 0xFF);
  for (let x = 0; x < 6; x++) m.wr(FLAGS + x, 0);
  m.wr(CHN, 0); m.apuWrite(0x4015, 0);
  m.wr(F, 0);
}

/** SndChannelOff 24:8465 */
function channelOff(m, x) {
  if (m.rd(FLAGS + x) & 0x80) return;
  const save = m.rd(DRUM);
  m.wr(DRUM, save & 0x10);
  m.apuWrite(0x4015, rom(m, CHN_MASK + x) | m.rd(DRUM));
  m.wr(DRUM, save);
}

/** 24:8388: give a stolen channel back (b7 -> b4 rest) */
function unsteal(m, x) {
  const f = m.rd(FLAGS + x);
  if (f & 0x80) m.wr(FLAGS + x, (f & 0x7F) | 0x10);
}

/** SndInitChannel 24:8132 (A = stream ptr hi); returns X+1 */
function initChannel(m, x, hi) {
  m.wr(PTR_HI + x, hi);
  m.wr(WAIT + x, 1); m.wr(LEN + x, 1);
  m.wr(OCT + x, 0);
  const y = regOf(m, x);
  m.apuWrite(0x4000 + y, 0); m.apuWrite(0x4002 + y, 0); m.apuWrite(0x4003 + y, 0);
  m.wr(REG0 + x, 0); m.wr(PER_LO + x, 0); m.wr(PER_HI + x, 0);
  m.apuWrite(0x4001 + y, 8); m.wr(SWEEP + x, 8);
  m.wr(FLAGS + x, 1);
  channelOff(m, x);
  return x + 1;
}

/** SndGetMusicHeader 24:8177: $F5/$F6 = SndMusicTable[id]; true if null */
function musicHeader(m, id) {
  const i = (id << 1) & 0xFF;
  m.wr(PTR, rom(m, MUSIC_TABLE + i)); m.wr(PTR + 1, rom(m, MUSIC_TABLE + 1 + i));
  return (m.rd(PTR) | m.rd(PTR + 1)) === 0;
}

/** SndStartSfx 24:8094 (A = command) */
function startSfx(m, a) {
  m.prgR7 = 0x0C;                                          // $8000 = 7, $8001 = 12 (no shadow update)
  const i = (a << 1) & 0xFF;
  m.wr(PTR, rom(m, 0x9FC0 + i)); m.wr(PTR + 1, rom(m, 0x9FC1 + i));
  if ((m.rd(PTR) | m.rd(PTR + 1)) === 0) return;
  if (m.rd(F) & 0x40) return;
  const pri = ptrY(m, 0);
  if (pri !== m.rd(SFX_PRI) && pri > m.rd(SFX_PRI)) return;
  m.wr(SFX_PRI, pri);
  m.wr(CHN, 0x0A);
  let x = 4;
  for (const y of [1, 3]) {                                // SndInitSfxChannel 24:8169
    m.wr(PTR_LO + x, ptrY(m, y));
    x = initChannel(m, x, (ptrY(m, y + 1) + 0x20) & 0xFF);
  }
  or(m, FLAGS + 1, 0x80); or(m, FLAGS + 3, 0x80);
}

/** SndStartMusic 24:80E4 (A = command < $20) */
function startMusic(m, a) {
  if (musicHeader(m, a)) return;
  if (m.rd(F) & 0x80) return;
  m.wr(FADE, 0); m.wr(DRUM, 0);                            // 24:8103
  m.wr(MUSIC_ID, m.rd(CMD) & 0x7F);
  and(m, F, 0xDF);
  let x = 0;
  for (const y of [1, 3, 5, 7]) {                          // 24:812A
    m.wr(PTR_LO + x, ptrY(m, y));
    x = initChannel(m, x, ptrY(m, y + 1));
  }
  const c = m.rd(CHN);
  if (c & 0x02) m.wr(FLAGS + 1, 0x81);
  if (c & 0x08) m.wr(FLAGS + 3, 0x81);
}

/** SndCommand 24:8048: command in $0110 */
export function sndCommand(m) {
  or(m, F, 0x01);
  const cmd = m.rd(CMD);                                   // 24:8058
  if (cmd === 0xFF) m.wr(FADE, 0xBC);
  else if (cmd & 0x80) {                                   // 24:8071 pause toggle
    if (!(m.rd(MUSIC_ID) & 0x80)) {
      m.apuWrite(0x4015, m.rd(CHN));
      m.wr(F, m.rd(F) ^ 0x20);
      if (!(m.rd(F) & 0x20)) {
        for (let x = 0; x < 4; x++) { channelOff(m, x); unsteal(m, x); }
      }
    }
  } else if (cmd >= 0x20) startSfx(m, cmd);
  else startMusic(m, cmd);
  and(m, F, 0xFE);
}

// ---- stream ops (SndOpcodeTable 24:8000); each gets x and returns nothing; X is restored by the caller ----

function opEnd(m, x) {                                     // 24:8364 $0x/$1x
  inc(m, FEND);
  channelOff(m, x);
  and(m, FLAGS + x, 0xFE);
  if (x < 4) { m.wr(NEXT_MUSIC, m.rd(OP) & 0x1F); return; }
  m.wr(CHN, rom(m, CHN_MASK + x) & m.rd(CHN));
  unsteal(m, type(m, x));
}

function sweepOp(m, x, off) {                              // 24:8395 / 83AE
  inc(m, FEND);
  or(m, DIRTY, 0x02);
  const y = m.rd(OFS); inc(m, OFS);
  const b = ptrY(m, y);
  m.wr(SWEEP + x, off ? 0 : b | 0x80);
}

function opSweep(m, x) { sweepOp(m, x, m.rd(OP) & 1); }    // 24:8395 $Cx

/** 24:83F8: period for note $F3 (+ $F7 to lo); returns the hi byte */
function notePeriod(m, x) {
  const a = m.rd(OP);
  if (type(m, x) >= 3) {
    m.wr(PER_LO + x, a & 0x0F);
    drumTrigger(m, x);
    return 0;
  }
  const y = (a & 0x0F) | m.rd(OCT + x);
  const lo = rom(m, PERIOD_LO + y) + m.rd(TMP);
  m.wr(PER_LO + x, lo & 0xFF);
  return (rom(m, PERIOD_HI + y) + (lo >> 8)) & 0xFF;
}

function noteWith(m, x, tmp) {                             // 24:83E7
  m.wr(TMP, tmp);
  inc(m, FEND);
  m.wr(PER_HI + x, notePeriod(m, x));
  or(m, DIRTY, 0x0C);
}

function opNote(m, x) { noteWith(m, x, 0); }                // 24:83E5 $2x
function opNoteSweep(m, x) {                               // 24:83E2 $Bx: JSR $83B6 enters after the FEND/dirty
  const y = m.rd(OFS); inc(m, OFS);                        // updates, so only the sweep byte is read here
  m.wr(SWEEP + x, ptrY(m, y) | 0x80);
  noteWith(m, x, 0);
}

function opNoteDetune(m, x) {                              // 24:83C2 $Dx
  if (type(m, x) === 3) { drumSelect(m, x); return; }
  noteWith(m, x, rom(m, DETUNE + (m.rd(OCT + x) >> 4)));
}

function opOctave(m, x) {                                  // 24:8422 $3x (falls into the RTS at $842D)
  m.wr(OCT + x, rom(m, OCTAVE + (m.rd(OP) & 0x0F)));
}

function opRest(m, x) {                                    // 24:8436 $4x
  inc(m, FEND);
  or(m, FLAGS + x, 0x10);
  const v = m.rd(OP) & 0x0F;
  if (v === 0) { channelOff(m, x); return; }
  m.wr(OP, v);
  if (!(x & 4) && (x & 1) && m.rd(CHN) !== 0) return;
  m.apuWrite(0x4000 + regOf(m, x), (m.rd(REG0 + x) & 0xF0) | v);
}

function opDuration(m, x) {                                // 24:847D $5x/$6x
  m.wr(LEN + x, rom(m, DURATION + ((m.rd(OP) - 0x50) & 0xFF)));
}

function opLegato(m, x) {                                  // 24:848A $7x
  m.wr(TMP, 0);
  inc(m, FEND);
  const v = m.rd(OP) & 0x0F;
  m.wr(OP, v);
  if (v === 7) return;
  const hi = notePeriod(m, x);
  const same = hi === m.rd(PER_HI + x);
  m.wr(PER_HI + x, hi);
  or(m, DIRTY, same ? 0x04 : 0x0C);
}

function opVolume(m, x) {                                  // 24:8532 $8x
  if (m.rd(FADE) !== 0) return;
  const v = m.rd(OP) & 0x0F;
  m.wr(OP, v);
  m.wr(REG0 + x, (m.rd(REG0 + x) & 0xF0) | v);
  or(m, DIRTY, 0x01);
}

function opDuty(m, x) {                                    // 24:854D $9x
  let a = (m.rd(OP) << 4) & 0xFF;
  if (type(m, x) !== 2) {
    const s = a & 0x10;
    m.wr(OP, s);
    if (!s) and(m, FLAGS + x, 0xBF);
    a = ((a << 1) & 0xC0) | s | 0x20;
  }
  m.wr(OP, a);
  m.wr(REG0 + x, (m.rd(REG0 + x) & 0x0F) | a);
  or(m, DIRTY, 0x01);
}

function loopEnd(m, x) {                                   // 24:859B (x or x+6)
  dec(m, L1_CNT + x);
  if (m.rd(L1_CNT + x) === 0) return;
  m.wr(PTR, m.rd(L1_LO + x)); m.wr(PTR + 1, m.rd(L1_HI + x));
  m.wr(OFS, 0);
}

function loopStart(m, x, y) {                              // 24:85B2 (x or x+6)
  m.wr(L1_CNT + x, ptrY(m, y));
  inc(m, OFS);
  const lo = m.rd(OFS) + m.rd(PTR);
  m.wr(L1_LO + x, lo & 0xFF);
  m.wr(L1_HI + x, (m.rd(PTR + 1) + (lo >> 8)) & 0xFF);
}

function opFlow(m, x) {                                    // 24:8584 $Ax
  let y = m.rd(OFS);
  const n = m.rd(OP) & 0x0F;
  if (n === 0) {                                           // 24:85C9 call: return address -> loop-2 slot
    const lo = ptrY(m, y); y = (y + 1) & 0xFF;
    let hi = ptrY(m, y); y = (y + 1) & 0xFF;
    m.wr(L2_CNT + x, y);
    const s = y + m.rd(PTR);
    m.wr(L2_LO + x, s & 0xFF);
    m.wr(OFS, 0);
    m.wr(L2_HI + x, (m.rd(PTR + 1) + (s >> 8)) & 0xFF);
    if (x >= 4) hi = (hi + 0x20) & 0xFF;                   // ADC #$1F with C = 1
    m.wr(PTR + 1, hi); m.wr(PTR, lo);
  } else if (n === 4) loopEnd(m, x);
  else if (n === 1) loopStart(m, x, y);
  else if (n === 2) loopStart(m, x + 6, y);
  else loopEnd(m, x + 6);
}

function opEnvelope(m, x) {                                // 24:85F7 $Ex
  m.wr(ENV + x, (m.rd(OP) << 4) & 0xFF);
  or(m, FLAGS + x, 0x40);
}

function opVibrato(m, x) {                                 // 24:8685 $Fx
  const v = (m.rd(OP) & 0x0F) << 3;
  m.wr(VIB + x, v);
  if (v < 0x40) or(m, FLAGS + x, 0x20);
  else if (v === 0x40) and(m, FLAGS + x, 0xDF);
  else inc(m, FEND);
}

function drumSelect(m, x) {                                // 24:863B
  if (x & 4) return;
  const v = m.rd(OP) & 0x0F;
  if (v === 0) { m.wr(DRUM, 0); m.apuWrite(0x4013, 0); return; }
  m.wr(DRUM, v | 0x10);
}

function drumTrigger(m, x) {                               // 24:8651
  if (x & 4) return;
  const d = m.rd(DRUM) & 0x0F;
  if (d === 0) { m.wr(DRUM, 0); return; }
  const y = d << 2;
  m.apuWrite(0x4013, 0);
  m.apuWrite(0x4015, 0x0F);
  for (let i = 0; i < 4; i++) m.apuWrite(0x4010 + i, rom(m, DMC + y + i));
}

// handler address (as stored in SndOpcodeTable) -> port
const OP_BY_ADDR = {
  0x8364: opEnd, 0x83E5: opNote, 0x8422: opOctave, 0x8436: opRest, 0x847D: opDuration, 0x848A: opLegato,
  0x8532: opVolume, 0x854D: opDuty, 0x8584: opFlow, 0x83E2: opNoteSweep, 0x8395: opSweep,
  0x83C2: opNoteDetune, 0x85F7: opEnvelope, 0x8685: opVibrato,
};

/** SndReadOp 24:8347 */
function readOp(m, x) {
  inc(m, OPCNT);
  const y = m.rd(OFS);
  const a = ptrY(m, y);
  inc(m, OFS);
  m.wr(OP, a);
  const t = (a >> 3) & 0x1E;
  const addr = rom(m, OPCODE_TABLE + t) | rom(m, OPCODE_TABLE + t + 1) << 8;
  m.wr(0xF8, addr & 0xFF); m.wr(0xF9, addr >> 8);
  const h = OP_BY_ADDR[addr];
  if (!h) throw new Error(`SndReadOp: no port for handler $${addr.toString(16)}`);
  h(m, x);
}

/** SndEnvelopeTick 24:84AF */
function envelopeTick(m, x) {
  if (m.rd(FLAGS + x) & 0x80) return;
  for (let guard = 0; guard < 4096; guard++) {
    const e = m.rd(ENV + x);
    let a;
    if ((e & 0x0F) !== 0x0F) {
      inc(m, ENV + x);
      a = rom(m, (type(m, x) === 3 ? ENV_NOISE : ENV_TONAL) + e);
      m.wr(OP, a & 0xCF);
      if (!(a & 0x10)) {                                   // 24:84F1
        const v = m.rd(OP) & 0x0F;
        m.wr(OP, v);
        and(m, DIRTY, 0xFE);                               // LSR $FC / ASL $FC
        const y = ((m.rd(REG0 + x) << 4) | v) & 0xFF;
        let out;
        if (a & 0xC0) {
          m.wr(OP, a & 0xC0);
          out = (m.rd(REG0 + x) & 0x30) | m.rd(OP) | rom(m, VOLUME + y);
        } else out = (m.rd(REG0 + x) & 0xF0) | rom(m, VOLUME + y);
        m.apuWrite(0x4000 + regOf(m, x), out);
        return;
      }
    }
    if (m.rd(FADE) === 0) { or(m, ENV + x, 0x0F); return; }   // 24:84DB
    dec(m, ENV + x); dec(m, ENV + x);                          // fade: back up 2 steps, re-read
  }
  throw new Error('SndEnvelopeTick: fade back-up loop did not end');
}

/** SndVibratoTick 24:86AA */
function vibratoTick(m, x) {
  const f = m.rd(FLAGS + x);
  if (f & 0x80) return;
  if ((f & 0x40) && (m.rd(ENV + x) & 0x0F) !== 0x0F) return;
  inc(m, OPCNT);
  const ph = (m.rd(VIB + x) + 1) & 0x07;
  m.wr(OP, ph);
  const v = (m.rd(VIB + x) & 0xF8) | ph;
  m.wr(VIB + x, v);
  if (v & 0x08) m.wr(OP, m.rd(OP) >> 1);
  m.wr(OP, m.rd(OP) & 0x03);
  let y = ((v >> 2) & 0xFC) | m.rd(OP);
  const e = rom(m, VIB_STEP + y);
  if (e !== 0) y = ((((e + 0xFE) & 0xFF) | m.rd(OCT + x)) + 4) & 0xFF;
  else y = 0;
  const lo = rom(m, VIB_OFS + y) + m.rd(PER_LO + x);
  m.wr(OP, lo & 0xFF);
  const hi = (rom(m, VIB_OFS + 1 + y) + m.rd(PER_HI + x) + (lo >> 8)) & 0xFF;
  if (hi !== m.rd(PER_HI + x)) return;
  m.apuWrite(0x4002 + regOf(m, x), m.rd(OP));
}

/** SndFadeStep 24:8609 */
function fadeStep(m) {
  for (let y = 0; y < 4; y++) {
    if (!(m.rd(FLAGS + y) & 1)) continue;
    const r0 = m.rd(REG0 + y);
    if (y === 2) { m.wr(FLAGS + 2, 0); m.wr(REG0 + 2, 0); return; }
    if (r0 & 0x0F) m.wr(REG0 + y, (r0 - 1) & 0xFF);
  }
}

/** SndChannelTick 24:8276 */
function channelTick(m, x) {
  if (!(m.rd(FLAGS + x) & 1)) return;
  m.wr(DIRTY, 0); m.wr(FEND, 0); m.wr(OFS, 0);
  dec(m, WAIT + x);
  if (m.rd(WAIT + x) === 0) {
    m.wr(SAVEX, x);
    m.wr(PTR, m.rd(PTR_LO + x)); m.wr(PTR + 1, m.rd(PTR_HI + x));
    for (let guard = 0; ; guard++) {
      if (guard > 100000) throw new Error(`SndChannelTick ${x}: stream never ends a frame`);
      readOp(m, x);
      if (m.rd(FEND) !== 0) break;
    }
    m.wr(WAIT + x, m.rd(LEN + x));
    const lo = m.rd(PTR) + m.rd(OFS);
    m.wr(PTR_LO + x, lo & 0xFF);
    m.wr(PTR_HI + x, (m.rd(PTR + 1) + (lo >> 8)) & 0xFF);
  }
  m.apuWrite(0x4015, m.rd(DRUM) | 0x0F);                   // 24:82B3
  if (m.rd(DIRTY) & 0x08) {
    and(m, ENV + x, 0xF0); and(m, VIB + x, 0xF8); and(m, FLAGS + x, 0xEF);
  }
  if (!(m.rd(FLAGS + x) & 0x10)) {
    if (m.rd(FLAGS + x) & 0x40) envelopeTick(m, x);
    if (m.rd(FLAGS + x) & 0x20) vibratoTick(m, x);
  }
  const fd = m.rd(FADE);                                   // 24:82F3
  if (fd !== 0) {
    if (fd & 0x3F) dec(m, FADE);
    else { fadeStep(m); m.wr(FADE, 0xBC); }
  }
  if (m.rd(FLAGS + x) & 0x80) return;                      // 24:8307
  const y = regOf(m, x);
  const d = m.rd(DIRTY);
  m.wr(DIRTY, d >> 4);                                     // four LSR $FC
  if (d & 1) m.apuWrite(0x4000 + y, m.rd(REG0 + x));
  if (d & 2) m.apuWrite(0x4001 + y, m.rd(SWEEP + x));
  if (d & 4) m.apuWrite(0x4002 + y, m.rd(PER_LO + x));
  if (d & 8) {
    m.apuWrite(0x4001 + y, m.rd(SWEEP + x));
    m.wr(SWEEP + x, (m.rd(SWEEP + x) & 0x7F) | 0x08);
    m.apuWrite(0x4003 + y, m.rd(PER_HI + x) | 0x08);
  }
}

/** SndTick 24:8186 */
export function sndTick(m) {
  let a = m.rd(F);
  if (a & 1) return;
  m.wr(OPCNT, 0);
  a = (a & 0xFE) | 1;
  m.wr(F, a); m.wr(OP, a);
  if (a & 0x80) {                                          // music stop request
    and(m, F, 0xDF);
    if (!(m.rd(MUSIC_ID) & 0x80)) { m.wr(MUSIC_ID, 0xFF); m.apuWrite(0x4015, m.rd(CHN)); }
  }
  m.wr(OP, (a << 1) & 0xFF);                               // 24:81AE ASL $F3
  if ((a & 0x40) && !(m.rd(SFX_PRI) & 0x80)) {             // SFX stop request
    m.wr(SFX_PRI, 0xFF);
    m.apuWrite(0x4015, m.rd(CHN) ^ 0x0F);
    m.wr(CHN, 0);
    unsteal(m, 1); unsteal(m, 3);
  }
  if (!(m.rd(MUSIC_ID) & 0x80) && !(m.rd(F) & 0x20)) musicTick(m);
  if (!(m.rd(SFX_PRI) & 0x80) && m.rd(OPCNT) < 0x14) {     // 24:8258
    m.prgR7 = 0x0C;
    channelTick(m, 4); channelTick(m, 5);
    if (m.rd(CHN) === 0) m.wr(SFX_PRI, 0xFF);
  }
  and(m, F, 0xFE);
}

/** 24:81F3: music channels 0-3, then the $0113 chain when all four have ended */
function musicTick(m) {
  if (m.apuRead(0x4015) & 0x10) m.apuWrite(0x4013, 0);
  for (let x = 0; x < 4; x++) channelTick(m, x);
  if ((m.rd(FLAGS) | m.rd(FLAGS + 1) | m.rd(FLAGS + 2) | m.rd(FLAGS + 3)) & 1) return;
  const next = m.rd(NEXT_MUSIC);
  m.wr(CMD, next);
  if (next === 0 || musicHeader(m, next)) { m.wr(MUSIC_ID, 0xFF); return; }
  let y = 1;
  for (let x = 0; x < 4; x++) {
    m.wr(PTR_LO + x, ptrY(m, y)); y++;
    m.wr(PTR_HI + x, ptrY(m, y)); y++;
    or(m, FLAGS + x, 1);
    m.wr(WAIT + x, 1);
  }
}
