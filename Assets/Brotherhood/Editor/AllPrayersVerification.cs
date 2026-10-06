using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Brotherhood;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class AllPrayersVerification
{
    public const string Flag="Temp/brotherhood-prayers-verify";
    static AllPrayersVerification(){EditorApplication.update+=Attach;}
    public static void RunInEditor()
    {
        PrayerAssets.Prepare();EditorSettings.enterPlayModeOptionsEnabled=false;
        File.WriteAllText(Flag,"");EditorSceneManager.OpenScene(BrotherhoodBuilder.ScenePath);EditorApplication.isPlaying=true;
    }
    static void Attach()
    {
        if(EditorApplication.isPlaying&&File.Exists(Flag)&&UnityEngine.Object.FindAnyObjectByType<AllPrayersPlayCheck>()==null)
            new GameObject("All prayers source verification").AddComponent<AllPrayersPlayCheck>();
    }
}
public sealed class AllPrayersPlayCheck:MonoBehaviour
{
    BrotherhoodGame game;bool failed;readonly List<string> checks=new List<string>();
    void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);failed|=!ok;Debug.Log("[Prayers] "+checks[checks.Count-1]);}
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.5f);
        try
        {
            game=FindAnyObjectByType<BrotherhoodGame>();game.enabled=false;game.controls.Simulation=true;Time.timeScale=0;
            CastChecks();BuffChecks();LegacyPrayerChecks.Run(game,Check,Step,Place);AdditionalPrayerChecks.Run(game,Check,Step,Place);
            CleanupChecks();VisualChecks();
        }
        catch(Exception e){Check(false,"Unexpected exception: "+e);Debug.LogException(e);}
        finally
        {
            Time.timeScale=1;File.WriteAllText("Documentation/prayers-playtest-results.txt","Run UTC: "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",checks)+"\nRESULT: "+(failed?"FAILED":"PASSED"));
            File.Delete(AllPrayersVerification.Flag);EditorApplication.isPlaying=false;
        }
    }
    void Step(float seconds)
    {
        for(float left=seconds;left>.00001f;){float dt=Mathf.Min(1f/120,left);game.player.Tick(dt);game.player.actor.Advance(dt);game.effects.Tick(dt);left-=dt;}
    }
    void Place(Vector2 point)
    {
        game.controls.ClearGameplayInput();game.player.Restore();game.effects.Clear();game.player.motor.Teleport(point);Step(.6f);Time.timeScale=0;
    }
    void Prepare(string id,string heart="")
    {
        game.progress=new PlayerProgress();game.Enter("D17Z01S02",null,null);foreach(var enemy in game.Current.enemies)enemy.health=0;
        Place(game.Current.start.position);game.progress.SetOwned(id,true);game.progress.equippedPrayer=id;game.progress.hasPrayer=true;game.progress.equippedSwordHeart=heart;game.player.fervour=200;
    }
    void Cast(){game.controls.Prayer=true;Step(.01f);game.controls.Prayer=false;Step(.55f);}
    void CastChecks()
    {
        var catalog=InventoryCatalog.Load();Check(catalog.Count("prayer")==17,"All seventeen original mobile prayers are present");
        foreach(var item in catalog.items)
        {
            if(item.category!="prayer")continue;
            Prepare(item.id);
            if(item.id=="PR202"){game.Enter("D17Z01S05",null,null);Place(game.Current.checkpoints[0].position);game.Interact(false,true);game.Enter("D17Z01S02",null,null);Place(game.Current.start.position);game.player.fervour=200;}
            game.controls.Prayer=true;Step(.01f);game.controls.Prayer=false;
            Check(game.player.PrayerCasting&&game.player.actor.Current=="penitent_aura_transform"&&game.player.fervour==200,item.id+" begins the source AuraTransform without spending mana early");
            Step(.53f);Check(game.player.fervour==200,item.id+" waits for the 0.55-second animation event");
            Step(.025f);Check(Mathf.Abs(game.player.fervour-(200-item.fervourNeeded))<.001f,item.id+" spends its original Fervour cost once");
            float mana=game.player.fervour;game.controls.Prayer=true;Step(.01f);game.controls.Prayer=false;
            Check(game.player.fervour==mana,item.id+" ignores repeat input during the casting animation");
            Capture(item.id);
            game.player.StopPrayers();Check(game.legacyPrayers.ActiveActors==0&&game.prayerEffects.ActiveActors==0&&game.prayerBuffs.ActiveActors==0,item.id+" explicitly releases its transient effects");
        }
        Prepare("PR03");game.player.fervour=39;Cast();Check(!game.player.PrayerCasting&&game.player.fervour==39,"Insufficient Fervour cannot start a prayer or consume mana");
        Prepare("PR03");game.player.motor.Teleport(game.player.transform.position+Vector3.up*5);game.player.motor.grounded=false;game.controls.Prayer=true;Step(.01f);game.controls.Prayer=false;
        Check(!game.player.PrayerCasting&&game.player.fervour==200,"Prayers require grounded casting as in the mobile source");
        Prepare("PR01");Cast();Step(.8f);float manaAfter=game.player.fervour;game.controls.Prayer=true;Step(.01f);game.controls.Prayer=false;
        Check(game.player.fervour==manaAfter&&!game.player.PrayerCasting,"An active timed prayer prevents another cast");
        Prepare("PR03");game.controls.Prayer=true;Step(.1f);game.controls.Prayer=false;game.player.Damage(1,game.player.transform.position.x-2,false);Step(.6f);
        Check(game.player.fervour==200&&game.legacyPrayers.ActiveActors==0,"Damage before the cast event cancels the invocation without spending Fervour");
        Prepare("PR01");game.progress.equippedRosaryBeads=new[]{"RB108"};Cast();Check(game.player.ActivePrayer=="PR01"&&Mathf.Abs(game.player.actor.Speed-1.25f)<.001f,"RB108 speeds up both prayer pose and event timing");
    }
    void BuffChecks()
    {
        Prepare("PR01","HE01");Cast();Check(game.player.PrayerTimeRemaining>14.9f&&game.prayerBuffs.TrailTimeRemaining>14.9f,"PR01 accepts the Heart's five-second effect extension");
        Step(.8f);game.controls.Attack=true;Step(.01f);game.controls.Attack=false;Check(game.player.actor.Speed==1.5f,"PR01 attack speed observes the source 1.5 maximum");
        Check(game.prayerBuffs.ActiveActors>0&&game.prayerBuffs.TrailColor.r>.87f,"PR01 periodically generates its source purple trail");Step(14.3f);Check(string.IsNullOrEmpty(game.player.ActivePrayer),"PR01 removes its timed stat after fifteen seconds with HE01");
        Prepare("PR04");Cast();Step(.8f);game.player.health=20;var enemy=game.Current.enemies[0];enemy.ResetEnemy();enemy.health=1000;enemy.motor.Teleport(game.player.transform.position+Vector3.right*5);
        PrayerCombat.Damage(game,enemy,20);Check(Mathf.Abs(game.player.health-23)<.001f,"Saeta restores fifteen percent from an ordinary successful hit");
        game.player.HeavyWeaponHit(enemy,40);Check(Mathf.Abs(game.player.health-29)<.001f,"Saeta also restores Life from a heavy weapon hit");
        Step(10);PrayerCombat.Damage(game,enemy,20);Check(Mathf.Abs(game.player.health-29)<.001f,"Saeta cannot heal after the prayer expires");
        Prepare("PR10");Cast();Step(.8f);Check(new InventoryModifiers(game.progress,game.player).DamageDealt(18)==45,"Solea multiplies sword Strength by 2.5");
        enemy=game.Current.enemies[0];enemy.ResetEnemy();enemy.health=1000;enemy.motor.Teleport(game.player.transform.position+Vector3.right*1.5f);game.controls.Attack=true;Step(.01f);game.controls.Attack=false;
        bool upgradedSlash=game.effects.HasActiveClip("Slash_clamped_attack_lvl2_1");Step(.5f);
        Check(enemy.health<1000&&upgradedSlash,"Solea uses the upgraded source sword slash and strikes an enemy");
        Step(10);Check(new InventoryModifiers(game.progress,game.player).DamageDealt(18)==18,"Solea restores normal Strength at expiry");
        Prepare("PR16","HE01");Cast();game.progress.tears=0;game.EnemyDefeated(10);
        Check(Mathf.Abs(game.progress.tears-13)<.001f&&game.prayerBuffs.HarvestTimeRemaining==2,"Zambra adds thirty percent Tears once and begins its kill effect");
        Step(.5f);game.EnemyDefeated(10);Check(game.prayerBuffs.HarvestTimeRemaining<1.51f,"Zambra's active harvest effect is not restarted by another kill");
        Step(24.7f);Check(game.player.PrayerTimeRemaining>4&&game.prayerBuffs.TrailTimeRemaining==0&&!game.itemEffects.HasTemporalFlag(0),"Zambra's trail and stop-Fervour component expire at twenty-five seconds even with the duration Heart");
        float before=game.progress.tears;game.EnemyDefeated(10);Check(Mathf.Abs(game.progress.tears-before-13)<.001f,"Zambra's extended Tears stat continues for the remaining five seconds");
        Step(5);before=game.progress.tears;game.EnemyDefeated(10);Check(Mathf.Abs(game.progress.tears-before-10)<.001f,"Zambra removes its Tears stat when the extended prayer ends");
        Prepare("PR03","HE01");Cast();Step(2.1f);Check(game.player.PrayerTimeRemaining>4&&!game.itemEffects.HasTemporalFlag(1),"Debla's fixed two-second invulnerability is not extended by the Heart");
    }
    void CleanupChecks()
    {
        Prepare("PR15");Cast();Step(.8f);int actors=game.legacyPrayers.ActiveActors;game.legacyPrayers.Tick(0);Check(game.legacyPrayers.ActiveActors==actors,"A paused prayer clock leaves effect lifetimes unchanged");
        game.Enter("D17Z01S05",null,null);Check(game.legacyPrayers.ActiveActors==0&&game.prayerEffects.ActiveActors==0&&game.player.ActivePrayer=="","Changing rooms releases prayer actors and stat effects");
        Prepare("PR08");Cast();Step(.8f);game.player.Damage(999,game.player.transform.position.x-2,false);Check(game.player.Dead&&game.legacyPrayers.ActiveActors==0,"Death clears the shields and other prayer effects");
        Prepare("PR01");Cast();Check(game.EquipInventoryItem(InventoryCatalog.Load().Find("PR01"))&&game.player.ActivePrayer==""&&game.prayerBuffs.ActiveActors==0,"Unequipping a prayer removes its temporary stats and trails");
    }
    void VisualChecks()
    {
        foreach(string id in new[]{"PR03","PR05","PR08","PR11","PR12","PR15","PR16","PR101","PR201","PR203"})
        {
            Prepare(id);Cast();
            if(id=="PR03")Step(.5f);
            else if(id=="PR12")Step(.1f);
            else if(id=="PR101"||id=="PR201")
            {
                Step(1.4f);game.controls.Attack=true;Step(.01f);game.controls.Attack=false;Step(id=="PR201"?.25f:.15f);
            }
            else Step(id=="PR05"?2.2f:1.3f);
            if(id=="PR11")game.player.Damage(10,game.player.transform.position.x-2,false);
            if(id=="PR16")game.EnemyDefeated(10);
            Capture(id+"-effect");
            if(id=="PR201")
            {
                Step(.6f);
                Capture(id+"-shards",new Vector3(game.prayerEffects.CompanionPosition.x+3,game.player.transform.position.y,0));
            }
        }
        game.player.StopPrayers();
    }
    void Capture(string id,Vector3? center=null)
    {
        var camera=game.view;var previousTarget=camera.targetTexture;var previousActive=RenderTexture.active;
        var target=new RenderTexture(960,540,24);var png=new Texture2D(960,540,TextureFormat.RGB24,false);
        try
        {
            camera.transform.position=(center??game.player.transform.position)+new Vector3(0,2,-10);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
            png.ReadPixels(new Rect(0,0,960,540),0,0);png.Apply();Directory.CreateDirectory("Documentation/Previews/Prayers");File.WriteAllBytes("Documentation/Previews/Prayers/"+id+".png",png.EncodeToPNG());
        }
        finally{camera.targetTexture=previousTarget;RenderTexture.active=previousActive;target.Release();Destroy(target);Destroy(png);}
    }
}
