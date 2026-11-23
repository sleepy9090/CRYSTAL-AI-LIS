// re-c C# port — states/talk.js. STUB until TASKS.md T8.
namespace ReC.Game.States;

public static class Talk
{
    public static IEnumerable<Y>? NpcTalk(Machine m) { Stub.NotPorted(m, "game/states/talk.js", "npcTalk"); return null; }

    /// <summary>talk.js TALK_ACTIONS[act](m, y)</summary>
    public static IEnumerable<Y>? TalkAction(Machine m, int act, int y) { Stub.NotPorted(m, "game/states/talk.js", $"TALK_ACTIONS[{act}]"); return null; }
}
