// re-c C# port — form revert (= web/src/game/states/formrevert.js, s148). REVERSE.md substate table row 25.
//
//   SubstFormRevert(m, y)  SubstFormRevert 19:BB9D (sub_state $19, set by PlayerMagic 27:A022 when obj1_flash b7 =
//                          changed form): any magic press turns the player back into a human (see the JS). Unlike
//                          SubstCureMagic the slot 1 meta is NOT pushed / restored: it stays $4C.
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class FormRevert
{
    // js: game/states/formrevert.js farSpriteDraw
    /// <summary>FarCall_B14_800E: sprite pass with banks 14/15 mapped, then $6E/$6F restored</summary>
    public static void FarSpriteDraw(Machine m)
    {
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        Prg.SwitchBank16(m, 0x0E);
        Obj.Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
    }

    public static IEnumerable<Y> SubstFormRevert(Machine m) => SubstFormRevert(m, 0);

    // js: game/states/formrevert.js substFormRevert
    /// <summary>SubstFormRevert 19:BB9D. `y` = the Y register at the dispatch (PlaySound stores it in $010F).</summary>
    public static IEnumerable<Y> SubstFormRevert(Machine m, int y)
    {
        foreach (var s in Telepathy.BlackenBg(m)) yield return s;   // BgBlackClearBuf 30:C874
        foreach (var s in Death.WaitFrame(m)) yield return s; // BBA0
        m.Wr(0x07F2, m.Rd(0x0714) != 0 ? 0x41 : 0x40);        // chr_r2_play by equip_shield
        m.Wr(0x07F3, 0x5C);                                   // chr_r3_play
        m.Wr(0x0381, m.Rd(0x0381) | 0x02);                    // obj_flags_s01
        m.Wr(0x0300, 0x02);
        m.Wr(0x0321, 0x00);                                   // obj_tile_ofs_s01
        m.Wr(0x0581, 0xD0);                                   // obj_overlay_id_s01
        m.Wr(0x04E1, 0x1F);                                   // obj_anim_s01
        m.Wr(0x0301, 0x4C);
        m.Wr(0x0710, m.Rd(0x0710) & 0x4F);                    // obj1_flash: form bits cleared
        PlaySound.Play(m, 0x39, y);
        Talk.OamClipTop(m);                                   // BBE4 (A = $F0 is overwritten at 30:C17D)
        do
        {                                                     // BBE7
            foreach (var s in Death.WaitFrame(m)) yield return s;
            FarSpriteDraw(m);
            m.Wr(0x04E1, (m.Rd(0x04E1) - 1) & 0xFF);
        } while (m.Rd(0x04E1) != 0);
        foreach (var s in Death.StatRecalc(m, 0)) yield return s;   // X = 0 after the sprite pass
        m.Wr(0x0300, 0xA7);
        FarSpriteDraw(m);                                     // BBFA
        m.Wr(R.SubState, 0x08);
        PpuQueue.QueueAttrAll(m);                             // JMP PpuQueueAttrAll 30:C739
    }
}
