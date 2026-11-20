// re-c web port — the SELECT equipment / item menu (session 98, "Ph7 menu screens, rest"):
// SubstEquipMenu 30:D8C7 (sub_state $12) + the bank 16 menu 16:8000 (run with bank 23 at $A000).
// NMI side: mainloop.js nmiMode6 (31:F7DB) + irqMenuChrSplit (IRQ handler 7, 31:F61A).
// REVERSE.md "Equipment menu (web port, session 98)".
//
//   substEquipMenu(m)   generator = SubstEquipMenu 30:D8C7 (substate 18)
//   equipMenu(m)        generator = 16:8000 (returns undefined on SELECT / the $7000 = $FA exit, or the name
//                       of an unported exit: SaveLoadPage 16:9AEE, 30:C80C)
//
// Ported: the window frame (16:881E), the Sword/Armor/Shield/Power grid and the Item/Magic grid (16:871F),
// the cursor (16:8699), A = equip / unequip (16:82B7), B = drop with the YES/NO confirm (16:8342, 16:84DA),
// the per-category inventory sort (16:8534) and the selection re-find (16:9A62 / 9A91 / 85AE), MenuPrint
// 16:8933 with its FE/FD/FC escapes, MenuSprDraw 16:86D3. Not ported (logged once): START = SaveLoadPage
// 16:9AEE, the $7000 = $FB/$AB exits (30:C80C), the shop / pawn variants ($07DD != 0: 16:8613 FarCall
// HudRedraw, 16:8631 gold, 16:846C), the load tail 30:D930, SubstRestoreScreen 30:CC4D (area $5F).
import { R } from '../ram.js';
import { cpuRd, indY, setR6, setR7, switchBank16, farRestore } from '../prg.js';
import { readPads, readPadsMenu } from '../pads.js';
import { queuePush, ppuQueueFlush, ppuCtrlNmiOff, ppuCtrlNmiOn } from '../world/ppuqueue.js';
import { spriteDraw } from '../obj/sprites.js';
import { waitFrame, statRecalc, paletteBuildRom } from './death.js';
import { waitPpuQueue } from './pause.js';

const BUF = 0x6000;
const KIND = 0x6424;          // 0 = Sword/Armor/Shield/Power grid, 1 = Item/Magic grid
const NCOL = 0x6426;          // columns of the current grid (4 / 8)
const CUR = 0x6427;           // cursor cell (row * cols + col)
const SEL = 0x6428;           // inv_select: selected column per row (b7 = none), rows 0-3 grid 0, 4-7 grid 1
const INV = 0x6430;
const SHOP = 0x07DD;          // menu variant (0 = the SELECT menu)

function unported(m, key, what) {
  if (!m.unported.has(key)) { m.unported.add(key); m.log(`${what} ${key} not ported yet`); }
}

/** 16:8A9F: A -> snd_queue $0103, count $0100 = 1 under the lock $0101 */
function menuSnd(m, a) {
  m.wr(0x0103, a); m.wr(0x0101, 1); m.wr(0x0100, 1); m.wr(0x0101, 0);
}

/** MenuFlush 16:8AB0: queue ($21 $20 $24 $25) with NMI off; PpuQueueFlush 30:C67D now if rendering is off */
function menuFlush(m) {
  ppuCtrlNmiOff(m);
  queuePush(m, m.rd(0x21), m.rd(0x20), m.rd(0x24), m.rd(0x25));
  if (!(m.rd(R.ppumask_shadow) & 0x18)) ppuQueueFlush(m);
  ppuCtrlNmiOn(m);
}

/** MenuPutc 16:8A02 (Y is the caller's) */
function putc(m, a) {
  const x = m.rd(0x28);
  m.wr(BUF + x, a);
  m.wr(0x24, (m.rd(0x24) + 1) & 0xFF);
  m.wr(0x28, (x + 1) & 0xFF);
}

/** MenuPrint 16:8933: record A of MenuStrings 16:8C1B (PPU lo, hi, text ptr); FF end, FE save-slot name
 *  ($7118 / $7718), FD save-slot level ($71F7 / $77F7, MenuLevelDigits 16:89E0), FC item name $19
 *  (MenuItemNames 16:9086, $FF -> $49). $7001 is the Y scratch of the FE/FD escapes. */
