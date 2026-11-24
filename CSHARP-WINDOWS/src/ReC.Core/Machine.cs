// re-c C# port — Machine: RAM/WRAM + PPU + frame sequencing (= web/src/game/machine.js; docs/architecture_web.md §2.1).
//
//   var m = new Machine(data); m.Reset();
//   m.RunFrame(buttons)  -> m.Ppu.Fb (256x240 NES colour indices) for the frame shown between two NMIs
//
// Frame order (one NES frame): MainLoop work (runs if the previous NMI set frame_ready) -> the PPU draws
// the frame from the state the previous NMI uploaded (IRQ splits scheduled by that NMI's mode routine)
// -> NMI work. The game only talks to the PPU through its registers (Ppu.WriteReg / OamDma / SetChrReg).
using ReC.Game.States;
using ReC.Gen;
using ReC.Nes;
using ReC.Util;

namespace ReC.Game;

/// <summary>an IRQ split scheduled by an NMI-mode routine: IrqHandlerTable index + MMC3 latch</summary>
public readonly record struct IrqSplit(int Handler, int Latch);

// js: game/machine.js Machine
public sealed class Machine
{
    public const double FrameHz = 60.0988;     // NTSC: 39375000/11/341/262*3 (odd-frame skip ignored)
    private const int FrameCycles3 = 341 * 262;   // CPU cycles per frame x 3 (= 29780.67 cycles)

    public readonly Data Data;
    public readonly Action<string> Log;
    public readonly Ram Mem;
    public readonly Ppu Ppu;
    /// <summary>pad 1 / pad 2 button bytes (A B Sel St U D L R)</summary>
    public readonly int[] Buttons = new int[2];
    /// <summary>pads.js `m.padDirTable` cache (data table PadDirTable)</summary>
    public int[]? PadDirTable;
    public readonly HashSet<string> Unported = new();
    public readonly Dictionary<string, int> StubHits = new();
    public List<IrqSplit> IrqSplits = new();
    public IEnumerator<Y>? Pending;
    /// <summary>obj/sprites.js module variable `regY`: the 6502 Y at ObjSpriteDraw's RTS (s198)</summary>
    public int SpriteRegY;
    public int Frame;
    public long RomRd;                         // s211: prg.js cpuRd calls (web/test/pass_count.mjs)
    public long Cyc;                           // s213: exact ROM cycles of modelled code (obj/collide.js)
    // s215: per-stage rd / wr / cpuRd counts (LagCnt) + m.Cyc deltas (LagCyc) of the current frame for the lag model
    public bool LagModelOn = true;
    public Dictionary<string, int[]> LagCnt = new();
    public Dictionary<string, long> LagCyc = new();
    private string _stage = "top";
    private int[] _lb = new int[3];
    private long _rom0, _cyc0;
    public bool LagPass;                       // s215: a whole SubstPlay pass ran this frame (states/play.js)
    public int LagLine = 252;                  // s215: start line of the next pass (last NMI's length)
    public int? LagStart;                      // s215: frame the current SubstPlay pass started (states/play.js)
    public bool LagMode7 = true;               // s218: also judge irq_mode 7 (boss) passes
    public bool LagMode2 = true;               // s220: also irq_mode 2 (AI $70 flash phases)
    public int LagFrames;
    // s215: the HUD IRQ's SoundUpdate skips the queued command when every queued entry was added after L189
    public int SndLate;
    public Apu Apu;
    private int _cyc3;
    public int PrgR6, PrgR7;
    // frame-sequencing flags set by mainloop / states (JS dynamic properties)
    public bool NoNmiResume, SkipNmi, Overrun, OverrunNmi, HudNmiLost;
    /// <summary>IRQ handlers (IrqHandlerTable 31:F433), index = $56; called as handler(m, line)</summary>
    public readonly Action<Machine, int>[] IrqHandlers;

