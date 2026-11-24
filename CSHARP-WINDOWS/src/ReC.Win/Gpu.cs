// re-c Windows host — "HD" graphics (T18, Video > Graphics > HD (GPU)). Not in the web port.
//
// A Direct3D 11 renderer called through raw COM vtables (no packages): d3d11.dll D3D11CreateDeviceAndSwapChain and
// d3dcompiler_47.dll D3DCompile (the HLSL below is compiled once at start). It draws at the window's own resolution
// (1080p, 4K ...), not at a fixed 4x.
//
// Inputs per frame, made on the CPU by Enhance.cs in ModeHd:
//   albedo  1024x960  the Enhanced picture without baked shading (dither blend, Scale2x rounding, graphics-pack tiles)
//   height  512x480   the height "model" of every visible shape: a pillow profile from each pixel's distance to its
//                     shape's edge (the 32-bit look's field) -- built from the colours on screen only, so shapes the
//                     game hides stay flat and invisible
//   lift    256x240   sprites that stand out from what is around them stand LiftH units above the background
// Passes (full-screen triangles, no vertex buffers):
//   1. light  -> HDR (RGBA16F, picture size): normal from the height model (central differences, bilinear, so it is
//      smooth at any resolution), Lambert diffuse + Blinn-Phong specular from the top left, ambient occlusion
//      (8 taps around), soft shadows (16 steps marched toward the light through the height model: sprites throw
//      shadows on the ground and on each other), "sharp bilinear" albedo (crisp pixels, no shimmer when scaled).
//      Flat ground is exactly its albedo colour (light normalised to the flat normal, specular above the flat level).
//   2. bloom: the part above 1.0 (specular highlights) at 1/4 size, Gaussian blur horizontal + vertical;
//   3. composite into the swap chain inside the picture rectangle (Video.cs layout: aspect, crop, bars stay black).
// Any failure (no Direct3D 11, compiler missing) -> the caller falls back to Enhanced (32-bit).
using System.Runtime.InteropServices;
using System.Text;

namespace ReC.Win;

internal sealed unsafe partial class Gpu : IDisposable
{
    public const int AW = Enhancer.W, AH = Enhancer.H, HW = AW / 2, HH = AH / 2;
    public string Info { get; private set; } = "";

    // ---- tunables (cbuffer)
    private static readonly (float X, float Y, float Z) Light = Norm(-0.45f, -0.62f, 0.64f);
    private const float Bump = 1.3f, Amb = 0.50f, Kd = 0.50f, SpecK = 0.45f, SpecPow = 24, LiftH = 2.2f,
        AoK = 0.40f, ShadowK = 0.55f, ShadowSlope = 0.16f, BloomThreshold = 1.02f, BloomK = 0.40f;

    private void* _dev, _ctx, _swap, _backRtv;
    private void* _albTex, _albSrv, _hTex, _hSrv, _lTex, _lSrv;
    private void* _hdrTex, _hdrRtv, _hdrSrv, _b1Tex, _b1Rtv, _b1Srv, _b2Tex, _b2Rtv, _b2Srv;
    private void* _vs, _psLight, _psBright, _psBlur, _psComp, _sampLin, _cb;
    private int _bw, _bh, _pw, _ph;               // back buffer, picture (HDR) size

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

    private const uint FmtBgra = 87, FmtR32F = 41, FmtR8 = 61, FmtRgba16F = 10;
    private const uint BindSrv = 8, BindRtv = 0x20;
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
        MakeTex(256, 240, FmtR8, out _lTex, out _lSrv);

        var samp = new SampDesc { Filter = 0x15 /* MIN_MAG_MIP_LINEAR */, U = 3, V = 3, W = 3, MaxAniso = 1, MaxLod = float.MaxValue };
        void* s;
        Check(((delegate* unmanaged[Stdcall]<void*, SampDesc*, void**, int>)Vt(_dev)[23])(_dev, &samp, &s), "CreateSamplerState");
        _sampLin = s;
        var bd = new BufDesc { ByteWidth = 8 * 16, Usage = 2 /* DYNAMIC */, BindFlags = 4 /* CONSTANT */, CpuAccess = 0x10000 };
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

