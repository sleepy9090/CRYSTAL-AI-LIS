# re-c Windows C# port (separate project, base folder `CSHARP-WINDOWS/`)

Single-file Windows exe (`dist/ReC.exe`, win-x64, self-contained) of re-c, cross-built on Linux with .NET 10 and
tested under Wine. Core copied from the finished `../CSHARP` port; host is raw Win32 P/Invoke. Design:
`docs/architecture_win.md`. `TASKS.md` drives the work; `re_loop.sh` runs autonomous sessions over it.

## Rules
- Core (`src/ReC.Core`) changes: data loading, parity fixes, and the output-only `Ppu.Tags` (T10, must never change Fb;
  `ppu_tags` test) and `Apu.OutChannels` (T13, must never change the mono stream; `apu_channels` test). Record any other in architecture §5.
- Keep everything inside `CSHARP-WINDOWS/`. `../web/`, `../CSHARP/`, `../REVERSE.md`, `../docs/`, `../tools/` are read-only.
- Do NOT try to identify the game title (no CRC/database lookups, no title-screen hunting for that purpose). Call it "re-c".
- Keep `// js: <module> <name>` tags on ported members (module path relative to web/src); new host code that replaces a
  web platform function (main.js / input.js / audio.js / save.js) gets that function's tag.
- Python helpers go into `tools/` as .py files; never inline `python3 -c`. No third-party disassemblers. No NuGet library packages.
- 0 build warnings. Wine runs use `WINEPREFIX=$PWD/.wine WINEDEBUG=-all` (the tools set this) and always a timeout.

## Commands (run from CSHARP-WINDOWS/)
- Build: `dotnet build ReC.Windows.slnx -c Release`; tests: `dotnet run --project tests/ReC.Tests -c Release`
- Publish exe: `dotnet publish src/ReC.Win -c Release -r win-x64 -o dist` (profile settings in the csproj)
- Parity: `python3 tools/core_vs_web.py ...` (Linux core) and `python3 tools/win_vs_web.py ...` (dist/ReC.exe under Wine)
- Coverage vs REA inventory: `python3 tools/cs_coverage.py [--module SUBSTR] [--missing]`
- REA on the exe: `python3 tools/rea_pe_report.py` (wraps `rea inspect-pe-imports|inspect-binary-hardening|inspect-pe-resources|
  inspect-managed-members|audit-release-exposure`); `mcp__rea__*` tools when the MCP server is connected.
