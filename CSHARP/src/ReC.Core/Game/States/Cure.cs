// re-c C# port — magic 5, cure (= web/src/game/states/cure.js, s147). REVERSE.md substate table row 24.
//
//   SubstCureMagic(m, y)  SubstCureMagic 19:BB39 (sub_state $18, set by MagicCure 27:A17D and the stone path 31:F019,
//                         both after SpendMp($18)): see the JS. JS default `y = 0` -> the one-argument overload.
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class Cure
{
    public static IEnumerable<Y> SubstCureMagic(Machine m) => SubstCureMagic(m, 0);

    // js: game/states/cure.js substCureMagic
    /// <summary>SubstCureMagic 19:BB39. `y` = the Y register at the dispatch (PlaySound stores it in $010F).</summary>
    public static IEnumerable<Y> SubstCureMagic(Machine m, int y)
    {
        foreach (var s in Telepathy.BlackenBg(m)) yield return s;   // BgBlackClearBuf 30:C874
        m.Wr(0x0710, m.Rd(0x0710) & 0xF0);                    // obj1_flash: status cleared
        m.Wr(0x05A0, 0);                                      // obj_freeze
        m.Wr(0x07F2, 0x40);                                   // chr_r2_play
        m.Wr(0x07F3, 0x42);                                   // chr_r3_play
        int meta1 = m.Rd(0x0301);                             // PHA
        m.Wr(0x0300, 0x02);
        m.Wr(0x0581, 0xCB);                                   // obj_overlay_id_s01
        m.Wr(0x04E1, 0x3F);                                   // obj_anim_s01
        m.Wr(0x0301, 0x4C);
        m.Wr(0x0321, 0x00);                                   // obj_tile_ofs_s01
        PlaySound.Play(m, 0x25, y);
        do
        {                                                     // BB75
            foreach (var s in Death.WaitFrame(m)) yield return s;
            int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);             // FarCall_B14_800E
            Prg.SwitchBank16(m, 0x0E);
            Obj.Sprites.SpriteDraw(m);
            Prg.FarRestore(m, r6, r7);
            m.Wr(0x04E1, (m.Rd(0x04E1) - 1) & 0xFF);
        } while (m.Rd(0x04E1) != 0);
        m.Wr(0x0301, meta1);                                  // PLA
        foreach (var s in Death.StatRecalc(m, 0)) yield return s;   // X = 0 after the sprite pass (s147 RAM diff)
        m.Wr(0x0300, 0xA7);
        m.Wr(R.SubState, 0x10);
        m.Wr(0x06C3, 0x02);                                   // pending_msg_id
        m.Wr(0x06E3, 0x20);                                   // pending_msg_group: msg 32.2
        PpuQueue.QueueAttrAll(m);                             // JMP PpuQueueAttrAll 30:C739
    }
}
