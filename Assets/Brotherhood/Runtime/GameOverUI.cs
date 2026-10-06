using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Brotherhood
{
    /// <summary>
    /// Death card using the original mobile game's complete title artwork.
    /// </summary>
    public sealed class GameOverUI : MonoBehaviour
    {
        private const float FadeInDuration = 0.18f;
        private const float CardDelay = 0.22f;

        private BrotherhoodGame game;
        private GameObject root;
        private CanvasGroup backdropGroup;
        private CanvasGroup cardGroup;
        private float elapsed;

        public bool IsShowing => root != null && root.activeSelf;

        public void Initialize(BrotherhoodGame owner)
        {
            game = owner;
            if (root != null) return;

            var canvasObject = new GameObject(
                "Game Over Canvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);

            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Gameplay options uses order 120 and must appear above this card.
            canvas.sortingOrder = 110;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            root = MakeRect(canvasObject.transform, "Game Over", Vector2.zero, Vector2.one);
            root.AddComponent<CanvasGroup>();

            var backdrop = root.GetComponent<Image>();
            backdrop.color = Color.black;
            // This is deliberately an input blocker so the living HUD cannot
            // receive a final touch while the death card is onscreen.
            backdrop.raycastTarget = true;
            backdropGroup = root.GetComponent<CanvasGroup>();

            var card = MakeRect(root.transform, "Mourning Card", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            var cardRect = card.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(520, 390);
            card.GetComponent<Image>().color = Color.clear;
            card.GetComponent<Image>().raycastTarget = false;
            cardGroup = card.AddComponent<CanvasGroup>();

            // `death-sreen-title.png` is the original mobile game's complete
            // Game Over artwork: tomb, sword and title in its authored red
            // pixel-art treatment.  Keep it as a single sprite so the source
            // composition is not rebuilt or approximated at runtime.
            var titleArt = MakeImage(card.transform, "Original Game Over title", LoadOriginalTitleSprite());
            titleArt.rectTransform.anchorMin = titleArt.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            titleArt.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            titleArt.rectTransform.anchoredPosition = new Vector2(0, 65);
            titleArt.rectTransform.sizeDelta = new Vector2(512, 256);
            titleArt.preserveAspect = true;
            titleArt.raycastTarget = false;

            MakeButton(card.transform,"HỒI SINH",new Vector2(-166,-146),Respawn);
            MakeButton(card.transform,"HOME",new Vector2(0,-146),Home);
            MakeButton(card.transform,"SETTINGS",new Vector2(166,-146),Settings);

            root.SetActive(false);
        }

        public void Show()
        {
            if (root == null || IsShowing) return;

            elapsed = 0f;
            backdropGroup.alpha = 0f;
            cardGroup.alpha = 0f;
            root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
        }

        private void Update()
        {
            if (!IsShowing) return;

            elapsed += Time.unscaledDeltaTime;
            backdropGroup.alpha = Mathf.Clamp01(elapsed / FadeInDuration);
            cardGroup.alpha = Mathf.Clamp01((elapsed - CardDelay) / FadeInDuration);

        }

        private void Respawn() { game?.Respawn(); }

        private void Home()
        {
            Hide();
            Time.timeScale=1f;
            SceneManager.LoadScene("MainMenu");
        }

        private void Settings()
        {
            if(game != null && game.controls != null && game.controls.optionsUI != null)
                game.controls.optionsUI.Show();
        }

        private static void MakeButton(Transform parent,string title,Vector2 position,System.Action action)
        {
            var buttonObject=new GameObject(title,typeof(RectTransform),typeof(Image),typeof(Button));
            var rect=buttonObject.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(.5f,.5f);rect.pivot=new Vector2(.5f,.5f);
            rect.anchoredPosition=position;rect.sizeDelta=new Vector2(160,54);
            var image=buttonObject.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("Menu/ButtonBase");
            image.type=Image.Type.Simple;image.color=Color.white;
            var button=buttonObject.GetComponent<Button>();button.transition=Selectable.Transition.ColorTint;
            button.onClick.AddListener(()=>action());
            var labelObject=new GameObject("Label",typeof(RectTransform),typeof(Text));
            var labelRect=labelObject.GetComponent<RectTransform>();labelRect.SetParent(rect,false);
            labelRect.anchorMin=Vector2.zero;labelRect.anchorMax=Vector2.one;
            labelRect.offsetMin=labelRect.offsetMax=Vector2.zero;
            var label=labelObject.GetComponent<Text>();label.text=title;
            label.font=Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");
            if(label.font==null)label.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize=20;label.alignment=TextAnchor.MiddleCenter;
            label.color=new Color(.94f,.72f,.42f);label.raycastTarget=false;
        }

        private static GameObject MakeRect(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rect = item.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return item;
        }

        private static Image MakeImage(Transform parent, string name, Sprite sprite)
        {
            var item = new GameObject(name, typeof(RectTransform), typeof(Image));
            item.transform.SetParent(parent, false);
            var image = item.GetComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            return image;
        }

        private static Sprite LoadOriginalTitleSprite()
        {
            var sprite = Resources.Load<Sprite>("Menu/GameOverTitle");
            if (sprite != null) return sprite;

            var texture = Resources.Load<Texture2D>("Menu/GameOverTitle");
            if (texture == null) return null;
            texture.filterMode = FilterMode.Point;
            return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 32f);
        }
    }
}
