// re-c Windows host — audio engine v2 (T47). Not in the web port; pure logic, unit-tested on Linux (win_audio_engine).
//
//   Resampler  the last stage before the device, both paths (Original mono and CD / Remastered stereo). It replaces
//              "drop the oldest samples when the game runs ahead": the output rate follows the device clock through a
//              ratio held within +-0.5 % of 1 (RateControl, a slow loop on the queued audio), so there are no clicks
//              from dropped blocks and the latency stays near its target. 4-point Catmull-Rom interpolation; at ratio
//              1 with an integer position the output is the input sample for sample. Fast forward (Space / RB) plays
//              the 4x stream at ratio 4 (box-averaged): continuous, pitched-up sound instead of chopped blocks.
//   RateControl  ratio = speed x (1 + 0.005 x clamp((queued - target) / target)), queued smoothed (EMA).
//   Crossfeed  headphone crossfeed (Audio > Headphone crossfeed): each side gets the other side's lows (one-pole 700 Hz)
//              0.3 ms late at -12 dB, so hard-panned voices stop sounding "inside one ear"; mono content is unchanged
//              at low frequencies.
namespace ReC.Win;

internal sealed class Resampler
{
    private readonly int _ch;
    private readonly List<float> _buf = new();   // interleaved frames not consumed yet; frame 0 = history (x[-1])
    private double _pos = 1;

    public Resampler(int channels)
    {
        _ch = channels;
        Clear();
    }

    public void Clear()
    {
        _buf.Clear();
        for (int c = 0; c < _ch; c++) _buf.Add(0);
        _pos = 1;
    }

    /// <summary>Catmull-Rom between x0 (t = 0) and x1 (t = 1)</summary>
    public static float Hermite(float xm1, float x0, float x1, float x2, float t)
    {
        float c1 = 0.5f * (x1 - xm1), c2 = xm1 - 2.5f * x0 + 2f * x1 - 0.5f * x2, c3 = 0.5f * (x2 - xm1) + 1.5f * (x0 - x1);
        return ((c3 * t + c2) * t + c1) * t + x0;
    }

    /// <summary>appends the interleaved input; writes output frames, ratio input frames apart (ratio > 1.5: box average)</summary>
    public void Process(List<float> input, double ratio, List<float> output)
    {
        _buf.AddRange(input);
        int n = _buf.Count / _ch;
        if (ratio > 1.5)
            while (_pos + ratio + 1 <= n)
            {
                int a = (int)_pos, b = Math.Max(a + 1, (int)(_pos + ratio));
                for (int c = 0; c < _ch; c++)
                {
                    float s = 0;
                    for (int i = a; i < b; i++) s += _buf[i * _ch + c];
                    output.Add(s / (b - a));
                }
                _pos += ratio;
            }
        else
            while (_pos + 2 < n)
            {
                int i = (int)_pos;
                float t = (float)(_pos - i);
                for (int c = 0; c < _ch; c++)
                    output.Add(t == 0 ? _buf[i * _ch + c]
                        : Hermite(_buf[(i - 1) * _ch + c], _buf[i * _ch + c], _buf[(i + 1) * _ch + c], _buf[(i + 2) * _ch + c], t));
                _pos += ratio;
            }
        int drop = Math.Max(0, (int)_pos - 1);       // keep one frame of history before the position
        drop = Math.Min(drop, n);
        _buf.RemoveRange(0, drop * _ch);
        _pos -= drop;
    }
}

internal sealed class RateControl
{
    public const double MaxAdjust = 0.005;
    private double _avg = -1;

    /// <summary>queued = frames waiting (device buffers + pending), target = the latency aimed at, in frames</summary>
    public double Ratio(double speed, double queued, double target)
    {
        _avg = _avg < 0 ? queued : _avg + (queued - _avg) * 0.05;
        double e = Math.Clamp((_avg - target) / target, -1, 1);
        return speed * (1 + MaxAdjust * e);
    }

    public void Reset() => _avg = -1;
}

internal sealed class Crossfeed
{
    private const int Delay = 13;                          // 0.3 ms at 44.1 kHz
    private const double G = 0.25, A = 0.0949;             // -12 dB; one-pole 700 Hz: 1 - exp(-2 pi 700 / 44100)
    private readonly double[] _l = new double[Delay], _r = new double[Delay];
    private int _i;
    private double _lpL, _lpR;

    public (double L, double R) Process(double l, double r)
    {
        double dl = _l[_i], dr = _r[_i];
        _l[_i] = l; _r[_i] = r;
        _i = (_i + 1) % Delay;
        _lpL += (dl - _lpL) * A;
        _lpR += (dr - _lpR) * A;
        return ((l + G * _lpR) / (1 + G), (r + G * _lpL) / (1 + G));
    }

    public void Clear()
    {
        Array.Clear(_l); Array.Clear(_r);
        _i = 0; _lpL = _lpR = 0;
    }
}

/// <summary>T49: frames per second over the last second + mean time per drawn frame (F2 / Video > Show FPS)</summary>
internal sealed class FrameStats
{
    private readonly Queue<(double At, double Ms)> _q = new();
    public double Fps { get; private set; }
    public double Ms { get; private set; }

    public void Record(double startMs, double endMs)
    {
        _q.Enqueue((endMs, endMs - startMs));
        while (_q.Count > 0 && _q.Peek().At <= endMs - 1000) _q.Dequeue();
        double span = Math.Max(1, endMs - _q.Peek().At);
        Fps = _q.Count < 2 ? 0 : (_q.Count - 1) * 1000.0 / span;
        Ms = _q.Average(e => e.Ms);
    }

    public string Text => $"{Fps:0} FPS  {Ms:0.0} MS";
}
