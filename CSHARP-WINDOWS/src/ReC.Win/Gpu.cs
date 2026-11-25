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
    public const int AW = 2048, AH = 1920;            // albedo: 8x
    public const int KW = 512, KH = 480;              // height field: 2x
    public const int LW = 128, LH = 120;              // light map
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
    /// <summary>T35: the player's HP (RAM $03C1) for the LIFE meter's fill (set before Upload)</summary>
    public int Hp;
    private float[] _meters = new float[8];
    /// <summary>T31: HD quality 2 = full, 1 = balanced (lighting at 75 % resolution, 16 shadow steps), 0 = fast (50 %, 10
    /// steps, no reflections / fine relief): for slower (integrated) graphics</summary>
    public int Quality = 2;
    private int _lw, _lh;
    private void* _a4Tex, _a4Rtv, _a4Srv, _albCur;          // fast quality: the albedo at 4x
    private int _albScale = 8;

    // ---- tunables (cbuffer)
    private static readonly (float X, float Y, float Z) Light = Norm(-0.45f, -0.62f, 0.64f);
    private const float Bump = 1.8f, Amb = 0.50f, Kd = 0.50f, SpecK = 0.55f, LiftH = 2.2f,
        AoK = 0.45f, ShadowK = 0.60f, ShadowSlope = 0.16f, BloomThreshold = 0.95f, BloomK = 0.45f,
        Saturation = 1.08f, Contrast = 0.12f, Vignette = 0.22f, ReflectK = 0.30f, LightK = 2.2f, ContactK = 0.30f;

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
    private void* _vs, _psScale, _psAlbedo, _psSeed, _psJfa, _psHeight, _psEmit, _psLight, _psBright, _psBlur, _psComp, _psFinal, _sampLin, _cb;
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
    public readonly byte[] Lay = new byte[256 * 240 * 4];
    public readonly uint[] ArtMap = new uint[256 * 240 * 2];

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
    private const int NSrv = 12, NConst = 18;
    private static readonly Guid IidTex2D = new("6f15aaf2-d208-4e89-9ab4-489535d34f9c");

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

        MakeTex(256, 240, FmtRgba8U, out _layTex, out _laySrv);
        MakeTex(256, 240, FmtRg32U, out _artTex, out _artSrv);
        MakeTex(256, 240, FmtRgba8, out _iTex, out _iSrv);
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
    public static void Layers(SceneRaster r, HdScene sc, IReadOnlyDictionary<long, int> slots, IReadOnlyDictionary<long, int> fades,
        bool[] tileArt, byte[] lay, uint[] artMap, bool edges = false)
    {
        var fb = r.Fb;
        int band = edges ? EdgeBand(r, sc) : 240;
        var (gap0, gap1) = edges ? SplitGap(r) : (0, 0);
        for (int y = 0; y < 240; y++)
        {
            // T32 clean edges: the lines a TV hid (232-239, where the game leaves stray tiles) and the left column the game
            // masks (backdrop colour), the backdrop rows under a bottom HUD panel and the HUD split seam (SplitGap) are exact black: HUD bit, no sprite, no art
            int ex = !edges ? 0 : y >= band || (y >= gap0 && y < gap1) ? 256 : r.LeftClip[y] ? 8 : 0;
            for (int x = 0; x < ex; x++)
            {
                int i = y * 256 + x, o = i * 4;
                lay[o] = 0x0F | 0x80; lay[o + 1] = 0; lay[o + 2] = 0; lay[o + 3] = 0;
                artMap[i * 2] = 0; artMap[i * 2 + 1] = 0;
            }
            var pal = r.LinePal[y];
            // the slot / step part of an art word depends only on tile + palette group (+ the line's palette): cached per
            // run of equal keys (tiles repeat for 8 pixels); the pixel's column / row / flips are added per pixel
            int bk = -1, sk = -1;
            uint bw = 0, sw = 0;
            for (int x = ex; x < 256; x++)
            {
                int i = y * 256 + x, o = i * 4;
                bool ui = (sc.Info[o + 2] & HdScene.FUi) != 0;
                int sp = r.Sp[i];
                int bgc = r.BgCol[i] & 63, partner = 0;
                // checkerboard dither: the 4 neighbours share one other colour and >= 2 diagonals have this one. Both
                // cells of a dither get the same key (the lower colour + the higher one as partner), so the rounding
                // sees one flat area (no diagonal stripes) and the albedo shows the 50/50 mix
                if (x > 0 && y > 0 && x < 255 && y < 239 && !ui && sp == 0)
                {
                    int p = fb[i], q = fb[i - 1];
                    if (q != p && fb[i + 1] == q && fb[i - 256] == q && fb[i + 256] == q &&
                        (fb[i - 257] == p ? 1 : 0) + (fb[i - 255] == p ? 1 : 0) + (fb[i + 255] == p ? 1 : 0) + (fb[i + 257] == p ? 1 : 0) >= 2)
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
        int band = 232;
        if (sc.HudFill < 0 || sc.HudTop < 160) return band;
        while (band > sc.HudTop + 1)
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
            if (x1 - x0 >= 12 && Black(x0 + 1, ly + 6) && Black(x1 - 2, ly + 6))
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
            if (last - 48 >= 15) { m[4] = 48; m[5] = last + 1; m[6] = fy; }
        }
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
    public void Upload(SceneRaster r, HdScene sc)
    {
        Layers(r, sc, _slots, _fades, _tileArt, Lay, ArtMap, Edges);
        fixed (byte* p = Lay) Update(_layTex, p, 256 * 4);
        fixed (uint* p = ArtMap) Update(_artTex, p, 256 * 8);
        fixed (byte* p = sc.Info) Update(_iTex, p, 256 * 4);
        fixed (float* p = sc.Lines) Update(_lnTex, p, 240 * 16);
        _camX = sc.CamX; _camY = sc.CamY; _time = sc.Time; _outdoor = sc.Outdoor; _voidGate = sc.VoidGate; _play = sc.Play;
        _icy = sc.Icy; _hot = sc.Hot;
        _hudFill = sc.HudFill; _hudTop = sc.HudTop; _hudBottom = sc.HudBottom;
        _meters = Meters(r, sc.HudFill, Hp);
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
            Light.X, Light.Y, Light.Z, Bump,
            hx, hy, hz, Light.Z,
            Amb, Kd, SpecK, 0,
            LiftH, AoK, Shadows ? ShadowK : 0, Shadows ? ContactK : 0,
            ShadowSlope, sharp, BloomThreshold, BloomK,
            0, 0, 0, 0,
            _camX, _camY, _time, 0,
            Parallax ? 1 : 0, Shadows ? 1 : 0, Atmosphere ? 1 : 0, Detail ? 1 : 0,
            _outdoor, _voidGate, _play, dark * dark * (3 - 2 * dark),
            _mr, _mg, _mb, 0,
            _exposure, Saturation, Contrast, Vignette,
            Reflections ? ReflectK : 0, Lights ? LightK : 0, Atmosphere ? _icy : 0, Atmosphere ? _hot : 0,
            _hudFill, _hudTop, _hudBottom + 1, HudStyle ? 1 : 0,
            Quality >= 2 ? 24 : Quality == 1 ? 16 : 10, Quality >= 1 ? 1 : 0, _albScale, 0,
            _meters[0], _meters[1], _meters[2], _meters[3],
            _meters[4], _meters[5], _meters[6], _meters[7],
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
        Pass(_backRtv, r.X, r.Y, pw, ph, _psFinal, _ldrSrv, null, null, null);
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
        void** srv = stackalloc void*[] { t0, t1, t2, t3, _iSrv, _lnSrv, _mSrv, _rgbSrv, _laySrv, _artSrv, _atlSrv, t11 };
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
        Release(ref _psBounce);
        Release(ref _laySrv); Release(ref _layTex); Release(ref _artSrv); Release(ref _artTex);
        Release(ref _iSrv); Release(ref _iTex); Release(ref _lnSrv); Release(ref _lnTex); Release(ref _mSrv); Release(ref _mTex);
        Release(ref _rgbSrv); Release(ref _rgbTex); Release(ref _atlSrv); Release(ref _atlTex);
        Release(ref _vs); Release(ref _psScale); Release(ref _psAlbedo); Release(ref _psSeed); Release(ref _psJfa);
        Release(ref _psHeight); Release(ref _psEmit); Release(ref _psLight); Release(ref _psBright); Release(ref _psBlur); Release(ref _psComp); Release(ref _psFinal);
        Release(ref _sampLin); Release(ref _cb);
        Release(ref _swap); Release(ref _ctx); Release(ref _dev);
    }
}
