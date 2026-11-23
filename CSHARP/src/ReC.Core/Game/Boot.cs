// re-c C# port — the lasting effects of the cold-boot path between Reset 31:F2A4 and the first MainLoop pass
// (= web/src/game/boot.js, session 106).
//
//   IntroLeftovers(m)   the RAM / PPU state the bank-18 cold-boot intro (18:8028, f0-213, not ported) leaves
//                       behind at the first 30:C900 (tools.emu, power-on, no input; see REVERSE.md "Boot")
//   ColdSaveInit(m)     ColdSaveInit 31:F1CD (cold path only: $0182-$0184 != "USO")
//   BootSaveCheck(m)    BootSaveCheck 31:F0A4 + the slot checks / resets / repair copies behind it
//   CopySramBlock(m)    CopySramBlock 31:F3A2 ($7D00 -> $6480, $7E00 -> $6580: the one-page-off boot copy)
//
// Order in Reset: (intro) -> ColdSaveInit -> BootSaveCheck -> CopySramBlock -> JMP MainLoop. ColdSaveInit runs
// before the intro, but the intro does not touch WRAM $6480+ / $6D00+ / $7000+, so the port can run all three
// after IntroLeftovers.
namespace ReC.Game;

public static class Boot
{
    private const int TEMPLATE = 0xBE00;          // new-save template page, bank 23 at $A000 ($17)
    private static readonly int[] TAG = { 0x41, 0x4D, 0x49 };   // "AMI" at page + $FD

    // js: game/boot.js T
    private static int T(Machine m, int x) => m.Data.Prg[0x17 * 0x2000 + ((TEMPLATE + x) & 0x1FFF)];

    /// <summary>Intro end state (tools.emu RAM at the first 30:C900, frame 214 line 222); see boot.js INTRO_RAM for
    /// why the port keeps frame_ready $09 = 0 here.</summary>
    private static readonly (int A, int V)[] INTRO_RAM =
    {
        (0x01, 0x1E),                              // ppumask_shadow (BG + sprites on)
        (0x49, 0xFF), (0x4A, 0xFF),
        (0x51, 0x05),                              // irq_mode 5 (NmiMode5 31:F7C5)
        (0x53, 0xF7), (0x54, 0x91), (0x55, 0xF6),  // irq_vector = IrqHandler6 31:F691 (the port uses m.IrqSplits)
        (0x56, 0x06),                              // SetIrqHandler(6)
        (0x58, 0x89), (0x59, 0x28),                // chr_r0_alt $58-$5A: IRQ6 latch $89 (line 137), PPUADDR $2800
        (0x8C, 0x2A), (0x8D, 0x22), (0x8E, 0x28), (0x8F, 0x80),
        (0x06FF, 0xF0),                            // title_ctr (cleared by MainState2's second pass)
        (0x07F0, 0x28), (0x07F1, 0x0E),            // chr_r0_play: R0 $28, R1 $0E, R2-R5 0
    };
    // $06CF-$06EF: the intro's last fade target (pal_target $06D0-$06EF is cleared by MainState2's second pass)
    private static readonly int[] INTRO_06CF = { 0x20, 0x20, 0x0F, 0x1C, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F,
        0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x0F, 0x20, 0x0F, 0x0F, 0x0F, 0x20, 0x0F, 0x0F, 0x0F, 0x20, 0x0F, 0x0F, 0x0F,
        0x20, 0x0F, 0x0F, 0x0F };

    // js: game/boot.js introLeftovers
    public static void IntroLeftovers(Machine m)
    {
        foreach (var (a, v) in INTRO_RAM) m.Wr(a, v);
        for (int i = 0; i < INTRO_06CF.Length; i++) m.Wr(0x06CF + i, INTRO_06CF[i]);
        for (int i = 0; i < 256; i += 4) m.Wr(0x0200 + i, 0xF0);      // OAM shadow: every sprite hidden (y = $F0)
        for (int a = 0x6140; a < 0x6160; a++) m.Wr(a, 0x0F);         // pal_buf: the intro ends faded to black
        Array.Fill(m.Ppu.Pal, (byte)0x0F);                           // ... and its NMIs uploaded that
        m.Ppu.WriteReg(1, 0x1E);
        for (int r = 0; r < 6; r++) m.Ppu.SetChrReg(r, m.Rd(0x07F0 + r));   // ApplyChrPlay as the intro left it
        m.Wr(0x6E, 0x12); m.Wr(0x6F, 0x13);                          // SwitchBank16(9) 31:F399: banks 18/19
        m.PrgR6 = 0x12; m.PrgR7 = 0x13;
    }

    // js: game/boot.js curSaveReset
    /// <summary>CurSaveReset 31:F219: $7D00/$6D00 &lt;- template, $7E00/$7F00/$6E00/$6F00 &lt;- 0, save_reset_flag $7001 = $FF</summary>
    private static void CurSaveReset(Machine m)
    {
        for (int x = 0; x < 256; x++)
        {
            m.Wr(0x6D00 + x, T(m, x)); m.Wr(0x7D00 + x, T(m, x));
            foreach (int a in new[] { 0x6E00, 0x7E00, 0x6F00, 0x7F00 }) m.Wr(a + x, 0);
        }
        m.Wr(0x7001, 0xFF);
    }

