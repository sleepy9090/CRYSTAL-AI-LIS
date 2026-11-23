// re-c web port — s215 (gap (c2)): CPU-overrun (lag frame) model for SubstPlay passes.
//
// The ROM's main loop has no cycle budget: when a SubstPlay 30:CB62 pass is still running at the next NMI, that
// NMI finds frame_ready != 0 (no OAM DMA, no scroll copy) and the next frame has no pass (s114, Machine.runFrame).
// The port has no CPU, so it estimates each pass's ROM cycles from what it did, with the linear fits of the
// s210-s214 study (REVERSE.md Verification Log row "Main-loop pass cost"; tools/passfit_runs.sh):
//   - Machine counts rd / wr / cpuRd calls per m.passStage (`top upd cam spr col colh` + one `sX:ID` bucket per
//     ObjUpdateAll dispatch, `nmi` = render + IRQ + NMI work, `snd` = SoundUpdate) and m.cyc per stage
//     (exact ROM cycles of ObjCollideAll / ObjOverlapScan / SubstPlayTail, s213);
//   - pass cycles = sum of the per-stage fits (per-AI fits for the ObjUpdateAll dispatches, `ai*` for other ids);
//   - + 202 (H0 split IRQ at L142) and, if the pass is still running at L189, the HUD IRQ (IrqHandler1Hud
//     31:F488 + its SoundUpdate, fitted on the `snd` bucket);
//   - the pass starts at line S, set by the previous NMI's length (fitted on the `nmi` bucket; tools.emu shows
//     1296-1315 cycles -> L252, 1599-1618 -> L255, 1819-1828 -> L257, 1992-2010 -> L258);
//   - s214 rule (exact on all 929 passes of the full $81 / $59 runs): the pass lags iff its cycles without the
//     overrun NMI > (503 - S) * 341/3.
// Coefficients (s215): bootstrapped. The s214 fits (short $81 + $59 runs, 200 passes) aligned the port with tools.emu
// far enough to refit on the full 480-frame runs: pass_fit.py --stages --ai --skip-lag on tools.emu L81 (to
// f2922) + L59 vs this model's pass_count.mjs runs = 846 passes, all-stage rms 49 / max 221 (col rms 13: the
// hit handlers now from ~20 hit passes, not 3); int_fit.py on the same 845 frames: hud rms 90, nmi rms 11 / max 58
// (the s214 nmi fit had 0.37 per cpuRd from 2 samples, so the 70-cpuRd 1993-cycle NMIs came out ~1380 = wrong
// start line L252 instead of L258). Out of sample: area $5A (group5c_check.mjs --last) 479/479 rows incl. its 6 lags.
// Only whole passes (SubstPlay started and ended in this frame, m.lagPass) that no ported heuristic (AI $08 /
// AI $70, hit-stop waits) already made lag are judged. m.lagModel = false switches it off (pass_count.mjs --no-lag).

export const CPL = 341 / 3;

