# re-c C# port — Tasks

Ordered; do the first unchecked task. Read `docs/architecture_cs.md` first (design, iterator rules, verification,
REA tracking). Source to translate: `../web/src/` (one `.cs` per `.js`, `// js: <module> <name>` tag above every
ported member). Oracle: the web port — `python3 tools/cs_vs_web.py` (built in T1) must show no difference.
Coverage: `python3 tools/cs_coverage.py --module <path>` against `docs/obligations.csv` (REA inventory).
Build: `dotnet build ReC.slnx -c Release` with **0 warnings**; tests: `dotnet run --project tests/ReC.Tests -c Release`.
Unported callees are stubs that log `not ported: <module> <name>`, so the solution always builds.
After a task, write what was done (files, checks run, results) into that task's line and tick it.
If parts remain, write them into the same task and leave it unchecked (no new follow-up tasks).

## Tasks

- [x] **Done (setup):** T0 project skeleton plan: docs/architecture_cs.md, REA inventory `docs/obligations.csv`
  (1,046 named callables of ../web/src, evidence `ev_2d5f769c…`), `tools/cs_coverage.py`, this list, `re_loop.sh`, CLAUDE.md.
- [x] T1 solution + data + PPU + runner: `ReC.slnx`; projects `src/ReC.Core` (classlib), `src/ReC.Run` (console),
  `src/ReC.Sdl` (console, empty Program for now), `tests/ReC.Tests` (console), all net10.0, nullable enabled,
  `TreatWarningsAsErrors`. `tools/gen_cs_data.py` (`../web/data/ram_labels.json` → `src/ReC.Core/Gen/R.cs`);
  `Data.cs` (= data.js, manifest size check), `Game/Ram.cs`, `Game/Prg.cs`, `Nes/Ppu.cs` (all of nes/ppu.js),
  `Util/Png.cs` (writer via `System.IO.Compression.ZLibStream`), `Util/Csv.cs`, `Util/StateFile.cs` (web_state.py JSON),
  `Machine.cs` skeleton (`LoadState` for RAM/WRAM/CIRAM/pal/OAM/PPU/mapper), `ReC.Run` with run.mjs's CLI,
  `tools/cs_vs_web.py`. Done when: `--load-state ../web/test/states/town02_enter.json --frames 1 --dump-screen` equals
  run.mjs's PNG, tests pass, coverage 100 % for nes/ppu.js, game/ram.js, game/prg.js, data.js.
  **C# s1 done:** `ReC.slnx` + `Directory.Build.props` (net10.0, nullable, TreatWarningsAsErrors; build 0 warnings);
  `tools/gen_cs_data.py [--check]` → `Gen/R.cs` (368 labels); `Data.cs`, `Game/{Ram,Prg}.cs`, `Nes/Ppu.cs` (all incl.
  timed mid-line writes), `Util/{Png (+Crc32),Csv (+Keys),StateFile}.cs`, `Machine.cs` (all of machine.js: Reset,
  LoadState + LoadSplits, RunFrame, PassStage buckets, SoundUpdate wrapper), `ReC.Run` (run.mjs CLI + `--data`),
  `tools/cs_vs_web.py` (`--canon --states --only KINDS --frames N --keep`, CASES table), `tools/state_peek.py`;
  `Game/Y.cs` (Y enum + `Stub.NotPorted`) and logging stubs for every unported callee. Also fully ported ahead of
  T3: `Game/MainLoop.cs` (all of mainloop.js), `Game/LagModel.cs`, `Game/Palette.cs`. Checks: `--load-state
  town02_enter --frames 1` PNG pixel-identical; `cs_vs_web.py --states --frames 1 --only png` = 87/87 first-frame
  screens identical (HUD / message-window / menu / intro splits); ReC.Tests 74/74; coverage 100 % for ppu, ram,
  prg, data, machine, mainloop, lagmodel, palette (95/1046 overall). CSV / RAM still differ (game code = stubs).
