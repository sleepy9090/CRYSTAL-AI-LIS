// re-c C# port — magic 7, change form (= web/src/game/states/changeform.js, s146). REVERSE.md substate table row 23.
//
//   SubstChangeForm(m)  SubstChangeForm 30:DEC1 (sub_state $17, set by MagicChangeForm 27:A161): msg 33.1 window
//                       (TelepathyOpen 30:DF8F), loop 30:DEE4 (cursor blink, caster, 4 form icons), A = ChangeFormApply
//                       30:DF2F, B/SEL/START = exit 30:DE51, L/R = form. See the JS.
using ReC.Game.Obj;
using ReC.Gen;

namespace ReC.Game.States;

public static class ChangeForm
{
    private const int Oam = 0x0200, Rot = 0x07FF, R4Play = 0x07F4, Flash = 0x0710;

    // js: game/states/changeform.js casterRecord
    /// <summary>ChangeFormCasterRecord 31:E038: record 0 at $6000 = the player (meta $02); while $1F != 0 the form preview:
    /// anim = $07FF, meta = $21*4 + $2E, tile ofs $D0</summary>
    private static void CasterRecord(Machine m)
    {
        int[] rec = [0x84, 0xA0, 0x02, 0x4C, 0x0B, 0x00, 0x00, 0x00];
        for (int i = 0; i < rec.Length; i++) m.Wr(0x6000 + i, rec[i]);
        if (m.Rd(0x1F) == 0) return;
        m.Wr(0x6007, m.Rd(Rot));
        m.Wr(0x6002, ((m.Rd(0x21) << 2) + 0x2E) & 0xFF);
        m.Wr(0x6003, 0xD0);
    }

    // js: game/states/changeform.js drawIcons
    /// <summary>30:DFD4: the 4 icons = records 30:E088 at $6008-$6027 (y = $4C - (cam_y &amp; 7)), $20 = 4, ObjSlot1FDraw</summary>
    private static void DrawIcons(Machine m)
    {
        for (int x = 0x1F; x >= 0; x--) m.Wr(0x6008 + x, Prg.CpuRd(m, 0xE088 + x));
        m.Wr(0x10, m.Rd(R.CamYLo) & 7);
        int y = (0x4C - m.Rd(0x10)) & 0xFF;
        foreach (int a in new[] { 0x6009, 0x6011, 0x6019, 0x6021 }) m.Wr(a, y);
        m.Wr(0x20, 4);
        Pause.ObjSlot1FDraw(m);
    }

    // js: game/states/changeform.js oamAttrPalClear
    /// <summary>OamAttrPalClear 31:E077: every OAM attribute byte &amp;= $FC (sprite palette 0)</summary>
    private static void OamAttrPalClear(Machine m)
    {
        for (int x = 0; x < 0x100; x += 4) m.Wr(Oam + 2 + x, m.Rd(Oam + 2 + x) & 0xFC);
    }

    // js: game/states/changeform.js substChangeForm
    /// <summary>SubstChangeForm 30:DEC1</summary>
    public static IEnumerable<Y> SubstChangeForm(Machine m)
    {
        int savedIrq = m.Rd(R.IrqMode);                       // PHA, pulled at 30:DE6A
        m.Wr(0x06C0, 0);                                      // orbit_level
        m.Wr(0x07DE, 0x01);                                   // msg 33.1
        m.Wr(0x07DF, 0x21);
        foreach (var s in Telepathy.SelectOpen(m)) yield return s;   // TelepathyOpen 30:DF8F
        m.Wr(0x5C, 0x4D);                                     // chr_r4_alt
        m.Wr(0x5A, 0x5C);                                     // chr_r2_alt
        m.Wr(0x07F3, 0x5C);                                   // chr_r3_play
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        for (; ; )
        {                                                     // DEE4
            int b = (m.Rd(Rot) - 1) & 0xFF;                   // TelepathyCursorBlink 30:DDCD
            m.Wr(Rot, b);
            m.Wr(Oam + 1, (b & 0x08) != 0 ? 0x3F : 0x3D);
            m.Wr(0x1F, 0);
            CasterRecord(m);                                  // 31:E038
            DrawIcons(m);                                     // 30:DFD4
            OamAttrPalClear(m);                               // 31:E077
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Pads.ReadPadsMenu(m);
            int k = Teleport.PadNewButtonIndex(m);
            if (k < 0 || k == 4 || k == 5) continue;          // BCC / U/D -> DEE4
            if (k == 0)
            {
                foreach (var s in ChangeFormApply(m)) yield return s;
                break;
            }
            if (k <= 3) break;                                // B / SELECT / START -> 30:DE51
            m.Wr(0x21, (m.Rd(0x21) + (k == 7 ? 1 : -1)) & 0xFF);   // DF1E INC INC DEC / DF22 DEC
            Telepathy.CursorX(m);                             // 30:DDED
            PlaySound.Play(m, 0x4B, k * 2);                   // Y = the jump-table index (TAY at 30:DF00)
        }
        foreach (var s in Telepathy.SelectExit(m, savedIrq)) yield return s;   // 30:DE51
    }

    // js: game/states/changeform.js changeFormApply
    /// <summary>ChangeFormApply 30:DF2F (Y = 0 from the A-key index)</summary>
    private static IEnumerable<Y> ChangeFormApply(Machine m)
    {
        PlaySound.Play(m, 0x39, 0);
        Telepathy.HideOam0(m);
        m.Wr(Rot, 0x1F);
        m.Wr(0x1F, 0x1F);
        int r4 = m.Rd(R4Play);
        m.Wr(R4Play, 0x4D);
        do
        {                                                     // DF47
            foreach (var s in Death.WaitFrame(m)) yield return s;
            CasterRecord(m);
            DrawIcons(m);
            OamAttrPalClear(m);
            m.Wr(Rot, (m.Rd(Rot) - 1) & 0xFF);
        } while (m.Rd(Rot) != 0);
        m.Wr(0x6003, 0x4C);
        DrawIcons(m);
        OamAttrPalClear(m);
        for (int y = 0x20; y != 0; y--) foreach (var s in Death.WaitFrame(m)) yield return s;   // 30:C176 (Y = $20)
        Death.OamClip(m, 0xF0);                               // OamClipTail
        foreach (var s in Death.WaitFrame(m)) yield return s;
        m.Wr(R4Play, r4);
        m.Wr(Flash, ((m.Rd(0x21) << 4) | m.Rd(Flash) | 0x80) & 0xFF);
        Prg.SetR6(m, 0x1A);
        // MagicChangeForm 27:A161 already checked MP >= $14; a short MP here would PLA PLA into the PHA'd irq_mode
        var ok = new Ret<bool>();
        foreach (var s in Player.SpendMp(m, 0x14, 0, ok)) yield return s;
        if (!ok.V) m.Log("ChangeFormApply: SpendMp short (stack would break in the ROM)");
    }
}
