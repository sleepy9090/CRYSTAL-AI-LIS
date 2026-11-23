// re-c Windows host — pure-logic tests of src/ReC.Win that run on Linux (architecture_win.md §2; linked sources, no
// Win32 call is made): keyboard map (Pad.cs Input), XInput mapping (Pad.Buttons), letterbox fit (Window.Fit),
// save file round trip (Save.cs), the --keys script key table.
using ReC.Game;
using ReC.Nes;
using ReC.Util;
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
        ("ppu_tags", () =>
        {
            // T10: Ppu.Tags never changes Fb, and every tag names the tile pixel the PPU drew. Checked without
            // assuming a palette (the game changes palettes mid-frame): on one line, all pixels tagged with the same
            // palette group + CHR slot have one colour, and the slots of a group give >= 2 colours where >= 2 are seen.
            var keys = Keys.Parse("1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80".Split(' '));
            var a = new Machine(TheData.Value, _ => { });
            var b = new Machine(TheData.Value, _ => { });
            a.Reset(); b.Reset();
            b.Ppu.Tags = new int[256 * 240];
            var enh = new Enhancer(b.Ppu.Chr, TheData.Value.NesRgb);
            long tagged = 0, conflicts = 0, sprites = 0, splitFrames = 0;
            bool same = true;
            var seen = new int[8 * 4];
            for (int f = 214; f <= 2460; f++)
            {
                int k = keys.TryGetValue(f, out int v) ? v : 0;
                a.RunFrame(k); b.RunFrame(k);
                if (!a.Ppu.Fb.AsSpan().SequenceEqual(b.Ppu.Fb)) { same = false; break; }
                if (f % 10 != 0) continue;
                if (b.IrqSplits.Count > 0) splitFrames++;
                for (int y = 0; y < 240; y++)
                {
                    Array.Fill(seen, -1);
                    for (int x = 0; x < 256; x++)
                    {
                        int i = y * 256 + x, t = b.Ppu.Tags[i];
                        if ((t & Ppu.TagValid) == 0) continue;
                        tagged++;
                        int g = t >> 19 & 7, slot = enh.Slot(t & 0x1FFF, t >> 13 & 7, t >> 16 & 7);
                        if (g >= 4) { sprites++; if (slot == 0) conflicts++; }   // a sprite tag is never transparent
                        int c = b.Ppu.Fb[i];
                        if (seen[g * 4 + slot] < 0) seen[g * 4 + slot] = c;
                        else if (seen[g * 4 + slot] != c) conflicts++;
                    }
                }
            }
            True(same, "Fb identical with tags on");
            True(tagged > 1_000_000 && sprites > 1000, $"tagged {tagged}, sprite {sprites}");
            True(splitFrames > 0, "some sampled frames have IRQ splits");
            Eq(0L, conflicts, "one colour per line + group + slot");
            Console.WriteLine($"  ppu_tags: {tagged} tagged pixels, {sprites} sprite, {splitFrames} split frames, {conflicts} conflicts");
        }),
        ("win_enhance", () =>
        {
            var d = TheData.Value;
            var m = new Machine(d, _ => { });
            m.Reset();
            m.Ppu.Tags = new int[256 * 240];
            for (int f = 0; f < 787; f++) m.RunFrame(0);           // frame_no 1000, the intro picture
            var enh = new Enhancer(m.Ppu.Chr, d.NesRgb, record: true);
            var hd = new uint[Enhancer.W * Enhancer.H];
            enh.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, hd);
            var orig = new HashSet<uint>(); var more = new HashSet<uint>();
            for (int i = 0; i < 256 * 240; i++) orig.Add((uint)d.NesRgb[m.Ppu.Fb[i] & 0x3F] & 0xFFFFFF);
            foreach (uint c in hd) more.Add(c);
            True(more.Count > orig.Count * 3, $"more colours: {orig.Count} -> {more.Count}");
            // timing (one frame at 60 Hz = 16.7 ms)
            // (after a warm-up: .NET compiles a method fully optimised only after ~30 calls, as in a real session)
            for (int r = 0; r < 100; r++) enh.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, hd);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int r = 0; r < 100; r++) enh.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, hd);
            double ms = sw.Elapsed.TotalMilliseconds / 100;
            Console.WriteLine($"  win_enhance: Render {ms:F1} ms / frame, colours {orig.Count} -> {more.Count}");
            True(ms < 10, $"Render fast enough: {ms:F1} ms");
            // nothing hidden leaks: a one-colour screen (tags from real tiles) stays that colour
            var flatFb = Enumerable.Repeat((byte)0x0F, 256 * 240).ToArray();
            enh.Render(flatFb, m.Ppu.Tags, m.Ppu.Pal, hd);
            uint black = (uint)d.NesRgb[0x0F] & 0xFFFFFF;
            True(hd.All(c => c == black), "one-colour frame renders as that colour only");
            enh.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, hd);
            // the block of an untagged (backdrop) pixel = its colour; block centres of flat areas keep the colour
            True(enh.Seen!.Count > 10, $"seen {enh.Seen.Count} tile + palette combinations");
            // a hand-drawn pack tile replaces its blocks
            string dir = Path.Combine(Path.GetTempPath(), $"rec_pack_{Environment.ProcessId}");
            try
            {
                long key = enh.Seen.First(k => enh.Exportable((int)(k >> 24), (int)(k >> 16 & 63), (int)(k >> 8 & 63), (int)(k & 63)));
                int tile = (int)(key >> 24), c1 = (int)(key >> 16 & 63), c2 = (int)(key >> 8 & 63), c3 = (int)(key & 63);
                Directory.CreateDirectory(dir);
                var rgba = new byte[Enhancer.T * Enhancer.T * 4];
                for (int i = 0; i < rgba.Length; i += 4) { rgba[i] = 255; rgba[i + 1] = 0; rgba[i + 2] = 255; rgba[i + 3] = 255; }
                File.WriteAllBytes(Path.Combine(dir, Enhancer.ArtName(tile, c1, c2, c3)), PngRead.EncodeRgba(Enhancer.T, Enhancer.T, rgba));
                File.WriteAllText(Path.Combine(dir, "bogus.png"), "x");
                File.WriteAllBytes(Path.Combine(dir, "0001_0F0F0F.png"), PngRead.EncodeRgba(8, 8, new byte[256]));
                enh.LoadPack(dir);
                Eq(1, enh.ArtCount, "1 pack tile"); Eq(2, enh.PackErrors.Count, "2 bad files reported");
                enh.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, hd);
                True(hd.Contains(0xFF00FFu), "pack tile drawn");
                int n = enh.Export(Path.Combine(dir, "export"));
                Eq(enh.Seen.Count(k => enh.Exportable((int)(k >> 24), (int)(k >> 16 & 63), (int)(k >> 8 & 63), (int)(k & 63))), n, "export count");
                False(enh.Exportable(0, 0x0F, 0x16, 0x30) && enh.Slot(0, 0, 0) == 0 && Enumerable.Range(0, 64).All(i => enh.Slot(0, i % 8, i / 8) == 0), "blank tile not exported");
                False(enh.Exportable(tile, 0x0F, 0x0F, 0x0F), "one-colour combination not exported");
                var (w, h, px) = PngRead.Decode(File.ReadAllBytes(Path.Combine(dir, "export", Enhancer.ArtName(tile, c1, c2, c3))));
                Eq((32, 32), (w, h), "export tile 32x32");
                True(File.Exists(Path.Combine(dir, "export", "original", Enhancer.ArtName(tile, c1, c2, c3))), "original written");
            }
            finally { try { Directory.Delete(dir, true); } catch (IOException) { } }
            True(Enhancer.TryParseArtName("1A3F_0F1630.png", out long kk) && kk == Enhancer.ArtKey(0x1A3F, 0x0F, 0x16, 0x30), "name parse");
            False(Enhancer.TryParseArtName("1A3F_0F1640.png", out _), "colour > 3F rejected");
            Eq(0x000000u, Enhancer.Shade(0x000000, 0), "black stays black in shadow"); Eq(0x123456u, Enhancer.Shade(0x123456, 16), "level 16 = unchanged");
            True(Enhancer.Shade(0x808080, 32) > 0x808080 && Enhancer.Shade(0x808080, 0) < 0x808080, "shade lightens / darkens");
            Eq("graphics=enhanced", VideoSettings.Parse("graphics=enhanced\n").Serialize().Split('\n').First(l => l.StartsWith("graphics")), "ini graphics");
            Eq(271, Window.CommandId("graphics:enhanced"), "--menu graphics:enhanced");
        }),
        ("win_png_read", () =>
        {
            var rgba = new byte[3 * 2 * 4];
            for (int i = 0; i < rgba.Length; i++) rgba[i] = (byte)(i * 11);
            var (w, h, px) = PngRead.Decode(PngRead.EncodeRgba(3, 2, rgba));
            Eq((3, 2), (w, h), "size");
            Eq(((uint)rgba[7] << 24 | (uint)rgba[4] << 16 | (uint)rgba[5] << 8 | rgba[6]), px[1], "pixel 1 ARGB");
            var rgb = Png.Encode(2, 2, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 });
            var (_, _, p2) = PngRead.Decode(rgb);
            Eq(0xFF0A0B0Cu, p2[3], "RGB decode, opaque");
            Throws(() => PngRead.Decode(new byte[] { 1, 2, 3 }), "not a PNG");
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
