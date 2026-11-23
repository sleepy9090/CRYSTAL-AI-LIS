// re-c C# port — BG row / column streaming into the PPU queue (= web/src/game/world/bgstream.js, s79).
// REVERSE.md "Screen / metatile pipeline" + "Background details". Instruction by instruction, including every
// zero-page temp and the bank registers ($50 $6E $6F, m.PrgR6/R7).
//
// $34-$37 (pos_x_lo/hi, pos_y_lo/hi) is the position being drawn; $30/$31 the step that moved it there.
//   BgRowRender(m)        BgRowRender 31:EB91 -> ScreenRowRender 31:EBC1 (record: NT row, len $20, src 0);
//                         rendering off -> PpuQueueFlush at once
//   BgColRender(m)        BgColRender 31:EC87 (record: hi b6 = +32 increment, len $1E, src $20)
//   FullScreenDraw(m, a)  30:C831: 30 rows bottom-up; A != 0 -> 30:C739 queues the attribute table
// All three are iterators: they yield where the ROM spins on the queue (30:C72B, WaitPpuQueue 30:C676).
using ReC.Game.Obj;

namespace ReC.Game.World;

public static class BgStream
{
    private const int Buf = 0x6000, Attr = 0x6100, Grid = 0x6300, Visited = 0x62F0;
    private const int PXl = 0x34, PXh = 0x35, PYl = 0x36, PYh = 0x37;

    // js: game/world/bgstream.js screenAtCamera
    /// <summary>ScreenAtCamera 31:EF1F -> (screen id, grid index Y)</summary>
    private static (int S, int Y) ScreenAtCamera(Machine m)
    {
        m.Wr(0x10, (m.Rd(PYh) << 3) & 0x78);
        int y = m.Rd(0x10) | (m.Rd(PXh) & 7);
        return (m.Rd(Grid + y), y);
    }

    // js: game/world/bgstream.js screenVisitedBit
    /// <summary>ScreenVisitedBit 31:EC6C: $62F0[Y>>3] &amp; $EC7F[Y&amp;7]</summary>
    private static int ScreenVisitedBit(Machine m, int y)
    {
        m.Wr(0x10, y & 7);
        return m.Rd(Visited + (y >> 3)) & Prg.CpuRd(m, 0xEC7F + (y & 7));
    }

    // js: game/world/bgstream.js screenPagePtr
    /// <summary>ScreenPagePtr 31:EF36: $11 = page hi, SwitchBank16(S>>6), $12 = pos_y_lo &amp; $F0</summary>
    private static void ScreenPagePtr(Machine m, int s)
    {
        m.Wr(0x11, (s & 0x3F) | 0x80);
        Prg.SwitchBank16(m, (s >> 6) & 3);
        m.Wr(0x12, m.Rd(PYl) & 0xF0);
    }

    // js: game/world/bgstream.js screenSubstPtr
    /// <summary>ScreenSubstPtr 31:ED89: $23/$24 = $BE00 + ($67 &amp; $3C) * 8 (the ASL carry feeds ADC #$BE)</summary>
    private static void ScreenSubstPtr(Machine m)
    {
        int v = (m.Rd(0x67) & 0x3C) << 3;
        m.Wr(0x23, v & 0xFF);
        m.Wr(0x24, 0xBE + (v >> 8));
    }

    // js: game/world/bgstream.js screenTileSubst
    /// <summary>ScreenTileSubst 31:ED99 (Y = page index, kept in $25)</summary>
    private static int ScreenTileSubst(Machine m, int a, int y)
    {
        if (a >= 0x20) return a;
        m.Wr(0x25, y);
        if (m.Rd(0x1E) == 0) return a;
        int r7 = m.Rd(0x6F);
        Prg.SetR7(m, 9);
        m.Wr(0x26, Prg.IndY(m, 0x23, a));
        Prg.SetR7(m, r7);
        return m.Rd(0x26);
    }

    // js: game/world/bgstream.js metatileAttrBits
    /// <summary>MetatileAttrBits 31:EECC: AttrReplicate[(tbl[M>>2] >> 2*(M&amp;3)) &amp; 3] for M = $6000[X]; Y -> $21</summary>
    private static int MetatileAttrBits(Machine m, int x, int y)
    {
        m.Wr(0x21, y);
        int mt = m.Rd(Buf + x);
        m.Wr(0x22, mt);
        int a = Prg.IndY(m, 0x10, mt >> 2);
        m.Wr(0x20, mt & 3);
        for (; ; )
        {
            m.Wr(0x20, m.Rd(0x20) - 1);
            if ((m.Rd(0x20) & 0x80) != 0) break;
            a >>= 2;
        }
        return Prg.CpuRd(m, 0xEF17 + (a & 3));
    }