export function* menuPrint(m, a) {
  m.wr(0x17, a);
  yield* waitPpuQueue(m);
  for (const z of [0x23, 0x22, 0x24, 0x25, 0x28]) m.wr(z, 0);
  const x = (a << 2) & 0xFF;
  m.wr(0x20, cpuRd(m, 0x8C1B + x)); m.wr(0x21, cpuRd(m, 0x8C1C + x));
  m.wr(0x10, cpuRd(m, 0x8C1D + x)); m.wr(0x11, cpuRd(m, 0x8C1E + x));
  let y = 0;
  for (;;) {
    const c = indY(m, 0x10, y);
    m.wr(0x18, c);
    if (c === 0xFF) break;
    if (c === 0xFE) {                                   // 8994: player name of save slot (b0)
      y = (y + 1) & 0xFF;
      m.wr(0x7001, y);
      m.wr(0x13, cpuRd(m, 0x89B6 + (indY(m, 0x10, y) & 1)));
      m.wr(0x12, 0x18);
      let yy = 0;
      do { putc(m, indY(m, 0x12, yy)); yy = (yy + 1) & 0xFF; } while (yy !== 6);
      y = (m.rd(0x7001) + 1) & 0xFF;
    } else if (c === 0xFD) {                            // 89B8: level of save slot
      y = (y + 1) & 0xFF;
      m.wr(0x7001, y);
      let yy = ((indY(m, 0x10, y) ? m.rd(0x77F7) : m.rd(0x71F7)) << 1) & 0xFF;
      for (let i = 0; i < 2; i++) {
        const d = cpuRd(m, 0x89E0 + yy);
        m.wr(0x18, d);
        putc(m, d);
        yy = (yy + 1) & 0xFF;
      }
      y = (m.rd(0x7001) + 1) & 0xFF;
    } else if (c === 0xFC) {                            // 8A0D: item name
      y = (y + 1) & 0xFF;
      let id = m.rd(0x19);
      if (id === 0xFF) id = 0x49;
      const xx = (id << 1) & 0xFF;
      m.wr(0x12, cpuRd(m, 0x9086 + xx)); m.wr(0x13, cpuRd(m, 0x9087 + xx));
      for (let yy = 0; ; yy = (yy + 1) & 0xFF) {
        const d = indY(m, 0x12, yy);
        m.wr(0x18, d);
        if (d === 0xFF) break;
        putc(m, d);
      }
    } else {
      putc(m, c);
      y = (y + 1) & 0xFF;
    }
  }
  yield* waitPpuQueue(m);
  menuFlush(m);
}

/** MenuSprDraw 16:86D3: shape $12 (MenuSprShapes 16:8B1F) at OAM slot $2E + 1, offsets $60F8-$60FB */
function menuSprDraw(m) {
  const s2 = (m.rd(0x12) << 1) & 0xFF;
  m.wr(0x12, cpuRd(m, 0x8B1F + s2)); m.wr(0x13, cpuRd(m, 0x8B20 + s2));
  m.wr(0x2F, indY(m, 0x12, 0));
  let y = 1;
  const a1 = m.rd(0x2E) << 1, a2 = (a1 & 0xFF) << 1;
  let x = ((a2 & 0xFF) + 4 + (a2 >> 8)) & 0xFF;           // ASL ASL ADC #4 (carry of the 2nd ASL)
  const put = (v) => { m.wr(0x0200 + x, v & 0xFF); x = (x + 1) & 0xFF; y = (y + 1) & 0xFF; };
  do {
    put(indY(m, 0x12, y) + m.rd(0x60F8));
    put(indY(m, 0x12, y) + m.rd(0x60F9));
    put(indY(m, 0x12, y) | m.rd(0x60FA));
    put(indY(m, 0x12, y) + m.rd(0x60FB));
    m.wr(0x2F, (m.rd(0x2F) - 1) & 0xFF);
  } while (m.rd(0x2F) !== 0);
}

/** 16:8605: WaitFrame, then all of OAM y = $F0 */
function* hideOam(m) {
  yield* waitFrame(m);
  for (let x = 0; x < 0x100; x++) m.wr(0x0200 + x, 0xF0);
}

const rowBase = (m) => (m.rd(KIND) ? 4 : 0);            // LDA $6424 / BEQ / LDA #4

/** 16:85E6: [inventory[(grid ? $10 : 0) + cursor], X] */
function curItem(m) {
  const x = ((m.rd(KIND) ? 0x10 : 0) + m.rd(CUR)) & 0xFF;
  return [m.rd(INV + x), x];
}

