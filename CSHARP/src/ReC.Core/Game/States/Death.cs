// re-c C# port — PlayerDeath 19:B900 (sub_state 3) + OpelRevive 19:B9B0, and the shared helpers they need
// (= web/src/game/states/death.js, s88). REVERSE.md "Death / game over sequence".
//
// C# s2 (T3): the shared helpers. PlayerDeath / OpelRevive / StatRecalc and the rest follow with T4.
//   WaitFrame / WaitFrames  WaitFrame 30:C169 / 30:C176
//   FadeOut / FadeIn        26:8C3D / 26:8C49 (5 PaletteBuild steps, A+1 frames apart, then 1 frame)
//   HudRedraw(m, e)         26:8E46 (HudElemTable 26:8EC5 record e -> digits -> PPU queue, src $7B)
//   HudHpBar(m)             26:8CC0 (HP bar $6060-$6070 -> PPU queue, src $60)
//
// A `Y.Spin` marks a ROM spin loop that does NOT clear frame_ready (PpuQueueWaitSrc 30:C72B, WaitPpuQueue
// 30:C676); a plain `Y.Frame` = WaitFrame (frame_ready cleared before the NMI). `Y.NoNmi` = the frame's NMI is
// lost (s107). JS SPIN / NO_NMI = Game/Y.cs.
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class Death
{
    // js: game/states/death.js dec
    private static int Dec(Machine m, int a) { int v = (m.Rd(a) - 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/states/death.js waitFrame
    /// <summary>WaitFrame 30:C169</summary>
    public static IEnumerable<Y> WaitFrame(Machine m)
    {
        m.Wr(R.FrameReady, 0);
        yield return Y.Frame;
        m.Wr(R.FrameReady, 0);
    }

    // js: game/states/death.js waitFrames
    /// <summary>30:C176: Y x WaitFrame (Y = 0 -> 256)</summary>
    public static IEnumerable<Y> WaitFrames(Machine m, int y)
    {
        y &= 0xFF;
        do { foreach (var s in WaitFrame(m)) yield return s; y = (y - 1) & 0xFF; } while (y != 0);
    }

    // js: game/states/death.js paletteBuildRom
    /// <summary>PaletteBuild 26:8C0E / 8C10 (A = fade) / 8C12 (fade = $11 as is), with the zero-page temps it leaves:
    /// $11 = fade, $1A-$1C = last group's pointer / id, $1D = mask shifted out, then SetR7(saved $6F).</summary>
    public static void PaletteBuildRom(Machine m, int? fade = null)
    {
        int f = fade ?? m.Rd(0x11);
        m.Wr(0x11, f);
        int r7 = m.Rd(0x6F);
        Palette.PaletteBuild(m, f);
        int id = (m.Rd(0x07E7) + 0xB0) & 0xFF;            // last group (X = $1C)
        m.Wr(0x1C, id);
        m.Wr(0x1A, ((id & 3) << 2) | 0xF0);
        m.Wr(0x1B, ((id >> 2) & 0x1F) | 0xA0);
        m.Wr(0x1D, 0);
        Prg.SetR7(m, r7);
    }

    // js: game/states/death.js fade
    /// <summary>26:8C3D (A = frames-1 per step; $11 0 -> $40: darker) / 26:8C49 ($11 $40 -> 0: back to full)</summary>
    private static IEnumerable<Y> Fade(Machine m, int a, int start, int step)
    {
        m.Wr(0x18, a); m.Wr(0x11, start); m.Wr(0x19, step); m.Wr(0x17, 5);
        do
        {
            foreach (var s in WaitFrames(m, m.Rd(0x18) + 1)) yield return s;
            PaletteBuildRom(m);                               // 8C5D JSR 8C12
            m.Wr(0x11, (m.Rd(0x11) + m.Rd(0x19)) & 0xFF);
        } while (Dec(m, 0x17) != 0);
        foreach (var s in WaitFrame(m)) yield return s;       // 8C6B JMP WaitFrame
    }

    // js: game/states/death.js fadeOut
    public static IEnumerable<Y> FadeOut(Machine m, int a) => Fade(m, a, 0x00, 0x10);
    // js: game/states/death.js fadeIn
    public static IEnumerable<Y> FadeIn(Machine m, int a) => Fade(m, a, 0x40, 0xF0);

    // js: game/states/death.js queueFlushNow
    /// <summary>30:C8B2: PpuQueueFlush right away (rendering off), with ppuctrl_shadow b7 clear meanwhile.
    /// Returns Y as PpuQueueFlush leaves it (s113; null = untouched).</summary>
    public static int? QueueFlushNow(Machine m)
    {
        int s = m.Rd(R.PpuctrlShadow);
        m.Wr(R.PpuctrlShadow, s & 0x7F);
        int? y = PpuQueue.PpuQueueFlush(m);
        m.Wr(R.PpuctrlShadow, s);
        return y;
    }

    // js: game/states/death.js hudQueue
    /// <summary>tail shared by the HUD writers: PpuNmiOff, queue record, flush now if rendering is off, PpuNmiOn</summary>
    public static int? HudQueue(Machine m, int hi, int lo, int len, int src)
    {
        PpuQueue.PpuCtrlNmiOff(m);
        PpuQueue.QueuePush(m, hi, lo, len, src);
        int? y = (m.Rd(R.PpumaskShadow) & 0x18) != 0 ? null : QueueFlushNow(m);
        PpuQueue.PpuCtrlNmiOn(m);
        return y;
    }

    // js: game/states/death.js toDecimal
    /// <summary>26:8F2B: 16-bit $10/$11 -> 5 decimal digits $20 (10^4) .. $24 (10^0), table 26:8F64</summary>
    private static void ToDecimal(Machine m)
    {
        for (int i = 0; i < 5; i++) m.Wr(0x20 + i, 0);
        m.Wr(0x12, 8);
        for (; ; )
        {
            int x = m.Rd(0x12);
            int p = Prg.CpuRd(m, 0x8F64 + x) | Prg.CpuRd(m, 0x8F65 + x) << 8;
            int v = (m.Rd(0x10) | m.Rd(0x11) << 8) - p;
            if (v < 0)
            {                                                 // BCC 8F5D: next power down
                int n = (x - 2) & 0xFF;
                m.Wr(0x12, n);
                if ((n & 0x80) != 0) return;
                continue;
            }
            m.Wr(0x10, v & 0xFF); m.Wr(0x11, v >> 8);
            m.Wr(0x13, x);
            int d = (8 - x) >> 1;
            m.Wr(0x20 + d, m.Rd(0x20 + d) + 1);
        }
    }

    /// <summary>HudRedraw's result: the Y register it leaves (s113), set when the iterator ends.</summary>
    public sealed class YOut { public int Y; }

    // js: game/states/death.js hudRedraw
    /// <summary>HudRedraw 26:8E46: element e (0-6) of HudElemTable 26:8EC5 (value ptr, PPU lo/hi, skip, 16-bit flag).
    /// The JS generator's return value (the Y register: LDY $18, or the flush's Y) goes to `res.Y`.</summary>
    public static IEnumerable<Y> HudRedraw(Machine m, int e, int xReg = 0, YOut? res = null)
    {
        m.Wr(0x14, xReg);
        while (PpuQueue.QueueHasSrc(m, 0x7B)) yield return Y.Spin;   // PpuQueueWaitSrc($7B)
        int x = e * 6;
        m.Wr(0x10, (e << 1) & 0xFF);
        m.Wr(0x18, x);
        int ptr = Prg.CpuRd(m, 0x8EC5 + x) | Prg.CpuRd(m, 0x8EC6 + x) << 8;
        m.Wr(0x12, ptr & 0xFF); m.Wr(0x13, ptr >> 8);
        m.Wr(0x10, Prg.CpuRd(m, ptr)); m.Wr(0x11, Prg.CpuRd(m, ptr + 1));
        if (Prg.CpuRd(m, 0x8ECA + x) == 0) m.Wr(0x11, 0);          // 8-bit value
        ToDecimal(m);
        int skip = Prg.CpuRd(m, 0x8EC9 + x);
        for (int y = 4, s = skip + 4; y >= 0; y--, s--) m.Wr(0x607B + y, m.Rd((0x20 + s) & 0xFF));
        int? yr = HudQueue(m, Prg.CpuRd(m, 0x8EC8 + x), Prg.CpuRd(m, 0x8EC7 + x), (5 - skip) & 0xFF, 0x7B);
        if (res != null) res.Y = yr ?? x;
    }

    // js: game/states/death.js hudRedrawCycles
    /// <summary>s216 (lag model): exact ROM cycles of HudRedraw 26:8E46 for element e, no-wait case with rendering on
    /// (see death.js for the cycle breakdown).</summary>
    public static int HudRedrawCycles(Machine m, int e)
    {
        int x = e * 6;
        int ptr = Prg.CpuRd(m, 0x8EC5 + x) | Prg.CpuRd(m, 0x8EC6 + x) << 8;
        bool wide = Prg.CpuRd(m, 0x8ECA + x) != 0;
        int v = Prg.CpuRd(m, ptr) | (wide ? Prg.CpuRd(m, ptr + 1) << 8 : 0), ds = 0;
        for (; v != 0; v /= 10) ds += v % 10;
        return 73 + ((ptr & 0xFF) == 0xFF ? 1 : 0) + (wide ? 3 : 7) + 143 + 6 + 212 + 52 * ds + 15 + 79 + 75 + 30;
    }

    // js: game/states/death.js hudHpBar
    /// <summary>HudHpBar 26:8CC0: 17 tiles at $2B26: $8C/$8D frame up to max HP, $88 full cells, partial cell 26:8D49</summary>
    public static IEnumerable<Y> HudHpBar(Machine m)
    {
        while (PpuQueue.QueueHasSrc(m, 0x60)) yield return Y.Spin;   // PpuQueueWaitSrc($60)
        for (int i = 0; i <= 0x10; i++) m.Wr(0x6060 + i, 0x20);
        // js: game/states/death.js less1
        static int Less1(int v) => v == 0 ? 0 : v - 1;
        int x = Less1(m.Rd(0x03C0)) >> 4;
        m.Wr(0x6061 + x, 0x8D);
        for (; x >= 0; x--) m.Wr(0x6060 + x, 0x8C);
        int hp = Less1(m.Rd(0x03C1));
        int y = hp >> 4;
        for (x = y; x > 0; x--) m.Wr(0x605F + x, 0x88);
        m.Wr(0x6060 + y, Prg.CpuRd(m, 0x8D49 + (hp & 0x0F)));
        HudQueue(m, 0x2B, 0x26, 0x11, 0x60);
    }
}
