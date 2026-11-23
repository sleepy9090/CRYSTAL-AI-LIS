# re-c NES RE project

Reverse-engineering + web port of `rom/re-c.nes` (NES, MMC3 mapper 4, 256KB PRG / 128KB CHR, battery WRAM).
Use the `/re` skill workflow: REVERSE.md is the source of truth; Next Tasks drives work.

## Platform conventions
- CPU: 6502 (2A03). Opcode DB in `tools/instruction_set.py`; shared ROM/label helpers in `tools/nesrom.py`.
- **Bank numbering = 8KB PRG banks 0-31.** Bank 30 fixed at `$C000`, bank 31 fixed at `$E000`.
  The game swaps 16KB pairs (2n, 2n+1) into `$8000-$BFFF` via `SwitchBank16` (`30:C40E`, A=n).
- Address notation: `BB:AAAA` (e.g. `31:F2A4`). File offset = `0x10 + bank*0x2000 + (addr & 0x1FFF)`.
- labels.csv format: `bank,addr,name,comment` — addr hex without prefix; RAM/WRAM/regs use bank `*`.
- CHR: 128KB = 128 x 1KB CHR banks (MMC3 CHR mode 0 set at reset: 2KB @ $0000, 1KB @ $1000).

## Tools (prefix `python3 tools/`)
- `nes_header.py [rom]` — header + per-bank summary
- `dis.py <bank> <addr> [nbytes] [--stop]` — banked disassembler, auto-loads labels.csv, names HW regs
- `compare_frames.py A.csv B.csv [--cols core,snd,pal,obj|prefix] [--frames LO-HI] [--shift N]` and/or
  `A.png B.png [--diff OUT.png ...]` — diff a tools.emu run against the web port (exit 1 on any difference)

## Web port tests (`node web/test/*.mjs`, Node 18+, no npm packages)
- `node web/test/run.mjs --frames N --keys 250:START 300:A+RIGHT:8 --compare web.csv --dump-screen web.png`
  runs the port headless. `--keys`/`--compare` use the tools.emu syntax and columns; frame numbers are
  tools.emu `frame_no` numbers because `--frame0` defaults to 214 (the port starts at the first MainLoop pass,
  which tools.emu reaches at f214). So `python3 -m tools.emu --frames 214+N --keys <same> --compare emu.csv`
  then `python3 tools/compare_frames.py emu.csv web.csv` lines up with no `--shift`.
- `python3 tools/web_compare.py [--frames F,F,..]` = both runs over the canonical keys at the `ref/` frames →
  `gfx/compare/web.json` + pairs; then `python3 tools/comparison.py report` refreshes COMPARISON.md's web column / §6.
- Button names map straight to the ROM-order byte (A=b7 … RIGHT=b0); only raw tools.emu numeric masks
  (A=b0) need `rev8()` from `web/test/harness.mjs`.
