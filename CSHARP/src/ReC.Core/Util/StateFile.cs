// re-c C# port — reader for tools/emu/web_state.py JSON states (../web/test/states/*.json), the input of
// Machine.LoadState (= the object run.mjs passes to machine.js loadState).
using System.Text.Json;

namespace ReC.Util;

public sealed class StateFile
{
    public int Frame;
    /// <summary>`frame0` if present (run.mjs: --frame0 defaults to frame0 ?? frame)</summary>
    public int? Frame0;
    /// <summary>scanline the state was taken at (null = not recorded)</summary>
    public int? Line;
    /// <summary>routine address `BB:AAAA` the state stopped at (play.js ENTRY_POINTS key)</summary>
    public string? Until;
    public byte[] Ram = Array.Empty<byte>(), Wram = Array.Empty<byte>(), Ciram = Array.Empty<byte>(),
        Pal = Array.Empty<byte>(), Oam = Array.Empty<byte>();
    public int Ctrl, Mask, V, T, X, W;
    public int[] MapperRegs = new int[8];
    public int Mirroring, ChrInv;
    /// <summary>the whole JSON (other fields some checks read)</summary>
    public JsonElement Raw;

    public static StateFile Load(string path) => Parse(File.ReadAllBytes(path));

    public static StateFile Parse(byte[] json)
    {
        using var doc = JsonDocument.Parse(json);
        var r = doc.RootElement;
        static int? OptInt(JsonElement e, string k) =>
            e.TryGetProperty(k, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : null;
        static byte[] Hex(JsonElement e, string k) => Convert.FromHexString(e.GetProperty(k).GetString() ?? "");
        var ppu = r.GetProperty("ppu");
        var mapper = r.GetProperty("mapper");
        return new StateFile
        {
            Frame = r.GetProperty("frame").GetInt32(),
            Frame0 = OptInt(r, "frame0"),
            Line = OptInt(r, "line"),
            Until = r.TryGetProperty("until", out var u) && u.ValueKind == JsonValueKind.String ? u.GetString() : null,
            Ram = Hex(r, "ram"), Wram = Hex(r, "wram"), Ciram = Hex(r, "ciram"), Pal = Hex(r, "pal"), Oam = Hex(r, "oam"),
            Ctrl = ppu.GetProperty("ctrl").GetInt32(), Mask = ppu.GetProperty("mask").GetInt32(),
            V = ppu.GetProperty("v").GetInt32(), T = ppu.GetProperty("t").GetInt32(),
            X = ppu.GetProperty("x").GetInt32(), W = ppu.GetProperty("w").GetInt32(),
            MapperRegs = mapper.GetProperty("regs").EnumerateArray().Select(e => e.GetInt32()).ToArray(),
            Mirroring = mapper.GetProperty("mirroring").GetInt32(),
            ChrInv = mapper.GetProperty("chr_inv").ValueKind == JsonValueKind.True ? 1
                : mapper.GetProperty("chr_inv").ValueKind == JsonValueKind.Number ? mapper.GetProperty("chr_inv").GetInt32() : 0,
            Raw = r.Clone(),
        };
    }
}
