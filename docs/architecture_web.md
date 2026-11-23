# re-c (NES) — Web Port Architecture

Written in session 49 as the design of the port; **refreshed in session 221** to match the finished port
(58 modules under `web/src/`, ~15.5k lines; every main state, every non-null substate, IRQ handler and NMI mode is ported).
Companion of `docs/architecture_exe.md` (the ROM side). REVERSE.md stays the source of truth for every routine named
here; each module's header comment names the REVERSE.md section it was ported from. `python3 tools/module_summary.py`
prints every module's header, imports, exports and the ROM addresses it cites (the tables below were checked against it).

---

## 1. Design rules (as built)

1. **Same RAM, same addresses.** Game state lives in `Ram` (`game/ram.js`): `ram` = `$0000-$07FF`, `wram` = `$6000-$7FFF`,
   at the ROM's addresses; names come from `labels.csv` bank `*` rows (`R.irq_mode`, `R.sub_state`, …, `web/data/ram_labels.json`).
   The `--compare` CSV, the save image and RAM-diff checks therefore line up byte for byte with `tools.emu`.
2. **Instruction-level ports.** Routines are ported instruction by instruction, **including the zero-page temps and the
   bank shadows** `$50/$6E/$6F` (`m.prgR6/m.prgR7`) — the routine checks (`obj_check.mjs`, `sprite_check.mjs`) diff the whole RAM + WRAM.
   (The s49 design dropped the bank shadows; that turned out to be wrong because the ROM reads them back.)
3. **No hand-copied ROM data.** Tables come from `web/data/` (`tools/gen_web_data.py`, §5). Most code reads the ROM
   through `prg.js` `cpuRd(m, addr)` on `prg.bin` with the current MMC3 R6/R7 mapping, exactly where the 6502 reads.
   `setR6` / `setR7` / `switchBank16` / `farRestore` keep the mapping state; CHR R0-R5 live in the PPU model.
4. **Logic is DOM-free.** Everything under `web/src/game/`, `web/src/nes/` and `web/src/data.js` runs under Node 18+
   (`web/test/run.mjs`). Only `main.js`, `input.js`, `audio.js` and `save.js` touch the browser.
5. **Frame-exact scheduling without a CPU.** The port does not emulate the 6502. ROM wait loops become generators
   (`function*`) that `yield` where the ROM waits for an NMI; IRQ handler bodies run as PPU split hooks at their scanline;
   CPU overruns (lag frames) come from a cycle model (`lagmodel.js`, §4.3).
6. **ES modules, no bundler, no npm packages**, served by `python3 -m http.server` from the repo root (`/web/index.html`).

---

## 2. Module graph

