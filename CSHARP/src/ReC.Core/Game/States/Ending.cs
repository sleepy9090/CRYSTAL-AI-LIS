// re-c C# port — main states 4 / 5: the ending, bank 17 (= web/src/game/states/ending.js, s90). REVERSE.md "Ending
// sequence, bank 17" + Verification Log row "Ending (web port)".
//
//   MainState4(m)   30:C944 SwitchBank16(8) -> EndingInit 17:A000 (INC main_state -> 5)
//   MainState5(m)   30:C94C SwitchBank16(8), ReadPadsMenu, EndingTick 17:A100
//
// EndingTick dispatches on $0602 (EndingModeTable 17:A11A: 0 = scene, 1 = EndingFadeIn AA74, 2 = EndingFadeOut AAAD).
// The scene runs the sub-effect $060E (EndingSubTable 17:A140) every 4 frames while it is non-zero, else the step
// obj_phase $0600 (EndingPhaseTable 17:A166, 30 steps). In-step waits are Death.WaitFrame yields. Direct PPU writes
// (NtFill_b17 17:A751) go through m.Ppu.WriteReg like the ROM's $2000-$2007. Steps / sub-effects are keyed by their
// handler address (Dictionary), plain steps return null (handler convention, MainLoop.cs).
using ReC.Gen;

namespace ReC.Game.States;

public static class Ending
{
    private const int Q = 0x6200;             // PPU queue ring (world/ppuqueue.js): hi, lo, len, src; write index $0B
    private const int Oam = 0x0200;
    private const int Tgt = 0x0618;           // $0618-$0637 fade target
    private const int Phase = 0x0600, Mode = 0x0602, WaitLo = 0x0604, WaitHi = 0x0605, Sub = 0x060E, Cnt = 0x0638;
    private const int Cols = 0x0639, ClipY = 0x063D, OamFrom = 0x063E, Lines = 0x063A;

    // js: game/states/ending.js inc
    private static void Inc(Machine m, int a) => m.Wr(a, m.Rd(a) + 1);

    // js: game/states/ending.js dec
    private static int Dec(Machine m, int a) { int v = (m.Rd(a) - 1) & 0xFF; m.Wr(a, v); return v; }

    // js: game/states/ending.js ptr16
    private static int Ptr16(Machine m, int a) => Prg.CpuRd(m, a) | Prg.CpuRd(m, a + 1) << 8;

    // js: game/states/ending.js setPtr
    /// <summary>zp pointer &lt;- the ptr16 variable at `a` (LDA a / STA zp / LDA a+1 / STA zp+1)</summary>
    private static void SetPtr(Machine m, int zp, int a) { m.Wr(zp, Prg.CpuRd(m, a)); m.Wr(zp + 1, Prg.CpuRd(m, a + 1)); }

    // js: game/states/ending.js addPtr
    /// <summary>16-bit zp pointer += n (CLC / ADC / LDA #0 / ADC)</summary>
    private static void AddPtr(Machine m, int zp, int n)
    {
        int v = m.Rd(zp) + (n & 0xFF);
        m.Wr(zp, v & 0xFF);
        m.Wr(zp + 1, m.Rd(zp + 1) + (v >> 8));
    }

    // js: game/states/ending.js nextStep
    private static void NextStep(Machine m) => Inc(m, Phase);

    // ---- bank 17 PPU / queue helpers

    // js: game/states/ending.js nmiOff
    /// <summary>PpuNmiOff_b17 17:A720</summary>
    private static void NmiOff(Machine m) => m.Ppu.WriteReg(0, m.Rd(R.PpuctrlShadow) & 0x7F);

    // js: game/states/ending.js nmiOn
    /// <summary>PpuNmiOn_b17 17:A728</summary>
    private static void NmiOn(Machine m) => m.Ppu.WriteReg(0, m.Rd(R.PpuctrlShadow) | 0x80);

    // js: game/states/ending.js maskOff
    /// <summary>17:A730</summary>
    private static void MaskOff(Machine m) { int v = m.Rd(R.PpumaskShadow) & 0xE1; m.Ppu.WriteReg(1, v); m.Wr(R.PpumaskShadow, v); }

    // js: game/states/ending.js maskOn
    /// <summary>17:A73A</summary>
    private static void MaskOn(Machine m) { int v = m.Rd(R.PpumaskShadow) | 0x1E; m.Ppu.WriteReg(1, v); m.Wr(R.PpumaskShadow, v); }

    // js: game/states/ending.js ntFill
    /// <summary>NtFill_b17 17:A751: PPU hi:00 &lt;- $3C0 x tile, then $40 x attr (NMI + rendering off meanwhile)</summary>
    private static void NtFill(Machine m, int hi, int tile, int attr)
    {
        m.Wr(0x80, tile); m.Wr(0x81, attr);
        NmiOff(m); MaskOff(m);
        m.Ppu.WriteReg(6, hi); m.Ppu.WriteReg(6, 0);
        for (int i = 0; i < 0x3C0; i++) m.Ppu.WriteReg(7, tile);
        m.Wr(0x82, 0);                                        // A773 DEC zp_82 loop ends at 0
        for (int i = 0; i < 0x40; i++) m.Ppu.WriteReg(7, attr);
        MaskOn(m); NmiOn(m);
    }

