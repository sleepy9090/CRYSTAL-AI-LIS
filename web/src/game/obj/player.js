// re-c web port — the player's two objects (s111, "Web fix: gameplay frames" (e2)). REVERSE.md "Player —
// movement, jump, sword, magic" (s44/s45/s76) and "World interaction". Ported instruction by instruction
// (zero-page temps included) so RAM diffs against tools.emu stay byte-exact.
//
//   AI $03 playerMove       PlayerMove 26:9D9A (slot 0, ground body): pad -> obj_behav, jump timer $0620,
//                           PlayerMagic 27:A022, sword charge, swing pose, ObjMoveSlide + corner slide,
//                           HudChargeMeter 26:8D59 every 4th frame, BG probe, low-obstacle freeze, sink,
//                           PlayerConveyor 26:9FD2 and the obj_aux ramp
//   AI $02 playerMoveLocked PlayerMoveLocked 26:9D81 (pads zeroed; area $58 -> AI $03 + pad_dir 4)
//   AI $01 playerUpdate     PlayerUpdate 26:9B45 (slot 1, drawn body): item-use menu, PlayerSetSpeed 26:9BF6,
//                           PlayerPlaceBody 26:9CEF, PlayerFacing 27:A1C9, status poses, shield overlay,
//                           slash shots ($04-$0F) and the charged shot at obj_phase $10
//
// SpendMp 26:9AE8 is spendMp() below: false = MP short, sub_state $1C, and the ROM's PLA PLA returns from the
// routine that JSR'd SpendMp's caller — every call site below returns at once on false.
// PlaySound's Y argument (saved in snd_saved_y $010F) is the real Y at each call where it is known; after a
// cast PlayerMagic's exit Y is only approximated (sel*2), so a sword-charge sound at level 16/24 on the same
// frame as a cast can leave a different $010F.
import { cpuRd } from '../prg.js';
import { objMoveSlide } from './move.js';
import { objPosToTemp, tempToObjPos, tempAddOffset, objBgProbe, spawnShot } from './util.js';
import { playSound } from '../playsound.js';
import { hudRedraw, hudHpBar } from '../states/death.js';
import { hudChargeMeter } from '../states/saveload.js';

const META = 0x0300, FLAGS2 = 0x0340, META_DIR = 0x0360, FLAGS = 0x0380, COLL = 0x03A0, BG_MASK = 0x0460;
const TIMER = 0x0480, ACTIVE = 0x04A0, OVERLAY = 0x0580, PHASE = 0x0600, TIMER2 = 0x0620, ACT_TIMER = 0x0640;
const AUX = 0x0660, PARAM1 = 0x0680, PARAM2 = 0x06A0, BEHAV = 0x06E0;
const FLASH = 0x0710, SWORD = 0x0711, MAGIC_SEL = 0x0712, SHIELD = 0x0714, USE_ITEM = 0x0715, ACCESSORY = 0x0716;
const MP = 0x0708, HP_MAX = 0x03C0, HP = 0x03C1, ORBIT = 0x06C0, ORBIT_BURST = 0x06C1, SWORD_POWER = 0x0719;
const BG_ATTR = 0x0641, SPAWN_PARAM_S01 = 0x0561, META_S01 = 0x0301, ANIM = 0x04E0, ANIM_S01 = 0x04E1;
const PAD_PREV = 0x43, PAD_DIR = 0x49, PAD_NEW = 0x4B, PAD_ACT = 0x4D, SUB_STATE = 0x41, AREA = 0x6C;
const JUMP_HEIGHT = 0x9D4D;

const dec = (m, a) => m.wr(a, (m.rd(a) - 1) & 0xFF);
const inc = (m, a) => m.wr(a, (m.rd(a) + 1) & 0xFF);
const status = (m) => m.rd(FLASH) & 0x0F;

/** SpendMp 26:9AE8 (A = cost); HudRedraw(4) keeps the caller's X */
function* spendMp(m, cost, x) {
  m.wr(0x1F, cost);
  const mp = m.rd(MP);
  if (mp === 0 || mp < cost) { m.wr(SUB_STATE, 0x1C); return false; }
  m.wr(MP, mp - cost);
  yield* hudRedraw(m, 4, x);
  return true;
}

// ---------------------------------------------------------------- PlayerMagic 27:A022 (X = 0)

