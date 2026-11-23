// re-c Windows host — pure-logic tests of src/ReC.Win that run on Linux (architecture_win.md §2; linked sources, no
// Win32 call is made): keyboard map (Pad.cs Input), XInput mapping (Pad.Buttons), letterbox fit (Window.Fit),
// save file round trip (Save.cs), the --keys script key table.
using ReC.Win;

namespace ReC.Tests;

internal static partial class Program
{
    private static readonly (string Name, Action Fn)[] WinTests =
    {
        ("win_input", () =>
        {
            var k = new Input();
            k.KeyDown('X'); k.KeyDown(Win32.VK_LEFT); Eq(0x82, k.Read(), "X + Left = A|Left");
            k.KeyUp('X'); Eq(0x02, k.Read(), "X released");
            k.KeyDown('K'); k.KeyDown('J'); k.KeyDown(Win32.VK_RETURN); k.KeyDown(Win32.VK_TAB);
            Eq(0xF2, k.Read(), "K / J / Enter / Tab");
            k.Blur(); Eq(0, k.Read(), "blur releases all");
            Eq(Win32.VK_RSHIFT, Input.Vk(Win32.VK_SHIFT, 0x36L << 16), "right shift by scan code");
            Eq(Win32.VK_SHIFT, Input.Vk(Win32.VK_SHIFT, 0x2AL << 16), "left shift stays VK_SHIFT");
            k.KeyDown(Input.Vk(Win32.VK_SHIFT, 0x36L << 16)); Eq(0x20, k.Read(), "right shift = Select");
            k.Blur(); k.KeyDown(Input.Vk(Win32.VK_SHIFT, 0x2AL << 16)); Eq(0, k.Read(), "left shift unmapped");
            foreach (var (bit, vk) in Input.ScriptKeys)
            {
                var s = new Input(); s.KeyDown(vk);
                Eq(bit, s.Read(), $"script key {vk:X2} -> bit {bit:X2}");
            }
        }),
        ("win_pad", () =>
        {
            Eq(Btn.A | Btn.Up, Pad.Buttons(0x1000 | 0x0001, 0, 0), "XInput A + dpad up");
            Eq(Btn.A | Btn.B, Pad.Buttons(0x2000 | 0x8000, 0, 0), "B = A, Y = B");
            Eq(Btn.Select | Btn.Start, Pad.Buttons(0x0020 | 0x0010, 0, 0), "back / start");
            Eq(Btn.Up | Btn.Left, Pad.Buttons(0, -20000, 20000), "stick up-left (XInput Y+ = up)");
            Eq(Btn.Down | Btn.Right, Pad.Buttons(0, 20000, -20000), "stick down-right");
            Eq(0, Pad.Buttons(0, 16000, -16000), "inside the 0.5 dead zone");
            if (!OperatingSystem.IsWindows())
            {
                var p = new Pad();
                Eq("none", p.Api, "no XInput off Windows");
                Eq(0, p.Read(0), "absent controller reads 0");
            }
        }),
        ("win_options", () =>
        {
            var o = WindowOptions.Parse(new[] { "--window", "--frames", "600", "--keys", "250:START", "300:A:8",
                "--save", "x.sav", "--turbo", "--mute", "--frame0", "10", "--scale", "2" })!;
            Eq(600, o.Frames, "frames"); Eq(2, o.KeySpecs.Count, "keys"); Eq("x.sav", o.SavePath, "save");
            True(o.Turbo && o.Mute && o.Frame0Given, "flags"); Eq(10, o.Frame0, "frame0"); Eq(2, o.Scale, "scale");
            Eq(null, WindowOptions.Parse(new[] { "--help" }), "--help");
            Throws(() => WindowOptions.Parse(new[] { "--bogus" }), "unknown option");
            Throws(() => WindowOptions.Parse(new[] { "--frames" }), "missing value");
        }),
        ("win_fit", () =>
        {
            Eq((0, 0, 256, 240), Window.Fit(256, 240), "1x exact");
            Eq((144, 60, 512, 480), Window.Fit(800, 600), "2x letterboxed");
            Eq((-78, -70, 256, 240), Window.Fit(100, 100), "never below 1x, centred (WM_GETMINMAXINFO keeps the client >= 1x)");
            Eq((320, 0, 1280, 1200), Window.Fit(1920, 1200), "5x pillarboxed");
        }),
        ("win_save", () =>
        {
            string dir = Path.Combine(Path.GetTempPath(), $"rec_save_{Environment.ProcessId}");
            string p = Path.Combine(dir, "sub", "t.sav");
            try
            {
                var w = new byte[Save.Size];
                w[0] = 0x12; w[Save.Size - 1] = 0x34;
                True(Save.SaveWram(p, w), "save creates folders");
                var r = new byte[Save.Size];
                True(Save.LoadWram(p, r) && r[0] == 0x12 && r[Save.Size - 1] == 0x34, "round trip");
                File.WriteAllBytes(p, new byte[10]);
                False(Save.LoadWram(p, r), "wrong size ignored");
                var saver = new WramSaver(p, w);
                False(saver.Poll(), "unchanged: no write");
                w[5] = 9; True(saver.Poll(), "changed: written");
                True(Save.LoadWram(p, r) && r[5] == 9, "poll wrote the image");
                True(Save.DefaultPath().EndsWith(Path.Combine("re-c", "re-c.sav")), "default path re-c/re-c.sav");
            }
            finally { try { Directory.Delete(dir, true); } catch (IOException) { } }
        }),
        ("win_video_layout", () =>
        {
            var v = new VideoSettings();
            Eq((144, 60, 512, 480), v.Layout(800, 600), "default = square integer (= Window.Fit)");
            v.Aspect = Aspect.SixteenNine;
            Eq((106, 60, 1707, 960), v.Layout(1920, 1080), "16:9 integer 4x at 1080p");
            Eq((213, 60, 853, 480), v.Layout(1280, 600), "16:9 integer 2x");
            v.Integer = false;
            Eq((0, 0, 1280, 720), v.Layout(1280, 720), "16:9 free scale fills 720p");
            Eq((0, 75, 800, 450), v.Layout(800, 600), "16:9 free scale letterboxed");
            v.Aspect = Aspect.FourThree;
            Eq((240, 0, 1440, 1080), v.Layout(1920, 1080), "4:3 pillarboxed at 1080p");
            v.Aspect = Aspect.Fill;
            Eq((0, 0, 1000, 300), v.Layout(1000, 300), "fill = whole client");
            v = new VideoSettings { Aspect = Aspect.Tv };
            Eq((126, 20, 1170, 960), v.Layout(1422, 1000), "TV 8:7 integer 4x: 256*8/7*4 = 1170");
            v.CropOverscan = true;
            Eq(224, v.VisibleH, "crop -> 224 lines"); Eq(8, v.FirstLine, "first line 8");
            Eq((0, 2, 1024, 896), new VideoSettings { CropOverscan = true }.Layout(1024, 900), "square crop 4x = 1024x896, centred");
            Eq((768, 720), new VideoSettings().ClientSizeAt(3), "3x window = 768x720");
            Eq((1280, 720), new VideoSettings { Aspect = Aspect.SixteenNine }.ClientSizeAt(3), "3x 16:9 window");
            var p = new VideoSettings { WindowW = 1600, WindowH = 900 };
            Eq((1600, 900), p.ClientSize(), "preset size");
            p.SetScale(9); Eq((6, 0), (p.Scale, p.WindowW), "scale clamps to 6x and drops the preset");
        }),
        ("win_video_ini", () =>
        {
            var v = new VideoSettings
            {
                Aspect = Aspect.SixteenTen, Integer = false, CropOverscan = true, Smooth = true, WindowW = 1280, WindowH = 720,
                Fullscreen = true, FsW = 1920, FsH = 1080, MenuBar = false,
            };
            var r = VideoSettings.Parse(v.Serialize());
            Eq(v.Serialize(), r.Serialize(), "round trip");
            Eq(Aspect.SixteenTen, r.Aspect, "aspect"); Eq((1280, 720), (r.WindowW, r.WindowH), "window preset");
            Eq((1920, 1080), (r.FsW, r.FsH), "fullscreen mode");
            var d = VideoSettings.Parse("aspect=bogus\nwindow=99x\nfullscreen_mode=12x5\nscaling=?\nnot a line\nwindow=4x\n");
            Eq(new VideoSettings { Scale = 4 }.Serialize(), d.Serialize(), "bad values keep the defaults");
            True(VideoSettings.DefaultPath().EndsWith(Path.Combine("re-c", "video.ini")), "default path re-c/video.ini");
            var modes = VideoSettings.ModeList(new[] { (1920, 1080, 32), (800, 600, 32), (1920, 1080, 32), (640, 400, 32), (1024, 768, 16) });
            Eq("800x600,1920x1080", string.Join(',', modes.Select(m => $"{m.W}x{m.H}")), "mode list: 32 bpp, >= 640x480, distinct, sorted");
        }),
        ("win_upscale", () =>
        {
            // 3x3 diagonal of 1s on 0 (edges clamp: the border blocks follow the same rules; hand-checked); flat stays flat
            uint[] diag = { 1, 0, 0, 0, 1, 0, 0, 0, 1 };
            var d2 = new uint[36];
            Upscale.Scale2x(diag, 3, 3, d2);
            Eq("110000,101000,011100,001110,000101,000011",
                string.Join(',', Enumerable.Range(0, 6).Select(y => string.Concat(d2.Skip(y * 6).Take(6)))), "scale2x diagonal");
            var flat = Enumerable.Repeat(7u, 12).ToArray();
            foreach (int k in new[] { 1, 2, 3, 4 })
            {
                var o = new uint[12 * k * k];
                Upscale.Apply(flat, 4, 3, k, o);
                True(o.All(v => v == 7), $"flat stays flat x{k}");
            }
            var d3 = new uint[81];
            Upscale.Scale3x(diag, 3, 3, d3);
            Eq("111000000,110100000,100100000,011111000,000111000,000111110,000001001,000001011,000000111",
                string.Join(',', Enumerable.Range(0, 9).Select(y => string.Concat(d3.Skip(y * 9).Take(9)))), "scale3x diagonal");
            // every output pixel of the centre comes from the source pixel or a 4-neighbour
            var rnd = new Random(5);
            var src = Enumerable.Range(0, 64).Select(_ => (uint)rnd.Next(3)).ToArray();
            var d4 = new uint[64 * 16];
            Upscale.Apply(src, 8, 8, 4, d4);
            var d22 = new uint[64 * 4]; var d44 = new uint[64 * 16];
            Upscale.Scale2x(src, 8, 8, d22); Upscale.Scale2x(d22, 16, 16, d44);
            True(d4.SequenceEqual(d44), "scale4x = scale2x twice");
            var v = VideoSettings.Parse("upscale=scale3x\n");
            Eq(3, v.Upscale, "ini upscale"); True(v.Serialize().Contains("upscale=scale3x"), "ini writes upscale");
            Eq(1, VideoSettings.Parse("upscale=hq9x\n").Upscale, "bad upscale ignored");
            Eq(262, Window.CommandId("upscale:scale2x"), "--menu upscale:scale2x");
        }),
        ("win_video_menu", () =>
        {
            Eq(213, Window.CommandId("size:3x"), "size:3x");
            Eq(223, Window.CommandId("size:1280x720"), "size preset index 3");
            Eq(240 + (int)Aspect.SixteenNine, Window.CommandId("aspect:16:9"), "aspect:16:9");
            Eq(200, Window.CommandId("Fullscreen"), "case-insensitive");
            True(Window.CommandId("fsmode:1280x720") < 0, "unlisted fullscreen mode -> direct request");
            Throws(() => Window.CommandId("size:7x"), "bad size");
            Throws(() => Window.CommandId("nope"), "unknown");
            unsafe
            {
                Eq(220, sizeof(Win32.DEVMODEW), "DEVMODEW size");
                Eq(104, sizeof(Win32.MONITORINFOEXW), "MONITORINFOEXW size");
                Eq(80, sizeof(Win32.MENUITEMINFOW), "MENUITEMINFOW size (x64)");
            }
            var o = WindowOptions.Parse(new[] { "--fullscreen", "--video-config", "v.ini", "--menu", "aspect:tv,crop", "--dump-window", "w.png", "--scale", "2" })!;
            True(o.Fullscreen && o.ScaleGiven, "flags"); Eq("v.ini", o.VideoConfig, "config");
            Eq("aspect:tv|crop", string.Join('|', o.MenuCmds), "menu cmds"); Eq("w.png", o.DumpWindow, "dump-window");
        }),
    };
}
