// re-c Windows host — the menu bar (Game, Video) + the same menus on right-click, and what the Video choices do:
// fullscreen (borderless at the desktop resolution, or an exclusive display mode = "Fullscreen resolution"), window size
// (1x-6x or a preset client size), aspect ratio / widescreen, integer scaling, overscan crop, smooth filter, menu bar.
// Settings: Video.cs (VideoSettings, saved to %APPDATA%\re-c\video.ini on every change).
//
// The menu runs a modal loop: the game stops while a menu is open (the frame clock restarts on WM_EXITMENULOOP).
// In fullscreen the menu bar is hidden; right-click opens the menus anywhere.
// Test hooks: --menu CMD,.. posts the same WM_COMMANDs after the window opens; --dump-window F.png reads the client
// area back at the end (tools/win_video_check.py).
using ReC.Util;

namespace ReC.Win;

internal static unsafe partial class Window
{
    // command ids
    private const int IdPause = 100, IdMute = 101, IdExit = 102;
    private const int IdFullscreen = 200, IdMenuBar = 201, IdInteger = 202, IdCrop = 203, IdSmooth = 204, IdReset = 205;
    private const int IdScale0 = 210;            // + 1..6
    private const int IdPreset0 = 220;           // + index in VideoSettings.Presets
    private const int IdAspect0 = 240;           // + (int)Aspect
    private const int IdFsDesktop = 300, IdFsMode0 = 301;   // + index in _modes

    private static VideoSettings _v = new();
    private static string? _videoPath;
    private static readonly List<IntPtr> _menus = new();   // every menu holding command items (bar + popup copies)
    private static IntPtr _menuBar, _popup;
    private static List<(int W, int H)> _modes = new();
    private static bool _exclusive, _altUsed;
    private static readonly char[] _device = new char[32];

    private static uint WindowStyle => _fullscreen ? Win32.WS_POPUP : Style;
    private static bool MenuShown => _v.MenuBar && !_fullscreen;

    /// <summary>--menu names -> command ids (also documents what each id does)</summary>
    public static int CommandId(string name)
    {
        string n = name.Trim().ToLowerInvariant();
        switch (n)
        {
            case "pause": return IdPause;
            case "mute": return IdMute;
            case "exit": return IdExit;
            case "fullscreen": return IdFullscreen;
            case "menubar": return IdMenuBar;
            case "integer": return IdInteger;
            case "crop": return IdCrop;
            case "smooth": return IdSmooth;
            case "reset": return IdReset;
            case "fsmode:desktop": return IdFsDesktop;
        }
        if (n.StartsWith("size:"))
        {
            string s = n[5..];
            if (s.EndsWith('x') && int.TryParse(s[..^1], out int k) && k is >= 1 and <= VideoSettings.MaxScale) return IdScale0 + k;
            if (VideoSettings.TryWxH(s, out int w, out int h))
            {
                int i = Array.IndexOf(VideoSettings.Presets, (w, h));
                if (i >= 0) return IdPreset0 + i;
            }
        }
        if (n.StartsWith("aspect:"))
            foreach (var a in VideoSettings.Aspects)
                if (a.Key == n[7..]) return IdAspect0 + (int)a.A;
        if (n.StartsWith("fsmode:") && VideoSettings.TryWxH(n[7..], out int fw, out int fh))
        {
            int i = _modes.IndexOf((fw, fh));
            // a mode this monitor does not list still goes through (ChangeDisplaySettings decides; fallback = borderless)
            return i >= 0 ? IdFsMode0 + i : -(fw << 16 | fh);
        }
        throw new ArgumentException($"unknown --menu command {name}");
    }

    private static void BuildMenus()
    {
        _modes = EnumModes();
        _menuBar = Win32.CreateMenu();
        Win32.AppendMenuW(_menuBar, Win32.MF_POPUP, (nuint)BuildGameMenu(), "&Game");
        Win32.AppendMenuW(_menuBar, Win32.MF_POPUP, (nuint)BuildVideoMenu(), "&Video");
        _popup = Win32.CreatePopupMenu();
        Win32.AppendMenuW(_popup, Win32.MF_POPUP, (nuint)BuildGameMenu(), "&Game");
        Win32.AppendMenuW(_popup, Win32.MF_POPUP, (nuint)BuildVideoMenu(), "&Video");
    }

