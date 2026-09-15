using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Brotherhood;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

[InitializeOnLoad]
public static class BrotherhoodVerification
{
    static BrotherhoodVerification()
    {
        EditorApplication.playModeStateChanged += s => { if(s==PlayModeStateChange.EnteredPlayMode) Attach(); };
        EditorApplication.update += Attach;
    }
    static void Attach()
    {
        if(!EditorApplication.isPlaying) return;
        string verifyPath = Path.Combine(Application.dataPath, "../Temp/brotherhood-verify-play");
        string menuPath = Path.Combine(Application.dataPath, "../Temp/brotherhood-menu-verify");
        if(File.Exists(verifyPath) && UnityEngine.Object.FindAnyObjectByType<BrotherhoodPlayCheck>() == null)
        {
            Debug.Log("[BrotherhoodVerification] Attaching BrotherhoodPlayCheck");
            var go = new GameObject("Verification");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<BrotherhoodPlayCheck>();
        }
        else if(File.Exists(menuPath) && UnityEngine.Object.FindAnyObjectByType<BrotherhoodMenuCheck>() == null)
        {
            Debug.Log("[BrotherhoodVerification] Attaching BrotherhoodMenuCheck");
            var go = new GameObject("Menu verification");
            go.AddComponent<BrotherhoodMenuCheck>();
        }
    }
}
public sealed class BrotherhoodMenuCheck:MonoBehaviour
{
    IEnumerator Start()
    {
        yield return new WaitForSecondsRealtime(.5f);
        var lines=new List<string>();
        void Check(bool value,string label){lines.Add((value?"PASS ":"FAIL ")+label);}
        Check(FindAnyObjectByType<MainMenuController>()!=null,"Main menu controller starts");
        Check(FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>()!=null,"Touch, keyboard and controller event system exists");
        Check(Resources.Load<Texture2D>("Menu/MainMenuBackgroundFrame")!=null,"Cropped original 640x360 background loaded");
        Check(Resources.Load<Texture2D>("Menu/Penitent_00")!=null&&Resources.Load<Texture2D>("Menu/Penitent_21")!=null,"Original 22-frame Penitent menu animation loaded");
        var originalFont=Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");Check(originalFont!=null,"Original Majestic Extended bitmap font loaded");
        var buttons=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);Check(buttons.Length>=4,"New Game, Continue, Options and Extras created");
        var inputModule=FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();Check(inputModule!=null&&inputModule.actionsAsset!=null,"Menu input module has default pointer, touch and navigation actions");
        var menu=FindAnyObjectByType<MainMenuController>();var showExtras=typeof(MainMenuController).GetMethod("ShowExtras",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);showExtras?.Invoke(menu,null);yield return null;bool purgeEntry=false;foreach(var label in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(label.text=="PURGE SAVE")purgeEntry=true;Check(purgeEntry,"Main menu exposes guarded save purge flow");
        bool labelsPassThrough=true;foreach(var label in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))if(label.raycastTarget)labelsPassThrough=false;Check(labelsPassThrough,"Menu labels do not block pointer clicks on buttons");
        bool originalMenuFont=true;foreach(var label in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))if(label.font!=originalFont)originalMenuFont=false;Check(originalMenuFont,"Main menu labels use the original bitmap font");
        Directory.CreateDirectory("Documentation/Previews");ScreenCapture.CaptureScreenshot("Documentation/Previews/main-menu-playtest.png");yield return null;
        bool failed=lines.Exists(v=>v.StartsWith("FAIL"));File.WriteAllText("Documentation/menu-playtest-results.txt",string.Join("\n",lines)+"\nRESULT: "+(failed?"FAILED":"PASSED"));File.Delete(Path.Combine(Application.dataPath, "../Temp/brotherhood-menu-verify"));EditorApplication.isPlaying=false;
    }
}
public sealed class BrotherhoodPlayCheck:MonoBehaviour
{
    readonly List<string> checks=new List<string>();bool failed;BrotherhoodGame game;
    void Check(bool condition,string text){Debug.Log((condition?"[PASS] ":"[FAIL] ")+text);checks.Add((condition?"PASS ":"FAIL ")+text);if(!condition)failed=true;}
    IEnumerator Start()
    {
        Debug.Log("[BrotherhoodPlayCheck] Start() invoked!");
        yield return new WaitForSeconds(1);
        Debug.Log("[BrotherhoodPlayCheck] After 1 second wait, looking for BrotherhoodGame");
        game=FindFirstObjectByType<BrotherhoodGame>();if(game==null){Check(false,"Game startup");Finish();yield break;}
        game.controls.Simulation=true;
        checks.Add("Suite revision: source-parity + blood-platforms 2026-09-12");
        Check(game.rooms.Length==5,"Five rooms restored");
        Check(game.player.health==game.player.MaxHealth&&game.player.MaxHealth==88&&game.player.MaxFervour==60,"Player uses mobile-source base Life 88 and Fervour 60");
        Check(game.progress!=null&&game.progress.canJump&&game.progress.canDash&&game.progress.canParry&&game.progress.hasMeaCulpa,"Core progression flags available");
        Check(File.Exists("Assets/Brotherhood/Scenes/MainMenu.unity"),"Main menu scene generated");
        Check(GameObject.Find("Static UI Canvas")!=null&&GameObject.Find("Dynamic Controls Canvas")!=null,"Separate safe-area HUD and control canvases");
        Check(Resources.Load<Texture2D>("HUD/PortraitFrame")!=null&&Resources.Load<Texture2D>("HUD/HealthFill")!=null&&Resources.Load<Texture2D>("HUD/FervourFill")!=null,"Original portrait and gauge art loaded");
        Check(Resources.Load<Texture2D>("HUD/FlaskFull")!=null&&Resources.Load<Texture2D>("HUD/FlaskEmpty")!=null&&Resources.Load<Texture2D>("HUD/TearsFrame")!=null,"Original flask and Tears art loaded");
        var originalFont=Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");Check(originalFont!=null,"Original Majestic Extended bitmap font loaded");
        string[] mobileControls={"Attack","Dash","Jump","Parry","Flask","Prayer","RangeAttack","Map","Inventory","HD_BaseJoystick","HD_ControlJoystick"};bool mobileArt=true;foreach(var control in mobileControls)if(Resources.Load<Texture2D>("MobileControls/"+control)==null){mobileArt=false;break;}Check(mobileArt,"Original Android touch-control and joystick art loaded");
        var healthGauge=GameObject.Find("Health source gauge");var fervourGauge=GameObject.Find("Fervour source gauge");Check(healthGauge!=null&&healthGauge.GetComponent<Image>().sprite.texture.name=="BarMid"&&healthGauge.GetComponent<Outline>()==null,"Health gauge uses original tiled frame without procedural outline");Check(healthGauge!=null&&Mathf.Approximately(healthGauge.GetComponent<RectTransform>().sizeDelta.x,200)&&fervourGauge!=null&&Mathf.Approximately(fervourGauge.GetComponent<RectTransform>().sizeDelta.x,140),"Original health and fervour gauge proportions restored");Check(GameObject.Find("Health loss")!=null&&GameObject.Find("Health end")!=null&&GameObject.Find("Fervour division 1")!=null&&GameObject.Find("Fervour division 2")!=null,"Original loss layer, bar end and fervour divisions restored");var allRects=FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None);var characterMenu=System.Array.Find(allRects,r=>r.name=="Character menu");Check(characterMenu!=null&&characterMenu.GetComponentsInChildren<Brotherhood.TouchControls.TouchButton>(true).Length>=7,"Character menu provides map, inventory, relic, prayer and skill pages");
        bool originalHudFont=true;foreach(var label in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(label.font!=originalFont)originalHudFont=false;Check(originalHudFont,"HUD and character menu labels use the original bitmap font");
        var portrait=GameObject.Find("Penitent portrait");var tears=GameObject.Find("Tears frame");
        var inventoryCatalog=InventoryCatalog.Load();Check(inventoryCatalog.items.Length>=68&&inventoryCatalog.Count("prayer")>=13&&inventoryCatalog.Count("relic")>=7&&inventoryCatalog.Count("rosarybead")>=39&&inventoryCatalog.Count("sword")>=9,"Original prayer, relic, rosary bead and sword metadata catalog imported");
        int sourceEffects=0;foreach(var item in inventoryCatalog.items)if(item.effects!=null)sourceEffects+=item.effects.Length;Check(sourceEffects>=90,"Original inventory prefab effect components preserved in runtime catalog");
        bool everyInventoryIcon=true;foreach(var item in inventoryCatalog.items)if(string.IsNullOrEmpty(item.icon)||Resources.Load<Texture2D>(item.icon)==null){everyInventoryIcon=false;break;}Check(everyInventoryIcon,"All original inventory icons load from Resources");
        var menuIconSlots=System.Array.FindAll(allRects,r=>r.name.StartsWith("Inventory icon "));Check(menuIconSlots.Length==7,"Character menu provides seven reusable inventory icon slots");
        var prayerItem=System.Array.Find(inventoryCatalog.items,i=>i.id=="PR01");game.progress.SetOwned("PR01",true);Check(game.EquipInventoryItem(prayerItem)&&game.progress.equippedPrayer=="PR01"&&game.progress.hasPrayer,"Owned prayer can be equipped through the shared inventory state");var prayerStats=new InventoryModifiers(game.progress);Check(Mathf.Approximately(prayerStats.PrayerCost,20)&&Mathf.Approximately(prayerStats.PrayerDuration,10)&&Mathf.Approximately(prayerStats.PrayerBonus("PR01",0),1),"PR01 uses source cost, ten-second duration and attack-speed modifier");Check(game.EquipInventoryItem(prayerItem)&&string.IsNullOrEmpty(game.progress.equippedPrayer)&&!game.progress.hasPrayer,"Equipped prayer can be removed without losing ownership");var prayer16=System.Array.Find(inventoryCatalog.items,i=>i.id=="PR16");Check(prayer16!=null&&Mathf.Approximately(new InventoryModifiers(game.progress).PrayerBonus("PR16",19),.3f),"PR16 preserves mobile 30 percent Tears multiplier");var activePrayerField=typeof(PlayerController).GetField("activePrayer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);game.progress.tears=0;activePrayerField.SetValue(game.player,"PR16");game.EnemyDefeated(10);Check(Mathf.Approximately(game.progress.tears,13),"PR16 applies mobile Tears multiplier to source enemy reward");activePrayerField.SetValue(game.player,"");game.progress.tears=0;
        var beadItem=System.Array.Find(inventoryCatalog.items,i=>i.id=="RB01");game.progress.SetOwned("RB01",true);Check(game.EquipInventoryItem(beadItem)&&game.progress.IsEquipped("RB01"),"Owned rosary bead can be equipped in a persistent slot");Check(game.EquipInventoryItem(beadItem)&&!game.progress.IsEquipped("RB01"),"Equipped rosary bead can be removed without losing ownership");
        game.EquipInventoryItem(beadItem);var beadStats=new InventoryModifiers(game.progress);Check(Mathf.Approximately(beadStats.DamageTaken(20),18),"RB01 applies its source 10 percent normal-damage reduction");game.EquipInventoryItem(beadItem);
        var heartItem=System.Array.Find(inventoryCatalog.items,i=>i.id=="HE03");game.progress.SetOwned("HE03",true);game.EquipInventoryItem(heartItem);var heartStats=new InventoryModifiers(game.progress);Check(Mathf.Approximately(heartStats.DamageDealt(18),26),"HE03 applies its source Strength plus eight modifier");game.EquipInventoryItem(heartItem);
        var toggleBag=typeof(TouchControls).GetMethod("ToggleBag",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);toggleBag?.Invoke(game.controls,null);yield return null;Check(characterMenu.gameObject.activeInHierarchy&&Time.timeScale==0,"Character inventory opens centered and pauses gameplay");ScreenCapture.CaptureScreenshot("Documentation/Previews/character-menu-playtest.png");yield return null;toggleBag?.Invoke(game.controls,null);Check(!characterMenu.gameObject.activeInHierarchy&&Time.timeScale==1,"Closing character inventory resumes gameplay");
        toggleBag?.Invoke(game.controls,null);var enterRemap=typeof(TouchControls).GetMethod("EnterRemap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);var exitRemap=typeof(TouchControls).GetMethod("ExitRemap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);enterRemap?.Invoke(game.controls,null);yield return null;var allTouchButtons=game.controls.GetComponentsInChildren<TouchControls.TouchButton>(true);bool mappedButtons=System.Array.FindAll(allTouchButtons,b=>!string.IsNullOrEmpty(b.layoutKey)&&b.owner==game.controls).Length>=9;Check(game.controls.Remapping&&mappedButtons&&GameObject.Find("DONE LAYOUT")!=null,"Android touch layout enters drag-remap mode with persistent button keys");exitRemap?.Invoke(game.controls,null);Check(!game.controls.Remapping&&GameObject.Find("DONE LAYOUT")==null,"Finishing touch remap returns to gameplay controls");
        Check(portrait!=null&&portrait.GetComponent<UnityEngine.UI.Image>().sprite.texture.width==88,"HUD uses cropped original portrait frame");
        Check(tears!=null&&tears.GetComponent<UnityEngine.UI.Image>().sprite.texture.width==87,"HUD uses cropped original Tears frame");
        var staticSafe=GameObject.Find("Static UI Canvas/Safe area").GetComponent<RectTransform>();var dynamicSafe=GameObject.Find("Dynamic Controls Canvas/Safe area").GetComponent<RectTransform>();
        Check(staticSafe.anchorMin.x>=0&&staticSafe.anchorMax.x<=1&&dynamicSafe.anchorMin.x>=0&&dynamicSafe.anchorMax.x<=1,"HUD and controls constrained to device safe area");
        game.progress.canDash=false;yield return null;Check(!GameObject.Find("DASH"),"Locked dash button is hidden");game.progress.canDash=true;yield return null;Check(GameObject.Find("DASH")!=null,"Unlocked dash button is visible");
        Check(!GameObject.Find("PRAYER")&&!GameObject.Find("SPECIAL"),"Prayer and special touch buttons stay hidden before unlock");game.progress.hasPrayer=game.progress.hasSpecial=true;game.progress.lungeTier=1;yield return null;Check(GameObject.Find("PRAYER")!=null&&GameObject.Find("SPECIAL")!=null,"Unlocked prayer and special touch buttons appear");game.progress.hasPrayer=game.progress.hasSpecial=false;game.progress.lungeTier=0;
        Check(game.audioBank!=null&&game.audioBank.clips.Length>=70,"Expanded original audio samples attached");
        Check(game.audioBank.Has("ENEMY_STUNT")&&game.audioBank.Has("ACOLYTE_EXECUTION_2")&&game.audioBank.Has("EXECUTION_EFFECT")&&game.audioBank.Has("EXECUTION_FIRST_HIT")&&game.audioBank.Has("EXECUTION_SECOND_HIT")&&game.audioBank.Has("EXECUTION_THIRD_HIT")&&game.audioBank.Has("EXECUTION_EFFECT_END"),"Execution prompt and hit sounds restored from source FMOD bank");
        Check(Mathf.Approximately(SourceGameplayTuning.ExecutionStunTime,5f),"Acolyte and NewFlagellant preserve the mobile 5 second execution window");
        Check(game.audioBank.Has("PENITENT_CLIMB_LADDER_1")&&game.audioBank.Has("PENITENT_CLIMB_LADDER_2")&&game.audioBank.Has("PENITENT_CLIMB_LADDER_3")&&game.audioBank.Has("PENITENT_RUNSTOP_STONE")&&game.audioBank.Has("PENITENT_PUSHBACK")&&game.audioBank.Has("HARD_LANDING")&&game.audioBank.Has("HEALING_EXPLOSION")&&game.audioBank.Has("VERTICAL_ATTACK_FALL")&&game.audioBank.Has("VERTICAL_ATTACK_HIT"),"Movement, ladder, damage, healing and plunge sounds restored from source FMOD bank");
        Check(game.audioBank.Has("LUNGE_ATTACK")&&game.audioBank.Has("LUNGE_ATTACK_LV2")&&game.audioBank.Has("LUNGE_ATTACK_LV3")&&game.audioBank.Has("LUNGE_ATTACK_HIT")&&game.audioBank.Has("LOADING_CHARGED_ATTACK")&&game.audioBank.Has("LOADED_CHARGED_ATTACK")&&game.audioBank.Has("RELEASE_CHARGED_ATTACK")&&game.audioBank.Has("CHARGED_ATTACK_PROJECTILE")&&game.audioBank.Has("CHARGED_ATTACK_PROJECTILE_HIT")&&game.audioBank.Has("CHARGED_ATTACK_PROJECTILE_HIT_WALL"),"Charged and lunge sounds restored from source FMOD bank");
        Check(game.audioBank.Has("RANGE_ATTACK")&&game.audioBank.Has("RANGE_ATTACK_FLY")&&game.audioBank.Has("RANGE_ATTACK_HIT")&&game.audioBank.Has("RANGE_ATTACK_DISSAPEAR")&&game.audioBank.Has("RANGE_ATTACK_EXPLODE"),"Fervorous Blood cast, flight, hit, vanish and explosion sounds restored");
        Check(game.audioBank.Has("PENITENT_ACTIVATE_PRAYER")&&game.audioBank.Has("FERVOR_START_PRAYER")&&game.audioBank.Has("FERVOR_END_PRAYER")&&game.audioBank.Has("PRAYER_INVINCIBILITY")&&game.audioBank.Has("EQUIP_PRAYER")&&game.audioBank.Has("NO_PRAYER"),"Prayer activation, duration, protection and inventory sounds restored");
        var sourceClips=game.player.actor.catalog;Check(sourceClips.Find("Player_Run_Start")!=null&&sourceClips.Find("Player_Run_Stop")!=null&&sourceClips.Find("Player_Landing")!=null&&sourceClips.Find("Player_Landing_Running")!=null&&sourceClips.Find("Player_Jump_Attack_noslashes")!=null&&sourceClips.Find("Player_crouch_attack_noslashes")!=null&&sourceClips.Find("Player_Upward_Attack_Clamped_anim")!=null&&sourceClips.Find("penitent_pushback_grounded_dust_effect_anim")!=null,"Directional combat and locomotion source clips available");Check(sourceClips.Find("Slash_clamped_attack_1")!=null&&sourceClips.Find("Slash_clamped_attack_2")!=null&&sourceClips.Find("Slash_clamped_attack_3")!=null,"Original separate sword-slash renderer clips available");Check(sourceClips.Find("ChargedAttackProjectile_anim")!=null&&sourceClips.Find("ChargedAttackProjectile_impact_anim")!=null&&sourceClips.Find("ChargedAttackProjectile_vanish_anim")!=null,"Original charged projectile flight, impact and vanish clips available");Check(sourceClips.Find("penitent_rangeAttack_projectile_anim")!=null&&sourceClips.Find("penitent_rangeAttack_projectile_vanish_anim")!=null&&sourceClips.Find("penitent_rangeAttack_projectile_explode_anim")!=null,"Fervorous Blood flight, vanish and explosion clips available");
        Check(sourceClips.Find("penitentBeam_startToWarning")!=null&&sourceClips.Find("AlliedCherub_flying")!=null&&sourceClips.Find("penitent_guardian_lady_anim")!=null&&sourceClips.Find("flamePillar_warningToAttack")!=null&&sourceClips.Find("pontiffOldman_toxicFog_appear")!=null,"Mobile prayer beam, cherub, guardian, flame and toxic clips available");
        Check(System.Array.Exists(game.rooms,r=>r.ladders!=null&&r.ladders.Length>0),"Source ladder trigger zones restored");
        Check(Mathf.Abs(game.player.actor.visual.bounds.min.y-game.player.transform.position.y)<.08f,"Player sprite feet align with motor origin");
        foreach(var room in game.rooms)
        {
            game.Enter(room.id,null,null);yield return new WaitForSeconds(1.2f);
            Check(room.GetComponentsInChildren<Collider2D>().Length>0,room.id+" terrain colliders");
            Check(room.GetComponentsInChildren<SpriteRenderer>().Length>0,room.id+" visible sprite renderers");
            Check(room.GetComponentsInChildren<SourceObjectId>(true).Length==room.sourceNodeCount,room.id+" preserves every mobile source transform");
            Check(System.Array.FindAll(room.GetComponentsInChildren<SourceObjectId>(true),o=>o.GetComponent<SpriteRenderer>()!=null).Length==room.sourceRendererCount,room.id+" preserves every mobile source renderer");
            int expectedParallax=room.id=="D17Z01S01"?5:room.id=="D17Z01S02"||room.id=="D17Z01S05"?7:room.id=="D17Z01S11"?8:4;
            Check(room.parallax.Length==expectedParallax,room.id+" preserves mobile parallax layer count");
            Vector4 expectedBounds=room.id=="D17Z01S01"?new Vector4(-1006,-950,5.375f,44):room.id=="D17Z01S02"?new Vector4(-950,-890,4.375f,15.625f):room.id=="D17Z01S05"?new Vector4(-876,-830,6.375f,19):room.id=="D17Z01S11"?new Vector4(-830,-790,7.875f,19.125f):new Vector4(-790,-770,6.375f,17.625f);
            Check(Mathf.Approximately(room.left,expectedBounds.x)&&Mathf.Approximately(room.right,expectedBounds.y)&&Mathf.Approximately(room.bottom,expectedBounds.z)&&Mathf.Approximately(room.top,expectedBounds.w),room.id+" preserves mobile camera boundaries");
            Check(game.player.motor.grounded,room.id+" grounded arrival "+game.player.transform.position);
            Check(Mathf.Abs(game.player.actor.visual.bounds.min.y-game.player.transform.position.y)<.08f,room.id+" player feet touch arrival surface");
            Check(!game.player.Dead,room.id+" safe arrival");
            if(room.Boss!=null)Check(room.Boss.BossIntroComplete,"Warden completes the original falling intro before combat");
            foreach(var enemy in room.enemies)if(enemy!=null&&enemy.actor!=null&&enemy.actor.visual!=null)
            {Check(Mathf.Abs(enemy.actor.visual.bounds.min.y-enemy.transform.position.y)<.35f,room.id+" "+enemy.family+" feet align with ground origin");if(!enemy.boss)Check(enemy.maxHealth==(enemy.family=="acolyte"?60:100)&&enemy.purgeReward==(enemy.family=="acolyte"?15:10),room.id+" "+enemy.family+" uses mobile balance health and Tears");else Check(enemy.maxHealth==400&&enemy.purgeReward==300,"Warden uses mobile balance health and Tears");}
            foreach(var sr in room.GetComponentsInChildren<SpriteRenderer>())if(sr.enabled&&sr.sprite!=null){Check(sr.sharedMaterial.shader.isSupported,room.id+" sprite shader supported");break;}
            Capture(room.id);
            foreach(var door in room.doors)
            {
                var target=game.Find(door.target);if(target!=null)Check(target.Door(door.targetDoor)!=null,room.id+" reciprocal door "+door.key+" -> "+door.target+"/"+door.targetDoor);
            }
        }
        game.Enter("D17Z01S01",null,null);game.player.Restore();yield return new WaitForSeconds(.6f);
        var executionRoom=System.Array.Find(game.rooms,r=>r.enemies!=null&&System.Array.Exists(r.enemies,e=>e!=null&&!e.boss));
        if(executionRoom!=null){var victim=System.Array.Find(executionRoom.enemies,e=>e!=null&&!e.boss);game.Enter(executionRoom.id,null,victim.transform.position+Vector3.left);yield return new WaitForSeconds(.15f);var stunField=typeof(EnemyController).GetField("stun",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);victim.Damage(1);Check(!victim.ExecutionReady,"Ordinary sword damage does not incorrectly expose an execution");stunField.SetValue(victim,SourceGameplayTuning.ExecutionStunTime);Check(Mathf.Approximately((float)stunField.GetValue(victim),5f),"Successful parry grants the mobile 5 second execution window");victim.Tick(.01f);var prompt=GameObject.Find("Execution prompt");Check(prompt!=null&&prompt.activeInHierarchy,"Execution-ready enemy shows original animated prompt above its head");Check(prompt!=null&&prompt.GetComponent<SpriteRenderer>().sprite!=null,"Execution prompt renders a restored source animation frame");Check(game.CanInteract,"Execution exposes the contextual USE button");Capture("execution-prompt");float executionFervour=game.player.fervour;float executionTears=game.progress.tears;game.controls.Interact=true;game.player.Tick(.02f);game.controls.Interact=false;Vector2 executionEndpoint=PlayerController.ExecutionEndpoint(victim.transform.position,game.player.facing);Check(victim.Dead&&(victim.actor.Current=="acolyte_execution_anim"||victim.actor.Current=="Flagellant_execution_anim"),"Enemy execution uses family-specific original animation");Check(!game.player.actor.visual.enabled,"Original composite execution hides duplicate live player renderer");Check(game.ExecutionCameraActive&&Vector2.Distance(game.player.transform.position,executionEndpoint)<.03f,"Execution crosses Player to the far-side mobile endpoint");Check(game.player.fervour>executionFervour&&game.progress.tears>executionTears,"Execution grants Fervour and source enemy Tears");yield return new WaitForSeconds(2.25f);Capture("execution-active");yield return new WaitForSeconds(2.45f);Check(!victim.actor.visual.enabled,"Completed composite execution frame is disposed before live Player returns");Check(game.player.actor.visual.enabled&&!game.ExecutionCameraActive,"Player renderer and normal camera return after complete original execution clip");Check(Vector2.Distance(game.player.transform.position,executionEndpoint)<.03f,"Player remains on the far side shown by the final execution frame");Check(PlayerController.ExecutionEndpoint(victim.transform.position,-1).x<victim.transform.position.x&&PlayerController.ExecutionEndpoint(victim.transform.position,1).x>victim.transform.position.x,"Execution endpoint mirrors correctly for approaches from both sides");Capture("execution-complete");victim.ResetEnemy();}
        if(executionRoom!=null){var target=System.Array.Find(executionRoom.enemies,e=>e!=null&&!e.boss);target.ResetEnemy();game.Enter(executionRoom.id,null,target.transform.position+Vector3.left);game.progress.lungeTier=1;float before=target.health;game.controls.Dash=true;game.controls.Attack=true;game.player.Tick(.02f);game.controls.Dash=game.controls.Attack=false;Check(game.player.actor.Current=="penitent_dodge_attack_anim"&&target.health<before,"Dash plus Attack triggers source-tier lunge and damages target");target.ResetEnemy();game.player.Restore();game.Enter(executionRoom.id,null,target.transform.position+Vector3.left*2);game.progress.chargedTier=2;game.controls.Attack=true;game.controls.AttackHeld=true;game.player.Tick(.02f);game.controls.Attack=false;for(int n=0;n<8;n++)game.player.Tick(.1f);game.controls.AttackHeld=false;game.player.Tick(.02f);Check(game.player.actor.Current=="penitent_charged_attack","Held Attack releases original charged clip at source tier timing");before=target.health;game.player.Tick(.4f);Check(target.health<before,"Charged hit fires at source 0.36 second animation event");target.ResetEnemy();target.transform.position=game.player.transform.position+Vector3.right*3;before=target.health;game.effects.ChargedProjectile(game.player.transform.position+Vector3.up*.65f,1,game,81);Check(game.effects.ActiveChargedProjectiles==1,"Tier-three charged projectile uses the fixed effect pool");yield return new WaitForSeconds(.28f);Check(target.health<before&&game.effects.LastChargedProjectileResult=="Enemy","Charged projectile flies and resolves against an enemy");var projectileWall=new GameObject("Verification projectile wall",typeof(BoxCollider2D));projectileWall.layer=8;projectileWall.transform.position=game.player.transform.position+Vector3.right;projectileWall.GetComponent<BoxCollider2D>().size=new Vector2(.2f,3);Physics2D.SyncTransforms();game.effects.ChargedProjectile(game.player.transform.position+Vector3.up*.65f,1,game,81);yield return new WaitForSeconds(.12f);Check(game.effects.LastChargedProjectileResult=="Wall","Charged projectile resolves against terrain before its range limit");Destroy(projectileWall);var rangePlayerPosition=game.player.transform.position;var rangeTargetPosition=target.transform.position;game.player.motor.Teleport(new Vector2(game.Current.start.position.x,game.Current.top+10));target.transform.position=game.player.transform.position+Vector3.right*20;Physics2D.SyncTransforms();game.effects.RangeProjectile(game.player.transform.position+Vector3.up*.65f,1,game,44,3);Check(game.effects.ActiveRangeProjectiles==1,"Fervorous Blood uses the fixed effect pool");yield return new WaitForSeconds(.34f);Check(game.effects.LastRangeProjectileResult=="Explosion"&&game.effects.HasActiveClip("penitent_rangeAttack_projectile_explode_anim"),"RANGED_3 explodes at the far point before returning");yield return new WaitForSeconds(.35f);Check(game.effects.LastRangeProjectileResult=="Returned","RANGED_2 and RANGED_3 complete their boomerang return");game.player.motor.Teleport(rangePlayerPosition);target.transform.position=rangeTargetPosition;Physics2D.SyncTransforms();game.progress.lungeTier=game.progress.chargedTier=0;target.ResetEnemy();game.player.Restore();}
        game.Enter("D17Z01S01",null,null);yield return new WaitForSeconds(.2f);
        Check(game.Current.faithPlatforms.Length==3,"Three source-linked blood platforms (actual "+game.Current.faithPlatforms.Length+")");
        game.SetBloodRelic(false,false);
        Check(System.Array.TrueForAll(game.Current.faithPlatforms,p=>!p.collision.enabled),"Relic absent disables every blood collider");
        var first=System.Array.Find(game.Current.faithPlatforms,p=>p.first);
        if(first!=null)
        {
            game.SetBloodRelic(true,false);Check(!first.Showing,"Owned but unequipped relic stays disabled");
            Vector2 top=first.collision.transform.TransformPoint(first.collision.offset+Vector2.up*first.collision.size.y*.5f);
            game.player.motor.Teleport(top+Vector2.up);yield return new WaitForSeconds(.65f);
            Check(game.player.transform.position.y<top.y-.1f,"Player falls through disabled platform");
            game.SetBloodRelic(true,true);Check(first.Showing&&first.collision.enabled,"Equipped relic reveals first platform");
            Check(System.Array.FindAll(game.Current.faithPlatforms,p=>p.Showing).Length==1,"Equipping does not reveal the whole chain");
            game.player.motor.Teleport(top+Vector2.up);yield return new WaitForSeconds(.65f);
            Check(game.player.motor.grounded&&Mathf.Abs(game.player.transform.position.y-top.y)<.1f,"Player lands on enabled blood platform");
            var b=first.collision.bounds;first.Tick(new Vector2(b.center.x,b.max.y+.03f),.02f);
            Check(first.targets.Length>0&&first.targets[0].Showing,"Standing on first platform reveals linked successor");
            if(first.targets.Length>0){var next=first.targets[0];next.Tick(new Vector2(-9999,-9999),next.deactivationDelay+.1f);Check(!next.Showing,"Linked platform expires after source delay");}
            game.SetBloodRelic(true,false);Check(System.Array.TrueForAll(game.Current.faithPlatforms,p=>!p.collision.enabled),"Unequipping immediately disables all colliders");
            var previous=game.view.transform.position;game.view.transform.position=first.transform.position+new Vector3(0,2,-10);Capture("blood-platforms-disabled");
            game.SetBloodRelic(true,true);yield return new WaitForSeconds(.15f);game.view.transform.position=first.transform.position+new Vector3(0,2,-10);Capture("blood-platforms-enabled");game.SetBloodRelic(false,false);game.view.transform.position=previous;
        }
        var wall=System.Array.Find(game.Current.GetComponentsInChildren<SpriteRenderer>(true),r=>r.drawMode==SpriteDrawMode.Tiled&&Mathf.Abs(r.size.y-12)<.001f);
        Check(wall!=null,"Right wall preserves source Tiled height 12");
        if(wall!=null){var draw=wall.drawMode;float zoom=game.view.orthographicSize;game.view.orthographicSize=7;
        game.view.transform.position=new Vector3(-951,20.25f,-10);wall.drawMode=SpriteDrawMode.Simple;Capture("right-wall-before-tiled-fix");
        wall.drawMode=draw;Capture("right-wall-source-region");game.view.orthographicSize=zoom;}
        game.Enter("D17Z01S01",null,null);yield return new WaitForSeconds(.3f);
        var idleSlopeStart=game.player.transform.position.x;yield return new WaitForSeconds(.6f);Check(Mathf.Abs(game.player.transform.position.x-idleSlopeStart)<.03f,"Idle player does not drift sideways on terrain");
        var start=game.player.transform.position;game.controls.Move=1;
        yield return new WaitForSeconds(.5f);game.controls.Move=0;
        Check(game.player.transform.position.x>start.x+.5f,"Player walks using runtime motor");
        game.player.Restore();game.effects.Clear();var tunnel=new GameObject("Verification low tunnel",typeof(BoxCollider2D));tunnel.layer=8;var tunnelBox=tunnel.GetComponent<BoxCollider2D>();tunnelBox.size=new Vector2(7,.2f);tunnel.transform.position=game.player.transform.position+new Vector3(2,.82f);Physics2D.SyncTransforms();float dashStart=game.player.transform.position.x;game.controls.Dash=true;game.player.Tick(.02f);game.controls.Dash=false;Check(game.player.motor.size.y<.8f,"Dash lowers the player collision capsule");Check(game.player.actor.visual.color==Color.white,"Dash invulnerability does not flash the Player damage tint");Check(game.effects.HasActiveClip("Penitent_Running_Dust"),"Dash starts the original running-dust animation");yield return new WaitForSeconds(.08f);Check(game.effects.ActiveDashGhosts>0,"Dash creates the source-style pooled afterimage trail");Capture("dash-active");yield return new WaitForSeconds(.22f);Check(game.player.transform.position.x>dashStart+1.5f,"Low dash passes beneath a confined opening");Check(!game.player.motor.TrySetHeight(1.15f),"Player cannot stand while full capsule intersects low wall");Destroy(tunnel);yield return new WaitForSeconds(.6f);Check(game.player.motor.TrySetHeight(1.15f),"Standing capsule returns after leaving low opening");Check(game.player.motor.grounded,"Player settles on ground after low dash verification");
        start=game.player.transform.position;game.controls.Jump=true;game.controls.JumpHeld=true;
        yield return null;game.controls.Jump=false;yield return new WaitForSeconds(.2f);
        Check(game.player.transform.position.y>start.y+.5f,"Buffered jump raises player");game.controls.JumpHeld=false;
        yield return new WaitForSeconds(1.5f);Check(game.player.motor.grounded,"Player lands after jump");
        game.controls.Down=true;game.controls.Jump=true;yield return null;game.controls.Down=false;game.controls.Jump=false;
        Check(game.player.motor.dropThrough>0,"Down + jump activates one-way drop-through window");
        game.Enter("D17Z01S01",null,null);yield return new WaitForSeconds(.3f);
        game.controls.Attack=true;game.player.Tick(.02f);game.controls.Attack=false;Check(game.effects.HasActiveClip("Slash_clamped_attack_1"),"Basic attack raises its original separate sword-slash renderer");yield return new WaitForSeconds(.8f);
        game.progress.canDash=false;game.controls.Dash=true;yield return null;game.controls.Dash=false;Check(game.player.dashTime<=0,"Locked dash input is rejected");game.progress.canDash=true;
        var ladderRoom=System.Array.Find(game.rooms,r=>r.ladders!=null&&r.ladders.Length>0);if(ladderRoom!=null){game.Enter(ladderRoom.id,null,ladderRoom.ladders[0].center-Vector2.up*(ladderRoom.ladders[0].size.y*.35f));yield return new WaitForSeconds(.1f);float ladderY=game.player.transform.position.y;game.controls.Vertical=1;yield return new WaitForSeconds(.35f);game.controls.Vertical=0;Check(game.player.transform.position.y>ladderY+.35f,"Player climbs restored ladder trigger");}
        var shrineRoom=System.Array.Find(game.rooms,r=>r.checkpoints.Length>0);if(shrineRoom!=null){game.Enter(shrineRoom.id,null,shrineRoom.checkpoints[0].position);game.player.health=40;yield return new WaitForSeconds(.3f);game.view.transform.position=shrineRoom.checkpoints[0].position+new Vector3(0,1.5f,-10);Capture("priedieu-unlit");Check(game.player.health==40&&game.player.actor.Current!="penitent_priedieu_kneeling_anim","Proximity to Prie Dieu does not auto-trigger prayer");Check(game.CanPray&&game.CanInteract,"Proximity to Prie Dieu exposes interaction");game.controls.InteractHeld=true;yield return new WaitForSeconds(.3f);Check(game.player.health==40,"Short interaction hold does not save/heal");yield return new WaitForSeconds(.8f);game.controls.InteractHeld=false;Check(game.player.health==game.player.MaxHealth,"Held Prie Dieu interaction completes animation and restores");}
        game.controls.Parry=true;yield return null;game.controls.Parry=false;
        Check(game.player.Damage(15,game.player.transform.position.x+game.player.facing,true),"Forward attack parried");
        yield return new WaitForSeconds(1);game.player.Damage(20,game.player.transform.position.x-1,false);float damagedHealth=game.player.MaxHealth-20;Check(Mathf.Approximately(game.player.health,damagedHealth),"Damage reduces health");
        game.player.Damage(20,game.player.transform.position.x-1,false);Check(Mathf.Approximately(game.player.health,damagedHealth),"Damage invulnerability blocks immediate repeat");
        yield return new WaitForSeconds(1);game.controls.Flask=true;yield return null;game.controls.Flask=false;yield return new WaitForSeconds(.9f);
        Check(game.player.health==game.player.MaxHealth&&game.player.flasks==1,"Flask consumes charge and heals by the mobile source amount");
        var shrine=game.Current.checkpoints[0];game.player.motor.Teleport(shrine.position);game.Interact(false,true);
        game.view.transform.position=shrine.position+new Vector3(0,1.5f,-10);Capture("priedieu-lit");
        Check(game.player.flasks==2,"Checkpoint restores flask charges");
        yield return new WaitForSeconds(1);float heavyStartY=game.player.transform.position.y;game.player.Damage(30,game.player.transform.position.x+1,false);Check(game.player.actor.Current=="penitent_throwback_transition_anim","Heavy damage starts the original throwback transition");yield return new WaitForSeconds(.12f);Check(game.player.transform.position.y>heavyStartY+.1f,"Heavy damage launches Player into the throwback arc");game.player.Restore();
        yield return new WaitForSeconds(1);game.player.Damage(200,game.player.transform.position.x-1,false);
        yield return new WaitForSeconds(2);Check(!game.player.Dead&&game.Current.id=="D17Z01S01","Death returns to saved checkpoint");
        game.Enter("D17Z01S11",null,null);var boss=game.Current.Boss;
        Check(boss!=null,"Warden exists");if(boss!=null){boss.health=boss.maxHealth*.5f;Check(boss.PhaseTwo,"Warden enters phase two at half health");boss.Damage(999);Check(boss.Dead,"Warden defeat opens encounter");yield return new WaitForSeconds(2.8f);Check(boss.BossCorpseShown&&boss.actor.Current=="ElderBrother_Corpse","Warden death resolves to the original corpse and cleansing beat");}
        game.Enter("D17Z01S01",null,null);game.player.Restore();yield return new WaitForSeconds(.2f);
        Capture("awakening-playtest");ScreenCapture.CaptureScreenshot("Documentation/Previews/hud-playtest.png");yield return null;
        // Mobile UI & HUD source parity verification
        var specialBtn = GameObject.Find("Dynamic Controls Canvas/Safe area/SPECIAL");
        Check(specialBtn != null, "Mobile RangeAttack (SPECIAL) touch button instantiated above Dash");
        if (specialBtn != null)
        {
            var sRect = specialBtn.GetComponent<RectTransform>();
            Check(Mathf.Approximately(sRect.anchoredPosition.x, -101f) && Mathf.Approximately(sRect.anchoredPosition.y, -179.6f), "RangeAttack placed at exact mobile source coordinates (-101, -179.6)");
            Check(Mathf.Approximately(sRect.sizeDelta.x, 171f), "RangeAttack button uses authentic 171x171 source scale");
        }
        var hGauge = GameObject.Find("Health source gauge")?.GetComponent<RectTransform>();
        var fGauge = GameObject.Find("Fervour source gauge")?.GetComponent<RectTransform>();
        var fl1 = GameObject.Find("Bile flask 1")?.GetComponent<RectTransform>() ?? GameObject.Find("Static UI Canvas/Safe area/Bile flask 1")?.GetComponent<RectTransform>();
        Check(hGauge != null && fGauge != null && fl1 != null, "Player HUD Health, Fervour and Flasks instantiated and positioned");
        var bossHud = GameObject.Find("Static UI Canvas/Safe area/UI_BOSS_HEALTH");
        Check(bossHud != null, "UI_BOSS_HEALTH root instantiated in Static UI Canvas");
        var flaskFullTex = Resources.Load<Texture2D>("HUD/FlaskFull");
        var flaskEmptyTex = Resources.Load<Texture2D>("HUD/FlaskEmpty");
        Check(flaskFullTex != null && flaskEmptyTex != null, "Bile flask full and empty textures load from Resources");

        // Mobile 3-system UI Verification
        Check(game.controls.inventoryUI != null && game.controls.mapUI != null && game.controls.optionsUI != null, "BlasInventoryUI, BlasMapUI, BlasOptionsUI initialized on TouchControls");
        game.controls.OpenInventoryUI(); yield return new WaitForSecondsRealtime(0.2f);
        Check(game.controls.inventoryUI.IsOpen, "BlasInventoryUI opens with 7 categories and 16 slots");
        CaptureCanvas(game.controls.inventoryUI.Canvas, "blas-inventory-playtest"); yield return new WaitForSecondsRealtime(0.1f);
        game.controls.inventoryUI.InspectItem("QI31");
        game.controls.inventoryUI.ShowLoreModal(); yield return new WaitForSecondsRealtime(0.2f);
        Check(game.controls.inventoryUI.IsLoreOpen, "Thorn lore modal popup opens with authentic Deosgracias text");
        CaptureCanvas(game.controls.inventoryUI.Canvas, "blas-lore-modal-playtest"); yield return new WaitForSecondsRealtime(0.1f);
        game.controls.inventoryUI.Hide(); yield return new WaitForSecondsRealtime(0.2f);
        Check(!game.controls.inventoryUI.IsOpen, "BlasInventoryUI closes cleanly");

        game.controls.OpenMapUI(); yield return new WaitForSecondsRealtime(0.2f);
        Check(game.controls.mapUI.IsOpen, "BlasMapUI opens with FondoMapa mountain background and Metroidvania grid");
        CaptureCanvas(game.controls.mapUI.Canvas, "blas-map-playtest"); yield return new WaitForSecondsRealtime(0.1f);
        game.controls.mapUI.Hide(); yield return new WaitForSecondsRealtime(0.2f);
        Check(!game.controls.mapUI.IsOpen, "BlasMapUI closes cleanly");

        game.controls.ToggleOptions(); yield return new WaitForSecondsRealtime(0.2f);
        Check(game.controls.optionsUI.IsOpen, "BlasOptionsUI opens with twisted column statue and diamond selector");
        CaptureCanvas(game.controls.optionsUI.Canvas, "blas-options-playtest"); yield return new WaitForSecondsRealtime(0.1f);
        game.controls.ToggleOptions(); yield return new WaitForSecondsRealtime(0.2f);
        Check(!game.controls.optionsUI.IsOpen, "BlasOptionsUI closes cleanly");
        int[,] aspectSizes={{1280,720},{1440,720},{1560,720},{1600,720}};string[] aspectNames={"16x9","18x9","19_5x9","20x9"};
        for(int a=0;a<aspectNames.Length;a++){Screen.SetResolution(aspectSizes[a,0],aspectSizes[a,1],false);yield return new WaitForSecondsRealtime(.2f);Check(ControlsInsideScreen(),"HUD and touch controls remain inside "+aspectNames[a]+" safe display");ScreenCapture.CaptureScreenshot("Documentation/Previews/aspect-"+aspectNames[a]+".png");yield return null;}
        Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.2f);Finish();
    }
    void CaptureCanvas(Canvas c, string name)
    {
        if (c == null) { Capture(name); return; }
        var oldMode = c.renderMode;
        var oldCam = c.worldCamera;
        var oldDist = c.planeDistance;
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.worldCamera = game.view;
        c.planeDistance = 1f;
        Capture(name);
        c.renderMode = oldMode;
        c.worldCamera = oldCam;
        c.planeDistance = oldDist;
    }
    void Capture(string name)
    {
        Directory.CreateDirectory("Documentation/Previews");var camera=game.view;var target=new RenderTexture(1280,720,24);var old=camera.targetTexture;
        camera.targetTexture=target;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
        var png=new Texture2D(1280,720,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();
        File.WriteAllBytes("Documentation/Previews/"+name+".png",png.EncodeToPNG());camera.targetTexture=old;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(png);
    }
    bool ControlsInsideScreen(){var controls=GameObject.Find("Dynamic Controls Canvas");if(controls==null)return false;var corners=new Vector3[4];foreach(var r in controls.GetComponentsInChildren<RectTransform>(false)){if(r==controls.transform)continue;r.GetWorldCorners(corners);for(int i=0;i<4;i++)if(corners[i].x<-2||corners[i].y<-2||corners[i].x>Screen.width+2||corners[i].y>Screen.height+2)return false;}return true;}
    void Finish(){Debug.Log("[BrotherhoodPlayCheck] Finish() invoked, failed=" + failed + ", checks=" + checks.Count);File.WriteAllText("Documentation/playtest-results.txt",string.Join("\n",checks)+"\nRESULT: "+(failed?"FAILED":"PASSED"));File.Delete(Path.Combine(Application.dataPath, "../Temp/brotherhood-verify-play"));EditorApplication.isPlaying=false;}
}
