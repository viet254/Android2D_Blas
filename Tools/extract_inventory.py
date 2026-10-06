import json,re,pathlib
src=pathlib.Path(r'D:/game/Bla_mobile_map/ExportedProject/Assets/Resources/inventory')
out=pathlib.Path(r'D:/game/Android2D_Blas/Assets/Brotherhood/Resources/Inventory');out.mkdir(parents=True,exist_ok=True)
items=[]
for category in ('prayer','relic','rosarybead','sword'):
 for p in sorted((src/category).glob('*.prefab')):
  text=p.read_text(encoding='utf-8-sig')
  def field(name,default=''):
   m=re.search(r'^  '+re.escape(name)+r':\s*(.*)$',text,re.M)
   return m.group(1).strip().strip('"') if m else default
  ident=field('id',p.stem); caption=field('caption',ident)
  pic=re.search(r'^  picture:.*guid:\s*([0-9a-f]+)',text,re.M)
  items.append({'id':ident,'category':category,'caption':caption,'pictureGuid':pic.group(1) if pic else ''})
(out/'catalog.json').write_text(json.dumps({'items':items},ensure_ascii=False,indent=2),encoding='utf-8')
print('inventory entries',len(items),{c:sum(i['category']==c for i in items) for c in ('prayer','relic','rosarybead','sword')})