// [rd, wr, rom, const] per stage / per AI id (hex); col = [colc, colh rd, colh wr, colh rom, const]
export const LAG_COEF = {
  // top: the fit gave 313.4 per cpuRd + 0, because every fitted pass has 3 cpuRd there (the constant went into the
  // cpuRd term); kept equal at 3 (940.3) with a generic 10 per cpuRd, else a town pass with ~19 more (group18 f2536)
  // got +5900 and a false lag
  top: [7.5165, 7.3263, 10, 910.27],
  cam: [4.1602, 10.1350, -3.8117, 147.433],
  spr: [5.8161, 9.1058, 15.0912, -791.159],
  col: [1.00101, 12.8345, 8.8081, -33.6003, 74.960],
  updrest: [-3.8021, 21.1948, -2.9417, -6.357],
  ai: {
    '*': [5.6430, 7.1299, -2.0833, 81.362],
    '01': [-7.1061, 9.1957, 33.1511, 455.474],
    '03': [2.9947, 0.6810, 49.3337, 254.592],
    '05': [5.4621, 0, 51.4585, 0],
    '10': [5.4491, 9.5786, -30.9644, 193.332],
    '11': [-0.9333, 16.7333, 0, 0],
    '1D': [8.7156, -4.8373, 27.7646, 83.599],
    '21': [3.3420, 11.2434, -9.2601, 92.470],
    '2A': [4.8995, 9.4120, -10.5512, 74.280],
    '2E': [3.9254, 11.0730, -12.2057, 145.090],
    '5C': [8.6247, 5.9928, -13.3392, 73.670],
    '5D': [-14.4914, 25.1229, 115.3878, -595.622],
    '5E': [7.3571, 15.5179, 0, 0],
    '5F': [16.25, 0, 0, 0],
    '70': [4.7683, 13.3423, -14.8028, 0],
    // s218 (irq_mode 7): pass_fit.py --coef (the fits above) --fit-new on the aligned boss runs: finalboss.state
    // to f3145 (445 passes, $6B rms 11 / max 62) and bosse2.state to f2914 ($62 30 dispatches, rms 1)
    '1B': [-11.5462, 29.6485, 1.6757, 0],
    '3F': [23.8571, 0, 0, 0],
    '62': [4.8190, 8.3093, -5.1978, 1829.689],
    '6B': [-3.7938, 17.6505, -9.1679, 1615.807],
    '7F': [23.4286, 0, 0, 0],
    // s219: pass_fit.py --coef (the fits above, web/test/lag_coef_dump.mjs) --fit-new on the s217 480-frame runs
    // group84 + group88 (gfx/group8{4,8}/cost.txt vs pass_count.mjs --stages, 954 passes): all-stage rms 100 -> 61
    // (ai* was ~20 high per $34 / $4E dispatch); per AI rms: $34 0 (3353), $4E 0 (420), $40 4 (1453), $07 0,
    // $7B 2, $7D 0 (was 15 / 26 / 11 / 15 / 22 / 11 with ai*)
    '07': [15, 0, 0, 0],
    '34': [8, 11.6667, 0, 0],
    '40': [3.6636, 7.5108, 2.9856, 93.570],
    '4E': [7, -2.25, 26.1544, 82.941],
    '7B': [3.1, 5.4, 15.7, 83.4],
    '7D': [2.8216, 8.6151, 3.3821, 78.318],
  },
  hud: [12.2281, -5.4246, 14.1850, 436.274],
  nmi: [17.4678, -28.7772, 10.9464, 611.152],
};
const H0 = 202;                 // s211: IrqHandler0 (L142 split) cycles, in every pass that runs past L142
const HUD_LINE = 189;           // s214: the HUD IRQ's line in the ints log (I189)

const dot = (c, b) => c[0] * b[0] + c[1] * b[1] + c[2] * b[2] + c[3];
const Z = [0, 0, 0];

/** ROM cycles of the pass held in cnt (stage -> [rd, wr, rom]) / cyc (stage -> m.cyc delta), interrupts excluded */
export function passCycles(cnt, cyc, parts = null) {
  const C = LAG_COEF;
  const p = parts || {};
  p.top = dot(C.top, cnt.top || Z); p.cam = dot(C.cam, cnt.cam || Z); p.spr = dot(C.spr, cnt.spr || Z);
  p.upd = dot(C.updrest, cnt.upd || Z);
  const h = cnt.colh || Z;
  p.col = C.col[0] * (cyc.col || 0) + C.col[1] * h[0] + C.col[2] * h[1] + C.col[3] * h[2] + C.col[4];
  for (const k in cnt) {
    if (k[0] !== 's' || k[1] > '9') continue;           // `sX:ID` dispatch buckets, not spr / snd
    const id = Number(k.slice(k.indexOf(':') + 1)).toString(16).toUpperCase().padStart(2, '0');
    p.upd += dot(C.ai[id] || C.ai['*'], cnt[k]);
  }
  return p.top + p.cam + p.spr + p.upd + p.col;
}

/** s218: NMI 31:F3EC with irq_mode 7 / 9 (bosses / their message windows): when the DMA branch ran
 *  (frame_ready was 0), the scroll copy is redone from slot $13 (LDA $83/$A3/$C3/$E3, STA $07D8-$07DB) = 4 x 7
 *  + the CMP / branch path: mode 7 +27, mode 9 +24 cycles over mode 0's BNE F40A. tools.emu: the E2 boss's NMIs
 *  are 1340 / 1643 cycles where mode 0's are 1315 / 1618 (the port's nmi bucket does not count these reads). */
export const NMI_MODE_EXTRA = { 7: 27, 9: 24 };

/** start line of the next pass from this frame's `nmi` bucket (NMI 31:F3B6 length; extra = NMI_MODE_EXTRA) */
export function startLine(nmiCnt, extra = 0) {
  const n = dot(LAG_COEF.nmi, nmiCnt || Z) + extra;
  if (n < 1450) return 252;
  if (n < 1720) return 255;
  if (n < 1910) return 257;
  if (n < 2050) return 258;
  return 241 + Math.floor((n + 20) / CPL);
}

