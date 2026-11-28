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
    /// <summary>widescreen copy: side pixels with no map data (not a verified map line, or outside the area)</summary>
    public readonly bool[] NoMap;
    /// <summary>game-column pixels the widening replaced with map behind the HUD (Widen hudFrom)</summary>
    public readonly bool[] Under;
    /// <summary>pixels of the game's outer columns the last Widen replaced with the map (stale scroll-edge tiles)</summary>
    public int EdgeFixed;
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
        NoMap = new bool[w * 240];
        Under = new bool[w * 240];
    }

    /// <summary>playtest 6, no flicker: the objects the game left out this frame (SideSprites.Draw missingOnly) where no
    /// sprite of the game is, composed like the PPU (behind-BG pixels stay hidden)</summary>
    public int ApplyMissing(byte[] spr, int[] tag)
    {
        int n = 0;
        for (int i = 0; i < W * 240; i++)
        {
            int sp = spr[i];
            if (sp == 0 || Sp[i] != 0) continue;
            var pal = LinePal[i / W];
            Sp[i] = (byte)sp; SpCol[i] = (byte)(pal[0x10 | (sp & 0x0F)] & 0x3F); SpTag[i] = tag[i];
            if ((sp & 0x40) == 0 || Bg[i] == 0) { Fb[i] = SpCol[i]; Tags[i] = tag[i]; }
            n++;
        }
        return n;
    }

    /// <summary>widescreen: the side sprites (SideSprites.Draw output) over the widened background, as the PPU composes:
    /// a sprite pixel behind an opaque background pixel stays hidden</summary>
    /// <summary>playtest 9: the player in the cryo pod (area 00, normal play) is drawn with sprite palette 0 all black ($0F x3)
    /// until the game fades him in ($26 $04 -> $36 $14): a black silhouette. This remake shows him dimmed in his own colours
    /// ($16 $04 $0F) instead; true when it recoloured</summary>
    public bool PodSilhouette(int area, int mainState, int subState)
    {
        if (area != 0 || mainState != 1 || subState != 8) return false;
        // playtest 11: recolouring (playtest 9) made the stand-in visible, then the game's own player stepped out above it
        // ("he appears above and drops onto himself"). On the NES the all-black stand-in vanishes into the black doorway, so
        // it is removed: its sprite pixels show the background. Lines where the game has begun its fade-in are left alone.
        bool any = false;
        var black = new bool[240];
        for (int y = 0; y < 240; y++)
        {
            var p = LinePal[y];
            black[y] = p[0x11] == 0x0F && p[0x12] == 0x0F && p[0x13] == 0x0F && p[1] != 0x0F;
            any |= black[y];
        }
        if (!any) return false;
        for (int i = 0; i < W * 240; i++)
        {
            int sp = Sp[i];
            if (sp == 0 || (sp & 0x0C) != 0 || !black[i / W]) continue;   // sprite palette 0 only, on all-black lines
            Sp[i] = 0; SpCol[i] = 0; SpTag[i] = 0;
            Fb[i] = BgCol[i]; Tags[i] = BgTag[i];
        }
        return true;
    }

    public void ApplySideSprites(byte[] spr, int[] tag)
    {
        for (int i = 0; i < W * 240; i++)
        {
            int sp = spr[i];
            if (sp == 0) continue;
            if (Sp[i] != 0) continue;                    // the game's own sprite there wins (edge objects reach in)
            var pal = LinePal[i / W];
            Sp[i] = (byte)sp; SpCol[i] = (byte)(pal[0x10 | (sp & 0x0F)] & 0x3F); SpTag[i] = tag[i];
            bool front = (sp & 0x40) == 0 || Bg[i] == 0;
            if (front) { Fb[i] = SpCol[i]; Tags[i] = tag[i]; }
        }
    }

    /// <summary>widescreen (Wide.cs): this (W wide) = src (256) in the middle, the area at the camera on the sides of the
    /// lines that show the map (and in src's masked left column there): background only, tags with the area's tiles
    /// (pack art / materials work there too). mapLine[y] = the line shows the map (Window.Compose's rule).</summary>
    /// <remarks>hudFrom < 240 (playtest 6, "the HUD should just overlay it"): from that line down the map continues behind
    /// the status bar over the whole width (at the last verified playfield line's offset); in the game's columns only the
    /// panel stays: per line, from its first to its last non-black pixel; black margins and gap rows become map
    /// (Under = true: world pixels for the HD info, not HUD). Rows hudFrom..panelFrom-1 (the split seam) and panelTo.. (the
    /// backdrop / stray rows under the panel, Gpu.EdgeBand) are map over the whole width; the masked left column is not panel
    /// (Under = true: world pixels for the HD info, not HUD)</remarks>
    public void Widen(SceneRaster src, Widescreen wide, bool[] mapLine, int hudFrom = 240, int panelFrom = 240, int panelTo = 240)
    {
        int e = (W - 256) / 2;
        var c = wide.Cache;
        Array.Clear(Under);
        EdgeFixed = 0;
        int hdx = 0, hdy = 0;
        bool hud = false;
        if (c != null && hudFrom < 240)
            for (int y = hudFrom - 1; y >= Math.Max(8, hudFrom - 24); y--)
                if (wide.LineOk[y]) { hdx = wide.LineDx[y]; hdy = wide.LineDy[y]; hud = true; break; }
        for (int y = 0; y < 240; y++)
        {
            LineScroll[y] = src.LineScroll[y];
            Array.Copy(src.LinePal[y], LinePal[y], 32);
            bool map = mapLine[y] && c != null;
            LeftClip[y] = false;                          // the sides and the masked column are filled or black here
            int d = y * W, s = y * 256;
            Array.Clear(NoMap, d, W);
            Array.Copy(src.Fb, s, Fb, d + e, 256); Array.Copy(src.Bg, s, Bg, d + e, 256); Array.Copy(src.BgCol, s, BgCol, d + e, 256);
            Array.Copy(src.Sp, s, Sp, d + e, 256); Array.Copy(src.SpCol, s, SpCol, d + e, 256);
            Array.Copy(src.Tags, s, Tags, d + e, 256); Array.Copy(src.BgTag, s, BgTag, d + e, 256); Array.Copy(src.SpTag, s, SpTag, d + e, 256);
            var pal = src.LinePal[y];
            if (hud && y >= hudFrom)
            {
                int pl = 256, pr = -1;                    // the panel: first .. last non-black pixel of the line
                bool panelRow = y >= panelFrom && y < panelTo;
                for (int x = src.LeftClip[y] ? 8 : 0; x < 256 && panelRow; x++)
                {
                    int col = src.Fb[s + x] & 0x3F;
                    if (!((col & 15) >= 0xD && col != 0x2D && col != 0x3D)) { if (pl > x) pl = x; pr = x; }
                }
                for (int x = 0; x < W; x++)
                {
                    int gx = x - e;
                    if (gx >= pl && gx <= pr) continue;    // the panel stays on top
                    int i = d + x, ax = gx + wide.MapX + hdx, ay = y + wide.MapY + hdy;
                    bool side = gx < 0 || gx > 255;
                    Sp[i] = 0; SpCol[i] = 0; SpTag[i] = 0;
                    if (ax < 0 || ay < 0 || ax >= c!.W || ay >= c.H)
                    {
                        // outside the area (the camera at the map's bottom): the same dim fill as the sides (playtest 14: the
                        // game's columns kept their black and the masked left column's stray green beside the panel)
                        Fb[i] = BgCol[i] = 0x0F; Bg[i] = 0; BgTag[i] = Tags[i] = 0; NoMap[i] = true;
                        continue;
                    }
                    uint p = c.Px[ay * c.W + ax];
                    int v = (int)(p & 3), g = (int)(p >> 2 & 3);
                    Fb[i] = BgCol[i] = (byte)AreaCache.Color(p, pal);
                    Bg[i] = (byte)(v == 0 ? 0 : g << 2 | v);
                    BgTag[i] = Tags[i] = Ppu.TagValid | g << 19 | (ay & 7) << 16 | (ax & 7) << 13 | (int)(p >> 4 & 0x1FFF);
                    Under[i] = !side;
                }
                continue;
            }
            // playtest 6: the top 8 lines (TV overscan, never verified) take line 8's mapping on the sides
            int ldx = wide.LineDx[y], ldy = wide.LineDy[y];
            if (y < 8 && c != null && mapLine[8] && wide.LineOk[8]) { map = true; ldx = wide.LineDx[8]; ldy = wide.LineDy[8]; }
            bool verified = map && y >= 8 && wide.LineOk[y];
            for (int x = 0; x < W; x++)
            {
                bool side = x < e || x >= e + 256;
                int gx = x - e;
                if (!side && !(src.LeftClip[y] && x < e + 8))
                {
                    // playtest 6: the game's outer 8 columns show stale tiles while it scrolls (the column it is still
                    // rewriting, hidden by a TV's frame): on a verified line the map's pixel where they differ (no sprite)
                    if (!verified || (gx >= 8 && gx < 248) || src.Sp[s + gx] != 0) continue;
                    int bx = gx + wide.MapX + ldx, by = y + wide.MapY + ldy;
                    if (bx < 0 || by < 0 || bx >= c!.W || by >= c.H) continue;
                    uint q = c.Px[by * c.W + bx];
                    int qc = AreaCache.Color(q, pal);
                    if (qc == (src.Fb[s + gx] & 0x3F)) continue;
                    int k = d + x, qv = (int)(q & 3), qg = (int)(q >> 2 & 3);
                    Fb[k] = BgCol[k] = (byte)qc;
                    Bg[k] = (byte)(qv == 0 ? 0 : qg << 2 | qv);
                    BgTag[k] = Tags[k] = Ppu.TagValid | qg << 19 | (by & 7) << 16 | (bx & 7) << 13 | (int)(q >> 4 & 0x1FFF);
                    Under[k] = true;                              // world info, like the map behind the HUD
                    EdgeFixed++;
                    continue;
                }
                int i = d + x;
                Sp[i] = 0; SpCol[i] = 0; SpTag[i] = 0;
                int ax = x - e + wide.MapX + ldx, ay = y + wide.MapY + ldy;
                if (!map || ax < 0 || ay < 0 || ax >= c!.W || ay >= c.H)
                {
                    Fb[i] = BgCol[i] = 0x0F; Bg[i] = 0; BgTag[i] = Tags[i] = 0;
                    NoMap[i] = side;
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
