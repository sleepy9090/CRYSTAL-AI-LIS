// re-c web port — area spawn entries (s112, gameplay task (e1)). REVERSE.md "Spawn entry" / "NpcHidden".
//
//   areaSpawnEntry(m, x, y)   AreaSpawnEntry 31:E1B6: the 4-byte entry at ($10),Y for slot X -> $2C-$2F,
//                             position, then SpawnHandlerTable 31:E218[b2 & 7] (handlers 0-4). Returns Y + 4.
//                             $10/$11 are pushed before the handler and restored by SpawnCommonTail 31:E28E.
//   npcHidden(m)              NpcHidden 14:80A0 ($23 = param -> $20 != 0: NPC absent)
//   chestPresence(m)          ChestPresence 14:85BF ($23 = chest param -> $20 = 1 if any condition true)
//   npcFlagTest(m, y)         NpcFlagTest 14:8135 / NpcFlagRef 14:8148 on the 2-byte ref at ($24),Y
//
// Every RAM write is the ROM's, in order (temps included: $12-$15, $20-$28, $2C-$2F). Bank 14 code runs with
// SwitchBank16(7), so its tables are read through cpuRd like the 6502 does.
import { cpuRd, indY, switchBank16 } from '../prg.js';
import { objInit } from '../obj/objinit.js';

const Y_LO = 0xB0, Y_HI = 0xD0, X_LO = 0x70, X_HI = 0x90;
const ACTIVE = 0x04A0, BEHAV = 0x06E0, FLAGS = 0x0380, META = 0x0300, TILE_OFS = 0x0320;
const PARAM1 = 0x0680, PARAM2 = 0x06A0, PHASE = 0x0600, ORBIT = 0x06C0, FREEZE = 0x05A0;
const SPAWN_B2 = 0x0540, SPAWN_P = 0x0560, FLAGS_WRAM = 0x6480;
const H1_PHASE = [0xDA, 0x22, 0x4A, 0xAA];                 // 31:E35F

export function areaSpawnEntry(m, x, y) {
  for (let i = 0; i < 4; i++) m.wr(0x2C + i, indY(m, 0x10, (y + i) & 0xFF));
  y = (y + 4) & 0xFF;
  const b0 = m.rd(0x2C), b1 = m.rd(0x2D), b2 = m.rd(0x2E);
  m.wr(Y_HI + x, b0 >> 4);
  m.wr(Y_LO + x, ((b0 << 4) | 0x0C) & 0xFF);
  m.wr(0x20, (b2 & 0x40) >> 3);
  m.wr(X_HI + x, (b1 >> 4) & 7);
  m.wr(X_LO + x, ((((b1 << 4) | 8) & 0xFF) + m.rd(0x20)) & 0xFF);   // SEC ROL, ASL x3, CLC ADC $20
  m.wr(0x14, y);
  const h = b2 & 7;
  if (h > 4) throw new Error(`AreaSpawnEntry: handler ${h} (code bytes past SpawnHandlerTable 31:E218)`);
  m.wr(0x12, cpuRd(m, 0xE218 + h * 2)); m.wr(0x13, cpuRd(m, 0xE219 + h * 2));
  const s10 = m.rd(0x10), s11 = m.rd(0x11);                // PHA $10, PHA $11
  HANDLERS[h](m, x);
  // SpawnCommonTail 31:E28E
  m.wr(SPAWN_B2 + x, m.rd(0x2E) & 3);
  m.wr(SPAWN_P + x, m.rd(0x2F));
  m.wr(0x11, s11); m.wr(0x10, s10);
  return m.rd(0x14);
}

function init(m, x, type) {                                // STA $11 / STX $10 / JSR ObjInit
  m.wr(0x11, type); m.wr(0x10, x);
  objInit(m);
}

/** SpawnSetTileHi 31:E3C7: obj_flags |= (b2 & $80) >> 2 */
function setTileHi(m, x) {
  m.wr(FLAGS + x, ((m.rd(0x2E) & 0x80) >> 2) | m.rd(FLAGS + x));
}

