"""re-c emulator CLI (Ph5.5 emu 4/6).

usage (repo root): python3 -m tools.emu [options]

Run control
  --rom PATH                 ROM (default rom/re-c.nes)
  --load-state FILE          start from a save state instead of power-on
  --frames N                 frames to run (default 60; 0 = none)
  --keys SPEC [SPEC ...]     pad-1 script, SPEC = FRAME:BUTTONS[:LEN] (absolute frame number =
                             ppu.frame_no, hold LEN frames, default 2), e.g. 120:START 300:A+RIGHT:8;
                             @FILE reads specs from a file (one or more per line, # comments)
  --boot-test                run (default 600 frames) and check NMIs fire, PC is in ROM, the
                             screen is not blank and no illegal opcode hit; exit 1 on failure
Debugging
  --trace [FILE]             instruction trace (default stdout), see debug.Tracer for the format
  --trace-range R            only PCs in R = [BB:]AAAA-AAAA (repeatable)
  --trace-frames A-B         only frames A..B
  --trace-max N              stop writing after N lines
  --break SPEC               stop before executing SPEC = BB:AAAA / AAAA / label (repeatable);
                             dumps registers, stack and zero page
  --break-hit N              stop on the Nth hit instead of the first
  --watch ADDR               print CPU writes to ADDR that change its value (hex, repeatable;
                             --watch-all prints every write)
  --run-func SPEC            after the frames above, call SPEC (label or BB:AAAA) like JSR and stop
                             at its RTS; preset with --a/--x/--y/--set ADDR=VAL
Output
  --dump-regs                print registers/banks at the end (+ stack, no ZP)
  --dump-ram FILE            2KB internal RAM ($0000-$07FF), raw
  --dump-wram FILE           8KB WRAM ($6000-$7FFF), raw
  --dump-screen FILE.png     last finished frame (--scale S)
  --save-state FILE          save state at the end
  --log-apu [FILE]           APU register writes (frame cycle addr value)
  --bench                    print frames/second only
"""
import argparse
import os
import sys
import time

from .nes import NES, Stop
from . import debug, state


def _hex(s):
    return int(s, 16)


