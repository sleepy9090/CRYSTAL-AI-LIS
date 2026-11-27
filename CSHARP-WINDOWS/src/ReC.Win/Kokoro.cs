// re-c Windows host — the Kokoro dialogue voice (playtest 7: "the voices just sound generic"). Not in the web port.
//
// Kokoro-82M (Apache-2.0; the int8 English v0.19 export from sherpa-onnx: 11 expressive speakers) synthesised by
// sherpa-onnx's offline TTS program (Apache-2.0, v1.13.8 static build: the last line that still phonemises Kokoro
// v0.19 with espeak-ng, whose data ships with the model) in voice\kokoro\ next to ReC.exe. One short-lived process per
// message page (the model loads in about a second); a newer page or Stop() kills the one still working. The model is
// shipped in two parts (model.int8.onnx.part1/2: GitHub refuses files over 100 MB) and joined once into
// %LOCALAPPDATA%\re-c\kokoro\model.int8.onnx. Each NPC: a speaker of its sex (Cast), a speed by age and a pitch
// (replay speed, NeuralVoice.Render); mixed into AudioOut like the Piper voice (IVoice).
using System.Diagnostics;

namespace ReC.Win;

/// <summary>a dialogue voice the mixer plays (Piper: NeuralVoice, Kokoro: KokoroVoice)</summary>
internal interface IVoice : IDisposable
{
    void Say(int npc, string text);
    void Stop();
    bool Alive { get; }
    string Name { get; }
    void BeginBlock();
    (float V, float Duck) Next();
}

internal sealed class KokoroVoice : IVoice
{
    // Kokoro v0.19 speakers: 0 af, 1 af_bella, 2 af_nicole, 3 af_sarah, 4 af_sky, 5 am_adam, 6 am_michael, 7 bf_emma,
    // 8 bf_isabella, 9 bm_george, 10 bm_lewis
    public static readonly int[] Female = { 0, 1, 2, 3, 4, 7, 8 }, Male = { 5, 6, 9, 10 };
    private readonly string _exe, _dir, _model, _tmp;
    private readonly object _lk = new();
    private int _seq;
    private Process? _running;
    private float[]? _next;
    private float[] _cur = Array.Empty<float>();
    private int _pos;
    private bool _clear;
    private float _duck = 1;
    public string Name => "KOKORO";
    public bool Alive => true;                         // a process per page: a failure is logged and that page stays silent

    private KokoroVoice(string exe, string dir, string model, string tmp) { _exe = exe; _dir = dir; _model = model; _tmp = tmp; }

    /// <summary>voice\kokoro next to the exe when it holds the program and the model (whole or in parts), else null</summary>
    public static string? Folder(string? baseDir = null)
    {
        string d = Path.Combine(baseDir ?? AppContext.BaseDirectory, "voice", "kokoro");
        bool model = File.Exists(Path.Combine(d, "model.int8.onnx")) || File.Exists(Path.Combine(d, "model.int8.onnx.part1"));
        return File.Exists(Path.Combine(d, "sherpa-onnx-offline-tts.exe")) && model && File.Exists(Path.Combine(d, "voices.bin")) &&
               File.Exists(Path.Combine(d, "tokens.txt")) && Directory.Exists(Path.Combine(d, "espeak-ng-data")) ? d : null;
    }

    /// <summary>the model file: as shipped, or the parts joined once into the local cache (checked by size)</summary>
    public static string? Model(string dir, string cacheDir)
    {
        string whole = Path.Combine(dir, "model.int8.onnx");
        if (File.Exists(whole)) return whole;
        var parts = Enumerable.Range(1, 9).Select(i => Path.Combine(dir, $"model.int8.onnx.part{i}")).TakeWhile(File.Exists).ToList();
        if (parts.Count == 0) return null;
        long size = parts.Sum(p => new FileInfo(p).Length);
        string cached = Path.Combine(cacheDir, "model.int8.onnx");
        if (File.Exists(cached) && new FileInfo(cached).Length == size) return cached;
        Directory.CreateDirectory(cacheDir);
        using (var o = File.Create(cached + ".tmp"))
            foreach (var p in parts) using (var i = File.OpenRead(p)) i.CopyTo(o);
        File.Move(cached + ".tmp", cached, true);
        return cached;
    }

