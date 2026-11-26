// re-c Windows host — widescreen playfield (W1-W2, Video > Widescreen playfield). Not in the web port.
//
// The NES draws 256 columns; the game's map is wider. The columns beside the screen come from the loaded area itself:
//   AreaCache   the whole area as packed pixels (pixel value 0-3 | palette group << 2 | global CHR tile << 4), built
//               like Bg.RenderArea (core, = bg.js renderArea: screen grid $6300, visited bitmap $62F0 substitution,
//               metatiles, attributes, CHR R0 / R1 from $07F0 / $07F1) but from a RAM snapshot taken on the game thread,
//               so it can be built on a worker thread (no stall when an area is entered).
//   Widescreen  per frame: the camera before the frame ($02/$03 x, s8($05)*240+$04 y: the area pixel at screen 0,0;
//               98-100 % of the playfield pixels equal the area render there), a match test on a sample of playfield
//               pixels (the sides only show while the screen really shows the map: not in menus, cut-scenes, area
//               transitions), and the wide picture: the game's 256 columns in the middle, the area's pixels in the
//               current palette at the sides (fades and palette cycling follow), black outside the area and on the
//               status-bar / overscan lines.
using ReC.Game;
using ReC.Nes;

namespace ReC.Win;

internal sealed class AreaCache
{
    public int W, H;
    public uint[] Px = Array.Empty<uint>();
    /// <summary>what the cache was built from (area id, grid, $67, CHR banks; + visited bitmap)</summary>
    public ulong AreaKey, FullKey;

    /// <summary>the RAM a build reads, copied on the game thread</summary>
    public sealed class Snapshot
    {
        public byte[] Ram = new byte[0x800], Wram = new byte[0x400];
        public ulong AreaKey, FullKey;
        public int Rd(int a) => a < 0x800 ? Ram[a] : a is >= 0x6000 and < 0x6400 ? Wram[a - 0x6000] : 0;
    }

    public static Snapshot Take(Machine m)
    {
        var s = new Snapshot();
        for (int a = 0; a < 0x800; a++) s.Ram[a] = (byte)m.Rd(a);
        for (int a = 0; a < 0x400; a++) s.Wram[a] = (byte)m.Rd(0x6000 + a);
        (s.AreaKey, s.FullKey) = Keys(m);
        return s;
    }

    /// <summary>(area key, area + visited-screens key) as hashes: a new area drops the cache, a new visited screen only
    /// rebuilds it (the old one stays up meanwhile)</summary>
    public static (ulong Area, ulong Full) Keys(Machine m)
    {
        ulong h = 1469598103934665603;
        void Mix(int v) { h ^= (ulong)(v & 0xFF); h *= 1099511628211; }
        foreach (int a in new[] { 0x6C, 0x62FC, 0x62FD, 0x07F0, 0x07F1, 0x62FF, 0x6A, 0x6B }) Mix(m.Rd(a));
        Mix(m.Rd(0x67) & 0xFC);                  // $67 b0-1 count every 4 frames; the map only uses &$FC / &$3C
        for (int a = 0x6300; a < 0x6380; a++) Mix(m.Rd(a));
        ulong area = h;
        for (int a = 0x62F0; a < 0x6300; a++) Mix(m.Rd(a));
        return (area, h);
    }

    /// <summary>= Bg.RenderArea(m, Visit.Ram), packed, from a snapshot (thread-safe: data files are read-only)</summary>
    /// <summary>reuse = a buffer of an older cache (no new ~30 MB array per area: large-object-heap allocations force
    /// blocking full GCs, which paused the game and its sound right at scene changes)</summary>
    public static AreaCache? Build(Data data, Snapshot s, uint[]? reuse = null)
    {
        try { return BuildUnsafe(data, s, reuse); }
        catch (Exception e) when (e is IndexOutOfRangeException or ArgumentException) { return null; }   // map memory not valid (yet)
    }

