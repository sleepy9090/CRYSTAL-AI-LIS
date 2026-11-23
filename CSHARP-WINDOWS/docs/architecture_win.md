# re-c (NES) — Windows C# Port Architecture

Separate project under `CSHARP-WINDOWS/` (paths relative to it). Goal: **a single Windows executable `ReC.exe`**
(win-x64, self-contained, no installer, no .NET install needed on the target PC) that plays re-c, built on this
Linux machine with the .NET 10 SDK and checked under Wine.

Relation to the other ports (read-only references, never edited from here):
- `../web/src/` — the web port, verified frame by frame against `tools.emu` / FCEUX. **The oracle.**
- `../CSHARP/` — the finished cross-platform C# port (translation of the web port, byte-identical to it on every
  check scenario). Its platform-neutral core is **copied** into this project in T1 (not referenced), so this project
  builds and evolves on its own.
- `../REVERSE.md` — ROM source of truth.

---

## 1. Design rules

1. **Core = copied C# core.** `src/ReC.Core` (net10.0 class library, no OS calls) starts as a copy of
   `../CSHARP/src/ReC.Core`; every member keeps its `// js: <module> <name>` tag. Changes to the core are allowed only
   to make it embed/load data differently or to fix a parity difference; record each deviation in §5.
2. **Windows host = raw Win32 via P/Invoke, no WinForms/WPF.** `src/ReC.Win` (`net10.0-windows`, `OutputType WinExe`):
   user32/gdi32 window (`RegisterClassExW`, `CreateWindowExW`, message loop, `StretchDIBits` blit of the 256×240 frame at
   integer scale, resizable with aspect kept), winmm `waveOut*` audio (double/triple-buffered 16-bit mono at the APU
   sample rate), XInput (`xinput1_4.dll`, fall back to `xinput9_1_0.dll`) for game controllers, `QueryPerformanceCounter`
   fixed-step 60.0988 Hz with `timeBeginPeriod(1)`. Rationale: WinForms cannot be trimmed and makes a ~116 MB exe
   (measured in setup); raw Win32 keeps the exe trimmable and small, and has no dependency to ship.
3. **Self-contained single file.** `dotnet publish src/ReC.Win -c Release -r win-x64` with `SelfContained`,
   `PublishSingleFile`, `PublishTrimmed` (+ `TrimMode=partial` if full trimming breaks `System.Text.Json`; prefer
   source-generated JSON contexts so full trimming works), `EnableWindowsTargeting=true` (cross-build on Linux),
   `InvariantGlobalization`. Output copied to `dist/ReC.exe`. NativeAOT is not used (cannot cross-compile from Linux).
4. **Game data embedded.** The `../web/data/` files (manifest-checked, = `data.js`) become `EmbeddedResource`s of
   `ReC.Core` (or ReC.Win), so `ReC.exe` runs from any folder with nothing beside it. `--data DIR` still overrides
   (used by tests). The data is derived from the user's ROM; the exe is for the user's own use.
5. **Saves.** Battery WRAM (8 KB raw) in `%APPDATA%\re-c\re-c.sav`; `--save FILE` overrides; `--load-state` runs write
   no save unless `--save` is given (same rule as ../CSHARP).
6. **Headless mode inside the exe.** `ReC.exe --frames N [run.mjs options]` runs without a window and writes the same
   `--compare` CSV / `--dump-screen` PNG / `--dump-ram|wram|ciram` files as `node ../web/test/run.mjs`, printing to the
   parent console (`AttachConsole(ATTACH_PARENT_PROCESS)`). This is what makes the *shipped Windows binary itself*
   testable under Wine.
