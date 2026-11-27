// re-c Windows host — the HD engine's GPU stages (T18 renderer, rebuilt in T20: Video > Graphics > HD (GPU)). Not in
// the web port. Shaders: GpuShaders.cs.
//
// A Direct3D 11 renderer called through raw COM vtables (no packages): d3d11.dll D3D11CreateDeviceAndSwapChain and
// d3dcompiler_47.dll D3DCompile (the HLSL is compiled once at start). The picture is drawn at the window's own
// resolution (1080p, 4K ...).
//
// The engine does not use the PPU's picture. Its inputs per frame come from the game's video state:
//   layers  256x240 uint  SceneRaster's background and sprite layers kept apart (colour, opaque, behind, HUD), plus the
//                         dither partner of checkerboard-dithered background pixels (Layers below)
//   art     256x240 uint  graphics-pack tile under each pixel (atlas slot + position inside the tile + flips), per layer
//   info    256x240       HdScene: sprite lift, NES colour (-> material), flags sprite / void / UI, void mask
//   lines   240           world position of each line (scroll): detail and parallax stay fixed to the world
//   atlas   2048x2048     every graphics-pack tile (32x32 each), uploaded when the pack is (re)loaded
// Frame passes (once per game frame, full-screen triangles, no vertex buffers):
//   1. rounding: Scale2x on the layers' colour keys, background and sprites separately (a sprite's silhouette is
//      rounded on its own, not against the scenery behind it), 256x240 -> 512x480 -> 1024x960;
//   2. albedo 2048x1920 (8x): a third Scale2x step, then per pixel the graphics-pack tile (sampled inside its
//      32x32 tile) or the rounded colour, dithers blended, sprites composed over the background with the PPU's
//      priority rule; HUD lines and text stay exact NES pixels;
//   3. height: edges of the 2x colour keys, jump-flood distance to the nearest edge (steps 4, 2, 1), pillow profile,
//      3x3 blur (the shape "model" the light pass bumps and shadows);
//   4. light map: emissive colours (fire, lava, lamps) at 128x120, blurred wide: local light in dark places.
// Draw passes (output resolution):
//   5. light -> HDR: material detail, normals, warm sun + cool sky, specular, glints, AO, soft height-field shadows,
//      sprite contact shadows, cloud shadows, local lights, water reflections, chasm parallax, mist, dust motes,
//      fine relief from the 8x art, water refraction + grass in the wind, sun shafts outdoors (T21);
//   6. bloom (soft knee, 1/4 size + a wide 1/8 halo); 7. composite: tone shoulder, grade, scene tint (warm outdoors,
//      cool in the dark), vignette (HUD untouched); 8. FXAA-style edge anti-aliasing at screen resolution (HUD exact).
// Any failure (no Direct3D 11, compiler missing) -> the caller falls back to Enhanced (32-bit).
using System.Runtime.InteropServices;
using System.Text;

namespace ReC.Win;

internal sealed unsafe partial class Gpu : IDisposable
{
    // sizes follow the picture width NesW (256; wider with the widescreen playfield)
    public int NesW { get; private set; } = 256;
    private int _sideE;
    /// <summary>widescreen: black side pixels above this line are void (chasm look like the middle); 0 = none</summary>
    public int SideVoidTop;
    /// <summary>playtest 6: widescreen map behind the status bar (the HUD floats over it; set per frame by the window)</summary>
    public bool HudOverlay;
    private int AW => NesW * 8;                       // albedo: 8x
    public const int AH = 1920;
    private int KW => NesW * 2;                       // height field: 2x
    public const int KH = 480;
    private int LW => NesW / 2;                       // light map
    public const int LH = 120;
    public const int MaxW = 560;
    public const int AtlasN = 128, MaxSlots = AtlasN * AtlasN;  // T27: 128 x 128 pack tiles of 32x32 (4096x4096; fades)
    public string Info { get; private set; } = "";

    /// <summary>effect switches (Video > Graphics > HD effects)</summary>
    public bool Parallax = true, Shadows = true, Atmosphere = true, Detail = true, Reflections = true, Lights = true;
    /// <summary>T30: present in step with the monitor (sync interval 1: no tearing while the screen scrolls)</summary>
    public bool VSync;
    /// <summary>T32: overscan lines 232-239 + a masked left column drawn as exact black (Layers)</summary>
    public bool Edges = true;
    /// <summary>T34: HUD style: rounded glyphs (Scale2x inside the HUD), bevelled metallic text, soft shadows, brushed panel</summary>
    public bool HudStyle = true;
    /// <summary>T48: contrast-adaptive sharpening after the anti-aliasing (Cam.w = strength)</summary>
    public bool Sharpen = true;
    /// <summary>T51: cut-scenes play like film shots (CineBand: camera dolly + parallax, particles, lens streaks, grain)</summary>
    public bool Cinematic = true;
    /// <summary>playtest 6: false on the title / status / name-entry screens (their pictures are not cut-scenes: no dolly zoom)</summary>
    public bool CineAllowed = true;
    /// <summary>playtest 7: a remade story scene to paint over the picture band (0 = none; 1 = the intro's prehistoric jungle)</summary>
    public int StoryScene;
    /// <summary>playtest 12: a shop or inn interior is on screen (warm lamp light, wood grain, curtain folds, plaster)</summary>
    public bool Room;
    /// <summary>playtest 9: Video > Debug info > HD scene</summary>
    public string DebugText => FormattableString.Invariant($"HD PLAY {_play:0.00}  OUTDOOR {_outdoor:0.00}  STORY {StoryScene}/{(_storyOn ? 1 : 0)} {_storyFade:0.00}  FILM {(Cinematic && CineAllowed ? _cineW : 0):0.00}  Q {Quality}");
    private bool _storyOn;
    private float _storyFade, _storyMax;
    private (double Strength, double Gust, double Dir) _wind;
    private int _storyKey;
    private int _storyBand;

    /// <summary>the story picture: the lit lines above the story screen's raster split (the picture band detection skips
    /// split screens), at least 24 lines; (first line, last line + 1, first x, last x + 1)</summary>
    public static (int Y0, int Y1, int X0, int X1)? StoryBand(SceneRaster r, HdScene? sc = null)
    {
        var (from, _) = SplitGap(r);
        int end = from > 0 ? from : 232, y0 = -1, y1 = -1, x0 = 256, x1 = 0;
        for (int y = 0; y < end; y++)
        {
            int n = 0;
            for (int x = 0; x < 256; x++)
            {
                int i = y * 256 + x;
                if ((r.Fb[i] & 63) == 0x0F || (sc != null && (sc.Info[i * 4 + 2] & HdScene.FUi) != 0)) continue;   // text is not picture
                n++; x0 = Math.Min(x0, x); x1 = Math.Max(x1, x + 1);
            }
            if (n >= 8) { if (y0 < 0) y0 = y; y1 = y + 1; }
        }
        return y0 < 0 || y1 - y0 < 24 ? null : (y0, y1, x0, x1);
    }

    /// <summary>playtest 7: the intro story shows several pictures (the jungle, later a village under a purple sky); the
    /// jungle is the one of browns and greens (NES hues 7-A) with almost no blue / purple (hues 1-4)</summary>
    public static bool JunglePicture(SceneRaster r, (int Y0, int Y1, int X0, int X1) b)
    {
        int warm = 0, cool = 0;
        for (int y = b.Y0; y < b.Y1; y++)
            for (int x = b.X0; x < b.X1; x++)
            {
                int v = r.Fb[y * 256 + x] & 63, h = v & 15;
                if (v == 0x0F || h >= 0xD || h == 0) continue;
                if (h >= 7 && h <= 0xA) warm++; else if (h >= 1 && h <= 4) cool++;
            }
        return warm > 200 && cool * 4 < warm;
    }

    /// <summary>the remade scene follows the game's own fades: the brightness of the original picture (NES colour rows)
    /// against the picture at full light (at least 1.8)</summary>
    private void StoryUpdate(SceneRaster rl, HdScene sc)
    {
        if (StoryScene != _storyKey) { _storyKey = StoryScene; _storyMax = 0; }
        var found = StoryBand(rl, sc);
        _storyOn = StoryScene > 0 && found != null && JunglePicture(rl, found.Value);
        // the jungle fills the lines above the story screen's split: a fixed band, so the art never squeezes or slides
        int split = SplitGap(rl).From;
        _storyBand = _storyOn ? 0 * 256 + (split > 24 ? split : 192) : 0;
        if (!_storyOn) { _storyFade = 0; return; }
        var b = found!.Value;
        double sum = 0; int n = 0;
        for (int y = b.Y0; y < b.Y1; y++)
            for (int x = b.X0; x < b.X1; x++)
            {
                int v = rl.Fb[y * 256 + x] & 63;
                if (v == 0x0F || (v & 15) >= 0xD) continue;
                n++; sum += (v >> 4 & 3) + 1;
            }
        float lv = n > 0 ? (float)(sum / n) : 0;
        _storyMax = Math.Max(Math.Max(_storyMax, lv), 1.8f);
        float fr = Math.Clamp(lv / _storyMax, 0, 1);
        float u = Math.Clamp((fr - 0.2f) / 0.55f, 0, 1);
        _storyFade = u * u * (3 - 2 * u);                     // full light from 75 % of the brightest frame: only real fades dim it
    }
    /// <summary>T52: smooth outlines (Vote: the last 2x step of the albedo as a Gaussian key vote instead of Scale2x)</summary>
    public bool SmoothOutlines = true;
    /// <summary>T53: the bars beside / above the picture show a soft, dim, mirrored blur of it (PSAmbient) instead of black</summary>
    public bool Ambient = true;
    /// <summary>T55: tilt-shift depth of field in gameplay (Mat.w)</summary>
    public bool TiltShift = true;
    /// <summary>T59: text from the glyph atlas (SetGlyphs) instead of the game's 8x8 font</summary>
    public bool ModernFont = true;
    /// <summary>widescreen: the engine can draw a wide picture (W2); false = the window keeps HD at 256 columns</summary>
    public bool WideOk => true;
    /// <summary>T60: display colour matrix (ColorAdjust, row-major), applied in PSFinal</summary>
    public float[] ColorM = ColorAdjust.Matrix(2, false, 0);
    /// <summary>T59: the CHR data (font tiles' ink / paper values); null = no modern font</summary>
    public byte[]? Chr;
    private void* _glyTex, _glySrv;
    public bool HasGlyphs => _glySrv != null;