/** 27:A1B0: status 4 -> PLA PLA + MagicFailSound (returns from the handler); true = blocked */
function status4Fail(m, y) {
  if (status(m) !== 4) return false;
  playSound(m, 0x4E, y);
  return true;
}

/** 27:A0B3 one heal step (run twice per frame by magic 1) */
function* healStep(m, x) {
  const hp = m.rd(HP);
  if (hp === m.rd(HP_MAX + x)) return;
  const n = (hp + 1) & 0xFF;
  m.wr(HP, n);
  if (n !== m.rd(HP_MAX + x) && (n & 3)) return;
  yield* hudHpBar(m);                                       // 26:8CC0
  if (!(yield* spendMp(m, 2, x))) return;
  castShot(m, x, 0x12);
}

/** 27:A0E6: SpawnShot(A, Y = slot 1 facing * 2) by JMP */
function castShot(m, x, id) {
  m.wr(0x10, id);
  spawnShot(m, x, id, (m.rd(META_S01) & 3) << 1);
}

function outOfMp(m) { m.wr(SUB_STATE, 0x1C); }             // 27:A15C

/** returns the Y register at the RTS (approximate after a cast, see the header) */
function* playerMagic(m, x) {
  let y = m.rd(MAGIC_SEL);
  if (y === 0 && m.rd(ACCESSORY) === 0x12) y = 9;
  m.wr(0x10, y);
  y = cpuRd(m, 0xA092 + y);                                 // MagicButtonSel: $08 pad_new / $00 pad_prev
  if (!(m.rd(PAD_PREV + y) & 0x80)) return y;
  if (status(m) === 4) { playSound(m, 0x4E, y); return y; } // A043 -> A01F JMP MagicFailSound
  const sel = m.rd(0x10);
  if (sel !== 8 && sel !== 9 && m.rd(TIMER2)) { playSound(m, 0x4E, y); return y; }
  if (m.rd(FLASH) & 0x80) { m.wr(SUB_STATE, 0x19); return m.rd(FLASH); }   // A05B JMP A1AD
  y = (sel & 0x0F) << 1;
  m.wr(0x10, cpuRd(m, 0xA072 + y));                         // JmpTable27_A072 -> JMP ($0010)
  m.wr(0x11, cpuRd(m, 0xA073 + y));
  switch (sel & 0x0F) {
    case 1:                                                 // A0A3 heal
      if (status4Fail(m, y)) return y;
      if (m.rd(MP) < 2) { outOfMp(m); return y; }
      yield* healStep(m, x);
      yield* healStep(m, x);
      return y;
    case 2:                                                 // A0D7
      if (status4Fail(m, y)) return y;
      if (m.rd(0x04AC)) return y;                           // slot $0C busy
      if (!(yield* spendMp(m, 4, x))) return y;
      castShot(m, x, 0x10);
      return y;
    case 3:                                                 // A18E telepathy
      if ((m.rd(AREA) & 0xF8) === 0x58) { playSound(m, 0x4E, y); return y; }
      if (m.rd(MP) < 8) { outOfMp(m); return y; }
      m.wr(SUB_STATE, 0x15);
      return y;
    case 4:                                                 // A1A1 teleport
      if (status4Fail(m, y)) return y;
      if (m.rd(MP) < 0x14) { outOfMp(m); return y; }
      m.wr(SUB_STATE, 0x14);
      return y;
    case 5:                                                 // A17D cure
      if (status(m) === 0) return y;
      if (!(yield* spendMp(m, 0x18, x))) return y;
      m.wr(SUB_STATE, 0x18);
      return y;
    case 6:                                                 // A0F4
      if (m.rd(TIMER2)) { playSound(m, 0x4E, y); return y; }
      if (m.rd(MP) === 0) { outOfMp(m); return y; }
      if ((m.rd(0x08) & 7) === 0 && !(yield* spendMp(m, 1, x))) return y;
      castShot(m, x, 0x11);
      return y;
    case 7:                                                 // A161 change form
      if ((m.rd(AREA) & 0xF8) === 0x58) { playSound(m, 0x4E, y); return y; }
      if (status4Fail(m, y)) return y;
      if (m.rd(TIMER2 + x)) return y;
      if (m.rd(MP) < 0x14) { outOfMp(m); return y; }
      m.wr(SUB_STATE, 0x17);
      return y;
    case 8:                                                 // MagicFlight A112
      if (m.rd(FLASH) & 0x40) return y;
      if ((m.rd(0x08) & 0x0F) === 0) {
        if (!(yield* spendMp(m, 1, x))) return y;
        y = 0x18;                                           // HudRedraw(4) returns with Y = 4 * 6
      }
      if (m.rd(TIMER2 + x) === 0) {
        if (m.rd(MP) === 0) { outOfMp(m); return y; }
        m.wr(BG_MASK + x, 0x34);
        jumpStart(m, x, y);
        return y;
      }
      if (m.rd(TIMER2 + x) === 0x12) inc(m, TIMER2 + x);   // hover at the peak
      m.wr(ACT_TIMER + x, m.rd(PAD_DIR));
      return y;
    case 9:                                                 // MagicJump A143
      if (m.rd(FLASH) & 0x40) return y;
      if (m.rd(TIMER2 + x)) return y;
      jumpStart(m, x, y);
      return y;
    default:                                                // A0A2 RTS (0, 10-15)
      return y;
  }
}