    // js: game/states/ending.js push
    /// <summary>one queue record at $0B (X = $0B, ... TXA / ADC #4 / AND #$1F / STA $0B)</summary>
    private static void Push(Machine m, int hi, int lo, int len, int src)
    {
        int x = m.Rd(0x0B);
        m.Wr(Q + x, hi & 0xFF); m.Wr(Q + 1 + x, lo & 0xFF); m.Wr(Q + 2 + x, len & 0xFF); m.Wr(Q + 3 + x, src);
        m.Wr(0x0B, (x + 4) & 0x1F);
    }

    // js: game/states/ending.js endingSnd
    /// <summary>EndingSnd 17:A1AB: A -> snd_queue $0103, count $0100 = 1 under snd_queue_lock $0101</summary>
    private static void EndingSnd(Machine m, int a)
    {
        m.Wr(0x0103, a);
        m.Wr(0x0101, 1); m.Wr(0x0100, 1);
        m.Wr(0x0101, 0);
    }

    // ---- palettes

    // js: game/states/ending.js palLoad
    /// <summary>EndingPalLoad 17:AA22: A b7 = sprite half, low nibble = palette n (EndingPalettes 17:AAED) -> target</summary>
    private static void PalLoad(Machine m, int a)
    {
        int x = (a & 0x80) != 0 ? 0x10 : 0;
        int y = (a & 0x0F) << 4;
        for (int i = 0; i < 16; i++) m.Wr(Tgt + x++, Prg.CpuRd(m, 0xAAED + y++));
        m.Wr(0x80, 0);
        int bd = m.Rd(Tgt);
        for (int i = 0; i < 4; i++) m.Wr(Tgt + 0x10 + 4 * i, bd);
    }

    // js: game/states/ending.js palApply
    /// <summary>EndingPalApply 17:AA55</summary>
    private static void PalApply(Machine m) { for (int x = 0x1F; x >= 0; x--) m.Wr(R.PalBuf + x, m.Rd(Tgt + x)); }

    // js: game/states/ending.js fadeInStart
    /// <summary>EndingFadeInStart 17:AA61</summary>
    private static void FadeInStart(Machine m)
    {
        for (int x = 0x1F; x >= 0; x--) m.Wr(R.PalBuf + x, m.Rd(Tgt + x) & 0x0F);
        m.Wr(Mode, 1);
    }

    // js: game/states/ending.js fadeOutStart
    /// <summary>EndingFadeOutStart 17:AAA7</summary>
    private static void FadeOutStart(Machine m) => m.Wr(Mode, 2);

    // js: game/states/ending.js fadeIn
    /// <summary>EndingFadeIn 17:AA74 (mode 1): every 8 frames pal_buf += $10 until it equals the target</summary>
    private static void FadeIn(Machine m)
    {
        if ((m.Rd(0x8C) & 7) != 0) return;
        m.Wr(Cnt, 0);
        for (int x = 0x1F; x >= 0; x--)
        {
            int a = m.Rd(R.PalBuf + x);
            if (a == m.Rd(Tgt + x)) Inc(m, Cnt);
            else m.Wr(R.PalBuf + x, a + 0x10);
        }
        if (m.Rd(Cnt) == 0x20) m.Wr(Mode, 0);
    }

    // js: game/states/ending.js fadeOut
    /// <summary>EndingFadeOut 17:AAAD (mode 2): every 4 frames pal_buf -= $10, floor $0F</summary>
    private static void FadeOut(Machine m)
    {
        if ((m.Rd(0x8C) & 3) != 0) return;
        m.Wr(Cnt, 0);
        for (int x = 0x1F; x >= 0; x--)
        {
            int a = m.Rd(R.PalBuf + x);
            if (a == 0x0F) Inc(m, Cnt);
            else if ((a & 0xF0) == 0) m.Wr(R.PalBuf + x, 0x0F);
            else m.Wr(R.PalBuf + x, a - 0x10);
        }
        if (m.Rd(Cnt) == 0x20) m.Wr(Mode, 0);
    }

    // ---- $060E sub-effects (17:A056-A0EA)

    // js: game/states/ending.js subUp
    /// <summary>17:A07C: Y entries from X: one step up towards the target (low nibble kept), $0638 counts the done ones</summary>
    private static void SubUp(Machine m, int x, int n)
    {
        do
        {
            int t = m.Rd(Tgt + x);
            m.Wr(0x060F, t & 0x0F);
            int a = (m.Rd(R.PalBuf + x) & 0xF0) | m.Rd(0x060F);
            if (a == t) Inc(m, Cnt);
            else a = (a + 0x10) & 0xFF;
            m.Wr(R.PalBuf + x, a);
            x++;
        } while (--n != 0);
    }

