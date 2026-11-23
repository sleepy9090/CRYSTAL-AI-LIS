// re-c C# port — NES 2A03 APU (= web/src/nes/apu.js, Ph7 web 7/8, s57). Register-level model of the five channels,
// driven by the writes of the ported sound driver (Game/Sound/Driver.cs via Machine.ApuWrite). I/O-free; the SDL host
// feeds the samples to the audio device.
//
//   var apu = new Apu(dmcRead);           dmcRead(addr) = CPU read for DMC fetches ($C000-$FFFF, wraps to $8000)
//   apu.Write(addr, v) / apu.Read(0x4015)
//   apu.Run(cpuCycles)                    advance; when apu.SampleRate > 0 the samples go to apu.Out (one double each)
//
// Numerics follow the JS exactly: the mixer tables are float32 (Float32Array), every sum / filter step is double.
// Without a sample rate (headless runs) only the parts that $4015 can see are advanced: the frame sequencer
// (length counters) and the DMC byte fetches, event to event.
namespace ReC.Nes;

// js: nes/apu.js Envelope
public sealed class Envelope
{
    public bool Start, Loop, Constant;
    public int Div, Decay, Vol;

    // js: nes/apu.js constructor (Envelope)
    public Envelope() { Start = false; Div = 0; Decay = 0; Loop = false; Constant = false; Vol = 0; }

    // js: nes/apu.js clock
    public void Clock()
    {
        if (Start) { Start = false; Decay = 15; Div = Vol; return; }
        if (Div > 0) { Div--; return; }
        Div = Vol;
        if (Decay > 0) Decay--; else if (Loop) Decay = 15;
    }

    // js: nes/apu.js out (Envelope)
    public int Out => Constant ? Vol : Decay;
}

// js: nes/apu.js Pulse
public sealed class Pulse
{
    public readonly bool Ones;
    public readonly Envelope Env = new();
    public bool Enabled, SwEn, SwNeg, SwReload;
    public int Len, Duty, Step, Period, Timer, SwPer, SwShift, SwDiv;

    // js: nes/apu.js constructor (Pulse)
    public Pulse(bool ones) { Ones = ones; }

    // js: nes/apu.js write (Pulse)
    public void Write(int r, int v)
    {
        switch (r)
        {
            case 0: Duty = v >> 6; Env.Loop = (v & 0x20) != 0; Env.Constant = (v & 0x10) != 0; Env.Vol = v & 15; break;
            case 1: SwEn = (v & 0x80) != 0; SwPer = (v >> 4) & 7; SwNeg = (v & 8) != 0; SwShift = v & 7; SwReload = true; break;
            case 2: Period = (Period & 0x700) | v; break;
            case 3:
                Period = (Period & 0xFF) | ((v & 7) << 8);
                if (Enabled) Len = Apu.Length[v >> 3];
                Step = 0; Env.Start = true; break;
        }
    }

    // js: nes/apu.js target
    public int Target()
    {
        int c = Period >> SwShift;
        return SwNeg ? Period - c - (Ones ? 1 : 0) : Period + c;
    }

    // js: nes/apu.js muted
    public bool Muted() => Period < 8 || Target() > 0x7FF;

    // js: nes/apu.js clockSweep
    public void ClockSweep()
    {
        if (SwDiv == 0 && SwEn && SwShift != 0 && !Muted()) Period = Math.Max(0, Target());
        if (SwDiv == 0 || SwReload) { SwDiv = SwPer; SwReload = false; } else SwDiv--;
    }

    // js: nes/apu.js clockLen (Pulse)
    public void ClockLen() { if (Len != 0 && !Env.Loop) Len--; }

    // js: nes/apu.js tick (Pulse)
    /// <summary>one APU cycle (2 CPU cycles)</summary>
    public void Tick()
    {
        if (Timer == 0) { Timer = Period; Step = (Step + 1) & 7; } else Timer--;
    }

    // js: nes/apu.js out (Pulse)
    public int Out => (Len != 0 && !Muted() && Apu.DutySeq[Duty, Step] != 0) ? Env.Out : 0;
}

// js: nes/apu.js Triangle
public sealed class Triangle
{
    public bool Enabled, Ctrl, LinReload;
    public int Len, LinLoad, Lin, Period, Timer, Step;

    // js: nes/apu.js constructor (Triangle)
    public Triangle() { }

