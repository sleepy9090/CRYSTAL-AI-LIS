// re-c Windows host — Audio > Orchestrated (T50). Not in the web port; pure logic, unit-tested (win_orchestra).
//
// The game's music is re-played on synthesized instruments instead of the chip's waves. Every output sample (44.1 kHz)
// reads the notes straight from the APU channel state (Apu.P1 / P2 / TriCh / NoiseCh: period -> pitch, envelope ->
// loudness, $4003 writes -> note starts); nothing in the core changes and the game's own mono stream is untouched.
//
//   pulse 1  -> violins: 3 detuned band-limited saws (+-7 cents, spread L / C / R), low-pass following the pitch,
//               25 ms bow attack, 180 ms release, delayed vibrato (5.6 Hz)
//   pulse 2  -> french horns: saw + square, darker low-pass, 45 ms attack, slower vibrato
//   triangle -> string basses: saw through a low-pass + a sine an octave down for weight
//   noise    -> percussion: low noise pitches = timpani (a pitched sine thump + rumble), high = snare / cymbal
//               (band-passed noise following the game's envelope)
//   DMC      -> the original samples (centred, DC blocked)
// Sound effects keep their chip sound: a pulse with its sweep unit on (jumps, hits, the sword) or changing pitch
// faster than music does is played as a clean band-limited square at its own duty. Seated like an orchestra
// (violins left, horns right, basses and timpani in the middle), then a concert-hall reverb (20 ms pre-delay, ~2 s
// tail), low shelf +2 dB / high shelf -1.5 dB (warmer than the chip).
using ReC.Nes;

namespace ReC.Win;

internal sealed class Orchestra
{
    private const double Fs = CdMixer.OutRate, Cpu = Apu.CpuHz;
    private readonly Voice _vn = new(Kind.Violins), _hn = new(Kind.Horns), _bs = new(Kind.Bass);
    private readonly Perc _pc = new();
    private double _dmcX, _dmcY;
    private double _dmcLp;
    private readonly float[] _pre = new float[2048];
    private int _pi;
    private readonly Freeverb _hallL = new(0, 0.86f, 1.45), _hallR = new(23, 0.86f, 1.45);
    private readonly Biquad _lsL = Biquad.LowShelf(150, 2), _lsR = Biquad.LowShelf(150, 2),
        _hsL = Biquad.HighShelf(6000, -1.5), _hsR = Biquad.HighShelf(6000, -1.5);
    public const double Level = 0.62;

    /// <summary>one 44.1 kHz stereo sample from the chip's state now; dmc = DMC output level (0..127); mute = CdMixer.Mute</summary>
    public (double L, double R) Render(Apu apu, float dmc, int mute)
    {
        var (v, vp) = (mute & 1) != 0 ? (0.0, 0.0) : _vn.Pulse(apu.P1);
        var (h, hp) = (mute & 2) != 0 ? (0.0, 0.0) : _hn.Pulse(apu.P2);
        var (b, _) = (mute & 4) != 0 ? (0.0, 0.0) : _bs.Tri(apu.TriCh);
        var (tp, sn) = (mute & 8) != 0 ? (0.0, 0.0) : _pc.Next(apu.NoiseCh);
        double d = (mute & 16) != 0 ? 0 : CdMixer.TndCurve(dmc);
        _dmcLp += (d - _dmcLp) * 0.68;                               // the samples low-passed ~8 kHz (their 1-bit steps)
        d = _dmcLp;
        double dy = d - _dmcX + 0.9972 * _dmcY; _dmcX = d; _dmcY = dy;
        // balance: each instrument at about its chip channel's loudness (measured on music $14 solo, CD path)
        v *= 0.5; vp *= 0.5; h *= 0.37; hp *= 0.37; b *= 0.45; tp *= 0.45; sn *= 0.45; dy *= 2.5;
        // seating: v (violins) and vp (their spread) left, h right, bass + timpani centre
        double l = v * 0.62 + vp * 0.25 + h * 0.32 + hp * 0.1 + b * 0.5 + tp * 0.55 + sn * 0.45 + dy * 0.5;
        double r = v * 0.32 - vp * 0.25 + h * 0.62 + hp * 0.1 + b * 0.5 + tp * 0.45 + sn * 0.55 + dy * 0.5;
        // concert hall: 20 ms pre-delay, more send from the strings and drums
        double send = v * 0.32 + h * 0.3 + b * 0.16 + tp * 0.35 + sn * 0.3;
        _pre[_pi] = (float)send;
        double ds = _pre[(_pi - 882) & 2047];
        _pi = (_pi + 1) & 2047;
        l += _hallL.Process(ds) * 0.55;
        r += _hallR.Process(ds) * 0.55;
        l = _hsL.Process(_lsL.Process(l));
        r = _hsR.Process(_lsR.Process(r));
        return (l * Level, r * Level);
    }

    public enum Kind { Violins, Horns, Bass }

