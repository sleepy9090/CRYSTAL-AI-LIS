// re-c web port — GiveItem 30:D3EB (sub_state 7, session 139) + the item hooks it runs. REVERSE.md
// "GiveItem / item-use dispatch", "Item hooks 0/1", "Event triggers — ItemHook2 14:80E3 / table 15:A17A".
//
//   giveItem(m)            GiveItem 30:D3EB: item id = obj_spawn_param[$0623] -> $07DC.
//                            id >= $80 -> 30:D539 (invisible event trigger: sub_state += 2, $08++, ItemEventHook(2),
//                                         EventActionDispatch, sub_state 8 and JMP MainStateGameplay = SubstPlay
//                                         runs in the same frame)
//                            id  = $70 -> TrapChest 30:D3DA (chest slot -> type $73, sub_state 8)
//                            else      -> ItemEventHook(0) (ItemGetHook 14:826F); full -> GiveItemFull 30:D47C;
//                                         else remap $50-$6F, SFX $3F / $93, msg 32.12, slot off, EventActionDispatch
//   itemEventHook(m, n)    ItemEventHook 30:D834 (hooks 0-2; hook 1 = ItemUseHook 14:831E + handlers, s141)
//   itemUseMenu(m)         ItemUseMenu 30:D497 (sub_state 6, s141; the Warp Boots' SubstTeleportMenu = states/teleport.js, s143)
//   showPendingMessage(m)  ShowPendingMessage 30:D085 (sub_state $10, s141; ItemUseFail "Nothing happens.")
//   eventActionDispatch(m) 30:D552 through EventActionTable 30:D563[$22] (ported entries; the rest log "not ported")
//
// The SwordPowerLevel 30:D9D8 call after a pickup (30:D408) discards its Y result: $0719 is only written by the
// equip menu (30:D8FD). Its one lasting effect is $10 = sword - 1 (when a sword and a ball are equipped).
import { R } from '../ram.js';
import { cpuRd, indY, setR7, switchBank16 } from '../prg.js';
import { playSound } from '../playsound.js';
import { objInit, farObjInit } from '../obj/objinit.js';
import { SPIN, statRecalc, waitFrames } from './death.js';
import { showMessage, showMessagePacked, npcResult, flagTest, flagApply, npcVanish, TALK_ACTIONS } from './talk.js';
import { restoreIfArea5F, saveGame } from './saveload.js';
import { swordPowerLevel } from './equip.js';
import { mainState1, playPaletteRestore } from './play.js';
import { substTeleportMenu } from './teleport.js';

const TALK_SLOT = 0x0623, SPAWN_P = 0x0560, ACTIVE = 0x04A0, CUR_ITEM = 0x07DC, MSG = 0x07DE, GROUP = 0x07DF;
const INV = 0x6430;
const PLA = Symbol('PLA PLA');                          // act 30: drops the caller's return address

function unported(m, what) {
  if (!m.unported.has(what)) { m.unported.add(what); m.log(`${what} not ported yet`); }
}
const inc = (m, a) => m.wr(a, (m.rd(a) + 1) & 0xFF);

// ---------------------------------------------------------------- bank 14 hooks
/** InvStoreItem 14:82A0 ($24 = get record, $29 = store value). Returns the Y register. */
function invStoreItem(m) {
  let x = indY(m, 0x24, 0), y = indY(m, 0x24, 1);
  if (y & 0x80) {                                       // fixed equip slot
    m.wr(0x23, m.rd(0x23) & 0x7F);
    const v = m.rd(0x29);
    if (v === 4) {                                      // 82B7 Crystalis
      m.wr(INV, 4);
      m.wr(0x0711, 5);
      for (const a of [0x6431, 0x6432, 0x6433, 0x643C, 0x643D, 0x643E, 0x643F]) m.wr(a, 0xFF);
      m.wr(0x6428, 0);
      return y;
    }
    m.wr(INV + x, v);                                   // 82DE
    if (v === 6 || v === 8 || v === 0x0A || v === 0x0C) {
      if (((v - 4) >> 1) === m.rd(0x0718)) { m.wr(0x642B, 0xFF); m.wr(0x0718, 0); }   // bracelet unequips the ball
    }
    return y;
  }
  do {                                                  // 8308: first $FF slot of start..start+count
    if (m.rd(INV + x) === 0xFF) {
      m.wr(INV + x, m.rd(0x29));
      m.wr(0x23, 0);
      return y;
    }
    x = (x + 1) & 0xFF;
    y = (y - 1) & 0xFF;
  } while (y !== 0);
  return y;                                             // full: $23 b7 stays set
}