    private static AreaCache? BuildUnsafe(Data data, Snapshot s, uint[]? reuse)
    {
        if (s.Rd(0x62FC) > 7 || s.Rd(0x62FD) > 15) return null;
        int gw = s.Rd(0x62FC) + 1, gh = s.Rd(0x62FD) + 1;
        int w = gw * 256, h = gh * 240;
        var px = reuse != null && reuse.Length >= w * h ? reuse : new uint[Math.Max(w * h, 1 << 21)];   // every pixel is written below
        byte[] chr = data.Chr, screens = data.BinFile("screens"), screensB10 = data.BinFile("screensB10"), mtb = data.BinFile("metatiles");
        int r0 = s.Rd(0x07F0), r1 = s.Rd(0x07F1), k67 = s.Rd(0x67);
        int Mt(int addr) => mtb[addr - 0x8000];
        int nChr = chr.Length >> 10;
        for (int gy = 0; gy < gh; gy++)
            for (int gx = 0; gx < gw; gx++)
            {
                int sid = s.Rd(0x6300 + (((gy << 3) & 0x78) | (gx & 7)));
                bool vis = (s.Rd(0x62F0 + gy) & (1 << (gx & 7))) != 0;
                int lo = sid & 0x3F;
                (byte[] buf, int off) = (s.Rd(0x62FF) & 0x80) != 0 && lo < 0x20 ? (screensB10, lo << 8) : (screens, sid << 8);
                if (off + 240 > buf.Length) return null;
                for (int r = 0; r < 15; r++)
                    for (int c = 0; c < 16; c++)
                    {
                        int mt = buf[off + r * 16 + c];
                        if (vis && mt < 0x20) mt = Mt(((s.Rd(0x6B) << 8) | s.Rd(0x6A)) + mt);
                        int g = (Mt(0xB000 + ((k67 & 0x3C) >> 2) * 0x40 + (mt >> 2)) >> (2 * (mt & 3))) & 3;
                        for (int q = 0; q < 4; q++)
                        {
                            int t = Mt((((k67 & 0xFC) | q) << 8) | mt);
                            int bank = t < 0x80 ? (r0 & 0xFE) + (t >> 6) : (r1 & 0xFE) + ((t >> 6) & 1);
                            int gt = (bank % nChr) * 64 + (t & 0x3F);
                            int x0 = gx * 256 + c * 16 + (q & 1) * 8, y0 = gy * 240 + r * 16 + (q >> 1) * 8;
                            for (int y = 0; y < 8; y++)
                            {
                                int o = gt * 16 + y;
                                for (int x = 0; x < 8; x++)
                                {
                                    int v = ((chr[o] >> (7 - x)) & 1) | (((chr[o + 8] >> (7 - x)) & 1) << 1);
                                    px[(y0 + y) * w + x0 + x] = (uint)(v | g << 2 | gt << 4);
                                }
                            }
                        }
                    }
            }
        return new AreaCache { W = w, H = h, Px = px, AreaKey = s.AreaKey, FullKey = s.FullKey };
    }

    /// <summary>NES colour of a packed pixel in a palette (32 bytes, PPU order)</summary>
    public static int Color(uint p, byte[] pal)
    {
        int v = (int)(p & 3), g = (int)(p >> 2 & 3);
        return (v == 0 ? pal[0] : pal[g * 4 + v]) & 0x3F;
    }
}

/// <summary>the camera for the frame about to be drawn (read before RunFrame), the cache, the fade of the sides</summary>
internal sealed class Widescreen
{
    public static readonly int[] Widths = { 256, 384, 424, 560 };     // off, 16:10, 16:9, 21:9 (square pixels, 240 lines)
    public AreaCache? Cache;
    private System.Threading.Tasks.Task<AreaCache?>? _building;
    private ulong _areaKey, _fullKey;
    private uint[]? _spare;                                   // the previous cache's pixels, for the next build
    private int _bad;
    public int CamX, CamY;
    /// <summary>the area pixel at screen (0, 0) used for the sides: the camera + a correction (Search) for areas where the
    /// camera bytes and the screen disagree (e.g. a scenario or a keep-position map: the screen shows another place)</summary>
    public int MapX => CamX + _corrX;
    public int MapY => CamY + _corrY;
    private int _corrX, _corrY, _poor;
    private AreaCache? _corrFor;
    /// <summary>0..1: how much of the sides shows (eases with the match test)</summary>
    public float Show;
    public bool Match { get; private set; }

