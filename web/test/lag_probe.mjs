// node web/test/lag_probe.mjs STATE.json LAST [--all] [keys..] — s215: the lag model (web/src/game/lagmodel.js) per
// frame of a load-state run: start line S, estimated pass cycles (stages + H0 + HUD if past L189), budget
// (503 - S) * 341/3, margin, LAG. Only frames with a whole SubstPlay pass are listed (--all: every frame).
// Compare with `python3 tools/lag_threshold.py` / `tools.emu.pass_cost` rows (port row F = tools.emu pass F-1).
import { readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { passEstimate, CPL } from '../src/game/lagmodel.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const argv = process.argv.slice(2);
const ai = argv.indexOf('--all'), all = ai >= 0 && argv.splice(ai, 1).length > 0;
const oi = argv.indexOf('--out'), outFile = oi >= 0 ? argv.splice(oi, 2)[1] : null;
const out = [];
const spi = argv.indexOf('--splits'), splits = spi >= 0 && argv.splice(spi, 1).length > 0;   // IRQ split latches per frame
const [stateFile, last, ...keyArgs] = argv;
if (!last) { console.log('usage: node web/test/lag_probe.mjs STATE.json LAST [--all] [keys..]'); process.exit(1); }
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const state = JSON.parse(readFileSync(stateFile, 'utf8'));
const keys = parseKeys(keyArgs);
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
const f0 = state.frame0 ?? state.frame;
while (f0 + m.frame <= Number(last)) {
  const f = f0 + m.frame, s = m.lagLine, lag0 = m.lagFrames || 0;
  const sp = m.irqSplits.slice();                   // this frame's splits (the NMI at the end rebuilds the list)
  if (splits) out.push(`f${f} splits ${sp.map((q) => `H${q.handler}@${q.latch}`).join(' ')}`);
  m.runFrame(keys.get(f) || 0);
  if (!m.lagPass && !all) continue;
  const e = m.lagPass ? passEstimate(m.lagCnt, m.lagCyc, s, sp) : { t: 0, hud: 0, parts: {} };
  const parts = e.parts, p = e.t - e.hud, h = e.hud;
  const b = (503 - s) * CPL;
  out.push(`f${f} L${s} est ${Math.round(p + h)} (hud ${Math.round(h)}) budget ${Math.round(b)} margin ${Math.round(p + h - b)}`
    + `${(m.lagFrames || 0) !== lag0 ? ' LAG' : ''} next L${m.lagLine} irq_mode ${m.rd(0x51)}`
    + (m.lagPass ? '  ' + Object.entries(parts).map(([k, v]) => `${k} ${Math.round(v)}`).join(' ') : ''));
}
if (outFile) writeFileSync(outFile, out.join('\n') + '\n');
else console.log(out.join('\n'));
