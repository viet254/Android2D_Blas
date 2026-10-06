"""Copy skill costs and prerequisites from the original mobile skill assets."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(r"D:\game\Bla_mobile_map\ExportedProject\Assets\Resources\skill")
OUTPUT = ROOT / "Assets/Brotherhood/Resources/Inventory/skill-source.json"


def field(data, name):
    match = re.search(r"(?m)^  " + re.escape(name) + r": (.*)$", data)
    return match.group(1).strip() if match else ""


def main():
    skills = []
    for path in sorted(SOURCE.glob("*.asset")):
        data = path.read_text(encoding="utf-8-sig")
        identifier = field(data, "id")
        if not re.match(r"^(CHARGED|COMBO|LUNGE|RANGED|VERTICAL)_[1-3]$", identifier):
            continue
        skills.append({
            "id": identifier,
            "cost": int(field(data, "cost")),
            "parentSkill": field(data, "parentSkill"),
            "tier": int(field(data, "tier")),
        })
    if len(skills) != 15:
        raise RuntimeError(f"Expected 15 source skills, found {len(skills)}")
    OUTPUT.write_text(json.dumps({"skills": skills}, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"Imported {len(skills)} original mobile skill records")


if __name__ == "__main__":
    main()