    /// <summary>on the game thread, before RunFrame: the camera, and a cache (re)build when the area changed</summary>
    public void BeforeFrame(Machine m, Data data, bool async = true)
    {
        CamX = m.Rd(2) | m.Rd(3) << 8;
        CamY = (sbyte)m.Rd(5) * 240 + m.Rd(4);
        if (_building is { IsCompleted: true } b)
        {
            var res = b.IsCompletedSuccessfully ? b.Result : null;     // a failed build is ignored (never thrown here)
            _building = null;
            if (res != null && res.AreaKey == _areaKey) { _spare = Cache?.Px; Cache = res; }
            else _spare ??= res?.Px;
        }
        // gameplay with the area loaded: not new-game init (0), area load (1), the wake-up cut-scene ($13)
        int sub = m.Rd(0x41);
        if (m.Rd(0x40) != 1 || sub is 0 or 1 or 0x13 || m.Rd(0x62FC) > 7 || m.Rd(0x62FD) > 15) return;
        var (area, full) = AreaCache.Keys(m);
        if (area != _areaKey) { _areaKey = area; _spare ??= Cache?.Px; Cache = null; Show = 0; _bad = 0; _fullKey = 0; }   // another area: drop at once
        if (full == _fullKey || _building != null) return;
        _fullKey = full;
        var snap = AreaCache.Take(m);
        var buf = _spare; _spare = null;                      // never the current cache's array
        if (async) _building = System.Threading.Tasks.Task.Run(() => AreaCache.Build(data, snap, buf));
        else { var c = AreaCache.Build(data, snap, buf); if (c != null) { _spare = Cache?.Px; Cache = c; } else _spare = buf; }
    }

    // per line: the area offset that matches (0 = the camera, or the line's own scroll after a mid-frame split), whether
    // the line verifiably shows the map, and its run of failed frames (a line turns off after 3)
    public readonly int[] LineDx = new int[240], LineDy = new int[240];
    public readonly bool[] LineOk = new bool[240];
    private readonly int[] _lineBad = new int[240];

    /// <summary>after the frame, per playfield line (8..hudTop-1): test the camera mapping and, when lineScroll is given and the
    /// line's scroll differs from the first line's (a mid-frame split), the line's own scroll, on every 3rd pixel (sprite
    /// pixels skipped when sp is given); a mapping with >= 88 % equal pixels marks the line as map with that offset.
    /// Show (the fade of all sides) eases in while >= 60 % of the lines are map and out after 8 frames under 30 %.</summary>
    public void AfterFrame(byte[] fb, byte[] pal, int hudTop, byte[][]? linePal = null, int[]? lineScroll = null, byte[]? sp = null)
    {
        var c = Cache;
        if (c == null)
        {
            Array.Clear(LineOk); Match = false; _bad = 8;
            Show = Math.Max(0, Show - 0.25f);
            return;
        }
        if (!ReferenceEquals(_corrFor, c)) { _corrFor = c; if (c.AreaKey != _corrArea) { _corrX = _corrY = 0; _corrArea = c.AreaKey; } _poor = 3; }
        static int Sx(int v) => ((v >> 10 & 1) << 8) | (v & 31) << 3 | (v >> 16 & 7);
        static int Sy(int v) => (v >> 11 & 1) * 240 + (v >> 5 & 31) * 8 + (v >> 12 & 7);
        int y0 = -1;
        if (lineScroll != null) for (int y = 1; y < 40 && y0 < 0; y++) if (lineScroll[y] >= 0) y0 = y;
        int lines = 0, ok = 0;
        for (int y = 0; y < 240; y++)
        {
            if (y < 8 || y >= hudTop) { LineOk[y] = false; _lineBad[y] = 0; continue; }
            var p = linePal?[y] ?? pal;
            int bestN = 0, bestS = 0, bdx = 0, bdy = 0;
            for (int cand = 0; cand < 3; cand++)
            {
                // 0: the camera; 1: the line's scroll relative to the first line (a mid-frame split); 2: the line's scroll
                // against the map position, within half a screen (a screen shake moves every line off the camera)
                int dx = 0, dy = 0;
                if (cand > 0)
                {
                    if (lineScroll == null || lineScroll[y] < 0 || (cand == 1 && y0 < 0)) continue;
                    int v = lineScroll[y];
                    if (cand == 1)
                    {
                        int v0 = lineScroll[y0];
                        dx = ((Sx(v) - Sx(v0)) % 512 + 768) % 512 - 256;
                        dy = ((Sy(v) - Sy(v0) - (y - y0)) % 480 + 720) % 480 - 240;
                    }
                    else
                    {
                        dx = ((Sx(v) - MapX) % 256 + 384) % 256 - 128;
                        dy = ((Sy(v) - y - MapY) % 240 + 360) % 240 - 120;
                    }
                    if (dx == 0 && dy == 0) continue;
                }
                int ay = y + MapY + dy, n = 0, s = 0;
                if (ay < 0 || ay >= c.H) continue;
                for (int x = 8; x < 256; x += 3)     // every 3rd: a 2- or 4-pixel pattern cannot fake a match
                {
                    int i = y * 256 + x, ax = x + MapX + dx;
                    if (ax < 0 || ax >= c.W || (sp != null && sp[i] != 0)) continue;
                    n++;
                    if ((fb[i] & 63) == AreaCache.Color(c.Px[ay * c.W + ax], p)) s++;
                }
                if (n >= 12 && s * bestN >= bestS * n && (bestN == 0 || s * bestN > bestS * n)) { bestN = n; bestS = s; bdx = dx; bdy = dy; }
            }
            bool now = bestN >= 12 && bestS * 100 >= bestN * 88;
            if (now) { LineOk[y] = true; _lineBad[y] = 0; LineDx[y] = bdx; LineDy[y] = bdy; }
            else if (bestN < 12) { }                                        // all sprites / outside: keep the last state
            else if (bestS * 2 < bestN || ++_lineBad[y] >= 3) LineOk[y] = false;   // clearly wrong: off now; borderline: after 3
            lines++;
            if (LineOk[y]) ok++;
        }
        int pct = lines > 0 ? ok * 100 / lines : 0;
        // many lines fail for 3 frames (or a new cache): look for the place in the area the screen really shows
        if (pct < 40 && lines > 40 && ++_poor >= 3) { _poor = 0; Search(c, fb, pal, hudTop, linePal, lineScroll, sp); }
        else if (pct >= 60) _poor = 0;
        Match = pct >= 50;
        if (pct >= 60) { _bad = 0; Show = Math.Min(1, Show + 0.125f); }
        else if (pct < 30 && ++_bad >= 8) Show = Math.Max(0, Show - 0.25f);
    }