    /// <summary>T59: the glyph atlas (GlyphAtlas.Render) to the GPU</summary>
    public void SetGlyphs(uint[] atlas)
    {
        Release(ref _glySrv); Release(ref _glyTex);
        MakeTex(GlyphAtlas.W, GlyphAtlas.H, FmtBgra, out _glyTex, out _glySrv);
        fixed (uint* p = atlas) Update(_glyTex, p, GlyphAtlas.W * 4);
    }
    /// <summary>T57: damage feedback from the window (DamageFeedback): hit flash 0..1, low life</summary>
    public float Hurt;
    public bool LowLife;
    private float _cineW, _shotStart;
    private (int Y0, int Y1, int X0, int X1) _band;
    private int _cineHold;
    public const float SharpenK = 0.6f;
    /// <summary>T35: the player's HP (RAM $03C1) for the LIFE meter's fill (set before Upload)</summary>
    public int Hp;
    /// <summary>T37: the on-screen display (Osd.Pix) is shown in the final pass</summary>
    private bool _osdOn;
    private void* _oTex, _oSrv;
    public void Overlay(bool on)
    {
        _osdOn = on;
        if (on && _oTex != null) fixed (uint* p = Osd.Pix) Update(_oTex, p, Osd.W * 4);
    }
    private float[] _meters = new float[8];
    /// <summary>T31: HD quality 2 = full, 1 = balanced (lighting at 75 % resolution, 16 shadow steps), 0 = fast (50 %, 10
    /// steps, no reflections / fine relief): for slower (integrated) graphics</summary>
    public int Quality = 2;
    private int _lw, _lh;
    private void* _a4Tex, _a4Rtv, _a4Srv, _albCur;          // fast quality: the albedo at 4x
    private int _albScale = 8;

    // ---- tunables (cbuffer)
    private static readonly (float X, float Y, float Z) Light = Norm(-0.45f, -0.62f, 0.64f);
    private const float Bump = 1.8f, Amb = 0.50f, Kd = 0.50f, SpecK = 0.55f, LiftH = 0.8f,   // playtest 7/9: 2.2 cast a dark silhouette beside each character
        AoK = 0.45f, ShadowK = 0.60f, ShadowSlope = 0.16f, BloomThreshold = 0.95f, BloomK = 0.45f,
        Saturation = 1.08f, Contrast = 0.12f, Vignette = 0.22f, ReflectK = 0.30f, LightK = 2.2f, ContactK = 0.12f;

    private void* _dev, _ctx, _swap, _backRtv;
    // uploads
    private void* _layTex, _laySrv, _artTex, _artSrv, _iTex, _iSrv, _lnTex, _lnSrv, _mTex, _mSrv, _rgbTex, _rgbSrv, _atlTex, _atlSrv;
    // frame targets
    private void* _k1Tex, _k1Rtv, _k1Srv, _k2Tex, _k2Rtv, _k2Srv, _albTex, _albRtv, _albSrv;
    private void* _jaTex, _jaRtv, _jaSrv, _jbTex, _jbRtv, _jbSrv, _hTex, _hRtv, _hSrv, _l1Tex, _l1Rtv, _l1Srv, _l2Tex, _l2Rtv, _l2Srv;
    private void* _g1Tex, _g1Rtv, _g1Srv, _g2Tex, _g2Rtv, _g2Srv, _psBounce;   // T23 bounce light (colour bleeding)
    private float _exposure = 1;
    // draw targets
    private void* _hdrTex, _hdrRtv, _hdrSrv, _b1Tex, _b1Rtv, _b1Srv, _b2Tex, _b2Rtv, _b2Srv;
    private void* _b3Tex, _b3Rtv, _b3Srv, _b4Tex, _b4Rtv, _b4Srv, _ldrTex, _ldrRtv, _ldrSrv;
    private void* _vs, _psScale, _psAlbedo, _psSeed, _psJfa, _psHeight, _psEmit, _psLight, _psBright, _psBlur, _psComp, _psFinal, _psAmb, _sampLin, _cb;
    private int _bw, _bh, _pw, _ph;
    private float _camX, _camY, _time, _outdoor, _voidGate, _mr = 0.3f, _mg = 0.3f, _mb = 0.3f, _play, _icy, _hot, _hudFill = -1, _hudTop, _hudBottom;
    private readonly float[] _c = new float[NConst * 4];

    // pack art -> atlas slots
    private readonly Dictionary<long, int> _slots = new();
    private bool[] _tileArt = Array.Empty<bool>();
    private Dictionary<long, int> _fades = new();
    private int _atlasN = 64;
    public int Slots => _slots.Count;

    // CPU side of the uploads
    public readonly byte[] Lay = new byte[MaxW * 240 * 4];
    public readonly uint[] ArtMap = new uint[MaxW * 240 * 2];
    private readonly byte[] _wInfo = new byte[MaxW * 240 * 4];   // widescreen: HdScene.Info widened

    [LibraryImport("d3d11.dll")]
    private static partial int D3D11CreateDeviceAndSwapChain(void* adapter, int driverType, IntPtr software, uint flags,
        int* levels, uint nLevels, uint sdk, SwapDesc* desc, void** swap, void** dev, int* level, void** ctx);

    [LibraryImport("d3dcompiler_47.dll")]
    private static partial int D3DCompile(byte* src, nuint size, byte* name, void* defines, void* include, byte* entry,
        byte* target, uint flags1, uint flags2, void** code, void** errors);

