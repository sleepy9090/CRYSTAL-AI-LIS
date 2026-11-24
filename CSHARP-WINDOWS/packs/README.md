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

**Made by:** `python3 tools/hero_pack.py packs/hero` (generated from the game's own sprite data by the painting rules
in that script; the output is deterministic). Hand-edit any PNG to improve it further — keep the name and 32×32 size;
`python3 tools/pack_check.py packs/hero` checks a folder the way the exe does.