```
web/index.html
└─ src/main.js                  browser entry: loadData → Machine, canvas blit (integer scale), fixed-step 60.0988 Hz loop
   ├─ src/input.js              keyboard + Gamepad API → button byte (A b7 B b6 Sel b5 St b4 U b3 D b2 L b1 R b0)
   ├─ src/audio.js              AudioOut: WebAudio sink for nes/apu.js samples (resume on a user gesture)
   ├─ src/save.js               battery WRAM ↔ localStorage (loadWram / saveWram / WramSaver)
   ├─ src/data.js               loadData(read): manifest.json + every listed file (sha1 checked); table(name)
   └─ src/game/machine.js       Machine: Ram, Ppu, Apu, IRQ handler table, reset(), loadState(), runFrame()
      ├─ src/nes/ppu.js         register-level PPU: 2 nametables, palette RAM, OAM, MMC3 CHR R0-R5, per-scanline
      │                         split hooks (ppu.splits, ppu.timed(dot, fn) for mid-line writes), 8 sprites/line
      ├─ src/nes/apu.js         2A03 APU: 2 pulse, triangle, noise, DMC; $4000-$4017 writes; sample generator
      ├─ game/ram.js            Ram (rd/wr on RAM/WRAM) + R (named addresses)
      ├─ game/prg.js            prgByte, cpuRd, indY, setR6/setR7, switchBank16, farRestore (31:FFDB)
      ├─ game/boot.js           Reset's cold path: introLeftovers, coldSaveInit, bootSaveCheck, copySramBlock
      ├─ game/mainloop.js       mainStep (MainLoop + MAIN_STATES), nmiWork (NMI), NMI_MODES[10], IRQ handler bodies,
      │                         applyChrPlay / applyChrAlt, scroll writes
      ├─ game/lagmodel.js       per-pass CPU-cycle estimate → lag frames (passLags, startLine, LAG_COEF)
      ├─ game/pads.js           readPads / readPadsMenu (hold / repeat logic $43-$4F)
      ├─ game/palette.js        paletteBuild (26:8C0E/8C6E), paletteUpload (31:F8CB)
      ├─ game/playsound.js      playSound: the RAM side of PlaySound 30:C125 (FIFO $0100-$0108)
      ├─ game/sound/            soundupdate.js (SoundUpdate 31:F7FE) → driver.js (bank 24 driver, real stream bytes)
      ├─ game/states/           main states + substates (§3.2)
      │   play.js               mainState1 (sub_state dispatch, SUBSTATES map), SubstPlay, ENTRY_POINTS
      │   areaenter.js          main states 0 / 8, substates 0 / 1, AreaEnter
      │   title.js              main states 2 / 3 (banks 18/19: title, opening, story, status, name entry)
      │   ending.js             main states 4 / 5 (bank 17)
      │   death.js              PlayerDeath + shared helpers: SPIN / NO_NMI, waitFrame(s), fades, HUD redraw, statRecalc
      │   pause.js equip.js saveload.js shop.js talk.js giveitem.js wake.js teleport.js telepathy.js changeform.js
      │   cure.js formrevert.js swordraise.js mesia.js finalscene.js
      ├─ game/world/            area.js (AreaLoad, k0-k4), spawn.js (spawn entries), bg.js (screen / metatile pipeline),
      │                         bgstream.js (row / column streaming, FullScreenDraw), ppuqueue.js ($6200 ring + flush),
      │                         camera.js (CameraTick/Update, ExitLookup, AreaSubTable)
      └─ game/obj/              objects.js (ObjUpdateAll + AI_TABLE), objinit.js, util.js, move.js, collide.js,
                                sprites.js, player.js, ai_static / ai_walk / ai_field / ai_shot / ai_effect /
                                ai_final / ai_boss / ai_thunder.js (AI handlers by ObjAiTable id)
web/test/                       Node harness + checks (§6)
web/catalog.html, catalog.js, apu_preview.js   asset catalog (session 27), separate from the port
```

**Dependency rule.** `game/*` never imports `main.js` or DOM code. `nes/ppu.js` and `nes/apu.js` know nothing about the
game: the game talks to them only through what the ROM writes (PPU registers, OAM DMA, CHR registers, `$4000-$4017`).
Cross-module helpers flow downward: `states/death.js` is the shared "frame wait / fade / HUD" library, `states/talk.js`
the message-window and text-printer library, `states/pause.js` the PPU-queue-block library; object code imports them.

### 2.1 Machine.runFrame(buttons)

```
runFrame(buttons, buttons2):                 // game/machine.js
  m.buttons = [buttons, buttons2]            // what ReadPadRaw 31:FF28 would assemble (ROM order A b7 … Right b0)
  _lagBegin()                                // per-frame stage buckets for the lag model
  mainStep(m)                                // MainLoop 30:C900: if frame_ready $09 != 0, resume the pending generator
                                             // or start MAIN_STATES[$40]; clear $09 (30:C919)
  _installSplits()                           // irqSplits from the last NMI-mode routine → ppu.splits (latch L → line L+1)
  ppu.renderFrame()                          // frame between two NMIs; split hooks run IRQ handler bodies (HUD + SoundUpdate)
  if lag model says the SubstPlay pass overran: $09 = 1 (NMI skips OAM DMA + scroll copy)
  nmiWork(m)                                 // NMI 31:F3B6 (unless NO_NMI / NMI lost to the HUD IRQ):
                                             //   OAM DMA + scroll copy if $09 == 0, PpuQueueFlush, PaletteUpload,
                                             //   PPUMASK, NmiModeDispatch[irq_mode] (schedules splits), INC $09
  lagLine = startLine(…)                     // where the next pass starts (this NMI's length)
  apu.run(cycles of one frame)               // audio samples
```

`Machine.loadState(s)` starts from a `tools.emu` state JSON (`tools/emu/web_state.py`, `tools/emu/scenario_state.py`):
RAM, WRAM, CIRAM, palette, OAM, PPU and MMC3 registers; it rebuilds the first frame's IRQ split from `$56/$57`
(`loadSplits`, s176) and, for a state stopped at a routine's first instruction, starts `ENTRY_POINTS[s.until]` (`states/play.js`).

---

## 3. EXE → web mapping

Addresses and file offsets of the ROM side: `docs/architecture_exe.md`. One row per ROM routine family.

### 3.1 Frame skeleton, hardware, data

