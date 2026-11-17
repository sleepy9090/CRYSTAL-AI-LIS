#!/usr/bin/env python3
"""Tests for emu 4/6: save states, hook API, run-func, tracer, --keys parsing, CLI.
Needs rom/re-c.nes. usage: python3 tools/emu/test_cli.py"""
import io
import os
import sys
import tempfile
import zlib

# import as tools.emu from the repo root: putting tools/ first on sys.path would let
# tools/dis.py shadow the stdlib `dis` that argparse needs
sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))))
from tools.emu.nes import NES, Stop  # noqa: E402
from tools.emu import state, debug  # noqa: E402
from tools.emu.__main__ import main as cli_main  # noqa: E402

RESULTS = []


def case(fn):
    try:
        fn()
        RESULTS.append((fn.__name__, None))
    except Exception as e:  # noqa: BLE001
        RESULTS.append((fn.__name__, f'{type(e).__name__}: {e}'))
    return fn


def digest(nes):
    return (zlib.crc32(bytes(nes.bus.ram)), zlib.crc32(bytes(nes.bus.wram)),
            zlib.crc32(bytes(nes.ppu.fb)), nes.cpu.pc, nes.cpu.cycles)


@case
def state_roundtrip_is_deterministic():
    keys = debug.parse_keys(['1250:START', '1400:START'])
    a = NES()
    a.keys = keys
    a.run_frames(1300)
    with tempfile.TemporaryDirectory() as d:
        p = os.path.join(d, 's.state')
        state.save(a, p)
        a.run_frames(200)
        b = NES()
        b.keys = keys
        state.load(b, p)
        assert b.ppu.frame_no == 1300
        b.run_frames(200)
    assert digest(a) == digest(b), (digest(a), digest(b))


@case
def run_func_switchbank16():
    nes = NES()
    nes.run_frames(3)
    n = nes.call(0xC40E, 30, a=5)            # SwitchBank16 A=5 -> banks 10/11 at $8000/$A000
    assert nes.mapper.prg_banks[:2] == (10, 11), nes.mapper.prg_banks
    assert 0 < n < 50, n


@case
def exec_hook_is_bank_aware():
    nes = NES()
    seen = {'any': 0, 'b31': 0, 'b0': 0}
    nes.on_exec(None, 0xF2A4, lambda n: seen.__setitem__('any', seen['any'] + 1))
    nes.on_exec(31, 0xF2A4, lambda n: seen.__setitem__('b31', seen['b31'] + 1))
    nes.on_exec(0, 0xF2A4, lambda n: seen.__setitem__('b0', seen['b0'] + 1))
    nes.run_frames(1)
    assert seen == {'any': 1, 'b31': 1, 'b0': 0}, seen


@case
def break_stops_before_instruction():
    nes = NES()

    def brk(n):
        raise Stop('x')
    nes.on_exec(30, 0xC40E, brk)
    try:
        nes.run_frames(5)
        raise AssertionError('no break')
    except Stop:
        pass
    assert nes.cpu.pc == 0xC40E


@case
def write_hook_and_frame_hook():
    nes = NES()
    w, fr = [], []
    nes.on_write(0x2000, lambda a, v: w.append(v))
    nes.on_write(0x2000, lambda a, v: w.append(-v))     # second hook on same addr chains
    nes.on_frame(lambda n: fr.append(n.ppu.frame_no))
    nes.run_frames(3)
    assert w and w[0] == 0 and w[1] == 0 and len(w) % 2 == 0, w[:6]
    assert fr == [1, 2, 3], fr
    nes.clear_hooks()
    assert nes.cpu.trace is None


@case
def tracer_format():
    nes = NES()
    out = io.StringIO()
    nes.set_tracer(debug.Tracer(nes, out, max_lines=3))
    nes.run_frames(1)
    lines = out.getvalue().splitlines()
    assert lines[0] == 'Reset:', lines
    assert '31:F2A4  78        SEI' in lines[1], lines[1]
    assert '31:F2A6  a2 ff     LDX #$FF' in lines[3], lines[3]


@case
def parse_keys_spec():
    k = debug.parse_keys(['120:START', '300:A+RIGHT:3'])
    assert k == {120: 8, 121: 8, 300: 0x81, 301: 0x81, 302: 0x81}, k


@case
def resolve_labels():
    assert debug.resolve('SwitchBank16') == (30, 0xC40E)
    assert debug.resolve('31:F2A4') == (31, 0xF2A4)
    assert debug.resolve('F2A4') == (31, 0xF2A4)


@case
def cli_dumps_and_state():
    with tempfile.TemporaryDirectory() as d:
        st, ram, wram, png = (os.path.join(d, n) for n in ('a.state', 'r.bin', 'w.bin', 's.png'))
        so = sys.stdout
        sys.stdout = io.StringIO()
        try:
            rc = cli_main(['--frames', '10', '--save-state', st, '--dump-ram', ram,
                           '--dump-wram', wram, '--dump-screen', png])
            rc2 = cli_main(['--load-state', st, '--frames', '0', '--run-func', 'SwitchBank16',
                            '--a', '2', '--dump-regs'])
            txt = sys.stdout.getvalue()
        finally:
            sys.stdout = so
        assert rc == 0 and rc2 == 0
        assert os.path.getsize(ram) == 0x800 and os.path.getsize(wram) == 0x2000
        assert open(png, 'rb').read(4) == b'\x89PNG'
        assert 'run-func SwitchBank16 (30:C40E): returned' in txt, txt
        assert 'prg=(4, 5, 30, 31)' in txt, txt


@case
def cli_compare_csv():
    """emu 6/6 (c): --compare writes a header + one row per frame, player slot filled in gameplay."""
    from tools.emu import compare
    st = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'states', 'area1_enter.state')
    with tempfile.TemporaryDirectory() as d:
        out = os.path.join(d, 'c.csv')
        rc = cli_main(['--load-state', st, '--frames', '5', '--compare', out])
        rows = [ln.split(',') for ln in open(out).read().splitlines()]
    assert rc == 0
    assert rows[0] == compare.columns() and len(rows) == 6, (len(rows), rows[0][:5])
    c = rows[0].index
    first = [int(v) for v in rows[1]]
    assert first[c('frame')] == 2450 and first[c('area')] == 1, first[:6]
    assert first[c('o01_ai')] == 1 and first[c('o01_x')] > 0, first[c('o01_ai'):c('o01_ai') + 3]
    assert [int(r[0]) for r in rows[1:]] == list(range(2450, 2455))


if __name__ == '__main__':
    bad = 0
    for name, err in RESULTS:
        print(('PASS ' if err is None else 'FAIL ') + name + ('' if err is None else '  ' + err))
        bad += err is not None
    print(f'{len(RESULTS) - bad}/{len(RESULTS)} passed')
    sys.exit(1 if bad else 0)
