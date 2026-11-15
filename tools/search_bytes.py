#!/usr/bin/env python3
"""Bank-aware byte-pattern search over PRG (or CHR) of rom/re-c.nes.

usage: search_bytes.py <hex> [--context N] [--disasm] [--bank B] [--chr] [--max M]
  hex        pattern, spaces optional; '??' = wildcard byte (e.g. "A9 ?? 20 0E C4")
  --context N  show N bytes of hex before/after each hit (default 0)
  --disasm     disassemble a few instructions starting at each hit
  --bank B     restrict to 8KB PRG bank B (0-31)
  --chr        search CHR ROM instead (reports 1KB CHR bank + offset)
  --max M      stop after M hits (default 500)
Hits are reported as BB:AAAA using the default CPU window of the bank
(30=$C000, 31=$E000, even=$8000, odd=$A000) plus the file offset.
Matches crossing an 8KB bank boundary are skipped (they are not contiguous in CPU space).
"""
import sys
from instruction_set import decode, fmt_operand
from nesrom import load_rom, load_labels, lookup, bank_cpu_addr, HDR, BANK_SIZE


def parse_pattern(s):
    s = s.replace(' ', '').replace(',', '')
    if len(s) % 2:
        raise SystemExit('pattern must have an even number of hex digits')
    pat = []
    for i in range(0, len(s), 2):
        t = s[i:i + 2]
        pat.append(None if t == '??' else int(t, 16))
    return pat


def find_all(buf, pat):
    # anchor on first concrete byte for speed
    anchor = next((i for i, b in enumerate(pat) if b is not None), None)
    if anchor is None:
        raise SystemExit('pattern has no concrete bytes')
    ab = bytes([pat[anchor]])
    start = 0
    n = len(pat)
    while True:
        j = buf.find(ab, start)
        if j < 0:
            return
        p = j - anchor
        start = j + 1
        if p < 0 or p + n > len(buf):
            continue
        if all(b is None or buf[p + k] == b for k, b in enumerate(pat)):
            yield p


def disasm_lines(rom, bank, addr, count=4):
    labels = load_labels()
    out = []
    pc = addr
    for _ in range(count):
        if (pc & 0x1FFF) >= 0x2000 or pc - addr > 0x40:
            break
        off = HDR + bank * BANK_SIZE + (pc & 0x1FFF)
        sz, mn, md, val, tgt = decode(rom, off, pc)
        name = None
        if tgt is not None:
            tl = lookup(labels, bank, tgt)
            if tl and tl[0]:
                name = tl[0]
        text = f'.db ${val:02X}' if mn == '.db' else f'{mn} {fmt_operand(md, val, name)}'.rstrip()
        out.append(f'      {pc:04X}: {rom[off:off+sz].hex(" "):<9} {text}')
        pc += sz
    return out


def main(argv):
    args = []
    opts = {'context': 0, 'bank': None, 'max': 500}
    i = 0
    while i < len(argv):
        a = argv[i]
        if a in ('--context', '--bank', '--max'):
            opts[a[2:]] = int(argv[i + 1], 0); i += 2; continue
        if a.startswith('--'):
            opts[a[2:]] = True; i += 1; continue
        args.append(a); i += 1
    if not args:
        print(__doc__); return 1
    pat = parse_pattern(''.join(args))
    rom, prg, chr_ = load_rom()
    ctx = opts['context']
    hits = 0
    if opts.get('chr'):
        base = HDR + len(prg)
        for p in find_all(chr_, pat):
            print(f'CHR 1KB bank {p // 0x400:3d} +{p % 0x400:03X}  file {base + p:05X}  ppu ${p % 0x2000:04X}(8K)')
            hits += 1
            if hits >= opts['max']:
                break
        print(f'-- {hits} hit(s)')
        return 0
    for p in find_all(prg, pat):
        bank = p // BANK_SIZE
        if (p + len(pat) - 1) // BANK_SIZE != bank:
            continue
        if opts['bank'] is not None and bank != opts['bank']:
            continue
        addr = bank_cpu_addr(bank, p)
        line = f'{bank:02d}:{addr:04X}  file {HDR + p:05X}'
        if ctx:
            lo = max(bank * BANK_SIZE, p - ctx)
            hi = min((bank + 1) * BANK_SIZE, p + len(pat) + ctx)
            line += f'  {prg[lo:p].hex(" ")} [{prg[p:p+len(pat)].hex(" ")}] {prg[p+len(pat):hi].hex(" ")}'
        print(line)
        if opts.get('disasm'):
            print('\n'.join(disasm_lines(rom, bank, addr)))
        hits += 1
        if hits >= opts['max']:
            print('-- max hits reached')
            break
    print(f'-- {hits} hit(s)')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
