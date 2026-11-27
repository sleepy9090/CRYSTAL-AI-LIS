// re-c Windows host — spoken dialogue (playtest 6: "when the text from the first npc occurs, make it audio as well").
// Not in the web port.
//
//   DialogueReader  the page the game's text printer built in WRAM $6000 (TextPrint 20:853A: 8 rows x 32 tiles, each row
//                   FE 1E, 29 characters, 1F; TextClearBuf 20:8720), read while a message window is open in gameplay
//                   (main_state 1, irq_mode 1 / 9: ShowMessage 30:D354). A page counts once it has stayed the same for
//                   Settle frames (TextPrint can span a few frames while the upload queue drains); each page is said
//                   once, a new page replaces the one being said, and closing the window stops the voice.
//   Speech          Windows' built-in text-to-speech (SAPI 5 SpVoice over raw COM, no packages); null when the PC has
//                   no speech engine (then the game simply stays silent).
using System.Text;

namespace ReC.Win;

internal sealed class DialogueReader
{
    public const int Buf = 0x6000, Settle = 4;
    private string _pending = "", _said = "";
    private int _stable;
    private bool _open;

    /// <summary>the page in the text buffer as words, or "" when the buffer is not a text page</summary>
    public static string Page(Func<int, int> rd)
    {
        var sb = new StringBuilder();
        for (int row = 0; row < 8; row++)
        {
            int o = Buf + row * 32;
            int b0 = rd(o);
            if ((b0 != 0xFE && b0 != 0x40) || rd(o + 1) != 0x1E || rd(o + 31) != 0x1F) return "";   // the frame, attributes, a map buffer
            var line = new StringBuilder();
            for (int x = 2; x < 31; x++)
            {
                int c = rd(o + x);
                if (c == 0x5F) line.Append("...");                                        // the game's ellipsis tile
                else if (c is >= 0x20 and < 0x7F && (char.IsAsciiLetterOrDigit((char)c) || " !\"&',-.:;?".Contains((char)c))) line.Append((char)c);
                else line.Append(' ');                                                     // icons, arrows, the page prompt
            }
            string t = line.ToString().Trim();
            if (t.Length == 0) continue;
            if (sb.Length > 0)
            {
                if (sb[^1] == '-' && char.IsLetter(t[0])) sb.Length--;                     // a word broken over two lines
                else sb.Append(' ');
            }
            sb.Append(t);
        }
        string s = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), " {2,}", " ").Trim();
        return s.Any(char.IsLetter) ? s : "";
    }

    /// <summary>after a frame: (text to say now or null, whether to stop the voice)</summary>
    public (string? Say, bool Stop) Update(int mainState, int irqMode, Func<int, int> rd)
    {
        bool open = mainState == 1 && irqMode is 1 or 9;
        if (!open)
        {
            bool stop = _open;
            _open = false; _pending = ""; _said = ""; _stable = 0;
            return (null, stop);
        }
        _open = true;
        string page = Page(rd);
        if (page != _pending) { _pending = page; _stable = 0; return (null, false); }
        if (page.Length == 0 || page == _said || ++_stable < Settle) return (null, false);
        _said = page;
        return (page, false);
    }
}

/// <summary>who says a page: the NPC being talked to (sub_state $11 NpcTalk 30:D0C8, talk slot $0623, spawn handler
/// $0540 & 3 = 1, NPC id p = $0560 + slot, the NpcScript 14:8000 index), else the narrator</summary>
internal enum VoiceClass { Narrator, Man, Woman, OldMan, OldWoman, Boy, Girl, Creature }

internal static class Cast
{
    /// <summary>playtest 6: every talking NPC (spawn handler 1, type $30; 125 ids) by how its sprite looks, classified by
    /// eye from `tools/npc_sheet.py` (both facings, each in its first area's CHR and palettes): long hair / gowns =
    /// women, beards / short hair = men, bald or bearded elders = old, the small figures and pigtails = children,
    /// non-human shapes = creatures. %APPDATA%\re-c\voices.ini (`npc_16=oldman` ..) overrides any entry.</summary>
    private const string Table =
        "00M 01O 02F 03F 04F 05F 06F 07M 08M 09F 0AF 0BM 0CM 0DO 0EF 0FM 10F 11B 12G 13C 14O 15M 16O 17F " +
        "18F 19F 1AF 1BM 1CM 1DC 1EC 1FC 20C 21C 22C 23C 24F 25F 26B 27B 28M 29F 2AF 2BO 2CM 2DM 2EM 2FM " +
        "30M 32O 33O 34O 35M 36F 37G 38F 39F 3AM 3DM 3EF 3FF 40M 41M 42M 43F 44M 45M 46M 47G 48M 49O 4AM " +
        "4BF 4CM 4DM 4EM 4FO 50F 51O 52G 53M 54M 55M 56F 57M 58F 59C 5AC 5BO 5CG 5DC 5EO 5FM 60O 61C 62F " +
        "63C 64M 65M 68M 6BM 6CM 6DM 6EO 6FM 70C 71C 72C 75C 76M 77M 78F 79F 7AF 7BF 7CC 7DC 7EM 80M 81M " +
        "83O 84F 85M 88M 8EF";