    // js: game/boot.js slotReset
    /// <summary>SaveSlot0Reset 31:F11F / SaveSlot1Reset 31:F1A3: primary + backup first page &lt;- template, other pages 0</summary>
    private static void SlotReset(Machine m, int pri, int bak)
    {
        for (int x = 0; x < 256; x++)
        {
            m.Wr(pri + x, T(m, x)); m.Wr(bak + x, T(m, x));
            foreach (int p in new[] { 0x100, 0x200 }) { m.Wr(pri + p + x, 0); m.Wr(bak + p + x, 0); }
        }
        m.Wr(0x7001, 0xFF);
    }

    // js: game/boot.js saveSumSlot
    /// <summary>SaveSumSlot 31:F23D: tmp_16 += 3 pages from `page` (8-bit; $16 is NOT cleared first, the seed quirk).
    /// Leaves the ROM's pointers in $10-$15.</summary>
    private static void SaveSumSlot(Machine m, int page)
    {
        int s = m.Rd(0x16);
        for (int y = 0; y < 256; y++)
            for (int p = 0; p < 3; p++) s = (s + m.Rd(((page + p) << 8) + y)) & 0xFF;
        m.Wr(0x16, s);
        m.Wr(0x10, 0); m.Wr(0x11, page); m.Wr(0x12, 0); m.Wr(0x13, page + 1); m.Wr(0x14, 0); m.Wr(0x15, page + 2);
    }

    // js: game/boot.js slotCopy
    /// <summary>SlotCopy 31:F273: 3 pages src -> dst (pairs at SlotCopyPages 31:F298)</summary>
    private static void SlotCopy(Machine m, int src, int dst)
    {
        for (int i = 0; i < 0x300; i++) m.Wr((dst << 8) + i, m.Rd((src << 8) + i));
        m.Wr(0x10, 0); m.Wr(0x11, src + 3); m.Wr(0x12, 0); m.Wr(0x13, dst + 3);
    }

    // js: game/boot.js same3
    private static bool Same3(Machine m, int a, int b)
    {
        for (int i = 0; i < 0x300; i++) if (m.Rd(a + i) != m.Rd(b + i)) return false;
        return true;
    }

    // js: game/boot.js tagOk
    private static bool TagOk(Machine m, int page)
    {
        for (int i = 0; i < TAG.Length; i++) if (m.Rd(page + 0xFD + i) != TAG[i]) return false;
        return true;
    }

    // js: game/boot.js coldSaveInit
    /// <summary>ColdSaveInit 31:F1CD: $700E/$700F = 0, boot_r7_save $7005 = R7, SetR7($17), JMP CurSaveReset (-> R7 back)</summary>
    public static void ColdSaveInit(Machine m)
    {
        m.Wr(0x700E, 0); m.Wr(0x700F, 0);
        m.Wr(0x7005, m.Rd(0x6F));
        CurSaveReset(m);
    }

    // js: game/boot.js bootSaveCheck
    /// <summary>BootSaveCheck 31:F0A4 (REVERSE.md "Boot save check"). Ends with SetR7($7005) (BootSaveRestoreR7 31:F1C7).</summary>
    public static void BootSaveCheck(Machine m)
    {
        m.Wr(0x7001, 0);
        m.Wr(0x7005, m.Rd(0x6F));
        // slot 0: $7100 primary, $7400 backup, checksums $70F0/$70F1
        if (Same3(m, 0x7100, 0x7400))
        {
            if (!TagOk(m, 0x7100)) SlotReset(m, 0x7100, 0x7400);
        }
        else
        {
            SaveSumSlot(m, 0x71);
            if (m.Rd(0x70F0) == m.Rd(0x16))
            {
                SlotCopy(m, 0x71, 0x74); m.Wr(0x70F0, m.Rd(0x16)); m.Wr(0x70F1, m.Rd(0x16));
            }
            else
            {
                SaveSumSlot(m, 0x74);                       // continues from the failed primary sum
                if (m.Rd(0x70F1) == m.Rd(0x16))
                {
                    SlotCopy(m, 0x74, 0x71); m.Wr(0x70F0, m.Rd(0x16)); m.Wr(0x70F1, m.Rd(0x16));
                }
                else SlotReset(m, 0x7100, 0x7400);
            }
        }
        // slot 1: $7700 / $7A00, $70F2/$70F3; a primary match copies, sets only $70F2, then falls into the backup check
        if (Same3(m, 0x7700, 0x7A00))
        {
            if (!TagOk(m, 0x7700)) SlotReset(m, 0x7700, 0x7A00);
        }
        else
        {
            SaveSumSlot(m, 0x77);
            if (m.Rd(0x70F2) == m.Rd(0x16)) { SlotCopy(m, 0x77, 0x7A); m.Wr(0x70F2, m.Rd(0x16)); }
            SaveSumSlot(m, 0x7A);                           // 31:F18A
            if (m.Rd(0x70F3) == m.Rd(0x16)) { SlotCopy(m, 0x7A, 0x77); m.Wr(0x70F3, m.Rd(0x16)); }
            else SlotReset(m, 0x7700, 0x7A00);
        }
        // BootCheckCurrent 31:F1E2: $7D00-$7FFF == $6D00-$6FFF and the AMI tag at $7DFD
        if (!Same3(m, 0x6D00, 0x7D00) || !TagOk(m, 0x7D00)) CurSaveReset(m);
        Prg.SetR7(m, m.Rd(0x7005));
    }

    // js: game/boot.js copySramBlock
    /// <summary>CopySramBlock 31:F3A2</summary>
    public static void CopySramBlock(Machine m)
    {
        for (int x = 0; x < 256; x++) { m.Wr(0x6480 + x, m.Rd(0x7D00 + x)); m.Wr(0x6580 + x, m.Rd(0x7E00 + x)); }
    }
}