    // js: game/states/ending.js subDown
    /// <summary>17:A0CF: Y entries from X: one step down to $0F</summary>
    private static void SubDown(Machine m, int x, int n)
    {
        do
        {
            int a = m.Rd(R.PalBuf + x);
            if (a == 0x0F) Inc(m, Cnt);
            else
            {
                int v = (a - 0x10) & 0xFF;
                if ((v & 0x80) != 0) v = 0x0F;
                m.Wr(R.PalBuf + x, v);
            }
            x++;
        } while (--n != 0);
    }

    // js: game/states/ending.js subDone
    private static void SubDone(Machine m, int n) { if (m.Rd(Cnt) == n) m.Wr(Sub, 0); }

    /// <summary>(ending.js SUBS) sub-effect handler address -> body</summary>
    private static readonly Dictionary<int, Action<Machine>> Subs = new()
    {
        [0xA056] = m => { SubUp(m, 0, 0x20); SubDone(m, 0x20); },                           // 1 fade all in
        [0xA069] = m => { SubUp(m, 9, 3); SubDone(m, 3); },                                 // 2 BG pal 2 in
        [0xA0A2] = m => { SubDown(m, 0, 0x0C); SubDown(m, 0x10, 0x10); SubDone(m, 0x1C); }, // 3 all but BG 3 out
        [0xA0BC] = m => { SubDown(m, 9, 3); SubDone(m, 3); },                               // 4 BG pal 2 out
    };

    // ---- metasprites (EndingSprites 17:B45E)

    // js: game/states/ending.js sprBegin
    /// <summary>clear OAM bytes $063E..$FF to $F0 (every byte, not just y), then $80/$81 = metasprite pointer</summary>
    private static void SprBegin(Machine m, int a)
    {
        int y = (a << 1) & 0xFF;
        int x = m.Rd(OamFrom);
        do { m.Wr(Oam + x, 0xF0); x = (x + 1) & 0xFF; } while (x != 0);
        m.Wr(0x80, Prg.CpuRd(m, 0xB45E + y)); m.Wr(0x81, Prg.CpuRd(m, 0xB45F + y));
    }

    // js: game/states/ending.js sprRel
    /// <summary>EndingSprRel 17:A94B: records attr, dx, dy, tile relative to x $71 / y $70; off-screen or y >= $063D dropped</summary>
    private static void SprRel(Machine m, int a)
    {
        SprBegin(m, a);
        int y = 0, x = m.Rd(OamFrom);
        // js: game/states/ending.js rel
        int Rel(int d, int @base)
        {                                                     // -1 = wrapped off-screen (JMP A9C9)
            if ((d & 0x80) == 0) { int v = d + m.Rd(@base); return v > 0xFF ? -1 : v; }
            m.Wr(0x72, ((d ^ 0xFF) + 1) & 0xFF);
            int w = m.Rd(@base) - m.Rd(0x72);
            return w < 0 ? -1 : w;
        }
        for (; ; )
        {
            m.Wr(0x73, y);
            int attr = Prg.IndY(m, 0x80, y);
            if (attr == 0xFF) return;
            m.Wr(Oam + 2 + x, attr);
            y = (y + 1) & 0xFF;
            bool skip = true;
            int sx = Rel(Prg.IndY(m, 0x80, y), 0x71);
            if (sx >= 0)
            {
                m.Wr(Oam + 3 + x, sx);
                y = (y + 1) & 0xFF;
                int sy = Rel(Prg.IndY(m, 0x80, y), 0x70);
                if (sy >= 0)
                {
                    m.Wr(Oam + x, sy);
                    y = (y + 1) & 0xFF;
                    if (sy < m.Rd(ClipY))
                    {
                        m.Wr(Oam + 1 + x, Prg.IndY(m, 0x80, y));
                        y = (y + 1) & 0xFF;
                        x = (x + 4) & 0xFF;
                        skip = false;
                    }
                }
            }
            if (skip)
            {                                                 // A9C9
                y = (m.Rd(0x73) + 4) & 0xFF;
                m.Wr(Oam + x, 0xF0);
            }
        }
    }

    // js: game/states/ending.js sprAbs
    /// <summary>EndingSprAbs 17:A9D7: records attr, x, y, tile as is</summary>
    private static void SprAbs(Machine m, int a)
    {
        SprBegin(m, a);
        int y = 0, x = m.Rd(OamFrom);
        for (; ; )
        {
            int attr = Prg.IndY(m, 0x80, y);
            if (attr == 0xFF) return;
            m.Wr(Oam + 2 + x, attr);
            m.Wr(Oam + 3 + x, Prg.IndY(m, 0x80, y + 1));
            m.Wr(Oam + x, Prg.IndY(m, 0x80, y + 2));
            m.Wr(Oam + 1 + x, Prg.IndY(m, 0x80, y + 3));
            y = (y + 4) & 0xFF;
            x = (x + 4) & 0xFF;
        }
    }

    // ---- blitters (queue rows, one frame each)

