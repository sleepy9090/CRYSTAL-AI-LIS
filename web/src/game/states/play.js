// re-c web port — MainStateGameplay 30:CAB6 (main_state 1): the sub_state dispatcher (session 88).
// SubstateHandlerTable 30:CADE (ptr16 x 40) -> $10/$11, SubstateBankTable 30:CB2E -> SwitchBank16, JMP ($0010).
// Only the ported substates are in SUBSTATES; the others log once and do nothing (gameplay = "Web fix:
// gameplay frames"). REVERSE.md "State Machine" + "Death / game over (session 88)".
//
// s110: SubstPlay 30:CB62 (sub_state 8) = one gameplay frame:
//   ReadPads -> PlayDeathMenuCheck 30:CB84 (HP 0 -> sub 3; START -> PauseScreen 15:BC40; SELECT -> sub $12;
//   pad-2 debug pause / area cycler) -> PlayerStatusTick 31:EF55 -> ObjUpdateAll -> CameraTick (exit -> the
//   frame ends) -> sprite pass 28:800E -> ObjCollideAll 26:8F6E (s113, obj/collide.js) -> SubstPlayTail
//   31:E0B8 (BgChrAnimTick 31:E75E + the 16-frame respawn tick). REVERSE.md "Rest of PlayerStatusTick".
import { R } from '../ram.js';
import { cpuRd, setR6, switchBank16 } from '../prg.js';
import { playerDeath, paletteBuildRom, waitFrame, waitFrames, hudRedraw, hudHpBar } from './death.js';
import { pauseScreen, pauseWaitEntry } from './pause.js';
import { substEquipMenu } from './equip.js';
import { substRestoreScreen } from './saveload.js';
import { substPawnShop, substArmorShop, substToolShop, substInn, substShopEmpty } from './shop.js';
import { substNewGameInit, substAreaEnter } from './areaenter.js';
import { readPads, readPadsMenu } from '../pads.js';
import { playSound } from '../playsound.js';
import { objUpdateAll } from '../obj/objects.js';
import { spriteDraw } from '../obj/sprites.js';
import { objCollideAll } from '../obj/collide.js';
import { cameraTick } from '../world/camera.js';
import { npcTalk, TALK_ACTIONS } from './talk.js';
import { substWakeCutscene } from './wake.js';
import { giveItem, itemUseMenu, showPendingMessage, area5FIrqOff, EVENT_ACTIONS } from './giveitem.js';
import { substTeleportMenu } from './teleport.js';
import { substTelepathySelect } from './telepathy.js';
import { substChangeForm } from './changeform.js';
import { substCureMagic } from './cure.js';
import { substFormRevert } from './formrevert.js';
import { swordRaisePose } from './swordraise.js';
import { substMesiaReunion } from './mesia.js';
import { substFinalSceneReset } from './finalscene.js';

function unported(m, what) {
  if (!m.unported.has(what)) { m.unported.add(what); m.log(`${what} not ported yet`); }
}

/** SubstPlay 30:CB62 */
function* substPlay(m) {
  const y = readPads(m);
  const r = yield* playDeathMenuCheck(m, y);
  if (r === true) return;                               // PLA PLA: the frame ends here
  yield* substPlayBody(m, r);
}

/** 30:CB68-CB81: everything after PlayDeathMenuCheck; y = the Y register (PlaySound saves it in $010F) */
function* substPlayBody(m, y) {
  // s211: m.passStage names the main-loop stage for web/test/pass_count.mjs (= tools.emu.pass_cost's columns)
  yield* playerStatusTick(m, y);                        // CB68
  m.passStage = 'upd';
  yield* objUpdateAll(m);                               // CB6B
  m.passStage = 'cam';
  if (yield* cameraTick(m)) return;                     // CB6E (ExitLookup PLAs CameraTick's caller)
  m.passStage = 'spr';
  switchBank16(m, 0x0E);                                // CB71
  spriteDraw(m);                                        // CB76 JSR 28:800E
  switchBank16(m, 0x0D);                                // CB79
  m.passStage = 'col';
  yield* objCollideAll(m);                              // CB7E JSR 26:8F6E (s113 obj/collide.js)
  substPlayTail(m);                                     // CB81 JMP 31:E0B8
}

