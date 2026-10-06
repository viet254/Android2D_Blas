"""Read-only source export -> portable room/animation data. Requires PyYAML.
Run with Python from any directory; never modifies the AssetRipper source.
"""
import json, math, os, re, shutil, sys
from pathlib import Path
import yaml

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path(os.environ.get('BLASPHEMOUS_SOURCE', 'D:/game/Bla_mobile_map/ExportedProject/Assets'))
OUT = ROOT / 'Assets/Brotherhood/SourceData'
# Source door chain from Deogracias to the first Mea Culpa skill altar:
# D01Z01S07 -> S01 -> S02 -> S03 -> D01Z02S01 -> S02 -> S06.
ROOMS = ['D17Z01S01', 'D17Z01S02', 'D17Z01S05', 'D17Z01S11', 'D17Z01S03',
         'D01Z01S07', 'D01Z01S01', 'D01Z01S02', 'D01Z01S03',
         'D01Z02S01', 'D01Z02S02', 'D01Z02S06']
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

def animation_data(path, data):
    """Keep the source timeline, including keys which clear the sprite."""
    curve = next((c for c in data.get('m_PPtrCurves', []) if c.get('attribute') == 'm_Sprite'), None)
    if not curve:
        return None
    frames = [{'time': float(k['time']), 'sprite': (k.get('value') or {}).get('guid', '')}
              for k in curve.get('curve', [])]
    if not frames:
        return None
    settings = data.get('m_AnimationClipSettings', {})
    tracks = []
    for source in data.get('m_FloatCurves', []):
        keys = []
        for key in source.get('curve', {}).get('m_Curve', []):
            incoming, outgoing = float(key.get('inSlope', 0)), float(key.get('outSlope', 0))
            keys.append({'time': float(key['time']), 'value': float(key['value']),
                         'inSlope': incoming if math.isfinite(incoming) else 0,
                         'outSlope': outgoing if math.isfinite(outgoing) else 0,
                         'inStep': not math.isfinite(incoming), 'outStep': not math.isfinite(outgoing),
                         'tangentMode': key.get('tangentMode', 0), 'weightedMode': key.get('weightedMode', 0),
                         'inWeight': key.get('inWeight', 1/3), 'outWeight': key.get('outWeight', 1/3)})
        tracks.append({'attribute': source['attribute'], 'path': source.get('path') or '',
                       'classID': source.get('classID', 0), 'keys': keys,
                       'preInfinity': source['curve'].get('m_PreInfinity', 2),
                       'postInfinity': source['curve'].get('m_PostInfinity', 2)})
    events = []
    for event in data.get('m_Events', []):
        obj = event.get('objectReferenceParameter') or {}
        events.append({'time': float(event['time']), 'functionName': event.get('functionName') or '',
                       'stringParameter': event.get('data') or '', 'floatParameter': event.get('floatParameter', 0),
                       'intParameter': event.get('intParameter', 0), 'objectGuid': obj.get('guid', ''),
                       'objectFileID': ref(obj), 'messageOptions': event.get('messageOptions', 0)})
    return {'name': path.stem, 'frames': frames, 'spritePath': curve.get('path') or '',
            'duration': float(settings.get('m_StopTime', frames[-1]['time'] + .08)),
            'loop': bool(settings.get('m_LoopTime', 0)), 'events': events, 'floatTracks': tracks}
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
    def sensor_box(docs, transform_ids, sensor_ref, section):
        sensor = docs.get(ref(sensor_ref))
        sensor_go = ref(sensor[1].get('m_GameObject')) if sensor else 0
        sensor_t = transform_ids.get(sensor_go)
        if not sensor_t or sensor_go not in docs: return None
        for component in docs[sensor_go][1].get('m_Component', []):
            c = docs.get(ref(component.get('component')))
            if c and c[0] == 'BoxCollider2D':
                return {'spawn': f'{section}_{sensor_t}', 'size': vec(c[1].get('m_Size')),
                        'offset': vec(c[1].get('m_Offset'))}
        return None
    def default_clip(controller):
        ds = load(controller)
        machine = next((d for k,d in ds.values() if k == 'AnimatorStateMachine'), None)
        state = ds.get(ref(machine.get('m_DefaultState'))) if machine else None
        motion = state[1].get('m_Motion', {}) if state else {}
        path = index.get(motion.get('guid'))
        return path.stem if path and path.suffix == '.anim' else ''
    sprite_guids = set()
    animation_guids = set()
    mesh_guids = set()
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
                        'layer':int(go.get('m_Layer',0)), 'section':section, 'colliders':[], 'hasRenderer':False,
                        'animation':'','mesh':None}
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
                            clip=default_clip(index[ag])
                            if (section=='DECO' or node['name'].startswith(('AlberoNPC','albero_npc','BodyKisser','BodyTirso'))
                                    or clip=='collectable_object_anim'):
                                node['animation']=clip
                            for _,(ak,ad) in load(index[ag]).items():
                                if ak=='AnimatorState':
                                    mg=ad.get('m_Motion',{}).get('guid')
                                    if mg and index.get(mg,Path('')).suffix=='.anim': animation_guids.add(mg)
                    elif k=='MeshFilter':
                        mg=d.get('m_Mesh',{}).get('guid')
                        if mg and mg in index:
                            mesh_guids.add(mg)
                            node['mesh']={'asset':mg,'enabled':True,'color':{'r':1,'g':1,'b':1,'a':1},'sortingLayer':0,'order':0}
                    elif k=='MeshRenderer' and node['mesh']:
                        node['mesh']['enabled']=bool(d.get('m_Enabled',1))
                        node['mesh']['sortingLayer']=int(d.get('m_SortingLayerID',0))
                        node['mesh']['order']=int(d.get('m_SortingOrder',0))
                        material_ref=(d.get('m_Materials') or [{}])[0]
                        material_path=index.get(material_ref.get('guid'))
                        if material_path:
                            material=next((v for mk,v in load(material_path).values() if mk=='Material'),{})
                            node['mesh']['color']=material.get('m_SavedProperties',{}).get('m_Colors',{}).get('_Color',node['mesh']['color'])
                for sn,sd in scripts:
                    if sn in ('Door','PrieDieu','DebugSpawn','EnemySpawnPoint','CameraNumericBoundaries','ParallaxController','ElderBrother','CherubCaptorSpawnConfigurator'):
                        marker={'kind':sn,'node':node['id'],'name':node['name'],'data':json.dumps(sd)}
                        if sn=='Door':
                            marker.update(target=sd.get('targetScene',''),door=sd.get('targetDoor',''),key=sd.get('identificativeName',''),spawn=f'{section}_{ref(sd.get("spawnPoint"))}')
                        if sn=='CameraNumericBoundaries':
                            marker.update(left=sd.get('LeftBoundary',-100),right=sd.get('RightBoundary',100),bottom=sd.get('BottomBoundary',-100),top=sd.get('TopBoundary',100))
                        room['markers'].append(marker)
                    if sn=='MissingScript': warnings.append(f'{room_id}/{section}/{node["name"]}: missing script')
                if section=='LOGIC' and node['name']=='ACT_MeaCulpaAltar':
                    interaction=next((sd for sn,sd in scripts if sn=='CustomInteraction'),None)
                    if interaction:
                        for sensor_ref in interaction.get('sensors',[]):
                            box=sensor_box(docs,transform_ids,sensor_ref,section)
                            if box: room['markers'].append({'kind':'MeaCulpaAltar','node':box['spawn'],'name':node['name'],
                                                             'size':box['size'],'offset':box['offset']})
                if section=='LOGIC' and any(sn=='MudAreaEffect' for sn,_ in scripts):
                    mud=next(sd for sn,sd in scripts if sn=='MudAreaEffect')
                    box=next((c for c in node['colliders'] if c['kind']=='BoxCollider2D' and c['enabled']),None)
                    if box: room['markers'].append({'kind':'MudAreaEffect','node':node['id'],'name':node['name'],
                        'size':box['size'],'offset':box['offset'],'data':json.dumps(mud)})
                if section=='LOGIC' and any(sn=='CollectibleItem' for sn,_ in scripts):
                    interaction=next(sd for sn,sd in scripts if sn=='CollectibleItem')
                    item=next((sd.get('item') for _,sd in scripts if isinstance(sd.get('item'),str) and sd.get('item')),None)
                    if item:
                        for sensor_ref in interaction.get('sensors',[]):
                            box=sensor_box(docs,transform_ids,sensor_ref,section)
                            if box: room['markers'].append({'kind':'CollectibleItem','node':node['id'],'name':node['name'],
                                'item':item,'spawn':box['spawn'],'size':box['size'],'offset':box['offset'],
                                'data':json.dumps({'height':interaction.get('height',1),'sound':interaction.get('collectItemSound','')})})
                room['nodes'].append(node)
        rooms.append(room)
        report['rooms'].append({'id':room_id,'nodes':len(room['nodes']),'markers':room['markers']})
        print(room_id,len(room['nodes']),'nodes',flush=True)
    # Include original actor animation families, not their old scripts/controllers.
    prefixes=('player_', 'penitent_', 'elderbrother_', 'acolyte_', 'acolyteb_', 'flagellant_', 'newflagellant_', 'fool_', 'wheelcarrier_', 'mudcrawler_', 'priedieu_', 'chargedattackprojectile_', 'slash_clamped_attack_',
              'alliedcherub_', 'penitentbeam_', 'flamepillar_', 'prayerpr12', 'pontiffoldman_toxic')
    for g,p in index.items():
        if p.suffix=='.anim' and p.stem.lower().startswith(prefixes): animation_guids.add(g)
    animations=[]
    for g in sorted(animation_guids):
        p=index[g]; ds=load(p)
        d=next((d for k,d in ds.values() if k=='AnimationClip'),{})
        animation=animation_data(p,d)
        if animation:
            animations.append(animation)
            sprite_guids.update(f['sprite'] for f in animation['frames'] if f['sprite'])
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
    for g in mesh_guids:
        p=index.get(g)
        if p and p.suffix=='.asset':
            dst=OUT/'Meshes'/f'{g}.asset';dst.parent.mkdir(exist_ok=True)
            if not dst.exists():shutil.copy2(p,dst)
        else: warnings.append('Missing mesh '+g)
    package={'rooms':rooms,'sprites':sprites,'animations':animations}
    (OUT/'brotherhood.json').write_text(json.dumps(package,separators=(',',':')),encoding='utf-8')
    report.update(sprites=len(sprites),textures=len(textures),meshes=len(mesh_guids),animations=len(animations),warnings=warnings,
                  packedSprites=sum(1 for s in sprites if s['packing']&1),textureSources={g:str(p) for g,p in textures.items()})
    audit=ROOT/'Documentation';audit.mkdir(exist_ok=True)
    (audit/'source-audit.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf-8')
    print('Complete',len(sprites),'sprites',len(textures),'textures',len(animations),'clips; warnings',len(warnings),flush=True)

if __name__=='__main__':main()
