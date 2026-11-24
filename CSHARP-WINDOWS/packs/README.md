# Graphics packs for ReC.exe (Video › Graphics › Enhanced)

## `hero/` — starter pack for the main character
133 tiles, 32×32 each, for the player sprite in its normal colours (`36140F` = skin, pink hair / clothes, black):
walk in all four directions, sword attacks, and the special poses, for both body graphics (without / with the shield)
and the sword. `hero_preview.png` shows every frame: NES original (left) and the pack (right).

The style is a 16-bit repaint at the same pixel size as the NES sprite (the SNES had the same resolution): richer
colour ramps for skin, hair and clothes with light from the top left, a tinted outline instead of flat black, pink
highlights in the hair, and the usual Enhanced rounding of the corners.

**Install:** copy the PNGs from `hero/` into `%APPDATA%\re-c\pack\` (create the folder), start ReC.exe, press F9 for
Enhanced graphics (F5 reloads the pack while playing). Or start `ReC.exe --pack path\to\hero`.

**Limits:** a pack tile only replaces its tile in exactly these colours. During a fade, a hit flash or another palette,
the hero falls back to the built-in Enhanced look for those frames.

**Made by (hero):** `python3 tools/hero_pack.py packs/hero` (generated from the game's own sprite data by the painting rules
in that script; the output is deterministic). Hand-edit any PNG to improve it further — keep the name and 32×32 size;
`python3 tools/pack_check.py packs/hero` checks a folder the way the exe does.

## `enemies/` — starter pack for the enemies and bosses
461 tiles (402 different tiles in their usual colours) for the enemies and bosses seen in 32 combat / boss scenarios
of the test suite (field groups, area enemies, the bosses, the final fights). `enemies_preview.png` shows one or two
objects per scenario: NES (left) and the pack (right).

Same style as the hero: every NES pixel keeps its place and gets a richer colour, from a 5-tone ramp built for each
NES colour (shadows shift toward blue-violet, highlights toward warm yellow), light from the top left, a tinted outline
and near-black eyes / seams, then the Enhanced rounding.

**Install:** copy `enemies/*.png` into the same `%APPDATA%\re-c\pack\` folder as the hero tiles (the names never
collide), press F9 / F5.

**Limits:** only enemies the scenarios show are covered (other areas / colour sets keep the built-in look); colours
that last only a moment (fade-ins, hit flashes) are left out on purpose; the player's own palette is not touched.

**Made by:** `python3 tools/sprite_capture.py` (headless core runs with `--dump-tags`, about a minute) then
`python3 tools/sprite_pack.py` (paints each object as it appears on screen and cuts it back into tiles keyed exactly as
the exe looks them up). Rerun both to cover more scenarios.

## `backgrounds/` — starter pack for the scenery
1,839 tiles (1,189 different tiles in their usual colours) for the backgrounds of every screen the test suite visits
(86 game situations: towns, shops, the inn, fields, forests, caves, ice, deserts, dungeons, boss rooms, the ending).
`backgrounds_preview.png` shows eight of them: NES (left) and the pack over the NES picture (right).

Same 16-bit style as the sprite packs: every NES pixel keeps its place and gets a richer colour from the 5-tone ramp of
its NES colour. Where a shape meets a clearly darker ground or outline it gets a lit rim at the top / left and a shadow
at the bottom / right (bricks, cobbles, rocks, shingles, fences stand out); two-tone shapes keep the NES art's own
shading, near-white highlights stay white, dither dots and cracks keep their colour, then the Enhanced rounding. Each
tile is painted from its own pixels, so floors and walls repeat without seams.

**Install:** copy `backgrounds/*.png` into the same `%APPDATA%\re-c\pack\` folder (names never collide with the hero /
enemy tiles), press F9 / F5. Works in Enhanced (16-bit) and Enhanced (32-bit).

**Left alone on purpose:** the status bar, window frames, menus and all text (CHR banks 38 / 39 / 3C / 3D, keep them
crisp), the backdrop colour (transparent in the tiles: the built-in look shows there), and colours that last only a
moment (fades).

**Limits:** only scenery the test runs show is covered (other areas keep the built-in look); a tile shown with another
palette (e.g. the same wall in another town's colours) needs its own copy under that name.

**Made by:** `python3 tools/sprite_capture.py --out out/bg --every 16 <every state in ../web/test/states>` (about two
minutes) then `python3 tools/bg_pack.py` (about six minutes; `--stats` lists the CHR banks the backgrounds use,
`tools/tag_banks.py` the ones in a screen rectangle). Deterministic.

## `npcs/` — starter pack for the townspeople
121 tiles for the villagers, shopkeepers and other people of the towns, shops, the inn and the story scenes (35 test
situations), plus two big sprites nobody else covers: the tower of the ending and the item on display in the shops.
`npcs_preview.png` shows each one: NES (left) and the pack (right).

The sprite packs' style in a gentler version for people: half-step light / shadow tones (no extremes, so faces stay
soft), black hair, beards and robes stay near-black, plain dark outlines, then the Enhanced rounding. Townspeople who
wear the hero's colours are covered too; the hero's own tiles are not touched.

**Install:** copy `npcs/*.png` into the same `%APPDATA%\re-c\pack\` folder (names never collide with the other three
packs: tiles the enemy pack already paints are left out), press F9 / F5.

**Limits:** only people the test runs show (other towns' residents keep the built-in look); fade-out steps and menu
cursors / small icons are left out.

**Made by:** the background capture (`out/bg`, see above), then
`python3 tools/sprite_pack.py --in out/bg --only armorshop,cure,ending,event01,event02,event07,event08,event11,event12,event13,event15,event19,event19b,event20,event21,event23,event28,giveitem,group18,inn,mesia,pawnshop,shopempty,shopmenu,talk16,talk19,talk20,talk22,talk25,talk26,talk_act1,talk_act8,talk_probe,toolshop,town02_enter --out packs/npcs --skip none --exclude packs/enemies --palettes 0,1,2,3 --min-height 12 --no-dark --soft-dark`
(under a minute; `tools/sprite_tiles.py DUMP.bin` lists the sprite tiles per palette of a dump).

## `bosses/` — starter pack for the bosses (adds to `enemies/`)
104 tiles (87 different tiles) that the enemy pack does not have: bosses drawn in palette 0 (the hero's palette, which
the enemy pack leaves out: the boss of the area-$28 speech, the fan turret, the AI-$70 boss of area $5F), later attack
phases of the boss fights, their 8-pixel pieces and shots, the figures of the final scenes and what the bosses leave
behind (chest, item). `bosses_preview.png`: NES (left) and the pack (right).

Same look as the enemy pack. Measured with `tools/pack_coverage.py` over 1,500-frame runs of the 9 boss states, boss
sprite pixels covered by enemies + bosses: area-$28 speech 4 -> 100 %, fan turret 3 -> 100 %, final locked / final scene
-> 100 %, AI $70 message 17 -> 91 %, $1A boss 67 -> 83 %, final boss 76 -> 87 %, area $5F 21 -> 75 %. The rest are
all-black silhouettes (one colour: nothing to paint) and hit-flash colours (left to the built-in look on purpose).

**Install:** copy `bosses/*.png` into the same `%APPDATA%\re-c\pack\` folder (no name collides with the other packs).

**Made by:** `python3 tools/sprite_capture.py --out out/boss/dumps --frames 1500 --every 8 bosse2 bossflee finalboss
finallocked finalscene ai70msg restore5f fanturret fanskip`, then `python3 tools/sprite_pack.py --in out/boss/dumps
--out packs/bosses --skip none --exclude packs/enemies,packs/npcs --palettes 0,1,2,3 --no-dark`.