/** 27:A14D */
function jumpStart(m, x, y) {
  m.wr(TIMER2 + x, 0x20);
  m.wr(ACT_TIMER + x, m.rd(PAD_DIR));
  playSound(m, 0x30, y);
}

// ---------------------------------------------------------------- PlayerMove 26:9D9A (slot 0)

/** PlayerConveyor 26:9FD2 */
function playerConveyor(m, x) {
  const sd = m.rd(META_DIR + x), sf = m.rd(FLAGS2 + x), st = m.rd(TIMER + x);
  m.wr(TIMER + x, m.rd(0x08));
  m.wr(FLAGS2 + x, m.rd(AUX + x));
  m.wr(META_DIR + x, m.rd(PARAM1 + x) & 0x0F);
  if (objMoveSlide(m, x)) m.wr(PARAM1 + x, cpuRd(m, 0xA00F + (m.rd(META_DIR + x) & 7)));   // 27:A00F
  m.wr(TIMER + x, st);
  m.wr(FLAGS2 + x, sf);
  m.wr(META_DIR + x, sd);
}

/** AI $02 PlayerMoveLocked 26:9D81 */
export function* playerMoveLocked(m, x) {
  m.wr(PAD_PREV, 0); m.wr(PAD_NEW, 0); m.wr(PAD_ACT, 0); m.wr(PAD_DIR, 0);
  if (m.rd(AREA) === 0x58) {
    m.wr(ACTIVE + x, 3);
    m.wr(PAD_DIR, 4);
  }
  yield* playerMove(m, x);
}

