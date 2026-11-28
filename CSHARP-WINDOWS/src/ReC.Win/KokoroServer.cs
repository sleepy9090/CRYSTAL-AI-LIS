// re-c Windows host — the Kokoro voice server (playtest 10: "the playback takes several seconds to start"). Not in the
// web port.
//
// `ReC.exe --tts-server DIR MODEL` (started hidden by KokoroVoice): loads sherpa-onnx's C API (sherpa-onnx-c-api.dll +
// onnxruntime.dll from DIR, the v1.13.8 shared MT build) and the Kokoro model ONCE, prints "READY <rate>", then reads
// requests from stdin, one per line: seq \t sid \t speed \t wav path \t text. Each request is synthesised
// (SherpaOnnxOfflineTtsGenerate) into a 16-bit mono WAV and answered "OK <path>" (or "ERR <path> <reason>"). Requests of
// an older seq than the newest seen are skipped ("SKIP <path>"): a new page drops what is still queued for the last.
// A crash here only ends this child process; the game then falls back to one sherpa-onnx-offline-tts.exe per page.
// Struct layouts = sherpa-onnx v1.13.8 c-api.h (SherpaOnnxOfflineTtsConfig and its model configs, x64 natural alignment).
using System.Runtime.InteropServices;
using System.Text;

namespace ReC.Win;

internal static unsafe class KokoroServer
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Vits { public IntPtr Model, Lexicon, Tokens, DataDir; public float NoiseScale, NoiseScaleW, LengthScale; public IntPtr DictDir; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Matcha { public IntPtr Acoustic, Vocoder, Lexicon, Tokens, DataDir; public float NoiseScale, LengthScale; public IntPtr DictDir; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Kokoro { public IntPtr Model, Voices, Tokens, DataDir; public float LengthScale; public IntPtr DictDir, Lexicon, Lang; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Kitten { public IntPtr Model, Voices, Tokens, DataDir; public float LengthScale; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Zipvoice { public IntPtr Tokens, Encoder, Decoder, Vocoder, DataDir, Lexicon; public float FeatScale, TShift, TargetRms, GuidanceScale; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Pocket { public IntPtr LmFlow, LmMain, Encoder, Decoder, TextConditioner, VocabJson, TokenScoresJson; public int CacheCapacity; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Supertonic { public IntPtr DurationPredictor, TextEncoder, VectorEstimator, Vocoder, TtsJson, UnicodeIndexer, VoiceStyle; }
    [StructLayout(LayoutKind.Sequential)]
    private struct ModelConfig
    {
        public Vits Vits; public int NumThreads, Debug; public IntPtr Provider;
        public Matcha Matcha; public Kokoro Kokoro; public Kitten Kitten; public Zipvoice Zipvoice; public Pocket Pocket; public Supertonic Supertonic;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct TtsConfig { public ModelConfig Model; public IntPtr RuleFsts; public int MaxNumSentences; public IntPtr RuleFars; public float SilenceScale; }
    [StructLayout(LayoutKind.Sequential)]
    private struct Audio { public float* Samples; public int N, SampleRate; }

    /// <summary>the layout this file assumes, checked by the tests against c-api.h (x64 sizes)</summary>
    public static (int Vits, int Kokoro, int Model, int Config) Sizes() =>
        (sizeof(Vits), sizeof(Kokoro), sizeof(ModelConfig), sizeof(TtsConfig));

    private static IntPtr U8(string s) => Marshal.StringToCoTaskMemUTF8(s);

    public static int Run(string[] args)
    {
        var o = Console.Out;
        try
        {
            string dir = args[1], model = args[2];
            Win32.SetDllDirectoryW(dir);                               // onnxruntime.dll next to the C API
            IntPtr lib = NativeLibrary.Load(Path.Combine(dir, "sherpa-onnx-c-api.dll"));
            var create = (delegate* unmanaged<TtsConfig*, IntPtr>)NativeLibrary.GetExport(lib, "SherpaOnnxCreateOfflineTts");
            var rate = (delegate* unmanaged<IntPtr, int>)NativeLibrary.GetExport(lib, "SherpaOnnxOfflineTtsSampleRate");
            var gen = (delegate* unmanaged<IntPtr, IntPtr, int, float, Audio*>)NativeLibrary.GetExport(lib, "SherpaOnnxOfflineTtsGenerate");
            var free = (delegate* unmanaged<Audio*, void>)NativeLibrary.GetExport(lib, "SherpaOnnxDestroyOfflineTtsGeneratedAudio");
            TtsConfig c = default;
            c.Model.Kokoro.Model = U8(model);
            c.Model.Kokoro.Voices = U8(Path.Combine(dir, "voices.bin"));
            c.Model.Kokoro.Tokens = U8(Path.Combine(dir, "tokens.txt"));
            c.Model.Kokoro.DataDir = U8(Path.Combine(dir, "espeak-ng-data"));
            c.Model.Kokoro.LengthScale = 1;
            c.Model.NumThreads = Math.Clamp(Environment.ProcessorCount / 2, 2, 6);
            c.Model.Provider = U8("cpu");
            c.MaxNumSentences = 1;
            c.SilenceScale = 0.2f;
            IntPtr tts = create(&c);
            if (tts == IntPtr.Zero) { o.WriteLine("ERR create"); o.Flush(); return 1; }
            // playtest 11: one throwaway sentence first: ONNX Runtime's first run is several times slower than the next ones
            { IntPtr w = U8("Hello there."); Audio* wa = gen(tts, w, 0, 1f); Marshal.FreeCoTaskMem(w); if (wa != null) free(wa); }
            o.WriteLine($"READY {rate(tts)}"); o.Flush();
            // a reader thread keeps the newest seq, so queued requests of an older page are skipped
            var q = new System.Collections.Concurrent.BlockingCollection<string>();
            int newest = 0;
            var rd = new Thread(() =>
            {
                string? line;
                while ((line = Console.In.ReadLine()) != null)
                {
                    int tab = line.IndexOf('\t');
                    if (tab > 0 && int.TryParse(line[..tab], out int s) && s > newest) Volatile.Write(ref newest, s);
                    q.Add(line);
                }
                q.CompleteAdding();
            }) { IsBackground = true };
            rd.Start();
            foreach (var line in q.GetConsumingEnumerable())
            {
                var f = line.Split('\t', 5);
                if (f.Length < 5 || !int.TryParse(f[0], out int seq)) continue;
                string wav = f[3];
                if (seq != 0 && seq < Volatile.Read(ref newest)) { o.WriteLine("SKIP " + wav); o.Flush(); continue; }   // seq 0 = a prefetch: always made
                int sid = int.Parse(f[1]);
                float speed = float.Parse(f[2], System.Globalization.CultureInfo.InvariantCulture);
                IntPtr text = U8(f[4]);
                Audio* a = gen(tts, text, sid, speed);
                Marshal.FreeCoTaskMem(text);
                if (a == null || a->N <= 0) { o.WriteLine("ERR " + wav + " generate"); o.Flush(); if (a != null) free(a); continue; }
                var pcm = new List<short>(a->N);
                for (int i = 0; i < a->N; i++) pcm.Add((short)Math.Clamp(a->Samples[i] * 32767f, -32768f, 32767f));
                int sr = a->SampleRate;
                free(a);
                File.WriteAllBytes(wav, WavOut.WavBytes(pcm, 1, sr));
                o.WriteLine("OK " + wav); o.Flush();
            }
            return 0;
        }
        catch (Exception e) { o.WriteLine("ERR " + e.GetType().Name + ": " + e.Message.Replace('\n', ' ')); o.Flush(); return 1; }
    }
}
