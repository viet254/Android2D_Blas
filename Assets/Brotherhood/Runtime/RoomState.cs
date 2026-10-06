using System;
using UnityEngine;
namespace Brotherhood
{
    [Serializable] public class RoomDoor { public string key,target,targetDoor; public Transform trigger,spawn; }
    [Serializable] public class ParallaxLayer { public Transform target; public float speed,speedY; public Vector3 origin,cameraOrigin; }
    [Serializable] public class CameraRegion { public float left,right,bottom,top; }
    [Serializable] public class LadderZone {public Transform sensor;public Vector2 center,size;public bool Contains(Vector2 p){return (sensor==null||sensor.gameObject.activeInHierarchy)&&Mathf.Abs(p.x-center.x)<=size.x*.5f+.25f&&p.y>=center.y-size.y*.5f-.2f&&p.y<=center.y+size.y*.5f+.2f;}}
    [Serializable] public class SkillAltarZone {public Transform sensor;public Vector2 offset,size;public bool Contains(Vector2 feet,Vector2 playerSize){if(sensor==null)return false;Vector2 center=sensor.TransformPoint(offset);Vector2 scale=sensor.lossyScale;return Mathf.Abs(feet.x-center.x)<=(Mathf.Abs(size.x*scale.x)+playerSize.x)*.5f&&Mathf.Abs(feet.y+playerSize.y*.5f-center.y)<=(Mathf.Abs(size.y*scale.y)+playerSize.y)*.5f;}}
    [Serializable] public class MudZone {public Transform source;public Vector2 offset,size;public float jumpSpeed,maxWalkSpeed,walkAcceleration,walkDrag,dashSpeed,dashDrag;public bool Contains(Vector2 feet,Vector2 playerSize){if(source==null)return false;Vector2 center=source.TransformPoint(offset),scale=source.lossyScale;return Mathf.Abs(feet.x-center.x)<=(Mathf.Abs(size.x*scale.x)+playerSize.x)*.5f&&Mathf.Abs(feet.y+playerSize.y*.5f-center.y)<=(Mathf.Abs(size.y*scale.y)+playerSize.y)*.5f;}}
    [Serializable] public class CollectibleZone {public GameObject root;public Transform sensor;public Vector2 offset,size;public string item;public bool halfHeight;public bool Contains(Vector2 feet,Vector2 playerSize){if(root==null||!root.activeInHierarchy||sensor==null)return false;Vector2 center=sensor.TransformPoint(offset),scale=sensor.lossyScale;return Mathf.Abs(feet.x-center.x)<=(Mathf.Abs(size.x*scale.x)+playerSize.x)*.5f&&Mathf.Abs(feet.y+playerSize.y*.5f-center.y)<=(Mathf.Abs(size.y*scale.y)+playerSize.y)*.5f;}}
    public sealed class RoomState : MonoBehaviour
    {
        public string id; public Transform start; public RoomDoor[] doors; public Transform[] checkpoints;public int sourceNodeCount,sourceRendererCount;
        public EnemyController[] enemies;public ParallaxLayer[] parallax;public FaithPlatform[] faithPlatforms;public LadderZone[] ladders;public GameObject[] breakables;public SkillAltarZone[] skillAltars;public MudZone[] mudZones;public CollectibleZone[] collectibles;
        public Vector3 parallaxOrigin;
        public CameraRegion[] cameraRegions=Array.Empty<CameraRegion>();
        public Transform undergroundTrigger;public Vector2 undergroundOffset,undergroundSize;bool undergroundCamera;
        Transform shockReceiver;Collider2D shockGateCollision;SpriteActor shockGateActor,shockSwitchActor;float shockGateOpening;
        public float left=-100,right=100,bottom=-20,top=20;
        public float KillY {get{float value=bottom;foreach(var region in cameraRegions)if(region.bottom<value)value=region.bottom;return value-8f;}}
        public float PlayerFallY(Vector2 position)
        {
            // Forest pits should end the fall at the edge of the active source
            // camera region. The lower D01Z01S01/S03 route stays reachable only
            // through its authored UndergroundBoundariesTrigger.
            if(id=="D01Z01S01"||id=="D01Z01S02"||id=="D01Z01S03")
                return CameraBoundsFor(position).bottom+.1f;
            return KillY;
        }
        public CameraRegion CameraBoundsFor(Vector2 player)
        {
            if(cameraRegions==null||cameraRegions.Length==0)return new CameraRegion{left=left,right=right,bottom=bottom,top=top};
            CameraRegion upper=cameraRegions[0],lower=cameraRegions[0];
            foreach(var region in cameraRegions)
            {
                if(region.bottom>upper.bottom)upper=region;
                if(region.bottom<lower.bottom)lower=region;
            }
            // Original CameraNumericBoundaries changes at an authored trigger,
            // not merely because the player is nearer the underground center.
            if(undergroundTrigger!=null&&upper!=lower)
            {
                Vector2 center=undergroundTrigger.TransformPoint(undergroundOffset),scale=undergroundTrigger.lossyScale;
                Vector2 half=new Vector2(Mathf.Abs(undergroundSize.x*scale.x),Mathf.Abs(undergroundSize.y*scale.y))*.5f;
                if(Mathf.Abs(player.x-center.x)<=half.x&&Mathf.Abs(player.y-center.y)<=half.y&&player.y<upper.bottom-.2f)undergroundCamera=true;
                else if(player.y>upper.bottom+.5f)undergroundCamera=false;
            }
            return undergroundCamera?lower:upper;
        }
        public void ResetCameraZone(){undergroundCamera=false;}
        SourceObjectId SourceNode(string node)
        {return Array.Find(GetComponentsInChildren<SourceObjectId>(true),o=>o.node==node);}
        SpriteActor SourceActor(SourceObjectId node,RestoredCatalog catalog,string initial)
        {
            if(node==null)return null;
            var renderer=node.GetComponent<SpriteRenderer>();if(renderer==null)return null;
            var actor=node.GetComponent<SpriteActor>();if(actor==null)actor=node.gameObject.AddComponent<SpriteActor>();
            actor.catalog=catalog;actor.visual=renderer;actor.initialClip=initial;actor.initialLoop=false;
            return actor;
        }
        public void RefreshShockGate(PlayerProgress progress,RestoredCatalog catalog)
        {
            if(id!="D17Z01S03")return;
            var receiver=SourceNode("LOGIC_124");var body=SourceNode("LOGIC_109");var graphic=SourceNode("LOGIC_91");
            shockReceiver=receiver==null?null:receiver.transform;
            shockGateCollision=body==null?null:body.GetComponent<Collider2D>();
            shockGateActor=SourceActor(body,catalog,"l_gate_closed_anim");
            shockSwitchActor=SourceActor(graphic,catalog,"glassSwitch_idle");
            bool opened=progress!=null&&progress.shockGateOpened;
            if(shockGateCollision!=null)shockGateCollision.enabled=!opened;
            if(shockGateActor!=null)
            {if(opened)shockGateActor.PlayHoldLastFrame("l_gate_opened_anim");else shockGateActor.Play("l_gate_closed_anim",false,true);}
            if(shockSwitchActor!=null)
            {if(opened)shockSwitchActor.PlayHoldLastFrame("glassSwitch_active");else shockSwitchActor.Play("glassSwitch_idle",false,true);}
            shockGateOpening=0;
        }
        public bool TryStrikeShockReceiver(Vector2 feet,float facing,float reach,float height,BrotherhoodGame game)
        {
            if(id!="D17Z01S03"||game==null||game.progress.shockGateOpened||shockReceiver==null)return false;
            // LOGIC_124 is the original 1x1 SlashReceiver, receiveHitsFrom=BOTH.
            // Its parent LOGIC_119 uses the hit to activate Gate and Switch once.
            Vector2 delta=(Vector2)shockReceiver.position-feet;
            // A sword touching the receiver's edge is a hit. Checking only its
            // center made a normal jump/upward slash miss at 30/60 Hz, although
            // the source box overlaps the blade. Keep the authored jump and reach.
            Vector3 scale=shockReceiver.lossyScale;
            Vector2 half=new Vector2(Mathf.Abs(scale.x),Mathf.Abs(scale.y))*.5f;
            if(delta.x*facing+half.x<-.6f||Mathf.Abs(delta.x)>reach+half.x||delta.y+half.y<-.5f||delta.y-half.y>height)return false;
            game.progress.shockGateOpened=true;
            if(shockGateCollision!=null)shockGateCollision.enabled=false;
            if(shockGateActor!=null){shockGateOpening=shockGateActor.Play("l_gate_going_up_anim",false,true);}
            if(shockSwitchActor!=null)shockSwitchActor.Play("glassSwitch_activating",false,true);
            game.Sfx("GATE_OPEN");game.Sfx("BELL_RECEIVER_ACTIVATE");game.SaveGame();
            return true;
        }
        void Update()
        {
            if(shockGateOpening<=0)return;
            shockGateOpening-=Time.deltaTime;
            if(shockGateOpening>0)return;
            if(shockGateActor!=null)shockGateActor.PlayHoldLastFrame("l_gate_opened_anim");
            if(shockSwitchActor!=null)shockSwitchActor.PlayHoldLastFrame("glassSwitch_active");
        }
        public EnemyController Boss {get{foreach(var e in enemies)if(e.boss)return e;return null;}}
        public RoomDoor Door(string key){foreach(var d in doors)if(d.key==key)return d;return null;}
        public void ResetEnemies(){foreach(var e in enemies)e.ResetEnemy();}
        public void ResetBreakables(){if(breakables!=null)foreach(var b in breakables)if(b!=null)b.SetActive(true);}
        public void RefreshCollectibles(PlayerProgress progress){if(collectibles!=null)foreach(var pickup in collectibles)if(pickup.root!=null)pickup.root.SetActive(!progress.Owns(pickup.item));}
        public bool OnLadder(Vector2 feet,out LadderZone ladder){foreach(var l in ladders)if(l.Contains(feet+Vector2.up*.6f)){ladder=l;return true;}ladder=null;return false;}
        void Awake(){EnsureFloorContinuity();}
        public void EnsureFloorContinuity()
        {
            if(id=="D17Z01S05")
            {
                // PenitenceGateOpen (LOGIC_187) has startOpen=1 in the mobile
                // scene. Restore Gate.Awake for already-built scenes as well.
                var openGate=Array.Find(GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_178");
                if(openGate!=null)
                {
                    foreach(var collider in openGate.GetComponents<Collider2D>())collider.enabled=false;
                    var gateActor=openGate.GetComponent<SpriteActor>();
                    if(gateActor!=null)gateActor.PlayHoldLastFrame("l_gate_opened_anim");
                }
                var bridge=transform.Find("FloorBridge_StatueToGate");
                if(bridge!=null){foreach(var collider in bridge.GetComponents<Collider2D>())collider.enabled=false;Destroy(bridge.gameObject);}
            }
        }
    }
}
