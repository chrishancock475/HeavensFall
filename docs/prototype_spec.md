# Prototype spec - combat + map slice

Status: **DRAFT, waiting for owner approval.** No code until approved.
Branch: `proto/combat-slice` off `NEW-MAIN`. Unity 2022.3.19f1.
Decisions this builds on: `docs/scope.md` Part 3 (Rayne + party, simple inputs with deep tech, John joins,
all rooms in one scene).

## 1. Goal of the slice
Prove three things in about 10 minutes of play:
1. Combat with a small input set feels deep because of the techniques it allows (section 4).
2. Mag-switch movement and combat work together (attacking on walls and ceilings, momentum into hits).
3. A connected map with one ability lock, a backtrack and a shortcut works in a single scene.

## 2. Party
| | Rayne | John |
|---|---|---|
| Role | fast, technical, surfaces | heavier hits, air control |
| Mag-switch | yes (existing) | **no** (no mag boots, falls off steel) |
| Movement ability | time slow (Shift, meter) | **air dash**, 8 directions, 1 per airtime |
| Attack string | 3-hit knife string, short range | 2-hit rifle-butt string, longer range, more knockback |
| Special | **Mag Pulse**: short-range push; on steel it launches Rayne off the wall | **Bayonet Lunge**: forward lunge that passes through enemies |
| Assist (partner called in) | Mag Pulse at the target | Bayonet Lunge at the target |

- John has no mag-switch, so swapping is a real choice: Rayne for routes along surfaces, John for gaps and
  air.
- **One shared HP pool** for the party (simpler UI, and swapping is never a way to escape damage).
- Swap happens instantly in place: the same Rigidbody, a different character profile (tuning, moveset,
  sprite).

## 3. Inputs (keyboard; controller mapped the same way)
| Action | Key | Notes |
|---|---|---|
| Move | WASD / arrows | existing axes |
| Jump | Space | existing |
| Super jump | M | existing; in the slice gated by an ability flag instead of `GameManager.LEVEL` |
| Attack | J | direction plus Attack: neutral, up (launcher), down in the air (pogo) |
| Special | K | per character |
| Dash | L | John only (Rayne: no effect, or a short ground roll if testing wants it) |
| Swap / Assist | Q | tap = swap; hold 0.25 s = call the partner in for one assist attack (cooldown) |
| Time slow | Left Shift | Rayne only; drains a meter, hits refill it |
| Pause | Esc | existing |

No motion inputs and no block button. Defence comes from dash i-frames (John) and mag-switch repositioning
(Rayne). Every enemy attack is telegraphed, and none use randomness (RPG doc pillar).

## 4. Advanced techniques (the depth)
Target for the slice: **at least 4 working**, in priority order. Each one gets a dedicated test room or
spot in the map.
1. **Hit cancel:** on hit, the attack's recovery can be cancelled into jump, dash, special or swap.
2. **Pogo reset:** a down attack in the air that hits an enemy or hazard bounces you up and refunds air dash
   and jump.
3. **Surface attacks:** attacks made while mag-switched are rotated to the surface (on the ceiling,
   "down" points into the room).
4. **Momentum hits:** damage and knockback scale with speed at the moment of the swing (capped). This
   rewards the existing momentum system.
5. **Tag combo:** swapping while an enemy is in hitstun keeps the combo counter and the hitstun; the
   incoming character appears at the outgoing one's position.
6. **Dash snap:** John's dash into steel, then a swap to Rayne within 0.15 s, snaps her onto the surface
   keeping the dash speed as running speed.
7. **Just dodge:** dashing through an attack in its last 6 frames before contact gives a free 1 s of time
   slow.
8. **Launcher into air combo:** an up attack launches light enemies; you follow with a jump cancel.