| ROM routine | Address | JS | Module |
|---|---|---|---|
| `Reset` (cold path) + `ColdSaveInit`, `BootSaveCheck`, `CopySramBlock` | `31:F2A4`, `31:F1CD`, `31:F0A4`, `31:F3A2` | `Machine.reset()` → `introLeftovers` (the state the skipped bank-18 intro leaves at f214), `coldSaveInit`, `bootSaveCheck`, `copySramBlock` | `machine.js`, `boot.js` |
| `MainLoop`, `MainStateTable` | `30:C900`, `30:C91F` | `mainStep()`, `MAIN_STATES` (0 1 2 3 4 5 8; 6/7 null) | `mainloop.js` |
| `NMI`, `NmiModeDispatch` / `JmpTable31_F6C2` | `31:F3B6`, `31:F6AD` / `31:F6C2` | `nmiWork()`, `nmiModeDispatch()`, `NMI_MODES[irq_mode]` (0 play, 1 message window, 2 pause / flash, 3 title, 4 dead `NmiMode4` `31:F782`, 5 intro, 6 SELECT menu, 7 = 0 with slot-`$13` scroll, 8 null, 9 = 1) | `mainloop.js` |
| `IRQ`, `SetIrqHandler`, `IrqHandlerTable` | `31:F443`, `31:F424`, `31:F433` | `irqIndex()` / `setIrqHandler()` → `m.irqSplits`; `m.irqHandlers[0-7]` = `irqWrapSplit` `F455`, `irqHud` `F488` (HUD + gameplay `SoundUpdate`), `irqMsgWindowTop` `F4E5`, `irqMsgWindowEnd` `F54B`, `irqRowStretch` `F5E1`, `irqOffSound` `F60E`, `irqIntroSplit` `F691`, `irqMenuChrSplit` `F61A` | `mainloop.js`, `machine.js` |
| `ApplyChrPlay` / `ApplyChrAlt` | `31:F883` / `31:F64F` | `applyChrPlay()` / `applyChrAlt()` | `mainloop.js` |
| `PpuQueueFlush`, `PpuQueueWaitSrc`, `PpuQueueAttrAll`, PPUCTRL NMI on/off | `30:C67D`, `30:C72B`, `30:C739`, `30:C436/C43E` | `ppuQueueFlush`, `queueHasSrc`, `queueAttrAll`, `queuePush`, `ppuCtrlNmiOn/Off` | `world/ppuqueue.js` |
| `PaletteBuild` / `PaletteLoadOne`, `PaletteUpload` | `26:8C0E` / `26:8C6E`, `31:F8CB` | `paletteBuild()`, `paletteUpload()`; ROM-call wrapper + fades `26:8C3D/8C49` = `death.js` `paletteBuildRom`, `fadeOut`, `fadeIn` | `palette.js`, `states/death.js` |
| `ReadPads` / `ReadPadsMenu` / `ReadPadStable` / `ReadPadRaw` | `31:FE80` / `FEE0` / `FF17` / `FF28` | `readPads()`, `readPadsMenu()` | `pads.js` |
| `SwitchBank16`, `SetR6`, `SetR7`, `RestoreBanks` | `30:C40E`, `30:C418`, `30:C427`, `31:FFDB` | `switchBank16`, `setR6`, `setR7`, `farRestore` | `prg.js` |
| far-call trampolines | `31:FF80-FFE2` | plain calls + `farRestore` (e.g. `farObjInit`, `farSpriteDraw`, `menuFarHud`) | callers |
| `PlaySound`, `SoundUpdate` | `30:C125`, `31:F7FE` | `playSound(m, id, y)`, `soundUpdate(m)` (run via `m.soundUpdate` as lag stage `snd`) | `playsound.js`, `sound/soundupdate.js` |
| `SndInit`, `SndCommand`, `SndTick`, `SndChannelTick`, `SndReadOp` + 16 ops, envelopes, vibrato, fade | `24:8020`, `24:8048`, `24:8186`, `24:8276`, `24:8347`, `24:84AF`, `24:86AA`, `24:8609` | `sndInit`, `sndCommand`, `sndTick` | `sound/driver.js` |

### 3.2 States and substates

