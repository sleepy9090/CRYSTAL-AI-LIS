// re-c web port — bank 26 object helpers (Ph7 web 6/8, s55). REVERSE.md "Enemies — catalogue + generic enemy
// AIs", "World interaction", "Shots". Ported instruction by instruction: every zero-page temp the ROM writes is
// written here too, so web/test/obj_check.mjs can diff the whole RAM + WRAM against tools.emu.
//
//   rand64(m)                 Rand64 26:97D7 -> value
//   objFindFree(m, y)         ObjFindFree 26:98B9 (end slot $10) -> free slot or -1 (C = 0)
//   objPosToTemp / tempToObjPos (26:98A8 / 26:9897), tempAddOffset 26:9824, tempAddStep 26:9861
//   dirToTarget(m, x, y)      DirToTarget 26:8409 -> $20 dir, $21 distance
//   objStepVector(m, x, dir)  ObjStepVector 26:8480 -> $30 dx, $31 dy
//   objDespawn / objBecomeNext (26:98C7 / 26:98CD)
//   spawnShot(m, x, id, dir)  SpawnShot 26:972D -> true if at least one object spawned
//   objBgProbe(m, x, a[, n])  ObjBgProbe 26:9A30 -> C (true only for the A b7 same-cell cache hit). s111: the cache
//                             test is BPL right after STA $14, i.e. the caller's N flag; n = a & $80 unless given
//                             (PlayerMove 26:9EFD: LDY obj_timer2 sets N, so a jump timer >= $80 probes cached)
import { cpuRd, indY, setR7 } from '../prg.js';
import { farObjInit } from './objinit.js';

const X_LO = 0x70, X_HI = 0x90, Y_LO = 0xB0, Y_HI = 0xD0;
const FLAGS2 = 0x0340, META_DIR = 0x0360, FLAGS = 0x0380, BG_MASK = 0x0460, TIMER = 0x0480, ACTIVE = 0x04A0;
const DEATH = 0x04C0, BEHAV = 0x06E0;
const P_XL = 0x34, P_XH = 0x35, P_YL = 0x36, P_YH = 0x37;

export function rand64(m) {
  m.wr(0x0E, (m.rd(0x0E) + 1) & 0x3F);
  return cpuRd(m, 0x97E4 + m.rd(0x0E));
}

export function objFindFree(m, y) {
  for (;;) {
    if (m.rd(ACTIVE + y) === 0) return y;
    y = (y + 1) & 0xFF;
    if (y === m.rd(0x10)) return -1;
  }
}

// zero-page,X: the slot position columns wrap inside page 0 (s104: a garbage SpawnShot record gave slot $42 and
// STA $D0,X wrote $12)
const zp = (a) => a & 0xFF;

export function objPosToTemp(m, x) {
  m.wr(P_XL, m.rd(zp(X_LO + x))); m.wr(P_XH, m.rd(zp(X_HI + x)));
  m.wr(P_YL, m.rd(zp(Y_LO + x))); m.wr(P_YH, m.rd(zp(Y_HI + x)));
}

export function tempToObjPos(m, x) {
  m.wr(zp(X_LO + x), m.rd(P_XL)); m.wr(zp(X_HI + x), m.rd(P_XH));
  m.wr(zp(Y_LO + x), m.rd(P_YL)); m.wr(zp(Y_HI + x), m.rd(P_YH));
}

function addX(m) {                       // shared x part of 9824 / 9861: signed $30 to $34/$35
  const dx = m.rd(0x30), s = m.rd(P_XL) + dx;
  m.wr(P_XL, s);
  if (dx & 0x80) { if (s < 0x100) m.wr(P_XH, m.rd(P_XH) - 1); } else if (s >= 0x100) m.wr(P_XH, m.rd(P_XH) + 1);
}

