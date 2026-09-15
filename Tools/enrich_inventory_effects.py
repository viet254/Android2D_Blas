import json, pathlib, re

SOURCE = pathlib.Path(r"D:/game/Bla_mobile_map/ExportedProject/Assets")
CATALOG = pathlib.Path(r"D:/game/Android2D_Blas/Assets/Brotherhood/Resources/Inventory/catalog.json")

guid_to_script = {}
for meta in (SOURCE / "Scripts").rglob("*.cs.meta"):
    text = meta.read_text(encoding="utf-8-sig", errors="ignore")
    match = re.search(r"^guid:\s*([0-9a-f]+)", text, re.M)
    if match:
        guid_to_script[match.group(1)] = meta.name[:-8]

catalog = json.loads(CATALOG.read_text(encoding="utf-8"))
by_id = {item["id"]: item for item in catalog["items"]}

def scalar(block, name, default=None):
    match = re.search(r"^  " + re.escape(name) + r":\s*([^\r\n]+)", block, re.M)
    if not match:
        return default
    return match.group(1).strip()

effect_count = 0
for category in ("prayer", "relic", "rosarybead", "sword"):
    folder = SOURCE / "Resources" / "inventory" / category
    for prefab in folder.glob("*.prefab"):
        item = by_id.get(prefab.stem)
        if item is None:
            continue
        text = prefab.read_text(encoding="utf-8-sig", errors="ignore")
        description = re.search(r'^  description:\s*(.*)$', text, re.M)
        if description:
            item["description"] = description.group(1).strip().strip('"')
        if category == "prayer":
            needed = re.search(r"^  fervourNeeded:\s*([^\r\n]+)", text, re.M)
            prayer_type = re.search(r"^  prayerType:\s*([^\r\n]+)", text, re.M)
            item["fervourNeeded"] = float(needed.group(1)) if needed else 0
            item["prayerType"] = int(prayer_type.group(1)) if prayer_type else 0
        components = []
        for block in re.split(r"(?=--- !u!114 )", text):
            guid = re.search(r"m_Script:.*?guid:\s*([0-9a-f]+)", block)
            if not guid:
                continue
            script = guid_to_script.get(guid.group(1), "UnknownEffect")
            if script in ("Prayer", "Relic", "RosaryBead", "Sword", "BaseInventoryObject"):
                continue
            component = {"script": script}
            limit_time = scalar(block, "LimitTime")
            effect_time = scalar(block, "EffectTime")
            if limit_time is not None:
                component["limitTime"] = int(limit_time)
            if effect_time is not None:
                component["effectTime"] = float(effect_time)
            stat_type = scalar(block, "statType")
            value = scalar(block, "value")
            if stat_type is not None and value is not None:
                component.update({
                    "statType": int(stat_type),
                    "value": float(value),
                    "effectMode": int(scalar(block, "effectMode", "0")),
                    "valueType": int(scalar(block, "valueType", "0")),
                    "statValueType": int(scalar(block, "statValueType", "0")),
                    "multiplier": float(scalar(block, "multiplier", "1")),
                })
            components.append(component)
        item["effects"] = components
        effect_count += len(components)

CATALOG.write_text(json.dumps(catalog, ensure_ascii=False, indent=2), encoding="utf-8")
print(f"enriched {len(catalog['items'])} items with {effect_count} source effect components")
