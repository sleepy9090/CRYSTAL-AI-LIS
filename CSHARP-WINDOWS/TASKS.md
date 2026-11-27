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
- [x] T13 (interactive, 2026-11-23) done: **Audio menu: Original / CD quality** (+ Stereo channels, Reverb, Mute).
  Core: output-only `Apu.OutChannels` (per-channel levels per sample; off by default; carried over by Machine.Reset).
  `CdAudio.cs` `CdMixer`: APU at 176.4 kHz, stereo panning through the NES mixer curves, 255-tap Kaiser low-pass +
  decimate x4 to 44.1 kHz, 20 Hz DC blocker (fuller bass than the NES 90 / 440 Hz high-passes), optional reverb, soft
  limiter, TPDF dither. `AudioOut` 2-channel waveOut in CD mode (reopened on switch). `--wav .. --cd [--mono] [--reverb]`.
  Tests 249/249 (`apu_channels`: mono stream identical with channel output on; `cd_audio`: 1 kHz +0.01 dB, 30 kHz
  -83 dB, silence within dither, P1 panned left, stereo-off balanced, music $14 +2.9 dB vs Original, no clipping,
  deterministic, reverb). Found on the way: `apu_wav_compare.py --exe 0x05 0x10` compared short effects that end after
  0.1 s; rerun on full songs `0x14 0x15 0x1B`: 352,959 samples identical each (README now lists those). Real-time Wine
  smoke: CD, and CD + reverb + switching Original <-> CD + Enhanced graphics: 0 underruns. `tools/wav_stats.py`.
  Final `dist/ReC.exe` 11,625,706 bytes, sha256 `f1b67961…`: core + exe vs web 156/156 each, win_video_check 18/18,
  smoke --canon (CD audio) ok, coverage 100 %, REA all expectations hold, cs_rea_members 1029/1029.
- [x] T14 (interactive, 2026-11-23) done: **Graphics: Enhanced (32-bit)** as a third option (Video > Graphics, F9 cycles).
  `Enhance.cs` Mode32: soft lighting (half-resolution height field from the visible colours, diffuse + brightness-scaled
  highlight, flat areas unchanged), soft sprite drop shadows (only where they land on another colour), edge
  anti-aliasing; pack tiles apply. First try was an emboss look (hard white streaks, grey highlights on black): toned
  down (shallower slope, softer highlight scaled by brightness, more ambient). 9.6 -> 5.2 ms / frame. Start-up audio
  stall: `TieredCompilationQuickJitForLoops=false` + a priming render. Tests 257/257 (`win_enhance32`: 76 -> 5588
  colours, one-colour frame unchanged, shadow down-right only, hidden sprite casts none, < 10 ms). win_video_check
  19/19 (+ `enhanced32`); exe vs web 156/156; smoke --canon ok; apu $14 identical; coverage 100 %; REA all hold.
  Final `dist/ReC.exe` 11,628,260 bytes, sha256 `e90a5826…`. Wine real-time 32-bit + CD: 1-2 underruns at start-up only.
- [x] T15 (interactive, 2026-11-24) done: **starter graphics pack for the backgrounds**, `packs/backgrounds/` (1,839
  tiles, 1,189 distinct). Capture: `sprite_capture.py --out out/bg --every 16` over all 86 states (2,580 dumps, 1,590
  unique screens). `tools/bg_pack.py`: BG pixels (Tags group 0-3) keyed with the renderer's per-line BG colours, keys
  in >= 3 dumps, HUD / window / font banks 38 39 3C 3D skipped; each tile placement painted from its own pixels
  (rim light / shadow only against a clearly darker neighbour, near-white never darkened, < 4-pixel specks kept),
  rounded with Scale2x on the NES colours carrying the painted colour, backdrop pixels transparent, most frequent
  painting per key. Rejected on screen under Wine: tile-local grain (speckled walls, blotchy paths), painting across
  tile edges (seam lines in flat ground: a flat tile took its edge painting), Scale2x on painted colours (blocky,
  no rounding), shading two-tone boundaries (striped shingles), darkening white highlight caps (grey cobbles), HUD
  banks 38 / 39 repainted (blotchy status bar). `tools/tag_banks.py` (banks / tiles in a screen rectangle).
  `win_video_check.py bgpack` (+ `--bg-pack DIR`): town $02 entry and area $29, 16-bit and 32-bit: pack loads clean,
  25-44 % of the picture repainted, hero pixels untouched. win_video_check 23/23. No exe change.