    // js: game/world/bgstream.js attrMerge
    /// <summary>31:EE80 (row, Y step 1) / 31:EEA4 (column, Y step 8): merge two metatiles per attribute byte into $6100</summary>
    private static void AttrMerge(Machine m, int x, int y, int ystep)
    {
        do
        {
            m.Wr(0x15, MetatileAttrBits(m, x, y) & m.Rd(0x12));
            x = (x + 2) & 0xFF;
            m.Wr(0x15, (MetatileAttrBits(m, x, y) & m.Rd(0x13)) | m.Rd(0x15));
            m.Wr(Attr + y, (m.Rd(0x14) & m.Rd(Attr + y)) | m.Rd(0x15));
            y = (y + ystep) & 0xFF;
            x = (x + 2) & 0xFF;
        } while (x != m.Rd(0x16));
    }

    // js: game/world/bgstream.js attrSetup
    private static void AttrSetup(Machine m)
    {
        Prg.SetR7(m, 9);
        int i = (m.Rd(0x67) & 0x3C) >> 1;                 // MetatileAttrPtrTable 31:EEEF
        m.Wr(0x10, Prg.CpuRd(m, 0xEEEF + i));
        m.Wr(0x11, Prg.CpuRd(m, 0xEEF0 + i));
    }

    // js: game/world/bgstream.js metatileAttrRow
    /// <summary>MetatileAttr 31:EDE5 (row): $2F |= (($31>>4) ^ pos_y_lo) &amp; 8; nothing to do while $2F = 0</summary>
    private static void MetatileAttrRow(Machine m)
    {
        m.Wr(0x2F, (((m.Rd(0x31) >> 4) ^ m.Rd(PYl)) & 8) | m.Rd(0x2F));
        if (m.Rd(0x2F) == 0) return;
        AttrSetup(m);
        int a = m.Rd(PYl) >> 4, h = a & 1;
        int y = Prg.CpuRd(m, 0xEF07 + a);                 // AttrRowOffset
        m.Wr(0x12, Prg.CpuRd(m, 0xEF1B + h));
        m.Wr(0x13, Prg.CpuRd(m, 0xEF1D + h));
        m.Wr(0x14, ~(m.Rd(0x12) | m.Rd(0x13)));
        m.Wr(0x16, 0x20);
        AttrMerge(m, 0, y, 1);
    }

    // js: game/world/bgstream.js metatileAttrCol
    /// <summary>MetatileAttrCol 31:EE31 (column): $2F |= (($30>>4) ^ pos_x_lo) &amp; 8</summary>
    private static void MetatileAttrCol(Machine m)
    {
        m.Wr(0x2F, (((m.Rd(0x30) >> 4) ^ m.Rd(PXl)) & 8) | m.Rd(0x2F));
        if (m.Rd(0x2F) == 0) return;
        AttrSetup(m);
        int y = m.Rd(PXl) >> 5, h = (m.Rd(PXl) & 0x10) >> 3;
        m.Wr(0x12, Prg.CpuRd(m, 0xEF1B + h));
        m.Wr(0x13, Prg.CpuRd(m, 0xEF1C + h));
        m.Wr(0x14, ~(m.Rd(0x12) | m.Rd(0x13)));
        m.Wr(0x16, 0x40);
        AttrMerge(m, 0x20, y, 8);
    }

    // js: game/world/bgstream.js metatileExpand
    /// <summary>MetatileExpand 31:EDBA: 16 metatiles at $6000+X -> tile pairs via ($66),Y with $67 = ($67&amp;$FC)|$12 (+$13)</summary>
    private static void MetatileExpand(Machine m, int x)
    {
        m.Wr(0x14, (m.Rd(0x67) & 0xFC) | m.Rd(0x12));
        m.Wr(0x15, 0x10);
        do
        {
            m.Wr(0x67, m.Rd(0x14));
            int y = m.Rd(Buf + x);
            m.Wr(Buf + x, Prg.IndY(m, 0x66, y));
            m.Wr(0x67, m.Rd(0x67) + m.Rd(0x13));
            m.Wr(Buf + 1 + x, Prg.IndY(m, 0x66, y));
            x = (x + 2) & 0xFF;
            m.Wr(0x15, m.Rd(0x15) - 1);
        } while (m.Rd(0x15) != 0);
    }

