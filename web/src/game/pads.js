// re-c web port — controller reads (ReadPads 31:FE80, ReadPadsMenu 31:FEE0, ReadPadStable 31:FF17,
// ReadPadRaw 31:FF28). Button byte = A b7, B b6, Select b5, Start b4, Up b3, Down b2, Left b1, Right b0.
// The port has no $4016 shift register: m.buttons[0/1] is what ReadPadRaw would assemble this frame.
// Both routines process pad 2 (X=1) first, then pad 1 (X=0), so pad_raw $4F ends as pad 1's byte.
import { R } from './ram.js';
import { table } from '../data.js';

/** ReadPadStable: reads until two agree; also leaves the raw value in pad_dir[x] (overwritten by ReadPads) */
function readStable(m, x) {
  const raw = m.buttons[x] & 0xFF;
  m.wr(R.pad_dir + x, raw);
  m.wr(R.pad_raw, raw);
  return raw;
}

/** ReadPads 31:FE80 — gameplay: pad_dir from PadDirTable, pad_new, A/B hold counters, pad_act */
export function readPads(m) {
  const dirTable = m.padDirTable || (m.padDirTable = table(m.data, 'PadDirTable'));
  for (let x = 1; x >= 0; x--) {
    const raw = readStable(m, x);
    m.wr(R.pad_dir + x, dirTable[raw & 0x0F]);
    const nw = ~m.rd(R.pad_prev + x) & raw & 0xFF;
    m.wr(R.pad_new + x, nw);
    m.wr(R.pad_prev + x, raw);
    if (nw & 0x80) m.wr(R.pad_hold_a + x, 0);
    if (nw & 0x40) m.wr(R.pad_hold_b + x, 0);
    let c = 0;                                   // carry -> pad_act b7 (A held: every frame from the 32nd)
    if (raw & 0x80) {
      let h = (m.rd(R.pad_hold_a + x) + 1) & 0xFF;
      if ((h & 0x1F) === 0) { h = (h - 1) & 0xFF; c = 1; }
      m.wr(R.pad_hold_a + x, h);
    }
    let y = 0;                                   // -> pad_act b6 (B held: one-frame pulse every 32 frames)
    if (raw & 0x40) {                            // BVC on the V flag of BIT pad_raw (b6)
      const h = (m.rd(R.pad_hold_b + x) + 1) & 0xFF;
      m.wr(R.pad_hold_b + x, h);
      if ((h & 0x1F) === 0) y = 0x80;
    }
    m.wr(R.pad_act + x, ((c << 7) | (y >> 1) | nw) & 0xFF);
  }
}

/** ReadPadsMenu 31:FEE0 — menus: pad_new + d-pad auto-repeat via pad_hold_a ($0C delay, then every 3) */
export function readPadsMenu(m) {
  for (let x = 1; x >= 0; x--) {
    const raw = readStable(m, x);                // pad_dir[x] keeps the raw byte here
    const prev = m.rd(R.pad_prev + x);
    let nw = ~prev & raw & 0xFF;
    if (nw & 0x0F) m.wr(R.pad_hold_a + x, 0x0C);
    if (((prev ^ raw) & 0x0F) === 0) {
      const h = (m.rd(R.pad_hold_a + x) - 1) & 0xFF;
      m.wr(R.pad_hold_a + x, h);
      if (h === 0) {
        m.wr(R.pad_hold_a + x, 3);
        nw |= raw & 0x0F;
      }
    }
    m.wr(R.pad_new + x, nw);
    m.wr(R.pad_prev + x, raw);
  }
}
