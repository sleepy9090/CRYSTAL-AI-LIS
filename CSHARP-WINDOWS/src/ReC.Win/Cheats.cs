// re-c Windows host — cheats (T36, Cheats menu): RAM writes after every frame while the game is in gameplay
// (main_state 1). Off by default; never used by the checks (canon / smoke / parity run without them). Not in the web port.
//   life      HP ($03C1) = max HP ($03C0); while plain play (sub_state 8) shows the HUD, the LIFE bar tiles at $2B26 are
//             redrawn the way HudHpBar 26:8CC0 draws them, so the NES bar is full too
//   magic     MP ($0708) = max MP ($0709)
//   gold      gold ($0702/$0703) = 60000
//   status    paralysis / poison (obj1_flash $0710 low nibble 1 / 3) cleared at once
//   charge    sword charge ($06C0 orbit_level) filled to the sword's cap (SwordChargeCap 26:9D7E) while B is held
//   xp        XP ($0704) = XP for the next level ($0706) - 1 (the next kill levels up)
using ReC.Game;

namespace ReC.Win;

internal sealed class Cheats
{
    public bool Life, Magic, Gold, Status, Charge, Xp, Items, Level, Speed, Spells;
    public bool Any => Life || Magic || Gold || Status || Charge || Xp || Items || Level || Speed || Spells;

    public static readonly string[] Names = { "life", "magic", "gold", "status", "charge", "xp", "items", "level", "speed", "spells" };
    public static readonly string[] Labels = { "INFINITE LIFE", "INFINITE MAGIC", "MAX GOLD", "NO PARALYSIS / POISON", "INSTANT CHARGE", "QUICK LEVEL-UP", "ALL ITEMS", "MAX LEVEL", "FAST WALK", "ALL MAGIC" };

    public bool Get(int i) => i switch { 0 => Life, 1 => Magic, 2 => Gold, 3 => Status, 4 => Charge, 5 => Xp, 6 => Items, 7 => Level, 8 => Speed, _ => Spells };
    public void Set(int i, bool v)
    {
        switch (i)
        {
            case 0: Life = v; break;
            case 1: Magic = v; break;
            case 2: Gold = v; break;
            case 3: Status = v; break;
            case 4: Charge = v; break;
            case 5: Xp = v; break;
            case 6: Items = v; break;
            case 7: Level = v; break;
            case 8: Speed = v; break;
            default: Spells = v; break;
        }
    }

