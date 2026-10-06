using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Brotherhood;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Batch entry point runs real Play Mode in an isolated verification save.
[InitializeOnLoad]
public static class PriorityOneVerification
{
    const string PhaseKey="Brotherhood.PriorityOne.BatchPhase";
    const string ExitKey="Brotherhood.PriorityOne.ExitAfter";
    static string Flag=>Path.Combine(Application.dataPath,"../Temp/brotherhood-priority-one-verify");
    static PriorityOneVerification()
    {
        EditorApplication.update+=Attach;
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state!=PlayModeStateChange.EnteredEditMode||SessionState.GetInt(PhaseKey,0)==0)return;
            EditorApplication.delayCall+=NextSuite;
        };
    }
    public static void RunBatch(){Run(true);}
    public static void RunInEditor(){Run(false);}
    static void Run(bool exitAfter)
    {
        Directory.CreateDirectory("Temp");Directory.CreateDirectory("Documentation");
        File.Delete("Temp/brotherhood-command.txt");
        File.Delete("Temp/brotherhood-verify-play");File.Delete("Temp/brotherhood-menu-verify");
        SessionState.SetInt(PhaseKey,exitAfter?1:0);SessionState.SetBool(ExitKey,exitAfter);EditorSettings.enterPlayModeOptionsEnabled=false;
        BrotherhoodBuilder.Build();
        File.WriteAllText(Flag,"");EditorSceneManager.OpenScene(BrotherhoodBuilder.ScenePath);EditorApplication.isPlaying=true;
    }
    static void Attach()
    {
        if(EditorApplication.isPlaying&&File.Exists(Flag)&&UnityEngine.Object.FindAnyObjectByType<PriorityOnePlayCheck>()==null)
            new GameObject("Priority one verification").AddComponent<PriorityOnePlayCheck>();
    }
    static void NextSuite()
    {
        int phase=SessionState.GetInt(PhaseKey,0);
        if(phase==1)
        {
            SessionState.SetInt(PhaseKey,2);File.WriteAllText("Temp/brotherhood-verify-play","");
            EditorSceneManager.OpenScene(BrotherhoodBuilder.ScenePath);EditorApplication.isPlaying=true;
        }
        else if(phase==2)
        {
            SessionState.SetInt(PhaseKey,3);File.WriteAllText("Temp/brotherhood-menu-verify","");
            EditorSceneManager.OpenScene(BrotherhoodBuilder.MenuScenePath);EditorApplication.isPlaying=true;
        }
        else if(phase==3)
        {
            SessionState.SetInt(PhaseKey,0);
            bool passed=new[]{"priority-one-playtest-results.txt","playtest-results.txt","menu-playtest-results.txt"}.All(name=>File.Exists("Documentation/"+name)&&File.ReadAllText("Documentation/"+name).Contains("RESULT: PASSED"));
            Debug.Log("[PriorityOneVerification] All suites completed: "+passed);if(SessionState.GetBool(ExitKey,false))EditorApplication.Exit(passed?0:1);
        }
    }
}