/** 16:85F6: equipment slot ($0711 + n) of row $2B (table 16:833A) */
const rowSlot = (m) => cpuRd(m, 0x833A + ((rowBase(m) + m.rd(0x2B)) & 0xFF));

/** 16:86AF: $60F8 / $60FB = position of the cursor cell (16:8B73 pairs + $30 / $38) */
function cellPos(m) {
  const x = ((((m.rd(KIND) ? 0x10 : 0) + m.rd(CUR)) & 0xFF) << 1) & 0xFF;
  m.wr(0x60F8, (cpuRd(m, 0x8B73 + x) + 0x30) & 0xFF);
  m.wr(0x60FB, (cpuRd(m, 0x8B74 + x) + 0x38) & 0xFF);
}

/** 16:8660: icon of the cursor item (tile 16:8FA5, flags 16:8FF0: b7 = 4-tile strip, b0-1 = palette) at
 *  OAM slot row * 9 + 1 */
function drawIcon(m) {
  const [a] = curItem(m);
  if (a >= 0x4B) return;
  m.wr(0x60F9, cpuRd(m, 0x8FA5 + a));
  m.wr(0x2E, (m.rd(0x2B) * 9) & 0xFF);
  const f = cpuRd(m, 0x8FF0 + a);
  m.wr(0x60FA, f & 3);
  m.wr(0x12, (f & 0x80) ? 3 : 2);
  cellPos(m);
  menuSprDraw(m);
}

/** 16:8699: the cursor frame (shape 0, palette 3) at OAM slot $39 */
function drawCursor(m) {
  m.wr(0x12, 0); m.wr(0x60F9, 0); m.wr(0x60FA, 3); m.wr(0x2E, 0x38);
  cellPos(m);
  menuSprDraw(m);
}

/** 16:850D: the YES/NO arrow (shape 1) at ($22, 16:8532[$23 & 1]) */
function confirmCursor(m) {
  m.wr(0x60FB, cpuRd(m, 0x8532 + (m.rd(0x23) & 1)));
  m.wr(0x60F8, m.rd(0x22));
  m.wr(0x60F9, 0); m.wr(0x60FA, 1); m.wr(0x2E, 0x36); m.wr(0x12, 1);
  menuSprDraw(m);
}

/** MenuConfirmYesNo 16:84DA: returns C (true = NO / cancel) */
function* confirm(m, a) {
  if (a === 0x41) a = 0x49;
  m.wr(0x22, a);
  m.wr(0x23, 1);
  for (;;) {
    yield* waitFrame(m);
    confirmCursor(m);
    readPadsMenu(m);
    if (m.rd(R.pad_new) & 0x0F) { m.wr(0x23, (m.rd(0x23) + 1) & 0xFF); menuSnd(m, 0x4B); }
    if (m.rd(R.pad_new) & 0xC0) break;
  }
  yield* waitFrame(m);
  m.wr(0x02DC, 0xF0);
  const v = m.rd(0x23);
  m.wr(0x23, v >> 1);
  return (v & 1) === 1;
}

/** 16:8A5A (X = caller's X): hide the 9 icon sprites of row $2B after a WaitFrame; the $60F8/$60FB
 *  writes index 16:8B73 with the stale X (dead values) */
function* eraseRowIcon(m, xIn) {
  m.wr(0x60F8, (cpuRd(m, 0x8B73 + xIn) + m.rd(0x2B)) & 0xFF);
  m.wr(0x60FB, (cpuRd(m, 0x8B74 + xIn) + m.rd(0x2A)) & 0xFF);
  let x = (m.rd(0x2B) * 36 + 4) & 0xFF;
  yield* waitFrame(m);
  for (let i = 0; i < 36; i++) { m.wr(0x0200 + x, 0xF8); x = (x + 1) & 0xFF; }
}

/** 16:8534: sort each of the 8 inventory categories ascending (counts 16:859E, offsets 16:85A6) via $60E0 */
function sortInventory(m) {
  for (let c = 0; c < 8; c++) {
    m.wr(0x2E, c);
    const n = cpuRd(m, 0x859E + c), o = cpuRd(m, 0x85A6 + c);
    for (let i = 0; i < n; i++) m.wr(0x60E0 + i, m.rd(INV + o + i));
    do {
      m.wr(0x2F, 0);
      let x = n;
      for (;;) {
        const a = m.rd(0x60DF + x);
        x--;
        if (x === 0) break;
        const b = m.rd(0x60DF + x);
        if (a >= b) continue;
        m.wr(0x60DF + x + 1, b);
        m.wr(0x60DF + x, a);
        m.wr(0x2F, (m.rd(0x2F) + 1) & 0xFF);
      }
    } while (m.rd(0x2F) !== 0);
    for (let i = 0; i < n; i++) m.wr(INV + o + i, m.rd(0x60E0 + i));
    m.wr(0x2D, 0);
  }
  m.wr(0x2E, 8);
}

