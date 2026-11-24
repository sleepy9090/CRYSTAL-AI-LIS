// re-c Windows host — "HD" graphics (T18, T19: Video > Graphics > HD (GPU)). Not in the web port.
//
// A Direct3D 11 renderer called through raw COM vtables (no packages): d3d11.dll D3D11CreateDeviceAndSwapChain and
// d3dcompiler_47.dll D3DCompile (the HLSL below is compiled once at start). It draws at the window's own resolution
// (1080p, 4K ...), not at a fixed 4x.
//
// Inputs per frame, made on the CPU by Enhance.cs (ModeHd) and HdScene.cs:
//   albedo  1024x960  the Enhanced picture without baked shading (dither blend, Scale2x rounding, graphics-pack tiles)
//   height  512x480   the height "model" of every visible shape (pillow profile from the distance to its edge)
//   info    256x240   sprite lift, NES colour (-> material), flags sprite / void (dark backdrop) / UI (font, HUD split)
//   lines   240       world position of each line (PPU scroll): material detail stays fixed to the world when scrolling
//   material 64       per NES colour: kind (stone, water / ice, plaster, earth / wood, foliage), specular, gloss, detail
// Passes (full-screen triangles, no vertex buffers):
//   1. light -> HDR (RGBA16F, picture size), per pixel:
//      - material detail: procedural surface relief per material (cracked stone, grain, grass blades, rippling water),
//        anchored to the world through the line scroll, as albedo variation + detail normals;
//      - normal = height model + sprite lift + detail; coloured key light (warm sun, cool hemisphere ambient: the
//        light is normalised so flat ground keeps its brightness), Blinn-Phong specular per material + twinkling glints
//        on water / ice, rim light on sprites, ambient occlusion (12 taps, 2 radii);
//      - shadows: soft shadows marched toward the sun through the height model (24 steps, penumbra grows with the
//        distance: sprites and walls throw shadows), drifting cloud shadows outdoors;
//      - parallax: a dark backdrop inside the playfield (pits, chasms, the void around caves) becomes depth -- cliff
//        walls seen in perspective from the screen centre, three rock layers below that scroll slower than the
//        playfield (camera from the scroll) and are shadowed by the ledges above; dust motes and mist in front
//        scroll faster than the playfield;
//      - HUD lines / font tiles: exactly the albedo (alpha 0 = no lighting, no post).
//   2. bloom: soft-knee bright pass at 1/4 size, Gaussian blur horizontal + vertical (twice);
//   3. composite into the swap chain rectangle: bloom, filmic shoulder, saturation / contrast grade, vignette.
// Any failure (no Direct3D 11, compiler missing) -> the caller falls back to Enhanced (32-bit).
using System.Runtime.InteropServices;
using System.Text;

namespace ReC.Win;

internal sealed unsafe partial class Gpu : IDisposable
{
    public const int AW = Enhancer.W, AH = Enhancer.H, HW = AW / 2, HH = AH / 2;
    public string Info { get; private set; } = "";

    /// <summary>T19 effect switches (Video > Graphics > HD effects)</summary>
    public bool Parallax = true, Shadows = true, Atmosphere = true, Detail = true;

    // ---- tunables (cbuffer)
    private static readonly (float X, float Y, float Z) Light = Norm(-0.45f, -0.62f, 0.64f);
    private const float Bump = 1.8f, Amb = 0.50f, Kd = 0.50f, SpecK = 0.55f, LiftH = 2.2f,
        AoK = 0.45f, ShadowK = 0.60f, ShadowSlope = 0.16f, BloomThreshold = 0.95f, BloomK = 0.45f,
        Saturation = 1.08f, Contrast = 0.12f, Vignette = 0.22f;

    private void* _dev, _ctx, _swap, _backRtv;
    private void* _albTex, _albSrv, _hTex, _hSrv, _iTex, _iSrv, _lnTex, _lnSrv, _mTex, _mSrv;
    private void* _hdrTex, _hdrRtv, _hdrSrv, _b1Tex, _b1Rtv, _b1Srv, _b2Tex, _b2Rtv, _b2Srv;
    private void* _vs, _psLight, _psBright, _psBlur, _psComp, _sampLin, _cb;
    private int _bw, _bh, _pw, _ph;               // back buffer, picture (HDR) size
    private float _camX, _camY, _time, _outdoor, _voidGate, _mr = 0.3f, _mg = 0.3f, _mb = 0.3f, _play;

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

