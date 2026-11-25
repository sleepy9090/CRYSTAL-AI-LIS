// re-c Windows host — on-screen display (T37): a 512x480 overlay over the picture (premultiplied RGBA, 0xAABBGGRR),
// drawn on the CPU with a built-in 5x7 font: the pause screen (dimmed game, PAUSED, keys, active cheats), short notices
// (a cheat or setting switched), the fast-forward mark. The HD engine composites it in its final pass (Gpu.Overlay),
// the other modes blend it into the picture before it is drawn (Blend). Not in the web port.
namespace ReC.Win;

internal static class Osd
{
    public const int W = 512, H = 480;
    public static readonly uint[] Pix = new uint[W * H];

    // 5x7 glyphs, rows top to bottom, '#' = on
    private static readonly Dictionary<char, string[]> Font = new()
    {
        [' '] = new[] { ".....", ".....", ".....", ".....", ".....", ".....", "....." },
        ['A'] = new[] { ".###.", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['B'] = new[] { "####.", "#...#", "#...#", "####.", "#...#", "#...#", "####." },
        ['C'] = new[] { ".###.", "#...#", "#....", "#....", "#....", "#...#", ".###." },
        ['D'] = new[] { "####.", "#...#", "#...#", "#...#", "#...#", "#...#", "####." },
        ['E'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#####" },
        ['F'] = new[] { "#####", "#....", "#....", "####.", "#....", "#....", "#...." },
        ['G'] = new[] { ".###.", "#...#", "#....", "#.###", "#...#", "#...#", ".####" },
        ['H'] = new[] { "#...#", "#...#", "#...#", "#####", "#...#", "#...#", "#...#" },
        ['I'] = new[] { ".###.", "..#..", "..#..", "..#..", "..#..", "..#..", ".###." },
        ['J'] = new[] { "..###", "...#.", "...#.", "...#.", "...#.", "#..#.", ".##.." },
        ['K'] = new[] { "#...#", "#..#.", "#.#..", "##...", "#.#..", "#..#.", "#...#" },
        ['L'] = new[] { "#....", "#....", "#....", "#....", "#....", "#....", "#####" },
        ['M'] = new[] { "#...#", "##.##", "#.#.#", "#.#.#", "#...#", "#...#", "#...#" },
        ['N'] = new[] { "#...#", "#...#", "##..#", "#.#.#", "#..##", "#...#", "#...#" },
        ['O'] = new[] { ".###.", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
        ['P'] = new[] { "####.", "#...#", "#...#", "####.", "#....", "#....", "#...." },
        ['Q'] = new[] { ".###.", "#...#", "#...#", "#...#", "#.#.#", "#..#.", ".##.#" },
        ['R'] = new[] { "####.", "#...#", "#...#", "####.", "#.#..", "#..#.", "#...#" },
        ['S'] = new[] { ".####", "#....", "#....", ".###.", "....#", "....#", "####." },
        ['T'] = new[] { "#####", "..#..", "..#..", "..#..", "..#..", "..#..", "..#.." },
        ['U'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", "#...#", ".###." },
        ['V'] = new[] { "#...#", "#...#", "#...#", "#...#", "#...#", ".#.#.", "..#.." },
        ['W'] = new[] { "#...#", "#...#", "#...#", "#.#.#", "#.#.#", "#.#.#", ".#.#." },
        ['X'] = new[] { "#...#", "#...#", ".#.#.", "..#..", ".#.#.", "#...#", "#...#" },
        ['Y'] = new[] { "#...#", "#...#", ".#.#.", "..#..", "..#..", "..#..", "..#.." },
        ['Z'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", "#....", "#####" },
        ['0'] = new[] { ".###.", "#...#", "#..##", "#.#.#", "##..#", "#...#", ".###." },
        ['1'] = new[] { "..#..", ".##..", "..#..", "..#..", "..#..", "..#..", ".###." },
        ['2'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".#...", "#####" },
        ['3'] = new[] { "####.", "....#", "....#", ".###.", "....#", "....#", "####." },
        ['4'] = new[] { "...#.", "..##.", ".#.#.", "#..#.", "#####", "...#.", "...#." },
        ['5'] = new[] { "#####", "#....", "####.", "....#", "....#", "#...#", ".###." },
        ['6'] = new[] { "..##.", ".#...", "#....", "####.", "#...#", "#...#", ".###." },
        ['7'] = new[] { "#####", "....#", "...#.", "..#..", ".#...", ".#...", ".#..." },
        ['8'] = new[] { ".###.", "#...#", "#...#", ".###.", "#...#", "#...#", ".###." },
        ['9'] = new[] { ".###.", "#...#", "#...#", ".####", "....#", "...#.", ".##.." },
        [':'] = new[] { ".....", "..#..", "..#..", ".....", "..#..", "..#..", "....." },
        ['.'] = new[] { ".....", ".....", ".....", ".....", ".....", ".##..", ".##.." },
        [','] = new[] { ".....", ".....", ".....", ".....", ".##..", "..#..", ".#..." },
        ['!'] = new[] { "..#..", "..#..", "..#..", "..#..", "..#..", ".....", "..#.." },
        ['?'] = new[] { ".###.", "#...#", "....#", "...#.", "..#..", ".....", "..#.." },
        ['-'] = new[] { ".....", ".....", ".....", ".###.", ".....", ".....", "....." },
        ['+'] = new[] { ".....", "..#..", "..#..", "#####", "..#..", "..#..", "....." },
        ['/'] = new[] { "....#", "....#", "...#.", "..#..", ".#...", "#....", "#...." },
        ['('] = new[] { "...#.", "..#..", ".#...", ".#...", ".#...", "..#..", "...#." },
        [')'] = new[] { ".#...", "..#..", "...#.", "...#.", "...#.", "..#..", ".#..." },
        ['>'] = new[] { ".#...", "..#..", "...#.", "....#", "...#.", "..#..", ".#..." },
        ['<'] = new[] { "...#.", "..#..", ".#...", "#....", ".#...", "..#..", "...#." },
        ['%'] = new[] { "##..#", "##..#", "...#.", "..#..", ".#...", "#..##", "#..##" },
        ['\''] = new[] { "..#..", "..#..", ".#...", ".....", ".....", ".....", "....." },
        ['='] = new[] { ".....", ".....", "#####", ".....", "#####", ".....", "....." },
        ['x'] = new[] { ".....", ".....", "#...#", ".#.#.", "..#..", ".#.#.", "#...#" },
    };

    public static int TextWidth(string s, int scale) => s.Length * 6 * scale - scale;

    public static void Clear() => Array.Clear(Pix);

    /// <summary>a box, colour (r, g, b) at opacity a (premultiplied, over what is there)</summary>
    public static void Box(int x0, int y0, int w, int h, float r, float g, float b, float a)
    {
        for (int y = Math.Max(0, y0); y < Math.Min(H, y0 + h); y++)
            for (int x = Math.Max(0, x0); x < Math.Min(W, x0 + w); x++)
            {
                // rounded corners (radius 6)
                int dx = Math.Max(Math.Max(x0 + 6 - x, x - (x0 + w - 7)), 0), dy = Math.Max(Math.Max(y0 + 6 - y, y - (y0 + h - 7)), 0);
                if (dx * dx + dy * dy > 36) continue;
                Over(x, y, r, g, b, a);
            }
    }

    private static void Over(int x, int y, float r, float g, float b, float a)
    {
        uint p = Pix[y * W + x];
        float k = 1 - a;
        float pr = (p & 255) / 255f, pg = (p >> 8 & 255) / 255f, pb = (p >> 16 & 255) / 255f, pa = (p >> 24) / 255f;
        pr = r * a + pr * k; pg = g * a + pg * k; pb = b * a + pb * k; pa = a + pa * k;
        Pix[y * W + x] = (uint)(pr * 255 + 0.5f) | (uint)(pg * 255 + 0.5f) << 8 | (uint)(pb * 255 + 0.5f) << 16 | (uint)(pa * 255 + 0.5f) << 24;
    }

    /// <summary>text at (x, y) (top left), each font pixel scale x scale, with a soft dark shadow</summary>
    public static void Text(int x, int y, string s, int scale, float r, float g, float b)
    {
        foreach (int pass in new[] { 0, 1 })
        {
            int cx = x;
            foreach (char c0 in s)
            {
                char c = Font.ContainsKey(c0) ? c0 : char.ToUpperInvariant(c0);
                if (Font.TryGetValue(c, out var rows))
                    for (int gy = 0; gy < 7; gy++)
                        for (int gx = 0; gx < 5; gx++)
                        {
                            if (rows[gy][gx] != '#') continue;
                            int px = cx + gx * scale, py = y + gy * scale;
                            for (int yy = 0; yy < scale; yy++)
                                for (int xx = 0; xx < scale; xx++)
                                {
                                    int X = px + xx + (pass == 0 ? Math.Max(1, scale / 2) : 0), Y = py + yy + (pass == 0 ? Math.Max(1, scale / 2) : 0);
                                    if (X < 0 || Y < 0 || X >= W || Y >= H) continue;
                                    if (pass == 0) Over(X, Y, 0, 0, 0, 0.7f);
                                    else
                                    {
                                        float t = (gy * scale + yy) / (7f * scale);            // a light top, a darker bottom
                                        float k = 1.12f - 0.3f * t;
                                        Over(X, Y, Math.Min(1, r * k), Math.Min(1, g * k), Math.Min(1, b * k), 1);
                                    }
                                }
                        }
                cx += 6 * scale;
            }
        }
    }

    public static void Centered(int y, string s, int scale, float r, float g, float b) =>
        Text((W - TextWidth(s, scale)) / 2, y, s, scale, r, g, b);

    /// <summary>the overlay for this moment; false = nothing to show (Pix cleared)</summary>
    /// <summary>T44: the F1 help panel's lines (key, what it does)</summary>
    public static readonly (string Key, string What)[] HelpLines =
    {
        ("ARROWS / WASD", "MOVE"), ("X / K", "A BUTTON"), ("Z / J", "SWORD, HOLD: CHARGE"),
        ("ENTER", "START"), ("TAB / R-SHIFT", "SELECT"), ("P", "PAUSE"), ("SPACE (HOLD)", "FAST FORWARD X4"),
        ("F1", "THIS HELP"), ("F2", "FPS COUNTER"), ("F9", "GRAPHICS MODE"), ("F11", "FULLSCREEN"), ("F12", "SCREENSHOT"), ("M", "MUTE"),
        ("1 - 6", "WINDOW SIZE"), ("RIGHT CLICK", "MENUS, CHEATS"), ("ESC", "QUIT (SAVES FIRST)"),
    };

    public static bool Build(bool paused, bool fast, string? notice, IReadOnlyList<string> cheats, bool help = false, string? stats = null)
    {
        Clear();
        bool any = false;
        if (help)
        {
            Box(0, 0, W, H, 0.01f, 0.02f, 0.06f, 0.7f);
            Box(32, 40, W - 64, H - 80, 0.05f, 0.08f, 0.20f, 0.93f);
            Box(36, 44, W - 72, 4, 0.55f, 0.7f, 1f, 0.6f);
            Centered(62, "KEYS", 4, 1f, 0.95f, 0.75f);
            for (int i = 0; i < HelpLines.Length; i++)
            {
                Text(56, 112 + i * 19, HelpLines[i].Key, 2, 1f, 0.85f, 0.55f);
                Text(56 + 15 * 12, 112 + i * 19, HelpLines[i].What, 2, 0.85f, 0.9f, 1f);
            }
            Centered(H - 58, "F1 OR P TO CLOSE", 1, 0.7f, 0.8f, 1f);
            return true;
        }
        if (paused)
        {
            Box(0, 0, W, H, 0.01f, 0.02f, 0.06f, 0.62f);
            Box(96, 120, 320, 220, 0.05f, 0.08f, 0.20f, 0.93f);
            Box(100, 124, 312, 4, 0.55f, 0.7f, 1f, 0.6f);
            Centered(150, "PAUSED", 6, 1f, 0.95f, 0.75f);
            Centered(222, "P  RESUME", 2, 0.85f, 0.9f, 1f);
            Centered(244, "SPACE  FAST FORWARD", 2, 0.85f, 0.9f, 1f);
            Centered(266, "RIGHT CLICK  MENUS", 2, 0.85f, 0.9f, 1f);
            Centered(288, "ESC  QUIT (SAVES FIRST)", 2, 0.85f, 0.9f, 1f);
            if (cheats.Count > 0) Centered(314, "CHEATS: " + string.Join(", ", cheats).ToUpperInvariant(), 1, 1f, 0.6f, 0.45f);
            any = true;
        }
        if (fast)
        {
            Box(W - 92, 10, 82, 30, 0.02f, 0.04f, 0.1f, 0.6f);
            Text(W - 84, 18, ">> X4", 2, 0.6f, 0.95f, 1f);
            any = true;
        }
        if (!string.IsNullOrEmpty(notice) && !paused)
        {
            int w = TextWidth(notice, 2) + 24;
            Box(10, 10, w, 30, 0.02f, 0.04f, 0.1f, 0.65f);
            Text(22, 18, notice, 2, 1f, 0.95f, 0.8f);
            any = true;
        }
        if (!string.IsNullOrEmpty(stats))                    // T49: FPS counter, under a notice
        {
            int y = !string.IsNullOrEmpty(notice) && !paused ? 46 : 10;
            Box(10, y, TextWidth(stats, 1) + 16, 18, 0.02f, 0.04f, 0.1f, 0.6f);
            Text(18, y + 6, stats, 1, 0.7f, 1f, 0.7f);
            any = true;
        }
        return any;
    }

    /// <summary>the overlay blended into a picture of w x h pixels (0x00RRGGBB), nearest sample</summary>
    public static void Blend(uint[] dst, int w, int h, int offset = 0)
    {
        for (int y = 0; y < h; y++)
        {
            int oy = y * H / h;
            for (int x = 0; x < w; x++)
            {
                uint o = Pix[oy * W + x * W / w];
                uint a = o >> 24;
                if (a == 0) continue;
                int i = offset + y * w + x;
                uint d = dst[i];
                uint k = 255 - a;
                uint r = Math.Min(255u, (o & 255) + ((d >> 16 & 255) * k + 127) / 255);
                uint g = Math.Min(255u, (o >> 8 & 255) + ((d >> 8 & 255) * k + 127) / 255);
                uint b = Math.Min(255u, (o >> 16 & 255) + ((d & 255) * k + 127) / 255);
                dst[i] = r << 16 | g << 8 | b;
            }
        }
    }
}