/** 16:85AE: after a drop, re-find item $26 in row $2B's category -> inv_select (>= 8 -> $FF) */
function reselect(m) {
  const t = m.rd(0x26);
  if (t === 0xFF) return;
  const k = (rowBase(m) + m.rd(0x2B)) & 0xFF;
  let x = cpuRd(m, 0x85A6 + k), y = 0;
  for (let g = 0; g < 0x10000; g++) {
    const a = m.rd(INV + x);
    y = (y + 1) & 0xFF; x = (x + 1) & 0xFF;
    if (a === t) break;
  }
  y = (y - 1) & 0xFF;
  m.wr(SEL + k, ((y - 8) & 0x80) ? y : 0xFF);
}

const catPtr = (m, i) => {
  const p = cpuRd(m, 0x9ADE + 2 * i) | cpuRd(m, 0x9ADF + 2 * i) << 8;
  m.wr(0x18, i); m.wr(0x28, p & 0xFF); m.wr(0x29, p >> 8);
  return p;
};

/** 16:9A62: $6000-$6007 = the selected item id of each row ($FF = none) */
function selToIds(m) {
  for (let i = 0; i < 8; i++) {
    const p = catPtr(m, i);
    const s = m.rd(SEL + i);
    m.wr(BUF + i, (s & 0x80) ? 0xFF : m.rd(p + s));
  }
  m.wr(0x18, 8);
}

/** 16:9A91 + 9ABD: inv_select = position of $6000+row in its category after the sort ($FF = none) */
function idsToSel(m) {
  for (let i = 0; i < 8; i++) {
    const p = catPtr(m, i);
    let a = m.rd(BUF + i);
    if (a !== 0xFF) {
      m.wr(0x22, a);
      const lim = i < 4 ? 4 : 8;
      m.wr(0x19, lim);
      let y = 0;
      for (;;) {
        if (a === m.rd(p + y)) break;
        y++;
        if (y === lim) { y = 0xFF; break; }
      }
      a = y;
    }
    m.wr(SEL + i, a);
  }
  m.wr(0x18, 8);
}

/** 16:821F: menu palette $6150-$615F from 16:8BFB + 16:8236[X] */
function menuPalette(m, x) {
  const o = cpuRd(m, 0x8236 + x);
  for (let y = 0; y < 16; y++) m.wr(0x6150 + y, cpuRd(m, 0x8BFB + o + y));
}

/** 16:826F: the icons of the selected item of rows 0-3 of the current grid */
function drawSelIcons(m) {
  m.wr(0x2B, 0);
  const g = m.rd(KIND);
  do {
    const r = m.rd(0x2B);
    const s = m.rd(SEL + (g ? 4 : 0) + r);
    if (!(s & 0x80)) {
      m.wr(0x2A, s);
      m.wr(CUR, ((g ? r * 8 : r * 4) + s) & 0xFF);
      drawIcon(m);
    }
    m.wr(0x2B, r + 1);
  } while (m.rd(0x2B) < 4);
}

/** 16:81FD: grid setup: palette, CHR R0 = $5E = $3C, R1 16:8232, columns 16:8238, $5F 16:8234; 16:826C */
function* setupGrid(m) {
  const k = m.rd(KIND);
  menuPalette(m, k);
  m.wr(0x58, 0x3C); m.wr(0x5E, 0x3C);
  m.wr(0x59, cpuRd(m, 0x8232 + k));
  m.wr(NCOL, cpuRd(m, 0x8238 + k));
  m.wr(0x5F, cpuRd(m, 0x8234 + k));
  yield* hideOam(m);
  drawSelIcons(m);
}

/** 16:87B8: 3x3 item picture, tiles A..A+8, at $6000 + 16:8BF3[$29] */
function cellTiles(m, a) {
  const x = cpuRd(m, 0x8BF3 + m.rd(0x29));
  [0, 1, 2, 0x20, 0x21, 0x22, 0x40, 0x41, 0x42].forEach((o, i) => m.wr(BUF + x + o, (a + i) & 0xFF));
}