    private static IntPtr NewMenu() { var m = Win32.CreatePopupMenu(); _menus.Add(m); return m; }

    private static IntPtr BuildGameMenu()
    {
        var m = NewMenu();
        Win32.AppendMenuW(m, 0, IdPause, "&Pause\tP");
        Win32.AppendMenuW(m, 0, IdMute, "&Mute\tM");
        Win32.AppendMenuW(m, Win32.MF_SEPARATOR, 0, null);
        Win32.AppendMenuW(m, 0, IdExit, "E&xit\tEsc");
        return m;
    }

    private static IntPtr BuildVideoMenu()
    {
        var m = NewMenu();
        Win32.AppendMenuW(m, 0, IdFullscreen, "&Fullscreen\tF11");
        var fs = NewMenu();
        Win32.AppendMenuW(fs, 0, IdFsDesktop, "&Desktop resolution (borderless)");
        if (_modes.Count > 0) Win32.AppendMenuW(fs, Win32.MF_SEPARATOR, 0, null);
        for (int i = 0; i < _modes.Count; i++)
            Win32.AppendMenuW(fs, 0, (nuint)(IdFsMode0 + i), $"{_modes[i].W} x {_modes[i].H}");
        Win32.AppendMenuW(m, Win32.MF_POPUP, (nuint)fs, "Fullscreen &resolution");
        Win32.AppendMenuW(m, Win32.MF_SEPARATOR, 0, null);

        var size = NewMenu();
        for (int s = 1; s <= VideoSettings.MaxScale; s++) Win32.AppendMenuW(size, 0, (nuint)(IdScale0 + s), $"&{s}x\t{s}");
        Win32.AppendMenuW(size, Win32.MF_SEPARATOR, 0, null);
        for (int i = 0; i < VideoSettings.Presets.Length; i++)
            Win32.AppendMenuW(size, 0, (nuint)(IdPreset0 + i), $"{VideoSettings.Presets[i].W} x {VideoSettings.Presets[i].H}");
        Win32.AppendMenuW(m, Win32.MF_POPUP, (nuint)size, "&Window size");

        var asp = NewMenu();
        foreach (var a in VideoSettings.Aspects) Win32.AppendMenuW(asp, 0, (nuint)(IdAspect0 + (int)a.A), a.Label);
        Win32.AppendMenuW(m, Win32.MF_POPUP, (nuint)asp, "&Aspect ratio");

        Win32.AppendMenuW(m, 0, IdInteger, "&Integer scaling (sharp)");
        Win32.AppendMenuW(m, 0, IdCrop, "Crop &overscan (8 lines top / bottom)");
        Win32.AppendMenuW(m, 0, IdSmooth, "S&mooth filter");
        Win32.AppendMenuW(m, Win32.MF_SEPARATOR, 0, null);
        Win32.AppendMenuW(m, 0, IdMenuBar, "Show menu &bar (right-click shows the menu)");
        Win32.AppendMenuW(m, 0, IdReset, "R&eset video settings");
        return m;
    }

    private static void Check(int id, bool on, bool radio = false)
    {
        var mii = new Win32.MENUITEMINFOW
        {
            CbSize = (uint)sizeof(Win32.MENUITEMINFOW), Mask = Win32.MIIM_STATE | Win32.MIIM_FTYPE,
            Type = Win32.MFT_STRING | (radio ? Win32.MFT_RADIOCHECK : 0), State = on ? Win32.MFS_CHECKED : 0,
        };
        foreach (var m in _menus) Win32.SetMenuItemInfoW(m, (uint)id, false, mii);
    }

