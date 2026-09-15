using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Brotherhood
{
    public sealed class BlasOptionsUI : MonoBehaviour
    {
        public BrotherhoodGame game;
        public TouchControls controls;

        private GameObject root;
        private Image cursorDiamond;
        private RectTransform statueRect;

        // Sub panels
        private GameObject mainMenuPanel;
        private GameObject gameSubPanel;
        private GameObject accessSubPanel;
        private GameObject soundSubPanel;
        private GameObject tipsSubPanel;

        private readonly GameObject[] menuButtons = new GameObject[6];
        private int selectedOption = 5; // Default: RESUME GAME

        // Audio sliders
        private Slider masterSlider, musicSlider, sfxSlider;
        private Text masterValText, musicValText, sfxValText;

        // Accessibility toggles
        private Text shakeToggleText, hapticsToggleText;

        // Game settings
        private Text joystickToggleText;
        private Slider touchScaleSlider;
        private Text touchScaleValText;

        private Font titleFont;
        private Font bodyFont;
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static readonly string[] OptionTitles = new string[]
        {
            "Game", "Accessibility", "Sound", "Gameplay Tips", "Exit to Main Menu", "Resume Game"
        };

        public bool IsOpen => root != null && root.activeSelf;
        public Canvas Canvas { get; private set; }

        void Awake()
        {
            LoadFonts();
        }

        private void LoadFonts()
        {
            titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bodyFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (titleFont == null) titleFont = bodyFont;
            if (bodyFont == null) bodyFont = titleFont;
            if (titleFont == null) titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bodyFont == null) bodyFont = titleFont;
        }

        public void Initialize(BrotherhoodGame g, TouchControls c)
        {
            game = g;
            controls = c;
            BuildUI();
        }

        public void Show()
        {
            if (root == null) BuildUI();
            root.SetActive(true);
            ShowSubPanel(null); // Show main 6 buttons
            selectedOption = 5;
            UpdateCursor();
            Time.timeScale = 0f;
            game?.Sfx("INVENTORY_OPEN");
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            Time.timeScale = 1f;
            game?.Sfx("INVENTORY_CLOSE");
        }

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }

        private void BuildUI()
        {
            if (root != null) return;

            var canvasObj = new GameObject("BlasOptionsCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            Canvas = canvasObj.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 30; // Above Inventory & Map

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            // Root dark overlay
            root = MakePanel(canvasObj.transform, "OptionsRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.02f, 0.02f, 0.03f, 0.95f));

            // Main container (width 1400, height 880)
            var container = MakePanel(root.transform, "Container", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400, 880), Color.clear);

            // Left Side: Twisted Column Statue (menu-options-spritesheet_5)
            var statueObj = MakePanel(container.transform, "Statue", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(160, 0), new Vector2(180, 820), Color.white);
            statueRect = statueObj.GetComponent<RectTransform>();
            var statueImg = statueObj.GetComponent<Image>();
            statueImg.sprite = LoadSprite("UI/Sprites/menu-options-spritesheet_5");
            statueImg.preserveAspect = true;
            statueImg.color = new Color(0.9f, 0.88f, 0.82f, 0.95f);

            // Right Side Title: "OPTIONS"
            var title = MakeLabel(container.transform, "OPTIONS", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(120, -50), new Vector2(600, 50), 32, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

            // Diamond Selector Cursor
            var cursorObj = MakePanel(container.transform, "DiamondCursor", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36), Color.clear);
            cursorDiamond = cursorObj.GetComponent<Image>();
            cursorDiamond.sprite = LoadSprite("UI/Sprites/dialogue-option-selector");
            if (cursorDiamond.sprite == null) cursorDiamond.sprite = LoadSprite("UI/dialogue-option-selector");
            cursorDiamond.color = cursorDiamond.sprite != null ? Color.white : Color.clear;
            cursorDiamond.preserveAspect = true;
            cursorDiamond.raycastTarget = false;

            // Main 6 Buttons Area
            mainMenuPanel = MakePanel(container.transform, "MainOptionsPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(700, 680), Color.clear);

            const float btnWidth = 420f;
            const float btnHeight = 56f;
            const float btnSpacing = 72f;
            const float startY = 220f;

            for (int i = 0; i < 6; i++)
            {
                int optIdx = i;
                float y = startY - i * btnSpacing;
                var btn = MakeButton(mainMenuPanel.transform, "Opt_" + i, new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(btnWidth, btnHeight), () => OnSelectOption(optIdx));
                
                var btnImg = btn.GetComponent<Image>();
                var btnSp = LoadSprite("UI/Sprites/Boton_04");
                if (btnSp != null) { btnImg.sprite = btnSp; btnImg.color = Color.white; btnImg.type = Image.Type.Sliced; }
                else { btnImg.color = new Color(0.18f, 0.09f, 0.09f, 0.9f); btn.AddComponent<Outline>().effectColor = new Color(0.7f, 0.5f, 0.2f); }

                MakeLabel(btn.transform, OptionTitles[i], Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.72f));
                menuButtons[i] = btn;
            }

            // Build Sub Panels
            BuildGameSubPanel(container.transform);
            BuildAccessSubPanel(container.transform);
            BuildSoundSubPanel(container.transform);
            BuildTipsSubPanel(container.transform);

            // Back button at bottom right — Boton_04 styled
            var backBtn = MakeButton(container.transform, "BackBtn", new Vector2(1, 0), new Vector2(-150, 60), new Vector2(200, 56), OnClickBack);
            var bkImg = backBtn.GetComponent<Image>();
            var bkSp = LoadSprite("UI/Sprites/Boton_04");
            if (bkSp != null) { bkImg.sprite = bkSp; bkImg.color = Color.white; bkImg.type = Image.Type.Sliced; }
            else { bkImg.color = new Color(0.18f, 0.09f, 0.09f, 0.9f); backBtn.AddComponent<Outline>().effectColor = new Color(0.7f, 0.5f, 0.2f); }
            MakeLabel(backBtn.transform, "BACK", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 22, TextAnchor.MiddleCenter, new Color(0.95f, 0.88f, 0.72f));


            root.SetActive(false);
        }

        private void BuildSoundSubPanel(Transform parent)
        {
            soundSubPanel = MakePanel(parent, "SoundPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(700, 600), Color.clear);
            MakeLabel(soundSubPanel.transform, "AUDIO SETTINGS", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(400, 40), 24, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

            // Master Volume
            MakeLabel(soundSubPanel.transform, "MASTER VOLUME", new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -100), new Vector2(240, 36), 18, TextAnchor.MiddleLeft, Color.white);
            masterSlider = MakeSlider(soundSubPanel.transform, "MasterSlider", new Vector2(0.5f, 1), new Vector2(60, -100), new Vector2(300, 24), v => OnChangeMasterVol(v));
            masterValText = MakeLabel(soundSubPanel.transform, "100%", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -100), new Vector2(80, 36), 18, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.45f));

            // Music Volume
            MakeLabel(soundSubPanel.transform, "MUSIC VOLUME", new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -180), new Vector2(240, 36), 18, TextAnchor.MiddleLeft, Color.white);
            musicSlider = MakeSlider(soundSubPanel.transform, "MusicSlider", new Vector2(0.5f, 1), new Vector2(60, -180), new Vector2(300, 24), v => OnChangeMusicVol(v));
            musicValText = MakeLabel(soundSubPanel.transform, "100%", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -180), new Vector2(80, 36), 18, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.45f));

            // SFX Volume
            MakeLabel(soundSubPanel.transform, "SFX VOLUME", new Vector2(0, 1), new Vector2(0, 1), new Vector2(80, -260), new Vector2(240, 36), 18, TextAnchor.MiddleLeft, Color.white);
            sfxSlider = MakeSlider(soundSubPanel.transform, "SfxSlider", new Vector2(0.5f, 1), new Vector2(60, -260), new Vector2(300, 24), v => OnChangeSfxVol(v));
            sfxValText = MakeLabel(soundSubPanel.transform, "100%", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-80, -260), new Vector2(80, 36), 18, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.45f));

            soundSubPanel.SetActive(false);
        }

        private void BuildAccessSubPanel(Transform parent)
        {
            accessSubPanel = MakePanel(parent, "AccessPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(700, 600), Color.clear);
            MakeLabel(accessSubPanel.transform, "ACCESSIBILITY", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(400, 40), 24, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

            // Screen Shake
            MakeLabel(accessSubPanel.transform, "SCREEN SHAKE", new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -120), new Vector2(300, 40), 20, TextAnchor.MiddleLeft, Color.white);
            var shakeBtn = MakeButton(accessSubPanel.transform, "ShakeBtn", new Vector2(1, 1), new Vector2(-150, -120), new Vector2(160, 48), ToggleShake);
            shakeBtn.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.15f, 0.9f);
            shakeBtn.AddComponent<Outline>().effectColor = new Color(0.7f, 0.5f, 0.2f);
            shakeToggleText = MakeLabel(shakeBtn.transform, "ON", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAnchor.MiddleCenter, new Color(0.3f, 0.85f, 0.4f));

            // Haptics Vibration
            MakeLabel(accessSubPanel.transform, "VIBRATION / HAPTICS", new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -200), new Vector2(300, 40), 20, TextAnchor.MiddleLeft, Color.white);
            var hapticsBtn = MakeButton(accessSubPanel.transform, "HapticsBtn", new Vector2(1, 1), new Vector2(-150, -200), new Vector2(160, 48), ToggleHaptics);
            hapticsBtn.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.15f, 0.9f);
            hapticsBtn.AddComponent<Outline>().effectColor = new Color(0.7f, 0.5f, 0.2f);
            hapticsToggleText = MakeLabel(hapticsBtn.transform, "ON", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAnchor.MiddleCenter, new Color(0.3f, 0.85f, 0.4f));

            accessSubPanel.SetActive(false);
        }

        private void BuildGameSubPanel(Transform parent)
        {
            gameSubPanel = MakePanel(parent, "GamePanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(700, 600), Color.clear);
            MakeLabel(gameSubPanel.transform, "GAME SETTINGS", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(400, 40), 24, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

            // Joystick Mode
            MakeLabel(gameSubPanel.transform, "JOYSTICK MODE", new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -100), new Vector2(280, 40), 18, TextAnchor.MiddleLeft, Color.white);
            var joyBtn = MakeButton(gameSubPanel.transform, "JoyModeBtn", new Vector2(1, 1), new Vector2(-150, -100), new Vector2(180, 48), ToggleJoystickMode);
            joyBtn.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.15f, 0.9f);
            joyBtn.AddComponent<Outline>().effectColor = new Color(0.7f, 0.5f, 0.2f);
            joystickToggleText = MakeLabel(joyBtn.transform, "FLOATING", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 16, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

            // Touch Scale
            MakeLabel(gameSubPanel.transform, "TOUCH CONTROLS SCALE", new Vector2(0, 1), new Vector2(0, 1), new Vector2(100, -180), new Vector2(280, 40), 18, TextAnchor.MiddleLeft, Color.white);
            touchScaleSlider = MakeSlider(gameSubPanel.transform, "ScaleSlider", new Vector2(0.5f, 1), new Vector2(60, -180), new Vector2(240, 24), v => OnChangeTouchScale(v));
            touchScaleSlider.minValue = 0.8f;
            touchScaleSlider.maxValue = 1.3f;
            touchScaleSlider.value = PlayerPrefs.GetFloat("BrotherhoodTouchScale", 1.0f);
            touchScaleValText = MakeLabel(gameSubPanel.transform, "100%", new Vector2(1, 1), new Vector2(1, 1), new Vector2(-60, -180), new Vector2(70, 36), 18, TextAnchor.MiddleRight, new Color(1f, 0.85f, 0.45f));

            // Customize Layout Button
            var layoutBtn = MakeButton(gameSubPanel.transform, "LayoutBtn", new Vector2(0.5f, 1), new Vector2(0, -280), new Vector2(380, 56), OnClickRemap);
            layoutBtn.GetComponent<Image>().color = new Color(0.2f, 0.15f, 0.1f, 0.9f);
            layoutBtn.AddComponent<Outline>().effectColor = new Color(0.85f, 0.65f, 0.25f);
            MakeLabel(layoutBtn.transform, "CUSTOMIZE BUTTON LAYOUT", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 18, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

            gameSubPanel.SetActive(false);
        }

        private void BuildTipsSubPanel(Transform parent)
        {
            tipsSubPanel = MakePanel(parent, "TipsPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(120, 0), new Vector2(700, 640), Color.clear);
            MakeLabel(tipsSubPanel.transform, "GAMEPLAY & COMBAT TIPS", new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -15), new Vector2(500, 36), 24, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

            string tips = 
                "* MEA CULPA COMBO: Tap [Attack] 3 times in rhythm for a deadly combo finish.\n\n" +
                "* CHARGED ATTACK: Hold [Attack] until sword glints with holy energy, then release.\n\n" +
                "* FERVOROUS BLOOD: Tap [Range Attack] (above Dash) to sacrifice Fervour and launch a ranged blood disc.\n\n" +
                "* SACRED ONSLAUGHT: Press [Dash] then immediately [Attack] to execute a lunging thrust.\n\n" +
                "* WEIGHT OF SIN: Press [Down] + [Attack] in mid-air to plunge downward with crushing force.\n\n" +
                "* RIGHTEOUS PARRY: Tap [Parry] just as an enemy strikes to stagger them and execute an execution counter.\n\n" +
                "* BILE FLASKS: Tap [Flask] when health is low to drink and restore your health.";

            var body = MakeLabel(tipsSubPanel.transform, tips, Vector2.zero, Vector2.one, new Vector2(40, 20), new Vector2(-40, -60), 18, TextAnchor.UpperLeft, new Color(0.9f, 0.88f, 0.82f));

            tipsSubPanel.SetActive(false);
        }

        private void OnSelectOption(int idx)
        {
            selectedOption = idx;
            UpdateCursor();
            game?.Sfx("INVENTORY_SCROLL");

            switch(idx)
            {
                case 0: ShowSubPanel(gameSubPanel); break;
                case 1: ShowSubPanel(accessSubPanel); break;
                case 2: ShowSubPanel(soundSubPanel); break;
                case 3: ShowSubPanel(tipsSubPanel); break;
                case 4: OnExitToMainMenu(); break;
                case 5: Hide(); break; // Resume
            }
        }

        private void ShowSubPanel(GameObject panel)
        {
            mainMenuPanel.SetActive(panel == null);
            gameSubPanel.SetActive(panel == gameSubPanel);
            accessSubPanel.SetActive(panel == accessSubPanel);
            soundSubPanel.SetActive(panel == soundSubPanel);
            tipsSubPanel.SetActive(panel == tipsSubPanel);

            if (cursorDiamond != null)
                cursorDiamond.gameObject.SetActive(panel == null);
        }

        private void OnClickBack()
        {
            if (!mainMenuPanel.activeSelf)
            {
                ShowSubPanel(null);
                UpdateCursor();
                game?.Sfx("INVENTORY_SCROLL");
            }
            else
            {
                Hide();
            }
        }

        private void UpdateCursor()
        {
            if (cursorDiamond == null || selectedOption < 0 || selectedOption >= menuButtons.Length) return;
            const float btnSpacing = 72f;
            const float startY = 220f;
            float y = startY - selectedOption * btnSpacing;
            cursorDiamond.rectTransform.anchoredPosition = new Vector2(-250, y);
        }

        private void OnChangeMasterVol(float v)
        {
            AudioListener.volume = v;
            PlayerPrefs.SetFloat("BrotherhoodVolume", v);
            if (masterValText != null) masterValText.text = Mathf.RoundToInt(v * 100) + "%";
        }

        private void OnChangeMusicVol(float v)
        {
            PlayerPrefs.SetFloat("BrotherhoodMusicVol", v);
            if (musicValText != null) musicValText.text = Mathf.RoundToInt(v * 100) + "%";
        }

        private void OnChangeSfxVol(float v)
        {
            PlayerPrefs.SetFloat("BrotherhoodSfxVol", v);
            if (sfxValText != null) sfxValText.text = Mathf.RoundToInt(v * 100) + "%";
        }

        private void ToggleShake()
        {
            bool on = PlayerPrefs.GetInt("BrotherhoodShake", 1) == 1;
            on = !on;
            PlayerPrefs.SetInt("BrotherhoodShake", on ? 1 : 0);
            if (shakeToggleText != null)
            {
                shakeToggleText.text = on ? "ON" : "OFF";
                shakeToggleText.color = on ? new Color(0.3f, 0.85f, 0.4f) : new Color(0.85f, 0.3f, 0.3f);
            }
            game?.Sfx("INVENTORY_EQUIP");
        }

        private void ToggleHaptics()
        {
            bool on = PlayerPrefs.GetInt("BrotherhoodHaptics", 1) == 1;
            on = !on;
            PlayerPrefs.SetInt("BrotherhoodHaptics", on ? 1 : 0);
            if (hapticsToggleText != null)
            {
                hapticsToggleText.text = on ? "ON" : "OFF";
                hapticsToggleText.color = on ? new Color(0.3f, 0.85f, 0.4f) : new Color(0.85f, 0.3f, 0.3f);
            }
            game?.Sfx("INVENTORY_EQUIP");
        }

        private void ToggleJoystickMode()
        {
            bool fixedMode = PlayerPrefs.GetInt("BrotherhoodFixedJoystick", 0) == 1;
            fixedMode = !fixedMode;
            PlayerPrefs.SetInt("BrotherhoodFixedJoystick", fixedMode ? 1 : 0);
            if (joystickToggleText != null)
            {
                joystickToggleText.text = fixedMode ? "FIXED" : "FLOATING";
            }
            game?.Sfx("INVENTORY_EQUIP");
        }

        private void OnChangeTouchScale(float v)
        {
            PlayerPrefs.SetFloat("BrotherhoodTouchScale", v);
            if (touchScaleValText != null) touchScaleValText.text = Mathf.RoundToInt(v * 100) + "%";
        }

        private void OnClickRemap()
        {
            Hide();
            controls?.EnterRemap();
        }

        private void OnExitToMainMenu()
        {
            Hide();
            game?.SaveGame();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        private Slider MakeSlider(Transform parent, string name, Vector2 a, Vector2 pos, Vector2 size, Action<float> onChange)
        {
            var sliderObj = MakePanel(parent, name, a, a, pos, size, new Color(0.1f, 0.08f, 0.1f, 0.9f));
            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;

            var fill = MakePanel(sliderObj.transform, "Fill", new Vector2(0, 0), new Vector2(1, 1), Vector2.zero, Vector2.zero, new Color(0.85f, 0.65f, 0.25f));
            slider.fillRect = fill.GetComponent<RectTransform>();

            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return slider;
        }

        private Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (spriteCache.TryGetValue(path, out var s)) return s;

            var sp = Resources.Load<Sprite>(path);
            if (sp != null)
            {
                spriteCache[path] = sp;
                return sp;
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
                spriteCache[path] = s;
                return s;
            }
            return null;
        }

        private GameObject MakePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private GameObject MakeButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
        {
            var go = MakePanel(parent, name, anchor, anchor, pos, size, Color.white);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            return go;
        }

        private Text MakeLabel(Transform parent, string text, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, int fontSize, TextAnchor alignment, Color color, bool isBold = true, bool wrap = false)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var txt = go.GetComponent<Text>();
            if (titleFont == null) LoadFonts();
            txt.font = isBold ? (titleFont ?? bodyFont) : (bodyFont ?? titleFont);
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.raycastTarget = false;
            txt.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            txt.verticalOverflow = wrap ? VerticalWrapMode.Overflow : VerticalWrapMode.Overflow;
            if (wrap) txt.lineSpacing = 1.2f;
            return txt;
        }
    }
}
