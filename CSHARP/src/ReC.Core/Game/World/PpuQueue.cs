// re-c C# port — the PPU queue ring $6200 (= web/src/game/world/ppuqueue.js, Ph7 web 4/8 leftovers (a), s79).
// REVERSE.md "NMI tail in full" (PpuQueueFlush) and "Screen / metatile pipeline" step 6. Ported instruction by
// instruction: every RAM/WRAM byte the ROM writes is written here too.
//
// Ring: 8 records of 4 bytes at $6200 (hi, lo, len, src), read index $0A, write index $0B (both += 4 & $1F).
//   PpuQueueFlush(m)     PpuQueueFlush 30:C67D (NMI F40A; also called by ScreenRowRender with rendering off)
//   QueueHasSrc(m, a)    the 30:C72B test: true while a record with src byte == a is still queued (the ROM spins)
//   QueueEmpty(m)        WaitPpuQueue 30:C676 test ($0A == $0B)
//   QueueAttrAll(m)      30:C739: record $23C0 / len $BF (attribute path) / src 0, under INC/DEC nmi_disable
//   PpuCtrlNmiOn/Off     30:C436 / 30:C43E: PPUCTRL <- ppuctrl_shadow | $80 / & $7F
namespace ReC.Game.World;

public static class PpuQueue
{
    private const int Q = 0x6200, BUF = 0x6000, ATTR = 0x6100;

    // js: game/world/ppuqueue.js ppuCtrlNmiOn
    public static void PpuCtrlNmiOn(Machine m) => m.Ppu.WriteReg(0, m.Rd(0x00) | 0x80);
    // js: game/world/ppuqueue.js ppuCtrlNmiOff
    public static void PpuCtrlNmiOff(Machine m) => m.Ppu.WriteReg(0, m.Rd(0x00) & 0x7F);

    // js: game/world/ppuqueue.js queueEmpty
    public static bool QueueEmpty(Machine m) => m.Rd(0x0A) == m.Rd(0x0B);

    // js: game/world/ppuqueue.js queueHasSrc
    public static bool QueueHasSrc(Machine m, int a)
    {
        for (int y = 0x1C; y >= 0; y -= 4) if (m.Rd(Q + 3 + y) == a) return true;
        return false;
    }

    // js: game/world/ppuqueue.js ppuQueueFlush
    /// <summary>PpuQueueFlush 30:C67D: one record per call unless its hi byte has b7 (chain); the attribute path never
    /// chains. Returns the Y register at exit (s113; null = no record flushed).</summary>
    public static int? PpuQueueFlush(Machine m)
    {
        var ppu = m.Ppu;
        int? ry = null;
        for (; ; )
        {
            int x = m.Rd(0x0A);
            if (x == m.Rd(0x0B)) return ry;
            int hi = m.Rd(Q + x);
            m.Wr(0x0C, hi);
            ppu.WriteReg(0, (m.Rd(0x00) & 0xFB) | ((hi & 0x40) != 0 ? 4 : 0));
            ppu.WriteReg(6, hi);
            ppu.WriteReg(6, m.Rd(Q + 1 + x));
            int len = m.Rd(Q + 2 + x);
            m.Wr(0x0D, len);
            int y = m.Rd(Q + 3 + x);
            m.Wr(Q + 3 + x, 0xFF);
            if ((len & 0x80) != 0)
            {                                            // PpuQueueAttrCopy 30:C6E7: 8 x 8 bytes, abs,Y (no 8-bit wrap)
                for (int g = 0; g < 8; g++)
                {
                    for (int k = 0; k < 8; k++) ppu.WriteReg(7, Prg.CpuRd(m, ATTR + y + k));
                    y = (y + 8) & 0xFF;
                }
                m.Wr(0x0A, (m.Rd(0x0A) + 4) & 0x1F);
                return y;
            }
            int n = len;                                 // DEX / BNE: len 0 copies 256
            do
            {
                ppu.WriteReg(7, m.Rd(BUF + y));
                y = (y + 1) & 0xFF;
                n = (n - 1) & 0xFF;
            } while (n != 0);
            m.Wr(0x0A, (m.Rd(0x0A) + 4) & 0x1F);
            ry = y;
            if ((m.Rd(0x0C) & 0x80) == 0) return y;
        }
    }

    // js: game/world/ppuqueue.js queueAttrAll
    /// <summary>30:C739: queue the whole attribute table ($23C0, 64 bytes from $6100)</summary>
    public static void QueueAttrAll(Machine m)
    {
        m.Wr(0x60, m.Rd(0x60) + 1);
        int x = m.Rd(0x0B);
        m.Wr(Q + x, 0x23); m.Wr(Q + 1 + x, 0xC0); m.Wr(Q + 2 + x, 0xBF); m.Wr(Q + 3 + x, 0x00);
        m.Wr(0x0B, (x + 4) & 0x1F);
        m.Wr(0x60, m.Rd(0x60) - 1);
    }

    // js: game/world/ppuqueue.js queuePush
    /// <summary>append one record at $0B (the row / column renders' tail)</summary>
    public static void QueuePush(Machine m, int hi, int lo, int len, int src)
    {
        int x = m.Rd(0x0B);
        m.Wr(Q + x, hi); m.Wr(Q + 1 + x, lo); m.Wr(Q + 2 + x, len); m.Wr(Q + 3 + x, src);
        m.Wr(0x0B, (x + 4) & 0x1F);
    }
}
