#!/usr/bin/env python3
"""6502 (NES 2A03) opcode database. Official opcodes + common illegal ones (marked '*').

MODES: imp acc imm zp zpx zpy abs abx aby ind izx izy rel
"""

SIZES = {'imp': 1, 'acc': 1, 'imm': 2, 'zp': 2, 'zpx': 2, 'zpy': 2, 'izx': 2, 'izy': 2,
         'rel': 2, 'abs': 3, 'abx': 3, 'aby': 3, 'ind': 3}

_TABLE = """
00 BRK imp|01 ORA izx|05 ORA zp|06 ASL zp|08 PHP imp|09 ORA imm|0A ASL acc|0D ORA abs|0E ASL abs
10 BPL rel|11 ORA izy|15 ORA zpx|16 ASL zpx|18 CLC imp|19 ORA aby|1D ORA abx|1E ASL abx
20 JSR abs|21 AND izx|24 BIT zp|25 AND zp|26 ROL zp|28 PLP imp|29 AND imm|2A ROL acc|2C BIT abs|2D AND abs|2E ROL abs
30 BMI rel|31 AND izy|35 AND zpx|36 ROL zpx|38 SEC imp|39 AND aby|3D AND abx|3E ROL abx
40 RTI imp|41 EOR izx|45 EOR zp|46 LSR zp|48 PHA imp|49 EOR imm|4A LSR acc|4C JMP abs|4D EOR abs|4E LSR abs
50 BVC rel|51 EOR izy|55 EOR zpx|56 LSR zpx|58 CLI imp|59 EOR aby|5D EOR abx|5E LSR abx
60 RTS imp|61 ADC izx|65 ADC zp|66 ROR zp|68 PLA imp|69 ADC imm|6A ROR acc|6C JMP ind|6D ADC abs|6E ROR abs
70 BVS rel|71 ADC izy|75 ADC zpx|76 ROR zpx|78 SEI imp|79 ADC aby|7D ADC abx|7E ROR abx
81 STA izx|84 STY zp|85 STA zp|86 STX zp|88 DEY imp|8A TXA imp|8C STY abs|8D STA abs|8E STX abs
90 BCC rel|91 STA izy|94 STY zpx|95 STA zpx|96 STX zpy|98 TYA imp|99 STA aby|9A TXS imp|9D STA abx
A0 LDY imm|A1 LDA izx|A2 LDX imm|A4 LDY zp|A5 LDA zp|A6 LDX zp|A8 TAY imp|A9 LDA imm|AA TAX imp|AC LDY abs|AD LDA abs|AE LDX abs
B0 BCS rel|B1 LDA izy|B4 LDY zpx|B5 LDA zpx|B6 LDX zpy|B8 CLV imp|B9 LDA aby|BA TSX imp|BC LDY abx|BD LDA abx|BE LDX aby
C0 CPY imm|C1 CMP izx|C4 CPY zp|C5 CMP zp|C6 DEC zp|C8 INY imp|C9 CMP imm|CA DEX imp|CC CPY abs|CD CMP abs|CE DEC abs
D0 BNE rel|D1 CMP izy|D5 CMP zpx|D6 DEC zpx|D8 CLD imp|D9 CMP aby|DD CMP abx|DE DEC abx
E0 CPX imm|E1 SBC izx|E4 CPX zp|E5 SBC zp|E6 INC zp|E8 INX imp|E9 SBC imm|EA NOP imp|EC CPX abs|ED SBC abs|EE INC abs
F0 BEQ rel|F1 SBC izy|F5 SBC zpx|F6 INC zpx|F8 SED imp|F9 SBC aby|FD SBC abx|FE INC abx
"""

# Common illegal opcodes (rarely used by commercial games; flagged with *)
_ILLEGAL = """
A7 *LAX zp|B7 *LAX zpy|AF *LAX abs|BF *LAX aby|A3 *LAX izx|B3 *LAX izy
87 *SAX zp|97 *SAX zpy|8F *SAX abs|83 *SAX izx
EB *SBC imm|1A *NOP imp|3A *NOP imp|5A *NOP imp|7A *NOP imp|DA *NOP imp|FA *NOP imp
04 *NOP zp|44 *NOP zp|64 *NOP zp|0C *NOP abs|80 *NOP imm
"""

