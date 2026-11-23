// re-c C# port — NPC talk + the gameplay message window (= web/src/game/states/talk.js, s115). REVERSE.md "NPC talk
// (30:D0C8)", "NPC script format", "Talk actions", "Who picks the message" and the bank-20 text printer table.
//
//   NpcTalk(m, y)          NpcTalk 30:D0C8 (sub_state $11): PlaySound($29) -> talker test -> NpcScript 14:8000 ->
//                          ShowMessagePacked -> TalkActionTable 30:D123[$22] (after the window closes)
//   NpcScript(m)           NpcScript 14:8000 (+ NpcResult 14:8078, NpcFlagApply 14:8112, NpcFlagRef 14:8148)
//   ShowMessage(m)         ShowMessage 30:D354 (iterator): font CHR, TextWindowOpen 20:8500, irq_mode 1/9, sprite
//                          pass + OamClipTop, TextPrintBox 20:8528, then wait for SELECT (replay) or A/B (close)
//   ShowMessagePacked(m)   30:D347: $21:$20 linear id -> $07DF/$07DE (TextIdUnpack 30:D3C2), then ShowMessage
//
// Every spin on the PPU queue ($0A != $0B) is a `yield return Y.Spin` (frame_ready is not cleared, so the NMI that
// comes meanwhile skips its OAM DMA / scroll copy exactly as on hardware). The ROM's `LSR frame_ready / BCC` wait
// (30:D38A, 20:8641) is LsrWait(). The Y register of the printer is kept 8-bit with the ROM's INC $2B / $2D carries.
// TalkActions also holds the entries giveitem.js adds to the JS TALK_ACTIONS object (3, $10, $11, $14, $19, $1A).
using ReC.Game.Obj;
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class Talk
{
    private const int Buf = 0x6000, Q = 0x6200;
    private const int Msg = 0x07DE, Group = 0x07DF, CurItem = 0x07DC, Name = 0x6400, NpcState = 0x6580, Flags = 0x6480;
    private const int TalkSlot = 0x0623, Freeze = 0x05A0, SpawnB2 = 0x0540, SpawnP = 0x0560, Active = 0x04A0;

    // js: game/states/talk.js unported
    private static void Unported(Machine m, string what)
    {
        if (m.Unported.Add(what)) m.Log($"{what} not ported yet");
    }

    // js: game/states/talk.js inc
    private static void Inc(Machine m, int a) => m.Wr(a, (m.Rd(a) + 1) & 0xFF);

    // js: game/states/talk.js dec
    private static int Dec(Machine m, int a) { int v = (m.Rd(a) - 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/states/talk.js oamClipTop
    /// <summary>OamClipTop 30:C17D: hide sprites 1-63 with y &lt;= $58 - ($04 &amp; 7)</summary>
    public static void OamClipTop(Machine m)
    {
        m.Wr(0x10, m.Rd(R.CamYLo) & 7);
        Death.OamClip(m, (0x58 - m.Rd(0x10)) & 0xFF);
    }

    // js: game/states/talk.js qWait
    /// <summary>spin until the PPU queue is empty ($850F / $85E9 / $87D0 / $87EF / $8722 loops)</summary>
    private static IEnumerable<Y> QWait(Machine m) { while (!PpuQueue.QueueEmpty(m)) yield return Y.Spin; }

    // js: game/states/talk.js lsrWait
    /// <summary>30:D38A / 20:8641: LSR frame_ready / BCC (wait for the NMI without clearing frame_ready first)</summary>
    public static IEnumerable<Y> LsrWait(Machine m)
    {
        for (; ; )
        {
            int v = m.Rd(R.FrameReady);
            m.Wr(R.FrameReady, v >> 1);
            if ((v & 1) != 0) yield break;
            if ((v >> 1) == 0) yield return Y.Spin;
        }
    }

    // ---------------------------------------------------------------- bank 20 text printer

    // js: game/states/talk.js textQueueUpload
    /// <summary>TextQueueUpload 20:8836: PPUCTRL NMI off, record [$21 $20 $24 $25] at $0B, NMI on</summary>
    private static void TextQueueUpload(Machine m)
    {
        PpuQueue.PpuCtrlNmiOff(m);
        PpuQueue.QueuePush(m, m.Rd(0x21), m.Rd(0x20), m.Rd(0x24), m.Rd(0x25));
        PpuQueue.PpuCtrlNmiOn(m);
    }

    // js: game/states/talk.js textFrameRows
    /// <summary>TextFrameRows 20:87EF: 2 rows of 32 tiles from the 3-byte templates at 20:8899+x -> $6000, queued; $20/$21 += $22/$23</summary>
    private static IEnumerable<Y> TextFrameRows(Machine m, int x)
    {
        foreach (var s in QWait(m)) yield return s;
        int y = 0;
        m.Wr(0x2F, 2);
        do
        {
            m.Wr(Buf + y, 0xFE); y++;
            m.Wr(Buf + y, Prg.CpuRd(m, 0x8899 + x)); x++; y++;
            m.Wr(0x2E, 0x1D);
            do { m.Wr(Buf + y, Prg.CpuRd(m, 0x8899 + x)); y++; } while (Dec(m, 0x2E) != 0);
            x++;
            m.Wr(Buf + y, Prg.CpuRd(m, 0x8899 + x)); x++; y++;
        } while (Dec(m, 0x2F) != 0);
        TextQueueUpload(m);
        int lo = m.Rd(0x22) + m.Rd(0x20);
        m.Wr(0x20, lo & 0xFF);
        m.Wr(0x21, (m.Rd(0x23) + m.Rd(0x21) + (lo >> 8)) & 0xFF);
    }

    private static readonly int[] FrameRowTemplates = [0, 3, 3, 3, 6];

    // js: game/states/talk.js textDrawFrame
    /// <summary>TextDrawFrame 20:87B7: NT2 rows 0-9 (5 x TextFrameRows), then attributes $2BC0 = $FF x $40 from $6001</summary>
    private static IEnumerable<Y> TextDrawFrame(Machine m)
    {
        foreach (int x in FrameRowTemplates) foreach (var s in TextFrameRows(m, x)) yield return s;
        foreach (var s in QWait(m)) yield return s;
        for (int x = 0x41; x > 0; x--) m.Wr(Buf + x, 0xFF);
        m.Wr(0x21, 0x2B); m.Wr(0x20, 0xC0); m.Wr(0x25, 1);
        TextQueueUpload(m);
    }

    // js: game/states/talk.js qWaitClear
    /// <summary>20:850F: wait for the queue, then the ring bytes $6200-$621F = $FF</summary>
    private static IEnumerable<Y> QWaitClear(Machine m)
    {
        foreach (var s in QWait(m)) yield return s;
        for (int x = 0x1F; x >= 0; x--) m.Wr(Q + x, 0xFF);
    }

    // js: game/states/talk.js textWindowOpen
    /// <summary>TextWindowOpen 20:8500</summary>
    public static IEnumerable<Y> TextWindowOpen(Machine m)
    {
        for (int x = 0; x < 0x10; x++) m.Wr(0x20 + x, Prg.CpuRd(m, 0x8879 + x));
        foreach (var s in TextDrawFrame(m)) yield return s;
        foreach (var s in QWaitClear(m)) yield return s;
    }

    // js: game/states/talk.js textPrintBox
    /// <summary>TextPrintBox 20:8528</summary>
    public static IEnumerable<Y> TextPrintBox(Machine m)
    {
        for (int x = 0; x < 0x10; x++) m.Wr(0x20 + x, Prg.CpuRd(m, 0x8889 + x));
        foreach (var s in TextPrint(m)) yield return s;
        foreach (var s in QWaitClear(m)) yield return s;  // JMP $850F
    }

    // js: game/states/talk.js textChar
    /// <summary>TextChar 20:87AE (the caller does the INY)</summary>
    private static void TextChar(Machine m, int a)
    {
        int x = m.Rd(0x28);
        m.Wr(Buf + x, a);
        m.Wr(0x28, (x + 1) & 0xFF);
    }

    // js: game/states/talk.js iny
    /// <summary>INY + INC zp when Y wraps</summary>
    private static int Iny(Machine m, int y, int zp)
    {
        y = (y + 1) & 0xFF;
        if (y == 0) Inc(m, zp);
        return y;
    }

    private static readonly (int Lo, int Src)[] FlushRows = [(0x20, 0), (0x60, 0x40), (0xA0, 0x80), (0xE0, 0xC0)];

    // js: game/states/talk.js textFlush
    /// <summary>TextFlush 20:85B8: 4 x $40 bytes of $6000 -> $2820 / $2860 / $28A0 / $28E0, each after the queue drains</summary>
    private static IEnumerable<Y> TextFlush(Machine m)
    {
        m.Wr(0x21, 0x28); m.Wr(0x20, 0x20); m.Wr(0x25, 0); m.Wr(0x24, 0x40);
        foreach (var (lo, src) in FlushRows)
        {
            m.Wr(0x20, lo); m.Wr(0x25, src);
            foreach (var s in QWait(m)) yield return s;
            TextQueueUpload(m);
        }
    }

    // js: game/states/talk.js textClearBuf
    /// <summary>TextClearBuf 20:8720: 8 rows of FE 1E 29x20 1F, $28 = $20 (keeps Y via $29); res.V = Y</summary>
    private static IEnumerable<Y> TextClearBuf(Machine m, int y, Ret<int> res)
    {
        m.Wr(0x29, y);
        foreach (var s in QWait(m)) yield return s;
        int x = 0;
        do
        {
            m.Wr(Buf + x, 0xFE); x++;
            m.Wr(Buf + x, 0x1E); x++;
            m.Wr(0x27, 0x1D);
            do { m.Wr(Buf + x, 0x20); x++; } while (Dec(m, 0x27) != 0);
            m.Wr(Buf + x, 0x1F); x = (x + 1) & 0xFF;
        } while (x != 0);
        m.Wr(0x28, 0x20);
        res.V = m.Rd(0x29);
    }

    // js: game/states/talk.js textPrintStr
    /// <summary>TextPrintStr 20:8693: ($2A) zero-terminated, then the auto-space peek at ($2C),$29+1 and resume at ($2C)</summary>
    private static int TextPrintStr(Machine m)
    {
        int y = 0;
        for (; ; )
        {
            int c = Prg.IndY(m, 0x2A, y);
            if (c == 0) break;
            TextChar(m, c); y = (y + 1) & 0xFF;
        }
        y = (m.Rd(0x29) + 1) & 0xFF;                      // 869F
        int a = Prg.IndY(m, 0x2C, y);
        if ((a >= 4 && a < 0x0A) || (a >= 0x40 && a != 0x5F)) TextChar(m, 0x20);
        y = Iny(m, m.Rd(0x29), 0x2D);
        m.Wr(0x2A, m.Rd(0x2C)); m.Wr(0x2B, m.Rd(0x2D));
        return y;
    }

    // js: game/states/talk.js textTableStr
    /// <summary>20:8710 + string table lookup (codes 05 / 06 / 07: table 20:8A00 / 8A14 / 8A5C)</summary>
    private static int TextTableStr(Machine m, int y, int table)
    {
        m.Wr(0x2C, m.Rd(0x2A)); m.Wr(0x2D, m.Rd(0x2B));
        y = Iny(m, y, 0x2D);
        m.Wr(0x29, y);
        int x = (Prg.IndY(m, 0x2A, y) << 1) & 0xFF;
        m.Wr(0x2A, Prg.CpuRd(m, table + x)); m.Wr(0x2B, Prg.CpuRd(m, table + x + 1));
        return TextPrintStr(m);
    }

    // js: game/states/talk.js textWord
    /// <summary>TextWord 20:86F5: dictionary word (byte &amp; $7F) from TextDictPtrs 20:8900</summary>
    private static int TextWord(Machine m, int y)
    {
        m.Wr(0x2C, m.Rd(0x2A)); m.Wr(0x2D, m.Rd(0x2B));
        m.Wr(0x29, y);
        int x = (Prg.IndY(m, 0x2A, y) << 1) & 0xFF;
        m.Wr(0x2A, Prg.CpuRd(m, 0x8900 + x)); m.Wr(0x2B, Prg.CpuRd(m, 0x8901 + x));
        return TextPrintStr(m);
    }

    // js: game/states/talk.js textCurItem
    /// <summary>TextCurItem 20:8772 (code 08): item name 20:8A5C[$07DC] ($FF -> $49), no auto-space</summary>
    private static int TextCurItem(Machine m, int y)
    {
        m.Wr(0x2C, m.Rd(0x2A)); m.Wr(0x2D, m.Rd(0x2B));
        m.Wr(0x29, y);
        int a = m.Rd(CurItem);
        if (a == 0xFF) a = 0x49;
        int x = (a << 1) & 0xFF;
        m.Wr(0x2A, Prg.CpuRd(m, 0x8A5C + x)); m.Wr(0x2B, Prg.CpuRd(m, 0x8A5D + x));
        for (int i = 0; ; i = (i + 1) & 0xFF)
        {
            int c = Prg.IndY(m, 0x2A, i);
            if (c == 0) break;
            TextChar(m, c);
        }
        y = Iny(m, m.Rd(0x29), 0x2D);
        m.Wr(0x2A, m.Rd(0x2C)); m.Wr(0x2B, m.Rd(0x2D));
        return y;
    }

    // js: game/states/talk.js textPageWait
    /// <summary>TextPageWait 20:8619 (code 03): flush, prompt tile $40 at $2910, wait for A/B, clear; res.V = Y</summary>
    private static IEnumerable<Y> TextPageWait(Machine m, int y, Ret<int> res)
    {
        m.Wr(0x29, y);
        foreach (var s in TextFlush(m)) yield return s;
        foreach (var s in QWait(m)) yield return s;
        m.Wr(0x21, 0x29); m.Wr(0x20, 0x10); m.Wr(0x24, 1); m.Wr(0x25, 0);
        m.Wr(Buf, 0x40);
        TextQueueUpload(m);
        m.Wr(0x24, 0x40);
        for (; ; )
        {
            foreach (var s in LsrWait(m)) yield return s;
            Pads.ReadPads(m);
            Prg.SetR7(m, Prg.CpuRd(m, 0x83FE + m.Rd(Group)));   // TextSetR7(TextGroupBank[group])
            if ((m.Rd(R.PadNew) & 0xC0) != 0) break;
        }
        y = Iny(m, m.Rd(0x29), 0x2B);
        foreach (var s in TextClearBuf(m, y, res)) yield return s;
    }

    // js: game/states/talk.js textPrint
    /// <summary>TextPrint 20:853A: message $07DE &amp; $1F of group $07DF</summary>
    private static IEnumerable<Y> TextPrint(Machine m)
    {
        int g = m.Rd(Group);
        m.Wr(0x6700, g);
        Prg.SetR7(m, Prg.CpuRd(m, 0x83FE + g));          // TextSetR7(TextGroupBank[g])
        int gx = (g << 1) & 0xFF;
        m.Wr(0x2C, Prg.CpuRd(m, 0x8422 + gx)); m.Wr(0x2D, Prg.CpuRd(m, 0x8423 + gx));
        int id = m.Rd(Msg);
        m.Wr(0x6701, id);
        int y = (id & 0x1F) << 1;
        m.Wr(0x2A, Prg.IndY(m, 0x2C, y)); y++;
        m.Wr(0x2B, Prg.IndY(m, 0x2C, y));
        y = 0;
        var ry = new Ret<int>();
        for (; ; )
        {
            int a = Prg.IndY(m, 0x2A, y);
            if (a < 0x0A)
            {                                             // TextCtrlDispatch 20:8593: $2C/$2D = handler address
                m.Wr(0x2C, Prg.CpuRd(m, 0x85A2 + a * 2)); m.Wr(0x2D, Prg.CpuRd(m, 0x85A3 + a * 2));
                switch (a)
                {
                    case 0:                               // 85B6 PLA PLA -> TextFlush, RTS out of TextPrint
                        foreach (var s in TextFlush(m)) yield return s;
                        yield break;
                    case 1:                               // 85F2 clear box
                        foreach (var s in TextClearBuf(m, y, ry)) yield return s;
                        y = ry.V;
                        m.Wr(0x29, 0x20);                 // STA $29 (A = $20 from TextClearBuf)
                        foreach (var s in QWait(m)) yield return s;
                        m.Wr(0x28, 0x23);
                        y = Iny(m, y, 0x2B);
                        break;
                    case 2:                               // 8607 newline
                        m.Wr(0x28, (((m.Rd(0x28) + 0x40) & 0xE0) + 4) & 0xFF);
                        y = Iny(m, y, 0x2B);
                        break;
                    case 3:
                        foreach (var s in TextPageWait(m, y, ry)) yield return s;
                        y = ry.V;
                        break;
                    case 4:
                    {                                     // 866B player name
                        m.Wr(0x29, y);
                        for (int i = 0; ; i = (i + 1) & 0xFF)
                        {
                            int c = m.Rd(Name + i);
                            if (c == 0) break;
                            TextChar(m, c);
                        }
                        y = Iny(m, m.Rd(0x29), 0x2B);
                        break;
                    }
                    case 5: y = TextTableStr(m, y, 0x8A00); break;
                    case 6: y = TextTableStr(m, y, 0x8A14); break;
                    case 7: y = TextTableStr(m, y, 0x8A5C); break;
                    case 8: y = TextCurItem(m, y); break;
                    case 9:
                    {                                     // 8753 n spaces (n = 0 -> 256)
                        m.Wr(0x29, y);
                        int n = Prg.IndY(m, 0x2A, (y + 1) & 0xFF);
                        do { TextChar(m, 0x20); n = (n - 1) & 0xFF; } while (n != 0);
                        y = Iny(m, m.Rd(0x29), 0x2B);
                        y = Iny(m, y, 0x2B);
                        break;
                    }
                }
            }
            else if ((a & 0x80) != 0)
            {
                y = TextWord(m, y);
            }
            else
            {
                TextChar(m, a);
                y = (y + 1) & 0xFF;                       // TextChar's INY (no $2B carry)
            }
        }
    }

    // ---------------------------------------------------------------- ShowMessage

    // js: game/states/talk.js blinkWindow
    /// <summary>30:D896 (sub_state 9 only): $10 x (irq_mode 0, frame, irq_mode 1, frame)</summary>
    private static IEnumerable<Y> BlinkWindow(Machine m)
    {
        m.Wr(0x20, 0x10);
        do
        {
            m.Wr(R.IrqMode, 0);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            m.Wr(R.IrqMode, 1);
            foreach (var s in Death.WaitFrame(m)) yield return s;
        } while (Dec(m, 0x20) != 0);
    }

    // js: game/states/talk.js showMessage
    /// <summary>ShowMessage 30:D354 ($07DE / $07DF set)</summary>
    public static IEnumerable<Y> ShowMessage(Machine m)
    {
        for (; ; )
        {
            m.Wr(R.ChrR0Alt, 0x3C);
            m.Wr(R.ChrR0Alt + 1, 0x38);
            Prg.SetR6(m, 0x14);
            foreach (var s in TextWindowOpen(m)) yield return s;
            foreach (var s in Death.WaitFrame(m)) yield return s;
            m.Wr(R.IrqMode, m.Rd(R.IrqMode) == 7 ? 9 : 1);
            AreaEnter.FarSpriteDraw(m);
            OamClipTop(m);
            if (m.Rd(R.SubState) == 9) foreach (var s in BlinkWindow(m)) yield return s;
            Prg.SetR6(m, 0x14);
            foreach (var s in TextPrintBox(m)) yield return s;
            bool replay = false;
            for (; ; )
            {
                foreach (var s in LsrWait(m)) yield return s;
                Pads.ReadPads(m);
                AreaEnter.FarSpriteDraw(m);
                OamClipTop(m);
                if ((m.Rd(R.PadNew) & 0x20) != 0) { replay = true; break; }   // SELECT: JMP ShowMessage
                if ((m.Rd(R.PadNew) & 0xC0) != 0) break;
            }
            if (replay) continue;
            m.Wr(0x20, 8);
            do
            {
                foreach (var s in Death.WaitFrame(m)) yield return s;
                AreaEnter.FarSpriteDraw(m);
                OamClipTop(m);
            } while (Dec(m, 0x20) != 0);
            m.Wr(R.IrqMode, 0);
            m.Wr(R.PadNew, 0);
            m.Wr(R.SubState, 8);
            yield break;
        }
    }

    // js: game/states/talk.js textIdUnpack
    /// <summary>TextIdUnpack 30:D3C2: $07DF = $07DF*8 + ($07DE >> 5), $07DE &amp;= $1F</summary>
    private static void TextIdUnpack(Machine m)
    {
        int id = m.Rd(Msg);
        m.Wr(Group, ((m.Rd(Group) << 3) | (id >> 5)) & 0xFF);
        m.Wr(Msg, id & 0x1F);
    }

    // js: game/states/talk.js showMessagePacked
    /// <summary>ShowMessagePacked 30:D347</summary>
    public static IEnumerable<Y> ShowMessagePacked(Machine m)
    {
        m.Wr(Msg, m.Rd(0x20));
        m.Wr(Group, m.Rd(0x21));
        TextIdUnpack(m);
        foreach (var s in ShowMessage(m)) yield return s;
    }

    // ---------------------------------------------------------------- NpcScript (bank 14)

    // js: game/states/talk.js flagRef
    /// <summary>NpcFlagRef 14:8148 on ($24),Y: $26 = b0, $28 = mask, $27 = b0 >> 3; returns (byte index X, Y + 2)</summary>
    private static (int X, int Y) FlagRef(Machine m, int y)
    {
        int r0 = Prg.IndY(m, 0x24, y);
        m.Wr(0x26, r0);
        int r1 = Prg.IndY(m, 0x24, (y + 1) & 0xFF);
        m.Wr(0x28, Prg.CpuRd(m, 0x8167 + (r1 & 7)));
        m.Wr(0x27, r0 >> 3);
        return (((r1 >> 3) | ((r0 & 7) << 5)) & 0xFF, (y + 2) & 0xFF);
    }

    // js: game/states/talk.js flagTest
    /// <summary>NpcFlagTest 14:8135 -> (cond true, Y)</summary>
    public static (bool Ok, int Y) FlagTest(Machine m, int y)
    {
        var (x, y2) = FlagRef(m, y);
        int v = m.Rd(Flags + x);
        if ((m.Rd(0x26) & 0x20) != 0) v ^= 0xFF;
        return ((v & m.Rd(0x28)) != 0, y2);
    }

    // js: game/states/talk.js flagApply
    /// <summary>NpcFlagApply 14:8112: b7 = clear, else set; stops after the item with b6</summary>
    public static int FlagApply(Machine m, int y)
    {
        do
        {
            var (x, y2) = FlagRef(m, y);
            y = y2;
            int mask = m.Rd(0x28);
            m.Wr(Flags + x, (m.Rd(0x26) & 0x80) != 0 ? m.Rd(Flags + x) & ~mask & 0xFF : m.Rd(Flags + x) | mask);
        } while ((m.Rd(0x26) & 0x40) == 0);
        return y;
    }

    // js: game/states/talk.js npcResult
    /// <summary>NpcResult 14:8078: $22 = r0 >> 3, $21 = r0 &amp; 7, $20 = r1</summary>
    public static int NpcResult(Machine m, int y)
    {
        int r0 = Prg.IndY(m, 0x24, y);
        m.Wr(0x22, r0 >> 3);
        m.Wr(0x21, r0 & 7);
        m.Wr(0x20, Prg.IndY(m, 0x24, (y + 1) & 0xFF));
        return (y + 2) & 0xFF;
    }

    // js: game/states/talk.js skipEntry
    /// <summary>14:808E (Y += 3) / 14:808F (Y += 2), then skip a set-list when $26 b6</summary>
    private static int SkipEntry(Machine m, int y, int n)
    {
        y = (y + n) & 0xFF;
        if ((m.Rd(0x26) & 0x40) != 0)
        {
            int a;
            do { a = Prg.IndY(m, 0x24, y); y = (y + 2) & 0xFF; } while ((a & 0x40) == 0);
        }
        return y;
    }

    // js: game/states/talk.js npcScript
    /// <summary>NpcScript 14:8000 ($23 = NPC param; banks 14/15 mapped)</summary>
    public static void NpcScript(Machine m)
    {
        int p = m.Rd(0x23), x = (p << 1) & 0xFF, t = (p & 0x80) != 0 ? 0x8A5D : 0x895D;
        m.Wr(0x24, Prg.CpuRd(m, t + x)); m.Wr(0x25, Prg.CpuRd(m, t + x + 1));
        int y = 0;
        bool ok;
        do
        {                                                 // section A: stateless overrides
            (ok, y) = FlagTest(m, y);
            if (ok) { NpcResult(m, y); return; }          // JMP NpcResult (set-list not applied)
            y = SkipEntry(m, y, 2);
        } while ((m.Rd(0x26) & 0x80) == 0);
        for (; ; )
        {                                                 // area list: (area, state) pairs until $FF
            int a = Prg.IndY(m, 0x24, y); y = (y + 1) & 0xFF;
            if (a == 0xFF) break;
            if (a == m.Rd(R.AreaId)) m.Wr(NpcState + p, Prg.IndY(m, 0x24, y));
            y = (y + 1) & 0xFF;
        }
        int @base = m.Rd(0x24) + y;                       // section B base = the byte after the $FF
        m.Wr(0x24, @base & 0xFF);
        m.Wr(0x25, (m.Rd(0x25) + (@base >> 8)) & 0xFF);
        y = m.Rd(NpcState + p);
        do
        {
            (ok, y) = FlagTest(m, y);
            if (ok)
            {
                y = NpcResult(m, y);
                m.Wr(NpcState + p, Prg.IndY(m, 0x24, y));
                if ((m.Rd(0x26) & 0x40) != 0) FlagApply(m, (y + 1) & 0xFF);
                return;
            }
            y = SkipEntry(m, y, 3);
        } while ((m.Rd(0x26) & 0x80) == 0);
        NpcResult(m, y);                                  // 8076 falls into NpcResult (bytes after the chain)
    }

    // ---------------------------------------------------------------- NpcTalk + TalkActionTable

    // js: game/states/talk.js npcVanish
    /// <summary>NpcVanish 30:D31C (Y = $0623) / D31F (Y given)</summary>
    public static void NpcVanish(Machine m) => NpcVanish(m, m.Rd(TalkSlot));

    /// <summary>NpcVanish 30:D31F (Y given)</summary>
    public static void NpcVanish(Machine m, int y)
    {
        m.Wr(0x0660 + y, 1);
        m.Wr(Active + y, 0x53);
        m.Wr(Freeze + y, 0);
        m.Wr(0x03A0 + y, 0);
        PlaySound.Play(m, 0x6D, y);
    }

    // js: game/states/talk.js makeFollower13
    /// <summary>MakeFollower13 30:D23F (talk acts 6 / 13, event acts 2 / 28): slot 13 becomes type $32 (follower)</summary>
    public static void MakeFollower13(Machine m)
    {
        m.Wr(Active + 0x0D, 0x32);
        m.Wr(0x03AD, 0); m.Wr(0x062D, 0); m.Wr(0x064D, 0);
        m.Wr(0x04ED, 1);
        m.Wr(0x060D, 0xFF);
    }

    // js: game/states/talk.js spawnFxAtSlot
    /// <summary>SpawnFxAtSlot 30:D1FF (s202): type $DF in slot $1E at slot x's position, $033E = $80, JMP AreaLoadB.
    /// ObjInit pushes/pulls X, so the STA obj_tile_ofs,X hits slot $1E.</summary>
    public static void SpawnFxAtSlot(Machine m, int x)
    {
        Prg.SetR6(m, 0x1A);
        ObjUtil.ObjPosToTemp(m, x);
        m.Wr(0x10, 0x1E);
        ObjUtil.TempToObjPos(m, 0x1E);
        m.Wr(0x11, 0xDF);
        ObjInit.Init(m);
        m.Wr(0x033E, 0x80);
        Area.AreaLoad(m, 0);                              // AreaLoadB 31:E148
    }

    // js: game/states/talk.js nop
    private static IEnumerable<Y> Nop(Machine m, int y) { yield break; }

    // talk act bodies (anonymous generators of the JS TALK_ACTIONS object, no inventory entries)
    private static IEnumerable<Y> Act01(Machine m, int y)
    {                                                     // D19C type $64 into the slot pair s, s+1
        m.Wr(0x10, (((m.Rd(TalkSlot) - 1) & 0xFE) + 1) & 0xFF);
        m.Wr(0x11, 0x64);
        ObjInit.Init(m);                                  // ObjInit restores $11 = $64
        Inc(m, 0x10);
        ObjInit.Init(m);
        yield break;
    }

    private static IEnumerable<Y> Act08(Machine m, int y)
    {                                                     // D63F re-init slot 14 with $04CE unless active = 8
        if (m.Rd(Active + 0x0E) == 0x08) yield break;
        m.Wr(0x10, 0x0E); m.Wr(0x11, m.Rd(0x04CE));
        ObjInit.Init(m);
    }

    private static IEnumerable<Y> Act09(Machine m, int y)
    {                                                     // D263 gold += 100 (INC low byte on carry: ROM quirk)
        PlaySound.Play(m, 0x3F, y);
        int s = m.Rd(0x0702) + 0x64;
        m.Wr(0x0702, s & 0xFF);
        if (s > 0xFF) Inc(m, 0x0702);
        Prg.SetR6(m, 0x1A);
        foreach (var q in Death.HudRedraw(m, 1)) yield return q;
    }

    /// <summary>talk act $17 D16D TalkFullRestore (also the tail of act $14, states/giveitem.js)</summary>
    public static IEnumerable<Y> Act17(Machine m, int y)
    {
        m.Wr(0x07D7, 0);
        m.Wr(0x03C1, m.Rd(0x03C0));
        m.Wr(0x0708, m.Rd(0x0709));
        Prg.SetR6(m, 0x1A);
        foreach (var s in Death.HudHpBar(m)) yield return s;
        foreach (var s in Death.HudRedraw(m, 4)) yield return s;
        NpcVanish(m);
    }

    private static IEnumerable<Y> Act0A(Machine m, int y)
    {                                                     // D2F4 TalkSpawnShot0F (s201): type $0F in $1E at the NPC
        Prg.SetR6(m, 0x1A);
        ObjUtil.ObjPosToTemp(m, m.Rd(TalkSlot));
        m.Wr(0x10, 0x1E); m.Wr(0x11, 0x0F);
        ObjUtil.TempToObjPos(m, 0x1E);
        ObjInit.FarObjInit(m);
        m.Wr(0x057E, 0x39); m.Wr(0x055E, 2); m.Wr(0x061E, 3);
        NpcVanish(m);                                     // falls into D31C
        yield break;
    }

    private static IEnumerable<Y> Act15(Machine m, int y)
    {                                                     // D2D3 TalkTwoFollowers (s201), skipped if slot 14 is $32
        if (m.Rd(Active + 0x0E) == 0x32) yield break;
        m.Wr(0x068D, 0x0E); m.Wr(0x068E, 0x0F);
        m.Wr(Active + 0x0D, 0x32); m.Wr(Active + 0x0E, 0x32);
        m.Wr(0x04ED, 1); m.Wr(0x04EE, 1);
    }

    private static IEnumerable<Y> Act18(Machine m, int y)
    {                                                     // D280 TalkWarp (s201): player slots 0/1 to slot $1E
        m.Wr(0x0620, 0);                                  // obj_timer2 slot 0
        Prg.SetR6(m, 0x1A);
        ObjUtil.ObjPosToTemp(m, 0x1E);
        ObjUtil.TempToObjPos(m, 0); ObjUtil.TempToObjPos(m, 1);
        m.Wr(0x0710, m.Rd(0x0710) | 0x40);                // obj1_flash b6 = warp exit
        m.Wr(0x0561, 2); m.Wr(0x0641, 2);
        m.Wr(Active + 0x1E, 0x0C);
        yield break;
    }

    private static IEnumerable<Y> Act13(Machine m, int y)
    {                                                     // D1B5 (s202): fx at the NPC, AreaLoadA, NpcVanish $0623+1
        SpawnFxAtSlot(m, m.Rd(TalkSlot));
        m.Wr(0x033E, 0xA0);
        Area.AreaLoad(m, 1);                              // AreaLoadA 31:E144
        NpcVanish(m, (m.Rd(TalkSlot) + 1) & 0xFF);        // JMP D31F
        yield break;
    }

    private static IEnumerable<Y> Act16(Machine m, int y)
    {                                                     // D1CA (s202): SFX $29, msg 19.0, AreaLoadA, fx at the NPC
        PlaySound.Play(m, 0x29, y);
        m.Wr(Msg, 0); m.Wr(Group, 0x13);
        foreach (var s in ShowMessage(m)) yield return s;
        Area.AreaLoad(m, 1);
        SpawnFxAtSlot(m, m.Rd(TalkSlot));
        m.Wr(0x033E, 0xA0);
        // s207: CPU overrun: AreaLoadA + SpawnFxAtSlot's AreaLoadB run from the message close (f2628 L253) past the
        // next NMI (f2629, frame_ready 0 = normal NMI) to the main-loop top at f2630 L252 (talk22.json, tools.emu)
        yield return Y.Frame;
    }

    private static IEnumerable<Y> Once(Action a) { a(); yield break; }

    /// <summary>(talk.js TALK_ACTIONS, + the giveitem.js entries) TalkActionTable 30:D123 acts -> handler(m, y)</summary>
    public static readonly Dictionary<int, Func<Machine, int, IEnumerable<Y>>> TalkActions = new()
    {
        [0x00] = Nop, [0x04] = Nop, [0x07] = Nop, [0x0B] = Nop, [0x0E] = Nop, [0x0F] = Nop, [0x12] = Nop,
        [0x1C] = Nop, [0x1D] = Nop, [0x1E] = Nop, [0x1F] = Nop,   // D163 RTS
        [0x01] = Act01,
        [0x02] = (m, y) => Once(() => NpcVanish(m)),      // D31C
        [0x06] = (m, y) => Once(() => MakeFollower13(m)), // D23F (s199)
        [0x0D] = (m, y) => Once(() => MakeFollower13(m)), // D23F (s199)
        [0x08] = Act08,
        [0x05] = (m, y) => Once(() => { m.Wr(0x04AE, 0); m.Wr(0x07D7, 0); }),   // D164
        [0x09] = Act09,
        [0x0C] = (m, y) => Once(() => m.Wr(Active + m.Rd(TalkSlot), 0x54)),     // D25A
        [0x17] = Act17,
        [0x1B] = (m, y) => Once(() => m.Wr(0x060D, 6)),   // D341
        [0x0A] = Act0A,
        [0x15] = Act15,
        [0x18] = Act18,
        [0x13] = Act13,
        [0x16] = Act16,
        // giveitem.js (s199 / s201 / s202)
        [0x03] = (m, y) => GiveItem.TalkGiveParam(m, false),
        [0x11] = (m, y) => GiveItem.TalkGiveParam(m, true),
        [0x14] = GiveItem.TalkAct14,
        [0x19] = GiveItem.TalkAct19,
        [0x1A] = GiveItem.TalkAct1A,
        [0x10] = GiveItem.TalkAct10,
    };

    /// <summary>TALK_ACTIONS[act](m, y), or the npcTalk "not ported" log for a missing act (ENTRY_POINTS / NpcTalk)</summary>
    public static IEnumerable<Y>? TalkAction(Machine m, int act, int y)
    {
        if (TalkActions.TryGetValue(act, out var fn)) return fn(m, y);
        Unported(m, $"TalkActionTable {act} (30:{m.Rd(0x10) | m.Rd(0x11) << 8:X})");
        return null;
    }

    // js: game/states/talk.js npcTalk
    /// <summary>NpcTalk 30:D0C8 (sub_state $11); Y = the Y register at the dispatch (PlaySound keeps it in $010F)</summary>
    public static IEnumerable<Y>? NpcTalk(Machine m) => NpcTalk(m, m.Rd(0x010F));

    /// <summary>NpcTalk 30:D0C8 with the dispatch Y given</summary>
    public static IEnumerable<Y> NpcTalk(Machine m, int yIn)
    {
        PlaySound.Play(m, 0x29, yIn);
        int y = m.Rd(TalkSlot);
        bool talker = m.Rd(Freeze + y) == 0;
        if (!talker && m.Rd(SpawnB2 + y) == 1)
        {
            int p = m.Rd(SpawnP + y);
            talker = p == 0x5E || p == 0x5F || p == 0x62 || p == 0x6D || p == 0x6C;
        }
        if (!talker)
        {                                                 // D0F3: group $1F msg 0
            m.Wr(Msg, 0); m.Wr(Group, 0x1F);
            foreach (var s in ShowMessage(m)) yield return s;
            yield break;
        }
        m.Wr(0x23, m.Rd(SpawnP + y));
        Prg.SwitchBank16(m, 7);
        NpcScript(m);
        int act = m.Rd(0x22);                             // PHA
        foreach (var s in ShowMessagePacked(m)) yield return s;
        int a = (act << 1) & 0xFF;                        // PLA / ASL / TAY -> ($10)
        m.Wr(0x10, Prg.CpuRd(m, 0xD123 + a)); m.Wr(0x11, Prg.CpuRd(m, 0xD124 + a));
        var g = TalkAction(m, a >> 1, a);
        if (g != null) foreach (var s in g) yield return s;
    }
}
