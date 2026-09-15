import re, os
from PIL import Image

tex_path = r'D:\game\Bla_mobile_map\ExportedProject\Assets\Texture2D\inventory-spritesheet.png'
sprite_dir = r'D:\game\Bla_mobile_map\ExportedProject\Assets\Sprite'
out_dir = r'D:\game\Android2D_Blas\temp_sprites'
os.makedirs(out_dir, exist_ok=True)

img = Image.open(tex_path)

for filename in os.listdir(sprite_dir):
    if filename.startswith('inventory-spritesheet_') and filename.endswith('.asset'):
        with open(os.path.join(sprite_dir, filename), 'r') as f:
            content = f.read()
            rect_match = re.search(r'm_Rect:.*?x: (\d+).*?y: (\d+).*?width: (\d+).*?height: (\d+)', content, re.DOTALL)
            if rect_match:
                x, y, w, h = map(int, rect_match.groups())
                crop = img.crop((x, img.height - y - h, x + w, img.height - y))
                out_name = filename.replace('.asset', '.png')
                crop.save(os.path.join(out_dir, out_name))
