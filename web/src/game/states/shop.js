// re-c web port — shops (session 121): the walk in / walk out shared by sub_states 32-36 and the pawn shop
// (sub_state 35). REVERSE.md "Substates" rows 32-36 and the Verification Log row "Pawn shop".
//
//   substPawnShop(m)   SubstPawnShop 30:D9AD = ShopWalkIn, 16:9509 (JMP 16:967A), ShopWalkOut
//   shopWalkIn(m)      ShopWalkIn 30:DA0C   (player walks down to y = $98, then right to x = $78; SwitchBank16(8))
//   shopWalkOut(m)     ShopWalkOut 30:DA72  (left to x = $68, up to y = $8F, ExitLookup -> area, sub_state 1)
//
// The pawn screen reuses the bank 16 menu (states/equip.js equipMenu) with $07DD = $FF: its shop branches
// (price line 16:81B4, A/B = sell 16:835E, "Anything else?" 16:846C) were ported in s109 and checked in s121.
// s122: the armor / tool shops (sub_states 32 / 33, 16:9558 / 16:957A, buy loop ShopBuyLoop 16:96CF, ShopBuy
// 16:9970; Verification Log row "Armor / tool shops"). Not ported yet: the inn 16:959C (sub_state 34) and
// SubstShopEmpty 30:D9C1 (needs ShowMessage).
import { R } from '../ram.js';
import { cpuRd, indY, setR6, switchBank16, farRestore } from '../prg.js';
import { readPadsMenu } from '../pads.js';
import { spriteDraw } from '../obj/sprites.js';
import { playerUpdate } from '../obj/player.js';
import { exitScan } from '../world/camera.js';
import { waitFrame, paletteBuildRom } from './death.js';
import { waitPpuQueue } from './pause.js';
import {
  equipMenu, menuPrint, menuSnd, menuFlush, confirm, hideOam, frameRows, selToIds, sortInventory, idsToSel,
  menuSprDraw, menuFarHud,
} from './equip.js';
import { slotPicture } from './saveload.js';

const inc = (m, a) => m.wr(a, (m.rd(a) + 1) & 0xFF);
const dec = (m, a) => m.wr(a, (m.rd(a) - 1) & 0xFF);

/** FarCall_B14_800E 31:FF8C: SpriteDraw with banks 28/29, then the caller's banks back */
function spriteDrawFar(m) {
  const r6 = m.rd(0x6E), r7 = m.rd(0x6F);
  switchBank16(m, 0x0E);
  spriteDraw(m);
  farRestore(m, r6, r7);
}

/** ClearOrbitSlots 30:DAFC */
function clearOrbitSlots(m) {
  m.wr(0x06C0, 0);
  for (let x = 4; x < 0x0C; x++) m.wr(0x04A0 + x, 0);
}

/** 30:DABF: one walk frame: WaitFrame, PlayerUpdate (slot 1, banks 26/27), obj_anim_s01 -= 1,
 *  obj_flags[1] &= $0B (X = 1 after PlayerUpdate: obj_phase is 0, so it returns at 26:9C2A), sprites */
function* walkStep(m) {
  yield* waitFrame(m);
  switchBank16(m, 0x0D);
  yield* playerUpdate(m, 1);
  dec(m, 0x04E1);
  m.wr(0x0381, m.rd(0x0381) & 0x0B);
  spriteDrawFar(m);
}

/** ShopWalkIn 30:DA0C */
export function* shopWalkIn(m) {
  // ShopDoorOpenStub 31:FE00 = RTS
  clearOrbitSlots(m);
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);                                // JSR 26:8C0E
  m.wr(0x58, 0x3C); m.wr(0x59, 0x38);                   // chr_r0_alt / chr_r1_alt
  m.wr(0x0360, 4);                                      // obj_meta_dir: down
  m.wr(0x0301, 2);
  m.wr(0x0600, 0); m.wr(0x0620, 0); m.wr(0x06C0, 0); m.wr(0x0BB2, 0);
  switchBank16(m, 0x0D);
  yield* playerUpdate(m, 1);                            // DA41 X = 1, JSR 26:9B45
  do {                                                  // DA46
    m.wr(0x0600, 0); m.wr(0x0620, 0);
    yield* walkStep(m);
    inc(m, 0xB1); inc(m, 0xB0);
  } while (m.rd(0xB0) !== 0x98);
  m.wr(0x0360, 2);                                      // DA5B: right
  do {
    yield* walkStep(m);
    inc(m, 0x71); inc(m, 0x70);
  } while (m.rd(0x70) !== 0x78);
  switchBank16(m, 8);                                   // banks 16/17
}

