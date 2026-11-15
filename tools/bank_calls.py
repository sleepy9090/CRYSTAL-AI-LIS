#!/usr/bin/env python3
"""Map every `LDA #n / JSR SwitchBank16` site in the fixed banks to n and the
$8000-$BFFF targets it reaches (JSR/JMP abs, JMP (ind)) in the next instructions.

usage: bank_calls.py [--window N] [--summary]
  --window N   instructions to follow after the JSR (default 12; stops at RTS/RTI/JMP)
  --summary    group by 16KB bank index n -> 8KB banks (2n, 2n+1)
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_rom, bank_window, load_labels, lookup
from instruction_set import OPCODES, SIZES as MODE_SIZES

SB16 = 0xC40E


def main():
    args = sys.argv[1:]
    win = 12
    if '--window' in args:
        win = int(args[args.index('--window') + 1])
    summary = '--summary' in args
    _, prg, _ = load_rom()
    labels = load_labels()
    sites = []
    for bank in (30, 31):
        base = bank * 0x2000
        data = prg[base:base + 0x2000]
        org = bank_window(bank)
        for i in range(len(data) - 4):
            if data[i] == 0xA9 and data[i + 2] == 0x20 and data[i + 3] == 0x0E and data[i + 4] == 0xC4:
                n = data[i + 1]
                pc = i + 5
                targets = []
                for _ in range(win):
                    if pc + 2 >= len(data):
                        break
                    op = data[pc]
                    if op in (0x20, 0x4C, 0x6C):
                        t = data[pc + 1] | data[pc + 2] << 8
                        kind = {0x20: 'JSR', 0x4C: 'JMP', 0x6C: 'JMP()'}[op]
                        if 0x8000 <= t < 0xC000 or op == 0x6C:
                            targets.append(f'{kind} ${t:04X}')
                        elif op == 0x20:
                            lb = lookup(labels, bank, t)
                            targets.append(f'jsr {lb[0] if lb else "$%04X" % t}')
                        if op != 0x20:
                            break
                        pc += 3
                        continue
                    if op in (0x60, 0x40):
                        break
                    sz = SIZES.get(op, 1)
                    pc += sz
                sites.append((n, f'{bank}:{org + i:04X}', targets))
    if summary:
        by = {}
        for n, at, t in sites:
            by.setdefault(n, []).append((at, t))
        for n in sorted(by):
            print(f'n={n:2d} (banks {2*n:2d}/{2*n+1:2d}):')
            for at, t in by[n]:
                print(f'    {at}  ' + ', '.join(t))
    else:
        for n, at, t in sites:
            print(f'{at}  n={n:2d} ({2*n}/{2*n+1})  ' + ', '.join(t))
    print(f'-- {len(sites)} site(s)')


# opcode byte -> instruction size, from the shared opcode DB
SIZES = {op: MODE_SIZES.get(md, 1) for op, (_mn, md) in OPCODES.items()}

if __name__ == '__main__':
    main()
