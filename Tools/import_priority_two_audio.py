"""Import only Priority 2 samples. Existing bank, scene and provenance stay intact."""
import json, subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = Path('D:/game/Bla_loc_map/ExportedProject/Assets/StreamingAssets')
DEST = ROOT / 'Assets/Brotherhood/Resources/Audio'
SELECTION = {
    'Master Bank': {
        'CHANGE_SELECTION': ['INVENTORY_SCROLL'],
        'CHANGE_TAB': ['INVENTORY_OPEN', 'INVENTORY_CLOSE'],
        'EQUIP_ITEM': ['INVENTORY_EQUIP'],
        'UNEQUIP_ITEM': ['INVENTORY_UNEQUIP'],
        'VERTICAL_ATTACK_HIT': ['VERTICAL_ATTACK_HIT'],
        'VERTICAL_ATTACK_FALL': ['VERTICAL_ATTACK_FALL'],
        'VERTICAL_ATTACK_START': ['VERTICAL_ATTACK_START'],
        'VERTICAL_ATTACK_HIT_LV2': ['VERTICAL_ATTACK_LV2'],
        'VERTICAL_ATTACK_HIT_LV3': ['VERTICAL_ATTACK_LV3'],
        'CHERUB_RESCUE': ['CHERUB_RESCUE'],
        'CHERUB_FLY': ['CHERUB_FLY'],
        'ESDRAS_THUNDER': ['TARANTO_THUNDER'],
    },
    'KeyEvents': {'Quest_Item': ['ITEM_ADDED'], 'USE_QUEST_ITEM': ['GUILT_RECOVER']},
    'CutScenes': {'Blood Baptism': ['CTS02_FOLEY']},
    'BackgroundLayer': {
        'Brotherhood_Ambient': ['Brotherhood_Ambient'],
        'Forest_Music': ['Forest_Music'],
        'Forest_ambient': ['Forest_ambient'],
        'Albero_MASTER': ['Albero_MASTER'],
        'Ambient_Village_Exterior': ['Ambient_Village_Exterior'],
    },
}

def main():
    index = json.loads((ROOT / 'Temp/priority-two-bank-index.json').read_text())
    DEST.mkdir(parents=True, exist_ok=True)
    imported = []
    for bank, samples in SELECTION.items():
        streams = {entry['name']: entry['stream'] for entry in index[bank]}
        for source_name, keys in samples.items():
            stream = streams[source_name]
            for key in keys:
                output = DEST / (key + '.wav')
                if not output.exists():
                    subprocess.run([str(ROOT / 'Tools/vgmstream/vgmstream-cli.exe'), '-i', '-s', str(stream), '-o', str(output), str(SOURCE / (bank + '.bank'))], stdout=subprocess.DEVNULL, check=True)
                imported.append({'bank': bank, 'stream': stream, 'sourceSample': source_name, 'runtimeKey': key, 'file': str(output.relative_to(ROOT))})
    (DEST / 'priority-two-audio-provenance.json').write_text(json.dumps({'openCloseAlias': 'ChangeTab is used for opening and closing the restored inventory; no distinct source Open/Close sample was identified.', 'samples': imported}, indent=2), encoding='utf-8')
    print('Imported', len(imported), 'bounded source samples')

if __name__ == '__main__':
    main()