    /// <summary>tick / radio marks = the current state</summary>
    private static void UpdateMenus()
    {
        if (_menus.Count == 0) return;
        Check(IdPause, _paused); Check(IdMute, _muted);
        Check(IdFullscreen, _fullscreen); Check(IdMenuBar, _v.MenuBar);
        Check(IdInteger, _v.Integer); Check(IdCrop, _v.CropOverscan); Check(IdSmooth, _v.Smooth);
        for (int s = 1; s <= VideoSettings.MaxScale; s++) Check(IdScale0 + s, _v.WindowW == 0 && _v.Scale == s, true);
        for (int i = 0; i < VideoSettings.Presets.Length; i++)
            Check(IdPreset0 + i, (_v.WindowW, _v.WindowH) == VideoSettings.Presets[i], true);
        foreach (var a in VideoSettings.Aspects) Check(IdAspect0 + (int)a.A, _v.Aspect == a.A, true);
        Check(IdFsDesktop, _v.FsW == 0, true);
        for (int i = 0; i < _modes.Count; i++) Check(IdFsMode0 + i, (_v.FsW, _v.FsH) == _modes[i], true);
    }

    private static void SettingsChanged(bool resize)
    {
        if (_videoPath != null) _v.TrySave(_videoPath);
        UpdateMenus();
        if (resize) ApplyWindowSize();
        Win32.InvalidateRect(_hwnd, IntPtr.Zero, false);
        Present();
    }

    /// <summary>WM_COMMAND from the menu bar, the right-click menu or --menu (negative id = --menu fsmode:WxH not listed)</summary>
    private static void Command(int id)
    {
        switch (id)
        {
            case IdPause: KeyCommand('P'); return;
            case IdMute: KeyCommand('M'); return;
            case IdExit: Win32.DestroyWindow(_hwnd); return;
            case IdFullscreen: ToggleFullscreen(); return;
            case IdMenuBar:
                _v.MenuBar = !_v.MenuBar;
                if (!_fullscreen) { Win32.SetMenu(_hwnd, _v.MenuBar ? _menuBar : IntPtr.Zero); }
                SettingsChanged(true);
                return;
            case IdInteger: _v.Integer = !_v.Integer; SettingsChanged(false); return;
            case IdCrop: _v.CropOverscan = !_v.CropOverscan; SettingsChanged(_v.WindowW == 0); return;
            case IdSmooth: _v.Smooth = !_v.Smooth; SettingsChanged(false); return;
            case IdReset:
            {
                bool wasFs = _fullscreen;
                if (wasFs) ToggleFullscreen();
                _v = new VideoSettings();
                Win32.SetMenu(_hwnd, _menuBar);
                SettingsChanged(true);
                return;
            }
            case IdFsDesktop: SetFsMode(0, 0); return;
        }
        if (id > IdScale0 && id <= IdScale0 + VideoSettings.MaxScale) { _v.SetScale(id - IdScale0); SettingsChanged(true); }
        else if (id >= IdPreset0 && id < IdPreset0 + VideoSettings.Presets.Length)
        {
            (_v.WindowW, _v.WindowH) = VideoSettings.Presets[id - IdPreset0];
            SettingsChanged(true);
        }
        else if (id >= IdAspect0 && id < IdAspect0 + VideoSettings.Aspects.Length)
        {
            _v.Aspect = (Aspect)(id - IdAspect0);
            SettingsChanged(_v.WindowW == 0);          // a 1x-6x window follows the new shape; a preset size stays
        }
        else if (id >= IdFsMode0 && id < IdFsMode0 + _modes.Count) SetFsMode(_modes[id - IdFsMode0].W, _modes[id - IdFsMode0].H);
        else if (id < 0) SetFsMode(-id >> 16, -id & 0xFFFF);
    }

    private static void SetFsMode(int w, int h)
    {
        (_v.FsW, _v.FsH) = (w, h);
        if (_fullscreen) { ToggleFullscreen(); ToggleFullscreen(); }     // leave + enter at the new resolution
        SettingsChanged(false);
    }

    private static void ContextMenu()
    {
        if (!Win32.GetCursorPos(out var p)) return;
        Win32.SetForegroundWindow(_hwnd);
        int id = Win32.TrackPopupMenu(_popup, Win32.TPM_RIGHTBUTTON | Win32.TPM_RETURNCMD, p.X, p.Y, 0, _hwnd, IntPtr.Zero);
        if (id != 0) Command(id);
    }

