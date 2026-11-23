// re-c Windows host — the playable window (= web/src/main.js): user32 window + message pump, StretchDIBits blit of the
// 256x240 frame at the largest integer scale (black letterbox), fixed-step 60.0988 Hz loop (Stopwatch = QPC, with
// timeBeginPeriod(1) sleeps), keyboard (Pad.cs), battery WRAM file (Save.cs). architecture_win.md §1 rules 2 / 5 / 8.
//
// Keys: arrows / WASD, X / K = A, Z / J = B, Enter = Start, Right Shift / Tab = Select, P pause, M mute, Esc quit,
// F11 / Alt+Enter fullscreen, 1-4 window scale.
// Test hooks: --frames N quits after N frames (exit 0); --turbo runs one frame per loop pass without waiting;
// --keys SPEC.. posts WM_KEYDOWN / WM_KEYUP to the window before each frame (same message path as the keyboard);
// --compare F.csv / --dump-screen F.png as in the headless mode.
using System.Diagnostics;
using System.Runtime.InteropServices;
using ReC.Game;
using ReC.Util;

namespace ReC.Win;

internal sealed class WindowOptions
{
    public int Scale = 3, Frames = -1, Frame0 = 214;
    public bool Frame0Given, Mute, NoAudio, Turbo, Quiet;
    public string? SavePath, DataDir, StatePath, Screen, Compare;
    public readonly List<string> KeySpecs = new();

    /// <summary>Window-mode options (ArgumentException / FormatException on bad input; `--help` -> null).</summary>
    public static WindowOptions? Parse(IReadOnlyList<string> args)
    {
        var o = new WindowOptions();
        for (int i = 0; i < args.Count; i++)
        {
            string k = args[i];
            string V() => i + 1 < args.Count ? args[++i] : throw new ArgumentException($"{k} needs a value");
            switch (k)
            {
                case "--window": break;
                case "--scale": o.Scale = int.Parse(V()); break;
                case "--save": o.SavePath = V(); break;
                case "--data": o.DataDir = V(); break;
                case "--load-state": o.StatePath = V(); break;
                case "--mute": o.Mute = true; break;
                case "--no-audio": o.NoAudio = true; break;
                case "--frames": o.Frames = int.Parse(V()); break;
                case "--frame0": o.Frame0 = int.Parse(V()); o.Frame0Given = true; break;
                case "--turbo": o.Turbo = true; break;
                case "--dump-screen": o.Screen = V(); break;
                case "--compare": o.Compare = V(); break;
                case "--keys": while (i + 1 < args.Count && !args[i + 1].StartsWith("--")) o.KeySpecs.Add(args[++i]); break;
                case "--quiet": o.Quiet = true; break;
                case "-h": case "--help": return null;
                default: throw new ArgumentException($"unknown option {k} (--help)");
            }
        }
        return o;
    }
}

internal static unsafe class Window
{
    private const string Title = "re-c";
    private const string ClassName = "ReCWindow";

    private static IntPtr _hwnd, _hdc;
    private static Input _input = new();
    private static AudioOut _audio = null!;
    private static Pad _pad = null!;
    private static bool _quit, _paused, _hidden, _fullscreen, _scripted;
    private static Win32.WINDOWPLACEMENT _placement;
    private static bool _muted;
    private static double _last, _acc;
    private static readonly Stopwatch Clock = new();
    private static readonly uint[] Px = new uint[256 * 240];
    private static Win32.BITMAPINFOHEADER _bmi;
    private static readonly uint Style = Win32.WS_OVERLAPPEDWINDOW;

    // js: main.js status
    /// <summary>= the page's status line: shown in the window title (paused / muted).</summary>
    private static void Status(string s)
    {
        if (_hwnd != IntPtr.Zero) Win32.SetWindowTextW(_hwnd, s.Length > 0 ? $"{Title} — {s}" : Title);
    }

