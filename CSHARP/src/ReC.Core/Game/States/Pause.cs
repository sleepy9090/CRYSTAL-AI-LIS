// re-c C# port — the pause / status screen (= web/src/game/states/pause.js, s94): PauseScreen 15:BC40 + PauseWait
// 15:BDDB (bank pair 7), StatusCondText 15:BE11, EquipNameRow 15:BE7A, StatusPlayerSprite 15:BEBF, ObjSlot1FDraw
// 30:C1AE, PpuQueueBlock 30:C482. REVERSE.md "State-1 substates" rows 12/13 + "Pause / status screen (web port)".
//
//   PauseScreen(m, res)     PauseScreen + PauseWait; res.V = the Y register ReadPads leaves (SubstPlay goes on with it)
//   PpuQueueBlock(m, a, res) PpuQueueBlock 30:C482 (block table 30:C518, 5 bytes per id); res.V = Y it leaves
using ReC.Game.Obj;
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class Pause
{
    private const int Buf = 0x6000;

    // js: game/states/pause.js waitPpuQueue
    /// <summary>WaitPpuQueue 30:C676 (spin: frame_ready stays set)</summary>
    public static IEnumerable<Y> WaitPpuQueue(Machine m)
    {
        while (!PpuQueue.QueueEmpty(m)) yield return Y.Spin;
    }

    // js: game/states/pause.js ppuQueueBlock
    /// <summary>PpuQueueBlock 30:C482: A = block id; record (hi lo len src count) from 30:C518 + 5*id into $20-$24, then
    /// count&amp;$7F + 1 records, each len bytes on (lo += max(len, $20), src += len); waits for the queue to drain after
    /// each record unless hi b7, and at the end unless count b7. res.V = the Y register it leaves (5, or
    /// PpuQueueFlush's Y when rendering is off; s120).</summary>
    public static IEnumerable<Y> PpuQueueBlock(Machine m, int a, Ret<int>? res = null)
    {
        m.Wr(0x10, a);
        foreach (var s in WaitPpuQueue(m)) yield return s;
        int p = 0xC518 + a * 5;
        m.Wr(0x26, (p >> 8) & 0xFF);
        for (int y = 0; y < 5; y++) m.Wr(0x20 + y, Prg.CpuRd(m, p + y));
        m.Wr(0x25, m.Rd(0x24) & 0x7F);
        int yReg = 5;
        do
        {
            PpuQueue.PpuCtrlNmiOff(m);                            // C4BE
            int x = m.Rd(0x0B);
            for (int i = 0; i < 4; i++) m.Wr(0x6200 + x + i, m.Rd(0x20 + i));
            m.Wr(0x0B, (x + 4) & 0x1F);
            if ((m.Rd(R.PpumaskShadow) & 0x18) == 0)
            {                                                     // C4E5 JSR C8B2
                int? fy = Death.QueueFlushNow(m);
                if (fy != null) yReg = fy.Value;
            }
            PpuQueue.PpuCtrlNmiOn(m);
            int len = m.Rd(0x22);
            int lo = m.Rd(0x21) + (len < 0x20 ? 0x20 : len);      // C4EB: next row (at least 32 bytes on)
            m.Wr(0x21, lo & 0xFF);
            if (lo > 0xFF) m.Wr(0x20, (m.Rd(0x20) + 1) & 0xFF);
            m.Wr(0x23, (m.Rd(0x23) + len) & 0xFF);
            if ((m.Rd(0x20) & 0x80) == 0) foreach (var s in WaitPpuQueue(m)) yield return s;   // C503
            m.Wr(0x25, (m.Rd(0x25) - 1) & 0xFF);
        } while ((m.Rd(0x25) & 0x80) == 0);
        if ((m.Rd(0x24) & 0x80) == 0) foreach (var s in WaitPpuQueue(m)) yield return s;       // C50E
        if (res != null) res.V = yReg;
    }

    // js: game/states/pause.js objSlot1FDraw
    /// <summary>30:C1AE: $20+1 sprite records of 8 bytes at $6000 (x y meta tile_ofs flags flags2 dir anim) drawn one by
    /// one through object slot $1F (28:8000 -> ObjDrawMeta with $1B = 1), OAM from offset 4, then ObjHideUnusedOam.</summary>
    public static void ObjSlot1FDraw(Machine m)
    {
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        Prg.SwitchBank16(m, 0x0E);
        m.Wr(0x10, 4);
        do
        {
            int x = (m.Rd(0x20) << 3) & 0xFF;
            // js: game/states/pause.js b
            int B(int i) => m.Rd(Buf + x + i);
            m.Wr(0x12, B(0)); m.Wr(0x13, B(1));
            m.Wr(0x031F, B(2)); m.Wr(0x059F, B(3)); m.Wr(0x039F, B(4));
            m.Wr(0x17, (B(4) << 1) & 0xFF);
            m.Wr(0x033F, B(5)); m.Wr(0x037F, B(6)); m.Wr(0x04FF, B(7));
            m.Wr(0x04BF, 1);
            m.Wr(0x1B, 1);                                        // 28:8000 LDX #$1F, LDY #1, STY $1B
            Sprites.DrawMeta(m, 0x1F);
            m.Wr(0x04BF, 0);
            m.Wr(0x20, (m.Rd(0x20) - 1) & 0xFF);
        } while ((m.Rd(0x20) & 0x80) == 0);
        Sprites.HideUnusedOam(m);                                 // C205 JSR 28:80A5
        Prg.SetR7(m, r7);
        Prg.SetR6(m, r6);
    }

    // js: game/states/pause.js statusPlayerSprite
    /// <summary>15:BEBF: the player figure on the status screen (normal = meta $0B at (48,54); changed form / stone =
    /// 8-byte record from 15:BF1E).</summary>
    private static IEnumerable<Y> StatusPlayerSprite(Machine m)
    {
        foreach (var s in WaitPpuQueue(m)) yield return s;
        int f = m.Rd(0x0710);
        int y = -1;
        if ((f & 0x80) != 0) y = (f >> 1) & 0x38;
        else if ((f & 0x0F) == 4) y = 0x20;
        if (y >= 0)
        {
            for (int x = 0; x < 8; x++) m.Wr(Buf + x, Prg.CpuRd(m, 0xBF1E + y + x));
        }
        else
        {
            m.Wr(Buf, 0x30); m.Wr(Buf + 1, 0x36);
            m.Wr(Buf + 2, m.Rd(0x0711) != 0 ? 0x06 : 0x02);
            m.Wr(Buf + 3, m.Rd(0x0714) != 0 ? 0x0E : 0x0A);
            m.Wr(Buf + 4, 0x0B);
            m.Wr(Buf + 5, 0); m.Wr(Buf + 6, 0); m.Wr(Buf + 7, 0);
        }
        m.Wr(0x20, 0);
        ObjSlot1FDraw(m);
    }

    // js: game/states/pause.js statusCondText
    /// <summary>15:BE11: 11-char condition text -> $601C</summary>
    private static void StatusCondText(Machine m)
    {
        int y = ((m.Rd(0x0710) & 0x0F) << 4) & 0xFF;
        for (int x = 0; x < 11; x++) m.Wr(Buf + 0x1C + x, Prg.CpuRd(m, 0xBE2A + y + x));
    }

    // js: game/states/pause.js equipNameRow
    /// <summary>EquipNameRow 15:BE7A: 16-char name of equipment class $18 (EquipNameBase 15:BEB7 in bank 6, + id * $100)
    /// -> $6010; id 0 -> the blank row at $80F0</summary>
    private static void EquipNameRow(Machine m)
    {
        Prg.SetR6(m, 6);
        int c = m.Rd(0x18);
        m.Wr(0x10, Prg.CpuRd(m, 0xBEB7 + c * 2));
        m.Wr(0x11, Prg.CpuRd(m, 0xBEB8 + c * 2));
        int a = m.Rd(0x0711 + c);
        if (a == 0) { m.Wr(0x10, 0xF0); m.Wr(0x11, 0x80); }
        m.Wr(0x11, (m.Rd(0x11) + a) & 0xFF);
        for (int x = 0; x < 16; x++) m.Wr(Buf + 0x10 + x, Prg.IndY(m, 0x10, x));
        Prg.SetR6(m, 0x1A);
    }

    // js: game/states/pause.js pauseScreen
    /// <summary>PauseScreen 15:BC40 + PauseWait 15:BDDB; res.V = ReadPads' Y</summary>
    public static IEnumerable<Y> PauseScreen(Machine m, Ret<int>? res = null)
    {
        int savedIrq = m.Rd(R.IrqMode);                          // PHA
        foreach (var s in WaitPpuQueue(m)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;
        int im = m.Rd(R.IrqMode) | 0x80;                         // b7: NMI skips PaletteUpload
        m.Wr(R.IrqMode, im);
        m.Wr(R.FrameReady, im);
        m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) & 0xEF);    // sprites off
        m.Wr(0x04AC, 0);
        for (int x = 0; x < 0x100; x++) m.Wr(Buf + x, 0x20);
        for (int x = 0xE0; x >= 0; x -= 0x20) { m.Wr(Buf + 1 + x, 0x1E); m.Wr(Buf + 0x1F + x, 0x1F); }
        foreach (var s in PpuQueueBlock(m, 5)) yield return s;
        foreach (var s in PpuQueueBlock(m, 6)) yield return s;
        for (int x = 0x1F; x >= 0; x--)
        {
            m.Wr(Buf + x, 0xFF); m.Wr(Buf + 0x20 + x, 0xFF); m.Wr(Buf + 0x40 + x, 0x19); m.Wr(Buf + 0xA0 + x, 0x1C);
        }
        m.Wr(Buf + 0x41, 0x18); m.Wr(Buf + 0x5F, 0x1A); m.Wr(Buf + 0xA1, 0x1B); m.Wr(Buf + 0xBF, 0x1D);
        foreach (var s in PpuQueueBlock(m, 7)) yield return s;
        foreach (var s in PpuQueueBlock(m, 8)) yield return s;
        for (int x = 0x3F; x >= 0; x--) m.Wr(Buf + 0x60 + x, 0xFF);
        foreach (var s in PpuQueueBlock(m, 3)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;
        foreach (var s in StatusPlayerSprite(m)) yield return s;
        m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) | 0x10);
        m.Wr(R.IrqMode, 2);
        m.Wr(0x58, 0x3C); m.Wr(0x59, 0x38);
        m.Wr(0x5A, m.Rd(0x07F2)); m.Wr(0x5B, m.Rd(0x07F3));
        m.Wr(R.PpuctrlShadow, (m.Rd(R.PpuctrlShadow) & 0xFC) | 2);
        for (int x = 0; x < 0x100; x++) m.Wr(Buf + x, Prg.CpuRd(m, 0xBD84 + x));   // status window text
        StatusCondText(m);
        foreach (var s in PpuQueueBlock(m, 0x0A)) yield return s;
        for (int x = 5; x >= 0; x--) { m.Wr(Buf + x, m.Rd(0x6410 + x)); m.Wr(Buf + 6 + x, m.Rd(0x6418 + x)); }
        foreach (var s in PpuQueueBlock(m, 9)) yield return s;
        for (int b = 0x0F; b < 0x14; b++) foreach (var s in PpuQueueBlock(m, b)) yield return s;
        Prg.SetR6(m, 0x1A);
        foreach (int e in new[] { 6, 7, 8, 9, 0x0A, 0x10 })
            foreach (var s in Death.HudRedraw(m, e, 0x14)) yield return s;   // X = $14 from the block loop
        m.Wr(0x17, 0x0B); m.Wr(0x18, 0); m.Wr(0x19, 3);
        do
        {                                                        // 4 equipment rows: icon + name, blocks $0B-$0E
            int y = (m.Rd(0x18) << 2) + 3;
            for (int x = 3; x >= 0; x--, y--) m.Wr(Buf + 0x0C + x, Prg.CpuRd(m, 0xBDCB + y));
            EquipNameRow(m);
            foreach (var s in PpuQueueBlock(m, m.Rd(0x17))) yield return s;
            m.Wr(0x17, m.Rd(0x17) + 1); m.Wr(0x18, m.Rd(0x18) + 1);
            m.Wr(0x19, (m.Rd(0x19) - 1) & 0xFF);
        } while ((m.Rd(0x19) & 0x80) == 0);
        m.Wr(R.IrqMode, 2);
        m.Wr(R.PpumaskShadow, m.Rd(R.PpumaskShadow) | 0x18);
        m.Wr(R.SubState, (m.Rd(R.SubState) + 1) & 0xFF);        // 15:BD7F INC $41 (8 -> 9 from play)
        foreach (var s in PauseWait(m, savedIrq, res)) yield return s;   // falls into PauseWait 15:BDDB
    }

    // js: game/states/pause.js pauseWait
    /// <summary>PauseWait 15:BDDB (the tail of PauseScreen). Ends with PLA -> irq_mode (savedIrq = the pushed byte).</summary>
    public static IEnumerable<Y> PauseWait(Machine m, int savedIrq, Ret<int>? res = null)
    {
        for (; ; )
        {
            foreach (var s in Death.WaitFrame(m)) yield return s;
            foreach (var s in StatusPlayerSprite(m)) yield return s;
            Pads.ReadPadsMenu(m);
            if ((m.Rd(R.PadNew) & 0x10) != 0) break;             // START
        }
        for (int x = 0x3F; x >= 0; x--) m.Wr(Buf + 0x80 + x, 0xFF);
        foreach (var s in PpuQueueBlock(m, 0x14)) yield return s;
        foreach (var s in WaitPpuQueue(m)) yield return s;
        foreach (var s in Death.StatRecalc(m, 0xFF)) yield return s;   // X = $FF from the fill loop
        m.Wr(0x04BF, 0);
        m.Wr(R.SubState, 8);
        m.Wr(R.IrqMode, savedIrq);                               // PLA
        int y = Pads.ReadPads(m);                                // JMP ReadPads (returns its Y register)
        if (res != null) res.V = y;
    }

    // js: game/states/pause.js pauseWaitEntry
    /// <summary>SubstateHandlerTable entry 13 = PauseWait entered from the dispatcher (s142): unreachable in the ROM;
    /// the port keeps irq_mode instead of crashing.</summary>
    public static IEnumerable<Y> PauseWaitEntry(Machine m, Ret<int>? res = null)
    {
        m.Log("sub_state 13 (PauseWait 15:BDDB) dispatched: unreachable in the ROM (its PLA eats the return address)");
        foreach (var s in PauseWait(m, m.Rd(R.IrqMode), res)) yield return s;
    }
}
