// re-c C# port — area load: header sub-tables k0/k1/k2/k4 and the area record (= web/src/game/world/area.js, s53).
// REVERSE.md "Area record + header sub-tables". The tables come pre-decoded from web/data/areas.json; every
// RAM/WRAM write below is the ROM's, in order.
//
//   LoadAreaMap(m)      AreaLoadMap 31:E628 (k0) -> AreaRestoreVisited 31:E702 (k4) -> entry point 31:E686 (k2)
//                       -> AreaLoadGfx 31:E6B9 (k1) -> AreaLoadB 31:E148 -> AreaLoad 31:E14A / LoadAreaChrRecord 31:E16D
//   LoadAreaChr(m, a)   LoadAreaChrRecord body: $07E6/$07E7, R4/R5 $07F4/$07F5, spawn entries from slot $0D
// Machine hooks (as machine.js): m.PlaySoundHook (PlaySound, Y = 0), m.SpawnEntry (world/spawn.js), m.EntryFF.
using System.Text.Json;

namespace ReC.Game.World;

public static class Area
{
    private static int I(JsonElement e, string k) => e.GetProperty(k).GetInt32();

    // js: game/world/area.js areaInfo
    public static JsonElement AreaInfo(Machine m, int? id = null)
    {
        int a = id ?? m.Rd(0x6C);
        string key = a.ToString("X2");                    // keys '%02X'
        if (!m.Data.JsonFile("areas").GetProperty("areas").TryGetProperty(key, out var r) || r.ValueKind == JsonValueKind.Null)
            throw new InvalidOperationException($"area ${a:x}: no record/header in areas.json (null area)");
        return r;
    }

    private static JsonElement? Header(JsonElement a) =>
        a.TryGetProperty("header", out var h) && h.ValueKind == JsonValueKind.Object ? h : null;

    // js: game/world/area.js loadAreaMap
    /// <summary>AreaLoadMap 31:E628 and everything it chains into (k0, k4, k2, k1, record).</summary>
    public static void LoadAreaMap(Machine m)
    {
        var a = AreaInfo(m);
        var h = Header(a);
        if (h == null || !h.Value.TryGetProperty("k0", out var k0) || k0.ValueKind != JsonValueKind.Object
            || (k0.TryGetProperty("garbage", out var g) && g.ValueKind != JsonValueKind.False && g.ValueKind != JsonValueKind.Null))
            throw new InvalidOperationException($"area ${m.Rd(0x6C):x}: no usable k0 header");
        m.PlaySoundHook(I(k0, "snd"));
        m.Wr(0x62FC, I(k0, "W") - 1);                    // area_w (width - 1, screens)
        m.Wr(0x62FD, I(k0, "H") - 1);                    // area_h
        m.Wr(0x13, I(k0, "H") - 1);
        m.Wr(0x62FE, I(k0, "f62FE"));
        m.Wr(0x62FF, I(k0, "f62FF"));
        for (int i = 0; i < 0x80; i++) m.Wr(0x6300 + i, 0x80);   // screen_grid prefill = page $80 (black)
        int r = 0;
        foreach (var row in k0.GetProperty("grid").EnumerateArray())
        {
            int c = 0;
            foreach (var s in row.EnumerateArray()) { m.Wr(0x6300 + r * 8 + c, s.GetInt32()); c++; }
            r++;
        }
        m.Wr(0x12, 0xFF);                                // DEC tmp_12 / BPL leave both loop counters at $FF
        m.Wr(0x13, 0xFF);
        for (int i = 0; i < 12; i++) m.Wr(0x62F0 + i, 0);         // visited_map
        RestoreVisited(m, a);
        PlacePlayer(m, a);
    }

    // js: game/world/area.js restoreVisited
    /// <summary>AreaRestoreVisited 31:E702: for each k4 `flag scr`, visited bit of screen scr = WRAM flag $64C0 bit.</summary>
    public static void RestoreVisited(Machine m, JsonElement? area = null)
    {
        var a = area ?? AreaInfo(m);
        foreach (var e in a.GetProperty("header").GetProperty("k4").EnumerateArray())
        {
            int flag = I(e, "flag"), scr = I(e, "scr");
            bool set = (m.Rd(0x64C0 + (flag >> 3)) & (1 << (flag & 7))) != 0;
            int bit = 1 << (scr & 7), row = 0x62F0 + (scr >> 4);
            m.Wr(row, (m.Rd(row) & ~bit) | (set ? bit : 0));
        }
    }

