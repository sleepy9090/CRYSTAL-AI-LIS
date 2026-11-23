// re-c C# port — sound driver, bank 24 (= web/src/game/sound/driver.js, Ph7 web 7/8, s56). Line-by-line port of
// SndInit 24:8020, SndCommand 24:8048, SndTick 24:8186, SndChannelTick 24:8276, SndReadOp 24:8347 + the 16
// SndOpcodeTable handlers, SndEnvelopeTick 24:84AF, SndVibratoTick 24:86AA, SndFadeStep 24:8609 (REVERSE.md
// "Sound driver", "Sound data format"). All state is the ROM's RAM ($0100-$017F, zp $F0-$FF); all tables and stream
// bytes are read from PRG through Prg.CpuRd with the mapping the driver sets (R6 = 24, R7 = 25, or 12 for SFX).
// APU access goes through m.ApuWrite(addr, v) / m.ApuRead(addr).
//
// Checked by tests/ReC.Tests (SoundFrames) against ../web/test/sound_frames/*.json (tools/emu/sound_capture.py):
// every APU write and every RAM byte, per call, like ../web/test/sound_check.mjs.
namespace ReC.Game.Sound;

public static class Driver
{
    // RAM fields (6-wide arrays, X = channel 0-5; flow ops use X+6 for the loop-2 slots)
    private const int FLAGS = 0x0114, PTR_LO = 0x011A, PTR_HI = 0x0120, L1_LO = 0x0126, L1_HI = 0x0132, L1_CNT = 0x013E;
    private const int WAIT = 0x014A, LEN = 0x0150, OCT = 0x0156, ENV = 0x015C, VIB = 0x0162, REG0 = 0x0168, SWEEP = 0x016E;
    private const int PER_LO = 0x0174, PER_HI = 0x017A;
    private const int L2_LO = 0x012C, L2_HI = 0x0138, L2_CNT = 0x0144;
    private const int MUSIC_ID = 0x0111, SFX_PRI = 0x0112, NEXT_MUSIC = 0x0113, CMD = 0x0110;
    // zero page
    private const int F = 0xF0, OFS = 0xF1, FEND = 0xF2, OP = 0xF3, PTR = 0xF5, TMP = 0xF7, SAVEX = 0xFA, CHN = 0xFB,
        DIRTY = 0xFC, OPCNT = 0xFD, FADE = 0xFE, DRUM = 0xFF;
    // ROM tables (bank 24, $8000 window)
    private const int OPCODE_TABLE = 0x8000, CHN_TYPE = 0x87A6, CHN_REG = 0x87AC, CHN_MASK = 0x87B2, PERIOD_LO = 0x87B8,
        PERIOD_HI = 0x8838, ENV_TONAL = 0x88B8, ENV_NOISE = 0x89B8, DURATION = 0x8AB8, VOLUME = 0x8AD8, DMC = 0x8BD8,
        MUSIC_TABLE = 0x8C0C, DETUNE = 0x83DA, OCTAVE = 0x842D, VIB_STEP = 0x8714, VIB_OFS = 0x8724;

    // js: game/sound/driver.js rom
    private static int Rom(Machine m, int a) => Prg.CpuRd(m, a & 0xFFFF);
    // js: game/sound/driver.js ptrY
    private static int PtrY(Machine m, int y) => Prg.CpuRd(m, (m.Rd(PTR) | m.Rd(PTR + 1) << 8) + (y & 0xFF));
    // js: game/sound/driver.js or
    private static void Or(Machine m, int a, int v) => m.Wr(a, m.Rd(a) | v);
    // js: game/sound/driver.js and
    private static void And(Machine m, int a, int v) => m.Wr(a, m.Rd(a) & v);
    // js: game/sound/driver.js inc
    private static void Inc(Machine m, int a) => m.Wr(a, (m.Rd(a) + 1) & 0xFF);
    // js: game/sound/driver.js dec
    private static void Dec(Machine m, int a) => m.Wr(a, (m.Rd(a) - 1) & 0xFF);
    // js: game/sound/driver.js type
    private static int Type(Machine m, int x) => Rom(m, CHN_TYPE + x);
    // js: game/sound/driver.js regOf
    private static int RegOf(Machine m, int x) => Rom(m, CHN_REG + x);

