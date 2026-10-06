using System;
using UnityEngine;
namespace Brotherhood
{
    public sealed class SpriteActor : MonoBehaviour
    {
        public RestoredCatalog catalog;
        public SpriteRenderer visual;
        public string initialClip;public bool initialLoop;
        RestoredClip current; float clock; bool repeat,animationHidden; bool visibleBeforeAnimation;
        public event Action<RestoredAnimationEvent> AnimationEvent;
        Color colorBeforeAnimation;Vector2 sizeBeforeAnimation;bool animatedColor,animatedSize;
        void OnEnable(){if(!string.IsNullOrEmpty(initialClip)&&catalog!=null&&visual!=null)Play(initialClip,initialLoop,true);}
        public bool Paused { get; set; }
        public float Speed { get; set; } = 1f;
        public string Current => current == null ? "" : current.name;
        public float Progress => current == null ? 1 : clock / Mathf.Max(.01f,current.duration);
        public float Play(string name, bool loop = false, bool restart = false, float speed = 1f)
        {
            Speed = Mathf.Max(0.01f, speed);
            if(!restart && Current==name) { Paused=false; return current.duration / Speed; }
            var next=catalog==null?null:catalog.Find(name); if(next==null) return .35f;
            RestoreProperties();current=next; clock=0; repeat=loop; Paused=false; Sample(); Dispatch(-1,0);return Mathf.Max(.05f,current.duration) / Speed;
        }
        public void PlayHoldLastFrame(string name)
        {
            var next=catalog.Find(name); if(next==null) return;
            RestoreProperties();current=next; clock=next.duration; repeat=false; Paused=false; Speed=1f; Sample();
        }
        public bool HasEvent(string function){return current?.events!=null&&Array.Exists(current.events,e=>e.functionName==function);}
        void Update(){Advance(Time.deltaTime);}
        public void Advance(float deltaTime)
        {
            if(current==null||Paused||deltaTime<=0)return;
            var playing=current;float remaining=deltaTime*Speed,duration=Mathf.Max(.05f,current.duration);
            while(remaining>0)
            {
                float before=clock,step=Mathf.Min(remaining,Mathf.Max(0,duration-clock));clock+=step;remaining-=step;
                Sample();Dispatch(before,clock);if(current!=playing)return;
                if(clock<duration||!repeat)break;
                clock=0;Sample();Dispatch(-1,0);if(current!=playing)return;
            }
        }
        void Dispatch(float from,float to)
        {
            var playing=current;if(playing.events==null)return;
            foreach(var evt in playing.events)if(evt.time>from&&evt.time<=to){AnimationEvent?.Invoke(evt);if(current!=playing)return;}
        }
        void RestoreVisibility(){if(animationHidden&&visual!=null)visual.enabled=visibleBeforeAnimation;animationHidden=false;}
        void RestoreProperties()
        {
            RestoreVisibility();if(visual==null)return;
            if(animatedColor)visual.color=colorBeforeAnimation;if(animatedSize)visual.size=sizeBeforeAnimation;
            animatedColor=animatedSize=false;
        }
        void SetAnimationVisible(bool visible)
        {
            if(!visible){if(!animationHidden)visibleBeforeAnimation=visual.enabled;animationHidden=true;visual.enabled=false;}
            else RestoreVisibility();
        }
        public static void SampleRenderer(RestoredClip clip,float time,SpriteRenderer renderer)
        {
            int frame=-1;while(frame+1<clip.times.Length&&clip.times[frame+1]<=time)frame++;
            renderer.sprite=frame>=0&&frame<clip.frames.Length?clip.frames[frame]:null;
            if(clip.floatTracks==null)return;
            foreach(var track in clip.floatTracks)
            {
                if(track.classID!=212||track.keys==null||track.keys.Length==0)continue;
                float value=track.Evaluate(time);Color color=renderer.color;Vector2 size=renderer.size;
                switch(track.attribute){case "m_Color.r":color.r=value;break;case "m_Color.g":color.g=value;break;case "m_Color.b":color.b=value;break;case "m_Color.a":color.a=value;break;case "m_Size.x":size.x=value;break;case "m_Size.y":size.y=value;break;default:continue;}
                renderer.color=color;renderer.size=size;
            }
        }
        void Sample()
        {
            if(visual==null)return;
            if(current.floatTracks!=null)foreach(var track in current.floatTracks)
            {
                if(track.classID!=212)continue;
                if(track.attribute.StartsWith("m_Color.")&&!animatedColor){colorBeforeAnimation=visual.color;animatedColor=true;}
                if(track.attribute.StartsWith("m_Size.")&&!animatedSize){sizeBeforeAnimation=visual.size;animatedSize=true;}
            }
            SampleRenderer(current,clock,visual);bool visible=visual.sprite!=null;
            if(current.floatTracks!=null)foreach(var track in current.floatTracks)if(track.classID==212&&track.attribute=="m_Enabled"&&track.keys!=null&&track.keys.Length>0)visible&=track.Evaluate(clock)>=.5f;
            SetAnimationVisible(visible);
        }
        public void Face(float direction) { if(Mathf.Abs(direction)>.01f) visual.flipX=direction<0; }
    }
}
