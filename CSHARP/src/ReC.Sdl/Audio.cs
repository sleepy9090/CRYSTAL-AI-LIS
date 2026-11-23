// re-c C# port — SDL audio output for Nes/Apu.cs (= web/src/audio.js, WebAudio -> SDL_QueueAudio).
//
//   var out = new AudioOut(machine);  out.Resume()   opens the device and turns the APU's synthesis on
//   out.Flush()                       after each batch of frames: queues the samples the APU produced
//   out.SetMuted(bool)
//
// The APU pushes one sample per Push(v) call at the device's rate. Samples are queued in chunks of >= Chunk. If the
// device queue ran dry (stall, pause) the next chunk is preceded by Lead seconds of silence, like the JS starting the
// next buffer Lead seconds ahead of currentTime. Until Resume() succeeds the APU stays headless (no synthesis).
using ReC.Game;

namespace ReC.Sdl;

internal sealed class AudioOut
{
    private const int Chunk = 1024;
    private const double Lead = 0.05;
    private const float Gain = 1.6f;          // APU mixer peaks near 0.4 after the output filters (apu_check)
    private const double MaxQueued = 0.25;    // SDL only: drop a chunk rather than let latency grow past this

    private readonly Machine _m;
    private uint _dev;
    private int _rate;
    private float[] _buf;
    private int _n;
    public bool Muted { get; private set; }

    // js: audio.js AudioOut
    // js: audio.js constructor (AudioOut)
    public AudioOut(Machine machine)
    {
        _m = machine;
        _dev = 0;
        _buf = new float[Chunk * 8];
        _n = 0;
        Muted = false;
    }

    // js: audio.js resume
    /// <summary>Opens the SDL device once (mono float32 at its preferred rate) and hands the APU the sink;
    /// Machine.Reset carries the sink over to its new Apu. Returns false if no device could be opened.</summary>
    public bool Resume()
    {
        if (_dev == 0)
        {
            var want = new Sdl.AudioSpec { Freq = 48000, Format = Sdl.AudioF32Sys, Channels = 1, Samples = Chunk };
            _dev = Sdl.OpenAudioDevice(IntPtr.Zero, 0, ref want, out var have, 0x1 /* SDL_AUDIO_ALLOW_FREQUENCY_CHANGE */);
            if (_dev == 0) return false;
            _rate = have.Freq;
            _m.Apu.SetOutput(_rate, Push);
        }
        Sdl.PauseAudioDevice(_dev, 0);
        return true;
    }

    // js: audio.js setMuted
    public void SetMuted(bool on) { Muted = on; }

    // js: audio.js push
    public void Push(double v)
    {
        if (_n == _buf.Length)                 // a long catch-up burst: grow rather than drop
        {
            var b = new float[_buf.Length * 2]; Array.Copy(_buf, b, _n); _buf = b;
        }
        _buf[_n++] = (float)v;
    }

    // js: audio.js flush
    public unsafe void Flush()
    {
        if (_dev == 0 || _n < Chunk) return;
        int n = _n;
        _n = 0;
        uint bytesPerSec = (uint)_rate * sizeof(float);
        uint queued = Sdl.GetQueuedAudioSize(_dev);
        if (queued > MaxQueued * bytesPerSec) return;            // running ahead of the device
        if (queued == 0)                                         // ran dry: start Lead seconds ahead
        {
            var pad = new float[(int)(Lead * _rate)];
            fixed (float* p = pad) Sdl.QueueAudio(_dev, (IntPtr)p, (uint)(pad.Length * sizeof(float)));
        }
        float g = Muted ? 0 : Gain;
        var outBuf = new float[n];
        for (int i = 0; i < n; i++) outBuf[i] = _buf[i] * g;
        fixed (float* p = outBuf) Sdl.QueueAudio(_dev, (IntPtr)p, (uint)(n * sizeof(float)));
    }

    public void Close()
    {
        if (_dev != 0) { Sdl.CloseAudioDevice(_dev); _dev = 0; }
    }
}
