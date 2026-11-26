// re-c Windows host — "CD quality" audio (T13, Audio menu): 44.1 kHz 16-bit stereo from the APU's per-channel output
// (Apu.OutChannels). Not in the web port; Original (Audio.cs mono path) stays the exact port. Pure logic, unit-tested
// on Linux (tests/ReC.Tests/WinHostTests.cs).
//
//   1. the APU emits channel levels at InRate = 4 x 44.1 kHz (each one averaged over its 1/176400 s, so already
//      box-filtered over ~10 CPU cycles);
//   2. stereo: pulse 1 a little left, pulse 2 a little right, noise a little right, triangle + DMC in the centre;
//      each side goes through the NES's own non-linear mixer curves (pulse 95.88 / (8128 / x + 100), TND 163.67 /
//      (24329 / x + 100)), so the balance between channels stays as on the console. Stereo off = both sides equal;
//   3. a 255-tap Kaiser-windowed low-pass (20 kHz, ~80 dB stopband) while decimating by 4 to 44.1 kHz: no aliasing of
//      the square waves' harmonics (the original box average lets part of them fold back as hiss);
//   4. a 20 Hz DC blocker instead of the NES's 90 Hz + 440 Hz high-passes: the bass of the triangle comes through;
//   5. optional light reverb (Schroeder / Freeverb style: 4 combs + 2 all-passes per side, 12 % wet);
//   6. gain, a soft limiter above 0.8 of full scale, and TPDF dither to 16 bits (fixed seed: deterministic output).
// T28 Remaster (Audio > Remastered): the channels are kept apart (4 buses: pulse 1, pulse 2, triangle, noise + DMC,
// each through its own mixer curve and the same low-pass / decimation), then at 44.1 kHz: a chorus on each pulse (a
// copy delayed 6-10 ms, swept by a slow LFO, panned to the other side: wide and warm), the triangle smoothed (one-pole
// low-pass at 5 kHz takes the buzz of its 4-bit steps away) with a little tape-like saturation, noise + DMC slightly
// right; a room reverb with per-bus sends (drums most), low shelf +2.5 dB at 120 Hz and high shelf +1.5 dB at 7 kHz.
namespace ReC.Win;

internal sealed class CdMixer
{
    public const int OutRate = 44100, Factor = 4, InRate = OutRate * Factor, Taps = 255;
    public bool Stereo = true, Reverb;
    /// <summary>T28: the remastered sound (chorus, smooth bass, room, EQ) instead of the CD path</summary>
    public bool Remaster;
    public float Gain = 1.3f;
    /// <summary>T47: muted channels (bit 0 pulse 1, 1 pulse 2, 2 triangle, 3 noise, 4 DMC; Audio > Channels)</summary>
    public int Mute;
    /// <summary>T47: headphone crossfeed (AudioFx.cs)</summary>
    public bool Crossfeed;
    private readonly Crossfeed _xf = new();
    /// <summary>T50: Audio > Orchestrated (Orchestra.cs): instruments played from the chip state Chip() returns</summary>
    public bool Orchestra;
    public Func<ReC.Nes.Apu?>? Chip;
    private Orchestra _orc = new();
    private float _dmcAcc;
    private int _oph;

    // pan gains (left, right) per channel; the two sides add up to 2 so mono balance is unchanged
    private static readonly (float L, float R) PanP1 = (1.25f, 0.75f), PanP2 = (0.75f, 1.25f), PanNoise = (0.85f, 1.15f);

    private static readonly float[] Fir = MakeFir();
    private readonly float[] _hl = new float[Taps * 2], _hr = new float[Taps * 2];   // doubled ring: contiguous window
    private int _pos, _phase;
    private double _dcxl, _dcyl, _dcxr, _dcyr;
    private readonly double _dcA = 1 - 2 * Math.PI * 20.0 / OutRate;
    private readonly Freeverb _rvL = new(0), _rvR = new(23);
    private uint _rng = 0x12345678;
    private readonly List<short> _out = new();

    /// <summary>interleaved L R 16-bit samples produced since the last Take()</summary>
    public int Count => _out.Count;

    public List<short> Take(List<short> into)
    {
        into.AddRange(_out);
        _out.Clear();
        return into;
    }