    // js: game/sound/driver.js sndInit
    /// <summary>SndInit 24:8020</summary>
    public static void SndInit(Machine m)
    {
        m.Wr(F, 1);
        m.Wr(MUSIC_ID, 0xFF); m.Wr(SFX_PRI, 0xFF);
        for (int x = 0; x < 6; x++) m.Wr(FLAGS + x, 0);
        m.Wr(CHN, 0); m.ApuWrite(0x4015, 0);
        m.Wr(F, 0);
    }

    // js: game/sound/driver.js channelOff
    /// <summary>SndChannelOff 24:8465</summary>
    private static void ChannelOff(Machine m, int x)
    {
        if ((m.Rd(FLAGS + x) & 0x80) != 0) return;
        int save = m.Rd(DRUM);
        m.Wr(DRUM, save & 0x10);
        m.ApuWrite(0x4015, Rom(m, CHN_MASK + x) | m.Rd(DRUM));
        m.Wr(DRUM, save);
    }

    // js: game/sound/driver.js unsteal
    /// <summary>24:8388: give a stolen channel back (b7 -> b4 rest)</summary>
    private static void Unsteal(Machine m, int x)
    {
        int f = m.Rd(FLAGS + x);
        if ((f & 0x80) != 0) m.Wr(FLAGS + x, (f & 0x7F) | 0x10);
    }

    // js: game/sound/driver.js initChannel
    /// <summary>SndInitChannel 24:8132 (A = stream ptr hi); returns X+1</summary>
    private static int InitChannel(Machine m, int x, int hi)
    {
        m.Wr(PTR_HI + x, hi);
        m.Wr(WAIT + x, 1); m.Wr(LEN + x, 1);
        m.Wr(OCT + x, 0);
        int y = RegOf(m, x);
        m.ApuWrite(0x4000 + y, 0); m.ApuWrite(0x4002 + y, 0); m.ApuWrite(0x4003 + y, 0);
        m.Wr(REG0 + x, 0); m.Wr(PER_LO + x, 0); m.Wr(PER_HI + x, 0);
        m.ApuWrite(0x4001 + y, 8); m.Wr(SWEEP + x, 8);
        m.Wr(FLAGS + x, 1);
        ChannelOff(m, x);
        return x + 1;
    }

    // js: game/sound/driver.js musicHeader
    /// <summary>SndGetMusicHeader 24:8177: $F5/$F6 = SndMusicTable[id]; true if null</summary>
    private static bool MusicHeader(Machine m, int id)
    {
        int i = (id << 1) & 0xFF;
        m.Wr(PTR, Rom(m, MUSIC_TABLE + i)); m.Wr(PTR + 1, Rom(m, MUSIC_TABLE + 1 + i));
        return (m.Rd(PTR) | m.Rd(PTR + 1)) == 0;
    }

    // js: game/sound/driver.js startSfx
    /// <summary>SndStartSfx 24:8094 (A = command)</summary>
    private static void StartSfx(Machine m, int a)
    {
        m.PrgR7 = 0x0C;                                          // $8000 = 7, $8001 = 12 (no shadow update)
        int i = (a << 1) & 0xFF;
        m.Wr(PTR, Rom(m, 0x9FC0 + i)); m.Wr(PTR + 1, Rom(m, 0x9FC1 + i));
        if ((m.Rd(PTR) | m.Rd(PTR + 1)) == 0) return;
        if ((m.Rd(F) & 0x40) != 0) return;
        int pri = PtrY(m, 0);
        if (pri != m.Rd(SFX_PRI) && pri > m.Rd(SFX_PRI)) return;
        m.Wr(SFX_PRI, pri);
        m.Wr(CHN, 0x0A);
        int x = 4;
        foreach (int y in new[] { 1, 3 })
        {                                                        // SndInitSfxChannel 24:8169
            m.Wr(PTR_LO + x, PtrY(m, y));
            x = InitChannel(m, x, (PtrY(m, y + 1) + 0x20) & 0xFF);
        }
        Or(m, FLAGS + 1, 0x80); Or(m, FLAGS + 3, 0x80);
    }

