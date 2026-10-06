with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasInventoryUI.cs', 'r') as f:
    content = f.read()

content = content.replace('float startX = 520f;', 'float startX = 650f;')
content = content.replace('lrt.anchoredPosition = new Vector2(80f, -140f); lrt.sizeDelta = new Vector2(400f, 400f);', 'lrt.anchoredPosition = new Vector2(100f, -120f); lrt.sizeDelta = new Vector2(450f, 450f);')

with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasInventoryUI.cs', 'w') as f:
    f.write(content)
