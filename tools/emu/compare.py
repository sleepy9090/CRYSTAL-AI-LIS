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
  ttl_mode ttl_step ttl_s3 ttl_wait ttl_ctr ttl_snd   title_mode $06F0, title_step $06F1, state3_mode $06F3,
               $06FE (step wait), title_ctr $06FF, pending title sound $06B2 (s82)
  oam_crc      crc32 of the OAM shadow $0200-$02FF (s82)
"""
import csv
import zlib

SLOTS = 32


def columns():
    cols = ['frame', 'state', 'sub', 'area', 'entry', 'pos_x', 'pos_y', 'cam_x', 'cam_y', 'music']
    cols += [f'snd{c}_period' for c in range(4)] + ['pal_crc']
    for s in range(SLOTS):
        cols += [f'o{s:02X}_ai', f'o{s:02X}_x', f'o{s:02X}_y']
    cols += TTL_COLS + ['oam_crc']
    return cols


# s82: title / state-3 variables (main states 2/3, bank 18/19) + crc32 of the OAM shadow $0200-$02FF
TTL_COLS = ['ttl_mode', 'ttl_step', 'ttl_s3', 'ttl_wait', 'ttl_ctr', 'ttl_snd']
TTL_ADDRS = [0x6F0, 0x6F1, 0x6F3, 0x6FE, 0x6FF, 0x6B2]


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
    out += [r[a] for a in TTL_ADDRS]
    out.append(zlib.crc32(bytes(r[0x200:0x300])))
    return out


class CompareWriter:
    """nes.on_frame callback writing one CSV row per frame to an open text file."""

    def __init__(self, f):
        self.w = csv.writer(f, lineterminator='\n')
        self.w.writerow(columns())

    def __call__(self, nes):
        self.w.writerow(row(nes))
