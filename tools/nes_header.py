#!/usr/bin/env python3
"""Parse iNES / NES 2.0 header and print ROM layout + per-bank vectors/stats."""
import sys, hashlib, zlib, math, collections

MAPPERS = {0:"NROM",1:"MMC1 (SxROM)",2:"UxROM",3:"CNROM",4:"MMC3 (TxROM)",5:"MMC5",7:"AxROM",
           9:"MMC2",10:"MMC4",11:"Color Dreams",66:"GxROM",69:"FME-7",71:"Camerica"}

def entropy(b):
    if not b: return 0.0
    c = collections.Counter(b); n = len(b)
    return -sum(v/n*math.log2(v/n) for v in c.values())

def main(path):
    d = open(path,'rb').read()
    h = d[:16]
    print(f"file: {path}  size: {len(d)} (0x{len(d):X})")
    print("header:", h.hex(' '))
    if h[:4] != b'NES\x1a':
        print("NOT an iNES file"); return
    prg16, chr8 = h[4], h[5]
    f6, f7 = h[6], h[7]
    nes2 = (f7 & 0x0C) == 0x08
    mapper = (f6 >> 4) | (f7 & 0xF0)
    if nes2: mapper |= (h[8] & 0x0F) << 8
    print(f"format: {'NES 2.0' if nes2 else 'iNES'}")
    print(f"PRG: {prg16} x 16KB = {prg16*16}KB   CHR: {chr8} x 8KB = {chr8*8}KB {'(CHR-RAM)' if chr8==0 else ''}")
    print(f"mapper: {mapper} {MAPPERS.get(mapper,'?')}")
    print(f"mirroring: {'four-screen' if f6&8 else ('vertical' if f6&1 else 'horizontal')}  battery: {bool(f6&2)}  trainer: {bool(f6&4)}")
    print(f"flags7=0x{f7:02X} byte8..15: {h[8:].hex(' ')}")
    off = 16 + (512 if f6&4 else 0)
    prg = d[off:off+prg16*0x4000]
    chr_ = d[off+prg16*0x4000: off+prg16*0x4000+chr8*0x2000]
    extra = len(d) - (off + len(prg) + len(chr_))
    print(f"PRG file range: 0x{off:X}-0x{off+len(prg)-1:X}")
    if chr_: print(f"CHR file range: 0x{off+len(prg):X}-0x{off+len(prg)+len(chr_)-1:X}")
    print(f"trailing bytes: {extra}")
    print(f"CRC32(PRG+CHR)=0x{zlib.crc32(prg+chr_):08X}  CRC32(file)=0x{zlib.crc32(d):08X}")
    print(f"SHA1(PRG+CHR)={hashlib.sha1(prg+chr_).hexdigest()}")
    print("\nPRG 16KB banks (vectors at end, as if mapped at $C000):")
    for i in range(prg16):
        b = prg[i*0x4000:(i+1)*0x4000]
        nmi, rst, irq = [b[0x3FFA+k*2] | b[0x3FFB+k*2]<<8 for k in range(3)]
        ff = b.count(0xFF); z = b.count(0)
        print(f"  bank {i:2d} file 0x{off+i*0x4000:05X}  NMI={nmi:04X} RST={rst:04X} IRQ={irq:04X}  "
              f"ent={entropy(b):.2f} FF={ff} 00={z}  tail={b[-16:].hex()}")
    if chr_:
        print("\nCHR 8KB banks:")
        for i in range(chr8):
            b = chr_[i*0x2000:(i+1)*0x2000]
            print(f"  chr {i:2d} file 0x{off+len(prg)+i*0x2000:05X} ent={entropy(b):.2f} 00={b.count(0)}")
    # ascii strings
    print("\nASCII strings >=6 in PRG:")
    cur=b''; start=0; n=0
    for i,c in enumerate(prg):
        if 0x20<=c<0x7f:
            if not cur: start=i
            cur+=bytes([c])
        else:
            if len(cur)>=6 and n<40:
                print(f"  0x{off+start:05X}: {cur.decode()}"); n+=1
            cur=b''

if __name__=='__main__':
    main(sys.argv[1] if len(sys.argv)>1 else 'rom/re-c.nes')
