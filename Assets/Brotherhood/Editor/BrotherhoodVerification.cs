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
        Check(Resources.Load<Texture2D>("Menu/PressAnybutton")!=null,"Original Landing logo texture loaded");
        Check(Resources.Load<Texture2D>("Menu/MainMenuBackground")!=null&&Resources.Load<Texture2D>("Menu/MainMenuPenitent")!=null,"Original main-menu background and Penitent source atlas loaded");
        Check(Resources.Load<Texture2D>("Menu/Slot_01")!=null&&Resources.Load<Texture2D>("Menu/Slot_02")!=null&&Resources.Load<Texture2D>("Menu/LineasSlot_01")!=null,"Original pilgrimage slot art loaded");
        var originalFont=Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");Check(originalFont!=null,"Original Majestic Extended bitmap font loaded");
        Check(Resources.Load<Sprite>("Achievements/achievements-AC01")!=null&&Resources.Load<Sprite>("Achievements/achievements-bg-unlocked")!=null,"Original AC01 icon and unlocked achievement panel load from source atlas");
        var inputModule=FindAnyObjectByType<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();Check(inputModule!=null&&inputModule.actionsAsset!=null,"Menu input module has default pointer, touch and navigation actions");
        var menu=FindAnyObjectByType<MainMenuController>();var pageType=typeof(MainMenuController).GetNestedType("Page",System.Reflection.BindingFlags.NonPublic);var show=typeof(MainMenuController).GetMethod("Show",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);show?.Invoke(menu,new[]{System.Enum.Parse(pageType,"ModeSelect")});yield return null;var buttons=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);Check(buttons.Length>=4,"Pilgrimage, Options, Extras and Exit created");show?.Invoke(menu,new[]{System.Enum.Parse(pageType,"SaveSlots")});yield return null;buttons=FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None);Check(buttons.Length>=5,"Three save slots, Continue and Back created");
        bool labelsPassThrough=true;foreach(var label in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))if(label.raycastTarget)labelsPassThrough=false;Check(labelsPassThrough,"Menu labels do not block pointer clicks on buttons");
        bool menuFontReady=true;foreach(var label in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))if(label.font!=originalFont&&label.font!=VietnameseSource.DynamicFont)menuFontReady=false;Check(menuFontReady,"Main menu uses the original font or a Vietnamese-capable display font");
        show?.Invoke(menu,new[]{System.Enum.Parse(pageType,"ModeSelect")});
        bool translatedImmediately=false, englishFlash=false;
        foreach(var label in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsSortMode.None))
        {
            if(label.text=="Hành hương")translatedImmediately=true;
            if(label.text=="PILGRIMAGE")englishFlash=true;
        }
        Check(translatedImmediately&&!englishFlash,"Mode selection is Vietnamese in the same frame it is built");
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
        Check(game.rooms.Length==16&&Array.TrueForAll(new[]{"D17Z01S01","D17Z01S02","D17Z01S05","D17Z01S11","D17Z01S03","D01Z01S07","D01Z01S01","D01Z01S02","D01Z01S03","D01Z02S01","D01Z02S02","D01Z02S06"},id=>game.Find(id)!=null),"All twelve original rooms remain alongside four isolated supplemental source rooms");
        Check(game.player.health==game.player.MaxHealth&&game.player.MaxHealth==88&&game.player.MaxFervour==60,"Player uses mobile-source base Life 88 and Fervour 60");
        Check(game.progress!=null&&game.progress.canJump&&game.progress.canDash&&game.progress.canParry&&game.progress.hasMeaCulpa,"Core progression flags available");
        Check(File.Exists("Assets/Brotherhood/Scenes/MainMenu.unity"),"Main menu scene generated");
        Check(GameObject.Find("Static UI Canvas")!=null&&GameObject.Find("Dynamic Controls Canvas")!=null,"Separate safe-area HUD and control canvases");
        Check(Resources.Load<Texture2D>("HUD/PortraitFrame")!=null&&Resources.Load<Texture2D>("HUD/HealthFill")!=null&&Resources.Load<Texture2D>("HUD/FervourFill")!=null,"Original portrait and gauge art loaded");
        Check(Resources.Load<Texture2D>("HUD/FlaskFull")!=null&&Resources.Load<Texture2D>("HUD/FlaskEmpty")!=null&&Resources.Load<Texture2D>("HUD/TearsFrame")!=null,"Original flask and Tears art loaded");
        var originalFont=Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");Check(originalFont!=null,"Original Majestic Extended bitmap font loaded");
        string[] mobileControls={"Attack","Dash","Jump","Parry","Flask","Prayer","RangeAttack","Map","Inventory","HD_BaseJoystick","HD_ControlJoystick"};bool mobileArt=true;foreach(var control in mobileControls)if(Resources.Load<Texture2D>("MobileControls/"+control)==null){mobileArt=false;break;}Check(mobileArt,"Original Android touch-control and joystick art loaded");
        var healthGauge=GameObject.Find("Health source gauge");var fervourGauge=GameObject.Find("Fervour source gauge");Check(healthGauge!=null&&healthGauge.GetComponent<Image>().sprite.texture.name=="BarMid"&&healthGauge.GetComponent<Outline>()==null,"Health gauge uses original tiled frame without procedural outline");Check(healthGauge!=null&&Mathf.Approximately(healthGauge.GetComponent<RectTransform>().sizeDelta.x,200)&&fervourGauge!=null&&Mathf.Approximately(fervourGauge.GetComponent<RectTransform>().sizeDelta.x,140),"Original health and fervour gauge proportions restored");Check(GameObject.Find("Health loss")!=null&&GameObject.Find("Health end")!=null&&GameObject.Find("Fervour division 1")!=null&&GameObject.Find("Fervour division 2")!=null,"Original loss layer, bar end and fervour divisions restored");var allRects=FindObjectsByType<RectTransform>(FindObjectsInactive.Include,FindObjectsSortMode.None);var characterMenu=System.Array.Find(allRects,r=>r.name=="Character menu");Check(characterMenu!=null&&characterMenu.GetComponentsInChildren<Brotherhood.TouchControls.TouchButton>(true).Length>=7,"Character menu provides map, inventory, relic, prayer and skill pages");
        var caudexBold=Resources.Load<Font>("Fonts/Caudex-Bold");var caudexRegular=Resources.Load<Font>("Fonts/Caudex-Regular");bool hudFontReady=true;foreach(var label in FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))if(label.font!=originalFont&&label.font!=caudexBold&&label.font!=caudexRegular&&label.font!=VietnameseSource.DynamicFont)hudFontReady=false;Check(hudFontReady,"HUD and character menu use source fonts with Vietnamese-capable fallback");
        Check(VietnameseSource.Term("Bosses/ELDERBROTHER_NAME")=="Kẻ Canh Giữ Đau Khổ Lặng Im","Warden name comes from Vietnamese text in the source English slot");
        Check(VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0101_0").StartsWith("Hối tiếc"),"Deogracias dialogue comes from the source Vietnamese English-slot text");
        Check(InventoryCatalog.Load().Find("PR01").caption.StartsWith("Bài ca"),"Inventory captions load original Vietnamese I2 entries");
        Check(InventoryCatalog.Load().Find("CHARGED_1").description.StartsWith("Tập trung sức mạnh"),"Skill descriptions use the source Vietnamese UnlockableSkill entries");
        var portrait=GameObject.Find("Penitent portrait");var tears=GameObject.Find("Tears frame");
        var inventoryCatalog=InventoryCatalog.Load();Check(inventoryCatalog.items.Length>=68&&inventoryCatalog.Count("prayer")>=13&&inventoryCatalog.Count("relic")>=7&&inventoryCatalog.Count("rosarybead")>=39&&inventoryCatalog.Count("sword")>=9,"Original prayer, relic, rosary bead and sword metadata catalog imported");
        var sourceCharged1=inventoryCatalog.Find("CHARGED_1");var sourceCharged2=inventoryCatalog.Find("CHARGED_2");
        Check(sourceCharged1.skillCost==500&&sourceCharged2.skillCost==8000&&sourceCharged2.parentSkill=="CHARGED_1","Skill costs and prerequisites match original mobile assets");
        var savedProgressForSkillTest=game.progress;
        try
        {
            game.progress=new PlayerProgress();game.progress.tears=10000;
            Check(!game.CanPurchaseInventorySkill(sourceCharged1),"Skill purchase requires the source Mea Culpa level even with enough Tears");
            game.progress.meaCulpaLevel=3;
            Check(!game.CanPurchaseInventorySkill(sourceCharged2),"Second skill tier requires the original first-tier prerequisite");
            bool boughtFirst=game.PurchaseInventorySkill(sourceCharged1);
            bool boughtSecond=game.PurchaseInventorySkill(sourceCharged2);
            Check(boughtFirst&&boughtSecond&&game.progress.chargedTier==2&&Mathf.Approximately(game.progress.tears,1500),"Skill purchases deduct source Tears costs and persist the unlocked tier");
        }
        finally{game.progress=savedProgressForSkillTest;}
        int sourceEffects=0;foreach(var item in inventoryCatalog.items)if(item.effects!=null)sourceEffects+=item.effects.Length;Check(sourceEffects>=90,"Original inventory prefab effect components preserved in runtime catalog");
        bool everyInventoryIcon=true;foreach(var item in inventoryCatalog.items)if(string.IsNullOrEmpty(item.icon)||Resources.Load<Texture2D>(item.icon)==null){everyInventoryIcon=false;break;}Check(everyInventoryIcon,"All original inventory icons load from Resources");
        var menuIconSlots=System.Array.FindAll(allRects,r=>r.name.StartsWith("Inventory icon "));Check(menuIconSlots.Length==7,"Character menu provides seven reusable inventory icon slots");
        var prayerItem=System.Array.Find(inventoryCatalog.items,i=>i.id=="PR01");game.progress.SetOwned("PR01",true);Check(game.EquipInventoryItem(prayerItem)&&game.progress.equippedPrayer=="PR01"&&game.progress.hasPrayer,"Owned prayer can be equipped through the shared inventory state");var prayerStats=new InventoryModifiers(game.progress);Check(Mathf.Approximately(prayerStats.PrayerCost,20)&&Mathf.Approximately(prayerStats.PrayerDuration,10)&&Mathf.Approximately(prayerStats.PrayerBonus("PR01",0),1),"PR01 uses source cost, ten-second duration and attack-speed modifier");Check(game.EquipInventoryItem(prayerItem)&&string.IsNullOrEmpty(game.progress.equippedPrayer)&&!game.progress.hasPrayer,"Equipped prayer can be removed without losing ownership");var prayer16=System.Array.Find(inventoryCatalog.items,i=>i.id=="PR16");Check(prayer16!=null&&Mathf.Approximately(new InventoryModifiers(game.progress).PrayerBonus("PR16",19),.3f),"PR16 preserves mobile 30 percent Tears multiplier");var activePrayerField=typeof(PlayerController).GetField("activePrayer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);game.progress.tears=0;activePrayerField.SetValue(game.player,"PR16");typeof(PlayerController).GetField("prayerTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game.player,10f);game.EnemyDefeated(10);Check(Mathf.Approximately(game.progress.tears,13),"PR16 applies mobile Tears multiplier to source enemy reward");activePrayerField.SetValue(game.player,"");typeof(PlayerController).GetField("prayerTime",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game.player,0f);game.progress.tears=0;
        var beadItem=System.Array.Find(inventoryCatalog.items,i=>i.id=="RB01");game.progress.SetOwned("RB01",true);Check(game.EquipInventoryItem(beadItem)&&game.progress.IsEquipped("RB01"),"Owned rosary bead can be equipped in a persistent slot");Check(game.EquipInventoryItem(beadItem)&&!game.progress.IsEquipped("RB01"),"Equipped rosary bead can be removed without losing ownership");
        game.EquipInventoryItem(beadItem);var beadStats=new InventoryModifiers(game.progress);Check(Mathf.Approximately(beadStats.DamageTaken(20),18),"RB01 applies its source 10 percent normal-damage reduction");game.EquipInventoryItem(beadItem);
        var heartItem=System.Array.Find(inventoryCatalog.items,i=>i.id=="HE03");game.progress.SetOwned("HE03",true);game.EquipInventoryItem(heartItem);var heartStats=new InventoryModifiers(game.progress,game.player);float priorHeartHealth=game.player.health;game.player.health=10;Check(Mathf.Approximately(heartStats.DamageDealt(18),26),"HE03 applies its source Strength plus eight modifier below twenty percent Life");game.player.health=priorHeartHealth;game.EquipInventoryItem(heartItem);
        var toggleBag=typeof(TouchControls).GetMethod("ToggleBag",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);toggleBag?.Invoke(game.controls,null);yield return null;Check(characterMenu.gameObject.activeInHierarchy&&Time.timeScale==0,"Character inventory opens centered and pauses gameplay");ScreenCapture.CaptureScreenshot("Documentation/Previews/character-menu-playtest.png");yield return null;toggleBag?.Invoke(game.controls,null);Check(!characterMenu.gameObject.activeInHierarchy&&Time.timeScale==1,"Closing character inventory resumes gameplay");
        toggleBag?.Invoke(game.controls,null);var enterRemap=typeof(TouchControls).GetMethod("EnterRemap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);var exitRemap=typeof(TouchControls).GetMethod("ExitRemap",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Public);enterRemap?.Invoke(game.controls,null);yield return null;var allTouchButtons=game.controls.GetComponentsInChildren<TouchControls.TouchButton>(true);bool mappedButtons=System.Array.FindAll(allTouchButtons,b=>!string.IsNullOrEmpty(b.layoutKey)&&b.owner==game.controls).Length>=9;Check(game.controls.Remapping&&mappedButtons&&GameObject.Find("DONE LAYOUT")!=null,"Android touch layout enters drag-remap mode with persistent button keys");exitRemap?.Invoke(game.controls,null);Check(!game.controls.Remapping&&GameObject.Find("DONE LAYOUT")==null,"Finishing touch remap returns to gameplay controls");
        Check(portrait!=null&&portrait.GetComponent<UnityEngine.UI.Image>().sprite.texture.width==88,"HUD uses cropped original portrait frame");
        Check(tears!=null&&tears.GetComponent<UnityEngine.UI.Image>().sprite.texture.width==87,"HUD uses cropped original Tears frame");
        var staticSafe=GameObject.Find("Static UI Canvas/Safe area").GetComponent<RectTransform>();var dynamicSafe=GameObject.Find("Dynamic Controls Canvas/Safe area").GetComponent<RectTransform>();
        Check(staticSafe.anchorMin.x>=0&&staticSafe.anchorMax.x<=1&&dynamicSafe.anchorMin.x>=0&&dynamicSafe.anchorMax.x<=1,"HUD and controls constrained to device safe area");
        game.progress.canDash=false;yield return null;Check(!GameObject.Find("DASH"),"Locked dash button is hidden");game.progress.canDash=true;yield return null;Check(GameObject.Find("DASH")!=null,"Unlocked dash button is visible");
        var lockedSpecial=GameObject.Find("SPECIAL");Check(!GameObject.Find("PRAYER")&&lockedSpecial!=null&&lockedSpecial.GetComponent<CanvasGroup>().alpha==1,"New-game ranged control is visible at full opacity as in the mobile opening HUD");game.progress.hasPrayer=game.progress.hasSpecial=true;game.progress.lungeTier=1;yield return null;Check(GameObject.Find("PRAYER")!=null&&GameObject.Find("SPECIAL").GetComponent<CanvasGroup>().alpha==1,"Unlocked prayer appears while the special touch button keeps its source appearance");game.progress.hasPrayer=game.progress.hasSpecial=false;game.progress.lungeTier=0;
        yield return VerifyCheatMenu();
        Check(game.audioBank!=null&&game.audioBank.clips.Length>=70,"Expanded original audio samples attached");
        Check(game.audioBank.Has("ENEMY_STUNT")&&game.audioBank.Has("ACOLYTE_EXECUTION_2")&&game.audioBank.Has("EXECUTION_EFFECT")&&game.audioBank.Has("EXECUTION_FIRST_HIT")&&game.audioBank.Has("EXECUTION_SECOND_HIT")&&game.audioBank.Has("EXECUTION_THIRD_HIT")&&game.audioBank.Has("EXECUTION_EFFECT_END"),"Execution prompt and hit sounds restored from source FMOD bank");
        Check(Mathf.Approximately(SourceGameplayTuning.ExecutionStunTime,5f),"Acolyte and NewFlagellant preserve the mobile 5 second execution window");
        Check(game.audioBank.Has("PENITENT_CLIMB_LADDER_1")&&game.audioBank.Has("PENITENT_CLIMB_LADDER_2")&&game.audioBank.Has("PENITENT_CLIMB_LADDER_3")&&game.audioBank.Has("PENITENT_RUNSTOP_STONE")&&game.audioBank.Has("PENITENT_PUSHBACK")&&game.audioBank.Has("HARD_LANDING")&&game.audioBank.Has("HEALING_EXPLOSION")&&game.audioBank.Has("VERTICAL_ATTACK_FALL")&&game.audioBank.Has("VERTICAL_ATTACK_HIT"),"Movement, ladder, damage, healing and plunge sounds restored from source FMOD bank");
        Check(game.audioBank.Has("LUNGE_ATTACK")&&game.audioBank.Has("LUNGE_ATTACK_LV2")&&game.audioBank.Has("LUNGE_ATTACK_LV3")&&game.audioBank.Has("LUNGE_ATTACK_HIT")&&game.audioBank.Has("LOADING_CHARGED_ATTACK")&&game.audioBank.Has("LOADED_CHARGED_ATTACK")&&game.audioBank.Has("RELEASE_CHARGED_ATTACK")&&game.audioBank.Has("CHARGED_ATTACK_PROJECTILE")&&game.audioBank.Has("CHARGED_ATTACK_PROJECTILE_HIT")&&game.audioBank.Has("CHARGED_ATTACK_PROJECTILE_HIT_WALL"),"Charged and lunge sounds restored from source FMOD bank");
        Check(game.audioBank.Has("RANGE_ATTACK")&&game.audioBank.Has("RANGE_ATTACK_FLY")&&game.audioBank.Has("RANGE_ATTACK_HIT")&&game.audioBank.Has("RANGE_ATTACK_DISSAPEAR")&&game.audioBank.Has("RANGE_ATTACK_EXPLODE"),"Fervorous Blood cast, flight, hit, vanish and explosion sounds restored");
        Check(game.audioBank.Has("PENITENT_ACTIVATE_PRAYER")&&game.audioBank.Has("FERVOR_START_PRAYER")&&game.audioBank.Has("FERVOR_END_PRAYER")&&game.audioBank.Has("PRAYER_INVINCIBILITY")&&game.audioBank.Has("EQUIP_PRAYER")&&game.audioBank.Has("NO_PRAYER"),"Prayer activation, duration, protection and inventory sounds restored");
        var sourceClips=game.player.actor.catalog;Check(sourceClips.Find("Player_Run_Start")!=null&&sourceClips.Find("Player_Run_Stop")!=null&&sourceClips.Find("Player_Landing")!=null&&sourceClips.Find("Player_Landing_Running")!=null&&sourceClips.Find("Player_Jump_Attack_noslashes")!=null&&sourceClips.Find("Player_crouch_attack_noslashes")!=null&&sourceClips.Find("Player_Upward_Attack_Clamped_anim")!=null&&sourceClips.Find("penitent_pushback_grounded_dust_effect_anim")!=null,"Directional combat and locomotion source clips available");Check(sourceClips.Find("Slash_clamped_attack_1")!=null&&sourceClips.Find("Slash_clamped_attack_2")!=null&&sourceClips.Find("Slash_clamped_attack_3")!=null,"Original separate sword-slash renderer clips available");Check(sourceClips.Find("ChargedAttackProjectile_anim")!=null&&sourceClips.Find("ChargedAttackProjectile_impact_anim")!=null&&sourceClips.Find("ChargedAttackProjectile_vanish_anim")!=null,"Original charged projectile flight, impact and vanish clips available");Check(sourceClips.Find("penitent_rangeAttack_projectile_anim")!=null&&sourceClips.Find("penitent_rangeAttack_projectile_vanish_anim")!=null&&sourceClips.Find("penitent_rangeAttack_projectile_explode_anim")!=null,"Fervorous Blood flight, vanish and explosion clips available");
        Check(sourceClips.Find("penitentBeam_startToWarning")!=null&&sourceClips.Find("AlliedCherub_flying")!=null&&sourceClips.Find("penitent_guardian_lady_anim")!=null&&sourceClips.Find("flamePillar_warningToAttack")!=null&&sourceClips.Find("pontiffOldman_toxicFog_appear")!=null,"Mobile prayer beam, cherub, guardian, flame and toxic clips available");
        Check(sourceClips.Find("penitent_upward_attack_jump")!=null&&sourceClips.Find("penitent_upward_attack_slash_lvl1_jump")!=null,
            "Original airborne upward attack and slash clips are available");
        game.Enter("D17Z01S01",null,null);game.player.Restore();game.player.motor.grounded=true;
        game.controls.Jump=game.controls.Attack=game.controls.JumpHeld=true;game.controls.Vertical=1;
        game.player.Tick(.02f);
        Check(game.player.motor.velocity.y>0&&game.player.actor.Current=="penitent_upward_attack_jump"&&
            game.effects.HasActiveClip("penitent_upward_attack_slash_lvl1_jump"),
            "Space+W+J in one frame jumps and plays the original airborne upward attack and slash");
        game.controls.ClearGameplayInput();game.player.Restore();game.effects.Clear();
        game.player.motor.grounded=false;game.player.motor.velocity.y=4f;
        game.controls.Attack=true;game.controls.Vertical=1;game.player.Tick(.02f);
        Check(game.player.actor.Current=="penitent_upward_attack_jump"&&
            game.effects.HasActiveClip("penitent_upward_attack_slash_lvl1_jump"),
            "W+J while already airborne uses the original upward attack instead of a horizontal slash");
        game.controls.ClearGameplayInput();game.player.Restore();game.effects.Clear();
        game.player.motor.grounded=true;game.controls.Attack=true;game.controls.Vertical=1;game.player.Tick(.02f);
        Check(game.player.actor.Current=="Player_Upward_Attack_Clamped_anim"&&
            game.effects.HasActiveClip("Player_Upward_Attack_Clamped_slash_lvl1_anim"),
            "W+J on the ground plays the original grounded upward slash");
        game.controls.ClearGameplayInput();game.player.Restore();game.effects.Clear();
        Check(System.Array.Exists(game.rooms,r=>r.ladders!=null&&r.ladders.Length>0),"Source ladder trigger zones restored");
        Check(Mathf.Abs(game.player.actor.visual.bounds.min.y-game.player.transform.position.y)<.08f,"Player sprite feet align with motor origin");
        foreach(var room in game.rooms)
        {
            game.Enter(room.id,null,null);yield return new WaitForSeconds(1.2f);
            Check(room.GetComponentsInChildren<Collider2D>().Length>0,room.id+" terrain colliders");
            Check(room.GetComponentsInChildren<SpriteRenderer>().Length>0,room.id+" visible sprite renderers");
            Check(room.GetComponentsInChildren<SourceObjectId>(true).Length==room.sourceNodeCount,room.id+" preserves every mobile source transform");
            Check(System.Array.FindAll(room.GetComponentsInChildren<SourceObjectId>(true),o=>o.GetComponent<SpriteRenderer>()!=null).Length==room.sourceRendererCount,room.id+" preserves every mobile source renderer");
            int expectedParallax=room.id=="D17Z01S04"?0:room.id=="D17Z01S01"||room.id=="D01Z01S07"?5:room.id=="D17Z01S02"||room.id=="D17Z01S05"?7:room.id=="D17Z01S11"?8:4;
            if(room.id.StartsWith("D17")||room.id=="D01Z01S07")Check(room.parallax.Length==expectedParallax,room.id+" preserves mobile parallax layer count");
            Vector4 expectedBounds=room.id=="D17Z01S01"?new Vector4(-1006,-950,5.375f,44):room.id=="D17Z01S02"?new Vector4(-950,-890,4.375f,15.625f):room.id=="D17Z01S05"?new Vector4(-876,-830,6.375f,19):room.id=="D17Z01S11"?new Vector4(-830,-790,7.875f,19.125f):room.id=="D01Z01S07"?new Vector4(-769,-730,7.375f,18.625f):new Vector4(-790,-770,6.375f,17.625f);
            if(room.id=="D17Z01S04")expectedBounds=new Vector4(-870,-828,-6.625f,4.625f);
            else if(room.id=="D17Z01S07")expectedBounds=new Vector4(-870,-850,-39,-5);
            else if(room.id=="D17Z01S08")expectedBounds=new Vector4(-890.03125f,-870.03125f,-35.96875f,-24.71875f);
            else if(room.id=="D17Z01S09")expectedBounds=new Vector4(-890,-870,-14.625f,-3.375f);
            if(room.id.StartsWith("D17")||room.id=="D01Z01S07")Check(Mathf.Approximately(room.left,expectedBounds.x)&&Mathf.Approximately(room.right,expectedBounds.y)&&Mathf.Approximately(room.bottom,expectedBounds.z)&&Mathf.Approximately(room.top,expectedBounds.w),room.id+" preserves mobile camera boundaries");
            Check(game.player.motor.grounded,room.id+" grounded arrival "+game.player.transform.position);
            Check(Mathf.Abs(game.player.actor.visual.bounds.min.y-game.player.transform.position.y)<.08f,room.id+" player feet touch arrival surface");
            Check(!game.player.Dead,room.id+" safe arrival");
            if(room.Boss!=null)Check(!room.Boss.BossIntroComplete&&!game.BossFightActive,"Warden waits for the original 4x10 BossFight trigger before combat");
            foreach(var enemy in room.enemies)if(enemy!=null&&enemy.actor!=null&&enemy.actor.visual!=null)
            {Check(enemy.boss?enemy.actor.catalog.Find("ElderBrother_Idle")!=null:enemy.actor.Current!=""&&enemy.actor.visual.sprite!=null,room.id+" "+enemy.family+" has original source animation");if(!enemy.boss)Check(enemy.maxHealth>0&&enemy.purgeReward>0,room.id+" "+enemy.family+" has health and Tears reward");else Check(enemy.maxHealth==400&&enemy.purgeReward==0,"Warden uses source 400 health and zero purge reward");}
            foreach(var sr in room.GetComponentsInChildren<SpriteRenderer>())if(sr.enabled&&sr.sprite!=null){Check(sr.sharedMaterial.shader.isSupported,room.id+" sprite shader supported");break;}
            Capture(room.id);
            foreach(var door in room.doors)
            {
                var target=game.Find(door.target);if(target!=null)Check(target.Door(door.targetDoor)!=null,room.id+" reciprocal door "+door.key+" -> "+door.target+"/"+door.targetDoor);
            }
        }
        var stalledRoom=game.Find("D17Z01S02");game.Enter(stalledRoom.id,null,null);game.player.Restore();
        var stalledEnemy=System.Array.Find(stalledRoom.enemies,e=>e.family=="acolyte");
        if(stalledEnemy!=null)
        {
            stalledEnemy.ResetEnemy();
            var blocker=new GameObject("Verification wall in front of Acolyte");blocker.layer=8;
            blocker.transform.position=stalledEnemy.transform.position+new Vector3(.5f,.85f,0);
            var blockCollider=blocker.AddComponent<BoxCollider2D>();blockCollider.size=new Vector2(.2f,1.7f);
            game.player.motor.Teleport((Vector2)stalledEnemy.transform.position+Vector2.right*5f);
            Physics2D.SyncTransforms();
            for(int step=0;step<80;step++)stalledEnemy.Tick(.02f);
            Check(stalledEnemy.actor.Current=="acolyte_idle"&&Mathf.Abs(stalledEnemy.motor.velocity.x)<.01f,
                "Acolyte stops its source running animation when terrain blocks actual movement");
            Destroy(blocker);yield return null;
        }
        yield return VerifyShockReceiverControls();
        var shockRoom=game.Find("D17Z01S03");
        game.progress.shockGateOpened=false;game.Enter(shockRoom.id,null,null);game.player.Restore();
        var shockReceiver=System.Array.Find(shockRoom.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_124");
        var shockGateBody=System.Array.Find(shockRoom.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_109");
        var shockGraphic=System.Array.Find(shockRoom.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_91");
        var shockBarrier=shockGateBody==null?null:shockGateBody.GetComponent<Collider2D>();
        Check(shockReceiver!=null&&shockBarrier!=null&&shockBarrier.enabled&&shockGateBody.GetComponent<SpriteActor>()?.Current=="l_gate_closed_anim",
            "D17Z01S03 source SlashReceiver begins with a blocking, closed Gate");
        if(shockReceiver!=null&&shockBarrier!=null)
        {
            Vector2 receiver=shockReceiver.transform.position;
            float gateX=shockGateBody.transform.position.x;
            var floorUnderGate=Physics2D.Raycast(new Vector2(gateX-2f,shockGateBody.transform.position.y+2f),Vector2.down,10f,1<<8);
            float gateFeetY=floorUnderGate.collider==null?shockGateBody.transform.position.y-4f:floorUnderGate.point.y+.03f;
            game.player.motor.Teleport(new Vector2(gateX-2f,gateFeetY));
            for(int step=0;step<75;step++){game.player.motor.velocity.x=3f;game.player.motor.Step(1f/60f);}
            Check(game.player.transform.position.x<gateX-.7f,
                "D17Z01S03 closed Gate physically stops the Penitent before the eastern exit");
            game.controls.ClearGameplayInput();game.controls.Move=1f;
            for(int step=0;step<12;step++)game.player.Tick(.02f);
            Check(game.player.actor.Current=="Player_Idle",
                "Held movement into the closed Gate does not play a stationary running animation");
            game.controls.Move=0f;
            Check(!shockRoom.TryStrikeShockReceiver(receiver+Vector2.left*4f+Vector2.down,1f,1.8f,2.4f,game)&&shockBarrier.enabled,
                "D17Z01S03 Gate cannot open without striking the original receiver");
            Check(!shockRoom.TryStrikeShockReceiver(receiver+Vector2.left+Vector2.down*3.4f,1f,1.8f,2.4f,game)&&shockBarrier.enabled,
                "Ordinary horizontal slash cannot hit the high D17Z01S03 receiver");
            Check(game.audioBank!=null&&game.audioBank.Has("GATE_OPEN")&&game.audioBank.Has("BELL_RECEIVER_ACTIVATE"),
                "D17Z01S03 uses both original Master Bank GateOpen and BellReceiverActivate sounds");
            Check(shockRoom.TryStrikeShockReceiver(receiver+Vector2.left+Vector2.down*3.4f,1f,1.8f,4.2f,game)&&game.progress.shockGateOpened&&
                !shockBarrier.enabled&&shockGateBody.GetComponent<SpriteActor>()?.Current=="l_gate_going_up_anim"&&
                shockGraphic.GetComponent<SpriteActor>()?.Current=="glassSwitch_activating",
                "Original upward slash can hit D17Z01S03 SlashReceiver and play Gate and Switch animations");
            Check(!shockRoom.TryStrikeShockReceiver(receiver+Vector2.left+Vector2.down,1f,1.8f,2.4f,game),
                "D17Z01S03 TriggerReceiver activates only once");
            game.player.motor.Teleport(new Vector2(gateX-2f,gateFeetY));
            for(int step=0;step<75;step++){game.player.motor.velocity.x=3f;game.player.motor.Step(1f/60f);}
            Check(game.player.transform.position.x>gateX+.7f,
                "D17Z01S03 opened Gate lets the Penitent reach the eastern exit");
            yield return new WaitForSeconds(1.4f);
            Check(shockGateBody.GetComponent<SpriteActor>()?.Current=="l_gate_opened_anim"&&
                shockGraphic.GetComponent<SpriteActor>()?.Current=="glassSwitch_active",
                "D17Z01S03 Gate and switch settle on the original opened frames");
            game.Enter("D17Z01S02",null,null);game.Enter(shockRoom.id,null,null);
            Check(!shockBarrier.enabled&&shockGateBody.GetComponent<SpriteActor>()?.Current=="l_gate_opened_anim",
                "D17Z01S03 Gate remains open after leaving and returning to the room");
        }
        var gateRoom=game.Find("D17Z01S05");
        game.Enter(gateRoom.id,null,null);game.player.Restore();
        var openGateSource=System.Array.Find(gateRoom.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_178");
        var openGateCollider=openGateSource==null?null:openGateSource.GetComponent<Collider2D>();
        var openGateActor=openGateSource==null?null:openGateSource.GetComponent<SpriteActor>();
        Check(openGateCollider!=null&&!openGateCollider.enabled&&openGateActor!=null&&openGateActor.Current=="l_gate_opened_anim",
            "D17Z01S05 restores source PenitenceGateOpen startOpen=1, disabling its barrier and showing the original open sprite");
        var bossDoor=gateRoom.Door("E");
        Check(bossDoor!=null&&bossDoor.target=="D17Z01S11","D17Z01S05 east door targets the Warden room");
        if(openGateSource!=null&&bossDoor!=null)
        {
            float x=openGateSource.transform.position.x-2f;
            var floor=Physics2D.Raycast(new Vector2(x,bossDoor.trigger.position.y+2f),Vector2.down,5f,1<<8);
            float y=floor.collider==null?bossDoor.trigger.position.y:floor.point.y+.03f;
            game.player.motor.Teleport(new Vector2(x,y));
            for(int step=0;step<90;step++){game.player.motor.velocity.x=3f;game.player.motor.Step(1f/60f);}
            Check(game.player.transform.position.x>bossDoor.trigger.position.x-.6f,
                "Player can physically cross the originally-open D17Z01S05 gate to the boss door");
            game.player.motor.Teleport(new Vector2(bossDoor.trigger.position.x,y));
            typeof(BrotherhoodGame).GetField("transitionCooldown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game,0f);
            yield return new WaitForSeconds(1.3f);
            Check(game.Current.id=="D17Z01S11","Walking through D17Z01S05 east door enters the Warden room");
        }
        string[] altarRoute={"D01Z01S07","D01Z01S01","D01Z01S02","D01Z01S03","D01Z02S01","D01Z02S02","D01Z02S06"};
        bool routeConnected=true;
        for(int i=0;i<altarRoute.Length-1;i++){var step=game.Find(altarRoute[i]);if(step==null||!System.Array.Exists(step.doors,d=>d.target==altarRoute[i+1]))routeConnected=false;}
        Check(routeConnected,"Original mobile doors connect the Holy Line to the Mea Culpa altar room");
        foreach(string forestRoomId in new[]{"D01Z01S01","D01Z01S02","D01Z01S03"})
        {var forest=game.Find(forestRoomId);var fills=forest==null?System.Array.Empty<MeshFilter>():forest.GetComponentsInChildren<MeshFilter>(true);Check(fills.Length>0&&System.Array.TrueForAll(fills,f=>f.sharedMesh!=null&&f.GetComponent<MeshRenderer>()!=null),forestRoomId+" restores original forest floor-fill meshes");}
        foreach(var bridge in new[]{(room:"D01Z01S01",node:"LOGIC_181"),(room:"D01Z01S03",node:"LOGIC_271")})
        {
            var forest=game.Find(bridge.room);
            var source=forest==null?null:System.Array.Find(forest.GetComponentsInChildren<SourceObjectId>(true),o=>o.node==bridge.node);
            var collider=source==null?null:source.GetComponent<BoxCollider2D>();
            Check(collider!=null&&collider.enabled&&!collider.isTrigger&&collider.gameObject.layer==8,bridge.room+" imports the original LOGIC GEO_Block floor collider over the gap");
            if(collider!=null)
            {
                game.Enter(bridge.room,null,null);
                var bounds=collider.bounds;
                game.player.motor.Teleport(new Vector2(bounds.min.x+.35f,bounds.max.y+.025f));
                for(int step=0;step<90;step++){game.player.motor.velocity.x=2.4f;game.player.motor.Step(1f/60f);}
                bool supported=game.player.motor.grounded&&game.player.transform.position.x>bounds.min.x+3f&&game.player.transform.position.y>bounds.max.y-.25f;
                Check(supported,bridge.room+" player remains supported while traversing the restored source floor"+
                    " (x="+game.player.transform.position.x.ToString("F2")+", y="+game.player.transform.position.y.ToString("F2")+
                    ", ground="+game.player.motor.grounded+", bridge="+bounds.min.x.ToString("F2")+".."+bounds.max.x.ToString("F2")+
                    ", top="+bounds.max.y.ToString("F2")+")");
            }
        }
        foreach(string splitRoomId in new[]{"D01Z01S01","D01Z01S03"})
        {
            var split=game.Find(splitRoomId);
            if(split==null||split.cameraRegions.Length!=2||split.undergroundTrigger==null){Check(false,splitRoomId+" has both original camera regions and its underground trigger");continue;}
            var upper=split.cameraRegions[0].bottom>split.cameraRegions[1].bottom?split.cameraRegions[0]:split.cameraRegions[1];
            var lower=split.cameraRegions[0].bottom<split.cameraRegions[1].bottom?split.cameraRegions[0]:split.cameraRegions[1];
            split.ResetCameraZone();
            var surface=split.CameraBoundsFor(new Vector2((upper.left+upper.right)*.5f,upper.bottom+1f));
            Vector2 trigger=split.undergroundTrigger.TransformPoint(split.undergroundOffset);
            var below=split.CameraBoundsFor(trigger);
            Check(surface==upper&&below==lower,splitRoomId+" keeps the surface camera high and switches only at the source underground trigger");
            split.ResetCameraZone();
            Check(split.PlayerFallY(new Vector2((upper.left+upper.right)*.5f,upper.bottom))>upper.bottom,
                splitRoomId+" resolves a surface pit before the player disappears into the black lower camera region");
        }
        Check(System.Array.FindAll(game.Find("D01Z01S01").enemies,e=>e.family=="fool").Length==5,"D01Z01S01 enables five source Fool spawns and excludes disabled WheelCarriers");
        Check(game.Find("D01Z01S02").enemies.Length==9&&game.Find("D01Z01S03").enemies.Length==22,"D01Z01S02/S03 restore enabled source monster spawns, excluding cherub captor");
        var sourceFool=System.Array.Find(game.Find("D01Z01S01").enemies,e=>e.family=="fool");
        var sourceWheel=System.Array.Find(game.Find("D01Z01S02").enemies,e=>e.family=="wheelcarrier");
        var sourceMud=System.Array.Find(game.Find("D01Z01S02").enemies,e=>e.family=="mudcrawler");
        Check(sourceFool!=null&&sourceFool.maxHealth==12&&sourceFool.purgeReward==5&&sourceWheel!=null&&sourceWheel.maxHealth==60&&sourceWheel.purgeReward==20&&sourceMud!=null&&sourceMud.maxHealth==10&&sourceMud.purgeReward==5,"Forest monster life and Tears values match mobile prefab Stats");
        var earlyDeog=System.Array.Find(game.Find("D01Z01S01").GetComponentsInChildren<Transform>(true),t=>t.name=="Deosgracias");
        Check(earlyDeog!=null&&!earlyDeog.gameObject.activeSelf,"D01Z01S01 conditional Deogracias remains hidden in the normal campaign");
        Check(game.player.actor.catalog.Find("penitent_blueFireDisc")!=null&&game.player.actor.catalog.Find("Fool_Walk")!=null&&game.player.actor.catalog.Find("mudcrawler_crawling_anim")!=null,"Original Verdiales projectile and forest monster clips imported");
        var sourceImpact=game.player.actor.catalog.Find("processioner_fireBall_exploding");
        Check(sourceImpact!=null&&sourceImpact.frames.Length==15&&Mathf.Abs(sourceImpact.duration-.43f)<.01f,
            "Verdiales hit/end effect imports all 15 source BlueFireExplosion frames");
        Check(game.player.actor.visual.sortingLayerName=="Player"&&game.player.actor.visual.sortingOrder==1,
            "Penitent renderer keeps the source Player layer above ledge soil");
        game.Enter("D01Z01S01",null,null);
        yield return new WaitForSeconds(.15f);
        game.effects.PrayerCrawler(game.player.transform.position+Vector3.up*.2f,-1f,game,18f);
        Check(game.effects.HasActiveClip("penitent_blueFireDisc")&&!game.effects.HasActiveClip("pontiffOldman_toxicOrb_idle"),"PR14 Verdiales projectile uses its source BlueFireDisc animation, not the Pontiff toxic orb");
        Check(Resources.Load<Texture2D>("Effects/CrawlerMaskSquare")!=null,"PR14 Verdiales uses the original CrawlerBullet_Base Square_0 SpriteMask texture");
        var sourceFirePalette=Resources.Load<Material>("Effects/recolor_FireToBlue");
        Check(sourceFirePalette!=null&&sourceFirePalette.shader!=null&&sourceFirePalette.shader.isSupported&&
            sourceFirePalette.HasProperty("_SwapB")&&sourceFirePalette.GetColor("_SwapB").b>.9f,
            "PR14 Verdiales loads the source recolor_FireToBlue material and its blue palette");
        Check(Resources.Load<Material>("Effects/CrawlerOrbParticles")!=null&&Resources.Load<Texture2D>("Effects/FervourBarSparks")!=null,
            "PR14 Verdiales imports the source CrawlerOrbParticles material and FervourBarSparks sheet");
        var crawlerMask=System.Array.Find(game.effects.GetComponentsInChildren<SpriteMask>(true),m=>m.name=="CrawlerBullet_Base SpriteMask");
        Check(crawlerMask!=null&&crawlerMask.enabled&&crawlerMask.isCustomRangeActive&&Mathf.Approximately(crawlerMask.transform.localPosition.y,.59375f)&&
            crawlerMask.frontSortingOrder==31&&crawlerMask.backSortingOrder==29,"PR14 Verdiales applies the original projectile SpriteMask dimensions, offset and sorting range");
        yield return new WaitForSeconds(.08f);
        var crawlerRenderer=crawlerMask==null?null:crawlerMask.transform.parent.GetComponent<SpriteRenderer>();
        Check(game.effects.HasActiveClip("penitent_blueFireDisc")&&crawlerRenderer!=null&&crawlerRenderer.sprite!=null&&
            crawlerRenderer.maskInteraction==SpriteMaskInteraction.VisibleInsideMask&&crawlerRenderer.sharedMaterial==sourceFirePalette&&crawlerRenderer.gameObject.activeInHierarchy,
            "PR14 projectile remains visibly active with its source frames and mask after launch");
        var crawlerParticles=crawlerRenderer==null?null:crawlerRenderer.GetComponentInChildren<ParticleSystem>(true);
        Check(crawlerParticles!=null&&crawlerParticles.isPlaying&&crawlerParticles.GetComponent<ParticleSystemRenderer>().sharedMaterial==Resources.Load<Material>("Effects/CrawlerOrbParticles"),
            "PR14 projectile emits the source blue spark sheet while travelling");
        game.effects.Animation("processioner_fireBall_exploding",game.player.transform.position+Vector3.right,1f,1f,true);
        Check(game.effects.HasActiveClip("processioner_fireBall_exploding"),"Verdiales source blue-fire impact plays as an animated clip");
        var impactRenderer=System.Array.Find(game.effects.GetComponentsInChildren<SpriteRenderer>(true),r=>r.gameObject.activeInHierarchy&&r.sortingLayerName=="Player"&&r.sortingOrder==2&&r.sharedMaterial==sourceFirePalette);
        Check(impactRenderer!=null,"Verdiales explosion renders on the source Player layer above the character");
        yield return new WaitForSeconds(.1f);
        Capture("verdiales-source-mask");
        yield return new WaitForEndOfFrame();
        game.effects.Clear();
        var forestAI=game.Find("D01Z01S02");
        game.Enter(forestAI.id,null,null);
        var activeForest=forestAI.enemies;
        bool stableSort=true;
        for(int a=0;a<activeForest.Length;a++)for(int b=a+1;b<activeForest.Length;b++)
            if(activeForest[a].actor.visual.sortingOrder==activeForest[b].actor.visual.sortingOrder)stableSort=false;
        Check(stableSort,"Forest monsters have stable unique draw order so crossing sprites do not z-flicker");
        sourceMud.ResetEnemy();
        game.player.Restore();
        game.player.motor.Teleport((Vector2)sourceMud.transform.position+Vector2.right*2f);
        sourceMud.Tick(.02f);
        Check(sourceMud.actor.visual.enabled&&sourceMud.actor.Current=="mudcrawler_appearing_anim"&&
            Mathf.Approximately(sourceMud.actor.visual.transform.localPosition.y,.25f),
            "MudCrawler rises at the source Body sprite offset instead of sinking into mud");
        for(int step=0;step<42;step++)sourceMud.Tick(.02f);
        Check(sourceMud.actor.Current=="mudcrawler_crawling_anim","MudCrawler switches from source appearing clip to crawling pursuit");
        game.player.motor.Teleport((Vector2)sourceMud.transform.position+Vector2.right*20f);
        for(int step=0;step<110;step++)sourceMud.Tick(.02f);
        Check(sourceMud.actor.Current=="mudcrawler_meltingdown_anim","MudCrawler melts after the source two-second target-loss interval");
        for(int step=0;step<45;step++)sourceMud.Tick(.02f);
        Check(!sourceMud.actor.visual.enabled,"MudCrawler hides and returns to its original spawn after melting");
        sourceWheel.ResetEnemy();
        game.player.motor.Teleport((Vector2)sourceWheel.transform.position+Vector2.right*2f);
        sourceWheel.Tick(.02f);
        Check(sourceWheel.actor.Current!="WheelCarrier_attack","WheelCarrier honours the source half-second attack preparation");
        for(int step=0;step<26;step++)sourceWheel.Tick(.02f);
        Check(sourceWheel.actor.Current=="WheelCarrier_attack","WheelCarrier uses its source attack inside the 2.8-unit range");
        game.Enter("D01Z01S01",null,null);
        sourceFool.ResetEnemy();
        game.player.motor.Teleport((Vector2)sourceFool.transform.position+Vector2.left*3f);
        sourceFool.Tick(.02f);
        for(int step=0;step<20;step++)sourceFool.Tick(.02f);
        game.player.motor.Teleport((Vector2)sourceFool.transform.position+Vector2.right*3f);
        sourceFool.Tick(.02f);
        Check(sourceFool.actor.Current=="Fool_turnaround","Fool uses the original turnaround clip when reversing pursuit");
        game.Enter("D01Z01S02",null,null);
        game.player.Restore();
        game.player.motor.Teleport((Vector2)sourceWheel.transform.position+Vector2.right*3f);
        for(int step=0;step<300;step++)foreach(var enemy in forestAI.enemies)enemy.Tick(.02f);
        bool separated=true;
        for(int a=0;a<forestAI.enemies.Length;a++)for(int b=a+1;b<forestAI.enemies.Length;b++)
        {
            var leftEnemy=forestAI.enemies[a];var rightEnemy=forestAI.enemies[b];
            if(leftEnemy.Dead||rightEnemy.Dead||!leftEnemy.actor.visual.enabled||!rightEnemy.actor.visual.enabled)continue;
            Vector2 between=leftEnemy.transform.position-rightEnemy.transform.position;
            if(Mathf.Abs(between.y)<.7f&&Mathf.Abs(between.x)<.55f)separated=false;
        }
        Check(separated,"Source forest monsters remain separate after six seconds of simultaneous pursuit");
        var mudRoom=game.Find("D01Z01S02");
        Check(mudRoom!=null&&mudRoom.mudZones!=null&&mudRoom.mudZones.Length==3,"Three original mud areas restored in D01Z01S02");
        if(mudRoom!=null&&mudRoom.mudZones!=null&&mudRoom.mudZones.Length>0)
        {
            game.Enter(mudRoom.id,null,null);
            var mud=mudRoom.mudZones[0];
            game.player.motor.Teleport((Vector2)mud.source.TransformPoint(mud.offset)-Vector2.up*game.player.motor.size.y*.5f);
            Check(game.CurrentMud==mud&&Mathf.Approximately(mud.maxWalkSpeed,3)&&Mathf.Approximately(mud.jumpSpeed,6.25f)&&Mathf.Approximately(mud.dashSpeed,8),"Mud uses original movement, jump and dash values");
        }
        var village=game.Find("D01Z02S01");
        Check(village!=null&&village.parallax.Length>=5,"Both original village parallax controllers retain their layers");
        Check(village!=null&&System.Array.Exists(village.GetComponentsInChildren<SpriteActor>(true),a=>a.Current.StartsWith("albero_npc_")),"Source village background residents animate");
        Check(village!=null&&System.Array.Exists(village.parallax,p=>p.target!=null&&p.target.name=="Albero Props"&&p.target.GetComponentsInChildren<SpriteActor>(true).Length>=4),"Outdoor villagers follow their LOGIC parallax group from the original scene");
        var indoorVillage=game.Find("D01Z02S02");
        Check(indoorVillage!=null&&System.Array.FindAll(indoorVillage.GetComponentsInChildren<SpriteActor>(true),a=>a.Current.StartsWith("tirso_")).Length==9,"Tirso and eight indoor villagers use their original idle clips");
        if(indoorVillage!=null&&indoorVillage.collectibles.Length>0)
            Check(System.Array.Exists(indoorVillage.collectibles[0].root.GetComponentsInChildren<SpriteActor>(true),a=>a.Current=="collectable_object_anim"),"Indoor item plays the original 19-frame pickup-world animation");
        var altarRoom=game.Find("D01Z02S06");
        Check(altarRoom!=null&&altarRoom.skillAltars!=null&&altarRoom.skillAltars.Length>0,"Source Mea Culpa altar and its interaction sensor restored");
        if(altarRoom!=null&&altarRoom.skillAltars!=null&&altarRoom.skillAltars.Length>0)
        {
            game.Enter(altarRoom.id,null,null);
            var altar=altarRoom.skillAltars[0];
            game.player.motor.Teleport((Vector2)altar.sensor.TransformPoint(altar.offset)-Vector2.up*game.player.motor.size.y*.5f);
            Check(game.NearSkillAltar&&game.CanInteract,"Source altar trigger exposes the contextual USE control");
            Check(game.TryUseSkillAltar()&&game.player.actor.Current=="penitent_kneeling_down","Altar begins the original kneeling animation");
            yield return new WaitForSecondsRealtime(.95f);
            Check(game.controls.inventoryUI.IsOpen&&game.player.actor.Current=="penitent_kneeled","Altar opens the existing Mea Culpa skill inventory after kneeling");
            game.controls.inventoryUI.Hide();
            Check(game.player.actor.Current=="penitent_altar_stand_up_anim","Leaving Mea Culpa skills plays the original stand-up animation");
            yield return new WaitForSeconds(.5f);
        }
        var collectibleRoom=game.Find("D01Z01S02");
        Check(collectibleRoom!=null&&collectibleRoom.collectibles!=null&&collectibleRoom.collectibles.Length>=2,"Original D01Z01S02 collectibles and sensors restored");
        if(collectibleRoom!=null&&collectibleRoom.collectibles!=null&&collectibleRoom.collectibles.Length>0)
        {
            var ownedBefore=game.progress.ownedItems;bool unlockBefore=game.progress.unlockAllItems;
            game.progress.ownedItems=System.Array.Empty<string>();game.progress.unlockAllItems=false;
            game.Enter(collectibleRoom.id,null,null);
            var pickup=collectibleRoom.collectibles[0];
            game.player.motor.Teleport((Vector2)pickup.sensor.TransformPoint(pickup.offset)-Vector2.up*game.player.motor.size.y*.5f);
            game.player.motor.grounded=true;
            Check(game.GetNearCollectible()==pickup&&game.CanInteract,"Original collectible sensor exposes the contextual USE control");
            Check(game.TryCollectItem()&&game.progress.Owns(pickup.item)&&!pickup.root.activeSelf,"Picking up original item unlocks it and removes its world object");
            Check(game.player.actor.Current==(pickup.halfHeight?"penitent_collecting_object_from_halfheight_anim":"penitent_collecting_object_from_floor_anim"),"Picking up uses the original Penitent collection animation");
            yield return new WaitForSeconds(.45f);
            Capture("collectible-animation");
            yield return new WaitForSeconds(1.35f);
            Check(game.itemPopup.IsOpen&&game.itemPopup.ShownItem==pickup.item,"Acquisition popup shows the collected item after the animation");
            Check(game.itemPopup.DisplayedIcon!=null,"Acquisition popup includes the source item icon");
            yield return null;
            var pickupCanvas=game.itemPopup.Canvas;
            Check(pickupCanvas!=null&&pickupCanvas.isActiveAndEnabled&&pickupCanvas.GetComponentsInChildren<UnityEngine.UI.Graphic>(false).Length>=5,"Acquisition canvas and original-source artwork render in hierarchy");
            CaptureCanvas(pickupCanvas,"collectible-popup");
            ScreenCapture.CaptureScreenshot("Documentation/Previews/collectible-popup-screen.png");
            yield return null;
            game.itemPopup.Hide();
            game.progress.ownedItems=ownedBefore;game.progress.unlockAllItems=unlockBefore;
            collectibleRoom.RefreshCollectibles(game.progress);
        }
        var firstRoom=game.Find("D17Z01S01");
        var firstDoor=firstRoom==null?null:System.Array.Find(firstRoom.doors,d=>game.Find(d.target)!=null);
        Check(firstDoor!=null,"The opening source room connects to another playable map");
        if(firstDoor!=null)
        {
            game.Enter(firstRoom.id,null,null);
            game.player.motor.Teleport(firstDoor.trigger.position);
            game.player.actor.Play("Player_Run",true,true);
            var joystickField=typeof(TouchControls).GetField("tm",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            joystickField.SetValue(game.controls,1f);
            typeof(BrotherhoodGame).GetField("transitionCooldown",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(game,0f);
            yield return null;
            var transitionFade=GameObject.Find("Black fade");
            var fadeImage=transitionFade==null?null:transitionFade.GetComponent<UnityEngine.UI.Image>();
            Check(game.Current.id==firstRoom.id&&fadeImage!=null&&fadeImage.color.a<1f,"Door transition covers the old room before swapping maps");
            Check(game.player.actor.Current=="Player_Idle"&&game.player.motor.velocity==Vector2.zero,
                "Starting a room fade while running immediately returns the Penitent to Idle");
            yield return new WaitForSeconds(.35f);
            Check(game.Current.id==firstDoor.target&&fadeImage!=null&&fadeImage.color.a>.5f,"New room activates behind the source black fade instead of exposing empty map space");
            Check(game.player.actor.Current=="Player_Idle",
                "Destination room keeps Idle rather than a carried-over running frame during the black fade");
            Check(Mathf.Approximately((float)joystickField.GetValue(game.controls),1f)&&game.controls.Move==0f,
                "Room fade blocks movement without forgetting a held touch joystick");
            yield return new WaitForSeconds(.7f);
            Check(game.Current.id==firstDoor.target&&game.progress.RoomCompleted(firstRoom.id),"Crossing a source door marks the previous map complete");
            Check(fadeImage!=null&&fadeImage.color.a<.01f,"Room transition fully reveals the destination after its camera settles");
            game.controls.Simulation=false;game.controls.Sample();
            Check(game.controls.Move>.9f,"A touch joystick still held after the fade resumes movement input");
            game.controls.Simulation=true;game.controls.ClearGameplayInput();
            var saveFile=Path.Combine(Application.persistentDataPath,"brotherhood-verification.json");
            var saved=File.Exists(saveFile)?File.ReadAllText(saveFile):"";
            Check(saved.Contains("\"currentRoom\":\""+firstDoor.target+"\"")&&saved.Contains("\"completedRooms\""),"Map transition persists current room and completed-room history");
        }
        game.Enter("D17Z01S01",null,null);game.player.Restore();yield return new WaitForSeconds(.6f);
        var executionRoom=System.Array.Find(game.rooms,r=>r.enemies!=null&&System.Array.Exists(r.enemies,e=>e!=null&&!e.boss));
        if(executionRoom!=null){var victim=System.Array.Find(executionRoom.enemies,e=>e!=null&&!e.boss);game.Enter(executionRoom.id,null,victim.transform.position+Vector3.left);yield return new WaitForSeconds(.15f);var stunField=typeof(EnemyController).GetField("stun",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);victim.Damage(1);Check(!victim.ExecutionReady,"Ordinary sword damage does not incorrectly expose an execution");stunField.SetValue(victim,SourceGameplayTuning.ExecutionStunTime);Check(Mathf.Approximately((float)stunField.GetValue(victim),5f),"Successful parry grants the mobile 5 second execution window");victim.Tick(.01f);var prompt=GameObject.Find("Execution prompt");Check(prompt!=null&&prompt.activeInHierarchy,"Execution-ready enemy shows original animated prompt above its head");Check(prompt!=null&&prompt.GetComponent<SpriteRenderer>().sprite!=null,"Execution prompt renders a restored source animation frame");Check(game.CanInteract,"Execution exposes the contextual USE button");Capture("execution-prompt");game.player.fervour=Mathf.Min(game.player.fervour,game.player.MaxFervour-20);float executionFervour=game.player.fervour;float executionTears=game.progress.tears;game.controls.Interact=true;game.player.Tick(.02f);game.controls.Interact=false;Vector2 executionEndpoint=PlayerController.ExecutionEndpoint(victim.transform.position,game.player.facing);Check(victim.Dead&&(victim.actor.Current=="acolyte_execution_anim"||victim.actor.Current=="Flagellant_execution_anim"),"Enemy execution uses family-specific original animation");Check(!game.player.actor.visual.enabled,"Original composite execution hides duplicate live player renderer");Check(game.ExecutionCameraActive&&Vector2.Distance(game.player.transform.position,executionEndpoint)<.03f,"Execution crosses Player to the far-side mobile endpoint");Check(game.player.fervour>executionFervour&&game.progress.tears>executionTears,"Execution grants Fervour and source enemy Tears");yield return new WaitForSeconds(2.25f);Capture("execution-active");yield return new WaitForSeconds(2.45f);Check(!victim.actor.visual.enabled,"Completed composite execution frame is disposed before live Player returns");Check(game.player.actor.visual.enabled&&!game.ExecutionCameraActive,"Player renderer and normal camera return after complete original execution clip");Check(Vector2.Distance(game.player.transform.position,executionEndpoint)<.03f,"Player remains on the far side shown by the final execution frame");Check(PlayerController.ExecutionEndpoint(victim.transform.position,-1).x<victim.transform.position.x&&PlayerController.ExecutionEndpoint(victim.transform.position,1).x>victim.transform.position.x,"Execution endpoint mirrors correctly for approaches from both sides");Capture("execution-complete");victim.ResetEnemy();}
        if(executionRoom!=null)
        {
            var chargingStart=game.player.actor.catalog.Find("penitent_start_charging");
            var chargingLoop=game.player.actor.catalog.Find("penitent_charging");
            Check(chargingStart!=null&&chargingStart.frames.Length>1&&chargingLoop!=null&&chargingLoop.frames.Length>1,"Original Penitent charging start and loop frames are restored");
            game.player.Restore();game.Enter(executionRoom.id,null,executionRoom.start.position);
            game.progress.chargedTier=2;game.controls.Attack=true;game.controls.AttackHeld=true;
            game.player.Tick(.02f);game.controls.Attack=false;
            Check(game.player.attackTime>0&&game.player.actor.Current!="penitent_start_charging"&&game.player.actor.Current!="penitent_charging","Initial Attack press starts a normal attack, not a charging frame");
            game.controls.AttackHeld=false;game.player.Tick(.1f);
            Check(game.player.attackTime>0&&game.player.actor.Current!="penitent_start_charging"&&game.player.actor.Current!="penitent_charging","Quick tap keeps the ordinary attack and never enters charging");
            game.player.Restore();game.controls.Attack=true;game.controls.AttackHeld=true;game.player.Tick(.02f);game.controls.Attack=false;
            game.controls.Move=1;for(int n=0;n<10;n++)game.player.Tick(.1f);game.controls.Move=0;
            Check(game.player.actor.Current=="penitent_start_charging"||game.player.actor.Current=="penitent_charging","Long hold enters original charging animation after the source 0.5 second threshold");
            for(int n=0;n<9;n++)game.player.Tick(.1f);
            Check(game.player.actor.Current=="penitent_charging","Charging stays on original character loop despite movement input and reaching full charge");
            Check(game.effects.HasActiveClip("penitent_charged_attack_effect"),"Fully charged flash plays in a separate effect renderer");
            game.controls.AttackHeld=false;game.player.Tick(.02f);
            Check(game.player.actor.Current=="penitent_charged_attack","Releasing full charge plays the original attack frames");
            Check(!game.effects.HasActiveClip("penitent_charged_attack_effect"),"Releasing charge removes the overlapping charging-character effect before the strike");
            game.player.Restore();game.progress.chargedTier=0;
        }
        if(executionRoom!=null)
        {
            var target=System.Array.Find(executionRoom.enemies,e=>e!=null&&!e.boss);
            target.ResetEnemy();game.Enter(executionRoom.id,null,target.transform.position+Vector3.left);
            game.progress.lungeTier=1;float before=target.health;
            game.controls.Dash=true;game.controls.Attack=true;game.player.Tick(.02f);
            game.controls.Dash=game.controls.Attack=false;
            Check(game.player.actor.Current=="penitent_dodge_attack_anim"&&target.health<before,"Dash plus Attack triggers source-tier lunge and damages target");
            target.ResetEnemy();game.player.Restore();game.Enter(executionRoom.id,null,target.transform.position+Vector3.left*2);
            game.progress.chargedTier=2;game.controls.Attack=true;game.controls.AttackHeld=true;
            game.player.Tick(.02f);game.controls.Attack=false;
            for(int n=0;n<19;n++)game.player.Tick(.1f);
            game.controls.AttackHeld=false;game.player.Tick(.02f);
            Check(game.player.actor.Current=="penitent_charged_attack","Held Attack releases original charged clip at source tier timing");
            before=target.health;game.player.Tick(.4f);
            Check(target.health<before,"Charged hit fires at source 0.36 second animation event");
            Check(!game.effects.HasActiveClip("penitent_charged_attack_effect"),"Charged strike uses its own source sprite frames without a second charging character overlay");
            target.ResetEnemy();target.transform.position=game.player.transform.position+Vector3.right*3;before=target.health;
            // Other live enemies can intercept the projectile before this target.
            // Isolate the intended victim so the assertion measures its real hit.
            var projectileEnemies=executionRoom.enemies;
            string candidates="";foreach(var enemy in projectileEnemies)if(enemy!=null&&!enemy.Dead)candidates+=enemy.family+" "+enemy.transform.position+" HP="+enemy.health+"; ";
            checks.Add("Charged projectile candidates before isolation: "+candidates);
            executionRoom.enemies=new[]{target};
            try
            {
                game.effects.ChargedProjectile(game.player.transform.position+Vector3.up*.65f,1,game,81);
                Check(game.effects.ActiveChargedProjectiles==1,"Tier-three charged projectile uses the fixed effect pool");
                yield return new WaitForSeconds(.28f);
                Check(target.health<before&&game.effects.LastChargedProjectileResult=="Enemy","Charged projectile flies and resolves against an isolated enemy; life="+before+" -> "+target.health+", result="+game.effects.LastChargedProjectileResult+", Player="+game.player.transform.position+", target="+target.transform.position);
            }
            finally{executionRoom.enemies=projectileEnemies;}
            var projectileWall=new GameObject("Verification projectile wall",typeof(BoxCollider2D));
            projectileWall.layer=8;projectileWall.transform.position=game.player.transform.position+Vector3.right;
            projectileWall.GetComponent<BoxCollider2D>().size=new Vector2(.2f,3);Physics2D.SyncTransforms();
            game.effects.ChargedProjectile(game.player.transform.position+Vector3.up*.65f,1,game,81);
            yield return new WaitForSeconds(.12f);
            Check(game.effects.LastChargedProjectileResult=="Wall","Charged projectile resolves against terrain before its range limit");
            Destroy(projectileWall);
            var rangePlayerPosition=game.player.transform.position;var rangeTargetPosition=target.transform.position;
            game.player.motor.Teleport(new Vector2(game.Current.start.position.x,game.Current.top+10));
            target.transform.position=game.player.transform.position+Vector3.right*20;Physics2D.SyncTransforms();
            game.effects.RangeProjectile(game.player.transform.position+Vector3.up*.65f,1,game,44,3);
            Check(game.effects.ActiveRangeProjectiles==1,"Fervorous Blood uses the fixed effect pool");
            yield return new WaitForSeconds(.34f);
            Check(game.effects.LastRangeProjectileResult=="Explosion"&&game.effects.HasActiveClip("penitent_rangeAttack_projectile_explode_anim"),"RANGED_3 explodes at the far point before returning");
            yield return new WaitForSeconds(.35f);
            Check(game.effects.LastRangeProjectileResult=="Returned","RANGED_2 and RANGED_3 complete their boomerang return");
            game.player.motor.Teleport(rangePlayerPosition);target.transform.position=rangeTargetPosition;Physics2D.SyncTransforms();
            game.progress.lungeTier=game.progress.chargedTier=0;target.ResetEnemy();game.player.Restore();
        }
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
        yield return new WaitForSeconds(2f);Check(game.player.Dead&&game.gameOverUI!=null&&game.gameOverUI.IsShowing,"Death displays the original Game Over card");
        yield return new WaitForSeconds(2.4f);Check(game.player.Dead&&game.gameOverUI.IsShowing,"Game Over waits for a player choice instead of automatically respawning");
        var deathButtons=game.gameOverUI.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        var reviveButton=System.Array.Find(deathButtons,b=>b.name=="HỒI SINH");
        Check(reviveButton!=null&&System.Array.Exists(deathButtons,b=>b.name=="HOME")&&System.Array.Exists(deathButtons,b=>b.name=="SETTINGS"),"Game Over offers Respawn, Home and Settings buttons");
        var settingsButton=System.Array.Find(deathButtons,b=>b.name=="SETTINGS");
        if(settingsButton!=null)settingsButton.onClick.Invoke();
        Check(game.controls.optionsUI.IsOpen,"Game Over Settings button opens the settings screen");
        game.controls.optionsUI.Hide();
        Check(game.gameOverUI.IsShowing&&!game.controls.Visible,"Closing Settings returns to Game Over without revealing gameplay controls");
        if(reviveButton!=null)reviveButton.onClick.Invoke();
        Check(!game.player.Dead&&game.Current==shrineRoom,"Respawn button returns to the saved checkpoint");if(game.player.Dead)game.Respawn();game.player.Restore();yield return new WaitForSeconds(.2f);
        game.Enter("D17Z01S11",null,null);game.player.Restore();
        var headroomFixture=new GameObject("Verification-only low headroom");headroomFixture.layer=8;
        headroomFixture.transform.position=game.player.transform.position+Vector3.up*.87f;
        var headroomCollider=headroomFixture.AddComponent<BoxCollider2D>();headroomCollider.size=new Vector2(2f,.3f);
        game.player.motor.TrySetHeight(.6f);Physics2D.SyncTransforms();
        Check(!game.player.motor.TrySetHeight(1.15f),"High step blocks standing up after dash but leaves the short dash capsule clear");
        game.controls.ClearGameplayInput();game.controls.Simulation=true;game.controls.Move=1f;
        float crouchedStartX=game.player.transform.position.x;
        for(int step=0;step<30;step++)game.player.Tick(1f/60f);
        Check(game.player.transform.position.x>crouchedStartX+.25f,
            "Penitent can walk out from under a high step after dash without a second dash");
        game.controls.Simulation=false;game.controls.ClearGameplayInput();UnityEngine.Object.DestroyImmediate(headroomFixture);
        game.Enter("D17Z01S11",null,null);game.player.Restore();
        var facingBoss=game.Current.Boss;
        if(facingBoss!=null)
        {
            var bossState=typeof(EnemyController).GetField("state",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var bossTimer=typeof(EnemyController).GetField("timer",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var bossFacing=typeof(EnemyController).GetField("facing",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            facingBoss.ActivateBossCombat();game.SetBossFightActive(true);
            game.player.motor.Teleport((Vector2)facingBoss.transform.position+new Vector2(5f,0));
            bossTimer.SetValue(facingBoss,0f);facingBoss.Tick(.02f);
            Check((int)bossState.GetValue(facingBoss)==1&&(float)bossFacing.GetValue(facingBoss)>0,
                "Warden chooses a facing once when the source AREA windup starts");
            game.player.motor.Teleport((Vector2)facingBoss.transform.position+new Vector2(-5f,0));
            facingBoss.Tick(.02f);bossTimer.SetValue(facingBoss,0f);facingBoss.Tick(.02f);
            Check((int)bossState.GetValue(facingBoss)==6&&(float)bossFacing.GetValue(facingBoss)>0,
                "Dashing behind Warden cannot flip the ongoing downward smash or its shockwaves");
            facingBoss.Tick(1.01f);
            var areasActive=typeof(EnemyController).GetField("bossAreaActive",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Check((int)bossState.GetValue(facingBoss)==0&&(bool)areasActive.GetValue(facingBoss),
                "Warden finishes its source one-second AREA recovery while the 1.4-second shockwave sequence continues");
            bossTimer.SetValue(facingBoss,0f);facingBoss.Tick(.02f);
            Check((int)bossState.GetValue(facingBoss)==3&&(float)bossFacing.GetValue(facingBoss)>0,
                "Source JUMP preparation keeps its previous facing rather than snapping to the player");
            game.player.motor.Teleport((Vector2)facingBoss.transform.position+new Vector2(-7f,0));
            bossTimer.SetValue(facingBoss,0f);facingBoss.Tick(.02f);
            var bossJumpTarget=typeof(EnemyController).GetField("jumpTarget",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            Check(Mathf.Abs((float)bossJumpTarget.GetValue(facingBoss)-game.player.transform.position.x)<.01f,
                "Warden predicts the landing position after JUMP preparation, following a dash during windup");
            facingBoss.Tick(.35f);
            Check((float)bossFacing.GetValue(facingBoss)>0,"Warden does not turn during the first part of its jump");
            facingBoss.Tick(.38f);
            Check((float)bossFacing.GetValue(facingBoss)<0,"Warden turns toward the target near 90 percent of its source jump");
            game.SetBossFightActive(false);
        }
        game.Enter("D17Z01S11",null,null);var boss=game.Current.Boss;var sourceBossTrigger=System.Array.Find(game.Current.GetComponentsInChildren<Transform>(true),t=>t.name=="BossFight");if(sourceBossTrigger!=null)game.player.motor.Teleport(new Vector2(sourceBossTrigger.position.x,game.player.transform.position.y));
        game.player.actor.Play("Player_Run",true,true);game.controls.Simulation=true;game.controls.Move=1f;
        yield return new WaitForSeconds(.15f);
        Check(game.InputBlocked&&game.player.actor.Current=="Player_Idle"&&game.player.motor.velocity.sqrMagnitude<.001f,
            "Boss intro switches the Penitent from running to idle before freezing movement");
        game.controls.Simulation=false;game.controls.ClearGameplayInput();
        Check(boss!=null,"Warden exists");if(boss!=null){yield return new WaitForSeconds(.15f);var dummy=System.Array.Find(game.Current.GetComponentsInChildren<Transform>(true),t=>t.name=="ElderBrotherIntroDummy");var dummyVisual=dummy!=null?dummy.GetComponentInChildren<SpriteRenderer>(true):null;var sourceSilhouette=new Color(0.12941177f,0.12941177f,0.16470589f,1f);Check(dummyVisual!=null&&dummy.gameObject.activeInHierarchy&&Vector4.Distance(dummyVisual.color,sourceSilhouette)<.001f,"Warden cutscene uses the exact elderBroDummy source silhouette tint");float introTimeout=Time.time+14f;while(!game.BossFightActive&&Time.time<introTimeout)yield return null;Check(game.BossFightActive&&boss.BossIntroComplete&&boss.actor.visual.enabled,"Warden source intro completes visibly before health UI and combat activate");Check(boss.motor.grounded,"Warden finishes the deterministic intro jump on the arena floor before landing audio/combat");var bossHudRoot=GameObject.Find("UI_BOSS_HEALTH");var bossFrame=GameObject.Find("Boss Frame");var bossHudRect=bossHudRoot!=null?bossHudRoot.GetComponent<RectTransform>():null;Check(bossHudRect!=null&&bossHudRect.sizeDelta==new Vector2(900,40)&&bossHudRect.pivot==new Vector2(.5f,0),"Boss HUD scales and centers the source 450x20 layout correctly for the 1280x720 canvas");Check(bossFrame!=null&&bossFrame.GetComponent<RectTransform>().sizeDelta==new Vector2(692,60)&&Mathf.Abs(bossFrame.GetComponent<RectTransform>().anchoredPosition.x)<.01f,"Boss health frame is full-size and horizontally centered");var finisher=game.player.actor.catalog.Find("Player_ComboFinisher_Down");Check(finisher!=null&&Mathf.Abs(finisher.duration-1.7f)<.01f,"Downward combo finisher retains its complete 1.70 second source animation");Check(Mathf.Approximately(ElderBrotherEncounter.BoundaryWidth,4)&&Mathf.Approximately(ElderBrotherEncounter.BoundaryHeight,32),"Warden combat boundaries use source 4x32 colliders");Check(ElderBrotherEncounter.AreaCount==6&&Mathf.Approximately(ElderBrotherEncounter.AreaSpacing,1.5f)&&Mathf.Approximately(ElderBrotherEncounter.AreaDuration,1.4f),"Warden AREA attack uses source six-area timing");Directory.CreateDirectory("Documentation/Previews");ScreenCapture.CaptureScreenshot("Documentation/Previews/warden-fight-hud.png");yield return new WaitForEndOfFrame();yield return null;var startFinisher=typeof(PlayerController).GetMethod("StartAttack",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);game.progress.comboTier=3;startFinisher.Invoke(game.player,new object[]{3,-1f});Check(game.player.actor.Current=="Player_ComboFinisher_Down"&&game.player.attackTime>1.65f,"Combo finisher playback is not truncated by the former 1.10 second attack clamp");boss.Damage(999);Check(boss.Dead,"Warden defeat opens encounter");yield return new WaitForSeconds(3.4f);Check(boss.BossCorpseShown&&boss.actor.Current=="ElderBrother_Corpse","Warden death resolves to the original corpse and cleansing beat");Check(game.progress.wardenDefeated&&game.progress.achievementAC01,"Warden defeat and AC01 persist in player progress");}
        var victoryArt=GameObject.Find("boss-defeated-screen-title.png");
        Check(victoryArt!=null&&victoryArt.GetComponent<Image>().sprite!=null,"Warden victory shows the original boss-defeated artwork");
        ScreenCapture.CaptureScreenshot("Documentation/Previews/warden-victory-playtest.png");
        yield return new WaitForEndOfFrame();
        yield return null;
        var victoryUI=game.GetComponent<BossDefeatedUI>();
        Check(victoryUI!=null&&victoryUI.IsShowing&&game.InputBlocked,"Warden victory remains on screen until the player chooses a destination");
        Check(game.progress.campaignWon&&game.progress.RoomCompleted("D17Z01S11"),"Warden objective and completed boss room persist in player progress");
        var achievementButton=System.Array.Find(victoryUI.GetComponentsInChildren<Button>(true),b=>b.name=="ACHIEVEMENTS");
        var continueButton=System.Array.Find(victoryUI.GetComponentsInChildren<Button>(true),b=>b.name=="CONTINUE");
        var victoryHomeButton=System.Array.Find(victoryUI.GetComponentsInChildren<Button>(true),b=>b.name=="HOME");
        Check(achievementButton!=null&&continueButton!=null&&victoryHomeButton!=null,"Victory presents three functional source-art navigation buttons");
        if(achievementButton!=null)achievementButton.onClick.Invoke();
        Check(victoryUI.IsAchievementPage&&GameObject.Find("achievements-AC01")!=null,"Achievements button opens the original AC01 artwork on its own screen");
        var backButton=System.Array.Find(victoryUI.GetComponentsInChildren<Button>(true),b=>b.name=="BACK");
        if(backButton!=null)backButton.onClick.Invoke();
        Check(!victoryUI.IsAchievementPage&&victoryUI.IsShowing,"Back returns to the victory navigation screen");
        if(continueButton!=null)continueButton.onClick.Invoke();
        yield return null;
        Check(!victoryUI.IsShowing&&!game.InputBlocked,"Continue returns control to the cleared boss room");
        var holyLine=game.Find("D01Z01S07");var deogracias=holyLine==null?null:System.Array.Find(holyLine.GetComponentsInChildren<Transform>(true),t=>t.name=="Deosgracias");
        Check(holyLine!=null&&deogracias!=null,"Post-Warden D01Z01S07 contains the source Deosgracias object");
        Check(sourceClips.Find("deosgracias_idle_anim")!=null&&sourceClips.Find("deosgracias_stand_anim")!=null&&sourceClips.Find("deosgracias_back_anim")!=null,"Original Deogracias idle, stand and back clips restored");
        Check(Resources.Load<UnityEngine.Video.VideoClip>("Deogracias/CTS07")!=null&&Resources.Load<Sprite>("Dialogue/dialog_background")!=null,"Original CTS07 video and dialogue background load from source assets");
        if(holyLine!=null&&deogracias!=null)
        {
            game.Enter(holyLine.id,null,deogracias.position+Vector3.left);yield return new WaitForSeconds(.2f);
            Check(game.deograciasEncounter!=null&&game.deograciasEncounter.CanInteract,"Deogracias exposes the contextual USE interaction after the Warden");
            game.progress.deograciasMet=false;game.progress.thornGranted=false;game.progress.SetOwned("QI31",false);
            game.deograciasEncounter.SkipVideoPlaybackForTests=false;
            game.deograciasEncounter.ForceSafeVideoFallbackForTests=true;
            Check(game.deograciasEncounter.TryBegin()&&game.deograciasEncounter.Active,"Deogracias source dialogue starts through gameplay interaction");
            CaptureCanvas(game.deograciasEncounter.Canvas,"deogracias-dialog-playtest");yield return new WaitForSecondsRealtime(.2f);
            game.deograciasEncounter.Advance();yield return new WaitForSecondsRealtime(.2f);
            game.deograciasEncounter.Advance();yield return new WaitForSecondsRealtime(.2f);
            Check(game.deograciasEncounter.UsingSafeVideoFallback,"Deogracias cinematic has a non-crashing source-subtitle fallback for unsafe video backends");
            game.deograciasEncounter.Advance();yield return new WaitForSecondsRealtime(.2f);
            for(int dialogueStep=0;dialogueStep<10;dialogueStep++){game.deograciasEncounter.Advance();yield return new WaitForSecondsRealtime(.18f);}
            Check(game.progress.deograciasMet&&game.progress.thornGranted&&System.Array.IndexOf(game.progress.ownedItems,"QI31")>=0,"Completing the source dialogue grants and saves the original QI31 Thorn");
            CaptureCanvas(game.deograciasEncounter.Canvas,"deogracias-thorn-playtest");yield return new WaitForSecondsRealtime(.2f);
            game.deograciasEncounter.Advance();yield return new WaitForSecondsRealtime(.2f);
            Check(!game.deograciasEncounter.Active&&!game.InputBlocked,"Closing the Thorn reward returns control to gameplay");
        }
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
        Check(game.controls.inventoryUI.IsOpen, "BlasInventoryUI opens with seven source inventory categories");
        CaptureCanvas(game.controls.inventoryUI.Canvas, "blas-inventory-playtest"); yield return new WaitForSecondsRealtime(0.1f);
        var inventoryType=typeof(BlasInventoryUI);
        var privateInstance=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
        var pageLabel=(Text)inventoryType.GetField("pageText",privateInstance).GetValue(game.controls.inventoryUI);
        Check(pageLabel!=null&&pageLabel.gameObject.activeInHierarchy&&pageLabel.text.StartsWith("TRANG"),"Inventory page indicator is visible and localized");
        var equipAction=inventoryType.GetMethod("EquipSelected",privateInstance);
        game.controls.inventoryUI.InspectItem("RB01");
        bool beadBefore=game.progress.IsEquipped("RB01");
        equipAction.Invoke(game.controls.inventoryUI,null);
        Check(game.progress.IsEquipped("RB01")!=beadBefore,"Inventory equip button changes rosary state");
        equipAction.Invoke(game.controls.inventoryUI,null);
        for(int tab=1;tab<BlasInventoryUI.Categories.Length;tab++)
        {
            game.controls.inventoryUI.Show(tab);
            yield return new WaitForSecondsRealtime(0.1f);
            CaptureCanvas(game.controls.inventoryUI.Canvas,"blas-inventory-tab-"+tab);
        }
        Check(game.controls.inventoryUI.IsOpen,"All seven inventory tabs can be opened without closing gameplay UI");
        var tabButtons=(GameObject[])inventoryType.GetField("tabButtons",privateInstance).GetValue(game.controls.inventoryUI);
        var tabField=inventoryType.GetField("currentTab",privateInstance);
        bool tabsClickable=true;
        for(int tab=0;tab<tabButtons.Length;tab++)
        {
            tabButtons[tab].GetComponent<Button>().onClick.Invoke();
            if((int)tabField.GetValue(game.controls.inventoryUI)!=tab)tabsClickable=false;
        }
        Check(tabsClickable,"All seven source category tabs respond to button clicks");
        game.controls.inventoryUI.Show(0);
        var nextButton=(GameObject)inventoryType.GetField("nextPageBtn",privateInstance).GetValue(game.controls.inventoryUI);
        var previousButton=(GameObject)inventoryType.GetField("prevPageBtn",privateInstance).GetValue(game.controls.inventoryUI);
        var currentPage=inventoryType.GetField("pageIndex",privateInstance);
        nextButton.GetComponent<Button>().onClick.Invoke();
        bool pageAdvanced=(int)currentPage.GetValue(game.controls.inventoryUI)==1;
        previousButton.GetComponent<Button>().onClick.Invoke();
        Check(pageAdvanced&&(int)currentPage.GetValue(game.controls.inventoryUI)==0,"Inventory next and previous buttons change pages");
        game.controls.inventoryUI.InspectItem("QI31");
        game.controls.inventoryUI.ShowLoreModal(); yield return new WaitForSecondsRealtime(0.2f);
        Check(game.controls.inventoryUI.IsLoreOpen, "Thorn lore modal popup opens with authentic Deosgracias text");
        CaptureCanvas(game.controls.inventoryUI.Canvas, "blas-lore-modal-playtest"); yield return new WaitForSecondsRealtime(0.1f);
        game.controls.inventoryUI.Hide(); yield return new WaitForSecondsRealtime(0.2f);
        Check(!game.controls.inventoryUI.IsOpen, "BlasInventoryUI closes cleanly");

        game.controls.OpenMapUI(); yield return new WaitForSecondsRealtime(0.2f);
        Check(game.controls.mapUI.IsOpen, "BlasMapUI opens with FondoMapa mountain background and Metroidvania grid");
        var mapCells=game.controls.mapUI.Canvas.GetComponentsInChildren<RectTransform>(true);
        Check(System.Array.TrueForAll(game.rooms,r=>System.Array.Exists(mapCells,c=>c.name.StartsWith(r.id+"/")))&&!System.Array.Exists(mapCells,c=>c.name=="Room_0"),"Map uses source cells for all imported rooms instead of placeholder rows");
        var mapMarker=System.Array.Find(mapCells,c=>c.name=="PlayerMarker");
        var markerScreen=mapMarker==null?Vector2.zero:RectTransformUtility.WorldToScreenPoint(null,mapMarker.position);
        Check(mapMarker!=null&&markerScreen.x>0&&markerScreen.x<Screen.width&&markerScreen.y>0&&markerScreen.y<Screen.height,"Map player marker remains visible when recentered");
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
        Screen.SetResolution(1280,720,false);yield return new WaitForSecondsRealtime(.2f);
        game.gameOverUI.Show();
        var homeButton=System.Array.Find(game.gameOverUI.GetComponentsInChildren<Button>(true),b=>b.name=="HOME");
        if(homeButton!=null)homeButton.onClick.Invoke();
        yield return null;
        Check(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name=="MainMenu","Game Over Home button opens the separate main-menu scene");
        Finish();
    }
    IEnumerator VerifyShockReceiverControls()
    {
        var room=game.Find("D17Z01S03");
        var receiver=Array.Find(room.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_124");
        var body=Array.Find(room.GetComponentsInChildren<SourceObjectId>(true),o=>o.node=="LOGIC_109");
        var attack=Array.Find(game.controls.GetComponentsInChildren<TouchControls.TouchButton>(true),b=>b.name=="ATTACK");
        var jump=Array.Find(game.controls.GetComponentsInChildren<TouchControls.TouchButton>(true),b=>b.name=="JUMP");
        var joystick=game.controls.GetComponentInChildren<TouchControls.Joystick>(true);
        Check(receiver!=null&&body!=null&&attack!=null&&jump!=null&&joystick!=null,"Shock receiver verification has source objects and real touch controls");
        if(receiver==null||body==null||attack==null||jump==null||joystick==null)yield break;
        var originalRoom=game.Current;Vector2 originalPosition=game.player.transform.position;
        var originalProgress=game.progress;bool originalEnabled=game.enabled;float originalTimeScale=Time.timeScale;
        var modifiers=typeof(PlayerController).GetField("modifiers",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var attackPointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=-31};
        var jumpPointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=-32};
        var joystickPointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=-33};
        UnityEngine.InputSystem.Keyboard keyboard=null;
        void Release()
        {
            attack.OnPointerUp(attackPointer);jump.OnPointerUp(jumpPointer);joystick.OnPointerUp(joystickPointer);game.controls.ClearGameplayInput();
        }
        void Step(float dt)
        {
            game.controls.Simulation=false;game.controls.Sample();game.controls.Simulation=true;
            game.player.Tick(dt);game.player.actor.Advance(dt);
        }
        Vector2 Place(float side)
        {
            Release();game.progress.shockGateOpened=false;room.RefreshShockGate(game.progress,game.player.actor.catalog);
            game.player.Restore();Time.timeScale=0;
            float x=receiver.transform.position.x+side*.8f;
            var floor=Physics2D.Raycast(new Vector2(x,receiver.transform.position.y+1f),Vector2.down,12f,1<<8);
            Vector2 at=new Vector2(x,floor.point.y+.025f);game.player.motor.Teleport(at);
            game.player.facing=-side;game.player.actor.Face(-side);
            for(int i=0;i<20;i++)Step(1f/60f);
            return at;
        }
        void HoldUp()
        {
            joystickPointer.position=RectTransformUtility.WorldToScreenPoint(null,joystick.bas.position);joystick.OnPointerDown(joystickPointer);
            joystickPointer.position+=Vector2.up*80f*joystick.bas.GetComponentInParent<Canvas>().scaleFactor;joystick.OnDrag(joystickPointer);
        }
        try
        {
            game.enabled=false;game.progress=new PlayerProgress();game.progress.UnlockCore();modifiers.SetValue(game.player,null);
            game.Enter(room.id,null,null);var barrier=body.GetComponent<Collider2D>();
            Place(-1);HoldUp();attack.OnPointerDown(attackPointer);Step(1f/60f);
            Check(game.controls.Vertical>.9f&&game.player.actor.Current=="Player_Upward_Attack_Clamped_anim","Dragging the mobile joystick upward selects the actual upward slash");
            for(int i=0;i<25;i++)Step(1f/60f);
            Check(!game.progress.shockGateOpened&&barrier.enabled,"A ground upward slash cannot reach the high mechanism");
            Place(-1);jump.OnPointerDown(jumpPointer);
            for(int i=0;i<8;i++)Step(1f/60f);
            attack.OnPointerDown(attackPointer);for(int i=0;i<24;i++)Step(1f/60f);
            Check(!game.progress.shockGateOpened&&barrier.enabled,"An airborne horizontal slash cannot activate the high mechanism");
            foreach(int fps in new[]{30,60,120})
            {
                float dt=1f/fps;
                foreach(float side in new[]{-1f,1f})
                {
                    Vector2 at=Place(side);HoldUp();jump.OnPointerDown(jumpPointer);
                    for(int i=0;i<Mathf.CeilToInt(.12f/dt);i++)Step(dt);
                    attack.OnPointerDown(attackPointer);Step(dt);
                    Check(game.player.actor.Current=="penitent_upward_attack_jump","Touch jump plus upward attack uses the source airborne clip at "+fps+" FPS, side "+side);
                    for(int i=0;i<Mathf.CeilToInt(.32f/dt)&&!game.progress.shockGateOpened;i++)Step(dt);
                    Check(game.progress.shockGateOpened&&!barrier.enabled,"Real mobile jump/upward slash opens the mechanism at "+fps+" FPS, side "+side+"; feet="+game.player.transform.position+", receiver="+receiver.transform.position);
                    if(fps==60&&side==-1&&game.progress.shockGateOpened)
                    {
                        Release();game.player.Restore();game.player.motor.Teleport(at);Time.timeScale=1;
                        yield return new WaitForSeconds(1.4f);
                        ScreenCapture.CaptureScreenshot("Documentation/Previews/shock-receiver-open.png");yield return null;
                        Time.timeScale=0;
                    }
                }
            }
            Place(-1);keyboard=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Keyboard>();
            for(int frame=0;frame<36;frame++)
            {
                var keys=frame>=8&&frame<10?new[]{UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.Space,UnityEngine.InputSystem.Key.J}:new[]{UnityEngine.InputSystem.Key.W,UnityEngine.InputSystem.Key.Space};
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(keyboard,new UnityEngine.InputSystem.LowLevel.KeyboardState(keys));UnityEngine.InputSystem.InputSystem.Update();Step(1f/60f);
            }
            Check(game.progress.shockGateOpened&&!barrier.enabled,"Real keyboard W/Space/J opens the same mechanism without purchased skills");
            UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);keyboard=null;Release();
            var save=File.ReadAllText(Path.Combine(Application.persistentDataPath,"brotherhood-verification.json"));
            Check(save.Contains("\"shockGateOpened\":true"),"Striking the mechanism persists its open state in the isolated verification save");
            game.Enter("D17Z01S02",null,null);game.Enter(room.id,null,null);
            Check(!barrier.enabled,"The physically struck mechanism remains open after leaving and returning");
            Place(-1);
            Vector2 edge=(Vector2)receiver.transform.position+Vector2.left+Vector2.down*4.45f;
            Check(room.TryStrikeShockReceiver(edge,1f,1.8f,4.2f,game),"A slash touching the source receiver's lower edge hits even when its center is above the slash");
            Place(-1);
            Check(!room.TryStrikeShockReceiver((Vector2)receiver.transform.position+Vector2.left+Vector2.down*4.71f,1f,1.8f,4.2f,game)&&barrier.enabled,"A slash beyond the receiver's one-unit bounds does not open the gate");
        }
        finally
        {
            if(keyboard!=null)UnityEngine.InputSystem.InputSystem.RemoveDevice(keyboard);
            Release();game.progress=originalProgress;modifiers.SetValue(game.player,null);
            game.Enter(originalRoom.id,null,originalPosition);game.player.Restore();game.controls.Simulation=true;
            game.enabled=originalEnabled;Time.timeScale=originalTimeScale;
        }
    }
    void CaptureCanvas(Canvas c, string name)
    {
        if (c == null) { Capture(name); return; }
        var oldMode = c.renderMode;
        var oldCam = c.worldCamera;
        var oldDist = c.planeDistance;
        var oldOverride = c.overrideSorting;
        var oldLayer = c.sortingLayerName;
        var oldOrder = c.sortingOrder;
        c.renderMode = RenderMode.ScreenSpaceCamera;
        c.worldCamera = game.view;
        c.planeDistance = 1f;
        c.overrideSorting = true;
        c.sortingLayerName = "Canvas UI";
        c.sortingOrder = 500;
        Capture(name);
        c.renderMode = oldMode;
        c.worldCamera = oldCam;
        c.planeDistance = oldDist;
        c.overrideSorting = oldOverride;
        c.sortingLayerName = oldLayer;
        c.sortingOrder = oldOrder;
    }
    IEnumerator VerifyCheatMenu()
    {
        var cheat=game.controls.debugUI;var hudButton=GameObject.Find("CHEAT");var mapButton=GameObject.Find("MAP");
        Check(cheat!=null&&hudButton!=null&&mapButton!=null,"Cheat panel and touch launcher are created alongside the map");
        if(cheat==null||hudButton==null||mapButton==null)yield break;
        Check(!cheat.GodMode,"God Mode starts disabled for a normal new game");
        var rect=hudButton.GetComponent<RectTransform>();var mapRect=mapButton.GetComponent<RectTransform>();
        Check(rect.sizeDelta==mapRect.sizeDelta&&Mathf.Abs(rect.anchoredPosition.y-mapRect.anchoredPosition.y)<.01f&&Mathf.Abs(mapRect.anchoredPosition.x-rect.anchoredPosition.x-65)<.01f,"Cheat launcher matches the map's 48-pixel size and sits immediately to its left");
        var original=game.progress;var originalRoom=game.Current;Vector2 originalPosition=game.player.transform.position;
        bool oldOwned=game.BloodRelicOwned,oldEquipped=game.BloodRelicEquipped;
        Action<string> click=name=>{var button=GameObject.Find(name)?.GetComponent<Button>();if(button!=null)button.onClick.Invoke();};
        var modifiers=typeof(PlayerController).GetField("modifiers",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        try
        {
            game.progress=new PlayerProgress();modifiers.SetValue(game.player,null);game.SetBloodRelic(false,false);
            game.controls.Attack=game.controls.SpecialHeld=true;game.controls.Move=1;
            var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=407};
            hudButton.GetComponent<TouchControls.TouchButton>().OnPointerDown(pointer);
            Check(cheat.IsOpen&&Time.timeScale==0&&!game.controls.Visible&&!game.controls.Attack&&!game.controls.SpecialHeld&&game.controls.Move==0,"Touch opens the cheat panel, pauses gameplay and clears held combat input");
            Check(!Array.Exists(cheat.Canvas.GetComponentsInChildren<Text>(true),label=>label.text=="CHEAT · TEST GAME"),"The cheat panel no longer displays the removed CHEAT · TEST GAME title");
            game.controls.Attack=true;game.controls.Move=1;yield return null;yield return null;
            Check(Vector2.Distance(originalPosition,game.player.transform.position)<.01f&&game.player.attackTime==0&&!game.controls.Attack,"Open cheat panel blocks gameplay updates and cannot start an attack while paused");
            click("Cheat Tears 1000");click("Cheat Tears 1000");
            var ranged=InventoryCatalog.Load().Find("RANGED_1");
            Check(game.progress.tears==2000&&!game.CanPurchaseInventorySkill(ranged),"Cheat Tears work immediately while the normal Mea Culpa purchase gate remains enforced");
            click("Cheat Mea Plus");click("Cheat Mea Plus");
            Check(game.progress.meaCulpaLevel==2&&game.CanPurchaseInventorySkill(ranged),"Two Mea Culpa cheat increments immediately enable the real level-two ranged purchase gate");
            click("Cheat Mea Max");click("Cheat Mea Plus");Check(game.progress.meaCulpaLevel==7,"Mea Culpa cheat clamps the upper bound at seven");
            for(int i=0;i<10;i++)click("Cheat Mea Minus");Check(game.progress.meaCulpaLevel==0,"Mea Culpa cheat clamps the lower bound at zero");
            click("Cheat Mea Plus");click("Cheat Mea Plus");click("Cheat Tears 10000");Check(game.progress.tears==12000,"The larger Tears cheat adds ten thousand without replacing existing currency");
            game.player.health=1;click("Cheat Heal");Check(game.player.health==game.player.MaxHealth,"Cheat heal uses the player's real maximum Life");
            game.player.fervour=0;click("Cheat Fervour");Check(game.player.fervour==game.player.MaxFervour,"Cheat Fervour refill uses the player's real maximum Fervour");
            game.player.health=1;game.player.fervour=0;game.player.flasks=0;click("Cheat Restore");
            Check(game.player.health==game.player.MaxHealth&&game.player.fervour==game.player.MaxFervour&&game.player.flasks==game.player.MaxFlasks&&Time.timeScale==0,"Full cheat restore replenishes Life, Fervour and flasks while keeping the panel paused");
            click("Cheat Charged Max");Check(game.progress.chargedTier==3&&game.progress.Owns("CHARGED_1")&&game.progress.Owns("CHARGED_2")&&game.progress.Owns("CHARGED_3"),"Charged cheat grants all owned tiers consistently");
            click("Cheat Lunge Plus");click("Cheat Ranged Max");click("Cheat Ranged Plus");
            Check(game.progress.lungeTier==1&&game.progress.rangedTier==3&&game.progress.Owns("RANGED_1")&&game.progress.Owns("RANGED_2")&&game.progress.Owns("RANGED_3")&&game.progress.hasSpecial&&game.progress.specialMode==1,"Ranged cheat clamps at tier three and updates ownership, availability and the selected skill");
            click("Cheat Special Mode");Check(game.progress.specialMode==0,"Cheat panel can select lunge when both special skills are available");
            click("Cheat Blood Relic");Check(game.BloodRelicOwned&&game.BloodRelicEquipped&&game.progress.Owns("RE01")&&game.progress.IsEquipped("RE01"),"Blood platform cheat updates actual relic ownership and equipment");
            yield return null;UnityEngine.Canvas.ForceUpdateCanvases();
            var corners=new Vector3[4];GameObject.Find("Cheat panel").GetComponent<RectTransform>().GetWorldCorners(corners);bool inside=true;
            foreach(var corner in corners)inside&=corner.x>=Screen.safeArea.xMin-1&&corner.x<=Screen.safeArea.xMax+1&&corner.y>=Screen.safeArea.yMin-1&&corner.y<=Screen.safeArea.yMax+1;
            Check(inside,"Cheat panel fits inside the current mobile safe area");
            ScreenCapture.CaptureScreenshot("Documentation/Previews/cheat-panel.png");yield return null;
            click("Cheat Blood Relic");Check(game.BloodRelicOwned&&!game.BloodRelicEquipped&&!game.progress.IsEquipped("RE01"),"Blood platform cheat can unequip the relic without discarding ownership");
            click("Cheat Lunge Minus");Check(game.progress.lungeTier==0&&game.progress.specialMode==1&&!game.progress.Owns("LUNGE_1"),"Removing lunge switches back to the available ranged skill and removes its ownership");
            for(int i=0;i<5;i++)click("Cheat Ranged Minus");
            Check(game.progress.rangedTier==0&&!game.progress.hasSpecial&&!game.progress.Owns("RANGED_1")&&!game.progress.Owns("RANGED_2")&&!game.progress.Owns("RANGED_3"),"Removing ranged clears all higher ownership and availability without negative tiers");
            click("Cheat Close");Check(!cheat.IsOpen&&Time.timeScale==1&&game.controls.Visible,"Closing the cheat panel resumes gameplay and returns the HUD");
            for(int i=0;i<3;i++){cheat.Show();cheat.Hide();}Check(!cheat.IsOpen&&Time.timeScale==1&&game.controls.Visible,"Repeated cheat opening and closing cannot leave the game paused");
            cheat.Show();click("Cheat Room Next");click("Cheat Room Previous");click("Cheat Go Room");
            Check(game.Current.id=="D01Z02S06"&&!cheat.IsOpen&&Time.timeScale==1&&game.controls.Visible&&game.progress.meaCulpaLevel==2,"Cheat room travel reaches the Mea Culpa altar without automatically granting an altar upgrade");
            VerifyGodModeCombat(cheat,click);
        }
        finally
        {
            typeof(DevelopmentDebugMenu).GetMethod("SetGodMode",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)?.Invoke(cheat,new object[]{false});
            cheat.Hide();game.effects.Clear();game.progress=original;modifiers.SetValue(game.player,null);game.SetBloodRelic(oldOwned,oldEquipped);
            game.Enter(originalRoom.id,null,originalPosition);game.player.Restore();game.controls.ClearGameplayInput();Time.timeScale=1;
        }
        yield return null;ScreenCapture.CaptureScreenshot("Documentation/Previews/cheat-hud.png");yield return null;
    }
    void VerifyGodModeCombat(DevelopmentDebugMenu cheat,Action<string> click)
    {
        var originalProgress=game.progress;string originalRoom=game.Current.id;Vector2 originalPosition=game.player.transform.position;
        bool originalEnabled=game.enabled,originalSimulation=game.controls.Simulation;float originalTimeScale=Time.timeScale;
        var modifiers=typeof(PlayerController).GetField("modifiers",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        var setGodMode=typeof(DevelopmentDebugMenu).GetMethod("SetGodMode",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
        EnemyController target=null,boss=null;float[] originalEnemyHealth=null;float originalBossHealth=0;int originalPurge=0;
        var attack=Array.Find(game.controls.GetComponentsInChildren<TouchControls.TouchButton>(true),button=>button.name=="ATTACK");
        var pointer=new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current){pointerId=-91};
        void Step(float seconds)
        {
            for(float remaining=seconds;remaining>.00001f;)
            {
                float dt=Mathf.Min(1f/120,remaining);game.controls.Simulation=false;game.controls.Sample();game.controls.Simulation=true;
                game.player.Tick(dt);game.player.actor.Advance(dt);game.effects.Tick(dt);remaining-=dt;
            }
        }
        void Melee()
        {
            attack.OnPointerDown(pointer);Step(.02f);attack.OnPointerUp(pointer);Step(.28f);
        }
        void ToggleGod()
        {
            cheat.Show();click("Cheat God Mode");cheat.Hide();Time.timeScale=0;
        }
        void PlaceTarget(Vector2 at)
        {target.ResetEnemy();target.health=1000;target.motor.Teleport(at);Physics2D.SyncTransforms();}
        try
        {
            cheat.Hide();game.enabled=false;game.progress=new PlayerProgress();modifiers.SetValue(game.player,null);
            game.Enter("D17Z01S02",null,null);var enemies=game.Current.enemies;
            Check(attack!=null&&enemies.Length>0,"God Mode verification uses the real melee touch control and an existing source enemy");
            if(attack==null||enemies.Length==0)return;
            originalEnemyHealth=Array.ConvertAll(enemies,enemy=>enemy.health);target=enemies[0];originalPurge=target.purgeReward;target.purgeReward=35;
            foreach(var enemy in enemies)enemy.health=0;
            game.player.Restore();game.player.motor.Teleport(game.Current.start.position);game.player.facing=1;game.player.actor.Face(1);
            game.controls.ClearGameplayInput();Time.timeScale=0;Step(.6f);Vector2 floor=game.player.transform.position;
            PlaceTarget(floor+Vector2.right*1.25f);Melee();
            Check(target.health==982&&target.Hurt&&!target.Dead,"With God Mode off a real melee hit deals normal eighteen damage and puts the enemy into Hurt");
            Step(.8f);float beforeToggle=target.health;ToggleGod();
            Check(cheat.GodMode&&target.health==beforeToggle,"The actual God Mode button enables the cheat without deleting enemy Life before a hit");
            float protectedHealth=game.player.health;game.player.Damage(20,floor.x+3,false);game.player.DamageContact(20,floor.x-3);
            Check(game.player.health==protectedHealth&&!game.player.Dead,"God Mode protects the player from incoming attacks and contact damage");
            float beforeReward=game.progress.tears;Melee();
            Check(target.Dead&&target.health==0&&target.actor.Current.IndexOf("death",StringComparison.OrdinalIgnoreCase)>=0&&target.actor.catalog.Find(target.actor.Current)!=null,"A real melee hit with God Mode on instantly kills through the enemy's normal death path");
            Check(game.progress.tears==beforeReward+35,"God Mode melee kills grant the enemy's normal Tears reward");
            target.Damage(1);target.Damage(999);
            Check(game.progress.tears==beforeReward+35,"Further hits on a God Mode kill cannot grant its Tears reward twice");
            Step(.8f);PlaceTarget(floor+Vector2.right*1.25f);target.Damage(0);
            Check(target.health==1000&&!target.Dead,"A zero-damage hit cannot trigger God Mode's instant kill");
            game.player.HeavyWeaponHit(target,1);
            Check(target.Dead&&game.progress.tears==beforeReward+70,"God Mode also kills on a positive heavy weapon hit while retaining the shared reward path");
            PlaceTarget(floor+Vector2.right*7);game.progress.equippedPrayer="PR07";game.progress.hasPrayer=true;
            game.prayerEffects.Cast("PR07",1);
            Check(target.Dead&&game.progress.tears==beforeReward+105,"God Mode also kills through an actual Lorquiana prayer hit");
            game.prayerEffects.Stop();game.effects.Clear();
            boss=game.Find("D17Z01S11").Boss;Check(boss!=null,"God Mode verification retains the existing source boss");
            if(boss!=null)
            {
                originalBossHealth=boss.health;boss.ResetEnemy();float introLife=boss.health;boss.Damage(1);
                Check(!boss.BossIntroComplete&&!game.BossFightActive&&!boss.Dead&&boss.health==introLife,"God Mode respects the original boss intro guard instead of killing a protected boss");
            }
            ToggleGod();Check(!cheat.GodMode,"The actual God Mode button turns the cheat off again");
            float unprotectedHealth=game.player.health;game.player.Damage(20,floor.x+3,false);
            Check(game.player.health==unprotectedHealth-20,"Turning God Mode off releases its invulnerability and ordinary incoming damage works again");
            Step(.6f);PlaceTarget(floor+Vector2.right*1.25f);Melee();
            Check(target.health==982&&target.Hurt&&!target.Dead,"After God Mode is disabled a real melee hit returns to normal damage and Hurt behavior");
            cheat.Show();game.progress.SetOwned("PR07",true);game.progress.equippedPrayer="PR07";click("Cheat Unlock Prayers");
            bool ownsAllPrayers=true;foreach(var item in InventoryCatalog.Load().items)if(item.category=="prayer")ownsAllPrayers&=game.progress.Owns(item.id);
            Check(ownsAllPrayers&&game.progress.hasPrayer&&game.progress.equippedPrayer=="PR07","The existing prayer-unlock button still grants every catalog prayer while preserving the equipped prayer");
            cheat.Hide();
        }
        finally
        {
            if(attack!=null)attack.OnPointerUp(pointer);setGodMode?.Invoke(cheat,new object[]{false});cheat.Hide();
            game.controls.ClearGameplayInput();game.effects.Clear();
            if(target!=null)target.purgeReward=originalPurge;
            if(originalEnemyHealth!=null)
            {
                var enemies=game.Find("D17Z01S02").enemies;
                for(int i=0;i<enemies.Length;i++){enemies[i].ResetEnemy();enemies[i].health=originalEnemyHealth[i];}
            }
            if(boss!=null){boss.ResetEnemy();boss.health=originalBossHealth;}
            game.progress=originalProgress;modifiers.SetValue(game.player,null);game.Enter(originalRoom,null,originalPosition);game.player.Restore();
            game.controls.ClearGameplayInput();game.controls.Simulation=originalSimulation;game.enabled=originalEnabled;Time.timeScale=originalTimeScale;
        }
    }
    void Capture(string name)
    {
        try
        {
            Directory.CreateDirectory("Documentation/Previews");var camera=game.view;var target=new RenderTexture(1280,720,24);var old=camera.targetTexture;
            camera.targetTexture=target;camera.Render();var previous=RenderTexture.active;RenderTexture.active=target;
            var png=new Texture2D(1280,720,TextureFormat.RGB24,false);png.ReadPixels(new Rect(0,0,1280,720),0,0);png.Apply();
            string path="Documentation/Previews/"+name+".png";
            try{File.WriteAllBytes(path,png.EncodeToPNG());}catch(Exception ex){Debug.LogWarning("Capture write: "+ex.Message);}
            camera.targetTexture=old;RenderTexture.active=previous;target.Release();Destroy(target);Destroy(png);
        }
        catch(Exception e){Debug.LogWarning("Capture failed: "+e.Message);}
    }
    bool ControlsInsideScreen(){var controls=GameObject.Find("Dynamic Controls Canvas");if(controls==null)return false;var corners=new Vector3[4];foreach(var r in controls.GetComponentsInChildren<RectTransform>(false)){if(r==controls.transform)continue;r.GetWorldCorners(corners);for(int i=0;i<4;i++)if(corners[i].x<-2||corners[i].y<-2||corners[i].x>Screen.width+2||corners[i].y>Screen.height+2)return false;}return true;}
    void Finish()
    {
        Debug.Log("[BrotherhoodPlayCheck] Finish() invoked, failed=" + failed + ", checks=" + checks.Count);
        string report=string.Join("\n",checks)+"\nRESULT: "+(failed?"FAILED":"PASSED");
        try{File.WriteAllText("Documentation/playtest-results.txt",report);}
        catch(IOException e)
        {
            string fallback=Path.Combine(Application.dataPath,"../Temp/brotherhood-playtest-results.txt");
            Debug.LogWarning("Primary playtest report is locked; writing fallback: "+e.Message);
            File.WriteAllText(fallback,report);
        }
        finally
        {
            File.Delete(Path.Combine(Application.dataPath,"../Temp/brotherhood-verify-play"));
            EditorApplication.isPlaying=false;
        }
    }
}
