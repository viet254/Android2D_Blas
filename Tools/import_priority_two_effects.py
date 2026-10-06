"""Import only priority-two effect clips; source and existing catalog stay unchanged."""
from pathlib import Path
from concurrent.futures import ThreadPoolExecutor
import json,re,shutil
from extract_brotherhood import SOURCE,ROOT,documents,animation_data

def index_sources():
    cache=ROOT/'Temp'/'priority-two-source-index.json'
    if cache.exists():return {k:Path(v) for k,v in json.loads(cache.read_text()).items()}
    metas=[]
    for folder in ['AnimationClip','AnimatorController','GameObject','Sprite','Texture2D','Scripts','MonoBehaviour','Resources']:
        metas.extend((SOURCE/folder).rglob('*.meta'))
    def read(meta):
        m=re.search(rb'^guid: (\w+)',meta.read_bytes(),re.M)
        return (m[1].decode(),str(meta)[:-5]) if m else None
    with ThreadPoolExecutor(max_workers=12) as executor:index=dict(pair for pair in executor.map(read,metas) if pair)
    cache.write_text(json.dumps(index),encoding='utf-8');print('Indexed',len(index),'source references',flush=True)
    return {k:Path(v) for k,v in index.items()}

def main():
    index=index_sources();wanted=set()
    for name,states in [('Player',['Combo_4','RegresoAPuerto']),('AuroraGuardianController',None),('MiriamPortalController',None),('SanTelmoTrapCore',None),('cherubCaptor',None)]:
        for kind,doc in documents(SOURCE/'AnimatorController'/(name+'.controller')).values():
            if kind=='AnimatorState' and (states is None or doc['m_Name'] in states):
                ref=doc.get('m_Motion',{});guid=ref.get('guid')
                if guid:wanted.add(guid)
    paths=[index[guid] for guid in sorted(wanted)]
    for prefab_guid in ['b78fe7833d8c52a41b1dc41e7a1c3660','c158a363bb1f7794b84a2de2462b088e','3255704ec5cb8c640b8ec8d1528bb165','4178b7acc64f6ac43a819b58e1b9e556']:
        for kind,doc in documents(index[prefab_guid]).values():
            if kind!='Animator':continue
            controller=index.get(doc.get('m_Controller',{}).get('guid'))
            if controller is None:continue
            for state_kind,state in documents(controller).values():
                motion=index.get(state.get('m_Motion',{}).get('guid'))
                if state_kind=='AnimatorState' and motion is not None and motion.suffix=='.anim':paths.append(motion)
    for name in ['SanTelmoLightning_anim','Player_ComboFinisher_Up','Player_ComboFinisher_Down','guiltSystem_guiltDropVanish','GuiltSystem_idle','guiltSystem_pickUpGuiltFx']:
        paths.append(SOURCE/'AnimationClip'/(name+'.anim'))
    # Teleport VFX and all three PR203 trap-core/beam timelines are independent.
    clips=[];sprites={};textures={}
    for path in dict.fromkeys(paths):
        source=next(v for k,v in documents(path).values() if k=='AnimationClip')
        clip=animation_data(path,source)
        if not clip:continue
        clips.append(clip)
        for guid in dict.fromkeys(f['sprite'] for f in clip['frames'] if f['sprite']):
            if guid in sprites:continue
            sp=next(v for k,v in documents(index[guid]).values() if k=='Sprite');rd=sp['m_RD'];tex=rd['texture']['guid']
            original=sp['m_Rect'];rect=rd.get('textureRect',original);trim=rd.get('textureRectOffset',{});pivot=sp.get('m_Pivot',{'x':.5,'y':.5})
            sprites[guid]={'id':guid,'name':sp['m_Name'],'texture':tex,'rect':rect,'pivot':{'x':(pivot['x']*original['width']-trim.get('x',0))/rect['width'],'y':(pivot['y']*original['height']-trim.get('y',0))/rect['height']},'border':sp.get('m_Border',{}),'ppu':sp.get('m_PixelsToUnits',32),'packing':rd.get('settingsRaw',0)}
            textures[tex]=index[tex]
    out=ROOT/'Assets/Brotherhood/Resources/Effects/PriorityTwo';out.mkdir(parents=True,exist_ok=True)
    for guid,path in textures.items():
        dest=out/(guid+'.png')
        if not dest.exists():shutil.copy2(path,dest)
    (out/'clips.json').write_text(json.dumps({'animations':clips,'sprites':list(sprites.values())},ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    print('Imported',len(clips),'clips,',len(sprites),'sprites,',len(textures),'atlases',flush=True)
    print('Clip names:',','.join(c['name'] for c in clips),flush=True)
if __name__=='__main__':main()
