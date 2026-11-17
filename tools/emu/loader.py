"""iNES loader for the emulator. Reuses nesrom.load_rom (header + PRG/CHR split)."""
from . import _TOOLS  # noqa: F401  (puts tools/ on sys.path)
import nesrom


class Cartridge:
    def __init__(self, path=nesrom.ROM_PATH):
        raw, prg, chr_ = nesrom.load_rom(path)
        if raw[:4] != b'NES\x1a':
            raise ValueError(f'{path}: not an iNES file')
        self.path = path
        self.prg = bytes(prg)
        self.chr = bytes(chr_)
        self.mapper = (raw[6] >> 4) | (raw[7] & 0xF0)
        self.battery = bool(raw[6] & 2)
        self.vertical_mirroring = bool(raw[6] & 1)
        self.four_screen = bool(raw[6] & 8)
        self.prg_banks8 = len(self.prg) // 0x2000
        self.chr_banks1k = len(self.chr) // 0x400

    def prg_bank(self, n):
        """8KB PRG bank n (negative = from end, e.g. -1 = last)."""
        n %= self.prg_banks8
        return memoryview(self.prg)[n * 0x2000:(n + 1) * 0x2000]

    def __repr__(self):
        return (f'Cartridge(mapper={self.mapper}, prg={len(self.prg)//1024}KB, chr={len(self.chr)//1024}KB, '
                f'battery={self.battery})')


def load(path=nesrom.ROM_PATH):
    return Cartridge(path)
