#!/usr/bin/env python3
"""s165: BossAiEB 15:B2DA (final boss) setup -> sway timing probe from a saved state (tools/emu/states/finalboss.state):
frame / scanline of the setup's FadeIn call / RTS, the sway / BossBgStrip entries, WaitPpuQueue spins, PaletteBuild
(26:8C12) steps, the NMIs (31:F3B6, with frame_ready) and the main-loop tops, from FROM to the end.

usage: python3 -m tools.emu.eb_setup_probe STATE FRAMES [FROM_FRAME]"""
import sys

from tools.emu.lag08_probe import load_nes

MARKS = {(15, 0xB3B3): 'FadeIn($10) call', (15, 0xB3B6): 'FadeIn done', (15, 0xB3BB): 'setup RTS',
         (15, 0xB3BC): 'sway', (15, 0xB6C1): 'BossBgStrip', (15, 0xB4C8): 'BossBgCycle',
         (15, 0xB39A): 'sprite pass', (15, 0xB39D): 'sprite pass done', (30, 0xCD2B): 'ObjUpdateAll INX',
         (26, 0x8C12): 'PaletteBuild', (30, 0xC676): 'WaitPpuQueue', (30, 0xC169): 'WaitFrame'}


def main():
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    nes = load_nes(sys.argv[1])
    frames = int(sys.argv[2])
    start = int(sys.argv[3]) if len(sys.argv) > 3 else 0
    log = []

    def at(tag):
        def f(n):
            fr, ln = n.ppu.frame_no, n.ppu.line
            if fr < start:
                return
            r = n.bus.ram
            s = f'  NMI frame_ready {r[0x09]:02X}' if tag == 'nmi' else tag
            log.append(f'f{fr} L{ln:3}: {s}  X={n.cpu.x:02X} x0D={r[0x7D]:02X} ph={r[0x060D]} behav={r[0x06ED]} t2={r[0x062D]:02X}')
        return f
    for (b, a), t in MARKS.items():
        nes.on_exec(b, a, at(t))
    nes.on_exec(30, 0xC904, at('top'))
    nes.on_exec(31, 0xF3B6, at('nmi'))
    nes.run_frames(frames)
    print('\n'.join(log[:400]))


if __name__ == '__main__':
    main()
