// re-c web port — magic 3, telepathy (session 145). REVERSE.md substate table rows 21/22, "Telepathy reply
// `Telepathy` `14:816F`" (text section) and the s145 note there.
//
//   substTelepathySelect(m)  SubstTelepathySelect 30:DD6B (sub_state $15, set by magic 3 27:A18E):
//     open 30:DF8F            msg 33.2 "With whom will you speak?" window (TextWindowOpen/TextPrintBox), 30:C874
//                             (BG palettes black, $6000-$603F = 0, PpuQueueBlock($2B)), cursor = OAM 0, irq_mode 1
//     setup 30:DD7B           CHR R4 alt $50, R2 $5C, palette $6150-$615F = 30:E0A8, $07FF = $3F
//     loop 30:DD9A            SFX $2C every 64 frames (30:DE72), cursor blink tile $3D/$3F ($07FF b3, 30:DDCD),
//                             the caster (30:DFFD) + 4 sage faces (30:DFCE: records 30:E088 via ObjSlot1FDraw), WaitFrame,
//                             ReadPadsMenu, PadNewButtonIndex -> JmpTable30_DDBD: A = reply, B/SEL/START = exit,
//                             U/D ignored, L/R = sage $21 -/+ 1 (& 3), cursor x = $21*48 + $38 (30:DDED)
//   telepathyReply(m)        SubstTelepathyReply 30:DE04 (A in the select; sub_state 22 is never stored):
//                             Telepathy 14:816F -> $21:$20, TextIdUnpack + TextPrintBox into the open window, then
//                             one face (30:DE7E: 30:DEA1 + sage*8) until a new A/B/SEL/START
//   exit 30:DE51              OAM 0 hidden, OamClipTail($F0), PaletteBuild(0), PpuQueueAttrAll, WaitFrame,
//                             StatRecalc, irq_mode = the value saved at entry (PHA at 30:DD6D), sub_state 8
import { R } from '../ram.js';
import { cpuRd, setR6, switchBank16 } from '../prg.js';
import { playSound } from '../playsound.js';
import { readPadsMenu } from '../pads.js';
import { waitFrame, paletteBuildRom, statRecalc, oamClip } from './death.js';
import { textWindowOpen, textPrintBox, oamClipTop, flagTest } from './talk.js';
import { waitPpuQueue, ppuQueueBlock, objSlot1FDraw } from './pause.js';
import { queueAttrAll } from '../world/ppuqueue.js';
import { padNewButtonIndex } from './teleport.js';

const OAM = 0x0200, BLINK = 0x07FF, MP = 0x0708, MP_MAX = 0x0709, RNG = 0x07C0, LEVEL = 0x0421;

export function hideOam0(m) { for (let i = 0; i < 4; i++) m.wr(OAM + i, 0xF0); }

/** 30:DE72: SFX $2C when $07FF & $3F = 0. Y = the caller's Y (PlaySound saves it in $010F). */
function chime(m, y) { if (!(m.rd(BLINK) & 0x3F)) playSound(m, 0x2C, y); }

/** 30:C874: BG palettes $6140-$615C (every 4th) and $6140-$614C = $0F, $6000-$603F = 0, PpuQueueBlock($2B), WaitFrame */
function* blackenBg(m) {
  yield* waitPpuQueue(m);
  for (let x = 0x1C; x >= 0; x -= 4) m.wr(0x6140 + x, 0x0F);
  for (let x = 0x0C; x >= 0; x--) m.wr(0x6140 + x, 0x0F);
  for (let x = 0x3F; x >= 0; x--) m.wr(0x6000 + x, 0);
  yield* ppuQueueBlock(m, 0x2B);
  yield* waitFrame(m);
}

/** 30:DF8F: the message window + cursor setup */
export function* selectOpen(m) {
  m.wr(0x04AC, 0);
  yield* waitPpuQueue(m);
  setR6(m, 0x14);
  yield* textWindowOpen(m);                             // 20:8500
  yield* textPrintBox(m);                               // 20:8528
  yield* blackenBg(m);                                  // 30:C874
  m.wr(R.chr_r0_alt, 0x3C);
  m.wr(R.chr_r0_alt + 1, 0x38);
  m.wr(OAM + 3, 0x38);
  m.wr(0x10, m.rd(R.cam_y_lo) & 7);
  m.wr(OAM, (0x4C - m.rd(0x10)) & 0xFF);
  m.wr(OAM + 2, 0);
  m.wr(R.irq_mode, 1);
  m.wr(0x21, 0);
}

