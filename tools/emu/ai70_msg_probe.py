#!/usr/bin/env python3
"""s173: register trace of AI $70 phase 1's table end 27:BB1A (messages $1B.05-07 via FarCall_D354, then
PlaySound($25), HudHpBar, HudRedraw(4)) in the `final_locked` scenario: X / Y going into each PlaySound and
after each ShowMessage, and snd_saved_y $010F.
usage: python3 -m tools.emu.ai70_msg_probe [scenario] [frames]"""
import sys
from tools.emu import scenario

PCS = [(27, 0xBB26, 'table end  '), (27, 0xBB34, 'PlaySnd 29 '), (27, 0xBB37, 'FarCall    '),
       (27, 0xBB3A, 'after msg  '), (27, 0xBB55, 'PlaySnd 25 '), (27, 0xBB58, 'after snd  '),
       (27, 0xBB5E, 'HudHpBar   '), (27, 0xBB61, 'after HPbar'), (27, 0xBB69, 'HudRedraw4 '),
       (27, 0xBB6C, 'after HR   ')]   # (BB78 RTS is shared by every early exit: not hooked)


def main():
    name = sys.argv[1] if len(sys.argv) > 1 else 'final_locked'
    sc = dict(scenario.SCENARIOS[name])
    if len(sys.argv) > 2:
        sc['frames'] = int(sys.argv[2])
    log = []

    def hook(tag):
        def f(nes):
            r, c = nes.bus.ram, nes.cpu
            log.append(f'  f{nes.ppu.frame_no}: {tag} A={c.a:02X} X={c.x:02X} Y={c.y:02X} S={c.s:02X} '
                       f'msg={r[0x07DF]:02X}.{r[0x07DE]:02X} $010F={r[0x010F]:02X} sub={r[0x41]:02X} '
                       f'irq={r[0x51]:02X} $6E/$6F={r[0x6E]:02X}/{r[0x6F]:02X}')
        return f
    sc['exec'] = list(sc.get('exec', [])) + [(b, pc, hook(t)) for b, pc, t in PCS]
    _, _, end = scenario.run(sc)
    print('\n'.join(log[:80]))
    print(end)


if __name__ == '__main__':
    main()