    private static readonly Dictionary<int, VoiceClass> Npcs = Parse();
    public static int Count => Npcs.Count;

    private static Dictionary<int, VoiceClass> Parse()
    {
        var d = new Dictionary<int, VoiceClass>();
        foreach (var e in Table.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            d[Convert.ToInt32(e[..2], 16)] = e[2] switch
            {
                'M' => VoiceClass.Man, 'F' => VoiceClass.Woman, 'O' => VoiceClass.OldMan, 'W' => VoiceClass.OldWoman,
                'B' => VoiceClass.Boy, 'G' => VoiceClass.Girl, 'C' => VoiceClass.Creature, _ => VoiceClass.Narrator,
            };
        return d;
    }

    /// <summary>voices.ini lines `npc_XX=man|woman|oldman|oldwoman|boy|girl|creature|narrator` (XX = NPC id, hex)</summary>
    public static int LoadOverrides(string path)
    {
        int n = 0;
        try
        {
            if (!File.Exists(path)) return 0;
            foreach (var raw in File.ReadAllLines(path))
            {
                var kv = raw.Split('=', 2);
                if (kv.Length != 2 || !kv[0].Trim().StartsWith("npc_", StringComparison.OrdinalIgnoreCase)) continue;
                if (!int.TryParse(kv[0].Trim()[4..], System.Globalization.NumberStyles.HexNumber, null, out int id)) continue;
                if (!Enum.TryParse(kv[1].Trim(), true, out VoiceClass c)) continue;
                Npcs[id & 0xFF] = c; n++;
            }
        }
        catch (Exception) { }
        return n;
    }

    public static VoiceClass Of(int npc) => npc >= 0 && Npcs.TryGetValue(npc, out var c) ? c : VoiceClass.Narrator;

    /// <summary>the speaker's NPC id after a frame, or -1 (not a conversation with a talking NPC)</summary>
    public static int Speaker(Func<int, int> rd)
    {
        if (rd(0x41) != 0x11) return -1;                     // sub_state $11: NpcTalk
        int slot = rd(0x623);
        if (slot is < 0x0D or >= 0x20 || (rd(0x540 + slot) & 3) != 1) return -1;
        return rd(0x560 + slot);
    }

    private static readonly string[] Male = { "Microsoft David", "Microsoft Mark", "Microsoft George", "Microsoft Guy" };
    private static readonly string[] Female = { "Microsoft Zira", "Microsoft Hazel", "Microsoft Susan", "Microsoft Aria" };

    /// <summary>the SAPI XML for a page in a speaker's voice: gender (required, any installed voice of it), a preferred
    /// voice name and pitch / speed by age, varied per NPC so no two neighbours sound alike</summary>
    public static string Xml(int npc, string text)
    {
        var cls = Of(npc);
        uint h = (uint)(npc < 0 ? 0 : npc + 1) * 2654435761u;
        int v1 = (int)(h >> 8 & 7), v2 = (int)(h >> 16 & 3), pick = (int)(h >> 24);
        string gender; int pitch, rate;
        switch (cls)
        {
            case VoiceClass.Man: gender = "Male"; pitch = v1 - 3; rate = v2 - 1; break;                // -3..4, -1..2
            case VoiceClass.Woman: gender = "Female"; pitch = v1 - 3; rate = v2 - 1; break;
            case VoiceClass.OldMan: gender = "Male"; pitch = -8 + (v1 >> 1); rate = -3 + (v2 >> 1); break;   // low, slow
            case VoiceClass.OldWoman: gender = "Female"; pitch = -6 + (v1 >> 1); rate = -3 + (v2 >> 1); break;
            case VoiceClass.Boy: gender = "Female"; pitch = 5 + (v1 >> 1); rate = 1 + (v2 >> 1); break;     // high, brisk
            case VoiceClass.Girl: gender = "Female"; pitch = 8 + (v1 >> 2); rate = 1 + (v2 >> 1); break;
            case VoiceClass.Creature: gender = (h & 1) != 0 ? "Male" : "Female"; pitch = (h & 2) != 0 ? 10 : -10; rate = 2 + (v2 >> 1); break;
            default: gender = ""; pitch = 0; rate = 0; break;
        }
        string body = System.Security.SecurityElement.Escape(text) ?? "";
        string inner = $"<pitch absmiddle=\"{pitch}\"><rate absspeed=\"{rate}\">{body}</rate></pitch>";
        if (gender.Length == 0) return inner;
        var names = gender == "Male" ? Male : Female;
        return $"<voice required=\"Gender={gender}\" optional=\"Name={names[pick % names.Length]}\">{inner}</voice>";
    }
}

internal sealed unsafe class Speech : IDisposable
{
    private void* _v;
    private const uint SpfAsync = 1, SpfPurge = 2, SpfIsXml = 8, SpfNotXml = 0x10;

