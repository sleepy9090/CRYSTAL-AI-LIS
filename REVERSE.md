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
- id p (+`$B0` if a sprite palette, X≥`$10`) → `SetR7(2 + (p>>7))` (banks 2/3 at `$A000`), source = `$A000 + ((p>>2)&$1F)·$100 + $F0 + (p&3)·4`: **4-byte palette records live in the last 16 bytes of every 256-byte page of banks 2/3** (512 slots; same page-tail layout as the bank 4/5 spawn records of handler 1).
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
| `20:8466-84FF` | more message text in bank 20 itself (reader TBD; possibly the `20:8500` path) |
| `20:8900` `TextDictPtrs` | 128 words: pointer for dictionary code b = `$8900 + (b&$7F)·2` (the `ASL` drops bit 7) |
| `20:8AF0-8D3x` `TextDict` | zero-terminated words, e.g. `you`, `your`, `you've`, `the`, `with`, `Please`, `magic`, plus proper nouns |
| 23 `$BC00-$BEFF` | **not text**: save/load code (below) |
| 21 `$BF00` / 23 `$BF00` | 256-byte per-area flag tables (`00`/`80`/`81`), not text |

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
| `03` | `8619` | – | page break: flush, put tile `$40` (prompt) at PPU `$2910`, wait for A/B (`$4B` b7/b6) one frame at a time (re-maps the text bank after each `ColdInitSub2` `FE80`), then clear |
| `04` | `866B` | – | print the zero-terminated **player name at `$6400`** (WRAM, saved to `$7D00`) |
| `05` | `8682` | n | string `20:8A00`[n] (10 common words/phrases) |
| `06` | `86CD` | n | string `20:8A14`[n] (36 proper nouns: people + places) |
| `07` | `86E1` | n | string `20:8A5C`[n] (74 item/equipment/spell names; `$49` = 17 spaces) |
| `08` | `8772` | – | string `20:8A5C`[`$07DC`] (`$FF` → `$49` blank): the current item |
| `09` | `8753` | n | n spaces |

**Auto-space quirk:** words (`$80+`) and codes 05/06/07 return through `$869F`. That code peeks at the next byte and writes a space if it is `04-09`, or ≥ `$40` and ≠ `$5F` (letters, `_`-excluded, words). Literal chars, `04` and `08` never auto-space, so the same table string can act as a word fragment (`every{05:one}`). The port must reproduce this rule exactly.

**Buffer/box:** `TextChar` `87AE` stores to `$6000[$28]`, `$28`++. `TextClearBuf` fills `$6000-$60FF` as 8 rows of 32: `FE 1E` + 29×`$20` + `1F` (left/right border tiles), cursor `$20`. `TextFlush` `85B8` uploads 4×`$40` bytes (`$25` = src offset 0/`$40`/`$80`/`$C0`) to PPU `$2820/$2860/$28A0/$28E0` through `20:8836`. The text box is therefore nametable 2 rows 1-8.

**Tool:** `tools/text_dump.py [dict|tables|msgs|all] [--json f] [--out f]`. Run session 11 → `gfx/text.txt` + `gfx/text.json`: **511 messages in 34 groups** (group sizes 1-32), no unknown control codes, and every message ends in `00`. Some pointers are shared, e.g. 0.2/0.5/0.8 → `21:A036`.

### Save / load module, bank 23 `$BC00` (session 11)

Bank 23 `$BC00-$BEFF` is code, not text. Jump table: `BC00`→`SaveGame` `BC09`, `BC03`→`LoadGame` `BC8E`, `BC06`→`LoadGameAndInit` `BCDA`. Bank 23 is mapped at `$A000` by `SetR7($17)` at **7 sites** (not one): `30:C9DF` (state-5 entry `C9DA` → `BC06`), `30:D4EC` (`JMP $BC00`), `30:D8DF` (together with `SetR6($10)`, then `JSR $8000` = bank 16 menu, which calls `BC00` at `16:9C4B/9C64` and `BC03` at `16:9BC6/9BD6`), `31:E500/E535` (`BC00`), `31:F0B0/F1DC` (boot: compares the WRAM copies `$7100-73FF` vs `$7400-76FF` and the `AMI` tag at `$71FD`; this is a second save layout, see the Ph5 save task).

