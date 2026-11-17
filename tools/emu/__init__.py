"""re-c pure-Python NES emulator (Ph5.5). No third-party emulators.

Modules:
  loader.py  - iNES loader (wraps tools/nesrom.py)            [emu 1/6]
  memory.py  - CPU bus: 2KB RAM mirror, 8KB WRAM, register dispatch, OAM DMA  [emu 1/6]
  cpu.py     - 2A03 core: official + instruction_set illegal opcodes, exact cycles, NMI/IRQ/BRK  [emu 1/6]
  mapper.py  - MMC3 (R0-R7, PRG/CHR modes, mirroring, WRAM gate, scanline IRQ + A12 filter)  [emu 2/6]
  ppu.py     - 2C02: registers, catch-up dot-range renderer, sprites, sprite-0, NMI, MMC3 clock  [emu 3/6]
  apu.py     - APU stub (register log, $4015 = 0, no IRQs)  [emu 3/6]
  pads.py    - $4016/$4017 controllers (strobe + shift), parse_buttons('A+RIGHT')  [emu 3/6]
  png.py     - zlib-only RGB PNG writer  [emu 3/6]
  nes.py     - NES(): wiring + run_frame()/run_frames()/dump_screen()  [emu 3/6];
               hooks on_exec(bank, addr, fn)/on_write/on_read/on_frame/set_tracer, Stop,
               nes.keys pad script, nes.call(addr, bank, a, x, y) JSR-style  [emu 4/6]
  state.py   - save/load states (gzip pickle of CPU/RAM/WRAM/mapper/PPU/APU/pads)  [emu 4/6]
  debug.py   - Tracer, dump_state, peek, disasm, resolve(label|BB:AAAA), parse_keys  [emu 4/6]
  __main__.py - python3 -m tools.emu --help (frames, keys, boot-test, trace, break, watch,
               run-func, dump-regs/ram/wram/screen, save/load-state, log-apu, bench)  [emu 4/6]
  shots.py, padprobe.py - debug: screens at frames with scripted presses; pad-var write probe
Tests: test_cpu.py [--rom], test_mapper.py, test_ppu.py [--rom], test_cli.py
(ROM cases need rom/re-c.nes). Import as `tools.emu` from the repo root (tools/ first on
sys.path makes tools/dis.py shadow the stdlib `dis`).
"""
import os
import sys

_TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if _TOOLS not in sys.path:
    # append, not insert: tools/dis.py must not shadow the stdlib `dis` (argparse -> inspect)
    sys.path.append(_TOOLS)
