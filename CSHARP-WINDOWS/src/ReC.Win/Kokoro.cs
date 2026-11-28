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
    private readonly Queue<float[]> _queue = new();        // sentences ready to play, in order
    private float[] _cur = Array.Empty<float>();
    private int _pos;
    private bool _clear;
    private float _duck = 1;
    public string Name => _serverDead ? "KOKORO (PER PAGE)" : "KOKORO (SERVER)";
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
            var kv = new KokoroVoice(Path.Combine(dir, "sherpa-onnx-offline-tts.exe"), dir, model, tmp);
            kv.StartServer();                          // playtest 10: the model loads now, while the intro plays
            return kv;
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

    /// <summary>playtest 9: Kokoro failed on this PC (both argument sets): the window switches to Piper for good</summary>
    public volatile bool Failed;
    /// <summary>the page that failed, for the voice taking over: (npc, text)</summary>
    public (int Npc, string Text)? FailedPage;
    private bool _minimal;                                // the plain argument set worked better (retry succeeded)

    private ProcessStartInfo Args(int spk, double len, string wav, string text, bool minimal)
    {
        var psi = new ProcessStartInfo(_exe)
        {
            UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true,
            WorkingDirectory = _dir,
        };
        var a = new List<string>
        {
            "--kokoro-model=" + _model, "--kokoro-voices=" + Path.Combine(_dir, "voices.bin"),
            "--kokoro-tokens=" + Path.Combine(_dir, "tokens.txt"), "--kokoro-data-dir=" + Path.Combine(_dir, "espeak-ng-data"),
        };
        if (!minimal) { a.Add(FormattableString.Invariant($"--kokoro-length-scale={len:0.00}")); a.Add("--num-threads=2"); }
        a.Add($"--sid={spk}"); a.Add("--output-filename=" + wav); a.Add(text);
        foreach (var s in a) psi.ArgumentList.Add(s);
        return psi;
    }

    // ---- playtest 10: the server (`ReC.exe --tts-server`, KokoroServer) keeps the model loaded; each page is split into
    // sentences, so the first one plays while the rest are made. Fallback: one sherpa-onnx-offline-tts.exe per page.
    private Process? _server;
    private StreamWriter? _serverIn;
    private volatile bool _serverDead;
    private readonly Dictionary<string, (int Seq, double Pitch, int Idx, string Cache)> _pending = new();
    // playtest 11: sentences of the current page by index, played in order as they come in (cache hits are instant)
    private readonly Dictionary<int, float[]> _ready = new();
    // playtest 13: sentences being made (cache path -> the pages waiting for them)
    private readonly Dictionary<string, List<(int Seq, int Idx, double Pitch)>> _inflight = new();
    private int _nextIdx;
    private static string CacheDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "re-c", "voicecache");

    /// <summary>the cache file of one sentence in one voice (speaker, speed); the pitch is applied on playback</summary>
    public static string CachePath(int spk, double len, string text)
    {
        ulong h = 1469598103934665603;
        foreach (char c in FormattableString.Invariant($"{spk}|{len:0.000}|{text}")) { h ^= c; h *= 1099511628211; }
        return Path.Combine(CacheDir, $"{h:x16}.wav");
    }

    /// <summary>(lock held) a sentence is ready: queue it and any that follow it in order</summary>
    private void Ready(int idx, float[] pcm)
    {
        _ready[idx] = pcm;
        while (_ready.Remove(_nextIdx, out var p)) { _queue.Enqueue(p); _nextIdx++; }
    }
    private (int Npc, string Text, int Seq)? _lastPage;

    /// <summary>the C API DLLs for the server are in the voice folder (shared build)</summary>
    public static bool ServerFiles(string dir) =>
        File.Exists(Path.Combine(dir, "sherpa-onnx-c-api.dll")) && File.Exists(Path.Combine(dir, "onnxruntime.dll"));

    private void StartServer()
    {
        try
        {
            if (!ServerFiles(_dir) || Environment.ProcessPath is not string self) { _serverDead = true; return; }
            var psi = new ProcessStartInfo(self)
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true, RedirectStandardOutput = true,
                RedirectStandardError = true, WorkingDirectory = _dir,
            };
            foreach (var a in new[] { "--tts-server", _dir, _model }) psi.ArgumentList.Add(a);
            var p = Process.Start(psi);
            if (p == null) { _serverDead = true; return; }
            p.StandardInput.AutoFlush = true;
            _server = p; _serverIn = p.StandardInput;
            p.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) NeuralVoice.Log("kokoro server: " + e.Data); };
            p.BeginErrorReadLine();
            new Thread(ServerLoop) { IsBackground = true, Name = "kokoro-server" }.Start();
            NeuralVoice.Log("kokoro: server started (the model stays loaded)");
        }
        catch (Exception e) { NeuralVoice.Log("kokoro server start: " + e.Message); _serverDead = true; }
    }

    private void ServerLoop()
    {
        try
        {
            string? line;
            while (_server != null && (line = _server.StandardOutput.ReadLine()) != null)
            {
                if (line.StartsWith("READY")) { NeuralVoice.Log("kokoro server: " + line); continue; }
                if (line.StartsWith("ERR") && !line.Contains(".wav")) { NeuralVoice.Log("kokoro server: " + line); break; }
                int sp = line.IndexOf(' ');
                if (sp < 0) continue;
                string rest = line[(sp + 1)..], wav = rest.Contains(".wav") ? rest[..(rest.IndexOf(".wav") + 4)] : rest;
                (int Seq, double Pitch, int Idx, string Cache) req;
                lock (_lk) { if (!_pending.Remove(wav, out req)) continue; }
                if (line.StartsWith("OK"))
                {
                    byte[] bytes = File.ReadAllBytes(wav);
                    float[] pcm = NeuralVoice.Render(bytes, req.Pitch, AudioOut.Rate);
                    try { Directory.CreateDirectory(CacheDir); File.Move(wav, req.Cache, true); } catch (Exception) { try { File.Delete(wav); } catch (Exception) { } }
                    lock (_lk)
                    {
                        if (req.Seq == _seq && req.Idx >= 0) Ready(req.Idx, pcm);
                        if (_inflight.Remove(req.Cache, out var waiters))          // a page that came while it was made
                            foreach (var w in waiters) if (w.Seq == _seq) Ready(w.Idx, NeuralVoice.Render(bytes, w.Pitch, AudioOut.Rate));
                    }
                }
                else if (line.StartsWith("ERR"))
                {
                    NeuralVoice.Log("kokoro server: " + line);
                    lock (_lk) _inflight.Remove(req.Cache);
                }
            }
        }
        catch (Exception e) { NeuralVoice.Log("kokoro server read: " + e.Message); }
        _serverDead = true;
        NeuralVoice.Log("kokoro: the server stopped; one program per page from now on");
        // the page still being made is said again the old way
        (int Npc, string Text, int Seq)? last;
        lock (_lk) last = _lastPage;
        if (last is { } lp && lp.Seq == _seq) Say(lp.Npc, lp.Text);
    }

    /// <summary>a page split after . ! ? (and the game's ellipsis), so speech starts after the first sentence</summary>
    public static List<string> Sentences(string text)
    {
        var parts = System.Text.RegularExpressions.Regex.Split(text.Trim(), @"(?<=[.!?])\s+")
            .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        // very short pieces ride along with the next one ("Hey!!" "There's a guy ...")
        var o = new List<string>();
        foreach (var s in parts)
            if (o.Count > 0 && o[^1].Length < 12) o[^1] += " " + s; else o.Add(s);
        if (o.Count == 0) o.Add(text);
        // playtest 11: a long first sentence starts at its first comma, so the voice is heard sooner
        int cm = o[0].IndexOf(", ");
        if (o[0].Length > 45 && cm >= 12 && cm < o[0].Length - 12) { string a = o[0][..(cm + 1)], b = o[0][(cm + 2)..]; o[0] = a; o.Insert(1, b); }
        return o;
    }

    private int _pf;

    /// <summary>playtest 13: makes every sentence of a message's pages ahead (MessageText, at the talk start): seq 0
    /// requests the server never skips; results go to the cache, and a page shown meanwhile waits for them</summary>
    public void Prefetch(int npc, IReadOnlyList<string> pages)
    {
        if (_server == null && !_serverDead) StartServer();
        if (_serverDead || _serverIn == null) return;
        var (spk, len, pitch) = Pick(npc);
        try
        {
            foreach (var page in pages)
                foreach (var s in Sentences(page))
                {
                    string cache = CachePath(spk, len, s);
                    if (File.Exists(cache)) continue;
                    string wav = Path.Combine(_tmp, $"p{++_pf}.wav");
                    lock (_lk)
                    {
                        if (_inflight.ContainsKey(cache)) continue;
                        _inflight[cache] = new List<(int, int, double)>();
                        _pending[wav] = (0, pitch, -1, cache);
                    }
                    _serverIn.WriteLine(FormattableString.Invariant($"0\t{spk}\t{1 / len:0.000}\t{wav}\t{s.Replace('\t', ' ')}"));
                }
        }
        catch (Exception e) { NeuralVoice.Log("kokoro prefetch: " + e.Message); _serverDead = true; }
    }

    public void Say(int npc, string text)
    {
        var (spk, len, pitch) = Pick(npc);
        int seq;
        Process? old;
        lock (_lk) { seq = ++_seq; _queue.Clear(); _clear = true; old = _running; _running = null; _lastPage = (npc, text, seq); }
        try { if (old != null && !old.HasExited) old.Kill(); } catch (Exception) { }
        if (_server == null && !_serverDead) StartServer();
        if (!_serverDead && _serverIn != null)
        {
            try
            {
                var ss = Sentences(text);
                lock (_lk) { _ready.Clear(); _nextIdx = 0; }
                for (int i = 0; i < ss.Count; i++)
                {
                    string cache = CachePath(spk, len, ss[i]);
                    if (File.Exists(cache))
                    {
                        try { var pc = NeuralVoice.Render(File.ReadAllBytes(cache), pitch, AudioOut.Rate); lock (_lk) Ready(i, pc); continue; }
                        catch (Exception) { }
                    }
                    lock (_lk)
                    {
                        if (_inflight.TryGetValue(cache, out var ws)) { ws.Add((seq, i, pitch)); continue; }   // prefetch is making it
                        _inflight[cache] = new List<(int, int, double)>();
                    }
                    string wav = Path.Combine(_tmp, $"k{seq}_{i}.wav");
                    lock (_lk) _pending[wav] = (seq, pitch, i, cache);
                    _serverIn.WriteLine(FormattableString.Invariant($"{seq}\t{spk}\t{1 / len:0.000}\t{wav}\t{ss[i].Replace('\t', ' ')}"));
                }
                return;
            }
            catch (Exception e) { NeuralVoice.Log("kokoro server write: " + e.Message); _serverDead = true; }
        }
        string wav1 = Path.Combine(_tmp, $"k{seq}.wav");
        var t = new Thread(() =>
        {
            int r = Run(Args(spk, len, wav1, text, _minimal), seq, wav1, pitch);
            if (r == 1 && !_minimal)                      // retry once with only the basic options
            {
                NeuralVoice.Log("kokoro: retry with the basic options");
                r = Run(Args(spk, len, wav1, text, true), seq, wav1, pitch);
                if (r == 0) _minimal = true;
            }
            if (r == 1) { FailedPage = (npc, text); Failed = true; }
        }) { IsBackground = true, Name = "kokoro" };
        t.Start();
    }

    /// <summary>0 = spoken (or superseded), 1 = the program failed</summary>
    private int Run(ProcessStartInfo psi, int seq, string wav, double pitch)
    {
        try
        {
            using var p = Process.Start(psi);
            if (p == null) { NeuralVoice.Log("kokoro: Process.Start returned null"); return 1; }
            lock (_lk) { if (seq != _seq) { try { p.Kill(); } catch (Exception) { } return 0; } _running = p; }
            var errTask = p.StandardError.ReadToEndAsync();
            string outText = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            string err = errTask.Result;
            bool killed;
            lock (_lk) { killed = _running != p; if (_running == p) _running = null; }
            if (killed || seq != _seq) return 0;          // a newer page / Stop() ended it
            if (p.ExitCode != 0 || !File.Exists(wav))
            {
                // the whole output (the program echoes its command line last, the reason comes before it)
                NeuralVoice.Log($"kokoro: exit 0x{p.ExitCode:X8}\n--- stderr ---\n{err.Trim()}\n--- stdout ---\n{outText.Trim()}\n---");
                return 1;
            }
            float[] pcm = NeuralVoice.Render(File.ReadAllBytes(wav), pitch, AudioOut.Rate);
            try { File.Delete(wav); } catch (Exception) { }
            lock (_lk) if (seq == _seq) _queue.Enqueue(pcm);
            return 0;
        }
        catch (Exception e) { NeuralVoice.Log("kokoro: " + e.Message); return 1; }
    }

    public void Stop()
    {
        Process? old;
        lock (_lk) { _seq++; _queue.Clear(); _clear = true; old = _running; _running = null; }
        try { if (old != null && !old.HasExited) old.Kill(); } catch (Exception) { }
    }

    public void BeginBlock()
    {
        lock (_lk)
        {
            if (_clear) { _cur = Array.Empty<float>(); _pos = 0; _clear = false; }
            if (_pos >= _cur.Length && _queue.Count > 0) { _cur = _queue.Dequeue(); _pos = 0; }
        }
    }

    public (float V, float Duck) Next()
    {
        if (_pos >= _cur.Length) lock (_lk) if (!_clear && _queue.Count > 0) { _cur = _queue.Dequeue(); _pos = 0; }   // the next sentence
        bool on = _pos < _cur.Length;
        _duck += ((on ? 0.5f : 1f) - _duck) * (on ? 0.002f : 0.0005f);
        return (on ? _cur[_pos++] * 1.15f : 0f, _duck);
    }

    public void Dispose()
    {
        Stop();
        try { _serverIn?.Close(); if (_server != null && !_server.WaitForExit(500)) _server.Kill(); } catch (Exception) { }
        _server?.Dispose(); _server = null;
    }
}
