# re-c — C# port

A C# (.NET 10) port of re-c, translated module by module from the web port in `../web/src/` and checked
frame by frame against it. Separate project: everything lives in this folder.

Status: being built — see `TASKS.md`.

## Requirements
- .NET 10 SDK (`sudo apt install dotnet-sdk-10.0`)
- SDL2 runtime for the playable build (`libSDL2-2.0.so.0`, already present on Ubuntu desktops)
- Node 18+ and Python 3 for the comparison tools (they run the web port as the reference)

## Layout
See `docs/architecture_cs.md` §2. Progress tracking: `python3 tools/cs_coverage.py` against the REA inventory
`docs/obligations.csv`.

## Autonomous build
`./re_loop.sh [--max N]` from this folder runs Claude sessions over `TASKS.md` until every task is ticked; each
session commits its work (only `CSHARP/` paths).
