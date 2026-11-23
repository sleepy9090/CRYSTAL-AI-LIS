// re-c C# port — background pipeline: screen pages -> metatiles -> tiles/attributes (= web/src/game/world/bg.js, s53).
// REVERSE.md "Screen / metatile pipeline" + "Background details"; data = web/data/screens*.bin, metatiles.bin.
//
//   ScreenPage(m, S, rowPath)    ScreenPagePtr 31:EF36 (+ the $62FF b7 bank-10 branch of ScreenRowRender 31:EBE5)
//   TileSubst(m, M, visited)     ScreenTileSubst 31:ED99 (visited screens: M < $20 -> $BE00 table, ptr $6A/$6B)
//   MetatileTiles / MetatileAttr MetatileExpand 31:EDBA / MetatileAttrBits 31:EECC
//   ScreenAt / ScreenVisited     ScreenAtCamera 31:EF1F / ScreenVisitedBit 31:EC6C
//   RenderArea(m, visited)       whole k0 grid as NES colour indices (static reference view, world_check.mjs)
namespace ReC.Game.World;

public static class Bg
{
    private static readonly int[] Bit = { 0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80 };   // BitMask 30:C000

    // js: game/world/bg.js screenPage
    /// <summary>(buf, off): the 256-byte page of screen id S (240 metatile ids + 16-byte tail)</summary>
    public static (byte[] Buf, int Off) ScreenPage(Machine m, int s, bool rowPath = false)
    {
        int lo = s & 0x3F;
        if (rowPath && (m.Rd(0x62FF) & 0x80) != 0 && lo < 0x20) return (m.Data.BinFile("screensB10"), lo << 8);
        return (m.Data.BinFile("screens"), s << 8);       // bank 2*(S>>6) + (lo>>5), page lo&$1F
    }

    // js: game/world/bg.js mtByte
    public static int MtByte(Machine m, int addr) => m.Data.BinFile("metatiles")[addr - 0x8000];   // banks 8/9 as $8000-$BFFF

    // js: game/world/bg.js tileSubst
    public static int TileSubst(Machine m, int mt, bool visited)
    {
        if (!visited || mt >= 0x20) return mt;
        return MtByte(m, ((m.Rd(0x6B) << 8) | m.Rd(0x6A)) + mt);
    }

    // js: game/world/bg.js metatileTiles
    public static int[] MetatileTiles(Machine m, int mt)
    {
        int p = m.Rd(0x67) & 0xFC;
        var t = new int[4];
        for (int q = 0; q < 4; q++) t[q] = MtByte(m, ((p | q) << 8) | mt);
        return t;
    }

    // js: game/world/bg.js metatileAttr
    public static int MetatileAttr(Machine m, int mt)
    {
        int b = 0xB000 + ((m.Rd(0x67) & 0x3C) >> 2) * 0x40;
        return (MtByte(m, b + (mt >> 2)) >> (2 * (mt & 3))) & 3;
    }

    // js: game/world/bg.js screenAt
    public static int ScreenAt(Machine m, int xhi, int yhi) => m.Rd(0x6300 + (((yhi << 3) & 0x78) | (xhi & 7)));

    // js: game/world/bg.js screenVisited
    public static bool ScreenVisited(Machine m, int xhi, int yhi) => (m.Rd(0x62F0 + yhi) & Bit[xhi & 7]) != 0;

    // js: game/world/bg.js bgTilePixel
    /// <summary>BG tile pixel: 2bpp from CHR with R0/R1 (2KB each) at PPU $0000/$0800, as ppu.js chrBanks() maps them</summary>
    private static int BgTilePixel(byte[] chr, int r0, int r1, int t, int x, int y)
    {
        int bank = t < 0x80 ? (r0 & 0xFE) + (t >> 6) : (r1 & 0xFE) + ((t >> 6) & 1);
        int o = (bank % (chr.Length >> 10)) * 0x400 + (t & 0x3F) * 16 + y;
        return ((chr[o] >> (7 - x)) & 1) | (((chr[o + 8] >> (7 - x)) & 1) << 1);
    }

    public enum Visit { None, All, Ram }

    // js: game/world/bg.js renderArea
    /// <summary>Static render of the loaded area (k0 grid in $6300, k1 in $67/$07F0/$07F1, pal_buf built): every grid
    /// cell drawn as a full screen. visited: None = no substitution, All = every screen visited, Ram = the $62F0 bitmap.</summary>
    public static (int W, int H, byte[] Fb) RenderArea(Machine m, Visit visited = Visit.None)
    {
        int gw = m.Rd(0x62FC) + 1, gh = m.Rd(0x62FD) + 1;
        int w = gw * 256, h = gh * 240;
        var fb = new byte[w * h];
        byte[] chr = m.Data.Chr;
        int r0 = m.Rd(0x07F0), r1 = m.Rd(0x07F1);
        // js: game/world/bg.js pal
        int Pal(int g, int c) => m.Rd(0x6140 + (c != 0 ? g * 4 + c : 0));   // PaletteUpload: entry 0 = backdrop
        for (int gy = 0; gy < gh; gy++)
            for (int gx = 0; gx < gw; gx++)
            {
                int s = ScreenAt(m, gx, gy);
                bool vis = visited == Visit.Ram ? ScreenVisited(m, gx, gy) : visited == Visit.All;
                var (buf, off) = ScreenPage(m, s, true);
                for (int r = 0; r < 15; r++)
                    for (int c = 0; c < 16; c++)
                    {
                        int mt = TileSubst(m, buf[off + r * 16 + c], vis);
                        int g = MetatileAttr(m, mt);
                        var tiles = MetatileTiles(m, mt);
                        for (int q = 0; q < 4; q++)
                        {
                            int x0 = gx * 256 + c * 16 + (q & 1) * 8, y0 = gy * 240 + r * 16 + (q >> 1) * 8;
                            for (int y = 0; y < 8; y++)
                                for (int x = 0; x < 8; x++)
                                    fb[(y0 + y) * w + x0 + x] = (byte)(Pal(g, BgTilePixel(chr, r0, r1, tiles[q], x, y)) & 0x3F);
                        }
                    }
            }
        return (w, h, fb);
    }
}
