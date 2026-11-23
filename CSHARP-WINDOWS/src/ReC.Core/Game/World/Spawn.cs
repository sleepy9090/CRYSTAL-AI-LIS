// re-c C# port — area spawn entries (= web/src/game/world/spawn.js, s112). REVERSE.md "Spawn entry" / "NpcHidden".
//
//   AreaSpawnEntry(m, x, y)   AreaSpawnEntry 31:E1B6: the 4-byte entry at ($10),Y for slot X -> $2C-$2F, position,
//                             then SpawnHandlerTable 31:E218[b2 &amp; 7] (handlers 0-4). Returns Y + 4.
//                             $10/$11 are pushed before the handler and restored by SpawnCommonTail 31:E28E.
//   NpcHidden(m)              NpcHidden 14:80A0 ($23 = param -> $20 != 0: NPC absent)
//   ChestPresence(m)          ChestPresence 14:85BF ($23 = chest param -> $20 = 1 if any condition true)
//   NpcFlagTest(m, y)         NpcFlagTest 14:8135 / NpcFlagRef 14:8148 on the 2-byte ref at ($24),Y
using ReC.Game.Obj;

namespace ReC.Game.World;

public static class Spawn
{
    private const int YLo = 0xB0, YHi = 0xD0, XLo = 0x70, XHi = 0x90;
    private const int Active = 0x04A0, Behav = 0x06E0, Flags = 0x0380, Meta = 0x0300, TileOfs = 0x0320;
    private const int Param1 = 0x0680, Param2 = 0x06A0, Phase = 0x0600, Orbit = 0x06C0, Freeze = 0x05A0;
    private const int SpawnB2 = 0x0540, SpawnP = 0x0560, FlagsWram = 0x6480;
    private static readonly int[] H1Phase = { 0xDA, 0x22, 0x4A, 0xAA };   // 31:E35F

    private static readonly Action<Machine, int>[] Handlers = { SpawnH0, SpawnH1, SpawnH2, SpawnH3, SpawnH4 };

    // js: game/world/spawn.js areaSpawnEntry
    public static int AreaSpawnEntry(Machine m, int x, int y)
    {
        for (int i = 0; i < 4; i++) m.Wr(0x2C + i, Prg.IndY(m, 0x10, (y + i) & 0xFF));
        y = (y + 4) & 0xFF;
        int b0 = m.Rd(0x2C), b1 = m.Rd(0x2D), b2 = m.Rd(0x2E);
        m.Wr(YHi + x, b0 >> 4);
        m.Wr(YLo + x, ((b0 << 4) | 0x0C) & 0xFF);
        m.Wr(0x20, (b2 & 0x40) >> 3);
        m.Wr(XHi + x, (b1 >> 4) & 7);
        m.Wr(XLo + x, ((((b1 << 4) | 8) & 0xFF) + m.Rd(0x20)) & 0xFF);   // SEC ROL, ASL x3, CLC ADC $20
        m.Wr(0x14, y);
        int h = b2 & 7;
        if (h > 4) throw new InvalidOperationException($"AreaSpawnEntry: handler {h} (code bytes past SpawnHandlerTable 31:E218)");
        m.Wr(0x12, Prg.CpuRd(m, 0xE218 + h * 2)); m.Wr(0x13, Prg.CpuRd(m, 0xE219 + h * 2));
        int s10 = m.Rd(0x10), s11 = m.Rd(0x11);          // PHA $10, PHA $11
        Handlers[h](m, x);
        // SpawnCommonTail 31:E28E
        m.Wr(SpawnB2 + x, m.Rd(0x2E) & 3);
        m.Wr(SpawnP + x, m.Rd(0x2F));
        m.Wr(0x11, s11); m.Wr(0x10, s10);
        return m.Rd(0x14);
    }

    // js: game/world/spawn.js init
    /// <summary>STA $11 / STX $10 / JSR ObjInit</summary>
    private static void Init(Machine m, int x, int type)
    {
        m.Wr(0x11, type); m.Wr(0x10, x);
        ObjInit.Init(m);
    }

    // js: game/world/spawn.js setTileHi
    /// <summary>SpawnSetTileHi 31:E3C7: obj_flags |= (b2 &amp; $80) >> 2</summary>
    private static void SetTileHi(Machine m, int x) =>
        m.Wr(Flags + x, ((m.Rd(0x2E) & 0x80) >> 2) | m.Rd(Flags + x));

