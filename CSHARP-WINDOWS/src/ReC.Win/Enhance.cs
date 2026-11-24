// re-c Windows host — "Enhanced (16-bit)" graphics (T10, Video > Graphics). Not in the web port.
//
// The core PPU tags every pixel with the 8x8 tile, palette and in-tile position that drew it (Ppu.Tags). Each frame
// becomes a 1024x960 picture (4x4 per NES pixel) in four steps:
//   1. dither blend (whole screen): a pixel inside an NES checkerboard dither (its 4 neighbours share one other colour
//      and >= 2 diagonal neighbours have its own) becomes the 50/50 mix of the two colours, so dithered skies and
//      shading turn into in-between colours (more colours per area, as on 16-bit hardware); dots and lines stay;
//   2. rounding (whole screen): Scale2x twice, so diagonal stair-steps and curves are smoothed with no tile seams;
//   3. bevel shading (whole screen): every visible shape is lit from the top left by its distance to its own edge
//      (a two-pass distance transform on the step-2 picture), so shapes look rounded and lit. It only sees colours on
//      screen: shapes the game hides (same colour as around them, e.g. a dark room) stay hidden; fades and flashes
//      just change the colours it shades;
//   4. hand-drawn tiles (graphics pack folder): `TTTT_AABBCC.png`, 32x32 RGBA, replace the 4x4 blocks of every
//      pixel of tile TTTT (hex, = CHR byte address / 16) drawn with NES colours AA BB CC in palette slots 1-3;
//      pixels with alpha < 128 show steps 1-3 instead.
// `--export-tiles DIR` writes every tile + colours seen during a run: DIR/TTTT_AABBCC.png = the built-in look of that
// tile alone (to paint over) and DIR/original/ = the NES tile at 4x.
using ReC.Nes;
using ReC.Util;

namespace ReC.Win;

internal sealed class Enhancer
{
    public const int S = 4, T = 8 * S;          // block size per NES pixel, enhanced tile size
    public const int W = 256 * S, H = 240 * S;
    private const int Levels = 33, Neutral = 16;   // shade levels (16 = unshaded)

    private readonly byte[] _chr;
    private readonly int _nTiles;
    private readonly uint[] _rgb;                // NES colour -> 0x00RRGGBB
    private readonly byte[] _dist = new byte[W * H], _colDist = new byte[W * H];
    /// <summary>T14: the "32-bit" look (Video > Graphics > Enhanced (32-bit)) instead of the 16-bit one</summary>
    public bool Mode32;
    /// <summary>T18: HD (GPU, Gpu.cs): Render makes the albedo only (steps 1, 2, 4: no baked shading) and fills
    /// HdHeight (the 32-bit look's height field of the visible shapes, 512x480) and HdLift (sprites that stand out)</summary>
    public bool ModeHd;
    public float[]? HdHeight;
    public readonly byte[] HdLift = new byte[256 * 240];
    private float[]? _h32, _t32;
    private byte[]? _sh1;
    private float[]? _shA, _shB;
    private uint[]? _aa;
    private const int D32 = 10;                  // 32-bit: height-field radius in output pixels
    private const int D = 5;                     // bevel width in output pixels
    private readonly Dictionary<long, uint[]> _art = new();
    private readonly bool[] _hasArt;
    private readonly uint[] _base = new uint[256 * 240], _x2 = new uint[512 * 480];
    private readonly int[] _linePal = new int[32];    // [group * 4 + slot] as seen on the current line
    public string? PackDir { get; private set; }
    public int ArtCount => _art.Count;
    public readonly List<string> PackErrors = new();
    public readonly HashSet<long>? Seen;

    public Enhancer(byte[] chr, int[] nesRgb, bool record = false)
    {
        _chr = chr;
        _nTiles = chr.Length / 16;
        _hasArt = new bool[_nTiles];
        _rgb = nesRgb.Select(c => (uint)c & 0xFFFFFF).ToArray();
        if (record) Seen = new();
    }

