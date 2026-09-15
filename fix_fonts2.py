import os

for root, _, files in os.walk(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime'):
    for file in files:
        if file.endswith('.cs') and 'Blas' in file:
            path = os.path.join(root, file)
            with open(path, 'r') as f:
                content = f.read()
            
            content = content.replace('Resources.Load<Font>("Fonts/Caudex-Bold")', 'Resources.GetBuiltinResource<Font>("Arial.ttf")')
            content = content.replace('Resources.Load<Font>("Fonts/Caudex-Regular")', 'Resources.GetBuiltinResource<Font>("Arial.ttf")')
            with open(path, 'w') as f:
                f.write(content)
            print(f"Bypassed Caudex in {file}")