- [x] T16 (interactive, 2026-11-24) done: **starter graphics pack for the NPCs**, `packs/npcs/` (121 tiles): villagers /
  shopkeepers of 35 town / shop / inn / event / talk states (the `out/bg` dumps), + the ending's tower and the shop item
  display. `sprite_pack.py` options (defaults unchanged: packs/enemies regenerates byte-identical): `--only`,
  `--exclude` (keys already in another pack: no collisions), `--palettes 0,1,2,3` (NPCs in the hero's palette; hero banks
  still excluded), `--min-height 12` (cursors / icons), `--no-dark` (held fade-out steps), `--soft-dark` (people: half-step
  ramps, near-black hair / robes, untinted outlines). Rejected on screen: the enemy look on people (grey beard / robe
  slabs, red-tinted eyes, orange face bands); menu / player-only states (cursors, item icons, the hero in other forms).
  `tools/sprite_tiles.py`. `win_video_check.py npcpack` (town $18, armour shop; the hero's exact pixels from a headless
  `--dump-tags` run, frame checked equal to the exe's, as NPCs wear the hero's magenta). All four packs together: 2,554
  files, no name collides. No exe change.
- [x] T17 (interactive, 2026-11-24) done: **starter graphics pack for the bosses**, `packs/bosses/` (104 tiles, adds to
  packs/enemies, which already painted most boss sprites). `tools/pack_coverage.py` (per state: share of sprite / BG
  pixels whose key has a pack PNG, top missing keys) found the gaps: bosses in palette 0 (left out by the enemy pack),
  later phases (enemy capture = 480 frames), 8-px pieces. Boss runs recaptured at 1,500 frames (`out/boss/dumps`);
  `sprite_pack.py --palettes 0,1,2,3 --exclude packs/enemies,packs/npcs --no-dark`. Coverage of boss sprite pixels now
  75-100 % per boss state (rest: one-colour silhouettes, hit flashes). Boss arenas' background is the HUD (skipped) or
  black. `win_video_check.py bosspack` (area-$28 speech at its boss frames, fan turret; exact hero pixels): 25/25 with
  the rest. Found: the ai70msg state walked past its scenario crashes the core at an exit into area $A7 ("no
  record/header in areas.json"); the web port crashes the same way (data gap there, see the web fix).
- [x] T18 (interactive, 2026-11-24) done: **Graphics: HD (GPU)** as a fourth option (Video > Graphics, F9 cycles 4).
  `Gpu.cs`: Direct3D 11 via raw COM vtables + `D3DCompile` (HLSL SM 4.0, 5 shaders: full-screen VS, light, bright,
  blur, composite), drawn at the window's / screen's resolution. Height "models" from the visible shapes (`Enhancer`
  `HeightField`, shared with Light32: 32-bit output unchanged) + sprite lift (hidden sprites none); light pass with
  normals, diffuse + specular, AO, 16-step soft shadows; bloom; composite into the layout rectangle. Fallback to the
  32-bit look without Direct3D 11. Tuned once on screen (AO 0.55 -> 0.40, bloom 0.55 -> 0.40: halos on HUD text and
  dark rims at every colour edge). Tests: `win_enhance_hd` (albedo unshaded, height 0..1 with relief, flat frame flat +
  nothing lifted, hidden sprite not lifted, 2.4 ms, ini / --menu); `win_video_check` `hd` (Direct3D 11 hardware FL
  11.0 under Wine, 4x: 5 -> 3911 colours, 43404 / 61440 flat block centres exact), `hdfull` (1080p fullscreen, bars
  black), `hdoff` (HD -> Original: GDI picture exact again). `tools/hd_shots.py` (screenshots per mode). Not measured:
  GPU frame time on real hardware (Wine renders with llvmpipe on the CPU).
- [x] T19 (interactive, 2026-11-24) done: **HD upgrade: materials, shadows, parallax** (request: graphics on par with
  ~2010, shadows + parallax; engine kept: own Direct3D 11 renderer, since the rules allow no packages / engines).
  Core: output-only `Ppu.LineScroll` (architecture §5.14). `HdScene.cs` (scene analysis per frame), `Gpu.cs` light
  pass rewritten (materials with world-anchored relief, coloured sun / sky, glints, rim light, 12-tap AO, 24-step soft
  shadows, cloud shadows, chasm parallax: cliff walls + 3 rock layers + ledge shadows, motes, mist; bloom twice-blurred
  with a soft knee; composite tone curve + grade + vignette, HUD untouched). Video > Graphics > HD: four effect switches
  (`hd_effects=` in video.ini). Found while tuning: the game draws its pits with black tiles (not the backdrop), and
  `pitfall` changes scroll mid-screen (so the HUD rule needs text, not just another scroll). Tests: `win_hd_scene`
  (LineScroll keeps Fb, kinds, UI lines, void opening, camera wrap + cut, no HUD = no world effects, ini / --menu);
  `win_video_check` `hd` (now: text + cut-scene black exact), `hdfx` (cave gameplay, all effects on vs off: playfield
  changes, HUD identical, `hd_effects=none` saved).

- [x] T20 (interactive, 2026-11-24) done: **HD engine: own renderer, no PPU picture** (request: "dont use the ppu
  anymore, create a new graphics engine"). Core (architecture §5.15): `Ppu.LineScroll` replaced by the output-only
  `Ppu.Capture` (`VideoCapture.cs`: nametables / palette / OAM per frame + copies on mid-frame writes, registers per
  line, per mid-line write and per sprite evaluation) and `Ppu.NoPixels` (the PPU keeps its register / scroll / split
  behaviour but draws nothing). `SceneRaster.cs` draws the background and sprite layers from the capture (PPU fetch
  order incl. mid-line writes) and composes them; `Gpu.cs` + `GpuShaders.cs` rebuilt: layer + art-map uploads, pack
  atlas (2048x2048, 4096 tiles; all 2658 starter-pack tiles fit), Scale2x per layer 256 -> 512 -> 1024, albedo at 8x
  (2048x1920), jump-flood height field, emissive light map, then the T19 light pass + contact shadows, local lights,
  water / ice reflections. New switches `hd:reflections` / `hd:lights` / `hd:noflicker` (every sprite on a line).
  Tests: `win_scene_raster` (raster = PPU Fb + Tags on 10330 frames: canon boot + 87 states x 90, 45060 mid-line
  writes, 0 different; NoPixels: every compare-CSV row unchanged), `win_hd_engine` (no-flicker keeps the 8-sprite
  pixels, +2608 sprite pixels in 2610 frames; layer / art words; atlas slots; ini / --menu); 292 passed.
  `win_video_check` hd 5 -> 30741 colours, hdfx changes 623881 / 714240 playfield pixels with the HUD identical; all 31
  ok; smoke + canon ok; core_vs_web / win_vs_web 155/155 (`--canon --states --checks`); coverage 100 %; REA ok.
  Found: Wine's shader compiler cannot unroll loops with conditional jumps (new passes use `[loop]`); a ragged shore
  mirrors scenery into streaks (scenery reflects at 0.12, sprites fully). Previews: out/hd20/. Not measured: GPU frame
  time on real hardware (Wine renders with llvmpipe).
- [x] T21 (interactive, 2026-11-24) done: **HD polish** (request: "make the HD graphics even better"). Host shaders only
  (architecture §5.16): water refraction + grass wind sway, fine relief from the 8x art, sun shafts outdoors, two-level
  bloom, scene tint (warm outdoors / cool in the dark), FXAA-style edge AA pass (HUD exact). `win_video_check` hd 5 ->
  40853 colours (T20: 30741), hdfx 667799 / 714240 playfield pixels changed, HUD identical; packs, smoke, canon ok;
  292 tests; REA ok. Previews: out/hd21/ (`town_cmp.png` T20 vs T21).
- [x] T22 (interactive, 2026-11-24) done: **HD weather + particles** (request: "make the HD graphics even better" again).
  Host only (architecture §5.17): scene measures `Icy` / `Hot` (tuned on real frames: the fire hall floor is $17, the
  ice cave's whites are $30), snow / ice crystals, rising embers, falling leaves (2 depths, parallax), shoreline foam,
  characters' warm light in the dark. 296 tests (weather per scene asserted); hd 40853 colours, hdfx 701326 / 714240
  playfield pixels changed, HUD identical; smoke + canon, REA, coverage ok. Previews: out/hd22/ (`weather.png`, `snow.png`).
- [x] T23 (interactive, 2026-11-24) done: **HD lighting** (request: "make the HD graphics even better" a third time).
  Host only (architecture §5.18): bounce light (colour bleeding), directional local lights, eye adaptation (gameplay
  only), output dither. 296 tests; hd 5 -> 53982 colours (T22: 40853), hdfx 697713 / 714240, HUD identical; packs,
  smoke + canon, REA ok. Previews: out/hd23/ (`fire_cmp.png` T22 vs T23).
- [x] T24 (interactive, 2026-11-24) done: **HD heat shimmer + water sheen** (request: "make the HD graphics even better"
  a fourth time). Host only (architecture §5.19). An xBR-style replacement for the first rounding step was built and
  dropped: its test showed Scale2x already handles 2:1 slopes and xBR made them worse. 296 tests; hd 5 -> 53881
  colours, hdfx 700457 / 714240, HUD identical; smoke + canon, REA ok. Previews: out/hd24/.
- [x] T25 (interactive, 2026-11-24) done: **Built-in art for the HD engine** (request: "keep making the HD graphics
  even better"). The five starter packs are embedded in ReC.exe (+0.66 MB) and used by HD by default (switch
  `hd:art`; user packs override). 303 tests; `win_video_check` 32 ok (new `hdart`); smoke + canon, REA (members core /
  win), cs_rea_members ok. Previews: out/hd25/art_cmp.png (off vs on).
- [x] T26 (interactive, 2026-11-24) done: **More built-in art** (request: "keep making the HD graphics even better").
  Exploration capture (6450 dumps from 86 states), +1143 new pack tiles (backgrounds +697, enemies +378, NPCs +68; old
  tiles unchanged; 3801 in all, atlas 4096). Coverage of explored screens: sprites 59 -> 82 %, background 51 -> 58 %.
  303 tests; hd 5 -> 65049 colours; hdart / pack checks / smoke + canon / REA ok. Exe 12.5 MB.
- [x] T27 (interactive, 2026-11-24) done: **Pack art through palette fades** (request: "dont leave out anything"):
  fade-step keys map to the painted tile + step, the shader recolours it (architecture §5.22); atlas grows on demand.
  Tested in `win_builtin_art` (fade rule, nearest step wins, own tile kept, art word step bits).
- [x] T28 (interactive, 2026-11-24) done: **Remastered audio** (user: "feel free to ... upgrade ... the audio engine"):
  Audio > Remastered (architecture §5.23). 317 tests; Wine renders `out/audio28/music14_{cd,remaster}.wav`
  (-19.8 vs -18.7 dBFS RMS, peaks 27189 vs 14973); hd / hdfx / hdart / heropack, smoke + canon, REA ok.
- [x] T29 (interactive, 2026-11-24) done: **Art coverage to 98 % + HUD panel** (request: "keep going ... dont leave out
  anything"). Exploration variants 1-3 (+1363 tiles), new `packs/objects` (+178: hero forms, tower, clouds, items),
  5342 tiles in all (atlas grows to 4096x4096). Playfield coverage of all explored screens: sprites 98 %, background
  97-98 % (`pack_coverage.py` now leaves out the HUD / font banks and one-colour keys: never painted, on purpose).
  HUD panel gloss + text drop shadow (text pixels exact). 321 tests; win_video_check 32 ok; smoke + canon, REA,
  coverage ok; exe 12.8 MB.
- [x] T30 (interactive, 2026-11-24) done: **VSync + faster HD CPU side** (architecture §5.25): HD presents with vsync
  (no tearing; switch `hd:vsync`); CPU side 6.2 -> 3.8 ms / frame; pack previews no longer embedded. 323 tests; HD /
  pack checks, smoke + canon, REA ok. Under Wine's software GPU HD still underruns audio (~47 / 600 frames, GPU-bound).
- [x] T31 (interactive, 2026-11-24) done: **HD quality levels** (Full / Balanced / Fast) for slower graphics
  (architecture §5.26). Under Wine's software GPU Fast keeps real time (0 underruns vs 45 at Full). 325 tests; HD /
  pack checks, smoke + canon, REA ok. Previews: out/hd31/q_cmp.png (full vs fast).
- [x] T32 (interactive, 2026-11-25) done: **Clean edges + lawns** (request: "keep making the HD graphics even better";
  architecture §5.27). HD draws the overscan rows (and the backdrop strip under a bottom HUD panel) and the game's masked
  left column as black (switch `hd:edges`, default on; no weather over them); plain grass gets warm / cool patches,
  wind-leaning blades and sparse flowers (pack tiles only where they are the plain grass colour). 327 tests;
  win_video_check 32 ok (hdfx saves `hd_effects=noflicker,art,vsync,edges`); smoke + canon, REA, coverage ok.
  Previews: out/hd32/ (e_* edges, l_* lawns, l_zoom.png fence close-up before / after).
- [x] T33 (interactive, 2026-11-25) done: **HUD split seam, plain ground, pack seams** (request: "keep making the HD
  graphics even better, rebuild the engine as necessary"; architecture §5.28). The junk line at the status-bar split
  (and a padding band the game scrolls to on purpose: group05, pitfall) is black; plain floors (earth, stone, carpet,
  sand) get stains, grain, specks and outdoor pebbles; pack tile edges blend into their neighbours where the game's
  picture is continuous. A tone-matching pass was built and dropped (it dulled painted highlights). 328 tests;
  win_video_check 32 ok (hd 5 -> 66042 colours); smoke + canon, REA, coverage ok. Previews: out/hd33/ (n_* floors,
  g_* split seam vs original, s_* / n_zoom3.png seams).
- [x] T34 (interactive, 2026-11-25) done: **HUD style** (request: "enhance the hud"; architecture §5.29). Scale2x rounds the
  HUD / dialog glyphs (only toward HUD keys); text gets a top-left bevel and a brushed-metal sheen, the panel a brushed
  gloss and soft drop shadows (switch `hd:hud`, default on; off = exact NES text). 329 tests; win_video_check 32 ok;
  smoke + canon, REA, coverage ok. Previews: out/hd34/hud_cmp.png, out/hd34/inn_cmp.png.
- [x] T35 (interactive, 2026-11-25) done: **LIFE / FORCE meters** (request: "make the life and force bars look better
  too"; architecture §5.30). LIFE = glossy red capsule to the drawn HP (cell marks per 16 HP, sweeping shine, heartbeat
  when low); FORCE = the game's chevrons / segments as dark glass, glowing cyan when lit by the charge. Found in all 83
  HUD test states. 330 tests; win_video_check 32 ok; smoke + canon, REA, coverage ok. Previews: out/hd35/bars.png,
  out/hd35/charge.png / charge2.png (original vs HD while charging).
- [x] T36-T38 (2026-11-25, user away 4 h: "enhance all aspects ... add a menu for cheat codes such as infinite energy"):
  **Cheats menu** (infinite life with the NES bar redrawn as HudHpBar draws it, infinite magic, max gold, no paralysis /
  poison, instant sword charge, quick level-up; gameplay only, ini `cheats=`), **fast forward** (hold Space, 4x),
  **on-screen display** (pause screen with keys + active cheats, notices, fast-forward mark; HD final pass + GDI blend),
  **turbo A / B** (Game menu). architecture §5.31-32. 344 tests (new win_cheats: tiles = HudHpBar on 400 random pairs;
  win_osd); win_video_check 32 ok; smoke + canon, REA, coverage ok. Previews: out/hd37/pause/.
- [x] T39-T40 (2026-11-25, user away): **Menu screens in HD** (equipment, status, shops, item use: HUD style on the
  menu windows, panel colour from font tiles, backdrop strips black; bottom band anchored on the status bar's fixed
  scroll) and **Audio › Volume**. architecture §5.33. 347 tests; win_video_check 32 ok; smoke + canon, REA, coverage
  ok. Previews: out/hd39/ (s_* after, menus.png).
- [x] T41-T42 (2026-11-25, user away): **Story / cut-scene text styled** (panel colour from font tiles without a status
  bar; black panels: glyph look + faint glow), **meter detection hardened** (an all-black death screen drew a LIFE
  capsule), **F12 screenshot**, `hd_shots.py reset`. 349 tests; win_video_check 32 ok; smoke + canon, REA, coverage ok.
  Previews: out/hd41/ (title.png intro original vs HD, intro_text.png, death_b.png).
- [x] T43-T44 (2026-11-25, user away): **status-bar shadow** (the not-yet-loaded tile row under the split, group81, is
  hidden: the map fades to black from the last tile row above the panel), **indoor greens** = moss / carpet (no lawn),
  **F1 help** panel. 351 tests; win_video_check 32 ok; smoke + canon, REA, coverage ok. Previews: out/hd43/, out/hd44/help.
- [x] T45-T46 (2026-11-25, user away): **Video › CRT look** (Original graphics: 3x scanlines + RGB aperture mask) and
  **controller extras** (RB = fast forward, LB = pause). 353 tests; win_video_check 32 ok (T45); smoke + canon (T46)
  ok; REA, coverage ok. Preview: out/hd45/crt_cmp.png.
- [x] T47-T49 (2026-11-25, request "update the graphics engine and the audio engine"): **audio engine v2**
  (AudioFx.cs: resampler + rate control instead of dropped blocks, continuous fast-forward sound, Audio › Channels
  mutes, Audio › Headphone crossfeed), **HD sharpening** (contrast adaptive, HD › Sharpen) + **water caustics**,
  **FPS counter** (F2). 379 tests. Preview: out/hd48/sharpen_cmp.png (off | on).
- [x] T50-T51 (2026-11-25, request "orchestrated audio option, movie-type cut-scenes"): **Audio › Orchestrated**
  (Orchestra.cs: violins, horns, string basses, timpani / snare from the chip's notes, concert hall; sound effects keep
  the chip sound) and **cinematic cut-scenes** (story / ending pictures: camera push-in with height parallax, embers or
  dust, light rays, lens streaks, film grade and grain). Demo WAVs: out/audio50/ (music_0x14/15/1B, cd vs orchestra).
  Previews: out/hd51/.
- [x] T52 (2026-11-25): **HD › Smooth outlines**: the 2x→4x and 4x→8x upscaling steps vote (Gaussian key vote) instead of
  Scale2x: curves instead of pixel steps (characters, crystals, shores). Preview: out/hd52/cmp29.png.
- [x] T53-T54 (2026-11-25): **HD › Ambient bars** (the bars beside the picture glow with a dim, blurred mirror of it)
  and **Audio › Ambient sounds** (wind + birds outdoors, cave rumble + drips, fire crackle, ice whistle; from the HD
  scene). Demo WAVs: out/audio50/ambience_*.wav.
- [x] T55 (2026-11-25): **HD › Tilt-shift depth of field** (gameplay: the top and bottom of the playfield soften, the
  middle stays sharp; the HD-2D diorama look).
- [x] T56 (2026-11-25): **frosted-glass pause / help screens** in HD (the game behind the overlay is blurred).
- [x] T57 (2026-11-25): **damage feedback** (Game menu): HD red vignette flash on a hit, heartbeat pulse at low life,
  controller rumble. Preview: out/hd57/hit/0_hd.png.
- [x] T58 (2026-11-25): **low-life heartbeat** sound (Damage feedback, any mode).
- [x] T59 (2026-11-25): **HD › Modern font**: dialog / story text drawn from a Windows-font glyph atlas (crisp, sized on
  the game font), in the game's colours. Preview: out/hd59/talk/0_hd.png.
- [x] T60 (2026-11-25): **Video › Display**: brightness 1-5, Natural / Vivid colour, colour-blind assist (protanopia,
  deuteranopia, tritanopia; daltonize), all graphics modes. Preview: out/hd60/cmp.png.
- [x] T61 (2026-11-25): **remappable keys** (Game › Remap keys..., an on-screen prompt per button). Preview:
  out/hd61/0_hd.png.
- [x] T62-T63 (2026-11-26): **HD quality: Auto** (the default; steps down / up with the frame rate) and **save backups**
  (re-c.sav.1-.3).
- [x] P1 (2026-11-26, playtest: "audio delayed and choppy, especially entering a new area; graphics take a little to
  fill in while moving"): ReadyToRun exe (no JIT stalls on new game routines), audio flushed before drawing, 8 × 512
  sample buffers, queued audio capped at ~128 ms, low-latency GC. The fill-in could not be reproduced in the game's
  own output (scrolling frames fill correctly); the JIT stalls are the likely cause (frames shown late).
- [x] W1-W2 (2026-11-26, request "add widescreen playfield"): **Video › Widescreen playfield** 16:10 / 16:9 / 21:9 in
  Original and HD graphics: the columns beside the screen are drawn from the area's own map (exact, the camera of
  each frame), fade in only while the screen shows the map. Previews: out/wide1/ (Original), out/wide2/ (HD 16:9).
- [x] P2 (2026-11-26, playtest 2): widescreen crash after name entry fixed (bounds-checked off-thread area build),
  widescreen drops / artifacts (cache rebuilt every 4 frames + from half-loaded areas; hysteresis; no void beyond the
  area), **objects drawn in the side columns** (replay of the game's sprite pass), orchestra pops (543 → 4 clicks per
  minute on the instruments; a disabled effect detector, per-sample allocation, hard level switches), audio cut-outs
  (no more drops on catch-up bursts), display lag (1 frame queued instead of 3).
- [x] P3 (2026-11-26, "fix the HD edge seam too"): the widescreen seam in HD (scene info of the masked column, void
  mask cut at the old edge) fixed; `win_wide` checks the seam columns. Preview out/wide5/seam_cmp.png.
- [x] P4 (2026-11-26, playtest 3: "tearing, glitching, blacking out in widescreen; orchestra audio delayed, popping"
  + "add mouse support for moving the character"): per-line verified widescreen mapping (mid-frame scroll splits),
  no per-frame large allocations (GC stalls), crossfaded backlog splice + fade-in after underruns, DwmFlush VSync
  for the GDI modes, **mouse control** (hold left to walk, right = sword, middle = A).
- [x] P5 (2026-11-26, playtest 4: "crackling/popping on scene change; huge black areas on the sides in widescreen HD"):
  no ~30 MB area allocations per scene change (blocking GCs), DMC level jumps ramped, screen shake kept, camera
  corrections searched, no-map side pixels filled with a soft mirrored glow instead of black.
- [x] P6 (2026-11-26, "add an option to take a screenshot"): Game › Take screenshot (F12) + Open screenshots folder; unique names.
- [x] P7 (2026-11-26, playtest recordings in `issues/`): the crackle = a DC offset from oscillator phase run-away (fixed + DC blocker); soft fire ambience; widescreen sides only in gameplay, stricter line proof.
- [x] P8 (2026-11-26, playtest 6): no dolly zoom / film look on the title, status and name-entry screens; name-entry glyphs B D Q 0 no longer inverted.
- [x] P9 (2026-11-26, playtest 6 recordings 3-5): widescreen cave bands no longer black, one offset per frame (no tears).
- [x] P10 (2026-11-26, playtest 6): spoken NPC dialogue (Windows text-to-speech), Audio menu toggle.
- [x] P11 (2026-11-26, playtest 6): per-NPC voices by sex and age (125 NPCs classified), neural Piper voice in dist\voice (fallback: Windows voice).
- [x] P12 (2026-11-26, playtest 6): no game sprite flicker in HD (objects the sprite pass skipped are drawn from object RAM).
- [x] P13 (2026-11-26, playtest 6): widescreen map continues behind the HUD (panel overlays it), scroll-edge seam fixed; Piper ships the VC++ runtime, voice.log + fallback notice.
- [x] P14 (2026-11-26, playtest 6): no ghost of the player in the wake-up cut-scene (no-flicker fill only in SubstPlay).
- [x] P15 (2026-11-26, playtest 7): build time in the title; no side line drop-outs (FrameGood); NPCs whole at the game-column edge.
- [x] P16 (2026-11-26, playtest 7): remade intro jungle (StoryDino, GPU-painted over the whole width); other story scenes next.
- [x] P17 (2026-11-27, playtest 8): story text kept, jungle only on its page, no title streaks, softer character shadows, smooth heat shimmer, masked-column sprites, Debug info, Warp cheat, Kokoro voice.
- [x] P18 (2026-11-27, playtest 9): pod silhouette recoloured, no title relief lines, softer character halo/shadow, smooth no-map fill, Kokoro fallback + full log + Voice engine menu, debug HD/sprites/input/audio, cheats items/level/speed.
- [x] P19 (2026-11-27, playtest 10): Kokoro voice server (model loaded once) + sentence streaming; per-page program as fallback.
- [x] P20 (2026-11-27, playtest 10): widescreen sides no longer fade with animated tiles / scrolling (area key).
- [x] P21 (2026-11-27, playtest 11): shop and inn dialogue spoken (nametable reader, keeper voices).
