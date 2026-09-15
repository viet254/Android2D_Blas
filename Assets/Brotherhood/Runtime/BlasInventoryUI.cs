using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Brotherhood
{
    public sealed class BlasInventoryUI : MonoBehaviour
    {
        public BrotherhoodGame game;
        public TouchControls controls;

        private GameObject root;
        private GameObject loreModal;

        private Text categoryTitleText;
        private Text itemNameText;
        private Text itemDescText;
        private Text loreTitleText;
        private Text loreSubtitleText;
        private Text loreBodyText;
        private Text equipBtnText;
        private Text pageText;
        private GameObject loreButton;
        private GameObject equipButton;
        private GameObject prevPageBtn;
        private GameObject nextPageBtn;

        private Image cursorImage;
        private Image leftGraphicImg;

        // Inventory Grid Slots (Up to 14 slots, 12 in 2x6 or 14 in 2x7)
        private const int MaxGridSlots = 14;
        private readonly GameObject[] slotObjects = new GameObject[MaxGridSlots];
        private readonly Image[] slotBoxImgs = new Image[MaxGridSlots];
        private readonly Image[] slotBgImgs = new Image[MaxGridSlots];
        private readonly Image[] slotIcons = new Image[MaxGridSlots];
        private readonly InventoryCatalog.Item[] currentItems = new InventoryCatalog.Item[MaxGridSlots];

        // Top 7 Tab Buttons + Close
        private readonly GameObject[] tabButtons = new GameObject[7];

        // Left-panel Equipped Slots:
        // Rosary (8 knots), Relics (3 slots), Prayers (1 slot), Sword Hearts (1 slot)
        private GameObject rosaryPanel;
        private readonly GameObject[] knotSlotObjects = new GameObject[8];
        private readonly Image[] knotSlotBoxImgs = new Image[8];
        private readonly Image[] knotSlotBgImgs = new Image[8];
        private readonly Image[] knotSlotIcons = new Image[8];

        private GameObject relicPanel;
        private readonly GameObject[] relicSlotObjects = new GameObject[3];
        private readonly Image[] relicSlotIcons = new Image[3];

        private GameObject prayerSlotObject;
        private Image prayerSlotIcon;

        private GameObject swordHeartSlotObject;
        private Image swordHeartSlotIcon;

        private InventoryCatalog catalog;
        private int currentTab = 0; // Default to Rosary Beads
        private int selectedSlot = -1;
        private int pageIndex = 0;
        private int totalPages = 1;
        private Font titleFont;
        private Font bodyFont;
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static readonly string[] Categories = {
            "rosarybead", "relic", "questitem", "sword", "prayer", "ability", "collectibleitem"
        };
        public static readonly string[] CategoryTitles = {
            "ROSARY BEADS", "RELICS", "QUEST ITEMS", "MEA CULPA HEARTS", "PRAYERS", "ABILITIES", "COLLECTIBLES"
        };
        static readonly string[] TabIcons = {
            "UI/Sprites/Item_RosaryBeads",
            "UI/Sprites/Item_Relics",
            "UI/Sprites/Item_QuestItems",
            "UI/Sprites/Item_MeaCulpa",
            "UI/Sprites/Item_Prayers",
            "UI/Sprites/Item_Abilities",
            "UI/Sprites/Item_Collectibles"
        };

        private static readonly Color PurpleSlotBg = new Color(0.337f, 0.157f, 0.275f, 1f);
        private static readonly Color DarkAubergine = new Color(0.045f, 0.025f, 0.035f, 1f);
        private static readonly Color GoldTitle = new Color(1f, 0.88f, 0.45f);
        private static readonly Color CreamText = new Color(0.88f, 0.82f, 0.65f);

        public bool IsOpen => root != null && root.activeSelf;
        public bool IsLoreOpen => loreModal != null && loreModal.activeSelf;
        public Canvas Canvas { get; private set; }

        public void InspectItem(string id)
        {
            if (catalog == null || catalog.items == null) return;
            var target = Array.Find(catalog.items, x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
            if (target == null) return;
            int catIdx = Array.FindIndex(Categories, c => string.Equals(c, target.category, StringComparison.OrdinalIgnoreCase));
            if (catIdx >= 0)
            {
                SwitchTab(catIdx);
                var list = GetItemsForCategory(target.category);
                int idx = list.FindIndex(x => string.Equals(x.id, id, StringComparison.OrdinalIgnoreCase));
                if (idx >= 0)
                {
                    int slotsPerPage = (catIdx == 2) ? 14 : 12;
                    pageIndex = idx / slotsPerPage;
                    selectedSlot = idx % slotsPerPage;
                    PopulateSlots();
                    UpdateDetails();
                }
            }
        }

        void Awake()
        {
            catalog = InventoryCatalog.Load();
            LoadFonts();
        }

        void LoadFonts()
        {
            titleFont = Resources.Load<Font>("Fonts/Caudex-Bold");
            bodyFont  = Resources.Load<Font>("Fonts/Caudex-Regular");
            if (titleFont == null) titleFont = Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");
            if (bodyFont  == null) bodyFont  = titleFont;
            if (titleFont == null) titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bodyFont  == null) bodyFont  = titleFont;
        }

        public void Initialize(BrotherhoodGame g, TouchControls c)
        {
            game = g;
            controls = c;
            BuildUI();
        }

        public void Toggle(int tab = 0)
        {
            if (IsOpen) Hide();
            else Show(tab);
        }

        public void Show(int tab = 0)
        {
            if (root == null) BuildUI();
            currentTab = Mathf.Clamp(tab, 0, Categories.Length - 1);
            // Tab 5 is Abilities
            selectedSlot = -1;
            pageIndex = 0;
            root.SetActive(true);
            if (loreModal != null) loreModal.SetActive(false);
            Time.timeScale = 0f;
            RefreshTab();
            game?.Sfx("INVENTORY_OPEN");
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            if (loreModal != null) loreModal.SetActive(false);
            Time.timeScale = 1f;
            game?.Sfx("INVENTORY_CLOSE");
        }

        void SwitchTab(int idx)
        {
            if (idx == currentTab) return;
            currentTab = Mathf.Clamp(idx, 0, Categories.Length - 1);
            selectedSlot = -1;
            pageIndex = 0;
            RefreshTab();
            game?.Sfx("INVENTORY_EQUIP");
        }

        List<InventoryCatalog.Item> GetItemsForCategory(string cat)
        {
            var list = new List<InventoryCatalog.Item>();
            if (catalog?.items != null)
            {
                foreach (var it in catalog.items)
                {
                    if (string.Equals(it.category, cat, StringComparison.OrdinalIgnoreCase))
                        list.Add(it);
                }
            }
            return list;
        }

        void RefreshTab()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = LoadSpriteRes(i == currentTab ? "UI/Sprites/Boton_01" : "UI/Sprites/Boton_02");
                    img.color = Color.white;
                }
            }
            if (categoryTitleText != null) categoryTitleText.text = CategoryTitles[currentTab];

            // Hide/Show Left Panels
            if (rosaryPanel != null) rosaryPanel.SetActive(currentTab == 0);
            if (relicPanel != null) relicPanel.SetActive(currentTab == 1);
            if (swordHeartSlotObject != null) swordHeartSlotObject.SetActive(currentTab == 3);
            if (prayerSlotObject != null) prayerSlotObject.SetActive(currentTab == 4);

            string bgPath = "";
            int cols = 6;
            float startX = 940f;
            Vector2 graphicPos = new Vector2(400f, -440f);
            Vector2 graphicSize = new Vector2(400f, 460f);

            if (currentTab == 0) // Rosary Beads
            {
                bgPath = ""; // Rosary panel handles its own knots
                cols = 6;
                startX = 940f;
            }
            else if (currentTab == 1) // Relics
            {
                bgPath = "UI/Sprites/inventory-bg-relicary";
                cols = 6;
                startX = 940f;
                graphicPos = new Vector2(390f, -440f);
                graphicSize = new Vector2(420f, 460f);
            }
            else if (currentTab == 2) // Quest Items (Full width 2x7 grid)
            {
                bgPath = "";
                cols = 7;
                startX = 360f;
            }
            else if (currentTab == 3) // Mea Culpa Hearts
            {
                bgPath = "";
                cols = 6;
                startX = 940f;
            }
            else if (currentTab == 4) // Prayers
            {
                bgPath = "UI/Sprites/inventory-bg-prayers";
                cols = 6;
                startX = 940f;
                graphicPos = new Vector2(390f, -440f);
                graphicSize = new Vector2(400f, 460f);
            }
            else if (currentTab == 5) // Abilities / Mea Culpa Skills
            {
                bgPath = "";
                cols = 6;
                startX = 940f;
                graphicPos = new Vector2(390f, -440f);
                graphicSize = new Vector2(400f, 400f);
            }
            else if (currentTab == 6) // Collectibles
            {
                bgPath = "UI/Sprites/inventory-bg-osary";
                cols = 6;
                startX = 940f;
                graphicPos = new Vector2(390f, -440f);
                graphicSize = new Vector2(520f, 440f);
            }

            if (leftGraphicImg != null)
            {
                if (!string.IsNullOrEmpty(bgPath))
                {
                    leftGraphicImg.sprite = LoadSpriteRes(bgPath);
                    leftGraphicImg.rectTransform.anchoredPosition = graphicPos;
                    leftGraphicImg.rectTransform.sizeDelta = graphicSize;
                    leftGraphicImg.gameObject.SetActive(leftGraphicImg.sprite != null);
                }
                else
                {
                    leftGraphicImg.gameObject.SetActive(false);
                }
            }

            // Position slots dynamically
            float slotGap = 120f;
            float row0Y = -270f, row1Y = -395f;
            int maxVisible = (cols == 7) ? 14 : 12;

            for (int i = 0; i < MaxGridSlots; i++)
            {
                if (slotObjects[i] == null) continue;
                if (i < maxVisible)
                {
                    int row = i / cols;
                    int col = i % cols;
                    var rt = slotObjects[i].GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(startX + col * slotGap, row == 0 ? row0Y : row1Y);
                    slotObjects[i].SetActive(true);
                }
                else
                {
                    slotObjects[i].SetActive(false);
                }
            }

            // Position page navigation buttons
            if (prevPageBtn != null && nextPageBtn != null && pageText != null)
            {
                float pY = -515f;
                float pCenterX = startX + ((cols - 1) * slotGap) * 0.5f;
                pageText.rectTransform.anchoredPosition = new Vector2(pCenterX, pY);
                prevPageBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(pCenterX - 110f, pY);
                nextPageBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(pCenterX + 110f, pY);
            }

            PopulateSlots();
            RefreshEquippedPanels();
            UpdateDetails();
        }

        void PopulateSlots()
        {
            string cat = Categories[currentTab];
            var allItems = GetItemsForCategory(cat);
            int slotsPerPage = (currentTab == 2) ? 14 : 12;
            totalPages = Mathf.Max(1, Mathf.CeilToInt(allItems.Count / (float)slotsPerPage));
            pageIndex = Mathf.Clamp(pageIndex, 0, totalPages - 1);

            if (pageText != null)
            {
                pageText.text = $"PAGE {pageIndex + 1} / {totalPages}";
                pageText.gameObject.SetActive(totalPages > 1);
            }
            if (prevPageBtn != null) prevPageBtn.SetActive(totalPages > 1 && pageIndex > 0);
            if (nextPageBtn != null) nextPageBtn.SetActive(totalPages > 1 && pageIndex < totalPages - 1);

            int startItemIdx = pageIndex * slotsPerPage;
            for (int i = 0; i < MaxGridSlots; i++)
            {
                currentItems[i] = null;
                if (i < slotsPerPage && (startItemIdx + i) < allItems.Count)
                {
                    var item = allItems[startItemIdx + i];
                    currentItems[i] = item;
                    if (slotBgImgs[i] != null)
                    {
                        slotBgImgs[i].color = PurpleSlotBg;
                        slotBgImgs[i].gameObject.SetActive(true);
                    }
                    if (slotIcons[i] != null)
                    {
                        var sp = LoadIconSprite(item.icon);
                        slotIcons[i].sprite = sp;
                        bool owned = game == null || game.progress.Owns(item.id);
                        slotIcons[i].color = owned ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.6f);
                        slotIcons[i].gameObject.SetActive(sp != null);
                    }
                }
                else
                {
                    if (slotBgImgs[i] != null) slotBgImgs[i].gameObject.SetActive(false);
                    if (slotIcons[i] != null) slotIcons[i].gameObject.SetActive(false);
                }
            }

            if (selectedSlot < 0 || selectedSlot >= slotsPerPage || currentItems[selectedSlot] == null)
            {
                selectedSlot = -1;
                for (int i = 0; i < slotsPerPage; i++)
                {
                    if (currentItems[i] != null)
                    {
                        selectedSlot = i;
                        break;
                    }
                }
            }

            UpdateCursor();
        }

        void ChangePage(int delta)
        {
            int next = Mathf.Clamp(pageIndex + delta, 0, totalPages - 1);
            if (next != pageIndex)
            {
                pageIndex = next;
                selectedSlot = 0;
                PopulateSlots();
                UpdateDetails();
                game?.Sfx("INVENTORY_EQUIP");
            }
        }

        void SelectSlot(int idx)
        {
            if (idx < 0 || idx >= MaxGridSlots || currentItems[idx] == null) return;
            selectedSlot = idx;
            UpdateCursor();
            UpdateDetails();
            game?.Sfx("INVENTORY_EQUIP");
        }

        void UpdateCursor()
        {
            if (cursorImage == null) return;
            if (selectedSlot >= 0 && selectedSlot < MaxGridSlots && slotObjects[selectedSlot] != null && currentItems[selectedSlot] != null)
            {
                cursorImage.transform.SetParent(slotObjects[selectedSlot].transform, false);
                var r = cursorImage.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = new Vector2(-4, -4);
                r.offsetMax = new Vector2(4, 4);
                cursorImage.gameObject.SetActive(true);
            }
            else
            {
                cursorImage.gameObject.SetActive(false);
            }
        }

        void RefreshEquippedPanels()
        {
            if (game == null) return;

            // 1. Rosary (8 knots)
            if (knotSlotObjects[0] != null)
            {
                var equipped = game.progress.equippedRosaryBeads ?? Array.Empty<string>();
                for (int i = 0; i < 8; i++)
                {
                    string beadId = i < equipped.Length ? equipped[i] : null;
                    var item = string.IsNullOrEmpty(beadId) ? null : Array.Find(catalog.items, x => x.id == beadId);
                    if (item != null)
                    {
                        knotSlotBgImgs[i].color = PurpleSlotBg;
                        knotSlotBgImgs[i].gameObject.SetActive(true);
                        knotSlotIcons[i].sprite = LoadIconSprite(item.icon);
                        knotSlotIcons[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        knotSlotBgImgs[i].gameObject.SetActive(false);
                        knotSlotIcons[i].gameObject.SetActive(false);
                    }
                }
            }

            // 2. Relics (3 slots)
            if (relicSlotObjects[0] != null)
            {
                var equipped = game.progress.equippedRelics ?? Array.Empty<string>();
                for (int i = 0; i < 3; i++)
                {
                    string relId = i < equipped.Length ? equipped[i] : null;
                    var item = string.IsNullOrEmpty(relId) ? null : Array.Find(catalog.items, x => x.id == relId);
                    if (item != null)
                    {
                        relicSlotIcons[i].sprite = LoadIconSprite(item.icon);
                        relicSlotIcons[i].gameObject.SetActive(true);
                    }
                    else
                    {
                        relicSlotIcons[i].gameObject.SetActive(false);
                    }
                }
            }

            // 3. Prayers (1 slot)
            if (prayerSlotIcon != null)
            {
                string pId = game.progress.equippedPrayer;
                var item = string.IsNullOrEmpty(pId) ? null : Array.Find(catalog.items, x => x.id == pId);
                if (item != null)
                {
                    prayerSlotIcon.sprite = LoadIconSprite(item.icon);
                    prayerSlotIcon.gameObject.SetActive(true);
                }
                else
                {
                    prayerSlotIcon.gameObject.SetActive(false);
                }
            }

            // 4. Mea Culpa Sword Heart (1 slot)
            if (swordHeartSlotIcon != null)
            {
                string hId = game.progress.equippedSwordHeart;
                var item = string.IsNullOrEmpty(hId) ? null : Array.Find(catalog.items, x => x.id == hId);
                if (item != null)
                {
                    swordHeartSlotIcon.sprite = LoadIconSprite(item.icon);
                    swordHeartSlotIcon.gameObject.SetActive(true);
                }
                else
                {
                    swordHeartSlotIcon.gameObject.SetActive(false);
                }
            }
        }

        void ClickKnotSlot(int idx)
        {
            if (game == null) return;
            var equipped = game.progress.equippedRosaryBeads ?? Array.Empty<string>();
            if (idx < equipped.Length)
            {
                string beadId = equipped[idx];
                var item = Array.Find(catalog.items, x => x.id == beadId);
                if (item != null) game.EquipInventoryItem(item);
            }
            else
            {
                if (selectedSlot >= 0 && selectedSlot < MaxGridSlots && currentItems[selectedSlot] != null)
                {
                    game.EquipInventoryItem(currentItems[selectedSlot]);
                }
            }
            RefreshEquippedPanels();
            PopulateSlots();
            UpdateDetails();
        }

        void ClickRelicSlot(int idx)
        {
            if (game == null) return;
            var equipped = game.progress.equippedRelics ?? Array.Empty<string>();
            if (idx < equipped.Length)
            {
                string relId = equipped[idx];
                var item = Array.Find(catalog.items, x => x.id == relId);
                if (item != null) game.EquipInventoryItem(item);
            }
            else if (selectedSlot >= 0 && selectedSlot < MaxGridSlots && currentItems[selectedSlot] != null)
            {
                game.EquipInventoryItem(currentItems[selectedSlot]);
            }
            RefreshEquippedPanels();
            PopulateSlots();
            UpdateDetails();
        }

        void ClickPrayerSlot()
        {
            if (game == null) return;
            if (!string.IsNullOrEmpty(game.progress.equippedPrayer))
            {
                var item = Array.Find(catalog.items, x => x.id == game.progress.equippedPrayer);
                if (item != null) game.EquipInventoryItem(item);
            }
            else if (selectedSlot >= 0 && selectedSlot < MaxGridSlots && currentItems[selectedSlot] != null)
            {
                game.EquipInventoryItem(currentItems[selectedSlot]);
            }
            RefreshEquippedPanels();
            PopulateSlots();
            UpdateDetails();
        }

        void ClickSwordHeartSlot()
        {
            if (game == null) return;
            if (!string.IsNullOrEmpty(game.progress.equippedSwordHeart))
            {
                var item = Array.Find(catalog.items, x => x.id == game.progress.equippedSwordHeart);
                if (item != null) game.EquipInventoryItem(item);
            }
            else if (selectedSlot >= 0 && selectedSlot < MaxGridSlots && currentItems[selectedSlot] != null)
            {
                game.EquipInventoryItem(currentItems[selectedSlot]);
            }
            RefreshEquippedPanels();
            PopulateSlots();
            UpdateDetails();
        }

        void UpdateDetails()
        {
            var item = selectedSlot >= 0 && selectedSlot < MaxGridSlots ? currentItems[selectedSlot] : null;
            if (itemNameText != null) itemNameText.text = item != null ? item.caption : "";
            if (itemDescText != null)
            {
                string desc = "";
                if (item != null)
                {
                    if (!string.IsNullOrEmpty(item.description)) desc = item.description;
                    else if (!string.IsNullOrEmpty(item.lore)) desc = item.lore.Length > 280 ? item.lore.Substring(0, 280) + "..." : item.lore;
                }
                itemDescText.text = desc;
            }
            bool hasLore    = item != null && !string.IsNullOrEmpty(item.lore);
            bool owned      = item != null && game != null && game.progress.Owns(item.id);
            bool equippable = owned && item != null && (item.category == "relic" || item.category == "prayer" || item.category == "sword" || item.category == "rosarybead" || item.category == "ability");
            if (loreButton  != null) loreButton.SetActive(hasLore);
            if (equipButton != null) equipButton.SetActive(equippable);
            if (equipBtnText != null && item != null)
            {
                if (item.category == "ability")
                    equipBtnText.text = game != null && game.progress.IsEquipped(item.id) ? "ACTIVE" : "UPGRADE";
                else
                    equipBtnText.text = game != null && game.progress.IsEquipped(item.id) ? "UNEQUIP" : "EQUIP";
            }
            if (currentTab == 5 && leftGraphicImg != null)
            {
                if (item != null && !string.IsNullOrEmpty(item.pictureGuid))
                {
                    leftGraphicImg.sprite = LoadSpriteRes(item.pictureGuid);
                    leftGraphicImg.rectTransform.anchoredPosition = new Vector2(400f, -440f);
                    leftGraphicImg.rectTransform.sizeDelta = new Vector2(380f, 380f);
                    leftGraphicImg.gameObject.SetActive(leftGraphicImg.sprite != null);
                }
                else
                {
                    leftGraphicImg.gameObject.SetActive(false);
                }
            }
        }

        public void ShowLoreModal()
        {
            var item = selectedSlot >= 0 && selectedSlot < MaxGridSlots ? currentItems[selectedSlot] : null;
            if (item == null && catalog?.items != null) item = Array.Find(catalog.items, x => x.id == "QI31");
            if (item == null || loreModal == null) return;
            if (loreTitleText    != null) loreTitleText.text    = item.caption;
            if (loreSubtitleText != null) loreSubtitleText.text = string.IsNullOrEmpty(item.subtitle) ? "Deosgracias' Farewell" : item.subtitle;
            if (loreBodyText     != null) loreBodyText.text     = string.IsNullOrEmpty(item.lore) ?
                "\"Brother Abbot, you know that I have been a scribe in this abbey since I was but a wee child...\"" : item.lore;
            loreModal.SetActive(true);
            game?.Sfx("INVENTORY_EQUIP");
        }

        void EquipSelected()
        {
            if (selectedSlot < 0 || selectedSlot >= MaxGridSlots) return;
            var item = currentItems[selectedSlot];
            if (item != null && game != null)
            {
                game.EquipInventoryItem(item);
                RefreshEquippedPanels();
                PopulateSlots();
                UpdateDetails();
            }
        }

        void BuildUI()
        {
            if (root != null) return;
            try
            {
                var canvasObj = new GameObject("BlasInventoryCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvasObj.transform.SetParent(transform, false);
                Canvas = canvasObj.GetComponent<Canvas>();
                Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                Canvas.sortingOrder = 25;
                var scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 1f;

                // FULLSCREEN Root dark burgundy background (#0b0609)
                root = MakeRect(canvasObj.transform, "InventoryRoot");
                var rootImg = root.AddComponent<Image>();
                rootImg.color = DarkAubergine;
                StretchFull(root.GetComponent<RectTransform>());

                // Outer border framing the entire screen (1920x1080)
                var win = MakeRect(root.transform, "MainWindow");
                var winRT = win.GetComponent<RectTransform>();
                StretchFull(winRT);

                // Top Tab Bar - Authentic 7 tabs + [X] Close button
                float tabW = 145f, tabH = 70f, tabSpacingX = 160f, tabStartX = 180f, tabY = -50f;
                for (int i = 0; i < 7; i++)
                {
                    int ti = i;
                    var tb = MakeRect(win.transform, "Tab_" + i);
                    var tbRT = tb.GetComponent<RectTransform>();
                    tbRT.anchorMin = new Vector2(0, 1);
                    tbRT.anchorMax = new Vector2(0, 1);
                    tbRT.pivot = new Vector2(0.5f, 0.5f);
                    tbRT.anchoredPosition = new Vector2(tabStartX + i * tabSpacingX, tabY);
                    tbRT.sizeDelta = new Vector2(tabW, tabH);
                    var tbImg = tb.AddComponent<Image>();
                    tbImg.sprite = LoadSpriteRes(i == currentTab ? "UI/Sprites/Boton_01" : "UI/Sprites/Boton_02");
                    tbImg.color = Color.white;
                    tabButtons[i] = tb;

                    var icon = MakeRect(tb.transform, "Icon");
                    var iconRT = icon.GetComponent<RectTransform>();
                    iconRT.anchorMin = new Vector2(0.5f, 0.5f);
                    iconRT.anchorMax = new Vector2(0.5f, 0.5f);
                    iconRT.pivot = new Vector2(0.5f, 0.5f);
                    iconRT.anchoredPosition = Vector2.zero;
                    iconRT.sizeDelta = new Vector2(46f, 46f);
                    var iconImg = icon.AddComponent<Image>();
                    iconImg.sprite = LoadSpriteRes(TabIcons[i]);
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                    AddClick(tb, () => SwitchTab(ti));
                }

                // Authentic [X] Close Button (Far Top-Right)
                var closeBtn = MakeRect(win.transform, "CloseBtn");
                var cRT = closeBtn.GetComponent<RectTransform>();
                cRT.anchorMin = new Vector2(1, 1);
                cRT.anchorMax = new Vector2(1, 1);
                cRT.pivot = new Vector2(1, 0.5f);
                cRT.anchoredPosition = new Vector2(-80f, tabY);
                cRT.sizeDelta = new Vector2(110f, 66f);
                var cImg = closeBtn.AddComponent<Image>();
                cImg.sprite = LoadSpriteRes("UI/Sprites/X");
                cImg.preserveAspect = true;
                AddClick(closeBtn, Hide);

                // Category Title (Centered under tab bar)
                var catTitle = MakeLabel(win.transform, CategoryTitles[currentTab], 36, titleFont, GoldTitle);
                var catRT = catTitle.GetComponent<RectTransform>();
                catRT.anchorMin = new Vector2(0.5f, 1);
                catRT.anchorMax = new Vector2(0.5f, 1);
                catRT.pivot = new Vector2(0.5f, 1f);
                catRT.anchoredPosition = new Vector2(0, -115f);
                catRT.sizeDelta = new Vector2(1200f, 48f);
                categoryTitleText = catTitle.GetComponent<Text>();
                categoryTitleText.alignment = TextAnchor.MiddleCenter;

                // Thin gold divider under category title
                var divTop = MakeRect(win.transform, "DivTop");
                var dtRT = divTop.GetComponent<RectTransform>();
                dtRT.anchorMin = new Vector2(0, 1);
                dtRT.anchorMax = new Vector2(1, 1);
                dtRT.pivot = new Vector2(0.5f, 0.5f);
                dtRT.anchoredPosition = new Vector2(0, -175f);
                dtRT.sizeDelta = new Vector2(-120f, 2f);
                divTop.AddComponent<Image>().color = new Color(0.6f, 0.42f, 0.17f, 0.75f);

                // Left Graphic (For prayers / relics / collectibles background illustrations)
                var lgo = MakeRect(win.transform, "LeftGraphic");
                var lrt = lgo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 1);
                lrt.anchorMax = new Vector2(0, 1);
                lrt.pivot = new Vector2(0.5f, 0.5f);
                lrt.anchoredPosition = new Vector2(390f, -440f);
                lrt.sizeDelta = new Vector2(400f, 460f);
                leftGraphicImg = lgo.AddComponent<Image>();
                leftGraphicImg.preserveAspect = true;
                leftGraphicImg.raycastTarget = false;
                leftGraphicImg.gameObject.SetActive(false);

                // Build Left Panels
                BuildRosaryPanel(win.transform);
                BuildRelicPanel(win.transform);
                BuildPrayerSlot(win.transform);
                BuildSwordHeartSlot(win.transform);

                // Slot Grid (14 slots instantiated, authentic 96x96 size)
                float slotSize = 96f;
                var slotBoxSprite = LoadSpriteRes("UI/Sprites/ItemSlot_Empty");
                if (slotBoxSprite == null) slotBoxSprite = LoadSpriteRes("UI/Sprites/Slot_01");

                for (int i = 0; i < MaxGridSlots; i++)
                {
                    int si = i;
                    var slot = MakeRect(win.transform, "Slot_" + i);
                    var slotRT = slot.GetComponent<RectTransform>();
                    slotRT.anchorMin = new Vector2(0, 1);
                    slotRT.anchorMax = new Vector2(0, 1);
                    slotRT.pivot = new Vector2(0.5f, 0.5f);
                    slotRT.sizeDelta = new Vector2(slotSize, slotSize);
                    slotObjects[i] = slot;

                    var slotImg = slot.AddComponent<Image>();
                    slotImg.sprite = slotBoxSprite;
                    slotImg.color = Color.white;
                    slotBoxImgs[i] = slotImg;
                    AddClick(slot, () => SelectSlot(si));

                    var bgGO = MakeRect(slot.transform, "SlotBg");
                    var bgRT = bgGO.GetComponent<RectTransform>();
                    bgRT.anchorMin = new Vector2(0.08f, 0.08f);
                    bgRT.anchorMax = new Vector2(0.92f, 0.92f);
                    bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
                    var bgImg = bgGO.AddComponent<Image>();
                    bgImg.color = PurpleSlotBg;
                    bgImg.raycastTarget = false;
                    bgGO.SetActive(false);
                    slotBgImgs[i] = bgImg;

                    var iconGO = MakeRect(slot.transform, "Icon");
                    var iRT = iconGO.GetComponent<RectTransform>();
                    iRT.anchorMin = new Vector2(0.12f, 0.12f);
                    iRT.anchorMax = new Vector2(0.88f, 0.88f);
                    iRT.offsetMin = iRT.offsetMax = Vector2.zero;
                    var iImg = iconGO.AddComponent<Image>();
                    iImg.preserveAspect = true;
                    iImg.raycastTarget = false;
                    iconGO.SetActive(false);
                    slotIcons[i] = iImg;
                }

                // Selection Cursor
                var cursorSprite = LoadSpriteRes("UI/Sprites/ItemCursorMobile 1_0");
                var cursorGO = MakeRect(win.transform, "SlotCursor");
                cursorImage = cursorGO.AddComponent<Image>();
                cursorImage.sprite = cursorSprite;
                cursorImage.raycastTarget = false;
                cursorGO.SetActive(false);

                // Page Navigation: Prev (<), Page text, Next (>)
                prevPageBtn = MakeRect(win.transform, "PrevPageBtn");
                var ppRT = prevPageBtn.GetComponent<RectTransform>();
                ppRT.anchorMin = new Vector2(0, 1);
                ppRT.anchorMax = new Vector2(0, 1);
                ppRT.pivot = new Vector2(0.5f, 0.5f);
                ppRT.sizeDelta = new Vector2(50f, 40f);
                prevPageBtn.AddComponent<Image>().sprite = LoadSpriteRes("UI/Sprites/Boton_04");
                prevPageBtn.GetComponent<Image>().type = Image.Type.Sliced;
                var ppLbl = MakeLabel(prevPageBtn.transform, "<", 24, titleFont, GoldTitle);
                StretchFull(ppLbl.GetComponent<RectTransform>());
                ppLbl.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                AddClick(prevPageBtn, () => ChangePage(-1));
                prevPageBtn.SetActive(false);

                var pageLblGO = MakeLabel(win.transform, "PAGE 1 / 1", 20, bodyFont, CreamText);
                var pRT = pageLblGO.GetComponent<RectTransform>();
                pRT.anchorMin = new Vector2(0, 1);
                pRT.anchorMax = new Vector2(0, 1);
                pRT.pivot = new Vector2(0.5f, 0.5f);
                pRT.sizeDelta = new Vector2(160f, 32f);
                pageText = pageLblGO.GetComponent<Text>();
                pageText.alignment = TextAnchor.MiddleCenter;
                pageLblGO.SetActive(false);

                nextPageBtn = MakeRect(win.transform, "NextPageBtn");
                var npRT = nextPageBtn.GetComponent<RectTransform>();
                npRT.anchorMin = new Vector2(0, 1);
                npRT.anchorMax = new Vector2(0, 1);
                npRT.pivot = new Vector2(0.5f, 0.5f);
                npRT.sizeDelta = new Vector2(50f, 40f);
                nextPageBtn.AddComponent<Image>().sprite = LoadSpriteRes("UI/Sprites/Boton_04");
                nextPageBtn.GetComponent<Image>().type = Image.Type.Sliced;
                var npLbl = MakeLabel(nextPageBtn.transform, ">", 24, titleFont, GoldTitle);
                StretchFull(npLbl.GetComponent<RectTransform>());
                npLbl.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                AddClick(nextPageBtn, () => ChangePage(1));
                nextPageBtn.SetActive(false);

                // Divider separating grid and description
                var divMid = MakeRect(win.transform, "DivMid");
                var dmRT = divMid.GetComponent<RectTransform>();
                dmRT.anchorMin = new Vector2(0, 1);
                dmRT.anchorMax = new Vector2(1, 1);
                dmRT.pivot = new Vector2(0.5f, 0.5f);
                dmRT.anchoredPosition = new Vector2(0, -560f);
                dmRT.sizeDelta = new Vector2(-120f, 2f);
                divMid.AddComponent<Image>().color = new Color(0.55f, 0.4f, 0.18f, 0.8f);

                // Item Name (Left-aligned, elegant gold)
                var nameGO = MakeLabel(win.transform, "", 32, titleFont, new Color(0.95f, 0.88f, 0.65f));
                var nRT = nameGO.GetComponent<RectTransform>();
                nRT.anchorMin = new Vector2(0, 1);
                nRT.anchorMax = new Vector2(1, 1);
                nRT.pivot = new Vector2(0, 1);
                nRT.anchoredPosition = new Vector2(120f, -590f);
                nRT.sizeDelta = new Vector2(-240f, 44f);
                itemNameText = nameGO.GetComponent<Text>();
                itemNameText.alignment = TextAnchor.MiddleLeft;

                // Description text (Clear warm cream font)
                var descGO = MakeLabel(win.transform, "", 24, bodyFont, CreamText);
                var dRT = descGO.GetComponent<RectTransform>();
                dRT.anchorMin = new Vector2(0, 1);
                dRT.anchorMax = new Vector2(1, 1);
                dRT.pivot = new Vector2(0, 1);
                dRT.anchoredPosition = new Vector2(120f, -640f);
                dRT.sizeDelta = new Vector2(-540f, 320f);
                itemDescText = descGO.GetComponent<Text>();
                itemDescText.alignment = TextAnchor.UpperLeft;
                itemDescText.horizontalOverflow = HorizontalWrapMode.Wrap;
                itemDescText.verticalOverflow = VerticalWrapMode.Overflow;

                // Lore button (Authentic Boton_Lore.png, 190x60)
                loreButton = MakeRect(win.transform, "LoreBtn");
                var lbRT = loreButton.GetComponent<RectTransform>();
                lbRT.anchorMin = new Vector2(1, 0);
                lbRT.anchorMax = new Vector2(1, 0);
                lbRT.pivot = new Vector2(1, 0);
                lbRT.anchoredPosition = new Vector2(-120f, 65f);
                lbRT.sizeDelta = new Vector2(190f, 60f);
                var lbImg = loreButton.AddComponent<Image>();
                lbImg.sprite = LoadSpriteRes("UI/Sprites/Boton_Lore");
                lbImg.preserveAspect = true;
                var lbl = MakeLabel(loreButton.transform, "Lore", 26, titleFont, new Color(0.95f, 0.85f, 0.55f));
                StretchFull(lbl.GetComponent<RectTransform>());
                lbl.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                AddClick(loreButton, ShowLoreModal);
                loreButton.SetActive(false);

                // Equip button (Left of Lore button)
                equipButton = MakeRect(win.transform, "EquipBtn");
                var ebRT = equipButton.GetComponent<RectTransform>();
                ebRT.anchorMin = new Vector2(1, 0);
                ebRT.anchorMax = new Vector2(1, 0);
                ebRT.pivot = new Vector2(1, 0);
                ebRT.anchoredPosition = new Vector2(-340f, 65f);
                ebRT.sizeDelta = new Vector2(190f, 60f);
                var ebImg = equipButton.AddComponent<Image>();
                ebImg.sprite = LoadSpriteRes("UI/Sprites/Boton_04");
                ebImg.type = Image.Type.Sliced;
                var elbGO = MakeLabel(equipButton.transform, "Equip", 26, titleFont, new Color(0.95f, 0.85f, 0.55f));
                StretchFull(elbGO.GetComponent<RectTransform>());
                elbGO.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
                equipBtnText = elbGO.GetComponent<Text>();
                AddClick(equipButton, EquipSelected);
                equipButton.SetActive(false);

                BuildLoreModal(canvasObj.transform);
            }
            catch (Exception ex)
            {
                Debug.LogError("Error building BlasInventoryUI: " + ex);
            }
            finally
            {
                if (root != null) root.SetActive(false); // ALWAYS CLOSED BY DEFAULT!
            }
        }

        void BuildRosaryPanel(Transform parent)
        {
            rosaryPanel = MakeRect(parent, "RosaryPanel");
            var rRT = rosaryPanel.GetComponent<RectTransform>();
            rRT.anchorMin = new Vector2(0, 1);
            rRT.anchorMax = new Vector2(0, 1);
            rRT.pivot = new Vector2(0.5f, 0.5f);
            rRT.anchoredPosition = new Vector2(380f, -380f);
            rRT.sizeDelta = new Vector2(360f, 440f);

            var knotFrameSprite = LoadSpriteRes("UI/Sprites/ItemSlot_Knot");
            if (knotFrameSprite == null) knotFrameSprite = LoadSpriteRes("UI/Sprites/Slot_01");

            float kSize = 90f, gapX = 130f, gapY = 105f;
            float startX = -gapX * 0.5f;
            float startY = gapY * 1.5f;

            for (int i = 0; i < 8; i++)
            {
                int ki = i;
                int row = i / 2;
                int col = i % 2;
                float x = startX + col * gapX;
                float y = startY - row * gapY;

                var knot = MakeRect(rosaryPanel.transform, "Knot_" + i);
                var kRT = knot.GetComponent<RectTransform>();
                kRT.anchorMin = kRT.anchorMax = new Vector2(0.5f, 0.5f);
                kRT.anchoredPosition = new Vector2(x, y);
                kRT.sizeDelta = new Vector2(kSize, kSize);
                knotSlotObjects[i] = knot;

                var kImg = knot.AddComponent<Image>();
                kImg.sprite = knotFrameSprite;
                kImg.color = Color.white;
                knotSlotBoxImgs[i] = kImg;
                AddClick(knot, () => ClickKnotSlot(ki));

                var bgGO = MakeRect(knot.transform, "Bg");
                var bgRT = bgGO.GetComponent<RectTransform>();
                bgRT.anchorMin = new Vector2(0.08f, 0.08f);
                bgRT.anchorMax = new Vector2(0.92f, 0.92f);
                bgRT.offsetMin = bgRT.offsetMax = Vector2.zero;
                var bgImg = bgGO.AddComponent<Image>();
                bgImg.color = PurpleSlotBg;
                bgImg.raycastTarget = false;
                bgGO.SetActive(false);
                knotSlotBgImgs[i] = bgImg;

                var iconGO = MakeRect(knot.transform, "Icon");
                var iRT = iconGO.GetComponent<RectTransform>();
                iRT.anchorMin = new Vector2(0.12f, 0.12f);
                iRT.anchorMax = new Vector2(0.88f, 0.88f);
                iRT.offsetMin = iRT.offsetMax = Vector2.zero;
                var iImg = iconGO.AddComponent<Image>();
                iImg.preserveAspect = true;
                iImg.raycastTarget = false;
                iconGO.SetActive(false);
                knotSlotIcons[i] = iImg;

                // Cord bead dot between columns
                if (col == 0)
                {
                    var dot = MakeRect(rosaryPanel.transform, "Dot_H_" + row);
                    var dRT = dot.GetComponent<RectTransform>();
                    dRT.anchorMin = dRT.anchorMax = new Vector2(0.5f, 0.5f);
                    dRT.anchoredPosition = new Vector2(0, y);
                    dRT.sizeDelta = new Vector2(12f, 12f);
                    var dImg = dot.AddComponent<Image>();
                    dImg.color = new Color(0.55f, 0.38f, 0.22f, 0.9f);
                    dImg.raycastTarget = false;
                }
                // Cord bead dot between rows
                if (row < 3)
                {
                    var dot = MakeRect(rosaryPanel.transform, $"Dot_V_{col}_{row}");
                    var dRT = dot.GetComponent<RectTransform>();
                    dRT.anchorMin = dRT.anchorMax = new Vector2(0.5f, 0.5f);
                    dRT.anchoredPosition = new Vector2(x, y - gapY * 0.5f);
                    dRT.sizeDelta = new Vector2(10f, 10f);
                    var dImg = dot.AddComponent<Image>();
                    dImg.color = new Color(0.55f, 0.38f, 0.22f, 0.9f);
                    dImg.raycastTarget = false;
                }
            }

            rosaryPanel.SetActive(false);
        }

        void BuildRelicPanel(Transform parent)
        {
            relicPanel = MakeRect(parent, "RelicPanel");
            var rRT = relicPanel.GetComponent<RectTransform>();
            rRT.anchorMin = new Vector2(0, 1);
            rRT.anchorMax = new Vector2(0, 1);
            rRT.pivot = new Vector2(0.5f, 0.5f);
            rRT.anchoredPosition = new Vector2(390f, -590f);
            rRT.sizeDelta = new Vector2(360f, 90f);

            var slotSprite = LoadSpriteRes("UI/Sprites/ItemSlot_Empty");
            if (slotSprite == null) slotSprite = LoadSpriteRes("UI/Sprites/Slot_01");

            float rSize = 84f, rGap = 110f;
            for (int i = 0; i < 3; i++)
            {
                int ri = i;
                float x = -rGap + i * rGap;
                var slot = MakeRect(relicPanel.transform, "RelicSlot_" + i);
                var sRT = slot.GetComponent<RectTransform>();
                sRT.anchorMin = sRT.anchorMax = new Vector2(0.5f, 0.5f);
                sRT.anchoredPosition = new Vector2(x, 0);
                sRT.sizeDelta = new Vector2(rSize, rSize);
                relicSlotObjects[i] = slot;

                var sImg = slot.AddComponent<Image>();
                sImg.sprite = slotSprite;
                sImg.color = Color.white;
                AddClick(slot, () => ClickRelicSlot(ri));

                var iconGO = MakeRect(slot.transform, "Icon");
                var iRT = iconGO.GetComponent<RectTransform>();
                iRT.anchorMin = new Vector2(0.12f, 0.12f);
                iRT.anchorMax = new Vector2(0.88f, 0.88f);
                iRT.offsetMin = iRT.offsetMax = Vector2.zero;
                var iImg = iconGO.AddComponent<Image>();
                iImg.preserveAspect = true;
                iImg.raycastTarget = false;
                iconGO.SetActive(false);
                relicSlotIcons[i] = iImg;
            }

            relicPanel.SetActive(false);
        }

        void BuildPrayerSlot(Transform parent)
        {
            prayerSlotObject = MakeRect(parent, "PrayerEquippedSlot");
            var pRT = prayerSlotObject.GetComponent<RectTransform>();
            pRT.anchorMin = new Vector2(0, 1);
            pRT.anchorMax = new Vector2(0, 1);
            pRT.pivot = new Vector2(0.5f, 0.5f);
            pRT.anchoredPosition = new Vector2(390f, -440f);
            pRT.sizeDelta = new Vector2(100f, 100f);

            var pImg = prayerSlotObject.AddComponent<Image>();
            var sp = LoadSpriteRes("UI/Sprites/ItemSlot_Empty");
            if (sp == null) sp = LoadSpriteRes("UI/Sprites/Slot_01");
            pImg.sprite = sp;
            pImg.color = Color.white;
            AddClick(prayerSlotObject, ClickPrayerSlot);

            var iconGO = MakeRect(prayerSlotObject.transform, "Icon");
            var iRT = iconGO.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.12f, 0.12f);
            iRT.anchorMax = new Vector2(0.88f, 0.88f);
            iRT.offsetMin = iRT.offsetMax = Vector2.zero;
            prayerSlotIcon = iconGO.AddComponent<Image>();
            prayerSlotIcon.preserveAspect = true;
            prayerSlotIcon.raycastTarget = false;
            iconGO.SetActive(false);

            prayerSlotObject.SetActive(false);
        }

        void BuildSwordHeartSlot(Transform parent)
        {
            swordHeartSlotObject = MakeRect(parent, "SwordHeartEquippedSlot");
            var sRT = swordHeartSlotObject.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 1);
            sRT.anchorMax = new Vector2(0, 1);
            sRT.pivot = new Vector2(0.5f, 0.5f);
            sRT.anchoredPosition = new Vector2(390f, -440f);
            sRT.sizeDelta = new Vector2(100f, 100f);

            var sImg = swordHeartSlotObject.AddComponent<Image>();
            var sp = LoadSpriteRes("UI/Sprites/ItemSlot_Empty");
            if (sp == null) sp = LoadSpriteRes("UI/Sprites/Slot_01");
            sImg.sprite = sp;
            sImg.color = Color.white;
            AddClick(swordHeartSlotObject, ClickSwordHeartSlot);

            var iconGO = MakeRect(swordHeartSlotObject.transform, "Icon");
            var iRT = iconGO.GetComponent<RectTransform>();
            iRT.anchorMin = new Vector2(0.12f, 0.12f);
            iRT.anchorMax = new Vector2(0.88f, 0.88f);
            iRT.offsetMin = iRT.offsetMax = Vector2.zero;
            swordHeartSlotIcon = iconGO.AddComponent<Image>();
            swordHeartSlotIcon.preserveAspect = true;
            swordHeartSlotIcon.raycastTarget = false;
            iconGO.SetActive(false);

            swordHeartSlotObject.SetActive(false);
        }

        void BuildLoreModal(Transform canvasT)
        {
            loreModal = MakeRect(canvasT, "LoreModal");
            loreModal.AddComponent<Image>().color = new Color(0.02f, 0.01f, 0.02f, 0.92f);
            StretchFull(loreModal.GetComponent<RectTransform>());

            var win = MakeRect(loreModal.transform, "LoreWindow");
            var wRT = win.GetComponent<RectTransform>();
            wRT.anchorMin = new Vector2(0.5f, 0.5f);
            wRT.anchorMax = new Vector2(0.5f, 0.5f);
            wRT.pivot = new Vector2(0.5f, 0.5f);
            wRT.anchoredPosition = Vector2.zero;
            wRT.sizeDelta = new Vector2(1100f, 700f);
            win.AddComponent<Image>().color = new Color(0.08f, 0.045f, 0.05f, 0.98f);
            var wOl = win.AddComponent<Outline>();
            wOl.effectColor = new Color(0.65f, 0.47f, 0.18f, 0.75f);
            wOl.effectDistance = new Vector2(2, -2);

            // Close button top-right
            var cBtn = MakeRect(win.transform, "CloseBtn");
            var cBRT = cBtn.GetComponent<RectTransform>();
            cBRT.anchorMin = new Vector2(1, 1);
            cBRT.anchorMax = new Vector2(1, 1);
            cBRT.pivot = new Vector2(1, 1);
            cBRT.anchoredPosition = new Vector2(-24f, -20f);
            cBRT.sizeDelta = new Vector2(150f, 52f);
            var cBImg = cBtn.AddComponent<Image>();
            cBImg.sprite = LoadSpriteRes("UI/Sprites/Boton_04");
            cBImg.type = Image.Type.Sliced;
            var cBLbl = MakeLabel(cBtn.transform, "Close", 24, titleFont, new Color(0.95f, 0.85f, 0.55f));
            StretchFull(cBLbl.GetComponent<RectTransform>());
            cBLbl.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            AddClick(cBtn, () => loreModal.SetActive(false));

            // Title
            var tGO = MakeLabel(win.transform, "Thorn", 32, titleFont, new Color(0.95f, 0.88f, 0.55f));
            var tRT = tGO.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 1);
            tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0, 1);
            tRT.anchoredPosition = new Vector2(50f, -60f);
            tRT.sizeDelta = new Vector2(-100f, 44f);
            loreTitleText = tGO.GetComponent<Text>();
            loreTitleText.alignment = TextAnchor.UpperLeft;

            // Subtitle
            var sGO = MakeLabel(win.transform, "Deosgracias' Farewell", 22, bodyFont, new Color(0.75f, 0.65f, 0.42f));
            var sRT = sGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 1);
            sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0, 1);
            sRT.anchoredPosition = new Vector2(50f, -112f);
            sRT.sizeDelta = new Vector2(-100f, 36f);
            loreSubtitleText = sGO.GetComponent<Text>();
            loreSubtitleText.alignment = TextAnchor.UpperLeft;

            // Divider
            var div = MakeRect(win.transform, "Div");
            var dRT2 = div.GetComponent<RectTransform>();
            dRT2.anchorMin = new Vector2(0, 1);
            dRT2.anchorMax = new Vector2(1, 1);
            dRT2.pivot = new Vector2(0.5f, 0.5f);
            dRT2.anchoredPosition = new Vector2(0, -156f);
            dRT2.sizeDelta = new Vector2(-80f, 2f);
            div.AddComponent<Image>().color = new Color(0.55f, 0.42f, 0.18f, 0.7f);

            // Body
            var bGO = MakeLabel(win.transform, "", 22, bodyFont, new Color(0.88f, 0.82f, 0.6f));
            var bRT = bGO.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0, 1);
            bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0, 1);
            bRT.anchoredPosition = new Vector2(50f, -175f);
            bRT.sizeDelta = new Vector2(-100f, 480f);
            loreBodyText = bGO.GetComponent<Text>();
            loreBodyText.alignment = TextAnchor.UpperLeft;
            loreBodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            loreBodyText.verticalOverflow = VerticalWrapMode.Overflow;

            loreModal.SetActive(false);
        }

        GameObject MakeRect(Transform parent, string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        GameObject MakeLabel(Transform parent, string text, int fontSize, Font font, Color color)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = fontSize;
            var f = font;
            if (f == null) f = titleFont;
            if (f == null) f = bodyFont;
            if (f == null) f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.font = f;
            t.color = color;
            t.raycastTarget = false;
            return go;
        }

        void AddClick(GameObject go, Action action)
        {
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => action());
        }

        Sprite LoadSpriteRes(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (spriteCache.TryGetValue(path, out var cached)) return cached;
            var tex = Resources.Load<Texture2D>(path);
            if (tex == null) { spriteCache[path] = null; return null; }
            tex.filterMode = FilterMode.Point;
            var sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            spriteCache[path] = sp;
            return sp;
        }

        Sprite LoadIconSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            return LoadSpriteRes(path);
        }
    }
}
