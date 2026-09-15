using UnityEngine;
namespace Brotherhood
{
    public sealed class SpriteActor : MonoBehaviour
    {
        public RestoredCatalog catalog;
        public SpriteRenderer visual;
        RestoredClip current; float clock; bool repeat;
        public string Current => current == null ? "" : current.name;
        public float Progress => current == null ? 1 : clock / Mathf.Max(.01f,current.duration);
        public float Play(string name, bool loop = false, bool restart = false)
        {
            if(!restart && Current==name) return current.duration;
            var next=catalog.Find(name); if(next==null) return .35f;
            current=next; clock=0; repeat=loop; Sample(); return Mathf.Max(.05f,current.duration);
        }
        void Update() { if(current==null)return; clock+=Time.deltaTime; if(repeat)clock%=Mathf.Max(.05f,current.duration); Sample(); }
        void Sample() { int frame=0; while(frame+1<current.times.Length && current.times[frame+1]<=clock) frame++; if(current.frames.Length>0)visual.sprite=current.frames[frame]; }
        public void Face(float direction) { if(Mathf.Abs(direction)>.01f) visual.flipX=direction<0; }
    }
}