    // js: game/sound/driver.js startMusic
    /// <summary>SndStartMusic 24:80E4 (A = command &lt; $20)</summary>
    private static void StartMusic(Machine m, int a)
    {
        if (MusicHeader(m, a)) return;
        if ((m.Rd(F) & 0x80) != 0) return;
        m.Wr(FADE, 0); m.Wr(DRUM, 0);                            // 24:8103
        m.Wr(MUSIC_ID, m.Rd(CMD) & 0x7F);
        And(m, F, 0xDF);
        int x = 0;
        foreach (int y in new[] { 1, 3, 5, 7 })
        {                                                        // 24:812A
            m.Wr(PTR_LO + x, PtrY(m, y));
            x = InitChannel(m, x, PtrY(m, y + 1));
        }
        int c = m.Rd(CHN);
        if ((c & 0x02) != 0) m.Wr(FLAGS + 1, 0x81);
        if ((c & 0x08) != 0) m.Wr(FLAGS + 3, 0x81);
    }

    // js: game/sound/driver.js sndCommand
    /// <summary>SndCommand 24:8048: command in $0110</summary>
    public static void SndCommand(Machine m)
    {
        Or(m, F, 0x01);
        int cmd = m.Rd(CMD);                                     // 24:8058
        if (cmd == 0xFF) m.Wr(FADE, 0xBC);
        else if ((cmd & 0x80) != 0)
        {                                                        // 24:8071 pause toggle
            if ((m.Rd(MUSIC_ID) & 0x80) == 0)
            {
                m.ApuWrite(0x4015, m.Rd(CHN));
                m.Wr(F, m.Rd(F) ^ 0x20);
                if ((m.Rd(F) & 0x20) == 0)
                {
                    for (int x = 0; x < 4; x++) { ChannelOff(m, x); Unsteal(m, x); }
                }
            }
        }
        else if (cmd >= 0x20) StartSfx(m, cmd);
        else StartMusic(m, cmd);
        And(m, F, 0xFE);
    }

    // ---- stream ops (SndOpcodeTable 24:8000); each gets x and returns nothing; X is restored by the caller ----

    // js: game/sound/driver.js opEnd
    /// <summary>24:8364 $0x/$1x</summary>
    private static void OpEnd(Machine m, int x)
    {
        Inc(m, FEND);
        ChannelOff(m, x);
        And(m, FLAGS + x, 0xFE);
        if (x < 4) { m.Wr(NEXT_MUSIC, m.Rd(OP) & 0x1F); return; }
        m.Wr(CHN, Rom(m, CHN_MASK + x) & m.Rd(CHN));
        Unsteal(m, Type(m, x));
    }

    // js: game/sound/driver.js sweepOp
    /// <summary>24:8395 / 83AE</summary>
    private static void SweepOp(Machine m, int x, int off)
    {
        Inc(m, FEND);
        Or(m, DIRTY, 0x02);
        int y = m.Rd(OFS); Inc(m, OFS);
        int b = PtrY(m, y);
        m.Wr(SWEEP + x, off != 0 ? 0 : b | 0x80);
    }

    // js: game/sound/driver.js opSweep
    /// <summary>24:8395 $Cx</summary>
    private static void OpSweep(Machine m, int x) => SweepOp(m, x, m.Rd(OP) & 1);

