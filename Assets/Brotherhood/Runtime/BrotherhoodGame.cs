using System;
using UnityEngine;
using UnityEngine.UI;
namespace Brotherhood
{
    public sealed class BrotherhoodGame : MonoBehaviour
    {
        public RoomState[] rooms;public PlayerController player;public TouchControls controls;public EffectPool effects;public Camera view;public RestoredAudio audioBank;public PlayerProgress progress=new PlayerProgress();public GuiltRuntime guilt;public ForbiddenZone forbiddenZone;public GameOverUI gameOverUI;public SourceItemPopup itemPopup;public ElderBrotherEncounter bossEncounter;public DeograciasEncounter deograciasEncounter;
        public RoomState Current {get;private set;}
        public bool BossFightActive {get;private set;}
        public bool InputBlocked {get;private set;}
        public bool IsBossDefeated=>bossDead;
        public string MessageText=>Time.unscaledTime<messageUntil?message:"";
        string message;float messageUntil,transitionCooldown,shake,executionZoom,cinematicZoom=5.625f;Transform executionFocus,cinematicFocus;
        Image roomFade;bool switchingRooms;
        bool mapDiscoveryDirty;float mapSaveRetryAfter;
        string checkpointRoom="D17Z01S01",resumeRoom;Vector2 checkpoint,resumePosition;bool bossDead,resumeLoaded;float resumeHealth,resumeFervour;int resumeFlasks;Vector3 cameraVelocity;
        [Serializable] class Save {public int version=6;public string room;public float x,y;public string currentRoom;public float currentX,currentY,health,fervour;public int flasks;public bool boss,bloodOwned,bloodEquipped;public PlayerProgress progress;public string[] litCheckpoints;}
        bool verification;
        public bool startWithBloodRelic;
        public bool BloodRelicOwned {get;private set;}
        public bool BloodRelicEquipped {get;private set;}
        public bool CanPray {get {if(Current==null||player==null)return false;foreach(var shrine in Current.checkpoints)if(NearShrine(shrine))return true;return false;} }
        public bool CanInteract {get {if(world!=null&&world.CanInteract||guilt!=null&&guilt.NearDrop()!=null||CanPray||NearSkillAltar||GetNearCollectible()!=null||deograciasEncounter!=null&&deograciasEncounter.CanInteract)return true;if(Current!=null)foreach(var enemy in Current.enemies)if(enemy!=null&&enemy.ExecutionReady&&player.motor.grounded&&enemy.motor.grounded&&Mathf.Abs(player.transform.position.y-enemy.transform.position.y)<=.45f&&Vector2.Distance(player.transform.position,enemy.transform.position)<1.8f)return true;return false;} }
        public bool NearSkillAltar {get {if(Current==null||player==null||player.motor==null||Current.skillAltars==null)return false;foreach(var altar in Current.skillAltars)if(altar.Contains(player.transform.position,player.motor.size))return true;return false;} }
        public int RosarySlots=>new InventoryModifiers(progress).RosarySlots;
        public PriorityTwoPrayers prayerEffects;
        public LegacyPrayerEffects legacyPrayers;
        public PrayerBuffVisuals prayerBuffs;
        public InventoryEffectRuntime itemEffects;public ProgressionEvents events;public SourceWorldRuntime world;public SourceBloodyBaptism bloodyBaptism;public BrotherhoodTrialRoute trials;
        public bool HasPrayerCheckpoint=>Find(checkpointRoom)!=null&&Find(checkpointRoom).checkpoints.Length>0&&litShrines.Count>0;
        public bool TeleportToPrayerCheckpoint()
        {
            if(!HasPrayerCheckpoint||player.Dead)return false;
            Enter(checkpointRoom,null,checkpoint);SaveGame();return true;
        }
        public bool TryUseSkillAltar()
        {
            if(!NearSkillAltar||controls==null||controls.inventoryUI==null)return false;
            foreach(var altar in Current.skillAltars)if(altar.Contains(player.transform.position,player.motor.size))
            {
                var identity=altar.sensor.GetComponent<SourceObjectId>();
                string id=Current.id+"/"+(identity!=null?identity.node:altar.sensor.name);
                if(progress.ActivateSkillAltar(id)){SaveGame();Message("Mea Culpa · "+progress.meaCulpaLevel);}
                if(trials!=null)trials.OnAltarActivated(id);
                break;
            }
            player.BeginAltarInteraction();return true;
        }
        public MudZone CurrentMud {get {if(progress.IsEquipped("RE03"))return null;if(Current==null||player==null||player.motor==null||Current.mudZones==null)return null;foreach(var mud in Current.mudZones)if(mud.Contains(player.transform.position,player.motor.size))return mud;return null;} }
        public CollectibleZone GetNearCollectible(){if(Current==null||player==null||player.motor==null||Current.collectibles==null||!player.motor.grounded)return null;foreach(var item in Current.collectibles)if(!progress.Owns(item.item)&&item.Contains(player.transform.position,player.motor.size))return item;return null;}
        public bool TryCollectItem(){var item=GetNearCollectible();if(item==null)return false;progress.SetOwned(item.item,true);item.root.SetActive(false);float duration=player.BeginCollect(item.halfHeight);var catalog=InventoryCatalog.Load();var definition=catalog==null?null:catalog.Find(item.item);SaveGame();StartCoroutine(ShowCollectedItem(duration,definition));return true;}
        System.Collections.IEnumerator ShowCollectedItem(float duration,InventoryCatalog.Item definition)
        {
            yield return new WaitForSeconds(Mathf.Max(.05f,duration));
            if(definition!=null&&itemPopup!=null)itemPopup.Show(definition);
        }
        public void SetItemPopupActive(bool active){InputBlocked=active;if(!active&&controls!=null)controls.ClearGameplayInput();}
        public bool ExecutionCameraActive=>executionFocus!=null;
        int SaveSlot=>Mathf.Clamp(PlayerPrefs.GetInt("BrotherhoodSaveSlot",1),1,3);
        string SavePath=>System.IO.Path.Combine(Application.persistentDataPath,verification?"brotherhood-verification.json":(SaveSlot==1?"brotherhood-save.json":"brotherhood-save-slot-"+SaveSlot+".json"));
        void Awake()
        {
            Application.runInBackground=true;Application.targetFrameRate=PlayerPrefs.GetInt("BrotherhoodFrameRate",60);QualitySettings.vSyncCount=0;Time.fixedDeltaTime=1f/60;Physics2D.queriesHitTriggers=false;
            var fadeCanvas=new GameObject("Source room transition fade",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            fadeCanvas.transform.SetParent(transform,false);
            var canvas=fadeCanvas.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.overrideSorting=true;canvas.sortingOrder=32000;
            var fadePanel=new GameObject("Black fade",typeof(RectTransform),typeof(Image));fadePanel.transform.SetParent(fadeCanvas.transform,false);
            var rect=fadePanel.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            roomFade=fadePanel.GetComponent<Image>();roomFade.color=new Color(0,0,0,0);roomFade.raycastTarget=false;
            controls.game=this;player.game=this;
            SourceWorldRuntime.AttachRooms(this);
            trials=gameObject.AddComponent<BrotherhoodTrialRoute>();trials.Initialize(this);
            // Resources/core/Penitent.prefab renders on Player, order 1. The
            // runtime assignment also corrects scenes built before this import.
            player.actor.visual.sortingLayerName="Player";player.actor.visual.sortingOrder=1;
            gameOverUI=gameObject.AddComponent<GameOverUI>();gameOverUI.Initialize(this);
            itemPopup=gameObject.AddComponent<SourceItemPopup>();itemPopup.Initialize(this);
            foreach(var room in rooms){foreach(var s in room.checkpoints){var l2=s.Find("Interactable Animation_level2");if(l2!=null)l2.gameObject.SetActive(false);var l3=s.Find("Interactable Animation_level3");if(l3!=null)l3.gameObject.SetActive(false);var l1=s.Find("Interactable Animation_level1");if(l1!=null)l1.gameObject.SetActive(true);}foreach(var enemy in room.enemies){enemy.game=this;enemy.Initialize();}foreach(var p in room.parallax)p.origin=p.target.position;room.gameObject.SetActive(false);}
            bossEncounter=gameObject.AddComponent<ElderBrotherEncounter>();bossEncounter.Initialize(this);
            deograciasEncounter=gameObject.AddComponent<DeograciasEncounter>();deograciasEncounter.Initialize(this);
            checkpoint=rooms[0].start.position;
            bool fullVerification=Application.isEditor&&System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"../Temp/brotherhood-verify-play"));
            verification=fullVerification||Application.isEditor&&(System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"../Temp/brotherhood-priority-one-verify"))||System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"../Temp/brotherhood-priority-two-verify"))||System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"../Temp/brotherhood-prayers-verify"))||System.IO.File.Exists(System.IO.Path.Combine(Application.dataPath,"../Temp/brotherhood-trial-verify")));
            if(!verification){if(PlayerPrefs.GetInt("BrotherhoodNewPilgrimage",0)==1){PlayerPrefs.DeleteKey("BrotherhoodNewPilgrimage");TryDeleteSave();}else LoadSave();}
            // Keep the full catalog available only to automated verification.
            // A normal pilgrimage receives QI31 from Deogracias' source FSM.
            var cat = InventoryCatalog.Load();
            if(fullVerification&&cat!=null&&cat.items!=null){
                var allIds=new System.Collections.Generic.List<string>();
                foreach(var it in cat.items)if(!string.IsNullOrEmpty(it.id))allIds.Add(it.id);
                progress.ownedItems=allIds.ToArray();progress.unlockAllItems=true;
            }
            progress.Normalize(6,cat);
            if(progress.thornGranted)progress.SetOwned("QI31",true);
            if(startWithBloodRelic){BloodRelicOwned=true;BloodRelicEquipped=true;}
            progress.SetOwned("RE01",BloodRelicOwned);
            SyncBloodRelicEquipment();
            mapDiscoveryDirty|=SourceMap.Load().MigrateLegacy(progress,false);guilt=gameObject.AddComponent<GuiltRuntime>();guilt.Initialize(this);
            prayerEffects=gameObject.AddComponent<PriorityTwoPrayers>();prayerEffects.Initialize(this);
            legacyPrayers=gameObject.AddComponent<LegacyPrayerEffects>();legacyPrayers.Initialize(this);
            prayerBuffs=gameObject.AddComponent<PrayerBuffVisuals>();prayerBuffs.Initialize(this);
            itemEffects=gameObject.AddComponent<InventoryEffectRuntime>();itemEffects.Initialize(this);events=gameObject.AddComponent<ProgressionEvents>();events.Initialize(this);
            world=gameObject.AddComponent<SourceWorldRuntime>();world.Initialize(this);
            bloodyBaptism=gameObject.AddComponent<SourceBloodyBaptism>();bloodyBaptism.Initialize(this);bloodyBaptism.SkipForVerification=verification;
            Enter(resumeLoaded?resumeRoom:checkpointRoom,null,resumeLoaded?resumePosition:checkpoint);player.Restore();
            if(resumeLoaded){player.health=Mathf.Clamp(resumeHealth,1,player.MaxHealth);player.fervour=Mathf.Clamp(resumeFervour,0,player.MaxFervour);player.flasks=Mathf.Clamp(resumeFlasks,0,player.MaxFlasks);}
            Message("BROTHERHOOD OF THE SILENT SORROW");
            InitForbiddenZone();
        }
        void InitForbiddenZone(){if(forbiddenZone==null){var fz=new GameObject("ForbiddenZoneSystem").AddComponent<ForbiddenZone>();fz.transform.SetParent(transform);fz.game=this;forbiddenZone=fz;}}
        void Start(){if(audioBank!=null)audioBank.EnterRegion(Current?.id);if(!verification&&!System.IO.File.Exists(SavePath))player.Awaken();}
        void PersistMapDiscovery()
        {
            // Entry discovery is flushed after Awake restores saved resources
            // and Start decides whether this is the first awakening.
            if(mapDiscoveryDirty&&player!=null&&!player.Dead&&Time.unscaledTime>=mapSaveRetryAfter)SaveGame();
        }
        void Update()
        {
            if(Current==null)return;
            if(controls!=null&&controls.mapUI!=null&&controls.mapUI.IsOpen){controls.ClearGameplayInput();PersistMapDiscovery();return;}
            if(controls!=null&&controls.debugUI!=null&&controls.debugUI.IsOpen){controls.ClearGameplayInput();PersistMapDiscovery();return;}
            float dt=Mathf.Min(Time.deltaTime,.0334f);if(!InputBlocked)controls.Sample();else controls.ClearGameplayInput(true);
            transitionCooldown-=dt;
            if(forbiddenZone!=null)forbiddenZone.Tick(dt);
            if(!InputBlocked&&trials!=null)trials.HandleWalkPassage();
            if(!InputBlocked)player.Tick(dt);else player.motor.velocity=Vector2.zero;
            if(trials!=null)trials.KeepPlayerAtWalkGate();
            foreach(var enemy in Current.enemies)enemy.Tick(dt);
            if(guilt!=null)guilt.RememberFloor();
            if(world!=null)world.Tick(dt);
            if(trials!=null)trials.Tick(dt);
            if(!player.Dead&&SourceMap.Load().Discover(progress,Current.id,(Vector2)player.transform.position+Vector2.up*.5f))mapDiscoveryDirty=true;
            PersistMapDiscovery();
            foreach(var p in Current.faithPlatforms)p.Tick(player.transform.position,dt);
            if(transitionCooldown<=0 && !switchingRooms && !player.Dead)
            foreach(var door in Current.doors)
            {
                // Trial doors retain their authored data and use the route's
                // directional gate rules instead of the source destination.
                if(trials!=null&&(trials.HandlesDoor(Current,door)||!trials.AllowSourceDoor(Current,door)))continue;
                if(world!=null&&!world.DoorAllowed(door))continue;
                var delta=player.transform.position-door.trigger.position;
                if(Mathf.Abs(delta.x)<.6f && Mathf.Abs(delta.y)<2.5f)
                {
                    if(Current.Boss!=null&&!Current.Boss.Dead){Message("Defeat the Warden to open the gate");break;}
                    if(Find(door.target)!=null){StartCoroutine(ChangeRoom(door.target,door.targetDoor,Current.id));break;}
                    Message("End of restored route");transitionCooldown=2;break;
                }
            }
        }
        void LateUpdate()
        {
            if(Current==null)return;
            float h=view.orthographicSize,w=h*view.aspect;
            Transform focus=cinematicFocus!=null?cinematicFocus:executionFocus;
            // Look ahead only while the Penitent is actually travelling.  Using
            // facing alone made the camera slide when a combat boundary blocked
            // the player, which looked like lost movement input in the boss room.
            float lookAhead=focus!=null||Mathf.Abs(player.motor.velocity.x)<.05f?0:Mathf.Sign(player.motor.velocity.x)*.8f;
            Vector3 target=(focus!=null?focus.position:player.transform.position)+new Vector3(lookAhead,focus!=null?1.1f:2.4f,-10);
            var bounds=Current.CameraBoundsFor(player.transform.position);
            target.x=bounds.right-bounds.left>2*w?Mathf.Clamp(target.x,bounds.left+w,bounds.right-w):(bounds.left+bounds.right)*.5f;
            target.y=bounds.top-bounds.bottom>2*h?Mathf.Clamp(target.y,bounds.bottom+h,bounds.top-h):(bounds.bottom+bounds.top)*.5f;
            view.transform.position=Vector3.SmoothDamp(view.transform.position,target,ref cameraVelocity,.12f);
            view.orthographicSize=Mathf.MoveTowards(view.orthographicSize,cinematicFocus!=null?cinematicZoom:executionFocus!=null?executionZoom:5.625f,Time.unscaledDeltaTime*4);
            if(!GameSettings.ScreenShake)shake=0;
            if(shake>0){shake-=Time.deltaTime;view.transform.position+=new Vector3(UnityEngine.Random.Range(-.07f,.07f),UnityEngine.Random.Range(-.07f,.07f),0);}
            PositionParallax();
        }
        void PositionParallax()
        {
            foreach(var p in Current.parallax)if(p.target!=null)
            {
                Vector3 position=p.origin+new Vector3((view.transform.position.x-p.cameraOrigin.x)*p.speed,(view.transform.position.y-p.cameraOrigin.y)*p.speedY,0);
                p.target.position=new Vector3(Mathf.Floor(position.x*32)/32f,Mathf.Floor(position.y*32)/32f,position.z);
            }
        }
        public bool TryTrialPassage(string target,Vector2 arrival,bool allowDuringCooldown=false)
        {
            if(Current==null||Find(target)==null||InputBlocked||switchingRooms||(!allowDuringCooldown&&transitionCooldown>0)||player.Dead)return false;
            StartCoroutine(ChangeRoom(target,null,Current.id,arrival));return true;
        }
        public bool TrialPassageCoolingDown=>transitionCooldown>0;
        System.Collections.IEnumerator ChangeRoom(string target,string targetDoor,string completed,Vector2? arrival=null)
        {
            // LevelManager in the source keeps the scene covered while the old
            // room deactivates, then fades from black over 0.6 seconds.
            switchingRooms=true;InputBlocked=true;controls.ClearGameplayInput(true);player.EnterRoomTransitionIdle();
            const float coverDuration=.25f;
            for(float elapsed=0;elapsed<coverDuration;elapsed+=Time.unscaledDeltaTime)
            {roomFade.color=new Color(0,0,0,Mathf.Clamp01(elapsed/coverDuration));yield return null;}
            roomFade.color=Color.black;
            Enter(target,targetDoor,arrival);progress.CompleteRoom(completed);SaveGame();
            InputBlocked=true;
            yield return new WaitForEndOfFrame();
            const float revealDuration=.6f;
            for(float elapsed=0;elapsed<revealDuration;elapsed+=Time.unscaledDeltaTime)
            {roomFade.color=new Color(0,0,0,1f-Mathf.Clamp01(elapsed/revealDuration));yield return null;}
            roomFade.color=new Color(0,0,0,0);InputBlocked=false;switchingRooms=false;
        }
        void SnapCameraToRoom(Vector2 spawn)
        {
            float h=view.orthographicSize,w=h*view.aspect;
            var bounds=Current.CameraBoundsFor(spawn);
            float x=bounds.right-bounds.left>2*w?Mathf.Clamp(spawn.x,bounds.left+w,bounds.right-w):(bounds.left+bounds.right)*.5f;
            float y=bounds.top-bounds.bottom>2*h?Mathf.Clamp(spawn.y+2.4f,bounds.bottom+h,bounds.top-h):(bounds.bottom+bounds.top)*.5f;
            view.transform.position=new Vector3(x,y,-10);cameraVelocity=Vector3.zero;
            PositionParallax();
        }
        public RoomState Find(string id){foreach(var room in rooms)if(room.id==id)return room;return null;}
        public void Enter(string id,string door,Vector2? location)
        {
            if(bloodyBaptism!=null&&bloodyBaptism.IsPlaying)bloodyBaptism.Cancel();
            var next=Find(id);if(next==null)return;player.StopPrayers();if(Current!=null)Current.gameObject.SetActive(false);
            Current=next;Current.gameObject.SetActive(true);Current.EnsureFloorContinuity();Current.RefreshShockGate(progress,player.actor.catalog);Current.ResetCameraZone();Physics2D.SyncTransforms();
            Current.RefreshCollectibles(progress);
            foreach(var p in Current.faithPlatforms)p.SetRelic(BloodRelicOwned&&BloodRelicEquipped);
            Vector2 spawn=location??(Vector2)next.start.position;var entry=next.Door(door);
            if(location==null && entry!=null)spawn=entry.spawn.position;
            // Keep arrival clear of the door sensor and snap to nearby floor when available.
            if(entry!=null && location==null)spawn.x+=entry.key=="W"?1:-1;
            if(trials!=null&&location==null)spawn=trials.ResolveSourceArrival(next,entry,spawn);
            var floor=Physics2D.Raycast(spawn+Vector2.up*1.5f,Vector2.down,5,1<<8);
            if(floor.collider!=null)spawn.y=floor.point.y+.03f;
            player.motor.Teleport(spawn);player.EnterRoomTransitionIdle();SnapCameraToRoom(spawn);transitionCooldown=1.2f;
            if(bossDead && Current.Boss!=null)Current.Boss.health=0;
            Current.ResetBreakables();
            foreach(var s in Current.checkpoints)
            {
                var l2=s.Find("Interactable Animation_level2");if(l2!=null)l2.gameObject.SetActive(false);
                var l3=s.Find("Interactable Animation_level3");if(l3!=null)l3.gameObject.SetActive(false);
                var l1=s.Find("Interactable Animation_level1");if(l1!=null)l1.gameObject.SetActive(true);
                var sa=s.GetComponentInChildren<SpriteActor>();if(sa!=null)sa.Play(IsShrineLit(s)?"Priedieu_shrine_lit_on":"Priedieu_shrine_off",true);
            }
            mapDiscoveryDirty|=SourceMap.Load().Discover(progress,Current.id,(Vector2)player.transform.position+Vector2.up*.5f);if(guilt!=null)guilt.RefreshRoom();if(world!=null)world.OnRoomEntered();
            if(trials!=null)trials.OnRoomEntered();
            if(effects!=null)effects.Clear();BossFightActive=false;InputBlocked=false;if(audioBank!=null)audioBank.EnterRegion(Current.id);Message(id=="D17Z01S11"?"WARDEN OF THE SILENT SORROW":id=="D01Z01S07"?"THE HOLY LINE":"BROTHERHOOD OF THE SILENT SORROW");if(bossEncounter!=null)bossEncounter.OnRoomEntered(Current);if(deograciasEncounter!=null)deograciasEncounter.OnRoomEntered(Current);
        }
        readonly System.Collections.Generic.HashSet<string> litShrines=new System.Collections.Generic.HashSet<string>();
        string ShrineKey(Transform shrine)=>shrine==null?"":((Current!=null?Current.id:"")+"_"+shrine.name);
        public bool IsShrineLit(Transform shrine){if(shrine==null)return false;string k=ShrineKey(shrine);return litShrines.Contains(k)||(Current!=null&&Current.id==checkpointRoom);}
        public void MarkShrineLit(Transform shrine){if(shrine!=null)litShrines.Add(ShrineKey(shrine));}
        public Transform GetNearShrine(){if(Current==null)return null;foreach(var s in Current.checkpoints)if(NearShrine(s))return s;return null;}
        public bool NearShrine(Transform shrine){if(shrine==null||player==null)return false;var delta=(Vector2)player.transform.position-(Vector2)shrine.position;return Mathf.Abs(delta.x)<=0.85f&&Mathf.Abs(delta.y)<=2.2f;}
        public void Interact(bool animate=true,bool validated=false)
        {
            if(!validated&&deograciasEncounter!=null&&deograciasEncounter.TryBegin())return;
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
        public void ShowGameOver(){if(controls!=null)controls.SetControlsVisible(false);if(gameOverUI!=null)gameOverUI.Show();else Respawn();}
        public void Respawn(){if(gameOverUI!=null)gameOverUI.Hide();if(controls!=null)controls.SetControlsVisible(true);Enter(checkpointRoom,null,checkpoint);Current.ResetEnemies();if(bossDead&&Current.Boss!=null)Current.Boss.health=0;player.Restore();}
        public void BossDefeated()
        {
            bool firstGrant=!progress.achievementAC01;
            bossDead=true;progress.wardenDefeated=true;progress.achievementAC01=true;progress.campaignWon=true;progress.CompleteRoom("D17Z01S11");SaveGame();Sfx("ELDER_BROTHER_DEATH");
            BossFightActive=false;if(audioBank!=null)audioBank.EndBossMusic();Message("REQUIEM AETERNAM · the way is open");
            if(bossEncounter!=null)bossEncounter.OnBossDefeated(firstGrant);
        }
        public void EnemyDefeated(int sourcePurge)
        {
            progress.tears=Mathf.Max(0,progress.tears+sourcePurge*new InventoryModifiers(progress,player).TearsMultiplier*GuiltRules.Load().Tears(progress));
            player.ItemEvent(8,0,player.ExecutionRewardContext,player.HeavyRewardContext);
            if(prayerBuffs!=null)prayerBuffs.EnemyKilled();
        }
        public void Message(string text){message=text;messageUntil=Time.unscaledTime+3;}
        public void Shake(float time){shake=GameSettings.ScreenShake?time:0;}
        public void SetBossFightActive(bool active){BossFightActive=active;if(active&&audioBank!=null)audioBank.Music(true);}
        public void SetEncounterInputBlocked(bool blocked){InputBlocked=blocked;if(controls!=null)controls.SetControlsVisible(!blocked);}
        public void BeginCinematicCamera(Transform target,float zoom){cinematicFocus=target;cinematicZoom=Mathf.Clamp(zoom,4f,6f);cameraVelocity=Vector3.zero;}
        public void EndCinematicCamera(){cinematicFocus=null;cameraVelocity=Vector3.zero;}
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
            else if(item.category=="prayer"){player.StopPrayers();progress.equippedPrayer=progress.equippedPrayer==item.id?"":item.id;progress.hasPrayer=!string.IsNullOrEmpty(progress.equippedPrayer);}
            else if(item.category=="sword"){if(itemEffects!=null&&itemEffects.HasTemporalFlag(5)){Message("Không thể tháo Trái Tim đã hợp nhất với Mea Culpa.");return false;}progress.equippedSwordHeart=progress.equippedSwordHeart==item.id?"":item.id;}
            else if(item.category=="ability")return false;
            else if(item.category=="rosarybead")
            {
                var list=new System.Collections.Generic.List<string>(progress.equippedRosaryBeads??Array.Empty<string>());
                if(list.Remove(item.id)){}else{if(list.Count>=RosarySlots){Message("Đã dùng hết nút chuỗi hạt");return false;}list.Add(item.id);}
                progress.equippedRosaryBeads=list.ToArray();
            }
            else return false;
            SaveGame();return true;
        }
        public bool CanPurchaseInventorySkill(InventoryCatalog.Item item)
        {
            if(item==null||item.category!="ability"||item.skillCost<=0||progress.Owns(item.id))return false;
            if(!item.id.StartsWith("CHARGED_")&&!item.id.StartsWith("LUNGE_")&&!item.id.StartsWith("RANGED_")&&!item.id.StartsWith("COMBO_")&&!item.id.StartsWith("VERTICAL_"))return false;
            if(!string.IsNullOrEmpty(item.parentSkill)&&item.parentSkill!="NO DEPENDENCY"&&!progress.Owns(item.parentSkill))return false;
            if(new InventoryModifiers(progress).MeaCulpaLevel<item.skillTier)return false;
            return progress.tears>=item.skillCost;
        }
        public bool PurchaseInventorySkill(InventoryCatalog.Item item)
        {
            if(!CanPurchaseInventorySkill(item))return false;
            progress.tears-=item.skillCost;
            progress.SetOwned(item.id,true);
            int tier=int.Parse(item.id.Substring(item.id.LastIndexOf('_')+1));
            if(item.id.StartsWith("CHARGED_"))progress.chargedTier=Math.Max(progress.chargedTier,tier);
            else if(item.id.StartsWith("LUNGE_"))progress.lungeTier=Math.Max(progress.lungeTier,tier);
            else if(item.id.StartsWith("RANGED_"))progress.rangedTier=Math.Max(progress.rangedTier,tier);
            else if(item.id.StartsWith("COMBO_"))progress.comboTier=Math.Max(progress.comboTier,tier);
            else if(item.id.StartsWith("VERTICAL_"))progress.verticalTier=Math.Max(progress.verticalTier,tier);
            progress.hasSpecial=progress.rangedTier>0||progress.lungeTier>0;progress.specialMode=progress.rangedTier>0?1:0;
            SaveGame();
            return true;
        }
        public void SaveGame()
        {
            try
            {
                // Keep the Prie Dieu as the death checkpoint while persisting
                // the newly reached source room for the next launch.
                bool alive=Current!=null&&player!=null&&!player.Dead;
                string currentRoom=alive?Current.id:checkpointRoom;
                Vector2 position=alive?(Vector2)player.transform.position:checkpoint;
                var s=new Save{room=checkpointRoom,x=checkpoint.x,y=checkpoint.y,currentRoom=currentRoom,currentX=position.x,currentY=position.y,health=alive?player.health:player.MaxHealth,fervour=alive?player.fervour:player.MaxFervour,flasks=alive?player.flasks:player.MaxFlasks,boss=bossDead,bloodOwned=BloodRelicOwned,bloodEquipped=BloodRelicEquipped,progress=progress,litCheckpoints=new System.Collections.Generic.List<string>(litShrines).ToArray()};
                SafeSaveFile.Write(SavePath,JsonUtility.ToJson(s),ValidSave);
                mapDiscoveryDirty=false;mapSaveRetryAfter=0;
            }
            catch(Exception e){if(mapDiscoveryDirty)mapSaveRetryAfter=Time.unscaledTime+1f;Debug.LogWarning("Save failed: "+e.Message);Message("Unable to save progress");}
        }
        bool ValidSave(string json)
        {
            try
            {
                var value=JsonUtility.FromJson<Save>(json);
                return value!=null&&value.version>=1&&value.version<=6&&Find(value.room)!=null
                    &&(value.version<6||value.progress!=null)&&(value.progress==null||Finite(value.progress.tears))&&Finite(value.x)&&Finite(value.y)&&(value.version<5||Find(value.currentRoom)!=null&&Finite(value.currentX)&&Finite(value.currentY)&&Finite(value.health)&&Finite(value.fervour));
            }
            catch(ArgumentException){return false;}
        }
        static bool Finite(float value){return !float.IsNaN(value)&&!float.IsInfinity(value);}
        void LoadSave()
        {
            try
            {
                if(!SafeSaveFile.TryRead(SavePath,ValidSave,out string json,out bool recovered))return;
                var s=JsonUtility.FromJson<Save>(json);
                if(recovered)Debug.Log("Recovered pilgrimage from a validated save backup");
                checkpointRoom=s.room;checkpoint=new Vector2(s.x,s.y);bossDead=s.boss;
                BloodRelicOwned=s.bloodOwned;BloodRelicEquipped=s.bloodOwned&&s.bloodEquipped;
                if(s.progress!=null)progress=s.progress;
                progress.Normalize(s.version,InventoryCatalog.Load());
                if(bossDead){progress.wardenDefeated=true;progress.achievementAC01=true;progress.campaignWon=true;progress.CompleteRoom("D17Z01S11");}
                // Only saves written before per-cell discovery existed may
                // recover approximate exploration from their room history.
                mapDiscoveryDirty=SourceMap.Load().MigrateLegacy(progress,!json.Contains("\"discoveredMapCells\""));
                mapSaveRetryAfter=0;
                progress.bloodOwned=BloodRelicOwned;progress.bloodEquipped=BloodRelicEquipped;
                if(s.litCheckpoints!=null)foreach(var id in s.litCheckpoints)litShrines.Add(id);
                if(!string.IsNullOrEmpty(checkpointRoom))litShrines.Add(checkpointRoom+"_ACT_PrieDieu");
                if(s.version>=5&&Find(s.currentRoom)!=null&&!float.IsNaN(s.currentX)&&!float.IsNaN(s.currentY))
                {resumeLoaded=true;resumeRoom=s.currentRoom;resumePosition=new Vector2(s.currentX,s.currentY);resumeHealth=s.health;resumeFervour=s.fervour;resumeFlasks=s.flasks;}
            }
            catch(Exception e){Debug.LogWarning("Ignoring unreadable save: "+e.Message);}
        }
        void TryDeleteSave(){try{foreach(string path in new[]{SavePath,SavePath+".bak",SavePath+".tmp"})if(System.IO.File.Exists(path))System.IO.File.Delete(path);}catch(Exception e){Debug.LogWarning("New pilgrimage could not clear save: "+e.Message);}}
        void OnApplicationPause(bool paused){Time.timeScale=paused?0:1;}
        void OnDestroy(){Time.timeScale=1;}
    }
}
