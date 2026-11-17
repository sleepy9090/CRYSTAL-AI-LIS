-- re-c reference capture for FCEUX (Ph6 validate 1/4, session 40).
--
-- Replays the canonical tools.emu --keys script from power-on and, after each listed frame,
-- writes into OUTDIR (default "ref", override with env RE_C_REF_DIR):
--   frame_NNNN.ram   2048 bytes  CPU $0000-$07FF  (OAM shadow = $0200-$02FF, DMA'd by 31:F3D3)
--   frame_NNNN.wram  8192 bytes  CPU $6000-$7FFF  (MMC3 WRAM; reads open bus if the game disabled it)
--   frame_NNNN.nt    4096 bytes  PPU $2000-$2FFF  (through the mapper's mirroring)  [needs ppu.readbyte]
--   frame_NNNN.pal     32 bytes  PPU $3F00-$3F1F                                     [needs ppu.readbyte]
--   frame_NNNN.png   screenshot (gui.savescreenshotas), or frame_NNNN.gd (gui.gdscreenshot, old FCEUX)
--   capture.txt      one line per frame: frame, fceux framecount, which files were written
-- FCEUX Lua cannot read OAM itself; tools/ref/compare_ref.py compares the $0200 shadow page instead.
--
-- Run from the repo root (delete any rom/re-c.sav first so WRAM starts like tools.emu):
--   fceux --loadlua tools/ref/fceux_capture.lua rom/re-c.nes
-- (headless: prefix with xvfb-run -a).  The script powers the console on, so frame 0 = first frame
-- after power-on, the same numbering as tools.emu ppu.frame_no / --keys.  Then:
--   python3 tools/ref/compare_ref.py            (writes docs/ref_compare.txt)
--
-- Frame alignment: the label N means "state after N+1 frameadvance() calls since power-on" and pad
-- input for frame N is set before the (N+1)th frameadvance().  FCEUX frames start at vblank while
-- tools.emu frames end at the line 261->0 wrap, so compare_ref.py searches a small frame offset.

local OUTDIR = os.getenv("RE_C_REF_DIR") or "ref"

-- canonical script, REVERSE.md (session 38): FRAME:BUTTONS[:LEN], default LEN 2
local KEYS = "1250:START 1400:START 1600:START 1700:START 2300:START 2340:DOWN:80"

-- frames to capture: tools.emu dumps in gfx/emu (frame_0120/0600/1200/1450/2460), the saved states
-- (title 1500, name_entry 2250, gameplay_first 2352, area1_enter 2450) and area 0 at 2385
local CAPTURE = {120, 600, 1200, 1450, 1500, 2250, 2352, 2385, 2392, 2409, 2450, 2460}

local emu_ = emu or FCEU
local BUTTON = {A = "A", B = "B", SELECT = "select", START = "start",
                UP = "up", DOWN = "down", LEFT = "left", RIGHT = "right"}

local function parse_keys(spec)
  local held = {}
  for tok in string.gmatch(spec, "%S+") do
    local f, btns, len = string.match(tok, "^(%d+):([%u%+]+):?(%d*)$")
    f, len = tonumber(f), tonumber(len) or 2
    for b in string.gmatch(btns, "[^+]+") do
      for i = f, f + len - 1 do
        held[i] = held[i] or {}
        held[i][BUTTON[b]] = true
      end
    end
  end
  return held
end

local function write_file(path, data)
  local fh = assert(io.open(path, "wb"))
  fh:write(data)
  fh:close()
end

local function cpu_range(addr, len)
  if memory.readbyterange then
    return memory.readbyterange(addr, len)
  end
  local t = {}
  for i = 0, len - 1 do t[#t + 1] = string.char(memory.readbyte(addr + i)) end
  return table.concat(t)
end

local function ppu_range(addr, len)
  if ppu and ppu.readbyterange then
    return ppu.readbyterange(addr, len)
  end
  local t = {}
  for i = 0, len - 1 do t[#t + 1] = string.char(ppu.readbyte(addr + i) % 256) end
  return table.concat(t)
end

local function capture(n, log)
  local base = string.format("%s/frame_%04d", OUTDIR, n)
  local files = {"ram", "wram"}
  write_file(base .. ".ram", cpu_range(0x0000, 0x800))
  write_file(base .. ".wram", cpu_range(0x6000, 0x2000))
  if ppu and (ppu.readbyte or ppu.readbyterange) then
    write_file(base .. ".nt", ppu_range(0x2000, 0x1000))
    write_file(base .. ".pal", ppu_range(0x3F00, 0x20))
    files[#files + 1] = "nt"
    files[#files + 1] = "pal"
  end
  if gui.savescreenshotas then
    gui.savescreenshotas(base .. ".png")
    files[#files + 1] = "png"
  elseif gui.gdscreenshot then
    write_file(base .. ".gd", gui.gdscreenshot())
    files[#files + 1] = "gd"
  end
  local fc = emu_.framecount and emu_.framecount() or (movie and movie.framecount()) or -1
  log:write(string.format("%d %d %s\n", n, fc, table.concat(files, ",")))
  log:flush()
end

os.execute('mkdir -p "' .. OUTDIR .. '"')
local log = assert(io.open(OUTDIR .. "/capture.txt", "w"))
log:write("# frame fceux_framecount files  (keys: " .. KEYS .. ")\n")

local held = parse_keys(KEYS)
local want, last = {}, 0
for _, n in ipairs(CAPTURE) do want[n] = true; if n > last then last = n end end

if emu_.speedmode then emu_.speedmode("maximum") end
emu_.poweron()
for n = 0, last do
  joypad.set(1, held[n] or {})
  emu_.frameadvance()
  if want[n] then capture(n, log) end
end
log:close()
print("re-c capture done: " .. OUTDIR)
if emu_.exit then emu_.exit() end
