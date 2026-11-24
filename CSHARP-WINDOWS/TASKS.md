# re-c Windows C# port — Tasks

Ordered; do the first unchecked task. Read `docs/architecture_win.md` first. Everything stays inside `CSHARP-WINDOWS/`;
`../web/`, `../CSHARP/`, `../REVERSE.md`, `../tools/` are read-only references. Build: `dotnet build ReC.Windows.slnx -c Release`
with 0 warnings. After a task, write what was done (files, checks, results) into its line and tick it; if parts remain,
write them into the same task and leave it unchecked (no follow-up tasks).

## Tasks

- [x] **Done (setup):** T0 plan: docs/architecture_win.md, REA inventory `docs/obligations.csv` + `docs/waivers.csv`
  (copied from ../CSHARP), `tools/cs_coverage.py`, this list, `re_loop.sh`, CLAUDE.md, README.md. Setup checks: the
  .NET 10 SDK cross-builds a `net10.0-windows` win-x64 single-file exe on this machine, and Wine runs it.
- [x] T1 (Win s1) done: `ReC.Windows.slnx` (Core/Run/Tests); tools fixed for this folder — `core_vs_web.py` (docstring,
  `ReC.Windows.slnx`, `compare_case(cs_cmd=, out_dir=)` hook for win_vs_web), `pass_count_diff.py` (imports core_vs_web),
  `apu_wav_compare.py`, `check_keys.mjs`, `gen_cs_data.py`, `cs_coverage.py` doc refs, `cs_rea_members.py` (ReC.Sdl → ReC.Win
  bundle, optional). Build 0 warnings; tests 88/88; cs_coverage 1012 ported + 15 waived / 1046, the 19 missing are only
  main.js / input.js / audio.js / save.js (T3-T5); `core_vs_web.py --canon --states --checks wake` 156/156 identical.
  Was: import the core (setup already copied `../CSHARP/src/ReC.Core`, `src/ReC.Run`, `tests/ReC.Tests` and tools `core_vs_web.py` (was cs_vs_web.py), `gen_cs_data.py`, `pass_count_diff.py`, `apu_wav_compare.py`, `cs_rea_members.py`, peek helpers, `check_keys.mjs` verbatim — fix their paths/names for this folder) and the tools they need
  (`cs_vs_web.py` → `tools/core_vs_web.py`, `gen_cs_data.py`, `pass_count_diff.py`, `apu_wav_compare.py`, peek helpers) with
  paths fixed for this folder; `ReC.Windows.slnx`; `.gitignore` already covers bin/obj/dist/out/.wine. Done when: build 0
  warnings, tests pass, `cs_coverage.py` shows every game/nes/data obligation ported (platform rows may be missing until T3-T5),
  and `core_vs_web.py --canon --states --checks` (+ `wake`) is fully identical.
- [x] T2 (Win s1) done: `ReC.Core.csproj` embeds `../web/data/*.bin;*.json` (minus `catalog_manifest.json`, not in the
  manifest) as `re-c.data.<file>`; `Data.EmbeddedNames / ReadEmbedded / LoadEmbedded / LoadDefault(dir)` (manifest size
  check kept); ReC.Run uses `LoadDefault` (embedded unless `--data`). JSON: the core only uses `JsonDocument` (no
  reflection `JsonSerializer` anywhere), so no `JsonSerializerContext` is needed — trim-safe as is; Core marked
  `IsTrimmable` + `IsAotCompatible`. Test `embedded` (names = manifest files + manifest.json, bytes equal, loads); tests
  113/113. `core_vs_web.py --canon --states --checks wake` 156/156 identical with no `--data`; `dotnet publish src/ReC.Win
  -r win-x64` (PublishTrimmed, TrimMode full) gives 0 trim warnings. Was: embedded data: `../web/data/` files as EmbeddedResources (generated item list or wildcard Include), `Data` loads
  them from the assembly by default (manifest size check kept), `--data DIR` overrides; JSON through source-generated
  `JsonSerializerContext` (trim-safe). Done when: core_vs_web still identical with no `--data`, a test proves the
  embedded set equals `../web/data/manifest.json`, and `dotnet publish -r win-x64 -p:PublishTrimmed=true` gives no trim
  warnings for ReC.Core.
