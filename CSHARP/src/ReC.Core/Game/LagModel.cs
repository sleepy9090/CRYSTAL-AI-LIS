// re-c C# port — s215 CPU-overrun (lag frame) model for SubstPlay passes (= web/src/game/lagmodel.js).
// The port has no CPU, so it estimates each pass's ROM cycles from what it did (rd / wr / cpuRd counts per
// m.PassStage + exact m.Cyc deltas), with the linear fits of the s210-s214 study. The coefficients are fitted
// port constants (copied as literals from lagmodel.js), not ROM data. Arithmetic is double, in the JS order.
namespace ReC.Game;

public static class LagModel
{
    public const double Cpl = 341.0 / 3;

    // [rd, wr, rom, const] per stage / per AI id (hex); col = [colc, colh rd, colh wr, colh rom, const]
    private static readonly double[] CTop = { 7.5165, 7.3263, 10, 910.27 };
    private static readonly double[] CCam = { 4.1602, 10.1350, -3.8117, 147.433 };
    private static readonly double[] CSpr = { 5.8161, 9.1058, 15.0912, -791.159 };
    private static readonly double[] CCol = { 1.00101, 12.8345, 8.8081, -33.6003, 74.960 };
    private static readonly double[] CUpdRest = { -3.8021, 21.1948, -2.9417, -6.357 };
    private static readonly double[] CAiDefault = { 5.6430, 7.1299, -2.0833, 81.362 };
    public static readonly Dictionary<string, double[]> CAi = new()
    {
        ["01"] = new[] { -7.1061, 9.1957, 33.1511, 455.474 },
        ["03"] = new[] { 2.9947, 0.6810, 49.3337, 254.592 },
        ["05"] = new[] { 5.4621, 0, 51.4585, 0 },
        ["10"] = new[] { 5.4491, 9.5786, -30.9644, 193.332 },
        ["11"] = new[] { -0.9333, 16.7333, 0, 0 },
        ["1D"] = new[] { 8.7156, -4.8373, 27.7646, 83.599 },
        ["21"] = new[] { 3.3420, 11.2434, -9.2601, 92.470 },
        ["2A"] = new[] { 4.8995, 9.4120, -10.5512, 74.280 },
        ["2E"] = new[] { 3.9254, 11.0730, -12.2057, 145.090 },
        ["5C"] = new[] { 8.6247, 5.9928, -13.3392, 73.670 },
        ["5D"] = new[] { -14.4914, 25.1229, 115.3878, -595.622 },
        ["5E"] = new[] { 7.3571, 15.5179, 0, 0 },
        ["5F"] = new[] { 16.25, 0, 0, 0 },
        ["70"] = new[] { 4.7683, 13.3423, -14.8028, 0 },
        // s218 (irq_mode 7)
        ["1B"] = new[] { -11.5462, 29.6485, 1.6757, 0 },
        ["3F"] = new[] { 23.8571, 0, 0, 0 },
        ["62"] = new[] { 4.8190, 8.3093, -5.1978, 1829.689 },
        ["6B"] = new[] { -3.7938, 17.6505, -9.1679, 1615.807 },
        ["7F"] = new[] { 23.4286, 0, 0, 0 },
        // s219
        ["07"] = new[] { 15.0, 0, 0, 0 },
        ["34"] = new[] { 8, 11.6667, 0, 0 },
        ["40"] = new[] { 3.6636, 7.5108, 2.9856, 93.570 },
        ["4E"] = new[] { 7, -2.25, 26.1544, 82.941 },
        ["7B"] = new[] { 3.1, 5.4, 15.7, 83.4 },
        ["7D"] = new[] { 2.8216, 8.6151, 3.3821, 78.318 },
    };
    private static readonly double[] CHud = { 12.2281, -5.4246, 14.1850, 436.274 };
    private static readonly double[] CNmi = { 17.4678, -28.7772, 10.9464, 611.152 };
    private const int H0 = 202;                 // s211: IrqHandler0 (L142 split) cycles
    private const int HudLine = 189;            // s214: the HUD IRQ's line in the ints log (I189)
    private static readonly int[] Z = { 0, 0, 0 };

    // js: game/lagmodel.js dot
    private static double Dot(double[] c, int[] b) => c[0] * b[0] + c[1] * b[1] + c[2] * b[2] + c[3];

    private static int[] Cnt(Dictionary<string, int[]> cnt, string k) => cnt.TryGetValue(k, out var v) ? v : Z;

    public sealed class Parts
    {
        public double Top, Cam, Spr, Upd, Col;
    }

