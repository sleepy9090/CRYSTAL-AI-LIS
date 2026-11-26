// re-c Windows host — HD modern font (T59, HD > Modern font). Not in the web port.
//
// The game prints text as raw ASCII tile numbers: with ShowMessage's CHR banks ($3C / $3D in the 1 KB numbering of
// the tags, = tiles $F00-$F7F globally) BG tile $F00 + c is the glyph of ASCII c for $21-$7E ($40 is the page-prompt
// arrow, not '@'; REVERSE.md "Font"). In HD such pixels are drawn from a glyph atlas rendered with a Windows font
// (GlyphAtlas, GDI, 64 x 64 per character, sized and placed on the game font's own cap height and baseline) in the
// tile's ink / paper colours (FontInk: per tile, from the CHR data, the two pixel values it uses).
namespace ReC.Win;

internal static class FontInk
{
    public const int First = 0xF00;                 // global tile of ASCII 0
    public const int Cell = 64;                      // atlas cell (= 8 NES pixels at the 8x albedo)
    /// <summary>the punctuation whose game glyph is the ASCII symbol; `_` is the game's ellipsis (drawn as '…'). The other
    /// symbol codes hold custom pieces (arrows, icons, word fragments) and keep their pixel glyphs</summary>
    public const string Standard = "!\"&',-.:;?_";

    /// <summary>the text drawn in the atlas for a code</summary>
    public static string Text(int code) => code == '_' ? "\u2026" : ((char)code).ToString();

    /// <summary>ASCII code of a global tile for a letter, digit or standard punctuation of the text font, else -1</summary>
    public static int Code(int tile)
    {
        int c = tile - First;
        return c is >= 0x21 and <= 0x7E && (char.IsAsciiLetterOrDigit((char)c) || Standard.Contains((char)c)) ? c : -1;
    }

    /// <summary>pixel value 0..3 of (col, row) in an 8x8 2bpp tile</summary>
    public static int Px(byte[] chr, int tile, int col, int row)
    {
        int a = tile * 16 + row;
        if (a + 8 >= chr.Length) return 0;
        return (chr[a] >> (7 - col) & 1) | (chr[a + 8] >> (7 - col) & 1) << 1;
    }

    /// <summary>(ink value, paper value) of a font tile: the most and second most frequent pixel values (paper = the
    /// most frequent); (-1, -1) if the tile is not a glyph or has a single value</summary>
    public static (int Ink, int Paper) Values(byte[] chr, int tile)
    {
        if (Code(tile) < 0) return (-1, -1);
        var n = new int[4];
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++) n[Px(chr, tile, c, r)]++;
        int paper = Array.IndexOf(n, n.Max());
        int ink = -1;
        for (int v = 0; v < 4; v++) if (v != paper && n[v] > 0 && (ink < 0 || n[v] > n[ink])) ink = v;
        return (ink, ink < 0 ? -1 : paper);
    }

    /// <summary>first and last ink row of a glyph tile (cap height / baseline of the game font from 'H')</summary>
    public static (int Top, int Bottom) InkRows(byte[] chr, int tile)
    {
        var (ink, _) = Values(chr, tile);
        int top = -1, bottom = -1;
        for (int r = 0; r < 8; r++)
            for (int c = 0; c < 8; c++)
                if (Px(chr, tile, c, r) == ink) { if (top < 0) top = r; bottom = r; }
        return (top, bottom);
    }
}

internal static unsafe class GlyphAtlas
{
    public const int W = 16 * FontInk.Cell, H = 8 * FontInk.Cell;

