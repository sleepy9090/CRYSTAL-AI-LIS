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
  nes.py     - NES(): wiring + run_frame()/run_frames()/dump_screen()  [emu 3/6]
  __main__.py - python3 -m tools.emu --frames N --dump-screen F.png [--scale S] [--log-apu [F]]
  shots.py, padprobe.py - debug: screens at frames with scripted presses; pad-var write probe
Tests: test_cpu.py [--rom], test_mapper.py, test_ppu.py [--rom] (ROM cases need rom/re-c.nes).
"""
import os
import sys

_TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if _TOOLS not in sys.path:
    # append, not insert: tools/dis.py must not shadow the stdlib `dis` (argparse -> inspect)
    sys.path.append(_TOOLS)
