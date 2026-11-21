#!/usr/bin/env python3
"""s132: per-frame order of HitShotEater (26:9073) vs HitPlayer's enemy-shot removal (26:942B) and
ObjCollideAll's record index, for the `shot_eater` scenario. usage: python3 -m tools.emu.shot_eater_probe"""
from tools.emu import scenario


def main():
    sc = dict(scenario.SCENARIOS['shot_eater'])
    log = []

    def hook(tag):
        def f(nes):
            r = nes.bus.ram
            log.append(f'  f{nes.ppu.frame_no}: {tag} X={nes.cpu.x:02X} Y={nes.cpu.y:02X} '
                       f'$0C act {r[0x04AC]:02X} ph {r[0x060C]:02X} scr {r[0x05CC]:02X},{r[0x05EC]:02X}')
        return f
    sc['exec'] = list(sc.get('exec', [])) + [(26, 0x9073, hook('eater   ')), (26, 0x942B, hook('hitplayer')),
                                             (26, 0x8FC4, hook('handler3 '))]
    sc['frames'] = 120
    _, _, end = scenario.run(sc)
    print('\n'.join(log[:60]))
    print(end)


if __name__ == '__main__':
    main()
