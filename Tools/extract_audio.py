"""Decode a bounded selection of original FMOD samples with vgmstream (local tool).
Tool source: https://github.com/vgmstream/vgmstream
No FMOD runtime DLL is copied into the Android project.
"""
from pathlib import Path
import re,subprocess,json
ROOT=Path(__file__).resolve().parents[1]
EXE=ROOT/'Tools/vgmstream/vgmstream-cli.exe'
SOURCE=Path('D:/game/Bla_loc_map/ExportedProject/Assets/StreamingAssets')
DEST=ROOT/'Assets/Brotherhood/Audio'
DEST.mkdir(exist_ok=True)
selected=[]
for bank in ['Master Bank','BossMusic','BackgroundLayer']:
    text=subprocess.check_output([str(EXE),'-m','-s','1','-S','0',str(SOURCE/(bank+'.bank'))],text=True)
    entries=re.findall(r'stream index: (\d+)\s+stream name: ([^\r\n]+)',text)
    for index,name in entries:
        wanted=(bank=='Master Bank' and (name.startswith(('ELDER_BROTHER_','PENITENT_SLASH_AIR_','PENITENT_RUN_STONE_')) or name in ['PENITENT_DASH','PENITENT_SIMPLE_DAMAGE_DEFAULT','PENITENT_GUARD','PENITENT_PARRY_SLOW','PENITENT_JUMP_FALL_STONE','USE_FLASK','FLASK_REFILL','PENITENT_COMBO_FINAL_DOWN','RANGE_ATTACK','RANGE_ATTACK_FLY','RANGE_ATTACK_HIT','RANGE_ATTACK_DISSAPEAR','RANGE_ATTACK_EXPLODE','PENITENT_ACTIVATE_PRAYER','FERVOR_START_PRAYER','FERVOR_END_PRAYER','PRAYER_INVINCIBILITY','PRAYER_SHOT','EQUIP_PRAYER','NO_PRAYER'])) or (bank=='BossMusic' and name=='Elder Brother_MASTER') or (bank=='BackgroundLayer' and 'brother' in name.lower())
        if not wanted:continue
        filename=re.sub(r'[^A-Za-z0-9_-]','_',name)+'.wav'
        subprocess.run([str(EXE),'-i','-s',index,'-o',str(DEST/filename),str(SOURCE/(bank+'.bank'))],stdout=subprocess.DEVNULL,check=True)
        selected.append({'bank':bank,'stream':int(index),'name':name,'file':filename})
(ROOT/'Documentation/audio-provenance.json').write_text(json.dumps(selected,indent=2))
print('Decoded',len(selected),'original audio samples')
