using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Brotherhood
{
    /// <summary>
    /// Interactive reconstruction of the supplied menu flow.  Visual layers,
    /// sprite frame rectangles and timing come from the extracted Blasphemous
    /// scene, animation clips and SaveSlot prefab.
    /// </summary>
    public sealed class MainMenuController : MonoBehaviour
    {
        enum Page
        {
            Title,
            ModeSelect,
            SaveSlots,
            Options,
            OptionsGame,
            OptionsAccessibility,
            OptionsVideo,
            OptionsAudio,
            OptionsTouch,
            OptionsTutorial,
            Extras,
            DeleteConfirm
        }

        // These states and timings mirror Landing.cs and NewMainMenu.cs from
        // the recovered PC/IL2CPP sources.  The original dependencies
        // (Rewired, FMOD and the game framework) are replaced by this
        // project's input, audio and save services.
        enum LandingState { FadeIn, Press, FadeOut, Nothing }

        struct SourceFrame
        {
            public readonly Rect rect;
            public readonly Vector2 pivot;
            public SourceFrame(float x, float y, float width, float height, float pivotX, float pivotY)
            {
                rect = new Rect(x, y, width, height);
                pivot = new Vector2(pivotX, pivotY);
            }
        }

        sealed class AnimatedLayer
        {
            readonly Image image;
            readonly Sprite[] frames;
            readonly float[] keyTimes;
            readonly float duration;
            int frame = -1;

            public AnimatedLayer(Image target, Sprite[] sourceFrames, float[] sourceTimes, float sourceDuration)
            {
                image = target;
                frames = sourceFrames;
                keyTimes = sourceTimes;
                duration = sourceDuration;
            }

            public void Update(float elapsed)
            {
                if (image == null || frames.Length == 0) return;
                float time = Mathf.Repeat(elapsed, duration);
                int next = 0;
                while (next + 1 < keyTimes.Length && time >= keyTimes[next + 1]) next++;
                if (next == frame) return;
                frame = next;
                image.sprite = frames[Mathf.Min(next, frames.Length - 1)];
            }
        }

        [Serializable]
        sealed class SavePreview
        {
            public int version;
            public string room;
            public bool boss;
            public PlayerProgress progress;
        }

        const string SlotPreference = "BrotherhoodSaveSlot";
        const int SlotCount = 3;

        RectTransform safe;
        RectTransform page;
        CanvasScaler menuScaler;
        Font originalFont;
        Button firstButton;
        Page current;
        int selectedSlot = 1;
        float pageOpenedAt;
        readonly List<AnimatedLayer> animatedLayers = new List<AnimatedLayer>();
        readonly List<Image> menuSelectors = new List<Image>();
        readonly List<Image> menuAuxSelectors = new List<Image>();
        readonly List<Image> menuMarkers = new List<Image>();
        readonly List<Text> menuLabels = new List<Text>();
        readonly List<Button> menuButtons = new List<Button>();
        readonly List<UnityEngine.Events.UnityAction> menuLeftActions = new List<UnityEngine.Events.UnityAction>();
        readonly List<UnityEngine.Events.UnityAction> menuRightActions = new List<UnityEngine.Events.UnityAction>();
        readonly List<Color> menuNormalColors = new List<Color>();
        readonly List<Color> menuHighlightColors = new List<Color>();
        int menuSelection;
        LandingState landingState;
        float landingStateAt;
        CanvasGroup landingPrompt;
        Image landingPulse;
        Image landingLoadingIcon;

        void Awake()
        {
            if (FindAnyObjectByType<Camera>() == null)
            {
                var camera = new GameObject("Main menu camera", typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.black;
                camera.orthographic = true;
                camera.transform.position = new Vector3(0, 0, -10);
            }

            var inputModule = FindAnyObjectByType<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                var events = new GameObject("Menu input", typeof(EventSystem), typeof(InputSystemUIInputModule));
                inputModule = events.GetComponent<InputSystemUIInputModule>();
            }
            if (inputModule.actionsAsset == null) inputModule.AssignDefaultActions();
        }

        void Start()
        {
            Application.runInBackground = true;
            Application.targetFrameRate = PlayerPrefs.GetInt("BrotherhoodFrameRate", 60);
            QualitySettings.vSyncCount = PlayerPrefs.GetInt("BrotherhoodVSync", 0);
            AudioListener.volume = PlayerPrefs.GetFloat("BrotherhoodVolume", 1f);
            selectedSlot = Mathf.Clamp(PlayerPrefs.GetInt(SlotPreference, 1), 1, SlotCount);
            BuildCanvas();
            ApplyResolutionMode();
            Show(Page.Title);
        }

        void Update()
        {
            for (int i = 0; i < animatedLayers.Count; i++) animatedLayers[i].Update(Time.unscaledTime - pageOpenedAt);
            if (current == Page.Title)
            {
                UpdateLanding();
                return;
            }
            if (current != Page.Title && WasBackPressed())
            {
                if (current == Page.ModeSelect) Show(Page.Title);
                else if (current == Page.SaveSlots || current == Page.Options || current == Page.Extras) Show(Page.ModeSelect);
                else if (IsOptionsSubpage(current)) Show(Page.Options);
                else if (current == Page.DeleteConfirm) Show(Page.SaveSlots);
                return;
            }
            if (current == Page.SaveSlots) UpdateSaveSlotInput();
            else if (current != Page.Title) UpdateMenuInput();
        }

        void UpdateLanding()
        {
            float stateTime = Time.unscaledTime - landingStateAt;
            if (landingPulse != null)
            {
                // PressAnyButton_text.anim: 0.334 -> 0.741 -> 0.334 over
                // 2.857143 seconds, looping with smooth tangents.
                float pulseTime = Mathf.Repeat(Time.unscaledTime - pageOpenedAt, 2.857143f);
                float half = 1.4285715f;
                float t = pulseTime <= half ? pulseTime / half : (pulseTime - half) / half;
                float alpha = pulseTime <= half
                    ? Mathf.SmoothStep(.334f, .741f, t)
                    : Mathf.SmoothStep(.741f, .334f, t);
                Color color = landingPulse.color;
                color.a = alpha;
                landingPulse.color = color;
            }

            switch (landingState)
            {
                case LandingState.FadeIn:
                    if (landingPrompt != null) landingPrompt.alpha = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(stateTime / 1.1333333f));
                    if (stateTime >= 1f)
                    {
                        landingState = LandingState.Press;
                        landingStateAt = Time.unscaledTime;
                    }
                    break;
                case LandingState.Press:
                    if (landingPrompt != null) landingPrompt.alpha = 1f;
                    if (WasAnySubmitPressed()) BeginLandingFadeOut();
                    break;
                case LandingState.FadeOut:
                    if (landingPrompt != null) landingPrompt.alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(stateTime));
                    if (stateTime >= 1f)
                    {
                        landingState = LandingState.Nothing;
                        Show(Page.ModeSelect);
                    }
                    break;
            }
        }

        void BeginLandingFadeOut()
        {
            if (landingState != LandingState.Press) return;
            landingState = LandingState.FadeOut;
            landingStateAt = Time.unscaledTime;
            if (landingLoadingIcon != null) landingLoadingIcon.gameObject.SetActive(true);
        }

        bool WasAnySubmitPressed()
        {
            return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                || (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
                || (Gamepad.current != null && (Gamepad.current.buttonSouth.wasPressedThisFrame || Gamepad.current.startButton.wasPressedThisFrame));
        }

        bool WasBackPressed()
        {
            return (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
                || (Gamepad.current != null && Gamepad.current.buttonEast.wasPressedThisFrame);
        }

        void BuildCanvas()
        {
            var canvasObject = new GameObject("Restored menu canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            menuScaler = canvasObject.GetComponent<CanvasScaler>();
            menuScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Landing.unity and GenericElements.unity both use 640x360 and
            // match height. Keeping the source coordinate space avoids the
            // oversized/cropped menu art from the prior reconstruction.
            menuScaler.referenceResolution = new Vector2(640, 360);
            menuScaler.matchWidthOrHeight = 1f;
            safe = new GameObject("Safe area", typeof(RectTransform)).GetComponent<RectTransform>();
            safe.SetParent(canvasObject.transform, false);
            ApplySafeArea();
        }

        void Show(Page next)
        {
            if (page != null)
            {
                page.gameObject.SetActive(false);
                Destroy(page.gameObject);
            }

            current = next;
            firstButton = null;
            animatedLayers.Clear();
            menuSelectors.Clear();
            menuAuxSelectors.Clear();
            menuMarkers.Clear();
            menuLabels.Clear();
            menuButtons.Clear();
            menuLeftActions.Clear();
            menuRightActions.Clear();
            menuNormalColors.Clear();
            menuHighlightColors.Clear();
            menuSelection = 0;
            pageOpenedAt = Time.unscaledTime;
            page = new GameObject(next.ToString(), typeof(RectTransform)).GetComponent<RectTransform>();
            page.SetParent(safe, false);
            Stretch(page);

            switch (next)
            {
                case Page.Title: BuildTitle(); break;
                case Page.ModeSelect: BuildModeSelect(); break;
                case Page.SaveSlots: BuildSaveSlots(); break;
                case Page.Options: BuildOptions(); break;
                case Page.OptionsGame: BuildOptionsGame(); break;
                case Page.OptionsAccessibility: BuildOptionsAccessibility(); break;
                case Page.OptionsVideo: BuildOptionsVideo(); break;
                case Page.OptionsAudio: BuildOptionsAudio(); break;
                case Page.OptionsTouch: BuildOptionsTouch(); break;
                case Page.OptionsTutorial: BuildOptionsTutorial(); break;
                case Page.Extras: BuildExtras(); break;
                case Page.DeleteConfirm: BuildDeleteConfirm(); break;
            }
            if (firstButton != null && EventSystem.current != null && EventSystem.current.currentSelectedGameObject != firstButton.gameObject)
                EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
        }

        void BuildTitle()
        {
            // Direct reconstruction of Landing.unity.  PressAnybutton.png,
            // inventory-spritesheet_123 and LoadingSpinningIco frames are all
            // from Bla_mobile_map; no screenshot is used as a UI layer.
            Fill(page, new Color(.06666667f, .03137255f, .011764706f));
            RectTransform promptRoot = RectAt(page, "PressAKey", new Vector2(.5f, .5f), new Vector2(0, 45), new Vector2(400, 150));
            landingPrompt = promptRoot.gameObject.AddComponent<CanvasGroup>();
            landingPrompt.alpha = 0f;
            SourceArtAt(promptRoot, "Menu/PressAnybutton", Vector2.zero, new Vector2(400, 150), Color.white, true);

            Sprite barSprite = LoadSprite("Menu/TitleUIAtlas", new Rect(5, 31, 214, 24), new Vector2(.5f, .5f));
            landingPulse = SourceArtAt(promptRoot, barSprite, new Vector2(0, -100), new Vector2(400, 24), new Color(1f, 1f, 1f, .334f), false);
            LabelAt(promptRoot, "TOUCH THE SCREEN OR PRESS ANY BUTTON", new Vector2(0, -100), new Vector2(350, 30), 16, new Color(.7137255f, .52156866f, .49019608f), TextAnchor.MiddleCenter);

            var loadingFrames = new Sprite[25];
            var loadingTimes = new float[25];
            for (int i = 0; i < loadingFrames.Length; i++)
            {
                loadingFrames[i] = LoadAtlasSprite("Menu/LoadingSpinningIco", new Rect((i % 5) * 100, 400 - (i / 5) * 100, 100, 100), new Vector2(500, 500), new Vector2(.5f, .5f));
                loadingTimes[i] = i * .06f;
            }
            landingLoadingIcon = SourceArtAt(page, loadingFrames[0], Vector2.zero, new Vector2(312.5f, 312.5f), Color.white, true);
            landingLoadingIcon.gameObject.name = "Loading Icon";
            landingLoadingIcon.gameObject.SetActive(false);
            animatedLayers.Add(new AnimatedLayer(landingLoadingIcon, loadingFrames, loadingTimes, 1.5f));

            landingState = LandingState.FadeIn;
            landingStateAt = Time.unscaledTime;

            var touchAnywhere = Fill(page, Color.clear);
            var button = touchAnywhere.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(BeginLandingFadeOut);
        }

        void BuildModeSelect()
        {
            BuildAnimatedMainMenuScene();
            // VerticalLayoutGroup in GenericElements.unity: 34px buttons,
            // 7px spacing, arranged in the source 640x360 coordinate space.
            MenuChoiceAt("PILGRIMAGE", new Vector2(490, 235), () => Show(Page.SaveSlots), true);
            MenuChoiceAt("OPTIONS", new Vector2(490, 194), () => Show(Page.Options), false);
            MenuChoiceAt("EXTRAS", new Vector2(490, 153), () => Show(Page.Extras), false);
            MenuChoiceAt("EXIT", new Vector2(490, 112), ExitGame, false);
        }

        void BuildSaveSlots()
        {
            // UI_SLOT from GenericElements.unity.  Every row remains a live
            // button; 01_Menus_Pilgrimage.png is deliberately not displayed.
            Fill(page, new Color(.07058824f, .039215688f, .05882353f));
            SourceArtAt(page, "Menu/SaveHeader", new Vector2(0, 152), new Vector2(640, 32), Color.white, false);
            LabelAt(page, "SELECT PILGRIMAGE", new Vector2(0, 152), new Vector2(320, 20), 20, Gold, TextAnchor.MiddleCenter);

            BuildSaveSlot(1, 239.3f);
            BuildSaveSlot(2, 165.3f);
            BuildSaveSlot(3, 91.3f);

            // The recovered desktop coordinates overlap when both 150 px frames
            // are shown on the 640 px mobile canvas. Keep the source baseline,
            // but separate their centres by 170 px so the hit areas never touch.
            SaveActionAt(SlotExists(selectedSlot) ? "CONTINUE" : "NEW GAME", new Vector2(340, 39), StartSelectedPilgrimage, true);
            SaveActionAt("BACK", new Vector2(510, 39), () => Show(Page.ModeSelect), true);
        }

        void BuildSaveSlot(int slot, float y)
        {
            bool exists = SlotExists(slot);
            bool selected = slot == selectedSlot;
            // SaveSlot prefab mapping: Empty=Slot_02_empty, normal selected=Slot_04,
            // normal unselected=Slot_02. These are original source sprites.
            string art = exists ? (selected ? "Menu/Slot_04" : "Menu/Slot_02") : "Menu/Slot_02_empty";
            Image background = SourceArtAt(page, art, new Vector2(-32, y - 180), new Vector2(472, 50), Color.white, false);
            background.gameObject.name = "Pilgrimage " + slot;
            Image lines = SourceArtAt(page, exists ? "Menu/LineasSlot_01" : "Menu/LineasSlot_02", new Vector2(-32, y - 179), exists ? new Vector2(614, 47) : new Vector2(493, 47), Color.white, false);
            lines.raycastTarget = false;
            var button = background.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => SelectSlot(slot));
            if (selected || firstButton == null) firstButton = button;
            AddPointerSelection(button, () => SelectSlot(slot));

            Color valueColor = selected ? new Color(.99607843f, .827451f, .06666667f) : new Color(.99215686f, .88235295f, .7764706f);
            LabelAt(page, slot + " -", new Vector2(-280, y - 177), new Vector2(23, 16), 18, valueColor, TextAnchor.MiddleCenter);
            if (!exists)
            {
                LabelAt(page, "EMPTY", new Vector2(-76, y - 180), new Vector2(350, 20), 16, valueColor, TextAnchor.MiddleCenter);
                return;
            }

            SavePreview preview = ReadSavePreview(slot);
            string checkpoint = preview != null && !string.IsNullOrEmpty(preview.room) ? preview.room : "CORRUPTED SAVE";
            LabelAt(page, checkpoint, new Vector2(-76, y - 171), new Vector2(350, 20), 16, valueColor, TextAnchor.MiddleLeft);
            float completion = preview != null && preview.progress != null ? Mathf.Clamp01(preview.progress.mapPercentage) * 100f : 0f;
            string info = "PILGRIMAGE SAVED  ·  " + completion.ToString("0.00") + "% COMPLETED";
            LabelAt(page, info, new Vector2(-56, y - 188), new Vector2(390, 20), 16, new Color(1f, .73333335f, .49411765f), TextAnchor.MiddleLeft);
            if (preview != null && preview.progress != null)
                LabelAt(page, Mathf.RoundToInt(preview.progress.tears).ToString(), new Vector2(172, y - 190), new Vector2(80, 20), 16, valueColor, TextAnchor.MiddleRight);

            if (selected) SaveActionAt("DELETE", new Vector2(578, y), () => Show(Page.DeleteConfirm), true, "Menu/ButtonDelete", new Vector2(100, 34));
        }

        void SaveActionAt(string text, Vector2 sourcePosition, UnityEngine.Events.UnityAction action, bool enabled, string art = "Menu/ButtonBase", Vector2? sourceSize = null)
        {
            Vector2 size = sourceSize ?? new Vector2(150, 34);
            Image baseArt = SourceArtAt(page, art, sourcePosition - new Vector2(320, 180), size, enabled ? Color.white : new Color(1, 1, 1, .35f), false);
            var button = baseArt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.interactable = enabled;
            button.onClick.AddListener(action);
            var label = Label(button.GetComponent<RectTransform>(), text, new Vector2(.5f, .5f), size, 16, enabled ? Gold : new Color(.42f, .32f, .25f));
            button.targetGraphic = label;
        }

        void SelectSlot(int slot)
        {
            if (slot < 1 || slot > SlotCount || selectedSlot == slot) return;
            selectedSlot = slot;
            PlayerPrefs.SetInt(SlotPreference, selectedSlot);
            PlayerPrefs.Save();
            Show(Page.SaveSlots);
        }

        SavePreview ReadSavePreview(int slot)
        {
            try
            {
                if(!SafeSaveFile.TryRead(SlotPath(slot),text=>{try{var saved=JsonUtility.FromJson<SavePreview>(text);return saved!=null&&saved.version>=1&&saved.version<=6&&SourceWorldRuntime.RoomImported(saved.room)&&(saved.version<6||saved.progress!=null)&&(saved.progress==null||!float.IsNaN(saved.progress.tears)&&!float.IsInfinity(saved.progress.tears));}catch(ArgumentException){return false;}},out string json,out _))return null;
                return JsonUtility.FromJson<SavePreview>(json);
            }
            catch (Exception) { return null; }
        }

        void BuildOptions()
        {
            BuildOptionsFrame();

            // Options_Main/Selection in GenericElements.unity contains five
            // entries at 33 px with 4 px spacing. Resume/Exit are runtime-only
            // entries and are hidden when Options is opened from the title menu.
            OptionsCategoryAt("GAME", 119.5f, () => Show(Page.OptionsGame), true);
            OptionsCategoryAt("ACCESSIBILITY", 82.5f, () => Show(Page.OptionsAccessibility), false);
            OptionsCategoryAt("VIDEO", 45.5f, () => Show(Page.OptionsVideo), false);
            OptionsCategoryAt("AUDIO", 8.5f, () => Show(Page.OptionsAudio), false);
            OptionsCategoryAt("HOW TO PLAY", -28.5f, () => Show(Page.OptionsTutorial), false);
            MenuChoiceAt("BACK", new Vector2(490, 30), () => Show(Page.ModeSelect), false);
        }

        void BuildOptionsGame()
        {
            BuildOptionsSubpage("GAME");
            bool howToPlay = PlayerPrefs.GetInt("BrotherhoodHowToPlay", 1) == 1;
            OptionsSettingAt("HOW TO PLAY", howToPlay ? "ON" : "OFF", 73.5f,
                () => ToggleBool("BrotherhoodHowToPlay", 1, Page.OptionsGame),
                () => ToggleBool("BrotherhoodHowToPlay", 1, Page.OptionsGame), true);
            OptionsSettingAt("TOUCH CONTROLS", "OPEN", 38.5f,
                () => Show(Page.OptionsTouch), () => Show(Page.OptionsTouch), false);
            MenuChoiceAt("BACK", new Vector2(490, 30), () => Show(Page.Options), false);
        }

        void BuildOptionsAccessibility()
        {
            BuildOptionsSubpage("ACCESSIBILITY");
            OptionsSettingAt("HAPTIC FEEDBACK", OnOff("BrotherhoodHaptics", 1), 73.5f,
                () => ToggleBool("BrotherhoodHaptics", 1, Page.OptionsAccessibility),
                () => ToggleBool("BrotherhoodHaptics", 1, Page.OptionsAccessibility), true);
            OptionsSettingAt("SCREEN SHAKE", OnOff("BrotherhoodScreenShake", 1), 32.1667f,
                () => ToggleBool("BrotherhoodScreenShake", 1, Page.OptionsAccessibility),
                () => ToggleBool("BrotherhoodScreenShake", 1, Page.OptionsAccessibility), false);
            OptionsSettingAt("ACHIEVEMENT POPUPS", OnOff("BrotherhoodAchievementPopups", 1), -9.1667f,
                () => ToggleBool("BrotherhoodAchievementPopups", 1, Page.OptionsAccessibility),
                () => ToggleBool("BrotherhoodAchievementPopups", 1, Page.OptionsAccessibility), false);
            MenuChoiceAt("BACK", new Vector2(490, 30), () => Show(Page.Options), false);
        }

        void BuildOptionsVideo()
        {
            BuildOptionsSubpage("VIDEO");
            OptionsSettingAt("VSYNC", OnOff("BrotherhoodVSync", 0), 73.5f,
                () => ToggleVSync(Page.OptionsVideo), () => ToggleVSync(Page.OptionsVideo), true);
            OptionsSettingAt("FRAME RATE", PlayerPrefs.GetInt("BrotherhoodFrameRate", 60) + " FPS", 38.5f,
                () => ToggleFrameRate(Page.OptionsVideo), () => ToggleFrameRate(Page.OptionsVideo), false);
            OptionsSettingAt("RESOLUTION MODE", PlayerPrefs.GetInt("BrotherhoodResolutionMode", 0) == 0 ? "PIXEL PERFECT" : "SCALE", 3.5f,
                () => ToggleResolutionMode(Page.OptionsVideo), () => ToggleResolutionMode(Page.OptionsVideo), false);
            MenuChoiceAt("BACK", new Vector2(490, 30), () => Show(Page.Options), false);
        }

        void BuildOptionsAudio()
        {
            BuildOptionsSubpage("AUDIO");
            OptionsSettingAt("MASTER VOLUME", VolumeText("BrotherhoodVolume", 1f), 73.5f,
                () => ChangeVolume("BrotherhoodVolume", -10, true, Page.OptionsAudio),
                () => ChangeVolume("BrotherhoodVolume", 10, true, Page.OptionsAudio), true);
            OptionsSettingAt("EFFECTS VOLUME", VolumeText("BrotherhoodSfxVol", 1f), 38.5f,
                () => ChangeVolume("BrotherhoodSfxVol", -10, false, Page.OptionsAudio),
                () => ChangeVolume("BrotherhoodSfxVol", 10, false, Page.OptionsAudio), false);
            OptionsSettingAt("MUSIC VOLUME", VolumeText("BrotherhoodMusicVol", 1f), 3.5f,
                () => ChangeVolume("BrotherhoodMusicVol", -10, false, Page.OptionsAudio),
                () => ChangeVolume("BrotherhoodMusicVol", 10, false, Page.OptionsAudio), false);
            OptionsSettingAt("VOICEOVER VOLUME", VolumeText("BrotherhoodVoiceoverVol", 1f), -31.5f,
                () => ChangeVolume("BrotherhoodVoiceoverVol", -10, false, Page.OptionsAudio),
                () => ChangeVolume("BrotherhoodVoiceoverVol", 10, false, Page.OptionsAudio), false);
            MenuChoiceAt("BACK", new Vector2(490, 30), () => Show(Page.Options), false);
        }

        void BuildOptionsTouch()
        {
            BuildOptionsSubpage("TOUCH CONTROLS");
            bool fixedJoystick = PlayerPrefs.GetInt("BrotherhoodFixedJoystick", 0) == 1;
            float touchScale = PlayerPrefs.GetFloat("BrotherhoodTouchScale", 1f);
            OptionsSettingAt("JOYSTICK", fixedJoystick ? "FIXED" : "FLOATING", 73.5f,
                () => ToggleJoystick(Page.OptionsTouch), () => ToggleJoystick(Page.OptionsTouch), true);
            OptionsSettingAt("CONTROL SIZE", Mathf.RoundToInt(touchScale * 100) + "%", 38.5f,
                () => ChangeTouchScale(-1), () => ChangeTouchScale(1), false);
            OptionsSettingAt("RESTORE DEFAULT", "RESET", 3.5f,
                RestoreTouchDefaults, RestoreTouchDefaults, false);
            MenuChoiceAt("BACK", new Vector2(490, 30), () => Show(Page.OptionsGame), false);
        }

        void BuildOptionsTutorial()
        {
            BuildOptionsSubpage("HOW TO PLAY");
            bool enabled = PlayerPrefs.GetInt("BrotherhoodHowToPlay", 1) == 1;
            LabelAt(page, "Tutorial prompts explain movement, combat and interaction\nduring the pilgrimage.", new Vector2(35, 52), new Vector2(440, 48), 15, OptionNormal, TextAnchor.MiddleCenter);
            OptionsSettingAt("TUTORIAL POPUPS", enabled ? "ON" : "OFF", -8f,
                () => ToggleBool("BrotherhoodHowToPlay", 1, Page.OptionsTutorial),
                () => ToggleBool("BrotherhoodHowToPlay", 1, Page.OptionsTutorial), true);
            MenuChoiceAt("BACK", new Vector2(490, 30), () => Show(Page.Options), false);
        }

        void BuildOptionsFrame()
        {
            // UI_OPTIONS/Background from GenericElements.unity. Canvas_02 and
            // Retorcido_01 are copied byte-for-byte from the extracted game.
            Fill(page, new Color(.07058824f, .039215688f, .05882353f));
            SourceArtAt(page, "Menu/SaveHeader", new Vector2(0, 153.3f), new Vector2(636, 32), Color.white, false);
            SourceArtAt(page, "Menu/OptionsSide", new Vector2(-260, 0), new Vector2(67, 316), Color.white, false);
            LabelAt(page, "OPTIONS", new Vector2(0, 153.3f), new Vector2(150, 20), 20, OptionHighlight, TextAnchor.MiddleCenter);
        }

        void BuildOptionsSubpage(string title)
        {
            BuildOptionsFrame();
            // Coordinates are converted from the child anchors used by the
            // original 450x230 Options_* RectTransforms.
            LabelAt(page, title, new Vector2(-115, 118), new Vector2(180, 20), 18, OptionHighlight, TextAnchor.MiddleLeft);
            SourceArtAt(page, "Menu/OptionsLine", new Vector2(62.5f, 103), new Vector2(509, 1), Color.white, false);
        }

        void BuildExtras()
        {
            BuildMenuBackdrop("EXTRAS");
            Label(page, "CREDITS", new Vector2(.78f, .65f), new Vector2(210, 24), 18, Gold);
            Label(page, "Blasphemous\nDeveloped by The Game Kitchen\n© 2024 The Game Kitchen, SL.", new Vector2(.78f, .51f), new Vector2(220, 60), 12, new Color(.71f, .52f, .49f));
            MenuChoice("BACK", .23f, () => Show(Page.ModeSelect), true);
        }

        void BuildDeleteConfirm()
        {
            Fill(page, new Color(.07058824f, .039215688f, .05882353f));
            Label(page, "DELETE PILGRIMAGE?", new Vector2(.5f, .64f), new Vector2(360, 28), 20, Gold);
            Label(page, "The selected save slot and its backup will be erased.", new Vector2(.5f, .54f), new Vector2(440, 22), 13, new Color(.7137255f, .043137256f, .043137256f));
            MenuChoice("CANCEL", .37f, () => Show(Page.SaveSlots), true, .37f);
            MenuChoice("DELETE", .37f, DeleteSelectedSlot, false, .63f);
        }

        void BuildMenuBackdrop(string title)
        {
            BuildAnimatedMainMenuScene();
            Label(page, title, new Vector2(.78f, .75f), new Vector2(215, 28), 20, Gold);
        }

        void BuildAnimatedMainMenuScene()
        {
            // MainMenuBackground_0.asset specifies this exact crop from the
            // source atlas. MainMenuPenitentIdle.anim is the animation paired
            // with this classic Penitent background. DLC sky/tree clips are
            // intentionally excluded: they belong to a different DLC backdrop.
            SourceArt(page, LoadSprite("Menu/MainMenuBackground", new Rect(0, 664, 640, 360), new Vector2(.5f, .5f)), new Vector2(.5f, .5f), new Vector2(640, 360), Color.white, false);
            // The reference capture places the native 520x360 Penitent frame
            // against the left edge of the 640x360 composition. Centering it in
            // a 640 px Image introduced the visible 60 px horizontal offset.
            // Keep the source frame unscaled and move only this visual layer;
            // menu/version controls retain their recovered coordinates.
            AddAnimatedLayer("Menu/MainMenuPenitent", new Vector2(2048, 4096), PenitentFrames, PenitentTimes, 3.2f, new Vector2(.5f, .5f), new Vector2(520, 360), new Vector2(-60, 0));
        }

        void AddAnimatedLayer(string resource, Vector2 sourceTextureSize, SourceFrame[] definitions, float[] keyTimes, float duration, Vector2 anchor, Vector2 size, Vector2 anchoredPosition)
        {
            // The original menu atlas is pixel art. Force point sampling at runtime as
            // well as in the importer so a stale Library cache cannot blur the frames.
            var texture = Resources.Load<Texture2D>(resource);
            if (texture != null)
            {
                texture.filterMode = FilterMode.Point;
                texture.wrapMode = TextureWrapMode.Clamp;
            }

            var frames = new Sprite[definitions.Length];
            for (int i = 0; i < definitions.Length; i++) frames[i] = LoadAtlasSprite(resource, definitions[i].rect, sourceTextureSize, definitions[i].pivot);
            var image = SourceArt(page, frames.Length > 0 ? frames[0] : null, anchor, size, Color.white, true);
            image.rectTransform.anchoredPosition = anchoredPosition;
            animatedLayers.Add(new AnimatedLayer(image, frames, keyTimes, duration));
        }

        void UpdateMenuInput()
        {
            if (menuButtons.Count == 0) return;
            int move = 0;
            if ((Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame)) move = -1;
            if ((Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame)) move = 1;
            if (move != 0)
            {
                int next = (menuSelection + move + menuButtons.Count) % menuButtons.Count;
                SetMenuSelection(next);
                return;
            }
            int horizontal = 0;
            if ((Keyboard.current != null && Keyboard.current.leftArrowKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame)) horizontal = -1;
            if ((Keyboard.current != null && Keyboard.current.rightArrowKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame)) horizontal = 1;
            if (horizontal != 0 && menuSelection >= 0 && menuSelection < menuButtons.Count)
            {
                UnityEngine.Events.UnityAction change = horizontal < 0 ? menuLeftActions[menuSelection] : menuRightActions[menuSelection];
                if (change != null) change.Invoke();
                return;
            }
            bool accept = (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
            if (accept && menuSelection >= 0 && menuSelection < menuButtons.Count && menuButtons[menuSelection].interactable) menuButtons[menuSelection].onClick.Invoke();
        }

        void UpdateSaveSlotInput()
        {
            int move = 0;
            if ((Keyboard.current != null && Keyboard.current.upArrowKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame)) move = -1;
            if ((Keyboard.current != null && Keyboard.current.downArrowKey.wasPressedThisFrame) || (Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame)) move = 1;
            if (move != 0)
            {
                selectedSlot = (selectedSlot - 1 + move + SlotCount) % SlotCount + 1;
                PlayerPrefs.SetInt(SlotPreference, selectedSlot);
                PlayerPrefs.Save();
                Show(Page.SaveSlots);
                return;
            }
            bool accept = (Keyboard.current != null && (Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame))
                || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
            if (accept) StartSelectedPilgrimage();
            bool delete = Keyboard.current != null && Keyboard.current.deleteKey.wasPressedThisFrame;
            if (delete && SlotExists(selectedSlot)) Show(Page.DeleteConfirm);
        }

        // Frame definitions and key times are transcribed directly from
        // MainMenuPenitentIdle.anim.
        static readonly SourceFrame[] PenitentFrames =
        {
            new SourceFrame(0, 0, 520, 360, .5346154f, .5f), new SourceFrame(520, 0, 520, 360, .5346154f, .5f), new SourceFrame(1040, 0, 520, 360, .5346154f, .5f),
            new SourceFrame(1040, 720, 519, 360, .5356455f, .5f), new SourceFrame(0, 1080, 519, 360, .5356455f, .5f), new SourceFrame(1038, 1800, 518, 360, .53667957f, .5f),
            new SourceFrame(0, 2520, 517, 360, .5377176f, .5f), new SourceFrame(519, 1080, 519, 360, .5356455f, .5f), new SourceFrame(1038, 1080, 519, 360, .5356455f, .5f),
            new SourceFrame(0, 1440, 519, 360, .5356455f, .5f), new SourceFrame(519, 1440, 519, 360, .5356455f, .5f), new SourceFrame(1038, 1440, 519, 360, .5356455f, .5f),
            new SourceFrame(0, 1800, 519, 360, .5356455f, .5f), new SourceFrame(0, 2160, 518, 360, .53667957f, .5f), new SourceFrame(518, 2160, 518, 360, .53667957f, .5f),
            new SourceFrame(1036, 2160, 518, 360, .53667957f, .5f), new SourceFrame(519, 1800, 519, 360, .5356455f, .5f), new SourceFrame(0, 360, 520, 360, .5346154f, .5f),
            new SourceFrame(520, 360, 520, 360, .5346154f, .5f), new SourceFrame(1040, 360, 520, 360, .5346154f, .5f), new SourceFrame(0, 720, 520, 360, .5346154f, .5f),
            new SourceFrame(520, 720, 520, 360, .5346154f, .5f)
        };

        static readonly float[] PenitentTimes = { 0f, .1f, .2f, .3f, .4f, .5f, .6f, .7f, .8f, .95f, 1.1f, 1.6f, 1.7f, 1.8f, 1.9f, 2f, 2.1f, 2.2f, 2.3f, 2.45f, 2.6f, 3.1f };

        void MenuChoice(string value, float y, UnityEngine.Events.UnityAction action, bool selected, float x = .79f)
        {
            MenuChoiceAt(value, new Vector2(x * 640f, y * 360f), action, selected);
        }

        void MenuChoiceAt(string value, Vector2 sourcePosition, UnityEngine.Events.UnityAction action, bool selected)
        {
            Vector2 anchored = sourcePosition - new Vector2(320, 180);
            // GenericElements.unity uses Boton_03 for the normal frame,
            // button_background_selected for focus, and marker at +96.5 px.
            var baseArt = SourceArtAt(page, "Menu/ButtonBase", anchored, new Vector2(160, 34), Color.white, false);
            var selector = SourceArtAt(page, "Menu/ButtonSelected", anchored, new Vector2(160, 42), Color.clear, false);
            selector.raycastTarget = false;
            var marker = SourceArtAt(page, "Menu/MenuMarker", anchored + new Vector2(96.5f, 3.2f), new Vector2(11, 15), Color.clear, true);
            marker.raycastTarget = false;
            var button = baseArt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            if (firstButton == null) firstButton = button;
            button.onClick.AddListener(action);
            LabelAt(selector.rectTransform, value, new Vector2(0, -1), new Vector2(150, 20), 16, new Color(.12f, .045f, .02f, .8f), TextAnchor.MiddleCenter);
            var label = LabelAt(selector.rectTransform, value, Vector2.zero, new Vector2(150, 20), 16, selected ? Gold : new Color(.76f, .58f, .36f), TextAnchor.MiddleCenter);
            button.targetGraphic = baseArt;
            int index = menuButtons.Count;
            menuSelectors.Add(selector);
            menuMarkers.Add(marker);
            menuLabels.Add(label);
            menuButtons.Add(button);
            menuAuxSelectors.Add(null);
            menuLeftActions.Add(null);
            menuRightActions.Add(null);
            menuNormalColors.Add(new Color(.76f, .58f, .36f));
            menuHighlightColors.Add(Gold);
            AddPointerSelection(button, () => SetMenuSelection(index));
            if (selected) SetMenuSelection(index);
        }

        void OptionsCategoryAt(string value, float y, UnityEngine.Events.UnityAction action, bool selected)
        {
            Sprite rowSprite = LoadSlicedSprite("Menu/OptionsRow", new Vector4(11, 15, 11, 11));
            Image row = SourceArtAt(page, rowSprite, new Vector2(0, y), new Vector2(300, 33), Color.white, false);
            row.type = Image.Type.Sliced;
            var button = row.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
            if (firstButton == null) firstButton = button;

            Sprite markerSprite = LoadSprite("Menu/OptionsUIAtlas", new Rect(4, 475, 10, 18), new Vector2(.5f, .5f));
            Image marker = SourceArtAt(page, markerSprite, new Vector2(-135, y), new Vector2(10, 18), selected ? Color.white : Color.clear, false);
            marker.raycastTarget = false;
            Text label = LabelAt(page, value, new Vector2(5, y), new Vector2(250, 20), 16, selected ? OptionHighlight : OptionNormal, TextAnchor.MiddleCenter);

            int index = menuButtons.Count;
            menuSelectors.Add(null);
            menuAuxSelectors.Add(null);
            menuMarkers.Add(marker);
            menuLabels.Add(label);
            menuButtons.Add(button);
            menuLeftActions.Add(null);
            menuRightActions.Add(null);
            menuNormalColors.Add(OptionNormal);
            menuHighlightColors.Add(OptionHighlight);
            AddPointerSelection(button, () => SetMenuSelection(index));
            if (selected) SetMenuSelection(index);
        }

        void OptionsSettingAt(string labelText, string valueText, float y,
            UnityEngine.Events.UnityAction leftAction, UnityEngine.Events.UnityAction rightAction, bool selected)
        {
            Image hitArea = SourceArtAt(page, (Sprite)null, new Vector2(20, y), new Vector2(480, 30), Color.clear, false);
            hitArea.gameObject.name = labelText;
            var button = hitArea.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(rightAction);
            if (firstButton == null) firstButton = button;

            Sprite markerSprite = LoadSprite("Menu/OptionsUIAtlas", new Rect(4, 475, 10, 18), new Vector2(.5f, .5f));
            Image marker = SourceArtAt(page, markerSprite, new Vector2(-204.5f, y), new Vector2(10, 18), selected ? Color.white : Color.clear, false);
            marker.raycastTarget = false;
            Text label = LabelAt(page, labelText, new Vector2(-134.5f, y), new Vector2(150, 20), 15, selected ? OptionHighlight : OptionNormal, TextAnchor.MiddleLeft);
            LabelAt(page, valueText, new Vector2(172, y), new Vector2(130, 20), 15, OptionNormal, TextAnchor.MiddleCenter);

            Image left = SourceArtAt(page, "Menu/OptionsArrow", new Vector2(97, y), new Vector2(16, 25), selected ? Color.white : Color.clear, false);
            left.rectTransform.localScale = new Vector3(-1, 1, 1);
            left.raycastTarget = false;
            Image right = SourceArtAt(page, "Menu/OptionsArrow", new Vector2(247, y), new Vector2(16, 25), selected ? Color.white : Color.clear, false);
            right.raycastTarget = false;

            int index = menuButtons.Count;
            menuSelectors.Add(left);
            menuAuxSelectors.Add(right);
            menuMarkers.Add(marker);
            menuLabels.Add(label);
            menuButtons.Add(button);
            menuLeftActions.Add(leftAction);
            menuRightActions.Add(rightAction);
            menuNormalColors.Add(OptionNormal);
            menuHighlightColors.Add(OptionHighlight);
            AddPointerSelection(button, () => SetMenuSelection(index));
            if (selected) SetMenuSelection(index);
        }

        void AddPointerSelection(Button button, UnityEngine.Events.UnityAction action)
        {
            var trigger = button.gameObject.AddComponent<EventTrigger>();
            var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
            enter.callback.AddListener(_ => action());
            trigger.triggers.Add(enter);
            var select = new EventTrigger.Entry { eventID = EventTriggerType.Select };
            select.callback.AddListener(_ => action());
            trigger.triggers.Add(select);
        }

        void SetMenuSelection(int index)
        {
            if (index < 0 || index >= menuButtons.Count) return;
            menuSelection = index;
            for (int i = 0; i < menuButtons.Count; i++)
            {
                if (menuSelectors[i] != null) menuSelectors[i].color = i == index ? Color.white : Color.clear;
                if (menuAuxSelectors[i] != null) menuAuxSelectors[i].color = i == index ? Color.white : Color.clear;
                if (menuMarkers[i] != null) menuMarkers[i].color = i == index ? Color.white : Color.clear;
                if (menuLabels[i] != null) menuLabels[i].color = i == index ? menuHighlightColors[i] : menuNormalColors[i];
            }
            // EventTriggerType.Select calls this while EventSystem is already in
            // SetSelectedGameObject. Selecting the same object again produces
            // "Attempting to select ... while already selecting an object".
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != menuButtons[index].gameObject)
                EventSystem.current.SetSelectedGameObject(menuButtons[index].gameObject);
        }

        void Hotspot(string name, Vector2 min, Vector2 max, UnityEngine.Events.UnityAction action)
        {
            var target = Fill(page, min, max, Color.clear);
            target.gameObject.name = name;
            var button = target.gameObject.AddComponent<Button>();
            if (firstButton == null) firstButton = button;
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(action);
        }

        static bool IsOptionsSubpage(Page value)
        {
            return value == Page.OptionsGame || value == Page.OptionsAccessibility || value == Page.OptionsVideo
                || value == Page.OptionsAudio || value == Page.OptionsTouch || value == Page.OptionsTutorial;
        }

        static string OnOff(string key, int defaultValue)
        {
            return PlayerPrefs.GetInt(key, defaultValue) == 1 ? "ON" : "OFF";
        }

        static string VolumeText(string key, float defaultValue)
        {
            return Mathf.RoundToInt(PlayerPrefs.GetFloat(key, defaultValue) * 100f) + "%";
        }

        void ToggleBool(string key, int defaultValue, Page returnPage)
        {
            PlayerPrefs.SetInt(key, 1 - PlayerPrefs.GetInt(key, defaultValue));
            PlayerPrefs.Save();
            Show(returnPage);
        }

        void ToggleJoystick(Page returnPage)
        {
            PlayerPrefs.SetInt("BrotherhoodFixedJoystick", 1 - PlayerPrefs.GetInt("BrotherhoodFixedJoystick", 0));
            PlayerPrefs.Save();
            Show(returnPage);
        }

        void ChangeTouchScale(int direction)
        {
            float currentValue = PlayerPrefs.GetFloat("BrotherhoodTouchScale", 1f);
            float[] values = { .8f, 1f, 1.2f };
            int index = 0;
            for (int i = 1; i < values.Length; i++)
                if (Mathf.Abs(values[i] - currentValue) < Mathf.Abs(values[index] - currentValue)) index = i;
            index = (index + direction + values.Length) % values.Length;
            PlayerPrefs.SetFloat("BrotherhoodTouchScale", values[index]);
            PlayerPrefs.Save();
            Show(Page.OptionsTouch);
        }

        void RestoreTouchDefaults()
        {
            PlayerPrefs.SetInt("BrotherhoodFixedJoystick", 0);
            PlayerPrefs.SetFloat("BrotherhoodTouchScale", 1f);
            PlayerPrefs.Save();
            Show(Page.OptionsTouch);
        }

        void ChangeVolume(string key, int delta, bool applyToListener, Page returnPage)
        {
            int value = Mathf.RoundToInt(PlayerPrefs.GetFloat(key, 1f) * 100f) + delta;
            if (value > 100) value = 0;
            else if (value < 0) value = 100;
            PlayerPrefs.SetFloat(key, value / 100f);
            PlayerPrefs.Save();
            if (applyToListener) AudioListener.volume = value / 100f;
            Show(returnPage);
        }

        void ToggleVSync(Page returnPage)
        {
            int enabled = 1 - PlayerPrefs.GetInt("BrotherhoodVSync", 0);
            PlayerPrefs.SetInt("BrotherhoodVSync", enabled);
            PlayerPrefs.Save();
            QualitySettings.vSyncCount = enabled;
            Show(returnPage);
        }

        void ToggleFrameRate(Page returnPage)
        {
            int value = PlayerPrefs.GetInt("BrotherhoodFrameRate", 60) == 60 ? 30 : 60;
            PlayerPrefs.SetInt("BrotherhoodFrameRate", value);
            PlayerPrefs.Save();
            Application.targetFrameRate = value;
            Show(returnPage);
        }

        void ToggleResolutionMode(Page returnPage)
        {
            PlayerPrefs.SetInt("BrotherhoodResolutionMode", 1 - PlayerPrefs.GetInt("BrotherhoodResolutionMode", 0));
            PlayerPrefs.Save();
            ApplyResolutionMode();
            Show(returnPage);
        }

        void ApplyResolutionMode()
        {
            if (menuScaler == null) return;
            // Matches OptionsWidget.SCALING_STRATEGY: PIXEL_PERFECT keeps the
            // source 640x360 height; SCALE balances width and height.
            menuScaler.matchWidthOrHeight = PlayerPrefs.GetInt("BrotherhoodResolutionMode", 0) == 0 ? 1f : .5f;
        }

        void StartSelectedPilgrimage()
        {
            PlayerPrefs.SetInt(SlotPreference, selectedSlot);
            if (SlotExists(selectedSlot)) PlayerPrefs.DeleteKey("BrotherhoodNewPilgrimage");
            else PlayerPrefs.SetInt("BrotherhoodNewPilgrimage", 1);
            PlayerPrefs.Save();
            StartCoroutine(LoadPilgrimage());
        }

        IEnumerator LoadPilgrimage()
        {
            yield return null;
            SceneManager.LoadScene("Brotherhood");
        }

        void DeleteSelectedSlot()
        {
            try
            {
                string path = SlotPath(selectedSlot);
                if (File.Exists(path)) File.Delete(path);
                if (File.Exists(path + ".bak")) File.Delete(path + ".bak");
                PlayerPrefs.DeleteKey("BrotherhoodNewPilgrimage");
                PlayerPrefs.Save();
            }
            catch (Exception error) { Debug.LogWarning("Could not delete save slot: " + error.Message); }
            Show(Page.SaveSlots);
        }

        void ExitGame()
        {
#if UNITY_EDITOR
            Debug.Log("EXIT is available in an Android build.");
#else
            Application.Quit();
#endif
        }

        bool SlotExists(int slot) => File.Exists(SlotPath(slot));
        static string SlotPath(int slot)
        {
            // Slot 1 deliberately keeps the prior filename so existing progress remains valid.
            return Path.Combine(Application.persistentDataPath, slot == 1 ? "brotherhood-save.json" : "brotherhood-save-slot-" + slot + ".json");
        }

        static readonly Color Gold = new Color(.9f, .68f, .3f);
        static readonly Color OptionNormal = new Color(1f, .73333335f, .49411765f);
        static readonly Color OptionHighlight = new Color(1f, .9529412f, .6392157f);

        RectTransform RectAt(RectTransform parent, string name, Vector2 anchor, Vector2 anchoredPosition, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            SetRect(rect, anchor, anchor, anchoredPosition, size, new Vector2(.5f, .5f));
            return rect;
        }

        Image SourceArtAt(RectTransform parent, string resource, Vector2 anchoredPosition, Vector2 size, Color tint, bool preserveAspect)
        {
            return SourceArtAt(parent, LoadSprite(resource), anchoredPosition, size, tint, preserveAspect);
        }

        Image SourceArtAt(RectTransform parent, Sprite sprite, Vector2 anchoredPosition, Vector2 size, Color tint, bool preserveAspect)
        {
            Image image = SourceArt(parent, sprite, new Vector2(.5f, .5f), size, tint, preserveAspect);
            image.rectTransform.anchoredPosition = anchoredPosition;
            return image;
        }

        Image SourceArt(RectTransform parent, string resource, Vector2 anchor, Vector2 size, Color tint, bool preserveAspect)
        {
            return SourceArt(parent, LoadSprite(resource), anchor, size, tint, preserveAspect);
        }

        Image SourceArt(RectTransform parent, Sprite sprite, Vector2 anchor, Vector2 size, Color tint, bool preserveAspect)
        {
            var image = new GameObject(sprite != null ? sprite.name : "Missing source art", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            var rect = image.rectTransform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = new Vector2(.5f, .5f);
            rect.sizeDelta = size;
            image.sprite = sprite;
            image.color = tint;
            image.preserveAspect = preserveAspect;
            return image;
        }

        Text LabelAt(RectTransform parent, string value, Vector2 anchoredPosition, Vector2 size, int fontSize, Color color, TextAnchor alignment)
        {
            Text text = Label(parent, value, new Vector2(.5f, .5f), size, fontSize, color);
            text.rectTransform.anchoredPosition = anchoredPosition;
            text.alignment = alignment;
            return text;
        }

        Image Fill(RectTransform parent, Color color) => Fill(parent, Vector2.zero, Vector2.one, color);
        Image Fill(RectTransform parent, Vector2 min, Vector2 max, Color color)
        {
            var image = new GameObject("Panel", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.rectTransform.SetParent(parent, false);
            image.rectTransform.anchorMin = min;
            image.rectTransform.anchorMax = max;
            image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
            image.color = color;
            return image;
        }

        Text Label(RectTransform parent, string value, Vector2 anchor, Vector2 size, int fontSize, Color color)
        {
            var text = new GameObject(value.Length > 0 ? value : "Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            text.rectTransform.SetParent(parent, false);
            text.rectTransform.anchorMin = text.rectTransform.anchorMax = anchor;
            text.rectTransform.pivot = new Vector2(.5f, .5f);
            text.rectTransform.sizeDelta = size;
            if (originalFont == null) originalFont = Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");
            text.font = originalFont != null ? originalFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.raycastTarget = false;
            VietnameseSource.Apply(text);
            return text;
        }

        Sprite LoadSprite(string resource)
        {
            var texture = Resources.Load<Texture2D>(resource);
            return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f));
        }

        Sprite LoadSlicedSprite(string resource, Vector4 border)
        {
            var texture = Resources.Load<Texture2D>(resource);
            return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, border);
        }

        Sprite LoadSprite(string resource, Rect sourceRect, Vector2 pivot)
        {
            var texture = Resources.Load<Texture2D>(resource);
            return texture == null ? null : Sprite.Create(texture, sourceRect, pivot);
        }

        Sprite LoadSprite(string resource, Vector2 pivot)
        {
            var texture = Resources.Load<Texture2D>(resource);
            return texture == null ? null : Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot);
        }

        Sprite LoadAtlasSprite(string resource, Rect sourceRect, Vector2 sourceTextureSize, Vector2 pivot)
        {
            var texture = Resources.Load<Texture2D>(resource);
            if (texture == null) return null;
            float scaleX = texture.width / sourceTextureSize.x;
            float scaleY = texture.height / sourceTextureSize.y;
            var importedRect = new Rect(sourceRect.x * scaleX, sourceRect.y * scaleY, sourceRect.width * scaleX, sourceRect.height * scaleY);
            return Sprite.Create(texture, importedRect, pivot);
        }

        void ApplySafeArea()
        {
            Rect area = Screen.safeArea;
            float width = Mathf.Max(1, Screen.width), height = Mathf.Max(1, Screen.height);
            safe.anchorMin = area.position / new Vector2(width, height);
            safe.anchorMax = (area.position + area.size) / new Vector2(width, height);
            safe.offsetMin = safe.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta, Vector2 pivot)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
            rect.pivot = pivot;
        }
    }
}