    /// <summary>Kaiser-windowed sinc, cutoff 20 kHz at InRate, beta 8, unity DC gain</summary>
    private static float[] MakeFir()
    {
        var h = new double[Taps];
        double fc = 20000.0 / InRate, beta = 8.0, sum = 0;
        int m = Taps - 1;
        for (int i = 0; i < Taps; i++)
        {
            double n = i - m / 2.0;
            double sinc = n == 0 ? 2 * fc : Math.Sin(2 * Math.PI * fc * n) / (Math.PI * n);
            double r = 2.0 * i / m - 1;
            h[i] = sinc * Bessel0(beta * Math.Sqrt(1 - r * r)) / Bessel0(beta);
            sum += h[i];
        }
        return h.Select(v => (float)(v / sum)).ToArray();
    }

    private static double Bessel0(double x)
    {
        double s = 1, t = 1;
        for (int k = 1; k < 40; k++) { t *= x / (2 * k); s += t * t; }
        return s;
    }

    public static float PulseCurve(float x) => x <= 0 ? 0 : 95.88f / (8128f / x + 100f);
    public static float TndCurve(float x) => x <= 0 ? 0 : 163.67f / (24329f / x + 100f);

    /// <summary>one InRate sample of channel levels (the Apu.OutChannels sink)</summary>
    public void Push(float p1, float p2, float tri, float noise, float dmc)
    {
        if (Mute != 0)
        {
            if ((Mute & 1) != 0) p1 = 0;
            if ((Mute & 2) != 0) p2 = 0;
            if ((Mute & 4) != 0) tri = 0;
            if ((Mute & 8) != 0) noise = 0;
            if ((Mute & 16) != 0) dmc = 0;
        }
        if (Orchestra && Chip?.Invoke() is { } apu)
        {
            _dmcAcc += dmc;
            if (++_oph < Factor) return;
            _oph = 0;
            var (ol, or) = _orc.Render(apu, _dmcAcc / Factor, Mute);
            _dmcAcc = 0;
            Emit(ol * Gain, or * Gain);
            return;
        }
        if (Remaster) { PushRemaster(p1, p2, tri, noise, dmc); return; }
        float l, r;
        if (Stereo)
        {
            l = PulseCurve(p1 * PanP1.L + p2 * PanP2.L) + TndCurve(3 * tri + 2 * noise * PanNoise.L + dmc);
            r = PulseCurve(p1 * PanP1.R + p2 * PanP2.R) + TndCurve(3 * tri + 2 * noise * PanNoise.R + dmc);
        }
        else l = r = PulseCurve(p1 + p2) + TndCurve(3 * tri + 2 * noise + dmc);
        PushStereo(l, r);
    }

    /// <summary>one InRate stereo sample (mixer output, 0..~1) -> filtered / decimated / processed output</summary>
    public void PushStereo(float l, float r)
    {
        _hl[_pos] = _hl[_pos + Taps] = l;
        _hr[_pos] = _hr[_pos + Taps] = r;
        _pos = (_pos + 1) % Taps;
        if (++_phase < Factor) return;
        _phase = 0;
        // window = the last Taps samples, oldest first: _h[_pos .. _pos + Taps)
        double sl = 0, sr = 0;
        for (int k = 0; k < Taps; k++) { sl += Fir[k] * _hl[_pos + k]; sr += Fir[k] * _hr[_pos + k]; }
        // DC blocker (20 Hz)
        double yl = sl - _dcxl + _dcA * _dcyl; _dcxl = sl; _dcyl = yl;
        double yr = sr - _dcxr + _dcA * _dcyr; _dcxr = sr; _dcyr = yr;
        if (Reverb)
        {
            double mono = (yl + yr) * 0.5;
            yl = yl * 0.88 + _rvL.Process(mono) * 0.12;
            yr = yr * 0.88 + _rvR.Process(mono) * 0.12;
        }
        Emit(yl * Gain, yr * Gain);
    }

    // ---- T28 remaster
    private readonly float[][] _bus = Enumerable.Range(0, 4).Select(_ => new float[Taps * 2]).ToArray();
    private readonly float[] _d1 = new float[1024], _d2 = new float[1024];      // chorus delay lines (23 ms)
    private int _dpos, _rn;
    private double _triLp, _ldc, _ldcY, _rdc, _rdcY;
    private readonly Biquad _lsL = Biquad.LowShelf(120, 2.5), _lsR = Biquad.LowShelf(120, 2.5),
        _hsL = Biquad.HighShelf(7000, 1.5), _hsR = Biquad.HighShelf(7000, 1.5);
    private readonly Freeverb _rmL = new(0), _rmR = new(23);
    private const double TriA = 0.5076;                         // one-pole at 5 kHz: 1 - exp(-2 pi 5000 / 44100)