/** AI $03 PlayerMove 26:9D9A */
export function* playerMove(m, x) {
  let fl = m.rd(FLASH), y = 0x4C;
  if (!(fl & 0x80) && (fl & 0x0F) !== 4) {
    m.wr(BEHAV + x, m.rd(PAD_DIR));
    y = 0xA7;
    if ((m.rd(FLAGS + x) & 0x40) && m.rd(TIMER2 + x) === 0) y = 0x4C;   // on the ground in a sink cell
  }
  m.wr(META + x, y);
  if (m.rd(TIMER2 + x)) {                                   // 9DBD jump timer
    dec(m, TIMER2 + x);
    const t = m.rd(TIMER2 + x);
    if (t === 0x11) playSound(m, 0x31, y);
    if (t >= 0x1E && !(m.rd(PAD_DIR) & 0x80)) m.wr(ACT_TIMER + x, m.rd(PAD_DIR));
  }
  y = yield* playerMagic(m, x);                             // 9DE1
  fl = m.rd(FLASH);
  if (!(fl & 0x80) && (fl & 0x0F) !== 4 && (m.rd(PAD_PREV) & 0x40)) {   // 9DE4 sword charge
    const ph = m.rd(PHASE + x);
    if ((ph < 0x0C || ph === 0x11) && m.rd(SWORD)) {
      m.wr(PHASE + x, 0x12);
      if ((m.rd(0x08) & 3) === 0) {
        inc(m, ORBIT);
        const lv = m.rd(ORBIT);
        if (lv === 8) { y = 0x0E; m.wr(ORBIT_BURST, 0x0E); }
        if ((lv & 7) === 0) playSound(m, 0x26, y);
        y = m.rd(SWORD_POWER);
        const cap = cpuRd(m, 0x9D7E + y);                   // SwordChargeCap
        if (cap < lv) m.wr(ORBIT, cap);
      }
    }
  }
  y = m.rd(PHASE + x);                                      // 9E39 swing counter / pose
  let path = 'swing';
  if (y === 0) path = 'idle';
  else if (y === 0x12 && (m.rd(PAD_PREV) & 0x40)) {
    if (!(m.rd(PAD_DIR) & 0x80)) path = 'idle';
    else if (m.rd(TIMER2 + x)) path = 'air';
  }
  if (path === 'idle') {                                    // 9E6F
    m.wr(META_S01, 0);
    if (m.rd(TIMER2 + x) === 0) {
      m.wr(BG_MASK + x, 0x32);
      if (!(m.rd(PAD_DIR) & 0x80)) dec(m, ANIM_S01);        // walk cycle
    }
  } else {
    if (path === 'air') m.wr(META_S01, 0);                  // 9E4F
    else {                                                  // 9E57
      m.wr(ANIM_S01, y);
      m.wr(META_S01, 4);
      dec(m, PHASE + x);
    }
    if (m.rd(TIMER2 + x) === 0) m.wr(BG_MASK + x, 0x32);   // 9E62
  }
  if (m.rd(FLASH) & 0x40) m.wr(BG_MASK, 0x34);              // 9E85 (absolute slot 0)
  y = m.rd(TIMER2 + x);                                     // 9E8F move
  let move = true;
  if (y !== 0) {
    m.wr(PAD_DIR, m.rd(ACT_TIMER + x));                     // the jump direction is latched
    if (cpuRd(m, JUMP_HEIGHT + y) === 0) move = false;
  }
  if (move) {
    m.wr(0x26, m.rd(META_DIR + x));
    if (m.rd(PAD_DIR) & 0x80) move = false;
  }
  if (move && (m.rd(PARAM1 + x) & 0x80)) {                  // 9EA7 forced move: DiagToCardinal 27:A017
    m.wr(ANIM_S01, 0);
    const v = cpuRd(m, 0xA017 + m.rd(PAD_DIR));
    if (v & 0x80) move = false;
    else m.wr(PAD_DIR, v);
  }
  if (move) {                                               // 9EBA
    m.wr(META_DIR + x, m.rd(PAD_DIR));
    dec(m, TIMER + x);
    if (!objMoveSlide(m, x)) {
      m.wr(SPAWN_PARAM_S01, m.rd(0x25));
      m.wr(META_DIR + x, m.rd(0x23));
      m.wr(PARAM2 + x, 8);
    } else if (m.rd(PARAM2 + x)) {                          // 9ED8 corner slide: keep the old direction
      dec(m, PARAM2 + x);
      m.wr(META_DIR + x, m.rd(0x26));
      if (!objMoveSlide(m, x)) m.wr(SPAWN_PARAM_S01, m.rd(0x25));
    }
  }
  if ((m.rd(0x08) & 3) === 0) { yield* hudChargeMeter(m); x = 0; }   // 9EEF (LDX #0 after)
  objPosToTemp(m, x);                                       // 9EFA
  const t2 = m.rd(TIMER2 + x);                              // LDA #0 / LDY obj_timer2 / BNE: N = timer2 b7
  objBgProbe(m, x, t2 ? 0 : 0x40, t2 & 0x80);
  if ((m.rd(TIMER2 + x) & 0xF0) === 0 && ((m.rd(SPAWN_PARAM_S01) | m.rd(0x20)) & 2)) {   // 9F09 low obstacle
    m.wr(ACT_TIMER + x, m.rd(BEHAV + x));
    if (m.rd(TIMER2)) {
      inc(m, TIMER2);                                       // cancels the DEC: the jump timer is frozen
      if (m.rd(MP) && (m.rd(0x08) & 0x0F) === 0 && !(yield* spendMp(m, 1, x))) return;
    }
  }
  if (m.rd(TIMER2 + x) === 0) {                             // 9F37 sink bit
    m.wr(0x10, m.rd(0x20) & 0x40);
    m.wr(BG_MASK + x, (m.rd(BG_MASK + x) & 0xBF) | m.rd(0x10));
  }
  m.wr(FLAGS + x, (m.rd(BG_MASK + x) & 0x40) | m.rd(FLAGS + x));
  const a20 = m.rd(0x20);
  if (!(a20 & 0x20)) {                                      // 9F5D off a conveyor
    const aux = m.rd(AUX + x);
    if ((m.rd(BG_ATTR) & 0x20) && ((aux - 7) & 0x80)) {     // just left one with obj_aux < 7 (signed CMP)
      m.wr(AUX + x, 0);
      m.wr(PARAM1 + x, 4);
      m.wr(BG_ATTR, a20);
      return;
    }
    m.wr(BG_ATTR, a20);                                     // 9F7B
    if (aux === 0) { m.wr(PARAM1 + x, 4); m.wr(BG_ATTR, a20); return; }
    if ((m.rd(0x08) & 3) === 0) dec(m, AUX + x);            // obj_aux decays
    m.wr(PARAM1 + x, 0x84);
    playerConveyor(m, x);                                   // BNE PlayerConveyor (its RTS ends PlayerMove)
    return;
  }
  m.wr(BG_ATTR, a20);                                       // 9F95 on a conveyor
  playerConveyor(m, x);
  const t = m.rd(TIMER2 + x);
  let ramp;
  if (t === 1) ramp = 'down';
  else if (t !== 0) return;
  else if (m.rd(FLASH) & 0x40) ramp = 'inc';
  else if (m.rd(0x08) & 0x0F) return;
  else ramp = 'down';
  if (ramp === 'down' && m.rd(PAD_DIR) === 4 && m.rd(AUX + x) <= 6) m.wr(AUX + x, 6);   // 9FB4
  inc(m, AUX + x);                                          // 9FC4
  if (m.rd(AUX + x) > 9) m.wr(AUX + x, 9);
}