7. **No NuGet library packages** (the SDK's own win-x64 runtime packs are fine). 0 build warnings.
8. Keys as `../web/index.html`: arrows/WASD, X/K = A, Z/J = B, Enter = Start, Right Shift/Tab = Select, P pause,
   M mute, Esc quit, F11 / Alt+Enter fullscreen toggle, 1-6 window scale (T8: + Video menu, see §5.8).

## 2. Layout

```
CSHARP-WINDOWS/
├─ ReC.Windows.slnx         solution
├─ TASKS.md  re_loop.sh  CLAUDE.md  README.md
├─ docs/   architecture_win.md, obligations.csv (REA inventory of ../web/src), waivers.csv, rea/ (PE reports)
├─ tools/  cs_coverage.py, win_vs_web.py (exe under Wine vs web port), core_vs_web.py (Linux dotnet core vs web port),
│          win_smoke.py (window mode under Wine + Xvfb), apu_wav_compare.py (--exe), make_icon.py (app.ico),
│          win_video_check.py (T8 Video menu under Wine + Xvfb), rea_pe_report.py (REA inspections of dist/ReC.exe → docs/rea/), cs_rea_members.py
├─ src/ReC.Core/            copied core (net10.0) + embedded ../web/data
├─ src/ReC.Run/             Linux/dotnet headless runner; Headless.cs is also compiled into ReC.Win
├─ src/ReC.Win/             Program.cs (args, headless vs window), Win32.cs (P/Invoke), Window.cs, Video.cs +
│                           VideoMenu.cs (T8 video settings / menus), Upscale.cs (T9), Audio.cs (waveOut),
│                           Pad.cs (keyboard Input + XInput Pad), Save.cs, WavOut.cs (--wav), app.ico,
│                           app.manifest (PerMonitorV2, Win10+)
├─ tests/ReC.Tests/         copied unit tests (+ Windows-host pure-logic tests that run on Linux)
└─ dist/ReC.exe             published exe (gitignored; size + sha256 recorded in docs/)
```

## 3. Verification

1. `python3 tools/core_vs_web.py --canon --states --checks` — the copied core (Linux `ReC.Run`) vs the web port: must stay
   byte-identical (same as ../CSHARP's cs_vs_web.py).
2. `python3 tools/win_vs_web.py [--canon] [--states] [--checks] [NAME..]` — **`dist/ReC.exe` run under Wine** in headless
   mode vs `node ../web/test/run.mjs`, diffed with `../tools/compare_frames.py` + PNG/RAM byte compares. Wine prefix:
   `WINEPREFIX=$PWD/.wine` (gitignored), `WINEDEBUG=-all`. Must be identical.
3. Window smoke test: `xvfb-run -a wine dist/ReC.exe --frames 600 --save out/smoke.sav --dump-screen out/win.png` runs the real
   window + audio path (Wine's null/pulse audio) for 600 frames and exits 0; the dumped frame equals the headless one.
4. Unit tests: `dotnet run --project tests/ReC.Tests -c Release`.
5. The interactive play-test on a real Windows PC is left to the user.

## 4. REA

- **Inventory:** `docs/obligations.csv` = 1,046 named callables of `../web/src` from REA's static JS analysis
  (evidence `ev_2d5f769c5345b420702a7295d868c61695ac465eaa7dad86d906dfef0461a42a`; web/src sha256 `60f1d718…`);
  `tools/cs_coverage.py` checks the `// js:` tags + `docs/waivers.csv` (waivers that named ReC.Sdl must be re-reasoned for ReC.Win).
- **Shipped binary:** `tools/rea_pe_report.py` runs REA on `dist/ReC.exe` and writes `docs/rea/`:
  `rea inspect-pe-imports` (only the expected DLLs: kernel32, user32, gdi32, winmm, xinput, … — no surprise imports),
  `rea inspect-binary-hardening` (ASLR / DEP / CFG flags), `rea inspect-pe-resources` (icon, version info, manifest),
  `rea inspect-managed-artifact` / `inspect-managed-members` on the published `ReC.Core.dll` / `ReC.Win.dll` (every tagged
  member present after trimming), and `rea audit-release-exposure dist/` (no PDB paths / secrets shipped).
  Use `mcp__rea__*` tools instead when connected.
- **Findings (T6, Win s1; `docs/rea/summary.md`, exe 11,582,032 bytes):**
  - Imports: 442 functions from kernel32, user32, advapi32, ole32, oleaut32, shell32 and the api-ms-win-crt-* sets,
    delay-loaded version.dll + api-ms-win-core-winrt — all from the stock .NET single-file host. ReC.Win's own
    user32 / gdi32 / winmm / xinput calls are LibraryImport P/Invokes bound at run time (xinput via NativeLibrary), so
    they do not appear in the import table. No surprise imports.
  - Hardening: dynamic base, high-entropy VA, NX compat and CFG (guard_cf) on; no Authenticode (unsigned, by design).
  - Resources: GROUP_ICON 32512 (app.ico 256/48/32/16, loaded by the window class), VERSION, MANIFEST, plus the
    host's CLRDEBUGINFO / minidump RCDATA.
  - Exposure: no .pdb / .map shipped, no build-machine path in the bundled assemblies (`Deterministic` + `PathMap` in
    Directory.Build.props, `DebugType none` when publishing). Two low findings, both inherent: `packaging.extractable_payload`
    (the single-file bundle's 17 assemblies are extractable — normal for .NET single-file; the game data is in there too)
    and `native.pdb_path` = Microsoft's CI path of `singlefilehost.pdb` in the stock host's debug directory.
  - Managed members: `ReC.Core` is a `TrimmerRootAssembly` (only the framework is trimmed), so all 1,029 `// js:` tags
    resolve to members of the trimmed ReC.Core.dll / ReC.dll (`tools/cs_rea_members.py`: 0 not found; without the root,
    17 render/test helpers were trimmed away).

## 5. Deviations from ../CSHARP (filled in as built)

1. **Core data loading (T2):** `Data.cs` gained `EmbeddedNames / ReadEmbedded / LoadEmbedded / LoadDefault`;
   `ReC.Core.csproj` embeds `../web/data/*.bin;*.json` (except `catalog_manifest.json`) as `re-c.data.<file>`. No other
   core file changed. The core never used reflection `JsonSerializer` (only `JsonDocument`), so rule 3's source-generated
   `JsonSerializerContext` is unnecessary; the core is marked `IsTrimmable` and full trimming gives 0 warnings.
2. **Headless runner shared (T3):** ../CSHARP's `ReC.Run/Program.cs` top-level code became `ReC.Run/Headless.cs`
   (`Headless.Run(args)`), linked into ReC.Win, so `ReC.exe --headless` and the Linux runner are one source.
3. **Mode rule (T3/T4):** headless iff `--headless`, or a test-output option (--compare, --dump-ram/-wram/-ciram,
   --pass-count, --shots) without `--window`. So the §3.3 smoke command (`--frames --save --dump-screen`) opens the window.
4. **Window `--keys` (T4):** scripted buttons are posted as WM_KEYDOWN / WM_KEYUP (one key per button: X, Z, Tab, Enter,
   arrows) and dispatched through the WndProc before each frame; focus-loss blur is ignored while scripted.
5. **Audio (T5):** fixed 44.1 kHz 16-bit waveOut (../CSHARP SDL: device rate, float32); the APU's float samples are
   scaled by gain 1.6 and rounded/clamped to int16. Latency bound by dropping the oldest samples over 0.25 s.
6. **Controller (T5):** XInput instead of SDL GameController; `Input.Read` is keyboard only and Window ORs in
   `Pad.Read` (input.js read() ORs both in one function). Scripted runs ignore the controller.
7. **`ReC.exe --wav ID OUT.wav [S]` (T5):** the tests' `--wav` (= apu_check.mjs) in the exe, for the Wine APU check.
8. **Video menu (T8, not in the web port):** menu bar Game / Video + right-click copy. `Video.cs` `VideoSettings` holds
   aspect (square / NES TV 8:7 / 4:3 / 16:10 / 16:9 / fill), integer vs free scaling, overscan crop (224 lines, drawn by
   offsetting the top-down DIB), smooth (HALFTONE), window size (1x-6x of the picture at the aspect, or a client preset),
   fullscreen + resolution (borderless desktop, or an exclusive `ChangeDisplaySettingsExW` mode restored on leave /
   deactivate / exit), menu bar; saved to `%APPDATA%\re-c\video.ini` on each change. `Window.Fit` (`// js: main.js fit`)
   = `VideoSettings.Layout`; the defaults reproduce the old integer letterbox exactly. Checked by
   `tools/win_video_check.py` (`--menu` / `--dump-window` test hooks).
9. **Upscale filter (T9):** Video > Upscale filter = none / Scale2x / Scale3x / Scale4x (`Upscale.cs`, AdvanceMAME
   rules, edges clamped; 4x = 2x twice). The 256x240 frame is upscaled into a k-times DIB (only when a new frame was
   blitted) and that DIB goes through the same layout / crop (offset by 8k lines) / StretchDIBits path. `video.ini`
   key `upscale`.
