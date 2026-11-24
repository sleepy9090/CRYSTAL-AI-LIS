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
namespace ReC.Win;

internal sealed class CdMixer
{
    public const int OutRate = 44100, Factor = 4, InRate = OutRate * Factor, Taps = 255;
    public bool Stereo = true, Reverb;
    public float Gain = 1.3f;

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
        _out.Add(ToPcm(yl * Gain));
        _out.Add(ToPcm(yr * Gain));
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
        Array.Clear(_hl); Array.Clear(_hr);
        _pos = _phase = 0;
        _dcxl = _dcyl = _dcxr = _dcyr = 0;
        _out.Clear();
    }
}

/// <summary>Freeverb-style reverb at 44.1 kHz: 4 damped feedback combs in parallel, then 2 all-passes (room ~0.6)</summary>
internal sealed class Freeverb
{
    private readonly float[][] _comb, _ap;
    private readonly int[] _ci, _ai;
    private readonly float[] _store;
    private const float Feedback = 0.80f, Damp = 0.35f, ApFeedback = 0.5f;

    public Freeverb(int spread)
    {
        int[] cl = { 1116, 1188, 1277, 1356 }, al = { 556, 441 };
        _comb = cl.Select(n => new float[n + spread]).ToArray();
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
