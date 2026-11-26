// re-c Windows host — video settings (Video menu, VideoMenu.cs): aspect ratio / widescreen, integer or free scaling,
// overscan crop, smoothing, window size, fullscreen mode + resolution, menu bar. Pure logic (no Win32 call), so the
// layout maths and the settings file are unit-tested on Linux (tests/ReC.Tests/WinHostTests.cs).
//
// Settings file: %APPDATA%\re-c\video.ini (`--video-config FILE` overrides), plain `key=value` lines, written whenever a
// menu choice changes something. Unknown keys / bad values are ignored (defaults kept), so an old or hand-edited file
// never stops the game from starting.
namespace ReC.Win;

internal enum Aspect { Square, Tv, FourThree, SixteenTen, SixteenNine, Fill }

internal sealed class VideoSettings
{
    public const int SrcW = 256, SrcH = 240, Overscan = 8;

    public Aspect Aspect = Aspect.Square;
    /// <summary>true = whole-number vertical scale (sharp, black bars); false = as large as the window allows.</summary>
    public bool Integer = true;
    /// <summary>hide the top and bottom 8 lines (a TV's overscan): 256x224 shown.</summary>
    public bool CropOverscan;
    /// <summary>GDI HALFTONE stretch (soft) instead of nearest pixel.</summary>
    public bool Smooth;
    /// <summary>window size: Scale (1-6) x the picture, or a fixed client size when WindowW > 0.</summary>
    public int Scale = 3, WindowW, WindowH;
    public bool Fullscreen;
    /// <summary>fullscreen resolution: 0 x 0 = borderless at the desktop resolution, else an exclusive display mode.</summary>
    public int FsW, FsH;
    public bool MenuBar = true;
    /// <summary>pixel-art upscale filter factor (Upscale.cs): 1 = none, 2-4 = Scale2x / 3x / 4x</summary>
    public int Upscale = 1;
    /// <summary>T10: Enhanced (16-bit) graphics (Enhance.cs) instead of the original tiles</summary>
    public bool Enhanced;
    /// <summary>T14: with Enhanced, the "32-bit" look (soft lighting, shadows, anti-aliasing) instead of the 16-bit one</summary>
    public bool Enhanced32;
    /// <summary>T18: with Enhanced, the HD look (Gpu.cs: Direct3D 11 lighting, shadows, bloom at the window's resolution)</summary>
    public bool Hd;
    /// <summary>T19: HD effects (Video > Graphics): parallax depth, shadows, atmosphere (clouds, mist, dust), material detail</summary>
    public bool HdParallax = true, HdShadows = true, HdAtmosphere = true, HdDetail = true;
    /// <summary>T20: HD engine effects: water reflections, local lights (fire, lamps), every sprite on a line (no flicker)</summary>
    public bool HdReflections = true, HdLights = true, HdNoFlicker = true;
    /// <summary>T25: the starter packs built into the exe, in the HD engine</summary>
    public bool HdArt = true;
    /// <summary>T30: HD presents with vsync (no tearing)</summary>
    public bool HdVsync = true;
    /// <summary>T32: HD draws the overscan lines 232-239 and the game's masked left column as black (a TV bezel)</summary>
    public bool HdEdges = true;
    /// <summary>T48: HD sharpening (hd_effects sharpen)</summary>
    public bool HdSharpen = true;
    /// <summary>T51: cinematic cut-scenes (hd_effects cinematic)</summary>
    public bool HdCinematic = true;
    /// <summary>T52: smooth outlines (hd_effects outlines)</summary>
    public bool HdSmooth = true;
    /// <summary>T53: ambient bars (hd_effects ambient)</summary>
    public bool HdAmbient = true;
    /// <summary>T55: tilt-shift (hd_effects tiltshift)</summary>
    public bool HdTilt = true;
    /// <summary>T59: modern font (hd_effects font)</summary>
    public bool HdFont = true;
    /// <summary>T34: HD HUD style (smooth bevelled text, soft shadows)</summary>
    public bool HdHud = true;
    /// <summary>T31: HD quality 2 full, 1 balanced, 0 fast (Gpu.Quality)</summary>
    public int HdQuality = 2;
    /// <summary>T36: cheats (Cheats menu; ini cheats=life,magic,..)</summary>
    public readonly Cheats Cheats = new();
    /// <summary>T38: turbo A / B (Game menu; ini turbo=a,b): a held button repeats, 3 frames down, 3 up</summary>
    public bool TurboA, TurboB;
    /// <summary>T40: master volume in % (Audio > Volume; ini volume=)</summary>
    public int Volume = 100;
    /// <summary>T45: CRT look for Original graphics (Video menu; ini crt=1)</summary>
    public bool Crt;
    /// <summary>T49: FPS counter on screen (F2 / Video > Show FPS; ini show_fps=1)</summary>
    public bool ShowFps;
    /// <summary>T60 Video > Display: brightness 0..4 (2 = as is), vivid colour, colour-blind assist (0 off, 1 protan, 2 deutan,
    /// 3 tritan); ini brightness=, vivid=, colorblind=</summary>
    public int Brightness = 2, ColorBlind;
    /// <summary>T61 Game > Controls: one extra key per button (Input.Order; 0 = none); ini keybinds=a:58,b:5A,..</summary>
    public int[] KeyBinds = new int[8];
    public bool Vivid;
    /// <summary>T57: damage feedback (Game menu; ini feedback=1): HD red flash / low-life pulse, controller rumble</summary>
    public bool Feedback = true;
    public static int Turbo(int buttons, int frame, bool a, bool b)
    {
        bool up = frame / 3 % 2 == 1;
        if (a && up) buttons &= ~0x80;
        if (b && up) buttons &= ~0x40;
        return buttons;
    }
    /// <summary>T13 Audio menu: CD quality (CdAudio.cs) instead of the original mono stream; stereo / reverb for it</summary>
    public bool CdAudio, AudioStereo = true, AudioReverb;
    /// <summary>T28: Audio > Remastered (CdMixer.Remaster: chorus, smooth bass, room, EQ); implies the CD device path</summary>
    public bool AudioRemaster;
    /// <summary>T50: Audio > Orchestrated (Orchestra.cs); implies the CD device path</summary>
    public bool AudioOrchestra;
    /// <summary>T47: muted channels for CD / Remastered (Audio > Channels; ini audio_mute=pulse1,..), CdMixer.Mute bits</summary>
    public int AudioMute;
    public static readonly string[] ChannelNames = { "pulse1", "pulse2", "triangle", "noise", "dmc" };
    /// <summary>T47: headphone crossfeed (ini crossfeed=1)</summary>
    public bool Crossfeed;
    /// <summary>T54: ambient sounds (Audio > Ambient sounds; ini ambience=1, HD graphics only)</summary>
    public bool Ambience = true;

