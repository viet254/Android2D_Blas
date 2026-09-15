import json,re,pathlib,sys
from PIL import Image
sys.path.insert(0,str(pathlib.Path(__file__).parent))
from extract_brotherhood import documents
source=pathlib.Path(r'D:/game/Bla_mobile_map/ExportedProject/Assets')
out=pathlib.Path(r'D:/game/Android2D_Blas/Assets/Brotherhood/Resources/Inventory/Icons');out.mkdir(parents=True,exist_ok=True)
index={}
for meta in source.rglob('*.meta'):
 try:text=meta.read_text(encoding='utf-8-sig')
 except:continue
 m=re.search(r'^guid: (\w+)',text,re.M)
 if m:index[m.group(1)]=pathlib.Path(str(meta)[:-5])
catalog_path=out.parent/'catalog.json';catalog=json.loads(catalog_path.read_text(encoding='utf-8'))
done=0
for item in catalog['items']:
 p=index.get(item.get('pictureGuid',''))
 if not p or not p.exists():continue
 try: docs=documents(p); sprite=next(d for k,d in docs.values() if k=='Sprite');rd=sprite.get('m_RD',{});rect=rd.get('textureRect') or sprite.get('m_Rect');tg=rd.get('texture',{}).get('guid');tex=index.get(tg)
 except Exception:continue
 if not tex or tex.suffix.lower()!='.png':continue
 im=Image.open(tex).convert('RGBA');x=int(round(rect['x']));y=int(round(rect['y']));w=int(round(rect['width']));h=int(round(rect['height']));crop=im.crop((x,im.height-y-h,x+w,im.height-y));crop.save(out/(item['id']+'.png'));item['icon']='Inventory/Icons/'+item['id'];done+=1
catalog_path.write_text(json.dumps(catalog,ensure_ascii=False,indent=2),encoding='utf-8')
print('icons',done,'of',len(catalog['items']))
