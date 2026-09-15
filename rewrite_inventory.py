import re

with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasInventoryUI.cs', 'r') as f:
    content = f.read()

# Add leftGraphicImg field
content = content.replace('private Image cursorImage;', 'private Image cursorImage;\n        private Image leftGraphicImg;')

# Modify BuildUI to create LeftGraphic
build_ui_hook = '// Grid 2x7=14 slots'
left_graphic_code = '''
            // Left Graphic
            var lgo = MakeRect(win.transform, "LeftGraphic");
            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 1); lrt.anchorMax = new Vector2(0, 1);
            lrt.pivot = new Vector2(0, 1); lrt.anchoredPosition = new Vector2(80f, -140f); lrt.sizeDelta = new Vector2(400f, 400f);
            leftGraphicImg = lgo.AddComponent<Image>(); leftGraphicImg.preserveAspect = true;
            leftGraphicImg.gameObject.SetActive(false);
            
            // Grid 14 slots
'''
content = content.replace(build_ui_hook, left_graphic_code)

# Now modify RefreshTab to update layout
refresh_tab_old = '''        void RefreshTab()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null) { img.sprite = LoadSpriteRes(i == currentTab ? "UI/Sprites/Boton_01" : "UI/Sprites/Boton_02"); img.color = Color.white; }
            }
            if (categoryTitleText != null) categoryTitleText.text = CategoryTitles[currentTab];
            PopulateSlots();
            UpdateDetails();
        }'''

refresh_tab_new = '''        void RefreshTab()
        {
            for (int i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null) continue;
                var img = tabButtons[i].GetComponent<Image>();
                if (img != null) { img.sprite = LoadSpriteRes(i == currentTab ? "UI/Sprites/Boton_01" : "UI/Sprites/Boton_02"); img.color = Color.white; }
            }
            if (categoryTitleText != null) categoryTitleText.text = CategoryTitles[currentTab];
            
            // Update left graphic and grid layout
            string bgPath = "";
            int cols = 5;
            float startX = 520f;
            if (currentTab == 0) bgPath = "UI/Sprites/inventory-bg-rosary"; // actually hand? wait, we'll use placeholder or real
            else if (currentTab == 1) bgPath = "UI/Sprites/inventory-bg-prayers";
            else if (currentTab == 2) { bgPath = ""; cols = 7; startX = 260f; } // Quest items
            else if (currentTab == 3) bgPath = "UI/Sprites/inventory-bg-relicary";
            else if (currentTab == 5) bgPath = "UI/Sprites/inventory-bg-osary"; // Collectibles
            
            if (leftGraphicImg != null)
            {
                if (!string.IsNullOrEmpty(bgPath)) {
                    leftGraphicImg.sprite = LoadSpriteRes(bgPath);
                    leftGraphicImg.gameObject.SetActive(leftGraphicImg.sprite != null);
                } else {
                    leftGraphicImg.gameObject.SetActive(false);
                }
            }
            
            // Reposition slots
            float slotGap = 102f;
            float row0Y = -160f, row1Y = -268f;
            for (int i = 0; i < 14; i++)
            {
                int row = i / cols;
                int col = i % cols;
                if (slotIcons[i] != null)
                {
                    var rt = slotIcons[i].transform.parent.GetComponent<RectTransform>();
                    if (row < 2) {
                        rt.anchoredPosition = new Vector2(startX + col * slotGap, row == 0 ? row0Y : row1Y);
                        rt.gameObject.SetActive(true);
                    } else {
                        rt.gameObject.SetActive(false); // hide extra slots if cols=5 (10-13)
                    }
                }
            }
            
            PopulateSlots();
            UpdateDetails();
        }'''

content = content.replace(refresh_tab_old, refresh_tab_new)

with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasInventoryUI.cs', 'w') as f:
    f.write(content)
