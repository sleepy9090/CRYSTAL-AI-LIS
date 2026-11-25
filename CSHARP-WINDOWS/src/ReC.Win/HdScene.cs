// re-c Windows host — HD scene analysis (T19, the inputs of Gpu.cs beside the Enhancer's albedo / height / lift).
// Not in the web port. Everything is derived from what is on screen this frame (Fb, Ppu.Tags, Ppu.LineScroll):
//   Info   256x240 RGBA8  r = sprite lift (Enhancer.HdLift), g = NES colour * 4, b = flags (Sprite / Void / Ui), a = void (bilinear mask)
//   Lines  240 x float4   x, y = world position (nametable space, 512 x 480, wraps) of the line's pixel 0; z = 1: not
//                         the playfield (HUD split, rendering off) -> drawn unlit, exactly as the albedo
//   scene  camera (unwrapped, from the playfield scroll: parallax layers), outdoor (share of foliage colours: clouds),
//          void gate (enough playfield on screen: the chasm parallax layers), mean colour (fog / sky tint), play (a HUD
//          split with text is on screen = gameplay: no pits, clouds or dust on the title, cut-scenes and menus)
// Void = large black areas of the background (NES blacks or a dark backdrop, opened with a 3x3 square so outlines and
// thin black details stay solid): pits and the darkness around caves. Colour-based like the rest, so a shape the game
// hides black-on-black stays hidden.
// Materials (Gpu material table) come from the NES colour alone: hue 1/2/C water & ice, 3-5 plaster & cloth,
// 6-8 earth / wood / sand / brick, 9-B foliage, greys stone, blacks none.
using ReC.Nes;

namespace ReC.Win;

internal sealed class HdScene
{
    public const byte FSprite = 1, FVoid = 2, FUi = 4;
    public readonly byte[] Info = new byte[256 * 240 * 4];
    public readonly float[] Lines = new float[240 * 4];
    public float CamX, CamY, Outdoor, VoidGate, Time;
    /// <summary>T22: share of icy colours (light water / ice blues, whites half) and of fire colours ($06 $16 $17 $27 $28,
    /// with at least a quarter of the playfield dark) in the playfield, smoothed: snow / ice crystals and rising embers</summary>
    public float Icy, Hot;
    /// <summary>T29: the HUD panel: its fill colour (the most common colour of the HUD lines, -1 = no HUD) and its first /
    /// last line (the panel gets a gloss gradient there; text and frame pixels stay exact)</summary>
    public int HudFill = -1, HudTop, HudBottom;
    /// <summary>1 = gameplay (a HUD split with text is on screen): world effects on; 0 = title, cut-scenes, menus</summary>
    public float Play;
    public (float R, float G, float B) Mean = (0.3f, 0.3f, 0.3f);
    private int _ox = -1, _oy = -1, _frames;
    private readonly uint[] _rgb;
    private readonly byte[] _black = new byte[256 * 240], _ero = new byte[256 * 240];
    private static readonly int[] FontBanks = { 0x38, 0x39, 0x3C, 0x3D };   // 1 KB CHR banks of the font / HUD icons
    private static readonly bool[] FontBank = Enumerable.Range(0, 128).Select(b => FontBanks.Contains(b)).ToArray();
    private readonly byte[] _tmp = new byte[256 * 240], _dil = new byte[256 * 240];

    /// <summary>3x3 box min (erode, and = true) or max (dilate) of a 0 / 1 image, edges clamped: rows, then columns</summary>
    private void Box3(byte[] src, byte[] dst, bool and)
    {
        var t = _tmp;
        for (int y = 0; y < 240; y++)
        {
            int r = y * 256;
            for (int x = 0; x < 256; x++)
            {
                int a = src[r + (x > 0 ? x - 1 : x)], b = src[r + x], c = src[r + (x < 255 ? x + 1 : x)];
                t[r + x] = (byte)(and ? a & b & c : a | b | c);
            }
        }
        for (int y = 0; y < 240; y++)
        {
            int r = y * 256, u = (y > 0 ? y - 1 : y) * 256, d = (y < 239 ? y + 1 : y) * 256;
            for (int x = 0; x < 256; x++)
                dst[r + x] = (byte)(and ? t[u + x] & t[r + x] & t[d + x] : t[u + x] | t[r + x] | t[d + x]);
        }
    }

    public HdScene(int[] nesRgb) { _rgb = nesRgb.Select(c => (uint)c & 0xFFFFFF).ToArray(); }

    /// <summary>material kind of an NES colour: 0 none, 1 stone, 2 water / ice, 3 plaster / cloth, 4 earth / wood, 5 foliage</summary>
    public static int Kind(int c)
    {
        c &= 0x3F;
        int hue = c & 15;
        if (hue >= 0xD) return 0;
        if (hue == 0) return c >= 0x20 ? 3 : 1;
        if (hue is 1 or 2 or 0xC) return 2;
        if (hue is >= 3 and <= 5) return 3;
        if (hue is >= 6 and <= 8) return 4;
        return 5;
    }

