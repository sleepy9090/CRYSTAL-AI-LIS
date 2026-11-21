// re-c web port — camera + exits (session 108; "Web fix: gameplay frames" (c)). REVERSE.md "Camera" /
// "World interaction". Ported instruction by instruction, every zero-page temp included.
//
//   cameraTick(m)     generator = CameraTick 31:E8F6 (boss_cam_lock 1-$7F: DEC + the shake 31:EADF; else
//                     CameraUpdate + ExitLookup). Returns true when ExitLookup took an exit: the ROM then PLAs
//                     two return addresses (31:EB68), i.e. CameraTick's CALLER returns at once — callers
//                     must `if (yield* cameraTick(m)) return;`.
//   cameraUpdate(m)   generator = CameraUpdate 31:E90A (CameraFollow, BgColRender / BgRowRender when the 8 px
//                     cell changes, PpuQueueAttrAll when $2F != 0, cam_ofs_x ease, $02-$05 = drawn scroll)
//   cameraClampX(m)   CameraClampX 31:EA72 (x 0..area_w screens, y 0..area_h + $28, cam_ofs_y)
//   exitLookup(m)     ExitLookup 31:EB09 (k3 records `x y area entry` at the player's 16 px cell) -> true = exit
import { R } from '../ram.js';
import { cpuRd, indY, setR6, switchBank16 } from '../prg.js';
import { tempAddOffset } from '../obj/util.js';
import { bgColRender, bgRowRender } from './bgstream.js';
import { queueAttrAll } from './ppuqueue.js';

const P = 0x34;                       // $34-$37 pos_x_lo .. pos_y_hi
const CAM = 0x3C;                     // $3C-$3F camera target
const AREA_W = 0x62FC, AREA_H = 0x62FD;
const CAM_LOCK = 0x07D7, SHAKE = 0x07D6;

/** CameraTick 31:E8F6 */
export function* cameraTick(m) {
  const l = m.rd(CAM_LOCK);
  if (l !== 0 && !(l & 0x80)) {
    m.wr(CAM_LOCK, l - 1);
    cameraShake(m);                   // JMP 31:EADF
    return false;
  }
  yield* cameraUpdate(m);
  return exitLookup(m);
}

/** 31:E993: $30/$31 = cam_ofs_x/y, 26:9824, $02-$05 = $34-$37 */
function scrollFromPos(m) {
  m.wr(0x30, m.rd(0x3A));
  m.wr(0x31, m.rd(0x3B));
  setR6(m, 0x1A);
  tempAddOffset(m);
  for (let x = 3; x >= 0; x--) m.wr(R.scroll_shadow0 + x, m.rd(P + x));
}

/** CameraUpdate 31:E90A */
export function* cameraUpdate(m) {
  cameraFollow(m);
  m.wr(0x2E, 0); m.wr(0x2F, 0);
  for (let i = 0; i < 4; i++) m.wr(P + i, m.rd(CAM + i));
  setR6(m, 0x1A);
  tempAddOffset(m);
  cameraClampX(m);
  for (let x = 3; x >= 0; x--) m.wr(0x28 + x, m.rd(P + x));
  m.wr(0x35, m.rd(0x3D));
  m.wr(0x34, m.rd(0x3C));
  if ((m.rd(0x3C) ^ m.rd(0x28)) & 0xF8) yield* bgColRender(m);
  m.wr(0x34, m.rd(0x28));
  m.wr(0x35, m.rd(0x29));
  m.wr(0x37, m.rd(0x3F));
  m.wr(0x36, m.rd(0x3E));
  if ((m.rd(0x3E) ^ m.rd(0x2A)) & 0xF8) yield* bgRowRender(m);
  if (m.rd(0x2F)) queueAttrAll(m);
  if (m.rd(AREA_W) === 0) m.wr(0x3A, 0);
  else {                                          // E971: ease cam_ofs_x toward +1 (step <= 0) / -8 (step > 0)
    let y = m.rd(0x3A);
    const a = m.rd(0x30);
    if (a === 0 || (a & 0x80)) { if (y !== 0x01) y = (y + 1) & 0xFF; }
    else if (y !== 0xF8) y = (y - 1) & 0xFF;
    m.wr(0x3A, y);
  }
  for (let x = 3; x >= 0; x--) { const v = m.rd(0x28 + x); m.wr(CAM + x, v); m.wr(P + x, v); }
  scrollFromPos(m);
}

