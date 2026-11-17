"""re-c emulator CLI (emu 3/6 subset; emu 4/6 adds --trace/--break/--keys/states/...).

usage (repo root): python3 -m tools.emu [--frames N] [--dump-screen FILE.png] [--scale S]
                                        [--log-apu [FILE]] [--rom PATH]
"""
import argparse
import sys
import time

from .nes import NES


def main(argv=None):
    ap = argparse.ArgumentParser(prog='python3 -m tools.emu')
    ap.add_argument('--rom', default=None)
    ap.add_argument('--frames', type=int, default=60)
    ap.add_argument('--dump-screen', metavar='FILE.png')
    ap.add_argument('--scale', type=int, default=1)
    ap.add_argument('--log-apu', nargs='?', const='-', metavar='FILE',
                    help='log APU register writes (frame cycle addr value); default stdout')
    a = ap.parse_args(argv)

    nes = NES(a.rom)
    if a.log_apu:
        nes.apu.log = []
    t0 = time.time()
    nes.run_frames(a.frames)
    dt = time.time() - t0
    p = nes.ppu
    print(f'frames={a.frames} cpu_cycles={nes.cpu.cycles} nmi={p.nmi_count} '
          f'mmc3_clocks={nes.mapper.irq_clocks} pc=${nes.cpu.pc:04X} '
          f'banks={nes.mapper.prg_banks} {a.frames / dt:.1f} fps', file=sys.stderr)
    if a.dump_screen:
        nes.dump_screen(a.dump_screen, a.scale)
    if a.log_apu:
        out = sys.stdout if a.log_apu == '-' else open(a.log_apu, 'w')
        for fr, cyc, addr, v in nes.apu.log:
            out.write(f'{fr} {cyc} {addr:04X} {v:02X}\n')
        if out is not sys.stdout:
            out.close()


if __name__ == '__main__':
    main()
