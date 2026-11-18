"""Save-path emulator probes (session 71, Ph5 save leftovers).

  python3 -m tools.emu.save_probe npc   [--state tools/emu/states/area1_enter.state]
      Fill npc_state $6580-$667F with a pattern, call SaveGame 23:BC09 for real (write watch on
      $7F00-$7FFF), wipe $6580-$667F, call LoadGameAndInit 23:BCDA, report which npc_state bytes
      came back.
  python3 -m tools.emu.save_probe boot  [--frames 2460] [--keys "SPEC SPEC ..."]
      Power-on + the canonical --keys run. Logs every write to main_state $40, every CPU read of
      $6480-$667F (story_flags / npc_state) with frame, PC and bank, and the frame of the boot copy
      CopySramBlock 31:F3A2 and of MainStateNewGame 30:C957 / MainStateContinue 30:C9DA.
"""
import argparse
import os
import sys

_HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(os.path.dirname(_HERE))
sys.path.insert(0, os.path.join(ROOT, 'tools', 'ref'))

from tools.emu.nes import NES  # noqa: E402
from tools.emu import debug, state  # noqa: E402


def load(nes, path):
    state.load(nes, path)


def ram(nes, a):
    return nes.bus.read(a)


def probe_npc(path):
    nes = NES()
    load(nes, os.path.join(ROOT, path) if not os.path.isabs(path) else path)
    area = ram(nes, 0x6C)
    print(f'state {path}: area_id=${area:02X} main_state={ram(nes, 0x40)} sub_state={ram(nes, 0x41)}')
    pat = [(i * 7 + 1) & 0xFF or 1 for i in range(256)]
    for i, v in enumerate(pat):
        nes.bus.write(0x6580 + i, v)
    writes = {}
    for a in range(0x7E00, 0x8000):
        nes.on_write(a, lambda ad, v: writes.__setitem__(ad, v))
    nes.bus.write(0x6E, 22)          # bank shadows: the NMI/IRQ restore R6/R7 from $6E/$6F
    nes.bus.write(0x6F, 23)
    n = nes.call(0xBC09, bank=23)
    print(f'SaveGame: {n} instr; writes to $7E00-$7FFF: {len(writes)} addrs')
    last_nonzero = [a for a, v in writes.items() if v]
    print(f'  $7F00 = ${ram(nes, 0x7F00):02X} (npc_state[0] = ${pat[0]:02X});'
          f' non-zero writes {min(last_nonzero):04X}-{max(last_nonzero):04X}' if last_nonzero else '  no non-zero writes')
    tail = [ram(nes, a) for a in range(0x7F01, 0x8000)]
    print(f'  $7F01-$7FFF non-zero bytes after save: {sum(1 for v in tail if v)}')
    nes.clear_hooks()
    for i in range(256):
        nes.bus.write(0x6580 + i, 0xEE)
    nes.call(0xBCDA, bank=23)
    back = [ram(nes, 0x6580 + i) for i in range(256)]
    same = [i for i in range(256) if back[i] == pat[i]]
    zero = [i for i in range(256) if back[i] == 0]
    print(f'LoadGameAndInit: npc_state restored = {same}; zeroed = {len(zero)}/256;'
          f' other = {256 - len(same) - len(zero)}')


def probe_boot(frames, keys=None):
    import compare_ref
    nes = NES()
    nes.keys = debug.parse_keys([keys or compare_ref.CANON_KEYS])
    log = []
    reads = {}

    def f():
        return nes.ppu.frame_no

    def pcbank():
        pc = nes.cpu.pc
        return f'{nes.mapper.prg_bank_at(pc) if pc >= 0x8000 else "*"}:{pc:04X}'

    for addr, name in ((0xF3A2, 'CopySramBlock'), (0xC957, 'MainStateNewGame'),
                       (0xC9DA, 'MainStateContinue'), (0xF0A4, 'BootSaveCheck')):
        nes.on_exec(None, addr, lambda n, name=name: log.append((f(), name)))
    nes.on_write(0x40, lambda a, v: log.append((f(), f'main_state <- {v} @ {pcbank()}')))
    nes.on_write(0x06F0, lambda a, v: log.append((f(), f'title_mode <- {v}')))
    nes.on_write(0x06F1, lambda a, v: v and log.append((f(), f'title_step <- {v}')))

    def rd(a, v):
        key = (pcbank(), 'npc' if a >= 0x6580 else 'flags')
        e = reads.setdefault(key, [f(), f(), 0])
        e[1] = f()
        e[2] += 1
    for a in range(0x6480, 0x6680):
        nes.on_read(a, rd)
    nes.run_frames(frames)
    for fr, what in log:
        print(f'f{fr:5d} {what}')
    print('reads of $6480-$667F (pc, region): first-last frame, count')
    for (pc, reg), (a, b, c) in sorted(reads.items(), key=lambda kv: kv[1][0]):
        print(f'  {pc:9s} {reg:5s} f{a}-f{b} x{c}')


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('mode', choices=('npc', 'boot'))
    ap.add_argument('--state', default='tools/emu/states/area1_enter.state')
    ap.add_argument('--frames', type=int, default=2460)
    ap.add_argument('--keys', default=None, help='one tools.emu --keys string (default: canonical); '
                    'CONTINUE: "1250:START 1400:START 1600:START 1690:SELECT 1700:START"')
    a = ap.parse_args()
    if a.mode == 'npc':
        probe_npc(a.state)
    else:
        probe_boot(a.frames, a.keys)


if __name__ == '__main__':
    main()
