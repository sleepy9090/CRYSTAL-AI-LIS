// re-c C# port — bank 26 object helpers (= web/src/game/obj/util.js, s55; class ObjUtil: `Util` would clash with
// the ReC.Util namespace). REVERSE.md "Enemies — catalogue + generic enemy AIs", "World interaction", "Shots".
// Every zero-page temp the ROM writes is written here too (obj_check.mjs diffs the whole RAM + WRAM).
//
//   Rand64(m)                 Rand64 26:97D7 -> value
//   ObjFindFree(m, y)         ObjFindFree 26:98B9 (end slot $10) -> free slot or -1 (C = 0)
//   ObjPosToTemp / TempToObjPos (26:98A8 / 26:9897), TempAddOffset 26:9824, TempAddStep 26:9861
//   DirToTarget(m, x, y)      DirToTarget 26:8409 -> $20 dir, $21 distance
//   ObjStepVector(m, x, dir)  ObjStepVector 26:8480 -> $30 dx, $31 dy
//   ObjDespawn / ObjBecomeNext (26:98C7 / 26:98CD)
//   SpawnShot(m, x, id, dir)  SpawnShot 26:972D -> true if at least one object spawned
//   ObjBgProbe(m, x, a[, n])  ObjBgProbe 26:9A30 -> C (true only for the A b7 same-cell cache hit); n = the caller's
//                             N flag (a &amp; $80 unless given; PlayerMove 26:9EFD passes the jump timer's)
namespace ReC.Game.Obj;

public static class ObjUtil
{
    private const int XLo = 0x70, XHi = 0x90, YLo = 0xB0, YHi = 0xD0;
    private const int Flags2 = 0x0340, MetaDir = 0x0360, Flags = 0x0380, BgMask = 0x0460, Timer = 0x0480, Active = 0x04A0;
    private const int DeathT = 0x04C0, Behav = 0x06E0;
    private const int PXl = 0x34, PXh = 0x35, PYl = 0x36, PYh = 0x37;

    // js: game/obj/util.js rand64
    public static int Rand64(Machine m)
    {
        m.Wr(0x0E, (m.Rd(0x0E) + 1) & 0x3F);
        return Prg.CpuRd(m, 0x97E4 + m.Rd(0x0E));
    }

    // js: game/obj/util.js objFindFree
    public static int ObjFindFree(Machine m, int y)
    {
        for (; ; )
        {
            if (m.Rd(Active + y) == 0) return y;
            y = (y + 1) & 0xFF;
            if (y == m.Rd(0x10)) return -1;
        }
    }

    // js: game/obj/util.js zp
    /// <summary>zero-page,X: the slot position columns wrap inside page 0 (s104)</summary>
    private static int Zp(int a) => a & 0xFF;

    // js: game/obj/util.js objPosToTemp
    public static void ObjPosToTemp(Machine m, int x)
    {
        m.Wr(PXl, m.Rd(Zp(XLo + x))); m.Wr(PXh, m.Rd(Zp(XHi + x)));
        m.Wr(PYl, m.Rd(Zp(YLo + x))); m.Wr(PYh, m.Rd(Zp(YHi + x)));
    }

    // js: game/obj/util.js tempToObjPos
    public static void TempToObjPos(Machine m, int x)
    {
        m.Wr(Zp(XLo + x), m.Rd(PXl)); m.Wr(Zp(XHi + x), m.Rd(PXh));
        m.Wr(Zp(YLo + x), m.Rd(PYl)); m.Wr(Zp(YHi + x), m.Rd(PYh));
    }

    // js: game/obj/util.js addX
    /// <summary>shared x part of 9824 / 9861: signed $30 to $34/$35</summary>
    private static void AddX(Machine m)
    {
        int dx = m.Rd(0x30), s = m.Rd(PXl) + dx;
        m.Wr(PXl, s);
        if ((dx & 0x80) != 0) { if (s < 0x100) m.Wr(PXh, m.Rd(PXh) - 1); }
        else if (s >= 0x100) m.Wr(PXh, m.Rd(PXh) + 1);
    }

    // js: game/obj/util.js tempAddOffset
    /// <summary>26:9824: add signed $30/$31 to $34-$37, y wraps at 240 with the ADC carry tested first</summary>
    public static void TempAddOffset(Machine m)
    {
        AddX(m);
        int dy = m.Rd(0x31), s = dy + m.Rd(PYl), r = s & 0xFF;
        if ((dy & 0x80) == 0)
        {
            if (s >= 0x100 || r >= 0xF0) { m.Wr(PYh, m.Rd(PYh) + 1); m.Wr(PYl, r + 0x10); } else m.Wr(PYl, r);
        }
        else if (s < 0x100 || r >= 0xF0) { m.Wr(PYl, r + 0xF0); m.Wr(PYh, m.Rd(PYh) - 1); }
        else m.Wr(PYl, r);
    }

