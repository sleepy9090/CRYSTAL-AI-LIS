# web/test/world_frames (s53)

RAM / WRAM after one frame from a `tools/emu/states/*.state`, the same frame `web/test/ppu_frames/NAME.json`
records (`python3 -m tools.emu.ppu_lines`). `web/test/world_check.mjs` loads them into a Machine, renders the
loaded area with `world/bg.js` and compares its BG against the emulator framebuffer in `ppu_frames/NAME.json`
(sprite pixels, the PPUMASK-hidden left 8 px and the HUD lines from the first IRQ split on are not compared).

Regenerate:

    python3 -m tools.emu --load-state tools/emu/states/area1_enter.state --frames 1 \
        --dump-ram web/test/world_frames/area1_enter.ram --dump-wram web/test/world_frames/area1_enter.wram
    python3 -m tools.emu --load-state tools/emu/states/gameplay_first.state --frames 1 \
        --dump-ram web/test/world_frames/gameplay_first.ram --dump-wram web/test/world_frames/gameplay_first.wram
