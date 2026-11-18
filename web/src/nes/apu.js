// re-c web port — NES 2A03 APU (Ph7 web 7/8, s57). Register-level model of the five channels, driven by the
// writes of the ported sound driver (game/sound/driver.js via Machine.apuWrite). DOM-free; main.js feeds the
// samples to WebAudio.
//
//   const apu = new Apu(dmcRead);         dmcRead(addr) = CPU read for DMC fetches ($C000-$FFFF, wraps to $8000)
//   apu.write(addr, v) / apu.read(0x4015)
//   apu.run(cpuCycles)                    advance; when apu.sampleRate > 0 the samples go to apu.out (Float32 push)
//
// NTSC tables: length counters, duty sequences, triangle 32-step, noise periods (CPU cycles), DMC rates (CPU
// cycles). Frame sequencer: 4-step mode quarter frames at 7457/14913/22371/29829 (half at 2 and 4, IRQ at 4 unless
// inhibited), 5-step at 7457/14913/22371/37281 (half at 2 and 5); a $4017 write with b7 clocks both at once.
// Sweep: pulse 1 negates with one's complement (-c-1), pulse 2 with two's; mutes when period < 8 or target > $7FF.
// Triangle holds its output when the timer period is < 2 (the ultrasonic pop is avoided, as most emulators do).
// Mixer: the non-linear 2A03 formulas (pulse 95.88 / (8128/(p1+p2) + 100), tnd 159.79 / (1/(t/8227+n/12241+d/22638)
// + 100)), then the console's output filters: high-pass 90 Hz, high-pass 440 Hz, low-pass 14 kHz.
//
// Without a sample rate (headless runs) only the parts that $4015 can see are advanced: the frame sequencer
// (length counters) and the DMC byte fetches, event to event.

export const CPU_HZ = 1789773;
const LENGTH = [10, 254, 20, 2, 40, 4, 80, 6, 160, 8, 60, 10, 14, 12, 26, 14,
  12, 16, 24, 18, 48, 20, 96, 22, 192, 24, 72, 26, 16, 28, 32, 30];
const DUTY = [[0, 1, 0, 0, 0, 0, 0, 0], [0, 1, 1, 0, 0, 0, 0, 0], [0, 1, 1, 1, 1, 0, 0, 0], [1, 0, 0, 1, 1, 1, 1, 1]];
const TRI = [15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15];
const NOISE_PERIOD = [4, 8, 16, 32, 64, 96, 128, 160, 202, 254, 380, 508, 762, 1016, 2034, 4068];
const DMC_RATE = [428, 380, 340, 320, 286, 254, 226, 214, 190, 160, 142, 128, 106, 84, 72, 54];
const SEQ4 = [7457, 14913, 22371, 29829], SEQ5 = [7457, 14913, 22371, 37281];

const PULSE_MIX = new Float32Array(31).map((_, i) => (i ? 95.88 / (8128 / i + 100) : 0));
const TND_MIX = new Float32Array(203).map((_, i) => (i ? 163.67 / (24329 / i + 100) : 0));   // standard lookup

class Envelope {
  constructor() { this.start = false; this.div = 0; this.decay = 0; this.loop = false; this.constant = false; this.vol = 0; }
  clock() {
    if (this.start) { this.start = false; this.decay = 15; this.div = this.vol; return; }
    if (this.div > 0) { this.div--; return; }
    this.div = this.vol;
    if (this.decay > 0) this.decay--; else if (this.loop) this.decay = 15;
  }
  get out() { return this.constant ? this.vol : this.decay; }
}

class Pulse {
  constructor(ones) {
    this.ones = ones; this.env = new Envelope(); this.enabled = false; this.len = 0; this.duty = 0; this.step = 0;
    this.period = 0; this.timer = 0; this.swEn = false; this.swPer = 0; this.swNeg = false; this.swShift = 0;
    this.swDiv = 0; this.swReload = false;
  }
  write(r, v) {
    switch (r) {
      case 0: this.duty = v >> 6; this.env.loop = !!(v & 0x20); this.env.constant = !!(v & 0x10); this.env.vol = v & 15; break;
      case 1: this.swEn = !!(v & 0x80); this.swPer = (v >> 4) & 7; this.swNeg = !!(v & 8); this.swShift = v & 7; this.swReload = true; break;
      case 2: this.period = (this.period & 0x700) | v; break;
      case 3:
        this.period = (this.period & 0xFF) | ((v & 7) << 8);
        if (this.enabled) this.len = LENGTH[v >> 3];
        this.step = 0; this.env.start = true; break;
    }
  }
  target() {
    const c = this.period >> this.swShift;
    return this.swNeg ? this.period - c - (this.ones ? 1 : 0) : this.period + c;
  }
  muted() { return this.period < 8 || this.target() > 0x7FF; }
  clockSweep() {
    if (this.swDiv === 0 && this.swEn && this.swShift && !this.muted()) this.period = Math.max(0, this.target());
    if (this.swDiv === 0 || this.swReload) { this.swDiv = this.swPer; this.swReload = false; } else this.swDiv--;
  }
  clockLen() { if (this.len && !this.env.loop) this.len--; }
  tick() {                                   // one APU cycle (2 CPU cycles)
    if (this.timer === 0) { this.timer = this.period; this.step = (this.step + 1) & 7; } else this.timer--;
  }
  get out() { return (this.len && !this.muted() && DUTY[this.duty][this.step]) ? this.env.out : 0; }
}

