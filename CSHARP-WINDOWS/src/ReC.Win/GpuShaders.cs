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
    float4 Mat;    // ambient, diffuse, specular strength, -
    float4 Fx;     // sprite lift, AO strength, shadow strength, contact shadow strength
    float4 Fx2;    // shadow slope, albedo sharpness, bloom threshold, bloom strength
    float4 Blur;   // blur step (uv), jump-flood step (pixels), -
    float4 Cam;    // camera x, y (NES pixels, unwrapped), time (s), -
    float4 Opt;    // parallax, shadows, atmosphere, material detail (0 / 1)
    float4 Scn;    // outdoor, void gate, gameplay, darkness
    float4 MeanC;  // mean playfield colour
    float4 Grade;  // exposure (eye adaptation), saturation, contrast, vignette
    float4 Opt2;   // reflection strength, local light strength, icy scene (snow), hot scene (embers)
    float4 HudC;   // HUD panel: fill colour (-1 = none), first line, last line + 1, -
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
SamplerState Lin : register(s0);

static const float2 NES = float2(256, 240);
static const float3 LUM = float3(0.299, 0.587, 0.114);
static const float2 ALB = float2(2048, 1920);

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
    if ((E.r & 0x80) != 0) return E;                                  // HUD / text: exact pixels
    int2 dx = int2(sub.x != 0 ? 1 : -1, 0), dy = int2(0, sub.y != 0 ? 1 : -1);
    uint4 X = KeyAt(q + dx, sz), X2 = KeyAt(q - dx, sz), Y = KeyAt(q + dy, sz), Y2 = KeyAt(q - dy, sz);
    uint4 r = E;
    uint xb = X.r | X.b << 8, yb = Y.r | Y.b << 8, x2b = X2.r | X2.b << 8, y2b = Y2.r | Y2.b << 8;   // background key: colour + dither partner
    if (xb == yb && yb != x2b && xb != y2b && (X.r & 0x80) == 0) { r.r = X.r; r.b = X.b; }
    if (X.g == Y.g && Y.g != X2.g && X.g != Y2.g) r.g = X.g;
    return r;
}

uint4 PSScale(V i) : SV_Target { return Scale2(int2(i.p.xy)); }

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

