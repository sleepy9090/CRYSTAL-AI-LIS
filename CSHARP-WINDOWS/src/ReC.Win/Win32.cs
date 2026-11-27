// re-c Windows host — raw Win32 P/Invoke declarations (architecture_win.md §1 rule 2). Source-generated
// LibraryImport only (trim-safe, no runtime marshalling stubs); structs are blittable.
using System.Runtime.InteropServices;

namespace ReC.Win;

internal static unsafe partial class Win32
{
    // ---- kernel32: console attach for the headless mode (§1 rule 6)
    public const uint AttachParentProcess = 0xFFFFFFFF;
    public const int StdInputHandle = -10, StdOutputHandle = -11, StdErrorHandle = -12;
    public const uint FileTypeUnknown = 0;

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AttachConsole(uint processId);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr GetStdHandle(int nStdHandle);

    [LibraryImport("kernel32.dll")]
    public static partial uint GetFileType(IntPtr hFile);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr GetModuleHandleW(string? name);

    // ---- winmm: 1 ms timer resolution for the frame pacing sleep
    [LibraryImport("winmm.dll")]
    public static partial uint timeBeginPeriod(uint ms);

    [LibraryImport("winmm.dll")]
    public static partial uint timeEndPeriod(uint ms);

    // ---- user32: window class, window, messages
    public const uint CS_HREDRAW = 0x2, CS_VREDRAW = 0x1, CS_OWNDC = 0x20;
    public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000, WS_POPUP = 0x80000000, WS_VISIBLE = 0x10000000;
    public const int CW_USEDEFAULT = unchecked((int)0x80000000);
    public const int GWL_STYLE = -16;
    public const int SW_SHOW = 5;
    public const uint SWP_NOMOVE = 0x2, SWP_NOZORDER = 0x4, SWP_NOOWNERZORDER = 0x200, SWP_FRAMECHANGED = 0x20,
        SWP_NOACTIVATE = 0x10;
    public const uint PM_REMOVE = 1;
    public const uint MONITOR_DEFAULTTONEAREST = 2;
    public const int IDC_ARROW = 32512;
    public const int BLACK_BRUSH = 4;

    public const uint WM_DESTROY = 0x0002, WM_SIZE = 0x0005, WM_ACTIVATE = 0x0006, WM_KILLFOCUS = 0x0008, WM_PAINT = 0x000F,
        WM_CLOSE = 0x0010, WM_QUIT = 0x0012, WM_ERASEBKGND = 0x0014, WM_GETMINMAXINFO = 0x0024, WM_KEYDOWN = 0x0100,
        WM_KEYUP = 0x0101, WM_CHAR = 0x0102, WM_SYSKEYDOWN = 0x0104, WM_SYSKEYUP = 0x0105, WM_SYSCHAR = 0x0106,
        WM_DPICHANGED = 0x02E0;
    public const int SIZE_MINIMIZED = 1;

    // virtual keys
    public const int VK_TAB = 0x09, VK_RETURN = 0x0D, VK_SHIFT = 0x10, VK_ESCAPE = 0x1B, VK_LEFT = 0x25, VK_UP = 0x26,
        VK_RIGHT = 0x27, VK_DOWN = 0x28, VK_F11 = 0x7A, VK_RSHIFT = 0xA1;

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    public struct POINT { public int X, Y; }