/** PlayDeathMenuCheck 30:CB84. Returns true when it PLAs SubstPlay's return address (the frame ends),
 *  else the Y register SubstPlay goes on with. */
function* playDeathMenuCheck(m, y0) {
  let y = y0;
  if (m.rd(0x03C1) === 0) { m.wr(R.sub_state, 3); return true; }             // player_hp = 0 -> death
  if (m.rd(R.pad_act) & 0x10) {                                              // START
    playPaletteRestore(m);
    if (m.rd(R.area_id) === 0x5F) m.wr(R.irq_mode, 0);
    switchBank16(m, 0x07);
    y = yield* pauseScreen(m);                                               // CBA8 JSR 15:BC40
    if (m.rd(R.area_id) === 0x5F) return yield* substRestoreScreen(m);       // CBB1 JMP (RTS -> CB68)
    return y;
  }
  if (m.rd(R.pad_new) & 0x20) { m.wr(R.sub_state, 0x12); return true; }     // SELECT -> menu
  if (m.rd(R.pad_new + 1) & 0x40) return yield* substDebugPause(m);         // pad 2 B
  if (m.rd(R.pad_prev) === 0xC0 && (m.rd(R.pad_new + 1) & 0x80)) {          // pad 1 A+B held + pad 2 A
    const i = m.rd(0x0780) & 0x0F;
    m.wr(R.area_id, cpuRd(m, 0xCBEC + i));                                   // AreaByIndexTable (debug cycler)
    m.wr(0x6D, 0);                                                           // area_entry
    m.wr(0x0780, (m.rd(0x0780) + 1) & 0xFF);
    m.wr(R.sub_state, 1);
    return true;
  }
  return y;
}

/** SubstDebugPause 30:CBFC (pad 2 B freeze until pad 2 B again), then JMP ReadPads -> back into SubstPlay */
function* substDebugPause(m) {
  do {
    yield* waitFrame(m);
    switchBank16(m, 0x0E); spriteDraw(m); switchBank16(m, 0x0D);           // FarCall_B14_800E
    readPadsMenu(m);
  } while (!(m.rd(R.pad_new + 1) & 0x40));
  m.wr(R.sub_state, 8);
  yield* waitFrames(m, 8);
  return readPads(m);
}

const OBJ1_FLASH = 0x0710, ACCESSORY = 0x0716, ARMOR = 0x0713, JUMP = 0x0620, HP = 0x03C1, HP_MAX = 0x03C0;
const MP = 0x0708, MP_MAX = 0x0709, BG_ATTR = 0x0641, RIDE = 0x0541, INVULN = 0x071A, FLAGS_S01 = 0x0381;

