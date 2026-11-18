#!/usr/bin/env python3
"""Full-ROM recursive-descent disassembler for re-c (session 22).

Seeds: the CPU vectors, every labels.csv code label (data labels are recognised by name),
the dispatch tables in JUMP_TABLES (each entry is traced with the bank context that the
dispatcher sets up), and JMP tables found at the start of swappable banks.

Bank context: each trace path carries ctx = (r6, r7), the 8KB banks mapped at $8000/$A000.
`LDA #n` followed by JSR/JMP SwitchBank16/SetR6/SetR7 (and the fixed tail-calls in
BANK_EFFECTS) updates it. A $8000-$BFFF target with an unknown ctx is resolved through
labels.csv when exactly one bank has a code label there. Otherwise it is listed as unresolved
in coverage.txt. Branches that always go one way after LDx #imm / CLC / SEC / CLV are treated
as unconditional.

Output: disasm/bank_NN.asm (CPU origin nesrom.bank_window, or filler_scan.WINDOW),
disasm/coverage.txt. Self-check: a small built-in assembler re-assembles every .asm file
and compares it with the bank bytes. The run fails if any byte differs.

Usage: python3 tools/disasm_all.py [--quiet]
"""
import os, re, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nesrom
from instruction_set import OPCODES, SIZES, decode, HW_REGS, BRANCHES
from filler_scan import DEAD, WINDOW

OUT_DIR = os.path.join(nesrom.ROOT, 'disasm')
SWITCH16, SETR6, SETR7 = 0xC40E, 0xC418, 0xC427
# fixed routines with a known bank effect regardless of A
BANK_EFFECTS = {0xC7A0: (26, 27), 0xE3D4: (12, 13)}
MAX_CTX = 6           # contexts traced per location

# Known pure-data areas (Data-Range Map): never traced as code.
KNOWN_DATA = [(b, None, None, 'screen pages / metatiles') for b in range(0, 10)] + [
    (21, None, None, 'dialogue text'), (22, None, None, 'dialogue text'),
    (23, 0xA000, 0xBBFF, 'dialogue text')]

# (bank, addr, count or None=auto, ctx(i) -> (r6, r7), note)
JUMP_TABLES = [
    (30, 0xC91F, 9, lambda i: (None, None), 'MainStateTable (6/7 null)'),
    (31, 0xF433, 8, lambda i: (None, None), 'IrqHandlerTable'),
    (30, 0xCADE, 40, None, 'SubstateHandlerTable (ctx from SubstateBankTable)'),
    (30, 0xD123, 32, lambda i: (14, 15), 'TalkActionTable'),
    (30, 0xD563, 32, lambda i: (14, 15), 'EventActionTable'),
    (30, 0xD885, 3, lambda i: (14, 15), 'ItemEventHookTable'),
    (31, 0xE218, 5, lambda i: (12, 13), 'SpawnHandlerTable'),
    (24, 0x8000, 16, lambda i: (24, 25), 'SndOpcodeTable'),
    (20, 0x85A2, 10, lambda i: (20, 21), 'TextCtrlTable'),
    (26, 0x8FBB, 4, lambda i: (26, 27), 'HitHandlerTable'),
    (14, 0x8399, 80, lambda i: (14, 15), 'ItemUseHandlers'),
    (27, 0xA314, 128, lambda i: (26, 15) if 0x60 <= i <= 0x6F else (26, 27), 'ObjAiTable'),
    (15, 0xA413, None, lambda i: (26, 15), 'BossPhaseTable'),
    (19, 0xA038, 3, lambda i: (18, 19), 'State3ModeTable ($06F3; JMP ($06FC) 19:A035)'),
    (19, 0xA052, 9, lambda i: (18, 19), 'State3SubTable ($06F0; JMP ($06FC) 19:A04F; 7-8 null)'),
    # session 67: the 12 remaining dispatchers (counts from the index range, see REVERSE.md)
    (17, 0xA11A, 3, lambda i: (16, 17), 'EndingModeTable ($0602; JMP ($008E) 17:A117)'),
    (17, 0xA140, 5, lambda i: (16, 17), 'EndingSubTable ($060E; JMP ($008E) 17:A13D; 0 null)'),
    (27, 0xA072, 16, lambda i: (26, 27), 'MagicCastTable ($10&$0F; JMP ($0010) 27:A06E)'),
    (27, 0xBA40, 13, lambda i: (26, 27), 'Ai70PhaseTable (obj_phase; JMP ($0010) 27:BA3D; 7/11 null)'),
    (30, 0xDBDC, 8, 'subst:20', 'TeleportMenuKeyTable (PadNewButtonIndex; JMP ($0010) 30:DBD9)'),
    (30, 0xDDBD, 8, 'subst:21', 'TelepathyKeyTable (PadNewButtonIndex; JMP ($0010) 30:DDBA)'),
    (30, 0xDF0E, 8, 'subst:23', 'ChangeFormKeyTable (PadNewButtonIndex; JMP ($0010) 30:DF0B)'),
    (31, 0xE3D9, 8, lambda i: (12, 13), 'AreaEntryTable (($6D&$E0)>>4; JMP ($0010) 31:E419; 4-6 null)'),
    (31, 0xF6C2, 10, lambda i: (None, None), 'NmiModeTable (irq_mode; JMP ($0052) 31:F6BF; 8 null)'),
    (19, 0xA193, 7, lambda i: (18, 19), 'StatusStepTable (title_step; title_mode 4)'),
    (19, 0xA6F3, 3, lambda i: (18, 19), 'TitleMenuStepTable (title_step; title_mode 6)'),
    (19, 0xA7BC, 25, lambda i: (18, 19), 'OpeningStepTable (title_step; title_mode 0)'),
    (19, 0xAC07, 28, lambda i: (18, 19), 'StoryStepTable (title_step; title_mode 2)'),
]

