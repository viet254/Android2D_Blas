import os
from PIL import Image

dir_path = r'D:\game\Android2D_Blas\Assets\Brotherhood\Resources\UI\Sprites'
for f in os.listdir(dir_path):
    if f.startswith('inventory-spritesheet_') and f.endswith('.png'):
        p = os.path.join(dir_path, f)
        img = Image.open(p)
        print(f"{f}: size={img.size}, mode={img.mode}")