    [StructLayout(LayoutKind.Sequential)]
    private struct SwapDesc
    {
        public uint Width, Height, RefNum, RefDen, Format, Scanline, Scaling;
        public uint SampleCount, SampleQuality, BufferUsage, BufferCount;
        public IntPtr OutputWindow;
        public int Windowed;
        public uint SwapEffect, Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TexDesc
    {
        public uint Width, Height, MipLevels, ArraySize, Format, SampleCount, SampleQuality, Usage, BindFlags, CpuAccess, Misc;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BufDesc { public uint ByteWidth, Usage, BindFlags, CpuAccess, Misc, Stride; }

    [StructLayout(LayoutKind.Sequential)]
    private struct SampDesc
    {
        public uint Filter, U, V, W;
        public float MipBias;
        public uint MaxAniso, Cmp;
        public float B0, B1, B2, B3, MinLod, MaxLod;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Viewport { public float X, Y, W, H, MinZ, MaxZ; }

    [StructLayout(LayoutKind.Sequential)]
    private struct Mapped { public void* Data; public uint RowPitch, DepthPitch; }

    private const uint FmtBgra = 87, FmtRgba8 = 28, FmtRgba8U = 30, FmtRg32U = 17, FmtRgba32F = 2, FmtRgba16F = 10;
    private const uint BindSrv = 8, BindRtv = 0x20;
    private const int NSrv = 13, NConst = 25;   // playtest 12: + Room
    private static readonly Guid IidTex2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");
    private static readonly Guid IidDxgiDevice1 = new("77db970f-6276-48ba-ba28-070143b4392c");

    private static void** Vt(void* o) => *(void***)o;

    private static void Release(ref void* o)
    {
        if (o == null) return;
        ((delegate* unmanaged[Stdcall]<void*, uint>)Vt(o)[2])(o);
        o = null;
    }

    private static void Check(int hr, string what)
    {
        if (hr < 0) throw new InvalidOperationException($"{what} failed (0x{hr:X8})");
    }

    private static (float, float, float) Norm(float x, float y, float z)
    {
        float l = MathF.Sqrt(x * x + y * y + z * z);
        return (x / l, y / l, z / l);
    }

    /// <summary>device + swap chain on hwnd, shaders compiled; null + error text when Direct3D 11 is not usable</summary>
    public static Gpu? TryCreate(IntPtr hwnd, int cw, int ch, int[] nesRgb, out string error)
    {
        var g = new Gpu();
        try
        {
            g.Init(hwnd, Math.Max(cw, 1), Math.Max(ch, 1), nesRgb);
            error = "";
            return g;
        }
        catch (Exception e) when (e is InvalidOperationException or DllNotFoundException or EntryPointNotFoundException)
        {
            g.Dispose();
            error = e.Message;
            return null;
        }
    }

    private void Init(IntPtr hwnd, int cw, int ch, int[] nesRgb)
    {
        var sd = new SwapDesc
        {
            Width = (uint)cw, Height = (uint)ch, Format = FmtBgra, SampleCount = 1, BufferUsage = 0x20 /* RENDER_TARGET_OUTPUT */,
            BufferCount = 1, OutputWindow = hwnd, Windowed = 1, SwapEffect = 0 /* DISCARD */,
        };
        int* levels = stackalloc int[] { 0xb000, 0xa100, 0xa000 };
        int level = 0;
        void* swap = null, dev = null, ctx = null;
        int hr = -1;
        string kind = "";
        foreach (var (type, name) in new[] { (1, "hardware"), (5, "WARP software") })
        {
            hr = D3D11CreateDeviceAndSwapChain(null, type, IntPtr.Zero, 0, levels, 3, 7, &sd, &swap, &dev, &level, &ctx);
            if (hr >= 0) { kind = name; break; }
        }
        Check(hr, "D3D11CreateDeviceAndSwapChain");
        _swap = swap; _dev = dev; _ctx = ctx;
        // playtest fix: at most 1 frame queued ahead of the screen (the default 3 = up to ~50 ms behind the game with VSync)
        Guid di = IidDxgiDevice1;
        void* dxgi = null;
        if (((delegate* unmanaged[Stdcall]<void*, Guid*, void**, int>)Vt(_dev)[0])(_dev, &di, &dxgi) >= 0 && dxgi != null)
        {
            ((delegate* unmanaged[Stdcall]<void*, uint, int>)Vt(dxgi)[12])(dxgi, 1);   // IDXGIDevice1::SetMaximumFrameLatency
            ((delegate* unmanaged[Stdcall]<void*, uint>)Vt(dxgi)[2])(dxgi);            // Release
        }
        Info = $"Direct3D 11 {kind}, feature level {level >> 12}.{level >> 8 & 15}";

        _vs = Compile("VS", "vs_4_0", vertex: true);
        _psScale = Compile("PSScale", "ps_4_0");
        _psAlbedo = Compile("PSAlbedo", "ps_4_0");
        _psSeed = Compile("PSSeed", "ps_4_0");
        _psJfa = Compile("PSJfa", "ps_4_0");
        _psHeight = Compile("PSHeight", "ps_4_0");
        _psEmit = Compile("PSEmit", "ps_4_0");
        _psBounce = Compile("PSBounce", "ps_4_0");
        _psLight = Compile("PSLight", "ps_4_0");
        _psBright = Compile("PSBright", "ps_4_0");
        _psBlur = Compile("PSBlur", "ps_4_0");
        _psComp = Compile("PSComp", "ps_4_0");
        _psFinal = Compile("PSFinal", "ps_4_0");
        _psAmb = Compile("PSAmbient", "ps_4_0");

        MakeTex(240, 1, FmtRgba32F, out _lnTex, out _lnSrv);
        MakeTex(64, 1, FmtRgba8, out _mTex, out _mSrv);
        MakeTex(64, 1, FmtRgba8, out _rgbTex, out _rgbSrv);
        MakeTex(64 * 32, 64 * 32, FmtBgra, out _atlTex, out _atlSrv);
        fixed (byte* m = HdScene.MaterialTable()) Update(_mTex, m, 64 * 4);
        var rgb = new byte[64 * 4];
        for (int i = 0; i < 64; i++)
        {
            int c = nesRgb[i];
            rgb[i * 4] = (byte)(c >> 16); rgb[i * 4 + 1] = (byte)(c >> 8); rgb[i * 4 + 2] = (byte)c; rgb[i * 4 + 3] = 255;
        }
        fixed (byte* p = rgb) Update(_rgbTex, p, 64 * 4);

        MakeWidthResources();
        MakeTex(Osd.W, Osd.H, FmtRgba8, out _oTex, out _oSrv);

        var samp = new SampDesc { Filter = 0x15 /* MIN_MAG_MIP_LINEAR */, U = 3, V = 3, W = 3, MaxAniso = 1, MaxLod = float.MaxValue };
        void* s;
        Check(((delegate* unmanaged[Stdcall]<void*, SampDesc*, void*, int>)Vt(_dev)[23])(_dev, &samp, &s), "CreateSamplerState");
        _sampLin = s;
        var bd = new BufDesc { ByteWidth = NConst * 16, Usage = 2 /* DYNAMIC */, BindFlags = 4 /* CONSTANT */, CpuAccess = 0x10000 };
        void* b;
        Check(((delegate* unmanaged[Stdcall]<void*, BufDesc*, void*, void**, int>)Vt(_dev)[3])(_dev, &bd, null, &b), "CreateBuffer");
        _cb = b;
        Resize(cw, ch);
    }

    /// <summary>the per-frame inputs and targets whose size follows the picture width</summary>
    private void MakeWidthResources()
    {
        MakeTex(NesW, 240, FmtRgba8U, out _layTex, out _laySrv);
        MakeTex(NesW, 240, FmtRg32U, out _artTex, out _artSrv);
        MakeTex(NesW, 240, FmtRgba8, out _iTex, out _iSrv);
        MakeTarget(KW, KH, FmtRgba8U, out _k1Tex, out _k1Rtv, out _k1Srv);
        MakeTarget(KW * 2, KH * 2, FmtRgba8U, out _k2Tex, out _k2Rtv, out _k2Srv);
        MakeTarget(AW, AH, FmtRgba8, out _albTex, out _albRtv, out _albSrv);
        MakeTarget(AW / 2, AH / 2, FmtRgba8, out _a4Tex, out _a4Rtv, out _a4Srv);
        MakeTarget(KW, KH, FmtRgba16F, out _jaTex, out _jaRtv, out _jaSrv);
        MakeTarget(KW, KH, FmtRgba16F, out _jbTex, out _jbRtv, out _jbSrv);
        MakeTarget(KW, KH, FmtRgba16F, out _hTex, out _hRtv, out _hSrv);
        MakeTarget(LW, LH, FmtRgba16F, out _l1Tex, out _l1Rtv, out _l1Srv);
        MakeTarget(LW, LH, FmtRgba16F, out _l2Tex, out _l2Rtv, out _l2Srv);
        MakeTarget(LW, LH, FmtRgba16F, out _g1Tex, out _g1Rtv, out _g1Srv);
        MakeTarget(LW, LH, FmtRgba16F, out _g2Tex, out _g2Rtv, out _g2Srv);
    }

    private void ReleaseWidthResources()
    {
        Unbind();
        Release(ref _laySrv); Release(ref _layTex); Release(ref _artSrv); Release(ref _artTex); Release(ref _iSrv); Release(ref _iTex);
        Release(ref _k1Srv); Release(ref _k1Rtv); Release(ref _k1Tex);
        Release(ref _k2Srv); Release(ref _k2Rtv); Release(ref _k2Tex);
        Release(ref _albSrv); Release(ref _albRtv); Release(ref _albTex);
        Release(ref _a4Srv); Release(ref _a4Rtv); Release(ref _a4Tex);
        Release(ref _jaSrv); Release(ref _jaRtv); Release(ref _jaTex);
        Release(ref _jbSrv); Release(ref _jbRtv); Release(ref _jbTex);
        Release(ref _hSrv); Release(ref _hRtv); Release(ref _hTex);
        Release(ref _l1Srv); Release(ref _l1Rtv); Release(ref _l1Tex);
        Release(ref _l2Srv); Release(ref _l2Rtv); Release(ref _l2Tex);
        Release(ref _g1Srv); Release(ref _g1Rtv); Release(ref _g1Tex); Release(ref _g2Srv); Release(ref _g2Rtv); Release(ref _g2Tex);
    }

    /// <summary>widescreen: rebuild the width-dependent resources for w columns</summary>
    public void SetWidth(int w)
    {
        w = Math.Clamp(w, 256, MaxW);
        if (w == NesW) return;
        ReleaseWidthResources();
        NesW = w;
        MakeWidthResources();
        _albCur = null;
    }

    private void Update(void* tex, void* data, int pitch) =>
        ((delegate* unmanaged[Stdcall]<void*, void*, uint, void*, void*, uint, uint, void>)Vt(_ctx)[48])(_ctx, tex, 0, null, data, (uint)pitch, 0);

    private void* Compile(string entry, string target, bool vertex = false)
    {
        byte[] src = Encoding.ASCII.GetBytes(Hlsl);
        byte[] e = Encoding.ASCII.GetBytes(entry + "\0"), t = Encoding.ASCII.GetBytes(target + "\0"), n = Encoding.ASCII.GetBytes("re-c-hd\0");
        void* blob = null, err = null;
        int hr;
        fixed (byte* ps = src, pe = e, pt = t, pn = n)
            hr = D3DCompile(ps, (nuint)src.Length, pn, null, null, pe, pt, 1u << 15 /* OPTIMIZATION_LEVEL3 */, 0, &blob, &err);
        if (hr < 0)
        {
            string msg = err != null
                ? Marshal.PtrToStringAnsi((IntPtr)((delegate* unmanaged[Stdcall]<void*, void*>)Vt(err)[3])(err)) ?? ""
                : "";
            Release(ref err);
            throw new InvalidOperationException($"HLSL {entry}: {msg.Trim()} (0x{hr:X8})");
        }
        Release(ref err);
        void* code = ((delegate* unmanaged[Stdcall]<void*, void*>)Vt(blob)[3])(blob);
        nuint len = ((delegate* unmanaged[Stdcall]<void*, nuint>)Vt(blob)[4])(blob);
        void* sh;
        int slot = vertex ? 12 : 15;                   // CreateVertexShader / CreatePixelShader
        hr = ((delegate* unmanaged[Stdcall]<void*, void*, nuint, void*, void**, int>)Vt(_dev)[slot])(_dev, code, len, null, &sh);
        Release(ref blob);
        Check(hr, $"Create shader {entry}");
        return sh;
    }

    private void MakeTex(int w, int h, uint fmt, out void* texOut, out void* srvOut)
    {
        var td = new TexDesc { Width = (uint)w, Height = (uint)h, MipLevels = 1, ArraySize = 1, Format = fmt, SampleCount = 1, BindFlags = BindSrv };
        void* tex, srv;
        Check(((delegate* unmanaged[Stdcall]<void*, TexDesc*, void*, void**, int>)Vt(_dev)[5])(_dev, &td, null, &tex), "CreateTexture2D");
        Check(((delegate* unmanaged[Stdcall]<void*, void*, void*, void**, int>)Vt(_dev)[7])(_dev, tex, null, &srv), "CreateShaderResourceView");
        texOut = tex; srvOut = srv;
    }

    private void MakeTarget(int w, int h, uint fmt, out void* texOut, out void* rtvOut, out void* srvOut)
    {
        var td = new TexDesc { Width = (uint)w, Height = (uint)h, MipLevels = 1, ArraySize = 1, Format = fmt, SampleCount = 1, BindFlags = BindSrv | BindRtv };
        void* tex, rtv, srv;
        Check(((delegate* unmanaged[Stdcall]<void*, TexDesc*, void*, void**, int>)Vt(_dev)[5])(_dev, &td, null, &tex), "CreateTexture2D (target)");
        Check(((delegate* unmanaged[Stdcall]<void*, void*, void*, void**, int>)Vt(_dev)[9])(_dev, tex, null, &rtv), "CreateRenderTargetView");
        Check(((delegate* unmanaged[Stdcall]<void*, void*, void*, void**, int>)Vt(_dev)[7])(_dev, tex, null, &srv), "CreateShaderResourceView (target)");
        texOut = tex; rtvOut = rtv; srvOut = srv;
    }

    /// <summary>back buffer = client size (ResizeBuffers on change)</summary>
    private void Resize(int cw, int ch)
    {
        if (cw == _bw && ch == _bh && _backRtv != null) return;
        Unbind();
        Release(ref _backRtv);
        if (_bw != 0)
            Check(((delegate* unmanaged[Stdcall]<void*, uint, uint, uint, uint, uint, int>)Vt(_swap)[13])(_swap, 0, (uint)cw, (uint)ch, 0, 0), "ResizeBuffers");
        _bw = cw; _bh = ch;
        void* back, rtv;
        Guid iid = IidTex2D;
        Check(((delegate* unmanaged[Stdcall]<void*, uint, Guid*, void**, int>)Vt(_swap)[9])(_swap, 0, &iid, &back), "GetBuffer");
        int hr = ((delegate* unmanaged[Stdcall]<void*, void*, void*, void**, int>)Vt(_dev)[9])(_dev, back, null, &rtv);
        Release(ref back);
        Check(hr, "CreateRenderTargetView (back buffer)");
        _backRtv = rtv;
    }

    /// <summary>HDR + bloom targets = picture size</summary>
    private void Targets(int pw, int ph)
    {
        float sc = Quality >= 2 ? 1f : Quality == 1 ? 0.75f : 0.5f;
        int lw = Math.Max((int)(pw * sc), 1), lh = Math.Max((int)(ph * sc), 1);
        if (pw == _pw && ph == _ph && lw == _lw && lh == _lh && _hdrTex != null) return;
        Unbind();
        Release(ref _hdrSrv); Release(ref _hdrRtv); Release(ref _hdrTex);
        Release(ref _b1Srv); Release(ref _b1Rtv); Release(ref _b1Tex);
        Release(ref _b2Srv); Release(ref _b2Rtv); Release(ref _b2Tex);
        Release(ref _b3Srv); Release(ref _b3Rtv); Release(ref _b3Tex); Release(ref _b4Srv); Release(ref _b4Rtv); Release(ref _b4Tex);
        Release(ref _ldrSrv); Release(ref _ldrRtv); Release(ref _ldrTex);
        _pw = pw; _ph = ph; _lw = lw; _lh = lh;
        MakeTarget(lw, lh, FmtRgba16F, out _hdrTex, out _hdrRtv, out _hdrSrv);       // lighting (scaled by quality)
        MakeTarget(Math.Max(lw / 4, 1), Math.Max(lh / 4, 1), FmtRgba16F, out _b1Tex, out _b1Rtv, out _b1Srv);
        MakeTarget(Math.Max(lw / 4, 1), Math.Max(lh / 4, 1), FmtRgba16F, out _b2Tex, out _b2Rtv, out _b2Srv);
        MakeTarget(Math.Max(lw / 8, 1), Math.Max(lh / 8, 1), FmtRgba16F, out _b3Tex, out _b3Rtv, out _b3Srv);
        MakeTarget(Math.Max(lw / 8, 1), Math.Max(lh / 8, 1), FmtRgba16F, out _b4Tex, out _b4Rtv, out _b4Srv);
        MakeTarget(pw, ph, FmtRgba8, out _ldrTex, out _ldrRtv, out _ldrSrv);
    }

    // ---- graphics pack -> atlas
    /// <summary>(re)build the atlas from the pack tiles (key = Enhancer.ArtKey); returns how many did not fit (> MaxSlots)</summary>
    public int SetArt(IReadOnlyDictionary<long, uint[]> art, int nTiles)
    {
        _tileArt = BuildSlots(art, nTiles, _slots);
        _fades = BuildFades(_slots);
        int n = _slots.Count <= 64 * 64 ? 64 : AtlasN;          // the atlas grows to 4096x4096 only when needed
        if (n != _atlasN)
        {
            Unbind();
            Release(ref _atlSrv); Release(ref _atlTex);
            MakeTex(n * 32, n * 32, FmtBgra, out _atlTex, out _atlSrv);
            _atlasN = n;
        }
        var px = new uint[n * 32 * n * 32];
        foreach (var (key, slot) in _slots)
        {
            var img = art[key];
            int ox = slot % n * 32, oy = slot / n * 32;
            for (int y = 0; y < 32; y++) Array.Copy(img, y * 32, px, (oy + y) * n * 32 + ox, 32);
        }
        fixed (uint* p = px) Update(_atlTex, p, n * 32 * 4);
        return art.Count - _slots.Count;
    }

    /// <summary>T27: palette fade steps (the game fades by $10 per step, BuiltinArt.Fade): faded key -> slot | step << 16,
    /// for every tile whose faded colours have no tile of their own (and are not all black)</summary>
    public static Dictionary<long, int> BuildFades(IReadOnlyDictionary<long, int> slots)
    {
        var fades = new Dictionary<long, int>();
        for (int k = 1; k <= 3; k++)                              // the nearest painting (smallest step) wins
            foreach (var (key, slot) in slots.OrderBy(kv => kv.Key))
            {
                int tile = (int)(key >> 24), c1 = (int)(key >> 16 & 63), c2 = (int)(key >> 8 & 63), c3 = (int)(key & 63);
                int f1 = BuiltinArt.Fade(c1, k), f2 = BuiltinArt.Fade(c2, k), f3 = BuiltinArt.Fade(c3, k);
                if (f1 == 0x0F && f2 == 0x0F && f3 == 0x0F) continue;
                long fk = Enhancer.ArtKey(tile, f1, f2, f3);
                if (!slots.ContainsKey(fk)) fades.TryAdd(fk, slot | k << 16);
            }
        return fades;
    }

    /// <summary>atlas slot of every pack tile (sorted by key, at most MaxSlots) + which CHR tiles have any</summary>
    public static bool[] BuildSlots(IReadOnlyDictionary<long, uint[]> art, int nTiles, Dictionary<long, int> slots)
    {
        slots.Clear();
        var tileArt = new bool[nTiles];
        foreach (long key in art.Keys.OrderBy(k => k))
        {
            if (slots.Count >= MaxSlots) break;
            int tile = (int)(key >> 24);
            if (tile >= nTiles) continue;
            slots[key] = slots.Count;
            tileArt[tile] = true;
        }
        return tileArt;
    }

    /// <summary>the layer + art words of one frame (CPU side of the uploads, pure: tested on Linux).
    /// Lay per pixel: r = background colour | 0x40 opaque | 0x80 HUD / text, g = sprite colour | 0x40 present |
    /// 0x80 behind the background, b = dither partner colour + 1 (0 = none), a = 0.
    /// ArtMap per pixel: [background, sprite] = atlas slot + 1 (15 bits) | col << 15 | row << 18 | flipH << 21 | flipV << 22 |
    /// fade step << 23 (0 = none)</summary>
    /// <summary>widescreen: HdScene.Info (256 wide) into a W-wide copy; the sides: no lift, the colour, no flags, no void</summary>
    /// (void: HdScene's rule on the sides above voidTop, near-black colours opened 3x3, so chasms continue)</summary>
    // WidenInfo's work buffers, reused (a new array per frame was ~200 KB on the large-object heap = blocking full GCs)
    [ThreadStatic] private static byte[]? _wBlk, _wTmp, _wEro, _wDil;

    public static void WidenInfo(byte[] info, SceneRaster r, int e, byte[] dst, int voidTop = 240, SceneRaster? rl = null, HdScene? sc = null)
    {
        // (seam fix) one void mask over the whole width with HdScene's own rule, so it does not change at the old edge:
        // near-black colours or the dark backdrop, not sprites, not text lines; the sides only above voidTop and inside
        // the area (a tile there). Pixels the widening changed (the game's masked left column, filled from the map) get
        // the side info too: their colour, no lift, no flags. Opening = separable 3x3 min then max, edges clamped.
        int w = r.W, n = w * 240, bd = sc?.DarkBackdrop ?? -1;
        if (_wBlk == null || _wBlk.Length < n) { _wBlk = new byte[n]; _wTmp = new byte[n]; _wEro = new byte[n]; _wDil = new byte[n]; }
        byte[] blk = _wBlk, tmp = _wTmp!, ero = _wEro!, dil = _wDil!;
        var fb = r.Fb; var bgTag = r.BgTag; var tags = r.Tags;
        for (int y = 0; y < 240; y++)
        {
            bool uiLine = sc != null && sc.Lines[y * 4 + 2] != 0, sideOk = y < voidTop;
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                int i = row + x, c = fb[i] & 63, tag = tags[i];
                bool side = x < e || x >= e + 256 || r.Under[i];       // map behind the HUD counts as side
                bool ok = side ? sideOk && (bgTag[i] & ReC.Nes.Ppu.TagValid) != 0 : !uiLine;
                bool spr = (tag & ReC.Nes.Ppu.TagValid) != 0 && (tag >> 19 & 7) >= 4;
                blk[i] = (byte)(ok && !spr && ((c & 15) >= 0xD && c != 0x2D && c != 0x3D || c == bd) ? 1 : 0);
            }
        }
        Pass3(blk, tmp, ero, w, true);
        Pass3(ero, tmp, dil, w, false);
        for (int y = 0; y < 240; y++)
        {
            int row = y * w;
            // the sides: colour, no lift, no flags; the middle: the scene info as it is
            for (int x = 0; x < e; x++) { int o = (row + x) * 4; dst[o] = 0; dst[o + 1] = (byte)((fb[row + x] & 63) * 4); dst[o + 2] = (byte)(r.NoMap[row + x] ? HdScene.FNoMap : 0); }
            for (int x = e + 256; x < w; x++) { int o = (row + x) * 4; dst[o] = 0; dst[o + 1] = (byte)((fb[row + x] & 63) * 4); dst[o + 2] = (byte)(r.NoMap[row + x] ? HdScene.FNoMap : 0); }
            Buffer.BlockCopy(info, y * 1024, dst, (row + e) * 4, 1024);
            for (int x = e; x < e + 256; x++)                 // map behind the HUD: world pixels (colour, no lift, no flags)
                if (r.Under[row + x]) { int o = (row + x) * 4; dst[o] = 0; dst[o + 1] = (byte)((fb[row + x] & 63) * 4); dst[o + 2] = 0; }
            if (rl != null)                                   // the widening only changes the masked left column
                for (int x = e; x < e + 8; x++)
                    if (fb[row + x] != rl.Fb[y * 256 + x - e]) { int o = (row + x) * 4; dst[o] = 0; dst[o + 1] = (byte)((fb[row + x] & 63) * 4); dst[o + 2] = 0; }
            for (int x = 0; x < w; x++)
            {
                int i = row + x, o = i * 4;
                bool v = blk[i] != 0 && dil[i] != 0;
                dst[o + 2] = (byte)((dst[o + 2] & ~HdScene.FVoid) | (v ? HdScene.FVoid : 0));
                dst[o + 3] = (byte)(v ? 255 : 0);
            }
        }
    }

    /// <summary>playtest 6: a pixel the widening filled with map in a row clean edges would paint black (the HUD seam, the rows
    /// under the panel): the map behind the HUD, kept</summary>
    private static bool MapUnderHud(SceneRaster r, int i, int x, int e, int gameW) =>
        r.Under[i] || ((x < e || x >= e + gameW) && (r.BgTag[i] & ReC.Nes.Ppu.TagValid) != 0);

    /// <summary>3x3 min (and) or max (or) of a 0/1 image, edges clamped: rows into tmp, then columns into dst</summary>
    private static void Pass3(byte[] src, byte[] tmp, byte[] dst, int w, bool and)
    {
        for (int y = 0; y < 240; y++)
        {
            int r = y * w;
            for (int x = 0; x < w; x++)
            {
                int a = src[r + (x > 0 ? x - 1 : 0)], b = src[r + x], c = src[r + (x < w - 1 ? x + 1 : x)];
                tmp[r + x] = (byte)(and ? a & b & c : a | b | c);
            }
        }
        for (int y = 0; y < 240; y++)
        {
            int r = y * w, u = (y > 0 ? y - 1 : 0) * w, d = (y < 239 ? y + 1 : 239) * w;
            for (int x = 0; x < w; x++)
                dst[r + x] = (byte)(and ? tmp[u + x] & tmp[r + x] & tmp[d + x] : tmp[u + x] | tmp[r + x] | tmp[d + x]);
        }
    }

    public static void Layers(SceneRaster r, HdScene sc, IReadOnlyDictionary<long, int> slots, IReadOnlyDictionary<long, int> fades,
        bool[] tileArt, byte[] lay, uint[] artMap, bool edges = false, byte[]? fontChr = null, byte[]? info = null, SceneRaster? rl = null)
    {
        var fb = r.Fb;
        info ??= sc.Info;
        rl ??= r;
        int W = r.W, e = (W - rl.W) / 2;
        // T59: ink / paper pixel values of the text font's glyph tiles (FontInk), when the modern font is on
        var ink = new (int Ink, int Paper)[0x80];
        if (fontChr != null) for (int c = 0; c < 0x80; c++) ink[c] = FontInk.Values(fontChr, FontInk.First + c);
        int band = edges ? EdgeBand(rl, sc) : 240;
        var (gap0, gap1) = edges ? SplitGap(rl) : (0, 0);
        var menuRows = edges ? MenuRows(rl, sc) : null;
        for (int y = 0; y < 240; y++)
        {
            // T32 clean edges: the lines a TV hid (232-239, where the game leaves stray tiles) and the left column the game
            // masks (backdrop colour), the backdrop rows under a bottom HUD panel and the HUD split seam (SplitGap) are exact black: HUD bit, no sprite, no art
            int ex = !edges ? 0 : y >= band || (y >= gap0 && y < gap1) || (menuRows != null && menuRows[y]) ? W : r.LeftClip[y] ? e + 8 : 0;
            for (int x = 0; x < ex; x++)
            {
                int i = y * W + x, o = i * 4;
                if (MapUnderHud(r, i, x, e, rl.W)) continue;   // playtest 6: map behind the HUD (drawn by the loop below)
                lay[o] = 0x0F | 0x80; lay[o + 1] = 0; lay[o + 2] = 0; lay[o + 3] = 0;
                artMap[i * 2] = 0; artMap[i * 2 + 1] = 0;
            }
            var pal = r.LinePal[y];
            // the slot / step part of an art word depends only on tile + palette group (+ the line's palette): cached per
            // run of equal keys (tiles repeat for 8 pixels); the pixel's column / row / flips are added per pixel
            int bk = -1, sk = -1;
            uint bw = 0, sw = 0;
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x, o = i * 4;
                if (x < ex && !MapUnderHud(r, i, x, e, rl.W)) continue;   // painted black by clean edges above
                bool ui = (info[o + 2] & HdScene.FUi) != 0;
                int sp = r.Sp[i];
                int bgc = r.BgCol[i] & 63, partner = 0;
                // checkerboard dither: the 4 neighbours share one other colour and >= 2 diagonals have this one. Both
                // cells of a dither get the same key (the lower colour + the higher one as partner), so the rounding
                // sees one flat area (no diagonal stripes) and the albedo shows the 50/50 mix
                if (x > 0 && y > 0 && x < W - 1 && y < 239 && !ui && sp == 0)
                {
                    int p = fb[i], q = fb[i - 1];
                    if (q != p && fb[i + 1] == q && fb[i - W] == q && fb[i + W] == q &&
                        (fb[i - W - 1] == p ? 1 : 0) + (fb[i - W + 1] == p ? 1 : 0) + (fb[i + W - 1] == p ? 1 : 0) + (fb[i + W + 1] == p ? 1 : 0) >= 2)
                    {
                        bgc = Math.Min(p & 63, q & 63);
                        partner = Math.Max(p & 63, q & 63) + 1;
                    }
                }
                lay[o] = (byte)(bgc | (r.Bg[i] != 0 ? 0x40 : 0) | (ui ? 0x80 : 0));
                lay[o + 1] = (byte)(sp != 0 ? r.SpCol[i] & 63 | 0x40 | (sp & 0x40) << 1 : 0);
                lay[o + 2] = (byte)partner;
                lay[o + 3] = 0;
                int bt = r.BgTag[i], bkey = bt & (ReC.Nes.Ppu.TagValid | 7 << 19 | 0x1FFF);
                if (bkey != bk) { bk = bkey; bw = ArtWord(bkey, pal, slots, fades, tileArt); }
                artMap[i * 2] = bw == 0 ? 0 : bw | PosBits(bt);
                if (fontChr != null && ui && (bt & ReC.Nes.Ppu.TagValid) != 0 && FontInk.Code(bt & 0x1FFF) is int code and >= 0 && ink[code].Ink >= 0)
                {
                    // T59 glyph word: bit 31, ASCII code, column / row in the tile, ink / paper colours
                    int g = bt >> 19 & 7, pb = g < 4 ? g * 4 : 0x10 + (g - 4) * 4;
                    int Col(int v) => (v == 0 ? pal[0] : pal[pb + v]) & 63;
                    artMap[i * 2] = 1u << 31 | (uint)code | (uint)(bt >> 13 & 7) << 7 | (uint)(bt >> 16 & 7) << 10 |
                                    (uint)Col(ink[code].Ink) << 13 | (uint)Col(ink[code].Paper) << 19;
                }
                if (sp != 0)
                {
                    int st = r.SpTag[i], skey = st & (ReC.Nes.Ppu.TagValid | 7 << 19 | 0x1FFF);
                    if (skey != sk) { sk = skey; sw = ArtWord(skey, pal, slots, fades, tileArt); }
                    artMap[i * 2 + 1] = sw == 0 ? 0 : sw | PosBits(st);
                }
                else artMap[i * 2 + 1] = 0;
            }
        }
    }

