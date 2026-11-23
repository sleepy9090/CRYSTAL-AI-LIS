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
| 1 – 6 | window scale 1× – 6× |
| right-click | the Game / Video menus (also in fullscreen) |
| Esc | quit (the save is written first) |

An XInput controller (Xbox-style) works too: D-pad or left stick to move, A/B buttons = A, X/Y buttons = B,
Back = Select, Start = Start. You can plug it in while the game is running; the exe looks for it every 2 s.
Minimising the window pauses the game. The picture is drawn at the largest whole-number scale that fits the window,
with black bars around it.

### Video menu
The menu bar has **Game** (pause, mute, exit) and **Video**. Right-click anywhere in the window opens the same menus,
which is how you reach them in fullscreen or with the menu bar hidden. While a menu is open the game waits.

| Video item | What it does |
|---|---|
| Fullscreen (F11) | fullscreen on / off |
| Fullscreen resolution | *Desktop resolution* (borderless, the default) or one of the monitor's display modes (32-bit, 640×480 and up). A display mode is changed only while the game is fullscreen and in front; alt-tab or leaving fullscreen puts the desktop back. If Windows refuses a mode, the desktop resolution is used. |
| Window size | 1× – 6× the picture (keys 1 – 6), or a fixed size: 640×480, 800×600, 1024×768, 1280×720, 1280×960, 1366×768, 1600×900, 1920×1080 |
| Aspect ratio | Square pixels (256:240, the default) · NES TV (8:7 pixels, as on a CRT) · 4:3 · 16:10 widescreen · 16:9 widescreen · Stretch to fill the window |
| Integer scaling | on (default): whole-number scale, sharp pixels, black bars · off: as large as the window allows |
| Crop overscan | hides the top and bottom 8 lines (256×224), as most TVs did |
| Smooth filter | soft scaling instead of sharp pixels |
| Show menu bar | hide it for a clean window (right-click still opens the menus) |
| Reset video settings | back to the defaults |

The choices are saved in `%APPDATA%\re-c\video.ini` (delete it to get the defaults back). The game itself is always
256×240; widescreen shapes stretch it.

### Save file
The game's battery RAM (8 KB) is kept in `%APPDATA%\re-c\re-c.sav`. It is loaded at start-up, written every ~5 s when
it has changed, and written again when you quit. `--save FILE` uses another file. A `--load-state` run writes no save
file unless you also give `--save`.

### Command-line options
```
ReC.exe [--scale S] [--fullscreen] [--video-config F.ini] [--save F.sav] [--data DIR] [--load-state F.json] [--mute]
        [--no-audio] [--frames N] [--frame0 N] [--turbo] [--keys SPEC..] [--compare F.csv] [--dump-screen F.png]
        [--menu CMD,..] [--dump-window F.png] [--quiet] [--window]
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
- `--video-config F.ini` uses another video settings file; `--fullscreen` starts fullscreen.
- Test hooks: `--menu CMD,..` runs menu items in order at start (`fullscreen`, `menubar`, `integer`, `crop`, `smooth`,
  `reset`, `pause`, `mute`, `size:3x`, `size:1280x720`, `aspect:square|tv|4:3|16:10|16:9|fill`, `fsmode:desktop`,
  `fsmode:1920x1080`); `--dump-window F.png` saves the window's picture area as drawn when the run ends.

## Checks (Linux, from this folder)
| Command | What it checks |
|---|---|
| `python3 tools/core_vs_web.py --canon --states --checks wake` | Linux core vs the web port: CSV, PNG, RAM, WRAM and CIRAM all identical |
| `python3 tools/win_vs_web.py --canon --states --checks wake` | **dist/ReC.exe under Wine** (headless) vs the web port |
| `python3 tools/win_smoke.py --canon [--no-xinput] [--verbose]` | the real window under Wine + Xvfb: a 600-frame run exits 0, writes the save file, and its PNG equals the headless one; a scripted windowed CSV equals the headless one |
| `python3 tools/win_video_check.py` | the Video menu under Wine + Xvfb: window sizes, aspect ratios, crop, fill / smooth, menu bar, fullscreen (+ resolution fallback), settings file saved and reloaded; the drawn picture against the headless frame |
| `python3 tools/apu_wav_compare.py --exe 0x05 0x10` | the exe's APU output matches `apu_check.mjs` byte for byte |
| `python3 tools/cs_coverage.py` | every web-port function is ported (`// js:` tag) or has a written reason in `docs/waivers.csv` |
| `python3 tools/rea_pe_report.py` + `python3 tools/cs_rea_members.py` | REA inspection of the exe → `docs/rea/summary.md` (imports, hardening, resources, exposure), and every tagged member is still present after trimming |

Wine runs use `WINEPREFIX=$PWD/.wine WINEDEBUG=-all` (the tools set both) and always a timeout.

## Autonomous build
`./re_loop.sh [--max N]` from this folder runs Claude sessions over `TASKS.md`. Each session commits only
`CSHARP-WINDOWS/` paths.
