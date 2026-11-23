// re-c C# port — SoundUpdate 31:F7FE (= web/src/game/sound/soundupdate.js, Ph7 web 7/8, s56): one driver frame.
// Called from IRQ handler 1 (gameplay HUD split, F4CF), IRQ handler 5 (F611) and the NMI-mode tails F6DC (mode 0/7,
// rendering off) / F76B / F77F (mode 3, title). On an area-load frame both F4CF and F6DC run it, so the driver
// ticks twice (REVERSE.md s42).
//
// Maps R6 = 24, R7 = 25 directly (no shadow update); re-issues the interrupted music when snd_cur_music $0102 b7
// is set and the jingle has ended; else runs at most one queued command; then SndTick; restores the banks
// from $6E/$6F (SetR7, SetR6: $50 ends as 6).
namespace ReC.Game.Sound;

public static class SoundUpdate
{
    // js: game/sound/soundupdate.js soundUpdate
    /// <summary>SoundUpdate 31:F7FE</summary>
    public static void Run(Machine m)
    {
        m.PrgR6 = 0x18; m.PrgR7 = 0x19;
        bool resume = false;
        if ((m.Rd(0x0102) & 0x80) != 0)
        {                                                        // F810
            if ((m.Rd(0xFE) & 0x80) != 0)
            {
                int v = (m.Rd(0x0168) | m.Rd(0x0169) | m.Rd(0x016A) | m.Rd(0x016B)) & 0x0F;
                resume = v == 0;
            }
            else resume = m.Rd(0x0111) == 0xFF;                  // F82B
        }
        if (resume)
        {                                                        // F832
            int id = m.Rd(0x0102) & 0x7F;
            m.Wr(0x0102, id); m.Wr(0x0110, id);
            Driver.SndCommand(m);
        }
        else if (m.Rd(0x0101) == 0 && m.Rd(0x0100) != 0)
        {                                                        // F843
            m.Wr(0x0110, m.Rd(0x0103));
            Driver.SndCommand(m);
            for (int y = 0; y < 6; y++) m.Wr(0x0103 + y, m.Rd(0x0104 + y));
            m.Wr(0x0100, (m.Rd(0x0100) - 1) & 0xFF);
        }
        m.PrgR7 = 0x19;                                          // F866
        Driver.SndTick(m);
        Prg.SetR7(m, m.Rd(0x6F));                                // F873
        Prg.SetR6(m, m.Rd(0x6E));
    }
}