/** ShopWalkOut 30:DA72 (the x loop branches back to DA72, so the palette build + irq_mode 0 run each step) */
export function* shopWalkOut(m) {
  do {
    // ShopDoorCloseStub 31:FE17 = RTS
    setR6(m, 0x1A);
    paletteBuildRom(m, 0);
    m.wr(R.irq_mode, 0);
    m.wr(0x0360, 6);                                    // left
    yield* walkStep(m);
    dec(m, 0x71); dec(m, 0x70);
  } while (m.rd(0x70) !== 0x68);
  m.wr(0x0360, 0);                                      // DA93: up
  do {
    yield* walkStep(m);
    dec(m, 0xB1); dec(m, 0xB0);
  } while (m.rd(0xB0) !== 0x8F);
  spriteDrawFar(m);                                     // DAA5
  const [, y] = exitScan(m);                            // DAA8 ExitLookup (sub_state != 8: no load)
  m.wr(R.area_id, indY(m, 0x10, y));
  m.wr(0x6D, indY(m, 0x10, (y + 1) & 0xFF));
  m.wr(R.irq_mode, 0);
  m.wr(R.pad_new, 0);
  m.wr(R.sub_state, 1);
}

/** 16:9912: the shop window ($2800: frame rows 6, 9 x3, $0C) + its attribute bytes ($2BC0, all $FF) */
function* shopWindow(m) {
  m.wr(0x20, 0); m.wr(0x25, 0); m.wr(0x21, 0x28); m.wr(0x24, 0x40);
  yield* frameRows(m, 6);
  m.wr(0x19, 3);
  do { yield* frameRows(m, 9); dec(m, 0x19); } while (m.rd(0x19) !== 0);
  yield* frameRows(m, 0x0C);
  yield* waitPpuQueue(m);
  for (let x = 0x41; x > 0; x--) m.wr(0x6000 + x, 0xFF);
  m.wr(0x21, 0x2B); m.wr(0x20, 0xC0); m.wr(0x25, 1);
  menuFlush(m);
}

/** 16:9A12 (A = 0 here): sprite palette 1 $6154-$6157 = 16:8BFB[A*4] (tool shop, sub_state $21: 16:8C0B) */
function shopSprPalette(m, a) {
  const base = m.rd(R.sub_state) === 0x21 ? 0x8C0B : 0x8BFB;
  for (let y = 0; y < 4; y++) m.wr(0x6154 + y, cpuRd(m, base + ((a << 2) & 0xFF) + y));
}

/** 16:950C: shop screen setup (inventory sort, CHR alt R2-R5 $58-$5B, window, irq_mode 1, $07DD = $FF) */
function* shopSetup(m) {
  selToIds(m); sortInventory(m); idsToSel(m);
  yield* waitFrame(m);
  for (let i = 0; i < 4; i++) m.wr(0x5A + i, 0x58 + i);
  m.wr(0x5E, 0x3C); m.wr(0x5F, 0x38);
  yield* shopWindow(m);
  yield* waitFrame(m);
  m.wr(R.irq_mode, 1);
  for (const a of [0x6424, 0x6425, 0x6426, 0x6427, 0x646E, 0x646F]) m.wr(a, 0);
  m.wr(0x07DD, 0xFF);
  shopSprPalette(m, 0);
  sortInventory(m);                                     // JMP InvSortAll
}

/** ShopIdForArea 16:9953: shop_id $646D = ShopAreaIds 16:9F75[index of area_id in ShopAreaTable 16:9F54],
 *  or $21 (the index) when the area is not listed */
function shopIdForArea(m) {
  const a = m.rd(R.area_id);
  for (let x = 0; x < 0x21; x++) {
    if (cpuRd(m, 0x9F54 + x) === a) { m.wr(0x646D, cpuRd(m, 0x9F75 + x)); return; }
  }
  m.wr(0x646D, 0x21);
}

/** 16:967A (via 16:9509): "PAWN SHOP" / "Is there anything you wish to sell?" YES NO; YES -> EquipMenu
 *  16:8000 in shop mode; NO -> strings $2B $2A $20 + wait for any key (16:9878) */
function* pawnShop(m) {
  selToIds(m); sortInventory(m); idsToSel(m);
  yield* shopSetup(m);
  shopIdForArea(m);
  for (const s of [0x1B, 0x27, 0x28, 0x29]) yield* menuPrint(m, s);
  if (yield* confirm(m, 0x41)) {                        // C set = NO
    menuSnd(m, 0x4C);
    for (const s of [0x2B, 0x2A, 0x20]) yield* menuPrint(m, s);
    do { yield* waitFrame(m); readPadsMenu(m); } while (!m.rd(R.pad_new));   // 16:9878 / 9884
    menuSnd(m, 0x4B);
  } else {
    menuSnd(m, 0x4C);                                   // 96BE
    yield* equipMenu(m);
  }
  yield* hideOam(m);                                    // 96C6 MenuHideOam
  m.wr(0x07DD, 0);
}