DATA_NAME_RE = re.compile(
    r'^(Stale|BitMask|Vectors$|IrqDefaultPtr$|TextWinParams|TextDict$|NameEntryRect$|'
    r'TextGroupBank$|EvType38Pos$|SaveSumPages$|SlotCopyPages$|SndDmcSample$|AttrRowOffset$|'
    r'AttrReplicate$|SwordElemBit$|SwordMpCost$|DiagToCardinal$|BgProbePoints$|ShotOffsets$)'
    r'|(Table\w*|Ptrs|PtrsLo|PtrsHi|PresenceLo|PresenceHi|PeriodLo|Strings|Names|Digits|Descs?|Flags|Ids|Params|'
    r'List|Lists|Recs|RecPtrs|Tiles|Index|Class|RandSeq|Gate|Cap|Grid|Str\d\d|Ofs|'
    r'Value|Handlers)$')
CODE_NAMES = {'SaveJumpTable', 'AreaSubTable'}
RESTORE_BANKS = 0xFFDB
# Label seeds whose code runs with a non-default partner bank:
# bank 15 AI/boss code is mapped at $A000 by ObjUpdateAll (SetR7($0F)) with bank 26 at $8000.
LABEL_CTX = [(15, 0xA400, 0xBBFF, (26, 15))]
FLAG_KEEP_ZN = {'STA', 'STX', 'STY', 'PHA', 'PHP', 'TXS', 'CLC', 'SEC', 'CLI', 'SEI', 'CLD',
                'SED', 'CLV', 'NOP', 'JMP'}
FLAG_KEEP_C = FLAG_KEEP_ZN | {'LDA', 'LDX', 'LDY', 'TAX', 'TAY', 'TXA', 'TYA', 'TSX', 'INX',
                              'INY', 'DEX', 'DEY', 'INC', 'DEC', 'AND', 'ORA', 'EOR', 'BIT',
                              'PLA'}
A_WRITERS = {'LDA', 'TXA', 'TYA', 'PLA', 'ADC', 'SBC', 'AND', 'ORA', 'EOR', 'ASL', 'LSR',
             'ROL', 'ROR', '*LAX', 'JSR'}


def win(bank):
    return WINDOW.get(bank, nesrom.bank_window(bank))


def default_ctx(bank, addr=None):
    w = win(bank)
    for b, lo, hi, ctx in LABEL_CTX:
        if b == bank and addr is not None and lo <= addr <= hi:
            return ctx
    if bank in (30, 31):
        return (None, None)
    if w == 0x8000:
        return (bank, bank + 1)
    return (bank - 1 if bank % 2 else None, bank)


def pin(bank, ctx):
    """Code running from a swappable window keeps its own bank mapped there."""
    if bank in (30, 31):
        return ctx
    return (bank, ctx[1]) if win(bank) == 0x8000 else (ctx[0], bank)


def is_data_label(name):
    return name not in CODE_NAMES and bool(DATA_NAME_RE.search(name))