/** 26:9824: add signed $30/$31 to $34-$37, y wraps at 240 with the ADC carry tested first */
export function tempAddOffset(m) {
  addX(m);
  const dy = m.rd(0x31), s = dy + m.rd(P_YL), r = s & 0xFF;
  if (!(dy & 0x80)) {
    if (s >= 0x100 || r >= 0xF0) { m.wr(P_YH, m.rd(P_YH) + 1); m.wr(P_YL, r + 0x10); } else m.wr(P_YL, r);
  } else if (s < 0x100 || r >= 0xF0) { m.wr(P_YL, r + 0xF0); m.wr(P_YH, m.rd(P_YH) - 1); } else m.wr(P_YL, r);
}

/** TempAddStep 26:9861: like 9824, but y only tests the sum against $F0 (the ADC carry is ignored) */
export function tempAddStep(m) {
  addX(m);
  const dy = m.rd(0x31), r = (dy + m.rd(P_YL)) & 0xFF;
  if (r < 0xF0) { m.wr(P_YL, r); return; }
  if (!(dy & 0x80)) { m.wr(P_YL, r + 0x10); m.wr(P_YH, m.rd(P_YH) + 1); } else { m.wr(P_YL, r + 0xF0); m.wr(P_YH, m.rd(P_YH) - 1); }
}

export function dirToTarget(m, x, ty) {
  let d = m.rd(X_LO + ty) - m.rd(X_LO + x);
  m.wr(0x10, d);
  let h = (m.rd(X_HI + ty) - m.rd(X_HI + x) - (d < 0 ? 1 : 0)) & 0xFF;
  m.wr(0x11, h);
  let a;
  if (((h & 0x80) ? h ^ 0xFF : h) === 0) {                  // 8422: ($11:$10) >> 4
    a = m.rd(0x10);
    for (let i = 0; i < 4; i++) {
      const v = m.rd(0x11), c = v & 1;
      m.wr(0x11, v >> 1);
      a = (a >> 1) | (c << 7);
    }
  } else a = h;                                             // 841E: |dx| >= 256 -> the hi byte is the cell
  m.wr(0x10, (a + 0x10) & 0x1F);
  d = m.rd(Y_LO + ty) - m.rd(Y_LO + x);
  m.wr(0x11, d);
  h = (m.rd(Y_HI + ty) - m.rd(Y_HI + x) - (d < 0 ? 1 : 0)) & 0xFF;
  let y = h;
  if (((h & 0x80) ? h ^ 0xFF : h) !== 0) {                  // 844B
    m.wr(0x11, h << 4);
    y = h >> 4;
  }
  a = m.rd(0x11) & 0xF0;                                    // 8458
  const c = a >> 7;
  m.wr(0x10, ((a << 1) & 0xFF) | m.rd(0x10));
  m.wr(0x11, ((((y << 1) | c) + 2) & 3) + 0x80);
  const v = indY(m, 0x10, 0);
  m.wr(0x20, v & 0x0F);
  m.wr(0x21, v >> 4);
}

export function objStepVector(m, x, dir) {
  m.wr(0x12, dir << 3);
  let y = m.rd(FLAGS2 + x) & 0x0F;
  if (m.rd(FLAGS + x) & 0x40) {                             // 848E: sinking -> speed - 2, floor 0
    y = (y - 2) & 0xFF;
    if (y & 0x80) y = 0;
  }
  m.wr(0x10, cpuRd(m, 0x84BC + y));
  m.wr(0x11, cpuRd(m, 0x84CC + y));
  m.wr(0x13, cpuRd(m, 0x84DC + y));
  y = (m.rd(TIMER + x) & 7) | m.rd(0x12);
  m.wr(0x31, indY(m, 0x10, y));
  y = (y + m.rd(0x13)) & 0xFF;
  m.wr(0x30, indY(m, 0x10, y));
}

export function objDespawn(m, x) { m.wr(ACTIVE + x, 0); }

export function objBecomeNext(m, x) {
  m.wr(0x11, m.rd(DEATH + x));
  m.wr(0x10, x);
  farObjInit(m);
}

