// re-c C# port — NES-style PPU (= web/src/nes/ppu.js).
//
// Model: the game writes the same state the ROM writes ($2000-$2007 via WriteReg, OAM via OamDma,
// MMC3 CHR registers R0-R5 via SetChrReg, nametable mirroring), and RenderFrame() turns it into a
// 256x240 framebuffer of NES colour indices ($00-$3F), one scanline at a time:
//   pre-render: if rendering, v <- t (vertical bits from the t latched after the last NMI, s125; horizontal live)
//   line L    : render BG from v/fine x (33 tiles) + the sprite line evaluated at the end of L-1
//               then (if rendering) Y increment, horizontal copy t -> v, sprite evaluation for L+1,
//               then the split hook for L+1 (IRQ handlers: scroll/CHR/mask changes take effect on the next line).
// Mid-line writes (s209): a split hook can queue writes at a dot of its line with Timed(dot, fn); such a
// line is rendered by RenderLineTimed with tools/emu/ppu.py's fetch pipeline (IRQ handlers 1/2/3 use it).
// Not modelled (no game code depends on it): sprite-0 hit / overflow flags, $2007 reads, colour emphasis.
namespace ReC.Nes;

// js: nes/ppu.js Ppu
public sealed class Ppu
{
    // js: nes/ppu.js PAL_MIRROR
    private static int PalMirror(int a)
    {
        a &= 0x1F;
        return (a & 0x13) == 0x10 ? a & 0x0F : a;
    }

    // 2bpp row decode cache: key lo<<8|hi -> 8 pixel values (leftmost first)
    private static readonly byte[] Dec = BuildDec();

    private static byte[] BuildDec()
    {
        var d = new byte[65536 * 8];
        for (int lo = 0; lo < 256; lo++)
            for (int hi = 0; hi < 256; hi++)
            {
                int o = ((lo << 8) | hi) * 8;
                for (int i = 0; i < 8; i++) d[o + i] = (byte)(((lo >> (7 - i)) & 1) | (((hi >> (7 - i)) & 1) << 1));
            }
        return d;
    }

    public readonly byte[] Chr;                   // 1KB banks (web/data/chr.bin)
    public readonly int NChr;
    public readonly byte[] Fb = new byte[256 * 240];
    public readonly byte[] Vram = new byte[0x800];   // CIRAM (2 nametables)
    public readonly byte[] Pal = new byte[32];
    public readonly byte[] Oam = new byte[256];
    public readonly byte[] ChrRegs = new byte[6];    // MMC3 R0-R5
    public readonly byte[] SprBuf = new byte[256];   // sprite line for the next line (tag | colour)
    public readonly byte[] SprNext = new byte[256];
    /// <summary>line -> fn(ppu) run before that line renders</summary>
    public readonly Dictionary<int, Action<Ppu>> Splits = new();

    public int Ctrl, Mask, Status;
    public int V, T, X, W;
    public int? TVert;
    public int OamAddr;
    public int Mirroring;
    public int ChrInv;
    public int FrameNo;

    private int[]? _banks;
    private List<(int Dot, Action<Ppu> Fn)>? _timed;
    private bool _inTimed;
    private readonly byte[] _bgLine = new byte[264];

    // ---- Windows port T10 (not in the web port): per-pixel source tags for the Enhanced graphics renderer.
    // Null = off (the default, and in every parity run). When set, every Fb pixel also gets
    //   tag = 1<<24 | flipV<<23 | flipH<<22 | group<<19 | row<<16 | col<<13 | tileNo
    // group 0-3 = BG palette, 4-7 = sprite palette; row / col = the pixel inside the 8x8 tile in tile space (before
    // the flip); tileNo = CHR byte address / 16 (0-8191); 0 = backdrop (no tile). Fb itself is never changed by it.
    public int[]? Tags;
    private readonly int[] _bgTag = new int[264];
    private readonly int[] _tileTag = new int[34 * 8];
    private readonly int[] _sprTag = new int[256];

    public const int TagValid = 1 << 24;

    // ---- Windows port T20 (not in the web port): the HD engine draws the picture itself from the video state.
    // Capture (null = off, the default, and in every parity run) records that state per frame (VideoCapture.cs),
    // output only. NoPixels (false = the default) skips the PPU's own pixel work (Fb and Tags are not updated, nothing
    // else changes: register, scroll and split behaviour stay exact; the game never reads the picture back).
    public VideoCapture? Capture;
    public bool NoPixels;

