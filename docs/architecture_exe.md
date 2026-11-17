# re-c (NES) — Executable Architecture

Written in session 48. It summarises REVERSE.md, which stays the source of truth: every claim here is
covered in detail by the REVERSE.md section named in brackets. Notation is `BB:AAAA` (8KB PRG bank 0-31 : CPU address).
File offset = `0x10 + bank*0x2000 + (addr & 0x1FFF)`. "VERIFIED" means checked in `tools.emu` (self-emu),
plus 12 FCEUX reference frames for the screens (Verification Log).

---

## 1. Binary layout

| Part | File range | Size | Notes |
|---|---|---|---|
| iNES header | `0x00000-0x0000F` | 16 | `4E 45 53 1A 10 10 42 00`: mapper 4 (MMC3), battery, horizontal |
| PRG ROM | `0x00010-0x4000F` | 256 KB | 32 × 8KB banks |
| CHR ROM | `0x40010-0x6000F` | 128 KB | 128 × 1KB banks (MMC3 CHR mode 0: R0/R1 2KB at `$0000`, R2-R5 1KB at `$1000`) |

**CPU windows (MMC3 PRG mode 0):** `$8000-$9FFF` = R6, `$A000-$BFFF` = R7, `$C000` = bank 30 (fixed), `$E000` = bank 31 (fixed, vectors).
The game mostly maps 16KB pairs (2n, 2n+1) with `SwitchBank16` `30:C40E` (A = n), keeping shadows `$6E/$6F` (R6/R7) and `$50` (select reg).
Single-window swaps use `SetR6` `30:C418` / `SetR7` `30:C427`. [Architecture → Bank switching]

**Vectors:** RESET `31:F2A4` (file `0x3F2B4`), NMI `31:F3B6` (`0x3F3C6`), IRQ `31:F443` (`0x3F453`).

### 1.1 Bank map (module map per bank)

