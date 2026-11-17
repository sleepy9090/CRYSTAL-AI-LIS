// re-c web port — RAM / WRAM at the ROM's addresses (docs/architecture_web.md design rule 1).
//
// ram  = $0000-$07FF (CPU RAM), wram = $6000-$7FFF (battery WRAM). Game code reads/writes them with the
// same addresses the 6502 code uses, so tools/emu/compare.py CSVs and the save image line up.
// The address constants below are *names* for RAM addresses (labels.csv bank `*` rows); web/test/core_check.mjs
// checks every one of them against web/data/ram_labels.json so they cannot drift from labels.csv.

export const R = Object.freeze({
  ppuctrl_shadow: 0x00,      // PPUCTRL shadow (NMI scroll routine sets nametable bits b0-b1)
  ppumask_shadow: 0x01,      // PPUMASK shadow, written to $2001 by the NMI (31:F412)
  scroll_shadow0: 0x02,      // drawn camera x lo / x hi / y lo / y hi ($02-$05), copied to $07D8-$07DB
  cam_x_hi: 0x03,
  cam_y_lo: 0x04,
  cam_y_hi: 0x05,
  nmi_busy_flag: 0x06,       // NMI only sets $07 and returns while != 0
  nmi_missed_flag: 0x07,
  frame_ready: 0x09,         // NMI INCs; MainLoop waits for != 0, runs the state, clears
  main_state: 0x40,
  sub_state: 0x41,
  pad_prev: 0x43, pad_hold_a: 0x45, pad_hold_b: 0x47, pad_dir: 0x49, pad_new: 0x4B, pad_act: 0x4D, pad_raw: 0x4F,
  mmc3_banksel_shadow: 0x50,
  irq_mode: 0x51,
  zp_57: 0x57,
  nmi_disable: 0x60,
  scroll_x_irq: 0x07D8,      // $07D8-$07DB = x lo, x hi, y lo, y hi used by the IRQ splits / NMI scroll
  chr_r0_play: 0x07F0,       // $07F0-$07F5 = gameplay CHR R0-R5 (ApplyChrPlay 31:F883)
  pal_buf: 0x6140,           // $6140-$615F palette buffer (PaletteUpload 31:F8CB)
});

export class Ram {
  constructor() {
    this.ram = new Uint8Array(0x800);
    this.wram = new Uint8Array(0x2000);
  }
  rd(a) {
    if (a < 0x2000) return this.ram[a & 0x7FF];
    if (a >= 0x6000 && a < 0x8000) return this.wram[a - 0x6000];
    throw new Error(`rd $${a.toString(16)}: not RAM/WRAM`);
  }
  wr(a, v) {
    if (a < 0x2000) this.ram[a & 0x7FF] = v & 0xFF;
    else if (a >= 0x6000 && a < 0x8000) this.wram[a - 0x6000] = v & 0xFF;
    else throw new Error(`wr $${a.toString(16)}: not RAM/WRAM`);
  }
  /** 256-byte page view (OAM DMA source, e.g. page 2 = OAM shadow $0200) */
  page(p) { return this.ram.subarray((p & 7) << 8, ((p & 7) << 8) + 256); }
}
