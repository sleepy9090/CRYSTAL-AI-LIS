# re-c C# port (separate project, base folder `CSHARP/`)

C# (.NET 10, no NuGet packages) translation of the finished web port `../web/src/`. Design: `docs/architecture_cs.md`.
`TASKS.md` drives the work (first unchecked task); `re_loop.sh` runs autonomous sessions over it.

## Rules
- Keep everything inside `CSHARP/`. Read-only use of `../web/`, `../REVERSE.md`, `../docs/`, `../tools/`, `../rom/`.
  Never edit files outside `CSHARP/`.
- Do NOT try to identify the game title (no CRC/database lookups, no title-screen hunting for that purpose). Call it "re-c".
- Translate from the JS module; put `// js: <module> <name>` above every ported member (module path relative to web/src).
- Python helpers go into `CSHARP/tools/` as .py files; never inline `python3 -c`. No third-party disassemblers.
- `dotnet build ReC.slnx -c Release` must have 0 warnings (TreatWarningsAsErrors).

## Commands (run from CSHARP/)
- Build: `dotnet build ReC.slnx -c Release`; tests: `dotnet run --project tests/ReC.Tests -c Release`
- Headless run (same CLI as `node ../web/test/run.mjs`): `dotnet run --project src/ReC.Run -c Release -- --frames N --keys 250:START --compare cs.csv --dump-screen cs.png`
- Play (SDL2): `src/ReC.Sdl/bin/Release/net10.0/ReC.Sdl [--scale S] [--save F] [--load-state F.json] [--mute]`;
  headless check `SDL_VIDEODRIVER=dummy SDL_AUDIODRIVER=dummy ReC.Sdl --frames 600 [--turbo] [--save out/x.sav]
  [--dump-screen F.png]` (use `--save out/...` in checks so `CSHARP/re-c.sav`, the player's save, stays untouched)
- Oracle diff: `python3 tools/cs_vs_web.py [--canon] [--states] [--only csv,png,ram,wram,ciram] [--frames N] [NAME[:FRAMES]..]`
  (outputs in `out/<case>/`, gitignored; shell `mkdir`/`rm` need approval in the sandbox, so let tools create files);
  `python3 tools/state_peek.py STATE.json [ADDR,..]` (state fields / RAM bytes); `python3 tools/json_peek.py F.json [a.b.0]`
  (JSON structure / one path, e.g. `../web/data/areas.json areas.00`); `python3 ../tools/ram_byte.py out/<case>/web.ram ADDR,..`; `python3 tools/gen_cs_data.py [--check]` (Gen/R.cs) (uses `node ../web/test/run.mjs` + `../tools/compare_frames.py`)
- Full check scenarios: `cs_vs_web.py --checks` = every CASES entry copied from a `../web/test/*_check.mjs` (state + LAST +
  KEYS as `last=` / `keys=`; add new ones there when a module's checks become reachable). C# s5: 67 state cases
  (every web check incl. boss / event / talk / T9 substates, plus T8 menu walks) + `canon` and `wake` from Reset
  (`--checks` skips those two: run them by name)
- Lag-model inputs: `python3 tools/pass_count_diff.py [--no-build] [--checks] [NAME..]` (`ReC.Run --pass-count F` vs
  `node ../web/test/pass_count.mjs --stages`: per-frame rd/wr/cpuRd per stage must match, or lag frames drift later;
  extra counted `m.Rd` calls the JS doesn't make show up here first)
- Sound: ReC.Tests `sound_frames` / `apu_*` cases; `python3 tools/apu_wav_compare.py [--no-build] [--seconds S] [ID..]`
  (C# `ReC.Tests --wav ID OUT.wav S` vs `node ../web/test/apu_check.mjs --wav`, byte-identical WAVs)
- Coverage vs REA inventory: `python3 tools/cs_coverage.py [--module SUBSTR] [--missing]` (also fails on waivers
  without a reason / not in the inventory / also tagged)
- Tags vs built assemblies: `python3 tools/cs_rea_members.py [--list] [--modules]` (reads `out/rea_members.json` +
  `out/rea_members_sdl.json` from `rea inspect-managed-members <dll> --detail summary --evidence_output ...`; exit 1 if a
  tagged member is missing or a bundle's sha256 no longer matches the DLL)
- REA: `rea analyze-javascript-application ../web/src ...` (inventory source), `rea inspect-managed-artifact <dll>` (built assembly);
  `mcp__rea__*` tools when the MCP server is connected.
