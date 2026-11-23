// re-c C# port — final scene (= web/src/game/states/finalscene.js, s152; checked s153 by finalscene_check.mjs).
// REVERSE.md substate table row 30.
//
//   SubstFinalSceneReset(m, y)  SubstFinalSceneReset 19:BDF2 (sub_state $1E, stored only by AI $70 phase 10 27:BD19):
//                               reset, blank nametables, the walk / messages / fly-off, FadeOut, main_state = 4.
//   FinalMsg(m)                 19:BFC9: message $2E of EventMsgTable19 19:BFE8, SFX $29, ShowMessage, $2E + 1.
using ReC.Game.Obj;
using ReC.Game.World;
using ReC.Gen;

namespace ReC.Game.States;

public static class FinalScene
{
    private const int MsgTable = 0xBFE8;                      // bank 19 at $A000 (SwitchBank16(9))

    // js: game/states/finalscene.js dec
    private static int Dec(Machine m, int a) { int v = (m.Rd(a) - 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/states/finalscene.js inc
    private static int Inc(Machine m, int a) { int v = (m.Rd(a) + 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/states/finalscene.js finalMsg
    /// <summary>19:BFC9: message $2E of EventMsgTable19, then $2E + 1</summary>
    private static IEnumerable<Y> FinalMsg(Machine m)
    {
        int n = m.Rd(0x2E), y = (n << 1) & 0xFF;
        m.Wr(0x07DF, Prg.CpuRd(m, MsgTable + y));
        m.Wr(0x07DE, Prg.CpuRd(m, MsgTable + y + 1));
        PlaySound.Play(m, 0x29, y);
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);                 // FarCall_D354
        foreach (var s in Talk.ShowMessage(m)) yield return s;
        Prg.FarRestore(m, r6, r7);
        m.Wr(0x2E, (n + 1) & 0xFF);
    }

    public static IEnumerable<Y> SubstFinalSceneReset(Machine m) => SubstFinalSceneReset(m, 0);

    // js: game/states/finalscene.js substFinalSceneReset
    /// <summary>SubstFinalSceneReset 19:BDF2. `y` = the Y register PlaySound $5C / $76 sees (after a sprite pass).</summary>
    public static IEnumerable<Y> SubstFinalSceneReset(Machine m, int y)
    {
        for (int sl = 4; sl < 0x1F; sl++) m.Wr(0x04A0 + sl, 0);
        m.Wr(R.IrqMode, 0);
        m.Wr(0x2E, 0); m.Wr(0x0600, 0); m.Wr(0x0620, 0); m.Wr(0x06C0, 0);
        m.Wr(0x07F4, 0x47);                                   // chr_r4_play
        m.Wr(0x07E6, 0x06); m.Wr(0x07E7, 0x1C);               // pal_id_spr2 / spr3
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);                          // 26:8C0E (keeps X)
        m.Wr(0x0710, 0);                                      // obj1_flash
        foreach (var s in Death.StatRecalc(m, 0x1E)) yield return s;   // X = sub_state from the CAB6 dispatch
        for (int i = 0; i < 0x100; i++) m.Wr(0x6000 + i, 0xFF);
        for (int b = 0x39; b <= 0x3C; b++) foreach (var s in Pause.PpuQueueBlock(m, b)) yield return s;
        for (int i = 0; i < 0x40; i++) m.Wr(0x6100 + i, 0);   // attr_shadow
        PpuQueue.QueueAttrAll(m);                             // 30:C739
        Prg.SetR6(m, 0x0B);
        for (int i = 0; i < 0x100; i++) m.Wr(0x6000 + i, Prg.CpuRd(m, 0x9E00 + i));
        foreach (var s in Pause.PpuQueueBlock(m, 0x45)) yield return s;
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;
        Prg.SetR6(m, 0x1A);

        do
        {                                                     // BE74: walk to x = $80
            Dec(m, 0x04E1);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            int d = 1;
            m.Wr(0x0301, 0x01); m.Wr(0x0581, 0x09);
            if ((m.Rd(0x70) & 0x80) != 0) { d = 0xFF; m.Wr(0x0301, 0x03); m.Wr(0x0581, 0x0B); }
            int x = (m.Rd(0x70) + d) & 0xFF;
            m.Wr(0x70, x); m.Wr(0x71, x);
            FormRevert.FarSpriteDraw(m);
        } while (m.Rd(0x70) != 0x80);
        m.Wr(0x0301, 0x00); m.Wr(0x0581, 0x08);
        do
        {                                                     // BEB1: walk up to y = $78
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Dec(m, 0x04E1);
            m.Wr(0xB1, Dec(m, 0xB0));
            FormRevert.FarSpriteDraw(m);
        } while (m.Rd(0xB0) != 0x78);
        m.Wr(0x0301, 0x04); m.Wr(0x0581, 0x08); m.Wr(0x04E1, 0x17);
        do
        {                                                     // BED5
            foreach (var s in Death.WaitFrame(m)) yield return s;
            FormRevert.FarSpriteDraw(m);
        } while (Dec(m, 0x04E1) != 0x08);
        m.Wr(0x0301, 0x00);
        PlaySound.Play(m, 0x5C, y);

        ObjUtil.ObjPosToTemp(m, 0);                           // BEEF: 26:98A8 X = 0
        m.Wr(0x10, 0x0D); m.Wr(0x11, 0xC2);
        ObjInit.FarObjInit(m);                                // FarCall_C25D
        int slot = m.Rd(0x10);
        ObjUtil.TempToObjPos(m, slot);                        // 26:9897
        m.Wr(0xB0 + slot, (m.Rd(0xB0 + slot) - 0x10) & 0xFF);
        m.Wr(0x038D, 0x09); m.Wr(0x030D, 0xFE); m.Wr(0x04ED, 0x08);
        FormRevert.FarSpriteDraw(m);
        foreach (var s in FinalMsg(m)) yield return s;
        foreach (var s in Death.WaitFrames(m, 0x10)) yield return s;
        m.Wr(0x04ED, 0x10);
        FormRevert.FarSpriteDraw(m);
        foreach (var s in FinalMsg(m)) yield return s;
        foreach (var s in Death.WaitFrames(m, 0x10)) yield return s;

        m.Wr(0x7E, 0x68); m.Wr(0xBE, 0xC8); m.Wr(0x9E, 0); m.Wr(0xDE, 0);   // BF33: slot $0E position
        m.Wr(0x10, 0x0E); m.Wr(0x11, 0xC2);
        ObjInit.FarObjInit(m);
        m.Wr(0x030E, 0x2C); m.Wr(0x038E, 0x09);
        do
        {                                                     // BF56
            foreach (var s in Death.WaitFrame(m)) yield return s;
            Dec(m, 0x04EE);
            Dec(m, 0xBE);
            FormRevert.FarSpriteDraw(m);
        } while (m.Rd(0xBE) != 0x78);
        m.Wr(0x030E, 0x2D);
        FormRevert.FarSpriteDraw(m);
        foreach (var s in FinalMsg(m)) yield return s;
        foreach (var s in Death.WaitFrames(m, 0x10)) yield return s;
        foreach (var s in FinalMsg(m)) yield return s;
        foreach (var s in FinalMsg(m)) yield return s;

        m.Wr(0x030E, 0x2E); m.Wr(0x0301, 0x02); m.Wr(0x0581, 0x0A);
        do
        {                                                     // BF8C
            if ((Dec(m, 0x08) & 0x18) == 0)
            {
                PlaySound.Play(m, 0x76, y);
                foreach (var s in Mesia.PalFlash(m, 0x16)) yield return s;   // MesiaPalFlash 19:BDDC, then JMP BFA4
            }
            else foreach (var s in Death.WaitFrame(m)) yield return s;
            FormRevert.FarSpriteDraw(m);                      // BFA4
            Dec(m, 0x04E1); Dec(m, 0x04EE);
            Inc(m, 0xB0);
            if ((Inc(m, 0xB1) & 7) != 0) Inc(m, 0xBE);
        } while (m.Rd(0xBE) != 0xE0);
        foreach (var s in Death.FadeOut(m, 0x20)) yield return s;   // 26:8C3D
        m.Wr(R.MainState, 0x04);                              // FinalSceneToEnding 19:BFC4
    }
}