    private const uint FmtBgra = 87, FmtR32F = 41, FmtRgba8 = 28, FmtRgba32F = 2, FmtRgba16F = 10;
    private const uint BindSrv = 8, BindRtv = 0x20;
    private const int NSrv = 7, NConst = 16;
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
    public static Gpu? TryCreate(IntPtr hwnd, int cw, int ch, out string error)
    {
        var g = new Gpu();
        try
        {
            g.Init(hwnd, Math.Max(cw, 1), Math.Max(ch, 1));
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

    private void Init(IntPtr hwnd, int cw, int ch)
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
        _psLight = Compile("PSLight", "ps_4_0");
        _psBright = Compile("PSBright", "ps_4_0");
        _psBlur = Compile("PSBlur", "ps_4_0");
        _psComp = Compile("PSComp", "ps_4_0");

        MakeTex(AW, AH, FmtBgra, out _albTex, out _albSrv);
        MakeTex(HW, HH, FmtR32F, out _hTex, out _hSrv);
        MakeTex(256, 240, FmtRgba8, out _iTex, out _iSrv);
        MakeTex(240, 1, FmtRgba32F, out _lnTex, out _lnSrv);
        MakeTex(64, 1, FmtRgba8, out _mTex, out _mSrv);
        fixed (byte* m = HdScene.MaterialTable())
            ((delegate* unmanaged[Stdcall]<void*, void*, uint, void*, void*, uint, uint, void>)Vt(_ctx)[48])(_ctx, _mTex, 0, null, m, 64 * 4, 0);

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

    private void MakeTarget(int w, int h, out void* texOut, out void* rtvOut, out void* srvOut)
    {
        var td = new TexDesc { Width = (uint)w, Height = (uint)h, MipLevels = 1, ArraySize = 1, Format = FmtRgba16F, SampleCount = 1, BindFlags = BindSrv | BindRtv };
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
        if (pw == _pw && ph == _ph && _hdrTex != null) return;
        Unbind();
        Release(ref _hdrSrv); Release(ref _hdrRtv); Release(ref _hdrTex);
        Release(ref _b1Srv); Release(ref _b1Rtv); Release(ref _b1Tex);
        Release(ref _b2Srv); Release(ref _b2Rtv); Release(ref _b2Tex);
        _pw = pw; _ph = ph;
        MakeTarget(pw, ph, out _hdrTex, out _hdrRtv, out _hdrSrv);
        MakeTarget(Math.Max(pw / 4, 1), Math.Max(ph / 4, 1), out _b1Tex, out _b1Rtv, out _b1Srv);
        MakeTarget(Math.Max(pw / 4, 1), Math.Max(ph / 4, 1), out _b2Tex, out _b2Rtv, out _b2Srv);
    }

    /// <summary>this frame's albedo (0x00RRGGBB), height model and scene (info, line scroll, camera, tint)</summary>
    public void Upload(uint[] albedo, float[] height, HdScene sc)
    {
        var upd = (delegate* unmanaged[Stdcall]<void*, void*, uint, void*, void*, uint, uint, void>)Vt(_ctx)[48];
        fixed (uint* a = albedo) upd(_ctx, _albTex, 0, null, a, AW * 4, 0);
        fixed (float* h = height) upd(_ctx, _hTex, 0, null, h, HW * 4, 0);
        fixed (byte* i = sc.Info) upd(_ctx, _iTex, 0, null, i, 256 * 4, 0);
        fixed (float* l = sc.Lines) upd(_ctx, _lnTex, 0, null, l, 240 * 16, 0);
        _camX = sc.CamX; _camY = sc.CamY; _time = sc.Time; _outdoor = sc.Outdoor; _voidGate = sc.VoidGate; _play = sc.Play;
        (_mr, _mg, _mb) = sc.Mean;
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

        // constants (16 x float4)
        var (hx, hy, hz) = Norm(Light.X, Light.Y, Light.Z + 1);
        float sharp = Math.Max(1f, ph / (lines * (float)Enhancer.S));
        float* c = stackalloc float[NConst * 4]
        {
            pw, ph, 1f / pw, 1f / ph,
            1, lines / 240f, 0, firstLine / 240f,
            Light.X, Light.Y, Light.Z, Bump,
            hx, hy, hz, Light.Z,
            Amb, Kd, SpecK, 0,
            LiftH, AoK, Shadows ? ShadowK : 0, 0,
            ShadowSlope, sharp, BloomThreshold, BloomK,
            0, 0, 0, 0,
            _camX, _camY, _time, 0,
            Parallax ? 1 : 0, Shadows ? 1 : 0, Atmosphere ? 1 : 0, Detail ? 1 : 0,
            _outdoor, _voidGate, _play, 0,
            _mr, _mg, _mb, 0,
            0, Saturation, Contrast, Vignette,
            0, 0, 0, 0,
            0, 0, 0, 0,
            0, 0, 0, 0,
        };
        SetConstants(c);

        var ia = (delegate* unmanaged[Stdcall]<void*, uint, void>)Vt(_ctx)[24];
        ia(_ctx, 4 /* TRIANGLELIST */);
        ((delegate* unmanaged[Stdcall]<void*, void*, void>)Vt(_ctx)[17])(_ctx, null);      // no input layout
        ((delegate* unmanaged[Stdcall]<void*, void*, void**, uint, void>)Vt(_ctx)[11])(_ctx, _vs, null, 0);
        void* samp = _sampLin, cb = _cb;
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(_ctx)[10])(_ctx, 0, 1, &samp);
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(_ctx)[16])(_ctx, 0, 1, &cb);

        // 1. light -> HDR
        Pass(_hdrRtv, 0, 0, pw, ph, _psLight, _albSrv, _hSrv, _iSrv, null, null);
        // 2. bloom: bright part at 1/4, blur H + V twice (wide, soft glow)
        int bw = Math.Max(pw / 4, 1), bh = Math.Max(ph / 4, 1);
        Pass(_b1Rtv, 0, 0, bw, bh, _psBright, null, null, null, _hdrSrv, null);
        for (int k = 0; k < 2; k++)
        {
            c[28] = 1f / bw; c[29] = 0; SetConstants(c);
            Pass(_b2Rtv, 0, 0, bw, bh, _psBlur, null, null, null, _b1Srv, null);
            c[28] = 0; c[29] = 1f / bh; SetConstants(c);
            Pass(_b1Rtv, 0, 0, bw, bh, _psBlur, null, null, null, _b2Srv, null);
        }
        // 3. composite into the picture rectangle
        Pass(_backRtv, r.X, r.Y, pw, ph, _psComp, null, null, null, _hdrSrv, _b1Srv);
        Unbind();
        if (present) ((delegate* unmanaged[Stdcall]<void*, uint, uint, int>)Vt(_swap)[8])(_swap, 0, 0);
    }

