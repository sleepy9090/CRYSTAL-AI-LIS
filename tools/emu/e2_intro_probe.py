#!/usr/bin/env python3
"""s163: BossAiE2 15:A675 intro timing probe from a saved state: frame / scanline of the phase-0 aligned path
(15:A68F), the PaletteBuild + first part build (A6C0 / A6C5), the first fade call (A6DB), every WaitFrame entry
(30:C8A5 hits listed only once the intro started), the NMIs (31:F3B6, with frame_ready) and the main-loop tops.

usage: python3 -m tools.emu.e2_intro_probe STATE FRAMES"""
import sys

from tools.emu.lag08_probe import load_nes

MARKS = {(15, 0xA68F): 'intro (aligned)', (15, 0xA6C0): 'PaletteBuild', (15, 0xA6C5): 'BuildParts(0)',
         (15, 0xA6C8): 'BuildParts(0) done', (15, 0xA6D6): 'sprite pass', (15, 0xA6D9): 'sprite pass done',
         (15, 0xA6DB): 'FadeIn($10) call', (15, 0xA6E0): 'FadeOut($10) call', (15, 0xA6E3): 'BuildParts($13)',
         (15, 0xA6F8): 'WaitPpuQueue', (15, 0xA72C): 'FadeIn($20) call', (15, 0xA73B): 'intro RTS'}


def main():
    if len(sys.argv) < 3:
        raise SystemExit(__doc__)
    nes = load_nes(sys.argv[1])
    frames = int(sys.argv[2])
    log, on = [], []

    def at(tag):
        def f(n):
            fr, ln = n.ppu.frame_no, n.ppu.line
            if tag in MARKS.values():
                on.append(1)
            if not on and tag not in ('top',):
                return
            s = f'  NMI frame_ready {n.bus.ram[0x09]:02X}' if tag == 'nmi' else tag
            log.append(f'f{fr} L{ln:3}: {s}')
        return f
    for (b, a), t in MARKS.items():
        nes.on_exec(b, a, at(t))
    nes.on_exec(30, 0xC904, at('top'))
    nes.on_exec(31, 0xF3B6, at('nmi'))
    nes.run_frames(frames)
    i = next((k for k, s in enumerate(log) if 'intro' in s), 0)
    print('\n'.join(log[max(0, i - 4):i + 60]))


if __name__ == '__main__':
    main()
