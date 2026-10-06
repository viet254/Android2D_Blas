"""Read-only hierarchy/RectTransform audit of the mobile source inventory scene."""
import re
from pathlib import Path

SCENE = Path(r"D:\game\Bla_mobile_map\ExportedProject\Assets\#Design\Scenes\UI\GenericElements.unity")
HEADER = re.compile(r"(?m)^--- !u!(\d+) &(\d+)\s*$")


def field(block, name):
    match = re.search(r"(?m)^  " + re.escape(name) + r": (.*)$", block)
    return match.group(1).strip() if match else ""


def main():
    text = SCENE.read_text(encoding="utf-8-sig")
    headers = list(HEADER.finditer(text))
    objects, transforms, components = {}, {}, {}
    for index, header in enumerate(headers):
        kind, file_id = header.groups()
        block = text[header.end():headers[index + 1].start() if index + 1 < len(headers) else len(text)]
        if kind == "1":
            objects[file_id] = field(block, "m_Name")
            for component in re.findall(r"(?m)^  - component: \{fileID: (\d+)\}", block):
                components[component] = file_id
        elif kind == "224":
            parent = re.search(r"m_Father: \{fileID: (\d+)\}", block)
            transforms[file_id] = (
                components.get(file_id), parent.group(1) if parent else "",
                field(block, "m_AnchoredPosition"), field(block, "m_SizeDelta"),
                field(block, "m_AnchorMin"), field(block, "m_AnchorMax"),
            )
    children = {}
    for transform_id, (_, parent, *_rest) in transforms.items():
        children.setdefault(parent, []).append(transform_id)
    roots = [fid for fid, (obj, *_rest) in transforms.items() if objects.get(obj) == "UI_NEWINVENTORY"]

    def walk(transform_id, depth=0):
        obj, _parent, pos, size, amin, amax = transforms[transform_id]
        print("  " * depth + f"{objects.get(obj, '?')} [{transform_id}] pos={pos} size={size} anchors={amin}/{amax}")
        if depth < 6:
            for child in children.get(transform_id, []):
                walk(child, depth + 1)

    for root in roots:
        walk(root)


if __name__ == "__main__":
    main()
