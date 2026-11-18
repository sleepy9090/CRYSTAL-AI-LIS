// node web/test/apu_check.mjs [--wav ID OUT.wav [SECONDS]] — Ph7 web 7/8 (s57): web/src/nes/apu.js.
// tools.emu has no APU (tools/emu/apu.py is a register log), so this checks the synth against the 2A03 formulas:
//   1. pitch: pulse 1/2 (CPU/(16(P+1))), triangle (CPU/(32(P+1))) measured by zero crossings, within 1 %
//   2. $4015 status: length counter (index 1 = 254 half frames), DMC bytes-remaining bit (headless and sampled
//      paths must agree), frame IRQ flag in 4-step mode without inhibit, cleared by the read
//   3. sweep mute (period < 8), pulse 1 one's-complement negate vs pulse 2 two's-complement
//   4. driver -> APU: every music id $00-$1F through Machine (SndInit, PlaySound queue, SoundUpdate per frame,
//      apu.run per frame) for 8 s: no NaN, |x| < 1, and non-silent for every id that has a track in sound.json
// --wav ID OUT.wav writes music ID (hex or dec) as 16-bit mono 44.1 kHz. Exit 1 on any failure.
import { readFileSync, writeFileSync } from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { Apu, CPU_HZ } from '../src/nes/apu.js';
import { loadData } from '../src/data.js';
import { Machine } from '../src/game/machine.js';
import { playSound } from '../src/game/playsound.js';

const WEB = path.dirname(path.dirname(fileURLToPath(import.meta.url)));
const SR = 44100;
let fail = 0;
const check = (ok, msg) => { console.log(`${ok ? 'ok  ' : 'FAIL'} ${msg}`); if (!ok) fail++; };

function render(apu, seconds) {
  const out = [];
  apu.setOutput(SR, out);
  apu.run(Math.round(CPU_HZ * seconds));
  return out;
}
function freqOf(pcm) {                    // rising zero crossings of the (high-passed) signal, skip 0.1 s settle
  const s = pcm.slice(SR / 10);
  let n = 0;
  for (let i = 1; i < s.length; i++) if (s[i - 1] < 0 && s[i] >= 0) n++;
  return n / (s.length / SR);
}

// 1. pitch
for (const [name, base, period, div] of [['pulse 1', 0x4000, 0x0FD, 16], ['pulse 2', 0x4004, 0x1AB, 16],
  ['triangle', 0x4008, 0x17C, 32]]) {
  const apu = new Apu();
  apu.write(0x4017, 0x40);
  apu.write(0x4015, 0x0F);
  apu.write(base, base === 0x4008 ? 0xFF : 0xBF);   // duty 2 const vol 15 / tri control + linear $7F
  if (base !== 0x4008) apu.write(base + 1, 0x08);   // sweep off, negate set (no effect with shift 0)
  apu.write(base + 2, period & 0xFF);
  apu.write(base + 3, (period >> 8) | 0xF8);
  const want = CPU_HZ / (div * (period + 1)), got = freqOf(render(apu, 1));
  check(Math.abs(got - want) / want < 0.01, `${name} P=$${period.toString(16)}: ${got.toFixed(1)} Hz, formula ${want.toFixed(1)} Hz`);
}

// 2. $4015 status
{
  const a = new Apu();
  a.write(0x4017, 0x40); a.write(0x4015, 0x01); a.write(0x4000, 0x10); a.write(0x4003, 0x08);   // length idx 1 = 254
  a.run(29830 * 126); const on = a.read(0x4015) & 1;
  a.run(29830 * 2); const off = a.read(0x4015) & 1;
  check(on === 1 && off === 0, `length counter 254 half frames: bit0 ${on} after 252, ${off} after 256`);
  const b = new Apu(); b.write(0x4017, 0x00); b.run(29830 + 10);
  const f1 = b.read(0x4015) & 0x40, f2 = b.read(0x4015) & 0x40;
  check(f1 && !f2, 'frame IRQ flag set in 4-step mode, cleared by the $4015 read');
  for (const sampled of [false, true]) {
    const d = new Apu(() => 0x55);
    d.write(0x4010, 0x0F); d.write(0x4012, 0x00); d.write(0x4013, 0x01);   // rate 54, 17 bytes
    d.write(0x4015, 0x10);
    const need = 17 * 8 * 54;                  // last byte fetched one byte early (buffered), done by `need`
    if (sampled) d.setOutput(SR, []);
    d.run(need - 8 * 54 - 600); const busy = d.read(0x4015) & 0x10;
    d.run(1200); const done = d.read(0x4015) & 0x10;
    check(busy && !done, `DMC 17-byte sample at rate 54 (${sampled ? 'sampled' : 'headless'}): bit4 busy then clear`);
  }
}

