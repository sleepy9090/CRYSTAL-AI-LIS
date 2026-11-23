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
        var vd = colDist ?? new byte[w * h];
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
                        dd[x] = (byte)(d < D ? d : D);
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
            for (int x = x0; x < x1; x++) vd[x] = (byte)(dist[x] == 1 ? 1 : D);
            for (int y = 1; y < h; y++)
                for (int x = x0, i = y * w + x0; x < x1; x++, i++)
                {
                    int d = dist[i] == 1 ? 1 : vd[i - w] + 1;
                    vd[i] = (byte)(d < D ? d : D);
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
        // 3. bevel shading
        Bevel(dst, W, H, _dist, null, _colDist);

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
