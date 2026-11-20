// re-c web port — MainStateGameplay 30:CAB6 (main_state 1): the sub_state dispatcher (session 88).
// SubstateHandlerTable 30:CADE (ptr16 x 40) -> $10/$11, SubstateBankTable 30:CB2E -> SwitchBank16, JMP ($0010).
// Only the ported substates are in SUBSTATES; the others log once and do nothing (gameplay = "Web fix:
// gameplay frames"). REVERSE.md "State Machine" + "Death / game over (session 88)".
import { R } from '../ram.js';
import { cpuRd, setR6, switchBank16 } from '../prg.js';
import { playerDeath, paletteBuildRom } from './death.js';
import { pauseScreen, pauseFromPlay } from './pause.js';
import { substEquipMenu } from './equip.js';
import { substNewGameInit, substAreaEnter } from './areaenter.js';

const SUBSTATES = new Map([
  [0, substNewGameInit],                              // 30:CA5F (s108)
  [1, substAreaEnter],                                // 30:CA2E (s108)
  [3, playerDeath],                                   // 19:B900
  [12, pauseScreen],                                  // 15:BC40 (vestigial entry; play calls it directly, s94)
  [18, substEquipMenu],                               // 30:D8C7 SELECT menu (s98)
]);

/** Routines a tools/emu/web_state.py export may stop at that are NOT reached through the substate dispatch
 *  (JSR from code the port does not run yet). Machine.loadState starts `ENTRY_POINTS[s.until](m)` as the
 *  pending generator. */
export const ENTRY_POINTS = {
  '15:BC40': pauseFromPlay,                           // START in SubstPlay: 30:CBA8 JSR PauseScreen
  // s98: a state stopped at a substate's first instruction is already past this frame's dispatch
  // (DEC $08, PlayPaletteRestore), so it starts the handler itself
  '30:D8C7': substEquipMenu,
};

/** PlayPaletteRestore 30:CC2E: slot 4 AI $1A (palette effect) -> clear it, PaletteBuild, irq_mode 2 -> 0 */
function playPaletteRestore(m) {
  if (m.rd(0x04A4) !== 0x1A) return;
  m.wr(0x04A4, 0);
  setR6(m, 0x1A);
  paletteBuildRom(m, 0);
  if (m.rd(R.irq_mode) === 2) m.wr(R.irq_mode, 0);
}

/** MainStateGameplay 30:CAB6 */
export function mainState1(m) {
  const s = m.rd(R.sub_state);
  if (s !== 8 && s !== 7) playPaletteRestore(m);
  m.wr(0x08, (m.rd(0x08) - 1) & 0xFF);
  m.wr(0x10, cpuRd(m, 0xCADE + s * 2));
  m.wr(0x11, cpuRd(m, 0xCADF + s * 2));
  switchBank16(m, cpuRd(m, 0xCB2E + s));
  const fn = SUBSTATES.get(s);
  if (fn) return fn(m);
  const key = `sub_state ${s}`;
  if (!m.unported.has(key)) { m.unported.add(key); m.log(`${key} (30:CAB6) not ported yet`); }
  return undefined;
}