    // js: game/obj/util.js tempAddStep
    /// <summary>TempAddStep 26:9861: like 9824, but y only tests the sum against $F0 (the ADC carry is ignored)</summary>
    public static void TempAddStep(Machine m)
    {
        AddX(m);
        int dy = m.Rd(0x31), r = (dy + m.Rd(PYl)) & 0xFF;
        if (r < 0xF0) { m.Wr(PYl, r); return; }
        if ((dy & 0x80) == 0) { m.Wr(PYl, r + 0x10); m.Wr(PYh, m.Rd(PYh) + 1); }
        else { m.Wr(PYl, r + 0xF0); m.Wr(PYh, m.Rd(PYh) - 1); }
    }

    // js: game/obj/util.js dirToTarget
    public static void DirToTarget(Machine m, int x, int ty)
    {
        int d = m.Rd(XLo + ty) - m.Rd(XLo + x);
        m.Wr(0x10, d);
        int h = (m.Rd(XHi + ty) - m.Rd(XHi + x) - (d < 0 ? 1 : 0)) & 0xFF;
        m.Wr(0x11, h);
        int a;
        if (((h & 0x80) != 0 ? h ^ 0xFF : h) == 0)
        {                                                         // 8422: ($11:$10) >> 4
            a = m.Rd(0x10);
            for (int i = 0; i < 4; i++)
            {
                int v = m.Rd(0x11), c = v & 1;
                m.Wr(0x11, v >> 1);
                a = (a >> 1) | (c << 7);
            }
        }
        else a = h;                                               // 841E: |dx| >= 256 -> the hi byte is the cell
        m.Wr(0x10, (a + 0x10) & 0x1F);
        d = m.Rd(YLo + ty) - m.Rd(YLo + x);
        m.Wr(0x11, d);
        h = (m.Rd(YHi + ty) - m.Rd(YHi + x) - (d < 0 ? 1 : 0)) & 0xFF;
        int y = h;
        if (((h & 0x80) != 0 ? h ^ 0xFF : h) != 0)
        {                                                         // 844B
            m.Wr(0x11, h << 4);
            y = h >> 4;
        }
        a = m.Rd(0x11) & 0xF0;                                    // 8458
        int cc = a >> 7;
        m.Wr(0x10, ((a << 1) & 0xFF) | m.Rd(0x10));
        m.Wr(0x11, ((((y << 1) | cc) + 2) & 3) + 0x80);
        int val = Prg.IndY(m, 0x10, 0);
        m.Wr(0x20, val & 0x0F);
        m.Wr(0x21, val >> 4);
    }

    // js: game/obj/util.js objStepVector
    public static void ObjStepVector(Machine m, int x, int dir)
    {
        m.Wr(0x12, dir << 3);
        int y = m.Rd(Flags2 + x) & 0x0F;
        if ((m.Rd(Flags + x) & 0x40) != 0)
        {                                                         // 848E: sinking -> speed - 2, floor 0
            y = (y - 2) & 0xFF;
            if ((y & 0x80) != 0) y = 0;
        }
        m.Wr(0x10, Prg.CpuRd(m, 0x84BC + y));
        m.Wr(0x11, Prg.CpuRd(m, 0x84CC + y));
        m.Wr(0x13, Prg.CpuRd(m, 0x84DC + y));
        y = (m.Rd(Timer + x) & 7) | m.Rd(0x12);
        m.Wr(0x31, Prg.IndY(m, 0x10, y));
        y = (y + m.Rd(0x13)) & 0xFF;
        m.Wr(0x30, Prg.IndY(m, 0x10, y));
    }

    // js: game/obj/util.js objDespawn
    public static void ObjDespawn(Machine m, int x) => m.Wr(Active + x, 0);

    // js: game/obj/util.js objBecomeNext
    public static void ObjBecomeNext(Machine m, int x)
    {
        m.Wr(0x11, m.Rd(DeathT + x));
        m.Wr(0x10, x);
        ObjInit.FarObjInit(m);
    }

