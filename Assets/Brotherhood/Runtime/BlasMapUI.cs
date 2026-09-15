using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Brotherhood
{
    public sealed class BlasMapUI : MonoBehaviour, IDragHandler, IPointerDownHandler
    {
        public BrotherhoodGame game;
        public TouchControls controls;
        public BlasOptionsUI optionsUI;

        private GameObject root;
        private RectTransform mapContent;
        private Text percentText;
        private Text cherubText;
        private Text zoneTitleText;
        private Text zoneSubText;

        private RectTransform playerMarker;
        private readonly List<GameObject> pinObjects = new List<GameObject>();
        private readonly Image[] pinSelectorImages = new Image[8];
        private int selectedPinType = -1; // -1 means none selected

        private int zoomLevel = 1; // 0: Z0, 1: Z1, 2: Z2
        private static readonly float[] ZoomScales = new float[] { 0.7f, 1.0f, 1.4f };

        private Font titleFont;
        private Font bodyFont;
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static readonly string[] MarkerNames = new string[]
        {
            "map-marker-cherub", "map-marker-npc", "map-marker-blue", "map-marker-chest",
            "map-marker-enemy", "map-marker-green", "map-marker-question", "map-marker-red"
        };

        public bool IsOpen => root != null && root.activeSelf;
        public Canvas Canvas { get; private set; }

        void Awake()
        {
            LoadFonts();
        }

        private void LoadFonts()
        {
            titleFont = Resources.Load<Font>("Fonts/Caudex-Bold");
            bodyFont  = Resources.Load<Font>("Fonts/Caudex-Regular");
            if (titleFont == null) titleFont = Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");
            if (bodyFont  == null) bodyFont  = titleFont;
            if (titleFont == null) titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bodyFont  == null) bodyFont  = titleFont;
        }

        public void Initialize(BrotherhoodGame g, TouchControls c, BlasOptionsUI opt)
        {
            game = g;
            controls = c;
            optionsUI = opt;
            BuildUI();
        }

        public void Show()
        {
            if (root == null) BuildUI();
            root.SetActive(true);
            Time.timeScale = 0f;
            RefreshMap();
            Recenter();
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

            var canvasObj = new GameObject("BlasMapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            Canvas = canvasObj.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 24;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f;

            // Fullscreen root
            root = MakePanel(canvasObj.transform, "MapRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);

            // FondoMapa background
            var bgObj = MakePanel(root.transform, "FondoMapa", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = LoadSprite("UI/FondoMapa");
            bgImg.type = Image.Type.Simple;
            bgImg.color = new Color(0.85f, 0.85f, 0.85f, 0.95f);

            // Dark map viewport area
            var viewport = MakePanel(root.transform, "MapViewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(0.04f, 0.04f, 0.05f, 0.65f));
            var mask = viewport.AddComponent<RectMask2D>();

            // Draggable Content root
            var contentObj = new GameObject("MapContent", typeof(RectTransform));
            contentObj.transform.SetParent(viewport.transform, false);
            mapContent = contentObj.GetComponent<RectTransform>();
            mapContent.anchorMin = mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            mapContent.sizeDelta = new Vector2(3000, 2000);
            mapContent.anchoredPosition = Vector2.zero;

            // Build Room Cells
            BuildRoomGrid();

            // Authentic [X] Close Button (Top-Right)
            var closeBtn = MakeButton(root.transform, "CloseBtn", new Vector2(1, 1), new Vector2(-75, -42f), new Vector2(110f, 66f), Hide);
            var cbImg = closeBtn.GetComponent<Image>();
            cbImg.sprite = LoadSprite("UI/Sprites/X");
            cbImg.color = Color.white;
            cbImg.preserveAspect = true;

            // Standalone Map Header Panel (Top of map)
            var header = MakePanel(root.transform, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -42f), new Vector2(0, 68f), new Color(0.04f, 0.03f, 0.04f, 0.92f));
            var headerOutline = header.AddComponent<Outline>();
            headerOutline.effectColor = new Color(0.7f, 0.5f, 0.2f, 0.8f);
            headerOutline.effectDistance = new Vector2(0, -2);

            // Discovery %
            var discIcon = MakePanel(header.transform, "DiscIcon", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(80, 0), new Vector2(32, 32), Color.white).GetComponent<Image>();
            discIcon.sprite = LoadSprite("UI/Sprites/map-discovery-icon");
            discIcon.preserveAspect = true;
            percentText = MakeLabel(header.transform, "1%", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(130, 0), new Vector2(80, 32), 22, TextAnchor.MiddleLeft, new Color(1f, 0.88f, 0.45f));

            // Cherubs Count
            var cherubIcon = MakePanel(header.transform, "CherubIcon", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(230, 0), new Vector2(30, 30), Color.white).GetComponent<Image>();
            cherubIcon.sprite = LoadSprite("UI/Sprites/map-marker-cherub");
            cherubIcon.preserveAspect = true;
            cherubText = MakeLabel(header.transform, "0 / 38", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(300, 0), new Vector2(120, 32), 22, TextAnchor.MiddleLeft, new Color(0.9f, 0.85f, 0.75f));

            // Zone Title (Center)
            zoneTitleText = MakeLabel(header.transform, "SUBURBS", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 10), new Vector2(500, 30), 22, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.4f));
            zoneSubText = MakeLabel(header.transform, "Brotherhood of the Silent Sorrow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(500, 22), 15, TextAnchor.MiddleCenter, new Color(0.75f, 0.7f, 0.65f));

            // Right-Side Controls (+, -, Recenter, Options Gear)
            var controlsPanel = MakePanel(root.transform, "SideControls", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-70, 0), new Vector2(70, 300), Color.clear);

            // Zoom In (+) — uses Boton_Mas.png
            var zoomInBtn = MakeButton(controlsPanel.transform, "ZoomIn", new Vector2(0.5f, 1), new Vector2(0, -35), new Vector2(64, 64), () => ChangeZoom(1));
            var ziImg = zoomInBtn.GetComponent<Image>(); ziImg.color = Color.white;
            var ziSp = LoadSprite("UI/Sprites/Boton_Mas");
            if (ziSp != null) { ziImg.sprite = ziSp; ziImg.preserveAspect = true; }
            else { ziImg.color = new Color(0.12f, 0.1f, 0.12f, 0.9f); zoomInBtn.AddComponent<Outline>().effectColor = new Color(0.7f,0.5f,0.2f); MakeLabel(zoomInBtn.transform,"+",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,30,TextAnchor.MiddleCenter,new Color(1f,0.85f,0.45f)); }

            // Zoom Out (-) — uses Boton_Menos.png
            var zoomOutBtn = MakeButton(controlsPanel.transform, "ZoomOut", new Vector2(0.5f, 1), new Vector2(0, -110), new Vector2(64, 64), () => ChangeZoom(-1));
            var zoImg = zoomOutBtn.GetComponent<Image>(); zoImg.color = Color.white;
            var zoSp = LoadSprite("UI/Sprites/Boton_Menos");
            if (zoSp != null) { zoImg.sprite = zoSp; zoImg.preserveAspect = true; }
            else { zoImg.color = new Color(0.12f, 0.1f, 0.12f, 0.9f); zoomOutBtn.AddComponent<Outline>().effectColor = new Color(0.7f,0.5f,0.2f); MakeLabel(zoomOutBtn.transform,"-",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,34,TextAnchor.MiddleCenter,new Color(1f,0.85f,0.45f)); }

            // Recenter — uses Boton_Centrar.png
            var centerBtn = MakeButton(controlsPanel.transform, "Recenter", new Vector2(0.5f, 1), new Vector2(0, -185), new Vector2(64, 64), Recenter);
            var ctImg = centerBtn.GetComponent<Image>(); ctImg.color = Color.white;
            var ctSp = LoadSprite("UI/Sprites/Boton_Centrar");
            if (ctSp != null) { ctImg.sprite = ctSp; ctImg.preserveAspect = true; }
            else { ctImg.color = new Color(0.12f, 0.1f, 0.12f, 0.9f); centerBtn.AddComponent<Outline>().effectColor = new Color(0.7f,0.5f,0.2f); MakeLabel(centerBtn.transform,"@",Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero,24,TextAnchor.MiddleCenter,new Color(1f,0.85f,0.45f)); }

            // Options Gear Button
            var optBtn = MakeButton(controlsPanel.transform, "OptionsBtn", new Vector2(0.5f, 1), new Vector2(0, -258), new Vector2(56, 56), OpenOptions);
            optBtn.GetComponent<Image>().color = new Color(0.12f, 0.1f, 0.12f, 0.9f);
            optBtn.AddComponent<Outline>().effectColor = new Color(0.7f, 0.5f, 0.2f);
            MakeLabel(optBtn.transform, "\u2699", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 26, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.45f));

            // Bottom 8 Pin Bar — uses Canvas_08.png pill background
            var pinBar = MakePanel(root.transform, "PinBar", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 45), new Vector2(720, 80), Color.white);
            var pinBarImg = pinBar.GetComponent<Image>();
            var pillSp = LoadSprite("UI/Sprites/Canvas_08");
            if (pillSp != null) { pinBarImg.sprite = pillSp; pinBarImg.type = Image.Type.Sliced; pinBarImg.color = new Color(1f, 1f, 1f, 0.97f); }
            else { pinBarImg.color = new Color(0.04f, 0.03f, 0.04f, 0.95f); var pinOutline2 = pinBar.AddComponent<Outline>(); pinOutline2.effectColor = new Color(0.7f, 0.5f, 0.2f, 0.8f); pinOutline2.effectDistance = new Vector2(0, 2); }


            for (int i = 0; i < 8; i++)
            {
                int pinIdx = i;
                float x = -300 + i * 85;
                var pBtn = MakeButton(pinBar.transform, "Pin_" + i, new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(56, 56), () => SelectPinTool(pinIdx));
                pBtn.GetComponent<Image>().color = new Color(0.15f, 0.12f, 0.15f, 0.9f);
                var pBorder = pBtn.AddComponent<Outline>();
                pBorder.effectColor = new Color(0.45f, 0.35f, 0.2f);
                pBorder.effectDistance = new Vector2(1, -1);
                pinSelectorImages[i] = pBorder.GetComponent<Image>();

                var icon = MakePanel(pBtn.transform, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36), Color.white).GetComponent<Image>();
                icon.sprite = LoadSprite("UI/Sprites/" + MarkerNames[i]);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            root.SetActive(false);
        }

        private void BuildRoomGrid()
        {
            // Room layout representing the 5 rooms of the Brotherhood chapter:
            // 0: Awakening (-180, 0)
            // 1: Hallway (-60, 0)
            // 2: Warden boss room (60, 0)
            // 3: Prie Dieu shrine (180, 0)
            // 4: Passage to Holy Line (300, 0)

            Vector2[] roomOffsets = new Vector2[]
            {
                new Vector2(-240, 0),
                new Vector2(-80, 0),
                new Vector2(80, 0),
                new Vector2(240, 0),
                new Vector2(400, 0)
            };

            for (int i = 0; i < roomOffsets.Length; i++)
            {
                int rIdx = i;
                var roomBox = MakePanel(mapContent, "Room_" + i, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), roomOffsets[i], new Vector2(140, 90), new Color(0.12f, 0.16f, 0.22f, 0.9f));
                var rOutline = roomBox.AddComponent<Outline>();
                rOutline.effectColor = new Color(0.35f, 0.65f, 0.85f, 0.8f);
                rOutline.effectDistance = new Vector2(2, -2);

                // Room block sprite
                var rImg = roomBox.GetComponent<Image>();
                var blockSp = LoadSprite("UI/Sprites/Z1-DDDD");
                if (blockSp != null) { rImg.sprite = blockSp; rImg.color = new Color(0.5f, 0.75f, 0.95f, 0.8f); }

                // Prie Dieu icon in Room 3
                if (i == 3)
                {
                    var altar = MakePanel(roomBox.transform, "Altar", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(36, 36), Color.white).GetComponent<Image>();
                    altar.sprite = LoadSprite("UI/Sprites/map-priedieu");
                    altar.preserveAspect = true;
                    altar.raycastTarget = false;
                }

                // Click to place/remove pin
                var btn = roomBox.AddComponent<Button>();
                btn.onClick.AddListener(() => OnClickRoom(rIdx, roomOffsets[rIdx]));
            }

            // Player Marker
            var pObj = MakePanel(mapContent, "PlayerMarker", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), roomOffsets[0], new Vector2(38, 38), Color.white);
            playerMarker = pObj.GetComponent<RectTransform>();
            var pImg = pObj.GetComponent<Image>();
            pImg.sprite = LoadSprite("UI/Sprites/map-cursor");
            pImg.preserveAspect = true;
            pImg.raycastTarget = false;
            var pGlow = pObj.AddComponent<Outline>();
            pGlow.effectColor = new Color(1f, 0.9f, 0.3f, 0.9f);
            pGlow.effectDistance = new Vector2(2, -2);
        }

        private void RefreshMap()
        {
            if (percentText != null && game != null && game.progress != null)
                percentText.text = Mathf.FloorToInt(game.progress.mapPercentage * 100) + "%";

            if (cherubText != null && game != null && game.progress != null)
                cherubText.text = game.progress.cherubsFreed + " / 38";

            // Update player marker to active room
            if (playerMarker != null && game != null && game.Current != null)
            {
                int rIdx = 0;
                string rId = game.Current.id;
                if (rId.EndsWith("02")) rIdx = 1;
                else if (rId.EndsWith("03")) rIdx = 2;
                else if (rId.EndsWith("04")) rIdx = 3;
                else if (rId.EndsWith("05")) rIdx = 4;

                Vector2[] roomOffsets = new Vector2[]
                {
                    new Vector2(-240, 0), new Vector2(-80, 0), new Vector2(80, 0), new Vector2(240, 0), new Vector2(400, 0)
                };
                playerMarker.anchoredPosition = roomOffsets[Mathf.Clamp(rIdx, 0, roomOffsets.Length - 1)];
            }

            RefreshSavedPins();
        }

        private void RefreshSavedPins()
        {
            foreach (var go in pinObjects) Destroy(go);
            pinObjects.Clear();

            if (game == null || game.progress == null || game.progress.mapPins == null) return;

            foreach (var pin in game.progress.mapPins)
            {
                if (pin.pinType >= 0 && pin.pinType < MarkerNames.Length)
                {
                    var pObj = MakePanel(mapContent, "Pin", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(pin.x, pin.y), new Vector2(30, 30), Color.white);
                    var img = pObj.GetComponent<Image>();
                    img.sprite = LoadSprite("UI/Sprites/" + MarkerNames[pin.pinType]);
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    pinObjects.Add(pObj);
                }
            }
        }

        private void SelectPinTool(int idx)
        {
            if (selectedPinType == idx) selectedPinType = -1; // toggle off
            else selectedPinType = idx;

            for (int i = 0; i < pinSelectorImages.Length; i++)
            {
                if (pinSelectorImages[i] != null)
                    pinSelectorImages[i].color = (i == selectedPinType) ? new Color(0.95f, 0.75f, 0.25f, 0.9f) : new Color(0.15f, 0.12f, 0.15f, 0.9f);
            }
            game?.Sfx("INVENTORY_SCROLL");
        }

        private void OnClickRoom(int roomIdx, Vector2 roomPos)
        {
            if (selectedPinType < 0 || game == null || game.progress == null) return;

            // Check if a pin already exists here; if so, remove it
            int existIdx = game.progress.mapPins.FindIndex(p => Vector2.Distance(new Vector2(p.x, p.y), roomPos) < 40f);
            if (existIdx >= 0)
            {
                game.progress.mapPins.RemoveAt(existIdx);
                game.Sfx("INVENTORY_EQUIP");
            }
            else
            {
                var newPin = new PlayerProgress.MapPinSaveData
                {
                    pinType = selectedPinType,
                    x = roomPos.x,
                    y = roomPos.y,
                    roomId = "Room_" + roomIdx
                };
                game.progress.mapPins.Add(newPin);
                game.Sfx("INVENTORY_EQUIP");
            }
            RefreshSavedPins();
        }

        private void ChangeZoom(int delta)
        {
            zoomLevel = Mathf.Clamp(zoomLevel + delta, 0, ZoomScales.Length - 1);
            if (mapContent != null)
                mapContent.localScale = Vector3.one * ZoomScales[zoomLevel];
            game?.Sfx("INVENTORY_SCROLL");
        }

        public void Recenter()
        {
            if (mapContent != null && playerMarker != null)
            {
                mapContent.anchoredPosition = -playerMarker.anchoredPosition * ZoomScales[zoomLevel];
            }
        }

        private void OpenOptions()
        {
            Hide();
            if (optionsUI != null) optionsUI.Show();
        }

        public void OnPointerDown(PointerEventData eventData) {}

        public void OnDrag(PointerEventData eventData)
        {
            if (mapContent != null)
            {
                mapContent.anchoredPosition += eventData.delta;
            }
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