| Bank | File offset | Pair n | Mapped by | Contents | Code % (s22) |
|---|---|---|---|---|---|
| 0-1 | `0x00010` | 0 | `ScreenPagePtr` (screen id `$00-$3F`) | **screen pages**: 256 B = 16×15 metatile ids + 16-byte tail | 0 |
| 2-3 | `0x04010` | 1 | screen ids `$40-$7F`; `SetR7(2/3)` | screen pages; page tails = **palette records** (256 slots × 4 B) | 0 |
| 4-5 | `0x08010` | 2 | screen ids `$80-$BF`; spawn handler 1 | screen pages; page tails = **handler-1 spawn records** | 0 |
| 6-7 | `0x0C010` | 3 | screen ids `$C0-$FF` | screen pages | 0 |
| 8-9 | `0x10010` | 4 | `MetatileExpand`, `SetR7(9)` | metatile quadrant pages (4×256 B per tileset), attr tables `9:B000`, visited-substitution tables `9:BE00` | 0 |
| 10 | `0x14010` | 5 | `AreaSubTable` `30:C7A5`; `SetR6($0A)` | `AreaHeaderTable` `10:8300/8400` (k0-k5 sub-tables); bank-10 screen pages (`$62FF` b7 areas) | 0 |
| 11 | `0x16010` | 5 | same | header data; `Wram6000InitTable` `11:BE00` (`SetR6($0B)`) | 0 |
| 12 | `0x18010` | 6 | `AreaLoad`; SFX via `SetR7(12)` | `AreaPtrTable` `12:9201/9301`; `SfxPtrTable` `12:A000` + SFX streams | data |
| 13 | `0x1A010` | 6 | `AreaLoad`; `ObjInit` `SetR7($0D)` | area records (CHR + spawn lists); `ObjTypeTable` `13:AC00/AD00` | 0 |
| 14 | `0x1C010` | 7 | `SwitchBank16(7)` | NPC scripts `NpcScript` `14:8000`, telepathy, `ItemGetTable`/`ItemUseTable`/`ItemUseHandlers` | 15 |
| 15 | `0x1E010` | 7 | n=7; `SetR7($0F)` for AI `$60-$6F` | boss/event AI: `BossPhaseDispatch` `15:A400`, `BossPhaseTable` `15:A413`, `EventTriggerTable` `15:A17A` | 41 |
| 16 | `0x20010` | 8 | `30:D8DD` (16 at `$8000` + 23 at `$A000`) | menus (inventory/save menu); calls `SaveJumpTable` `23:BC00` | 59 |
| 17 | `0x22010` | 8 | `C944/C94C` | main states 4/5 entries `17:A000`/`A100`; `W H` rectangle data | 4 |
| 18 | `0x24010` | 9 | `C931`, Reset cold path | intro (`18:8028`), `MainState2Init` `18:9FCC` (runs on into 19), state-3 modes 1/2; rectangles | 11 |
| 19 | `0x26010` | 9 | `C939` | `MainState3` `19:A015`: title menu, story pages, status screen, name entry; common screen setup `19:BA65`; `PlayerDeath` `19:B900` | 29 |
| 20 | `0x28010` | 10 | text printer; `SwitchBank16(10)`; `SetR7($14)` | `TextPrint` `20:853A`, message/dictionary ptr tables, `WakeCutscene` `20:9400`, `ShotTable` `20:BC00` | 19 |
| 21-22 | `0x2A010` | (single `$A000`) | `TextGroupBank` `20:83FE` | dialogue text groups 0-25 | 0 |
| 23 | `0x2E010` | (single `$A000`) | text printer; `SetR7($17)` ×7 | text groups 26-33; **save/load module** `23:BC00-BDFF`; `AreaSaveFlags` `23:BF00` | 6 |
| 24 | `0x30010` | 12 | Reset; `SoundUpdate` writes R6 directly | **sound driver**: `SndInit` `8020`, `SndCommand` `8048`, `SndTick` `8186`, opcode table `8000`, music table `8C0C` | 22 |
| 25 | `0x32010` | 12 | `SoundUpdate` R7=25 | music stream data | 0 |
| 26 | `0x34010` | 13 | `SwitchBank16(13)`; `SetR6($1A)`; far-calls | player (`PlayerUpdate` `9B45`), palette (`PaletteBuild` `8C0E`, fades `8C3D/8C49`), HUD (`HudRedraw` `8E46`), collision (`ObjCollideAll` `8F6E`), damage/XP (`HitPlayer` `9357`, `EnemyKilled` `9152`), movement (`ObjMoveStep` `9947`, `ObjBgProbe` `9A30`), shots (`SpawnShot` `972D`) | 57 |
| 27 | `0x36010` | 13 | same | object AI: `ObjAiTable` `27:A314` (128 ids), generic enemy AIs, NPC/path AIs, `PlayerMagic` `A022` | 68 |
| 28 | `0x38010` | 14 | `SwitchBank16(14)`, far-call `FF8C` | **sprite pass** `ObjSpriteDraw` `28:800E`, `ObjDrawMeta` `822E`, orbit ring `80D6`, `ObjMetaPtrTableA/B` `845C/855C` | 11 |
| 29 | `0x3A010` | 14 | (as `$A000` half of n=14) | metasprite records (ids ≥ `$80` mostly) | 0 |
| 30 | `0x3C010` | fixed `$C000` | – | main loop, state + substate dispatch, `ObjInit`, `ObjUpdateAll`, `PlaySound`, PPU queue flush, NPC talk/events, menus glue | 77 |
| 31 | `0x3E010` | fixed `$E000` | – | Reset/NMI/IRQ, IRQ handlers, area load + BG render + camera, exits, `PlayerStatusTick`, pads, `SoundUpdate`, `PaletteUpload`, CHR apply, boot save check, far-call trampolines, DMC sample `31:FA00-FDE0` | 72 |

Dead stale-copy filler (24 ranges, 4102 bytes) and the full-ROM code coverage (40,170 code bytes, 15.3 %) are in
REVERSE.md "Stale-copy filler" / "Code coverage"; every bank is disassembled to `disasm/bank_NN.asm` (round-trips byte-exact).

---

## 2. Call graph from the entry points

