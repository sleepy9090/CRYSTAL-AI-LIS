// re-c web port — WebAudio output for nes/apu.js (Ph7 web 7/8, s57).
//
//   const out = new AudioOut(machine);  out.resume()  (call from a user gesture: browsers start contexts suspended)
//   out.flush()                          after each batch of frames: schedules the samples the APU produced
//   out.setMuted(bool)
//
// The APU pushes one Float32 sample per call to out.push(v) at the context's sample rate. Samples are sent in
// chunks of >= CHUNK as AudioBufferSourceNodes queued back to back on the context clock. If the queue runs dry
// (tab stalled, pause) the next chunk starts LEAD seconds ahead of currentTime. Until resume() succeeds the APU
// stays in its headless mode (no synthesis).
const CHUNK = 1024;
const LEAD = 0.05;
const GAIN = 1.6;          // APU mixer peaks near 0.4 after the output filters (apu_check), so lift it a little

export class AudioOut {
  constructor(machine) {
    this.m = machine;
    this.ctx = null;
    this.buf = new Float32Array(CHUNK * 8);
    this.n = 0;
    this.next = 0;
    this.muted = false;
  }

  resume() {
    const AC = globalThis.AudioContext || globalThis.webkitAudioContext;
    if (!AC) return;
    if (!this.ctx) {
      this.ctx = new AC();
      this.gain = this.ctx.createGain();
      this.gain.gain.value = this.muted ? 0 : GAIN;
      this.gain.connect(this.ctx.destination);
      this.m.apu.setOutput(this.ctx.sampleRate, this);   // Machine.reset carries the sink over to its new Apu
    }
    if (this.ctx.state === 'suspended') this.ctx.resume();
  }

  setMuted(on) {
    this.muted = on;
    if (this.gain) this.gain.gain.value = on ? 0 : GAIN;
  }

  push(v) {
    if (this.n === this.buf.length) {      // a long catch-up burst: grow rather than drop
      const b = new Float32Array(this.buf.length * 2); b.set(this.buf); this.buf = b;
    }
    this.buf[this.n++] = v;
  }

  flush() {
    if (!this.ctx || this.n < CHUNK) return;
    const ctx = this.ctx;
    const ab = ctx.createBuffer(1, this.n, ctx.sampleRate);
    ab.copyToChannel(this.buf.subarray(0, this.n), 0);
    this.n = 0;
    const src = ctx.createBufferSource();
    src.buffer = ab;
    src.connect(this.gain);
    if (this.next < ctx.currentTime) this.next = ctx.currentTime + LEAD;
    src.start(this.next);
    this.next += ab.duration;
  }
}
