// re-c C# port — key scripts + the compare CSV (= web/test/harness.mjs parseButtons / parseKeys /
// compareColumns / compareRow; tools/emu/compare.py columns and values).
using ReC.Game;

namespace ReC.Util;

public static class Keys
{
    /// <summary>input.js BTN: ROM-order button byte (A = b7 ... RIGHT = b0)</summary>
    public static readonly IReadOnlyDictionary<string, int> Btn = new Dictionary<string, int>
    {
        ["A"] = 0x80, ["B"] = 0x40, ["SELECT"] = 0x20, ["START"] = 0x10,
        ["UP"] = 0x08, ["DOWN"] = 0x04, ["LEFT"] = 0x02, ["RIGHT"] = 0x01,
    };

    /// <summary>tools.emu masks put A in b0; the port takes the ROM-order byte (harness.mjs rev8)</summary>
    public static int Rev8(int b)
    {
        int r = 0;
        for (int i = 0; i < 8; i++) if ((b & (1 << i)) != 0) r |= 0x80 >> i;
        return r;
    }

    public static int ParseButtons(string s)
    {
        int m = 0;
        foreach (string p in s.Replace(',', '+').Split('+'))
        {
            string part = p.Trim().ToUpperInvariant();
            if (part.Length == 0 || part == "NONE") continue;
            if (!Btn.TryGetValue(part, out int b))
                throw new ArgumentException($"unknown button \"{part}\" (use {string.Join(' ', Btn.Keys)})");
            m |= b;
        }
        return m;
    }

    /// <summary>['120:START', '300:A+RIGHT:8', '@file'] -> frame -> mask; default hold 2 frames (= debug.parse_keys)</summary>
    public static Dictionary<int, int> Parse(IEnumerable<string> specs)
    {
        var flat = new List<string>();
        foreach (string s in specs)
        {
            if (s.StartsWith('@'))
            {
                foreach (string ln0 in File.ReadAllText(s[1..]).Split('\n'))
                {
                    string ln = ln0.Split('#', 2)[0].Trim();
                    if (ln.Length > 0) flat.AddRange(ln.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
                }
            }
            else flat.AddRange(s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        }
        var keys = new Dictionary<int, int>();
        foreach (string s in flat)
        {
            var p = s.Split(':');
            if (!int.TryParse(p[0], out int start)) throw new ArgumentException($"bad key spec \"{s}\"");
            int mask = ParseButtons(p.Length > 1 ? p[1] : "");
            int n = 2;
            if (p.Length > 2 && !int.TryParse(p[2], out n)) throw new ArgumentException($"bad key spec \"{s}\"");
            for (int f = start; f < start + n; f++) keys[f] = (keys.TryGetValue(f, out int k) ? k : 0) | mask;
        }
        return keys;
    }
}

public static class Csv
{
    public const int Slots = 32;
    // s82: title / state-3 variables + OAM shadow crc (tools/emu/compare.py TTL_COLS / TTL_ADDRS)
    private static readonly string[] TtlCols = { "ttl_mode", "ttl_step", "ttl_s3", "ttl_wait", "ttl_ctr", "ttl_snd" };
    private static readonly int[] TtlAddrs = { 0x6F0, 0x6F1, 0x6F3, 0x6FE, 0x6FF, 0x6B2 };

    public static List<string> CompareColumns()
    {
        var cols = new List<string> { "frame", "state", "sub", "area", "entry", "pos_x", "pos_y", "cam_x", "cam_y", "music" };
        for (int c = 0; c < 4; c++) cols.Add($"snd{c}_period");
        cols.Add("pal_crc");
        for (int s = 0; s < Slots; s++)
        {
            string n = s.ToString("X2");
            cols.Add($"o{n}_ai"); cols.Add($"o{n}_x"); cols.Add($"o{n}_y");
        }
        cols.AddRange(TtlCols);
        cols.Add("oam_crc");
        return cols;
    }

    /// <summary>one row after a finished frame; `frame` = the script frame number of that frame</summary>
    public static List<long> CompareRow(Machine m, int frame)
    {
        byte[] r = m.Mem.RamBytes, w = m.Mem.Wram;
        var @out = new List<long> { frame, r[0x40], r[0x41], r[0x6C], r[0x6D],
            r[0x34] | r[0x35] << 8, r[0x36] | r[0x37] << 8,
            r[0x02] | r[0x03] << 8, r[0x04] | r[0x05] << 8, r[0x102] };
        for (int c = 0; c < 4; c++) @out.Add(r[0x174 + c] | r[0x17A + c] << 8);
        @out.Add(Crc32.Compute(w.AsSpan(0x140, 0x20)));
        for (int s = 0; s < Slots; s++)
        {
            int ai = r[0x4A0 + s];
            if (ai != 0) { @out.Add(ai); @out.Add(r[0x70 + s] | r[0x90 + s] << 8); @out.Add(r[0xB0 + s] | r[0xD0 + s] << 8); }
            else { @out.Add(0); @out.Add(0); @out.Add(0); }
        }
        foreach (int a in TtlAddrs) @out.Add(r[a]);
        @out.Add(Crc32.Compute(r.AsSpan(0x200, 0x100)));
        return @out;
    }
}