def build_parser():
    ap = argparse.ArgumentParser(prog='python3 -m tools.emu', description='re-c NES emulator',
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('--rom', default=None)
    ap.add_argument('--load-state', metavar='FILE')
    ap.add_argument('--frames', type=int, default=None)
    ap.add_argument('--keys', nargs='+', action='extend', default=[], metavar='SPEC')
    ap.add_argument('--boot-test', action='store_true')
    ap.add_argument('--trace', nargs='?', const='-', metavar='FILE')
    ap.add_argument('--trace-range', action='append', default=[], metavar='R')
    ap.add_argument('--trace-frames', metavar='A-B')
    ap.add_argument('--trace-max', type=int)
    ap.add_argument('--break', dest='breaks', action='append', default=[], metavar='SPEC')
    ap.add_argument('--break-hit', type=int, default=1)
    ap.add_argument('--watch', action='append', default=[], type=_hex, metavar='ADDR')
    ap.add_argument('--watch-all', action='store_true')
    ap.add_argument('--run-func', metavar='SPEC')
    ap.add_argument('--a', type=lambda s: int(s, 0))
    ap.add_argument('--x', type=lambda s: int(s, 0))
    ap.add_argument('--y', type=lambda s: int(s, 0))
    ap.add_argument('--set', action='append', default=[], metavar='ADDR=VAL')
    ap.add_argument('--max-instr', type=int, default=2_000_000)
    ap.add_argument('--dump-regs', action='store_true')
    ap.add_argument('--dump-ram', metavar='FILE')
    ap.add_argument('--dump-wram', metavar='FILE')
    ap.add_argument('--dump-screen', metavar='FILE.png')
    ap.add_argument('--scale', type=int, default=1)
    ap.add_argument('--save-state', metavar='FILE')
    ap.add_argument('--log-apu', nargs='?', const='-', metavar='FILE')
    ap.add_argument('--bench', action='store_true')
    return ap


def boot_test(nes, frames):
    fails = []
    nmi0 = nes.ppu.nmi_count
    try:
        nes.run_frames(frames)
    except Exception as e:          # IllegalOpcode etc.
        fails.append(f'exception: {e!r}')
    if nes.ppu.nmi_count - nmi0 < frames // 2:
        fails.append(f'only {nes.ppu.nmi_count - nmi0} NMIs in {frames} frames')
    if nes.cpu.pc < 0x8000:
        fails.append(f'PC ${nes.cpu.pc:04X} outside ROM')
    if len(set(nes.ppu.fb)) < 2:
        fails.append('screen is a single colour')
    print(f'boot-test: {frames} frames, nmi={nes.ppu.nmi_count - nmi0}, pc={debug.where(nes)}, '
          f'colours={len(set(nes.ppu.fb))}: ' + ('PASS' if not fails else 'FAIL ' + '; '.join(fails)))
    return not fails


def _open(path):
    return sys.stdout if path == '-' else open(path, 'w')


def main(argv=None):
    a = build_parser().parse_args(argv)
    nes = NES(a.rom)
    if a.load_state:
        state.load(nes, a.load_state)
    if a.keys:
        nes.keys = debug.parse_keys(a.keys)
    if a.log_apu:
        nes.apu.log = []

    trace_out = None
    if a.trace:
        trace_out = _open(a.trace)
        frames = tuple(int(v) for v in a.trace_frames.split('-')) if a.trace_frames else None
        nes.set_tracer(debug.Tracer(nes, trace_out, [debug.parse_range(r) for r in a.trace_range],
                                    frames, a.trace_max))

    hits = {'n': 0}
    for spec in a.breaks:
        bank, addr = debug.resolve(spec)

        def brk(n, spec=spec):
            hits['n'] += 1
            if hits['n'] >= a.break_hit:
                raise Stop(f'break {spec} (hit {hits["n"]})')
        nes.on_exec(bank if addr >= 0x8000 else None, addr, brk)

    for addr in a.watch:
        last = {}

        def w(ad, v, last=last):
            if not a.watch_all and last.get(ad) == v:
                return
            last[ad] = v
            print(f'write ${ad:04X}={v:02X}  frame={nes.ppu.frame_no} line={nes.ppu.line} '
                  f'pc={debug.where(nes)} {debug.label_at(nes, nes.cpu.pc) or ""}')
        nes.on_write(addr, w)

    ok = True
    stopped = None
    frames = a.frames if a.frames is not None else (600 if a.boot_test else 60)
    f0 = nes.ppu.frame_no
    t0 = time.time()
    try:
        if a.boot_test:
            ok = boot_test(nes, frames)
        else:
            nes.run_frames(frames)
        if a.run_func:
            bank, addr = debug.resolve(a.run_func)
            for kv in a.set:
                k, v = kv.split('=')
                nes.bus.write(int(k, 16), int(v, 0))
            c0 = nes.cpu.cycles
            n = nes.call(addr, bank, a.a, a.x, a.y, a.max_instr)
            print(f'run-func {a.run_func} ({bank}:{addr:04X}): returned after {n} instructions, '
                  f'{nes.cpu.cycles - c0} cycles; {debug.regs_line(nes)}')
    except Stop as e:
        stopped = str(e)
    dt = time.time() - t0
    ran = nes.ppu.frame_no - f0

    if trace_out is not None and trace_out is not sys.stdout:
        trace_out.close()
    if stopped:
        print(f'STOP: {stopped}')
        print(debug.dump_state(nes))
    if a.bench:
        print(f'{ran} frames in {dt:.2f}s = {ran / dt if dt else 0:.1f} fps')
    elif not a.boot_test:
        print(f'frames={ran} (frame_no={nes.ppu.frame_no}) cpu_cycles={nes.cpu.cycles} '
              f'nmi={nes.ppu.nmi_count} pc={debug.where(nes)} {ran / dt if dt else 0:.1f} fps',
              file=sys.stderr)
    if a.dump_regs and not stopped:
        print(debug.dump_state(nes, zp=False))
    if a.dump_ram:
        with open(a.dump_ram, 'wb') as f:
            f.write(nes.bus.ram)
    if a.dump_wram:
        with open(a.dump_wram, 'wb') as f:
            f.write(nes.bus.wram)
    if a.dump_screen:
        nes.dump_screen(a.dump_screen, a.scale)
    if a.save_state:
        d = os.path.dirname(a.save_state)
        if d:
            os.makedirs(d, exist_ok=True)
        state.save(nes, a.save_state)
    if a.log_apu:
        out = _open(a.log_apu)
        for fr, cyc, addr, v in nes.apu.log:
            out.write(f'{fr} {cyc} {addr:04X} {v:02X}\n')
        if out is not sys.stdout:
            out.close()
    return 0 if ok else 1


if __name__ == '__main__':
    sys.exit(main())
