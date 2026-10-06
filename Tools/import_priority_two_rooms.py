"""Use the existing read-only extractor for a bounded, separate S04 package."""
import inspect, re
from pathlib import Path
import extract_brotherhood as extractor
from import_priority_two_effects import index_sources

def main():
    root = Path(__file__).resolve().parents[1]
    index = index_sources()
    for meta in (extractor.SOURCE / 'Mesh').glob('*.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)
        if match: index[match[1]] = Path(str(meta)[:-5])
    body = inspect.getsource(extractor.main)
    start = body.index('    index = {}')
    end = body.index('    cache = {}', start)
    body = body[:start] + '    index = source_index\n' + body[end:]
    # Only clips referenced by the new room, not every actor family again.
    start = body.index('    prefixes=')
    end = body.index('    animations=[]', start)
    body = body[:start] + body[end:]
    temporary = root / 'Temp/priority-two-room-import'
    temporary.mkdir(parents=True, exist_ok=True)
    namespace = dict(extractor.__dict__, ROOT=temporary, OUT=root / 'Assets/Brotherhood/PriorityTwo/SourceData', ROOMS=['D17Z01S04','D17Z01S07','D17Z01S08','D17Z01S09'], source_index=index)
    exec(compile(body, __file__, 'exec'), namespace)
    namespace['main']()

if __name__ == '__main__': main()