/** TelepathyCasterRecord 30:DFFD-31:E037 (runs on across the bank seam into the fixed bank): record 0 at $6000 =
 *  the player at (x $84, y $A0), meta $CD, tile ofs $4C, flags $0B, anim $6007 - 1; while $1F != 0 the casting
 *  pose instead: meta $CC, anim = $07FF >> 2, and $1F = 0 once that is 0. */
function casterRecord(m) {
  const rec = [0x84, 0xA0, 0xCD, 0x4C, 0x0B, 0x00, 0x00];
  rec.forEach((v, i) => m.wr(0x6000 + i, v));
  m.wr(0x6007, (m.rd(0x6007) - 1) & 0xFF);
  if (!m.rd(0x1F)) return;
  m.wr(0x6002, 0xCC);
  const a = m.rd(BLINK) >> 2;
  m.wr(0x6007, a);
  if (!a) m.wr(0x1F, 0);
}

/** 30:DFCE: the caster + the 4 sage faces = records 30:E088 at $6008-$6027 (y = $4C - (cam_y & 7)), ObjSlot1FDraw */
function* drawFaces(m) {
  yield* waitPpuQueue(m);
  casterRecord(m);                                      // 30:DFFD
  for (let x = 0x1F; x >= 0; x--) m.wr(0x6008 + x, cpuRd(m, 0xE088 + x));
  m.wr(0x10, m.rd(R.cam_y_lo) & 7);
  const y = (0x4C - m.rd(0x10)) & 0xFF;
  for (const a of [0x6009, 0x6011, 0x6019, 0x6021]) m.wr(a, y);
  m.wr(0x20, 4);
  objSlot1FDraw(m);
}

/** 30:DDED: $21 &= 3, cursor x = $21 * 48 + $38 */
export function cursorX(m) {
  const s = m.rd(0x21) & 3;
  m.wr(0x21, s);
  m.wr(0x10, (s << 4) & 0xFF);
  m.wr(OAM + 3, (s * 0x30 + 0x38) & 0xFF);
}

/** SubstTelepathySelect 30:DD6B */
export function* substTelepathySelect(m) {
  const savedIrq = m.rd(R.irq_mode);                   // PHA, pulled at 30:DE6A
  m.wr(0x07DE, 0x02);                                   // msg 33.2
  m.wr(0x07DF, 0x21);
  yield* selectOpen(m);
  m.wr(0x5C, 0x50);                                     // chr_r4_alt
  m.wr(0x07F2, 0x5C);                                   // chr_r2_play
  m.wr(0x5A, 0x5C);                                     // chr_r2_alt
  for (let x = 0x0F; x >= 0; x--) m.wr(0x6150 + x, cpuRd(m, 0xE0A8 + x));
  m.wr(BLINK, 0x3F);
  m.wr(0x1F, 1);
  let y = m.rd(0x010F);                                 // Y at the first DE72: never used ($07FF = $3F)
  for (;;) {                                            // DD9A
    chime(m, y);
    const b = (m.rd(BLINK) - 1) & 0xFF;                 // 30:DDCD
    m.wr(BLINK, b);
    m.wr(OAM + 1, (b & 0x08) ? 0x3F : 0x3D);
    yield* drawFaces(m);
    yield* waitFrame(m);
    readPadsMenu(m);
    const k = padNewButtonIndex(m);
    y = k < 0 ? 0xFF : k * 2;
    if (k < 0 || k === 4 || k === 5) continue;
    if (k === 0) { yield* telepathyReply(m); break; }
    if (k <= 3) break;                                  // B / SELECT / START -> 30:DE51
    m.wr(0x21, (m.rd(0x21) + (k === 7 ? 1 : -1)) & 0xFF);   // DDE1 INC INC DEC / DDE5 DEC
    cursorX(m);
  }
  yield* selectExit(m, savedIrq);
}