    private static int BgTag(int[] offs, int pa, int p, int col) =>
        TagValid | (p >> 2) << 19 | (pa & 7) << 16 | col << 13 | ((offs[pa >> 10] + (pa & 0x3FF)) >> 4);
    private readonly short[] _tileBuf = new short[34 * 8];
    private readonly int[] _offs = new int[8];

    // js: nes/ppu.js constructor
    public Ppu(byte[] chr)
    {
        Chr = chr;
        NChr = chr.Length >> 10;
        Reset();
    }

    // js: nes/ppu.js reset
    public void Reset()
    {
        Ctrl = 0; Mask = 0; Status = 0;
        V = 0; T = 0; X = 0; W = 0; TVert = null;
        OamAddr = 0;
        Mirroring = 1;                    // re-c Reset writes $A000 = 1 (horizontal)
        ChrInv = 0;                       // MMC3 $8000 b7 (re-c: always 0, CHR mode 0)
        ChrRegs[0] = 0; ChrRegs[1] = 2; ChrRegs[2] = 4; ChrRegs[3] = 5; ChrRegs[4] = 6; ChrRegs[5] = 7;
        Array.Clear(Vram); Array.Clear(Pal); Array.Clear(Oam); Array.Clear(Fb);
        Array.Clear(SprBuf);
        FrameNo = 0;
    }

    // js: nes/ppu.js rendering
    public bool Rendering => (Mask & 0x18) != 0;

    // ------------------------------------------------------------ MMC3 CHR
    // js: nes/ppu.js setChrReg
    public void SetChrReg(int r, int val)
    {
        if (_timed != null) FlushTimed();
        ChrRegs[r] = (byte)val;
    }

    // js: nes/ppu.js chrBanks
    /// <summary>eight 1KB bank numbers mapped at PPU $0000-$1FFF</summary>
    public int[] ChrBanks()
    {
        var r = ChrRegs;
        int[] lo = { r[0] & 0xFE, r[0] | 1, r[1] & 0xFE, r[1] | 1 };
        int[] hi = { r[2], r[3], r[4], r[5] };
        var all = ChrInv != 0 ? hi.Concat(lo) : lo.Concat(hi);
        return all.Select(b => b % NChr).ToArray();
    }

    // js: nes/ppu.js setChrBanks
    /// <summary>override the computed mapping (used by tests that replay emulator snapshots)</summary>
    public void SetChrBanks(int[]? banks) => _banks = banks;

    // js: nes/ppu.js _bankOffsets
    private int[] BankOffsets()
    {
        var o = _offs;
        if (_banks != null)
        {
            for (int i = 0; i < 8; i++) o[i] = _banks[i] << 10;
            return o;
        }
        var r = ChrRegs;
        int n = NChr;
        int b0 = r[0] & 0xFE, b1 = r[0] | 1, b2 = r[1] & 0xFE, b3 = r[1] | 1;
        if (ChrInv != 0)
        {
            o[0] = (r[2] % n) << 10; o[1] = (r[3] % n) << 10; o[2] = (r[4] % n) << 10; o[3] = (r[5] % n) << 10;
            o[4] = (b0 % n) << 10; o[5] = (b1 % n) << 10; o[6] = (b2 % n) << 10; o[7] = (b3 % n) << 10;
        }
        else
        {
            o[0] = (b0 % n) << 10; o[1] = (b1 % n) << 10; o[2] = (b2 % n) << 10; o[3] = (b3 % n) << 10;
            o[4] = (r[2] % n) << 10; o[5] = (r[3] % n) << 10; o[6] = (r[4] % n) << 10; o[7] = (r[5] % n) << 10;
        }
        return o;
    }

    // ------------------------------------------------------------ VRAM
    // js: nes/ppu.js ntIndex
    public int NtIndex(int a)
    {
        a &= 0x0FFF;
        if (Mirroring == 0) return a & 0x7FF;                       // vertical
        return ((a >> 1) & 0x400) | (a & 0x3FF);                    // horizontal
    }

