#!/usr/bin/env python3
"""Which areas does the visited-screen substitution (ScreenTileSubst 31:ED99) actually change?

usage: visited_scan.py [--render]     -> gfx/visited_scan.txt (+ --render: render_level --visited
                                         for every area with a visible change -> gfx/areas/area_NNN_v.png)

Every screen the player stands on gets its $62F0 bit (30:C75C), so any area can show the
substitution; k4 (30:C765 / 31:E702) only decides which screens keep the bit across area loads.
Per grid cell: metatiles M < $20 of the screen page (bank-10 rule as render_screen) whose
substitute 9:$BE00+($67&$3C)*8+M draws differently (other quadrant tiles or attribute).
Static only (s68).
"""
import os, sys
sys.path.insert(0, os.path.dirname(__file__))
import render_screen as rs  # noqa
import area_dump  # noqa
from nesrom import ROOT  # noqa


def k4_cells(area):
    _, ps = area_dump.sub_header(area)
    p, out = ps[4], []
    while area_dump.rd10(p) != 0xFF and len(out) < 64:
        out.append((area_dump.rd10(p), area_dump.rd10(p + 1)))
        p += 2
    return out


def cell_changes(a, s):
    v67 = a['v67']
    page = rs.page_bytes(s, a['bank10'])
    n = 0
    for i in range(240):
        m = page[i]
        if m < 0x20:
            t = rs.subst(v67, m)
            if t != m and (rs.mt_tiles(v67, t) != rs.mt_tiles(v67, m) or rs.mt_attr(v67, t) != rs.mt_attr(v67, m)):
                n += 1
    return n


def main(argv):
    lines, hits = [], []
    nareas = 0
    tally = {'k4_changed': 0, 'k4_none': 0, 'free_changed': 0, 'free_areas': set(), 'k4_none_list': []}
    for area in range(256):
        a = rs.area_bg(area)
        if a is None:
            continue
        nareas += 1
        cells = {}
        for gy, row in enumerate(a['grid']):
            for gx, s in enumerate(row):
                n = cell_changes(a, s)
                if n:
                    cells[gy << 4 | gx] = (s, n)
        k4 = k4_cells(area)
        if not cells and not k4:
            continue
        k4s = {scr for _, scr in k4}
        tot = sum(n for _, n in cells.values())
        lines.append(f'area ${area:02X}  tileset $67={a["v67"]:02X}  cells changed {len(cells)}  metatiles {tot}  '
                     f'k4 {len(k4)}' + ('' if not k4 else
                                        f' ({sum(1 for c in k4s if c in cells)} of them changed)'))
        for c, (s, n) in sorted(cells.items()):
            lines.append(f'    cell y{c >> 4:X} x{c & 15:X} screen ${s:02X}: {n} metatiles'
                         + ('  [k4]' if c in k4s else ''))
            if c in k4s:
                tally['k4_changed'] += 1
            else:
                tally['free_changed'] += 1; tally['free_areas'].add(area)
        for f, scr in k4:
            if scr not in cells:
                lines.append(f'    k4 f{f:02X}@scr{scr:02X}: no visible change')
                tally['k4_none'] += 1; tally['k4_none_list'].append(f'{area:02X}:f{f:02X}@{scr:02X}')
        if cells:
            hits.append(area)
    head = [f'{nareas} areas scanned; {len(hits)} with a visible visited substitution: '
            + ' '.join(f'{x:02X}' for x in hits),
            f'changed cells listed in k4 (persist across loads): {tally["k4_changed"]}; '
            f'changed cells not in k4 (change only while the area stays loaded): {tally["free_changed"]} in areas '
            + ' '.join(f'{x:02X}' for x in sorted(tally['free_areas'])),
            f'k4 entries whose screen shows no change: {tally["k4_none"]}: ' + ' '.join(tally['k4_none_list'])]
    with open(os.path.join(ROOT, 'gfx', 'visited_scan.txt'), 'w') as f:
        f.write('\n'.join(head + lines) + '\n')
    print('\n'.join(head))
    if '--render' in argv:
        import render_level  # noqa
        for area in hits:
            render_level.render_area(area, visited=True)
        print(f'rendered {len(hits)} visited variants to gfx/areas/')


if __name__ == '__main__':
    main(sys.argv[1:])