/** SpawnH0 31:E27F: type = p + $50 */
function spawnH0(m, x) {
  init(m, x, (m.rd(0x2F) + 0x50) & 0xFF);
  setTileHi(m, x);
}

/** SpawnH3 31:E2A3: type = p + $D0 */
function spawnH3(m, x) {
  init(m, x, (m.rd(0x2F) + 0xD0) & 0xFF);
  setTileHi(m, x);
}

/** SpawnH4_Item 31:E222: type = p; p & $F0 = $40 -> sprite palettes 2/3 = (area & 7) + 9 and, in areas
 *  $C2-$C4 (flag $84) / $F5-$F7 (flag $27), p = $4E once that story flag is set. */
function spawnH4(m, x) {
  if ((m.rd(0x2F) & 0xF0) === 0x40) {
    const area = m.rd(0x6C), pal = ((area & 7) + 9) & 0xFF;
    m.wr(0x07E6, pal); m.wr(0x07E7, pal);
    const f = [0xC2, 0xC3, 0xC4].includes(area) ? 0x84 : [0xF5, 0xF6, 0xF7].includes(area) ? 0x27 : -1;
    if (f >= 0) {
      m.wr(0x10, m.rd(FLAGS_WRAM + (f >> 3)));
      if (cpuRd(m, 0xC000 + (f & 7)) & m.rd(0x10)) m.wr(0x2F, 0x4E);   // BitMask 30:C000
    }
  }
  init(m, x, m.rd(0x2F));
  setTileHi(m, x);
}

/** SpawnH1_Indirect 31:E2B5: 4-byte record at banks 4/5 $8000 + (p>>2)*$100 + $F0 + (p&3)*4 -> $20-$23.
 *  rec[0] = $FF -> SpawnH1_TypeRec 31:E363 (type rec[1]); else type $30 with param1/2, behaviour, tile,
 *  phase, meta = orbit = rec[3], freeze = (X & 3) + 1. Both -> SpawnH1_Tail 31:E340 (NpcHidden). */
function spawnH1(m, x) {
  switchBank16(m, 2);
  const p = m.rd(0x2F);
  m.wr(0x24, ((p & 3) << 2) | 0xF0);
  m.wr(0x25, (p >> 2) | 0x80);
  for (let i = 3; i >= 0; i--) m.wr(0x20 + i, indY(m, 0x24, i));
  if (m.rd(0x20) === 0xFF) {                               // E363 SpawnH1_TypeRec
    switchBank16(m, 6);
    init(m, x, m.rd(0x21));
    setTileHi(m, x);
  } else {
    switchBank16(m, 6);
    init(m, x, 0x30);
    setTileHi(m, x);
    m.wr(PARAM1 + x, m.rd(0x20));
    m.wr(PARAM2 + x, m.rd(0x21));
    const r2 = m.rd(0x22);
    if (r2 & 0x80) {
      m.wr(ACTIVE + x, r2 & 0x7F);
      m.wr(BEHAV + x, r2 & 0x7F);
    } else {
      m.wr(BEHAV + x, r2);
      m.wr(TILE_OFS + x, r2 & 0x70);
    }
    if (!(m.rd(BEHAV + x) & 4)) m.wr(PHASE + x, H1_PHASE[m.rd(0x2F) & 3]);   // E311
    if (m.rd(0x22) & 8) m.wr(Y_LO + x, m.rd(Y_LO + x) | 0x0E);
    m.wr(META + x, m.rd(0x23));
    m.wr(ORBIT + x, m.rd(0x23));
    m.wr(FREEZE + x, (x & 3) + 1);
  }
  // SpawnH1_Tail 31:E340
  m.wr(0x23, m.rd(0x2F));
  m.wr(0x15, x);
  switchBank16(m, 7);
  npcHidden(m);
  switchBank16(m, 6);
  if (m.rd(0x20) !== 0) m.wr(ACTIVE + x, 0);
}