    public static readonly (Aspect A, string Key, string Label)[] Aspects =
    {
        (Aspect.Square, "square", "&Square pixels (256:240)"),
        (Aspect.Tv, "tv", "NES &TV (8:7 pixels)"),
        (Aspect.FourThree, "4:3", "&4:3"),
        (Aspect.SixteenTen, "16:10", "16:1&0 widescreen"),
        (Aspect.SixteenNine, "16:9", "16:&9 widescreen"),
        (Aspect.Fill, "fill", "Stretch to &fill"),
    };

    /// <summary>window-size presets (client area) besides the 1x-6x scales</summary>
    public static readonly (int W, int H)[] Presets =
    {
        (640, 480), (800, 600), (1024, 768), (1280, 720), (1280, 960), (1366, 768), (1600, 900), (1920, 1080),
    };

    public const int MaxScale = 6;

    public int VisibleH => CropOverscan ? SrcH - 2 * Overscan : SrcH;
    public int FirstLine => CropOverscan ? Overscan : 0;

    /// <summary>shown width / height of the picture (Fill: the window decides; square pixels used for sizing)</summary>
    public double Ratio => Aspect switch
    {
        Aspect.Tv => SrcW * 8.0 / 7.0 / VisibleH,
        Aspect.FourThree => 4.0 / 3.0,
        Aspect.SixteenTen => 16.0 / 10.0,
        Aspect.SixteenNine => 16.0 / 9.0,
        _ => (double)SrcW / VisibleH,
    };