    // js: game/obj/util.js spawnShot
    public static bool SpawnShot(Machine m, int x, int id, int dir)
    {
        m.Wr(0x14, dir);
        int yy = (id << 2) & 0xFF, page = (id & 0x40) != 0 ? 0xBD00 : 0xBC00;
        int saved = m.Rd(0x6F);
        Prg.SetR7(m, 0x14);                                       // 9735
        m.Wr(0x23, 0);
        m.Wr(0x21, Prg.CpuRd(m, page + yy)); m.Wr(0x22, Prg.CpuRd(m, page + yy + 1));
        m.Wr(0x11, Prg.CpuRd(m, page + yy + 2)); m.Wr(0x20, Prg.CpuRd(m, page + yy + 3));
        if ((m.Rd(0x21) & 0x80) != 0) { Prg.SetR7(m, saved); return false; }   // 976D
        do
        {
            m.Wr(0x10, m.Rd(0x22));                               // 9773
            int y = ObjFindFree(m, m.Rd(0x21));
            if (y < 0) break;
            m.Wr(0x10, y);
            ObjInit.FarObjInit(m);                                // 9780
            ObjPosToTemp(m, x);
            int f = m.Rd(Flags + x);
            m.Wr(0x12, f);
            int n = m.Rd(0x10);
            m.Wr(Flags + n, m.Rd(Flags + n) | (f & 0x80));
            m.Wr(0x31, (f & 0x40) != 0 ? 2 : 0);
            int d = m.Rd(0x14);
            m.Wr(MetaDir + n, d);
            m.Wr(0x13, (d << 1) & 0x0F);
            int oy = ((m.Rd(Behav + n) << 4) & 0xFF) | m.Rd(0x13);   // 97AB: the NEW object's obj_behav
            m.Wr(0x30, Prg.CpuRd(m, 0xBD80 + oy));
            m.Wr(0x31, Prg.CpuRd(m, 0xBD81 + oy) + m.Rd(0x31));
            TempAddOffset(m);
            TempToObjPos(m, n);
            m.Wr(0x23, m.Rd(0x23) - 1);
            m.Wr(0x20, m.Rd(0x20) - 1);
        } while (m.Rd(0x20) != 0);
        Prg.SetR7(m, saved);                                      // 97D0
        int t = m.Rd(0x23);
        m.Wr(0x23, t << 1);
        return (t & 0x80) != 0;
    }

    // js: game/obj/util.js objBgProbe
    public static bool ObjBgProbe(Machine m, int x, int a, int? nFlag = null)
    {
        int n = nFlag ?? (a & 0x80);
        m.Wr(0x14, a);                                            // STA $14 keeps the flags: BPL tests the caller's N
        if (n != 0 && ((m.Rd(PYl) ^ m.Rd(YLo + x)) & 0xF0) == 0 && ((m.Rd(PXl) ^ m.Rd(XLo + x)) & 0xF0) == 0)
        {
            m.Wr(0x20, m.Rd(Flags + x) & 0x50);                   // 9A44 same 16 px cell: cached
            return true;
        }
        int gi = ((m.Rd(PYh) << 3) | m.Rd(PXh)) & 0xFF;           // 9A4D
        int g = m.Rd(0x6300 + gi);
        m.Wr(0x11, (g & 0x1F) | 0xA0);
        Prg.SetR7(m, g >> 5);                                     // 9A67 (inline: $6F, $50 = 7, R7)
        m.Wr(0x12, m.Rd(PYl) & 0xF0);
        m.Wr(0x10, (m.Rd(PXl) >> 4) | m.Rd(0x12));
        m.Wr(0x68, Prg.IndY(m, 0x10, 0));
        Prg.SetR7(m, 9);                                          // 9A89
        m.Wr(0x20, Prg.IndY(m, 0x68, 0));
        if ((m.Rd(0x20) & 8) != 0 && (m.Rd(0x62F0 + m.Rd(PYh)) & Prg.CpuRd(m, 0x9AE0 + m.Rd(PXh))) != 0)
        {                                                         // 9A9D visited subst
            m.Wr(0x68, Prg.IndY(m, 0x6A, m.Rd(0x68)));
            m.Wr(0x20, Prg.IndY(m, 0x68, 0));
        }
        Prg.SetR7(m, 0x1B);                                       // 9AB9
        if ((m.Rd(0x14) & 0x40) != 0)
        {
            int f = m.Rd(Flags + x) & 0xAF;
            m.Wr(0x10, f);
            m.Wr(Flags + x, (m.Rd(0x20) & m.Rd(BgMask + x) & 0x50) | f);
        }
        return false;
    }
}