    private ulong _corrArea;

    /// <summary>the correction with the most equal pixels (>= 85 % on a sample of lines): candidates are every screen-sized
    /// step (256 x 240) of the area that agrees with the PPU scroll of the first line (nametable space repeats every 512 x
    /// 480, a screen is 256 x 240), or with the camera when there is no scroll; the camera itself first</summary>
    private void Search(AreaCache c, byte[] fb, byte[] pal, int hudTop, byte[][]? linePal, int[]? lineScroll, byte[]? sp)
    {
        int y0 = -1;
        if (lineScroll != null) for (int y = 1; y < 40 && y0 < 0; y++) if (lineScroll[y] >= 0) y0 = y;
        int nx = CamX, ny = CamY;
        if (y0 >= 0)
        {
            int v = lineScroll![y0];
            nx = ((v >> 10 & 1) << 8) | (v & 31) << 3 | (v >> 16 & 7);
            ny = (v >> 11 & 1) * 240 + (v >> 5 & 31) * 8 + (v >> 12 & 7) - y0;
        }
        int bx = ((nx % 256) + 256) % 256, by = ((ny % 240) + 240) % 240;
        int top = Math.Min(hudTop, 200);
        double Score(int ox, int oy)
        {
            int n = 0, s = 0;
            for (int y = 12; y < top; y += 9)
            {
                var p = linePal?[y] ?? pal;
                int ay = y + oy;
                if (ay < 0 || ay >= c.H) continue;
                for (int x = 8; x < 256; x += 5)
                {
                    int i = y * 256 + x, ax = x + ox;
                    if (ax < 0 || ax >= c.W || (sp != null && sp[i] != 0) || (fb[i] & 63) == 0x0F) continue;
                    n++;
                    if ((fb[i] & 63) == AreaCache.Color(c.Px[ay * c.W + ax], p)) s++;
                }
            }
            return n >= 60 ? (double)s / n : 0;
        }
        double best = Score(CamX, CamY);
        int cx = 0, cy = 0;
        if (best < 0.85)
            for (int oy = by - 240; oy < c.H; oy += 240)
                for (int ox = bx - 256; ox < c.W; ox += 256)
                {
                    double sc = Score(ox, oy);
                    if (sc > best + 0.02) { best = sc; cx = ox - CamX; cy = oy - CamY; }
                }
        if (best >= 0.85) { _corrX = cx; _corrY = cy; }
    }