/** ItemGetHook 14:826F ($23 = item; chains the record's `next` item). Returns the Y register. */
function itemGetHook(m) {
  for (;;) {
    const item = m.rd(0x23);
    m.wr(0x23, item | 0x80);
    const x = (item << 1) & 0xFF;
    m.wr(0x24, cpuRd(m, 0x9B00 + x));                   // ItemGetTable
    m.wr(0x25, cpuRd(m, 0x9B01 + x));
    m.wr(0x29, cpuRd(m, 0x9D66 + item));                // ItemStoreValue
    let y = invStoreItem(m);
    if (m.rd(0x23) & 0x80) return y;
    y = flagApply(m, npcResult(m, 2));
    const next = indY(m, 0x24, y);
    if (next & 0x80) return y;
    m.wr(0x23, next);
  }
}

/** ItemHook2 14:80E3: EventTriggerTable 15:A17A[id & $7F]; all conditions true -> result + set-list. Returns Y. */
function itemHook2(m) {
  const x = (m.rd(0x23) << 1) & 0xFF;                   // the CMP #$86 / BCC is a no-op
  m.wr(0x23, 0xFF);
  m.wr(0x24, cpuRd(m, 0xA17A + x));
  m.wr(0x25, cpuRd(m, 0xA17B + x));
  let y = 0, ok;
  do {
    [ok, y] = flagTest(m, y);
    if (!ok) { m.wr(0x20, 0); m.wr(0x21, 0); return y; }
  } while (!(m.rd(0x26) & 0x80));
  m.wr(0x23, 0);
  return flagApply(m, npcResult(m, y));                 // 810F JSR NpcResult, falls into NpcFlagApply
}

/** InvRemoveItem 14:8354 ($23 = item). ROM quirk: the shift loop never INXes, so it only rewrites slot X
 *  (from X+1, Y-1 times) and then stores $FF there: the later slots are NOT shifted down. Returns Y. */
function invRemoveItem(m) {
  const it = m.rd(0x23), t = (it << 1) & 0xFF;
  m.wr(0x24, cpuRd(m, 0x9B00 + t));                     // ItemGetTable
  m.wr(0x25, cpuRd(m, 0x9B01 + t));
  let x = indY(m, 0x24, 0), y = indY(m, 0x24, 1);
  while (m.rd(INV + x) !== it) {
    x = (x + 1) & 0xFF;
    y = (y - 1) & 0xFF;
    if (y === 0) return 0;                              // 8376: not found
  }
  for (; y !== 1; y = (y - 1) & 0xFF) m.wr(INV + x, m.rd(INV + 1 + x));
  m.wr(INV + x, 0xFF);
  m.wr(0x642A + (x >> 3), 0xFF);
  m.wr(0x0715, 0);
  return y;
}

