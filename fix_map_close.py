with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasMapUI.cs', 'r', encoding='utf-8') as f:
    code = f.read()

old_close = '''            // Close [X] Button
            var closeBtn = MakeButton(header.transform, "CloseBtn", new Vector2(1, 0.5f), new Vector2(-60, 0), new Vector2(52, 52), Hide);
            closeBtn.GetComponent<Image>().color = new Color(0.75f, 0.15f, 0.12f, 0.9f);
            MakeLabel(closeBtn.transform, "X", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, 28, TextAnchor.MiddleCenter, Color.white);'''

new_close = '''            // Close [X] Button
            var closeBtn = MakeButton(header.transform, "CloseBtn", new Vector2(1, 0.5f), new Vector2(-60, 0), new Vector2(56, 34), Hide);
            var cbImg = closeBtn.GetComponent<Image>();
            cbImg.sprite = LoadSprite("UI/Sprites/X");
            cbImg.color = Color.white;
            cbImg.preserveAspect = true;'''

if old_close in code:
    code = code.replace(old_close, new_close)
    with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasMapUI.cs', 'w', encoding='utf-8') as f:
        f.write(code)
    print("Updated Map Close button to authentic X.png")
else:
    print("Old close pattern not found in BlasMapUI.cs")