/** SubstPawnShop 30:D9AD (sub_state $23, keeper type $43) */
export function* substPawnShop(m) {
  yield* shopWalkIn(m);
  yield* pawnShop(m);
  yield* shopWalkOut(m);
}

// ---- armor / tool shops (session 122): 16:9558 / 16:957A, the buy loop 16:96CF ----

const GOODS = 0x6470, PRICES = 0x6478, CUR = 0x6427, ITEM = 0x646C;

/** MenuWaitAnyKey 16:9878: MenuWaitPad 16:9884 (WaitFrame + ReadPadsMenu) until pad_new != 0, MenuSfx($4B) */
function* waitAnyKey(m) {
  do { yield* waitFrame(m); readPadsMenu(m); } while (!m.rd(R.pad_new));
  menuSnd(m, 0x4B);
}

/** ShopSetupArmor 16:988A / ShopSetupTool 16:98E3: goods (4) + prices (8 bytes) for shop_id, then the four
 *  3x3 pictures (16:98B6: column 16:9F96[i], row 3, tiles 16:8F5A[item] or $FF = blank) */
function* shopSetupGoods(m, goodsTab, priceTab) {
  shopIdForArea(m);
  const id = m.rd(0x646D);
  for (let y = 0; y < 4; y++) m.wr(GOODS + y, cpuRd(m, goodsTab + ((id << 2) & 0xFF) + y));
  for (let y = 0; y < 8; y++) m.wr(PRICES + y, cpuRd(m, priceTab + ((id << 3) & 0xFF) + y));
  for (let i = 0; i < 4; i++) {
    m.wr(0x19, i);
    m.wr(0x26, cpuRd(m, 0x9F96 + i)); m.wr(0x27, 3);
    const a = m.rd(GOODS + i);
    m.wr(0x29, a === 0xFF ? 0xFF : cpuRd(m, 0x8F5A + a));
    yield* slotPicture(m);
  }
}

/** 16:9815: the cursor arrow (shape 0) under goods slot $6427 (x = 16:9874[$6427], OAM slot $3B + 1) */
function cursorSpr(m) {
  m.wr(0x60FB, cpuRd(m, 0x9874 + m.rd(CUR)));
  m.wr(0x60F8, 0x21); m.wr(0x12, 0); m.wr(0x60F9, 0); m.wr(0x60FA, 1);
  m.wr(0x2E, 0x3B);
  menuSprDraw(m);
}

/** 16:9837: the item sprite (shape 2, tile 16:8FA5[item], OAM slot $37 + 1) + its palette 16:8FF0[item] & 3;
 *  nothing for an empty ($FF) slot */
function itemSpr(m) {
  const a = m.rd(GOODS + m.rd(CUR));
  if (a === 0xFF) return;
  m.wr(0x60F9, cpuRd(m, 0x8FA5 + a));
  m.wr(0x60FB, cpuRd(m, 0x9874 + m.rd(CUR)));
  m.wr(0x60F8, 0x21); m.wr(0x60FA, 1); m.wr(0x12, 2); m.wr(0x2E, 0x37);
  menuSprDraw(m);
  shopSprPalette(m, cpuRd(m, 0x8FF0 + a) & 3);
}

/** ShopPriceSel 16:9A37: $6474/75 = shop_prices[$6427] (0 for an empty slot) */
function shopPriceSel(m) {
  const c = m.rd(CUR), x = (c << 1) & 0xFF;
  const full = m.rd(GOODS + c) !== 0xFF;
  m.wr(0x6474, full ? m.rd(PRICES + x) : 0);
  m.wr(0x6475, full ? m.rd(PRICES + x + 1) : 0);
}

/** 16:9970: buy $646C for $6474/75: 1 = not enough gold, 2 = no free inventory slot, 0 = bought
 *  (gold -= price, inventory[$2E + $2F] = item). Armor shop: item >= $15 (BPL) -> armor row $6434 ($2E = 4),
 *  else $6438 ($2E = 8); tool shop: item $29 -> $6448 ($2E = $18; full -> X = 7, overwrites the last slot),
 *  else $6440 ($2E = $10). */