// ItemUseHandlers 14:8399[item] (hook 1). Each takes/returns Y (index into the ItemUseTable record at $24).
const useFail = (m, y) => { m.wr(0x23, 0xFF); return y; };              // 84DB
/** UseTouchMatch 14:8497: record param/b2 == the touched spawn ($0643) -> [match, Y] */
function useTouchMatch(m, y) {
  const x = m.rd(0x0643);
  const p = indY(m, 0x24, y); y = (y + 1) & 0xFF;
  if (p !== m.rd(SPAWN_P + x)) return [false, y];
  const b = indY(m, 0x24, y); y = (y + 1) & 0xFF;
  return [b === m.rd(0x0540 + x), y];
}
function useTouchOrFail(m, y) {                         // 8491
  const [ok, y2] = useTouchMatch(m, y);
  return ok ? y2 : useFail(m, y2);
}
/** UseFindSlotObj 14:8469: an on-screen object in 0D-1F with the record's param/b2 */
function useFindSlotObj(m, y) {
  m.wr(0x26, indY(m, 0x24, y)); y = (y + 1) & 0xFF;
  m.wr(0x27, indY(m, 0x24, y)); y = (y + 1) & 0xFF;
  for (let x = 0x0D; x < 0x20; x++) {
    if (m.rd(0x0380 + x) & 0x80) continue;
    if (m.rd(SPAWN_P + x) === m.rd(0x26) && m.rd(0x0540 + x) === m.rd(0x27)) return y;
  }
  return useFail(m, y);
}
function useBowCheck(m, y) {                            // 8451 (UseFail inside UseFindSlotObj returns here)
  y = useFindSlotObj(m, y);
  const l = m.rd(0x07D7);
  return l === 0 || (l & 0x80) ? useFail(m, y) : y;
}
function useStatus(m, y, want) {                        // 8524/852F/853A/854A -> 8553
  if ((m.rd(0x0710) & 0x0F) !== want) return useFail(m, y);
  if (want === 2) m.wr(0x05A0, 0);                      // Fruit of Lime also clears obj_freeze
  m.wr(0x0710, m.rd(0x0710) & 0xF0);
  m.wr(0x2F, y);
  playSound(m, 0x25, y);
  return y;
}
function useTouchList(m, ys) {                          // 84A9 / 859E: try records at Y = 0, 6, 12, ...
  for (const y0 of ys.slice(0, -1)) { const [ok, y] = useTouchMatch(m, y0); if (ok) return y; }
  return useTouchOrFail(m, ys[ys.length - 1]);
}
const USE_HANDLERS = {
  0x84DB: useFail,
  0x8491: useTouchOrFail,
  0x8564: (m, y) => y,                                  // UseWarpBoots: RTS (consumed)
  0x8450: (m, y) => y,                                  // item $26: RTS
  0x84E0: (m, y) => {                                   // Medical Herb: touch match, else HP +32
    const [ok, y2] = useTouchMatch(m, y);
    if (ok) return y2;
    y = 6;
    let a = 0x20 + m.rd(0x03C1);
    if (a > 0xFF) a = 0xFF;
    if (a >= m.rd(0x03C0)) a = m.rd(0x03C0);
    m.wr(0x03C1, a);
    m.wr(0x2F, y);
    playSound(m, 0x25, y);
    return y;
  },
  0x8507: (m, y) => {                                   // Fruit of Power: MP +32
    let a = m.rd(0x0708) + 0x20;
    if (a > 0xFF) a = 0xFF;
    if (a >= m.rd(0x0709)) a = m.rd(0x0709);
    m.wr(0x0708, a);
    return y;
  },
  0x851D: (m, y) => { m.wr(0x0708, m.rd(0x0709)); return y; },   // Magic Ring
  0x8524: (m, y) => useStatus(m, y, 3),
  0x852F: (m, y) => useStatus(m, y, 1),
  0x853A: (m, y) => useStatus(m, y, 2),
  0x854A: (m, y) => useStatus(m, y, 4),
  0x8565: (m, y) => useTouchOrFail(m, y),               // 8565 (same as 8491: mismatch -> 8579 UseFail)
  0x84B3: (m, y) => {                                   // UseInsectFlute
    y = (y + 1) & 0xFF;
    if (m.rd(R.area_id) !== 0x1A || (m.rd(0x038D) & 0x80) || m.rd(0x04AD) === 0 || m.rd(0x70) < 0x0A) return useFail(m, y);
    m.wr(0x04AD, 0xE2);
    return y;
  },
  0x859E: (m) => useTouchList(m, [0, 6, 0x0C, 0x12]),
  0x84A9: (m) => useTouchList(m, [0, 6]),
  0x845F: (m, y) => {                                   // area byte
    const a = indY(m, 0x24, y);
    m.wr(0x26, a);
    y = (y + 1) & 0xFF;
    return a === m.rd(R.area_id) ? y : useFail(m, y);
  },
  0x856B: (m, y) => {                                   // Shell Flute: areas $60/$64/$68 + flag
    if (![0x60, 0x64, 0x68].includes(m.rd(R.area_id))) return useFail(m, y);
    const [ok, y2] = flagTest(m, y);
    return ok ? y2 : useFail(m, y2);
  },
  0x8585: (m, y) => {                                   // Glowing Lamp: Broken Statue $38 -> Statue of Gold $3A
    for (let x = 0x18; x >= 0; x--) {
      if (m.rd(0x643F + x) === 0x38) { m.wr(0x643F + x, 0x3A); playSound(m, 0x68, y); return y; }
    }
    return useFail(m, y);
  },
  0x84D0: (m, y) => {
    const [ok, y2] = useTouchMatch(m, y);
    if (!ok) return useFail(m, y2);
    m.wr(0x62F2, 0xFF);
    return y2;
  },
  0x8439: (m, y) => (m.rd(R.area_id) === 0x62 ? useFail(m, y) : useTouchOrFail(m, y)),
  0x8451: useBowCheck,
  0x8442: (m, y) => {                                   // Bow of Truth
    y = useBowCheck(m, y);
    return m.rd(0x060D) === 0 || m.rd(R.area_id) !== 0xA6 ? useFail(m, y) : y;
  },
};

