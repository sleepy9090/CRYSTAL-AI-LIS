#!/usr/bin/env python3
"""state_json_peek.py <state.json> ADDR[+N] ... (session 134)

Print RAM ($0000-$07FF) / WRAM ($6000-$7FFF) bytes from a tools.emu.web_state JSON
(`ram` / `wram` hex strings), e.g. `0623 0560+32 6480+8`.
"""
import json
import sys


def main():
    if len(sys.argv) < 3:
        print(__doc__)
        sys.exit(1)
    with open(sys.argv[1]) as f:
        doc = json.load(f)
    ram, wram = bytes.fromhex(doc['ram']), bytes.fromhex(doc['wram'])
    for spec in sys.argv[2:]:
        a, _, n = spec.partition('+')
        a, n = int(a, 16), int(n or '1', 0)
        if a < 0x800:
            vals = ram[a:a + n]
        elif 0x6000 <= a < 0x8000:
            vals = wram[a - 0x6000:a - 0x6000 + n]
        else:
            print(f'${a:04X}: not RAM/WRAM')
            continue
        print(f'${a:04X}: ' + ' '.join(f'{v:02X}' for v in vals))


if __name__ == '__main__':
    main()
