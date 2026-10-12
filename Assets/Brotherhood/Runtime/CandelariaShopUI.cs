using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Brotherhood
{
    public sealed class CandelariaShopUI : MonoBehaviour
    {
        public static CandelariaShopUI Instance { get; private set; }

        public sealed class ShopItemDef
        {
            public string id;
            public int price;
            public string fallbackName;
            public string fallbackCategory;
            public string fallbackDesc;
        }

        public static readonly ShopItemDef[] ShopItems = new[]
        {
            new ShopItemDef
            {
                id = "RB02",
                price = 500,
                fallbackName = "Tro Tàn Hỏa Táng Thiêng Liêng",
                fallbackCategory = "Hạt Mân Côi",
                fallbackDesc = "Hạt tro được ban phước, tăng cường phòng thủ và kháng hỏa."
            },
            new ShopItemDef
            {
                id = "QI58",
                price = 400,
                fallbackName = "Chìa Khóa Phòng Trưởng Huynh",
                fallbackCategory = "Vật Phẩm Nhiệm Vụ",
                fallbackDesc = "Chìa khóa mở căn phòng bị khóa của Tu viện Trưởng Huynh."
            },
            new ShopItemDef
            {
                id = "RB05",
                price = 1000,
                fallbackName = "Ngọc Trai Rỗng",
                fallbackCategory = "Hạt Mân Côi",
                fallbackDesc = "Ngọc trai huyền bí, hồi phục Nhiệt Huyết khi đã cạn kiệt."
            },
            new ShopItemDef
            {
                id = "QI44",
                price = 1200,
                fallbackName = "Nút Thắt Dây Mân Côi",
                fallbackCategory = "Vật Phẩm Nhiệm Vụ",
                fallbackDesc = "Dây thắt cổ xưa. Đưa cho Soledad để mở thêm ô chứa Hạt Mân Côi."
            },
            new ShopItemDef
            {
                id = "RB08",
                price = 3000,
                fallbackName = "Mảnh Mặt Nạ Vàng",
                fallbackCategory = "Hạt Mân Côi",
                fallbackDesc = "Mảnh mặt nạ cổ đại, tăng khả năng kháng lửa và sấm sét."
            },
            new ShopItemDef
            {
                id = "QI11",
                price = 800,
                fallbackName = "Ruy Băng Cưới Rách",
                fallbackCategory = "Vật Phẩm Nhiệm Vụ",
                fallbackDesc = "Một dải lụa trắng nhuốm bi kịch, mang lại ân điển linh thiêng."
            }
        };

        BrotherhoodGame game;
        Canvas canvas;
        GameObject root;
        Text tearsText;
        Font mainFont, boldFont;
        readonly List<ShopCard> cards = new List<ShopCard>();
        float inputCooldown;

        sealed class ShopCard
        {
            public ShopItemDef def;
            public InventoryCatalog.Item catalogItem;
            public Button buyButton;
            public Text buyBtnText;
            public Image buyBtnImage;
            public Image iconImage;
            public Text nameText;
            public Text priceText;
            public Text descText;
        }

        public bool IsOpen => root != null && root.activeSelf;

        public void Initialize(BrotherhoodGame owner)
        {
            Instance = this;
            game = owner;

            mainFont = VietnameseSource.DynamicFont ?? Resources.Load<Font>("Fonts/Caudex-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            boldFont = VietnameseSource.DynamicFont ?? Resources.Load<Font>("Fonts/Caudex-Bold") ?? mainFont;

            var canvasGO = new GameObject("Candelaria Shop Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGO.transform.SetParent(transform, false);
            canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 75;

            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;

            // Fullscreen backdrop
            root = MakeRect(canvasGO.transform, "Shop Root", Vector2.zero, Vector2.one).gameObject;
            var backdrop = root.AddComponent<Image>();
            backdrop.color = new Color(0.015f, 0.01f, 0.02f, 0.88f);

            // Main Dialog Frame
            var panel = MakeRect(root.transform, "Shop Panel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            panel.sizeDelta = new Vector2(1100, 620);
            var panelImg = panel.gameObject.AddComponent<Image>();
            panelImg.color = new Color(0.065f, 0.045f, 0.055f, 0.98f);
            var outline = panel.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.72f, 0.52f, 0.22f, 1f);
            outline.effectDistance = new Vector2(2, -2);

            // Header Banner
            var header = MakeRect(panel, "Header", new Vector2(0, 1), new Vector2(1, 1));
            header.anchoredPosition = new Vector2(0, -42);
            header.sizeDelta = new Vector2(-40, 70);

            var title = MakeLabel(header, "CỬA HÀNG CANDELARIA", 28, boldFont, new Color(0.96f, 0.85f, 0.55f), TextAnchor.MiddleLeft);
            title.rectTransform.anchorMin = new Vector2(0, 0.4f);
            title.rectTransform.anchorMax = new Vector2(0.6f, 1f);
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            var sub = MakeLabel(header, "Thương nhân Candelaria · Trao đổi Nước Mắt Chuộc Tội lấy thánh vật", 15, mainFont, new Color(0.80f, 0.75f, 0.65f), TextAnchor.MiddleLeft);
            sub.rectTransform.anchorMin = new Vector2(0, 0);
            sub.rectTransform.anchorMax = new Vector2(0.6f, 0.45f);
            sub.rectTransform.offsetMin = sub.rectTransform.offsetMax = Vector2.zero;

            // Tears counter on top-right
            var tearsBox = MakeRect(header, "Tears Box", new Vector2(0.65f, 0.1f), new Vector2(1f, 0.9f));
            var tbImg = tearsBox.gameObject.AddComponent<Image>();
            tbImg.color = new Color(0.12f, 0.08f, 0.06f, 0.9f);
            var tbOl = tearsBox.gameObject.AddComponent<Outline>();
            tbOl.effectColor = new Color(0.55f, 0.38f, 0.15f, 0.8f);
            tbOl.effectDistance = new Vector2(1, -1);

            tearsText = MakeLabel(tearsBox, "NƯỚC MẮT: 0", 19, boldFont, new Color(1f, 0.90f, 0.45f), TextAnchor.MiddleCenter);
            tearsText.rectTransform.anchorMin = Vector2.zero;
            tearsText.rectTransform.anchorMax = Vector2.one;
            tearsText.rectTransform.offsetMin = tearsText.rectTransform.offsetMax = Vector2.zero;

            // Grid Container for 6 cards (2 rows of 3)
            var grid = MakeRect(panel, "Item Grid", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
            grid.anchoredPosition = new Vector2(0, -10);
            grid.sizeDelta = new Vector2(1040, 420);

            float cardW = 330f, cardH = 195f;
            float spacingX = 22f, spacingY = 16f;
            float totalW = 3 * cardW + 2 * spacingX;
            float startX = -totalW * 0.5f + cardW * 0.5f;
            float startY = cardH * 0.5f + spacingY * 0.5f;

            var catalog = InventoryCatalog.Load();

            for (int i = 0; i < ShopItems.Length; i++)
            {
                var def = ShopItems[i];
                int col = i % 3;
                int row = i / 3;
                float px = startX + col * (cardW + spacingX);
                float py = (row == 0 ? startY : -startY);

                var cardRT = MakeRect(grid, "Card_" + def.id, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
                cardRT.anchoredPosition = new Vector2(px, py);
                cardRT.sizeDelta = new Vector2(cardW, cardH);

                var card = BuildItemCard(cardRT, def, catalog);
                cards.Add(card);
            }

            // Bottom Bar: Close button
            var closeBtnRT = MakeRect(panel, "Close Button", new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            closeBtnRT.anchoredPosition = new Vector2(0, 36);
            closeBtnRT.sizeDelta = new Vector2(180, 44);
            var cbImg = closeBtnRT.gameObject.AddComponent<Image>();
            cbImg.color = new Color(0.22f, 0.12f, 0.06f, 0.95f);
            var cbOl = closeBtnRT.gameObject.AddComponent<Outline>();
            cbOl.effectColor = new Color(0.65f, 0.45f, 0.20f, 1f);
            cbOl.effectDistance = new Vector2(1, -1);
            var closeBtn = closeBtnRT.gameObject.AddComponent<Button>();
            closeBtn.onClick.AddListener(Close);

            var closeLbl = MakeLabel(closeBtnRT, "ĐÓNG", 19, boldFont, new Color(0.95f, 0.85f, 0.65f), TextAnchor.MiddleCenter);
            closeLbl.rectTransform.anchorMin = Vector2.zero;
            closeLbl.rectTransform.anchorMax = Vector2.one;
            closeLbl.rectTransform.offsetMin = closeLbl.rectTransform.offsetMax = Vector2.zero;

            root.SetActive(false);
        }

        ShopCard BuildItemCard(RectTransform parent, ShopItemDef def, InventoryCatalog catalog)
        {
            var cardImg = parent.gameObject.AddComponent<Image>();
            cardImg.color = new Color(0.10f, 0.07f, 0.09f, 0.96f);
            var cardOl = parent.gameObject.AddComponent<Outline>();
            cardOl.effectColor = new Color(0.40f, 0.28f, 0.14f, 0.75f);
            cardOl.effectDistance = new Vector2(1, -1);

            var catItem = catalog != null ? catalog.Find(def.id) : null;
            string itemName = catItem != null && !string.IsNullOrEmpty(catItem.caption) ? catItem.caption : def.fallbackName;
            string itemDesc = catItem != null && !string.IsNullOrEmpty(catItem.description) ? catItem.description : def.fallbackDesc;

            // Icon Slot Frame
            var slotRT = MakeRect(parent, "Icon Slot", new Vector2(0, 1), new Vector2(0, 1));
            slotRT.anchoredPosition = new Vector2(36, -38);
            slotRT.sizeDelta = new Vector2(54, 54);
            var slotImg = slotRT.gameObject.AddComponent<Image>();
            slotImg.color = new Color(0.18f, 0.12f, 0.10f, 0.95f);
            var slotOl = slotRT.gameObject.AddComponent<Outline>();
            slotOl.effectColor = new Color(0.55f, 0.38f, 0.16f, 0.8f);

            var iconRT = MakeRect(slotRT, "Icon", Vector2.zero, Vector2.one);
            iconRT.offsetMin = new Vector2(4, 4);
            iconRT.offsetMax = new Vector2(-4, -4);
            var iconImg = iconRT.gameObject.AddComponent<Image>();
            iconImg.preserveAspect = true;
            iconImg.sprite = LoadItemSprite(def.id);

            // Title and Price
            var titleLbl = MakeLabel(parent, itemName, 15, boldFont, new Color(0.96f, 0.85f, 0.55f), TextAnchor.MiddleLeft);
            titleLbl.rectTransform.anchorMin = new Vector2(0, 1);
            titleLbl.rectTransform.anchorMax = new Vector2(1, 1);
            titleLbl.rectTransform.pivot = new Vector2(0, 1);
            titleLbl.rectTransform.anchoredPosition = new Vector2(72, -14);
            titleLbl.rectTransform.sizeDelta = new Vector2(245, 24);

            var priceLbl = MakeLabel(parent, def.price.ToString("N0") + " Nước Mắt", 14, boldFont, new Color(1f, 0.78f, 0.35f), TextAnchor.MiddleLeft);
            priceLbl.rectTransform.anchorMin = new Vector2(0, 1);
            priceLbl.rectTransform.anchorMax = new Vector2(1, 1);
            priceLbl.rectTransform.pivot = new Vector2(0, 1);
            priceLbl.rectTransform.anchoredPosition = new Vector2(72, -38);
            priceLbl.rectTransform.sizeDelta = new Vector2(245, 20);

            // Description text
            var descLbl = MakeLabel(parent, itemDesc, 12, mainFont, new Color(0.82f, 0.78f, 0.72f), TextAnchor.UpperLeft);
            descLbl.horizontalOverflow = HorizontalWrapMode.Wrap;
            descLbl.verticalOverflow = VerticalWrapMode.Truncate;
            descLbl.rectTransform.anchorMin = new Vector2(0, 0);
            descLbl.rectTransform.anchorMax = new Vector2(1, 1);
            descLbl.rectTransform.offsetMin = new Vector2(12, 44);
            descLbl.rectTransform.offsetMax = new Vector2(-12, -70);

            // Buy Button
            var btnRT = MakeRect(parent, "Buy Button", new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            btnRT.anchoredPosition = new Vector2(0, 22);
            btnRT.sizeDelta = new Vector2(290, 32);
            var btnImg = btnRT.gameObject.AddComponent<Image>();
            btnImg.color = new Color(0.28f, 0.18f, 0.08f, 0.95f);
            var btnOl = btnRT.gameObject.AddComponent<Outline>();
            btnOl.effectColor = new Color(0.65f, 0.48f, 0.18f, 0.9f);
            var btn = btnRT.gameObject.AddComponent<Button>();

            var btnLbl = MakeLabel(btnRT, "MUA", 14, boldFont, Color.white, TextAnchor.MiddleCenter);
            btnLbl.rectTransform.anchorMin = Vector2.zero;
            btnLbl.rectTransform.anchorMax = Vector2.one;
            btnLbl.rectTransform.offsetMin = btnLbl.rectTransform.offsetMax = Vector2.zero;

            var card = new ShopCard
            {
                def = def,
                catalogItem = catItem,
                buyButton = btn,
                buyBtnText = btnLbl,
                buyBtnImage = btnImg,
                iconImage = iconImg,
                nameText = titleLbl,
                priceText = priceLbl,
                descText = descLbl
            };

            btn.onClick.AddListener(() => BuyItem(card));
            return card;
        }

        Sprite LoadItemSprite(string id)
        {
            var tex = Resources.Load<Texture2D>("Inventory/Icons/" + id);
            if (tex == null) return null;
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        public void Open()
        {
            if (root == null || game == null) return;
            RefreshUI();
            root.SetActive(true);
            game.SetEncounterInputBlocked(true);
            inputCooldown = 0.25f;
            game.Sfx("INVENTORY_OPEN");
        }

        public void Close()
        {
            if (root == null || !root.activeSelf) return;
            root.SetActive(false);
            if (game != null)
            {
                game.SetEncounterInputBlocked(false);
                game.Sfx("INVENTORY_CLOSE");
            }
        }

        public void RefreshUI()
        {
            if (game == null || game.progress == null) return;

            int tears = Mathf.FloorToInt(game.progress.tears);
            if (tearsText != null)
                tearsText.text = "NƯỚC MẮT: " + tears.ToString("N0");

            foreach (var card in cards)
            {
                bool owned = game.progress.Owns(card.def.id);
                bool canAfford = tears >= card.def.price;

                if (owned)
                {
                    card.buyButton.interactable = false;
                    card.buyBtnImage.color = new Color(0.16f, 0.14f, 0.16f, 0.9f);
                    card.buyBtnText.text = "ĐÃ SỞ HỮU";
                    card.buyBtnText.color = new Color(0.60f, 0.58f, 0.55f);
                }
                else if (canAfford)
                {
                    card.buyButton.interactable = true;
                    card.buyBtnImage.color = new Color(0.32f, 0.20f, 0.08f, 0.98f);
                    card.buyBtnText.text = "MUA (" + card.def.price.ToString("N0") + ")";
                    card.buyBtnText.color = new Color(1f, 0.92f, 0.65f);
                }
                else
                {
                    card.buyButton.interactable = false;
                    card.buyBtnImage.color = new Color(0.20f, 0.10f, 0.10f, 0.9f);
                    card.buyBtnText.text = "THIẾU NƯỚC MẮT (" + card.def.price.ToString("N0") + ")";
                    card.buyBtnText.color = new Color(0.75f, 0.45f, 0.45f);
                }
            }
        }

        void BuyItem(ShopCard card)
        {
            if (game == null || game.progress == null) return;
            if (game.progress.Owns(card.def.id)) return;
            if (game.progress.tears < card.def.price)
            {
                game.Message(VietnameseSource.Term("ST10_CANDELARIA/DLG_1012_0", "Quay trở lại khi ngài đã có thêm Nước Mắt Chuộc Tội"));
                return;
            }

            game.progress.tears -= card.def.price;
            game.progress.SetOwned(card.def.id, true);
            game.Sfx("ITEM_ADDED");
            game.SaveGame();

            // Notify room visuals so counter shelf updates
            CandelariaMerchantRoom.Instance?.RefreshDisplayItems();

            RefreshUI();

            if (card.catalogItem != null)
                game.itemPopup?.Show(card.catalogItem);
            else
                game.Message("Đã nhận: " + card.def.fallbackName);
        }

        void Update()
        {
            if (!IsOpen) return;
            inputCooldown -= Time.unscaledDeltaTime;
            if (inputCooldown > 0) return;

            var key = Keyboard.current;
            var pad = Gamepad.current;
            if (key != null && (key.escapeKey.wasPressedThisFrame || key.backspaceKey.wasPressedThisFrame) ||
                pad != null && pad.buttonEast.wasPressedThisFrame)
            {
                Close();
            }
        }

        static RectTransform MakeRect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Text MakeLabel(Transform parent, string text, int size, Font font, Color color, TextAnchor align)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = text;
            t.fontSize = size;
            t.font = font;
            t.color = color;
            t.alignment = align;
            t.raycastTarget = false;
            return t;
        }
    }
}
