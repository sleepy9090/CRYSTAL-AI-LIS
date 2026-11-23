// re-c C# port — data loader (= web/src/data.js). Everything the port uses comes from ../web/data/
// (written by tools/gen_web_data.py, docs/architecture_web.md §5). Load() reads manifest.json, then every
// listed file, and checks each file's size against the manifest.
// Result: Manifest, Bin[name], Json[name], plus the camelCase short keys of data.js as typed properties.
using System.Text.Json;

namespace ReC;

public sealed class Data
{
    public JsonElement Manifest { get; private set; }
    public Dictionary<string, byte[]> Bin { get; } = new();
    public Dictionary<string, JsonElement> Json { get; } = new();
    /// <summary>data.js `out[camel(name)]`: byte[] for .bin, JsonElement for .json</summary>
    public Dictionary<string, object> Short { get; } = new();

    // the short keys the game code uses (data.chr, data.prg, data.palettes, data.tables, ...)
    public byte[] Chr => (byte[])Short["chr"];
    public byte[] Prg => (byte[])Short["prg"];
    public JsonElement Palettes => (JsonElement)Short["palettes"];
    public JsonElement Tables => (JsonElement)Short["tables"];
    public JsonElement RamLabels => (JsonElement)Short["ramLabels"];

    private int[]? _nesRgb;
    /// <summary>palettes.json nesRgb (0xRRGGBB per NES colour)</summary>
    public int[] NesRgb => _nesRgb ??= Palettes.GetProperty("nesRgb").EnumerateArray().Select(e => e.GetInt32()).ToArray();

    private int[][]? _palSlots;
    /// <summary>palettes.json slots (256 x 4 colour bytes, $FF = backdrop)</summary>
    public int[][] PalSlots => _palSlots ??= Palettes.GetProperty("slots").EnumerateArray()
        .Select(s => s.EnumerateArray().Select(e => e.GetInt32()).ToArray()).ToArray();

    public byte[] BinFile(string shortName) => (byte[])Short[shortName];
    public JsonElement JsonFile(string shortName) => (JsonElement)Short[shortName];

    // js: data.js camel
    private static string Camel(string name)
    {
        string n = name.EndsWith(".bin", StringComparison.Ordinal) || name.EndsWith(".json", StringComparison.Ordinal)
            ? name[..name.LastIndexOf('.')] : name;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < n.Length; i++)
        {
            char c = n[i];
            if (c == '_' && i + 1 < n.Length && (char.IsAsciiLetterLower(n[i + 1]) || char.IsAsciiDigit(n[i + 1])))
            {
                sb.Append(char.ToUpperInvariant(n[i + 1]));
                i++;
            }
            else sb.Append(c);
        }
        return sb.ToString();
    }

    // js: data.js loadData
    public static Data Load(Func<string, byte[]> read, IReadOnlyCollection<string>? only = null)
    {
        var d = new Data();
        using (var mdoc = JsonDocument.Parse(read("manifest.json")))
            d.Manifest = mdoc.RootElement.Clone();
        foreach (var f in d.Manifest.GetProperty("files").EnumerateObject())
        {
            string name = f.Name;
            if (only != null && !only.Contains(name)) continue;
            byte[] bytes = read(name);
            if (f.Value.TryGetProperty("size", out var sz) && bytes.Length != sz.GetInt32())
                throw new InvalidDataException(
                    $"web/data/{name}: {bytes.Length} bytes, manifest says {sz.GetInt32()} (rerun tools/gen_web_data.py)");
            if (name.EndsWith(".json", StringComparison.Ordinal))
            {
                using var doc = JsonDocument.Parse(bytes);
                var el = doc.RootElement.Clone();
                d.Json[name] = el;
                d.Short[Camel(name)] = el;
            }
            else
            {
                d.Bin[name] = bytes;
                d.Short[Camel(name)] = bytes;
            }
        }
        return d;
    }

    /// <summary>Load from a directory holding manifest.json (Node's fs.readFile variant of data.js's `read`).</summary>
    public static Data LoadDir(string dir, IReadOnlyCollection<string>? only = null) =>
        Load(n => File.ReadAllBytes(Path.Combine(dir, n)), only);

    /// <summary>The web/data directory: `hint` if given, else walk up from the executable (and the cwd)
    /// to the first `web/data/manifest.json` (docs/architecture_cs.md §1 rule 4).</summary>
    public static string FindDir(string? hint = null)
    {
        if (hint != null)
        {
            if (!File.Exists(Path.Combine(hint, "manifest.json"))) throw new FileNotFoundException($"{hint}/manifest.json not found");
            return hint;
        }
        foreach (string start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            for (var dir = new DirectoryInfo(start); dir != null; dir = dir.Parent)
            {
                string p = Path.Combine(dir.FullName, "web", "data");
                if (File.Exists(Path.Combine(p, "manifest.json"))) return p;
            }
        }
        throw new FileNotFoundException("web/data/manifest.json not found above the executable (use --data DIR)");
    }

    private readonly Dictionary<string, int[]> _tableCache = new();

    // js: data.js table
    /// <summary>values of a named fixed-size ROM table from tables.json (throws if missing, so typos fail loudly)</summary>
    public static int[] Table(Data data, string name)
    {
        if (data._tableCache.TryGetValue(name, out var v)) return v;
        if (!data.Short.ContainsKey("tables") || !data.Tables.TryGetProperty(name, out var t))
            throw new KeyNotFoundException($"tables.json has no table {name}");
        v = t.GetProperty("values").EnumerateArray().Select(e => e.GetInt32()).ToArray();
        data._tableCache[name] = v;
        return v;
    }
}
