"""Import the Vietnamese text stored in the English slot of Bla_loc_map.

The game's localization export labels slot 1 "English", but the older
Bla_loc_map export contains Vietnamese there. The mobile export supplies the
matching English text for the same I2 term keys. This script copies source
strings only; it does not machine-translate or alter the original files.
"""

import json
from pathlib import Path

import yaml


ROOT = Path(__file__).resolve().parents[1]
LOCALIZED = Path(r"D:\game\Bla_loc_map\ExportedProject\Assets\Resources")
MOBILE = Path(r"D:\game\Bla_mobile_map\ExportedProject\Assets\Resources")
SOURCES = (
    ("common", "I2Languages.prefab", "I2Languages.asset"),
    ("dialog", "dialog/Languages.prefab", "dialog/Languages.prefab"),
    ("inventory", "inventory/Languages.prefab", "inventory/Languages.prefab"),
)
OUTPUT = ROOT / "Assets/Brotherhood/Resources/Localization/SourceVietnamese.json"


def scalar(value):
    if not value or value in ("~", "null"):
        return ""
    if value[0] in "'\"":
        parsed = yaml.safe_load(value)
        return "" if parsed is None else str(parsed)
    return value


def read_terms(path):
    terms = {}
    term = None
    languages = []
    in_languages = False
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        value = line.strip()
        if value.startswith("- Term:"):
            if term is not None:
                terms[term] = languages
            term = scalar(value[len("- Term:"):].strip())
            languages = []
            in_languages = False
        elif value == "Languages:" and term is not None:
            in_languages = True
        elif in_languages and value.startswith("- "):
            languages.append(scalar(value[2:].strip()))
        elif in_languages:
            in_languages = False
    if term is not None:
        terms[term] = languages
    return terms


def main():
    entries = []
    for group, local_rel, mobile_rel in SOURCES:
        localized = read_terms(LOCALIZED / local_rel)
        english = read_terms(MOBILE / mobile_rel)
        for term, strings in localized.items():
            if len(strings) < 2 or not strings[1].strip():
                continue
            source_english = english.get(term, [])
            entries.append({
                "group": group,
                "term": term,
                "english": source_english[1] if len(source_english) > 1 else "",
                "vietnamese": strings[1],
            })
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    OUTPUT.write_text(
        json.dumps({"entries": entries}, ensure_ascii=False, separators=(",", ":")),
        encoding="utf-8",
    )
    print(f"Imported {len(entries)} original Vietnamese I2 entries to {OUTPUT}")
    for key in ("Achievements/AC01_NAME", "Prayer/PR01_CAPTION",
                "ST01_DEOSGRACIAS/DLG_0101_0"):
        match = next((x for x in entries if x["term"] == key), None)
        print(key, "=>", ascii(match["vietnamese"]) if match else "missing")


if __name__ == "__main__":
    main()
