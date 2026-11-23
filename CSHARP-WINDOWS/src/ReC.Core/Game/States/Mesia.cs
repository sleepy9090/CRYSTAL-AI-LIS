// re-c C# port — Mesia reunion scene (= web/src/game/states/mesia.js, s150). REVERSE.md substate table row 27.
//
//   SubstMesiaReunion(m)  SubstMesiaReunion 19:BC6B (sub_state $1B, stored only by EventActionTable act 30
//                         EvActEnding 30:D827): MesiaScript 19:BCB0 over MesiaStepTable 19:BD59 (5-byte records),
//                         MesiaSpawnFly 19:BD91, then sub_state 7 (GiveItem reads slot $1F's param). See the JS.
//   PalFlash(m, a)        19:BDDC (also used by finalscene.js)
using ReC.Game.Obj;
using ReC.Gen;

namespace ReC.Game.States;

public static class Mesia
{
    private const int StepTable = 0xBD59;                     // bank 19 at $A000 (SwitchBank16(9))

    // js: game/states/mesia.js palFlash
    /// <summary>19:BDDC: pal_buf $6140-$615F = A, WaitFrame, SetR6($1A) + PaletteBuild 26:8C0E, WaitFrame</summary>
    public static IEnumerable<Y> PalFlash(Machine m, int a)
    {
        for (int y = 0x1F; y >= 0; y--) m.Wr(0x6140 + y, a);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);
        foreach (var s in Death.WaitFrame(m)) yield return s;
    }

    // js: game/states/mesia.js mesiaScript
    /// <summary>MesiaScript 19:BCB0 (X = 1)</summary>
    private static IEnumerable<Y> MesiaScript(Machine m)
    {
        const int x = 1;
        for (; ; )
        {
            int y = (m.Rd(0x20) * 5) & 0xFF, b = Prg.CpuRd(m, StepTable + y);
            if (b == 0x81) yield break;
            if (b != 0x80)
            {
                m.Wr(0x30, b);
                m.Wr(0x31, Prg.CpuRd(m, StepTable + y + 1));
                m.Wr(0x21, Prg.CpuRd(m, StepTable + y + 2));
                m.Wr(0x0300 + x, Prg.CpuRd(m, StepTable + y + 3));
                m.Wr(0x0580 + x, Prg.CpuRd(m, StepTable + y + 4));
                do
                {                                             // BCDC
                    foreach (var s in Death.WaitFrame(m)) yield return s;
                    ObjUtil.ObjPosToTemp(m, x);
                    ObjUtil.TempAddStep(m);
                    ObjUtil.TempToObjPos(m, x);
                    ObjUtil.TempToObjPos(m, x - 1);           // BCE8: X = 1 -> slot 0 follows
                    m.Wr(0x04E0 + x, (m.Rd(0x04E0 + x) - 1) & 0xFF);
                    FormRevert.FarSpriteDraw(m);
                    m.Wr(0x21, (m.Rd(0x21) - 1) & 0xFF);
                } while (m.Rd(0x21) != 0);
                m.Wr(0x20, (m.Rd(0x20) + 1) & 0xFF);
                continue;
            }
            m.Wr(0x04E0 + x, Prg.CpuRd(m, StepTable + y + 2));   // BD00
            m.Wr(0x0300 + x, Prg.CpuRd(m, StepTable + y + 3));
            m.Wr(0x0580 + x, Prg.CpuRd(m, StepTable + y + 4));
            do
            {                                                 // BD12
                foreach (var s in Death.WaitFrame(m)) yield return s;
                FormRevert.FarSpriteDraw(m);
                m.Wr(0x04E0 + x, (m.Rd(0x04E0 + x) - 1) & 0xFF);
            } while (m.Rd(0x04E0 + x) != 0x08);
            m.Wr(0x20, (m.Rd(0x20) + 1) & 0xFF);
            ObjUtil.ObjPosToTemp(m, x);                       // BD26
            m.Wr(0x10, m.Rd(0x22));
            m.Wr(0x11, 0xC2);
            ObjInit.FarObjInit(m);                            // FarCall_C25D
            int sl = m.Rd(0x10);
            ObjUtil.TempToObjPos(m, sl);
            m.Wr(0xB0 + sl, (m.Rd(0xB0 + sl) - 0x18) & 0xFF);
            m.Wr(0x0380 + sl, 0x0D);
            m.Wr(0x0300 + sl, 0xFE);
            m.Wr(0x22, (m.Rd(0x22) + 1) & 0xFF);
        }
    }

    // js: game/states/mesia.js mesiaSpawnFly
    /// <summary>MesiaSpawnFly 19:BD91</summary>
    private static IEnumerable<Y> MesiaSpawnFly(Machine m)
    {
        m.Wr(0x21, 0x10);
        do
        {                                                     // BD95
            foreach (var s in Death.WaitFrame(m)) yield return s;
            m.Wr(0x85, (m.Rd(0x85) + 3) & 0xFF);
            m.Wr(0x86, (m.Rd(0x86) + 1) & 0xFF);
            m.Wr(0x87, (m.Rd(0x87) - 1) & 0xFF);
            m.Wr(0x88, (m.Rd(0x88) - 3) & 0xFF);
            FormRevert.FarSpriteDraw(m);
            m.Wr(0x21, (m.Rd(0x21) - 1) & 0xFF);
        } while (m.Rd(0x21) != 0);
        m.Wr(0x04B5, 0); m.Wr(0x04B6, 0); m.Wr(0x04B7, 0);    // BDAF
        m.Wr(0x0398, 0x09);
        m.Wr(0x21, 0x08);
        do
        {                                                     // BDC3
            foreach (var s in PalFlash(m, 0x30)) yield return s;
            foreach (var s in Death.WaitFrame(m)) yield return s;
            m.Wr(0xC8, (m.Rd(0xC8) + 1) & 0xFF);              // obj_y_lo slot $18
            FormRevert.FarSpriteDraw(m);
            m.Wr(0x21, (m.Rd(0x21) - 1) & 0xFF);
        } while (m.Rd(0x21) != 0);
        m.Wr(0x04B8, 0);
    }

    // js: game/states/mesia.js substMesiaReunion
    /// <summary>SubstMesiaReunion 19:BC6B</summary>
    public static IEnumerable<Y> SubstMesiaReunion(Machine m)
    {
        m.Wr(0x0600, 0); m.Wr(0x0620, 0); m.Wr(0x06C0, 0); m.Wr(0x0710, 0);
        m.Wr(0x0381, m.Rd(0x0381) & 0xFB);
        Prg.SetR6(m, 0x1A);
        foreach (var s in Death.StatRecalc(m, 0x1B)) yield return s;   // X = sub_state from the CAB6 dispatch
        m.Wr(0x20, 0x00);
        m.Wr(0x22, 0x15);
        foreach (var s in MesiaScript(m)) yield return s;
        foreach (var s in MesiaSpawnFly(m)) yield return s;
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);                          // 26:8C0E
        m.Wr(0x0623, 0x1F);
        m.Wr(0x057F, 0x04);
        m.Wr(R.SubState, 0x07);
    }
}
