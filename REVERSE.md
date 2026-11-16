# re-c (NES) — Reverse Engineering Notes

> Working name: **re-c** (from the ROM filename). Do NOT attempt to identify the game title — out of scope by user request.

## Binary Identification

| Field | Value |
|-------|-------|
| File | `rom/re-c.nes` (copy of `re-c.nes`) |
| Format | iNES 1.0 (header `4E 45 53 1A 10 10 42 00 …`) |
| Platform | Nintendo Entertainment System / Famicom |
| CPU | Ricoh 2A03 (6502, no decimal mode) |
| Mapper | 4 — MMC3 (TxROM, probably TKROM/TLROM-class with battery) |
| PRG ROM | 256 KB = 16 x 16KB = **32 x 8KB banks** (file `0x00010-0x4000F`) |
| CHR ROM | 128 KB = 16 x 8KB = 128 x 1KB banks (file `0x40010-0x6000F`) |
| Mirroring | header: horizontal (MMC3 overrides at runtime; reset writes `$A000=1` → horizontal) |
| Battery / PRG-RAM | yes — 8KB WRAM at `$6000-$7FFF`, enabled by reset (`$A001=$80`) |
| Trainer | none |
| Header size | 16 bytes |
| File size | 393,232 (0x60010), no trailing data |
| CRC32 file / PRG+CHR | `F54A8E9F` / `FDACD5A8` |
| SHA1 PRG+CHR | `314fdc697f19db2418f0947794faa5b0e49b5703` |
| Entry point | RESET `31:F2A4` (file `0x3F2B4`), NMI `31:F3B6`, IRQ `31:F443` |

Banks noted in tools as **8KB bank numbers 0-31**. File offset = `0x10 + bank*0x2000 + (addr & 0x1FFF)`.

---

## Memory Map

