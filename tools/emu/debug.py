"""Debug helpers for the re-c emulator (Ph5.5 emu 4/6): tracer, register/stack/ZP dumps,
labels.csv lookups and --keys scripts.

Trace line (one per instruction, before it executes):
    FRAME LINE,DOT BB:AAAA  RAW       TEXT                         A:00 X:00 Y:00 P:nvUbdIzc S:FD
BB = 8KB PRG bank mapped at AAAA ('**' for RAM/WRAM code). TEXT uses the dis.py decoder
(instruction_set.decode/fmt_operand) with labels.csv names and hardware register names; a
labelled address gets its own 'Label:' line first. Operand bytes are read with peek() (no
register side effects).
"""
from . import _TOOLS  # noqa: F401
from instruction_set import decode, fmt_operand, HW_REGS
from nesrom import load_labels, lookup, parse_bank_addr

from .pads import parse_buttons

_LABELS = None


def labels():
    global _LABELS
    if _LABELS is None:
        _LABELS = load_labels()
    return _LABELS


def resolve(spec):
    """'BB:AAAA' / 'AAAA' / label name -> (bank|None, addr). Fixed-bank addresses get bank 30/31."""
    try:
        bank, addr = parse_bank_addr(spec)
    except ValueError:
        hits = [(b, a) for (b, a), (n, _) in labels().items() if n == spec]
        if not hits:
            raise SystemExit(f'unknown label or address: {spec}')
        if len(hits) > 1:
            raise SystemExit(f'ambiguous label {spec}: ' + ', '.join(f'{b}:{a:04X}' for b, a in hits))
        bank, addr = hits[0]
        if bank == '*':
            bank = None
    if bank is None and addr >= 0xC000:
        bank = 30 if addr < 0xE000 else 31
    return bank, addr


def parse_range(spec):
    """'BB:AAAA-AAAA' or 'AAAA-AAAA' -> (bank|None, lo, hi) inclusive."""
    lo_s, hi_s = spec.split('-', 1)
    bank, lo = parse_bank_addr(lo_s)
    return bank, lo, int(hi_s, 16)


def peek(nes, a):
    """Side-effect-free CPU-space read (registers/open bus read as 0)."""
    a &= 0xFFFF
    if a < 0x2000:
        return nes.bus.ram[a & 0x7FF]
    if 0x6000 <= a < 0x8000:
        return nes.bus.wram[a & 0x1FFF]
    if a >= 0x8000:
        return nes.mapper.read(a)
    return 0


def bank_of(nes, pc):
    return nes.mapper.prg_bank_at(pc) if pc >= 0x8000 else None


def pflags(p):
    return ''.join(c.upper() if p & (0x80 >> i) else c for i, c in enumerate('nvubdizc'))


def disasm(nes, pc):
    """(bank, raw_hex, text) for the instruction at pc."""
    buf = bytes(peek(nes, pc + i) for i in range(3))
    sz, mn, md, val, tgt = decode(buf, 0, pc)
    bank = bank_of(nes, pc)
    name = None
    if tgt is not None:
        if tgt in HW_REGS and md in ('abs', 'abx', 'aby'):
            name = HW_REGS[tgt]
        lab = lookup(labels(), bank if bank is not None else 31, tgt)
        if lab and lab[0]:
            name = lab[0]
    text = f'.db ${val:02X}' if mn == '.db' else f'{mn} {fmt_operand(md, val, name)}'.rstrip()
    return bank, buf[:sz].hex(' '), text


def label_at(nes, pc):
    bank = bank_of(nes, pc)
    lab = lookup(labels(), bank if bank is not None else 31, pc)
    return lab[0] if lab and lab[0] else None


def regs_line(nes):
    c = nes.cpu
    return f'A:{c.a:02X} X:{c.x:02X} Y:{c.y:02X} P:{pflags(c.p)} S:{c.s:02X}'


