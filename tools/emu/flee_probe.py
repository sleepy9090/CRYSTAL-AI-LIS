#!/usr/bin/env python3
"""s160: register / RAM trace of BossFleeSpeech 15:BA98 (record hook of areas $28/$6E/$F2) in the `boss_flee`
scenario: X after StatRecalc, the BossFleeRecs record, ClearEnemySlots, ShowMessage and the walk-off loop.
usage: python3 -m tools.emu.flee_probe [scenario] [frames]"""
import sys
from tools.emu import scenario

PCS = [(15, 0xBA98, 'entry      '), (15, 0xBAAB, 'StatRecalc '), (15, 0xBAAE, 'after SR   '),
       (15, 0xBAB3, 'STX $1F    '), (15, 0xBAC6, 'LDX $1F    '), (15, 0xBAF0, 'ClrEnemies '),
       (15, 0xBAF8, 'ShowMsg    '), (15, 0xBAFB, 'after msg  '), (15, 0xBB5B, 'walk loop  '),
       (15, 0xBB89, 'walk done  ')]


def main():
    name = sys.argv[1] if len(sys.argv) > 1 else 'boss_flee'
    sc = dict(scenario.SCENARIOS[name])
    if len(sys.argv) > 2:
        sc['frames'] = int(sys.argv[2])
    log = []
    seen = {}

    def hook(tag, pc):
        def f(nes):
            n = seen.get(pc, 0)
            seen[pc] = n + 1
            if pc == 0xBB5B and 3 < n and n % 16:
                return
            r = nes.bus.ram
            c = nes.cpu
            log.append(f'  f{nes.ppu.frame_no}: {tag} A={c.a:02X} X={c.x:02X} Y={c.y:02X} $1F={r[0x1F]:02X} '
                       f'ph0F={r[0x060F]:02X} msg={r[0x07DF]:02X}.{r[0x07DE]:02X} s1F act={r[0x04BF]:02X} '
                       f'meta={r[0x031F]:02X} tile={r[0x033F]:02X} x={r[0x90 + 0x1F]:02X}{r[0x70 + 0x1F]:02X} '
                       f'y={r[0xD0 + 0x1F]:02X}{r[0xB0 + 0x1F]:02X} anim={r[0x04FF]:02X} $CF={r[0xCF]:02X} '
                       f'sub={r[0x41]:02X} irq={r[0x51]:02X}')
        return f
    sc['exec'] = list(sc.get('exec', [])) + [(b, pc, hook(t, pc)) for b, pc, t in PCS]
    _, _, end = scenario.run(sc)
    print('\n'.join(log[:120]))
    print(end)


if __name__ == '__main__':
    main()