Format: `routine` `BB:AAAA` (file offset) — what it does. Indentation = calls / dispatch.

### 2.1 Reset → main loop

```
Reset                     31:F2A4 (0x3F2B4)   [Architecture → Boot] VERIFIED
├─ hw init: PPU off, APU IRQs off, MMC3 mirroring/WRAM/IRQ off, $8000=0, $51=3, IRQ vec $0054 ← $F421
├─ fill $6200-$621F, $6140-$615F with $FF
├─ SwitchBank16(12) → SndInit                 24:8020 (0x30030)
├─ main_state $40 = 2, clear $0700-$07FF, PPUCTRL=$88, $07FE=9, CLI
├─ warm boot? ($0182-$0184 == "USO") ─ no ─→ ColdSaveInit 31:F1CD (0x3F1DD), ReadPads,
│                                            SwitchBank16(9) → intro 18:8028 (0x24038) (~212 frames, IRQ handler 6)
├─ BootSaveCheck          31:F0A4 (0x3F0B4)
├─ copy WRAM $7D00-$7EFF → $6480-$657F (boot copy quirk)
└─ JMP MainLoop           30:C900 (0x3C910)
```

### 2.2 Main loop and main states

```
MainLoop                  30:C900 (0x3C910)   VERIFIED
  wait frame_ready $09 (set by NMI) → JMP MainStateTable[$40] (30:C91F, 0x3C92F) via $10 trampoline → clear $09 → loop

  state 0  30:C957 (0x3C967)  new-game init: clear story/NPC flags $6480/$6580, inventory $6428; area $6C=0 entry $6D=1;
                              zero player stats; main_state=1, sub_state=0
  state 1  30:CAB6 (0x3CAC6)  GAMEPLAY: JSR $CC2E unless sub_state 7/8; DEC $08;
                              SwitchBank16(SubstateBankTable[$41]) (30:CB2E); JMP SubstateHandlerTable[$41] (30:CADE)
  state 2  30:C931 (0x3C941)  SwitchBank16(9); JMP MainState2Init 18:9FCC (0x25FDC) → falls into 19:A000; INC main_state
  state 3  30:C939 (0x3C949)  SwitchBank16(9); ReadPadsMenu; JMP MainState3 19:A015 (0x26025)
                              → State3ModeTable 19:A038[$06F3] → State3SubTable 19:A052[$06F0]
                                (title menu, story pages, status screen, name entry → writes main_state 0)
  state 4  30:C944 (0x3C954)  SwitchBank16(8); JMP 17:A000 (0x22010)   (not reached in any emulator run)
  state 5  30:C94C (0x3C95C)  SwitchBank16(8); ReadPadsMenu; JMP 17:A100 (0x22110)
```

**State 1 substates** (`$41`, `SubstateHandlerTable` `30:CADE`, 40 words, 10 null; bank pair from `SubstateBankTable`: 3→n=9, 12/13→n=7, else n=0):

| $41 | Handler | Meaning | | $41 | Handler | Meaning |
|---|---|---|---|---|---|---|
| 0 | `30:CA5F` | `SubstNewGameInit` | | 20 | `30:DB28` | `SubstTeleportMenu` |
| 1 | `30:CA2E` | (area-load step) | | 21 | `30:DD6B` | `SubstTelepathySelect` |
| 3 | `19:B900` | `PlayerDeath` (n=9) | | 22 | `30:DE04` | `SubstTelepathyReply` |
| 4 | `30:CBFC` | warp/continue (`AreaByIndexTable`) | | 23 | `30:DEC1` | `SubstChangeForm` |
| 6 | `30:D497` | `ItemUseMenu` | | 24-27 | `19:BB39 BB9D BC04 BC6B` (n=9) | 26 = `SwordRaisePose` cut-scene |
| 7 | `30:D3EB` | `GiveItem` | | 28 | `30:CC17` | |
| **8** | **`30:CB62`** | **`SubstPlay` — the normal gameplay frame** | | 29 | `30:DB28` | `SubstTeleportMenu` (2nd entry) |
| 12/13 | `15:BC40` / `15:BDDB` | (n=7: bank 15 scenes) | | 30-31 | `19:BDF2` (n=9), `30:CC4D` | |
| 16 | `30:D085` | `ShowPendingMessage` | | 32-36 | `30:D985 D98E D997 D9AD D9C1` | |
| 17 | `30:D0C8` | `NpcTalk` | | | | |
| 18 | `30:D8C7` | | | | | |
| 19 | `30:D9B6` | `SubstWakeCutscene` → `20:9400` | | | | |

