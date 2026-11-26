// re-c Windows host — HD engine, stage 1 (T20): the scene layers of one frame, drawn from the game's video state
// (VideoCapture: nametables, attribute bytes, palette, OAM, CHR banks, scroll + mid-line writes per line), not from
// the PPU's pixels (Ppu.NoPixels is on while HD is). Not in the web port.
//
// Output, 256x240 each:
//   Bg / BgCol / BgTag    the background layer: pixel value (palette group << 2 | colour slot, 0 = transparent),
//                         its NES colour, its tile tag (Ppu.Tags format)
//   Sp / SpCol / SpTag    the sprite layer: the front sprite's pixel (0 = none; 0x40 = behind the background,
//                         0x80 = sprite 0, group << 2 | slot), its NES colour, its tag
//   Fb / Tags             the two layers composed exactly as the PPU composes them (= Ppu.Fb / Ppu.Tags with
//                         SpriteLimit on: `win_scene_raster` checks every frame of a long run against the PPU)
//   LineScroll            v | fine x << 16 at each line's start (-1 = rendering off), for HdScene
//   LinePal               the palette each line was drawn with (the game changes palettes mid-frame)
//   LeftClip              the line hides the background's left 8 pixels (PPUMASK bit 1 clear): they show the backdrop
// SpriteLimit false (HD default: Video > Graphics > HD: no sprite flicker) shows every sprite on a line instead of the
// first 8 by OAM order, so objects the game flickers to get past the hardware limit stay solid.
using ReC.Nes;

namespace ReC.Win;

internal sealed class SceneRaster
{
    /// <summary>columns (256; a widescreen copy made by Widen is wider)</summary>
    public readonly int W;
    public readonly byte[] Fb, Bg, BgCol, Sp, SpCol;
    public readonly int[] Tags, BgTag, SpTag;
    public readonly int[] LineScroll = new int[240];
    public readonly bool[] LeftClip = new bool[240];
    public readonly byte[][] LinePal = Enumerable.Range(0, 240).Select(_ => new byte[32]).ToArray();
    public bool SpriteLimit = true;
    public int FrameNo;

    private readonly byte[] _chr;
    private readonly int _nChr;
    private readonly short[] _tiles = new short[34 * 8];
    private readonly int[] _tileTag = new int[34 * 8];
    private readonly byte[] _spr = new byte[256];
    private readonly int[] _sprTag = new int[256];

    public SceneRaster(byte[] chr, int w = 256)
    {
        _chr = chr; _nChr = chr.Length >> 10; W = w;
        Fb = new byte[w * 240]; Bg = new byte[w * 240]; BgCol = new byte[w * 240]; Sp = new byte[w * 240]; SpCol = new byte[w * 240];
        Tags = new int[w * 240]; BgTag = new int[w * 240]; SpTag = new int[w * 240];
    }

    /// <summary>widescreen: the side sprites (SideSprites.Draw output) over the widened background, as the PPU composes:
    /// a sprite pixel behind an opaque background pixel stays hidden</summary>
    public void ApplySideSprites(byte[] spr, int[] tag)
    {
        for (int i = 0; i < W * 240; i++)
        {
            int sp = spr[i];
            if (sp == 0) continue;
            var pal = LinePal[i / W];
            Sp[i] = (byte)sp; SpCol[i] = (byte)(pal[0x10 | (sp & 0x0F)] & 0x3F); SpTag[i] = tag[i];
            bool front = (sp & 0x40) == 0 || Bg[i] == 0;
            if (front) { Fb[i] = SpCol[i]; Tags[i] = tag[i]; }
        }
    }

