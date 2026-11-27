// re-c Windows host — winmm waveOut output for Nes/Apu.cs (= web/src/audio.js, WebAudio -> waveOut; architecture_win.md
// §1 rule 2).
//
//   var out = new AudioOut(machine);  out.Resume()   opens the device and turns the APU's synthesis on
//   out.Flush()                       after each batch of frames: queues the samples the APU produced
//   out.SetMuted(bool)
//
// 16-bit mono PCM at 44.1 kHz in Buffers fixed waveOut buffers of BufSamples each (polled through WHDR_DONE, no
// callback thread). Samples wait in a pending queue until a whole buffer is free; when the game runs ahead of the
// device (queued > MaxQueue, ~116 ms) the backlog is spliced out with a crossfade, so latency stays bounded. When every buffer has
// drained (stall, pause, window drag) the next audio starts with Lead seconds of silence, like the JS starting its
// next buffer LEAD seconds ahead of currentTime. Underruns are only counted (Underruns), never logged per event.
// Until Resume() succeeds the APU stays headless (no synthesis).
// T13: Cd = true opens the device as 44.1 kHz 16-bit STEREO and feeds it from CdMixer (CdAudio.cs, Apu.OutChannels
// at 176.4 kHz); false = the original mono path above (the exact port). SetCd reopens the device.
using System.Runtime.InteropServices;
using ReC.Game;

namespace ReC.Win;

internal static unsafe partial class Win32
{
    public const uint WAVE_MAPPER = 0xFFFFFFFF, CALLBACK_NULL = 0, WHDR_DONE = 0x1, WHDR_PREPARED = 0x2;
    public const ushort WAVE_FORMAT_PCM = 1;

    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct WAVEFORMATEX
    {
        public ushort FormatTag, Channels;
        public uint SamplesPerSec, AvgBytesPerSec;
        public ushort BlockAlign, BitsPerSample, CbSize;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEHDR
    {
        public IntPtr Data;
        public uint BufferLength, BytesRecorded;
        public IntPtr User;
        public uint Flags, Loops;
        public IntPtr Next, Reserved;
    }

    [LibraryImport("winmm.dll")]
    public static partial uint waveOutOpen(out IntPtr hwo, uint device, in WAVEFORMATEX fmt, IntPtr callback, IntPtr inst, uint flags);

    [LibraryImport("winmm.dll")]
    public static partial uint waveOutPrepareHeader(IntPtr hwo, WAVEHDR* hdr, uint size);

    [LibraryImport("winmm.dll")]
    public static partial uint waveOutUnprepareHeader(IntPtr hwo, WAVEHDR* hdr, uint size);

    [LibraryImport("winmm.dll")]
    public static partial uint waveOutWrite(IntPtr hwo, WAVEHDR* hdr, uint size);

    [LibraryImport("winmm.dll")]
    public static partial uint waveOutReset(IntPtr hwo);

    [LibraryImport("winmm.dll")]
    public static partial uint waveOutClose(IntPtr hwo);
}

internal sealed unsafe class AudioOut
{
    public const int Rate = 44100;
    // playtest fix: smaller blocks (512 = 11.6 ms) in 8 device buffers: refilled more often, so a slow frame no longer
    // starves the device (audio.js CHUNK is 1024)
    private const int Chunk = 512;
    private const int Buffers = 8, BufSamples = 512;
    private const double Lead = 0.02;            // silence before the sound starts again after the device ran dry
    private const float Gain = 1.6f;           // APU mixer peaks near 0.4 after the output filters (apu_check)
    /// <summary>T40: master volume 0..1 (Audio > Volume), both paths</summary>
    public float Volume = 1f;

    private readonly Machine _m;
    private IntPtr _hwo;
    private Win32.WAVEHDR* _hdr;
    private short* _mem;
    private float[] _buf;
    private int _n;
    private readonly Queue<short> _pending = new();
    private bool _started;
    private readonly List<short> _cdTake = new();
    // T47: rate control + resampling to the device (AudioFx.cs) instead of dropping the oldest samples
    private Resampler _rs = new(1);
    private readonly RateControl _rc = new();
    private readonly List<float> _in = new(), _res = new();
    /// <summary>T47: game speed (4 while fast forward): the stream is resampled by it, so the sound stays continuous</summary>
    public double Speed = 1;
    /// <summary>T54: ambient sounds (Ambience.cs), targets set per frame by the window from the HD scene</summary>
    public readonly Ambience Amb = new();
    /// <summary>playtest 6: the neural dialogue voice (Dialogue.cs), mixed in the centre with the game ducked under it</summary>
    public IVoice? Voice;
    private const int Target = 3072;                 // frames queued (device + pending) the rate loop aims at (~70 ms)
    private const int MaxQueue = Target + 2048;      // above ~116 ms queued the backlog is spliced out with a crossfade
    private const int Xfade = 256;                   // 5.8 ms: the splice and the fade-in after an underrun (no click)
    private int _fadeIn;
    /// <summary>T13 CD-quality stereo (CdAudio.cs) instead of the original mono stream</summary>
    public bool Cd { get; private set; }
    public readonly CdMixer Mixer = new();
    private int Ch => Cd ? 2 : 1;
    public bool Muted { get; private set; }
    public int Underruns { get; private set; }
    public int Dropped { get; private set; }

