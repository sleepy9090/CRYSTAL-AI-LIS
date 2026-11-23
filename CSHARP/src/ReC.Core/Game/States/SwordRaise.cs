// re-c C# port — sword raise pose (= web/src/game/states/swordraise.js, s149). REVERSE.md substate table row 26.
//
//   SwordRaisePose(m, y)  SwordRaisePose 19:BC04 (sub_state $1A, stored by EventActionTable act 22 EvActSwordState
//                         30:D78C; event act 1 EvActThunderWarp 30:D5B4 JSRs it directly before msg 28.17): see the JS.
//                         JS default `y = 0` -> the one-argument overload.
using ReC.Gen;

namespace ReC.Game.States;

public static class SwordRaise
{
    public static IEnumerable<Y> SwordRaisePose(Machine m) => SwordRaisePose(m, 0);

    // js: game/states/swordraise.js swordRaisePose
    /// <summary>SwordRaisePose 19:BC04. `y` = the Y register at the dispatch (PlaySound stores it in $010F).</summary>
    public static IEnumerable<Y> SwordRaisePose(Machine m, int y)
    {
        for (int s = 4; s < 0x0C; s++) m.Wr(0x04A0 + s, 0);   // 30:C867
        foreach (var w in Death.WaitFrame(m)) yield return w; // BC07
        m.Wr(0x07F2, 0x5C);                                   // chr_r2_play
        m.Wr(0x07F3, 0x42);                                   // chr_r3_play
        m.Wr(0x06C0, 0x00);                                   // orbit_level
        int meta1 = m.Rd(0x0301), over1 = m.Rd(0x0581);       // PHA PHA
        m.Wr(0x0301, 0xB8);
        m.Wr(0x0581, 0x4C);                                   // obj_overlay_id_s01
        m.Wr(0x04E1, 0x7F);                                   // obj_anim_s01
        m.Wr(0x0102, m.Rd(0x0102) | 0x80);                    // snd_cur_music b7
        PlaySound.Play(m, 0x93, y);
        Talk.OamClipTop(m);                                   // BC3F
        do
        {                                                     // BC42
            foreach (var w in Death.WaitFrame(m)) yield return w;
            FormRevert.FarSpriteDraw(m);
            m.Wr(0x04E1, (m.Rd(0x04E1) - 1) & 0xFF);
        } while (m.Rd(0x04E1) != 0);
        foreach (var w in Death.WaitFrame(m)) yield return w; // BC4D
        m.Wr(0x0581, over1);                                  // PLA
        m.Wr(0x0301, meta1);                                  // PLA
        foreach (var w in Death.StatRecalc(m, 0)) yield return w;   // X = 0 after the sprite pass
        Talk.OamClipTop(m);                                   // BC5D
        FormRevert.FarSpriteDraw(m);                          // BC60
        foreach (var w in Death.WaitFrame(m)) yield return w; // BC63
        m.Wr(R.SubState, 0x08);
    }
}
