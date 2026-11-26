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
| P | pause: the game dims and a pause panel shows the keys and the active cheats (in HD the game behind it is frosted glass) |
| Space (hold) | fast forward (4×) |
| F12 | screenshot (the picture as shown, HD included) to `Pictures\re-c` |
| F1 | help: every key on screen (the game pauses under it) |
| F2 | FPS counter (frames per second + time per frame; also Video › Show FPS) |
| Game › Turbo B / Turbo A | a held button repeats (rapid sword with B) |
| Game › Remap keys... | choose your own key for each button (on-screen prompts; Backspace keeps, Esc cancels); Reset keys to the defaults undoes it |
| Game › Mouse control | hold the left mouse button to walk toward the cursor; right button = sword, middle button = A (on by default) |
| Game › Damage feedback | the controller rumbles when you are hit and a heartbeat plays while life is low; in HD the screen edges also flash red and pulse |
| F9 | graphics: Original → Enhanced (16-bit) → Enhanced (32-bit) → HD engine (GPU) |
| F5 | reload the graphics pack |
| M | mute |
| F11 / Alt+Enter | fullscreen on / off |
| 1 – 6 | window scale 1× – 6× |
| right-click | the Game / Video menus (also in fullscreen) |
| Esc | quit (the save is written first) |

An XInput controller (Xbox-style) works too: D-pad or left stick to move, A/B buttons = A, X/Y buttons = B,
Back = Select, Start = Start. **RB** (hold) = fast forward, **LB** = pause. You can plug it in while the game is running; the exe looks for it every 2 s.
Minimising the window pauses the game. The picture is drawn at the largest whole-number scale that fits the window,
with black bars around it.

### Cheats menu
Right-click › **Cheats** (or the menu bar): **Infinite life** (energy always full; the LIFE bar stays full), **Infinite
magic**, **Max gold** (60000), **No paralysis / poison**, **Instant sword charge** (hold B), **Quick level-up** (the next
kill levels up), **All cheats off**. They work in gameplay only, are remembered in the settings file (`cheats=`), and the
title bar and pause screen list the ones that are on. A short notice on screen confirms each switch.

### Video menu
The menu bar has **Game** (pause, mute, exit) and **Video**. Right-click anywhere in the window opens the same menus,
which is how you reach them in fullscreen or with the menu bar hidden. While a menu is open the game waits.

| Video item | What it does |
|---|---|
| Fullscreen (F11) | fullscreen on / off |
| Fullscreen resolution | *Desktop resolution* (borderless, the default) or one of the monitor's display modes (32-bit, 640×480 and up). A display mode is changed only while the game is fullscreen and in front; alt-tab or leaving fullscreen puts the desktop back. If Windows refuses a mode, the desktop resolution is used. |
| Window size | 1× – 6× the picture (keys 1 – 6), or a fixed size: 640×480, 800×600, 1024×768, 1280×720, 1280×960, 1366×768, 1600×900, 1920×1080 |
| Graphics (F9) | Original (NES) · Enhanced (16-bit) · Enhanced (32-bit) · HD (GPU: materials, lighting, shadows, parallax); HD effects: Parallax depth · Shadows · Atmosphere · Material detail; Reload graphics pack (F5) — see below |
| Upscale filter | None (sharp pixels, the default) · Scale2x · Scale3x · Scale4x: pixel-art upscalers that round off diagonal stair-steps and curves (text, slopes, sprites) while keeping edges crisp; the result is then sized to the window as usual |
| Aspect ratio | Square pixels (256:240, the default) · NES TV (8:7 pixels, as on a CRT) · 4:3 · 16:10 widescreen · 16:9 widescreen · Stretch to fill the window |
| Integer scaling | on (default): whole-number scale, sharp pixels, black bars · off: as large as the window allows |
| Crop overscan | hides the top and bottom 8 lines (256×224), as most TVs did |
| Smooth filter | soft scaling instead of sharp pixels |
| Show menu bar | hide it for a clean window (right-click still opens the menus) |
| Reset video settings | back to the defaults |

The choices are saved in `%APPDATA%\re-c\video.ini` (delete it to get the defaults back). The game itself is always
256×240; widescreen shapes stretch it.

