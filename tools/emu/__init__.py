"""re-c pure-Python NES emulator (Ph5.5). No third-party emulators.

Modules:
  loader.py  - iNES loader (wraps tools/nesrom.py)            [emu 1/6]
  memory.py  - CPU bus: 2KB RAM mirror, 8KB WRAM, register dispatch, OAM DMA  [emu 1/6]
  cpu.py     - 2A03 core: official + instruction_set illegal opcodes, exact cycles, NMI/IRQ/BRK  [emu 1/6]
  mapper.py  - MMC3 (R0-R7, PRG/CHR modes, mirroring, WRAM gate, scanline IRQ + A12 filter)  [emu 2/6]
Tests: test_cpu.py [--rom], test_mapper.py (both need rom/re-c.nes for the ROM cases).
"""
import os
import sys

_TOOLS = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
if _TOOLS not in sys.path:
    sys.path.insert(0, _TOOLS)
