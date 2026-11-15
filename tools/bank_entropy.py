#!/usr/bin/env python3
"""Per-bank Shannon entropy (bits/byte) for PRG 8KB banks, optionally per 1KB/256B block.

Compressed data typically sits at 7.5-8.0 bits/byte; raw tile maps / tables are far lower.

usage: bank_entropy.py [--block N] [banks...]
  --block N   also print entropy per N-byte block (e.g. 1024)
"""
import math
import sys
from collections import Counter

sys.path.insert(0, __import__("os").path.dirname(__file__))
from nesrom import load_rom  # noqa: E402


def entropy(data):
    if not data:
        return 0.0
    c = Counter(data)
    n = len(data)
    return -sum(v / n * math.log2(v / n) for v in c.values())


def main(argv):
    block = None
    banks = []
    i = 0
    while i < len(argv):
        if argv[i] == "--block":
            block = int(argv[i + 1], 0)
            i += 2
            continue
        banks.append(int(argv[i], 0))
        i += 1
    _, prg, _ = load_rom()
    if not banks:
        banks = list(range(len(prg) // 0x2000))
    for b in banks:
        d = prg[b * 0x2000:(b + 1) * 0x2000]
        ff = d.count(0xFF) * 100 // len(d)
        z = d.count(0x00) * 100 // len(d)
        print(f"bank {b:2d}: H={entropy(d):.2f}  00={z:2d}%  FF={ff:2d}%  distinct={len(set(d))}")
        if block:
            row = []
            for o in range(0, 0x2000, block):
                row.append(f"{entropy(d[o:o + block]):.1f}")
            print("          " + " ".join(row))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
