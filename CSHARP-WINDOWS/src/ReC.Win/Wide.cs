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

namespace ReC.Win;

internal sealed class AreaCache
{
    public int W, H;
    public uint[] Px = Array.Empty<uint>();
    /// <summary>what the cache was built from (area id, grid, $67, CHR banks, visited bitmap): rebuilt when it changes</summary>
    public string Key = "";

    /// <summary>the RAM a build reads, copied on the game thread</summary>
    public sealed class Snapshot
    {
        public byte[] Ram = new byte[0x800], Wram = new byte[0x400];
        public string Key = "";
        public int Rd(int a) => a < 0x800 ? Ram[a] : a is >= 0x6000 and < 0x6400 ? Wram[a - 0x6000] : 0;
    }

    public static Snapshot Take(Machine m)
    {
        var s = new Snapshot();
        for (int a = 0; a < 0x800; a++) s.Ram[a] = (byte)m.Rd(a);
        for (int a = 0; a < 0x400; a++) s.Wram[a] = (byte)m.Rd(0x6000 + a);
        var k = new System.Text.StringBuilder();
        k.Append(m.Rd(0x6C)).Append(',').Append(m.Rd(0x62FC)).Append(',').Append(m.Rd(0x62FD)).Append(',').Append(m.Rd(0x67))
         .Append(',').Append(m.Rd(0x07F0)).Append(',').Append(m.Rd(0x07F1)).Append(',').Append(m.Rd(0x62FF)).Append(',')
         .Append(m.Rd(0x6A)).Append(',').Append(m.Rd(0x6B));
        for (int a = 0x6300; a < 0x6380; a++) k.Append(',').Append(m.Rd(a));
        for (int a = 0x62F0; a < 0x62F8; a++) k.Append(',').Append(m.Rd(a));
        s.Key = k.ToString();
        return s;
    }

    /// <summary>= Bg.RenderArea(m, Visit.Ram), packed, from a snapshot (thread-safe: data files are read-only)</summary>
    public static AreaCache Build(Data data, Snapshot s)
    {
        int gw = s.Rd(0x62FC) + 1, gh = s.Rd(0x62FD) + 1;
        int w = gw * 256, h = gh * 240;
        var px = new uint[w * h];
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
        return new AreaCache { W = w, H = h, Px = px, Key = s.Key };
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
    private System.Threading.Tasks.Task<AreaCache>? _building;
    private string _wantKey = "";
    public int CamX, CamY;
    /// <summary>0..1: how much of the sides shows (eases with the match test)</summary>
    public float Show;
    public bool Match { get; private set; }

    /// <summary>on the game thread, before RunFrame: the camera, and a cache (re)build when the area changed</summary>
    public void BeforeFrame(Machine m, Data data, bool async = true)
    {
        CamX = m.Rd(2) | m.Rd(3) << 8;
        CamY = (sbyte)m.Rd(5) * 240 + m.Rd(4);
        if (_building is { IsCompleted: true } b) { Cache = b.Result; _building = null; }
        if (m.Rd(0x40) != 1 || m.Rd(0x62FC) > 7 || m.Rd(0x62FD) > 15) return;      // gameplay only; grid sanity
        var snap = AreaCache.Take(m);
        if (snap.Key == _wantKey) return;
        _wantKey = snap.Key;
        if (async) _building = System.Threading.Tasks.Task.Run(() => AreaCache.Build(data, snap));
        else Cache = AreaCache.Build(data, snap);
    }

    /// <summary>after the frame: share of sampled playfield pixels (lines 8..hudTop-1, every 7th) equal to the area render
    /// at the camera; the sides show (fading over ~8 frames) while it is at least 85 %</summary>
    public void AfterFrame(byte[] fb, byte[] pal, int hudTop)
    {
        var c = Cache;
        int same = 0, n = 0;
        if (c != null)
            for (int i = 8 * 256 + 3; i < hudTop * 256; i += 7)
            {
                int x = i & 255, y = i >> 8, ax = x + CamX, ay = y + CamY;
                if (x < 8 || ax < 0 || ay < 0 || ax >= c.W || ay >= c.H) continue;
                n++;
                if ((fb[i] & 63) == AreaCache.Color(c.Px[ay * c.W + ax], pal)) same++;
            }
        Match = n > 200 && same * 100 >= n * 85;
        Show = Math.Clamp(Show + (Match ? 0.125f : -0.25f), 0, 1);
    }

    /// <summary>the lines whose sides show the map: shown (fade above `min`), above the status bar, and not a line the game
    /// leaves black (columns 8-255 all -eF: the unloaded rows above the status bar)</summary>
    public void MapLines(byte[] fb, int hudTop, float min, bool[] dst)
    {
        for (int y = 0; y < 240; y++)
        {
            bool map = Show > min && y < hudTop;
            if (map) { map = false; for (int x = 8; x < 256 && !map; x++) map = (fb[y * 256 + x] & 0x3F) != 0x0F; }
            dst[y] = map;
        }
    }

    /// <summary>NES colour index of side pixel (x in wide coordinates, e = side width), or 0x0F (black) when outside the
    /// area / not shown</summary>
    public int SideColor(int wx, int y, int e, byte[] pal)
    {
        var c = Cache;
        if (c == null) return 0x0F;
        int ax = wx - e + CamX, ay = y + CamY;
        if (ax < 0 || ay < 0 || ax >= c.W || ay >= c.H) return 0x0F;
        return AreaCache.Color(c.Px[ay * c.W + ax], pal);
    }
}
