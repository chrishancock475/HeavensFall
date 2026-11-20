# Heaven's Fall - scope

Status: **DRAFT**. Part 2 is a proposal for the owner to edit. Nothing in it is decided until the open
questions in Part 3 are answered.

Sources
- Platformer doc, 12/13/2023: `docs/design_platformer_2023-12-13.txt` (PDF: `Heaven's fall final Doc.pdf`)
- RPG doc, 5/29/2023: `docs/design_rpg_2023-05-29.txt` (PDF: `Let the heavens fall Design Doc.pdf`)
- Code: repo branch NEW-MAIN @ 8fb315e

Structure (owner decision, 2026-11-19): the spring 2024 demo (Rayne, mag-switch tower climb) is the
**prologue**. The full game is a **metroidvania platformer** that combines gameplay from both docs.

---

## Part 1 - Prologue inventory (platformer doc vs NEW-MAIN)

Doc tags: MVP = green (core), EXTRA = red (nice to have), POLISH = yellow. The PDF highlights were lost in
text extraction, so the tags below are inferred from the milestone list and must be confirmed.

| Feature | Doc | Status | Notes |
|---|---|---|---|
| Run on floor / walls / ceiling (mag-switch) | MVP | Built | `PlayerMovement.cs`, `surface` bitmask + `MagSwitch` |
| Momentum carry jump <-> run | MVP | Built | `conserveMomentum`, wall jump speed |
| Variable jump (hold = low gravity) | MVP | Built | `lowGravMultiplier` |
| Aerial drift with speed cap | MVP | Built | `airMultiplier` |
| Coyote time, jump buffer | POLISH | Built | not in doc, already present |
| Super jump | - | Built | not in doc; gated by `GameManager.LEVEL > 1` |
| Time slow on Shift | - | Built | not in platformer doc; matches Paul's ability in RPG doc |
| Steel (red, magnetic) / aluminium (blue) blocks | MVP | Partial | tile palettes exist; colour language to verify in scenes |
| Spikes, one-hit death | MVP | Built | `Spike.prefab`, `KillPlayer.cs`, used in Level_1/2 |
| Respawn at room start / checkpoints | MVP | Built | `Checkpoint.cs`, `GameManager.checkpoint` |
| Room camera (fixed or follow) | MVP | Built | Cinemachine + `CameraSelect.cs` |
| 5 rooms over 3 stages | MVP | Partial | Level_1, Level_2 built (construction yard?). Garden and control room missing |
| Main menu, credits, instructions | MVP | Built | `UIButtons.cs` |
| Pause menu (dimmed, resume / options / exit) | MVP | Partial | pause + exit built; options missing |
| Options: ability descriptions + key rebinding | EXTRA | Missing | |
| Intro text crawl | MVP | Partial? | Cutscene_1 is a video, auto-advances at 38 s |
| Cutscene captions (black = Rayne, red = Voice) | MVP | Missing | |
| Cutscene skip on Esc, max 30 s | POLISH | Missing | Cutscene_1 is 38 s with no skip |
| Cutscene_2 | MVP | Partial | scene exists, not in build list |
| Ending: touch crystal, fade, "let the heavens fall" | MVP | Missing | |
| Speedrun timer / 10 min demo limit | EXTRA | Missing | |
| Music: title, cutscene, stage theme | MVP | Built | `Assets/Music` |
| SFX: run, jump, mag-switch, landing per area, death | MVP | Missing | no SFX found |
| Animations | MVP | Partial | jump animation added; mag-switch animation not checked |

---

## Part 2 - Full game: combined metroidvania (PROPOSAL)

### Pillars
1. **Movement is the main verb** (from the platformer doc): mag-switch surface running, momentum and fast
   traversal stay the core feel.
2. **Combat is skill, not stats** (from the RPG doc): no RNG, every enemy attack can be dodged or blocked,
   and real-time combos borrow fighting-game structure.
3. **Short and respectful of the player's time** (from the RPG doc): full heal after encounters or at save
   points, the only HP upgrades come from mandatory bosses, optional loot is power, not a requirement.

### Taken from the platformer doc
- Mag-switch, steel/aluminium colour language (red = magnetic, blue = not), spikes, rooms with one exit
  become rooms in an interconnected map.
- The crashed colony ship / tower as a setting, with the construction yard, garden and control room as
  biomes.
- Minimal HUD like Celeste during traversal; cutscene captions and the pitch-shift freeze.
- Speedrun timer (fits the metroidvania speedrun culture).

### Taken from the RPG doc
- **Abilities that gate areas** (the metroidvania key-and-lock):
  - Time slow (Paul) - already in code on Shift.
  - Air dash / 8-way "skip" (John) - extends combos and crosses gaps.
  - Double jump (all characters in the RPG doc).
  - Super jump - already in code, already level-gated.
  - Later mag-switch upgrades (e.g. polarity flip, magnetise aluminium) as new locks.
- **Combat:** light/medium/heavy normals (A/B/C), standing/crouching/air variants, specials with motion
  inputs (236, 214, 623), high/mid/low with stand and crouch block, a combo finisher gauge that fills per
  hit, knockdown on 2B.
- **Progression:** HP rises only from story bosses; weapons and equipment found by exploring change the
  play style (e.g. single sword vs dual swords); auto-combos as an accessibility unlock.
- **Setting and story:** the Exodus ships, the AI Leviticus and its Levites, the Second Law knights, the
  habitat control room with the wireframe ship map; boss pair Cain (then Cain's mech).
- **Map ideas:** a habitat that wraps left to right (non-Euclidean loop) and one large landmark visible
  from most of each area.

### Dropped or changed from the RPG doc
- Turn-based battle mode, overworld grid movement, fleeing to restore items, round-based two-win fights.
- The 3D environment (the game stays 2D pixel art).

### Story bridge (prologue -> main game)
The prologue ends with Rayne touching the crystal and "let the heavens fall". Proposal: the crystal is the
device holding the backup from the RPG doc, so the main game starts from its consequences (Leviticus wakes
up and the Levites march). How Rayne relates to Paul and John is open (Part 3).

---

## Part 3 - Decisions and open questions

### Decided (owner, 2026-11-19)
1. **Party:** Rayne plus a party. The RPG doc's party members (Paul / John / Georgia / Ringo) join;
   switching and assists come from the RPG doc's assist system. Who joins when is still open.
2. **Combat:** a **simple input set** (no motion-input specials, no full high/mid/low tree) whose depth
   comes from **advanced combat and movement techniques**: things skilled players discover, such as
   cancels, momentum carried into attacks, mag-switch surface attacks, dash and jump cancels, and assist
   or character-switch extensions.
3. **Next milestone:** a **combat + map prototype slice** (not finishing the prologue first).

### Still open
4. Does the prologue stay a linear set of rooms (as built) or become the first area of the connected map?
5. Do mag-switch tiles keep red and blue literally, or should the colour language be redone in the new art?
6. Which party member is in the prototype slice besides Rayne, and what is their gating ability?
