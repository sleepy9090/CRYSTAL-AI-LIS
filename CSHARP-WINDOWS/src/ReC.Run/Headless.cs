// re-c C# port — headless runner, the twin of `node ../web/test/run.mjs` (same options, CSV columns, PNG, dumps).
// Shared by ReC.Run (Linux dotnet) and ReC.Win (`ReC.exe --frames N ...` without a window flag; linked source,
// Win s1 T3), so the shipped exe runs exactly this code.
//   --frames N            frames to run (default 60)
//   --keys SPEC ...       pad-1 script, SPEC = FRAME:BUTTONS[:LEN] (hold LEN frames, default 2) or @FILE
//   --frame0 N            script/CSV frame number of the port's first frame (default 214; with --load-state the
//                         state's frame0 ?? frame)
//   --dump-screen F.png   last finished frame (--scale S)
//   --shots F,F,... DIR   also write DIR/web_NNNN.png (1x) after each listed frame_no
//   --dump-ram F / --dump-wram F / --dump-ciram F   raw 2KB RAM / 8KB WRAM / 2KB nametable RAM
//   --compare F.csv       one row per frame, tools/emu/compare.py columns
//   --load-state F.json   start from a tools/emu/web_state.py export instead of Reset
//   --data DIR            web/data directory (default: the copy embedded in ReC.Core, Win s1 T2)
//   --quiet               no unported-routine log lines
//   --pass-count F        per frame the lag model's stage buckets (= node ../web/test/pass_count.mjs --stages output)
using System.Diagnostics;
using ReC.Game;
using ReC.Util;

namespace ReC.Run;

public static class Headless
{
    /// <summary>Options this runner understands (ReC.Win uses it to decide headless vs window).</summary>
    public static readonly string[] Options =
    {
        "--frames", "--frame0", "--keys", "--dump-screen", "--shots", "--scale", "--dump-ram", "--dump-wram",
        "--dump-ciram", "--compare", "--quiet", "--load-state", "--data", "--pass-count", "--dump-tags",
    };

    public static string Help(string prog) =>
        $"usage: {prog} [--frames N] [--frame0 N] [--keys SPEC..] [--compare F.csv] [--dump-screen F.png] [--scale S]\n" +
        "               [--shots F,F.. DIR] [--dump-ram F] [--dump-wram F] [--dump-ciram F] [--load-state F.json]\n" +
        "               [--data DIR] [--quiet] [--pass-count F] [--dump-tags DIR EVERY]\n" +
        "  --dump-tags: every EVERY frames DIR/tFFFF.bin = Ppu.Tags (61440 int32 LE) + Fb (61440 bytes) + palette (32)\n" +
        "               (Windows port T12, for tools/sprite_pack.py)";