    // js: game/world/spawn.js spawnH0
    /// <summary>SpawnH0 31:E27F: type = p + $50</summary>
    private static void SpawnH0(Machine m, int x)
    {
        Init(m, x, (m.Rd(0x2F) + 0x50) & 0xFF);
        SetTileHi(m, x);
    }

    // js: game/world/spawn.js spawnH3
    /// <summary>SpawnH3 31:E2A3: type = p + $D0</summary>
    private static void SpawnH3(Machine m, int x)
    {
        Init(m, x, (m.Rd(0x2F) + 0xD0) & 0xFF);
        SetTileHi(m, x);
    }

    // js: game/world/spawn.js spawnH4
    /// <summary>SpawnH4_Item 31:E222: type = p; p &amp; $F0 = $40 -> sprite palettes 2/3 = (area &amp; 7) + 9 and, in areas
    /// $C2-$C4 (flag $84) / $F5-$F7 (flag $27), p = $4E once that story flag is set.</summary>
    private static void SpawnH4(Machine m, int x)
    {
        if ((m.Rd(0x2F) & 0xF0) == 0x40)
        {
            int area = m.Rd(0x6C), pal = ((area & 7) + 9) & 0xFF;
            m.Wr(0x07E6, pal); m.Wr(0x07E7, pal);
            int f = area is 0xC2 or 0xC3 or 0xC4 ? 0x84 : area is 0xF5 or 0xF6 or 0xF7 ? 0x27 : -1;
            if (f >= 0)
            {
                m.Wr(0x10, m.Rd(FlagsWram + (f >> 3)));
                if ((Prg.CpuRd(m, 0xC000 + (f & 7)) & m.Rd(0x10)) != 0) m.Wr(0x2F, 0x4E);   // BitMask 30:C000
            }
        }
        Init(m, x, m.Rd(0x2F));
        SetTileHi(m, x);
    }

    // js: game/world/spawn.js spawnH1
    /// <summary>SpawnH1_Indirect 31:E2B5: 4-byte record at banks 4/5 $8000 + (p>>2)*$100 + $F0 + (p&amp;3)*4 -> $20-$23.
    /// rec[0] = $FF -> SpawnH1_TypeRec 31:E363 (type rec[1]); else type $30 with param1/2, behaviour, tile, phase,
    /// meta = orbit = rec[3], freeze = (X &amp; 3) + 1. Both -> SpawnH1_Tail 31:E340 (NpcHidden).</summary>
    private static void SpawnH1(Machine m, int x)
    {
        Prg.SwitchBank16(m, 2);
        int p = m.Rd(0x2F);
        m.Wr(0x24, ((p & 3) << 2) | 0xF0);
        m.Wr(0x25, (p >> 2) | 0x80);
        for (int i = 3; i >= 0; i--) m.Wr(0x20 + i, Prg.IndY(m, 0x24, i));
        if (m.Rd(0x20) == 0xFF)
        {                                                 // E363 SpawnH1_TypeRec
            Prg.SwitchBank16(m, 6);
            Init(m, x, m.Rd(0x21));
            SetTileHi(m, x);
        }
        else
        {
            Prg.SwitchBank16(m, 6);
            Init(m, x, 0x30);
            SetTileHi(m, x);
            m.Wr(Param1 + x, m.Rd(0x20));
            m.Wr(Param2 + x, m.Rd(0x21));
            int r2 = m.Rd(0x22);
            if ((r2 & 0x80) != 0)
            {
                m.Wr(Active + x, r2 & 0x7F);
                m.Wr(Behav + x, r2 & 0x7F);
            }
            else
            {
                m.Wr(Behav + x, r2);
                m.Wr(TileOfs + x, r2 & 0x70);
            }
            if ((m.Rd(Behav + x) & 4) == 0) m.Wr(Phase + x, H1Phase[m.Rd(0x2F) & 3]);   // E311
            if ((m.Rd(0x22) & 8) != 0) m.Wr(YLo + x, m.Rd(YLo + x) | 0x0E);
            m.Wr(Meta + x, m.Rd(0x23));
            m.Wr(Orbit + x, m.Rd(0x23));
            m.Wr(Freeze + x, (x & 3) + 1);
        }
        // SpawnH1_Tail 31:E340
        m.Wr(0x23, m.Rd(0x2F));
        m.Wr(0x15, x);
        Prg.SwitchBank16(m, 7);
        NpcHidden(m);
        Prg.SwitchBank16(m, 6);
        if (m.Rd(0x20) != 0) m.Wr(Active + x, 0);
    }

