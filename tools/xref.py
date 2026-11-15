#!/usr/bin/env python3
"""Bank-aware cross-reference finder for the MMC3 PRG.

usage: xref.py <[BB:]addr> [--bank B] [--all] [--data] [--imm] [--loose]
  addr      CPU address (hex). Optional 'BB:' prefix names the bank that holds the target
            (needed for $8000-$BFFF targets; ignored for RAM/regs/fixed banks).
  --bank B  only scan code in 8KB bank B
  --all     for $8000-$BFFF targets, also scan banks outside the target's 16KB pair
            (default: target pair + fixed banks 30/31, which can always see the window)
  --data    also report raw little-endian words (pointer-table entries)
  --imm     also report split immediates: LDA/LDX/LDY #<lo ... #>hi within 8 bytes (either order)
  --loose   include hits whose instruction-stream sync check failed (likely data)

Method: pattern scan for every opcode whose mode can reference the target
(abs/abx/aby/ind for >=$100; zp/zpx/zpy/izx/izy + abs forms for <$100; rel branches
within the same bank). Each hit is sync-checked: some start point 1..24 bytes earlier
must linearly decode (no undefined opcodes) and land exactly on the hit. Hits that pass
are 'code', others 'data?' (hidden unless --loose).
"""
import sys
from instruction_set import OPCODES, SIZES, decode
from nesrom import (load_rom, load_labels, lookup, bank_window, parse_bank_addr,
                    FIXED, HDR, BANK_SIZE)

REF_MODES_ABS = ('abs', 'abx', 'aby', 'ind')
REF_MODES_ZP = ('zp', 'zpx', 'zpy', 'izx', 'izy')
OFFICIAL = {op for op, (mn, _) in OPCODES.items() if not mn.startswith('*')}


def synced(prg, bank, p):
    """True if a linear decode from some byte 1..24 before p lands exactly on p
    using only official opcodes (and p itself is official)."""
    base = bank * BANK_SIZE
    if prg[p] not in OFFICIAL:
        return False
    if p == base:
        return True
    for back in range(1, 25):
        s = p - back
        if s < base:
            break
        q = s
        ok = True
        while q < p:
            op = prg[q]
            if op not in OFFICIAL:
                ok = False; break
            q += SIZES[OPCODES[op][1]]
        if ok and q == p:
            return True
    return False


def scan_bank(prg, bank, target, want_zp, opts):
    base = bank * BANK_SIZE
    win = bank_window(bank)
    data = prg[base:base + BANK_SIZE]
    lo, hi = target & 0xFF, target >> 8
    hits = []
    for i in range(BANK_SIZE):
        op = data[i]
        if op not in OPCODES:
            continue
        mn, md = OPCODES[op]
        sz = SIZES[md]
        if i + sz > BANK_SIZE:
            continue
        pc = win + i
        kind = None
        if md in REF_MODES_ABS and data[i + 1] == lo and data[i + 2] == hi:
            kind = 'abs'
        elif want_zp and md in REF_MODES_ZP and data[i + 1] == lo:
            kind = 'zp'
        elif md == 'rel' and (target & 0xE000) == win:
            off = data[i + 1]
            off = off - 256 if off >= 128 else off
            if (pc + 2 + off) & 0xFFFF == target:
                kind = 'rel'
        if kind is None:
            continue
        ok = synced(prg, bank, base + i)
        if not ok and not opts.get('loose'):
            continue
        hits.append((pc, base + i, ok, sz))
    return hits


def scan_data(prg, bank, target):
    base = bank * BANK_SIZE
    win = bank_window(bank)
    w = bytes([target & 0xFF, target >> 8])
    out, s = [], base
    while True:
        j = prg.find(w, s, base + BANK_SIZE)
        if j < 0:
            return out
        out.append(win + (j - base))
        s = j + 1


def scan_imm(prg, bank, target):
    base = bank * BANK_SIZE
    win = bank_window(bank)
    lo, hi = target & 0xFF, target >> 8
    imm_ops = (0xA9, 0xA2, 0xA0)  # LDA/LDX/LDY #
    out = []
    for i in range(base, base + BANK_SIZE - 1):
        if prg[i] in imm_ops and prg[i + 1] == lo:
            for j in range(i + 2, min(i + 10, base + BANK_SIZE - 1)):
                if prg[j] in imm_ops and prg[j + 1] == hi:
                    out.append((win + i - base, win + j - base)); break
        if prg[i] in imm_ops and prg[i + 1] == hi and lo != hi:
            for j in range(i + 2, min(i + 10, base + BANK_SIZE - 1)):
                if prg[j] in imm_ops and prg[j + 1] == lo:
                    out.append((win + i - base, win + j - base)); break
    return out


def main(argv):
    args, opts, i = [], {'bank': None}, 0
    while i < len(argv):
        a = argv[i]
        if a == '--bank':
            opts['bank'] = int(argv[i + 1], 0); i += 2; continue
        if a.startswith('--'):
            opts[a[2:]] = True; i += 1; continue
        args.append(a); i += 1
    if not args:
        print(__doc__); return 1
    tbank, target = parse_bank_addr(args[0])
    rom, prg, _ = load_rom()
    labels = load_labels()
    nb = len(prg) // BANK_SIZE
    want_zp = target < 0x100

    # which banks can see the target?
    if opts['bank'] is not None:
        banks = [opts['bank']]
    elif 0x8000 <= target < 0xC000 and not opts.get('all'):
        if tbank is None:
            tbank = None
            banks = list(range(nb))  # unknown owner: scan all, note pairing in output
        else:
            pair = tbank & ~1
            banks = sorted({pair, pair + 1, 30, 31})
    else:
        banks = list(range(nb))
    if (target & 0xE000) in FIXED:
        tbank = FIXED[target & 0xE000]

    tl = lookup(labels, tbank if tbank is not None else 0, target)
    tname = f' ({tl[0]})' if tl and tl[0] else ''
    print(f'xrefs to {"%02d:" % tbank if isinstance(tbank, int) else ""}{target:04X}{tname}')
    total = 0
    for b in banks:
        for pc, poff, ok, sz in scan_bank(prg, b, target, want_zp, opts):
            sz_, mn, md, val, tgt = decode(prg, poff, pc)
            fn = lookup(labels, b, pc)
            tag = '' if ok else '  data?'
            print(f'  {b:02d}:{pc:04X}  file {HDR + poff:05X}  {prg[poff:poff+sz].hex(" "):<9} {mn} {md}{tag}')
            total += 1
        if opts.get('data'):
            for pc in scan_data(prg, b, target):
                print(f'  {b:02d}:{pc:04X}  word  .dw ${target:04X}')
                total += 1
        if opts.get('imm'):
            for a1, a2 in scan_imm(prg, b, target):
                print(f'  {b:02d}:{a1:04X}  imm-split (#lo/#hi pair, 2nd at {a2:04X})')
                total += 1
    print(f'-- {total} ref(s)')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
