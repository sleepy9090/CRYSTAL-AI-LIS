#!/usr/bin/env python3
"""List code sites that touch the story-flag bytes $6480-$657F directly.

usage: flag_code_sites.py [--lo 6480] [--hi 657F] [--all] [--out FILE]
  --all  keep hits whose next 3 instructions include .db/illegal opcodes
         (default drops them: they are almost always ASCII text in banks 0-23)

Scans every 8KB bank for abs / abs,X / abs,Y operands in the range (sync-checked
with xref.synced, so data hits are dropped). For each hit prints the instruction
and the next 3 instructions; when an immediate mask is used with the access
(LDA/ORA #m/STA, LDA/AND #m, or ORA/AND #m before/after) the flag numbers
F### = (byte-$6480)*8 + bit are derived.
"""
import sys
from instruction_set import decode
from nesrom import load_rom, load_labels, lookup, bank_window, BANK_SIZE
from xref import synced

FLAG_BASE = 0x6480


def fmt(mn, md, val):
    if md == 'imm':
        return f'{mn} #${val:02X}'
    if md in ('abs', 'abx', 'aby'):
        suf = {'abs': '', 'abx': ',X', 'aby': ',Y'}[md]
        return f'{mn} ${val:04X}{suf}'
    if md in ('zp', 'zpx', 'zpy'):
        return f'{mn} ${val:02X}'
    if val is None:
        return mn
    return f'{mn} ${val:X}'


def flags_of(byte_addr, mask):
    n = byte_addr - FLAG_BASE
    return [f'F{n * 8 + b:03X}' for b in range(8) if mask & (1 << b)]


def main(argv):
    lo, hi, out, keep_all = 0x6480, 0x657F, None, False
    i = 0
    while i < len(argv):
        if argv[i] == '--all':
            keep_all = True; i += 1
        elif argv[i] == '--lo':
            lo = int(argv[i + 1], 16); i += 2
        elif argv[i] == '--hi':
            hi = int(argv[i + 1], 16); i += 2
        elif argv[i] == '--out':
            out = argv[i + 1]; i += 2
        else:
            print(__doc__); return 1
    _, prg, _ = load_rom()
    lines = []
    for bank in range(32):
        base = bank * BANK_SIZE
        win = bank_window(bank)
        data = prg[base:base + BANK_SIZE]
        for p in range(BANK_SIZE - 2):
            sz, mn, md, val, _ = decode(data, p, win + p)
            if md not in ('abs', 'abx', 'aby') or not (lo <= val <= hi):
                continue
            if not synced(prg, bank, base + p):
                continue
            # context: previous instruction (best effort: 2-byte imm before) + next 3
            prev = ''
            if p >= 2:
                psz, pmn, pmd, pval, _ = decode(data, p - 2, win + p - 2)
                if pmd == 'imm' and psz == 2:
                    prev = fmt(pmn, pmd, pval)
            nxt = []
            q = p + sz
            for _ in range(3):
                if q >= BANK_SIZE:
                    break
                nsz, nmn, nmd, nval, _ = decode(data, q, win + q)
                nxt.append((nmn, nmd, nval))
                q += nsz
            # derive flags from a mask immediate adjacent to the access
            mask, kind = None, ''
            if md == 'abs':
                if mn in ('ORA', 'AND', 'BIT', 'EOR') and prev.startswith('LDA #'):
                    mask = int(prev[6:], 16)
                    kind = {'ORA': 'set?', 'AND': 'test', 'BIT': 'test', 'EOR': 'tog?'}[mn]
                elif mn == 'LDA' and nxt and nxt[0][1] == 'imm' and nxt[0][0] in ('ORA', 'AND', 'EOR'):
                    m = nxt[0][2]
                    if nxt[0][0] == 'ORA':
                        mask, kind = m, 'set'
                    elif nxt[0][0] == 'AND':
                        # AND #inv then STA = clear; AND #m then branch = test
                        if len(nxt) > 1 and nxt[1][0] == 'STA' and nxt[1][2] == val:
                            mask, kind = (~m) & 0xFF, 'clr'
                        else:
                            mask, kind = m, 'test'
                    else:
                        mask, kind = m, 'toggle'
                elif mn in ('STA', 'STX', 'STY') and prev.endswith('#$00'):
                    mask, kind = 0xFF, 'clr-byte'
                elif mn in ('INC', 'DEC'):
                    kind = 'count'
            # text/data filter: the next 3 instructions must be official opcodes
            if not keep_all and any(a == '.db' or a.startswith('*') for a, _, _ in nxt):
                continue
            fl = ' '.join(flags_of(val, mask)) if mask else ''
            ctx = '; '.join(fmt(a, b, c) for a, b, c in nxt)
            byte_desc = f'${val:04X}'
            lines.append(f'{bank:02d}:{win + p:04X}  {(prev + " / ") if prev else ""}{fmt(mn, md, val):<14} '
                         f'| {kind:<8} {fl:<20} | next: {ctx}')
    text = '\n'.join(lines) + f'\n# {len(lines)} sites in ${lo:04X}-${hi:04X}\n'
    if out:
        with open(out, 'w') as f:
            f.write(text)
    sys.stdout.write(text)
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
