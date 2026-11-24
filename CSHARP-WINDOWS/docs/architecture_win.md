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
│                           VideoMenu.cs (T8 video settings / menus), Upscale.cs (T9),
│                           Enhance.cs + PngRead.cs (T10 Enhanced graphics + pack),
│                           CdAudio.cs (T13 CD-quality audio), Audio.cs (waveOut),
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
10. **Enhanced (16-bit) graphics (T10) — the one core change outside data loading:** `Ppu.Tags` (null = off, the default
    and every parity run) records per Fb pixel the CHR tile (byte address / 16), in-tile row / col (tile space), flips
    and palette group of the layer that won, in `RenderLine`, `EvalSprites` and the mid-line `RenderLineTimed` path; Fb
    is never touched by it (`tests` `ppu_tags`: Fb identical with tags on over the canon run; 13.6 M tagged pixels, one
    colour per line + group + slot, 0 conflicts). Host `Enhance.cs`: per frame, dither blend (screen) → Scale2x twice
    (screen) → bevel shading from a separable distance-to-edge transform of the visible colours (screen, so hidden
    same-colour shapes never show) → hand-drawn pack tiles `TTTT_AABBCC.png` (32x32 RGBA, keyed by tile + the line's
    own slot colours, learned from the frame because the game changes palettes mid-frame). Rows in parallel; ~4 ms per
    frame after JIT warm-up (`win_enhance`). `--export-tiles DIR` writes the built-in look + the 4x original of every
    non-blank, non-one-colour tile + colours seen. `video.ini` key `graphics`; F9 toggle, F5 reload pack.
11. **CD-quality audio (T13) — second output-only core addition:** `Apu.OutChannels` (null = off, the default and every
    parity run) passes the raw channel levels averaged over each sample period (pulse 1/2, triangle, noise 0-15, DMC
    0-127); `Machine.Reset` carries it over like `Out`. The mono `Out` stream is unchanged (`apu_channels` test:
    identical with the channel output on). Host `CdAudio.cs` `CdMixer`: APU at 176.4 kHz → per-side panning (P1 L,
    P2 R, noise slightly R) through the NES mixer curves → 255-tap Kaiser low-pass (20 kHz, −83 dB at 30 kHz) decimating
    ×4 → 20 Hz DC blocker (instead of 90 / 440 Hz) → optional Freeverb-style reverb (12 % wet) → gain 1.3, soft limit
    above 0.8 FS, TPDF dither (fixed seed). `AudioOut` opens waveOut as 2-channel 44.1 kHz 16-bit in CD mode and
    reopens it on a switch. Audio menu + `video.ini` keys `audio` / `audio_stereo` / `audio_reverb`; `--wav .. --cd`.
    Level of music `$14` vs the in-game Original path: +2.9 dB, no clipping.
12. **Enhanced (32-bit) graphics (T14):** `Enhancer.Mode32` (`video.ini` `graphics=enhanced32`, F9 cycles 3 modes).
    After the 16-bit dither blend + Scale2x x2: `Light32` on the 512x480 step (4x fewer pixels; lighting is smooth):
    distance to the nearest other colour (cap 5) → pillow height → 3x3 blur → normals → Lambert (ambient 0.55) +
    Blinn nh^18 highlight scaled by the surface brightness, normalised so flat areas keep their colour; applied to the
    1024x960 image per 2x2 block. Pack tiles; then `Shadows32` (sprite pixels whose colour differs from where the
    shadow lands cast a 40 % shadow 2 NES px down-right, blurred at 1x and bilinear-upsampled, never on sprites) and
    `AntiAlias32` (edge pixels → (4c + 4 neighbours) / 8). 5.2 ms / frame after JIT warm-up (`win_enhance32`, from 9.6
    ms: half-resolution lighting, 1x shadow blur, SIMD / fused blurs). First-frame stall fixed by
    `TieredCompilationQuickJitForLoops=false` (ReC.Win.csproj) + one Enhanced render before the audio starts. Wine
    real-time: 1-2 audio underruns at start-up with 32-bit + CD (16-bit: 0-1), none dropped, not growing.
