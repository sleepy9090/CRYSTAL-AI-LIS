// re-c web port — keyboard + Gamepad API -> NES button byte (A b7, B b6, Select b5, Start b4,
// Up b3, Down b2, Left b1, Right b0; the order ReadPadRaw 31:FF28 shifts them in). Browser only.

export const BTN = Object.freeze({ A: 0x80, B: 0x40, SELECT: 0x20, START: 0x10, UP: 0x08, DOWN: 0x04, LEFT: 0x02, RIGHT: 0x01 });

const KEYMAP = {
  KeyX: BTN.A, KeyK: BTN.A,
  KeyZ: BTN.B, KeyJ: BTN.B,
  ShiftRight: BTN.SELECT, Tab: BTN.SELECT,
  Enter: BTN.START,
  ArrowUp: BTN.UP, KeyW: BTN.UP,
  ArrowDown: BTN.DOWN, KeyS: BTN.DOWN,
  ArrowLeft: BTN.LEFT, KeyA: BTN.LEFT,
  ArrowRight: BTN.RIGHT, KeyD: BTN.RIGHT,
};

// standard-mapping gamepad: 0 = bottom face (A), 2 = left face (B), 8 select, 9 start, 12-15 d-pad
const PAD_BUTTONS = [[0, BTN.A], [1, BTN.A], [2, BTN.B], [3, BTN.B], [8, BTN.SELECT], [9, BTN.START],
  [12, BTN.UP], [13, BTN.DOWN], [14, BTN.LEFT], [15, BTN.RIGHT]];

export class Input {
  constructor(target = window) {
    this.keys = 0;
    target.addEventListener('keydown', (e) => {
      const b = KEYMAP[e.code];
      if (b === undefined) return;
      this.keys |= b; e.preventDefault();
    });
    target.addEventListener('keyup', (e) => {
      const b = KEYMAP[e.code];
      if (b === undefined) return;
      this.keys &= ~b; e.preventDefault();
    });
    target.addEventListener('blur', () => { this.keys = 0; });
  }

  /** button byte for pad 1 (keyboard OR first connected gamepad) */
  read() {
    let b = this.keys;
    const pads = navigator.getGamepads ? navigator.getGamepads() : [];
    for (const gp of pads) {
      if (!gp || !gp.connected) continue;
      for (const [i, bit] of PAD_BUTTONS) if (gp.buttons[i] && gp.buttons[i].pressed) b |= bit;
      const [ax, ay] = [gp.axes[0] || 0, gp.axes[1] || 0];
      if (ax < -0.5) b |= BTN.LEFT; else if (ax > 0.5) b |= BTN.RIGHT;
      if (ay < -0.5) b |= BTN.UP; else if (ay > 0.5) b |= BTN.DOWN;
      break;
    }
    // opposite directions are passed through: PadDirTable maps U+D / L+R to $FF like the ROM does
    return b & 0xFF;
  }
}