/** ItemUseHook 14:831E ($23 = item). Returns the Y register. */
function itemUseHook(m) {
  const it = m.rd(0x23), x = (it << 1) & 0xFF;
  m.wr(0x24, cpuRd(m, 0x9BE2 + x));                     // ItemUseTable
  m.wr(0x25, cpuRd(m, 0x9BE3 + x));
  m.wr(0x26, cpuRd(m, 0x8399 + x));                     // ItemUseHandlers
  m.wr(0x27, cpuRd(m, 0x839A + x));
  const h = m.rd(0x26) | m.rd(0x27) << 8, fn = USE_HANDLERS[h];
  let y;
  if (fn) y = fn(m, 0);
  else { unported(m, `ItemUseHandler 14:${h.toString(16).toUpperCase()}`); y = useFail(m, 0); }
  if (m.rd(0x23) === 0xFF) return y;
  y = flagApply(m, npcResult(m, y));
  if (m.rd(0x23) === 0x36) return y;                    // Shell Flute is kept
  return invRemoveItem(m);
}

const HOOKS = [itemGetHook, itemUseHook, itemHook2];

/** Area5FIrqOff 30:C89E */
export function area5FIrqOff(m) { if (m.rd(R.area_id) === 0x5F) m.wr(R.irq_mode, 0); }

/** ItemEventHook 30:D834 (A = hook). Returns the Y register after it. */
export function* itemEventHook(m, n) {
  m.wr(0x23, m.rd(SPAWN_P + m.rd(TALK_SLOT)));
  switchBank16(m, 7);
  m.wr(0x10, 0x4C);
  m.wr(0x11, cpuRd(m, 0xD885 + n * 2));
  m.wr(0x12, cpuRd(m, 0xD886 + n * 2));
  const hook = HOOKS[n];
  if (!hook) { unported(m, `ItemEventHook(${n})`); m.wr(0x23, 0xFF); m.wr(R.sub_state, 8); return 0; }
  const y = hook(m);
  if (!(m.rd(0x23) & 0x80) && (m.rd(0x20) | m.rd(0x21))) {
    const a22 = m.rd(0x22), a23 = m.rd(0x23);
    playPaletteRestore(m);
    area5FIrqOff(m);
    playSound(m, 0x29, y);
    yield* showMessagePacked(m);
    yield* statRecalc(m, 0);                            // X = 0 after ShowMessage's OamClipTop loop
    yield* restoreIfArea5F(m);
    m.wr(0x23, a23);
    m.wr(0x22, a22);
  }
  m.wr(R.sub_state, 8);
  return y;
}

// ---------------------------------------------------------------- EventActionTable 30:D563
/** GiveItemA 30:D22B: A -> $057F, GiveItem with $0623 = $1F (restored after) */
function* giveItemA(m, a) {
  m.wr(0x057F, a);
  const s = m.rd(TALK_SLOT);
  m.wr(TALK_SLOT, 0x1F);
  yield* giveItem(m);
  m.wr(TALK_SLOT, s);
}

