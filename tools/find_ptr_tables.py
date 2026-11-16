#!/usr/bin/env python3
"""Pointer-table finder for re-c (session 32).

Two passes over the ROM, using the disasm_all.py tracer (via check_bank_refs.build):

1. Consumers (traced code): indexed loads LDA/LDX/LDY tbl,X|tbl,Y of ROM addresses.
     word   load tbl,R then (within 8 insns, same R) load tbl+1,R          -> interleaved .dw
     split  load T1,R -> STA/PHA, then load T2,R (T2 != T1+1) whose value goes to the
            next byte (zp k / k+1, abs k / k+1, or PHA hi-then-lo for RTS dispatch)  -> lo/hi
   For each, entries are counted from the base while they look like pointers (see below).
2. Orphans (scan): runs of >= 4 little-endian words in bytes that are not traced code, where
   every word lies in one CPU window ($8000/$A000/$C000/$E000 8KB) and >= 3 distinct values.
   Reported only if no consumer table covers them. Text banks 21/22 and the screen/metatile
   banks 0-9 are skipped (their bytes are not pointers).

Entry count for consumer tables: stop at a word outside $8000-$FFFF (except $0000 nulls,
allowed when followed by more valid words), at a traced-code byte, at the next labelled
address, or at another table's base. For split tables the lo/hi gap caps the count.

Element type: 'code' if >= half of the targets are traced instruction starts in the
consumer's bank context, else 'data'.

Output: docs/ptr_tables.txt. Usage: python3 tools/find_ptr_tables.py [--orphans N]
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nesrom
import disasm_all as DA
from check_bank_refs import build, fmt_ctx

OUT = os.path.join(nesrom.ROOT, 'docs', 'ptr_tables.txt')
LOADS = ('LDA', 'LDX', 'LDY')
SKIP_SCAN = set(range(0, 10)) | {21, 22}
MAXN = 256


def next_insns(d, b, a, n):
    out = []
    k = (b, a)
    for _ in range(n):
        if k not in d.insn:
            break
        sz, mn, md, val, ctx = d.insn[k]
        out.append((k, mn, md, val, ctx))
        if mn in ('RTS', 'RTI', 'JMP') or mn in DA.BRANCHES:
            break
        k = (k[0], k[1] + sz)
    return out


def dest_of(seq, i):
    """Where the value loaded by seq[i] goes: ('zp'|'abs', addr) / ('pha', n) / None."""
    reg = seq[i][1][2]          # LDA -> A, LDX -> X, LDY -> Y
    pushes = 0
    for (k, mn, md, val, ctx) in seq[i + 1:]:
        if mn == 'ST' + reg and md in ('zp', 'abs'):
            return ('mem', val)
        if reg == 'A' and mn == 'PHA':
            return ('pha', k)
        if reg == 'A' and mn in DA.A_WRITERS:
            return None
        if mn.startswith('LD' + reg) or mn in ('T' + reg + 'A',):
            continue
    return None


def consumers(d):
    word, split = {}, {}
    for (b, a), (sz, mn, md, val, ctx) in sorted(d.insn.items()):
        if mn not in LOADS or md not in ('abx', 'aby') or val < 0x8000:
            continue
        seq = next_insns(d, b, a, 10)
        reg = md[-1]
        d0 = dest_of(seq, 0)
        for j in range(1, len(seq)):
            k2, mn2, md2, v2, _ = seq[j]
            if mn2 not in LOADS or md2 != md:
                continue
            tb = d.resolve(val, ctx, (b, a))
            if tb is None:
                break
            d1 = dest_of(seq, j)
            if v2 == val + 1:
                if d0 and d1 and d0[0] == 'mem' and d1[0] == 'mem' and d1[1] != d0[1] + 1:
                    break
                word.setdefault((tb, val), set()).add((b, a, reg, ctx))
                break
            if d0 and d1 and ((d0[0] == 'mem' and d1[0] == 'mem' and d1[1] == d0[1] + 1)):
                split.setdefault((tb, val, d.resolve(v2, ctx, k2), v2), set()).add((b, a, reg, ctx))
                break
            if d0 and d1 and d0[0] == 'pha' and d1[0] == 'pha':
                # pushed first = hi byte (RTS dispatch pushes hi then lo)
                split.setdefault((d.resolve(v2, ctx, k2), v2, tb, val), set()).add((b, a, reg, ctx))
                break
    return word, split


def target_kind(d, tb_ctx, v):
    if v < 0x8000:
        return None
    tb = d.resolve(v, tb_ctx, None)
    if tb is None:
        return None
    return 'code' if (tb, v) in d.insn else 'data'


def stop_set(d):
    s = set()
    for (b, a), _ in d.labels.items():
        if b != '*':
            s.add((b, a))
    return s


def count_entries(d, b, base, step, hi_base=None, stops=(), ctx=(None, None)):
    """Walk entries; returns list of (index, value)."""
    out = []
    for i in range(MAXN):
        lo_a = base + (i * step if hi_base is None else i)
        hi_a = lo_a + 1 if hi_base is None else hi_base + i
        if not d.in_win(b, lo_a) or not d.in_win(b, hi_a):
            break
        if i and ((b, lo_a) in stops or (hi_base is None and (b, hi_a) in stops)):
            break
        if (b, lo_a) in d.code or (b, hi_a) in d.code:
            break
        if hi_base is not None and i and lo_a == hi_base:
            break
        v = d.byte(b, lo_a) | d.byte(b, hi_a) << 8
        if v == 0:
            out.append((i, v))
            continue
        if v < 0x8000:
            break
        out.append((i, v))
    while out and out[-1][1] == 0:
        out.pop()
    return out


def describe(d, ents, ctx):
    vals = [v for _, v in ents if v]
    kinds = [target_kind(d, ctx, v) for v in vals]
    code = sum(1 for k in kinds if k == 'code')
    el = 'code' if vals and code * 2 >= len(vals) else 'data'
    wins = sorted({v & 0xE000 for v in vals})
    rng = f'${min(vals):04X}-${max(vals):04X}' if vals else '-'
    nulls = sum(1 for _, v in ents if v == 0)
    return el, code, rng, wins, nulls


def orphan_scan(d, covered):
    found = []
    for b in range(d.nb):
        if b in SKIP_SCAN:
            continue
        w = DA.win(b)
        for par in (0, 1):
            a = w + par
            run = []
            while a + 1 < w + 0x2000:
                if (b, a) in d.code or (b, a + 1) in d.code or (b, a) in d.dead \
                        or (b, a) in covered:
                    v = None
                else:
                    v = d.byte(b, a) | d.byte(b, a + 1) << 8
                if run and v is not None and v >= 0x8000 and (v & 0xE000) == (run[0][1] & 0xE000):
                    run.append((a, v))
                elif v is not None and v >= 0x8000:
                    if len(run) >= 4:
                        found.append((b, run))
                    run = [(a, v)]
                else:
                    if len(run) >= 4:
                        found.append((b, run))
                    run = []
                a += 2
            if len(run) >= 4:
                found.append((b, run))
    out = []
    for b, run in found:
        vals = [v for _, v in run]
        if len(set(vals)) < 3:
            continue
        # skip runs whose own bytes look like ascending text/tiles: require targets not
        # all inside the run itself
        score = len(run)
        mono = sum(1 for x, y in zip(vals, vals[1:]) if y >= x) / (len(vals) - 1)
        on_label = sum(1 for v in vals if any((bb, v) in d.labels or (bb, v) in d.insn
                                              for bb in range(d.nb)))
        out.append((score, mono, on_label, b, run))
    # keep the better parity when two runs overlap
    out.sort(key=lambda r: (-r[0], r[3], r[4][0][0]))
    keep, used = [], set()
    for r in out:
        b, run = r[3], r[4]
        span = {(b, a + i) for a, _ in run for i in (0, 1)}
        if span & used:
            continue
        used |= span
        keep.append(r)
    return keep


def main(argv):
    n_orph = 60
    if '--orphans' in argv:
        n_orph = int(argv[argv.index('--orphans') + 1])
    d = build()
    stops = stop_set(d)
    word, split = consumers(d)
    jt = {(b, a) for b, a, *_ in DA.JUMP_TABLES}
    rows, covered = [], set()
    bases = {k for k in word} | {(k[0], k[1]) for k in split} | {(k[2], k[3]) for k in split}
    for (tb, base), cons in sorted(word.items()):
        ctx = sorted(cons, key=str)[0][3]
        ents = count_entries(d, tb, base, 2, stops=stops | (bases - {(tb, base)}), ctx=ctx)
        el, code, rng, wins, nulls = describe(d, ents, ctx)
        for i, _ in ents:
            covered |= {(tb, base + 2 * i), (tb, base + 2 * i + 1)}
        rows.append(('word', tb, base, None, len(ents), el, code, rng, nulls, cons, ctx))
    for (lb, lo, hb, hi), cons in sorted(split.items(), key=lambda kv: str(kv[0])):
        ctx = sorted(cons, key=str)[0][3]
        if lb is None or hb is None or (lb, lo - 1) in word or (hb, hi - 1) in word:
            continue          # second byte of an interleaved word table, not a lo/hi pair
        if lb != hb:
            rows.append(('split?', lb, lo, hi, 0, '?', 0, f'hi in bank {hb}', 0, cons, ctx))
            continue
        cap = abs(hi - lo) if hi != lo else MAXN
        ents = count_entries(d, lb, lo, 1, hi_base=hi, stops=stops - {(lb, lo), (lb, hi)}, ctx=ctx)
        ents = [e for e in ents if e[0] < cap]
        el, code, rng, wins, nulls = describe(d, ents, ctx)
        for i, _ in ents:
            covered |= {(lb, lo + i), (lb, hi + i)}
        rows.append(('split', lb, lo, hi, len(ents), el, code, rng, nulls, cons, ctx))

    out = ['# docs/ptr_tables.txt — tools/find_ptr_tables.py (session 32)', '',
           'kind  table          count  elem  code-hits  target range   nulls  label / consumers', '']
    ptr = [r for r in rows if r[4] >= 2]
    rec = [r for r in rows if r[4] < 2]
    out.append(f'## pointer tables found from consumers ({len(ptr)})')
    for kind, b, base, hi, n, el, code, rng, nulls, cons, ctx in ptr:
        lab = d.labels.get((b, base), ('',))[0]
        loc = f'{b:02}:{base:04X}' + (f'/{hi:04X}' if hi is not None else '')
        cs = ', '.join(sorted({f'{cb:02}:{ca:04X}' for cb, ca, _, _ in cons}))
        jtm = ' [JUMP_TABLES]' if (b, base) in jt else ''
        out.append(f'{kind:<6}{loc:<16}{n:>4}   {el:<5} {code:>4}      {rng:<14} {nulls:>3}   '
                   f'{lab or "(unlabelled)"}{jtm}  <- {cs}  ctx={fmt_ctx(ctx)}')
    out.append('')
    out.append(f'## word/byte-pair reads that are NOT pointer tables (<2 pointer-like entries: '
               f'records, coordinates, field pairs) ({len(rec)})')
    out.append('  ' + ', '.join(f'{b:02}:{base:04X}' + (f'/{hi:04X}' if hi is not None else '') +
                                (f' {d.labels[(b, base)][0]}' if (b, base) in d.labels else '')
                                for _, b, base, hi, *_ in rec))
    out.append('')
    orph = orphan_scan(d, covered | {(b, a + i) for (b, a), _ in d.tables.items() for i in (0, 1)})
    good = [r for r in orph if r[2] * 2 >= len(r[4]) and r[1] >= 0.6]
    out.append(f'## orphan word runs (no traced consumer; >=4 words in one window, >=50% of targets '
               f'on a label or traced insn, >=60% ascending) — {len(good)} of {len(orph)} raw runs; '
               f'top {n_orph} by length')
    for score, mono, on_label, b, run in good[:n_orph]:
        a0 = run[0][0]
        lab = d.labels.get((b, a0), ('',))[0]
        vals = [v for _, v in run]
        out.append(f'{b:02}:{a0:04X}  x{len(run):<3} asc {mono:4.2f}  on-code/label {on_label:>3}  '
                   f'${min(vals):04X}-${max(vals):04X}  {lab}')
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w') as f:
        f.write('\n'.join(out) + '\n')
    print(f'{len(rows)} consumer tables, {len(orph)} orphan runs -> {os.path.relpath(OUT, nesrom.ROOT)}')
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