    private static double Dot(float[] h, int o)
    {
        double s = 0;
        for (int k = 0; k < Taps; k++) s += Fir[k] * h[o + k];
        return s;
    }

    private readonly float[] _v4 = new float[4];

    private void PushRemaster(float p1, float p2, float tri, float noise, float dmc)
    {
        var v = _v4;                                              // no allocation per sample
        v[0] = PulseCurve(p1); v[1] = PulseCurve(p2); v[2] = TndCurve(3 * tri); v[3] = TndCurve(2 * noise + dmc);
        for (int b = 0; b < 4; b++) _bus[b][_pos] = _bus[b][_pos + Taps] = v[b];
        _pos = (_pos + 1) % Taps;
        if (++_phase < Factor) return;
        _phase = 0;
        double b1 = Dot(_bus[0], _pos), b2 = Dot(_bus[1], _pos), bt = Dot(_bus[2], _pos), bn = Dot(_bus[3], _pos);
        // chorus: delayed copies swept 6-10 ms by 0.5 / 0.37 Hz LFOs (linear interpolation)
        _d1[_dpos] = (float)b1; _d2[_dpos] = (float)b2;
        double t = _rn++ / (double)OutRate;
        double c1 = Tap(_d1, 0.008 + 0.002 * Math.Sin(2 * Math.PI * 0.5 * t)), c2 = Tap(_d2, 0.008 + 0.002 * Math.Sin(2 * Math.PI * 0.37 * t + 1.7));
        _dpos = (_dpos + 1) & 1023;
        // triangle: smooth + warm
        _triLp += (bt - _triLp) * TriA;
        double tr = Math.Tanh(_triLp * 2.0) / 2.0;
        double l = b1 * 1.05 + b2 * 0.40 + c2 * 0.60 + tr + bn * 0.92;
        double r = b2 * 1.05 + b1 * 0.40 + c1 * 0.60 + tr + bn * 1.08;
        // DC blocker
        double yl = l - _ldc + _dcA * _ldcY; _ldc = l; _ldcY = yl;
        double yr = r - _rdc + _dcA * _rdcY; _rdc = r; _rdcY = yr;
        // room: sends per bus (drums most)
        double send = (b1 + b2 + (c1 + c2) * 0.5) * 0.10 + tr * 0.04 + bn * 0.22;
        yl += _rmL.Process(send) * 0.2;
        yr += _rmR.Process(send) * 0.2;
        yl = _hsL.Process(_lsL.Process(yl));
        yr = _hsR.Process(_lsR.Process(yr));
        Emit(yl * Gain * 0.45, yr * Gain * 0.45);
    }

    /// <summary>delay line read `sec` seconds back, linear interpolation</summary>
    private double Tap(float[] d, double sec)
    {
        double back = sec * OutRate;
        int i = (int)back;
        double f = back - i;
        float a = d[(_dpos - i) & 1023], b = d[(_dpos - i - 1) & 1023];
        return a + (b - a) * f;
    }

    private void Emit(double l, double r)
    {
        if (Crossfeed) (l, r) = _xf.Process(l, r);
        _out.Add(ToPcm(l));
        _out.Add(ToPcm(r));
    }

    /// <summary>soft limiter above 0.8 FS, TPDF dither (+-1 LSB), round to 16 bits</summary>
    private short ToPcm(double x)
    {
        double a = Math.Abs(x);
        if (a > 0.8) x = Math.Sign(x) * (0.8 + 0.2 * Math.Tanh((a - 0.8) / 0.2));
        double d = (Rand() - Rand()) / 32768.0;               // triangular, +-1 LSB
        return (short)Math.Clamp(Math.Round((x + d) * 32767), -32768, 32767);
    }

    private double Rand()
    {
        _rng ^= _rng << 13; _rng ^= _rng >> 17; _rng ^= _rng << 5;   // xorshift32
        return (_rng & 0xFFFF) / 65536.0;
    }

    public void Clear()
    {
        foreach (var b in _bus) Array.Clear(b);
        Array.Clear(_d1); Array.Clear(_d2);
        _dpos = _rn = 0; _triLp = _ldc = _ldcY = _rdc = _rdcY = 0;
        Array.Clear(_hl); Array.Clear(_hr);
        _pos = _phase = 0;
        _dcxl = _dcyl = _dcxr = _dcyr = 0;
        _xf.Clear();
        _orc = new Orchestra(); _dmcAcc = 0; _oph = 0;
        _out.Clear();
    }
}