    // js: nes/apu.js write (Triangle)
    public void Write(int r, int v)
    {
        switch (r)
        {
            case 0: Ctrl = (v & 0x80) != 0; LinLoad = v & 0x7F; break;
            case 2: Period = (Period & 0x700) | v; break;
            case 3:
                Period = (Period & 0xFF) | ((v & 7) << 8);
                if (Enabled) Len = Apu.Length[v >> 3];
                LinReload = true; break;
        }
    }

    // js: nes/apu.js clockLinear
    public void ClockLinear()
    {
        if (LinReload) Lin = LinLoad; else if (Lin != 0) Lin--;
        if (!Ctrl) LinReload = false;
    }

    // js: nes/apu.js clockLen (Triangle)
    public void ClockLen() { if (Len != 0 && !Ctrl) Len--; }

    // js: nes/apu.js tick (Triangle)
    public void Tick()
    {
        if (Timer == 0)
        {
            Timer = Period;
            if (Len != 0 && Lin != 0 && Period >= 2) Step = (Step + 1) & 31;
        }
        else Timer--;
    }

    // js: nes/apu.js out (Triangle)
    public int Out => Apu.Tri[Step];
}

// js: nes/apu.js Noise
public sealed class Noise
{
    public readonly Envelope Env = new();
    public bool Enabled, Mode;
    public int Len, Period = 4, Timer, Sr = 1;

    // js: nes/apu.js constructor (Noise)
    public Noise() { }

    // js: nes/apu.js write (Noise)
    public void Write(int r, int v)
    {
        switch (r)
        {
            case 0: Env.Loop = (v & 0x20) != 0; Env.Constant = (v & 0x10) != 0; Env.Vol = v & 15; break;
            case 2: Mode = (v & 0x80) != 0; Period = Apu.NoisePeriod[v & 15]; break;
            case 3: if (Enabled) Len = Apu.Length[v >> 3]; Env.Start = true; break;
        }
    }

    // js: nes/apu.js clockLen (Noise)
    public void ClockLen() { if (Len != 0 && !Env.Loop) Len--; }

    // js: nes/apu.js tick (Noise)
    /// <summary>CPU cycle (the periods are in CPU cycles)</summary>
    public void Tick()
    {
        if (Timer == 0)
        {
            Timer = Period - 1;
            int fb = (Sr ^ (Sr >> (Mode ? 6 : 1))) & 1;
            Sr = (Sr >> 1) | (fb << 14);
        }
        else Timer--;
    }

    // js: nes/apu.js out (Noise)
    public int Out => (Len != 0 && (Sr & 1) == 0) ? Env.Out : 0;
}

// js: nes/apu.js Dmc
public sealed class Dmc
{
    private readonly Func<int, int> _read;
    public bool IrqEn, Loop, Silent = true, Irq;
    public int Rate, Timer, Level, AddrLoad = 0xC000, LenLoad = 1, Addr = 0xC000, Remain, Buf = -1, Shift, Bits = 8;

    // js: nes/apu.js constructor (Dmc)
    public Dmc(Func<int, int> read) { _read = read; Rate = Apu.DmcRate[0]; Timer = Apu.DmcRate[0] - 1; }

    // js: nes/apu.js write (Dmc)
    public void Write(int r, int v)
    {
        switch (r)
        {
            case 0: IrqEn = (v & 0x80) != 0; Loop = (v & 0x40) != 0; Rate = Apu.DmcRate[v & 15]; if (!IrqEn) Irq = false; break;
            case 1: Level = v & 0x7F; break;
            case 2: AddrLoad = 0xC000 | (v << 6); break;
            case 3: LenLoad = (v << 4) | 1; break;
        }
    }

    // js: nes/apu.js restart
    public void Restart() { Addr = AddrLoad; Remain = LenLoad; }

    // js: nes/apu.js fetch
    public void Fetch()
    {
        if (Buf >= 0 || Remain == 0) return;
        Buf = _read(Addr);
        Addr = Addr == 0xFFFF ? 0x8000 : Addr + 1;
        if (--Remain == 0)
        {
            if (Loop) Restart(); else if (IrqEn) Irq = true;
        }
    }