- **`SaveGame`**: saves `$10-$2F` → `$6000` and keeps `$0710` hi nibble only. `SaveAllowed` `BDF6` (C = bit 7 of `23:BF00`[`$6C`]) decides what happens:
  - **Save allowed:** zero `$7D00-$7FFF`, then `SavePlayerState` `BD1F`: `$7DF0`=`$6C` area, `$7DF1-F4` = player x lo/hi, y lo/hi (`$70/$90/$B0/$D0`), `$7DF5/F6`=`$03C0/$03C1`, `$7DF7`=`$0421`, `$7DF8`=`$0620`, and the tag `$7DFD-$7DFF` = `41 4D 49`. Then 5 block copies from descriptors `23:BDC0` (6 bytes: src, dst, count lo/hi): `$6400→$7D00` (128), `$6480→$7E00`, `$0702→$7D80`, `$07DC→$7DB0`, `$7D00→$6D00`. Then `SaveChecksum` `BD92`: an 8-bit sum of `$7D00-$7FFF`, stored in `$70F5` (copy `$70F4`).
  - **Both paths:** copy `$7D00-$7FFF` → `$6D00-$6FFF` (backup mirror), then restore `$10-$2F`.
- **Copy-length quirk** (`BlockCopy` `BCF7`): the count loop copies **lo + 1 + hi** bytes, not the 16-bit count + 1. For example `FF 01` → 257, `FF 02` → 258, `7F 00` → 128, `1C 00` → 29, `23 00` → 36. The port must copy exactly these lengths.
- **`LoadGame`**: 4 reverse descriptors `23:BDDE` (`$7D00→$6400`, `$7E00→$6480`, `$7D80→$0702`, `$7DB0→$07DC`), then `LoadPlayerState` `BD60` (inverse of `BD1F`). No checksum check here.
- **`LoadGameAndInit`**: `LoadGame`, then copy `$7E00→$6480` and `$7F00→$6580` (512 bytes, overwriting part of the `$6480` block), `$6D`=`$FF`, `$41`=1.
- `23:BF00` (256 bytes, one per area `$6C`): `$80`/`$81` = save allowed (bit 0 meaning TBD), `00` = not allowed. Bank 21 `$BF00` holds a similar per-area table (reader TBD).

### Data Structures

*(none yet — object table: see Object sprite pass)*

### State Machine

