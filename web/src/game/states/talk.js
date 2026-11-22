// re-c web port — NPC talk + the gameplay message window (session 115). REVERSE.md "NPC talk (30:D0C8)",
// "NPC script format", "Talk actions", "Who picks the message" and the bank-20 text printer table.
//
//   npcTalk(m, y)          NpcTalk 30:D0C8 (sub_state $11): PlaySound($29) -> talker test -> NpcScript 14:8000 ->
//                          ShowMessagePacked -> TalkActionTable 30:D123[$22] (after the window closes)
//   npcScript(m)           NpcScript 14:8000 (+ NpcResult 14:8078, NpcFlagApply 14:8112, NpcFlagRef 14:8148)
//   showMessage(m)         ShowMessage 30:D354 (generator): font CHR, TextWindowOpen 20:8500, irq_mode 1/9, sprite
//                          pass + OamClipTop, TextPrintBox 20:8528, then wait for SELECT (replay) or A/B (close)
//   showMessagePacked(m)   30:D347: $21:$20 linear id -> $07DF/$07DE (TextIdUnpack 30:D3C2), then showMessage
//
// Every spin on the PPU queue ($0A != $0B) is a `yield SPIN` (frame_ready is not cleared, so the NMI that comes
// meanwhile skips its OAM DMA / scroll copy exactly as on hardware). The ROM's `LSR frame_ready / BCC` wait
// (30:D38A, 20:8641) is lsrWait(). The Y register of the printer is kept 8-bit with the ROM's INC $2B / $2D carries
// (and without them where the ROM has none, e.g. TextChar's INY in the main loop).
import { R } from '../ram.js';
import { cpuRd, indY, setR6, setR7, switchBank16 } from '../prg.js';
import { SPIN, waitFrame, oamClip, hudRedraw, hudHpBar } from './death.js';
import { farSpriteDraw } from './areaenter.js';
import { readPads } from '../pads.js';
import { playSound } from '../playsound.js';
import { objInit } from '../obj/objinit.js';
import { queueEmpty, queuePush, ppuCtrlNmiOn, ppuCtrlNmiOff } from '../world/ppuqueue.js';

const BUF = 0x6000, Q = 0x6200;
const MSG = 0x07DE, GROUP = 0x07DF, CUR_ITEM = 0x07DC, NAME = 0x6400, NPC_STATE = 0x6580, FLAGS = 0x6480;
const TALK_SLOT = 0x0623, FREEZE = 0x05A0, SPAWN_B2 = 0x0540, SPAWN_P = 0x0560, ACTIVE = 0x04A0;

function unported(m, what) {
  if (!m.unported.has(what)) { m.unported.add(what); m.log(`${what} not ported yet`); }
}
const inc = (m, a) => m.wr(a, (m.rd(a) + 1) & 0xFF);
const dec = (m, a) => { const v = (m.rd(a) - 1) & 0xFF; m.wr(a, v); return v; };

/** OamClipTop 30:C17D: hide sprites 1-63 with y <= $58 - ($04 & 7) */
export function oamClipTop(m) {
  m.wr(0x10, m.rd(R.cam_y_lo) & 7);
  oamClip(m, (0x58 - m.rd(0x10)) & 0xFF);
}

/** spin until the PPU queue is empty ($850F / $85E9 / $87D0 / $87EF / $8722 loops) */
function* qWait(m) { while (!queueEmpty(m)) yield SPIN; }

/** 30:D38A / 20:8641: LSR frame_ready / BCC (wait for the NMI without clearing frame_ready first) */
export function* lsrWait(m) {
  for (;;) {
    const v = m.rd(R.frame_ready);
    m.wr(R.frame_ready, v >> 1);
    if (v & 1) return;
    if ((v >> 1) === 0) yield SPIN;
  }
}

// ---------------------------------------------------------------- bank 20 text printer
/** TextQueueUpload 20:8836: PPUCTRL NMI off, record [$21 $20 $24 $25] at $0B, NMI on */
function textQueueUpload(m) {
  ppuCtrlNmiOff(m);
  queuePush(m, m.rd(0x21), m.rd(0x20), m.rd(0x24), m.rd(0x25));
  ppuCtrlNmiOn(m);
}