## 5. Progression in the slice
- **Combo gauge** (from the RPG doc's finisher gauge): fills per hit, more from many-hit strings. In the
  slice it only refills time slow and the assist cooldown. A finisher move is a stretch goal.
- **HP:** starts at 5. The mini-boss drops **+1 HP** (the only HP source, per the RPG doc).
- **Save points:** full heal, set respawn. Death respawns at the last save point with full HP.
- **Spikes:** 1 damage and you are put back at the last safe ground in the room (not a one-hit kill in
  the full game; the prologue keeps one-hit kills).

## 6. Enemies (all deterministic)
| Enemy | Behaviour | Teaches |
|---|---|---|
| Levite Trooper | ground patrol, 0.6 s wind-up, then a lunge slash; light, can be launched | hit cancel, launcher |
| Levite Drone | flies a fixed path, telegraphs then fires a slow shot | pogo, surface attacks, dodging |
| Mini-boss: Levite Warden | 3 attacks in a fixed cycle (sweep, leap slam, drone summon); armoured during the slam | tag combo, just dodge |

## 7. Map (single scene `Assets/Levels/Proto/Proto_Slice.unity`)
Each room is a child GameObject `Room_XX` with a trigger that turns on its Cinemachine virtual camera,
the same pattern as `CameraSelect.cs`, plus a CinemachineConfiner2D on the room bounds. Tiles are steel
(red, magnetic) and aluminium (blue, not magnetic) using the existing palettes.

```
R1 Landing [SAVE] --- R2 Beam Hall ---- R3 Shaft (vertical, steel) --- R4 Camp (John joins)
   |                      |  (air-dash gap, visible early)                   |
   |                  R6 Ledge Cache                                   R5 Warden Arena -> +1 HP
   |                      |                                                  |
   +-- R8 Shortcut <-- R7 Return Corridor <----------------------------------+
         (one-way door, opens from the R7 side)
```
- **Lock:** the gap in R2 can't be crossed until John joins in R4 (air dash).
- **Backtrack:** after the boss, R7 leads back; R2's gap now opens R6 (combo-gauge test enemies and a
  stretch reward).
- **Shortcut:** R8's door opens from the far side and links back to R1's save point.
- Room count: 8.

## 8. Code plan
New code goes in `Assets/Scripts/Prototype/`. Existing scripts are only touched for small hooks.

Hooks in existing scripts:
- `PlayerMovement.cs`: expose `Facing`, `IsGrounded`, `Surface` (read only); add `bool inputLocked` and
  `bool magSwitchEnabled` (default true so the prologue is unchanged); move the Shift time slow behind a
  flag that defaults to the current behaviour.
- `PlayerMovement.cs` / `PauseMenu.cs`: all `Time.timeScale` writes go through one `TimeController` so
  pause and time slow stop fighting (this fixes the existing bug in the prologue too).

New scripts:
- `CharacterProfile` (ScriptableObject): tuning overrides, moveset, sprite/animator, abilities.
- `PartyController`: active character, swap, assist call, shared `Health`.
- `CombatController`: reads Attack / Special / Dash, runs `AttackData` (startup / active / recovery
  frames, cancel windows, hitbox shape, damage, knockback, momentum scaling).
- `Hitbox` / `Hurtbox` / `Health`: new layers `PlayerAttack` and `Enemy`.
- `ComboTracker`: hit count, gauge, hitstun bookkeeping for tag combos.
- `AbilityState`: unlock flags (air dash, super jump, party members) saved for the session.
- `EnemyBase` plus `TrooperAI`, `DroneAI`, `WardenBoss`: frame-timed state machines, no RNG.
- `SavePoint`, `HpUpgrade`, `AbilityGate`, `OneWayDoor`.
- `DebugHud`: HP, gauge, combo counter, active character, current tech triggered (for tuning).

Art: placeholders. John uses Rayne's sprite tinted until real art exists; hitboxes are drawn as gizmos.

## 9. Acceptance (done when)
1. `Proto_Slice` plays from R1 through the boss, back through R7 and R8 to R1 with no console errors.
2. Swap and assist work; John can't mag-switch and Rayne can.
3. At least 4 of the section 4 techniques work and are shown on the DebugHud when they happen.
4. Trooper, Drone and Warden all work; the Warden gives +1 HP; save points heal and respawn.
5. The air-dash lock in R2 can't be passed before John joins and can be passed after.
6. The prologue (Main_menu -> Level_2) still plays the same; holding Shift while paused no longer
   un-pauses.
7. Play-tested by the owner (or Grok) with a short notes or log file in `docs/`.

## 10. Out of scope for the slice
Real art and animation, sound effects, cutscenes, the other party members, save files on disk,
controller rebinding, the finisher move (stretch only).

## 11. Questions for the owner (can be answered while building)
1. Is "John has no mag-switch" right? It is the main reason to swap.
2. Shared HP pool for the party, or separate HP per character?
3. Should spikes stay one-hit in the main game, or deal damage and return you to safe ground (as above)?
4. Are J / K / L / Q fine, or do you want the RPG doc's U / J / I layout?