/** 16:87EA: empty cell (frame tiles $F5-$FC, centre $20) */
function cellBlank(m) {
  const x = cpuRd(m, 0x8BF3 + m.rd(0x29));
  const t = [0xF5, 0xF6, 0xF7, 0xF8, 0x20, 0xF9, 0xFA, 0xFB, 0xFC];
  [0, 1, 2, 0x20, 0x21, 0x22, 0x40, 0x41, 0x42].forEach((o, i) => m.wr(BUF + x + o, t[i]));
}

/** 16:8795: flush one 3-tile-row band ($60 bytes) with irq_mode b7 (no PaletteUpload), $20 += $60 */
function* gridBand(m) {
  m.wr(0x29, 0);
  yield* waitPpuQueue(m);
  m.wr(R.irq_mode, m.rd(R.irq_mode) | 0x80);
  menuFlush(m);
  yield* waitPpuQueue(m);
  const v = m.rd(0x20) + 0x60;
  m.wr(0x20, v & 0xFF);
  m.wr(0x21, (m.rd(0x21) + (v >> 8)) & 0xFF);
  m.wr(0x28, (m.rd(0x28) + 1) & 0xFF);
}

/** 16:871F: the 4 grid rows ($28C0 on, 16:8AFD setup, row template 16:8BD3, pictures 16:8F5A) */
function* drawGrid(m) {
  yield* waitPpuQueue(m);
  for (let i = 0; i < 16; i++) m.wr(0x20 + i, cpuRd(m, 0x8AFD + i));
  for (let y = 0; y < 0x60; y++) m.wr(BUF + y, cpuRd(m, 0x8BD3 + (y & 0x1F)));
  m.wr(0x29, 0); m.wr(0x28, 0); m.wr(0x27, 0);
  do {
    yield* waitPpuQueue(m);
    do {
      m.wr(R.frame_ready, 0);                           // 874D (no wait: the NMI does the OAM DMA)
      const x = m.rd(KIND) ? (m.rd(0x27) + 0x10) & 0xFF : m.rd(0x27);
      const a = m.rd(INV + x);
      if (a >= 0x4B) cellBlank(m);
      else cellTiles(m, cpuRd(m, 0x8F5A + a));
      m.wr(0x27, (m.rd(0x27) + 1) & 0xFF);
      m.wr(0x29, (m.rd(0x29) + 1) & 0xFF);
    } while ((m.rd(0x29) - m.rd(NCOL)) & 0x80);         // CMP $6426 / BMI
    yield* gridBand(m);
  } while ((m.rd(0x28) - 4) & 0x80);
  m.wr(R.irq_mode, m.rd(R.irq_mode) & 0x7F);
}

/** 16:8864: two 32-tile frame rows (FE, left, 29 x fill, right; 3 bytes of 16:8B0D each), $20 += $40 */
function* frameRows(m, x) {
  yield* waitPpuQueue(m);
  let y = 0;
  m.wr(0x2F, 2);
  do {
    m.wr(BUF + y++, 0xFE);
    m.wr(BUF + y++, cpuRd(m, 0x8B0D + x)); x++;
    m.wr(0x2E, 0x1D);
    do { m.wr(BUF + y++, cpuRd(m, 0x8B0D + x)); m.wr(0x2E, m.rd(0x2E) - 1); } while (m.rd(0x2E) !== 0);
    x++;
    m.wr(BUF + y++, cpuRd(m, 0x8B0D + x)); x++;
    m.wr(0x2F, m.rd(0x2F) - 1);
  } while (m.rd(0x2F) !== 0);
  menuFlush(m);
  const v = m.rd(0x20) + 0x40;
  m.wr(0x20, v & 0xFF);
  m.wr(0x21, (m.rd(0x21) + (v >> 8)) & 0xFF);
}

/** 16:881E: the window ($2800, 24 rows) + its attribute table ($2BC0 from $6001, all $FF) */
function* drawWindow(m) {
  const s = m.rd(R.irq_mode);
  m.wr(R.irq_mode, s | 0x80);
  yield* frameRows(m, 0);
  yield* frameRows(m, 6);
  m.wr(0x19, 9);
  do { yield* frameRows(m, 9); m.wr(0x19, m.rd(0x19) - 1); } while (m.rd(0x19) !== 0);
  yield* frameRows(m, 0x0C);
  yield* waitPpuQueue(m);
  for (let x = 0x41; x > 0; x--) m.wr(BUF + x, 0xFF);
  m.wr(0x21, 0x2B); m.wr(0x20, 0xC0); m.wr(0x25, 1);
  menuFlush(m);
  yield* waitPpuQueue(m);
  m.wr(R.irq_mode, s);
}