    // js: game/sound/driver.js notePeriod
    /// <summary>24:83F8: period for note $F3 (+ $F7 to lo); returns the hi byte</summary>
    private static int NotePeriod(Machine m, int x)
    {
        int a = m.Rd(OP);
        if (Type(m, x) >= 3)
        {
            m.Wr(PER_LO + x, a & 0x0F);
            DrumTrigger(m, x);
            return 0;
        }
        int y = (a & 0x0F) | m.Rd(OCT + x);
        int lo = Rom(m, PERIOD_LO + y) + m.Rd(TMP);
        m.Wr(PER_LO + x, lo & 0xFF);
        return (Rom(m, PERIOD_HI + y) + (lo >> 8)) & 0xFF;
    }

    // js: game/sound/driver.js noteWith
    /// <summary>24:83E7</summary>
    private static void NoteWith(Machine m, int x, int tmp)
    {
        m.Wr(TMP, tmp);
        Inc(m, FEND);
        m.Wr(PER_HI + x, NotePeriod(m, x));
        Or(m, DIRTY, 0x0C);
    }

    // js: game/sound/driver.js opNote
    /// <summary>24:83E5 $2x</summary>
    private static void OpNote(Machine m, int x) => NoteWith(m, x, 0);

    // js: game/sound/driver.js opNoteSweep
    /// <summary>24:83E2 $Bx: JSR $83B6 enters after the FEND/dirty updates, so only the sweep byte is read here</summary>
    private static void OpNoteSweep(Machine m, int x)
    {
        int y = m.Rd(OFS); Inc(m, OFS);
        m.Wr(SWEEP + x, PtrY(m, y) | 0x80);
        NoteWith(m, x, 0);
    }

    // js: game/sound/driver.js opNoteDetune
    /// <summary>24:83C2 $Dx</summary>
    private static void OpNoteDetune(Machine m, int x)
    {
        if (Type(m, x) == 3) { DrumSelect(m, x); return; }
        NoteWith(m, x, Rom(m, DETUNE + (m.Rd(OCT + x) >> 4)));
    }

    // js: game/sound/driver.js opOctave
    /// <summary>24:8422 $3x (falls into the RTS at $842D)</summary>
    private static void OpOctave(Machine m, int x) => m.Wr(OCT + x, Rom(m, OCTAVE + (m.Rd(OP) & 0x0F)));

    // js: game/sound/driver.js opRest
    /// <summary>24:8436 $4x</summary>
    private static void OpRest(Machine m, int x)
    {
        Inc(m, FEND);
        Or(m, FLAGS + x, 0x10);
        int v = m.Rd(OP) & 0x0F;
        if (v == 0) { ChannelOff(m, x); return; }
        m.Wr(OP, v);
        if ((x & 4) == 0 && (x & 1) != 0 && m.Rd(CHN) != 0) return;
        m.ApuWrite(0x4000 + RegOf(m, x), (m.Rd(REG0 + x) & 0xF0) | v);
    }

    // js: game/sound/driver.js opDuration
    /// <summary>24:847D $5x/$6x</summary>
    private static void OpDuration(Machine m, int x) => m.Wr(LEN + x, Rom(m, DURATION + ((m.Rd(OP) - 0x50) & 0xFF)));

    // js: game/sound/driver.js opLegato
    /// <summary>24:848A $7x</summary>
    private static void OpLegato(Machine m, int x)
    {
        m.Wr(TMP, 0);
        Inc(m, FEND);
        int v = m.Rd(OP) & 0x0F;
        m.Wr(OP, v);
        if (v == 7) return;
        int hi = NotePeriod(m, x);
        bool same = hi == m.Rd(PER_HI + x);
        m.Wr(PER_HI + x, hi);
        Or(m, DIRTY, same ? 0x04 : 0x0C);
    }

    // js: game/sound/driver.js opVolume
    /// <summary>24:8532 $8x</summary>
    private static void OpVolume(Machine m, int x)
    {
        if (m.Rd(FADE) != 0) return;
        int v = m.Rd(OP) & 0x0F;
        m.Wr(OP, v);
        m.Wr(REG0 + x, (m.Rd(REG0 + x) & 0xF0) | v);
        Or(m, DIRTY, 0x01);
    }

