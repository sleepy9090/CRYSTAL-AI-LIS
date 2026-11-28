// re-c Windows host — the playable window (= web/src/main.js): user32 window + message pump, StretchDIBits blit of the
// 256x240 frame at the largest integer scale (black letterbox), fixed-step 60.0988 Hz loop (Stopwatch = QPC, with
// timeBeginPeriod(1) sleeps), keyboard (Pad.cs), battery WRAM file (Save.cs). architecture_win.md §1 rules 2 / 5 / 8.
//
// Keys: arrows / WASD, X / K = A, Z / J = B, Enter = Start, Right Shift / Tab = Select, P pause, M mute, Esc quit,
// F11 / Alt+Enter fullscreen, 1-6 window scale. Menu bar (Game / Video) + right-click menu: VideoMenu.cs; video
// settings (aspect / widescreen, scaling, overscan, filter, window size, fullscreen resolution): Video.cs.
// Test hooks: --frames N quits after N frames (exit 0); --turbo runs one frame per loop pass without waiting;
// --keys SPEC.. posts WM_KEYDOWN / WM_KEYUP to the window before each frame (same message path as the keyboard);
// --compare F.csv / --dump-screen F.png as in the headless mode; --menu CMD,.. / --dump-window F.png (VideoMenu.cs).
using System.Diagnostics;
using System.Runtime.InteropServices;
using ReC.Game;
using ReC.Util;

namespace ReC.Win;

internal sealed class WindowOptions
{
    public int Scale = 3, Frames = -1, Frame0 = 214;
    /// <summary>playtest 13: --warp AREA (hex): warp there as soon as normal play is reached (testing / debugging)</summary>
    public int Warp = -1;
    public bool Frame0Given, ScaleGiven, Mute, NoAudio, Turbo, Quiet, Fullscreen;
    public string? SavePath, DataDir, StatePath, Screen, Compare, VideoConfig, DumpWindow, PackDir, ExportTiles, OsdShot;
    public readonly List<string> KeySpecs = new(), MenuCmds = new();

    /// <summary>Window-mode options (ArgumentException / FormatException on bad input; `--help` -> null).</summary>
    public static WindowOptions? Parse(IReadOnlyList<string> args)
    {
        var o = new WindowOptions();
        for (int i = 0; i < args.Count; i++)
        {
            string k = args[i];
            string V() => i + 1 < args.Count ? args[++i] : throw new ArgumentException($"{k} needs a value");
            switch (k)
            {
                case "--window": break;
                case "--scale": o.Scale = int.Parse(V()); o.ScaleGiven = true; break;
                case "--fullscreen": o.Fullscreen = true; break;
                case "--video-config": o.VideoConfig = V(); break;
                case "--menu": o.MenuCmds.AddRange(V().Split(',', StringSplitOptions.RemoveEmptyEntries)); break;
                case "--dump-window": o.DumpWindow = V(); break;
                case "--osd": o.OsdShot = V(); break;           // T37 checks: pause | fast | notice=TEXT before --dump-window
                case "--pack": o.PackDir = V(); break;
                case "--export-tiles": o.ExportTiles = V(); break;
                case "--save": o.SavePath = V(); break;
                case "--data": o.DataDir = V(); break;
                case "--load-state": o.StatePath = V(); break;
                case "--mute": o.Mute = true; break;
                case "--no-audio": o.NoAudio = true; break;
                case "--frames": o.Frames = int.Parse(V()); break;
                case "--warp": o.Warp = Convert.ToInt32(V(), 16); break;
                case "--frame0": o.Frame0 = int.Parse(V()); o.Frame0Given = true; break;
                case "--turbo": o.Turbo = true; break;
                case "--dump-screen": o.Screen = V(); break;
                case "--compare": o.Compare = V(); break;
                case "--keys": while (i + 1 < args.Count && !args[i + 1].StartsWith("--")) o.KeySpecs.Add(args[++i]); break;
                case "--quiet": o.Quiet = true; break;
                case "-h": case "--help": return null;
                default: throw new ArgumentException($"unknown option {k} (--help)");
            }
        }
        return o;
    }
}