/** 16:823A: the row labels (string $11, $10, then 0-3 or 4-5) */
function* drawLabels(m) {
  yield* menuPrint(m, 0x11);
  yield* menuPrint(m, 0x10);
  for (const s of (m.rd(KIND) ? [4, 5] : [0, 1, 2, 3])) yield* menuPrint(m, s);
}

/** 16:82B7 (A): equip the cursor item in its row, or unequip it if it is the selected one */
function* pressA(m) {
  if (m.rd(SHOP)) { yield* pressB(m, true); return; }   // 82BC JMP 835E
  const [a] = curItem(m);
  if (a === 0xFF) { menuSnd(m, 0x4E); yield* menuPrint(m, 0x0F); return; }
  const x = (rowBase(m) + m.rd(0x2B)) & 0xFF;
  const s = m.rd(SEL + x);
  if (!(s & 0x80) && s === m.rd(0x2A)) {                // 82E6: unequip
    m.wr(SEL + x, s | 0x80);
    menuSnd(m, 0x4B);
    yield* eraseRowIcon(m, x);
    m.wr(0x0711 + rowSlot(m), 0);
    yield* menuPrint(m, 0x0F);
    return;
  }
  m.wr(SEL + x, m.rd(0x2A));                           // 8304: equip
  drawIcon(m);
  menuSnd(m, 0x4C);
  const [b] = curItem(m);
  m.wr(0x19, b);
  yield* menuPrint(m, (cpuRd(m, 0x8FF0 + b) & 0x20) ? 9 : 0x0A);
  const slot = rowSlot(m);
  const [c] = curItem(m);
  m.wr(0x0711 + slot, cpuRd(m, 0x903B + c));
}

/** 16:8342 (B): drop the cursor item after "YES/NO" (flag 16:8FF0 b6 = cannot drop) */
function* pressB(m, fromA = false) {
  if (!fromA && m.rd(SHOP)) { unported(m, '16:8347', 'menu shop variant (B)'); return; }
  const [a, xi] = curItem(m);                          // 835E
  if (a === 0xFF) { menuSnd(m, 0x4E); return; }
  m.wr(0x2C, xi); m.wr(0x19, a); m.wr(0x646E, a);
  if (cpuRd(m, 0x8FF0 + a) & 0x40) {
    menuSnd(m, 0x4E);
    yield* menuPrint(m, 0x0F);
    yield* menuPrint(m, m.rd(SHOP) ? 0x0D : 8);
    return;
  }
  menuSnd(m, 0x4C);
  yield* menuPrint(m, 0x0F);
  yield* menuPrint(m, m.rd(SHOP) ? 0x0B : 7);
  yield* menuPrint(m, 0x0E);
  if (yield* confirm(m, 0xA0)) {                       // NO
    menuSnd(m, 0x4B);
    yield* menuPrint(m, 0x0F);
    return;
  }
  menuSnd(m, 0x4C);
  if (m.rd(SHOP)) unported(m, '16:8631', 'menu shop variant (sell gold + 16:8613 HUD)');
  const k = (rowBase(m) + m.rd(0x2B)) & 0xFF;          // 83DB
  m.wr(0x26, m.rd(INV + ((cpuRd(m, 0x85A6 + k) + m.rd(SEL + k)) & 0xFF)));
  if ((m.rd(SEL + k) & 7) === m.rd(0x2A)) {             // dropping the equipped item
    m.wr(SEL + k, 0xFF);
    m.wr(0x26, 0xFF);
    const slot = rowSlot(m);
    m.wr(0x0711 + slot, 0);
    yield* waitFrame(m);
    m.wr(0x607F, m.rd(CUR));
    yield* eraseRowIcon(m, slot);
    m.wr(CUR, m.rd(0x607F));
  }
  yield* menuPrint(m, 0x0F);                           // 8423
  if (!m.rd(SHOP)) yield* menuPrint(m, 0x12);
  m.wr(INV + m.rd(0x2C), 0xFF);
  m.wr(0x14, m.rd(0x2A)); m.wr(0x15, m.rd(0x2B));
  const cur = m.rd(CUR);
  sortInventory(m);
  reselect(m);
  m.wr(R.frame_ready, m.rd(R.frame_ready) & 0xFE);
  drawSelIcons(m);
  yield* drawGrid(m);
  m.wr(CUR, cur);
  m.wr(0x2A, m.rd(0x14)); m.wr(0x2B, m.rd(0x15));
  yield* drawLabels(m);
  if (m.rd(SHOP)) unported(m, '16:846C', 'menu shop variant (after drop)');
}