OPCODES = {}  # byte -> (mnemonic, mode)
for blob, in ((_TABLE,), (_ILLEGAL,)):
    for line in blob.strip().splitlines():
        for ent in line.split('|'):
            op, mn, md = ent.split()
            OPCODES[int(op, 16)] = (mn, md)

BRANCHES = {'BPL', 'BMI', 'BVC', 'BVS', 'BCC', 'BCS', 'BNE', 'BEQ'}
TERMINATORS = {'RTS', 'RTI', 'JMP', 'BRK'}
WRITES = {'STA', 'STX', 'STY', 'INC', 'DEC', 'ASL', 'LSR', 'ROL', 'ROR', '*SAX'}

# NES hardware registers
HW_REGS = {
    0x2000: 'PPUCTRL', 0x2001: 'PPUMASK', 0x2002: 'PPUSTATUS', 0x2003: 'OAMADDR', 0x2004: 'OAMDATA',
    0x2005: 'PPUSCROLL', 0x2006: 'PPUADDR', 0x2007: 'PPUDATA',
    0x4000: 'SQ1_VOL', 0x4001: 'SQ1_SWEEP', 0x4002: 'SQ1_LO', 0x4003: 'SQ1_HI',
    0x4004: 'SQ2_VOL', 0x4005: 'SQ2_SWEEP', 0x4006: 'SQ2_LO', 0x4007: 'SQ2_HI',
    0x4008: 'TRI_LINEAR', 0x400A: 'TRI_LO', 0x400B: 'TRI_HI',
    0x400C: 'NOISE_VOL', 0x400E: 'NOISE_LO', 0x400F: 'NOISE_HI',
    0x4010: 'DMC_FREQ', 0x4011: 'DMC_RAW', 0x4012: 'DMC_START', 0x4013: 'DMC_LEN',
    0x4014: 'OAMDMA', 0x4015: 'SND_CHN', 0x4016: 'JOY1', 0x4017: 'JOY2_FRAMECNT',
    # MMC3
    0x8000: 'MMC3_BANKSEL', 0x8001: 'MMC3_BANKDATA', 0xA000: 'MMC3_MIRROR', 0xA001: 'MMC3_PRGRAM',
    0xC000: 'MMC3_IRQLATCH', 0xC001: 'MMC3_IRQRELOAD', 0xE000: 'MMC3_IRQOFF', 0xE001: 'MMC3_IRQON',
}


def decode(buf, pos, pc):
    """Decode one instruction at buf[pos], CPU address pc.
    Returns (size, mnemonic, mode, operand_value, target_or_None)."""
    op = buf[pos]
    if op not in OPCODES:
        return 1, '.db', 'imp', op, None
    mn, md = OPCODES[op]
    sz = SIZES[md]
    if pos + sz > len(buf):
        return 1, '.db', 'imp', op, None
    if sz == 1:
        val = None
    elif sz == 2:
        val = buf[pos + 1]
    else:
        val = buf[pos + 1] | buf[pos + 2] << 8
    tgt = None
    if md == 'rel':
        off = val - 256 if val >= 128 else val
        tgt = (pc + 2 + off) & 0xFFFF
        val = tgt
    elif md in ('abs', 'abx', 'aby', 'ind', 'zp', 'zpx', 'zpy', 'izx', 'izy'):
        tgt = val
    return sz, mn, md, val, tgt


def fmt_operand(md, val, name=None):
    if md in ('imp',):
        return ''
    if md == 'acc':
        return 'A'
    if md == 'imm':
        return f'#${val:02X}'
    s = name if name else (f'${val:02X}' if md in ('zp', 'zpx', 'zpy', 'izx', 'izy') else f'${val:04X}')
    return {'zp': s, 'abs': s, 'rel': s, 'zpx': f'{s},X', 'abx': f'{s},X', 'zpy': f'{s},Y',
            'aby': f'{s},Y', 'ind': f'({s})', 'izx': f'({s},X)', 'izy': f'({s}),Y'}[md]