// the albedo at 8x: Scale2x level 3 on the 4x keys, pack tiles, dither blend, sprite over background
float4 PSAlbedo(V i) : SV_Target
{
    int2 o = int2(i.p.xy), n = o / 8;
    uint4 L = Lay.Load(int3(n, 0));
    bool ui = (L.r & 0x80) != 0;
    uint4 k = ui ? L : Scale2(o);
    float2 sub = (float2(o - n * 8) + 0.5) / 8;
    float3 bg = NesRgb(k.r);
    if (k.b != 0) bg = (bg + NesRgb(k.b - 1)) * 0.5;
    uint2 art = ArtM.Load(int3(n, 0));
    if (!ui && (art.x & 0x7FFF) != 0)
    {
        float4 a = ArtSample(art.x, sub);
        if (a.a >= 0.5) bg = Faded(a.rgb, art.x, k.r);
    }
    float3 c = bg;
    if (SprFront(k))
    {
        c = NesRgb(k.g);
        if (!ui && (art.y & 0x7FFF) != 0)
        {
            float4 a = ArtSample(art.y, sub);
            if (a.a >= 0.5) c = Faded(a.rgb, art.y, k.g);
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
            bool inside = q.x >= 0 && q.y >= 0 && q.x < 512 && q.y < 480;
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
float4 InfoPx(float2 p) { return Inf.Load(int3(clamp(int2(floor(p)), int2(0, 0), int2(255, 239)), 0)); }
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
    float2 le = float2(2.0 / 128, 2.0 / 120);
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
float3 HudPanel(float3 a, float2 p, float4 inf)
{
    int fill = (int)round(HudC.x);
    if (fill < 0 || (int)round(inf.g * 255 / 4) != fill || p.y < HudC.y || p.y >= HudC.z) return a;
    float t = saturate((p.y - HudC.y) / max(HudC.z - HudC.y, 1));
    float g = 1.10 - 0.20 * t + 0.08 * exp(-pow((t - 0.12) * 9, 2));
    float4 q = InfoPx(p - float2(1, 1));
    bool shade = (Flags(q) & 4) != 0 && (int)round(q.g * 255 / 4) != fill && p.y - 1 >= HudC.y;
    return saturate(a * g * (shade ? 0.7 : 1));
}

float4 PSLight(V i) : SV_Target
{
    float2 s = Uv.zw + i.uv * Uv.xy;
    float3 a = Albedo(s);
    float2 p = s * NES;
    float4 inf = InfoPx(p);
    uint fl = Flags(inf);
    float4 ln = Lne.Load(int3(clamp(int(p.y), 0, 239), 0, 0));
    if (ln.z > 0.5 || (fl & 4) != 0) return float4(HudPanel(a, p, inf), 0);   // HUD / text: unlit (panel gloss only)
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
        if (k0 == 5)
            off.x = sin(t * 1.4 + w0.x * 0.045 + w0.y * 0.07) * 0.3 * WN(w0 + float2(t * 6, 0), float2(48, 48));
        // heat shimmer: only just above a light source (the light map is brighter a few pixels below)
        float lh = dot(SrcC.SampleLevel(Lin, s, 0).rgb, LUM), lb = dot(SrcC.SampleLevel(Lin, s + float2(0, 6.0 / 240), 0).rgb, LUM);
        float heat = saturate((lb - lh) * 12) * saturate(lb * 4);
        off += float2(FN(p * 0.9 + float2(0, t * 14), 2.5) - 0.5, (FN(p * 0.9 + float2(t * 5, t * 14) + 31, 2.5) - 0.5) * 0.5) * heat * 1.2;
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
        // fine relief from the 8x art itself (pack tiles, faces, bricks): its light and dark as a small bump
        if (Opt.w > 0.5)
        {
            float2 eu = float2(1.5 / 2048, 1.5 / 1920);
            float lx = dot(Albedo(s + float2(eu.x, 0)), LUM) - dot(Albedo(s - float2(eu.x, 0)), LUM);
            float ly = dot(Albedo(s + float2(0, eu.y)), LUM) - dot(Albedo(s - float2(0, eu.y)), LUM);
            gd += clamp(float2(lx, ly) * 0.5, -0.3, 0.3);
        }
        // water reflections (before lighting: the reflected scenery is lit like the water)
        if (kind == 2 && Opt2.x > 0 && (fl & 2) == 0)
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
        float rim = spr ? pow(saturate(1 - n.z), 1.5) * 0.7 : 0;
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
    // two bloom sizes: a tight glow (1/4) and a wide halo (1/8)
    float3 bl = SrcB.SampleLevel(Lin, i.uv, 0).rgb * 0.75 + SrcC.SampleLevel(Lin, i.uv, 0).rgb * 0.55;
    float3 c = Tone(h.rgb * Grade.x + bl * Fx2.w);
    c = lerp(dot(c, LUM).xxx, c, Grade.y);
    c = lerp(c, c * c * (3 - 2 * c), Grade.z);
    // scene grade: warmer outdoors, cooler in dark places
    c *= lerp(1, float3(1.04, 1.0, 0.94), Scn.x * 0.7) * lerp(1, float3(0.93, 0.97, 1.07), Scn.w * 0.7);
    float2 d = i.uv - 0.5;
    c *= 1 - Grade.w * dot(d, d) * 2;
    c += (Hash(i.p.xy + frac(Cam.z) * 517) - 0.5) / 255;               // dither: no banding in smooth gradients
    return float4(saturate(c), 1);
}

// edge anti-aliasing at screen resolution (FXAA-style: blend along the edge where the contrast is high); HUD exact
float4 PSFinal(V i) : SV_Target
{
    float2 px = Out.zw;
    float4 m = SrcA.SampleLevel(Lin, i.uv, 0);
    if (m.a < 0.5) return float4(m.rgb, 1);
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
    return float4(lMax - lMin < 0.08 ? m.rgb : r, 1);
}
""";
}
