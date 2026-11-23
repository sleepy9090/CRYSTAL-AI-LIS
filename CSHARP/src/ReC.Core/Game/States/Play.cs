// re-c C# port — states/play.js. STUB until TASKS.md T4.
namespace ReC.Game.States;

public static class Play
{
    public static IEnumerable<Y>? MainState1(Machine m) { Stub.NotPorted(m, "game/states/play.js", "mainState1"); return null; }

    /// <summary>(play.js ENTRY_POINTS) routine address `BB:AAAA` -> routine started by Machine.LoadState. Empty until T4:
    /// a state whose `until` is missing here logs once.</summary>
    public static readonly Dictionary<string, Func<Machine, IEnumerable<Y>>> EntryPoints = new();
}
