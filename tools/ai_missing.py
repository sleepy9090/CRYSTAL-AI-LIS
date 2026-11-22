#!/usr/bin/env python3
"""List ObjAiTable ids (gfx/obj_ai.txt) that the web port has no handler for.

Reads the `0xNN:` keys of every exported AI map in web/src/game/obj/*.js (the maps
merged into objects.js AI_TABLE) and prints the ids from gfx/obj_ai.txt not present,
with their ROM address and the object types that use them.

usage: python3 tools/ai_missing.py
"""
import glob
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def ported_ids():
    ids = {}
    for path in sorted(glob.glob(os.path.join(ROOT, 'web/src/game/obj/*.js'))):
        src = open(path).read()
        # only inside `export const AI_* = { ... };` blocks
        for m in re.finditer(r'export const (AI_\w+)\s*=\s*\{(.*?)\};', src, re.S):
            for k in re.finditer(r'(?<![\w$])0x([0-9A-Fa-f]{2})\s*:', m.group(2)):
                ids.setdefault(int(k.group(1), 16), f'{os.path.basename(path)}:{m.group(1)}')
    return ids


def rom_ids():
    out = []
    for line in open(os.path.join(ROOT, 'gfx/obj_ai.txt')):
        m = re.match(r'ai \$([0-9A-F]{2}) -> (\S+)\s+types ?(.*)', line.strip())
        if m:
            out.append((int(m.group(1), 16), m.group(2), m.group(3).strip()))
    return out


def main():
    have = ported_ids()
    rom = rom_ids()
    missing = [r for r in rom if r[0] not in have]
    for i, addr, types in missing:
        print(f'ai ${i:02X} -> {addr}  types {types or "(none)"}')
    print(f'; {len(rom) - len(missing)}/{len(rom)} ported, {len(missing)} missing')


if __name__ == '__main__':
    main()
