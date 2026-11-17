"""Save states for the re-c emulator (Ph5.5 emu 4/6).

    from emu import state
    state.save(nes, 'tools/emu/states/title.state')
    state.load(nes, 'tools/emu/states/title.state')   # nes = NES() built from the same ROM

File = gzip-compressed pickle of a plain dict (version, CPU registers + cycle counter + pending
interrupts, 2KB RAM, 8KB WRAM, bus open-bus value, mapper.state(), ppu.state() incl. the
framebuffer and the in-flight tile/sprite line buffers, apu.state(), pads.state()). Debug hooks,
the tracer and logs are NOT saved. Only load state files you made yourself (pickle).
Restoring a state and running N frames gives the same RAM/WRAM/framebuffer as running straight
through (checked by test_cli.py).
"""
import gzip
import pickle

VERSION = 1
_CPU_KEYS = ('a', 'x', 'y', 's', 'p', 'pc', 'cycles', 'stall', 'op_cycles', 'nmi_pending',
             'irq_flags')


def snapshot(nes):
    cpu, bus = nes.cpu, nes.bus
    return {
        'version': VERSION,
        'rom': nes.cart.path,
        'cpu': {k: getattr(cpu, k) for k in _CPU_KEYS},
        'ram': bytes(bus.ram),
        'wram': bytes(bus.wram),
        'open_bus': bus.open_bus,
        'mapper': nes.mapper.state(),
        'ppu': nes.ppu.state(),
        'apu': nes.apu.state(),
        'pads': nes.pads.state(),
    }


def restore(nes, d):
    if d.get('version') != VERSION:
        raise ValueError(f'state version {d.get("version")} != {VERSION}')
    bus = nes.bus
    bus.ram[:] = d['ram']
    bus.wram[:] = d['wram']
    bus.open_bus = d['open_bus']
    nes.mapper.load_state(d['mapper'])
    nes.ppu.load_state(d['ppu'])
    nes.apu.load_state(d['apu'])
    nes.pads.load_state(d['pads'])
    for k, v in d['cpu'].items():          # last: mapper.load_state touches cpu.irq_flags
        setattr(nes.cpu, k, v)


def save(nes, path):
    with gzip.open(path, 'wb') as f:
        pickle.dump(snapshot(nes), f, protocol=pickle.HIGHEST_PROTOCOL)


def load(nes, path):
    with gzip.open(path, 'rb') as f:
        restore(nes, pickle.load(f))