    // js: audio.js AudioOut
    // js: audio.js constructor (AudioOut)
    public AudioOut(Machine machine)
    {
        _m = machine;
        Mixer.Chip = () => _m.Apu;                     // T50: the orchestra reads the notes (Machine.Reset makes a new Apu)
        _hwo = IntPtr.Zero;
        _buf = new float[Chunk * 8];
        _n = 0;
        Muted = false;
    }

    // js: audio.js resume
    /// <summary>Opens the waveOut device once and hands the APU the sink (Machine.Reset carries it over to its new
    /// Apu). Returns false if no device could be opened (the game then runs silent).</summary>
    public bool Resume()
    {
        if (_hwo != IntPtr.Zero) return true;
        var fmt = new Win32.WAVEFORMATEX
        {
            FormatTag = Win32.WAVE_FORMAT_PCM, Channels = (ushort)Ch, SamplesPerSec = Rate, AvgBytesPerSec = (uint)(Rate * 2 * Ch),
            BlockAlign = (ushort)(2 * Ch), BitsPerSample = 16,
        };
        try
        {
            if (Win32.waveOutOpen(out _hwo, Win32.WAVE_MAPPER, fmt, IntPtr.Zero, IntPtr.Zero, Win32.CALLBACK_NULL) != 0)
            {
                _hwo = IntPtr.Zero;
                return false;
            }
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            _hwo = IntPtr.Zero;
            return false;
        }
        _hdr = (Win32.WAVEHDR*)NativeMemory.AllocZeroed((nuint)(Buffers * sizeof(Win32.WAVEHDR)));
        _mem = (short*)NativeMemory.AllocZeroed((nuint)(Buffers * BufSamples * 2 * Ch));
        for (int i = 0; i < Buffers; i++)
        {
            _hdr[i].Data = (IntPtr)(_mem + i * BufSamples * Ch);
            _hdr[i].BufferLength = (uint)(BufSamples * 2 * Ch);
            _hdr[i].Flags = Win32.WHDR_DONE;          // free until first written
        }
        _pending.Clear(); _n = 0; _started = false;
        _rs = new Resampler(Ch); _rc.Reset();
        if (Cd)
        {
            Mixer.Clear();
            _m.Apu.SetOutput(CdMixer.InRate, null);
            _m.Apu.OutChannels = Mixer.Push;
        }
        else
        {
            _m.Apu.OutChannels = null;
            _m.Apu.SetOutput(Rate, Push);
        }
        return true;
    }

    // js: audio.js setMuted
    public void SetMuted(bool on) { Muted = on; }

    /// <summary>T13: switch Original (mono) / CD quality (stereo); reopens the device when it is open</summary>
    public bool SetCd(bool on)
    {
        if (on == Cd) return _hwo != IntPtr.Zero;
        bool open = _hwo != IntPtr.Zero;
        if (open) Close();
        Cd = on;
        return !open || Resume();
    }

    // js: audio.js push
    public void Push(double v)
    {
        if (_n == _buf.Length)                 // a long catch-up burst: grow rather than drop
        {
            var b = new float[_buf.Length * 2]; Array.Copy(_buf, b, _n); _buf = b;
        }
        _buf[_n++] = (float)v;
    }

    private bool Free(int i) => (_hdr[i].Flags & Win32.WHDR_DONE) != 0;

