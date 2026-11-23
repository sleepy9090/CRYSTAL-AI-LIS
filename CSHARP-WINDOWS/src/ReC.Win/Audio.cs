// re-c Windows host — winmm waveOut output for Nes/Apu.cs (= web/src/audio.js, WebAudio -> waveOut; architecture_win.md
// §1 rule 2).
//
//   var out = new AudioOut(machine);  out.Resume()   opens the device and turns the APU's synthesis on
//   out.Flush()                       after each batch of frames: queues the samples the APU produced
//   out.SetMuted(bool)
//
// 16-bit mono PCM at 44.1 kHz in Buffers fixed waveOut buffers of BufSamples each (polled through WHDR_DONE, no
// callback thread). Samples wait in a pending queue until a whole buffer is free; when the game runs ahead of the
// device (pending > MaxPending) the oldest samples are dropped, so latency stays bounded. When every buffer has
// drained (stall, pause, window drag) the next audio starts with Lead seconds of silence, like the JS starting its
// next buffer LEAD seconds ahead of currentTime. Underruns are only counted (Underruns), never logged per event.
// Until Resume() succeeds the APU stays headless (no synthesis).
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
    private const int Chunk = 1024;            // = audio.js CHUNK: flush waits for at least this many samples
    private const int Buffers = 4, BufSamples = 1024;
    private const double Lead = 0.05;
    private const float Gain = 1.6f;           // APU mixer peaks near 0.4 after the output filters (apu_check)
    private const int MaxPending = Rate / 4;   // drop the oldest samples rather than let latency grow past 0.25 s

    private readonly Machine _m;
    private IntPtr _hwo;
    private Win32.WAVEHDR* _hdr;
    private short* _mem;
    private float[] _buf;
    private int _n;
    private readonly Queue<short> _pending = new();
    private bool _started;
    public bool Muted { get; private set; }
    public int Underruns { get; private set; }
    public int Dropped { get; private set; }

    // js: audio.js AudioOut
    // js: audio.js constructor (AudioOut)
    public AudioOut(Machine machine)
    {
        _m = machine;
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
            FormatTag = Win32.WAVE_FORMAT_PCM, Channels = 1, SamplesPerSec = Rate, AvgBytesPerSec = Rate * 2,
            BlockAlign = 2, BitsPerSample = 16,
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
        _mem = (short*)NativeMemory.AllocZeroed((nuint)(Buffers * BufSamples * 2));
        for (int i = 0; i < Buffers; i++)
        {
            _hdr[i].Data = (IntPtr)(_mem + i * BufSamples);
            _hdr[i].BufferLength = BufSamples * 2;
            _hdr[i].Flags = Win32.WHDR_DONE;          // free until first written
        }
        _m.Apu.SetOutput(Rate, Push);
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

    private bool Free(int i) => (_hdr[i].Flags & Win32.WHDR_DONE) != 0;

    // js: audio.js flush
    public void Flush()
    {
        if (_hwo == IntPtr.Zero || _n < Chunk) return;
        float g = Muted ? 0 : Gain * 32767f;
        bool drained = true;
        for (int i = 0; i < Buffers; i++) drained &= Free(i);
        if (drained)
        {
            if (_started) Underruns++;
            _pending.Clear();
            for (int i = 0; i < (int)(Lead * Rate); i++) _pending.Enqueue(0);
        }
        for (int i = 0; i < _n; i++) _pending.Enqueue((short)Math.Clamp(MathF.Round(_buf[i] * g), -32768f, 32767f));
        _n = 0;
        while (_pending.Count > MaxPending) { _pending.Dequeue(); Dropped++; }
        for (int i = 0; i < Buffers && _pending.Count >= BufSamples; i++)
        {
            if (!Free(i)) continue;
            Win32.WAVEHDR* h = _hdr + i;
            if ((h->Flags & Win32.WHDR_PREPARED) != 0) Win32.waveOutUnprepareHeader(_hwo, h, (uint)sizeof(Win32.WAVEHDR));
            short* d = _mem + i * BufSamples;
            for (int k = 0; k < BufSamples; k++) d[k] = _pending.Dequeue();
            h->Flags = 0;
            Win32.waveOutPrepareHeader(_hwo, h, (uint)sizeof(Win32.WAVEHDR));
            Win32.waveOutWrite(_hwo, h, (uint)sizeof(Win32.WAVEHDR));
            _started = true;
        }
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