    // js: game/sound/driver.js opDuty
    /// <summary>24:854D $9x</summary>
    private static void OpDuty(Machine m, int x)
    {
        int a = (m.Rd(OP) << 4) & 0xFF;
        if (Type(m, x) != 2)
        {
            int s = a & 0x10;
            m.Wr(OP, s);
            if (s == 0) And(m, FLAGS + x, 0xBF);
            a = ((a << 1) & 0xC0) | s | 0x20;
        }
        m.Wr(OP, a);
        m.Wr(REG0 + x, (m.Rd(REG0 + x) & 0x0F) | a);
        Or(m, DIRTY, 0x01);
    }

    // js: game/sound/driver.js loopEnd
    /// <summary>24:859B (x or x+6)</summary>
    private static void LoopEnd(Machine m, int x)
    {
        Dec(m, L1_CNT + x);
        if (m.Rd(L1_CNT + x) == 0) return;
        m.Wr(PTR, m.Rd(L1_LO + x)); m.Wr(PTR + 1, m.Rd(L1_HI + x));
        m.Wr(OFS, 0);
    }

    // js: game/sound/driver.js loopStart
    /// <summary>24:85B2 (x or x+6)</summary>
    private static void LoopStart(Machine m, int x, int y)
    {
        m.Wr(L1_CNT + x, PtrY(m, y));
        Inc(m, OFS);
        int lo = m.Rd(OFS) + m.Rd(PTR);
        m.Wr(L1_LO + x, lo & 0xFF);
        m.Wr(L1_HI + x, (m.Rd(PTR + 1) + (lo >> 8)) & 0xFF);
    }

    // js: game/sound/driver.js opFlow
    /// <summary>24:8584 $Ax</summary>
    private static void OpFlow(Machine m, int x)
    {
        int y = m.Rd(OFS);
        int n = m.Rd(OP) & 0x0F;
        if (n == 0)
        {                                                        // 24:85C9 call: return address -> loop-2 slot
            int lo = PtrY(m, y); y = (y + 1) & 0xFF;
            int hi = PtrY(m, y); y = (y + 1) & 0xFF;
            m.Wr(L2_CNT + x, y);
            int s = y + m.Rd(PTR);
            m.Wr(L2_LO + x, s & 0xFF);
            m.Wr(OFS, 0);
            m.Wr(L2_HI + x, (m.Rd(PTR + 1) + (s >> 8)) & 0xFF);
            if (x >= 4) hi = (hi + 0x20) & 0xFF;                 // ADC #$1F with C = 1
            m.Wr(PTR + 1, hi); m.Wr(PTR, lo);
        }
        else if (n == 4) LoopEnd(m, x);
        else if (n == 1) LoopStart(m, x, y);
        else if (n == 2) LoopStart(m, x + 6, y);
        else LoopEnd(m, x + 6);
    }

    // js: game/sound/driver.js opEnvelope
    /// <summary>24:85F7 $Ex</summary>
    private static void OpEnvelope(Machine m, int x)
    {
        m.Wr(ENV + x, (m.Rd(OP) << 4) & 0xFF);
        Or(m, FLAGS + x, 0x40);
    }

    // js: game/sound/driver.js opVibrato
    /// <summary>24:8685 $Fx</summary>
    private static void OpVibrato(Machine m, int x)
    {
        int v = (m.Rd(OP) & 0x0F) << 3;
        m.Wr(VIB + x, v);
        if (v < 0x40) Or(m, FLAGS + x, 0x20);
        else if (v == 0x40) And(m, FLAGS + x, 0xDF);
        else Inc(m, FEND);
    }

    // js: game/sound/driver.js drumSelect
    /// <summary>24:863B</summary>
    private static void DrumSelect(Machine m, int x)
    {
        if ((x & 4) != 0) return;
        int v = m.Rd(OP) & 0x0F;
        if (v == 0) { m.Wr(DRUM, 0); m.ApuWrite(0x4013, 0); return; }
        m.Wr(DRUM, v | 0x10);
    }