/** SpawnH2 31:E375: chest. p bit 7 -> type $0E; else type $0D, ChestPresence (none true -> obj_active = 0),
 *  and meta $4C for chest params $25/$3C/$3B. */
function spawnH2(m, x) {
  const p = m.rd(0x2F);
  if (p & 0x80) {
    init(m, x, 0x0E);
    setTileHi(m, x);
    return;
  }
  init(m, x, 0x0D);
  m.wr(0x15, x);
  switchBank16(m, 7);
  m.wr(0x23, p);
  chestPresence(m);
  switchBank16(m, 6);
  if (m.rd(0x20) === 0) m.wr(ACTIVE + x, 0);
  setTileHi(m, x);
  if (p === 0x25 || p === 0x3C || p === 0x3B) m.wr(META + x, 0x4C);
}

const HANDLERS = [spawnH0, spawnH1, spawnH2, spawnH3, spawnH4];

/** NpcFlagRef 14:8148 + NpcFlagTest 14:8135 on the ref at ($24),Y: returns [cond true, next Y]. */
export function npcFlagTest(m, y) {
  const r0 = indY(m, 0x24, y);
  m.wr(0x26, r0); m.wr(0x27, r0);
  const r1 = indY(m, 0x24, (y + 1) & 0xFF);
  y = (y + 2) & 0xFF;
  m.wr(0x28, cpuRd(m, 0x8167 + (r1 & 7)));
  const idx = ((r1 >> 3) | ((r0 & 7) << 5)) & 0xFF;         // LSR $27 / ROR A x3
  m.wr(0x27, r0 >> 3);
  let v = m.rd(FLAGS_WRAM + idx);
  if (m.rd(0x26) & 0x20) v ^= 0xFF;
  return [(v & m.rd(0x28)) !== 0, y];
}

/** NpcHidden 14:80A0: per-param area list (NpcPresenceLo 14:85E0 / Hi 14:86E0 for params >= $80):
 *  `area cond.. ` groups (cond b7 = last, list ends at $FF); in a group for the current area, the first
 *  false condition sets $20 = 1 (NPC absent). */
export function npcHidden(m) {
  m.wr(0x20, 0);
  let y = 0;
  const t = (m.rd(0x23) << 1) & 0xFF, base = m.rd(0x23) & 0x80 ? 0x86E0 : 0x85E0;
  m.wr(0x24, cpuRd(m, base + t));
  m.wr(0x25, cpuRd(m, base + t + 1));
  for (;;) {
    const a = indY(m, 0x24, y);
    y = (y + 1) & 0xFF;
    if (a === 0xFF) return;
    if (a === m.rd(0x6C)) {
      for (;;) {
        const [ok, ny] = npcFlagTest(m, y);
        y = ny;
        if (!ok) { m.wr(0x20, m.rd(0x20) + 1); return; }
        if (m.rd(0x26) & 0x80) break;                      // last condition: next area group
      }
    } else {
      for (;;) {                                           // 80D9: skip the group's 2-byte conditions
        const c = indY(m, 0x24, y);
        y = (y + 2) & 0xFF;
        if (c & 0x80) break;
      }
    }
  }
}

/** ChestPresence 14:85BF: $20 = 1 if any condition of ChestPresencePtrs 14:9C82[$23] is true. */
export function chestPresence(m) {
  m.wr(0x20, 0);
  const t = (m.rd(0x23) << 1) & 0xFF;
  m.wr(0x24, cpuRd(m, 0x9C82 + t));
  m.wr(0x25, cpuRd(m, 0x9C83 + t));
  let y = 0;
  for (;;) {
    const [ok, ny] = npcFlagTest(m, y);
    y = ny;
    if (ok) { m.wr(0x20, m.rd(0x20) + 1); return; }
    if (m.rd(0x26) & 0x80) return;
  }
}
