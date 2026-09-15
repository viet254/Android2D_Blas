import re

with open(r'D:\game\Bla_mobile_map\ExportedProject\Assets\#Design\Scenes\UI\GenericElements.unity', 'r', encoding='utf-8') as f:
    text = f.read()

objects = {}
for match in re.finditer(r'--- !u!1 &(\d+)\nGameObject:(.*?)(?=\n---|\Z)', text, re.DOTALL):
    obj_id = match.group(1)
    content = match.group(2)
    name_match = re.search(r'm_Name:\s*(.*)', content)
    name = name_match.group(1).strip() if name_match else 'Unknown'
    comp_matches = re.findall(r'- component: \{fileID: (\d+)\}', content)
    objects[obj_id] = {'name': name, 'components': comp_matches}

rects = {}
for match in re.finditer(r'--- !u!224 &(\d+)\nRectTransform:(.*?)(?=\n---|\Z)', text, re.DOTALL):
    rt_id = match.group(1)
    content = match.group(2)
    go_match = re.search(r'm_GameObject:\s*\{fileID: (\d+)\}', content)
    go_id = go_match.group(1) if go_match else None
    children_block = re.search(r'm_Children:\s*((?:- \{fileID: \d+\}\s*)*)', content)
    children_matches = re.findall(r'- \{fileID: (\d+)\}', children_block.group(1)) if children_block else []
    pos_match = re.search(r'm_AnchoredPosition:\s*\{x: ([^,]+), y: ([^\}]+)\}', content)
    size_match = re.search(r'm_SizeDelta:\s*\{x: ([^,]+), y: ([^\}]+)\}', content)
    amin_match = re.search(r'm_AnchorMin:\s*\{x: ([^,]+), y: ([^\}]+)\}', content)
    amax_match = re.search(r'm_AnchorMax:\s*\{x: ([^,]+), y: ([^\}]+)\}', content)
    pivot_match = re.search(r'm_Pivot:\s*\{x: ([^,]+), y: ([^\}]+)\}', content)
    rects[rt_id] = {
        'go_id': go_id,
        'children': children_matches,
        'pos': pos_match.groups() if pos_match else ('N/A', 'N/A'),
        'size': size_match.groups() if size_match else ('N/A', 'N/A'),
        'amin': amin_match.groups() if amin_match else ('N/A', 'N/A'),
        'amax': amax_match.groups() if amax_match else ('N/A', 'N/A'),
        'pivot': pivot_match.groups() if pivot_match else ('N/A', 'N/A'),
    }

def print_tree(rt_id, depth=0):
    rt = rects.get(rt_id)
    if not rt: return
    go = objects.get(rt['go_id'])
    name = go['name'] if go else 'Unknown'
    p = rt['pos']
    s = rt['size']
    ami = rt['amin']
    ama = rt['amax']
    piv = rt['pivot']
    print('  ' * depth + f'- {name} [pos={p}, size={s}, aMin={ami}, aMax={ama}, pivot={piv}]')
    for child_id in rt['children']:
        print_tree(child_id, depth+1)

for rt_id, rt in rects.items():
    go = objects.get(rt['go_id'])
    if go and go['name'] == 'Inventory':
        print_tree(rt_id)