`SubstateBankTable` `30:CB2E` (40 bytes): n=9 for 3, 24-27, 30; n=7 for 12/13; n=0 otherwise. Unnamed substates are
still open (REVERSE.md task "decode `30:CADE`/`CB2E` substate tables").

### 2.3 Gameplay frame (`SubstPlay` `30:CB62`, 0x3CB72)

```
SubstPlay                 30:CB62   [Player / World interaction] VERIFIED order (s39)
├─ ReadPads               31:FE80 (0x3FE90)   pad_raw $4F, pad_dir $49, pad_new $4B, pad_act $4D
├─ 30:CB84                death / START (menu) / SELECT checks → change $41
├─ PlayerStatusTick       31:EF55 (0x3EF65)   hazards, poison, regen, status timers
├─ ObjUpdateAll           30:CCCC (0x3CCDC)   SwitchBank16(13) → banks 26/27
│   for slot X = 0..30: knockback ($0340 hi) → freeze $05A0 → ObjAiTable[obj_active&$7F] 27:A314 (0x36324)
│       AI $01 = PlayerUpdate 26:9B45 (0x35B55); AI $60-$6F → SetR7($0F), handlers in 15:$A000
│       bosses: BossPhaseDispatch 15:A400 (0x1E410) → BossPhaseTable[base + $0600,X]
├─ CameraUpdate           31:E90A (0x3E91A)   clamp, then BgColRender 31:EC87 / BgRowRender 31:EB91 on 8-px cell change
│       → ScreenPagePtr 31:EF36 → ScreenTileSubst 31:ED99 → MetatileExpand 31:EDBA → MetatileAttr 31:EDE5
│       → PPU queue $6200 (4-byte records, ring of 8)
├─ ObjSpriteDraw          28:800E (0x3801E)   SwitchBank16(14): 4 draw orders by $07FF, ObjDrawMeta, hide unused, orbit ring
├─ ObjCollideAll          26:8F6E (0x34F7E)   CollPairTable 26:96F1, HitHandlerTable 26:8FBB
│       → HitPlayer 26:9357 / hit handler 0 26:90AA → EnemyKilled 26:9152 (XP, level up)
└─ JMP 31:E0B8 (0x3E0C8)  unless boss_cam_lock: JSR 31:E75E; every 16 frames re-checks one spawn slot
                          $0D + ($08>>4) and, if empty, re-reads the area record (respawn tick, static RE only)
```

Area changes: `ExitLookup` `31:EB09` (k3 exit list) / `AreaPitFall` `31:E544` → `AreaEdgeLink` `31:E573` (k5) / scripted
warps set `$6C/$6D` → `AreaLoad` `31:E14A` (0x3E15A): `AreaPtrTable` record → CHR R4/R5 + palettes `$07E6/7` + spawns
(`SpawnHandlerTable` `31:E218` → `ObjInit` `30:C25D` (0x3C26D) from `ObjTypeTable` `13:AC00`), `AreaLoadMap` `31:E628`
(k0 music + grid → `$6300`), k1 (BG palettes, tileset `$67`, CHR R0/R1), k2 (player start), k4 (persistent screen flags).
Area entry dispatch `JmpTable31_E3D9` on `$6D>>4`. [Area records; Area header sub-tables; World interaction]

### 2.4 NMI

