// re-c C# port — keyboard + SDL game controller -> NES button byte (A b7, B b6, Select b5, Start b4,
// Up b3, Down b2, Left b1, Right b0; the order ReadPadRaw 31:FF28 shifts them in). = web/src/input.js.
namespace ReC.Sdl;

internal static class Btn
{
    public const int A = 0x80, B = 0x40, Select = 0x20, Start = 0x10, Up = 0x08, Down = 0x04, Left = 0x02, Right = 0x01;
}

internal sealed class Input
{
    // = input.js KEYMAP (KeyboardEvent.code -> SDL scancode; Enter also on the keypad)
    private static readonly Dictionary<int, int> KeyMap = new()
    {
        [Sdl.ScX] = Btn.A, [Sdl.ScK] = Btn.A,
        [Sdl.ScZ] = Btn.B, [Sdl.ScJ] = Btn.B,
        [Sdl.ScRShift] = Btn.Select, [Sdl.ScTab] = Btn.Select,
        [Sdl.ScReturn] = Btn.Start, [Sdl.ScKpEnter] = Btn.Start,
        [Sdl.ScUp] = Btn.Up, [Sdl.ScW] = Btn.Up,
        [Sdl.ScDown] = Btn.Down, [Sdl.ScS] = Btn.Down,
        [Sdl.ScLeft] = Btn.Left, [Sdl.ScA] = Btn.Left,
        [Sdl.ScRight] = Btn.Right, [Sdl.ScD] = Btn.Right,
    };

    // = input.js PAD_BUTTONS (standard mapping: bottom / right face = A, left / top face = B, back = select)
    private static readonly (int Button, int Bit)[] PadButtons =
    [
        (Sdl.CbA, Btn.A), (Sdl.CbB, Btn.A), (Sdl.CbX, Btn.B), (Sdl.CbY, Btn.B), (Sdl.CbBack, Btn.Select), (Sdl.CbStart, Btn.Start),
        (Sdl.CbDpadUp, Btn.Up), (Sdl.CbDpadDown, Btn.Down), (Sdl.CbDpadLeft, Btn.Left), (Sdl.CbDpadRight, Btn.Right),
    ];

    private const short AxisThreshold = 16384;     // = 0.5 of the axis range

    private int _keys;
    private readonly List<IntPtr> _pads = new();

    // js: input.js Input
    // js: input.js constructor (Input)
    /// <summary>The JS constructor registers keydown / keyup / blur listeners; here Program's event loop passes each
    /// SDL event to <see cref="HandleEvent"/>. Controllers already plugged in arrive as CONTROLLERDEVICEADDED events.</summary>
    public Input() { _keys = 0; }

    /// <summary>The bodies of the JS listeners (keydown / keyup / blur) plus controller hot-plug.</summary>
    public void HandleEvent(in Sdl.Event e)
    {
        switch (e.Type)
        {
            case Sdl.EvKeyDown:
                if (KeyMap.TryGetValue(e.Scancode, out int b)) _keys |= b;
                break;
            case Sdl.EvKeyUp:
                if (KeyMap.TryGetValue(e.Scancode, out int u)) _keys &= ~u;
                break;
            case Sdl.EvWindow:
                if (e.WindowEvent == Sdl.WinFocusLost) _keys = 0;     // = 'blur'
                break;
            case Sdl.EvControllerAdded:
                if (Sdl.IsGameController(e.Which) != 0)
                {
                    IntPtr gc = Sdl.GameControllerOpen(e.Which);
                    if (gc != IntPtr.Zero && !_pads.Contains(gc)) _pads.Add(gc);
                }
                break;
            case Sdl.EvControllerRemoved:
                for (int i = _pads.Count - 1; i >= 0; i--)
                    if (Sdl.GameControllerGetAttached(_pads[i]) == 0) { Sdl.GameControllerClose(_pads[i]); _pads.RemoveAt(i); }
                break;
        }
    }

    // js: input.js read
    /// <summary>button byte for pad 1 (keyboard OR first connected controller)</summary>
    public int Read()
    {
        int b = _keys;
        foreach (IntPtr gp in _pads)
        {
            if (Sdl.GameControllerGetAttached(gp) == 0) continue;
            foreach (var (i, bit) in PadButtons) if (Sdl.GameControllerGetButton(gp, i) != 0) b |= bit;
            short ax = Sdl.GameControllerGetAxis(gp, Sdl.AxisLeftX), ay = Sdl.GameControllerGetAxis(gp, Sdl.AxisLeftY);
            if (ax < -AxisThreshold) b |= Btn.Left; else if (ax > AxisThreshold) b |= Btn.Right;
            if (ay < -AxisThreshold) b |= Btn.Up; else if (ay > AxisThreshold) b |= Btn.Down;
            break;
        }
        // opposite directions are passed through: PadDirTable maps U+D / L+R to $FF like the ROM does
        return b & 0xFF;
    }

    public void Close()
    {
        foreach (IntPtr gc in _pads) Sdl.GameControllerClose(gc);
        _pads.Clear();
    }
}
