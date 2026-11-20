#!/usr/bin/env python3
"""Static reachable-code tree from one entry point (session 98).

usage: call_tree.py <bank> <addr> [--ctx B8000,BA000] [--stop ADDR,..] [--flat]

Follows fall-through, branches, JSR and JMP abs (not JMP (ind)) from bank:addr.
$8000-$9FFF / $A000-$BFFF map to the --ctx banks (default: bank, bank+1 for an
even bank, or bank-1, bank for an odd one); $C000+ to the fixed banks 30/31.
Fixed-bank routines are listed but not followed (they are shared helpers) unless
--follow-fixed is given. --stop lists addresses not to descend into.
Prints every routine entry (JSR/JMP target) with its byte count, label and the
JMP (ind) / RTS-trick sites it could not follow.
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from nesrom import load_rom, load_labels, lookup
from instruction_set import decode, BRANCHES


def main():
    a = sys.argv[1:]
    if len(a) < 2:
        print(__doc__); sys.exit(1)
    bank = int(a[0]); addr = int(a[1], 16)
    lo, hi = (bank, bank + 1) if bank % 2 == 0 else (bank - 1, bank)
    if '--ctx' in a:
        lo, hi = [int(x) for x in a[a.index('--ctx') + 1].split(',')]
    stops = set()
    if '--stop' in a:
        stops = {int(x, 16) for x in a[a.index('--stop') + 1].split(',')}
    follow_fixed = '--follow-fixed' in a
    _, prg, _ = load_rom()
    labels = load_labels()

    def bank_of(pc):
        if pc >= 0xE000: return 31
        if pc >= 0xC000: return 30
        if pc >= 0xA000: return hi
        if pc >= 0x8000: return lo
        return None

    def byte_at(pc):
        b = bank_of(pc)
        return prg[b * 0x2000 + (pc & 0x1FFF)]

    seen = set()          # instruction addresses visited
    routines = {}         # entry -> set of instr addrs
    fixed_calls = {}      # fixed-bank target -> list of callers
    indirect = []
    work = [(addr, addr)]
    entries = [addr]
    while work:
        pc, ent = work.pop()
        while True:
            if pc in seen or bank_of(pc) is None:
                break
            seen.add(pc)
            buf = bytes(byte_at(pc + i) for i in range(3))
            sz, mn, md, val, tgt = decode(buf, 0, pc)
            routines.setdefault(ent, set()).add(pc)
            if mn in BRANCHES:
                work.append((tgt, ent))
            elif mn in ('JSR', 'JMP') and md == 'abs':
                if tgt >= 0xC000 and not follow_fixed:
                    fixed_calls.setdefault(tgt, []).append(pc)
                elif tgt in stops:
                    fixed_calls.setdefault(tgt, []).append(pc)
                elif tgt not in routines and tgt not in [e for e in entries]:
                    entries.append(tgt)
                    work.append((tgt, tgt))
                if mn == 'JMP':
                    break
            elif mn == 'JMP':
                indirect.append(pc)
                break
            elif mn in ('RTS', 'RTI', 'BRK') or mn == '.db':
                if mn == '.db':
                    indirect.append(pc)
                break
            pc += sz
    total = 0
    print(f'entry {bank}:{addr:04X}  ctx {lo}/{hi}')
    for e in sorted(routines):
        ins = routines[e]
        n = 0
        for pc in ins:
            buf = bytes(byte_at(pc + i) for i in range(3))
            n += decode(buf, 0, pc)[0]
        total += n
        b = bank_of(e)
        print(f'  {b:2d}:{e:04X} {len(ins):4d} ins {n:5d} B  {lookup(labels, b, e) or ""}')
    print(f'total {total} bytes in {len(routines)} entries')
    print('fixed/stopped calls:')
    for t in sorted(fixed_calls):
        b = bank_of(t)
        print(f'  {b:2d}:{t:04X} x{len(fixed_calls[t])}  {lookup(labels, b, t) or ""}')
    if indirect:
        print('unfollowed (JMP ind / bad opcode):', ' '.join(f'{p:04X}' for p in indirect))


if __name__ == '__main__':
    main()
