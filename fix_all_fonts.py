import os

runtime_dir = r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime'
for f in os.listdir(runtime_dir):
    if f.endswith('.cs'):
        p = os.path.join(runtime_dir, f)
        with open(p, 'r', encoding='utf-8') as file:
            content = file.read()
        if 'Arial.ttf' in content:
            content = content.replace('Arial.ttf', 'LegacyRuntime.ttf')
            with open(p, 'w', encoding='utf-8') as file:
                file.write(content)
            print(f'Replaced Arial.ttf in {f}')