/** PlayerStatusTick 31:EF55-F0A3 (y = the Y register for PlaySound; X is $FF from ReadPads, 0 after damage) */
function* playerStatusTick(m, y) {
  const f = m.rd(0x08);
  let x = 0xFF;
  const status = () => m.rd(OBJ1_FLASH) & 0x0F;
  let hit = false;
  if (((f << 1) & 0xFF) === 0 && status() === 3) hit = (f & 0x1F) === 0;   // poison, every 128 frames
  else if (m.rd(ACCESSORY) !== 0x13 && m.rd(JUMP) === 0 && ((m.rd(0x0561) | m.rd(BG_ATTR)) & 0x80)) {
    hit = (f & 0x1F) === 0;                                                // damage floor
  } else if (m.rd(R.area_id) === 0x1A && m.rd(ACCESSORY) !== 0x0D) hit = (f & 7) === 0;
  if (hit) {                                                               // EF92
    const hp = m.rd(HP) - 4;
    m.wr(HP, hp < 0 ? 0 : hp);
    playSound(m, 0x36, y);
    for (x = 3; x > 0; x--) m.wr(0x6150 + x, 0x30);                        // white BG flash; X ends 0
    yield* waitFrame(m);
    setR6(m, 0x1A);
    yield* hudHpBar(m);                                                    // 26:8CC0 (keeps X)
    paletteBuildRom(m, 0);                                                 // 26:8C0E
  }
  setR6(m, 0x1A);                                                          // EFBC
  if (status() === 1 && m.rd(0x06C0) === 4) {                              // paralysis: no sword charge
    m.wr(0x06C0, 0);
    playSound(m, 0x31, y);
  }
  if (status() === 2) {                                                    // EFDB stone
    if (m.rd(0x05A0) === 0) {
      m.wr(OBJ1_FLASH, m.rd(OBJ1_FLASH) & 0xF0);
      m.wr(0x07E4, 0);
      paletteBuildRom(m, 0);
    } else if (m.rd(0x0712) === 5 && (m.rd(R.pad_new) & 0x80) && m.rd(R.sub_state) === 8) {
      setR6(m, 0x1A);
      // SpendMp 26:9AE8 ($18): MP 0 or short -> sub_state $1C and PLA PLA = return from PlayerStatusTick
      m.wr(0x1F, 0x18);
      const mp = m.rd(MP);
      if (mp === 0 || mp < 0x18) { m.wr(R.sub_state, 0x1C); return; }
      m.wr(MP, mp - 0x18);
      yield* hudRedraw(m, 4, x);
      m.wr(R.sub_state, 0x18);
    }
  }
  if (m.rd(ACCESSORY) === 0x11 && (m.rd(R.pad_dir) & 0x80) && (f & 0x3F) === 0 && m.rd(MP) !== m.rd(MP_MAX)) {
    m.wr(MP, (m.rd(MP) + 1) & 0xFF);                                       // F036 MP regen
    yield* hudRedraw(m, 4, x);
  }
  if (m.rd(ARMOR) === 8 && (m.rd(R.pad_dir) & 0x80) && m.rd(HP_MAX) !== m.rd(HP) && (f & 0x0F) === 0) {
    m.wr(HP, (m.rd(HP) + 1) & 0xFF);                                       // F057 HP regen
    yield* hudHpBar(m);
  }
  if (m.rd(R.area_id) === 0x52) m.wr(0x04FE, (m.rd(0x04FE) - 1) & 0xFF);
  if (m.rd(JUMP) === 0 && (m.rd(BG_ATTR) & 1) && m.rd(RIDE) === 0) {      // F068 pit fall
    m.wr(R.sub_state, 1);
    m.wr(0x6D, (m.rd(0x6D) & 0x1F) | 0x60);                                // area_entry -> pit-fall entry
    m.wr(BG_ATTR, 0);
  }
  const t = m.rd(INVULN);                                                  // F089
  if (t !== 0) {
    m.wr(INVULN, t - 1);
    m.wr(FLAGS_S01, m.rd(FLAGS_S01) | 0x04);
  } else m.wr(FLAGS_S01, m.rd(FLAGS_S01) & 0xFB);
}

/** SubstPlayTail 31:E0B8: BgChrAnimTick + every 16 frames slot $0D + ($08 >> 4): if empty, look for its area
 *  record entry with b1 b7 (respawning) -> AreaSpawnEntry, kept only when the new object is off screen. */
function substPlayTail(m) {
  if (m.rd(0x07D7) !== 0) return;                                          // boss_cam_lock
  bgChrAnimTick(m);
  const f = m.rd(0x08);
  if (f & 0x0F) return;
  const slot = (f >> 4) + 0x0D;
  m.wr(0x13, slot);
  if (m.rd(0x04A0 + slot) !== 0) return;
  switchBank16(m, 6);                                                      // E0D8 SwitchBank16_6
  const id = m.rd(R.area_id), t = (id & 0x80) ? 0x9301 : 0x9201, yy = (id << 1) & 0xFF;
  m.wr(0x10, cpuRd(m, t + yy));
  m.wr(0x11, cpuRd(m, t + yy + 1));
  if (!(m.rd(0x11) & 0x80)) return;                                        // no area record
  const p = m.rd(0x10) | m.rd(0x11) << 8;
  for (let y = 5, x = 0x0D; x !== 0x1D; y += 4, x++) {
    const b0 = cpuRd(m, p + y);
    if ((b0 & 0xF0) === 0xF0) return;                                      // $Fx (FE/FF) ends the scan
    if (x !== slot) continue;
    const b1 = cpuRd(m, p + y + 1);
    if (!(b1 & 0x80)) return;                                              // not a respawning entry
    m.spawnEntry(x, y);                                                    // E110 AreaSpawnEntry (s112)
    if (!objOffscreen(m, x)) m.wr(0x04A0 + x, 0);                          // E113: on screen -> undo
    return;
  }
}

