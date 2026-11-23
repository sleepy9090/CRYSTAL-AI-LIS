// re-c C# port — states/equip.js. STUB until TASKS.md T8 (C# s4: SwordPowerLevel ported for giveitem.js).
namespace ReC.Game.States;

public static class Equip
{
    public static IEnumerable<Y>? SubstEquipMenu(Machine m) { Stub.NotPorted(m, "game/states/equip.js", "substEquipMenu"); return null; }

    // js: game/states/equip.js swordPowerLevel
    /// <summary>SwordPowerLevel 30:D9D8: Y = 0/1/2</summary>
    public static int SwordPowerLevel(Machine m)
    {
        int sw = m.Rd(0x0711);
        if (sw == 0) return 0;
        int ball = m.Rd(0x0718);
        if (ball == 0)
        {                                                 // D9FC
            int a = m.Rd(0x643B + sw);
            if (a == 0xFF) return 0;
            return (a & 1) != 0 ? 0 : 1;
        }
        m.Wr(0x10, sw - 1);
        if (((ball - 1) & 3) != sw - 1) return 0;
        return ((ball - 1) & 0x0C) != 0 ? 2 : 1;
    }
}