    // js: game/sound/driver.js drumTrigger
    /// <summary>24:8651</summary>
    private static void DrumTrigger(Machine m, int x)
    {
        if ((x & 4) != 0) return;
        int d = m.Rd(DRUM) & 0x0F;
        if (d == 0) { m.Wr(DRUM, 0); return; }
        int y = d << 2;
        m.ApuWrite(0x4013, 0);
        m.ApuWrite(0x4015, 0x0F);
        for (int i = 0; i < 4; i++) m.ApuWrite(0x4010 + i, Rom(m, DMC + y + i));
    }

    // js: game/sound/driver.js readOp
    /// <summary>SndReadOp 24:8347; the switch = JS OP_BY_ADDR (handler address as stored in SndOpcodeTable -> port)</summary>
    private static void ReadOp(Machine m, int x)
    {
        Inc(m, OPCNT);
        int y = m.Rd(OFS);
        int a = PtrY(m, y);
        Inc(m, OFS);
        m.Wr(OP, a);
        int t = (a >> 3) & 0x1E;
        int addr = Rom(m, OPCODE_TABLE + t) | Rom(m, OPCODE_TABLE + t + 1) << 8;
        m.Wr(0xF8, addr & 0xFF); m.Wr(0xF9, addr >> 8);
        Action<Machine, int> h = addr switch
        {
            0x8364 => OpEnd, 0x83E5 => OpNote, 0x8422 => OpOctave, 0x8436 => OpRest, 0x847D => OpDuration,
            0x848A => OpLegato, 0x8532 => OpVolume, 0x854D => OpDuty, 0x8584 => OpFlow, 0x83E2 => OpNoteSweep,
            0x8395 => OpSweep, 0x83C2 => OpNoteDetune, 0x85F7 => OpEnvelope, 0x8685 => OpVibrato,
            _ => throw new InvalidOperationException($"SndReadOp: no port for handler ${addr:x}"),
        };
        h(m, x);
    }

    // js: game/sound/driver.js envelopeTick
    /// <summary>SndEnvelopeTick 24:84AF</summary>
    private static void EnvelopeTick(Machine m, int x)
    {
        if ((m.Rd(FLAGS + x) & 0x80) != 0) return;
        for (int guard = 0; guard < 4096; guard++)
        {
            int e = m.Rd(ENV + x);
            if ((e & 0x0F) != 0x0F)
            {
                Inc(m, ENV + x);
                int a = Rom(m, (Type(m, x) == 3 ? ENV_NOISE : ENV_TONAL) + e);
                m.Wr(OP, a & 0xCF);
                if ((a & 0x10) == 0)
                {                                                // 24:84F1
                    int v = m.Rd(OP) & 0x0F;
                    m.Wr(OP, v);
                    And(m, DIRTY, 0xFE);                         // LSR $FC / ASL $FC
                    int y = ((m.Rd(REG0 + x) << 4) | v) & 0xFF;
                    int @out;
                    if ((a & 0xC0) != 0)
                    {
                        m.Wr(OP, a & 0xC0);
                        @out = (m.Rd(REG0 + x) & 0x30) | m.Rd(OP) | Rom(m, VOLUME + y);
                    }
                    else @out = (m.Rd(REG0 + x) & 0xF0) | Rom(m, VOLUME + y);
                    m.ApuWrite(0x4000 + RegOf(m, x), @out);
                    return;
                }
            }
            if (m.Rd(FADE) == 0) { Or(m, ENV + x, 0x0F); return; }  // 24:84DB
            Dec(m, ENV + x); Dec(m, ENV + x);                       // fade: back up 2 steps, re-read
        }
        throw new InvalidOperationException("SndEnvelopeTick: fade back-up loop did not end");
    }