    /// <summary>T32: first line of the black bottom band: 232 (overscan), or higher over the plain backdrop rows (one colour =
    /// the line's backdrop) right above it when a HUD panel sits at the bottom (the strip under the panel)</summary>
    public static int EdgeBand(SceneRaster r, HdScene sc)
    {
        int band = 232, hudTop = SplitGap(r).To;              // T39: the status bar's first line (fixed scroll)
        if (hudTop < 160) return band;
        while (band > hudTop + 1)
        {
            int y = band - 1, c = r.Fb[y * 256] & 63;
            if (c != (r.LinePal[y][0] & 63) || r.Sp[y * 256] != 0) break;
            bool flat = true;
            for (int x = 1; x < 256 && flat; x++) flat = (r.Fb[y * 256 + x] & 63) == c;
            if (!flat) break;
            band = y;
        }
        return band;
    }

    /// <summary>T39: on a menu screen (font tiles on most lines: equipment, status, shops) the one-colour backdrop rows
    /// around the windows are black too; null when this is no menu screen</summary>
    public static bool[]? MenuRows(SceneRaster r, HdScene sc)
    {
        int fontLines = 0;
        for (int y = 0; y < 240; y++)
        {
            bool any = false;
            for (int x = 0; x < 256 && !any; x++) any = (sc.Info[(y * 256 + x) * 4 + 2] & HdScene.FUi) != 0;
            if (any) fontLines++;
        }
        if (fontLines < 150) return null;
        var rows = new bool[240];
        for (int y = 0; y < 240; y++)
        {
            int c0 = r.Fb[y * 256] & 63;
            bool flat = r.Sp[y * 256] == 0;
            for (int x = 1; x < 256 && flat; x++) flat = (r.Fb[y * 256 + x] & 63) == c0 && r.Sp[y * 256 + x] == 0;
            rows[y] = flat;
        }
        return rows;
    }

