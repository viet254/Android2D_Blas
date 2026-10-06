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
using UnityEngine.EventSystems;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BrotherhoodTrialVerification
{
    public const string Flag="Temp/brotherhood-trial-verify";
    const string BackupKey="Brotherhood.Trial.VerificationBackup";
    static string SavePath=>Path.Combine(Application.persistentDataPath,"brotherhood-verification.json");
    static readonly string[] Suffixes={"",".bak",".tmp"};
    static BrotherhoodTrialVerification()
    {
        EditorApplication.playModeStateChanged+=state=>
        {
            if(state==PlayModeStateChange.EnteredPlayMode&&File.Exists(Flag)&&UnityEngine.Object.FindAnyObjectByType<BrotherhoodTrialPlayCheck>()==null)
                new GameObject("Brotherhood custom trial verification").AddComponent<BrotherhoodTrialPlayCheck>();
            if(state==PlayModeStateChange.EnteredEditMode&&SessionState.GetBool(BackupKey,false))RestoreVerificationSave();
        };
    }
    public static void RunInEditor()
    {
        if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play Mode before starting the trial verification.");
        if(SessionState.GetBool(BackupKey,false))RestoreVerificationSave();
        foreach(string suffix in Suffixes)
        {
            string path=SavePath+suffix;
            SessionState.SetString(BackupKey+suffix,File.Exists(path)?Convert.ToBase64String(File.ReadAllBytes(path)):"absent");
            File.Delete(path);
        }
        SessionState.SetBool(BackupKey,true);
        try
        {
            Directory.CreateDirectory("Temp");Directory.CreateDirectory("Documentation/Previews/Trial");
            EditorSettings.enterPlayModeOptionsEnabled=false;File.WriteAllText(Flag,"");
            // Open the existing saved scene. This runner never calls the Builder.
            EditorSceneManager.OpenScene(BrotherhoodBuilder.ScenePath);EditorApplication.isPlaying=true;
        }
        catch{RestoreVerificationSave();throw;}
    }
    public static void RestoreVerificationSave()
    {
        if(!SessionState.GetBool(BackupKey,false))return;
        foreach(string suffix in Suffixes)
        {
            string previous=SessionState.GetString(BackupKey+suffix,"absent"),path=SavePath+suffix;
            if(previous=="absent")File.Delete(path);else File.WriteAllBytes(path,Convert.FromBase64String(previous));
            SessionState.EraseString(BackupKey+suffix);
        }
        SessionState.EraseBool(BackupKey);File.Delete(Flag);
    }
}