### Audio menu: Original or CD quality
**Audio › Volume** sets the master volume (100 / 75 / 50 / 25 %).

**Audio › Orchestrated** replays the music on an orchestra: violins carry the melody, french horns the harmony,
string basses the bass line, timpani and snare the drums, in a concert hall. Sound effects keep their original sound.

**Audio › Ambient sounds** (with HD graphics) adds a soundscape under the music: wind and birds outdoors, dripping
caves, crackling fire, whistling ice.

**Audio › Channels** mutes single sound channels (melody, harmony, bass, drums, samples) and **Audio › Headphone
crossfeed** blends a little of each side into the other so hard-panned voices sound natural on headphones (both for CD
quality and Remastered). The sound now follows the sound card's clock smoothly (no clicks from dropped blocks), and
fast forward plays sped-up sound instead of chopped pieces.

**Audio › CD quality (44.1 kHz 16-bit stereo)** upgrades the sound; **Audio › Original (NES, mono)** is the exact
sound of the port. You can switch at any time, and the choice is saved. CD quality:
- **stereo**: pulse 1 a little to the left, pulse 2 a little to the right, noise slightly right, the triangle bass and
  samples in the centre (Audio › Stereo channels turns it back to mono-in-both-ears);
- **cleaner highs**: the sound chip is computed at 4× the CD rate and filtered down with a steep 20 kHz filter, so the
  square waves no longer fold back as hiss (aliasing);
- **fuller bass**: the NES's 90 Hz + 440 Hz bass cut is replaced by a 20 Hz DC filter;
- **the NES's own channel balance** (its non-linear mixer), a soft limiter instead of hard clipping, and 16-bit dither;
- optional light **Reverb** (Audio › Reverb).

**Audio › Remastered** goes further, in the style of a 16-bit remaster (built on the CD path):
- **chorus** on the two melody voices: each gets a slowly swaying, slightly delayed copy on the other side, so they
  sound wide and warm instead of thin;
- **smooth bass**: the triangle channel's 4-bit steps (a faint buzz) are smoothed, with a touch of tape-like warmth;
- **room**: a reverb with more space on the drums than on the melody; a little extra bass (+2.5 dB at 120 Hz) and air
  (+1.5 dB at 7 kHz). Same loudness as CD quality; the bass stays centred.

`ReC.exe --wav ID OUT.wav [SECONDS] --cd [--mono] [--reverb]` writes a music track through the CD path as a stereo WAV,
`--remaster` through the remastered one (music ids `0x14`, `0x15`, `0x1B` are full songs).

### Graphics: Original, Enhanced (16-bit), Enhanced (32-bit) or HD
HD quality: **Auto** (the default) lowers the HD quality when your graphics card cannot keep 50 frames per second and
raises it again when there is room. Each start also keeps the last three saves as `re-c.sav.1` (newest) to `.3` next
to `re-c.sav` (rename one to `re-c.sav` to go back).

**Video › Widescreen playfield** (16:10, 16:9 or 21:9) shows more of the map on both sides of the screen, in Original
and HD graphics: the extra columns are drawn from the area's own map, so they line up exactly and scroll with the
game. They only appear while you are on the map (menus and cut-scenes keep the normal width). Enemies and other objects
out there are drawn too (the game itself only draws its own 256 columns; the extra ones are drawn the same way).

**Video › Display** sets brightness (5 steps), Natural or Vivid colour, and a colour-blind assist for protanopia,
deuteranopia or tritanopia (colours that would look alike are shifted apart), in every graphics mode.

**Video › CRT look** gives Original graphics the look of a TV: scanlines and an RGB aperture mask.

HD › Smooth outlines (on by default) turns pixel stair-steps into smooth curves (characters, rocks, shores).

HD › Modern font (on by default) draws the dialog and story text with a crisp modern font instead of the 8×8 pixel font.

HD › Tilt-shift depth of field (on by default) softens the top and bottom of the playfield like a miniature diorama
(the "HD-2D" look); the middle, where you play, stays sharp.

HD › Ambient bars (on by default) fills the bars beside the picture with a soft glow of it, as modern players do.

HD › Cinematic cut-scenes (on by default) plays the story pictures like film shots: a slow camera push-in with
depth, drifting embers or dust, light rays, lens streaks, film colour and grain; the text stays sharp.