    // js: nes/ppu.js vramWrite
    public void VramWrite(int a, int val)
    {
        if (_timed != null) FlushTimed();
        a &= 0x3FFF;
        if (a < 0x2000) return;                                     // CHR ROM: ignored
        if (a < 0x3F00) Vram[NtIndex(a)] = (byte)val;
        else Pal[PalMirror(a)] = (byte)(val & 0x3F);
        Capture?.Dirty(a < 0x3F00 ? 0 : 1);
    }

    // js: nes/ppu.js vramRead
    public int VramRead(int a)
    {
        a &= 0x3FFF;
        if (a < 0x2000) { var o = BankOffsets(); return Chr[o[a >> 10] + (a & 0x3FF)]; }
        if (a < 0x3F00) return Vram[NtIndex(a)];
        return Pal[PalMirror(a)];
    }

    // ------------------------------------------------------------ CPU registers ($2000-$2007, vblank semantics)
    // js: nes/ppu.js readStatus
    /// <summary>$2002: clears w (vblank flag not modelled)</summary>
    public int ReadStatus()
    {
        if (_timed != null) FlushTimed();
        W = 0;
        return Status;
    }

    // js: nes/ppu.js writeReg
    public void WriteReg(int r, int val)
    {
        if (_timed != null) FlushTimed();
        val &= 0xFF;
        switch (r & 7)
        {
            case 0: Ctrl = val; T = (T & 0xF3FF) | ((val & 3) << 10); break;
            case 1: Mask = val; break;
            case 3: OamAddr = val; break;
            case 4: Oam[OamAddr] = (byte)val; OamAddr = (OamAddr + 1) & 0xFF; Capture?.Dirty(2); break;
            case 5:
                if (W == 0) { T = (T & 0x7FE0) | (val >> 3); X = val & 7; W = 1; }
                else { T = (T & 0x0C1F) | ((val & 7) << 12) | ((val & 0xF8) << 2); W = 0; }
                break;
            case 6:
                if (W == 0) { T = (T & 0x00FF) | ((val & 0x3F) << 8); W = 1; }
                else { T = (T & 0x7F00) | val; V = T; W = 0; }
                break;
            case 7:
                VramWrite(V, val);
                V = (V + ((Ctrl & 4) != 0 ? 32 : 1)) & 0x7FFF;
                break;
            default: break;
        }
    }

    // js: nes/ppu.js oamDma
    /// <summary>$4014: 256 bytes from RAM page</summary>
    public void OamDma(byte[] src, int off = 0)
    {
        if (_timed != null) FlushTimed();
        for (int i = 0; i < 256; i++) Oam[(OamAddr + i) & 0xFF] = src[off + i];
        Capture?.Dirty(2);
    }

    public void OamDma(ArraySegment<byte> page) => OamDma(page.Array!, page.Offset);

    // ------------------------------------------------------------ scroll helpers (loopy v)
    // js: nes/ppu.js _incX
    private void IncX()
    {
        int v = V;
        V = (v & 0x1F) == 31 ? (v & ~0x1F) ^ 0x400 : v + 1;
    }

    // js: nes/ppu.js _incY
    private void IncY()
    {
        int v = V;
        if ((v & 0x7000) != 0x7000) { V = v + 0x1000; return; }
        v &= ~0x7000;
        int y = (v & 0x3E0) >> 5;
        if (y == 29) { y = 0; v ^= 0x800; } else if (y == 31) y = 0; else y++;
        V = (v & ~0x3E0) | (y << 5);
    }