// ---------------------------------------------------------------- PlayerUpdate 26:9B45 (slot 1)

/** PlayerSetSpeed 26:9BF6: slot 0 obj_flags2 low nibble */
function playerSetSpeed(m, a) {
  m.wr(0x10, a);
  m.wr(FLAGS2, (m.rd(FLAGS2) & 0xF0) | a);
}

/** PlayerPlaceBody 26:9CEF */
function playerPlaceBody(m, x) {
  let y = m.rd(TIMER2);
  const h = cpuRd(m, JUMP_HEIGHT + y);
  m.wr(0x31, h);
  if (h) {
    m.wr(COLL + x, 0x60);
    objPosToTemp(m, x);
    objBgProbe(m, x, 0x40);
    y = 0x1B;                                               // ObjBgProbe's lookup path ends with LDY #$1B
    m.wr(FLAGS + x, m.rd(FLAGS + x) & 0xBF);
  } else {
    m.wr(COLL + x, 0x50);
    m.wr(0x10, m.rd(FLAGS + x) & 7);                        // keeps its own b0-2, takes slot 0's b3-7
    m.wr(FLAGS + x, (m.rd(FLAGS) & 0xF8) | m.rd(0x10));
  }
  if (m.rd(0x31) === 0xF9) {                                // landing from a pit fall
    playSound(m, 0x56, y);
    m.wr(0x07D7, 0x10);
    m.wr(0x07D6, 0x10);
  }
  m.wr(0x10, x);
  objPosToTemp(m, 0);
  m.wr(0x30, 0);
  tempAddOffset(m);                                         // 26:9824
  tempToObjPos(m, x);                                       // JMP 26:9897
}

/** PlayerFacing 27:A1C9 */
function playerFacing(m, x) {
  if (m.rd(AREA) === 0x5F && m.rd(PHASE)) {
    m.wr(META_S01, m.rd(META_S01) & 0xFC);
    return;
  }
  const b = m.rd(BEHAV);
  if (b !== 0xFF) {                                         // $FF: BEQ A1F9 lands on BNE with Z = 1 (from the CMP)
    const meta = m.rd(META + x);                            // -> no direction always takes slot 0's direction
    if (cpuRd(m, 0xA211 + ((((b & 7) ^ 0xFF) + 1 + meta + meta) & 7))) return;   // FacingKeep
  }
  const d = m.rd(META_DIR);
  m.wr(META_DIR + x, d);
  m.wr(0x10, (d >> 1) & 3);
  m.wr(META + x, (m.rd(META + x) & 0xFC) | m.rd(0x10));
}

