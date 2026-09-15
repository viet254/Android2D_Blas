"""Refresh optional platform markers and provenance from the 25 source scenes."""
import json,re,subprocess
from extract_brotherhood import SOURCE,ROOT,documents,ref,vec

def augment():
    path=ROOT/'Assets/Brotherhood/SourceData/brotherhood.json'
    data=json.loads(path.read_text())
    tiled=set()
    faith_guid=re.search(r'^guid: (\w+)',(SOURCE/'Scripts/Assembly-CSharp/Tools/Level/Actionables/FaithPlatform.cs.meta').read_text(),re.M)[1]
    for room in data['rooms']:
        for node in room['nodes']:node['hasRenderer']='renderer' in node
        room['markers']=[m for m in room['markers'] if m['kind']!='FaithPlatform']
        for scene in SOURCE.glob('#Design/**/'+room['id']+'_*.unity'):
            section=scene.stem.rsplit('_',1)[1];docs=documents(scene)
            transforms={ref(d.get('m_GameObject')):i for i,(k,d) in docs.items() if k=='Transform'}
            for n in room['nodes']:
                if n['section']==section:n['gameObject']=ref(docs[int(n['id'].split('_')[1])][1].get('m_GameObject'))
            by_go={n['gameObject']:n for n in room['nodes'] if n['section']==section}
            for _,(kind,sr) in docs.items():
                if kind=='SpriteRenderer' and ref(sr.get('m_GameObject')) in by_go:
                    renderer=by_go[ref(sr['m_GameObject'])]['renderer']
                    renderer.update(drawMode=sr.get('m_DrawMode',0),size=vec(sr.get('m_Size')),
                                    tileMode=sr.get('m_SpriteTileMode',0),adaptive=sr.get('m_AdaptiveModeThreshold',.5),mask=sr.get('m_MaskInteraction',0))
                    if renderer['drawMode'] and renderer['sprite']:tiled.add(renderer['sprite'])
            for _,(k,d) in docs.items():
                if k!='MonoBehaviour' or d.get('m_Script',{}).get('guid')!=faith_guid:continue
                def node_for_component(key):
                    cid=ref(d.get(key));go=ref(docs[cid][1].get('m_GameObject'))
                    return section+'_'+str(transforms[go])
                cd=docs[ref(d['collision'])][1]
                room['markers'].append({'kind':'FaithPlatform','node':section+'_'+str(transforms[ref(d['m_GameObject'])]),
                    'first':bool(d.get('firstPlatform')), 'delay':d.get('deactivationDelay',3),
                    'rendererNode':node_for_component('spriteRenderer'),'colliderNode':node_for_component('collision'),
                    'targets':[section+'_'+str(transforms[ref(t)]) for t in d.get('target',[]) if ref(t) in transforms],
                    'size':vec(cd.get('m_Size'),(2,.25,0)),'offset':vec(cd.get('m_Offset')),'data':json.dumps(d)})
    lookup={s['id']:s for s in data['sprites']}
    for guid in tiled:
        p=SOURCE/'Sprite'/(lookup[guid]['name']+'.asset')
        if not p.exists() or re.search(r'^guid: (\w+)',p.with_suffix('.asset.meta').read_text(),re.M)[1]!=guid:
            raise RuntimeError('Cannot resolve tiled sprite border by verified identity: '+guid)
        sprite=next(d for k,d in documents(p).values() if k=='Sprite')
        lookup[guid]['border']=sprite.get('m_Border',{})
    path.write_text(json.dumps(data,separators=(',',':')))
    print('Faith platforms:',sum(m['kind']=='FaithPlatform' for r in data['rooms'] for m in r['markers']))

if __name__=='__main__':augment()