    // js: audio.js flush
    public void Flush()
    {
        if (_hwo == IntPtr.Zero) return;
        if (Cd ? Mixer.Count < Chunk * 2 : _n < Chunk) return;
        float g = Muted ? 0 : Gain * Volume * 32767f;
        bool drained = true;
        for (int i = 0; i < Buffers; i++) drained &= Free(i);
        if (drained)
        {
            if (_started) Underruns++;
            _pending.Clear();
            for (int i = 0; i < (int)(Lead * Rate) * Ch; i++) _pending.Enqueue(0);
            _fadeIn = Xfade;                                  // the sound comes back in a short fade, not a step
        }
        _in.Clear(); _res.Clear();
        if (Cd)
        {
            _cdTake.Clear();
            foreach (short v in Mixer.Take(_cdTake)) _in.Add(v);
            g = Muted ? 0 : Volume;
        }
        else
            for (int i = 0; i < _n; i++) _in.Add(_buf[i]);
        _n = 0;
        int queued = _pending.Count / Ch;
        for (int i = 0; i < Buffers; i++) if (!Free(i)) queued += BufSamples;
        _rs.Process(_in, _rc.Ratio(Speed, queued, Target), _res);
        float av = Muted ? 0 : Volume;
        var vo = Voice;
        vo?.BeginBlock();
        for (int i = 0; i < _res.Count; i += Ch)
        {
            var (al, ar) = Amb.Silent ? (0.0, 0.0) : Amb.Next();   // T54 ambient sounds (16-bit units)
            if (vo != null)
            {
                var (vs, duck) = vo.Next();
                _res[i] *= duck; if (Ch == 2) _res[i + 1] *= duck; al *= duck; ar *= duck;
                al += vs * 26000; ar += vs * 26000;
            }
            float fi = _fadeIn > 0 ? 1f - _fadeIn-- / (float)Xfade : 1f;
            if (fi < 1f) { _res[i] *= fi; if (Ch == 2) _res[i + 1] *= fi; al *= fi; ar *= fi; }
            if (Ch == 2)
            {
                _pending.Enqueue((short)Math.Clamp(MathF.Round(_res[i] * g + (float)al * av), -32768f, 32767f));
                _pending.Enqueue((short)Math.Clamp(MathF.Round(_res[i + 1] * g + (float)ar * av), -32768f, 32767f));
            }
            else _pending.Enqueue((short)Math.Clamp(MathF.Round(_res[i] * g + (float)(al + ar) * 0.5f * av), -32768f, 32767f));
        }
        int busy = 0;
        for (int i = 0; i < Buffers; i++) if (!Free(i)) busy += BufSamples;
        if (_pending.Count / Ch + busy > MaxQueue) SpliceOut(_pending.Count / Ch + busy - Target);
        for (int i = 0; i < Buffers && _pending.Count >= BufSamples * Ch; i++)
        {
            if (!Free(i)) continue;
            Win32.WAVEHDR* h = _hdr + i;
            if ((h->Flags & Win32.WHDR_PREPARED) != 0) Win32.waveOutUnprepareHeader(_hwo, h, (uint)sizeof(Win32.WAVEHDR));
            short* d = _mem + i * BufSamples * Ch;
            for (int k = 0; k < BufSamples * Ch; k++) d[k] = _pending.Dequeue();
            h->Flags = 0;
            Win32.waveOutPrepareHeader(_hwo, h, (uint)sizeof(Win32.WAVEHDR));
            Win32.waveOutWrite(_hwo, h, (uint)sizeof(Win32.WAVEHDR));
            _started = true;
        }
    }

    /// <summary>a backlog (a catch-up burst after a stall) cut out of the pending audio: `frames` frames go, the sound on
    /// either side of the cut is crossfaded over Xfade frames (a plain cut would click; draining it by speeding up would
    /// keep the sound late for seconds)</summary>
    private void SpliceOut(int frames)
    {
        var a = _pending.ToArray();
        var b = Splice(a, Ch, frames, Xfade);
        if (b.Length == a.Length) return;
        _pending.Clear();
        foreach (short v in b) _pending.Enqueue(v);
        Dropped += (a.Length - b.Length) / Ch;
    }

    /// <summary>interleaved samples with `frames` frames cut out after the first, the two sides crossfaded over xfade frames</summary>
    public static short[] Splice(short[] a, int ch, int frames, int xfade)
    {
        int n = a.Length / ch;
        frames = Math.Min(frames, n - xfade);
        if (frames <= 0) return a;
        var o = new short[(n - frames) * ch];
        for (int f = 0; f < n - frames; f++)
            for (int c = 0; c < ch; c++)
            {
                float v = a[(f + frames) * ch + c];
                if (f < xfade) { float t = (f + 0.5f) / xfade; v = a[f * ch + c] * (1 - t) + v * t; }
                o[f * ch + c] = (short)Math.Clamp(MathF.Round(v), -32768f, 32767f);
            }
        return o;
    }

    public void Close()
    {
        if (_hwo == IntPtr.Zero) return;
        Win32.waveOutReset(_hwo);
        for (int i = 0; i < Buffers; i++)
            if ((_hdr[i].Flags & Win32.WHDR_PREPARED) != 0) Win32.waveOutUnprepareHeader(_hwo, _hdr + i, (uint)sizeof(Win32.WAVEHDR));
        Win32.waveOutClose(_hwo);
        _hwo = IntPtr.Zero;
        NativeMemory.Free(_hdr); _hdr = null;
        NativeMemory.Free(_mem); _mem = null;
    }
}