13. **HD graphics (T18, GPU):** `Gpu.cs`, `video.ini` `graphics=hd`, Video > Graphics > HD, F9 cycles 4 modes. Direct3D 11
    through raw COM vtables (`d3d11.dll` `D3D11CreateDeviceAndSwapChain`, hardware then WARP; `d3dcompiler_47.dll`
    `D3DCompile` of the embedded HLSL, SM 4.0; no packages). CPU part (`Enhancer.ModeHd`, 2.4 ms): albedo = the
    Enhanced picture without step 3 (dither blend, Scale2x x2, pack tiles), `HdHeight` = the 32-bit look's height
    field (`HeightField()`, shared with `Light32`: pillow profile of the visible shapes, 512x480), `HdLift` = sprites
    whose edge shows a colour different from the non-sprite pixel beside them (hidden sprites get no height, cast no
    shadow). GPU part at the picture size of the window (any resolution): light pass to RGBA16F (normals from the
    height model + lift, Lambert + Blinn-Phong normalised so flat ground = albedo exactly, 8-tap AO, 16-step soft
    shadow march toward the light, "sharp bilinear" albedo), bloom of the part above 1.0 at 1/4 size (Gaussian H + V),
    composite into the picture rectangle of the swap chain (bars black). Paint goes to `Gpu.Draw` while HD is on; the
    swap chain is released when another look is chosen (GDI draws again); `--dump-window` reads the back buffer
    (`Gpu.Capture`, staging copy). No Direct3D 11 / compiler -> stderr + title say so and the 32-bit look is used.
    Under Wine (wined3d on Xvfb's llvmpipe GL) the device is "hardware, feature level 11.0" and all shaders compile.
14. **HD materials, shadows, parallax (T19):** second core output hook `Ppu.LineScroll` (null = off: every parity run;
    set with Tags while Enhanced is on): per line, after the split hook, v (15 bits) | fine X << 16, -1 = rendering off;
    never read back (`tests` `win_hd_scene`: Fb identical with it on). `HdScene.cs` per frame (CPU): Info texture
    256x240 (lift, NES colour, flags sprite / void / UI, bilinear void mask), Lines (world x, y of each line in the
    512x480 nametable space + UI flag), camera (frame-to-frame playfield scroll delta, wrap-aware, > 48 px = cut),
    outdoor (mid-tone greens), void gate, mean colour, play (HUD present). Playfield = the most common per-line offset
    (world y - line); UI = rendering off, or lines of another offset with font-bank text (CHR 1 KB banks $38 $39 $3C
    $3D) within 8 lines in the same offset run (a mid-screen scroll change without text stays playfield). Void = NES
    blacks of the background (or a dark backdrop) opened with a 3x3 square. Material table: 64 entries by hue.
    `Gpu.cs` light pass adds: world-anchored value noise (lattice periods divide 512 x 480, so it repeats with the
    nametables) per material as albedo variation + detail normals; coloured sun / sky normalised by luminance; Blinn
    per material + glints; rim light on sprites; 12-tap AO; 24-step soft shadow with distance penumbra; cloud
    shadows (outdoor x atmosphere x shadows); chasm (cliff walls by stepping toward the screen centre through the void
    mask, 3 fBm rock layers at rate 1 / (1 + 0.05 depth) in scale and camera, ledge shadow lookup toward the sun, rim
    darkening); motes (rate 1.25) and mist (1.3). UI pixels go out with alpha 0 and skip bloom and grading. Composite:
    shoulder tone curve (identity below 0.75), saturation 1.08, contrast 0.12, vignette 0.22. Four switches
    (`Gpu.Parallax / Shadows / Atmosphere / Detail`, menu ids 275-278, `--menu hd:parallax` etc. toggle).