    /// <summary>widescreen (Wide.cs): this (W wide) = src (256) in the middle, the area at the camera on the sides of the
    /// lines that show the map (and in src's masked left column there): background only, tags with the area's tiles
    /// (pack art / materials work there too). mapLine[y] = the line shows the map (Window.Compose's rule).</summary>
    public void Widen(SceneRaster src, Widescreen wide, bool[] mapLine)
    {
        int e = (W - 256) / 2;
        var c = wide.Cache;
        for (int y = 0; y < 240; y++)
        {
            LineScroll[y] = src.LineScroll[y];
            Array.Copy(src.LinePal[y], LinePal[y], 32);
            bool map = mapLine[y] && c != null;
            LeftClip[y] = false;                          // the sides and the masked column are filled or black here
            int d = y * W, s = y * 256;
            Array.Copy(src.Fb, s, Fb, d + e, 256); Array.Copy(src.Bg, s, Bg, d + e, 256); Array.Copy(src.BgCol, s, BgCol, d + e, 256);
            Array.Copy(src.Sp, s, Sp, d + e, 256); Array.Copy(src.SpCol, s, SpCol, d + e, 256);
            Array.Copy(src.Tags, s, Tags, d + e, 256); Array.Copy(src.BgTag, s, BgTag, d + e, 256); Array.Copy(src.SpTag, s, SpTag, d + e, 256);
            var pal = src.LinePal[y];
            for (int x = 0; x < W; x++)
            {
                bool side = x < e || x >= e + 256;
                if (!side && !(src.LeftClip[y] && x < e + 8)) continue;
                int i = d + x;
                Sp[i] = 0; SpCol[i] = 0; SpTag[i] = 0;
                int ax = x - e + wide.CamX + wide.LineDx[y], ay = y + wide.CamY + wide.LineDy[y];
                if (!map || ax < 0 || ay < 0 || ax >= c!.W || ay >= c.H)
                {
                    Fb[i] = BgCol[i] = 0x0F; Bg[i] = 0; BgTag[i] = Tags[i] = 0;
                    continue;
                }
                uint p = c.Px[ay * c.W + ax];
                int v = (int)(p & 3), g = (int)(p >> 2 & 3), col = AreaCache.Color(p, pal);
                Fb[i] = BgCol[i] = (byte)col;
                Bg[i] = (byte)(v == 0 ? 0 : g << 2 | v);
                BgTag[i] = Tags[i] = Ppu.TagValid | g << 19 | (ay & 7) << 16 | (ax & 7) << 13 | (int)(p >> 4 & 0x1FFF);
            }
        }
    }

    private static int PalMirror(int a)
    {
        a &= 0x1F;
        return (a & 0x13) == 0x10 ? a & 0x0F : a;
    }

    private int NtIndex(int mirroring, int a)
    {
        a &= 0x0FFF;
        return mirroring == 0 ? a & 0x7FF : ((a >> 1) & 0x400) | (a & 0x3FF);
    }

    private static int IncX(int v) => (v & 0x1F) == 31 ? (v & ~0x1F) ^ 0x400 : v + 1;

    /// <summary>the 8 pixels of BG tile slot t from v with state r (= Ppu fetch)</summary>
    private void Fetch(VideoCapture cap, in VideoCapture.Regs r, int v, int t)
    {
        byte[] vram = cap.Vram[r.Vram], chr = _chr;
        int m = cap.Mirroring;
        int tile = vram[NtIndex(m, 0x2000 | (v & 0x0FFF))];
        int at = vram[NtIndex(m, 0x23C0 | (v & 0x0C00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07))];
        int p = ((at >> (((v >> 4) & 4) | (v & 2))) & 3) << 2;
        int pa = ((r.Ctrl & 0x10) != 0 ? 0x1000 : 0) + tile * 16 + ((v >> 12) & 7);
        int a0 = r.Banks[pa >> 10] + (pa & 0x3FF), pb = pa + 8, a1 = r.Banks[pb >> 10] + (pb & 0x3FF);
        int lo = chr[a0], hi = chr[a1];
        for (int i = 0; i < 8; i++)
        {
            int c = (lo >> (7 - i) & 1) | (hi >> (7 - i) & 1) << 1;
            _tiles[t * 8 + i] = (short)(c != 0 ? (p | c) : 0);
            _tileTag[t * 8 + i] = Ppu.TagValid | (p >> 2) << 19 | (pa & 7) << 16 | i << 13 | (a0 >> 4);
        }
    }