    // js: nes/apu.js clockOutput
    /// <summary>one output-unit clock (every `rate` CPU cycles)</summary>
    public void ClockOutput()
    {
        if (!Silent)
        {
            if ((Shift & 1) != 0) { if (Level <= 125) Level += 2; } else if (Level >= 2) Level -= 2;
        }
        Shift >>= 1;
        if (--Bits == 0)
        {
            Bits = 8;
            if (Buf < 0) Silent = true; else { Silent = false; Shift = Buf; Buf = -1; }
            Fetch();
        }
    }

    // js: nes/apu.js tick (Dmc)
    public void Tick() { if (Timer == 0) { Timer = Rate - 1; ClockOutput(); } else Timer--; }
}

// js: nes/apu.js Apu
public sealed class Apu
{
    /// <summary>= apu.js CPU_HZ (a constant, not an inventory callable)</summary>
    public const int CpuHz = 1789773;
    internal static readonly int[] Length = { 10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14,
        12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30 };
    internal static readonly int[,] DutySeq = { { 0, 1, 0, 0, 0, 0, 0, 0 }, { 0, 1, 1, 0, 0, 0, 0, 0 },
        { 0, 1, 1, 1, 1, 0, 0, 0 }, { 1, 0, 0, 1, 1, 1, 1, 1 } };
    internal static readonly int[] Tri = { 15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0,
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15 };
    internal static readonly int[] NoisePeriod = { 4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068 };
    internal static readonly int[] DmcRate = { 428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54 };
    private static readonly int[] Seq4 = { 7457, 14913, 22371, 29829 }, Seq5 = { 7457, 14913, 22371, 37281 };
    // Float32Array tables in the JS: computed in double, stored as float32
    private static readonly float[] PulseMix = MakeMix(31, 95.88, 8128);
    private static readonly float[] TndMix = MakeMix(203, 163.67, 24329);   // standard lookup

    private static float[] MakeMix(int n, double k, double d)
    {
        var t = new float[n];
        for (int i = 1; i < n; i++) t[i] = (float)(k / (d / i + 100));
        return t;
    }

    public readonly Pulse P1 = new(true), P2 = new(false);
    public readonly Triangle TriCh = new();
    public readonly Noise NoiseCh = new();
    public readonly Dmc DmcCh;
    public bool Mode5, IrqInhibit, FrameIrq;
    public int SeqCycle;
    /// <summary>CPU cycles since power-on (parity = APU cycle phase)</summary>
    public long Cycle;
    public int SampleRate;
    public Action<double>? Out;
    private double _acc, _frac, _step;
    private int _n;
    private double _hp1x, _hp1y, _hp2x, _hp2y, _lp;
    private double _a90, _a440, _b14k;

    // js: nes/apu.js constructor (Apu)
    public Apu(Func<int, int>? dmcRead = null) { DmcCh = new Dmc(dmcRead ?? (_ => 0)); }

    // js: nes/apu.js setOutput
    /// <summary>Output on: samples at `rate` Hz are passed to `sink`.</summary>
    public void SetOutput(int rate, Action<double>? sink)
    {
        SampleRate = rate; Out = sink;
        _step = (double)CpuHz / rate; _frac = 0; _acc = 0; _n = 0;
        double dt = 1.0 / rate;
        // js: nes/apu.js rc
        static double Rc(double f) => 1 / (2 * Math.PI * f);
        _a90 = Rc(90) / (Rc(90) + dt); _a440 = Rc(440) / (Rc(440) + dt); _b14k = dt / (Rc(14000) + dt);
    }

    // js: nes/apu.js write (Apu)
    public void Write(int a, int v)
    {
        v &= 0xFF;
        if (a < 0x4004) P1.Write(a & 3, v);
        else if (a < 0x4008) P2.Write(a & 3, v);
        else if (a < 0x400C) TriCh.Write(a & 3, v);
        else if (a < 0x4010) NoiseCh.Write(a & 3, v);
        else if (a < 0x4014) DmcCh.Write(a & 3, v);
        else if (a == 0x4015)
        {
            P1.Enabled = (v & 1) != 0; if (!P1.Enabled) P1.Len = 0;
            P2.Enabled = (v & 2) != 0; if (!P2.Enabled) P2.Len = 0;
            TriCh.Enabled = (v & 4) != 0; if (!TriCh.Enabled) TriCh.Len = 0;
            NoiseCh.Enabled = (v & 8) != 0; if (!NoiseCh.Enabled) NoiseCh.Len = 0;
            DmcCh.Irq = false;
            if ((v & 0x10) == 0) DmcCh.Remain = 0;
            else if (DmcCh.Remain == 0) { DmcCh.Restart(); DmcCh.Fetch(); }
        }
        else if (a == 0x4017)
        {
            Mode5 = (v & 0x80) != 0; IrqInhibit = (v & 0x40) != 0;
            if (IrqInhibit) FrameIrq = false;
            SeqCycle = 0;
            if (Mode5) { Quarter(); Half(); }
        }
    }