/** TextFrameRows 20:87EF: 2 rows of 32 tiles from the 3-byte templates at 20:8899+x -> $6000, queued; $20/$21 += $22/$23 */
function* textFrameRows(m, x) {
  yield* qWait(m);
  let y = 0;
  m.wr(0x2F, 2);
  do {
    m.wr(BUF + y, 0xFE); y++;
    m.wr(BUF + y, cpuRd(m, 0x8899 + x)); x++; y++;
    m.wr(0x2E, 0x1D);
    do { m.wr(BUF + y, cpuRd(m, 0x8899 + x)); y++; } while (dec(m, 0x2E) !== 0);
    x++;
    m.wr(BUF + y, cpuRd(m, 0x8899 + x)); x++; y++;
  } while (dec(m, 0x2F) !== 0);
  textQueueUpload(m);
  const lo = m.rd(0x22) + m.rd(0x20);
  m.wr(0x20, lo & 0xFF);
  m.wr(0x21, (m.rd(0x23) + m.rd(0x21) + (lo >> 8)) & 0xFF);
}

/** TextDrawFrame 20:87B7: NT2 rows 0-9 (5 x TextFrameRows), then attributes $2BC0 = $FF x $40 from $6001 */
function* textDrawFrame(m) {
  for (const x of [0, 3, 3, 3, 6]) yield* textFrameRows(m, x);
  yield* qWait(m);
  for (let x = 0x41; x > 0; x--) m.wr(BUF + x, 0xFF);
  m.wr(0x21, 0x2B); m.wr(0x20, 0xC0); m.wr(0x25, 1);
  textQueueUpload(m);
}

/** 20:850F: wait for the queue, then the ring bytes $6200-$621F = $FF */
function* qWaitClear(m) {
  yield* qWait(m);
  for (let x = 0x1F; x >= 0; x--) m.wr(Q + x, 0xFF);
}

/** TextWindowOpen 20:8500 */
export function* textWindowOpen(m) {
  for (let x = 0; x < 0x10; x++) m.wr(0x20 + x, cpuRd(m, 0x8879 + x));
  yield* textDrawFrame(m);
  yield* qWaitClear(m);
}

/** TextPrintBox 20:8528 */
export function* textPrintBox(m) {
  for (let x = 0; x < 0x10; x++) m.wr(0x20 + x, cpuRd(m, 0x8889 + x));
  yield* textPrint(m);
  yield* qWaitClear(m);                                  // JMP $850F
}

/** TextChar 20:87AE (the caller does the INY) */
function textChar(m, a) {
  const x = m.rd(0x28);
  m.wr(BUF + x, a);
  m.wr(0x28, (x + 1) & 0xFF);
}

/** INY + INC zp when Y wraps */
function iny(m, y, zp) {
  y = (y + 1) & 0xFF;
  if (y === 0) inc(m, zp);
  return y;
}

/** TextFlush 20:85B8: 4 x $40 bytes of $6000 -> $2820 / $2860 / $28A0 / $28E0, each after the queue drains */
function* textFlush(m) {
  m.wr(0x21, 0x28); m.wr(0x20, 0x20); m.wr(0x25, 0); m.wr(0x24, 0x40);
  for (const [lo, src] of [[0x20, 0], [0x60, 0x40], [0xA0, 0x80], [0xE0, 0xC0]]) {
    m.wr(0x20, lo); m.wr(0x25, src);
    yield* qWait(m);
    textQueueUpload(m);
  }
}

/** TextClearBuf 20:8720: 8 rows of FE 1E 29x20 1F, $28 = $20 (keeps Y via $29) */
function* textClearBuf(m, y) {
  m.wr(0x29, y);
  yield* qWait(m);
  let x = 0;
  do {
    m.wr(BUF + x, 0xFE); x++;
    m.wr(BUF + x, 0x1E); x++;
    m.wr(0x27, 0x1D);
    do { m.wr(BUF + x, 0x20); x++; } while (dec(m, 0x27) !== 0);
    m.wr(BUF + x, 0x1F); x = (x + 1) & 0xFF;
  } while (x !== 0);
  m.wr(0x28, 0x20);
  return m.rd(0x29);
}

/** TextPrintStr 20:8693: ($2A) zero-terminated, then the auto-space peek at ($2C),$29+1 and resume at ($2C) */
function textPrintStr(m) {
  let y = 0;
  for (;;) {
    const a = indY(m, 0x2A, y);
    if (a === 0) break;
    textChar(m, a); y = (y + 1) & 0xFF;
  }
  y = (m.rd(0x29) + 1) & 0xFF;                           // 869F
  const a = indY(m, 0x2C, y);
  if ((a >= 4 && a < 0x0A) || (a >= 0x40 && a !== 0x5F)) textChar(m, 0x20);
  y = iny(m, m.rd(0x29), 0x2D);
  m.wr(0x2A, m.rd(0x2C)); m.wr(0x2B, m.rd(0x2D));
  return y;
}

