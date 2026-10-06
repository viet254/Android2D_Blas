using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Linq;
using Brotherhood;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PriorityTwoVerification
{
    const string Flag="Temp/brotherhood-priority-two-verify";
    static PriorityTwoVerification(){EditorApplication.update+=Attach;}
    public static void RunInEditor()
    {
        // Test the saved scene directly. Do not regenerate catalog, maps or UI.
        File.Delete("Temp/brotherhood-priority-one-verify");File.Delete("Temp/brotherhood-verify-play");
        PriorityTwoAssets.Prepare();PrayerAssets.Prepare();EditorSettings.enterPlayModeOptionsEnabled=false;File.WriteAllText(Flag,"");
        EditorSceneManager.OpenScene(BrotherhoodBuilder.ScenePath);EditorApplication.isPlaying=true;
    }
    static void Attach()
    {
        if(EditorApplication.isPlaying&&File.Exists(Flag)&&UnityEngine.Object.FindAnyObjectByType<PriorityTwoPlayCheck>()==null)
            new GameObject("Priority two verification").AddComponent<PriorityTwoPlayCheck>();
    }
}
public sealed class PriorityTwoPlayCheck:MonoBehaviour
{
    readonly List<string> checks=new List<string>();BrotherhoodGame game;bool failed;
    static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    void Check(bool ok,string label){checks.Add((ok?"PASS ":"FAIL ")+label);failed|=!ok;Debug.Log("[PriorityTwo] "+checks[checks.Count-1]);}
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.5f);
        try
        {
            game=FindAnyObjectByType<BrotherhoodGame>();Check(game!=null,"Saved scene starts without rebuilding");
            if(game!=null)
            {
                game.enabled=false;game.controls.Simulation=true;Time.timeScale=0;
                var regression=gameObject.AddComponent<PriorityOnePlayCheck>();regression.enabled=false;
                typeof(PriorityOnePlayCheck).GetMethod("Run",Private).Invoke(regression,null);
                var previous=(List<string>)typeof(PriorityOnePlayCheck).GetField("checks",Private).GetValue(regression);
                foreach(var check in previous){checks.Add("P1 "+check);failed|=check.StartsWith("FAIL ");}
                CoreChecks();GameplayChecks();AdditionalPrayerChecks();MapAndGuiltChecks();AudioChecks();WorldChecks();ItemAndSettingsChecks();
            }
        }
        catch(Exception e){Check(false,"Unexpected exception: "+e);Debug.LogException(e);}
        finally
        {
            Time.timeScale=1;File.WriteAllText("Documentation/priority-two-playtest-results.txt","Run UTC: "+DateTime.UtcNow.ToString("O")+"\n"+string.Join("\n",checks)+"\nRESULT: "+(failed?"FAILED":"PASSED"));
            File.Delete("Temp/brotherhood-priority-two-verify");EditorApplication.isPlaying=false;
        }
    }
    void CoreChecks()
    {
        var progress=new PlayerProgress();game.progress=progress;game.player.Restore();
        var mods=new InventoryModifiers(progress,game.player);var catalog=InventoryCatalog.Load();
        Check(mods.RangedCost==7,"Ranged cost is the source seven Fervour");
        progress.equippedSwordHeart="HE01";
        Check(mods.RangedCost==5.25f,"HE01 applies its source 25 percent ranged cost reduction");
        Check(Mathf.Abs(mods.RangedDamage(40)-70)<.001f,"HE01 zero-value multiplier increases ranged strength");
        progress.equippedSwordHeart="HE201";Check(mods.DamageDealt(18)==36,"HE201 doubles strength without requiring an additive value");
        progress.equippedSwordHeart="HE11";Check(Mathf.Abs(mods.ParryWindow-.225f)<.001f,"HE11 multiplies the source parry window");
        progress.equippedSwordHeart="HE04";Check(Mathf.Abs(mods.DamageDealt(20)-26)<.001f&&mods.DamageTaken(10)==20,"HE04 applies both increased damage and its defense drawback");
        progress.equippedSwordHeart="HE03";game.player.health=88;Check(mods.DamageDealt(18)==18,"Conditional Heart stays inactive above its life threshold");
        game.player.health=10;Check(mods.DamageDealt(18)==26,"Conditional Heart activates below twenty percent life");
        progress.equippedSwordHeart="";progress.equippedRosaryBeads=new[]{"RB201"};game.player.flasks=1;
        Check(mods.DamageTaken(10)==10,"Empty-flask defense bead stays inactive while a flask remains");
        game.player.flasks=0;Check(Mathf.Abs(mods.DamageTaken(10)-7.76f)<.001f,"Empty-flask bead applies source damage reduction followed by flat Defense");
        progress.equippedRosaryBeads=Array.Empty<string>();progress.meaCulpaLevel=7;progress.tears=200000;
        Check(!progress.IsEquipped("COMBO_1")&&!progress.IsEquipped("VERTICAL_1"),"Unpurchased added skills are not implicitly equipped");
        foreach(string prefix in new[]{"COMBO_","VERTICAL_"})
        {
            Check(!game.PurchaseInventorySkill(catalog.Find(prefix+"3")),prefix+"third upgrade requires its parent");
            for(int tier=1;tier<=3;tier++)
            {
                var item=catalog.Find(prefix+tier);float before=progress.tears;
                Check(game.PurchaseInventorySkill(item)&&progress.IsEquipped(item.id)&&progress.tears==before-item.skillCost,item.id+" purchase uses source cost, tier and ownership");
                Check(!game.PurchaseInventorySkill(item)&&progress.tears==before-item.skillCost,item.id+" cannot be bought twice");
            }
        }
        SaveChecks();
        bool shake=GameSettings.ScreenShake;
        try{GameSettings.ScreenShake=false;game.Shake(.5f);Check((float)typeof(BrotherhoodGame).GetField("shake",Private).GetValue(game)==0,"Disabled screen shake is applied in gameplay");}
        finally{GameSettings.ScreenShake=shake;}
    }
    void SaveChecks()
    {
        string path=Path.Combine(Application.persistentDataPath,"brotherhood-p2-file-test.json");
        Func<string,bool> valid=text=>text=="first"||text=="second";
        foreach(string file in new[]{path,path+".bak",path+".tmp"})File.Delete(file);
        try
        {
            SafeSaveFile.Write(path,"first",valid);SafeSaveFile.Write(path,"second",valid);
            Check(File.ReadAllText(path)=="second"&&File.ReadAllText(path+".bak")=="first"&&!File.Exists(path+".tmp"),"Safe replacement preserves a valid previous save");
            File.WriteAllText(path,"corrupt");
            Check(SafeSaveFile.TryRead(path,valid,out string json,out bool recovered)&&recovered&&json=="first","Unreadable primary recovers from a validated backup");
            SafeSaveFile.Write(path,"second",valid);Check(File.ReadAllText(path+".bak")=="first","Saving after corruption does not overwrite the valid backup with corrupt data");
            bool rejected=false;try{SafeSaveFile.Write(path,"invalid",valid);}catch(InvalidDataException){rejected=true;}
            Check(rejected&&File.ReadAllText(path)=="second","Invalid save payload is rejected before altering the primary");
        }
        finally{foreach(string file in new[]{path,path+".bak",path+".tmp"})File.Delete(file);}
    }
    void Set(object target,string field,object value){target.GetType().GetField(field,Private).SetValue(target,value);}
    object Call(object target,string method,params object[] arguments)=>target.GetType().GetMethod(method,Private).Invoke(target,arguments);
    void Step(float seconds)
    {
        for(float left=seconds;left>.00001f;){float dt=Mathf.Min(1f/120,left);game.player.Tick(dt);game.player.actor.Advance(dt);left-=dt;}
    }
    void Place(Vector2 point)
    {
        game.controls.ClearGameplayInput();game.player.Restore();game.player.motor.Teleport(point);Step(.6f);Time.timeScale=0;
    }
    void GameplayChecks()
    {
        game.progress=new PlayerProgress();game.Enter("D17Z01S02",null,null);
        var enemies=game.Current.enemies;var enemy=enemies[0];foreach(var other in enemies)other.health=0;
        Vector2 floor=game.Current.start.position;Place(floor);floor=game.player.transform.position;
        game.progress.comboTier=1;Call(game.player,"StartAttack",3,0f);
        Check(game.player.actor.Current=="penitent_ParryStab_old_anim","First combo upgrade uses the source center-finisher motion");
        Place(floor);game.progress.comboTier=2;Call(game.player,"StartAttack",3,1f);
        Check(game.player.actor.Current=="Player_ComboFinisher_Up","Second combo upgrade selects the source upward finisher");
        Place(floor);game.progress.comboTier=2;Call(game.player,"StartAttack",3,-1f);
        Check(game.player.actor.Current=="penitent_ParryStab_old_anim","Down finisher remains unavailable before combo tier three");
        Place(floor);game.progress.comboTier=3;Call(game.player,"StartAttack",3,-1f);
        Check(game.player.actor.Current=="Player_ComboFinisher_Down"&&game.player.attackTime>1.65f,"Third combo upgrade preserves the complete 1.70-second downward finisher");
        for(int tier=1;tier<=3;tier++)
        {
            Place(floor);game.progress.verticalTier=tier;enemy.health=1000;enemy.motor.Teleport(floor+new Vector2(tier==1?.8f:2.2f,0));
            game.player.motor.Teleport(floor+Vector2.up*5);game.player.motor.velocity=Vector2.down;game.controls.Down=game.controls.AttackHeld=true;
            Step(.24f);Check(!game.player.VerticalCasting,"VERTICAL_"+tier+" waits for the source hold threshold");
            Step(.12f);Check(game.player.VerticalCasting&&game.player.actor.Current=="penitent_verticalattack_start_anim","VERTICAL_"+tier+" starts from actual held airborne input");
            game.controls.ClearGameplayInput();Step(1.2f);
            Check(!game.player.VerticalCasting&&enemy.health==946,"VERTICAL_"+tier+" lands and deals source three-times-strength area damage");enemy.health=0;
        }
        game.progress.verticalTier=0;game.progress.comboTier=0;
        foreach(string id in new[]{"PR101","PR201","PR202","PR203","PR07","PR09","PR10"})
            Check(game.player.actor.catalog.Find(id=="PR101"?"auroraGuardia_attack":id=="PR201"?"miriamPortal_attack":id=="PR202"?"penitent_pr202":id=="PR203"?"SanTelmoLightning_anim":id=="PR07"?"threeAnguish_spear_attackLine":id=="PR09"?"pontiff_lightningBolt_attack":"Player_Clamped_Attack_NoSlashes_1")!=null,id+" required source timeline loads from the supplemental or main catalog");
        Brotherhood.AdditionalPrayerChecks.Run(game,Check,Step,Place);
        game.Enter("D17Z01S02",null,null);enemies=game.Current.enemies;enemy=enemies[0];foreach(var other in enemies)other.health=0;Place(game.Current.start.position);floor=game.player.transform.position;
        Place(floor);game.progress.equippedPrayer="PR10";game.progress.hasPrayer=true;game.player.fervour=60;enemy.health=1000;enemy.motor.Teleport(floor+Vector2.right*1.5f);
        game.controls.Prayer=true;Step(1f/120);game.controls.Prayer=false;Step(.56f);
        Check(enemy.health==1000&&game.player.fervour==0,"Solea consumes sixty and activates a melee buff without an immediate area strike");
        Check(new InventoryModifiers(game.progress,game.player).DamageDealt(18)==45,"Solea applies its source strength multiplier 2.5");
        Step(10.1f);Check(new InventoryModifiers(game.progress,game.player).DamageDealt(18)==18,"Solea removes its multiplier after ten seconds");enemy.health=0;
        Place(floor);game.progress.equippedPrayer="";game.progress.hasPrayer=false;game.progress.tears=100;
        int signals=0;Action<string,string> listener=(name,value)=>{if(name=="PENANCE")signals++;};game.events.Raised+=listener;
        game.controls.SpecialHeld=true;Step(.3f);game.controls.SpecialHeld=false;Step(2);game.events.Raised-=listener;
        Check(signals==1&&game.progress.penanceUses==1,"One held Penance broadcasts and persists exactly one framework PENANCE signal");
        Place(floor);game.progress.equippedRosaryBeads=new[]{"RB101"};game.player.fervour=0;Step(2.1f);
        Check(game.player.fervour==2,"Update-event bead restores Fervour on its one-second source interval");
        Place(floor);game.progress.equippedRosaryBeads=new[]{"RB202"};game.controls.Parry=true;Step(.05f);game.controls.Parry=false;
        Check(game.player.Damage(10,game.player.transform.position.x-game.player.facing),"Bidirectional parry bead allows a valid guard from behind");
        game.progress.equippedRosaryBeads=Array.Empty<string>();foreach(var other in enemies)other.ResetEnemy();
    }
    void MapAndGuiltChecks()
    {
        game.progress=new PlayerProgress();game.Enter("D17Z01S02",null,null);var source=SourceMap.Load();var cells=source.RoomCells(game.Current.id);
        Check(source.cells.Length==993&&source.cells.Count(c=>!c.ignored&&!c.ngPlus)==809,"Imported Cvstodia map retains 993 source cells and 809 counted discovery cells");
        game.progress.discoveredMapCells=Array.Empty<string>();source.Recalculate(game.progress);Check(game.progress.mapPercentage==0,"New exploration starts at zero instead of a fabricated hundred percent");
        var cell=cells[0];Vector2 at=new Vector2(cell.left+1,cell.bottom+1);
        Check(source.Discover(game.progress,cell.room,at)&&Mathf.Abs(game.progress.mapPercentage-1f/809)<.000001f,"Entering a real source cell adds its exact share of world discovery");
        Check(!source.Discover(game.progress,cell.room,at)&&game.progress.discoveredMapCells.Length==1,"Repeated visits do not duplicate discovery");
        Check(source.At(cell.room,new Vector2(cell.left+cell.width,cell.bottom+1))?.key==cells[1].key,"Cell boundary crossing selects the next source cell without overlap");
        var map=game.controls.mapUI;Call(map,"RefreshMap");Set(map,"selectedPinType",2);Call(map,"OnClickRoom",cell.room,cell.MapPosition,cell.key);
        string path=Path.Combine(Application.persistentDataPath,"brotherhood-verification.json");
        Check(game.progress.mapPins.Count==1&&File.ReadAllText(path).Contains("\"cellKey\":\""+cell.key+"\""),"Placing a map pin immediately saves its exact source-cell key");
        Call(map,"OnClickRoom",cell.room,cell.MapPosition,cell.key);
        Check(game.progress.mapPins.Count==0&&!File.ReadAllText(path).Contains("\"cellKey\":\""+cell.key+"\""),"Removing a pin immediately persists its removal");
        foreach(var other in game.Current.enemies)other.health=0;
        Place(game.Current.start.position);game.guilt.RememberFloor();game.player.Damage(999,game.player.transform.position.x+1,false);
        Check(game.progress.guiltDrops.Count==1&&game.player.Dead,"A lethal gameplay hit creates one persisted Guilt fragment");
        float reduced=game.player.MaxFervour;Check(reduced<60&&new InventoryModifiers(game.progress,game.player).FervourGain(4)<4,"Guilt uses source curves to reduce maximum and gain of Fervour");
        game.player.Restore();Step(.6f);game.guilt.RememberFloor();game.guilt.OnPlayerDeath();
        Check(game.progress.guiltDrops.Count==2&&game.progress.guiltDrops[0].group==game.progress.guiltDrops[1].group,"Nearby deaths link fragments using the source twenty-unit distance");
        game.player.health=10;game.player.fervour=0;game.player.motor.grounded=true;
        Check(game.guilt.TryRecover()&&game.progress.guiltDrops.Count==0&&Mathf.Abs(game.player.health-39.04f)<.01f&&game.player.fervour==30,"Recovering a linked fragment restores all linked guilt, 33 percent Life and 50 percent Fervour");
        Place(game.Current.start.position);game.progress.equippedRosaryBeads=new[]{"RB38"};game.progress.SetOwned("RB38",true);game.player.Damage(999,game.player.transform.position.x+1,false);
        Check(game.progress.guiltDrops.Count==0&&game.progress.IsEquipped("RB39")&&game.progress.Owns("RB39")&&!game.progress.Owns("RB38"),"Immaculate Bead prevents Guilt and advances exactly one death stage");
        Check(File.ReadAllText(path).Contains("RB39"),"Changed death-bead ownership and equipment are saved after the death event");
        game.player.Restore();game.progress.equippedRosaryBeads=Array.Empty<string>();game.progress.tears=111;game.SaveGame();game.progress.tears=222;game.SaveGame();File.WriteAllText(path,"broken-json");game.progress=new PlayerProgress();Call(game,"LoadSave");
        Check(game.progress.tears==111,"Gameplay LoadSave recovers actual progression from the valid previous backup");
    }
    void AudioChecks()
    {
        foreach(string key in new[]{"INVENTORY_OPEN","INVENTORY_CLOSE","INVENTORY_EQUIP","INVENTORY_SCROLL","ITEM_ADDED","GUILT_RECOVER","VERTICAL_ATTACK_START","VERTICAL_ATTACK_LV2","VERTICAL_ATTACK_LV3"})Check(game.audioBank.Has(key),key+" has a source-bank sample");
        foreach(string room in new[]{"D17Z01S02","D01Z01S01","D01Z02S01"})
        {
            game.audioBank.EnterRegion(room);var music=(AudioSource)typeof(RestoredAudio).GetField("music",Private).GetValue(game.audioBank);var ambient=(AudioSource)typeof(RestoredAudio).GetField("ambience",Private).GetValue(game.audioBank);
            Check(music.clip!=null&&ambient.clip!=null&&ambient.loop,room+" selects its regional music and looped ambience");
        }
        bool old=game.audioBank.SfxMuted;game.audioBank.SetSfxMuted(true);var source=(AudioSource)typeof(RestoredAudio).GetField("ambience",Private).GetValue(game.audioBank);Check(source.mute,"SFX mute also silences environmental ambience");game.audioBank.SetSfxMuted(old);
    }
    void AdditionalPrayerChecks()
    {
        game.progress=new PlayerProgress();game.Enter("D17Z01S02",null,null);foreach(var enemy in game.Current.enemies)enemy.health=0;
        Place(game.Current.start.position);Vector2 floor=game.player.transform.position;game.progress.rangedTier=1;game.progress.hasSpecial=true;game.progress.specialMode=1;game.progress.equippedSwordHeart="HE01";game.player.fervour=30;
        game.controls.Special=true;Step(.02f);game.controls.Special=false;Check(Mathf.Abs(game.player.fervour-24.75f)<.001f,"Actual ranged input charges precisely 5.25 with HE01");
        Place(floor);game.progress.rangedTier=0;game.progress.equippedSwordHeart="";game.progress.equippedPrayer="PR09";game.progress.hasPrayer=true;game.player.fervour=60;
        var target=game.Current.enemies[0];target.health=1000;target.motor.Teleport(floor+new Vector2(1.25f,14));game.controls.Prayer=true;Step(.02f);game.controls.Prayer=false;Step(.56f);
        Check(game.player.fervour==20&&target.health==980,"Taranto charges forty and reaches a target fourteen units above ground using its source twenty-unit collider");
        Step(.14f);Check(target.health<=960,"Taranto applies repeated hits at the source 0.12-second tick interval");Step(2);Check(game.prayerEffects.ActiveActors==0,"Taranto cleans up columns at the end of its effect");target.health=0;
        game.Enter("D17Z01S05",null,null);Place(game.Current.checkpoints[0].position);game.Interact(false,true);string checkpoint=game.Current.id;
        game.Enter("D17Z01S02",null,null);Place(game.Current.start.position);game.progress.equippedPrayer="PR202";game.player.health=30;game.player.flasks=1;game.player.fervour=60;game.controls.Prayer=true;Step(.02f);game.controls.Prayer=false;Step(.56f);
        Check(game.Current.id=="D17Z01S02"&&game.player.fervour==20,"PR202 charges forty and waits before teleporting");Step(.35f);
        Check(game.Current.id==checkpoint&&game.player.health==30&&game.player.flasks==1&&game.player.fervour==20,"PR202 returns to the activated Prie Dieu without granting free healing, flasks or Fervour");
    }
    void WorldChecks()
    {
        game.progress=new PlayerProgress();game.Enter("D17Z01S05",null,null);
        Check(game.rooms.Length==16,"Supplemental prefabs add four rooms while retaining all twelve saved-scene rooms");
        foreach(var expected in new[]{new{room="D17Z01S04",count=246},new{room="D17Z01S07",count=307},new{room="D17Z01S08",count=145},new{room="D17Z01S09",count=171}})
        {var room=game.Find(expected.room);Check(room!=null&&room.GetComponentsInChildren<SourceObjectId>(true).Length==expected.count,expected.room+" preserves every authored node in its isolated prefab");}
        var door=game.Current.Door("S");Check(door!=null&&!game.world.DoorAllowed(door),"S05 shortcut remains locked before the source S04 switch flag");
        game.Enter("D17Z01S04",null,null);var receiver=game.world.Node("LOGIC_188");Check(receiver!=null,"S04 restores the exact original shortcut SlashReceiver");
        if(receiver!=null)
        {
            Check(!game.world.Strike(new Bounds(receiver.transform.position+Vector3.right*5,Vector3.one))&&!game.progress.HasFlag("LEVEL_FLAGS/D17Z01S04_SHORTCUT"),"A strike outside the S04 sensor does not unlock the shortcut");
            Check(game.world.Strike(new Bounds(receiver.transform.position,Vector3.one))&&game.progress.HasFlag("LEVEL_FLAGS/D17Z01S04_SHORTCUT"),"Striking the S04 receiver persists its original shortcut flag");
        }
        game.Enter("D17Z01S05",null,null);Check(game.world.Node("LOGIC_105").gameObject.activeSelf&&!game.world.Node("LOGIC_133").gameObject.activeSelf&&game.world.DoorAllowed(game.Current.Door("S")),"Returning to S05 opens only the original GateOpen ladder group and allows its south door");
        game.Enter("D17Z01S08",null,null);Check(game.Current.skillAltars.Length==1,"Supplemental S08 contains the second source Mea Culpa altar");
        Place(game.Current.skillAltars[0].sensor.position);game.player.motor.grounded=true;Check(game.TryUseSkillAltar()&&game.progress.meaCulpaLevel==1&&game.progress.strengthUpgrades==1,"Source altar upgrades Mea Culpa and Strength together");
        game.TryUseSkillAltar();Check(game.progress.meaCulpaLevel==1&&game.progress.strengthUpgrades==1,"Revisiting an altar cannot grant another Strength or Mea Culpa upgrade");game.controls.inventoryUI.Hide();game.player.Restore();
        game.progress=new PlayerProgress();game.Enter("D17Z01S01",null,null);Check(game.world.ActiveCherubs==1,"The entry room restores its source cherub instead of silently excluding its spawn");
        game.world.Tick(.1f);var cherub=Array.Find(game.Current.GetComponentsInChildren<SpriteActor>(),a=>a.name=="RESCUED_CHERUB_06");
        if(cherub!=null){Check(game.world.Strike(new Bounds(cherub.transform.position+Vector3.down*.96f,new Vector3(1,3,2)))&&game.progress.cherubsFreed==1,"Hitting the captive frees one cherub and saves its original persistent id");game.world.Strike(new Bounds(cherub.transform.position,Vector3.one*4));Check(game.progress.cherubsFreed==1,"Further hits cannot count a rescued cherub twice");}
        else Check(false,"Source cherub actor is present");
        game.Enter("D17Z01S02",null,null);game.Enter("D17Z01S01",null,null);Check(game.world.ActiveCherubs==0,"A rescued cherub stays absent after leaving and re-entering");
        game.Enter("D01Z01S03",null,null);Check(game.world.ActiveCherubs==1,"The forest restores its separately configured five-second cherub path");
        game.Enter("D01Z02S02",null,null);var tirso=Array.Find(game.Current.GetComponentsInChildren<SourceObjectId>(true),n=>n.name=="Tirso");game.player.Restore();game.player.motor.Teleport(tirso.transform.position);game.player.motor.grounded=true;
        var dialogue=game.world.Dialogue;game.progress.SetOwned("QI19",true);Check(dialogue.TryInteract()&&dialogue.IsOpen&&game.InputBlocked,"Actual Tirso interaction opens source dialogue and blocks gameplay input");dialogue.Close();Check(game.progress.Owns("QI19")&&!game.InputBlocked,"Closing a quest dialogue preserves the offered item and restores input");
        Check(dialogue.DeliverHerb("QI19")&&!game.progress.Owns("QI19")&&game.progress.Owns("QI66"),"Tirso consumes the first herb and rewards the source empty flask QI66");game.itemPopup.Hide();
        Check(!dialogue.DeliverHerb("QI19"),"A delivered herb cannot be rewarded twice");float tears=game.progress.tears;
        for(int i=1;i<SourceNpcDialogue.Herbs.Length;i++){game.progress.SetOwned(SourceNpcDialogue.Herbs[i],true);Check(dialogue.DeliverHerb(SourceNpcDialogue.Herbs[i]),"Tirso accepts source herb "+SourceNpcDialogue.Herbs[i]);game.itemPopup.Hide();}
        Check(game.progress.Owns("QI56")&&game.progress.tears-tears==18500,"All six herbs grant Tirso's source knot and ordered Tears rewards");
        game.Enter("D17Z01S09",null,null);var soledad=game.world.Node("LOGIC_59");game.player.Restore();game.player.motor.Teleport(soledad.transform.position);game.player.motor.grounded=true;
        for(int i=0;i<SourceNpcDialogue.Knots.Length;i++){var id=SourceNpcDialogue.Knots[i];game.progress.SetOwned(id,true);Check(dialogue.ExchangeKnot(id)&&game.progress.rosarySlots==3+i&&!game.progress.Owns(id),"Soledad consumes "+id+" and adds precisely one saved rosary slot");}
        Check(game.progress.rosarySlots==8&&game.progress.HasFlag("ST21_JAILED_GHOST/JAILED_QUEST_COMPLETED")&&!dialogue.ExchangeKnot("QI44"),"Eight rosary slots complete Soledad's source quest and prevent further knot consumption");
        game.progress.guiltDrops.Add(new PlayerProgress.GuiltDrop{id="test",group="test",room=game.Current.id});game.bloodyBaptism.Complete();
        Check(game.progress.guiltDrops.Count==0&&game.progress.HasFlag("CUTSCENE/CTS02_COMPLETED"),"Completion of Bloody Baptism removes Guilt and persists the cutscene marker");
        var preview=Resources.Load<TextAsset>("Cutscenes/CTS02-preview");Check(Resources.Load<UnityEngine.Video.VideoClip>("Cutscenes/CTS02")!=null&&preview!=null&&Resources.Load<Texture2D>("Cutscenes/CTS02Preview_10")!=null,"Both original CTS02 video and safe source-frame preview are packaged");
        game.SaveGame();string save=File.ReadAllText(Path.Combine(Application.persistentDataPath,"brotherhood-verification.json"));Check(save.Contains("RESCUED_CHERUB_06")&&save.Contains("JAILED_QUEST_COMPLETED")&&save.Contains("TIRSO_QI65_DELIVERED"),"Cherubs and both NPC quest flags survive save serialization");
    }
    void ItemAndSettingsChecks()
    {
        game.progress=new PlayerProgress();game.Enter("D17Z01S02",null,null);foreach(var enemy in game.Current.enemies)enemy.health=0;Place(game.Current.start.position);Vector2 floor=game.player.transform.position;
        foreach(string prayer in new[]{"PR01","PR03","PR04","PR09","PR11","PR14","PR16"})
        {game.itemEffects.BeginPrayer(prayer);Check(game.itemEffects.HasTemporalFlag(0),prayer+" correctly decodes source enum zero (StopFervourRecolection) from Unity's hex list");Check(game.itemEffects.HasTemporalFlag(1)==Array.Exists(new[]{"PR03","PR09","PR11","PR14"},p=>p==prayer),prayer+" correctly decodes source invulnerability instead of treating YAML octal integers as bit flags");}
        game.itemEffects.BeginPrayer("PR03");game.player.health=50;Set(game.player,"invincible",0f);game.player.Damage(20,game.player.transform.position.x+1,false);Check(game.player.health==50,"Source PR03 temporal invulnerability blocks an actual incoming hit");game.itemEffects.Tick(2.01f);Set(game.player,"invincible",0f);game.player.Damage(20,game.player.transform.position.x+1,false);Check(game.player.health==30,"PR03 removes its invulnerability after its source two seconds");
        Place(floor);game.progress.equippedSwordHeart="HE201";game.progress.SetOwned("HE201",true);Check(game.itemEffects.HasTemporalFlag(5)&&!game.itemEffects.HasTemporalFlag(4),"HE201 means DisableUnEquipSword, while its enum does not suppress Guilt");Check(!game.EquipInventoryItem(InventoryCatalog.Load().Find("HE201"))&&game.progress.equippedSwordHeart=="HE201","HE201 respects its source permanent sword-equip restriction");
        game.progress.equippedSwordHeart="HE06";game.player.health=20;int flasks=game.player.flasks;game.controls.Flask=true;Step(1.1f);game.controls.Flask=false;Check(game.player.health==20&&game.player.flasks==flasks,"HE06 prevents actual flask usage without consuming a flask");
        game.progress.equippedSwordHeart="";game.progress.equippedRosaryBeads=new[]{"RB42"};game.itemEffects.Tick(.1f);Check(game.itemEffects.Familiar!=null&&game.itemEffects.Familiar.visual.sprite!=null,"RB42 summons its original animated familiar and source clip");game.progress.equippedRosaryBeads=Array.Empty<string>();game.itemEffects.Tick(.1f);Check(game.itemEffects.Familiar==null,"Unequipping RB42 disposes its familiar");
        var target=game.Current.enemies[0];for(int equipped=0;equipped<2;equipped++)
        {
            Place(floor);game.progress.equippedRosaryBeads=equipped==0?Array.Empty<string>():new[]{"RB21"};game.player.motor.Teleport(floor+Vector2.up*5);game.player.motor.grounded=false;target.motor.Teleport(floor+Vector2.up*5+Vector2.right);target.health=1000;game.controls.Down=true;
            for(int hit=0;hit<4;hit++){game.player.motor.velocity=Vector2.down;Call(game.player,"StartAttack",0,-1f);Call(game.player,"HitEnemies");Check((game.player.motor.velocity.y==6)==(hit<(equipped==0?2:3)),"Air impulse "+(hit+1)+" respects "+(equipped==0?"source base two":"RB21 source bonus third")+" strikes per airborne sequence");}
        }
        game.controls.ClearGameplayInput();target.health=0;game.progress.equippedRosaryBeads=new[]{"RB15"};Check(game.itemEffects.AnimationSpeed("HardLandingBeadEffect")==3,"RB15 restores source HardLanding recovery speed three");
        game.progress.equippedRosaryBeads=new[]{"RB05"};Place(floor);game.player.fervour=0;game.player.ItemEvent(7);Check(game.player.fervour==3,"Breakable event seven applies the source three-Fervour bead reward");
        game.progress.equippedRosaryBeads=new[]{"RB28"};Place(floor);game.player.health=20;game.controls.Flask=true;Step(.01f);game.controls.Flask=false;Set(game.player,"invincible",0f);game.player.Damage(10,floor.x+1,false);game.player.DamageContact(10,floor.x+1);Check(game.player.health==20,"RB28 blocks both incoming attack and contact damage during actual flask animation");Step(1.1f);float afterHealing=game.player.health;Set(game.player,"invincible",0f);game.player.Damage(5,floor.x+1,false);Check(game.player.health==afterHealing-5,"RB28 protection ends with the flask animation");
        game.progress.equippedRosaryBeads=new[]{"RB103"};Place(floor);game.player.fervour=0;int manaFlasks=game.player.flasks;game.controls.Flask=true;Step(.01f);game.controls.Flask=false;Step(1.1f);Check(game.player.health==game.player.MaxHealth&&game.player.fervour==40&&game.player.flasks==manaFlasks-1,"RB103 converts one actual flask into Fervour and allows use at full Life");
        game.progress.equippedRosaryBeads=new[]{"RB102"};Place(floor);game.player.health=20;game.controls.Flask=true;Step(.01f);game.controls.Flask=false;Step(1.1f);float regenerationStart=game.player.health;game.itemEffects.Tick(2);Check(regenerationStart>=33.2f&&Mathf.Abs(game.player.health-regenerationStart-2)<.001f,"RB102 uses its reduced flask heal then restores one Life per second at the source base flask level");Set(game.player,"invincible",0f);game.player.Damage(5,floor.x+1,false);float wounded=game.player.health;game.itemEffects.Tick(2);Check(game.player.health==wounded,"An actual incoming hit cancels RB102 passive flask regeneration");
        game.progress.equippedRosaryBeads=new[]{"RB16"};Place(floor);target.ResetEnemy();target.motor.Teleport(floor+Vector2.right);target.motor.grounded=true;game.player.motor.grounded=true;target.StunEnemy(5);game.player.flasks=0;int rewardSeed=1;for(;rewardSeed<1000;rewardSeed++){UnityEngine.Random.InitState(rewardSeed);if(UnityEngine.Random.Range(0,100)<20)break;}UnityEngine.Random.InitState(rewardSeed);Check(game.player.TryExecution()&&game.player.flasks==1,"RB16 receives execution context from a real kill and grants its source probabilistic flask reward");
        Place(floor);target.ResetEnemy();target.motor.Teleport(floor+Vector2.right);game.player.flasks=0;UnityEngine.Random.InitState(rewardSeed);target.Damage(999);Check(game.player.flasks==0,"The same RB16 chance does not grant execution or heavy rewards for an ordinary kill");
        Place(floor);target.ResetEnemy();game.player.flasks=0;for(rewardSeed=1;rewardSeed<1000;rewardSeed++){UnityEngine.Random.InitState(rewardSeed);if(UnityEngine.Random.Range(0,100)<10)break;}UnityEngine.Random.InitState(rewardSeed);game.player.HeavyWeaponHit(target,999);Check(game.player.flasks==1,"RB16 receives heavy-weapon kill context without changing EnemyController");
        game.progress.equippedRosaryBeads=Array.Empty<string>();Place(floor);target.ResetEnemy();target.motor.Teleport(floor+Vector2.right*5);target.health=1000;game.progress.SetOwned("PR16",true);game.progress.equippedPrayer="PR16";game.progress.hasPrayer=true;game.player.fervour=60;game.controls.Prayer=true;Step(.01f);game.controls.Prayer=false;Step(.56f);Check(game.player.ActivePrayer=="PR16"&&target.health==1000,"Casting PR16 activates its Tears buff without an invented generic AoE attack");game.progress.tears=0;game.EnemyDefeated(10);Check(Mathf.Abs(game.progress.tears-13)<.001f,"PR16 source thirty-percent Tears bonus is applied once through the shared stat pipeline");Step(new InventoryModifiers(game.progress,game.player).PrayerDuration+.1f);game.EnemyDefeated(10);Check(Mathf.Abs(game.progress.tears-23)<.001f,"Expired PR16 does not retain its Tears bonus");target.health=0;
        bool existed=PlayerPrefs.HasKey("BrotherhoodHowToPlay");int before=PlayerPrefs.GetInt("BrotherhoodHowToPlay",1);game.progress=new PlayerProgress();game.Enter("D17Z01S05",null,null);var tutorial=game.world.Node("LOGIC_121");game.player.motor.Teleport(tutorial.transform.position);game.player.motor.grounded=true;game.controls.Simulation=false;
        try{PlayerPrefs.SetInt("BrotherhoodHowToPlay",0);game.world.Tick(.01f);Check(!game.world.Dialogue.IsOpen,"Disabled HowToPlay suppresses actual source tutorial sensors");PlayerPrefs.SetInt("BrotherhoodHowToPlay",1);game.world.Tick(.01f);Check(game.world.Dialogue.IsOpen,"Enabled HowToPlay displays a localized source tutorial at its original sensor");CaptureCanvas(game.world.Dialogue.Canvas,"priority-two-tutorial");game.world.Dialogue.Close();}
        finally{if(existed)PlayerPrefs.SetInt("BrotherhoodHowToPlay",before);else PlayerPrefs.DeleteKey("BrotherhoodHowToPlay");game.controls.Simulation=true;}
        game.progress.discoveredMapCells=SourceMap.Load().cells.Where(cell=>Array.Exists(game.rooms,room=>room.id==cell.room)&&!cell.ngPlus).Select(cell=>cell.key).ToArray();SourceMap.Load().Recalculate(game.progress);game.controls.mapUI.Show();CaptureCanvas(game.controls.mapUI.Canvas,"priority-two-source-map");game.controls.mapUI.Hide();
        game.progress=new PlayerProgress();game.Enter("D17Z01S11",null,null);game.bloodyBaptism.SkipForVerification=false;var play=game.bloodyBaptism.Play();Check(play.MoveNext()&&game.bloodyBaptism.IsPlaying,"Bloody Baptism enters its actual playback sequence before chapter victory");Call(game.bloodyBaptism,"ShowFrame",14f);CaptureCanvas(game.bloodyBaptism.Canvas,"priority-two-bloody-baptism");game.Enter("D17Z01S02",null,null);Check(!game.bloodyBaptism.IsPlaying&&!game.progress.HasFlag("CUTSCENE/CTS02_COMPLETED"),"Changing rooms safely cancels the cutscene without falsely marking it complete");play.MoveNext();game.bloodyBaptism.SkipForVerification=true;
    }
    void CaptureCanvas(Canvas canvas,string name)
    {
        var mode=canvas.renderMode;var cam=canvas.worldCamera;float distance=canvas.planeDistance;bool sorting=canvas.overrideSorting;int layer=canvas.sortingLayerID,order=canvas.sortingOrder;var previous=RenderTexture.active;var target=new RenderTexture(1280,720,24);var image=new Texture2D(1280,720,TextureFormat.RGB24,false);var cameraTarget=game.view.targetTexture;
        try{canvas.renderMode=RenderMode.ScreenSpaceCamera;canvas.worldCamera=game.view;canvas.planeDistance=1;canvas.overrideSorting=true;canvas.sortingLayerName="Canvas UI";canvas.sortingOrder=500;Canvas.ForceUpdateCanvases();game.view.targetTexture=target;game.view.Render();RenderTexture.active=target;image.ReadPixels(new Rect(0,0,1280,720),0,0);image.Apply();Directory.CreateDirectory("Documentation/Previews");File.WriteAllBytes("Documentation/Previews/"+name+".png",image.EncodeToPNG());}
        finally{canvas.renderMode=mode;canvas.worldCamera=cam;canvas.planeDistance=distance;canvas.overrideSorting=sorting;canvas.sortingLayerID=layer;canvas.sortingOrder=order;game.view.targetTexture=cameraTarget;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(image);}
    }
}
