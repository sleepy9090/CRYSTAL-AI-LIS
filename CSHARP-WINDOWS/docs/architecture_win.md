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
14. **HD materials, shadows, parallax (T19):** second core output hook `Ppu.LineScroll` (T20: replaced by item 15; null = off: every parity run;
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
15. **HD engine (T20): the HD look no longer uses the PPU's picture.** Core: `Ppu.LineScroll` (item 14) is replaced by
    `Ppu.Capture` (`Nes/VideoCapture.cs`, null = off: every parity run) and `Ppu.NoPixels` (false = default). Capture
    records per frame the nametables / palette / OAM as the frame starts (a new copy only if the game writes one while
    the frame is drawn), per line the registers it starts with after its split hook (v, fine x, PPUCTRL, PPUMASK, the 8
    CHR bank offsets), the registers after every mid-line write (`Timed`, with its dot) and the registers each line's
    sprite evaluation used; output only. NoPixels skips `RenderLine`'s pixels, `RenderLineTimed`'s drawing and the
    sprite decode (overflow flag still set); all register, scroll and split behaviour is unchanged and the game never
    reads the picture. While HD is chosen both are on (Tags off). Host: `SceneRaster.cs` draws the background and
    sprite layers from the capture with the PPU's fetch order (tiles 0 / 1 at the line start, tile t at dot 8(t-1); a
    write at dot D is seen by fetches at >= D and pixels >= D-1) and composes them as the PPU does: `win_scene_raster`
    checks Fb + Tags against the PPU on every frame of the canon boot run + 90 frames of each of the 87 web test states
    (10330 frames, 45060 mid-line writes: 0 different) and that NoPixels leaves every compare-CSV row unchanged.
    `SpriteLimit` false (default, `hd_effects` `noflicker`) shows every sprite on a line; the 8-sprite picture's pixels
    are kept (`win_hd_engine`). `Gpu.cs` + `GpuShaders.cs`: uploads `Gpu.Layers` (per pixel uint4: background colour +
    opaque + HUD, sprite colour + present + behind, symmetric dither pair) and an art map (uint2: pack atlas slot + 1,
    tile column / row, flips, per layer; keys from each line's own palette), atlas 2048x2048 = 64x64 pack tiles of
    32x32 (`SetArt` at start and on F5). Frame passes: Scale2x on the keys (background and sprite layers separately, HUD
    exact) 256 -> 512 -> 1024; albedo 2048x1920 (third Scale2x step, pack tiles sampled bilinear inside their cell,
    dither mix, PPU priority); height = jump flood (4, 2, 1) to the nearest edge of the 2x keys, pillow of 1 + distance
    (cap 5), 3x3 blur; light map 128x120 of emissive colours ($27 $28; $16 $26 $37 at 0.55) blurred 3 x H + V. Draw
    passes as T19 plus: sprite contact shadow (5 taps of the lift above), local light from the map scaled by darkness
    (mean playfield luminance 0.12-0.30), water / ice reflection (march up to 28 px to the shore, mirror, ripple,
    sprites full / scenery 0.12, softened). Wine's vkd3d compiler cannot unroll loops with conditional jumps: the new
    passes use `[loop]`. Without Direct3D 11 HD uses the raster's Fb + Tags in the 32-bit look (no PPU pixels either).
    Menu ids 279-281 (`hd:reflections`, `hd:lights`, `hd:noflicker`).
16. **HD polish (T21, host only):** light pass: albedo resampled with a per-pixel offset (dark water `c < $20` of kind 2:
    world noise waves 0.7 px; foliage: wind sway 0.3 px with gusts) under the material-detail switch; fine relief =
    luminance gradient of the 8x albedo (±1.5 texels, x 0.5, clamped ±0.3) added to the detail normal; sun shafts =
    value noise across the light direction (world-anchored at camera rate 1.15), outdoor x atmosphere. Bloom gets a
    second level (1/8, blurred twice, weights 0.75 / 0.55). Composite adds the scene tint (outdoor warm, darkness cool)
    and writes an RGBA8 picture with HUD alpha 0; `PSFinal` = FXAA-style anti-aliasing (diagonal lumas, edge direction,
    2 + 2 taps, range test) into the swap chain, HUD pixels copied exactly.
17. **HD weather (T22, host only):** `HdScene` adds `Icy` (share of light kind-2 colours, whites half, smoothstep
    0.25-0.45) and `Hot` (share of $06 $16 $17 $27 $28, 0.35-0.55, times a dark-screen factor: non-solid share
    0.25-0.45), both gated by the HUD and smoothed like `Outdoor`; sent as `Opt2.z / w` (x Atmosphere switch).
    Shader `Particle()` = one particle per cell of a world grid moving with a velocity, at a camera rate > 1 (closer
    than the playfield); `Weather()` = snow (2 layers) and embers (additive, > 1 so bloom catches them); leaves are
    blended in outdoors. Shoreline foam: dark water (`c < $20`) with non-water within 1.6 px (8 taps), animated world
    noise. The light map also gets the sprite lift (warm, 0.35): characters light their surroundings in the dark.
    `win_hd_engine` checks the measures on four scenes (ice cave snow, fire hall embers, town leaves only, red cave none).
18. **HD lighting (T23, host only):** bounce light = `PSBounce` (playfield albedo averaged per 2x2 NES pixels, HUD weight 0,
    weight in alpha) at 128x120 + 2 x H + V blur at a 2-texel step, bound at t11 (`Pass` got a 5th input); the light
    pass tints the sky ambient by the luminance-normalised bounce colour (35 %, clamped 0-2). Local lights take their
    direction from the light map's gradient (`LocalDiffuse`: 0.35 + 0.9 n.l). Eye adaptation on the CPU (`Upload`):
    exposure = sqrt(0.42 / mean playfield luminance) clamped 0.9-1.22, times gameplay (cut-scenes stay 1), eased 5 % per
    frame, applied before the tone curve (`Grade.x`). Composite adds +-0.5 / 255 hash dither.
19. **HD heat + sheen (T24, host only):** heat shimmer = albedo offset (up to 1.2 px, fast-rising value noise) where the
    light map 6 lines below is brighter than here (x 12, gated by its level); sky sheen on kind 2 = sky x (0.06 + 0.10
    toward the screen top) x drifting world noise, ice (`c >= $20`) at 0.2; both under the material-detail switch.
    Tried and dropped: an xBR-style first 2x step on the keys. A test showed Scale2x already draws 2:1 slopes as straight
    lines at 2x and the xBR rule turned them into 2-row stairs; the remaining steps are the game art's own irregular
    steps, which a faithful upscaler keeps.
20. **Built-in art (T25):** `ReC.Win.csproj` embeds `packs/**/*.png` as `re-c.pack.<folder>/<file>` (+0.66 MB exe, the
    PNGs total 848 KB). `BuiltinArt.Embedded()` decodes them once (`PngRead`, 32x32 each) when the HD engine builds its
    atlas; `Window.GpuArt()` = built-in (if `HdArt`, `hd_effects` `art`, menu id 282) merged under the user pack and
    sent to `Gpu.SetArt` (at device creation, F5, and the switch). The Enhanced 16-bit / 32-bit looks still use only
    the user pack. `win_builtin_art`: 2658 files -> 2658 tiles, no collisions, fits the 4096-slot atlas, user overrides;
    `win_video_check` `hdart`: atlas line, 501865 playfield pixels repainted, HUD identical.
21. **Pack coverage (T26):** `sprite_capture.py --explore` (2400-frame walk) over all 86 states + `pack_coverage.py
    --sample N` (TOTAL line); new keys only were added to packs/backgrounds / enemies / npcs (3801 tiles, built into the
    exe: 12.5 MB). Explored screens: sprites 59 -> 82 %, background 51 -> 58 %. `win_video_check` `hdart` now counts the
    packs/ files.
22. **Fades for pack art (T27):** `Gpu.BuildFades` maps every key a palette fade step (1-3, `BuiltinArt.Fade` = the ROM's
    c - $10 k, negative -> $0F) makes of a pack tile to that tile + step (smallest step wins; keys with their own tile
    and all-black steps excluded). The art word got 15 slot bits + fade step bits 23-24; the atlas is 2048x2048 up to
    4096 tiles and grows to 4096x4096 (`AtlasN` 128) only when needed (shader reads the size). `PSAlbedo` `Faded()`:
    painted colour x luminance ratio onto the faded NES colour on screen (black stays black).
23. **Remastered audio (T28):** `CdMixer.Remaster` (`audio=remaster`, Audio menu id 414, `--wav .. --remaster`): 4 buses
    (pulse 1, pulse 2, triangle, noise + DMC) through their mixer curves and the shared 255-tap low-pass / decimation;
    at 44.1 kHz chorus per pulse (6-10 ms delay, LFO 0.5 / 0.37 Hz, cross-panned), triangle one-pole 5 kHz + tanh,
    pulses panned 1.05 / 0.40 + chorus 0.60, noise slightly right, DC blocker, Freeverb room with per-bus sends (return
    0.2), RBJ low shelf +2.5 dB @ 120 Hz and high shelf +1.5 dB @ 7 kHz, gain 0.45 x, shared limiter + dither.
    `win_audio_remaster`: music $14 +0.4 dB vs CD, peak 13978, mid / high L/R correlation 0.813 (CD 0.933),
    triangle step buzz reduced, shelf gains, deterministic, ini / --menu.
24. **Coverage + HUD (T29):** `sprite_capture.py --explore V` variants 1-3; `packs/objects` (palette-0 sprites outside
    the hero banks, min height 9); `pack_coverage.py` leaves out HUD / font banks and one-colour keys unless `--all`.
    5342 tiles (atlas 4096x4096). HUD panel: `HdScene.HudFill / HudTop / HudBottom` (most common colour of the HUD
    lines, not black), constants row 14 `HudC`; `HudPanel()` in the light pass: fill pixels x (1.10 - 0.20 t + 0.08
    gloss band), x 0.7 under a non-fill HUD pixel up-left (drop shadow); text / frame pixels unchanged.
25. **VSync + CPU side (T30):** `Gpu.VSync` (`hd_effects` `vsync`, default on, menu id 283; never with `--turbo`)
    presents with sync interval 1. CPU side of an HD frame 6.2 -> 3.8 ms (`win_hd_engine` times it, limit 5 ms):
    art words cached per run of equal tile + group keys (`PosBits` added per pixel), `HdScene` font-bank lookup table
    and separable 3x3 erode / dilate (`Box3`). Embedded packs: `packs/*/*.png` only (the preview PNGs stay out).
    Real time under Wine (llvmpipe renders the shaders on the CPU): ~47 audio underruns in 600 frames with HD on, 0
    with Original; vsync on / off makes no difference. Not measured on real GPU hardware.
26. **HD quality (T31):** `Gpu.Quality` (`hd_quality=full|balanced|fast`, menu ids 284-286, `--menu hd:fast` ..): HDR /
    bloom targets at 1 / 0.75 / 0.5 of the picture (composite + FXAA upsample to the picture), shadow steps 24 / 16 / 10
    (`QualC.x`), reflections + fine relief off in fast (`QualC.y`), fast builds the albedo at 4x (1024x960, from the 2x
    keys, no K2 pass; `QualC.z` = albedo scale, HLSL `ALB` macro). Real time under Wine (llvmpipe), group29, 600 frames,
    CD audio: full 45 underruns, balanced 22, fast 0 (42 samples dropped); Original 0.

27. **Clean edges + lawns (T32):** `Gpu.Edges` (`hd_effects` `edges`, default on, menu id 287, `--menu hd:edges`): `Layers`
    writes exact black (HUD bit, no sprite, no art) to the lines from `Gpu.EdgeBand` (232 = NTSC overscan, where the game
    leaves stray tiles; higher over the plain backdrop rows right above it when a HUD panel sits at the bottom) and to the
    left 8 pixels of lines the game masks (`SceneRaster.LeftClip`: PPUMASK bit 1 clear, they show the backdrop colour).
    PSLight treats a `Lay` HUD bit as HUD (no weather / lighting over the edges). `Lawn()` (material detail, foliage
    kind 5): background without pack art, and plain grass-coloured pixels of pack tiles, get brightness-based warm / cool
    patches (the NES greens are too saturated for a multiply), 20 % less neon, wind-leaning blade strokes and sparse
    five-petal flowers (world-anchored: they stay put while the map scrolls).

28. **HUD split seam, plain ground, pack seams (T33):** `Gpu.SplitGap` (with `Edges`): the gameplay HUD's scroll is fixed
    (nametable 2, row 24, v & `$0FE0` = `$0B00`; IRQ handler 1 at line 190, REVERSE.md), so its first line is found
    from `SceneRaster.LineScroll`; the line above it (half the HUD's top row, stray tiles) is black, and so is a padding
    band above that when the game scrolled there on purpose (the scroll does not follow the PPU's own y increment
    `NextLine`, from line 150, mostly dark): 83 of 86 test states have the seam, group05 / pitfall a band.
    `Ground()` (material detail, kinds 1 / 3 / 4, background without pack art): stains, grain, specks, pebbles on outdoor
    earth, very saturated fills 22 % less neon, white in icy places only a trace. `Seams()` in PSAlbedo: within one NES pixel
    of a pack tile's edge, where the NES colour across the edge is the same, blend toward the neighbour's art at the
    shared edge (both sides meet at 50 / 50). Tried and dropped: a tone pass (NES colour − art average per NES pixel,
    blurred, added to the albedo) pulled painted highlights (ice, characters) back toward the NES palette.

29. **HUD style (T34):** `Gpu.HudStyle` (`hd_effects` `hud`, default on, menu id 288, `--menu hd:hud`; constant `HudC.w`).
    Scale2x now also rounds HUD pixels (only toward other HUD keys: `(X.r & 0x80) == (E.r & 0x80)`), so letters, digits,
    meters and the panel frame are smooth at 8x; PSAlbedo no longer bypasses Scale2 for HUD pixels. `HudPanel`: the
    glyph mask is "albedo differs from the panel fill colour"; glyphs get a bevel lit from the top left (mask gradient
    1.5 texels around), a brighter top edge and a cooler darker lower edge (half an NES pixel up / down: brushed metal),
    dark glyphs a little light; the fill gets the gloss, a brushed sheen and a soft drop shadow (4 mask taps down-right).
    Dialog boxes use the same path (same fill colour). Off = the T29 look (exact text, hard 1-pixel shadow).

30. **HUD meters (T35, with HUD style):** `Gpu.Meters` finds both under the HUD's fixed scroll: LIFE = nametable row 25
    (`HudHpBar` `26:8CC0`: a black frame from column 6 up to max HP, a cell per 16 HP; frame x-extent from its top line),
    FORCE = row 26 (charge meter `26:8D7A`, from x 48 to the last non-fill column). The LIFE fill end is column 6 + HP / 2
    with HP = RAM `$03C1` (`Gpu.Hp`, set by Window before Upload; the bar is drawn from it). Constant rows 16 / 17
    (`MeterL`, `MeterF`; NConst 18). Shader: LIFE = an anti-aliased rounded capsule (`SdRound`), rim with a light top lip,
    dark glass trough, red gradient fill, a cell mark every 16 HP, a shine sweeping along, a heartbeat under 25 %; the
    NES frame's square corners show the panel. FORCE keeps the game's (rounded) chevrons / segments: unlit (black) =
    bevelled dark glass, lit (any other NES colour of the pixel) = glowing energy (white -> cyan, or the colour's own hue)
    with a pulse running along. Found in all 83 HUD test states (`win_hd_engine`).

31. **Cheats (T36):** `Cheats.cs`, menu **Cheats** (ids 600-605, 620 = all off; `--menu cheat:life` ..; ini `cheats=`).
    RAM writes after every frame in gameplay only (`main_state` 1): life (`$03C1` = `$03C0`; in plain play, sub_state 8,
    the 17 LIFE tiles at `$2B26` are rewritten as `HudHpBar` `26:8CC0` draws them, VRAM only: the `$6060` buffer is
    shared with the charge meter), magic (`$0708` = `$0709`), gold (`$0702/3` = 60000), status (obj1_flash `$0710`
    low nibble 1 / 3 cleared), charge (`orbit_level` `$06C0` = `SwordChargeCap` `26:9D7E`[`$0719`] while B is held), xp
    (`$0704` = `$0706` − 1). `win_cheats`: the tile rule = the ported HudHpBar on 400 random max / HP pairs. Off by default
    (canon / smoke / parity never use them). Space held = fast forward (4x frames; audio drops what does not fit).
32. **On-screen display (T37):** `Osd.cs`: a 512x480 premultiplied overlay over the visible picture with a built-in 5x7
    font: pause screen (dim 62 %, a 94 % panel with PAUSED, the keys, active cheats), notices for 2 s (cheat / graphics /
    sound switched; only after start-up, so `--menu` runs stay clean), the fast-forward mark. HD: texture `_oTex`, PSFinal
    = `Fxaa()` + composite when `QualC.w`; GDI modes: `Osd.Blend` into a copy of the visible picture. `--osd
    pause|fast|notice=TEXT` sets one before `--dump-window` (checks, `hd_shots.py --osd`).
    **Turbo (T38):** Game menu Turbo A / B (ids 103 / 104, ini `turbo=a,b`): a held button is released every other 3 frames
    (`VideoSettings.Turbo`; not for `--keys` runs).

33. **Menu screens (T39):** the HUD style reaches the game's menu windows (equipment, status, shops, item use): font tiles
    are flagged per pixel (`FUi`) and menu windows are drawn with the playfield's scroll, so `HdScene` takes the panel
    colour from the HUD lines plus font pixels anywhere, leaving out one-colour rows, and `HudPanel` styles font pixels
    outside the status-bar rows too (gloss by screen height). `Gpu.MenuRows`: on a menu screen (font tiles on >= 150
    lines) one-colour rows around the windows are black. `EdgeBand` anchors on the status bar's fixed-scroll first line
    (`SplitGap`) instead of `HudTop` (a dialog box at the top made it small).
    **Volume (T40):** Audio › Volume 100 / 75 / 50 / 25 % (ids 420-423, ini `volume=`): `AudioOut.Volume` scales both
    the original and the CD / remastered paths.
    **Story text + meter check (T41):** the panel colour is found from font tiles even without status-bar lines, and a
    black panel counts (story text, cut scenes): its text gets the glyph look and a faint glow, no gloss / meters; the
    plain HUD mode keeps black panels exact. `Gpu.Meters` now needs a LIFE frame of at most 17 tiles with white inside
    (ticks / end cap) and a FORCE row of at most 17 tiles (an all-black death screen drew a capsule). `hd_shots.py reset`
    = from power-on (title, intro).
    **Screenshot (T42):** F12 = `DumpWindow` to `Pictures\re-c\re-c_YYYYMMDD_HHMMSS.png` (notice cleared first).
    **Status-bar shadow, indoor greens (T43):** with HUD style the map darkens to black from the first line of the last
    tile row above the status bar (`MeterF.w` = status-bar line × 256 + that row's start, from `Gpu.Meters` / `SplitGap`),
    fading in over 4 lines (`BarShadow`, also on font-tile pixels there): the row the game has not loaded yet under the
    split (group81: junk tiles at lines 184-190 with a continuous scroll, so `SplitGap` cannot tell it) stays hidden.
    Foliage colours indoors (`Scn.x` <= 0.3) are moss / carpet (kind 3): no lawn, blades or sway.
    **Help (T44):** F1 toggles a key-list panel on the on-screen display (`Osd.HelpLines`; pauses; `--osd help`).
    **CRT look (T45):** Video › CRT look (id 290, ini `crt=1`, Original graphics): `Crt.Apply` (Upscale.cs) makes each NES
    pixel 3x3 with a darker third row (scanline) and an RGB aperture mask, brightness made up for; then the usual
    stretch (and on-screen display).
    **Controller extras (T46):** `Pad.Raw` (XInput buttons of the last read); polled every loop pass (also paused):
    RB held = fast forward (with Space), LB = pause on / off.
    **Audio engine v2 (T47, AudioFx.cs):** the last stage before waveOut, both paths, is a `Resampler` (4-point
    Catmull-Rom; ratio 1 at an integer position = the input sample for sample) driven by `RateControl`: ratio = speed ×
    (1 + 0.005 × clamp((queued − target) / target)), queued = pending + busy device buffers (EMA 0.05), target 2.5
    buffers. It replaces dropping the oldest samples (clicks) when the game runs ahead; the drop stays only as a 0.25 s
    safety cap. Fast forward sets speed 4 (`AudioOut.Speed`, box average): continuous pitched-up sound. CD / Remastered:
    `CdMixer.Mute` (Audio › Channels, ids 430-434, ini `audio_mute=pulse1,pulse2,triangle,noise,dmc`, `--menu
    channel:NAME`) zeroes channel levels before the mixer curves; `Crossfeed` (Audio › Headphone crossfeed, id 415, ini
    `crossfeed=1`) feeds each side the other's lows (one-pole 700 Hz, 0.3 ms, −12 dB), centred bass unchanged. The
    APU's mono stream is untouched (`apu_channels`). Test `win_audio_engine`.
    **Sharpening + caustics (T48):** `PSFinal` runs a contrast-adaptive sharpen (AMD CAS style cross, weight from the
    local min / max, `Cam.w` = 0.6) after FXAA, HUD (alpha 0) exact; HD › Sharpen (id 289, `hd_effects` `sharpen`,
    on by default). Deep water (`kind` 2, colour < `$20`, material detail on) gets caustics: two drifting ridge noises
    multiplied, `pow 8`, cyan-white, weaker indoors.
    **FPS counter (T49):** F2 / Video › Show FPS (id 291, ini `show_fps=1`, `--menu fps`): `FrameStats` (frames in the
    last second, mean `Blit` time incl. HD render + present) as a line on the on-screen display (top left, under a
    notice). Test `win_frame_stats`.
    **Orchestrated audio (T50, Orchestra.cs):** Audio › Orchestrated (id 416, ini `audio=orchestra`, `--menu
    audio:orchestra`, `--wav ID OUT --orchestra`): `CdMixer.Orchestra` + `CdMixer.Chip` (= `() => machine.Apu`; the APU
    channel classes are public, read only, no core change). Every 4th `OutChannels` call (= 44.1 kHz) renders one sample
    from the channel state: pulse 1 → violins (3 PolyBLEP saws ±7 cents, TPT SVF low-pass following pitch, 25 ms / 180 ms,
    delayed 5.6 Hz vibrato), pulse 2 → horns (saw + square, darker), triangle → string basses (saw + sub sine), noise →
    timpani (period index ≥ 8: decaying sine 58-120 Hz + low rumble) / snare-cymbal (band-passed noise, envelope from the
    game), DMC as is. Note starts = `Env.Start` rising, the channel turning on, or a pitch jump > 4 %. A pulse with its
    sweep on or a period jumping > 4.5 % several times within ~50 ms (sound effects, arpeggios) plays as a band-limited
    square at its duty. Seating (violins left, horns right), hall reverb (Freeverb size 1.45, feedback 0.86, 20 ms
    pre-delay), shelves +2 dB @ 150 Hz / −1.5 dB @ 6 kHz; per-instrument gains measured against each chip channel solo.
    Test `win_orchestra` (A4 at 440 Hz, triangle pitch, music `$14`: +2.6 dB vs CD, melody semitone match 0.78).
    The test runner's `--cdwav ID OUT SECONDS [cd|remaster|orchestra] [MUTE]` writes WAVs on Linux (`CDWAV_TRACE=1`:
    pulse 1 state per frame); `tools/wav_env.py` prints loudness per window.
    **Cinematic cut-scenes (T51):** `Gpu.CineBand` finds a cut-scene picture: no HUD split, `Play` < 0.5, the longest run
    of lines with ≥ 8 lit non-font pixels (black gaps without text bridged up to 64 lines, text ends it), ≥ 24 lines,
    ≥ 64 px wide. `CineUpdate`: a new band = a new shot (start time), weight eases in over 30 frames, held 20 frames
    through fades, 0 at once with a HUD. Constant row 19 `Cine` = (weight, y0·256+y1, x0·256+x1, shot start). In
    `PSLight` pixels inside the band are remapped by `CineCam` before everything else: a push-in to zoom 1.10 over 10 s
    toward a per-shot side, plus parallax from the height field (higher = moves more); `CineFx` adds embers (warm mean
    colour) or dust and slow light rays in screen space. `PSComp`: an anamorphic streak (tight bloom stretched sideways,
    blue), teal-shadow / warm-highlight grade, deeper vignette, 24 fps grain. Text / HUD (alpha 0) untouched. HD ›
    Cinematic cut-scenes (id 292, `hd_effects` `cinematic`, on). Test `win_cinematic` (intro band, no text inside,
    gameplay / menus / shops never).
    **Smooth outlines (T52):** HD › Smooth outlines (id 293, `hd_effects` `outlines`, on; `MeanC.w`): the 2x → 4x
    (`PSScale` when the source keys are ≥ 512 wide) and 4x → 8x (`PSAlbedo`) steps use `Vote` instead of Scale2x: per
    output pixel a Gaussian (σ 0.8 texel) weight over the 4×4 source key texels, the background key (colour + dither
    partner + HUD bit) and the sprite key each take the key with the most weight; HUD / text keep Scale2x and the HUD
    edge never moves. The 1x → 2x step stays Scale2x (pixel-art aware). Stair steps become curves at any window size;
    painted pack tiles are not affected. Previews: out/hd52/cmp29.png (off | on).
    **Ambient bars (T53):** HD › Ambient bars (id 294, `hd_effects` `ambient`, on): when the picture does not fill the
    client area, `PSAmbient` draws the whole back buffer first (the picture pass then covers its rectangle): the lit HDR
    picture mirrored at the nearest edge, 5×5 taps 3 % apart, × 0.42, fading `exp(−4.5 · distance)` (client uv).
    `Blur` = picture origin / size, `Out` = client size for that pass only. `win_video_check` `hdfull` turns it off
    (black-bar layout check); `hdamb` = group29 fullscreen: bars lit, dimmer than the picture, darker at the screen edge.
    **Ambient sounds (T54, Ambience.cs):** Audio › Ambient sounds (id 417, ini `ambience=1`, default on): `Window.
    AmbienceTargets` per frame from `HdScene` (HD engine only, else silent): Outdoor, Hot, Icy, and Cave = Play × not
    outdoor / hot / icy × darkness (mean playfield luminance < 0.32). Beds fade over ~1.5 s: wind (band-passed noise
    320-700 Hz, gusts), birds (3-6 sine chirps 2.8-4.4 kHz every 3-10 s), cave rumble (two one-poles) + drips (rising
    sine, 65 ms) through a two-tap echo (210 / 300 ms), fire crackles (Poisson clicks, high-passed) + roar, ice whistle
    (Q 14 bands). `AudioOut.Flush` adds `Amb.Next()` per output frame after the resampler (any mode), × volume.
    Levels −35 .. −40 dBFS. Test `win_ambience`; test runner `--ambwav outdoor|cave|hot|icy OUT [SECONDS]`.
    **Tilt-shift (T55):** HD › Tilt-shift depth of field (id 295, `hd_effects` `tiltshift`, on; `Mat.w`): in `PSComp`,
    gameplay only (`Scn.z` > 0.5, no cut-scene), the playfield (line 0 to the HUD's first line) keeps a sharp band
    centred at 55 % ± 22 %; beyond it a 12-tap golden-angle disc blur grows to 1.3 NES pixels over the next 25 %
    (HUD samples excluded, HUD pixels untouched) — the "HD-2D" diorama look.
    **Frosted glass (T56):** while the on-screen display shows (pause, help, notices, FPS), `Draw` also makes the blurred
    1/8 picture (the T53 passes) and binds it to `PSFinal` as t2: under overlay alpha a (×1.6, saturated) the picture
    blends 90 % toward the blur before the overlay is composited — the game behind the pause / help panels is frosted.
    **Damage feedback (T57, Feedback.cs):** Game › Damage feedback (id 105, ini `feedback=1`, on): after each frame
    `DamageFeedback.Update(player_hp $03C1, player_hp_max $03C0, SubstPlay ($40 = 1, $41 = 8), frame)`: a drop = a hit
    (refills never count; the first frame after leaving play is skipped). HD: constant row 20 `Dmg` = (hit flash 1 → 0
    over 27 frames, low life = hp ≤ max / 4 and > 0): `PSComp` mixes the screen edges toward dark red (flash × 0.65,
    low life a heartbeat pulse 0.15-0.40 at 1.2 Hz); HUD untouched. Controller: `Pad.Rumble` (XInputSetState, bound
    with XInputGetState) 160-400 ms, stronger for bigger hits; stopped by a later `Read`. Not when keys are scripted.
    Test `win_feedback`: group09_check's keys give exactly its 7 contact hits (HitPlayerDamage 26:9410).
    **Heartbeat (T58):** with Damage feedback on and life low (`DamageFeedback.Low`, not paused), `Ambience.Heartbeat`:
    lub (55 Hz sine) + dub (48 Hz, 0.7) 0.28 s apart every 0.85 s, ~90 ms decay, linear 0.3 s fade in / out, about
    −28 dBFS; mixed like the ambient sounds (any audio mode, any graphics mode). Test in `win_ambience`.
    **Modern font (T59, Glyphs.cs):** HD › Modern font (id 296, `hd_effects` `font`, on). The text font's tiles are
    ASCII (`$F00` + code in the tags' global numbering, banks `$3C/$3D`). Only letters, digits and `! " & ' , - . : ; ?` are
    the ASCII symbols; `_` is the game's ellipsis (drawn as …); the other symbol codes hold custom pieces (arrows,
    icons, word fragments, the `$40` page prompt) and keep their pixels (`FontInk.Code`). At GPU start
    `GlyphAtlas.Render` draws those with GDI (Segoe UI semibold, antialiased; Wine substitutes) into 16 × 8 cells of
    64 px (= 8 NES px at the 8x albedo): the em is scaled so 'H' has the game font's cap height (rows 0-6 from
    `FontInk.InkRows`) and placed on its top row; glyphs wider than the cell are squeezed. `Gpu.Layers` (with
    `fontChr`) writes a glyph word into the art map for UI pixels of such tiles: bit 31, code, column / row in the tile,
    ink / paper colours (`FontInk.Values`: the tile's two most frequent pixel values through the line's palette).
    `PSAlbedo` draws those pixels from the atlas (t12 `Gly`, 13 SRVs) as paper → ink by coverage; the HUD styling
    (bevel, gloss) then applies as before. The status bar's own tile set ($E00 range) is unchanged. Test `win_font`
    (talk16: 3840 glyph pixels, every one's ink / paper = the colour on screen).
    **Display colours (T60, ColorAdjust.cs):** Video › Display: brightness 1-5 (ids 640-644, ini `brightness=0..4`,
    gains 0.80-1.25), Natural / Vivid (645 / 646, ini `vivid=1`, saturation × 1.25 around Rec.601 luma), colour-blind
    assist Off / Protanopia / Deuteranopia / Tritanopia (647-650, ini `colorblind=off|protan|deutan|tritan`): one 3×3
    matrix = gain × daltonize × saturation. Daltonize = (I + Shift (I − Sim)) with Sim = Machado 2009 (severity 1);
    tritan uses the classic shift rows, red-green ones rows searched on the 64 NES colours (pairs > 0.25 apart that the
    simulated deficiency sees < 0.1 apart: protan 18 → 5, deutan 20 → 4, tritan 14 → 4). Applied to the NES palette
    lookup (`Window._lut`: Original, CRT, upscale filters), to the 16/32-bit enhanced picture per frame
    (`ColorAdjust.ApplyAll` into `_hdCol`), and in HD by `PSFinal` (constant rows 21-23 `Col0-2`, before the OSD).
    Defaults are the identity (the pictures stay exact; `win_video_check` unchanged). Test `win_color`.
    **Remappable keys (T61):** Game › Remap keys... (id 106, `--menu remapkeys`) asks on the on-screen display for one
    key per button in `Input.Order` (A, B, Select, Start, Up, Down, Left, Right): Backspace keeps the current one, Esc
    cancels; play pauses meanwhile (not in a `--frames` run). The keys are extra (the defaults keep working, so `--keys`
    scripts are unaffected); a bound key wins over a default meaning of the same key and over the window hotkeys
    (`KeyCommand` is skipped for it). Game › Reset keys to the defaults (107). ini `keybinds=a:20,up:49` (hex VK).
    Test `win_controls`.
    **HD quality Auto (T62):** HD quality: Auto (id 297, ini `hd_quality=auto`, now the default): `AutoQuality` (in
    Feedback.cs) after each `Blit`, from `FrameStats.Fps`: < 50 fps for 3 s → one step down (full → balanced → fast),
    ≥ 58 for 20 s → one step up to the ceiling; a step up undone within 10 s lowers the ceiling. Off while paused and in
    `--turbo` runs (those stay at full: tests unchanged). A notice names the new level.
    **Save backups (T63):** at start (the default save only), `Save.Backup` keeps `re-c.sav.1` (newest) .. `.3`,
    rotated only when the save differs from `.1`.
    **Playtest fixes (P1):** delayed / choppy sound, worst when entering an area. Causes found: (1) the exe JIT-compiled
    each game routine on its first call (a new area runs new routines: 10-40 ms stalls measured on Linux) → the publish
    is now ReadyToRun (`PublishReadyToRun`, exe 12.6 → 15.1 MB); (2) samples reached the device only after the frame
    was drawn and presented → `AudioOut.Flush` now runs before `Blit`; (3) a catch-up burst after a stall left up to
    250 ms queued, drained at the rate loop's 0.5 % → above ~128 ms queued (`MaxQueue`) the excess is dropped at once;
    (4) the device buffers are 8 × 512 samples (11.6 ms each, refilled more often) with a 70 ms target; (5)
    `GCLatencyMode.SustainedLowLatency` while running.
    **Widescreen playfield (W1-W2, Wide.cs):** Video › Widescreen playfield Off / 16:10 / 16:9 / 21:9 (ids 660-663, ini
    `widescreen=`, `--menu widescreen:16:9`): 384 / 424 / 560 columns, square pixels; Original (+ CRT / upscale filters)
    and HD (the 16/32-bit looks stay 256). The columns beside the game's 256 come from the loaded area: `AreaCache`
    (= `Bg.RenderArea` with the visited bitmap, packed pixel value / palette group / global CHR tile, built on a worker
    thread from a RAM snapshot taken before a frame when the area key changes; `win_wide`: identical to RenderArea);
    `Widescreen` takes the camera before each frame (`$02/$03`, `s8($05)·240+$04`: 98-100 % of the playfield equals
    the area there), tests a sample of playfield pixels against the area each drawn frame (≥ 85 % = the screen shows
    the map; the sides fade in over 8 frames and out over 4, so menus, cut-scenes and transitions keep black sides),
    and colours the sides with the current palette (fades follow). `Wide.MapLines`: only lines above the status bar
    that the game does not leave black; the game's masked left column is filled from the area too. Sprites exist only
    in the middle. Original: `Window.Compose` builds the wide `Px`; the overlay stays on the middle 256 columns
    (`Osd.Blend` x offset + stride; the pause dim covers the sides). HD: `SceneRaster.Widen` makes a W-wide raster
    (middle = the game's raster, sides = area pixels with tile tags, so pack art, materials and the font path apply);
    `Gpu.Upload(wide, scene, r256)` sizes its inputs and targets from the width (`NesW`, `SetWidth` rebuilds them;
    shaders read `Scr` = width, 240, side width; `#define NES`); line analyses (status bar, edges, menus, meters,
    cut-scene band) run on the 256 raster, x positions shifted; `WidenInfo` copies the scene info and gives the side
    pixels the void rule (near-black, opened 3×3) above the status bar so chasms continue. Layout uses
    `VideoSettings.PicW` (the window widens). Test `win_wide`.
    **Playtest round 2 (P2):** (1) **crash** with widescreen on through name entry: `AreaCache.Build` read the
    uninitialised map grid at new-game init (sub_state 0) and threw on the worker thread; the exception surfaced on the
    game thread through `Task.Result`. Now builds are bounds-checked (`BuildUnsafe` + catch → null), a failed build is
    ignored, and none starts in sub_states 0 / 1 (area load) / `$13` (wake-up scene). Test: `win_wide` runs the canon
    keys from power-on with every widescreen step. (2) **drops / stale sides / artifacts**: the old string key held all
    of `$67`, whose bits 0-1 count every 4 frames, so the whole area was rebuilt every 4 frames (and could be rebuilt
    from a half-loaded area); the key is now a hash with `$67 & $FC`, split into an area key (another area: the cache is
    dropped at once) and a visited-screens key (rebuilt in the background, the old cache stays). The match test has
    hysteresis: on at 80 %, off only after 8 frames under 60 %. Pixels outside the area are plain black, never void
    (no chasm effect beyond the area's edge). (3) **sprites in the sides**: `SideSprites` replays `ObjSpriteDraw` /
    `ObjDrawMeta` (off-screen and vertical tests, overlay, mirroring, frames, tile offset, priority, sinking) from a RAM
    snapshot taken with the camera before the frame and draws only the pixels outside the game's 256 columns
    (Original: over `Px`, behind-background rule from the area; HD: `SceneRaster.ApplySideSprites` into the wide
    raster's sprite layer). On the game's own columns the replay reproduces 96-98.5 % of the PPU's sprite pixels (the
    rest = orbit sprites, another routine), 0 extra. (4) **orchestra pops**: a commented-out line had disabled the
    sound-effect detector's period-jump count; a per-sample array allocation (44 100 / s) in the effect square; a
    hard level reset when switching instrument ↔ chip sound (now two levels crossfading over ~1-2 ms); an instant ×0.7
    dip on each new note (removed); timpani strokes at full level at any phase (now from phase 0 with a 1 ms attack);
    the snare's near-instant attack up to 9 kHz (now 1 ms, ≤ 6 kHz, low-passed); DMC samples unfiltered (low-passed
    ~8 kHz). Clicks in 60 s of music (melodic instruments): 543 → 4. The remaster path's per-sample array is gone too.
    (5) **cut-outs**: P1's hard drop above ~128 ms queued cut audio on every catch-up burst; now the rate loop drains a
    backlog at up to +2 % (±0.5 % near the target) and only > 300 ms is dropped. (6) **display lag**: the swap chain let
    the CPU queue 3 frames ahead with VSync; `IDXGIDevice1::SetMaximumFrameLatency(1)` now. `tools/win_run.py` runs the
    exe under Wine with any arguments and a long timeout.
    **HD seam (P3):** the 1-2 px step at the edges of the game's 256 columns came from the scene info, not the map: the
    game's masked left column (filled from the area by `Widen`) kept HdScene's info for the black strip (backdrop colour,
    void on 163 of 190 lines), and the void mask changed at the old edge (HdScene's rule also counts the dark backdrop,
    the side rule did not; its 3×3 opening was clamped at x 0 / 255). `WidenInfo(…, rl, sc)` now recomputes one void
    mask over the whole width with HdScene's rule (`HdScene.DarkBackdrop`, sprites and text lines excluded, the sides
    only above the status bar and inside the area) and gives pixels the widening changed the side info. `win_wide`:
    colour off 0 at the seam columns, void lines 100 / 99 across the left edge and 100 / 99 across the right (were 49 /
    163 and 101 / 48). Preview out/wide5/seam_cmp.png (before | after).
    **Playtest round 3 (P4):** (1) **per-line widescreen**: 11 of 40 test states change the scroll mid-playfield (from
    line 10 to 177), so one camera did not fit every line (glitches; the old whole-screen match then blacked the sides
    out). `Widescreen.AfterFrame` now checks each playfield line (every 3rd pixel, sprite pixels skipped) against the
    area at the camera and, in HD, at the line's own scroll relative to the first line (`LineScroll`); a mapping with
    ≥ 88 % equal pixels marks the line as map with that offset (`LineDx/LineDy`, used by `SideColor`, `SideOpaque`,
    `SceneRaster.Widen`). A line clearly wrong (< 50 %) turns off at once, a borderline one after 3 frames; `Show` fades
    with the share of map lines (in ≥ 60 %, out after 8 frames < 30 %). `win_wide`: on 5 split states every line marked
    as map matches the game 100 %. Every 3rd pixel, because a 4-pixel pattern let a 2-pixel misalignment pass.
    (2) **GC stalls**: `WidenInfo` allocated ~200 KB per frame (large-object heap → blocking full GCs → audio
    dropouts); now reused buffers, separable 3×3 passes, block copies: 2.7 → 1.6 ms, 210 → 11 KB per frame (test:
    < 16 KB). (3) **audio**: a backlog above ~116 ms is spliced out with a 5.8 ms crossfade (`AudioOut.Splice`, tested on
    a sine) instead of draining at +2 % (late for seconds) or a plain cut (a click); the rate loop is ±0.5 % only;
    after an underrun 20 ms of silence (was 50) and a 5.8 ms fade-in. (4) **tearing** in the GDI modes: with VSync on,
    `DwmFlush` after each picture (the compositor's next frame); the menu item is now "VSync (all graphics modes)".
    (5) **mouse control** (Game › Mouse control, id 108, ini `mouse=1`, on): left button held = walk toward the
    cursor (`MouseControl.Dir` from the player, object slot 1, at the screen position the sprite pass stored in
    `$05C1/$05E1`; 6 px dead zone; diagonals when neither axis dominates by 2.5×), right = B (sword) in play (else
    the context menu), middle = A; `MouseControl.ToNes` maps client points through the layout, widescreen sides
    included; mouse capture while held, released with focus. `win_mouse`: the player walks 60 px up, then right.
    **Playtest round 4 (P5):** reported: crackle / pops at scene changes (Orchestrated + stereo + ambient sounds), large
    black areas at the sides in HD widescreen. (1) `AreaCache.Build` allocated a new array of the whole area (up to
    ~30 MB) on every area change and every newly visited screen, on the large-object heap = blocking full GCs that pause
    every thread (the sound included) right at scene changes. Now two buffers alternate (`Widescreen._spare`,
    `Build(…, reuse)`): `win_wide` measures what widescreen adds over the game's own allocations across a further area
    change (~0.4 KB / frame). (2) A direct `$4011` load moves the DMC level by more than sample playback can in one
    output sample (a step = a pop, on the console too); in the orchestra such jumps (> 0.03) ramp over ~4 ms
    (`win_orchestra`: largest step 0.002 instead of ~0.37). (3) Black sides: a screen shake moves every line off the
    camera, so neither earlier candidate matched; a third per-line candidate is the line's scroll against the map
    position within half a screen (`win_wide`: a 3 px shake keeps 99 / 120 lines). Areas where the screen and the
    camera disagree get a correction searched over screen-sized steps of the area that agree with the PPU scroll
    (`Widescreen.Search`, kept while it matches). (4) Side pixels with no map (outside the area, unverified lines) are
    flagged `HdScene.FNoMap` (`SceneRaster.NoMap` → `WidenInfo`) and `PSFinal` fills them with the blurred lit picture
    mirrored back across the game's column edge, dimmer outward (the blurred copy is made whenever there are sides),
    instead of black. Preview out/wide6/0_hd.png (a one-screen corridor at 16:9).
    **Screenshot menu (P6):** Game › Take screenshot (id 109, `--menu screenshot`, = F12) and Game › Open screenshots
    folder (id 110, `ShellExecuteW` "open" on `Pictures\re-c`). `Window.ShotPath` adds `_2`, `_3` .. so shots in the same
    second do not overwrite each other. Test `win_screenshot`. (Under Wine "My Pictures" is the Linux ~/Pictures.)
    **Playtest recordings (P7):** the user's two screen recordings (`issues/*.mp4`, Win 11 / GTX 1080, 57-60 FPS at
    5-12 ms per frame) were decoded (GStreamer / ffmpeg) into `out/issues/` and measured with new tools:
    `tools/wav_events.py` (clipping, clicks = second-difference spikes, dropouts, with times), `tools/wav_dc.py` (mean /
    AC per window), `tools/wav_dump.py` (samples around a time). **The crackle was a DC offset**: seconds-long constant
    levels of −23 000 to −32 700 (pinned at the limiter), jumping = pops. Reproduced offline with the test runner's new
    `--playwav STATE OUT SECONDS [mode] [KEYS]` (the game from a state through the mixer + ambience; `PLAY_MUTE` /
    `PLAY_AMB` to isolate): every melodic voice produced it. `Orchestra.Saw` / `Square` wrapped the phase with a single
    `-= 1`, so a step > 1 (a frequency above the output rate: a triangle period near 0 ≈ 56 kHz, effect sweeps) left
    the phase growing forever = a huge constant on every later note. Now: step clamped to 0.45, `ph -= floor(ph)`, NaN
    reset; voices track 20 Hz-12 kHz and keep their pitch while silent; a 15 Hz DC blocker + NaN guard on the
    orchestra's output. `win_orchestra`: the 20 s group09 fight that pinned at −32 700 now has a largest 1 s mean of 4
    and no clipped samples. The fire ambience's crackle was raw high-passed steps (408 "clicks" in 30 s of a lava cave):
    now soft band-passed pops (1 click left, as with no ambience). **Widescreen**: the recordings showed sides on the
    title / name-entry screens (black pixels matched any dark map) and wrong strips on plain lines; per-line checks now
    ignore black-on-black samples (24 real samples needed), accept the split / shake offsets only at 93 % on lines with
    detail (4+ colour changes), lines without evidence turn off after 3 frames, and the sides only exist in gameplay
    (`Widescreen.Active`: main_state 1, not sub_state 0 / 1 / `$13`).
    **Playtest 6:** the title screen "zoomed in": the T51 cut-scene dolly found a picture band on the logo. `Gpu.CineAllowed`
    (set per frame from `Window.CineAllowed(main_state, title_mode)`) keeps the film look for the opening and story pages
    (main_state 3, `title_mode` 0-2) and the in-game cut-scenes, and turns it off on the title menu (5 / 6), the status and
    name-entry screens (3 / 4), where the parallax also split the letters into slices (`win_cine_gate`). The name-entry
    letters B, D, Q, 0 showed as white boxes: `FontInk.Values` took the most frequent pixel value as paper, and these
    heavy glyphs have more ink than background; the border ring now counts 4x (`win_font`: every glyph's paper = 'A''s).
    **Playtest 6 recordings 3-5 (widescreen):** in dark caves whole bands of side lines went black (the P7 rule skipped
    black-on-black samples, so mostly black lines had "no evidence" and turned off), and neighbouring lines could pick
    different offsets (camera / split / shake), tearing the sides. `Widescreen.AfterFrame` now decides in two passes:
    the camera wins whenever it matches (black counts for it); a split / shake offset only when the camera fails and
    at least 8 lines of the frame agree on that one offset (`_lineState`, `_altDx/_altDy`); lone offsets count as no
    evidence. group08 (cave): 7250 / 7300 lines verified (was 5500); `win_wide` asserts no frame mixes offsets.
    **Spoken dialogue (playtest 6, `Dialogue.cs`, Audio > Spoken dialogue, ini `voice=1`, on by default):**
    `DialogueReader` reads the page the text printer built in WRAM `$6000` (TextPrint 20:853A / TextClearBuf 20:8720:
    8 rows × 32 tiles, `FE 1E` + 29 characters + `1F`; anything else = not a page) while a message window is open in
    gameplay (main_state 1, `irq_mode` 1 / 9, ShowMessage 30:D354). A page counts after 4 unchanged frames and is said
    once; the next page cuts it off, closing the window stops it. Lines are joined (a trailing `-` joins the word),
    `_` = ellipsis, icon / arrow tiles are skipped. `Speech` = SAPI 5 `SpVoice` over raw COM (`CoCreateInstance`,
    ISpVoice vtable 20 Speak async + purge, 28 SetRate, 30 SetVolume), created on the first page; none on a PC without
    a speech engine. Not used in scripted (`--keys`) runs, fast forward or mute. `win_dialogue`: group18's walk-in talk
    says Akahana's 2 pages before / after the first A and stops on close (f2769), then the next NPC.
    **Voices per NPC (playtest 6):** `Cast` classifies the 125 talking NPCs (spawn handler 1, type `$30`, NPC id p =
    `$0560`+slot = the NpcScript index) as man / woman / old man / old woman / boy / girl / creature, by eye from
    `tools/npc_sheet.py` contact sheets (both facings, own area CHR + palettes); `%APPDATA%\re-c\voices.ini`
    (`npc_16=oldman`) overrides. The speaker is the talk slot `$0623` while sub_state = `$11` (NpcTalk), else the
    narrator. **Neural voice** (`NeuralVoice`): Piper 2023.11.14-2 (MIT; espeak-ng GPL-3, ONNX Runtime) as a hidden child
    process in JSON mode with the multi-speaker `en_US-libritts_r-medium` model (904 LibriTTS-R readers, CC BY 4.0) in
    `dist\voice\` (`tools/voice_pack.py` from the downloads; `voice/speakers.txt` = reader sex from LibriSpeech
    SPEAKERS.TXT via `tools/voice_speakers.py`: 474 male, 430 female). Each NPC: a reader of its sex (hash of p), age by
    replay speed (elders 0.88, boys 1.20, girls 1.28, creatures 1.45 / 0.72, ±3 % per NPC); the WAV is resampled to
    44.1 kHz and mixed after the resampler with the game ducked to 0.5. Without `dist\voice\` the SAPI voice is used
    with the same cast (`Cast.Xml`: `<voice required="Gender=..">` + pitch / rate by age). Piper itself could not be run
    in this sandbox (blocked), so the neural path is untested end to end; `win_dialogue` covers the cast, speaker
    picks, WAV replay and JSON.
    **No sprite flicker, the game's own (playtest 6):** besides the 8-per-line limit (SpriteLimit off), the sprite pass
    ObjSpriteDraw 28:800E drops objects itself: the `$07FF` rotation picks a different start slot each frame, so with
    the 64-entry list `$0200` full other objects drop out each frame, and `$0380` b2 objects (the player's 30-frame
    invulnerability after a hit) are drawn every other frame. With HD "No sprite flicker" on, `Window.FillFlicker` (HD
    path, right after `SceneRaster.Build`, gameplay only: main_state 1, `irq_mode` 0, so message windows / menus keep
    their hidden sprites) replays the pass from the RAM snapshot taken before the frame (= the objects and the list
    `$0200` the NMI copied for this frame): `SideSprites.Draw(missingOnly)` keeps the pass's exact on-screen tests
    (active, X hi / Y hi, sy range) and drops only the two flicker skips; an object with any entry in `$0200` was drawn
    by the game and is skipped, the rest are drawn whole where the game has no sprite pixel
    (`SceneRaster.ApplyMissing`). `win_noflicker`: group09's fight has 188 blink frames, the game skips 90, the fill adds
    exactly the player on those 90 and nothing else.
    **Widescreen HUD overlay (playtest 6, "the HUD should just overlay it"):** while the sides show in gameplay,
    `SceneRaster.Widen(hudFrom = SplitGap.From, panelFrom = SplitGap.To, panelTo = Gpu.EdgeBand)` continues the map below
    the playfield over the whole width (offset of the last verified line); in the game's columns only the status panel
    stays (per line its first .. last non-black pixel, the masked left column ignored; the seam rows and the stray /
    backdrop rows under the panel are all map). Those pixels are `Under` = world: `WidenInfo` gives them side info,
    `Layers` keeps them out of clean edges (the main loop now also covers exempted pixels: before, a pixel skipped by
    the clean-edge loop was never written = black), `PSLight` treats a UI line's pixels without the UI flag as world in
    widescreen (`Scr.z > 0`), `HudPanel` leaves them alone, and `Gpu.HudOverlay` drops the bar's drop shadow (`m[7]`),
    which painted the last tile row above the bar black. **Scroll edge:** the game's outer 8 columns show stale tiles
    while it scrolls (the column it is still rewriting, hidden by a TV): on verified lines `Widen` takes the map's pixel
    there when it differs (no sprite), which removed the seam at the edge of the game's columns; the sides of the top 8
    lines (overscan, never verified) take line 8's mapping. `win_wide`: rows 191-239 behind the HUD, panel unchanged
    pixel for pixel, 126 stale edge pixels replaced, the playfield's inner columns untouched.
    **Voice start (playtest 6):** piper.exe needs the VC++ 2015-2022 runtime; `tools/voice_pack.py --vcrt` copies
    Microsoft's MSVCP140 / VCRUNTIME140 / VCRUNTIME140_1 (PyPI `msvc-runtime` 14.51) next to it. Piper starts with the game
    (`Window.StartVoice`; not in `--frames` / `--keys` runs), `%APPDATA%\re-c\voice.log` records the start, Piper's
    stderr, the first page and why it fell back; a Piper that exits switches to the Windows voice with a notice.
    **No-flicker fill only in SubstPlay (playtest 6 bug):** coming out of the first chamber, a ghost of the player (and its
    HD shadow) showed before he climbed out: the wake-up cut-scene (sub_state `$13`, WakeCutscene 20:9400) keeps the
    player object but does not draw it, and the fill drew it from object RAM. `FillFlicker` now runs only in sub_state 8
    (SubstPlay); cut-scenes, area loads, talks and menus show exactly what the game draws. `win_noflicker_wake`
    (power-on, new game without a held button, f2300-3399): 505 cut-scene frames, objects added in 504 before, 0 now.
    **Playtest 7 (recordings 212411 / 212539):** the recordings came from a build before the HUD overlay and the wake-up
    ghost fix (the window title now carries the build time, `AssemblyMetadata BuildTime` → `Window.Title`). Fixed on top:
    side rows dropping out alone (animated tiles, plain or sprite-covered rows) = dim smeared "line drags" and side NPCs
    losing those rows: `Widescreen.FrameGood` (70 %+ of the lines match at the camera) puts every playfield line on the
    camera mapping unless its own camera match is clearly wrong (24+ samples, < 60 %), and `MapLines` then keeps all-black
    rows too (`win_wide_holes`: the village walk, 0 holes in 300 frames). NPCs clipping at the edge of the game's columns:
    the game only draws objects whose X is in its 256 columns, so an NPC just outside lost the part reaching in;
    `SideSprites.Draw` draws those objects' pixels inside the columns too (`offGame`), under the game's own sprites
    (`ApplySideSprites` skips game sprite pixels). Checked in an HD render of f2760 (the pod's white flash): no silhouette.
    **Remade story art (playtest 7, "modernize the intro / cut scenes"):** no hand-painted assets can be made here, so the
    story pictures are repainted live on the GPU at the output resolution: `PSFinal` replaces the picture band (`Scr.w` =
    first line * 256 + last line + 1, from `Gpu.StoryBand`: the lit lines above the story screen's raster split; the T51
    band detection skips split screens) over the whole width with scene `Dmg.z`, faded with the game (`Dmg.w`: the
    original picture's brightness against its brightest frame; full light from 75 %). Scene 1 (`Window.StoryScene`:
    main_state 3, title_mode 2) = the intro's prehistoric jungle, which the game draws as a narrow column: `StoryDino`
    (dusk sky, sun + god rays, drifting clouds, a smoking volcano, palm line, two long-necked giants with bobbing necks
    and rim light, fliers, ground mist, chevron-leaved ferns framing both sides, sunlit motes; SDF shapes, branch-free
    loops for Wine). `win_story`: the band is found in 107 of 118 story frames. The other story pictures and cut-scenes
    are next (same mechanism, one scene function each).
    **Playtest 8 (recordings 0720 / 0729 / 0730, voice.log):** Piper ran but sounded generic; title and story text
    problems; streaks; characters with a dark silhouette. Fixed: (1) the intro story is a sequence of pages (the jungle,
    a text page, a village under a purple sky): `Gpu.StoryBand(r, sc)` counts only non-text pixels, `JunglePicture`
    (browns / greens, almost no blue / purple) picks the jungle, the band is fixed (lines 0 .. the split), and `PSFinal`
    keeps text pixels on top (`win_story`: the text page never gets a band, the village is never taken for the jungle).
    (2) Title-screen streaks from "CONTINUE" and the logo = the letters' cast shadows on the flat backdrop: AO, sun and
    contact shadows now scale with the gameplay weight (`_play`). (3) The silhouette beside characters = their sun
    shadow: sprite lift 2.2 → 1.1, contact shadow 0.30 → 0.18. (4) Torn rows over lava = heat shimmer with 2.5 px noise
    cells per row: now 9 px cells, slower, a third of the amplitude. (5) Widescreen: sprites in the PPU-masked left 8
    columns (hidden by the NES, then cleared by the widening) are drawn by the side-sprite replay (`leftMask`).
    New: Video > Debug info (area / entrance / map size, player + camera, main / sub / IRQ / frame, objects + flicker
    fill, widescreen, voice engine + speaker; ini `debug=`), Cheats > Warp to area (areas with an AreaPtrTable record,
    `$6C` / `$6D` = 0 / sub_state 1 from normal play: `win_warp_debug` warps 08 → 02 in 44 frames). Voice: Kokoro-82M
    (Kokoro.cs, `IVoice`) through sherpa-onnx 1.13.8's offline TTS program, one process per page, preferred over Piper
    when `dist\voice\kokoro` exists (`tools/kokoro_pack.py`; model in two < 100 MB parts joined into %LOCALAPPDATA%);
    not run here (downloaded programs are not executed in this sandbox). The pod's black figure could not be reproduced
    with the user's settings (an HD render at f3099 shows the player in colour).
    **Playtest 9 (recordings 1108 / 1109 / 1111, voice.log):** (1) The black figure after the cryo chamber is the original
    game's: in area 00 (normal play) it draws the player with sprite palette 0 all `$0F` until it fades him in
    (`$26 $04` → `$36 $14`, one colour at a time; reproduced in an HD render at frame 2700 with the user's settings).
    `SceneRaster.PodSilhouette` gives the still-black colours `$16` / `$04` (dimmed, "behind the glass") in the HD raster,
    so the game's fade carries on from there (`win_hidden_sprites`: 217 frames recoloured, no black body pixel). (2) The
    title lines along START / CONTINUE were the relief lighting's ridges along the text rows on the flat backdrop: `Bump`
    now scales with the gameplay weight (HD render of the title with the user's settings: clean). (3) Characters: sprite
    rim light 0.7 → 0.2 (the bright halo), lift 1.1 → 0.8, contact shadow 0.18 → 0.12. (4) "Line drags" beside small
    areas: the widescreen no-map fill was a single tap of the wide blur (rows smeared sideways); now a 9-tap 2D average.
    (5) Voice: Kokoro exited 0xFFFFFFFF on the user's PC (the log kept only the echoed command line); now the whole
    output is logged, Kokoro retries once with the basic options, and on failure Piper takes over and says the page
    (Audio > Voice engine: auto / kokoro / piper / windows, ini `voice_engine`). New debug lines: HD scene, sprites,
    input, audio; new cheats from the game's own dead `DebugPauseCheat` 15:BF47: all items (`DebugItemTable` 15:BF7F into
    empty inventory slots), max level (`$0421` = `$0F`, MP 255), fast walk (`$0340` / `$0341` = 9).
    **Kokoro latency (playtest 10: "several seconds to start"):** one sherpa-onnx-offline-tts.exe per page reloaded the
    model each time. Now `ReC.exe --tts-server DIR MODEL` (KokoroServer.cs, started hidden when Kokoro is chosen, while the
    intro plays) loads sherpa-onnx's C API (`sherpa-onnx-c-api.dll` + `onnxruntime.dll`, v1.13.8 shared MT build, packed by
    `tools/kokoro_pack.py --lib`) and the model once, then answers stdin requests (seq, speaker, speed, wav, text) with
    WAVs; requests of an older page are skipped. Pages are split into sentences (`KokoroVoice.Sentences`; pieces under
    12 characters ride with the next), so the first sentence plays while the rest are made, queued back to back. The
    structs follow c-api.h (`win_dialogue` checks the x64 sizes 56 / 64 / 416 / 448). If the server cannot start or dies,
    the per-page program is used again (and the page being made is said again). Debug > Voice shows SERVER / PER PAGE.
    **Widescreen sides fading in and out (playtest 10, debug overlay: WIDE SHOW cycling with LINES 183 GOOD):** the area
    key held two bytes that change inside one area: `$07F0/$07F1` (CHR banks the game swaps every few frames to animate
    water and grass) and `$67` (MetatileExpand 31:EDBA's scratch while scrolling). Each change looked like a new area:
    cache dropped, sides faded to 0 and back. The CHR banks moved to the full key (a background refresh, sides stay);
    the tile set comes from `$6A/$6B` (`AreaCache.Tileset`: $BE00 + ($67 & $3C) * 8 from AreaLoadGfx; every area's $67
    is $80-$AC). `win_wide_steady`: walks in the recorded areas $60 / $14 / $A9 / $02 / $04 by warp: sides below full in
    445 / 376 / 445 of ~500 frames before, 0 / 8 / 0 now.
    **Shop / inn voices (playtest 11):** sub_states $20 armor / $21 tool / $22 inn / $23 pawn (16:9509 ff.) print their
    words with menuPrint straight into nametable $2800, not the $6000 page, so nothing was said. `DialogueReader.ShopPage`
    reads rows 0-12 of $2800 (VramRead), drops all-caps words (title, YES / NO) and keeps only whole sentences (item names
    and "It will cost you $.." are skipped). The keeper slot is not kept ($0623 = 0), so each shop type has a fixed
    keeper voice (`Cast.Speaker` = 0x100 | sub_state: armor man, tool woman, inn woman, pawn old man; voices.ini
    `npc_120..123` overrides). `win_shop_voice`: the four shop states.