Main state `$40` — 6 states, see MainLoop. Semantics TBD.

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
| `tools/area_dump.py` | `list`/`chr`/`spawns`/`indirect [areas]` — decodes `AreaPtrTable` `12:9201` (252 areas): `$07E6/$07E7`, R4/R5, per-slot spawn entries (session 7, run + verified); `indirect` = handler-1 bank 4/5 records (session 8, run); `sub [areas]` / `sub stats` = area header sub-tables k0-k5 (session 9, run) |
| `tools/near_reads.py` | `<addr> [span]` — abs,X/abs,Y reads whose base is ≤ addr within span (finds table reads xref.py misses; `--all-ops` adds stores). Not sync-checked (session 8, run) |
| `tools/bank_entropy.py` | `[--block N] [banks]` — Shannon entropy per 8KB PRG bank (+ per N-byte block), `00`/`FF` share, distinct bytes (session 10, run) |
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
- [ ] Ph3: text follow-ups — (a) who picks the message: trace the `$07DE/$07DF` setters (`30:D0B6/D0F5/D1D1/D1ED/D349`, `15:BAE7`, `19:BFD7`, `27:BB28` with `INC $07DE` at `BB3A`) and link NPC objects (type `$30`, `$0680/$06A0` from "area follow-ups 3") to groups/messages; (b) the text in bank 20 `$8466-$84FF` and the `20:8500` window/params path (`20:8879`/`8889` 16-byte param blocks → `$20-$2F`); (c) the character→tile mapping for the text box (BG CHR in use while `TextPrint` runs, and the tile ids `$1E/$1F/$FE/$40` for borders/prompt); (d) bit 0 of `23:BF00` and the bank 21 `$BF00` per-area table.
- [ ] Ph3: full-ROM disassembly — write `tools/disasm_all.py`: recursive-descent code tracer seeded from the vectors, every `labels.csv` code label, the `SwitchBank16`/far-call (`31:FF80-FFE2`) targets, and known jump/pointer tables (MainStateTable, IrqHandlerTable, SndOpcodeTable, substate tables, swappable-bank `$8000` entry tables). Follow JSR/JMP/branches within the bank (bank 30/31 targets cross into fixed banks; `$8000-$BFFF` targets need the bank from the call context). Output `disasm/bank_NN.asm` (one per 8KB bank, CPU origin per `nesrom.bank_window`) with labels + comments from `labels.csv`, auto-names `sub_BB_AAAA`/`loc_BB_AAAA` for unlabeled targets, data as `.db` rows (16/line), and `disasm/coverage.txt` (code/data/unknown bytes per bank). Verify: reassembling is not required, but every decoded byte must round-trip (concatenated bytes of each .asm == the bank). Update the Data-Range Map from the coverage report.
- [ ] Ph4 assets 1/5 — tiles + palettes: `tools/extract_tiles.py` dumps all 128 CHR 1KB banks to `gfx/chr/chr_NNN.png` (grayscale, 16x4 tiles, 4x scale) + one `gfx/chr_all.png` sheet; `tools/palette_dump.py` (shared with "area follow-ups 3" — reuse if it already exists) with a standard 64-color NES master palette (2C02 RGB in the tool, cited), renders every palette id from `PaletteBuild` `26:8C0E` / bank 2/3 page tails to `gfx/palettes.png` and writes `gfx/palettes.json`. Also find the PPU `$3F00` writer and confirm the palette upload path.
- [ ] Ph4 assets 2/5 — backgrounds (format known, no compression — see "Compression", session 10): decode `MetatileAttr` `31:EDE5`/`EEEF` and the bank 9 `$BE00` visited-screen substitution tables; check which grid ids map to which banks and whether `$80` (no screen) is ever read. Write `tools/render_screen.py` (screen id → packed bank+page `31:EF36` → raw 16×15 metatile page in banks 0-7 → metatiles from banks 8/9 with tileset `$67` → BG CHR R0/R1 + k1 palettes → `gfx/screens/screen_NNN.png`) and `tools/render_level.py` (whole area from the `$6300` screen grid via `area_dump` k0 → `gfx/areas/area_NNN.png`). Render every area `area_dump.py` lists, plus the title/menu `W H` rectangles from banks 17/18.
- [ ] Ph4 assets 3/5 — sprites: extend `meta_dump.py` to render each area's enemy sets with the correct R4/R5 CHR (`LoadAreaChrRecord`) and sprite palettes, and export animation frame sequences (frame mask, ÷8 timing) to `gfx/sprites/` + `gfx/sprites.json` (id → frames, offsets, attrs, tiles).
- [ ] Ph4 assets 4/5 — audio + text: write `tools/sound_export.py` that converts every music track and SFX (via `sound_dump.py` decoding) to `web/data/sound.json` (per-channel event lists: notes, lengths, duty, envelope, vibrato params) so the web port can synthesize them; optionally render WAV previews to `gfx/audio/`. Text: build on the "banks 21-23 text" task above; find the font tiles and any text/string tables (character encoding → `tools/text_dump.py`, `gfx/text.txt`); if the game has no text beyond tile graphics, record that.
- [ ] Ph4 assets 5/5 — `web/catalog.html` asset browser (plain HTML+JS, `python3 -m http.server`): CHR banks, palettes, sprites with animation playback per area, rendered screens/areas, sound player (WebAudio from `sound.json`), text. All data loaded from `gfx/*.json`/`web/data/`. Record the asset formats and output files in REVERSE.md (Intermediate Output Files + format specs).
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
- [ ] Ph3: identify controller read routine and input RAM vars
- [x] Ph3: identify sound driver bank + entry points — covered by session 2 (see above)

### Web Port Fixes

### Documentation

- [ ] Final docs refresh: update `docs/architecture_exe.md` and `docs/architecture_web.md` to match the finished port (module graph, EXE→web mapping table, state machine), and check every REVERSE.md section is linked from one of them.
