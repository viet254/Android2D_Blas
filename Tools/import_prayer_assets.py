"""Import prayer-only source timelines and audio without rebuilding saved scenes."""
from pathlib import Path
import json, re, shutil, subprocess
from import_priority_two_effects import index_sources
from extract_brotherhood import SOURCE, ROOT, documents, animation_data

def main():
    index=index_sources()
    for meta in (SOURCE/'AnimatorOverrideController').glob('*.meta'):
        match=re.search(r'^guid: (\w+)',meta.read_text(),re.M)
        if match:index[match[1]]=Path(str(meta)[:-5])
    paths=set()
    names=['penitent_aura_transform','TearsUp_Effect','miriamPortal_shatter','landing_effects_anim','Penitent_Attack_Spark1','prayerHealingEffect']
    for name in names:
        path=SOURCE/'AnimationClip'/(name+'.anim')
        if path.exists():paths.add(path)
    for path in (SOURCE/'AnimationClip').glob('MiriamSpike*.anim'):paths.add(path)
    visited=set()
    def controller(path):
        if path in visited:return
        visited.add(path)
        for kind,doc in documents(path).values():
            if kind=='AnimatorState':
                motion=index.get(doc.get('m_Motion',{}).get('guid'))
                if motion is not None and motion.suffix=='.anim':paths.add(motion)
            elif kind=='AnimatorOverrideController':
                for entry in doc.get('m_Clips',[]):
                    motion=index.get(entry.get('m_OverrideClip',{}).get('guid'))
                    if motion is not None:paths.add(motion)
                base=index.get(doc.get('m_Controller',{}).get('guid'))
                if base is not None:controller(base)
    prefabs=['AlliedCherub','AlliedCherubSystem','PenitentShieldSystem','PenitentGuardian','PrayerPoisonAreaEffect','Penitent_PR12PlayerEffect_SimpleVFX','Penitent_PR12EnemyDamage_SimpleVFX','TearHarvestEffect','GhostSlashDamage','MiriamPortal','PenitentBeam']
    for guid in ['6b6f817ca849bb147bcee8c2f76b2ada','b9e983d431fee384f986b72c4e5602b5','1d1d7b405c35849488f7118a32385bf3','7bad743095d998244b49fb385f30a2a0','81f3d2a93cbe8d74d866b4137de8528d','ea71d500e4bbef04785c4ad0a4c788bd','b2900eda07f48ef42a69421155e48c91','4c0b9ad4a0f04774a916d0d7a2625590']:
        if guid in index:prefabs.append(index[guid].stem)
    for name in prefabs:
        p=SOURCE/'GameObject'/(name+'.prefab')
        if not p.exists():continue
        for kind,doc in documents(p).values():
            if kind=='Animator':
                path=index.get(doc.get('m_Controller',{}).get('guid'))
                if path is not None:controller(path)
    for name in ['AlliedCherub','PenitentGuardian','penitentBeam','DivineBeam','TearsUpController','floatingWeaponShield','shieldSummonFx','LightningExplosion']:
        p=SOURCE/'AnimatorController'/(name+'.controller')
        if p.exists():controller(p)
    clips=[];sprites={};textures={}
    imported=[]
    for path in sorted(paths):
        source=next(v for k,v in documents(path).values() if k=='AnimationClip');clip=animation_data(path,source)
        if clip:imported.append(clip)
    imported.append({'name':'PrayerShieldSprite','spritePath':'','duration':1,'loop':True,'events':[],'floatTracks':[],'frames':[{'time':0,'sprite':'cb60dcfef8ba4dc48acd9a257f10ddd6'}]})
    for clip in imported:
        clips.append(clip)
        for guid in dict.fromkeys(f['sprite'] for f in clip['frames'] if f['sprite']):
            if guid in sprites:continue
            sp=next(v for k,v in documents(index[guid]).values() if k=='Sprite');rd=sp['m_RD'];tex=rd['texture']['guid']
            original=sp['m_Rect'];rect=rd.get('textureRect',original);trim=rd.get('textureRectOffset',{});pivot=sp.get('m_Pivot',{'x':.5,'y':.5})
            sprites[guid]={'id':guid,'name':sp['m_Name'],'texture':tex,'rect':rect,'pivot':{'x':(pivot['x']*original['width']-trim.get('x',0))/rect['width'],'y':(pivot['y']*original['height']-trim.get('y',0))/rect['height']},'border':sp.get('m_Border',{}),'ppu':sp.get('m_PixelsToUnits',32),'packing':rd.get('settingsRaw',0)}
            textures[tex]=index[tex]
    out=ROOT/'Assets/Brotherhood/Resources/Effects/Prayers';out.mkdir(parents=True,exist_ok=True)
    for guid,path in textures.items():
        dest=out/(guid+'.png')
        if not dest.exists():shutil.copy2(path,dest)
    (out/'clips.json').write_text(json.dumps({'animations':clips,'sprites':list(sprites.values())},ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    print('Prayer assets:',len(clips),'clips,',len(sprites),'sprites,',len(textures),'atlases')
    print('Clips:',','.join(c['name'] for c in clips))

if __name__=='__main__':main()
