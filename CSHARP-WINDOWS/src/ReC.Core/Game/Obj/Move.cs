// re-c C# port — bank 26 movement with BG collision (= web/src/game/obj/move.js, s80). REVERSE.md "World interaction"
// (ObjMoveStep / BgProbePoints / ObjScreenEdge, s46) and "Per-frame order inside ObjUpdateAll".
//
//   ObjMoveSlide(m, x)   ObjMoveSlide 26:98D7 -> C (true = blocked). A blocked diagonal also tries dir-1 then dir+1.
//                        $23 = the original dir, $25 = OR of the probes.
//   ObjMoveStep(m, x)    ObjMoveStep 26:9947 -> Moved | C (JS 'moved' / true / false). On success the ROM PLA PLAs the
//                        return address of ObjMoveSlide's JSR, so ObjMoveSlide itself returns with C = 0.
//   ObjScreenEdge(m, x)  ObjScreenEdge 26:99FF (slot 0 only) -> C (true = outside the screen box)
namespace ReC.Game.Obj;

public static class Move
{
    private const int XLo = 0x70, XHi = 0x90, YLo = 0xB0, YHi = 0xD0;
    private const int Flags2 = 0x0340, MetaDir = 0x0360, Flags = 0x0380, BgMask = 0x0460;
    private const int Obj1Flash = 0x0710;

    /// <summary>ObjMoveStep's result: JS false (C = 0) / true (C = 1) / 'moved'</summary>
    public enum Step { Clear, Blocked, Moved }

    // js: game/obj/move.js objScreenEdge
    public static bool ObjScreenEdge(Machine m, int x)
    {
        int sx = (m.Rd(0x34) - m.Rd(0x02)) & 0xFF;
        bool @out = sx < 0x0A || sx >= 0xFC;
        if (!@out)
        {
            int sy = m.Rd(0x36) - m.Rd(0x04);
            if (sy < 0) sy = (sy - 0x0F - 1) & 0xFF;              // 9A13 SBC #$0F with C = 0 (borrow) -> -16
            @out = sy < 0x18 || sy >= 0xD0;
        }
        m.Wr(0x20, (m.Rd(Flags + x) & 0xA0) | (@out ? 6 : 0));
        return @out;
    }

    // js: game/obj/move.js objMoveStep
    public static Step ObjMoveStep(Machine m, int x)
    {
        m.Wr(0x34, m.Rd(XLo + x)); m.Wr(0x35, m.Rd(XHi + x)); m.Wr(0x36, m.Rd(YLo + x)); m.Wr(0x37, m.Rd(YHi + x));
        int dir = m.Rd(MetaDir + x) & 7;
        m.Wr(MetaDir + x, dir);
        m.Wr(0x24, dir << 3);
        ObjUtil.ObjStepVector(m, x, dir);
        ObjUtil.TempAddStep(m);
        m.Wr(0x1C, m.Rd(0x34)); m.Wr(0x1D, m.Rd(0x35)); m.Wr(0x1E, m.Rd(0x36)); m.Wr(0x1F, m.Rd(0x37));
        for (; ; )
        {                                                         // 997D: walk the probe points
            int y = m.Rd(0x24);
            m.Wr(0x30, Prg.CpuRd(m, 0x9907 + y));
            m.Wr(0x31, Prg.CpuRd(m, 0x9908 + y));
            if ((m.Rd(0x30) | m.Rd(0x31)) == 0) break;            // all points clear -> 99D4
            m.Wr(0x24, (y + 2) & 0xFF);
            ObjUtil.TempAddStep(m);
            if (x == 0)
            {
                if (ObjScreenEdge(m, x)) return Step.Blocked;     // 999B BCS -> RTS (C = 1)
                bool c = ObjUtil.ObjBgProbe(m, x, 0x80);
                m.Wr(0x25, m.Rd(0x20) | m.Rd(0x25));
                if (m.Rd(Flags2 + x) == 0x08 && (m.Rd(Obj1Flash) & 0x40) != 0)
                {                                                 // flight / warp exit: mask $24
                    if (c) continue;                              // 99B4 BCS (the same-cell cache hit)
                    if (m.Rd(0x20) == 0) return Step.Clear;       // 99B8 BEQ -> RTS with C = 0
                    if ((m.Rd(0x20) & 0x24) == 0) continue;
                    return Step.Clear;                            // 99BE RTS, C still 0
                }
            }
            else ObjUtil.ObjBgProbe(m, x, 0x80);
            int mask = x == 0 ? 0x06 : 0x27;                      // 99C4 (CPX #0 leaves C = 1)
            if (x == 0 && m.NoClip) continue;                     // host cheat (Windows: walk through walls); off = the ROM
            if ((m.Rd(0x20) & mask & m.Rd(BgMask + x)) != 0) return Step.Blocked;
        }
        // 99D4: commit (PLA PLA = return from ObjMoveSlide)
        m.Wr(XLo + x, m.Rd(0x1C)); m.Wr(0x34, m.Rd(0x1C));
        m.Wr(XHi + x, m.Rd(0x1D)); m.Wr(0x35, m.Rd(0x1D));
        m.Wr(YLo + x, m.Rd(0x1E)); m.Wr(0x36, m.Rd(0x1E));
        m.Wr(YHi + x, m.Rd(0x1F)); m.Wr(0x37, m.Rd(0x1F));
        ObjUtil.ObjBgProbe(m, x, 0x40);
        int cur = m.Rd(MetaDir + x);
        m.Wr(MetaDir + x, m.Rd(0x23));
        m.Wr(0x23, cur);
        return Step.Moved;
    }

    // js: game/obj/move.js objMoveSlide
    public static bool ObjMoveSlide(Machine m, int x)
    {
        m.Wr(0x25, 0);
        m.Wr(0x23, m.Rd(MetaDir + x));
        if (ObjMoveStep(m, x) == Step.Moved) return false;
        if ((m.Rd(MetaDir + x) & 1) != 0)
        {                                                         // 98E6 LSR: odd = diagonal
            m.Wr(0x25, 0);
            m.Wr(MetaDir + x, m.Rd(MetaDir + x) - 1);
            if (ObjMoveStep(m, x) == Step.Moved) return false;
            m.Wr(0x25, 0);
            m.Wr(MetaDir + x, m.Rd(MetaDir + x) + 2);
            if (ObjMoveStep(m, x) == Step.Moved) return false;
        }
        m.Wr(MetaDir + x, m.Rd(0x23));                            // 9900
        return true;
    }
}
