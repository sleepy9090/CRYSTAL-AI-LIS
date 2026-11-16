#!/usr/bin/env python3
"""Bank-reference checker for re-c (session 32).

Runs the disasm_all.py recursive-descent tracer, then checks every cross-window reference
that the traced code makes:

  calls   JSR/JMP abs to $8000-$FFFF: the target bank (from the tracer's (r6, r7) context)
          is mapped at that window and the target is the first byte of a traced instruction.
  tables  every .dw entry of disasm_all.JUMP_TABLES and the CPU vectors: same check.
  banks   every JSR/JMP to SwitchBank16 / SetR6 / SetR7: the A value (LDA #imm found by
          walking back the straight-line code) is a valid bank (SwitchBank16: n <= 14 so
          the pair stays inside banks 0-29; SetR6/SetR7: 0-29). Dynamic A is listed with
          the instruction that sets it.
  data    abs / abs,X / abs,Y data operands into $8000-$FFFF: target bank resolved, and the
          target does not land in the middle of a traced instruction (a read of code bytes
          is reported, it may be deliberate).

Output: docs/bank_refs.txt (summary + every non-OK row). Exit 1 if a call/table check fails.
Usage: python3 tools/check_bank_refs.py [--all]   (--all also lists OK rows)
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nesrom
import disasm_all as DA

OUT = os.path.join(nesrom.ROOT, 'docs', 'bank_refs.txt')
BANK_FUNCS = {DA.SWITCH16: 'SwitchBank16', DA.SETR6: 'SetR6', DA.SETR7: 'SetR7'}
DATA_MODES = ('abs', 'abx', 'aby')


def build():
    """Run the tracer; returns the Dis object with insn/code/tables filled."""
    d = DA.Dis()
    d.table_counts = {}
    d.entry_tables = []
    d.seed()
    d.run()
    d.mark_data_labels()
    # per-bank ordered instruction list + "previous contiguous instruction" map
    d.prev = {}
    by_bank = {}
    for (b, a), v in d.insn.items():
        by_bank.setdefault(b, []).append((a, v[0]))
    for b, lst in by_bank.items():
        lst.sort()
        ends = {a + sz: a for a, sz in lst}
        for a, _ in lst:
            if a in ends:
                d.prev[(b, a)] = (b, ends[a])
    return d


def name(d, b, a):
    if b is None:
        return f'${a:04X}'
    n = d.name_of(b, a)
    return f'{b:02}:{a:04X}' + (f' {n}' if n else '')


def target_state(d, b, a):
    if b is None:
        return 'UNRESOLVED'
    if not d.in_win(b, a):
        return 'OUTWIN'
    if (b, a) in d.insn:
        return 'OK'
    if (b, a) in d.code:
        return 'MID-INSN'
    if (b, a) in d.data:
        return 'DATA'
    return 'UNTRACED'


def a_source(d, b, a):
    """Walk back straight-line code from the call at (b, a) to the last A writer.
    Returns (value or None, description)."""
    k = (b, a)
    joined = False
    for _ in range(8):
        if k in d.auto and k != (b, a):
            joined = True             # another path enters here
        p = d.prev.get(k)
        if p is None:
            return None, 'no straight-line A writer (entry)'
        sz, mn, md, val, _ = d.insn[p]
        if mn == 'LDA' and md == 'imm':
            return val, f'LDA #${val:02X} @{p[0]:02}:{p[1]:04X}' + (' (+joined path)' if joined else '')
        if mn in DA.A_WRITERS or md == 'acc':
            op = DA.fmt_op(md, val, d.operand_name(p[0], md, val, d.insn[p][4]))
            return None, f'{mn} {op} @{p[0]:02}:{p[1]:04X}'
        if mn in ('RTS', 'RTI', 'JMP'):
            return None, 'path start'
        k = p
    return None, 'no A writer within 8 insns'


def check(d):
    res = {'calls': [], 'tables': [], 'banks': [], 'data': []}
    for (b, a), (sz, mn, md, val, ctx) in sorted(d.insn.items()):
        if mn in ('JSR', 'JMP') and md == 'abs' and val >= 0x8000:
            tb = d.resolve(val, ctx, (b, a))
            st = target_state(d, tb, val)
            res['calls'].append((st, f'{b:02}:{a:04X} {mn} -> {name(d, tb, val)}  ctx={fmt_ctx(ctx)}'))
            if val in BANK_FUNCS:
                v, src = a_source(d, b, a)
                fn = BANK_FUNCS[val]
                if v is None:
                    st2 = 'DYNAMIC'
                elif fn == 'SwitchBank16':
                    st2 = 'OK' if v <= 14 else 'BAD'
                    src += f'  -> banks {2 * v}/{2 * v + 1}'
                else:
                    st2 = 'OK' if v <= 29 else 'BAD'
                res['banks'].append((st2, f'{b:02}:{a:04X} {fn:<12} {src}'))
        elif md in DATA_MODES and val >= 0x8000 and mn not in ('JSR', 'JMP'):
            tb = d.resolve(val, ctx, (b, a))
            if tb is None:
                st = 'UNRESOLVED'
            elif not d.in_win(tb, val):
                st = 'OUTWIN'
            elif (tb, val) in d.code:
                st = 'CODE-READ' if mn in ('LDA', 'LDX', 'LDY', 'CMP', 'CPX', 'CPY', 'ADC',
                                           'SBC', 'AND', 'ORA', 'EOR', 'BIT') else 'CODE-WRITE?'
            else:
                st = 'OK'
            if mn.startswith('ST') or mn in ('INC', 'DEC', 'ASL', 'LSR', 'ROL', 'ROR'):
                st = 'ROM-WRITE' if st != 'UNRESOLVED' else st   # mapper register writes
                if val >= 0x8000 and (val & 0xE001) in (0x8000, 0x8001, 0xA000, 0xA001,
                                                          0xC000, 0xC001, 0xE000, 0xE001):
                    st = 'MAPPER'
            op = DA.fmt_op(md, val, None)
            res['data'].append((st, f'{b:02}:{a:04X} {mn} {op:<10} -> '
                                    f'{name(d, tb, val)}  ctx={fmt_ctx(ctx)}'))
    res['labels'] = []
    for (b, a), (n, _) in sorted(d.labels.items(), key=lambda kv: str(kv[0])):
        if b == '*' or a < 0x8000:
            continue
        if not d.in_win(b, a):
            alt = [x for x in range(d.nb) if x != b and d.in_win(x, a) and (x // 2 == b // 2)]
            res['labels'].append(('OUTWIN', f'{b:02}:{a:04X} {n} — bank {b} sits at '
                                            f'${DA.win(b):04X}; same 16KB pair: '
                                            f'{",".join(f"{x:02}:{a:04X}" for x in alt) or "-"}'))
        else:
            res['labels'].append(('OK', f'{b:02}:{a:04X} {n}'))
    for (b, ea), (tb, v) in sorted(d.tables.items(), key=lambda kv: kv[0]):
        if not v:
            res['tables'].append(('NULL', f'{b:02}:{ea:04X} .dw $0000'))
            continue
        st = target_state(d, tb, v)
        res['tables'].append((st, f'{b:02}:{ea:04X} .dw -> {name(d, tb, v)}'))
    return res


def fmt_ctx(ctx):
    return '/'.join('--' if x is None else f'{x:02}' for x in ctx)


def main(argv):
    show_all = '--all' in argv
    d = build()
    res = check(d)
    out = ['# docs/bank_refs.txt — tools/check_bank_refs.py (session 32)', '',
           'Traced code from tools/disasm_all.py; ctx = (8KB bank at $8000, bank at $A000), -- = unknown.',
           '']
    fail = 0
    for kind in ('calls', 'tables', 'banks', 'data', 'labels'):
        rows = res[kind]
        cnt = {}
        for st, _ in rows:
            cnt[st] = cnt.get(st, 0) + 1
        out.append(f'## {kind}: {len(rows)} — ' + ', '.join(f'{k} {v}' for k, v in sorted(cnt.items())))
        for st, txt in rows:
            if show_all or st not in ('OK', 'MAPPER'):
                out.append(f'  {st:<11} {txt}')
            if kind in ('calls', 'tables') and st in ('OUTWIN', 'MID-INSN', 'DATA'):
                fail += 1
        out.append('')
    os.makedirs(os.path.dirname(OUT), exist_ok=True)
    with open(OUT, 'w') as f:
        f.write('\n'.join(out) + '\n')
    for line in out:
        if line.startswith('## '):
            print(line)
    print(f'-> {os.path.relpath(OUT, nesrom.ROOT)}; {fail} hard failures (calls/tables)')
    return 1 if fail else 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