    // js: game/states/ending.js blitHead
    /// <summary>$80 = W, $81 = H from ($88), $88 += 2, $84/$85 = $8A/$8B</summary>
    private static void BlitHead(Machine m)
    {
        m.Wr(0x84, m.Rd(0x8A)); m.Wr(0x85, m.Rd(0x8B));
        m.Wr(0x80, Prg.IndY(m, 0x88, 0)); m.Wr(0x81, Prg.IndY(m, 0x88, 1));
        AddPtr(m, 0x88, 2);
    }

    // js: game/states/ending.js rect
    /// <summary>EndingRect 17:A7A0: H lines of W tiles; rows (+$20) or, with $0639 = $40, columns (+1)</summary>
    private static IEnumerable<Y> Rect(Machine m)
    {
        BlitHead(m);
        do
        {
            int n = m.Rd(0x80), y = 0;
            do { m.Wr(0x6000 + y, Prg.IndY(m, 0x88, y)); y = (y + 1) & 0xFF; } while ((--n & 0xFF) != 0);
            AddPtr(m, 0x88, y);
            NmiOff(m);
            Push(m, m.Rd(0x85) | m.Rd(Cols), m.Rd(0x84), m.Rd(0x80), 0);
            AddPtr(m, 0x84, m.Rd(Cols) != 0 ? 1 : 0x20);
            NmiOn(m);
            foreach (var s in Death.WaitFrame(m)) yield return s;
        } while (Dec(m, 0x81) != 0);
    }

    // js: game/states/ending.js textLines
    /// <summary>EndingTextLines 17:AB1D: H lines of W tiles + 1 address-advance byte</summary>
    private static IEnumerable<Y> TextLines(Machine m)
    {
        BlitHead(m);
        do
        {
            int n = m.Rd(0x80), y = 0;
            do { m.Wr(0x6000 + y, Prg.IndY(m, 0x88, y)); y = (y + 1) & 0xFF; } while ((--n & 0xFF) != 0);
            int adv = Prg.IndY(m, 0x88, y);
            y = (y + 1) & 0xFF;
            AddPtr(m, 0x88, y);
            NmiOff(m);
            Push(m, m.Rd(0x85) | m.Rd(Cols), m.Rd(0x84), m.Rd(0x80), 0);
            AddPtr(m, 0x84, adv);
            NmiOn(m);
            foreach (var s in Death.WaitFrame(m)) yield return s;
        } while (Dec(m, 0x81) != 0);
    }

    // js: game/states/ending.js picture
    /// <summary>EndingPicture 17:A81C: W x H metatiles (tables B004/B0C4/B184/B244), 2 chained rows per frame, then 64
    /// attribute bytes (1 frame), 32 bytes fade target, 2 -> chr_r0/r1 $07F0, 4 -> $07F2-$07F5</summary>
    private static IEnumerable<Y> Picture(Machine m)
    {
        BlitHead(m);
        do
        {
            m.Wr(0x87, m.Rd(0x80));
            m.Wr(0x82, 0); m.Wr(0x83, 0);
            do
            {
                int t = Prg.IndY(m, 0x88, m.Rd(0x82));
                Inc(m, 0x82);
                int yy = m.Rd(0x83);
                m.Wr(0x6000 + yy, Prg.CpuRd(m, 0xB004 + t));
                m.Wr(0x6001 + yy, Prg.CpuRd(m, 0xB0C4 + t));
                m.Wr(0x6040 + yy, Prg.CpuRd(m, 0xB184 + t));
                m.Wr(0x6041 + yy, Prg.CpuRd(m, 0xB244 + t));
                m.Wr(0x83, yy + 2);
            } while (Dec(m, 0x87) != 0);
            AddPtr(m, 0x88, m.Rd(0x80));
            NmiOff(m);
            Push(m, m.Rd(0x85) | 0x80, m.Rd(0x84), m.Rd(0x83), 0x00);
            AddPtr(m, 0x84, 0x20);
            Push(m, m.Rd(0x85), m.Rd(0x84), m.Rd(0x83), 0x40);
            AddPtr(m, 0x84, 0x20);
            NmiOn(m);
            foreach (var s in Death.WaitFrame(m)) yield return s;
        } while (Dec(m, 0x81) != 0);
        int y = 0;
        for (int x = 0; x < 0x40; x++, y++) m.Wr(0x6000 + y, Prg.IndY(m, 0x88, y));
        NmiOff(m);
        Push(m, m.Rd(0x8B) | 0x03, 0xC0, 0x40, 0x00);
        NmiOn(m);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        for (int x = 0; x < 0x20; x++, y++) m.Wr(Tgt + x, Prg.IndY(m, 0x88, y));
        for (int x = 0; x < 2; x++, y++) m.Wr(R.ChrR0Play + x, Prg.IndY(m, 0x88, y));
        for (int x = 0; x < 4; x++, y++) m.Wr(R.ChrR0Play + 2 + x, Prg.IndY(m, 0x88, y));
        AddPtr(m, 0x88, y);
    }

    // ---- steps (EndingPhaseTable 17:A166)