    // ------------------------------------------------------------ rendering
    // js: nes/ppu.js renderLine
    /// <summary>Render one visible line from the current v (= line start, before the 2-tile prefetch).</summary>
    public void RenderLine(int line)
    {
        if (NoPixels) return;
        byte[] fb = Fb, pal = Pal;
        int @base = line * 256, mask = Mask;
        int grey = (mask & 1) != 0 ? 0x30 : 0x3F;
        if ((mask & 0x18) == 0)
        {
            int v = V;
            byte c = (byte)(((v & 0x3F00) == 0x3F00 ? pal[PalMirror(v)] : pal[0]) & grey);
            Array.Fill(fb, c, @base, 256);
            if (Tags != null) Array.Clear(Tags, @base, 256);
            return;
        }
        // BG: 33 tiles starting at v
        var bgLine = _bgLine;
        if ((mask & 0x08) != 0)
        {
            var offs = BankOffsets();
            byte[] chr = Chr, vram = Vram;
            int ptBase = (Ctrl & 0x10) != 0 ? 0x1000 : 0;
            int v0 = V;
            int v = v0;
            for (int t = 0; t < 33; t++)
            {
                int tile = vram[NtIndex(0x2000 | (v & 0x0FFF))];
                int at = vram[NtIndex(0x23C0 | (v & 0x0C00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07))];
                int p = ((at >> (((v >> 4) & 4) | (v & 2))) & 3) << 2;
                int pa = ptBase + tile * 16 + ((v >> 12) & 7);
                int lo = chr[offs[pa >> 10] + (pa & 0x3FF)];
                int pb = pa + 8;
                int hi = chr[offs[pb >> 10] + (pb & 0x3FF)];
                int d = ((lo << 8) | hi) * 8;
                for (int i = 0; i < 8; i++) { int c = Dec[d + i]; bgLine[t * 8 + i] = (byte)(c != 0 ? (p | c) : 0); }
                if (Tags != null) for (int i = 0; i < 8; i++) _bgTag[t * 8 + i] = BgTag(offs, pa, p, i);
                v = (v & 0x1F) == 31 ? (v & ~0x1F) ^ 0x400 : v + 1;
            }
            V = v0;
        }
        else Array.Clear(bgLine);
        int bgFrom = (mask & 0x08) != 0 ? ((mask & 2) != 0 ? 0 : 8) : 256;
        int spFrom = (mask & 4) != 0 ? 0 : 8;
        byte[]? spr = (mask & 0x10) != 0 ? SprBuf : null;
        int fx = X;
        for (int x = 0; x < 256; x++)
        {
            int bp = x >= bgFrom ? bgLine[x + fx] : 0;
            int sp = (spr != null && x >= spFrom) ? spr[x] : 0;
            int col;
            if (sp != 0) col = (bp != 0 && (sp & 0x40) != 0) ? pal[bp] : pal[0x10 | (sp & 0x0F)];
            else col = pal[bp];
            fb[@base + x] = (byte)(col & grey);
        }
        if (Tags != null) TagLine(@base, bgFrom, spr, spFrom, i => bgLine[i], i => _bgTag[i], fx, (mask & 0x08) != 0);
    }

    /// <summary>T10 tags for pixels [x0, xe) of a line, with the same layer choice as the pixel loops</summary>
    private void TagLine(int @base, int bgFrom, byte[]? spr, int spFrom, Func<int, int> bgPix, Func<int, int> bgTag,
        int fx, bool bgOn, int x0 = 0, int xe = 256)
    {
        var tags = Tags!;
        for (int x = x0; x < xe; x++)
        {
            int bp = x >= bgFrom ? bgPix(x + fx) : 0;
            int sp = (spr != null && x >= spFrom) ? spr[x] : 0;
            if (sp != 0 && !(bp > 0 && (sp & 0x40) != 0)) tags[@base + x] = _sprTag[x];
            // a transparent BG pixel keeps its tile tag (the enhanced tile may round into it); none left of bgFrom
            else tags[@base + x] = bgOn && x >= bgFrom ? bgTag(x + fx) : 0;
        }
    }

