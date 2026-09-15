"""Read-only source export -> portable room/animation data. Requires PyYAML.
Run with Python from any directory; never modifies the AssetRipper source.
"""
import json, os, re, shutil, sys
from pathlib import Path
import yaml

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(os.environ.get('BLASPHEMOUS_SOURCE', 'D:/game/Bla_mobile_map/ExportedProject/Assets'))
OUT = ROOT / 'Assets/Brotherhood/SourceData'
ROOMS = ['D17Z01S01', 'D17Z01S02', 'D17Z01S05', 'D17Z01S11', 'D17Z01S03']
LOADER = getattr(yaml, 'CSafeLoader', yaml.SafeLoader)

def documents(path):
    text = path.read_text(encoding='utf-8-sig')
    result = {}
    parts = re.split(r'^--- !u!(\d+) &(-?\d+).*\n', text, flags=re.M)
    for i in range(1, len(parts), 3):
        doc = yaml.load(parts[i+2], Loader=LOADER)
        if doc:
            kind, data = next(iter(doc.items()))
            result[int(parts[i+1])] = (kind, data)
    return result

def ref(x): return x.get('fileID', 0) if isinstance(x, dict) else 0
def vec(x, default=(0,0,0)):
    x = x or {}
    return {k:float(x.get(k, v)) for k,v in zip('xyz', default)}