def where(nes):
    pc = nes.cpu.pc
    b = bank_of(nes, pc)
    return f'{"**" if b is None else f"{b:02d}"}:{pc:04X}'


class Tracer:
    """cpu.trace callable writing trace lines to `out` (file object)."""

    def __init__(self, nes, out, ranges=None, frames=None, max_lines=None):
        self.nes, self.out = nes, out
        self.ranges = ranges or []          # [(bank|None, lo, hi)]
        self.frames = frames                # (first, last) inclusive or None
        self.max_lines = max_lines
        self.lines = 0

    def __call__(self, cpu):
        nes = self.nes
        ppu = nes.ppu
        if self.frames and not (self.frames[0] <= ppu.frame_no <= self.frames[1]):
            return
        pc = cpu.pc
        if self.ranges:
            b = bank_of(nes, pc)
            for rb, lo, hi in self.ranges:
                if lo <= pc <= hi and (rb is None or rb == b):
                    break
            else:
                return
        if self.max_lines is not None and self.lines >= self.max_lines:
            return
        lab = label_at(nes, pc)
        if lab:
            self.out.write(f'{lab}:\n')
        bank, raw, text = disasm(nes, pc)
        bs = '**' if bank is None else f'{bank:02d}'
        self.out.write(f'{ppu.frame_no:5d} {ppu.line:3d},{ppu.dot:3d} {bs}:{pc:04X}  {raw:<9} '
                       f'{text:<28} {regs_line(nes)}\n')
        self.lines += 1


def hexdump(get, base, n, width=16):
    rows = []
    for r in range(0, n, width):
        rows.append(f'  {base + r:04X}: ' + ' '.join(f'{get(base + r + i):02X}' for i in range(width)))
    return '\n'.join(rows)


def dump_state(nes, zp=True, stack=True):
    """Registers + position + banks (+ stack, + zero page) as text."""
    cpu, ppu, m = nes.cpu, nes.ppu, nes.mapper
    pc = cpu.pc
    lab = label_at(nes, pc)
    bank, raw, text = disasm(nes, pc)
    out = [f'PC={where(nes)}{" (" + lab + ")" if lab else ""}  {raw:<9} {text}',
           f'{regs_line(nes)}  cycles={cpu.cycles}',
           f'frame={ppu.frame_no} line={ppu.line} dot={ppu.dot} nmi={ppu.nmi_count} '
           f'state($40)=${nes.bus.ram[0x40]:02X}',
           f'prg={m.prg_banks} chr={m.chr_banks} ctrl=${ppu.ctrl:02X} mask=${ppu.mask:02X} '
           f'irq(latch={m.irq_latch} ctr={m.irq_counter} en={int(m.irq_enabled)})']
    if stack:
        sp = cpu.s
        items = [f'{nes.bus.ram[0x100 + a]:02X}' for a in range(sp + 1, 0x100)][:24]
        out.append(f'stack ($01{sp + 1 & 0xFF:02X}..): ' + ' '.join(items))
    if zp:
        out.append('zero page:')
        out.append(hexdump(lambda a: nes.bus.ram[a], 0, 0x100))
    return '\n'.join(out)


def parse_keys(specs):
    """['120:START', '300:A+RIGHT:8', '@file'] -> {frame: mask}. Default hold = 2 frames.
    A file holds one spec per line (# comments, blank lines ok)."""
    flat = []
    for s in specs:
        if s.startswith('@'):
            with open(s[1:]) as f:
                for ln in f:
                    ln = ln.split('#', 1)[0].strip()
                    if ln:
                        flat.extend(ln.split())
        else:
            flat.extend(s.split())
    keys = {}
    for s in flat:
        p = s.split(':')
        start, mask = int(p[0]), parse_buttons(p[1])
        n = int(p[2]) if len(p) > 2 else 2
        for f in range(start, start + n):
            keys[f] = keys.get(f, 0) | mask
    return keys
