with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasOptionsUI.cs', 'r') as f:
    content = f.read()

content = content.replace('const float btnWidth = 520f;', 'const float btnWidth = 420f;')
content = content.replace('const float btnHeight = 72f;', 'const float btnHeight = 56f;')
content = content.replace('const float btnSpacing = 95f;', 'const float btnSpacing = 72f;')
content = content.replace('new Vector2(120 - 300, y)', 'new Vector2(-250, y)') # move cursor left of the new button width

with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\BlasOptionsUI.cs', 'w') as f:
    f.write(content)
