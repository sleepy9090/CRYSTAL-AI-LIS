# re-c (NES) — Web Port Architecture

Written in session 49 (Ph7 web 1/8) as the **design** of the port, before any port code exists. Companion of
`docs/architecture_exe.md` (the ROM side); REVERSE.md stays the source of truth for every routine named here.
Status column values: `planned` (no code yet) — later Ph7 sessions update this file as modules land
(task "Final docs refresh").

---

## 1. Design rules

1. **Same RAM, same addresses.** Game state lives in `ram = new Uint8Array(0x800)` and `wram = new Uint8Array(0x2000)`
   (`$6000-$7FFF`) at the **same addresses as the ROM** (`docs/ram_map.txt`, `docs/obj_struct.txt`). JS code names them
   through constants (`OBJ_X_LO = 0x70`, `AREA_ID = 0x6C`, …) generated from `labels.csv` bank `*` rows.
   Why: the `--compare` CSV (`tools/emu/compare.py`) reads raw RAM, the save file is the WRAM image
   (`docs/save_layout.txt`), and routines that index columns with stride `$20` port line by line.
   The CPU stack, zero-page temps that never live across a routine, and the bank shadows `$6E/$6F/$50` are **not** mirrored.
2. **No hand-copied ROM data.** Every table comes from `web/data/` written by `tools/gen_web_data.py`
   (§5). The JS only holds *code constants* (an address the routine computes, a loop bound, an opcode meaning).
   Tables keep the ROM's CPU addressing where the routine indexes them by address (screen pages, metatile window,
   sound streams), so a JS routine reads `win89[addr - 0x8000]` exactly where the 6502 does `LDA ($10),Y`.
3. **Bank switching disappears.** Each `SwitchBank16(n)` / `SetR6` / `SetR7` call becomes a direct reference to
   the exported blob or decoded table it was mapping in. Only the **CHR** registers R0-R5 stay (they are visible state
   in the PPU model). The bank-10 screen-page quirk (`$62FF` b7, row path only) is kept as an explicit branch.
4. **Logic is DOM-free.** Everything under `web/src/game/`, `web/src/nes/ppu.js`, `web/src/nes/apu_core.js` and
   `web/src/data.js` runs under Node 18+ (`web/test/run.mjs`). Only `main.js`, `input.js` and `apu_webaudio.js`
   touch the browser.
5. **Frame-exact scheduling.** The port does not emulate the 6502, but it keeps the ROM's frame structure:
   main-loop work → (scanline 190 IRQ work) → NMI work, including the two known timing quirks:
   two `SndTick`s on an area-load frame, and the "main loop overran → skip OAM DMA/scroll copy" path (§4).
6. **ES modules, no bundler**, served by `python3 -m http.server` from the repo root (`/web/index.html`).

---

## 2. Module graph

