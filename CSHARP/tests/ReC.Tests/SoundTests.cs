// re-c C# port — sound tests (TASKS.md T2):
//   sound_frames  = ../web/test/sound_check.mjs: replay ../web/test/sound_frames/*.json (tools/emu/sound_capture.py)
//                   through SoundUpdate.Run; per call the APU write list and all RAM except $0185-$01FF must match.
//   apu_*         = ../web/test/apu_check.mjs checks 1-4 (pitch, $4015 status, sweep, driver -> APU per music id).
//   --wav ID OUT.wav [SECONDS] = apu_check.mjs --wav (16-bit mono 44.1 kHz); tools/apu_wav_compare.py diffs both.
using System.Text;
using System.Text.Json;
using ReC.Game;
using ReC.Game.Sound;
using ReC.Nes;

namespace ReC.Tests;

internal static partial class Program
{
    private const int SR = 44100;

    private static readonly (string Name, Action Fn)[] SoundTests =
    {
        ("sound_frames", SoundFrames),
        ("apu_pitch", ApuPitch),
        ("apu_status", ApuStatus),
        ("apu_sweep", ApuSweep),
        ("apu_driver", ApuDriver),
    };

    private static void SoundFrames()
    {
        var dir = Path.Combine(Data.FindDir(), "..", "test", "sound_frames");
        var m = new Machine(TheData.Value);
        var writes = new StringBuilder();
        m.ApuWrite = (a, v) => writes.Append($"{a - 0x4000:X2}{v & 0xFF:X2}");
        m.ApuRead = _ => 0;
        int nsc = 0, fail = 0, ncalls = 0, nw = 0;
        foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.Ordinal))
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(file));
            foreach (var sc in doc.RootElement.GetProperty("scenarios").EnumerateArray())
            {
                nsc++;
                m.Reset();
                var want = Convert.FromHexString(sc.GetProperty("ram").GetString()!);
                want.CopyTo(m.Mem.RamBytes, 0);
                string bad = "";
                int i = 0;
                foreach (var st in sc.GetProperty("steps").EnumerateArray())
                {
                    if (st.TryGetProperty("p", out var p))
                        foreach (var e in p.EnumerateArray())
                        {
                            int a = e[0].GetInt32(), v = e[1].GetInt32();
                            m.Wr(a, v); want[a] = (byte)v;
                        }
                    foreach (var e in st.GetProperty("d").EnumerateArray()) want[e[0].GetInt32()] = (byte)e[1].GetInt32();
                    writes.Clear();
                    try { SoundUpdate.Run(m); }
                    catch (Exception ex) { bad = $"call {i}: {ex.Message}"; break; }
                    string w = st.GetProperty("w").GetString()!;
                    ncalls++; nw += w.Length / 4;
                    string got = writes.ToString();
                    if (got != w)
                    {
                        int k = 0;
                        while (k + 4 <= got.Length && k + 4 <= w.Length && got.AsSpan(k, 4).SequenceEqual(w.AsSpan(k, 4))) k += 4;
                        bad = $"call {i}: APU writes differ at #{k / 4}";
                    }
                    var ram = m.Mem.RamBytes;
                    int nd = 0;
                    string first = "";
                    for (int a = 0; a < 0x800; a++)
                        if (!(a >= 0x185 && a < 0x200) && ram[a] != want[a]) { if (nd++ == 0) first = $"${a:X4} port {ram[a]:X2} emu {want[a]:X2}"; }
                    if (nd > 0) bad += $"{(bad.Length > 0 ? "; " : "")}call {i}: {nd} RAM byte(s), first {first}";
                    if (bad.Length > 0) break;
                    i++;
                }
                if (bad.Length > 0) { fail++; if (fail <= 6) Console.WriteLine($"  FAIL {sc.GetProperty("name").GetString()}: {bad}"); }
            }
        }
        Console.WriteLine($"  {nsc - fail}/{nsc} scenario(s) identical, {ncalls} SoundUpdate calls, {nw} APU writes compared");
        True(nsc > 0, "sound_frames scenarios found");
        Eq(0, fail, "sound_frames failing scenarios");
    }

    private static List<double> Render(Apu apu, double seconds)
    {
        var @out = new List<double>();
        apu.SetOutput(SR, @out.Add);
        apu.Run((int)JsRound(Apu.CpuHz * seconds));
        return @out;
    }

    /// <summary>JS Math.round (half up toward +inf)</summary>
    private static double JsRound(double x)
    {
        double r = Math.Floor(x);
        return x - r >= 0.5 ? r + 1 : r;
    }

    private static double FreqOf(List<double> pcm)
    {                                           // rising zero crossings of the (high-passed) signal, skip 0.1 s settle
        var s = pcm.Skip(SR / 10).ToList();
        int n = 0;
        for (int i = 1; i < s.Count; i++) if (s[i - 1] < 0 && s[i] >= 0) n++;
        return n / ((double)s.Count / SR);
    }

    private static void ApuPitch()
    {
        foreach (var (name, @base, period, div) in new[] { ("pulse 1", 0x4000, 0x0FD, 16), ("pulse 2", 0x4004, 0x1AB, 16),
                     ("triangle", 0x4008, 0x17C, 32) })
        {
            var apu = new Apu();
            apu.Write(0x4017, 0x40);
            apu.Write(0x4015, 0x0F);
            apu.Write(@base, @base == 0x4008 ? 0xFF : 0xBF);
            if (@base != 0x4008) apu.Write(@base + 1, 0x08);
            apu.Write(@base + 2, period & 0xFF);
            apu.Write(@base + 3, (period >> 8) | 0xF8);
            double want = (double)Apu.CpuHz / (div * (period + 1)), got = FreqOf(Render(apu, 1));
            True(Math.Abs(got - want) / want < 0.01, $"{name} P=${period:x}: {got:F1} Hz, formula {want:F1} Hz");
        }
    }

    private static void ApuStatus()
    {
        var a = new Apu();
        a.Write(0x4017, 0x40); a.Write(0x4015, 0x01); a.Write(0x4000, 0x10); a.Write(0x4003, 0x08);   // length idx 1 = 254
        a.Run(29830 * 126); int on = a.Read(0x4015) & 1;
        a.Run(29830 * 2); int off = a.Read(0x4015) & 1;
        True(on == 1 && off == 0, $"length counter 254 half frames: bit0 {on} after 252, {off} after 256");
        var b = new Apu(); b.Write(0x4017, 0x00); b.Run(29830 + 10);
        int f1 = b.Read(0x4015) & 0x40, f2 = b.Read(0x4015) & 0x40;
        True(f1 != 0 && f2 == 0, "frame IRQ flag set in 4-step mode, cleared by the $4015 read");
        foreach (bool sampled in new[] { false, true })
        {
            var d = new Apu(_ => 0x55);
            d.Write(0x4010, 0x0F); d.Write(0x4012, 0x00); d.Write(0x4013, 0x01);   // rate 54, 17 bytes
            d.Write(0x4015, 0x10);
            int need = 17 * 8 * 54;
            if (sampled) d.SetOutput(SR, _ => { });
            d.Run(need - 8 * 54 - 600); int busy = d.Read(0x4015) & 0x10;
            d.Run(1200); int done = d.Read(0x4015) & 0x10;
            True(busy != 0 && done == 0, $"DMC 17-byte sample at rate 54 ({(sampled ? "sampled" : "headless")}): bit4 busy then clear");
        }
    }

    private static void ApuSweep()
    {
        Apu p1 = new(), p2 = new();
        foreach (var (a, @base) in new[] { (p1, 0x4000), (p2, 0x4004) })
        {
            a.Write(0x4015, 3); a.Write(@base + 1, 0x89); a.Write(@base + 2, 0x00); a.Write(@base + 3, 0x01);  // P=$100, shift 1
        }
        Eq(0x7F, p1.P1.Target(), "sweep negate: pulse 1 one's complement");
        Eq(0x80, p2.P2.Target(), "sweep negate: pulse 2 two's complement");
        var q = new Apu(); q.Write(0x4015, 1); q.Write(0x4000, 0xBF); q.Write(0x4002, 0x07); q.Write(0x4003, 0x08);
        var pcm = Render(q, 0.2);
        True(pcm.Skip(SR / 20).All(v => Math.Abs(v) < 1e-3), "pulse with period 7 is muted");
    }

    /// <summary>apu_check.mjs playMusic: SndInit (Reset), PlaySound, per frame SoundUpdate + 29781 APU cycles</summary>
    private static List<double> PlayMusic(int id, double seconds)
    {
        var m = new Machine(TheData.Value);
        var pcm = new List<double>();
        m.Apu.SetOutput(SR, pcm.Add);
        m.Reset();
        PlaySound.Play(m, id);
        int frames = (int)JsRound(seconds * 60.0988);
        for (int f = 0; f < frames; f++) { m.SoundUpdate(m); m.Apu.Run(29781); }
        return pcm;
    }

    private static void ApuDriver()
    {
        using var snd = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(Data.FindDir(), "sound.json")));
        var tracks = snd.RootElement.GetProperty("music").EnumerateArray().Select(t => t.GetProperty("id").GetInt32()).ToHashSet();
        var silent = new List<string>();
        var bad = new List<string>();
        for (int id = 0; id < 0x20; id++)
        {
            var pcm = PlayMusic(id, 8);
            double peak = 0, sum = 0;
            int nan = 0;
            foreach (var v in pcm) { if (!double.IsFinite(v)) nan++; else { peak = Math.Max(peak, Math.Abs(v)); sum += v * v; } }
            double rms = Math.Sqrt(sum / pcm.Count);
            if (nan > 0 || peak >= 1) bad.Add($"${id:x} nan {nan} peak {peak:F3}");
            if (id == 0 ? rms >= 0.005 : tracks.Contains(id) && rms < 0.005) silent.Add($"${id:x} rms {rms:F4}");
        }
        True(bad.Count == 0, $"music $00-$1F through the driver: finite, |x| < 1 {string.Join(", ", bad)}");
        True(silent.Count == 0, $"every music id with a track is audible (rms >= 0.005), $00 silent {string.Join(", ", silent)}");
    }

    /// <summary>--wav ID OUT.wav [SECONDS]: apu_check.mjs --wav byte for byte</summary>
    private static int WriteWav(string[] args)
    {
        string s = args[1];
        int id = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(s[2..], 16) : int.Parse(s);
        var pcm = PlayMusic(id, args.Length > 3 ? double.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture) : 30);
        var b = new byte[44 + pcm.Count * 2];
        void U32(int o, int v) => BitConverter.TryWriteBytes(b.AsSpan(o, 4), (uint)v);
        void U16(int o, int v) => BitConverter.TryWriteBytes(b.AsSpan(o, 2), (ushort)v);
        Encoding.ASCII.GetBytes("RIFF").CopyTo(b, 0); U32(4, 36 + pcm.Count * 2); Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(b, 8);
        U32(16, 16); U16(20, 1); U16(22, 1); U32(24, SR);
        U32(28, SR * 2); U16(32, 2); U16(34, 16); Encoding.ASCII.GetBytes("data").CopyTo(b, 36);
        U32(40, pcm.Count * 2);
        for (int i = 0; i < pcm.Count; i++)
            BitConverter.TryWriteBytes(b.AsSpan(44 + i * 2, 2), (short)Math.Max(-32768, Math.Min(32767, JsRound(pcm[i] * 65536))));
        File.WriteAllBytes(args[2], b);
        Console.WriteLine($"wrote {args[2]}: {(double)pcm.Count / SR:F1} s");
        return 0;
    }
}
