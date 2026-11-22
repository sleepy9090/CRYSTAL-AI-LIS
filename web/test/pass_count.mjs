// node web/test/pass_count.mjs STATE.json LAST [--out FILE] [--stages] [keys..] — s210 (gap (c) lag model study): per frame of a
// load-state run, the number of Machine rd / wr calls made by the port (main-loop pass + NMI + IRQ work), as
// `frame rd wr` lines (tools.emu frame numbers). Compare with `python3 -m tools.emu.pass_cost` totals
// (tools/pass_fit.py) to see how well the port's RAM-access count predicts a pass's CPU cycles.
// s211 --stages: one line per frame `frame top=rd/wr/ppu upd=.. cam=.. spr=.. col=.. nmi=..`, bucketed by
// m.passStage (set by states/play.js substPlayBody at each JSR of SubstPlay, = pass_cost's columns; `top` is
// everything before ObjUpdateAll, `col` runs to the pass end, `nmi` = render + NMI / IRQ work); rom = prg.js
// cpuRd calls (ROM table / indirect reads, m.romRd). Fit with `python3 tools/pass_fit.py COST COUNTS --stages`.
import { readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const argv = process.argv.slice(2);
const oi = argv.indexOf('--out'), outFile = oi >= 0 ? argv.splice(oi, 2)[1] : null;
const si = argv.indexOf('--stages'), stages = si >= 0 && argv.splice(si, 1).length > 0;
const [stateFile, last, ...keyArgs] = argv;
if (!last) { console.log('usage: node web/test/pass_count.mjs STATE.json LAST [--out F] [--stages] [keys..]'); process.exit(1); }
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const state = JSON.parse(readFileSync(stateFile, 'utf8'));
const keys = parseKeys(keyArgs);
const m = new Machine(data, { log: () => {} });
m.reset();
m.loadState(state);
// s213: `colh` = ObjCollideAll's hit handlers (collide.js), `colc` = the exact ROM cycles collide.js adds to m.cyc
// during `col` (record loop + ObjOverlapScan, handlers excluded)
const ST = ['top', 'upd', 'cam', 'spr', 'col', 'colh', 'nmi'];
let cnt = {}, cyc = {};
let stage = 'top', rom0 = 0, cyc0 = 0;
const bucket = () => cnt[stage] ??= [0, 0, 0];
const flushRom = () => {
  bucket()[2] += m.romRd - rom0; rom0 = m.romRd;
  cyc[stage] = (cyc[stage] || 0) + m.cyc - cyc0; cyc0 = m.cyc;
};
Object.defineProperty(m, 'passStage', { get: () => stage, set: (s) => { flushRom(); stage = s; } });
const r0 = m.rd.bind(m), w0 = m.wr.bind(m);
m.rd = (a) => { bucket()[0]++; return r0(a); };
m.wr = (a, v) => { bucket()[1]++; w0(a, v); };
const rf0 = m.ppu.renderFrame.bind(m.ppu);
m.ppu.renderFrame = () => { m.passStage = 'nmi'; return rf0(); };
const f0 = state.frame0 ?? state.frame;
const out = [];
while (f0 + m.frame <= Number(last)) {
  const f = f0 + m.frame;
  cnt = {}; cyc = {};
  m.passStage = 'top';
  m.runFrame(keys.get(f) || 0);
  flushRom();
  const tot = Object.values(cnt).reduce((s, b) => [s[0] + b[0], s[1] + b[1]], [0, 0]);
  // s212: objects.js sets passStage `sX:ID` (decimal) per active slot dispatch; `upd` sums them with the rest,
  // and each dispatch is also listed as `XX:YY=rd/wr/rom` (hex, = pass_cost --slots tokens)
  const sl = Object.keys(cnt).filter((k) => /^s\d/.test(k)).map((k) => {
    const [x, id] = k.slice(1).split(':').map(Number), b = cnt[k];
    const u = cnt.upd ??= [0, 0, 0];
    for (let i = 0; i < 3; i++) u[i] += b[i];
    return [x, `${x.toString(16).toUpperCase().padStart(2, '0')}:${id.toString(16).toUpperCase().padStart(2, '0')}=${b.join('/')}`];
  }).sort((a, b) => a[0] - b[0]).map((t) => t[1]);
  if (stages) out.push(`${f} ` + ST.map((s) => `${s}=${(cnt[s] || [0, 0, 0]).join('/')}`).join(' ') + ` colc=${cyc.col || 0}` + (sl.length ? '  ' + sl.join(' ') : ''));
  else out.push(`${f} ${tot[0]} ${tot[1]}`);
}
if (outFile) writeFileSync(outFile, out.join('\n') + '\n');
else console.log(out.join('\n'));
