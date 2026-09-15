import os
from PIL import Image

dir_path = r'D:\game\Bla_mobile_map\ExportedProject\Assets\Texture2D'
for f in os.listdir(dir_path):
    if f.startswith('Boton') and f.endswith('.png'):
        p = os.path.join(dir_path, f)
        img = Image.open(p)
        print(f"{f}: size={img.size}")