public sealed class BrotherhoodTrialPlayCheck:MonoBehaviour
{
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    readonly List<string> checks=new List<string>();bool failed;BrotherhoodGame game;BrotherhoodTrialRoute route;
    string SavePath=>Path.Combine(Application.persistentDataPath,"brotherhood-verification.json");
    void Check(bool condition,string label){checks.Add((condition?"PASS ":"FAIL ")+label);failed|=!condition;Debug.Log("[Trial] "+checks[checks.Count-1]);}
    object Call(object target,string method,params object[] values)=>target.GetType().GetMethod(method,Private).Invoke(target,values);
    T Read<T>(object target,string field)=>(T)target.GetType().GetField(field,Private).GetValue(target);
    [Serializable] sealed class TrialSave
    {
        public int version;public string room,currentRoom;public float x,y,currentX,currentY,health,fervour;public int flasks;public bool boss;public PlayerProgress progress;
    }
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.5f);IEnumerator routine=null;
        try
        {
            bool ready=false;
            try
            {
                game=FindAnyObjectByType<BrotherhoodGame>();route=FindAnyObjectByType<BrotherhoodTrialRoute>();
                Check(game!=null&&route!=null,"Saved scene initializes the separate custom trial route without rebuilding");
                if(game!=null&&route!=null)
                {
                    Check((string)typeof(BrotherhoodGame).GetProperty("SavePath",Private).GetValue(game)==SavePath,"Trial verification reads and writes only its isolated save");
                    Check(FindAnyObjectByType<PriorityTwoPlayCheck>()==null&&FindAnyObjectByType<BrotherhoodPlayCheck>()==null,"Only the trial driver runs in this isolated Play Mode session");
                    ready=(string)typeof(BrotherhoodGame).GetProperty("SavePath",Private).GetValue(game)==SavePath;
                }
            }
            catch(Exception exception){Check(false,"Trial initialization exception: "+exception);Debug.LogException(exception);}
            if(ready)
            {
                foreach(var suite in new[]{RouteChecks(),LegacyS04ReturnChecks(),TouchUseDoorChecks(),TouchWallDoorsChecks(),WalkAllEdgeGatesChecks(),LockedArenaGateChecks(),LegacyBossSaveChecks(),SourceArrivalChecks(),TrialAiAndCombatChecks(),TrialBossCombatChecks()})
                {
                    routine=suite;
                    while(true)
                    {
                        bool next;
                        try{next=routine.MoveNext();}
                        catch(Exception exception){Check(false,"Trial verification exception: "+exception);Debug.LogException(exception);break;}
                        if(!next)break;yield return routine.Current;
                    }
                    (routine as IDisposable)?.Dispose();routine=null;
                }
            }
        }
        finally
        {
            (routine as IDisposable)?.Dispose();if(game!=null){game.enabled=false;game.controls.ClearGameplayInput();}
            Time.timeScale=1;
            File.WriteAllText("Documentation/trial-playtest-results.txt","Run UTC: "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",checks)+"\nRESULT: "+(failed?"FAILED":"PASSED"));
            EditorApplication.isPlaying=false;
        }
    }
    IEnumerator WalkTo(Vector2 destination,string label,float timeout=14,bool allowFlask=false)
    {
        string room=game.Current.id;Vector2 origin=game.player.transform.position;float deadline=Time.realtimeSinceStartup+timeout;
        float unchanged=0,nextFlask=0;Vector2 previous=origin;
        while(game.Current.id==room&&!game.player.Dead&&Mathf.Abs(game.player.transform.position.x-destination.x)>.18f&&Time.realtimeSinceStartup<deadline)
        {
            float direction=Mathf.Sign(destination.x-game.player.transform.position.x);game.controls.Move=direction;
            Vector2 at=game.player.transform.position;
            unchanged=Mathf.Abs(at.x-previous.x)<.003f?unchanged+Time.unscaledDeltaTime:0;previous=at;
            bool gap=Physics2D.Raycast(at+new Vector2(direction*.85f,1),Vector2.down,2.1f,(1<<8)|(1<<9)).collider==null;
            game.controls.Jump=game.player.motor.grounded&&(gap||unchanged>.4f);game.controls.JumpHeld=game.controls.Jump;
            if(allowFlask&&game.player.health<game.player.MaxHealth*.45f&&game.player.flasks>0&&Time.realtimeSinceStartup>=nextFlask)
            {game.controls.Flask=true;nextFlask=Time.realtimeSinceStartup+2;}
            yield return null;game.controls.Jump=game.controls.Flask=false;
        }
        game.controls.ClearGameplayInput();yield return new WaitForSecondsRealtime(.08f);
        float landingDeadline=Time.realtimeSinceStartup+3;
        while(!game.player.motor.grounded&&!game.player.Dead&&Time.realtimeSinceStartup<landingDeadline)yield return null;
        bool arrived=game.Current.id==room&&!game.player.Dead&&Mathf.Abs(game.player.transform.position.x-destination.x)<=.25f&&Mathf.Abs(game.player.transform.position.y-destination.y)<2.5f;
        Check(arrived,label+" is reached by real movement input on the room's terrain"+(arrived?"":"; actual="+game.player.transform.position+", target="+destination+", room="+game.Current.id+", dead="+game.player.Dead));
        Check(Mathf.Abs(origin.x-destination.x)<=.25f||Vector2.Distance(game.player.transform.position,origin)>.01f,label+" records physical player travel when the target is outside its starting tolerance");
    }
    IEnumerator Interact()
    {
        game.controls.ClearGameplayInput();float deadline=Time.realtimeSinceStartup+3;
        while((!game.player.motor.grounded||game.InputBlocked||Read<float>(game,"transitionCooldown")>0)&&!game.player.Dead&&Time.realtimeSinceStartup<deadline)yield return null;
        Debug.Log("[Trial input] "+PlayerDiagnostic()+", interact="+game.CanInteract+", routeInteract="+route.CanInteract);
        game.controls.Interact=true;yield return null;game.controls.Interact=false;yield return new WaitForSecondsRealtime(.1f);
    }
    IEnumerator Attack(float seconds=.5f)
    {
        game.controls.Attack=true;yield return null;game.controls.Attack=false;yield return new WaitForSecondsRealtime(seconds);
    }
    IEnumerator Traverse(string target)
    {
        string origin=game.Current.id;var passage=route.Passages.FirstOrDefault(item=>item.room==origin&&item.target==target);
        Check(passage!=null,origin+" has an explicit trial passage to "+target);if(passage==null)yield break;
        float lifeBeforeGate=game.player.health,minOriginY=float.PositiveInfinity;
        if(passage.walkDirection==0)
        {
            var walk=WalkTo(passage.position,origin+" → "+target+" wall doorway");while(walk.MoveNext())yield return walk.Current;
            if(game.Current.id!=origin||game.player.Dead)yield break;
            Check(game.player.motor.grounded&&Mathf.Abs(game.player.transform.position.y-passage.position.y)<.6f&&route.CanInteract,
                origin+" → "+target+" keeps the authored back-wall doorway available through USE; "+PlayerDiagnostic());
            yield return new WaitForSecondsRealtime(.35f);
            Check(game.Current.id==origin&&!game.InputBlocked&&!game.MessageText.Contains("End of restored route"),origin+" back-wall doorway waits for actual USE instead of auto-transitioning");
            var input=Interact();while(input.MoveNext())yield return input.Current;
        }
        else
        {
            Vector2 staging=new Vector2(passage.position.x-passage.walkDirection*1.5f,passage.position.y);
            var walk=WalkTo(staging,origin+" → "+target+" safe gate approach",20,true);while(walk.MoveNext())yield return walk.Current;
            if(game.Current.id!=origin||game.player.Dead)yield break;
            Check(game.player.motor.grounded&&!route.CanInteract&&Mathf.Abs(game.player.transform.position.y-passage.position.y)<.6f,
                origin+" → "+target+" side gate starts on safe floor without requiring the USE action; "+PlayerDiagnostic());
            lifeBeforeGate=game.player.health;float crossingDeadline=Time.realtimeSinceStartup+4;
            while(game.Current.id==origin&&!game.player.Dead&&Time.realtimeSinceStartup<crossingDeadline)
            {
                minOriginY=Mathf.Min(minOriginY,game.player.transform.position.y);
                game.controls.Move=passage.walkDirection;game.controls.Interact=false;game.controls.Jump=false;
                yield return null;
            }
            game.controls.ClearGameplayInput();
            Check(game.Current.id==target&&!game.player.Dead,
                origin+" → "+target+" changes room by held horizontal movement alone; "+PlayerDiagnostic());
            if(origin=="D17Z01S04"&&passage.sourceKey=="W")
                Check(game.Current.id==target&&game.player.health==lifeBeforeGate&&minOriginY>passage.position.y-1.2f,
                    "Walking left through S04 W crosses before the floor edge without fall damage or death; minOriginY="+minOriginY+", "+PlayerDiagnostic());
        }
        float deadline=Time.realtimeSinceStartup+3;
        while((game.Current.id!=target||game.InputBlocked)&&Time.realtimeSinceStartup<deadline)yield return null;
        Check(game.Current.id==target&&!game.InputBlocked&&!game.player.Dead,(passage.walkDirection==0?"USE":"Walking")+" crosses "+origin+" → "+target+" through the normal room fade");
        if(game.Current.id==target)
        {
            yield return new WaitForSecondsRealtime(.12f);
            Check(Vector2.Distance(game.player.transform.position,passage.arrival)<2&&game.player.motor.grounded,target+" passage arrival is clear of the return interaction and rests on real floor");
            yield return new WaitForSecondsRealtime(1.3f);
            Check(game.Current.id==target&&!game.player.Dead,target+" stays in the destination room after the entry cooldown without bouncing back");
        }
    }
    string PlayerDiagnostic()=>"feet="+game.player.transform.position+", health="+game.player.health+", flasks="+game.player.flasks+", grounded="+game.player.motor.grounded+", blocked="+game.InputBlocked+", lock="+Read<float>(game.player,"lockTime")+", altar="+Read<int>(game.player,"altarPhase")+", ladder="+Read<bool>(game.player,"laddering")+", cooldown="+Read<float>(game,"transitionCooldown");
    string SourceStoryState()=>game.IsBossDefeated+"|"+game.progress.wardenDefeated+"|"+game.progress.achievementAC01+"|"+game.progress.campaignWon+"|"+game.progress.RoomCompleted("D17Z01S11");
    IEnumerator ClimbLowerTrialLadder(bool up,string label)
    {
        var ladder=game.Current.ladders.FirstOrDefault(item=>item.sensor!=null&&item.sensor.name=="Trial return ladder"&&item.center.x<-860);
        Check(ladder!=null,label+" has a physical lower ladder connecting the authored floors");if(ladder==null)yield break;
        Vector2 landings=route.LadderLandings(ladder);float from=up?landings.x:landings.y,to=up?landings.y:landings.x;
        var walk=WalkTo(new Vector2(ladder.center.x,from),label+" ladder approach",20,true);while(walk.MoveNext())yield return walk.Current;
        Check(game.Current.id=="D17Z01S07"&&!game.player.Dead&&Mathf.Abs(game.player.transform.position.y-from)<.15f,label+" reaches its starting floor without teleporting; "+PlayerDiagnostic());
        if(game.Current.id!="D17Z01S07"||game.player.Dead)yield break;
        float start=Time.realtimeSinceStartup,deadline=start+(landings.y-landings.x)/3.2f*4+5;bool climbed=false;
        game.controls.ClearGameplayInput();game.controls.Vertical=up?1:-1;
        while(game.Current.id=="D17Z01S07"&&!game.player.Dead&&(Mathf.Abs(game.player.transform.position.y-to)>.08f||!game.player.motor.grounded||Read<bool>(game.player,"laddering"))&&Time.realtimeSinceStartup<deadline)
        {climbed|=game.player.actor.Current==(up?"penitent_ladder_going_up":"penitent_ladder_going_down");yield return null;}
        game.controls.ClearGameplayInput();yield return new WaitForSecondsRealtime(.12f);
        // The live Scout can hit immediately at the bottom. Wait for that real
        // Hurt knockback to settle before checking the standing floor position.
        float settlingDeadline=Time.realtimeSinceStartup+2.5f;
        while(game.Current.id=="D17Z01S07"&&!game.player.Dead&&(!game.player.motor.grounded||Read<bool>(game.player,"laddering")||Mathf.Abs(game.player.transform.position.y-to)>=.1f)&&Time.realtimeSinceStartup<settlingDeadline)yield return null;
        Check(climbed&&game.Current.id=="D17Z01S07"&&!game.player.Dead&&game.player.motor.grounded&&!Read<bool>(game.player,"laddering")&&Mathf.Abs(game.player.transform.position.y-to)<.1f&&game.player.motor.IsClearAt(game.player.transform.position,game.player.motor.size.y),
            label+" crosses the lower ladder with held "+(up?"Up":"Down")+" and lands safely; elapsed="+(Time.realtimeSinceStartup-start).ToString("0.0")+", targetY="+to+", "+PlayerDiagnostic());
    }
    void SaveRoundTrip(string flag,string label)
    {
        var save=File.Exists(SavePath)?JsonUtility.FromJson<TrialSave>(File.ReadAllText(SavePath)):null;
        Check(save?.progress!=null&&save.progress.HasFlag(flag),label+" writes its own gameplay objective flag into the actual save");
        if(save?.progress==null)return;
        string[] flags=game.progress.sourceFlags.ToArray(),cells=game.progress.discoveredMapCells.ToArray();
        int level=game.progress.meaCulpaLevel,slots=game.progress.rosarySlots;float tears=game.progress.tears;
        game.enabled=false;game.controls.ClearGameplayInput();game.progress=new PlayerProgress();Call(game,"LoadSave");
        Check(game.progress.HasFlag(flag)&&new HashSet<string>(game.progress.sourceFlags).SetEquals(flags)&&new HashSet<string>(game.progress.discoveredMapCells).SetEquals(cells)&&game.progress.meaCulpaLevel==level&&game.progress.rosarySlots==slots&&game.progress.tears==tears,
            label+" survives actual LoadSave with its resources, exploration and earlier objectives intact");
        string resumeRoom=Read<string>(game,"resumeRoom");Vector2 resumePosition=Read<Vector2>(game,"resumePosition");
        game.Enter(resumeRoom,null,resumePosition);game.player.Restore();game.player.health=Read<float>(game,"resumeHealth");game.player.fervour=Read<float>(game,"resumeFervour");game.player.flasks=Read<int>(game,"resumeFlasks");
        Check(game.Current.id==save.currentRoom&&Vector2.Distance(game.player.transform.position,new Vector2(save.currentX,save.currentY))<.1f,label+" restores the saved current room and arrival location");
        game.enabled=true;Time.timeScale=1;
    }
    IEnumerator FightArena(bool stopAfterOne=false)
    {
        float deadline=Time.realtimeSinceStartup+50,attackAfter=0,healAfter=0,dashAfter=0;
        while(route.ArenaEnemies.Any(enemy=>!enemy.Dead)&&(!stopAfterOne||route.ArenaEnemies.All(enemy=>!enemy.Dead))&&!game.player.Dead&&Time.realtimeSinceStartup<deadline)
        {
            var enemy=route.ArenaEnemies.Where(item=>!item.Dead).OrderBy(item=>Mathf.Abs(item.transform.position.x-game.player.transform.position.x)).First();
            float dx=enemy.transform.position.x-game.player.transform.position.x;game.controls.Move=Mathf.Abs(dx)>1.05f?Mathf.Sign(dx):0;
            if(game.player.health<game.player.MaxHealth*.4f&&game.player.flasks>0&&Time.realtimeSinceStartup>healAfter)
            {game.controls.Flask=true;healAfter=Time.realtimeSinceStartup+2;}
            else if(Mathf.Abs(dx)<1.75f&&Time.realtimeSinceStartup>attackAfter)
            {
                if(Mathf.Sign(dx)!=game.player.facing)game.controls.Move=Mathf.Sign(dx);
                game.controls.Attack=true;attackAfter=Time.realtimeSinceStartup+.22f;
            }
            if(Mathf.Abs(dx)<1.3f&&Time.realtimeSinceStartup>dashAfter&&game.player.health<game.player.MaxHealth*.65f)
            {game.controls.Move=Mathf.Sign(dx);game.controls.Dash=true;dashAfter=Time.realtimeSinceStartup+2.5f;}
            yield return null;game.controls.Attack=game.controls.Flask=game.controls.Dash=false;
        }
        game.controls.ClearGameplayInput();yield return new WaitForSecondsRealtime(.2f);
        if(!stopAfterOne)
        {
            Check(!game.controls.debugUI.GodMode&&!game.player.Dead&&route.ArenaEnemies.Length==2&&route.ArenaEnemies.All(enemy=>enemy.Dead),"Two custom arena NPCs are defeated by actual melee input with Godmode disabled; health="+game.player.health+", position="+game.player.transform.position);
            Check(game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag),"Only defeating both arena NPCs completes the arena objective");
        }
    }
    IEnumerator FightTrialBoss()
    {
        var boss=route.TrialBoss;float deadline=Time.realtimeSinceStartup+100,attackAfter=0,healAfter=0,dashAfter=0;
        var phases=new HashSet<BrotherhoodTrialLaviaAI.BrainState>();int attacks=0,heals=0;bool capturedAttack=false;
        float initialLife=boss.health,initialTears=game.progress.tears;
        float reward=boss.purgeReward*new InventoryModifiers(game.progress,game.player).TearsMultiplier*GuiltRules.Load().Tears(game.progress);
        while(!boss.Dead&&!game.player.Dead&&game.Current.id=="D17Z01S09"&&Time.realtimeSinceStartup<deadline)
        {
            var state=boss.laviaBoss.State;phases.Add(state);
            bool telegraph=state==BrotherhoodTrialLaviaAI.BrainState.WingWindup||state==BrotherhoodTrialLaviaAI.BrainState.DiveWindup||state==BrotherhoodTrialLaviaAI.BrainState.CastWindup;
            if(telegraph&&!capturedAttack){capturedAttack=true;yield return Capture("lavia-attack");}
            float dx=boss.transform.position.x-game.player.transform.position.x;
            game.controls.Move=Mathf.Abs(dx)>1.35f?Mathf.Sign(dx):0;
            if(game.player.health<game.player.MaxHealth*.45f&&game.player.flasks>0&&Time.realtimeSinceStartup>=healAfter)
            {game.controls.Flask=true;healAfter=Time.realtimeSinceStartup+2;heals++;}
            else if(telegraph&&Mathf.Abs(dx)<3.3f&&game.player.attackTime<=0&&game.player.motor.grounded&&Time.realtimeSinceStartup>=dashAfter)
            {game.controls.Move=-Mathf.Sign(dx==0?game.player.facing:dx);game.controls.Dash=true;dashAfter=Time.realtimeSinceStartup+1.8f;}
            else if(Mathf.Abs(dx)<2.05f&&Time.realtimeSinceStartup>=attackAfter)
            {
                if(Mathf.Sign(dx)!=game.player.facing)game.controls.Move=Mathf.Sign(dx);
                game.controls.Attack=true;attackAfter=Time.realtimeSinceStartup+.22f;attacks++;
            }
            yield return null;game.controls.Attack=game.controls.Flask=game.controls.Dash=game.controls.Jump=game.controls.JumpHeld=false;
        }
        game.controls.ClearGameplayInput();yield return new WaitForSecondsRealtime(.2f);
        Check(!game.controls.debugUI.GodMode&&!game.player.Dead&&boss.Dead&&initialLife>0&&attacks>0,
            "S09 Lavia is defeated through actual sword, dodge and Flask input with Godmode disabled; attacks="+attacks+", flasks="+heals+", bossLife="+boss.health+", phases="+string.Join(",",phases)+", "+PlayerDiagnostic());
        Check(capturedAttack&&phases.Any(state=>state==BrotherhoodTrialLaviaAI.BrainState.DiveWindup||state==BrotherhoodTrialLaviaAI.BrainState.WingWindup||state==BrotherhoodTrialLaviaAI.BrainState.CastWindup),
            "The real Lavia fight presents a visible attack telegraph before defeat");
        Check(boss.Dead&&route.BossDefeated&&game.progress.HasFlag(BrotherhoodTrialRoute.BossFlag),"Only the custom boss defeat completes the second objective and unlocks its altar door");
        Check(boss.Dead&&Mathf.Abs(game.progress.tears-initialTears-reward)<.001f,"The custom boss grants its ordinary source-scaled Purge once without an additional relay or mission payout");
    }
    IEnumerator RouteChecks()
    {
        game.controls.Simulation=true;game.controls.ClearGameplayInput();Time.timeScale=1;
        Check(!game.controls.debugUI.GodMode,"Custom trial walkthrough starts with Godmode disabled");
        Check(!game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&!game.progress.HasFlag(BrotherhoodTrialRoute.BossFlag)&&!game.progress.HasFlag(BrotherhoodTrialRoute.AltarFlag),"Fresh isolated pilgrimage has none of the three current custom objectives completed");
        string[] branch={BrotherhoodTrialRoute.EntranceRoom,"D17Z01S04","D17Z01S07","D17Z01S09","D17Z01S08"};
        Check(branch[0]=="D17Z01S03"&&route.Passages.Length==9&&branch.All(id=>game.Find(id)!=null),"The route starts at the selected source relic doorway with four bidirectional links and one existing-door escape for legacy S04 saves");
        for(int i=0;i<branch.Length-1;i++)
        {
            string from=branch[i],to=branch[i+1];
            Check(route.Passages.Any(item=>item.room==from&&item.target==to)&&route.Passages.Any(item=>item.room==to&&item.target==from),from+" ↔ "+to+" supports independent forward and return interactions");
        }
        Check(route.Passages.Count(item=>item.walkDirection==0)==3&&route.Passages.Count(item=>item.walkDirection!=0)==6,
            "Three back-wall doors use USE and six side gates open by walking into them");
        Check(route.Passages.All(item=>item.room!=item.target),"The route no longer adds artificial same-room portal shortcuts");
        Check(route.Passages.All(item=>item.sourceDoor!=null||item.artAnchor!=null),"Every trial interaction binds an existing authored RoomDoor or source doorway artwork");
        Check(route.Passages.Where(item=>item.room==branch[2]&&item.target==branch[3]).All(item=>item.requiredFlag==BrotherhoodTrialRoute.ArenaFlag)&&
            route.Passages.Where(item=>item.room==branch[3]&&item.target==branch[4]).All(item=>item.requiredFlag==BrotherhoodTrialRoute.BossFlag),"Forward doors require the arena and new boss objectives independently of the legacy relay flag");
        var sourceDoors=route.Passages.Where(item=>item.sourceDoor!=null).Select(item=>item.sourceDoor).Distinct().ToArray();
        var sourceDestinations=sourceDoors.ToDictionary(door=>door,door=>door.target+"|"+door.targetDoor);
        var sourceTriggers=sourceDoors.ToDictionary(door=>door,door=>door.trigger.position);
        var sourceSpawns=sourceDoors.ToDictionary(door=>door,door=>door.spawn.position);
        var oldShapes=game.rooms.SelectMany(room=>room.GetComponentsInChildren<Transform>(true)).Where(item=>item.name.StartsWith("Trial passage ")||item.name.StartsWith("Trial relay ")||item.name=="Trial pulse trap").ToArray();
        Check(oldShapes.Length==0,"No handmade portal rectangle, ordered relay actor or pulse trap remains in the saved scene's runtime route");
        // This is the single branch-entry fixture. The rest of the pilgrimage uses movement, ladders and interaction input.
        var entrance=route.Passages.First(passage=>passage.room==branch[0]&&passage.target==branch[1]);
        game.enabled=false;game.progress=new PlayerProgress();game.Enter(branch[0],null,entrance.position+Vector2.left*2);game.player.Restore();game.gameOverUI.Hide();game.enabled=true;
        string story=SourceStoryState();yield return new WaitForSecondsRealtime(1.3f);
        yield return Capture("authored-entrance");
        var firstPassage=Traverse(branch[1]);while(firstPassage.MoveNext())yield return firstPassage.Current;if(game.Current.id!=branch[1])yield break;
        var toArena=Traverse(branch[2]);while(toArena.MoveNext())yield return toArena.Current;if(game.Current.id!=branch[2])yield break;
        Check(!game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&route.ArenaEnemies.Length==2&&route.ArenaEnemies.All(enemy=>!enemy.Dead),"First arena arrival contains both live trial NPCs and keeps its right authored gate locked");
        var northwest=game.Current.Door("NW");var southwest=game.Current.Door("SW");
        Check(northwest!=null&&southwest!=null&&!route.AllowSourceDoor(game.Current,northwest)&&!route.AllowSourceDoor(game.Current,southwest),"The alternate authored S07 exits cannot bypass the incomplete arena or boss objectives");
        var down=ClimbLowerTrialLadder(false,"Approach to the arena");while(down.MoveNext())yield return down.Current;if(game.player.Dead)yield break;
        var firstFight=FightArena(true);while(firstFight.MoveNext())yield return firstFight.Current;
        Check(!game.player.Dead&&route.ArenaEnemies.Count(enemy=>enemy.Dead)==1&&!game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag),"One actual arena kill cannot complete the two-target objective");
        var fight=FightArena();while(fight.MoveNext())yield return fight.Current;if(game.player.Dead||route.ArenaEnemies.Any(enemy=>!enemy.Dead))yield break;
        yield return Capture("arena-cleared");
        SaveRoundTrip(BrotherhoodTrialRoute.ArenaFlag,"Arena objective");yield return new WaitForSecondsRealtime(1.3f);
        Check(route.ArenaEnemies.All(enemy=>enemy.Dead),"Reloading the cleared arena does not respawn its quest targets");
        var toBoss=Traverse(branch[3]);while(toBoss.MoveNext())yield return toBoss.Current;if(game.Current.id!=branch[3])yield break;
        var boss=route.TrialBoss;
        Check(boss!=null&&!boss.boss&&boss.family=="lavia"&&boss.trialBoss==null&&boss.laviaBoss!=null&&!boss.Dead&&!route.BossDefeated,"S09 begins with its living custom Lavia boss independently of the source Warden encounter");
        if(boss==null||boss.laviaBoss==null)yield break;
        var bossVisual=boss.GetComponentInChildren<BrotherhoodTrialLaviaVisual>(true);
        var bossRenderer=bossVisual!=null?bossVisual.GetComponent<SpriteRenderer>():null;
        Check(bossVisual!=null&&bossVisual.IsReady&&bossRenderer!=null&&bossRenderer.sprite!=null&&bossRenderer.bounds.size.y>=2f&&bossRenderer.bounds.size.y<=5.5f,
            "Lavia uses the supplied animated sprite atlas at readable boss scale in the S09 room; bounds="+(bossRenderer!=null?bossRenderer.bounds.size.ToString():"missing"));
        var bossDoor=route.Passages.First(item=>item.room==branch[3]&&item.target==branch[4]);
        Check(route.GateFx!=null&&route.GateFx.SourceTextureLoaded&&!route.GateFx.IsOpen&&route.GateFx.BlockingArtVisible&&route.GateFx.BurstCount==0,
            "Before the custom boss dies, the S09 rubble still visibly seals the authored left gate and its source crumble texture is ready");
        var lockWalk=WalkTo(new Vector2(bossDoor.walkAtX+1.5f,bossDoor.position.y),"Locked authored boss exit approach",20,true);while(lockWalk.MoveNext())yield return lockWalk.Current;
        float lockedUntil=Time.realtimeSinceStartup+.8f;game.controls.ClearGameplayInput();
        while(game.Current.id==branch[3]&&!game.player.Dead&&Time.realtimeSinceStartup<lockedUntil)
        {game.controls.Move=-1;game.controls.Interact=false;yield return null;}
        game.controls.ClearGameplayInput();
        Check(!route.CanInteract&&!game.player.Dead&&game.Current.id==branch[3]&&game.player.transform.position.x>=bossDoor.walkAtX-.25f&&game.player.transform.position.y>bossDoor.position.y-1f&&!boss.Dead&&!game.progress.HasFlag(BrotherhoodTrialRoute.BossFlag),
            "Walking into the locked S09 art-only gate cannot bypass the living custom boss or drop the player; "+PlayerDiagnostic());
        if(game.player.Dead)yield break;
        yield return Capture("boss-before-defeat");
        yield return Capture("lavia-before-defeat");
        var bossFight=FightTrialBoss();while(bossFight.MoveNext())yield return bossFight.Current;if(game.player.Dead||!boss.Dead)yield break;
        Check(route.GateFx!=null&&route.GateFx.IsOpen&&!route.GateFx.BlockingArtVisible&&route.GateFx.BurstCount==1&&route.GateFx.ActiveShardCount>0,
            "The first actual boss death blows away the S09 rubble with one visible source-textured crumble burst");
        Check(SourceStoryState()==story,"The custom boss victory does not change Warden defeat, AC01, campaign victory or S11 completion");
        yield return Capture("boss-defeated");
        yield return new WaitForSecondsRealtime(1.8f);
        Check(route.GateFx!=null&&route.GateFx.IsOpen&&!route.GateFx.BlockingArtVisible&&route.GateFx.ActiveShardCount==0,
            "After the source crumble dust settles, the permanent S09 doorway remains visibly clear");
        yield return Capture("boss-gate-open");
        SaveRoundTrip(BrotherhoodTrialRoute.BossFlag,"Boss objective");yield return new WaitForSecondsRealtime(1.3f);
        Check(route.TrialBoss.Dead&&route.BossDefeated&&route.TrialBoss.laviaBoss.DefeatNotified&&
            route.TrialBoss.laviaBoss.DeathShown&&!route.TrialBoss.gameObject.activeSelf,
            "Reloading the Lavia objective retains her defeat and hides the dead actor beside the open doorway");
        Check(route.GateFx!=null&&route.GateFx.IsOpen&&!route.GateFx.BlockingArtVisible&&route.GateFx.BurstCount==1,
            "Reloading the saved boss victory keeps the S09 rubble gone without replaying its crumble burst");
        var toAltar=Traverse(branch[4]);while(toAltar.MoveNext())yield return toAltar.Current;if(game.Current.id!=branch[4])yield break;
        Check(!game.progress.HasFlag(BrotherhoodTrialRoute.AltarFlag)&&game.Current.skillAltars.Length==1,"The altar room keeps its source altar and arrival alone cannot complete the final objective");
        if(game.Current.skillAltars.Length!=1)yield break;
        var altar=game.Current.skillAltars[0];var altarWalk=WalkTo(altar.sensor.position,"Source Mea Culpa altar",20,true);while(altarWalk.MoveNext())yield return altarWalk.Current;
        int level=game.progress.meaCulpaLevel,strength=game.progress.strengthUpgrades;float altarTears=game.progress.tears;
        var altarInput=Interact();while(altarInput.MoveNext())yield return altarInput.Current;yield return new WaitForSecondsRealtime(.2f);
        Check(game.progress.HasFlag(BrotherhoodTrialRoute.AltarFlag)&&game.progress.meaCulpaLevel==level+1&&game.progress.strengthUpgrades==strength+1,"Actual source-altar interaction completes the final objective and upgrades Mea Culpa exactly once");
        Check(Mathf.Abs(game.progress.tears-altarTears-250)<.001f,"The altar completion grants only its single authorized 250 Tears reward");
        if(!game.progress.HasFlag(BrotherhoodTrialRoute.AltarFlag))yield break;
        SaveRoundTrip(BrotherhoodTrialRoute.AltarFlag,"Altar objective");yield return new WaitForSecondsRealtime(1.3f);
        Check(route.CompletedObjectives==3&&game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&game.progress.HasFlag(BrotherhoodTrialRoute.BossFlag)&&game.progress.HasFlag(BrotherhoodTrialRoute.AltarFlag),"All three current gameplay objectives remain complete after restoring the final room");
        float completedTears=game.progress.tears;var repeatAltar=Interact();while(repeatAltar.MoveNext())yield return repeatAltar.Current;
        Check(game.progress.meaCulpaLevel==level+1&&game.progress.strengthUpgrades==strength+1&&game.progress.tears==completedTears,"A second actual altar interaction cannot duplicate its source upgrade or trial reward");
        yield return Capture("altar-complete");
        // Close the source altar's inventory through its existing dismissal flow before opening the map.
        float altarCloseDeadline=Time.realtimeSinceStartup+3;
        while(!game.controls.inventoryUI.IsOpen&&Read<int>(game.player,"altarPhase")==1&&Time.realtimeSinceStartup<altarCloseDeadline)yield return null;
        game.controls.inventoryUI.Hide();altarCloseDeadline=Time.realtimeSinceStartup+3;
        while(Read<int>(game.player,"altarPhase")!=0&&Time.realtimeSinceStartup<altarCloseDeadline)yield return null;
        Check(Read<int>(game.player,"altarPhase")==0&&!game.controls.inventoryUI.IsOpen,"The existing altar inventory closes and returns control through its normal stand-up animation");
        var map=game.controls.mapUI;string[] explored=game.progress.discoveredMapCells.ToArray();string actualSave=File.ReadAllText(SavePath);
        game.controls.OpenMapUI();yield return new WaitForSecondsRealtime(.1f);
        var content=Read<RectTransform>(map,"mapContent");var overlay=content.Find("TrialMapOverlay");
        Check(overlay!=null&&overlay.gameObject.activeInHierarchy,"The completed route appears on actually explored source cells in its separate map overlay");
        if(overlay!=null)
        {
            var markers=overlay.Find("TunnelMarkers");var links=overlay.Find("PassageLinks");var caption=overlay.Find("TrialProgress");
            Check(markers!=null&&links!=null&&caption!=null&&caption.GetComponentInChildren<Text>().text.Contains("3/3"),"The trial map caption counts arena, boss and altar completion");
            Check(markers!=null&&markers.Cast<Transform>().Where(item=>item.gameObject.activeInHierarchy).All(item=>SourceMap.Load().Discovered(game.progress,item.name.Substring("Tunnel_".Length))),"Trial doorway markers stay hidden for unexplored source cells");
            Check(links!=null&&links.Cast<Transform>().Where(item=>item.gameObject.activeInHierarchy).All(item=>item.name.Substring("Passage_".Length).Split('|').All(key=>SourceMap.Load().Discovered(game.progress,key))),"Trial route links appear only when both endpoints have actually been explored");
            var group=overlay.GetComponent<CanvasGroup>();Check(group!=null&&!group.blocksRaycasts&&!group.interactable,"The trial map overlay preserves existing drag and pin input");
        }
        yield return Capture("trial-map-complete");map.Hide();
        Check(game.progress.discoveredMapCells.SequenceEqual(explored)&&File.ReadAllText(SavePath)==actualSave,"Viewing the trial map leaves exploration and saved mission flags unchanged");
        foreach(string previous in new[]{branch[3],branch[2],branch[1],branch[0]})
        {
            if(game.Current.id==branch[2])
            {var up=ClimbLowerTrialLadder(true,"Return to the authored west door");while(up.MoveNext())yield return up.Current;}
            var traversal=Traverse(previous);while(traversal.MoveNext())yield return traversal.Current;if(game.Current.id!=previous)yield break;
        }
        Check(game.progress.meaCulpaLevel==level+1&&game.progress.strengthUpgrades==strength+1&&game.progress.tears==completedTears&&game.progress.sourceFlags.Count(flag=>flag==BrotherhoodTrialRoute.AltarFlag)==1,"The full return route preserves the source upgrade and grants no duplicate boss or altar rewards");
        Check(SourceStoryState()==story,"The complete custom pilgrimage leaves the source Warden story progression unchanged");
        Check(sourceDoors.All(door=>door.target+"|"+door.targetDoor==sourceDestinations[door]&&door.trigger.position==sourceTriggers[door]&&door.spawn.position==sourceSpawns[door]),"Using existing authored doorways preserves source RoomDoor destinations and trigger/spawn transforms");
    }
    IEnumerator LegacyS04ReturnChecks()
    {
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;Vector2 previousPosition=game.player.transform.position;
        float health=game.player.health,fervour=game.player.fervour;int flasks=game.player.flasks;bool enabled=game.enabled,warden=game.IsBossDefeated;
        string checkpointRoom=Read<string>(game,"checkpointRoom"),resumeRoom=Read<string>(game,"resumeRoom");Vector2 checkpoint=Read<Vector2>(game,"checkpoint"),resumePosition=Read<Vector2>(game,"resumePosition");bool resumeLoaded=Read<bool>(game,"resumeLoaded");
        var files=new[]{SavePath,SavePath+".bak",SavePath+".tmp"}.ToDictionary(path=>path,path=>File.Exists(path)?File.ReadAllBytes(path):null);
        var room=game.Find("D17Z01S04");var front=room.Door("FrontR");
        var passage=route.Passages.FirstOrDefault(item=>item.room==room.id&&item.sourceDoor==front&&item.target==BrotherhoodTrialRoute.EntranceRoom);
        Check(front!=null&&passage!=null,"The old right-hand S04 save position has an escape bound to its existing FrontR doorway");if(front==null||passage==null)yield break;
        string target=front.target,targetDoor=front.targetDoor;Vector3 trigger=front.trigger.position,spawn=front.spawn.position;
        try
        {
            game.enabled=false;game.controls.ClearGameplayInput();
            var legacy=new PlayerProgress{meaCulpaLevel=1,strengthUpgrades=1};legacy.SetFlag(BrotherhoodTrialRoute.ArenaFlag);legacy.SetFlag(BrotherhoodTrialRoute.RelayFlag);legacy.SetFlag(BrotherhoodTrialRoute.AltarFlag);legacy.SetFlag(BrotherhoodTrialRoute.VisitedFlag);
            var save=new TrialSave{version=6,room=BrotherhoodTrialRoute.EntranceRoom,x=-780,y=8.025f,currentRoom=room.id,currentX=-835,currentY=-4.975f,health=100,fervour=100,flasks=2,progress=legacy};
            File.WriteAllText(SavePath,JsonUtility.ToJson(save));File.Delete(SavePath+".bak");File.Delete(SavePath+".tmp");
            game.progress=new PlayerProgress();Call(game,"LoadSave");game.Enter(Read<string>(game,"resumeRoom"),null,Read<Vector2>(game,"resumePosition"));game.player.Restore();
            game.enabled=true;Time.timeScale=1;yield return new WaitForSecondsRealtime(1.3f);
            Check(game.Current.id==room.id&&!game.player.Dead&&Vector2.Distance(game.player.transform.position,new Vector2(-835,-4.975f))<.1f,"Actual LoadSave restores the legacy S04 position on the right of its sealed middle wall");
            var walk=WalkTo(passage.position,"Legacy S04 FrontR escape",14,true);while(walk.MoveNext())yield return walk.Current;
            bool offered=game.player.motor.grounded&&route.CanInteract&&Mathf.Abs(game.player.transform.position.y-passage.position.y)<.6f;
            game.controls.ClearGameplayInput();yield return new WaitForSecondsRealtime(.35f);
            Check(offered&&game.Current.id==room.id&&!game.InputBlocked&&!game.MessageText.Contains("End of restored route"),"The authored FrontR doorway waits for interaction instead of automatically routing to its missing source destination; "+PlayerDiagnostic());
            var input=Interact();while(input.MoveNext())yield return input.Current;float deadline=Time.realtimeSinceStartup+3;
            while((game.Current.id!=passage.target||game.InputBlocked)&&Time.realtimeSinceStartup<deadline)yield return null;
            yield return new WaitForSecondsRealtime(.12f);
            Check(game.Current.id==BrotherhoodTrialRoute.EntranceRoom&&!game.player.Dead&&!game.InputBlocked&&game.player.motor.grounded&&Vector2.Distance(game.player.transform.position,passage.arrival)<1&&game.player.motor.IsClearAt(game.player.transform.position,game.player.motor.size.y),"A real FrontR interaction escapes the old S04 save to a safe source S03 floor");
            Check(game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&game.progress.HasFlag(BrotherhoodTrialRoute.RelayFlag)&&game.progress.HasFlag(BrotherhoodTrialRoute.AltarFlag)&&!game.progress.HasFlag(BrotherhoodTrialRoute.BossFlag)&&game.progress.meaCulpaLevel==1&&game.progress.strengthUpgrades==1,"Escaping the legacy S04 save preserves its earned objectives and Mea Culpa level without granting the new boss defeat");
            var written=File.Exists(SavePath)?JsonUtility.FromJson<TrialSave>(File.ReadAllText(SavePath)):null;
            Check(written?.progress!=null&&written.currentRoom==passage.target&&written.progress.HasFlag(BrotherhoodTrialRoute.RelayFlag)&&!written.progress.HasFlag(BrotherhoodTrialRoute.BossFlag),"The real legacy escape saves the new current room and preserves its independent boss lock");
            Check(front.target==target&&front.targetDoor==targetDoor&&front.trigger.position==trigger&&front.spawn.position==spawn,"The legacy FrontR escape preserves the source RoomDoor destination, trigger and spawn transforms");
        }
        finally
        {
            game.enabled=false;game.controls.ClearGameplayInput();typeof(BrotherhoodGame).GetField("bossDead",Private).SetValue(game,warden);
            typeof(BrotherhoodGame).GetField("checkpointRoom",Private).SetValue(game,checkpointRoom);typeof(BrotherhoodGame).GetField("checkpoint",Private).SetValue(game,checkpoint);
            typeof(BrotherhoodGame).GetField("resumeRoom",Private).SetValue(game,resumeRoom);typeof(BrotherhoodGame).GetField("resumePosition",Private).SetValue(game,resumePosition);typeof(BrotherhoodGame).GetField("resumeLoaded",Private).SetValue(game,resumeLoaded);
            game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();game.player.health=health;game.player.fervour=fervour;game.player.flasks=flasks;
            foreach(var file in files){if(file.Value==null)File.Delete(file.Key);else File.WriteAllBytes(file.Key,file.Value);}
            game.controls.ClearGameplayInput();game.enabled=enabled;Time.timeScale=1;
        }
    }
    IEnumerator TouchUseDoorChecks()
    {
        var controls=game.controls;bool previousSimulation=controls.Simulation,enabled=game.enabled,warden=game.IsBossDefeated;
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;
        Vector2 previousPosition=game.player.transform.position;float health=game.player.health,fervour=game.player.fervour;int flasks=game.player.flasks;
        string checkpointRoom=Read<string>(game,"checkpointRoom"),resumeRoom=Read<string>(game,"resumeRoom");
        Vector2 checkpoint=Read<Vector2>(game,"checkpoint"),resumePosition=Read<Vector2>(game,"resumePosition");bool resumeLoaded=Read<bool>(game,"resumeLoaded");
        var files=new[]{SavePath,SavePath+".bak",SavePath+".tmp"}.ToDictionary(path=>path,path=>File.Exists(path)?File.ReadAllBytes(path):null);
        var passage=route.Passages.FirstOrDefault(item=>item.room=="D17Z01S04"&&item.sourceDoor!=null&&item.sourceDoor.key=="FrontR");
        Check(passage!=null,"The live touch-input fixture uses the existing S04 FrontR doorway");if(passage==null)yield break;
        var use=Read<GameObject>(controls,"useButton");var events=FindFirstObjectByType<EventSystem>();
        Check(use!=null&&events!=null,"The actual USE touch button and EventSystem exist in the saved scene");if(use==null||events==null)yield break;
        try
        {
            controls.Simulation=false;controls.ClearGameplayInput();game.enabled=false;
            game.Enter(passage.room,null,passage.position);game.player.Restore();game.enabled=true;Time.timeScale=1;
            float readyDeadline=Time.realtimeSinceStartup+.55f;
            while((!game.player.motor.grounded||!route.CanInteract||!use.activeInHierarchy)&&Time.realtimeSinceStartup<readyDeadline)yield return null;
            Check(game.Current.id==passage.room&&route.CanInteract&&use.activeInHierarchy&&!game.InputBlocked&&Read<float>(game,"transitionCooldown")>.2f,
                "The USE button is offered at the authored doorway while the room-entry cooldown is still active; "+PlayerDiagnostic());
            if(!route.CanInteract||!use.activeInHierarchy||Read<float>(game,"transitionCooldown")<=0)yield break;

            var useRect=use.GetComponent<RectTransform>();var corners=new Vector3[4];useRect.GetWorldCorners(corners);
            var pointer=new PointerEventData(events){pointerId=-78,position=RectTransformUtility.WorldToScreenPoint(null,(corners[0]+corners[2])*.5f),button=PointerEventData.InputButton.Left};
            var hits=new List<RaycastResult>();events.RaycastAll(pointer,hits);
            bool hitsUse=hits.Count>0&&(hits[0].gameObject==use||hits[0].gameObject.transform.IsChildOf(use.transform));
            Check(hitsUse,"The visible USE control is the foremost UI raycast target at its screen center"+(hits.Count>0?"; top="+hits[0].gameObject.name:"; no UI hit"));
            if(!hitsUse)yield break;

            pointer.pointerCurrentRaycast=hits[0];
            ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(hits[0].gameObject,pointer,ExecuteEvents.pointerDownHandler);
            Check(Read<bool>(controls,"qi")&&Read<bool>(controls,"ti"),"A real USE PointerDown queues one press and begins touch hold before gameplay samples input");
            yield return null;
            ExecuteEvents.ExecuteHierarchy<IPointerUpHandler>(hits[0].gameObject,pointer,ExecuteEvents.pointerUpHandler);
            Check(!Read<bool>(controls,"qi")&&!Read<bool>(controls,"ti")&&!controls.InteractHeld,
                "Gameplay consumes the one-shot USE press and PointerUp ends the hold without a second press");
            float transitionDeadline=Time.realtimeSinceStartup+3;
            while((game.Current.id!=passage.target||game.InputBlocked)&&Time.realtimeSinceStartup<transitionDeadline)yield return null;
            Check(game.Current.id==passage.target&&!game.InputBlocked&&!game.player.Dead,
                "One actual touch USE press during cooldown crosses the authored door when cooldown ends; "+PlayerDiagnostic());
            if(game.Current.id!=passage.target)yield break;

            // A second early press must not trigger a delayed room change once the
            // player has walked away from the same door before cooldown expires.
            game.enabled=false;controls.ClearGameplayInput();game.Enter(passage.room,null,passage.position+Vector2.right*.45f);game.player.Restore();game.enabled=true;
            readyDeadline=Time.realtimeSinceStartup+.55f;
            while((!game.player.motor.grounded||!route.CanInteract||!use.activeInHierarchy)&&Time.realtimeSinceStartup<readyDeadline)yield return null;
            Check(game.Current.id==passage.room&&route.CanInteract&&use.activeInHierarchy&&Read<float>(game,"transitionCooldown")>.2f,
                "The second touch starts inside the same authored doorway during a fresh cooldown; "+PlayerDiagnostic());
            if(!route.CanInteract||!use.activeInHierarchy||Read<float>(game,"transitionCooldown")<=0)yield break;
            corners=new Vector3[4];useRect.GetWorldCorners(corners);pointer.position=RectTransformUtility.WorldToScreenPoint(null,(corners[0]+corners[2])*.5f);
            hits.Clear();events.RaycastAll(pointer,hits);hitsUse=hits.Count>0&&(hits[0].gameObject==use||hits[0].gameObject.transform.IsChildOf(use.transform));
            Check(hitsUse,"The USE control remains a raycast target after returning to the authored door");if(!hitsUse)yield break;
            pointer.pointerCurrentRaycast=hits[0];ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(hits[0].gameObject,pointer,ExecuteEvents.pointerDownHandler);
            yield return null;ExecuteEvents.ExecuteHierarchy<IPointerUpHandler>(hits[0].gameObject,pointer,ExecuteEvents.pointerUpHandler);
            typeof(TouchControls).GetField("tm",Private).SetValue(controls,1f);
            float walkDeadline=Time.realtimeSinceStartup+.7f;
            while(game.Current.id==passage.room&&game.player.transform.position.x-passage.position.x<1.3f&&Time.realtimeSinceStartup<walkDeadline)yield return null;
            typeof(TouchControls).GetField("tm",Private).SetValue(controls,0f);
            Check(game.Current.id==passage.room&&game.player.transform.position.x-passage.position.x>1.2f&&Read<float>(game,"transitionCooldown")>0,
                "Touch movement leaves the door interaction radius before the buffered press can execute; "+PlayerDiagnostic());
            float cancelDeadline=Time.realtimeSinceStartup+1.5f;
            while(Read<float>(game,"transitionCooldown")>-.1f&&Time.realtimeSinceStartup<cancelDeadline)yield return null;
            yield return new WaitForSecondsRealtime(.15f);
            Check(game.Current.id==passage.room&&!game.InputBlocked&&game.player.transform.position.x-passage.position.x>1.2f,
                "Walking out of the doorway cancels its pending USE press instead of changing rooms later; "+PlayerDiagnostic());
        }
        finally
        {
            game.enabled=false;controls.ClearGameplayInput();controls.Simulation=previousSimulation;
            typeof(BrotherhoodGame).GetField("bossDead",Private).SetValue(game,warden);
            typeof(BrotherhoodGame).GetField("checkpointRoom",Private).SetValue(game,checkpointRoom);typeof(BrotherhoodGame).GetField("checkpoint",Private).SetValue(game,checkpoint);
            typeof(BrotherhoodGame).GetField("resumeRoom",Private).SetValue(game,resumeRoom);typeof(BrotherhoodGame).GetField("resumePosition",Private).SetValue(game,resumePosition);
            typeof(BrotherhoodGame).GetField("resumeLoaded",Private).SetValue(game,resumeLoaded);
            game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();game.player.health=health;game.player.fervour=fervour;game.player.flasks=flasks;
            foreach(var file in files){if(file.Value==null)File.Delete(file.Key);else File.WriteAllBytes(file.Key,file.Value);}
            controls.ClearGameplayInput();game.enabled=enabled;Time.timeScale=1;
        }
    }
    IEnumerator TouchWallDoorsChecks()
    {
        var controls=game.controls;bool previousSimulation=controls.Simulation,enabled=game.enabled,warden=game.IsBossDefeated;
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;
        Vector2 previousPosition=game.player.transform.position;float health=game.player.health,fervour=game.player.fervour;int flasks=game.player.flasks;
        string checkpointRoom=Read<string>(game,"checkpointRoom"),resumeRoom=Read<string>(game,"resumeRoom");
        Vector2 checkpoint=Read<Vector2>(game,"checkpoint"),resumePosition=Read<Vector2>(game,"resumePosition");bool resumeLoaded=Read<bool>(game,"resumeLoaded");
        var files=new[]{SavePath,SavePath+".bak",SavePath+".tmp"}.ToDictionary(path=>path,path=>File.Exists(path)?File.ReadAllBytes(path):null);
        var use=Read<GameObject>(controls,"useButton");var events=FindFirstObjectByType<EventSystem>();
        Check(route.Passages.Count(item=>item.walkDirection==0)==3&&game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&game.progress.HasFlag(BrotherhoodTrialRoute.BossFlag)&&game.progress.HasFlag(BrotherhoodTrialRoute.AltarFlag),
            "All three back-wall touch fixtures start with the arena, boss and altar objectives genuinely unlocked by the walkthrough");
        Check(use!=null&&events!=null,"Back-wall touch fixtures use the gameplay EventSystem and visible USE control");
        if(use==null||events==null)yield break;
        try
        {
            controls.Simulation=false;Time.timeScale=1;
            int pointerId=-90;
            foreach(var passage in route.Passages.Where(item=>item.walkDirection==0))
            {
                string label=passage.room+" → "+passage.target+" "+(passage.sourceDoor!=null?passage.sourceDoor.key:passage.artNode);
                game.enabled=false;controls.ClearGameplayInput();game.Enter(passage.room,null,passage.position);game.player.Restore();game.enabled=true;
                float readyDeadline=Time.realtimeSinceStartup+.55f;
                while((!game.player.motor.grounded||!route.CanInteract||!use.activeInHierarchy)&&Time.realtimeSinceStartup<readyDeadline)yield return null;
                bool ready=game.Current.id==passage.room&&game.player.motor.grounded&&route.CanInteract&&game.CanInteract&&use.activeInHierarchy&&!game.InputBlocked&&Read<float>(game,"transitionCooldown")>0;
                Check(ready,label+" offers its real USE control while standing at the authored doorway; "+PlayerDiagnostic());
                if(!ready)continue;

                var corners=new Vector3[4];use.GetComponent<RectTransform>().GetWorldCorners(corners);
                var pointer=new PointerEventData(events){pointerId=pointerId--,position=RectTransformUtility.WorldToScreenPoint(null,(corners[0]+corners[2])*.5f),button=PointerEventData.InputButton.Left};
                var hits=new List<RaycastResult>();events.RaycastAll(pointer,hits);
                bool useHit=hits.Count>0&&(hits[0].gameObject==use||hits[0].gameObject.transform.IsChildOf(use.transform));
                Check(useHit,label+" USE is the top UI raycast target"+(hits.Count>0?"; top="+hits[0].gameObject.name:"; no hit"));
                if(!useHit)continue;

                pointer.pointerCurrentRaycast=hits[0];ExecuteEvents.ExecuteHierarchy<IPointerDownHandler>(hits[0].gameObject,pointer,ExecuteEvents.pointerDownHandler);
                Check(Read<bool>(controls,"qi")&&Read<bool>(controls,"ti"),label+" PointerDown queues the one-shot touch action");
                yield return null;
                ExecuteEvents.ExecuteHierarchy<IPointerUpHandler>(hits[0].gameObject,pointer,ExecuteEvents.pointerUpHandler);
                Check(!Read<bool>(controls,"qi")&&!Read<bool>(controls,"ti")&&!controls.InteractHeld,label+" USE is sampled by gameplay and PointerUp releases the hold");
                float deadline=Time.realtimeSinceStartup+3;
                while((game.Current.id!=passage.target||game.InputBlocked)&&Time.realtimeSinceStartup<deadline)yield return null;
                Check(game.Current.id==passage.target&&!game.InputBlocked&&!game.player.Dead,
                    label+" changes room from a single real UI tap, including the source fade and cooldown; "+PlayerDiagnostic());
            }
        }
        finally
        {
            game.enabled=false;controls.ClearGameplayInput();controls.Simulation=previousSimulation;
            typeof(BrotherhoodGame).GetField("bossDead",Private).SetValue(game,warden);
            typeof(BrotherhoodGame).GetField("checkpointRoom",Private).SetValue(game,checkpointRoom);typeof(BrotherhoodGame).GetField("checkpoint",Private).SetValue(game,checkpoint);
            typeof(BrotherhoodGame).GetField("resumeRoom",Private).SetValue(game,resumeRoom);typeof(BrotherhoodGame).GetField("resumePosition",Private).SetValue(game,resumePosition);
            typeof(BrotherhoodGame).GetField("resumeLoaded",Private).SetValue(game,resumeLoaded);
            game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();game.player.health=health;game.player.fervour=fervour;game.player.flasks=flasks;
            foreach(var file in files){if(file.Value==null)File.Delete(file.Key);else File.WriteAllBytes(file.Key,file.Value);}
            controls.ClearGameplayInput();game.enabled=enabled;Time.timeScale=1;
        }
    }
    IEnumerator WalkAllEdgeGatesChecks()
    {
        var controls=game.controls;bool previousSimulation=controls.Simulation,enabled=game.enabled,warden=game.IsBossDefeated;
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;
        Vector2 previousPosition=game.player.transform.position;float health=game.player.health,fervour=game.player.fervour;int flasks=game.player.flasks;
        string checkpointRoom=Read<string>(game,"checkpointRoom"),resumeRoom=Read<string>(game,"resumeRoom");
        Vector2 checkpoint=Read<Vector2>(game,"checkpoint"),resumePosition=Read<Vector2>(game,"resumePosition");bool resumeLoaded=Read<bool>(game,"resumeLoaded");
        var files=new[]{SavePath,SavePath+".bak",SavePath+".tmp"}.ToDictionary(path=>path,path=>File.Exists(path)?File.ReadAllBytes(path):null);
        var touchAxis=typeof(TouchControls).GetField("tm",Private);
        Check(route.Passages.Count(item=>item.walkDirection!=0)==6&&game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&game.progress.HasFlag(BrotherhoodTrialRoute.BossFlag),
            "Six side-gate movement fixtures start after real arena and boss victories");
        Check(touchAxis!=null,"Side-gate fixtures drive the same sampled horizontal axis as the touch joystick");if(touchAxis==null)yield break;
        try
        {
            controls.Simulation=false;Time.timeScale=1;
            foreach(var passage in route.Passages.Where(item=>item.walkDirection!=0))
            {
                string label=passage.room+" → "+passage.target+" "+(passage.sourceDoor!=null?passage.sourceDoor.key:passage.artNode);
                Vector2 staging=new Vector2(passage.position.x-passage.walkDirection*1.6f,passage.position.y);
                game.enabled=false;controls.ClearGameplayInput();game.Enter(passage.room,null,staging);game.player.Restore();game.enabled=true;
                float readyDeadline=Time.realtimeSinceStartup+.65f;
                while(!game.player.motor.grounded&&!game.player.Dead&&Time.realtimeSinceStartup<readyDeadline)yield return null;
                Check(game.Current.id==passage.room&&game.player.motor.grounded&&!route.CanInteract&&!game.InputBlocked&&Mathf.Abs(game.player.transform.position.x-passage.walkAtX)>1,
                    label+" begins on safe floor away from the gate, without an available USE action; "+PlayerDiagnostic());
                float lifeBefore=game.player.health,minOriginY=float.PositiveInfinity;
                touchAxis.SetValue(controls,(float)passage.walkDirection);
                float crossingDeadline=Time.realtimeSinceStartup+3.5f;
                while(game.Current.id==passage.room&&!game.player.Dead&&Time.realtimeSinceStartup<crossingDeadline)
                {minOriginY=Mathf.Min(minOriginY,game.player.transform.position.y);yield return null;}
                Check(game.Current.id==passage.target&&!game.player.Dead&&!Read<bool>(controls,"qi"),
                    label+" crosses by held joystick movement with no USE press; minOriginY="+minOriginY+", "+PlayerDiagnostic());
                if(passage.room=="D17Z01S04"&&passage.sourceKey=="W")
                    Check(game.Current.id==passage.target&&game.player.health==lifeBefore&&minOriginY>passage.position.y-1.2f,
                        "S04 W opens before the left floor edge with no fall damage; minOriginY="+minOriginY+", "+PlayerDiagnostic());
                float fadeDeadline=Time.realtimeSinceStartup+3;
                while(game.Current.id==passage.target&&game.InputBlocked&&Time.realtimeSinceStartup<fadeDeadline)yield return null;
                Check(game.Current.id==passage.target&&!game.InputBlocked&&!game.player.Dead&&Vector2.Distance(game.player.transform.position,passage.arrival)<2,
                    label+" reaches its intended arrival after the room fade without manual input or teleporting mid-crossing");
                if(game.Current.id!=passage.target||game.player.Dead)break;

                if(passage.room=="D17Z01S04"&&passage.sourceKey=="W")
                {
                    // The real touch axis survives ClearGameplayInput(true) during
                    // the fade. Holding Left must stop at the return gate in S07.
                    yield return new WaitForSecondsRealtime(1.25f);
                    var reverse=route.Passages.First(item=>item.room==passage.target&&item.target==passage.room&&item.walkDirection==passage.walkDirection);
                    Check(game.Current.id==passage.target&&!game.player.Dead&&game.player.transform.position.x>=reverse.walkAtX-.25f,
                        "Keeping the same joystick direction after arrival cannot bounce through the reverse W gate; "+PlayerDiagnostic());
                    touchAxis.SetValue(controls,0f);yield return null;
                    Check(game.Current.id==passage.target,"Releasing the joystick leaves the Penitent in the arrived room before another crossing");
                    touchAxis.SetValue(controls,(float)passage.walkDirection);
                    float returnDeadline=Time.realtimeSinceStartup+3;
                    while(game.Current.id==passage.target&&!game.player.Dead&&Time.realtimeSinceStartup<returnDeadline)yield return null;
                    touchAxis.SetValue(controls,0f);returnDeadline=Time.realtimeSinceStartup+3;
                    while(game.Current.id==passage.room&&game.InputBlocked&&Time.realtimeSinceStartup<returnDeadline)yield return null;
                    Check(game.Current.id==passage.room&&!game.InputBlocked&&!game.player.Dead&&Vector2.Distance(game.player.transform.position,reverse.arrival)<2,
                        "Releasing and pressing the same joystick direction again intentionally returns through the W gate; "+PlayerDiagnostic());
                }
                else touchAxis.SetValue(controls,0f);
                controls.ClearGameplayInput();yield return new WaitForSecondsRealtime(1.25f);
                if(passage.room!="D17Z01S04"||passage.sourceKey!="W")
                    Check(game.Current.id==passage.target&&!game.player.Dead,label+" does not bounce back after the destination cooldown while the joystick is released");
            }
        }
        finally
        {
            touchAxis.SetValue(controls,0f);game.enabled=false;controls.ClearGameplayInput();controls.Simulation=previousSimulation;
            typeof(BrotherhoodGame).GetField("bossDead",Private).SetValue(game,warden);
            typeof(BrotherhoodGame).GetField("checkpointRoom",Private).SetValue(game,checkpointRoom);typeof(BrotherhoodGame).GetField("checkpoint",Private).SetValue(game,checkpoint);
            typeof(BrotherhoodGame).GetField("resumeRoom",Private).SetValue(game,resumeRoom);typeof(BrotherhoodGame).GetField("resumePosition",Private).SetValue(game,resumePosition);
            typeof(BrotherhoodGame).GetField("resumeLoaded",Private).SetValue(game,resumeLoaded);
            game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();game.player.health=health;game.player.fervour=fervour;game.player.flasks=flasks;
            foreach(var file in files){if(file.Value==null)File.Delete(file.Key);else File.WriteAllBytes(file.Key,file.Value);}
            controls.ClearGameplayInput();game.enabled=enabled;Time.timeScale=1;
        }
    }
    IEnumerator LockedArenaGateChecks()
    {
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;Vector2 previousPosition=game.player.transform.position;
        float health=game.player.health,fervour=game.player.fervour;int flasks=game.player.flasks;bool enabled=game.enabled,warden=game.IsBossDefeated,simulation=game.controls.Simulation;
        string checkpointRoom=Read<string>(game,"checkpointRoom"),resumeRoom=Read<string>(game,"resumeRoom");
        Vector2 checkpoint=Read<Vector2>(game,"checkpoint"),resumePosition=Read<Vector2>(game,"resumePosition");bool resumeLoaded=Read<bool>(game,"resumeLoaded");
        var files=new[]{SavePath,SavePath+".bak",SavePath+".tmp"}.ToDictionary(path=>path,path=>File.Exists(path)?File.ReadAllBytes(path):null);
        var gate=route.Passages.First(item=>item.room=="D17Z01S07"&&item.target=="D17Z01S09");
        var arenaEnemies=route.ArenaEnemies;
        var enemyHealth=arenaEnemies.Select(enemy=>enemy.health).ToArray();
        var enemyActive=arenaEnemies.Select(enemy=>enemy.gameObject.activeSelf).ToArray();
        var enemyPosition=arenaEnemies.Select(enemy=>(Vector2)enemy.transform.position).ToArray();
        var enemyReward=arenaEnemies.Select(enemy=>Read<bool>(enemy,"rewardGranted")).ToArray();
        try
        {
            game.enabled=false;game.controls.Simulation=true;game.controls.ClearGameplayInput();
            game.progress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(previousProgress));
            game.progress.sourceFlags=game.progress.sourceFlags.Where(flag=>flag!=BrotherhoodTrialRoute.ArenaFlag&&flag!=BrotherhoodTrialRoute.BossFlag&&flag!=BrotherhoodTrialRoute.AltarFlag).ToArray();
            game.Enter(gate.room,null,new Vector2(gate.walkAtX-1.5f,gate.position.y));game.player.Restore();
            foreach(var enemy in arenaEnemies)enemy.ResetEnemy();
            Check(arenaEnemies.Length==2&&arenaEnemies.All(enemy=>!enemy.Dead&&enemy.gameObject.activeInHierarchy)&&!game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag),
                "The locked-gate fixture respawns both living arena NPCs before gameplay can evaluate its completion flag");
            game.enabled=true;Time.timeScale=1;
            float readyDeadline=Time.realtimeSinceStartup+.5f;
            while(!game.player.motor.grounded&&!game.player.Dead&&Time.realtimeSinceStartup<readyDeadline)yield return null;
            float minY=float.PositiveInfinity,deadline=Time.realtimeSinceStartup+.9f;
            while(game.Current.id==gate.room&&!game.player.Dead&&Time.realtimeSinceStartup<deadline)
            {minY=Mathf.Min(minY,game.player.transform.position.y);game.controls.Move=1;game.controls.Interact=false;yield return null;}
            game.controls.ClearGameplayInput();
            Check(game.Current.id==gate.room&&!game.player.Dead&&!game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&game.player.transform.position.x<=gate.walkAtX+.25f&&minY>gate.position.y-1f,
                "Walking into locked S07 SE cannot cross the arena gate or fall off the right floor; minY="+minY+", "+PlayerDiagnostic());

            // Cross the threshold during a real dash, then reverse the joystick
            // before the next input sample while committed motor velocity still
            // points into the locked gate.
            game.enabled=false;game.controls.ClearGameplayInput();game.player.Restore();
            game.player.motor.Teleport(new Vector2(gate.walkAtX-.55f,gate.position.y));game.player.facing=1;Physics2D.SyncTransforms();
            game.enabled=true;readyDeadline=Time.realtimeSinceStartup+.7f;
            while(!game.player.motor.grounded&&!game.player.Dead&&Time.realtimeSinceStartup<readyDeadline)yield return null;
            game.controls.Move=1;game.controls.Dash=true;yield return null;game.controls.Dash=false;
            float dashDeadline=Time.realtimeSinceStartup+.35f;
            while(game.Current.id==gate.room&&!game.player.Dead&&game.player.transform.position.x<gate.walkAtX&&Time.realtimeSinceStartup<dashDeadline)yield return null;
            bool committedDash=game.Current.id==gate.room&&game.player.dashTime>0&&game.player.motor.velocity.x>8f&&game.player.transform.position.x>=gate.walkAtX;
            Check(committedDash,"Locked S07 SE dash fixture reaches the gate with positive high-speed motor velocity before reversing the joystick; "+PlayerDiagnostic());
            game.controls.Move=-1;yield return null;
            Check(committedDash&&ReferenceEquals(Read<object>(route,"blockedWalkPassage"),gate)&&game.Current.id==gate.room&&!game.player.Dead&&!game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&game.player.transform.position.x<=gate.walkAtX+.25f&&game.player.transform.position.y>gate.position.y-1f,
                "The locked S07 SE gate stops an active dash despite opposite joystick input, keeping the player alive on its safe floor; "+PlayerDiagnostic());
            game.controls.ClearGameplayInput();yield return new WaitForSecondsRealtime(.4f);
            Check(game.Current.id==gate.room&&!game.player.Dead&&!game.progress.HasFlag(BrotherhoodTrialRoute.ArenaFlag)&&game.player.transform.position.y>gate.position.y-1f,
                "The reversed dash cannot cross the locked arena gate or fall after its remaining momentum expires; "+PlayerDiagnostic());
        }
        finally
        {
            game.enabled=false;game.controls.ClearGameplayInput();game.controls.Simulation=simulation;typeof(BrotherhoodGame).GetField("bossDead",Private).SetValue(game,warden);
            typeof(BrotherhoodGame).GetField("checkpointRoom",Private).SetValue(game,checkpointRoom);typeof(BrotherhoodGame).GetField("checkpoint",Private).SetValue(game,checkpoint);
            typeof(BrotherhoodGame).GetField("resumeRoom",Private).SetValue(game,resumeRoom);typeof(BrotherhoodGame).GetField("resumePosition",Private).SetValue(game,resumePosition);
            typeof(BrotherhoodGame).GetField("resumeLoaded",Private).SetValue(game,resumeLoaded);
            game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();game.player.health=health;game.player.fervour=fervour;game.player.flasks=flasks;
            for(int i=0;i<arenaEnemies.Length;i++)
            {
                arenaEnemies[i].motor.Teleport(enemyPosition[i]);arenaEnemies[i].health=enemyHealth[i];arenaEnemies[i].gameObject.SetActive(enemyActive[i]);
                typeof(EnemyController).GetField("rewardGranted",Private).SetValue(arenaEnemies[i],enemyReward[i]);
            }
            foreach(var file in files){if(file.Value==null)File.Delete(file.Key);else File.WriteAllBytes(file.Key,file.Value);}
            game.controls.ClearGameplayInput();game.enabled=enabled;Time.timeScale=1;
        }
    }
    IEnumerator LegacyBossSaveChecks()
    {
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;Vector2 previousPosition=game.player.transform.position;
        float health=game.player.health,fervour=game.player.fervour;int flasks=game.player.flasks;bool enabled=game.enabled,warden=game.IsBossDefeated;
        var files=new[]{SavePath,SavePath+".bak",SavePath+".tmp"}.ToDictionary(path=>path,path=>File.Exists(path)?File.ReadAllBytes(path):null);
        try
        {
            game.enabled=false;game.controls.ClearGameplayInput();
            var exit=route.Passages.First(item=>item.room=="D17Z01S09"&&item.target=="D17Z01S08");
            var legacy=new PlayerProgress();legacy.SetFlag(BrotherhoodTrialRoute.ArenaFlag);legacy.SetFlag(BrotherhoodTrialRoute.RelayFlag);legacy.SetFlag(BrotherhoodTrialRoute.AltarFlag);legacy.SetFlag(BrotherhoodTrialRoute.VisitedFlag);
            var save=new TrialSave{version=6,room=BrotherhoodTrialRoute.EntranceRoom,x=-780,y=8.025f,currentRoom=exit.room,currentX=exit.position.x+.1f,currentY=exit.position.y,health=100,fervour=100,flasks=2,progress=legacy};
            File.WriteAllText(SavePath,JsonUtility.ToJson(save));File.Delete(SavePath+".bak");File.Delete(SavePath+".tmp");
            game.progress=new PlayerProgress();Call(game,"LoadSave");
            game.Enter(Read<string>(game,"resumeRoom"),null,Read<Vector2>(game,"resumePosition"));game.player.Restore();
            Check(game.progress.HasFlag(BrotherhoodTrialRoute.RelayFlag)&&!game.progress.HasFlag(BrotherhoodTrialRoute.BossFlag)&&!route.BossDefeated&&route.TrialBoss!=null&&!route.TrialBoss.Dead&&route.TrialBoss.gameObject.activeInHierarchy,
                "Loading an actual old relay-and-altar save preserves its earned flags but does not defeat or hide the new S09 boss");
            Check(route.GateFx!=null&&!route.GateFx.IsOpen&&route.GateFx.BlockingArtVisible,
                "Loading an old save without the custom boss flag restores the visible S09 rubble gate");
            Check(route.CompletedObjectives==2,"The retired relay flag does not count as the new boss objective in current trial progress");
            var arenaRoom=game.Find("D17Z01S07");
            Check(route.AllowSourceDoor(arenaRoom,arenaRoom.Door("NW"))&&!route.AllowSourceDoor(arenaRoom,arenaRoom.Door("SW")),"Legacy arena completion permits its original northwestern exit while the original altar exit still requires the new boss defeat");
            game.enabled=true;Time.timeScale=1;yield return new WaitForSecondsRealtime(1.3f);
            float lockedUntil=Time.realtimeSinceStartup+.7f;game.controls.ClearGameplayInput();
            while(game.Current.id==exit.room&&!game.player.Dead&&Time.realtimeSinceStartup<lockedUntil)
            {game.controls.Move=-1;game.controls.Interact=false;yield return null;}
            game.controls.ClearGameplayInput();
            Check(!route.CanInteract&&!game.player.Dead&&game.Current.id==exit.room&&game.player.transform.position.x>=exit.walkAtX-.25f&&game.player.transform.position.y>exit.position.y-1f&&!route.TrialBoss.Dead&&!route.BossDefeated,
                "Walking into the art-only gate remains safe and locked after loading an old relay-completion save; "+PlayerDiagnostic());
            Check(!game.progress.wardenDefeated&&!game.progress.achievementAC01&&!game.progress.campaignWon&&!game.IsBossDefeated,"Legacy trial migration does not award the original Warden story victory");
            // A source Warden victory is a separate save scenario, never a custom boss victory.
            game.enabled=false;save.boss=true;legacy.wardenDefeated=legacy.achievementAC01=legacy.campaignWon=true;legacy.CompleteRoom("D17Z01S11");
            File.WriteAllText(SavePath,JsonUtility.ToJson(save));File.Delete(SavePath+".bak");File.Delete(SavePath+".tmp");
            game.progress=new PlayerProgress();Call(game,"LoadSave");game.Enter(exit.room,null,exit.position+Vector2.right*.1f);game.player.Restore();
            Check(game.IsBossDefeated&&game.progress.wardenDefeated&&game.progress.achievementAC01&&game.progress.campaignWon&&!route.BossDefeated&&route.TrialBoss!=null&&!route.TrialBoss.Dead,
                "A saved source Warden victory leaves the independent custom boss alive and its new objective incomplete");
            Check(route.GateFx!=null&&!route.GateFx.IsOpen&&route.GateFx.BlockingArtVisible,
                "The unrelated source Warden victory does not clear the custom S09 rubble gate");
        }
        finally
        {
            game.enabled=false;game.controls.ClearGameplayInput();typeof(BrotherhoodGame).GetField("bossDead",Private).SetValue(game,warden);
            game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();game.player.health=health;game.player.fervour=fervour;game.player.flasks=flasks;
            foreach(var file in files){if(file.Value==null)File.Delete(file.Key);else File.WriteAllBytes(file.Key,file.Value);}
            game.controls.ClearGameplayInput();game.enabled=enabled;Time.timeScale=1;
        }
    }
    void PrepareTrialBoss(EnemyController boss,float gap)
    {
        boss.ResetEnemy();game.player.Restore();game.controls.ClearGameplayInput();Time.timeScale=0;
        game.player.motor.Teleport(boss.laviaBoss.Home+Vector2.right*gap);Physics2D.SyncTransforms();
    }
    enum StrikeFixture { Normal, Upward, Charged, Lunge, Riposte }
    float SwordDamageAt(EnemyController target,Vector2 playerFeet,float direction,StrikeFixture strike=StrikeFixture.Normal)
    {
        // Invoke the real attack receiver at a controlled active frame, with
        // each strike independent of the previous swing's hit list.
        game.player.Restore();game.player.motor.Teleport(playerFeet);game.player.facing=direction;
        game.player.actor.Face(direction);game.player.attackTime=.3f;
        typeof(PlayerController).GetField("attackVertical",Private).SetValue(game.player,strike==StrikeFixture.Upward?1f:0f);
        typeof(PlayerController).GetField("combo",Private).SetValue(game.player,0);
        typeof(PlayerController).GetField("hitCount",Private).SetValue(game.player,0);
        Physics2D.SyncTransforms();float before=target.health;
        switch(strike)
        {
            case StrikeFixture.Charged:Call(game.player,"HitCharged");break;
            case StrikeFixture.Lunge:Call(game.player,"HitLunge");break;
            case StrikeFixture.Riposte:Call(game.player,"HitRiposte",2.5f,52f);break;
            default:Call(game.player,"HitEnemies");break;
        }
        return before-target.health;
    }
    IEnumerator TrialBossCombatChecks()
    {
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;Vector2 previousPosition=game.player.transform.position;
        float health=game.player.health,fervour=game.player.fervour;int flasks=game.player.flasks;bool enabled=game.enabled,warden=game.IsBossDefeated;float previousTime=Time.timeScale;
        var files=new[]{SavePath,SavePath+".bak",SavePath+".tmp"}.ToDictionary(path=>path,path=>File.Exists(path)?File.ReadAllBytes(path):null);
        var cheat=game.controls.debugUI;
        try
        {
            game.enabled=false;game.controls.ClearGameplayInput();game.progress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(previousProgress));
            game.progress.sourceFlags=game.progress.sourceFlags.Where(flag=>flag!=BrotherhoodTrialRoute.BossFlag).ToArray();game.Enter("D17Z01S09",null,new Vector2(-876,-12.975f));game.player.Restore();Time.timeScale=0;
            var boss=route.TrialBoss;Check(boss!=null&&boss.laviaBoss!=null&&boss.trialBoss==null&&!boss.boss,"Boss combat fixtures use the actual Lavia actor with a separate AI and ordinary source damage pipeline");if(boss==null||boss.laviaBoss==null)yield break;
            string story=SourceStoryState();
            PrepareTrialBoss(boss,6);AiStep(.02f,boss);Call(boss.laviaBoss,"LateUpdate");
            var bossHud=Read<Canvas>(boss.laviaBoss,"hud");
            var hudSafe=Read<RectTransform>(boss.laviaBoss,"safeArea");
            var hudPanel=hudSafe==null?null:hudSafe.Find("Boss health") as RectTransform;
            var frameImage=hudPanel==null?null:hudPanel.Find("Source boss frame")?.GetComponent<Image>();
            var lifeFill=Read<Image>(boss.laviaBoss,"lifeFill");
            var lifeLoss=Read<Image>(boss.laviaBoss,"lifeLoss");
            var lifeTitle=Read<Text>(boss.laviaBoss,"title");
            Check(bossHud!=null&&bossHud.enabled&&frameImage!=null&&frameImage.sprite!=null,
                "The active Lavia encounter shows its health HUD with the imported source boss frame sprite");
            bool safeLayout=Screen.width>0&&Screen.height>0&&hudSafe!=null&&hudPanel!=null&&
                Vector2.Distance(hudSafe.anchorMin,new Vector2(Screen.safeArea.xMin/Screen.width,Screen.safeArea.yMin/Screen.height))<.01f&&
                Vector2.Distance(hudSafe.anchorMax,new Vector2(Screen.safeArea.xMax/Screen.width,Screen.safeArea.yMax/Screen.height))<.01f&&
                Vector2.Distance(hudPanel.anchorMin,new Vector2(.5f,0))<.01f&&
                Vector2.Distance(hudPanel.anchorMax,new Vector2(.5f,0))<.01f&&
                Mathf.Abs(hudPanel.anchoredPosition.x)<.01f&&hudPanel.anchoredPosition.y>=hudPanel.rect.height*.5f;
            Check(safeLayout,"Lavia's boss bar is centered along the lower safe-area edge without clipping below it");
            Check(lifeFill!=null&&lifeTitle!=null&&Mathf.Abs(lifeFill.rectTransform.anchorMax.x-1)<.001f&&
                lifeTitle.text.Contains(Mathf.CeilToInt(boss.maxHealth)+" / "+Mathf.CeilToInt(boss.maxHealth)),
                "The fresh boss bar starts full and labels Lavia's maximum Life");
            boss.health=boss.maxHealth*.44f;Call(boss.laviaBoss,"LateUpdate");
            float fraction=boss.health/boss.maxHealth;
            Check(lifeFill!=null&&lifeLoss!=null&&lifeTitle!=null&&boss.laviaBoss.Enraged&&
                Mathf.Abs(lifeFill.rectTransform.anchorMax.x-fraction)<.001f&&
                lifeLoss.rectTransform.anchorMax.x>=lifeFill.rectTransform.anchorMax.x&&
                lifeTitle.text.Contains("CUỒNG NỘ")&&
                lifeTitle.text.Contains(Mathf.CeilToInt(boss.health)+" / "+Mathf.CeilToInt(boss.maxHealth)),
                "Damage shortens Lavia's Life fill while the delayed loss and enraged HP label follow her real health");
            foreach(float side in new[]{-1f,1f})
            {
                // Lavia's feet sit 1.05 units above the arena floor at rest.
                // The broad wing sprite must not enlarge the damageable torso.
                PrepareTrialBoss(boss,4);
                Vector2 floor=boss.laviaBoss.Home;
                float damage=SwordDamageAt(boss,floor+Vector2.right*(side*2.15f),-side);
                Check(damage>0,"A normal sword slash reaches Lavia's torso from the "+(side<0?"left":"right")+" at a 2.15-unit gap");
                PrepareTrialBoss(boss,4);
                damage=SwordDamageAt(boss,floor+Vector2.right*(side*2.55f),-side);
                Check(Mathf.Abs(damage)<.001f,"A normal sword slash misses Lavia beyond its visible blade reach on the "+(side<0?"left":"right")+" at a 2.55-unit gap");
            }
            PrepareTrialBoss(boss,4);
            float behind=SwordDamageAt(boss,boss.laviaBoss.Home+Vector2.right*1.15f,1);
            Check(Mathf.Abs(behind)<.001f,"A normal sword slash cannot damage Lavia behind the player's facing direction");
            PrepareTrialBoss(boss,4);
            float below=SwordDamageAt(boss,boss.laviaBoss.Home+new Vector2(1.9f,3.05f),-1);
            Check(Mathf.Abs(below)<.001f,"A horizontal slash cannot reach Lavia when her torso is below the blade's vertical span");
            int originalChargedTier=game.progress.chargedTier;
            game.progress.chargedTier=0; // Keep the separate charged projectile out of fixed hitbox checks.
            foreach(var strike in new[]{StrikeFixture.Upward,StrikeFixture.Charged,StrikeFixture.Lunge,StrikeFixture.Riposte})
            {
                float near=strike==StrikeFixture.Upward?1.8f:strike==StrikeFixture.Charged?3.8f:strike==StrikeFixture.Lunge?2.6f:2.8f;
                float far=strike==StrikeFixture.Upward?2.1f:strike==StrikeFixture.Charged?4.15f:strike==StrikeFixture.Lunge?2.9f:3.1f;
                PrepareTrialBoss(boss,4);
                float damage=SwordDamageAt(boss,boss.laviaBoss.Home+Vector2.right*near,-1,strike);
                Check(damage>0,"Lavia's torso receives the real "+strike+" attack at a "+near.ToString("0.00")+"-unit gap");
                PrepareTrialBoss(boss,4);
                damage=SwordDamageAt(boss,boss.laviaBoss.Home+Vector2.right*far,-1,strike);
                Check(Mathf.Abs(damage)<.001f,"The real "+strike+" attack misses Lavia beyond its torso edge at a "+far.ToString("0.00")+"-unit gap");
            }
            game.progress.chargedTier=originalChargedTier;
            PrepareTrialBoss(boss,6);bool sawTelegraph=false;float elapsed=0;
            while(elapsed<3&&boss.laviaBoss.State!=BrotherhoodTrialLaviaAI.BrainState.Dive)
            {AiStep(.01f,boss);elapsed+=.01f;sawTelegraph|=boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.DiveWindup;}
            Check(sawTelegraph&&boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.Dive&&boss.laviaBoss.DiveAttacksStarted>0,
                "At long range Lavia visibly winds up before beginning a dive");
            float direction=boss.laviaBoss.DiveDirection;Vector2 diveStart=boss.transform.position;
            game.player.motor.Teleport(boss.laviaBoss.Home+Vector2.left*3);AiStep(.12f,boss);
            Check(boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.Dive&&Mathf.Sign(boss.transform.position.x-diveStart.x)==Mathf.Sign(direction)&&boss.transform.position.y<diveStart.y-.04f&&boss.laviaBoss.DiveDirection==direction,
                "A committed dive descends and keeps its captured direction when the player crosses behind Lavia");
            float before=boss.health;int interrupted=boss.laviaBoss.Interruptions;boss.Damage(18);
            game.player.motor.Teleport((Vector2)boss.transform.position+Vector2.right*.3f);typeof(PlayerController).GetField("invincible",Private).SetValue(game.player,0f);
            float playerLife=game.player.health;AiStep(.2f,boss);game.player.attackTime=0;game.player.Tick(.02f);
            Check(boss.health==before-18&&boss.Hurt&&!boss.CanDamagePlayer&&boss.laviaBoss.DiveRemaining<=0&&boss.laviaBoss.Interruptions>interrupted&&game.player.health==playerLife,
                "A normal hit cancels Lavia's dive and Hurt cannot deal dive or contact damage");
            float hurtTime=Read<float>(boss,"timer");before=boss.health;
            boss.Damage(18);
            Check(boss.health==before-18&&Mathf.Abs(Read<float>(boss,"timer")-hurtTime)<.001f,
                "Further sword hits deal damage without restarting Lavia's stagger and locking out all attacks");
            PrepareTrialBoss(boss,2);elapsed=0;sawTelegraph=false;
            while(elapsed<3&&boss.laviaBoss.State!=BrotherhoodTrialLaviaAI.BrainState.WingStrike)
            {
                AiStep(.01f,boss);elapsed+=.01f;
                if(!sawTelegraph&&boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.WingWindup)
                {
                    sawTelegraph=true;typeof(PlayerController).GetField("invincible",Private).SetValue(game.player,0f);
                    float windupLife=game.player.health;game.player.Tick(.02f);
                    Check(!boss.CanDamagePlayer&&game.player.health==windupLife,
                        "Standing beside Lavia during the wing telegraph causes no contact or premature strike damage");
                }
            }
            Check(sawTelegraph&&boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.WingStrike&&boss.laviaBoss.WingAttacksStarted==1&&!boss.CanDamagePlayer,
                "At sword range Lavia telegraphs a wing slash; body contact outside the slash does not damage the player");
            typeof(PlayerController).GetField("invincible",Private).SetValue(game.player,0f);playerLife=game.player.health;
            AiStep(.3f,boss);float afterWing=game.player.health;AiStep(.12f,boss);
            Check(boss.laviaBoss.WingHits==1&&afterWing<playerLife&&game.player.health==afterWing,
                "The active wing-strike frames hit once without repeated damage during the same slash");
            PrepareTrialBoss(boss,2);elapsed=0;
            while(elapsed<3&&boss.laviaBoss.State!=BrotherhoodTrialLaviaAI.BrainState.WingWindup){AiStep(.01f,boss);elapsed+=.01f;}
            bool wingPending=boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.WingWindup;boss.StunEnemy(.6f);
            game.player.motor.Teleport((Vector2)boss.transform.position+Vector2.right*.3f);typeof(PlayerController).GetField("invincible",Private).SetValue(game.player,0f);playerLife=game.player.health;
            AiStep(.2f,boss);game.player.Tick(.02f);
            Check(wingPending&&!boss.CanDamagePlayer&&boss.laviaBoss.WingAttacksStarted==0&&game.player.health==playerLife,
                "Source stun cancels Lavia's pending wing slash and suppresses contact while stunned");
            PrepareTrialBoss(boss,2);boss.health=boss.maxHealth*.45f;elapsed=0;
            while(elapsed<3&&boss.laviaBoss.State!=BrotherhoodTrialLaviaAI.BrainState.WingWindup){AiStep(.01f,boss);elapsed+=.01f;}
            bool enragedWing=boss.laviaBoss.Enraged&&boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.WingWindup;boss.OnParried(game.player.facing);
            Check(enragedWing&&boss.Hurt&&!boss.CanDamagePlayer&&boss.laviaBoss.WingAttacksStarted==0,
                "Lavia's low-Life phase stays parryable and cancels a telegraphed wing slash");
            PrepareTrialBoss(boss,6);typeof(BrotherhoodTrialLaviaAI).GetField("attackSequence",Private).SetValue(boss.laviaBoss,2);elapsed=0;sawTelegraph=false;
            while(elapsed<3&&boss.laviaBoss.State!=BrotherhoodTrialLaviaAI.BrainState.CastRelease)
            {AiStep(.01f,boss);elapsed+=.01f;sawTelegraph|=boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.CastWindup;}
            Check(sawTelegraph&&boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.CastRelease&&boss.laviaBoss.CastAttacksStarted==1&&boss.laviaBoss.ProjectileActive,
                "The third ranged attack winds up, then releases Lavia's source-sheet fire projectile");
            interrupted=boss.laviaBoss.Interruptions;boss.Damage(18);
            Check(boss.laviaBoss.Interruptions>interrupted&&!boss.laviaBoss.ProjectileActive&&!boss.CanDamagePlayer,
                "A sword hit interrupts Lavia's cast and removes its pending fire projectile");
            PrepareTrialBoss(boss,2);before=boss.health;cheat.Show();
            var godButton=Array.Find(cheat.GetComponentsInChildren<Button>(true),button=>button.name=="Cheat God Mode");Check(godButton!=null,"Custom boss Godmode uses the actual existing cheat button");if(godButton==null)yield break;
            godButton.onClick.Invoke();cheat.Hide();Time.timeScale=0;
            Check(cheat.GodMode&&boss.health==before&&!boss.Dead,"Enabling Godmode leaves the custom boss alive until a positive player hit");
            playerLife=game.player.health;game.player.Damage(24,game.player.transform.position.x+1,false);game.player.DamageContact(24,game.player.transform.position.x+1);
            Check(game.player.health==playerLife,"Godmode still protects the player from the custom boss damage path");
            float expectedTears=game.progress.tears+boss.purgeReward*new InventoryModifiers(game.progress,game.player).TearsMultiplier*GuiltRules.Load().Tears(game.progress);
            int burstsBeforeKill=route.GateFx!=null?route.GateFx.BurstCount:-1;
            boss.Damage(1,true);AiStep(.02f,boss);
            Check(boss.Dead&&boss.health==0&&route.BossDefeated&&boss.laviaBoss.DefeatNotified&&boss.laviaBoss.State==BrotherhoodTrialLaviaAI.BrainState.Defeated&&Mathf.Abs(game.progress.tears-expectedTears)<.001f,
                "A single positive Godmode hit kills Lavia through her normal death, objective and Purge paths");
            Check(route.GateFx!=null&&route.GateFx.IsOpen&&!route.GateFx.BlockingArtVisible&&route.GateFx.BurstCount==burstsBeforeKill+1&&route.GateFx.ActiveShardCount>0,
                "A Godmode boss kill opens the rubble gate through exactly one source-textured crumble burst");
            boss.Damage(1);AiStep(.02f,boss);
            Check(game.progress.sourceFlags.Count(flag=>flag==BrotherhoodTrialRoute.BossFlag)==1&&Mathf.Abs(game.progress.tears-expectedTears)<.001f,"Repeated custom boss hits cannot duplicate its objective flag or scaled reward");
            Check(route.GateFx!=null&&route.GateFx.BurstCount==burstsBeforeKill+1,
                "Repeated hits on the defeated custom boss cannot trigger a second gate explosion");
            AiStep(2.5f,boss);
            Check(boss.laviaBoss.DeathShown&&!boss.gameObject.activeSelf,"Lavia shows her death animation before the defeated actor disappears");
            Check(SourceStoryState()==story,"Custom boss Hurt, stun, parry and Godmode leave all source Warden victory flags unchanged");
            cheat.Show();godButton.onClick.Invoke();cheat.Hide();Time.timeScale=0;
            game.progress.sourceFlags=game.progress.sourceFlags.Where(flag=>flag!=BrotherhoodTrialRoute.BossFlag).ToArray();boss.ResetEnemy();before=boss.health;boss.Damage(18);
            Check(!cheat.GodMode&&!boss.Dead&&boss.health==before-18&&boss.Hurt,"Disabling Godmode restores ordinary custom boss damage and Hurt");
        }
        finally
        {
            game.enabled=false;game.controls.ClearGameplayInput();
            if(cheat.GodMode){cheat.Show();Array.Find(cheat.GetComponentsInChildren<Button>(true),button=>button.name=="Cheat God Mode")?.onClick.Invoke();cheat.Hide();}
            typeof(BrotherhoodGame).GetField("bossDead",Private).SetValue(game,warden);game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();
            game.player.health=health;game.player.fervour=fervour;game.player.flasks=flasks;foreach(var file in files){if(file.Value==null)File.Delete(file.Key);else File.WriteAllBytes(file.Key,file.Value);}
            game.controls.ClearGameplayInput();game.enabled=enabled;Time.timeScale=previousTime;
        }
    }
    IEnumerator Capture(string name)
    {
        string path="Documentation/Previews/Trial/"+name+".png";Directory.CreateDirectory("Documentation/Previews/Trial");
        ScreenCapture.CaptureScreenshot(path);yield return null;
    }
    IEnumerator SourceArrivalChecks()
    {
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;Vector2 previousPosition=game.player.transform.position;
        float health=game.player.health,fervour=game.player.fervour;int flasks=game.player.flasks;bool enabled=game.enabled;
        var files=new[]{SavePath,SavePath+".bak",SavePath+".tmp"}.ToDictionary(path=>path,path=>File.Exists(path)?File.ReadAllBytes(path):null);
        var room=game.Find("D17Z01S07");var door=room.Door("N");
        Check(door!=null,"S07 retains its authored north RoomDoor independently of the custom passages");if(door==null)yield break;
        string target=door.target,targetDoor=door.targetDoor;Vector3 sourceSpawn=door.spawn.position,sourceTrigger=door.trigger.position;
        try
        {
            game.enabled=false;game.controls.ClearGameplayInput();game.Enter(room.id,"N",null);game.player.Restore();game.enabled=true;Time.timeScale=1;
            yield return new WaitForSecondsRealtime(.2f);
            Vector2 feet=game.player.transform.position;var floor=Physics2D.Raycast(feet+Vector2.up*.2f,Vector2.down,.5f,(1<<8)|(1<<9));
            Check(game.Current.id==room.id&&!game.player.Dead&&game.player.motor.grounded&&floor.collider!=null&&floor.normal.y>.6f&&game.player.motor.IsClearAt(feet,game.player.motor.size.y),
                "Entering the actual S07 north RoomDoor resolves to a grounded floor position with clear standing-capsule space");
            Check(door.target==target&&door.targetDoor==targetDoor&&door.spawn.position==sourceSpawn&&door.trigger.position==sourceTrigger,
                "Protecting the S07 north arrival preserves the authored RoomDoor destination, trigger and spawn transforms");
            var before=feet;yield return new WaitForSecondsRealtime(.35f);
            Check(game.Current.id==room.id&&!game.player.Dead&&Vector2.Distance(game.player.transform.position,before)<.1f,
                "The protected S07 north arrival remains stable instead of falling through terrain or bouncing into another door");
            var ladders=room.ladders.Where(ladder=>ladder.sensor!=null&&ladder.sensor.name=="Trial return ladder").OrderByDescending(ladder=>ladder.size.y).ToArray();
            Check(ladders.Length==2,"S07 adds exactly its long and short trial return ladders alongside the source ladder zones");
            foreach(var ladder in ladders)
            {
                string label=ladder.center.x>-860?"Upper trial ladder":"Lower trial ladder";
                Vector2 ends=route.LadderLandings(ladder);float top=ends.y-.025f,bottom=ends.x;
                game.enabled=false;game.controls.ClearGameplayInput();game.player.Restore();game.player.motor.Teleport(new Vector2(ladder.center.x,top-.9f));
                game.enabled=true;game.controls.Vertical=-1;bool descending=false;float deadline=Time.realtimeSinceStartup+ladder.size.y/3.2f*4+5;
                while(game.Current.id==room.id&&!game.player.Dead&&(game.player.transform.position.y>bottom+.05f||!game.player.motor.grounded)&&Time.realtimeSinceStartup<deadline)
                {descending|=game.player.actor.Current=="penitent_ladder_going_down";yield return null;}
                yield return new WaitForSecondsRealtime(.6f);
                Check(descending&&game.Current.id==room.id&&!game.player.Dead&&game.player.motor.grounded&&Mathf.Abs(game.player.transform.position.y-bottom)<.08f&&game.player.motor.IsClearAt(game.player.transform.position,game.player.motor.size.y),
                    label+" responds to held Down and remains grounded at its solid floor instead of descending through it");
                game.controls.ClearGameplayInput();game.controls.Vertical=1;bool ascending=false;deadline=Time.realtimeSinceStartup+ladder.size.y/3.2f*4+5;
                while(game.Current.id==room.id&&!game.player.Dead&&(game.player.transform.position.y<ends.y-.001f||!game.player.motor.grounded||Read<bool>(game.player,"laddering"))&&Time.realtimeSinceStartup<deadline)
                {ascending|=game.player.actor.Current=="penitent_ladder_going_up";yield return null;}
                game.controls.ClearGameplayInput();yield return new WaitForSecondsRealtime(.2f);
                Check(ascending&&game.Current.id==room.id&&!game.player.Dead&&game.player.motor.grounded&&!Read<bool>(game.player,"laddering")&&Mathf.Abs(game.player.transform.position.y-top-.025f)<.1f&&game.player.motor.IsClearAt(game.player.transform.position,game.player.motor.size.y),
                    label+" responds to held Up and exits onto its upper source platform with clear standing space; actual="+game.player.transform.position+", targetY="+(top+.025f)+", grounded="+game.player.motor.grounded);
            }
        }
        finally
        {
            game.enabled=false;game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();game.player.health=health;game.player.fervour=fervour;game.player.flasks=flasks;
            foreach(var file in files){if(file.Value==null)File.Delete(file.Key);else File.WriteAllBytes(file.Key,file.Value);}
            game.controls.ClearGameplayInput();game.enabled=enabled;Time.timeScale=1;
        }
    }
    void AiStep(float seconds,params EnemyController[] enemies)
    {
        for(float remaining=seconds;remaining>.00001f;)
        {
            float dt=Mathf.Min(1f/120,remaining);
            foreach(var enemy in enemies){enemy.Tick(dt);enemy.actor.Advance(dt);}
            game.effects.Tick(dt);remaining-=dt;
        }
    }
    void PrepareGuard(EnemyController guard,EnemyController scout)
    {
        scout.health=0;guard.ResetEnemy();game.player.Restore();game.player.motor.Teleport((Vector2)guard.transform.position+Vector2.left*2.2f);
        guard.trialAI.Configure(guard,BrotherhoodTrialEnemyAI.Kind.Guard,3,-1);Physics2D.SyncTransforms();Time.timeScale=0;
        AiStep(1.16f,guard);
    }
    IEnumerator TrialAiAndCombatChecks()
    {
        var previousProgress=JsonUtility.FromJson<PlayerProgress>(JsonUtility.ToJson(game.progress));string previousRoom=game.Current.id;Vector2 previousPosition=game.player.transform.position;
        bool previousEnabled=game.enabled;float previousTime=Time.timeScale;var room=game.Find("D17Z01S07");var originalEnemies=room.enemies;
        var cheat=game.controls.debugUI;GameObject wall=null,platform=null;
        var enemyOrigins=new Dictionary<EnemyController,Vector2>();var brainHomes=new Dictionary<EnemyController,Vector2>();
        try
        {
            game.enabled=false;game.controls.ClearGameplayInput();game.Enter(room.id,null,null);game.player.Restore();Time.timeScale=0;
            var scout=route.ArenaEnemies.FirstOrDefault(enemy=>enemy.trialAI?.EnemyKind==BrotherhoodTrialEnemyAI.Kind.Scout);
            var guard=route.ArenaEnemies.FirstOrDefault(enemy=>enemy.trialAI?.EnemyKind==BrotherhoodTrialEnemyAI.Kind.Guard);
            Check(scout!=null&&guard!=null,"Trial AI fixtures use the two actual custom arena NPC components");if(scout==null||guard==null)yield break;
            foreach(var enemy in new[]{scout,guard}){enemyOrigins[enemy]=Read<Vector2>(enemy,"origin");brainHomes[enemy]=enemy.trialAI.Home;enemy.ResetEnemy();}
            Check(new[]{scout,guard}.All(enemy=>enemy.transform.Find("Trial sign")==null&&
                enemy.GetComponentsInChildren<TextMesh>(true).Length==0&&
                enemy.GetComponentsInChildren<Text>(true).All(label=>label.GetComponentInParent<Canvas>()?.renderMode!=RenderMode.WorldSpace)),
                "Neither S07 custom enemy creates a visible name label above its sprite");
            // This small enemy keeps the old sword allowance while Lavia gets a
            // torso-sized receiver; isolate it from nearby Guard shield logic.
            room.enemies=new[]{scout};scout.ResetEnemy();
            float scoutHit=SwordDamageAt(scout,(Vector2)scout.transform.position+Vector2.right*2.15f,-1);
            Check(scoutHit>0&&Mathf.Abs(scout.Radius-.4f)<.001f,
                "An ordinary small enemy retains the source sword reach and collision radius");
            scout.ResetEnemy();
            float scoutMiss=SwordDamageAt(scout,(Vector2)scout.transform.position+Vector2.right*2.55f,-1);
            Check(Mathf.Abs(scoutMiss)<.001f,"The same ordinary enemy remains outside a normal slash at 2.55 units");
            room.enemies=originalEnemies;scout.ResetEnemy();Time.timeScale=0;
            Vector2 guardSpawn=guard.trialAI.Home;
            var guardFloor=Physics2D.Raycast(guardSpawn+Vector2.up*.25f,Vector2.down,.65f,(1<<8)|(1<<9));
            Check(Mathf.Abs(guardSpawn.x+862.4f)<.05f&&guard.motor.IsClearAt(guardSpawn,guard.motor.size.y)&&
                guardFloor.collider!=null&&guardFloor.normal.y>.6f,
                "The Guard now spawns on clear supported floor before the S07 stair ramp");
            RaycastHit2D ramp=default;
            for(float x=-859.25f;x<=-855f&&ramp.collider==null;x+=.25f)
                foreach(var hit in Physics2D.RaycastAll(new Vector2(x,-29),Vector2.down,10f,(1<<8)|(1<<9)))
                {
                    // On a 1:2 ramp the uphill side of a standing capsule is
                    // higher than its centre. Sample above that full width.
                    Vector2 feet=new Vector2(x,hit.point.y+.3f);
                    if(hit.collider!=null&&hit.normal.y>.65f&&hit.normal.y<.98f&&
                        scout.motor.IsClearAt(feet,scout.motor.size.y)&&guard.motor.IsClearAt(feet,guard.motor.size.y))
                    {ramp=hit;break;}
                }
            Check(ramp.collider!=null,"The actual S07 terrain contains a walkable stair slope for both custom enemies");
            if(ramp.collider!=null)
            {
                Vector2 rampFeet=ramp.point+Vector2.up*.3f;
                foreach(var enemy in new[]{scout,guard})
                {
                    room.enemies=new[]{enemy};enemy.motor.Teleport(rampFeet);
                    enemy.trialAI.Configure(enemy,enemy.trialAI.EnemyKind,4,1);
                    game.player.motor.Teleport(new Vector2(-868.5f,-36.975f));
                    float startX=enemy.transform.position.x,previousX=startX;int reversals=0;
                    for(int sample=0;sample<60;sample++)
                    {
                        AiStep(1f/120,enemy);
                        if(enemy.transform.position.x<previousX-.004f)reversals++;
                        previousX=enemy.transform.position.x;
                    }
                    var feet=enemy.transform.position;
                    var support=Physics2D.Raycast((Vector2)feet+Vector2.up*.3f,Vector2.down,.7f,(1<<8)|(1<<9));
                    Check(feet.x>startX+.18f&&reversals==0&&support.collider!=null&&support.normal.y>.6f&&
                        Mathf.Abs(feet.y-support.point.y)<.35f,
                        enemy.name+" climbs the authored S07 stair slope steadily without back-and-forth jitter; dx="+
                        (feet.x-startX).ToString("0.000")+", reversals="+reversals+
                        ", support="+(support.collider==null?"none":support.collider.name));
                }
                room.enemies=originalEnemies;
                foreach(var enemy in new[]{scout,guard})
                {
                    enemy.motor.Teleport(brainHomes[enemy]);
                    enemy.trialAI.Configure(enemy,enemy.trialAI.EnemyKind,3,-1);enemy.ResetEnemy();
                }
            }
            guard.motor.Teleport((Vector2)scout.transform.position+Vector2.right*3);
            guard.trialAI.Configure(guard,BrotherhoodTrialEnemyAI.Kind.Guard,3,1);
            scout.trialAI.Configure(scout,BrotherhoodTrialEnemyAI.Kind.Scout,3,-1);
            Vector2 scoutHome=scout.trialAI.Home;game.player.motor.Teleport(scoutHome+Vector2.left*4);
            wall=new GameObject("Trial verification line-of-sight wall");wall.layer=8;wall.transform.position=scoutHome+new Vector2(-2,1.1f);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.25f,3.2f);Physics2D.SyncTransforms();
            AiStep(.3f,scout,guard);
            Check(!scout.trialAI.PlayerVisible&&scout.trialAI.AlertsRaised==0&&scout.trialAI.MemoryRemaining==0&&!scout.trialAI.CanStrikePlayer(),
                "A terrain wall blocks the Scout's player acquisition, alarm and strike perception");
            Destroy(wall);wall=null;yield return null;Physics2D.SyncTransforms();
            Check(!guard.trialAI.PlayerVisible&&guard.trialAI.MemoryRemaining==0,"The Guard initially faces away and has no independent player memory");
            var sourcePeer=game.Find("D17Z01S02").enemies.First(enemy=>enemy.trialAI==null);int sourceState=Read<int>(sourcePeer,"state");
            room.enemies=originalEnemies.Concat(new[]{sourcePeer}).ToArray();AiStep(.62f,scout,guard);
            Check(scout.trialAI.PlayerVisible&&scout.trialAI.AlertsRaised==1&&scout.trialAI.State==BrotherhoodTrialEnemyAI.BrainState.Chase,
                "An unobstructed player stimulus makes the Scout stop, alert once and choose pursuit");
            Check(guard.trialAI.MemoryRemaining>0&&guard.trialAI.State!=BrotherhoodTrialEnemyAI.BrainState.Patrol&&sourcePeer.trialAI==null&&Read<int>(sourcePeer,"state")==sourceState,
                "The Scout's nearby alert changes its custom teammate's decision without modifying a source NPC");
            Vector2 lastSeen=scout.trialAI.LastSeen;guard.health=0;
            wall=new GameObject("Trial verification memory occluder");wall.layer=8;wall.transform.position=(Vector2)scout.transform.position+new Vector2(-1.1f,1.1f);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.25f,3.2f);game.player.motor.Teleport(scoutHome+Vector2.left*7);Physics2D.SyncTransforms();AiStep(.3f,scout);
            Check(!scout.trialAI.PlayerVisible&&Vector2.Distance(scout.trialAI.LastSeen,lastSeen)<.001f&&scout.trialAI.MemoryRemaining>0,
                "An occluded moving player leaves the Scout pursuing its remembered last-seen position rather than tracking through walls");
            bool searched=false,returned=false,reachedHome=false;
            for(int step=0;step<80;step++){AiStep(.1f,scout);searched|=scout.trialAI.State==BrotherhoodTrialEnemyAI.BrainState.Search;returned|=scout.trialAI.State==BrotherhoodTrialEnemyAI.BrainState.Return;reachedHome|=returned&&scout.trialAI.State==BrotherhoodTrialEnemyAI.BrainState.Patrol&&Vector2.Distance(scout.transform.position,scoutHome)<.2f;}
            Check(searched&&returned&&reachedHome&&scout.trialAI.MemoryRemaining==0,
                "After sight memory expires the Scout searches, gives up and returns to its patrol home");
            Destroy(wall);wall=null;yield return null;
            // A raised, short platform makes the edge stimulus independent of
            // the authored arena's safe continuous floor.
            scout.ResetEnemy();float highestTerrain=room.GetComponentsInChildren<Collider2D>().Where(collider=>collider.enabled&&(collider.gameObject.layer==8||collider.gameObject.layer==9)).Select(collider=>collider.bounds.max.y).DefaultIfEmpty(room.top).Max();
            Vector2 edge=new Vector2(scoutHome.x,highestTerrain+8);
            scout.motor.Teleport(edge);scout.trialAI.Configure(scout,BrotherhoodTrialEnemyAI.Kind.Scout,3,1);
            game.player.motor.Teleport(edge+Vector2.right*25);platform=new GameObject("Trial verification cliff platform");platform.layer=8;
            platform.transform.position=edge+new Vector2(-1,-.13f);platform.AddComponent<BoxCollider2D>().size=new Vector2(2.4f,.2f);Physics2D.SyncTransforms();
            var forwardFloor=Physics2D.Raycast(edge+new Vector2(scout.Radius+.3f,.4f),Vector2.down,1.4f,(1<<8)|(1<<9));
            var currentFloor=Physics2D.Raycast(edge+Vector2.up*.12f,Vector2.down,.23f,(1<<8)|(1<<9));
            Func<RaycastHit2D,string> hitDetail=hit=>hit.collider==null?"none":hit.collider.name+"/layer"+hit.collider.gameObject.layer+"/"+hit.collider.bounds;
            Check(forwardFloor.collider==null&&currentFloor.collider==platform.GetComponent<Collider2D>(),
                "Cliff fixture has a supporting platform under the Scout and no forward floor"+(forwardFloor.collider==null&&currentFloor.collider==platform.GetComponent<Collider2D>()?"":"; forward="+hitDetail(forwardFloor)+", ground="+hitDetail(currentFloor)));
            AiStep(.02f,scout);
            bool safeCliff=Mathf.Abs(scout.transform.position.x-edge.x)<.02f&&scout.Facing<0&&Mathf.Abs(scout.motor.velocity.x)<.01f;
            Check(safeCliff,"A missing floor ahead makes the Scout pause and reverse before walking off a cliff"+(safeCliff?"":"; dx="+(scout.transform.position.x-edge.x)+", facing="+scout.Facing+", vx="+scout.motor.velocity.x+", brain="+scout.trialAI.State+", playerDead="+game.player.Dead+", blocked="+game.InputBlocked+", applies="+scout.trialAI.AppliesTo(scout)+", sourceState="+Read<int>(scout,"state")+", timer="+Read<float>(scout,"timer")+", forward="+hitDetail(forwardFloor)+", ground="+hitDetail(currentFloor)));
            Destroy(platform);platform=null;yield return null;scout.motor.Teleport(scoutHome);scout.trialAI.Configure(scout,BrotherhoodTrialEnemyAI.Kind.Scout,3,-1);
            PrepareGuard(guard,scout);
            Check(guard.trialAI.State==BrotherhoodTrialEnemyAI.BrainState.Guard&&!guard.AttackInProgress&&Mathf.Abs(guard.motor.velocity.x)<.01f,
                "At a useful fighting gap the Guard chooses a stationary defense before attacking");
            game.player.attackTime=1;float before=guard.health;guard.Damage(18,true);
            Check(guard.health==before&&guard.trialAI.GuardedHits==1&&guard.trialAI.State==BrotherhoodTrialEnemyAI.BrainState.Counter,
                "A frontal sword hit is guarded once and causes a deliberate counter decision");
            AiStep(.25f,guard);
            Check(guard.AttackInProgress&&guard.actor.Current=="NewFlagellant_attack","The Guard's counter uses the existing Flagellant attack sequence");
            guard.Damage(1);game.player.motor.Teleport((Vector2)guard.transform.position+Vector2.left*.4f);typeof(PlayerController).GetField("invincible",Private).SetValue(game.player,0f);
            float playerLife=game.player.health;AiStep(.2f,guard);game.player.attackTime=0;game.player.Tick(.02f);
            Check(guard.Hurt&&!guard.AttackInProgress&&!guard.CanDamagePlayer&&game.player.health==playerLife,
                "An incoming hit interrupts the custom counter and Hurt cannot deal attack or contact damage");
            PrepareGuard(guard,scout);game.player.attackTime=1;before=guard.health;guard.Damage(18);
            Check(guard.health==before-18&&guard.Hurt&&guard.trialAI.GuardedHits==0,"A non-melee hit bypasses the custom shield even while the player has a sword attack animation");
            PrepareGuard(guard,scout);game.player.attackTime=1;before=guard.health;game.player.HeavyWeaponHit(guard,18);
            Check(guard.health==before-18&&guard.Hurt&&guard.trialAI.GuardedHits==0,"A heavy weapon hit retains its source context and bypasses the custom shield");
            guard.ResetEnemy();guard.health=guard.maxHealth*.4f;game.player.Restore();game.player.motor.Teleport((Vector2)guard.transform.position+Vector2.left*2.2f);
            guard.trialAI.Configure(guard,BrotherhoodTrialEnemyAI.Kind.Guard,3,-1);Vector2 retreatStart=guard.transform.position;Time.timeScale=0;AiStep(.65f,guard);
            Check(guard.trialAI.State==BrotherhoodTrialEnemyAI.BrainState.Retreat&&guard.transform.position.x>retreatStart.x+.05f&&guard.Facing<0,
                "Low Life makes the Guard retreat to increase its gap while continuing to face the player");
            PrepareGuard(guard,scout);guard.OnParried(1);playerLife=game.player.health;game.player.motor.Teleport((Vector2)guard.transform.position+Vector2.right*.4f);typeof(PlayerController).GetField("invincible",Private).SetValue(game.player,0f);AiStep(.2f,guard);game.player.Tick(.02f);
            Check(!guard.CanDamagePlayer&&game.player.health==playerLife,"The existing parry stun also suppresses the custom Guard's pending attack and contact damage");
            PrepareGuard(guard,scout);before=guard.health;cheat.Show();
            var godButton=Array.Find(cheat.GetComponentsInChildren<Button>(true),button=>button.name=="Cheat God Mode");
            Check(godButton!=null,"Custom NPC Godmode regression uses the actual cheat toggle button");if(godButton==null)yield break;
            godButton.onClick.Invoke();cheat.Hide();Time.timeScale=0;game.player.attackTime=1;
            Check(cheat.GodMode&&guard.health==before,"Enabling Godmode leaves custom enemy Life unchanged until a positive hit");
            playerLife=game.player.health;game.player.Damage(20,game.player.transform.position.x+1,false);game.player.DamageContact(20,game.player.transform.position.x+1);
            Check(game.player.health==playerLife,"Godmode still protects the player against both attack and contact damage in the custom branch");
            float tears=game.progress.tears;float expectedTears=tears+guard.purgeReward*new InventoryModifiers(game.progress,game.player).TearsMultiplier*GuiltRules.Load().Tears(game.progress);guard.Damage(1,true);
            Check(guard.Dead&&guard.health==0&&Mathf.Abs(game.progress.tears-expectedTears)<.001f,"Godmode bypasses the custom shield and kills through the original reward and death path");
            guard.Damage(1,true);Check(Mathf.Abs(game.progress.tears-expectedTears)<.001f,"Repeated hits on a custom Godmode kill cannot duplicate its reward");
            var boss=game.Find("D17Z01S11").Boss;float bossLife=boss.health;boss.Damage(1,true);
            Check(!boss.BossIntroComplete&&!boss.Dead&&boss.health==bossLife,"Custom AI and Godmode leave the original boss intro protection intact");
            cheat.Show();godButton.onClick.Invoke();cheat.Hide();Time.timeScale=0;guard.ResetEnemy();before=guard.health;guard.Damage(18);
            Check(!cheat.GodMode&&guard.health==before-18&&guard.Hurt&&!guard.Dead,"After disabling Godmode custom NPC damage and Hurt return to normal");
        }
        finally
        {
            if(wall!=null)Destroy(wall);if(platform!=null)Destroy(platform);
            if(cheat.GodMode){cheat.Show();Array.Find(cheat.GetComponentsInChildren<Button>(true),button=>button.name=="Cheat God Mode")?.onClick.Invoke();cheat.Hide();}
            room.enemies=originalEnemies;
            foreach(var entry in enemyOrigins)
            {
                typeof(EnemyController).GetField("origin",Private).SetValue(entry.Key,entry.Value);entry.Key.motor.Teleport(brainHomes[entry.Key]);
                entry.Key.trialAI.Configure(entry.Key,entry.Key.trialAI.EnemyKind,3,-1);entry.Key.ResetEnemy();
            }
            game.progress=previousProgress;game.Enter(previousRoom,null,previousPosition);game.player.Restore();game.controls.ClearGameplayInput();game.enabled=previousEnabled;Time.timeScale=previousTime;
        }
    }
}