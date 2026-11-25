// re-c Windows host — keyboard (+ XInput controller, T5) -> NES button byte (A b7, B b6, Select b5, Start b4, Up b3,
// Down b2, Left b1, Right b0; the order ReadPadRaw 31:FF28 shifts them in). = web/src/input.js; keys per
// ../web/index.html (architecture_win.md §1 rule 8).
namespace ReC.Win;

internal static class Btn
{
    public const int A = 0x80, B = 0x40, Select = 0x20, Start = 0x10, Up = 0x08, Down = 0x04, Left = 0x02, Right = 0x01;
}

internal sealed class Input
{
    // = input.js KEYMAP (KeyboardEvent.code -> Win32 virtual key; Right Shift is told apart by its scan code)
    private static readonly Dictionary<int, int> KeyMap = new()
    {
        ['X'] = Btn.A, ['K'] = Btn.A,
        ['Z'] = Btn.B, ['J'] = Btn.B,
        [Win32.VK_RSHIFT] = Btn.Select, [Win32.VK_TAB] = Btn.Select,
        [Win32.VK_RETURN] = Btn.Start,
        [Win32.VK_UP] = Btn.Up, ['W'] = Btn.Up,
        [Win32.VK_DOWN] = Btn.Down, ['S'] = Btn.Down,
        [Win32.VK_LEFT] = Btn.Left, ['A'] = Btn.Left,
        [Win32.VK_RIGHT] = Btn.Right, ['D'] = Btn.Right,
    };

    /// <summary>One key per button, for the `--keys` script: it posts these as WM_KEYDOWN / WM_KEYUP so a scripted
    /// run takes the same window-message path as a real keyboard (Window.cs).</summary>
    public static readonly (int Bit, int Vk)[] ScriptKeys =
    [
        (Btn.A, 'X'), (Btn.B, 'Z'), (Btn.Select, Win32.VK_TAB), (Btn.Start, Win32.VK_RETURN),
        (Btn.Up, Win32.VK_UP), (Btn.Down, Win32.VK_DOWN), (Btn.Left, Win32.VK_LEFT), (Btn.Right, Win32.VK_RIGHT),
    ];

    private int _keys;

    // js: input.js Input
    // js: input.js constructor (Input)
    /// <summary>The JS constructor registers keydown / keyup / blur listeners; here Window.cs's WndProc calls
    /// <see cref="KeyDown"/> / <see cref="KeyUp"/> / <see cref="Blur"/>.</summary>
    public Input() { _keys = 0; }

    /// <summary>VK of a WM_KEYDOWN / WM_KEYUP: VK_SHIFT becomes VK_RSHIFT for the right key (scan code $36).</summary>
    public static int Vk(int wParam, long lParam) =>
        wParam == Win32.VK_SHIFT && ((lParam >> 16) & 0xFF) == 0x36 ? Win32.VK_RSHIFT : wParam;

    public void KeyDown(int vk) { if (KeyMap.TryGetValue(vk, out int b)) _keys |= b; }
    public void KeyUp(int vk) { if (KeyMap.TryGetValue(vk, out int b)) _keys &= ~b; }

    /// <summary>= the 'blur' listener: focus lost releases every key.</summary>
    public void Blur() => _keys = 0;

    // js: input.js read
    /// <summary>button byte of the keyboard; Window.cs ORs in <see cref="Pad.Read"/> (the JS read() ORs the gamepads)</summary>
    public int Read()
    {
        int b = _keys;
        // opposite directions are passed through: PadDirTable maps U+D / L+R to $FF like the ROM does
        return b & 0xFF;
    }
}

/// <summary>XInput controller (= input.js's navigator.getGamepads part). xinput1_4.dll, else xinput9_1_0.dll, bound at
/// run time (NativeLibrary) so a PC without either still starts; no controller = keyboard only. While no controller is
/// connected the four slots are polled every 2 s (XInputGetState on an empty slot is slow on real Windows).</summary>
internal sealed unsafe class Pad
{
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint Packet;
        public ushort Buttons;
        public byte LeftTrigger, RightTrigger;
        public short ThumbLX, ThumbLY, ThumbRX, ThumbRY;
    }

    private const uint ErrorSuccess = 0;
    private const double AbsentPollMs = 2000;
    private const short AxisThreshold = 16384;    // = input.js's 0.5 of the axis range

    // = input.js PAD_BUTTONS (standard mapping: bottom / right face = A, left / top face = B, back = select)
    private static readonly (ushort Mask, int Bit)[] Map =
    [
        (0x1000, Btn.A), (0x2000, Btn.A), (0x4000, Btn.B), (0x8000, Btn.B), (0x0020, Btn.Select), (0x0010, Btn.Start),
        (0x0001, Btn.Up), (0x0002, Btn.Down), (0x0004, Btn.Left), (0x0008, Btn.Right),
    ];

    private readonly delegate* unmanaged<uint, XInputState*, uint> _getState;
    private int _slot = -1;                  // connected controller slot, -1 = none
    private double _nextPoll;                // ms (Window clock) of the next scan while none is connected

    /// <summary>DLL in use, or "none".</summary>
    public string Api { get; } = "none";

    public Pad()
    {
        foreach (string dll in new[] { "xinput1_4.dll", "xinput9_1_0.dll" })
        {
            if (!OperatingSystem.IsWindows()) break;
            if (!System.Runtime.InteropServices.NativeLibrary.TryLoad(dll, out IntPtr lib)) continue;
            if (!System.Runtime.InteropServices.NativeLibrary.TryGetExport(lib, "XInputGetState", out IntPtr fn)) continue;
            _getState = (delegate* unmanaged<uint, XInputState*, uint>)fn;
            Api = dll;
            break;
        }
    }

    /// <summary>NES button byte of the connected controller (0 if none); `nowMs` paces the absent-controller scan.</summary>
    /// <summary>T46: the controller's raw XInput buttons of the last Read (0 = none): shoulders for fast forward / pause</summary>
    public ushort Raw;

    public int Read(double nowMs)
    {
        if (_getState == null) return 0;
        Raw = 0;
        XInputState s;
        if (_slot < 0)
        {
            if (nowMs < _nextPoll) return 0;
            _nextPoll = nowMs + AbsentPollMs;
            for (uint i = 0; i < 4 && _slot < 0; i++)
                if (_getState(i, &s) == ErrorSuccess) _slot = (int)i;
            if (_slot < 0) return 0;
        }
        if (_getState((uint)_slot, &s) != ErrorSuccess)
        {
            _slot = -1;                          // unplugged: back to the 2 s scan
            _nextPoll = nowMs + AbsentPollMs;
            return 0;
        }
        Raw = s.Buttons;
        return Buttons(s.Buttons, s.ThumbLX, s.ThumbLY);
    }

    /// <summary>XInput buttons + left stick -> NES byte (stick Y is positive = up in XInput).</summary>
    public static int Buttons(ushort buttons, short lx, short ly)
    {
        int b = 0;
        foreach (var (mask, bit) in Map) if ((buttons & mask) != 0) b |= bit;
        if (lx < -AxisThreshold) b |= Btn.Left; else if (lx > AxisThreshold) b |= Btn.Right;
        if (ly > AxisThreshold) b |= Btn.Up; else if (ly < -AxisThreshold) b |= Btn.Down;
        return b;
    }
}