/** CamStepClamp 31:EA2F: signed step -> -3..+3 */
function camStepClamp(a) {
  if (a === 0) return 0;
  if (!(a & 0x80)) return a < 3 ? a : 3;
  return a >= 0xFE ? a : 0xFD;
}

/** CameraFollow 31:E9AD: $30/$31 camera step */
function cameraFollow(m) {
  m.wr(0x30, 0); m.wr(0x31, 0); m.wr(0x07D5, 0);
  if (m.rd(CAM_LOCK) & 0x80) { bossCamCenter(m); return; }
  let s = m.rd(0xB0) - m.rd(0x3E);                // E9BE: y in screen rows (240-line wrap: -$10)
  if (s < 0) s -= 0x10;
  const dy = s & 0xFF;
  m.wr(0x10, dy);
  if (dy < 0x68) m.wr(0x31, camStepClamp((dy - 0x68) & 0xFF));
  else if (dy >= 0x78) m.wr(0x31, camStepClamp((dy - 0x78) & 0xFF));
  if ((m.rd(0x0340) & 0x80) || (m.rd(R.pad_prev) & 0x03)) {   // E9E5: knockback or L/R held
    const dx = (m.rd(0x70) - m.rd(0x3C)) & 0xFF;
    m.wr(0x10, dx);
    if (dx < 0x68) m.wr(0x30, camStepClamp((dx - 0x68) & 0xFF));
    else if (dx >= 0xA0) m.wr(0x30, camStepClamp((dx - 0xA0) & 0xFF));
    return;
  }
  // E9FE: re-centre 1 px on the 16 px cell (+8 when obj_screen_x < $88, ASL of the difference)
  let a = m.rd(0x70) & 0xF0;
  if (!(((m.rd(0x05C0) - 0x88) & 0xFF) & 0x80)) a = (a + 8) & 0xFF;
  const dx = (a - m.rd(0x3C)) & 0xFF;
  m.wr(0x10, dx);
  let r;
  if (dx < 0x68) r = (dx - 0x68) & 0xFF;
  else {
    if (dx < 0x98) return;
    r = (dx - 0x98) & 0xFF;
    if (r === 0) return;
  }
  m.wr(0x30, (r & 0x80) ? 0xFF : 0x01);           // EA21: LDY #0 STY $30, BPL -> INC; else DEC DEC INC
}

/** 31:EA41 (boss_cam_lock b7, odd frames): step $3C / $3E one px toward 0; $07D5 = 1 once both are 0 */
function bossCamCenter(m) {
  if (!(m.rd(0x08) & 1)) return;
  const step = (x, y) => {
    const a = m.rd(CAM + y);
    if (a === 0) return;
    m.wr(0x30 + x, (m.rd(0x30 + x) + ((a & 0x80) ? 1 : -1)) & 0xFF);
    m.wr(0x07D5, 0);
  };
  step(0, 0);
  step(1, 2);
  if ((m.rd(0x3C) | m.rd(0x3E)) === 0) m.wr(0x07D5, 1);
}