- [x] T3 (Win s1) done: `src/ReC.Win` (WinExe, net10.0-windows, win-x64, AssemblyName `ReC`, LibraryImport-only
  `Win32.cs`, `app.manifest` PerMonitorV2 + Win10 + asInvoker); ReC.Run's loop moved into `src/ReC.Run/Headless.cs`
  (ReC.Run's Program.cs calls it) and compiled into ReC.Win as linked source, so the exe runs the same code. Mode rule
  (`Program.IsHeadless`): `--headless`, or any of --compare/--dump-ram/--dump-wram/--dump-ciram/--pass-count/--shots
  without `--window` (T4's smoke command, `--frames --save --dump-screen`, therefore opens the window). Console:
  `AttachConsole(ATTACH_PARENT_PROCESS)` only when stderr is not already redirected. Publish (in the csproj when
  publishing): single file, self-contained, `PublishTrimmed` TrimMode full, compressed, InvariantGlobalization,
  UseSystemResourceKeys, no PDB. `tools/win_vs_web.py` (core_vs_web CASES, `wine dist/ReC.exe --headless`, Z:\ paths,
  per-run timeout, out/win/). `win_vs_web.py --canon --states wake` 89/89 identical under Wine;
  **dist/ReC.exe = 11,526,958 bytes** (headless-only build). Was: Win32 host skeleton + headless mode: `src/ReC.Win` (WinExe, net10.0-windows, `EnableWindowsTargeting`, app.manifest
  DPI-aware), `Program.cs` arg parsing; with `--frames`/`--compare`/… and no window flag it runs headless exactly like
  ReC.Run (AttachConsole for output). Publish profile → `dist/ReC.exe` (self-contained, single file, trimmed,
  InvariantGlobalization). `tools/win_vs_web.py` (Wine, `WINEPREFIX=$PWD/.wine`, `WINEDEBUG=-all`, timeouts). Done when:
  `win_vs_web.py --canon --states` is identical **running dist/ReC.exe under Wine**, and the exe size is recorded here.
- [x] T4 (Win s1) done: `Win32.cs` (user32/gdi32/kernel32/winmm LibraryImports, blittable structs, WndProc as an
  `[UnmanagedCallersOnly]` function pointer), `Window.cs` (= main.js start/status/blit/fit/tick: class + message pump,
  CS_OWNDC + StretchDIBits 32-bpp top-down DIB at the largest integer scale with black FillRect letterbox, min track size
  1x, F11 / Alt+Enter borderless fullscreen via WINDOWPLACEMENT, 1-4 scale, title "re-c — paused (P) / muted (M)",
  minimise = pause, fixed step on Stopwatch(QPC) + timeBeginPeriod(1) sleeps, `WindowOptions.Parse`), `Pad.cs`
  Input (input.js keymap on VKs, Right Shift by scan code $36, focus loss = blur), `Save.cs` (%APPDATA%\re-c\re-c.sav,
  folder created, tmp+move, `--save`, no save for `--load-state` without `--save`; saved in `finally` on Esc / close).
  `--keys` in window mode posts WM_KEYDOWN/WM_KEYUP (Pad.cs `ScriptKeys`) before each frame and pumps them through the
  WndProc (focus-loss blur ignored while scripted); `--turbo` = one frame per pass; `--compare` works windowed.
  `tools/win_smoke.py [--canon] [--no-xinput] [--verbose]`: `xvfb-run -a wine dist/ReC.exe --frames 600 --save
  out/smoke/smoke.sav --dump-screen out/smoke/win.png` exits 0, save 8 KB, PNG byte-equal to `--headless` f600; windowed
  `--window --turbo --keys <canon>` 2247-frame CSV = headless CSV. Host pure-logic tests (linked sources, run on Linux):
  win_input / win_pad / win_options / win_fit / win_save; tests 157/157. Esc / window close are exercised by hand only.
  Was: window + input + save: `Win32.cs` P/Invoke (user32/gdi32/kernel32), `Window.cs` (class registration, message pump,
  StretchDIBits integer-scale blit with letterbox, resize, F11/Alt+Enter fullscreen, 1-4 scale keys, title shows
  paused/muted), keyboard mapping per architecture §1.8, fixed-step timing (QPC + timeBeginPeriod), `Save.cs`
  (%APPDATA%\re-c\re-c.sav, `--save`), clean exit on close/Esc (save flushed). Done when: `xvfb-run -a wine dist/ReC.exe
  --frames 600 --save out/smoke.sav --dump-screen out/win.png` exits 0 and the PNG equals the headless run's frame 600;
  a `--keys` script drives the windowed path too (same input code path as the keyboard) and a windowed `--canon`-keys run
  to f2460 matches the headless CSV.
- [x] T5 (Win s1) done: `Audio.cs` (= audio.js AudioOut/resume/setMuted/push/flush: winmm waveOut 16-bit mono 44.1 kHz,
  4 × 1024-sample buffers polled via WHDR_DONE, pending queue capped at 0.25 s (oldest dropped), 0.05 s lead silence after
  a full drain, underruns only counted and printed once at exit, gain 1.6, M mute, `--mute`, `--no-audio`, no device =
  silent). `Pad` in `Pad.cs`: XInputGetState bound at run time (xinput1_4 → xinput9_1_0, NativeLibrary), input.js
  PAD_BUTTONS mapping + left stick 0.5 threshold (Y+ = up), first connected slot, 2 s rescan while absent/unplugged;
  keyboard ORed in. `ReC.exe --wav ID OUT.wav [S]` (`WavOut.cs`) + `apu_wav_compare.py --exe [PATH]`: $05 and $10 (8 s)
  byte-identical to `apu_check.mjs --wav` under Wine. Wine runs (`win_smoke.py --verbose`): audio=waveOut, 600 / 900
  frames with 0 underruns (one run had 1), exit 0; `--no-xinput` (both DLLs disabled) → pad=none, smoke + windowed canon
  still identical; with Wine's xinput1_4 and no controller → no crash. cs_coverage now 1031 + 15 / 1046, 0 missing.
  Was: audio + controller: `Audio.cs` (winmm waveOut, 3-4 buffers, APU samples, mute/pause), `Pad.cs` XInput
  (xinput1_4 → xinput9_1_0 fallback, D-pad + left stick, A/B/Back/Start mapping, hot-plug poll every 2 s when absent).
  Done when: windowed Wine smoke run with audio enabled exits 0 with no underrun spam, a `--wav OUT.wav S` option in the
  exe writes the APU stream and it is byte-identical to `node ../web/test/apu_check.mjs --wav` for 2 music ids,
  and XInput absence is handled (Wine has none: no crash, keyboard works).
- [x] T6 (Win s1) done: `tools/make_icon.py` (stdlib PNG decode/encode; intro frame 1000, 128² crop → ICO with PNG
  entries 256/48/32/16) → `src/ReC.Win/app.ico` (ApplicationIcon; window class loads group 32512, big + small);
  version resource (Product re-c, 1.0.0.0). Directory.Build.props: `Deterministic` + `PathMap` (no build paths) and
  `DebugType none` when publishing; `TrimmerRootAssembly ReC.Core` so every ported member ships. `tools/rea_pe_report.py`
  → `docs/rea/{pe_imports,hardening,pe_resources,exposure,members_core,members_win}.json` (paths scrubbed to `.`) +
  `docs/rea/summary.md`, all expectations hold: imports only kernel32/user32/advapi32/ole32/oleaut32/shell32 +
  api-ms-win-crt-*, delay version.dll / winrt (stock single-file host); ASLR + high-entropy VA + NX + CFG on; icon group,
  version, manifest present; no .pdb/.map, no home path in the bundled assemblies; 2 low exposure items inherent to .NET
  single-file (extractable bundle, Microsoft's singlefilehost.pdb CI path). `cs_rea_members.py` on the trimmed
  assemblies: 1029/1029 tags found. Findings in architecture §4. Final `dist/ReC.exe` = 11,582,032 bytes, sha256
  `40b1d0cb38f38db18be9798bb4a87e0d3540955aab4491d0afab4d2b3fcbfe09`; on it: `win_vs_web.py --canon --states --checks wake`
  156/156 identical, `win_smoke.py --canon` ok (0 underruns), `apu_wav_compare.py --exe 0x05 0x10` identical.
  Was: release build + REA: app icon (`app.ico` generated by a tools/ script from the title-screen frame or CHR tiles),
  version info, `dist/ReC.exe` final publish; `tools/rea_pe_report.py` → `docs/rea/` (inspect-pe-imports, inspect-binary-hardening,
  inspect-pe-resources, inspect-managed-members on the published managed DLLs if extractable or the build outputs,
  audit-release-exposure on dist/); confirm imports are only the expected system DLLs, ASLR/DEP/high-entropy VA on, icon +
  manifest present, no PDB/secret exposure. Done when: report committed, findings summarised in architecture §4, and
  `win_vs_web.py --canon --states --checks` (+ `wake`) is identical on the final exe.
- [x] T7 (Win s1) done: `cs_coverage.py` exits 0 (1031 ported + 15 waived / 1046; main/input/audio/save.js tagged in
  ReC.Win; the 3 platform waivers `main.js $`, `save.js toB64/fromB64` re-reasoned for ReC.Win); tests 157/157; final
  checks on the final build: `core_vs_web.py --canon --states --checks wake` 156/156, `win_vs_web.py` same set 156/156 on
  dist/ReC.exe under Wine, `win_smoke.py --canon` ok, `apu_wav_compare.py --exe` ok, `rea_pe_report.py` all expectations
  hold, `cs_rea_members.py` 0 not found. Architecture §5 lists 7 deviations; README.md rewritten (Linux build, copy/run on
  Windows + SmartScreen/unsigned note, keys, controller, save location, options, checks). Not automated: interactive
  play-test on a real Windows PC (Esc / close / fullscreen / real XInput pad / real audio device) — left to the user.
  Was: close-out: `cs_coverage.py` exits 0 (platform waivers re-reasoned for ReC.Win: main.js/input.js/audio.js/save.js
  members tagged in ReC.Win or waived with reasons), tests pass, all checks identical; architecture §5 deviations filled in;
  README.md = how to build the exe on Linux, how to copy/run it on Windows (SmartScreen "unknown publisher" note: the exe
  is unsigned), keys, save location, command-line options, checks.
- [x] T8 (interactive, 2026-11-23) done: **Video menu.** Menu bar `Game` (Pause / Mute / Exit) + `Video`, and the same
  menus on right-click (the only way in fullscreen or with the bar hidden). Video: Fullscreen (F11 / Alt+Enter);
  Fullscreen resolution = desktop (borderless) or an exclusive display mode from `EnumDisplaySettingsW` (32 bpp,
  >= 640x480; `ChangeDisplaySettingsExW` CDS_FULLSCREEN on the window's monitor, undone on leaving / alt-tab / exit;
  falls back to borderless if refused); Window size 1x-6x (keys 1-6) or client presets 640x480 … 1920x1080; Aspect ratio
  square pixels / NES TV 8:7 / 4:3 / 16:10 / 16:9 widescreen / stretch to fill; Integer scaling on/off; Crop overscan
  (256x224); Smooth filter (GDI HALFTONE); Show menu bar; Reset. `Video.cs` (VideoSettings: layout maths + `video.ini`
  in %APPDATA%\re-c, `--video-config F` overrides), `VideoMenu.cs` (menus, commands, fullscreen modes). New options
  `--fullscreen`, `--video-config F`, `--menu CMD,..` (test hook = the menu's Command path), `--dump-window F.png`
  (client area as drawn). Tests 193/193 (win_video_layout / _ini / _menu). `tools/win_video_check.py`: 9 cases under
  Wine + Xvfb 1920x1080 (client size, picture rectangle, black bars, picture vs the headless frame, settings file) all ok;
  Xvfb offers no other display mode, so the exclusive case exercises the fallback; a real mode change is left to the
  play-test on Windows. Final `dist/ReC.exe` 11,591,197 bytes, sha256 `7e02cfc2…`; on it: `win_vs_web.py --canon --states --checks wake`
  156/156, `win_smoke.py --canon` ok, `cs_coverage.py` 100 %, `rea_pe_report.py` all expectations hold, `cs_rea_members.py` 1029/1029.
- [x] T9 (interactive, 2026-11-23) done: **Upscale filter menu.** Video > Upscale filter: None / Scale2x / Scale3x /
  Scale4x (`src/ReC.Win/Upscale.cs`, AdvanceMAME / EPX edge rules, edges clamped, 4x = 2x twice); the frame is upscaled
  into a k-times DIB once per new frame and drawn through the usual layout (aspect, integer scaling, overscan crop,
  smooth filter). `video.ini` `upscale=`, `--menu upscale:scale2x|..`. Tests 204/204 (`win_upscale`: hand-checked 3x3
  diagonal for 2x / 3x, flat stays flat, 4x = 2x∘2x, ini + menu id). `win_video_check.py` + 3 cases (2x, 3x + crop,
  4x at 16:9) compared exactly against a Python Scale2x/3x of the headless frame: 12/12 ok. On the final
  `dist/ReC.exe` (11,592,312 bytes, sha256 `bbd870c8…`): `win_vs_web.py --canon --states --checks wake` 156/156,
  `win_smoke.py --canon` ok, `cs_coverage.py` 100 %, `rea_pe_report.py` all expectations hold, `cs_rea_members.py` 1029/1029.
- [x] T10 (interactive, 2026-11-23) done: **Graphics: Original / Enhanced (16-bit)** (Video > Graphics, F9; F5 reloads
  the pack). Core: output-only `Ppu.Tags` (tile / row / col / flips / palette group per Fb pixel, off by default;
  architecture §5.10). `Enhance.cs`: dither blend → Scale2x x2 → bevel shading from the visible colours (screen level;
  first try was per tile: blocky dither mosaics and it leaked black-on-black shapes in dark rooms — replaced) →
  hand-drawn pack tiles `TTTT_AABBCC.png` (32x32 RGBA; `%APPDATA%\re-c\pack`, `--pack DIR`); ~4 ms / frame (parallel rows).
  `--export-tiles DIR` (built-in look + original 4x, blank / one-colour combos skipped). `PngRead.cs` decoder.
  Tests 231/231 (`ppu_tags`: Fb identical with tags, 13.6 M tagged pixels 0 conflicts; `win_enhance`: colours 5 -> 76,
  one-colour frame stays one colour, pack tile drawn, export; `win_png_read`). `win_video_check.py` 15/15 incl.
  enhanced / packtile / original; real-time 600-frame smoke with Enhanced: 0 audio underruns. Final `dist/ReC.exe`
  11,621,459 bytes, sha256 `2fd7bbf2…`; `core_vs_web` + `win_vs_web` `--canon --states --checks wake` 156/156 each,
  `win_smoke.py --canon` ok, coverage 100 %, REA all expectations hold, `cs_rea_members` 1029/1029.
  Not automated: hand-drawn art itself (the pack is empty; the built-in look is generated), play-test on Windows.
- [x] T11 (interactive, 2026-11-23) done: **starter graphics pack for the main character**, `packs/hero/` (133 tiles
  `TTTT_36140F.png`: metasprite records 00-07 walk / attack, 09/0B/0D/0F, BC/BD/CC/CD poses; body banks R2 = $40 / $41,
  sword R3 = $42; found by `--export-tiles` runs + `metasprites.json`, `tools/chr_sheet.py` sheets). `tools/hero_pack.py`
  repaints each whole frame per NES pixel (5-tone ramps skin / magenta / dark cloth, light from the top left, tinted
  selective outline, near-black details, the NES skin-colour hair shine -> pink highlight), rounds with Scale2x x2 inside
  the NES silhouette, cuts back into tile-space tiles (first frame wins); deterministic. Two earlier tries were
  rejected on screen: shading per output pixel (pale, speckled) and 2-px outline thinning (crumbly edges at game scale).
  `tools/pack_check.py` (pack folder rules), `png_crop.py` / `png_find.py` (close-ups), `pngio.py`.
  `win_video_check.py heropack`: pack loads with 0 skipped files, 3667 hero pixels repainted, 0 elsewhere.
  Not done: other palettes (fades / hit flash fall back to the built-in look), other characters.
- [x] T12 (interactive, 2026-11-23) done: **starter graphics pack for the enemies**, `packs/enemies/` (461 tiles, 402
  distinct). `ReC.Run --dump-tags DIR EVERY` (Headless.cs: Ppu.Tags + Fb + palette per sampled frame);
  `tools/sprite_capture.py` (34 enemy / boss states x 480 frames with a walk + sword key script, every 8th frame);
  `tools/sprite_pack.py` (objects = 8-connected sprite pixels in palettes 1-3 outside the player banks, keys seen in
  >= 3 dumps only (drops fade / flash steps), the hero's repaint rules made generic with per-NES-colour hue-shifted
  ramps, cut back into tiles with the renderer's own per-line colour keys). Rejected first output: fade steps and the
  player in other palettes got painted (fixed by the >= 3-dump rule and leaving palette 0 out).
  `win_video_check.py enemypack`: areas $29 / $05: 3642 / 2045 enemy pixels repainted, hero untouched, pack loads clean.