    private void SetConstants(float* c)
    {
        Mapped m;
        Check(((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, Mapped*, int>)Vt(_ctx)[14])(_ctx, _cb, 0, 4 /* WRITE_DISCARD */, 0, &m), "Map (constants)");
        Buffer.MemoryCopy(c, m.Data, NConst * 16, NConst * 16);
        ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)Vt(_ctx)[15])(_ctx, _cb, 0);
    }

    private void Pass(void* rtv, int x, int y, int w, int h, void* ps, void* t0, void* t1, void* t2, void* t3, void* t4)
    {
        Unbind();
        ((delegate* unmanaged[Stdcall]<void*, uint, void**, void*, void>)Vt(_ctx)[33])(_ctx, 1, &rtv, null);
        var vp = new Viewport { X = x, Y = y, W = w, H = h, MaxZ = 1 };
        ((delegate* unmanaged[Stdcall]<void*, uint, Viewport*, void>)Vt(_ctx)[44])(_ctx, 1, &vp);
        void** srv = stackalloc void*[] { t0, t1, t2, t3, t4, _lnSrv, _mSrv };
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
        Release(ref _hdrSrv); Release(ref _hdrRtv); Release(ref _hdrTex);
        Release(ref _b1Srv); Release(ref _b1Rtv); Release(ref _b1Tex);
        Release(ref _b2Srv); Release(ref _b2Rtv); Release(ref _b2Tex);
        Release(ref _albSrv); Release(ref _albTex); Release(ref _hSrv); Release(ref _hTex); Release(ref _iSrv); Release(ref _iTex);
        Release(ref _lnSrv); Release(ref _lnTex); Release(ref _mSrv); Release(ref _mTex);
        Release(ref _vs); Release(ref _psLight); Release(ref _psBright); Release(ref _psBlur); Release(ref _psComp);
        Release(ref _sampLin); Release(ref _cb);
        Release(ref _swap); Release(ref _ctx); Release(ref _dev);
    }

    // ---- shaders (Shader Model 4.0)
    private const string Hlsl = """
cbuffer C : register(b0)
{
    float4 Out;    // picture w, h, 1/w, 1/h
    float4 Uv;     // source uv = Uv.zw + uv * Uv.xy (overscan crop)
    float4 Lgt;    // direction to the sun (screen x right, y down, z out), bump
    float4 Hlf;    // half vector, flat diffuse
    float4 Mat;    // ambient, diffuse, specular strength, -
    float4 Fx;     // sprite lift, AO strength, shadow strength, -
    float4 Fx2;    // shadow slope, albedo sharpness, bloom threshold, bloom strength
    float4 Blur;   // blur step (uv)
    float4 Cam;    // camera x, y (NES pixels, unwrapped), time (s), -
    float4 Opt;    // parallax, shadows, atmosphere, material detail (0 / 1)
    float4 Scn;    // outdoor, void gate, gameplay, -
    float4 MeanC;  // mean playfield colour
    float4 Grade;  // -, saturation, contrast, vignette
};
Texture2D Alb : register(t0);
Texture2D Hgt : register(t1);
Texture2D Inf : register(t2);
Texture2D Src : register(t3);
Texture2D Blm : register(t4);
Texture2D Lne : register(t5);
Texture2D MatT : register(t6);
SamplerState Lin : register(s0);

static const float2 NES = float2(256, 240);
static const float3 LUM = float3(0.299, 0.587, 0.114);

struct V { float4 p : SV_Position; float2 uv : TEXCOORD0; };

V VS(uint id : SV_VertexID)
{
    V o;
    float2 t = float2((id << 1) & 2, id & 2);
    o.uv = t;
    o.p = float4(t * float2(2, -2) + float2(-1, 1), 0, 1);
    return o;
}

// ---- inputs
float4 InfoPx(float2 p) { return Inf.Load(int3(clamp(int2(floor(p)), int2(0, 0), int2(255, 239)), 0)); }
uint Flags(float4 i) { return (uint)round(i.b * 255); }
// void (pits, darkness): bilinear, so its edge follows the rounded albedo instead of NES pixel steps
float VoidS(float2 p) { return Inf.SampleLevel(Lin, p / NES, 0).a; }
bool IsVoid(float2 p) { return VoidS(p) > 0.5; }

// the height model: visible shapes (0 at their edges, 1 inside) + sprites standing above the ground
float H(float2 s)
{
    return Hgt.SampleLevel(Lin, s, 0).r + Fx.x * Inf.SampleLevel(Lin, s, 0).r;
}

// crisp albedo: bilinear only across the last fraction of a texel (no blur, no shimmer at any scale)
float3 Albedo(float2 s)
{
    float2 sz = float2(1024, 960);
    float2 t = s * sz - 0.5;
    float2 i = floor(t), f = t - i;
    f = saturate((f - 0.5) * Fx2.y + 0.5);
    return Alb.SampleLevel(Lin, (i + f + 0.5) / sz, 0).rgb;
}

// ---- noise (hash without sine; value noise on a lattice that repeats every `per` cells)
float Hash(float2 p)
{
    float3 p3 = frac(float3(p.xyx) * 0.1031);
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.x + p3.y) * p3.z);
}
float VNoise(float2 p, float2 cell, float2 per)
{
    float2 q = p / cell, i = floor(q), f = q - i;
    float2 u = f * f * (3 - 2 * f);
    float2 i0 = i - per * floor(i / per), i1 = i0 + 1;
    i1 -= per * floor(i1 / per);
    float a = Hash(i0), b = Hash(float2(i1.x, i0.y)), c = Hash(float2(i0.x, i1.y)), d = Hash(i1);
    return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
}
// world-anchored: repeats with the 512 x 480 nametable space, so it stays put while the game scrolls
float WN(float2 w, float2 cell) { return VNoise(w, cell, float2(512, 480) / cell); }
// free (camera / parallax space)
float FN(float2 p, float cell) { return VNoise(p, float2(cell, cell), float2(4096, 4096)); }

// ---- materials: procedural surface relief (0..1) per kind
float Detail(float2 w, int kind, float t)
{
    if (kind == 1)                       // stone: cracks between rounded grains
    {
        float r = 1 - abs(WN(w, float2(8, 8)) * 2 - 1);
        return 0.5 * (WN(w, float2(4, 4)) * 0.5 + WN(w, float2(2, 2)) * 0.3 + WN(w, float2(1, 1)) * 0.2) + 0.5 * (1 - r * r * r * r);
    }
    if (kind == 2)                       // water / ice: slow ripples
    {
        float2 d = float2(t * 1.5, t * 0.6);
        return 0.35 * WN(w + d, float2(16, 16)) + 0.35 * WN(w - d.yx, float2(8, 8)) + 0.3 * WN(w, float2(2, 2));
    }
    if (kind == 3)                       // plaster / cloth: fine grain
        return WN(w, float2(1, 1)) * 0.5 + WN(w, float2(0.5, 0.5)) * 0.5;
    if (kind == 4)                       // earth / wood / sand: fbm
        return WN(w, float2(4, 4)) * 0.4 + WN(w, float2(2, 2)) * 0.3 + WN(w, float2(1, 1)) * 0.2 + WN(w, float2(0.5, 0.5)) * 0.1;
    if (kind == 5)                       // foliage: blades
        return WN(w, float2(0.5, 2)) * 0.55 + WN(w, float2(2, 2)) * 0.25 + WN(w, float2(0.5, 0.5)) * 0.2;
    return 0.5;
}

// ---- parallax depth in a dark backdrop: cliff walls, then three rock layers that scroll slower than the playfield
float3 Chasm(float2 p, float t)
{
    float2 ctr = NES * 0.5;
    float2 view = (p - ctr) / ctr.y;
    // 1. cliff walls: a ledge between this point and the screen centre shows its side (perspective from the centre)
    [loop] for (int j = 1; j <= 10; j++)
    {
        float2 q = p - view * j * 0.9;
        if (!IsVoid(q))
        {
            float3 top = Albedo(q / NES);
            float f = j / 10.0;
            float lit = saturate(0.55 + 0.5 * dot(normalize(view + 1e-4), -normalize(Lgt.xy)));
            float strata = 0.8 + 0.4 * FN(float2(q.x + Cam.x, (q.y + Cam.y) * 0.3 + j * 1.7), 3);
            return top * (0.62 - 0.4 * f) * lit * strata;
        }
    }
    // 2. rock layers below (far first), lit from the sun, shadowed by the ledges above
    float3 fogC = MeanC.rgb * 0.22 + float3(0.015, 0.025, 0.05);
    float3 c = fogC * 0.4;
    [unroll] for (int k = 2; k >= 0; k--)
    {
        float depth = (k + 1) * 6.0;
        float rate = 1.0 / (1.0 + depth * 0.05);
        float2 lp = (p - ctr) * rate + Cam.xy * rate + float2(k * 97.0, k * 57.0);
        float cell = 20 + 8 * k;
        float d0 = FN(lp, cell) * 0.6 + FN(lp, 7) * 0.3 + FN(lp, 2.5) * 0.1;
        float2 lq = lp - Lgt.xy * 1.5;
        float d1 = FN(lq, cell) * 0.6 + FN(lq, 7) * 0.3 + FN(lq, 2.5) * 0.1;
        float solid = smoothstep(0.50, 0.56, d0);
        float shade = saturate(0.7 + (d0 - d1) * 7);
        float edge = saturate((d0 - d1) * 12) * (1 - smoothstep(0.56, 0.64, d0));   // lit rims of the rock edges
        float3 lc = lerp(MeanC.rgb * (0.6 - 0.13 * k) * shade * (1 + 0.9 * edge), fogC, 0.3 + 0.2 * k);
        float lit = IsVoid(p + Lgt.xy / max(Lgt.z, 0.2) * depth * 0.45) ? 1 : 1 - 0.65 * Opt.y;
        c = lerp(c, lc * lit, solid);
    }
    // 3. the ledge rim darkens the void next to it
    float rim = 0;
    [unroll] for (int r = 0; r < 8; r++)
    {
        float an = r * 0.785398;
        rim += IsVoid(p + float2(cos(an), sin(an)) * 3) ? 0 : 1;
    }
    return c * (1 - 0.6 * rim / 8);
}

// dust motes in front of the playfield (they scroll faster: closer to the eye)
float3 Motes(float2 p, float t)
{
    float2 mp = p + Cam.xy * 1.25 + float2(t * 3, sin(t * 0.3) * 4);
    float2 cell = floor(mp / 20), f = mp - cell * 20;
    float h = Hash(cell);
    float2 pos = float2(Hash(cell + 3.1), Hash(cell + 7.7)) * 16 + 2 + float2(sin(t + h * 30), cos(t * 0.8 + h * 20)) * 1.5;
    float tw = 0.5 + 0.5 * sin(t * 2 + h * 50);
    return float3(1, 0.95, 0.8) * smoothstep(1.1, 0.2, length(f - pos)) * 0.22 * tw * step(0.55, h);
}

float4 PSLight(V i) : SV_Target
{
    float2 s = Uv.zw + i.uv * Uv.xy;
    float3 a = Albedo(s);
    float2 p = s * NES;
    float4 inf = InfoPx(p);
    uint fl = Flags(inf);
    float4 ln = Lne.Load(int3(clamp(int(p.y), 0, 239), 0, 0));
    if (ln.z > 0.5 || (fl & 4) != 0) return float4(a, 0);          // HUD / text: the albedo, unlit
    float t = Cam.z;
    float3 c = a;
    bool spr = (fl & 1) != 0;
    float mv = Opt.x > 0.5 && Scn.y > 0.01 && !spr ? smoothstep(0.3, 0.7, VoidS(p)) * (1 - smoothstep(0.06, 0.18, dot(a, LUM))) : 0;
    if (mv < 0.999)
    {
        float2 w = ln.xy + float2(p.x, frac(p.y));                    // world position (nametable space)
        int col = (int)round(inf.g * 255 / 4);
        float4 m = MatT.Load(int3(col, 0, 0));
        int kind = spr ? 0 : (int)round(m.r * 255 / 40);
        // material detail
        float2 gd = 0;
        float amp = kind > 0 ? m.a * Opt.w : 0;
        if (amp > 0)
        {
            float e = 0.25;
            float d0 = Detail(w, kind, t), dx = Detail(w + float2(e, 0), kind, t), dy = Detail(w + float2(0, e), kind, t);
            gd = float2(dx - d0, dy - d0) / e * amp * 0.22;
            a *= 1 + amp * 0.55 * (d0 - 0.5);
        }
        // macro normal from the height model
        float2 dxs = float2(1.0 / 512, 0), dys = float2(0, 1.0 / 480);
        float h = H(s);
        float gx = (H(s + dxs) - H(s - dxs)) * Lgt.w, gy = (H(s + dys) - H(s - dys)) * Lgt.w;
        float3 n = normalize(float3(-gx - gd.x, -gy - gd.y, 1));
        // ambient occlusion: higher ground close by darkens
        float occ = 0;
        [unroll] for (int k = 0; k < 6; k++)
        {
            float an = k * 1.047198, an2 = an + 0.523599;
            occ += saturate(H(s + float2(cos(an), sin(an)) * float2(3.0 / 512, 3.0 / 480)) - h);
            occ += 0.6 * saturate(H(s + float2(cos(an2), sin(an2)) * float2(8.0 / 512, 8.0 / 480)) - h);
        }
        float ao = 1 - Fx.y * saturate(occ / 7.5);
        // soft shadow toward the sun; the penumbra widens with the distance to the caster
        float2 ld = normalize(Lgt.xy);
        float sh = 0;
        [loop] for (int j = 1; j <= 24; j++)
        {
            float d = j;
            float hq = H(s + float2(ld.x / 512, ld.y / 480) * d);
            sh = max(sh, saturate((hq - h - d * Fx2.x) * 2 / (0.4 + 0.06 * d)) * (1 - j / 25.0));
        }
        float shade = 1 - Fx.z * sh;
        // drifting cloud shadows outdoors
        float2 cp = p + Cam.xy + float2(t * 5, t * 2);
        float cl = FN(cp, 70) * 0.65 + FN(cp, 28) * 0.35;
        shade *= 1 - smoothstep(0.5, 0.7, cl) * 0.40 * Scn.x * Opt.z * Opt.y;
        // light: warm sun, cool sky; normalised so flat lit ground keeps its brightness
        float3 sunC = float3(1.0, 0.95, 0.86);
        float3 sky = lerp(float3(0.78, 0.86, 1.0), MeanC.rgb / max(max(MeanC.r, MeanC.g), max(MeanC.b, 0.05)), 0.2);
        float diff = saturate(dot(n, Lgt.xyz));
        float norm = dot(Mat.x * sky + Mat.y * Hlf.w * sunC, LUM);
        float3 lightC = (Mat.x * sky * ao + Mat.y * diff * sunC * shade) / norm;
        // specular per material, glints on water / ice
        float gp = 6 + (spr ? 0.3 : m.b) * 120;
        float nh = saturate(dot(n, Hlf.xyz));
        float lum = dot(a, LUM);
        float sp = max(0, pow(nh, gp) - pow(Hlf.z, gp)) * (spr ? 0.35 : m.g) * Mat.z * (0.3 + 0.7 * lum) * shade * (gp + 8) / 40;
        float3 glint = 0;
        if (kind == 2)
        {
            float2 gc = floor(w * 2), gf = w * 2 - gc;
            gc -= float2(1024, 960) * floor(gc / float2(1024, 960));
            float hg = Hash(gc);
            float tw = pow(saturate(sin(t * 3 + Hash(gc + 7) * 40)), 8);
            glint = sunC * step(0.985, hg) * tw * smoothstep(0.35, 0.0, length(gf - 0.5)) * 2.5 * shade * Opt.w;
        }
        // rim light on sprites (their lifted edges catch the sky)
        float rim = spr ? pow(saturate(1 - n.z), 1.5) * 0.7 : 0;
        c = a * lightC + sp * sunC + glint + rim * sky * (0.4 + 0.6 * lum);
        // mist outdoors (closer than the playfield)
        float2 mp = p + Cam.xy * 1.3 + float2(t * 4, t * 1.5);
        float mist = smoothstep(0.55, 0.85, FN(mp, 60) * 0.6 + FN(mp, 24) * 0.4);
        c = lerp(c, float3(0.92, 0.95, 1.0) * max(lum, 0.6), mist * 0.08 * Scn.x * Opt.z);
    }
    if (mv > 0.001) c = lerp(c, lerp(c, Chasm(p, t), Scn.y), mv);
    c += Motes(p, t) * Opt.z * Scn.z;
    return float4(c, 1);
}

float4 PSBright(V i) : SV_Target
{
    float4 h = Src.SampleLevel(Lin, i.uv, 0);
    float l = max(h.r, max(h.g, h.b));
    float k = saturate((l - Fx2.z + 0.25) / 0.5);                     // soft knee
    return float4(h.a < 0.5 ? 0 : h.rgb * k * k * max(l - Fx2.z, 0) / max(l, 1e-4) * 4, 1);
}

float4 PSBlur(V i) : SV_Target
{
    static const float w[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
    float3 c = Src.SampleLevel(Lin, i.uv, 0).rgb * w[0];
    [unroll] for (int k = 1; k < 5; k++)
    {
        c += Src.SampleLevel(Lin, i.uv + Blur.xy * k * 1.5, 0).rgb * w[k];
        c += Src.SampleLevel(Lin, i.uv - Blur.xy * k * 1.5, 0).rgb * w[k];
    }
    return float4(c, 1);
}

// filmic shoulder: identity below 0.75, highlights roll off toward 1
float3 Tone(float3 c)
{
    const float k = 0.75;
    return c < k ? c : k + (1 - k) * (1 - exp(-(c - k) / (1 - k)));
}

float4 PSComp(V i) : SV_Target
{
    float4 h = Src.SampleLevel(Lin, i.uv, 0);
    if (h.a < 0.5) return float4(saturate(h.rgb), 1);                   // HUD: untouched
    float3 c = Tone(h.rgb + Blm.SampleLevel(Lin, i.uv, 0).rgb * Fx2.w);
    c = lerp(dot(c, LUM).xxx, c, Grade.y);
    c = lerp(c, c * c * (3 - 2 * c), Grade.z);
    float2 d = i.uv - 0.5;
    c *= 1 - Grade.w * dot(d, d) * 2;
    return float4(saturate(c), 1);
}
""";
}
