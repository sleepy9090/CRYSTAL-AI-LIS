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
    private readonly List<short> _cdTake = new();
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
        float g = Muted ? 0 : Gain * 32767f;
        bool drained = true;
        for (int i = 0; i < Buffers; i++) drained &= Free(i);
        if (drained)
        {
            if (_started) Underruns++;
            _pending.Clear();
            for (int i = 0; i < (int)(Lead * Rate) * Ch; i++) _pending.Enqueue(0);
        }
        if (Cd)
        {
            _cdTake.Clear();
            foreach (short v in Mixer.Take(_cdTake)) _pending.Enqueue(Muted ? (short)0 : v);
        }
        else
            for (int i = 0; i < _n; i++) _pending.Enqueue((short)Math.Clamp(MathF.Round(_buf[i] * g), -32768f, 32767f));
        _n = 0;
        while (_pending.Count > MaxPending * Ch) { for (int c = 0; c < Ch; c++) _pending.Dequeue(); Dropped++; }
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