public sealed class PriorityOnePlayCheck:MonoBehaviour
{
    readonly List<string> checks=new List<string>();BrotherhoodGame game;bool failed;
    void Check(bool condition,string label){checks.Add((condition?"PASS ":"FAIL ")+label);Debug.Log("[PriorityOne] "+checks[checks.Count-1]);failed|=!condition;}
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.5f);
        try{Run();}catch(Exception e){Check(false,"Unexpected runtime exception: "+e);Debug.LogException(e);}
        Time.timeScale=1;
        File.WriteAllText("Documentation/priority-one-playtest-results.txt","Run UTC: "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",checks)+"\nRESULT: "+(failed?"FAILED":"PASSED"));
        File.Delete("Temp/brotherhood-priority-one-verify");EditorApplication.isPlaying=false;
    }
    void Step(float seconds)
    {
        for(float remaining=seconds;remaining>.00001f;){float dt=Mathf.Min(1f/120,remaining);game.player.Tick(dt);game.player.actor.Advance(dt);remaining-=dt;}
    }
    void Place(Vector2 at)
    {
        game.progress.guiltDrops.Clear();game.controls.ClearGameplayInput();game.player.Restore();Time.timeScale=0;game.player.motor.Teleport(at);Step(.6f);
    }
    static void Set(object instance,string name,object value){instance.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(instance,value);}
    void Run()
    {
        game=FindAnyObjectByType<BrotherhoodGame>();Check(game!=null,"Generated scene starts");if(game==null)return;
        game.enabled=false;game.controls.Simulation=true;Time.timeScale=0;
        var progress=game.progress;var items=InventoryCatalog.Load();var player=game.player;var catalog=player.actor.catalog;
        Check(progress.tears==0&&progress.rangedTier==0&&!progress.hasSpecial&&!progress.Owns("RANGED_1"),"New pilgrimage starts with zero Tears and no free ranged skill");
        Check(game.RosarySlots==2&&progress.meaCulpaLevel==0,"New pilgrimage has two rosary slots and source Mea Culpa level zero");
        progress.tears=20000;var first=items.Find("CHARGED_1");var second=items.Find("CHARGED_2");
        Check(!game.PurchaseInventorySkill(first)&&progress.tears==20000,"Enough Tears cannot bypass the Mea Culpa gate");
        progress.meaCulpaLevel=1;Check(game.PurchaseInventorySkill(first),"Mea Culpa level one allows its first source skill");
        Check(!game.PurchaseInventorySkill(second)&&progress.chargedTier==1,"Owned prerequisite cannot bypass Mea Culpa level three");
        progress.meaCulpaLevel=3;Check(game.PurchaseInventorySkill(second)&&progress.chargedTier==2&&progress.tears==11500,"Level three purchase deducts the source cost exactly once");
        var legacy=new PlayerProgress{tears=321,rangedTier=1,chargedTier=2,ownedItems=new[]{"CHARGED_1","CHARGED_2"},equippedRosaryBeads=new[]{"RB01","RB02","RB03"}};
        legacy.Normalize(5,items);
        Check(legacy.rangedTier==0&&legacy.chargedTier==2&&legacy.meaCulpaLevel==3&&legacy.tears==321&&legacy.rosarySlots==3,"v5 migration removes the unpurchased ranged grant and retains purchased skills, currency and equipped beads");
        var purchasedRange=new PlayerProgress{rangedTier=1,ownedItems=new[]{"RANGED_1"}};purchasedRange.Normalize(5,items);
        Check(purchasedRange.rangedTier==1&&purchasedRange.hasSpecial&&purchasedRange.Owns("RANGED_1"),"v5 migration keeps a purchased ranged skill");
        var beadIds=new[]{"RB01","RB02","RB03"};foreach(string id in beadIds)progress.SetOwned(id,true);
        Check(game.EquipInventoryItem(items.Find(beadIds[0]))&&game.EquipInventoryItem(items.Find(beadIds[1]))&&!game.EquipInventoryItem(items.Find(beadIds[2]))&&progress.equippedRosaryBeads.Length==2,"Two unlocked knots reject a third bead without replacing an equipped bead");
        progress.rosarySlots=3;Check(game.EquipInventoryItem(items.Find(beadIds[2])),"Persisted rosary progression makes its newly opened slot usable");
        progress.equippedRosaryBeads=Array.Empty<string>();progress.rosarySlots=2;progress.meaCulpaLevel=0;
        game.SaveGame();string save=File.ReadAllText(Path.Combine(Application.persistentDataPath,"brotherhood-verification.json"));
        Check(save.Contains("\"version\":6")&&save.Contains("\"rosarySlots\":2")&&save.Contains("\"CHARGED_2\""),"Save v6 persists source progression and acquired skills in the isolated save");

        var source=JsonUtility.FromJson<ImportData>(File.ReadAllText("Assets/Brotherhood/SourceData/brotherhood.json"));
        Check(source.animations.Sum(c=>c.frames.Count(f=>string.IsNullOrEmpty(f.sprite)))==124&&catalog.clips.Sum(c=>c.frames.Count(f=>f==null))>=124,"Generated catalog retains all 124 null sprite keys");
        Check(source.animations.Sum(c=>c.events.Length)==268&&source.animations.Sum(c=>c.floatTracks.Length)==16,"Animation events and all source float tracks survive extraction");
        var fixture=new GameObject("Animation timeline fixture");var actor=fixture.AddComponent<SpriteActor>();actor.catalog=catalog;actor.visual=fixture.AddComponent<SpriteRenderer>();
        bool nullWindows=true;int windows=0;
        foreach(var clip in source.animations.Where(c=>c.frames.Any(f=>string.IsNullOrEmpty(f.sprite))))
        {
            actor.visual.enabled=true;actor.Play(clip.name,false,true);float time=0;
            for(int i=0;i<clip.frames.Length;i++)
            {
                float sample=clip.frames[i].time+.00001f;actor.Advance(Mathf.Max(0,sample-time));time=sample;
                if(string.IsNullOrEmpty(clip.frames[i].sprite)){windows++;nullWindows&=actor.visual.sprite==null&&!actor.visual.enabled;}
                else nullWindows&=actor.visual.sprite!=null;
            }
        }
        Check(nullWindows&&windows==124,"Runtime clears and restores the renderer through every source null window");
        actor.visual.enabled=true;actor.visual.color=Color.white;actor.Play("penitent_crossing_opendoor_out_anim",false,true);actor.Advance(.9f);
        Check(Mathf.Abs(actor.visual.color.a-.5f)<.015f,"Door exit alpha follows the source float curve halfway through its fade");
        actor.Play("Player_Idle",true,true);Check(Mathf.Approximately(actor.visual.color.a,1)&&actor.visual.enabled,"Changing clips restores animation-owned alpha and visibility");
        var received=new List<RestoredAnimationEvent>();actor.AnimationEvent+=received.Add;
        actor.Play("NewFlagellant_attack",false,true);actor.Advance(.519f);Check(received.Count(e=>e.functionName=="AnimationEvent_FastAttack")==0,"Flagellant animation emits no hit before 0.52 seconds");
        actor.Advance(.002f);Check(received.Count(e=>e.functionName=="AnimationEvent_FastAttack")==1,"Flagellant first hit is emitted at the source 0.52-second event");
        actor.Advance(.578f);Check(received.Count(e=>e.functionName=="AnimationEvent_FastAttack")==1,"Flagellant second hit waits until the source 1.10-second event");
        actor.Advance(.002f);actor.Advance(2);actor.Advance(2);Check(received.Count(e=>e.functionName=="AnimationEvent_FastAttack")==2,"Nonlooping hit events fire exactly twice and never replay after completion");
        received.Clear();actor.Play("NewFlagellant_attack",true,true);actor.Advance(catalog.Find("NewFlagellant_attack").duration*2+.001f);
        Check(received.Count(e=>e.functionName=="AnimationEvent_FastAttack")==4,"A large time step dispatches source events once in each completed loop");
        Destroy(fixture);

        game.Enter("D17Z01S05",null,new Vector2(-840,9.025f));var room=game.Current;Physics2D.SyncTransforms();
        var floor=Array.Find(room.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_166");var box=floor==null?null:floor.GetComponent<BoxCollider2D>();
        Check(box!=null&&box.enabled&&!box.isTrigger&&box.gameObject.layer==9&&box.size==new Vector2(4,1)&&box.offset==new Vector2(2,-.5f)&&Mathf.Abs(box.bounds.max.y-9)<.001f,"S05 restores LOGIC_166 as the source one-way floor at height nine");
        Check(room.transform.Find("FloorBridge_StatueToGate")==null,"S05 contains no authored replacement bridge");
        var openGroup=Array.Find(room.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_105");var closedGroup=Array.Find(room.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_133");
        Check(openGroup!=null&&!openGroup.gameObject.activeSelf&&closedGroup!=null&&closedGroup.gameObject.activeSelf&&!room.OnLadder(new Vector2(-836.5f,5.3f),out _),"S05 preserves the source closed passage and blocks its inactive ladder sensor");
        Place(new Vector2(-839.5f,9.025f));float walkStart=player.transform.position.x;game.controls.Move=1;Step(1.2f);game.controls.Move=0;
        Check(player.transform.position.x>walkStart+5.8f&&player.transform.position.y>8.99f,"Player walks over the restored source floor at five units per second");
        // Exercise the alternate source geometry without granting its absent
        // quest/area unlock in a normal pilgrimage.
        closedGroup.gameObject.SetActive(false);openGroup.gameObject.SetActive(true);Physics2D.SyncTransforms();
        Check(room.ladders.Length>0&&room.OnLadder(new Vector2(-836.5f,5.3f),out var ladder)&&Mathf.Abs(ladder.center.x+836.5f)<.01f&&Mathf.Abs(ladder.center.y-5.75f)<.01f,"S05 open passage retains the original LadderTrigger with its parent rotation and usable sensor");
        Place(new Vector2(-839.5f,9.025f));game.controls.Move=1;Step(1.2f);game.controls.Move=0;
        Check(player.transform.position.x>-833.7f&&player.transform.position.y>8.99f,"Player also crosses the source one-way floor in the open passage state");
        Place(new Vector2(-836,9.025f));player.motor.dropThrough=.4f;player.motor.velocity=Vector2.down;for(int i=0;i<36;i++)player.motor.Step(1f/120);
        var underneath=Physics2D.Raycast(new Vector2(-836,11),Vector2.down,5,(1<<8)|(1<<9));
        Check(player.transform.position.y<8.7f,"Source floor supports dropping through instead of acting as a solid replacement block; feet="+player.transform.position+", first surface="+(underneath.collider==null?"none":underneath.collider.name+" layer "+underneath.collider.gameObject.layer));
        closedGroup.gameObject.SetActive(true);openGroup.gameObject.SetActive(false);Physics2D.SyncTransforms();
        Place(new Vector2(-868,9.025f));game.controls.Jump=game.controls.JumpHeld=true;player.Tick(.001f);game.controls.Jump=false;
        Check(Mathf.Abs(player.motor.velocity.y-(10-player.motor.gravity*.001f))<.02f,"Jump launches with the source ten-unit impulse");
        Place(new Vector2(-868,9.025f));game.controls.Parry=true;Step(.001f);game.controls.Parry=false;Step(.091f);Step(.28f);
        Check(player.Parrying,"Parry remains active inside its source chance window");Step(.04f);Check(!player.Parrying,"Parry chance closes after 0.30 seconds");

        foreach(string id in new[]{"PR01","PR04"})
        {
            Place(new Vector2(-868,9.025f));progress.SetOwned(id,true);player.fervour=13;game.EquipInventoryItem(items.Find(id));
            Check(player.fervour==13,id+" equip does not refill Fervour");player.fervour=60;game.controls.Prayer=true;Step(.001f);game.controls.Prayer=false;Step(.56f);
            Check(player.ActivePrayer==id&&Mathf.Abs(player.PrayerTimeRemaining-10)<.01f&&Mathf.Abs(player.fervour-(60-items.Find(id).fervourNeeded))<.01f,id+" cast applies its source cost and real ten-second duration");
            if(id=="PR01")
            {
                Step(.8f);game.controls.Attack=true;Step(.001f);game.controls.Attack=false;
                Check(Mathf.Abs(player.actor.Speed-1.5f)<.01f,"PR01 applies the source maximum attack animation speed of 1.5");Step(9f);
            }
            else Step(9.8f);
            Check(player.ActivePrayer==id&&player.PrayerTimeRemaining>0,id+" effect remains active before ten seconds");Step(.3f);
            Check(string.IsNullOrEmpty(player.ActivePrayer)&&player.PrayerTimeRemaining==0,id+" effect expires at its source duration");
        }
        Check(Mathf.Abs(new InventoryModifiers(progress).PrayerHitHealing("PR04",20)-3)<.001f,"PR04 restores the source fifteen percent of dealt damage");
        string[] added={"PR101","PR201","PR202","PR203"};float[] costs={100,40,40,90};
        for(int i=0;i<added.Length;i++)
        {
            var item=items.Find(added[i]);Check(item.fervourNeeded==costs[i]&&item.effects.Length>0&&!string.IsNullOrEmpty(item.effects[0].sourceSettings),added[i]+" retains its source cost, effect type and complete component settings");
            Place(new Vector2(-868,9.025f));progress.SetOwned(item.id,true);game.EquipInventoryItem(item);player.fervour=60;game.controls.Prayer=true;Step(.001f);game.controls.Prayer=false;Step(.56f);
            bool canCast=PlayerController.PrayerImplemented(item.id)&&player.MaxFervour>=item.fervourNeeded&&(item.id!="PR202"||game.HasPrayerCheckpoint);
            Check(canCast?Mathf.Abs(player.fervour-(60-item.fervourNeeded))<.001f:player.fervour==60,item.id+" uses its explicit adapter or refuses the cast without spending resources");
        }

        var flagellant=Array.Find(room.enemies,e=>e.family=="flagellant");Check(flagellant!=null,"Flagellant combat fixture exists");
        if(flagellant!=null)
        {
            Place(new Vector2(-863.2f,9.025f));flagellant.ResetEnemy();flagellant.motor.Teleport(new Vector2(-865,9.025f));Set(flagellant,"timer",0f);flagellant.Tick(.001f);
            float elapsed=0;Action<float> enemyStep=seconds=>{for(float remaining=seconds;remaining>.00001f;){float dt=Mathf.Min(.001f,remaining);flagellant.Tick(dt);flagellant.actor.Advance(dt);remaining-=dt;elapsed+=dt;}};
            float life=player.health;enemyStep(.519f);Check(player.health==life,"Flagellant deals no premature timer-based damage");
            enemyStep(.002f);Check(Mathf.Abs(player.health-(life-10))<.01f,"Flagellant event applies source Strength ten on its first hit");
            enemyStep(.578f);Set(player,"invincible",0f);enemyStep(.002f);Check(Mathf.Abs(player.health-(life-20))<.01f,"Flagellant second source event applies ten damage without a duplicate timer hit");
        }
        VerifyCombatFeedback();
        game.Enter("D01Z02S06",null,null);var altar=game.Current.skillAltars[0];var center=(Vector2)altar.sensor.TransformPoint(altar.offset);player.motor.Teleport(center-Vector2.up*.5f);progress.meaCulpaLevel=0;progress.activatedSkillAltars=Array.Empty<string>();
        Check(game.TryUseSkillAltar()&&progress.meaCulpaLevel==1,"First visit activates the source altar and grants one Mea Culpa level");
        Check(game.TryUseSkillAltar()&&progress.meaCulpaLevel==1,"Reusing the same altar cannot grant repeated Mea Culpa levels");
    }
    void VerifyCombatFeedback()
    {
        var player=game.player;var room=game.Current;var originalEnemies=room.enemies;var originalProgress=game.progress;
        var updateHud=typeof(TouchControls).GetMethod("LateUpdate",BindingFlags.Instance|BindingFlags.NonPublic);
        room.enemies=Array.Empty<EnemyController>();
        try
        {
            game.progress=new PlayerProgress{meaCulpaLevel=2,tears=2000};game.progress.UnlockCore();
            Place(new Vector2(-868,9.025f));updateHud.Invoke(game.controls,null);
            var special=GameObject.Find("SPECIAL");
            Check(special!=null&&special.GetComponent<CanvasGroup>().alpha==1,"The shared Penance/ranged control is visible at full opacity before buying a skill");
            if(special!=null)
            {
                game.effects.Clear();player.fervour=60;PressSpecial(special);
                Check(player.fervour==60&&game.effects.ActiveRangeProjectiles==0&&!game.progress.hasSpecial&&game.MessageText.Contains("2.000 Tears"),"Tapping the locked control explains its unlock gate without granting or casting a skill");
                VerifyFervourPenance(special);
                Check(game.PurchaseInventorySkill(InventoryCatalog.Load().Find("RANGED_1"))&&game.progress.tears==0,"Ranged skill can be earned through the source level-two, 2000-Tears purchase");
                updateHud.Invoke(game.controls,null);Check(special.GetComponent<CanvasGroup>().alpha==1,"Purchasing ranged preserves the mobile button appearance");
                PressSpecial(special);Step(.36f);
                Check(player.fervour==53&&game.effects.ActiveRangeProjectiles==1,"The restored touch button spends seven source Fervour and launches one sword projectile");
                game.effects.Clear();Place(new Vector2(-868,9.025f));player.fervour=5;game.progress.lungeTier=1;
                PressSpecial(special);Step(.36f);
                Check(player.fervour==5&&game.effects.ActiveRangeProjectiles==0&&!player.actor.Current.StartsWith("penitent_dodge_attack")&&game.MessageText.Contains("/7"),"Insufficient ranged Fervour gives feedback without silently substituting a lunge");
                Place(new Vector2(-868,9.025f));player.fervour=60;BeginSpecialHold(special);Step(.1f);
                Check(player.fervour==60&&game.effects.ActiveRangeProjectiles==0,"Pressing the learned shared control waits for tap/hold resolution before firing");
                var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=123};
                special.GetComponent<TouchControls.TouchButton>().OnPointerExit(pointer);game.controls.Simulation=false;game.controls.Sample();game.controls.Simulation=true;Step(.4f);
                Check(player.fervour==60&&game.effects.ActiveRangeProjectiles==0,"Sliding off the shared touch control cancels rather than firing a projectile");
            }
            game.progress=originalProgress;game.effects.Clear();
            string[] families={"acolyte","flagellant","fool","wheelcarrier","mudcrawler"};
            string[] hurtClips={"acolyte_get_hit_low","NewFlagellant_hurt","Fool_Hurt","WheelCarrier_stun","mudcrawler_hurt_anim"};
            for(int index=0;index<families.Length;index++)
            {
                room.enemies=Array.Empty<EnemyController>();Place(new Vector2(-868,9.025f));player.facing=1;
                var obj=new GameObject("Hurt regression "+families[index]);obj.transform.position=new Vector3(-866.8f,9.025f);
                var enemy=obj.AddComponent<EnemyController>();enemy.game=game;enemy.family=families[index];enemy.maxHealth=100;
                enemy.motor=obj.AddComponent<KinematicMotor>();enemy.actor=obj.AddComponent<SpriteActor>();enemy.actor.catalog=player.actor.catalog;
                enemy.actor.visual=obj.AddComponent<SpriteRenderer>();enemy.actor.visual.sharedMaterial=player.actor.visual.sharedMaterial;enemy.Initialize();
                room.enemies=new[]{enemy};Set(enemy,"timer",0f);
                try
                {
                    if(enemy.family=="mudcrawler")
                    {
                        player.motor.Teleport((Vector2)enemy.transform.position+Vector2.left*.1f);Set(player,"invincible",0f);float life=player.health;player.Tick(.001f);
                        Check(player.health==life&&!enemy.actor.visual.enabled,"Hidden MudCrawler cannot inflict invisible contact damage");
                        player.motor.Teleport(new Vector2(-868,9.025f));enemy.Tick(.001f);enemy.Tick(.71f);
                    }
                    else if(enemy.family=="wheelcarrier")for(int i=0;i<61;i++){enemy.Tick(.01f);enemy.actor.Advance(.01f);}
                    else enemy.Tick(.001f);
                    if(enemy.family=="acolyte"||enemy.family=="flagellant"||enemy.family=="wheelcarrier")
                        Check(enemy.actor.Current.EndsWith("attack"),enemy.family+" starts a real pending attack before the interruption test");
                    float lifeBefore=player.health;game.controls.Attack=true;player.Tick(.001f);game.controls.Attack=false;
                    for(int i=0;i<80&&!enemy.Hurt;i++){player.Tick(1f/120);player.actor.Advance(1f/120);}
                    Check(enemy.health<100&&enemy.Hurt&&enemy.actor.Current==hurtClips[index]&&!enemy.ExecutionReady,enemy.family+" ordinary player slash enters Hurt without granting an execution");
                    Step(.7f);player.motor.Teleport((Vector2)enemy.transform.position+Vector2.left*.1f);Set(player,"invincible",0f);
                    float duration=enemy.actor.catalog.Find(hurtClips[index]).duration;if(enemy.family=="flagellant")duration=Mathf.Max(duration,1.2f);
                    Vector2 enemyStart=enemy.transform.position;
                    StepCombat(enemy,duration-.03f);
                    Check(player.health==lifeBefore,enemy.family+" Hurt blocks both overlapping contact and the cancelled pending attack");
                    Check(enemy.Hurt&&enemy.actor.Current==hurtClips[index]&&Mathf.Abs(enemy.transform.position.x-enemyStart.x)<.01f,enemy.family+" AI preserves the full Hurt clip/recovery and stops horizontal movement");
                    enemy.Damage(1);StepCombat(enemy,.05f);
                    Check(enemy.Hurt&&player.health==lifeBefore,enemy.family+" another hit renews Hurt without leaking damage");
                    player.motor.Teleport((Vector2)enemy.transform.position+Vector2.left*1.8f);StepCombat(enemy,duration+.4f);
                    Check(!enemy.Hurt&&player.health==lifeBefore,enemy.family+" recovers and starts fresh AI without resuming a cancelled hit");
                    player.motor.Teleport((Vector2)enemy.transform.position+Vector2.left*.1f);Set(player,"invincible",0f);player.Tick(.001f);
                    Check(player.health<lifeBefore,enemy.family+" healthy contact becomes dangerous again after Hurt ends");
                    player.Restore();Time.timeScale=0;enemy.StunEnemy(.2f);player.motor.Teleport((Vector2)enemy.transform.position+Vector2.left*.1f);Set(player,"invincible",0f);float stunLife=player.health;player.Tick(.001f);
                    Check(player.health==stunLife,enemy.family+" stun suppresses contact even without an execution prompt");
                    enemy.Tick(.21f);Check(enemy.CanDamagePlayer,enemy.family+" expired stun returns to a working AI state");
                    if(enemy.family=="acolyte"||enemy.family=="flagellant")
                    {
                        enemy.OnParried(1);enemy.Damage(1);
                        Check(enemy.ExecutionReady&&!enemy.CanDamagePlayer,enemy.family+" damage during parry stun preserves the execution window and cannot hurt Player");
                    }
                    enemy.Damage(999);player.Restore();Time.timeScale=0;Set(player,"invincible",0f);player.motor.Teleport(enemy.transform.position);float deadLife=player.health;player.Tick(.001f);
                    Check(player.health==deadLife,enemy.family+" death cannot leave contact damage enabled");
                    enemy.ResetEnemy();enemy.gameObject.SetActive(false);player.Tick(.001f);
                    Check(player.health==deadLife,enemy.family+" inactive enemy cannot cause contact damage");
                }
                finally{room.enemies=Array.Empty<EnemyController>();Destroy(obj);}
            }
        }
        finally{game.progress=originalProgress;room.enemies=originalEnemies;game.effects.Clear();game.controls.ClearGameplayInput();player.Restore();Time.timeScale=0;}
    }
    void PressSpecial(GameObject button)
    {
        BeginSpecialHold(button);Step(.001f);EndSpecialHold(button);
    }
    void BeginSpecialHold(GameObject button)
    {
        game.controls.ClearGameplayInput();game.controls.Simulation=false;
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=123};
        button.GetComponent<TouchControls.TouchButton>().OnPointerDown(pointer);game.controls.Sample();game.controls.Simulation=true;
    }
    void EndSpecialHold(GameObject button)
    {
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=123};
        button.GetComponent<TouchControls.TouchButton>().OnPointerUp(pointer);game.controls.Simulation=false;game.controls.Sample();game.controls.Simulation=true;
        Step(.001f);game.controls.ClearGameplayInput();
    }
    void HoldSpecial(GameObject button){BeginSpecialHold(button);Step(.26f);EndSpecialHold(button);}
    void VerifyFervourPenance(GameObject button)
    {
        var player=game.player;var previous=game.progress;
        try
        {
            game.progress=new PlayerProgress{tears=200};Set(player,"modifiers",null);Place(new Vector2(-868,9.025f));player.fervour=0;
            float life=player.health;Vector2 position=player.transform.position;
            BeginSpecialHold(button);Step(.2f);
            Check(player.health==life&&player.fervour==0&&game.progress.tears==200,"Shared control does not sacrifice resources before the source 0.25-second hold");
            Step(.06f);
            Check(player.health==life-15&&player.fervour==25&&game.progress.tears==175,"New-game Penance trades exactly fifteen Life and twenty-five Tears for twenty-five Fervour");
            Check(player.PenanceCasting&&player.actor.Current=="penitent_fervour_penance_anim"&&!game.progress.hasSpecial&&game.progress.rangedTier==0,"Penance plays the source animation without requiring or granting RANGED_1");
            Check(game.audioBank.Has("FERVOR_SELF_DAMAGE"),"Penance uses its decoded original FervorSelfDamage audio sample");
            game.controls.Move=1;Step(.3f);
            Check(Vector2.Distance(position,player.transform.position)<.01f,"Penance blocks movement while its source animation is playing");
            Step(.9f);Check(player.PenanceCasting,"Penance retains its animation lock through the source 1.5-second clip");
            Step(.5f);
            Check(!player.PenanceCasting&&player.health==life-15&&player.fervour==25&&game.progress.tears==175,"Holding past animation completion cannot sacrifice again without a fresh press");
            EndSpecialHold(button);
            Check(game.effects.ActiveRangeProjectiles==0&&player.fervour==25&&!game.MessageText.Contains("2.000 Tears"),"Releasing a Penance hold cannot also fire a projectile or show the tap-only unlock hint");
            game.controls.Move=0;HoldSpecial(button);
            Check(player.health==life-30&&player.fervour==50&&game.progress.tears==150,"A fresh press allows another Penance after the first animation completes");

            Place(new Vector2(-868,9.025f));player.fervour=55;Set(player,"invincible",100f);HoldSpecial(button);
            Check(player.health==player.MaxHealth-15&&player.fervour==player.MaxFervour,"Self-sacrifice bypasses damage invulnerability and clamps restored Fervour to its maximum");
            Place(new Vector2(-868,9.025f));player.fervour=0;player.health=15;game.progress.tears=200;HoldSpecial(button);
            Check(player.health==15&&player.fervour==0&&game.progress.tears==200&&!player.PenanceCasting,"Penance refuses Life equal to its cost and cannot kill the player");
            Place(new Vector2(-868,9.025f));player.fervour=0;player.health=15.1f;HoldSpecial(button);
            Check(Mathf.Abs(player.health-.1f)<.001f&&player.fervour==25&&!player.Dead,"Life just above the source cost can pay for Penance safely");
            Place(new Vector2(-868,9.025f));player.fervour=0;game.progress.tears=25;HoldSpecial(button);
            Check(player.health==player.MaxHealth&&player.fervour==0&&game.progress.tears==25&&game.MessageText.Contains("25 Tears"),"Penance preserves the source strict Tears-greater-than-cost condition");
            Place(new Vector2(-868,9.025f));player.fervour=0;game.progress.tears=26;HoldSpecial(button);
            Check(game.progress.tears==1&&player.fervour==25,"One Tear above the starting Penance cost permits the sacrifice");
            game.progress.meaCulpaLevel=2;Place(new Vector2(-868,9.025f));player.fervour=0;game.progress.tears=75;HoldSpecial(button);
            Check(player.fervour==0&&game.progress.tears==75,"Mea Culpa level two raises the Penance cost to seventy-five Tears with the same strict gate");
            Place(new Vector2(-868,9.025f));player.fervour=0;game.progress.tears=76;HoldSpecial(button);
            Check(game.progress.tears==1&&player.fervour==25,"Penance consumes the source twenty-five plus twenty-five times Mea Culpa level cost");

            game.progress.tears=300;Place(new Vector2(-868,9.025f));player.fervour=0;player.motor.Teleport(new Vector2(-868,13.025f));player.motor.grounded=false;
            BeginSpecialHold(button);Step(.3f);
            Check(player.health==player.MaxHealth&&player.fervour==0&&game.progress.tears==300,"Penance cannot begin in the air");
            player.motor.Teleport(new Vector2(-868,9.025f));player.motor.grounded=true;Step(.4f);
            Check(player.fervour==0&&game.progress.tears==300,"An airborne hold cannot queue a sacrifice upon landing");EndSpecialHold(button);
            Place(new Vector2(-868,9.025f));player.fervour=0;game.controls.Attack=true;Step(.001f);game.controls.Attack=false;
            BeginSpecialHold(button);Step(.3f);
            Check(!player.PenanceCasting&&player.fervour==0&&game.progress.tears==300,"An attack in progress blocks Penance without consuming resources");Step(.6f);
            Check(!player.PenanceCasting&&player.fervour==0,"A blocked hold cannot turn into a delayed sacrifice after the attack ends");EndSpecialHold(button);
            Place(new Vector2(-868,9.025f));player.fervour=0;game.SetItemPopupActive(true);HoldSpecial(button);
            Check(player.fervour==0&&game.progress.tears==300,"Blocked gameplay input cannot initiate Penance");game.SetItemPopupActive(false);
            Place(new Vector2(-868,9.025f));player.fervour=0;HoldSpecial(button);Set(player,"invincible",0f);player.Damage(1,player.transform.position.x-1,false);
            Check(!player.PenanceCasting&&player.actor.Current=="penitent_pushback_grounded","Enemy damage interrupts Penance without leaving the input locked");
            Place(new Vector2(-868,9.025f));player.fervour=0;game.progress.rangedTier=1;game.progress.hasSpecial=true;game.progress.specialMode=1;game.progress.tears=300;
            HoldSpecial(button);Step(1.6f);
            Check(player.fervour==25&&game.progress.tears==225&&game.effects.ActiveRangeProjectiles==0,"Learning ranged preserves Penance on hold and suppresses ranged casting on its release");
            BeginSpecialHold(button);Step(.1f);game.controls.ClearGameplayInput();Step(.4f);EndSpecialHold(button);Step(.4f);
            Check(player.fervour==25&&game.progress.tears==225&&game.effects.ActiveRangeProjectiles==0,"Clearing gameplay input cancels a partial hold without sacrificing or firing");
            VerifyPenanceKeyboard();
            Place(new Vector2(-868,9.025f));player.fervour=0;game.progress.tears=300;HoldSpecial(button);player.Restore();game.controls.ClearGameplayInput();Step(.3f);
            Check(!player.PenanceCasting&&player.health==player.MaxHealth&&player.fervour==player.MaxFervour&&game.progress.tears==225,"Restore clears the Penance animation lock without repeating its resource exchange");
        }
        finally{game.SetItemPopupActive(false);game.progress=previous;Set(player,"modifiers",null);game.effects.Clear();Place(new Vector2(-868,9.025f));}
    }
    void VerifyPenanceKeyboard()
    {
        var keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>("Penance verification keyboard");
        Action<bool> key=pressed=>
        {
            UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,pressed
                ?new UnityEngine.InputSystem.LowLevel.KeyboardState(UnityEngine.InputSystem.Key.F)
                :new UnityEngine.InputSystem.LowLevel.KeyboardState());
            UnityEngine.InputSystem.InputSystem.Update();game.controls.Simulation=false;game.controls.Sample();game.controls.Simulation=true;
        };
        try
        {
            var player=game.player;Place(new Vector2(-868,9.025f));player.fervour=60;game.effects.Clear();
            key(true);Step(.1f);Check(player.fervour==60,"Keyboard F does not fire the shared skill on key down");
            key(false);Step(.001f);game.controls.ClearGameplayInput();Step(.36f);
            Check(player.fervour==53&&game.effects.ActiveRangeProjectiles==1,"A quick keyboard F press fires a learned ranged attack on release");
            game.effects.Clear();Place(new Vector2(-868,9.025f));player.fervour=0;game.progress.tears=300;
            key(true);Step(.26f);key(false);Step(.001f);game.controls.ClearGameplayInput();Step(1.6f);
            Check(player.fervour==25&&player.health==player.MaxHealth-15&&game.progress.tears==225&&game.effects.ActiveRangeProjectiles==0,"Holding keyboard F performs Penance once and suppresses ranged on release");
            Place(new Vector2(-868,9.025f));player.fervour=60;game.progress.tears=300;
            key(true);Step(.1f);game.controls.ClearGameplayInput();key(false);Step(.001f);game.controls.ClearGameplayInput();Step(.4f);
            Check(player.fervour==60&&game.progress.tears==300&&game.effects.ActiveRangeProjectiles==0,"Clearing input disarms keyboard F until a new press and prevents a release-only cast");
        }
        finally{UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);game.controls.ClearGameplayInput();}
    }
    void StepCombat(EnemyController enemy,float seconds)
    {
        for(float remaining=seconds;remaining>.00001f;)
        {
            float dt=Mathf.Min(1f/120,remaining);enemy.Tick(dt);enemy.actor.Advance(dt);game.player.Tick(dt);game.player.actor.Advance(dt);remaining-=dt;
        }
    }
}