export function spawnShot(m, x, id, dir) {
  m.wr(0x14, dir);
  const yy = (id << 2) & 0xFF, page = id & 0x40 ? 0xBD00 : 0xBC00;
  const saved = m.rd(0x6F);
  setR7(m, 0x14);                                           // 9735
  m.wr(0x23, 0);
  m.wr(0x21, cpuRd(m, page + yy)); m.wr(0x22, cpuRd(m, page + yy + 1));
  m.wr(0x11, cpuRd(m, page + yy + 2)); m.wr(0x20, cpuRd(m, page + yy + 3));
  if (m.rd(0x21) & 0x80) { setR7(m, saved); return false; } // 976D
  do {
    m.wr(0x10, m.rd(0x22));                                 // 9773
    const y = objFindFree(m, m.rd(0x21));
    if (y < 0) break;
    m.wr(0x10, y);
    farObjInit(m);                                          // 9780
    objPosToTemp(m, x);
    const f = m.rd(FLAGS + x);
    m.wr(0x12, f);
    const n = m.rd(0x10);
    m.wr(FLAGS + n, m.rd(FLAGS + n) | (f & 0x80));
    m.wr(0x31, f & 0x40 ? 2 : 0);
    const d = m.rd(0x14);
    m.wr(META_DIR + n, d);
    m.wr(0x13, (d << 1) & 0x0F);
    const oy = ((m.rd(BEHAV + n) << 4) & 0xFF) | m.rd(0x13);  // 97AB: the NEW object's obj_behav
    m.wr(0x30, cpuRd(m, 0xBD80 + oy));
    m.wr(0x31, cpuRd(m, 0xBD81 + oy) + m.rd(0x31));
    tempAddOffset(m);
    tempToObjPos(m, n);
    m.wr(0x23, m.rd(0x23) - 1);
    m.wr(0x20, m.rd(0x20) - 1);
  } while (m.rd(0x20) !== 0);
  setR7(m, saved);                                          // 97D0
  const t = m.rd(0x23);
  m.wr(0x23, t << 1);
  return (t & 0x80) !== 0;
}

export function objBgProbe(m, x, a, n = a & 0x80) {
  m.wr(0x14, a);                                            // STA $14 keeps the flags: BPL tests the caller's N
  if (n &&!((m.rd(P_YL) ^ m.rd(Y_LO + x)) & 0xF0) && !((m.rd(P_XL) ^ m.rd(X_LO + x)) & 0xF0)) {
    m.wr(0x20, m.rd(FLAGS + x) & 0x50);                     // 9A44 same 16 px cell: cached
    return true;
  }
  const gi = ((m.rd(P_YH) << 3) | m.rd(P_XH)) & 0xFF;      // 9A4D
  const g = m.rd(0x6300 + gi);
  m.wr(0x11, (g & 0x1F) | 0xA0);
  setR7(m, g >> 5);                                         // 9A67 (inline: $6F, $50 = 7, R7)
  m.wr(0x12, m.rd(P_YL) & 0xF0);
  m.wr(0x10, (m.rd(P_XL) >> 4) | m.rd(0x12));
  m.wr(0x68, indY(m, 0x10, 0));
  setR7(m, 9);                                              // 9A89
  m.wr(0x20, indY(m, 0x68, 0));
  if (m.rd(0x20) & 8 && (m.rd(0x62F0 + m.rd(P_YH)) & cpuRd(m, 0x9AE0 + m.rd(P_XH)))) {   // 9A9D visited subst
    m.wr(0x68, indY(m, 0x6A, m.rd(0x68)));
    m.wr(0x20, indY(m, 0x68, 0));
  }
  setR7(m, 0x1B);                                           // 9AB9
  if (m.rd(0x14) & 0x40) {
    const f = m.rd(FLAGS + x) & 0xAF;
    m.wr(0x10, f);
    m.wr(FLAGS + x, (m.rd(0x20) & m.rd(BG_MASK + x) & 0x50) | f);
  }
  return false;
}
