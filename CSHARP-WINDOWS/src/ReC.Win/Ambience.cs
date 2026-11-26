// re-c Windows host — Audio > Ambient sounds (T54). Not in the web port; pure logic, unit-tested (win_ambience).
//
// A soundscape under the game's own sound during gameplay, chosen from what the HD engine sees on screen (HdScene:
// Outdoor, Hot, Icy, Play, Mean): wind in gusts + birds now and then outdoors, a low rumble + water drips with an echo
// in dark caves, crackling fire + a low roar in hot places, a whistling wind on ice. Each bed fades in / out over about
// 1.5 s. Synthesized (filtered noise, sine chirps); deterministic (fixed seed). Output in 16-bit sample units, added
// after the resampler by AudioOut (any audio mode), scaled by the master volume.
namespace ReC.Win;

internal sealed class Ambience
{
    private const double Fs = 44100;
    /// <summary>targets 0..1, set once per video frame</summary>
    public float Outdoor, Hot, Icy, Cave;
    private double _wo, _wh, _wi, _wc;
    private uint _rng = 0x9E3779B9;
    private double _gust, _gustT, _gustTo;
    private Bp _wl, _wr, _whl, _whr;
    private double _lpl, _lpr, _ru1, _ru2, _roar, _crk, _crkHp;
    // birds
    private int _birdWait = 44100 * 3, _notes, _noteT, _noteLen, _gap;
    private double _bf, _bdf, _bph, _bpan;
    // drips + echo
    private int _dripWait = 44100;
    private double _df, _dph, _da;
    private readonly float[] _echo = new float[16384];
    private int _ei;

    private struct Bp
    {
        private double _a, _b;
        public double Run(double x, double hz, double q)
        {
            double g = Math.Tan(Math.PI * hz / Fs), k = 1 / q;
            double v1 = (_a + g * (x - _b)) / (1 + g * (g + k)), v2 = _b + g * v1;
            _a = 2 * v1 - _a; _b = 2 * v2 - _b;
            return v1;
        }
    }

    private double White()
    {
        _rng ^= _rng << 13; _rng ^= _rng >> 17; _rng ^= _rng << 5;
        return (_rng & 0xFFFFFF) / 8388608.0 - 1;
    }

    private double Uni() => (White() + 1) * 0.5;

    public bool Silent => _wo + _wh + _wi + _wc < 1e-4 && Outdoor + Hot + Icy + Cave <= 0;

    /// <summary>one stereo sample (16-bit units)</summary>
    public (double L, double R) Next()
    {
        const double k = 1 / (1.5 * Fs);
        _wo += (Outdoor - _wo) * k * 3; _wh += (Hot - _wh) * k * 3; _wi += (Icy - _wi) * k * 3; _wc += (Cave - _wc) * k * 3;
        if (_wo + _wh + _wi + _wc < 1e-4 && _da < 1e-3) return (0, 0);
        double l = 0, r = 0;
        // gusts: a new target every 0.6-2 s, eased
        if (--_gustT <= 0) { _gustTo = Uni(); _gustT = (0.6 + 1.4 * Uni()) * Fs; }
        _gust += (_gustTo - _gust) * (1 / (0.8 * Fs));
        double wind = _wo + _wi * 0.8;
        if (wind > 1e-4)
        {
            _lpl += (White() - _lpl) * 0.15; _lpr += (White() - _lpr) * 0.15;
            double c = 320 + 380 * _gust;
            double a = 7800 * (0.35 + 0.65 * _gust) * wind;
            l += _wl.Run(_lpl, c, 0.9) * a;
            r += _wr.Run(_lpr, c * 1.07, 0.9) * a;
        }
        if (_wi > 1e-4)
        {
            // whistle: narrow bands drifting slowly
            double c = 900 + 260 * Math.Sin(_gustT / Fs * 0.9) + 300 * _gust;
            double a = 800 * _wi * (0.5 + 0.5 * _gust);
            l += _whl.Run(White(), c, 14) * a;
            r += _whr.Run(White(), c * 1.33, 14) * a * 0.8;
        }
        // birds: 3-6 chirps (rising or falling sweeps) every 3-10 s outdoors
        if (_notes > 0)
        {
            if (_gap > 0) _gap--;
            else
            {
                double env = Math.Sin(Math.PI * _noteT / _noteLen);
                _bph += (_bf + _bdf * _noteT / _noteLen) / Fs;
                if (_bph >= 1) _bph -= 1;
                double s = Math.Sin(2 * Math.PI * _bph) * env * env * 700 * _wo;
                l += s * (1 - _bpan); r += s * _bpan;
                if (++_noteT >= _noteLen) { _noteT = 0; _notes--; _gap = (int)(Fs * (0.03 + 0.06 * Uni())); _bf *= 0.97 + 0.08 * Uni(); }
            }
        }
        else if (--_birdWait <= 0)
        {
            _birdWait = (int)(Fs * (3 + 7 * Uni()));
            if (_wo > 0.3)
            {
                _notes = 3 + (int)(Uni() * 4); _noteT = 0; _gap = 0;
                _noteLen = (int)(Fs * (0.04 + 0.05 * Uni()));
                _bf = 2800 + 1600 * Uni(); _bdf = (Uni() - 0.4) * 1600; _bpan = 0.15 + 0.7 * Uni();
            }
        }
        // cave: rumble + drips through an echo
        if (_wc > 1e-4)
        {
            _ru1 += (White() - _ru1) * 0.012; _ru2 += (_ru1 - _ru2) * 0.012;
            double rb = _ru2 * 9000 * _wc;
            l += rb; r += rb;
            if (--_dripWait <= 0)
            {
                _dripWait = (int)(Fs * (0.8 + 3 * Uni()));
                if (_wc > 0.3) { _df = 900 + 900 * Uni(); _da = 1; _dph = 0; }
            }
        }
        double drip = 0;
        if (_da > 1e-3)
        {
            _df *= 1.00004;                                        // a drop's pitch rises as its bubble shrinks
            _dph += _df / Fs; if (_dph >= 1) _dph -= 1;
            drip = Math.Sin(2 * Math.PI * _dph) * _da * 1500 * _wc;
            _da *= 0.99965;                                        // ~65 ms
        }
        float e1 = _echo[(_ei - 9261) & 16383], e2 = _echo[(_ei - 13230) & 16383];
        _echo[_ei] = (float)(drip + (e1 + e2) * 0.3);
        _ei = (_ei + 1) & 16383;
        l += drip + e1 * 0.7; r += drip * 0.8 + e2 * 0.7;
        // fire: random crackles (high-passed clicks) over a low roar
        if (_wh > 1e-4)
        {
            if (Uni() < 22 / Fs) _crk = (Uni() - 0.5) * 9000;
            _crk *= 0.93;
            double hp = _crk - _crkHp; _crkHp = _crk;
            _roar += (White() - _roar) * 0.03;
            double f = (hp * 1.3 + _roar * 4800) * _wh;
            l += f * (0.8 + 0.4 * _gust); r += f * (1.2 - 0.4 * _gust);
        }
        return (l, r);
    }
}
