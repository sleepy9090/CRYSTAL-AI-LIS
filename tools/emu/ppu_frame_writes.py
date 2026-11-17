"""List the PPU/MMC3 writes recorded in a web/test/ppu_frames/*.json snapshot (debug aid for ppu_check).

    python3 tools/emu/ppu_frame_writes.py web/test/ppu_frames/name_entry.json [--lines 0-5,261]
"""
import argparse
import json


def parse_lines(s):
    out = set()
    for part in s.split(','):
        if '-' in part:
            a, b = part.split('-')
            out.update(range(int(a), int(b) + 1))
        else:
            out.add(int(part))
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument('json')
    ap.add_argument('--lines')
    a = ap.parse_args()
    d = json.load(open(a.json))
    want = parse_lines(a.lines) if a.lines else None
    print('midline:', d.get('midline'))
    for line, dot, kind, addr, v in d.get('writes', []):
        if want is None or line in want:
            print(f'line {line:3d} dot {dot:3d} {kind:4s} ${addr:04X} = ${v:02X}')
    for i, (v, x, c, m, b) in enumerate(d['lines'][:3]):
        print(f'rec line {i}: v={v:04x} x={x} ctrl={c:02x} mask={m:02x} chr={b}')


if __name__ == '__main__':
    main()
