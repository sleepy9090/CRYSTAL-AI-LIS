// apu_preview.js — offline renderer for web/data/sound.json tracks (catalog preview).
// Steps each channel's event list frame by frame with the driver rules documented in
// REVERSE.md (sound driver, "Tick timing + flow exact"): envelope tick from
// envTonal/envNoise + SndVolumeTable, vibrato with the 24:8724 off-by-one quirk (via the raw
// bytes from 24:8714), rest/hold, DMC drum triggers. Then synthesises 2A03 channels at the
// output sample rate and mixes with the NESdev non-linear mixer formula.
// Approximations (the exact port is Ph7 web 7/8): hardware sweep is ignored, a channel with no
// software envelope plays at its base volume, fades are not modelled, SFX play alone.

export const CPU_HZ = 1789773;
export const FRAME_HZ = 60.0988;
const DMC_RATES = [428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54];
const DUTY = [
  [0, 1, 0, 0, 0, 0, 0, 0], [0, 1, 1, 0, 0, 0, 0, 0],
  [0, 1, 1, 1, 1, 0, 0, 0], [1, 0, 0, 1, 1, 1, 1, 1],
];
const TRI_SEQ = Array.from({ length: 32 }, (_, i) => (i < 16 ? 15 - i : i - 16));
const TYPE = { SQ1: 0, SQ2: 1, TRI: 2, NOISE: 3, 'SFX-SQ2': 1, 'SFX-NOISE': 3 };

// Per-frame register state for one channel: [{period, vol, duty, on, dmc}] (dmc = record or 0)
export function channelFrames(ch, tables, nFrames) {
  const type = TYPE[ch.chn];
  const evs = ch.events;
  const raw = tables.vibRawFrom8714; // raw bytes from 24:8714 (step table @+0, offsets @+$10)
  const envTab = type === 3 ? tables.envNoise : tables.envTonal;
  const out = [];
  let i = 0, left = 0, ev = null, done = false;
  let envStep = 0, restFlag = false, outVol = 0, envDuty = 0, vibPhase = 0, silenced = true;
  let triStart = 0;
  for (let f = 0; f < nFrames; f++) {
    let dmc = 0;
    if (!done && left === 0) {
      if (i >= evs.length || evs[i].k === 'truncated') {
        if (ch.loop !== null && ch.loop !== undefined) i = ch.loop; else done = true;
      }
      if (!done) {
        ev = evs[i++];
        if (ev.k === 'end') { done = true; silenced = true; }
        else {
          left = ev.len;
          if (ev.k === 'note' || ev.k === 'legato') {
            if (ev.retrig) { envStep = 0; vibPhase = 0; restFlag = false; triStart = f; }
            silenced = false;
            if (ev.dmc) dmc = ev.dmc;
            if (ev.env === null || ev.env === undefined) outVol = ev.vol;
          } else if (ev.k === 'rest') { restFlag = true; silenced = true; }
          else if (ev.k === 'hold') { restFlag = true; silenced = false; outVol = ev.holdVol; }
        }
      }
    }
    if (done || !ev) { out.push({ period: 0, vol: 0, duty: 0, on: false, dmc }); continue; }
    let period = ev.period;
    if (!restFlag) {
      // envelope tick (24:84AF)
      if (ev.env !== null && ev.env !== undefined) {
        const e = envTab[((ev.env & 15) << 4) | envStep];
        if (e & 0x10) envStep = 15; // end marker: freeze
        else {
          outVol = tables.volume[((ev.vol & 15) << 4) | (e & 15)];
          envDuty = e >> 6;
          if (envStep !== 15) envStep++;
        }
      } else { outVol = ev.vol; envDuty = 0; }
      // vibrato tick (24:86AA), waits for the envelope to finish
      if (ev.vib !== null && ev.vib !== undefined && type !== 3 &&
          (ev.env === null || ev.env === undefined || envStep === 15)) {
        const x = ev.vib;
        vibPhase = (vibPhase + 1) & 7;
        const p = ((x & 1) ? vibPhase >> 1 : vibPhase) & 3;
        const e = raw[(x >> 1) * 4 + p] ?? 0;
        if (e) {
          const y = ev.oct * 16 + e + 2;
          let off = (raw[0x10 + y] ?? 0) | ((raw[0x11 + y] ?? 0) << 8);
          if (off & 0x8000) off -= 0x10000;
          const lo = (period & 0xFF) + off;
          if (lo >= 0 && lo <= 0xFF) period = (period & 0x700) | lo; // only if hi unchanged
        }
      }
    }
    let duty = (ev.ctl >> 2) & 3;
    if (envDuty) duty = envDuty;
    let on = !silenced && outVol > 0;
    let vol = outVol;
    if (type === 2) {
      const reg = ((ev.ctl << 4) | (ev.k === 'hold' ? ev.holdVol : ev.vol)) & 0xFF;
      on = !silenced && (reg & 0x7F) > 0;
      if (!(reg & 0x80) && (f - triStart) * 4 >= (reg & 0x7F)) on = false; // linear counter
      vol = 15;
    }
    if (type === 3) period = tables.noisePeriods[ev.period & 15];
    out.push({ period, vol, duty, on, dmc });
    left--;
  }
  return out;
}