/** 20:8710 + string table lookup (codes 05 / 06 / 07: table 20:8A00 / 8A14 / 8A5C) */
function textTableStr(m, y, table) {
  m.wr(0x2C, m.rd(0x2A)); m.wr(0x2D, m.rd(0x2B));
  y = iny(m, y, 0x2D);
  m.wr(0x29, y);
  const x = (indY(m, 0x2A, y) << 1) & 0xFF;
  m.wr(0x2A, cpuRd(m, table + x)); m.wr(0x2B, cpuRd(m, table + x + 1));
  return textPrintStr(m);
}

/** TextWord 20:86F5: dictionary word (byte & $7F) from TextDictPtrs 20:8900 */
function textWord(m, y) {
  m.wr(0x2C, m.rd(0x2A)); m.wr(0x2D, m.rd(0x2B));
  m.wr(0x29, y);
  const x = (indY(m, 0x2A, y) << 1) & 0xFF;
  m.wr(0x2A, cpuRd(m, 0x8900 + x)); m.wr(0x2B, cpuRd(m, 0x8901 + x));
  return textPrintStr(m);
}

/** TextCurItem 20:8772 (code 08): item name 20:8A5C[$07DC] ($FF -> $49), no auto-space */
function textCurItem(m, y) {
  m.wr(0x2C, m.rd(0x2A)); m.wr(0x2D, m.rd(0x2B));
  m.wr(0x29, y);
  let a = m.rd(CUR_ITEM);
  if (a === 0xFF) a = 0x49;
  const x = (a << 1) & 0xFF;
  m.wr(0x2A, cpuRd(m, 0x8A5C + x)); m.wr(0x2B, cpuRd(m, 0x8A5D + x));
  for (let i = 0; ; i = (i + 1) & 0xFF) {
    const c = indY(m, 0x2A, i);
    if (c === 0) break;
    textChar(m, c);
  }
  y = iny(m, m.rd(0x29), 0x2D);
  m.wr(0x2A, m.rd(0x2C)); m.wr(0x2B, m.rd(0x2D));
  return y;
}

/** TextPageWait 20:8619 (code 03): flush, prompt tile $40 at $2910, wait for A/B, clear */
function* textPageWait(m, y) {
  m.wr(0x29, y);
  yield* textFlush(m);
  yield* qWait(m);
  m.wr(0x21, 0x29); m.wr(0x20, 0x10); m.wr(0x24, 1); m.wr(0x25, 0);
  m.wr(BUF, 0x40);
  textQueueUpload(m);
  m.wr(0x24, 0x40);
  for (;;) {
    yield* lsrWait(m);
    readPads(m);
    setR7(m, cpuRd(m, 0x83FE + m.rd(GROUP)));            // TextSetR7(TextGroupBank[group])
    if (m.rd(R.pad_new) & 0xC0) break;
  }
  y = iny(m, m.rd(0x29), 0x2B);
  return yield* textClearBuf(m, y);
}

