// re-c C# port — controller reads (= web/src/game/pads.js: ReadPads 31:FE80, ReadPadsMenu 31:FEE0, ReadPadStable
// 31:FF17, ReadPadRaw 31:FF28). Button byte = A b7, B b6, Select b5, Start b4, Up b3, Down b2, Left b1, Right b0.
// The port has no $4016 shift register: m.Buttons[0/1] is what ReadPadRaw would assemble this frame.
// Both routines process pad 2 (X=1) first, then pad 1 (X=0), so pad_raw $4F ends as pad 1's byte.
using ReC.Gen;

namespace ReC.Game;

public static class Pads
{
    // js: game/pads.js readStable
    /// <summary>ReadPadStable: reads until two agree; also leaves the raw value in pad_dir[x] (overwritten by ReadPads)</summary>
    private static int ReadStable(Machine m, int x)
    {
        int raw = m.Buttons[x] & 0xFF;
        m.Wr(R.PadDir + x, raw);
        m.Wr(R.PadRaw, raw);
        return raw;
    }

    // js: game/pads.js readPads
    /// <summary>ReadPads 31:FE80 — gameplay: pad_dir from PadDirTable, pad_new, A/B hold counters, pad_act.
    /// Returns the Y register it leaves (pad 1's B pulse: 0 or $80; s110, SubstPlay's PlaySound Y). X ends at $FF.</summary>
    public static int ReadPads(Machine m)
    {
        var dirTable = m.PadDirTable ??= Data.Table(m.Data, "PadDirTable");
        int yOut = 0;
        for (int x = 1; x >= 0; x--)
        {
            int raw = ReadStable(m, x);
            m.Wr(R.PadDir + x, dirTable[raw & 0x0F]);
            int nw = ~m.Rd(R.PadPrev + x) & raw & 0xFF;
            m.Wr(R.PadNew + x, nw);
            m.Wr(R.PadPrev + x, raw);
            if ((nw & 0x80) != 0) m.Wr(R.PadHoldA + x, 0);
            if ((nw & 0x40) != 0) m.Wr(R.PadHoldB + x, 0);
            int c = 0;                                   // carry -> pad_act b7 (A held: every frame from the 32nd)
            if ((raw & 0x80) != 0)
            {
                int h = (m.Rd(R.PadHoldA + x) + 1) & 0xFF;
                if ((h & 0x1F) == 0) { h = (h - 1) & 0xFF; c = 1; }
                m.Wr(R.PadHoldA + x, h);
            }
            int y = 0;                                   // -> pad_act b6 (B held: one-frame pulse every 32 frames)
            if ((raw & 0x40) != 0)
            {                                            // BVC on the V flag of BIT pad_raw (b6)
                int h = (m.Rd(R.PadHoldB + x) + 1) & 0xFF;
                m.Wr(R.PadHoldB + x, h);
                if ((h & 0x1F) == 0) y = 0x80;
            }
            m.Wr(R.PadAct + x, ((c << 7) | (y >> 1) | nw) & 0xFF);
            yOut = y;
        }
        return yOut;
    }

    // js: game/pads.js readPadsMenu
    /// <summary>ReadPadsMenu 31:FEE0 — menus: pad_new + d-pad auto-repeat via pad_hold_a ($0C delay, then every 3)</summary>
    public static void ReadPadsMenu(Machine m)
    {
        for (int x = 1; x >= 0; x--)
        {
            int raw = ReadStable(m, x);                  // pad_dir[x] keeps the raw byte here
            int prev = m.Rd(R.PadPrev + x);
            int nw = ~prev & raw & 0xFF;
            if ((nw & 0x0F) != 0) m.Wr(R.PadHoldA + x, 0x0C);
            if (((prev ^ raw) & 0x0F) == 0)
            {
                int h = (m.Rd(R.PadHoldA + x) - 1) & 0xFF;
                m.Wr(R.PadHoldA + x, h);
                if (h == 0)
                {
                    m.Wr(R.PadHoldA + x, 3);
                    nw |= raw & 0x0F;
                }
            }
            m.Wr(R.PadNew + x, nw);
            m.Wr(R.PadPrev + x, raw);
        }
    }
}