- [x] T2 sound: `Nes/Apu.cs`, `Game/Sound/Driver.cs`, `Game/Sound/SoundUpdate.cs`, `Game/PlaySound.cs`. Done when the
  snd CSV columns match the web port on the sound_check scenarios (via cs_vs_web or a ReC.Tests case fed by
  `node ../web/test/sound_check.mjs`'s inputs) and APU samples equal `apu_check.mjs --wav` output.
  **C# s2 done:** all of nes/apu.js (Envelope/Pulse/Triangle/Noise/Dmc/Apu; float32 mixer tables + double sums as in
  the JS; sink = `Action<double>`), driver.js (44 members, opcode dispatch as a switch on the table address),
  soundupdate.js, playsound.js; `Machine.ApuWrite` / `ApuRead` are now replaceable delegates (= the JS closures).
  Tests `tests/ReC.Tests/SoundTests.cs`: `sound_frames` replays `../web/test/sound_frames/*.json` like sound_check.mjs
  → 130/130 scenarios, 35 940 SoundUpdate calls, 210 892 APU writes + RAM identical; `apu_pitch/status/sweep/driver`
  = apu_check.mjs 1-4 all ok; `ReC.Tests --wav ID OUT.wav [S]` = apu_check.mjs --wav. `tools/apu_wav_compare.py`
  → 32/32 music ids $00-$1F byte-identical WAVs (8 s each). ReC.Tests 88/88, build 0 warnings, coverage 100 % for
  apu/driver/soundupdate/playsound (187/1046 overall), first-frame PNGs still 87/87. cs_vs_web `snd` columns
  (120 frames): identical on town02_enter / pause / mesia / nmimode4_08; death / ending / bosse2 / finalboss differ
  only because their game code is still stubs (e.g. ending.js mainState4 doesn't stop the music) → closes with T3+.
- [x] T3 frame loop + title: `Machine.cs` (RunFrame, Reset, LoadState incl. Pending/ENTRY_POINTS), `Game/MainLoop.cs`,
  `Game/LagModel.cs`, `Game/Pads.cs`, `Game/Palette.cs`, `Game/Boot.cs`, `Game/States/Title.cs`, `Game/States/Death.cs`
  helpers (Spin/NoNmi, WaitFrames, fades). Done when `cs_vs_web.py --canon` passes from Reset through f2250.
  (C# s1: MainLoop.cs, LagModel.cs, Palette.cs, Machine.cs already complete + tagged; remaining: Pads, Boot (stub),
  Title, Death helpers, Play.EntryPoints wiring; Y.Spin / Y.NoNmi exist in `Game/Y.cs`.)
  **C# s2 done:** `Game/Pads.cs` (3/3, PadDirTable cached in `m.PadDirTable`), `Game/Boot.cs` (11/11),
  `States/Title.cs` (93/93: all title modes / opening / menu / story / status / name entry; MODES / STEPS as
  dictionaries; plain steps return null, iterator steps their iterator), `World/PpuQueue.cs` (7/7, pulled forward from
  T4: the NMI's PpuQueueFlush is needed by the status screen; returns `int?` = JS `ry`), `States/Death.cs` helpers
  (14/22: dec, waitFrame(s), paletteBuildRom, fade/fadeOut/fadeIn, queueFlushNow, hudQueue, toDecimal, hudRedraw
  (Y result via `Death.YOut`), hudRedrawCycles, hudHpBar, less1). Checks: `cs_vs_web.py canon:2037` (Reset -> f2250)
  = 2037 frames identical in csv, png, ram, wram, ciram; full `canon` identical through f2321 (f2322 = MainStateNewGame,
  T4); states first frame 87/87; ReC.Tests 88/88; build 0 warnings; coverage 315/1046. Left for T4 (already listed
  there): the other 8 death.js members (statRecalc, stat, oamClip, poseHold, gameOver, hpBarBank, opelRevive,
  playerDeath) and the `Play.EntryPoints` table.
- [x] T4 world + gameplay entry: `Game/World/*` (Area, Bg, BgStream, Camera, PpuQueue, Spawn), `States/AreaEnter.cs`,
  `States/Play.cs` (Substates, EntryPoints, SubstPlay), rest of `Death.cs`. Done when `--canon` passes through f2460
  and the `death` state passes.
  **C# s3 done:** `World/{Area,Bg,BgStream,Camera,Spawn}.cs` (all of area/bg/bgstream/camera/spawn.js),
  `States/AreaEnter.cs` (20/20), `States/Play.cs` (15/15: Substates table, EntryPoints table complete, SubstPlay,
  PlayerStatusTick, SubstPlayTail with the s213 cycle counts), `States/Death.cs` (22/22: StatRecalc, OamClip,
  PlayerDeath / OpelRevive / GameOver), `States/Pause.cs` (10/10), `States/SaveLoad.cs` (18/24: all but the menu page
  that needs equip.js, see T8). The canon run needs ObjUpdateAll / ObjCollideAll and AIs $01/$03/$05/$14/$50/$7F, so
  T5's modules (below) and three T6 modules (`Obj/AiStatic.cs` 8/8, `Obj/AiShot.cs` 28/28, `Obj/AiEffect.cs` 14/14)
  were ported too. Stub classes for the other substates (`Equip`, `Shop`, `Talk`, `GiveItem`, `Substates9.cs`) and
  `Obj/AiThunder.cs`. Checks: `cs_vs_web.py canon` = Reset -> f2460, **2247 frames identical** (csv, png, ram, wram,
  ciram); `death:700` identical (through game over); states first frame 87/87 PNGs; `--states` (120 frames) 8/87
  identical (the rest diverge in unported AIs / substates); ReC.Tests 88/88; build 0 warnings; coverage 572 + 12 waived
  of 1046 (55.8 %). New: `docs/waivers.csv` (sprites.js `rd` / `wr` aliases), `tools/json_peek.py`.
- [x] T5 objects: `Game/Obj/{Objects,ObjInit,Move,Collide,Util,Sprites,Player}.cs`. Done when 100 % coverage for those
  modules and cs_vs_web passes for area1_enter / town02_enter.
  (C# s3: all seven files ported, coverage 100 % for objects / objinit / move / collide / util / sprites (2 waivers) /
  player. There is no `area1_enter` state in ../web/test/states (area 1 is entered in the canon run, which passes).)
  **C# s4 done:** with AI $30/$78 (T6) and GiveItem (T8) ported, `cs_vs_web.py town02_enter:600` = 600 frames identical
  (csv, png, ram, wram, ciram); canon still 2247 frames identical.
- [x] T6 field AIs: `Obj/{AiWalk,AiField,AiStatic,AiShot,AiEffect,AiThunder}.cs`. Done when 100 % coverage for them and
  the group / fan / kill / thunder states pass.
  (C# s3: AiStatic, AiShot, AiEffect done (100 %).)
  **C# s4:** `Obj/AiWalk.cs` (22/22), `Obj/AiField.cs` (33/33), `Obj/AiThunder.cs` (5/5) ported; `Objects.AiTable` merges
  static, walk, field, shot, effect, player (final / boss go between effect and player when T7 adds them). All six
  T6 modules 100 % coverage. `cs_vs_web.py` CASES now holds every group / fan / kill / thunder check (state + LAST +
  KEYS copied from its `*_check.mjs`, `last=` -> frames; `--checks` runs them): **20/23 identical at full length**
  (group05/08/09/11/18/28/29/40/59/5a/6b/7c/84/84b/88/b2, thunder, fanskip, killgold, killlevelup). New lag-model
  input oracle `tools/pass_count_diff.py` (`ReC.Run --pass-count` vs `pass_count.mjs --stages`, per stage rd/wr/rom):
  19/23 identical; it found an extra counted `main_state` read in `MainLoop.MainStep` (fixed: `top` bucket 30 -> 29 rd
  per pass). Remaining (all T7 AIs, not T6 code): group6e from f2859 (reveal into type $7D = AI $66, ai_boss.js),
  group7f / group81 (AI $70, ai_final.js; group81 from f2507), group28's CSV passes but its AI $63 dispatch counts
  differ (stub). This task closes when T7 lands and those three pass.
  **C# s5 done:** with T7's AiBoss / AiFinal, `cs_vs_web.py --checks` = **23/23 identical at full length** (group6e,
  group7f, group81 now too) and `pass_count_diff.py --checks` 23/23 (group28's AI $63 counts now match).
- [x] T7 boss AIs: `Obj/AiBoss.cs` (1,436 JS lines, may span sessions), `Obj/AiFinal.cs`. Done when 100 % coverage and
  the boss states pass (bosse2, bossflee, bossa6, finalboss, ai70*).
  **C# s5 done:** `Obj/AiFinal.cs` (19/19: Ai70PhaseDispatch phases 0-12, HitFlashPal, FlashIrq with the s174 heuristic /
  s220 `fl70` cycle booking) and `Obj/AiBoss.cs` (109/109: BossPhaseDispatch keyed by handler address, AIs $60-$6F,
  $79, $3F, BossDefeated + its 5 hooks incl. BossFleeSpeech / BossHookA6). Phase handlers are `Ph(m, x, c, r)` (c = the
  dispatch's ASL carry for e2Walk; r = the JS return: e9Fire's SpawnShot bool pops ObjUpdateAll as in the JS, ebSetup
  r.X = 0, AI $79 r.X = $2F). `Objects.AiTable` now merges every module in the JS spread order. New CASES (from the
  checks): bosse2 (f3002), bossflee (2761, keys 2695/2735/2775:A), bossa6 (finallocked, 2535), finalboss (3220),
  ai70msg (3179, 11 A presses), ai70ph3 (restore5f, 2758): **6/6 identical** (csv, png, ram, wram, ciram) and
  `pass_count_diff.py` 6/6 (bosse2 7, finalboss 27, ai70ph3 21 lag-model frames match). States at 120 frames: 67/87
  (event21 now passes; the 20 left are T8 shop / equip / saveload and T9 substates / ending). ReC.Tests 88/88, build
  0 warnings, coverage 828 + 12 waived of 1046 (80.3 %).
- [x] T8 menu / town substates: `States/{Pause,Equip,SaveLoad,Shop,Talk,GiveItem}.cs`. Done when 100 % coverage and
  their states pass (equip, armorshop, …, giveitem, event*, itemuse*).
  (C# s3: Pause.cs 10/10 done; SaveLoad.cs 18/24, left: saveLoadPage, menuSave, slotPicture, pageSetup, modeCursor,
  slotCursor (need equip.js). Equip / Shop / Talk / GiveItem are stub classes; GiveItem.EventAction / Talk.TalkAction
  stand for the JS EVENT_ACTIONS / TALK_ACTIONS tables used by Play.EntryPoints.)
  **C# s4:** `States/Talk.cs` (37/37: text printer, ShowMessage, NpcScript, NpcTalk, TalkActions incl. the giveitem.js
  entries) and `States/GiveItem.cs` (29/29: item hooks 0-2, ItemUseMenu, ShowPendingMessage, all 32 EventActions with
  act 30's PLA as a `Ret<bool>`) done; also equip.js `swordPowerLevel` (Equip.cs) and formrevert.js `farSpriteDraw`
  (Substates9.cs FormRevert). States identical at 120 frames: giveitem, event02/07/08/11/12/13/15/19/19b/20/23/28,
  itemuse_fail/herb/opel, talk16/19/20/22/25/26, talk_act1/act8/probe, outofmp, tmp_iu. Left: equip.js (43), shop.js
  (30), saveload.js 6 menu-page members; event01 needs swordraise.js (T9), event21 AI $6B (T7); armorshop / inn /
  pawnshop / toolshop / shopempty / shopmenu / equip / saveload states fail on the Shop / Equip stubs.
  **C# s5 done:** `States/Equip.cs` (44/44: MenuPrint + FE/FD/FC escapes, MenuSprDraw, grids, cursor, A equip /
  unequip, B drop + YES/NO, sort / reselect, shop mode price / sell / "Anything else?", SubstEquipMenu; the s121 IRQ 2
  re-apply hook in SetupGrid; JS 'exit' = `Ret<bool>`), `States/Shop.cs` (30/30: walk in / out, pawn shop, armor /
  tool buy loop, inn, empty shop; the two JS `pr` closures = one static `Pr`), `States/SaveLoad.cs` 24/24 (SaveLoadPage,
  MenuSave, SlotPicture, PageSetup, ModeCursor, SlotCursor). Also T9's `States/SwordRaise.cs` (1/1, needed by event01).
  Checks: every T8 state at 120 frames + 600-frame runs of equip / saveload / armorshop / inn / pawnshop / toolshop /
  shopempty identical; new CASES (no web check drives these screens, keys chosen here; confirms start on NO, so a
  direction press first = YES): equip_walk (900 f), equip_drop, save_yes, load_yes, saveload_nav, armorshop_buy,
  toolshop_buy, inn_yes, inn_no, pawnshop_sell, pawnshop_no = **11/11 identical** (gold changes in the buy / inn /
  sell runs, so those paths ran). event_check.mjs as CASES (event01/02/07/08/11/12/13/15/19/19b/20/21/23/28, talk16/19/
  20/22/25/26 with its keys + LAST) = **20/20 identical**; giveitem / itemuse_herb/fail/opel with their check keys and
  town02_enter 900 f = 5/5 identical.
- [x] T9 remaining substates + ending: `States/{Wake,Teleport,Telepathy,ChangeForm,Cure,FormRevert,SwordRaise,Mesia,
  FinalScene,Ending}.cs`. Done when 100 % game-layer coverage and their states pass.
  (C# s3: their stub classes sit together in `States/Substates9.cs`; move each into its own file when porting it.)
  **C# s5:** ported, each in its own file: `Wake.cs` 15/15, `Teleport.cs` 7/7, `Telepathy.cs` 12/12 (JS `telepathy` ->
  `TelepathyMsg`: a member can't share its class name), `ChangeForm.cs` 5/5, `Cure.cs` 1/1, `FormRevert.cs` 2/2,
  `SwordRaise.cs` 1/1, `Mesia.cs` 4/4, `FinalScene.cs` 4/4 (JS `y = 0` defaults = one-argument overloads, as play.js
  calls every substate with `(m)`; teleport's `yIn = m.rd($010F)` overload reads at the call). `Substates9.cs` is now
  comment-only (rm needs approval in the sandbox; delete it). New CASES from the checks: wake (Reset -> f3400, 3187 f),
  teleport_warp / _magic / _cancel, telepathy_magic / _hint / _cancel, changeform / _left / _cancel, cure, formrevert,
  swordraise, mesia = **all identical**; finalscene identical through f3604, then main_state 4 = ending.js.
  **C# s5 done:** `States/Ending.cs` (55/55: EndingInit / EndingTick, fades, sub-effects, metasprites, blitters, all
  30 steps keyed by handler address; JS `throw` = InvalidOperationException). `ending` 120 f + **8000 f identical**
  (ends at step $1D, the final RTS loop), `finalscene` (LAST 3620) and 3000 f identical (into the ending). Game layer
  100 % (states/ 430/430, obj/ 238/238); the only missing inventory entries are the 22 platform callables (audio.js,
  input.js, main.js, save.js = T11).
- [x] T10 full parity: `cs_vs_web.py --canon --states` passes for every `../web/test/states/*.json`. Fix differences in
  C#; the web port is the reference (if the web port is known-wrong vs tools.emu, keep parity and comment it).
  **C# s5 done (no fixes needed after T6-T9):** `--states` **87/87 identical** (120 f each), `canon` 2247 f identical,
  `--checks` **67/67** CASES identical at full length (every *_check.mjs run with its state / keys / LAST, plus the
  T8 menu walks), `wake` (Reset -> f3400) identical; `pass_count_diff.py --checks` 67/67 (lag-model inputs per stage
  per frame); ReC.Tests 88/88; build 0 warnings; coverage 1012 + 12 waived of 1046 (97.9 %, rest = T11 platform).
- [ ] T11 playable frontend `src/ReC.Sdl`: P/Invoke `libSDL2-2.0.so.0` (`Sdl.cs`, no NuGet), window with integer scale,
  fixed-step 60.0988 Hz, keyboard like ../web/index.html (arrows/WASD, X/K = A, Z/J = B, Enter = Start,
  Right Shift/Tab = Select, P pause, M mute) + game controller, APU audio via `SDL_QueueAudio`, battery WRAM in
  `CSHARP/re-c.sav`. Done when it builds and `SDL_VIDEODRIVER=dummy SDL_AUDIODRIVER=dummy ReC.Sdl --frames 600` exits 0;
  the interactive play-test is left to the user.
- [ ] T12 close-out: `cs_coverage.py` exits 0 (every waiver has a reason); tests + `cs_vs_web.py --canon --states` pass;
  `rea inspect-managed-artifact` on the Release `ReC.Core.dll` → confirm every tagged member exists in the assembly
  (write `tools/cs_rea_members.py` to cross-check, record the evidence id in docs/); refresh docs/architecture_cs.md
  (as built: JS → C# module table, deviations) and README.md (build / run / play / check commands).