    private Speech(void* v) { _v = v; }

    private static void** Vt(void* o) => *(void***)o;

    /// <summary>SpVoice, or null when no speech engine is installed / COM fails</summary>
    public static Speech? TryCreate()
    {
        try
        {
            Win32.CoInitializeEx(null, 2);                                   // apartment; "changed mode" is fine too
            Guid clsid = new("96749377-3391-11D2-9EE3-00C04F797396"), iid = new("6C44DF74-72B9-4992-A1EC-EF996E0422D4");
            void* v;
            if (Win32.CoCreateInstance(&clsid, null, 0x17, &iid, &v) < 0 || v == null) return null;
            return new Speech(v);
        }
        catch (Exception) { return null; }
    }

    /// <summary>ISpVoice::SetVolume (0-100)</summary>
    public void Volume(int pct)
    {
        if (_v != null) ((delegate* unmanaged[Stdcall]<void*, ushort, int>)Vt(_v)[30])(_v, (ushort)Math.Clamp(pct, 0, 100));
    }

    /// <summary>says the text at once, cutting off anything still being said (ISpVoice::Speak, asynchronous)</summary>
    public void Say(string text)
    {
        if (_v == null) return;
        fixed (char* p = text)
            ((delegate* unmanaged[Stdcall]<void*, char*, uint, uint*, int>)Vt(_v)[20])(_v, p, SpfAsync | SpfPurge | SpfNotXml, null);
    }

    /// <summary>says SAPI XML (voice / pitch / rate tags from Cast.Xml), cutting off anything still being said</summary>
    public void SayXml(string xml)
    {
        if (_v == null) return;
        fixed (char* p = xml)
            ((delegate* unmanaged[Stdcall]<void*, char*, uint, uint*, int>)Vt(_v)[20])(_v, p, SpfAsync | SpfPurge | SpfIsXml, null);
    }

    public void Stop() => Say("");

    public void Dispose()
    {
        if (_v == null) return;
        Stop();
        ((delegate* unmanaged[Stdcall]<void*, uint>)Vt(_v)[2])(_v);
        _v = null;
    }
}

/// <summary>playtest 6: the neural dialogue voice. Piper (MIT, offline ONNX text-to-speech; `voice\piper\piper.exe` next
/// to ReC.exe) with the multi-speaker libritts_r model (904 LibriTTS-R readers, CC BY 4.0) runs as one hidden child
/// process in JSON mode: each page is a line {text, speaker_id, output_file}, Piper prints the WAV's path when done.
/// Every NPC gets a reader of the right sex (voice\speakers.txt, from LibriSpeech SPEAKERS.TXT) and an age by pitch:
/// the WAV is replayed faster / slower (children up, elders down). Mixed into AudioOut after the resampler, with the
/// game ducked while someone speaks. A newer page or Stop() drops what is playing or still being made.</summary>
internal sealed class NeuralVoice : IDisposable
{
    public const string ModelName = "en_US-libritts_r-medium.onnx";
    private readonly System.Diagnostics.Process _p;
    private readonly string _tmp;
    private readonly int[] _male, _female;
    private readonly object _lk = new();
    private int _seq;
    private float[]? _next;                     // a finished page waiting for the audio thread
    private float[] _cur = Array.Empty<float>();
    private int _pos;
    private bool _clear;
    private float _duck = 1;
    /// <summary>a page came back from Piper at least once</summary>
    public bool Said;

    private NeuralVoice(System.Diagnostics.Process p, string tmp, int[] male, int[] female)
    {
        _p = p; _tmp = tmp; _male = male; _female = female;
        var t = new Thread(ReadLoop) { IsBackground = true, Name = "voice" };
        t.Start();
    }