export function trackFrames(track, maxFrames) {
  let n = 0;
  for (const ch of track.channels) {
    const loops = ch.loop !== null && ch.loop !== undefined;
    n = Math.max(n, loops ? ch.frames * 2 : ch.frames + 30);
  }
  return Math.min(n, maxFrames);
}

// Render a track to mono float samples. mute = Set of channel names to skip.
export function renderTrack(track, sound, sampleRate, { maxSeconds = 90, mute = new Set() } = {}) {
  const tables = sound.tables;
  const nFrames = trackFrames(track, Math.ceil(maxSeconds * FRAME_HZ));
  const spf = sampleRate / FRAME_HZ;
  const nSamples = Math.ceil(nFrames * spf);
  const pulse = [new Float32Array(nSamples), new Float32Array(nSamples)];
  const tri = new Float32Array(nSamples), noise = new Float32Array(nSamples), dmcOut = new Float32Array(nSamples);
  const dmcBytes = hexBytes(sound.dmc.bytes);
  const cyc = CPU_HZ / sampleRate;
  let dmcState = null;
  for (const ch of track.channels) {
    if (mute.has(ch.chn)) continue;
    const type = TYPE[ch.chn];
    const frames = channelFrames(ch, tables, nFrames);
    let phase = 0, lfsr = 1, nacc = 0;
    for (let f = 0; f < nFrames; f++) {
      const st = frames[f];
      const s0 = Math.floor(f * spf), s1 = Math.min(nSamples, Math.floor((f + 1) * spf));
      if (st.dmc && type === 3 && !mute.has('DMC')) {
        const rec = tables.dmc[st.dmc] || [15, 0, 0xE8, 0x3E];
        dmcState = { start: s0, rate: DMC_RATES[rec[0] & 15], level: rec[1] & 0x7F };
        renderDmc(dmcOut, dmcBytes, dmcState, cyc);
      }
      if (!st.on) continue;
      if (type <= 1) {
        if (st.period < 8 || st.period > 0x7FF) continue;
        const inc = cyc / (16 * (st.period + 1)), seq = DUTY[st.duty], buf = pulse[type];
        for (let s = s0; s < s1; s++) { phase = (phase + inc) % 1; buf[s] = seq[(phase * 8) | 0] * st.vol; }
      } else if (type === 2) {
        if (st.period < 2) continue;
        const inc = cyc / (32 * (st.period + 1));
        for (let s = s0; s < s1; s++) { phase = (phase + inc) % 1; tri[s] = TRI_SEQ[(phase * 32) | 0]; }
      } else {
        for (let s = s0; s < s1; s++) {
          nacc += cyc;
          while (nacc >= st.period) {
            nacc -= st.period;
            const fb = (lfsr & 1) ^ ((lfsr >> 1) & 1);
            lfsr = (lfsr >> 1) | (fb << 14);
          }
          noise[s] = (lfsr & 1) ? 0 : st.vol;
        }
      }
    }
  }
  // mixer (NESdev "APU Mixer" non-linear approximation) + DC-blocking high-pass
  const out = new Float32Array(nSamples);
  let prevIn = 0, prevOut = 0;
  for (let s = 0; s < nSamples; s++) {
    const p = pulse[0][s] + pulse[1][s];
    const po = p ? 95.88 / (8128 / p + 100) : 0;
    const t = tri[s] / 8227 + noise[s] / 12241 + dmcOut[s] / 22638;
    const to = t ? 159.79 / (1 / t + 100) : 0;
    const x = po + to;
    prevOut = 0.996 * (prevOut + x - prevIn); prevIn = x;
    out[s] = prevOut * 1.6;
  }
  return out;
}

function renderDmc(buf, bytes, st, cyc) {
  // 1-bit delta stream, LSB first; a new trigger overwrites the rest of the old one
  let level = st.level, acc = 0, bit = 0;
  const total = bytes.length * 8;
  for (let s = st.start; s < buf.length; s++) {
    acc += cyc;
    while (acc >= st.rate && bit < total) {
      acc -= st.rate;
      const b = (bytes[bit >> 3] >> (bit & 7)) & 1;
      if (b) { if (level <= 125) level += 2; } else if (level >= 2) level -= 2;
      bit++;
    }
    buf[s] = level;
    if (bit >= total) { buf.fill(level, s); break; }
  }
}

function hexBytes(h) {
  const a = new Uint8Array(h.length >> 1);
  for (let i = 0; i < a.length; i++) a[i] = parseInt(h.substr(i * 2, 2), 16);
  return a;
}
