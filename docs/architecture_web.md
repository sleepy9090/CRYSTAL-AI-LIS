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
      ├─ src/nes/ppu.js               256×240 indexed framebuffer: 2 nametables (H mirroring), attrs, palette RAM,
      │                               OAM, CHR R0-R5 (mode 0), per-scanline scroll/CHR/mask split list, 8 spr/line
      ├─ src/nes/apu_core.js          2 pulse, triangle, noise, DMC; register writes $4000-$4017; sample generator
      ├─ src/game/ram.js              RAM/WRAM arrays + named address constants (generated, §5) + obj column helpers
      ├─ src/game/mainloop.js         MainLoop, MainStateTable, NMI work, IRQ split list, frame_ready/$09 handshake
      ├─ src/game/states/             state0_newgame.js state1_gameplay.js state2_intro.js state3_title.js state45.js
      │   └─ substates.js             SubstateHandlerTable (40) → handler functions; substate banks are irrelevant
      ├─ src/game/world/
      │   ├─ area.js                  AreaLoad, LoadAreaChrRecord, spawn handlers 0-4, AreaLoadMap, k1-k5, AreaEntryTable
      │   ├─ bg.js                    ScreenPagePtr, ScreenTileSubst, MetatileExpand, MetatileAttr, row/col render
      │   ├─ ppuqueue.js              PPU queue ring $6200 (records as in the ROM) + PpuQueueFlush into ppu.js
      │   ├─ camera.js                CameraUpdate, CameraFollow, CameraClampX, scroll copy $07D8-$07DB
      │   └─ exits.js                 ExitLookup (k3), AreaPitFall, AreaEdgeLink (k5), visited bitmap $62F0
      ├─ src/game/obj/
      │   ├─ objects.js               ObjInit (ObjTypeTable records), ObjUpdateAll, free-slot search, respawn tick
      │   ├─ ai_table.js              ObjAiTable (128 ids) → JS functions; unported ids log once + despawn-safe no-op
      │   ├─ ai/*.js                  generic enemies, NPC/path, platforms, bosses (BossPhaseTable), shots
      │   ├─ player.js                PlayerMove (slot 0), PlayerUpdate (slot 1), sword/charge, PlayerMagic, SpendMp
      │   ├─ move.js                  ObjStepVector, ObjMoveStep, BgProbePoints, ObjBgProbe, ObjScreenEdge
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
      ├─ src/game/sound/
      │   ├─ playsound.js             PlaySound FIFO $0103 (5 deep), SoundUpdate (≤1 command per call)
      │   └─ driver.js                SndCommand, SndStartMusic/Sfx, SndTick, 16 stream ops, envelopes, vibrato quirk
      ├─ src/game/palette.js          PaletteBuild, fades, PaletteUpload ($6140-$615F → ppu palette RAM)
      ├─ src/game/pads.js             ReadPads / ReadPadsMenu ($43-$4F hold/repeat logic) from the button byte
      └─ src/game/rand.js             Rand64 (26:97D7 table walk on $0E)
web/test/run.mjs                      Node harness (Ph7 web 3/8): --keys, --frames, --dump-screen, --compare
web/test/png.mjs                      minimal PNG encoder (zlib from node:zlib)
web/catalog.html, catalog.js, apu_preview.js   existing asset catalog (session 27), kept separate from the port
```

Dependency rule: `game/*` modules import `ram.js`, `data.js` and siblings, never `main.js`/DOM. `nes/ppu.js` and
`nes/apu_core.js` know nothing about the game; the game talks to them only through the same registers/state
the ROM writes (CHR regs, scroll, PPU queue writes, OAM shadow, `$4000-$4017`).

### 2.1 Machine.runFrame(buttons)

```
runFrame(buttons):
  pads.latch(buttons)                       // what $4016 would return this frame
  if ram[$09] == 0 (frame_ready): mainloop.step()     // MainStateTable[$40] → state/substate handler
  // the handler may span several frames in the ROM (wait loops on $09); see §4
  ppu.beginFrame(splitList)                 // splits registered by the IRQ set-up of the NMI-mode routine
  irqWork()                                 // e.g. handler 1: HUD split at line 190 + SoundUpdate
  nmiWork()                                 // OAM DMA if !overran, scroll copy, PpuQueueFlush, PaletteUpload,
                                            // NmiModeDispatch[$51], INC $09
  ppu.render()                              // framebuffer for this frame (uses split list)
  apu.endFrame()                            // flush audio samples for 1/60.0988 s
```

---

## 3. EXE → web mapping

Every ROM routine the port reproduces, with its JS home. File offsets are in `docs/architecture_exe.md`.

### 3.1 Frame skeleton, hardware, data

| ROM routine | Address | JS function | Module |
|---|---|---|---|
| `Reset` | `31:F2A4` | `Machine.reset()` (cold/warm "USO" check, `BootSaveCheck`, WRAM `$7D00→$6480` boot copy) | `game/machine.js` |
| `MainLoop` / `MainStateTable` | `30:C900` / `30:C91F` | `mainloop.step()` / `MAIN_STATES[6]` | `game/mainloop.js` |
| `NMI` | `31:F3B6` | `mainloop.nmiWork()` | `game/mainloop.js` |
| `NmiModeDispatch` / `JmpTable31_F6C2` | `31:F6AD` / `31:F6C2` | `NMI_MODES[irq_mode]` | `game/mainloop.js` |
| `IRQ`, `SetIrqHandler`, `IrqHandlerTable` | `31:F443`, `31:F424`, `31:F433` | `setIrqHandler(i)` → split list entries for `ppu.js` | `game/mainloop.js` |
| `PpuQueueFlush` | `30:C67D` | `ppuqueue.flush(ppu)` | `game/world/ppuqueue.js` |
| `PaletteUpload` | `31:F8CB` | `palette.upload(ppu)` | `game/palette.js` |
| `PaletteBuild`, fades | `26:8C0E`, `26:8C3D/8C49` | `palette.build()`, `palette.fadeOut/In()` | `game/palette.js` |
| `ApplyChrPlay` / `ApplyChrAlt` | `31:F883` / – | `ppu.setChr(R0..R5)` from `$07F0-$07F5` / `$58-$5D` | `game/mainloop.js` |
| `ReadPads` / `ReadPadsMenu`, `PadDirTable` | `31:FE80`, `31:FED0` | `pads.read()` / `pads.readMenu()` | `game/pads.js` |
| `Rand64` | `26:97D7` | `rand64()` | `game/rand.js` |
| far-call trampolines | `31:FF80-FFE2` | plain calls (no banks) | – |

### 3.2 States and substates

| ROM routine | Address | JS function | Module |
|---|---|---|---|
| state 0 new game | `30:C957` | `state0()` | `states/state0_newgame.js` |
| state 1 gameplay dispatcher | `30:CAB6` | `state1()` → `SUBSTATES[$41]` | `states/state1_gameplay.js` |
| state 2 / `MainState2Init` / intro | `30:C931`, `18:9FCC`, `18:8028` | `state2()`, `intro()` | `states/state2_intro.js` |
| state 3 / `MainState3`, `State3ModeTable`, `State3SubTable` | `30:C939`, `19:A015`, `19:A038`, `19:A052` | `state3()` (title menu, story pages, status, name entry) | `states/state3_title.js` |
| states 4/5 | `17:A000` / `17:A100` | `state4()` / `state5()` | `states/state45.js` |
| `SubstateHandlerTable` | `30:CADE` | `SUBSTATES[40]` | `states/substates.js` |
| `SubstNewGameInit`, area-load step | `30:CA5F`, `30:CA2E` | `substNewGameInit()`, `substAreaLoad()` | `states/substates.js` |
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
| `AreaLoadMap` / k1 / k2 / k4 | `31:E628` / `31:E6B9` / `31:E686` / `30:C765`, `31:E702` | `loadAreaMap()`, `loadAreaGfx()`, `placePlayer()`, `applyScreenFlags()` | `world/area.js` |
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
| `ObjUpdateAll`, `ObjAiTable` | `30:CCCC`, `27:A314` | `objUpdateAll()`, `AI[128]` | `obj/objects.js`, `obj/ai_table.js` |
| `BossPhaseDispatch`, `BossPhaseTable` | `15:A400`, `15:A413` | `bossPhase(slot)` | `obj/ai/boss.js` |
| generic enemy AIs + helpers (`DirToTarget`, `AiShootCheck`, `SteerToward`) | bank 27 | `ai/enemies.js` | `obj/ai/` |
| `PlayerMove` / `PlayerUpdate` / `PlayerMagic` / `SpendMp` | `26:9D9A` / `26:9B45` / `27:A022` / – | `playerMove()`, `playerUpdate()`, `playerMagic()`, `spendMp()` | `obj/player.js` |
| `ObjStepVector`, `ObjMoveStep`, `BgProbePoints` | `26:8480`, `26:9947`, `26:9907` | `stepVector()`, `moveStep()`, `BG_PROBE` (data) | `obj/move.js` |
| `SpawnShot`, `ShotTable`, `ShotOffsets` | `26:972D`, `20:BC00`, `20:BD80` | `spawnShot(id, dir)` (`shots.json`) | `obj/objects.js` |
| `ObjCollideAll`, `CollPairTable`, `HitboxTable`, `HitHandlerTable` | `26:8F6E`, `26:96F1`, `26:9691`, `26:8FBB` | `collideAll()`, `HIT_HANDLERS[4]` | `obj/collide.js` |
| `HitPlayer`, `EnemyKilled`, `AddXp` | `26:9357`, `26:9152`, `26:924B` | `hitPlayer()`, `enemyKilled()`, `addXp()` | `obj/collide.js` |
| `PlayerStatusTick` | `31:EF55` | `playerStatusTick()` | `obj/status.js` |
| `ObjSpriteDraw`, `ObjDrawMeta`, `ObjDrawOrderTable`, orbit ring | `28:800E`, `28:822E`, `28:83DC`, `28:80D6` | `spriteDraw()`, `drawMeta()` | `obj/sprites.js` |

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
   │ 4 / 5 (17:A000 / A100) — not reached in any emulator run; ported last
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
| `metasprites.json` | per id 0-255: frames of `[dx, dy, tile, attr]` + mirror alias info (`meta_dump.meta_json`) | banks 28/29 | `sprites.js` |
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
