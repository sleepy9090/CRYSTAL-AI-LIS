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
    /// <summary>--ambwav outdoor|cave|hot|icy OUT.wav [SECONDS]: one ambient bed (T54) as a stereo WAV</summary>
    private static int WinTests_AmbWav(string[] a)
    {
        var amb = new Ambience();
        switch (a[1]) { case "outdoor": amb.Outdoor = 1; break; case "cave": amb.Cave = 1; break; case "hot": amb.Hot = 1; break; default: amb.Icy = 1; break; }
        int n = (int)((a.Length > 3 ? double.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture) : 20) * 44100);
        var pcm = new List<short>(n * 2);
        for (int i = 0; i < n; i++) { var (l, r) = amb.Next(); pcm.Add((short)Math.Clamp(l, -32768, 32767)); pcm.Add((short)Math.Clamp(r, -32768, 32767)); }
        File.WriteAllBytes(a[2], WavOut.WavBytes(pcm, 2, 44100));
        Console.WriteLine($"wrote {a[2]}");
        return 0;
    }

    /// <summary>--cdwav ID OUT.wav SECONDS [cd|remaster|orchestra] [MUTE]: a music id through the CD path (T50 listening / analysis)</summary>
    /// <summary>--playwav STATE OUT.wav SECONDS [orchestra|cd|remaster] [KEYS..]: the game itself from a state (music, effects,
    /// scene changes) through the CD mixer + ambient sounds (fire + cave + outdoor beds on), mixed as AudioOut does</summary>
    private static int WinTests_PlayWav(string[] a)
    {
        var d = TheData.Value;
        string mode = a.Length > 4 ? a[4] : "orchestra";
        var m = new Machine(d, _ => { }); m.Reset();
        var st = StateFile.Load(Path.Combine(Data.FindDir(), "..", "test", "states", a[1] + ".json"));
        m.LoadState(st);
        var mix = new CdMixer { Remaster = mode == "remaster", Orchestra = mode == "orchestra", Mute = int.Parse(Environment.GetEnvironmentVariable("PLAY_MUTE") ?? "0") };
        mix.Chip = () => m.Apu;
        m.Apu.SetOutput(CdMixer.InRate, null);
        m.Apu.OutChannels = mix.Push;
        var amb = Environment.GetEnvironmentVariable("PLAY_AMB") == "0" ? new Ambience() : new Ambience { Hot = 1, Cave = 0.5f, Outdoor = 0.3f };
        var keys = Keys.Parse(a.Skip(5));
        int f0 = st.Frame0 ?? st.Frame;
        var pcm = new List<short>(); var tmp = new List<short>();
        double sec = double.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture);
        for (int f = 0; f < (int)(sec * 60.0988); f++)
        {
            m.RunFrame(keys.TryGetValue(f0 + m.Frame, out int b) ? b : 0);
            tmp.Clear(); mix.Take(tmp);
            for (int i = 0; i + 1 < tmp.Count; i += 2)
            {
                var (al, ar) = amb.Next();
                pcm.Add((short)Math.Clamp(Math.Round(tmp[i] + al), -32768, 32767));
                pcm.Add((short)Math.Clamp(Math.Round(tmp[i + 1] + ar), -32768, 32767));
            }
        }
        File.WriteAllBytes(a[2], WavOut.WavBytes(pcm, 2, CdMixer.OutRate));
        Console.WriteLine($"wrote {a[2]}: {pcm.Count / 2.0 / CdMixer.OutRate:F1} s ({mode} + ambience)");
        return 0;
    }

    private static int WinTests_CdWav(string[] a)
    {
        int id = a[1].StartsWith("0x") ? Convert.ToInt32(a[1][2..], 16) : int.Parse(a[1]);
        double sec = double.Parse(a[3], System.Globalization.CultureInfo.InvariantCulture);
        string mode = a.Length > 4 ? a[4] : "orchestra";
        int mute = a.Length > 5 ? int.Parse(a[5]) : 0;
        var m = new Machine(TheData.Value);
        var mix = new CdMixer { Remaster = mode == "remaster", Orchestra = mode == "orchestra", Mute = mute };
        mix.Chip = () => m.Apu;
        m.Apu.SetOutput(CdMixer.InRate, null);
        m.Apu.OutChannels = mix.Push;
        m.Reset();
        PlaySound.Play(m, id);
        var pcm = new List<short>();
        for (int f = 0; f < (int)(sec * 60.0988); f++)
        {
            m.SoundUpdate(m); m.Apu.Run(29781); mix.Take(pcm);
            if (Environment.GetEnvironmentVariable("CDWAV_TRACE") == "1")
                Console.WriteLine($"f{f} {f / 60.0988:F2}s p1 per {m.Apu.P1.Period} vol {m.Apu.P1.Env.Out} len {m.Apu.P1.Len} duty {m.Apu.P1.Duty} sw {m.Apu.P1.SwEn}");
        }
        File.WriteAllBytes(a[2], WavOut.WavBytes(pcm, 2, CdMixer.OutRate));
        Console.WriteLine($"wrote {a[2]}: {pcm.Count / 2.0 / CdMixer.OutRate:F1} s ({mode}, mute {mute})");
        return 0;
    }

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
        ("apu_channels", () =>
        {
            // T13: Apu.OutChannels on must not change the mono stream (the Original path / parity)
            var d = TheData.Value;
            List<double> Mono(bool chans)
            {
                var m = new Machine(d, _ => { });
                var pcm = new List<double>();
                m.Apu.SetOutput(44100, pcm.Add);
                if (chans) m.Apu.OutChannels = (_, _, _, _, _) => { };
                m.Reset();
                True(chans == (m.Apu.OutChannels != null), "Reset carries OutChannels over");
                PlaySound.Play(m, 0x05);
                for (int f = 0; f < 120; f++) { m.SoundUpdate(m); m.Apu.Run(29781); }
                return pcm;
            }
            var a = Mono(false); var b = Mono(true);
            True(a.Count > 80000 && a.SequenceEqual(b), $"mono stream identical with channel output ({a.Count} samples)");
        }),
        ("cd_audio", () =>
        {
            static double Rms(List<short> s, int ch, int skip)
            {
                double sum = 0; int n = 0;
                for (int i = skip * 2 + ch; i < s.Count; i += 2) { sum += (double)s[i] * s[i]; n++; }
                return Math.Sqrt(sum / n);
            }
            List<short> Tone(double hz, double amp, bool stereoSame = true)
            {
                var mx = new CdMixer();
                for (int i = 0; i < CdMixer.InRate / 2; i++)
                {
                    float v = (float)(amp * Math.Sin(2 * Math.PI * hz * i / CdMixer.InRate));
                    mx.PushStereo(v, stereoSame ? v : 0);
                }
                return mx.Take(new List<short>());
            }
            var t1 = Tone(1000, 0.25);
            double want = 0.25 / Math.Sqrt(2) * 1.3 * 32767;
            double db = 20 * Math.Log10(Rms(t1, 0, 2000) / want);
            True(Math.Abs(db) < 0.5, $"1 kHz passes: {db:F2} dB");
            var t30 = Tone(30000, 0.25);
            double rej = 20 * Math.Log10(Rms(t30, 0, 2000) / want);
            True(rej < -60, $"30 kHz (would alias) rejected: {rej:F1} dB");
            Eq(CdMixer.OutRate, t1.Count / 2 * 2, "0.5 s in -> 22050 stereo frames out");
            // silence = dither only
            var mz = new CdMixer();
            for (int i = 0; i < 40000; i++) mz.Push(0, 0, 0, 0, 0);
            True(mz.Take(new List<short>()).All(v => Math.Abs(v) <= 2), "silence stays within dither (+-2 LSB)");
            // panning: a 1 kHz square on pulse 1 is louder left; Stereo off = balanced
            (double L, double R) Pan(bool stereo)
            {
                var mx = new CdMixer { Stereo = stereo };
                for (int i = 0; i < CdMixer.InRate / 2; i++) mx.Push((i / 88) % 2 == 0 ? 15 : 0, 0, 0, 0, 0);
                var o = mx.Take(new List<short>());
                return (Rms(o, 0, 2000), Rms(o, 1, 2000));
            }
            var ps = Pan(true); var pm = Pan(false);
            True(ps.L > ps.R * 1.4, $"pulse 1 panned left: L {ps.L:F0} R {ps.R:F0}");
            True(Math.Abs(pm.L / pm.R - 1) < 0.01, $"stereo off balanced: L {pm.L:F0} R {pm.R:F0}");
            // real music: CD path level near the original mono path (gain 1.6 there), no clipping, stereo, deterministic
            var d = TheData.Value;
            var cd = WavOut.PlayMusicCd(d, 0x14, 6);
            var cd2 = WavOut.PlayMusicCd(d, 0x14, 6);
            True(cd.SequenceEqual(cd2), "deterministic (fixed dither seed)");
            var m = new Machine(d, _ => { });
            var mono = new List<double>();
            m.Apu.SetOutput(44100, mono.Add); m.Reset(); PlaySound.Play(m, 0x14);
            for (int f = 0; f < 361; f++) { m.SoundUpdate(m); m.Apu.Run(29781); }
            double monoRms = Math.Sqrt(mono.Skip(4410).Average(x => x * x * 1.6 * 1.6)) * 32767;
            double cdRms = (Rms(cd, 0, 4410) + Rms(cd, 1, 4410)) / 2;
            double lvl = 20 * Math.Log10(cdRms / monoRms);
            int peak = cd.Max(v => Math.Abs((int)v));
            Console.WriteLine($"  cd_audio: 1 kHz {db:F2} dB, 30 kHz {rej:F1} dB, music level vs original {lvl:+0.0;-0.0} dB, peak {peak}");
            True(lvl > -4 && lvl < 6, $"music level vs original: {lvl:F1} dB");
            True(peak < 32700, $"no hard clipping: peak {peak}");
            True(Enumerable.Range(0, cd.Count / 2).Any(i => cd[2 * i] != cd[2 * i + 1]), "left and right differ");
            var rv = WavOut.PlayMusicCd(d, 0x14, 3, reverb: true);
            True(!rv.SequenceEqual(cd.Take(rv.Count)), "reverb changes the output");
            var v = VideoSettings.Parse("audio=cd\naudio_stereo=0\naudio_reverb=1\n");
            True(v.CdAudio && !v.AudioStereo && v.AudioReverb, "ini audio keys");
            True(VideoSettings.Parse(v.Serialize()).Serialize() == v.Serialize(), "ini round trip");
            Eq(411, Window.CommandId("audio:cd"), "--menu audio:cd");
            Eq(413, Window.CommandId("reverb"), "--menu reverb");
        }),
        ("win_enhance32", () =>
        {
            var d = TheData.Value;
            var m = new Machine(d, _ => { });
            m.Reset();
            m.Ppu.Tags = new int[256 * 240];
            for (int f = 0; f < 787; f++) m.RunFrame(0);           // the intro picture
            var e16 = new Enhancer(m.Ppu.Chr, d.NesRgb);
            var e32 = new Enhancer(m.Ppu.Chr, d.NesRgb) { Mode32 = true };
            var h16 = new uint[Enhancer.W * Enhancer.H]; var h32 = new uint[Enhancer.W * Enhancer.H];
            e16.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, h16);
            e32.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, h32);
            int c16 = h16.Distinct().Count(), c32 = h32.Distinct().Count();
            True(c32 > c16 * 4, $"32-bit has far more colours: {c16} -> {c32}");
            for (int r = 0; r < 100; r++) e32.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, h32);   // warm-up (tiered JIT)
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int r = 0; r < 50; r++) e32.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, h32);
            double ms = sw.Elapsed.TotalMilliseconds / 50;
            Console.WriteLine($"  win_enhance32: Render {ms:F1} ms / frame, colours 16-bit {c16} -> 32-bit {c32}");
            True(ms < 10, $"32-bit render fast enough: {ms:F1} ms");
            // one-colour frame stays one colour (tags of real tiles; lighting / AA / shadows must not reveal anything)
            var flat = Enumerable.Repeat((byte)0x0F, 256 * 240).ToArray();
            e32.Render(flat, m.Ppu.Tags, m.Ppu.Pal, h32);
            uint black = (uint)d.NesRgb[0x0F] & 0xFFFFFF;
            True(h32.All(c => c == black), "one-colour frame stays one colour");
            // shadows: a 16x16 sprite block (colour $16) on a $21 background casts a shadow down-right, none up-left;
            // the same sprite in the background colour casts none
            var fb = Enumerable.Repeat((byte)0x21, 256 * 240).ToArray();
            var tags = new int[256 * 240];
            for (int y = 100; y < 116; y++)
                for (int x = 100; x < 116; x++) { fb[y * 256 + x] = 0x16; tags[y * 256 + x] = Ppu.TagValid | 5 << 19 | 1; }
            e32.Render(fb, tags, m.Ppu.Pal, h32);
            uint bg = (uint)d.NesRgb[0x21] & 0xFFFFFF;
            static int Lum(uint c) => (int)((c >> 16 & 255) + (c >> 8 & 255) + (c & 255));
            int under = Lum(h32[(117 * 4) * Enhancer.W + 110 * 4]), far = Lum(h32[(30 * 4) * Enhancer.W + 30 * 4]);
            int above = Lum(h32[(97 * 4) * Enhancer.W + 97 * 4]);
            True(under < far - 30, $"shadow below-right: {under} vs {far}");
            Eq(Lum(bg), above, "no shadow up-left");
            for (int y = 100; y < 116; y++) for (int x = 100; x < 116; x++) fb[y * 256 + x] = 0x21;
            e32.Render(fb, tags, m.Ppu.Pal, h32);
            True(h32.All(c => c == bg), "a sprite in the background colour casts no shadow (stays hidden)");
            var v = VideoSettings.Parse("graphics=enhanced32\n");
            True(v.Enhanced && v.Enhanced32 && v.Serialize().Contains("graphics=enhanced32"), "ini graphics=enhanced32");
            Eq(273, Window.CommandId("graphics:32bit"), "--menu graphics:32bit");
        }),
        ("win_enhance_hd", () =>
        {
            // T18 HD: the CPU half of the GPU renderer (Gpu.cs needs Direct3D: checked under Wine, win_video_check hd)
            var d = TheData.Value;
            var m = new Machine(d, _ => { });
            m.Reset();
            m.Ppu.Tags = new int[256 * 240];
            for (int f = 0; f < 787; f++) m.RunFrame(0);           // the intro picture
            var e16 = new Enhancer(m.Ppu.Chr, d.NesRgb);
            var ehd = new Enhancer(m.Ppu.Chr, d.NesRgb) { ModeHd = true };
            var h16 = new uint[Enhancer.W * Enhancer.H]; var alb = new uint[Enhancer.W * Enhancer.H];
            e16.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, h16);
            ehd.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, alb);
            // albedo = no baked shading: every colour is an NES colour or a dither 50/50 mix, fewer than the 16-bit look
            True(alb.Distinct().Count() < h16.Distinct().Count(), $"albedo unshaded: {alb.Distinct().Count()} colours vs 16-bit {h16.Distinct().Count()}");
            var hgt = ehd.HdHeight!;
            Eq(512 * 480, hgt.Length, "height model 512x480");
            True(hgt.All(v => v >= 0 && v <= 1.0001f), "height in 0..1");
            True(hgt.Distinct().Count() > 20, $"height has relief ({hgt.Distinct().Count()} levels)");
            for (int r = 0; r < 100; r++) ehd.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, alb);   // warm-up (tiered JIT)
            var sw = System.Diagnostics.Stopwatch.StartNew();
            for (int r = 0; r < 50; r++) ehd.Render(m.Ppu.Fb, m.Ppu.Tags, m.Ppu.Pal, alb);
            double ms = sw.Elapsed.TotalMilliseconds / 50;
            Console.WriteLine($"  win_enhance_hd: CPU part {ms:F1} ms / frame (albedo + height model + lift)");
            True(ms < 10, $"HD CPU part fast enough: {ms:F1} ms");
            // one-colour frame: flat height, nothing lifted (hidden shapes stay hidden)
            var flat = Enumerable.Repeat((byte)0x0F, 256 * 240).ToArray();
            ehd.Render(flat, m.Ppu.Tags, m.Ppu.Pal, alb);
            True(ehd.HdHeight!.Distinct().Count() == 1, "one-colour frame: flat height model");
            True(ehd.HdLift.All(v => v == 0), "one-colour frame: no sprite lifted");
            // a visible sprite stands up; the same sprite in the background colour does not
            var fb = Enumerable.Repeat((byte)0x21, 256 * 240).ToArray();
            var tags = new int[256 * 240];
            for (int y = 100; y < 116; y++)
                for (int x = 100; x < 116; x++) { fb[y * 256 + x] = 0x16; tags[y * 256 + x] = Ppu.TagValid | 5 << 19 | 1; }
            ehd.Render(fb, tags, m.Ppu.Pal, alb);
            Eq(255, (int)ehd.HdLift[108 * 256 + 108], "visible sprite lifted");
            Eq(0, (int)ehd.HdLift[50 * 256 + 50], "background not lifted");
            for (int y = 100; y < 116; y++) for (int x = 100; x < 116; x++) fb[y * 256 + x] = 0x21;
            ehd.Render(fb, tags, m.Ppu.Pal, alb);
            True(ehd.HdLift.All(v => v == 0), "a sprite in the background colour is not lifted (stays hidden)");
            var v = VideoSettings.Parse("graphics=hd\n");
            True(v.Enhanced && v.Hd && !v.Enhanced32 && v.Serialize().Contains("graphics=hd"), "ini graphics=hd");
            Eq(274, Window.CommandId("graphics:hd"), "--menu graphics:hd");
        }),
        ("win_hd_scene", () =>
        {
            // T19 HD scene analysis (HdScene.cs); line scroll from the T20 scene raster
            var d = TheData.Value;
            var m2 = new Machine(d, _ => { });
            m2.Reset();
            m2.Ppu.Capture = new VideoCapture();
            for (int f = 0; f < 400; f++) m2.RunFrame(0);
            var sr = new SceneRaster(m2.Ppu.Chr);
            sr.Build(m2.Ppu.Capture);
            True(sr.LineScroll.Any(v => v != 0), "line scroll recorded");

            Eq("0,5,2,1,3,4", string.Join(",", new[] { 0x0F, 0x2A, 0x21, 0x00, 0x30, 0x17 }.Select(HdScene.Kind)), "material kinds");
            // synthetic gameplay frame: green ground, a 20x20 black pit, a 1-pixel black line, HUD text under a scroll split
            var fb = Enumerable.Repeat((byte)0x2A, 256 * 240).ToArray();
            var tags = new int[256 * 240];
            for (int i = 0; i < tags.Length; i++) tags[i] = Ppu.TagValid | 1;
            for (int y = 50; y < 70; y++) for (int x = 50; x < 70; x++) fb[y * 256 + x] = 0x0F;
            for (int x = 10; x < 200; x++) fb[120 * 256 + x] = 0x0F;
            for (int y = 200; y < 208; y++) for (int x = 20; x < 100; x++) tags[y * 256 + x] = Ppu.TagValid | 0x38 << 6;
            // v at each line's start as the PPU makes it: world y = top + line (coarse Y / fine Y), x = coarse X + NT bit
            static int V(int wx, int wy) => (wx >> 3 & 31) | (wx & 256) << 2 | (wy % 240 >> 3) << 5 | (wy % 240 & 7) << 12 | (wy >= 240 ? 0x800 : 0);
            var ls = new int[240];
            void Scroll(int wx) { for (int y = 0; y < 240; y++) ls[y] = y < 190 ? V(wx, y) : V(0, 160 + y); }   // HUD: another scroll
            Scroll(0);
            var pal = new byte[32];
            var lift = new byte[256 * 240];
            var sc = new HdScene(d.NesRgb);
            sc.Build(fb, tags, pal, lift, ls, 0);
            Eq((0f, 0f, 1f), (sc.Lines[100 * 4 + 2], sc.Lines[185 * 4 + 2], sc.Lines[204 * 4 + 2]), "UI lines = text under the split only");
            True(sc.Play == 1 && sc.VoidGate > 0.9f, $"gameplay: play {sc.Play}, void gate {sc.VoidGate}");
            True(sc.Outdoor > 0.9f, $"green ground = outdoor ({sc.Outdoor})");
            Eq(HdScene.FVoid, (byte)(sc.Info[(60 * 256 + 60) * 4 + 2] & HdScene.FVoid), "pit is void");
            Eq(0, sc.Info[(120 * 256 + 100) * 4 + 2] & HdScene.FVoid, "a 1-pixel black line is not void");
            // camera: a jump is a cut; then 8-pixel steps across the nametable edge (coarse X 31 -> 0 with the NT bit)
            Scroll(240); sc.Build(fb, tags, pal, lift, ls, 1);
            Eq(0f, sc.CamX, "a 240-pixel jump is a cut (no camera motion)");
            Scroll(248); sc.Build(fb, tags, pal, lift, ls, 2);
            Scroll(256); sc.Build(fb, tags, pal, lift, ls, 3);
            Scroll(264); sc.Build(fb, tags, pal, lift, ls, 4);
            Eq(24f, sc.CamX, "camera follows the scroll across the nametable edge (248 -> 256 -> 264)");
            // no HUD split (title, cut-scenes): no world effects
            var sc2 = new HdScene(d.NesRgb);
            for (int y = 0; y < 240; y++) ls[y] = V(0, y);
            sc2.Build(fb, tags, pal, lift, ls, 0);
            True(sc2.Play == 0 && sc2.VoidGate == 0 && sc2.Outdoor == 0, $"no HUD: no pits, clouds or dust (play {sc2.Play}, gate {sc2.VoidGate}, outdoor {sc2.Outdoor})");

            var v = VideoSettings.Parse("graphics=hd\nhd_effects=parallax,detail\n");
            True(v.HdParallax && !v.HdShadows && !v.HdAtmosphere && v.HdDetail && v.Serialize().Contains("hd_effects=parallax,detail"), "ini hd_effects");
            True(new VideoSettings().Serialize().Contains("hd_effects=parallax,shadows,atmosphere,detail,"), "all effects on by default");
            Eq(276, Window.CommandId("hd:shadows"), "--menu hd:shadows");
        }),
        ("win_scene_raster", () =>
        {
            // T20 HD engine stage 1: the scene raster draws every frame from the captured video state exactly as the PPU
            // does (Fb + Tags, sprite limit on), Capture never changes Fb, and NoPixels never changes the game
            var d = TheData.Value;
            long frames = 0, events = 0, versions = 0;
            var bad = new List<string>();
            void Run(Machine a, Machine b, Func<int, int> keys, int f0, int n, string name)
            {
                a.Ppu.Tags = new int[256 * 240];
                a.Ppu.Capture = new VideoCapture();
                b.Ppu.Capture = new VideoCapture();
                b.Ppu.NoPixels = true;
                var ra = new SceneRaster(a.Ppu.Chr); var rb = new SceneRaster(b.Ppu.Chr);
                for (int f = f0; f < f0 + n; f++)
                {
                    int k = keys(f);
                    a.RunFrame(k); b.RunFrame(k);
                    ra.Build(a.Ppu.Capture); rb.Build(b.Ppu.Capture);
                    frames++;
                    events += a.Ppu.Capture.Events.Sum(e => e.Count);
                    versions += a.Ppu.Capture.Vram.Count + a.Ppu.Capture.Pal.Count;
                    string why = !ra.Fb.AsSpan().SequenceEqual(a.Ppu.Fb) ? "Fb"
                        : !ra.Tags.AsSpan().SequenceEqual(a.Ppu.Tags) ? "Tags"
                        : !rb.Fb.AsSpan().SequenceEqual(ra.Fb) ? "NoPixels raster"
                        : string.Join(',', Csv.CompareRow(a, f)) != string.Join(',', Csv.CompareRow(b, f)) ? "NoPixels game state" : "";
                    if (why == "") continue;
                    int first = Enumerable.Range(0, 256 * 240).FirstOrDefault(i => ra.Fb[i] != a.Ppu.Fb[i] || ra.Tags[i] != a.Ppu.Tags[i]);
                    bad.Add($"{name} f{f}: {why} (first pixel {first & 255},{first >> 8})");
                    break;
                }
            }
            var keys = Keys.Parse("1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80 2430:RIGHT:200".Split(' '));
            var a0 = new Machine(d, _ => { }); var b0 = new Machine(d, _ => { });
            a0.Reset(); b0.Reset();
            Run(a0, b0, f => keys.TryGetValue(f, out int v) ? v : 0, 214, 2500, "boot");
            string dir = Path.Combine(Data.FindDir(), "..", "test", "states");
            foreach (string p in Directory.GetFiles(dir, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                var st = StateFile.Load(p);
                var a = new Machine(d, _ => { }); var b = new Machine(d, _ => { });
                a.Reset(); b.Reset(); a.LoadState(st); b.LoadState(st);
                int f0 = st.Frame;
                Run(a, b, f => (f / 23 % 5) switch { 0 => 0x01, 1 => 0x80, 2 => 0x04, 3 => 0x40, _ => 0 }, f0, 90, Path.GetFileNameWithoutExtension(p));
            }
            Console.WriteLine($"  win_scene_raster: {frames} frames, {events} mid-line writes, {versions} nametable + palette copies, {bad.Count} bad");
            foreach (string s in bad.Take(12)) Console.WriteLine($"    {s}");
            Eq(0, bad.Count, "scene raster = PPU on every frame");
            True(events > 0, "mid-line writes seen");
        }),
        ("win_hd_engine", () =>
        {
            // T20 HD engine: no-flicker sprite layer, the GPU layer words (Gpu.Layers) and the pack atlas slots
            var d = TheData.Value;
            string dir = Path.Combine(Data.FindDir(), "..", "test", "states");
            long extra = 0, checkedPx = 0;
            bool superset = true;
            SceneRaster? last = null; Machine? lastM = null;
            int gaps = 0; bool gapOk = true; var padded = new List<string>();
            int lifeN = 0, forceN = 0; bool meterOk = true;
            foreach (string p in Directory.GetFiles(dir, "*.json").OrderBy(p => p, StringComparer.Ordinal))
            {
                var st = StateFile.Load(p);
                var m = new Machine(d, _ => { });
                m.Reset(); m.LoadState(st);
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var lim = new SceneRaster(m.Ppu.Chr); var all = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                for (int f = 0; f < 30; f++)
                {
                    m.RunFrame(0);
                    lim.Build(m.Ppu.Capture); all.Build(m.Ppu.Capture);
                    for (int i = 0; i < 256 * 240; i++)
                    {
                        if (lim.Sp[i] != 0 && (all.Sp[i] != lim.Sp[i] || all.SpTag[i] != lim.SpTag[i])) superset = false;
                        if (lim.Sp[i] == 0 && all.Sp[i] != 0) extra++;
                    }
                    checkedPx += 256 * 240;
                }
                if (Path.GetFileNameWithoutExtension(p) == "town02_enter") { last = all; lastM = m; }
                // T33 HUD split gap: under the HUD's fixed scroll, a seam line + at most a short padding band
                var (gf, gt) = Gpu.SplitGap(all);
                if (gt > 0)
                {
                    gaps++;
                    if (gt - gf > 1) padded.Add($"{Path.GetFileNameWithoutExtension(p)} {gf}-{gt - 1}");
                    gapOk &= gt >= 180 && gt <= 200 && gt - gf >= 1 && gt - gf <= 24;
                    // T35 meters under the HUD: LIFE frame from column 6 (x 46..48), fill inside it, FORCE from x 48
                    var scm = new HdScene(d.NesRgb);
                    scm.Build(all.Fb, all.Tags, all.LinePal[239], new byte[61440], all.LineScroll, 0);
                    var mt = Gpu.Meters(all, scm.HudFill, m.Mem.RamBytes[0x03C1]);
                    if (mt[1] > 0) { lifeN++; meterOk &= mt[0] >= 40 && mt[0] <= 48 && mt[1] > 60 && mt[2] > mt[0] && mt[2] < mt[1] && mt[3] > gt && mt[3] < gt + 12; }
                    if (mt[5] > 0) { forceN++; meterOk &= mt[4] == 48 && mt[5] > 64 && mt[6] > mt[3]; }
                }
            }
            Console.WriteLine($"  win_hd_engine: no flicker shows {extra} more sprite pixels in {checkedPx / 61440} frames");
            True(superset, "no-flicker keeps every sprite pixel the 8-sprite limit shows");
            Console.WriteLine($"  win_hd_engine: HUD split gap in {gaps} states, padding band in {padded.Count}: {string.Join(", ", padded.Take(10))}");
            True(gaps > 40 && gapOk, "HUD split gap under the HUD's fixed scroll, at most 24 lines");
            Console.WriteLine($"  win_hd_engine: HUD meters: LIFE in {lifeN} states, FORCE in {forceN}");
            True(lifeN > 40 && forceN > 40 && meterOk, "LIFE / FORCE meters found under the HUD, fill inside the frame");
            // layer words of a town frame
            var r = last!;
            var sc = new HdScene(d.NesRgb);
            var lift = new byte[256 * 240];
            sc.Build(r.Fb, r.Tags, r.LinePal[239], lift, r.LineScroll, 0);
            int tagged = Enumerable.Range(0, 256 * 240).First(i => (r.BgTag[i] & Ppu.TagValid) != 0 && r.Bg[i] != 0 && (sc.Info[i * 4 + 2] & HdScene.FUi) == 0);
            int tile = r.BgTag[tagged] & 0x1FFF, g = r.BgTag[tagged] >> 19 & 7;
            var pal = r.LinePal[tagged >> 8];
            long key = Enhancer.ArtKey(tile, pal[g * 4 + 1], pal[g * 4 + 2], pal[g * 4 + 3]);
            var art = new Dictionary<long, uint[]> { [key] = new uint[32 * 32], [Enhancer.ArtKey(0x1FFF, 1, 2, 3)] = new uint[32 * 32] };
            var slots = new Dictionary<long, int>();
            var tileArt = Gpu.BuildSlots(art, r.Tags.Length > 0 ? lastM!.Ppu.Chr.Length / 16 : 0, slots);
            True(tileArt[tile] && slots[key] == 0 && slots.Count == 2, "atlas slots by key");
            var lay = new byte[256 * 240 * 4]; var am = new uint[256 * 240 * 2];
            Gpu.Layers(r, sc, slots, Gpu.BuildFades(slots), tileArt, lay, am);
            {
                // CPU side of one HD frame (raster + scene analysis + sprite lift + layer words), after JIT warm-up
                var enhT = new Enhancer(lastM!.Ppu.Chr, d.NesRgb);
                var fullArt = BuiltinArt.FromDirectory(Path.Combine(Data.FindDir(), "..", "..", "CSHARP-WINDOWS", "packs"), new List<string>());
                var fs = new Dictionary<long, int>(); var fta = Gpu.BuildSlots(fullArt, 8192, fs); var ff = Gpu.BuildFades(fs);
                var lay2 = new byte[lay.Length]; var am2 = new uint[am.Length];
                var parts = new double[4];
                var swp = new System.Diagnostics.Stopwatch();
                void Part(int k, Action a) { swp.Restart(); a(); parts[k] += swp.Elapsed.TotalMilliseconds; }
                void One()
                {
                    Part(0, () => r.Build(lastM.Ppu.Capture!));
                    Part(1, () => enhT.LiftHd(r.Fb, r.Tags));
                    Part(2, () => sc.Build(r.Fb, r.Tags, r.LinePal[239], enhT.HdLift, r.LineScroll, 1));
                    Part(3, () => Gpu.Layers(r, sc, fs, ff, fta, lay2, am2));
                }
                for (int k = 0; k < 60; k++) One();
                Array.Clear(parts);
                for (int k = 0; k < 60; k++) One();
                double msc = parts.Sum() / 60;
                Console.WriteLine($"  win_hd_engine: CPU side {msc:F2} ms / frame: raster {parts[0] / 60:F2}, lift {parts[1] / 60:F2}, scene {parts[2] / 60:F2}, layers {parts[3] / 60:F2}");
                True(msc < 5, $"HD CPU side fast enough ({msc:F2} ms)");
            }
            bool ok = true;
            int arts = 0, hud = 0;
            for (int i = 0; i < 256 * 240; i++)
            {
                int o = i * 4;
                ok &= (lay[o + 2] != 0 || (lay[o] & 63) == (r.BgCol[i] & 63)) && ((lay[o] & 0x40) != 0) == (r.Bg[i] != 0);
                ok &= r.Sp[i] == 0 ? lay[o + 1] == 0 : (lay[o + 1] & 63) == (r.SpCol[i] & 63) && ((lay[o + 1] & 0x80) != 0) == ((r.Sp[i] & 0x40) != 0);
                if ((lay[o] & 0x80) != 0) hud++;
                if (am[i * 2] != 0) { arts++; ok &= (am[i * 2] & 0x7FFF) == 1 && (r.BgTag[i] & 0x1FFF) == tile; }
            }
            True(ok, "layer words = the raster's layers");
            True(hud > 1000, $"HUD pixels flagged ({hud})");
            True(arts >= 8 && (am[tagged * 2] >> 15 & 7) == (r.BgTag[tagged] >> 13 & 7), $"pack tile under its pixels ({arts}), with the pixel's column");
            {
                // T32 clean edges: overscan lines + masked left columns exact black, everything else unchanged
                var lay3 = new byte[lay.Length]; var am3 = new uint[am.Length];
                Gpu.Layers(r, sc, slots, Gpu.BuildFades(slots), tileArt, lay3, am3, edges: true);
                bool eOk = true; int black = 0, clipLines = 0, band = Gpu.EdgeBand(r, sc);
                var (g0, g1) = Gpu.SplitGap(r);
                for (int y = 0; y < 240; y++)
                {
                    if (r.LeftClip[y]) clipLines++;
                    for (int x = 0; x < 256; x++)
                    {
                        int i = y * 256 + x, o = i * 4;
                        bool edge = y >= band || (y >= g0 && y < g1) || (x < 8 && r.LeftClip[y]);
                        if (edge) { black++; eOk &= lay3[o] == 0x8F && lay3[o + 1] == 0 && am3[i * 2] == 0 && am3[i * 2 + 1] == 0; }
                        else eOk &= lay3[o] == lay[o] && lay3[o + 1] == lay[o + 1] && lay3[o + 2] == lay[o + 2] && am3[i * 2] == am[i * 2] && am3[i * 2 + 1] == am[i * 2 + 1];
                    }
                }
                Console.WriteLine($"  win_hd_engine: clean edges: {black} black pixels, bottom band from line {band}, {clipLines} masked-column lines");
                True(eOk && black >= 8 * 256, "clean edges: overscan + masked column black, rest unchanged");
            }
            var v = VideoSettings.Parse("graphics=hd\nhd_effects=reflections,noflicker\n");
            True(v.HdReflections && !v.HdLights && v.HdNoFlicker && !v.HdShadows, "ini hd_effects (T20 switches)");
            True(new VideoSettings().Serialize().Contains("hd_effects=parallax,shadows,atmosphere,detail,reflections,lights,noflicker,art,vsync,edges,hud"), "all on by default");
            Eq(283, Window.CommandId("hd:vsync"), "--menu hd:vsync");
            Eq(287, Window.CommandId("hd:edges"), "--menu hd:edges");
            Eq(288, Window.CommandId("hd:hud"), "--menu hd:hud");
            Eq(284, Window.CommandId("hd:fast"), "--menu hd:fast");
            True(VideoSettings.Parse("hd_quality=balanced\n").HdQuality == 1 && new VideoSettings().Serialize().Contains("hd_quality=auto"), "ini hd_quality (T62: auto by default)");
            Eq(281, Window.CommandId("hd:noflicker"), "--menu hd:noflicker");
            // T22 scene weather: icy / hot / outdoor per scene
            var weather = new Dictionary<string, (float Icy, float Hot, float Out)>();
            foreach (string name in new[] { "group29", "finalboss", "town02_enter", "group7c" })
            {
                var st = StateFile.Load(Path.Combine(dir, name + ".json"));
                var m = new Machine(d, _ => { });
                m.Reset(); m.LoadState(st);
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var rr = new SceneRaster(m.Ppu.Chr);
                var s2 = new HdScene(d.NesRgb);
                var hist = new int[64];
                for (int f = 0; f < 60; f++)
                {
                    m.RunFrame(0); rr.Build(m.Ppu.Capture);
                    s2.Build(rr.Fb, rr.Tags, rr.LinePal[239], lift, rr.LineScroll, f);
                }
                for (int i = 0; i < 256 * 200; i++) hist[rr.Fb[i] & 63]++;
                weather[name] = (s2.Icy, s2.Hot, s2.Outdoor);
                // T29 HUD panel: found under the playfield, a real (not black) fill colour that most HUD pixels have
                True(s2.HudFill is >= 0 and not 0x0F && s2.HudTop > 150 && s2.HudBottom < 240 && s2.HudBottom > s2.HudTop + 16, $"{name}: HUD panel fill {s2.HudFill:X2} lines {s2.HudTop}-{s2.HudBottom}");
                Console.WriteLine($"  {name}: icy {s2.Icy:F2} hot {s2.Hot:F2} outdoor {s2.Outdoor:F2}; colours " +
                                  string.Join(' ', Enumerable.Range(0, 64).OrderByDescending(c => hist[c]).Take(5).Select(c => $"{c:X2}:{hist[c] * 100 / 51200}%")));
            }
            True(weather["group29"].Icy > 0.5f && weather["group29"].Hot < 0.05f, "ice cave: snow, no embers");
            True(weather["finalboss"].Hot > 0.5f && weather["finalboss"].Icy < 0.05f, "fire hall: embers, no snow");
            True(weather["town02_enter"].Out > 0.5f && weather["town02_enter"].Icy < 0.05f && weather["town02_enter"].Hot < 0.05f, "town: leaves only");
            True(weather["group7c"].Icy < 0.05f && weather["group7c"].Hot < 0.05f && weather["group7c"].Out < 0.05f, "red cave: no weather");
        }),
        ("win_builtin_art", () =>
        {
            // T25 built-in starter packs: every pack PNG decodes into a tile, no name collisions, a user tile wins
            string packs = Path.Combine(Data.FindDir(), "..", "..", "CSHARP-WINDOWS", "packs");
            int files = Directory.GetDirectories(packs).Sum(d => Directory.GetFiles(d, "*.png").Length);
            var errors = new List<string>();
            var art = BuiltinArt.FromDirectory(packs, errors);
            Console.WriteLine($"  win_builtin_art: {files} pack files -> {art.Count} tiles, {errors.Count} errors");
            Eq(0, errors.Count, "all pack files decode");
            Eq(files, art.Count, "no two packs paint the same tile + colours");
            True(art.Count <= Gpu.MaxSlots, "fits the GPU atlas");
            long key = art.Keys.First();
            var mine = new uint[32 * 32];
            var merged = BuiltinArt.Merge(art, new Dictionary<long, uint[]> { [key] = mine });
            True(merged.Count == art.Count && ReferenceEquals(merged[key], mine), "user pack overrides a built-in tile");
            Eq(0, BuiltinArt.Merge(null, new Dictionary<long, uint[]>()).Count, "built-in off + no user pack = no art");
            var v = VideoSettings.Parse("graphics=hd\nhd_effects=parallax\n");
            True(!v.HdArt && new VideoSettings().HdArt, "ini: art on by default, off when not listed");
            Eq(282, Window.CommandId("hd:art"), "--menu hd:art");
            // T27 fades: a tile painted for $30 $21 $11 is found in fade steps 1-2 ($20 $11 $01, $10 $01 $0F) with its step;
            // step 3 ($00 $0F $0F) too; an all-black step is not; a faded key with its own tile keeps that tile
            Eq((0x20, 0x0F, 0x0F), (BuiltinArt.Fade(0x30, 1), BuiltinArt.Fade(0x0F, 1), BuiltinArt.Fade(0x05, 1)), "NES fade rule");
            var sl = new Dictionary<long, int> { [Enhancer.ArtKey(5, 0x30, 0x21, 0x11)] = 0, [Enhancer.ArtKey(5, 0x10, 0x01, 0x0F)] = 1 };
            var fd = Gpu.BuildFades(sl);
            Eq(1 << 16, fd[Enhancer.ArtKey(5, 0x20, 0x11, 0x01)], "step 1 -> slot 0");
            False(fd.ContainsKey(Enhancer.ArtKey(5, 0x10, 0x01, 0x0F)), "a faded key with its own tile keeps it");
            Eq(1 | 1 << 16, fd[Enhancer.ArtKey(5, 0x00, 0x0F, 0x0F)], "step 3 of slot 0 = step 1 of slot 1: the nearer painting wins");
            var ta = new bool[8192]; ta[5] = true;
            var fpal = new byte[32]; fpal[1] = 0x20; fpal[2] = 0x11; fpal[3] = 0x01;
            uint w = Gpu.ArtWord(Ppu.TagValid | 5, fpal, sl, fd, ta);
            Eq((1u, 1u), (w & 0x7FFF, w >> 23 & 3), "art word: slot 0 + 1, fade step 1");
        }),
        ("win_cheats", () =>
        {
            // T36 cheats: the HP-bar tiles = the game's own HudHpBar buffer ($6060) in every gameplay state; the cheats
            // set what they promise in gameplay only; ini round trip; menu names
            var d = TheData.Value;
            string dir = Path.Combine(Data.FindDir(), "..", "test", "states");
            // the game's own HudHpBar (ported, 26:8CC0) on 400 random max / HP pairs vs Cheats.HpBarTiles (test states
            // poke HP without redrawing the bar, so their nametables are no reference)
            var mh = new Machine(d, _ => { });
            mh.Reset(); mh.LoadState(StateFile.Load(Path.Combine(dir, "group09.json")));
            var rng = new Random(36);
            int same = 0;
            for (int k = 0; k < 400; k++)
            {
                int max = rng.Next(1, 256), hp = rng.Next(0, max + 1);
                mh.Mem.RamBytes[0x03C0] = (byte)max; mh.Mem.RamBytes[0x03C1] = (byte)hp;
                int steps = 0;
                foreach (var _ in ReC.Game.States.Death.HudHpBar(mh)) if (++steps > 4) break;
                var t = Cheats.HpBarTiles(max, hp, i => Prg.PrgByte(mh, 26, 0x8D49 + i));
                if (Enumerable.Range(0, 17).All(i => mh.Mem.Wram[0x60 + i] == t[i])) same++;
                mh.RunFrame(0);                                      // flushes the PPU queue (HudHpBar waits for it)
            }
            Console.WriteLine($"  win_cheats: HP-bar tiles = the game's HudHpBar for {same} / 400 random max / HP pairs");
            Eq(400, same, "HP-bar tiles as HudHpBar draws them");
            // the cheats on a gameplay state
            var mc = new Machine(d, _ => { });
            mc.Reset(); mc.LoadState(StateFile.Load(Path.Combine(dir, "group09.json")));
            mc.RunFrame(0);
            var r = mc.Mem.RamBytes;
            r[0x03C1] = 1; r[0x0708] = 0; r[0x0702] = 5; r[0x0703] = 0; r[0x0710] = (byte)(r[0x0710] & 0xF0 | 3); r[0x06C0] = 0;
            var c = new Cheats();
            c.Parse("life,magic,gold,status,charge,xp");
            True(c.Life && c.Magic && c.Gold && c.Status && c.Charge && c.Xp && c.Serialize() == "life,magic,gold,status,charge,xp", "cheats ini round trip");
            c.Apply(mc, true);
            int cap = Prg.PrgByte(mc, 26, 0x9D7E + Math.Min((int)r[0x0719], 2));
            True(r[0x03C1] == r[0x03C0] && r[0x0708] == r[0x0709] && (r[0x0702] | r[0x0703] << 8) == 60000 && (r[0x0710] & 0x0F) == 0 &&
                 r[0x06C0] == cap && (r[0x0704] | r[0x0705] << 8) == (r[0x0706] | r[0x0707] << 8) - 1, "cheats set HP, MP, gold, status, charge, XP");
            var tb = Cheats.HpBarTiles(r[0x03C0], r[0x03C1], k => Prg.PrgByte(mc, 26, 0x8D49 + k));
            True(Enumerable.Range(0, 17).All(i => mc.Ppu.Vram[mc.Ppu.NtIndex(0x2B26 + i)] == tb[i]), "infinite life redraws the HP bar");
            for (int f = 0; f < 60; f++) { mc.RunFrame(0); c.Apply(mc, false); }
            True(r[0x03C1] == r[0x03C0], "life stays full while playing");
            r[0x40] = 2; r[0x03C1] = 3; c.Apply(mc, false);
            True(r[0x03C1] == 3, "no cheats outside gameplay");
            True(!new Cheats().Any && new Cheats().Serialize() == "none" && VideoSettings.Parse("cheats=gold\n").Cheats.Gold, "off by default, ini key");
            Eq(602, Window.CommandId("cheat:gold"), "--menu cheat:gold");
            // T38 turbo: B held = 3 frames down, 3 up; other buttons untouched; ini
            var pulse = Enumerable.Range(0, 12).Select(f => VideoSettings.Turbo(0x40 | 0x01, f, false, true)).ToArray();
            True(pulse.All(b => (b & 0x01) != 0) && pulse.Count(b => (b & 0x40) != 0) == 6 && (pulse[0] & 0x40) != 0 && (pulse[3] & 0x40) == 0, "turbo B pulses, D-pad kept");
            True(VideoSettings.Turbo(0x80, 4, false, false) == 0x80 && VideoSettings.Parse("turbo=a,b\n").TurboA && new VideoSettings().Serialize().Contains("turbo=none"), "turbo off by default, ini");
            Eq(104, Window.CommandId("turbo:b"), "--menu turbo:b");
            // T40 volume
            True(new VideoSettings().Volume == 100 && VideoSettings.Parse("volume=50\n").Volume == 50 && VideoSettings.Parse("volume=900\n").Volume == 100, "volume ini (clamped)");
            Eq(422, Window.CommandId("volume:50"), "--menu volume:50");
            // T45 CRT look: 3x, the scanline row darker than the others, ini
            var crtOut = new uint[256 * 240 * 9];
            Crt.Apply(Enumerable.Repeat(0x808080u, 256 * 240).ToArray(), 256, 240, crtOut);
            True((crtOut[2 * 768] & 0xFF00) < (crtOut[768] & 0xFF00) && VideoSettings.Parse("crt=1\n").Crt && !new VideoSettings().Crt, "CRT scanlines, ini");
            Eq(290, Window.CommandId("crt"), "--menu crt");
        }),
        ("win_osd", () =>
        {
            // T37 on-screen display: nothing by default, the pause screen dims everything and puts an opaque panel in the
            // middle, notices / fast forward sit at the top, the GDI blend darkens under the dim
            True(!Osd.Build(false, false, null, new List<string>()) && Osd.Pix.All(p => p == 0), "nothing to show: empty overlay");
            True(Osd.Build(true, false, null, new List<string> { "life" }), "pause screen");
            uint corner = Osd.Pix[5 * Osd.W + 5], panel = Osd.Pix[330 * Osd.W + 110];
            Console.WriteLine($"  win_osd: pause dim alpha {corner >> 24}, panel alpha {panel >> 24}");
            True((corner >> 24) is > 140 and < 180 && (panel >> 24) > 230, "dim ~62 %, panel ~94 % opaque");
            var pic = Enumerable.Repeat(0xFFFFFFu & 0xC0C0C0, 64 * 60).ToArray();
            Osd.Blend(pic, 64, 60);
            True((pic[10 * 64 + 10] & 255) < 0x60, "GDI blend darkens under the dim");
            True(Osd.Build(false, true, "INFINITE LIFE: ON", new List<string>()) && (Osd.Pix[25 * Osd.W + 30] >> 24) > 100 && (Osd.Pix[25 * Osd.W + Osd.W - 50] >> 24) > 100, "notice + fast forward");
            True(Osd.TextWidth("AB", 2) == 22, "text width");
            True(Osd.HelpLines.All(l => 56 + 15 * 12 + Osd.TextWidth(l.What, 2) <= Osd.W - 40 && Osd.TextWidth(l.Key, 2) < 15 * 12), "help lines fit the panel");
            True(Osd.Build(true, false, null, new List<string>(), help: true) && (Osd.Pix[200 * Osd.W + 256] >> 24) > 230, "help panel");
        }),
        ("win_audio_remaster", () =>
        {
            // T28 Remastered audio (CdMixer.Remaster): same loudness as CD quality, no clipping, wider stereo, the
            // triangle's step buzz reduced, the EQ curve as specified, deterministic
            var d = TheData.Value;
            var cd = WavOut.PlayMusicCd(d, 0x14, 8);
            var rm = WavOut.PlayMusicCd(d, 0x14, 8, remaster: true);
            True(rm.SequenceEqual(WavOut.PlayMusicCd(d, 0x14, 8, remaster: true)), "deterministic");
            double Rms(List<short> s) => Math.Sqrt(s.Average(v => (double)v * v));
            double Corr(List<short> s)
            {
                double sl = 0, sr = 0, slr = 0;
                // on the sample-to-sample differences: the mids / highs (the melody voices; bass stays centred on purpose)
                for (int i = 2; i + 1 < s.Count; i += 2)
                {
                    double a = s[i] - s[i - 2], b = s[i + 1] - s[i - 1];
                    sl += a * a; sr += b * b; slr += a * b;
                }
                return slr / Math.Sqrt(sl * sr);
            }
            double db = 20 * Math.Log10(Rms(rm) / Rms(cd));
            int peak = rm.Max(v => Math.Abs((int)v));
            Console.WriteLine($"  win_audio_remaster: music $14 level {db:+0.0;-0.0} dB vs CD, peak {peak}, mid / high L/R correlation {Corr(rm):F3} (CD {Corr(cd):F3})");
            True(Math.Abs(db) < 3, $"loudness within 3 dB of CD ({db:F1})");
            True(peak < 32700, $"no clipping ({peak})");
            True(Corr(rm) < Corr(cd) - 0.05, "wider stereo than CD in the mids / highs");
            // triangle alone (a 4-bit staircase at 110 Hz): less energy above 6 kHz (step buzz) than the CD path
            double Hf(bool remaster)
            {
                var mx = new CdMixer { Remaster = remaster };
                for (int i = 0; i < CdMixer.InRate / 2; i++)
                {
                    double ph = i * 110.0 / CdMixer.InRate % 1;
                    int step = (int)(ph * 32) % 32;
                    mx.Push(0, 0, step < 16 ? 15 - step : step - 16, 0, 0);
                }
                var o = mx.Take(new List<short>());
                double hf = 0, prev = 0;
                for (int i = 0; i < o.Count; i += 2) { double x = o[i]; hf += (x - prev) * (x - prev); prev = x; }   // first difference ~ highs
                return hf / Math.Max(1, o.Sum(v => (double)v * v));
            }
            double hcd = Hf(false), hrm = Hf(true);
            True(hrm < hcd * 0.6, $"triangle buzz reduced: {hrm:E2} vs CD {hcd:E2}");
            var ls = Biquad.LowShelf(120, 2.5); var hs = Biquad.HighShelf(7000, 1.5);
            double Db(double g) => 20 * Math.Log10(g);
            True(Math.Abs(Db(ls.GainAt(20)) - 2.5) < 0.3 && Math.Abs(Db(ls.GainAt(5000))) < 0.2, "low shelf +2.5 dB");
            True(Math.Abs(Db(hs.GainAt(18000)) - 1.5) < 0.3 && Math.Abs(Db(hs.GainAt(200))) < 0.2, "high shelf +1.5 dB");
            var v = VideoSettings.Parse("audio=remaster\n");
            True(v.AudioRemaster && !v.CdAudio && v.Serialize().Contains("audio=remaster"), "ini audio=remaster");
            Eq(414, Window.CommandId("audio:remaster"), "--menu audio:remaster");
        }),
        ("win_audio_engine", () =>
        {
            // T47 audio engine v2 (AudioFx.cs): resampler + rate control instead of dropped blocks, fast forward at
            // ratio 4, headphone crossfeed, channel mutes
            var rnd = new Random(7);
            var src = Enumerable.Range(0, 5000).Select(_ => (float)(rnd.NextDouble() * 2 - 1)).ToList();
            List<float> Run(Resampler rs, List<float> input, double ratio, int chunk)
            {
                var o = new List<float>();
                for (int i = 0; i < input.Count; i += chunk) rs.Process(input.GetRange(i, Math.Min(chunk, input.Count - i)), ratio, o);
                return o;
            }
            var id = Run(new Resampler(1), src, 1, 37);
            True(id.Count == src.Count - 2 && id.SequenceEqual(src.Take(id.Count)), "ratio 1: sample for sample");
            // a 440 Hz sine at 44.1 kHz through ratio 1.005: the ideal sine at the new rate, no seams at chunk edges
            var sine = Enumerable.Range(0, 44100).Select(i => (float)Math.Sin(2 * Math.PI * 440 * i / 44100)).ToList();
            var up = Run(new Resampler(1), sine, 1.005, 333);
            double err = 0;
            for (int k = 0; k < up.Count; k++) err = Math.Max(err, Math.Abs(up[k] - Math.Sin(2 * Math.PI * 440 * k * 1.005 / 44100)));
            True(Math.Abs(up.Count - 44100 / 1.005) < 4, $"ratio 1.005: {up.Count} frames");
            True(err < 1e-3, $"ratio 1.005: max error {err:E1} vs the ideal sine");
            var st = sine.SelectMany(v => new[] { v, -v }).ToList();
            var so = Run(new Resampler(2), st, 0.995, 500);
            True(Enumerable.Range(0, so.Count / 2).All(k => so[2 * k + 1] == -so[2 * k]), "stereo sides resampled alike");
            var ff = Run(new Resampler(1), Enumerable.Repeat(0.25f, 8000).ToList(), 4, 1024);
            True(Math.Abs(ff.Count - 2000) < 3 && ff.All(v => Math.Abs(v - 0.25f) < 1e-6), $"fast forward 4x: {ff.Count} frames, level kept");
            // playtest 3: a backlog spliced out with a crossfade: no step in a 440 Hz sine (a plain cut steps by up to the peak)
            {
                var sn = Enumerable.Range(0, 8000).Select(i => (short)(12000 * Math.Sin(2 * Math.PI * 440 * i / 44100))).ToArray();
                var sp2 = AudioOut.Splice(sn, 1, 3000, 256);
                int maxStep = Enumerable.Range(1, sp2.Length - 1).Max(i => Math.Abs(sp2[i] - sp2[i - 1]));
                int sineStep = Enumerable.Range(1, sn.Length - 1).Max(i => Math.Abs(sn[i] - sn[i - 1]));
                True(sp2.Length == 5000 && maxStep <= sineStep * 2, $"splice: 3000 frames out, steps {maxStep} (the sine's own {sineStep})");
            }
            var rc = new RateControl();
            Eq(1.0, rc.Ratio(1, 2560, 2560), "on target: ratio 1");
            var hi = new RateControl(); double rh = 0;
            for (int i = 0; i < 200; i++) rh = hi.Ratio(1, 20000, 2560);
            True(rh > 1 && rh <= 1 + RateControl.MaxAdjust + RateControl.FarAdjust + 1e-12, $"far too much queued: faster ({rh:F4}), at most +2 %");
            var near = new RateControl(); double rn = 0;
            for (int i = 0; i < 200; i++) rn = near.Ratio(1, 3000, 2560);
            True(rn > 1 && rn <= 1 + RateControl.MaxAdjust, $"a little too much: at most +0.5 % ({rn:F4})");
            var lo = new RateControl(); double rl = 0;
            for (int i = 0; i < 200; i++) rl = lo.Ratio(4, 0, 2560);
            True(Math.Abs(rl - 4 * (1 - RateControl.MaxAdjust)) < 1e-9, "starved at 4x: 4 x 0.995");
            // crossfeed: centred bass unchanged, a hard-left bass reaches the right side at about -14 dB, highs much less
            double Pass(double hz, bool right, bool mono)
            {
                var xf = new Crossfeed(); double pk = 0;
                for (int i = 0; i < 44100; i++)
                {
                    double x = Math.Sin(2 * Math.PI * hz * i / 44100);
                    var (l, r) = xf.Process(x, mono ? x : 0);
                    if (i > 22050) pk = Math.Max(pk, Math.Abs(right ? r : l));
                }
                return 20 * Math.Log10(pk);
            }
            double mb = Pass(100, false, true), lb = Pass(100, true, false), lh = Pass(8000, true, false);
            Console.WriteLine($"  win_audio_engine: crossfeed centre 100 Hz {mb:+0.0;-0.0} dB, left->right 100 Hz {lb:F1} dB, 8 kHz {lh:F1} dB");
            True(Math.Abs(mb) < 0.5, "centred bass unchanged");
            True(lb > -16 && lb < -12, "hard-left bass bleeds to the right");
            True(lh < lb - 10, "highs stay on their side");
            double Level(int mute)
            {
                var mx = new CdMixer { Mute = mute };
                for (int i = 0; i < CdMixer.InRate / 4; i++) mx.Push((i / 88) % 2 == 0 ? 15 : 0, 0, (i / 200) % 16, 0, 0);
                return mx.Take(new List<short>()).Skip(4000).Sum(v => (double)v * v);
            }
            True(Level(1 | 4) < Level(0) * 1e-6 && Level(1) > Level(0) * 0.01 && Level(4) > Level(0) * 0.01 && Level(2) == Level(0), "channel mutes (pulse 1, triangle) silence only that channel (dither left)");
            var v = VideoSettings.Parse("audio_mute=pulse1,noise\ncrossfeed=1\n");
            True(v.AudioMute == 9 && v.Crossfeed && v.Serialize().Contains("audio_mute=pulse1,noise") && v.Serialize().Contains("crossfeed=1"), "ini audio_mute / crossfeed");
            True(new VideoSettings().Serialize().Contains("audio_mute=none"), "ini default: nothing muted");
            Eq(415, Window.CommandId("crossfeed"), "--menu crossfeed");
            Eq(433, Window.CommandId("channel:noise"), "--menu channel:noise");
        }),
        ("win_orchestra", () =>
        {
            // T50 Audio > Orchestrated: instruments played from the APU state
            // 1. pitch: pulse 1 at period 253 (A4 = 440 Hz) -> energy at 440 Hz, not a semitone away
            List<short> Drive(Action<ReC.Nes.Apu> setup, double sec)
            {
                var apu = new ReC.Nes.Apu();
                var mx = new CdMixer { Orchestra = true };
                mx.Chip = () => apu;
                apu.SetOutput(CdMixer.InRate, null);
                apu.OutChannels = mx.Push;
                setup(apu);
                var o = new List<short>();
                for (int i = 0; i < (int)(sec * 60); i++) { apu.Run(29830); mx.Take(o); }
                return o;
            }
            double Goertzel(List<short> s, double hz, int from)
            {
                double w = 2 * Math.Cos(2 * Math.PI * hz / 44100), a = 0, b = 0;
                for (int i = from; i < s.Count; i += 2) { double x = s[i] + s[i + 1]; double c = x + w * a - b; b = a; a = c; }
                return a * a + b * b - w * a * b;
            }
            var a4 = Drive(apu => { apu.Write(0x4015, 0x0F); apu.Write(0x4000, 0xBF); apu.Write(0x4002, 253); apu.Write(0x4003, 0x08); }, 1.0);
            double e440 = Goertzel(a4, 440, 8820), e466 = Goertzel(a4, 466.2, 8820), e415 = Goertzel(a4, 415.3, 8820);
            True(e440 > 20 * e466 && e440 > 20 * e415, $"pulse 1 A4 plays at 440 Hz ({e440:E1} vs {e466:E1} / {e415:E1})");
            // 2. a pulse with the sweep on (a sound effect) keeps the chip's square: odd harmonics dominate
            var fx = Drive(apu => { apu.Write(0x4015, 0x0F); apu.Write(0x4000, 0xBF); apu.Write(0x4001, 0x89); apu.Write(0x4002, 253); apu.Write(0x4003, 0x08); }, 0.05);
            True(fx.Any(v => Math.Abs((int)v) > 2000), "a sweeping pulse (sound effect) still sounds");
            // 3. triangle at period 126 -> 440 Hz as a bass
            var tri = Drive(apu => { apu.Write(0x4015, 0x0F); apu.Write(0x4008, 0xFF); apu.Write(0x400A, 126); apu.Write(0x400B, 0x08); }, 1.0);
            True(Goertzel(tri, 440.4, 8820) > 10 * Goertzel(tri, 466, 8820), "triangle pitch kept");
            // 4. a real song: as loud as CD quality (+-4 dB), no clipping, the same rhythm, deterministic, stereo
            var d = TheData.Value;
            var cd = WavOut.PlayMusicCd(d, 0x14, 10);
            var orc = WavOut.PlayMusicCd(d, 0x14, 10, orchestra: true);
            True(orc.SequenceEqual(WavOut.PlayMusicCd(d, 0x14, 10, orchestra: true)), "deterministic");
            double Rms(List<short> s) => Math.Sqrt(s.Average(v => (double)v * v));
            double db = 20 * Math.Log10(Rms(orc) / Rms(cd));
            int peak = orc.Max(v => Math.Abs((int)v));
            // the same notes at the same time: the melody alone (pulse 1), per 100 ms the energy at 48 semitones (C3-B6),
            // chip vs violins, cosine similarity averaged over windows with sound
            double[] Notes(List<short> s, int k)
            {
                var seg = s.Skip(k * 8820).Take(8820).ToList();
                return Enumerable.Range(0, 48).Select(n => Goertzel(seg, 130.81 * Math.Pow(2, n / 12.0), 0)).ToArray();
            }
            var chipM = WavOut.PlayMusicCd(d, 0x14, 10, mute: 30);
            var orcM = WavOut.PlayMusicCd(d, 0x14, 10, orchestra: true, mute: 30);
            var sims = new List<double>();
            for (int k = 0; k < chipM.Count / 8820; k++)
            {
                double[] x = Notes(chipM, k), y = Notes(orcM, k);
                double xy = x.Zip(y, (p, q) => p * q).Sum(), xx = x.Sum(p => p * p), yy = y.Sum(q => q * q);
                if (xx > 0 && yy > 0) sims.Add(xy / Math.Sqrt(xx * yy));
            }
            double rc = sims.Average();
            double sd = 0; for (int i = 0; i + 1 < orc.Count; i += 2) sd += Math.Abs(orc[i] - orc[i + 1]);
            Console.WriteLine($"  win_orchestra: music $14 {db:+0.0;-0.0} dB vs CD, peak {peak}, melody notes match {rc:F2} (cosine over {sims.Count} windows), mean |L-R| {sd / (orc.Count / 2):F0}");
            True(Math.Abs(db) < 4, $"loudness within 4 dB of CD ({db:F1})");
            True(peak < 32700, "no clipping");
            True(rc > 0.7, $"the same notes at the same time ({rc:F2})");
            True(sd / (orc.Count / 2) > 100, "seated in stereo");
            // pops: isolated spikes of the second difference (a step in the waveform) well above its local level; the
            // orchestra must not click more than the chip path does
            int Clicks(List<short> s)
            {
                int n = 0;
                var d2 = new double[s.Count / 2];
                for (int i = 2; i < d2.Length; i++) d2[i] = Math.Abs((s[2 * i] + s[2 * i + 1]) - 2.0 * (s[2 * i - 2] + s[2 * i - 1]) + (s[2 * i - 4] + s[2 * i - 3]));
                double avg = 0;
                for (int i = 2; i < d2.Length; i++)
                {
                    avg += (d2[i] - avg) * 0.002;
                    if (i > 2000 && d2[i] > 1500 && d2[i] > avg * 12) { n++; i += 200; }
                }
                return n;
            }
            var many = new List<short>();               // the drums (noise, DMC samples) left out: percussion is transient by nature
            foreach (int id in new[] { 0x14, 0x15, 0x1B }) many.AddRange(WavOut.PlayMusicCd(d, id, 20, orchestra: true, mute: 8 | 16));
            var manyCd = new List<short>();
            foreach (int id in new[] { 0x14, 0x15, 0x1B }) manyCd.AddRange(WavOut.PlayMusicCd(d, id, 20));
            int co = Clicks(many), cc = Clicks(manyCd);
            Console.WriteLine($"  win_orchestra: clicks in 60 s of music: orchestra {co}, CD path {cc}");
            True(co <= 15, $"orchestra instruments (no drums): almost no clicks ({co} in 60 s; the chip path's square edges count {cc})");
            // playtest 5 (from the recordings): the oscillators' phase grew forever when a step exceeded 1 (frequency above
            // the output rate: a triangle period near 0, effect sweeps) = a huge DC offset on every later note, pinned at
            // the limiter, popping whenever it changed
            {
                double ph = 0, mx = 0;
                for (int i = 0; i < 10000; i++) mx = Math.Max(mx, Math.Abs(Orchestra.Saw(ref ph, 1.7 + i * 0.001)));
                True(mx <= 2.5 && ph >= 0 && ph < 1, $"saw bounded at any step (max {mx:F2})");
                var orch2 = new Orchestra(); var apu2 = new ReC.Nes.Apu();
                apu2.Write(0x4015, 0x0F); apu2.Write(0x4008, 0xFF); apu2.Write(0x400A, 1); apu2.Write(0x400B, 0x08);
                double sum = 0; int n2 = 0;
                for (int i = 0; i < 44100; i++) { var (l, _) = orch2.Render(apu2, 0, 0); if (i > 22050) { sum += l; n2++; } }
                True(Math.Abs(sum / n2) < 0.005, $"a triangle at period 1 (56 kHz): no DC ({sum / n2:F4})");
                var mf = new Machine(d, _ => { }); mf.Reset();
                var stf = StateFile.Load(Path.Combine(Data.FindDir(), "..", "test", "states", "group09.json"));
                mf.LoadState(stf);
                var mixf = new CdMixer { Orchestra = true };
                mixf.Chip = () => mf.Apu;
                mf.Apu.SetOutput(CdMixer.InRate, null);
                mf.Apu.OutChannels = mixf.Push;
                var kf = Keys.Parse("2531:UP:50 2585:RIGHT:330 2931:B:4 2951:B:4 2971:B:4 2990:RIGHT:21 3022:B:3 3040:B:5 3061:RIGHT:29 3101:B:4 3121:B:4 3141:B:4 3161:B:4".Split(' '));
                var fight = new List<short>();
                for (int f = 0; f < 60 * 20; f++) { mf.RunFrame(kf.TryGetValue(2506 + mf.Frame, out int b) ? b : 0); mixf.Take(fight); }
                double worst = 0;
                for (int k = 0; k + 88200 <= fight.Count; k += 88200) worst = Math.Max(worst, Math.Abs(fight.Skip(k).Take(88200).Average(x => (double)x)));
                int clipped = fight.Count(x => Math.Abs((int)x) >= 32700);
                Console.WriteLine($"  win_orchestra: 20 s fight (group09): largest 1 s mean {worst:F0}, {clipped} clipped samples");
                True(worst < 500 && clipped == 0, "the fight that pinned the output at -32700 now stays centred");
            }
            // playtest 4: a direct DMC level jump (0 -> 100, e.g. at a scene change) ramps instead of stepping (a pop)
            {
                var orch = new Orchestra(); var apu = new ReC.Nes.Apu();
                double prevL = 0, maxStep = 0;
                for (int i = 0; i < 4000; i++)
                {
                    var (l, _) = orch.Render(apu, i < 2000 ? 0 : 100, 0);
                    if (i > 10) maxStep = Math.Max(maxStep, Math.Abs(l - prevL));
                    prevL = l;
                }
                Console.WriteLine($"  win_orchestra: DMC jump 0 -> 100: largest step {maxStep:F4} (full scale 1)");
                True(maxStep < 0.01, "a DMC level jump ramps (no pop)");
            }
            var v = VideoSettings.Parse("audio=orchestra\n");
            True(v.AudioOrchestra && !v.CdAudio && !v.AudioRemaster && v.Serialize().Contains("audio=orchestra"), "ini audio=orchestra");
            Eq(416, Window.CommandId("audio:orchestra"), "--menu audio:orchestra");
        }),
        ("win_cinematic", () =>
        {
            // T51 cinematic cut-scenes: the intro's picture is found as a band (text and black excluded); gameplay and
            // menu screens never are
            var d = TheData.Value;
            (SceneRaster R, HdScene S) Scene(Machine m, int frames)
            {
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                for (int f = 0; f < frames; f++) { m.RunFrame(0); if (f >= frames - 2) r.Build(m.Ppu.Capture); }
                var sc = new HdScene(d.NesRgb);
                for (int k = 0; k < 2; k++) sc.Build(r.Fb, r.Tags, r.LinePal[239], new byte[61440], r.LineScroll, k);
                return (r, sc);
            }
            var mi = new Machine(d, _ => { }); mi.Reset();
            var (ri, si) = Scene(mi, 900);
            var band = Gpu.CineBand(ri, si);
            Console.WriteLine($"  win_cinematic: intro frame 900 picture band {band}");
            True(band is { } b && b.Y0 >= 8 && b.Y1 <= 135 && b.Y1 - b.Y0 >= 60 && b.X0 <= 20 && b.X1 >= 236, "intro picture found, the text under it left out");
            if (band is { } bb)
            {
                bool noText = true;
                for (int y = bb.Y0; y < bb.Y1; y++)
                    for (int x = 0; x < 256; x++) noText &= (si.Info[(y * 256 + x) * 4 + 2] & HdScene.FUi) == 0 || (ri.Fb[y * 256 + x] & 63) == 0x0F;
                True(noText, "no lit font (text) pixel inside the picture");
            }
            string dir = Path.Combine(Data.FindDir(), "..", "test", "states");
            foreach (string name in new[] { "town02_enter", "group29", "equip", "shopmenu" })
            {
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, name + ".json")));
                var (r, s) = Scene(m, 10);
                Console.WriteLine($"  win_cinematic: {name} band {Gpu.CineBand(r, s)}");
                True(Gpu.CineBand(r, s) == null, $"{name}: not a cut-scene");
            }
            foreach (string name in new[] { "ending", "finalscene", "death", "mesia", "swordraise" })
            {
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, name + ".json")));
                var (r, s) = Scene(m, 10);
                Console.WriteLine($"  win_cinematic: {name}: {(Gpu.CineBand(r, s) is { } cb ? $"picture lines {cb.Y0}-{cb.Y1 - 1}" : "no picture band")}");
            }
            var v = VideoSettings.Parse("hd_effects=edges\n");
            True(new VideoSettings().HdCinematic && !v.HdCinematic, "ini hd_effects cinematic");
            Eq(292, Window.CommandId("hd:cinematic"), "--menu hd:cinematic");
            True(new VideoSettings().HdSmooth && new VideoSettings().Serialize().Contains("cinematic,outlines") && !v.HdSmooth, "ini hd_effects outlines (T52)");
            Eq(293, Window.CommandId("hd:outlines"), "--menu hd:outlines");
            True(new VideoSettings().HdAmbient && new VideoSettings().Serialize().Contains("outlines,ambient") && !v.HdAmbient, "ini hd_effects ambient (T53)");
            Eq(294, Window.CommandId("hd:ambient"), "--menu hd:ambient");
            True(new VideoSettings().HdTilt && new VideoSettings().Serialize().Contains("ambient,tiltshift") && !v.HdTilt, "ini hd_effects tiltshift (T55)");
            Eq(295, Window.CommandId("hd:tiltshift"), "--menu hd:tiltshift");
        }),
        ("win_ambience", () =>
        {
            // T54 ambient sounds: silent until a scene asks; per bed a sensible level, birds / drips / crackles present,
            // fade out, deterministic
            var off = new Ambience();
            True(Enumerable.Range(0, 44100).All(_ => off.Next() == (0, 0)) && off.Silent, "nothing until a scene asks");
            (double Db, double[] L, double[] R) Bed(Action<Ambience> set, double sec)
            {
                var a = new Ambience(); set(a);
                int n = (int)(sec * 44100);
                var l = new double[n]; var r = new double[n];
                for (int i = 0; i < n; i++) (l[i], r[i]) = a.Next();
                double rms = Math.Sqrt(l.Skip(n / 4).Concat(r.Skip(n / 4)).Average(v => v * v));
                return (20 * Math.Log10(Math.Max(rms, 1e-9) / 32768), l, r);
            }
            double Kurt(double[] x)
            {
                double m = x.Average(), v = x.Average(e => (e - m) * (e - m)), k4 = x.Average(e => Math.Pow(e - m, 4));
                return k4 / (v * v);
            }
            double HighBand(double[] x, int from, int len)
            {
                // energy of the second difference (~ above 4 kHz) in a window
                double s = 0;
                for (int i = from + 2; i < from + len; i++) { double d = x[i] - 2 * x[i - 1] + x[i - 2]; s += d * d; }
                return s / len;
            }
            var o = Bed(a => a.Outdoor = 1, 12);
            var c = Bed(a => a.Cave = 1, 12);
            var h = Bed(a => a.Hot = 1, 12);
            var ic = Bed(a => a.Icy = 1, 12);
            Console.WriteLine($"  win_ambience: outdoor {o.Db:F1} dBFS, cave {c.Db:F1}, fire {h.Db:F1}, ice {ic.Db:F1}");
            True(new[] { o.Db, c.Db, h.Db, ic.Db }.All(db => db > -50 && db < -24), "every bed under the music (-50 .. -24 dBFS)");
            True(o.L.Zip(o.R).Count(p => Math.Abs(p.First - p.Second) > 1) > o.L.Length / 2, "wind in stereo");
            var hb = Enumerable.Range(0, o.L.Length / 4410).Select(k => HighBand(o.L, k * 4410, 4410) + HighBand(o.R, k * 4410, 4410)).ToArray();
            double med = hb.OrderBy(x => x).ElementAt(hb.Length / 2);
            True(hb.Count(x => x > med * 8) >= 2, "birds chirp now and then (high-band bursts)");
            int drips = 0; bool inDrip = false;
            for (int i = 0; i < c.L.Length; i += 64)
            {
                bool loud = Math.Abs(c.L[i] - c.R[i]) > 300;          // the drip echo is uneven between the sides
                if (loud && !inDrip) drips++;
                inDrip = loud;
            }
            True(drips >= 3, $"cave drips ({drips} in 12 s)");
            double maxD2 = Enumerable.Range(2, h.L.Length - 2).Max(i => Math.Abs(h.L[i] - 2 * h.L[i - 1] + h.L[i - 2]));
            True(Kurt(h.L) > 3.1 && maxD2 < 3000, $"fire pops: present (kurtosis {Kurt(h.L):F1}) but soft (largest step {maxD2:F0}, no clicks)");
            var f = new Ambience { Outdoor = 1, Cave = 1 };
            for (int i = 0; i < 44100 * 3; i++) f.Next();
            f.Outdoor = f.Cave = 0;
            double tail = 0;
            for (int i = 0; i < 44100 * 4; i++) { var (l, r) = f.Next(); if (i > 44100 * 3) tail = Math.Max(tail, Math.Abs(l) + Math.Abs(r)); }
            True(tail < 30, $"fades out in a few seconds ({tail:F1})");
            True(Bed(a => a.Outdoor = 1, 2).L.SequenceEqual(Bed(a => a.Outdoor = 1, 2).L), "deterministic");
            // T58 heartbeat: two thumps per 0.85 s (lub, dub), low and soft; silent again after it stops
            var hbA = new Ambience { Heartbeat = true };
            var hbs = Enumerable.Range(0, 44100 * 4).Select(_ => hbA.Next().L).ToArray();
            var env = Enumerable.Range(0, 40).Select(q => hbs.Skip(q * 4410).Take(4410).Max(x => Math.Abs(x))).ToArray();
            int thumps = Enumerable.Range(1, 38).Count(q => env[q] > 3000 && env[q] > env[q - 1] && env[q] >= env[q + 1]);   // peaks of the 0.1 s envelope
            double hbDb = 20 * Math.Log10(Math.Sqrt(hbs.Skip(44100).Average(x => x * x)) / 32768);
            Console.WriteLine($"  win_ambience: heartbeat {thumps} thumps in 4 s, {hbDb:F1} dBFS");
            True(thumps >= 8 && thumps <= 10, "lub-dub every 0.85 s");
            True(hbDb > -45 && hbDb < -24, "heartbeat level under the music");
            hbA.Heartbeat = false;
            for (int i = 0; i < 44100 * 2; i++) hbA.Next();
            True(hbA.Silent, "silent once the heartbeat stops");
            var v = VideoSettings.Parse("ambience=0\n");
            True(!v.Ambience && new VideoSettings().Ambience && v.Serialize().Contains("ambience=0"), "ini ambience");
            Eq(417, Window.CommandId("ambience"), "--menu ambience");
        }),
        ("win_feedback", () =>
        {
            // T57 damage feedback: drops in life during play are hits, refills / menus are not; low life at <= 1/4
            var f = new DamageFeedback();
            False(f.Update(40, 80, true, 0), "first frame: no hit");
            True(f.Update(32, 80, true, 1), "life drop = hit");
            True(Math.Abs(f.LastHit - 0.1) < 1e-9 && f.Hurt == 1, "hit size, flash at full");
            f.Update(32, 80, true, 1 + 13);
            True(Math.Abs(f.Hurt - 0.52) < 0.01, "flash about halved after 13 frames");
            f.Update(32, 80, true, 60);
            True(f.Hurt == 0, "flash over");
            False(f.Update(80, 80, true, 61), "refill (cheat / herb) is no hit");
            False(f.Update(10, 80, false, 62), "outside play no hit");
            False(f.Update(5, 80, true, 63), "first play frame after a menu: no hit");
            True(f.Low, "life <= 1/4: low");
            f.Update(0, 80, true, 64);
            False(f.Low, "dead is not 'low'");
            // a real fight: area $09's chasers, group09_check's keys (frame0 2506): 7 contact hits, the last one to 0
            var d = TheData.Value;
            var m = new Machine(d, _ => { }); m.Reset();
            m.LoadState(StateFile.Load(Path.Combine(Data.FindDir(), "..", "test", "states", "group09.json")));
            var g = new DamageFeedback();
            int hits = 0, low = 0, gf0 = 2506;
            var gk = Keys.Parse(new[] { "2531:UP:50", "2585:RIGHT:330", "2931:B:4", "2951:B:4", "2971:B:4", "2990:RIGHT:21", "3022:B:3", "3040:B:5", "3061:RIGHT:29", "3101:B:4", "3121:B:4", "3141:B:4", "3161:B:4" });
            for (int i = 0; gf0 + m.Frame <= 3268; i++)
            {
                m.RunFrame(gk.TryGetValue(gf0 + m.Frame, out int kb) ? kb : 0);
                var ram = m.Mem.RamBytes;
                if (g.Update(ram[0x03C1], ram[0x03C0], ram[0x40] == 1 && ram[0x41] == 8, m.Frame)) { hits++; if (hits == 1) Console.WriteLine($"  win_feedback: first hit after {m.Frame} frames (f{gf0 + m.Frame - 1})"); }
                if (g.Low) low++;
            }
            Console.WriteLine($"  win_feedback: group09 scripted fight to f3268: {hits} hits, {low} low-life frames");
            Eq(7, hits, "the 7 contact hits group09_check lists (HitPlayerDamage 26:9410), from the life byte");
            True(low > 0, "low life before the last hits");
            var v = VideoSettings.Parse("feedback=0\n");
            True(!v.Feedback && new VideoSettings().Feedback && v.Serialize().Contains("feedback=0"), "ini feedback");
            Eq(105, Window.CommandId("feedback"), "--menu feedback");
        }),
        ("win_noflicker", () =>
        {
            // playtest 6 "NES flicker": group09 (4 chasers, 7 contact hits = 7 x 30 frames of the player's half-rate draw,
            // $0380 b2). Per frame: on-screen active objects with no sprite pixel in the picture, game vs filled
            var d = TheData.Value;
            var m = new Machine(d, _ => { }); m.Reset();
            m.LoadState(StateFile.Load(Path.Combine(Data.FindDir(), "..", "test", "states", "group09.json")));
            m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
            var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
            var ss = new SideSprites();
            var keys = Keys.Parse("2531:UP:50 2585:RIGHT:330 2931:B:4 2951:B:4 2971:B:4 2990:RIGHT:21 3022:B:3 3040:B:5 3061:RIGHT:29 3101:B:4 3121:B:4 3141:B:4 3161:B:4".Split(' '));
            int frames = 0, playerAdded = 0, otherAdded = 0, inv = 0, dup = 0;
            var slots = new SortedDictionary<int, int>();
            for (int f = 0; f < 760; f++)
            {
                ss.Snapshot(m);
                m.RunFrame(keys.TryGetValue(2506 + m.Frame, out int b) ? b : 0);
                r.Build(m.Ppu.Capture);
                if (f < 20) continue;
                Window.FillFlicker(m.Ppu.Capture, r, ss, m, 232);
                if (ss.Ram[0x40] != 1 || ss.Ram[0x51] != 0) continue;
                frames++;
                bool blink = (ss.Ram[0x381] & 4) != 0;               // the player's half-rate draw flag
                if (blink) inv++;
                foreach (int s in ss.Added) slots[s] = slots.GetValueOrDefault(s) + 1;
                if (ss.Added.Contains(1)) { playerAdded++; if (!blink) dup++; }
                if (ss.Added.Any(s => s != 1)) otherAdded++;
            }
            Console.WriteLine($"  win_noflicker: group09 {frames} play frames, player blinking in {inv}: player added in {playerAdded}, other objects in {otherAdded}; slots {string.Join(" ", slots.Select(kv => kv.Key.ToString("X2") + "x" + kv.Value))}");
            True(inv >= 100, $"the fight has the player's half-rate blink ({inv} frames)");
            True(Math.Abs(playerAdded - inv / 2) <= inv / 6, $"the player is filled in on the blink frames the game skips ({playerAdded} of {inv})");
            Eq(0, dup, "the player is only added while blinking (never drawn twice)");
        }),
        ("win_dialogue", () =>
        {
            // playtest 6: NPC text read aloud. group18 walks into Akahana (TalkStart f2562), 2 pages closed by A x2 (f2769)
            var d = TheData.Value;
            var m = new Machine(d, _ => { }); m.Reset();
            m.LoadState(StateFile.Load(Path.Combine(Data.FindDir(), "..", "test", "states", "group18.json")));
            var keys = Keys.Parse("2527:LEFT:47 2701:A:4 2761:A:9 2801:UP:34 2841:LEFT:80".Split(' '));
            var dr = new DialogueReader();
            var said = new List<(int F, string T)>(); var stops = new List<int>();
            var speakers = new List<int>();
            for (int f = 0; f < 480; f++)
            {
                int fn = 2506 + m.Frame;
                m.RunFrame(keys.TryGetValue(fn, out int b) ? b : 0);
                var (say, stop) = dr.Update(m.Rd(0x40), m.Rd(0x51), a => m.Rd(a));
                if (say != null) said.Add((fn, say));
                if (say != null) speakers.Add(Cast.Speaker(a => m.Rd(a)));
                if (stop) stops.Add(fn);
            }
            foreach (var (f, t) in said) Console.WriteLine($"  win_dialogue: f{f} say \"{t}\"");
            Console.WriteLine($"  win_dialogue: stop at {string.Join(",", stops)}");
            True(said.Count == 3 && said[2].T.StartsWith("This town"), $"Akahana's 2 pages + the next NPC (UP), each said once ({said.Count})");
            True(said.Count > 0 && said[0].T.Contains("Akahana") && said[0].F < 2701, "page 1 (Akahana) before the first A");
            True(said.Count > 1 && said[1].F > 2701 && said[1].F < 2761 && said[1].T != said[0].T, "page 2 after the first A");
            True(stops.Count >= 1 && stops[0] > 2761 && stops[0] < 2800, "the voice stops when the window closes");
            // a buffer that is not a text page (map rows) is never read
            var junk = new byte[256]; new Random(5).NextBytes(junk);
            Eq("", DialogueReader.Page(a => junk[a - DialogueReader.Buf]), "random buffer: nothing to say");
            var page = new int[256];
            for (int r = 0; r < 8; r++) { page[r * 32] = 0xFE; page[r * 32 + 1] = 0x1E; for (int x = 2; x < 31; x++) page[r * 32 + x] = 0x20; page[r * 32 + 31] = 0x1F; }
            void Put(int r, string s) { for (int i = 0; i < s.Length; i++) page[r * 32 + 4 + i] = s[i]; }
            Put(1, "The wind is cold, but some-"); Put(3, "times we like it_"); page[3 * 32 + 26] = 0x7C;
            Eq("The wind is cold, but sometimes we like it...", DialogueReader.Page(a => page[a - DialogueReader.Buf]), "joins lines, word breaks, ellipsis, skips icons");
            var v = VideoSettings.Parse("voice=0\n");
            True(!v.Voice && new VideoSettings().Voice && v.Serialize().Contains("voice=0"), "ini voice");
            Eq(418, Window.CommandId("voice"), "--menu voice");
            // who speaks: Akahana = NPC $16 (bald elder), the next NPC = $08
            Eq("22,22,8", string.Join(",", speakers), "speaker NPC ids from the talk slot");
            Eq(VoiceClass.OldMan, Cast.Of(0x16), "Akahana sounds old");
            Eq(-1, Cast.Speaker(a => a == 0x41 ? 8 : 0x0D), "no conversation (sub_state 8): the narrator");
            True(Cast.Count == 125, $"every talking NPC classified ({Cast.Count})");
            // Windows voice XML: gender + age, text escaped
            string xml = Cast.Xml(0x16, "Tom & <Jerry>");
            True(xml.Contains("Gender=Male") && xml.Contains("absmiddle=\"-") && xml.Contains("Tom &amp; &lt;Jerry&gt;"), $"SAPI xml: {xml}");
            True(Cast.Xml(0x12, "hi").Contains("Gender=Female") && !Cast.Xml(-1, "hi").Contains("<voice"), "girl female, narrator default");
            // neural voice: speakers by sex, each NPC its own reader + pitch, ages by pitch
            string sp = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "voice", "speakers.txt");
            var (male, female) = NeuralVoice.Speakers(sp);
            True(male.Length == 474 && female.Length == 430, $"speakers.txt: {male.Length} male, {female.Length} female");
            var picks = new HashSet<(int, int)>(); int wrong = 0;
            foreach (int npc in Enumerable.Range(0, 256).Where(n => Cast.Of(n) != VoiceClass.Narrator))
            {
                var (s, pch) = NeuralVoice.Pick(npc, male, female);
                var c = Cast.Of(npc);
                bool isMale = male.Contains(s);
                if (c is VoiceClass.Man or VoiceClass.OldMan && !isMale) wrong++;
                if (c is VoiceClass.Woman or VoiceClass.OldWoman or VoiceClass.Girl or VoiceClass.Boy && isMale) wrong++;
                if (c is VoiceClass.OldMan or VoiceClass.OldWoman && pch > 0.92) wrong++;
                if (c is VoiceClass.Girl or VoiceClass.Boy && pch < 1.15) wrong++;
                if (c is VoiceClass.Man or VoiceClass.Woman && Math.Abs(pch - 1) > 0.04) wrong++;
                picks.Add((s, (int)Math.Round(pch * 100)));
            }
            Eq(0, wrong, "readers match sex, pitches match age");
            True(picks.Count >= 120, $"NPCs sound different from each other ({picks.Count} distinct voices for 125)");
            // the WAV replay: a 22050 Hz tone at speed 1.2 -> 44100 Hz, shorter, faded ends
            var wav = WavOut.WavBytes(Enumerable.Range(0, 22050).Select(i => (short)(10000 * Math.Sin(i * 0.1))).ToList(), 1, 22050);
            var pcm = NeuralVoice.Render(wav, 1.2, 44100);
            True(Math.Abs(pcm.Length - 44100 / 1.2) < 3 && pcm[0] == 0 && pcm.Max() > 0.29, $"replay: {pcm.Length} samples");
            Eq("\"a \\\"b\\\" c\\\\d\\u000a\"", NeuralVoice.Json("a \"b\" c\\d\n"), "json escaping");
            True(NeuralVoice.Folder(Path.GetTempPath()) == null, "no voice folder: Windows voice fallback");
        }),
        ("win_cine_gate", () =>
        {
            // playtest 6: the title screen "zoomed in" (the cut-scene dolly on the logo); the gate allows the film look only
            // in the opening and story pages (title_mode 0-2) and outside main_state 3
            var d = TheData.Value;
            var m = new Machine(d, _ => { }); m.Reset();
            var keys = Keys.Parse("1250:START 1400:START 1600:START 1700:START".Split(' '));
            var runs = new List<string>(); int lastKey = -1, from = 0, storyFrames = 0, titleFrames = 0;
            for (int f = 0; f < 1900; f++)
            {
                m.RunFrame(keys.TryGetValue(214 + m.Frame, out int b) ? b : 0);
                int ms = m.Rd(0x40), tm = m.Rd(0x6F0), key = ms * 16 + tm + (tm == 0 ? m.Rd(0x6F1) << 8 : 0);
                if (Window.CineAllowed(ms, tm) && ms == 3) storyFrames++;
                if (ms == 3 && tm is 5 or 6 && !Window.CineAllowed(ms, tm)) titleFrames++;
                if (key != lastKey) { if (lastKey >= 0) runs.Add($"{lastKey >> 4 & 15}/{lastKey & 15}.{lastKey >> 8}@{from}"); lastKey = key; from = 214 + m.Frame; }
            }
            runs.Add($"{lastKey >> 4 & 15}/{lastKey & 15}.{lastKey >> 8}@{from}");
            Console.WriteLine($"  win_cine_gate: main/title_mode from power-on: {string.Join(" ", runs)}");
            True(storyFrames > 1000 && titleFrames > 100, $"opening + story pages ({storyFrames} frames) keep the film look, title ({titleFrames}) does not");
            True(!Window.CineAllowed(3, 6) && !Window.CineAllowed(3, 5) && !Window.CineAllowed(3, 4) && Window.CineAllowed(3, 0) && Window.CineAllowed(3, 2) && Window.CineAllowed(1, 0), "gate");
        }),
        ("win_font", () =>
        {
            // T59 modern font: ASCII tiles $F21-$F7E (not $F40), ink / paper per tile from CHR, glyph words on text pixels
            // whose ink / paper colours are the ones the game drew
            var d = TheData.Value;
            var m = new Machine(d, _ => { }); m.Reset();
            var chr = m.Ppu.Chr;
            Eq(0x41, FontInk.Code(0xF41), "tile $F41 = 'A'");
            Eq(-1, FontInk.Code(0xF40), "$40 is the page prompt, not '@'");
            Eq(-1, FontInk.Code(0xF20), "space has no glyph");
            Eq(-1, FontInk.Code(0xF7C), "custom symbol tiles keep their pixels ('|' is an arrow piece)");
            Eq(0x5F, FontInk.Code(0xF5F), "'_' = the game's ellipsis");
            Eq("\u2026", FontInk.Text(0x5F), "drawn as '…'");
            int glyphs = Enumerable.Range(0x21, 0x5E).Count(c => FontInk.Values(chr, FontInk.First + c).Ink >= 0);
            var (top, bottom) = FontInk.InkRows(chr, FontInk.First + 'H');
            Console.WriteLine($"  win_font: {glyphs} glyph tiles with ink, 'H' rows {top}-{bottom}");
            True(glyphs == 26 * 2 + 10 + FontInk.Standard.Length, $"letters, digits, standard punctuation have ink ({glyphs})");
            var inverted = Enumerable.Range(0x21, 0x5E).Where(c => FontInk.Code(FontInk.First + c) >= 0 && FontInk.Values(chr, FontInk.First + c).Paper != FontInk.Values(chr, FontInk.First + 'A').Paper).Select(c => (char)c);
            True(!inverted.Any(), $"playtest 6: every glyph has the same paper as 'A' (heavy B D Q 0 were inverted: {string.Concat(inverted)})");
            True(top >= 0 && bottom <= 7 && bottom - top >= 4, "'H' cap height");
            string dir = Path.Combine(Data.FindDir(), "..", "test", "states");
            var mt = new Machine(d, _ => { }); mt.Reset(); mt.LoadState(StateFile.Load(Path.Combine(dir, "talk16.json")));
            mt.Ppu.Capture = new VideoCapture(); mt.Ppu.NoPixels = true;
            var r = new SceneRaster(mt.Ppu.Chr) { SpriteLimit = false };
            int words = 0, right = 0, frames = 0;
            for (int f = 0; f < 30; f++) { mt.RunFrame(0); frames++; }
            {
                int f = 30;
                mt.RunFrame(0); r.Build(mt.Ppu.Capture); frames++;
                var sc = new HdScene(d.NesRgb);
                sc.Build(r.Fb, r.Tags, r.LinePal[239], new byte[61440], r.LineScroll, f);
                var lay = new byte[256 * 240 * 4]; var am = new uint[256 * 240 * 2];
                Gpu.Layers(r, sc, new Dictionary<long, int>(), new Dictionary<long, int>(), new bool[8192], lay, am, false, mt.Ppu.Chr);
                for (int i = 0; i < 232 * 256; i++)
                {
                    uint w = am[i * 2];
                    if ((w >> 31) == 0) continue;
                    words++;
                    int tile = FontInk.First + (int)(w & 127), col = (int)(w >> 7 & 7), row = (int)(w >> 10 & 7);
                    var (ink, _) = FontInk.Values(mt.Ppu.Chr, tile);
                    int want = FontInk.Px(mt.Ppu.Chr, tile, col, row) == ink ? (int)(w >> 13 & 63) : (int)(w >> 19 & 63);
                    if ((r.Fb[i] & 63) == want) right++;
                }
            }
            Console.WriteLine($"  win_font: talk16 after {frames} frames: {words} glyph pixels, {right} with the drawn colour");
            True(words > 2000, "the message box gives glyph words");
            True(right == words, "ink / paper colours = the colours on screen");
            var v = VideoSettings.Parse("hd_effects=edges\n");
            True(new VideoSettings().HdFont && !v.HdFont && new VideoSettings().Serialize().Contains("tiltshift,font"), "ini hd_effects font");
            Eq(296, Window.CommandId("hd:font"), "--menu hd:font");
        }),
        ("win_color", () =>
        {
            // T60 display colours: defaults change nothing; brightness / vivid do what they say; each colour-blind
            // assist leaves fewer NES colour pairs that look alike to that deficiency but differ to normal vision
            var d = TheData.Value;
            var pal = d.NesRgb.Select(c => (uint)c & 0xFFFFFF).ToArray();
            var id0 = ColorAdjust.Matrix(2, false, 0);
            True(ColorAdjust.IsIdentity(id0) && pal.All(c => ColorAdjust.Apply(id0, c) == c), "defaults: identity, palette unchanged");
            double Lum(uint c) => (c >> 16 & 255) * 0.299 + (c >> 8 & 255) * 0.587 + (c & 255) * 0.114;
            double LumAll(float[] m) => pal.Sum(c => Lum(ColorAdjust.Apply(m, c)));
            True(LumAll(ColorAdjust.Matrix(0, false, 0)) < LumAll(id0) && LumAll(ColorAdjust.Matrix(4, false, 0)) > LumAll(id0), "brightness 1 darker, 5 brighter");
            double Sat(uint c) { int r = (int)(c >> 16 & 255), g = (int)(c >> 8 & 255), b = (int)(c & 255); return Math.Max(r, Math.Max(g, b)) - Math.Min(r, Math.Min(g, b)); }
            var viv = ColorAdjust.Matrix(2, true, 0);
            True(pal.Sum(c => Sat(ColorAdjust.Apply(viv, c))) > pal.Sum(Sat) * 1.1, "vivid: more saturated");
            True(ColorAdjust.Apply(viv, 0x808080) == 0x808080, "vivid: greys unchanged");
            (double R, double G, double B) V(uint c) => ((c >> 16 & 255) / 255.0, (c >> 8 & 255) / 255.0, (c & 255) / 255.0);
            double Dist(uint a, uint b) { var (r1, g1, b1) = V(a); var (r2, g2, b2) = V(b); return Math.Sqrt((r1 - r2) * (r1 - r2) + (g1 - g2) * (g1 - g2) + (b1 - b2) * (b1 - b2)); }
            var distinct = pal.Distinct().ToArray();
            for (int k = 1; k <= 3; k++)
            {
                var sim = ColorAdjust.Simulate(k);
                var fix = ColorAdjust.Matrix(2, false, k);
                int Confusable(float[] m)
                {
                    int n = 0;
                    for (int i = 0; i < distinct.Length; i++)
                        for (int j = i + 1; j < distinct.Length; j++)
                            if (Dist(distinct[i], distinct[j]) > 0.25 && Dist(ColorAdjust.Apply(sim, ColorAdjust.Apply(m, distinct[i])), ColorAdjust.Apply(sim, ColorAdjust.Apply(m, distinct[j]))) < 0.1) n++;
                    return n;
                }
                int before = Confusable(id0), after = Confusable(fix);
                Console.WriteLine($"  win_color: {ColorAdjust.BlindNames[k]}: {before} confusable NES colour pairs -> {after} with the assist");
                True(after < before * 0.7, $"{ColorAdjust.BlindNames[k]} assist separates confusable colours");
            }
            var v = VideoSettings.Parse("brightness=4\nvivid=1\ncolorblind=deutan\n");
            True(v.Brightness == 4 && v.Vivid && v.ColorBlind == 2 && v.Serialize().Contains("colorblind=deutan"), "ini brightness / vivid / colorblind");
            True(new VideoSettings().Serialize().Contains("brightness=2") && new VideoSettings().Serialize().Contains("colorblind=off"), "ini defaults");
            Eq(644, Window.CommandId("brightness:5"), "--menu brightness:5");
            Eq(649, Window.CommandId("colorblind:deutan"), "--menu colorblind:deutan");
            Eq(646, Window.CommandId("vivid"), "--menu vivid");
        }),
        ("win_controls", () =>
        {
            // T61 remapped keys: an extra key per button on top of the defaults; it wins over a default meaning
            var k = new Input();
            k.SetBinds(new[] { 0x20, 'L', 0, 0, 'I', 0, 0, 0 });            // A = Space, B = L, Up = I
            k.KeyDown(0x20); Eq(Btn.A, k.Read(), "Space = A");
            k.KeyUp(0x20); k.KeyDown('X'); Eq(Btn.A, k.Read(), "default X still A");
            k.KeyUp('X'); k.KeyDown('L'); Eq(Btn.B, k.Read(), "L = B");
            k.KeyUp('L'); k.KeyDown('I'); Eq(Btn.Up, k.Read(), "I = Up");
            k.KeyUp('I');
            True(k.IsBound(0x20) && !k.IsBound('X'), "bound keys known (no hotkey for them)");
            k.SetBinds(new[] { 'D', 0, 0, 0, 0, 0, 0, 0 });                // D (default Right) rebound to A
            k.KeyDown('D'); Eq(Btn.A, k.Read(), "a rebound default key takes its new button");
            k.KeyUp('D');
            Eq("SPACE", Input.KeyName(0x20), "key name"); Eq("F5", Input.KeyName(0x74), "F-key name"); Eq("Q", Input.KeyName('Q'), "letter");
            var v = VideoSettings.Parse("keybinds=a:20,up:49\n");
            True(v.KeyBinds[0] == 0x20 && v.KeyBinds[4] == 0x49 && v.KeyBinds[1] == 0 && v.Serialize().Contains("keybinds=a:20,up:49"), "ini keybinds");
            True(new VideoSettings().Serialize().Contains("keybinds=none"), "ini default: none");
            True(Osd.Build(false, false, null, new List<string>(), prompt: "PRESS A KEY FOR A\nNOW: SPACE") && (Osd.Pix[200 * Osd.W + 256] >> 24) > 200, "remap prompt drawn");
            Eq(106, Window.CommandId("remapkeys"), "--menu remapkeys");
            Eq(107, Window.CommandId("resetkeys"), "--menu resetkeys");
        }),
        ("win_auto_quality", () =>
        {
            // T62 HD quality Auto: down after 3 s under 50 fps, up after 20 s at 58+, a failed step up lowers the ceiling
            var a = new AutoQuality();
            double t = 0;
            void Run(double fps, double sec) { for (double e = 0; e < sec * 1000; e += 16.7) { a.Update(fps, t); t += 16.7; } }
            Run(60, 30); Eq(2, a.Level, "smooth: stays full");
            Run(40, 2); Eq(2, a.Level, "a short dip: no change");
            Run(40, 1.5); Eq(1, a.Level, "3 s slow: balanced");
            Run(40, 3.1); Eq(0, a.Level, "still slow: fast");
            Run(60, 21); Eq(1, a.Level, "20 s smooth: one step up");
            Run(40, 3.1); Eq(0, a.Level, "slow again within 10 s: back down");
            Eq(0, a.Ceiling, "ceiling lowered");
            Run(60, 60); Eq(0, a.Level, "no more bouncing");
            True(VideoSettings.Parse("hd_quality=auto\n").HdQuality == 3 && new VideoSettings().Serialize().Contains("hd_quality=auto"), "ini hd_quality=auto (the default)");
            Eq(297, Window.CommandId("hd:auto"), "--menu hd:auto");
            // T63 save backups: rotated when the save changed, kept when not
            string dir = Path.Combine(Path.GetTempPath(), "rec_backup_" + Environment.ProcessId);
            Directory.CreateDirectory(dir);
            string p = Path.Combine(dir, "re-c.sav");
            try
            {
                Eq(0, Save.Backup(p), "no save: no backup");
                File.WriteAllBytes(p, new byte[] { 1 }); Eq(1, Save.Backup(p), "first backup");
                Eq(1, Save.Backup(p), "unchanged: no new backup");
                File.WriteAllBytes(p, new byte[] { 2 }); Save.Backup(p);
                File.WriteAllBytes(p, new byte[] { 3 }); Save.Backup(p);
                File.WriteAllBytes(p, new byte[] { 4 }); Eq(3, Save.Backup(p), "three kept");
                True(File.ReadAllBytes(p + ".1")[0] == 4 && File.ReadAllBytes(p + ".2")[0] == 3 && File.ReadAllBytes(p + ".3")[0] == 2, "newest first");
            }
            finally { Directory.Delete(dir, true); }
        }),
        ("win_wide", () =>
        {
            // widescreen playfield: the area cache = Bg.RenderArea (visited bitmap); the camera before a frame puts the
            // cache on the frame (match test on); the sides continue the playfield
            var d = TheData.Value;
            string dir = Path.Combine(Data.FindDir(), "..", "test", "states");
            foreach (var (name, keys) in new[] { ("group09", new[] { "2531:UP:50", "2585:RIGHT:330" }), ("group08", new[] { "2526:UP:84" }), ("town02_enter", Array.Empty<string>()) })
            {
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, name + ".json")));
                var cache = AreaCache.Build(d, AreaCache.Take(m))!;
                var (w, h, fb) = ReC.Game.World.Bg.RenderArea(m, ReC.Game.World.Bg.Visit.Ram);
                var pal = Enumerable.Range(0, 32).Select(i => (byte)m.Rd(0x6140 + i)).ToArray();
                bool same = cache.W == w && cache.H == h && Enumerable.Range(0, w * h).All(i => AreaCache.Color(cache.Px[i], pal) == fb[i]);
                True(same, $"{name}: area cache = Bg.RenderArea ({w}x{h})");
                var wide = new Widescreen();
                var kp = Keys.Parse(keys);
                int matched = 0, frames = 0;
                for (int f = 0; f < 200; f++)
                {
                    wide.BeforeFrame(m, d, async: false);
                    m.RunFrame(kp.TryGetValue(2506 + m.Frame, out int b) ? b : 0);
                    wide.AfterFrame(m.Ppu.Fb, m.Ppu.Pal, 180);
                    frames++; if (wide.Match) matched++;
                }
                Console.WriteLine($"  win_wide: {name}: area {w}x{h}, playfield matched the area at the camera in {matched} / {frames} frames, sides shown {wide.Show:F2}");
                True(matched > frames * 0.7 && wide.Show >= 1, $"{name}: the camera puts the area on the screen (none built during the area load)");
            }
            {
                // playtest 6: the map continues behind the status bar; the HUD panel stays on top, unchanged
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, "group09.json")));
                var kp = Keys.Parse(new[] { "2531:UP:50" });
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                var wide = new Widescreen();
                for (int f = 0; f < 90; f++)
                {
                    wide.BeforeFrame(m, d, async: false);
                    m.RunFrame(kp.TryGetValue(2506 + m.Frame, out int b) ? b : 0);
                    r.Build(m.Ppu.Capture);
                    wide.AfterFrame(r.Fb, m.Ppu.Pal, Gpu.SplitGap(r).To is > 0 and var t ? t : 232, r.LinePal, r.LineScroll, r.Sp);
                }
                var gap = Gpu.SplitGap(r);
                int hudFrom = gap.From > 0 ? gap.From : 232;
                var lines = new bool[240];
                wide.MapLines(r.Fb, hudFrom, 0.5f, lines);
                var wr = new SceneRaster(m.Ppu.Chr, 424);
                var scn = new HdScene(d.NesRgb);
                scn.Build(r.Fb, r.Tags, r.LinePal[239], new byte[61440], r.LineScroll, 0);
                int panelTo = Gpu.EdgeBand(r, scn);
                wr.Widen(r, wide, lines, hudFrom, gap.To > 0 ? gap.To : hudFrom, panelTo);
                int e = 84, under = 0, sideMap = 0, panelKept = 0, panelPx = 0, aboveUnder = 0;
                for (int y = 0; y < 240; y++)
                    for (int x = 0; x < 424; x++)
                    {
                        int i = y * 424 + x;
                        if (y < hudFrom) { if (wr.Under[i] && x - e >= 8 && x - e < 248) aboveUnder++; continue; }   // only the stale edge columns may change
                        if (wr.Under[i]) under++;
                        bool side = x < e || x >= e + 256;
                        if (side && (wr.BgTag[i] & Ppu.TagValid) != 0) sideMap++;
                        if (!side && !wr.Under[i]) { panelPx++; if (wr.Fb[i] == r.Fb[y * 256 + x - e]) panelKept++; }
                    }
                {
                    int diffR = 0, diffL = 0, tagR = 0;
                    for (int yy = 10; yy < 170; yy++)
                    {
                        int i0 = yy * 424;
                        if (wr.Fb[i0 + e + 255] != wr.Fb[i0 + e + 256] && wr.Fb[i0 + e + 254] == wr.Fb[i0 + e + 255]) diffR++;
                        if (wr.Fb[i0 + e - 1] != wr.Fb[i0 + e] && wr.Fb[i0 + e] == wr.Fb[i0 + e + 1]) diffL++;
                        if (((wr.BgTag[i0 + e + 256] >> 13) & 7) != ((((wr.BgTag[i0 + e + 255] >> 13) & 7) + 1) & 7)) tagR++;
                    }
                    Console.WriteLine($"  win_wide: scroll edge: {wr.EdgeFixed} stale edge-column pixels replaced by the map; colour steps at the edges right {diffR} left {diffL} of 160 rows, tile phase breaks {tagR}");
                    True(wr.EdgeFixed > 0 && tagR == 0, "playtest 6: stale scroll-edge tiles replaced by the map, tile phase continuous across the edge");
                }
                Console.WriteLine($"  win_wide: HUD overlay: rows {hudFrom}-239, panel {gap.To}-{panelTo - 1}, {under} game-column pixels + {sideMap} side pixels show the map, panel {panelKept} / {panelPx} pixels unchanged");
                int sideNo = Enumerable.Range(hudFrom * 424, (240 - hudFrom) * 424).Count(i => (i % 424 < e || i % 424 >= e + 256) && wr.NoMap[i]);
                True(under > 1000 && sideMap + sideNo == (240 - hudFrom) * 168 && sideMap > sideNo, $"the map continues behind the status bar across the width ({sideNo} side pixels past the area's edge: glow)");
                True(panelPx > 0 && panelKept == panelPx && aboveUnder == 0, "the HUD panel stays exactly as the game draws it; the playfield is untouched");
            }
            // HD: the wide raster (SceneRaster.Widen) = the game's raster in the middle, the area at the camera on the sides
            // with valid tags; the layer / info words fill the whole width
            {
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, "group09.json")));
                var kp = Keys.Parse(new[] { "2531:UP:50", "2585:RIGHT:330" });
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                var wide = new Widescreen();
                for (int f = 0; f < 200; f++)
                {
                    wide.BeforeFrame(m, d, async: false);
                    m.RunFrame(kp.TryGetValue(2506 + m.Frame, out int b) ? b : 0);
                    r.Build(m.Ppu.Capture);
                    wide.AfterFrame(r.Fb, m.Ppu.Pal, 190);
                }
                var lines = new bool[240];
                wide.MapLines(r.Fb, 190, 0.5f, lines);
                var wr = new SceneRaster(m.Ppu.Chr, 424);
                wr.Widen(r, wide, lines);
                int e = 84, side = 0, sideOk = 0;
                bool mid = true;
                for (int y = 0; y < 240; y++)
                    for (int x = 0; x < 424; x++)
                    {
                        int i = y * 424 + x;
                        if (x >= e + 8 && x < e + 256) { mid &= wr.Fb[i] == r.Fb[y * 256 + x - e] && wr.SpTag[i] == r.SpTag[y * 256 + x - e]; continue; }
                        if (x >= e) continue;
                        if (!lines[y]) continue;
                        side++;
                        if (wr.Fb[i] == wide.SideColor(x, y, e, r.LinePal[y]) && (wr.BgTag[i] & ReC.Nes.Ppu.TagValid) != 0) sideOk++;
                    }
                Console.WriteLine($"  win_wide: HD wide raster 424: {lines.Count(l => l)} map lines, {sideOk} / {side} left-side pixels = the area with tags");
                True(mid, "middle = the game's raster");
                True(side > 10000 && sideOk == side, "sides = the area at the camera, tagged");
                var sc = new HdScene(d.NesRgb);
                sc.Build(r.Fb, r.Tags, r.LinePal[239], new byte[61440], r.LineScroll, 0);
                var info = new byte[424 * 240 * 4];
                Gpu.WidenInfo(sc.Info, wr, e, info, 190, r, sc);
                var lay = new byte[424 * 240 * 4]; var am = new uint[424 * 240 * 2];
                Gpu.Layers(wr, sc, new Dictionary<long, int>(), new Dictionary<long, int>(), new bool[8192], lay, am, true, null, info, r);
                True(Enumerable.Range(0, 424 * 180).Count(i => (lay[i * 4] & 63) != 0x0F) > 424 * 100, "wide layer words cover the sides");
            }
            True(VideoSettings.Parse("widescreen=16:9\n").Wide == 2 && new VideoSettings().Serialize().Contains("widescreen=off"), "ini widescreen");
            Eq(662, Window.CommandId("widescreen:16:9"), "--menu widescreen:16:9");
            {
                // HD seam: the scene info of the wide picture is continuous across the edges of the game's columns (colour
                // = the picture, the void mask the same on both sides)
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, "group09.json")));
                var kp = Keys.Parse(new[] { "2531:UP:50", "2585:RIGHT:330" });
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                var wide = new Widescreen();
                for (int f = 0; f < 200; f++)
                {
                    wide.BeforeFrame(m, d, async: false);
                    m.RunFrame(kp.TryGetValue(2506 + m.Frame, out int b) ? b : 0);
                    r.Build(m.Ppu.Capture);
                    wide.AfterFrame(r.Fb, m.Ppu.Pal, 190);
                }
                var lines = new bool[240];
                wide.MapLines(r.Fb, 190, 0.5f, lines);
                var wr = new SceneRaster(m.Ppu.Chr, 424);
                wr.Widen(r, wide, lines);
                var sc = new HdScene(d.NesRgb);
                sc.Build(r.Fb, r.Tags, r.LinePal[239], new byte[61440], r.LineScroll, 0);
                var info = new byte[424 * 240 * 4];
                Gpu.WidenInfo(sc.Info, wr, 84, info, 190, r, sc);
                int e = 84;
                (int ColBad, int Void) Col(int x)
                {
                    int colBad = 0, voidN = 0, cacheBad = 0;
                    for (int y = 0; y < 180; y++)
                    {
                        if (!lines[y]) continue;
                        int i = y * 424 + x;
                        if (info[i * 4 + 1] / 4 != (wr.Fb[i] & 63)) colBad++;
                        if ((info[i * 4 + 2] & HdScene.FVoid) != 0) voidN++;
                        if (wide.SideColor(x, y, e, r.LinePal[y]) != (wr.Fb[i] & 63) && wr.Sp[i] == 0) cacheBad++;
                    }
                    return (colBad + cacheBad, voidN);
                }
                var cols = new[] { e - 1, e, e + 7, e + 8, e + 255, e + 256 }.Select(Col).ToArray();
                Console.WriteLine($"  win_wide: seam columns (left side, masked 0, 7, 8, 255, right side): void lines {string.Join(" ", cols.Select(c => c.Void))}");
                True(cols.All(c => c.ColBad == 0), "scene info colour = the picture at the seams");
                True(Math.Abs(cols[0].Void - cols[1].Void) <= 8 && Math.Abs(cols[4].Void - cols[5].Void) <= 8, "void mask continuous across both edges");
            }
            {
                // playtest 3: the HD widescreen host path per frame: time, and (above all) allocations (large per-frame arrays
                // went to the large-object heap = blocking full GCs = audio dropouts)
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, "group09.json")));
                var kp = Keys.Parse(new[] { "2531:UP:50", "2585:RIGHT:330" });
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                var wr = new SceneRaster(m.Ppu.Chr, 424);
                var wide = new Widescreen(); var ss = new SideSprites();
                var sc = new HdScene(d.NesRgb);
                var enh = new Enhancer(m.Ppu.Chr, d.NesRgb, record: false);
                var lines = new bool[240];
                var info = new byte[424 * 240 * 4]; var lay = new byte[424 * 240 * 4]; var am = new uint[424 * 240 * 2];
                var spr = new byte[424 * 240]; var tg = new int[424 * 240];
                var sw = new System.Diagnostics.Stopwatch();
                var ms = new List<double>(); var al = new List<long>();
                for (int f = 0; f < 300; f++)
                {
                    wide.BeforeFrame(m, d, async: false); ss.Snapshot(m);
                    m.RunFrame(kp.TryGetValue(2506 + m.Frame, out int b) ? b : 0);
                    long a0 = GC.GetAllocatedBytesForCurrentThread();
                    sw.Restart();
                    r.Build(m.Ppu.Capture);
                    enh.LiftHd(r.Fb, r.Tags);
                    sc.Build(r.Fb, r.Tags, r.LinePal[239], enh.HdLift, r.LineScroll, f);
                    wide.AfterFrame(r.Fb, m.Ppu.Pal, 190);
                    wide.MapLines(r.Fb, 190, 0.5f, lines);
                    wr.Widen(r, wide, lines);
                    var cap = m.Ppu.Capture; var banks = new int[8];
                    for (int k = 0; k < 8; k++) banks[k] = cap.Spr[100].Banks[k];
                    ss.Draw(d, m.Ppu.Chr, banks, cap.Spr[100].Ctrl, 424, 84, spr, tg, lines);
                    wr.ApplySideSprites(spr, tg);
                    Gpu.WidenInfo(sc.Info, wr, 84, info, 190, r, sc);
                    Gpu.Layers(wr, sc, new Dictionary<long, int>(), new Dictionary<long, int>(), new bool[8192], lay, am, true, null, info, r);
                    Gpu.Meters(r, sc.HudFill, 0); Gpu.CineBand(r, sc);
                    ms.Add(sw.Elapsed.TotalMilliseconds); al.Add(GC.GetAllocatedBytesForCurrentThread() - a0);
                }
                var s2 = ms.Skip(20).OrderBy(x => x).ToList();
                long allocMed = al.Skip(20).OrderBy(x => x).ElementAt(140);
                Console.WriteLine($"  win_wide: HD wide host path: median {s2[s2.Count / 2]:F2} ms, p99 {s2[(int)(s2.Count * 0.99)]:F2} ms; {allocMed / 1024} KB allocated per frame");
                True(allocMed < 16 * 1024, "no large per-frame allocations (the test's own 8 KB tables included)");
            }
            {
                // mid-frame scroll splits: every line marked as map really matches the game there with its chosen offset
                // (the camera, or the line's own scroll), and most playfield lines are map
                foreach (string name in new[] { "group81", "group2b", "group29", "town02_enter", "group08" })
                {
                    var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, name + ".json")));
                    m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                    var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                    var wd = new Widescreen();
                    long okLines = 0, lines = 0, good = 0, tot = 0, mixed = 0;
                    for (int f = 0; f < 60; f++)
                    {
                        wd.BeforeFrame(m, d, async: false);
                        m.RunFrame(0); r.Build(m.Ppu.Capture);
                        int bar = Gpu.SplitGap(r).To; if (bar <= 0) bar = 232;
                        wd.AfterFrame(r.Fb, m.Ppu.Pal, bar, r.LinePal, r.LineScroll, r.Sp);
                        if (f < 10) continue;
                        if (Enumerable.Range(8, bar - 8).Where(y => wd.LineOk[y]).Select(y => (wd.LineDx[y], wd.LineDy[y])).Distinct().Count() > 1) mixed++;
                        for (int y = 8; y < bar; y++)
                        {
                            if (r.Fb.Skip(y * 256 + 8).Take(248).All(c => (c & 63) == 0x0F)) continue;
                            lines++;
                            if (!wd.LineOk[y]) continue;
                            okLines++;
                            for (int x = 8; x < 256; x += 2)
                            {
                                if (r.Sp[y * 256 + x] != 0) continue;
                                tot++;
                                if (wd.SideColor(x + 100, y, 100, r.LinePal[y]) == (r.Fb[y * 256 + x] & 63)) good++;
                            }
                        }
                    }
                    Console.WriteLine($"  win_wide: {name}: {okLines} / {lines} playfield lines verified as map, their pixels match {100.0 * good / Math.Max(1, tot):F1} %");
                    True(good >= tot * 0.9, $"{name}: verified lines match the game");
                    Eq(0L, mixed, $"playtest 6: {name}: no frame shows the sides at two different offsets (tears)");
                }
            }
            {
                // playtest 4: a screen shake (every line 3 px off the camera) is still map; area changes do not allocate
                // new big arrays; side pixels outside the area are flagged "no map" (the HD fill)
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, "group09.json")));
                var kp = Keys.Parse(new[] { "2531:UP:50", "2585:RIGHT:330" });
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                var wd = new Widescreen();
                for (int f = 0; f < 150; f++) { wd.BeforeFrame(m, d, async: false); m.RunFrame(kp.TryGetValue(2506 + m.Frame, out int b) ? b : 0); }
                r.Build(m.Ppu.Capture);
                var shFb = new byte[61440]; var shSc = new int[240];
                for (int y = 0; y < 240; y++)
                {
                    for (int x = 0; x < 256; x++) shFb[y * 256 + x] = r.Fb[y * 256 + Math.Min(255, x + 3)];
                    int v = r.LineScroll[y];
                    if (v < 0) { shSc[y] = v; continue; }
                    int fx = (v >> 16 & 7) + 3, cx = (v & 31) + (fx >> 3), nt = (v >> 10 & 1) ^ (cx >> 5);
                    shSc[y] = (v & ~0x7041F) | (cx & 31) | nt << 10 | (fx & 7) << 16 | (v & 0x7000);
                }
                for (int k = 0; k < 4; k++) wd.AfterFrame(shFb, m.Ppu.Pal, 160, r.LinePal, shSc, r.Sp);
                int okL = Enumerable.Range(20, 120).Count(y => wd.LineOk[y] && wd.LineDx[y] == 3);
                Console.WriteLine($"  win_wide: screen shaken 3 px: {okL} / 120 lines verified at offset 3");
                True(okL >= 80, "a screen shake keeps the sides on most lines (the line's scroll against the map; plain lines need more proof)");
                // area changes: walk right through group18's exit and on; after the first build, no big allocations
                var m2 = new Machine(d, _ => { }); m2.Reset(); m2.LoadState(StateFile.Load(Path.Combine(dir, "group18.json")));
                var w2 = new Widescreen();
                for (int f = 0; f < 10; f++) { w2.BeforeFrame(m2, d, async: false); m2.RunFrame(0); }
                for (int f = 0; f < 200; f++) { w2.BeforeFrame(m2, d, async: false); m2.RunFrame(f < 120 ? Btn.Right : Btn.Up); }   // warm: the pool fills (2 buffers, once)
                int area0 = m2.Rd(0x6C);
                long a0 = GC.GetAllocatedBytesForCurrentThread();
                for (int f = 0; f < 240; f++) { w2.BeforeFrame(m2, d, async: false); m2.RunFrame(f < 80 ? Btn.Down : Btn.Left); }   // back through the exit
                long alloc = GC.GetAllocatedBytesForCurrentThread() - a0;
                // the same walk without widescreen: what the game itself allocates
                var m3 = new Machine(d, _ => { }); m3.Reset(); m3.LoadState(StateFile.Load(Path.Combine(dir, "group18.json")));
                for (int f = 0; f < 210; f++) m3.RunFrame(f < 10 ? 0 : f < 130 ? Btn.Right : Btn.Up);
                long b0 = GC.GetAllocatedBytesForCurrentThread();
                for (int f = 0; f < 240; f++) m3.RunFrame(f < 80 ? Btn.Down : Btn.Left);
                long game = GC.GetAllocatedBytesForCurrentThread() - b0;
                Console.WriteLine($"  win_wide: 240 more frames (area ${area0:X2} -> ${m2.Rd(0x6C):X2}): {alloc / 1024} KB allocated with widescreen, {game / 1024} KB by the game alone");
                True(alloc - game < 1024 * 1024, "widescreen adds no big arrays in steady state");
                // no-map flag
                var wr = new SceneRaster(m.Ppu.Chr, 560);
                var lines = Enumerable.Repeat(true, 240).ToArray();
                wr.Widen(r, wd, lines);
                int noMap = Enumerable.Range(0, 560 * 240).Count(i => wr.NoMap[i]), outside = 0;
                for (int y = 0; y < 240; y++)
                    for (int x = 0; x < 560; x++)
                    {
                        int ax = x - 152 + wd.MapX + wd.LineDx[y], ay = y + wd.MapY + wd.LineDy[y];
                        bool side = x < 152 || x >= 408;
                        if (side && (ax < 0 || ay < 0 || ax >= wd.Cache!.W || ay >= wd.Cache.H)) outside++;
                    }
                True(noMap == outside && outside > 0, $"outside-area side pixels flagged ({noMap} / {outside})");
            }
            // side sprites: the replay of the sprite pass, drawn on the game's own columns too, reproduces the sprite
            // pixels the PPU shows there (the orbiting sword sprites come from another routine: not all)
            foreach (var (name, keys) in new[] { ("group09", new[] { "2531:UP:50", "2585:RIGHT:330" }), ("group05", Array.Empty<string>()), ("group18", Array.Empty<string>()) })
            {
                var m = new Machine(d, _ => { }); m.Reset(); m.LoadState(StateFile.Load(Path.Combine(dir, name + ".json")));
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                var kp = Keys.Parse(keys);
                var ss = new SideSprites();
                var spr = new byte[256 * 240]; var tg = new int[256 * 240];
                var all = Enumerable.Repeat(true, 240).ToArray();
                long game = 0, same = 0, extra = 0;
                for (int f = 0; f < 150; f++)
                {
                    ss.Snapshot(m);
                    m.RunFrame(kp.TryGetValue(2506 + m.Frame, out int b) ? b : 0);
                    r.Build(m.Ppu.Capture);
                    if (f < 5) continue;
                    var cap = m.Ppu.Capture;
                    int my = Enumerable.Range(16, 160).FirstOrDefault(y => cap.SprValid[y], -1);
                    if (my < 0) continue;
                    var banks = new int[8];
                    for (int k = 0; k < 8; k++) banks[k] = cap.Spr[my].Banks[k];
                    ss.Draw(d, m.Ppu.Chr, banks, cap.Spr[my].Ctrl, 256, 0, spr, tg, all, includeCenter: true);
                    for (int y = 16; y < 180; y++)
                        for (int x = 8; x < 248; x++)
                        {
                            int i = y * 256 + x;
                            if (r.Sp[i] != 0) { game++; if (spr[i] != 0 && (spr[i] & 0x0F) == (r.Sp[i] & 0x0F)) same++; }
                            else if (spr[i] != 0) extra++;
                        }
                }
                Console.WriteLine($"  win_wide: side-sprite replay {name}: {same} / {game} game sprite pixels reproduced ({100.0 * same / Math.Max(1, game):F1} %), {extra} extra");
                True(same >= game * 0.75, $"{name}: the sprite replay matches the game's sprites");
            }
            // playtest: widescreen on from power-on through the title, name entry and into the game (canon keys): no
            // exception in the cache build, the match test, the wide raster
            {
                var m = new Machine(d, _ => { }); m.Reset();
                m.Ppu.Capture = new VideoCapture(); m.Ppu.NoPixels = true;
                var r = new SceneRaster(m.Ppu.Chr) { SpriteLimit = false };
                var wr = new SceneRaster(m.Ppu.Chr, 424);
                var wide = new Widescreen();
                var kp = Keys.Parse("1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80".Split(' '));
                var lines = new bool[240];
                string? err = null; int shown = 0, outsidePlay = 0;
                for (int f = 0; f < 2600 && err == null; f++)
                {
                    try
                    {
                        wide.BeforeFrame(m, d, async: false);
                        m.RunFrame(kp.TryGetValue(214 + m.Frame, out int b) ? b : 0);
                        r.Build(m.Ppu.Capture);
                        wide.AfterFrame(r.Fb, m.Ppu.Pal, 190);
                        wide.MapLines(r.Fb, 190, 0.5f, lines);
                        wr.Widen(r, wide, lines);
                        if (wide.Show > 0.5f) shown++;
                        if (wide.LineOk.Any(o => o) && !(m.Rd(0x40) == 1)) outsidePlay++;
                    }
                    catch (Exception ex) { err = $"frame {f} (main {m.Rd(0x40)} sub {m.Rd(0x41)}): {ex.GetType().Name} {ex.Message}"; }
                }
                Console.WriteLine($"  win_wide: power-on run: {(err ?? "no exception")}, sides shown in {shown} frames");
                True(err == null, "widescreen on through name entry: no exception");
                True(outsidePlay == 0, $"no sides on title / intro / name entry ({outsidePlay} frames)");
            }
            // menus / title: no match, no sides
            var mt = new Machine(d, _ => { }); mt.Reset();
            var wt = new Widescreen();
            for (int f = 0; f < 300; f++) { wt.BeforeFrame(mt, d, async: false); mt.RunFrame(0); wt.AfterFrame(mt.Ppu.Fb, mt.Ppu.Pal, 180); }
            True(!wt.Match && wt.Show == 0, "title / intro: no sides");
        }),
        ("win_mouse", () =>
        {
            // mouse control: client → NES point, direction toward the cursor, and in the game: holding the "mouse" right
            // of / above the player walks it there
            Eq(Btn.Right, MouseControl.Dir(40, 3), "right");
            Eq(Btn.Up | Btn.Left, MouseControl.Dir(-30, -28), "up-left diagonal");
            Eq(0, MouseControl.Dir(3, -4), "inside the dead zone: stand");
            Eq(Btn.Down, MouseControl.Dir(5, 50), "mostly down: straight down");
            var (nx, ny) = MouseControl.ToNes(512, 240, (0, 0, 1024, 960), 256, 0, 240);
            True(Math.Abs(nx - 128) < 1e-9 && Math.Abs(ny - 60) < 1e-9, "client centre-top → NES (128, 60)");
            var (wx, _) = MouseControl.ToNes(0, 0, (0, 0, 848, 480), 424, 0, 240);
            True(Math.Abs(wx + 84) < 1e-9, "widescreen: the left edge is 84 columns left of the game's");
            var d = TheData.Value;
            var m = new Machine(d, _ => { }); m.Reset();
            m.LoadState(StateFile.Load(Path.Combine(Data.FindDir(), "..", "test", "states", "group09.json")));
            for (int f = 0; f < 25; f++) m.RunFrame(0);
            int x0 = m.Rd(0x71) | m.Rd(0x91) << 8, y0 = m.Rd(0xB1);
            for (int f = 0; f < 30; f++) m.RunFrame(MouseControl.Dir(0, -60));   // the cursor 60 px above the player
            int y2 = m.Rd(0xB1);
            for (int f = 0; f < 30; f++) m.RunFrame(MouseControl.Dir(60, 0));    // then 60 px right of it
            int x1 = m.Rd(0x71) | m.Rd(0x91) << 8;
            Console.WriteLine($"  win_mouse: player x {x0} → {x1} walking toward a cursor on the right, y {y0} → {y2} toward one above");
            True(x1 > x0 + 20, "the player walks toward the cursor (right)");
            True(y2 < y0 - 3, "and up");
            var v = VideoSettings.Parse("mouse=0\n");
            True(!v.Mouse && new VideoSettings().Mouse && v.Serialize().Contains("mouse=0"), "ini mouse");
            Eq(108, Window.CommandId("mouse"), "--menu mouse");
        }),
        ("win_screenshot", () =>
        {
            // Game > Take screenshot / Open screenshots folder; names do not overwrite within the same second
            string dir = Path.Combine(Path.GetTempPath(), "rec_shots_" + Environment.ProcessId);
            Directory.CreateDirectory(dir);
            try
            {
                var t = new DateTime(2026, 11, 26, 12, 34, 56);
                string a = Window.ShotPath(dir, t);
                File.WriteAllBytes(a, new byte[1]);
                string b = Window.ShotPath(dir, t);
                File.WriteAllBytes(b, new byte[1]);
                string c = Window.ShotPath(dir, t);
                True(Path.GetFileName(a) == "re-c_20261126_123456.png" && Path.GetFileName(b) == "re-c_20261126_123456_2.png" && Path.GetFileName(c) == "re-c_20261126_123456_3.png", "unique names");
            }
            finally { Directory.Delete(dir, true); }
            Eq(109, Window.CommandId("screenshot"), "--menu screenshot");
            Eq(110, Window.CommandId("screenshots"), "--menu screenshots");
        }),
        ("win_frame_stats", () =>
        {
            // T49 FPS counter: 60 frames per second at 4 ms each, then a slow patch at 30 fps / 20 ms
            var fs = new FrameStats();
            for (int i = 0; i < 180; i++) fs.Record(i * 1000.0 / 60, i * 1000.0 / 60 + 4);
            True(Math.Abs(fs.Fps - 60) < 1 && Math.Abs(fs.Ms - 4) < 1e-9, $"60 fps, 4 ms ({fs.Text})");
            Eq("60 FPS  4.0 MS", fs.Text, "text");
            double t = 3000;
            for (int i = 0; i < 60; i++) { fs.Record(t, t + 20); t += 1000.0 / 30; }
            True(Math.Abs(fs.Fps - 30) < 1.5 && Math.Abs(fs.Ms - 20) < 1e-9, $"30 fps after a second of slow frames ({fs.Text})");
            True(Osd.Build(false, false, null, new List<string>(), stats: fs.Text) && (Osd.Pix[18 * Osd.W + 14] >> 24) > 100, "counter drawn top left");
            True(Osd.Build(false, false, "NOTICE", new List<string>(), stats: fs.Text) && (Osd.Pix[54 * Osd.W + 14] >> 24) > 100, "under a notice");
            var v = VideoSettings.Parse("show_fps=1\n");
            True(v.ShowFps && v.Serialize().Contains("show_fps=1") && new VideoSettings().Serialize().Contains("show_fps=0"), "ini show_fps");
            Eq(291, Window.CommandId("fps"), "--menu fps");
            Eq(289, Window.CommandId("hd:sharpen"), "--menu hd:sharpen");
            True(new VideoSettings().HdSharpen && !VideoSettings.Parse("hd_effects=edges\n").HdSharpen, "ini hd_effects sharpen");
            True((Osd.HelpLines.Length - 1) * 19 + 112 + 14 <= Osd.H - 58, "help lines above the footer");
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