const nop = function* () {};
const EVENT_ACTIONS = {
  0: nop, 3: nop, 5: nop, 18: nop, 26: nop, 4: nop,     // D5A3 / D653 RTS
  8: (m) => giveItemA(m, 0x42),                          // D654 Paralysis
  10: function* (m, y) { playSound(m, 0x6E, y); },       // D63A
  11: (m) => giveItemA(m, 0x46),                         // D6D8 Barrier
  12: function* (m) { yield* giveItemA(m, 0x47); npcVanish(m, 0x0E); },   // D6DD Change, then D31F Y = $0E
  13: (m) => giveItemA(m, 0x3E),                         // D6E7 Bow of Moon
  15: function* (m) {                                    // D70D Refresh into the trigger slot, JMP GiveItem
    m.wr(SPAWN_P + m.rd(TALK_SLOT), 0x41);
    yield* giveItem(m);
  },
  16: (m) => TALK_ACTIONS[8](m),                         // D63F = talk act 8
  22: function* (m) { m.wr(R.sub_state, 0x1A); },        // D78C
  25: function* (m) {                                    // D7B7 EvActPushBack (carry INCs the low byte: ROM quirk)
    for (const a of [0xB0, 0xB1]) {
      const s = m.rd(a) + 8;
      m.wr(a, s & 0xFF);
      if (s > 0xFF) inc(m, a);
    }
  },
  29: function* (m) {                                    // D805 EvActConsume
    m.wr(ACTIVE + m.rd(TALK_SLOT), 0);
    if (m.rd(R.area_id) !== 0xF2) return;
    m.wr(0x7D, 0x80); m.wr(0xBD, 0x40);
    m.wr(0x10, 0x0D); m.wr(0x11, 0x88);
    farObjInit(m);
  },
  30: function* (m) { m.wr(R.sub_state, 0x1B); return PLA; },   // D827 EvActEnding
  31: function* (m) { m.wr(0x04AD, 0x55); },             // D82E
};

/** EventActionDispatch 30:D552 (JMP ($10) with $10/$11 = EventActionTable[$22]) */
function* eventActionDispatch(m) {
  const a = (m.rd(0x22) << 1) & 0xFF;
  m.wr(0x10, cpuRd(m, 0xD563 + a));
  m.wr(0x11, cpuRd(m, 0xD564 + a));
  const fn = EVENT_ACTIONS[a >> 1];
  if (fn) return yield* fn(m, a);
  unported(m, `EventActionTable ${a >> 1} (30:${(m.rd(0x10) | m.rd(0x11) << 8).toString(16).toUpperCase()})`);
  return undefined;
}

// ---------------------------------------------------------------- ItemUseMenu (sub_state 6) / ShowPendingMessage (16)
/** 30:D4F2: flute SFX, slot 3 param = item, ItemEventHook(1), EventActionDispatch or ItemUseFail 30:D526 */
function* itemUseRun(m, yIn) {
  const sel = m.rd(0x0715);
  if (sel === 0x0B || sel === 0x0C || sel === 0x15) playSound(m, 0x48, yIn);   // flutes
  m.wr(SPAWN_P + 3, (sel + 0x1C) & 0xFF);
  m.wr(TALK_SLOT, 3);
  inc(m, 0x08);
  m.wr(R.sub_state, 8);
  const y = yield* itemEventHook(m, 1);
  if (!(m.rd(0x23) & 0x80)) { yield* eventActionDispatch(m); return; }
  m.wr(R.sub_state, 0x10);                              // ItemUseFail: pending msg 32.19 "Nothing happens."
  m.wr(0x06C3, 0x13);
  m.wr(0x06E3, 0x20);
  playSound(m, 0x4E, y);
}

/** ItemUseMenu 30:D497 (B with use_item_sel $0715 = item id - $1C). yIn = Y at the dispatch. */
export function* itemUseMenu(m, yIn = m.rd(0x010F)) {
  const sel = m.rd(0x0715);
  if (sel === 0x0A && m.rd(0x0743) === 0) {             // Opel Statue used by hand: refused, SubstPlay this frame
    inc(m, 0x08);
    m.wr(R.sub_state, 8);
    const g = mainState1(m);
    if (g) yield* g;
    return;
  }
  m.wr(0x0743, 0);
  const flash = m.rd(0x0710);
  if (sel === 0x20 && (flash & 0x80) && (flash & 0x30) !== 0x20) { m.wr(R.sub_state, 8); return; }   // Kirisa Plant
  if (sel !== 0x08) { yield* itemUseRun(m, yIn); return; }
  if (flash & 0x80) { m.wr(R.sub_state, 8); return; }   // Warp Boots blocked
  const area = m.rd(R.area_id);
  yield* substTeleportMenu(m, yIn);                     // 30:D4DC (s143; sub_state 6 -> no SpendMp)
  if (area === m.rd(R.area_id)) return;                 // refused / cancelled / same town -> 30:D45B RTS
  yield* itemUseRun(m, yIn);                            // 30:D4E7
  setR7(m, 0x17);                                       // D4EA SetR7($17) (SaveAllowed reads 23:BF00), JMP 23:BC00
  if (saveGame(m)) { yield; yield SPIN; }
}