/** 31:E12B: carry = slot x not on the camera's screen (x hi / y hi differ) */
function objOffscreen(m, x) {
  const xb = m.rd(0x70 + x) < m.rd(0x02) ? 1 : 0;
  if (((m.rd(0x90 + x) - m.rd(0x03) - xb) & 0xFF) !== 0) return true;
  const yb = m.rd(0xB0 + x) < m.rd(0x04) ? 1 : 0;
  return ((m.rd(0xD0 + x) - m.rd(0x05) - yb) & 0xFF) !== 0;
}

/** BgChrAnimTick 31:E75E: $62FE != 0 -> chr_r1_play = BgChrAnimTable 31:E779[$62FE*8 + (($08>>3)&7)] */
function bgChrAnimTick(m) {
  const b = (m.rd(0x62FE) << 3) & 0xFF;
  m.wr(0x10, b);
  if (b === 0) return;
  m.wr(0x07F1, cpuRd(m, 0xE779 + (((m.rd(0x08) >> 3) & 7) | b)));
}

/** SubstOutOfMp 30:CC17 (sub_state 28, set by SpendMp 26:9AF7 / MagicOutOfMp 27:A15C): Area5FIrqOff,
 *  SFX $4E, sub_state $10 with pending message 32.14 ($06C3 = $0E, $06E3 = $20), RTS (s151) */
export function substOutOfMp(m, y = 0) {
  area5FIrqOff(m);
  playSound(m, 0x4E, y);                              // y = the Y register at the dispatch (as states/cure.js)
  m.wr(R.sub_state, 0x10);
  m.wr(0x06C3, 0x0E);
  m.wr(0x06E3, 0x20);
}

const SUBSTATES = new Map([
  [0, substNewGameInit],                              // 30:CA5F (s108)
  [1, substAreaEnter],                                // 30:CA2E (s108)
  [3, playerDeath],                                   // 19:B900
  [6, itemUseMenu],                                   // 30:D497 ItemUseMenu + item hook 1 (s141, states/giveitem.js)
  [7, giveItem],                                      // 30:D3EB GiveItem + item hooks 0/2 (s139, states/giveitem.js)
  [8, substPlay],                                     // 30:CB62 (s110)
  [16, showPendingMessage],                           // 30:D085 (s141; ItemUseFail, entity messages)
  [12, pauseScreen],                                 // 15:BC40 (vestigial entry; play calls it directly, s94)
  [13, pauseWaitEntry],                               // 15:BDDB (s142; unreachable, see states/pause.js)
  [17, npcTalk],                                      // 30:D0C8 NpcTalk + ShowMessage (s115, states/talk.js)
  [18, substEquipMenu],                               // 30:D8C7 SELECT menu (s98)
  [19, substWakeCutscene],                            // 30:D9B6 -> 20:9400 new-game wake-up (s138, states/wake.js)
  [20, substTeleportMenu],                            // 30:DB28 magic 4 teleport menu (s143, states/teleport.js)
  [21, substTelepathySelect],                         // 30:DD6B magic 3 telepathy + reply 30:DE04 (s145, states/telepathy.js)
  [23, substChangeForm],                              // 30:DEC1 magic 7 change form (s146, states/changeform.js)
  [24, substCureMagic],                               // 19:BB39 magic 5 / stone cure (s147, states/cure.js)
  [25, substFormRevert],                              // 19:BB9D magic press in a changed form (s148, states/formrevert.js)
  [26, swordRaisePose],                               // 19:BC04 event act 22 (s149, states/swordraise.js)
  [27, substMesiaReunion],                            // 19:BC6B event act 30 (s150, states/mesia.js)
  [28, substOutOfMp],                                 // 30:CC17 SpendMp short of MP -> msg 32.14 (s151)
  [29, substTeleportMenu],                         // 30:DB28 2nd entry (never stored)
  [30, substFinalSceneReset],                         // 19:BDF2 AI $70 phase 10 -> ending (s152, states/finalscene.js)
  [31, substRestoreScreen],                           // 30:CC4D (s120; set by AI $70 phase 2)
  [32, substArmorShop],                               // 30:D985 (s122, states/shop.js; types $42 $48)
  [33, substToolShop],                                // 30:D98E (s122; types $41 $44 $47)
  [34, substInn],                                     // 30:D997 (s123, states/shop.js; types $40 $45 $46)
  [35, substPawnShop],                               // 30:D9AD (s121, states/shop.js; keeper type $43)
  [36, substShopEmpty],                               // 30:D9C1 (s124, states/shop.js; AI $0E type $4E)
]);