    // js: game/world/spawn.js spawnH2
    /// <summary>SpawnH2 31:E375: chest. p bit 7 -> type $0E; else type $0D, ChestPresence (none true -> obj_active = 0),
    /// and meta $4C for chest params $25/$3C/$3B.</summary>
    private static void SpawnH2(Machine m, int x)
    {
        int p = m.Rd(0x2F);
        if ((p & 0x80) != 0)
        {
            Init(m, x, 0x0E);
            SetTileHi(m, x);
            return;
        }
        Init(m, x, 0x0D);
        m.Wr(0x15, x);
        Prg.SwitchBank16(m, 7);
        m.Wr(0x23, p);
        ChestPresence(m);
        Prg.SwitchBank16(m, 6);
        if (m.Rd(0x20) == 0) m.Wr(Active + x, 0);
        SetTileHi(m, x);
        if (p == 0x25 || p == 0x3C || p == 0x3B) m.Wr(Meta + x, 0x4C);
    }

    // js: game/world/spawn.js npcFlagTest
    /// <summary>NpcFlagRef 14:8148 + NpcFlagTest 14:8135 on the ref at ($24),Y: returns (cond true, next Y).</summary>
    public static (bool Ok, int Y) NpcFlagTest(Machine m, int y)
    {
        int r0 = Prg.IndY(m, 0x24, y);
        m.Wr(0x26, r0); m.Wr(0x27, r0);
        int r1 = Prg.IndY(m, 0x24, (y + 1) & 0xFF);
        y = (y + 2) & 0xFF;
        m.Wr(0x28, Prg.CpuRd(m, 0x8167 + (r1 & 7)));
        int idx = ((r1 >> 3) | ((r0 & 7) << 5)) & 0xFF;   // LSR $27 / ROR A x3
        m.Wr(0x27, r0 >> 3);
        int v = m.Rd(FlagsWram + idx);
        if ((m.Rd(0x26) & 0x20) != 0) v ^= 0xFF;
        return ((v & m.Rd(0x28)) != 0, y);
    }

    // js: game/world/spawn.js npcHidden
    /// <summary>NpcHidden 14:80A0: per-param area list (NpcPresenceLo 14:85E0 / Hi 14:86E0 for params >= $80): `area cond..`
    /// groups (cond b7 = last, list ends at $FF); in a group for the current area, the first false condition sets $20 = 1.</summary>
    public static void NpcHidden(Machine m)
    {
        m.Wr(0x20, 0);
        int y = 0;
        int t = (m.Rd(0x23) << 1) & 0xFF, b = (m.Rd(0x23) & 0x80) != 0 ? 0x86E0 : 0x85E0;
        m.Wr(0x24, Prg.CpuRd(m, b + t));
        m.Wr(0x25, Prg.CpuRd(m, b + t + 1));
        for (; ; )
        {
            int a = Prg.IndY(m, 0x24, y);
            y = (y + 1) & 0xFF;
            if (a == 0xFF) return;
            if (a == m.Rd(0x6C))
            {
                for (; ; )
                {
                    var (ok, ny) = NpcFlagTest(m, y);
                    y = ny;
                    if (!ok) { m.Wr(0x20, m.Rd(0x20) + 1); return; }
                    if ((m.Rd(0x26) & 0x80) != 0) break;   // last condition: next area group
                }
            }
            else
            {
                for (; ; )
                {                                         // 80D9: skip the group's 2-byte conditions
                    int c = Prg.IndY(m, 0x24, y);
                    y = (y + 2) & 0xFF;
                    if ((c & 0x80) != 0) break;
                }
            }
        }
    }

    // js: game/world/spawn.js chestPresence
    /// <summary>ChestPresence 14:85BF: $20 = 1 if any condition of ChestPresencePtrs 14:9C82[$23] is true.</summary>
    public static void ChestPresence(Machine m)
    {
        m.Wr(0x20, 0);
        int t = (m.Rd(0x23) << 1) & 0xFF;
        m.Wr(0x24, Prg.CpuRd(m, 0x9C82 + t));
        m.Wr(0x25, Prg.CpuRd(m, 0x9C83 + t));
        int y = 0;
        for (; ; )
        {
            var (ok, ny) = NpcFlagTest(m, y);
            y = ny;
            if (ok) { m.Wr(0x20, m.Rd(0x20) + 1); return; }
            if ((m.Rd(0x26) & 0x80) != 0) return;
        }
    }
}
