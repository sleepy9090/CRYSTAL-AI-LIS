// re-c Windows host — battery WRAM in a file (= web/src/save.js, localStorage -> %APPDATA%\re-c\re-c.sav;
// architecture_win.md §1 rule 5). Copied from ../CSHARP/src/ReC.Sdl/Save.cs; only the default path differs.
//
// The cartridge keeps all 8 KB of WRAM $6000-$7FFF across power cycles, and the game's save system lives in it
// (../REVERSE.md "Save / load module"; BootSaveCheck 31:F0B0 checks it at boot). The file holds the same 8 KB byte for
// byte (raw, where the JS stores base64 text in localStorage: toB64 / fromB64 are waived).
namespace ReC.Win;

internal static class Save
{
    public const int Size = 0x2000;

    /// <summary>%APPDATA%\re-c\re-c.sav (the folder is created on the first save).</summary>
    public static string DefaultPath()
    {
        string app = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        if (string.IsNullOrEmpty(app)) app = AppContext.BaseDirectory;
        return Path.Combine(app, "re-c", "re-c.sav");
    }

    // js: save.js loadWram
    public static bool LoadWram(string path, byte[] wram)
    {
        byte[] b;
        try { if (!File.Exists(path)) return false; b = File.ReadAllBytes(path); } catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
        if (b.Length != Size) return false;
        Array.Copy(b, wram, Size);
        return true;
    }

    // js: save.js saveWram
    /// <summary>Written to PATH.tmp, then moved over PATH, so a crash mid-write never leaves half an image.</summary>
    /// <summary>T63: keep the last three saves: path.1 (newest) .. path.3, rotated at start when the save differs from
    /// path.1. Returns the number of backups now on disk.</summary>
    public static int Backup(string path)
    {
        try
        {
            if (!File.Exists(path)) return 0;
            byte[] cur = File.ReadAllBytes(path);
            string b1 = path + ".1";
            if (!File.Exists(b1) || !File.ReadAllBytes(b1).AsSpan().SequenceEqual(cur))
            {
                if (File.Exists(path + ".2")) File.Copy(path + ".2", path + ".3", overwrite: true);
                if (File.Exists(b1)) File.Copy(b1, path + ".2", overwrite: true);
                File.Copy(path, b1, overwrite: true);
            }
            return Enumerable.Range(1, 3).Count(i => File.Exists(path + "." + i));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return 0; }
    }

    public static bool SaveWram(string path, byte[] wram)
    {
        try
        {
            string? dir = Path.GetDirectoryName(Path.GetFullPath(path));
            if (dir != null) Directory.CreateDirectory(dir);
            string tmp = path + ".tmp";
            File.WriteAllBytes(tmp, wram.AsSpan(0, Size).ToArray());
            File.Move(tmp, path, overwrite: true);
            return true;
        }
        catch (IOException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}

// js: save.js WramSaver
internal sealed class WramSaver
{
    private readonly string _path;
    private readonly byte[] _wram;
    private readonly byte[] _last;

    // js: save.js constructor (WramSaver)
    public WramSaver(string path, byte[] wram) { _path = path; _wram = wram; _last = wram.AsSpan(0, Save.Size).ToArray(); }

    // js: save.js poll
    public bool Poll()
    {
        if (_wram.AsSpan(0, Save.Size).SequenceEqual(_last)) return false;
        Array.Copy(_wram, _last, Save.Size);
        return Save.SaveWram(_path, _wram);
    }
}
