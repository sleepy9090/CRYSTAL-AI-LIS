#!/usr/bin/env python3
"""Save data layout dump (Ph5 structs 3/5, session 31).

Decodes, straight from the ROM:
  * SaveCopyDesc 23:BDC0 (5 x 6 bytes) / LoadCopyDesc 23:BDDE (4 x 6 bytes):
    src, dst, count lo/hi.  BlockCopy 23:BCF7 copies lo+1+hi bytes (quirk); the
    intended length is the 16-bit count + 1.  Both are shown.
  * SaveSlotPages 31:F26D (page bases summed by SaveSumSlot 31:F23D) and
    SlotCopyPages 31:F298 (src/dst page pairs used by SlotCopy 31:F273).
  * Slot descriptors 16:9D32 (src,dst words, used by 16:9CE0).
  * The new-save template page 23:BE00-BEFF, which BootSaveCheck/ColdInitSub copy
    into $7D00 / $7100 / $7400 / $7700 / $7A00 when a save image is invalid
    (the following two pages are zero-filled).
  * A byte-ownership map of the 768-byte current save image $7D00-$7FFF
    (which RAM source SaveGame really writes to each byte).

usage: save_dump.py [--out FILE]
"""
import sys, os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import nesrom


def rd(prg, bank, addr, n):
    o = bank * nesrom.BANK_SIZE + (addr & 0x1FFF)
    return prg[o:o + n]


def desc_list(prg, addr, count):
    out = []
    for i in range(count):
        b = rd(prg, 23, addr + i * 6, 6)
        src = b[0] | b[1] << 8
        dst = b[2] | b[3] << 8
        lo, hi = b[4], b[5]
        out.append((src, dst, lo, hi, lo + 1 + hi, (lo | hi << 8) + 1))
    return out


def ram_name(labels, addr):
    for b in ('*',):
        n = labels.get((b, addr)) if isinstance(labels, dict) else None
        if n:
            return n[0] if isinstance(n, tuple) else n
    return ''


# Field names for the $6400 page (template offsets) -- from REVERSE.md / labels.csv
PAGE_6400 = [
    (0x00, 7, 'player_name', 'zero-terminated name used by dialogue code 04 ("------",0 in template)'),
    (0x07, 0x09, '$6407-$640F', 'no static refs'),
    (0x10, 0x08, 'player_name_hi', '$6410-$6415 name upper tile row (name entry 19:A456; PauseScreen 15:BD01, ending 17:A40C)'),
    (0x18, 6, 'player_name6', '6-char name-entry buffer (menu FE escape reads slot+$18)'),
    (0x1E, 0x0A, '$641E-$6427', 'bank-16 menu vars ($6424/$6426/$6427 used by bank 16)'),
    (0x28, 8, 'inv_select', 'equipped/selected item row $6428-$642F (template FF)'),
    (0x30, 0x30, 'inventory', '$6430-$645F inventory (template FF = empty)'),
    (0x60, 0x0A, '$6460-$6469', 'template FF; no static refs'),
    (0x6A, 0x06, 'menu vars', '$646A-$646F bank-16 menu vars incl. save_slot_sel $646B, shop_id $646D'),
    (0x70, 0x08, 'shop_goods', '$6470-$6473 goods ids, $6474/75 shop_price_sel (bank 16 shops, s71)'),
    (0x78, 8, 'shop_prices', '$6478-$647F 4 price words; template 30,40,60,50 = tool shop 0 (16:9E54)'),
]

PLAYER_STATE = [
    (0xF0, 'area_id $6C'), (0xF1, 'obj_x_lo $70 (player slot 0)'), (0xF2, 'obj_x_hi $90'),
    (0xF3, 'obj_y_lo $B0'), (0xF4, 'obj_y_hi $D0'), (0xF5, 'player_hp_max $03C0'),
    (0xF6, 'player_hp $03C1'), (0xF7, 'player_level $0421'), (0xF8, '$0620 (obj_timer2 slot 0)'),
    (0xFD, 'tag "A"'), (0xFE, 'tag "M"'), (0xFF, 'tag "I"'),
]


