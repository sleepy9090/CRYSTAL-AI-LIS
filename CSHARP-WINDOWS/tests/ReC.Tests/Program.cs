// re-c C# port — unit tests (docs/architecture_cs.md §5): console project with its own tiny assert helper.
// `dotnet run --project tests/ReC.Tests -c Release [FILTER]` — exit 1 on any failure. Sound tests: SoundTests.cs
// (`-- --wav ID OUT.wav [SECONDS]` = apu_check.mjs --wav).
using System.IO.Compression;
using ReC;
using ReC.Game;
using ReC.Gen;
using ReC.Nes;
using ReC.Util;

namespace ReC.Tests;

internal static partial class Program
{
    private static int _fail, _pass;
    private static readonly Lazy<Data> TheData = new(() => Data.LoadDir(Data.FindDir()));

    private static void Eq<T>(T want, T got, string what)
    {
        if (EqualityComparer<T>.Default.Equals(want, got)) _pass++;
        else { _fail++; Console.WriteLine($"  FAIL {what}: want {want}, got {got}"); }
    }

    private static void True(bool c, string what) => Eq(true, c, what);

    private static readonly (string Name, Action Fn)[] Tests =
    {
        ("crc32", () =>
        {
            Eq(0xCBF43926u, Crc32.Compute("123456789"u8), "crc32 check value");
            Eq(0u, Crc32.Compute(ReadOnlySpan<byte>.Empty), "crc32 empty");
        }),
        ("keys", () =>
        {
            var k = Keys.Parse(new[] { "250:START", "300:A+RIGHT:3 301:B" });
            Eq(0x10, k[250], "START f250"); Eq(0x10, k[251], "START held 2"); False(k.ContainsKey(252), "f252 free");
            Eq(0x81, k[300], "A+RIGHT"); Eq(0xC1, k[301], "A+RIGHT|B"); Eq(0xC1, k[302], "B 2nd frame"); Eq(0x81 | 0, k[300], "or");
            Eq(0, Keys.ParseButtons("NONE"), "NONE");
            Eq(0x80, Keys.Rev8(0x01), "rev8 A");
            Eq(0x05, Keys.Rev8(0xA0), "rev8");
        }),
        ("png", () =>
        {
            var rgb = new byte[3 * 2 * 3];
            for (int i = 0; i < rgb.Length; i++) rgb[i] = (byte)(i * 13);
            var png = Png.Encode(3, 2, rgb);
            Eq(0x89, (int)png[0], "signature");
            // IDAT at 8 + 25 (IHDR chunk): length, "IDAT", zlib data
            int len = png[33] << 24 | png[34] << 16 | png[35] << 8 | png[36];
            Eq("IDAT", System.Text.Encoding.ASCII.GetString(png, 37, 4), "IDAT tag");
            using var zs = new ZLibStream(new MemoryStream(png, 41, len), CompressionMode.Decompress);
            var raw = new MemoryStream(); zs.CopyTo(raw);
            var r = raw.ToArray();
            Eq(2 * (3 * 3 + 1), r.Length, "raw size");
            Eq(0, (int)r[0], "filter 0"); Eq((int)rgb[9], (int)r[11], "row 1 first byte");
            var (w, h, rgb2) = Png.FbToRgb(new byte[256 * 240], new int[64].Select((_, i) => i == 0 ? 0x123456 : 0).ToArray(), 2);
            Eq(512, w, "scaled w"); Eq(480, h, "scaled h"); Eq(0x34, (int)rgb2[1], "green");
        }),
        ("ram", () =>
        {
            var r = new Ram();
            r.Wr(0x0802, 0x1FF); Eq(0xFF, r.Rd(0x0002), "mirror + mask");
            r.Wr(0x7FFF, 7); Eq(7, r.Wram[0x1FFF], "wram");
            Throws(() => r.Rd(0x4000), "rd $4000");
            var p = r.Page(2); Eq(0x200, p.Offset, "page 2");
        }),
        ("prg", () =>
        {
            var m = new Machine(TheData.Value);
            Prg.SwitchBank16(m, 0x0D);
            Eq(0x1A, m.PrgR6, "R6"); Eq(0x1B, m.PrgR7, "R7"); Eq(6, m.Rd(0x50), "$50 ends 6"); Eq(0x1A, m.Rd(0x6E), "$6E");
            int want = TheData.Value.Prg[31 * 0x2000 + 0x1FFC];
            Eq(want, Prg.CpuRd(m, 0xFFFC), "reset vector lo");
            m.Wr(0x10, 0xFC); m.Wr(0x11, 0xFF);
            Eq(want, Prg.IndY(m, 0x10, 0), "(zp),Y");
            Prg.FarRestore(m, 2, 3); Eq(2, m.PrgR6, "far R6"); Eq(3, m.PrgR7, "far R7");
        }),
        ("data", () =>
        {
            var d = TheData.Value;
            Eq(131072, d.Chr.Length, "chr size"); Eq(262144, d.Prg.Length, "prg size"); Eq(64, d.NesRgb.Length, "nesRgb");
            Eq(256, d.PalSlots.Length, "pal slots");
            True(d.Short.ContainsKey("spritesB28") && d.Short.ContainsKey("ramLabels"), "camel keys");
            Throws(() => Data.Table(d, "no_such_table"), "missing table throws");
            foreach (var t in d.Tables.EnumerateObject()) { Data.Table(d, t.Name); break; }
            Throws(() => Data.Load(n => n == "manifest.json" ? System.Text.Encoding.UTF8.GetBytes(
                "{\"files\":{\"x.bin\":{\"size\":3}}}") : new byte[2]), "size check");
            // Gen/R.cs = ram_labels.json
            Eq(d.RamLabels.GetProperty("sub_state").GetInt32(), R.SubState, "R.SubState");
            Eq(d.RamLabels.GetProperty("pal_buf").GetInt32(), R.PalBuf, "R.PalBuf");
            Eq(d.RamLabels.GetProperty("scroll_x_irq").GetInt32(), R.ScrollXIrq, "R.ScrollXIrq");
        }),
        ("embedded", () =>
        {
            // Win s1 T2: the embedded set = ../web/data/manifest.json + every file it lists, byte for byte
            string dir = Data.FindDir();
            var want = new SortedSet<string>(TheData.Value.Manifest.GetProperty("files").EnumerateObject().Select(f => f.Name),
                StringComparer.Ordinal) { "manifest.json" };
            var got = new SortedSet<string>(Data.EmbeddedNames(), StringComparer.Ordinal);
            Eq(string.Join(",", want), string.Join(",", got), "embedded names = manifest files");
            foreach (string n in want)
                True(File.ReadAllBytes(Path.Combine(dir, n)).AsSpan().SequenceEqual(Data.ReadEmbedded(n)), $"embedded {n} bytes");
            var e = Data.LoadEmbedded();
            Eq(TheData.Value.Bin.Count, e.Bin.Count, "embedded bin count");
            Eq(TheData.Value.Json.Count, e.Json.Count, "embedded json count");
            True(Data.LoadDefault(null).Prg.AsSpan().SequenceEqual(TheData.Value.Prg), "LoadDefault(null) = embedded");
            Throws(() => Data.ReadEmbedded("no_such.bin"), "missing embedded file throws");
        }),
        ("ppu", () =>
        {
            var p = new Ppu(new byte[0x20000]);
            p.WriteReg(6, 0x3F); p.WriteReg(6, 0x10); p.WriteReg(7, 0x2A);
            Eq(0x2A, (int)p.Pal[0], "$3F10 mirrors $3F00");
            Eq(0x3F11, p.V, "v +1");
            p.WriteReg(0, 0x04); p.WriteReg(6, 0x20); p.WriteReg(6, 0x00); p.WriteReg(7, 1); Eq(0x2020, p.V, "v +32");
            p.ReadStatus(); p.WriteReg(5, 0x7D); p.WriteReg(5, 0x5E);
            Eq(5, p.X, "fine x"); Eq((0x7D >> 3) | ((0x5E & 7) << 12) | ((0x5E & 0xF8) << 2), p.T & 0x7FFF & ~0x0C00, "t");
            p.Mirroring = 1; Eq(0x400, p.NtIndex(0x2800), "horizontal"); p.Mirroring = 0; Eq(0x000, p.NtIndex(0x2800), "vertical");
            p.SetChrReg(0, 5); Eq(4, p.ChrBanks()[0], "R0 even"); Eq(5, p.ChrBanks()[1], "R0|1");
            Eq(0x2000, Ppu.Unprefetch(0x2002), "unprefetch");
            Eq(0x241F ^ 0x400, Ppu.Unprefetch(0x2401) ^ 0x400 ^ 0x400, "unprefetch wrap");
            // rendering off: line = backdrop
            p.Reset(); p.Pal[0] = 0x21; p.RenderFrame(); Eq(0x21, (int)p.Fb[100 * 256 + 7], "backdrop");
            // timed write: mask on from dot 101 -> pixels >= 100 use the new state
            p.Reset(); p.Pal[0] = 0x0F; p.Pal[0x10] = 0x30;
            p.Splits[5] = q => q.Timed(101, r => { r.Pal[0] = 0x16; });
            p.RenderFrame();
            Eq(0x0F, (int)p.Fb[5 * 256 + 98], "before dot"); Eq(0x16, (int)p.Fb[5 * 256 + 100], "after dot");
        }),
        ("mainstep", () =>
        {
            var m = new Machine(TheData.Value);
            m.Wr(R.FrameReady, 1);
            int n = 0;
            IEnumerable<Y> G() { n++; yield return Y.Frame; n++; yield return Y.Spin; n++; }
            m.Pending = G().GetEnumerator();
            True(MainLoop.MainStep(m), "ran"); Eq(1, n, "1st step"); Eq(0, m.Rd(R.FrameReady), "frame_ready cleared");
            False(MainLoop.MainStep(m), "no frame_ready -> no step");
            m.Wr(R.FrameReady, 1); MainLoop.MainStep(m); Eq(2, n, "2nd"); Eq(1, m.Rd(R.FrameReady), "SPIN keeps frame_ready");
            MainLoop.MainStep(m); Eq(3, n, "3rd"); True(m.Pending == null, "done -> pending cleared");
        }),
        ("state", () =>
        {
            string p = Path.Combine(Data.FindDir(), "..", "test", "states", "town02_enter.json");
            var s = StateFile.Load(p);
            Eq(2450, s.Frame, "frame"); Eq(254, s.Line ?? -1, "line"); Eq("30:CA2E", s.Until ?? "", "until");
            Eq(0x800, s.Ram.Length, "ram"); Eq(0x2000, s.Wram.Length, "wram"); Eq(32, s.Pal.Length, "pal"); Eq(8, s.MapperRegs.Length, "regs");
            var m = new Machine(TheData.Value, _ => { });
            m.Reset(); m.LoadState(s);
            Eq(1, m.IrqSplits.Count, "loadSplits: HUD"); Eq(new IrqSplit(1, 0xBE), m.IrqSplits[0], "H1 at $BE");
            Eq(s.MapperRegs[6] & 0x3F, m.PrgR6, "R6");
        }),
    };

    private static void False(bool c, string what) => Eq(false, c, what);

    private static void Throws(Action a, string what)
    {
        try { a(); _fail++; Console.WriteLine($"  FAIL {what}: no exception"); }
        catch (Exception) { _pass++; }
    }

    private static int Main(string[] args)
    {
        if (args.Length >= 3 && args[0] == "--wav") return WriteWav(args);
        if (args.Length >= 4 && args[0] == "--cdwav") return WinTests_CdWav(args);
        if (args.Length >= 3 && args[0] == "--ambwav") return WinTests_AmbWav(args);
        foreach (var (name, fn) in Tests.Concat(SoundTests).Concat(WinTests))
        {
            if (args.Length > 0 && !args.Any(name.Contains)) continue;
            int f0 = _fail;
            try { fn(); }
            catch (Exception e) { _fail++; Console.WriteLine($"  FAIL {name}: {e.GetType().Name}: {e.Message}"); }
            Console.WriteLine($"{(_fail == f0 ? "ok  " : "FAIL")} {name}");
        }
        Console.WriteLine($"{_pass} passed, {_fail} failed");
        return _fail == 0 ? 0 : 1;
    }
}