/** TextPrint 20:853A: message $07DE & $1F of group $07DF */
function* textPrint(m) {
  const g = m.rd(GROUP);
  m.wr(0x6700, g);
  setR7(m, cpuRd(m, 0x83FE + g));                        // TextSetR7(TextGroupBank[g])
  const gx = (g << 1) & 0xFF;
  m.wr(0x2C, cpuRd(m, 0x8422 + gx)); m.wr(0x2D, cpuRd(m, 0x8423 + gx));
  const id = m.rd(MSG);
  m.wr(0x6701, id);
  let y = (id & 0x1F) << 1;
  m.wr(0x2A, indY(m, 0x2C, y)); y++;
  m.wr(0x2B, indY(m, 0x2C, y));
  y = 0;
  for (;;) {
    const a = indY(m, 0x2A, y);
    if (a < 0x0A) {                                      // TextCtrlDispatch 20:8593: $2C/$2D = handler address
      m.wr(0x2C, cpuRd(m, 0x85A2 + a * 2)); m.wr(0x2D, cpuRd(m, 0x85A3 + a * 2));
      switch (a) {
        case 0: yield* textFlush(m); return;             // 85B6 PLA PLA -> TextFlush, RTS out of TextPrint
        case 1:                                          // 85F2 clear box
          y = yield* textClearBuf(m, y);
          m.wr(0x29, 0x20);                              // STA $29 (A = $20 from TextClearBuf)
          yield* qWait(m);
          m.wr(0x28, 0x23);
          y = iny(m, y, 0x2B);
          break;
        case 2:                                          // 8607 newline
          m.wr(0x28, ((((m.rd(0x28) + 0x40) & 0xE0) + 4) & 0xFF));
          y = iny(m, y, 0x2B);
          break;
        case 3: y = yield* textPageWait(m, y); break;
        case 4: {                                        // 866B player name
          m.wr(0x29, y);
          for (let i = 0; ; i = (i + 1) & 0xFF) {
            const c = m.rd(NAME + i);
            if (c === 0) break;
            textChar(m, c);
          }
          y = iny(m, m.rd(0x29), 0x2B);
          break;
        }
        case 5: y = textTableStr(m, y, 0x8A00); break;
        case 6: y = textTableStr(m, y, 0x8A14); break;
        case 7: y = textTableStr(m, y, 0x8A5C); break;
        case 8: y = textCurItem(m, y); break;
        case 9: {                                        // 8753 n spaces (n = 0 -> 256)
          m.wr(0x29, y);
          let n = indY(m, 0x2A, (y + 1) & 0xFF);
          do { textChar(m, 0x20); n = (n - 1) & 0xFF; } while (n !== 0);
          y = iny(m, m.rd(0x29), 0x2B);
          y = iny(m, y, 0x2B);
          break;
        }
      }
    } else if (a & 0x80) {
      y = textWord(m, y);
    } else {
      textChar(m, a);
      y = (y + 1) & 0xFF;                                // TextChar's INY (no $2B carry)
    }
  }
}

// ---------------------------------------------------------------- ShowMessage
/** 30:D896 (sub_state 9 only): $10 x (irq_mode 0, frame, irq_mode 1, frame) */
function* blinkWindow(m) {
  m.wr(0x20, 0x10);
  do {
    m.wr(R.irq_mode, 0);
    yield* waitFrame(m);
    m.wr(R.irq_mode, 1);
    yield* waitFrame(m);
  } while (dec(m, 0x20) !== 0);
}

/** ShowMessage 30:D354 ($07DE / $07DF set) */
export function* showMessage(m) {
  for (;;) {
    m.wr(R.chr_r0_alt, 0x3C);
    m.wr(R.chr_r0_alt + 1, 0x38);
    setR6(m, 0x14);
    yield* textWindowOpen(m);
    yield* waitFrame(m);
    m.wr(R.irq_mode, m.rd(R.irq_mode) === 7 ? 9 : 1);
    farSpriteDraw(m);
    oamClipTop(m);
    if (m.rd(R.sub_state) === 9) yield* blinkWindow(m);
    setR6(m, 0x14);
    yield* textPrintBox(m);
    let replay = false;
    for (;;) {
      yield* lsrWait(m);
      readPads(m);
      farSpriteDraw(m);
      oamClipTop(m);
      if (m.rd(R.pad_new) & 0x20) { replay = true; break; }   // SELECT: JMP ShowMessage
      if (m.rd(R.pad_new) & 0xC0) break;
    }
    if (replay) continue;
    m.wr(0x20, 8);
    do {
      yield* waitFrame(m);
      farSpriteDraw(m);
      oamClipTop(m);
    } while (dec(m, 0x20) !== 0);
    m.wr(R.irq_mode, 0);
    m.wr(R.pad_new, 0);
    m.wr(R.sub_state, 8);
    return;
  }
}

/** TextIdUnpack 30:D3C2: $07DF = $07DF*8 + ($07DE >> 5), $07DE &= $1F */
function textIdUnpack(m) {
  const id = m.rd(MSG);
  m.wr(GROUP, ((m.rd(GROUP) << 3) | (id >> 5)) & 0xFF);
  m.wr(MSG, id & 0x1F);
}

/** ShowMessagePacked 30:D347 */
export function* showMessagePacked(m) {
  m.wr(MSG, m.rd(0x20));
  m.wr(GROUP, m.rd(0x21));
  textIdUnpack(m);
  yield* showMessage(m);
}