    /// <summary>TPT state-variable filter (low-pass / band-pass)</summary>
    private struct Svf
    {
        private double _a, _b;
        public double Lp, Bp;

        public void Run(double x, double hz, double q)
        {
            double g = Math.Tan(Math.PI * Math.Min(hz, Fs * 0.45) / Fs), k = 1 / q;
            double v1 = (_a + g * (x - _b)) / (1 + g * (g + k)), v2 = _b + g * v1;
            _a = 2 * v1 - _a; _b = 2 * v2 - _b;
            Lp = v2; Bp = v1;
        }
    }

    public static double PolyBlep(double t, double dt)
    {
        if (t < dt) { t /= dt; return t + t - t * t - 1; }
        if (t > 1 - dt) { t = (t - 1) / dt; return t * t + t + t + 1; }
        return 0;
    }

    /// <summary>band-limited saw, phase in [0, 1)</summary>
    public static double Saw(ref double ph, double dt)
    {
        ph += dt;
        if (ph >= 1) ph -= 1;
        return 2 * ph - 1 - PolyBlep(ph, dt);
    }

    /// <summary>band-limited pulse of the given duty</summary>
    public static double Square(ref double ph, double dt, double duty)
    {
        ph += dt;
        if (ph >= 1) ph -= 1;
        double t2 = ph + 1 - duty; if (t2 >= 1) t2 -= 1;
        return (ph < duty ? 1 : -1) + PolyBlep(ph, dt) - PolyBlep(t2, dt) - (2 * duty - 1);
    }

    private sealed class Voice
    {
        private readonly Kind _k;
        private readonly double _att, _rel;
        private double _amp, _logF = -1, _age, _p0, _p1, _p2, _p3, _vib;
        private double _ampFx, _pf;
        private static readonly double[] Duties = { 0.125, 0.25, 0.5, 0.75 };
        private bool _wasStart, _wasOn, _fx;
        private int _prevPer = -1, _fxHold;
        private Svf _f, _f2;
        private readonly double[] _det;

        public Voice(Kind k)
        {
            _k = k;
            (_att, _rel) = k switch { Kind.Violins => (0.025, 0.18), Kind.Horns => (0.045, 0.2), _ => (0.012, 0.12) };
            _det = k == Kind.Violins ? new[] { Math.Pow(2, -7 / 1200.0), 1.0, Math.Pow(2, 7 / 1200.0) } : new[] { 1.0, Math.Pow(2, 4 / 1200.0), 1.0 };
            _p1 = 0.33; _p2 = 0.67;
        }

        private static double Coef(double sec) => 1 - Math.Exp(-1 / (sec * Fs));

        /// <summary>follow the target pitch / loudness; note = a new note started</summary>
        private void Track(bool on, double hz, double vol, bool note)
        {
            double lf = Math.Log(hz);
            if (note || _logF < 0) { _logF = lf; _age = 0; }
            else _logF += (lf - _logF) * Coef(0.003);
            _age += 1 / Fs;
            double target = on ? vol : 0;
            _amp += (target - _amp) * Coef(target > _amp ? _att : _rel);
        }

        public (double Main, double Spread) Pulse(Pulse p)
        {
            bool on = p.Len != 0 && !p.Muted();
            double hz = Cpu / (16.0 * (p.Period + 1));
            bool start = p.Env.Start && !_wasStart;
            _wasStart = p.Env.Start;
            // a sound effect: sweep on, or the period jumping around faster than music (2+ big changes within 50 ms)
            if (p.Period != _prevPer && _prevPer >= 0 && Math.Abs(Math.Log((p.Period + 1.0) / (_prevPer + 1.0))) > 0.045)   // above the driver's vibrato (<= ~2 %)
                _fxHold += 2205 / 2;
            _prevPer = p.Period;
            _fxHold = Math.Max(0, _fxHold - 1);
            bool fx = on && ((p.SwEn && p.SwShift > 0) || _fxHold > 2205);
            _fx = fx;
            bool note = start || (on && !_wasOn) || (on && _logF > 0 && Math.Abs(Math.Log(hz) - _logF) > 0.04);
            _wasOn = on;
            double vol = p.Env.Out / 15.0;
            // the chip's own sound (band-limited) for sound effects, the instrument for music: each with its own level, eased
            // over ~1-2 ms, so switching between them crossfades instead of cutting (a cut is a pop)
            double fxT = _fx && on ? vol : 0;
            _ampFx += (fxT - _ampFx) * 0.02;
            double fxs = 0;
            if (_ampFx > 1e-4) fxs = Square(ref _pf, hz / Fs, Duties[p.Duty]) * _ampFx * 0.35;
            Track(on && !_fx, hz, vol, note && !_fx);
            var (main, spread) = Render(Math.Exp(_logF), p.Duty);
            return (main + fxs, spread);
        }