    /// <summary>the voice folder next to the exe when it holds Piper, the model and the speaker list, else null</summary>
    public static string? Folder(string? baseDir = null)
    {
        string d = Path.Combine(baseDir ?? AppContext.BaseDirectory, "voice");
        return File.Exists(Path.Combine(d, "piper", "piper.exe")) && File.Exists(Path.Combine(d, ModelName)) &&
               File.Exists(Path.Combine(d, ModelName + ".json")) && File.Exists(Path.Combine(d, "speakers.txt")) ? d : null;
    }

    /// <summary>speaker indices by sex from speakers.txt (`index sex reader`)</summary>
    public static (int[] Male, int[] Female) Speakers(string path)
    {
        var m = new List<int>(); var f = new List<int>();
        foreach (var line in File.ReadAllLines(path))
        {
            var p = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (p.Length < 2 || line.StartsWith('#') || !int.TryParse(p[0], out int i)) continue;
            (p[1] == "M" ? m : f).Add(i);
        }
        return (m.ToArray(), f.ToArray());
    }

    /// <summary>(Piper speaker index, playback speed = pitch factor) for an NPC id (-1 = the narrator): the sex and age
    /// from Cast, the reader and a small pitch spread from the NPC id, so each NPC keeps one distinct voice</summary>
    public static (int Speaker, double Pitch) Pick(int npc, int[] male, int[] female)
    {
        var cls = Cast.Of(npc);
        uint h = (uint)(npc + 2) * 2654435761u;
        double spread = 0.97 + 0.06 * ((h >> 12 & 255) / 255.0);
        bool man = cls is VoiceClass.Man or VoiceClass.OldMan || (cls == VoiceClass.Creature && (h & 1) != 0);
        int[] pool = man ? male : female;
        if (cls == VoiceClass.Narrator) return (female.Length > 0 ? female[0] : male.Length > 0 ? male[0] : 0, 1.0);
        int spk = pool.Length > 0 ? pool[(int)(h >> 16) % pool.Length] : 0;
        double pitch = cls switch
        {
            VoiceClass.OldMan or VoiceClass.OldWoman => 0.88,
            VoiceClass.Boy => 1.20,
            VoiceClass.Girl => 1.28,
            VoiceClass.Creature => (h & 2) != 0 ? 1.45 : 0.72,
            _ => 1.0,
        };
        return (spk, pitch * spread);
    }

    /// <summary>%APPDATA%\re-c\voice.log: why the neural voice did or did not start (playtest 6: it fell back silently)</summary>
    public static string? LogPath;
    private static int _logLines;