class Triangle {
  constructor() { this.enabled = false; this.len = 0; this.ctrl = false; this.linLoad = 0; this.lin = 0; this.linReload = false; this.period = 0; this.timer = 0; this.step = 0; }
  write(r, v) {
    switch (r) {
      case 0: this.ctrl = !!(v & 0x80); this.linLoad = v & 0x7F; break;
      case 2: this.period = (this.period & 0x700) | v; break;
      case 3:
        this.period = (this.period & 0xFF) | ((v & 7) << 8);
        if (this.enabled) this.len = LENGTH[v >> 3];
        this.linReload = true; break;
    }
  }
  clockLinear() {
    if (this.linReload) this.lin = this.linLoad; else if (this.lin) this.lin--;
    if (!this.ctrl) this.linReload = false;
  }
  clockLen() { if (this.len && !this.ctrl) this.len--; }
  tick() {
    if (this.timer === 0) {
      this.timer = this.period;
      if (this.len && this.lin && this.period >= 2) this.step = (this.step + 1) & 31;
    } else this.timer--;
  }
  get out() { return TRI[this.step]; }
}

class Noise {
  constructor() { this.env = new Envelope(); this.enabled = false; this.len = 0; this.mode = false; this.period = 4; this.timer = 0; this.sr = 1; }
  write(r, v) {
    switch (r) {
      case 0: this.env.loop = !!(v & 0x20); this.env.constant = !!(v & 0x10); this.env.vol = v & 15; break;
      case 2: this.mode = !!(v & 0x80); this.period = NOISE_PERIOD[v & 15]; break;
      case 3: if (this.enabled) this.len = LENGTH[v >> 3]; this.env.start = true; break;
    }
  }
  clockLen() { if (this.len && !this.env.loop) this.len--; }
  tick() {                                   // CPU cycle (the periods are in CPU cycles)
    if (this.timer === 0) {
      this.timer = this.period - 1;
      const fb = (this.sr ^ (this.sr >> (this.mode ? 6 : 1))) & 1;
      this.sr = (this.sr >> 1) | (fb << 14);
    } else this.timer--;
  }
  get out() { return (this.len && !(this.sr & 1)) ? this.env.out : 0; }
}

class Dmc {
  constructor(read) {
    this.read = read; this.irqEn = false; this.loop = false; this.rate = DMC_RATE[0]; this.timer = DMC_RATE[0] - 1;
    this.level = 0; this.addrLoad = 0xC000; this.lenLoad = 1; this.addr = 0xC000; this.remain = 0; this.buf = -1;
    this.shift = 0; this.bits = 8; this.silent = true; this.irq = false;
  }
  write(r, v) {
    switch (r) {
      case 0: this.irqEn = !!(v & 0x80); this.loop = !!(v & 0x40); this.rate = DMC_RATE[v & 15]; if (!this.irqEn) this.irq = false; break;
      case 1: this.level = v & 0x7F; break;
      case 2: this.addrLoad = 0xC000 | (v << 6); break;
      case 3: this.lenLoad = (v << 4) | 1; break;
    }
  }
  restart() { this.addr = this.addrLoad; this.remain = this.lenLoad; }
  fetch() {
    if (this.buf >= 0 || this.remain === 0) return;
    this.buf = this.read(this.addr);
    this.addr = this.addr === 0xFFFF ? 0x8000 : this.addr + 1;
    if (--this.remain === 0) {
      if (this.loop) this.restart(); else if (this.irqEn) this.irq = true;
    }
  }
  clockOutput() {                            // one output-unit clock (every `rate` CPU cycles)
    if (!this.silent) {
      if (this.shift & 1) { if (this.level <= 125) this.level += 2; } else if (this.level >= 2) this.level -= 2;
    }
    this.shift >>= 1;
    if (--this.bits === 0) {
      this.bits = 8;
      if (this.buf < 0) this.silent = true; else { this.silent = false; this.shift = this.buf; this.buf = -1; }
      this.fetch();
    }
  }
  tick() { if (this.timer === 0) { this.timer = this.rate - 1; this.clockOutput(); } else this.timer--; }
}

export class Apu {
  constructor(dmcRead = () => 0) {
    this.p1 = new Pulse(true); this.p2 = new Pulse(false); this.tri = new Triangle(); this.noise = new Noise();
    this.dmc = new Dmc(dmcRead);
    this.mode5 = false; this.irqInhibit = false; this.frameIrq = false; this.seqCycle = 0;
    this.cycle = 0;                          // CPU cycles since power-on (parity = APU cycle phase)
    this.sampleRate = 0; this.out = null; this._acc = 0; this._n = 0; this._frac = 0;
    this._hp1 = { x: 0, y: 0 }; this._hp2 = { x: 0, y: 0 }; this._lp = 0;
  }