    /// <summary>the lines whose sides show the map: shown (fade above `min`), verified (LineOk), above the status bar, and not
    /// a line the game leaves black (columns 8-255 all $0F: the unloaded rows above the status bar)</summary>
    public void MapLines(byte[] fb, int hudTop, float min, bool[] dst)
    {
        for (int y = 0; y < 240; y++)
        {
            bool map = Show > min && y < hudTop && LineOk[y];
            if (map) { map = false; for (int x = 8; x < 256 && !map; x++) map = (fb[y * 256 + x] & 0x3F) != 0x0F; }
            dst[y] = map;
        }
    }

    /// <summary>NES colour index of side pixel (x in wide coordinates, e = side width), or 0x0F (black) when outside the
    /// area / not shown</summary>
    /// <summary>the side pixel's background is opaque (pixel value != 0): a sprite behind it stays hidden</summary>
    public bool SideOpaque(int wx, int y, int e)
    {
        var c = Cache;
        int ax = wx - e + MapX + LineDx[y], ay = y + MapY + LineDy[y];
        return c != null && ax >= 0 && ay >= 0 && ax < c.W && ay < c.H && (c.Px[ay * c.W + ax] & 3) != 0;
    }

    public int SideColor(int wx, int y, int e, byte[] pal)
    {
        var c = Cache;
        if (c == null) return 0x0F;
        int ax = wx - e + MapX + LineDx[y], ay = y + MapY + LineDy[y];
        if (ax < 0 || ay < 0 || ax >= c.W || ay >= c.H) return 0x0F;
        return AreaCache.Color(c.Px[ay * c.W + ax], pal);
    }
}

/// <summary>widescreen: the sprites of objects in the side columns. The game's sprite pass (Sprites.SpriteDraw 28:800E)
/// only emits entries on its 256 columns: objects further out are flagged off-screen, and metasprite entries that
/// would wrap past an edge are skipped. This replays the same pass for every active object (same off-screen and
/// vertical tests, overlay order, mirroring, animation frame, tile offset, priority, sinking) from a RAM snapshot taken
/// with the camera before the frame (the state the shown OAM was built from), and draws only the pixels outside the
/// game's columns: pixel = palette group << 2 | colour (| $40 behind the background), as SceneRaster.Sp.</summary>
internal sealed class SideSprites
{
    public readonly byte[] Ram = new byte[0x800];
    public bool Valid;

    /// <summary>before RunFrame, with the camera</summary>
    public void Snapshot(Machine m)
    {
        for (int a = 0; a < 0x800; a++) Ram[a] = (byte)m.Rd(a);
        Valid = true;
    }

