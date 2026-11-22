#!/usr/bin/env python3
"""SoundUpdate 31:F7FE CPU cost (session 174).

The HUD IRQ (IrqHandler1Hud 31:F488) runs SoundUpdate between `STA nmi_busy_flag` 31:F4CF ($06 = 1) and
31:F4D4 ($06 = 0). When that window still covers line 241, the NMI only sets nmi_missed_flag $07 and
returns: no OAM DMA, no PpuQueueFlush / PaletteUpload, no frame_ready increment (a lost frame); the IRQ
then runs NmiModeDispatch alone. A new music track makes the first call long (all 4 channels read their
opening ops in one tick).

usage (repo root):
  python3 -m tools.emu.sound_cost_probe scene STATE FRAMES [--from F]
      per frame: line/dot and CPU cycle count of the busy window, and whether the NMI found $06 set
  python3 -m tools.emu.sound_cost_probe music STATE [--calls N]
      per music id $00-$1F: SndInit, queue the id as PlaySound does, then the cycles of the first N
      SoundUpdate calls (NMI / IRQ blocked, like tools/emu/sound_capture.py)
"""
import sys

from .nes import NES
from . import state

SOUND_UPDATE, SND_INIT = 0xF7FE, 0x8020


def load(path):
    nes = NES()
    state.load(nes, path)
    return nes


def scene(path, frames, lo):
    nes = load(path)
    cur = {}
    log = []

    def busy_on(n):
        cur['c0'], cur['at'] = n.cpu.cycles, (n.ppu.frame_no, n.ppu.line, n.ppu.dot)

    def busy_off(n):
        if 'c0' not in cur or n.ppu.frame_no < lo:
            return
        f, ln, d = cur.pop('at')
        log.append(f'f{f} busy L{ln}.{d} -> L{n.ppu.line}.{n.ppu.dot}  {n.cpu.cycles - cur.pop("c0")} cyc'
                   f'  queue {n.bus.ram[0x0100]} music_id {n.bus.ram[0x0111]:02X}')

    def nmi(n):
        if n.ppu.frame_no >= lo and n.bus.ram[0x06]:
            log.append(f'f{n.ppu.frame_no} NMI at L{n.ppu.line}: nmi_busy_flag set -> frame lost')
    nes.on_exec(31, 0xF4CF, busy_on)
    nes.on_exec(31, 0xF4D4, busy_off)
    nes.on_exec(31, 0xF3BE, nmi)
    nes.run_frames(frames)
    print('\n'.join(log))


def music(path, calls):
    nes = load(path)
    cpu = nes.cpu
    cpu.nmi = lambda: None
    base = bytes(nes.bus.ram)
    for mid in range(0x20):
        nes.bus.ram[:] = base
        cpu.p |= 0x04
        nes.call(SND_INIT, bank=24)
        nes.mapper.regs[6], nes.mapper.regs[7] = nes.bus.ram[0x6E], nes.bus.ram[0x6F]
        nes.mapper._update_prg()
        ram = nes.bus.ram
        ram[0x0103] = mid
        ram[0x0100] = 1
        out = []
        for _ in range(calls):
            c0 = cpu.cycles
            cpu.p |= 0x04
            nes.call(SOUND_UPDATE)
            out.append(cpu.cycles - c0)
        print(f'music {mid:02X}: ' + ' '.join(str(c) for c in out))


def main():
    a = sys.argv[1:]
    if len(a) < 2:
        raise SystemExit(__doc__)
    if a[0] == 'scene':
        lo = int(a[a.index('--from') + 1]) if '--from' in a else 0
        scene(a[1], int(a[2]), lo)
    elif a[0] == 'music':
        calls = int(a[a.index('--calls') + 1]) if '--calls' in a else 4
        music(a[1], calls)
    else:
        raise SystemExit(__doc__)


if __name__ == '__main__':
    main()