- Checks: `core_check.mjs`, `ppu_check.mjs`, `serve_check.mjs`, `world_check.mjs`, `sprite_check.mjs`, `obj_check.mjs`,
  `sound_check.mjs` (driver vs `tools/emu/sound_capture.py`), `apu_check.mjs` (APU synth vs 2A03 formulas; `--wav ID OUT.wav`)
  `wake_check.mjs` (new game with no held button → wake-up cut-scene, vs `web/test/wake_ref.csv`)
  `giveitem_check.mjs` (GiveItem: town02 entry trigger + item pickup state, vs `web/test/giveitem_ref.csv`)
  `itemuse_check.mjs` (ItemUseMenu sub_state 6 + ShowPendingMessage: herb / fail / Opel states vs `web/test/itemuse_*_ref.csv`)
  `pause13_check.mjs` (substate 13 PauseWait entry, unreachable in the ROM: forced 13 waits for START and returns to 8)
  `teleport_check.mjs` (SubstTeleportMenu 30:DB28: Warp Boots + magic 4 vs `web/test/teleport_*_ref.csv`, plus cancel / locked / same town / boss lock)
  `telepathy_check.mjs` (magic 3 SubstTelepathySelect 30:DD6B + reply: per-phase shifted rows vs `web/test/telepathy_ref.csv`, reply ids, hint, cancel)
  `changeform_check.mjs` (magic 7 SubstChangeForm 30:DEC1 + ChangeFormApply: rows vs `web/test/changeform_ref.csv` at shift 0, form bits, MP, LEFT wrap, cancel)
  `cure_check.mjs` (magic 5 / stone SubstCureMagic 19:BB39: rows vs `web/test/cure_ref.csv` at shift 0 through msg 32.2, status cleared, form bits kept)
  `formrevert_check.mjs` (magic in a changed form, SubstFormRevert 19:BB9D: rows vs `web/test/formrevert_ref.csv` at shift 0, R2 by shield, form bits cleared)
  `swordraise_check.mjs` (sub_state 26 SwordRaisePose 19:BC04: rows vs `web/test/swordraise_ref.csv` at shift 0, pose / CHR during, meta + overlay restored)
  `mesia_check.mjs` (sub_state 27 SubstMesiaReunion 19:BC6B: rows vs `web/test/mesia_ref.csv`, post-WaitFrame cells may be 1 frame late, 4 `$C2` spawns, sub_state 7 + GiveItem slot `$1F`)
  `outofmp_check.mjs` (sub_state 28 SubstOutOfMp 30:CC17: magic 2 with MP 0 → msg 32.14 rows vs `web/test/outofmp_ref.csv` at shift 0, handler writes, area `$5F` irq_mode clear)
  `finalscene_check.mjs` (sub_state 30 SubstFinalSceneReset 19:BDF2: rows vs `web/test/finalscene_ref.csv`, LATE / EARLY 1-frame cells + mid-PaletteBuild flash/fade rows, entry clears, 5 messages, main_state 4 at f3604)
  `bossflee_check.mjs` (BossDefeated hook BossFleeSpeech 15:BA98, area $28: rows vs `web/test/bossflee_ref.csv`, f2537 cells may be 1 frame late, record + kept slots)
  `bossa6_check.mjs` (BossDefeated hook BossHookA6 15:B8FD, area $A6: rows vs `web/test/bossa6_ref.csv` f2505-2535 at shift 0, FullScreenDraw lag rows `FSD_LAG_WF`, rendering off/on)
  `bosse2_check.mjs` (AI $62 BossAiE2 15:A675, area $1A Insect-Flute boss: rows vs `web/test/bosse2_ref.csv` f2524-3002 at shift 0, 8 PARTIAL mid-PaletteBuild / mid-BuildParts cells = previous frame, intro overrun `yield SPIN`;
  s218: 7 `irq_mode`-7 lag-model overruns from f2915 + 9 PARTIAL `pos_x/y` in the player's 3-frame wait passes)
  `ai70msg_check.mjs` (AI $70 phase 1 table end 27:BB1A, scenario `final_locked`: messages $1B.05-07 via ShowMessage, rows vs `web/test/ai70msg_ref.csv` f2861-3179 at shift 0 with keys +1 frame, no snd columns)
  `ai70ph3_check.mjs` (AI $70 phases 3-6 in area $5F from `restore5f.json`: rows vs `web/test/ai70ph3_ref.csv` f2507-2757 at shift 0, listed PARTIAL / LATE / EARLY cells; lost NMI after the music-$14 start in the HUD IRQ + phase 4-6 overruns; s220: from the lag model in `irq_mode` 2, `m.lagMode2 = false` = old heuristic)
  `kill_check.mjs` (real kills, scenarios `gold_class_3` / `kill_levelup` from `scenario_state` +28: rows vs `web/test/killgold_ref.csv` / `killlevelup_ref.csv` at shift 0, per-press port key frames from `tools.emu --watch 004B`, 2 LATE + 4 PARTIAL mid-sprite-pass oam cells)
  `fanskip_check.mjs` (fan-turret frame, scenario `fan_skip` +12: AiShootCheck's PLA PLA pops ObjUpdateAll at the attack start f2679, slot `$1C` skips its AI; rows vs `web/test/fanskip_ref.csv` f2509-2807 at shift 0 incl. snd; s178: area-`$02` entry from f2719 with 66 listed PARTIAL / LATE / FSD-cursor cells)
  `group08_check.mjs` (area `$08`'s own enemy group, scenario `group_08` +12: AI `$28` AiAimShooter 27:AB61 + 3 shot hits; rows vs `web/test/group08_ref.csv` f2507-2985 at shift 0 incl. snd; port keys per press/release = pass frames of `tools.emu --watch 0043`)
  `group09_check.mjs` (area `$09`'s group, scenario `group_09` +12: AI `$24` AiChase ×4 + `$20`, 7 contact hits → death sub_state 3; rows vs `web/test/group09_ref.csv` f2507-3504 at shift 0 incl. snd, listed LATE / byte-granular PARTIAL death cells)
  `group05_check.mjs` (area `$05`'s group, scenario `group_05` +12: AI `$20` type `$52`, contact hit, sword kill f2662 → puff + coin `PickupGold`, respawned walkers;
  rows vs `web/test/group05_ref.csv` f2507-2985 at shift 0 incl. snd, no listed cells; group scenarios need `$0711` sword + `$03E1` attack poked or swings do no damage)
  `group11_check.mjs` (area `$11`'s group, scenario `group_11` +12: AI `$20` `$61` + poisonous AI `$24` `$63`, 2 hits, `PlayerPoisonRoll` msg `$19` closed by A, poison tick;
  rows vs `web/test/group11_ref.csv` f2507-2984 at shift 0 incl. snd, 9 listed PARTIAL cells (ShowMessage close-loop OAM, 2 before `OamClipTop`; `$34-$37` scratch))
  `group18_check.mjs` (area `$18` town, scenario `group_18` +12: 6 h1 NPCs AI `$30` + trigger p=`$A6`, walk-in talk f2562-2769 closed by A ×2;
  rows vs `web/test/group18_ref.csv` f2508-2984 at shift 0 incl. snd, 22 listed `sub` cells (probe on the trigger: port 7 at pass end, emu 8 after `GiveTrigger`))
  `group29_check.mjs` (area `$29`'s group, scenario `group_29` +12: AI `$25` type `$65` ×4 + respawns, sword 2 poked (elem 1 blocks sword 1), 2 contact hits,
  kills f2665 / f2933 + coin AI `$7B`; rows vs `web/test/group29_ref.csv` f2508-2984 at shift 0 incl. snd, no listed cells)
  `group6b_check.mjs` (area `$6B`'s group, scenario `group_6B` +12: AI `$26` type `$78` (elem 2 → sword 1), 4 hits + kill f2897, no shot;
  rows vs `web/test/group6b_ref.csv` f2508-2984 at shift 0 incl. snd, no listed cells; a `group_probe.py` row F = `--keys` F-1 at L261 pad reads)
  `group40_check.mjs` (area `$40`'s group, scenario `group_40` +12 with `place_player` (entry ledge walled off): AI `$27` type `$6A` (elem 2 → sword 1),
  2 hits + kill f2824 + coin; rows vs `web/test/group40_ref.csv` f2509-2985 at shift 0 incl. snd, no listed cells)
  `group7c_check.mjs` (area `$7C`'s group, scenario `group_7C` +12 with `place_player` + defences `$0401`/`$0400` poked: AI `$29` `AiBurrower` type `$82`,
  hittable only while surfaced (`obj_coll` `$11`), 4 hits + kill f2847, contact hit f2602; rows vs `web/test/group7c_ref.csv` f2506-2984 at shift 0 incl. snd, no listed cells)
  `group28_check.mjs` (area `$28`'s group, scenario `group_28` +12 with `place_player` + mid defences `$0401`/`$0400` = `$28`/`$20`: AI `$2A` `AiLunger` type `$64` ×3,
  2 hits + kill of slot `$12` f2807, 4 player hits by contact / lunge shots (AI `$10`); rows vs `web/test/group28_ref.csv` f2507-2984 at shift 0 incl. snd, no listed cells)
  `group7f_check.mjs` (area `$7F`'s group, scenario `group_7F` +12, max defences: AI `$2E` type `$84` (elem 3 → sword 3, `$03E1` = `$14`), contact hit f2623,
  4 hits + kill of slot `$0E` f2935 + coin AI `$7B`; rows vs `web/test/group7f_ref.csv` f2506-2984 at shift 0 incl. snd, no listed cells)
  `group84_check.mjs` (area `$84`'s group, scenario `group_84` +12, no defence pokes: AI `$40` `AiWander16` type `$55` ×3 (area `$2B`'s one is out of reach),
  kills f2715 / f2877 + contact hit f2852; rows vs `web/test/group84_ref.csv` f2506-2984 at shift 0 incl. snd, no listed cells)
  `group84b_check.mjs` (AI `$21` `AiShooterWalk` type `$85` in area `$84` slot `$11`, scenario `group_84b` +12 with `place_player`, sword 4 (elem 5), max defences:
  4 hits + kill f2655; rows vs `web/test/group84b_ref.csv` f2506-2984 at shift 0 incl. snd, no listed cells)
  `group88_check.mjs` (AI `$4E` `AiTurret` type `$87` in area `$88` slot `$0D`, scenario `group_88` +12 with `place_player` beside it, sword 1, max defences:
  9 hits + kill f2617, 2nd turret refills `$0E`; rows vs `web/test/group88_ref.csv` f2506-2984 at shift 0 incl. snd, no listed cells)
  `group6e_check.mjs` (AI `$3A` `AiNpcBossReveal` type `$2A` in area `$6E` slot `$0E`, scenario `group_6E` +12 with `place_player` below the NPC, sword 1:
  3 hits → msgs `$10.$10-$12` closed by B, reveal into type `$7D` f2858; rows vs `web/test/group6e_ref.csv` f2509-2987 at shift 0 incl. snd, 18 PARTIAL close-loop `oam_crc` cells;
  `oam_dump` `--all` shows hidden entries)
  `groupb2_check.mjs` (AI `$41` `AiShellOnHit` type `$92` in area `$B2` slot `$0D`, scenario `group_B2` +12, sword 4 (elem 7), max defences: hit f2666 → shell `$41` until f2742,
  slot `$14` kill f2870; rows vs `web/test/groupb2_ref.csv` f2508-2986 at shift 0 incl. snd, 6 PARTIAL mid-pass `oam_crc` (byte-granular) + 2 `pos_x/y` cells; area renders are `gfx/areas/area_NNN.png`, NNN decimal)
  `thunder_check.mjs` (AI `$1A` ThunderStorm `30:CD8A` = sword-4 charge-3 shot `$0F`, scenario `thunder_B2` +12 (group_B2 + ball 8 → `sword_power` 2), B held 120:
  rows vs `web/test/thunder_ref.csv` f2508-2945 at shift 0 incl. snd, 240 LATE (pass resumes in vblank after a SPIN wait) + 29 PARTIAL mid-palette-write cells, rebuilt as byte mixes)
  `group81_check.mjs` (area `$81`, scenario `group_81`: the whole ref f2506-2984 (s215 lag model + s216 exact `EnemyKilled` / `PickupGold` cycles; tools.emu overruns 18/480 frames there); `--last F`)
  `python3 -m tools.emu.span_cost S.state N ENTER EXIT [ENTER EXIT ..] [--from F] --keys ..` (s216: CPU cycles between two PCs `BB:AAAA` per occurrence, entry / exit line:dot; to check exact-cycle counts in the port)
  `group5c_check.mjs` (AI `$5C` `AiPatrolGunner` type `$A2` slot `$16`, scenarios `group_59` / `group_5A` with `place_player`: gunner out of sword reach, attack + shot hits;
  s215: the whole refs `web/test/group59_ref.csv` f2507-2985 / `group5a_ref.csv` f2517-2995 incl. 11 / 6 CPU overruns from the lag model; `place_player` y = screen row : lo, not pixels)
- s215 lag model (`web/src/game/lagmodel.js`, `irq_mode` 0 SubstPlay passes; `m.lagModel = false` = off): `node web/test/lag_probe.mjs STATE.json LAST [--all] [--splits] [--out F] keys`
  (per pass: start line, estimate, HUD part, budget, margin, LAG, stage parts) + `python3 tools/lag_model_check.py PROBE.txt COST.txt [--all]` (vs a `pass_cost --split-int` table:
  bias / rms, start-line misses, LAG disagreements; s217: `tools/lag_stage_diff.py PROBE.txt COST.txt [--slots]` = per-stage port − emu); `pass_count.mjs` reads `m.lagCnt` (`--no-lag` for the pre-s215 runs). Refit: `pass_fit.py` / `int_fit.py` on full aligned runs.
  `python3 -m tools.emu.pass_probe S.state N [--from F --to F] [--slots] --keys ..` (main-loop top / routine start lines + NMI `frame_ready` per frame: `frame_ready 01` = lag frame;
  check a group scenario's run with it before writing the ref); `python3 tools/keyseq.py 2505:DOWN:2 '2569:DOWN:6,+6:B:4*26/16'` (expands repeated key patterns);
  `tools.emu ... --watch 0043 | python3 tools/pad_watch_keys.py - --last F` (port key items per press / release)
  `python3 tools/emu/group_probe.py S.state --slots 0D,13 [--every N | --changes] --seg NONE:20 UP:25 'B:4,NONE:8*30' ..` (player + slot x/y/HP/AI per frame, to plan a group scenario's keys)
  `python3 tools/cell_class.py A.csv B.csv [COLS] [LO-HI]` labels each shift-0 cell LATE / EARLY / PARTIAL (`emu-odd`); `csv_cols.py` ranges take `LO-HI:STEP`
  `oam_dump.mjs STATE.json F,..` / `python3 -m tools.emu.oam_dump S.state F,..` (OAM entries per row); `python3 tools/csv_cols.py A.csv B.csv COLS [LO-HI]` (two CSVs side by side)
  `ram_watch.mjs STATE.json N ADDR,..` (port writes to RAM addresses with the JS caller, = `tools.emu --watch`); `python3 -m tools.emu.sound_cost_probe scene|music` (SoundUpdate cycle cost / HUD busy window)
  `event_check.mjs` (EventActionTable acts 1 `30:D5A4` / 7 `30:D5E6` / 8 / 21 / 23 (s204) / 2 / 15 / 28 (s205) / 11-13 / 19 / 20 (s206) from `web_state.py --force 30:D552:0022=NN --until <act>` states;
  s207: TalkActionTable acts 16 / 19 / 20 / 22 / 25 / 26 as `run('tDD')` → `talkDD_ref.csv`, entered by also forcing `30:D560:0010=LO` / `0011=HI` (`tools/emu/talk_states.sh`)
  (`ENTRY_POINTS`; `--force` repeats, e.g. `event19b` adds `30:D552:04AE=00`): rows vs `web/test/eventDD_ref.csv` (DD = act in decimal) at shift 0, act 1 listed LATE / PARTIAL / `$F2`-trigger `sub` cells, act 21 2 LATE + 8 PARTIAL)
  `pending_probe.mjs STATE.json LAST [keys]` (frame where a state's `ENTRY_POINTS` routine returns)
  `finalboss_check.mjs` (AI $6B AiBowOfTruth9E 15:B2DA final boss, scenario `final_boss_idle`: rows vs `web/test/finalboss_ref.csv` f2505-3220 at shift 0, 8 PARTIAL pal cells + LATE music f2590 = previous frame; setup RTS X = 0 re-runs slots 1-$1E;
  s218: the dive f3146-3218 = 27 lag-model overruns, EARLY cells f3167 / f3217 = next frame (lagging pass crosses tools.emu's row capture))
  `nmimode4_check.mjs` (s208, dead `irq_mode` 4: `NmiMode4` `31:F782` + IRQ H4 `IrqRowStretch` / H5 `IrqOffSound`, states forced with `web_state.py --force 30:C900:0051=04
  --force 30:C900:0058=NN` (08 / 01 / 00): rows vs `web/test/nmimode4_NN_ref.csv` f2354-2592 at shift 0, port keys = emu keys +1 frame, screens as CRC32
  from `python3 tools/png_crc.py emu_FFFF.png` (decoded RGB, = node `crc32(fbToRgb(..).rgb)`), split lines every `$58`+2)
- s210 lag study: `python3 -m tools.emu.pass_cost S.state N [--slots] [--out F] --keys ..` (cycles per main-loop stage per pass,
  pass-end line, LAG; `--keys` last), `python3 tools/pass_cost_stats.py T.txt` (stage stats, LAG threshold window),
  `node web/test/pass_count.mjs STATE.json LAST [--out F] keys` (port rd/wr per frame) + `python3 tools/pass_fit.py COST COUNTS`;
  s211: `pass_cost --split-int` (NMI / IRQ cycles out of the stages, `int N` column) + `pass_count.mjs --stages`
  (rd/wr/cpuRd per `m.passStage`) + `pass_fit.py C1,C2 N1,N2 --stages [--skip-lag] [--save J | --coef J [--fit-new]] [--show S|int] [--from F --to F]`
  (s218: `--fit-new` fits only AI ids missing from `--coef`; `--to` = up to a run's first misaligned lag; `pass_count.mjs` rows end in `im=` = `irq_mode`, `int_fit.py --im 0,7`;
  lag model covers `irq_mode` 0 + 7 (+ 2 since s220, AI `$70` flash: `m.lagMode2`), `m.lagMode7 = false` = no mode 7;
  s219: `node web/test/lag_coef_dump.mjs OUT.json` = `LAG_COEF` as a `--coef` file, for `--fit-new` on new runs; run `lag_probe.mjs`
  without `--all` before `lag_model_check.py` (`--all`'s `est 0` hit-stop rows stop the join))
  (per-stage fit; `--coef` = out-of-sample check). The sandbox can't `head` files in /tmp: read them with the tools.
  s212: `sh tools/passfit_runs.sh [--no-emu]` = the whole study ($81 + $59 fit, $5A cross-check) with `pass_cost --slots`
  + `pass_fit.py --ai` (per-AI dispatch fits from `pass_count.mjs` `XX:YY` tokens) and `pass_cost`'s `padretry` (pad re-reads).
  s214: `pass_cost --split-int` rows end in `ints I142=..,I189=..,N241=..` (every interrupt to the next top), `pass_count.mjs` has a `snd`
  bucket (SoundUpdate); `python3 tools/int_fit.py COST COUNTS [--save|--coef F]` (HUD IRQ / NMI cost fits), `python3 tools/lag_threshold.py COST..`
  (LAG iff tot − overrun NMI > (503 − start line) × 341/3; full-run keys from `keyseq.py`, see `group81_check.mjs`).
  s213: `m.cyc` = exact ROM cycles of modelled code (`obj/collide.js` record loop + `ObjOverlapScan`, `play.js` `substPlayTail`),
  `pass_count.mjs` prints it as `colc` (+ `colh` = hit handlers / `AreaSpawnEntry`); `pass_fit.py` uses it for the col stage (rms 1).
- s221 docs: `python3 tools/module_summary.py [web/src]` (per web module: header, imports, exports, ROM addresses cited) and
  `python3 tools/doc_section_links.py --levels 2,3,4` (REVERSE.md sections not named in `docs/architecture_*.md`)
- `python3 tools/ram_byte.py DUMP.bin ADDR[,ADDR..] [DUMP2.bin ..]` — bytes of `--dump-ram` files (instead of `od`)
- `python3 -m tools.emu.split_probe S.state N [--from F --to F] [--hist] [--mapper]` — line:dot of PPU register (+ MMC3 CHR) writes
  on visible lines per frame / as a histogram; `python3 tools/png_diff_lines.py A.png B.png` — differing x runs per scanline.
  Mid-line IRQ writes in the port: `ppu.timed(dot, fn)` inside a split hook (s209, H1 / H2 / H3 use it).
- `python3 tools/run_checks.py [SUBSTR..]` — every `web/test/*_check.mjs`, one line each + FAIL lines (exit code not hidden by `tail`);
  `node web/test/state_meta.mjs J.json ..` — a state JSON's frame / frame0 / line / until + sound RAM.
  Since s176 `loadState` rebuilds the first frame's IRQ split (`loadSplits`), so `snd*` columns match after loads.
- `python3 -m tools.emu.scenario_state SCENARIO OFF --out J.json --save-state S.state` — port state from a `tools/emu/scenario.py`
  run at the main-loop top of scenario frame OFF (spawns due that frame applied first); then `tools/web_state_compare.py`.
- `python3 tools/phase_compare.py A.csv B.csv --cols C LO-HI:SHIFT ...` — compare_frames per frame range with its own shift.
  A state exported at a substate handler's first instruction needs an `ENTRY_POINTS` entry in `states/play.js`, or the load re-dispatches (double `DEC $08`).
  (each exits 1 on failure). `world_check.mjs` needs `gfx/areas/` from `python3 tools/render_level.py --all` and the RAM dumps in
  `web/test/world_frames/` (regenerate per its README).
- Routine-level checks: capture RAM at a ROM routine's entry/exit in tools.emu (`tools/emu/sprite_capture.py`,
  `real` = hooks during a run, `fuzz` = random RAM + `nes.call`), replay each case through the JS port and
  diff the whole RAM (`sprite_check.mjs`). Fuzz cases catch the paths a real run never takes.
  For any other routine use `tools/emu/routine_capture.py` (add it to `ROUTINES`, RAM + WRAM, caller frame under the
  return address) + `obj_check.mjs` (`RUN` map). ROM tables are read through `web/src/game/prg.js` `cpuRd` (`prg.bin`).

## Rules
- Do NOT try to identify the game title (no CRC/database lookups, no title-screen hunting for that purpose). Call it "re-c".
- Original ROM stays at project root `re-c.nes`; tools read `rom/re-c.nes`.
- Write .py tools into tools/, never inline `python3 -c`. No third-party disassemblers.
