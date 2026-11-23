// re-c Windows host — `ReC.exe --wav ID OUT.wav [SECONDS]`: one music id through the sound driver + APU into a
// 16-bit mono 44.1 kHz WAV, byte for byte = `node ../web/test/apu_check.mjs --wav ID OUT.wav SECONDS` (and the
// tests' --wav). Lets tools/apu_wav_compare.py --exe check the shipped exe's APU stream under Wine (TASKS.md T5).
using System.Globalization;
using System.Text;
using ReC.Game;

namespace ReC.Win;

internal static class WavOut
{
    private const int SR = 44100;

    /// <summary>JS Math.round (half up toward +inf)</summary>
    private static double JsRound(double x)
    {
        double r = Math.Floor(x);
        return x - r >= 0.5 ? r + 1 : r;
    }

    /// <summary>apu_check.mjs playMusic: SndInit (Reset), PlaySound, per frame SoundUpdate + 29781 APU cycles</summary>
    private static List<double> PlayMusic(Data data, int id, double seconds)
    {
        var m = new Machine(data);
        var pcm = new List<double>();
        m.Apu.SetOutput(SR, pcm.Add);
        m.Reset();
        PlaySound.Play(m, id);
        int frames = (int)JsRound(seconds * 60.0988);
        for (int f = 0; f < frames; f++) { m.SoundUpdate(m); m.Apu.Run(29781); }
        return pcm;
    }

    /// <summary>args = ID OUT.wav [SECONDS] [--data DIR]; ID decimal or 0x-hex</summary>
    public static int Run(string[] args)
    {
        var rest = args.ToList();
        string? dataDir = null;
        int di = rest.IndexOf("--data");
        if (di >= 0 && di + 1 < rest.Count) { dataDir = rest[di + 1]; rest.RemoveRange(di, 2); }
        if (rest.Count < 2) { Console.Error.WriteLine("usage: ReC.exe --wav ID OUT.wav [SECONDS] [--data DIR]"); return 2; }
        string s = rest[0];
        int id = s.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? Convert.ToInt32(s[2..], 16) : int.Parse(s, CultureInfo.InvariantCulture);
        double seconds = rest.Count > 2 ? double.Parse(rest[2], CultureInfo.InvariantCulture) : 30;
        var pcm = PlayMusic(Data.LoadDefault(dataDir), id, seconds);
        var b = new byte[44 + pcm.Count * 2];
        void U32(int o, int v) => BitConverter.TryWriteBytes(b.AsSpan(o, 4), (uint)v);
        void U16(int o, int v) => BitConverter.TryWriteBytes(b.AsSpan(o, 2), (ushort)v);
        Encoding.ASCII.GetBytes("RIFF").CopyTo(b, 0); U32(4, 36 + pcm.Count * 2); Encoding.ASCII.GetBytes("WAVEfmt ").CopyTo(b, 8);
        U32(16, 16); U16(20, 1); U16(22, 1); U32(24, SR);
        U32(28, SR * 2); U16(32, 2); U16(34, 16); Encoding.ASCII.GetBytes("data").CopyTo(b, 36);
        U32(40, pcm.Count * 2);
        for (int i = 0; i < pcm.Count; i++)
            BitConverter.TryWriteBytes(b.AsSpan(44 + i * 2, 2), (short)Math.Max(-32768, Math.Min(32767, JsRound(pcm[i] * 65536))));
        File.WriteAllBytes(rest[1], b);
        Console.WriteLine($"wrote {rest[1]}: {(double)pcm.Count / SR:F1} s");
        return 0;
    }
}