| ROM routine | Address | JS | Module |
|---|---|---|---|
| state 0 new game / state 8 continue, `GameStartObjs`, `LoadGameAndInit` | `30:C957` / `30:C9DA`, `30:C9FF`, `23:BCDA` | `mainStateNewGame`, `mainStateContinue` | `states/areaenter.js` |
| state 1 `MainStateGameplay`, `SubstateHandlerTable` / `SubstateBankTable`, `PlayPaletteRestore` | `30:CAB6`, `30:CADE` / `30:CB2E`, `30:CC2E` | `mainState1()` → `SUBSTATES` map, `playPaletteRestore` | `states/play.js` |
| states 2 / 3: `MainState2Init`, `MainState3`, `State3ModeTable` / `State3SubTable`, bank-18 screen library (`SceneMetaDraw` `18:80E7`, `WaitFrameReady` `18:8198`, `NtFill`, `NtRectBlit`, menu palettes) | `30:C931`, `18:9FCC`, `19:A015`, `19:A038` / `19:A052` | `mainState2`, `mainState3`, `waitFrameReady`, `ntFill`, `ntRectBlit`, `sceneMetaDraw`, `pal*`, `titleSound` | `states/title.js` |
| states 4 / 5: `EndingInit`, `EndingTick`, `EndingPhaseTable` (30 steps) | `17:A000`, `17:A100`, `17:A166` | `mainState4`, `mainState5` | `states/ending.js` |
| sub 0 `SubstNewGameInit`, sub 1 `SubstAreaEnter`, `AreaEnter` + `AreaEntryTable`, `AreaPitFall`, `AreaEdgeLink`, `PlayerPlaceBody` | `30:CA5F`, `30:CA2E`, `31:E3E9` + `31:E3D9`, `31:E544`, `31:E573`, `26:9CEF` | `substNewGameInit`, `substAreaEnter`, `areaEnter`, `playerPlaceBody` | `states/areaenter.js` |
| sub 3 `PlayerDeath` + `OpelRevive` | `19:B900`, `19:B9B0` | `playerDeath` | `states/death.js` |
| sub 6 `ItemUseMenu` + hook 1 `ItemUseHook`, sub 7 `GiveItem` + hooks 0/2 (`ItemGetHook`, `ItemHook2`), sub 16 `ShowPendingMessage`, `EventActionTable` (31 acts), `Area5FIrqOff` | `30:D497`, `14:831E`, `30:D3EB`, `14:826F`, `14:80E3`, `30:D085`, `30:D563`, `30:C89E` | `itemUseMenu`, `giveItem`, `itemEventHook`, `showPendingMessage`, `EVENT_ACTIONS`, `area5FIrqOff` | `states/giveitem.js` |
| sub 8 `SubstPlay` + `PlayDeathMenuCheck`, `SubstPlayTail`, `BgChrAnimTick`; sub 28 `SubstOutOfMp` | `30:CB62`, `30:CB84`, `31:E0B8`, `31:E75E`; `30:CC17` | `substPlay` → `substPlayBody`, `substPlayTail`, `substOutOfMp` | `states/play.js` |
| sub 12 / 13 `PauseScreen` / `PauseWait`, `PpuQueueBlock`, `ObjSlot1FDraw`, NMI mode 2 | `15:BC40` / `15:BDDB`, `30:C482`, `30:C1AE`, `31:F751` | `pauseScreen` (SubstPlay's START = `pauseFromPlay`), `pauseWait`, `pauseWaitEntry`, `ppuQueueBlock`, `objSlot1FDraw` | `states/pause.js` |
| sub 17 `NpcTalk`, `NpcScript` VM, `TalkActionTable`, `ShowMessage`, bank-20 text printer (`TextWindowOpen`, `TextPrintBox`, `TextPrint` codes), `OamClipTop`, `NpcFlagApply` | `30:D0C8`, `14:8000`, `30:D123`, `30:D354`, `20:8500-8A5C`, `30:C17D` | `npcTalk`, `npcScript`, `TALK_ACTIONS`, `showMessage`, `showMessagePacked`, `textWindowOpen`, `textPrintBox`, `oamClipTop`, `flagTest`/`flagApply` | `states/talk.js` |
| sub 18 `SubstEquipMenu` + bank-16 menu (`EquipMenu`, `MenuPrint`, `MenuSprDraw`, sort, yes/no), NMI mode 6 + IRQ 7 | `30:D8C7`, `16:8000`, `16:8933`, `16:86D3`, `31:F7DB`, `31:F61A` | `substEquipMenu`, `equipMenu`, `menuPrint`, `menuSprDraw`, `sortInventory`, `confirm` | `states/equip.js` |
| save / load page, bank-23 save module, menu exit fade, load tail, `HudWindowSetup`, `HudChargeMeter`, sub 31 `SubstRestoreScreen` | `16:9AEE`, `23:BC09` / `BC8E` / `BCDA`, `30:C80C`, `30:D930`, `19:BA65`, `26:8D59`, `30:CC4D` | `saveLoadPage`, `saveGame`, `loadGame`, `loadGameAndInit`, `saveSlotCopy`, `menuExitFade`, `loadTail`, `hudWindowSetup`, `hudChargeMeter`, `substRestoreScreen` | `states/saveload.js` |
| sub 19 `SubstWakeCutscene` → `WakeCutscene` | `30:D9B6` → `20:9400` | `substWakeCutscene` | `states/wake.js` |
| sub 20 / 29 `SubstTeleportMenu` (magic 4, Warp Boots) | `30:DB28` | `substTeleportMenu` | `states/teleport.js` |
| sub 21 `SubstTelepathySelect` + reply `SubstTelepathyReply` (sub 22, never stored) + `Telepathy` | `30:DD6B`, `30:DE04`, `14:816F` | `substTelepathySelect`, `telepathyReply`, `selectOpen` / `selectExit`, `blackenBg` `30:C874` | `states/telepathy.js` |
| sub 23 `SubstChangeForm` + `ChangeFormApply` | `30:DEC1`, `30:DF2F` | `substChangeForm` | `states/changeform.js` |
| sub 24 `SubstCureMagic` | `19:BB39` | `substCureMagic` | `states/cure.js` |
| sub 25 `SubstFormRevert` | `19:BB9D` | `substFormRevert` | `states/formrevert.js` |
| sub 26 `SwordRaisePose` | `19:BC04` | `swordRaisePose` | `states/swordraise.js` |
| sub 27 `SubstMesiaReunion` + `MesiaScript` / `MesiaSpawnFly` / `MesiaPalFlash` | `19:BC6B`, `19:BCB0` / `BD91` / `BDDC` | `substMesiaReunion`, `palFlash` | `states/mesia.js` |
| sub 30 `SubstFinalSceneReset` → main state 4 | `19:BDF2` | `substFinalSceneReset` | `states/finalscene.js` |
| sub 32-36 armor shop / tool shop / inn / pawn shop / `SubstShopEmpty`, `ShopWalkIn` / `ShopWalkOut`, bank-16 shop screens | `30:D985`, `30:D98E`, `30:D997`, `30:D9AD`, `30:D9C1`, `30:DA0C` / `30:DA72` | `substArmorShop`, `substToolShop`, `substInn`, `substPawnShop`, `substShopEmpty`, `shopWalkIn` / `shopWalkOut` | `states/shop.js` |
| sub 4 `SubstDebugPause` | `30:CBFC` | not in `SUBSTATES` (never stored; pad-2 debug only) | – |

### 3.3 World

| ROM routine | Address | JS | Module |
|---|---|---|---|
| `AreaLoad`, `LoadAreaChrRecord`, `AreaLoadMap` / k1 / k2 / k4 | `31:E14A`, `31:E16D`, `31:E628` / `31:E6B9` / `31:E686` / `31:E702` | `areaLoad`, `loadAreaChr`, `loadAreaMap`, `loadAreaGfx`, `placePlayer`, `restoreVisited`, `areaInfo` (pre-decoded `areas.json`) | `world/area.js` |
| `AreaSpawnEntry` + `SpawnHandlerTable` 0-4, `NpcHidden`, `ChestPresence`, `NpcFlagTest` | `31:E1B6`, `31:E218`, `14:80A0`, `14:85BF`, `14:8135` | `areaSpawnEntry` (= `m.spawnEntry`), `npcHidden`, `chestPresence`, `npcFlagTest` | `world/spawn.js` |
| `ScreenPagePtr`, `ScreenTileSubst`, `MetatileExpand`, `MetatileAttr` | `31:EF36`, `31:ED99`, `31:EDBA`, `31:EDE5` | `screenPage`, `tileSubst`, `metatileTiles`, `metatileAttr`, `renderArea` (static render for `world_check.mjs`) | `world/bg.js` |
| `BgRowRender` / `ScreenRowRender`, `BgColRender`, `FullScreenDraw` | `31:EB91` / `31:EBC1`, `31:EC87`, `30:C831` | `bgRowRender`, `bgColRender`, `fullScreenDraw(m, a, lagRows)` (generator; `FSD_LAG` / `FSD_LAG_WF` = the ROM's lag rows) | `world/bgstream.js` |
| `CameraTick` (+ shake `31:EADF`), `CameraUpdate`, `CameraFollow`, `CameraClampX`, `ExitLookup`, `AreaSubTable` | `31:E8F6`, `31:E90A`, `31:E9AD`, `31:EA72`, `31:EB09`, `30:C7A5` | `cameraTick` (generator; true = exit taken), `cameraUpdate`, `cameraClampX`, `exitLookup`, `exitScan`, `areaSubTable` | `world/camera.js` |
| `MarkScreenVisited` | `30:C75C` | `markScreenVisited` | `obj/ai_effect.js` |

### 3.4 Objects, player, combat

| ROM routine | Address | JS | Module |
|---|---|---|---|
| `ObjUpdateAll`, `ObjAiTable` (128 ids), `HitSetFlag`, `ObjUpdateAi1A` | `30:CCCC`, `27:A314`, `26:9011`, `30:CD7F` | `objUpdateAll` (generator; AI returns true = popped caller, a number = new X), `AI_TABLE`, `hitSetFlag` | `obj/objects.js` |
| `ObjInit` + `FarCall_C25D` (`ObjTypeTable` `13:AC00`) | `30:C25D`, `31:FF80` | `objInit`, `farObjInit` | `obj/objinit.js` |
| bank-26 helpers: `Rand64`, `ObjFindFree`, `ObjPosToTemp` / `TempToObjPos`, `DirToTarget`, `ObjStepVector`, `ObjDespawn`, `SpawnShot`, `ObjBgProbe` | `26:97D7`, `26:98B9`, `26:98A8` / `26:9897`, `26:8409`, `26:8480`, `26:98C7`, `26:972D`, `26:9A30` | `rand64`, `objFindFree`, `objPosToTemp`, `tempToObjPos`, `dirToTarget`, `objStepVector`, `objDespawn`, `objBecomeNext`, `spawnShot`, `objBgProbe` | `obj/util.js` |
| `ObjMoveSlide`, `ObjMoveStep`, `ObjScreenEdge` | `26:98D7`, `26:9947`, `26:99FF` | `objMoveSlide`, `objMoveStep`, `objScreenEdge` | `obj/move.js` |
| player AIs `$01-$03` `PlayerUpdate` / `PlayerMove`, `PlayerMagic`, `SpendMp` | `26:9B45` / `26:9D9A`, `27:A022` | `playerUpdate`, `playerMove`, `playerMoveLocked`, `spendMp`, `AI_PLAYER` | `obj/player.js` |
| static / projectile AIs `$10 $11 $2C $4C/$4D $4E`, `AiShootCheck` | `27:B0B5 B0A8 ACC0 B595 B5D9 B714` | `AI_HANDLERS` | `obj/ai_static.js` |
| walker / chaser AIs `$20-$2A $2E $41` | `27:A915-B339` | `AI_WALK` | `obj/ai_walk.js` |
| field AIs (`$2B $30 $31 $38 $3C $40 $44 $45 $48 $4F $58 $5C-$5E $78 $7B` …) | bank 27 | `AI_FIELD` | `obj/ai_field.js` |
| boss shot / part / small event AIs (`$14 $16 $17` …, duel, shops' `$0E`/`$0F`) | bank 27 | `AI_SHOT` | `obj/ai_shot.js` |
| effect AIs `$04 $06 $08 $57 $74 $76` … | `27:B2FC`, `27:B7F6`, `27:B83B`, `27:A4DC` … | `AI_EFFECT` | `obj/ai_effect.js` |
| AI `$70` `Ai70PhaseDispatch` (13 phases), `HitFlashPal`, `Ai70FlashIrq` | `27:BA2E`, `26:9B07`, `27:BCD7` | `ai70PhaseDispatch`, `hitFlashPal`, `AI_FINAL` | `obj/ai_final.js` |
| AI `$1A` `ThunderStorm` (sword-4 charge-3 shot) | `30:CD8A` | `thunderStep` | `obj/ai_thunder.js` |
| bank-15 bosses `$60-$6F`: `BossPhaseDispatch` / `BossPhaseTable`, `BossAiE0` … `BossAiEA`, `AiBowOfTruth9E`, `BossDefeated` + hooks, `ClearEnemySlots`, `AiBodyPart` | `15:A400` / `15:A413`, `15:A477-B2DA`, `15:B807`, `15:BA98`, `15:B8FD` | `bossPhaseDispatch`, `bossAi*`, `aiBowOfTruth9E`, `bossDefeated`, `clearEnemySlots`, `AI_BOSS` | `obj/ai_boss.js` |
| `ObjCollideAll`, `ObjOverlapScan`, `CollPairTable`, `HitHandlerTable` + handlers (`HitByPlayerShot`, `HitPlayer`, `TalkProbeHit`, `HitHandler3`), `EnemyKilled`, `AddXp`, `PickupGold` | `26:8F6E`, `26:95F8`, `26:96F1`, `26:8FBB`, `26:90AA`, `26:9357`, `26:94B0`, `26:8FC4`, `26:9152`, `26:924B` | `objCollideAll` (generator), `objOverlapScan`, `HIT_HANDLERS` | `obj/collide.js` |
| `PlayerStatusTick` | `31:EF55` | `playerStatusTick` (generator, called from `substPlayBody`) | `states/play.js` |
| `ObjSpriteDraw`, `ObjDrawMeta`, `ObjHideUnusedOam`, orbit ring | `28:800E`, `28:822E`, `28:80A5`, `28:80D6` | `spriteDraw`, `spriteDrawY`, `drawMeta`, `hideUnusedOam` | `obj/sprites.js` |

### 3.5 HUD, text, save persistence

| ROM routine | Address | JS | Module |
|---|---|---|---|
| `HudRedraw` / `HudElemTable`, HP bar, `StatRecalc` | `26:8E46` / `26:8EC5`, `26:8F2B`, `26:8CC0` | `hudRedraw`, `hudQueue`, `hudHpBar`, `statRecalc`, `hudRedrawCycles` (lag model) | `states/death.js` |
| text printer, dictionary, control codes | `20:853A`, `20:85A2` | `textPrintBox` and helpers (message bytes read from PRG via `cpuRd`) | `states/talk.js` |
| battery WRAM | cartridge | `loadWram` / `WramSaver` (`localStorage`, base64 WRAM) | `src/save.js` |

---

## 4. State machine and frame flow

```
                 Reset ──cold──▶ (bank-18 intro, not run: boot.js introLeftovers) ──▶ BootSaveCheck
                                                                                        │
   ┌──────────── main_state $40 ◀───────────────────────────────────────────────────────┘
   │ 2 MainState2Init ──INC──▶ 3 title / opening / story / status / name entry  (title.js)
   │                               │ new game → $40 = 0           │ continue → $40 = 8
   │                               ▼                              ▼
   │ 0 new-game init ─────────▶ 1 GAMEPLAY  (play.js)  ◀──── 8 LoadGameAndInit   (areaenter.js)
   │                                sub_state $41: 0 init → 1 area enter → 8 SubstPlay ⇄
   │                                {6 item menu, 7 give item, 16 pending message, 17 NPC talk, 18 SELECT menu,
   │                                 19 wake scene, 20 teleport, 21 telepathy, 23 change form, 24 cure, 25 form revert,
   │                                 26 sword raise, 27 Mesia, 28 out of MP, 31 restore screen, 32-36 shops}
   │                                START in SubstPlay → PauseScreen (JSR, not a substate switch)
   │                                3 PlayerDeath ──▶ main 2 (game over) / Opel revive → sub 8
   │                                30 SubstFinalSceneReset ──▶ main 4
   │ 4 ending init ─▶ 5 ending (30 steps; step 29 = RTS until reset)   (ending.js)
   └──────────────────────────────────────────────────────────────────────────────────
```

### 4.1 Multi-frame routines

ROM routines that wait for the NMI (`LDA $09 / BEQ`, `LSR $09 / BCC`, `WaitFrame`, fades, `FullScreenDraw`) are
generators. `mainStep` keeps one pending generator (`m.pending`) and resumes it once per frame while `frame_ready`
is set. Yield values (`states/death.js`):

- bare `yield` — the ROM waits for the next NMI here;
- `yield SPIN` — a busy-wait whose exit happens mid-frame (e.g. `PpuQueueDrainWait`): the pass resumes in the same frame budget;
- `yield NO_NMI` — a lost NMI (rendering switched on after vblank): `Machine.runFrame` skips `nmiWork` once
  and the next `mainStep` resumes with `frame_ready` = 0.

### 4.2 Per-frame order (gameplay)

`ReadPads` → `PlayDeathMenuCheck` (death / START / SELECT) → `PlayerStatusTick` → `ObjUpdateAll` (generator) →
`CameraTick` (queues BG rows / columns) → `ObjSpriteDraw` → `ObjCollideAll` → `SubstPlayTail`; then the HUD IRQ
(handler 1, line 190: scroll / CHR split + `SoundUpdate`), then the NMI. Area-load frames tick the driver twice
(IRQ 1 and NMI mode 0 with rendering off), reproduced by the same `irq_mode` / PPUMASK test.

### 4.3 Lag frames (CPU overruns)

The ROM has no frame budget: a `SubstPlay` pass still running at the next NMI makes that NMI skip OAM DMA and the
scroll copy, and the next frame has no pass. `lagmodel.js` (s215-s220) estimates each pass's ROM cycles from per-stage
`m.rd` / `m.wr` / `cpuRd` counts (`m.passStage` buckets) plus exact cycle counts of the modelled hot code (`m.cyc`:
collision record loop, `ObjOverlapScan`, `substPlayTail`, `Ai70FlashIrq`), fitted per stage and per AI id (`LAG_COEF`).
It covers `irq_mode` 0, 7 (`m.lagMode7`) and 2 (`m.lagMode2`); `m.lagModel = false` turns it off. `startLine()` gives
the next pass's start line from the NMI length. Older per-routine heuristics (`m.overrun`, `FSD_LAG` rows) remain where
a fixed number of lag frames is known (area entry, `AiScreenRedraw08`, `BossHookA6`).

---

## 5. Data export (`tools/gen_web_data.py` → `web/data/`)

`python3 tools/gen_web_data.py` (reads `rom/re-c.nes`) regenerates everything; `manifest.json` lists each file with size,
sha1, ROM source range and REVERSE.md section, plus the ROM sha1. Binary blobs keep ROM byte order.

| File | Content | Read by |
|---|---|---|
| `prg.bin` | all 32 PRG banks; `prg.js` `cpuRd` maps `$8000`/`$A000` to `m.prgR6`/`m.prgR7` | almost all game code (tables, scripts, text, ObjTypeTable, ShotTable) |
| `chr.bin` | 128 KB CHR ROM (128 × 1 KB) | `nes/ppu.js` |
| `screens.bin`, `screens_b10.bin`, `metatiles.bin` | screen pages (banks 0-7, bank 10), metatile / attr / BG-collision / visited-subst pages (banks 8-9) | `world/bg.js`, `world/bgstream.js` |
| `areas.json` | pre-decoded area records + header sub-tables k0-k5 | `world/area.js` |
| `palettes.json` | `nesRgb[64]` + palette slots | `palette.js` (`slots`), `main.js` / tests (`nesRgb` colour output) |
| `sprites_b28.bin` | banks 28-29 as `$8000-$BFFF` | `obj/sprites.js` |
| `sound_b24.bin`, `sound_b25.bin`, `sound_b12.bin`, `dmc.bin` | driver + music banks, SFX bank, DMC sample | `sound/driver.js`, `nes/apu.js` |
| `ram_labels.json` | `labels.csv` bank-`*` rows | `game/ram.js` |
| `objtypes.json`, `metasprites.json`, `shots.json`, `tables.json`, `text.json`, `sound.json` | decoded views (s49 design) | catalog page and tests; the port reads the ROM bytes instead |
| `catalog_manifest.json` | asset catalog (`tools/catalog_export.py`) | `web/catalog.html` |

---

## 6. Verification (as built)

| Level | Tool | Criterion |
|---|---|---|
| Data | `gen_web_data.py --check`, `core_check.mjs`, `serve_check.mjs` | manifest sha1s; port boots and serves |
| Hardware models | `ppu_check.mjs`, `apu_check.mjs`, `sound_check.mjs` | PPU rendering rules; APU vs 2A03 formulas; driver vs `tools/emu/sound_capture.py` |
| Routines | `obj_check.mjs`, `sprite_check.mjs`, `world_check.mjs` | whole RAM + WRAM identical to `tools.emu` captures (`routine_capture.py` real + fuzz cases); area renders vs `gfx/areas/` |
| Frames | `run.mjs --compare` vs `python3 -m tools.emu --compare` → `tools/compare_frames.py`; `tools/web_compare.py` + `COMPARISON.md` | per-frame state, positions, camera, sound periods, palette / OAM CRCs, 32 object slots |
| Scenarios | ~45 `*_check.mjs` (substates, events, talk acts, bosses, enemy groups, lag model) vs `web/test/*_ref.csv` | rows equal at shift 0 except the cells each check lists (LATE / EARLY / PARTIAL, `tools/cell_class.py`) |
| All | `python3 tools/run_checks.py` | every check passes (52/52 in s220, see REVERSE.md Verification Log) |

Debug / probe tools in `web/test/`: `ram_watch.mjs`, `oam_dump.mjs`, `state_meta.mjs`, `pending_probe.mjs`,
`lag_probe.mjs`, `pass_count.mjs`, `lag_coef_dump.mjs`, `case_trace.mjs`, `teleport_probe.mjs` (CLAUDE.md lists their arguments).
