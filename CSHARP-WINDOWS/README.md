# re-c — Windows port (C#)

A single Windows executable, `ReC.exe` (64-bit, about 11.6 MB, no installer, no .NET install needed), that plays re-c.
The game core is the verified C# port (`../CSHARP`, a frame-exact translation of the web port `../web`). The Windows
host is plain Win32: a user32/gdi32 window, winmm waveOut audio and XInput controllers. The game data (made from your
ROM by `../tools/gen_web_data.py`) is embedded, so the exe runs from any folder with nothing beside it. It is meant for
your own use only.

Design notes: `docs/architecture_win.md`. Work log: `TASKS.md`.

## Build on Linux
Requirements: the .NET 10 SDK. For the checks you also need Wine and `xvfb-run`, Node 18+ and Python 3.

```sh
cd CSHARP-WINDOWS
dotnet build ReC.Windows.slnx -c Release                    # 0 warnings
dotnet run --project tests/ReC.Tests -c Release             # unit tests
dotnet publish src/ReC.Win -c Release -r win-x64 -o dist    # → dist/ReC.exe
```
The publish settings live in `src/ReC.Win/ReC.Win.csproj`: self-contained, single file, trimmed (the framework is
trimmed and ReC.Core is kept whole), compressed, no PDB. `python3 tools/make_icon.py` rebuilds `src/ReC.Win/app.ico` from
the intro picture (frame 1000).

## Run on Windows
Copy `dist/ReC.exe` to the Windows PC (Windows 10 or 11, x64) and double-click it. The exe is **not signed**, so the
first time you run it, SmartScreen may say "Windows protected your PC / unknown publisher". Click **More info → Run
anyway**, or right-click the file → Properties → **Unblock**.

### Keys
| Key | Action |
|---|---|
| Arrows / WASD | D-pad |
| X / K | A |
| Z / J | B |
| Enter | Start |
| Right Shift / Tab | Select |
| P | pause (the title bar shows it) |
| M | mute |
| F11 / Alt+Enter | fullscreen on / off |
| 1 – 4 | window scale 1× – 4× |
| Esc | quit (the save is written first) |

An XInput controller (Xbox-style) works too: D-pad or left stick to move, A/B buttons = A, X/Y buttons = B,
Back = Select, Start = Start. You can plug it in while the game is running; the exe looks for it every 2 s.
Minimising the window pauses the game. The picture is drawn at the largest whole-number scale that fits the window,
with black bars around it.

### Save file
The game's battery RAM (8 KB) is kept in `%APPDATA%\re-c\re-c.sav`. It is loaded at start-up, written every ~5 s when
it has changed, and written again when you quit. `--save FILE` uses another file. A `--load-state` run writes no save
file unless you also give `--save`.

### Command-line options
```
ReC.exe [--scale S] [--save F.sav] [--data DIR] [--load-state F.json] [--mute] [--no-audio]
        [--frames N] [--frame0 N] [--turbo] [--keys SPEC..] [--compare F.csv] [--dump-screen F.png] [--quiet] [--window]
ReC.exe --headless [--frames N] [--frame0 N] [--keys SPEC..] [--compare F.csv] [--dump-screen F.png] [--scale S]
        [--shots F,F.. DIR] [--dump-ram F] [--dump-wram F] [--dump-ciram F] [--load-state F.json] [--data DIR]
        [--quiet] [--pass-count F]
ReC.exe --wav ID OUT.wav [SECONDS]
```
- Window mode (the default): `--frames N` quits after N frames, `--turbo` runs frames without waiting for real time,
  and `--keys 250:START 300:A+RIGHT:8 ..` (frame:buttons[:frames held]) sends key messages to the window as if the keys
  were pressed.
- Headless mode (no window): this is `node ../web/test/run.mjs` with the same options and output files. It is chosen by
  `--headless`, or by any of `--compare` / `--dump-ram` / `--dump-wram` / `--dump-ciram` / `--pass-count` / `--shots`
  without `--window`. Output goes to the console that started it.
- `--wav`: writes one music track as a 16-bit mono 44.1 kHz WAV, the same bytes as `node ../web/test/apu_check.mjs --wav`.
- `--data DIR` loads a `web/data` folder instead of the embedded data.

## Checks (Linux, from this folder)
| Command | What it checks |
|---|---|
| `python3 tools/core_vs_web.py --canon --states --checks wake` | Linux core vs the web port: CSV, PNG, RAM, WRAM and CIRAM all identical |
| `python3 tools/win_vs_web.py --canon --states --checks wake` | **dist/ReC.exe under Wine** (headless) vs the web port |
| `python3 tools/win_smoke.py --canon [--no-xinput] [--verbose]` | the real window under Wine + Xvfb: a 600-frame run exits 0, writes the save file, and its PNG equals the headless one; a scripted windowed CSV equals the headless one |
| `python3 tools/apu_wav_compare.py --exe 0x05 0x10` | the exe's APU output matches `apu_check.mjs` byte for byte |
| `python3 tools/cs_coverage.py` | every web-port function is ported (`// js:` tag) or has a written reason in `docs/waivers.csv` |
| `python3 tools/rea_pe_report.py` + `python3 tools/cs_rea_members.py` | REA inspection of the exe → `docs/rea/summary.md` (imports, hardening, resources, exposure), and every tagged member is still present after trimming |

Wine runs use `WINEPREFIX=$PWD/.wine WINEDEBUG=-all` (the tools set both) and always a timeout.

## Autonomous build
`./re_loop.sh [--max N]` from this folder runs Claude sessions over `TASKS.md`. Each session commits only
`CSHARP-WINDOWS/` paths.
