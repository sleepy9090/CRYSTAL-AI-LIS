// re-c C# port — playable SDL2 frontend (= web/src/main.js): load data, window blit at integer scale, fixed-step
// 60.0988 Hz loop, keyboard / controller (Input.cs), APU audio (Audio.cs), battery WRAM file (Save.cs).
//
//   ReC.Sdl [--scale S] [--save F.sav] [--data DIR] [--load-state F.json] [--mute] [--no-audio]
//           [--frames N] [--turbo] [--dump-screen F.png] [--quiet]
//
// Keys (= ../web/index.html): arrows / WASD = d-pad, X / K = A, Z / J = B, Enter = Start, Right Shift / Tab = Select,
// P = pause, M = mute, Esc = quit (desktop only). --frames N quits after N frames (exit 0), --turbo runs frames as
// fast as possible instead of at 60.0988 Hz; both are for headless checks
// (`SDL_VIDEODRIVER=dummy SDL_AUDIODRIVER=dummy ReC.Sdl --frames 600`).
using System.Diagnostics;
using System.Runtime.InteropServices;
using ReC;
using ReC.Game;
using ReC.Util;

namespace ReC.Sdl;

internal static class Program
{
    private const string Title = "re-c";

    private static int Main(string[] args)
    {
        var o = new Options();
        try
        {
            for (int i = 0; i < args.Length; i++)
            {
                string k = args[i];
                string V() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"{k} needs a value");
                switch (k)
                {
                    case "--scale": o.Scale = int.Parse(V()); break;
                    case "--save": o.SavePath = V(); break;
                    case "--data": o.DataDir = V(); break;
                    case "--load-state": o.StatePath = V(); break;
                    case "--mute": o.Mute = true; break;
                    case "--no-audio": o.NoAudio = true; break;
                    case "--frames": o.Frames = int.Parse(V()); break;
                    case "--turbo": o.Turbo = true; break;
                    case "--dump-screen": o.Screen = V(); break;
                    case "--quiet": o.Quiet = true; break;
                    case "-h": case "--help":
                        Console.WriteLine("usage: ReC.Sdl [--scale S] [--save F.sav] [--data DIR] [--load-state F.json] [--mute]\n" +
                                          "               [--no-audio] [--frames N] [--turbo] [--dump-screen F.png] [--quiet]");
                        return 0;
                    default: throw new ArgumentException($"unknown option {k} (--help)");
                }
            }
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            Console.Error.WriteLine(e.Message);
            return 2;
        }
        try { return Start(o); }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException)
        {
            Console.Error.WriteLine($"ReC.Sdl: SDL2 not usable ({e.Message}); install libSDL2-2.0.so.0");
            return 1;
        }
    }

    private sealed class Options
    {
        public int Scale = 3, Frames = -1;
        public string? SavePath, DataDir, StatePath, Screen;
        public bool Mute, NoAudio, Turbo, Quiet;
    }

    // js: main.js status
    /// <summary>= the page's status line: shown in the window title.</summary>
    private static void Status(IntPtr window, string s) => Sdl.SetWindowTitle(window, s.Length > 0 ? $"{Title} — {s}" : Title);

    /// <summary>CSHARP/re-c.sav: the folder holding ReC.slnx above the executable, else the working directory.</summary>
    private static string DefaultSavePath()
    {
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "ReC.slnx"))) return Path.Combine(d.FullName, "re-c.sav");
        return Path.Combine(Directory.GetCurrentDirectory(), "re-c.sav");
    }

    // js: main.js start
    private static int Start(Options o)
    {
        var data = Data.LoadDir(Data.FindDir(o.DataDir));
        var m = new Machine(data, s => { if (!o.Quiet) Console.Error.WriteLine($"[re-c] {s}"); });
        // battery WRAM survives power-off: restore it before Reset, as the cartridge would (Save.cs)
        string savePath = o.SavePath ?? DefaultSavePath();
        if (Save.LoadWram(savePath, m.Mem.Wram) && !o.Quiet) Console.Error.WriteLine($"[re-c] WRAM restored from {savePath}");
        m.Reset();
        // a loaded state brings its own WRAM: it only goes to a save file named with --save, never over re-c.sav
        bool saving = o.StatePath == null || o.SavePath != null;
        if (o.StatePath != null) m.LoadState(StateFile.Load(o.StatePath));
        var saver = saving ? new WramSaver(savePath, m.Mem.Wram) : null;

        uint flags = Sdl.InitVideo | Sdl.InitEvents | Sdl.InitGameController | Sdl.InitJoystick | (o.NoAudio ? 0 : Sdl.InitAudio);
        if (Sdl.Init(flags) != 0)
        {
            // no audio device / driver must not stop the game: retry without audio
            if (o.NoAudio || Sdl.Init(flags & ~Sdl.InitAudio) != 0)
            {
                Console.Error.WriteLine($"ReC.Sdl: SDL_Init failed: {Sdl.GetError()}");
                return 1;
            }
            o.NoAudio = true;
        }
        IntPtr window = IntPtr.Zero, renderer = IntPtr.Zero, texture = IntPtr.Zero;
        var input = new Input();
        var audio = new AudioOut(m);
        try
        {
            int scale = Math.Max(1, o.Scale);
            window = Sdl.CreateWindow(Title, Sdl.WindowPosCentered, Sdl.WindowPosCentered, 256 * scale, 240 * scale,
                Sdl.WindowShown | Sdl.WindowResizable | Sdl.WindowAllowHighDpi);
            if (window == IntPtr.Zero) { Console.Error.WriteLine($"ReC.Sdl: SDL_CreateWindow failed: {Sdl.GetError()}"); return 1; }
            Sdl.SetWindowMinimumSize(window, 256, 240);
            renderer = Sdl.CreateRenderer(window, -1, Sdl.RendererAccelerated | Sdl.RendererPresentVsync);
            bool vsync = renderer != IntPtr.Zero;
            if (renderer == IntPtr.Zero) renderer = Sdl.CreateRenderer(window, -1, Sdl.RendererSoftware);
            if (renderer == IntPtr.Zero) { Console.Error.WriteLine($"ReC.Sdl: SDL_CreateRenderer failed: {Sdl.GetError()}"); return 1; }
            texture = Sdl.CreateTexture(renderer, Sdl.PixelFormatArgb8888, Sdl.TextureAccessStreaming, 256, 240);
            if (texture == IntPtr.Zero) { Console.Error.WriteLine($"ReC.Sdl: SDL_CreateTexture failed: {Sdl.GetError()}"); return 1; }

            // no autoplay policy on the desktop: audio starts right away (JS: on the first key / click); M = mute
            if (!o.NoAudio && !audio.Resume() && !o.Quiet)
                Console.Error.WriteLine($"[re-c] no audio device ({Sdl.GetError()}); running silent");
            audio.SetMuted(o.Mute);
            if (!o.Quiet) Console.Error.WriteLine($"[re-c] video={Sdl.VideoDriver()} audio={(o.NoAudio ? "off" : Sdl.AudioDriver())} save={(saving ? savePath : "off (--load-state without --save)")}");

            // NES colour index -> ARGB8888: palettes.json nesRgb is 0xRRGGBB
            uint[] lut = data.NesRgb.Select(c => 0xFF000000u | (uint)c).ToArray();
            var px = new uint[256 * 240];

            // js: main.js blit
            void Blit(byte[] fb)
            {
                for (int i = 0; i < fb.Length; i++) px[i] = lut[fb[i] & 0x3F];
                var h = GCHandle.Alloc(px, GCHandleType.Pinned);
                try { Sdl.UpdateTexture(texture, IntPtr.Zero, h.AddrOfPinnedObject(), 256 * sizeof(uint)); }
                finally { h.Free(); }
            }

            // js: main.js fit
            // largest integer scale that fits the window (drawable pixels), centred; the rest stays black
            Sdl.Rect Fit()
            {
                Sdl.GetRendererOutputSize(renderer, out int w, out int hh);
                int s = Math.Max(1, Math.Min(w / 256, hh / 240));
                return new Sdl.Rect { X = (w - 256 * s) / 2, Y = (hh - 240 * s) / 2, W = 256 * s, H = 240 * s };
            }

            void Present()
            {
                var dst = Fit();
                Sdl.SetRenderDrawColor(renderer, 0, 0, 0, 255);
                Sdl.RenderClear(renderer);
                Sdl.RenderCopy(renderer, texture, IntPtr.Zero, ref dst);
                Sdl.RenderPresent(renderer);
            }

            double step = 1000.0 / Machine.FrameHz;
            var clock = Stopwatch.StartNew();
            double acc = 0, last = clock.Elapsed.TotalMilliseconds;
            bool paused = false, hidden = false, quit = false;
            int ranTotal = 0;

            // js: main.js tick
            // one pass of the host loop: run the frames due since the last pass, blit + queue audio if any ran
            bool Tick(double now)
            {
                bool ran = false;
                if (!paused && !hidden)
                {
                    acc += Math.Min(now - last, 250);       // never run more than ~15 frames to catch up
                    if (o.Turbo) acc = Math.Max(acc, step);
                    while (acc >= step)
                    {
                        m.RunFrame(input.Read()); acc -= step; ran = true; ranTotal++;
                        if (saver != null && m.Frame % 300 == 0) saver.Poll();     // ~5 s
                        if (o.Frames >= 0 && ranTotal >= o.Frames) { quit = true; break; }
                        if (o.Turbo) acc = 0;
                    }
                    if (ran) { Blit(m.Ppu.Fb); audio.Flush(); }
                }
                last = now;
                return ran;
            }

            Status(window, "");
            Blit(m.Ppu.Fb);
            while (!quit)
            {
                while (Sdl.PollEvent(out var e) != 0)
                {
                    input.HandleEvent(e);
                    switch (e.Type)
                    {
                        case Sdl.EvQuit: quit = true; break;
                        case Sdl.EvKeyDown when e.KeyRepeat == 0:
                            if (e.Scancode == Sdl.ScP) { paused = !paused; Status(window, paused ? "paused (P)" : ""); }
                            else if (e.Scancode == Sdl.ScM) { audio.SetMuted(!audio.Muted); Status(window, audio.Muted ? "muted (M)" : ""); }
                            else if (e.Scancode == Sdl.ScEscape) quit = true;
                            break;
                        case Sdl.EvWindow:
                            // = visibilitychange: a minimised / hidden window pauses; the clock restarts on return
                            if (e.WindowEvent is Sdl.WinMinimized or Sdl.WinHidden) hidden = true;
                            else if (e.WindowEvent is Sdl.WinRestored or Sdl.WinShown) hidden = false;
                            last = clock.Elapsed.TotalMilliseconds; acc = 0;
                            break;
                    }
                }
                if (quit) break;
                Tick(clock.Elapsed.TotalMilliseconds);
                Present();                                   // vsync paces the loop when the renderer has it
                if (!o.Turbo && !vsync)
                {
                    double wait = step - acc - 1;            // without vsync (dummy / software): sleep until near the next frame
                    if (wait >= 1) Sdl.Delay((uint)Math.Min(wait, step));
                }
            }

            if (o.Screen != null) Png.WriteFb(o.Screen, m.Ppu.Fb, data.NesRgb, 1);
            if (!o.Quiet) Console.Error.WriteLine($"[re-c] frames={ranTotal} main_state={m.Rd(0x40)} sub_state={m.Rd(0x41)}");
            return 0;
        }
        finally
        {
            if (saving) Save.SaveWram(savePath, m.Mem.Wram);   // always, on leaving (= 'pagehide')
            audio.Close();
            input.Close();
            if (texture != IntPtr.Zero) Sdl.DestroyTexture(texture);
            if (renderer != IntPtr.Zero) Sdl.DestroyRenderer(renderer);
            if (window != IntPtr.Zero) Sdl.DestroyWindow(window);
            Sdl.Quit();
        }
    }
}
