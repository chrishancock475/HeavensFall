"""Generate the ASCII map for the combat + map prototype slice.

Writes Assets/Levels/Proto/proto_slice_map.txt and proto_slice_rooms.txt.
The Unity menu "Heaven's Fall/Build Prototype Slice" turns these into Proto_Slice.unity.

Coordinates: x to the right, y up, 1 cell = 1 Unity unit. (0,0) is the bottom-left cell.
Run:  python -I tools/gen_proto_map.py   (prints a preview)

Legend
  #  steel (magnetic, layer Ground)        =  aluminium (not magnetic)
  ^  spike (1 damage, pogo-able)           .  empty
  F  energy fence (only John's dash passes) D  arena entrance door (open until the fight starts)
  E  arena exit door (closed until the Warden falls)
  H  one-way hatch (opens when touched from above)
  P  player start   S save point   t trooper   d drone   x training dummy
  W  Warden (mini-boss)   J John (joins the party)   C Mag Coil (time-slow meter +50%)
"""
import os

W, H = 176, 70
g = [['#'] * W for _ in range(H)]


def put(x, y, c):
    if 0 <= x < W and 0 <= y < H:
        g[y][x] = c


def rect(x0, y0, x1, y1, c):
    """Fill inclusive rectangle."""
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(x, y, c)


rooms = []  # (id, name, x0, y0, x1, y1, flags, hint)


def room(rid, name, x0, y0, x1, y1, hint, flags=""):
    rect(x0, y0, x1, y1, '.')
    rooms.append((rid, name, x0, y0, x1, y1, flags, hint))


# ---------------------------------------------------------------- rooms (interiors)
room("R1", "Landing", 2, 2, 33, 17,
     "U attack (W+U launcher, air S+U pogo)  J heavy  I finisher  Shift time slow  Space jump  M super jump. Hit the dummy.")
room("R2", "Beam Hall", 36, 2, 85, 21,
     "Troopers wind up before they lunge. The fence above only lets a dash through.")
room("R3", "Shaft", 88, 2, 107, 49,
     "Run up the red walls. Blue panels are not magnetic: jump to the other wall. At the top, hold toward the ledge.")
room("R4", "Camp", 110, 40, 139, 53,
     "Talk to John. O: tap to swap, hold to call an assist. John: Shift = 8-way air dash.")
room("R5", "Warden Arena", 142, 40, 173, 57,
     "The Warden: sweep, leap slam (armoured), summon. Dash through the slam for a just dodge.", "arena")
room("R6", "Ledge Cache", 36, 24, 60, 35,
     "Past the fence. Pogo the drone, then claim the Mag Coil.")
room("R7", "Return Corridor", 20, 60, 173, 67,
     "Ceiling-run over the spikes, or pogo off them.")
room("R8", "Shortcut", 8, 20, 15, 65,
     "Drop down. The hatch only opens from this side.")

# ---------------------------------------------------------------- connections
rect(34, 2, 35, 6, '.')      # R1 -> R2 (floor level, 5 tall)
rect(86, 2, 87, 6, '.')      # R2 -> R3
rect(108, 44, 109, 48, '.')  # R3 top -> R4 (4 above R4 floor)
rect(108, 40, 109, 43, '#')  # step under that opening (keeps it 4 above R4 floor)
rect(140, 40, 141, 44, 'D')  # R4 -> R5, closes during the fight
rect(142, 58, 147, 59, 'E')  # R5 ceiling -> R7, opens after the Warden falls
rect(16, 60, 19, 64, '.')    # R7 left end -> R8
rect(8, 18, 15, 19, 'H')     # R8 bottom -> R1 ceiling, one-way hatch
rect(44, 22, 49, 23, 'F')    # R2 ceiling -> R6, energy fence

# ---------------------------------------------------------------- R1 Landing
put(5, 2, 'S')
put(8, 2, 'P')
rect(16, 7, 22, 7, '#')      # practice platform (hang under it as Rayne)
put(27, 2, 'x')              # training dummy
rect(31, 2, 33, 9, '#')      # right wall step up to practise wall runs (exit stays open at 34-35)
rect(31, 2, 33, 6, '.')      # keep the exit passage clear

# ---------------------------------------------------------------- R2 Beam Hall
rect(56, 0, 63, 1, '#')
rect(56, 2, 63, 2, '^')      # spike pit floor (pogo-able)
rect(50, 7, 54, 7, '#')      # beam
rect(65, 7, 70, 7, '#')      # beam
rect(52, 20, 67, 21, '=')    # aluminium ceiling over the pit: no ceiling-run shortcut
rect(38, 6, 42, 6, '#')      # beam staircase for John (jump ~2 + dash ~5 per step)
rect(44, 11, 49, 11, '#')
rect(38, 16, 42, 16, '#')    # top step: jump + diagonal dash up-right through the fence
put(42, 2, 't')
put(76, 2, 't')

# ---------------------------------------------------------------- R3 Shaft
# climbable part is 10 wide (x 88-97); x 98-107 is solid up to y 43, the top strip y 44-49 is full width
rect(98, 2, 107, 43, '#')
rect(98, 20, 99, 27, '=')    # aluminium on the right wall face: jump to the left wall
rect(86, 32, 87, 39, '=')    # aluminium on the left wall face: jump back to the right wall
rect(91, 24, 93, 24, '#')    # small rest ledge
put(93, 16, 'd')
put(93, 36, 'd')

# ---------------------------------------------------------------- R4 Camp
put(114, 40, 'S')
put(127, 40, 'J')
put(134, 40, 'x')

# ---------------------------------------------------------------- R5 Warden Arena
rect(147, 46, 151, 46, '#')
rect(164, 46, 168, 46, '#')
put(166, 40, 'W')
# the left wall (x 140-141) runs on up beside the exit E, so Rayne can wall-run up into R7

# ---------------------------------------------------------------- R6 Ledge Cache
rect(36, 24, 43, 25, '#')    # raised floor left of the fence opening
rect(50, 24, 54, 24, '^')    # spikes to pogo over
put(46, 30, 'd')
put(57, 24, 'C')

# ---------------------------------------------------------------- R7 Return Corridor
rect(120, 58, 135, 59, '#')
rect(120, 60, 135, 60, '^')  # spike floor, steel ceiling above: ceiling-run or pogo
put(127, 64, 'd')
rect(80, 66, 95, 67, '=')    # aluminium ceiling
rect(84, 60, 91, 60, '^')    # spikes under it: pogo or dash
put(70, 64, 'd')
put(108, 60, 't')
put(55, 60, 't')
rect(30, 60, 33, 61, '#')    # low step

# ---------------------------------------------------------------- write
here = os.path.dirname(os.path.abspath(__file__))
out = os.path.join(here, "..", "Assets", "Levels", "Proto")
os.makedirs(out, exist_ok=True)
lines = ["".join(row) for row in reversed(g)]  # top row first
with open(os.path.join(out, "proto_slice_map.txt"), "w", newline="\n") as f:
    f.write("\n".join(lines) + "\n")
with open(os.path.join(out, "proto_slice_rooms.txt"), "w", newline="\n") as f:
    f.write("# id|name|x0|y0|x1|y1|flags|hint   (interior cells, inclusive, y up)\n")
    for r in rooms:
        f.write("|".join(str(v) for v in r) + "\n")

if __name__ == "__main__":
    for line in lines:
        print(line)