    public static KokoroVoice? TryStart(string dir)
    {
        try
        {
            string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "re-c", "kokoro");
            string? model = Model(dir, cache);
            if (model == null) { NeuralVoice.Log("kokoro: no model"); return null; }
            string tmp = Path.Combine(Path.GetTempPath(), "re-c-voice");
            Directory.CreateDirectory(tmp);
            NeuralVoice.Log($"kokoro: model {model}");
            return new KokoroVoice(Path.Combine(dir, "sherpa-onnx-offline-tts.exe"), dir, model, tmp);
        }
        catch (Exception e) { NeuralVoice.Log("kokoro start failed: " + e.Message); return null; }
    }

    /// <summary>(speaker, speech speed for --kokoro-length-scale (above 1 = slower), pitch = replay speed) for an NPC:
    /// the sex and age from Cast, the speaker and a small spread from the NPC id (each NPC keeps one voice)</summary>
    public static (int Speaker, double Length, double Pitch) Pick(int npc)
    {
        var cls = Cast.Of(npc);
        uint h = (uint)(npc + 2) * 2654435761u;
        double spread = 0.97 + 0.06 * ((h >> 12 & 255) / 255.0);
        bool man = cls is VoiceClass.Man or VoiceClass.OldMan || (cls == VoiceClass.Creature && (h & 1) != 0);
        int[] pool = man ? Male : Female;
        if (cls == VoiceClass.Narrator) return (9, 1.05, 1.0);                       // bm_george: a calm storyteller
        int spk = pool[(int)(h >> 16) % pool.Length];
        return cls switch
        {
            VoiceClass.OldMan or VoiceClass.OldWoman => (spk, 1.15, 0.9 * spread),
            VoiceClass.Boy => (spk, 0.95, 1.16),
            VoiceClass.Girl => (spk, 0.95, 1.22),
            VoiceClass.Creature => (spk, 1.0, (h & 2) != 0 ? 1.4 : 0.75),
            _ => (spk, 1.0, spread),
        };
    }

    public void Say(int npc, string text)
    {
        var (spk, len, pitch) = Pick(npc);
        int seq;
        Process? old;
        lock (_lk) { seq = ++_seq; _next = null; _clear = true; old = _running; _running = null; }
        try { if (old != null && !old.HasExited) old.Kill(); } catch (Exception) { }
        string wav = Path.Combine(_tmp, $"k{seq}.wav");
        var psi = new ProcessStartInfo(_exe)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = _dir,
        };
        foreach (var a in new[]
        {
            "--kokoro-model=" + _model, "--kokoro-voices=" + Path.Combine(_dir, "voices.bin"),
            "--kokoro-tokens=" + Path.Combine(_dir, "tokens.txt"), "--kokoro-data-dir=" + Path.Combine(_dir, "espeak-ng-data"),
            FormattableString.Invariant($"--kokoro-length-scale={len:0.00}"), "--num-threads=2", $"--sid={spk}",
            "--output-filename=" + wav, text,
        }) psi.ArgumentList.Add(a);
        var t = new Thread(() => Run(psi, seq, wav, pitch)) { IsBackground = true, Name = "kokoro" };
        t.Start();
    }

    private void Run(ProcessStartInfo psi, int seq, string wav, double pitch)
    {
        try
        {
            using var p = Process.Start(psi);
            if (p == null) { NeuralVoice.Log("kokoro: Process.Start returned null"); return; }
            lock (_lk) { if (seq != _seq) { try { p.Kill(); } catch (Exception) { } return; } _running = p; }
            string err = p.StandardError.ReadToEnd();
            p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            lock (_lk) if (_running == p) _running = null;
            if (p.ExitCode != 0 || !File.Exists(wav))
            {
                NeuralVoice.Log($"kokoro: exit 0x{p.ExitCode:X8} " + err.Trim().Split('\n').LastOrDefault());
                return;
            }
            float[] pcm = NeuralVoice.Render(File.ReadAllBytes(wav), pitch, AudioOut.Rate);
            try { File.Delete(wav); } catch (Exception) { }
            lock (_lk) if (seq == _seq) _next = pcm;
        }
        catch (Exception e) { NeuralVoice.Log("kokoro: " + e.Message); }
    }

    public void Stop()
    {
        Process? old;
        lock (_lk) { _seq++; _next = null; _clear = true; old = _running; _running = null; }
        try { if (old != null && !old.HasExited) old.Kill(); } catch (Exception) { }
    }

    public void BeginBlock()
    {
        lock (_lk)
        {
            if (_clear) { _cur = Array.Empty<float>(); _pos = 0; _clear = false; }
            if (_next != null) { _cur = _next; _pos = 0; _next = null; }
        }
    }

    public (float V, float Duck) Next()
    {
        bool on = _pos < _cur.Length;
        _duck += ((on ? 0.5f : 1f) - _duck) * (on ? 0.002f : 0.0005f);
        return (on ? _cur[_pos++] * 1.15f : 0f, _duck);
    }

    public void Dispose() => Stop();
}
