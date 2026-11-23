// re-c C# port — hand-written SDL2 P/Invoke declarations (libSDL2-2.0.so.0, no NuGet). Only what ReC.Sdl uses.
using System.Runtime.InteropServices;

namespace ReC.Sdl;

internal static class Sdl
{
    private const string Lib = "libSDL2-2.0.so.0";

    public const uint InitAudio = 0x10, InitVideo = 0x20, InitJoystick = 0x200, InitGameController = 0x2000, InitEvents = 0x4000;
    public const int WindowPosCentered = 0x2FFF0000;
    public const uint WindowShown = 0x4, WindowResizable = 0x20, WindowAllowHighDpi = 0x2000;
    public const uint RendererSoftware = 1, RendererAccelerated = 2, RendererPresentVsync = 4;
    public const uint PixelFormatArgb8888 = 0x16362004;
    public const int TextureAccessStreaming = 1;
    public const ushort AudioF32Sys = 0x8120;      // little-endian float32

    // SDL_EventType
    public const uint EvQuit = 0x100, EvWindow = 0x200, EvKeyDown = 0x300, EvKeyUp = 0x301,
        EvControllerAdded = 0x653, EvControllerRemoved = 0x654;
    // SDL_WindowEventID
    public const byte WinShown = 1, WinHidden = 2, WinMinimized = 7, WinRestored = 9, WinFocusGained = 12, WinFocusLost = 13;

    // SDL_Scancode (physical keys = KeyboardEvent.code in the browser)
    public const int ScA = 4, ScD = 7, ScJ = 13, ScK = 14, ScM = 16, ScP = 19, ScS = 22, ScW = 26, ScX = 27, ScZ = 29,
        ScReturn = 40, ScEscape = 41, ScTab = 43, ScRight = 79, ScLeft = 80, ScDown = 81, ScUp = 82, ScKpEnter = 88,
        ScRShift = 229;

    // SDL_GameControllerButton / Axis
    public const int CbA = 0, CbB = 1, CbX = 2, CbY = 3, CbBack = 4, CbStart = 6,
        CbDpadUp = 11, CbDpadDown = 12, CbDpadLeft = 13, CbDpadRight = 14;
    public const int AxisLeftX = 0, AxisLeftY = 1;

    /// <summary>SDL_Event union (56 bytes). Fields used: type; window.event; key.repeat / key.keysym.scancode;
    /// cdevice.which.</summary>
    [StructLayout(LayoutKind.Explicit, Size = 56)]
    public struct Event
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public int Which;          // SDL_ControllerDeviceEvent.which (device index / instance id)
        [FieldOffset(12)] public byte WindowEvent;  // SDL_WindowEvent.event
        [FieldOffset(13)] public byte KeyRepeat;    // SDL_KeyboardEvent.repeat
        [FieldOffset(16)] public int Scancode;      // SDL_KeyboardEvent.keysym.scancode
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Rect { public int X, Y, W, H; }

    [StructLayout(LayoutKind.Sequential)]
    public struct AudioSpec
    {
        public int Freq;
        public ushort Format;
        public byte Channels;
        public byte Silence;
        public ushort Samples;
        public ushort Padding;
        public uint Size;
        public IntPtr Callback;
        public IntPtr Userdata;
    }

    [DllImport(Lib, EntryPoint = "SDL_Init")] public static extern int Init(uint flags);
    [DllImport(Lib, EntryPoint = "SDL_Quit")] public static extern void Quit();
    [DllImport(Lib, EntryPoint = "SDL_GetError")] private static extern IntPtr GetErrorPtr();
    public static string GetError() => Marshal.PtrToStringUTF8(GetErrorPtr()) ?? "";
    [DllImport(Lib, EntryPoint = "SDL_GetCurrentVideoDriver")] private static extern IntPtr GetCurrentVideoDriverPtr();
    public static string VideoDriver() => Marshal.PtrToStringUTF8(GetCurrentVideoDriverPtr()) ?? "";
    [DllImport(Lib, EntryPoint = "SDL_GetCurrentAudioDriver")] private static extern IntPtr GetCurrentAudioDriverPtr();
    public static string AudioDriver() => Marshal.PtrToStringUTF8(GetCurrentAudioDriverPtr()) ?? "";

