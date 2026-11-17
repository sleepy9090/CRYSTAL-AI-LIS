"""List metasprite ids whose ObjMetaPtrTableA/B pointer (or FF redirect) leaves banks 28/29 ($8000-$BFFF).

usage: python3 tools/meta_ptr_check.py
ObjDrawMeta 28:822E reads the record through ($15),Y with no range check, so such an id would read RAM /
registers / the fixed banks. Prints each bad id with its pointer chain (session 54).
"""
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_rom  # noqa: E402

PRG = load_rom()[1]


def rd(addr):
    """byte at CPU addr with banks 28/29 at $8000-$BFFF and 30/31 fixed; None for RAM/IO."""
    if 0x8000 <= addr < 0xC000:
        return PRG[28 * 0x2000 + addr - 0x8000]
    if addr >= 0xC000:
        return PRG[30 * 0x2000 + addr - 0xC000]
    return None


def main():
    bad = 0
    for mid in range(256):
        tab = 0x855C if mid & 0x80 else 0x845C
        y = (mid << 1) & 0xFF
        ptr = rd(tab + y) | rd(tab + y + 1) << 8
        chain = [ptr]
        while True:
            if not 0x8000 <= ptr < 0xC000:
                break
            n = rd(ptr)
            if n < 0x80:
                break
            ptr = rd(ptr + 1) | rd(ptr + 2) << 8
            chain.append(ptr)
            if len(chain) > 4:
                break
        if not 0x8000 <= chain[-1] < 0xC000:
            bad += 1
            print(f'id ${mid:02X}: ' + ' -> '.join(f'${p:04X}' for p in chain))
    print(f'{bad} id(s) leave banks 28/29')


if __name__ == '__main__':
    main()