```
web/index.html
└─ src/main.js                (browser: canvas, rAF + fixed 60.0988 Hz accumulator, pause/visibility)
   ├─ src/input.js            (keyboard + Gamepad API → NES button byte A B Sel St U D L R)
   ├─ src/nes/apu_webaudio.js (AudioWorklet / ScriptProcessor sink for apu_core samples)
   └─ src/game/machine.js     (Machine: ram, wram, ppu, apu, data; runFrame(buttons))
      ├─ src/data.js                  loadData(fetchFn) → typed arrays + JSON from web/data/manifest.json
      ├─ src/game/boot.js             (s106) Reset's cold path: intro end state, ColdSaveInit, BootSaveCheck, CopySramBlock
      ├─ src/nes/ppu.js               256×240 indexed framebuffer: 2 nametables (H mirroring), attrs, palette RAM,
      │                               OAM, CHR R0-R5 (mode 0), per-scanline scroll/CHR/mask split list, 8 spr/line
      ├─ src/nes/apu_core.js          2 pulse, triangle, noise, DMC; register writes $4000-$4017; sample generator
      ├─ src/game/ram.js              RAM/WRAM arrays + named address constants (generated, §5) + obj column helpers
      ├─ src/game/mainloop.js         MainLoop, MainStateTable, NMI work, IRQ split list, frame_ready/$09 handshake
      ├─ src/game/states/             title.js (s82: states 2 + 3, bank 18/19 screen library); play.js + death.js (s88/89); pause.js (s94: pause / status screen); equip.js (s98: SELECT equipment / item menu, bank 16); saveload.js (s102: save/load page, bank 23 save module, menu exit fade, load tail + HUD window rebuild); ending.js (s90: states 4 + 5, bank 17); areaenter.js (s108: states 0 + 8, substates 0 + 1, AreaEnter)
      │   └─ substates.js             SubstateHandlerTable (40) → handler functions; substate banks are irrelevant
      ├─ src/game/world/
      │   ├─ camera.js                (s108) CameraTick / CameraUpdate / CameraFollow / CameraClampX / shake, AreaSubTable, ExitLookup
      │   ├─ area.js                  AreaLoad, LoadAreaChrRecord, spawn handlers 0-4, AreaLoadMap, k1-k5, AreaEntryTable
      │   ├─ bg.js                    ScreenPagePtr, ScreenTileSubst, MetatileExpand, MetatileAttr (static area render)
      │   ├─ bgstream.js              BgRowRender, BgColRender, FullScreenDraw 30:C831 (generators; s79)
      │   ├─ ppuqueue.js              PPU queue ring $6200 (records as in the ROM) + PpuQueueFlush into ppu.js (s79)
      │   ├─ camera.js                CameraUpdate, CameraFollow, CameraClampX, scroll copy $07D8-$07DB
      │   └─ exits.js                 ExitLookup (k3), AreaPitFall, AreaEdgeLink (k5), visited bitmap $62F0
      ├─ src/game/obj/
      │   ├─ objinit.js               (s55, done) ObjInit 30:C25D (ObjTypeTable records via cpuRd) + FarCall_C25D
      │   ├─ util.js                  (s55, done) Rand64, ObjFindFree, ObjPosToTemp/TempToObjPos, TempAddOffset/Step,
      │   │                           DirToTarget, ObjStepVector, ObjDespawn, ObjBecomeNext, SpawnShot, ObjBgProbe
      │   ├─ ai_static.js             (s55, done) AIs $10 $11 $2C $4C/$4D $4E + AiShootCheck; AI_HANDLERS id → fn
      │   ├─ ai_walk.js               (s81, done) walker/chaser AIs $20-$2A $2E $41 (AiChase, AiWalkPause, …); AI_WALK
      │   ├─ ai_effect.js             (s93, done) AIs $04 (slot $0C on the player; leaves X = $0C), $57, $76 MP drain
      │   ├─ ai_final.js              (s97, done) AI $70 final scene: 11 phases (window wipe, gather, fades, shots, follower) + HitFlashPal
      │   │                           (generator: yield* hudRedraw); AI_EFFECT
      │   ├─ ai_boss.js               (s100, partial) bank 15 bosses: BossPhaseDispatch (handlers keyed by address),
      │   │                           ClearEnemySlots, AIs $60 (E0 teleporter) $61 (homing shot) $63 (E3) $64 (rock);
      │   │                           s104: $66 (E6 flyer) $67 (E7 bouncer) $68 (E8 waypoint flyer); AI_BOSS
      │   ├─ objects.js               (s80, done) ObjUpdateAll generator (knockback, obj_freeze + HitSetFlag, JSR $0010
      │   │                           dispatch; an AI returning true = "popped" → return, a number = new X (s93),
      │   │                           a generator is run with yield* (s93)) + AI_TABLE id → fn (unported
      │   │                           ids log once and do nothing); AI $1A 30:CD8A still unported
      │   ├─ ai/*.js                  generic enemies, NPC/path, platforms, bosses (BossPhaseTable)
      │   ├─ player.js                PlayerMove (slot 0), PlayerUpdate (slot 1), sword/charge, PlayerMagic, SpendMp
      │   ├─ move.js                  (s80, done) ObjMoveSlide 26:98D7, ObjMoveStep (BgProbePoints), ObjScreenEdge
      │   ├─ collide.js               ObjCollideAll, HitboxTable, CollPairTable, HitPlayer, EnemyKilled, AddXp
      │   ├─ status.js                PlayerStatusTick (hazards, poison, regen, pit fall, invulnerability)
      │   └─ sprites.js               ObjSpriteDraw (4 draw orders), ObjDrawMeta, orbit ring/burst, OAM shadow $0200
      ├─ src/game/ui/
      │   ├─ hud.js                   HudRedraw (HudElemTable), level tables
      │   ├─ text.js                  TextPrint, dictionary, control codes, message window
      │   ├─ npc.js                   NpcTalk, NpcScript, TalkActionTable, EventTriggerTable, Telepathy
      │   ├─ items.js                 ItemGetTable/ItemUseTable + handlers, GiveItem, ItemUseMenu
      │   ├─ menus.js                 bank 16/17/19 menus: status, equipment, save/load page, teleport, change form
      │   └─ save.js                  SaveGame/LoadGame on wram $7100-$7FFF; persistence = localStorage (base64 WRAM)
      ├─ src/game/playsound.js        (s55, done) PlaySound 30:C125 FIFO $0103 (5 deep) — RAM side only
      ├─ src/game/prg.js              (s55) prgByte / cpuRd (current m.prgR6/prgR7 mapping) / setR6 / setR7 / farRestore
      ├─ src/game/sound/
      │   ├─ (soundupdate.js)         SoundUpdate (≤1 command per call)
      │   └─ driver.js                SndCommand, SndStartMusic/Sfx, SndTick, 16 stream ops, envelopes, vibrato quirk
      ├─ src/game/palette.js          PaletteBuild, fades, PaletteUpload ($6140-$615F → ppu palette RAM)
      ├─ src/game/pads.js             ReadPads / ReadPadsMenu ($43-$4F hold/repeat logic) from the button byte
      └─ (Rand64 26:97D7 lives in obj/util.js, s55)
web/test/run.mjs                      Node harness (Ph7 web 3/8): --keys, --frames, --frame0, --dump-screen,
                                      --dump-ram/--dump-wram, --compare
web/test/harness.mjs                  key-script parser, compare.py CSV row, PNG encoder (node:zlib)
web/test/world_check.mjs              (s53) world/area.js + bg.js + paletteBuild vs gfx/areas/ (render_level.py)
                                      and 2 tools.emu frames (web/test/world_frames/ RAM/WRAM dumps)
web/test/sprite_check.mjs             (s54) obj/sprites.js spriteDraw() vs RAM captured at 28:800E entry/exit
                                      (web/test/sprite_frames/, tools/emu/sprite_capture.py real + fuzz)
web/test/obj_check.mjs                (s55) any object routine vs tools/emu/routine_capture.py fuzz cases: whole RAM +
                                      WRAM + return kind (web/test/obj_frames/, 1000/1000 identical)
web/catalog.html, catalog.js, apu_preview.js   existing asset catalog (session 27), kept separate from the port
                                               (real-browser check: python3 tools/browser_check.py, s95)
```