/** Telepathy 14:816F ($23 = sage; banks 14/15 mapped) -> $21:$20 = linear message id */
function telepathy(m) {
  const sage = m.rd(0x23);
  const row = (r) => {                                  // 14:8179
    const x = ((((r << 2) | sage) << 1) & 0xFF);
    m.wr(0x21, cpuRd(m, 0x9A2C + x));
    m.wr(0x20, cpuRd(m, 0x9A2D + x));
  };
  const mp = m.rd(MP) - 8;
  if (mp < 0) { row(0); return; }
  m.wr(MP, mp);
  const n = m.rd(RNG);                                  // 14:8222
  m.wr(RNG, (n + 1) & 0xFF);
  const r = cpuRd(m, 0x821A + cpuRd(m, 0x822F + (n & 0x3F)));
  if (r === 0) {
    const v = 0x20 + mp;
    m.wr(MP, v > 0xFF || v >= m.rd(MP_MAX) ? m.rd(MP_MAX) : v);
    row(1);
    return;
  }
  if (r === 1) { row(2); return; }
  m.wr(0x29, r & 1);
  const cls = cpuRd(m, 0x98F4 + m.rd(R.area_id));
  m.wr(0x28, cls);
  if (cpuRd(m, 0x8213 + cls) >= m.rd(LEVEL)) { row(3); return; }
  // TelepathyHint 14:81D1: first entry whose flag test is false gives the message (alt with b6 + $29)
  const x = ((((cls << 2) | sage) << 1) & 0xFF);
  m.wr(0x24, cpuRd(m, 0x99F4 + x));
  m.wr(0x25, cpuRd(m, 0x99F5 + x));
  const ptr = m.rd(0x24) | (m.rd(0x25) << 8);
  let y = 0;
  for (;;) {
    const [t, y2] = flagTest(m, y);
    y = y2;
    const b0 = m.rd(0x26);
    if (!t) {
      if ((b0 & 0x40) && m.rd(0x29)) y += 2;
      m.wr(0x21, cpuRd(m, (ptr + y) & 0xFFFF));
      m.wr(0x20, cpuRd(m, (ptr + y + 1) & 0xFFFF));
      return;
    }
    y += 2;
    if (b0 & 0x40) y += 2;
    if (b0 & 0x80) { row(3); return; }
  }
}

/** SubstTelepathyReply 30:DE04 */
function* telepathyReply(m) {
  hideOam0(m);
  oamClipTop(m);
  const sage = m.rd(0x21);
  m.wr(0x23, sage);
  switchBank16(m, 7);
  telepathy(m);
  m.wr(0x07DE, m.rd(0x20));
  m.wr(0x07DF, m.rd(0x21));
  const id = m.rd(0x07DE);                              // TextIdUnpack 30:D3C2
  m.wr(0x07DF, ((m.rd(0x07DF) << 3) | (id >> 5)) & 0xFF);
  m.wr(0x07DE, id & 0x1F);
  setR6(m, 0x14);
  yield* textPrintBox(m);                               // 20:8528
  m.wr(0x21, sage);
  for (;;) {                                            // DE2F
    yield* waitFrame(m);
    yield* waitPpuQueue(m);
    chime(m, m.rd(0x010F));                             // Y here = ReadPadStable's (not tracked; approximation)
    m.wr(BLINK, (m.rd(BLINK) - 1) & 0xFF);
    m.wr(0x20, 1);
    casterRecord(m);                                    // 30:DFFD
    const s = (m.rd(0x21) << 3) & 0xFF;                 // 30:DE7E
    for (let i = 0; i < 8; i++) m.wr(0x6008 + i, cpuRd(m, 0xDEA1 + s + i));
    m.wr(0x10, m.rd(R.cam_y_lo) & 7);
    m.wr(0x6009, (0x44 - m.rd(0x10)) & 0xFF);
    objSlot1FDraw(m);
    readPadsMenu(m);
    if (m.rd(R.pad_new) & 0xF0) return;
  }
}

/** 30:DE51 */
export function* selectExit(m, savedIrq) {
  hideOam0(m);
  oamClip(m, 0xF0);                                     // OamClipTail
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);                                // JSR 26:8C0E
  queueAttrAll(m);
  yield* waitFrame(m);
  yield* statRecalc(m);
  m.wr(R.irq_mode, savedIrq);
  m.wr(R.sub_state, 8);
}
