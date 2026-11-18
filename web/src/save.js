// re-c web port — battery WRAM in localStorage (Ph7 web 7/8, s57).
//
// The cartridge keeps all 8 KB of WRAM $6000-$7FFF across power cycles, and the game's save system lives in it
// (REVERSE.md "Save / load module" + "Save data + WRAM layout": current image $7D00-$7FFF, backup mirror
// $6D00-$6FFF, slot images $7100-$73FF / $7400-$76FF with checksums $70F0-$70F5, "AMI" tag $71FD, all checked by
// BootSaveCheck 31:F0B0 at boot). The port stores the same 8 KB, byte for byte, so the ROM's own code finds the
// layout it expects; nothing is parsed or re-encoded here.
//
//   loadWram(storage, wram) -> true if a saved image was restored (call before Machine.reset, like power-on)
//   saveWram(storage, wram) -> writes the image (base64)
//   new WramSaver(storage, wram).poll() -> saves when the bytes changed since the last save
export const KEY = 're-c.wram.v1';
const SIZE = 0x2000;

const toB64 = (u8) => {
  let s = '';
  for (let i = 0; i < u8.length; i += 0x1000) s += String.fromCharCode(...u8.subarray(i, i + 0x1000));
  return globalThis.btoa ? btoa(s) : Buffer.from(u8).toString('base64');
};
const fromB64 = (s) => (globalThis.atob
  ? Uint8Array.from(atob(s), (c) => c.charCodeAt(0))
  : new Uint8Array(Buffer.from(s, 'base64')));

export function loadWram(storage, wram) {
  let s = null;
  try { s = storage.getItem(KEY); } catch { return false; }
  if (!s) return false;
  const b = fromB64(s);
  if (b.length !== SIZE) return false;
  wram.set(b);
  return true;
}

export function saveWram(storage, wram) {
  try { storage.setItem(KEY, toB64(wram.subarray(0, SIZE))); return true; } catch { return false; }
}

export class WramSaver {
  constructor(storage, wram) { this.storage = storage; this.wram = wram; this.last = wram.slice(0, SIZE); }
  poll() {
    const w = this.wram;
    for (let i = 0; i < SIZE; i++) {
      if (w[i] !== this.last[i]) { this.last.set(w.subarray(0, SIZE)); return saveWram(this.storage, w); }
    }
    return false;
  }
}