/** ShowPendingMessage 30:D085 (sub_state $10): message $06C3/$06E3 after a 32-frame wait */
export function* showPendingMessage(m) {
  area5FIrqOff(m);
  const irq = m.rd(R.irq_mode);
  yield* waitFrames(m, 0x20);                           // Y = 0 after
  if (m.rd(0x06C3) === 0x0D && m.rd(0x06E3) === 0x20) {
    m.wr(0x0102, m.rd(0x0102) | 0x80);
    playSound(m, 0x93, 0);
  } else playSound(m, 0x29, 0);
  m.wr(MSG, m.rd(0x06C3));
  m.wr(GROUP, m.rd(0x06E3));
  yield* showMessage(m);
  m.wr(R.irq_mode, irq);
  yield* restoreIfArea5F(m);
}

// ---------------------------------------------------------------- GiveItem
/** TrapChest 30:D3DA */
function trapChest(m) {
  m.wr(0x10, m.rd(TALK_SLOT));
  m.wr(0x11, 0x73);
  objInit(m);
  m.wr(R.sub_state, 8);
}

/** GiveItemFull 30:D47C (Y = 0: InvStoreItem's count loop ran out) */
function* giveItemFull(m, y) {
  if (m.rd(0x08) & 0x0F) return;
  playPaletteRestore(m);
  m.wr(MSG, 0x0F);
  m.wr(GROUP, 0x20);
  playSound(m, 0x29, y);
  yield* showMessage(m);
}

/** 30:D539: invisible event trigger */
function* giveTrigger(m) {
  inc(m, R.sub_state); inc(m, R.sub_state);             // 9: ShowMessage blinks the window
  inc(m, 0x08);
  yield* itemEventHook(m, 2);
  if (!(m.rd(0x23) & 0x80)) {
    if ((yield* eventActionDispatch(m)) === PLA) return;
  }
  m.wr(R.sub_state, 8);
  const g = mainState1(m);                              // D54F JMP MainStateGameplay
  if (g) yield* g;
}

/** GiveItem 30:D3EB */
export function* giveItem(m) {
  let y = m.rd(TALK_SLOT);
  const id = m.rd(SPAWN_P + y);
  m.wr(CUR_ITEM, id);
  if (id >= 0x80) { yield* giveTrigger(m); return; }
  if (id === 0x70) { trapChest(m); return; }
  const hy = yield* itemEventHook(m, 0);
  if (m.rd(0x23) & 0x80) { yield* giveItemFull(m, hy); return; }
  swordPowerLevel(m);                                   // D408: Y result unused
  y = m.rd(TALK_SLOT);
  const raw = m.rd(SPAWN_P + y);
  if (raw >= 0x50) { y = raw; m.wr(CUR_ITEM, cpuRd(m, 0xD40C + raw)); }   // ItemRemapTable (name only)
  if ((m.rd(CUR_ITEM) & 0xF0) === 0x40) {
    m.wr(0x0102, m.rd(0x0102) | 0x80);
    playSound(m, 0x93, y);
  } else playSound(m, 0x3F, y);
  m.wr(GROUP, 0x20);
  m.wr(MSG, 0x0C);
  const a22 = m.rd(0x22);
  yield* showMessage(m);
  playPaletteRestore(m);
  m.wr(0x22, a22);
  m.wr(ACTIVE + m.rd(TALK_SLOT), 0);
  if ((yield* eventActionDispatch(m)) === PLA) unported(m, 'EventActionTable 30 after an item pickup');
}
