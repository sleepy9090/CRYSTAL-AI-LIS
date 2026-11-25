// re-c Windows host — HD engine (T25): the starter graphics packs (packs/hero, enemies, npcs, bosses, backgrounds; made
// by tools/*_pack.py from the game's own graphics) built into ReC.exe as embedded PNGs (`re-c.pack.<folder>.<file>`),
// so the HD look has them without installing anything. Only the HD engine uses them (Video > Graphics > HD: Built-in
// art); a user pack (%APPDATA%\re-c\pack or --pack) overrides tile by tile. Not in the web port.
using System.Reflection;

namespace ReC.Win;

internal static class BuiltinArt
{
    public const string Prefix = "re-c.pack.";
    private static Dictionary<long, uint[]>? _cache;
    public static readonly List<string> Errors = new();

    /// <summary>name + bytes of every pack PNG -> tiles by Enhancer.ArtKey (bad ones listed in errors, skipped)</summary>
    public static Dictionary<long, uint[]> Decode(IEnumerable<(string Name, byte[] Png)> files, List<string> errors)
    {
        var art = new Dictionary<long, uint[]>();
        foreach (var (name, png) in files)
        {
            if (!Enhancer.TryParseArtName(name, out long key)) { errors.Add($"{name}: name is not TTTT_AABBCC.png"); continue; }
            try
            {
                var (w, h, px) = PngRead.Decode(png);
                if (w != Enhancer.T || h != Enhancer.T) { errors.Add($"{name}: {w}x{h}"); continue; }
                art.TryAdd(key, px);
            }
            catch (Exception e) when (e is InvalidDataException or ArgumentException or IndexOutOfRangeException)
            {
                errors.Add($"{name}: {e.Message}");
            }
        }
        return art;
    }

    /// <summary>the packs embedded in this exe (decoded once)</summary>
    public static Dictionary<long, uint[]> Embedded()
    {
        if (_cache != null) return _cache;
        var asm = Assembly.GetExecutingAssembly();
        var files = asm.GetManifestResourceNames().Where(n => n.StartsWith(Prefix, StringComparison.Ordinal))
            .OrderBy(n => n, StringComparer.Ordinal).Select(n =>
            {
                using var s = asm.GetManifestResourceStream(n)!;
                var b = new byte[s.Length];
                s.ReadExactly(b);
                return (n[(n.LastIndexOf('.', n.Length - 5) + 1)..], b);    // "<folder>.TTTT_AABBCC.png" -> file name
            });
        return _cache = Decode(files, Errors);
    }

    /// <summary>the same from a packs folder on disk (packs/*/*.png; tests and tools)</summary>
    public static Dictionary<long, uint[]> FromDirectory(string packs, List<string> errors) =>
        Decode(Directory.GetDirectories(packs).OrderBy(d => d, StringComparer.Ordinal)
            .SelectMany(d => Directory.GetFiles(d, "*.png").OrderBy(f => f, StringComparer.Ordinal))
            .Select(f => (Path.GetFileName(f), File.ReadAllBytes(f))), errors);

    /// <summary>NES fade step k of colour c (PaletteBuild 26:8C10: c - $10 k, negative -> $0F)</summary>
    public static int Fade(int c, int k)
    {
        int f = (c & 0x3F) - 0x10 * k;
        return f < 0 ? 0x0F : f;
    }

    /// <summary>the HD engine's tiles: built-in (if on), then the user pack over them</summary>
    public static Dictionary<long, uint[]> Merge(IReadOnlyDictionary<long, uint[]>? builtin, IReadOnlyDictionary<long, uint[]> user)
    {
        var d = builtin != null ? new Dictionary<long, uint[]>(builtin) : new Dictionary<long, uint[]>();
        foreach (var (k, v) in user) d[k] = v;
        return d;
    }
}
