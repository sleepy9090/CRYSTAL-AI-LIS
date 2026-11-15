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
| `0x00010` | `0x3C00F` | 240KB | unclassified | PRG banks 0-29. Banks 2/3, 6/7, 8/9…: identical tail patterns hint at shared data tables per bank pair |
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

Never mapped by any found path: banks 22/23 as a pair (n=11; 23 only via `SetR7($17)`), 30/31 (fixed).

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
e6 e7                    -> $07E6, $07E7 (meaning TBD; flag-item handler overrides both with ($6C&7)+9)
R4 R5                    -> $07F4/$07F5 sprite CHR (1KB banks for tiles $80-$FF); $FF seen in 30+ areas (meaning TBD — likely "keep")
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

`tools/area_dump.py [list|chr|spawns] [areas]` decodes all of it. `chr` mode groups areas by (R4,R5): 120+ distinct pairs; most common `4A/4A` (28 areas, `$BC-$FB` range), `67/66` (12 areas: `11 23 24 26 27 2D 44 48 49 4B 4E 4F`), `FF/FF` (10). Verified: `meta_dump.py png --chr 40,42,67,66` renders the blue humanoid enemy ids (`$58-$5F`, `$6B-$6F`, `$8C-$8F`) cleanly where the old `50,51` placeholder scrambled them → `gfx/metasprites_area67_66.png`.

**Port note:** reimplement as: for slot in order[rot&3]: compute screen pos, resolve record (follow one `FF` with mirror), emit N sprites of frame f; then hide rest; then orbit ring. Sprite limit is 63 (OAM `$04-$FF`) minus 8 orbit slots overlapped — the orbit writes `$E0-$FC` unconditionally after the hide pass, overwriting objects there.

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
| `tools/area_dump.py` | `list`/`chr`/`spawns [areas]` — decodes `AreaPtrTable` `12:9201` (252 areas): `$07E6/$07E7`, R4/R5, per-slot spawn entries (session 7, run + verified) |
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
- [ ] Ph3: area follow-ups — meaning of `$07E6/$07E7` and R4/R5 = `$FF`; `ObjInit` `30:C25D` type dispatch; bank 4/5 indirect spawn records (`$80F0+`); what sets `$6C` (area transitions); `$18` reader
- [ ] Ph3: decode `30:CADE`/`CB2E` substate tables (length, name each state-1 substate handler)
- [ ] Ph3: trace `31:EF38` packed bank+pointer decoder — find the pointer tables that feed it (banks 0-7 data streams)
- [ ] Ph3: decode jump tables at start of swappable banks (`$8000-$802F` in banks 12, 18, …)
- [ ] Ph3: trace the 6 main states (`C957 CAB6 C931 C939 C944 C94C`) — name them (title/menu/gameplay/…)
- [ ] Ph3: trace NMI fully (`31:F3B6`-`F421`): PPU update queue, `$51` modes 7/9, scroll writes, sound driver call
- [ ] Ph3: decode IRQ handlers 1-7 (`IrqHandlerTable`) — which screens use which split
- [ ] Ph3: identify controller read routine and input RAM vars
- [x] Ph3: identify sound driver bank + entry points — covered by session 2 (see above)
- [ ] Ph3: trace cold-init subs `F1CD`, `FE80`, `F0A4` and the WRAM save layout (`$6000-$7FFF`, `$7D00` block)
- [ ] Ph4: build `tools/extract_tiles.py` — dump all 128 CHR 1KB banks to `gfx/` with grayscale palette
- [ ] Ph4: locate palette data (writes to PPU `$3F00`) and build `tools/palette_dump.py`
- [ ] Ph5: build `tools/check_bank_refs.py` (`tools/decode_tables.py` done session 3)
- [ ] Ph5.5: decide whether a scriptable 6502+MMC3 emulator (`tools/emu/`) is needed

### Web Port Fixes

### Documentation

- [ ] docs/architecture_exe.md
- [ ] docs/architecture_web.md