/** CameraClampX 31:EA72 (Y = 0 throughout) */
export function cameraClampX(m) {
  const w = m.rd(AREA_W);
  if (w === 0) { m.wr(0x34, 0); m.wr(0x35, 0); }
  else if (m.rd(0x35) & 0x80) {
    if (m.rd(0x34) < 0xF8) { m.wr(0x30, 0); m.wr(0x34, 0xF8); }
  } else if (m.rd(0x35) >= w) {
    m.wr(0x34, 0); m.wr(0x35, w); m.wr(0x30, 0);
  }
  const h = m.rd(AREA_H);
  if (h === 0) {                                  // EAA6
    m.wr(0x37, 0); m.wr(0x36, 0);
    m.wr(0x3B, m.rd(R.area_id) === 0x5F ? 0 : 0xFC);
    return;
  }
  m.wr(0x3B, 8);
  if (m.rd(0x37) & 0x80) { m.wr(0x37, 0); m.wr(0x36, 0); m.wr(0x31, 0); return; }
  if (m.rd(0x37) >= h && m.rd(0x36) >= 0x28) { m.wr(0x37, h); m.wr(0x36, 0x28); m.wr(0x31, 0); }
}

/** 31:EADF: the boss-landing shake ($07D6 frames, cam_ofs_x from 31:EB05[$08 & 3] = 0 -4 0 +4) */
function cameraShake(m) {
  const s = m.rd(SHAKE);
  if (s === 0) return;
  m.wr(SHAKE, s - 1);
  if (s - 1 === 0) { m.wr(0x3A, 0); return; }
  m.wr(0x3A, cpuRd(m, 0xEB05 + (m.rd(0x08) & 3)));
  for (let x = 3; x >= 0; x--) m.wr(P + x, m.rd(CAM + x));
  scrollFromPos(m);
}

/** AreaSubTable 30:C7A5 (A = k): SwitchBank16(5); ($10) = header 10:8300/8400[$6C][k]; Y = 0 */
export function areaSubTable(m, k) {
  switchBank16(m, 5);
  const id = m.rd(R.area_id), base = (id & 0x80) ? 0x8400 : 0x8300, y = (id << 1) & 0xFF;
  m.wr(0x10, cpuRd(m, base + y));
  m.wr(0x11, cpuRd(m, base + y + 1));
  const lo = indY(m, 0x10, k << 1), hi = indY(m, 0x10, (k << 1) + 1);
  m.wr(0x11, hi);
  m.wr(0x10, lo);
}

/** ExitLookup 31:EB09: $12 / $13 = the player's 16 px cell x / y; in SubstPlay a matching k3 record sets
 *  area_id / area_entry and sub_state 1 (SubstAreaEnter) -> true */
export function exitLookup(m) {
  const [hit, y] = exitScan(m);
  if (!hit || m.rd(R.sub_state) !== 8) return false;
  m.wr(R.area_id, indY(m, 0x10, y));
  m.wr(0x6D, indY(m, 0x10, y + 1));
  m.wr(R.sub_state, 1);
  return true;
}

/** ExitLookup 31:EB09 up to its sub_state test: [matched, Y]. On a match Y indexes the record's area byte
 *  at ($10); without one Y is past the $FF pair (or 0 after a wrap). s121: ShopWalkOut 30:DAA8 calls
 *  ExitLookup outside sub_state 8 and reads ($10),Y itself. */
export function exitScan(m) {
  m.wr(0x12, ((m.rd(0x70) >> 4) | (m.rd(0x90) << 4)) & 0xFF);
  m.wr(0x13, ((m.rd(0xB0) >> 4) | (m.rd(0xD0) << 4)) & 0xFF);
  areaSubTable(m, 3);
  let y = 0;
  for (;;) {
    m.wr(0x14, indY(m, 0x10, y)); y = (y + 1) & 0xFF;
    m.wr(0x15, indY(m, 0x10, y)); y = (y + 1) & 0xFF;
    if (m.rd(0x14) === 0xFF) return [false, y];
    if (m.rd(0x15) === m.rd(0x13) && m.rd(0x14) === m.rd(0x12)) return [true, y];
    y = (y + 2) & 0xFF;
    if (y === 0) return [false, y];
  }
}
