# Heaven's Fall

Unity 2022.3.19f1 2D game. The spring 2024 PantherDev demo (Rayne, mag-switch platformer) is being turned
into the **prologue** of a full **metroidvania** that merges ideas from both original design docs.

Read first: `docs/SOP.txt` (workflow and repo rules), `docs/session.txt` (current goal), `docs/scope.md`
(combined design + what is built).

Key code
- `Assets/Scripts/PlayerMovement.cs`: running, jumping, mag-switch surface walking (`surface`, `MagSwitch`),
  super jump (gated by `GameManager.LEVEL > 1`), Shift = time slow (timeScale 0.5), `Die()`.
- `Assets/Scripts/GameManager.cs`: static checkpoint, level-end event, Cinemachine blend override.
- Scenes in build: Main_menu, Cutscene_1-3, Level_1, Level_2, End screen, Proto_Slice.
- Branches: origin/test-2 is the final Apr 2024 demo; GitHub NEW-MAIN is stale (broken Level_1 merge).
  Work happens on proto/combat-slice, which merges test-2.
- Checks: Unity batch smoke tests, see docs/SOP.txt.

Rules
- Do not push to GitHub or touch remote branches without the owner's OK.
- Do not move or rename assets outside Unity (GUIDs live in .meta files).
- Folder is `Assets/Scripts` on disk/git; its meta is `Assets/scripts.meta` (case mismatch, harmless on Windows).
