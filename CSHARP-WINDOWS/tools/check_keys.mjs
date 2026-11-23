// node tools/check_keys.mjs ../web/test/NAME_check.mjs [..] — the scenario runs of web/test *_check.mjs scripts
// (state, last frame, keys) as JSON lines, for tools/core_vs_web.py. Read-only on ../web: the check's top-level
// prelude (everything before its first state load / run loop) is copied into out/check_keys/ with absolute imports
// and evaluated; then LAST + KEYS, or its CASES / RUNS lists (kill_check / group5c_check style), are printed.
// Keys come out as run.mjs specs ("F:A+B:N", run-length encoded from parseKeys' frame -> mask map).
import { readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath, pathToFileURL } from 'node:url';

const ROOT = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const OUT = path.join(ROOT, 'out', 'check_keys');
mkdirSync(OUT, { recursive: true });

const CUT = /^(const state = JSON\.parse|for \(const |const m = new Machine|const out = \[\];|const rows = )/m;

async function prelude(file) {
  const dir = path.dirname(path.resolve(file));
  let src = readFileSync(file, 'utf8');
  const cut = src.search(CUT);
  if (cut < 0) throw new Error(`${file}: no state load / run loop found`);
  src = src.slice(0, cut);
  src = src.replace(/from '(\.\.?\/[^']+)'/g, (_, p) => `from '${pathToFileURL(path.join(dir, p)).href}'`);
  src = src.replace(/fileURLToPath\(import\.meta\.url\)/g, JSON.stringify(path.resolve(file)));
  src += `\nexport const __r = {
  LAST: typeof LAST === 'undefined' ? null : LAST,
  KEYS: typeof KEYS === 'undefined' ? null : [...KEYS],
  CASES: typeof CASES === 'undefined' ? null : CASES,
  RUNS: typeof RUNS === 'undefined' ? null : RUNS,
};\n`;
  const tmp = path.join(OUT, path.basename(file));
  writeFileSync(tmp, src);
  return (await import(pathToFileURL(tmp).href + '?t=' + Date.now())).__r;
}

const { parseKeys } = await import(pathToFileURL(path.join(ROOT, '..', 'web', 'test', 'harness.mjs')).href);
const { BTN } = await import(pathToFileURL(path.join(ROOT, '..', 'web', 'src', 'input.js')).href);
const NAMES = Object.entries(BTN).sort((a, b) => b[1] - a[1]);

function specs(entries) {
  const map = new Map(entries), out = [];
  const frames = [...map.keys()].sort((a, b) => a - b);
  for (let i = 0; i < frames.length;) {
    const f = frames[i], v = map.get(f);
    let n = 1;
    while (i + n < frames.length && frames[i + n] === f + n && map.get(f + n) === v) n++;
    if (v) out.push(`${f}:${NAMES.filter(([, b]) => v & b).map(([k]) => k).join('+')}:${n}`);
    i += n;
  }
  return out.join(' ');
}

for (const file of process.argv.slice(2)) {
  const stem = path.basename(file).replace(/_check\.mjs$/, '');
  const r = await prelude(file);
  if (r.CASES) {
    for (const c of r.CASES)
      console.log(JSON.stringify({ check: stem, state: c.name, last: c.last ?? r.LAST, keys: specs([...parseKeys(c.keys)]) }));
  } else if (r.RUNS) {
    for (const c of r.RUNS) {
      const keys = typeof c.keys === 'string' ? c.keys.split(' ') : c.keys;
      console.log(JSON.stringify({ check: stem, state: stem.replace(/5c$/, '') + c.name, last: c.last, keys: specs([...parseKeys(keys)]) }));
    }
  } else {
    console.log(JSON.stringify({ check: stem, state: stem, last: r.LAST, keys: r.KEYS ? specs(r.KEYS) : '' }));
  }
}