    // js: game/states/ending.js stepWait
    /// <summary>EndingWait 17:A0EB: 60 frames x ($0605 + 1), then the next step</summary>
    private static IEnumerable<Y>? StepWait(Machine m)
    {
        if ((Dec(m, WaitLo) & 0x80) == 0) return null;
        m.Wr(WaitLo, 0x3B);
        if ((Dec(m, WaitHi) & 0x80) == 0) return null;
        NextStep(m);
        return null;
    }

    // js: game/states/ending.js setWait
    private static void SetWait(Machine m, int lo, int hi) { m.Wr(WaitLo, lo); m.Wr(WaitHi, hi); }

    // js: game/states/ending.js sndStep
    private static Func<Machine, IEnumerable<Y>?> SndStep(int id) => m => { EndingSnd(m, id); NextStep(m); return null; };

    // js: game/states/ending.js fadeOutStep
    private static IEnumerable<Y>? FadeOutStep(Machine m) { FadeOutStart(m); NextStep(m); return null; }

    // js: game/states/ending.js step0
    /// <summary>step 0 EndingStep0Setup 17:A1BC</summary>
    private static IEnumerable<Y> Step0(Machine m)
    {
        m.Wr(0x8A, 0x00); m.Wr(0x8B, 0x20);
        SetPtr(m, 0x88, 0xABD9);
        foreach (var s in Picture(m)) yield return s;
        SetPtr(m, 0x88, 0xB854);
        m.Wr(0x78, 0xC0); m.Wr(0x79, 0x20);
        m.Wr(0x7A, 0x00); m.Wr(0x7B, 0x28);
        m.Wr(Lines, 0);
        m.Wr(0x70, 0xF0); m.Wr(0x71, 0x5C);
        SetWait(m, 0, 1);
        FadeInStart(m);
        NextStep(m);
    }

    // js: game/states/ending.js rowAdvance
    /// <summary>17:A26B / A2BA: row pointer at zp += $20; $x3C0 of the 1st / 2nd nametable wraps to the other one</summary>
    private static void RowAdvance(Machine m, int zp)
    {
        AddPtr(m, zp, 0x20);
        if (m.Rd(zp) == 0xC0 && (m.Rd(zp + 1) & 3) == 3)
        {
            m.Wr(zp, 0);
            m.Wr(zp + 1, (m.Rd(zp + 1) & 0x28) ^ 0x08);
        }
    }

    // js: game/states/ending.js step3
    /// <summary>step 3 EndingStep3Scroll 17:A212: one tile-column row (+ a blank row 30 lines below) per frame</summary>
    private static IEnumerable<Y> Step3(Machine m)
    {
        for (; ; )
        {
            int col = Prg.IndY(m, 0x88, 0);
            if (col != 0) break;
            // A337: end of the list -> rewind; with C = 0 here the test is $063A - $2D >= 0 (so 64 rows in all)
            SetPtr(m, 0x88, 0xB854);
            if ((((m.Rd(Lines) - 0x2D) & 0xFF) & 0x80) != 0) continue;   // JMP EndingStep3Scroll
            m.Wr(0x8D, 0x3C);
            FadeOutStart(m);
            m.Wr(Mode, 0);
            SetWait(m, 0, 1);
            m.Wr(0x7F, 8);
            do
            {                                                 // A363: flashing palette, 40 frames
                PalLoad(m, 1); PalApply(m);
                foreach (var s in Death.WaitFrame(m)) yield return s;
                foreach (var s in Death.WaitFrame(m)) yield return s;
                PalLoad(m, 2); PalApply(m);
                foreach (var s in Death.WaitFrame(m)) yield return s;
                foreach (var s in Death.WaitFrame(m)) yield return s;
                foreach (var s in Death.WaitFrame(m)) yield return s;
            } while (Dec(m, 0x7F) != 0);
            EndingSnd(m, 0x5B);
            NextStep(m);
            yield break;
        }
        m.Wr(0x7C, Prg.IndY(m, 0x88, 0));
        int y = 1, x = 0;
        for (int a; (a = Prg.IndY(m, 0x88, y)) != 0; y++, x++) m.Wr(0x6000 + x, a);
        y++;
        AddPtr(m, 0x88, y);
        NmiOff(m);
        Push(m, m.Rd(0x79) | 0x80, m.Rd(0x78) + m.Rd(0x7C), x, 0x00);
        RowAdvance(m, 0x78);
        for (int i = 0x1F; i >= 0; i--) m.Wr(0x6020 + i, 0xFF);
        Push(m, m.Rd(0x7B) | 0x80, m.Rd(0x7A), 0x20, 0x20);
        RowAdvance(m, 0x7A);
        if (m.Rd(Lines) == 0x18)
        {                                                     // A2D9: attribute row $23C0 = $55
            for (int i = 0x0F; i >= 0; i--) m.Wr(0x6040 + i, 0x55);
            Push(m, 0x23, 0xC0, 0x10, 0x40);
        }
        NmiOn(m);
        int n = m.Rd(Lines);
        Inc(m, Lines);
        if (n >= 0x14)
        {                                                     // A30E: scroll down 10 lines per row
            int v = (m.Rd(R.CamYLo) + 0x0A) & 0xFF;
            m.Wr(R.CamYLo, v);
            if (v == 0xF0) { m.Wr(R.CamYLo, 0); m.Wr(R.CamYHi, m.Rd(R.CamYHi) ^ 1); }
        }
        if (m.Rd(Lines) < 0x20) yield break;
        m.Wr(0x70, m.Rd(0x70) - 4);
        SprRel(m, 8);
    }