```
NMI                       31:F3B6 (0x3F3C6)   [Architecture → NMI] VERIFIED
├─ $60≠0 → RTI;  $06≠0 (IRQ handler busy) → $07=1, RTI
├─ if frame_ready $09 == 0: OAM DMA $4014←$02; $07D8-$07DB ← camera $02-$05
│                            (irq_mode 7/9: ← object slot $13 x/y instead)
├─ PpuQueueFlush          30:C67D (0x3C68D)   ring $6200 from $0A to $0B → PPUCTRL inc bit, PPUADDR, data
├─ PaletteUpload          31:F8CB (0x3F8DB)   pal_buf $6140-$615F → $3F00 (skipped if $51 b7)
├─ PPUMASK ← $01
├─ NmiModeDispatch        31:F6AD (0x3F6BD)   JMP JmpTable31_F6C2[irq_mode $51]
│       mode 0/7: SoundUpdate only if rendering off (F6DC); mode 3 (title): SoundUpdate (F77F); others set up IRQ / CHR
└─ INC $09
```

### 2.5 IRQ (MMC3 scanline splits)

```
IRQ                       31:F443 (0x3F453)   ack MMC3, JMP ($0054)
SetIrqHandler             31:F424 (0x3F434)   $54/$55 ← IrqHandlerTable 31:F433[A], $56=A
  0 F455  mid-frame scroll split from $07D8 (in-area effects)
  1 F488  GAMEPLAY HUD split at line 190: sprites off, PPUADDR $2B00, scroll X 0, CHR R0=$3C R1=$38,
          then SoundUpdate 31:F7FE (0x3F80E) with $06=1, then the missed NMI-mode routine if $07≠0
  2/3     line 10/14 and 85/89 splits (seen in the area tour)
  5 F611  calls SoundUpdate (not seen in runs)
  6 F691  title / intro split (line 137)
  4, 7    never seen
  default $F421 (set by Reset)
```

### 2.6 Sound

```
PlaySound                 30:C125 (0x3C135)   game API (85 callers): id → FIFO $0103.. (5 deep); music ids <$20, SFX $20-$9F
SoundUpdate               31:F7FE (0x3F80E)   R6=24 R7=25 directly; ≤1 SndCommand per call; SndTick; restore R6/R7/$8000
  ├─ SndCommand           24:8048 (0x30058)   → SndStartMusic 24:80E4 / SndStartSfx 24:8094 (SFX headers in bank 12)
  └─ SndTick              24:8186 (0x30196)   4 music + 2 SFX channels; SndReadOp 24:8347 → SndOpcodeTable 24:8000
```
Called once per frame from IRQ handler 1 (gameplay), NMI mode 3 (title) or NMI mode 0 (rendering off).
**Quirk:** on an area-load frame both IRQ 1 and NMI mode 0 run it → 2 driver ticks in that frame. [Sound driver; Sound data format]

### 2.7 Far-call trampolines (`31:FF80-FFE2`)

Push `$6E/$6F`, map, call, `JMP RestoreBanks` `31:FFDB` (0x3FFEB):
`FF80`→`ObjInit` `$C25D` · `FF8C`→`28:800E` sprite pass · `FF9D`→`BgRowRender` `$EB91` · `FFA9`→`26:8E46` `HudRedraw` (A via `$10`) ·
`FFBE`→`26:8CC0` · `FFCF`→`$D354` (message wait).

### 2.8 Other subsystem entry points

| Subsystem | Entry | File | Reached from |
|---|---|---|---|
| Text printer | `TextPrint` `20:853A` | `0x2854A` | message display, `TextCtrlTable` `20:85A2`; banks 21-23 mapped one at a time at `$A000` |
| NPC scripts | `NpcScript` `14:8000` | `0x1C010` | `NpcTalk` `30:D0C8`; actions `TalkActionTable` `30:D123`, `EventActionTable` `30:D563` |
| Telepathy | `Telepathy` `14:816F` | `0x1C17F` | `SubstTelepathyReply` |
| Item use | `ItemUseHandlers` `14:8399` | `0x1C3A9` | `PlayerUpdate` B-button, `ItemUseMenu` |
| Menus | `16:8000` (+23 at `$A000`) | `0x20010` | `30:D8DD` |
| Save/load | `SaveJumpTable` `23:BC00`, `SaveGame` `23:BC09` | `0x2FC10`, `0x2FC19` | autosave areas, menu |
| Palette build | `PaletteBuild` `26:8C0E` | `0x34C1E` | 18 callers; fade entries `26:8C3D/8C49` |
| HUD | `HudRedraw` `26:8E46` | `0x34E56` | far-call `FFA9`, level-up, pickups |