    /// <summary>sprite line y (= Ppu.EvalSprites for line y, done at the end of y - 1)</summary>
    private void Sprites(VideoCapture cap, int y)
    {
        Array.Clear(_spr);
        if (y <= 0 || !cap.SprValid[y]) return;
        var r = cap.Spr[y];
        var oam = cap.Oam[r.Oam];
        int ctrl = r.Ctrl, h = (ctrl & 0x20) != 0 ? 16 : 8, n = 0;
        for (int i = 0; i < 256; i += 4)
        {
            int row = y - 1 - oam[i];
            if (row < 0 || row >= h) continue;
            if (SpriteLimit && n == 8) break;
            n++;
            int tile = oam[i + 1], at = oam[i + 2], sx = oam[i + 3];
            if ((at & 0x80) != 0) row = h - 1 - row;
            int pa = h == 16
                ? ((tile & 1) << 12) + ((tile & 0xFE) + (row >> 3)) * 16 + (row & 7)
                : ((ctrl & 8) != 0 ? 0x1000 : 0) + tile * 16 + row;
            int a0 = r.Banks[pa >> 10] + (pa & 0x3FF), a1 = r.Banks[(pa + 8) >> 10] + ((pa + 8) & 0x3FF);
            int lo = _chr[a0], hi = _chr[a1];
            int tag = ((at & 3) << 2) | ((at & 0x20) != 0 ? 0x40 : 0) | (i == 0 ? 0x80 : 0);
            bool fh = (at & 0x40) != 0;
            for (int k = 0; k < 8; k++)
            {
                int xx = sx + k;
                if (xx > 255) break;
                int b = fh ? 7 - k : k;
                int c = (lo >> (7 - b) & 1) | (hi >> (7 - b) & 1) << 1;
                if (c == 0 || _spr[xx] != 0) continue;
                _spr[xx] = (byte)(tag | c);
                _sprTag[xx] = Ppu.TagValid | ((at & 0x80) != 0 ? 1 << 23 : 0) | (fh ? 1 << 22 : 0) |
                              (4 + (at & 3)) << 19 | (pa & 7) << 16 | b << 13 | (a0 >> 4);
            }
        }
    }

    /// <summary>this frame's layers from the captured video state</summary>
    public void Build(VideoCapture cap)
    {
        FrameNo = cap.FrameNo;
        for (int y = 0; y < 240; y++)
        {
            var r0 = cap.Line[y];
            var ev = cap.Events[y];
            LineScroll[y] = r0.Rendering ? (r0.V & 0x7FFF) | r0.X << 16 : -1;
            LeftClip[y] = r0.Rendering && (r0.Mask & 0x0A) == 0x08;
            Buffer.BlockCopy(cap.Pal[r0.Pal], 0, LinePal[y], 0, 32);
            Sprites(cap, y);
            // BG fetches in PPU order: tiles 0 / 1 at the line start, tile t >= 2 at dot 8 (t - 1); a mid-line write at
            // dot D is seen by fetches at dots >= D (its recorded v already counts the fetches before it)
            Array.Fill(_tiles, (short)-1);
            int v = r0.V, e = 0;
            var rs = r0;
            for (int t = 0; t < 33; t++)
            {
                if (t >= 2)
                    while (e < ev.Count && ev[e].Dot <= 8 * (t - 1)) { rs = ev[e].R; v = rs.V; e++; }
                if (!rs.Rendering) continue;
                Fetch(cap, rs, v, t);
                v = IncX(v);
            }
            // pixels: a write at dot D is seen by pixels x >= D - 1
            var rp = r0;
            int ep = 0, o = y * 256;
            for (int x = 0; x < 256; x++)
            {
                while (ep < ev.Count && ev[ep].Dot - 1 <= x) { rp = ev[ep].R; ep++; }
                byte[] pal = cap.Pal[rp.Pal];
                int mask = rp.Mask, grey = (mask & 1) != 0 ? 0x30 : 0x3F, i = o + x;
                if ((mask & 0x18) == 0)
                {
                    int vv = rp.V;
                    byte c = (byte)(((vv & 0x3F00) == 0x3F00 ? pal[PalMirror(vv)] : pal[0]) & grey);
                    Fb[i] = c; BgCol[i] = c; Tags[i] = 0; Bg[i] = 0; BgTag[i] = 0; Sp[i] = 0; SpCol[i] = 0; SpTag[i] = 0;
                    continue;
                }
                bool bgOn = (mask & 0x08) != 0;
                int bgFrom = bgOn ? ((mask & 2) != 0 ? 0 : 8) : 256;
                int spFrom = (mask & 4) != 0 ? 0 : 8;
                int fx = rp.X;
                int b = x >= bgFrom ? _tiles[x + fx] : 0, bp = b > 0 ? b : 0;
                int sp = (mask & 0x10) != 0 && x >= spFrom ? _spr[x] : 0;
                int bgTag = bgOn && x >= bgFrom && _tiles[x + fx] >= 0 ? _tileTag[x + fx] : 0;
                Bg[i] = (byte)bp; BgCol[i] = (byte)(pal[bp] & grey); BgTag[i] = bgTag;
                Sp[i] = (byte)sp; SpCol[i] = (byte)(sp != 0 ? pal[0x10 | (sp & 0x0F)] & grey : 0); SpTag[i] = sp != 0 ? _sprTag[x] : 0;
                bool front = sp != 0 && !(bp != 0 && (sp & 0x40) != 0);
                Fb[i] = front ? SpCol[i] : BgCol[i];
                Tags[i] = front ? _sprTag[x] : bgTag;
            }
        }
    }
}