    // js: nes/ppu.js evalSprites
    /// <summary>Sprite evaluation for line `line` (OAM y = line-1); result used by the next RenderLine.</summary>
    public void EvalSprites(int line)
    {
        var buf = SprBuf;
        Array.Clear(buf);
        if (line <= 0 || line >= 240) return;
        int ctrl = Ctrl, h = (ctrl & 0x20) != 0 ? 16 : 8;
        var oam = Oam;
        if (NoPixels)                                 // T20: only the overflow flag
        {
            int cnt = 0;
            for (int i = 0; i < 256; i += 4) { int row = line - 1 - oam[i]; if (row >= 0 && row < h && ++cnt > 8) { Status |= 0x20; break; } }
            return;
        }
        var offs = BankOffsets();
        var chr = Chr;
        int n = 0;
        for (int i = 0; i < 256; i += 4)
        {
            int row = line - 1 - oam[i];
            if (row < 0 || row >= h) continue;
            if (n == 8) { Status |= 0x20; break; }
            n++;
            int tile = oam[i + 1], at = oam[i + 2], sx = oam[i + 3];
            if ((at & 0x80) != 0) row = h - 1 - row;
            int pa = h == 16
                ? ((tile & 1) << 12) + ((tile & 0xFE) + (row >> 3)) * 16 + (row & 7)
                : ((ctrl & 8) != 0 ? 0x1000 : 0) + tile * 16 + row;
            int lo = chr[offs[pa >> 10] + (pa & 0x3FF)];
            int hi = chr[offs[(pa + 8) >> 10] + ((pa + 8) & 0x3FF)];
            int d = ((lo << 8) | hi) * 8;
            int tag = ((at & 3) << 2) | ((at & 0x20) != 0 ? 0x40 : 0) | (i == 0 ? 0x80 : 0);
            for (int k = 0; k < 8; k++)
            {
                int xx = sx + k;
                if (xx > 255) break;
                int c = Dec[d + ((at & 0x40) != 0 ? 7 - k : k)];
                if (c != 0 && buf[xx] == 0)
                {
                    buf[xx] = (byte)(tag | c);
                    if (Tags != null)
                    {
                        int a = offs[pa >> 10] + (pa & 0x3FF);
                        _sprTag[xx] = TagValid | ((at & 0x80) != 0 ? 1 << 23 : 0) | ((at & 0x40) != 0 ? 1 << 22 : 0) |
                                      (4 + (at & 3)) << 19 | (pa & 7) << 16 | ((at & 0x40) != 0 ? 7 - k : k) << 13 | (a >> 4);
                    }
                }
            }
        }
    }

    // js: nes/ppu.js latchVert
    /// <summary>s125: the pre-render line's vertical copy (t -> v bits $7BE0) happens ~20 lines after the NMI;
    /// Machine latches t here once its NMI is done (see ppu.js).</summary>
    public void LatchVert() => TVert = T;

    // js: nes/ppu.js renderFrame
    /// <summary>Render a whole frame. Split hooks (Splits: line -> fn(ppu)) run before their line.</summary>
    public void RenderFrame()
    {
        Status &= 0x1F;
        if (Rendering)
        {
            int tv = TVert ?? T;
            V = (tv & 0x7BE0) | (T & 0x041F);
        }
        Array.Clear(SprBuf);                  // no sprites on line 0
        Capture?.Begin(this);
        for (int line = 0; line < 240; line++)
        {
            if (Splits.TryGetValue(line, out var hook)) hook(this);
            if (Capture != null) Capture.Line[line] = Capture.Snap(this, BankOffsets());
            if (_timed != null) RenderLineTimed(line);
            else RenderLine(line);
            if (Rendering)
            {
                IncY();
                V = (V & ~0x041F) | (T & 0x041F);
                if (Capture != null && line < 239) { Capture.Spr[line + 1] = Capture.Snap(this, BankOffsets()); Capture.SprValid[line + 1] = true; }
                EvalSprites(line + 1);
            }
            else Array.Clear(SprBuf);
        }
        FlushTimed();                         // a split hook on line 239's end has no line left
        Status |= 0x80;                       // vblank
        FrameNo++;
    }

    // ------------------------------------------------------------ mid-line writes (s209)
    // js: nes/ppu.js timed
    /// <summary>Queue fn(ppu) to run at `dot` of the line the current split hook precedes. Any other register
    /// access first runs the queue at once, so the register state stays in program order; only the line's
    /// pixels see the dot.</summary>
    public void Timed(int dot, Action<Ppu> fn)
    {
        (_timed ??= new()).Add((dot, fn));
    }

    // js: nes/ppu.js _flushTimed
    private void FlushTimed()
    {
        var q = _timed;
        if (q == null || _inTimed) return;
        _timed = null;
        _inTimed = true;
        foreach (var (_, fn) in q) fn(this);
        _inTimed = false;
    }

