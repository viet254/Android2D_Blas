using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Brotherhood
{
    /// <summary>Warden victory, using the original boss-title, button and AC01 art.</summary>
    public sealed class BossDefeatedUI : MonoBehaviour
    {
        BrotherhoodGame game;
        GameObject root, victoryPage, achievementPage;
        Sprite runtimeSprite;

        public bool IsShowing => root != null && root.activeSelf;
        public bool IsAchievementPage => achievementPage != null && achievementPage.activeSelf;

        public void Initialize(BrotherhoodGame owner)
        {
            game = owner;
            if (root != null) return;
            var canvasObject = new GameObject("Boss Defeated Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110;
            canvas.pixelPerfect = true;
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = .5f;

            root = Rect(canvasObject.transform, "Original Requiem Aeternam", Vector2.zero, Vector2.one);
            var shade = root.AddComponent<Image>();
            shade.color = new Color(0, 0, 0, .76f);
            shade.raycastTarget = true;

            victoryPage = Rect(root.transform, "Victory navigation", Vector2.zero, Vector2.one);
            var title = Image(victoryPage.transform, "boss-defeated-screen-title.png", new Vector2(0, 45), new Vector2(656, 404), LoadSourceSprite("Menu/BossDefeatedTitle"));
            title.preserveAspect = true;
            Button(victoryPage.transform, "CONTINUE", new Vector2(-210, -255), Continue);
            Button(victoryPage.transform, "HOME", new Vector2(0, -255), Home);
            Button(victoryPage.transform, "ACHIEVEMENTS", new Vector2(210, -255), Achievements);

            achievementPage = Rect(root.transform, "Achievements screen", Vector2.zero, Vector2.one);
            Image(achievementPage.transform, "achievements-bg-unlocked", new Vector2(0, 35), new Vector2(726, 114), Resources.Load<Sprite>("Achievements/achievements-bg-unlocked"));
            Image(achievementPage.transform, "achievements-AC01", new Vector2(-290, 35), new Vector2(82, 82), Resources.Load<Sprite>("Achievements/achievements-AC01"));
            Label(achievementPage.transform, "A LONG PATH AHEAD", new Vector2(60, 55), new Vector2(540, 40), 24);
            Label(achievementPage.transform, "Defeat the Warden of Silent Sorrow.", new Vector2(60, 17), new Vector2(540, 34), 18);
            Button(achievementPage.transform, "BACK", new Vector2(0, -255), Back);
            achievementPage.SetActive(false);
            root.SetActive(false);
        }

        public void Show()
        {
            if (root == null) return;
            victoryPage.SetActive(true);
            achievementPage.SetActive(false);
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            if (achievementPage != null) achievementPage.SetActive(false);
            if (victoryPage != null) victoryPage.SetActive(true);
        }

        void Continue() { Hide(); }

        void Home()
        {
            if (game != null) game.SaveGame();
            Hide();
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainMenu");
        }

        void Achievements()
        {
            victoryPage.SetActive(false);
            achievementPage.SetActive(true);
        }

        void Back()
        {
            achievementPage.SetActive(false);
            victoryPage.SetActive(true);
        }

        static GameObject Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            return go;
        }

        static Image Image(Transform parent, string name, Vector2 position, Vector2 size, Sprite sprite)
        {
            var go = Rect(parent, name, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var image = go.AddComponent<Image>(); image.sprite = sprite;
            image.preserveAspect = true; image.raycastTarget = false;
            return image;
        }

        static void Label(Transform parent, string value, Vector2 position, Vector2 size, int fontSize)
        {
            var go = Rect(parent, value, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position; rect.sizeDelta = size;
            var label = go.AddComponent<Text>();
            label.text = value;
            label.font = Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");
            if (label.font == null) label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = fontSize; label.alignment = TextAnchor.MiddleCenter;
            label.color = new Color(.94f, .72f, .42f); label.raycastTarget = false;
        }

        static void Button(Transform parent, string title, Vector2 position, System.Action action)
        {
            var go = Rect(parent, title, new Vector2(.5f, .5f), new Vector2(.5f, .5f));
            var rect = go.GetComponent<RectTransform>();
            rect.anchoredPosition = position; rect.sizeDelta = new Vector2(192, 54);
            var image = go.AddComponent<Image>();
            image.sprite = Resources.Load<Sprite>("Menu/ButtonBase");
            image.color = Color.white;
            var button = go.AddComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            button.onClick.AddListener(() => action());
            Label(go.transform, title, Vector2.zero, new Vector2(188, 50), 18);
        }

        Sprite LoadSourceSprite(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null) return sprite;
            var texture = Resources.Load<Texture2D>(path);
            if (texture == null) return null;
            texture.filterMode = FilterMode.Point;
            runtimeSprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100);
            return runtimeSprite;
        }

        void OnDestroy() { if (runtimeSprite != null) Destroy(runtimeSprite); }
    }
}