    // js: game/states/ending.js step5
    /// <summary>step 5 EndingStep5Sink 17:A38F: hero sprite 8 down 1 px / 4 frames; after $8D runs out, fade out</summary>
    private static IEnumerable<Y>? Step5(Machine m)
    {
        SprRel(m, 8);
        if ((m.Rd(0x8C) & 3) == 0) Inc(m, 0x70);
        if ((Dec(m, 0x8D) & 0x80) == 0) return null;
        if ((m.Rd(0x8C) & 7) != 0) return null;
        m.Wr(Mode, 2);
        FadeOut(m);
        bool done = m.Rd(Mode) == 0;
        m.Wr(Mode, 0);
        if (!done) return null;
        SetWait(m, 0, 1);
        NextStep(m);
        return null;
    }

    // js: game/states/ending.js step7
    /// <summary>step 7 EndingStep7Epilogue 17:A3CB: text page 1 + the player name centred on rows $2208 / $2228</summary>
    private static IEnumerable<Y> Step7(Machine m)
    {
        m.Wr(R.ChrR0Play, 0x3C);
        m.Wr(R.CamYLo, 0); m.Wr(R.CamYHi, 0);
        NtFill(m, 0x20, 0x10, 0x00);
        Title.OamHideAll(m);
        Title.PalBlack(m);
        m.Wr(0x0619, 0x30);
        SetPtr(m, 0x88, 0xBE37);
        m.Wr(0x8A, 0xC4); m.Wr(0x8B, 0x20);
        foreach (var s in TextLines(m)) yield return s;
        int len = m.Rd(0x6420);
        m.Wr(0x7E, (6 - len) & 0xFF);
        int x = 0, y = len;
        do
        {
            m.Wr(0x6000 + x, m.Rd(0x6410 + x));
            m.Wr(0x6020 + x, m.Rd(0x6418 + x));
            x = (x + 1) & 0xFF;
            y = (y - 1) & 0xFF;
        } while (y != 0);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        NmiOff(m);
        Push(m, 0xA2, 0x08 + m.Rd(0x7E), len, 0x00);
        Push(m, 0x22, 0x28 + m.Rd(0x7E), len, 0x20);
        NmiOn(m);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        FadeInStart(m);
        SetWait(m, 2, 0x0C);
        NextStep(m);
    }

    // js: game/states/ending.js step11
    /// <summary>step 11 EndingStep11Page2 17:A483</summary>
    private static IEnumerable<Y> Step11(Machine m)
    {
        NtFill(m, 0x20, 0x10, 0x00);
        m.Wr(0x8A, 0xC4); m.Wr(0x8B, 0x20);
        foreach (var s in TextLines(m)) yield return s;
        FadeInStart(m);
        SetWait(m, 0, 5);
        NextStep(m);
    }

    // js: game/states/ending.js step14
    /// <summary>step 14 EndingStep14StaffCard 17:A4AF: "STAFF" ($A4E3) at $21CE</summary>
    private static IEnumerable<Y> Step14(Machine m)
    {
        m.Wr(R.ChrR0Play, 0x1C);
        NtFill(m, 0x20, 0x20, 0x00);
        SetPtr(m, 0x88, 0xA4E3);
        m.Wr(0x8A, 0xCE); m.Wr(0x8B, 0x21);
        foreach (var s in Rect(m)) yield return s;
        FadeInStart(m);
        SetWait(m, 0, 2);
        NextStep(m);
    }

    // js: game/states/ending.js step18
    /// <summary>step 18 EndingStep18RollFrame 17:A4F3: border box (column + row blocks of ($AB95)), blank card area</summary>
    private static IEnumerable<Y> Step18(Machine m)
    {
        NtFill(m, 0x20, 0x2A, 0xFF);
        SetPtr(m, 0x88, 0xAB95);
        m.Wr(0x8A, 0x03); m.Wr(0x8B, 0x20); m.Wr(Cols, 0x40);
        foreach (var s in Rect(m)) yield return s;
        m.Wr(0x8A, 0x20); m.Wr(0x8B, 0x22); m.Wr(Cols, 0x00);
        foreach (var s in Rect(m)) yield return s;
        SetPtr(m, 0x88, 0xAB95);
        m.Wr(0x8A, 0x0E); m.Wr(0x8B, 0x20); m.Wr(Cols, 0x40);
        foreach (var s in Rect(m)) yield return s;
        m.Wr(0x8A, 0x40); m.Wr(0x8B, 0x23); m.Wr(Cols, 0x00);
        foreach (var s in Rect(m)) yield return s;
        SetPtr(m, 0x88, 0xA57A);
        m.Wr(0x8A, 0x40); m.Wr(0x8B, 0x22);
        foreach (var s in Rect(m)) yield return s;
        SetPtr(m, 0x88, 0xAC83);
        m.Wr(0x8D, 0);
        Title.OamHideAll(m);
        NextStep(m);
    }