        public (double Main, double Spread) Tri(Triangle t)
        {
            bool on = t.Len != 0 && t.Lin != 0 && t.Period >= 2;
            double hz = Cpu / (32.0 * (t.Period + 1));
            bool note = (on && !_wasOn) || (on && _logF > 0 && Math.Abs(Math.Log(hz) - _logF) > 0.04);
            _wasOn = on;
            Track(on, hz, 0.8, note);
            return Render(Math.Exp(_logF), 2);
        }

        private (double, double) Render(double hz, int duty)
        {
            if (_amp < 1e-5) return (0, 0);
            // vibrato fades in after the attack
            double vd = _k == Kind.Bass ? 0 : Math.Clamp((_age - 0.12) / 0.25, 0, 1) * (_k == Kind.Violins ? 0.0035 : 0.002);
            _vib += (_k == Kind.Violins ? 5.6 : 4.8) / Fs;
            if (_vib >= 1) _vib -= 1;
            double f = hz * (1 + vd * Math.Sin(2 * Math.PI * _vib));
            double main, spread = 0, cut, q;
            switch (_k)
            {
                case Kind.Violins:
                {
                    double a = Saw(ref _p0, f * _det[0] / Fs), b = Saw(ref _p1, f * _det[1] / Fs), c = Saw(ref _p2, f * _det[2] / Fs);
                    main = (a + b + c) / 3;
                    spread = (a - c) / 3;
                    cut = 1800 + f * (duty <= 1 ? 3.5 : 2.5); q = 0.75;
                    break;
                }
                case Kind.Horns:
                    main = Saw(ref _p0, f / Fs) * 0.6 + Square(ref _p1, f * _det[1] / Fs, 0.5) * 0.4;
                    cut = 700 + f * 1.8; q = 0.9;
                    break;
                default:
                    main = Saw(ref _p0, f / Fs) * 0.7;
                    _p3 += f * 0.5 / Fs; if (_p3 >= 1) _p3 -= 1;
                    main += Math.Sin(2 * Math.PI * _p3) * 0.55;
                    cut = 380 + f * 1.5; q = 0.8;
                    break;
            }
            // a touch more brightness at the attack (bow / breath)
            cut *= 1 + 0.6 * Math.Exp(-_age * 25);
            _f.Run(main, cut, q);
            _f2.Run(spread, cut, q);
            return (_f.Lp * _amp, _f2.Lp * _amp);
        }
    }

    private sealed class Perc
    {
        private static readonly int[] NoisePeriods = { 4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068 };   // = Apu.NoisePeriod
        private double _amp, _thump, _thumpT, _tph, _tf, _snLp;
        private bool _wasStart, _wasOn;
        private int _idx;
        private uint _rng = 0x2545F491;
        private Svf _lo, _hi;

        private double White()
        {
            _rng ^= _rng << 13; _rng ^= _rng >> 17; _rng ^= _rng << 5;
            return (_rng & 0xFFFF) / 32768.0 - 1;
        }

        /// <summary>(timpani, snare / cymbal)</summary>
        public (double, double) Next(Noise n)
        {
            bool on = n.Len != 0;
            bool start = (n.Env.Start && !_wasStart) || (on && !_wasOn);
            _wasStart = n.Env.Start; _wasOn = on;
            double vol = on ? n.Env.Out / 15.0 : 0;
            if (start)
            {
                _idx = Array.IndexOf(NoisePeriods, n.Period);
                if (_idx >= 8)                                    // low noise: a timpani stroke
                {
                    if (_thump < 0.02) _tph = 0;
                    _thumpT = Math.Max(_thumpT, 0.3 + 0.7 * n.Env.Out / 15.0);
                    _tf = 58 + (15 - _idx) * 9;
                }
            }
            _amp += (vol - _amp) * (vol > _amp ? 0.03 : 0.004);    // ~1 ms attack: a drum hit, not a click
            double w = White();
            // timpani: a decaying sine dropping slightly in pitch + low rumble
            _tph += _tf * (1 + 0.15 * _thump) / Fs; if (_tph >= 1) _tph -= 1;
            double tp = Math.Sin(2 * Math.PI * _tph) * _thump;
            if (_thumpT > _thump) { _thump += (_thumpT - _thump) * 0.025; if (_thumpT - _thump < 0.01) _thumpT = 0; }   // ~1 ms attack
            else _thump *= 0.99988;                              // ~0.4 s decay
            _lo.Run(w, 260, 0.7);
            tp += _lo.Lp * (_idx >= 8 ? _amp * 1.4 : 0);
            // snare / cymbal: band-passed noise, brighter for short periods; the metallic mode rings
            double centre = Math.Min(6000, _idx < 0 ? 4000 : 9000 * Math.Pow(0.78, _idx));   // not above 6 kHz (harsh)
            _hi.Run(w, Math.Max(centre, 900), n.Mode ? 6 : 0.9);
            double sn = _hi.Bp * (_idx >= 8 ? 0.25 : 1) * _amp * (n.Mode ? 1.6 : 1.2);
            _snLp += (sn - _snLp) * 0.55;                            // gentle low-pass (~6 kHz): softer edges
            sn = _snLp;
            return (tp, sn);
        }
    }
}
