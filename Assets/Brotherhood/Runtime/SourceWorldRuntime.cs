using System;
using System.Collections.Generic;
using UnityEngine;
namespace Brotherhood
{
    public sealed class SourceWorldRuntime:MonoBehaviour
    {
        [Serializable] public class CherubData{public string room,node,pathNode,id;public float seconds;public Vector3[] points;}
        [Serializable] public class TutorialData{public string room,node,id;public Vector2 offset,size;}
        [Serializable] class WorldData{public string[] rooms;public CherubData[] cherubs;public TutorialData[] tutorials;}
        static string[] importedRooms;
        public static bool RoomImported(string id){if(importedRooms==null){var json=Resources.Load<TextAsset>("World/priority-two-world");importedRooms=json==null?Array.Empty<string>():JsonUtility.FromJson<WorldData>(json.text).rooms??Array.Empty<string>();}return Array.IndexOf(importedRooms,id)>=0;}
        sealed class Captor{public CherubData data;public Transform path;public SpriteActor actor;public float clock;public bool rescued;}
        readonly List<Captor> captors=new List<Captor>();WorldData data;BrotherhoodGame game;SourceNpcDialogue dialogue;
        public int ActiveCherubs{get{int count=0;foreach(var c in captors)if(!c.rescued)count++;return count;}}
        public static void AttachRooms(BrotherhoodGame game)
        {
            var rooms=new List<RoomState>(game.rooms);
            foreach(var prefab in Resources.LoadAll<GameObject>("Rooms/PriorityTwo"))
            {
                var source=prefab.GetComponent<RoomState>();if(source==null||rooms.Exists(r=>r.id==source.id))continue;
                var room=Instantiate(prefab,game.transform).GetComponent<RoomState>();room.name=source.id;room.gameObject.SetActive(false);rooms.Add(room);
            }
            game.rooms=rooms.ToArray();
        }
        public void Initialize(BrotherhoodGame owner)
        {
            game=owner;var source=Resources.Load<TextAsset>("World/priority-two-world");data=source!=null?JsonUtility.FromJson<WorldData>(source.text):new WorldData();
            dialogue=gameObject.AddComponent<SourceNpcDialogue>();dialogue.Initialize(game);
        }
        public SourceObjectId Node(string id)=>game.Current==null?null:Array.Find(game.Current.GetComponentsInChildren<SourceObjectId>(true),n=>n.node==id);
        public void OnRoomEntered()
        {
            dialogue.Close();foreach(var c in captors)if(c.actor!=null)Destroy(c.actor.gameObject);captors.Clear();
            ApplyShortcut();
            if(game.Current.id=="D17Z01S04")foreach(var n in game.Current.GetComponentsInChildren<SourceObjectId>(true))
                if(n.name=="Redento")n.gameObject.SetActive(game.progress.HasFlag("ST02_REDENTO/REDENTO_LOCATION_2"));
            foreach(var source in data.cherubs??Array.Empty<CherubData>())
            {
                if(source.room!=game.Current.id||Array.IndexOf(game.progress.rescuedCherubIds,source.id)>=0)continue;
                var node=Node(source.node);var path=Node(source.pathNode);if(node==null||path==null||!node.gameObject.activeInHierarchy)continue;
                var obj=new GameObject(source.id);obj.transform.SetParent(game.Current.transform);obj.transform.position=node.transform.position;
                var actor=obj.AddComponent<SpriteActor>();actor.enabled=false;actor.catalog=game.player.actor.catalog;actor.visual=obj.AddComponent<SpriteRenderer>();actor.visual.sharedMaterial=game.player.actor.visual.sharedMaterial;actor.visual.sortingOrder=100;actor.Play("cherubCaptor_idle",true,true);
                captors.Add(new Captor{data=source,path=path.transform,actor=actor});
            }
            if(game.Current.id=="D17Z01S09"&&game.progress.HasFlag("ST21_JAILED_GHOST/JAILED_QUEST_COMPLETED"))
                foreach(var actor in game.Current.GetComponentsInChildren<SpriteActor>(true))if(actor.initialClip=="soledad-ghost-anim")actor.visual.enabled=false;
        }
        public void Tick(float dt)
        {
            foreach(var c in captors)
            {
                if(c.actor==null)continue;c.actor.Advance(dt);if(c.rescued){if(c.actor.Progress>=1)c.actor.visual.enabled=false;continue;}
                c.clock+=dt;var points=c.data.points;int segments=(points.Length-1)/3;if(segments<=0)continue;
                float t=Mathf.Repeat(c.clock/Mathf.Max(.1f,c.data.seconds),1)*segments;int i=Mathf.Min(segments-1,(int)t)*3;t-=Mathf.Floor(t);float u=1-t;
                Vector3 at=u*u*u*points[i]+3*u*u*t*points[i+1]+3*u*t*t*points[i+2]+t*t*t*points[i+3];c.actor.transform.position=c.path.TransformPoint(at);
            }
            if(!GameSettings.HowToPlay||game.InputBlocked||game.player.Dead||game.controls.Simulation)return;
            foreach(var t in data.tutorials??Array.Empty<TutorialData>())
            {
                if(t.room!=game.Current.id||game.progress.HasFlag("TUTORIAL/"+t.id))continue;var node=Node(t.node);if(node==null||!node.gameObject.activeInHierarchy)continue;
                var bounds=new Bounds(node.transform.TransformPoint(t.offset),new Vector3(Mathf.Abs(t.size.x*node.transform.lossyScale.x),Mathf.Abs(t.size.y*node.transform.lossyScale.y),1));
                Vector2 p=(Vector2)game.player.transform.position+Vector2.up*game.player.motor.size.y*.5f;
                if(!bounds.Contains(new Vector3(p.x,p.y,bounds.center.z)))continue;
                string number=t.id.Split('_')[0],text=VietnameseSource.Term("Tutorial/TUT"+number+"_TEXT");if(string.IsNullOrEmpty(text))continue;
                game.progress.SetFlag("TUTORIAL/"+t.id);game.SaveGame();dialogue.ShowTutorial(VietnameseSource.Term("Tutorial/TUT"+number+"_CAPTION","Cách chơi"),text);break;
            }
        }
        public bool Strike(Bounds attack)
        {
            attack.center=new Vector3(attack.center.x,attack.center.y,0);attack.size=new Vector3(attack.size.x,attack.size.y,2);
            return Strike(target=>attack.Intersects(target));
        }
        public bool Strike(Func<Bounds,bool> intersects)
        {
            bool hit=game.trials!=null&&game.trials.Strike(intersects);
            foreach(var c in captors)
            {
                if(c.rescued||!intersects(new Bounds(new Vector3(c.actor.transform.position.x,c.actor.transform.position.y-.96f,0),new Vector3(.67f,2.68f,2))))continue;
                if(!game.progress.RescueCherub(c.data.id))continue;c.rescued=true;c.actor.Play("cherubCaptor_death",false,true);game.Sfx("CHERUB_RESCUE");game.SaveGame();hit=true;
            }
            if(game.Current.id=="D17Z01S04"&&!game.progress.HasFlag("LEVEL_FLAGS/D17Z01S04_SHORTCUT"))
            {
                var sensor=Node("LOGIC_188");if(sensor!=null&&intersects(new Bounds(new Vector3(sensor.transform.position.x,sensor.transform.position.y,0),Vector3.one)))
                {
                    game.progress.SetFlag("LEVEL_FLAGS/D17Z01S04_SHORTCUT");game.progress.SetFlag("MAP/D17Z01S05_shorcut");
                    var graphic=Node("LOGIC_130");if(graphic!=null&&graphic.TryGetComponent<SpriteRenderer>(out var visual)){var actor=graphic.GetComponent<SpriteActor>()??graphic.gameObject.AddComponent<SpriteActor>();actor.catalog=game.player.actor.catalog;actor.visual=visual;actor.PlayHoldLastFrame("glassSwitch_active");}
                    game.Sfx("SWITCH_ON");game.effects.Burst(sensor.transform.position,Color.yellow);
                    StartCoroutine(LowerLadderCoroutine());
                    game.Message("CƠ CHẾ ĐÃ KÍCH HOẠT · THANG ĐÃ ĐƯỢC HẠ XUỐNG");
                    game.SaveGame();hit=true;
                }
            }
            if(game.Current.id=="D17Z01S04"&&!game.progress.HasFlag("LEVEL_FLAGS/D17Z01S04_SECRET_WALL"))
            {
                var wall=Node("LOGIC_143");
                if(wall!=null&&intersects(new Bounds(new Vector3(wall.transform.position.x,wall.transform.position.y,0),new Vector3(2.5f,4f,2f))))
                {
                    BreakSecretWall(wall);
                    hit=true;
                }
            }
            return hit;
        }
        void BreakSecretWall(SourceObjectId wall)
        {
            game.progress.SetFlag("LEVEL_FLAGS/D17Z01S04_SECRET_WALL");
            wall.gameObject.SetActive(false);
            game.effects.Burst(wall.transform.position+Vector3.up,new Color(.65f,.6f,.55f));
            game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",wall.transform.position,1);
            game.Sfx("CHARGED_ATTACK_PROJECTILE_HIT_WALL");
            game.Shake(.35f);
            game.SaveGame();
            StartCoroutine(SecretWallCutscene(wall.transform.position));
        }
        System.Collections.IEnumerator SecretWallCutscene(Vector3 wallPos)
        {
            game.SetEncounterInputBlocked(true);
            game.player.motor.velocity=Vector2.zero;
            var focusNode=new GameObject("SecretWallFocus");
            focusNode.transform.position=wallPos+new Vector3(-4f,0,0);
            game.BeginCinematicCamera(focusNode.transform,4.8f);
            game.Message("KHU VỰC BÍ MẬT · BỨC TƯỜNG ĐÃ BỊ PHÁ VỠ");
            yield return new WaitForSeconds(1.5f);
            game.EndCinematicCamera();
            Destroy(focusNode);
            game.SetEncounterInputBlocked(false);
        }
        public bool DoorAllowed(RoomDoor door)=>door.trigger.gameObject.activeInHierarchy&&!(game.Current.id=="D17Z01S05"&&door.key=="S"&&!game.progress.HasFlag("LEVEL_FLAGS/D17Z01S04_SHORTCUT"));
        void ApplyShortcut()
        {
            if(game.Current.id=="D17Z01S05")
            {
                bool opened=game.progress.HasFlag("LEVEL_FLAGS/D17Z01S04_SHORTCUT");var open=Node("LOGIC_105");var closed=Node("LOGIC_133");if(open!=null)open.gameObject.SetActive(opened);if(closed!=null)closed.gameObject.SetActive(!opened);
            }
            else if(game.Current.id=="D17Z01S04")
            {
                bool opened=game.progress.HasFlag("LEVEL_FLAGS/D17Z01S04_SHORTCUT");
                ApplyLadderState(opened);
                if(opened)
                {
                    var graphic=Node("LOGIC_130");if(graphic!=null&&graphic.TryGetComponent<SpriteRenderer>(out var visual)){var actor=graphic.GetComponent<SpriteActor>()??graphic.gameObject.AddComponent<SpriteActor>();actor.catalog=game.player.actor.catalog;actor.visual=visual;actor.PlayHoldLastFrame("glassSwitch_active");}
                }
                if(game.progress.HasFlag("LEVEL_FLAGS/D17Z01S04_SECRET_WALL"))
                {
                    var wall=Node("LOGIC_143");if(wall!=null)wall.gameObject.SetActive(false);
                }
            }
        }
        void ApplyLadderState(bool opened)
        {
            if(game.Current==null||game.Current.id!="D17Z01S04")return;
            var ladderSpriteObj=Node("LOGIC_184");
            var ladderSprite=ladderSpriteObj!=null?ladderSpriteObj.GetComponent<SpriteRenderer>():null;
            var ladderNode=Node("LOGIC_129");
            Transform endObj=ladderNode!=null&&ladderNode.transform.childCount>1?ladderNode.transform.GetChild(1):null;
            if(ladderSprite!=null)ladderSprite.size=new Vector2(1,opened?8.5f:0f);
            if(endObj!=null)endObj.localPosition=new Vector3(opened?8.5f:0f,0,0);
            var triggerObj=Node("LAYOUT_64")??Node("LAYOUT_63");
            if(triggerObj!=null)triggerObj.gameObject.SetActive(opened);
        }
        System.Collections.IEnumerator LowerLadderCoroutine()
        {
            var ladderSpriteObj=Node("LOGIC_184");
            var ladderSprite=ladderSpriteObj!=null?ladderSpriteObj.GetComponent<SpriteRenderer>():null;
            var ladderNode=Node("LOGIC_129");
            Transform endObj=ladderNode!=null&&ladderNode.transform.childCount>1?ladderNode.transform.GetChild(1):null;
            float elapsed=0f,duration=.75f;
            while(elapsed<duration)
            {
                elapsed+=Time.deltaTime;
                float progress=Mathf.Clamp01(elapsed/duration);
                float h=Mathf.Lerp(0f,8.5f,progress);
                if(ladderSprite!=null)ladderSprite.size=new Vector2(1,h);
                if(endObj!=null)endObj.localPosition=new Vector3(h,0,0);
                yield return null;
            }
            ApplyLadderState(true);
        }
        bool NearBlueAltar=>game.Current!=null&&game.Current.id=="D17Z01S04"&&Mathf.Abs(game.player.transform.position.x-(-833.5f))<3.5f&&Mathf.Abs(game.player.transform.position.y-(-4.975f))<2.5f;
        bool TryInteractBlueAltar()
        {
            if(!NearBlueAltar)return false;
            bool first=!game.progress.HasFlag("D17Z01S04/BLUE_ALTAR_ACTIVATED");
            if(first)
            {
                game.progress.SetFlag("D17Z01S04/BLUE_ALTAR_ACTIVATED");
                game.progress.fervourUpgrades=Math.Max(1,game.progress.fervourUpgrades+1);
                game.player.fervour=game.player.MaxFervour;
                game.player.BeginCollect(false);
                game.effects.Burst(new Vector3(-832.5f,-3.5f,0),Color.cyan);
                game.Sfx("HEALING_EXPLOSION");
                game.Message("BÀN THỜ FERVOUR · LƯỢNG MANA TỐI ĐA ĐÃ GIA TĂNG (+20 FERVOUR)");
            }
            else
            {
                game.player.fervour=game.player.MaxFervour;
                game.player.BeginCollect(false);
                game.effects.Burst(new Vector3(-832.5f,-3.5f,0),new Color(.2f,.7f,1f));
                game.Sfx("SWITCH_ON");
                game.Message("BÀN THỜ FERVOUR · FERVOUR ĐÃ ĐƯỢC HỒI ĐẦY");
            }
            game.SaveGame();
            return true;
        }
        public bool CanInteract=>game.trials!=null&&game.trials.CanInteract||dialogue!=null&&dialogue.NearNpc!=null||NearBlueAltar;
        public bool TryInteract()=>game.trials!=null&&game.trials.TryInteract()||(dialogue!=null&&dialogue.TryInteract())||TryInteractBlueAltar();
        public SourceNpcDialogue Dialogue=>dialogue;
        public Transform FreeCherub{get{foreach(var c in captors)if(!c.rescued&&c.actor!=null)return c.actor.transform;return null;}}
    }
}
