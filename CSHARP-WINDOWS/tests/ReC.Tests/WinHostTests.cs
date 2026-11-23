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
    };
}