function shopBuy(m) {
  const g = m.rd(0x0702) + (m.rd(0x0703) << 8);
  const p = m.rd(0x6474) + (m.rd(0x6475) << 8);
  const d = (g - p) & 0xFFFF;
  m.wr(0x6476, d & 0xFF); m.wr(0x6477, d >> 8);
  if (g < p) return 1;
  const item = m.rd(ITEM);
  let base, n, fullX = -1;
  if (m.rd(R.sub_state) === 0x21) {
    if (item === 0x29) { base = 0x18; n = 8; fullX = 7; } else { base = 0x10; n = 8; }
  } else if (((item - 0x15) & 0x80) === 0) { base = 4; n = 4; } else { base = 8; n = 4; }
  m.wr(0x2E, base);
  let x = 0;
  while (x < n && m.rd(0x6430 + base + x) !== 0xFF) x++;
  if (x === n) { if (fullX < 0) return 2; x = fullX; }
  m.wr(0x2F, x);
  m.wr(0x0702, m.rd(0x6476)); m.wr(0x0703, m.rd(0x6477));
  m.wr(0x6430 + ((base + x) & 0xFF), item);
  return 0;
}

/** 16:96CF: "Buy something?" YES/NO, then the buy loop: LEFT/RIGHT move the cursor (16:97EF), SELECT leaves
 *  at once, B -> "Anything else?", A -> buy (16:9970) + its message, then "Anything else?" YES/NO;
 *  NO -> string $26 + any key (16:97DD) */
function* buyLoop(m) {
  const pr = function* (...s) { for (const a of s) yield* menuPrint(m, a); };
  const leave = function* () {                          // 97DD
    menuSnd(m, 0x4C);
    yield* pr(0x2A, 0x26);
    yield* waitAnyKey(m);
  };
  if (yield* confirm(m, 0x41)) { yield* leave(); return; }
  menuSnd(m, 0x4C);
  cursorSpr(m); itemSpr(m);
  yield* pr(0x2A);
  m.wr(0x19, m.rd(GOODS));
  yield* pr(0x1D, 0x32);
  for (;;) {
    yield* waitFrame(m); readPadsMenu(m);               // 96F8 MenuWaitPad
    m.wr(ITEM, m.rd(GOODS + m.rd(CUR)));
    const p = m.rd(R.pad_new);
    let step = null;                                    // 'move' (97EF) / 'again' (973A) / 'ask' (97A4) / 'price' (97CD)
    if (p & 0x02) { m.wr(CUR, (m.rd(CUR) - 1) & 3); step = 'move'; }
    else if (p & 0x01) { m.wr(CUR, (m.rd(CUR) + 1) & 3); step = 'move'; }
    else if (p & 0x20) return;                          // SELECT: RTS
    else if (p & 0x40) step = 'again';
    else if (!(p & 0x80)) step = 'price';
    else {
      menuSnd(m, 0x4C);
      const r = shopBuy(m);
      if (!(m.rd(0x6474) | m.rd(0x6475))) step = 'again';
      else {
        if (r === 1) yield* pr(0x2A, 0x22);
        else if (r === 2) yield* pr(0x2A, 0x21);
        else { yield* pr(0x2A, 0x2E); yield* menuFarHud(m, 1); }
        yield* waitAnyKey(m);
        step = 'ask';
      }
    }
    if (step === 'move') {                              // 97EF
      menuSnd(m, 0x4B);
      cursorSpr(m); itemSpr(m);
      yield* pr(0x2A);
      m.wr(0x19, m.rd(GOODS + m.rd(CUR)));
      yield* pr(0x1D, 0x32);
      continue;
    }
    if (step === 'again' || step === 'ask') {
      yield* pr(0x2A, step === 'again' ? 0x30 : 0x1F, 0x29);
      if (yield* confirm(m, 0x41)) { yield* leave(); return; }   // 97B3
      menuSnd(m, 0x4C);
      yield* pr(0x2A, 0x1D);
      m.wr(R.pad_new, 0);
    }
    shopPriceSel(m);                                    // 97CD
    yield* menuFarHud(m, 0x0B);
    yield* pr(0x32);
  }
}

/** 16:9558 armor (sub_state $20, chr_r1_alt $3E, title $18) / 16:957A tool ($21, $3A, $19) */
function* goodsShop(m, tool) {
  yield* shopSetup(m);
  yield* waitFrame(m);
  m.wr(0x59, tool ? 0x3A : 0x3E);
  for (const s of [tool ? 0x19 : 0x18, 0x1C, 0x29]) yield* menuPrint(m, s);
  if (tool) yield* shopSetupGoods(m, 0x9E28, 0x9E54);
  else yield* shopSetupGoods(m, 0x9DA4, 0x9DD0);
  yield* buyLoop(m);
  yield* hideOam(m);                                    // 96C6
  m.wr(0x07DD, 0);
}

/** SubstArmorShop 30:D985 (sub_state $20) */
export function* substArmorShop(m) {
  yield* shopWalkIn(m);
  yield* goodsShop(m, false);
  yield* shopWalkOut(m);
}

/** SubstToolShop 30:D98E (sub_state $21) */
export function* substToolShop(m) {
  yield* shopWalkIn(m);
  yield* goodsShop(m, true);
  yield* shopWalkOut(m);
}
