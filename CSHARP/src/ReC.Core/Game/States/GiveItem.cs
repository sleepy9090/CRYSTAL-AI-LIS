// re-c C# port — states/giveitem.js. STUB until TASKS.md T8.
namespace ReC.Game.States;

public static class GiveItem
{
    /// <summary>giveitem.js giveItem (GiveItem 30:D3EB)</summary>
    public static IEnumerable<Y>? Give(Machine m) { Stub.NotPorted(m, "game/states/giveitem.js", "giveItem"); return null; }
    public static IEnumerable<Y>? ItemUseMenu(Machine m) { Stub.NotPorted(m, "game/states/giveitem.js", "itemUseMenu"); return null; }
    public static IEnumerable<Y>? ShowPendingMessage(Machine m) { Stub.NotPorted(m, "game/states/giveitem.js", "showPendingMessage"); return null; }
    public static void Area5FIrqOff(Machine m) => Stub.NotPorted(m, "game/states/giveitem.js", "area5FIrqOff");

    /// <summary>giveitem.js EVENT_ACTIONS[act](m, y)</summary>
    public static IEnumerable<Y>? EventAction(Machine m, int act, int y) { Stub.NotPorted(m, "game/states/giveitem.js", $"EVENT_ACTIONS[{act}]"); return null; }
}