Dependency rule: `game/*` modules import `ram.js`, `data.js` and siblings, never `main.js`/DOM. `nes/ppu.js` and
`nes/apu_core.js` know nothing about the game; the game talks to them only through the same registers/state
the ROM writes (CHR regs, scroll, PPU queue writes, OAM shadow, `$4000-$4017`).

### 2.1 Machine.runFrame(buttons)

```
runFrame(buttons):                          // as implemented in game/machine.js (s51)
  m.buttons = [buttons, 0]                  // what ReadPadRaw would assemble (ROM order A b7 … Right b0)
  mainStep(m)                               // only if $09 != 0 (the NMI set it): MainStateTable[$40], then $09 = 0
  // a state may be a generator spanning several frames (ROM wait loops on $09); see §4
  _installSplits()                          // split list from the last NMI-mode routine → ppu.splits (latch L → line L+1)
  ppu.renderFrame()                         // frame shown between the two NMIs; IRQ handler bodies run as split hooks
  nmiWork(m)                                // early-outs $60/$06; OAM DMA + $07D8-DB copy only if $09 == 0
                                            // (else main loop overran); PpuQueueFlush, PaletteUpload,
                                            // PPUMASK ← $01, NmiModeDispatch[$51] (schedules splits), INC $09
  apu.endFrame()                            // (7/8) flush audio samples for 1/60.0988 s
```

Correction (s51): MainLoop `30:C900` runs the state when `frame_ready` **≠ 0** and clears it afterwards; the NMI
skips the OAM DMA and scroll copy while it is still ≠ 0. The first version of this section had the test inverted.

---

## 3. EXE → web mapping

Every ROM routine the port reproduces, with its JS home. File offsets are in `docs/architecture_exe.md`.

### 3.1 Frame skeleton, hardware, data

| ROM routine | Address | JS function | Module |
|---|---|---|---|
| `Reset` | `31:F2A4` | `Machine.reset()` (always the cold path: RAM is zeroed) → s106 `introLeftovers` (the RAM/PPU state the skipped bank-18 intro leaves at the first `30:C900`), `coldSaveInit` `31:F1CD`, `bootSaveCheck` `31:F0A4` (slot checks / resets / repair copies, `$16` seed kept live), `copySramBlock` `31:F3A2` | `game/machine.js`, `game/boot.js` |
| `NmiMode5` / `IrqHandler6` | `31:F7C5` / `31:F691` | `nmiMode5` / `irqIntroSplit` (s106; only the f214 NMI uses irq_mode 5 in the port) | `game/mainloop.js` |
| `MainLoop` / `MainStateTable` | `30:C900` / `30:C91F` | `mainStep()` / `MAIN_STATES[9]` (6/7 null; s108: all states ported) | `game/mainloop.js` |
| `NMI` | `31:F3B6` | `mainloop.nmiWork()` | `game/mainloop.js` |
| `NmiModeDispatch` / `JmpTable31_F6C2` | `31:F6AD` / `31:F6C2` | `NMI_MODES[irq_mode]` | `game/mainloop.js` |
| `IRQ`, `SetIrqHandler`, `IrqHandlerTable` | `31:F443`, `31:F424`, `31:F433` | `setIrqHandler(i)` → split list entries for `ppu.js` | `game/mainloop.js` |
| `PpuQueueFlush`, `PpuQueueWaitSrc`, `PpuQueueAttrAll` | `30:C67D`, `30:C72B`, `30:C739` | `ppuQueueFlush(m)` (= `Machine.ppuQueueFlush`), `queueHasSrc`, `queueAttrAll` | `game/world/ppuqueue.js` |
| `BgRowRender`/`ScreenRowRender`, `BgColRender`, `FullScreenDraw` | `31:EB91`/`EBC1`, `31:EC87`, `30:C831` | `bgRowRender(m)`, `bgColRender(m)`, `fullScreenDraw(m, a)` (generators) | `game/world/bgstream.js` |
| `PaletteUpload` | `31:F8CB` | `palette.upload(ppu)` | `game/palette.js` |
| `PaletteBuild`, fades | `26:8C0E`, `26:8C3D/8C49` | `palette.build()`, `palette.fadeOut/In()` | `game/palette.js` |
| `ApplyChrPlay` / `ApplyChrAlt` | `31:F883` / – | `ppu.setChr(R0..R5)` from `$07F0-$07F5` / `$58-$5D` | `game/mainloop.js` |
| `ReadPads` / `ReadPadsMenu`, `PadDirTable` | `31:FE80`, `31:FED0` | `pads.read()` / `pads.readMenu()` | `game/pads.js` |
| `Rand64` | `26:97D7` | `rand64()` | `game/rand.js` |
| far-call trampolines | `31:FF80-FFE2` | plain calls (no banks) | – |

