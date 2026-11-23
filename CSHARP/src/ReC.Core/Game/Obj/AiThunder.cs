// re-c C# port — AI $1A, the sword-4 level-3 charged shot (= web/src/game/obj/ai_thunder.js, s197). REVERSE.md
// "AI $1A ThunderStorm". Shot id $0F = (sword 4 - 1) * 4 | charge level 3 (26:9CA7) spawns type $1F in slot 4 with
// obj_active $1A; ObjUpdateAll sends id $1A to ObjUpdateAi1A 30:CD7F instead of ObjAiTable (whose $1A entry 27:B2F4
// is an RTS).
//
//   ThunderStep(m, x)  iterator = 30:CD8A, one step per ObjUpdateAll pass (it waits on frames inside the pass):
//     30:CDD5  SetR6($1A), SpawnShot($1E, dir 0) (type $1B bolt, slots 5-$0B) placed on slot $0D + (phase & $0F)
//     WaitPpuQueue, 30:CE07 ($6000-$60BF = $7F), irq_mode saved, 30:CE12 step [obj_phase], irq_mode restored,
//     obj_phase++; at $6A: obj_active = 0, WaitFrame, irq_mode = 0, PaletteBuild. Else if irq_mode != 0:
//     obj_flags slot 1 |= $10 (the slot-0 ORA at CDCC is dead: its result is overwritten by LDA, no STA).
//   Step record 30:CF47 + 3 * phase (Y = 3p & $FF, the ADC carry picks the $D047 copy = the same table); see the JS.
//   30:CF28: irq_mode = 2, CHR alt R0/R1 = $34, R2-R5 alt = the play set $07F2-$07F5.
using ReC.Game.States;
using ReC.Gen;

namespace ReC.Game.Obj;

public static class AiThunder
{
    private const int Phase = 0x0600, Active = 0x04A0, Flags = 0x0380;
    private static readonly int[] PosBases = [0x70, 0x90, 0xB0, 0xD0];

    // js: game/obj/ai_thunder.js irqAlt
    /// <summary>30:CF28</summary>
    private static void IrqAlt(Machine m)
    {
        m.Wr(R.IrqMode, 2);
        m.Wr(R.ChrR0Alt, 0x34);
        m.Wr(R.ChrR0Alt + 1, 0x34);
        for (int i = 2; i < 6; i++) m.Wr(R.ChrR0Alt + i, m.Rd(0x07F0 + i));
    }

    // js: game/obj/ai_thunder.js spawnBolt
    /// <summary>30:CDD5: a bolt on enemy slot $0D + (phase &amp; $0F) (copied even when that slot is empty)</summary>
    private static void SpawnBolt(Machine m, int x)
    {
        Prg.SetR6(m, 0x1A);
        if (!ObjUtil.SpawnShot(m, x, 0x1E, 0)) return;
        m.Wr(0x11, x);
        int src = (m.Rd(Phase + x) & 0x0F) + 0x0D, dst = m.Rd(0x10);
        foreach (int a in PosBases) m.Wr(a + dst, m.Rd(a + src));
    }

    // js: game/obj/ai_thunder.js nibbleRow
    /// <summary>30:CE89: 8 bytes at ($10),Y -> 16 nibble tiles $70+n at $6000 + x; returns (X + $20, Y)</summary>
    private static (int X, int Y) NibbleRow(Machine m, int x, int y)
    {
        int ptr = (m.Rd(0x11) << 8) | m.Rd(0x10);
        m.Wr(0x12, 8);
        do
        {
            int b = Prg.CpuRd(m, ptr + y);
            m.Wr(0x6000 + x, (b >> 4) + 0x70);
            m.Wr(0x6001 + x, (b & 0x0F) + 0x70);
            x += 2; y++;
            m.Wr(0x12, m.Rd(0x12) - 1);
        } while (m.Rd(0x12) != 0);
        return (x + 0x10, y);
    }