    /// <summary>level 0..32 (16 = none): > 16 toward white (less on dark colours, so outlines stay outlines), &lt; 16 toward black</summary>
    public static uint Shade(uint rgb, int level)
    {
        if (level == Neutral) return rgb;
        int r = (int)(rgb >> 16 & 255), g = (int)(rgb >> 8 & 255), b = (int)(rgb & 255);
        int s = level - Neutral;                                   // -16..16
        if (s > 0)
        {
            int lum = (299 * r + 587 * g + 114 * b) / 1000;         // 0..255
            int t = s * (64 + 3 * lum / 4) * 115 / (16 * 255);      // <= ~0.45 * 256
            r += (255 - r) * t >> 8; g += (255 - g) * t >> 8; b += (255 - b) * t >> 8;
        }
        else
        {
            int t = -s * 115 / 16;                                  // <= 0.45 * 256
            r -= r * t >> 8; g -= g * t >> 8; b -= b * t >> 8;
        }
        return (uint)r << 16 | (uint)g << 8 | (uint)b;
    }

    public static long ArtKey(int tile, int c1, int c2, int c3) => (long)tile << 24 | (long)(c1 & 63) << 16 | (long)(c2 & 63) << 8 | (uint)(c3 & 63);

    public static string ArtName(int tile, int c1, int c2, int c3) => $"{tile:X4}_{c1 & 63:X2}{c2 & 63:X2}{c3 & 63:X2}.png";

    public static bool TryParseArtName(string file, out long key)
    {
        key = 0;
        string n = Path.GetFileNameWithoutExtension(file);
        if (n.Length != 11 || n[4] != '_') return false;
        if (!int.TryParse(n[..4], System.Globalization.NumberStyles.HexNumber, null, out int t)) return false;
        var c = new int[3];
        for (int i = 0; i < 3; i++)
            if (!int.TryParse(n.Substring(5 + i * 2, 2), System.Globalization.NumberStyles.HexNumber, null, out c[i]) || c[i] > 63)
                return false;
        key = ArtKey(t, c[0], c[1], c[2]);
        return true;
    }

