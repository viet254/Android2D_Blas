"""Refresh existing animation timelines without copying room/texture assets."""
import json
from extract_brotherhood import OUT, SOURCE, animation_data, documents

path = OUT / 'brotherhood.json'
data = json.loads(path.read_text(encoding='utf-8'))
for i, old in enumerate(data['animations']):
    source = SOURCE / 'AnimationClip' / (old['name'] + '.anim')
    clip = next(v for kind, v in documents(source).values() if kind == 'AnimationClip')
    data['animations'][i] = animation_data(source, clip)
path.write_text(json.dumps(data, ensure_ascii=False, separators=(',', ':'), allow_nan=False), encoding='utf-8')
print('Refreshed', len(data['animations']), 'timelines;',
      sum(not f['sprite'] for c in data['animations'] for f in c['frames']), 'null sprite keys;',
      sum(len(c['events']) for c in data['animations']), 'events;',
      sum(len(c['floatTracks']) for c in data['animations']), 'float tracks')