// ---------------------------------------------------------------- NpcScript (bank 14)
/** NpcFlagRef 14:8148 on ($24),Y: $26 = $27 = b0, $28 = mask, $27 = b0 >> 3; returns [byte index X, Y + 2] */
function flagRef(m, y) {
  const r0 = indY(m, 0x24, y);
  m.wr(0x26, r0);
  const r1 = indY(m, 0x24, (y + 1) & 0xFF);
  m.wr(0x28, cpuRd(m, 0x8167 + (r1 & 7)));
  m.wr(0x27, r0 >> 3);
  return [((r1 >> 3) | ((r0 & 7) << 5)) & 0xFF, (y + 2) & 0xFF];
}

/** NpcFlagTest 14:8135 -> [cond true, Y] */
export function flagTest(m, y) {
  const [x, y2] = flagRef(m, y);
  let v = m.rd(FLAGS + x);
  if (m.rd(0x26) & 0x20) v ^= 0xFF;
  return [(v & m.rd(0x28)) !== 0, y2];
}

/** NpcFlagApply 14:8112: b7 = clear, else set; stops after the item with b6 */
export function flagApply(m, y) {
  do {
    const [x, y2] = flagRef(m, y);
    y = y2;
    const mask = m.rd(0x28);
    m.wr(FLAGS + x, m.rd(0x26) & 0x80 ? m.rd(FLAGS + x) & ~mask & 0xFF : m.rd(FLAGS + x) | mask);
  } while (!(m.rd(0x26) & 0x40));
  return y;
}

/** NpcResult 14:8078: $22 = r0 >> 3, $21 = r0 & 7, $20 = r1 */
export function npcResult(m, y) {
  const r0 = indY(m, 0x24, y);
  m.wr(0x22, r0 >> 3);
  m.wr(0x21, r0 & 7);
  m.wr(0x20, indY(m, 0x24, (y + 1) & 0xFF));
  return (y + 2) & 0xFF;
}

/** 14:808E (Y += 3) / 14:808F (Y += 2), then skip a set-list when $26 b6 */
function skipEntry(m, y, n) {
  y = (y + n) & 0xFF;
  if (m.rd(0x26) & 0x40) {
    let a;
    do { a = indY(m, 0x24, y); y = (y + 2) & 0xFF; } while (!(a & 0x40));
  }
  return y;
}

/** NpcScript 14:8000 ($23 = NPC param; banks 14/15 mapped) */
export function npcScript(m) {
  const p = m.rd(0x23), x = (p << 1) & 0xFF, t = p & 0x80 ? 0x8A5D : 0x895D;
  m.wr(0x24, cpuRd(m, t + x)); m.wr(0x25, cpuRd(m, t + x + 1));
  let y = 0, ok;
  do {                                                   // section A: stateless overrides
    [ok, y] = flagTest(m, y);
    if (ok) { npcResult(m, y); return; }                 // JMP NpcResult (set-list not applied)
    y = skipEntry(m, y, 2);
  } while (!(m.rd(0x26) & 0x80));
  for (;;) {                                             // area list: (area, state) pairs until $FF
    const a = indY(m, 0x24, y); y = (y + 1) & 0xFF;
    if (a === 0xFF) break;
    if (a === m.rd(R.area_id)) m.wr(NPC_STATE + p, indY(m, 0x24, y));
    y = (y + 1) & 0xFF;
  }
  const base = m.rd(0x24) + y;                           // section B base = the byte after the $FF
  m.wr(0x24, base & 0xFF);
  m.wr(0x25, (m.rd(0x25) + (base >> 8)) & 0xFF);
  y = m.rd(NPC_STATE + p);
  do {
    [ok, y] = flagTest(m, y);
    if (ok) {
      y = npcResult(m, y);
      m.wr(NPC_STATE + p, indY(m, 0x24, y));
      if (m.rd(0x26) & 0x40) flagApply(m, (y + 1) & 0xFF);
      return;
    }
    y = skipEntry(m, y, 3);
  } while (!(m.rd(0x26) & 0x80));
  npcResult(m, y);                                       // 8076 falls into NpcResult (bytes after the chain)
}

// ---------------------------------------------------------------- NpcTalk + TalkActionTable
/** NpcVanish 30:D31C / D31F (Y given) */
export function npcVanish(m, y =m.rd(TALK_SLOT)) {
  m.wr(0x0660 + y, 1);
  m.wr(ACTIVE + y, 0x53);
  m.wr(FREEZE + y, 0);
  m.wr(0x03A0 + y, 0);
  playSound(m, 0x6D, y);
}

