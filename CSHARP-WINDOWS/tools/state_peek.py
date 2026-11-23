"""Print fields / RAM bytes of a tools/emu/web_state.py state JSON (../web/test/states/*.json).

usage: python3 tools/state_peek.py STATE.json [ADDR[,ADDR..]]   (ADDR hex; $0000-$07FF RAM, $6000-$7FFF WRAM)
Without ADDR: frame / frame0 / line / until / ppu / mapper + irq_mode $51, $56, $57, $58, ppumask $01.
"""
import json
import sys


def byte(s, a):
    if a < 0x800:
        h = s['ram']
    elif 0x6000 <= a < 0x8000:
        h, a = s['wram'], a - 0x6000
    else:
        sys.exit(f'${a:04X}: not RAM/WRAM')
    return int(h[a * 2:a * 2 + 2], 16)


def main():
    if len(sys.argv) < 2:
        sys.exit(__doc__)
    s = json.load(open(sys.argv[1]))
    if len(sys.argv) > 2:
        addrs = [int(x, 16) for x in sys.argv[2].split(',')]
    else:
        print({k: s.get(k) for k in ('frame', 'frame0', 'line', 'until', 'src', 'ppu', 'mapper')})
        print('other keys:', sorted(k for k in s if k not in ('frame', 'frame0', 'line', 'until', 'src', 'ppu',
                                                               'mapper', 'ram', 'wram', 'ciram', 'pal', 'oam')))
        addrs = [0x01, 0x40, 0x41, 0x51, 0x56, 0x57, 0x58, 0x6C]
    print(' '.join(f'${a:04X}={byte(s, a):02X}' for a in addrs))


if __name__ == '__main__':
    main()