    // js: main.js start
    public static int Run(WindowOptions o)
    {
        var data = Data.LoadDefault(o.DataDir);
        var m = new Machine(data, s => { if (!o.Quiet) Console.Error.WriteLine($"[re-c] {s}"); });
        // battery WRAM survives power-off: restore it before Reset, as the cartridge would (Save.cs)
        string savePath = o.SavePath ?? Save.DefaultPath();
        if (Save.LoadWram(savePath, m.Mem.Wram) && !o.Quiet) Console.Error.WriteLine($"[re-c] WRAM restored from {savePath}");
        m.Reset();
        // a loaded state brings its own WRAM: it only goes to a save file named with --save, never over re-c.sav
        bool saving = o.StatePath == null || o.SavePath != null;
        int frame0 = o.Frame0;
        if (o.StatePath != null)
        {
            var st = StateFile.Load(o.StatePath);
            m.LoadState(st);
            if (!o.Frame0Given) frame0 = st.Frame0 ?? st.Frame;
        }
        var saver = saving ? new WramSaver(savePath, m.Mem.Wram) : null;
        var keys = Keys.Parse(o.KeySpecs);
        _scripted = o.KeySpecs.Count > 0;
        var rows = o.Compare != null ? new List<string> { string.Join(',', Csv.CompareColumns()) } : null;
        _muted = o.Mute;
        _audio = new AudioOut(m);
        _pad = new Pad();

        // NES colour index -> 32-bit BI_RGB pixel (B, G, R, x in memory = 0x00RRGGBB): palettes.json nesRgb as is
        uint[] lut = data.NesRgb.Select(c => (uint)c & 0xFFFFFF).ToArray();
        _bmi = new Win32.BITMAPINFOHEADER
        {
            Size = (uint)sizeof(Win32.BITMAPINFOHEADER), Width = 256, Height = -240, Planes = 1, BitCount = 32,
        };

        Win32.timeBeginPeriod(1);
        int ranTotal = 0;
        try
        {
            if (!CreateMainWindow(o.Scale)) { Console.Error.WriteLine("ReC.exe: could not create the window"); return 1; }
            // no autoplay policy on the desktop: audio starts right away (JS: on the first key / click); M = mute
            bool audioOn = !o.NoAudio && _audio.Resume();
            _audio.SetMuted(_muted);
            if (_muted) Status("muted (M)");
            if (!o.Quiet)
                Console.Error.WriteLine($"[re-c] audio={(o.NoAudio ? "off" : audioOn ? $"waveOut {AudioOut.Rate} Hz" : "no device (silent)")} " +
                                        $"pad={_pad.Api} save={(saving ? savePath : "off (--load-state without --save)")}");

            // js: main.js blit
            void Blit(byte[] fb)
            {
                for (int i = 0; i < fb.Length; i++) Px[i] = lut[fb[i] & 0x3F];
                Present();
            }

            double step = 1000.0 / Machine.FrameHz;
            Clock.Restart();
            _last = Clock.Elapsed.TotalMilliseconds;
            _acc = 0;
            int scriptMask = 0;

            // js: main.js tick
            // one pass of the host loop: run the frames due since the last pass, blit (+ audio, T5) if any ran
            void Tick(double now)
            {
                bool ran = false;
                if (!_paused && !_hidden)
                {
                    _acc += Math.Min(now - _last, 250);       // never run more than ~15 frames to catch up
                    if (o.Turbo) _acc = Math.Max(_acc, step);
                    while (_acc >= step && !_quit)
                    {
                        int f = frame0 + m.Frame;
                        if (_scripted)
                        {
                            int want = keys.TryGetValue(f, out int b) ? b : 0;
                            PostScriptKeys(scriptMask, want);
                            scriptMask = want;
                            Pump();
                        }
                        m.RunFrame(_input.Read() | (_scripted ? 0 : _pad.Read(now))); _acc -= step; ran = true; ranTotal++;
                        rows?.Add(string.Join(',', Csv.CompareRow(m, f)));
                        if (saver != null && m.Frame % 300 == 0) saver.Poll();     // ~5 s
                        if (o.Frames >= 0 && ranTotal >= o.Frames) { _quit = true; break; }
                        if (o.Turbo) _acc = 0;
                    }
                    if (ran) { Blit(m.Ppu.Fb); _audio.Flush(); }
                }
                _last = now;
            }

            Status("");
            Blit(m.Ppu.Fb);
            if (o.Frames == 0) _quit = true;
            while (!_quit)
            {
                Pump();
                if (_quit) break;
                Tick(Clock.Elapsed.TotalMilliseconds);
                if (!o.Turbo && !_quit)
                {
                    double wait = step - _acc - 1;            // sleep until near the next frame (1 ms timer)
                    Thread.Sleep(wait >= 1 ? (int)Math.Min(wait, step) : (_paused || _hidden ? 10 : 0));
                }
            }

            if (rows != null) File.WriteAllText(o.Compare!, string.Join('\n', rows) + "\n");
            if (o.Screen != null) Png.WriteFb(o.Screen, m.Ppu.Fb, data.NesRgb, 1);
            if (!o.Quiet) Console.Error.WriteLine($"[re-c] frames={ranTotal} main_state={m.Rd(0x40)} sub_state={m.Rd(0x41)} " +
                                                  $"audio_underruns={_audio.Underruns} audio_dropped={_audio.Dropped}");
            return 0;
        }
        finally
        {
            _audio.Close();
            if (saving) Save.SaveWram(savePath, m.Mem.Wram);   // always, on leaving (= 'pagehide')
            if (_hwnd != IntPtr.Zero) { Win32.DestroyWindow(_hwnd); _hwnd = IntPtr.Zero; }
            Win32.timeEndPeriod(1);
        }
    }

