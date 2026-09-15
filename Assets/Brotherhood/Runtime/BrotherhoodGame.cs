using System;
using UnityEngine;
namespace Brotherhood
{
    public sealed class BrotherhoodGame : MonoBehaviour
    {
        public RoomState[] rooms;public PlayerController player;public TouchControls controls;public EffectPool effects;public Camera view;public RestoredAudio audioBank;public PlayerProgress progress=new PlayerProgress();
        public RoomState Current {get;private set;}
        public string MessageText=>Time.unscaledTime<messageUntil?message:"";
        string message;float messageUntil,transitionCooldown,shake,executionZoom;Transform executionFocus;
        string checkpointRoom="D17Z01S01";Vector2 checkpoint;bool bossDead;Vector3 cameraVelocity;
        [Serializable] class Save {public int version=4;public string room;public float x,y;public bool boss,bloodOwned,bloodEquipped;public PlayerProgress progress;public string[] litCheckpoints;}
        bool verification;
        public bool startWithBloodRelic;
        public bool BloodRelicOwned {get;private set;}
        public bool BloodRelicEquipped {get;private set;}
        public bool CanPray {get {if(Current==null||player==null)return false;foreach(var shrine in Current.checkpoints)if(NearShrine(shrine))return true;return false;} }
        public bool CanInteract {get {if(CanPray)return true;if(Current!=null)foreach(var enemy in Current.enemies)if(enemy!=null&&enemy.ExecutionReady&&player.motor.grounded&&enemy.motor.grounded&&Mathf.Abs(player.transform.position.y-enemy.transform.position.y)<=.45f&&Vector2.Distance(player.transform.position,enemy.transform.position)<1.8f)return true;return false;} }
        public bool ExecutionCameraActive=>executionFocus!=null;
        string SavePath=>System.IO.Path.Combine(Application.persistentDataPath,verification?"brotherhood-verification.json":"brotherhood-save.json");
        void Awake()
        {
            Application.runInBackground=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.fixedDeltaTime=1f/60;Physics2D.queriesHitTriggers=false;
            controls.game=this;player.game=this;
            foreach(var room in rooms){foreach(var s in room.checkpoints){var l2=s.Find("Interactable Animation_level2");if(l2!=null)l2.gameObject.SetActive(false);var l3=s.Find("Interactable Animation_level3");if(l3!=null)l3.gameObject.SetActive(false);var l1=s.Find("Interactable Animation_level1");if(l1!=null)l1.gameObject.SetActive(true);}foreach(var enemy in room.enemies){enemy.game=this;enemy.Initialize();}foreach(var p in room.parallax)p.origin=p.target.localPosition;room.gameObject.SetActive(false);}
            checkpoint=rooms[0].start.position;
            verification=Application.isEditor && System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"../Temp/brotherhood-verify-play"));
            if(!verification){if(PlayerPrefs.GetInt("BrotherhoodNewPilgrimage",0)==1){PlayerPrefs.DeleteKey("BrotherhoodNewPilgrimage");TryDeleteSave();}else LoadSave();}
            // Unlock all inventory items from catalog for player testing
            var cat = InventoryCatalog.Load();
            if(cat!=null&&cat.items!=null){
                var allIds=new System.Collections.Generic.List<string>();
                foreach(var it in cat.items)if(!string.IsNullOrEmpty(it.id))allIds.Add(it.id);
                progress.ownedItems=allIds.ToArray();
            }
            progress.hasSpecial=true;
            if(progress.rangedTier<1)progress.rangedTier=1;
            if(progress.specialMode<1)progress.specialMode=1;
            progress.SetOwned("QI31",true);
            if(startWithBloodRelic){BloodRelicOwned=true;BloodRelicEquipped=true;}
            progress.SetOwned("RE01",BloodRelicOwned);
            SyncBloodRelicEquipment();
            Enter(checkpointRoom,null,checkpoint);player.Restore();
            Message("BROTHERHOOD OF THE SILENT SORROW");
        }
        void Start(){if(audioBank!=null)audioBank.Music(Current.Boss!=null&&!bossDead);if(!verification&&!System.IO.File.Exists(SavePath))player.Awaken();}
        void Update()
        {
            if(Current==null)return;
            controls.Sample();float dt=Mathf.Min(Time.deltaTime,.0334f);
            transitionCooldown-=dt;
            player.Tick(dt);foreach(var enemy in Current.enemies)enemy.Tick(dt);
            foreach(var p in Current.faithPlatforms)p.Tick(player.transform.position,dt);
            if(transitionCooldown<=0 && !player.Dead)
            foreach(var door in Current.doors)
            {
                var delta=player.transform.position-door.trigger.position;
                if(Mathf.Abs(delta.x)<.6f && Mathf.Abs(delta.y)<2.5f)
                {
                    if(Current.Boss!=null&&!Current.Boss.Dead){Message("Defeat the Warden to open the gate");break;}
                    if(Find(door.target)!=null){Enter(door.target,door.targetDoor,null);break;}
                    Message("End of restored route");transitionCooldown=2;break;
                }
            }
        }
        void LateUpdate()
        {
            if(Current==null)return;
            float h=view.orthographicSize,w=h*view.aspect;
            Vector3 target=(executionFocus!=null?executionFocus.position:player.transform.position)+new Vector3(executionFocus!=null?0:player.facing*.8f,executionFocus!=null?1.1f:2.4f,-10);
            target.x=Current.right-Current.left>2*w?Mathf.Clamp(target.x,Current.left+w,Current.right-w):(Current.left+Current.right)*.5f;
            target.y=Current.top-Current.bottom>2*h?Mathf.Clamp(target.y,Current.bottom+h,Current.top-h):(Current.bottom+Current.top)*.5f;
            view.transform.position=Vector3.SmoothDamp(view.transform.position,target,ref cameraVelocity,.12f);
            view.orthographicSize=Mathf.MoveTowards(view.orthographicSize,executionFocus!=null?executionZoom:5.625f,Time.unscaledDeltaTime*4);
            if(shake>0){shake-=Time.deltaTime;view.transform.position+=new Vector3(UnityEngine.Random.Range(-.07f,.07f),UnityEngine.Random.Range(-.07f,.07f),0);}
            Vector3 pOrigin=Current.parallaxOrigin!=Vector3.zero?Current.parallaxOrigin:Current.start.position;
            foreach(var p in Current.parallax)if(p.target!=null)p.target.localPosition=p.origin+new Vector3((view.transform.position.x-pOrigin.x)*p.speed,(view.transform.position.y-pOrigin.y)*p.speedY,0);
        }
        public RoomState Find(string id){foreach(var room in rooms)if(room.id==id)return room;return null;}
        public void Enter(string id,string door,Vector2? location)
        {
            var next=Find(id);if(next==null)return;if(Current!=null)Current.gameObject.SetActive(false);
            Current=next;Current.gameObject.SetActive(true);Current.EnsureFloorContinuity();Physics2D.SyncTransforms();
            foreach(var p in Current.faithPlatforms)p.SetRelic(BloodRelicOwned&&BloodRelicEquipped);
            Vector2 spawn=location??(Vector2)next.start.position;var entry=next.Door(door);
            if(location==null && entry!=null)spawn=entry.spawn.position;
            // Keep arrival clear of the door sensor and snap to nearby floor when available.
            if(entry!=null && location==null)spawn.x+=entry.key=="W"?1:-1;
            var floor=Physics2D.Raycast(spawn+Vector2.up*1.5f,Vector2.down,5,1<<8);
            if(floor.collider!=null)spawn.y=floor.point.y+.03f;
            player.motor.Teleport(spawn);view.transform.position=new Vector3(spawn.x,spawn.y+2.4f,-10);cameraVelocity=Vector3.zero;transitionCooldown=1.2f;
            if(bossDead && Current.Boss!=null)Current.Boss.health=0;
            Current.ResetBreakables();
            foreach(var s in Current.checkpoints)
            {
                var l2=s.Find("Interactable Animation_level2");if(l2!=null)l2.gameObject.SetActive(false);
                var l3=s.Find("Interactable Animation_level3");if(l3!=null)l3.gameObject.SetActive(false);
                var l1=s.Find("Interactable Animation_level1");if(l1!=null)l1.gameObject.SetActive(true);
                var sa=s.GetComponentInChildren<SpriteActor>();if(sa!=null)sa.Play(IsShrineLit(s)?"Priedieu_shrine_lit_on":"Priedieu_shrine_off",true);
            }
            if(effects!=null)effects.Clear();if(audioBank!=null)audioBank.Music(Current.Boss!=null&&!bossDead);Message(id=="D17Z01S11"?"WARDEN OF THE SILENT SORROW":"BROTHERHOOD OF THE SILENT SORROW");
        }
        readonly System.Collections.Generic.HashSet<string> litShrines=new System.Collections.Generic.HashSet<string>();
        string ShrineKey(Transform shrine)=>shrine==null?"":((Current!=null?Current.id:"")+"_"+shrine.name);
        public bool IsShrineLit(Transform shrine){if(shrine==null)return false;string k=ShrineKey(shrine);return litShrines.Contains(k)||(Current!=null&&Current.id==checkpointRoom);}
        public void MarkShrineLit(Transform shrine){if(shrine!=null)litShrines.Add(ShrineKey(shrine));}
        public Transform GetNearShrine(){if(Current==null)return null;foreach(var s in Current.checkpoints)if(NearShrine(s))return s;return null;}
        public bool NearShrine(Transform shrine){if(shrine==null||player==null)return false;var delta=(Vector2)player.transform.position-(Vector2)shrine.position;return Mathf.Abs(delta.x)<=0.85f&&Mathf.Abs(delta.y)<=2.2f;}
        public void Interact(bool animate=true,bool validated=false)
        {
            foreach(var shrine in Current.checkpoints)
                if(validated||NearShrine(shrine))
                {
                    checkpointRoom=Current.id;checkpoint=player.transform.position;player.health=player.MaxHealth;player.flasks=player.MaxFlasks;player.fervour=player.MaxFervour;Current.ResetEnemies();Current.ResetBreakables();if(bossDead&&Current.Boss!=null)Current.Boss.health=0;
                    MarkShrineLit(shrine);
                    var l2=shrine.Find("Interactable Animation_level2");if(l2!=null)l2.gameObject.SetActive(false);
                    var l3=shrine.Find("Interactable Animation_level3");if(l3!=null)l3.gameObject.SetActive(false);
                    var l1=shrine.Find("Interactable Animation_level1");if(l1!=null)l1.gameObject.SetActive(true);
                    var sa=shrine.GetComponentInChildren<SpriteActor>();if(sa!=null)sa.Play("Priedieu_shrine_lit_on",true);
                    if(animate)player.Kneel();
                    Sfx("FLASK_REFILL");SaveGame();Message("PRIE DIEU · progress saved");return;
                }
        }
        public void Respawn(){Enter(checkpointRoom,null,checkpoint);Current.ResetEnemies();if(bossDead&&Current.Boss!=null)Current.Boss.health=0;player.Restore();}
        public void BossDefeated(){bossDead=true;SaveGame();Sfx("ELDER_BROTHER_DEATH");if(audioBank!=null)audioBank.Music(false);Message("REQUIEM AETERNAM · the way is open");}
        public void EnemyDefeated(int sourcePurge)
        {
            float multiplier=player!=null&&player.ActivePrayer=="PR16"?1+new InventoryModifiers(progress).PrayerBonus("PR16",19):1;
            progress.tears=Mathf.Max(0,progress.tears+sourcePurge*multiplier);
        }
        public void Message(string text){message=text;messageUntil=Time.unscaledTime+3;}
        public void Shake(float time){shake=time;}
        public void BeginExecutionCamera(Transform target,float duration){executionFocus=target;executionZoom=Mathf.Max(3.8f,5.625f-Mathf.Min(1.2f,duration*.2f));Sfx("EXECUTION_EFFECT",.8f);}
        public void ZoomOutExecutionCamera(){if(executionFocus!=null)executionZoom=5.625f;}
        public void EndExecutionCamera(){executionFocus=null;Sfx("EXECUTION_EFFECT_END",.75f);}
        public void Sound(float pitch){Sfx("PENITENT_SIMPLE_DAMAGE_DEFAULT");}
        public void Sfx(string name,float volume=1){if(audioBank!=null)audioBank.Play(name,volume);}
        public void SetBloodRelic(bool owned,bool equipped)
        {BloodRelicOwned=owned;BloodRelicEquipped=owned&&equipped;progress.bloodOwned=BloodRelicOwned;progress.bloodEquipped=BloodRelicEquipped;progress.SetOwned("RE01",owned);SyncBloodRelicEquipment();foreach(var r in rooms)foreach(var p in r.faithPlatforms)p.SetRelic(BloodRelicEquipped);SaveGame();}
        void SyncBloodRelicEquipment()
        {
            var list=new System.Collections.Generic.List<string>(progress.equippedRelics??Array.Empty<string>());list.RemoveAll(v=>v=="RE01");if(BloodRelicEquipped)list.Insert(0,"RE01");progress.equippedRelics=list.ToArray();
        }
        public bool EquipInventoryItem(InventoryCatalog.Item item)
        {
            if(item==null||!progress.Owns(item.id))return false;
            if(item.category=="relic")
            {
                if(item.id=="RE01"){SetBloodRelic(true,!BloodRelicEquipped);return true;}
                var list=new System.Collections.Generic.List<string>(progress.equippedRelics??Array.Empty<string>());if(list.Remove(item.id)){}else{if(list.Count>=3)list.RemoveAt(list.Count-1);list.Add(item.id);}progress.equippedRelics=list.ToArray();
            }
            else if(item.category=="prayer"){progress.equippedPrayer=progress.equippedPrayer==item.id?"":item.id;progress.hasPrayer=!string.IsNullOrEmpty(progress.equippedPrayer);if(player!=null)player.fervour=player.MaxFervour;}
            else if(item.category=="sword"){progress.equippedSwordHeart=progress.equippedSwordHeart==item.id?"":item.id;}
            else if(item.category=="ability")
            {
                if(item.id.StartsWith("CHARGED_"))
                {
                    int t=int.Parse(item.id.Substring(8));
                    progress.chargedTier=progress.chargedTier==t?Math.Max(0,t-1):t;
                }
                else if(item.id.StartsWith("LUNGE_"))
                {
                    int t=int.Parse(item.id.Substring(6));
                    progress.lungeTier=progress.lungeTier==t?Math.Max(0,t-1):t;
                }
                else if(item.id.StartsWith("RANGED_"))
                {
                    int t=int.Parse(item.id.Substring(7));
                    progress.rangedTier=progress.rangedTier==t?Math.Max(0,t-1):t;
                }
            }
            else if(item.category=="rosarybead")
            {
                var list=new System.Collections.Generic.List<string>(progress.equippedRosaryBeads??Array.Empty<string>());
                if(list.Remove(item.id)){}else{if(list.Count>=8)list.RemoveAt(0);list.Add(item.id);}
                progress.equippedRosaryBeads=list.ToArray();
            }
            else return false;
            SaveGame();return true;
        }
        public void SaveGame()
        {try{var s=new Save{room=checkpointRoom,x=checkpoint.x,y=checkpoint.y,boss=bossDead,bloodOwned=BloodRelicOwned,bloodEquipped=BloodRelicEquipped,progress=progress,litCheckpoints=new System.Collections.Generic.List<string>(litShrines).ToArray()};string temp=SavePath+".tmp";System.IO.File.WriteAllText(temp,JsonUtility.ToJson(s));if(System.IO.File.Exists(SavePath))System.IO.File.Copy(SavePath,SavePath+".bak",true);System.IO.File.Copy(temp,SavePath,true);System.IO.File.Delete(temp);}catch(Exception e){Debug.LogWarning("Save failed: "+e.Message);Message("Unable to save progress");}}
        void LoadSave(){try{if(!System.IO.File.Exists(SavePath))return;var s=JsonUtility.FromJson<Save>(System.IO.File.ReadAllText(SavePath));if(s!=null&&s.version>=1&&Find(s.room)!=null&&!float.IsNaN(s.x)&&!float.IsNaN(s.y)){checkpointRoom=s.room;checkpoint=new Vector2(s.x,s.y);bossDead=s.boss;BloodRelicOwned=s.bloodOwned;BloodRelicEquipped=s.bloodOwned&&s.bloodEquipped;if(s.progress!=null)progress=s.progress;progress.bloodOwned=BloodRelicOwned;progress.bloodEquipped=BloodRelicEquipped;if(s.litCheckpoints!=null)foreach(var id in s.litCheckpoints)litShrines.Add(id);if(!string.IsNullOrEmpty(checkpointRoom))litShrines.Add(checkpointRoom+"_ACT_PrieDieu");}}catch(Exception e){Debug.LogWarning("Ignoring unreadable save: "+e.Message);}}
        void TryDeleteSave(){try{if(System.IO.File.Exists(SavePath))System.IO.File.Delete(SavePath);}catch(Exception e){Debug.LogWarning("New pilgrimage could not clear save: "+e.Message);}}
        void OnApplicationPause(bool paused){Time.timeScale=paused?0:1;}
        void OnDestroy(){Time.timeScale=1;}
    }
}
