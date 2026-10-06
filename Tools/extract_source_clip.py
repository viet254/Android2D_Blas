"""Print one exact mobile-export sprite animation and its source sprite metadata.

Read-only: the caller decides how to merge the JSON and copy referenced PNGs.
"""
import json
import re
import sys
from pathlib import Path

from extract_brotherhood import SOURCE, documents


def main(name):
    index = {}
    for meta in SOURCE.rglob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match:
            index[match[1]] = Path(str(meta)[:-5])
    clip_path = SOURCE / 'AnimationClip' / (name + '.anim')
    clip = next(value for kind, value in documents(clip_path).values() if kind == 'AnimationClip')
    curve = next(curve for curve in clip['m_PPtrCurves'] if curve['attribute'] == 'm_Sprite')
    frames = [{'time': float(key['time']), 'sprite': key['value']['guid']} for key in curve['curve']]
    sprites = []
    textures = {}
    for guid in dict.fromkeys(frame['sprite'] for frame in frames):
        source = next(value for kind, value in documents(index[guid]).values() if kind == 'Sprite')
        rd = source.get('m_RD', {})
        texture = rd['texture']['guid']
        original = source['m_Rect']
        rect = rd.get('textureRect', original)
        trim = rd.get('textureRectOffset', {})
        pivot = source.get('m_Pivot', {'x': .5, 'y': .5})
        sprites.append({'id': guid, 'name': source['m_Name'], 'texture': texture,
                        'rect': rect, 'pivot': {'x': (pivot['x'] * original['width'] - trim.get('x', 0)) / max(1, rect['width']),
                                                'y': (pivot['y'] * original['height'] - trim.get('y', 0)) / max(1, rect['height'])},
                        'border': source.get('m_Border', {}), 'ppu': source.get('m_PixelsToUnits', 32),
                        'packing': rd.get('settingsRaw', 0)})
        textures[texture] = str(index[texture])
    settings = clip.get('m_AnimationClipSettings', {})
    print(json.dumps({'animations': [{'name': name, 'frames': frames,
                                      'duration': float(settings.get('m_StopTime', frames[-1]['time'] + .08)),
                                      'loop': bool(settings.get('m_LoopTime', 0))}],
                      'sprites': sprites, 'textures': textures}, separators=(',', ':')))


if __name__ == '__main__':
    main(sys.argv[1])