    // js: nes/ppu.js _renderLineTimed
    /// <summary>One visible line with queued writes, after tools/emu/ppu.py: tiles 0/1 come from the previous
    /// line's prefetch (v, v+1); tile t >= 2 is fetched at dot 8(t-1) from the live v / CHR banks, then coarse
    /// X++. A write at dot D is seen by fetches at dots >= D and by pixels x >= D-1, with mask / fine x /
    /// palette read live per pixel. V is left at the reloaded v.</summary>
    private void RenderLineTimed(int line)
    {
        var q = _timed!.OrderBy(e => e.Dot).ToList();   // stable, like Array.prototype.sort
        _timed = null;
        var fb = Fb;
        int @base = line * 256;
        var tiles = _tileBuf;
        Array.Fill(tiles, (short)-1);                    // -1: not fetched (transparent)
        if (Tags != null) Array.Clear(_tileTag);

        // js: nes/ppu.js fetch
        void Fetch(int t)
        {
            int v = V;
            byte[] vram = Vram, chr = Chr;
            var offs = BankOffsets();
            int tile = vram[NtIndex(0x2000 | (v & 0x0FFF))];
            int at = vram[NtIndex(0x23C0 | (v & 0x0C00) | ((v >> 4) & 0x38) | ((v >> 2) & 0x07))];
            int p = ((at >> (((v >> 4) & 4) | (v & 2))) & 3) << 2;
            int pa = ((Ctrl & 0x10) != 0 ? 0x1000 : 0) + tile * 16 + ((v >> 12) & 7);
            int lo = chr[offs[pa >> 10] + (pa & 0x3FF)], pb = pa + 8, hi = chr[offs[pb >> 10] + (pb & 0x3FF)];
            int d = ((lo << 8) | hi) * 8;
            for (int i = 0; i < 8; i++) { int c = Dec[d + i]; tiles[t * 8 + i] = (short)(c != 0 ? (p | c) : 0); }
            if (Tags != null) for (int i = 0; i < 8; i++) _tileTag[t * 8 + i] = BgTag(offs, pa, p, i);
            IncX();
        }

        if (Rendering) { Fetch(0); Fetch(1); }
        int nextT = 2, x = 0;

        // js: nes/ppu.js fetchBefore
        void FetchBefore(int dot)                        // fetches at dots 8.. < dot
        {
            while (nextT <= 32 && 8 * (nextT - 1) < dot) { if (Rendering) Fetch(nextT); nextT++; }
        }

        // js: nes/ppu.js drawBefore
        void DrawBefore(int xe)                          // pixels x .. < xe with the current state
        {
            xe = Math.Min(xe, 256);
            if (NoPixels) { x = Math.Max(x, xe); return; }
            byte[] pal = Pal;
            int mask = Mask, grey = (mask & 1) != 0 ? 0x30 : 0x3F;
            if ((mask & 0x18) == 0)
            {
                int v = V;
                byte c = (byte)(((v & 0x3F00) == 0x3F00 ? pal[PalMirror(v)] : pal[0]) & grey);
                if (Tags != null && x < xe) Array.Clear(Tags, @base + x, xe - x);
                for (; x < xe; x++) fb[@base + x] = c;
                return;
            }
            int bgFrom = (mask & 0x08) != 0 ? ((mask & 2) != 0 ? 0 : 8) : 256;
            int spFrom = (mask & 4) != 0 ? 0 : 8;
            byte[]? spr = (mask & 0x10) != 0 ? SprBuf : null;
            int fx = X;
            if (Tags != null && x < xe)
                TagLine(@base, bgFrom, spr, spFrom, i => tiles[i], i => tiles[i] >= 0 ? _tileTag[i] : 0, fx, (mask & 0x08) != 0, x, xe);
            for (; x < xe; x++)
            {
                int b = x >= bgFrom ? tiles[x + fx] : 0;
                int bp = b > 0 ? b : 0;
                int sp = (spr != null && x >= spFrom) ? spr[x] : 0;
                int col;
                if (sp != 0) col = (bp != 0 && (sp & 0x40) != 0) ? pal[bp] : pal[0x10 | (sp & 0x0F)];
                else col = pal[bp];
                fb[@base + x] = (byte)(col & grey);
            }
        }

        _inTimed = true;
        foreach (var (dot, fn) in q)
        {
            FetchBefore(dot);
            DrawBefore(dot - 1);
            fn(this);
            Capture?.Events[line].Add(new VideoCapture.Event(dot, Capture.Snap(this, BankOffsets())));
        }
        _inTimed = false;
        FetchBefore(257);
        DrawBefore(256);
    }

    // js: nes/ppu.js unprefetch
    /// <summary>helper for tests/snapshots: v at dot 0 (after the 2-tile prefetch) -> v at the line start</summary>
    public static int Unprefetch(int v)
    {
        for (int i = 0; i < 2; i++) v = (v & 0x1F) == 0 ? (v | 0x1F) ^ 0x400 : v - 1;
        return v;
    }
}