    // js: game/states/ending.js step19
    /// <summary>step 19 EndingScript 17:A597 on EndingScriptBytes 17:A65E[$8D]</summary>
    private static IEnumerable<Y> Step19(Machine m)
    {
        // js: game/states/ending.js next
        int Next() { int y = m.Rd(0x8D); Inc(m, 0x8D); return Prg.CpuRd(m, 0xA65E + y); }
        for (; ; )
        {
            int b = Next();
            if ((b & 0x80) != 0) { FadeOutStart(m); m.Wr(Phase, 0x16); yield break; }
            if (b != 0)
            {
                int y = (b << 1) & 0xFF;
                int s88 = m.Rd(0x88), s89 = m.Rd(0x89);       // PHA x2 (stack bytes not modelled)
                m.Wr(0x88, Prg.CpuRd(m, 0xB8D2 + y)); m.Wr(0x89, Prg.CpuRd(m, 0xB8D3 + y));
                m.Wr(0x8A, 0x50); m.Wr(0x8B, 0x22);
                foreach (var s in Rect(m)) yield return s;
                m.Wr(0x89, s89); m.Wr(0x88, s88);
                int i = m.Rd(0x8D);
                m.Wr(Sub, Prg.CpuRd(m, 0xA65E + i));
                m.Wr(WaitHi, Prg.CpuRd(m, 0xA65E + ((i + 1) & 0xFF)));
                m.Wr(WaitLo, Prg.CpuRd(m, 0xA65E + ((i + 2) & 0xFF)));
                m.Wr(0x8D, (i + 3) & 0xFF);
                NextStep(m);
                yield break;
            }
            m.Wr(0x8A, 0x04); m.Wr(0x8B, 0x20);               // 0: next portrait of the ($88) stream + its metasprite
            foreach (var s in Picture(m)) yield return s;
            SprAbs(m, Next());
        }
    }

    // js: game/states/ending.js step21
    /// <summary>step 21 EndingScriptNext 17:A603</summary>
    private static IEnumerable<Y>? Step21(Machine m)
    {
        int y = m.Rd(0x8D);
        Inc(m, 0x8D);
        m.Wr(Sub, Prg.CpuRd(m, 0xA65E + y));
        m.Wr(Phase, 0x13);
        return null;
    }

    // js: game/states/ending.js step22
    /// <summary>step 22 EndingStep22Thanks 17:A613: ($BD78) at $2128</summary>
    private static IEnumerable<Y> Step22(Machine m)
    {
        Title.PalBlack(m);
        for (int x = 0x1F; x >= 0; x--) m.Wr(Tgt + x, 0x0F);
        Title.OamHideAll(m);
        NtFill(m, 0x20, 0x20, 0x00);
        m.Wr(0x0619, 0x20);
        m.Wr(0x8A, 0x28); m.Wr(0x8B, 0x21);
        SetPtr(m, 0x88, 0xBD78);
        foreach (var s in Rect(m)) yield return s;
        FadeInStart(m);
        SetWait(m, 0x14, 5);
        NextStep(m);
    }

    // js: game/states/ending.js step25
    /// <summary>step 25 EndingStep25Final 17:A696: final picture ($B304), metasprites 5 / 6 (abs), 9 (rel, clip y $78)</summary>
    private static IEnumerable<Y> Step25(Machine m)
    {
        SetPtr(m, 0x88, 0xB304);
        m.Wr(0x8A, 0x00); m.Wr(0x8B, 0x20);
        foreach (var s in Picture(m)) yield return s;
        SprAbs(m, 5);
        m.Wr(OamFrom, 0x8C);
        SprAbs(m, 6);
        m.Wr(OamFrom, 0xAC);
        m.Wr(0x71, 0x74); m.Wr(0x70, 0x14);
        m.Wr(ClipY, 0x78);
        SprRel(m, 9);
        FadeInStart(m);
        NextStep(m);
    }

    // js: game/states/ending.js step26
    /// <summary>step 26 EndingStep26Sink 17:A6D8</summary>
    private static IEnumerable<Y>? Step26(Machine m)
    {
        SprRel(m, 9);
        if ((m.Rd(0x8C) & 3) == 0) Inc(m, 0x70);
        if (m.Rd(0x70) < 0x8C) return null;
        SetWait(m, 0x28, 2);
        NextStep(m);
        return null;
    }

    // js: game/states/ending.js step28
    /// <summary>step 28 EndingStep28Last 17:A6FB: target $0635-$0637 = 20 24 04, applied at once; metasprite 7</summary>
    private static IEnumerable<Y>? Step28(Machine m)
    {
        m.Wr(0x0635, 0x20); m.Wr(0x0636, 0x24); m.Wr(0x0637, 0x04);
        PalApply(m);
        m.Wr(OamFrom, 0x8C);
        SprAbs(m, 7);
        NextStep(m);
        return null;
    }