    /// <summary>--keys: WM_KEYDOWN / WM_KEYUP for every button whose script state changed (Pad.cs ScriptKeys).</summary>
    private static void PostScriptKeys(int was, int want)
    {
        foreach (var (bit, vk) in Input.ScriptKeys)
        {
            if (((was ^ want) & bit) == 0) continue;
            Win32.PostMessageW(_hwnd, (want & bit) != 0 ? Win32.WM_KEYDOWN : Win32.WM_KEYUP, vk,
                (want & bit) != 0 ? 1 : unchecked((IntPtr)(long)0xC0000001));
        }
    }

    private static void Pump()
    {
        while (Win32.PeekMessageW(out var msg, IntPtr.Zero, 0, 0, Win32.PM_REMOVE))
        {
            if (msg.Message == Win32.WM_QUIT) { _quit = true; continue; }
            Win32.TranslateMessage(msg);
            Win32.DispatchMessageW(msg);
        }
    }

    private static bool CreateMainWindow(int scale)
    {
        IntPtr inst = Win32.GetModuleHandleW(null);
        fixed (char* cls = ClassName)
        {
            var wc = new Win32.WNDCLASSEXW
            {
                CbSize = (uint)sizeof(Win32.WNDCLASSEXW),
                Style = Win32.CS_HREDRAW | Win32.CS_VREDRAW | Win32.CS_OWNDC,
                WndProc = &WndProc,
                Instance = inst,
                // app.ico (ApplicationIcon): the compiler's Win32 icon group id 32512; 0 = Windows' default icon
                Icon = Win32.LoadImageW(inst, 32512, 1 /* IMAGE_ICON */, 0, 0, 0x40 /* LR_DEFAULTSIZE */),
                IconSm = Win32.LoadImageW(inst, 32512, 1, 16, 16, 0),
                Cursor = Win32.LoadCursorW(IntPtr.Zero, Win32.IDC_ARROW),
                Background = IntPtr.Zero,
                ClassName = cls,
            };
            if (Win32.RegisterClassExW(wc) == 0) return false;
        }
        scale = Math.Clamp(scale, 1, 8);
        var (w, h) = OuterSize(scale);
        _hwnd = Win32.CreateWindowExW(0, ClassName, Title, Style, Win32.CW_USEDEFAULT, Win32.CW_USEDEFAULT, w, h,
            IntPtr.Zero, IntPtr.Zero, inst, IntPtr.Zero);
        if (_hwnd == IntPtr.Zero) return false;
        // shrink the default scale until the window fits the monitor's work area
        var mi = new Win32.MONITORINFO { CbSize = (uint)sizeof(Win32.MONITORINFO) };
        if (Win32.GetMonitorInfoW(Win32.MonitorFromWindow(_hwnd, Win32.MONITOR_DEFAULTTONEAREST), ref mi))
        {
            while (scale > 1 && (OuterSize(scale).W > mi.Work.Right - mi.Work.Left || OuterSize(scale).H > mi.Work.Bottom - mi.Work.Top))
                scale--;
            SetScale(scale);
        }
        _hdc = Win32.GetDC(_hwnd);
        Win32.SetStretchBltMode(_hdc, Win32.COLORONCOLOR);
        Win32.ShowWindow(_hwnd, Win32.SW_SHOW);
        return true;
    }