---

## 3. Per-frame execution flow (gameplay)

```
 scanline  0 ─────────────── visible playfield ────────────────
           MainLoop wakes (frame_ready=1) and runs SubstPlay:
             ReadPads → menus check → PlayerStatusTick → ObjUpdateAll (AI per slot)
             → CameraUpdate (queues BG column/row into $6200) → ObjSpriteDraw (fills $0200 OAM shadow)
             → ObjCollideAll (uses $05C0/$05E0 written by the sprite pass) → respawn tick
           MainLoop clears $09 and waits
 scanline 190 ── IRQ handler 1: HUD split (nametable 2 bottom, CHR R0/R1 = HUD font), SoundUpdate (1 command + SndTick)
 scanline 241 ── NMI: OAM DMA (if the main loop finished), scroll copy, PPU queue flush, PaletteUpload,
                  PPUMASK, NMI-mode routine (CHR apply / IRQ set-up), INC $09
```
If the main loop overruns (`$09`≠0 at NMI), the DMA and scroll copy are skipped (previous OAM shown again).
If the NMI hits while the IRQ handler holds `$06`, it only sets `$07` and IRQ handler 1 runs the NMI-mode routine itself.

Title/menus (states 2/3) use the same skeleton with `irq_mode` 3/5, IRQ handler 6 and SoundUpdate from NMI mode 3.

---

## 4. Data flow (ROM tables → RAM structs → PPU/APU)

| ROM source | Reader | RAM / WRAM target | Output |
|---|---|---|---|
| `AreaHeaderTable` `10:8300` k0 grid | `AreaLoadMap` `31:E628` | `$62FC-$62FF` dims/flags, grid `$6300-$637F` | – |
| k1 | `31:E6B9` | BG palette ids `$07E0-$07E3`, tileset `$67`, `$69`, CHR R0/R1 `$07F0/$07F1` | – |
| k2 / k3 / k4 / k5 | `31:E686` / `ExitLookup` / `30:C765`, `31:E702` / `31:E573` | player start `$34-$37`; `$6C/$6D`; visited bitmap `$62F0-$62FB` + flags `$64C0` | – |
| screen pages banks 0-7/10 | `ScreenPagePtr` → `ScreenRowRender`/`BgColRender` | column/row buffer `$6000`, attr shadow `$6100` | PPU queue `$6200` → nametables (NMI) |
| metatiles `8/9:$67&$FC`, subst `9:BE00`, attrs `9:B000` | `MetatileExpand`, `ScreenTileSubst`, `MetatileAttr` | same | same |
| `AreaPtrTable` `12:9201` records | `AreaLoad`/`LoadAreaChrRecord` `31:E16D` | `$07E6/$07E7`, CHR R4/R5 `$07F4/$07F5`, spawns slots `$0D-$1F` | MMC3 CHR via `ApplyChrPlay` `31:F883` |
| `ObjTypeTable` `13:AC00` | `ObjInit` `30:C25D` | object columns `$0300-$06E0` (stride `$20`) | – |
| AI code banks 26/27/15 | `ObjUpdateAll` | object x/y `$70/$90/$B0/$D0`, state columns | – |
| `ObjMetaPtrTableA/B` `28:845C/855C` → banks 28/29 | `ObjDrawMeta` `28:822E` | OAM shadow `$0200-$02FF`; screen pos `$05C0/$05E0` | OAM DMA (NMI) |
| palette records banks 2/3 page tails | `PaletteBuild` `26:8C0E` | `pal_buf` `$6140-$615F` (fade `$11`, mask `$07E8`) | `PaletteUpload` → `$3F00` (NMI) |
| messages banks 21-23, dictionary `20:8900` | `TextPrint` `20:853A` | text buffer / PPU queue | nametable text window |
| `SndMusicTable` `24:8C0C`, streams 24/25, SFX `12:A000` | `SndCommand`/`SndTick` | `$0100-$017F`, ZP `$F0-$FF` | APU `$4000-$4015`; DMC sample `31:FA00` |
| `HudElemTable` `26:8EC5` | `HudRedraw` | – | nametable 2 `$2B39…` |
| `Wram6000InitTable` `11:BE00` | `30:CCA4`, `19:BE5E` | `$6000-$60FF` | – |
| save image | `SaveGame` `23:BC09` | `$7D00-$7FFF` (+ mirror), slots `$7100-$7CFF` | battery |