    // js: game/obj/ai_thunder.js step
    /// <summary>30:CE12: the step for phase a</summary>
    private static IEnumerable<Y> Step(Machine m, int a)
    {
        m.Wr(0x10, a);
        int t = ((a << 1) & 0xFF) + a, y = t & 0xFF;
        int @base = t > 0xFF ? 0xD047 : 0xCF47;
        for (int i = 0; i < 3; i++) m.Wr(0x20 + i, Prg.CpuRd(m, @base + y + i));
        Prg.SwitchBank16(m, 0);                                   // CE39
        int t20 = m.Rd(0x20);
        if ((t20 & 0x80) != 0)
        {
            m.Wr(0x11, m.Rd(0x21));
            if (m.Rd(0x21) == 0) yield break;                     // CE47: RTS, no frame wait
            m.Wr(0x10, 0xF0);
            int x = t20, yy = 0;
            (x, yy) = NibbleRow(m, x, yy);
            (x, yy) = NibbleRow(m, x, yy);
            foreach (var s in Pause.PpuQueueBlock(m, m.Rd(0x22))) yield return s;
            if ((m.Rd(0x08) & 1) != 0) { Prg.SetR6(m, 0x1A); Death.PaletteBuildRom(m, 0); yield break; }   // CE61 JMP $8C0E
            for (int i = 0x1F; i > 0; i--) m.Wr(R.PalBuf + i, 0x0F);
            for (int i = 0x0C; i >= 0; i -= 4) for (int j = 1; j <= 3; j++) m.Wr(R.PalBuf + i + j, 0x30);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            yield break;
        }
        if (t20 == 0)
        {
            Prg.SwitchBank16(m, 0);
            if (m.Rd(0x22) != 0) IrqAlt(m);
            m.Wr(0x11, m.Rd(0x21));
            m.Wr(0x10, 0xF0);
            int ptr = (m.Rd(0x21) << 8) | 0xF0;
            for (int i = 0; i < 0x10; i++)
            {
                m.Wr(R.PalBuf + i, Prg.CpuRd(m, ptr + i));
                m.Wr(R.PalBuf + 0x10 + i, Prg.CpuRd(m, ptr + 0x100 + i));
            }
            foreach (var s in Death.WaitFrame(m)) yield return s;
            yield break;
        }
        if (t20 == 1)
        {
            foreach (var s in Death.WaitFrame(m)) yield return s;
            PlaySound.Play(m, 0x43, y);                           // Y = 3p (WaitFrame keeps Y)
            IrqAlt(m);
            for (int i = 0x0F; i >= 0; i--)
            {
                m.Wr(0x6060 + i, 0); m.Wr(0x6070 + i, 0x55); m.Wr(0x6078 + i, 0xAA);
                m.Wr(0x6088 + i, 0xAA); m.Wr(0x6090 + i, 0xFF);
            }
            foreach (var s in Pause.PpuQueueBlock(m, 3)) yield return s;
            foreach (var s in Death.WaitFrame(m)) yield return s;
            yield break;
        }
        if (m.Rd(R.IrqMode) == 2) m.Wr(R.IrqMode, 0);            // CF13
        Prg.SetR6(m, 0x1A);
        Death.PaletteBuildRom(m, 0);
        foreach (var s in Death.WaitFrame(m)) yield return s;
    }

    // js: game/obj/ai_thunder.js thunderStep
    /// <summary>30:CD8A</summary>
    public static IEnumerable<Y> ThunderStep(Machine m, int x)
    {
        SpawnBolt(m, x);
        foreach (var s in Pause.WaitPpuQueue(m)) yield return s;  // CD8F
        for (int i = 0; i < 0xC0; i++) m.Wr(0x6000 + i, 0x7F);    // CE07
        int irq = m.Rd(R.IrqMode);
        foreach (var s in Step(m, m.Rd(Phase + x))) yield return s;
        m.Wr(R.IrqMode, irq);
        int p = (m.Rd(Phase + x) + 1) & 0xFF;
        m.Wr(Phase + x, p);
        if (p == 0x6A)
        {
            m.Wr(Active + x, 0);
            foreach (var s in Death.WaitFrame(m)) yield return s;
            m.Wr(R.IrqMode, 0);
            Prg.SetR6(m, 0x1A);
            Death.PaletteBuildRom(m, 0);
            yield break;
        }
        if (m.Rd(R.IrqMode) != 0) m.Wr(Flags + 1, m.Rd(Flags + 1) | 0x10);   // CDC0 (slot 0's ORA has no STA)
    }
}