/** AI $01 PlayerUpdate 26:9B45 */
export function* playerUpdate(m, x) {
  if ((m.rd(PAD_NEW) & 0x40) && m.rd(USE_ITEM) && m.rd(SUB_STATE) === 8) m.wr(SUB_STATE, 6);
  playerSetSpeed(m, m.rd(FLAGS2 + x) & 0x0F);
  playerPlaceBody(m, x);
  playerFacing(m, x);
  if (status(m) === 4) {                                    // 9B68 status 4
    playerSetSpeed(m, 2);
    m.wr(META + x, 0x53);
  } else if (m.rd(FLASH) & 0x40) {                          // 9B7D warp exit spin
    playerSetSpeed(m, 9);
    if (((m.rd(SPAWN_PARAM_S01) | m.rd(BG_ATTR)) & 6) === 0) m.wr(FLASH, m.rd(FLASH) & 0xBF);
    else {
      m.wr(META, cpuRd(m, 0x9B41 + (m.rd(META_S01) & 3)));  // WarpSpinPoseTable
      m.wr(FLAGS + 1, m.rd(FLAGS + 1) | 0x40);
      dec(m, ANIM);
      if (m.rd(PHASE)) {
        if ((m.rd(META_S01) & 3) === 2 && (m.rd(PAD_DIR) & 0x80)) m.wr(META_S01, 0x96);
      } else m.wr(ANIM_S01, m.rd(ANIM));
    }
  }
  if (m.rd(FLASH) & 0x80) {                                 // 9BD2 status b7
    const y = (m.rd(FLASH) & 0x30) >> 4;
    m.wr(0x10, cpuRd(m, 0x9BF2 + y));
    m.wr(META + x, (m.rd(META + x) & 3) | m.rd(0x10));
    playerSetSpeed(m, 5);
    return;
  }
  const meta = m.rd(META + x);                              // 9C03 shield overlay
  if (m.rd(SHIELD) === 0) m.wr(OVERLAY + x, meta & 4 ? 0x4C : (meta & 3) | 8);
  else m.wr(OVERLAY + x, ((meta & 7) + 0x0C) & 0xFF);
  const y = ((m.rd(META + x) << 1) & 7);                   // 9C2A
  const ph = m.rd(PHASE);
  if (ph === 0x10) { yield* chargedShot(m, x); return; }
  if (ph > 0x10 || ph < 4) return;
  if (m.rd(ACTIVE) === 0x0B && ph !== 0x0F) return;         // sparring duel: one slash only
  m.wr(0x04A2, 0);                                          // 9C4C slash
  spawnShot(m, x, 0x19, y);
  const s = m.rd(SWORD);
  m.wr(0x0502, cpuRd(m, 0x9C66 + s));                       // SwordElemBit
  m.wr(0x03E2, cpuRd(m, 0x8BD2 + s));                       // SwordAttackTable
}

/** 26:9C6C the release shot */
function* chargedShot(m, x) {
  const sword = m.rd(SWORD);
  if (sword === 5) {                                        // 9CDA Crystalis: only in area $5F
    if (m.rd(AREA) === 0x5F) spawnShot(m, x, 0x13, (m.rd(META + x) & 3) << 1);
  } else if (sword !== 0) {
    m.wr(0x10, m.rd(ORBIT) >> 3);
    if (m.rd(0x10) === 0 && status(m) !== 1 && m.rd(ACCESSORY) === 0x0F) inc(m, 0x10);
    if (m.rd(0x10) === 3 && cpuRd(m, 0x8BD8 + sword) >= m.rd(MP)) {   // level 3 needs MP > cost
      dec(m, 0x10);
      dec(m, ORBIT);
    }
    const id = (((sword - 1) << 2) | m.rd(0x10)) & 0xFF;
    if (spawnShot(m, x, id, (m.rd(META + x) & 3) << 1) && (m.rd(ORBIT) & 0xF8) === 0x18) {
      if (!(yield* spendMp(m, cpuRd(m, 0x8BD8 + m.rd(SWORD)), x))) return;   // PLA PLA: no reset
    }
  }
  m.wr(ORBIT, 0);                                           // 9CD1
  m.wr(ORBIT_BURST, 0);
}

export const AI_PLAYER = { 0x01: playerUpdate, 0x02: playerMoveLocked, 0x03: playerMove };