    [StructLayout(LayoutKind.Sequential)]
    public struct MSG
    {
        public IntPtr Hwnd;
        public uint Message;
        public IntPtr WParam, LParam;
        public uint Time;
        public POINT Pt;
        public uint Private;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WNDCLASSEXW
    {
        public uint CbSize, Style;
        public delegate* unmanaged<IntPtr, uint, IntPtr, IntPtr, IntPtr> WndProc;
        public int ClsExtra, WndExtra;
        public IntPtr Instance, Icon, Cursor, Background;
        public char* MenuName, ClassName;
        public IntPtr IconSm;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PAINTSTRUCT
    {
        public IntPtr Hdc;
        public int Erase;
        public RECT Paint;
        public int Restore, IncUpdate;
        public fixed byte Reserved[32];
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint CbSize;
        public RECT Monitor, Work;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT
    {
        public uint Length, Flags, ShowCmd;
        public POINT MinPosition, MaxPosition;
        public RECT NormalPosition;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT Reserved, MaxSize, MaxPosition, MinTrackSize, MaxTrackSize;
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial ushort RegisterClassExW(in WNDCLASSEXW wc);

    [LibraryImport("user32.dll", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr CreateWindowExW(uint exStyle, string className, string title, uint style, int x, int y,
        int w, int h, IntPtr parent, IntPtr menu, IntPtr instance, IntPtr param);

    [LibraryImport("user32.dll")]
    public static partial IntPtr DefWindowProcW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PeekMessageW(out MSG msg, IntPtr hwnd, uint min, uint max, uint remove);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TranslateMessage(in MSG msg);

    [LibraryImport("user32.dll")]
    public static partial IntPtr DispatchMessageW(in MSG msg);

    [LibraryImport("user32.dll")]
    public static partial void PostQuitMessage(int code);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PostMessageW(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(IntPtr hwnd, int cmd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetClientRect(IntPtr hwnd, out RECT r);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AdjustWindowRectEx(ref RECT r, uint style, [MarshalAs(UnmanagedType.Bool)] bool menu, uint exStyle);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int w, int h, uint flags);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetWindowLongPtrW(IntPtr hwnd, int index);

    [LibraryImport("user32.dll")]
    public static partial IntPtr SetWindowLongPtrW(IntPtr hwnd, int index, IntPtr value);

    [LibraryImport("user32.dll")]
    public static partial IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoW(IntPtr monitor, ref MONITORINFO mi);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetWindowPlacement(IntPtr hwnd, ref WINDOWPLACEMENT wp);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPlacement(IntPtr hwnd, in WINDOWPLACEMENT wp);

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowTextW(IntPtr hwnd, string text);

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetDC(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    public static partial int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [LibraryImport("user32.dll")]
    public static partial IntPtr BeginPaint(IntPtr hwnd, out PAINTSTRUCT ps);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EndPaint(IntPtr hwnd, in PAINTSTRUCT ps);

    [LibraryImport("user32.dll")]
    public static partial int FillRect(IntPtr hdc, in RECT r, IntPtr brush);

    [LibraryImport("user32.dll")]
    public static partial IntPtr LoadCursorW(IntPtr instance, IntPtr name);

    [LibraryImport("user32.dll")]
    public static partial IntPtr LoadImageW(IntPtr instance, IntPtr name, uint type, int cx, int cy, uint flags);

    [LibraryImport("user32.dll")]
    public static partial uint GetDpiForWindow(IntPtr hwnd);

    // ---- gdi32: blit
    public const uint DIB_RGB_COLORS = 0, SRCCOPY = 0x00CC0020;

    [StructLayout(LayoutKind.Sequential)]
    public struct BITMAPINFOHEADER
    {
        public uint Size;
        public int Width, Height;
        public ushort Planes, BitCount;
        public uint Compression, SizeImage;
        public int XPelsPerMeter, YPelsPerMeter;
        public uint ClrUsed, ClrImportant;
    }

    [LibraryImport("gdi32.dll")]
    public static partial int StretchDIBits(IntPtr hdc, int xDest, int yDest, int wDest, int hDest, int xSrc, int ySrc,
        int wSrc, int hSrc, void* bits, in BITMAPINFOHEADER bmi, uint usage, uint rop);

    [LibraryImport("gdi32.dll")]
    public static partial IntPtr GetStockObject(int obj);

    [LibraryImport("gdi32.dll")]
    public static partial int SetStretchBltMode(IntPtr hdc, int mode);

    public const int COLORONCOLOR = 3, HALFTONE = 4;

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetBrushOrgEx(IntPtr hdc, int x, int y, IntPtr old);

    // ---- gdi32: --dump-window (read the client area back)
    [LibraryImport("gdi32.dll")]
    public static partial IntPtr CreateCompatibleDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    public static partial IntPtr CreateCompatibleBitmap(IntPtr hdc, int w, int h);

    [LibraryImport("gdi32.dll")]
    public static partial IntPtr SelectObject(IntPtr hdc, IntPtr obj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(IntPtr obj);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(IntPtr hdc);

    [LibraryImport("gdi32.dll")]
    public static partial int GetDIBits(IntPtr hdc, IntPtr bmp, uint start, uint lines, void* bits, ref BITMAPINFOHEADER bmi, uint usage);

    // ---- user32: mouse capture (mouse control: a release outside the window still counts)
    [LibraryImport("user32.dll")]
    public static partial IntPtr SetCapture(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ReleaseCapture();

    // ---- shell32: open a folder in Explorer (Game > Open screenshots folder)
    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr ShellExecuteW(IntPtr hwnd, string? op, string file, string? args, string? dir, int show);

    // ---- kernel32: playtest 10, the Kokoro server loads sherpa-onnx-c-api.dll + onnxruntime.dll from the voice folder
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetDllDirectoryW(string? path);

    // ---- ole32: playtest 6, spoken dialogue (SAPI SpVoice, Dialogue.cs)
    [LibraryImport("ole32.dll")]
    public static partial int CoInitializeEx(void* reserved, uint coInit);
    [LibraryImport("ole32.dll")]
    public static partial int CoCreateInstance(Guid* clsid, void* outer, uint ctx, Guid* iid, void** obj);

    // ---- user32: playtest 11, the reset confirmation
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int MessageBoxW(IntPtr hwnd, string text, string caption, uint type);

    // ---- dwmapi: playtest 3, the GDI picture paced to the compositor (no tearing)
    [LibraryImport("dwmapi.dll")]
    public static partial int DwmFlush();

    // ---- gdi32: T59 HD font glyphs (Glyphs.cs)
    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr CreateFontW(int height, int width, int escapement, int orientation, int weight, uint italic,
        uint underline, uint strikeOut, uint charSet, uint outPrecision, uint clipPrecision, uint quality, uint pitchAndFamily, string face);

    [LibraryImport("gdi32.dll")]
    public static partial uint SetTextColor(IntPtr hdc, uint color);

    [LibraryImport("gdi32.dll")]
    public static partial int SetBkMode(IntPtr hdc, int mode);

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool TextOutW(IntPtr hdc, int x, int y, string text, int count);

    [StructLayout(LayoutKind.Sequential)]
    public struct SIZE { public int Cx, Cy; }

    [LibraryImport("gdi32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetTextExtentPoint32W(IntPtr hdc, string text, int count, out SIZE size);

    // ---- user32: menus (Video menu, VideoMenu.cs)
    public const uint MF_STRING = 0x0, MF_GRAYED = 0x1, MF_CHECKED = 0x8, MF_POPUP = 0x10, MF_SEPARATOR = 0x800,
        MF_BYCOMMAND = 0x0, MFT_RADIOCHECK = 0x200;
    public const uint TPM_RIGHTBUTTON = 0x2, TPM_RETURNCMD = 0x100;
    public const uint WM_MOUSEMOVE = 0x0200, WM_LBUTTONDOWN = 0x0201, WM_LBUTTONUP = 0x0202, WM_RBUTTONDOWN = 0x0204,
        WM_MBUTTONDOWN = 0x0207, WM_MBUTTONUP = 0x0208;
    public const uint WM_COMMAND = 0x0111, WM_ENTERMENULOOP = 0x0211, WM_EXITMENULOOP = 0x0212, WM_RBUTTONUP = 0x0205,
        WM_ACTIVATEAPP = 0x001C, WM_ENTERSIZEMOVE = 0x0231, WM_EXITSIZEMOVE = 0x0232, WM_CONTEXTMENU = 0x007B;
    public const int SW_MINIMIZE = 6, SW_RESTORE = 9;

    [StructLayout(LayoutKind.Sequential)]
    public struct MENUITEMINFOW
    {
        public uint CbSize, Mask, Type, State, Id;
        public IntPtr SubMenu, BmpChecked, BmpUnchecked, ItemData;
        public char* TypeData;
        public uint Cch;
        public IntPtr BmpItem;
    }

    public const uint MIIM_STATE = 0x1, MIIM_FTYPE = 0x100, MFS_CHECKED = 0x8, MFS_GRAYED = 0x3, MFT_STRING = 0x0;

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool InvalidateRect(IntPtr hwnd, IntPtr rect, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [LibraryImport("user32.dll")]
    public static partial IntPtr CreateMenu();

    [LibraryImport("user32.dll")]
    public static partial IntPtr CreatePopupMenu();

    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool AppendMenuW(IntPtr menu, uint flags, nuint id, string? text);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetMenuItemInfoW(IntPtr menu, uint item, [MarshalAs(UnmanagedType.Bool)] bool byPosition, in MENUITEMINFOW mii);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetMenu(IntPtr hwnd, IntPtr menu);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DrawMenuBar(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DestroyMenu(IntPtr menu);

    [LibraryImport("user32.dll")]
    public static partial int TrackPopupMenu(IntPtr menu, uint flags, int x, int y, int reserved, IntPtr hwnd, IntPtr rect);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetCursorPos(out POINT p);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr hwnd);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsZoomed(IntPtr hwnd);

    // ---- user32: display modes (exclusive fullscreen resolution)
    public const int ENUM_CURRENT_SETTINGS = -1;
    public const uint DM_BITSPERPEL = 0x40000, DM_PELSWIDTH = 0x80000, DM_PELSHEIGHT = 0x100000;
    public const uint CDS_FULLSCREEN = 0x4;
    public const int DISP_CHANGE_SUCCESSFUL = 0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MONITORINFOEXW
    {
        public uint CbSize;
        public RECT Monitor, Work;
        public uint Flags;
        public fixed char Device[32];
    }

    /// <summary>DEVMODEW, 220 bytes (display fields only are used).</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DEVMODEW
    {
        public fixed char DeviceName[32];
        public ushort SpecVersion, DriverVersion, Size, DriverExtra;
        public uint Fields;
        public int PositionX, PositionY;
        public uint DisplayOrientation, DisplayFixedOutput;
        public short Color, Duplex, YResolution, TTOption, Collate;
        public fixed char FormName[32];
        public ushort LogPixels;
        public uint BitsPerPel, PelsWidth, PelsHeight, DisplayFlags, DisplayFrequency;
        public uint IcmMethod, IcmIntent, MediaType, DitherType, Reserved1, Reserved2, PanningWidth, PanningHeight;
    }

    [LibraryImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool GetMonitorInfoExW(IntPtr monitor, ref MONITORINFOEXW mi);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool EnumDisplaySettingsW(char* device, int mode, ref DEVMODEW dm);

    [LibraryImport("user32.dll")]
    public static partial int ChangeDisplaySettingsExW(char* device, DEVMODEW* dm, IntPtr hwnd, uint flags, IntPtr param);

    public const int VK_F10 = 0x79;

    public static int LoWord(IntPtr p) => (int)((long)p & 0xFFFF);
    public static int HiWord(IntPtr p) => (int)(((long)p >> 16) & 0xFFFF);
}