    public static void Log(string s)
    {
        if (LogPath == null || _logLines++ > 400) return;
        try { File.AppendAllText(LogPath, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {s}{Environment.NewLine}"); } catch (Exception) { }
    }

    /// <summary>whether the Piper process is still running (it exits at once when a DLL is missing)</summary>
    public bool Alive { get { try { return !_p.HasExited; } catch (Exception) { return false; } } }
    public string ExitInfo { get { try { return _p.HasExited ? $"exit code 0x{_p.ExitCode:X8}" : "running"; } catch (Exception e) { return e.Message; } } }

    /// <summary>starts Piper hidden (null when the folder is missing or the process fails)</summary>
    public static NeuralVoice? TryStart(string dir)
    {
        try
        {
            var (male, female) = Speakers(Path.Combine(dir, "speakers.txt"));
            string tmp = Path.Combine(Path.GetTempPath(), "re-c-voice");
            Directory.CreateDirectory(tmp);
            foreach (var f in Directory.GetFiles(tmp, "*.wav")) { try { File.Delete(f); } catch (IOException) { } }
            var psi = new System.Diagnostics.ProcessStartInfo(Path.Combine(dir, "piper", "piper.exe"))
            {
                UseShellExecute = false, CreateNoWindow = true, RedirectStandardInput = true,
                RedirectStandardOutput = true, RedirectStandardError = true,
                WorkingDirectory = Path.Combine(dir, "piper"),
            };
            foreach (var a in new[] { "--model", Path.Combine(dir, ModelName), "--json-input", "--output_dir", tmp, "--quiet" })
                psi.ArgumentList.Add(a);
            Log($"start {psi.FileName} model {ModelName}, {male.Length} male + {female.Length} female speakers");
            var p = System.Diagnostics.Process.Start(psi);
            if (p == null) { Log("Process.Start returned null"); return null; }
            p.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Log("piper: " + e.Data); };
            p.BeginErrorReadLine();
            return new NeuralVoice(p, tmp, male, female);
        }
        catch (Exception e) { Log("start failed: " + e.Message); return null; }
    }

    /// <summary>JSON string literal (quotes, backslashes, control characters escaped)</summary>
    public static string Json(string s)
    {
        var sb = new StringBuilder("\"");
        foreach (char c in s)
            sb.Append(c switch { '"' => "\\\"", '\\' => "\\\\", < ' ' => $"\\u{(int)c:x4}", _ => c.ToString() });
        return sb.Append('"').ToString();
    }

    /// <summary>says a page in the NPC's voice (replaces whatever is playing or being made)</summary>
    public void Say(int npc, string text)
    {
        var (spk, pitch) = Pick(npc, _male, _female);
        int seq;
        lock (_lk) { seq = ++_seq; _next = null; _clear = true; }
        string file = Path.Combine(_tmp, $"v{seq}_{(int)Math.Round(pitch * 1000)}.wav");
        try
        {
            _p.StandardInput.WriteLine($"{{\"text\": {Json(text)}, \"speaker_id\": {spk}, \"output_file\": {Json(file)}}}");
            _p.StandardInput.Flush();
        }
        catch (Exception) { }
    }

    public void Stop() { lock (_lk) { _seq++; _next = null; _clear = true; } }

    private void ReadLoop()
    {
        try
        {
            string? line;
            while ((line = _p.StandardOutput.ReadLine()) != null)
            {
                string path = line.Trim();
                string name = Path.GetFileNameWithoutExtension(path);
                var parts = name.TrimStart('v').Split('_');
                if (parts.Length != 2 || !int.TryParse(parts[0], out int seq) || !int.TryParse(parts[1], out int pm)) continue;
                float[]? pcm = null;
                lock (_lk) if (seq != _seq) { TryDelete(path); continue; }
                try { pcm = Render(File.ReadAllBytes(path), pm / 1000.0, AudioOut.Rate); } catch (Exception e) { Log("wav: " + e.Message); }
                if (!Said) { Said = true; Log($"first page spoken ({pcm?.Length ?? 0} samples)"); }
                TryDelete(path);
                lock (_lk) if (seq == _seq && pcm != null) _next = pcm;
            }
        }
        catch (Exception) { }
    }

    private static void TryDelete(string f) { try { File.Delete(f); } catch (Exception) { } }

    /// <summary>a 16-bit PCM WAV replayed at `speed` (pitch and tempo together) at outRate, as floats -1..1, with 5 ms
    /// fades at both ends</summary>
    public static float[] Render(byte[] wav, double speed, int outRate)
    {
        int pos = 12, ch = 1, rate = 22050, off = -1, len = 0;
        while (pos + 8 <= wav.Length)
        {
            string id = Encoding.ASCII.GetString(wav, pos, 4);
            int size = BitConverter.ToInt32(wav, pos + 4);
            if (id == "fmt ") { ch = BitConverter.ToInt16(wav, pos + 10); rate = BitConverter.ToInt32(wav, pos + 12); }
            else if (id == "data") { off = pos + 8; len = Math.Min(size, wav.Length - off); break; }
            pos += 8 + size + (size & 1);
        }
        if (off < 0) return Array.Empty<float>();
        int n = len / (2 * ch);
        double step = rate * speed / outRate;
        int outN = (int)((n - 1) / step);
        var o = new float[Math.Max(0, outN)];
        int fade = outRate / 200;
        for (int i = 0; i < o.Length; i++)
        {
            double t = i * step; int k = (int)t; double fr = t - k;
            float a = BitConverter.ToInt16(wav, off + k * 2 * ch) / 32768f, b = BitConverter.ToInt16(wav, off + (k + 1) * 2 * ch) / 32768f;
            float v = (float)(a + (b - a) * fr);
            if (i < fade) v *= i / (float)fade;
            if (o.Length - i < fade) v *= (o.Length - i) / (float)fade;
            o[i] = v;
        }
        return o;
    }

    /// <summary>audio thread, once per block: takes a finished page / a stop</summary>
    public void BeginBlock()
    {
        lock (_lk)
        {
            if (_clear) { _cur = Array.Empty<float>(); _pos = 0; _clear = false; }
            if (_next != null) { _cur = _next; _pos = 0; _next = null; }
        }
    }

    /// <summary>the next voice sample (-1..1) and the gain for the game under it (eases to 0.5 while speaking)</summary>
    public (float V, float Duck) Next()
    {
        bool on = _pos < _cur.Length;
        _duck += ((on ? 0.5f : 1f) - _duck) * (on ? 0.002f : 0.0005f);
        return (on ? _cur[_pos++] : 0f, _duck);
    }

    public bool Speaking => _pos < _cur.Length;

    public void Dispose()
    {
        try { _p.StandardInput.Close(); if (!_p.WaitForExit(500)) _p.Kill(); } catch (Exception) { }
        _p.Dispose();
    }
}