    private static (int W, int H) OuterSize(int cw, int ch)
    {
        var r = new Win32.RECT { Right = cw, Bottom = ch };
        Win32.AdjustWindowRectEx(ref r, Style, MenuShown, 0);
        return (r.Right - r.Left, r.Bottom - r.Top);
    }

    /// <summary>size the (windowed) client area to the setting; a maximised window is restored first</summary>
    private static void ApplyWindowSize()
    {
        if (_fullscreen || _hwnd == IntPtr.Zero) return;
        if (Win32.IsZoomed(_hwnd)) Win32.ShowWindow(_hwnd, Win32.SW_RESTORE);
        var (cw, ch) = _v.ClientSize();
        var (w, h) = OuterSize(cw, ch);
        const uint flags = Win32.SWP_NOMOVE | Win32.SWP_NOZORDER | Win32.SWP_NOOWNERZORDER;
        Win32.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, w, h, flags);
        // a narrow window wraps the menu bar onto two lines: grow by what AdjustWindowRectEx did not count
        if (Win32.GetClientRect(_hwnd, out var rc) && rc.Bottom != ch && rc.Bottom > 0)
            Win32.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, w, h + ch - rc.Bottom, flags);
    }

    // ---- exclusive fullscreen (a display mode change on the window's monitor, undone on leaving / losing focus / exit)
    private static List<(int W, int H)> EnumModes()
    {
        var all = new List<(int, int, int)>();
        if (!MonitorDevice()) return new();
        var dm = new Win32.DEVMODEW { Size = (ushort)sizeof(Win32.DEVMODEW) };
        fixed (char* d = _device)
            for (int i = 0; i < 1000 && Win32.EnumDisplaySettingsW(d, i, ref dm); i++)
                all.Add(((int)dm.PelsWidth, (int)dm.PelsHeight, (int)dm.BitsPerPel));
        return VideoSettings.ModeList(all);
    }

    private static bool MonitorDevice()
    {
        var mi = new Win32.MONITORINFOEXW { CbSize = (uint)sizeof(Win32.MONITORINFOEXW) };
        if (!Win32.GetMonitorInfoExW(Win32.MonitorFromWindow(_hwnd, Win32.MONITOR_DEFAULTTONEAREST), ref mi)) return false;
        for (int i = 0; i < 32; i++) _device[i] = mi.Device[i];
        return true;
    }

    private static bool SetDisplayMode(int w, int h)
    {
        var dm = new Win32.DEVMODEW
        {
            Size = (ushort)sizeof(Win32.DEVMODEW), Fields = Win32.DM_PELSWIDTH | Win32.DM_PELSHEIGHT | Win32.DM_BITSPERPEL,
            PelsWidth = (uint)w, PelsHeight = (uint)h, BitsPerPel = 32,
        };
        fixed (char* d = _device)
            return Win32.ChangeDisplaySettingsExW(d, &dm, IntPtr.Zero, Win32.CDS_FULLSCREEN, IntPtr.Zero) == Win32.DISP_CHANGE_SUCCESSFUL;
    }

    private static void RestoreDisplayMode()
    {
        if (!_exclusive) return;
        fixed (char* d = _device) Win32.ChangeDisplaySettingsExW(d, null, IntPtr.Zero, 0, IntPtr.Zero);
        _exclusive = false;
    }

    /// <summary>borderless popup over the whole monitor (after the mode change, when exclusive)</summary>
    private static void CoverMonitor()
    {
        var mi = new Win32.MONITORINFO { CbSize = (uint)sizeof(Win32.MONITORINFO) };
        if (!Win32.GetMonitorInfoW(Win32.MonitorFromWindow(_hwnd, Win32.MONITOR_DEFAULTTONEAREST), ref mi)) return;
        Win32.SetWindowPos(_hwnd, IntPtr.Zero, mi.Monitor.Left, mi.Monitor.Top, mi.Monitor.Right - mi.Monitor.Left,
            mi.Monitor.Bottom - mi.Monitor.Top, Win32.SWP_NOOWNERZORDER | Win32.SWP_FRAMECHANGED);
    }

    /// <summary>F11 / Alt+Enter / menu: fullscreen at the chosen resolution, and back to the saved window placement.</summary>
    private static void ToggleFullscreen()
    {
        if (!_fullscreen)
        {
            _placement = new Win32.WINDOWPLACEMENT { Length = (uint)sizeof(Win32.WINDOWPLACEMENT) };
            if (!Win32.GetWindowPlacement(_hwnd, ref _placement)) return;
            _fullscreen = true;
            Win32.SetMenu(_hwnd, IntPtr.Zero);
            Win32.SetWindowLongPtrW(_hwnd, Win32.GWL_STYLE, unchecked((IntPtr)(long)(Win32.WS_POPUP | Win32.WS_VISIBLE)));
            if (_v.FsW > 0)
            {
                _exclusive = MonitorDevice() && SetDisplayMode(_v.FsW, _v.FsH);
                if (!_exclusive) Status($"{_v.FsW} x {_v.FsH} not available — desktop resolution used");
            }
            CoverMonitor();
        }
        else
        {
            RestoreDisplayMode();
            _fullscreen = false;
            Win32.SetWindowLongPtrW(_hwnd, Win32.GWL_STYLE, (IntPtr)(Style | Win32.WS_VISIBLE));
            if (_v.MenuBar) Win32.SetMenu(_hwnd, _menuBar);
            Win32.SetWindowPlacement(_hwnd, _placement);
            Win32.SetWindowPos(_hwnd, IntPtr.Zero, 0, 0, 0, 0,
                Win32.SWP_NOMOVE | 0x1 /* NOSIZE */ | Win32.SWP_NOZORDER | Win32.SWP_NOOWNERZORDER | Win32.SWP_FRAMECHANGED);
        }
        if (_v.Fullscreen != _fullscreen) { _v.Fullscreen = _fullscreen; if (_videoPath != null) _v.TrySave(_videoPath); }
        UpdateMenus();
    }

    /// <summary>alt-tab out of an exclusive mode gives the desktop its resolution back; coming back sets it again</summary>
    private static void ActivateApp(bool active)
    {
        if (!_fullscreen || _v.FsW == 0) return;
        if (!active && _exclusive) { RestoreDisplayMode(); Win32.ShowWindow(_hwnd, Win32.SW_MINIMIZE); }
        else if (active && !_exclusive)
        {
            _exclusive = MonitorDevice() && SetDisplayMode(_v.FsW, _v.FsH);
            CoverMonitor();
        }
    }

    /// <summary>--dump-window: the client area as drawn (letterbox, aspect, filter) -> PNG</summary>
    private static void DumpWindow(string path)
    {
        if (!Win32.GetClientRect(_hwnd, out var rc) || rc.Right <= 0 || rc.Bottom <= 0) return;
        int w = rc.Right, h = rc.Bottom;
        Present();
        IntPtr mem = Win32.CreateCompatibleDC(_hdc), bmp = Win32.CreateCompatibleBitmap(_hdc, w, h);
        IntPtr old = Win32.SelectObject(mem, bmp);
        try
        {
            // draw straight into the memory bitmap (reading a window DC back is unreliable when it is covered)
            Paint(mem, w, h);
            Win32.SelectObject(mem, old);
            var bi = new Win32.BITMAPINFOHEADER { Size = (uint)sizeof(Win32.BITMAPINFOHEADER), Width = w, Height = -h, Planes = 1, BitCount = 32 };
            var px = new uint[w * h];
            fixed (uint* p = px) Win32.GetDIBits(mem, bmp, 0, (uint)h, p, ref bi, Win32.DIB_RGB_COLORS);
            var rgb = new byte[w * h * 3];
            for (int i = 0; i < px.Length; i++)
            {
                rgb[i * 3] = (byte)(px[i] >> 16); rgb[i * 3 + 1] = (byte)(px[i] >> 8); rgb[i * 3 + 2] = (byte)px[i];
            }
            File.WriteAllBytes(path, Png.Encode(w, h, rgb));
        }
        finally
        {
            Win32.DeleteObject(bmp);
            Win32.DeleteDC(mem);
        }
    }
}
