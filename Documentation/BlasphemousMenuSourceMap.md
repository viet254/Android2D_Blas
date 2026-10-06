# Blasphemous menu source map

The interactive menu reconstruction in `Assets/Brotherhood/Runtime/MainMenuController.cs` is traced to the recovered game data below. Full-screen reference screenshots are not used as UI layers.

## Title screen

- Scene: `D:/game/Bla_mobile_map/ExportedProject/Assets/#Design/Scenes/UI/Landing.unity`
- Logo: `Texture2D/PressAnybutton.png` with `Sprite/PressAnybutton.asset`
- Prompt bar: `Texture2D/inventory-spritesheet.png`, sprite rectangle `(5, 31, 214, 24)` from `inventory-spritesheet_123.asset`
- Loading animation: `Texture2D/LoadingSpinningIco.png`, 25 frames at 0.06 second intervals from `LoadingIcon.anim`
- Animation data: `NewMainMenu_init.anim`, `NewMainMenu_fadeout.anim`, and `PressAnyButton_text.anim`
- Behaviour: PC and IL2CPP `Gameplay.UI.Others.MenuLogic/Landing.cs`

## Mode and screen select

- Layout: `D:/game/Bla_mobile_map/ExportedProject/Assets/#Design/Scenes/UI/GenericElements.unity`, object `UI_MAINMENU/Menu`
- Background: `Texture2D/MainMenuBackground.png`, sprite rectangle `(0, 664, 640, 360)`
- Animated Penitent: `Texture2D/MainMenuPenitent-anim-spritesheet.png`, 22 source sprites and timings from `MainMenuPenitentIdle.anim`
- Penitent composition: native 517–520 by 360 px frames are aligned to the left edge of the 640x360 background (`x=0`); they are not centred inside a 640 px image
- Normal button: `Texture2D/Boton_03.png`
- Selected button: `Texture2D/button_background_selected.png`
- Selection marker: `Texture2D/marker.png`
- Behaviour: PC and IL2CPP `Gameplay.UI.Others.MenuLogic/NewMainMenu.cs`

## Load and save

- Layout: `GenericElements.unity`, object `UI_MAINMENU/UI_SLOT`
- Header: `Texture2D/Canvas_02.png`
- Slot frames: `Slot_02_empty.png`, `Slot_02.png`, `Slot_03.png`, `Slot_04.png`, and `Slot_05.png`
- Slot lines: `LineasSlot_01.png` and `LineasSlot_02.png`
- Row delete button: `Texture2D/Boton_06.png`
- Behaviour: PC and IL2CPP `Gameplay.UI.Others.MenuLogic/SelectSaveSlots.cs` and `Gameplay.UI.Widgets/SaveSlot.cs`

The recovered Continue/Back centres are too close for two 150 px mobile hit areas. Their shared source baseline is retained, while the centres are separated to `x=340` and `x=510` (20 px clear space between hit areas).

## Options

- Layout: `GenericElements.unity`, object `UI_OPTIONS` and its `Options_Main`, `Options_Game`, `Options_Accessibility`, `Options_Video`, `Options_Audio`, and `Options_TouchRemap` children
- Header: `Texture2D/Canvas_02.png`
- Side ornament: `Texture2D/Retorcido_01.png`
- Main option row: `Texture2D/Boton_04.png`, sliced with the recovered `(11, 15, 11, 11)` border
- Setting marker: rectangle `(4, 475, 10, 18)` from `Texture2D/menu-options-spritesheet.png`
- Setting arrows and divider: `Texture2D/Flecha_01.png` and `Texture2D/Linea_02.png`
- Exact normal/highlight colours: `(1, .73333335, .49411765, 1)` and `(1, .9529412, .6392157, 1)`
- Behaviour and option groups: IL2CPP `Gameplay.UI.Others.MenuLogic/OptionsWidget.cs`; volume steps use its recovered 0–100 range and 10-point step

The main Options page hides the runtime-only Resume/Exit entries, matching `OptionsWidget.OnShow(optionsIsInitial)` when opened from the title menu. The project adapter persists the applicable audio, accessibility, video, and touch settings through `PlayerPrefs`; master volume, VSync, frame-rate, control size, joystick mode, and haptics are connected to runtime behaviour.

The original scripts depend on FMOD, Rewired, Framework.Managers, and the original save system, so their state transitions and UI behaviour are adapted to the project's Input System, JSON save files, and `Brotherhood` scene instead of copying uncompilable classes into the project.