/** MakeFollower13 30:D23F (talk acts 6 / 13, event acts 2 / 28): slot 13 becomes type $32 (follower) */
export function makeFollower13(m) {
  m.wr(ACTIVE + 0x0D, 0x32);
  m.wr(0x03AD, 0); m.wr(0x062D, 0); m.wr(0x064D, 0);
  m.wr(0x04ED, 1);
  m.wr(0x060D, 0xFF);
}

const nop = function* () {};
/** TalkActionTable 30:D123 (ported entries; the rest log "not ported") */
export const TALK_ACTIONS = {
  0x00: nop, 0x04: nop, 0x07: nop, 0x0B: nop, 0x0E: nop, 0x0F: nop, 0x12: nop,
  0x1C: nop, 0x1D: nop, 0x1E: nop, 0x1F: nop,            // D163 RTS
  0x01: function* (m) {                                  // D19C type $64 into the slot pair s, s+1
    m.wr(0x10, (((m.rd(TALK_SLOT) - 1) & 0xFE) + 1) & 0xFF);
    m.wr(0x11, 0x64);
    objInit(m);                                          // ObjInit restores $11 = $64
    inc(m, 0x10);
    objInit(m);
  },
  0x02: function* (m) { npcVanish(m); },                 // D31C
  0x06: function* (m) { makeFollower13(m); },            // D23F (s199)
  0x0D: function* (m) { makeFollower13(m); },            // D23F (s199)
  0x08: function* (m) {                                  // D63F re-init slot 14 with $04CE unless active = 8
    if (m.rd(ACTIVE + 0x0E) === 0x08) return;
    m.wr(0x10, 0x0E); m.wr(0x11, m.rd(0x04CE));
    objInit(m);
  },
  0x05: function* (m) { m.wr(0x04AE, 0); m.wr(0x07D7, 0); },   // D164
  0x09: function* (m, y) {                               // D263 gold += 100 (INC low byte on carry: ROM quirk)
    playSound(m, 0x3F, y);
    const s = m.rd(0x0702) + 0x64;
    m.wr(0x0702, s & 0xFF);
    if (s > 0xFF) inc(m, 0x0702);
    setR6(m, 0x1A);
    yield* hudRedraw(m, 1);
  },
  0x0C: function* (m) { m.wr(ACTIVE + m.rd(TALK_SLOT), 0x54); },   // D25A
  0x17: function* (m) {                                  // D16D TalkFullRestore
    m.wr(0x07D7, 0);
    m.wr(0x03C1, m.rd(0x03C0));
    m.wr(0x0708, m.rd(0x0709));
    setR6(m, 0x1A);
    yield* hudHpBar(m);
    yield* hudRedraw(m, 4);
    npcVanish(m);
  },
  0x1B: function* (m) { m.wr(0x060D, 6); },              // D341
};

/** NpcTalk 30:D0C8 (sub_state $11). yIn = the Y register at the dispatch (PlaySound keeps it in $010F). */
export function* npcTalk(m, yIn = m.rd(0x010F)) {
  playSound(m, 0x29, yIn);
  const y = m.rd(TALK_SLOT);
  let talker = m.rd(FREEZE + y) === 0;
  if (!talker && m.rd(SPAWN_B2 + y) === 1) talker = [0x5E, 0x5F, 0x62, 0x6D, 0x6C].includes(m.rd(SPAWN_P + y));
  if (!talker) {                                         // D0F3: group $1F msg 0
    m.wr(MSG, 0); m.wr(GROUP, 0x1F);
    yield* showMessage(m);
    return;
  }
  m.wr(0x23, m.rd(SPAWN_P + y));
  switchBank16(m, 7);
  npcScript(m);
  const act = m.rd(0x22);                                // PHA
  yield* showMessagePacked(m);
  const a = (act << 1) & 0xFF;                           // PLA / ASL / TAY -> ($10)
  m.wr(0x10, cpuRd(m, 0xD123 + a)); m.wr(0x11, cpuRd(m, 0xD124 + a));
  const fn = TALK_ACTIONS[a >> 1];
  if (fn) yield* fn(m, a);
  else unported(m, `TalkActionTable ${a >> 1} (30:${(m.rd(0x10) | m.rd(0x11) << 8).toString(16).toUpperCase()})`);
}
