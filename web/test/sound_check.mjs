// node web/test/sound_check.mjs [--verbose] [--only NAME] — Ph7 web 7/8 (s56): the sound driver port vs tools.emu.
// web/test/sound_frames/*.json (tools/emu/sound_capture.py, README there) holds SoundUpdate call sequences:
// RAM after SndInit, then per call the queue pokes, the APU register writes in order and the RAM bytes it changed.
// The port runs soundUpdate() on the same RAM; per call the APU write list must be identical and so must all of
// RAM except the stack page above the driver's variables ($0185-$01FF). $4015 reads return 0 like tools.emu's
// APU stub. Exit 1 on any difference.
import { readFileSync, readdirSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { soundUpdate } from '../src/game/sound/soundupdate.js';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const args = process.argv.slice(2);
const verbose = args.includes('--verbose');
const only = args.includes('--only') ? args[args.indexOf('--only') + 1] : null;
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const m = new Machine(data);
const hx = (v, n = 2) => v.toString(16).toUpperCase().padStart(n, '0');
let writes = [];
m.apuWrite = (a, v) => { writes.push(hx(a - 0x4000) + hx(v & 0xFF)); };
m.apuRead = () => 0;
const skip = (a) => a >= 0x185 && a < 0x200;

const dir = path.join(WEB, 'test/sound_frames');
let fail = 0, nsc = 0, ncalls = 0, nw = 0;
for (const file of readdirSync(dir).filter((f) => f.endsWith('.json')).sort()) {
  const j = JSON.parse(readFileSync(path.join(dir, file), 'utf8'));
  for (const sc of j.scenarios) {
    if (only && sc.name !== only) continue;
    nsc++;
    m.reset();
    const want = Uint8Array.from(Buffer.from(sc.ram, 'hex'));
    m.mem.ram.set(want);
    let bad = '';
    for (let i = 0; i < sc.steps.length && !bad; i++) {
      const st = sc.steps[i];
      for (const [a, v] of st.p || []) { m.wr(a, v); want[a] = v; }
      for (const [a, v] of st.d) want[a] = v;
      writes = [];
      try { soundUpdate(m); } catch (e) { bad = `call ${i}: ${e.message}`; break; }
      ncalls++; nw += st.w.length / 4;
      const got = writes.join('');
      if (got !== st.w) {
        let k = 0;
        while (k < got.length && got.slice(k, k + 4) === st.w.slice(k, k + 4)) k += 4;
        bad = `call ${i}: APU writes differ at #${k / 4}: port ${got.slice(k, k + 24) || '-'} emu ${st.w.slice(k, k + 24) || '-'}`;
      }
      const diffs = [];
      for (let a = 0; a < 0x800; a++) {
        if (!skip(a) && m.mem.ram[a] !== want[a]) diffs.push(`$${hx(a, 4)} port ${hx(m.mem.ram[a])} emu ${hx(want[a])}`);
      }
      if (diffs.length) bad += `${bad ? '; ' : ''}call ${i}: ${diffs.length} RAM byte(s): ${diffs.slice(0, 8).join(', ')}`;
    }
    if (bad) {
      fail++;
      if (fail <= 6 || verbose) console.log(`FAIL ${sc.name}: ${bad}`);
    } else if (verbose) console.log(`ok   ${sc.name} (${sc.steps.length} calls)`);
  }
}
console.log(`${nsc - fail}/${nsc} scenario(s) identical, ${ncalls} SoundUpdate calls, ${nw} APU writes compared`);
process.exit(fail || !nsc ? 1 : 0);