### 3.2 States and substates

| ROM routine | Address | JS function | Module |
|---|---|---|---|
| state 0 new game / state 8 continue, `GameStartObjs` | `30:C957` / `30:C9DA`, `30:C9FF` | `mainStateNewGame()` / `mainStateContinue()` (generators; s108; `LoadGameAndInit` runs through one NMI) | `states/areaenter.js` |
| state 1 gameplay dispatcher | `30:CAB6` | `mainState1()` → `SUBSTATES[$41]` (ported: 0, 1, 3, 12, 18) | `states/play.js` |
| `SubstNewGameInit` (+ `NewGameSkipCheck` `30:CAA2`), `SubstAreaEnter`, `AreaEnter` (`AreaEntryTable` `31:E3D9`: `E422`/`E41C`/`E51C`/`E533`; `E544` pit fall not ported), `AreaEnterCommon` `31:E439`, `PlayerPlaceBody` `26:9CEF` | `30:CA5F`, `30:CA2E`, `31:E3E9` | `substNewGameInit()`, `substAreaEnter()`, `areaEnter()` (generators; s108). Lag frames from a tools.emu trace: `fullScreenDraw(m, 1, FSD_LAG)` (rendering off: NMIs after rows 7/13/19/25/30), full `SaveGame` = 2 NMIs | `states/areaenter.js` |
| `CameraTick`, `CameraUpdate`, `CameraFollow`, `CameraClampX`, `CameraShake` `31:EADF`, `ExitLookup`, `AreaSubTable` | `31:E8F6`, `31:E90A`, `31:E9AD`, `31:EA72`, `31:EB09`, `30:C7A5` | `cameraTick()` (generator; returns true when `ExitLookup` took an exit = the ROM's PLA×4, so the caller must return), `cameraUpdate()`, `cameraClampX()`, `exitLookup()`, `areaSubTable()` (s108) | `world/camera.js` |
| IRQ handlers 0 (wrap) / 1 (HUD + gameplay `SoundUpdate`) | `31:F455` / `31:F488` | `irqWrapSplit(m, line)` / `irqHud(m)` (s108; split hooks get their line, handler 0 chains handler 1 with `chainSplit`). The NMI now starts with `LDA PPUSTATUS` (`31:F3B7`): `irqHud` leaves the `w` latch at 1 | `mainloop.js` |
| state 2 / `MainState2Init` | `30:C931`, `18:9FCC` → `19:A000` | `mainState2()` (generator; s82). The cold-boot intro `18:8028` is not run (port starts at f214) | `states/title.js` |
| state 3 / `MainState3`, `State3ModeTable`, `State3SubTable` | `30:C939`, `19:A015`, `19:A038`, `19:A052` | `mainState3()`; modes in the `MODES` map, step tables in `STEPS` (ROM address → function). s82: fades `18:8310/834A`, opening `19:A79F` (25 steps), title menu `19:A653/A6DF`; s107: story `19:ABB4/ABEA` (28 steps), status screen + name entry `19:A0B5/A176` (7 steps) → `main_state` 0 | `states/title.js` |
| bank-18 screen library | `18:80E7` `SceneMetaDraw`, `8198` `WaitFrameReady`, `81A1` `NtFill`, `81D8` `OamHideAll18`, `81F0` `NtRectBlit`, `82B3-82FD` menu palettes | `sceneMetaDraw`, `waitFrameReady` (generator), `ntFill`, `oamHideAll`, `ntRectBlit` (direct `ppu.writeReg` 6/7), `palBlack/palLoad/palApply/palFadeIn`. A lag frame (the NMI firing at a `PpuNmiOn18` after line 241) is a bare `yield` at that point. s107: `18:80DF` `sceneMetaDrawClip`; `19:A092` `PpuQueueDrainWait` = `yield SPIN` loop; a **lost NMI** (the `PpuNmiOn18` comes after vblank ended, e.g. `TitleStatusInit` in f1702) is `yield NO_NMI`: `mainStep` sets `m.skipNmi` (so `Machine.runFrame` skips `nmiWork` once) and `m.noNmiResume` (so the next `mainStep` resumes with `frame_ready` = 0) | `states/title.js`, `mainloop.js`, `machine.js` |
| states 4/5 (ending) | `17:A000` `EndingInit` / `17:A100` `EndingTick`, `EndingPhaseTable` `17:A166` (30 steps), `EndingSubTable` `17:A140`, blitters `A7A0`/`A81C`/`AB1D`, sprites `A94B`/`A9D7`, fades `AA17-AAEC` | `mainState4()` / `mainState5()` (generators; s90). Steps in `STEPS`, sub-effects in `SUBS` (ROM address → function); `EndingWaitFrame` = `death.js` `waitFrame` | `states/ending.js` |
| pause / status screen | `15:BC40` `PauseScreen` + `15:BDDB` `PauseWait`, `30:C482` `PpuQueueBlock`, `30:C1AE` `ObjSlot1FDraw`, NMI mode 2 `31:F751` + `ApplyChrAlt` `31:F64F` | `pauseScreen()` / `pauseFromPlay()` / `ppuQueueBlock()` (generators; s94); `nmiMode2`/`applyChrAlt` in `mainloop.js`. Reached in tests through `play.js` `ENTRY_POINTS['15:BC40']` (SubstPlay calls it by `JSR`) | `states/pause.js` |
| save / load page (START in the SELECT menu) | `16:9AEE` `SaveLoadPage` + `9B3A` loop, `9BE1` `MenuSave`, `9CE0` `SaveSlotCopy`; bank 23 `BC09` `SaveGame` / `BC8E` `LoadGame` / `BCDA` `LoadGameAndInit`; `30:C80C` `MenuExitFade`; `30:D930` load tail + `19:BA65` `HudWindowSetup` + `26:8D59` `HudChargeMeter` | `states/saveload.js` `saveLoadPage()` / `menuExitFade()` / `loadTail()` / `hudWindowSetup()` (generators), `saveGame()` / `loadGame()` / `loadGameAndInit()` / `saveSlotCopy()`; called from `equip.js` (s102) |
| equipment / item menu (SELECT) | `30:D8C7` `SubstEquipMenu` (substate `$12`) + bank 16 `16:8000` `EquipMenu` (window `881E`, grids `871F`, A `82B7`, B `8342` + `MenuConfirmYesNo` `84DA`, sort `8534`, `MenuPrint` `8933`, `MenuSprDraw` `86D3`), NMI mode 6 `31:F7DB` + IRQ handler 7 `31:F61A` | `substEquipMenu()` / `equipMenu()` / `menuPrint()` (generators; s98); `nmiMode6`/`irqMenuChrSplit` in `mainloop.js` (`machine.js` `irqHandlers[7]`). `SUBSTATES[18]` + `ENTRY_POINTS['30:D8C7']`. Unported exits (`SaveLoadPage` `16:9AEE`, `30:C80C`, load tail `30:D930`, shop variants) are logged and stop the generator | `states/equip.js` |
| `SubstateHandlerTable` | `30:CADE` | `SUBSTATES[40]` | `states/substates.js` |
| `SubstPlay` | `30:CB62` | `substPlay()` | `states/state1_gameplay.js` |
| `SubstPlayTail` (respawn tick) | `31:E0B8` | `substPlayTail()` | `obj/objects.js` |
| `PlayerDeath` | `19:B900` | `playerDeath()` | `states/substates.js` |
| `ItemUseMenu`, `GiveItem` | `30:D497`, `30:D3EB` | `itemUseMenu()`, `giveItem()` | `ui/items.js` |
| `ShowPendingMessage`, `NpcTalk` | `30:D085`, `30:D0C8` | `showPendingMessage()`, `npcTalk()` | `ui/npc.js` |
| `SubstWakeCutscene` / `WakeCutscene` | `30:D9B6` / `20:9400` | `wakeCutscene()` | `states/substates.js` |
| `SubstTeleportMenu`, `SubstTelepathySelect/Reply`, `SubstChangeForm` | `30:DB28`, `30:DD6B/DE04`, `30:DEC1` | same names | `ui/menus.js`, `ui/npc.js` |
| `SwordRaisePose` (substate 26) etc. | `19:BB39-BC6B` | `substCutscene24..27()` | `states/substates.js` |
| bank-15 scenes (substates 12/13) | `15:BC40/BDDB` | `substScene12/13()` | `states/substates.js` |

### 3.3 World

| ROM routine | Address | JS function | Module |
|---|---|---|---|
| `AreaLoad`, `LoadAreaChrRecord` | `31:E14A`, `31:E16D` | `loadArea()`, `loadAreaChr()` | `world/area.js` |
| `SpawnHandlerTable` handlers 0-4 | `31:E218` | `SPAWN_HANDLERS[5]` (handler 1 uses `areas.json.indirect`) | `world/area.js` |
| `AreaSubTable` (k0-k5) | `30:C7A5` | `data.areas[id].k0..k5` (pre-decoded) | `world/area.js` |
| `AreaLoadMap` / k1 / k2 / k4 | `31:E628` / `31:E6B9` / `31:E686` / `30:C765`, `31:E702` | `loadAreaMap()`, `loadAreaGfx()`, `placePlayer()`, `restoreVisited()` (s53, done; `30:C765` visited marking still planned) | `world/area.js` |
| `PaletteBuild` / `PaletteLoadOne` | `26:8C0E` / `26:8C6E` | `paletteBuild(m, fade)` (s53, done) | `game/palette.js` |
| `AreaEntryTable` | `31:E3D9` | `AREA_ENTRY[($6D&$E0)>>5]` | `world/area.js` |
| `ScreenPagePtr`, `ScreenTileSubst`, `MetatileExpand`, `MetatileAttr` | `31:EF36`, `31:ED99`, `31:EDBA`, `31:EDE5` | `screenPage()`, `tileSubst()`, `metatileTiles()`, `metatileAttr()` | `world/bg.js` |
| `ScreenRowRender` / `BgRowRender` / `BgColRender` | `31:EBC1` / `31:EB91` / `31:EC87` | `renderRow()`, `renderCol()` → PPU queue | `world/bg.js` |
| `CameraUpdate`, `CameraFollow`, `CameraClampX` | `31:E90A`, `31:E9AD`, – | `cameraUpdate()`, `cameraFollow()`, `cameraClampX()` | `world/camera.js` |
| `ExitLookup`, `AreaPitFall`, `AreaEdgeLink` | `31:EB09`, `31:E544`, `31:E573` | `exitLookup()`, `pitFall()`, `edgeLink()` | `world/exits.js` |
| `ObjBgProbe` | `26:9A30` | `bgProbe()` (attr page `$69` from `bgcoll`) | `obj/move.js` |

### 3.4 Objects, player, combat

| ROM routine | Address | JS function | Module |
|---|---|---|---|
| `ObjInit` | `30:C25D` | `objInit(slot, type)` (applies `objtypes.json` column inits) | `obj/objects.js` |
| `ObjUpdateAll`, `ObjAiTable`, `HitSetFlag` | `30:CCCC`, `27:A314`, `26:9011` | `objUpdateAll()`, `AI_TABLE`, `hitSetFlag()` (s80, done: fuzz 174/174 + gaps) | `obj/objects.js` |
| `ObjMoveSlide`, `ObjMoveStep`, `ObjScreenEdge` | `26:98D7`, `26:9947`, `26:99FF` | `objMoveSlide()`, `objMoveStep()`, `objScreenEdge()` (s80, done: fuzz 200/200) | `obj/move.js` |
| `BossPhaseDispatch`, `BossPhaseTable` | `15:A400`, `15:A413` | `bossPhase(slot)` | `obj/ai/boss.js` |
| generic enemy AIs + helpers (`DirToTarget`, `AiShootCheck`, `SteerToward`) | bank 27 | `ai/enemies.js` | `obj/ai/` |
| `PlayerMove` / `PlayerUpdate` / `PlayerMagic` / `SpendMp` | `26:9D9A` / `26:9B45` / `27:A022` / – | `playerMove()`, `playerUpdate()`, `playerMagic()`, `spendMp()` | `obj/player.js` |
| `ObjStepVector`, `ObjMoveStep`, `BgProbePoints` | `26:8480`, `26:9947`, `26:9907` | `stepVector()`, `moveStep()`, `BG_PROBE` (data) | `obj/move.js` |
| `SpawnShot`, `ShotTable`, `ShotOffsets` | `26:972D`, `20:BC00`, `20:BD80` | `spawnShot(m, x, id, dir)` (s55, done; reads `prg.bin` via `cpuRd`, not `shots.json`) | `obj/util.js` |
| `ObjInit` + `FarCall_C25D`, `ObjStepVector`, `ObjBgProbe`, `DirToTarget`, `Rand64`, `ObjFindFree` | `30:C25D`, `31:FF80`, `26:8480`, `26:9A30`, `26:8409`, `26:97D7`, `26:98B9` | `objInit()`, `farObjInit()`, `objStepVector()`, `objBgProbe()`, `dirToTarget()`, `rand64()`, `objFindFree()` (s55, done) | `obj/objinit.js`, `obj/util.js` |
| AIs `$10 $11 $2C $4C/$4D $4E`, `AiShootCheck` | `27:B0B5 B0A8 ACC0 B595 B5D9 B714` | `AI_HANDLERS` (s55, done: `obj_check.mjs`) | `obj/ai_static.js` |
| AIs `$20 $21 $22 $24 $25 $26 $27 $28 $29 $2A $2E $41`, `AiWalkPick`, `AiTryMove`, `AiLungeCheck` | `27:A915 A969 AB42 AA57 A9D2 A9E4 A9DB AB61 AB9E ABF8 A9B4 B339`, `A94A`, `AAFA`, `AC40` | `AI_WALK` (s81, done: `fuzz_walk.json` 1200/1200) | `obj/ai_walk.js` |
| AIs `$04 $57 $76` (`AiPlayerFollowC`, `AiStraightMoverTail`, `AiMpDrain`) | `27:B2FC B0BE A4DC` | `AI_EFFECT` (s93, done: `fuzz_effect.json` 300/300) | `obj/ai_effect.js` |
| AI `$70` `Ai70PhaseDispatch` (13 phases `JmpTable27_BA40`) + `HitFlashPal` | `27:BA2E`, `26:9B07` | `AI_FINAL` (s97, done: `fuzz_final.json` 200/200; `ShowMessage` path logged as unported) | `obj/ai_final.js` |
| Bank 15 bosses `BossAiE0`/`BossAiE3`/`BossAiE6`/`AiBoss88`/`BossAiE8` + `BossHomingShot`/`BossRockShot`, `BossPhaseDispatch`, `ClearEnemySlots` | `15:A477`, `15:A95D`, `15:AC7B`, `15:ADD4`, `15:AF48`, `15:A5EE`, `15:AB3E`, `15:A400`, `15:BB29` | `AI_BOSS` (s100/s104, partial: `fuzz_boss.json` 2100/2100; bosses `$62 $65 $69-$6F` not yet) | `obj/ai_boss.js` |
| `PlaySound` (queue side) | `30:C125` | `playSound(m, id, y)` (s55, done) | `playsound.js` |
| `ObjCollideAll`, `CollPairTable`, `HitboxTable`, `HitHandlerTable` | `26:8F6E`, `26:96F1`, `26:9691`, `26:8FBB` | `collideAll()`, `HIT_HANDLERS[4]` | `obj/collide.js` |
| `HitPlayer`, `EnemyKilled`, `AddXp` | `26:9357`, `26:9152`, `26:924B` | `hitPlayer()`, `enemyKilled()`, `addXp()` | `obj/collide.js` |
| `PlayerStatusTick` | `31:EF55` | `playerStatusTick()` | `obj/status.js` |
| `ObjSpriteDraw`, `ObjDrawMeta`, `ObjDrawOrderTable`, orbit ring | `28:800E`, `28:822E`, `28:83DC`, `28:80D6` | `spriteDraw()`, `drawMeta()` (s54, done: 552/552 RAM-identical) | `obj/sprites.js` |

### 3.5 UI, text, sound, save

| ROM routine | Address | JS function | Module |
|---|---|---|---|
| `HudRedraw`, `HudElemTable` | `26:8E46`, `26:8EC5` | `hudRedraw(a)` | `ui/hud.js` |
| `TextPrint`, `TextCtrlTable` | `20:853A`, `20:85A2` | `textPrint()` (`text.json` messages + dictionary) | `ui/text.js` |
| `NpcScript`, `TalkActionTable`, `EventTriggerTable`, `EventActionTable` | `14:8000`, `30:D123`, `15:A17A`, `30:D563` | `npcScript()`, `TALK_ACTIONS`, `eventTrigger()` | `ui/npc.js` |
| `Telepathy` | `14:816F` | `telepathy()` | `ui/npc.js` |
| `ItemGetTable`, `ItemUseTable`, `ItemUseHandlers` | `14:9B00`, `14:9BE2`, `14:8399` | `itemGet()`, `itemUse()` | `ui/items.js` |
| menus `16:8000`, `SaveLoadPage` | `16:8000`, `16:9AEE` | `menus.*` | `ui/menus.js` |
| `SaveGame` / `LoadGame` / `LoadGameAndInit` | `23:BC09` / `23:BC03` / `23:BC06` | `saveGame()`, `loadGame()` + `localStorage['re-c.wram']` | `ui/save.js` |
| `PlaySound`, `SoundUpdate` | `30:C125`, `31:F7FE` | `playSound(id)`, `soundUpdate()` | `sound/playsound.js` |
| `SndInit`, `SndCommand`, `SndTick`, `SndReadOp`, `SndOpcodeTable` | `24:8020`, `24:8048`, `24:8186`, `24:8347`, `24:8000` | `sndInit()`, `sndCommand()`, `sndTick()`, `OPS[16]` | `sound/driver.js` |

---

## 4. State machine and frame flow

```
                 Reset ──cold──▶ intro (state 2 path via 18:8028, IRQ 6) ──▶ BootSaveCheck
                   │warm "USO"                                                   │
                   ▼                                                             ▼
   ┌──────────── main_state $40 ◀───────────────────────────────────────────────┘
   │ 2 MainState2Init ──INC──▶ 3 title/menus (State3ModeTable[$06F3] → State3SubTable[$06F0])
   │                               │ "new game"/load: name entry → $40=0
   │                               ▼
   │ 0 new-game init ─────────▶ 1 GAMEPLAY  sub_state $41:
   │                                0 init → 1 area load → 8 SubstPlay ⇄ {6 item menu, 7 give item,
   │                                16 message, 17 NPC talk, 19 wake scene, 20/29 teleport, 21/22 telepathy,
   │                                23 change form, 24-27/30 cut-scenes, 12/13 bank-15 scenes, 32-36 …}
   │                                3 PlayerDeath ──▶ main 3 (back to the intro/title)
   │ 4 ending init (17:A000) ─▶ 5 ending (17:A100, 30 steps; step 29 = RTS until reset) — states/ending.js (s90)
   └───────────────────────────────────────────────────────────────────────────
```

**Multi-frame routines.** Many ROM routines wait inside a loop (`LDA $09 / BEQ` or `JSR` to a "wait N frames"
helper, e.g. the message wait far-call `FFCF`→`$D354`, fades). The port turns each such routine into a
**generator function** (`function*`) that `yield`s where the ROM waits for the next NMI; `mainloop.step()` resumes the
current generator once per frame. This keeps the exact number of frames a fade/message/menu takes, which the
`--compare` CSV checks. Routines without waits stay plain functions.

**Per-frame order (gameplay, from `architecture_exe.md` §3):** `ReadPads` → menu/death checks → `PlayerStatusTick` →
`ObjUpdateAll` → `CameraUpdate` (queues BG writes) → `ObjSpriteDraw` → `ObjCollideAll` → `SubstPlayTail`;
then IRQ handler 1 at line 190 (HUD split: scroll 0, nametable `$2B00`, CHR R0=`$3C` R1=`$38`, `SoundUpdate`);
then NMI (OAM DMA + scroll copy unless overran, queue flush, palette upload, NMI-mode routine, `INC $09`).
**Overrun:** the port never overruns on its own; a "frame budget" is not modelled. The ROM's overrun frames are
rare and only visible in the CSV as a one-frame camera lag; if `--compare` shows them, model them per substate.

**Area-load frame quirk:** `SoundUpdate` runs from both IRQ handler 1 and NMI mode 0 → two driver ticks
(REVERSE.md Verification Log, s42). `nmiWork()` reproduces it by following the same `irq_mode`/rendering-off test.

---

## 5. Data export (`tools/gen_web_data.py` → `web/data/`)

One command regenerates everything: `python3 tools/gen_web_data.py` (reads `rom/re-c.nes`). `web/data/manifest.json`
lists every file with size, sha1, ROM source range(s) and the REVERSE.md section, plus the ROM sha1, so the
port can refuse stale data. Binary blobs keep ROM byte order; JSON numbers are plain integers.

| File | Content | ROM source | Read by |
|---|---|---|---|
| `chr.bin` | 128 KB CHR ROM, 128 × 1 KB banks, NES 2bpp planar tiles (16 B/tile) | CHR `0x40010` | `ppu.js` (R0-R5 index 1 KB banks) |
| `screens.bin` | 64 KB, screen page S at `S*256` (= PRG banks 0-7 in order) | banks 0-7 | `bg.js` `screenPage()`, `move.js` `bgProbe()` |
| `screens_b10.bin` | 8 KB bank 10 (`$62FF` b7 pages S&`$1F` at `(S&$1F)*256`) | bank 10 | `bg.js` (row path only) |
| `metatiles.bin` | 16 KB banks 8/9 as the `$8000-$BFFF` window: quadrant pages, attr tables `$B000`, BG collision pages `$69<<8`, visited subst `$BE00` | banks 8-9 | `bg.js`, `move.js` |
| `palettes.json` | `nesRgb[64]` (2C02 approximation), `slots[256][4]` (BG id p → slot p, sprite id p → slot (p+$B0)&$FF) | banks 2/3 page tails | `palette.js` |
| `areas.json` | per area id: record (`e6 e7 r4 r5`, spawn entries per slot), header k0 (snd, W, H, `62FE`, `62FF`, grid), k1, k2 entries, k3 exits, k4 screen flags, k5 links (raw); `indirect` handler-1 records `p → 4 B` | banks 4/5, 10/11, 12/13 | `area.js`, `exits.js` |
| `objtypes.json` | per type 0-255: `snd`, column inits `{addr: value}`, AI id + handler address | `13:AC00`, `27:A314` | `objects.js` |
| `metasprites.json` | per id 0-255: frames of `[dx, dy, tile, attr]` + mirror alias info (`meta_dump.meta_json`) | banks 28/29 | catalog only; the port reads `sprites_b28.bin` (s54) |
| `sprites_b28.bin` | PRG banks 28-29 as `$8000-$BFFF` (index = cpu addr − `$8000`): draw orders, orbit/burst tables, meta ptr tables + records, read through CPU addresses like `($15),Y` | banks 28/29 | `sprites.js` |
| `prg.bin` | all 32 PRG banks (s55); byte at bank:addr = `[bank·$2000 + (addr & $1FFF)]`; `prg.js` `cpuRd` maps `$8000`/`$A000` to `m.prgR6`/`m.prgR7` like MMC3 mode 0 | whole PRG | `prg.js` → `obj/*.js` (ObjTypeTable, ShotTable, bank 26/27 tables, BG collision pages) |
| `shots.json` | ShotTable records `{first, end, type, count}` for ids `$00-$5F` + `ShotOffsets` | `20:BC00/BD00/BD80` | `objects.js` |
| `tables.json` | named fixed-size tables (each `{bank, addr, fmt, values}`): level HP/MP/XP, jump heights, step speeds, probe points, hitboxes, collision pairs, draw orders, orbit/burst, pad dirs, Rand64 table, area-by-index, HUD elements, boss areas, sword attack, … | see file | many |
| `text.json` | dictionary, code 05/06/07 tables, all messages (`text_dump.py --json`) | banks 20-23 | `text.js` |
| `sound.json` | driver tables, DMC sample, music/SFX stream decode (`sound_export.py`) | banks 24/25, 12, 31 | `apu_preview.js`, tests |
| `sound_b24.bin`, `sound_b25.bin`, `sound_b12.bin` | raw 8 KB banks: driver tables + music streams (`$8000`/`$A000`), SFX streams (`$A000` via R7) | banks 24, 25, 12 | `sound/driver.js` (runs the real stream bytes) |
| `dmc.bin` | DMC sample `31:FA00` (length from `SndDmcTable`) | bank 31 | `apu_core.js` |
| `ram_labels.json` | `labels.csv` rows with bank `*` → `{name: addr}` | labels.csv | `ram.js` |

`web/data/catalog_manifest.json` stays owned by `tools/catalog_export.py` (catalog page only).

---

## 6. Verification plan (Ph7 web 3/8 - 8/8)

| Check | Tool | Pass criterion |
|---|---|---|
| Data integrity | `gen_web_data.py --check` | every manifest sha1 matches; re-run is byte-identical |
| Per-frame state | `web/test/run.mjs --compare` vs `python3 -m tools.emu --compare` → `tools/compare_frames.py` | state/sub/area/pos/cam/music/periods/pal_crc and 32 object slots identical per frame |
| Screens | `--dump-screen` vs `tools.emu` / `ref/` PNGs | pixel-identical (title, first gameplay frame, one later area) |
| Areas | world module vs `render_level.py` (`gfx/areas/`) | every renderable area identical |
| Sound | `driver.js` channel periods vs CSV `snd{c}_period` | identical |