| Address Range | Purpose |
|---------------|---------|
| `$0000-$00FF` | Zero page (game vars; `$10-$12` = JMP trampoline) |
| `$0100-$01FF` | Stack; `$0182-$0184` = warm-boot signature "USO" |
| `$0200-$02FF` | OAM shadow (DMA'd by NMI via `$4014=$02`) |
| `$0300-$07FF` | General RAM (`$0700-$07FF` cleared at boot; `$07D8-$07DB` scroll copies; `$07FE` IRQ delay) |
| `$2000-$2007` | PPU registers |
| `$4000-$4017` | APU / OAM DMA / joypads |
| `$6000-$7FFF` | 8KB battery WRAM (save data; `$7D00-$7EFF` copied to `$6480-$657F` at boot) |
| `$8000-$9FFF` | MMC3 R6 — swappable 8KB PRG (PRG mode 0) |
| `$A000-$BFFF` | MMC3 R7 — swappable 8KB PRG |
| `$C000-$DFFF` | fixed: PRG bank 30 |
| `$E000-$FFFF` | fixed: PRG bank 31 (vectors) |
| MMC3 regs | `$8000/$8001` bank select/data, `$A000` mirroring, `$A001` PRG-RAM protect, `$C000/$C001` IRQ latch/reload, `$E000/$E001` IRQ disable/enable |

PRG mode: reset writes `$8000=$00` → PRG mode 0, CHR mode 0 (2KB banks at `$0000`).

---

## Data-Range Map

16KB-granularity first pass (vector-area tails + entropy from `tools/nes_header.py`):

| Start (file) | End | Size | Classification | Notes |
|-------|-----|------|----------------|-------|
| `0x00000` | `0x0000F` | 16 | header | iNES |
| `0x00010` | `0x1000F` | 64KB | data (raw) | PRG banks 0-7: **screen pages**, 256 B each = 16×15 metatile ids + 16-byte tail (palette records in 2/3, spawn records in 4/5). Addressed by screen id (`ScreenPagePtr` `31:EF36`). Session 10 |
| `0x10010` | `0x1400F` | 16KB | data (raw) | banks 8/9: metatile quadrant pages (4×256 B per tileset at `$67&$FC`), bank 9 `$BE00` visited-substitution tables + attr ptrs `31:EEEF`. Session 10 |
| `0x14010` | `0x3C00F` | 160KB | mixed code/data | banks 10-29 (see SwitchBank16 map). Banks 21-23 = dialogue text (below) |
| `0x28010` | `0x2A00F` | 8KB | code + data | bank 20: message ptr arrays `$8000-$83FD`, group bank/ptr tables `$83FE-$8465`, text `$8466-$84FF`, text printer `$8500-$88FF`, string ptr tables `$8900-$8AEF`, dictionary + name strings `$8AF0-~$8F00`, other code `$9400+`. Session 11 |
| `0x2A010` | `0x2C00F` | 8KB | text | bank 21: messages `$A000-$BEFF` (groups 0-13), per-area table `$BF00` |
| `0x2C010` | `0x2E00F` | 8KB | text | bank 22: messages (groups 14-25) |
| `0x2E010` | `0x3000F` | 8KB | text + code | bank 23: messages `$A000-~$BBFF` (groups 26-33), save/load code `$BC00-$BDFF`, per-area save flags `$BF00` |
| `0x3C010` | `0x3E00F` | 8KB | code (fixed `$C000`) | bank 30: SwitchBank16 `C40E`, MainLoop `C900` |
| `0x3E010` | `0x4000F` | 8KB | code (fixed `$E000`) | bank 31: Reset/NMI/IRQ, vectors at `FFFA` |
| `0x40010` | `0x6000F` | 128KB | CHR ROM | 2bpp tiles, unclassified |

### Stale-copy filler (session 21, `tools/filler_scan.py` → `gfx/filler_scan.txt`)

The scan compares every pair of 8KB PRG banks at the **same in-bank offset** and lists runs of ≥32 identical bytes. Runs with fewer than 4 distinct byte values are classed as padding (18 runs, 952 bytes, mostly `$80` in screen banks 0-7 and `$FF`). That leaves 32 content runs. Each one was judged by hand, using the decoded-text coverage (`gfx/text.json`, which the tool prints as `text%A/B`), the known table extents, xrefs and the code boundaries. The tool's `refs` column (abs operands pointing into the run) is too noisy in the data and text banks to decide anything on its own.

**Result: 25 dead ranges, 4164 bytes** (session 21). **Session 22 correction: 24 ranges, 4102 bytes.** `18:9FC2-9FFF` turned out to be live code (see its row below) and was moved from `DEAD` to `LIVE_BOTH`. The ranges are listed in `filler_scan.DEAD`, which `disasm_all.py` imports and emits as `.db`:

| Dead range | Live source | Evidence |
|---|---|---|
| `20:8466-84FF`, `20:88A5-88FF`, `20:923D-93FF`, `20:9B4E-9BFF`, `20:9E30-9FFF` | bank 22 text (`22:A466`, `A8A5`, `B23D`, `BB4E`, `BE30`) | 100% covered by decoded messages on the bank 22 side; nothing in bank 20 points at them. The frame templates `20:8899-88A1` (9 bytes) stay live |
| `30:C8C0-C8FF` | `22:A8C0` text | lies between the `RTS` at `C8BF` and MainLoop `C900`, 0 xrefs. **Stale text inside fixed bank 30** |
| `31:F9BA-F9FF` | `23:B9BA` text ("is increased.", "Your magic power is too low.") | follows the `RTS` at `F9B9` |
| `31:FDE0-FDFF` | `23:BDE0` | load descriptors + `SaveAllowed` (known since session 11, now 32 bytes) |
| `31:FE2E-FE7F`, `23:BE2E-BE7F` | — | `$FF` padding + 8 stale bytes (`1E 00 28 00 3C 00 32 00`) before `ReadPads` `FE80`. Both sides are dead |
| `21:BF32-BFFF`, `27:BE40-BFFF` | `21:BE40` text + `23:BF30` | `23:BF00` is the 256-byte per-area save table. Bank 27 also holds a copy of the bank 21 message tails |
| `22:BF90-BFFF` | `18:9F90` | lies after the last bank 22 message. Bank 22's own tail is a stale copy of bank 18, and `20:9F90` copies the same bytes |
| ~~`18:9FC2-9FFF`~~ **LIVE (session 22)** | `19:ABA1` (different offset) | **Wrong in session 21.** Main state 2 (`30:C931`: `SwitchBank16(9)`, `JMP $9FCC`) enters at `18:9FCC` (`MainState2Init`) and runs on across the window edge into `19:A000`; the `STA $04` at `18:9FFF` is split between the banks. `xref.py` missed this because the `JMP` comes from fixed bank 30 into a swapped window. `9FC2-9FCB` (`Sub18_9FC2`, the same bytes as `19:ABA1`) still has no caller, but it sits in a live code run. Session 21 text: stale **code**: the same bytes as the live `DEC $06FE / BEQ / RTS / INC $06F1 / RTS / JSR …` at `19:ABA1`, with 0 xrefs. `18:9F05-9FC1` is a name-entry-style character grid (`A-Z`, `a-z`, `0-9 : ; ? ! " & ' , - . _`, `$FF` = empty cell). No reader has been found; the same characters appear as the displayed rectangle at `18:9E5A` (on a `$10` background) |
| `19:B895-B8FF` | `17:B895` | `17:B8D4` is a pointer table into bank 17 `W H` rectangles (`B8E6` = `10 08` + spaces). In bank 19 the same pointers would land inside `PlayerDeath` `19:B900` |
| `23:BBD3-BBFF` | `19:BBD3` | a copy of the code just before `SwordRaisePose` (the `19:BBE4` `OamClipTop` call; the session-19 note already called `23:BBE4` a stale copy) |
| `16:9FB0-9FFF` | `24:9FB0` | follows an `$FF` terminator in bank 16. The bytes are bank 24 sound-stream data |
| `13:ABA3-ABFF`, `13:BE91-BFEF` | bank 5 screen pages | these lie after the last area record (`~13:A9D7`) and the last ObjType record (`$FE` @`BE7D`, which ends at `BE90`). `13:BFF0` is the live area-0 record |
| `15:A3C0-A3FF` | `13:A3C0` | `13:A3C0` is live (pointer at `13:B93A`). `BossPhaseDispatch` starts at `15:A400` |
| `15:BBAF-BC3F`, `15:BF97-BFFF` | `13:BBAF`, `13:BF97` | copies of ObjType records (`$B8` @`BBBE`, `$3F` @`BBFD`), and of the bank 5 page plus the area-0 record |
| `09:BF80-BFFF` | `07:BF80` | the substitution tables `9:BE00`+k·32 are only used for k ≤ 11: every area's `$67` is ≤ `$AC` (`area_dump.py sub`; the `$A4/$A8/$AC` tilesets use `BF20-BF7F`). k ≥ 12 is never selected, and these bytes are bank 7's last screen page (rows 8-14 + tail) |
| `11:BEE0-BFFF`, `11:BCFA-BDFF` | `09:BEE0`, `09:BCFA` | bank 11 data ends with area `$5F`'s sub-table record (`11:BCDD-BCF9`) |

**Matches that are not filler:** `08:8835/8935/8A3F/8B3F` ↔ `09:A835/…` are the `$88` and `$A8` tilesets sharing quadrant-page rows; both are live. `09:BCFA-BDFF` is the live source of the bank 11 copy, but nothing known reads it either (`9:B300-BDFF` lies past the attribute tables `B000-B2FF` from `31:EEEF`). It stays as data and is unproven.

The pattern: a bank's unused tail keeps the bytes the build buffer held from another bank. That bank is usually close in number (20←22, 27←21, 15←13, 13←5, 11←9) or is the text bank mapped next to it (30←22, 31←23).

### Code coverage (session 22, `tools/disasm_all.py` → `disasm/coverage.txt`)

The recursive-descent tracer classifies **40,170 bytes as code (15.3% of PRG)**. 159,084 bytes are known data: banks 0-9, 12 and 21-22, the text in 23, the DEAD filler, jump tables, vectors, and labelled data runs (each counted from the label to the next label or code byte, so this is an upper bound). The remaining 62,890 bytes are unknown. All 32 banks round-trip byte-exact.

| Bank | Code % | Note |
|---|---|---|
| 30 / 31 | 77.2 / 72.4 | fixed banks; the unknown bytes are mostly the 8 untraced dispatch tables and their handlers (task at the end of RE Investigation) |
| 27 / 26 | 67.8 / 56.6 | object AI + player/HUD (`ObjAiTable` seeds all 128 entries) |
| 16 | 58.8 | menus. Entered with **16 at `$8000` and 23 at `$A000`** (`30:D8DD`: `SetR7($17)`, `JSR $8000`), which is why bank 16 calls `SaveJumpTable` `$BC00/$BC03` without mapping bank 23 itself |
| 15 | 41.3 | boss/AI code (context 26/15) |
| 19 / 18 | 29.0 / 11.2 | main states 2/3 (title/menu screens; see "Main states 2/3" below). Most of 18 is `W H` rectangle data |
| 24 | 21.5 | sound driver; the rest is sound data |
| 20 / 14 | 19.0 / 15.3 | text printer / NPC scripts; the rest is tables + script data |
| 28 | 10.5 | sprite pass; the rest is metasprite data |
| 23 | 5.5 | save module `BC00-BDFF` only |
| 17 | 4.0 | state 4/5 entries `$A000/$A100` + the `17:A117` dispatcher (untraced table `17:A11A`); mostly `W H` rectangles |
| 10, 11, 13, 25, 29 | 0 | no code reached. 10/11 = area headers + sub-tables, 13 = area records + `ObjTypeTable`. **25 and 29 are 100% unknown**: 25 = sound data used through R7=25 (`SoundUpdate`), 29 = metasprite data (`ObjMetaPtrTableB` → `$A000`). No label points into either yet |

---

## Key Findings

### Architecture

**Boot (`31:F2A4` Reset)**
1. SEI/CLD, stack=$FF, PPU off, wait 2 vblanks.
2. DMC IRQ off, APU frame IRQ off (`$4017=$40`), mirroring horizontal, WRAM enabled, MMC3 IRQ off, `$8000=0`.
3. `$51=3`; IRQ vector `$0054` ← `$F421` (default).
4. Toggles PPUADDR A12 16× (MMC3 IRQ counter clock sanity).
5. Fills `$6200-$621F` and `$6140-$615F` with `$FF`.
6. `SwitchBank16($0C)` → banks 24/25 at `$8000`; `JSR $8020` (init in bank 24). *(Corrected session 1: earlier note said n=6/bank 12 — the immediate at `31:F320` is `A9 0C`.)*
7. State `$40=2`, clears `$0700-$07FF`, PPUCTRL=`$88` (NMI on, sprites @ `$1000`), `$07FE=9`, CLI.
8. Warm-boot check: if `$0182-$0184` == `"USO"` skip to `F39F`; else cold init (`F1CD`, write signature, `FE80`, `$0F` = (`$43`==`$C0`), `SwitchBank16(9)` → `JSR $8028` in bank 18).
9. `F0A4`, then copy WRAM `$7D00-$7EFF` → `$6480-$657F`, `JMP $C900`.

**Bank switching (`30:C40E` SwitchBank16)** — A = 16KB bank index *n*; maps 8KB banks `2n+1` (via `C427`, $A000 window) and `2n` (R6, $8000). `$6E` = current 2n, `$50` = select-register shadow. So the game treats PRG as 16KB pairs at `$8000-$BFFF` with `$C000-$FFFF` fixed. Bank-switched entry points are at fixed offsets (`$8020`, `$8028` …) → likely a jump table at the start of each swappable bank.

**Bank-switch primitives (verified session 1)** — three entry points share one body:

| Addr | Name | In | Effect |
|------|------|----|--------|
| `30:C40E` | SwitchBank16 | A=n | `ASL`; `$6E`=2n; `SetR7(2n|1)`; then reloads `$6E` and falls into the R6 write (`C416` `LDA $6E`/`C418` `STA $6E` — the redundant pair is the seam) |
| `30:C418` | SetR6 | A=8KB bank | `$6E`=A; `$50`=6; `$8000`←6, `$8001`←A |
| `30:C427` | SetR7 | A=8KB bank | `$6F`=A; `$50`=7; `$8000`←7, `$8001`←A |

`$6E`/`$6F` are the R6/R7 shadows (8KB bank numbers). The game sometimes swaps one 8KB window alone: `SetR6($1A)` → bank 26 at `$8000` (`31:FFB1`, `FFC4`); `SetR7($17)` → bank 23 at `$A000` (`30:C9DF`); bank 26 itself calls `SetR7` 5× (`26:8C2F/8C93/9737/976E/97D1`) and bank 27 once (`27:A90E`) — so banks 26/27 swap in their own data partners. 27 SetR7 xrefs total, 6 from swappable code.

**Far-call trampolines (`31:FF80-FFE2`)** — each pushes `$6E`,`$6F`, maps a bank, calls, then `JMP RestoreBanks` (`31:FFDB`: `PLA→SetR7`, `PLA→JMP SetR6`). Lets fixed-bank code be called from swappable code without losing the caller's bank:
`FF80`→`$C25D` · `FF8C`→SwitchBank16(14) `28:$800E` · `FF9D`→`$EB91` · `FFA9`→SetR6(26) `26:$8E46` (A passed via `$10`) · `FFBE`→SetR6(26) `26:$8CC0` · `FFCF`→`$D354`.

**SwitchBank16 call-site map** (48 xrefs, all in fixed banks 30/31; 42 are `LDA #n / JSR`, plus `JMP` tail-calls at `C7A2`(n=13), `C82E`(n=8), `DA6F`, `E3D6`, and non-immediate `CAD8`, `EF43`). Tool: `search_bytes.py "A9 ?? 20 0E C4" --disasm --context 14`.

| n | 8KB banks | Call sites | Entry points called | Subsystem (tentative) |
|---|-----------|-----------|---------------------|-----------------------|
| 0 | 0/1 | `CE39`, `CEB4` | (data only — reads via ptrs `$20-$22` from table at `30:D047`) | data tables (map/metatile?) |
| 2 | 4/5 | `E2B5` | inline, uses `$2F` | data |
| 4 | 8/9 | `EC14`, `ED20` | inline, uses `$36`/`$34` bit 3 | data (two similar renderers) |
| 5 | 10/11 | `C7A6`, `E628` | ptr table at `$8300` indexed by `$6C` (`C7A6`); `JSR C7A5` (`E628`) | pointer-indexed data (screens/text?) |
| 7 | 14/15 | `CBA3`, `D105`, `D83D`, `DE0F`, `E346`, `E384` | `$BC40`, `$8000`, `$816F`, `$80A0`, `$85BF`; `D83D` dispatches via table at `30:D885` | object/entity logic (uses `$23` = slot, `$2F`) |
| 8 | 16/17 | `C7FA`, `C944`, `C94C`, `C82E`(JMP) | `JMP $A000` (state 4), `JMP $A100` (state 5) | main states 4/5 |
| 9 | 18/19 | `C931`, `C939`, `C9EB`, `CA5F`, `CC78`, `D5AF`, `D974`, `F397` | `JMP $9FCC` (state 2), `JMP $A015` (state 3, after `JSR $FEE0`), `$BA65` (×4), `$BC04`, `$8028` (cold boot) | main states 2/3 + common screen setup `$BA65` |
| 10 | 20/21 | `D9B6` | `$9400` | single subsystem (TBD) |
| 12 | 24/25 | `F320` (Reset) | `$8020` SndInit | **sound driver** (confirmed session 2; also mapped directly by `SoundUpdate` `31:F7FE`) |
| 13 | 26/27 | `C7A0`(JMP), `C7E2`, `C80C`, `CB79`, `CCD1`, `CD82`, `DA3C`, `DAC2`, `E43C`, `E461`, `E47F`, `E547`, `E5E8` | `$8C3D`/`$8C49` (fade, A=3/4), `$8F6E`, `$9B45` (X=1), `$9824`, `$9897`/`$98A8` (X=0/1), `$9CEF` | most-used: palette/fade + per-player (X=0/1) HUD/screen routines |
| 14 | 28/29 | `C1B4`, `CB71`, `E4CD`, `FF92` | `$800E` (all four) | **object→OAM sprite pass** (not sound — session 2): `$07FF` decremented each call rotates start slot (flicker); `$04A0,X` active flag, `$70,X` pos − `$02` scroll |

Remaining sites (checked):
- `30:DA6D` `LDA #8 / JMP` → n=8 tail-call.
- `31:E3D4` `LDA #6 / JMP` → **n=6, banks 12/13**; called right after the n=7 entity calls (`E34E`, `E38E…`) → banks 12/13 are the "home" pair that entity code returns to (likely main gameplay code).
- `31:EF43` — n = top 2 bits of a pointer hi byte (`ROL×3 / AND #3`), low 6 bits `|$80` → `$11` → **pointer-encoded bank**: n 0-3 (8KB banks 0-7) + `$8000-$BFFF` address. Explains banks 2/3, 6/7 (n=1/3) having no immediate: they're reached only through these packed pointers (data, likely graphics/map streams).
- `30:CAD8` — state-1 (`CAB6`) substate dispatcher: `$41` → handler `SubstateHandlerTable` `30:CADE` (→`$10/$11`) and n from `SubstateBankTable` `30:CB2E` (substate 3→n=9, 12/13→n=7, all others n=0), then `JMP ($0010)`.

Never mapped by any found path: banks 22/23 as a pair (n=11), 30/31 (fixed). Banks 21/22/23 are mapped one at a time at `$A000` by the text printer (`20:885A`, bank from `TextGroupBank` `20:83FE`). Bank 23 is also mapped by `SetR7($17)` at 7 sites for the save module (session 11, see "Save / load module").

**`30:C7D6` FadeSaveZpToWram** — copies ZP `$0A-$29` → `$6000`, `SwitchBank16(13)`, `26:$8C3D(A=4)`, waits 30 frames on `$09`, `26:$8C49(A=4)`, `SwitchBank16(8)`, restores `$6000`→`$0A`. `$8C3D` sets `$11=0,$19=$10`; `$8C49` sets `$11=$40,$19=$F0` → opposite step directions (fade out/in, tentative).

**Sound driver (bank 24/25, session 2)** — `search_bytes.py "8D ?? 40"`: the only swappable-bank `STA $40xx` writes are in bank 24 (19 hits `24:8042-8EBB`); bank 28 has none. Layers:

| Addr | Name | Role |
|------|------|------|
| `30:C125` | PlaySound | **Game API**, 85 callers (banks 14/15/19/23/26/27/30/31). A=id. Y saved/returned via `$010F`. Ids `<$20` = music: skipped if == `$0102`, else `$0102`=id. `$80-$9F` → `AND #$7F` (forced re-issue). `≥$A0` / `$20-$7F` → SFX. Sets `$0101` lock, appends to FIFO `$0103+$0100`; when 5 queued, SFX overwrite slot 5, music is dropped. (`C120` is a 2nd entry used by bank 6 data? — xrefs `06:80FE/84FE` are likely false positives.) |
| `31:F7FE` | SoundUpdate | Called from NMI (`F4CF`, wrapped by `$06`=1) and IRQ handler 5 (`F611`), plus JMP tails `F6DC/F76B/F77F` (other IRQ handlers). Writes MMC3 R6=24, R7=25 directly (no shadow update). If `$0102` bit7 and music idle (`$0111=$FF`, `$0168-$016A`=0?) re-issues music. Else if not locked and queue non-empty: `$0110`←`$0103`, `JSR SndCommand`, shift FIFO, `DEC $0100`. Then R7=25, `JSR SndTick`, restore R6/R7 from `$6E/$6F` and `$8000` from `$50`. → **one sound command per frame**. |
| `24:8020` | SndInit | silence: `$F0`=0, `$0111/$0112`=`$FF`, `$0114-$0119`=0, `$FB`=0, `$4015`=0. Called once from Reset (`31:F325`). |
| `24:8048` | SndCommand | sets `$F0` bit0 (busy), dispatches `$0110`: `$FF` → `$FE=$BC` (none); bit7 → pause toggle (`$F0` bit5, `$4015`←`$FB`, re-init 4 channels via `$8465/$8388`); `<$20` → SndStartMusic `$80E4`; `≥$20` → SndStartSfx `$8094`. |
| `24:8094` | SndStartSfx | R7←bank **12** (`$0C`); header ptr = `$9FC0+id*2` → for id≥`$20` this lands in `12:$A000+(id−$20)*2` (SFX pointer table lives at start of bank 12). Byte 0 = priority: rejected if > `$0112`. Sets `$FB=$0A` (pulse2+noise?), inits channels 4 and 3 via `$8169`, flags `$0115/$0117` bit7. Ignored if `$F0` bit6. |
| `24:80E4` | SndStartMusic | `$8177` check, `$0111`=id&`$7F`, clears `$FE/$FF`, inits 4 channel streams `$812A` (Y=1,3,5,7 → stream ptr pairs). |
| `24:8186` | SndTick | per-frame playback; exits if `$F0` bit0 set; stops music on `$F0` bit7 (sets `$0111`=`$FF`, `$4015`←`$FB`). |
| `24:8000` | SndOpcodeTable | **Corrected session 3:** this IS a jump table — the stream-byte opcode dispatch (`24:8347` SndReadOp: `op = byte>>4`, `JMP (SndOpcodeTable+op*2)`). See "Sound data format" below. |

Sound RAM: `$0100-$0112` (queue + state), `$0114-$0119` channel flags, `$F0` flags, `$F3`, `$F5/$F6` data ptr, `$FB` `$4015` shadow, `$FD-$FF`. **Port note:** sound can be implemented as a FIFO of ids consumed one per frame; music ids `$00-$1F`, SFX `$20-$9F` (data in bank 12 `$A000`), bank 25 holds tick-time data.

**Sound data format (session 3)** — tables decoded with `tools/decode_tables.py`.

*Channels* (X index, arrays 6 wide at `$0114+X`…): 0=SQ1, 1=SQ2, 2=TRI, 3=NOISE (music); 4=SFX on SQ2, 5=SFX on NOISE. `SndChnTypeTable` `24:87A6` = `00 01 02 03 01 03`, reg offset `87AC` = `0 4 8 C 4 C`, `$4015` mask `87B2`. While an SFX plays, music chn 1/3 get `$0114` bit7 (muted, stream keeps running).

*Headers*
- Music: `SndMusicTable` `24:8C0C` ptr16[32], ids `$00-$1F`; null (ignored) ids `$02 $05 $10`. Header = 1 byte (unused by code, always `01` so far) + 4 stream ptr16 (SQ1, SQ2, TRI, NOISE). Id `$00` (`$8C4C`) = silence: each stream `53 40 00`.
- SFX: `SfxPtrTable` = file bank 12 offset 0, seen at `$A000` via `SetR7(12)` (`SndStartSfx` indexes `$9FC0+id*2`, so id `$20` → entry 0). 88 entries → ids `$20-$77`; null ids (all 88 checked, session 4): `$22 $2D $34 $35 $38 $44 $46 $4D $4F $59 $70 $71 $73 $77`. First header at `$A0B0` = end of the 88-entry table, confirming the count. Header = priority byte (lower = more important; rejected if > `$0112`, `$FF` = idle) + 2 stream ptr16 (SQ2, NOISE). Stream ptrs are stored as `$8xxx` and `+$20` hi (`SndInitSfxChannel` `24:8169`) — data assembled for bank 12's native window.

*Per-channel RAM* (`+X`): `$0114` flags (b0 active, b4 rest/hold, b5 vibrato, b6 envelope, b7 stolen-by-SFX), `$011A/$0120` stream ptr, `$0126/$0132/$013E` loop1 ptr/count, `$012C/$0138/$0144` loop2 ptr/count, `$014A` frames left, `$0150` note length, `$0156` octave×16, `$015C` envelope#<<4 | step, `$0162` vibrato depth, `$0168` `$4000` shadow (duty/vol), `$016E` sweep, `$0174/$017A` period lo/hi. `$0113` = next music id (chain). `$FC` = per-frame reg-dirty bits (b0 vol, b1 sweep, b2 lo, b3 hi). `$FE` = fade timer (`$BC` on cmd `$FF`; decrements volumes via `24:8609`). `$FF` = DMC-pending/`$4015` DMC bit.

*Stream bytes* — `SndReadOp` `24:8347`: opcode = high nibble → `SndOpcodeTable` `24:8000`. Ops are read in a loop until one "ends the frame" (sets `$F2`), then the channel waits `$0150` frames.

| Byte | Handler | Meaning |
|------|---------|---------|
| `0x`/`1x` | `8364` | end of stream. Music: `$0113` = byte&`$1F` → when all 4 channels end, music id `$0113` restarts (≠0 = loop/chain, 0 = stop). SFX: channel released. |
| `2x` | `83E5` | **note** x, ends frame |
| `3x` | `8422` | octave: `$0156` = (x−1)·`$10` (x=1..8) |
| `4x` | `8436` | rest/hold, ends frame; x=0 → channel silenced, else const volume x |
| `50-6F` | `847D` | note length = `SndDurationTable[byte−$50]` (`24:8AB8`: `05 40 38 01 14 02 28 03 50 04 1E 1C 07 2A 13 15 06 08 0C 09 18 0A 30 0B 60 12 24 0D 20 0E 10 0F` frames) |
| `7x` | `848A` | legato pitch change (no retrigger unless hi period changes); x=7 = tie; ends frame |
| `8x` | `8532` | base volume x (skipped during fade) |
| `9x` | `854D` | duty / const-volume bits of `$4000` shadow |
| `A0 lo hi` | `8584` | jump to ptr (also stores return in loop2 slot) |
| `A1 n` / `A2 n` | `8584` | loop1 / loop2 start, count n |
| `A4` / `A3`,`A5+` | `8584` | loop1 end / loop2 end (dec count, jump back if ≠0) |
| `Bx yy` | `83E2` | sweep on (yy) + note x |
| `Cx yy` | `8395` | sweep reg = yy (x bit0: 1=off, 0=on), ends frame |
| `Dx` | `83C2` | note x with octave-dependent detune `$83DA` (`0C 06 04 03 02 02 01 01`); on NOISE chn → DMC drum #x (`SndDmcTable` `24:8BD8`, 4-byte `$4010-$4013` records) |
| `Ex` | `85F7` | envelope #x (`$015C`=x<<4, enables `$0114` b6) |
| `Fx` | `8685` | vibrato depth x·8; x<8 on, x=8 off, x>8 = empty frame |

*Op `9x` exact (`SndOpDuty` `24:854D`, session 4)* — writes the high nibble of the `$0168` reg-0 shadow (low nibble = volume kept), sets `$FC` b0:
- **TRI** (type 2): high nibble = x verbatim → `$4008` bit7 (control/halt) + linear-reload bits 4-6 (`$0168 = x<<4 | vol`).
- **SQ/NOISE**: x bit0 → const-volume bit4; x bits1-2 → duty bits6-7; bit5 (length halt) always set. So `$4000` high nibble = `(x&6)<<5 | (x&1)<<4 | $20`. If x bit0 = 0 (hardware envelope), the software-envelope flag `$0114` b6 is cleared.

*Vibrato tick (`SndVibratoTick` `24:86AA`, session 4)* — called per frame from `24:82F0` when `$0114` b5. Skipped if b7 (SFX-stolen); if software envelope active (b6), waits until env step (`$015C`&`$0F`) = `$F`. `INC $FD`. `$0162` = depth x<<3 | phase(0-7); phase = (phase+1)&7; p = x odd ? phase>>1 : phase, then p&=3 (x odd = half speed). `e = SndVibStepTable[(x>>1)*4 + p]` (`24:8714`: `00 02 00 04 / 00 06 00 08 / 00 0A 00 0C / 00 0E 00 10`); e=0 → offset 0, else **Y = octave×16 + e + 2** and offset = s16 at `SndVibOffsetTable` `24:8724+Y`. Period lo written to `$4002+reg` = (`$0174` + offset) **only if** the hi byte would be unchanged (no hi write, base `$0174/$017A` untouched — non-cumulative).
- Offset table is 8 s16 per octave row starting at `$8726+oct*16`: oct0 `+4 −4 +8 −8 +12 −12 +16 −16`, oct1 `+2 −2 +4 −4 +6 −6 +8 −8`, oct2 `+1 −1 +2 −2 +3 −3 +4 −4`, oct3 `+1 −1 +1 −1 +2 −2 +2 −2`, oct4-7 `+1 −1` repeated.
- **Original off-by-one** (replicate in port): because of `+2` instead of `+0`, phase1 reads the *negative* of pair k and phase3 reads the *positive* of pair k+1 — e.g. depth 0/1, octave 0: cycle `0, −4, 0, +8`. Depth 6/7 phase3 reads the next octave row's first word; at octave 7 that is `$87A6` = `SndChnTypeTable` bytes `00 01` → **+256** (harmless only if unused).

*Pitch*: `SndPeriodLo/Hi` `24:87B8`/`24:8838`, 128 entries indexed `octave*16 + x`. **The note nibble is the note's hex letter**: `C`=C, `D`=D, `E`=E, `F`=F, `A`=A, `B`=B, plus `0`=G, `1`=C♯, `2`=D♯, `4`=F♯, `5`=G♯, `6`=A♯ (`3 7 8 9` = 0, unused). Row 0: C=`$6AC` (65.4 Hz, C2) … B=`$388`; each row = +1 octave. NOISE chn uses x directly as noise period.

*Envelopes*: `$84AF` steps `$015C` through `SndEnvTableTonal` `24:88B8` (noise: `24:89B8`), 16 envelopes × 16 steps; value low nibble = volume, bits6-7 = duty override, bit4 = hold last step (or back up 2 during fade).

*Ids used by PlaySound callers* (`search_bytes.py "A9 ?? 20 25 C1" --disasm`, 67 immediate sites):
- Music: `$00` (19:B91C, silence), `$0C` (15:B310), `$12` ×8 (bank 15: A4B7 A68F A99A ACB0 AE0E AF82 B186), `$80` (15:BAAE → forced `$00` = stop), `$93` ×3 (19:BC38, 30:D0A6, 30:D42D → forced `$13`).
- SFX: `$21 $23×2 $24 $25×4 $26 $29×7 $2F $31×3 $36×2 $39×3 $3A $3D×2 $3E $3F×2 $43 $44 $45 $48 $4B×2 $4E×2 $54 $56 $5C $5D $5F $64 $66 $68 $6B×2 $6E $6F $76` (`$29` is the most common — 15:BAF3, 19:BFDA, 27:BB32, 30:D0AE/D0C8/D1CA/D48F). Note `$44` is a null SFX entry (26:92D6 → silently ignored).
- The other ~18 PlaySound callers load A from RAM/tables (not enumerated).

*Music ids by caller (session 4)* — headers: `$0C` `24:9F4B`, `$12` `25:A5DB`, `$13` `25:A796` (all byte0 `01`).
- **`$13` = interrupting jingle** ("fanfare"). Always issued as `$93` right after the caller sets `snd_cur_music` (`$0102`) bit7, so when `$13` ends (every stream ends `00` = stop, ~150 frames ≈ 2.5 s; SQ1: `93 8F E9 60 34 2D 5C 2E 2F 54 20 65 2A 50 41 20 54 2A 56 77 53 40 00`) `SoundUpdate` re-issues the previous track. Played *instead of* an SFX on a special case: `30:D090` if `$06C3=$0D && $06E3=$20` (else SFX `$29`); `30:D41C` if `$07DC&$F0=$40` (else SFX `$3F`); also `19:BC38`. Likely an item/achievement jingle — confirm when `$06C3/$06E3/$07DC` are named.
- **`$12` = entity-triggered track.** 8 sites in bank 15 (n=7, entity logic), each an entity handler doing `STA $05A0` (value `$00`/`$10`) then, only if `$07D5≠0`, `PlaySound $12`. Uses loops (`A1`/`A4`) → a full looping piece; likely boss/encounter music (one per boss-type entity).
- **`$0C`** — single site `15:B310`, entity handler that first far-calls bank 14 `$800E`, sets `$07E8=$38`, calls `15:8C3D` (A=`$10`); a scene/event theme. Semantics TBD with entity tracing.

**Port note:** this is enough to write a JS sequencer: per channel, read ops until a frame-ending op, count down `SndDurationTable` frames, apply envelope table steps per frame, map note nibble via period table to APU square/triangle/noise.

**Main loop (`30:C900`)** — spins on `$09` (set by NMI), then dispatches `MainStateTable[$40]` (`30:C91F`) through the ZP trampoline `$10: JMP abs`, clears `$09`, repeats. States: 0=`C957` 1=`CAB6` 2=`C931` (initial) 3=`C939` 4=`C944` 5=`C94C`.

**NMI (`31:F3B6`)** — returns immediately if `$60≠0`, or if `$06≠0` (lag: sets `$07`). Else OAM DMA from `$0200`, copies `$02-$05` → `$07D8-$07DB`, special handling for `$51` = 7/9 (to trace).

**IRQ (`31:F443`)** — ack/re-enable MMC3 IRQ, `JMP ($0054)`. Handler table `IrqHandlerTable` at `31:F433` (8 entries) selected by `SetIrqHandler` (`31:F424`, A=index). Handler 0 (`F455`) does a delayed mid-frame scroll split from `$07D8`.

**Object sprite pass (`28:800E` ObjSpriteDraw, session 5)** — reached via `SwitchBank16(14)` from `C1B4/CB71/E4CD` and far-call `FF8C`.
1. `$07FF` (obj_draw_rot) `DEC`; order = `ObjDrawOrderTable` `28:83DC + ObjDrawOrderOfs[$07FF&3]` — **4 fixed 32-slot permutations** (not a simple rotation): `04 05 06 07 02 01 03 08…1F 00`, a shuffled one, another, and `02 01 1F 1E … 03 00`. Slot 0 is drawn last in 3 of 4; slots 1/2 near the front.
2. OAM write ptr `$10` starts at 4 (sprite 0 reserved for split). For each slot X with `$04A0,X≠0`: sx = (`$70`,`$90`) − (`$02`,`$03`), hi must be 0. Store `$05C0`. sy = (`$B0`,`$D0`) − (`$04`,`$05`) with 240-row wrap (extra −16 on borrow), hi must be 0; sy ≥ `$E8` → skipped silently; sy < `$10` or off-page → `$0380` b7 set (offscreen). Else `$05E0`=sy, b7 cleared, draw unless OAM full or (b2 half-rate and (X+`$07FF`) odd).
3. `ObjHideUnusedOam` `80A5`: Y=`$F0` from `$10` to end. If `$0710` b7: clears palette bits of obj 1's 6 sprites (4 if `$0381` b6) from `$07EF` → **hit flash for obj 1 = player**.
4. `OrbitSprites` `80D6`: 8 sprites in OAM `$E0-$FC` (attr 1) centred on obj 1. `$06C1≠0`: burst — timer decrements, drawn while <`$0E`, tile `$29`, rays from `BurstDirTable` `81DE` × `BurstRadiusTable` `81FE`. Else if `$06C0≥8`: orbit — 8 points on `OrbitSineTable` `81BA` (32 s8, radius ≈`$21`) phase `$08`+k·8, tile `OrbitTileTable[$06C0>>3]` (`29 29 2A 32 20`). Points that wrap horizontally are skipped.

**Metasprite drawing (`ObjDrawMeta` `822E`)**
- Y origin `$13` = sy (+3 if `$0380` b6). If `$0380` b1: first draws overlay id `$0580,X` (temporarily `$0300`←`$0580`, b0 set, b5 clear), then the main sprite.
- id = `$0300,X` + (`$0380` b0 ? 0 : `$0360,X`). id `<$80` → `ObjMetaPtrTableA` `28:845C`, else `ObjMetaPtrTableB` `28:855C` (ptr16, targets up to `$BFxx` → bank 29). `X==1` records `$07EF`=OAM offset.
- tile add `$1A` = `$0320,X` + (`$0380` b5 ? `$40`:0); attr OR = `$0380` b4 → bit5 (behind BG).

*Metasprite record format* (all in banks 28/29):
```
FF lo hi            redirect: draw record at hi:lo HORIZONTALLY MIRRORED (x' = -dx-8, attr ^= $40)
00                  nothing drawn
N  M  {frames}      N = sprites per frame (1..), M = frame mask
                    frame f = ($04E0,X >> 3) & M  (anim advances every 8 ticks)
                    frame data at +2 + f*N*4; each sprite = dx(s8) dy(s8) attr tile
                    dx = $80 → end of frame early (frames padded with 80 80 80 80)
                    attr bit2 (unused by PPU) = "lower part": if $0380 b6, drawing STOPS at it
```
Example id 2 (`$865C`): `06 01` → 6 sprites, 2 frames; 16×24 body: `F8 E9 00 1A / 00 E9 00 1B / F8 F1 00 00 / 00 F1 00 01 / F8 F9 04 20 / 00 F9 04 21` (bottom row flagged b2 → hidden when sinking); frame 1 is the same body with attr `$40` (self-mirrored) rows. Id 0 (`$868E`) uses tiles `1E 1F 06 07 26 27`. Id 3 (`$86F2`) = `FF C0 86` → mirrored id 1 — facing variants are `FF` aliases.

Object RAM (32 slots, index X 0-31): `$70/$90` X lo/hi, `$B0/$D0` Y lo/hi, `$0300` meta id, `$0320` tile ofs, `$0340` b7 (palette-cycle, read but **unused** — `$1D` is computed and never read: dead code), `$0360` dir/anim id add, `$0380` flags (b0 no-dir, b1 overlay, b2 half-rate flicker, b4 behind BG, b5 tile+$40, b6 sink/half-hide, b7 offscreen out), `$04A0` active, `$04E0` anim counter, `$0580` overlay id, `$05C0/$05E0` screen X/Y out. **Obj 1 = player** (hit flash, orbit centre).

**Sprite CHR banks (session 6)** — PPUCTRL `$88` → 8×8 sprites from `$1000` = MMC3 R2-R5 (1KB each; tile t uses R[2 + t>>6]). Two shadow sets: `$07F0-$07F5` (play field, `ApplyChrPlay` `31:F883`, called from IRQ-side code `F779/F785/F7C8`) and `$58-$5D` (`ApplyChrAlt` `31:F64F`, inside an IRQ handler). In-game writers:
- R2 `$07F2` = `$40`, or `$41` if `$0713`≠0 (`30:C0B8`) → **player tiles**. Other scenes: `$64`/`$4D` (`30:C100/C107`), `$73` (`30:DCDF`), `$5C` (`30:DD7F`, also `$5A`), plus 9 sites in bank 19 and `17:A935`.
- R3 `$07F3` = `$41` + clamp(`$0711`, 1..5) = `$42-$46` (`30:C0C3`; clamped value also → `$07E5`); `$42` at `30:C114`.
- R4/R5 `$07F4/$07F5` = per-area: `LoadAreaChrRecord` `31:E16D` reads a record via `($10)` (ptr from `$9300,Y` table): byte0 must be 0, then `$07E6`, `$07E7`, R4, R5, … Also `$FF/$51` at `31:E832`, `$4D` at `30:DF44`, and bank 15 entity handlers (8 sites, same handlers as the music `$12` sites → boss CHR swap).

**`tools/meta_dump.py` (session 6)** — `text [ids]` prints decoded records (FF-mirror resolved); `png [out] [--chr R2,R3,R4,R5] [--cell N] [--frames N]` renders all 256 ids in a 16×16 grid (frames side by side, attr palette tinted, origin pixel marked) → `gfx/metasprites.png`. Default `--chr 40,42,50,51`. Verified: ids `00-07` (player: 4 facings + 4 sword/attack poses) render clean with R2=`$40`; ids using tiles ≥`$80` need the area's R4/R5 (the `50,51` default is a placeholder, so those look scrambled). Only 4 of the 256 ids are empty. Next: decode the `$9300` area table so each id range can be rendered with its real R4/R5.

**Area records (session 7)** — `AreaLoad` `31:E14A` (entries `AreaLoadA` `E144` sets `$18`=1, `AreaLoadB` `E148` sets `$18`=0; callers `30:D1C0/D1DC/D6EC` and JMP tails `30:D199/D21A/31:E6FF`; `$18` not read in this routine). `SwitchBank16(6)` → banks 12/13, then ptr = `AreaPtrTable` `12:9201`[`$6C`] (one ptr16 table — the `$9301` load is the same table's upper half, chosen by the carry from `ASL $6C`). **`$6C` = current area id**; table has **252 entries** (ends `$93F9`, where area `$3C`'s record starts). Ptr hi bit7 clear → no record (`$0000` nulls, e.g. areas 11, 13, 18, 19). `LoadAreaChrRecord` `E16D` is the record body:
```
00                       must be 0 (else whole record ignored)
e6 e7                    -> $07E6, $07E7 = sprite sub-palette 2/3 ids (session 8; flag-item handler overrides both with ($6C&7)+9)
R4 R5                    -> $07F4/$07F5 sprite CHR (1KB banks for tiles $80-$FF); $FF = CHR bank 127, not "keep" (session 8)
{b0 b1 b2 b3}*           one 4-byte entry per object slot, X starting at $0D (slots $0D-$1F)
FF                       end
```
- `FE` as b0 → slot skipped (4 bytes). b1 bit7 → slot skipped (`E19D` ASL/BCC).
- **Position:** Y = b0·16 + 12 (`$D0`=b0>>4, `$B0`=b0<<4|`$0C`); X = (b1&`$7F`)·16 + 8 + (b2 b6 ? 8 : 0) (`$90`=(b1>>4)&7). 16-px cell grid, up to 8 screens wide/16 high.
- **b2**: bits0-2 = handler index (`SpawnHandlerTable` `31:E218`, 5 valid entries — entry 4 = `$E222`, the code right after the table; bytes beyond are code, so values 5-7 are invalid); bits0-1 also → `$0540,X`; b6 = x+8; b7 → `$0380` b5 (tile+`$40`, via `E3C7`).
- **b3** = param `$2F` → `$0560,X`.
- Every handler calls `ObjInit` `30:C25D` (slot `$10`=X, type `$11`; type 1 = no-op) then common tail `SpawnCommonTail` `E28E`.

| h | Addr | Spawn |
|---|------|-------|
| 0 | `E27F` | type = p+`$50` |
| 1 | `E2B5` | indirect: `SwitchBank16(2)` (banks 4/5), 4-byte rec at `$8000 + (p>>2)·$100 + $F0 + (p&3)·4` → `$20-$23`. If `$20`=`$FF`: type=`$21`. Else type `$30`, `$0680`=`$20`, `$06A0`=`$21`, `$22` b7 → `$04A0`/`$06E0`=`$22&$7F`, else `$06E0`=`$22`, `$0320`=`$22&$70`; `$22` b3 → y\|=`$0E`; `$0300`=`$06C0`=`$23`; `$0600` from `E35F[p&3]` unless `$06E0` b2. Then bank 14 `$80A0` with `$23`=p |
| 2 | `E375` | p bit7 → type `$0E`; else type `$0D` + bank 14 `$85BF` (`$23`=p); p ∈ {`$25 $3B $3C`} → meta id `$4C` |
| 3 | `E2A3` | type = p+`$D0` |
| 4 | `E222` | type = p; if p&`$F0`=`$40` (**flag item**): `$07E6/$07E7`=(`$6C`&7)+9; in areas `$C2-$C4` (flag `$84`) / `$F5-$F7` (flag `$27`) if WRAM bit `$6480[f>>3] & BitMask[f&7]` set → p=`$4E` (already-collected replacement). `BitMask` = `30:C000` (`01 02 … 80`) |

**Palette ids `$07E0-$07E7` (session 8)** — `$07E6/$07E7` are **sprite sub-palettes 2 and 3** (the enemy palettes). `$07E0-$07E7` = 8 palette ids (BG 0-3, sprite 0-3). `PaletteBuild` `26:8C0E` (sets fade `$11`=0, i.e. full brightness; 18 callers in banks 26/27/30/31; `8C10` is an A=fade-step entry with no direct xrefs) loops X=0..$1F step 4, calls `PaletteLoadOne` `26:8C6E` with A=`$07E0[X>>2]`:
- id p (+`$B0` if a sprite palette, X≥`$10`) → `SetR7(2 + (p>>7))` (banks 2/3 at `$A000`), source = `$A000 + ((p>>2)&$1F)·$100 + $F0 + (p&3)·4`: **4-byte palette records live in the last 16 bytes of every 256-byte page of banks 2/3** (**256 slots**, not 512: slot s = bank 2+(s>>7), page (s>>2)&$1F, record s&3; same page-tail layout as the bank 4/5 spawn records of handler 1). The sprite offset is `CLC/ADC #$B0` (8-bit), so sprite id p → slot (p+`$B0`)&`$FF`, and sprite ids `$50-$FF` alias BG slots `$00-$4F` (session 23).
- **Palette dump (session 23, `tools/palette_dump.py` → `gfx/palettes.png/.json`):** all 256 slots decode cleanly. Every non-`$FF` byte is ≤ `$3F` (no out-of-range values). There are 144 `$FF` (backdrop) bytes, and 17 slots are entirely `$FF` (`9F A5-AD CB F4-F7 FB FC`). Sprite slots `$B0+` mostly start with `$FF`: colour 0 of a sprite palette is transparent, and the byte just copies the backdrop. Areas reference 178 slots: 131 as BG ids (k1 `p0-p2`, plus the constant BG3 id `$7F`) and 48 as sprite ids (`$07E6/$07E7` → slot +`$B0`). Sprite palettes 0/1 (`$07E4/$07E5`, player/common) are not set per area and not traced here.
- **Upload path (session 23):** NMI `31:F40D` `LDX #0` → `JSR PaletteUpload` `31:F8CB`. If `irq_mode` `$51` bit 7 is set it returns early (no upload). Otherwise it does: `PPUMASK`=0; `PPUCTRL` = shadow with b2 cleared (+1 increment); `PPUADDR`=`$3F00`; 32 unrolled `$2007` writes from `pal_buf,X`, where the first byte of **every** 4-colour group is `$6140` (the backdrop), so `$3F04/08/0C/10/14/18/1C` always mirror colour 0 and bytes `$6144/48/…/5C` are never uploaded. It ends with `PPUADDR` `$3F00` then `$0000` (moves v off the palette so the display doesn't pick up a palette colour), and the NMI then restores `PPUMASK` from `$01`. This is the only `$3F00` writer in the ROM: `search_bytes "A9 3F 8D 06 20"` finds just the 2 hits inside this routine. Other screens (banks 16-20, `30:C87B/CE6D`, `31:F31A`) write `pal_buf` directly and rely on this upload.
- each colour: `$FF` → copy of `$6140` (backdrop); then colour − `$11` (fade, steps of `$10`), negative → `$0F` black; written to **palette buffer `$6140-$615F`** (WRAM).
- `$07E8` = per-palette "ignore fade" mask, bit i = palette i (shifted out LSB first; only 8 bits, one per palette). Also tested via `BIT` at `30:C0E5`, `31:E3FE/E4E5/E4F4`.
- So the flag-item handler's `($6C&7)+9` → sprite ids `$B9-$C0`.
- `$6040-$605F` filled with `$0F` by `26:8C32` (a second palette buffer — target/black).

**R4/R5 = `$FF` (session 8)** — `LoadAreaChrRecord` stores it unchanged and `ApplyChrPlay` `31:F883` writes `$07F4/$07F5` to MMC3 R4/R5 with no check. So `$FF` is **not** a "keep" marker: it selects CHR bank `$FF` mod 128 = **1KB bank 127**. Those areas probably have no enemies that use tiles `$80-$FF` (not verified).

**`ObjInit` `30:C25D` (session 8)** — type `$11` (1 = no-op). `SetR7($0D)` (bank 13 at `$A000`), ptr = `ObjTypeTable` `13:AC00`[type·2] (`$AD00` upper half when type ≥ `$80`, using the carry from ASL). Record:
```
snd                 if ≠0 → PlaySound(snd)
                    (then all 32 columns $0300/$0400/$0500/$0600,X+n·$20 are zeroed for the slot; type $FF stops here)
m0 {..}             bitmask, MSB first: each set bit → next byte stored to $0300,$0320,…,$03E0 (,X)
m1 {..}             → $0400 … $04E0   ($04A0 = active, $04E0 = anim)
m2 {..}             → $0500 … $05E0   ($0540/$0560 spawn b2/param, $0580 overlay, $05C0/$05E0 screen pos)
m3 {..}             → $0600 … $06E0   ($06C0 last; $06E0 only if bit 0 set)
```
So each object type is a sparse initialiser for the 32 `$0300-$06E0` column arrays. Spawn handlers write their position/param fields after `ObjInit`.

**Area transitions — what sets `$6C` (session 8)** — `AreaSubTable` `30:C7A5` (A=k): `SwitchBank16(5)` (banks 10/11), header ptr = `10:8300`[`$6C`·2] (`$8400` upper half), returns `($10)` = header's k-th ptr16, Y=0. **Sub-table 3 = exit list**, scanned by `ExitLookup` `31:EB09`: player (obj 0 `$70/$90`, `$B0/$D0`) → 16-px cell ($12 = x>>4 | xhi<<4, $13 = y>>4 | yhi<<4); records `xc yc area dest6D`, `FF` end. On a match: if `$41`=8 → `$6C`=area, `$6D`=dest, `$41`=1 and the two return addresses are popped (aborts the caller). Otherwise it returns Y at the record's byte 2, and the caller (`30:DAAB`, after a walk-up/door animation that runs until `$B0`=`$8F`) does the same stores itself (+ `$51`=`$4B`=0). `$6D` = entry point in the new area (meaning TBD). Other writers: `30:C96F` (=0, new game, after clearing `$6480/$6580` flags), `30:CBDC` (`AreaByIndexTable` `30:CBEC`[`$0780`&15] = `02 03 04 14 1A 20 40 42 60 64 72 78 7C 90 A8 98` — warp/continue destinations), `30:D5CB` (=`$F2`, scripted), `30:DC93` (`30:DC58`[`$22`]), `31:E5E3` (restored from stack), plus banks 10/22/23/28.

**Area header sub-tables (session 9)** — header at `10:8300`[`$6C`] (banks 10/11; 252 entries, the table ends at `$84F8` = area 0's header) = **5 ptr16s (k=0..4), data in order k0..k4 right after**. Only **10 areas have a 6th ptr (k5)**: `6A 6D 6E 8A 9F A2 A5 AE AF B4` (`area_dump.py sub stats`); for every other area a k5 read would return the k0 bytes (`snd`,`W`), so `31:E573` must only run in those areas. Null areas (null in `AreaPtrTable`) also have null/garbage headers. Consumers:
| k | Reader | Format |
|---|--------|--------|
| 0 | `AreaLoadMap` `31:E628` | `snd W H f62FE f62FF {grid}`: `PlaySound(snd)` (area music); `$62FC`=W = width−1 in screens, `$62FD`=H = height−1, `$62FE`, `$62FF` (b7 → `SetR6($0A)` when reading map data at `31:EBE5`; `$62FE`·8 used at `31:E75E`, TBD). Then **screen grid** → `$6300-$637F` (pre-filled `$80`): H+1 rows of W+1 bytes, each row at an 8-byte stride (8 screens wide × 16 high max). Each byte = screen/room id; **`$80` = no screen** (same as the prefill; grids are mostly `$80-$9F`, plus `$E0-$FF` and low ids like `$71/$72` in shafts) |
| 1 | `31:E6B9` | `p0 p1 p2 v67 v69 R0 R1` → BG palette ids `$07E0-$07E2` (`$07E3` always `$7F`), `$67`, `$69` (`$66`=`$68`=0; `$6A/$6B` = `$BE00`+(`$67`&`$3C`)·8 ptr), BG CHR shadows `$07F0/$07F1` (MMC3 R0/R1, 2KB). Then `JMP AreaLoadB` |
| 2 | `31:E686` | **entry-point table**: 4-byte records indexed by `$6D`&`$1F` → `$34-$37` = **camera X lo/hi, Y lo/hi** (`31:EA72` clamps `$34/$35` to 0..`$62FC` screens). `$6D`=`$FF` → no record, bank 26 `$98A8` X=0 instead; `$6D`&`$E0`=`$20` → keep current camera (record skipped) |
| 3 | `ExitLookup` `31:EB09` | exit list (session 8) |
| 4 | `30:C765`, `31:E702` | **screen ↔ persistent-flag list**: 2-byte `flag scr` pairs, `FF` end; scr = y_hi<<4 \| x_hi. `30:C75C` marks the player's screen in the **visited-screen bitmap `$62F0-$62FB`** (row y_hi, bit `BitMask[x_hi]`, i.e. 8 cols × 12 rows — automap) and, if the screen is in this list, sets WRAM flag `$64C0`[flag>>3] bit (flag&7). On area load `31:E702` (after zeroing `$62F0-$62FB`) restores each listed screen's visited bit from its `$64C0` flag → visited state of selected screens persists across areas |
| 5 | `31:E573` | **edge links** (only when `$20` b0 at `31:E561`): 4-byte `area a21 a22 a23`, scanned until match; match if a21>>4 = player `$90` and (a22&`$F0`)\|(a23>>4) = `$D0`. Then `$B0`+=16, new `$D0` = ((a22<<1)&`$F0`)\|(a23&`$0F`), new `$90` = a21&`$0F`, `$6C`=area (via `$E628` reload). Y wraps (256 bytes) with no match → `$41`=8, bank 26 `$8C49` A=4. No terminator byte. Example: area `$6A` first record = "x_hi 5, y_hi 1 → area `$69` at x_hi 5, y_hi 2" — probably **falling down a pit into another area** (not verified in an emulator; the k5 areas are tall, W=1 H=3 shafts like `$6E`/`$9F` or multi-floor maps) |

`$6D` = **entry-point index into sub-table 2** (camera start); `$FF` / `$2x` special values above. Area 0 example: k0 `00 00 00 00 00 [FF]` (1×1 screen), k1 `7C 7D 7E 98 B9 28 00`, k2 `88 00 BF 00` / `88 00 78 00`, k3 exits cells (6..A, C) → area 1 entry 0, k4 `EF 00`. `tools/area_dump.py sub [areas]` decodes all of it.

**Screen / metatile pipeline (session 10)** — no compression; all fixed-size raw tables.
1. **Screen id → page** (`ScreenPagePtr` `31:EF36`, entry `EF33` = `ScreenAtCamera` `EF1F` first): screen id S from `$6300`[(cam_y_hi<<3 & `$78`) | (cam_x_hi & 7)] (`EF1F`). **`SwitchBank16(S>>6)`** (n 0-3 → 8KB banks 0-7) and `$10/$11` = `$8000 + (S&$3F)·$100` → each screen is **one 256-byte page** in banks 0-7. Grid ids `$80-$BF` → banks 4/5, `$C0-$FF` → banks 6/7, `$40-$7F` → banks 2/3. (This is the "packed pointer" noted under `31:EF43`.)
2. **Screen page = 16×15 metatile ids** (byte (y&`$F0`)\|(x>>4), 240 bytes); the 16-byte tail `$F0-$FF` of each page is reused for other tables (palette records in banks 2/3, handler-1 spawn records in banks 4/5 — see above).
3. **Visited substitution** (`ScreenTileSubst` `31:ED99`): if metatile M < `$20` and the screen's visited bit (`$62F0` bitmap, read by `ScreenVisitedBit` `31:EC6C` → `$1E` for this screen, `$1F` for the next) is set, M = bank 9 (`SetR7(9)`) byte at `$BE00 + ($67&$3C)·8 + M` (`ScreenSubstPtr` `31:ED89`, 32-byte table per tileset). → visited screens show alternate metatiles (opened doors/broken blocks, tentative).
4. Column/row of metatile ids is buffered at `$6000,X`; `ScreenRowRender` `31:EBC1` crosses into the next screen at Y=`$10` (`$21`, `$1F`).
5. **Metatile → tiles** (`MetatileExpand` `31:EDBA`, banks 8/9 via `SwitchBank16(4)` at `EC14`): 4 parallel 256-byte pages at `$67&$FC` + 0/1/2/3 = TL/TR/BL/BR tile of metatile M (`$66`=0, so `($66),Y` with Y=M). `$67` = k1 `v67` (tileset). Attributes: `31:EDE5` → `SetR7(9)`, ptr table `31:EEEF`[(`$67`&`$3C`)>>1].
6. PPU upload: `31:ED64` builds nametable addr from camera, queued as a 4-byte record in `$6200+$0B` (`$6200`=hi\|`$0744`, `$6201`=lo&`$E0`, `$6202`=`$20` length, `$6203`=0), ring of 8 entries (`$0B` += 4 &`$1F`).

**Controller input (session 20)** — the only `$4016` readers are in `31:FF28-FF43`. Both pads are read (X=1 = pad 2 via `JOY1+X` = `$4017`, then X=0 = pad 1), so every pad variable is a 2-byte pair (pad 1 at the even address).
- `ReadPadRaw` `31:FF28`: strobe `$4016` 1/0, then 8× `LDA $4016,X` / `AND #3` (bit 0 or Famicom expansion bit 1) / `ROL $4F`. Byte layout of `pad_raw` `$4F`: **A=b7, B=b6, Select=b5, Start=b4, Up=b3, Down=b2, Left=b1, Right=b0**.
- `ReadPadStable` `31:FF17`: reads until two reads agree (DPCM-glitch guard); `$49,X` is used as the scratch copy.
- **`ReadPads` `31:FE80`** (gameplay; was mislabelled `ColdInitSub2`, 7 callers incl. `SubstPlay` `30:CB62` and the message wait loops): `pad_dir` `$49` = `PadDirTable` `31:FED0`[d-pad nibble] (0 = up, clockwise to 7 = up-left; `$FF` = none, U+D, L+R or all four; with three keys the opposed pair cancels, e.g. U+L+R → 0, U+D+L → 6); `pad_new` `$4B` = ~`pad_prev` & raw (edge); `pad_prev` `$43` = raw. Hold counters: on a new A (B) press `$45` (`$47`) = 0; while held it counts up. A: saturates at 31 and then sets carry each frame → `pad_act` `$4D` b7 = "A held ≥31 frames" (continuous). B: wraps every 32 frames → `$4D` b6 pulses once per 32 frames. `pad_act` = those two bits \| `pad_new`.
- **`ReadPadsMenu` `31:FEE0`** (menus: `16:80D4/8494/84EC/9887/9B42`, `15:BDE1`, `22:8BA3`): `pad_new` as above, plus **d-pad auto-repeat**: a new d-pad press loads `$45` = `$0C`; while the same d-pad bits stay held `$45` counts down, and at 0 it reloads 3 and ORs the d-pad bits into `pad_new` again (12-frame delay, then every 3 frames). `$45` therefore means A-hold in gameplay and d-pad repeat in menus.
- Known uses of `pad_new`: b7/b6 A/B close or advance messages (text code `03`, `30:D354`); b5 Select replays a message; **b6 B with an item selected → `PlayerUpdate` `26:9B45`** (use the item).

### Full-ROM disassembly (session 22, `tools/disasm_all.py`)

**Method.** Recursive descent from these seeds:
- the 3 vectors;
- every labels.csv ROM label whose name is not a data name (`--labels` shows the split);
- 15 dispatch tables, each with the bank context its dispatcher sets up: `MainStateTable`, `IrqHandlerTable`, `SubstateHandlerTable` (context from `SubstateBankTable`), `TalkActionTable`, `EventActionTable`, `ItemEventHookTable` (14/15), `SpawnHandlerTable` (12/13), `SndOpcodeTable` (24/25), `TextCtrlTable`, `HitHandlerTable`, `ItemUseHandlers`, `ObjAiTable` (26/27, ids `$60-$6F` → 26/15), `BossPhaseTable` (auto count **25**), and the new `State3ModeTable`/`State3SubTable`.

Each path carries `ctx = (r6, r7)`:
- `LDA #n` before `JSR/JMP SwitchBank16 / SetR6 / SetR7` updates it, and so do the fixed tail-calls `C7A0` (26/27) and `E3D4` (12/13).
- Any other `JSR` applies the **callee's summarised bank effect**: the tool walks the callee's fall-through path to its `RTS`. This catches helpers like `30:DA0C` (maps 20/21 before `JSR $9500`) and `19:B9FB` (`SetR6($1A)`, `JMP $8CC0`). A path through `RestoreBanks` `FFDB` counts as no change.
- Code running from a swappable window always keeps its own bank pinned there.
- If the context is unknown, a target resolves only when exactly one bank has a code label at that address.
- Each target must pass a 10-instruction decode check before it is traced; targets that fail are reported as "rejected".
- Z/N/C are tracked through `LDx #imm`, `CLC/SEC`, flag-neutral instructions and not-taken branches. This makes `LDA #0 / STA / BEQ` and `BNE x / BEQ y` pairs unconditional (e.g. MainLoop `C91D`, `15:BF1A`).
- Code and operands may run across a window edge into the next window's bank (16KB pairs, and `$C000→$E000`).

There are no JSR-with-inline-data helpers: all 6 `PLA/STA/PLA/STA` sites restore values pushed earlier. Swappable banks have **no** JMP entry table at `$8000`/`$A000`; the call-site map's "fixed entry offsets" (`$8020`, `$8028` …) are plain routine addresses.

**Results:**
- 40,170 code bytes; 1,978 auto-named targets.
- 0 warnings, 0 rejected targets.
- 1 unresolved target: `15:AB84 JSR $9824`, after `PLA / JSR SetR6` at `15:AB6F` restores a pushed bank, almost certainly 26. `15:AB87 JMP $9897` resolves through the `TempToObjPos` label.
- 2 straddling instructions: `18:9FFF` (`STA $04` → `19:A000`) and `30:DFFF` (`STA abs` → `31:E000`). `26:9FF4` branches across into `27:A002`.
- 33 indirect-jump sites. All but 8 are the known tables. The 8 dispatchers with untraced tables:

| Site | Index | Table | Note |
|---|---|---|---|
| `17:A117` `JMP ($008E)` | `$0A`·2 | `17:A11A` | state 4/5 code |
| `27:A06E` `JMP ($0010)` | A·2 | `27:A072` | AI code |
| `27:BA3D` `JMP ($0010)` | `$0600,X`·2 | `27:BA40` | AI phase dispatch |
| `30:DBD9` `JMP ($0010)` | A·2 | `30:DBDC` | after `SubstTeleportMenu` |
| `30:DDBA` `JMP ($0010)` | A·2 | `30:DDBD` | after `SubstTelepathySelect` |
| `30:DF0B` `JMP ($0010)` | A·2 | `30:DF0E` | after `SubstChangeForm` |
| `31:E419` `JMP ($0010)` | A>>4 (no ASL) | `31:E3D9` | after `SwitchBank16_6`; spawn/entity path |
| `31:F6BF` `JMP ($0052)` | `irq_mode`·2 | `31:F6C2` | NMI/IRQ-mode dispatch |

**Main states 2/3 (new, session 22).**
- State 2 `30:C931` = `SwitchBank16(9)`, `JMP $9FCC`. This lands in **`18:9FCC` `MainState2Init`**, which session 21 had wrongly listed as dead filler. It calls `18:82B3`, `81D8` and `8198`, sets `irq_mode`=3, and clears zero page `$70-$98` (object x/y arrays), `$0600-$0628` and `$0680-$06FF`. It then runs straight on across the window edge into `19:A000`, sets `$06B2`=`$FF`, does `INC main_state` (→ 3), and calls `19:A07C` twice.
- State 3 `30:C939` = `ReadPadsMenu`, then `JMP $A015` (**`MainState3`**). It calls `A07C` unless `$06B2` bit 7 is set, clears `frame_ready`, decrements `$06FF`, and dispatches on `$06F3` through `State3ModeTable` `19:A038` (3 entries: `19:A03E`, `18:8310`, `18:834A`).
- Mode 0, `State3SubDispatch` `19:A03E`, dispatches on `$06F0` through `State3SubTable` `19:A052`: 7 handlers `A79F ABB4 ABEA A0B5 A176 A653 A6DF` plus 2 null words.
- Reset sets state 2, so states 2/3 are the boot / title-side screens. The individual modes are not named yet.

### Compression (session 10)

**Result: no compressed data found in PRG.** CHR is ROM (128 KB, loaded by bank-switching, never uploaded), so there is no CHR decompressor either. Evidence:
1. Every PPU-data writer was checked (`search_bytes.py "8D 07 20"`, 49 sites): fixed-bank sites copy from RAM buffers; bank 17/18 stream readers are raw `W H {tiles}` rectangles (below).
2. The level renderer reads **raw fixed-size tables**: 256-byte screen pages in banks 0-7 → metatile quadrant pages in banks 8/9 (see "Screen / metatile pipeline"). The only transform is the visited-screen metatile substitution (a table lookup, not compression).
3. Stream reads with bit-7 control tests (`B1 ?? 30`, `B1 ?? 10`, `B1 ?? 29 7F` — the usual RLE/LZ flag patterns) give 10 hits, all non-compression: signed sprite offsets (`17:A974/A994`, `20:9538`), an object script (`14:8297`), and data false positives (bank 27, `12:9685`).
4. Entropy (`tools/bank_entropy.py --block 1024`): no 1KB block reaches 7.0 bits/byte (max 6.9 in banks 20/28 = code). LZ/Huffman data would be 7.5 or higher. Banks 0-7 (screens) are 2.8-6.5. `strings` on bank 0 shows visibly repeated 16-byte metatile rows (`WWWW…`).
5. Banks 21-23 have a very flat ~5.6-5.8 per 1KB block. Session 11 showed this is **ASCII dialogue text with a 128-word dictionary** (single-byte word codes, not DTE or compression). See "Text / dialogue".

So `tools/decompress.py` is not needed. The web port can load screens, metatiles and tile maps directly from ROM offsets.

**Nametable uploaders (session 10)** — all `STA $2007` sites (`search_bytes.py "8D 07 20"`, 49 hits): fixed banks 30 `C6B4-C716` and 31 `F8EC-F9A6` are **unrolled copies from RAM buffers** (PPU queue / palette); the only stream readers are a small screen-setup library duplicated in **bank 18 (`$8174-$82B2`) and bank 17 (`$A720-…`, ZP `$80-$82` instead of `$72/$73/$88`)**:
- `NtFill` `18:81A1` (A = PPU addr hi, X = tile, Y = attr byte): rendering off, 10×`$60` = `$3C0` tiles of X then `$40` attr bytes of Y. 17 callers (banks 18/19).
- `NtRectBlit` `18:81F0`: stream at `($8E)` = **`W H` then H rows of W raw tile bytes**, each + `$06FE` (tile bias), written to PPU at `$8C/$8D`, row stride `$20`. 32 callers (bank 18 `8071`, 31 in bank 19 `A10B-B148`) → title/menu/cut-scene screens.
- `NtRectBlitQueued` `18:824B` (no static xref — reached indirectly or dead): same `W H {rows}` format but each row is copied to `$6000` and queued as a `$6200` PPU record, one row per frame (`$8198` waits for `$09`).
→ **No RLE/LZ**: tile maps are raw W×H rectangles.

**`$18` (session 8)** — the flag `AreaLoadA/B` writes has **no reader**. Fixed banks 30/31 only store to `$18`, and the zp hits in banks 12/13/14 (the banks mapped during AreaLoad and its spawn handlers) all decode as data. `Bank26_FadeA/B` reuse `$18` as a temp. It is most likely a dead write. Not checked in an emulator.

**Handler-1 indirect spawn records (session 8)** — `area_dump.py indirect` lists every handler-1 param used by any area together with its bank 4/5 record. 137 distinct params; all records exist (no garbage):
- p `$00-$8E` → `00 00 xx mm`-style **type `$30` generic objects**. Byte 3 = meta id, mostly `$2C-$4A` (static props/NPCs: `$2C $30 $34 $38` most common), some `$AD`/`$DB`/`$BF`. Byte 2 is usually `$02/$04/$06` (→ `$06E0` behaviour flags) and sometimes has b3 set (y\|=`$0E`, sits low in its cell). `$0680/$06A0` are non-zero in only ~15 records (e.g. p `$5E` = `41 34`, `$7E` = `47 48`). These are probably talk/dialogue or event ids (TBD).
- p `$C0-$CC` → `FF tt FF FF` = **direct type redirect** (type `$57 $5E $68 $70 $8B $90 $93 $97 $9C $9D $9B $A5`), one per area (`0A 1A 28 43 A9 AC B9 B6 A0 9F/A6 6C`). These are probably bosses or unique objects.
- only p `$65` (area `$01`) has `$22` b7 set (`$04A0`=`$50`, i.e. starts inactive/special).

`tools/area_dump.py [list|chr|spawns|indirect] [areas]` decodes all of it. `chr` mode groups areas by (R4,R5): 120+ distinct pairs; most common `4A/4A` (28 areas, `$BC-$FB` range), `67/66` (12 areas: `11 23 24 26 27 2D 44 48 49 4B 4E 4F`), `FF/FF` (10). Verified: `meta_dump.py png --chr 40,42,67,66` renders the blue humanoid enemy ids (`$58-$5F`, `$6B-$6F`, `$8C-$8F`) cleanly where the old `50,51` placeholder scrambled them → `gfx/metasprites_area67_66.png`.

**Port note:** reimplement as: for slot in order[rot&3]: compute screen pos, resolve record (follow one `FF` with mirror), emit N sprites of frame f; then hide rest; then orbit ring. Sprite limit is 63 (OAM `$04-$FF`) minus 8 orbit slots overlapped — the orbit writes `$E0-$FC` unconditionally after the hide pass, overwriting objects there.

### Text / dialogue (session 11)

**Storage.** Messages are ASCII bytes (`$20-$7E` literal), single-byte **dictionary words** (`$80-$FF`), and control codes `$00-$09`. There is no DTE and no compression. Bank profile (`tools/bank_profile.py 21 22 23 --pages`): 70-80% printable ASCII on every page of banks 21 and 22 and on 23 `$A000-$BAFF`. The top bytes are `20 65 61 74 73`, i.e. English letter frequency.

| Where | What |
|---|---|
| `20:83FE` `TextGroupBank` | 36 bytes, bank per group (`$07DF`): groups 0-13 → bank 21 (`$15`), 14-25 → bank 22 (`$16`), 26-35 → bank 23 (`$17`). Only 34 groups have pointers, so the last 2 bytes are unused |
| `20:8422` `TextGroupPtrs` | 34 words (groups 0-33) → per-group message pointer arrays at `20:8000-83FD`. Entries "34/35" (`$F720`, `$7573`) are text bytes, not pointers |
| `20:8000-83FD` | per-group arrays of 2-byte message pointers into `$A000-$BFFF` of the group's bank. Group n has (ptr[n+1]-ptr[n])/2 messages |
| `20:8466-84FF` | **dead filler**: a stale byte-for-byte copy of bank 22 `$A466-$A4FF` at the same in-bank offset (msgs 15.18-15.23). No message pointer is < `$A000`, so it is never read (session 12, `text_dump.py at 20:8466 8`). **Session 21 correction:** the copy ends at `84FF`, not `8527`; `20:8500` is live code (`TextWindowOpen`). Other bank-20 stale text: see "Stale-copy filler" under the Data-Range Map |
| `20:8900` `TextDictPtrs` | 128 words: pointer for dictionary code b = `$8900 + (b&$7F)·2` (the `ASL` drops bit 7) |
| `20:8AF0-8D3x` `TextDict` | zero-terminated words, e.g. `you`, `your`, `you've`, `the`, `with`, `Please`, `magic`, plus proper nouns |
| 23 `$BC00-$BEFF` | **not text**: save/load code (below) |
| 23 `$BF00` | 256-byte per-area save table (`00`/`80`/`81`), not text — see Save / load module |
| 21 `$BF00-$BF30` | real text: tails of messages 13.1-13.4 (last message ends at `$BF30`). **`21:BF32-BFFF` is dead filler**, a stale copy of `23:BF32-BFFF` (identical bytes, same offset); there is no separate bank 21 table and nothing reads it (session 12) |

**Printer `TextPrint` `20:853A`** (bank 20 mapped alone with `SetR6($14)`; callers `30:D35C/D382/DB6A/DE24/DF97`; `20:8528` loads 16 bytes of window params from `20:8889` to `$20-$2F`, then calls it):
1. X = `$07DF` (group) → `$6700`; `SetR7(TextGroupBank[X])` through the inline copy `20:885A`, which writes `$6F`/`$50` itself and does not call `SetR7`.
2. `$2C/$2D` = `TextGroupPtrs[X]`; Y = (`$07DE` & `$1F`)·2 (`$07DE` → `$6701`), so there are at most 32 messages per group. `$2A/$2B` = message pointer.
3. Loop on `($2A),Y`: b < `$0A` → control dispatch `20:8593` (jump table `20:85A2`, 10 entries); b ≥ `$80` → `TextWord` `20:86F5` (saves the pointer in `$2C/$2D`/`$29`, switches `$2A` to the dictionary word, and continues at `$8693`); otherwise → `TextChar` `20:87AE`.

Control-code table `20:85A2`:

| Code | Handler | Arg | Meaning |
|---|---|---|---|
| `00` | `85B6` | – | end: `PLA PLA` drops the loop's return, then `TextFlush` `85B8` uploads the line buffer and returns from `TextPrint` |
| `01` | `85F2` | – | clear box (`TextClearBuf` `8720`, waits for the PPU queue `$0A`=`$0B`), cursor `$28`=`$23` |
| `02` | `8607` | – | newline: `$28` = ((`$28`+`$40`)&`$E0`)+4, i.e. down 2 rows, column 4 (text lines are double-spaced) |
| `03` | `8619` | – | page break: flush, put tile `$40` (prompt) at PPU `$2910`, wait for A/B (`$4B` b7/b6) one frame at a time (re-maps the text bank after each `ReadPads` `FE80`), then clear |
| `04` | `866B` | – | print the zero-terminated **player name at `$6400`** (WRAM, saved to `$7D00`) |
| `05` | `8682` | n | string `20:8A00`[n] (10 common words/phrases) |
| `06` | `86CD` | n | string `20:8A14`[n] (36 proper nouns: people + places) |
| `07` | `86E1` | n | string `20:8A5C`[n] (74 item/equipment/spell names; `$49` = 17 spaces) |
| `08` | `8772` | – | string `20:8A5C`[`$07DC`] (`$FF` → `$49` blank): the current item |
| `09` | `8753` | n | n spaces |

**Auto-space quirk:** words (`$80+`) and codes 05/06/07 return through `$869F`. That code peeks at the next byte and writes a space if it is `04-09`, or ≥ `$40` and ≠ `$5F` (letters, `_`-excluded, words). Literal chars, `04` and `08` never auto-space, so the same table string can act as a word fragment (`every{05:one}`). The port must reproduce this rule exactly.

**Buffer/box:** `TextChar` `87AE` stores to `$6000[$28]`, `$28`++. `TextClearBuf` fills `$6000-$60FF` as 8 rows of 32: `FE 1E` + 29×`$20` + `1F` (left/right border tiles), cursor `$20`. `TextFlush` `85B8` uploads 4×`$40` bytes (`$25` = src offset 0/`$40`/`$80`/`$C0`) to PPU `$2820/$2860/$28A0/$28E0` through `20:8836`. The text box is therefore nametable 2 rows 1-8.

#### Who picks the message (session 12)

All message display in gameplay funnels through `ShowMessage` `30:D354` (`$07DE`/`$07DF` already set):
1. `$58/$59` (BG CHR R0/R1 shadows) = `$3C`/`$38` → the **text font** is CHR 1KB banks `$3C-$3D` (R0, 2KB @ PPU `$0000`) + `$38-$39` (R1, `$0800`).
2. `SetR6($14)`, `JSR 20:8500` (window open), `$C169`, `$51` = 1 (or 9 if it was 7 — keeps the split for that mode), sprite pass `FarCall_B14_800E`, `$C17D`.
3. `SetR6($14)`, `JSR 20:8528` (params from `20:8889` → `$20-$2F`, then `TextPrint`).
4. Wait loop (one frame each, `ReadPads` `FE80` + sprite pass): `$4B` bit 5 (**Select**) → **replay** the message from step 1; A/B (`$C0`) → close: 8 more frames, `$51`=0, `$4B`=0, sub_state `$41`=8.

Setters of `$07DE/$07DF` (27/24 xrefs — many more than session 11 listed):

| Site | Message | Context |
|---|---|---|
| `30:D0B3` | (`$06C3`, `$06E3`) | **pending message slot**: written by entity code in banks 15/19/26/27 and `30:CC25/D52C`; entry `30:D085` saves `$51`, plays SFX `$29` (or `$93` + music bit 7 when it is msg `$0D` of group `$20`), shows it, restores `$51`, `JMP $C8A9` |
| `30:D0F5` | group `$1F` msg 0 | default reply when the touched spawn entry is not a talker |
| `30:D1D1` | group `$13` msg 0 | after SFX `$29`, then `AreaLoadA` |
| `30:D1ED` | group `$0B` msg 1 | then SFX/event `$45` via `$D22B` |
| `30:D347` `ShowMessagePacked` | `$20`/`$21` → `$07DE`/`$07DF`, then `TextIdUnpack` `30:D3C2` | **linear id**: `$07DF` = `$21`·8 + (`$20`>>5), `$07DE` = `$20`&`$1F`, i.e. `$21:$20` is a 16-bit message number = group·32 + msg |
| `30:D43C` | 32.12 "You now have {ITEM}" | `GiveItem` `30:D3EB` (see Talk actions) |
| `30:D48C` | 32.15 "You have too many items!" | `GiveItem` path when `$23` bit 7 (no free inventory slot) and `$08`&`$0F`=0, SFX `$29` |
| `30:D5BE` | 28.17 (Sword of Thunder found) | after `SwitchBank16(9)` `JSR 19:BC04` (`SwordRaisePose`, session 18; `$BC04` is in the `$A000` half, so the routine is in bank 19); SFX `$23`, then `area_id` = `$F2` (story event warp) |
| `30:D628` | 14.5 (Mesia's message) | after `ObjInit` type `$DE` in slot `$1E` at a saved position |
| `30:D7A3` | 22.0, 22.2, 22.4… | Azteca's long speech: `ShowMessage` loop with `INC $07DE` ×2 per page |
| `30:D9CB` | 32.17 "Nobody's here." | after `30:DA0C`, then `sub_state` = `$24` (probably a spell/action with no target; caller not traced) |
| `30:DB67` | 33.0 (town list) | **substate 20 `30:DB28` = teleport menu** (`PaletteBuild`, `$51`=0, window + print, `30:DD36` cursor) |
| `30:DD75` | 33.2 "With whom will you speak?" | **substate 21 `30:DD6B` = telepathy: pick a sage** (CHR R4=`$50`, R2=`$5C`) |
| `30:DE1E` | from `14:816F` | **substate 22 `30:DE04` = telepathy reply** (see below) |
| `30:DED0` | 33.1 "What form will you take?" | **substate 23 `30:DEC1` = change-form menu** (`orbit_level`=0, CHR R4=`$4D`, R2/R3=`$5C`) |
| `15:BAE7` | from data | entity in bank 15: `$0600,X` (object state) → ptr table `15:BAFE`, record bytes `[1]`→`$031F`, `[2]`→`$033F`, `[3]` msg, `[4]` group (spawns an object at a saved position first via `FarCall_C25D`/`TempToObjPos`) |
| `19:BFD1` | table `19:BFE8`[`$2E`] = `(group, msg)` | 5 entries: 32.29, 27.15, 27.16, 27.17, 27.18; SFX `$29`; nearby code sets `main_state` = 4 |
| `27:BB28` | 27.5, 27.6, … | entity whose `orbit_level,X` counter has gone negative; SFX `$29`, `FarCall_D354` (ShowMessage) in a loop with `INC $07DE` |

Substates 20-23 are entries `30:CB06-CB0C` of the substate table at `30:CADE`.

**Telepathy reply `Telepathy` `14:816F`** (`$23` = sage 0-3 = Tornel/Zebu/Asina/Kensu, from `$21`): result `$21:$20` = word `14:9A2C`[(row·4 + sage)·2] (linear message id).
- MP < 8 → row 0 = 32.14 "Your magic power is too low." (same for all 4 sages).
- Else MP −= 8, r = `14:821A`[`14:822F`[`$07C0`&`$3F`]] (`$07C0` incremented each call; 64-byte pseudo-random sequence of 0-7, mapped to classes `00 01 02 03 02 02 03 03`).
  - r=0 → MP += `$20` (clamped to `mp_max`), row 1 (e.g. 29.1 "…I will recover your magic power").
  - r=1 → row 2.
  - else → `$29` = r&1, `$28` = `14:98F4`[`area_id`]; if `14:8213`[`$28`] ≥ `$0421` → row 3, else an area-specific hint (code continues at `14:81D1`).
- **Hint path (session 14)**: `TelepathyHintClass` `14:98F4` (256 bytes, area → class 0-6), `TelepathyLevelGate` `14:8213` = `02 04 06 08 0A 0C 0E`: a hint is given only when **player level `$0421` > gate** (else row 3, a generic blessing). `TelepathyHintLists` `14:99F4`[(class·4 + sage)·2] → list of entries `flagref(2) msg(2) [altmsg(2) if b0 bit 6]`. `14:81E5` walks the list: the **first entry whose flag test is FALSE** supplies the message (the alt message if `$29`=1), so each entry is "show this hint until flag F is set". An entry that tests TRUE and has b0 bit 7 ends the list → row 3. Fixed rows: 0 = MP too low (32.14), 1 = MP restore (29.1/28.13/29.14/29.28), 2 = refusal (29.2/28.14/29.15/29.29), 3 = blessing (5.12/19.19/26.10/19.4). Classes follow game progress by area id (class 0 = areas `00-27`+`C0-D0`, 1 = `28-3F`, 2 = `40-57`, 3 = `60-8E`+`E3-F7`, 4 = `A8 BC-BF`, 5 = `8F-BA` subset, 6 = `58-5F`, `90-A7` subset, `F8-FB`). Lists are shared, e.g. Kensu classes 0-4 all use `9AE0`. **Tool:** `tools/telepathy_dump.py [--out f]` → `gfx/telepathy.txt` (run session 14).

**NPC talk (`30:D0C8`)**: Y = `$0623` (the spawn entry the player is touching). If `$05A0,Y` = 0, or `obj_spawn_b2,Y` = 1 and `obj_spawn_param,Y` ∈ {`$5E $5F $62 $6C $6D`}, it is a talker: `$23` = `obj_spawn_param,Y`, `SwitchBank16(7)` (banks 14/15), `JSR 14:8000` **NPC script** → returns `$20/$21` (packed message id) and `$22` (action). `ShowMessagePacked`, then `JMP` through **`TalkActionTable` `30:D123`** (32 words, indexed by `$22`; `$D163` = no-op `RTS` after clearing `$04AE/$07D7`). Otherwise → group `$1F` msg 0.

**NPC script format (`NpcScript` `14:8000`, banks 14/15)** — script ptr = `14:895D`[p] for p < `$80`, `14:8A5D`[p&`$7F`] otherwise (params `$C4-$FF` mostly have no valid script; many params share a script, e.g. `$80`-`$C2` → `8AE5`/`8B39`):
- **Flag ref** (2 bytes, `NpcFlagRef` `14:8148`): `b0 b1`; flag number F = (b0&7)·256 + b1 → byte `$6480`+(F>>3), mask `14:8167`[b1&7] = 1<<(b1&7). b0 bit 7 = last entry of a list; bit 6 = entry carries a set-list (inside a set-list: last item); bit 5 = inverted test. **`$6480-$657F` = 2048 story/event flags** (saved to `$7E00`). F`000` is never set, so `!F000` = "always" and `F000` = "never".
- `NpcFlagTest` `14:8135`: Z clear = condition true. `NpcFlagApply` `14:8112`: for each set-list item, bit 7 → clear flag, else set; stops at an item with bit 6.
- **Result** (2 bytes, `NpcResult` `14:8078`): r0 r1 → `$22` = r0>>3 (talk action, `TalkActionTable` index), `$21` = r0&7, `$20` = r1 → message id (r0&7)·256+r1 (group = id>>5, msg = id&31).
- **Section A** (stateless override): entries `cond result [set-list]`; the first true cond returns its result **without** applying its set-list (`JMP 8078`). Ends at cond with bit 7. Status flags F`028-02B` (`$6485` b0-b3) are tested here first by many NPCs (reply `" ___"` group 30 = status-effect reactions).
- **Area list**: `(area, state)` pairs until `FF`: if area = `$6C`, **`$6580`[p] = state** (`$6580-$667F` per-NPC conversation state, saved via `$7F00`).
- **Section B** (stateful, base = byte after the `FF`): start at offset `$6580`[p]; entries `cond result next [set-list]`; the first true cond → result, `$6580`[p] = next, set-list applied. Each chain ends at bit 7 (normally `!F000` = default reply).
- Other bank 14 entry points with the same flag-ref format — callers traced session 13:
  - **`NpcHidden` `14:80A0`** (per-param area list at `14:85E0/86E0`): called from the spawn handler at `31:E34B` (`$23` = spawn param `$2F`); if it returns `$20`≠0 the freshly spawned NPC slot is cleared (`obj_active`=0) → **the NPC is absent** in this area under the current story flags.
  - **`ItemEventHook` `30:D834`** (A = hook 0-2, `$23` = `obj_spawn_param`[`$0623`]): `SwitchBank16(7)`, calls word `30:D885`[A] = `14:826F` (A=0, from `GiveItem` `30:D401`), `14:831E` (A=1, `30:D51C`), `14:80E3` (A=2, `30:D541`, items ≥`$80`; table `14:A17A`, applies a set-list). If after the hook `$23` bit 7 is clear and `$21:$20`≠0, it plays SFX `$29`, shows that message (`ShowMessagePacked`), and sets `sub_state` = 8. A `$23` with bit 7 set makes `GiveItem` take the "too many items" path.
  - **`Telepathy` `14:816F`**: the 8-MP telepathy spell, called from substate 22 `30:DE04`, with message table `14:9A2C` (see "Who picks the message").
- **Decode fixes + junk scripts (session 19).** The VM (`14:8000-8076`, checked against the disassembly) only reads a section-B chain until the first true cond. The dumper used to follow every `next` byte, which caused two kinds of junk:
  - **Sentinel `next`.** `$38` (areas `$DF/$E1`) ends its only chain (B+00-B+6E, every `next`=00) with `80 00 00 00 FF`: cond F000, which is never true, and `next`=`FF`. Following that `FF` decoded the bytes after the script, which gave the bogus "set F100" entries at B+E0/B+EB. Fix: the `next` of a never-true (`F000`) entry is not followed.
  - **Exhaustive chain without a last bit.** `$45`'s chain is `F08F`, `!F030`, `F030` with no bit 7, so it always ends at the `F030`/`!F030` pair. The bytes after it (`00 28 03 C2 … FF`) are the next script's section A. Fix: a chain stops after `!F000` or after a cond that negates an earlier cond of the same chain.
  - **Real overlap, `$7E` (Kensu; shared with `$74`), static-RE only.** B+00 = `E0 00 9A 66 00`: last + set-list + `!F000`. Its set-list is the bytes of the B+05/B+0A entries, which the area list (`F1:05`, `62:0A`) also uses as entry starts. So when you talk to `$7E` in area `$EF` (state 0) the VM applies `clr F000, set F261, set F540, clr F451, clr F00A`. That is how the bytes decode; confirm it in the emulator.
  - **`tools/npc_script_check.py [p…] [--flag Fxxx]`** reports section-B entries that start inside another entry. `linear p…` decodes B back to back and marks `next` values that don't land on an entry start. After the fixes, the only params flagged are `$74/$7E` (the real overlap) and params whose pointer leads into code or other data: `$CB $D0 $D5 $DA` → `14:8000` (the VM itself; these are boss params), `$D4`, `$DD`, `$E3`, and `$E6/$EB/$F0`. None of the latter is placed as an NPC.
  - **`flag_sites.py`** now moves flags from junk scripts (pointer `14:8000`, or unplaced and misaligned) to the "unplaced-only" list. Regenerated `gfx/npc_scripts.*` and `gfx/flag_sites.txt`: **232 flags with a real site** (was 496), and 193 only from unplaced triggers or junk scripts. **F100** is now just the Rabbit Boots pickup (the first boss's reward) plus the presence test for boss param `$C0` in area `$0A`, which answers the session-18 question.
- **Tool:** `tools/npc_script_dump.py [p...] [--out f] [--json f]` decodes every script with message text (imports `text_dump`). Run session 12 → `gfx/npc_scripts.txt` + `gfx/npc_scripts.json` (256 params, 50 without a valid pointer). Example `$5E` (`Zebu`): section A → status replies; B+0F `if F00A → act 3, msg 0.27 "I will now teach you the magic…" [set F039]`; area `$AA` → state `$33`.

#### Talk actions — `TalkActionTable` `30:D123` (session 13)

Run after the NPC reply is shown (`$22` = action, Y/`$0623` = the NPC's spawn slot). Helpers:
- **`GiveItemA` `30:D22B`**: `$057F` (= `obj_spawn_param` of slot `$1F`) = A, temporarily `$0623` = `$1F`, `JSR GiveItem` `30:D3EB`. `GiveItem` takes the item id from `obj_spawn_param,Y` → `$07DC` (ids ≥`$80` → `30:D539`, `$70` → `30:D3DA`, ids `$50-$7F` remapped through `30:D40C`[id]), plays SFX `$3F` (or `$93` + music bit 7 for ids `$40-$4F`), shows **msg 32.12 "You now have {ITEM[$07DC]}"**, clears the slot. Item names are the code 07/08 table `20:8A5C` (`gfx/text.txt`); ids `$41+` are magic spells, e.g. `$45` Recover, `$48` Flight.
- **`NpcVanish` `30:D31C`** (entry `D31F` with Y given): `$0660,Y`=1, `obj_active,Y`=`$53` (vanish/fade type), `$05A0,Y`=`$03A0,Y`=0, SFX `$6D`.
- **`SpawnFxAtSlot` `30:D1FF`** (X = source slot): `ObjPosToTemp` `26:98A8` (slot X x/y → `$34-$37`), `TempToObjPos` `26:9897` into slot `$1E`, `ObjInit` type `$DF` in slot `$1E`, `obj_tile_ofs`=`$80`, `JMP AreaLoadB`.
- **`HudRedraw` `26:8E46`** (A = element): 7 six-byte records at `26:8EC5` = `value ptr, PPU addr, digits, flags`: 0 `$0421`→`$2B39`, 1 `$0702`→`$2B59` (16-bit), 2 `$0704`→`$2B68`, 3 `$0706`→`$2B6E`, 4 `$0708`→`$2B77`, 5 `$0709`→`$2B7B`, 6 `$0421`→`$2929`.

| `$22` | Handler | Effect |
|---|---|---|
| 0,4,7,11,14,15,18,28-31 | `D163` | no-op `RTS` |
| 1 | `D19C` | `ObjInit` type `$64` into slots s and s+1, where s = ((`$0623`−1)&`$FE`)+1 (an odd/even slot pair) |
| 2 | `D31C` | NPC vanishes (`NpcVanish`) |
| 3 | `D225` | **give item `$0680`[NPC]** (the NPC's 2nd param; "teach magic" scripts use this) |
| 5 | `D164` | clear `$04AE` (slot 14 active) and `$07D7` |
| 6, 13 | `D23F` | slot 13 becomes type `$32` (follower): `$03AD`=`$062D`=`$064D`=0, `$04ED`=1, `$060D`=`$FF` |
| 8 | `D63F` | if `$04AE`≠8: re-`ObjInit` slot 14 with type `$04CE` |
| 9 | `D263` | SFX `$3F`, **`$0702` += 100**, `HudRedraw(1)` — **quirk**: on carry it does `INC $0702` (low byte) instead of `$0703`, so a wrap gives low+101 and loses the 256 |
| 10 | `D2F4` | `ObjInit` type `$0F` in slot `$1E` at the NPC's position, `$057E`=`$39`, `$055E`=2, `$061E`=3, then `NpcVanish` |
| 12 | `D25A` | NPC `obj_active` = `$54` |
| 16 | `D1EB` | msg 11.1 ("teach Recover"), give item `$45` (Recover), `SpawnFxAtSlot` slot 14 |
| 17 | `D21D` | give item `$06A0`[NPC] if non-zero, else `$0680`[NPC] |
| 19 | `D1B5` | `SpawnFxAtSlot` NPC, `$033E`=`$A0`, `AreaLoadA`, `NpcVanish` slot `$0623`+1 |
| 20 | `D18E` | give item `$48` (Flight), then action 23 |
| 21 | `D2D3` | if `$04AE`≠`$32`: slots 13+14 both type `$32`, `$068D`=`$0E`, `$068E`=`$0F`, `$04ED`=`$04EE`=1 |
| 22 | `D1CA` | SFX `$29`, msg 19.0, `AreaLoadA`, `SpawnFxAtSlot` NPC, `$033E`=`$A0` |
| 23 | `D16D` | **full restore**: `$03C1` (player HP) = `$03C0` (max), `$0708` (MP) = `$0709` (max MP), redraw life bar (`26:8CC0`) + `HudRedraw(4)`, then `NpcVanish` |
| 24 | `D280` | warp: `$0620`=0, player slots 0/1 moved to slot `$1E`'s position, `obj1_flash` |= `$40`, `$0561`=`$0641`=2, `$04BE`=`$0C` |
| 25 | `D2AE` | give item `$30` (Shield Ring), slot 14 becomes type `$32` follower (`$068E`=`$0D`) |
| 26 | `D336` | give item `$09` (Ball of Water), `$060D`=4 |
| 27 | `D341` | `$060D`=6 |

**`$0702/$0703` = gold** (16-bit, saturates at `$FFFF`): enemy drops add `26:8BDE`[(`$0500`>>4)·2] at `26:946D` (SFX `$24`). `$0708`/`$0709` = MP / max MP, `$03C1`/`$03C0` = player HP / max HP (based on action 23; static RE only).

#### Event triggers — `ItemHook2` `14:80E3` / table `14:A17A` (session 14)

Spawn entries whose "item" id is ≥ `$80` are **invisible event triggers**, not items. `GiveItem` → `30:D539`: `sub_state` += 2, `$08`++, `ItemEventHook(2)` → `14:80E3`. Record = `EventTriggerTable` `14:A17A`[(id&`$7F`)] (67 words, ids `$80-$C2`):
- `cond…` flag refs (2 bytes each) up to the one with b0 bit 7. **All must be true**; else return with `$23`=`$FF` and no message (the trigger stays armed).
- then `result` (2 bytes, same as `NpcResult`: action = r0>>3, message = linear id; id 0 = no message), then a set-list applied by falling into `NpcFlagApply` (at least one item is always applied, so "no-op" records use `clr F000`).
- Quirk: `14:80E6` `CMP #$86 / BCC $80EA` branches to the next instruction, so the range check does nothing. Ids > `$C2` would read past the table.
- After the hook (`$23` bit 7 clear): `30:D548` → `JSR 30:D552` jumps through a **second 32-entry action table `EventActionTable` `30:D563`** indexed by `$22` (differs from `TalkActionTable` `30:D123`; handlers named below), then `sub_state`=8 and `JMP $CAB6`.
- **`EventActionTable` handlers (session 15)** — Y/`$0623` = the trigger's spawn slot; "slot 13/14" are the follower slots, `$1E` the FX slot. Item names from `gfx/text.txt`. Helper **`WaitSfxDone` `30:D88B`**: `WaitFrame` `30:C169` until `snd_sfx_priority` `$0112`=`$FF`. Helper **`MakeFollower13` `30:D23F`** = talk act 6 (slot 13 type `$32`).

| act | Handler | Effect | Used by triggers |
|---|---|---|---|
| 0,3,5,18,26 | `D5A3` | no-op `RTS` (4 = `D653`, also `RTS`) | most; `$87-$89` wise-men rescue |
| 1 | `D5A4` | **Sword of Thunder warp**: `$0600`=`$0620`=`orbit_level`=0, `SwitchBank16(9)` + `JSR 19:BC04` (`SwordRaisePose`, see below), msg 28.17 ("You've finally found the Sword of Thunder… Azteca awaits you"), SFX `$23`, `area_id`=`$F2`, `area_entry`=`$41`, `sub_state`=1 (area load) | — |
| 2 | `D5D6` | `$0623`=13, give item `$06A0`/`$0680`[13] (talk act 17), `$030D`=`$88`, `MakeFollower13` | — |
| 6, 17 | `D6D5` | `JMP AreaLoadB` (reload area) | — |
| 7 | `D5E6` | **Mesia's message**: trigger slot cleared; slot 13 `$030D`=`$D2`, waits `$20` frames (`D62E`: `WaitFrame` + `FarCall_B14_800E` loop on `$04ED`), `$030D`=`$06CD`=`$2E`, `ObjPosToTemp` slot 13 → `ObjInit` type `$DE` in slot `$1E` → msg 14.5 ("I am Mesia… I will go to Draygonia now") | `$95` (sets F01B) |
| 8 | `D654` | give item `$42` (Paralysis) | `$B2` (F037) |
| 9 | `D659` | if player not flashing (`obj1_flash` b6) and `$04BE`=0: `ObjInit` type `$38` in slot `$1E`, pos+param from 5-byte records `D6A8` (`x lo,x hi,y lo,y hi,$069E`; 9 records, `$FF` = none) indexed by area `$64`→rec 1, `$68`→rec 2, else rec `area_entry`; SFX `$48` | — |
| 10 | `D63A` | SFX `$6E` only | `$BE` (!F07B) |
| 11 | `D6D8` | give item `$46` (Barrier) | `$84` whirlpool closed |
| 12 | `D6DD` | give item `$47` (Change), then `NpcVanish` slot 14 | — |
| 13 | `D6E7` | give item `$3E` (Bow of Moon) | — |
| 14 | `D6EC` | `AreaLoadA`, `ObjPosToTemp` slot 15 → `ObjInit` type `$DF` in slot `$1E`, `$033E`=`$A0` | — |
| 15 | `D70D` | trigger slot param := `$41`, `GiveItem` (Refresh; clears the trigger) | `$B4` Zebu, `$B5` Tornel |
| 16 | `D63F` | = talk act 8 (re-`ObjInit` slot 14 with `$04CE` unless `$04AE`=8) | — |
| 19 / 20 | `D718` / `D71F` | clear slot 13 / slot 14, SFX `$6B`, `WaitSfxDone`; if slots 13 and 14 are both empty: `$07D7`=`$7F`, `ObjInit` slot 15 type `$04CF` | — |
| 21 | `D746` | SFX `$6B`, `PlaySound(0)` (stop music), `WaitSfxDone`, `ObjInit` type `$9E` in slot 13 | — |
| 22 | `D78C` | `sub_state` = `$1A` | — |
| 23 | `D791` | **wise-men history**: `FarCall_B14_800E`, clear `$0381` b2, then msgs 22.0, 22.2, 22.4, 22.6 in a row (Azteca / Zebu / … backstory) | `$82` (F05F & !F02D) |
| 24 | `D75E` | trigger slot cleared; slots 13+14 both type `$32` followers, `$068D`=`$0B`, `$068E`=`$0A` | `$85` Tornel (F2F7 & !F00E) |
| 25 | `D7B7` | **push the player back**: `obj_y_lo` of slots 0 and 1 += 8 — **quirk**: on carry it does `INC $B0`/`INC $B1` (the low byte again) instead of the high byte `$D0/$D1` | `$86`/`$BA` "you cannot climb this yet", `$8A` "go to Joel" |
| 27 | `D7CE` | trigger slot cleared; every slot 13-14 that is active, not `$32` and has `$05A0`=0 becomes type `$32` follower with `$0680`=slot−11, `obj_anim`=1 | `$8F`, `$BB-$BD` |
| 28 | `D7FD` | give item `$29` (Gas Mask), `MakeFollower13` | — |
| 29 | `D805` | **trigger slot cleared**; only in area `$F2`: `ObjInit` type `$88` in slot 13 at x lo `$80`, y lo `$40` | boss intros `$98 $9A-$A0`, `$B1` — so act 29 is "consume the trigger", not a boss start; the boss itself is already spawned by the area |
| 30 | `D827` | `sub_state` = `$1B` and `PLA PLA` (drops the caller's return, skipping `sub_state`=8) | `$A4` Mesia reunion (F068) — likely the ending |
| 31 | `D82E` | `$04AD` = `$55` (slot 13 becomes type `$55`) | `$B0` (F021) |

  Acts 2/12/13/14/16/19-22/28 are not used by any `14:A17A` record. **Session 18:** they are used by the **item-use results** instead. `ItemUseHook` → `30:D523` `JMP EventActionDispatch` sends the use record's `$22` through this same table. `GiveItem` `30:D458` does the same for `ItemGetHook` results, which is how act 22 (swords → `sub_state` `$1A`) and act 1 (Sword of Thunder warp) run. See "GiveItem / item-use dispatch" below. Object types `$38 $55 $88 $9E $DE` are not traced yet.
- Examples (`tools/flag_sites.py --hook2`): `$84` "you've closed the whirlpool" → F051; `$87-$89` wise-men rescue speeches while !F055/!F056/!F057 (act 26); `$98` Kelbesque intro while !F066, `$9A` Mado while F027&!F067, `$9B-$A0` second boss round / Karmine / Emperor / Draygon while !F05D !F060 !F062 !F05C !F06C !F05E (act 29 = boss start, probably); `$A1-$A3` tower messages F0EA-F0EC; `$A4` Mesia reunion F068; `$B2`/`$B4`/`$B5` magic teaching F037/F039/F03F; many silent triggers just set a flag (F2F5-F2FF, F093-F095, F018, F05B…).

#### Item hooks 0/1 — inventory + item flags (session 16)

`ItemEventHook` `30:D834` hooks 0 and 1 are the **inventory** routines, and both apply set-lists, so **picking up or using an item sets story flags**. **Tool:** `tools/item_dump.py [--out f]` → `gfx/items.txt` (run session 16).

- **Inventory `$6430-$645F`** (WRAM, item ids/values; `$FF` = empty): `6430-33` swords, `6434-37` armor, `6438-3B` shields, `643C-3F` balls/bracelets, `6440-47` consumables, `6448-4F` rings, `6450-57` key items, `6458-5F` magic. `$6428-$642F` = per-category selection (`$FF` = none; static guess). New game fills `$6428-$6477` with `$FF` (`30:C957`).
- **`ItemGetHook` `14:826F`** (hook 0, from `GiveItem` `30:D401`; `$23` = item): `$23` |= `$80` ("failed"), record = `ItemGetTable` `14:9B00`[item] (113 words, ids `00-70`), value `$29` = `ItemStoreValue` `14:9D66`[item]. Record = `start count r0 r1 set-list next`:
  - `InvStoreItem` `14:82A0`: **count bit 7 set** = fixed equip slot `$6430`+start (overwritten, `$23` bit 7 cleared). Value 4 (**Crystalis**) puts 4 in `$6430`, sets `$0711`=5, clears `$6431-$6433` (the other swords) and `$643C-$643F` (balls/bracelets) to `$FF` and sets `$6428`=0. A bracelet (value 6/8/A/C) whose ball index (v−4)/2 equals `$0718` sets `$642B`=`$FF` and `$0718`=0, so the ball is unequipped. **Count bit 7 clear** = put the item in the first `$FF` slot of `$6430`+start..+count (`$23`=0). If every slot is full, `$23` bit 7 stays set, which gives the "too many items" path.
  - On success: `NpcResult` (r0 r1) + `NpcFlagApply` (set-list), then the `next` byte, if bit 7 is clear, is given as a further item (loops to `826F`).
  - Sword records use act 22 (Sword of Thunder uses act 1 = the `EventActionTable` warp). Every other record uses act 0.
  - Ids `$50-$70` store a **base item** (e.g. `$50`/`$52`/`$53` → `$1D` Medical Herb, `$51` → `$12` Sacred Shield + next item `$20` Fruit of Lime). Each sets its own flag, and most base consumables do too: F0AA-F0E7 and F100-F10C are per-pickup **"already collected"** flags (**session 17 correction:** F100-F10C are the **boss-defeated** flags tested by `NpcHidden` for boss params `$C0-$CC`; see "Object update + boss defeat"). **Session 18:** the hook gets the **raw** id; the `$50-$6F` remap only runs after the hook and changes nothing but the displayed name (see "GiveItem / item-use dispatch"). So every raw id `$00-$6F` reaches its own record. Record `$70` (Medical Herb, F0DD) is **unreachable**: `GiveItem` sends id `$70` to the trap-chest path, and no record uses `$70` as its `next` item.
- **`ItemUseHook` `14:831E`** (hook 1, `30:D51C`): handler `ItemUseHandlers` `14:8399`[item] (only 80 words, items `00-4F`; the table ends where the code starts at `8439`), record `ItemUseTable` `14:9BE2`[item], `JMP ($26)` with Y=0. If the handler leaves `$23`≠`$FF`: `NpcResult` + `NpcFlagApply` from the handler's Y, then `InvRemoveItem` `14:8354` (shifts the category down, last slot `$FF`, `$642A`+(X>>3)=`$FF`, `$0715`=0) **unless the item is `$36` Shell Flute** (reusable). Handlers: `UseFail` `84DB` (`$23`=`$FF`, the default for most items); `UseTouchMatch` `8497` (the record's `param b2` must equal the spawn the player touches, `$0643`); area checks; `UseFindSlotObj` `8469` (an object in slots `0D-1F` with that param/b2); consumables (Medical Herb HP+32 `84E0` capped at `$03C0`, Fruit of Power MP+32, Magic Ring MP=max; Antidote / Lysis Plant / Fruit of Lime / Fruit of Repun clear status `obj1_flash`&`0F` = 3 / 1 / 2 / 4); Glowing Lamp `8585` turns a Broken Statue (`$38`) into Statue of Gold (`$3A`) in place; Insect Flute `84B3` (area `$1A`, slot 13 → type `$E2`).
- **Use flags** (from `gfx/items.txt`): Windmill Key in area `$0F` → **F00A** (windmill turns; Zebu's precondition), Flute of Lime on the 4 stone people `$85-$88` → F06A/F069/F06B/F035, Key to Prison → clr F084 + set F00D, Kirisa Plant → F010, Ivory Statue → F065, Love Pendant → F03C, Statue of Onyx → F050, Alarm Flute → F00F/F0A4, Fog Lamp → F021, Shell Flute (areas `$60/$64/$68`, needs F09B) → F054, Statue of Gold → F283+F08F (and `$62F2`=`$FF`), Bows of Moon/Sun → F109/F10A (statues destroyed), Crystalis/Medical Herb on param `$63` → F025.

#### GiveItem / item-use dispatch (session 18)

- **`GiveItem` `30:D3EB` flow**:
  1. Ids ≥`$80` → `30:D539` (event trigger).
  2. Id `$70` → `TrapChest` `30:D3DA`.
  3. Otherwise `ItemEventHook(0)` (= `ItemGetHook`, with the **raw** id in `$23`). On failure (`$23` bit 7 set) → `30:D47C`, "too many items" (only if `$08`&`$0F`=0).
  4. On success → `JSR SwordPowerLevel` `30:D9D8` (session 19, below). Then, for raw ids ≥`$50`, `$07DC` = `ItemRemapTable` `30:D40C`[id]. Ids are always ≥`$50` here, so the bytes actually read are **`30:D45C-D47B`** (32 bytes, ids `$50-$6F`). Id `$70` would read code at `D47C` but never gets here.
  5. SFX `$3F` (or `$93` + music bit 7 for display ids `$40-$4F`), msg 32.12 "You now have {ITEM[$07DC]}".
  6. Clear the source slot (`obj_active`=0), then **`JMP EventActionDispatch` `30:D552`** with the get-record's `$22`.
- **The remap only changes the name shown in the message.** The pickup record, store value and flags all come from the raw id. Remap: `$50/52/53/55-57`→`$1D`, `$51`→`$12`, `$54/58/65/67-69/6B/6C/6F`→`$22`, `$59/5A/61/62/64`→`$21`, `$5B`→`$28`, `$5C/5D`→`$1F`, `$5E-60`→`$1E`, `$63/6D`→`$26`, `$66`→`$23`, `$6A/6E`→`$24`.
- **`TrapChest` `30:D3DA` (item `$70`)**: `ObjInit` turns the chest slot into **object type `$73`** (AI `$2B` `27:AC60`), and `sub_state` becomes 8. No item is given and no message is shown.
  - **Phase 0** (`$0600`=0): metasprite `$AA`. It runs `27:8409` (Y=1) until `$21`=0, then `$0600`++.
  - **Phase 1**: metasprite `$90`. It wanders with a `$0620` timer and turns around (dir+4) when blocked (`27:9A30` with A=`$40`, `$20`&6). In other words, a **chest monster**.
  - Item `$70` is placed in 9 chests (`gfx/spawn_params.txt` `h2 p=$70`: areas `4A 54 6B 85 89 A3`).
- **Item use `30:D497-D536`** (from the item menu; `$0715` = selected entry):
  - `$0A`: only with `$0743`=0 (else `$0743` is cleared and it falls through).
  - `$20`: blocked while `obj1_flash` b7 is set, unless (`flash`&`$30`)=`$20`.
  - `$08`: blocked while `obj1_flash` b7 is set; otherwise opens `SubstTeleportMenu` `30:DB28`. If the area changed, it re-runs this path and then `SetR7($17)`, `JMP 23:BC00` (`SaveGame`), so **teleporting saves the game**.
  - `$0B/$0C/$15` play SFX `$48`.
  - Then `$0563` (param of slot 3) = `$0715`+`$1C`, `$0623`=3, `$08`++, `sub_state`=8, `ItemEventHook(1)`.
  - Success → `JMP EventActionDispatch` with the use-record's `$22`. Failure → `sub_state`=`$10`, pending msg **32.19 "Nothing happens."**, SFX `$4E`.
  - **`$0715` encoding (session 19):** it is the selected item **id − `$1C`** (0 = nothing selected). `SelectedUseItem` `30:DB0E` sets it from the consumable selection (`$642C` ≥0 → `$6440`[`$642C`]), otherwise from the key-item selection (`$642E` → `$6450`[`$642E`]). It is cleared by `InvRemoveItem` `14:8395` and the new-game init `30:C999`. So `$0563` = `$0715`+`$1C` is the real item id. The menu checks decode as: `$08` = **Warp Boots** `$24` (teleport menu), `$0A` = **Opel Statue** `$26`, `$20` = **Kirisa Plant** `$3C`, `$0B/$0C/$15` = **Insect Flute / Flute of Lime / Alarm Flute** `$27/$28/$31` (SFX `$48` = flute sound).
  - **Opel Statue auto-revive:** substate 3 (`SubstateHandlerTable` entry 3 = `19:B900`, bank n=9) is **`PlayerDeath`**. It clears the status bits of `obj1_flash` and the orbit level. If `$0715`=`$0A` (Opel Statue selected) it jumps to `OpelRevive` `19:B9B0`: `$0743`=`$40`, SFX `$68`, HP +1 per step, 4 steps per frame, up to `player_hp_max`; then `$0381` |= 4, `$071A`=`$FF`, `sub_state`=6. **`$0743` = `opel_revive` (session 20):** written only at `19:B9B2` (=`$40`), `30:C9BA` (new game, 0) and `30:D4AE` (cleared). Sub_state 6 is `ItemUseMenu` `30:D497`, so the revive hands over to the use path with `$0743`≠0: `$0743` is cleared and the statue goes through `ItemEventHook(1)` → consumed. A **manual** use of the Opel Statue (`$0743`=0) is refused: `$08`++, `sub_state`=8 (`SubstPlay` `30:CB62`, the normal gameplay frame) with no message — the statue only works by dying with it selected. **`PlayerUpdate` `26:9B45`** — this is the **player object's AI** (`ObjAiTable` id `$01`, object type `$04`; also called directly from `30:DA43`/`DAC9` with X=1). Its first check sets `sub_state` 8→6 when `pad_new` `$4B` b6 (**B newly pressed**, see "Controller input") and `use_item_sel`≠0: **B uses the selected item** (confirmed from the pad bit layout). It then runs `26:9BF6` (A = `obj_flags2`&`$0F`), `26:9CEF`, `26:A1C9`, and the status visuals: `obj1_flash`&`$0F`=4 → metasprite `$53`; `obj1_flash` b6 → 4-frame cycle `26:9B41`[`$0301`&3] (`88 C2 8A EF`). Entry point for "gameplay 1/3".
- **`SwordPowerLevel` `30:D9D8` (session 19)** returns Y = sword power level 0/1/2, which is stored in **`$0719`** (`30:D8FD`; cleared at new game `30:C9B1`). It is called after every successful pickup (`30:D408`) and when the equip menu closes (`30:D8FA`, after `SelectedUseItem`). Inputs: `$0711` = equipped sword 1-4 (5 = Crystalis), `$0718` = equipped ball/bracelet (0 = none, 1-4 = ball of element n, 5-8 = bracelet of element n−4).
  - No sword → 0.
  - `$0718`≠0: if (`$0718`−1)&3 ≠ `$0711`−1 (the element doesn't match the sword) → 0. Otherwise (`$0718`−1)&`$0C` ≠0 → **2 (bracelet)**, else **1 (ball)**.
  - `$0718`=0: Y=`$0711`, read `$643B`+Y (the `$643C-$643F` ball/bracelet slot of the sword's element). `$FF` or an odd value → 0, even value → 1. (Crystalis, Y=5, reads `$6440`, the first consumable slot; static quirk, not checked in the emulator.) **Session 20:** so a ball slot holding a **bracelet** (even ids `06/08/0A/0C`) gives level 1 even when nothing is equipped; a ball (odd `05/07/09/0B`) gives 0. **Crystalis pickup** (`ItemGetHook` special case `14:82B1-82DD`, id 4): `$6430`=4, `equip_sword`=5, `$6431-$6433` and **all four ball/bracelet slots `$643C-$643F` = `$FF`**, `inv_select` `$6428`=0; `$0718` is **not** cleared. Consequences (static): with a stale `$0718`≠0, (`$0718`−1)&3 can never equal 4 → level 0; with `$0718`=0 the level is the parity of the first consumable id (`$6440`): Medical Herb `$1D` / Lysis `$1F` / Fruit of Power `$21` / Fruit of Repun `$23` / empty → 0, Antidote `$1E` / Fruit of Lime `$20` / Magic Ring `$22` / Warp Boots `$24` / Opel `$26` → 1. The charge-cap path `26:9E03-9E36` has no Crystalis special case, so the Crystalis charge stage depends on that slot. Whether other code (`26:9C56-9CC8` read `$0711`) overrides it is still open.
  - Readers: `26:9E2B` clamps `orbit_level` `$06C0` to `SwordChargeCap` `26:9D7E` = `08 10 18`[`$0719`], so level 0/1/2 allows charge stage 1/2/3. `26:8D7A` draws the **HUD charge meter**: index = `$0719`·8 | (`$08`&4)/4 (blink when charging) | (`orbit_level`&`$18`)/4 → `26:8DDD`[index] → offset into 9-byte tile rows at `26:8DF5`, copied to the HUD buffer `$6067-$606F`.
- **`OamClipTop` `30:C17D` (session 19)**: hides every OAM sprite 1-63 (`$0204+`) whose Y ≤ `$58` − (`$04`&7) by setting Y=`$F0`. It keeps sprites out of the top window area during text/cutscenes. Callers: `19:BBE4/BC3F/BC5D` (`SwordRaisePose`), `23:BBE4` (the same code in a stale copy), `30:D376/D394/D3B0` (message display), `30:D8B7` (equip menu), `30:DBC2`, `30:DE07`.
- **Use-result acts** (`gfx/items.txt`, all run through `EventActionTable`):
  - 2: Crystalis/Medical Herb on param `$63` → slot 13 item + follower
  - 3/4/5: no-op
  - 6: reload area (stone people, flutes)
  - 9: Shell Flute → type `$38` at a per-area position
  - 10: Statue of Gold → SFX `$6E`
  - 12: Love Pendant → Change
  - 13: Kirisa Plant → Bow of Moon
  - 14: Ivory Statue → FX in slot `$1E`
  - 16: prison key / `$E4` secret door → re-init slot 14
  - 19/20: Bows of Moon/Sun → clear slot 13/14
  - 21: Bow of Truth → type `$9E` in slot 13
  - 28: Statue of Onyx → Gas Mask + follower
- **`SwordRaisePose` `19:BC04`** (only caller: event act 1 `30:D5B4`, Sword of Thunder). A short cutscene where the player raises the sword:
  1. `JSR 30:C867`, `WaitFrame`.
  2. Player sprite CHR: `chr_r2_play`=`$5C`, `chr_r3_play`=`$42`; `orbit_level`=0.
  3. Save `$0301` (slot 1 metasprite) and `$0581`, then set them to `$B8` (the raise pose) and `$4C`.
  4. Play jingle `$93` with music bit 7 set (`snd_cur_music` |= `$80`, interrupt mode).
  5. `30:C17D`(A=`$F0`), then a 127-frame loop (`$04E1`=`$7F`) of `WaitFrame` + `FarCall_B14_800E` (sprite pass).
  6. Restore `$0301`/`$0581`, `JSR 30:C008` (`SetR6($1A)` helper), `30:C17D`(`$F0`), sprite pass, `sub_state`=8.

  Bytes `18:BC04+` are a nametable/tile block (`10 10 … F1 F4 F5 F1`), not code. The old `18:BC04` notes were a bank-numbering slip.

**Boss gates (task "text follow-ups 5" (a), session 16):** four are set by **picking up the boss's reward**:
- F05F by Sword of Thunder `$03`,
- **F067** (Mado 1) by Ball of Thunder `$0B`,
- **F062** (Mado 2) by item `$51` (Sacred Shield variant),
- **F06C** (Emperor) by Psycho Armor `$1C`.

**Resolved session 17 (see "Boss defeat" below): F05C/F05D/F05E/F060/F066 are never set, and they don't need to be.** They are tested only by event triggers `$98 $9B-$A0`, and no area record or code places those triggers (`area_dump.py params`: only `$9A` at `F2.12` and `$B1` at `A0` are placed). Each boss AI shows its own intro message (e.g. `15:A99F`). The real "boss defeated" flags are **F100-F10C**, tested by `NpcHidden` and set by picking up the reward. F062/F067/F06C are set as a side effect of the reward pickup; only `$9A` (F067, Mado 1 in area `$F2`) is a live gate. Session 16 text kept for the record: **F05C (Karmine), F05D (Kelbesque 2), F05E (Draygon), F066 (Kelbesque 1) still have no set site.** F060 (Sabera 2) is set only by NPC params `$CB/$D0/$D5/$DA`, which have bogus scripts. What was checked: every `NpcFlagApply` caller (`14:806D` NPC B, `80E3` hook 2, `8294` hook 0, `8344` hook 1; `13:AB4A` is data), all set-list byte pairs `40 xx`/`00 xx` for these flags in banks 14/15 (only `14:9737`, not inside a set-list), indexed writes `9D/99 xx 64` in the code banks (only the known sites plus `30:C95B` inventory init), and the 3 callers of `HitSetFlag` (`30:CCF7` calls it with `$12`=0 to **clear** the hit flag when the `$05A0` timer runs out; the `26:8E05/8E20` hits are text). Likely remaining paths: (1) the gate is never set and the trigger is consumed instead (act 29 clears the trigger slot, and these bosses never come back); (2) a `(zp),Y` write with a pointer built at run time; (3) the `$12`=`$FF` write path inside the other hit handlers (`26:8FBB` table).

#### Object update + boss defeat (session 17)

**`ObjUpdateAll` `30:CCCC`** (the per-frame object loop): `SwitchBank16($0D)` maps banks 26/27. For slots X = 0-30:
- If `$0340` b4-7 ≠ 0, run the hit-stun/knockback path `30:CD31` (`26:98D7`).
- If the `$05A0` timer reaches 0 on a `b2`=1 object, `HitSetFlag` is called with `$12`=0 (clears the hit flag; see session 16).
- Handler = **`ObjAiTable` `27:A314`**[(`obj_active` & `$7F`)·2]; 0 = no handler. AI ids `$60-$6F` (incl. `$E0-$EF`) also `SetR7($0F)`, so **their handlers are in bank 15 at `$A000`**, with bank 26 at `$8000`. AI id `$1A` is special (`30:CD7F`, palette/IRQ sequence, `$0600` counts to `$6A`).
- **`obj_active` `$04A0` = AI id**: bit 7 is kept but ignored by the dispatch. `ObjInit` sets it from the type record. `$04C0` = **death type**.

**AI handlers of the story/event object types (session 20, static):**

| type | AI id → handler | behaviour |
|---|---|---|
| `$04` | `$01` → `26:9B45` `PlayerUpdate` | the player (slot 1); see "GiveItem / item-use dispatch" for the B-button item use |
| `$38` | `$32` → `27:AE31` `AiPathWalk` (only user) | **scripted path walker**. Moves one step on the frames where `BitMask`[`$08`&7] & `$0600` ≠ 0 (`$0600` = speed bit pattern); every 16 steps (`obj_anim`&`$0F`=0) reads the next byte of path `AiPathTable` `27:AF04`[`$0680`] (16 paths, data `AF24-AFA5`) at index `$0620`. Byte 0-7 = direction (also sets the metasprite's low 2 bits from dir>>1). Terminators: **`$FC`** → slots 13/14 become path walkers too (`$04AD/$04AE`=`$32`, path `$0D`, step 0, `obj_anim`=1); **`$FD`** → slot 0 AI=`$0A`, self AI=`$30` (talkable NPC `27:ACD9`), `$06E0`=6, `$06C0`=`$2E`; **`$FE`** → despawn (`obj_active`=0); other `$80+` → AI `$30` (stops and becomes talkable). Area `$D4` special case: slot 13 on path `$0D` drags slot 0 and slot 1 (player) along (`x`+16, same y) and zeroes their `$0620/$0640/$06C0`. Type `$38` is what the Shell Flute summons (use act 9, records `30:D6A8`) |
| `$DE` | `$FF` → `27:A419` = `RTS` | static: no AI at all (talk-only object, e.g. msg 14.5 in slot `$1E`) |
| `$DF` | `$14` → `27:B2C5` (shared with `$13 $17 $1B $C0 $D5 $D7`) | countdown: `obj_anim` −= `$0600` each frame; on borrow → `27:98C7` (end/next phase). `snd`=`$39` |
| `$55` | `$40` → `27:B39E` (also `$7C`) | wanderer: `27:B769` first; when `$0600` hits 0 it reloads `$D0` and picks a new direction = (`$20`·2 + `27:B41E`[(x+y+`$0480`)&3]) & `$0F` (16 directions, pseudo-random from position), then `$0600`−− |
| `$88` | `$E7` → `15:ADD4` | boss body: `$0600`≠0 → `$07D7`=4; `26:9B07`(A=`$C0`); **`BossPhaseDispatch` `15:A400`** with A=`$1B` |
| `$9E` | `$EB` → `15:B2DA` | Bow of Truth result (slot 13): `26:9B07`(A=`$C7`); `$0600`≠0 → `$07D7`=`$10` and IRQ mode `$51`=7 (unless already 2); `BossPhaseDispatch` with A=`$2B` |

- **`BossPhaseDispatch` `15:A400`**: `JMP (BossPhaseTable 15:A413[(A + $0600,X)·2])` — A is the boss's base index and `$0600` its phase, so each boss owns a run of consecutive phase handlers in one shared table.
- `26:9B07` stores A in `$10` and, while `obj_flags2` b7 is clear, `JMP PaletteBuild` — the A value (`$C0`/`$C7`) is a palette argument (not followed further).
- Still open: `$07D7` (set to 4 / `$10` by boss phases ≠0), the generic AIs `$10` (`27:B0B5`, types `$32 $E2` + 26 others), `$20` (`27:A915`, `$53` + 9 others) and `$28` (`27:AB61`, `$54 $67`).

**Tool:** `tools/objtype_dump.py [types] [--ai] [--out f]` decodes every `ObjTypeTable` `13:AC00` record (groups of mask + bytes → columns `$0300+g·$100+b·$20`) and the AI table. Run → `gfx/objtypes.txt`, `gfx/obj_ai.txt`.

**Enemy death `EnemyKilled` `26:9152`** (from hit handler 0 when HP `$03C0,Y` underflows):
- `AddXp` `26:924B`: `$0520` xp value (bit 7 → (v&`$7F`)·16) added to **XP `$0704/$0705`**.
- `$04C0`=0 → `EnemyDeathFx` `26:91FA` (AI `$7D` puff). Otherwise `ObjInit(slot, $04C0)` (the slot turns into its death type).
- **Level up** when XP ≥ `$0706/$0707` and level < 16: XP reset to 0, `player_level`+1. Max HP = `LevelHpTable` `26:8B7F`[lv], max MP = `LevelMpTable` `26:8B8F`[lv]. HP and MP rise by the table difference. The next XP is `LevelXpTable` `26:8B9E`[lv·2]. HUD redraw, `sub_state`=`$10`, msg 32.13.

**Boss chain:** the boss types have `$04C0`=`$DC` (or `$CF` directly for `$68`/`$88`; `$D4` for statues `$9C/$9D`).
- `$DC` → `$DD` are explosions (AI `$74` `ObjAiExplode` `27:B83B`, `ObjBecomeNext` `26:98CD`).
- `$DD` → **`$CF` = `BossDefeated` `15:B807`** (AI `$6F`). It turns the slot into type `$0F` (**the reward drop**) at x lo `$80`, y lo `$90`, finds the area in `BossAreaList` `15:B95D` (index → `$0600`), and reads a 21-byte record from `BossRecPtrs` `15:B96B`:

```
+0  ptr16  code hook (0 = none; called via the $10 JMP trampoline)
+2  ?      (always 00)
+3  music  -> snd_cur_music $0102 (resumed area music)
+4  param  -> obj_spawn_param of the drop = the REWARD ITEM
+5  8 x pal id -> $07E0-$07E7 (0 = keep)
+13 6 x CHR  -> $07F0-$07F5 (0 = keep)
+19 $62FE
+20 !=0 -> every enemy in slots $0E-$1E (except AI $78/$7F/$30/$39/$70) becomes type $D5
```

| Area | Spawn p | Boss type / AI | Hook | Reward (+4) | Defeated flag (presence) |
|---|---|---|---|---|---|
| `0A` | `$C0` | `$57` / `$E0` | — | `$2E` Rabbit Boots | F100 |
| `1A` | `$C1` | `$5E` / `$7F` | `B8CF` (fills `$6000`, slots `$0E-$13` → AI `$73`) | `$07` Ball of Fire | F101 |
| `28` | `$C2` | `$68` / `$E3` | `BA98` flee speech 6.1 | `$08` Flame Bracelet | F102 |
| `43` | `$C3` | `$70` / `$E5` | (not in list; `BossAreaList` slot 3 = `$FF`) | — | F103 (Ball of Water sets it) |
| `6E` | — | — | `BA98` flee speech 16.19 | `$38` Broken Statue | — |
| `F2` | (trigger `$9A`, act 29 → type `$88`) | `$88` / `$E7` | `BA98` flee speech 25.5 | `$0B` Ball of Thunder | F067 (trigger gate) |
| `A9` | `$C5` | `$8B` / `$E3` | — | `$26` Opel Statue | F105 |
| `AC` | `$C6` | `$90` / `$E6` | — | `$23` Fruit of Repun | F106 |
| `B9` | `$C7` | `$93` / `$E7` | — | `$12` Sacred Shield | F107 |
| `B6` | `$C8` | `$97` / `$E8` | — | `$3D` Ivory Statue | F108 |
| `A0` | `$C9`/`$CA` | `$9C`/`$9D` / `$E9` (statues, death type `$D4`) | — | — | F109/F10A (set by using Bow of Moon/Sun) |
| `9F` | `$CB` | `$9B` / `$EA` | `B8EE` (spawns type `$B2` in slot `$1E`) | `$1C` Psycho Armor | F10B |
| `A6` | `$CB` | `$9B` / `$EA` | `B8FD` (clears slots `$0D-$13`, camera from `$3C`, slot `$14` = `$04D4`, slot `$15` = `$B3`) | `$FF` none | F10B |
| `6C` | `$CC` | `$A5` / `$E0` | — | `$61` (→ Fruit of Power) | F10C |
| `5F` | — | — | `B947` (slots `$0D-$0F` cleared, slot `$14` = type `$B5`) | `$00`, music `$FF` | — |

Boss names come from the intro/flee messages; they are inferred, not confirmed.
- **`BossFleeSpeech` `15:BA98`** (areas `28`/`6E`/`F2`) does the following, in order:
  - waits `$20` frames, then clears `obj1_flash`, `$0600`, `$0620` and `orbit_level`;
  - plays sound `$80` and puts type `$2B` in slot `$1F` at the boss position;
  - picks the record from **`BossFleeRecPtrs` `15:BAFE`** (indexed by boss record index, so this is the "`15:BAFE` event records" item). It uses 5-byte records `?, $031F, $033F, msg id, group`: `BB1A` = 6.1, `BB1F` = 16.19, `BB24` = 25.5. Those are the "Next time…" / "Mesia is still mine!" / "we still have Mesia" lines;
  - runs `ClearEnemySlots` `15:BB29`, plays SFX `$29`, shows the message, and finishes with the fly-off loop at `15:BB5B`.
- **Presence = boss-defeated flag.** The direct-type spawn path (`31:E363`) also runs `NpcHidden` (`31:E34B`). The lists `NpcPresenceHi` `14:86E0`[p−`$80`] for `$C0-$CC` are `area, !F100+i`, so each boss spawns only while its F10x flag is clear. The reward pickup sets that flag (item set-lists, `gfx/items.txt`). **Correction to session 16:** F100-F10C are not "already collected" pickup flags; they are the **boss-defeated** flags. The other per-item flags (F0AA-F0E7) may still be pickup flags. **Tool:** `tools/presence_dump.py [params] [--out f]` → `gfx/presence.txt` (all 187 params with lists). The list format has bit 7 of the cond's b0 = **last** cond, the opposite of NPC script conds.
- **Not covered:** the bank 15 boss AI state machines (`15:A477 A95D ABDA AC7B ADD4 AF48 B0B6 B130 B2DA`). Each shows its own intro (`pending_msg` writes at `15:A9A1 ACED AE2D AF91 B0EC B195`, music `$12`); that belongs to task "gameplay 3/3". The area `$43` boss (type `$70`, AI `$E5`) has no `BossRecs` entry, so how its reward is given is not traced.

#### Story flags (session 14)

`tools/flag_sites.py [--hook2] [--out f]` cross-references every data-driven flag site: NPC script conds + set-lists, event triggers (`14:A17A`) and telepathy hints. Run → `gfx/flag_sites.txt`. Code that touches `$6480+` directly is listed separately by `tools/flag_code_sites.py` (session 15, below). The NPC side also lists flags from bogus scripts (params without valid messages), so the count of 453 is too high. Names below are **inferred from the message text** at the set/test sites (static RE only):

| Flag | Byte&mask | Name (evidence) |
|---|---|---|
| F000 | `$6480&01` | never set: `!F000` = always |
| F00A | `$6481&04` | precondition for Zebu's teaching (NPC `$5E` B+0F, trigger `$B4`) |
| F010 | `$6482&01` | Kirisa Plant step (Asina hint 29.4 until set; set by an NPC after "Did you visit the elder's house yet?") |
| F013 | `$6482&08` | "enemies you can't see" step (Tornel hint 28.25); trigger `$96` needs it |
| F01E | `$6483&40` | Recover taught / fortune-teller truth told (NPC 11.0, act 16) |
| F027 | `$6484&80` | Shyron attacked (trigger `$80` 29.19, needs F05F) |
| F028-F02B | `$6485&01-08` | player status effects (session 12) |
| F02E | `$6485&40` | Deo's gift received (NPC 26.16 act 3; Tornel hint 28.31 "look for a rabbit") |
| F037 / F039 / F03F | | magic taught by trigger `$B2` (28.16) / Zebu (0.27) / Tornel (5.11) |
| F041 | `$6488&02` | Oak people's request done (Tornel hint 28.19) |
| F051 | `$648A&02` | whirlpool closed (trigger `$84`) |
| F055 / F056 / F057 | `$648A&20/40/80` | Zebu / Tornel / Asina rescued (msgs 24.1/24.3/24.5, full restore) |
| F05C F05D F05E F060 F062 F066 F067 F06C | | boss gates: Karmine, Kelbesque 2, Draygon (final), Sabera 2, Mado 2, Kelbesque 1, Mado 1, Emperor (trigger shows the intro while clear). Session 16: F067/F062/F06C (and F05F) are set by picking up the boss reward (`ItemGetHook`, see "Item hooks 0/1"); F05C/F05D/F05E/F066 have no set site found. **Session 17: F05C/F05D/F05E/F060/F066 are never set; their triggers `$98 $9B-$A0` are never placed (dead data). The live boss gates are F100-F10C** |
| F100-F10C | `$64A0-$64A1` | **boss defeated** (session 17): boss spawn params `$C0-$CC` are absent once set; set by picking up the boss reward (F100 Rabbit Boots … F10C item `$61`), F109/F10A by using the Bows of Moon/Sun on the statues |
| F063 | `$648C&08` | Kensu found after his change of form (hints 28.26/29.7/29.22) |
| F065 | `$648C&20` | Kensu taught the last spell (Tornel hint 28.30) |
| F068 | `$648D&01` | reunited with Mesia (trigger `$A4`) |
| F075 | `$648E&20` | met Kensu at the lighthouse (NPC 15.24 sets it; Zebu hint 28.5 "special glasses") |
| F077 | `$648E&80` | Flame Bracelet obtained (Zebu hint 28.2) |
| F079 / F082 | `$648F&02` / `$6490&04` | Azteca's gift (NPC 27.1 act 3; hint 29.12 "both Bows") |
| F07B | `$648F&08` | Shield Ring bought (Tornel hint 28.22) |
| F07C | `$648F&10` | Warrior Ring (Tornel hint 28.28) |
| F07D | `$648F&20` | Mt.Hydra cave (hint 29.11) |
| F08B | `$6491&08` | Shell Flute (Asina hint 29.5) |
| F0EA-F0EC | `$649D&04-10` | tower messages read (triggers `$A1-$A3`) |
| F2F5-F2FF | `$64DE-$64DF` | silent one-shot triggers (`$99 $A6-$AC $8B $8E $90`) |

**Code that touches the flags directly (session 15)** — `tools/flag_code_sites.py [--all] [--out f]` scans every bank for abs/abs,X/abs,Y operands in `$6480-$657F` (sync-checked; by default it drops hits followed by `.db`/illegal opcodes, which removes most of the ASCII text in banks 0-23). Run → `gfx/flag_code_sites.txt` (69 sites; the ones in banks 0-13/16/19-23 are still text). The real code sites:

| Site | What it does |
|---|---|
| `14:8119-813D` | NPC script VM set/clear/test (`NpcFlagApply`, session 12) |
| `23:BCE2`, `31:F3A7` | load / cold-init copy `$7E00`/`$7F00` → `$6480`/`$6580` (save module) |
| `30:C964` | clear `$6480-$657F` (new game) |
| `30:C063-C07F` | status effects: keeps `$6485` & `$F0` (F02C-F02F), recomputes the low nibble (F028-F02B, session 12) |
| `30:C798`, `31:E717` | `$64C0+` = k4 visited-screen flags (area follow-ups 2), not story flags |
| **`26:9011` `HitSetFlag`** | **object-hit flag setter**: in the hit handler `26:8FC4` (entry 3 of the 4-word hit table `26:8FBB`; Y = object hit), `$13` = `obj_spawn_param`,Y, `$05A0`,Y=`$FF`. If `obj_spawn_b2`,Y = 1 and the param is in the 20-byte immune list `26:9058` (`7E 74 38 39 73 83 6E 76 4E 63 69 70 71 72 68 6B 75 8E 27 26`), `$05A0`,Y=0 and nothing else happens. Otherwise the param is looked up in the 0-terminated list `26:9045` = `26 27 5E 5F 62 6D 6C 34 25` and flag `26:904F`[i] is **set** (byte = id>>3, so only F000-F0FF): param `$26`→F058, `$27`→F059, `$5E`→F06D, `$5F`→F06E, `$62`→F06F, `$6D`→F070, `$6C`→F071, `$34`→F0A1, `$25`→F099. These are the "destroyed/defeated" flags that persist a kill; F0A1 is cleared again by trigger `$90`, F099 is tested by trigger `$BC`. Which objects carry those params is part (c). |
| `30:DD58` `TestFlagF2F0` | A+5 → flag F2F0+A+5 test (`$64DE`,X & `BitMask`,Y); callers `30:DC82`, `30:DD46` |
| `31:E7C4-E829` | area hook: in area `$1C`, if F053 set and F045 clear → **set F045** and slot 14 becomes a type `$32` follower with `$0680`=`$0C`; otherwise **clear F053**; area `$CE` → `E822` tests F027 (Shyron attacked). Matches trigger `$8F` (F053 → set F045 / clr F053). So F053 = "follower `$0C` is waiting", F045 = "follower `$0C` has joined". |
| `31:E269` | spawn handler flag test (`NpcHidden` path, session 13) |
| `27:A74F`/`A7A3` | `$657F` (last byte of the per-NPC state block) used as a counter (`INC`, then ×2+6) — not a story flag |

**No direct writes to the boss-gate flags** (F05C-F06C) were found: they are not set by `STA $648B`-style code. Since `HitSetFlag` only reaches F058/F059/F06D-F071/F099/F0A1, the boss gates must be set through the NPC/trigger set-lists or through an indexed path that this scan cannot resolve (e.g. `26:903A` with another caller, or a `$6480,Y` write with a computed Y). Open question for (b). **Session 16:** partly answered: the item set-lists (hooks 0/1) set F05F/F062/F067/F06C. See "Item hooks 0/1" for what was checked and what is still open.

#### Text window + font (session 12)

- **PPU upload queue** `TextQueueUpload` `20:8836`: with NMI disabled (`20:8871`/`8869` clear/set PPUCTRL b7), append a 4-byte entry at `$6200`+`$0B`: `PPU hi ($21), PPU lo ($20), length ($24), source offset in page $60 ($25)`; `$0B` = (`$0B`+4)&`$1F` → **8-entry ring `$6200-$621F`**, write index `$0B`, read index `$0A` (NMI consumer). Callers spin on `$0A`≠`$0B` until the queue drains.
- **Param blocks** (16 bytes → `$20-$2F`): `20:8879` (window open) = `00 28 40 00 40 00 …` → dest `$2800`, row stride `$22/$23`=`$0040`, len `$40`, src 0; `20:8889` (print) = `20 28 00 00 40 00 …` → dest `$2820`, len `$40`.
- **`TextWindowOpen` `20:8500`**: load `20:8879`, `TextDrawFrame` `20:87B7`, wait for the queue, fill `$6200-$621F` with `$FF`. `87B7` calls `TextFrameRows` `20:87EF` 5× (X = 0,3,3,3,6) — each builds 2 rows of 32 bytes in `$6000` from the 3-byte templates `20:8899` (`left fill×29 right`, every row starts with tile `$FE`), queues them to `$20/$21` and adds the stride. Templates: `18 19 1A` (top), `1E 20 1F` (sides), `1B 1C 1D` (bottom). Result: **nametable 2 rows 0-9** (`$2800-$293F`) = top border, 8 text rows, bottom border. Then `$6001-$6041` = `$FF` → 64 bytes to `$2BC0` (attribute table of NT2: palette 3 everywhere).
- **Font** (`tools/font_dump.py [R0 R1] [--out] [--scale]`, run session 12 → `gfx/font.png`): with `ShowMessage`'s R0=`$3C`/R1=`$38`, **BG tile id = ASCII code** for `$20-$7E` (A-Z `$41-$5A`, a-z `$61-$7A`, digits also at `$00-$09`/`$30-$39`); `$18-$1D` frame corners/edges, `$1E/$1F` left/right border, `$40` = ▼ page prompt (`TextPageBreak` draws it at `$2910`), `$FE` = blank (R1 bank `$39`). R1 `$38-$39` (tiles `$80-$FF`) holds the HUD set (`LIFE FORCE`, bars, `EX:`, `$`, `LV:`), so `TextPrint` writes raw ASCII bytes into the nametable with no translation table.

#### Menu strings — bank 16 `MenuPrint` `16:8933` (session 14)

The bank 16 menu (shops, inn, pawn shop, inventory drop/use, save/load) does not use `TextPrint`. It has its own printer:
- **`MenuPrint` `16:8933`** (A = string id → `$17`; 9+ callers in `16:81A4-8246` and others): `JSR $C676`, then clears `$22-$25`/`$28`. The 4-byte record at **`MenuStrings` `16:8C1B`**`[id]` = `PPU lo, PPU hi` → `$20/$21`, `string ptr` → `$10/$11`. Each string byte is stored as-is (BG tile id) to `$6000[$28]` by `MenuPutc` `16:8A02` (`$24` = length, `$28` = cursor). On `FF` it runs `JSR $C676`, `JMP 16:8AB0`, which queues one entry `($21, $20, $24, $25)` in the PPU ring `$6200` (same format as `TextQueueUpload`) and waits via `$C67D` while rendering is on (`$01`&`$18`).
- **Escape bytes:**

| Byte | Handler | Meaning |
|---|---|---|
| `FF` | `898E` | end + upload |
| `FE s` | `8994` | 6 chars of the **save-slot name** from `$7118` (s even) / `$7718` (s odd), i.e. offset `$18` of the slot image = WRAM `$6418` (6-char name-entry buffer written by bank 19 `19:A3E7/A45F/A552`; the dialogue code 04 uses the zero-terminated copy at `$6400`) |
| `FD s` | `89B8` | **save-slot level**: `$71F7` (s=0) / `$77F7` (s≠0) as 2 digits from `16:89E0` (`"00"`…`"16"`, 17 pairs). Offset `$F7` of the slot image = `$7DF7` = `$0421` (`SavePlayerState`) → this **confirms `$0421` = player level, range 0-16** |
| `FC` | `8A0D` | item name `MenuItemNames` `16:9086`[`$19`] (`$FF` → `$49`), `FF`-terminated; same 74-entry order as `20:8A5C` |

- **Save slots** (`16:9CE0`, A = descriptor, table `16:9D32`: `src, dst` words): 0 `$7100→$7D00`, 1 `$7400→$7D00`, 2 `$7700→$7D00`, 3 `$7A00→$7D00`, 4-7 the reverse. Each slot image is `$300` bytes (`$7D00-$7FFF`). So **slot 0 = `$7100` + backup `$7400`, slot 1 = `$7700` + backup `$7A00`**. `MenuSave` uses 4,5 (slot 0) / 6,7 (slot 1).
- **The menu font is not pure ASCII**: letters/digits/punctuation are ASCII tile ids, but `0A 0B 0C` = "Sword", `7B 11 12` = "Armor", `0D 0E 0F` = "Shield", `5C 5D` = " of " (e.g. item 0 `0A 0B 0C 5C 5D Wind` = "Sword of Wind"), `3C 3D 3E 5B` after spell-ball names (item 6 "Tornado <=>[") is a 4-tile glyph, likely "Bracelet". Record 1E's raw text is `You Durchad` (record 2E has `You purchased`). In the menu CHR, `D`/`c`/`d` may be glyph tiles, so the dump shows the bytes as-is.
- **Records** (59, `16:8C1B-8D06`; full list from `tools/menu_dump.py --items --out gfx/menu_strings.txt`, run session 14): `00-05` equipment-category headings (Sword/Armor/Shield/…) at `$28E3-$2A03`, `07` "Do you wish to drop?", `08` "You cannot drop!", `09/0A` "Now using {item}.", `0B` "Sell {item}?", `0C` "Anything else?", `0D` "I won't buy that.", `0E/29` "YES NO", `0F/28/2A/2B` 64-byte line clear, `14/15/38` "1:/2:{name} LV:{lv}", `16` "Cannot save!", `18-1B` shop titles ARMOR SHOP / TOOL SHOP / INN / PAWN SHOP, `1C` "Do you wish to buy?", `20/26/2C` "Please come again.", `21` "You have too many already.", `22/2F` "You haven't enough money.", `23` "Need to rest? Please stay.", `24` "It will cost you $", `25` "How do feel today?" (sic), `27` "Is there anything you wish to sell?", `2E` "You purchased {item}.", `36` "Load this game?", `37` "Save this game?", `39/3A` "GAME:{name} LV:{lv}". `MenuSave`'s ids (`$0F`,`$16`,`$38`,`$37`,`$0E`) match: clear / "Cannot save!" / slot line / "Save this game?" / YES NO.

**Tool:** `tools/text_dump.py [dict|tables|msgs|all|at BB:AAAA [n]] [--json f] [--out f]`. Run session 11 → `gfx/text.txt` + `gfx/text.json`: **511 messages in 34 groups** (group sizes 1-32), no unknown control codes, and every message ends in `00`. Some pointers are shared, e.g. 0.2/0.5/0.8 → `21:A036`.

### Save / load module, bank 23 `$BC00` (session 11)

Bank 23 `$BC00-$BEFF` is code, not text. Jump table: `BC00`→`SaveGame` `BC09`, `BC03`→`LoadGame` `BC8E`, `BC06`→`LoadGameAndInit` `BCDA`. Bank 23 is mapped at `$A000` by `SetR7($17)` at **7 sites** (not one): `30:C9DF` (state-5 entry `C9DA` → `BC06`), `30:D4EC` (`JMP $BC00`), `30:D8DF` (together with `SetR6($10)`, then `JSR $8000` = bank 16 menu, which calls `BC00` at `16:9C4B/9C64` and `BC03` at `16:9BC6/9BD6`), `31:E500/E535` (`BC00`), `31:F0B0/F1DC` (boot: compares the WRAM copies `$7100-73FF` vs `$7400-76FF` and the `AMI` tag at `$71FD`; this is a second save layout, see the Ph5 save task).

- **`SaveGame`**: saves `$10-$2F` → `$6000` and keeps `$0710` hi nibble only. `SaveAllowed` `BDF6` (C = bit 7 of `23:BF00`[`$6C`]) decides what happens:
  - **Save allowed:** zero `$7D00-$7FFF`, then `SavePlayerState` `BD1F`: `$7DF0`=`$6C` area, `$7DF1-F4` = player x lo/hi, y lo/hi (`$70/$90/$B0/$D0`), `$7DF5/F6`=`$03C0/$03C1`, `$7DF7`=`$0421`, `$7DF8`=`$0620`, and the tag `$7DFD-$7DFF` = `41 4D 49`. Then 5 block copies from descriptors `23:BDC0` (6 bytes: src, dst, count lo/hi): `$6400→$7D00` (128), `$6480→$7E00`, `$0702→$7D80`, `$07DC→$7DB0`, `$7D00→$6D00`. Then `SaveChecksum` `BD92`: an 8-bit sum of `$7D00-$7FFF`, stored in `$70F5` (copy `$70F4`).
  - **Both paths:** copy `$7D00-$7FFF` → `$6D00-$6FFF` (backup mirror), then restore `$10-$2F`.
- **Copy-length quirk** (`BlockCopy` `BCF7`): the count loop copies **lo + 1 + hi** bytes, not the 16-bit count + 1. For example `FF 01` → 257, `FF 02` → 258, `7F 00` → 128, `1C 00` → 29, `23 00` → 36. The port must copy exactly these lengths.
- **`LoadGame`**: 4 reverse descriptors `23:BDDE` (`$7D00→$6400`, `$7E00→$6480`, `$7D80→$0702`, `$7DB0→$07DC`), then `LoadPlayerState` `BD60` (inverse of `BD1F`). No checksum check here.
- **`LoadGameAndInit`**: `LoadGame`, then copy `$7E00→$6480` and `$7F00→$6580` (512 bytes, overwriting part of the `$6480` block), `$6D`=`$FF`, `$41`=1.
- `23:BF00` `AreaSaveFlags` (256 bytes, one per area `$6C`; only 3 readers: `23:BDF8`, `16:9BE8`, and the dead copy `31:FDF8`): **bit 7** = `SaveGame` writes the record (`SaveAllowed` `BDF6`); **bit 0** = the **menu Save command is allowed here** (session 12). `MenuSave` `16:9BE1` (bank 16 menu, bank 23 at `$A000`): `$7000`=`$FB`; if bit 0 = 0 → menu strings `$0F`,`$16` via `16:8933` (+ SFX `$4E` via `16:8A9F`), wait 48 frames, `$4B`=0, `$7000`=`$AB`, exit. If bit 0 = 1 but `$0710`&`$0F` ≠ 0 (player status/hit state) → strings `$0F`,`$38`, same wait. Otherwise strings `$37`,`$0E`, confirm `16:84DA` (A=`$A1`; C set → cancel, string `$0F`, back to `16:9B3A`), then `SaveGame` and slot pick by `$646B` bit 0: slot 0 → `16:9CE0`(4),(5), checksum `$70F5` → `$70F0/$70F1`; slot 1 → `9CE0`(6),(7), → `$70F2/$70F3`. So `$80` = autosave-only (record written but menu refuses), `$81` = both.
- **Dead code copy:** `31:FDE0-FDFD` is byte-identical to `23:BDE0-BDFD` (tail of the load descriptors + `SaveAllowed`; `FDC0-FDDF` differ), 0 xrefs to `FDF6` — build filler, like `20:8466` and `21:BF32`. The stale-copy pattern (leftover bytes from a previous build at the same in-bank offset) should be expected in other bank tails too.

### Data Structures

*(none yet — object table: see Object sprite pass)*

### State Machine

Main state `$40` — 6 states, see MainLoop. Semantics TBD. Session 22: state 2 = init (`18:9FCC` → `19:A000`, ends `INC main_state`), state 3 = per-frame `19:A015` with sub-modes `$06F3`/`$06F0` (see "Full-ROM disassembly").

---

## Intermediate Output Files

| File | Contents |
|------|----------|
| `tools/nes_header.py` | iNES header + bank vector/entropy summary |
| `tools/instruction_set.py` | 6502 opcode DB (official + common illegal), NES/MMC3 register names |
| `tools/nesrom.py` | ROM loader, bank↔file-offset mapping, labels.csv loader |
| `tools/dis.py` | banked disassembler (`dis.py <bank> <addr> [nbytes] [--stop]`) |
| `tools/search_bytes.py` | byte-pattern search, `??` wildcards; `--context N --disasm --bank B --chr --max M`; hits as `BB:AAAA` in the bank's default window |
| `tools/xref.py` | xrefs to `[BB:]addr`: abs/abx/aby/ind, zp forms (targets <$100), same-bank rel branches; sync-check filters data false-positives (`--loose` to show); `--data` raw `.dw` words, `--imm` split `#lo/#hi` immediates; `$8000-$BFFF` targets with `BB:` scan only the 16KB pair + fixed banks (`--all` to widen) |

| `tools/decode_tables.py` | `BB:AAAA count fmt` — fmt `u8`/`s8`/`u16`/`ptr16`/`hex<N>`/`split:<hiaddr>`; resolves `$8000-$BFFF` within the 16KB pair (so a bank mapped via R7 at `$A000`, e.g. bank 12, must be read as `12:8000`) |
| `tools/sound_ids.py` | groups `LDA #id / JSR PlaySound` sites by id. Written session 3, not yet run (permission prompt) |
| `tools/sound_dump.py` | `music <id>` / `sfx <id>` / `all` (`--max N`): prints each channel stream as op text (notes, LEN frames, loops/CALL with SFX `+$2000` fix-up, drums on NOISE); `all` lists every header. Written session 4, not yet run (permission prompt) — first run should verify `music 0x13` matches the bytes quoted above |
| `tools/meta_dump.py` | metasprite records `text [ids]` / `png [out] [--chr R2,R3,R4,R5]` → `gfx/metasprites.png` (session 6, run + verified on player ids) |
| `tools/bank_profile.py` | `<banks> [--pages] [--strings] [--min N]`: per-bank/page ASCII ratio, top bytes, printable runs (session 11, run) |
| `tools/text_dump.py` | `dict`/`tables`/`msgs`/`all [--json f] [--out f]`: decodes the text format (dictionary, ctrl 05/06/07 tables, 511 messages) (session 11, run) |
| `gfx/text.txt`, `gfx/text.json` | full decoded dialogue: `{dict, t05, t06, t07, messages:[{group,msg,bank,addr,end,text}]}` (session 11) |
| `tools/text_dump.py at BB:AAAA [n]` | decode n consecutive messages from any address (session 12, run) |
| `tools/npc_script_dump.py` | `[params] [--out f] [--json f]`: NPC talk scripts `14:8000` (flag conds, section A/area list/section B chains, set-lists, message text) (session 12, run; session 19: no `next` from never-true entries, chains stop when exhaustive, re-run) |
| `tools/npc_script_check.py` | `[p…] [--flag Fxxx]` / `linear p…`: NPC script sanity check, reporting misaligned/overlapping section-B entries and junk pointers (session 19, run) |
| `gfx/npc_scripts.txt`, `gfx/npc_scripts.json` | all 256 NPC params: `[{param, ptr, A:[{off,cond,action,msg,group,idx,sets}], areas:[[area,state]], Bbase, B:[{…,next,last}], end}]` (session 12) |
| `tools/font_dump.py` | `[R0 R1] [--out f] [--scale N]`: render the 256-tile BG table for a given R0/R1 pair (session 12, run) |
| `gfx/font.png` | text-box BG tiles R0=`$3C` R1=`$38` (ASCII font + frame + HUD tiles), 16×16 grid, 3× (session 12) |
| `tools/menu_dump.py` | `[--items] [--out f]`: bank 16 menu strings `16:8C1B` (59 records, escapes FE/FD/FC) + item names `16:9086` (session 14, run) |
| `gfx/menu_strings.txt` | output of `menu_dump.py --items` (session 14) |
| `tools/telepathy_dump.py` | `[--out f]`: Telepathy fixed rows `14:9A2C`, area→class `14:98F4`, level gates `14:8213`, hint lists `14:99F4` with message text (session 14, run) |
| `gfx/telepathy.txt` | output of `telepathy_dump.py` (session 14) |
| `tools/flag_sites.py` | `[--hook2] [--out f]`: story-flag cross-reference (NPC scripts, event triggers `14:A17A`, telepathy hints); `--hook2` lists the 67 trigger records (session 14, run). **Session 18:** also merges item get/use set-lists and use-preconditions (`item_dump`), `NpcHidden` presence conds for placed handler-1 params (`presence_dump`, kind `pres`), and the decoded direct code sites from `gfx/flag_code_sites.txt` (kind `code`). Triggers that no area places (`area_dump.param_index`, handler 2) are moved to an "unplaced-only" tail. Run: 496 flags (**session 19:** 232 after the NPC-decoder fixes and dropping junk scripts; 193 flags are now unplaced-only, see "Decode fixes + junk scripts"). Only **F05C, F05D** (tested by triggers `$9E`/`$9B`), **F091** (set by `$C0`) and **F09F** (cleared by `$B9`) come solely from unplaced triggers. F05E/F060/F066 are still tested by NPC scripts or presence lists, but nothing sets them. |
| `gfx/flag_sites.txt` | output of `flag_sites.py` (session 18, without `--hook2`) |
| `tools/flag_code_sites.py` | `[--lo] [--hi] [--all] [--out f]`: code sites with abs/abs,X/abs,Y operands in `$6480-$657F`, with the flag numbers derived from adjacent masks (session 15, run) |
| `gfx/flag_code_sites.txt` | output of `flag_code_sites.py` (69 sites; entries in the text banks are still noise) (session 15) |
| `tools/area_dump.py` | `list`/`chr`/`spawns`/`indirect [areas]` — decodes `AreaPtrTable` `12:9201` (252 areas): `$07E6/$07E7`, R4/R5, per-slot spawn entries (session 7, run + verified); `indirect` = handler-1 bank 4/5 records (session 8, run); `sub [areas]` / `sub stats` = area header sub-tables k0-k5 (session 9, run) |
| `area_dump.py params [h…]` / `--out f` | (session 17, run) index (spawn handler, param) → `area.slot` list, with type / indirect record → `gfx/spawn_params.txt`; `spawns --out gfx/area_spawns.txt` |
| `tools/objtype_dump.py` | `[types] [--ai] [--out f]`: `ObjTypeTable` `13:AC00` records decoded to columns + AI id/handler; `--ai` = `ObjAiTable` `27:A314` with the types using each id (session 17, run) → `gfx/objtypes.txt`, `gfx/obj_ai.txt`. Note: AI ids `$60-$6F`/`$E0-$EF` are bank-15 addresses |
| `tools/presence_dump.py` | `[params] [--out f]`: `NpcHidden` presence lists `14:85E0/86E0` (area + flag conds) (session 17, run) → `gfx/presence.txt` |
| `tools/near_reads.py` | `<addr> [span]` — abs,X/abs,Y reads whose base is ≤ addr within span (finds table reads xref.py misses; `--all-ops` adds stores). Not sync-checked (session 8, run) |
| `tools/bank_entropy.py` | `[--block N] [banks]` — Shannon entropy per 8KB PRG bank (+ per N-byte block), `00`/`FF` share, distinct bytes (session 10, run) |
| `tools/filler_scan.py` | `[--min N] [--distinct D] [--pad] [--out f]`: same-offset identical runs between all PRG bank pairs, with text coverage, abs-operand ref hints and labels per side, plus the hand-judged `DEAD` / `LIVE_BOTH` verdict lists (session 21, run, 0.2 s) → `gfx/filler_scan.txt`. It uses manual argv parsing because `tools/dis.py` shadows the stdlib `dis` module, which breaks `argparse` on Python 3.14 |
| `tools/disasm_all.py` | `[--quiet] [--labels]`: full-ROM recursive-descent disassembler (session 22, run, 0.7 s). See "Full-ROM disassembly" under Key Findings. `--labels` prints how each ROM label is classified (code/data by name: `DATA_NAME_RE`, `CODE_NAMES`). Its output is checked by a built-in 2-pass assembler that re-assembles every file and compares it with the ROM; the run fails if any byte differs |
| `disasm/bank_NN.asm` | 32 files, one per 8KB bank, ca65-like syntax: `.org` = CPU window (bank 22 at `$A000`); `NAME = $XXXX` equates for symbols outside the file; `a:` forces absolute mode on operands below `$100`; jump tables as `.dw name`; data as `.db` (16 per line, split at labels); each code line ends with `; AAAA  raw bytes`. Auto-names are `sub_BB_AAAA` (JSR / table targets) and `loc_BB_AAAA` (branch / JMP targets). An instruction that runs over a window edge is emitted as `.db` with a `-- continues in the next window` comment |
| `disasm/coverage.txt` | code / data / unknown bytes per bank, round-trip status, auto-name count, auto table counts, the 33 indirect `JMP (…)` sites, unresolved `$8000-$BFFF` targets, rejected targets, and warnings |
| `tools/extract_tiles.py` | `[--scale N] [--outdir d]`: dumps all 128 CHR 1KB banks (session 23, run, 3 s) |
| `gfx/chr/chr_NNN.png` | one 1KB CHR bank = 64 tiles as 16×4 tiles, grayscale (index 0-3 → 0/85/170/255), 4× scale (session 23). No CHR bank is all zero. Banks 0-63 hold BG art (font/HUD/end-screen glyphs among them); banks 64-127 hold sprite characters |
| `gfx/chr_all.png` | the 128 banks, 8 per row, 2× scale, with 1 px magenta separators (2057×1041) (session 23) |
| `tools/palette_dump.py` | `[--out base] [--scale N]`: all 256 palette slots (banks 2/3 page tails) → PNG + JSON, with per-area BG/sprite references taken from `area_dump` (session 23, run). Uses the common 2C02 RGB approximation (`7C7C7C 0000FC …`, the NESdev wiki "PPU palettes" table); helpers `slot_addr`, `bg_slot`, `spr_slot`, `rgb` |
| `gfx/palettes.png` | 16×16 slots, each 4 swatches; `$FF` (backdrop) shown as a checker; left gutter bar green = area BG id, magenta = area sprite id, yellow = both (session 23) |
| `gfx/palettes.json` | `{format, master_palette:[64 hex RGB], slots:[{slot, bank, addr, colors:[4], bg_areas, spr_areas}]}` (session 23) |
| `tools/bank_calls.py` | lists each `LDA #n / JSR SwitchBank16` site with n, 8KB pair, and following `$8000-$BFFF` JSR/JMP targets (`--summary` groups by n). Not yet run (permission prompt) |

`nesrom.py` gained `bank_window(bank)` (30→$C000, 31→$E000, even→$8000, odd→$A000), `bank_cpu_addr`, `parse_bank_addr("BB:AAAA")`.

---

## Verification Checklist

- [ ] Ph3: 3+ functions traced and cross-checked against emulator trace
- [ ] Ph4: 5+ sprites/tiles extracted and visually compared to emulator
- [ ] Ph5: key data struct confirmed in emulator memory dump, all fields match
- [ ] Ph6: full game session played, no major logic gaps found
- [ ] Ph7: web port pixel-compared against emulator screenshots

---

## Reference Resources

- NESdev wiki: MMC3 (https://www.nesdev.org/wiki/MMC3), PPU registers, APU, iNES
- 6502 opcode reference (tools/instruction_set.py)

---

## Next Tasks

### RE Investigation

- [x] Ph1: identify binary header and platform
- [x] Ph3: build `tools/search_bytes.py` and `tools/xref.py` (bank-aware, reuse `nesrom.py` + `instruction_set.py`) — verified: `xref.py C40E` → 48 call sites, **all in fixed banks 30/31** (swappable banks never call SwitchBank16 directly); `xref.py 40 --bank 30` → C904/C977/C9F5; `--data` found `31:F422` IrqDefaultPtr. Pattern for next task: `search_bytes.py "A9 ?? 20 0E C4" --disasm`
- [x] Ph3: confirm `30:C427` (R7 setter) and document `SwitchBank16` fully; find all `LDA #n / JSR C40E` call sites → map which 16KB bank holds which subsystem — done session 1: SetR6 `C418`/SetR7 `C427`, far-call trampolines `FF80-FFE2`, full n→bank table in Key Findings; corrected reset bank (n=$0C → 24/25). New `tools/bank_calls.py` written (needs run-permission approval; not yet executed — `search_bytes.py --disasm --context 14` used instead)
- [x] Ph3: verify bank 28 `$800E` (n=14) and bank 24 `$8020` (reset init) — which is the sound driver? Check for `$4000-$4015` writes in banks 24-29 — done session 2: **bank 24/25 = sound driver** (SndInit `$8020`, SndCommand `$8048`, SndTick `$8186`; API `PlaySound` `30:C125`, per-frame `SoundUpdate` `31:F7FE`); bank 28 `$800E` = object→OAM sprite pass. See "Sound driver" in Key Findings.
- [x] Ph3: decode sound data formats — music ptr table `24:$8000`, channel stream init `$812A`, SFX table `12:$A000`, tick `24:$8186` opcode set; list all music/SFX ids used by PlaySound callers — done session 3: `24:8000` is the **opcode** table (16 ops); music headers `24:8C0C`, SFX headers file-bank 12 offset 0; hex-letter note encoding; ids listed. See "Sound data format".
- [x] Ph3: sound follow-ups — decode `SndOpDuty` `24:854D` triangle path + vibrato `24:86AA` exactly; check SFX table entries 48-87 for nulls; name music `$0C`/`$12`/`$13` by their callers' states; write `tools/sound_dump.py` to disassemble a stream into op text — done session 4: duty bit layout, vibrato algorithm incl. original off-by-one table index; SFX nulls `$59 $70 $71 $73 $77`; `$13`=interrupting jingle, `$12`=entity/boss track, `$0C`=event theme; `sound_dump.py` written (unrun, needs approval)
- [x] Ph3: trace bank 28 `$800E` object sprite pass fully (object table `$04A0`/`$70` layout, metasprite format at `$83D8/$83DC`) — done session 5: 32-slot object table, 4 draw-order permutations, metasprite record format (count/mask/frames, `FF` mirrored redirect, `$80` terminator, attr b2 half-hide), obj 1 = player (hit flash `$0710`, orbit/burst ring `$06C0/$06C1`). See "Object sprite pass".
- [x] Ph4: write `tools/meta_dump.py` — render metasprite ids 0-255 from `28:845C/855C` using CHR (needs sprite CHR banks from MMC3 R2-R5 at draw time) — done session 6: tool written + run → `gfx/metasprites.png`; sprite CHR shadows `$07F0-$07F5`/`$58-$5D` found, player R2=`$40/$41`, R3=`$42-$46`; ids 0-7 (player) verified clean. See "Sprite CHR banks".
- [x] Ph3: decode area table at `$9300` (bank pair TBD, read by `31:E15F`) + `LoadAreaChrRecord` `31:E16D` record fields (`$07E6/$07E7`, R4/R5, rest) → per-area enemy CHR so `meta_dump.py --chr` can render each enemy set correctly — done session 7: one 252-entry `AreaPtrTable` `12:9201` indexed by area `$6C` (banks 12/13); record = `00 e6 e7 R4 R5` + 4-byte per-slot spawn entries (slots `$0D+`, `FF` end) with 5 spawn handlers `31:E218`; `tools/area_dump.py` written + run. See "Area records".
- [x] Ph3: area follow-ups — meaning of `$07E6/$07E7` and R4/R5 = `$FF`; `ObjInit` `30:C25D` type dispatch; bank 4/5 indirect spawn records (`$80F0+`); what sets `$6C` (area transitions); `$18` reader — done session 8: `$07E0-$07E7` = 8 palette ids (`PaletteBuild` `26:8C0E`, palette records in banks 2/3 page tails); R4/R5 `$FF` = CHR bank 127 (no check); `ObjInit` = bitmask-sparse column initialiser from `13:AC00`; `$6C` set by exit lists (`AreaSubTable` `30:C7A5` k=3, `ExitLookup` `31:EB09`); `$18` has no reader; `area_dump.py indirect` written + run. See "Area records".
- [x] Ph3: area follow-ups 2 — decode the other `AreaSubTable` sub-tables (k≠3, `10:8300` headers) and the meaning of `$6D` (entry point); write `tools/palette_dump.py` that renders palette ids from banks 2/3 page tails; dump `ObjTypeTable` `13:AC00` records per type (extend `area_dump.py` or a new `objtype_dump.py`); meaning of `$0680/$06A0` in type `$30` objects — done session 9 (sub-tables + `$6D`): k0 music + W/H + screen grid → `$6300`, k1 BG palettes/CHR R0-R1, k2 camera entry points (`$6D` index → `$34-$37`), k4 visited-screen persistence (`$62F0`/`$64C0`), k5 pit/edge links (10 areas); `area_dump.py sub` written + run. The other three parts were split into the next task.
- [x] Ph2: find compressed data + decompressors — look for routines that read a data stream via `(zp),Y` and write PPU `$2007` or WRAM/RAM buffers with run-length/back-reference logic (start with the screen-layout reader `31:EBC1`/`$ED89`, the `$6300` screen grid fill, the `31:EF38` banks 0-7 stream decoder, and nametable/CHR uploaders). For each format found: document it in a "Compression" section, write `tools/decompress.py` (decode by bank:addr, report compressed→decompressed sizes), verify by decoding every stream that is referenced, and mark the regions in the Data-Range Map. If nothing is compressed, record that with the evidence — done session 10: **no compression**. Screen ids are packed bank+page pointers (`31:EF36`) to raw 16×15 metatile pages in banks 0-7; metatiles are 4 quadrant pages in banks 8/9 with a visited-screen substitution table (bank 9 `$BE00`); bank 17/18 uploaders are raw `W H` rectangles. Evidence (all 49 `$2007` writers, bit-7 stream patterns, `tools/bank_entropy.py`) is under "Compression". Data-Range Map updated, 15 labels added.
- [x] Ph3: banks 21-23 flat entropy (~5.7/1KB): look for text/dialogue (DTE/dictionary?) and the routine that reads it (bank 23 is mapped only via `SetR7($17)` `30:C9DF`) — done session 11: banks 21-23 hold **ASCII dialogue with a 128-word dictionary** (`$80-$FF`) and 10 control codes. Printer `TextPrint` `20:853A`, group→bank `20:83FE`, group ptrs `20:8422`. There are 34 groups and 511 messages, all decoded by `tools/text_dump.py` → `gfx/text.txt/json`. The auto-space quirk is documented. Bank 23 `$BC00` is the **save/load module** (`SaveGame`/`LoadGame`, `AMI` tag, 8-bit checksum, copy-length quirk); bank 23 is mapped at 7 sites, not 1. 38 labels added. See "Text / dialogue" and "Save / load module".
- [x] Ph3: text follow-ups — done session 12: (a) `ShowMessage` `30:D354` / `ShowMessagePacked` `30:D347` (linear id group·32+msg), pending-message slot `$06C3/$06E3` (`30:D085`), NPC talk `30:D0C8` → **NPC script VM** `14:8000` (2048 story flags `$6480`, per-NPC state `$6580`, sections A/B, set-lists) decoded by `tools/npc_script_dump.py` → `gfx/npc_scripts.*`; (b) `20:8466-8527` is dead filler (stale copy of bank 22), `20:8500` = `TextWindowOpen` (frame templates `20:8899`, PPU queue ring `$6200`); (c) font = ASCII tile ids in CHR `$3C-$3D`, HUD in `$38-$39` (`gfx/font.png`); (d) `23:BF00` bit 0 = menu Save allowed (`MenuSave` `16:9BE1`); `21:BF32+` and `31:FDE0-FDFD` are stale copies. 25 new labels. See "Who picks the message", "Text window + font". Original text: (a) who picks the message: trace the `$07DE/$07DF` setters (`30:D0B6/D0F5/D1D1/D1ED/D349`, `15:BAE7`, `19:BFD7`, `27:BB28` with `INC $07DE` at `BB3A`) and link NPC objects (type `$30`, `$0680/$06A0` from "area follow-ups 3") to groups/messages; (b) the text in bank 20 `$8466-$84FF` and the `20:8500` window/params path (`20:8879`/`8889` 16-byte param blocks → `$20-$2F`); (c) the character→tile mapping for the text box (BG CHR in use while `TextPrint` runs, and the tile ids `$1E/$1F/$FE/$40` for borders/prompt); (d) bit 0 of `23:BF00` and the bank 21 `$BF00` per-area table.
- [x] Ph3: text follow-ups 2 — done session 13 for (a)+(b): all 32 talk actions named in "Talk actions" (`GiveItem` `30:D3EB`, give gold +100 with a carry bug, full restore, NPC vanish, followers in slots 13/14, warp, teach Recover/Flight); every `$07DE/$07DF` setter traced. This also found substates 20-23 (teleport menu, telepathy pick/reply, change form), the `Telepathy` spell `14:816F` (8 MP, random reply rows `14:9A2C`), `NpcHidden` `14:80A0` (called from the spawn handler `31:E34B`), `ItemEventHook` `30:D834` → `14:826F/831E/80E3`, and RAM vars gold `$0702`, MP `$0708/$0709`, HP `$03C1/$03C0`. (c)-(e) moved to "text follow-ups 3". Original: (a) name the 32 `TalkActionTable` `30:D123` handlers (`$22` actions used by `gfx/npc_scripts.txt`: teach magic = 3, …) and the remaining `$07DE/$07DF` setters (`30:D43C D48C D5BE D628 D7A3 D9CB DB67 DD75 DE1E DED0`, `15:BAE7`, `19:BFD1`, `27:BB28`); (b) callers of bank 14 entries `80A0` (NPC presence, tables `85E0/86E0`), `80E3` (table `A17A`), `8170` (`$0708`, table `9A2C`); (c) name the story flags (F`00A`, F`028-02B` status, F`039`…) from their set sites; (d) map `obj_spawn_param` (NPC param) per area via `area_dump.py spawns` so each NPC gets an area + script; (e) bank 16 menu string table used by `16:8933`.
- [x] Ph3: text follow-ups 3 — done session 14 for (c)+(e) + telepathy hints + `GiveItem` ≥`$80`: (e) bank 16 `MenuPrint` `16:8933`, 59 records `16:8C1B` (escapes FE name / FD level / FC item), menu font glyphs, save slots `$7100/$7400/$7700/$7A00` (`16:9D32`), **`$0421` = player level confirmed** (`tools/menu_dump.py` → `gfx/menu_strings.txt`); telepathy hint path `14:81D1` (area classes `14:98F4`, level gates `14:8213`, lists `14:99F4`; `tools/telepathy_dump.py` → `gfx/telepathy.txt`); items ≥`$80` = **event triggers** `14:A17A` + second action table `30:D563`; (c) ~30 story flags named in "Story flags" (`tools/flag_sites.py` → `gfx/flag_sites.txt`). (d) and the other leftovers moved to "text follow-ups 4". Original: (c) name the story flags (F`00A`, F`028-02B` status, F`039`…) from their set sites (NPC set-lists, `14:80E3` table `14:A17A`, `14:826F`/`831E`); (d) map `obj_spawn_param` (NPC param) per area via `area_dump.py spawns` so each NPC gets an area + script; (e) bank 16 menu string table used by `16:8933`; plus the leftovers from session 13: what `14:826F`/`14:831E` do (and the `30:D500` caller of hook 1), the telepathy hint path `14:81D1` (`14:98F4` per-area table, `14:8213`, `$0421` = level?), the `GiveItem` paths for ids `$70` (`30:D3DA`) and ≥`$80` (`30:D539`), object types `$32` (follower), `$53` (vanish), `$54`, `$DE`, `$DF`, and the `15:BAFE` event records.
- [x] Ph3: text follow-ups 4 — done session 15 for (a)+(b): all 32 `EventActionTable` handlers named (table under "Event triggers"; act 29 = consume the trigger, **not** a boss start; act 1 = Sword of Thunder warp to area `$F2`; act 25 push-back with a carry bug; act 30 = `sub_state` `$1B`, the Mesia reunion/ending); new `tools/flag_code_sites.py` → `gfx/flag_code_sites.txt` lists the code sites that touch `$6480-$657F` directly; the object-hit flag setter `HitSetFlag` `26:9011` (params → F058/F059/F06D-F071/F099/F0A1) and the area-`$1C` follower hook `31:E7C4` (F053/F045) were found. Boss-gate setters are still unknown. (c)-(h) moved to "text follow-ups 5". 24 labels. Original: (a) name the 32 `EventActionTable` `30:D563` handlers (act 29 = boss start? 24/25/26/27/30/31 used by event triggers `14:A17A`); (b) find the **code** sites that set boss/story flags directly (`xref.py` on `$648A-$648F`, `$649D`, `$64DE/F`) so `tools/flag_sites.py` can list them, and drop bogus-script flags from its NPC side (params whose messages are all in groups ≥34); (c) map `obj_spawn_param` (NPC param / trigger id) per area via `area_dump.py spawns` so each NPC/trigger gets an area + script; (d) what `14:826F`/`14:831E` do (hooks 0/1; caller `30:D500`/`D51C`); (e) `GiveItem` path for id `$70` (`30:D3DA`); (f) object types `$32` (follower), `$53` (vanish), `$54`, `$DE`, `$DF`; (g) `15:BAFE` event records; (h) confirm the menu glyph tiles (`0A-0F`, `11-17`, `3C-3E`, `5B-5D`) by rendering the bank 16 menu CHR with `font_dump.py` (find the R0/R1 values the menu sets).
- [x] Ph3: text follow-ups 5 — done session 16 for (d) and part of (a). Hooks 0/1 are the **inventory** routines: `ItemGetHook` `14:826F` uses table `14:9B00`, `ItemUseHook` `14:831E` uses handler `14:8399` + records `14:9BE2`. Inventory is `$6430-$645F`. Both apply set-lists, so items set story flags. Boss gates F05F/F062/F067/F06C are set by picking up the boss reward; F05C/F05D/F05E/F066 still have no set site (what was checked is listed under "Item hooks 0/1"). New `tools/item_dump.py` → `gfx/items.txt`, 13 labels. The rest moved to "text follow-ups 6". Original: (a) find the **boss-gate flag setters** (F05C-F06C): not written by abs code (`flag_code_sites.py`) and not by `HitSetFlag` `26:9011`; check `(zp),Y` writes into `$6480+`, the NPC set-lists, and the boss death paths in bank 15/27 (e.g. a `JSR` into `HitSetFlag` with `$13` preset); (b) let `tools/flag_sites.py` merge `gfx/flag_code_sites.txt`, and drop bogus-script flags from its NPC side (params whose messages are all in groups ≥34); (c) map `obj_spawn_param` (NPC param / trigger id / `HitFlagParams`) per area via `area_dump.py spawns` so each NPC/trigger/flagged object gets an area + script; (d) what `14:826F`/`14:831E` do (hooks 0/1; caller `30:D500`/`D51C`); (e) `GiveItem` path for id `$70` (`30:D3DA`); (f) object types `$32` (follower), `$38`, `$53` (vanish), `$54`, `$55`, `$88`, `$9E`, `$DE`, `$DF`; the other hit handlers `26:90AA/9357/94B0` (table `26:8FBB`) and the `26:906C`/`907C` paths of `HitHandler3`; (g) `15:BAFE` event records; (h) confirm the menu glyph tiles (`0A-0F`, `11-17`, `3C-3E`, `5B-5D`) by rendering the bank 16 menu CHR with `font_dump.py` (find the R0/R1 values the menu sets); (i) `18:BC04` (called by event act 1 before the warp).
- [x] Ph3: text follow-ups 6 — done session 17 for (a), (g) and the index part of (c). Found the object loop `ObjUpdateAll` `30:CCCC` (AI table `27:A314`; AI ids `$60-$6F`/`$E0-$EF` run in bank 15), the enemy death/XP/level-up path `EnemyKilled` `26:9152`, and the **boss-defeat chain** `$DC`→`$DD`→`$CF` `BossDefeated` `15:B807` (per-area 21-byte records `15:B987`: reward drop, music, palettes, CHR). The real boss gates are **F100-F10C** (`NpcHidden` presence for boss params `$C0-$CC`, set by the reward pickup). F05C/F05D/F05E/F060/F066 are never set because their triggers `$98 $9B-$A0` are never placed. (g) `15:BAFE` = boss flee-speech records. New tools: `objtype_dump.py`, `presence_dump.py`, and `area_dump.py params`/`--out`. 21 labels. The rest moved to "text follow-ups 7". Original: (a) the remaining boss gates F05C/F05D/F05E/F066 (+ F060): check the boss-death code in bank 15/27 for `(zp),Y` writes and the `$12`=`$FF` callers of the `26:9026` write path, and look at which object types run on the boss areas; if nothing sets them, record "never set (trigger consumed instead)"; (b) let `tools/flag_sites.py` merge `gfx/flag_code_sites.txt` **and the item get/use set-lists from `tools/item_dump.py`**, and drop bogus-script flags from its NPC side (message groups ≥34); (c) map `obj_spawn_param` (NPC param / trigger id / `HitFlagParams` / use-record param) per area via `area_dump.py spawns`; (e) `GiveItem` path for id `$70` (`30:D3DA`) and which raw ids `$50-$7F` reach `ItemGetHook` after the `30:D40C` remap; the `$22` acts of the use-hook results (which action table runs them after `30:D51C`); (f) object types `$32 $38 $53 $54 $55 $88 $9E $DE $DF $E2` and hit handlers `26:90AA/9357/94B0` + `26:906C/907C`; (g) `15:BAFE` event records; (h) menu glyph tiles via `font_dump.py`; (i) `18:BC04`.
- [x] Ph3: text follow-ups 7 — done session 18 for (b), (e), (i):
  - (e) `GiveItem` passes the **raw** id to `ItemGetHook`. The `$50-$6F` remap (bytes really at `30:D45C`) only changes the name shown in the message.
  - Item `$70` is a **trap chest**: `TrapChest` `30:D3DA` turns the chest into object type `$73`, a wandering chest monster (AI `27:AC60`). Its get-record is unreachable.
  - Item-use results run through `EventActionTable`, which covers the event acts that looked unused.
  - Teleporting from the item menu calls `SaveGame`.
  - (i) the routine is `19:BC04` `SwordRaisePose`, not `18:BC04`.
  - (b) `flag_sites.py` now merges item, presence and code sites and separates unplaced triggers.
  - 7 labels. (c), (f), (h) moved to "text follow-ups 8".

  Original task: (b) let `tools/flag_sites.py` merge `gfx/flag_code_sites.txt`, the item get/use set-lists (`item_dump.py`) and the `NpcHidden` presence conds (`presence_dump.py`), and drop flags that come only from bogus NPC scripts (message groups ≥34) or from unplaced triggers (`area_dump.py params`: e.g. `$98 $9B-$A0`); (c) join `gfx/spawn_params.txt` with `npc_scripts`/`presence`/trigger records so each NPC param / trigger / `HitFlagParams` entry gets its areas + script in one report; (e) `GiveItem` path for id `$70` (`30:D3DA`), which raw ids `$50-$7F` reach `ItemGetHook` after the `30:D40C` remap, and the `$22` acts of use-hook results (`30:D51C`); (f) object types `$32 $38 $53 $54 $55 $88 $9E $DE $DF $E2` via `objtype_dump.py` + their AI handlers (`gfx/obj_ai.txt`), hit handlers `26:90AA/9357/94B0` + `26:906C/907C`, and the area `$43` boss (type `$70`, AI `$E5`, no `BossRecs` entry) reward path; (h) menu glyph tiles via `font_dump.py`; (i) `18:BC04`.
- [x] Ph3: text follow-ups 8 — done session 19 for the session-18 add-ons:
  - `$0715` = selected use-item id − `$1C` (`SelectedUseItem` `30:DB0E`). The menu checks are Warp Boots / Opel Statue / Kirisa Plant / the flutes.
  - Substate 3 = `PlayerDeath` `19:B900`. With the Opel Statue selected it runs `OpelRevive` `19:B9B0`.
  - `30:D9D8` = `SwordPowerLevel` → `$0719` (0/1/2 from sword and ball/bracelet element match). It caps the charge (`26:9D7E` = `08 10 18`) and selects the HUD charge meter.
  - `30:C17D` = `OamClipTop`.
  - The F100 "NPC sets" were decoder junk. Fixed in `npc_script_dump.py` (sentinel `next`, exhaustive chains). New `npc_script_check.py`. `flag_sites.py` now drops junk scripts (232 real flags). `$7E` has a real overlapping set-list.
  - 12 labels. (c), (f), (h) moved to "text follow-ups 9".

  Original task: (c) join `gfx/spawn_params.txt` with `npc_scripts`/`presence`/trigger records so each NPC param / trigger / `HitFlagParams` entry gets its areas + script in one report; (f) object types `$32 $38 $53 $54 $55 $73 $88 $9E $DE $DF $E2` via `objtype_dump.py` + their AI handlers (`gfx/obj_ai.txt`), hit handlers `26:90AA/9357/94B0` + `26:906C/907C`, and the area `$43` boss (type `$70`, AI `$E5`, no `BossRecs` entry) reward path; (h) menu glyph tiles via `font_dump.py`; plus from session 18: the `$0715` encoding in `ItemUseMenu` `30:D497` (menu index vs item id; `$0563` = `$0715`+`$1C`), `30:D9D8` (called after every successful pickup), `30:C17D` (used by `SwordRaisePose`), and why `flag_sites` shows F100 set by NPC `$38`/`$DD` scripts (bogus scripts with msg 0.0?).
- [x] Ph3: text follow-ups 9 — done session 20 for the session-19 add-ons and part of (f):
  - **Controller input** decoded: `ReadPadRaw` `31:FF28`, `ReadPadStable` `31:FF17`, gameplay `ReadPads` `31:FE80` (was mislabelled `ColdInitSub2`), menu `ReadPadsMenu` `31:FEE0` with d-pad auto-repeat; pad vars `$43-$4F`, button bits A=b7 B=b6 Sel=b5 Start=b4 U D L R. This also closes "identify controller read routine".
  - `$4B` b6 = **B newly pressed**. `26:9B45` is **`PlayerUpdate`** (AI `$01`, type `$04`), and B uses the selected item.
  - `$0743` = `opel_revive`: the Opel Statue can't be used from the menu (sub_state 8 = `SubstPlay` `30:CB62`, no message). It's only consumed by the death revive.
  - `$643B`+Y: a bracelet in the sword's element slot gives power 1 even when it isn't equipped. Crystalis pickup clears all ball slots, so Crystalis power = parity of the first consumable id (static quirk).
  - (f) AI handlers for types `$38` (`AiPathWalk` + `AiPathTable`), `$DE` (none), `$DF`, `$55`, `$88`/`$9E` (`BossPhaseDispatch` `15:A400`).
  - 22 labels. (c), (h) and the rest of (f) moved to "text follow-ups 10".

  Original task: (c) join `gfx/spawn_params.txt` with `npc_scripts`/`presence`/trigger records so each NPC param / trigger / `HitFlagParams` entry gets its areas and script in one report (reuse `npc_script_check.placed_params`); (f) object types `$32 $38 $53 $54 $55 $73 $88 $9E $DE $DF $E2` via `objtype_dump.py` + their AI handlers (`gfx/obj_ai.txt`), hit handlers `26:90AA/9357/94B0` + `26:906C/907C`, and the reward path for the area `$43` boss (type `$70`, AI `$E5`, no `BossRecs` entry); (h) menu glyph tiles via `font_dump.py`; plus from session 19: what `$0743` is (Opel revive sets `$40`; item `$0A` use clears it), `$4B` b6 in `26:9B45` (quick-use button?), the `$643B`+`$0711` read for Crystalis (`$6440`) in `SwordPowerLevel`, and the `$7E` overlapping set-list (confirm in the emulator).
- [x] Ph2: stale-copy filler scan — done session 21: `tools/filler_scan.py` → `gfx/filler_scan.txt`. It finds 32 content runs plus 18 padding runs. Judged by hand: **25 dead ranges (4164 bytes)**, listed in `filler_scan.DEAD` for `disasm_all.py`, with a table under "Stale-copy filler" in the Data-Range Map. New finds: stale text inside the fixed banks (`30:C8C0-C8FF`, `31:F9BA-F9FF`), stale code at `18:9FC2`, and the fact that substitution tables k≥12 (`9:BF80+`) are unused. Corrected `20:8466-8527` to `84FF`. Original: write `tools/filler_scan.py`: for every bank, find runs ≥ 32 bytes that are byte-identical to another bank at the **same in-bank offset** (known: `20:8466`↔`22:A466`, `21:BF32`↔`23:BF32`, `31:FDE0`↔`23:BDE0`), report them, and mark them as dead filler in the Data-Range Map so `disasm_all.py` treats them as data.
- [x] Ph3: full-ROM disassembly — done session 22:
  - `tools/disasm_all.py` → `disasm/bank_00-31.asm` + `disasm/coverage.txt`. **All 32 banks re-assemble byte-exact** with the built-in 2-pass assembler.
  - Bank context is tracked per path, including summarised callee bank effects, and straddling across window edges is handled. 40,170 code bytes, 1,978 auto-names, 0 warnings, 1 unresolved target.
  - **Found:** `18:9FC2-9FFF` is **live** main-state-2 code (`MainState2Init` `18:9FCC` → `19:A000`), not dead filler; `filler_scan.DEAD` is now 24 ranges.
  - State 3 sub-dispatch decoded: `State3ModeTable` `19:A038`, `State3SubTable` `19:A052`.
  - Bank 16 menus run with bank 23 at `$A000`.
  - Swappable banks have no `$8000` entry JMP tables.
  - 8 dispatch tables are still untraced and banks 25/29 are 100% unknown; both are in the new task at the end of the list. Data-Range Map "Code coverage" added. 6 labels.

  Original task: write `tools/disasm_all.py`: recursive-descent code tracer seeded from the vectors, every `labels.csv` code label, the `SwitchBank16`/far-call (`31:FF80-FFE2`) targets, and known jump/pointer tables (MainStateTable, IrqHandlerTable, SndOpcodeTable, substate tables, swappable-bank `$8000` entry tables). Follow JSR/JMP/branches within the bank (bank 30/31 targets cross into fixed banks; `$8000-$BFFF` targets need the bank from the call context). Output `disasm/bank_NN.asm` (one per 8KB bank, CPU origin per `nesrom.bank_window`) with labels + comments from `labels.csv`, auto-names `sub_BB_AAAA`/`loc_BB_AAAA` for unlabeled targets, data as `.db` rows (16/line), and `disasm/coverage.txt` (code/data/unknown bytes per bank). Verify: reassembling is not required, but every decoded byte must round-trip (concatenated bytes of each .asm == the bank). Update the Data-Range Map from the coverage report.
- [x] Ph4 assets 1/5 — done session 23: `tools/extract_tiles.py` → `gfx/chr/chr_000-127.png` + `gfx/chr_all.png`; `tools/palette_dump.py` → `gfx/palettes.png/.json` (all 256 slots, area BG/sprite references; this covers the `palette_dump.py` part of "area follow-ups 3"). Corrected the slot count to **256**, not 512; sprite ids wrap via `+$B0`. The `$3F00` writer is `PaletteUpload` `31:F8CB`, called only from the NMI. It is skipped while `$51` b7 is set, and the backdrop is forced into every group. See "Palette ids". 1 label. Original: tiles + palettes: `tools/extract_tiles.py` dumps all 128 CHR 1KB banks to `gfx/chr/chr_NNN.png` (grayscale, 16x4 tiles, 4x scale) + one `gfx/chr_all.png` sheet; `tools/palette_dump.py` (shared with "area follow-ups 3" — reuse if it already exists) with a standard 64-color NES master palette (2C02 RGB in the tool, cited), renders every palette id from `PaletteBuild` `26:8C0E` / bank 2/3 page tails to `gfx/palettes.png` and writes `gfx/palettes.json`. Also find the PPU `$3F00` writer and confirm the palette upload path.
- [ ] Ph4 assets 2/5 — backgrounds (format known, no compression — see "Compression", session 10): decode `MetatileAttr` `31:EDE5`/`EEEF` and the bank 9 `$BE00` visited-screen substitution tables; check which grid ids map to which banks and whether `$80` (no screen) is ever read. Write `tools/render_screen.py` (screen id → packed bank+page `31:EF36` → raw 16×15 metatile page in banks 0-7 → metatiles from banks 8/9 with tileset `$67` → BG CHR R0/R1 + k1 palettes → `gfx/screens/screen_NNN.png`) and `tools/render_level.py` (whole area from the `$6300` screen grid via `area_dump` k0 → `gfx/areas/area_NNN.png`). Render every area `area_dump.py` lists, plus the title/menu `W H` rectangles from banks 17/18.
- [ ] Ph4 assets 3/5 — sprites: extend `meta_dump.py` to render each area's enemy sets with the correct R4/R5 CHR (`LoadAreaChrRecord`) and sprite palettes, and export animation frame sequences (frame mask, ÷8 timing) to `gfx/sprites/` + `gfx/sprites.json` (id → frames, offsets, attrs, tiles).
- [ ] Ph4 assets 4/5 — audio + text: write `tools/sound_export.py` that converts every music track and SFX (via `sound_dump.py` decoding) to `web/data/sound.json` (per-channel event lists: notes, lengths, duty, envelope, vibrato params) so the web port can synthesize them; optionally render WAV previews to `gfx/audio/`. Text: build on the "banks 21-23 text" task above; find the font tiles and any text/string tables (character encoding → `tools/text_dump.py`, `gfx/text.txt`); if the game has no text beyond tile graphics, record that.
- [ ] Ph4 assets 5/5 — `web/catalog.html` asset browser (plain HTML+JS, `python3 -m http.server`): CHR banks, palettes, sprites with animation playback per area, rendered screens/areas, sound player (WebAudio from `sound.json`), text. All data loaded from `gfx/*.json`/`web/data/`. Record the asset formats and output files in REVERSE.md (Intermediate Output Files + format specs).
- [ ] Ph3 (low priority) text/gameplay leftovers from sessions 11-20 (was "text follow-ups 10") — (c) join `gfx/spawn_params.txt` with `npc_scripts`/`presence`/trigger records so each NPC param / trigger / `HitFlagParams` entry gets its areas and script in one report (reuse `npc_script_check.placed_params`); (f) the generic AIs `$10` `27:B0B5` (types `$32 $E2` + 26 others; helpers `27:9897/98A8/98C7/98CD/9861`, `27:8480`, `27:9A30`), `$20` `27:A915` (`$53` + 9), `$28` `27:AB61` (`$54 $67`), `$30` `27:ACD9` (talkable NPC); hit handlers `26:90AA/9357/94B0` + `26:906C/907C`; the area `$43` boss reward path (type `$70`, AI `$E5`, no `BossRecs` entry); decode `BossPhaseTable` `15:A413` (count, per-boss base indices: `$1B` = type `$88`, `$2B` = type `$9E`) and what `$07D7` does; (h) menu glyph tiles via `font_dump.py`; plus from session 20: whether `26:9C56-9CC8` (other `$0711` readers) special-case Crystalis (`equip_sword`=5) so its charge level does not depend on the first consumable's parity; `PlayerUpdate` sub-calls `26:9BF6/9CEF/A1C9` (hand to "gameplay 1/3" if large).
- [ ] Ph5 structs 1/5 — RAM map: write `tools/ram_map.py` that scans all code (use `disasm_all.py` output if present, else `xref.py` logic) and lists every RAM address `$0000-$07FF` and WRAM `$6000-$7FFF` with read/write counts per bank, the `labels.csv` name, and whether it is indexed (`,X`/`,Y`). Detect parallel arrays (same index register, stride-1 bases 32 apart, e.g. the object table). Output `docs/ram_map.txt`, then write a "RAM Map" table under `### Data Structures` (range, name, size, type, notes) and add labels for every unlabeled address with ≥3 references.
- [ ] Ph5 structs 2/5 — object slot struct: full field table for the 32-slot object arrays (`$70/$90/$B0/$D0`, `$0300-$06FF`, `$04A0/$04E0`, `$0680/$06A0`, …). For each field: base, size, type, who writes it (`ObjInit` `30:C25D`, spawn handlers `31:E218`, entity logic bank 15), who reads it (`ObjSpriteDraw` `28:800E`, collision, AI). Decode `ObjTypeTable` `13:AC00` per-type records with a new `tools/struct_dump.py` (pre-configured layouts: objtype, area record, area sub-tables k0-k5, sound headers, metasprite, palette record, metatile). Reuse/close the `objtype_dump.py` part of "area follow-ups 3".
- [ ] Ph5 structs 3/5 — save data: trace cold-init subs `F1CD`, `FE80`, `F0A4`, the `$7D00-$7EFF`→`$6480` copy, `$6140`/`$6200` (`$FF`-filled at reset), `$62F0`/`$64C0` visited-screen flags, `$6300` screen grid; produce a field table for `$6000-$7FFF` and identify the save/checksum routine (if any).
- [ ] Ph5 structs 4/5 — pointer tables + bank refs: write `tools/check_bank_refs.py` (every `.dw` pointer and far-call/`SwitchBank16`/`SetR6`/`SetR7` target lands inside a valid bank window and on decoded code/data) and `tools/find_ptr_tables.py` (runs of ≥4 plausible pointers per bank, cross-checked against `xref.py` indexed loads `LDA tbl,X` / `LDA tbl+1,X`). Catalogue every table in a "Pointer Tables" list under Data Structures (bank:addr, count, element type, consumer); label the unlabeled ones and queue tasks for any undecoded ones.
- [ ] Ph5 structs 5/5 — consolidate: rewrite `### Data Structures` as an index — one field table per struct (object slot, object type, area record + sub-tables k0-k5, screen page/metatile, metasprite, palette record, sound music/SFX headers + channel state, save data, RAM map) with links to the subsystem sections that explain behaviour. Mark each struct "static-RE only" until the Ph5 emulator memory-dump checkpoint confirms it.
- [ ] Ph5.5 emu 1/6 — CPU + memory: `tools/emu/` package (pure Python, no third-party emulators). `cpu.py` (all official 6502 opcodes + the illegal ones in `instruction_set.py`, exact cycle counts incl. page-cross/branch penalties, NMI/IRQ/BRK, no decimal mode — reuse `instruction_set.OPCODES`), `memory.py` (2KB RAM mirrored, 8KB WRAM, register dispatch), `loader.py` (iNES via `nesrom.py`). Write `tools/emu/test_cpu.py`: hand-written per-opcode unit tests for flags (ADC/SBC overflow, CMP, BIT, shifts, page-wrap of `JMP ($xxFF)` and zp,X), run and passing.
- [ ] Ph5.5 emu 2/6 — MMC3 mapper: `mapper.py` with R0-R7, PRG mode 0/1, CHR mode 0/1 (2KB/1KB banks), mirroring, PRG-RAM enable/protect, and the scanline IRQ counter (latch/reload/disable/enable, clocked on PPU A12 rises — approximate at PPU dot 260 of each visible+pre-render line when BG/sprites use opposite pattern tables). Test: replay Reset `31:F2A4` → `SwitchBank16` and check banks 24/25 mapped and `$8020` reached; verify `$C40E` bank shadows `$6E/$50`.
- [ ] Ph5.5 emu 3/6 — PPU + frame loop: `ppu.py` with `$2000-$2007` semantics (v/t/x/w latch, `$2007` read buffer, increment 1/32, palette mirroring, PPUSTATUS vblank/sprite-0/overflow clears), OAM DMA (`$4014`, 513/514 cycles), vblank NMI at scanline 241, sprite-0 hit. Scanline renderer (BG + 8x8/8x16 sprites, priority, 8-per-line limit, left-8 masking, per-scanline scroll so the IRQ splits at `IrqHandlerTable` render correctly) using the 2C02 palette from `palette_dump.py`. `--dump-screen FILE.png` (Pillow if available, else a tiny PNG writer in `tools/emu/png.py`). APU: register writes logged only (`--log-apu`), `$4015` reads and frame-counter IRQ stubbed. Controllers: `$4016/$4017` strobe+shift.
- [ ] Ph5.5 emu 4/6 — CLI + scripting: `python3 -m tools.emu` with `--frames N`, `--dump-regs`, `--boot-test`, `--trace [--trace-range BB:AAAA-AAAA]` (text from `dis.py` decoder + labels), `--break BB:AAAA` (bank-aware, dump regs/stack/ZP), `--run-func LABEL` (labels.csv lookup, preset A/X/Y/RAM, stop at matching RTS), `--keys FRAME:BUTTONS` (e.g. `120:START`, `300:A+RIGHT`), `--dump-ram FILE` / `--dump-wram FILE`, `--dump-screen FILE.png`, `state.py` save/load (`--save-state`/`--load-state`), and a Python hook API (`emu.on_exec(bank, addr, fn)`, `emu.on_write(addr, fn)`, per-frame callback). Benchmark frames/second and record it.
- [ ] Ph5.5 emu 5/6 — boot to gameplay: script a run from power-on through the title/menu to the first controllable gameplay frame (find the button sequence by reading the main-state handlers; record it in REVERSE.md as the canonical `--keys` script). Save states at title, first gameplay frame and one area transition into `tools/emu/states/`. Dump screens of each to `gfx/emu/` and compare against the static renderers (`render_screen.py`, `meta_dump.py`) with `tools/render_compare.py IMG1 IMG2 [--diff]`; log discrepancies as tasks.
- [ ] Ph5.5 emu 6/6 — verification checkpoints: (a) trace 3+ functions (`SwitchBank16`, `PlaySound`, `ObjSpriteDraw`, `PaletteBuild`) and compare the trace against the documented behaviour → tick the Ph3 checkpoint; (b) dump RAM/WRAM in gameplay and check every field of the object slot struct, area record, sound channel state and save layout against "Data Structures" → tick the Ph5 checkpoint, change "static-RE only" to "(VERIFIED)" per struct; (c) add `--compare` mode that writes per-frame CSV (object positions, state `$40`, scroll, sound channel periods) for later diffing against the web port.
- [ ] Ph6 validate 1/4 — reference cross-check of `tools.emu`: write `tools/ref/fceux_capture.lua` (FCEUX Lua: replay the canonical `--keys` script, and at listed frames save screenshot + RAM `$0000-$07FF` + WRAM + PPU nametables/palette/OAM to `ref/frame_NNNN.*`) and `tools/ref/compare_ref.py` (diff those against `tools.emu` at the same frames: RAM byte diffs, palette/OAM diffs, pixel diff via `render_compare.py`). If `ref/` is empty (no reference emulator available to the session), say so in REVERSE.md and leave the comparison unrun — do NOT treat `tools.emu` output alone as ground truth for PPU/IRQ timing; mark such checks "self-emu only".
- [ ] Ph6 validate 2/4 — findings sweep: add a `### Verification Log` table under Key Findings (subsystem, claim, check method, result, session). For every Key Findings subsection (boot, bank switching, main states, NMI, IRQ splits, sound driver + vibrato quirk, object sprite pass, area records + sub-tables, screens/metatiles, palettes, save data) run at least one emulator check (exec/write hook or trace) and mark the section heading `(VERIFIED)` / `(VERIFIED self-emu)` or log the discrepancy as a new task.
- [ ] Ph6 validate 3/4 — full session: script a playthrough that visits as many areas as possible (plan the route from `ExitLookup` `31:EB09` / k3 exit lists; extend the `--keys` script, use save states between legs). Per area visited, record area id `$6C`, music id, CHR R0-R5, palette ids, spawned object types vs `area_dump.py`, and stitch emulator screens into a map to compare with `render_level.py` output. Collect code coverage (exec hook per bank:addr vs `disasm_all.py` code ranges); list main states/substates and large routines never executed → new tasks for logic gaps. Tick the Ph6 checkpoint only if no major gaps remain.
- [ ] Ph6 validate 4/4 — `COMPARISON.md`: one row per screen/state (title, menu, each visited area, pause/status, game over, save) with image pairs (reference or emulator vs static renderer now; a web-port column left empty for Ph7), severity FIXED / MINOR / CRITICAL, and REVERSE.md cross-references. Tick the Ph4 checkpoint (5+ sprites/tiles visually matched) once the pairs show it.
- [ ] Ph3 gameplay 1/3 — player: find the player update routine (object slot 1; controller vars from the "controller read" task), and document movement + physics exactly (sub-pixel position bytes, velocity/accel/friction/gravity constants, jump arc, facing, animation ids → metasprite ids), the actions/attacks, and the player state machine. Every constant goes in a table with its bank:addr.
- [ ] Ph3 gameplay 2/3 — world interaction: collision against BG (metatile attributes `MetatileAttr` `31:EDE5`/`EEEF`: solid/hazard/ladder/etc. bits), object-vs-object hitboxes (per-type sizes from `ObjTypeTable` `13:AC00`), damage/knockback/invulnerability (`$0710` hit flash), health/lives/score/items RAM, pit/edge links (k5) and exit triggers (k3, `ExitLookup` `31:EB09`), camera scroll rules (k2 entry points `$34-$37`).
- [ ] Ph3 gameplay 3/3 — enemies: catalogue the entity handlers in bank 15 (n=7) by object type — dispatch table, per-type AI state machine, movement constants, spawn/despawn rules, projectile types, boss behaviour (music `$12` sites), drops. One subsection per type; split into more tasks if there are more than ~10 distinct types.
- [ ] Ph7 prereq — `docs/architecture_exe.md`: binary layout (bank map + Data-Range Map summary), call graph from Reset/NMI/IRQ/MainLoop with entry points + file offsets, module map per bank, per-frame execution flow (main state → substate → object update → sprite pass → NMI upload → IRQ splits → sound tick), data flow (ROM tables → RAM structs → PPU/APU), shared RAM/WRAM regions.
- [ ] Ph7 web 1/8 — design + data export: write `docs/architecture_web.md` first (JS module graph with exports/deps, EXE→web mapping table: each ROM routine → JS function, game loop flow, state machine). Then `tools/gen_web_data.py` exports everything from the ROM to `web/data/` (CHR as packed 2bpp `.bin`, palettes, metasprites, area records + sub-tables, screen pages, metatiles + attrs, object types, sound data, text) with a `manifest.json`; the port must not hand-copy table values.
- [ ] Ph7 web 2/8 — core: `web/index.html` + ES modules (no bundler, served by `python3 -m http.server`). NES-style `ppu.js` that renders into a 256×240 indexed `Uint8Array` framebuffer (nametables, attribute tables, OAM, 8-sprite-per-line limit, CHR bank selection mirroring R0-R5, per-scanline scroll for the IRQ splits) then blits to a canvas at integer scale; fixed-step loop at 60.0988 Hz; input (keyboard + Gamepad API → NES button byte); main state machine mirroring `MainStateTable` (`30:C91F`) with stub states. Keep game logic free of DOM so it runs under Node.
- [ ] Ph7 web 3/8 — Node test harness: `web/test/run.mjs` runs the port headless (`--keys` script in the same `FRAME:BUTTONS` format as `tools.emu`, `--frames N`, `--dump-screen out.png` via a small PNG encoder, `--compare out.csv` with the same columns as `tools.emu --compare`). Write `tools/compare_frames.py` to diff two CSVs and two PNGs (uses `render_compare.py`). Add `Bash(node web/test/*)` usage notes to CLAUDE.md.
- [ ] Ph7 web 4/8 — world: area load (area record, sub-tables k0-k5, CHR R0/R1 + R4/R5, palette build), screen grid → screen pages → metatiles → nametables, camera/scrolling and entry points, visited-screen substitution + persistence, exits and area transitions. Compare each area's map against `render_level.py`.
- [ ] Ph7 web 5/8 — objects + player: 32-slot object table with the same field layout, spawn lists + `ObjInit`, sprite pass port (4 draw orders, flicker, mirrored records, half-hide, player hit flash, orbit ring incl. its OAM overwrite quirk), player movement/physics/actions from "gameplay 1/3", BG + object collision and damage from "gameplay 2/3". Validate with `--compare` CSV against `tools.emu` over the canonical input script: player x/y must match frame-for-frame.
- [ ] Ph7 web 6/8 — enemies + bosses from "gameplay 3/3", one type at a time, each validated by a save-state scenario in `tools.emu` vs the port (object positions per frame).
- [ ] Ph7 web 7/8 — audio + screens: WebAudio APU (2 pulse with duty, triangle, noise, DMC samples, NTSC periods) driven by a port of the sound driver tick (`24:8186`, all 16 ops, envelopes, vibrato incl. the original off-by-one table quirk); title/menu/pause/game-over screens; HUD; save data in `localStorage` mirroring the WRAM layout.
- [ ] Ph7 web 8/8 — compare + checkpoint: run the harness and `tools.emu` (and `ref/` captures if present) for at least 3 game states (title, first gameplay frame, one later area), fill the web-port column of `COMPARISON.md`, file every mismatch as a task under `### Web Port Fixes`, and tick the Ph7 checkpoint once those 3 states match pixel-for-pixel.
- [ ] Ph3: area follow-ups 3 — write `tools/palette_dump.py` (render palette ids from bank 2/3 page tails; note k1 BG ids e.g. `7C 7D 7E`); dump `ObjTypeTable` `13:AC00` records per type (`objtype_dump.py`); meaning of `$0680/$06A0` in type `$30` objects; `$62FE` (·8 at `31:E75E`), `$67/$69` and `$6A/$6B` ptr from sub-table 1; screen ids in `$6300` → which bank holds the screen layouts (`31:EBC1` reads via `$ED89`)
- [ ] Ph3: decode `30:CADE`/`CB2E` substate tables (length, name each state-1 substate handler)
- [ ] Ph3: trace `31:EF38` packed bank+pointer decoder — find the pointer tables that feed it (banks 0-7 data streams)
- [ ] Ph3: decode jump tables at start of swappable banks (`$8000-$802F` in banks 12, 18, …)
- [ ] Ph3: trace the 6 main states (`C957 CAB6 C931 C939 C944 C94C`) — name them (title/menu/gameplay/…)
- [ ] Ph3: trace NMI fully (`31:F3B6`-`F421`): PPU update queue, `$51` modes 7/9, scroll writes, sound driver call
- [ ] Ph3: decode IRQ handlers 1-7 (`IrqHandlerTable`) — which screens use which split
- [x] Ph3: identify controller read routine and input RAM vars — done session 20 (see "Controller input" under the screen pipeline in Key Findings)
- [x] Ph3: identify sound driver bank + entry points — covered by session 2 (see above)
- [ ] Ph2 (low priority) filler leftovers from session 21 — find the reader of `NameCharGrid` `18:9F05-9FC1` (it may be read through a `(zp),Y` pointer; if no reader exists, add it to `filler_scan.DEAD`), and check whether anything reads `9:B300-BDFF` (past the attribute tables `B000-B2FF`; `09:BCFA-BDFF` is the live source of the bank 11 copy). Confirm both with an emulator read watch once Ph5.5 exists.
- [ ] Ph3 (low priority) disasm coverage leftovers from session 22:
  - Decode the 8 untraced dispatch tables (table under "Full-ROM disassembly": `17:A11A`, `27:A072`, `27:BA40`, `30:DBDC`, `30:DDBD`, `30:DF0E`, `31:E3D9` (index A>>4), `31:F6C2` (`irq_mode`)). Find their counts from the index range, then add them to `disasm_all.JUMP_TABLES` with the right bank context and re-run.
  - Name the state-3 modes (`State3SubTable` handlers `19:A79F…A6DF`, modes 1/2 `18:8310`/`18:834A`).
  - Confirm that banks 25 and 29 hold no code (sound data via R7=25; metasprites via `ObjMetaPtrTableB`).
  - Re-check `15:AB84` (`PLA → SetR6`, likely 26:9824).
  - Then update the Data-Range Map coverage table.

### Web Port Fixes

### Documentation

- [ ] Final docs refresh: update `docs/architecture_exe.md` and `docs/architecture_web.md` to match the finished port (module graph, EXE→web mapping table, state machine), and check every REVERSE.md section is linked from one of them.