    /// <summary>64 x RGBA8 material table: r = kind * 40, g = specular, b = gloss, a = detail strength</summary>
    public static byte[] MaterialTable()
    {
        var t = new byte[64 * 4];
        (float S, float G, float D)[] k =
        {
            (0, 0, 0), (0.30f, 0.35f, 0.85f), (0.95f, 0.92f, 0.35f), (0.10f, 0.20f, 0.40f), (0.18f, 0.28f, 0.75f), (0.14f, 0.22f, 1.00f),
        };
        for (int c = 0; c < 64; c++)
        {
            int kind = Kind(c);
            t[c * 4] = (byte)(kind * 40);
            t[c * 4 + 1] = (byte)(k[kind].S * 255);
            t[c * 4 + 2] = (byte)(k[kind].G * 255);
            t[c * 4 + 3] = (byte)(k[kind].D * 255);
        }
        return t;
    }

    private static float Luma(uint c) => ((c >> 16 & 255) * 0.299f + (c >> 8 & 255) * 0.587f + (c & 255) * 0.114f) / 255f;

    private static float Smooth(float a, float b, float x)
    {
        float t = Math.Clamp((x - a) / (b - a), 0, 1);
        return t * t * (3 - 2 * t);
    }

    /// <summary>this frame's Info / Lines / scene values; lineScroll null = no scroll known (camera stays)</summary>
    public void Build(byte[] fb, int[] tags, byte[] pal, byte[] lift, int[]? lineScroll, int frameNo)
    {
        Time = frameNo / 60f;
        // per line: world position, playfield offset
        var offs = new int[240];
        var votes = new Dictionary<int, int>();
        for (int y = 0; y < 240; y++)
        {
            int ls = lineScroll?[y] ?? 0;
            if (ls < 0) { offs[y] = -1; continue; }
            int v = ls & 0x7FFF, fx = ls >> 16 & 7;
            int wx = ((v & 0x400) != 0 ? 256 : 0) + (v & 31) * 8 + fx;
            int wy = ((v & 0x800) != 0 ? 240 : 0) + ((v >> 5) & 31) * 8 + ((v >> 12) & 7);
            Lines[y * 4] = wx; Lines[y * 4 + 1] = wy;
            int o = wx | (((wy - y) % 480 + 480) % 480) << 10;
            offs[y] = o;
            votes[o] = votes.GetValueOrDefault(o) + 1;
        }
        int play = votes.Count > 0 ? votes.MaxBy(kv => kv.Value).Key : -1;
        // not the playfield: rendering off, or lines with another scroll that have text within 8 lines (the HUD split);
        // other scroll runs (raster effects, wraps) are lit like the playfield, each at its own world position
        var fontPx = new int[240];
        for (int i = 0; i < 256 * 240; i++)
        {
            int tag = tags[i];
            if ((tag & Ppu.TagValid) != 0 && FontBank[(tag & 0x1FFF) >> 6]) fontPx[i >> 8]++;
        }
        for (int y = 0; y < 240; y++)
        {
            bool ui = offs[y] < 0;
            if (!ui && offs[y] != play)                          // text within 8 lines in the same scroll run
                for (int d = -8; d <= 8 && !ui; d++)
                {
                    int q = y + d;
                    if (q < 0 || q >= 240 || fontPx[q] < 4) continue;
                    int lo = Math.Min(q, y), hi = Math.Max(q, y);
                    bool same = true;
                    for (int r = lo; r <= hi && same; r++) same = offs[r] == offs[y];
                    ui = same;
                }
            Lines[y * 4 + 2] = ui ? 1 : 0;
        }
        bool hud = false;
        for (int y = 0; y < 240 && !hud; y++) hud = offs[y] >= 0 && Lines[y * 4 + 2] > 0;
        HudFill = -1;
        HudTop = 0; HudBottom = -1;
        // T41: also without status-bar lines (story text, menus): the panel colour from the font tiles
        {
            var hist = new int[64];
            int top = 240, bot = -1;
            for (int y = 0; y < 240; y++)
            {
                if (offs[y] < 0 || Lines[y * 4 + 2] == 0) continue;
                top = Math.Min(top, y); bot = Math.Max(bot, y);
            }
            // T39: the panel colour from the HUD lines and the font tiles anywhere (menu windows are drawn with the
            // playfield's scroll), leaving out one-colour rows (the backdrop strips around a panel)
            for (int y = 0; y < 240; y++)
            {
                if (offs[y] < 0) continue;
                int c0 = fb[y * 256] & 0x3F;
                bool flat = true;
                for (int x = 1; x < 256 && flat; x++) flat = (fb[y * 256 + x] & 0x3F) == c0;
                if (flat) continue;
                bool uiLine = Lines[y * 4 + 2] > 0;
                for (int x = 0; x < 256; x++)
                {
                    int i = y * 256 + x, tag = tags[i];
                    bool font = (tag & Ppu.TagValid) != 0 && (tag >> 19 & 7) < 4 && FontBank[(tag & 0x1FFF) >> 6];
                    if (uiLine || font) hist[fb[i] & 0x3F]++;
                }
            }
            int best = Array.IndexOf(hist, hist.Max());
            // T41: a black panel is a panel too (story text, cut scenes): the shader styles only its text, no gloss
            if (hist[best] >= 256) { HudFill = best == 0x0D ? 0x0F : best; if (hud) { HudTop = top; HudBottom = bot; } }
        }

        // camera from the playfield offset (wrap-aware); a jump of more than 48 pixels is a cut (no motion)
        bool cut = false;
        if (play >= 0)
        {
            int ox = play & 1023, oy = play >> 10;
            if (_ox >= 0)
            {
                int dx = ((ox - _ox) % 512 + 768) % 512 - 256, dy = ((oy - _oy) % 480 + 720) % 480 - 240;
                if (Math.Abs(dx) <= 48 && Math.Abs(dy) <= 48) { CamX += dx; CamY += dy; } else cut = true;
            }
            _ox = ox; _oy = oy;
        }

        // backdrop colour: the most common colour of pixels with no tile / slot 0
        int bd = pal[0] & 0x3F;
        var info = Info;
        int nPlay = 0, nSolid = 0, nLeaf = 0, nIce = 0, nHot = 0;
        double mr = 0, mg = 0, mb = 0;
        bool darkBack = Luma(_rgb[bd]) < 0.25f;
        // void: background blacks, opened (erode + dilate 3x3)
        for (int i = 0; i < 256 * 240; i++)
        {
            int tag = tags[i], c = fb[i] & 0x3F;
            bool spr = (tag & Ppu.TagValid) != 0 && (tag >> 19 & 7) >= 4;
            _black[i] = (byte)(!spr && Lines[(i >> 8) * 4 + 2] == 0 && ((c & 15) >= 0xD && c != 0x2D && c != 0x3D || c == bd && darkBack) ? 1 : 0);
        }
        Box3(_black, _ero, true);                                // erode, then dilate: the opening
        Box3(_ero, _dil, false);
        for (int y = 0; y < 240; y++)
        {
            bool ui = Lines[y * 4 + 2] > 0;
            for (int x = 0; x < 256; x++)
            {
                int i = y * 256 + x, tag = tags[i], c = fb[i] & 0x3F;
                bool valid = (tag & Ppu.TagValid) != 0;
                bool spr = valid && (tag >> 19 & 7) >= 4;
                bool font = valid && FontBank[(tag & 0x1FFF) >> 6];
                bool isVoid = _black[i] != 0 && _dil[i] != 0;
                byte f = (byte)((spr ? FSprite : 0) | (isVoid ? FVoid : 0) | (ui || font ? FUi : 0));
                int o = i * 4;
                info[o] = lift[i]; info[o + 1] = (byte)(c * 4); info[o + 2] = f; info[o + 3] = (byte)(isVoid ? 255 : 0);
                if (ui) continue;
                nPlay++;
                if (spr || isVoid || font) continue;
                nSolid++;
                if (Kind(c) == 5 && c < 0x30) nLeaf++;            // grass / trees (not pastel greens)
                if (Kind(c) == 2 && c >= 0x20) nIce += 2; else if (c is 0x20 or 0x30) nIce++;   // light blues, half for whites
                if (c is 0x06 or 0x16 or 0x17 or 0x27 or 0x28) nHot++;
                uint rgb = _rgb[c];
                mr += rgb >> 16 & 255; mg += rgb >> 8 & 255; mb += rgb & 255;
            }
        }
        float outdoor = nSolid > 0 ? Smooth(0.05f, 0.25f, nLeaf / (float)nSolid) : 0;
        float gate = hud && nPlay > 0 ? Smooth(0.10f, 0.25f, nSolid / (float)nPlay) : 0;
        var mean = nSolid > 0 ? ((float)(mr / nSolid / 255), (float)(mg / nSolid / 255), (float)(mb / nSolid / 255)) : (0f, 0f, 0f);
        float k = cut || _frames == 0 ? 1 : 0.12f;                   // settle over a few frames, jump on a cut
        Outdoor += (outdoor * (hud ? 1 : 0) - Outdoor) * k;
        float icy = nSolid > 0 ? Smooth(0.25f, 0.45f, nIce / 2f / nSolid) : 0, hot = nSolid > 0 ? Smooth(0.35f, 0.55f, nHot / (float)nSolid) * Smooth(0.25f, 0.45f, (nPlay - nSolid) / (float)nPlay) : 0;   // + a dark, cavernous screen
        Icy += (icy * (hud ? 1 : 0) - Icy) * k;
        Hot += (hot * (hud ? 1 : 0) - Hot) * k;
        Play += ((hud ? 1 : 0) - Play) * k;
        VoidGate += (gate - VoidGate) * k;
        Mean = (Mean.R + (mean.Item1 - Mean.R) * k, Mean.G + (mean.Item2 - Mean.G) * k, Mean.B + (mean.Item3 - Mean.B) * k);
        _frames++;
    }
}