---

## 5. Shared RAM / WRAM regions

Full list: `docs/ram_map.txt` (`tools/ram_map.py`), object columns `docs/obj_struct.txt`, save layout `docs/save_layout.txt`.

| Range | Owner / meaning |
|---|---|
| `$00-$05` | PPUCTRL / PPUMASK shadows, camera X/Y `$02-$05` (drawn camera) |
| `$06/$07` | IRQ-busy / missed-NMI flags |
| `$08` | frame counter (DEC each state-1 frame) |
| `$09` | `frame_ready` (NMI sets, main loop clears) |
| `$0A/$0B` | PPU queue read/write index (ring `$6200`, 8 × 4-byte records) |
| `$10-$12` | `JMP abs` trampoline / scratch |
| `$34-$37`, `$3C-$3F` | shared position cursor / camera candidate |
| `$40/$41` | `main_state` / `sub_state` |
| `$43-$4F` | pads: `pad_prev` `$43`, hold counters `$45/$47`, `pad_dir` `$49`, `pad_new` `$4B`, `pad_act` `$4D`, `pad_raw` `$4F` |
| `$50`, `$6E/$6F` | MMC3 select shadow, R6/R7 shadows |
| `$51`, `$54-$56` | `irq_mode`, IRQ vector + handler index |
| `$58-$5D` | alt CHR shadows (`ApplyChrAlt`) |
| `$66-$6B` | tileset (`$67`), subst ptr |
| `$6C/$6D` | `area_id` / `area_entry` |
| `$70-$DF` | object x lo/hi, y lo/hi (4 × 32) |
| `$F0-$FF` | sound driver ZP |
| `$0100-$017F` | sound queue (`$0100-$0112`) + 6-channel state (`$0114-$017F`) |
| `$0182-$0184` | warm-boot signature "USO" |
| `$0200-$02FF` | OAM shadow |
| `$0300-$06FF` | object columns, 32 slots, stride `$20` (slot 1 = player body, slot 0 = ground body) |
| `$0700-$07FF` | player stats (gold `$0702`, XP `$0704`, next XP `$0706`, MP `$0708/9`, equipment `$0711-$0719`), scroll copies `$07D8-$07DB`, palette ids `$07E0-$07E8`, CHR shadows `$07F0-$07F5`, `$07FE` IRQ delay, `$07FF` draw rotation |
| `$6000-$60FF` | WRAM scratch (BG column buffer; ZP save during fades) |
| `$6100-$613F` | attribute shadow |
| `$6140-$615F` | `pal_buf` |
| `$6200-$621F` | PPU queue |
| `$62F0-$62FF` | visited-screen bitmap, area dims/flags |
| `$6300-$637F` | screen grid |
| `$6428-$6477` | inventory / item selections |
| `$6480-$657F` | story flags `$6480`, screen flags `$64C0`, NPC state `$6580` (copied from save at boot) |
| `$6D00`, `$7100-$7CFF`, `$7D00-$7FFF` | save mirror, save slots, current save image |

---

## 6. Where to read more

REVERSE.md sections: Binary Identification, Memory Map, Data-Range Map, Verification Log, Architecture (Boot, Bank switching,
Sound, Main loop, NMI, IRQ, Sprite pass, Area records, Screen/metatile pipeline, Controller input), Full-ROM disassembly,
Text / dialogue, Player, World interaction, Enemies, Save / load module, Data Structures (struct index + field tables),
Pointer tables + bank references. Generated reports live in `docs/*.txt` and `gfx/`.