class Dis:
    def __init__(self):
        _, self.prg, _ = nesrom.load_rom()
        self.nb = nesrom.nbanks(self.prg)
        self.labels = nesrom.load_labels()
        self.insn = {}        # (bank, addr) -> (size, mn, md, val, ctx)
        self.code = set()     # (bank, addr) code bytes
        self.data = {}        # (bank, addr) -> reason for known-data bytes
        self.visited = {}     # (bank, addr) -> set(ctx)
        self.auto = {}        # (bank, addr) -> 'sub'|'loc'
        self.tables = {}      # (bank, addr) -> (bank, target) for .dw entries
        self.warn = []
        self.unresolved = {}  # (bank, addr, tgt) -> kind
        self.indirect = []
        self.work = []
        self.rejected = []     # (src, bank, addr) targets that failed the plausibility check
        self.straddle = set()  # instructions whose bytes continue in the next window's bank
        self.summary = {}      # (bank, addr) -> callee bank effect
        self.code_labels_by_addr = {}
        for (b, a), (n, _) in self.labels.items():
            if b != '*' and not is_data_label(n):
                self.code_labels_by_addr.setdefault(a, []).append(b)
        self.dead = {}
        for b, lo, hi, src, ev in DEAD:
            for a in range(lo, hi + 1):
                self.dead[(b, a)] = f'dead filler (copy of {src})'
        for b, lo, hi, why in KNOWN_DATA:
            lo = lo or win(b)
            hi = hi or win(b) + 0x1FFF
            for a in range(lo, hi + 1):
                self.data.setdefault((b, a), why)
        for k, v in self.dead.items():
            self.data[k] = v

    def byte(self, bank, addr):
        return self.prg[bank * 0x2000 + (addr & 0x1FFF)]

    def in_win(self, bank, addr):
        return win(bank) <= addr < win(bank) + 0x2000

    def resolve(self, addr, ctx, src):
        """CPU addr -> 8KB bank, using ctx; None if RAM/unknown."""
        if addr >= 0xE000:
            return 31
        if addr >= 0xC000:
            return 30
        if addr < 0x8000:
            return None
        b = ctx[0] if addr < 0xA000 else ctx[1]
        if b is None:
            cands = [x for x in self.code_labels_by_addr.get(addr, []) if x not in (30, 31)]
            if len(cands) == 1:
                return cands[0]
            return None
        return b

    def push(self, bank, addr, ctx, kind, src):
        if bank is None:
            self.unresolved[(src[0], src[1], addr)] = kind
            return
        if not self.in_win(bank, addr):
            self.warn.append(f'{src[0]:02}:{src[1]:04X} {kind} ${addr:04X} -> bank {bank} '
                             f'not mapped at its window ${win(bank):04X}')
            return
        if kind in ('sub', 'loc') and (bank, addr) not in self.auto:
            self.auto[(bank, addr)] = kind
        elif kind == 'sub':
            self.auto[(bank, addr)] = 'sub'
        # the target's own window is fixed by where it lives
        r6, r7 = ctx
        if bank not in (30, 31):
            if win(bank) == 0x8000:
                r6 = bank
            else:
                r7 = bank
        self.work.append((bank, addr, (r6, r7), src))

    def run(self):
        while self.work:
            self.trace(*self.work.pop())

    def locate(self, bank, addr, ctx):
        """(bank, addr) of a CPU address reached by falling through; follows the 16KB pair
        ($8000->$A000) and fixed ($C000->$E000) windows. None if not mapped/known."""
        if self.in_win(bank, addr):
            return bank
        if addr >= 0x10000:
            return None
        return self.resolve(addr, ctx, None)

    def decode_at(self, bank, addr, ctx):
        """Decode one instruction; bytes past the window come from the next window's bank.
        Returns (sz, mn, md, val, [(bank, addr) per byte]) or None."""
        b0 = self.byte(bank, addr)
        if b0 not in OPCODES:
            return None
        mn, md = OPCODES[b0]
        sz = SIZES[md]
        locs, raw = [(bank, addr)], [b0]
        for i in range(1, sz):
            b = self.locate(bank, addr + i, ctx)
            if b is None:
                return None
            locs.append((b, addr + i))
            raw.append(self.byte(b, addr + i))
        s, mn, md, val, _ = decode(bytes(raw), 0, addr)
        return s, mn, md, val, locs

    def plausible(self, bank, addr, ctx, n=10):
        """Quick linear check: the first n instructions decode as official opcodes."""
        for _ in range(n):
            if (bank, addr) in self.insn:
                return True
            if (bank, addr) in self.data or not self.in_win(bank, addr):
                return False
            d = self.decode_at(bank, addr, ctx)
            if d is None or d[1].startswith('*') or d[1] == 'BRK':
                return False
            if d[1] in ('RTS', 'RTI', 'JMP'):
                return True
            addr += d[0]
            nb = self.locate(bank, addr, ctx)
            if nb is None:
                return True
            bank = nb
        return True

    def trace(self, bank, addr, ctx, src):
        a_known = None
        zn = c = None          # known (z, n) / carry after LDx #imm, CLC, SEC
        if not self.plausible(bank, addr, ctx):
            self.rejected.append((src, bank, addr))
            return
        while True:
            key = (bank, addr)
            if key in self.data and key not in self.code:
                self.warn.append(f'{src[0]:02}:{src[1]:04X} -> {bank:02}:{addr:04X} runs into '
                                 f'data ({self.data[key]})')
                return
            seen = self.visited.setdefault(key, set())
            if ctx in seen or len(seen) >= MAX_CTX:
                return
            if key in self.code and key not in self.insn:
                self.warn.append(f'{bank:02}:{addr:04X} jump into the middle of an instruction '
                                 f'(from {src[0]:02}:{src[1]:04X})')
                return
            seen.add(ctx)
            d = self.decode_at(bank, addr, ctx)
            if d is None or d[1].startswith('*'):
                self.warn.append(f'{bank:02}:{addr:04X} bad opcode ${self.byte(bank, addr):02X} '
                                 f'(path from {src[0]:02}:{src[1]:04X})')
                return
            sz, mn, md, val, locs = d
            clash = [l for l in locs[1:] if l in self.insn]
            if clash:
                self.warn.append(f'{bank:02}:{addr:04X} overlaps instruction at '
                                 f'{clash[0][0]:02}:{clash[0][1]:04X}')
                return
            for l in locs:
                self.code.add(l)
            if locs[-1][0] != bank:
                self.straddle.add((bank, addr))
            if key not in self.insn:
                self.insn[key] = (sz, mn, md, val, ctx)
            here = (bank, addr)
            nxt = addr + sz
            if mn == 'JSR':
                tb = self.resolve(val, ctx, here)
                if val >= 0x8000:
                    self.push(tb, val, ctx, 'sub', here)
                ctx = pin(bank, self.call_effect(tb, val, a_known, ctx))
                a_known = zn = c = None
            elif mn == 'JMP' and md == 'abs':
                tb = self.resolve(val, ctx, here)
                if val >= 0x8000:
                    self.push(tb, val, ctx, 'loc', here)
                else:
                    self.indirect.append((bank, addr, f'JMP ${val:04X} (RAM)'))
                return
            elif mn == 'JMP':
                self.indirect.append((bank, addr, f'JMP (${val:04X})'))
                return
            elif mn in ('RTS', 'RTI'):
                return
            elif mn == 'BRK':
                self.warn.append(f'{bank:02}:{addr:04X} BRK reached')
                return
            elif mn in BRANCHES:
                taken = branch_fate(mn, zn, c)
                if taken is not False:
                    self.push(self.locate(bank, val, ctx), val, ctx, 'loc', here)
                if taken is True:
                    return
                # fall-through: the tested flag is now known (BNE/BEQ pairs etc.)
                z, n = zn if zn else (None, None)
                if mn in ('BNE', 'BEQ'):
                    zn = (mn == 'BNE', n)
                elif mn in ('BPL', 'BMI'):
                    zn = (z, mn == 'BPL')
                elif mn in ('BCC', 'BCS'):
                    c = mn == 'BCC'
                addr_n = self.locate(bank, nxt, ctx)
                if addr_n is None:
                    return
                bank, addr = addr_n, nxt
                continue
            # register / flag state
            if mn == 'LDA' and md == 'imm':
                a_known = val
            elif mn in A_WRITERS or (mn in ('ASL', 'LSR', 'ROL', 'ROR') and md == 'acc'):
                a_known = None
            if mn in ('LDA', 'LDX', 'LDY') and md == 'imm':
                zn = (val == 0, bool(val & 0x80))
            elif mn not in FLAG_KEEP_ZN and mn not in BRANCHES:
                zn = None
            if mn == 'CLC':
                c = False
            elif mn == 'SEC':
                c = True
            elif mn not in FLAG_KEEP_C and mn not in BRANCHES:
                c = None
            nb = self.locate(bank, nxt, ctx)
            if nb is None:
                self.warn.append(f'{bank:02}:{addr:04X} falls off the window (next bank unknown)')
                return
            bank, addr = nb, nxt

    def call_effect(self, tb, tgt, a, ctx):
        if tgt == SWITCH16:
            return (2 * a, 2 * a + 1) if a is not None else (None, None)
        if tgt == SETR6:
            return (a, ctx[1])
        if tgt == SETR7:
            return (ctx[0], a)
        if tgt in BANK_EFFECTS:
            return BANK_EFFECTS[tgt]
        if tb is None or tgt < 0x8000:
            return ctx
        eff = self.summarize(tb, tgt, 0)
        if eff is None:
            return ctx
        return tuple(ctx[i] if eff[i] == 'keep' else eff[i] for i in (0, 1))

    def summarize(self, bank, addr, depth):
        """Net bank effect of calling (bank, addr): walks the fall-through path (branches not
        taken, JMP abs followed) to the first RTS. Returns (r6, r7) with 'keep' for an
        unchanged window, or None if nothing changes / cannot tell."""
        key = (bank, addr)
        if key in self.summary:
            return self.summary[key]
        self.summary[key] = None          # recursion guard
        ctx = ('keep', 'keep')
        a = None
        res = None
        rctx = default_ctx(bank, addr)
        for _ in range(80):
            if not self.in_win(bank, addr) or (bank, addr) in self.data:
                break
            d = self.decode_at(bank, addr, rctx)
            if d is None:
                break
            sz, mn, md, val, _ = d
            if mn in ('JSR', 'JMP') and md == 'abs':
                if val == RESTORE_BANKS:
                    break                 # far-call trampoline: banks restored
                if val in (SWITCH16, SETR6, SETR7) or val in BANK_EFFECTS:
                    ctx = self.call_effect(None, val, a, ctx)
                    if mn == 'JMP':
                        res = ctx
                        break
                    a = None
                    addr += sz
                    continue
                base = rctx if bank not in (30, 31) else (None, None)
                eff = tuple(base[i] if ctx[i] == 'keep' else ctx[i] for i in (0, 1))
                tb = self.resolve(val, eff, None)
                if mn == 'JSR':
                    if tb is not None and depth < 3:
                        e = self.summarize(tb, val, depth + 1)
                        if e is not None:
                            ctx = tuple(ctx[i] if e[i] == 'keep' else e[i] for i in (0, 1))
                    a = None
                    addr += sz
                    continue
                if tb is None:
                    break
                bank, addr = tb, val
                continue
            if mn in ('RTS', 'RTI'):
                res = ctx
                break
            if mn == 'JMP' or mn == 'BRK' or mn.startswith('*'):
                break
            if mn == 'LDA' and md == 'imm':
                a = val
            elif mn in A_WRITERS or md == 'acc':
                a = None
            addr += sz
        if res == ('keep', 'keep'):
            res = None
        self.summary[key] = res
        return res

    # ---- seeds -------------------------------------------------------------
    def seed(self):
        for i, nm in enumerate(('NMI', 'RESET', 'IRQ')):
            a = 0xFFFA + 2 * i
            v = self.byte(31, a) | self.byte(31, a + 1) << 8
            self.tables[(31, a)] = (31, v)
            self.push(31, v, (None, None), 'sub', (31, a))
        for (b, a), (n, _) in sorted(self.labels.items(), key=lambda kv: str(kv[0])):
            if b == '*' or a < 0x8000:
                continue
            if is_data_label(n):
                continue
            if not self.in_win(b, a):
                self.warn.append(f'label {n} {b}:{a:04X} outside bank window')
                continue
            self.push(b, a, default_ctx(b, a), 'label', (b, a))
        for b, a, cnt, cfn, note in JUMP_TABLES:
            if cnt is None:
                cnt = self.auto_count(b, a)
                note += f' (auto count {cnt})'
                self.table_counts[(b, a)] = cnt
            for i in range(cnt):
                ea = a + 2 * i
                v = self.byte(b, ea) | self.byte(b, ea + 1) << 8
                if isinstance(cfn, str):          # 'subst:N' = the ctx of substate N
                    ctx = self.substate_ctx(int(cfn.split(':')[1]))
                else:
                    ctx = cfn(i) if cfn else self.substate_ctx(i)
                tb = self.resolve(v, ctx, (b, ea)) if v else None
                self.tables[(b, ea)] = (tb, v)
                if v:
                    self.push(tb, v, ctx, 'sub', (b, ea))
        for b in range(10, 30):
            if any(kb == b and lo is None for kb, lo, _, _ in KNOWN_DATA):
                continue
            w = win(b)
            a = w
            while self.byte(b, a) == 0x4C:
                v = self.byte(b, a + 1) | self.byte(b, a + 2) << 8
                if not self.in_win(b, v):
                    break
                self.push(b, a, default_ctx(b), 'entry', (b, a))
                a += 3
            if a > w:
                self.entry_tables.append((b, w, (a - w) // 3))

    def auto_count(self, b, a):
        lo = win(b)
        hi = lo + 0x2000
        n, first = 0, hi
        while a + 2 * n < first:
            v = self.byte(b, a + 2 * n) | self.byte(b, a + 2 * n + 1) << 8
            if not (lo <= v < hi):
                break
            first = min(first, v)
            n += 1
        return n

    def substate_ctx(self, i):
        n = self.byte(30, 0xCB2E + i)
        return (2 * n, 2 * n + 1)

    # ---- output ------------------------------------------------------------
    def name_of(self, bank, addr):
        if bank is None:
            return None
        lab = self.labels.get((bank, addr))
        if lab:
            return lab[0]
        k = self.auto.get((bank, addr))
        if k:
            return f'{k}_{bank:02}_{addr:04X}'
        return None

    def operand_name(self, bank, md, val, ctx):
        if md in ('imm', 'imp', 'acc'):
            return None
        if val < 0x8000:
            lab = self.labels.get(('*', val))
            if lab:
                return lab[0]
            return HW_REGS.get(val)
        if md == 'rel':
            return self.name_of(bank, val)
        tb = self.resolve(val, ctx, None)
        if tb is None:
            return None
        return self.name_of(tb, val)

    def emit_bank(self, bank):
        w = win(bank)
        lines = []
        sym = {}           # name -> value (equates + local labels)
        local = {}         # addr -> name defined in this file
        equ = {}
        for a in range(w, w + 0x2000):
            n = self.name_of(bank, a)
            if n:
                local[a] = n
        # data label + references to own data
        body = []
        a = w
        pending_db = []

        def flush():
            if not pending_db:
                return
            start = pending_db[0][0]
            body.append((start, '    .db ' + ','.join(f'${v:02X}' for _, v in pending_db),
                         f'{bank:02}:{start:04X}'))
            pending_db.clear()

        def refname(n, v):
            if n is None:
                return None
            if n in sym and sym[n] != v:
                return None
            sym[n] = v
            return n

        while a < w + 0x2000:
            n = local.get(a)
            ins = self.insn.get((bank, a))
            tab = self.tables.get((bank, a))
            if n and (ins or tab or (bank, a) not in self.code):
                flush()
                cmt = self.labels.get((bank, a), ('', ''))[1]
                body.append((a, f'{n}:', cmt))
                sym[n] = a
            if ins:
                flush()
                sz, mn, md, val, ctx = ins
                nm = refname(self.operand_name(bank, md, val, ctx), val)
                if nm and nm not in local.values():
                    equ[nm] = val
                op = fmt_op(md, val, nm)
                if (bank, a) in self.straddle:
                    n_here = w + 0x2000 - a
                    body.append((a, '    .db ' + ','.join(f'${self.byte(bank, a + i):02X}'
                                                         for i in range(n_here)),
                                 f'{a:04X}  {mn} {op} -- continues in the next window'))
                    a += n_here
                    continue
                raw = ' '.join(f'{self.byte(bank, a + i):02x}' for i in range(sz))
                body.append((a, f'    {mn} {op}'.rstrip(), f'{a:04X}  {raw}'))
                a += sz
                continue
            if tab:
                flush()
                tb, v = tab
                nm = refname(self.name_of(tb, v), v) if v else None
                if nm and nm not in local.values():
                    equ[nm] = v
                body.append((a, f'    .dw {nm or f"${v:04X}"}', f'{bank:02}:{a:04X}'))
                a += 2
                continue
            if pending_db and (len(pending_db) == 16 or n):
                flush()
            pending_db.append((a, self.byte(bank, a)))
            a += 1
        flush()
        # labels that sit inside instructions -> equates
        defined = {t.rstrip(':') for _, t, _ in body if t.endswith(':')}
        for addr, n in local.items():
            if n not in defined:
                equ[n] = addr
        for n in list(equ):
            if n in defined:
                del equ[n]
        hdr = [f'; re-c bank {bank:02}  file 0x{nesrom.bank_file_off(bank, w):05X}  '
               f'CPU ${w:04X}-${w + 0x1FFF:04X}  (tools/disasm_all.py)', '']
        for n, v in sorted(equ.items(), key=lambda kv: kv[1]):
            hdr.append(f'{n} = ${v:04X}' if v >= 0x100 else f'{n} = ${v:02X}')
        hdr += ['', f'    .org ${w:04X}']
        out = hdr[:]
        for a, text, cmt in body:
            if cmt:
                out.append(f'{text:<40}; {cmt}' if not text.endswith(':') else f'{text}  ; {cmt}')
            else:
                out.append(text)
        return '\n'.join(out) + '\n'

    def coverage(self):
        rows, tot = [], [0, 0, 0]
        for b in range(self.nb):
            w = win(b)
            c = d = 0
            for a in range(w, w + 0x2000):
                k = (b, a)
                if k in self.code:
                    c += 1
                elif k in self.data or k in self.tables or (b, a - 1) in self.tables \
                        or self.in_data_label(b, a):
                    d += 1
            u = 0x2000 - c - d
            tot[0] += c; tot[1] += d; tot[2] += u
            rows.append(f'{b:4}  ${w:04X}  {c:6} {d:6} {u:6}   {100 * c / 0x2000:5.1f}%')
        return rows, tot

    def in_data_label(self, b, a):
        return (b, a) in self.data_label_bytes

    def mark_data_labels(self):
        """Bytes from a data label up to the next label or code byte count as known data."""
        self.data_label_bytes = set()
        for (b, a), (n, _) in self.labels.items():
            if b == '*' or not is_data_label(n) or not self.in_win(b, a):
                continue
            x = a
            end = win(b) + 0x2000
            while x < end and (b, x) not in self.code:
                self.data_label_bytes.add((b, x))
                x += 1
                if (b, x) in self.labels:
                    break


def branch_fate(mn, zn, c):
    """True = always taken, False = never, None = unknown (from tracked Z/N and C)."""
    if zn is not None and mn in ('BEQ', 'BNE', 'BMI', 'BPL'):
        z, n = zn
        f = z if mn in ('BEQ', 'BNE') else n
        if f is None:
            return None
        return {'BEQ': z, 'BNE': not z, 'BMI': n, 'BPL': not n}[mn]
    if c is not None and mn in ('BCC', 'BCS'):
        return c if mn == 'BCS' else not c
    return None


def fmt_op(md, val, name):
    if md == 'imp':
        return ''
    if md == 'acc':
        return 'A'
    if md == 'imm':
        return f'#${val:02X}'
    absm = md in ('abs', 'abx', 'aby')
    if name:
        s = name
    elif md in ('zp', 'zpx', 'zpy', 'izx', 'izy'):
        s = f'${val:02X}'
    else:
        s = f'${val:04X}'
    if absm and val < 0x100:
        s = 'a:' + s
    return {'zp': s, 'abs': s, 'rel': s, 'zpx': f'{s},X', 'abx': f'{s},X', 'zpy': f'{s},Y',
            'aby': f'{s},Y', 'ind': f'({s})', 'izx': f'({s},X)', 'izy': f'({s}),Y'}[md]


# ---- round-trip assembler ------------------------------------------------------
REV = {}
for _op, (_mn, _md) in OPCODES.items():
    if not _mn.startswith('*'):
        REV[(_mn, _md)] = _op


def assemble(text):
    """Tiny 2-pass assembler for the emitted syntax. Returns (org, bytes)."""
    sym, lines = {}, []
    for raw in text.splitlines():
        s = raw.split(';', 1)[0].strip()
        if not s:
            continue
        m = re.match(r'^(\w+)\s*=\s*\$([0-9A-Fa-f]+)$', s)
        if m:
            sym[m.group(1)] = int(m.group(2), 16)
            continue
        lines.append(s)

    def val(tok, pass2):
        tok = tok.strip()
        if tok.startswith('$'):
            return int(tok[1:], 16), len(tok) <= 3
        if tok in sym:
            return sym[tok], sym[tok] < 0x100
        if pass2:
            raise ValueError(f'undefined symbol {tok}')
        return 0x8000, False

    def parse(s, pass2):
        if s.startswith('.db'):
            return [int(x.strip()[1:], 16) for x in s[3:].split(',')]
        if s.startswith('.dw'):
            v, _ = val(s[3:], pass2)
            return [v & 0xFF, v >> 8]
        mn, _, op = s.partition(' ')
        op = op.strip()
        force = False
        if 'a:' in op:
            force = True
            op = op.replace('a:', '')
        if not op:
            md, v = 'imp', None
        elif op == 'A':
            md, v = 'acc', None
        elif op.startswith('#'):
            md, v = 'imm', int(op[2:], 16)
        elif op.endswith(',X)'):
            md, v = 'izx', val(op[1:-3], pass2)[0]
        elif op.endswith('),Y'):
            md, v = 'izy', val(op[1:-3], pass2)[0]
        elif op.startswith('('):
            md, v = 'ind', val(op[1:-1], pass2)[0]
        elif op.endswith(',X') or op.endswith(',Y'):
            v, zp = val(op[:-2], pass2)
            r = op[-1]
            if zp and not force and (mn, 'zp' + r.lower()) in REV:
                md = 'zp' + r.lower()
            else:
                md = 'ab' + r.lower()
        elif mn in BRANCHES:
            md, v = 'rel', val(op, pass2)[0]
        else:
            v, zp = val(op, pass2)
            md = 'zp' if zp and not force and (mn, 'zp') in REV else 'abs'
        return (mn, md, v)

    for pass2 in (False, True):
        pc, org, out = None, None, bytearray()
        for s in lines:
            if s.startswith('.org'):
                pc = org = int(s.split('$')[1], 16)
                continue
            m = re.match(r'^(\w+):$', s)
            if m:
                sym[m.group(1)] = pc
                continue
            p = parse(s, pass2)
            if isinstance(p, list):
                out += bytes(p)
                pc += len(p)
                continue
            mn, md, v = p
            op = REV[(mn, md)]
            sz = SIZES[md]
            if pass2:
                out.append(op)
                if md == 'rel':
                    d = v - (pc + 2)
                    if not -128 <= d <= 127:
                        raise ValueError(f'branch out of range at ${pc:04X}')
                    out.append(d & 0xFF)
                elif sz == 2:
                    out.append(v & 0xFF)
                elif sz == 3:
                    out += bytes((v & 0xFF, v >> 8))
            pc += sz
    return org, bytes(out)


def main(argv):
    quiet = '--quiet' in argv
    if '--labels' in argv:      # show how each ROM label is classified
        for (b, a), (n, _) in sorted(nesrom.load_labels().items(), key=lambda kv: str(kv[0])):
            if b != '*':
                print(f'{"data" if is_data_label(n) else "code"}  {b:02}:{a:04X}  {n}')
        return 0
    d = Dis()
    d.table_counts = {}
    d.entry_tables = []
    d.seed()
    d.run()
    d.mark_data_labels()
    os.makedirs(OUT_DIR, exist_ok=True)
    bad = []
    for b in range(d.nb):
        text = d.emit_bank(b)
        path = os.path.join(OUT_DIR, f'bank_{b:02}.asm')
        with open(path, 'w') as f:
            f.write(text)
        org, data = assemble(text)
        ref = d.prg[b * 0x2000:(b + 1) * 0x2000]
        if org != win(b) or data != ref:
            first = next((i for i in range(min(len(data), len(ref))) if data[i] != ref[i]),
                         min(len(data), len(ref)))
            bad.append(f'bank {b:02}: round-trip FAIL (len {len(data)}, first diff +${first:04X})')
    rows, tot = d.coverage()
    rep = ['# disasm/coverage.txt — tools/disasm_all.py (session 22)', '',
           'bank  win     code   data  unknown  code%   (data = vectors, jump tables, '
           'labelled data runs, DEAD filler, known data banks)']
    rep += rows
    rep.append(f'total        {tot[0]:6} {tot[1]:6} {tot[2]:6}')
    rep += ['', f'round-trip: {"OK, all 32 banks" if not bad else "; ".join(bad)}', '',
            f'auto-named targets: {sum(1 for k in d.auto if d.labels.get(k) is None)}',
            f'swappable-bank JMP entry tables: ' +
            ', '.join(f'{b:02}:{a:04X} x{n}' for b, a, n in d.entry_tables),
            'auto table counts: ' + ', '.join(f'{b:02}:{a:04X}={n}' for (b, a), n in
                                              d.table_counts.items()), '']
    rep.append(f'## indirect jumps ({len(d.indirect)})')
    rep += [f'{b:02}:{a:04X}  {t}' for b, a, t in sorted(set(d.indirect))]
    rep.append('')
    rep.append(f'## unresolved $8000-$BFFF targets, unknown bank context ({len(d.unresolved)})')
    rep += [f'{b:02}:{a:04X} -> ${t:04X} ({k})' for (b, a, t), k in sorted(d.unresolved.items())]
    rep.append('')
    rej = sorted({(s, b, a) for s, b, a in d.rejected
                  if not any((ob, a) in d.insn for ob in range(d.nb) if ob != b)})
    rep.append(f'## rejected targets: no bank context gave a decodable target ({len(rej)})')
    rep += [f'{s[0]:02}:{s[1]:04X} -> ${a:04X} (tried bank {b})' for s, b, a in rej]
    rep.append('')
    rep.append(f'## warnings ({len(set(d.warn))})')
    rep += sorted(set(d.warn))
    with open(os.path.join(OUT_DIR, 'coverage.txt'), 'w') as f:
        f.write('\n'.join(rep) + '\n')
    if not quiet:
        print('\n'.join(rep[:40]))
        print(f'... {len(d.indirect)} indirect, {len(d.unresolved)} unresolved, '
              f'{len(set(d.warn))} warnings -> disasm/coverage.txt')
    if bad:
        print('\n'.join(bad))
        return 1
    return 0


if __name__ == '__main__':
    sys.exit(main(sys.argv[1:]))