def main():
    OUT.mkdir(parents=True, exist_ok=True)
    index = {}
    for p in SOURCE.rglob('*.meta'):
        m = re.search(r'^guid: (\w+)', p.read_text(encoding='utf-8-sig'), re.M)
        if m: index[m[1]] = Path(str(p)[:-5])
    print('Indexed', len(index), flush=True)
    cache = {}
    def load(p):
        if p not in cache: cache[p] = documents(p)
        return cache[p]
    def script(d):
        p = index.get(d.get('m_Script', {}).get('guid'))
        return p.stem if p else 'MissingScript'
    sprite_guids = set()
    animation_guids = set()
    warnings = []
    report = {'rooms': [], 'missing': [], 'source': str(SOURCE)}
    rooms = []
    for room_id in ROOMS:
        room = {'id':room_id, 'nodes':[], 'markers':[]}
        for scene in sorted(SOURCE.glob(f'#Design/**/{room_id}_*.unity')):
            docs = load(scene)
            section = scene.stem.rsplit('_',1)[1]
            transform_ids = {ref(d.get('m_GameObject')):i for i,(k,d) in docs.items() if k in ('Transform','RectTransform')}
            for go_id,(kind,go) in docs.items():
                if kind != 'GameObject': continue
                ti = transform_ids.get(go_id)
                if ti is None: continue
                t = docs[ti][1]
                components = [(ci, docs[ci]) for c in go.get('m_Component',[]) if (ci:=ref(c.get('component'))) in docs]
                scripts = [(script(d),d) for _,(k,d) in components if k=='MonoBehaviour']
                node = {'id':f'{section}_{ti}', 'gameObject':go_id, 'parent':f'{section}_{ref(t.get("m_Father"))}',
                        'name':go.get('m_Name','Object'), 'active':bool(go.get('m_IsActive',1)),
                        'position':vec(t.get('m_LocalPosition')), 'scale':vec(t.get('m_LocalScale'),(1,1,1)),
                        'rotation':t.get('m_LocalRotation',{'x':0,'y':0,'z':0,'w':1}),
                        'layer':int(go.get('m_Layer',0)), 'section':section, 'colliders':[], 'hasRenderer':False}
                for ci,(k,d) in components:
                    if k == 'SpriteRenderer':
                        node['hasRenderer']=True
                        g=d.get('m_Sprite',{}).get('guid','')
                        if g: sprite_guids.add(g)
                        visible=bool(d.get('m_Enabled',1))
                        for sn,sd in scripts:
                            if sn=='LayoutElement' and not sd.get('showInGame',False): visible=False
                        node['renderer']={'sprite':g,'enabled':visible,'color':d.get('m_Color',{'r':1,'g':1,'b':1,'a':1}),
                                          'order':d.get('m_SortingOrder',0),'sortingLayer':d.get('m_SortingLayerID',0),
                                          'flipX':bool(d.get('m_FlipX',0)),'flipY':bool(d.get('m_FlipY',0)),
                                          'drawMode':d.get('m_DrawMode',0),'size':vec(d.get('m_Size')),'tileMode':d.get('m_SpriteTileMode',0),'adaptive':d.get('m_AdaptiveModeThreshold',.5),'mask':d.get('m_MaskInteraction',0)}
                    elif k.endswith('Collider2D'):
                        paths=d.get('m_Points',{}).get('m_Paths',[]) if isinstance(d.get('m_Points'),dict) else []
                        if k=='EdgeCollider2D': paths=[d.get('m_Points',[])]
                        node['colliders'].append({'kind':k,'enabled':bool(d.get('m_Enabled',1)), 'trigger':bool(d.get('m_IsTrigger',0)),
                             'offset':vec(d.get('m_Offset')), 'size':vec(d.get('m_Size'),(1,1,0)), 'radius':d.get('m_Radius',0.5),
                             'paths':[{'points':[vec(v) for v in path]} for path in paths]})
                    elif k=='Animator':
                        ag=d.get('m_Controller',{}).get('guid')
                        if ag and ag in index:
                            for _,(ak,ad) in load(index[ag]).items():
                                if ak=='AnimatorState':
                                    mg=ad.get('m_Motion',{}).get('guid')
                                    if mg and index.get(mg,Path('')).suffix=='.anim': animation_guids.add(mg)
                for sn,sd in scripts:
                    if sn in ('Door','PrieDieu','DebugSpawn','EnemySpawnPoint','CameraNumericBoundaries','ParallaxController','ElderBrother','CherubCaptorSpawnConfigurator'):
                        marker={'kind':sn,'node':node['id'],'name':node['name'],'data':json.dumps(sd)}
                        if sn=='Door':
                            marker.update(target=sd.get('targetScene',''),door=sd.get('targetDoor',''),key=sd.get('identificativeName',''),spawn=f'{section}_{ref(sd.get("spawnPoint"))}')
                        if sn=='CameraNumericBoundaries':
                            marker.update(left=sd.get('LeftBoundary',-100),right=sd.get('RightBoundary',100),bottom=sd.get('BottomBoundary',-100),top=sd.get('TopBoundary',100))
                        room['markers'].append(marker)
                    if sn=='MissingScript': warnings.append(f'{room_id}/{section}/{node["name"]}: missing script')
                room['nodes'].append(node)
        rooms.append(room)
        report['rooms'].append({'id':room_id,'nodes':len(room['nodes']),'markers':room['markers']})
        print(room_id,len(room['nodes']),'nodes',flush=True)
    # Include original actor animation families, not their old scripts/controllers.
    prefixes=('player_', 'penitent_', 'elderbrother_', 'acolyte_', 'acolyteb_', 'flagellant_', 'newflagellant_', 'priedieu_', 'chargedattackprojectile_', 'slash_clamped_attack_',
              'alliedcherub_', 'penitentbeam_', 'flamepillar_', 'prayerpr12', 'pontiffoldman_toxic')
    for g,p in index.items():
        if p.suffix=='.anim' and p.stem.lower().startswith(prefixes): animation_guids.add(g)
    animations=[]
    for g in sorted(animation_guids):
        p=index[g]; ds=load(p)
        d=next((d for k,d in ds.values() if k=='AnimationClip'),{})
        curves=d.get('m_PPtrCurves',[])
        curve=next((c for c in curves if c.get('attribute')=='m_Sprite'),None)
        if not curve: continue
        frames=[]
        for key in curve.get('curve',[]):
            sg=key.get('value',{}).get('guid','')
            if sg:
                sprite_guids.add(sg);frames.append({'time':float(key['time']),'sprite':sg})
        if frames:
            settings=d.get('m_AnimationClipSettings',{})
            animations.append({'name':p.stem,'frames':frames,'duration':float(settings.get('m_StopTime',frames[-1]['time']+0.08)), 'loop':bool(settings.get('m_LoopTime',0))})
    sprites=[];textures={}
    for g in sorted(sprite_guids):
        p=index.get(g)
        if not p:
            warnings.append('Missing sprite '+g);continue
        d=next((d for k,d in load(p).values() if k=='Sprite'),None)
        if not d: continue
        rd=d.get('m_RD',{}); tg=rd.get('texture',{}).get('guid'); tp=index.get(tg)
        if not tp or tp.suffix!='.png': warnings.append('Missing texture '+str(tp));continue
        rect=rd.get('textureRect',d['m_Rect']); trim=rd.get('textureRectOffset',{})
        original=d['m_Rect']; pivot=d.get('m_Pivot',{'x':.5,'y':.5})
        pivot={'x':(pivot['x']*original['width']-trim.get('x',0))/max(1,rect['width']),
               'y':(pivot['y']*original['height']-trim.get('y',0))/max(1,rect['height'])}
        sprites.append({'id':g,'name':d['m_Name'],'texture':tg,'rect':rect,'pivot':pivot,'border':d.get('m_Border',{}),'ppu':d.get('m_PixelsToUnits',32),'packing':rd.get('settingsRaw',0)})
        textures[tg]=tp
    for g,p in textures.items():
        dst=OUT/'Textures'/f'{g}.png';dst.parent.mkdir(exist_ok=True)
        if not dst.exists():shutil.copy2(p,dst)
    package={'rooms':rooms,'sprites':sprites,'animations':animations}
    (OUT/'brotherhood.json').write_text(json.dumps(package,separators=(',',':')),encoding='utf-8')
    report.update(sprites=len(sprites),textures=len(textures),animations=len(animations),warnings=warnings,
                  packedSprites=sum(1 for s in sprites if s['packing']&1),textureSources={g:str(p) for g,p in textures.items()})
    audit=ROOT/'Documentation';audit.mkdir(exist_ok=True)
    (audit/'source-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Complete',len(sprites),'sprites',len(textures),'textures',len(animations),'clips; warnings',len(warnings),flush=True)

if __name__=='__main__':main()