/** bank 16 menu entry 16:8000 */
export function* equipMenu(m) {
  yield* hideOam(m);
  yield* waitFrame(m);
  m.wr(0x7000, 0);
  for (let i = 0; i < 16; i++) m.wr(0x20 + i, cpuRd(m, 0x8AED + i));
  for (let i = 0; i < 4; i++) m.wr(0x5A + i, 0x58 + i);
  m.wr(0x5E, 0x3C); m.wr(0x5F, 0x38);
  yield* drawWindow(m);
  yield* waitFrame(m);
  m.wr(R.irq_mode, 6);
  m.wr(R.ppuctrl_shadow, (m.rd(R.ppuctrl_shadow) & 0xFC) | 2);
  for (let a = 0x6140; a < 0x6160; a += 4) m.wr(a, 0x0F);
  for (const a of [0x6424, 0x6425, 0x6426, 0x6427, 0x646E, 0x646F]) m.wr(a, 0);
  m.wr(R.pad_new, 0);
  selToIds(m);
  sortInventory(m);
  idsToSel(m);
  let grid = 0, lbl = 'grid';
  for (;;) {
    if (lbl === 'grid') {                               // 807A / 807E
      m.wr(KIND, grid);
      const s = m.rd(0x7000);
      if (s === 0xFA) { menuSnd(m, 0x4C); yield* hideOam(m); return undefined; }
      if (s === 0xFB || s === 0xAB) {
        menuSnd(m, s === 0xFB ? 0x55 : 0x4C);
        yield* hideOam(m);
        unported(m, '30:C80C', 'menu exit');
        return '30:C80C';
      }
      m.wr(R.irq_mode, 0x86);
      const c2A = m.rd(0x2A), c2B = m.rd(0x2B), cur = m.rd(CUR);
      yield* setupGrid(m);
      yield* drawGrid(m);
      m.wr(CUR, cur); m.wr(0x2B, c2B); m.wr(0x2A, c2A);
      yield* drawLabels(m);
      m.wr(R.irq_mode, 6);
      lbl = 'moved';
    } else if (lbl === 'moved') {                       // 8171
      m.wr(0x646E, curItem(m)[0]);
      menuSnd(m, 0x4B);
      const r = m.rd(0x2B), c = m.rd(0x2A);
      m.wr(CUR, ((m.rd(KIND) ? r * 8 : r * 4) + c) & 0xFF);
      drawCursor(m);
      const [a] = curItem(m);
      if (a === 0xFF) { yield* menuPrint(m, 0x0F); lbl = 'loop'; continue; }
      m.wr(0x19, a);
      yield* menuPrint(m, 0x0F);
      if (m.rd(SHOP)) unported(m, '16:81B4', 'menu shop variant (price)');
      else yield* menuPrint(m, 6);
      lbl = 'ab';
    } else if (lbl === 'loop') {                        // 80D1
      yield* waitFrame(m);
      readPadsMenu(m);
      const p = m.rd(R.pad_new);
      const clamp = () => { if (!((m.rd(0x2A) - 4) & 0x80)) m.wr(0x2A, 3); };
      if (p & 0x08) {                                   // UP
        const r = (m.rd(0x2B) - 1) & 0xFF;
        m.wr(0x2B, r);
        if (!(r & 0x80)) { lbl = 'moved'; continue; }
        m.wr(0x2B, 3); clamp(); lbl = 'flip'; continue;
      }
      if (p & 0x04) {                                   // DOWN
        const r = (m.rd(0x2B) + 1) & 0xFF;
        m.wr(0x2B, r);
        if (r < 4) { lbl = 'moved'; continue; }
        m.wr(0x2B, 0); clamp(); lbl = 'flip'; continue;
      }
      if (p & 0x02) {                                   // LEFT
        const c = (m.rd(0x2A) - 1) & 0xFF;
        m.wr(0x2A, (c & 0x80) ? 0 : c);
        lbl = 'moved'; continue;
      }
      if (p & 0x01) {                                   // RIGHT
        const c = m.rd(0x2A);
        m.wr(0x2A, (c + 1) & 0xFF);
        if (m.rd(0x2A) >= m.rd(NCOL)) m.wr(0x2A, c);
        lbl = 'moved'; continue;
      }
      if (p & 0x20) return undefined;                   // SELECT: 81EC RTS
      if ((p & 0x10) && !m.rd(SHOP)) {                  // START: 16:821F(0), JMP SaveLoadPage
        menuPalette(m, 0);
        unported(m, '16:9AEE', 'SaveLoadPage');
        return '16:9AEE';
      }
      lbl = 'ab';
    } else if (lbl === 'ab') {                          // 815C
      if (m.rd(R.pad_new) & 0x80) yield* pressA(m);
      if (m.rd(R.pad_new) & 0x40) yield* pressB(m);
      lbl = 'loop';
    } else {                                            // 'flip' 81ED
      yield* menuPrint(m, 0x0F);
      grid = m.rd(KIND) ? 0 : 1;
      lbl = 'grid';
    }
  }
}