    // js: game/world/bgstream.js camNtAddr
    /// <summary>CamNtAddr 31:ED64: $10/$11 = $2000 + (pos_y_lo>>3)*32 + (pos_x_lo>>3)</summary>
    private static void CamNtAddr(Machine m)
    {
        m.Wr(0x10, m.Rd(PXl) >> 3);
        m.Wr(0x11, 0x20);
        m.Wr(0x10, (((m.Rd(PYl) & 0xF8) << 2) & 0xFF) | m.Rd(0x10));
        m.Wr(0x11, ((m.Rd(PYl) >> 6) + 0x20) | m.Rd(0x11));
    }

    // js: game/world/bgstream.js screenRowRender
    /// <summary>ScreenRowRender 31:EBC1 (entered from BgRowRender with $1E/$1F/$20/$21 and the first page set up)</summary>
    private static void ScreenRowRender(Machine m)
    {
        m.Wr(0x10, m.Rd(PYl) & 0xF0);
        int y = m.Rd(PXl) >> 4, x = (m.Rd(PXl) & 0xF0) >> 3;
        m.Wr(0x13, ((m.Rd(PXl) & 8) >> 3) + 0x10);
        ScreenSubstPtr(m);
        if ((m.Rd(0x62FF) & 0x80) != 0) Prg.SetR6(m, 0x0A);   // bank-10 pages: first screen of a row only (the quirk)
        for (; ; )
        {
            m.Wr(Buf + x, ScreenTileSubst(m, Prg.IndY(m, 0x10, y), y));
            y = (y + 1) & 0xFF;
            x = (x + 2) & 0xFF;
            m.Wr(0x13, m.Rd(0x13) - 1);
            if (m.Rd(0x13) == 0) break;
            if (y != 0x10) continue;
            ScreenPagePtr(m, m.Rd(0x21));                 // EC04: next screen, no SetR6($0A)
            m.Wr(0x1E, m.Rd(0x1F));
            x = 0; y = 0;
        }
        MetatileAttrRow(m);
        Prg.SwitchBank16(m, 4);
        m.Wr(0x12, (m.Rd(PYl) & 8) >> 2);
        m.Wr(0x13, 1);
        MetatileExpand(m, 0);
        CamNtAddr(m);
        PpuQueue.PpuCtrlNmiOff(m);
        PpuQueue.QueuePush(m, m.Rd(0x11) | m.Rd(0x0744), m.Rd(0x10) & 0xE0, 0x20, 0);
        if ((m.Rd(0x01) & 0x18) != 0) { PpuQueue.PpuCtrlNmiOn(m); return; }
        int ctrl = m.Rd(0x00);                            // rendering off: flush now (NMI bit clear in the shadow)
        m.Wr(0x00, ctrl & 0x7F);
        PpuQueue.PpuQueueFlush(m);
        m.Wr(0x00, ctrl);
        PpuQueue.PpuCtrlNmiOn(m);
    }

    // js: game/world/bgstream.js bgRowRender
    /// <summary>BgRowRender 31:EB91</summary>
    public static IEnumerable<Y> BgRowRender(Machine m)
    {
        while (PpuQueue.QueueHasSrc(m, 0)) yield return Y.Frame;   // 30:C72B (A = 0): no queued record uses $6000
        if ((m.Rd(0x31) & 0x80) == 0) m.Wr(PYh, m.Rd(PYh) + 1);
        var (s, y) = ScreenAtCamera(m);
        m.Wr(0x20, s);
        m.Wr(0x1E, ScreenVisitedBit(m, y));
        m.Wr(0x10, y & 0xF8);
        y = ((y + 1) & 7) | m.Rd(0x10);                   // next screen to the right, same grid row
        m.Wr(0x21, m.Rd(Grid + y));
        m.Wr(0x1F, ScreenVisitedBit(m, y));
        ScreenPagePtr(m, m.Rd(0x20));
        ScreenRowRender(m);
    }