    // ---- hand-drawn pack
    /// <summary>(re)load DIR/*.png; bad files are listed in PackErrors and skipped. Missing folder = no art.</summary>
    public void LoadPack(string? dir)
    {
        _art.Clear(); PackErrors.Clear(); Array.Clear(_hasArt);
        PackDir = dir;
        if (dir == null || !Directory.Exists(dir)) return;
        foreach (string f in Directory.GetFiles(dir, "*.png").OrderBy(f => f, StringComparer.Ordinal))
        {
            if (!TryParseArtName(f, out long key)) { PackErrors.Add($"{Path.GetFileName(f)}: name is not TTTT_AABBCC.png"); continue; }
            int tile = (int)(key >> 24);
            if (tile >= _nTiles) { PackErrors.Add($"{Path.GetFileName(f)}: tile {tile:X4} > {_nTiles - 1:X4}"); continue; }
            try
            {
                var (w, h, px) = PngRead.Decode(File.ReadAllBytes(f));
                if (w != T || h != T) { PackErrors.Add($"{Path.GetFileName(f)}: {w}x{h}, want {T}x{T}"); continue; }
                _art[key] = px;
                _hasArt[tile] = true;
            }
            catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                PackErrors.Add($"{Path.GetFileName(f)}: {e.Message}");
            }
        }
    }

    // ---- per-tile light map
    /// <summary>slot (0-3) of tile pixel (x, y) from CHR</summary>
    public int Slot(int tile, int x, int y)
    {
        int a = tile * 16 + y;
        return (_chr[a] >> (7 - x) & 1) | (_chr[a + 8] >> (7 - x) & 1) << 1;
    }

    /// <summary>the tile's 8x8 slots upscaled 4x (Scale2x twice, edges clamp)</summary>
    public uint[] Slots32(int tile)
    {
        var s8 = new uint[64];
        for (int y = 0; y < 8; y++) for (int x = 0; x < 8; x++) s8[y * 8 + x] = (uint)Slot(tile, x, y);
        var s16 = new uint[256];
        var s32 = new uint[T * T];
        Upscale.Scale2x(s8, 8, 8, s16);
        Upscale.Scale2x(s16, 16, 16, s32);
        return s32;
    }

    /// <summary>
    /// Bevel shading of img (w x h) in place. An edge pixel touches another colour (4-neighbours); dist = 1 + the
    /// distance to the nearest edge pixel along its row or its column, capped at D (rows, then column blocks, in
    /// parallel); level = 16 + the slope of dist toward +x / +y (light from the top left). Pixels equal to `skip`
    /// (export: the transparent key) are not shaded but still count as another colour.
    /// </summary>
    public static void Bevel(uint[] img, int w, int h, byte[] dist, uint? skip = null, byte[]? colDist = null)
    {
        Distance(img, w, h, dist, colDist ?? new byte[w * h], D);
        bool hasSkip = skip.HasValue;
        uint sk = skip ?? 0;
        Rows(h, y =>
        {
            int up = Math.Max(y - 1, 0) * w, dn = Math.Min(y + 1, h - 1) * w, r = y * w;
            unsafe
            {
                fixed (uint* pi = img)
                fixed (byte* pd = dist)
                fixed (int* lv = LevelOf)
                {
                    byte* row = pd + r, du = pd + up, ddn = pd + dn;
                    uint* ir = pi + r;
                    for (int x = 0; x < w; x++)
                    {
                        if (row[x] >= D) continue;
                        if (hasSkip && ir[x] == sk) continue;
                        int gx = row[x < w - 1 ? x + 1 : x] - row[x > 0 ? x - 1 : x];
                        int lvl = lv[gx + ddn[x] - du[x] + 4 * D];
                        if (lvl != Neutral) ir[x] = Shade(ir[x], lvl);
                    }
                }
            }
        });
    }

    /// <summary>shade level for a slope gx + gy in -4D..4D: 16 + (gx + gy) / (2D) * 16 * 1.1, rounded</summary>
    private static readonly int[] LevelOf = Enumerable.Range(-4 * D, 8 * D + 1)
        .Select(g => Math.Clamp(Neutral + (int)Math.Round(g * Neutral * 1.1 / (2.0 * D)), 0, Levels - 1)).ToArray();

    /// <summary>dist = 1 + distance (capped at cap) along the row or column to the nearest pixel touching another
    /// colour (4-neighbours); rows, then column blocks, in parallel. vd = scratch, w * h.</summary>
    public static void Distance(uint[] img, int w, int h, byte[] dist, byte[] vd, int cap)
    {
        Rows(h, y =>                                           // edges + row distance -> dist
        {
            int r = y * w, last = -100;
            unsafe
            {
                fixed (uint* pi = img)
                fixed (byte* pdist = dist)
                {
                    uint* row = pi + r, up = pi + (y > 0 ? r - w : r), dn = pi + (y < h - 1 ? r + w : r);
                    byte* dd = pdist + r;
                    for (int x = 0; x < w; x++)
                    {
                        uint c = row[x];
                        bool edge = up[x] != c || dn[x] != c || (x > 0 && row[x - 1] != c) || (x < w - 1 && row[x + 1] != c);
                        if (edge) last = x;
                        int d = x - last + 1;
                        dd[x] = (byte)(d < cap ? d : cap);
                    }
                }
            }
            last = w + 100;
            for (int x = w - 1; x >= 0; x--)
            {
                int i = r + x;
                if (dist[i] == 1) { last = x; continue; }
                int d = last - x + 1;
                if (d < dist[i]) dist[i] = (byte)d;
            }
        });
        int cb = Math.Min(Environment.ProcessorCount * 2, 32), per = (w + cb - 1) / cb;
        void Cols(int b)                                       // column distance, min into dist
        {
            int x0 = b * per, x1 = Math.Min(w, x0 + per);
            if (x0 >= x1) return;
            for (int x = x0; x < x1; x++) vd[x] = (byte)(dist[x] == 1 ? 1 : cap);
            for (int y = 1; y < h; y++)
                for (int x = x0, i = y * w + x0; x < x1; x++, i++)
                {
                    int d = dist[i] == 1 ? 1 : vd[i - w] + 1;
                    vd[i] = (byte)(d < cap ? d : cap);
                }
            for (int y = h - 2; y >= 0; y--)
                for (int x = x0, i = y * w + x0; x < x1; x++, i++)
                {
                    int d = vd[i + w] + 1;
                    if (d < vd[i]) vd[i] = (byte)d;
                    if (vd[i] < dist[i]) dist[i] = vd[i];
                }
            for (int x = x0, i = (h - 1) * w + x0; x < x1; x++, i++) if (vd[i] < dist[i]) dist[i] = vd[i];
        }
        if (w * h < 4096) { for (int b = 0; b < cb; b++) Cols(b); } else Parallel.For(0, cb, Cols);
    }

    /// <summary>rows 0..h-1 in parallel blocks (small images inline)</summary>
    private static void Rows(int h, Action<int> row)
    {
        if (h < 64) { for (int y = 0; y < h; y++) row(y); return; }
        int blocks = Math.Min(Environment.ProcessorCount * 2, 32), per = (h + blocks - 1) / blocks;
        Parallel.For(0, blocks, b => { for (int y = b * per, e = Math.Min(h, y + per); y < e; y++) row(y); });
    }

    private static uint Mix(uint a, uint b) => ((a & 0xFEFEFE) >> 1) + ((b & 0xFEFEFE) >> 1);

    /// <summary>colour of slot s for palette group g (0-3 BG, 4-7 sprites); slot 0 = backdrop</summary>
    private static int PalColour(byte[] pal, int g, int s) => s == 0 ? pal[0] : pal[(g < 4 ? g * 4 : 0x10 + (g - 4) * 4) + s];

    /// <summary>fb + tags -> dst (W x H, 0x00RRGGBB); pal = the palette at the end of the frame (pack keys only)</summary>
    public void Render(byte[] fb, int[] tags, byte[] pal, uint[] dst)
    {
        // 1. dither blend
        for (int y = 0; y < 240; y++)
            for (int x = 0; x < 256; x++)
            {
                int i = y * 256 + x, p = fb[i];
                bool d = false;
                if (x > 0 && y > 0 && x < 255 && y < 239)
                {
                    int q = fb[i - 1];
                    if (q != p && fb[i + 1] == q && fb[i - 256] == q && fb[i + 256] == q)
                    {
                        int diag = (fb[i - 257] == p ? 1 : 0) + (fb[i - 255] == p ? 1 : 0) + (fb[i + 255] == p ? 1 : 0) + (fb[i + 257] == p ? 1 : 0);
                        d = diag >= 2;
                    }
                }
                _base[i] = d ? Mix(_rgb[p & 0x3F], _rgb[fb[i - 1] & 0x3F]) : _rgb[p & 0x3F];
            }
        // 2. rounding
        Rows(240, y => Upscale.Scale2xRow(_base, 256, 240, _x2, y));
        Rows(480, y => Upscale.Scale2xRow(_x2, 512, 480, dst, y));
        // 3. bevel shading (16-bit) / soft lighting (32-bit) / none (HD: the GPU lights it)
        if (ModeHd) HdHeight = HeightField();
        else if (Mode32) Light32(dst);
        else Bevel(dst, W, H, _dist, null, _colDist);

        // 4. hand-drawn tiles
        var lp = _linePal;
        for (int g = 0; g < 8; g++) for (int s = 0; s < 4; s++) lp[g * 4 + s] = PalColour(pal, g, s) & 0x3F;
        for (int y = 0; y < 240; y++)
        {
            for (int x = 0; x < 256; x++)       // the line's own colours (the game changes palettes mid-frame)
            {
                int i = y * 256 + x, tag = tags[i];
                if ((tag & Ppu.TagValid) == 0 || (tag & 0x1FFF) >= _nTiles) continue;
                int s = Slot(tag & 0x1FFF, tag >> 13 & 7, tag >> 16 & 7);
                if (s != 0) lp[(tag >> 19 & 7) * 4 + s] = fb[i] & 0x3F;
            }
            for (int x = 0; x < 256; x++)
            {
                int i = y * 256 + x, tag = tags[i];
                if ((tag & Ppu.TagValid) == 0 || (tag & 0x1FFF) >= _nTiles) continue;
                int tile = tag & 0x1FFF, col = tag >> 13 & 7, row = tag >> 16 & 7, g = tag >> 19 & 7;
                bool fh = (tag & 1 << 22) != 0, fv = (tag & 1 << 23) != 0;
                uint[]? art = null;
                if (Seen != null || _hasArt[tile])
                {
                    long key = ArtKey(tile, lp[g * 4 + 1], lp[g * 4 + 2], lp[g * 4 + 3]);
                    Seen?.Add(key);
                    if (_hasArt[tile]) _art.TryGetValue(key, out art);
                }
                if (art == null) continue;
                int o = y * S * W + x * S;
                for (int sy = 0; sy < S; sy++)
                {
                    int ty = row * S + (fv ? S - 1 - sy : sy);
                    for (int sx = 0; sx < S; sx++)
                    {
                        int tx = col * S + (fh ? S - 1 - sx : sx), at = o + sy * W + sx;
                        if (art[ty * T + tx] >> 24 >= 128) dst[at] = art[ty * T + tx] & 0xFFFFFF;
                    }
                }
            }
        }
        if (ModeHd) LiftHd(fb, tags);                   // 5. HD: which sprites stand above the background
        else if (Mode32)
        {
            Shadows32(fb, tags, dst);                  // 5. soft sprite shadows
            AntiAlias32(dst);                          // 6. edge anti-aliasing
        }
    }

    // ---- T14 "32-bit" look ------------------------------------------------------------------------------------
    // Light32: a smooth height field per visible shape (pillow profile from the distance to the shape's edge, blurred),
    // normals from its slope, Lambert diffuse + Blinn specular from the top left; a flat area keeps its exact colour
    // (the light is normalised to the flat normal). Like the 16-bit bevel it only sees colours on screen.
    private const float Amb = 0.55f, Kd = 0.45f, Bump = 1.3f, SpecK = 0.30f;   // Bump for the half-resolution field
    private static readonly (float X, float Y, float Z) Lv = Norm(-0.45f, -0.62f, 0.64f);
    private static readonly (float X, float Y, float Z) Hv = Norm(Lv.X, Lv.Y, Lv.Z + 1);
    private const float SpecPow = 18;                                         // as nh^16 * nh^2 below
    private static readonly float Diff0 = Lv.Z, Spec0 = MathF.Pow(Hv.Z, SpecPow);

    private static (float, float, float) Norm(float x, float y, float z)
    {
        float l = MathF.Sqrt(x * x + y * y + z * z);
        return (x / l, y / l, z / l);
    }

    private float[]? _f32, _s32;
    private const int LW = W / 2, LH = H / 2;     // lighting resolution: the 512x480 rounding step (smooth anyway)

    /// <summary>lighting computed on half (the 2x image, D32 / 2 radius), applied to the 4x image per 2x2 block</summary>
    private void Light32(uint[] img)
    {
        const int Cap = D32 / 2;
        var h = HeightField();
        var f = _f32 ??= new float[LW * LH];
        var sp = _s32 ??= new float[LW * LH];
        var dist = _dist;
        // per 2x pixel: light factor and highlight (0 / 1 = flat)
        Rows(LH, y =>
        {
            int r = y * LW, up = Math.Max(y - 1, 0) * LW, dn = Math.Min(y + 1, LH - 1) * LW;
            for (int x = 0; x < LW; x++)
            {
                int i = r + x;
                if (dist[i] >= Cap) { f[i] = 1; sp[i] = 0; continue; }
                float gx = (h[r + Math.Min(x + 1, LW - 1)] - h[r + Math.Max(x - 1, 0)]) * Bump;
                float gy = (h[dn + x] - h[up + x]) * Bump;
                float inv = MathF.ReciprocalSqrtEstimate(gx * gx + gy * gy + 1);
                float nx = -gx * inv, ny = -gy * inv, nz = inv;
                float diff = MathF.Max(0, nx * Lv.X + ny * Lv.Y + nz * Lv.Z);
                f[i] = (Amb + Kd * diff) / (Amb + Kd * Diff0);
                float nh = MathF.Max(0, nx * Hv.X + ny * Hv.Y + nz * Hv.Z), n2 = nh * nh, n4 = n2 * n2, n8 = n4 * n4;
                sp[i] = MathF.Max(0, n8 * n8 * n2 - Spec0) * SpecK * 255;                 // nh^18
            }
        });
        Rows(H, y =>
        {
            int r = y * W, lr = (y >> 1) * LW;
            for (int x = 0; x < W; x++)
            {
                int li = lr + (x >> 1);
                float ff = f[li], s2 = sp[li];
                if (ff == 1 && s2 == 0) continue;
                uint c = img[r + x];
                // the highlight scales with the surface's own brightness: black stays black, light colours shine
                float cr = c >> 16 & 255, cg = c >> 8 & 255, cb = c & 255;
                float s3 = s2 * (0.15f + 0.85f * (cr * 0.299f + cg * 0.587f + cb * 0.114f) / 255f);
                img[r + x] = (uint)Math.Clamp((int)(cr * ff + s3), 0, 255) << 16 | (uint)Math.Clamp((int)(cg * ff + s3), 0, 255) << 8 |
                             (uint)Math.Clamp((int)(cb * ff + s3), 0, 255);
            }
        });
    }

    /// <summary>the height field of the visible shapes at 512x480 (the 2x rounding step): pillow profile of each pixel's
    /// distance to its shape's edge (cap D32 / 2), blurred 3x3; leaves the distances in _dist (Light32 uses them)</summary>
    private float[] HeightField()
    {
        const int Cap = D32 / 2;
        Distance(_x2, LW, LH, _dist, _colDist, Cap);
        var h = _h32 ??= new float[LW * LH];
        var t = _t32 ??= new float[LW * LH];
        var dist = _dist;
        var pl = PillowHalf;
        Rows(LH, y =>
        {
            int r = y * LW;
            for (int x = 0; x < LW; x++)
            {
                float acc = 0;
                if (x >= 1 && x < LW - 1) acc = pl[dist[r + x - 1]] + pl[dist[r + x]] + pl[dist[r + x + 1]];
                else for (int k = -1; k <= 1; k++) acc += pl[dist[r + Math.Clamp(x + k, 0, LW - 1)]];
                t[r + x] = acc * (1f / 3);
            }
        });
        Rows(LH, y =>
        {
            int r = y * LW, a0 = Math.Max(y - 1, 0) * LW, a2 = Math.Min(y + 1, LH - 1) * LW;
            for (int x = 0; x < LW; x++) h[r + x] = (t[a0 + x] + t[r + x] + t[a2 + x]) * (1f / 3);
        });
        return h;
    }

    private readonly int[] _liftStack = new int[256 * 240];
    private readonly byte[] _liftSeen = new byte[256 * 240];
    private readonly List<int> _liftComp = new();

    /// <summary>HD: a sprite (8-connected sprite pixels) stands above the background (HdLift = 255) only if one of its
    /// edge pixels shows a colour different from the non-sprite pixel next to it: a sprite the game hides (drawn in the
    /// colour around it) gets no height, so it casts no shadow and stays hidden</summary>
    private void LiftHd(byte[] fb, int[] tags)
    {
        var lift = HdLift; var seen = _liftSeen; var st = _liftStack;
        Array.Clear(lift); Array.Clear(seen);
        static bool Spr(int t) => (t & Ppu.TagValid) != 0 && (t >> 19 & 7) >= 4;
        for (int start = 0; start < 256 * 240; start++)
        {
            if (seen[start] != 0 || !Spr(tags[start])) continue;
            int top = 0;
            bool shows = false;
            st[top++] = start; seen[start] = 1;
            var comp = _liftComp;
            comp.Clear();
            while (top > 0)
            {
                int p = st[--top];
                comp.Add(p);
                int px = p & 255, py = p >> 8;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = px + dx, ny = py + dy;
                        if ((uint)nx >= 256 || (uint)ny >= 240) continue;
                        int q = ny * 256 + nx;
                        if (Spr(tags[q])) { if (seen[q] == 0) { seen[q] = 1; st[top++] = q; } }
                        else if ((dx == 0 || dy == 0) && (fb[q] & 0x3F) != (fb[p] & 0x3F)) shows = true;
                    }
            }
            if (shows) foreach (int p in comp) lift[p] = 255;
        }
    }

    /// <summary>pillow height profile for the half-resolution distance (cap D32 / 2)</summary>
    private static readonly float[] PillowHalf = Enumerable.Range(0, D32 / 2 + 1)
        .Select(d => d == 0 ? 0f : (float)(1 - Math.Pow(1 - Math.Min(d, D32 / 2) / (double)(D32 / 2), 2))).ToArray();

    // Shadows32: every sprite pixel casts a soft shadow 2 NES pixels down-right onto non-sprite pixels (40 % darker at
    // the core, blurred over ~1.5 NES pixels). Only a sprite pixel that differs in colour from where its shadow lands
    // casts one, so a sprite drawn in the background colour (hidden) stays hidden.
    private void Shadows32(byte[] fb, int[] tags, uint[] img)
    {
        var src = _sh1 ??= new byte[256 * 240];
        Array.Clear(src);
        bool any = false;
        for (int y = 0; y < 238; y++)
            for (int x = 0; x < 254; x++)
            {
                int i = y * 256 + x, t = tags[i];
                if ((t & Ppu.TagValid) == 0 || (t >> 19 & 7) < 4) continue;
                int j = i + 2 * 256 + 2;
                if (fb[i] != fb[j]) { src[j] = 1; any = true; }
            }
        if (!any) return;
        // alpha at the NES resolution: two 3x3 box blurs (~1.5 NES pixels), then bilinear up to 4x
        var a = _shA ??= new float[256 * 240];
        var b = _shB ??= new float[256 * 240];
        for (int i = 0; i < a.Length; i++) a[i] = src[i];
        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < 240; y++)
                for (int x = 0; x < 256; x++)
                {
                    float acc = 0;
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int yy = Math.Clamp(y + dy, 0, 239) * 256;
                        acc += a[yy + Math.Max(x - 1, 0)] + a[yy + x] + a[yy + Math.Min(x + 1, 255)];
                    }
                    b[y * 256 + x] = acc / 9;
                }
            (a, b) = (b, a);
        }
        var al1 = a;
        Rows(H, y =>
        {
            int r = y * W;
            float fy = (y + 0.5f) / S - 0.5f;
            int y0 = Math.Clamp((int)MathF.Floor(fy), 0, 239), y1 = Math.Min(y0 + 1, 239);
            float wy = Math.Clamp(fy - y0, 0, 1);
            int tr = (y / S) * 256;
            for (int x = 0; x < W; x++)
            {
                float fx = (x + 0.5f) / S - 0.5f;
                int x0 = Math.Clamp((int)MathF.Floor(fx), 0, 255), x1 = Math.Min(x0 + 1, 255);
                float wx = Math.Clamp(fx - x0, 0, 1);
                float top = al1[y0 * 256 + x0] * (1 - wx) + al1[y0 * 256 + x1] * wx;
                float bot = al1[y1 * 256 + x0] * (1 - wx) + al1[y1 * 256 + x1] * wx;
                float al = top * (1 - wy) + bot * wy;
                if (al < 0.004f) continue;
                int t = tags[tr + x / S];
                if ((t & Ppu.TagValid) != 0 && (t >> 19 & 7) >= 4) continue;   // never on the sprites themselves
                float f = 1 - 0.4f * al;
                uint c = img[r + x];
                img[r + x] = (uint)((c >> 16 & 255) * f) << 16 | (uint)((c >> 8 & 255) * f) << 8 | (uint)((c & 255) * f);
            }
        });
    }

    // AntiAlias32: a pixel on a colour edge becomes (4 x itself + its 4 neighbours) / 8 -> smooth, anti-aliased curves;
    // flat areas are untouched.
    private void AntiAlias32(uint[] img)
    {
        var src = _aa ??= new uint[W * H];
        Array.Copy(img, src, img.Length);
        Rows(H, y =>
        {
            int r = y * W, up = y > 0 ? r - W : r, dn = y < H - 1 ? r + W : r;
            for (int x = 0; x < W; x++)
            {
                uint c = src[r + x], l = src[r + Math.Max(x - 1, 0)], rr = src[r + Math.Min(x + 1, W - 1)], u = src[up + x], d = src[dn + x];
                if (c == l && c == rr && c == u && c == d) continue;
                uint R(uint v) => v >> 16 & 255;
                uint G(uint v) => v >> 8 & 255;
                uint B(uint v) => v & 255;
                uint nr = (4 * R(c) + R(l) + R(rr) + R(u) + R(d) + 4) >> 3;
                uint ng = (4 * G(c) + G(l) + G(rr) + G(u) + G(d) + 4) >> 3;
                uint nb = (4 * B(c) + B(l) + B(rr) + B(u) + B(d) + 4) >> 3;
                img[r + x] = nr << 16 | ng << 8 | nb;
            }
        });
    }

    /// <summary>--export-tiles: every tile + colours seen (not blank, not one colour) -> DIR/TTTT_AABBCC.png (the built-in look of the tile alone:
    /// rounded + shaded, slot 0 transparent) and DIR/original/TTTT_AABBCC.png (the NES tile at 4x); returns the count</summary>
    public int Export(string dir)
    {
        if (Seen == null) return 0;
        Directory.CreateDirectory(Path.Combine(dir, "original"));
        int n = 0;
        foreach (long key in Seen)
        {
            int tile = (int)(key >> 24);
            int[] cs = { 0, (int)(key >> 16 & 63), (int)(key >> 8 & 63), (int)(key & 63) };
            if (!Exportable(tile, cs[1], cs[2], cs[3])) continue;
            n++;
            var s32 = Slots32(tile);
            const uint Clear = 0xFF000000;                    // not an RGB colour: the transparent key
            var img = new uint[T * T];
            for (int i = 0; i < img.Length; i++) img[i] = s32[i] == 0 ? Clear : _rgb[cs[s32[i]]];
            Bevel(img, T, T, new byte[T * T], Clear);
            var enh = new byte[T * T * 4];
            var org = new byte[T * T * 4];
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    int o = (y * T + x) * 4;
                    if (img[y * T + x] != Clear) Put(enh, o, img[y * T + x]);
                    int os = Slot(tile, x / S, y / S);
                    if (os != 0) Put(org, o, _rgb[cs[os]]);
                }
            string name = ArtName(tile, cs[1], cs[2], cs[3]);
            File.WriteAllBytes(Path.Combine(dir, name), PngRead.EncodeRgba(T, T, enh));
            File.WriteAllBytes(Path.Combine(dir, "original", name), PngRead.EncodeRgba(T, T, org));
        }
        return n;
    }

    /// <summary>export skips blank tiles and one-colour combinations (fade steps: nothing to draw)</summary>
    public bool Exportable(int tile, int c1, int c2, int c3)
    {
        if (c1 == c2 && c2 == c3) return false;
        for (int i = 0; i < 16; i++) if (_chr[tile * 16 + i] != 0) return true;
        return false;
    }

    private static void Put(byte[] b, int o, uint c)
    {
        b[o] = (byte)(c >> 16); b[o + 1] = (byte)(c >> 8); b[o + 2] = (byte)c; b[o + 3] = 255;
    }
}