    /// <summary>coverage atlas (0x00RRGGBB grey, 16 x 8 cells, cell = ASCII code) or null without GDI; the font's cap
    /// height is scaled to (capBottom - capTop + 1) NES rows and its cap top placed at capTop</summary>
    public static uint[]? Render(int capTop, int capBottom, string face = "Segoe UI")
    {
        if (!OperatingSystem.IsWindows()) return null;
        const int C = FontInk.Cell, SW = C * 3;
        IntPtr sdc = Win32.GetDC(IntPtr.Zero);
        IntPtr dc = Win32.CreateCompatibleDC(sdc), bmp = Win32.CreateCompatibleBitmap(sdc, SW, C * 2);
        Win32.ReleaseDC(IntPtr.Zero, sdc);
        if (dc == IntPtr.Zero || bmp == IntPtr.Zero) return null;
        IntPtr oldBmp = Win32.SelectObject(dc, bmp);
        var buf = new uint[SW * C * 2];
        var atlas = new uint[W * H];
        try
        {
            Win32.SetBkMode(dc, 1 /* TRANSPARENT */);
            Win32.SetTextColor(dc, 0xFFFFFF);
            // draw one character at (x, y) in the scratch bitmap and read it back (top-down 32-bit)
            uint[] Draw(IntPtr font, string s, int x, int y)
            {
                Win32.FillRect(dc, new Win32.RECT { Right = SW, Bottom = C * 2 }, Win32.GetStockObject(Win32.BLACK_BRUSH));
                IntPtr of = Win32.SelectObject(dc, font);
                Win32.TextOutW(dc, x, y, s, s.Length);
                Win32.SelectObject(dc, of);
                var bi = new Win32.BITMAPINFOHEADER { Size = 40, Width = SW, Height = -C * 2, Planes = 1, BitCount = 32 };
                fixed (uint* p = buf) Win32.GetDIBits(dc, bmp, 0, (uint)(C * 2), p, ref bi, Win32.DIB_RGB_COLORS);
                return buf;
            }
            (int Top, int Bottom) Rows(uint[] b)
            {
                int top = -1, bottom = -1;
                for (int y = 0; y < C * 2; y++)
                    for (int x = 0; x < SW; x++)
                        if ((b[y * SW + x] & 0xFF) > 100) { if (top < 0) top = y; bottom = y; break; }
                return (top, bottom);
            }
            IntPtr Font(int px) => Win32.CreateFontW(-px, 0, 0, 0, 600, 0, 0, 0, 0, 0, 0, 4 /* ANTIALIASED */, 0, face);
            // measure 'H' at 48 px, then size the font so its cap height = the game font's
            IntPtr f0 = Font(48);
            var (t0, b0) = Rows(Draw(f0, "H", C, 0));
            Win32.DeleteObject(f0);
            if (t0 < 0) return null;
            int want = (capBottom - capTop + 1) * 8;
            int em = Math.Max(8, (int)Math.Round(48.0 * want / (b0 - t0 + 1)));
            IntPtr font = Font(em);
            var (t1, _) = Rows(Draw(font, "H", C, 0));
            int yOff = capTop * 8 - t1;
            for (int code = 0x21; code <= 0x7E; code++)
            {
                if (FontInk.Code(FontInk.First + code) < 0) continue;
                string s = FontInk.Text(code);
                IntPtr of = Win32.SelectObject(dc, font);
                Win32.GetTextExtentPoint32W(dc, s, 1, out var sz);
                Win32.SelectObject(dc, of);
                int w = Math.Max(1, sz.Cx);
                var b = Draw(font, s, C + (C - w) / 2, yOff + C / 2);
                // the glyph's ink columns, squeezed into the cell when wider
                int x0 = SW, x1 = -1;
                for (int y = 0; y < C * 2; y++)
                    for (int x = 0; x < SW; x++)
                        if ((b[y * SW + x] & 0xFF) > 8) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); }
                if (x1 < 0) continue;
                int gw = x1 - x0 + 1, pad = 3;
                double sx = gw > C - 2 * pad ? (double)gw / (C - 2 * pad) : 1;
                int ox = (int)Math.Round((C - gw / sx) / 2);
                int cx = code % 16 * C, cy = code / 16 * C;
                for (int y = 0; y < C; y++)
                    for (int x = 0; x < C; x++)
                    {
                        double fx = x0 + (x - ox + 0.5) * sx - 0.5;
                        int ix = (int)Math.Floor(fx);
                        double fr = fx - ix;
                        if (ix < x0 - 1 || ix > x1) continue;
                        int row = (y + C / 2) * SW;
                        double v0 = ix >= 0 && ix < SW ? b[row + ix] & 0xFF : 0, v1 = ix + 1 < SW && ix + 1 >= 0 ? b[row + ix + 1] & 0xFF : 0;
                        uint g = (uint)Math.Clamp(Math.Round(v0 + (v1 - v0) * fr), 0, 255);
                        atlas[(cy + y) * W + cx + x] = g << 16 | g << 8 | g;
                    }
            }
            Win32.DeleteObject(font);
            return atlas;
        }
        finally
        {
            Win32.SelectObject(dc, oldBmp);
            Win32.DeleteObject(bmp);
            Win32.DeleteDC(dc);
        }
    }
}
