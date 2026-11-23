// re-c C# port — STUBS until TASKS.md T9 for the substate modules play.js dispatches to (each class moves into its own
// file, Wake.cs / Teleport.cs / ..., when ported; delete it from here then).
namespace ReC.Game.States;

public static class Wake
{
    public static IEnumerable<Y>? SubstWakeCutscene(Machine m) { Stub.NotPorted(m, "game/states/wake.js", "substWakeCutscene"); return null; }
}

public static class Teleport
{
    public static IEnumerable<Y>? SubstTeleportMenu(Machine m) => SubstTeleportMenu(m, m.Rd(0x010F));
    public static IEnumerable<Y>? SubstTeleportMenu(Machine m, int yIn) { Stub.NotPorted(m, "game/states/teleport.js", "substTeleportMenu"); return null; }
}

public static class Telepathy
{
    public static IEnumerable<Y>? SubstTelepathySelect(Machine m) { Stub.NotPorted(m, "game/states/telepathy.js", "substTelepathySelect"); return null; }
}

public static class ChangeForm
{
    public static IEnumerable<Y>? SubstChangeForm(Machine m) { Stub.NotPorted(m, "game/states/changeform.js", "substChangeForm"); return null; }
}

public static class Cure
{
    public static IEnumerable<Y>? SubstCureMagic(Machine m) { Stub.NotPorted(m, "game/states/cure.js", "substCureMagic"); return null; }
}

public static class FormRevert
{
    public static IEnumerable<Y>? SubstFormRevert(Machine m) { Stub.NotPorted(m, "game/states/formrevert.js", "substFormRevert"); return null; }

    // js: game/states/formrevert.js farSpriteDraw
    /// <summary>FarCall_B14_800E: sprite pass with banks 14/15 mapped, then $6E/$6F restored</summary>
    public static void FarSpriteDraw(Machine m)
    {
        int r6 = m.Rd(0x6E), r7 = m.Rd(0x6F);
        Prg.SwitchBank16(m, 0x0E);
        Obj.Sprites.SpriteDraw(m);
        Prg.FarRestore(m, r6, r7);
    }
}

public static class SwordRaise
{
    public static IEnumerable<Y>? SwordRaisePose(Machine m) => SwordRaisePose(m, 0);
    public static IEnumerable<Y>? SwordRaisePose(Machine m, int y) { Stub.NotPorted(m, "game/states/swordraise.js", "swordRaisePose"); return null; }
}

public static class Mesia
{
    public static IEnumerable<Y>? SubstMesiaReunion(Machine m) { Stub.NotPorted(m, "game/states/mesia.js", "substMesiaReunion"); return null; }
}

public static class FinalScene
{
    public static IEnumerable<Y>? SubstFinalSceneReset(Machine m) { Stub.NotPorted(m, "game/states/finalscene.js", "substFinalSceneReset"); return null; }
}