/// <summary>RBJ shelving biquad at 44.1 kHz (T28 remaster EQ)</summary>
internal sealed class Biquad
{
    private readonly double _b0, _b1, _b2, _a1, _a2;
    private double _x1, _x2, _y1, _y2;

    private Biquad(double b0, double b1, double b2, double a0, double a1, double a2)
    {
        _b0 = b0 / a0; _b1 = b1 / a0; _b2 = b2 / a0; _a1 = a1 / a0; _a2 = a2 / a0;
    }

    private static Biquad Shelf(double hz, double db, bool high)
    {
        double a = Math.Pow(10, db / 40), w = 2 * Math.PI * hz / CdMixer.OutRate, cs = Math.Cos(w), al = Math.Sin(w) / 2 * Math.Sqrt(2);
        double sa = 2 * Math.Sqrt(a) * al;
        return high
            ? new Biquad(a * ((a + 1) + (a - 1) * cs + sa), -2 * a * ((a - 1) + (a + 1) * cs), a * ((a + 1) + (a - 1) * cs - sa),
                (a + 1) - (a - 1) * cs + sa, 2 * ((a - 1) - (a + 1) * cs), (a + 1) - (a - 1) * cs - sa)
            : new Biquad(a * ((a + 1) - (a - 1) * cs + sa), 2 * a * ((a - 1) - (a + 1) * cs), a * ((a + 1) - (a - 1) * cs - sa),
                (a + 1) + (a - 1) * cs + sa, -2 * ((a - 1) + (a + 1) * cs), (a + 1) + (a - 1) * cs - sa);
    }

    public static Biquad LowShelf(double hz, double db) => Shelf(hz, db, false);
    public static Biquad HighShelf(double hz, double db) => Shelf(hz, db, true);

    public double Process(double x)
    {
        double y = _b0 * x + _b1 * _x1 + _b2 * _x2 - _a1 * _y1 - _a2 * _y2;
        _x2 = _x1; _x1 = x; _y2 = _y1; _y1 = y;
        return y;
    }

    /// <summary>gain at hz (for tests)</summary>
    public double GainAt(double hz)
    {
        double w = 2 * Math.PI * hz / CdMixer.OutRate;
        System.Numerics.Complex z = System.Numerics.Complex.FromPolarCoordinates(1, -w), z2 = z * z;
        return ((_b0 + _b1 * z + _b2 * z2) / (1 + _a1 * z + _a2 * z2)).Magnitude;
    }
}

/// <summary>Freeverb-style reverb at 44.1 kHz: 4 damped feedback combs in parallel, then 2 all-passes (room ~0.6)</summary>
internal sealed class Freeverb
{
    private readonly float[][] _comb, _ap;
    private readonly int[] _ci, _ai;
    private readonly float[] _store;
    private readonly float Feedback, Damp;
    private const float ApFeedback = 0.5f;

    /// <summary>size scales the comb lengths (T50 hall: 1.45 with feedback 0.86)</summary>
    public Freeverb(int spread, float feedback = 0.80f, double size = 1.0, float damp = 0.35f)
    {
        Feedback = feedback; Damp = damp;
        int[] cl = { 1116, 1188, 1277, 1356 }, al = { 556, 441 };
        _comb = cl.Select(n => new float[(int)(n * size) + spread]).ToArray();
        _ap = al.Select(n => new float[n + spread]).ToArray();
        _ci = new int[_comb.Length]; _ai = new int[_ap.Length]; _store = new float[_comb.Length];
    }

    public double Process(double input)
    {
        float x = (float)input * 0.25f, o = 0;
        for (int c = 0; c < _comb.Length; c++)
        {
            var b = _comb[c];
            float y = b[_ci[c]];
            _store[c] = y * (1 - Damp) + _store[c] * Damp;
            b[_ci[c]] = x + _store[c] * Feedback;
            _ci[c] = (_ci[c] + 1) % b.Length;
            o += y;
        }
        for (int a = 0; a < _ap.Length; a++)
        {
            var b = _ap[a];
            float bo = b[_ai[a]];
            b[_ai[a]] = o + bo * ApFeedback;
            o = bo - o;
            _ai[a] = (_ai[a] + 1) % b.Length;
        }
        return o;
    }
}
