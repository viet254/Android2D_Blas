"""Create a bounded Editor-safe preview from CTS02; keep its original video."""
from pathlib import Path
import json,subprocess,sys,struct
from PIL import Image
ROOT=Path(__file__).resolve().parents[1]
sys.path.insert(0,str(ROOT/'Temp/priority-two-video-runtime'))
import imageio_ffmpeg

def main():
    video=ROOT/'Assets/Brotherhood/Resources/Cutscenes/CTS02.m4v'
    out=video.parent;frames=ROOT/'Temp/priority-two-cts02-frames';frames.mkdir(parents=True,exist_ok=True)
    subprocess.run([imageio_ffmpeg.get_ffmpeg_exe(),'-hide_banner','-loglevel','error','-y','-i',str(video),'-vf','fps=15,scale=320:180','-frames:v','352',str(frames/'frame-%04d.png')],check=True)
    paths=sorted(frames.glob('frame-*.png'))
    for offset in range(0,len(paths),16):
        atlas=Image.new('RGB',(1280,720),'black')
        for i,path in enumerate(paths[offset:offset+16]):
            with Image.open(path) as frame:atlas.paste(frame,((i%4)*320,(i//4)*180))
        atlas.save(out/f'CTS02Preview_{offset//16:02d}.png')
    b=video.read_bytes();i=b.find(b'mvhd');scale,duration=struct.unpack_from('>II',b,i+16)
    (out/'CTS02-preview.json').write_text(json.dumps(dict(fps=15,frames=len(paths),duration=duration/scale,frameWidth=320,frameHeight=180,columns=4,rows=4)),encoding='utf-8')
    print('CTS02 safe preview:',len(paths),'frames;',duration/scale,'seconds; original video untouched')
if __name__=='__main__':main()
