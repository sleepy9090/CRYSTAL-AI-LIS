// node web/test/apu_preview_check.mjs — smoke test for web/apu_preview.js:
// renders every music track (10 s cap) and SFX, reports length, peak, RMS, NaN count,
// and optionally writes one track as a 16-bit WAV: --wav music 0x13 out.wav
import { readFileSync, writeFileSync } from 'node:fs';
import { renderTrack } from '../apu_preview.js';

const snd = JSON.parse(readFileSync(new URL('../data/sound.json', import.meta.url)));
const SR = 44100;
const args = process.argv.slice(2);

function stats(pcm) {
  let peak = 0, sum = 0, nan = 0;
  for (const v of pcm) { if (Number.isNaN(v)) { nan++; continue; } peak = Math.max(peak, Math.abs(v)); sum += v * v; }
  return { peak, rms: Math.sqrt(sum / Math.max(1, pcm.length)), nan };
}

if (args[0] === '--wav') {
  const t = snd[args[1]].find((x) => x.id === Number(args[2]));
  const pcm = renderTrack(t, snd, SR, { maxSeconds: 30 });
  const b = Buffer.alloc(44 + pcm.length * 2);
  b.write('RIFF', 0); b.writeUInt32LE(36 + pcm.length * 2, 4); b.write('WAVEfmt ', 8);
  b.writeUInt32LE(16, 16); b.writeUInt16LE(1, 20); b.writeUInt16LE(1, 22); b.writeUInt32LE(SR, 24);
  b.writeUInt32LE(SR * 2, 28); b.writeUInt16LE(2, 32); b.writeUInt16LE(16, 34); b.write('data', 36);
  b.writeUInt32LE(pcm.length * 2, 40);
  pcm.forEach((v, i) => b.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(v * 32767))), 44 + i * 2));
  writeFileSync(args[3], b);
  console.log('wrote', args[3], (pcm.length / SR).toFixed(1), 's', stats(pcm));
} else {
  let bad = 0, silent = 0;
  for (const kind of ['music', 'sfx']) {
    for (const t of snd[kind]) {
      const pcm = renderTrack(t, snd, SR, { maxSeconds: 10 });
      const s = stats(pcm);
      if (s.nan || s.peak > 1.5) bad++;
      if (s.rms < 1e-4) silent++;
      console.log(`${kind} $${t.id.toString(16).padStart(2, '0')} ${(pcm.length / SR).toFixed(2)}s peak ${s.peak.toFixed(3)} rms ${s.rms.toFixed(4)}${s.nan ? ' NaN ' + s.nan : ''}${s.rms < 1e-4 ? ' SILENT' : ''}`);
    }
  }
  console.log(`bad ${bad}, silent ${silent}`);
}
