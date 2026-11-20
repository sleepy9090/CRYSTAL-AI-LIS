"""Re-run one routine_capture case in tools.emu and print its JSR/JMP/RTS flow (session 111).

usage (repo root):
  python3 -m tools.emu.case_trace --load-state S --cases FILE.json --case NAME [--all]

Rebuilds the capture setup (state, run to the next ObjUpdateAll entry, the case's RAM + WRAM, the bank pair,
call2) and prints every JSR / JMP / RTS / RTI / BRK with its PC (--all: every instruction).
"""
import argparse
import json

from .nes import NES, Stop
from . import state
from .routine_capture import ROUTINES, OBJ_UPDATE_ALL, call2

FLOW = {0x20: 'JSR', 0x4C: 'JMP', 0x6C: 'JMP()', 0x60: 'RTS', 0x40: 'RTI', 0x00: 'BRK'}


def main(argv=None):
    ap = argparse.ArgumentParser(prog='python3 -m tools.emu.case_trace')
    ap.add_argument('--load-state', required=True)
    ap.add_argument('--cases', required=True)
    ap.add_argument('--case', required=True)
    ap.add_argument('--all', action='store_true')
    a = ap.parse_args(argv)
    j = json.load(open(a.cases))
    case = next(c for c in j['cases'] if c['name'] == a.case)
    nes = NES()
    state.load(nes, a.load_state)

    def grab(_n):
        raise Stop('base')
    nes.on_exec(None, OBJ_UPDATE_ALL, grab)
    try:
        nes.run_frames(600)
    except Stop:
        pass
    nes.clear_hooks()
    ram = bytes.fromhex(case['ram'])
    wram = bytearray(bytes.fromhex(j['wram_base']))
    for ad, v in case['wram']:
        wram[ad - 0x6000] = v
    nes.bus.ram[:] = ram
    nes.bus.wram[:] = wram
    bank, addr, _, _ = ROUTINES[case['routine']]

    def tr(cpu):
        op = nes.bus.read(cpu.pc)
        if a.all or op in FLOW:
            lo, hi = nes.bus.read(cpu.pc + 1), nes.bus.read(cpu.pc + 2)
            print(f'{cpu.pc:04X} {FLOW.get(op, f"{op:02X}"):5} {hi:02X}{lo:02X}  A={cpu.a:02X} X={cpu.x:02X} '
                  f'Y={cpu.y:02X} S={cpu.s:02X}')
    nes.set_tracer(tr)
    print('ret', call2(nes, addr, bank, case['regs']))


if __name__ == '__main__':
    main()