    /// <summary>this frame's albedo (0x00RRGGBB), height model and sprite lift</summary>
    public void Upload(uint[] albedo, float[] height, byte[] lift)
    {
        var upd = (delegate* unmanaged[Stdcall]<void*, void*, uint, void*, void*, uint, uint, void>)Vt(_ctx)[48];
        fixed (uint* a = albedo) upd(_ctx, _albTex, 0, null, a, AW * 4, 0);
        fixed (float* h = height) upd(_ctx, _hTex, 0, null, h, HW * 4, 0);
        fixed (byte* l = lift) upd(_ctx, _lTex, 0, null, l, 256, 0);
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

        // constants (8 x float4)
        var (hx, hy, hz) = Norm(Light.X, Light.Y, Light.Z + 1);
        float diff0 = Light.Z, spec0 = MathF.Pow(hz, SpecPow);
        float sharp = Math.Max(1f, ph / (lines * (float)Enhancer.S));
        float* c = stackalloc float[32]
        {
            pw, ph, 1f / pw, 1f / ph,
            1, lines / 240f, 0, firstLine / 240f,
            Light.X, Light.Y, Light.Z, Bump,
            hx, hy, hz, diff0,
            Amb, Kd, SpecK, SpecPow,
            LiftH, AoK, ShadowK, spec0,
            ShadowSlope, sharp, BloomThreshold, BloomK,
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
        Pass(_hdrRtv, 0, 0, pw, ph, _psLight, _albSrv, _hSrv, _lSrv, null, null);
        // 2. bloom: bright part at 1/4, blur H then V
        int bw = Math.Max(pw / 4, 1), bh = Math.Max(ph / 4, 1);
        Pass(_b1Rtv, 0, 0, bw, bh, _psBright, null, null, null, _hdrSrv, null);
        c[28] = 1f / bw; c[29] = 0; SetConstants(c);
        Pass(_b2Rtv, 0, 0, bw, bh, _psBlur, null, null, null, _b1Srv, null);
        c[28] = 0; c[29] = 1f / bh; SetConstants(c);
        Pass(_b1Rtv, 0, 0, bw, bh, _psBlur, null, null, null, _b2Srv, null);
        // 3. composite into the picture rectangle
        Pass(_backRtv, r.X, r.Y, pw, ph, _psComp, null, null, null, _hdrSrv, _b1Srv);
        Unbind();
        if (present) ((delegate* unmanaged[Stdcall]<void*, uint, uint, int>)Vt(_swap)[8])(_swap, 0, 0);
    }

    private void SetConstants(float* c)
    {
        Mapped m;
        Check(((delegate* unmanaged[Stdcall]<void*, void*, uint, uint, uint, Mapped*, int>)Vt(_ctx)[14])(_ctx, _cb, 0, 4 /* WRITE_DISCARD */, 0, &m), "Map (constants)");
        Buffer.MemoryCopy(c, m.Data, 128, 128);
        ((delegate* unmanaged[Stdcall]<void*, void*, uint, void>)Vt(_ctx)[15])(_ctx, _cb, 0);
    }

    private void Pass(void* rtv, int x, int y, int w, int h, void* ps, void* t0, void* t1, void* t2, void* t3, void* t4)
    {
        Unbind();
        ((delegate* unmanaged[Stdcall]<void*, uint, void**, void*, void>)Vt(_ctx)[33])(_ctx, 1, &rtv, null);
        var vp = new Viewport { X = x, Y = y, W = w, H = h, MaxZ = 1 };
        ((delegate* unmanaged[Stdcall]<void*, uint, Viewport*, void>)Vt(_ctx)[44])(_ctx, 1, &vp);
        void** srv = stackalloc void*[] { t0, t1, t2, t3, t4 };
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(_ctx)[8])(_ctx, 0, 5, srv);
        ((delegate* unmanaged[Stdcall]<void*, void*, void**, uint, void>)Vt(_ctx)[9])(_ctx, ps, null, 0);
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void>)Vt(_ctx)[13])(_ctx, 3, 0);
    }

    /// <summary>no texture bound as both input and target</summary>
    private void Unbind()
    {
        if (_ctx == null) return;
        void** srv = stackalloc void*[5];
        ((delegate* unmanaged[Stdcall]<void*, uint, uint, void**, void>)Vt(_ctx)[8])(_ctx, 0, 5, srv);
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
        Release(ref _albSrv); Release(ref _albTex); Release(ref _hSrv); Release(ref _hTex); Release(ref _lSrv); Release(ref _lTex);
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
    float4 Lgt;    // direction to the light (screen x right, y down, z out), bump
    float4 Hlf;    // half vector, flat diffuse
    float4 Mat;    // ambient, diffuse, specular strength, specular power
    float4 Fx;     // sprite lift, AO strength, shadow strength, flat specular
    float4 Fx2;    // shadow slope, albedo sharpness, bloom threshold, bloom strength
    float4 Blur;   // blur step (uv)
};
Texture2D Alb : register(t0);
Texture2D Hgt : register(t1);
Texture2D Lft : register(t2);
Texture2D Src : register(t3);
Texture2D Blm : register(t4);
SamplerState Lin : register(s0);

