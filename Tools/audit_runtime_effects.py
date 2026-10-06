from pathlib import Path
import json,re
root=Path('Assets/Brotherhood')
data=json.loads((root/'SourceData/brotherhood.json').read_text(encoding='utf8'))
clips={x['name'] for x in data['animations']}
audio={x.stem for x in (root/'Audio').glob('*.wav')}
code='\n'.join(p.read_text(encoding='utf-8-sig',errors='ignore') for p in (root/'Runtime').glob('*.cs'))
clip_refs=set(re.findall(r'(?:actor\.Play|effects\.Animation|SetClip)\(\s*"([^"]+)"(?=\s*[,)])',code))
sfx_refs=set(re.findall(r'(?:game\.)?Sfx\(\s*"([^"]+)"(?=\s*[,)])',code))
missing_clips=sorted(clip_refs-clips)
missing_audio=sorted(sfx_refs-audio)
lines=['# Runtime action effect audit','',f'- Imported animation clips: `{len(clips)}`',f'- Runtime literal animation/VFX references: `{len(clip_refs)}`',f'- Imported decoded audio samples: `{len(audio)}`',f'- Runtime literal SFX references: `{len(sfx_refs)}`','', '## Missing literal animation/VFX references']
lines += [f'- `{x}`' for x in missing_clips] or ['- None']
lines += ['', '## Missing literal SFX references']
lines += [f'- `{x}`' for x in missing_audio] or ['- None']
lines += ['', 'Dynamic family/tier names are verified separately by the Unity playtest suite.']
Path('Documentation/RUNTIME_EFFECT_AUDIT.md').write_text('\n'.join(lines)+'\n',encoding='utf8')
print('missing clips',missing_clips);print('missing audio',missing_audio)