    // js: game/lagmodel.js passCycles
    /// <summary>ROM cycles of the pass held in cnt (stage -> [rd, wr, rom]) / cyc (stage -> m.Cyc delta), interrupts excluded</summary>
    public static double PassCycles(Dictionary<string, int[]> cnt, Dictionary<string, long> cyc, Parts? parts = null)
    {
        var p = parts ?? new Parts();
        p.Top = Dot(CTop, Cnt(cnt, "top")); p.Cam = Dot(CCam, Cnt(cnt, "cam")); p.Spr = Dot(CSpr, Cnt(cnt, "spr"));
        p.Upd = Dot(CUpdRest, Cnt(cnt, "upd")) + (cyc.TryGetValue("fl70", out long fl) ? fl : 0);   // s220: Ai70FlashIrq, exact
        var h = Cnt(cnt, "colh");
        p.Col = CCol[0] * (cyc.TryGetValue("col", out long cc) ? cc : 0) + CCol[1] * h[0] + CCol[2] * h[1] + CCol[3] * h[2] + CCol[4];
        foreach (var (k, v) in cnt)                       // insertion order, = JS object key order
        {
            if (k.Length < 2 || k[0] != 's' || k[1] > '9') continue;   // `sX:ID` dispatch buckets, not spr / snd
            string id = int.Parse(k[(k.IndexOf(':') + 1)..]).ToString("X2");
            p.Upd += Dot(CAi.TryGetValue(id, out var c) ? c : CAiDefault, v);
        }
        return p.Top + p.Cam + p.Spr + p.Upd + p.Col;
    }

    /// <summary>s218 / s220: NMI cycles over mode 0's per irq_mode (lagmodel.js NMI_MODE_EXTRA)</summary>
    public static readonly Dictionary<int, int> NmiModeExtra = new() { [7] = 27, [9] = 24, [2] = -99 };

    // js: game/lagmodel.js startLine
    /// <summary>start line of the next pass from this frame's `nmi` bucket (NMI 31:F3B6 length; extra = NmiModeExtra)</summary>
    public static int StartLine(int[]? nmiCnt, int extra = 0)
    {
        double n = Dot(CNmi, nmiCnt ?? Z) + extra;
        if (n < 1255) return 251;                                     // s220: mode-2 NMIs 1215 (L251) vs 1296 (L252)
        if (n < 1450) return 252;
        if (n < 1567) return 254;                                     // s220: 1519 / 1535 (L254) vs 1599 (L255)
        if (n < 1720) return 255;
        if (n < 1910) return 257;
        if (n < 2050) return 258;
        return 241 + (int)Math.Floor((n + 20) / Cpl);
    }

    // js: game/lagmodel.js withInts
    /// <summary>s215: pass cycles t plus the interrupts it runs into, from this frame's IRQ splits (H0 at its
    /// latch - 1, the HUD IRQ at I189 when the pass is still running there).</summary>
    private static (double T, bool Hud) WithInts(double t, List<IrqSplit> splits, int s)
    {
        bool hasHud = false;
        foreach (var q in splits)
        {
            if (q.Handler == 0) { hasHud = true; if (t > (262 - s + q.Latch - 1) * Cpl) t += H0; }
            else if (q.Handler == 1) hasHud = true;
        }
        return (t, hasHud && t > (262 - s + HudLine) * Cpl);
    }

    // js: game/lagmodel.js lagDomain
    /// <summary>irq_mode 0 (+ 7 with m.LagMode7, + 2 with m.LagMode2) passes are judged</summary>
    public static bool LagDomain(Machine m)
    {
        int im = m.Mem.Rd(0x51);
        return im == 0 || (m.LagMode7 && im == 7) || (m.LagMode2 && im == 2);
    }

    // js: game/lagmodel.js passLags
    /// <summary>true if the pass (started at line s) overruns the NMI at L241 of the next frame</summary>
    public static bool PassLags(Dictionary<string, int[]> cnt, Dictionary<string, long> cyc, int s, List<IrqSplit> splits) =>
        PassEstimate(cnt, cyc, s, splits).T > (503 - s) * Cpl + LagMargin;

    /// <summary>s215: the boundary offset (midpoint of the closest LAG / non-LAG margins)</summary>
    public const int LagMargin = -130;

    // js: game/lagmodel.js passEstimate
    /// <summary>(T: pass + interrupt cycles, Hud: HUD IRQ cycles inside T (0 if the pass ends before it), parts)</summary>
    public static (double T, double Hud, Parts Parts) PassEstimate(Dictionary<string, int[]> cnt, Dictionary<string, long> cyc,
        int s, List<IrqSplit> splits)
    {
        var parts = new Parts();
        var r = WithInts(PassCycles(cnt, cyc, parts), splits, s);
        double hud = r.Hud ? Dot(CHud, Cnt(cnt, "snd")) : 0;
        return (r.T + hud, hud, parts);
    }

    // js: game/lagmodel.js hudCycles
    /// <summary>HUD IRQ cost estimate of this frame's `snd` bucket</summary>
    public static double HudCycles(Dictionary<string, int[]> cnt) => Dot(CHud, Cnt(cnt, "snd"));

    // js: game/lagmodel.js lateSoundCheck
    /// <summary>s215: PlaySound 30:C125 inside a SubstPlay pass: counts queue entries added after the estimated
    /// L189 HUD point in m.SndLate (the HUD SoundUpdate then skips the command branch).</summary>
    public static void LateSoundCheck(Machine m)
    {
        if (!m.LagModelOn || m.LagStart != m.Frame || !LagDomain(m)) return;
        m.PassStage = m.PassStage;                                    // book the pending cpuRd / m.Cyc deltas
        if (WithInts(PassCycles(m.LagCnt, m.LagCyc), m.IrqSplits, m.LagLine).Hud) m.SndLate++;
    }
}
