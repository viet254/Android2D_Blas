using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Brotherhood
{
 [DefaultExecutionOrder(-1000)]
 public sealed class DevelopmentDebugMenu:MonoBehaviour
 {
  public BrotherhoodGame game;TouchControls controls;GameObject root;RectTransform safe,panel;Font font;
  Text meaText,tearsText,lifeText,fervourText,flaskText,chargedText,lungeText,rangedText,relicText,modeText,roomText,godText,prayerText;
  float resumeTimeScale=1;int roomIndex;Rect lastSafe;Vector2 lastScreen;
  InventoryCatalog inventory;PlayerController protectedPlayer;float nativeInvincibility,nativeInvincibilityAt;
  const float GodInvincibility=10000f;
  static readonly FieldInfo invincibilityField=typeof(PlayerController).GetField("invincible",BindingFlags.Instance|BindingFlags.NonPublic);
  static readonly object godInvincibilityValue=GodInvincibility;
  readonly Color gold=new Color(.95f,.72f,.32f);
  public bool GodMode {get;private set;}
  public float EnemyHitDamage(float amount,float enemyHealth)=>GodMode&&amount>0?Mathf.Max(amount,enemyHealth):amount;
  public bool IsOpen=>root!=null&&root.activeSelf;
  public Canvas Canvas {get;private set;}
  public void Initialize(BrotherhoodGame owner,TouchControls input)
  {
   game=owner;controls=input;if(root!=null)return;inventory=InventoryCatalog.Load();
   font=Resources.Load<Font>("Fonts/Caudex-Regular");if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
   roomIndex=game.rooms==null?0:Mathf.Max(0,Array.FindIndex(game.rooms,r=>r.id=="D01Z02S06"));BuildUI();
  }
  void Update()
  {
   MaintainGodMode();
   var k=Keyboard.current;if(k!=null&&k.f1Key.wasPressedThisFrame)Toggle();else if(IsOpen&&k!=null&&k.escapeKey.wasPressedThisFrame)Hide();
   if(IsOpen&&(lastSafe!=Screen.safeArea||lastScreen!=new Vector2(Screen.width,Screen.height)))UpdateSafeArea();
  }
  public void Toggle(){if(IsOpen)Hide();else Show();}
  public void Show()
  {
   if(root==null||game==null||game.player==null||game.player.Dead||game.InputBlocked||controls.Remapping||game.gameOverUI!=null&&game.gameOverUI.IsShowing)return;
   if(IsOpen)return;
   if(controls.inventoryUI!=null&&controls.inventoryUI.IsOpen)controls.inventoryUI.Hide();
   if(controls.mapUI!=null&&controls.mapUI.IsOpen)controls.mapUI.Hide();
   if(controls.optionsUI!=null&&controls.optionsUI.IsOpen)controls.optionsUI.Hide();
   resumeTimeScale=Time.timeScale;controls.ClearGameplayInput();controls.SetControlsVisible(false);
   root.SetActive(true);Time.timeScale=0;Refresh();UnityEngine.Canvas.ForceUpdateCanvases();UpdateSafeArea();
  }
  public void Hide()
  {
   if(!IsOpen)return;root.SetActive(false);Time.timeScale=resumeTimeScale;if(controls==null)return;controls.ClearGameplayInput();
   if(game!=null&&!game.InputBlocked&&(game.gameOverUI==null||!game.gameOverUI.IsShowing)
    &&(controls.mapUI==null||!controls.mapUI.IsOpen)&&(controls.inventoryUI==null||!controls.inventoryUI.IsOpen)
    &&(controls.optionsUI==null||!controls.optionsUI.IsOpen))controls.SetControlsVisible(true);
  }
  void LateUpdate()
  {
   if(!GodMode)return;
   // An immortal player must not fall forever outside the room's playable area.
   if(!IsOpen&&game!=null&&!game.InputBlocked&&game.Current!=null&&game.player!=null&&game.player.transform.position.y<game.Current.PlayerFallY(game.player.transform.position))game.Respawn();
   MaintainGodMode();
  }
  void OnDisable(){if(IsOpen)Hide();SetGodMode(false);}
  void OnDestroy(){if(IsOpen)Time.timeScale=1;SetGodMode(false);}
  void Change(Action action){if(!IsOpen)return;action();MaintainGodMode();controls.ClearGameplayInput();Time.timeScale=0;Refresh();}
  void SetGodMode(bool enabled)
  {
   if(enabled==GodMode)return;
   if(enabled&&invincibilityField==null){game.Message("Godmode chưa sẵn sàng");return;}
   GodMode=enabled;if(enabled)MaintainGodMode();else ReleaseGodProtection();
  }
  void MaintainGodMode()
  {
   if(!GodMode||game==null||game.player==null)return;
   var player=game.player;
   if(protectedPlayer!=player){ReleaseGodProtection();protectedPlayer=player;}
   float value=(float)invincibilityField.GetValue(player);
   // Use the player's existing immunity timer exclusively from this cheat.
   // Preserve legitimate immunity from Restore/parry when Godmode is turned off.
   if(value<GodInvincibility*.5f){nativeInvincibility=value;nativeInvincibilityAt=Time.time;}
   player.health=player.MaxHealth;invincibilityField.SetValue(player,godInvincibilityValue);
  }
  void ReleaseGodProtection()
  {
   if(protectedPlayer!=null&&invincibilityField!=null&&(float)invincibilityField.GetValue(protectedPlayer)>=GodInvincibility*.5f)
    invincibilityField.SetValue(protectedPlayer,Mathf.Max(0,nativeInvincibility-(Time.time-nativeInvincibilityAt)));
   protectedPlayer=null;nativeInvincibility=0;
  }
  void UnlockPrayers()
  {
   var p=game.progress;InventoryCatalog.Item first=null;
   if(inventory?.items==null)return;
   foreach(var item in inventory.items)if(item.category=="prayer"){p.SetOwned(item.id,true);if(first==null)first=item;}
   var equipped=inventory.Find(p.equippedPrayer);
   if(equipped==null||equipped.category!="prayer")p.equippedPrayer=(inventory.Find("PR14")??first)?.id??"";
   p.hasPrayer=!string.IsNullOrEmpty(p.equippedPrayer);
  }
  void AdjustSkill(string prefix,int delta)
  {
   var p=game.progress;int old=prefix=="CHARGED_"?p.chargedTier:prefix=="LUNGE_"?p.lungeTier:p.rangedTier;int tier=Mathf.Clamp(old+delta,0,3);
   if(prefix=="CHARGED_")p.chargedTier=tier;else if(prefix=="LUNGE_")p.lungeTier=tier;else p.rangedTier=tier;
   for(int i=1;i<=3;i++)p.SetOwned(prefix+i,i<=tier);p.hasSpecial=p.rangedTier>0||p.lungeTier>0;
   if(prefix=="RANGED_"&&tier>0)p.specialMode=1;else if(prefix=="LUNGE_"&&tier>0)p.specialMode=0;
   if(p.rangedTier<=0)p.specialMode=0;else if(p.lungeTier<=0)p.specialMode=1;
  }
  void SelectRoom(int delta){if(game.rooms==null||game.rooms.Length==0)return;roomIndex=(roomIndex+delta+game.rooms.Length)%game.rooms.Length;Refresh();}
  void GoToRoom(){if(game.rooms==null||game.rooms.Length==0)return;string id=game.rooms[Mathf.Clamp(roomIndex,0,game.rooms.Length-1)].id;Hide();game.Enter(id,null,null);}
  void Refresh()
  {
   var p=game.progress;var player=game.player;
   Set(meaText,"Mea Culpa: "+p.meaCulpaLevel+" / 7");Set(tearsText,"Tears: "+Mathf.FloorToInt(p.tears).ToString("N0"));
   Set(lifeText,"Máu: "+Mathf.CeilToInt(player.health)+" / "+Mathf.CeilToInt(player.MaxHealth));
   Set(fervourText,"Fervour: "+Mathf.CeilToInt(player.fervour)+" / "+Mathf.CeilToInt(player.MaxFervour));Set(flaskText,"Bình máu: "+player.flasks+" / "+player.MaxFlasks);
   Set(chargedText,"Chém tích lực: "+p.chargedTier+" / 3");Set(lungeText,"Lướt chém: "+p.lungeTier+" / 3");Set(rangedText,"Phóng kiếm: "+p.rangedTier+" / 3");
   Set(relicText,"Bệ máu: "+(game.BloodRelicEquipped?"BẬT":"TẮT"));Set(modeText,"Chạm kỹ năng: "+(p.specialMode==1?"phóng kiếm":"lướt chém"));
   Set(godText,GodMode?"BẬT · BẤM ĐỂ TẮT":"TẮT · BẤM ĐỂ BẬT");
   int prayers=0,ownedPrayers=0;if(inventory?.items!=null)foreach(var item in inventory.items)if(item.category=="prayer"){prayers++;if(p.Owns(item.id))ownedPrayers++;}
   Set(prayerText,"MỞ KINH CẦU NGUYỆN · "+ownedPrayers+" / "+prayers);
   if(game.rooms!=null&&game.rooms.Length>0){var r=game.rooms[Mathf.Clamp(roomIndex,0,game.rooms.Length-1)];Set(roomText,r.id+(r.id=="D01Z02S06"?" · Bàn Mea Culpa":""));}
  }
  static void Set(Text label,string value){label.text=value;VietnameseSource.Apply(label);}
  void BuildUI()
  {
   var co=new GameObject("CheatCanvas",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));co.transform.SetParent(transform,false);
   Canvas=co.GetComponent<Canvas>();Canvas.renderMode=RenderMode.ScreenSpaceOverlay;Canvas.sortingOrder=140;
   var scaler=co.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
   root=new GameObject("Cheat panel root",typeof(RectTransform),typeof(Image));root.transform.SetParent(co.transform,false);
   var backdrop=root.GetComponent<RectTransform>();backdrop.anchorMin=Vector2.zero;backdrop.anchorMax=Vector2.one;backdrop.offsetMin=backdrop.offsetMax=Vector2.zero;root.GetComponent<Image>().color=new Color(.02f,.02f,.03f,.83f);
   safe=new GameObject("Cheat safe area",typeof(RectTransform)).GetComponent<RectTransform>();safe.SetParent(root.transform,false);safe.anchorMin=Vector2.zero;safe.anchorMax=Vector2.one;safe.offsetMin=safe.offsetMax=Vector2.zero;
   panel=new GameObject("Cheat panel",typeof(RectTransform),typeof(Image),typeof(Outline)).GetComponent<RectTransform>();panel.SetParent(safe,false);panel.anchorMin=panel.anchorMax=panel.pivot=new Vector2(.5f,.5f);panel.sizeDelta=new Vector2(920,664);
   panel.GetComponent<Image>().color=new Color(.055f,.035f,.065f,.99f);panel.GetComponent<Outline>().effectColor=gold;panel.GetComponent<Outline>().effectDistance=new Vector2(2,-2);
   Label("Cheat description","Chỉnh lượt chơi hiện tại · Game tạm dừng khi bảng này mở",28,-65,860,30,16);
   ActionButton("Cheat Close","ĐÓNG",834,-22,60,40,Hide);Label("Cheat character title","NHÂN VẬT",32,-110,390,30,20);Label("Cheat skills title","KỸ NĂNG",478,-110,414,30,20);
   meaText=Label("Cheat Mea value","",32,-154,180,44);
   ActionButton("Cheat Mea Minus","−",214,-154,50,44,()=>Change(()=>game.progress.meaCulpaLevel=Mathf.Clamp(game.progress.meaCulpaLevel-1,0,7)));
   ActionButton("Cheat Mea Plus","+",278,-154,50,44,()=>Change(()=>game.progress.meaCulpaLevel=Mathf.Clamp(game.progress.meaCulpaLevel+1,0,7)));
   ActionButton("Cheat Mea Max","CẤP 7",342,-154,80,44,()=>Change(()=>game.progress.meaCulpaLevel=7));
   tearsText=Label("Cheat Tears value","",32,-208,180,44);
   ActionButton("Cheat Tears 1000","+1.000",214,-208,92,44,()=>Change(()=>game.progress.tears=Mathf.Max(0,game.progress.tears)+1000));
   ActionButton("Cheat Tears 10000","+10.000",318,-208,104,44,()=>Change(()=>game.progress.tears=Mathf.Max(0,game.progress.tears)+10000));
   lifeText=Label("Cheat Life value","",32,-264,206,44);ActionButton("Cheat Heal","HỒI ĐẦY MÁU",246,-264,176,44,()=>Change(()=>game.player.health=game.player.MaxHealth));
   fervourText=Label("Cheat Fervour value","",32,-318,206,44);ActionButton("Cheat Fervour","HỒI FERVOUR",246,-318,176,44,()=>Change(()=>game.player.fervour=game.player.MaxFervour));
   flaskText=Label("Cheat Flask value","",32,-372,206,44);ActionButton("Cheat Restore","HỒI PHỤC TẤT CẢ",246,-372,176,44,()=>Change(()=>game.player.Restore()));
   Label("Cheat Relic label","THÁNH TÍCH",32,-426,170,44);relicText=ActionButton("Cheat Blood Relic","",214,-426,208,44,()=>Change(()=>game.SetBloodRelic(true,!game.BloodRelicEquipped)));
   Label("Cheat God label","GODMODE",32,-480,170,44);godText=ActionButton("Cheat God Mode","",214,-480,208,44,()=>Change(()=>SetGodMode(!GodMode)));
   chargedText=SkillRow("Charged","CHARGED_",154);lungeText=SkillRow("Lunge","LUNGE_",208);rangedText=SkillRow("Ranged","RANGED_",262);
   modeText=ActionButton("Cheat Special Mode","",478,-318,414,44,()=>Change(()=>{var p=game.progress;if(p.rangedTier>0&&p.lungeTier>0)p.specialMode=1-p.specialMode;}));
   ActionButton("Cheat Unlock Core","MỞ DI CHUYỂN CƠ BẢN",478,-372,414,44,()=>Change(()=>game.progress.UnlockCore()));
   prayerText=ActionButton("Cheat Unlock Prayers","",478,-426,414,44,()=>Change(UnlockPrayers));
   Label("Cheat skill hint","Godmode: bất tử, một đòn hạ quái.\nRơi vực trở về điểm lưu · Chọn kinh trong túi đồ.",478,-480,414,54,15);
   ActionButton("Cheat Room Previous","‹",32,-574,48,44,()=>SelectRoom(-1));roomText=Label("Cheat Room value","",96,-574,520,44,18);
   ActionButton("Cheat Room Next","›",636,-574,48,44,()=>SelectRoom(1));ActionButton("Cheat Go Room","ĐẾN PHÒNG",716,-574,176,44,GoToRoom);
   Label("Cheat footer","Mea Culpa thường tăng một cấp ở mỗi bàn thờ mới · F1 / Esc: đóng mở bảng",32,-625,860,24,14);root.SetActive(false);
  }
  Text SkillRow(string name,string prefix,float y)
  {
   var value=Label("Cheat "+name+" value","",478,-y,215,44);
   ActionButton("Cheat "+name+" Minus","−",700,-y,50,44,()=>Change(()=>AdjustSkill(prefix,-1)));
   ActionButton("Cheat "+name+" Plus","+",764,-y,50,44,()=>Change(()=>AdjustSkill(prefix,1)));
   ActionButton("Cheat "+name+" Max","CẤP 3",828,-y,64,44,()=>Change(()=>AdjustSkill(prefix,3)));return value;
  }
  RectTransform Place(string name,float x,float y,float width,float height,Transform parent)
  {
   var r=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(parent,false);r.anchorMin=r.anchorMax=r.pivot=new Vector2(0,1);r.anchoredPosition=new Vector2(x,y);r.sizeDelta=new Vector2(width,height);return r;
  }
  Text Label(string name,string value,float x,float y,float width,float height,int size=18){return TextIn(Place(name,x,y,width,height,panel),value,size,TextAnchor.MiddleLeft);}
  Text TextIn(RectTransform r,string value,int size,TextAnchor alignment)
  {
   var t=r.gameObject.AddComponent<Text>();t.font=font;t.fontSize=size;t.color=gold;t.alignment=alignment;t.raycastTarget=false;t.horizontalOverflow=HorizontalWrapMode.Wrap;Set(t,value);return t;
  }
  Text ActionButton(string name,string value,float x,float y,float width,float height,Action action)
  {
   var r=Place(name,x,y,width,height,panel);var image=r.gameObject.AddComponent<Image>();image.color=new Color(.15f,.09f,.16f);
   var outline=r.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.65f,.42f,.22f);outline.effectDistance=new Vector2(1,-1);
   var button=r.gameObject.AddComponent<Button>();button.targetGraphic=image;button.onClick.AddListener(()=>action());return TextIn(Place(name+" label",4,-2,width-8,height-4,r),value,16,TextAnchor.MiddleCenter);
  }
  void UpdateSafeArea()
  {
   lastSafe=Screen.safeArea;lastScreen=new Vector2(Mathf.Max(1,Screen.width),Mathf.Max(1,Screen.height));safe.anchorMin=lastSafe.position/lastScreen;safe.anchorMax=(lastSafe.position+lastSafe.size)/lastScreen;safe.offsetMin=safe.offsetMax=Vector2.zero;
   Vector2 available=lastSafe.size/Mathf.Max(.01f,Canvas.scaleFactor);Vector2 needed=panel.sizeDelta+Vector2.one*40;panel.localScale=Vector3.one*Mathf.Min(1,Mathf.Min(available.x/needed.x,available.y/needed.y));
  }
 }
}