/** state entry at 15:BC40 from SubstPlay (30:CBA8 JSR, after PlayPaletteRestore / area $5F irq_mode = 0) */
function* pauseFromPlay(m) {
  let y = yield* pauseScreen(m);
  if (m.rd(R.area_id) === 0x5F) y = yield* substRestoreScreen(m);   // 30:CBB1 JMP; its RTS -> CB68
  yield* substPlayBody(m, y);                         // 30:CBAF RTS -> SubstPlay goes on at CB68
}

/** Routines a tools/emu/web_state.py export may stop at that are NOT reached through the substate dispatch
 *  (JSR from code the port does not run yet). Machine.loadState starts `ENTRY_POINTS[s.until](m)` as the
 *  pending generator. */
export const ENTRY_POINTS = {
  '15:BC40': pauseFromPlay,                           // START in SubstPlay: 30:CBA8 JSR PauseScreen
  // s98: a state stopped at a substate's first instruction is already past this frame's dispatch
  // (DEC $08, PlayPaletteRestore), so it starts the handler itself
  '30:D8C7': substEquipMenu,
  '30:CC4D': substRestoreScreen,                      // s120 (web/test/states/restore5f.json)
  '30:D9AD': substPawnShop,                           // s121 (web/test/states/pawnshop.json)
  '30:D985': substArmorShop,                          // s122 (web/test/states/armorshop.json)
  '30:D98E': substToolShop,                           // s122 (web/test/states/toolshop.json)
  '30:D997': substInn,                                // s123 (web/test/states/inn.json)
  '30:D9C1': substShopEmpty,                          // s124 (web/test/states/shopempty.json)
  '30:D3EB': giveItem,                                // s139 (web/test/states/giveitem.json)
  '30:D497': itemUseMenu,                             // s141 (web/test/states/itemuse_*.json)
  '30:D085': showPendingMessage,                      // s141
  '30:DD6B': substTelepathySelect,                    // s145 (web/test/states/telepathy_magic.json)
  '30:DEC1': substChangeForm,                         // s146 (web/test/states/changeform.json)
  '19:BB39': substCureMagic,                          // s147 (web/test/states/cure.json)
  '19:BB9D': substFormRevert,                         // s148 (web/test/states/formrevert.json)
  '19:BC04': swordRaisePose,                          // s149 (web/test/states/swordraise.json)
  '19:BC6B': substMesiaReunion,                       // s150 (web/test/states/mesia.json)
  '30:CC17': function* (m) { substOutOfMp(m); },      // s151 (web/test/states/outofmp.json; one pass, no wait)
  '19:BDF2': substFinalSceneReset,                    // s152 (web/test/states/finalscene.json)
  // s203: event acts reached by ItemUseRun's JMP 30:D523 (their RTS ends the substate handler); Y = $22 * 2
  '30:D5A4': (m) => EVENT_ACTIONS[1](m, 2),           // EvActThunderWarp (web/test/states/event01.json)
  '30:D5E6': (m) => EVENT_ACTIONS[7](m, 14),          // EvActMesiaMessage (web/test/states/event07.json)
  // s204: frame-waiting acts (web/test/states/eventNN.json, NN = act in decimal)
  '30:D654': (m) => EVENT_ACTIONS[8](m, 16),          // give item $42 Paralysis (event08.json)
  '30:D746': (m) => EVENT_ACTIONS[21](m, 42),         // EvActSpawn9E (event21.json)
  '30:D791': (m) => EVENT_ACTIONS[23](m, 46),         // EvActHistory (event23.json)
  // s205
  '30:D5D6': (m) => EVENT_ACTIONS[2](m, 4),           // slot 13 item + MakeFollower13 (event02.json)
  '30:D70D': (m) => EVENT_ACTIONS[15](m, 30),         // Refresh via GiveItem (event15.json)
  '30:D7FD': (m) => EVENT_ACTIONS[28](m, 56),         // Gas Mask + MakeFollower13 (event28.json)
  // s206
  '30:D6D8': (m) => EVENT_ACTIONS[11](m, 22),         // give item $46 Barrier (event11.json)
  '30:D6DD': (m) => EVENT_ACTIONS[12](m, 24),         // give item $47 Change + NpcVanish slot 14 (event12.json)
  '30:D6E7': (m) => EVENT_ACTIONS[13](m, 26),         // give item $3E Bow of Moon (event13.json)
  '30:D718': (m) => EVENT_ACTIONS[19](m, 38),         // EvActClearSlot13 (event19.json)
  '30:D71F': (m) => EVENT_ACTIONS[20](m, 40),         // EvActClearSlot14 (event20.json)
  // s207: talk acts entered through EventActionDispatch's JMP ($0010) redirected at 30:D560 (Y = act * 2,
  // as after NpcTalk's TAY 30:D115); web/test/states/talkDD.json, DD = act in decimal
  '30:D1EB': (m) => TALK_ACTIONS[0x10](m, 32),        // teach Recover + SpawnFxAtSlot slot 14
  '30:D18E': (m) => TALK_ACTIONS[0x14](m, 40),        // Flight + TalkFullRestore
  '30:D1B5': (m) => TALK_ACTIONS[0x13](m, 38),        // fx at the NPC, AreaLoadA, NpcVanish $0623+1
  '30:D1CA': (m) => TALK_ACTIONS[0x16](m, 44),        // SFX $29, msg 19.0, AreaLoadA, fx at the NPC
  '30:D2AE': (m) => TALK_ACTIONS[0x19](m, 50),        // Shield Ring + slot 14 follower
  '30:D336': (m) => TALK_ACTIONS[0x1A](m, 52),        // Ball of Water, $060D = 4
};

/** PlayPaletteRestore 30:CC2E: slot 4 AI $1A (palette effect) -> clear it, PaletteBuild, irq_mode 2 -> 0 */
export function playPaletteRestore(m) {
  if (m.rd(0x04A4) !== 0x1A) return;
  m.wr(0x04A4, 0);
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);
  if (m.rd(R.irq_mode) === 2) m.wr(R.irq_mode, 0);
}

/** MainStateGameplay 30:CAB6 */
export function mainState1(m) {
  const s = m.rd(R.sub_state);
  if (s !== 8 && s !== 7) playPaletteRestore(m);
  m.wr(0x08, (m.rd(0x08) - 1) & 0xFF);
  m.wr(0x10, cpuRd(m, 0xCADE + s * 2));
  m.wr(0x11, cpuRd(m, 0xCADF + s * 2));
  switchBank16(m, cpuRd(m, 0xCB2E + s));
  const fn = SUBSTATES.get(s);
  if (fn) return fn(m);
  const key = `sub_state ${s}`;
  if (!m.unported.has(key)) { m.unported.add(key); m.log(`${key} (30:CAB6) not ported yet`); }
  return undefined;
}
