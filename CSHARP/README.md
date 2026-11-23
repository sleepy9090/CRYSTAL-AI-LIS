# re-c — C# port

A C# (.NET 10) port of re-c, translated module by module from the web port in `../web/src/` and checked
frame by frame against it. Separate project: everything lives in this folder.

Status: **complete** (T0-T12 in `TASKS.md`). Every web-port callable is ported or waived with a reason, and every
headless comparison against the web port is byte-identical (CSV rows, screen PNGs, RAM / WRAM / CIRAM dumps).
The interactive play-test (real window, speakers, controller) is up to you.

## Requirements
- .NET 10 SDK (`sudo apt install dotnet-sdk-10.0`); no NuGet packages
- SDL2 runtime for the playable build (`libSDL2-2.0.so.0`, already present on Ubuntu desktops)
- the game data in `../web/data/` (found by walking up from the executable, or `--data DIR`)
- Node 18+ and Python 3 for the comparison tools (they run the web port as the reference)

## Build
```sh
dotnet build ReC.slnx -c Release          # 0 warnings (TreatWarningsAsErrors)
```

## Play
```sh
src/ReC.Sdl/bin/Release/net10.0/ReC.Sdl [--scale S] [--save F] [--load-state F.json] [--mute] [--no-audio]
```
Keys (as in `../web/index.html`): arrows / WASD = D-pad, X or K = A, Z or J = B, Enter = Start,
Right Shift or Tab = Select, P = pause, M = mute, Esc = quit. Game controllers work too (hot-plug).
The battery save is `re-c.sav` in this folder (raw 8 KB WRAM); `--load-state` runs write no save unless `--save` is given.
Headless smoke test: `SDL_VIDEODRIVER=dummy SDL_AUDIODRIVER=dummy ReC.Sdl --frames 600 --save out/x.sav [--turbo] [--dump-screen F.png]`.

## Run headless
Same CLI as `node ../web/test/run.mjs`:
```sh
dotnet run --project src/ReC.Run -c Release -- --frames N --keys 250:START 300:A+RIGHT:8 \
    --compare cs.csv --dump-screen cs.png [--load-state ../web/test/states/X.json] [--dump-ram F] [--pass-count F]
```

## Check
| Command | What |
|---|---|
| `dotnet run --project tests/ReC.Tests -c Release` | unit tests, sound driver replay, APU cases (88) |
| `python3 tools/cs_vs_web.py --canon --states` | canonical run from Reset + every `../web/test/states/*.json` vs the web port |
| `python3 tools/cs_vs_web.py --checks` / `canon` / `wake` | every `*_check.mjs` scenario at full length; the two Reset runs by name |
| `python3 tools/pass_count_diff.py --checks` | lag-model inputs (counted reads / writes per stage per frame) vs the web port |
| `python3 tools/apu_wav_compare.py` | byte-identical music WAVs vs `apu_check.mjs --wav` |
| `python3 tools/cs_coverage.py` | `// js:` tags + `docs/waivers.csv` vs the REA inventory `docs/obligations.csv` |
| `python3 tools/cs_rea_members.py` | every tag's C# member exists in the built assemblies (REA bundles, see below) |

`cs_rea_members.py` reads REA evidence of the Release build; regenerate it after a rebuild:
```sh
rea inspect-managed-members src/ReC.Core/bin/Release/net10.0/ReC.Core.dll --detail summary --evidence_output out/rea_members.json
rea inspect-managed-members src/ReC.Sdl/bin/Release/net10.0/ReC.Sdl.dll --detail summary --evidence_output out/rea_members_sdl.json
```
(REA never overwrites a bundle: delete the old ones first.) Outputs go to `out/` (gitignored).

## Layout and design
`docs/architecture_cs.md`: design rules, iterator conventions, the JS → C# module table and the deviations (§5.2),
REA tracking and evidence ids (§6).

## Autonomous build
`./re_loop.sh [--max N]` from this folder runs Claude sessions over `TASKS.md` until every task is ticked; each
session commits its work (only `CSHARP/` paths).