    // js: nes/apu.js read
    public int Read(int a)
    {
        if (a != 0x4015) return 0;
        int v = (P1.Len != 0 ? 1 : 0) | (P2.Len != 0 ? 2 : 0) | (TriCh.Len != 0 ? 4 : 0) | (NoiseCh.Len != 0 ? 8 : 0) |
            (DmcCh.Remain != 0 ? 0x10 : 0) | (FrameIrq ? 0x40 : 0) | (DmcCh.Irq ? 0x80 : 0);
        FrameIrq = false;
        return v;
    }

    // js: nes/apu.js _quarter
    private void Quarter() { P1.Env.Clock(); P2.Env.Clock(); NoiseCh.Env.Clock(); TriCh.ClockLinear(); }

    // js: nes/apu.js _half
    private void Half() { P1.ClockLen(); P2.ClockLen(); TriCh.ClockLen(); NoiseCh.ClockLen(); P1.ClockSweep(); P2.ClockSweep(); }

    // js: nes/apu.js _seqEvent
    private void SeqEvent(int i)
    {
        Quarter();
        if (i == 1 || i == 3) Half();
        if (i == 3 && !Mode5 && !IrqInhibit) FrameIrq = true;
    }

    // js: nes/apu.js _seqTick
    /// <summary>One CPU cycle of the frame sequencer: fire the event at this count, then count (wrap after the last).</summary>
    private void SeqTick()
    {
        var s = Mode5 ? Seq5 : Seq4;
        int c = SeqCycle;
        if (c == s[0]) SeqEvent(0);
        else if (c == s[1]) SeqEvent(1);
        else if (c == s[2]) SeqEvent(2);
        else if (c == s[3]) { SeqEvent(3); SeqCycle = 0; return; }
        SeqCycle = c + 1;
    }

    // js: nes/apu.js _seqRun
    /// <summary>n CPU cycles of the frame sequencer, event to event (same result as n SeqTick calls).</summary>
    private void SeqRun(int n)
    {
        var s = Mode5 ? Seq5 : Seq4;
        int c = SeqCycle;
        while (n > 0)
        {
            int i = 0;
            while (s[i] < c) i++;
            int d = s[i] - c;
            if (d >= n) { c += n; break; }
            n -= d + 1;
            SeqEvent(i);
            c = i == 3 ? 0 : s[i] + 1;
        }
        SeqCycle = c;
    }

    // js: nes/apu.js run
    public void Run(int n)
    {
        if (SampleRate == 0)
        {                                                // headless: sequencer + DMC fetches only
            SeqRun(n);
            int k = n;
            var d = DmcCh;
            while (k > 0)
            {
                if (d.Timer >= k) { d.Timer -= k; break; }
                k -= d.Timer + 1; d.Timer = d.Rate - 1; d.ClockOutput();
            }
            Cycle += n;
            return;
        }
        Pulse p1 = P1, p2 = P2;
        Triangle tri = TriCh;
        Noise noise = NoiseCh;
        Dmc dmc = DmcCh;
        for (int i = 0; i < n; i++)
        {
            SeqTick();
            if ((Cycle & 1) != 0) { p1.Tick(); p2.Tick(); }
            tri.Tick(); noise.Tick(); dmc.Tick();
            Cycle++;
            _acc += (double)PulseMix[p1.Out + p2.Out] + (double)TndMix[3 * tri.Out + 2 * noise.Out + dmc.Level];
            _n++;
            if (++_frac >= _step) { _frac -= _step; Emit(); }
        }
    }

    // js: nes/apu.js _emit
    private void Emit()
    {
        double x = _acc / _n; _acc = 0; _n = 0;
        _hp1y = _a90 * (_hp1y + x - _hp1x); _hp1x = x;
        _hp2y = _a440 * (_hp2y + _hp1y - _hp2x); _hp2x = _hp1y;
        _lp += _b14k * (_hp2y - _lp);
        Out?.Invoke(_lp);
    }
}