    // = ../web/test/run.mjs (a test script, not an inventory callable: no js tag)
    public static int Run(string[] args, string prog = "ReC.Run")
    {
        int frames = 60, frame0 = 214, scale = 1;
        bool quiet = false, frame0Given = false;
        var keySpecs = new List<string>();
        string? screen = null, ramOut = null, wramOut = null, ciramOut = null, compare = null, statePath = null, dataDir = null, shotDir = null;
        string? passCountOut = null, tagDir = null;
        int tagEvery = 0;
        HashSet<int>? shots = null;
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                string k = args[i];
                string V() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{k} needs a value");
                switch (k)
                {
                    case "--frames": frames = int.Parse(V()); break;
                    case "--frame0": frame0 = int.Parse(V()); frame0Given = true; break;
                    case "--keys": while (i + 1 < args.Length && !args[i + 1].StartsWith("--")) keySpecs.Add(args[++i]); break;
                    case "--dump-screen": screen = V(); break;
                    case "--shots": shots = V().Split(',').Select(int.Parse).ToHashSet(); shotDir = V(); break;
                    case "--scale": scale = int.Parse(V()); break;
                    case "--dump-ram": ramOut = V(); break;
                    case "--dump-wram": wramOut = V(); break;
                    case "--dump-ciram": ciramOut = V(); break;
                    case "--compare": compare = V(); break;
                    case "--quiet": quiet = true; break;
                    case "--load-state": statePath = V(); break;
                    case "--data": dataDir = V(); break;
                    case "--pass-count": passCountOut = V(); break;
                    case "--dump-tags": tagDir = V(); tagEvery = int.Parse(V()); break;
                    case "-h": case "--help": Console.WriteLine(Help(prog)); return 0;
                    default: throw new ArgumentException($"unknown option {k} (--help)");
                }
            }
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            Console.Error.WriteLine(e.Message);
            return 2;
        }

        var data = Data.LoadDefault(dataDir);
        var m = new Machine(data, s => { if (!quiet) Console.Error.WriteLine($"[re-c] {s}"); });
        m.Reset();
        if (statePath != null)
        {
            var s = StateFile.Load(statePath);
            m.LoadState(s);
            if (!frame0Given) frame0 = s.Frame0 ?? s.Frame;
        }
        var keys = Keys.Parse(keySpecs);
        if (tagDir != null) { Directory.CreateDirectory(tagDir); m.Ppu.Tags = new int[256 * 240]; }
        var rows = compare != null ? new List<string> { string.Join(',', Csv.CompareColumns()) } : null;
        var passCount = passCountOut != null ? new List<string>() : null;
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < frames; i++)
        {
            int f = frame0 + m.Frame;
            int lag0 = m.LagFrames;
            m.RunFrame(keys.TryGetValue(f, out int b) ? b : 0);
            rows?.Add(string.Join(',', Csv.CompareRow(m, f)));
            if (passCount != null)
            {
                passCount.Add(PassCountLine(m, f));
                if (m.LagFrames != lag0) passCount.Add($"# LAG {f} (lag model, next frame has no pass)");
            }
            if (tagDir != null && tagEvery > 0 && i % tagEvery == tagEvery - 1)
            {
                var buf = new byte[256 * 240 * 4 + 256 * 240 + 32];
                Buffer.BlockCopy(m.Ppu.Tags!, 0, buf, 0, 256 * 240 * 4);
                Buffer.BlockCopy(m.Ppu.Fb, 0, buf, 256 * 240 * 4, 256 * 240);
                Buffer.BlockCopy(m.Ppu.Pal, 0, buf, 256 * 240 * 5, 32);
                File.WriteAllBytes(Path.Combine(tagDir, $"t{f:D5}.bin"), buf);
            }
            if (shots != null && shots.Contains(f))
                Png.WriteFb(Path.Combine(shotDir!, $"web_{f:D4}.png"), m.Ppu.Fb, data.NesRgb, 1);
        }
        double dt = sw.Elapsed.TotalSeconds;
        if (rows != null) File.WriteAllText(compare!, string.Join('\n', rows) + "\n");
        if (passCount != null) File.WriteAllText(passCountOut!, string.Join('\n', passCount) + "\n");
        if (screen != null) Png.WriteFb(screen, m.Ppu.Fb, data.NesRgb, scale);
        if (ramOut != null) File.WriteAllBytes(ramOut, m.Mem.RamBytes);
        if (wramOut != null) File.WriteAllBytes(wramOut, m.Mem.Wram);
        if (ciramOut != null) File.WriteAllBytes(ciramOut, m.Ppu.Vram);
        string stubs = "{" + string.Join(',', m.StubHits.Select(kv => $"\"{kv.Key}\":{kv.Value}")) + "}";
        Console.Error.WriteLine($"frames={frames} (last frame_no={frame0 + m.Frame - 1}) main_state={m.Rd(0x40)} sub_state={m.Rd(0x41)} " +
            $"unported={m.Unported.Count} stubs={stubs} {(dt > 0 ? (frames / dt).ToString("F0") : "-")} fps");
        return 0;
    }

    // = node ../web/test/pass_count.mjs --stages: the lag model's buckets of this frame (rd/wr/rom), colc, irq_mode and
    // the per-slot dispatch buckets `XX:YY` (hex); `upd` includes the dispatches, as pass_count.mjs prints it
    private static string PassCountLine(Machine m, int f)
    {
        var cnt = m.LagCnt;
        int[] Get(string k) => cnt.TryGetValue(k, out var v) ? v : [0, 0, 0];
        var upd = (int[])Get("upd").Clone();
        var sl = new List<(int X, string S)>();
        foreach (var (k, v) in cnt)
        {
            if (k.Length < 2 || k[0] != 's' || !char.IsDigit(k[1])) continue;
            var p = k[1..].Split(':');
            int x = int.Parse(p[0]), id = int.Parse(p[1]);
            for (int i = 0; i < 3; i++) upd[i] += v[i];
            sl.Add((x, $"{x:X2}:{id:X2}={v[0]}/{v[1]}/{v[2]}"));
        }
        string B(string k, int[] v) => $"{k}={v[0]}/{v[1]}/{v[2]}";
        string line = $"{f} " + string.Join(' ', new[] { "top", "upd", "cam", "spr", "col", "colh", "nmi", "snd" }
            .Select(s => B(s, s == "upd" ? upd : Get(s))))
            + $" colc={(m.LagCyc.TryGetValue("col", out long c) ? c : 0)} im={m.Mem.Rd(0x51):x2}";
        if (sl.Count > 0) line += "  " + string.Join(' ', sl.OrderBy(t => t.X).Select(t => t.S));
        return line;
    }
}
