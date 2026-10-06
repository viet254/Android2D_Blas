"""Decode Unity's hex byte list before YAML interprets it as an octal number."""
import json,re,struct
from extract_brotherhood import ROOT,documents
from import_priority_two_effects import index_sources

def main():
    flags=[];index=index_sources()
    for path in index.values():
        if path.suffix!='.prefab' or 'inventory' not in path.parts or path.stem not in {'PR01','PR03','PR04','PR09','PR11','PR14','PR16','RB38','RB39','RB40','HE201'}:continue
        text=path.read_text(encoding='utf-8-sig');values=[]
        for block in re.split(r'(?m)^--- ',text):
            if 'guid: 02ea84a2d08bc8a991443f314732afc9' not in block:continue
            match=re.search(r'(?m)^  effects: ([0-9a-fA-F]+)$',block)
            if match:
                raw=bytes.fromhex(match[1]);values.extend(struct.unpack('<'+'i'*(len(raw)//4),raw))
        flags.append(dict(item=path.stem,effects=values))
    out=ROOT/'Assets/Brotherhood/Resources/Inventory/temporal-effects-source.json';out.write_text(json.dumps(dict(items=sorted(flags,key=lambda x:x['item'])),separators=(',',':')),encoding='utf-8')
    print('Decoded exact source enum lists:',flags)
if __name__=='__main__':main()