HD › Sharpen (on by default) makes the HD picture crisper without halos; deep water shows moving light caustics.

**Video › Graphics** (or **F9**, which cycles Original → 16-bit → 32-bit → HD) switches the look at any time, even
mid-game. The choice is saved.

**HD engine (GPU)** renders on the graphics card with Direct3D 11 at your screen's own resolution (1080p, 1440p, 4K:
in fullscreen every screen pixel is drawn, not a 4× picture stretched), in the style of a modern lit 2D game. It has
its own renderer: it reads what the game put in video memory (map tiles, palettes, sprite list, scroll and screen
splits) and draws the scene itself. It does not take the emulated NES picture.
- **Layers at 8×:** background and sprites are kept apart and each is rounded on its own (a character's outline is
  smoothed against itself, not against the floor behind it), then built at 8× the NES resolution (2048×1920) with
  graphics-pack tiles placed and filtered inside their own 32×32 cell, so edges stay smooth at any screen size. HUD
  text and numbers stay exact pixels.
- **No sprite flicker:** every sprite on a line is drawn, not only the first 8 (the NES limit that makes busy scenes
  flicker). Switch it off to see the original behaviour.
- **Materials:** every surface is given a material from its colour (stone, water / ice, plaster / cloth, earth / wood /
  sand, grass / foliage) with its own procedural surface relief: cracked stone, grain, grass blades, slowly rippling
  water with twinkling glints. The relief is fixed to the world (it scrolls with the map, it does not swim).
- **Lighting:** a generated height "model" for every visible shape plus the material relief give per-pixel normals,
  lit by a warm sun and a cool sky (glossy highlights per material, ambient occlusion in the creases, rim light on
  characters); bloom on the brightest highlights; a filmic highlight roll-off, a light colour grade and a vignette.
- **Shadows:** soft shadows marched toward the sun through the height model (characters, enemies and walls throw them;
  the edge softens with distance), a soft contact shadow under every character, drifting cloud shadows outdoors.
- **Local lights:** fire, lava and lamp colours light up their surroundings, mostly in dark places (caves, dungeons).
- **Reflections:** water and ice mirror the characters and enemies standing on the shore (rippled), and faintly the
  scenery.
- **Motion and light:** deep water refracts in slow waves, grass sways in gusts of wind, soft sun shafts drift across
  outdoor scenes; fine relief is taken from the art itself (roof tiles, bricks, faces); a tight glow plus a wide halo
  on bright spots; a warmer tone outdoors and a cooler one in dark places; edges are anti-aliased at screen
  resolution (the HUD stays exact).
- **Weather and particles:** snow / ice crystals drift down in icy places, embers rise in fire halls, leaves blow
  across outdoor scenes (each in two depths, scrolling faster than the map); foam lines deep water along the shore
  and around characters wading in it; characters carry a little warm light into dark places.
- **Light that behaves:** the ambient light takes on the colour of its surroundings (bounce light: a red carpet warms
  the floor beside it); fires and characters light from a direction, so bumps facing them catch the light; in
  gameplay the exposure adapts gently to dark and bright places; the output is dithered so smooth gradients do not band.
- **Heat and sheen:** the air shimmers just above fires; deep water (and faintly ice) carries a drifting sky sheen.
- **Built-in art:** the starter graphics packs (hero, enemies, townspeople, bosses, objects, scenery: 5342 tiles, 98 % of
  the sprites and 97 % of the scenery on the screens the test runs explore) are built into
  ReC.exe and used by the HD engine without installing anything (**Video › Graphics › HD: Built-in art** turns them
  off; a pack of your own in `%APPDATA%\re-c\pack` or `--pack` overrides them tile by tile).
- **Quality:** **Video › Graphics › HD quality** Full (default), Balanced or Fast for slower (integrated) graphics:
  Balanced lights at 75 % resolution with shorter shadow marches; Fast lights at 50 %, builds the picture at 4× instead
  of 8× and leaves out reflections and fine relief. **HD: VSync** (on by default) presents in step with the monitor
  (no tearing).
- **Clean edges:** the rows under the status bar that a TV hid (the game leaves stray tiles there) and the left column the
  game masks while scrolling are drawn black like a TV bezel (**HD: Clean edges** turns it off).
