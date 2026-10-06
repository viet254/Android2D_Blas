code = '''using System;
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
        private GameObject loreButton;
        private GameObject equipButton;

        private Image cursorImage;
        private Image leftGraphicImg;
        private readonly Image[] slotIcons = new Image[14];
        private readonly InventoryCatalog.Item[] currentItems = new InventoryCatalog.Item[14];
        private readonly GameObject[] tabButtons = new GameObject[7];

        private InventoryCatalog catalog;
        private int currentTab = 2; // Default to Quest Items
        private int selectedSlot = -1;
        private Font titleFont;
        private Font bodyFont;
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static readonly string[] Categories = {
            "rosarybead", "prayer", "questitem", "relic", "sword", "collectibleitem", "ability"
        };
        public static readonly string[] CategoryTitles = {
            "ROSARY BEADS", "PRAYERS", "QUEST ITEMS", "RELICS", "MEA CULPA HEARTS", "COLLECTIBLES", "ABILITIES"
        };
        static readonly string[] TabIcons = {
            "UI/Sprites/Item_RosaryBeads",
            "UI/Sprites/Item_Prayers",
            "UI/Sprites/Item_QuestItems",
            "UI/Sprites/Item_Relics",
            "UI/Sprites/Item_MeaCulpa",
            "UI/Sprites/Item_Collectibles",
            "UI/Sprites/Item_Abilities"
        };

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
                for (int i = 0; i < 14; i++)
                {
                    if (currentItems[i] != null && string.Equals(currentItems[i].id, id, StringComparison.OrdinalIgnoreCase))
                    {
                        selectedSlot = i;
                        UpdateCursor();
                        UpdateDetails();
                        return;
                    }
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

        public void Toggle(int tab = 2)
        {
            if (IsOpen) Hide();
            else Show(tab);
        }

        public void Show(int tab = 2)
        {
            if (root == null) BuildUI();
            currentTab = Mathf.Clamp(tab, 0, Categories.Length - 1);
            selectedSlot = -1;
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
            RefreshTab();
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

            // Background illustration per category
            string bgPath = "";
            int cols = 5;
            float startX = 640f; // Shift to right when illustration is on left
            if (currentTab == 1) { bgPath = "UI/Sprites/inventory-bg-prayers"; cols = 5; startX = 640f; } // Prayers (Sacred book)
            else if (currentTab == 2) { bgPath = ""; cols = 7; startX = 280f; } // Quest Items (Full-width grid)
            else if (currentTab == 3) { bgPath = "UI/Sprites/inventory-bg-relicary"; cols = 5; startX = 640f; } // Relics
            else if (currentTab == 5) { bgPath = "UI/Sprites/inventory-bg-osary"; cols = 5; startX = 640f; } // Collectibles (Bone chest)
            else if (currentTab == 0) { bgPath = "UI/Sprites/inventory-bg-rosary"; cols = 5; startX = 640f; } // Rosary beads

            if (leftGraphicImg != null)
            {
                if (!string.IsNullOrEmpty(bgPath))
                {
                    leftGraphicImg.sprite = LoadSpriteRes(bgPath);
                    leftGraphicImg.gameObject.SetActive(leftGraphicImg.sprite != null);
                }
                else
                {
                    leftGraphicImg.gameObject.SetActive(false);
                }
            }

            // Reposition slots dynamically
            float slotGap = 96f;
            float row0Y = -155f, row1Y = -255f;
            for (int i = 0; i < 14; i++)
            {
                int row = i / cols;
                int col = i % cols;
                if (slotIcons[i] != null)
                {
                    var rt = slotIcons[i].transform.parent.GetComponent<RectTransform>();
                    if (row < 2)
                    {
                        rt.anchoredPosition = new Vector2(startX + col * slotGap, row == 0 ? row0Y : row1Y);
                        rt.gameObject.SetActive(true);
                    }
                    else
                    {
                        rt.gameObject.SetActive(false); // Hide slots 10-13 if 5 cols
                    }
                }
            }

            PopulateSlots();
            UpdateDetails();
        }

        void PopulateSlots()
        {
            string cat = Categories[currentTab];
            int slot = 0;
            for (int i = 0; i < 14; i++) currentItems[i] = null;
            if (catalog != null && catalog.items != null)
            {
                foreach (var item in catalog.items)
                {
                    if (!string.Equals(item.category, cat, StringComparison.OrdinalIgnoreCase)) continue;
                    if (slot >= 14) break;
                    currentItems[slot] = item;
                    if (slotIcons[slot] != null)
                    {
                        var sp = LoadIconSprite(item.icon);
                        slotIcons[slot].sprite = sp;
                        slotIcons[slot].color = game != null && game.progress.Owns(item.id) ? Color.white : new Color(0.3f, 0.3f, 0.3f, 0.6f);
                        slotIcons[slot].gameObject.SetActive(sp != null);
                    }
                    slot++;
                }
            }
            for (; slot < 14; slot++)
            {
                if (slotIcons[slot] != null) slotIcons[slot].gameObject.SetActive(false);
                currentItems[slot] = null;
            }
            if (selectedSlot < 0)
            {
                for (int i = 0; i < 14; i++)
                {
                    if (currentItems[i] != null && game != null && game.progress.Owns(currentItems[i].id))
                    {
                        selectedSlot = i;
                        break;
                    }
                }
                if (selectedSlot < 0) selectedSlot = 0;
            }
            UpdateCursor();
        }

        void SelectSlot(int idx)
        {
            if (idx < 0 || idx >= 14) return;
            selectedSlot = idx;
            UpdateCursor();
            UpdateDetails();
        }

        void UpdateCursor()
        {
            if (cursorImage == null) return;
            if (selectedSlot >= 0 && selectedSlot < 14 && slotIcons[selectedSlot] != null)
            {
                cursorImage.transform.SetParent(slotIcons[selectedSlot].transform.parent, false);
                var r = cursorImage.rectTransform;
                r.anchorMin = Vector2.zero;
                r.anchorMax = Vector2.one;
                r.offsetMin = r.offsetMax = Vector2.zero;
                cursorImage.gameObject.SetActive(true);
            }
            else
            {
                cursorImage.gameObject.SetActive(false);
            }
        }

        void UpdateDetails()
        {
            var item = selectedSlot >= 0 && selectedSlot < 14 ? currentItems[selectedSlot] : null;
            if (itemNameText != null) itemNameText.text = item != null ? item.caption : "";
            if (itemDescText != null)
            {
                string desc = "";
                if (item != null)
                {
                    if (!string.IsNullOrEmpty(item.description)) desc = item.description;
                    else if (!string.IsNullOrEmpty(item.lore)) desc = item.lore.Length > 200 ? item.lore.Substring(0, 200) + "..." : item.lore;
                }
                itemDescText.text = desc;
            }
            bool hasLore    = item != null && !string.IsNullOrEmpty(item.lore);
            bool owned      = item != null && game != null && game.progress.Owns(item.id);
            bool equippable = owned && item != null && (item.category == "relic" || item.category == "prayer" || item.category == "sword" || item.category == "rosarybead");
            if (loreButton  != null) loreButton.SetActive(hasLore);
            if (equipButton != null) equipButton.SetActive(equippable);
            if (equipBtnText != null && item != null)
                equipBtnText.text = game != null && game.progress.IsEquipped(item.id) ? "Unequip" : "Equip";
        }

        public void ShowLoreModal()
        {
            var item = selectedSlot >= 0 && selectedSlot < 14 ? currentItems[selectedSlot] : null;
            if (item == null && catalog?.items != null) item = Array.Find(catalog.items, x => x.id == "QI31");
            if (item == null || loreModal == null) return;
            if (loreTitleText    != null) loreTitleText.text    = item.caption;
            if (loreSubtitleText != null) loreSubtitleText.text = string.IsNullOrEmpty(item.subtitle) ? "Deosgracias\' Farewell" : item.subtitle;
            if (loreBodyText     != null) loreBodyText.text     = string.IsNullOrEmpty(item.lore) ?
                "\\\"Brother Abbot, you know that I have been a scribe in this abbey since I was but a wee child...\\\"" : item.lore;
            loreModal.SetActive(true);
            game?.Sfx("INVENTORY_EQUIP");
        }

        void EquipSelected()
        {
            if (selectedSlot < 0 || selectedSlot >= 14) return;
            var item = currentItems[selectedSlot];
            if (item != null && game != null) { game.EquipInventoryItem(item); UpdateDetails(); }
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

                // Dark overlay
                root = MakeRect(canvasObj.transform, "InventoryRoot");
                var rootImg = root.AddComponent<Image>();
                rootImg.color = new Color(0.02f, 0.01f, 0.02f, 0.88f);
                StretchFull(root.GetComponent<RectTransform>());

                // Main window 1280x720
                var win = MakeRect(root.transform, "MainWindow");
                var winRT = win.GetComponent<RectTransform>();
                winRT.anchorMin = new Vector2(0.5f, 0.5f);
                winRT.anchorMax = new Vector2(0.5f, 0.5f);
                winRT.pivot = new Vector2(0.5f, 0.5f);
                winRT.anchoredPosition = Vector2.zero;
                winRT.sizeDelta = new Vector2(1280, 720);
                win.AddComponent<Image>().color = new Color(0.055f, 0.04f, 0.055f, 0.98f);
                var winOl = win.AddComponent<Outline>();
                winOl.effectColor = new Color(0.7f, 0.5f, 0.18f, 0.85f);
                winOl.effectDistance = new Vector2(3, -3);

                // Top Tab Bar
                float tabW = 118f, tabH = 64f, tabSpacingX = 126f, tabStartX = 64f, tabY = -36f;
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
                    iconRT.sizeDelta = new Vector2(38f, 38f);
                    var iconImg = icon.AddComponent<Image>();
                    iconImg.sprite = LoadSpriteRes(TabIcons[i]);
                    iconImg.preserveAspect = true;
                    iconImg.raycastTarget = false;
                    AddClick(tb, () => SwitchTab(ti));
                }

                // Authentic [X] Close Button (Top-Right of Tab Bar)
                var closeBtn = MakeRect(win.transform, "CloseBtn");
                var cRT = closeBtn.GetComponent<RectTransform>();
                cRT.anchorMin = new Vector2(1, 1);
                cRT.anchorMax = new Vector2(1, 1);
                cRT.pivot = new Vector2(1, 0.5f);
                cRT.anchoredPosition = new Vector2(-40f, tabY);
                cRT.sizeDelta = new Vector2(92f, 55f);
                var cImg = closeBtn.AddComponent<Image>();
                cImg.sprite = LoadSpriteRes("UI/Sprites/X");
                cImg.preserveAspect = true;
                AddClick(closeBtn, Hide);

                // Category Title (Centered under tab bar)
                var catTitle = MakeLabel(win.transform, CategoryTitles[currentTab], 28, titleFont, new Color(1f, 0.88f, 0.45f));
                var catRT = catTitle.GetComponent<RectTransform>();
                catRT.anchorMin = new Vector2(0.5f, 1);
                catRT.anchorMax = new Vector2(0.5f, 1);
                catRT.pivot = new Vector2(0.5f, 1f);
                catRT.anchoredPosition = new Vector2(0, -82f);
                catRT.sizeDelta = new Vector2(900f, 40f);
                categoryTitleText = catTitle.GetComponent<Text>();
                categoryTitleText.alignment = TextAnchor.MiddleCenter;

                // Thin gold divider under category title
                var divTop = MakeRect(win.transform, "DivTop");
                var dtRT = divTop.GetComponent<RectTransform>();
                dtRT.anchorMin = new Vector2(0, 1);
                dtRT.anchorMax = new Vector2(1, 1);
                dtRT.pivot = new Vector2(0.5f, 0.5f);
                dtRT.anchoredPosition = new Vector2(0, -124f);
                dtRT.sizeDelta = new Vector2(-60f, 1f);
                divTop.AddComponent<Image>().color = new Color(0.65f, 0.47f, 0.17f, 0.7f);

                // Left Graphic (For category background illustration)
                var lgo = MakeRect(win.transform, "LeftGraphic");
                var lrt = lgo.GetComponent<RectTransform>();
                lrt.anchorMin = new Vector2(0, 1);
                lrt.anchorMax = new Vector2(0, 1);
                lrt.pivot = new Vector2(0, 1);
                lrt.anchoredPosition = new Vector2(120f, -140f);
                lrt.sizeDelta = new Vector2(420f, 210f);
                leftGraphicImg = lgo.AddComponent<Image>();
                leftGraphicImg.preserveAspect = true;
                leftGraphicImg.raycastTarget = false;
                leftGraphicImg.gameObject.SetActive(false);

                // Slot Grid (14 slots instantiated)
                float slotSize = 82f, slotGap = 96f, gridStartX = 280f, row0Y = -155f, row1Y = -255f;
                var cursorSprite = LoadSpriteRes("UI/Sprites/ItemCursorMobile 1_0");
                var cursorGO = MakeRect(win.transform, "SlotCursor");
                var cursorRT2 = cursorGO.GetComponent<RectTransform>();
                cursorRT2.anchorMin = Vector2.zero;
                cursorRT2.anchorMax = Vector2.one;
                cursorRT2.offsetMin = cursorRT2.offsetMax = Vector2.zero;
                cursorImage = cursorGO.AddComponent<Image>();
                cursorImage.sprite = cursorSprite;
                cursorImage.raycastTarget = false;
                cursorGO.SetActive(false);

                for (int i = 0; i < 14; i++)
                {
                    int si = i;
                    int row = i / 7, col = i % 7;
                    float sx = gridStartX + col * slotGap;
                    float sy = row == 0 ? row0Y : row1Y;
                    var slot = MakeRect(win.transform, "Slot_" + i);
                    var slotRT = slot.GetComponent<RectTransform>();
                    slotRT.anchorMin = new Vector2(0, 1);
                    slotRT.anchorMax = new Vector2(0, 1);
                    slotRT.pivot = new Vector2(0.5f, 0.5f);
                    slotRT.anchoredPosition = new Vector2(sx, sy);
                    slotRT.sizeDelta = new Vector2(slotSize, slotSize);
                    var slotImg = slot.AddComponent<Image>();
                    slotImg.sprite = LoadSpriteRes("UI/Sprites/Slot_01");
                    slotImg.color = Color.white;
                    AddClick(slot, () => SelectSlot(si));

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

                // Item Name (Left aligned)
                var nameGO = MakeLabel(win.transform, "", 24, titleFont, new Color(0.95f, 0.88f, 0.65f));
                var nRT = nameGO.GetComponent<RectTransform>();
                nRT.anchorMin = new Vector2(0, 1);
                nRT.anchorMax = new Vector2(1, 1);
                nRT.pivot = new Vector2(0, 1);
                nRT.anchoredPosition = new Vector2(64f, -340f);
                nRT.sizeDelta = new Vector2(-128f, 32f);
                itemNameText = nameGO.GetComponent<Text>();
                itemNameText.alignment = TextAnchor.MiddleLeft;

                // Horizontal separator under item name
                var divMid = MakeRect(win.transform, "DivMid");
                var dmRT = divMid.GetComponent<RectTransform>();
                dmRT.anchorMin = new Vector2(0, 1);
                dmRT.anchorMax = new Vector2(1, 1);
                dmRT.pivot = new Vector2(0.5f, 0.5f);
                dmRT.anchoredPosition = new Vector2(0, -376f);
                dmRT.sizeDelta = new Vector2(-60f, 1f);
                divMid.AddComponent<Image>().color = new Color(0.55f, 0.42f, 0.18f, 0.8f);

                // Description text
                var descGO = MakeLabel(win.transform, "", 20, bodyFont, new Color(0.88f, 0.82f, 0.6f));
                var dRT = descGO.GetComponent<RectTransform>();
                dRT.anchorMin = new Vector2(0, 1);
                dRT.anchorMax = new Vector2(1, 1);
                dRT.pivot = new Vector2(0, 1);
                dRT.anchoredPosition = new Vector2(64f, -388f);
                dRT.sizeDelta = new Vector2(-128f, 210f);
                itemDescText = descGO.GetComponent<Text>();
                itemDescText.alignment = TextAnchor.UpperLeft;
                itemDescText.horizontalOverflow = HorizontalWrapMode.Wrap;
                itemDescText.verticalOverflow = VerticalWrapMode.Overflow;

                // Lore button (Authentic Boton_Lore)
                loreButton = MakeRect(win.transform, "LoreBtn");
                var lbRT = loreButton.GetComponent<RectTransform>();
                lbRT.anchorMin = new Vector2(1, 0);
                lbRT.anchorMax = new Vector2(1, 0);
                lbRT.pivot = new Vector2(1, 0);
                lbRT.anchoredPosition = new Vector2(-60f, 40f);
                lbRT.sizeDelta = new Vector2(150f, 47f);
                var lbImg = loreButton.AddComponent<Image>();
                lbImg.sprite = LoadSpriteRes("UI/Sprites/Boton_Lore");
                lbImg.preserveAspect = true;
                var lbl = MakeLabel(loreButton.transform, "Lore", 22, titleFont, new Color(0.95f, 0.85f, 0.55f));
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
                ebRT.anchoredPosition = new Vector2(-225f, 40f);
                ebRT.sizeDelta = new Vector2(150f, 47f);
                var ebImg = equipButton.AddComponent<Image>();
                ebImg.sprite = LoadSpriteRes("UI/Sprites/Boton_04");
                ebImg.type = Image.Type.Sliced;
                var elbGO = MakeLabel(equipButton.transform, "Equip", 22, titleFont, new Color(0.95f, 0.85f, 0.55f));
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
                if (root != null) root.SetActive(false); // ALWAYS CLOSE BY DEFAULT!
            }
        }

        void BuildLoreModal(Transform canvasT)
        {
            loreModal = MakeRect(canvasT, "LoreModal");
            loreModal.AddComponent<Image>().color = new Color(0.02f, 0.01f, 0.02f, 0.85f);
            StretchFull(loreModal.GetComponent<RectTransform>());

            var win = MakeRect(loreModal.transform, "LoreWindow");
            var wRT = win.GetComponent<RectTransform>();
            wRT.anchorMin = new Vector2(0.5f, 0.5f);
            wRT.anchorMax = new Vector2(0.5f, 0.5f);
            wRT.pivot = new Vector2(0.5f, 0.5f);
            wRT.anchoredPosition = Vector2.zero;
            wRT.sizeDelta = new Vector2(880f, 540f);
            win.AddComponent<Image>().color = new Color(0.09f, 0.05f, 0.05f, 0.97f);
            var wOl = win.AddComponent<Outline>();
            wOl.effectColor = new Color(0.65f, 0.47f, 0.18f, 0.75f);
            wOl.effectDistance = new Vector2(2, -2);

            // Close button top-right
            var cBtn = MakeRect(win.transform, "CloseBtn");
            var cBRT = cBtn.GetComponent<RectTransform>();
            cBRT.anchorMin = new Vector2(1, 1);
            cBRT.anchorMax = new Vector2(1, 1);
            cBRT.pivot = new Vector2(1, 1);
            cBRT.anchoredPosition = new Vector2(-18f, -14f);
            cBRT.sizeDelta = new Vector2(130f, 44f);
            var cBImg = cBtn.AddComponent<Image>();
            cBImg.sprite = LoadSpriteRes("UI/Sprites/Boton_04");
            cBImg.type = Image.Type.Sliced;
            var cBLbl = MakeLabel(cBtn.transform, "Close", 21, titleFont, new Color(0.95f, 0.85f, 0.55f));
            StretchFull(cBLbl.GetComponent<RectTransform>());
            cBLbl.GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            AddClick(cBtn, () => loreModal.SetActive(false));

            // Title
            var tGO = MakeLabel(win.transform, "Thorn", 26, titleFont, new Color(0.95f, 0.85f, 0.55f));
            var tRT = tGO.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 1);
            tRT.anchorMax = new Vector2(1, 1);
            tRT.pivot = new Vector2(0, 1);
            tRT.anchoredPosition = new Vector2(36f, -50f);
            tRT.sizeDelta = new Vector2(-72f, 38f);
            loreTitleText = tGO.GetComponent<Text>();
            loreTitleText.alignment = TextAnchor.UpperLeft;

            // Subtitle
            var sGO = MakeLabel(win.transform, "Deosgracias\' Farewell", 20, bodyFont, new Color(0.75f, 0.65f, 0.42f));
            var sRT = sGO.GetComponent<RectTransform>();
            sRT.anchorMin = new Vector2(0, 1);
            sRT.anchorMax = new Vector2(1, 1);
            sRT.pivot = new Vector2(0, 1);
            sRT.anchoredPosition = new Vector2(36f, -92f);
            sRT.sizeDelta = new Vector2(-72f, 30f);
            loreSubtitleText = sGO.GetComponent<Text>();
            loreSubtitleText.alignment = TextAnchor.UpperLeft;

            // Divider
            var div = MakeRect(win.transform, "Div");
            var dRT2 = div.GetComponent<RectTransform>();
            dRT2.anchorMin = new Vector2(0, 1);
            dRT2.anchorMax = new Vector2(1, 1);
            dRT2.pivot = new Vector2(0.5f, 0.5f);
            dRT2.anchoredPosition = new Vector2(0, -126f);
            dRT2.sizeDelta = new Vector2(-60f, 1f);
            div.AddComponent<Image>().color = new Color(0.55f, 0.42f, 0.18f, 0.7f);

            // Body
            var bGO = MakeLabel(win.transform, "", 19, bodyFont, new Color(0.88f, 0.82f, 0.6f));
            var bRT = bGO.GetComponent<RectTransform>();
            bRT.anchorMin = new Vector2(0, 1);
            bRT.anchorMax = new Vector2(1, 1);
            bRT.pivot = new Vector2(0, 1);
            bRT.anchoredPosition = new Vector2(36f, -138f);
            bRT.sizeDelta = new Vector2(-90f, 350f);
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
'''

with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasInventoryUI.cs', 'w', encoding='utf-8') as f:
    f.write(code)
print("Completely rewrote BlasInventoryUI.cs successfully!")
