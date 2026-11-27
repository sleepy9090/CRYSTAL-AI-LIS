// re-c Windows host — the HD engine's shaders (Gpu.cs, Shader Model 4.0). Not in the web port.
namespace ReC.Win;

internal sealed partial class Gpu
{
    private const string Hlsl = """
cbuffer C : register(b0)
{
    float4 Out;    // picture w, h, 1/w, 1/h
    float4 Uv;     // source uv = Uv.zw + uv * Uv.xy (overscan crop)
    float4 Lgt;    // direction to the sun (screen x right, y down, z out), bump
    float4 Hlf;    // half vector, flat diffuse
    float4 Mat;    // ambient, diffuse, specular strength, tilt-shift (T55, 0 = off)
    float4 Fx;     // sprite lift, AO strength, shadow strength, contact shadow strength
    float4 Fx2;    // shadow slope, albedo sharpness, bloom threshold, bloom strength
    float4 Blur;   // blur step (uv), jump-flood step (pixels), -
    float4 Cam;    // camera x, y (NES pixels, unwrapped), time (s), sharpening (T48, 0 = off)
    float4 Opt;    // parallax, shadows, atmosphere, material detail (0 / 1)
    float4 Scn;    // outdoor, void gate, gameplay, darkness
    float4 MeanC;  // mean playfield colour; w = smooth outlines (T52)
    float4 Grade;  // exposure (eye adaptation), saturation, contrast, vignette
    float4 Opt2;   // reflection strength, local light strength, icy scene (snow), hot scene (embers)
    float4 HudC;   // HUD panel: fill colour (-1 = none), first line, last line + 1, -
    float4 QualC;  // shadow steps, extras (reflections, fine relief) 0 / 1, albedo scale (8 / 4), on-screen display 0 / 1
    float4 MeterL; // T35 LIFE meter: x0, x1 (frame), fill end, top line (x1 = 0: none)
    float4 MeterF; // T35 FORCE meter: x0, x1, top line; w = the status bar's first line (T43, 0 = none)
    float4 Cine;   // T51 cut-scene: weight, picture y0 * 256 + y1, x0 * 1024 + x1, shot start time (s)
    float4 Dmg;    // T57 damage feedback: hit flash 0..1, low life 0 / 1, -, -
    float4 Col0, Col1, Col2;   // T60 display colour matrix rows (brightness, vivid, colour-blind assist)
    float4 Scr;    // widescreen: picture columns (256 or wider), lines (240), side width, -
};
Texture2D SrcA : register(t0);
Texture2D SrcB : register(t1);
Texture2D SrcC : register(t2);
Texture2D<uint4> Keys : register(t3);
Texture2D Inf : register(t4);
Texture2D Lne : register(t5);
Texture2D MatT : register(t6);
Texture2D Rgb : register(t7);
Texture2D<uint4> Lay : register(t8);
Texture2D<uint2> ArtM : register(t9);
Texture2D Atlas : register(t10);
Texture2D Bnc : register(t11);   // bounce light: rgb = albedo sum, a = weight (blurred)
Texture2D Gly : register(t12);   // T59 glyph atlas: 16 x 8 cells of 64 px, cell = ASCII code, coverage in r
SamplerState Lin : register(s0);

#define NES (Scr.xy)                 // the picture in NES pixels: 256 x 240, wider with the widescreen playfield
static const float3 LUM = float3(0.299, 0.587, 0.114);
#define ALB (NES * QualC.z)    // albedo size: 8x (2048x1920), 4x in fast quality

struct V { float4 p : SV_Position; float2 uv : TEXCOORD0; };

V VS(uint id : SV_VertexID)
{
    V o;
    float2 t = float2((id << 1) & 2, id & 2);
    o.uv = t;
    o.p = float4(t * float2(2, -2) + float2(-1, 1), 0, 1);
    return o;
}

// ================================================================ frame passes
// layer key words (Gpu.Layers): r = background colour | 0x40 opaque | 0x80 HUD, g = sprite colour | 0x40 present |
// 0x80 behind, b = dither partner + 1
uint4 KeyAt(int2 q, int2 sz) { return Keys.Load(int3(clamp(q, int2(0, 0), sz - 1), 0)); }

// Scale2x of the key image at output pixel o (source = o / 2): background (r + its partner b) and sprites (g) separately
uint4 Scale2(int2 o)
{
    uint w, h;
    Keys.GetDimensions(w, h);
    int2 sz = int2(w, h), q = o / 2, sub = o - q * 2;
    uint4 E = KeyAt(q, sz);
    if ((E.r & 0x80) != 0 && HudC.w < 0.5) return E;                  // HUD / text: exact pixels (T34: rounded with HUD style)
    int2 dx = int2(sub.x != 0 ? 1 : -1, 0), dy = int2(0, sub.y != 0 ? 1 : -1);
    uint4 X = KeyAt(q + dx, sz), X2 = KeyAt(q - dx, sz), Y = KeyAt(q + dy, sz), Y2 = KeyAt(q - dy, sz);
    uint4 r = E;
    uint xb = X.r | X.b << 8, yb = Y.r | Y.b << 8, x2b = X2.r | X2.b << 8, y2b = Y2.r | Y2.b << 8;   // background key: colour + dither partner
    if (xb == yb && yb != x2b && xb != y2b && (X.r & 0x80) == (E.r & 0x80)) { r.r = X.r; r.b = X.b; }
    if (X.g == Y.g && Y.g != X2.g && X.g != Y2.g) r.g = X.g;
    return r;
}

uint4 Vote(int2 o);
uint4 PSScale(V i) : SV_Target
{
    uint w, h;
    Keys.GetDimensions(w, h);
    return MeanC.w > 0.5 && w >= (uint)(NES.x * 2) ? Vote(int2(i.p.xy)) : Scale2(int2(i.p.xy));   // T52: 2x -> 4x voted too
}

// T52 smooth outlines (MeanC.w): the last 2x step as a vote instead of Scale2x. Each output pixel weighs the 4x4 key
// texels around its centre with a Gaussian (sigma 0.8 texel) and takes, separately for the background key (colour +
// dither partner + HUD bit) and the sprite key, the one with the most weight: the stair steps of Scale2x become smooth
// curves and diagonals; features a texel or more wide stay
uint4 Vote(int2 o)
{
    uint w, h;
    Keys.GetDimensions(w, h);
    int2 sz = int2(w, h);
    float2 c = (float2(o) + 0.5) * 0.5 - 0.5;
    int2 b = int2(floor(c)) - 1;
    uint bk[16], sk[16];
    float wt[16];
    uint4 E = KeyAt(o / 2, sz);
    if ((E.r & 0x80) != 0) return Scale2(o);                       // HUD / text: as before
    [loop] for (int i = 0; i < 16; i++)
    {
        int2 q = b + int2(i & 3, i >> 2);
        uint4 k = KeyAt(q, sz);
        float2 d = c - float2(q);
        wt[i] = exp(-dot(d, d) * 0.78);
        bk[i] = k.r | k.b << 8;
        sk[i] = k.g;
    }
    uint bb = E.r | E.b << 8, bs = E.g;
    float bw = -1, sw = -1;
    [loop] for (int j = 0; j < 16; j++)
    {
        float vb = 0, vs = 0;
        [loop] for (int m = 0; m < 16; m++)
        {
            vb += bk[m] == bk[j] ? wt[m] : 0;
            vs += sk[m] == sk[j] ? wt[m] : 0;
        }
        if (vb > bw) { bw = vb; bb = bk[j]; }
        if (vs > sw) { sw = vs; bs = sk[j]; }
    }
    if ((E.r & 0x80) != (bb & 0x80)) bb = E.r | E.b << 8;          // never move the HUD edge
    return uint4(bb & 0xFF, bs, bb >> 8, E.a);
}

float3 NesRgb(uint c) { return Rgb.Load(int3(c & 63, 0, 0)).rgb; }
bool SprFront(uint4 k) { return (k.g & 0x40) != 0 && !((k.g & 0x80) != 0 && (k.r & 0x40) != 0); }

// a graphics-pack tile at position sub (0..1) inside its NES pixel: bilinear inside the tile's 32x32 cell only
float4 ArtSample(uint a, float2 sub)
{
    uint slot = (a & 0x7FFF) - 1;
    float2 cr = float2((a >> 15) & 7, (a >> 18) & 7);
    float2 f = float2(((a >> 21) & 1) != 0 ? 1 - sub.x : sub.x, ((a >> 22) & 1) != 0 ? 1 - sub.y : sub.y);
    float2 t = clamp((cr + f) * 4, 0.5, 31.5);
    uint aw, ah;
    Atlas.GetDimensions(aw, ah);
    uint n = aw / 32;
    float2 org = float2(slot % n, slot / n) * 32;
    return Atlas.SampleLevel(Lin, (org + t) / aw, 0);
}

// T27: a pack tile in a palette fade step (art word bits 23-24): its painted colours follow the faded NES colour on
// screen, keeping their light / shade relative to the colour they were painted for (the step's $10 lighter one)
float3 Faded(float3 c, uint a, uint col)
{
    uint k = (a >> 23) & 3;
    col &= 63;
    if (k == 0) return c;
    if (col == 0x0F) return NesRgb(0x0F);
    float ls = dot(NesRgb(min(col + 0x10 * k, 0x3F)), LUM);
    return NesRgb(col) * (ls > 0.03 ? clamp(dot(c, LUM) / ls, 0, 2.5) : 1);
}

// T33 seams: pack tiles are painted one by one, so a tile's edge need not meet its neighbour's. Where the NES pixels on
// both sides of a tile edge have the same colour (the game's picture is continuous there), the last NES pixel before the
// edge blends toward the neighbour's art at the shared edge: both sides reach the same 50 / 50 mix at the edge, so the
// seam closes. Real edges (another colour across them) stay crisp. layer 0 = background, 1 = sprite
float3 ArtAt(int2 n, float2 sub, uint layer, uint col)
{
    uint2 aw = ArtM.Load(int3(n, 0));
    uint a = layer == 0 ? aw.x : aw.y;
    if ((a & 0x7FFF) == 0) return float3(-1, -1, -1);
    float4 s = ArtSample(a, sub);
    return s.a >= 0.5 ? Faded(s.rgb, a, col) : float3(-1, -1, -1);
}
float3 Seams(float3 c, int2 n, float2 sub, uint a, uint layer, uint col)
{
    // the pixel's place in its tile on screen (the art word's column / row, mirrored back by the flips)
    float cx = (a >> 15) & 7, cy = (a >> 18) & 7;
    if (((a >> 21) & 1) != 0) cx = 7 - cx;
    if (((a >> 22) & 1) != 0) cy = 7 - cy;
    float2 tp = float2(cx, cy) + sub;
    int2 dirs[4] = { int2(-1, 0), int2(1, 0), int2(0, -1), int2(0, 1) };
    float dist[4] = { tp.x, 8 - tp.x, tp.y, 8 - tp.y };
    float3 r = c;
    [loop] for (int k = 0; k < 4; k++)
    {
        if (dist[k] >= 1) continue;
        int2 m = n + dirs[k];
        if (m.x < 0 || m.y < 0 || m.x > (int)NES.x - 1 || m.y > 239) continue;
        uint4 L = Lay.Load(int3(m, 0));
        if ((L.r & 0x80) != 0) continue;
        uint mc = layer == 0 ? (L.r & 63) : ((L.g & 0x40) != 0 ? (L.g & 63) : 999);
        if (mc != col) continue;
        // the neighbour's sub-pixel at the shared edge, same place along it
        float2 es = k == 0 ? float2(1, sub.y) : k == 1 ? float2(0, sub.y) : k == 2 ? float2(sub.x, 1) : float2(sub.x, 0);
        float3 o = ArtAt(m, es, layer, col);
        if (o.x < 0) continue;
        r = lerp(r, o, 0.5 * (1 - dist[k]));
    }
    return r;
}

// the albedo at 8x: Scale2x level 3 on the 4x keys, pack tiles, dither blend, sprite over background
float4 PSAlbedo(V i) : SV_Target
{
    int sc = (int)QualC.z;
    int2 o = int2(i.p.xy), n = o / sc;
    uint4 L = Lay.Load(int3(n, 0));
    bool ui = (L.r & 0x80) != 0;
    uint4 k = ui && HudC.w < 0.5 ? L : MeanC.w > 0.5 ? Vote(o) : Scale2(o);
    float2 sub = (float2(o - n * sc) + 0.5) / sc;
    float3 bg = NesRgb(k.r);
    if (k.b != 0) bg = (bg + NesRgb(k.b - 1)) * 0.5;
    uint2 art = ArtM.Load(int3(n, 0));
    // T59 modern font: a text-font glyph (art word bit 31) from the atlas, in the tile's ink / paper colours
    if ((art.x >> 31) != 0)
    {
        float2 gp = (float2((art.x >> 7) & 7, (art.x >> 10) & 7) + sub) / 8;
        uint code = art.x & 127;
        float cov = Gly.SampleLevel(Lin, (float2(code % 16, code / 16) + gp) / float2(16, 8), 0).r;
        bg = lerp(NesRgb((art.x >> 19) & 63), NesRgb((art.x >> 13) & 63), cov);
    }
    if (!ui && (art.x & 0x7FFF) != 0)
    {
        float4 a = ArtSample(art.x, sub);
        if (a.a >= 0.5) bg = Seams(Faded(a.rgb, art.x, k.r), n, sub, art.x, 0, L.r & 63);
    }
    float3 c = bg;
    if (SprFront(k))
    {
        c = NesRgb(k.g);
        if (!ui && (art.y & 0x7FFF) != 0)
        {
            float4 a = ArtSample(art.y, sub);
            if (a.a >= 0.5) c = Seams(Faded(a.rgb, art.y, k.g), n, sub, art.y, 1, L.g & 63);
        }
    }
    return float4(c, 1);
}

// ---- height: jump flood toward the nearest colour edge of the 2x keys
uint KeyC(uint4 k) { return SprFront(k) ? (k.g | 0x100) : ((k.r & 63) | (k.b << 9)); }

float4 PSSeed(V i) : SV_Target
{
    uint w, h;
    Keys.GetDimensions(w, h);
    int2 sz = int2(w, h), o = int2(i.p.xy);
    uint c = KeyC(KeyAt(o, sz));
    bool edge = KeyC(KeyAt(o + int2(0, -1), sz)) != c || KeyC(KeyAt(o + int2(0, 1), sz)) != c ||
                (o.x > 0 && KeyC(KeyAt(o + int2(-1, 0), sz)) != c) || (o.x < sz.x - 1 && KeyC(KeyAt(o + int2(1, 0), sz)) != c);
    return edge ? float4(float2(o), 0, 1) : float4(-10000, -10000, 0, 0);
}

float4 PSJfa(V i) : SV_Target
{
    float2 o = floor(i.p.xy);
    float2 best = SrcA.Load(int3(o, 0)).xy;
    float bd = dot(o - best, o - best);
    [loop] for (int y = -1; y <= 1; y++)
        [loop] for (int x = -1; x <= 1; x++)
        {
            float2 q = o + float2(x, y) * Blur.z;
            bool inside = q.x >= 0 && q.y >= 0 && q.x < NES.x * 2 && q.y < 480;
            float2 s = SrcA.Load(int3(clamp(q, float2(0, 0), float2(511, 479)), 0)).xy;
            float d = dot(o - s, o - s);
            bool take = inside && s.x > -1000 && d < bd;          // selects, no branches (unrolled loop)
            bd = take ? d : bd;
            best = take ? s : best;
        }
    return float4(best, 0, 1);
}

// pillow profile of 1 + the distance to the nearest edge (cap 5), blurred 3x3 (= the 32-bit look's height field)
float Pillow(float2 o)
{
    o = clamp(o, float2(0, 0), float2(511, 479));
    float2 s = SrcA.Load(int3(o, 0)).xy;
    float d = s.x > -1000 ? length(o - s) : 99;
    float t = 1 - min(1 + d, 5) / 5;
    return 1 - t * t;
}

float4 PSHeight(V i) : SV_Target
{
    float2 o = floor(i.p.xy);
    float acc = 0;
    [loop] for (int y = -1; y <= 1; y++)
        [loop] for (int x = -1; x <= 1; x++) acc += Pillow(o + float2(x, y));
    return float4(acc / 9, 0, 0, 1);
}

// ---- light map: emissive colours (red / orange / yellow fire tones) of the playfield, 2x2 NES pixels per texel
float Emissive(uint c)
{
    c &= 63;
    if (c == 0x27 || c == 0x28) return 1;
    if (c == 0x16 || c == 0x26 || c == 0x37) return 0.55;
    return 0;
}

// bounce light source: the playfield albedo averaged per 2x2 NES pixels (HUD left out), weight in alpha
float4 PSBounce(V i) : SV_Target
{
    int2 o = int2(i.p.xy) * 2;
    float4 acc = 0;
    [loop] for (int y = 0; y < 2; y++)
        [loop] for (int x = 0; x < 2; x++)
        {
            int2 n = o + int2(x, y);
            float wgt = (Lay.Load(int3(n, 0)).r & 0x80) != 0 ? 0 : 1;
            acc += float4(SrcA.SampleLevel(Lin, (float2(n) + 0.5) / NES, 0).rgb, 1) * wgt;
        }
    return acc / 4;
}

float4 PSEmit(V i) : SV_Target
{
    int2 o = int2(i.p.xy) * 2;
    float3 acc = 0;
    [loop] for (int y = 0; y < 2; y++)
        [loop] for (int x = 0; x < 2; x++)
        {
            uint4 L = Lay.Load(int3(o + int2(x, y), 0));
            uint c = SprFront(L) ? L.g : L.r;
            acc += (L.r & 0x80) != 0 ? 0 : NesRgb(c) * Emissive(c);
            // characters carry a little warm light (visible in dark places; hidden sprites have no lift)
            acc += float3(1.0, 0.8, 0.55) * Inf.Load(int3(o + int2(x, y), 0)).r * 0.35;
        }
    return float4(acc / 4, 1);
}

// ================================================================ draw passes
float4 InfoPx(float2 p) { return Inf.Load(int3(clamp(int2(floor(p)), int2(0, 0), int2((int)NES.x - 1, 239)), 0)); }
uint Flags(float4 i) { return (uint)round(i.b * 255); }
// void (pits, darkness): bilinear, so its edge follows the rounded albedo instead of NES pixel steps
float VoidS(float2 p) { return Inf.SampleLevel(Lin, p / NES, 0).a; }
bool IsVoid(float2 p) { return VoidS(p) > 0.5; }
int KindAt(float2 p)
{
    float4 inf = InfoPx(p);
    if ((Flags(inf) & 7) != 0) return 0;                              // sprites, void, HUD
    return (int)round(MatT.Load(int3((int)round(inf.g * 255 / 4), 0, 0)).r * 255 / 40);
}

// the height model: visible shapes (0 at their edges, 1 inside) + sprites standing above the ground
float H(float2 s)
{
    return SrcB.SampleLevel(Lin, s, 0).r + Fx.x * Inf.SampleLevel(Lin, s, 0).r;
}

// crisp albedo: bilinear only across the last fraction of a texel (no blur, no shimmer at any scale)
float3 Albedo(float2 s)
{
    float2 t = s * ALB - 0.5;
    float2 i = floor(t), f = t - i;
    f = saturate((f - 0.5) * Fx2.y + 0.5);
    return SrcA.SampleLevel(Lin, (i + f + 0.5) / ALB, 0).rgb;
}

// local lights come from a direction: toward where the light map gets brighter, so bumps facing a fire are lit
float LocalDiffuse(float2 s, float3 n, float3 lm)
{
    float2 le = float2(2.0 / (NES.x / 2), 2.0 / 120);
    float gx = dot(SrcC.SampleLevel(Lin, s + float2(le.x, 0), 0).rgb - SrcC.SampleLevel(Lin, s - float2(le.x, 0), 0).rgb, LUM);
    float gy = dot(SrcC.SampleLevel(Lin, s + float2(0, le.y), 0).rgb - SrcC.SampleLevel(Lin, s - float2(0, le.y), 0).rgb, LUM);
    float3 l = normalize(float3(gx, gy, 0.08 + dot(lm, LUM) * 0.5));
    return 0.35 + 0.9 * saturate(dot(n, l));
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

// ---- T33 plain ground: the flat fills of earth / sand / wood (kind 4), plaster / cloth (3) and stone (1) without pack
// art (the game's one-colour tiles) get wear: mottled stains, fine grain and specks; outdoor earth also small pebbles lit
// from the sun. World-anchored like the rest of the detail. PlainW: 1 off pack tiles, in a pack tile only near its NES colour
float PlainW(float2 s, int col)
{
    if (ArtM.Load(int3(clamp(int2(s * NES), int2(0, 0), int2((int)NES.x - 1, 239)), 0)).x == 0) return 1;
    return 1 - smoothstep(0.06, 0.16, length(Albedo(s) - NesRgb(col)));
}
float3 Ground(float3 a, float2 s, float2 w, int kind, int col)
{
    // only the game's own plain tiles: painted pack tiles keep their colours (ice highlights are NES white = "plaster")
    if (ArtM.Load(int3(clamp(int2(s * NES), int2(0, 0), int2((int)NES.x - 1, 239)), 0)).x != 0) return a;
    float wl = kind == 3 && col >= 0x30 && Opt2.z > 0.3 ? 0.3 : 1;   // snow in icy places: only a trace
    // very saturated fills (NES magenta, red) a little less neon
    float3 g = a;
    float sat = max(a.r, max(a.g, a.b)) - min(a.r, min(a.g, a.b));
    g = lerp(dot(a, LUM).xxx, g, 1 - 0.22 * smoothstep(0.45, 0.8, sat));
    // stains: large soft patches, a touch darker and warmer (earth) or just darker (cloth, stone)
    float st = WN(w, float2(23, 23)) * 0.6 + WN(w, float2(9, 9)) * 0.4;
    float3 stainC = kind == 4 ? float3(0.86, 0.80, 0.72) : kind == 3 ? float3(0.90, 0.88, 0.90) : float3(0.84, 0.86, 0.88);
    g *= lerp(float3(1.05, 1.04, 1.02), stainC, smoothstep(0.35, 0.8, st));
    // grain
    g *= 1 + (WN(w, float2(0.5, 0.5)) - 0.5) * (kind == 3 ? 0.07 : 0.12);
    // specks: dark grit and a few bright grains
    float2 sc = floor(w * 0.5);
    float hs = Hash(sc + 41.7);
    float2 so = frac(w * 0.5) - 0.5 - (float2(Hash(sc + 2.3), Hash(sc + 8.9)) - 0.5) * 0.5;
    float sd = 1 - smoothstep(0.12, 0.3, length(so));
    g *= 1 - sd * (hs > 0.93 ? 0.28 : 0) + sd * (hs < 0.03 ? 0.18 : 0);
    // pebbles (outdoor earth): one in ~7 cells of 5 x 5 pixels, a rounded stone with a lit top and a shadow below
    if (kind == 4 && Scn.x > 0.3)
    {
        float2 pc = floor(w / 5), po = frac(w / 5) * 5 - 2.5;
        float hp = Hash(pc + 13.1);
        if (hp > 0.86)
        {
            float2 ctr = (float2(Hash(pc + 4.4), Hash(pc + 7.7)) - 0.5) * 2.0;
            float rad = 0.7 + 0.6 * Hash(pc + 1.9);
            float2 q = (po - ctr) / rad;
            q.x *= 0.85 + 0.3 * Hash(pc + 6.1);
            float r = length(q);
            float sh = 1 - smoothstep(0.7, 1.3, length(q - float2(0.35, 0.55)));      // shadow, down-right
            g *= 1 - 0.3 * sh * Scn.x * step(1.0, r);
            if (r < 1)
            {
                float3 nrm = normalize(float3(q, sqrt(saturate(1 - r * r)) + 0.2));
                float lit = saturate(dot(nrm, normalize(float3(-0.5, -0.7, 0.6))));
                float3 stone = dot(a, LUM) * float3(1.05, 1.0, 0.95) * (0.75 + 0.2 * Hash(pc + 3.3));
                g = lerp(g, stone * (0.6 + 0.65 * lit), 1 - smoothstep(0.85, 1.0, r));
            }
        }
    }
    return lerp(a, g, wl * Opt.w);
}

// ---- T32 lawns: foliage-coloured background without pack art (the game's flat grass tiles and their generated look) gets
// warm / cool patches, blade strokes that lean with the wind and a few small flowers; pack tiles keep their own painting
// except where it is the plain grass colour
float3 Lawn(float3 a, float2 s, float2 w, float t, int col)
{
    float wl = PlainW(s, col);            // in a pack tile only its plain grass (the fill between fence posts)
    if (wl <= 0) return a;
    // patches: sunny dry grass and deep cool clumps
    float patch = WN(w, float2(28, 28)) * 0.6 + WN(w, float2(11, 11)) * 0.4;
    // (tints toward target colours by brightness: the NES greens are so saturated that a multiply barely shows)
    float l = dot(a, LUM);
    float3 base = lerp(float3(l, l, l), a, 0.8);                      // a little less neon
    float3 warm = l * float3(1.25, 1.18, 0.42), cool = l * float3(0.42, 0.95, 0.70) * 0.85;
    float pw = smoothstep(0.45, 0.8, patch), pc = 1 - smoothstep(0.2, 0.5, patch);
    float3 g = lerp(lerp(base, warm, pw * 0.45), cool, pc * 0.4);
    // blades: thin vertical strokes (fine in x, long in y), leaning with a slow gust
    float lean = sin(t * 1.1 + w.y * 0.05 + w.x * 0.02) * 0.35;
    float2 bw = float2(w.x + (frac(w.y / 3) - 0.5) * lean, w.y);
    float bl = WN(bw, float2(0.55, 3.2)), tip = WN(bw + 17.3, float2(0.7, 2.1));
    g *= 1 - 0.3 * smoothstep(0.58, 0.9, bl);
    g *= 1 + 0.2 * smoothstep(0.68, 0.95, tip);
    // flowers: one cell in ~40 of 6 x 6 pixels, a soft round blossom in white / yellow / pink
    float2 fc = floor(w / 6), fo = frac(w / 6) - 0.5;
    float h = Hash(fc + 3.7);
    if (h > 0.975)
    {
        float2 ctr = float2(Hash(fc + 9.1), Hash(fc + 5.3)) * 0.5 - 0.25;
        float r = length((fo - ctr) * 6);
        float hue = frac(h * 97);
        float3 fl = hue < 0.4 ? float3(1, 0.97, 0.9) : hue < 0.75 ? float3(1, 0.86, 0.3) : float3(1, 0.6, 0.75);
        float2 pd = (fo - ctr) * 6;
        float petal = 0.75 + 0.3 * cos(atan2(pd.y, pd.x) * 5 + h * 40);        // five petals
        g *= 1 - 0.3 * (smoothstep(0.8, 1.0, r) - smoothstep(1.0, 1.5, r));     // a little shade round it
        g = lerp(g, fl, (1 - smoothstep(petal - 0.15, petal, r)) * 0.95);
        g = lerp(g, float3(1, 0.8, 0.2), 1 - smoothstep(0.2, 0.32, r));          // centre
    }
    return lerp(a, g, wl * Opt.w);
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

// one particle per cell of a world grid that moves with time (vel, NES px / s), seen at camera rate `rate` (parallax:
// > 1 = closer than the playfield); returns (coverage, cell hash)
float2 Particle(float2 p, float rate, float cell, float2 vel, float t, float size, float sway)
{
    float2 q = p + Cam.xy * rate - vel * t;
    float2 ci = floor(q / cell), f = q - ci * cell;
    float h = Hash(ci + rate * 17);
    float2 pos = float2(Hash(ci + 3.1), Hash(ci + 7.7)) * (cell - 8) + 4;
    pos.x += sin(t * 1.3 + h * 40) * sway;
    return float2(smoothstep(size, size * 0.3, length(f - pos)), h);
}

float3 Weather(float2 p, float t)
{
    float3 c = 0;
    if (Opt2.z > 0.01)                                                // snow / ice crystals: two layers, near = bigger, faster
    {
        float2 a = Particle(p, 1.2, 12, float2(-4, 18), t, 1.2, 2);
        float2 b = Particle(p, 1.45, 18, float2(-6, 30), t, 1.9, 2);
        c += float3(0.9, 0.95, 1.0) * (a.x * step(0.3, a.y) * 0.65 + b.x * step(0.45, b.y) * 0.8) * Opt2.z;
    }
    if (Opt2.w > 0.01)                                                // embers rising, flickering
    {
        float2 e = Particle(p, 1.15, 14, float2(3, -22), t, 1.1, 2);
        c += float3(1.6, 0.6, 0.15) * e.x * step(0.5, e.y) * (0.6 + 0.4 * sin(t * 9 + e.y * 60)) * Opt2.w;
    }
    return c;
}

// water / ice: what stands above the shore, mirrored and rippled (marches up to the first non-water pixel). Sprites
// (people, enemies) reflect clearly; scenery only faintly and softened (a ragged shore would mirror into streaks)
float4 Reflect(float2 p, float t)
{
    [loop] for (int j = 1; j <= 28; j++)
    {
        float2 q = float2(p.x, p.y - j);
        if (q.y < 0) break;
        if (KindAt(q) == 2) continue;
        if (Lne.Load(int3(clamp(int(q.y), 0, 239), 0, 0)).z > 0.5 || IsVoid(q)) break;
        float sy = floor(q.y) + 1;                                     // the shore line
        float2 r = float2(p.x + sin(p.y * 1.7 + t * 2.3) * 0.6, 2 * sy - p.y - 0.5);
        if (r.y < 0 || Lne.Load(int3(clamp(int(r.y), 0, 239), 0, 0)).z > 0.5) break;
        float fade = 1 - (p.y - sy) / 28;
        float spr = Inf.SampleLevel(Lin, r / NES, 0).r;               // sprite lift there (0..1)
        float3 c = (Albedo(r / NES) * 2 + Albedo((r + float2(1.5, 0)) / NES) + Albedo((r - float2(1.5, 0)) / NES)) / 4;
        return float4(c, fade * fade * lerp(0.12, 1, spr));
    }
    return 0;
}

// T29: the HUD panel's fill colour gets a soft gloss (lighter band near the top, darker toward the bottom) and a 1-pixel
// drop shadow under text and frame pixels; every text / frame pixel itself stays exactly its NES colour
// T34 HUD style (HudC.w): the glyph mask = HUD pixels whose colour is not the panel fill (letters, digits, meters, the
// frame), from the rounded albedo, so the shapes are the smooth Scale2x ones
float HudMask(float2 s, float3 fc) { return length(Albedo(s) - fc) > 0.1 ? 1 : 0; }

// ---- T35 HUD meters (HUD style): LIFE = a glossy capsule (dark glass trough, red fill to the HP the game draws, a cell
// mark every 16 HP, a shine sweeping along, a pulse when low); FORCE = the game's chevrons / segments (rounded) as dark
// bevelled glass, glowing when lit. Geometry from Gpu.Meters (MeterL / MeterF, NES pixels)
float SdRound(float2 q, float2 he, float r)
{
    float2 d = abs(q) - he + r;
    return length(max(d, 0)) + min(max(d.x, d.y), 0) - r;
}
float3 PanelFill(float2 p, float2 s, float3 fc, float t)
{
    // the panel: glossy top, a brushed sheen, a soft drop shadow under the glyphs (down-right, 4 taps)
    float2 tx = 1.0 / ALB;
    float g = 1.12 - 0.24 * t + 0.10 * exp(-pow((t - 0.10) * 9, 2));
    g *= 1 + 0.025 * (FN(float2(p.x * 0.15, p.y * 6), 1) - 0.5);
    float2 so = float2(0.9, 0.9) / NES;
    float sh = HudMask(s - so, fc) * 0.4 + HudMask(s - so + float2(tx.x * 3, 0), fc) * 0.2 +
               HudMask(s - so + float2(0, tx.y * 3), fc) * 0.2 + HudMask(s - so * 1.6, fc) * 0.2;
    return saturate(fc * g * (1 - 0.45 * sh));
}
float4 LifeMeter(float2 p, float3 panel)
{
    if (MeterL.y <= 0) return 0;
    float x0 = MeterL.x, w = MeterL.y - MeterL.x, fx = MeterL.z - MeterL.x;
    float2 q = p - float2(x0, MeterL.w);
    if (q.x < -1 || q.x > w + 1 || q.y < -1 || q.y > 8) return 0;
    float2 c = float2(w * 0.5, 3.5);
    float d = SdRound(q - c, float2(w * 0.5, 3.5), 3.2);
    float cov = 1 - smoothstep(-0.18, 0.18, d);
    if (cov <= 0) return float4(panel, 1);                             // the NES frame's square corners: panel
    float3 rim = lerp(float3(0.02, 0.03, 0.07), float3(0.70, 0.78, 0.92), saturate(1 - q.y / 1.1) * 0.85);
    float ci = 1 - smoothstep(-0.15, 0.15, SdRound(q - c, float2(w * 0.5 - 0.9, 2.6), 2.4));
    float ty = saturate((q.y - 0.9) / 5.2);
    float3 trough = lerp(float3(0.02, 0.03, 0.07), float3(0.09, 0.11, 0.19), ty);
    float cf = 1 - smoothstep(fx - 0.25, fx + 0.25, q.x);
    float3 red = lerp(float3(1.0, 0.62, 0.52), float3(0.93, 0.13, 0.10), smoothstep(0.0, 0.45, ty));
    red = lerp(red, float3(0.42, 0.03, 0.05), smoothstep(0.55, 1.0, ty));
    float ratio = saturate(fx / max(w - 2, 1));
    if (ratio < 0.25) red *= 1 + 0.3 * (0.5 + 0.5 * sin(Cam.z * 7));     // low life: a heartbeat
    float sweep = frac(Cam.z * 0.22) * (w + 24) - 12;                    // a shine sweeping along
    red += 0.35 * exp(-pow((q.x - sweep - q.y * 0.8) * 0.55, 2)) * float3(1, 0.85, 0.75) * cf;
    float cm = frac((p.x - 48) / 8);                                     // a cell mark every 16 HP
    red *= 1 - 0.22 * (1 - smoothstep(0.0, 0.07, min(cm, 1 - cm))) * step(48.5, p.x);
    float3 tip = lerp(trough, red, cf) + 0.25 * exp(-pow((q.x - fx) * 2.5, 2)) * float3(1, 0.6, 0.5) * step(0.5, fx);
    return float4(lerp(panel, lerp(rim, tip, ci), cov), 1);
}
float4 ForceMeter(float2 p, float2 s, float3 fc)
{
    if (MeterF.y <= 0) return 0;
    float2 q = p - float2(MeterF.x, MeterF.z);
    if (q.x < -0.5 || p.x > MeterF.y + 0.5 || q.y < 0 || q.y >= 8) return 0;
    if (HudMask(s, fc) < 0.5) return 0;                                  // the panel between: normal path
    float3 a = Albedo(s);
    float2 tx = 1.0 / ALB, ox = float2(tx.x * 1.5, 0), oy = float2(0, tx.y * 1.5);
    float bevel = ((HudMask(s + ox, fc) - HudMask(s - ox, fc)) + (HudMask(s + oy, fc) - HudMask(s - oy, fc))) * 0.5;
    float top = 1 - HudMask(s - float2(0, 0.6 / NES.y), fc);
    float ty = saturate((q.y - 1) / 6);
    float3 col;
    int nc = (int)round(InfoPx(p).g * 255 / 4);                        // the NES colour decides (rounded edges blend)
    if (nc != 0x0F && nc != 0x0D && nc != 0x1D && nc != (int)round(HudC.x))
    {
        // lit: the game's colour as glowing energy (white -> electric cyan), a pulse running along
        float3 nr = NesRgb((uint)nc);
        float sat = max(nr.r, max(nr.g, nr.b)) - min(nr.r, min(nr.g, nr.b));
        float3 hue = sat < 0.15 ? float3(0.25, 0.85, 1.0) : nr;
        col = lerp(float3(0.95, 1, 1), hue, smoothstep(0.1, 0.7, ty)) * (1 + 0.18 * sin(Cam.z * 9 - p.x * 0.45));
        col += 0.3 * top;
    }
    else
        col = lerp(float3(0.13, 0.17, 0.30), float3(0.02, 0.03, 0.07), ty) + (0.4 * saturate(bevel) + 0.25 * top) * float3(0.55, 0.7, 1);
    return float4(saturate(col), 1);
}

float3 HudPanel(float3 a, float2 p, float2 s, float4 inf)
{
    int fill = (int)round(HudC.x);
    bool rows = p.y >= HudC.y && p.y < HudC.z;
    if (fill < 0) return a;
    // T39: with HUD style, font tiles anywhere (menu windows: equipment, status, shops) get the same look
    if (!rows && (HudC.w < 0.5 || (Flags(inf) & 4) == 0)) return a;
    if (rows && Scr.z > 0 && (Flags(inf) & 4) == 0) return a;   // playtest 6: widescreen map behind the HUD is world
    float3 fc = NesRgb((uint)fill);
    float t = rows ? saturate((p.y - HudC.y) / max(HudC.z - HudC.y, 1)) : 0.15 + 0.6 * saturate(p.y / 240);
    bool isFill = length(a - fc) < 0.1;
    bool black = fill == 0x0F;
    if (HudC.w < 0.5)
    {
        // plain (T29): gloss on the fill, a 1-pixel drop shadow under the text
        if (black) return a;                                           // (T41: black panels stay exact)
        if ((int)round(inf.g * 255 / 4) != fill) return a;
        float g0 = 1.10 - 0.20 * t + 0.08 * exp(-pow((t - 0.12) * 9, 2));
        float4 q = InfoPx(p - float2(1, 1));
        bool shade = (Flags(q) & 4) != 0 && (int)round(q.g * 255 / 4) != fill && p.y - 1 >= HudC.y;
        return saturate(a * g0 * (shade ? 0.7 : 1));
    }
    float2 tx = 1.0 / ALB;                                            // one albedo texel
    if (black)
    {
        // T41 text on black (story, cut scenes): the glyph look below, a faint cool glow around the letters
        if (isFill)
        {
            float2 g1 = float2(0.8, 0) / NES, g2 = float2(0, 0.8) / NES;
            float glow = (HudMask(s + g1, fc) + HudMask(s - g1, fc) + HudMask(s + g2, fc) + HudMask(s - g2, fc)) * 0.25;
            return a + glow * 0.10 * float3(0.6, 0.75, 1);
        }
    }
    else
    {
    float3 panel = PanelFill(p, s, fc, t);
    float4 mt = LifeMeter(p, panel);
    if (mt.a > 0) return mt.rgb;
    mt = ForceMeter(p, s, fc);
    if (mt.a > 0) return mt.rgb;
    if (isFill) return panel;
    }
    // glyphs: bevel lit from the top left (gradient of the mask 1.5 texels around) + a metallic sheen, brighter near the
    // glyph's top edge and darker near its bottom edge (half an NES pixel up / down)
    float2 ox = float2(tx.x * 1.5, 0), oy = float2(0, tx.y * 1.5);
    float gx = HudMask(s + ox, fc) - HudMask(s - ox, fc), gy = HudMask(s + oy, fc) - HudMask(s - oy, fc);
    float bevel = (gx + gy) * 0.5;
    float2 r2 = float2(0, 0.5 / NES.y);
    float top = 1 - HudMask(s - r2, fc), bottom = 1 - HudMask(s + r2, fc);
    float l = dot(a, LUM);
    float3 c = a * (1 + 0.22 * bevel + 0.12 * top);
    c *= lerp(float3(1, 1, 1), float3(0.76, 0.83, 0.95), bottom);          // cool, darker lower edge: brushed metal
    c += (0.10 * saturate(bevel) + 0.05 * top) * (1 - l) * float3(0.85, 0.92, 1);   // dark glyphs catch a little light too
    return saturate(c);
}

// T43 the status bar's shadow factor at p (1 = none): black from the last tile row above the panel, fading in over 4 lines;
// pixels on or below the panel's first line are not shaded (font tiles in the junk row take the HUD path, so both use it)
float BarShadow(float2 p)
{
    if (HudC.w < 0.5 || MeterF.w <= 0) return 1;
    float top = floor(MeterF.w / 256), rs = MeterF.w - top * 256;
    if (p.y >= top) return 1;
    float u = saturate((rs - p.y) / 4);
    return u * u * (3 - 2 * u);
}


// T51 cut-scenes as film shots: is this NES pixel inside the picture (y0, y1, x0, x1)?
float4 CineRect()
{
    float y0 = floor(Cine.y / 256), x0 = floor(Cine.z / 1024);
    return float4(y0, Cine.y - y0 * 256, x0, Cine.z - x0 * 1024);
}
bool InCine(float2 p)
{
    float4 b = CineRect();
    return Cine.x > 0 && p.y >= b.x && p.y < b.y && p.x >= b.z && p.x < b.w;
}

// the camera: a slow push-in (zoom 1 -> 1.10 over 10 s, eased) drifting toward one side (per shot), and 2.5D parallax:
// higher ground (the engine's height field) moves more than the background as the camera travels
float2 CineCam(float2 p)
{
    float4 b = CineRect();
    float u = Cam.z - Cine.w, e = smoothstep(0, 10, u) * Cine.x;
    float dir = Hash(float2(Cine.w, 3.7)) > 0.5 ? 1 : -1, dv = Hash(float2(Cine.w, 9.1)) > 0.5 ? 1 : -1;
    float2 c = float2(b.z + b.w, b.x + b.y) * 0.5, hs = float2(b.w - b.z, b.y - b.x) * 0.5;
    float zoom = 1 + 0.10 * e;
    float2 m = hs * (1 - 1 / zoom);
    float2 q = c + (p - c) / zoom + float2(m.x * 0.85 * dir, m.y * 0.5 * dv);
    q.x += (H(q / NES) - 0.5) * 5 * e * dir;
    return clamp(q, float2(b.z, b.x) + 0.5, float2(b.w, b.y) - 0.5);
}

// drifting embers (warm pictures) or dust in the light, and slow light rays sweeping the picture
float3 CineFx(float2 p, float t)
{
    bool warm = MeanC.r > MeanC.b * 1.25;
    float2 e1 = Particle(p, 0, 16, warm ? float2(3, -16) : float2(5, 2), t, warm ? 1.7 : 1.1, 3);
    float2 e2 = Particle(p + 7, 0, 11, warm ? float2(-2, -25) : float2(8, 3), t, 1.0, 2);
    float tw = 0.6 + 0.4 * sin(t * 7 + e1.y * 30);
    float3 c = warm ? float3(1.0, 0.55, 0.18) * (e1.x * tw * 2.2 + e2.x * 1.4) : float3(0.9, 0.92, 1.0) * (e1.x * 0.6 + e2.x * 0.35);
    float ray = smoothstep(0.55, 1, sin((p.x * 0.8 + p.y * 0.6) * 0.045 - t * 0.35)) * (0.5 + 0.5 * FN(p + float2(t * 6, 0), 30));
    c += (warm ? float3(1.0, 0.75, 0.45) : float3(0.8, 0.88, 1.0)) * ray * 0.10;
    return c * Cine.x;
}

float4 PSLight(V i) : SV_Target
{
    float2 s = Uv.zw + i.uv * Uv.xy;
    bool cine = InCine(s * NES);
    if (cine) s = CineCam(s * NES) / NES;
    float3 a = Albedo(s);
    float2 p = s * NES;
    float4 inf = InfoPx(p);
    uint fl = Flags(inf);
    float4 ln = Lne.Load(int3(clamp(int(p.y), 0, 239), 0, 0));
    bool hudPx = (fl & 4) != 0 || (Lay.Load(int3(clamp(int2(p), int2(0, 0), int2((int)NES.x - 1, 239)), 0)).r & 0x80) != 0;
    if ((ln.z > 0.5 && (Scr.z <= 0 || hudPx)) || hudPx)   // playtest 6: in widescreen the map behind the HUD (no UI flag) is lit world
        return float4(HudPanel(a, p, s, inf) * BarShadow(p), 0);   // HUD / text + clean edges (Layers HUD bit): unlit (panel gloss only)
    float t = Cam.z;
    bool spr = (fl & 1) != 0;
    // motion in the world (material detail switch): water refracts in slow waves, grass sways in gusts of wind
    if (!spr && Opt.w > 0.5)
    {
        float2 w0 = ln.xy + float2(p.x, frac(p.y));
        int c0 = (int)round(inf.g * 255 / 4);
        int k0 = (int)round(MatT.Load(int3(c0, 0, 0)).r * 255 / 40);
        float2 off = 0;
        if (k0 == 2 && c0 < 0x20)                                      // dark (deep) water, not ice
            off = float2(WN(w0 + float2(t * 2, 0), float2(6, 6)) - 0.5, WN(w0 + float2(0, t * 1.5), float2(6, 6)) - 0.5) * 0.7;
        if (k0 == 5 && Scn.x > 0.3)
            off.x = sin(t * 1.4 + w0.x * 0.045 + w0.y * 0.07) * 0.3 * WN(w0 + float2(t * 6, 0), float2(48, 48));
        // heat shimmer: only just above a light source (the light map is brighter a few pixels below)
        float lh = dot(SrcC.SampleLevel(Lin, s, 0).rgb, LUM), lb = dot(SrcC.SampleLevel(Lin, s + float2(0, 6.0 / 240), 0).rgb, LUM);
        float heat = saturate((lb - lh) * 12) * saturate(lb * 4);
        // playtest 7: smooth, slow and small (fine 2.5 px cells shifted each row on its own = torn rows over a lava floor)
        off += float2(FN(p * 0.5 + float2(0, t * 5), 9) - 0.5, (FN(p * 0.5 + float2(t * 2, t * 5) + 31, 9) - 0.5) * 0.5) * heat * 0.35;
        a = Albedo(s + off / NES);
    }
    float3 c = a;
    float mv = Opt.x > 0.5 && Scn.y > 0.01 && !spr ? smoothstep(0.3, 0.7, VoidS(p)) * (1 - smoothstep(0.06, 0.18, dot(a, LUM))) : 0;
    if (mv < 0.999)
    {
        float2 w = ln.xy + float2(p.x, frac(p.y));                    // world position (nametable space)
        int col = (int)round(inf.g * 255 / 4);
        float4 m = MatT.Load(int3(col, 0, 0));
        int kind = spr ? 0 : (int)round(m.r * 255 / 40);
        if (kind == 5 && Scn.x <= 0.3) kind = 3;                         // T43: green indoors = moss / carpet, not grass
        // material detail
        float2 gd = 0;
        float amp = kind > 0 ? m.a * Opt.w : 0;
        if (amp > 0)
        {
            float e = 0.25;
            float d0 = Detail(w, kind, t), dx = Detail(w + float2(e, 0), kind, t), dy = Detail(w + float2(0, e), kind, t);
            gd = float2(dx - d0, dy - d0) / e * amp * 0.22;
            a *= 1 + amp * 0.55 * (d0 - 0.5);
            if (kind == 5) a = Lawn(a, s, w, t, col);
            else if (kind == 1 || kind == 3 || kind == 4) a = Ground(a, s, w, kind, col);
        }
        // fine relief from the 8x art itself (pack tiles, faces, bricks): its light and dark as a small bump
        if (Opt.w > 0.5 && QualC.y > 0.5)
        {
            float2 eu = 1.5 / ALB;
            float lx = dot(Albedo(s + float2(eu.x, 0)), LUM) - dot(Albedo(s - float2(eu.x, 0)), LUM);
            float ly = dot(Albedo(s + float2(0, eu.y)), LUM) - dot(Albedo(s - float2(0, eu.y)), LUM);
            gd += clamp(float2(lx, ly) * 0.5, -0.3, 0.3);
        }
        // water reflections (before lighting: the reflected scenery is lit like the water)
        if (kind == 2 && Opt2.x > 0 && QualC.y > 0.5 && (fl & 2) == 0)
        {
            float4 rf = Reflect(p, t);
            a = lerp(a, rf.rgb * 0.85 + a * 0.15, rf.a * Opt2.x * 1.6);
        }
        // shoreline foam: deep water next to land (or a character wading in it), animated
        if (kind == 2 && col < 0x20 && Opt.w > 0.5)
        {
            float edge = 0;
            [loop] for (int r = 0; r < 8; r++)
            {
                float an = r * 0.785398;
                float2 q = p + float2(cos(an), sin(an)) * 1.6;
                edge += (KindAt(q) != 2 && !IsVoid(q)) ? 1 : 0;
            }
            float fn = WN(w * 2 + float2(t * 3, t * 1.2), float2(3, 3));
            a = lerp(a, float3(0.92, 0.97, 1.0), saturate(edge / 3) * smoothstep(0.35, 0.65, fn) * 0.6);
            // T48: caustics: bright threads where two drifting ridge patterns cross (sunlight focused by the waves)
            float r1 = 1 - abs(2 * WN(w * 0.9 + float2(t * 2.2, t * 0.8), float2(12, 12)) - 1);
            float r2 = 1 - abs(2 * WN(w * 0.9 + float2(-t * 1.4, t * 1.9) + 17, float2(9, 9)) - 1);
            a += float3(0.55, 0.8, 0.9) * pow(saturate(r1 * r2), 8) * 0.22 * (0.4 + 0.6 * Scn.x);
        }
        // macro normal from the height model
        float2 dxs = float2(1.0 / (2 * NES.x), 0), dys = float2(0, 1.0 / 480);
        float h = H(s);
        // playtest 11: on sand / earth / plaster the game's dither dots made rows of lit ridges = horizontal lines: a quarter
        float rel = (kind == 4 || kind == 3) ? 0.25 : 1;
        float gx = (H(s + dxs) - H(s - dxs)) * Lgt.w * rel, gy = (H(s + dys) - H(s - dys)) * Lgt.w * rel;
        float3 n = normalize(float3(-gx - gd.x, -gy - gd.y, 1));
        // ambient occlusion: higher ground close by darkens
        float occ = 0;
        [unroll] for (int k = 0; k < 6; k++)
        {
            float an = k * 1.047198, an2 = an + 0.523599;
            occ += saturate(H(s + float2(cos(an), sin(an)) * float2(3.0 / (2 * NES.x), 3.0 / 480)) - h);
            occ += 0.6 * saturate(H(s + float2(cos(an2), sin(an2)) * float2(8.0 / (2 * NES.x), 8.0 / 480)) - h);
        }
        occ *= rel;
        float ao = 1 - Fx.y * saturate(occ / 7.5);
        // soft shadow toward the sun; the penumbra widens with the distance to the caster
        float2 ld = normalize(Lgt.xy);
        float sh = 0;
        [loop] for (int j = 1; j <= 24; j++)
        {
            if (j > (int)QualC.x) break;
            float d = j;
            float hq = H(s + float2(ld.x / (2 * NES.x), ld.y / 480) * d);
            sh = max(sh, saturate((hq - h - d * Fx2.x) * 2 / (0.4 + 0.06 * d)) * (1 - j / 25.0));
        }
        sh *= rel;
        float shade = 1 - Fx.z * sh;
        // contact shadow under sprites (they stand on the ground: a soft dark patch just below and behind them)
        if (!spr)
        {
            float cs = 0;
            [loop] for (int k = 0; k < 5; k++)
            {
                float2 o = float2(-1.2 + 0.6 * k, -2.5 - 0.4 * abs(k - 2));
                cs += Inf.SampleLevel(Lin, (p + o) / NES, 0).r;
            }
            shade *= 1 - Fx.w * saturate(cs / 5 * 1.6);
        }
        // drifting cloud shadows outdoors
        float2 cp = p + Cam.xy + float2(t * 5, t * 2);
        float cl = FN(cp, 70) * 0.65 + FN(cp, 28) * 0.35;
        shade *= 1 - smoothstep(0.5, 0.7, cl) * 0.40 * Scn.x * Opt.z * Opt.y;
        // light: warm sun, cool sky; normalised so flat lit ground keeps its brightness
        float3 sunC = float3(1.0, 0.95, 0.86);
        float3 sky = lerp(float3(0.78, 0.86, 1.0), MeanC.rgb / max(max(MeanC.r, MeanC.g), max(MeanC.b, 0.05)), 0.2);
        float diff = saturate(dot(n, Lgt.xyz));
        float norm = dot(Mat.x * sky + Mat.y * Hlf.w * sunC, LUM);
        // bounce light: the ambient takes on the colour of what is around (a red carpet warms the floor beside it)
        float4 bn = Bnc.SampleLevel(Lin, s, 0);
        float3 bc = bn.a > 0.01 ? bn.rgb / bn.a : sky;
        bc = clamp(bc / max(dot(bc, LUM), 0.08), 0, 2);
        float3 amb = sky * lerp(1, bc, 0.35);
        float3 lightC = (Mat.x * amb * ao + Mat.y * diff * sunC * shade) / norm;
        // local lights (fire, lava, lamps): in dark places the light map lights everything around it
        float3 lm = SrcC.SampleLevel(Lin, s, 0).rgb;
        float lightUp = Opt2.y * (0.05 + 0.95 * Scn.w);
        lightC = lightC * (1 - 0.2 * Scn.w * saturate(Opt2.y)) + lm * lightUp * LocalDiffuse(s, n, lm);
        // specular per material, glints on water / ice
        float gp = 6 + (spr ? 0.3 : m.b) * 120;
        float nh = saturate(dot(n, Hlf.xyz));
        float lum = dot(a, LUM);
        float sp = max(0, pow(nh, gp) - pow(Hlf.z, gp)) * (spr ? 0.35 : m.g) * Mat.z * (0.3 + 0.7 * lum) * shade * (gp + 8) / 40;
        // sky sheen on water / ice: stronger toward the top of the screen (a flatter view), drifting slowly
        float sheen = kind == 2 ? (0.06 + 0.10 * (1 - p.y / 240)) * (0.55 + 0.45 * WN(w * 0.5 + float2(t * 2, t), float2(24, 24))) * (col < 0x20 ? 1 : 0.2) : 0;
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
        float rim = spr ? pow(saturate(1 - n.z), 1.5) * 0.2 : 0;   // playtest 9: 0.7 = a bright halo around every character
        c = a * lightC + sp * sunC + glint + rim * sky * (0.4 + 0.6 * lum) + sky * sheen * Opt.w;
        // emissive colours glow a little themselves in the dark
        c += a * Emissive((uint)col) * Scn.w * Opt2.y * 0.25;
        // sun shafts outdoors: soft beams along the light direction, fixed to the world, slowly drifting
        float2 sd = normalize(Lgt.xy), sq = float2(-sd.y, sd.x);
        float2 wp = p + Cam.xy * 1.15;
        float band = FN(float2(dot(wp, sq), dot(wp, sd) * 0.08 + t * 1.5), 14);
        float shaft = smoothstep(0.5, 0.85, band) * Scn.x * Opt.z;
        c = c * (1 - 0.06 * Scn.x * Opt.z + 0.06 * shaft) + sunC * shaft * 0.14 * (0.5 + 0.5 * shade);
        // mist outdoors (closer than the playfield)
        float2 mp = p + Cam.xy * 1.3 + float2(t * 4, t * 1.5);
        float mist = smoothstep(0.55, 0.85, FN(mp, 60) * 0.6 + FN(mp, 24) * 0.4);
        c = lerp(c, float3(0.92, 0.95, 1.0) * max(lum, 0.6), mist * 0.08 * Scn.x * Opt.z);
    }
    if (mv > 0.001) c = lerp(c, lerp(c, Chasm(p, t), Scn.y), mv);
    c += Motes(p, t) * Opt.z * Scn.z;
    // weather by scene: snow / ice crystals, rising embers (additive, they glow), falling leaves outdoors
    c += Weather(p, t) * Scn.z;
    float2 lf = Particle(p, 1.3, 26, float2(14, 9), t, 1.2, 2);
    c = lerp(c, lerp(float3(0.30, 0.50, 0.12), float3(0.75, 0.45, 0.12), frac(lf.y * 7)), lf.x * step(0.82, lf.y) * 0.8 * Scn.x * Opt.z * Scn.z);
    // T43: the status bar casts a soft shadow up onto the map: from the last tile row above the panel (the row the game
    // has not loaded yet under the split) black, fading in over 4 lines; MeterF.w = status-bar line * 256 + row start
    if (cine) c += CineFx(i.uv * NES, Cam.z);   // T51: in screen space (they do not ride the dolly)
    c *= BarShadow(p);
    return float4(c, 1);
}

float4 PSBright(V i) : SV_Target
{
    float4 h = SrcA.SampleLevel(Lin, i.uv, 0);
    float l = max(h.r, max(h.g, h.b));
    float k = saturate((l - Fx2.z + 0.25) / 0.5);                     // soft knee
    return float4(h.a < 0.5 ? 0 : h.rgb * k * k * max(l - Fx2.z, 0) / max(l, 1e-4) * 4, 1);
}

float4 PSBlur(V i) : SV_Target
{
    static const float w[5] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };
    float3 c = SrcA.SampleLevel(Lin, i.uv, 0).rgb * w[0];
    [unroll] for (int k = 1; k < 5; k++)
    {
        c += SrcA.SampleLevel(Lin, i.uv + Blur.xy * k * 1.5, 0).rgb * w[k];
        c += SrcA.SampleLevel(Lin, i.uv - Blur.xy * k * 1.5, 0).rgb * w[k];
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
    float4 h = SrcA.SampleLevel(Lin, i.uv, 0);
    if (h.a < 0.5) return float4(saturate(h.rgb), 0);                   // HUD: untouched (alpha 0: no AA either)
    // T55 tilt-shift (Mat.w): in gameplay the playfield's top and bottom soften, the middle stays sharp (HD-2D diorama
    // look); a 12-tap golden-angle disc up to 1.3 NES pixels, HUD samples left out
    if (Mat.w > 0 && Scn.z > 0.5 && Cine.x <= 0)
    {
        float ny = (Uv.w + i.uv.y * Uv.y) * 240;
        float bot = HudC.x >= 0 && HudC.y > 100 ? HudC.y : 240;
        float rr = saturate((abs(ny - bot * 0.55) - bot * 0.22) / (bot * 0.25)) * Mat.w * Out.y / 240 * 1.3;
        if (rr > 0.3)
        {
            float4 acc = float4(h.rgb, 1);
            [unroll] for (int k = 0; k < 12; k++)
            {
                float an = k * 2.39996, rad = sqrt((k + 0.5) / 12) * rr;
                float4 sm = SrcA.SampleLevel(Lin, i.uv + float2(cos(an), sin(an)) * rad * Out.zw, 0);
                acc += float4(sm.rgb, 1) * step(0.5, sm.a);              // no branch: Wine cannot unroll one
            }
            h.rgb = acc.rgb / acc.w;
        }
    }
    // two bloom sizes: a tight glow (1/4) and a wide halo (1/8)
    float3 bl = SrcB.SampleLevel(Lin, i.uv, 0).rgb * 0.75 + SrcC.SampleLevel(Lin, i.uv, 0).rgb * 0.55;
    // T51 cut-scenes: an anamorphic lens streak (the tight bloom stretched sideways, blue)
    if (Cine.x > 0)
    {
        float3 st = 0;
        [unroll] for (int k = 1; k <= 6; k++)
            st += (SrcB.SampleLevel(Lin, i.uv + float2(k * 0.02, 0), 0).rgb + SrcB.SampleLevel(Lin, i.uv - float2(k * 0.02, 0), 0).rgb) * (1 - k / 7.0);
        bl += st * float3(0.30, 0.50, 1.0) * 0.22 * Cine.x;
    }
    float3 c = Tone(h.rgb * Grade.x + bl * Fx2.w);
    c = lerp(dot(c, LUM).xxx, c, Grade.y);
    c = lerp(c, c * c * (3 - 2 * c), Grade.z);
    // scene grade: warmer outdoors, cooler in dark places
    c *= lerp(1, float3(1.04, 1.0, 0.94), Scn.x * 0.7) * lerp(1, float3(0.93, 0.97, 1.07), Scn.w * 0.7);
    float2 d = i.uv - 0.5;
    c *= 1 - Grade.w * dot(d, d) * 2;
    // T57 damage feedback: a red vignette flash on a hit, a heartbeat pulse at the edges while life is low
    if (Dmg.x > 0 || Dmg.y > 0)
    {
        float e = smoothstep(0.08, 0.32, dot(d, d));
        float beat = pow(0.5 + 0.5 * sin(Cam.z * 7.5), 6);
        c = lerp(c, float3(0.55, 0.02, 0.03), saturate(e * (Dmg.x * 0.65 + Dmg.y * (0.15 + 0.25 * beat))));
    }
    if (Cine.x > 0)
    {
        // film: teal shadows / warm highlights, a deeper vignette, moving grain
        float l = dot(c, LUM);
        c = saturate(c + (float3(-0.03, 0.0, 0.035) * (1 - l) + float3(0.035, 0.012, -0.03) * l) * Cine.x);
        c *= 1 - Cine.x * 0.6 * dot(d, d);
        c += (Hash(i.p.xy * 0.5 + frac(Cam.z * 24) * 731) - 0.5) * 0.045 * Cine.x;
    }
    c += (Hash(i.p.xy + frac(Cam.z) * 517) - 0.5) / 255;               // dither: no banding in smooth gradients
    return float4(saturate(c), 1);
}

// edge anti-aliasing at screen resolution (FXAA-style: blend along the edge where the contrast is high); HUD exact
float3 Fxaa(V i)
{
    float2 px = Out.zw;
    float4 m = SrcA.SampleLevel(Lin, i.uv, 0);
    if (m.a < 0.5) return m.rgb;
    float lNW = dot(SrcA.SampleLevel(Lin, i.uv + float2(-1, -1) * px, 0).rgb, LUM);
    float lNE = dot(SrcA.SampleLevel(Lin, i.uv + float2(1, -1) * px, 0).rgb, LUM);
    float lSW = dot(SrcA.SampleLevel(Lin, i.uv + float2(-1, 1) * px, 0).rgb, LUM);
    float lSE = dot(SrcA.SampleLevel(Lin, i.uv + float2(1, 1) * px, 0).rgb, LUM);
    float lM = dot(m.rgb, LUM);
    float lMin = min(lM, min(min(lNW, lNE), min(lSW, lSE))), lMax = max(lM, max(max(lNW, lNE), max(lSW, lSE)));
    float2 dir = float2(-((lNW + lNE) - (lSW + lSE)), (lNW + lSW) - (lNE + lSE));
    float red = max((lNW + lNE + lSW + lSE) * 0.03125, 1.0 / 128);
    dir = clamp(dir / (min(abs(dir.x), abs(dir.y)) + red), -4, 4) * px;
    float3 a = 0.5 * (SrcA.SampleLevel(Lin, i.uv - dir / 6, 0).rgb + SrcA.SampleLevel(Lin, i.uv + dir / 6, 0).rgb);
    float3 b = a * 0.5 + 0.25 * (SrcA.SampleLevel(Lin, i.uv - dir * 0.5, 0).rgb + SrcA.SampleLevel(Lin, i.uv + dir * 0.5, 0).rgb);
    float lB = dot(b, LUM);
    float3 r = lB < lMin || lB > lMax ? a : b;
    return lMax - lMin < 0.08 ? m.rgb : r;
}

// T48: contrast-adaptive sharpening (AMD CAS style): a negative-lobe cross filter whose weight falls where the
// neighbourhood is already near black or white, so edges get crisper without halos; HUD (alpha 0) exact
float3 Sharpen(float3 c, V i)
{
    if (Cam.w <= 0 || SrcA.SampleLevel(Lin, i.uv, 0).a < 0.5) return c;
    float2 px = Out.zw;
    float3 n = SrcA.SampleLevel(Lin, i.uv - float2(0, px.y), 0).rgb, s = SrcA.SampleLevel(Lin, i.uv + float2(0, px.y), 0).rgb;
    float3 e = SrcA.SampleLevel(Lin, i.uv + float2(px.x, 0), 0).rgb, w = SrcA.SampleLevel(Lin, i.uv - float2(px.x, 0), 0).rgb;
    float3 mn = min(c, min(min(n, s), min(e, w))), mx = max(c, max(max(n, s), max(e, w)));
    float3 amp = sqrt(saturate(min(mn, 1 - mx) / max(mx, 1e-4)));
    float3 wt = -amp * lerp(0.125, 0.2, saturate(Cam.w));
    return saturate((c + (n + s + e + w) * wt) / (1 + 4 * wt));
}

// T53 ambient bars: Blur.xy = the picture's origin, Blur.zw its size (client uv). A bar pixel shows the lit picture
// (SrcA = the picture already blurred at 1/8) mirrored at the nearest edge, 9 more taps over ~10 %, dimmed, fading out
float4 PSAmbient(V i) : SV_Target
{
    float2 q = (i.uv - Blur.xy) / Blur.zw;
    float2 m = q < 0 ? -q : q > 1 ? 2 - q : q;
    m = clamp(m, 0.02, 0.98);
    float2 d = max(max(-q, q - 1), 0) * Blur.zw;
    float dist = length(d);
    float3 c = 0;
    [unroll] for (int k = 0; k < 9; k++)                             // one loop: Wine cannot unroll nested ones
        c += SrcA.SampleLevel(Lin, clamp(m + float2(k % 3 - 1, k / 3 - 1) * 0.05, 0.01, 0.99), 0).rgb;
    c /= 9;
    c = saturate(c) * 0.42 * exp(-dist * 4.5);
    c += (Hash(i.p.xy + frac(Cam.z) * 517) - 0.5) / 255;
    return float4(saturate(c), 1);
}

// ---- playtest 7: remade story art (Dmg.z = scene, Dmg.w = fade). The game's picture band is repainted at the output
// resolution over the whole width. Scene 1: the prehistoric jungle of the intro (the game draws it as a narrow column).
// Loops are branch-free (Wine's compiler cannot unroll a loop with a branch).
float SN(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    float a = Hash(i), b = Hash(i + float2(1, 0)), c = Hash(i + float2(0, 1)), d = Hash(i + float2(1, 1));
    return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
}
float SFbm(float2 p)
{
    float s = 0, a = 0.5;
    [unroll] for (int k = 0; k < 5; k++) { s += a * SN(p); p = p * 2.03 + float2(17.1, 9.3); a *= 0.5; }
    return s;
}
float SdCap(float2 p, float2 a, float2 b, float r)
{
    float2 pa = p - a, ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h) - r;
}
float SdEll(float2 p, float2 c, float2 r)
{
    float2 q = (p - c) / r;
    return (length(q) - 1) * min(r.x, r.y);
}
float SMin(float a, float b, float k) { float h = saturate(0.5 + 0.5 * (b - a) / k); return lerp(b, a, h) - k * h * (1 - h); }
// a long-necked giant facing right: body, curved neck, small head, tapering tail, four legs (signed distance)
float SdSauropod(float2 p, float2 g, float s, float t)
{
    float bob = 0.012 * sin(t * 0.7 + g.x * 3);
    float2 b = g + float2(0, -0.085) * s;
    float d = SdEll(p, b, float2(0.115, 0.052) * s);
    float2 n0 = b + float2(0.085, -0.02) * s, n1 = b + float2(0.15, -0.12) * s, n2 = b + float2(0.17, -0.22 + bob) * s, n3 = b + float2(0.2, -0.25 + bob) * s;
    d = SMin(d, SdCap(p, n0, n1, 0.026 * s), 0.03 * s);
    d = SMin(d, SdCap(p, n1, n2, 0.018 * s), 0.02 * s);
    d = SMin(d, SdCap(p, n2 + float2(0.005, -0.006) * s, n3 + float2(0.018, 0.004) * s, 0.013 * s), 0.01 * s);
    float2 t0 = b + float2(-0.1, 0.0) * s, t1 = b + float2(-0.2, 0.03) * s, t2 = b + float2(-0.29, 0.05 + 0.006 * sin(t * 0.9)) * s;
    d = SMin(d, SdCap(p, t0, t1, 0.02 * s), 0.02 * s);
    d = SMin(d, SdCap(p, t1, t2, 0.008 * s), 0.01 * s);
    [unroll] for (int k = 0; k < 4; k++)
    {
        float lx = (-0.07 + 0.045 * k + (k > 1 ? 0.03 : 0)) * s;
        d = min(d, SdCap(p, b + float2(lx, 0.02 * s), float2(b.x + lx + 0.004 * s, g.y), 0.017 * s));
    }
    return d;
}
// a gliding flier, wings beating
float SdFlier(float2 p, float2 c, float s, float ph)
{
    float w = sin(ph) * 0.5;
    float d = SdEll(p, c, float2(0.02, 0.006) * s);
    d = min(d, SdCap(p, c, c + float2(-0.045, -0.02 - w * 0.03) * s, 0.004 * s));
    d = min(d, SdCap(p, c, c + float2(0.045, -0.02 - w * 0.03) * s, 0.004 * s));
    return d;
}
// a fern frond: a curved spine from base with leaflets (stripes across the spine), dark against the light
float FernMask(float2 p, float2 base, float ang, float len, float sway)
{
    float2 d = p - base;
    float a = ang + sway;
    float2 dir = float2(cos(a), sin(a)), nrm = float2(-dir.y, dir.x);
    float u = dot(d, dir), v = dot(d, nrm);
    float s = saturate(u / len);
    v -= 0.25 * len * s * s;                                        // the frond bends downward toward its tip
    float width = 0.2 * len * (1 - s * 0.8) * sin(3.14159 * min(s * 1.2 + 0.08, 1));
    float chev = frac((u - abs(v) * 0.9) / (len * 0.055));            // chevron leaflets slanting toward the tip
    float leaf = step(0.28, chev) * step(abs(v), width) + step(abs(v), width * 0.25);
    float inside = step(0, u) * step(u, len) * saturate(leaf);
    float spine = step(0, u) * step(u, len) * step(abs(v), 0.004);
    return max(inside, spine);
}
float3 StoryDino(float2 q, float aspect, float t)
{
    float cx = aspect * 0.5;
    float y = q.y;
    // sky: deep teal above a hot amber horizon, the sun low on the right
    float3 sky = lerp(float3(0.2, 0.4, 0.55), float3(1.0, 0.7, 0.38), smoothstep(0.0, 0.65, y));
    float2 sun = float2(cx + aspect * 0.22, 0.34);
    float sd = length(q - sun);
    sky += float3(1.0, 0.8, 0.5) * (exp(-sd * 9) * 0.9 + exp(-sd * 2.5) * 0.25);
    // drifting cloud banks lit from below
    float cl = SFbm(float2(q.x * 2.2 + t * 0.008, y * 5));
    float cm = smoothstep(0.55, 0.78, cl) * smoothstep(0.5, 0.1, y) * smoothstep(0.08, 0.25, sd);
    sky = lerp(sky, lerp(float3(1.0, 0.72, 0.5), float3(0.45, 0.36, 0.42), smoothstep(0.05, 0.4, y)), cm * 0.55);
    sky = lerp(sky, float3(1.0, 0.98, 0.9), smoothstep(0.05, 0.03, sd));
    sky += float3(1.0, 0.85, 0.55) * exp(-sd * 14) * 0.6;
    // god rays from the sun
    float ang = atan2(q.y - sun.y, q.x - sun.x);
    float rays = pow(SN(float2(ang * 9, t * 0.05)), 3) * exp(-sd * 1.6) * 0.35;
    float3 c = sky + float3(1.0, 0.8, 0.5) * rays;
    // a smoking volcano far away
    float vx = cx - aspect * 0.28;
    float vol = 0.6 - 0.2 * exp(-pow((q.x - vx) / 0.22, 2)) + 0.025 * SFbm(float2(q.x * 7, 3));
    vol = max(vol, 0.47 + 0.02 * sin(q.x * 3));
    float smoke = SFbm(float2((q.x - vx) * 3 - y * 1.5 + t * 0.03, y * 3 + t * 0.06)) * smoothstep(0.42, 0.05, y) * exp(-pow((q.x - vx - (0.42 - y) * 0.5) / (0.06 + (0.42 - y) * 0.35), 2));
    c = lerp(c, float3(0.36, 0.3, 0.33), saturate(smoke * 1.6));
    c = lerp(c, lerp(float3(0.42, 0.36, 0.45), sky, 0.45), step(vol, y));
    c += float3(1.0, 0.35, 0.1) * exp(-pow((q.x - vx) / 0.03, 2)) * exp(-abs(y - 0.41) * 40) * 0.8;   // lava glow at the rim
    // far jungle line
    float j1 = 0.64 + 0.035 * SFbm(float2(q.x * 9, 1)) - 0.02 * abs(sin(q.x * 23));
    float3 far = lerp(float3(0.3, 0.46, 0.42), sky, 0.42);
    c = lerp(c, far, step(j1, y));
    float palm = 1;
    [unroll] for (int pi = 0; pi < 54; pi++)             // 9 palms x 6 fronds in one loop (Wine: no nested loops)
    {
        int k = pi / 6, f = pi % 6;
        float px = (k + 0.3 + 0.4 * Hash(float2(k, 7))) * aspect / 9, ph = 0.1 + 0.08 * Hash(float2(k, 3));
        float2 pb = float2(px, j1 + 0.01), top = pb + float2(0.02 * sin(k * 1.3), -ph);
        palm = min(palm, SdCap(q, pb, top, 0.004));
        float a = -2.9 + f * 0.55 + 0.05 * sin(t * 0.8 + k);
        float2 tip = top + float2(cos(a), sin(a) + 0.55) * 0.045;
        palm = min(palm, SdCap(q, top, tip, 0.005));
    }
    c = lerp(c, far * 0.8, smoothstep(0.002, -0.001, palm));
    // ground mist between the layers
    c = lerp(c, float3(1.0, 0.8, 0.6), 0.35 * exp(-pow((y - 0.72) / 0.05, 2)) * (0.7 + 0.3 * SN(float2(q.x * 3 + t * 0.05, 0))));
    // two giants in the middle distance, rim-lit by the sun
    float gy = 0.8;
    float d1 = SdSauropod(q, float2(cx - aspect * 0.12, gy), 1.0, t);
    float d2 = SdSauropod(q, float2(cx + aspect * 0.05, gy + 0.03), 0.72, t + 2);
    float dd = min(d1, d2);
    float3 beast = float3(0.22, 0.25, 0.2);
    float rim = smoothstep(0.006, 0.0, abs(dd + 0.003)) * saturate(dot(normalize(sun - q), float2(1, -0.3)));
    c = lerp(c, beast + float3(1.0, 0.75, 0.45) * rim * 0.6, smoothstep(0.002, -0.002, dd));
    // mid jungle and the ground in front of them
    float j2 = 0.79 + 0.045 * SFbm(float2(q.x * 4 + 5, 2)) + 0.012 * sin(q.x * 40);
    c = lerp(c, float3(0.16, 0.27, 0.16) + float3(0.35, 0.25, 0.08) * exp(-abs(y - j2) * 50), step(j2, y));
    // fliers crossing the sky
    float fl = 1;
    [unroll] for (int k = 0; k < 3; k++)
    {
        float2 pc = float2(frac(t * 0.012 + k * 0.37) * (aspect + 0.4) - 0.2, 0.16 + 0.07 * k + 0.015 * sin(t * 0.5 + k));
        fl = min(fl, SdFlier(q, pc, 1.0 - 0.2 * k, t * 4.5 + k * 2));
    }
    c = lerp(c, float3(0.12, 0.1, 0.13), smoothstep(0.002, -0.001, fl) * 0.9);
    // foreground ferns framing both sides, swaying
    float fern = 0;
    [unroll] for (int k = 0; k < 7; k++)
    {
        float sw = 0.04 * sin(t * 0.9 + k * 1.7);
        fern = max(fern, FernMask(q, float2(0.0 + k * 0.035, 1.04), -1.15 + k * 0.17, 0.5 + 0.1 * (k % 3), sw));
        fern = max(fern, FernMask(q, float2(aspect - k * 0.035, 1.04), -2.0 - k * 0.17, 0.48 + 0.1 * ((k + 1) % 3), -sw));
    }
    c = lerp(c, float3(0.03, 0.07, 0.04), fern);
    // sunlit motes
    float2 mp = q * float2(40, 40) + float2(0, -t * 0.6);
    float2 mi = floor(mp), mf = frac(mp) - 0.5;
    float mh = Hash(mi);
    c += float3(1.0, 0.85, 0.55) * step(0.97, mh) * smoothstep(0.1, 0.0, length(mf - (Hash(mi + 3) - 0.5) * 0.6)) * 0.7 * smoothstep(0.5, 0.65, y) * smoothstep(0.98, 0.8, y);
    // grade: warm, a soft vignette across the wide picture
    float vg = 1 - 0.35 * pow(abs(q.x - cx) / (cx + 0.001), 2) - 0.2 * pow(abs(y - 0.5) * 2, 2);
    return saturate(c * vg * 1.15);
}
// T37: the on-screen display (Osd, premultiplied) over the anti-aliased picture
float4 PSFinal(V i) : SV_Target
{
    float3 c = Fxaa(i);
    c = Sharpen(c, i);
    // playtest 7: remade story art over the game's picture band (Scr.w = band lines), the whole width, faded with the game
    if (Dmg.z > 0.5)
    {
        float2 sp2 = (Uv.zw + i.uv * Uv.xy) * NES;
        float y0 = floor(Scr.w / 256), y1 = Scr.w - y0 * 256;      // the story band (Gpu.StoryBand)
        if (y1 > y0 + 8 && sp2.y >= y0 && sp2.y < y1)
        {
            float h = y1 - y0;
            if ((Flags(InfoPx(sp2)) & 4) == 0)                      // the game's text stays on top
                c = StoryDino(float2(sp2.x / h, (sp2.y - y0) / h), NES.x / h, Cam.z) * Dmg.w;
        }
    }
    // widescreen: side pixels with no map data (outside the area, a line that does not show the map) get a dim, blurred
    // continuation of the picture (SrcC = the lit picture at 1/8, blurred wide), dimmer toward the outer edge
    if (Scr.z > 0)
    {
        float2 np = (Uv.zw + i.uv * Uv.xy) * NES;
        if ((Flags(InfoPx(np)) & 8) != 0 && Dmg.z < 0.5)                  // (not under remade story art)
        {
            // mirrored back across the edge of the game's columns (the scene's colours carry across), dimmer outward
            float out1 = saturate(max(Scr.z - np.x, np.x - (Scr.z + 256)) / Scr.z);
            float mx = np.x < Scr.z ? 2 * Scr.z - np.x : np.x > Scr.z + 256 ? 2 * (Scr.z + 256) - np.x : np.x;
            float2 muv = float2((mx / NES.x - Uv.z) / Uv.x, i.uv.y);
            // playtest 9: a 2D 9-tap average over a wide area (the single tap smeared rows sideways = "line drags")
            float3 acc = 0;
            [unroll] for (int k = 0; k < 9; k++) acc += SrcC.SampleLevel(Lin, saturate(muv + float2(k % 3 - 1, k / 3 - 1) * float2(0.04, 0.07)), 0).rgb;
            c = saturate(acc / 9) * (0.7 - 0.45 * out1);
        }
    }
    c = saturate(float3(dot(Col0.xyz, c), dot(Col1.xyz, c), dot(Col2.xyz, c)));   // T60
    if (QualC.w > 0.5)
    {
        float4 o = SrcB.SampleLevel(Lin, i.uv, 0);
        // T56 frosted glass: under the overlay (the pause / help dim, panels, notices) the game shows blurred (SrcC = the
        // picture at 1/8, blurred wide)
        float fr = saturate(o.a * 1.6);
        if (fr > 0.01) c = lerp(c, saturate(SrcC.SampleLevel(Lin, i.uv, 0).rgb), fr * 0.9);
        c = c * (1 - o.a) + o.rgb;
    }
    return float4(c, 1);
}
""";
}