- **Lawns:** plain grass gets sunny and shaded patches, blades that lean with the wind and a few small flowers.
- **Plain floors:** the game's flat floors (earth, stone, carpet, sand) get soft stains, grain and grit; outdoor earth a
  few small pebbles. The junk line where the status bar splits from the map is hidden too.
- **HUD style:** the status bar and dialog boxes have smooth, rounded letters and digits with a bevelled, brushed-metal
  look and soft shadows on a glossy panel (**HD: HUD style** turns it off for the exact NES text).
- **Menus:** the equipment, status, shop and item screens get the same bevelled text and glossy windows, with the
  backdrop strips around the windows cleaned up.
- **LIFE and FORCE bars:** LIFE is a glossy red capsule (one mark per 16 HP, a shine sweeping along, a heartbeat when
  low); FORCE keeps the game's chevrons as dark glass that glow cyan as the sword charges.
- **Fades keep the art:** when the game fades a room in or out, painted tiles follow the fade (their colours darken
  with the NES palette) instead of dropping back to the generated look.
- **Parallax:** pits, chasms and the darkness around caves become depth: cliff walls seen in perspective from the
  middle of the screen, and three rock layers far below that scroll slower than the map (the camera comes from the
  game's own scroll) and lie in the ledges' shadows. Dust motes and outdoor mist float in front and scroll faster.
- Each effect can be switched off under **Video › Graphics › HD: …** (saved as `hd_effects=` in `video.ini`). The
  HUD and all text are never lit or filtered; the title, cut-scenes and menus get no pits, clouds or dust (world
  effects need the in-game HUD on screen); graphics-pack tiles are used; nothing the game hides black-on-black is
  revealed (pits are found from large black areas, which stay black-on-black).

Needs a Direct3D 11 graphics card (any PC from the last ~12 years); without one the 32-bit look is used and the title
bar says so. Everything is generated from the game's own graphics at run time (no new artwork or 3D models).

**Enhanced (32-bit)** goes a generation further, toward the look of the 32-bit consoles: everything of the 16-bit
mode below, plus soft rendered lighting (every visible shape is shaded like a smooth, lit surface with a gentle
highlight from the top left; flat areas keep their exact colour), soft drop shadows under characters, enemies and items,
and anti-aliased edges. It is generated from the game's own graphics (no new artwork), costs ~5 ms per frame, and
graphics-pack tiles are used in it too. Nothing the game hides (shapes or sprites drawn in the background colour) is
revealed by the lighting or the shadows.

The 16-bit mode: Enhanced draws every NES pixel as a 4×4 block and adds what 16-bit hardware could show:
- **in-between colours**: NES checkerboard dithering becomes smooth blended colour;
- **rounded shapes**: diagonal stair-steps and curves are smoothed across the whole screen;
- **lighting**: every visible shape gets bevel shading (light from the top left), so rocks, walls, text and characters
  look rounded. Fades and flashes work as before, and nothing the game hides becomes visible.

The game itself is unchanged; only the drawing differs. Enhanced replaces the Upscale filter while it is on.

This built-in look is made by the program from the original tiles. It is richer than the NES picture, but it is not
hand-drawn 16-bit art. For that, you can add your own tiles:

#### Making your own enhanced tiles (graphics pack)
1. Run `ReC.exe --export-tiles C:\some\folder` and play through the part you want to redraw, then quit. The folder gets
   one 32×32 PNG per tile and colour combination you saw, e.g. `01A3_0F1630.png`, showing the built-in Enhanced look of
   that tile, plus `original\` with the NES version at 4×.
2. Paint over the PNGs you want to improve. Keep the size (32×32) and the name. Transparent pixels (alpha below 50 %)
   show the built-in look underneath.
3. Copy the finished PNGs into `%APPDATA%\re-c\pack\` (or start with `--pack DIR`) and press **F5** (Video ›
   Graphics › Reload graphics pack) to see them in the game at once.

**Starter pack for the main character:** `packs/hero/` (133 tiles, see `packs/README.md` and
`packs/hero_preview.png`) — copy its PNGs into `%APPDATA%\re-c\pack\` and press F9.
**Starter pack for the enemies and bosses:** `packs/enemies/` (461 tiles, `packs/enemies_preview.png`) — copy it into
the same folder.
**Starter pack for the backgrounds:** `packs/backgrounds/` (1,839 tiles, `packs/backgrounds_preview.png`) — towns,
fields, caves, shops and the other scenery seen in the test runs; the status bar, menus and text stay as they are. Copy
it into the same folder too.
**Starter pack for the townspeople:** `packs/npcs/` (121 tiles, `packs/npcs_preview.png`) — villagers, shopkeepers and
the people of the story scenes. Same folder.
**Starter pack for the bosses:** `packs/bosses/` (104 tiles, `packs/bosses_preview.png`) — adds what the enemy pack
misses (bosses in the hero's palette, later attack phases, the final scenes). Same folder; the five packs' names never
collide.

How a name matches: `TTTT` is the tile number (hex) and `AABBCC` are the three NES colours (hex) it is drawn with.
The same tile in other colours (another palette, or a step of a fade) uses the built-in look, so draw the tile in the
colours you see at full brightness and copy it under the other names if needed. Bad files are skipped and listed in
the console; the title bar shows how many pack tiles are loaded.

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
ReC.exe --wav ID OUT.wav [SECONDS] [--cd [--mono] [--reverb]]
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
- `--pack DIR` uses another graphics pack folder; `--export-tiles DIR` writes the tiles seen during the run (see above).
- Test hooks: `--menu CMD,..` runs menu items in order at start (`fullscreen`, `menubar`, `integer`, `crop`, `smooth`,
  `reset`, `pause`, `mute`, `size:3x`, `size:1280x720`, `aspect:square|tv|4:3|16:10|16:9|fill`, `upscale:none|scale2x|scale3x|scale4x`, `graphics:original|enhanced|32bit`, `reloadpack`, `audio:original|cd`, `stereo`, `reverb`, `fsmode:desktop`,
  `fsmode:1920x1080`); `--dump-window F.png` saves the window's picture area as drawn when the run ends.

## Checks (Linux, from this folder)
| Command | What it checks |
|---|---|
| `python3 tools/core_vs_web.py --canon --states --checks wake` | Linux core vs the web port: CSV, PNG, RAM, WRAM and CIRAM all identical |
| `python3 tools/win_vs_web.py --canon --states --checks wake` | **dist/ReC.exe under Wine** (headless) vs the web port |
| `python3 tools/win_smoke.py --canon [--no-xinput] [--verbose]` | the real window under Wine + Xvfb: a 600-frame run exits 0, writes the save file, and its PNG equals the headless one; a scripted windowed CSV equals the headless one |
| `python3 tools/win_video_check.py` | the Video menu under Wine + Xvfb: Enhanced graphics 16-bit, 32-bit and HD (Direct3D 11 under Wine, 4x and 1080p fullscreen, HD effects on vs off on a cave frame) (+ export, pack tile, back to Original, the hero / enemy / boss / NPC / background starter packs), upscale filters, window sizes, aspect ratios, crop, fill / smooth, menu bar, fullscreen (+ resolution fallback), settings file saved and reloaded; the drawn picture against the headless frame |
| `python3 tools/apu_wav_compare.py --exe 0x14 0x15 0x1B` | the exe's Original APU output matches `apu_check.mjs` byte for byte on 3 full songs (`0x05` / `0x10` are short effects that end in 0.1 s) |
| `python3 tools/wav_stats.py OUT.wav [SKIP]` | RMS / peak per channel of a WAV (compare Original and CD quality levels) |
| `python3 tools/pack_check.py DIR` | a graphics-pack folder: names, 32×32, decodable (what the exe would skip) |
| `python3 tools/cs_coverage.py` | every web-port function is ported (`// js:` tag) or has a written reason in `docs/waivers.csv` |
| `python3 tools/rea_pe_report.py` + `python3 tools/cs_rea_members.py` | REA inspection of the exe → `docs/rea/summary.md` (imports, hardening, resources, exposure), and every tagged member is still present after trimming |

Wine runs use `WINEPREFIX=$PWD/.wine WINEDEBUG=-all` (the tools set both) and always a timeout.

## Autonomous build
`./re_loop.sh [--max N]` from this folder runs Claude sessions over `TASKS.md`. Each session commits only
`CSHARP-WINDOWS/` paths.
