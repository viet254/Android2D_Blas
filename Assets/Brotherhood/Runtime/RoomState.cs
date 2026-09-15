using System;
using UnityEngine;
namespace Brotherhood
{
    [Serializable] public class RoomDoor { public string key,target,targetDoor; public Transform trigger,spawn; }
    [Serializable] public class ParallaxLayer { public Transform target; public float speed,speedY; public Vector3 origin; }
    [Serializable] public class LadderZone {public Vector2 center,size;public bool Contains(Vector2 p){return Mathf.Abs(p.x-center.x)<=size.x*.5f+.25f&&p.y>=center.y-size.y*.5f-.2f&&p.y<=center.y+size.y*.5f+.2f;}}
    public sealed class RoomState : MonoBehaviour
    {
        public string id; public Transform start; public RoomDoor[] doors; public Transform[] checkpoints;public int sourceNodeCount,sourceRendererCount;
        public EnemyController[] enemies;public ParallaxLayer[] parallax;public FaithPlatform[] faithPlatforms;public LadderZone[] ladders;public GameObject[] breakables;
        public Vector3 parallaxOrigin;
        public float left=-100,right=100,bottom=-20,top=20;
        public float KillY=>bottom-8;
        public EnemyController Boss {get{foreach(var e in enemies)if(e.boss)return e;return null;}}
        public RoomDoor Door(string key){foreach(var d in doors)if(d.key==key)return d;return null;}
        public void ResetEnemies(){foreach(var e in enemies)e.ResetEnemy();}
        public void ResetBreakables(){if(breakables!=null)foreach(var b in breakables)if(b!=null)b.SetActive(true);}
        public bool OnLadder(Vector2 feet,out LadderZone ladder){foreach(var l in ladders)if(l.Contains(feet+Vector2.up*.6f)){ladder=l;return true;}ladder=null;return false;}
        void Awake(){EnsureFloorContinuity();}
        public void EnsureFloorContinuity()
        {
            if(id=="D17Z01S05")
            {
                ladders=new LadderZone[0];
                var bridge=transform.Find("FloorBridge_StatueToGate");
                if(bridge==null)
                {
                    var go=new GameObject("FloorBridge_StatueToGate");
                    go.transform.SetParent(transform,true);
                    go.transform.position=new Vector3(-836f,7f,0f);
                    go.layer=8;
                    var col=go.AddComponent<BoxCollider2D>();
                    col.size=new Vector2(4f,4f);
                    col.offset=Vector2.zero;
                }
            }
        }
    }
}
