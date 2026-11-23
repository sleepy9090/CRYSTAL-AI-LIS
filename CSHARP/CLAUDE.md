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
- Oracle diff: `python3 tools/cs_vs_web.py [--canon] [--states] [--only csv,png,ram,wram,ciram] [--frames N] [NAME[:FRAMES]..]`
  (outputs in `out/<case>/`, gitignored; shell `mkdir`/`rm` need approval in the sandbox, so let tools create files);
  `python3 tools/state_peek.py STATE.json [ADDR,..]` (state fields / RAM bytes); `python3 tools/json_peek.py F.json [a.b.0]`
  (JSON structure / one path, e.g. `../web/data/areas.json areas.00`); `python3 ../tools/ram_byte.py out/<case>/web.ram ADDR,..`; `python3 tools/gen_cs_data.py [--check]` (Gen/R.cs) (uses `node ../web/test/run.mjs` + `../tools/compare_frames.py`)
- Sound: ReC.Tests `sound_frames` / `apu_*` cases; `python3 tools/apu_wav_compare.py [--no-build] [--seconds S] [ID..]`
  (C# `ReC.Tests --wav ID OUT.wav S` vs `node ../web/test/apu_check.mjs --wav`, byte-identical WAVs)
- Coverage vs REA inventory: `python3 tools/cs_coverage.py [--module SUBSTR] [--missing]`
- REA: `rea analyze-javascript-application ../web/src ...` (inventory source), `rea inspect-managed-artifact <dll>` (built assembly);
  `mcp__rea__*` tools when the MCP server is connected.