    // js: game/machine.js constructor
    public Machine(Data data, Action<string>? log = null)
    {
        Data = data;
        Log = log ?? (_ => { });
        Mem = new Ram();
        Ppu = new Ppu(data.Chr);
        LagCnt["top"] = _lb;
        Apu = new Apu(a => Prg.CpuRd(this, a));
        ApuWrite = (a, v) => Apu.Write(a, v);
        ApuRead = a => Apu.Read(a);
        IrqHandlers = new Action<Machine, int>[]
        {
            MainLoop.IrqWrapSplit,                     // 31:F455
            MainLoop.IrqHud,                           // 31:F488
            MainLoop.IrqMsgWindowTop,                  // 31:F4E5
            MainLoop.IrqMsgWindowEnd,                  // 31:F54B
            MainLoop.IrqRowStretch,                    // 31:F5E1
            MainLoop.IrqOffSound,                      // 31:F60E
            MainLoop.IrqIntroSplit,                    // 31:F691
            MainLoop.IrqMenuChrSplit,                  // 31:F61A
        };
    }

    /// <summary>SoundUpdate 31:F7FE run as passStage 'snd' (machine.js constructor `this.soundUpdate`); the HUD
    /// IRQ's call (stage 'nmi') skips the queued command when only late entries are queued (m.SndLate).</summary>
    public void SoundUpdate(Machine m)
    {
        string s = _stage;
        PassStage = "snd";
        int q0 = -1;
        if (SndLate != 0 && s == "nmi")
        {
            int q = Mem.Rd(0x0100);
            if (q <= SndLate) { q0 = q; Mem.Wr(0x0100, 0); }
            SndLate = 0;
        }
        Sound.SoundUpdate.Run(m);
        if (q0 >= 0) Mem.Wr(0x0100, q0);
        PassStage = s;
    }

    /// <summary>APU access of the sound driver (machine.js constructor `this.apuWrite` / `this.apuRead`); replaceable
    /// like the JS closures (sound tests log the writes and read $4015 as 0)</summary>
    public Action<int, int> ApuWrite;
    public Func<int, int> ApuRead;
    /// <summary>PpuQueueFlush 30:C67D (world/ppuqueue.js), called by the NMI at F40A</summary>
    public void PpuQueueFlush(Machine m) => _ = World.PpuQueue.PpuQueueFlush(m);
    /// <summary>AreaLoadMap 31:E634 PlaySound (Y = 0)</summary>
    public void PlaySoundHook(int id) => Game.PlaySound.Play(this, id, 0);
    /// <summary>AreaSpawnEntry 31:E1B6 (world/spawn.js, entry at ($10),Y)</summary>
    public int SpawnEntry(int x, int y) => World.Spawn.AreaSpawnEntry(this, x, y);
    /// <summary>26:98A8 ObjPosToTemp (the $6D = $FF entry)</summary>
    public void EntryFF(int x) => Obj.ObjUtil.ObjPosToTemp(this, x);

    // js: game/machine.js rd
    public int Rd(int a) { _lb[0]++; return Mem.Rd(a); }

    // js: game/machine.js wr
    public void Wr(int a, int v) { _lb[1]++; Mem.Wr(a, v); }

    // js: game/machine.js passStage
    /// <summary>s211 main-loop stage (states/play.js, obj/objects.js); s215: switching it books the cpuRd / m.Cyc deltas</summary>
    public string PassStage
    {
        get => _stage;
        set
        {
            _lb[2] += (int)(RomRd - _rom0); _rom0 = RomRd;
            LagCyc[_stage] = (LagCyc.TryGetValue(_stage, out long c) ? c : 0) + Cyc - _cyc0; _cyc0 = Cyc;
            _stage = value;
            if (!LagCnt.TryGetValue(value, out var lb)) LagCnt[value] = lb = new int[3];
            _lb = lb;
        }
    }

    // js: game/machine.js _lagBegin
    /// <summary>s215: clear the per-frame stage buckets (start of RunFrame)</summary>
    private void LagBegin()
    {
        LagCnt = new(); LagCyc = new();
        _stage = "top"; _lb = LagCnt["top"] = new int[3];
        _rom0 = RomRd; _cyc0 = Cyc;
        LagPass = false;
        SndLate = 0;
    }

