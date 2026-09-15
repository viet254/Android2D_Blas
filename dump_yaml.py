import yaml, sys
def find_gameobjects(data):
    gos = {}
    for doc in data:
        if 'GameObject' in doc:
            gos[doc['GameObject']['m_LocalIdentfierInFile']] = doc['GameObject']
    return gos
def find_rects(data):
    rts = {}
    for doc in data:
        if 'RectTransform' in doc:
            rts[doc['RectTransform']['m_LocalIdentfierInFile']] = doc['RectTransform']
    return rts
with open(r"D:\game\Bla_mobile_map\ExportedProject\Assets\#Design\Scenes\UI\GenericElements.unity", "r", encoding="utf-8") as f:
    text = f.read()
    # Remove Unity's specific yaml tags
    lines = []
    for line in text.split('\n'):
        if line.startswith('--- !u!'):
            lines.append('---')
        else:
            lines.append(line)
    docs = list(yaml.safe_load_all('\n'.join(lines)))
gos = find_gameobjects(docs)
rts = find_rects(docs)
def print_tree(go_id, depth=0):
    go = gos.get(go_id)
    if not go: return
    name = go.get('m_Name', 'Unknown')
    rt = None
    for comp in go.get('m_Component', []):
        rt_id = comp.get('component', {}).get('fileID')
        if rt_id in rts: rt = rts[rt_id]
    
    pos = "N/A"
    size = "N/A"
    anchorMin = "N/A"
    anchorMax = "N/A"
    pivot = "N/A"
    if rt:
        pos = rt.get('m_AnchoredPosition', 'N/A')
        size = rt.get('m_SizeDelta', 'N/A')
        anchorMin = rt.get('m_AnchorMin', 'N/A')
        anchorMax = rt.get('m_AnchorMax', 'N/A')
        pivot = rt.get('m_Pivot', 'N/A')
    
    print("  " * depth + f"- {name} [pos={pos}, size={size}, aMin={anchorMin}, aMax={anchorMax}, pivot={pivot}]")
    if rt and 'm_Children' in rt:
        for child in rt['m_Children']:
            child_rt_id = child.get('fileID')
            if child_rt_id in rts:
                child_rt = rts[child_rt_id]
                child_go_id = child_rt.get('m_GameObject', {}).get('fileID')
                if child_go_id: print_tree(child_go_id, depth+1)

for gid, go in gos.items():
    if go.get('m_Name') in ('LeftPart', 'SPECIAL'):
        print_tree(gid)
