// re-c Windows port T20 (not in the web port): the video state of one frame, for the HD engine (ReC.Win SceneRaster),
// which draws the picture itself instead of using the PPU's pixels. Output only: the PPU writes it, nothing in the
// core reads it back, and it never changes Fb (`win_scene_raster` test).
//
// Per frame: the nametables, palette and OAM as the frame starts, plus a new copy whenever the game changes one of them
// while the frame is drawn (split hooks, mid-frame palette writes); per line: the registers the line starts with
// (after its split hook), the registers each mid-line write leaves (Ppu.Timed: dot + state), and the registers the
// sprite evaluation for the line used (end of the line before).
namespace ReC.Nes;

public sealed class VideoCapture
{
    /// <summary>register state: v, fine x, PPUCTRL, PPUMASK + the 8 CHR bank byte offsets, and which nametable /
    /// palette / OAM copy is current</summary>
    public struct Regs
    {
        public int V, X, Ctrl, Mask, Vram, Pal, Oam;
        public Offs Banks;
        public readonly bool Rendering => (Mask & 0x18) != 0;
    }

    [System.Runtime.CompilerServices.InlineArray(8)]
    public struct Offs { private int _e; }

    public readonly struct Event
    {
        public readonly int Dot;
        public readonly Regs R;
        public Event(int dot, Regs r) { Dot = dot; R = r; }
    }

    public int Mirroring;
    public readonly Regs[] Line = new Regs[240];
    /// <summary>sprite evaluation for line L (done at the end of L - 1); Valid false = no sprites on L</summary>
    public readonly Regs[] Spr = new Regs[240];
    public readonly bool[] SprValid = new bool[240];
    /// <summary>mid-line writes of line L, in dot order (empty for most lines)</summary>
    public readonly List<Event>[] Events = Enumerable.Range(0, 240).Select(_ => new List<Event>()).ToArray();
    public readonly List<byte[]> Vram = new(), Pal = new(), Oam = new();
    private int _nVram, _nPal, _nOam;
    private bool _dVram, _dPal, _dOam;
    public int FrameNo;
    /// <summary>frames recorded so far (0 = nothing to draw yet)</summary>
    public int Frames;

    private static byte[] Copy(List<byte[]> pool, ref int n, byte[] src)
    {
        if (n == pool.Count) pool.Add(new byte[src.Length]);
        var d = pool[n++];
        Buffer.BlockCopy(src, 0, d, 0, src.Length);
        return d;
    }

    internal void Begin(Ppu p)
    {
        _nVram = _nPal = _nOam = 0;
        Copy(Vram, ref _nVram, p.Vram); Copy(Pal, ref _nPal, p.Pal); Copy(Oam, ref _nOam, p.Oam);
        _dVram = _dPal = _dOam = false;
        Mirroring = p.Mirroring;
        Frames++;
        FrameNo = p.FrameNo;
        foreach (var e in Events) e.Clear();
        Array.Clear(SprValid);
    }

    /// <summary>a nametable (0), palette (1) or OAM (2) write while the frame is drawn: a new copy before the next use</summary>
    internal void Dirty(int what)
    {
        if (what == 0) _dVram = true; else if (what == 1) _dPal = true; else _dOam = true;
    }

    internal Regs Snap(Ppu p, int[] offs)
    {
        if (_dVram) { Copy(Vram, ref _nVram, p.Vram); _dVram = false; }
        if (_dPal) { Copy(Pal, ref _nPal, p.Pal); _dPal = false; }
        if (_dOam) { Copy(Oam, ref _nOam, p.Oam); _dOam = false; }
        var r = new Regs { V = p.V, X = p.X, Ctrl = p.Ctrl, Mask = p.Mask, Vram = _nVram - 1, Pal = _nPal - 1, Oam = _nOam - 1 };
        for (int i = 0; i < 8; i++) r.Banks[i] = offs[i];
        return r;
    }
}
