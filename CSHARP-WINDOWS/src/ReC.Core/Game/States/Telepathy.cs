// re-c C# port — magic 3, telepathy (= web/src/game/states/telepathy.js, s145). REVERSE.md substate table rows 21/22,
// "Telepathy reply `Telepathy` `14:816F`".
//
//   SubstTelepathySelect(m)  SubstTelepathySelect 30:DD6B (sub_state $15, set by magic 3 27:A18E): window 30:DF8F,
//                            loop 30:DD9A (chime, cursor blink, caster + 4 sage faces, A = reply, B/SEL/START = exit,
//                            L/R = sage), SubstTelepathyReply 30:DE04, exit 30:DE51. See the JS.
//   BlackenBg / SelectOpen / CursorX / SelectExit are shared with cure.js / formrevert.js / changeform.js.
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class Telepathy
{
    private const int Oam = 0x0200, Blink = 0x07FF, Mp = 0x0708, MpMax = 0x0709, Rng = 0x07C0, Level = 0x0421;

    // js: game/states/telepathy.js hideOam0
    public static void HideOam0(Machine m) { for (int i = 0; i < 4; i++) m.Wr(Oam + i, 0xF0); }

    // js: game/states/telepathy.js chime
    /// <summary>30:DE72: SFX $2C when $07FF &amp; $3F = 0. Y = the caller's Y (PlaySound saves it in $010F).</summary>
    private static void Chime(Machine m, int y) { if ((m.Rd(Blink) & 0x3F) == 0) PlaySound.Play(m, 0x2C, y); }

    // js: game/states/telepathy.js blackenBg
    /// <summary>30:C874: BG palettes = $0F, $6000-$603F = 0, PpuQueueBlock($2B), WaitFrame</summary>
    public static IEnumerable<Y> BlackenBg(Machine m)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (int x = 0x1C; x >= 0; x -= 4) m.Wr(0x6140 + x, 0x0F);
        for (int x = 0x0C; x >= 0; x--) m.Wr(0x6140 + x, 0x0F);
        for (int x = 0x3F; x >= 0; x--) m.Wr(0x6000 + x, 0);
        foreach (var s in Pause.PpuQueueBlock(m, 0x2B)) yield return s;
        foreach (var s in Death.WaitFrame(m)) yield return s;
    }

    // js: game/states/telepathy.js selectOpen
    /// <summary>30:DF8F: the message window + cursor setup</summary>
    public static IEnumerable<Y> SelectOpen(Machine m)
    {
        m.Wr(0x04AC, 0);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        Prg.SetR6(m, 0x14);
        foreach (var s in Talk.TextWindowOpen(m)) yield return s;   // 20:8500
        foreach (var s in Talk.TextPrintBox(m)) yield return s;     // 20:8528
        foreach (var s in BlackenBg(m)) yield return s;             // 30:C874
        m.Wr(R.ChrR0Alt, 0x3C);
        m.Wr(R.ChrR0Alt + 1, 0x38);
        m.Wr(Oam + 3, 0x38);
        m.Wr(0x10, m.Rd(R.CamYLo) & 7);
        m.Wr(Oam, (0x4C - m.Rd(0x10)) & 0xFF);
        m.Wr(Oam + 2, 0);
        m.Wr(R.IrqMode, 1);
        m.Wr(0x21, 0);
    }

    // js: game/states/telepathy.js casterRecord
    /// <summary>TelepathyCasterRecord 30:DFFD-31:E037: record 0 at $6000 = the player (meta $CD); while $1F != 0 the
    /// casting pose (meta $CC, anim = $07FF >> 2, $1F = 0 once that is 0)</summary>
    private static void CasterRecord(Machine m)
    {
        int[] rec = [0x84, 0xA0, 0xCD, 0x4C, 0x0B, 0x00, 0x00];
        for (int i = 0; i < rec.Length; i++) m.Wr(0x6000 + i, rec[i]);
        m.Wr(0x6007, (m.Rd(0x6007) - 1) & 0xFF);
        if (m.Rd(0x1F) == 0) return;
        m.Wr(0x6002, 0xCC);
        int a = m.Rd(Blink) >> 2;
        m.Wr(0x6007, a);
        if (a == 0) m.Wr(0x1F, 0);
    }

    // js: game/states/telepathy.js drawFaces
    /// <summary>30:DFCE: the caster + the 4 sage faces = records 30:E088 at $6008-$6027, ObjSlot1FDraw</summary>
    private static IEnumerable<Y> DrawFaces(Machine m)
    {
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        CasterRecord(m);                                      // 30:DFFD
        for (int x = 0x1F; x >= 0; x--) m.Wr(0x6008 + x, Prg.CpuRd(m, 0xE088 + x));
        m.Wr(0x10, m.Rd(R.CamYLo) & 7);
        int y = (0x4C - m.Rd(0x10)) & 0xFF;
        foreach (int a in new[] { 0x6009, 0x6011, 0x6019, 0x6021 }) m.Wr(a, y);
        m.Wr(0x20, 4);
        Pause.ObjSlot1FDraw(m);
    }

    // js: game/states/telepathy.js cursorX
    /// <summary>30:DDED: $21 &amp;= 3, cursor x = $21 * 48 + $38</summary>
    public static void CursorX(Machine m)
    {
        int s = m.Rd(0x21) & 3;
        m.Wr(0x21, s);
        m.Wr(0x10, (s << 4) & 0xFF);
        m.Wr(Oam + 3, (s * 0x30 + 0x38) & 0xFF);
    }

    // js: game/states/telepathy.js substTelepathySelect
    /// <summary>SubstTelepathySelect 30:DD6B</summary>
    public static IEnumerable<Y> SubstTelepathySelect(Machine m)
    {
        int savedIrq = m.Rd(R.IrqMode);                       // PHA, pulled at 30:DE6A
        m.Wr(0x07DE, 0x02);                                   // msg 33.2
        m.Wr(0x07DF, 0x21);
        foreach (var s in SelectOpen(m)) yield return s;
        m.Wr(0x5C, 0x50);                                     // chr_r4_alt
        m.Wr(0x07F2, 0x5C);                                   // chr_r2_play
        m.Wr(0x5A, 0x5C);                                     // chr_r2_alt
        for (int x = 0x0F; x >= 0; x--) m.Wr(0x6150 + x, Prg.CpuRd(m, 0xE0A8 + x));
        m.Wr(Blink, 0x3F);
        m.Wr(0x1F, 1);
        int y = m.Rd(0x010F);                                 // Y at the first DE72: never used ($07FF = $3F)
        for (; ; )
        {                                                     // DD9A
            Chime(m, y);
            int b = (m.Rd(Blink) - 1) & 0xFF;                 // 30:DDCD
            m.Wr(Blink, b);
            m.Wr(Oam + 1, (b & 0x08) != 0 ? 0x3F : 0x3D);
            foreach (var s in DrawFaces(m)) yield return s;
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Pads.ReadPadsMenu(m);
            int k = Teleport.PadNewButtonIndex(m);
            y = k < 0 ? 0xFF : k * 2;
            if (k < 0 || k == 4 || k == 5) continue;
            if (k == 0)
            {
                foreach (var s in TelepathyReply(m)) yield return s;
                break;
            }
            if (k <= 3) break;                                // B / SELECT / START -> 30:DE51
            m.Wr(0x21, (m.Rd(0x21) + (k == 7 ? 1 : -1)) & 0xFF);   // DDE1 INC INC DEC / DDE5 DEC
            CursorX(m);
        }
        foreach (var s in SelectExit(m, savedIrq)) yield return s;
    }

    // js: game/states/telepathy.js telepathy
    /// <summary>Telepathy 14:816F ($23 = sage; banks 14/15 mapped) -> $21:$20 = linear message id</summary>
    private static void TelepathyMsg(Machine m)
    {
        int sage = m.Rd(0x23);
        // js: game/states/telepathy.js row
        void Row(int r)
        {                                                     // 14:8179
            int x = (((r << 2) | sage) << 1) & 0xFF;
            m.Wr(0x21, Prg.CpuRd(m, 0x9A2C + x));
            m.Wr(0x20, Prg.CpuRd(m, 0x9A2D + x));
        }
        int mp = m.Rd(Mp) - 8;
        if (mp < 0) { Row(0); return; }
        m.Wr(Mp, mp);
        int n = m.Rd(Rng);                                    // 14:8222
        m.Wr(Rng, (n + 1) & 0xFF);
        int r = Prg.CpuRd(m, 0x821A + Prg.CpuRd(m, 0x822F + (n & 0x3F)));
        if (r == 0)
        {
            int v = 0x20 + mp;
            m.Wr(Mp, v > 0xFF || v >= m.Rd(MpMax) ? m.Rd(MpMax) : v);
            Row(1);
            return;
        }
        if (r == 1) { Row(2); return; }
        m.Wr(0x29, r & 1);
        int cls = Prg.CpuRd(m, 0x98F4 + m.Rd(R.AreaId));
        m.Wr(0x28, cls);
        if (Prg.CpuRd(m, 0x8213 + cls) >= m.Rd(Level)) { Row(3); return; }
        // TelepathyHint 14:81D1: first entry whose flag test is false gives the message (alt with b6 + $29)
        int xx = (((cls << 2) | sage) << 1) & 0xFF;
        m.Wr(0x24, Prg.CpuRd(m, 0x99F4 + xx));
        m.Wr(0x25, Prg.CpuRd(m, 0x99F5 + xx));
        int ptr = m.Rd(0x24) | (m.Rd(0x25) << 8);
        int y = 0;
        for (; ; )
        {
            var (t, y2) = Talk.FlagTest(m, y);
            y = y2;
            int b0 = m.Rd(0x26);
            if (!t)
            {
                if ((b0 & 0x40) != 0 && m.Rd(0x29) != 0) y += 2;
                m.Wr(0x21, Prg.CpuRd(m, (ptr + y) & 0xFFFF));
                m.Wr(0x20, Prg.CpuRd(m, (ptr + y + 1) & 0xFFFF));
                return;
            }
            y += 2;
            if ((b0 & 0x40) != 0) y += 2;
            if ((b0 & 0x80) != 0) { Row(3); return; }
        }
    }

    // js: game/states/telepathy.js telepathyReply
    /// <summary>SubstTelepathyReply 30:DE04</summary>
    private static IEnumerable<Y> TelepathyReply(Machine m)
    {
        HideOam0(m);
        Talk.OamClipTop(m);
        int sage = m.Rd(0x21);
        m.Wr(0x23, sage);
        Prg.SwitchBank16(m, 7);
        TelepathyMsg(m);
        m.Wr(0x07DE, m.Rd(0x20));
        m.Wr(0x07DF, m.Rd(0x21));
        int id = m.Rd(0x07DE);                                // TextIdUnpack 30:D3C2
        m.Wr(0x07DF, ((m.Rd(0x07DF) << 3) | (id >> 5)) & 0xFF);
        m.Wr(0x07DE, id & 0x1F);
        Prg.SetR6(m, 0x14);
        foreach (var s in Talk.TextPrintBox(m)) yield return s;   // 20:8528
        m.Wr(0x21, sage);
        for (; ; )
        {                                                     // DE2F
            foreach (var s in Death.WaitFrame(m)) yield return s;
            foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
            Chime(m, m.Rd(0x010F));                           // Y here = ReadPadStable's (not tracked; approximation)
            m.Wr(Blink, (m.Rd(Blink) - 1) & 0xFF);
            m.Wr(0x20, 1);
            CasterRecord(m);                                  // 30:DFFD
            int sh = (m.Rd(0x21) << 3) & 0xFF;                // 30:DE7E
            for (int i = 0; i < 8; i++) m.Wr(0x6008 + i, Prg.CpuRd(m, 0xDEA1 + sh + i));
            m.Wr(0x10, m.Rd(R.CamYLo) & 7);
            m.Wr(0x6009, (0x44 - m.Rd(0x10)) & 0xFF);
            Pause.ObjSlot1FDraw(m);
            Pads.ReadPadsMenu(m);
            if ((m.Rd(R.PadNew) & 0xF0) != 0) yield break;
        }
    }

    // js: game/states/telepathy.js selectExit
    /// <summary>30:DE51</summary>
    public static IEnumerable<Y> SelectExit(Machine m, int savedIrq)
    {
        HideOam0(m);
        Death.OamClip(m, 0xF0);                               // OamClipTail
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);                          // JSR 26:8C0E
        PpuQueue.QueueAttrAll(m);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        foreach (var s in Death.StatRecalc(m)) yield return s;
        m.Wr(R.IrqMode, savedIrq);
        m.Wr(R.SubState, 8);
    }
}