  /** Output on: samples at `rate` Hz are pushed to `sink` (an array, or anything with push(number)). */
  setOutput(rate, sink) {
    this.sampleRate = rate; this.out = sink;
    this._step = CPU_HZ / rate; this._frac = 0; this._acc = 0; this._n = 0;
    const dt = 1 / rate, rc = (f) => 1 / (2 * Math.PI * f);
    this._a90 = rc(90) / (rc(90) + dt); this._a440 = rc(440) / (rc(440) + dt); this._b14k = dt / (rc(14000) + dt);
  }

  write(a, v) {
    v &= 0xFF;
    if (a < 0x4004) this.p1.write(a & 3, v);
    else if (a < 0x4008) this.p2.write(a & 3, v);
    else if (a < 0x400C) this.tri.write(a & 3, v);
    else if (a < 0x4010) this.noise.write(a & 3, v);
    else if (a < 0x4014) this.dmc.write(a & 3, v);
    else if (a === 0x4015) {
      const chs = [this.p1, this.p2, this.tri, this.noise];
      chs.forEach((c, i) => { c.enabled = !!(v & (1 << i)); if (!c.enabled) c.len = 0; });
      this.dmc.irq = false;
      if (!(v & 0x10)) this.dmc.remain = 0;
      else if (this.dmc.remain === 0) { this.dmc.restart(); this.dmc.fetch(); }
    } else if (a === 0x4017) {
      this.mode5 = !!(v & 0x80); this.irqInhibit = !!(v & 0x40);
      if (this.irqInhibit) this.frameIrq = false;
      this.seqCycle = 0;
      if (this.mode5) { this._quarter(); this._half(); }
    }
  }

  read(a) {
    if (a !== 0x4015) return 0;
    const v = (this.p1.len ? 1 : 0) | (this.p2.len ? 2 : 0) | (this.tri.len ? 4 : 0) | (this.noise.len ? 8 : 0) |
      (this.dmc.remain ? 0x10 : 0) | (this.frameIrq ? 0x40 : 0) | (this.dmc.irq ? 0x80 : 0);
    this.frameIrq = false;
    return v;
  }

  _quarter() { this.p1.env.clock(); this.p2.env.clock(); this.noise.env.clock(); this.tri.clockLinear(); }
  _half() { this.p1.clockLen(); this.p2.clockLen(); this.tri.clockLen(); this.noise.clockLen(); this.p1.clockSweep(); this.p2.clockSweep(); }

  _seqEvent(i) {
    this._quarter();
    if (i === 1 || i === 3) this._half();
    if (i === 3 && !this.mode5 && !this.irqInhibit) this.frameIrq = true;
  }

  /** One CPU cycle of the frame sequencer: fire the event at this count, then count (wrap after the last). */
  _seqTick() {
    const s = this.mode5 ? SEQ5 : SEQ4, c = this.seqCycle;
    if (c === s[0]) this._seqEvent(0);
    else if (c === s[1]) this._seqEvent(1);
    else if (c === s[2]) this._seqEvent(2);
    else if (c === s[3]) { this._seqEvent(3); this.seqCycle = 0; return; }
    this.seqCycle = c + 1;
  }

  /** n CPU cycles of the frame sequencer, event to event (same result as n _seqTick calls). */
  _seqRun(n) {
    const s = this.mode5 ? SEQ5 : SEQ4;
    let c = this.seqCycle;
    while (n > 0) {
      let i = 0;
      while (s[i] < c) i++;
      const d = s[i] - c;
      if (d >= n) { c += n; break; }
      n -= d + 1;
      this._seqEvent(i);
      c = i === 3 ? 0 : s[i] + 1;
    }
    this.seqCycle = c;
  }

  run(n) {
    if (!this.sampleRate) {                  // headless: sequencer + DMC fetches only
      this._seqRun(n);
      let k = n;
      const d = this.dmc;
      while (k > 0) {
        if (d.timer >= k) { d.timer -= k; break; }
        k -= d.timer + 1; d.timer = d.rate - 1; d.clockOutput();
      }
      this.cycle += n;
      return;
    }
    const { p1, p2, tri, noise, dmc } = this;
    for (let i = 0; i < n; i++) {
      this._seqTick();
      if (this.cycle & 1) { p1.tick(); p2.tick(); }
      tri.tick(); noise.tick(); dmc.tick();
      this.cycle++;
      this._acc += PULSE_MIX[p1.out + p2.out] + TND_MIX[3 * tri.out + 2 * noise.out + dmc.level];
      this._n++;
      if (++this._frac >= this._step) { this._frac -= this._step; this._emit(); }
    }
  }

  _emit() {
    const x = this._acc / this._n; this._acc = 0; this._n = 0;
    const h1 = this._hp1, h2 = this._hp2;
    h1.y = this._a90 * (h1.y + x - h1.x); h1.x = x;
    h2.y = this._a440 * (h2.y + h1.y - h2.x); h2.x = h1.y;
    this._lp += this._b14k * (h2.y - this._lp);
    this.out.push(this._lp);
  }
}
