"""ROM reference traces for the web-port core check (Ph7 web 2/8) -> web/test/core_trace.json.

    python3 -m tools.emu.core_trace [-o web/test/core_trace.json]

1. pads: from tools/emu/states/title.state, call the real ReadPads (31:FE80) and ReadPadsMenu
   (31:FEE0) once per step over a fixed 160-step button script (long A/B holds, d-pad holds and
   changes, chords). Records $43-$4F before the run and after every call.
2. nmi: for every state in tools/emu/states/ (not tmp_*), run 6 frames and, for each NMI, record RAM
   + WRAM at NMI entry (31:F3B6) and the PPU/MMC3 state at entry and after `INC frame_ready`
   (31:F41A -> hook at F41C): t, x, w, ctrl, mask, palette RAM, OAM, CHR R0-R5, $56, IRQ latch/enable.
   web/test/core_check.mjs runs nmiWork() from the entry RAM and compares what the port models.

Button bytes in this file are in ROM order (A b7 ... Right b0, what ReadPadRaw assembles);
tools.emu Pads use A = b0, so they are bit-reversed before Pads.set().
"""
import argparse
import glob
import json
import os

from tools.emu.nes import NES
from tools.emu import state

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))


def rev8(b):
    return int(f'{b & 0xFF:08b}'[::-1], 2)


def button_script():
    """160 ROM-order button bytes: holds longer than 32 frames, d-pad repeat, chords."""
    seq = []
    seq += [0x00] * 3
    seq += [0x80] * 70              # A held 70 frames (pad_act b7 from the 32nd)
    seq += [0x00] * 2
    seq += [0x40] * 40              # B held 40 (b6 pulse at 32)
    seq += [0x08] * 20              # Up held (menu repeat: 12, then every 3)
    seq += [0x09, 0x09, 0x01, 0x05, 0x05, 0x0C, 0x03, 0x00]   # diagonal, change, U+D, L+R
    seq += [0xC0] * 10 + [0x30, 0x20, 0x10, 0x00, 0xFF, 0xFF, 0x00]
    assert len(seq) == 160, len(seq)
    return seq


def zp_pads(nes):
    return [nes.bus.ram[a] for a in range(0x43, 0x50)]


def pad_trace(state_path):
    out = {}
    for name, addr in (('ReadPads', 0xFE80), ('ReadPadsMenu', 0xFEE0)):
        nes = NES()
        state.load(nes, state_path)
        steps = []
        start = zp_pads(nes)
        for b in button_script():
            nes.pads.set(0, rev8(b))
            nes.pads.set(1, 0)
            nes.call(addr)
            steps.append(zp_pads(nes))
        out[name] = {'addr': f'31:{addr:04X}', 'start': start, 'steps': steps}
    out['buttons'] = button_script()
    return out


def ppu_regs(nes):
    p, m = nes.ppu, nes.mapper
    return {
        't': p.t, 'v': p.v, 'x': p.x, 'w': p.w, 'ctrl': p.ctrl, 'mask': p.mask,
        'pal': bytes(p.pal).hex(), 'oam': bytes(p.oam).hex(), 'chr': list(m.regs[:6]),
        'irq_latch': m.irq_latch, 'irq_enabled': bool(m.irq_enabled),
    }


def nmi_trace(state_path, frames=6):
    nes = NES()
    state.load(nes, state_path)
    recs, cur = [], {}

    def entry(n):
        cur.clear()
        cur['frame'] = n.ppu.frame_no
        cur['line'] = n.ppu.line
        cur['ram'] = bytes(n.bus.ram).hex()
        cur['wram'] = bytes(n.bus.wram).hex() if hasattr(n.bus, 'wram') else None
        cur['pre'] = ppu_regs(n)

    def leave(n):
        if 'pre' not in cur:
            return
        r = dict(cur)
        r['post'] = ppu_regs(n)
        r['post_ram'] = bytes(n.bus.ram).hex()
        recs.append(r)
        cur.clear()

    nes.on_exec(None, 0xF3B6, entry)
    nes.on_exec(None, 0xF41C, leave)
    nes.run_frames(frames)
    nes.clear_hooks()
    return recs


def main():
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument('-o', '--out', default=os.path.join(ROOT, 'web/test/core_trace.json'))
    a = ap.parse_args()
    states = sorted(p for p in glob.glob(os.path.join(ROOT, 'tools/emu/states/*.state'))
                    if not os.path.basename(p).startswith('tmp_'))
    out = {'pads': pad_trace(os.path.join(ROOT, 'tools/emu/states/title.state')), 'nmi': {}}
    for p in states:
        out['nmi'][os.path.basename(p)[:-6]] = nmi_trace(p)
    with open(a.out, 'w') as f:
        json.dump(out, f)
    n = sum(len(v) for v in out['nmi'].values())
    print(f'wrote {os.path.relpath(a.out, ROOT)}: pads 2x{len(out["pads"]["buttons"])} steps, {n} NMIs')


if __name__ == '__main__':
    main()