struct V { float4 p : SV_Position; float2 uv : TEXCOORD0; };

V VS(uint id : SV_VertexID)
{
    V o;
    float2 t = float2((id << 1) & 2, id & 2);
    o.uv = t;
    o.p = float4(t * float2(2, -2) + float2(-1, 1), 0, 1);
    return o;
}

// the height model: visible shapes (0 at their edges, 1 inside) + sprites standing above the ground
float H(float2 s)
{
    return Hgt.SampleLevel(Lin, s, 0).r + Fx.x * Lft.SampleLevel(Lin, s, 0).r;
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

float4 PSLight(V i) : SV_Target
{
    float2 s = Uv.zw + i.uv * Uv.xy;
    float3 a = Albedo(s);
    float2 dx = float2(1.0 / 512, 0), dy = float2(0, 1.0 / 480);
    float h = H(s);
    float gx = (H(s + dx) - H(s - dx)) * Lgt.w, gy = (H(s + dy) - H(s - dy)) * Lgt.w;
    float3 n = normalize(float3(-gx, -gy, 1));
    float diff = saturate(dot(n, Lgt.xyz));
    float f = (Mat.x + Mat.y * diff) / (Mat.x + Mat.y * Hlf.w);
    float lum = dot(a, float3(0.299, 0.587, 0.114));
    float sp = max(0, pow(saturate(dot(n, Hlf.xyz)), Mat.w) - Fx.w) * Mat.z * (0.15 + 0.85 * lum);
    // ambient occlusion: higher ground close by darkens
    float occ = 0;
    [unroll] for (int k = 0; k < 8; k++)
    {
        float an = k * 0.785398;
        occ += saturate(H(s + float2(cos(an) * 3.0 / 512, sin(an) * 3.0 / 480)) - h);
    }
    float ao = 1 - Fx.y * saturate(occ / 8);
    // soft shadow: march toward the light; anything higher than the light ray shades this point
    float2 ld = normalize(Lgt.xy);
    float sh = 0;
    [loop] for (int j = 1; j <= 16; j++)
    {
        float d = j * 1.5;
        float need = h + d * Fx2.x;
        float hq = H(s + float2(ld.x / 512, ld.y / 480) * d);
        sh = max(sh, saturate((hq - need) * 1.5) * (1 - j / 17.0));
    }
    float shade = 1 - Fx.z * sh;
    return float4(a * f * ao * shade + sp, 1);
}

float4 PSBright(V i) : SV_Target
{
    float3 c = Src.SampleLevel(Lin, i.uv, 0).rgb;
    return float4(max(c - Fx2.z, 0), 1);
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

float4 PSComp(V i) : SV_Target
{
    float3 c = Src.SampleLevel(Lin, i.uv, 0).rgb + Blm.SampleLevel(Lin, i.uv, 0).rgb * Fx2.w;
    return float4(saturate(c), 1);
}
""";
}