    // js: game/sound/driver.js vibratoTick
    /// <summary>SndVibratoTick 24:86AA</summary>
    private static void VibratoTick(Machine m, int x)
    {
        int f = m.Rd(FLAGS + x);
        if ((f & 0x80) != 0) return;
        if ((f & 0x40) != 0 && (m.Rd(ENV + x) & 0x0F) != 0x0F) return;
        Inc(m, OPCNT);
        int ph = (m.Rd(VIB + x) + 1) & 0x07;
        m.Wr(OP, ph);
        int v = (m.Rd(VIB + x) & 0xF8) | ph;
        m.Wr(VIB + x, v);
        if ((v & 0x08) != 0) m.Wr(OP, m.Rd(OP) >> 1);
        m.Wr(OP, m.Rd(OP) & 0x03);
        int y = ((v >> 2) & 0xFC) | m.Rd(OP);
        int e = Rom(m, VIB_STEP + y);
        if (e != 0) y = ((((e + 0xFE) & 0xFF) | m.Rd(OCT + x)) + 4) & 0xFF;
        else y = 0;
        int lo = Rom(m, VIB_OFS + y) + m.Rd(PER_LO + x);
        m.Wr(OP, lo & 0xFF);
        int hi = (Rom(m, VIB_OFS + 1 + y) + m.Rd(PER_HI + x) + (lo >> 8)) & 0xFF;
        if (hi != m.Rd(PER_HI + x)) return;
        m.ApuWrite(0x4002 + RegOf(m, x), m.Rd(OP));
    }

    // js: game/sound/driver.js fadeStep
    /// <summary>SndFadeStep 24:8609</summary>
    private static void FadeStep(Machine m)
    {
        for (int y = 0; y < 4; y++)
        {
            if ((m.Rd(FLAGS + y) & 1) == 0) continue;
            int r0 = m.Rd(REG0 + y);
            if (y == 2) { m.Wr(FLAGS + 2, 0); m.Wr(REG0 + 2, 0); return; }
            if ((r0 & 0x0F) != 0) m.Wr(REG0 + y, (r0 - 1) & 0xFF);
        }
    }

    // js: game/sound/driver.js channelTick
    /// <summary>SndChannelTick 24:8276</summary>
    private static void ChannelTick(Machine m, int x)
    {
        if ((m.Rd(FLAGS + x) & 1) == 0) return;
        m.Wr(DIRTY, 0); m.Wr(FEND, 0); m.Wr(OFS, 0);
        Dec(m, WAIT + x);
        if (m.Rd(WAIT + x) == 0)
        {
            m.Wr(SAVEX, x);
            m.Wr(PTR, m.Rd(PTR_LO + x)); m.Wr(PTR + 1, m.Rd(PTR_HI + x));
            for (int guard = 0; ; guard++)
            {
                if (guard > 100000) throw new InvalidOperationException($"SndChannelTick {x}: stream never ends a frame");
                ReadOp(m, x);
                if (m.Rd(FEND) != 0) break;
            }
            m.Wr(WAIT + x, m.Rd(LEN + x));
            int lo = m.Rd(PTR) + m.Rd(OFS);
            m.Wr(PTR_LO + x, lo & 0xFF);
            m.Wr(PTR_HI + x, (m.Rd(PTR + 1) + (lo >> 8)) & 0xFF);
        }
        m.ApuWrite(0x4015, m.Rd(DRUM) | 0x0F);                   // 24:82B3
        if ((m.Rd(DIRTY) & 0x08) != 0)
        {
            And(m, ENV + x, 0xF0); And(m, VIB + x, 0xF8); And(m, FLAGS + x, 0xEF);
        }
        if ((m.Rd(FLAGS + x) & 0x10) == 0)
        {
            if ((m.Rd(FLAGS + x) & 0x40) != 0) EnvelopeTick(m, x);
            if ((m.Rd(FLAGS + x) & 0x20) != 0) VibratoTick(m, x);
        }
        int fd = m.Rd(FADE);                                     // 24:82F3
        if (fd != 0)
        {
            if ((fd & 0x3F) != 0) Dec(m, FADE);
            else { FadeStep(m); m.Wr(FADE, 0xBC); }
        }
        if ((m.Rd(FLAGS + x) & 0x80) != 0) return;               // 24:8307
        int y = RegOf(m, x);
        int d = m.Rd(DIRTY);
        m.Wr(DIRTY, d >> 4);                                     // four LSR $FC
        if ((d & 1) != 0) m.ApuWrite(0x4000 + y, m.Rd(REG0 + x));
        if ((d & 2) != 0) m.ApuWrite(0x4001 + y, m.Rd(SWEEP + x));
        if ((d & 4) != 0) m.ApuWrite(0x4002 + y, m.Rd(PER_LO + x));
        if ((d & 8) != 0)
        {
            m.ApuWrite(0x4001 + y, m.Rd(SWEEP + x));
            m.Wr(SWEEP + x, (m.Rd(SWEEP + x) & 0x7F) | 0x08);
            m.ApuWrite(0x4003 + y, m.Rd(PER_HI + x) | 0x08);
        }
    }

