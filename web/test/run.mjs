// node web/test/run.mjs [options] — run the web port headless (Ph7 web 3/8), the Node twin of
// `python3 -m tools.emu`. Options (same meaning as tools.emu where the name matches):
//   --frames N            frames to run (default 60)
//   --keys SPEC ...       pad-1 script, SPEC = FRAME:BUTTONS[:LEN] (hold LEN frames, default 2) or @FILE;
//                         frame numbers are tools.emu ppu.frame_no numbers (see --frame0)
//   --frame0 N            script/CSV frame number of the port's first frame (default 214 = the frame
//                         tools.emu runs the first MainLoop pass in, Verification Log row "Boot" s41;
//                         the port skips the 212-frame cold-boot intro). 0 = count from the port's frame 0.
//   --dump-screen F.png   last finished frame (--scale S)
//   --shots F,F,... DIR   also write DIR/web_NNNN.png (1x) after each listed frame_no (Ph7 web 8/8)
//   --dump-ram F / --dump-wram F   2KB RAM / 8KB WRAM, raw (same as tools.emu)
//   --compare F.csv       one row per frame, tools/emu/compare.py columns (diff with tools/compare_frames.py)
//   --quiet               no unported-routine log lines
// Button names: A B SELECT START UP DOWN LEFT RIGHT (input.js BTN, ROM order A=b7 ... RIGHT=b0).
import { readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { parseKeys, compareColumns, compareRow, encodePng, fbToRgb } from './harness.mjs';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));

function parseArgs(argv) {
  const a = { frames: 60, keys: [], frame0: 214, scale: 1, quiet: false };
  for (let i = 0; i < argv.length; i++) {
    const k = argv[i], v = () => { if (i + 1 >= argv.length) throw new Error(`${k} needs a value`); return argv[++i]; };
    switch (k) {
      case '--frames': a.frames = parseInt(v(), 10); break;
      case '--frame0': a.frame0 = parseInt(v(), 10); break;
      case '--keys': while (i + 1 < argv.length && !argv[i + 1].startsWith('--')) a.keys.push(argv[++i]); break;
      case '--dump-screen': a.screen = v(); break;
      case '--shots': a.shots = new Set(v().split(',').map((s) => parseInt(s, 10))); a.shotDir = v(); break;
      case '--scale': a.scale = parseInt(v(), 10); break;
      case '--dump-ram': a.ram = v(); break;
      case '--dump-wram': a.wram = v(); break;
      case '--compare': a.compare = v(); break;
      case '--quiet': a.quiet = true; break;
      case '-h': case '--help':
        console.log(readFileSync(fileURLToPath(import.meta.url), 'utf8').split('\nimport')[0]); process.exit(0);
      // eslint-disable-next-line no-fallthrough
      default: throw new Error(`unknown option ${k} (--help)`);
    }
  }
  return a;
}

const a = parseArgs(process.argv.slice(2));
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const logs = [];
const m = new Machine(data, { log: (s) => { logs.push(s); if (!a.quiet) console.error(`[re-c] ${s}`); } });
m.reset();
const keys = parseKeys(a.keys);
const rows = a.compare ? [compareColumns().join(',')] : null;
const t0 = performance.now();
for (let i = 0; i < a.frames; i++) {
  const f = a.frame0 + m.frame;
  m.runFrame(keys.get(f) || 0);
  if (rows) rows.push(compareRow(m, f).join(','));
  if (a.shots && a.shots.has(f)) {
    const { w, h, rgb } = fbToRgb(m.ppu.fb, data.palettes.nesRgb, 1);
    writeFileSync(path.join(a.shotDir, `web_${String(f).padStart(4, '0')}.png`), encodePng(w, h, rgb));
  }
}
const dt = (performance.now() - t0) / 1000;
if (rows) writeFileSync(a.compare, rows.join('\n') + '\n');
if (a.screen) { const { w, h, rgb } = fbToRgb(m.ppu.fb, data.palettes.nesRgb, a.scale); writeFileSync(a.screen, encodePng(w, h, rgb)); }
if (a.ram) writeFileSync(a.ram, m.mem.ram);
if (a.wram) writeFileSync(a.wram, m.mem.wram);
console.error(`frames=${a.frames} (last frame_no=${a.frame0 + m.frame - 1}) main_state=${m.rd(0x40)} sub_state=${m.rd(0x41)} ` +
  `unported=${m.unported.size} stubs=${JSON.stringify(m.stubHits)} ${dt ? (a.frames / dt).toFixed(0) : '-'} fps`);
