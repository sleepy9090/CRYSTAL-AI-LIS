// re-c Windows host — ReC.exe entry point (architecture_win.md §1 rules 2 + 6).
//
//   ReC.exe [window options]                 play in a window (Window.cs, T4)
//   ReC.exe --headless [run.mjs options]     no window: = ReC.Run / node ../web/test/run.mjs (Headless.cs)
//
// Mode: headless when `--headless` is given, or when any data-output option of the runner (--compare, --dump-ram,
// --dump-wram, --dump-ciram, --pass-count, --shots) is given without `--window`; otherwise the window. The headless
// mode prints to the parent console (AttachConsole) or to redirected std handles (tools/win_vs_web.py pipes).
namespace ReC.Win;

internal static class Program
{
    /// <summary>Runner options that only make sense without a window (they write the test outputs).</summary>
    private static readonly HashSet<string> HeadlessOnly =
        new(StringComparer.Ordinal) { "--compare", "--dump-ram", "--dump-wram", "--dump-ciram", "--pass-count", "--shots" };

    public static bool IsHeadless(IReadOnlyList<string> args) =>
        args.Contains("--headless") || (!args.Contains("--window") && args.Any(HeadlessOnly.Contains));

    [STAThread]
    private static int Main(string[] args)
    {
        AttachParentConsole();
        if (args.Length > 0 && args[0] == "--wav") return WavOut.Run(args[1..]);
        if (IsHeadless(args))
            return Run.Headless.Run(args.Where(a => a is not "--headless" and not "--window").ToArray(), "ReC.exe --headless");
        WindowOptions? o;
        try { o = WindowOptions.Parse(args); }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            Console.Error.WriteLine(e.Message);
            return 2;
        }
        if (o == null) { Console.WriteLine(Usage); return 0; }
        return Window.Run(o);
    }

    private static string Usage =>
        "usage: ReC.exe [--scale S] [--save F.sav] [--data DIR] [--load-state F.json] [--mute] [--no-audio]\n" +
        "               [--frames N] [--frame0 N] [--turbo] [--keys SPEC..] [--compare F.csv] [--dump-screen F.png]\n" +
        "               [--quiet] [--window]\n" +
        "  plays in a window; save file %APPDATA%\\re-c\\re-c.sav unless --save\n" +
        Run.Headless.Help("ReC.exe --headless") + "\n" +
        "  no window (also when --compare / --dump-ram / --dump-wram / --dump-ciram / --pass-count / --shots\n" +
        "  is given without --window)\n" +
        "usage: ReC.exe --wav ID OUT.wav [SECONDS]   music id -> 16-bit mono 44.1 kHz WAV (= apu_check.mjs --wav)";

    /// <summary>A WinExe has no console: write to the parent's (cmd / PowerShell) unless stderr is already redirected.</summary>
    private static void AttachParentConsole()
    {
        if (!OperatingSystem.IsWindows()) return;
        if (Win32.GetFileType(Win32.GetStdHandle(Win32.StdErrorHandle)) != Win32.FileTypeUnknown) return;
        Win32.AttachConsole(Win32.AttachParentProcess);
    }
}