internal static unsafe partial class Window
{
    /// <summary>playtest 6: the title carries the build time (UTC, from the csproj), so a recording shows which build it is</summary>
    private static readonly string Title = "re-c  [build " + (System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyMetadataAttribute), false)
        .OfType<System.Reflection.AssemblyMetadataAttribute>().FirstOrDefault(a => a.Key == "BuildTime")?.Value ?? "dev") + "]";
    private const string ClassName = "ReCWindow";

    private static IntPtr _hwnd, _hdc;
    private static Input _input = new();
    private static AudioOut _audio = null!;
    private static Pad _pad = null!;
    private static bool _quit, _paused, _hidden, _fullscreen, _scripted;
    private static Win32.WINDOWPLACEMENT _placement;
    private static bool _muted;
    private static double _last, _acc;
    private static readonly Stopwatch Clock = new();
    private static readonly uint[] Px = new uint[560 * 240];   // widescreen: up to 560 columns (_pw wide)
    private static uint[] _up = Array.Empty<uint>();
    private static readonly uint[] _upTmp = new uint[560 * 240 * 4];
    private static int _frameNo, _upFrame = -1, _upK;
    // T10 enhanced graphics (Enhance.cs): the 1024x960 picture, made once per new frame from Fb + Ppu.Tags
    private static Machine? _m;
    private static Enhancer? _enh;
    private static readonly uint[] _hd = new uint[Enhancer.W * Enhancer.H];
    // T60 display colours: the NES palette lookup (as loaded / through the matrix), the matrix, the enhanced picture copy
    private static uint[] _lutBase = new uint[64], _lut = new uint[64], _hdCol = Array.Empty<uint>();
    private static float[] _colorM = ColorAdjust.Matrix(2, false, 0);
    private static int _hdColFrame = -1;
    // widescreen playfield (Wide.cs): the picture is _pw columns; the game's 256 in the middle
    private static readonly Widescreen Wide = new();
    private static int _pw = 256;
    /// <summary>picture width for the current mode: wide in Original graphics (and HD, W2) when on</summary>
    private static int WideW => _v.Wide > 0 && (!_v.Enhanced || (HdEngine && _gpu != null && _gpu.WideOk)) ? Widescreen.Widths[_v.Wide] : 256;
    /// <summary>the NES frame (+ the area at the sides) through the palette lookup into Px</summary>
    private static void Compose()
    {
        if (_m == null) return;
        var fb = ShownFb;
        int pw = WideW;
        if (pw != _pw) { _pw = pw; _v.PicW = pw; _crtFrame = _upFrame = -1; ApplyWindowSize(); }
        if (pw == 256) { for (int i = 0; i < fb.Length; i++) Px[i] = _lut[fb[i] & 0x3F]; return; }
        int e = (pw - 256) / 2, hudTop = HudTop();
        var pal = _m.Ppu.Pal;
        WideAfterFrame(fb);
        float s = Wide.Show;
        Wide.MapLines(fb, hudTop, 0, _mapLines);
        for (int y = 0; y < 240; y++)
        {
            int row = y * pw;
            for (int x = 0; x < 256; x++) Px[row + e + x] = _lut[fb[y * 256 + x] & 0x3F];
            // the sides only beside map lines: not the status bar, not a line the game leaves black (the unloaded rows
            // above the status bar)
            bool map = _mapLines[y];
            if (map && Wide.Match)                        // the game's masked left column (backdrop): the map instead
                for (int x = 0; x < 8; x++) Px[row + e + x] = _lut[Wide.SideColor(e + x, y, e, pal)];
            else if (s > 0 && y < hudTop && Wide.Match)  // a black line: its masked column black too
                for (int x = 0; x < 8; x++) Px[row + e + x] = 0;
            for (int x = 0; x < e; x++)
            {
                uint l = 0, r = 0;
                if (map)
                {
                    l = Dim(_lut[Wide.SideColor(x, y, e, pal)], s);
                    r = Dim(_lut[Wide.SideColor(e + 256 + x, y, e, pal)], s);
                }
                Px[row + x] = l; Px[row + e + 256 + x] = r;
            }
        }
        // the objects in the side columns
        var banks = _m.Ppu.ChrBanks().Select(b => b * 1024).ToArray();
        SideSpr.Draw(_m.Data, _m.Data.Chr, banks, _m.Ppu.Ctrl, pw, e, _sSpr, _sTag, _mapLines);
        if (s > 0)
            for (int i = 0; i < pw * 240; i++)
            {
                int sp = _sSpr[i];
                if (sp == 0) continue;
                int x = i % pw, y = i / pw;
                if ((sp & 0x40) != 0 && Wide.SideOpaque(x, y, e)) continue;
                Px[i] = Dim(_lut[pal[0x10 | (sp & 0x0F)] & 0x3F], s);
            }
    }
    private static readonly bool[] _mapLines = new bool[240];
    private static readonly SideSprites SideSpr = new();
    private static bool _dwmOff;
    // mouse control (Pad.cs MouseControl): cursor in client pixels, buttons held
    private static int _mx, _my;
    private static bool _mL, _mR, _mM;
    private static bool Playing() => _m != null && _m.Rd(0x40) == 1 && _m.Rd(0x41) == 8;
    /// <summary>the mouse as NES buttons: left held = walk toward the cursor (from the player, object slot 1, at its screen
    /// position the sprite pass stored: $05C1 / $05E1), right = B, middle = A (play only, except A)</summary>
    private static int MouseButtons()
    {
        if (!_v.Mouse || _m == null || _hwnd == IntPtr.Zero) return 0;
        bool play = Playing();
        int b = (_mR && play ? Btn.B : 0) | (_mM ? Btn.A : 0);
        if (_mL && play)
        {
            Win32.GetClientRect(_hwnd, out var rc);
            var (nx, ny) = MouseControl.ToNes(_mx, _my, Fit(rc.Right, rc.Bottom), _v.PicW, _v.FirstLine, _v.VisibleH);
            b |= MouseControl.Dir(nx - _m.Rd(0x05C1), ny - (_m.Rd(0x05E1) - 8));
        }
        return b;
    }
    private static readonly byte[] _sSpr = new byte[560 * 240];
    private static readonly int[] _sTag = new int[560 * 240];
    private static int _wideFrame = -1;
    private static SceneRaster? _wideRaster;
    /// <summary>playtest 6: the film look (dolly zoom, grain, streaks) is for the story pages and in-game cut-scenes, not the
    /// title / status / name-entry screens: main_state 3 with title_mode 3-6 (status, name entry, title menu); the opening (0) and the story (1 / 2) are pictures</summary>
    public static bool CineAllowed(int mainState, int titleMode) => mainState != 3 || titleMode is 0 or 1 or 2;
    /// <summary>playtest 7: which remade story scene the game shows (main_state 3, title_mode 2 = the intro story: the
    /// prehistoric jungle; the text pages have no picture band, so nothing is painted there)</summary>
    public static int StoryScene(int mainState, int titleMode) => mainState == 3 && titleMode == 2 ? 1 : 0;
    /// <summary>where the sides stop: the status bar's first line when the HD raster shows it, else 232 (each line is
    /// verified against the map anyway, so the status bar never gets sides)</summary>
    private static int HudTop() => _m != null && _m.Ppu.NoPixels && _raster != null && Gpu.SplitGap(_raster).To is > 0 and var t ? t : 232;
    /// <summary>the match test + fade, once per drawn frame (HD does it right after building the raster)</summary>
    private static void WideAfterFrame(byte[] fb)
    {
        if (_wideFrame == _frameNo || _m == null) return;
        _wideFrame = _frameNo;
        if (_m.Ppu.NoPixels && _raster != null)               // HD: per-line palettes, scrolls (mid-frame splits), sprite layer
            Wide.AfterFrame(fb, _m.Ppu.Pal, HudTop(), _raster.LinePal, _raster.LineScroll, _raster.Sp);
        else Wide.AfterFrame(fb, _m.Ppu.Pal, HudTop());
    }
    private static uint Dim(uint c, float s) => s >= 1 ? c :
        (uint)((int)((c >> 16 & 255) * s) << 16 | (int)((c >> 8 & 255) * s) << 8 | (int)((c & 255) * s));
    private static void ApplyColor()
    {
        _colorM = ColorAdjust.Matrix(_v.Brightness, _v.Vivid, _v.ColorBlind);
        for (int i = 0; i < 64; i++) _lut[i] = ColorAdjust.Apply(_colorM, _lutBase[i]);
        _hdColFrame = -1; _crtFrame = -1; _upFrame = -1;
        if (_gpu != null) _gpu.ColorM = _colorM;
        Compose();                                      // paused: recolour now
    }
    private static int _hdFrame = -1;
    private static bool _quiet, _exporting;
    private static bool _turbo;

    /// <summary>T20: HD draws with its own engine (SceneRaster + Gpu) from the captured video state, the PPU makes no pixels</summary>
    private static bool HdEngine => _v.Enhanced && _v.Hd;

    /// <summary>a picture to make per frame: Enhanced looks (PPU tags), HD engine (video capture), --export-tiles</summary>
    private static bool NeedRender => _m != null && (_m.Ppu.Tags != null || _m.Ppu.Capture != null);

    /// <summary>the frame on screen as NES colours: the HD engine's own raster while it runs, else the PPU's</summary>
    private static byte[] ShownFb => _m!.Ppu.NoPixels && _raster != null ? _raster.Fb : _m.Ppu.Fb;

    /// <summary>tags / video capture are recorded only while they are needed (Enhanced on, or --export-tiles)</summary>
    private static void EnsureTags()
    {
        if (_m == null) return;
        var ppu = _m.Ppu;
        if (HdEngine)
        {
            ppu.Capture ??= new ReC.Nes.VideoCapture();
            ppu.NoPixels = true;
            ppu.Tags = null;                                 // the raster makes its own tags
            _raster ??= new SceneRaster(ppu.Chr);
        }
        else
        {
            if (ppu.NoPixels && _raster != null) Array.Copy(_raster.Fb, ppu.Fb, ppu.Fb.Length);   // until the PPU draws again
            ppu.NoPixels = false;
            ppu.Capture = null;
            if (_v.Enhanced || _exporting) ppu.Tags ??= new int[256 * 240];
            else ppu.Tags = null;
        }
        _hdFrame = -1;
    }

    private static readonly byte[] _fSpr = new byte[256 * 240];
    private static readonly int[] _fTag = new int[256 * 240];
    private static readonly bool[] _fLines = new bool[240];

    /// <summary>playtest 6 ("NES flicker"): in plain play (main_state 1, sub_state 8 SubstPlay, no message window: irq_mode 0) the objects the
    /// game left out of this frame's sprite list are drawn from object RAM, so nothing blinks at 30 Hz</summary>
    private static readonly byte[] _xSpr = new byte[256 * 240];
    private static readonly int[] _xTag = new int[256 * 240];
    private static readonly bool[] _xLines = Enumerable.Repeat(true, 240).ToArray();

    /// <summary>playtest 12 ("enhance the boss lasers and bullets, all the boss effects"): the shown sprite pixels of enemy / boss
    /// shots (spawn byte $0540 = $FF, IsEnemyShot 26:907C), player shots (slots 2, 4-11) and explosions / puffs (AI $74 boss
    /// explosion, $08 its shared step, $7D death puff) get HdScene.FShotEnemy / FShotPlayer / FBoom in the scene info: the
    /// shaders make them glow, bloom and light their surroundings. Gameplay only (main_state 1). Returns the pixels marked</summary>
    public static int FxMark(ReC.Nes.VideoCapture cap, SceneRaster raster, HdScene sc, SideSprites ss, Machine m)
    {
        if (!ss.Valid || ss.Ram[0x40] != 1 || raster.W != 256) return 0;
        int my = Enumerable.Range(16, 160).FirstOrDefault(y => cap.SprValid[y], -1);
        if (my < 0) return 0;
        var rg = cap.Spr[my];
        var banks = new int[8];
        for (int k = 0; k < 8; k++) banks[k] = rg.Banks[k];
        var ram = ss.Ram;
        int marked = 0;
        foreach (var (flag, test) in new (byte, Func<int, bool>)[]
        {
            (HdScene.FShotEnemy, x => x >= 0x0D && ram[0x540 + x] == 0xFF),
            (HdScene.FBoom, x => x >= 0x0D && ram[0x4A0 + x] is 0x74 or 0x08 or 0x7D),
        })
        {
            if (!Enumerable.Range(0, 32).Any(x => ram[0x4A0 + x] != 0 && test(x))) continue;
            ss.Draw(m.Data, m.Ppu.Chr, banks, rg.Ctrl, 256, 0, _xSpr, _xTag, _xLines, includeCenter: true, slotOnly: test);
            for (int i = 0; i < 256 * 240; i++)
                if (_xSpr[i] != 0 && raster.Sp[i] != 0) { sc.Info[i * 4 + 2] |= flag; marked++; }
        }
        // playtest 13: the player's body (slot 1) for the armour finish
        if (ram[0x4A1] != 0)
        {
            ss.Draw(m.Data, m.Ppu.Chr, banks, rg.Ctrl, 256, 0, _xSpr, _xTag, _xLines, includeCenter: true, slotOnly: x => x == 1);
            for (int i = 0; i < 256 * 240; i++)
                if (_xSpr[i] != 0 && raster.Sp[i] != 0 && (sc.Info[i * 4 + 2] & 0x70) == 0) sc.Info[i * 4 + 2] |= HdScene.FPlayer;
        }
        // player sword shots, charge and magic: drawn from the sword's CHR bank (R3 = $41 + sword level, banks $41-$46;
        // the player himself is bank $40), not by the object sprite pass, so they are found by their tiles
        // (items and other world objects that share the bank are slots $0D-$1F: their replayed pixels are left out)
        ss.Draw(m.Data, m.Ppu.Chr, banks, rg.Ctrl, 256, 0, _xSpr, _xTag, _xLines, includeCenter: true, slotOnly: x => x >= 0x0D);
        for (int i = 0; i < 256 * 240; i++)
        {
            int st = raster.SpTag[i];
            if (raster.Sp[i] == 0 || (st & ReC.Nes.Ppu.TagValid) == 0) continue;
            int bank = (st & 0x1FFF) >> 6;
            if (bank is < 0x41 or > 0x46) continue;
            if (_xSpr[i] != 0) continue;                                        // a world object's pixel
            int dx = (i & 255) - ram[0x5C1], dy = (i >> 8) - ram[0x5E1];      // the sword in his hand: its element glow
            if (dx is > -14 and < 14 && dy is > -26 and < 8) { sc.Info[i * 4 + 2] |= HdScene.FPlayer | HdScene.FBoom; continue; }
            sc.Info[i * 4 + 2] |= HdScene.FShotPlayer; marked++;
        }
        return marked;
    }

    public static int FillFlicker(ReC.Nes.VideoCapture cap, SceneRaster raster, SideSprites ss, Machine m, int hudTop)
    {
        // SubstPlay only (sub_state 8): cut-scenes (the wake-up scene $13 keeps the player object undrawn until he climbs out
        // of the chamber), area loads, talks and menus draw what they mean to
        if (!ss.Valid || ss.Ram[0x40] != 1 || ss.Ram[0x41] != 8 || ss.Ram[0x51] != 0) return 0;
        int my = Enumerable.Range(16, 160).FirstOrDefault(y => cap.SprValid[y], -1);
        if (my < 0) return 0;
        var rg = cap.Spr[my];
        var banks = new int[8];
        for (int k = 0; k < 8; k++) banks[k] = rg.Banks[k];
        for (int y = 0; y < 240; y++) _fLines[y] = y >= 8 && y < hudTop;
        // playtest 13: the player (slot 1) only while his after-hit blink runs (invuln_timer $071A), and never in the cryo-pod
        // room (area $00): the walk out of the pod blinks him on purpose (materialising), and a whole extra copy over the
        // game's own frames showed a second pair of legs
        var sram = ss.Ram;
        ss.Draw(m.Data, m.Ppu.Chr, banks, rg.Ctrl, 256, 0, _fSpr, _fTag, _fLines, missingOnly: true, slotOnly: x => x != 1 || (sram[0x71A] != 0 && sram[0x6C] != 0));
        return raster.ApplyMissing(_fSpr, _fTag);
    }

    private static void FillFlicker(ReC.Nes.VideoCapture cap) => FillFlicker(cap, _raster!, SideSpr, _m!, HudTop());

    private static void RenderHd()
    {
        if (_m == null || _enh == null) return;
        byte[] fb, pal;
        int[] tags;
        if (_m.Ppu.Capture is { } cap)
        {
            if (cap.Frames == 0 || _raster == null) return;  // nothing captured yet (HD was just chosen)
            _raster.SpriteLimit = !_v.HdNoFlicker;
            _raster.Build(cap);
            if (_v.HdNoFlicker) FillFlicker(cap);               // playtest 6: objects the game skipped this frame
            _raster.PodSilhouette(_m.Rd(0x6C), _m.Rd(0x40), _m.Rd(0x41));   // playtest 9: no black figure in the cryo pod
            (fb, tags, pal) = (_raster.Fb, _raster.Tags, _raster.LinePal[239]);
        }
        else if (_m.Ppu.Tags is { } t) (fb, tags, pal) = (_m.Ppu.Fb, t, _m.Ppu.Pal);
        else return;
        if (HdEngine && _gpu != null)
        {
            if (_exporting) { _enh.ModeHd = true; _enh.Render(fb, tags, pal, _hd); }   // records the tiles seen
            else _enh.LiftHd(fb, tags);
            _scene ??= new HdScene(_nesRgb);
            _scene.Build(fb, tags, pal, _enh.HdLift, _raster!.LineScroll, _frameNo);
            if (_m.Ppu.Capture is { } fxCap) FxMark(fxCap, _raster, _scene, SideSpr, _m);   // playtest 12: glowing shots / explosions
            _gpu.Hp = _m.Mem.RamBytes[0x03C1];               // T35: LIFE meter fill (HudHpBar draws from it)
            _gpu.Hurt = _v.Feedback ? Damage.Hurt : 0; _gpu.LowLife = _v.Feedback && Damage.Low;   // T57
            _gpu.CineAllowed = CineAllowed(_m.Mem.RamBytes[0x40], _m.Mem.RamBytes[0x6F0]);
            _gpu.StoryScene = StoryScene(_m.Mem.RamBytes[0x40], _m.Mem.RamBytes[0x6F0]);   // playtest 7: remade story art
            _gpu.Room = DialogueReader.InShop(_m.Mem.RamBytes[0x40], _m.Mem.RamBytes[0x41]);   // playtest 12: shop / inn interiors
            _gpu.Armor = _m.Mem.RamBytes[0x713]; _gpu.Sword = _m.Mem.RamBytes[0x711];   // playtest 13: equipment finish
            if (WideW > 256)                                 // widescreen: the wide copy of the raster, the sides from the area
            {
                WideAfterFrame(_raster!.Fb);
                if (_wideRaster == null || _wideRaster.W != WideW) _wideRaster = new SceneRaster(_m.Ppu.Chr, WideW);
                Wide.MapLines(_raster.Fb, HudTop(), 0.5f, _mapLines);
                // playtest 6: the map continues behind the status bar (the HUD panel floats over it) while the sides show
                var gap = Gpu.SplitGap(_raster);
                int hudFrom = Wide.Show > 0.5f && Wide.Active ? (gap.From > 0 ? gap.From : HudTop()) : 240;
                _wideRaster.Widen(_raster, Wide, _mapLines, hudFrom, gap.To > 0 ? gap.To : hudFrom, Gpu.EdgeBand(_raster, _scene));
                {
                    var capt = _m.Ppu.Capture;
                    int my = capt == null ? -1 : Enumerable.Range(16, 160).FirstOrDefault(y => capt.SprValid[y], -1);   // a line's sprite CHR banks + PPUCTRL
                    if (capt != null && my >= 0)
                    {
                        var rg = capt.Spr[my];
                        var banks = new int[8];
                        for (int k = 0; k < 8; k++) banks[k] = rg.Banks[k];
                        SideSpr.Draw(_m.Data, _m.Ppu.Chr, banks, rg.Ctrl, WideW, (WideW - 256) / 2, _sSpr, _sTag, _mapLines, leftMask: _raster!.LeftClip);   // playtest 7: the masked left column hid sprites
                        _wideRaster.ApplySideSprites(_sSpr, _sTag);
                    }
                }
                int bar = Gpu.SplitGap(_raster).To;               // the status bar's first line (the first IRQ split may be higher)
                _gpu.HudOverlay = hudFrom < 240;
                _gpu.SideVoidTop = Wide.Show > 0.5f ? (hudFrom < 240 ? 240 : bar > 0 ? bar : HudTop()) : 0;
                _gpu.Upload(_wideRaster, _scene, _raster);
            }
            else { _gpu.HudOverlay = false; _gpu.Upload(_raster, _scene); }
        }
        else
        {
            _enh.ModeHd = false;
            _enh.Mode32 = _v.Enhanced32 || HdEngine;          // HD without Direct3D: the 32-bit look
            _enh.Render(fb, tags, pal, _hd);
        }
        _hdFrame = _frameNo;
    }

    private static SceneRaster? _raster;

    /// <summary>T25: the HD engine atlas = the built-in starter packs (if on) + the user pack over them; returns the tiles
    /// left out (atlas full)</summary>
    private static int GpuArt()
    {
        if (_gpu == null || _enh == null || _m == null) return 0;
        var builtin = _v.HdArt ? BuiltinArt.Embedded() : null;
        foreach (string e in BuiltinArt.Errors) Console.Error.WriteLine($"[re-c] built-in art: {e}");
        BuiltinArt.Errors.Clear();
        return _gpu.SetArt(BuiltinArt.Merge(builtin, _enh.Art), _m.Ppu.Chr.Length / 16);
    }

    // T18 HD graphics (Gpu.cs): created when HD is chosen, released when another look is (GDI draws again)
    private static Gpu? _gpu;
    private static HdScene? _scene;
    private static readonly DamageFeedback Damage = new();     // T57
    private static readonly DialogueReader _dialogue = new();
    private static Speech? _speech;
    private static IVoice? _neural;
    private static int _lastIds = -1;
    private static bool _speechTried;
    private static bool _automated;
    private static int _pendingWarp = -1;
    /// <summary>playtest 6: says each message page as it appears (DialogueReader); the voice starts on first use</summary>
    /// <summary>playtest 6: the dialogue voice, started with the game (the model loads in the background): Piper from the
    /// voice folder next to the exe, else the Windows voice; %APPDATA%\re-c\voice.log says which and why</summary>
    private static void StartVoice()
    {
        if (_speechTried || !_v.Voice || _scripted || _automated) return;
        _speechTried = true;
        string cfg = Path.GetDirectoryName(VideoSettings.DefaultPath())!;
        NeuralVoice.LogPath = Path.Combine(cfg, "voice.log");
        try { File.WriteAllText(NeuralVoice.LogPath, ""); } catch (Exception) { }
        Cast.LoadOverrides(Path.Combine(cfg, "voices.ini"));
        // playtest 7: Kokoro (voice\kokoro) first, the more natural voice; then Piper (voice\piper); then Windows.
        // playtest 9: Audio > Voice engine picks one (auto = that order)
        string eng = _v.VoiceEngine;
        NeuralVoice.Log($"voice engine setting: {eng}");
        if (eng is "auto" or "kokoro")
        {
            if (KokoroVoice.Folder() is string kd && KokoroVoice.TryStart(kd) is { } kv) { _neural = kv; NeuralVoice.Log("using Kokoro (voice\\kokoro)"); }
            else NeuralVoice.Log("no Kokoro: voice\\kokoro must hold sherpa-onnx-offline-tts.exe, the model, voices.bin, tokens.txt, espeak-ng-data");
        }
        if (_neural == null && eng is "auto" or "kokoro" or "piper")
        {
            string? vd = NeuralVoice.Folder();
            if (vd == null) NeuralVoice.Log($"no Piper: {Path.Combine(AppContext.BaseDirectory, "voice")} must hold piper\\piper.exe, {NeuralVoice.ModelName} (+ .json) and speakers.txt");
            else _neural = NeuralVoice.TryStart(vd);
        }
        if (_neural == null) { _speech = Speech.TryCreate(); NeuralVoice.Log(_speech != null ? "using the Windows voice" : "no Windows voice either"); }
        _audio.Voice = _neural;
    }

    /// <summary>playtest 9: a new voice engine choice: stop and start the voice again</summary>
    private static void RestartVoice()
    {
        _audio.Voice = null;
        _neural?.Dispose(); _neural = null;
        _speech?.Dispose(); _speech = null;
        _speechTried = false;
        StartVoice();
    }

    private static void Speak(Machine m)
    {
        // playtest 9: Kokoro failed on this PC (voice.log has its whole output): Piper (or Windows) takes over, and says
        // the page that failed
        if (_neural is KokoroVoice kf && kf.Failed)
        {
            var page = kf.FailedPage;
            kf.Dispose();
            _neural = NeuralVoice.Folder() is string pd ? NeuralVoice.TryStart(pd) : null;
            if (_neural == null) _speech ??= Speech.TryCreate();
            _audio.Voice = _neural;
            NeuralVoice.Log("kokoro failed: now " + (_neural?.Name ?? (_speech != null ? "WINDOWS" : "no voice")));
            Notice("KOKORO VOICE FAILED - " + (_neural?.Name ?? "WINDOWS") + " VOICE (SEE VOICE.LOG)");
            if (page is { } pg && _dialogue.Showing)
            {
                if (_neural != null) _neural.Say(pg.Npc, pg.Text);
                else _speech?.SayXml(Cast.Xml(pg.Npc, pg.Text));
            }
        }
        // playtest 13: a message id set during gameplay (the NPC script, an event) = its pages are made now, ahead of the window
        int ids = m.Rd(0x7DF) << 8 | m.Rd(0x7DE);
        if (ids != _lastIds && m.Rd(0x40) == 1 && _v.Voice && !_muted && _neural is KokoroVoice kp)
        {
            try { kp.Prefetch(Cast.Speaker(a => m.Rd(a)), MessageText.Pages(m.Data.Prg, m.Rd(0x7DF), m.Rd(0x7DE), a => m.Rd(a))); }
            catch (Exception e) when (e is IndexOutOfRangeException or ArgumentException) { }
        }
        _lastIds = ids;
        var (say, stop) = _dialogue.Update(m.Rd(0x40), m.Rd(0x51), a => m.Rd(a), m.Ppu.VramRead);
        if (stop) { _speech?.Stop(); _neural?.Stop(); }
        if (say == null || !_v.Voice || _muted || _fast) return;
        StartVoice();
        if (_neural != null && !_neural.Alive)
        {
            // Piper ended (a missing DLL, a broken model): the Windows voice from now on, and say why
            NeuralVoice.Log("piper stopped: " + ((_neural as NeuralVoice)?.ExitInfo ?? "?"));
            _audio.Voice = null; _neural.Dispose(); _neural = null;
            _speech ??= Speech.TryCreate();
            Notice("NEURAL VOICE STOPPED - WINDOWS VOICE (SEE VOICE.LOG)");
        }
        int npc = Cast.Speaker(a => m.Rd(a));
        if (_neural != null) _neural.Say(npc, say);
        else _speech?.SayXml(Cast.Xml(npc, say));   // each NPC its own voice (gender, age)
    }
    /// <summary>T54: what the ambient sounds play, from the HD scene (gameplay only; nothing without the HD engine)</summary>
    private static double _lastStrike = -1;
    private static void AmbienceTargets()
    {
        if (_audio == null) return;
        var a = _audio.Amb;
        a.Heartbeat = _v.Feedback && Damage.Low && !_paused;      // T58
        if (_v.Ambience && HdEngine && _gpu != null && _scene is { } s)
        {
            float lum = s.Mean.R * 0.299f + s.Mean.G * 0.587f + s.Mean.B * 0.114f;
            a.Outdoor = s.Outdoor; a.Hot = s.Hot; a.Icy = s.Icy;
            a.WeatherWind = Weather.Wind(s.Time).Strength;           // playtest 12: heard as the trees show it
            // playtest 13: rain outdoors (snow is silent), thunder a moment after each lightning strike
            var sky = Weather.Sky(s.Time);
            float outside = DialogueReader.InShop(_m!.Rd(0x40), _m.Rd(0x41)) ? 0 : Math.Clamp((s.Outdoor - 0.55f) / 0.3f, 0, 1);   // not indoors
            a.Rain = (float)(sky.Precip * outside * (1 - Math.Min(1, s.Icy * 2)) * s.Play);
            if (sky.Strike > _lastStrike && outside > 0.5) { a.Thunder(0.5 + 1.8 * ((sky.Strike * 7.31) % 1)); }
            if (sky.Strike > _lastStrike) _lastStrike = sky.Strike;
            a.Cave = s.Play * (1 - s.Outdoor) * (1 - s.Hot) * (1 - s.Icy) * Math.Clamp((0.32f - lum) / 0.15f, 0, 1);
        }
        else { a.Outdoor = a.Hot = a.Icy = a.Cave = 0; a.Rain = 0; }
    }
    private static int[] _nesRgb = Array.Empty<int>();

    /// <summary>T19: the Video > Graphics > HD effect switches -> the renderer</summary>
    private static void ApplyHdEffects()
    {
        if (_gpu == null) return;
        _gpu.Parallax = _v.HdParallax; _gpu.Shadows = _v.HdShadows; _gpu.Atmosphere = _v.HdAtmosphere; _gpu.Detail = _v.HdDetail;
        _gpu.Reflections = _v.HdReflections; _gpu.Lights = _v.HdLights;
        _gpu.VSync = _v.HdVsync && !_turbo;                  // --turbo runs never wait for the monitor
        _gpu.Quality = _v.HdQuality == 3 ? Auto.Level : _v.HdQuality;   // T62: 3 = auto
        _gpu.Edges = _v.HdEdges;
        _gpu.HudStyle = _v.HdHud;
        _gpu.Sharpen = _v.HdSharpen;
        _gpu.Cinematic = _v.HdCinematic;
        _gpu.SmoothOutlines = _v.HdSmooth;
        _gpu.Ambient = _v.HdAmbient;
        _gpu.TiltShift = _v.HdTilt;
        _gpu.ModernFont = _v.HdFont;
    }
    private static string _gpuError = "";
    private static bool _gpuTried;

    private static void EnsureGpu()
    {
        if (!_v.Hd)
        {
            if (_gpu != null) { _gpu.Dispose(); _gpu = null; _gpuTried = false; Win32.InvalidateRect(_hwnd, IntPtr.Zero, true); }
            return;
        }
        if (_gpu != null || _gpuTried || _hwnd == IntPtr.Zero) return;
        _gpuTried = true;
        Win32.GetClientRect(_hwnd, out var rc);
        _gpu = Gpu.TryCreate(_hwnd, rc.Right, rc.Bottom, _nesRgb, out _gpuError);
        if (_gpu != null) _gpu.ColorM = _colorM;
        ApplyHdEffects();
        string font = "";
        if (_gpu != null && _m != null)
        {
            // T59 modern font: glyphs sized on the game font's 'H' (cap height, top row)
            var chr = _m.Ppu.Chr;
            var (top, bottom) = FontInk.InkRows(chr, FontInk.First + 'H');
            var atlas = top >= 0 ? GlyphAtlas.Render(top, bottom) : null;
            if (atlas != null) { _gpu.Chr = chr; _gpu.SetGlyphs(atlas); font = $", modern font (caps rows {top}-{bottom})"; }
        }
        int left = GpuArt();
        if (!_quiet)
            Console.Error.WriteLine(_gpu != null ? $"[re-c] HD engine: {_gpu.Info}, {_gpu.Slots} pack tiles in the atlas{font}" +
                                                   (left > 0 ? $" ({left} left out: atlas full)" : "")
                                                 : $"[re-c] HD graphics not available ({_gpuError}): using the 32-bit look");
        _hdFrame = -1;
    }
    private static Win32.BITMAPINFOHEADER _bmi;
    private static readonly uint Style = Win32.WS_OVERLAPPEDWINDOW;

    // js: main.js status
    /// <summary>= the page's status line: shown in the window title (paused / muted).</summary>
    /// <summary>T42: F12 = the picture as shown (HD included) to Pictures\re-c\re-c_YYYYMMDD_HHMMSS.png</summary>
    /// <summary>the folder screenshots go to: Pictures\re-c</summary>
    private static string ShotDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "re-c");

    /// <summary>a free file name: re-c_YYYYMMDD_HHMMSS.png, then _2, _3 .. within the same second</summary>
    public static string ShotPath(string dir, DateTime t)
    {
        string b = Path.Combine(dir, $"re-c_{t:yyyyMMdd_HHmmss}");
        string p = b + ".png";
        for (int k = 2; File.Exists(p); k++) p = $"{b}_{k}.png";
        return p;
    }

    private static void OpenShotFolder()
    {
        try { Directory.CreateDirectory(ShotDir); Win32.ShellExecuteW(_hwnd, "open", ShotDir, null, null, 1 /* SW_SHOWNORMAL */); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or DllNotFoundException or EntryPointNotFoundException) { Notice("COULD NOT OPEN THE FOLDER"); }
    }

    private static void Screenshot()
    {
        try
        {
            string dir = ShotDir;
            Directory.CreateDirectory(dir);
            string path = ShotPath(dir, DateTime.Now);
            _notice = null;                                    // no notice in the picture
            DumpWindow(path);
            Notice("SCREENSHOT SAVED: PICTURES/RE-C");
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { Notice("SCREENSHOT FAILED"); }
        Present();
    }

    /// <summary>T36: the title-bar status: paused, muted, fast forward, cheats</summary>
    private static void StatusLine()
    {
        var p = new List<string>();
        if (_paused) p.Add("paused (P)");
        if (_muted) p.Add("muted (M)");
        if (_fast) p.Add("fast forward (Space)");
        if (_v.Cheats.Any) p.Add("cheats: " + _v.Cheats.Serialize());
        Status(string.Join(", ", p));
    }
    private static bool _fast;
    private static bool _fastKey, _lbDown;
    /// <summary>T46: controller shoulders: RB held = fast forward (with Space), LB = pause on / off</summary>
    private static void PadExtras(double now)
    {
        _pad.Read(now);
        bool f = _fastKey || (_pad.Raw & 0x0200) != 0;
        if (f != _fast) { _fast = f; StatusLine(); if (_paused) Present(); }
        bool lb = (_pad.Raw & 0x0100) != 0;
        if (lb && !_lbDown) KeyCommand('P');
        _lbDown = lb;
    }
    // T37 on-screen display: a short notice (cheat / setting switched), the pause screen, the fast-forward mark
    private static bool _osdReady;
    private static string? _notice;
    private static double _noticeUntil;
    private static uint[] _osdBuf = Array.Empty<uint>();
    public static void Notice(string s)
    {
        if (!_osdReady) return;
        _notice = s; _noticeUntil = Clock.Elapsed.TotalMilliseconds + 2000;
    }
    /// <summary>builds the overlay for now; false = nothing on screen</summary>
    private static bool BuildOsd()
    {
        if (_notice != null && Clock.Elapsed.TotalMilliseconds > _noticeUntil) _notice = null;
        if (!_paused && !_fast && _notice == null && !_help && !_osdShown && !_v.ShowFps && _v.Debug.Count == 0 && _remap < 0 && _portrait == null) return false;
        var cheats = Enumerable.Range(0, Cheats.Names.Length).Where(_v.Cheats.Get).Select(i => Cheats.Names[i]).ToList();
        _osdShown = Osd.Build(_paused, _fast, _notice, cheats, _help, _v.ShowFps ? Stats.Text : null,
            _remap >= 0 ? $"PRESS A KEY FOR {Input.Names[_remap]}\nNOW: {(_v.KeyBinds[_remap] != 0 ? Input.KeyName(_v.KeyBinds[_remap]) : "DEFAULT KEYS ONLY")}\nBACKSPACE KEEPS IT   ESC CANCELS" : null,
            DebugLines());
        if (_portrait != null && !_paused) { Osd.Portrait(12, 172, 1, _portrait, _portW, _portH); _osdShown = true; }
        return _osdShown;
    }
    private static bool _osdShown;
    private static uint[]? _portrait;
    private static int _portW, _portH;
    private static readonly byte[] _pSpr = new byte[256 * 240];
    private static readonly int[] _pTag = new int[256 * 240];

    /// <summary>playtest 13 ("when talking to NPCs add a face portrait by the text"): while an NPC talks (sub_state $11, a talking
    /// NPC id from Cast.Speaker), its sprite (talk slot $0623) is replayed from the object snapshot (SideSprites), cropped
    /// to its pixels and kept for the overlay; cleared when the talk ends. HD engine (the sprite CHR banks of the capture)</summary>
    /// <summary>Scale2x (EPX): each pixel into 2 x 2, corners taking a neighbour's colour where two edges agree (0 = clear)</summary>
    private static (uint[] P, int W, int H) Scale2x(uint[] s, int w, int h)
    {
        var o = new uint[w * 2 * h * 2];
        uint At(int x, int y) => x < 0 || y < 0 || x >= w || y >= h ? 0 : s[y * w + x];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                uint p = At(x, y), a = At(x, y - 1), b = At(x + 1, y), c = At(x - 1, y), d = At(x, y + 1);
                uint e0 = p, e1 = p, e2 = p, e3 = p;
                if (a != d && c != b)
                {
                    if (c == a) e0 = a;
                    if (a == b) e1 = b;
                    if (c == d) e2 = c;
                    if (d == b) e3 = d;
                }
                int ox = x * 2, oy = y * 2, ow = w * 2;
                o[oy * ow + ox] = e0; o[oy * ow + ox + 1] = e1; o[(oy + 1) * ow + ox] = e2; o[(oy + 1) * ow + ox + 1] = e3;
            }
        return (o, w * 2, h * 2);
    }

    private static void UpdatePortrait()
    {
        var m = _m;
        if (m == null || m.Rd(0x41) != 0x11 || m.Ppu.Capture is not { } cap || !SideSpr.Valid) { _portrait = null; return; }
        int npc = Cast.Speaker(a => m.Rd(a));
        if (npc is < 0 or >= 0x100) { _portrait = null; return; }
        int slot = m.Rd(0x623);
        // the CHR banks and palette of the NPC's own line (the message window rows use other banks)
        int ny = Math.Clamp(m.Rd(0x5E0 + slot), 0, 239);
        int my = Enumerable.Range(0, 240).Select(k => (ny + k) % 240).FirstOrDefault(y => cap.SprValid[y], -1);
        if (my < 0) return;                                          // keep the last one
        var rg = cap.Spr[my];
        var banks = new int[8];
        for (int k = 0; k < 8; k++) banks[k] = rg.Banks[k];
        // playtest 13: always the front (facing down, standing): the facing offset $0360 and the walk frame $04E0 set to 0 in
        // the snapshot copy for this replay, then put back
        var ram = SideSpr.Ram;
        byte keepDir = ram[0x360 + slot], keepAnim = ram[0x4E0 + slot], keepMeta = ram[0x300 + slot];
        // the front view is the most left-right symmetric of the facings (0-3): each is replayed and scored, the best is kept
        int bestDir = 2; double bestSym = -1;
        // NPCs turn by their sprite id (the facing flag b0 of $0380 is set): candidates = the ids of the pair, and the facings
        int[] cands = { keepMeta & 0xFC, (keepMeta & 0xFC) + 1, (keepMeta & 0xFC) + 2, (keepMeta & 0xFC) + 3 };   // the four facings
        for (int dir = 0; dir < 4; dir++)
        {
            ram[0x300 + slot] = (byte)cands[dir]; ram[0x360 + slot] = 0; ram[0x4E0 + slot] = 0;
            SideSpr.Draw(m.Data, m.Ppu.Chr, banks, rg.Ctrl, 256, 0, _pSpr, _pTag, _xLines, includeCenter: true, slotOnly: x => x == slot);
            int bx0 = 256, bx1 = -1;
            for (int i = 0; i < 256 * 240; i++) if (_pSpr[i] != 0) { bx0 = Math.Min(bx0, i & 255); bx1 = Math.Max(bx1, i & 255); }
            if (bx1 < 0) continue;
            int same = 0, tot = 0;
            for (int i = 0; i < 256 * 240; i++)
            {
                if (_pSpr[i] == 0) continue;
                int x = i & 255, mx = bx0 + bx1 - x;
                tot++;
                if ((_pSpr[(i & ~255) + mx] & 0x0F) == (_pSpr[i] & 0x0F)) same++;
            }
            // + detail in the head (colour changes inside its top 10 lines): a face has eyes, the back of a head is one colour
            int by0 = 240; for (int i = 0; i < 256 * 240; i++) if (_pSpr[i] != 0) { by0 = i >> 8; break; }
            int changes = 0;
            for (int y = by0; y < Math.Min(240, by0 + 10); y++)
                for (int x = bx0 + 1; x <= bx1; x++)
                {
                    int a2 = _pSpr[y * 256 + x], b2 = _pSpr[y * 256 + x - 1];
                    if (a2 != 0 && b2 != 0 && (a2 & 0x0F) != (b2 & 0x0F)) changes++;
                }
            double sym = same / (double)Math.Max(1, tot) + 0.03 * changes;
            if (sym > bestSym + 0.02) { bestSym = sym; bestDir = dir; }
        }
        ram[0x300 + slot] = (byte)cands[bestDir]; ram[0x360 + slot] = 0; ram[0x4E0 + slot] = 0;
        SideSpr.Draw(m.Data, m.Ppu.Chr, banks, rg.Ctrl, 256, 0, _pSpr, _pTag, _xLines, includeCenter: true, slotOnly: x => x == slot);
        ram[0x360 + slot] = keepDir; ram[0x4E0 + slot] = keepAnim; ram[0x300 + slot] = keepMeta;
        int x0 = 256, x1 = -1, y0 = 240, y1 = -1;
        for (int i = 0; i < 256 * 240; i++)
            if (_pSpr[i] != 0) { int x = i & 255, y = i >> 8; x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        if (x1 < 0) return;                                          // not drawn this frame: keep the last one
        int w = Math.Min(x1 - x0 + 1, 32), h = Math.Min(y1 - y0 + 1, 16);   // the head and shoulders
        var pix = new uint[w * h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var pal = _raster != null ? _raster.LinePal[y0 + y] : m.Ppu.Pal;
                int sp = _pSpr[(y0 + y) * 256 + x0 + x];
                if (sp == 0) continue;
                int col = pal[0x10 | (sp & 0x0F)] & 0x3F;
                pix[y * w + x] = (uint)_nesRgb[col] | 0x01000000;          // (nonzero even for black)
            }
        // playtest 13: "more sophisticated": Scale2x twice (smooth pixel-art edges, x4), then shading: a light from the top left
        // across the figure, a darker lower part, and a soft dark outline around it
        var (p2, w2, h2) = Scale2x(pix, w, h);
        var (p4, w4, h4) = Scale2x(p2, w2, h2);
        var shaded = new uint[w4 * h4];
        for (int y = 0; y < h4; y++)
            for (int x = 0; x < w4; x++)
            {
                uint c = p4[y * w4 + x];
                if (c == 0)
                {
                    bool edge = false;
                    for (int k = 0; k < 9 && !edge; k++)
                    {
                        int qx = x + k % 3 - 1, qy = y + k / 3 - 1;
                        if (qx >= 0 && qy >= 0 && qx < w4 && qy < h4 && p4[qy * w4 + qx] != 0) edge = true;
                    }
                    if (edge) shaded[y * w4 + x] = 0x01101018;      // outline
                    continue;
                }
                float l = 1.18f - 0.38f * (y / (float)h4) - 0.12f * (x / (float)w4);
                int r = (int)Math.Min(255, (c >> 16 & 255) * l), g = (int)Math.Min(255, (c >> 8 & 255) * l), b = (int)Math.Min(255, (c & 255) * l);
                shaded[y * w4 + x] = 0x01000000u | (uint)r << 16 | (uint)g << 8 | (uint)b;
            }
        _portrait = shaded; _portW = w4; _portH = h4;
    }

    /// <summary>playtest 7: the Video > Debug info lines, from game RAM: area_id $6C / entrance $6D, player slot 1
    /// position ($71/$91, $B1/$D1), camera ($02-$05), main_state $40, sub_state $41, irq_mode $51, active object slots
    /// ($04A0); the widescreen and voice state</summary>
    public static List<string> DebugLines(Machine? mm = null, VideoSettings? vs = null)
    {
        var l = new List<string>();
        var m = mm ?? _m;
        var v = vs ?? _v;
        if (m == null || v == null || v.Debug.Count == 0) return l;
        if (v.Debug.Contains("area"))
        {
            var c = Wide.Cache;
            l.Add($"AREA {m.Rd(0x6C):X2}  ENTRANCE {m.Rd(0x6D):X2}" + (c != null ? $"  MAP {c.W}x{c.H}" : ""));
        }
        if (v.Debug.Contains("pos"))
            l.Add($"PLAYER {m.Rd(0x71) | m.Rd(0x91) << 8} {m.Rd(0xB1) | m.Rd(0xD1) << 8}  CAMERA {m.Rd(2) | m.Rd(3) << 8} {m.Rd(4) | m.Rd(5) << 8}");
        if (v.Debug.Contains("state"))
            l.Add($"MAIN {m.Rd(0x40):X2}  SUB {m.Rd(0x41):X2}  IRQ {m.Rd(0x51):X2}  FRAME {m.Frame}");
        if (v.Debug.Contains("objects"))
            l.Add($"OBJECTS {Enumerable.Range(0, 32).Count(s => m.Rd(0x4A0 + s) != 0)} ACTIVE  FLICKER FILL {SideSpr.Added.Count}");
        if (v.Debug.Contains("wide"))
            l.Add(v.Wide > 0 ? $"WIDE SHOW {Wide.Show:0.00}  LINES {Wide.LineOk.Count(o => o)}  OFFSET {Wide.MapX} {Wide.MapY}" + (Wide.FrameGood ? "  GOOD" : "") : "WIDE OFF");
        if (v.Debug.Contains("voice"))
        {
            int npc = Cast.Speaker(a => m.Rd(a));
            l.Add($"VOICE {(_neural != null ? _neural.Name : _speech != null ? "WINDOWS" : v.Voice ? "NOT STARTED" : "OFF")}"
                  + (npc >= 0 ? $"  NPC {npc:X2} {Cast.Of(npc).ToString().ToUpperInvariant()}" : ""));
        }
        if (v.Debug.Contains("hd"))
            l.Add(_gpu != null && HdEngine ? _gpu.DebugText : "HD OFF");
        if (v.Debug.Contains("sprites"))
        {
            // the game's sprite list $0200 (Y >= $EF = hidden), attribute b5 = behind the background
            int vis = 0, behind = 0;
            for (int k = 0; k < 64; k++) { if (m.Rd(0x200 + k * 4) >= 0xEF) continue; vis++; if ((m.Rd(0x202 + k * 4) & 0x20) != 0) behind++; }
            l.Add($"SPRITES {vis} OF 64  BEHIND {behind}  FLICKER FILL {SideSpr.Added.Count}");
        }
        if (v.Debug.Contains("input"))
            l.Add($"PAD {m.Rd(0x4F):X2}  NEW {m.Rd(0x4B):X2}  ACT {m.Rd(0x4D):X2}  DIR {m.Rd(0x49):X2}");
        if (v.Debug.Contains("audio") && _audio != null)
            l.Add($"AUDIO {(v.AudioOrchestra ? "ORCHESTRA" : v.AudioRemaster ? "REMASTER" : v.CdAudio ? "CD" : "ORIGINAL")}  UNDERRUNS {_audio.Underruns}  DROPPED {_audio.Dropped}" + (v.Ambience ? "  AMBIENCE" : ""));
        return l;
    }
    private static readonly FrameStats Stats = new();          // T49
    private static readonly AutoQuality Auto = new();          // T62
    // T61 key remap (Game > Controls > Remap keys): the button being asked for (-1 = not remapping) and the new keys
    private static int _remap = -1;
    private static int[] _remapNew = new int[8];
    private static void StartRemap() { _remap = 0; _remapNew = (int[])_v.KeyBinds.Clone(); _input.Blur(); Present(); }
    private static void RemapKey(int vk)
    {
        if (vk == Win32.VK_ESCAPE) { _remap = -1; Notice("REMAP CANCELLED"); Present(); return; }
        if (vk != 0x08) { for (int i = 0; i < 8; i++) if (_remapNew[i] == vk) _remapNew[i] = 0; _remapNew[_remap] = vk; }
        if (++_remap < 8) { Present(); return; }
        _remap = -1;
        _v.KeyBinds = _remapNew;
        _input.SetBinds(_v.KeyBinds);
        Notice("CONTROLS SAVED");
        SettingsChanged(false);
    }
    private static uint[] _crt = Array.Empty<uint>();
    private static int _crtFrame = -1;
    private static bool _help;                                 // T44: F1 help panel (the game pauses under it)

    private static void Status(string s)
    {
        if (_hwnd != IntPtr.Zero) Win32.SetWindowTextW(_hwnd, s.Length > 0 ? $"{Title} — {s}" : Title);
    }

    // js: main.js start
    public static int Run(WindowOptions o)
    {
        var data = Data.LoadDefault(o.DataDir);
        var m = new Machine(data, s => { if (!o.Quiet) Console.Error.WriteLine($"[re-c] {s}"); });
        // battery WRAM survives power-off: restore it before Reset, as the cartridge would (Save.cs)
        string savePath = o.SavePath ?? Save.DefaultPath();
        if (Save.LoadWram(savePath, m.Mem.Wram) && !o.Quiet) Console.Error.WriteLine($"[re-c] WRAM restored from {savePath}");
        m.Reset();
        // a loaded state brings its own WRAM: it only goes to a save file named with --save, never over re-c.sav
        bool saving = o.StatePath == null || o.SavePath != null;
        int frame0 = o.Frame0;
        if (o.StatePath != null)
        {
            var st = StateFile.Load(o.StatePath);
            m.LoadState(st);
            if (!o.Frame0Given) frame0 = st.Frame0 ?? st.Frame;
        }
        if (saving && o.SavePath == null) Save.Backup(savePath);   // T63: the last three saves as re-c.sav.1-.3
        var saver = saving ? new WramSaver(savePath, m.Mem.Wram) : null;
        var keys = Keys.Parse(o.KeySpecs);
        _scripted = o.KeySpecs.Count > 0;
        _automated = o.Frames >= 0;                      // test runs (--frames N): no dialogue voice process
        _pendingWarp = o.Warp;
        var rows = o.Compare != null ? new List<string> { string.Join(',', Csv.CompareColumns()) } : null;
        _muted = o.Mute;
        _m = m;
        _quiet = o.Quiet;
        _turbo = o.Turbo;
        _exporting = o.ExportTiles != null;
        _enh = new Enhancer(m.Ppu.Chr, data.NesRgb, record: _exporting);
        _nesRgb = data.NesRgb;
        _enh.LoadPack(o.PackDir ?? Path.Combine(Path.GetDirectoryName(Save.DefaultPath())!, "pack"));
        foreach (string e in _enh.PackErrors) Console.Error.WriteLine($"[re-c] graphics pack: {e}");
        _videoPath = o.VideoConfig ?? VideoSettings.DefaultPath();
        _v = VideoSettings.Load(_videoPath);
        _input.SetBinds(_v.KeyBinds);                   // T61
        if (o.ScaleGiven) _v.SetScale(o.Scale);
        EnsureTags();
        _audio = new AudioOut(m);
        _pad = new Pad();
        StartVoice();                                    // playtest 6: Piper loads its model while the title plays

        // NES colour index -> 32-bit BI_RGB pixel (B, G, R, x in memory = 0x00RRGGBB): palettes.json nesRgb as is
        _lutBase = data.NesRgb.Select(c => (uint)c & 0xFFFFFF).ToArray();
        ApplyColor();                                   // T60: brightness / colour / colour-blind assist -> _lut
        _bmi = new Win32.BITMAPINFOHEADER
        {
            Size = (uint)sizeof(Win32.BITMAPINFOHEADER), Width = 256, Height = -240, Planes = 1, BitCount = 32,
        };

        Win32.timeBeginPeriod(1);
        System.Runtime.GCSettings.LatencyMode = System.Runtime.GCLatencyMode.SustainedLowLatency;   // playtest fix: no blocking GCs mid-game
        int ranTotal = 0;
        try
        {
            if (!CreateMainWindow()) { Console.Error.WriteLine("ReC.exe: could not create the window"); return 1; }
            if (_v.Fullscreen || o.Fullscreen) { _v.Fullscreen = false; ToggleFullscreen(); }
            foreach (string c in o.MenuCmds) { Command(CommandId(c)); Pump(); }   // = clicking the menu items in order
            _osdReady = true;                          // T37: notices only for what the player does from here on
            // no autoplay policy on the desktop: audio starts right away (JS: on the first key / click); M = mute
            // T14: compile the Enhanced pipeline before the audio starts (its first frame would stall the sound)
            EnsureGpu();
            if (_v.Enhanced && NeedRender)
            {
                RenderHd();
                _hdFrame = -1;
                if (_gpu != null && Win32.GetClientRect(_hwnd, out var prc)) _gpu.Draw(prc.Right, prc.Bottom, Fit(prc.Right, prc.Bottom), _v.FirstLine, _v.VisibleH);
            }
            ApplyAudio();                              // T13: CD quality / stereo / reverb from the settings (device closed)
            bool audioOn = !o.NoAudio && _audio.Resume();
            _audio.SetMuted(_muted);
            if (_muted) Status("muted (M)");
            if (!o.Quiet)
                Console.Error.WriteLine($"[re-c] audio={(o.NoAudio ? "off" : audioOn ? $"waveOut {AudioOut.Rate} Hz {(_audio.Cd ? "stereo (CD quality)" : "mono")}" : "no device (silent)")} " +
                                        $"pad={_pad.Api} save={(saving ? savePath : "off (--load-state without --save)")}");

            // js: main.js blit
            void Blit()
            {
                _frameNo++;
                double t0 = Clock.Elapsed.TotalMilliseconds;
                // every line of a frame run with Tags / Capture set is recorded -> the enhanced / HD picture (+ --export-tiles)
                if (NeedRender) RenderHd();
                AmbienceTargets();                       // T54
                Compose();
                Present();
                if (!(HdEngine && _gpu != null) && _v.HdVsync && !_turbo && !_dwmOff)   // GDI: wait for the compositor's next frame
                    try { if (Win32.DwmFlush() < 0) _dwmOff = true; }
                    catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { _dwmOff = true; }
                Stats.Record(t0, Clock.Elapsed.TotalMilliseconds);
                if (_v.HdQuality == 3 && HdEngine && _gpu != null && !_turbo && !_paused)   // T62 HD quality: Auto
                {
                    int q = Auto.Update(Stats.Fps, Clock.Elapsed.TotalMilliseconds);
                    if (q != _gpu.Quality) { _gpu.Quality = q; Notice("HD QUALITY: " + new[] { "FAST", "BALANCED", "FULL" }[q] + " (AUTO)"); }
                }
            }

            double step = 1000.0 / Machine.FrameHz;
            Clock.Restart();
            _last = Clock.Elapsed.TotalMilliseconds;
            _acc = 0;
            int scriptMask = 0;

            // js: main.js tick
            // one pass of the host loop: run the frames due since the last pass, blit (+ audio, T5) if any ran
            void Tick(double now)
            {
                bool ran = false;
                if (!_scripted) PadExtras(now);                      // T46: RB = fast forward, LB = pause
                if (!_paused && !_hidden && (_remap < 0 || o.Frames >= 0))   // T61: the remap prompt pauses play (not a --frames run)
                {
                    _acc += Math.Min(now - _last, 250) * (_fast ? 4 : 1);   // never run more than ~15 frames to catch up; T36: Space = 4x
                    if (o.Turbo) _acc = Math.Max(_acc, step);
                    while (_acc >= step && !_quit)
                    {
                        int f = frame0 + m.Frame;
                        if (_scripted)
                        {
                            int want = keys.TryGetValue(f, out int b) ? b : 0;
                            PostScriptKeys(scriptMask, want);
                            scriptMask = want;
                            Pump();
                        }
                        int buttons = _input.Read() | (_scripted ? 0 : _pad.Read(now) | MouseButtons());
                        if (!_scripted) buttons = VideoSettings.Turbo(buttons, m.Frame, _v.TurboA, _v.TurboB);   // T38
                        if (_v.Wide > 0) Wide.BeforeFrame(m, data);
                        if (_v.Wide > 0 || HdEngine) SideSpr.Snapshot(m);   // objects + sprite list shown this frame   // widescreen: camera + objects of this frame
                        m.RunFrame(buttons); _acc -= step; ran = true; ranTotal++;
                        _v.Cheats.Apply(m, (buttons & 0x40) != 0);           // T36 cheats (B = bit 6)
                        if (!_scripted) Speak(m);                            // playtest 6: spoken dialogue
                        if (HdEngine) UpdatePortrait(); else _portrait = null;   // playtest 13: NPC portrait
                        if (_pendingWarp >= 0 && Warp.To(m, _pendingWarp)) _pendingWarp = -1;   // playtest 13: --warp
                        {
                            var ram = m.Mem.RamBytes;                        // T57 damage feedback: a life drop in SubstPlay
                            bool playing = ram[0x40] == 1 && ram[0x41] == 8;
                            if (Damage.Update(ram[0x03C1], ram[0x03C0], playing, m.Frame) && _v.Feedback && !_scripted)
                                _pad.Rumble(0.35 + 0.65 * Damage.LastHit, 0.25 + 0.5 * Damage.LastHit, 160 + 240 * Damage.LastHit, now);
                        }
                        rows?.Add(string.Join(',', Csv.CompareRow(m, f)));
                        if (saver != null && m.Frame % 300 == 0) saver.Poll();     // ~5 s
                        if (o.Frames >= 0 && ranTotal >= o.Frames) { _quit = true; break; }
                        if (o.Turbo) _acc = 0;
                    }
                    if (ran) { _audio.Speed = _fast ? 4 : 1; _audio.Flush(); Blit(); }   // playtest fix: audio first (rendering may be slow)
                }
                _last = now;
            }

            Status("");
            Blit();
            if (o.Frames == 0) _quit = true;
            while (!_quit)
            {
                Pump();
                if (_quit) break;
                Tick(Clock.Elapsed.TotalMilliseconds);
                if (!o.Turbo && !_quit)
                {
                    double wait = step - _acc - 1;            // sleep until near the next frame (1 ms timer)
                    Thread.Sleep(wait >= 1 ? (int)Math.Min(wait, step) : (_paused || _hidden ? 10 : 0));
                }
            }

            if (rows != null) File.WriteAllText(o.Compare!, string.Join('\n', rows) + "\n");
            if (o.Screen != null) Png.WriteFb(o.Screen, ShownFb, data.NesRgb, 1);
            if (o.OsdShot != null)
            {
                _osdReady = true;
                if (o.OsdShot == "pause") _paused = true;
                else if (o.OsdShot == "fast") _fast = true;
                else if (o.OsdShot == "help") { _help = true; _paused = true; }
                else if (o.OsdShot.StartsWith("notice=", StringComparison.Ordinal)) Notice(o.OsdShot[7..]);
            }
            if (o.DumpWindow != null && _hwnd != IntPtr.Zero) DumpWindow(o.DumpWindow);
            if (o.ExportTiles != null)
            {
                int n = _enh.Export(o.ExportTiles);
                if (!o.Quiet) Console.Error.WriteLine($"[re-c] exported {n} tile + palette combinations to {o.ExportTiles}");
            }
            if (!o.Quiet) Console.Error.WriteLine($"[re-c] frames={ranTotal} main_state={m.Rd(0x40)} sub_state={m.Rd(0x41)} " +
                                                  $"audio_underruns={_audio.Underruns} audio_dropped={_audio.Dropped}");
            return 0;
        }
        finally
        {
            RestoreDisplayMode();
            _audio.Close();
            _neural?.Dispose(); _speech?.Dispose();          // the Piper child process ends with the game
            if (saving) Save.SaveWram(savePath, m.Mem.Wram);   // always, on leaving (= 'pagehide')
            if (_hwnd != IntPtr.Zero) { Win32.DestroyWindow(_hwnd); _hwnd = IntPtr.Zero; }
            if (_menuBar != IntPtr.Zero && !MenuShown) Win32.DestroyMenu(_menuBar);   // an attached one goes with the window
            if (_popup != IntPtr.Zero) Win32.DestroyMenu(_popup);
            _menus.Clear(); _menuBar = _popup = IntPtr.Zero;
            Win32.timeEndPeriod(1);
        }
    }

    /// <summary>--keys: WM_KEYDOWN / WM_KEYUP for every button whose script state changed (Pad.cs ScriptKeys).</summary>
    private static void PostScriptKeys(int was, int want)
    {
        foreach (var (bit, vk) in Input.ScriptKeys)
        {
            if (((was ^ want) & bit) == 0) continue;
            Win32.PostMessageW(_hwnd, (want & bit) != 0 ? Win32.WM_KEYDOWN : Win32.WM_KEYUP, vk,
                (want & bit) != 0 ? 1 : unchecked((IntPtr)(long)0xC0000001));
        }
    }

    private static void Pump()
    {
        while (Win32.PeekMessageW(out var msg, IntPtr.Zero, 0, 0, Win32.PM_REMOVE))
        {
            if (msg.Message == Win32.WM_QUIT) { _quit = true; continue; }
            Win32.TranslateMessage(msg);
            Win32.DispatchMessageW(msg);
        }
    }

    private static bool CreateMainWindow()
    {
        IntPtr inst = Win32.GetModuleHandleW(null);
        fixed (char* cls = ClassName)
        {
            var wc = new Win32.WNDCLASSEXW
            {
                CbSize = (uint)sizeof(Win32.WNDCLASSEXW),
                Style = Win32.CS_HREDRAW | Win32.CS_VREDRAW | Win32.CS_OWNDC,
                WndProc = &WndProc,
                Instance = inst,
                // app.ico (ApplicationIcon): the compiler's Win32 icon group id 32512; 0 = Windows' default icon
                Icon = Win32.LoadImageW(inst, 32512, 1 /* IMAGE_ICON */, 0, 0, 0x40 /* LR_DEFAULTSIZE */),
                IconSm = Win32.LoadImageW(inst, 32512, 1, 16, 16, 0),
                Cursor = Win32.LoadCursorW(IntPtr.Zero, Win32.IDC_ARROW),
                Background = IntPtr.Zero,
                ClassName = cls,
            };
            if (Win32.RegisterClassExW(wc) == 0) return false;
        }
        var (cw, ch) = _v.ClientSize();
        var (w, h) = OuterSize(cw, ch);
        _hwnd = Win32.CreateWindowExW(0, ClassName, Title, Style, Win32.CW_USEDEFAULT, Win32.CW_USEDEFAULT, w, h,
            IntPtr.Zero, IntPtr.Zero, inst, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero) return false;
        BuildMenus();
        if (_v.MenuBar) Win32.SetMenu(_hwnd, _menuBar);
        // a 1x-6x window that does not fit the monitor's work area shrinks until it does (not saved)
        var mi = new Win32.MONITORINFO { CbSize = (uint)sizeof(Win32.MONITORINFO) };
        if (_v.WindowW == 0 && Win32.GetMonitorInfoW(Win32.MonitorFromWindow(_hwnd, Win32.MONITOR_DEFAULTTONEAREST), ref mi))
        {
            int want = _v.Scale;
            bool Fits(int s)
            {
                var (ow, oh) = OuterSize(_v.ClientSizeAt(s).W, _v.ClientSizeAt(s).H);
                return ow <= mi.Work.Right - mi.Work.Left && oh <= mi.Work.Bottom - mi.Work.Top;
            }
            while (_v.Scale > 1 && !Fits(_v.Scale)) _v.Scale--;
            ApplyWindowSize();
            _v.Scale = want;
        }
        else ApplyWindowSize();
        UpdateMenus();
        _hdc = Win32.GetDC(_hwnd);
        Win32.ShowWindow(_hwnd, Win32.SW_SHOW);
        return true;
    }

    // js: main.js fit
    /// <summary>where the picture goes in the client area with the current video settings (Video.cs Layout)</summary>
    public static (int X, int Y, int W, int H) Fit(int cw, int ch) => _v.Layout(cw, ch);

    private static void Present() => Paint(_hdc);

    private static void Paint(IntPtr hdc)
    {
        if (hdc == IntPtr.Zero || !Win32.GetClientRect(_hwnd, out var rc)) return;
        Paint(hdc, rc.Right, rc.Bottom);
    }

    private static void Paint(IntPtr hdc, int cw, int ch)
    {
        if (cw <= 0 || ch <= 0) return;
        bool osd = BuildOsd();
        if (_gpu != null && _v.Hd)                    // T18: Direct3D draws the whole client area (bars included)
        {
            _gpu.Overlay(osd);
            _gpu.Draw(cw, ch, Fit(cw, ch), _v.FirstLine, _v.VisibleH);
            return;
        }
        var (x, y, w, h) = Fit(cw, ch);
        IntPtr black = Win32.GetStockObject(Win32.BLACK_BRUSH);
        if (y > 0) Win32.FillRect(hdc, new Win32.RECT { Right = cw, Bottom = y }, black);
        if (y + h < ch) Win32.FillRect(hdc, new Win32.RECT { Top = y + h, Right = cw, Bottom = ch }, black);
        if (x > 0) Win32.FillRect(hdc, new Win32.RECT { Top = y, Right = x, Bottom = y + h }, black);
        if (x + w < cw) Win32.FillRect(hdc, new Win32.RECT { Left = x + w, Top = y, Right = cw, Bottom = y + h }, black);
        Win32.SetStretchBltMode(hdc, _v.Smooth ? Win32.HALFTONE : Win32.COLORONCOLOR);
        if (_v.Smooth) Win32.SetBrushOrgEx(hdc, 0, 0, IntPtr.Zero);
        // upscale filter (Upscale.cs) into Up, re-run only when the frame or the factor changed (WM_PAINT repaints)
        int k = Math.Clamp(_v.Upscale, 1, 4);
        uint[] src = Px;
        if (_v.Enhanced && _hdFrame == _frameNo)
        {
            src = _hd; k = Enhancer.S;
            if (!ColorAdjust.IsIdentity(_colorM))            // T60: the enhanced picture through the colour matrix
            {
                if (_hdCol.Length != _hd.Length) _hdCol = new uint[_hd.Length];
                if (_hdColFrame != _frameNo) { ColorAdjust.ApplyAll(_colorM, _hd, _hdCol, _hd.Length); _hdColFrame = _frameNo; }
                src = _hdCol;
            }
        }
        else if (_v.Crt)                               // T45: CRT look from the NES frame at 3x
        {
            if (_crt.Length != _pw * 240 * 9) { _crt = new uint[_pw * 240 * 9]; _crtFrame = -1; }
            if (_crtFrame != _frameNo) { Crt.Apply(Px, _pw, 240, _crt); _crtFrame = _frameNo; }
            src = _crt; k = 3;
        }
        else if (k > 1)
        {
            if (_up.Length != _pw * 240 * k * k) { _up = new uint[_pw * 240 * k * k]; _upFrame = -1; }
            if (_upFrame != _frameNo || _upK != k) { Upscale.Apply(Px, _pw, 240, k, _up, _upTmp); _upFrame = _frameNo; _upK = k; }
            src = _up;
        }
        // overscan crop: start the top-down DIB 8 lines in (no source-rectangle origin quirks)
        var bmi = _bmi;
        int sw = src == _hd || src == _hdCol ? 256 : _pw;    // the enhanced looks stay 256 wide
        bmi.Width = sw * k;
        bmi.Height = -_v.VisibleH * k;
        int first = _v.FirstLine * k * sw * k;
        if (osd)                                       // T37: the overlay blended into a copy of the visible picture
        {
            int n = sw * k * _v.VisibleH * k;
            if (_osdBuf.Length != n) _osdBuf = new uint[n];
            Array.Copy(src, first, _osdBuf, 0, n);
            Osd.Blend(_osdBuf, 256 * k, _v.VisibleH * k, (sw - 256) / 2 * k, sw * k);   // centred on the game's columns
            src = _osdBuf; first = 0;
        }
        fixed (uint* p = src)
            Win32.StretchDIBits(hdc, x, y, w, h, 0, 0, sw * k, _v.VisibleH * k, p + first, bmi,
                Win32.DIB_RGB_COLORS, Win32.SRCCOPY);
    }

    /// <summary>playtest 11: Game > Reset game (F8): like the console's Reset button. Machine.Reset clears RAM and restarts at
    /// the title; the battery WRAM (the save) is kept. The voice stops, the widescreen map is dropped</summary>
    /// <summary>the machine part of ResetGame (tests)</summary>
    public static void ResetMachineForTest(Machine m) => m.Reset();

    /// <summary>playtest 13: F3 / Video > Debug info > All on / off: any on -> all off (the choice kept); none on -> the kept
    /// choice back, or all of them</summary>
    public static void ToggleAllDebug(VideoSettings? vs = null)
    {
        var v = vs ?? _v;
        if (v.Debug.Count > 0) { v.DebugSaved = v.Debug.ToList(); v.Debug.Clear(); }
        else foreach (var k in v.DebugSaved.Count > 0 ? v.DebugSaved : VideoSettings.DebugNames.ToList()) v.Debug.Add(k);
    }

    public static bool ResetGame(bool ask)
    {
        if (_m == null) return false;
        if (ask && Win32.MessageBoxW(_hwnd, "Reset the game and go back to the title screen?\n\nYour save is kept; progress since the last save is lost.",
                "re-c", 0x21 /* OKCANCEL | QUESTION */) != 1) return false;
        _speech?.Stop(); _neural?.Stop();
        _dialogue.Update(0, 0, _ => 0);                    // closes the open page
        _m.Reset();
        Wide.Drop();
        _paused = false; _help = false; _acc = 0;
        Notice("GAME RESET");
        StatusLine(); Present();
        return true;
    }

    private static void KeyCommand(int vk)
    {
        switch (vk)
        {
            case 'P':
                _paused = !_paused; _help = false;
                _last = Clock.Elapsed.TotalMilliseconds; _acc = 0;
                StatusLine();
                Present();                             // T37: the pause screen at once
                break;
            case 'M':
                _muted = !_muted;
                _audio.SetMuted(_muted);
                Notice(_muted ? "SOUND OFF" : "SOUND ON");
                StatusLine();
                break;
            case Win32.VK_ESCAPE: Win32.DestroyWindow(_hwnd); break;
            case Win32.VK_F11: ToggleFullscreen(); break;
            case 0x78 /* F9 */: SetGraphics((GraphicsMode + 1) % 4); Notice("GRAPHICS: " + new[] { "ORIGINAL", "ENHANCED 16-BIT", "ENHANCED 32-BIT", "HD ENGINE" }[GraphicsMode]); break;
            case 0x74 /* F5 */: ReloadPack(); break;
            case 0x20 /* Space */: _fastKey = _fast = true; StatusLine(); if (_paused) Present(); break;
            case 0x7B /* F12 */: Screenshot(); break;                     // T42
            case 0x77 /* F8 */: Command(IdResetGame); break;              // playtest 11: reset game
            case 0x72 /* F3 */: Command(IdDebugAll); break;               // playtest 13: all debug info on / off
            case 0x71 /* F2 */: Command(IdFps); break;                    // T49
            case 0x70 /* F1 */:
                _help = !_help; _paused = _help;
                _last = Clock.Elapsed.TotalMilliseconds; _acc = 0;
                StatusLine(); Present(); break;
            case >= '1' and <= '6': _v.SetScale(vk - '0'); SettingsChanged(true); break;
        }
        UpdateMenus();
    }

    public static bool Muted => _muted;

    [UnmanagedCallersOnly]
    private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            switch (msg)
            {
                case Win32.WM_KEYDOWN:
                {
                    int vk = Input.Vk((int)wParam, (long)lParam);
                    bool repeat = ((long)lParam & 0x40000000) != 0;
                    if (_remap >= 0) { if (!repeat) RemapKey(vk); return IntPtr.Zero; }   // T61
                    _input.KeyDown(vk);
                    if (!repeat && !_input.IsBound(vk)) KeyCommand(vk);
                    return IntPtr.Zero;
                }
                case Win32.WM_KEYUP:
                    _input.KeyUp(Input.Vk((int)wParam, (long)lParam));
                    if ((int)wParam == 0x20 && _fastKey) { _fastKey = _fast = false; StatusLine(); }
                    return IntPtr.Zero;
                case Win32.WM_SYSKEYDOWN:
                    if ((int)wParam == Win32.VK_RETURN)
                    {
                        if (((long)lParam & 0x40000000) == 0) ToggleFullscreen();
                        _altUsed = true;
                        return IntPtr.Zero;
                    }
                    break;
                case Win32.WM_SYSKEYUP:
                    // Alt released after Alt+Enter: no menu-bar activation
                    if ((int)wParam == 0x12 /* VK_MENU */ && _altUsed) { _altUsed = false; return IntPtr.Zero; }
                    break;
                case Win32.WM_COMMAND:
                    Command(Win32.LoWord(wParam));
                    return IntPtr.Zero;
                case Win32.WM_MOUSEMOVE:
                    _mx = (short)((long)lParam & 0xFFFF); _my = (short)(((long)lParam >> 16) & 0xFFFF);
                    return IntPtr.Zero;
                case Win32.WM_LBUTTONDOWN:
                    _mx = (short)((long)lParam & 0xFFFF); _my = (short)(((long)lParam >> 16) & 0xFFFF);
                    _mL = true; Win32.SetCapture(hwnd);
                    return IntPtr.Zero;
                case Win32.WM_LBUTTONUP:
                    _mL = false; Win32.ReleaseCapture();
                    return IntPtr.Zero;
                case Win32.WM_MBUTTONDOWN: _mM = true; return IntPtr.Zero;
                case Win32.WM_MBUTTONUP: _mM = false; return IntPtr.Zero;
                case Win32.WM_RBUTTONDOWN:
                    if (_v.Mouse && Playing()) { _mR = true; return IntPtr.Zero; }   // mouse control: the sword in play
                    break;
                case Win32.WM_RBUTTONUP:
                    if (_mR) { _mR = false; return IntPtr.Zero; }
                    ContextMenu();
                    return IntPtr.Zero;
                case Win32.WM_EXITMENULOOP:
                case Win32.WM_EXITSIZEMOVE:
                    _last = Clock.Elapsed.TotalMilliseconds; _acc = 0;    // the game stood still in the modal loop
                    break;
                case Win32.WM_ACTIVATEAPP:
                    ActivateApp(wParam != IntPtr.Zero);
                    break;
                case Win32.WM_SYSCHAR:
                    if ((int)wParam == '\r') return IntPtr.Zero;     // no beep for Alt+Enter
                    break;
                case Win32.WM_KILLFOCUS:
                    if (!_scripted) { _input.Blur(); _mL = _mR = _mM = false; }   // = 'blur' (+ the mouse); a --keys run ignores focus changes
                    break;
                case Win32.WM_SIZE:
                    // = visibilitychange: a minimised window pauses; the clock restarts on return
                    _hidden = (int)wParam == Win32.SIZE_MINIMIZED;
                    _last = Clock.Elapsed.TotalMilliseconds; _acc = 0;
                    break;
                case Win32.WM_GETMINMAXINFO:
                {
                    var (w, h) = OuterSize(_v.ClientSizeAt(1).W, _v.ClientSizeAt(1).H);
                    var mmi = (Win32.MINMAXINFO*)lParam;
                    mmi->MinTrackSize = new Win32.POINT { X = w, Y = h };
                    return IntPtr.Zero;
                }
                case Win32.WM_ERASEBKGND:
                    return 1;
                case Win32.WM_PAINT:
                {
                    IntPtr dc = Win32.BeginPaint(hwnd, out var ps);
                    if (_hwnd != IntPtr.Zero) Paint(dc);
                    Win32.EndPaint(hwnd, ps);
                    return IntPtr.Zero;
                }
                case Win32.WM_DESTROY:
                    _quit = true;
                    Win32.PostQuitMessage(0);
                    return IntPtr.Zero;
            }
        }
        catch (Exception e)
        {
            // never let an exception unwind through the native frame
            Console.Error.WriteLine($"ReC.exe: {e}");
            _quit = true;
        }
        return Win32.DefWindowProcW(hwnd, msg, wParam, lParam);
    }
}
