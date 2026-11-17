# web/test/sprite_frames (s54)

RAM before / after the object sprite pass `ObjSpriteDraw` `28:800E`, captured by `tools/emu/sprite_capture.py`.
`web/test/sprite_check.mjs` loads each `before` into a Machine, runs `obj/sprites.js` `spriteDraw()` and compares
the whole 2KB RAM with `before + after` (stack scratch `$0100+sp-15 .. $0100+sp+2` excluded).

- `real_canonical.json`: every 2nd pass of a real run (new game, start room, area 0 -> 1, walking + one sword swing).
- `fuzz.json`: 500 cases with randomised camera, draw rotation, `$08`, hit flash, orbit level / burst timer, OAM and
  all 32 object slots, run through `nes.call(28:800E)`. This set covers what the real run does not: mirrored
  records, overlays, sinking, OAM overflow, the burst ring, the half-rate flicker.

Regenerate:

    python3 -m tools.emu.sprite_capture real --load-state tools/emu/states/name_entry.state --frames 260 \
        --keys 2300:START 2340:DOWN:80 2430:RIGHT:60 2470:A 2490:LEFT:40 --every 2 \
        --out web/test/sprite_frames/real_canonical.json
    python3 -m tools.emu.sprite_capture fuzz --load-state tools/emu/states/gameplay_first.state --cases 500 \
        --out web/test/sprite_frames/fuzz.json
