"""Per-frame state CSV for diffing tools.emu against the web port (Ph5.5 emu 6/6 (c)).

`python3 -m tools.emu ... --compare out.csv` writes one row after every completed frame.
The web harness (`web/test/run.mjs --compare`, Ph7 web 3/8) must write the same columns.
All values are decimal integers read from RAM after the frame (= after its NMI):

  frame        ppu.frame_no of the frame just finished
  state sub    main_state $40, sub_state $41
  area entry   area_id $6C, area_entry $6D
  pos_x pos_y  ($35<<8|$34), ($37<<8|$36) position cursor (last value the frame left there)
  cam_x cam_y  $02 | $03<<8, $04 | $05<<8 camera (what ObjSpriteDraw subtracts)
  music        snd_cur_music $0102
  snd{c}_period  (snd_ch_period_hi $017A,c <<8) | snd_ch_period_lo $0174,c for channels 0-3
               (SQ1 SQ2 TRI NOISE; base period, before vibrato)
  pal_crc      crc32 of pal_buf $6140-$615F (WRAM)
  o{NN}_ai o{NN}_x o{NN}_y  obj_active $04A0,s; x = $90,s<<8|$70,s; y = $D0,s<<8|$B0,s for slots 00-1F
               (x/y are written as 0 when the slot is free, so stale positions don't cause diffs)
"""
import csv
import zlib

SLOTS = 32


def columns():
    cols = ['frame', 'state', 'sub', 'area', 'entry', 'pos_x', 'pos_y', 'cam_x', 'cam_y', 'music']
    cols += [f'snd{c}_period' for c in range(4)] + ['pal_crc']
    for s in range(SLOTS):
        cols += [f'o{s:02X}_ai', f'o{s:02X}_x', f'o{s:02X}_y']
    return cols


def row(nes):
    r, w = nes.bus.ram, nes.bus.wram
    out = [nes.ppu.frame_no - 1, r[0x40], r[0x41], r[0x6C], r[0x6D],
           r[0x34] | r[0x35] << 8, r[0x36] | r[0x37] << 8,
           r[0x02] | r[0x03] << 8, r[0x04] | r[0x05] << 8, r[0x102]]
    out += [r[0x174 + c] | r[0x17A + c] << 8 for c in range(4)]
    out.append(zlib.crc32(bytes(w[0x140:0x160])))
    for s in range(SLOTS):
        ai = r[0x4A0 + s]
        if ai:
            out += [ai, r[0x70 + s] | r[0x90 + s] << 8, r[0xB0 + s] | r[0xD0 + s] << 8]
        else:
            out += [0, 0, 0]
    return out


class CompareWriter:
    """nes.on_frame callback writing one CSV row per frame to an open text file."""

    def __init__(self, f):
        self.w = csv.writer(f, lineterminator='\n')
        self.w.writerow(columns())

    def __call__(self, nes):
        self.w.writerow(row(nes))