// 3. sweep
{
  const p1 = new Apu(), p2 = new Apu();
  for (const [a, base] of [[p1, 0x4000], [p2, 0x4004]]) {
    a.write(0x4015, 3); a.write(base + 1, 0x89); a.write(base + 2, 0x00); a.write(base + 3, 0x01);  // P=$100, shift 1
  }
  check(p1.p1.target() === 0x7F && p2.p2.target() === 0x80, `sweep negate: pulse 1 target $${p1.p1.target().toString(16)} (one's), pulse 2 $${p2.p2.target().toString(16)} (two's)`);
  const q = new Apu(); q.write(0x4015, 1); q.write(0x4000, 0xBF); q.write(0x4002, 0x07); q.write(0x4003, 0x08);
  const pcm = render(q, 0.2);
  check(pcm.slice(SR / 20).every((v) => Math.abs(v) < 1e-3), 'pulse with period 7 is muted');
}

// 4. driver -> APU
const data = await loadData((n) => readFileSync(path.join(WEB, 'data', n)));
const snd = JSON.parse(readFileSync(path.join(WEB, 'data/sound.json'), 'utf8'));
const tracks = new Set(snd.music.map((t) => t.id));
function playMusic(id, seconds) {
  const m = new Machine(data);
  const pcm = [];
  m.apu.setOutput(SR, pcm);
  m.reset();
  playSound(m, id);
  const frames = Math.round(seconds * 60.0988);
  for (let f = 0; f < frames; f++) { m.soundUpdate(m); m.apu.run(29781); }
  return pcm;
}
const args = process.argv.slice(2);
if (args[0] === '--wav') {
  const pcm = playMusic(Number(args[1]), Number(args[3] || 30));
  const b = Buffer.alloc(44 + pcm.length * 2);
  b.write('RIFF', 0); b.writeUInt32LE(36 + pcm.length * 2, 4); b.write('WAVEfmt ', 8);
  b.writeUInt32LE(16, 16); b.writeUInt16LE(1, 20); b.writeUInt16LE(1, 22); b.writeUInt32LE(SR, 24);
  b.writeUInt32LE(SR * 2, 28); b.writeUInt16LE(2, 32); b.writeUInt16LE(16, 34); b.write('data', 36);
  b.writeUInt32LE(pcm.length * 2, 40);
  pcm.forEach((v, i) => b.writeInt16LE(Math.max(-32768, Math.min(32767, Math.round(v * 65536))), 44 + i * 2));
  writeFileSync(args[2], b);
  console.log(`wrote ${args[2]}: ${(pcm.length / SR).toFixed(1)} s`);
  process.exit(0);
}
let silent = [], bad = [];
const t0 = Date.now();
for (let id = 0; id < 0x20; id++) {
  const pcm = playMusic(id, 8);
  let peak = 0, sum = 0, nan = 0;
  for (const v of pcm) { if (!Number.isFinite(v)) nan++; else { peak = Math.max(peak, Math.abs(v)); sum += v * v; } }
  const rms = Math.sqrt(sum / pcm.length);
  if (nan || peak >= 1) bad.push(`$${id.toString(16)} nan ${nan} peak ${peak.toFixed(3)}`);
  // $00 = silence (REVERSE.md "Sound data format": each stream 53 40 00), so it must stay quiet
  if (id === 0 ? rms >= 0.005 : tracks.has(id) && rms < 0.005) silent.push(`$${id.toString(16)} rms ${rms.toFixed(4)}`);
}
check(!bad.length, `music $00-$1F through the driver: finite, |x| < 1 ${bad.join(', ')}`);
check(!silent.length, `every music id with a track is audible (rms >= 0.005), $00 silent ${silent.join(', ')}`);
console.log(`  (32 x 8 s rendered in ${((Date.now() - t0) / 1000).toFixed(1)} s)`);
console.log(fail ? `${fail} check(s) failed` : 'all APU checks passed');
process.exit(fail ? 1 : 0);