    // js: game/machine.js reset
    /// <summary>Reset 31:F2A4, the parts that set state the frame skeleton depends on. The cold-boot intro
    /// (bank 18, 212 frames) is not run: the machine starts at the first MainLoop pass with main_state = 2.</summary>
    public void Reset()
    {
        Array.Clear(Mem.RamBytes);
        Ppu.Reset();
        Ppu.Mirroring = 1;                             // $A000 = 1 (horizontal)
        for (int a = 0x6140; a < 0x6160; a++) Wr(a, 0xFF);   // pal_buf (31:F315)
        for (int a = 0x6200; a < 0x6220; a++) Wr(a, 0xFF);   // PPU queue ring
        Wr(0x0182, 0x55); Wr(0x0183, 0x53); Wr(0x0184, 0x4F);   // "USO" warm-boot signature
        Wr(0x07FE, 9);                                 // IRQ delay
        Wr(R.IrqMode, 3);                              // first $51 write
        Wr(R.PpuctrlShadow, 0x88);
        Ppu.WriteReg(0, 0x88);
        Wr(R.MainState, 2);
        Pending = null;
        IrqSplits = new();
        Frame = 0;
        PrgR6 = 0x18;                                  // MMC3 R6/R7 as left by reset's SwitchBank16($0C) (prg.js)
        PrgR7 = 0x19;
        var @out = Apu.Out;
        var chans = Apu.OutChannels;                   // Windows port T13: the CD mixer's sink carries over too
        int rate = Apu.SampleRate;
        Apu = new Apu(a => Prg.CpuRd(this, a));
        if (rate != 0) Apu.SetOutput(rate, @out);
        Apu.OutChannels = chans;
        Apu.Write(0x4010, 0);                          // 31:F2C9 DMC IRQ off
        Apu.Write(0x4017, 0x40);                       // 31:F2CE APU frame IRQ off
        Sound.Driver.SndInit(this);                    // 31:F325 JSR $8020 with banks 24/25
        // s106: cold path: what the skipped intro leaves behind, then the save side of Reset -> MainLoop
        Boot.IntroLeftovers(this);
        Boot.ColdSaveInit(this);
        Boot.BootSaveCheck(this);
        Boot.CopySramBlock(this);
        _cyc3 = 0;
    }

    // js: game/machine.js loadSplits
    /// <summary>s176: the IRQ split the NMI just before a loaded state scheduled (a state taken in vblank, line > 240).
    /// Every NMI mode leaves its SetIrqHandler index in $56 and the latch in $57, except mode 5 (latch $58);
    /// modes 0/2/7 with rendering off and mode 3 schedule none.</summary>
    private static List<IrqSplit> LoadSplits(Machine m, StateFile s)
    {
        if (!(s.Line > 240)) return new();             // mid-frame state: this frame's split is already past
        int mode = m.Rd(R.IrqMode) & 0x7F;
        if (mode == 3 || mode == 8) return new();
        if ((mode == 0 || mode == 2 || mode == 7) && (m.Rd(R.PpumaskShadow) & 0x18) == 0) return new();
        if (mode == 5) return new() { new IrqSplit(6, m.Rd(R.ChrR0Alt)) };
        if (mode == 4) return m.Rd(R.ChrR0Alt) != 0 ? new() { new IrqSplit(4, 4) } : new();   // s208: NmiMode4 31:F782
        return new() { new IrqSplit(m.Rd(0x56), m.Rd(R.Zp57)) };
    }