    [DllImport(Lib, EntryPoint = "SDL_CreateWindow")]
    public static extern IntPtr CreateWindow([MarshalAs(UnmanagedType.LPUTF8Str)] string title, int x, int y, int w, int h, uint flags);
    [DllImport(Lib, EntryPoint = "SDL_DestroyWindow")] public static extern void DestroyWindow(IntPtr window);
    [DllImport(Lib, EntryPoint = "SDL_SetWindowTitle")]
    public static extern void SetWindowTitle(IntPtr window, [MarshalAs(UnmanagedType.LPUTF8Str)] string title);
    [DllImport(Lib, EntryPoint = "SDL_SetWindowMinimumSize")] public static extern void SetWindowMinimumSize(IntPtr window, int w, int h);

    [DllImport(Lib, EntryPoint = "SDL_CreateRenderer")] public static extern IntPtr CreateRenderer(IntPtr window, int index, uint flags);
    [DllImport(Lib, EntryPoint = "SDL_DestroyRenderer")] public static extern void DestroyRenderer(IntPtr renderer);
    [DllImport(Lib, EntryPoint = "SDL_GetRendererOutputSize")] public static extern int GetRendererOutputSize(IntPtr renderer, out int w, out int h);
    [DllImport(Lib, EntryPoint = "SDL_SetRenderDrawColor")] public static extern int SetRenderDrawColor(IntPtr renderer, byte r, byte g, byte b, byte a);
    [DllImport(Lib, EntryPoint = "SDL_RenderClear")] public static extern int RenderClear(IntPtr renderer);
    [DllImport(Lib, EntryPoint = "SDL_RenderCopy")] public static extern int RenderCopy(IntPtr renderer, IntPtr texture, IntPtr src, ref Rect dst);
    [DllImport(Lib, EntryPoint = "SDL_RenderPresent")] public static extern void RenderPresent(IntPtr renderer);
    [DllImport(Lib, EntryPoint = "SDL_CreateTexture")] public static extern IntPtr CreateTexture(IntPtr renderer, uint format, int access, int w, int h);
    [DllImport(Lib, EntryPoint = "SDL_DestroyTexture")] public static extern void DestroyTexture(IntPtr texture);
    [DllImport(Lib, EntryPoint = "SDL_UpdateTexture")] public static extern int UpdateTexture(IntPtr texture, IntPtr rect, IntPtr pixels, int pitch);

    [DllImport(Lib, EntryPoint = "SDL_PollEvent")] public static extern int PollEvent(out Event e);
    [DllImport(Lib, EntryPoint = "SDL_Delay")] public static extern void Delay(uint ms);

    [DllImport(Lib, EntryPoint = "SDL_NumJoysticks")] public static extern int NumJoysticks();
    [DllImport(Lib, EntryPoint = "SDL_IsGameController")] public static extern int IsGameController(int index);
    [DllImport(Lib, EntryPoint = "SDL_GameControllerOpen")] public static extern IntPtr GameControllerOpen(int index);
    [DllImport(Lib, EntryPoint = "SDL_GameControllerClose")] public static extern void GameControllerClose(IntPtr gc);
    [DllImport(Lib, EntryPoint = "SDL_GameControllerGetAttached")] public static extern int GameControllerGetAttached(IntPtr gc);
    [DllImport(Lib, EntryPoint = "SDL_GameControllerGetButton")] public static extern byte GameControllerGetButton(IntPtr gc, int button);
    [DllImport(Lib, EntryPoint = "SDL_GameControllerGetAxis")] public static extern short GameControllerGetAxis(IntPtr gc, int axis);

    [DllImport(Lib, EntryPoint = "SDL_OpenAudioDevice")]
    public static extern uint OpenAudioDevice(IntPtr device, int iscapture, ref AudioSpec desired, out AudioSpec obtained, int allowedChanges);
    [DllImport(Lib, EntryPoint = "SDL_CloseAudioDevice")] public static extern void CloseAudioDevice(uint dev);
    [DllImport(Lib, EntryPoint = "SDL_PauseAudioDevice")] public static extern void PauseAudioDevice(uint dev, int pauseOn);
    [DllImport(Lib, EntryPoint = "SDL_QueueAudio")] public static extern int QueueAudio(uint dev, IntPtr data, uint len);
    [DllImport(Lib, EntryPoint = "SDL_GetQueuedAudioSize")] public static extern uint GetQueuedAudioSize(uint dev);
    [DllImport(Lib, EntryPoint = "SDL_ClearQueuedAudio")] public static extern void ClearQueuedAudio(uint dev);
}
