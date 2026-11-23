// re-c C# port — headless runner, the twin of `node ../web/test/run.mjs` (same options, CSV columns, PNG, dumps).
//   --frames N            frames to run (default 60)
//   --keys SPEC ...       pad-1 script, SPEC = FRAME:BUTTONS[:LEN] (hold LEN frames, default 2) or @FILE
//   --frame0 N            script/CSV frame number of the port's first frame (default 214; with --load-state the
//                         state's frame0 ?? frame)
//   --dump-screen F.png   last finished frame (--scale S)
//   --shots F,F,... DIR   also write DIR/web_NNNN.png (1x) after each listed frame_no
//   --dump-ram F / --dump-wram F / --dump-ciram F   raw 2KB RAM / 8KB WRAM / 2KB nametable RAM
//   --compare F.csv       one row per frame, tools/emu/compare.py columns
//   --load-state F.json   start from a tools/emu/web_state.py export instead of Reset
//   --data DIR            web/data directory (default: found by walking up from the executable)
//   --quiet               no unported-routine log lines
using System.Diagnostics;
using ReC;
using ReC.Game;
using ReC.Util;

static string Help() =>
    "usage: ReC.Run [--frames N] [--frame0 N] [--keys SPEC..] [--compare F.csv] [--dump-screen F.png] [--scale S]\n" +
    "               [--shots F,F.. DIR] [--dump-ram F] [--dump-wram F] [--dump-ciram F] [--load-state F.json]\n" +
    "               [--data DIR] [--quiet]";

int frames = 60, frame0 = 214, scale = 1;
bool quiet = false, frame0Given = false;
var keySpecs = new List<string>();
string? screen = null, ramOut = null, wramOut = null, ciramOut = null, compare = null, statePath = null, dataDir = null, shotDir = null;
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
            case "-h": case "--help": Console.WriteLine(Help()); return 0;
            default: throw new ArgumentException($"unknown option {k} (--help)");
        }
    }
}
catch (Exception e) when (e is ArgumentException or FormatException)
{
    Console.Error.WriteLine(e.Message);
    return 2;
}

var data = Data.LoadDir(Data.FindDir(dataDir));
var m = new Machine(data, s => { if (!quiet) Console.Error.WriteLine($"[re-c] {s}"); });
m.Reset();
if (statePath != null)
{
    var s = StateFile.Load(statePath);
    m.LoadState(s);
    if (!frame0Given) frame0 = s.Frame0 ?? s.Frame;
}
var keys = Keys.Parse(keySpecs);
var rows = compare != null ? new List<string> { string.Join(',', Csv.CompareColumns()) } : null;
var sw = Stopwatch.StartNew();
for (int i = 0; i < frames; i++)
{
    int f = frame0 + m.Frame;
    m.RunFrame(keys.TryGetValue(f, out int b) ? b : 0);
    rows?.Add(string.Join(',', Csv.CompareRow(m, f)));
    if (shots != null && shots.Contains(f))
        Png.WriteFb(Path.Combine(shotDir!, $"web_{f:D4}.png"), m.Ppu.Fb, data.NesRgb, 1);
}
double dt = sw.Elapsed.TotalSeconds;
if (rows != null) File.WriteAllText(compare!, string.Join('\n', rows) + "\n");
if (screen != null) Png.WriteFb(screen, m.Ppu.Fb, data.NesRgb, scale);
if (ramOut != null) File.WriteAllBytes(ramOut, m.Mem.RamBytes);
if (wramOut != null) File.WriteAllBytes(wramOut, m.Mem.Wram);
if (ciramOut != null) File.WriteAllBytes(ciramOut, m.Ppu.Vram);
string stubs = "{" + string.Join(',', m.StubHits.Select(kv => $"\"{kv.Key}\":{kv.Value}")) + "}";
Console.Error.WriteLine($"frames={frames} (last frame_no={frame0 + m.Frame - 1}) main_state={m.Rd(0x40)} sub_state={m.Rd(0x41)} " +
    $"unported={m.Unported.Count} stubs={stubs} {(dt > 0 ? (frames / dt).ToString("F0") : "-")} fps");
return 0;
