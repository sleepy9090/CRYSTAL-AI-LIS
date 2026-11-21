// re-c web port — shops (session 121): the walk in / walk out shared by sub_states 32-36 and the pawn shop
// (sub_state 35). REVERSE.md "Substates" rows 32-36 and the Verification Log row "Pawn shop".
//
//   substPawnShop(m)   SubstPawnShop 30:D9AD = ShopWalkIn, 16:9509 (JMP 16:967A), ShopWalkOut
//   shopWalkIn(m)      ShopWalkIn 30:DA0C   (player walks down to y = $98, then right to x = $78; SwitchBank16(8))
//   shopWalkOut(m)     ShopWalkOut 30:DA72  (left to x = $68, up to y = $8F, ExitLookup -> area, sub_state 1)
//
// The pawn screen reuses the bank 16 menu (states/equip.js equipMenu) with $07DD = $FF: its shop branches
// (price line 16:81B4, A/B = sell 16:835E, "Anything else?" 16:846C) were ported in s109 and checked in s121.
// Not ported (logged once): the armor / tool / inn entries 16:9500 / 9503 / 9506 (sub_states 32-34) and
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
} from './equip.js';

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