    public string Serialize() => Any ? string.Join(",", Enumerable.Range(0, Names.Length).Where(Get).Select(i => Names[i])) : "none";
    public void Parse(string s)
    {
        var e = s.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < Names.Length; i++) Set(i, e.Contains(Names[i]));
    }

    /// <summary>after a frame: the switched-on cheats (gameplay only); bHeld = the B button is down</summary>
    public void Apply(Machine m, bool bHeld)
    {
        if (!Any) return;
        var ram = m.Mem.RamBytes;
        if (ram[0x40] != 1) return;                              // main_state 1 = gameplay (title / menus / ending: no)
        if (Life && ram[0x03C0] != 0 && ram[0x03C1] != ram[0x03C0])
        {
            ram[0x03C1] = ram[0x03C0];
            if (ram[0x41] == 8) DrawHpBar(m);
        }
        if (Magic && ram[0x0708] < ram[0x0709]) ram[0x0708] = ram[0x0709];
        if (Gold && (ram[0x0702] | ram[0x0703] << 8) < 60000) { ram[0x0702] = 60000 & 0xFF; ram[0x0703] = 60000 >> 8; }
        if (Status && (ram[0x0710] & 0x0F) is 1 or 3) ram[0x0710] = (byte)(ram[0x0710] & 0xF0);
        if (Charge && bHeld)
        {
            int cap = Prg.PrgByte(m, 26, 0x9D7E + Math.Min((int)ram[0x0719], 2));
            if (ram[0x06C0] < cap) ram[0x06C0] = (byte)cap;
        }
        if (Xp)
        {
            int xp = ram[0x0704] | ram[0x0705] << 8, next = ram[0x0706] | ram[0x0707] << 8;
            if (next > 0 && xp < next - 1) { ram[0x0704] = (byte)((next - 1) & 0xFF); ram[0x0705] = (byte)((next - 1) >> 8); }
        }
        // playtest 9: the game's own dead debug cheat DebugPauseCheat 15:BF47 (switched off by one byte: its JSR targets the
        // RTS before it): empty inventory slots (b7 set) filled from DebugItemTable 15:BF7F (24 ids), level $0F + MP $FF,
        // and on pad-2 A the speed of slots 0 / 1 ($0340 / $0341) = 9
        if (Items)
            for (int x = 0x17; x >= 0; x--)
                if ((m.Rd(0x6430 + x) & 0x80) != 0) m.Wr(0x6430 + x, Prg.PrgByte(m, 15, 0xBF7F + x));
        if (Level && (ram[0x0421] != 0x0F || ram[0x0709] != 0xFF)) { ram[0x0421] = 0x0F; ram[0x0709] = 0xFF; ram[0x0708] = 0xFF; }
        if (Speed && ram[0x41] == 8) { ram[0x0340] = 9; ram[0x0341] = 9; }
        // playtest 13: all eight spells (item ids $41-$48, GiveItem 30:D3EB) in the magic slots $6458-$645F
        if (Spells) for (int x = 0; x < 8; x++) if (m.Rd(0x6458 + x) != 0x41 + x) m.Wr(0x6458 + x, 0x41 + x);
    }

    /// <summary>the 17 HP-bar tiles from $2B26 as HudHpBar 26:8CC0 draws them</summary>
    public static void DrawHpBar(Machine m)
    {
        var t = HpBarTiles(m.Mem.RamBytes[0x03C0], m.Mem.RamBytes[0x03C1], k => Prg.PrgByte(m, 26, 0x8D49 + k));
        for (int i = 0; i < t.Length; i++) m.Ppu.VramWrite(0x2B26 + i, t[i]);   // not the $6060 buffer: the charge meter shares it
    }

    // js: game/states/death.js hudHpBar
    /// <summary>HudHpBar's tile row: $20 blank, $8C frame up to max HP, $8D end cap, $88 full cells, partial[hp & 15]</summary>
    public static int[] HpBarTiles(int max, int hp, Func<int, int> partial)
    {
        var t = new int[17];
        Array.Fill(t, 0x20);
        static int Less1(int v) => v == 0 ? 0 : v - 1;
        int x = Less1(max) >> 4;
        if (x + 1 < 17) t[x + 1] = 0x8D;
        for (; x >= 0; x--) t[x] = 0x8C;
        int h = Less1(hp), y = h >> 4;
        for (x = y; x > 0; x--) t[x - 1] = 0x88;
        t[y] = partial(h & 0x0F);
        return t;
    }
}

/// <summary>playtest 7: Cheats > Warp to area. The game's own way into an area (what the exits do and the test tools'
/// area scenarios poke): area_id $6C, entrance $6D = 0, sub_state $41 = 1 (SubstAreaEnter, AreaLoad 31:E14A).
/// Only from plain play (main_state 1, sub_state 8); only areas with a record in AreaPtrTable 12:9201 (252 entries,
/// pointer high byte bit 7 set)</summary>
internal static class Warp
{
    public static bool[] Areas(ReC.Data d)
    {
        var a = new bool[256];
        for (int i = 0; i < 252; i++) a[i] = (d.Prg[12 * 0x2000 + 0x1202 + i * 2] & 0x80) != 0;
        return a;
    }

    public static bool To(Machine m, int area)
    {
        if (m.Rd(0x40) != 1 || m.Rd(0x41) != 8) return false;
        m.Wr(0x6C, area & 0xFF); m.Wr(0x6D, 0); m.Wr(0x41, 1);
        return true;
    }
}