    /// <summary>draws into spr / tag (W wide, 240 lines; only where empty and outside columns e..e+255, plus the masked
    /// left column 0-7 of the game when fillLeft): banks = the 8 CHR byte offsets for PPU $0000-$1FFF, ctrl = PPUCTRL</summary>
    public void Draw(Data data, byte[] chr, int[] banks, int ctrl, int w, int e, byte[] spr, int[] tag, bool[] lines, bool includeCenter = false)
    {
        Array.Clear(spr, 0, w * 240);
        Array.Clear(tag, 0, w * 240);
        if (!Valid) return;
        byte[] rom = data.BinFile("spritesB28");
        int Rom(int a) { a &= 0xFFFF; return a < 0x800 ? Ram[a] : a >= 0x8000 && a < 0xC000 ? rom[a - 0x8000] : 0; }
        int camX = Ram[2] | Ram[3] << 8;
        int h = (ctrl & 0x20) != 0 ? 16 : 8;
        for (int x = 0; x < 0x20; x++)
        {
            if (Ram[0x4A0 + x] == 0) continue;
            int objX = Ram[0x70 + x] | Ram[0x90 + x] << 8;
            int sx = objX - camX;                                       // screen x, may be outside 0..255
            if (sx < -e - 64 || sx > 255 + e + 64) continue;
            // vertical: ObjSpriteDraw 804D-8060
            int ylo = Ram[0xB0 + x], cyl = Ram[4];
            if (((Ram[0xD0 + x] - Ram[5] - (ylo < cyl ? 1 : 0)) & 0xFF) != 0) continue;
            int d = ylo - cyl, sy = d & 0xFF;
            if (d < 0) sy = (sy - 0x10) & 0xFF;
            if (sy >= 0xE8 || sy < 0x10) continue;
            int fl = Ram[0x380 + x] & 0x7F;
            if ((fl << 1 & 0x80) != 0) sy += 3;                         // sinking: DrawMeta 8230
            if ((fl & 2) != 0) Meta(x, Ram[0x580 + x], (fl & 0xDF) | 1, sx, sy);   // overlay first
            Meta(x, Ram[0x300 + x], fl, sx, sy);
        }

        void Meta(int x, int id, int fl, int sx, int sy)
        {
            if ((fl & 1) == 0) id = (id + Ram[0x360 + x]) & 0xFF;
            int tab = (id & 0x80) != 0 ? 0x855C : 0x845C, yi = (id << 1) & 0xFF;
            int ptr = Rom(tab + yi) | Rom(tab + yi + 1) << 8;
            bool mirror = false;
            int n;
            for (int guard = 0; ; guard++)
            {
                n = Rom(ptr);
                if (n == 0 || guard > 4) return;
                if (n < 0x80) break;
                ptr = Rom(ptr + 1) | Rom(ptr + 2) << 8;
                mirror = true;
            }
            int f = (Ram[0x4E0 + x] >> 3) & Rom(ptr + 1);
            int a = 0, t = n;
            for (int b = 0; b < 4; b++) { if ((f >> b & 1) != 0) a = (a + t) & 0xFF; if (b < 3) t = (t << 1) & 0xFF; }
            ptr = (ptr + a * 4) & 0xFFFF;
            int t18 = (fl << 1) & 0xFF, pri = t18 & 0x20;
            int tileAdd = (Ram[0x320 + x] + ((fl & 0x20) << 1)) & 0xFF;
            int y = 2, left = n;
            for (int guard = 0; guard < 64; guard++)
            {
                int dx = Rom(ptr + y);
                if (dx == 0x80) break;
                int ex = mirror ? (((dx ^ 0xFF) - 8) & 0xFF) : dx;
                int px = sx + (sbyte)ex;                                // the entry's screen x (game columns)
                int oy = (Rom(ptr + y + 1) + sy) & 0xFF;
                int attr = Rom(ptr + y + 2) | pri;
                if (mirror) attr ^= 0x40;
                if ((attr & 4) != 0 && (t18 & 0x80) != 0) break;        // the sunk lower part
                int tile = (Rom(ptr + y + 3) + tileAdd) & 0xFF;
                // the game emits it when it does not wrap (0..255); we add what lies outside its columns
                Entry(px, oy, tile, attr, px < 0 || px > 248);
                y += 4;
                left = (left - 1) & 0xFF;
                if (left == 0) break;
            }
        }

        void Entry(int px, int oy, int tile, int attr, bool any)
        {
            if (!any && !includeCenter) return;
            for (int row = 0; row < h; row++)
            {
                int yy = oy + 1 + row;
                if (yy >= 240) break;
                if (!lines[yy]) continue;
                int r = (attr & 0x80) != 0 ? h - 1 - row : row;
                int pa = h == 16 ? ((tile & 1) << 12) + ((tile & 0xFE) + (r >> 3)) * 16 + (r & 7) : ((ctrl & 8) != 0 ? 0x1000 : 0) + tile * 16 + r;
                int a0 = banks[pa >> 10] + (pa & 0x3FF), a1 = banks[(pa + 8) >> 10] + ((pa + 8) & 0x3FF);
                if (a1 >= chr.Length) continue;
                int lo = chr[a0], hi = chr[a1];
                bool fh = (attr & 0x40) != 0;
                for (int k = 0; k < 8; k++)
                {
                    int sxk = px + k, wx = sxk + e;
                    if (wx < 0 || wx >= w || (!includeCenter && sxk >= 0 && sxk <= 255)) continue;   // the game's own columns: its pixels
                    int b = fh ? 7 - k : k;
                    int c = (lo >> (7 - b) & 1) | (hi >> (7 - b) & 1) << 1;
                    int i = yy * w + wx;
                    if (c == 0 || spr[i] != 0) continue;
                    spr[i] = (byte)(((attr & 3) << 2) | ((attr & 0x20) != 0 ? 0x40 : 0) | c);
                    tag[i] = Ppu.TagValid | ((attr & 0x80) != 0 ? 1 << 23 : 0) | (fh ? 1 << 22 : 0) | (4 + (attr & 3)) << 19 |
                             (pa & 7) << 16 | b << 13 | (a0 >> 4);
                }
            }
        }
    }
}