    /// <summary>(ending.js STEPS) step handler address -> step</summary>
    private static readonly Dictionary<int, Func<Machine, IEnumerable<Y>?>> Steps = new()
    {
        [0xA1BC] = Step0, [0xA0EB] = StepWait, [0xA209] = SndStep(0x4A), [0xA212] = Step3, [0xA38F] = Step5,
        [0xA3CB] = Step7, [0xA47A] = SndStep(0x1D), [0xA4A8] = FadeOutStep, [0xA483] = Step11, [0xA4AF] = Step14,
        [0xA58E] = SndStep(0x1C), [0xA4EC] = FadeOutStep, [0xA4F3] = Step18, [0xA597] = Step19, [0xA603] = Step21,
        [0xA613] = Step22, [0xA657] = FadeOutStep, [0xA696] = Step25, [0xA6D8] = Step26, [0xA6FB] = Step28,
        [0xA71B] = m => null,                                 // step 29: bare RTS, forever
    };

    // js: game/states/ending.js jmp
    /// <summary>JMP ($008E) dispatch: $8E/$8F &lt;- table word (kept for RAM fidelity)</summary>
    private static int Jmp(Machine m, int tbl, int i)
    {
        m.Wr(0x8E, Prg.CpuRd(m, tbl + 2 * i)); m.Wr(0x8F, Prg.CpuRd(m, tbl + 2 * i + 1));
        return Ptr16(m, tbl + 2 * i);
    }

    // js: game/states/ending.js call
    /// <summary>run fn: plain function (one frame) or iterator (spans frames)</summary>
    private static IEnumerable<Y> Call(Func<Machine, IEnumerable<Y>?> fn, Machine m)
    {
        var r = fn(m);
        if (r != null) foreach (var s in r) yield return s;
    }

    // js: game/states/ending.js scene
    /// <summary>scene 17:A14A: sub-effect 17:A120 every 4 frames while $060E != 0, else step obj_phase</summary>
    private static IEnumerable<Y> Scene(Machine m)
    {
        if (m.Rd(Sub) != 0)
        {
            if ((m.Rd(0x8C) & 3) != 0) yield break;
            m.Wr(Cnt, 0);
            int a = Jmp(m, 0xA140, m.Rd(Sub));
            if (!Subs.TryGetValue(a, out var fn)) throw new InvalidOperationException($"ending sub-effect {m.Rd(Sub)} -> {a:x}");
            fn(m);
            yield break;
        }
        int b = Jmp(m, 0xA166, m.Rd(Phase));
        if (!Steps.TryGetValue(b, out var st)) throw new InvalidOperationException($"ending step {m.Rd(Phase)} -> {b:x}");
        foreach (var s in Call(st, m)) yield return s;
    }

    // js: game/states/ending.js endingTick
    /// <summary>EndingTick 17:A100</summary>
    private static IEnumerable<Y> EndingTick(Machine m)
    {
        m.Wr(R.FrameReady, 0);
        Dec(m, 0x8C);
        int mode = m.Rd(Mode);
        Jmp(m, 0xA11A, mode);
        if (mode == 0) { foreach (var s in Scene(m)) yield return s; }
        else if (mode == 1) FadeIn(m);
        else if (mode == 2) FadeOut(m);
        else throw new InvalidOperationException($"ending mode {mode}");
    }

    // ---- main states

    // js: game/states/ending.js mainState4
    /// <summary>30:C944 + EndingInit 17:A000</summary>
    public static IEnumerable<Y> MainState4(Machine m)
    {
        Prg.SwitchBank16(m, 8);
        EndingSnd(m, 0);                                      // EndingSilence 17:A1A2
        Title.PalBlack(m);
        foreach (var s in Death.WaitFrame(m)) yield return s;
        NtFill(m, 0x20, 0xFF, 0x55);
        NtFill(m, 0x28, 0xFF, 0x55);
        Title.OamHideAll(m);
        m.Wr(R.IrqMode, 3);
        m.Wr(R.FrameReady, 0);
        m.Wr(R.ScrollShadow0, 0); m.Wr(R.CamYLo, 0); m.Wr(R.CamXHi, 0); m.Wr(R.CamYHi, 0);
        foreach (int a in new[] { Sub, Phase, 0x0601, Mode, Cols }) m.Wr(a, 0);
        PalLoad(m, 0x00);
        PalLoad(m, 0x80);
        m.Wr(ClipY, 0xF0);
        m.Wr(OamFrom, 0x04);
        Inc(m, R.MainState);
        m.Wr(0x700E, 0xFF);
    }

    // js: game/states/ending.js mainState5
    /// <summary>30:C94C + EndingTick 17:A100</summary>
    public static IEnumerable<Y> MainState5(Machine m)
    {
        Prg.SwitchBank16(m, 8);
        Pads.ReadPadsMenu(m);
        foreach (var s in EndingTick(m)) yield return s;
    }
}
