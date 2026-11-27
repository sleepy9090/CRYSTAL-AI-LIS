// re-c Windows host — spoken dialogue (playtest 6: "when the text from the first npc occurs, make it audio as well").
// Not in the web port.
//
//   DialogueReader  the page the game's text printer built in WRAM $6000 (TextPrint 20:853A: 8 rows x 32 tiles, each row
//                   FE 1E, 29 characters, 1F; TextClearBuf 20:8720), read while a message window is open in gameplay
//                   (main_state 1, irq_mode 1 / 9: ShowMessage 30:D354). A page counts once it has stayed the same for
//                   Settle frames (TextPrint can span a few frames while the upload queue drains); each page is said
//                   once, a new page replaces the one being said, and closing the window stops the voice.
//   Speech          Windows' built-in text-to-speech (SAPI 5 SpVoice over raw COM, no packages); null when the PC has
//                   no speech engine (then the game simply stays silent).
using System.Text;

namespace ReC.Win;

internal sealed class DialogueReader
{
    public const int Buf = 0x6000, Settle = 4;
    private string _pending = "", _said = "";
    private int _stable;
    private bool _open;

    /// <summary>the page in the text buffer as words, or "" when the buffer is not a text page</summary>
    public static string Page(Func<int, int> rd)
    {
        var sb = new StringBuilder();
        for (int row = 0; row < 8; row++)
        {
            int o = Buf + row * 32;
            int b0 = rd(o);
            if ((b0 != 0xFE && b0 != 0x40) || rd(o + 1) != 0x1E || rd(o + 31) != 0x1F) return "";   // the frame, attributes, a map buffer
            var line = new StringBuilder();
            for (int x = 2; x < 31; x++)
            {
                int c = rd(o + x);
                if (c == 0x5F) line.Append("...");                                        // the game's ellipsis tile
                else if (c is >= 0x20 and < 0x7F && (char.IsAsciiLetterOrDigit((char)c) || " !\"&',-.:;?".Contains((char)c))) line.Append((char)c);
                else line.Append(' ');                                                     // icons, arrows, the page prompt
            }
            string t = line.ToString().Trim();
            if (t.Length == 0) continue;
            if (sb.Length > 0)
            {
                if (sb[^1] == '-' && char.IsLetter(t[0])) sb.Length--;                     // a word broken over two lines
                else sb.Append(' ');
            }
            sb.Append(t);
        }
        string s = System.Text.RegularExpressions.Regex.Replace(sb.ToString(), " {2,}", " ").Trim();
        return s.Any(char.IsLetter) ? s : "";
    }

    /// <summary>after a frame: (text to say now or null, whether to stop the voice)</summary>
    public (string? Say, bool Stop) Update(int mainState, int irqMode, Func<int, int> rd)
    {
        bool open = mainState == 1 && irqMode is 1 or 9;
        if (!open)
        {
            bool stop = _open;
            _open = false; _pending = ""; _said = ""; _stable = 0;
            return (null, stop);
        }
        _open = true;
        string page = Page(rd);
        if (page != _pending) { _pending = page; _stable = 0; return (null, false); }
        if (page.Length == 0 || page == _said || ++_stable < Settle) return (null, false);
        _said = page;
        return (page, false);
    }
}

internal sealed unsafe class Speech : IDisposable
{
    private void* _v;
    private const uint SpfAsync = 1, SpfPurge = 2, SpfNotXml = 0x10;

    private Speech(void* v) { _v = v; }

    private static void** Vt(void* o) => *(void***)o;

    /// <summary>SpVoice, or null when no speech engine is installed / COM fails</summary>
    public static Speech? TryCreate()
    {
        try
        {
            Win32.CoInitializeEx(null, 2);                                   // apartment; "changed mode" is fine too
            Guid clsid = new("96749377-3391-11D2-9EE3-00C04F797396"), iid = new("6C44DF74-72B9-4992-A1EC-EF996E0422D4");
            void* v;
            if (Win32.CoCreateInstance(&clsid, null, 0x17, &iid, &v) < 0 || v == null) return null;
            var s = new Speech(v);
            ((delegate* unmanaged[Stdcall]<void*, int, int>)Vt(v)[28])(v, 1);          // ISpVoice::SetRate: a little brisk
            return s;
        }
        catch (Exception) { return null; }
    }

    /// <summary>ISpVoice::SetVolume (0-100)</summary>
    public void Volume(int pct)
    {
        if (_v != null) ((delegate* unmanaged[Stdcall]<void*, ushort, int>)Vt(_v)[30])(_v, (ushort)Math.Clamp(pct, 0, 100));
    }

    /// <summary>says the text at once, cutting off anything still being said (ISpVoice::Speak, asynchronous)</summary>
    public void Say(string text)
    {
        if (_v == null) return;
        fixed (char* p = text)
            ((delegate* unmanaged[Stdcall]<void*, char*, uint, uint*, int>)Vt(_v)[20])(_v, p, SpfAsync | SpfPurge | SpfNotXml, null);
    }

    public void Stop() => Say("");

    public void Dispose()
    {
        if (_v == null) return;
        Stop();
        ((delegate* unmanaged[Stdcall]<void*, uint>)Vt(_v)[2])(_v);
        _v = null;
    }
}