    // js: game/machine.js loadState
    // js: game/machine.js hex
    /// <summary>Start from a tools.emu state taken at the first instruction of a routine the port runs from its start
    /// (tools/emu/web_state.py): RAM, WRAM, CIRAM, palette, OAM, PPU registers, MMC3 CHR/PRG registers and mirroring.
    /// The next RunFrame() is tools.emu frame `s.Frame`. The split the NMI just before scheduled is rebuilt (LoadSplits).</summary>
    public void LoadState(StateFile s)
    {
        s.Ram.CopyTo(Mem.RamBytes, 0);
        s.Wram.CopyTo(Mem.Wram, 0);
        var p = Ppu;
        s.Ciram.CopyTo(p.Vram, 0); s.Pal.CopyTo(p.Pal, 0); s.Oam.CopyTo(p.Oam, 0);
        p.Ctrl = s.Ctrl; p.Mask = s.Mask; p.V = s.V; p.T = s.T; p.X = s.X; p.W = s.W;
        p.LatchVert();
        for (int r = 0; r < 6; r++) p.SetChrReg(r, s.MapperRegs[r]);
        p.Mirroring = s.Mirroring;
        p.ChrInv = s.ChrInv;
        PrgR6 = s.MapperRegs[6] & 0x3F;
        PrgR7 = s.MapperRegs[7] & 0x3F;
        Pending = null;
        IrqSplits = LoadSplits(this, s);
        // s94: a routine reached by JSR from unported code
        // (no entry = a state taken at a routine the dispatch reaches by itself: nothing pending, as in the JS)
        if (s.Until != null && Play.EntryPoints.TryGetValue(s.Until, out var entry)) Pending = entry(this).GetEnumerator();
    }

    // js: game/machine.js _installSplits
    /// <summary>Map the split list from the last NMI-mode routine onto Ppu.Splits (an MMC3 IRQ with latch L fires at
    /// the end of line L, so its register changes show from line L+1).</summary>
    private void InstallSplits()
    {
        var s = Ppu.Splits;
        s.Clear();
        foreach (var (handler, latch) in IrqSplits)
        {
            int line = latch + 1;
            int h = handler;
            if (line < 240) s[line] = _ => IrqHandlers[h](this, line);
        }
    }

    // js: game/machine.js runFrame
    public byte[] RunFrame(int buttons = 0, int buttons2 = 0)
    {
        Buttons[0] = buttons & 0xFF;
        Buttons[1] = buttons2 & 0xFF;
        LagBegin();
        MainLoop.MainStep(this);
        InstallSplits();
        PassStage = "nmi";                             // s215: render + IRQ + NMI work (lag model `nmi` bucket)
        Ppu.RenderFrame();
        // s215: a whole SubstPlay pass whose estimated ROM cycles reach the next NMI is a CPU overrun:
        // frame_ready stays 1 for this NMI (MainStep cleared it already)
        if (LagModelOn && LagPass && !OverrunNmi && LagModel.LagDomain(this)
            && LagModel.PassLags(LagCnt, LagCyc, LagLine, IrqSplits))
        {
            Wr(0x09, 1);
            OverrunNmi = true;
            LagFrames++;
        }
        int nmiExtra = 0;                              // s218: irq_mode 7 / 9 slot-$13 scroll copy (lagmodel.js)
        if (SkipNmi) SkipNmi = false;                  // s107: NO_NMI (lost NMI, see states/death.js)
        else if (HudNmiLost) { HudNmiLost = false; MainLoop.NmiLostToHud(this); }   // s174: see IrqHud
        else
        {
            if (Rd(0x09) == 0) nmiExtra = LagModel.NmiModeExtra.TryGetValue(Mem.Rd(0x51), out int e) ? e : 0;
            MainLoop.NmiWork(this);
        }
        // s114: CPU overrun. The ROM's frame ran past line 241, so its NMI found frame_ready != 0; then MainLoop's
        // 30:C919 clears it and waits a whole frame for the next NMI.
        if (OverrunNmi) { OverrunNmi = false; Wr(0x09, 0); }
        PassStage = "nmi";                             // flush the last cpuRd / m.Cyc deltas
        LagLine = LagModel.StartLine(LagCnt.TryGetValue("nmi", out var nc) ? nc : null, nmiExtra);   // s215
        Ppu.LatchVert();                               // s125: pre-render vertical copy (see Ppu.LatchVert)
        _cyc3 += FrameCycles3;
        int n = _cyc3 / 3;
        _cyc3 -= n * 3;
        Apu.Run(n);
        Frame++;
        return Ppu.Fb;
    }
}