/** SelectedUseItem 30:DB0E: $0715 = selected consumable ($642C -> $6440) or key item ($642E -> $6450) - $1C */
function selectedUseItem(m) {
  let y = m.rd(0x642C), a;
  if (!(y & 0x80)) a = m.rd(0x6440 + y);
  else {
    y = m.rd(0x642E);
    if (y & 0x80) return;
    a = m.rd(0x6450 + y);
  }
  m.wr(0x0715, (a - 0x1C) & 0xFF);
}

/** SwordPowerLevel 30:D9D8: Y = 0/1/2 */
function swordPowerLevel(m) {
  const sw = m.rd(0x0711);
  if (sw === 0) return 0;
  const ball = m.rd(0x0718);
  if (ball === 0) {                                     // D9FC
    const a = m.rd(0x643B + sw);
    if (a === 0xFF) return 0;
    return (a & 1) ? 0 : 1;
  }
  m.wr(0x10, sw - 1);
  if (((ball - 1) & 3) !== sw - 1) return 0;
  return ((ball - 1) & 0x0C) ? 2 : 1;
}

/** SubstEquipMenu 30:D8C7 (sub_state $12, SELECT in play) */
export function* substEquipMenu(m) {
  if (m.rd(R.area_id) === 0x5F) m.wr(R.irq_mode, 0);
  const sword = m.rd(0x0711), im = m.rd(R.irq_mode);
  setR6(m, 0x10);
  setR7(m, 0x17);
  const exit = yield* equipMenu(m);
  if (exit) { for (;;) yield; }                        // unported exit: stop here
  yield* waitFrame(m);                                  // D8E7
  readPads(m);
  for (let x = 0; x < 0x100; x++) m.wr(0x0200 + x, 0xF0);
  selectedUseItem(m);
  m.wr(0x0719, swordPowerLevel(m));
  m.wr(R.irq_mode, im);
  let xReg = 0;
  if (sword !== m.rd(0x0711)) {                         // 30:DAFC: orbit level 0, slots 4-11 off
    m.wr(0x06C0, 0);
    for (let x = 4; x < 0x0C; x++) m.wr(0x04A0 + x, 0);
    xReg = 0x0C;
  }
  if (m.rd(R.area_id) === 0x5F) unported(m, '30:CC4D', 'SubstRestoreScreen');
  for (let y = 0x0D; y < 0x1F; y++) {                   // 30:DADA: drop the $EB/$EC objects with tile ofs 0
    if (!m.rd(0x04A0 + y)) continue;
    const t = m.rd(0x0300 + y);
    if ((t === 0xEB || t === 0xEC) && m.rd(0x0320 + y) === 0) m.wr(0x04A0 + y, 0);
  }
  if (m.rd(0x7000) === 0xFA) {                          // D930: after a menu LOAD
    unported(m, '30:D930', 'menu load tail');
    for (;;) yield;
  }
  yield* statRecalc(m, xReg);
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);              // FarCall_B14_800E
  switchBank16(m, 0x0E);
  spriteDraw(m);
  farRestore(m, r6, r7);
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);                                // JSR 26:8C0E
  m.wr(R.pad_new, 0);                                   // 30:D8BE
  m.wr(R.sub_state, 8);
}