def main():
    out_path = None
    if '--out' in sys.argv:
        out_path = sys.argv[sys.argv.index('--out') + 1]
    _, prg, _ = nesrom.load_rom()
    labels = nesrom.load_labels()
    L = []
    p = L.append

    p('== SaveCopyDesc 23:BDC0 (SaveGame, 5 records) ==')
    p(' #  src    dst    cnt(lo,hi)  copied(quirk)  intended')
    save = desc_list(prg, 0xBDC0, 5)
    for i, (s, d, lo, hi, q, full) in enumerate(save):
        p(f' {i}  ${s:04X}  ${d:04X}  {lo:02X} {hi:02X}       {q:4d} (${s:04X}-${s+q-1:04X})  {full:4d}')
    p('')
    p('== LoadCopyDesc 23:BDDE (LoadGame, 4 records) ==')
    load = desc_list(prg, 0xBDDE, 4)
    for i, (s, d, lo, hi, q, full) in enumerate(load):
        p(f' {i}  ${s:04X}  ${d:04X}  {lo:02X} {hi:02X}       {q:4d} (${d:04X}-${d+q-1:04X})  {full:4d}')
    p('')

    pages = rd(prg, 31, 0xF26D, 6)
    p('== SaveSumSlot 31:F23D page table 31:F26D (X -> 3 pages summed) ==')
    p('  ' + '  '.join(f'X={i}:${b:02X}00' for i, b in enumerate(pages)))
    pairs = rd(prg, 31, 0xF298, 12)
    p('== SlotCopy 31:F273 table 31:F298 (A -> src page, dst page; 3 pages) ==')
    p('  ' + '  '.join(f'A={i}:${pairs[2*i]:02X}00->${pairs[2*i+1]:02X}00' for i in range(6)))
    sd = rd(prg, 16, 0x9D32, 32)
    p('== Slot descriptors 16:9D32 (16:9CE0, A -> src, dst) ==')
    p('  ' + '  '.join(f'{i}:${sd[4*i]|sd[4*i+1]<<8:04X}->${sd[4*i+2]|sd[4*i+3]<<8:04X}' for i in range(8)))
    p('')

    # byte ownership of $7D00-$7FFF after SaveGame
    own = ['zero'] * 0x300
    for i in range(0xF0, 0xF9):
        own[i] = 'player'
    for i in (0xFD, 0xFE, 0xFF):
        own[i] = 'tag'
    for (s, d, lo, hi, q, full) in save:
        if 0x7D00 <= d < 0x8000:
            for k in range(q):
                own[d - 0x7D00 + k] = f'${s + k:04X}'
    p('== Current save image $7D00-$7FFF after SaveGame (byte ownership, runs) ==')
    start = 0
    def kind(v):
        return v if not v.startswith('$') else 'copy'
    i = 0
    while i < 0x300:
        j = i
        while j + 1 < 0x300 and kind(own[j + 1]) == kind(own[i]) and (
                kind(own[i]) != 'copy' or int(own[j + 1][1:], 16) == int(own[j][1:], 16) + 1):
            j += 1
        if kind(own[i]) == 'copy':
            what = f'<- {own[i]}-{own[j]}'
        elif own[i] == 'player':
            what = 'SavePlayerState 23:BD1F'
        elif own[i] == 'tag':
            what = '"AMI" tag'
        else:
            what = 'zero (cleared at 23:BC2B, never written)'
        p(f'  ${0x7D00+i:04X}-${0x7D00+j:04X} ({j-i+1:3d})  {what}')
        i = j + 1
    p('')
    p('  player-state bytes:')
    for off, n in PLAYER_STATE:
        p(f'    ${0x7D00+off:04X}  {n}')
    p('')

    tpl = rd(prg, 23, 0xBE00, 0x100)
    p('== New-save template 23:BE00 (-> $7D00 page; $7E00/$7F00 pages zeroed) ==')
    for r in range(0, 0x100, 16):
        p(f'  +{r:02X} (${0x7D00+r:04X})  ' + ' '.join(f'{b:02X}' for b in tpl[r:r+16]))
    p('')
    p('  $6400 page fields (template +$00-$7F):')
    for off, n, name, note in PAGE_6400:
        p(f'    +{off:02X}-{off+n-1:02X}  {name:12s} {" ".join(f"{b:02X}" for b in tpl[off:off+n][:12])}{" .." if n > 12 else ""}  {note}')
    p('  $0702 block (template +$80-$9C, 29 bytes -> $0702-$071E):')
    for k in range(29):
        a = 0x0702 + k
        nm = ''
        for key in (('*', a),):
            v = labels.get(key) if isinstance(labels, dict) else None
            if v:
                nm = v[0] if isinstance(v, (tuple, list)) else str(v)
        p(f'    ${0x7D80+k:04X} = ${a:04X} {nm:16s} {tpl[0x80+k]:02X}')
    p('  $07DC block (template +$B0-$D3, 36 bytes -> $07DC-$07FF):')
    row = []
    for k in range(36):
        a = 0x07DC + k
        v = labels.get(('*', a)) if isinstance(labels, dict) else None
        nm = (v[0] if isinstance(v, (tuple, list)) else str(v)) if v else f'${a:04X}'
        row.append(f'{nm}={tpl[0xB0+k]:02X}')
    for k in range(0, 36, 6):
        p('    ' + '  '.join(row[k:k+6]))
    p('  player state (template +$F0-$FF):')
    for off, n in PLAYER_STATE:
        p(f'    ${0x7D00+off:04X}  {n:28s} {tpl[off]:02X}')
    p('  bytes outside every load range (stale, never loaded): '
      f'+9D-AF = {" ".join(f"{b:02X}" for b in tpl[0x9D:0xB0])}')
    p(f'                                                         +D4-EF = {" ".join(f"{b:02X}" for b in tpl[0xD4:0xF0])}')

    txt = '\n'.join(L) + '\n'
    if out_path:
        open(out_path, 'w').write(txt)
        print(f'wrote {out_path} ({len(L)} lines)')
    else:
        sys.stdout.write(txt)


if __name__ == '__main__':
    main()
