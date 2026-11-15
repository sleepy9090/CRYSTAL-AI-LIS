#!/usr/bin/env python3
"""Shared helpers: ROM loading, MMC3 8KB bank <-> file offset mapping, labels.csv loading.

Bank convention (labels.csv + all tools): 8KB PRG bank number 0..31.
  Bank 31 is fixed at $E000-$FFFF, bank 30 at $C000-$DFFF (PRG mode 0, the power-on default).
  Swappable banks are disassembled at whatever CPU window you pass (addr high bits pick the window).
RAM/registers use bank '*'.
"""
import csv, os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ROM_PATH = os.path.join(ROOT, 'rom', 're-c.nes')
LABELS_PATH = os.path.join(ROOT, 'labels.csv')
HDR = 16
BANK_SIZE = 0x2000
FIXED = {0xC000: 30, 0xE000: 31}


def load_rom(path=ROM_PATH):
    d = open(path, 'rb').read()
    prg_n = d[4] * 0x4000
    return d, d[HDR:HDR + prg_n], d[HDR + prg_n:HDR + prg_n + d[5] * 0x2000]


def nbanks(prg):
    return len(prg) // BANK_SIZE


def bank_file_off(bank, addr):
    return HDR + bank * BANK_SIZE + (addr & 0x1FFF)


def bank_window(bank):
    """Default CPU window for an 8KB bank: 30->$C000, 31->$E000, even->$8000, odd->$A000
    (the game maps 16KB pairs (2n,2n+1) at $8000-$BFFF via SwitchBank16)."""
    if bank in (30, 31):
        return 0xC000 if bank == 30 else 0xE000
    return 0x8000 if bank % 2 == 0 else 0xA000


def bank_cpu_addr(bank, prg_off_in_bank):
    return bank_window(bank) + (prg_off_in_bank & 0x1FFF)


def parse_bank_addr(s):
    """'BB:AAAA' -> (bank, addr); 'AAAA' -> (None, addr). Hex addr, decimal bank."""
    s = s.strip()
    if ':' in s:
        b, a = s.split(':', 1)
        return (parse_bank(b), int(a, 16))
    return (None, int(s, 16))


def parse_bank(s):
    s = str(s).strip()
    if s == '*':
        return '*'
    return int(s, 0) if s.startswith('0x') else int(s)


def load_labels(path=LABELS_PATH):
    """Returns dict keyed by (bank, addr) -> (name, comment). bank may be '*'."""
    out = {}
    if not os.path.exists(path):
        return out
    with open(path, newline='') as f:
        for row in csv.reader(f):
            if not row or row[0].startswith('#') or row[0] == 'bank':
                continue
            row += [''] * (4 - len(row))
            try:
                b = parse_bank(row[0])
                a = int(row[1], 16)
            except ValueError:
                continue
            out[(b, a)] = (row[2].strip(), row[3].strip())
    return out


def lookup(labels, bank, addr):
    """Find label for addr as seen from code in `bank`."""
    if addr < 0x8000:
        return labels.get(('*', addr))
    win = addr & 0xE000
    if win in FIXED:
        return labels.get((FIXED[win], addr)) or labels.get((bank, addr))
    return labels.get((bank, addr))
