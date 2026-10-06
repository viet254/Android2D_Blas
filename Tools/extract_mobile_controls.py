"""Copy the original Android touch-control art recovered by AssetRipper."""
from pathlib import Path
import shutil, json

ROOT=Path(__file__).resolve().parents[1]
SOURCE=Path(r"D:/game/Bla_mobile_map/ExportedProject/Assets/Texture2D")
DEST=ROOT/"Assets/Brotherhood/Resources/MobileControls"
NAMES=["Attack","Dash","Jump","Parry","Flask","Prayer","RangeAttack","Map","Inventory","HD_BaseJoystick","HD_ControlJoystick"]
DEST.mkdir(parents=True,exist_ok=True)
items=[]
for name in NAMES:
    src=SOURCE/(name+".png")
    if not src.exists(): raise FileNotFoundError(src)
    dst=DEST/(name+".png")
    shutil.copyfile(src,dst)
    items.append({"name":name,"source":str(src),"destination":str(dst.relative_to(ROOT))})
(ROOT/"Documentation/mobile-control-provenance.json").write_text(json.dumps(items,indent=2),encoding="utf-8")
print("Copied",len(items),"original Android control textures")