    /// <summary>
    /// Where the picture goes in a cw x ch client area (centred; the rest is painted black). Integer: the largest
    /// whole-number vertical scale whose width also fits (at least 1x, which may overflow a tiny window); otherwise the
    /// largest rectangle of the aspect ratio. Fill: the whole client area.
    /// </summary>
    public (int X, int Y, int W, int H) Layout(int cw, int ch)
    {
        if (Aspect == Aspect.Fill) return (0, 0, cw, ch);
        double r = Ratio;
        int w, h;
        if (Integer)
        {
            int s = Math.Max(1, ch / VisibleH);
            while (s > 1 && (int)Math.Round(s * VisibleH * r) > cw) s--;
            h = s * VisibleH;
            w = (int)Math.Round(h * r);
        }
        else
        {
            h = ch;
            w = (int)Math.Round(ch * r);
            if (w > cw) { w = cw; h = (int)Math.Round(cw / r); }
        }
        return ((cw - w) / 2, (ch - h) / 2, w, h);
    }

    /// <summary>client size of the window: the preset, or Scale x the picture at the aspect ratio</summary>
    public (int W, int H) ClientSize() => WindowW > 0 ? (WindowW, WindowH) : ClientSizeAt(Scale);

    public (int W, int H) ClientSizeAt(int scale)
    {
        int h = scale * VisibleH;
        double r = Aspect == Aspect.Fill ? (double)SrcW / VisibleH : Ratio;
        return ((int)Math.Round(h * r), h);
    }

    public void SetScale(int s) { Scale = Math.Clamp(s, 1, MaxScale); WindowW = WindowH = 0; }

    public VideoSettings Clone() => (VideoSettings)MemberwiseClone();

    // ---- settings file
    public static string DefaultPath() => Path.Combine(Path.GetDirectoryName(Save.DefaultPath())!, "video.ini");

    public string Serialize() => string.Join("\n", new[]
    {
        "# re-c video settings (Video menu); delete this file to get the defaults back",
        $"aspect={Aspects.First(a => a.A == Aspect).Key}",
        $"scaling={(Integer ? "integer" : "fit")}",
        $"crop_overscan={(CropOverscan ? 1 : 0)}",
        $"smooth={(Smooth ? 1 : 0)}",
        $"window={(WindowW > 0 ? $"{WindowW}x{WindowH}" : $"{Scale}x")}",
        $"fullscreen={(Fullscreen ? 1 : 0)}",
        $"fullscreen_mode={(FsW > 0 ? $"{FsW}x{FsH}" : "desktop")}",
        $"menu_bar={(MenuBar ? 1 : 0)}",
        $"graphics={(!Enhanced ? "original" : Hd ? "hd" : Enhanced32 ? "enhanced32" : "enhanced")}",
        $"hd_effects={HdEffects()}",
        $"hd_quality={(HdQuality >= 2 ? "full" : HdQuality == 1 ? "balanced" : "fast")}",
        $"audio={(AudioOrchestra ? "orchestra" : AudioRemaster ? "remaster" : CdAudio ? "cd" : "original")}",
        $"audio_stereo={(AudioStereo ? 1 : 0)}",
        $"audio_reverb={(AudioReverb ? 1 : 0)}",
        $"volume={Volume}",
        $"crt={(Crt ? 1 : 0)}",
        $"show_fps={(ShowFps ? 1 : 0)}",
        $"brightness={Brightness}",
        $"keybinds={(KeyBinds.All(k => k == 0) ? "none" : string.Join(",", Enumerable.Range(0, 8).Where(i => KeyBinds[i] != 0).Select(i => $"{Input.Names[i].ToLowerInvariant()}:{KeyBinds[i]:X2}")))}",
        $"vivid={(Vivid ? 1 : 0)}",
        $"colorblind={ColorAdjust.BlindNames[ColorBlind]}",
        $"feedback={(Feedback ? 1 : 0)}",
        $"audio_mute={(AudioMute == 0 ? "none" : string.Join(",", ChannelNames.Where((_, i) => (AudioMute >> i & 1) != 0)))}",
        $"crossfeed={(Crossfeed ? 1 : 0)}",
        $"ambience={(Ambience ? 1 : 0)}",
        $"upscale={ReC.Win.Upscale.Filters.First(f => f.K == Upscale).Key}",
        $"cheats={Cheats.Serialize()}",
        $"turbo={(TurboA && TurboB ? "a,b" : TurboA ? "a" : TurboB ? "b" : "none")}",
    }) + "\n";

