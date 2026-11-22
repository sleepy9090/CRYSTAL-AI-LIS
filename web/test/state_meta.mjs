// node web/test/state_meta.mjs STATE.json ... — print a state JSON's short fields (frame, frame0, line, until, ...)
// and its sound-driver RAM $0100-$015F / $F0-$FF (s176: the 1-frame snd*_period offset after loads).
import { readFileSync } from 'node:fs';

for (const p of process.argv.slice(2)) {
  const s = JSON.parse(readFileSync(p, 'utf8'));
  const o = {};
  for (const [k, v] of Object.entries(s)) if (typeof v !== 'string' || v.length < 80) o[k] = v;
  delete o.ppu; delete o.mapper;
  console.log(p.split('/').pop(), JSON.stringify(o));
  if (s.ram) {
    console.log('  0100:', s.ram.slice(0x200, 0x2C0).match(/.{32}/g).join(' '));
    console.log('  00F0:', s.ram.slice(0x1E0, 0x200));
  }
}
