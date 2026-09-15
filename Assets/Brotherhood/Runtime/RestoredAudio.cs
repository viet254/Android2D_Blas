using UnityEngine;
namespace Brotherhood
{
    public sealed class RestoredAudio:MonoBehaviour
    {
        public AudioClip[] clips;
        AudioSource[] voices;AudioSource music;int cursor;
        void Awake(){voices=new AudioSource[12];for(int i=0;i<voices.Length;i++){voices[i]=gameObject.AddComponent<AudioSource>();voices[i].playOnAwake=false;voices[i].volume=.65f;}music=gameObject.AddComponent<AudioSource>();music.loop=true;music.volume=.22f;music.playOnAwake=false;}
        public void Play(string key,float volume=1){if(voices==null)return;var clip=Find(key);if(clip==null)return;var voice=voices[cursor++%voices.Length];voice.clip=clip;voice.volume=.65f*volume;voice.pitch=1;voice.Play();}
        public void Music(bool boss){if(music==null)return;var clip=Find(boss?"Elder_Brother_MASTER":"Brotherhood");if(music.clip==clip)return;music.Stop();music.clip=clip;if(clip!=null)music.Play();}
        AudioClip Find(string key){foreach(var c in clips)if(c!=null && c.name==key)return c;return null;}
        public bool Has(string key){return Find(key)!=null;}
    }
}