    private string HdEffects()
    {
        var e = new List<string>();
        if (HdParallax) e.Add("parallax");
        if (HdShadows) e.Add("shadows");
        if (HdAtmosphere) e.Add("atmosphere");
        if (HdDetail) e.Add("detail");
        if (HdReflections) e.Add("reflections");
        if (HdLights) e.Add("lights");
        if (HdNoFlicker) e.Add("noflicker");
        if (HdArt) e.Add("art");
        if (HdVsync) e.Add("vsync");
        if (HdEdges) e.Add("edges");
        if (HdHud) e.Add("hud");
        if (HdSharpen) e.Add("sharpen");
        if (HdCinematic) e.Add("cinematic");
        if (HdSmooth) e.Add("outlines");
        if (HdAmbient) e.Add("ambient");
        if (HdTilt) e.Add("tiltshift");
        if (HdFont) e.Add("font");
        return e.Count > 0 ? string.Join(",", e) : "none";
    }

    public static VideoSettings Parse(string text)
    {
        var v = new VideoSettings();
        foreach (string raw in text.Split('\n'))
        {
            string line = raw.Trim();
            int eq = line.IndexOf('=');
            if (line.StartsWith('#') || eq < 0) continue;
            string k = line[..eq].Trim().ToLowerInvariant(), val = line[(eq + 1)..].Trim().ToLowerInvariant();
            switch (k)
            {
                case "aspect":
                    foreach (var a in Aspects) if (a.Key == val) v.Aspect = a.A;
                    break;
                case "scaling": if (val is "integer" or "fit") v.Integer = val == "integer"; break;
                case "crop_overscan": v.CropOverscan = val == "1"; break;
                case "smooth": v.Smooth = val == "1"; break;
                case "window":
                    if (val.EndsWith('x') && int.TryParse(val[..^1], out int s) && s is >= 1 and <= MaxScale) v.SetScale(s);
                    else if (TryWxH(val, out int w, out int h) && w >= 128 && h >= 112) { v.WindowW = w; v.WindowH = h; }
                    break;
                case "fullscreen": v.Fullscreen = val == "1"; break;
                case "fullscreen_mode":
                    if (val == "desktop") v.FsW = v.FsH = 0;
                    else if (TryWxH(val, out int fw, out int fh) && fw >= 320 && fh >= 200) { v.FsW = fw; v.FsH = fh; }
                    break;
                case "menu_bar": v.MenuBar = val != "0"; break;
                case "audio":
                    if (val is "original" or "cd" or "remaster" or "orchestra") { v.CdAudio = val == "cd"; v.AudioRemaster = val == "remaster"; v.AudioOrchestra = val == "orchestra"; }
                    break;
                case "audio_stereo": v.AudioStereo = val != "0"; break;
                case "hd_effects":
                {
                    var e = val.Split(',', StringSplitOptions.TrimEntries);
                    v.HdParallax = e.Contains("parallax"); v.HdShadows = e.Contains("shadows");
                    v.HdAtmosphere = e.Contains("atmosphere"); v.HdDetail = e.Contains("detail");
                    v.HdReflections = e.Contains("reflections"); v.HdLights = e.Contains("lights"); v.HdNoFlicker = e.Contains("noflicker"); v.HdArt = e.Contains("art"); v.HdVsync = e.Contains("vsync"); v.HdEdges = e.Contains("edges"); v.HdHud = e.Contains("hud"); v.HdSharpen = e.Contains("sharpen"); v.HdCinematic = e.Contains("cinematic"); v.HdSmooth = e.Contains("outlines"); v.HdAmbient = e.Contains("ambient"); v.HdTilt = e.Contains("tiltshift"); v.HdFont = e.Contains("font");
                    break;
                }
                case "hd_quality":
                    if (val is "full" or "balanced" or "fast") v.HdQuality = val == "full" ? 2 : val == "balanced" ? 1 : 0;
                    break;
                case "audio_reverb": v.AudioReverb = val == "1"; break;
                case "volume": if (int.TryParse(val, out int vol)) v.Volume = Math.Clamp(vol, 0, 100); break;
                case "crt": v.Crt = val == "1"; break;
                case "show_fps": v.ShowFps = val == "1"; break;
                case "brightness": if (int.TryParse(val, out int br)) v.Brightness = Math.Clamp(br, 0, 4); break;
                case "vivid": v.Vivid = val == "1"; break;
                case "keybinds":
                    foreach (var kb in val.Split(','))
                    {
                        var kp = kb.Split(':');
                        int bi = kp.Length == 2 ? Array.IndexOf(Input.Names, kp[0].ToUpperInvariant()) : -1;
                        if (bi >= 0 && int.TryParse(kp[1], System.Globalization.NumberStyles.HexNumber, null, out int vk) && vk is > 0 and < 256) v.KeyBinds[bi] = vk;
                    }
                    break;
                case "colorblind": { int cb = Array.IndexOf(ColorAdjust.BlindNames, val); if (cb >= 0) v.ColorBlind = cb; break; }
                case "feedback": v.Feedback = val == "1"; break;
                case "audio_mute":
                {
                    var t = val.Split(','); v.AudioMute = 0;
                    for (int i = 0; i < ChannelNames.Length; i++) if (t.Contains(ChannelNames[i])) v.AudioMute |= 1 << i;
                    break;
                }
                case "crossfeed": v.Crossfeed = val == "1"; break;
                case "ambience": v.Ambience = val == "1"; break;
                case "cheats": v.Cheats.Parse(val); break;
                case "turbo": { var t = val.Split(','); v.TurboA = t.Contains("a"); v.TurboB = t.Contains("b"); break; }
                case "graphics":
                    if (val is "original" or "enhanced" or "enhanced32" or "hd")
                    {
                        v.Enhanced = val != "original"; v.Enhanced32 = val == "enhanced32"; v.Hd = val == "hd";
                    }
                    break;
                case "upscale":
                    foreach (var f in ReC.Win.Upscale.Filters) if (f.Key == val) v.Upscale = f.K;
                    break;
            }
        }
        return v;
    }

    public static bool TryWxH(string s, out int w, out int h)
    {
        w = h = 0;
        int x = s.IndexOf('x');
        return x > 0 && int.TryParse(s[..x], out w) && int.TryParse(s[(x + 1)..], out h) && w > 0 && h > 0;
    }

    public static VideoSettings Load(string path)
    {
        try { return File.Exists(path) ? Parse(File.ReadAllText(path)) : new VideoSettings(); }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return new VideoSettings(); }
    }

    public bool TrySave(string path)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
            File.WriteAllText(path, Serialize());
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException) { return false; }
    }

    /// <summary>
    /// Display modes for the Fullscreen resolution menu: distinct W x H at 32 bpp, at least 640 x 480, sorted; at most
    /// `max` (the largest ones kept).
    /// </summary>
    public static List<(int W, int H)> ModeList(IEnumerable<(int W, int H, int Bpp)> modes, int max = 24)
    {
        var l = modes.Where(m => m.Bpp == 32 && m.W >= 640 && m.H >= 480).Select(m => (m.W, m.H)).Distinct()
            .OrderBy(m => m.W).ThenBy(m => m.H).ToList();
        return l.Count > max ? l.GetRange(l.Count - max, max) : l;
    }
}