    // js: game/world/area.js placePlayer
    /// <summary>31:E686: $6D = $FF -> bank 26 $98A8 (X=0); $6D&amp;$E0 = $20 -> keep the position; else k2[$6D&amp;$1F] -> $34-$37.</summary>
    public static void PlacePlayer(Machine m, JsonElement? area = null)
    {
        var a = area ?? AreaInfo(m);
        int e = m.Rd(0x6D);
        if (e == 0xFF) m.EntryFF(0);
        else if ((e & 0xE0) != 0x20)
        {
            var k2 = a.GetProperty("header").GetProperty("k2");
            int i = e & 0x1F;
            if (i >= k2.GetArrayLength() || k2[i].ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException($"area ${m.Rd(0x6C):x}: no k2 entry {i}");
            int x = I(k2[i], "x"), y = I(k2[i], "y");
            m.Wr(0x34, x & 0xFF); m.Wr(0x35, x >> 8); m.Wr(0x36, y & 0xFF); m.Wr(0x37, y >> 8);
        }
        LoadAreaGfx(m, a);
    }

    // js: game/world/area.js loadAreaGfx
    /// <summary>AreaLoadGfx 31:E6B9 (k1) then JMP AreaLoadB 31:E148 ($18 = 0) -> the area record.</summary>
    public static void LoadAreaGfx(Machine m, JsonElement? area = null)
    {
        var a = area ?? AreaInfo(m);
        var k1 = a.GetProperty("header").GetProperty("k1");
        var pal = k1.GetProperty("pal");
        m.Wr(0x07E0, pal[0].GetInt32()); m.Wr(0x07E1, pal[1].GetInt32()); m.Wr(0x07E2, pal[2].GetInt32()); m.Wr(0x07E3, 0x7F);
        m.Wr(0x66, 0); m.Wr(0x68, 0);
        int v67 = I(k1, "v67");
        m.Wr(0x67, v67); m.Wr(0x69, I(k1, "v69"));
        int sub = 0xBE00 + (v67 & 0x3C) * 8;            // ASL x3 carry feeds ADC #$BE: a plain 16-bit sum
        m.Wr(0x6A, sub & 0xFF); m.Wr(0x6B, sub >> 8);
        m.Wr(0x07F0, I(k1, "R0")); m.Wr(0x07F1, I(k1, "R1"));
        AreaLoad(m, 0, a);
    }

    // js: game/world/area.js areaLoad
    /// <summary>AreaLoad 31:E14A: $18 = flag; AreaPtrTable 12:9201[$6C] (hi b7 clear = no record) -> LoadAreaChrRecord.</summary>
    public static void AreaLoad(Machine m, int flag18, JsonElement? area = null)
    {
        var a = area ?? AreaInfo(m);
        m.Wr(0x18, flag18);
        Prg.SwitchBank16(m, 6);                          // E14C SwitchBank16_6
        int id = m.Rd(0x6C), t = (id & 0x80) != 0 ? 0x9301 : 0x9201, yy = (id << 1) & 0xFF;
        m.Wr(0x10, Prg.CpuRd(m, t + yy));                // ($10) = AreaPtrTable entry (AreaSpawnEntry reads it)
        m.Wr(0x11, Prg.CpuRd(m, t + yy + 1));
        if (a.TryGetProperty("record", out var r) && r.ValueKind == JsonValueKind.Object
            && (I(r, "ptr") & 0x8000) != 0 && !r.TryGetProperty("ignored", out _))
            LoadAreaChr(m, a);
    }

    // js: game/world/area.js loadAreaChr
    /// <summary>LoadAreaChrRecord 31:E16D (record byte 0 != 0 -> RTS: areas.json marks those `ignored`). Spawn entries
    /// from slot $0D at ($10)+5: $FF ends, $FE / b1 bit 7 (respawning, SubstPlayTail) skip, else AreaSpawnEntry.</summary>
    public static void LoadAreaChr(Machine m, JsonElement? area = null)
    {
        var a = area ?? AreaInfo(m);
        var r = a.GetProperty("record");
        m.Wr(0x07E6, I(r, "e6")); m.Wr(0x07E7, I(r, "e7")); m.Wr(0x07F4, I(r, "r4")); m.Wr(0x07F5, I(r, "r5"));
        for (int y = 5, x = 0x0D; ; x = (x + 1) & 0xFF)
        {
            int b0 = Prg.IndY(m, 0x10, y);
            if (b0 == 0xFF) return;
            if (b0 == 0xFE || (Prg.IndY(m, 0x10, (y + 1) & 0xFF) & 0x80) != 0) { y = (y + 4) & 0xFF; continue; }
            y = m.SpawnEntry(x, y);
        }
    }
}
