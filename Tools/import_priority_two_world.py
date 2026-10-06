"""Read only source cherub paths and tutorial sensors for imported rooms."""
import json, re, shutil
from pathlib import Path
from extract_brotherhood import SOURCE, ROOT, documents

def main():
    original=json.loads((ROOT/'Assets/Brotherhood/SourceData/brotherhood.json').read_text(encoding='utf-8'))
    extra=json.loads((ROOT/'Assets/Brotherhood/PriorityTwo/SourceData/brotherhood.json').read_text(encoding='utf-8'))
    cherubs=[];tutorials=[]
    scenes={p.stem:p for p in (SOURCE/'#Design').rglob('*_LOGIC.unity')}
    for room in original['rooms']+extra['rooms']:
        docs=documents(scenes[room['id']+'_LOGIC']);transforms={d['m_GameObject']['fileID']:fid for fid,(kind,d) in docs.items() if kind=='Transform'}
        by_go={}
        for fid,(kind,d) in docs.items():by_go.setdefault(d.get('m_GameObject',{}).get('fileID'),[]).append((fid,kind,d))
        for fid,(kind,d) in docs.items():
            if d.get('cherubId'):
                config=docs[d['spawner']['fileID']][1];path=docs[config['path']['fileID']][1]
                cherubs.append(dict(room=room['id'],node='LOGIC_'+str(transforms[config['m_GameObject']['fileID']]),pathNode='LOGIC_'+str(transforms[path['m_GameObject']['fileID']]),id=d['cherubId'],seconds=config['secondsToCompletePatrol'],points=path['points']))
            if 'fsm' not in d:continue
            ids=[]
            for state in d['fsm'].get('states',[]):
                a=state.get('actionData',{})
                if any('ShowHowToPlayPopup' in n for n in a.get('actionNames',[])):
                    ids.extend(v.get('value','') for v in a.get('fsmStringParams',[]) if re.match(r'^\d+_',str(v.get('value',''))))
            if not ids:continue
            t=transforms[d['m_GameObject']['fileID']];sensor=t
            while sensor:
                go=docs[sensor][1]['m_GameObject']['fileID']
                boxes=[val for _,typ,val in by_go[go] if typ=='BoxCollider2D' and val.get('m_Enabled')]
                if boxes:break
                sensor=docs[sensor][1]['m_Father']['fileID']
            if not sensor or not boxes:continue
            c=boxes[0]
            for id in ids:
                tutorials.append(dict(room=room['id'],node='LOGIC_'+str(sensor),id=id,offset=c['m_Offset'],size=c['m_Size']))
    # Duplicate popups from alternative source states share one saved tutorial id.
    seen=set();tutorials=[v for v in tutorials if (v['room'],v['node'],v['id']) not in seen and not seen.add((v['room'],v['node'],v['id']))]
    out=ROOT/'Assets/Brotherhood/Resources/World';out.mkdir(parents=True,exist_ok=True)
    (out/'priority-two-world.json').write_text(json.dumps(dict(rooms=[r['id'] for r in original['rooms']+extra['rooms']],cherubs=cherubs,tutorials=tutorials),ensure_ascii=False,separators=(',',':')),encoding='utf-8')
    cutscene=ROOT/'Assets/Brotherhood/Resources/Cutscenes';cutscene.mkdir(parents=True,exist_ok=True)
    if not (cutscene/'CTS02.m4v').exists():shutil.copy2(SOURCE/'VideoClip/CTS02.m4v',cutscene/'CTS02.m4v')
    print('Imported',len(cherubs),'cherub paths and',len(tutorials),'source tutorial sensors; CTS02 preserved')

if __name__=='__main__':main()
