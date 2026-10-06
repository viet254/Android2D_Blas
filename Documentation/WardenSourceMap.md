# Warden of the Silent Sorrow — source restoration map

This implementation uses the extracted Blasphemous data already supplied with the project. No new boss, UI, icon, or animation artwork was drawn.

## Original sources used

- `D:/game/Blasphemous_PC_Source/Assembly-CSharp/ElderBrotherIntroDummy.cs`
  - two `BigSmashPreparation → Smash → ShakeWave` beats;
  - waits: `1.0`, `0.3`, `1.0`, `1.0`, `0.3`, `1.6`, jump rise `0.8`, then `0.1` seconds.
- `D:/game/Blasphemous_PC_Source/Assembly-CSharp/Gameplay.GameControllers.Bosses.ElderBrother/ElderBrotherBehaviour.cs`
  - only two attacks: `AREA` and `JUMP`;
  - repeated attacks disabled;
  - intro jump preparation `0.65` seconds;
  - predicted player position for `JUMP`.
- `D:/game/Bla_mobile_map/ExportedProject/Assets/#Design/#CVSTODIA/D17 - Brotherhood of the Penitent One/D17Z01 - Brotherhood/D17Z01S11 - Brotherhood/D17Z01S11_LOGIC.unity`
  - combat boundaries: `4 × 32`;
  - `AREA`: 6 areas, initial offset `3.7`, spacing `1.5`, duration `1.4`;
  - area/mace damage: `12`;
  - boss-dead flag flow and boundary removal;
  - Warden purge reward: `0`.
- `D:/game/Bla_mobile_map/ExportedProject/Assets/Resources/achievements/BASE_GAME_ACHIEVEMENTS.asset` and `I2Languages.asset`
  - achievement `AC01`;
  - English display name: `A Long Path Ahead`;
  - English description: `Defeat the Warden of Silent Sorrow.`
- `D:/game/Bla_mobile_map/ExportedProject/Assets/Texture2D/achievements-menu.png`
  - copied byte-for-byte into project Resources.
- `D:/game/Bla_mobile_map/ExportedProject/Assets/Sprite/achievements-AC01.asset`
  and `achievements-bg-unlocked.asset`
  - copied byte-for-byte and used by the runtime achievement notification.

## Restored project behavior

- Entering `D17Z01S11` leaves the source left boundary open. Crossing the original `BossFight` 4×10 trigger then blocks input, closes both combat boundaries, and runs the original two-smash dummy intro.
- The real Warden performs the source intro jump to `IntroJumpPoint`.
- Boss name/health and boss music activate only when combat starts.
- Warden alternates `AREA` and `JUMP`; there is no invented low-health speed phase.
- Death plays `ElderBrother_Death`, resolves to `ElderBrother_Corpse`, ends boss music, removes boundaries, grants `AC01`, and saves `wardenDefeated`/`achievementAC01`.
- A loaded save with the boss-dead flag skips the intro and keeps the route open.

## Assignment progression and victory UI

- The restored playable chapter uses defeat of the source Warden as its explicit win objective. This is **not** the ending of the full original Blasphemous story; the original post-boss door and Deogracias continuation remain playable.
- The victory title comes from `D:/game/Bla_mobile_map/ExportedProject/Assets/Texture2D/boss-defeated-screen-title.png`, copied into `Assets/Brotherhood/Resources/Menu/BossDefeatedTitle.png`. The button background is the existing source-derived `Menu/ButtonBase`, and AC01 uses the source achievement sprites and localization above. No new artwork was drawn.
- Victory waits for a choice: Continue returns to gameplay, Home opens the existing main menu, and Achievements opens a separate AC01 panel with Back navigation. The source `ELDER_BROTHER_DEATH` sample plays on the kill.
- The project saves the current room, player position, Life, Fervour, flasks, completed-room IDs, and chapter-win flag after each original door transition. This automatic per-room save is an assignment-specific addition; the original Prie Dieu checkpoint remains the death-respawn location.

## Source post-boss continuation

- The Warden room's original east door leads to `D01Z01S07 - Forest`, whose source scene contains `Deosgracias`.
- The encounter follows the original PlayMaker chain exactly: `DLG_0101 → CTS07-Deosgracias → DLG_0112 → DLG_0113 → DLG_0115 → DLG_0114 → QI31`.
- `deosgracias_idle_anim`, `deosgracias_stand_anim`, `deosgracias_back_anim`, `CTS07.m4v`, the dialogue panel, the QI31 icon, and every English dialogue/subtitle line are loaded from the supplied mobile export.
- Completing the conversation grants the original `QI31` Thorn and persists `deograciasMet`, `thornGranted`, and ownership in the save file.

## Deliberately not added

The supplied prose is a creative interpretation. These details were not found in the original D17 Warden sources and therefore were not fabricated:

- mask cracking;
- a special slow-motion sword thrust/finisher animation;
- the Warden dissolving into ash;
- a faster second phase;
- Tears awarded for this boss (the source value is `0`);

The prose phrase `A Long Road Ahead` was corrected to the original English localization: `A Long Path Ahead`.
