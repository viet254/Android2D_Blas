import os

for root, _, files in os.walk(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime'):
    for file in files:
        if file.endswith('.cs'):
            path = os.path.join(root, file)
            with open(path, 'r') as f:
                content = f.read()
            
            if 'LegacyRuntime.ttf' in content:
                content = content.replace('LegacyRuntime.ttf', 'Arial.ttf')
                with open(path, 'w') as f:
                    f.write(content)
                print(f"Fixed fonts in {file}")
