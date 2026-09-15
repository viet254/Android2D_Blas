using UnityEngine;
namespace Brotherhood
{
    // Feet-based capsule; bounded casts avoid per-frame allocations and resolve slopes.
    public sealed class KinematicMotor : MonoBehaviour
    {
        public Vector2 size=new Vector2(.5f,1.15f);
        public Vector2 velocity;
        public bool grounded;
        public float gravity=27, maxFall=20;
        public float dropThrough;
        readonly RaycastHit2D[] hits=new RaycastHit2D[16];
        const float Skin=.025f;
        public bool TrySetHeight(float height)
        {
            height=Mathf.Max(.45f,height);if(height<=size.y){size.y=height;return true;}
            // Feet stay fixed when changing stance. Test the exact final capsule;
            // casting the short capsule upward could miss side overlap at wall seams.
            if(!IsClearAt(transform.position,height))return false;size.y=height;return true;
        }
        public bool IsClearAt(Vector2 feet,float height)
        {
            Vector2 testSize=new Vector2(size.x+Skin*2,height+Skin);Vector2 center=feet+Vector2.up*height*.5f;
            return Physics2D.OverlapCapsule(center,testSize,CapsuleDirection2D.Vertical,0,1<<8)==null;
        }
        public void Step(float dt)
        {
            dropThrough=Mathf.Max(0,dropThrough-dt);
            bool holdSlope=grounded&&Mathf.Abs(velocity.x)<.01f&&velocity.y<=0;
            velocity.y=holdSlope?0:Mathf.Max(-maxFall,velocity.y-gravity*dt);
            Vector2 remaining=velocity*dt;
            grounded=false;
            for(int iteration=0;iteration<4 && remaining.sqrMagnitude>.0000001f;iteration++)
            {
                float distance=remaining.magnitude;
                var filter=new ContactFilter2D {useLayerMask=true,layerMask=(1<<8)|(1<<9),useTriggers=false};
                int count=Physics2D.CapsuleCast((Vector2)transform.position+Vector2.up*size.y*.5f,size,CapsuleDirection2D.Vertical,0,remaining/distance,filter,hits,distance+Skin);
                RaycastHit2D best=default; float near=distance+Skin;
                for(int i=0;i<count;i++)
                {
                    if(hits[i].collider.gameObject.layer==9 && (dropThrough>0||remaining.y>0||hits[i].normal.y<.6f||transform.position.y<hits[i].point.y-.05f))continue;
                    if(hits[i].distance<near && Vector2.Dot(remaining,hits[i].normal)<-.00001f) {best=hits[i];near=best.distance;}
                }
                if(best.collider==null) {transform.position+=(Vector3)remaining;break;}
                float travel=Mathf.Max(0,near-Skin);
                transform.position+=(Vector3)(remaining.normalized*travel);
                remaining=remaining.normalized*Mathf.Max(0,distance-travel);
                remaining-=best.normal*Mathf.Min(0,Vector2.Dot(remaining,best.normal));
                if(best.normal.y>.6f)grounded=true;
                velocity-=best.normal*Mathf.Min(0,Vector2.Dot(velocity,best.normal));
            }
            if(velocity.y<=.1f)
            {
                var ground=Physics2D.Raycast((Vector2)transform.position+Vector2.up*.12f,Vector2.down,.23f,(1<<8)|(dropThrough<=0?1<<9:0));
                if(ground.collider!=null && ground.normal.y>.6f) { grounded=true; transform.position=new Vector3(transform.position.x,ground.point.y+Skin,0);velocity.y=0; }
            }
        }
        public void Teleport(Vector2 feet) { transform.position=new Vector3(feet.x,feet.y,0);velocity=Vector2.zero;grounded=false;Physics2D.SyncTransforms(); }
        public bool Ledge(float facing,out Vector2 point)
        {
            point=default;
            var wall=Physics2D.Raycast((Vector2)transform.position+Vector2.up*.6f,Vector2.right*facing,.55f,1<<8);
            if(wall.collider==null)return false;
            float targetX=wall.point.x+facing*(size.x*.5f+Skin*2);
            var top=Physics2D.Raycast(new Vector2(targetX,transform.position.y+2.6f),Vector2.down,2,1<<8);
            if(top.collider==null||top.normal.y<.65f||top.point.y<transform.position.y+.6f)return false;
            Vector2 feet=new Vector2(targetX,top.point.y+Skin);
            if(!IsClearAt(feet,size.y))return false;
            point=feet;return true;
        }
    }
}
