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
- [ ] T2 sound: `Nes/Apu.cs`, `Game/Sound/Driver.cs`, `Game/Sound/SoundUpdate.cs`, `Game/PlaySound.cs`. Done when the
  snd CSV columns match the web port on the sound_check scenarios (via cs_vs_web or a ReC.Tests case fed by
  `node ../web/test/sound_check.mjs`'s inputs) and APU samples equal `apu_check.mjs --wav` output.
- [ ] T3 frame loop + title: `Machine.cs` (RunFrame, Reset, LoadState incl. Pending/ENTRY_POINTS), `Game/MainLoop.cs`,
  `Game/LagModel.cs`, `Game/Pads.cs`, `Game/Palette.cs`, `Game/Boot.cs`, `Game/States/Title.cs`, `Game/States/Death.cs`
  helpers (Spin/NoNmi, WaitFrames, fades). Done when `cs_vs_web.py --canon` passes from Reset through f2250.
  (C# s1: MainLoop.cs, LagModel.cs, Palette.cs, Machine.cs already complete + tagged; remaining: Pads, Boot (stub),
  Title, Death helpers, Play.EntryPoints wiring; Y.Spin / Y.NoNmi exist in `Game/Y.cs`.)
- [ ] T4 world + gameplay entry: `Game/World/*` (Area, Bg, BgStream, Camera, PpuQueue, Spawn), `States/AreaEnter.cs`,
  `States/Play.cs` (Substates, EntryPoints, SubstPlay), rest of `Death.cs`. Done when `--canon` passes through f2460
  and the `death` state passes.
- [ ] T5 objects: `Game/Obj/{Objects,ObjInit,Move,Collide,Util,Sprites,Player}.cs`. Done when 100 % coverage for those
  modules and cs_vs_web passes for area1_enter / town02_enter.
- [ ] T6 field AIs: `Obj/{AiWalk,AiField,AiStatic,AiShot,AiEffect,AiThunder}.cs`. Done when 100 % coverage for them and
  the group / fan / kill / thunder states pass.
- [ ] T7 boss AIs: `Obj/AiBoss.cs` (1,436 JS lines, may span sessions), `Obj/AiFinal.cs`. Done when 100 % coverage and
  the boss states pass (bosse2, bossflee, bossa6, finalboss, ai70*).
- [ ] T8 menu / town substates: `States/{Pause,Equip,SaveLoad,Shop,Talk,GiveItem}.cs`. Done when 100 % coverage and
  their states pass (equip, armorshop, …, giveitem, event*, itemuse*).
- [ ] T9 remaining substates + ending: `States/{Wake,Teleport,Telepathy,ChangeForm,Cure,FormRevert,SwordRaise,Mesia,
  FinalScene,Ending}.cs`. Done when 100 % game-layer coverage and their states pass.
- [ ] T10 full parity: `cs_vs_web.py --canon --states` passes for every `../web/test/states/*.json`. Fix differences in
  C#; the web port is the reference (if the web port is known-wrong vs tools.emu, keep parity and comment it).
- [ ] T11 playable frontend `src/ReC.Sdl`: P/Invoke `libSDL2-2.0.so.0` (`Sdl.cs`, no NuGet), window with integer scale,
  fixed-step 60.0988 Hz, keyboard like ../web/index.html (arrows/WASD, X/K = A, Z/J = B, Enter = Start,
  Right Shift/Tab = Select, P pause, M mute) + game controller, APU audio via `SDL_QueueAudio`, battery WRAM in
  `CSHARP/re-c.sav`. Done when it builds and `SDL_VIDEODRIVER=dummy SDL_AUDIODRIVER=dummy ReC.Sdl --frames 600` exits 0;
  the interactive play-test is left to the user.
- [ ] T12 close-out: `cs_coverage.py` exits 0 (every waiver has a reason); tests + `cs_vs_web.py --canon --states` pass;
  `rea inspect-managed-artifact` on the Release `ReC.Core.dll` → confirm every tagged member exists in the assembly
  (write `tools/cs_rea_members.py` to cross-check, record the evidence id in docs/); refresh docs/architecture_cs.md
  (as built: JS → C# module table, deviations) and README.md (build / run / play / check commands).