    /// <summary>T51: the picture of a cut-scene (story screens, the title): no HUD split; the longest run
    /// of lines (black gaps without text bridged, up to 64 lines) with at least 8 pixels that are neither black nor font, at least 24 lines
    /// high and 64 pixels wide. Returns (first line, last line + 1, first x, last x + 1) or null.</summary>
    public static (int Y0, int Y1, int X0, int X1)? CineBand(SceneRaster r, HdScene sc)
    {
        if (SplitGap(r) != (0, 0) || sc.Play > 0.5f) return null;
        bool[] art = new bool[240], text = new bool[240];
        for (int y = 0; y < 240; y++)
        {
            int n = 0;
            for (int x = 0; x < 256; x++)
            {
                bool lit = (r.Fb[y * 256 + x] & 63) != 0x0F, ui = (sc.Info[(y * 256 + x) * 4 + 2] & HdScene.FUi) != 0;
                if (lit && !ui) n++;
                if (lit && ui) text[y] = true;
            }
            art[y] = n >= 8;
        }
        int best0 = 0, best1 = 0;
        for (int y = 0; y < 240;)
        {
            if (!art[y]) { y++; continue; }
            int e = y + 1, gap = 0;
            for (; e < 240; e++)
            {
                if (art[e]) gap = 0;
                else if (text[e] || ++gap > 64) break;     // black gaps inside one picture are bridged, text ends it
            }
            e -= gap;
            if (e - y > best1 - best0) (best0, best1) = (y, e);
            y = e + 1;
        }
        if (best1 - best0 < 24) return null;
        int x0 = 256, x1 = 0;
        for (int y = best0; y < best1; y++)
            for (int x = 0; x < 256; x++)
                if ((r.Fb[y * 256 + x] & 63) != 0x0F && (sc.Info[(y * 256 + x) * 4 + 2] & HdScene.FUi) == 0) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x + 1); }
        return x1 - x0 < 64 ? null : (best0, best1, x0, x1);
    }

    /// <summary>T51: weight eases in over half a second per shot (a new band = a new shot: the dolly starts over); a lost
    /// band (fade to black) is held 20 frames; a HUD on screen ends it at once</summary>
    private void CineUpdate((int Y0, int Y1, int X0, int X1)? b)
    {
        if (b is { } nb)
        {
            bool shot = _cineW <= 0 || Math.Abs(nb.Y0 - _band.Y0) > 4 || Math.Abs(nb.Y1 - _band.Y1) > 4;
            if (shot) { _shotStart = _time; _cineW = 0; }
            _band = nb; _cineHold = 20;
            _cineW = Math.Min(1, _cineW + 1 / 30f);
        }
        else if (_cineHold > 0 && _play < 0.5f) _cineHold--;
        else _cineW = 0;
    }

    /// <summary>T33: the black band at the HUD split, [from, to): the gameplay HUD's scroll is fixed (nametable 2, row 24:
    /// v & $0FE0 = $0B00), and the IRQ at line 190 that sets it leaves a seam line above it (half the HUD's top row, stray
    /// tiles). Above the seam, a band the game scrolled to on purpose (a scroll that does not follow the PPU's own line
    /// increment, from line 150 on, running into the HUD) shows unloaded nametable rows: black too when it is mostly dark
    /// background. (0, 0) = no HUD split on this frame.</summary>
    public static (int From, int To) SplitGap(SceneRaster r)
    {
        int hud = -1;
        for (int y = 160; y < 232 && hud < 0; y++)
            if (r.LineScroll[y] >= 0 && (r.LineScroll[y] & 0x0FE0) == 0x0B00 && (r.LineScroll[y - 1] & 0x0FE0) != 0x0B00) hud = y;
        if (hud < 0) return (0, 0);
        int from = hud - 1;
        // walk up while each line continues the one above it (same fine x / coarse x, v = the PPU's y increment)
        int b = hud - 1;
        while (b > 150 && r.LineScroll[b - 1] >= 0 && r.LineScroll[b] == NextLine(r.LineScroll[b - 1])) b--;
        if (b > 150 && b < hud - 1)
        {
            long lit = 0;
            for (int y = b; y < hud; y++)
                for (int x = 0; x < 256; x++)
                    if ((r.BgCol[y * 256 + x] & 63) != 0x0F && r.Bg[y * 256 + x] != 0) lit++;
            if (lit * 100 < (long)(hud - b) * 256 * 30) from = b;   // < 30 % lit: padding, not a scene
        }
        return (from, hud);
    }

    /// <summary>T35 HUD meters, found on screen under the HUD's fixed scroll: LIFE = nametable row 25 (HudHpBar 26:8CC0
    /// draws 17 tiles from $2B26 = column 6: a black frame up to max HP, a cell per 16 HP), FORCE = row 26 (the charge
    /// meter 26:8D7A: chevrons + segments from column 6). Returns {life x0, x1, fill end, top line, force x0, x1, top
    /// line, 0}; x1 = 0 = not on screen. The LIFE fill end is the HP the bar is drawn from ($03C1): column 6 + HP / 2.</summary>
    public static float[] Meters(SceneRaster r, int fill, int hp)
    {
        var m = new float[8];
        int Band(int row)
        {
            for (int y = 150; y < 232; y++)
            {
                int ls = r.LineScroll[y];
                if (ls >= 0 && (ls & 0x0FE0) == (0x0B00 | row << 5) && (ls >> 12 & 7) == 0) return y;
            }
            return -1;
        }
        bool Black(int x, int y) => (r.Fb[y * 256 + x] & 63) == 0x0F;
        int ly = Band(1);
        if (ly >= 0 && ly + 6 < 240 && Black(48, ly))
        {
            int x0 = 48, x1 = 48;
            while (x0 > 40 && Black(x0 - 1, ly)) x0--;
            while (x1 < 255 && Black(x1, ly)) x1++;
            int lit = 0;                                         // ticks / the end cap: white inside the frame
            for (int y = ly + 1; y <= ly + 5; y++)
                for (int x = x0; x < x1; x++) if (!Black(x, y) && (r.Fb[y * 256 + x] & 63) != fill) lit++;
            if (x1 - x0 >= 12 && x1 - x0 <= 17 * 8 + 8 && lit >= 4 && Black(x0 + 1, ly + 6) && Black(x1 - 2, ly + 6))
            {
                m[0] = x0; m[1] = x1; m[3] = ly;
                m[2] = Math.Clamp(48 + hp * 0.5f, x0 + 1, x1 - 1);
            }
        }
        int fy = Band(2);
        if (fy >= 0 && fy + 7 < 240 && fill >= 0)
        {
            int last = -1, gap = 0;
            for (int x = 48; x < 256 && gap <= 6; x++)
            {
                bool any = false;
                for (int y = fy; y < fy + 8 && !any; y++) any = (r.Fb[y * 256 + x] & 63) != fill;
                if (any) { last = x; gap = 0; } else gap++;
            }
            if (last - 48 >= 15 && last - 48 <= 17 * 8) { m[4] = 48; m[5] = last + 1; m[6] = fy; }
        }
        // T43: the status bar's first line and the first line of the last tile row above it (fine y 0), for its shadow
        int hudTop = SplitGap(r).To, rowStart = hudTop - 1;
        while (hudTop > 0 && rowStart > hudTop - 9 && r.LineScroll[rowStart] >= 0 && (r.LineScroll[rowStart] >> 12 & 7) != 0) rowStart--;
        m[7] = hudTop > 0 ? hudTop * 256 + rowStart : 0;
        return m;
    }

    /// <summary>the line-start scroll word (v | fine x << 16) of the next line by the PPU's own y increment</summary>
    private static int NextLine(int ls)
    {
        int v = ls & 0x7FFF, fx = ls & 0x70000;
        if ((v & 0x7000) != 0x7000) v += 0x1000;
        else
        {
            v &= ~0x7000;
            int cy = (v >> 5) & 31;
            if (cy == 29) { cy = 0; v ^= 0x800; }
            else if (cy == 31) cy = 0;
            else cy++;
            v = (v & ~0x3E0) | cy << 5;
        }
        return v | fx;
    }

    /// <summary>the column / row / flip bits of an art word from a tag</summary>
    private static uint PosBits(int tag) => (uint)(tag >> 13 & 7) << 15 | (uint)(tag >> 16 & 7) << 18 |
        ((tag & 1 << 22) != 0 ? 1u << 21 : 0) | ((tag & 1 << 23) != 0 ? 1u << 22 : 0);

    public static uint ArtWord(int tag, byte[] pal, IReadOnlyDictionary<long, int> slots, IReadOnlyDictionary<long, int> fades, bool[] tileArt)
    {
        if ((tag & ReC.Nes.Ppu.TagValid) == 0) return 0;
        int tile = tag & 0x1FFF;
        if (tile >= tileArt.Length || !tileArt[tile]) return 0;
        int g = tag >> 19 & 7, b = g < 4 ? g * 4 : 0x10 + (g - 4) * 4;
        long key = Enhancer.ArtKey(tile, pal[b + 1], pal[b + 2], pal[b + 3]);
        int step = 0;
        if (!slots.TryGetValue(key, out int slot))
        {
            if (!fades.TryGetValue(key, out int fs)) return 0;
            slot = fs & 0xFFFF; step = fs >> 16;                     // a fade step of a painted tile
        }
        return (uint)(slot + 1) | (uint)(tag >> 13 & 7) << 15 | (uint)(tag >> 16 & 7) << 18 |
               ((tag & 1 << 22) != 0 ? 1u << 21 : 0) | ((tag & 1 << 23) != 0 ? 1u << 22 : 0) | (uint)step << 23;
    }

    /// <summary>this frame: layers + art + scene to the GPU, then the frame passes (rounding, albedo, height, light map)</summary>
    /// <summary>r256 = the game's 256-column raster when r is its widescreen copy (SceneRaster.Widen): line analyses (status
    /// bar, edges, meters, cut-scene band) run on it, x positions shifted by the side width</summary>
    public void Upload(SceneRaster r, HdScene sc, SceneRaster? r256 = null)
    {
        var rl = r256 ?? r;
        int e = (r.W - rl.W) / 2;
        _sideE = e;
        SetWidth(r.W);
        byte[] info = sc.Info;
        if (e > 0) { WidenInfo(sc.Info, r, e, _wInfo, SideVoidTop, rl, sc); info = _wInfo; }
        Layers(r, sc, _slots, _fades, _tileArt, Lay, ArtMap, Edges, ModernFont && HasGlyphs ? Chr : null, info, rl);
        fixed (byte* p = Lay) Update(_layTex, p, NesW * 4);
        fixed (uint* p = ArtMap) Update(_artTex, p, NesW * 8);
        fixed (byte* p = info) Update(_iTex, p, NesW * 4);
        fixed (float* p = sc.Lines) Update(_lnTex, p, 240 * 16);
        _camX = sc.CamX; _camY = sc.CamY; _time = sc.Time; _outdoor = sc.Outdoor; _voidGate = sc.VoidGate; _play = sc.Play;
        _icy = sc.Icy; _hot = sc.Hot;
        _wind = Weather.Wind(_time);
        _hudFill = sc.HudFill; _hudTop = sc.HudTop; _hudBottom = sc.HudBottom;
        _meters = Meters(rl, sc.HudFill, Hp);
        if (HudOverlay) _meters[7] = 0;                    // playtest 6: the map continues behind the HUD: no bar shadow on it
        if (e > 0) foreach (int k in new[] { 0, 1, 2, 4, 5 }) if (_meters[k] > 0) _meters[k] += e;
        var cb = CineBand(rl, sc);
        StoryUpdate(rl, sc);
        CineUpdate(cb is { } b0 && e > 0 ? (b0.Y0, b0.Y1, b0.X0 + e, b0.X1 + e) : cb);
        // eye adaptation (gameplay only): dark scenes a little brighter, bright ones a little darker, eased over ~1 s
        float lum = Math.Max(_mr * 0.299f + _mg * 0.587f + _mb * 0.114f, 0.05f);
        float want = 1 + (Math.Clamp(MathF.Sqrt(0.42f / lum), 0.9f, 1.22f) - 1) * _play;
        _exposure += (want - _exposure) * 0.05f;
        (_mr, _mg, _mb) = sc.Mean;

        _albScale = Quality == 0 ? 4 : 8;
        Constants(1, 1, 0, 240);
        Begin();
        Pass(_k1Rtv, 0, 0, KW, KH, _psScale, null, null, null, _laySrv);
        if (_albScale == 8)
        {
            Pass(_k2Rtv, 0, 0, KW * 2, KH * 2, _psScale, null, null, null, _k1Srv);
            Pass(_albRtv, 0, 0, AW, AH, _psAlbedo, null, null, null, _k2Srv);
            _albCur = _albSrv;
        }
        else
        {
            Pass(_a4Rtv, 0, 0, AW / 2, AH / 2, _psAlbedo, null, null, null, _k1Srv);   // fast: 4x straight from the 2x keys
            _albCur = _a4Srv;
        }
        // height: seeds, jump flood 4 / 2 / 1, pillow + blur
        Pass(_jaRtv, 0, 0, KW, KH, _psSeed, null, null, null, _k1Srv);
        void* a = _jaSrv, ar = _jaRtv, b = _jbSrv, br = _jbRtv;
        foreach (int k in new[] { 4, 2, 1 })
        {
            _c[30] = k; SetConstants();
            Pass(br, 0, 0, KW, KH, _psJfa, a, null, null, null);
            void* ts = a; a = b; b = ts;
            void* tr = ar; ar = br; br = tr;
        }
        Pass(_hRtv, 0, 0, KW, KH, _psHeight, a, null, null, null);
        // light map: emissive colours, blurred wide (3 x H + V)
        Pass(_l1Rtv, 0, 0, LW, LH, _psEmit, null, null, null, null);
        for (int k = 0; k < 3; k++)
        {
            _c[28] = 1f / LW; _c[29] = 0; SetConstants();
            Pass(_l2Rtv, 0, 0, LW, LH, _psBlur, _l1Srv, null, null, null);
            _c[28] = 0; _c[29] = 1f / LH; SetConstants();
            Pass(_l1Rtv, 0, 0, LW, LH, _psBlur, _l2Srv, null, null, null);
        }
        // bounce light: the albedo averaged at 128x120, blurred very wide (2 x H + V at 2x the step)
        Pass(_g1Rtv, 0, 0, LW, LH, _psBounce, _albCur, null, null, null);
        for (int k = 0; k < 2; k++)
        {
            _c[28] = 2f / LW; _c[29] = 0; SetConstants();
            Pass(_g2Rtv, 0, 0, LW, LH, _psBlur, _g1Srv, null, null, null);
            _c[28] = 0; _c[29] = 2f / LH; SetConstants();
            Pass(_g1Rtv, 0, 0, LW, LH, _psBlur, _g2Srv, null, null, null);
        }
        Unbind();
    }

    /// <summary>constants (16 x float4) for picture size pw x ph, source lines first..first+lines</summary>
    private void Constants(int pw, int ph, int firstLine, int lines)
    {
        var (hx, hy, hz) = Norm(Light.X, Light.Y, Light.Z + 1);
        float sharp = Math.Max(1f, ph / (lines * (float)_albScale));
        float lum = _mr * 0.299f + _mg * 0.587f + _mb * 0.114f;
        float dark = 1 - Math.Clamp((lum - 0.12f) / 0.18f, 0, 1);
        float[] c =
        {
            pw, ph, 1f / pw, 1f / ph,
            1, lines / 240f, 0, firstLine / 240f,
            Light.X, Light.Y, Light.Z, Bump * _play,              // playtest 9: no relief on title / story screens (ridges along text rows)
            hx, hy, hz, Light.Z,
            Amb, Kd, SpecK, TiltShift ? 1 : 0,
            LiftH, AoK * _play, Shadows ? ShadowK * _play : 0, Shadows ? ContactK * _play : 0,   // playtest 7: no cast shadows on the title / story screens (text streaks)
            ShadowSlope, sharp, BloomThreshold, BloomK,
            0, 0, 0, 0,
            _camX, _camY, _time, Sharpen ? SharpenK : 0,
            Parallax ? 1 : 0, Shadows ? 1 : 0, Atmosphere ? 1 : 0, Detail ? 1 : 0,
            _outdoor, _voidGate, _play, dark * dark * (3 - 2 * dark),
            _mr, _mg, _mb, SmoothOutlines ? 1 : 0,
            _exposure, Saturation, Contrast, Vignette,
            Reflections ? ReflectK : 0, Lights ? LightK : 0, Atmosphere ? _icy : 0, Atmosphere ? _hot : 0,
            _hudFill, _hudTop, _hudBottom + 1, HudStyle ? 1 : 0,
            Quality >= 2 ? 24 : Quality == 1 ? 16 : 10, Quality >= 1 ? 1 : 0, _albScale, _osdOn ? 1 : 0,
            _meters[0], _meters[1], _meters[2], _meters[3],
            _meters[4], _meters[5], _meters[6], _meters[7],
            Cinematic && CineAllowed ? _cineW : 0, _band.Y0 * 256 + _band.Y1, _band.X0 * 1024 + _band.X1, _shotStart,
            Hurt, LowLife ? 1 : 0, Cinematic && _storyOn ? StoryScene : 0, _storyFade,
            ColorM[0], ColorM[1], ColorM[2], (float)_wind.Strength,                // playtest 12: the weather's wind (Weather.cs)
            ColorM[3], ColorM[4], ColorM[5], (float)_wind.Gust,
            ColorM[6], ColorM[7], ColorM[8], (float)_wind.Dir,
            NesW, 240, _sideE, _storyBand,
            Room ? 1 : 0, 0, 0, 0,                                          // playtest 12: shop / inn interior
        };
        c.CopyTo(_c, 0);
        SetConstants();
    }

    private void Begin()
    {
        ((delegate* unmanaged[Stdcall]<void*, uint, void>)Vt(_ctx)[24])(_ctx, 4 /* TRIANGLELIST */);
        ((delegate* unmanaged[Stdcall]<void*, void*, void>)Vt(_ctx)[17])(_ctx, null);      // no input layout
        ((delegate* unmanaged[Stdcall]<void*, void*, void**, uint, void>)Vt(_ctx)[11])(_ctx, _vs, null, 0);
        void* samp = _sampLin, cb = _cb;
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(_ctx)[10])(_ctx, 0, 1, &samp);
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(_ctx)[16])(_ctx, 0, 1, &cb);
    }

    /// <summary>draw into the back buffer: black, the lit picture in rect (x, y, w, h), source lines first..first+lines;
    /// present = show it (false: leave it for Capture)</summary>
    public void Draw(int cw, int ch, (int X, int Y, int W, int H) r, int firstLine, int lines, bool present = true)
    {
        Resize(Math.Max(cw, 1), Math.Max(ch, 1));
        int pw = Math.Max(r.W, 1), ph = Math.Max(r.H, 1);
        Targets(pw, ph);
        float* clear = stackalloc float[] { 0, 0, 0, 1 };
        ((delegate* unmanaged[Stdcall]<void*, void*, float*, void>)Vt(_ctx)[50])(_ctx, _backRtv, clear);
        int lw = _lw, lh = _lh;
        Constants(lw, lh, firstLine, lines);
        Begin();
        // 5. light -> HDR (at the quality's resolution)
        Pass(_hdrRtv, 0, 0, lw, lh, _psLight, _albCur == null ? _albSrv : _albCur, _hSrv, _l1Srv, null, _g1Srv);
        // 6. bloom: bright part at 1/4, blur H + V twice (wide, soft glow)
        int bw = Math.Max(lw / 4, 1), bh = Math.Max(lh / 4, 1);
        Pass(_b1Rtv, 0, 0, bw, bh, _psBright, _hdrSrv, null, null, null);
        for (int k = 0; k < 2; k++)
        {
            _c[28] = 1f / bw; _c[29] = 0; SetConstants();
            Pass(_b2Rtv, 0, 0, bw, bh, _psBlur, _b1Srv, null, null, null);
            _c[28] = 0; _c[29] = 1f / bh; SetConstants();
            Pass(_b1Rtv, 0, 0, bw, bh, _psBlur, _b2Srv, null, null, null);
        }
        // wide halo: the glow again at 1/8, blurred twice more
        int cw8 = Math.Max(lw / 8, 1), ch8 = Math.Max(lh / 8, 1);
        _c[28] = 0; _c[29] = 0; SetConstants();
        Pass(_b3Rtv, 0, 0, cw8, ch8, _psBlur, _b1Srv, null, null, null);
        for (int k = 0; k < 2; k++)
        {
            _c[28] = 1f / cw8; _c[29] = 0; SetConstants();
            Pass(_b4Rtv, 0, 0, cw8, ch8, _psBlur, _b3Srv, null, null, null);
            _c[28] = 0; _c[29] = 1f / ch8; SetConstants();
            Pass(_b3Rtv, 0, 0, cw8, ch8, _psBlur, _b4Srv, null, null, null);
        }
        // 7. composite (tone, grade), then 8. edge anti-aliasing into the picture rectangle
        _c[0] = pw; _c[1] = ph; _c[2] = 1f / pw; _c[3] = 1f / ph; SetConstants();
        Pass(_ldrRtv, 0, 0, pw, ph, _psComp, _hdrSrv, _b1Srv, _b3Srv, null);
        bool bars = Ambient && (r.X > 0 || r.Y > 0 || r.W < cw || r.H < ch);
        if (bars || _osdOn || _sideE > 0)
        {
            // T53 / T56: the lit picture at 1/8 (the halo targets are free after the composite), blurred wide (steps of 4
            // texels, twice each way): the ambient bars and the frosted glass under the on-screen display
            _c[28] = 0.5f / cw8; _c[29] = 0.5f / ch8; SetConstants();
            Pass(_b3Rtv, 0, 0, cw8, ch8, _psBlur, _hdrSrv, null, null, null);
            for (int k = 0; k < 2; k++)
            {
                _c[28] = 4f / cw8; _c[29] = 0; SetConstants();
                Pass(_b4Rtv, 0, 0, cw8, ch8, _psBlur, _b3Srv, null, null, null);
                _c[28] = 0; _c[29] = 4f / ch8; SetConstants();
                Pass(_b3Rtv, 0, 0, cw8, ch8, _psBlur, _b4Srv, null, null, null);
            }
        }
        if (bars)
        {
            // T53 ambient bars: the whole client area first (the picture pass then draws over its rectangle)
            _c[0] = cw; _c[1] = ch; _c[2] = 1f / cw; _c[3] = 1f / ch;
            _c[28] = (float)r.X / cw; _c[29] = (float)r.Y / ch; _c[30] = (float)pw / cw; _c[31] = (float)ph / ch; SetConstants();
            Pass(_backRtv, 0, 0, cw, ch, _psAmb, _b3Srv, null, null, null);
            _c[0] = pw; _c[1] = ph; _c[2] = 1f / pw; _c[3] = 1f / ph; SetConstants();
        }
        Pass(_backRtv, r.X, r.Y, pw, ph, _psFinal, _ldrSrv, _oSrv, _osdOn || _sideE > 0 ? _b3Srv : null, null);
        Unbind();
        if (present) ((delegate* unmanaged[Stdcall]<void*, uint, uint, int>)Vt(_swap)[8])(_swap, VSync ? 1u : 0u, 0);
    }

    private void SetConstants()
    {
        Mapped m;
        Check(((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, Mapped*, int>)Vt(_ctx)[14])(_ctx, _cb, 0, 4 /* WRITE_DISCARD */, 0, &m), "Map (constants)");
        fixed (float* c = _c) Buffer.MemoryCopy(c, m.Data, NConst * 16, NConst * 16);
        ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)Vt(_ctx)[15])(_ctx, _cb, 0);
    }

    /// <summary>one full-screen pass: t0-t2 float inputs, t3 uint keys; t4-t10 = the uploads (info, lines, materials,
    /// NES colours, layers, art map, atlas), bound for every pass</summary>
    private void Pass(void* rtv, int x, int y, int w, int h, void* ps, void* t0, void* t1, void* t2, void* t3, void* t11 = null)
    {
        Unbind();
        ((delegate* unmanaged[Stdcall]<void*, uint, void**, void*, void>)Vt(_ctx)[33])(_ctx, 1, &rtv, null);
        var vp = new Viewport { X = x, Y = y, W = w, H = h, MaxZ = 1 };
        ((delegate* unmanaged[Stdcall]<void*, uint, Viewport*, void>)Vt(_ctx)[44])(_ctx, 1, &vp);
        void** srv = stackalloc void*[] { t0, t1, t2, t3, _iSrv, _lnSrv, _mSrv, _rgbSrv, _laySrv, _artSrv, _atlSrv, t11, _glySrv };
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(_ctx)[8])(_ctx, 0, NSrv, srv);
        ((delegate* unmanaged[Stdcall]<void*, void*, void**, uint, void>)Vt(_ctx)[9])(_ctx, ps, null, 0);
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void>)Vt(_ctx)[13])(_ctx, 3, 0);
    }

    /// <summary>no texture bound as both input and target</summary>
    private void Unbind()
    {
        if (_ctx == null) return;
        void** srv = stackalloc void*[NSrv];
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(_ctx)[8])(_ctx, 0, NSrv, srv);
        void* none = null;
        ((delegate* unmanaged[Stdcall]<void*, uint, void**, void*, void>)Vt(_ctx)[33])(_ctx, 0, &none, null);
    }

    /// <summary>--dump-window: draw without presenting, copy the back buffer to the CPU (0x00RRGGBB, cw x ch)</summary>
    public uint[] Capture(int cw, int ch, (int X, int Y, int W, int H) r, int firstLine, int lines)
    {
        Draw(cw, ch, r, firstLine, lines, present: false);
        void* back, stage;
        Guid iid = IidTex2D;
        Check(((delegate* unmanaged[Stdcall]<void*, uint, Guid*, void**, int>)Vt(_swap)[9])(_swap, 0, &iid, &back), "GetBuffer");
        var td = new TexDesc { Width = (uint)_bw, Height = (uint)_bh, MipLevels = 1, ArraySize = 1, Format = FmtBgra, SampleCount = 1, Usage = 3 /* STAGING */, CpuAccess = 0x20000 };
        int hr = ((delegate* unmanaged[Stdcall]<void*, TexDesc*, void*, void**, int>)Vt(_dev)[5])(_dev, &td, null, &stage);
        if (hr < 0) { Release(ref back); Check(hr, "CreateTexture2D (staging)"); }
        ((delegate* unmanaged[Stdcall]<void*, void*, void*, void>)Vt(_ctx)[47])(_ctx, stage, back);
        Release(ref back);
        Mapped m;
        hr = ((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, Mapped*, int>)Vt(_ctx)[14])(_ctx, stage, 0, 1 /* READ */, 0, &m);
        if (hr < 0) { Release(ref stage); Check(hr, "Map (staging)"); }
        var px = new uint[_bw * _bh];
        for (int y = 0; y < _bh; y++)
        {
            uint* row = (uint*)((byte*)m.Data + y * m.RowPitch);
            for (int x = 0; x < _bw; x++) px[y * _bw + x] = row[x] & 0xFFFFFF;
        }
        ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)Vt(_ctx)[15])(_ctx, stage, 0);
        Release(ref stage);
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, int>)Vt(_swap)[8])(_swap, 0, 0);
        return px;
    }

    public void Dispose()
    {
        Unbind();
        Release(ref _backRtv);
        Release(ref _b3Srv); Release(ref _b3Rtv); Release(ref _b3Tex); Release(ref _b4Srv); Release(ref _b4Rtv); Release(ref _b4Tex);
        Release(ref _ldrSrv); Release(ref _ldrRtv); Release(ref _ldrTex);
        Release(ref _hdrSrv); Release(ref _hdrRtv); Release(ref _hdrTex);
        Release(ref _b1Srv); Release(ref _b1Rtv); Release(ref _b1Tex);
        Release(ref _b2Srv); Release(ref _b2Rtv); Release(ref _b2Tex);
        Release(ref _k1Srv); Release(ref _k1Rtv); Release(ref _k1Tex);
        Release(ref _k2Srv); Release(ref _k2Rtv); Release(ref _k2Tex);
        Release(ref _albSrv); Release(ref _albRtv); Release(ref _albTex);
        Release(ref _a4Srv); Release(ref _a4Rtv); Release(ref _a4Tex);
        Release(ref _jaSrv); Release(ref _jaRtv); Release(ref _jaTex);
        Release(ref _jbSrv); Release(ref _jbRtv); Release(ref _jbTex);
        Release(ref _hSrv); Release(ref _hRtv); Release(ref _hTex);
        Release(ref _l1Srv); Release(ref _l1Rtv); Release(ref _l1Tex);
        Release(ref _l2Srv); Release(ref _l2Rtv); Release(ref _l2Tex);
        Release(ref _g1Srv); Release(ref _g1Rtv); Release(ref _g1Tex); Release(ref _g2Srv); Release(ref _g2Rtv); Release(ref _g2Tex);
        Release(ref _psBounce); Release(ref _oSrv); Release(ref _oTex);
        Release(ref _laySrv); Release(ref _layTex); Release(ref _artSrv); Release(ref _artTex);
        Release(ref _iSrv); Release(ref _iTex); Release(ref _lnSrv); Release(ref _lnTex); Release(ref _mSrv); Release(ref _mTex);
        Release(ref _rgbSrv); Release(ref _rgbTex); Release(ref _atlSrv); Release(ref _atlTex); Release(ref _glySrv); Release(ref _glyTex);
        Release(ref _vs); Release(ref _psScale); Release(ref _psAlbedo); Release(ref _psSeed); Release(ref _psJfa);
        Release(ref _psHeight); Release(ref _psEmit); Release(ref _psLight); Release(ref _psBright); Release(ref _psBlur); Release(ref _psComp); Release(ref _psFinal); Release(ref _psAmb);
        Release(ref _sampLin); Release(ref _cb);
        Release(ref _swap); Release(ref _ctx); Release(ref _dev);
    }
}
