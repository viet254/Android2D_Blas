import os
from PIL import Image

dir_path = r'D:\game\Bla_mobile_map\ExportedProject\Assets\Texture2D'
for f in ['X.png', 'X_02.png', 'X_1.png']:
    p = os.path.join(dir_path, f)
    if os.path.exists(p):
        img = Image.open(p)
        print(f"{f}: size={img.size}")
