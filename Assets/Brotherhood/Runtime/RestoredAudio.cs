using System.Collections;
using UnityEngine;
namespace Brotherhood
{
    public sealed class RestoredAudio:MonoBehaviour
    {
        public AudioClip[] clips;
        AudioSource[] voices;AudioSource music,ambience;int cursor;Coroutine endingTransition;
        string regionalMusic="Brotherhood",regionalAmbience="Brotherhood_Ambient";bool cinematicAudio;
        public bool SfxMuted { get; private set; }
        public bool MusicMuted { get; private set; }

        void Awake()
        {
            SfxMuted = PlayerPrefs.GetInt("BrotherhoodSfxMuted", 0) == 1;
            MusicMuted = PlayerPrefs.GetInt("BrotherhoodMusicMuted", 0) == 1;
            voices = new AudioSource[12];
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
                voices[i].volume = .65f;
            }
            music = gameObject.AddComponent<AudioSource>();
            music.loop = true;
            music.volume = .22f;
            music.mute = MusicMuted;
            music.playOnAwake = false;
            ambience=gameObject.AddComponent<AudioSource>();ambience.loop=true;ambience.playOnAwake=false;
        }

        void Update()
        {
            if(music!=null&&endingTransition==null)music.volume=.22f*PlayerPrefs.GetFloat("BrotherhoodMusicVol",1);
            if(music!=null)music.mute=MusicMuted||cinematicAudio;
            if(ambience!=null){ambience.volume=.18f*PlayerPrefs.GetFloat("BrotherhoodSfxVol",1);ambience.mute=SfxMuted||cinematicAudio;}
        }
        public void SetCinematicAudio(bool active){cinematicAudio=active;if(music!=null)music.mute=MusicMuted||active;if(ambience!=null)ambience.mute=SfxMuted||active;}

        public void EnterRegion(string room)
        {
            regionalMusic=room!=null&&room.StartsWith("D01Z01")?"Forest_Music":room!=null&&room.StartsWith("D01Z02")?"Albero_MASTER":"Brotherhood";
            regionalAmbience=regionalMusic=="Forest_Music"?"Forest_ambient":regionalMusic=="Albero_MASTER"?"Ambient_Village_Exterior":"Brotherhood_Ambient";
            if(ambience!=null)
            {
                var clip=Find(regionalAmbience);if(ambience.clip!=clip){ambience.Stop();ambience.clip=clip;if(clip!=null)ambience.Play();}
                ambience.mute=SfxMuted;ambience.volume=.18f*PlayerPrefs.GetFloat("BrotherhoodSfxVol",1);
            }
            Music(false);
        }

        public void SetSfxMuted(bool muted)
        {
            SfxMuted = muted;
            if(ambience!=null)ambience.mute=muted;
            PlayerPrefs.SetInt("BrotherhoodSfxMuted", muted ? 1 : 0);
            PlayerPrefs.Save();
        }

        public void SetMusicMuted(bool muted)
        {
            MusicMuted = muted;
            PlayerPrefs.SetInt("BrotherhoodMusicMuted", muted ? 1 : 0);
            PlayerPrefs.Save();
            if (music != null)
            {
                music.mute = muted;
                if (!muted && !music.isPlaying && music.clip != null) music.Play();
            }
        }

        public void Play(string key, float volume = 1)
        {
            if (voices == null || SfxMuted) return;
            float sfxVol = PlayerPrefs.GetFloat("BrotherhoodSfxVol", 1f);
            if (sfxVol <= 0.001f) return;
            var clip = Find(key);
            if (clip == null) return;
            var voice = voices[cursor++ % voices.Length];
            voice.clip = clip;
            voice.volume = .65f * volume * sfxVol;
            voice.pitch = 1;
            voice.Play();
        }

        public void Music(bool boss)
        {
            if (music == null) return;
            if(endingTransition!=null){StopCoroutine(endingTransition);endingTransition=null;}
            var clip = Find(boss ? "Elder_Brother_MASTER" : regionalMusic);
            float musicVol = PlayerPrefs.GetFloat("BrotherhoodMusicVol", 1f);
            music.volume = .22f * musicVol;
            music.mute = MusicMuted;
            if (music.clip == clip) return;
            music.Stop();
            music.clip = clip;
            if (clip != null && !MusicMuted) music.Play();
        }

        public void EndBossMusic()
        {
            if(music==null)return;if(endingTransition!=null)StopCoroutine(endingTransition);endingTransition=StartCoroutine(BossEnding());
        }

        IEnumerator BossEnding()
        {
            // BossFightAudio drives the source FMOD "Ending" parameter. The
            // restored WAV has no parameter channel, so preserve that beat with
            // a short fade before returning to the Brotherhood score.
            float initial=music.volume;for(float t=0;t<1.5f;t+=Time.unscaledDeltaTime){music.volume=Mathf.Lerp(initial,0,t/1.5f);yield return null;}
            music.Stop();music.clip=Find(regionalMusic);music.volume=.22f*PlayerPrefs.GetFloat("BrotherhoodMusicVol",1f);music.mute=MusicMuted;if(music.clip!=null&&!MusicMuted)music.Play();endingTransition=null;
        }

        AudioClip Find(string key)
        {
            if(clips!=null)foreach(var c in clips)if(c!=null&&c.name==key)return c;
            // Small original-bank clips added after the scene's serialized bank
            // was built remain available without regenerating that scene.
            return Resources.Load<AudioClip>("Audio/"+key);
        }
        public bool Has(string key) { return Find(key) != null; }
    }
}