    // js: game/sound/driver.js sndTick
    /// <summary>SndTick 24:8186</summary>
    public static void SndTick(Machine m)
    {
        int a = m.Rd(F);
        if ((a & 1) != 0) return;
        m.Wr(OPCNT, 0);
        a = (a & 0xFE) | 1;
        m.Wr(F, a); m.Wr(OP, a);
        if ((a & 0x80) != 0)
        {                                                        // music stop request
            And(m, F, 0xDF);
            if ((m.Rd(MUSIC_ID) & 0x80) == 0) { m.Wr(MUSIC_ID, 0xFF); m.ApuWrite(0x4015, m.Rd(CHN)); }
        }
        m.Wr(OP, (a << 1) & 0xFF);                               // 24:81AE ASL $F3
        if ((a & 0x40) != 0 && (m.Rd(SFX_PRI) & 0x80) == 0)
        {                                                        // SFX stop request
            m.Wr(SFX_PRI, 0xFF);
            m.ApuWrite(0x4015, m.Rd(CHN) ^ 0x0F);
            m.Wr(CHN, 0);
            Unsteal(m, 1); Unsteal(m, 3);
        }
        if ((m.Rd(MUSIC_ID) & 0x80) == 0 && (m.Rd(F) & 0x20) == 0) MusicTick(m);
        if ((m.Rd(SFX_PRI) & 0x80) == 0 && m.Rd(OPCNT) < 0x14)
        {                                                        // 24:8258
            m.PrgR7 = 0x0C;
            ChannelTick(m, 4); ChannelTick(m, 5);
            if (m.Rd(CHN) == 0) m.Wr(SFX_PRI, 0xFF);
        }
        And(m, F, 0xFE);
    }

    // js: game/sound/driver.js musicTick
    /// <summary>24:81F3: music channels 0-3, then the $0113 chain when all four have ended</summary>
    private static void MusicTick(Machine m)
    {
        if ((m.ApuRead(0x4015) & 0x10) != 0) m.ApuWrite(0x4013, 0);
        for (int x = 0; x < 4; x++) ChannelTick(m, x);
        if (((m.Rd(FLAGS) | m.Rd(FLAGS + 1) | m.Rd(FLAGS + 2) | m.Rd(FLAGS + 3)) & 1) != 0) return;
        int next = m.Rd(NEXT_MUSIC);
        m.Wr(CMD, next);
        if (next == 0 || MusicHeader(m, next)) { m.Wr(MUSIC_ID, 0xFF); return; }
        int y = 1;
        for (int x = 0; x < 4; x++)
        {
            m.Wr(PTR_LO + x, PtrY(m, y)); y++;
            m.Wr(PTR_HI + x, PtrY(m, y)); y++;
            Or(m, FLAGS + x, 1);
            m.Wr(WAIT + x, 1);
        }
    }
}
