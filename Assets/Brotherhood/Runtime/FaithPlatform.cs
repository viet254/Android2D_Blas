using UnityEngine;
namespace Brotherhood
{
    public sealed class FaithPlatform:MonoBehaviour
    {
        public bool first;public float deactivationDelay=3;
        public SpriteActor actor;public BoxCollider2D collision;public FaithPlatform[] targets;
        public bool Showing {get;private set;}
        bool relic;float remaining;
        public void SetRelic(bool active){relic=active;remaining=0;SetShowing(active&&first);}
        public void Tick(Vector2 feet,float dt)
        {
            if(!relic)return;
            if(!first&&Showing){remaining-=dt;if(remaining<=0)SetShowing(false);}
            if(!Showing)return;var bounds=collision.bounds;
            bool on=feet.x>=bounds.min.x-.15f&&feet.x<=bounds.max.x+.15f&&Mathf.Abs(feet.y-bounds.max.y)<.2f;
            if(on){remaining=deactivationDelay;foreach(var next in targets)if(next!=null)next.Reveal();}
        }
        public void Reveal(){if(!relic)return;remaining=deactivationDelay;SetShowing(true);}
        void SetShowing(bool value){bool changed=Showing!=value;Showing=value;collision.enabled=value;actor.Play(value?"bloodplatform_64x64_anim":"bloodplatform_no_relic_anim",true,changed);}
    }
}