    private static (int W, int H) OuterSize(int scale)
    {
        var r = new Win32.RECT { Right = 256 * scale, Bottom = 240 * scale };
        Win32.AdjustWindowRectEx(ref r, Style, false, 0);
        return (r.Right - r.Left, r.Bottom - r.Top);
    }

    private static void SetScale(int scale)
    {
        if (_fullscreen) return;
        var (w, h) = OuterSize(scale);
        Win32.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, w, h, Win32.SWP_NOMOVE | Win32.SWP_NOZORDER | Win32.SWP_NOOWNERZORDER);
    }

    /// <summary>F11 / Alt+Enter: borderless window over the whole monitor, and back to the saved placement.</summary>
    private static void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            var mi = new Win32.MONITORINFO { CbSize = (uint)sizeof(Win32.MONITORINFO) };
            _placement = new Win32.WINDOWPLACEMENT { Length = (uint)sizeof(Win32.WINDOWPLACEMENT) };
            if (!Win32.GetWindowPlacement(_hwnd, ref _placement) ||
                !Win32.GetMonitorInfoW(Win32.MonitorFromWindow(_hwnd, Win32.MONITOR_DEFAULTTONEAREST), ref mi)) return;
            Win32.SetWindowLongPtrW(_hwnd, Win32.GWL_STYLE, unchecked((IntPtr)(long)(Win32.WS_POPUP | Win32.WS_VISIBLE)));
            Win32.SetWindowPos(_hwnd, IntPtr.Zero, mi.Monitor.Left, mi.Monitor.Top, mi.Monitor.Right - mi.Monitor.Left,
                mi.Monitor.Bottom - mi.Monitor.Top, Win32.SWP_NOOWNERZORDER | Win32.SWP_FRAMECHANGED);
            _fullscreen = true;
        }
        else
        {
            Win32.SetWindowLongPtrW(_hwnd, Win32.GWL_STYLE, (IntPtr)(Style | Win32.WS_VISIBLE));
            Win32.SetWindowPlacement(_hwnd, _placement);
            Win32.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | 0x1 /* NOSIZE */ | Win32.SWP_NOZORDER | Win32.SWP_NOOWNERZORDER | Win32.SWP_FRAMECHANGED);
            _fullscreen = false;
        }
    }

    // js: main.js fit
    /// <summary>largest integer scale that fits the client area, centred; the rest is painted black</summary>
    public static (int X, int Y, int W, int H) Fit(int cw, int ch)
    {
        int s = Math.Max(1, Math.Min(cw / 256, ch / 240));
        return ((cw - 256 * s) / 2, (ch - 240 * s) / 2, 256 * s, 240 * s);
    }

    private static void Present() => Paint(_hdc);

    private static void Paint(IntPtr hdc)
    {
        if (hdc == IntPtr.Zero || !Win32.GetClientRect(_hwnd, out var rc)) return;
        int cw = rc.Right, ch = rc.Bottom;
        if (cw <= 0 || ch <= 0) return;
        var (x, y, w, h) = Fit(cw, ch);
        IntPtr black = Win32.GetStockObject(Win32.BLACK_BRUSH);
        if (y > 0) Win32.FillRect(hdc, new Win32.RECT { Right = cw, Bottom = y }, black);
        if (y + h < ch) Win32.FillRect(hdc, new Win32.RECT { Top = y + h, Right = cw, Bottom = ch }, black);
        if (x > 0) Win32.FillRect(hdc, new Win32.RECT { Top = y, Right = x, Bottom = y + h }, black);
        if (x + w < cw) Win32.FillRect(hdc, new Win32.RECT { Left = x + w, Top = y, Right = cw, Bottom = y + h }, black);
        fixed (uint* p = Px)
            Win32.StretchDIBits(hdc, x, y, w, h, 0, 0, 256, 240, p, _bmi, Win32.DIB_RGB_COLORS, Win32.SRCCOPY);
    }

    private static void KeyCommand(int vk)
    {
        switch (vk)
        {
            case 'P':
                _paused = !_paused;
                _last = Clock.Elapsed.TotalMilliseconds; _acc = 0;
                Status(_paused ? "paused (P)" : _muted ? "muted (M)" : "");
                break;
            case 'M':
                _muted = !_muted;
                _audio.SetMuted(_muted);
                Status(_paused ? "paused (P)" : _muted ? "muted (M)" : "");
                break;
            case Win32.VK_ESCAPE: Win32.DestroyWindow(_hwnd); break;
            case Win32.VK_F11: ToggleFullscreen(); break;
            case >= '1' and <= '4': SetScale(vk - '0'); break;
        }
    }

    public static bool Muted => _muted;

    [UnmanagedCallersOnly]
    private static IntPtr WndProc(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        try
        {
            switch (msg)
            {
                case Win32.WM_KEYDOWN:
                {
                    int vk = Input.Vk((int)wParam, (long)lParam);
                    bool repeat = ((long)lParam & 0x40000000) != 0;
                    _input.KeyDown(vk);
                    if (!repeat) KeyCommand(vk);
                    return IntPtr.Zero;
                }
                case Win32.WM_KEYUP:
                    _input.KeyUp(Input.Vk((int)wParam, (long)lParam));
                    return IntPtr.Zero;
                case Win32.WM_SYSKEYDOWN:
                    if ((int)wParam == Win32.VK_RETURN)
                    {
                        if (((long)lParam & 0x40000000) == 0) ToggleFullscreen();
                        return IntPtr.Zero;
                    }
                    break;
                case Win32.WM_SYSCHAR:
                    if ((int)wParam == '\r') return IntPtr.Zero;     // no beep for Alt+Enter
                    break;
                case Win32.WM_KILLFOCUS:
                    if (!_scripted) _input.Blur();                 // = 'blur'; a --keys run ignores focus changes
                    break;
                case Win32.WM_SIZE:
                    // = visibilitychange: a minimised window pauses; the clock restarts on return
                    _hidden = (int)wParam == Win32.SIZE_MINIMIZED;
                    _last = Clock.Elapsed.TotalMilliseconds; _acc = 0;
                    break;
                case Win32.WM_GETMINMAXINFO:
                {
                    var (w, h) = OuterSize(1);
                    var mmi = (Win32.MINMAXINFO*)lParam;
                    mmi->MinTrackSize = new Win32.POINT { X = w, Y = h };
                    return IntPtr.Zero;
                }
                case Win32.WM_ERASEBKGND:
                    return 1;
                case Win32.WM_PAINT:
                {
                    IntPtr dc = Win32.BeginPaint(hwnd, out var ps);
                    if (_hwnd != IntPtr.Zero) Paint(dc);
                    Win32.EndPaint(hwnd, ps);
                    return IntPtr.Zero;
                }
                case Win32.WM_DESTROY:
                    _quit = true;
                    Win32.PostQuitMessage(0);
                    return IntPtr.Zero;
            }
        }
        catch (Exception e)
        {
            // never let an exception unwind through the native frame
            Console.Error.WriteLine($"ReC.exe: {e}");
            _quit = true;
        }
        return Win32.DefWindowProcW(hwnd, msg, wParam, lParam);
    }
}
