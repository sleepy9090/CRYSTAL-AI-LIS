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
6. `SwitchBank16($0C)` → banks 24/25 at `$8000`; `JSR $8020` (init in bank 24). *(Corrected session 5: earlier note said n=6/bank 12 — the immediate at `31:F320` is `A9 0C`.)*
7. State `$40=2`, clears `$0700-$07FF`, PPUCTRL=`$88` (NMI on, sprites @ `$1000`), `$07FE=9`, CLI.
8. Warm-boot check: if `$0182-$0184` == `"USO"` skip to `F39F`; else cold init (`F1CD`, write signature, `FE80`, `$0F` = (`$43`==`$C0`), `SwitchBank16(9)` → `JSR $8028` in bank 18).
9. `F0A4`, then copy WRAM `$7D00-$7EFF` → `$6480-$657F`, `JMP $C900`.

**Bank switching (`30:C40E` SwitchBank16)** — A = 16KB bank index *n*; maps 8KB banks `2n+1` (via `C427`, $A000 window) and `2n` (R6, $8000). `$6E` = current 2n, `$50` = select-register shadow. So the game treats PRG as 16KB pairs at `$8000-$BFFF` with `$C000-$FFFF` fixed. Bank-switched entry points are at fixed offsets (`$8020`, `$8028` …) → likely a jump table at the start of each swappable bank.

**Bank-switch primitives (verified session 5)** — three entry points share one body:

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
| 12 | 24/25 | `F320` (Reset) | `$8020` | boot init (sound driver init? — check) |
| 13 | 26/27 | `C7A0`(JMP), `C7E2`, `C80C`, `CB79`, `CCD1`, `CD82`, `DA3C`, `DAC2`, `E43C`, `E461`, `E47F`, `E547`, `E5E8` | `$8C3D`/`$8C49` (fade, A=3/4), `$8F6E`, `$9B45` (X=1), `$9824`, `$9897`/`$98A8` (X=0/1), `$9CEF` | most-used: palette/fade + per-player (X=0/1) HUD/screen routines |
| 14 | 28/29 | `C1B4`, `CB71`, `E4CD`, `FF92` | `$800E` (all four) | per-frame tick (`$08` frame ctr, `DEC $07FF`) — sound driver candidate |

Remaining sites (checked):
- `30:DA6D` `LDA #8 / JMP` → n=8 tail-call.
- `31:E3D4` `LDA #6 / JMP` → **n=6, banks 12/13**; called right after the n=7 entity calls (`E34E`, `E38E…`) → banks 12/13 are the "home" pair that entity code returns to (likely main gameplay code).
- `31:EF43` — n = top 2 bits of a pointer hi byte (`ROL×3 / AND #3`), low 6 bits `|$80` → `$11` → **pointer-encoded bank**: n 0-3 (8KB banks 0-7) + `$8000-$BFFF` address. Explains banks 2/3, 6/7 (n=1/3) having no immediate: they're reached only through these packed pointers (data, likely graphics/map streams).
- `30:CAD8` — state-1 (`CAB6`) substate dispatcher: `$41` → handler `SubstateHandlerTable` `30:CADE` (→`$10/$11`) and n from `SubstateBankTable` `30:CB2E` (substate 3→n=9, 12/13→n=7, all others n=0), then `JMP ($0010)`.

Never mapped by any found path: banks 22/23 as a pair (n=11; 23 only via `SetR7($17)`), 30/31 (fixed).

**`30:C7D6` FadeSaveZpToWram** — copies ZP `$0A-$29` → `$6000`, `SwitchBank16(13)`, `26:$8C3D(A=4)`, waits 30 frames on `$09`, `26:$8C49(A=4)`, `SwitchBank16(8)`, restores `$6000`→`$0A`. `$8C3D` sets `$11=0,$19=$10`; `$8C49` sets `$11=$40,$19=$F0` → opposite step directions (fade out/in, tentative).

**Main loop (`30:C900`)** — spins on `$09` (set by NMI), then dispatches `MainStateTable[$40]` (`30:C91F`) through the ZP trampoline `$10: JMP abs`, clears `$09`, repeats. States: 0=`C957` 1=`CAB6` 2=`C931` (initial) 3=`C939` 4=`C944` 5=`C94C`.

**NMI (`31:F3B6`)** — returns immediately if `$60≠0`, or if `$06≠0` (lag: sets `$07`). Else OAM DMA from `$0200`, copies `$02-$05` → `$07D8-$07DB`, special handling for `$51` = 7/9 (to trace).

**IRQ (`31:F443`)** — ack/re-enable MMC3 IRQ, `JMP ($0054)`. Handler table `IrqHandlerTable` at `31:F433` (8 entries) selected by `SetIrqHandler` (`31:F424`, A=index). Handler 0 (`F455`) does a delayed mid-frame scroll split from `$07D8`.

### Data Structures

*(none yet)*

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
- [x] Ph3: confirm `30:C427` (R7 setter) and document `SwitchBank16` fully; find all `LDA #n / JSR C40E` call sites → map which 16KB bank holds which subsystem — done session 5: SetR6 `C418`/SetR7 `C427`, far-call trampolines `FF80-FFE2`, full n→bank table in Key Findings; corrected reset bank (n=$0C → 24/25). New `tools/bank_calls.py` written (needs run-permission approval; not yet executed — `search_bytes.py --disasm --context 14` used instead)
- [ ] Ph3: verify bank 28 `$800E` (n=14) and bank 24 `$8020` (reset init) — which is the sound driver? Check for `$4000-$4015` writes in banks 24-29
- [ ] Ph3: decode `30:CADE`/`CB2E` substate tables (length, name each state-1 substate handler)
- [ ] Ph3: trace `31:EF38` packed bank+pointer decoder — find the pointer tables that feed it (banks 0-7 data streams)
- [ ] Ph3: decode jump tables at start of swappable banks (`$8000-$802F` in banks 12, 18, …)
- [ ] Ph3: trace the 6 main states (`C957 CAB6 C931 C939 C944 C94C`) — name them (title/menu/gameplay/…)
- [ ] Ph3: trace NMI fully (`31:F3B6`-`F421`): PPU update queue, `$51` modes 7/9, scroll writes, sound driver call
- [ ] Ph3: decode IRQ handlers 1-7 (`IrqHandlerTable`) — which screens use which split
- [ ] Ph3: identify controller read routine and input RAM vars
- [ ] Ph3: identify sound driver bank + entry points
- [ ] Ph3: trace cold-init subs `F1CD`, `FE80`, `F0A4` and the WRAM save layout (`$6000-$7FFF`, `$7D00` block)
- [ ] Ph4: build `tools/extract_tiles.py` — dump all 128 CHR 1KB banks to `gfx/` with grayscale palette
- [ ] Ph4: locate palette data (writes to PPU `$3F00`) and build `tools/palette_dump.py`
- [ ] Ph5: build `tools/decode_tables.py` and `tools/check_bank_refs.py`
- [ ] Ph5.5: decide whether a scriptable 6502+MMC3 emulator (`tools/emu/`) is needed

### Web Port Fixes

### Documentation

- [ ] docs/architecture_exe.md
- [ ] docs/architecture_web.md
