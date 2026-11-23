// re-c C# port — what a multi-frame routine yields (docs/architecture_cs.md §3).
// JS `yield` (undefined) = Y.Frame; `yield SPIN` / `yield NO_NMI` (states/death.js) = Y.Spin / Y.NoNmi.
namespace ReC.Game;

public enum Y : byte
{
    /// <summary>the ROM waits for the next NMI (JS bare `yield`)</summary>
    Frame,
    /// <summary>ROM spin loop (queue wait): frame_ready stays set (death.js SPIN)</summary>
    Spin,
    /// <summary>the next NMI is lost (death.js NO_NMI)</summary>
    NoNmi,
}

/// <summary>Logging stubs for callees whose JS module is not translated yet (TASKS.md: the solution always
/// builds; a stub logs `not ported: &lt;module&gt; &lt;name&gt;` once per name, like the JS stubs did).</summary>
public static class Stub
{
    public static void NotPorted(Machine m, string module, string name)
    {
        string k = module + " " + name;
        if (m.Unported.Add(k)) m.Log($"not ported: {k}");
        m.StubHits[k] = (m.StubHits.TryGetValue(k, out int n) ? n : 0) + 1;
    }
}