/** s215: pass cycles t plus the interrupts it runs into, from this frame's IRQ splits (m.irqSplits, scheduled by
 *  the last NMI): H0 IrqWrapSplit 31:F455 at its latch - 1 (tools.emu I142 for latch 143; it re-arms H1) and the
 *  HUD IRQ (H1 IrqHandler1Hud, latch 190 = I189) when the pass is still running there. hud = its cost or null
 *  (only H0 is added, for the running estimate in lateSoundCheck). */
function withInts(t, splits, s, hud) {
  let hasHud = false;
  for (const q of splits) {
    if (q.handler === 0) { hasHud = true; if (t > (262 - s + q.latch - 1) * CPL) t += H0; }
    else if (q.handler === 1) hasHud = true;
  }
  return { t, hud: hasHud && t > (262 - s + HUD_LINE) * CPL };
}

/** s215: the fits cover NmiMode 0 (irq_mode $51 = 0: HUD split, the normal field mode of $81 / $59 / $5A).
 *  s218: also irq_mode 7 (bosses: the same NmiMode0 F6D6, scroll from slot $13): NMI +27 (NMI_MODE_EXTRA), own
 *  fits for AIs $62 / $6B / $3F / $7F / $1B; checked on the whole bosse2 run (7/7 lags) and the final boss dive
 *  (27 whole-pass lags f3146-3218). The L259 / L260 start lines the s215 note blamed on mode 7 are real: the rain
 *  phase's 2198-cycle NMIs. m.lagMode7 = false limits it to mode 0. Not judged: mode 2 (NmiMode2 F751, set in
 *  play by AI $70's Ai70FlashIrq 27:BCE8; its overruns are the ported AI $70 heuristics) and 1 / 3-6 / 9 (messages,
 *  menus, title, ending, dead 4: no SubstPlay pass). */
export const lagDomain = (m) => m.mem.rd(0x51) === 0 || (m.lagMode7 && m.mem.rd(0x51) === 7);

/** true if the pass (started at line s) overruns the NMI at L241 of the next frame */
export function passLags(cnt, cyc, s, splits) {
  return passEstimate(cnt, cyc, s, splits).t > (503 - s) * CPL + LAG_MARGIN;
}

// s215: the boundary offset. tools.emu's own boundary lies in the window -99 .. 0 (s214, tools/lag_threshold.py), and the
// estimate runs low near it: the closest LAG passes of the full $81 / $59 runs come out at port margin -57 / -96
// (est - emu -138 / -96), the closest non-LAG ones at -170 / -177 / -204 (tools/lag_model_check.py) -> midpoint.
export const LAG_MARGIN = -130;

/** { t: pass + interrupt cycles, hud: HUD IRQ cycles inside t (0 if the pass ends before it), parts } */
export function passEstimate(cnt, cyc, s, splits) {
  const parts = {};
  const r = withInts(passCycles(cnt, cyc, parts), splits, s);
  const hud = r.hud ? dot(LAG_COEF.hud, cnt.snd || Z) : 0;
  return { t: r.t + hud, hud, parts };
}

/** HUD IRQ cost estimate of this frame's `snd` bucket (web/test/lag_probe.mjs) */
export const hudCycles = (cnt) => dot(LAG_COEF.hud, cnt.snd || Z);

/** s215: PlaySound 30:C125 inside a SubstPlay pass. tools.emu runs the HUD IRQ's SoundUpdate at L189, in the middle
 *  of a long pass; SoundUpdate takes at most one queued command (31:F843), so a sound queued after that point is
 *  started one frame later than in the port, which runs the IRQ after the whole pass (s214: `$81` f2596 / f2597
 *  HUD 1347 / 3355). Counts the queue entries the pass adds after the estimated L189 point in m.sndLate; the
 *  HUD SoundUpdate (Machine.soundUpdate) skips the command branch when only late entries are queued. */
export function lateSoundCheck(m) {
  if (!m.lagModel || m.lagStart !== m.frame || !lagDomain(m)) return;
  m.passStage = m.passStage;                                    // book the pending cpuRd / m.cyc deltas
  if (withInts(passCycles(m.lagCnt, m.lagCyc), m.irqSplits, m.lagLine).hud) m.sndLate++;
}