    // js: game/world/bgstream.js bgColRender
    /// <summary>BgColRender 31:EC87</summary>
    public static IEnumerable<Y> BgColRender(Machine m)
    {
        while (PpuQueue.QueueHasSrc(m, 0x20)) yield return Y.Frame;   // 30:C72B (A = $20)
        m.Wr(0x18, 0);
        m.Wr(0x19, 1);
        if ((m.Rd(0x30) & 0x80) == 0) m.Wr(PXh, m.Rd(PXh) + 1);
        var (s, y) = ScreenAtCamera(m);
        m.Wr(0x20, s);
        m.Wr(0x1E, ScreenVisitedBit(m, y));
        int a = (y + 8) & 0xFF;                           // next screen down: grid index + 8, 12 rows ($60) wrap
        if ((a & 0x80) != 0) a &= 7;
        if (a >= 0x60) a -= 0x5F;
        y = a;
        m.Wr(0x21, m.Rd(Grid + y));
        m.Wr(0x1F, ScreenVisitedBit(m, y));
        ScreenPagePtr(m, m.Rd(0x20));
        m.Wr(0x10, (m.Rd(PXl) & 0xF0) >> 4);
        y = (m.Rd(PYl) & 0xF0) | m.Rd(0x10);
        m.Wr(0x10, 0);
        int x = (((m.Rd(PYl) & 0xF0) >> 3) + 0x20) & 0xFF;
        m.Wr(0x13, ((m.Rd(PYl) & 8) >> 3) + 0x0F);
        m.Wr(0x14, 0);
        ScreenSubstPtr(m);
        for (; ; )
        {
            m.Wr(Buf + x, ScreenTileSubst(m, Prg.IndY(m, 0x10, y), y));
            x = (x + 2) & 0xFF;
            y = (y + 0x10) & 0xFF;
            m.Wr(0x13, m.Rd(0x13) - 1);
            if (m.Rd(0x13) == 0) break;
            if (y < 0xF0) continue;
            m.Wr(0x1E, m.Rd(0x1F));
            ScreenPagePtr(m, m.Rd(0x21));
            y &= 0x0F;
            x = 0x20;
        }
        MetatileAttrCol(m);
        Prg.SwitchBank16(m, 4);
        m.Wr(0x12, (m.Rd(PXl) & 8) >> 3);
        m.Wr(0x13, 2);
        MetatileExpand(m, 0x20);
        CamNtAddr(m);
        PpuQueue.PpuCtrlNmiOff(m);
        PpuQueue.QueuePush(m, (m.Rd(0x11) & 0x30) | 0x40, m.Rd(0x10) & 0x1F, 0x1E, 0x20);
        PpuQueue.PpuCtrlNmiOn(m);
    }

    // js: game/world/bgstream.js FSD_LAG
    /// <summary>FullScreenDraw with rendering off (AreaEnter): measured in tools.emu (s108, area 0, f2340-2345) the NMIs
    /// fall after rows 7, 13, 19, 25 (~6 rows of ~43 scanlines per frame) and after the last row.</summary>
    public static readonly Func<int, bool> FsdLag = n => n == 30 || (n > 1 && n % 6 == 1);

    // js: game/world/bgstream.js FSD_LAG_WF
    /// <summary>s161: the same draw started right after a WaitFrame (BossHookA6 15:B8FD): NMIs after rows 9, 15, 21, 27</summary>
    public static readonly Func<int, bool> FsdLagWf = n => n == 29 || (n < 30 && n % 6 == 0);

    // js: game/world/bgstream.js fullScreenDraw
    /// <summary>30:C831: draw the 30 rows of the screen at $34-$37 (bottom-up), banks restored; A != 0 -> 30:C739</summary>
    public static IEnumerable<Y> FullScreenDraw(Machine m, int a, Func<int, bool>? lagRows = null)
    {
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        m.Wr(PYh, m.Rd(PYh) + 1);
        m.Wr(0x2E, 0x1E);
        int row = 0;
        do
        {
            while (!PpuQueue.QueueEmpty(m)) yield return Y.Frame;   // WaitPpuQueue 30:C676
            Prg.SetR6(m, 0x1A);
            m.Wr(0x30, 0);
            m.Wr(0x31, 0xF8);
            ObjUtil.TempAddOffset(m);                     // 26:9824: one row (8 px) up
            foreach (var s in BgRowRender(m)) yield return s;
            m.Wr(0x2E, m.Rd(0x2E) - 1);
            // s108: an NMI after row n finds frame_ready set (no DMA / scroll copy) -> SPIN
            if (lagRows != null && lagRows(++row)) yield return Y.Spin;
        } while (m.Rd(0x2E) != 0);
        Prg.SetR7(m, r7);
        Prg.SetR6(m, r6);
        if ((a & 0xFF) != 0) PpuQueue.QueueAttrAll(m);
    }

    // js: game/world/bgstream.js runNow
    /// <summary>run an iterator to the end within this frame; throws if it would wait for an NMI (checks / tools)</summary>
    public static void RunNow(IEnumerable<Y> gen)
    {
        foreach (var _ in gen) throw new InvalidOperationException("routine waits for the PPU queue (NMI)");
    }
}
