using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Brotherhood
{
public sealed class TouchControls:MonoBehaviour
{
 bool ti;float tv;bool fixedJoystick;float shownHealth=1,shownLoss=1;int menuPage,inventoryOffset;public bool Remapping {get;private set;}
 public float Move,Vertical;public bool Jump,Attack,Dash,Parry,Flask,Prayer,Special,Interact,InteractHeld,JumpHeld,AttackHeld,Down,Visible=true;[System.NonSerialized]public bool Simulation;public BrotherhoodGame game;
 bool tj,ta,td;float tm;bool qj,qa,qd,qp,qf,qpr,qsp,qi;Text msgText,bossText,bossTextShadow,menuText,tearsText,equipText;Image hp,hpLoss,fervour,boss,bossLoss;Image[] flaskIcons,menuIcons;InventoryCatalog.Item[] shownItems;InventoryCatalog.Item selectedItem;RectTransform hudSafe,controlSafe,menu;Rect lastSafe;Sprite disc,flaskFull,flaskEmpty;Font originalFont;GameObject jumpButton,attackButton,dashButton,parryButton,flaskButton,prayerButton,specialButton,useButton,mapButton,bagButton,doneLayoutButton,bossRoot;int oldFlask=-1;string oldMsg;InventoryCatalog inventory;readonly Dictionary<string,Sprite> inventorySprites=new Dictionary<string,Sprite>();
 public BlasInventoryUI inventoryUI;public BlasMapUI mapUI;public BlasOptionsUI optionsUI;
 void Awake(){var module=FindAnyObjectByType<InputSystemUIInputModule>();if(module==null){var events=new GameObject("Input events",typeof(EventSystem),typeof(InputSystemUIInputModule));module=events.GetComponent<InputSystemUIInputModule>();}if(module.actionsAsset==null)module.AssignDefaultActions();}
  public void Sample(){if(Simulation)return;var k=Keyboard.current;var p=Gamepad.current;Move=tm;if(k!=null){if(k.aKey.isPressed||k.leftArrowKey.isPressed)Move=-1;if(k.dKey.isPressed||k.rightArrowKey.isPressed)Move=1;}if(p!=null&&Mathf.Abs(p.leftStick.x.ReadValue())>.15f)Move=p.leftStick.x.ReadValue();Jump=qj||(k!=null&&k.spaceKey.wasPressedThisFrame)||(p!=null&&p.buttonSouth.wasPressedThisFrame);Attack=qa||(k!=null&&k.jKey.wasPressedThisFrame)||(p!=null&&p.buttonWest.wasPressedThisFrame);Dash=qd||(k!=null&&(k.kKey.wasPressedThisFrame||k.leftShiftKey.wasPressedThisFrame))||(p!=null&&p.buttonEast.wasPressedThisFrame);Parry=qp||(k!=null&&k.lKey.wasPressedThisFrame)||(p!=null&&p.leftShoulder.wasPressedThisFrame);Flask=qf||(k!=null&&k.qKey.wasPressedThisFrame)||(p!=null&&p.rightShoulder.wasPressedThisFrame);Prayer=qpr||(k!=null&&k.rKey.wasPressedThisFrame);Special=qsp||(k!=null&&k.fKey.wasPressedThisFrame);Interact=qi||(k!=null&&k.eKey.wasPressedThisFrame)||(p!=null&&p.buttonNorth.wasPressedThisFrame);JumpHeld=tj||(k!=null&&k.spaceKey.isPressed)||(p!=null&&p.buttonSouth.isPressed);AttackHeld=ta||(k!=null&&k.jKey.isPressed)||(p!=null&&p.buttonWest.isPressed);InteractHeld=ti||(k!=null&&k.eKey.isPressed)||(p!=null&&p.buttonNorth.isPressed);Down=td||(k!=null&&k.sKey.isPressed)||(p!=null&&p.leftStick.y.ReadValue()<-.5f);qj=qa=qd=qp=qf=qpr=qsp=qi=false;}
 void Start(){if(FindFirstObjectByType<EventSystem>()==null){var e=new GameObject("Input events");e.AddComponent<EventSystem>();e.AddComponent<InputSystemUIInputModule>();}AudioListener.volume=PlayerPrefs.GetFloat("BrotherhoodVolume",1);fixedJoystick=PlayerPrefs.GetInt("BrotherhoodFixedJoystick",0)==1;if(PlayerPrefs.GetInt("BrotherhoodControlLayoutVersion",0)<3){foreach(var key in new[]{"JUMP","ATTACK","DASH","PARRY","FLASK","PRAYER","SPECIAL","USE","MAP","BAG"}){PlayerPrefs.DeleteKey("BrotherhoodControl_"+key+"_X");PlayerPrefs.DeleteKey("BrotherhoodControl_"+key+"_Y");}PlayerPrefs.SetInt("BrotherhoodControlLayoutVersion",3);}inventory=InventoryCatalog.Load();disc=MakeDisc();hudSafe=Canvas("Static UI Canvas",0,new Vector2(1280,720));controlSafe=Canvas("Dynamic Controls Canvas",1,new Vector2(1920,1080));Safe();BuildHud();BuildControls();useButton.GetComponent<TouchButton>().held=v=>{ti=v;InteractHeld=v;};BuildMenu();inventoryUI=gameObject.AddComponent<BlasInventoryUI>();inventoryUI.Initialize(game,this);optionsUI=gameObject.AddComponent<BlasOptionsUI>();optionsUI.Initialize(game,this);mapUI=gameObject.AddComponent<BlasMapUI>();mapUI.Initialize(game,this,optionsUI);}
 void Update(){if(menu!=null)menu.anchoredPosition=Vector2.zero;}
 RectTransform Canvas(string name,int order,Vector2 refRes){var o=new GameObject(name,typeof(UnityEngine.Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));o.transform.SetParent(transform);var c=o.GetComponent<UnityEngine.Canvas>();c.renderMode=RenderMode.ScreenSpaceOverlay;c.sortingOrder=order;var s=o.GetComponent<CanvasScaler>();s.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;s.referenceResolution=refRes;s.matchWidthOrHeight=1.0f;var r=new GameObject("Safe area",typeof(RectTransform)).GetComponent<RectTransform>();r.SetParent(o.transform,false);return r;}
   void BuildHud()
 {
  AddArt(hudSafe,"Penitent portrait","HUD/PortraitFrame",new Vector2(0,1),new Vector2(36,-42),new Vector2(176,116));
  hp=OriginalGauge(hudSafe,"Health",new Vector2(127,-60),170,12,"HUD/HealthFill",false,out hpLoss);
  Image unused;
  fervour=OriginalGauge(hudSafe,"Fervour",new Vector2(179,-83),118,10,"HUD/FervourFill",true,out unused);
  if(unused!=null)unused.gameObject.SetActive(false);
  flaskFull=LoadSprite("HUD/FlaskFull");flaskEmpty=LoadSprite("HUD/FlaskEmpty");flaskIcons=new Image[2];
  for(int i=0;i<flaskIcons.Length;i++){
   flaskIcons[i]=AddArt(hudSafe,"Bile flask "+(i+1),"HUD/FlaskFull",new Vector2(0,1),new Vector2(143+i*31,-107),new Vector2(28,44));
   flaskIcons[i].raycastTarget=false;
  }
  mapButton=Button(hudSafe,"MAP",new Vector2(1,1),new Vector2(-230,-48),48,OpenMapUI,null,true);
  bagButton=Button(hudSafe,"BAG",new Vector2(1,1),new Vector2(-165,-48),48,OpenInventoryUI,null,true);
  AddArt(hudSafe,"Tears frame","HUD/TearsFrame",new Vector2(1,1),new Vector2(-38,-38),new Vector2(174,92));
  tearsText=Label(hudSafe,"0000",new Vector2(1,1),new Vector2(-185,-64),new Vector2(115,34),24);
  tearsText.alignment=TextAnchor.MiddleRight;
  msgText=Label(hudSafe,"",new Vector2(.5f,.78f),new Vector2(-425,0),new Vector2(850,50),23);
  msgText.alignment=TextAnchor.MiddleCenter;
  BuildBossHud();
 }
 void BuildBossHud()
 {
  bossRoot=Panel(hudSafe,"UI_BOSS_HEALTH",new Vector2(.5f,0),new Vector2(0,32),new Vector2(450,40),Color.clear,false).gameObject;
  var frame=AddArt(bossRoot.GetComponent<RectTransform>(),"Boss Frame","UI/Sprites/inventory-spritesheet_72",new Vector2(.5f,.5f),Vector2.zero,new Vector2(346,30));
  var bg=Panel(frame.rectTransform,"Back",new Vector2(.5f,.5f),new Vector2(0,-2),new Vector2(289,8),new Color(.13f,.09f,.14f,.95f),false);
  bossLoss=GaugeLayer(bg,"Loss","UI/Sprites/inventory-spritesheet_121",new Color(.85f,.72f,.25f),8);
  boss=GaugeLayer(bg,"Fill","UI/Sprites/inventory-spritesheet_121",new Color(.85f,.15f,.12f),8);
  bossTextShadow=Label(bossRoot.GetComponent<RectTransform>(),"WARDEN OF THE SILENT SORROW",new Vector2(.5f,.5f),new Vector2(0,26),new Vector2(450,26),16);
  bossTextShadow.color=new Color(.02f,.03f,.03f,.95f);bossTextShadow.alignment=TextAnchor.MiddleCenter;
  bossText=Label(bossRoot.GetComponent<RectTransform>(),"WARDEN OF THE SILENT SORROW",new Vector2(.5f,.5f),new Vector2(-1,27),new Vector2(450,26),16);
  bossText.color=new Color(.95f,.75f,.35f);bossText.alignment=TextAnchor.MiddleCenter;
  bossRoot.SetActive(false);
 }
 void BuildControls()
 {
  jumpButton=Button(controlSafe,"JUMP",new Vector2(1f,0.5f),new Vector2(-432f,-306f),221,()=>qj=true,v=>tj=v,true);
  attackButton=Button(controlSafe,"ATTACK",new Vector2(1f,0.5f),new Vector2(-338.8f,-64.6f),171,()=>qa=true,v=>ta=v,true);
  dashButton=Button(controlSafe,"DASH",new Vector2(1f,0.5f),new Vector2(-152.9f,-414.4f),171,()=>qd=true,null,true);
  parryButton=Button(controlSafe,"PARRY",new Vector2(1f,0.5f),new Vector2(-104f,64.1f),123,()=>qp=true,null,true);
  flaskButton=Button(controlSafe,"FLASK",new Vector2(1f,0.5f),new Vector2(-115.8f,231.9f),109,()=>qf=true,null,true);
  prayerButton=Button(controlSafe,"PRAYER",new Vector2(1f,0.5f),new Vector2(-287.6f,185.4f),123,()=>qpr=true,null,true);
  specialButton=Button(controlSafe,"SPECIAL",new Vector2(1f,0.5f),new Vector2(-101f,-179.6f),171,()=>qsp=true,null,true);
  useButton=Button(controlSafe,"USE",new Vector2(1f,0.5f),new Vector2(-581.9f,34.2f),141,()=>qi=true,null,true);
  float scale=PlayerPrefs.GetFloat("BrotherhoodTouchScale",1);foreach(var o in new[]{jumpButton,attackButton,dashButton,parryButton,flaskButton,prayerButton,specialButton,useButton})o.transform.localScale=Vector3.one*scale;
   var zone=Panel(controlSafe,fixedJoystick?"Fixed joystick zone":"Floating joystick zone",Vector2.zero,Vector2.zero,Vector2.zero,Color.clear,false);zone.anchorMin=Vector2.zero;zone.anchorMax=new Vector2(0.5f,1f);zone.offsetMin=zone.offsetMax=Vector2.zero;var b=Panel(controlSafe,"Joystick base",new Vector2(0f,0.5f),new Vector2(320,-255),new Vector2(300,300),Color.white,false);b.pivot=new Vector2(.5f,.5f);b.anchoredPosition=new Vector2(320,-255);b.GetComponent<Image>().sprite=LoadSprite("MobileControls/HD_BaseJoystick");b.GetComponent<Image>().preserveAspect=true;var knob=Panel(b,"Joystick knob",new Vector2(.5f,.5f),Vector2.zero,new Vector2(125,125),Color.white,false);knob.GetComponent<Image>().sprite=LoadSprite("MobileControls/HD_ControlJoystick");knob.GetComponent<Image>().preserveAspect=true;b.localScale=Vector3.one*scale;var j=zone.gameObject.AddComponent<Joystick>();j.c=this;j.bas=b;j.knob=knob;j.fixedMode=fixedJoystick;
  doneLayoutButton=Button(controlSafe,"DONE LAYOUT",new Vector2(.5f,1),new Vector2(-90,-70),90,ExitRemap,null,false);doneLayoutButton.GetComponent<RectTransform>().sizeDelta=new Vector2(180,52);CenteredButtonLabel(doneLayoutButton,"DONE",150,16);doneLayoutButton.SetActive(false);
 }
 void BuildMenu(){menu=Panel(hudSafe,"Character menu",new Vector2(.5f,.5f),new Vector2(-500,-300),new Vector2(1000,600),new Color(.025f,.025f,.035f,.97f),false);var ol=menu.gameObject.AddComponent<Outline>();ol.effectColor=new Color(.75f,.48f,.18f);ol.effectDistance=new Vector2(2,-2);var title=Label(menu,"THE PENITENT ONE",new Vector2(.5f,1),new Vector2(-260,-22),new Vector2(520,45),28);title.alignment=TextAnchor.MiddleCenter;Tab("MAP",80,0);Tab("INVENTORY",260,1);Tab("RELICS",440,2);Tab("PRAYERS",620,3);Tab("SKILLS",800,4);menuText=Label(menu,"",new Vector2(0,1),new Vector2(70,-135),new Vector2(860,300),22);menuIcons=new Image[7];shownItems=new InventoryCatalog.Item[7];for(int i=0;i<menuIcons.Length;i++){int slot=i;menuIcons[i]=AddArt(menu,"Inventory icon "+i,"Inventory/Icons/RE01",new Vector2(0,1),new Vector2(90+i*105,-430),new Vector2(76,76));menuIcons[i].raycastTarget=true;menuIcons[i].gameObject.AddComponent<Outline>().effectColor=new Color(.65f,.42f,.18f);menuIcons[i].gameObject.AddComponent<TouchButton>().press=()=>SelectShownItem(slot);}var equip=Button(menu,"EQUIP",new Vector2(0,0),new Vector2(70,22),130,EquipSelected,null,false);equip.GetComponent<RectTransform>().sizeDelta=new Vector2(180,52);equipText=CenteredButtonLabel(equip,"EQUIP",160,16);MenuCommand("PREVIOUS",280,()=>TurnInventoryPage(-1));MenuCommand("NEXT",440,()=>TurnInventoryPage(1));MenuCommand("CONTROLS",600,EnterRemap);var close=Button(menu,"CLOSE",new Vector2(1,0),new Vector2(-220,22),150,Toggle,null,false);close.GetComponent<RectTransform>().sizeDelta=new Vector2(150,52);CenteredButtonLabel(close,"CLOSE",120,16);menu.gameObject.SetActive(false);}
 Text CenteredButtonLabel(GameObject button,string caption,float width,int size){var label=Label(button.GetComponent<RectTransform>(),caption,new Vector2(.5f,.5f),new Vector2(-width*.5f,0),new Vector2(width,40),size);label.alignment=TextAnchor.MiddleCenter;return label;}
 void MenuCommand(string caption,float x,System.Action action){var b=Button(menu,caption,new Vector2(0,0),new Vector2(x,22),110,action,null,false);b.GetComponent<RectTransform>().sizeDelta=new Vector2(140,52);CenteredButtonLabel(b,caption,120,14);}
 void Tab(string text,float x,int page){var b=Button(menu,"TAB "+text,new Vector2(0,1),new Vector2(x,-82),42,()=>OpenPage(page),null,false);b.GetComponent<RectTransform>().sizeDelta=new Vector2(160,44);CenteredButtonLabel(b,text,145,15);}
 void OpenPage(int page){inventoryOffset=0;ShowPage(page);}
  void ToggleMap(){menuPage=0;Toggle();}void ToggleBag(){menuPage=1;Toggle();}public void ToggleOptions(){if(optionsUI!=null)optionsUI.Toggle();}
  public void OpenMapUI(){if(mapUI!=null)mapUI.Toggle();else ToggleMap();}public void OpenInventoryUI(){if(inventoryUI!=null)inventoryUI.Toggle();else ToggleBag();}
 void Toggle(){bool show=!menu.gameObject.activeSelf;for(int i=0;i<hudSafe.childCount;i++){var child=hudSafe.GetChild(i);if(child!=menu)child.gameObject.SetActive(!show);}controlSafe.gameObject.SetActive(!show);menu.gameObject.SetActive(show);Time.timeScale=show?0:1;if(show)ShowPage(menuPage);}
 public void EnterRemap(){Toggle();Remapping=true;doneLayoutButton.SetActive(true);MessageForLayout("DRAG BUTTONS · TAP DONE TO SAVE");}
 void ExitRemap(){Remapping=false;doneLayoutButton.SetActive(false);PlayerPrefs.Save();Clear();MessageForLayout("TOUCH LAYOUT SAVED");}
 void MessageForLayout(string text){if(msgText!=null){msgText.text=text;oldMsg=text;}}
 public void SaveControlPosition(string key,RectTransform rect){if(string.IsNullOrEmpty(key)||rect==null)return;PlayerPrefs.SetFloat("BrotherhoodControl_"+key+"_X",rect.anchoredPosition.x);PlayerPrefs.SetFloat("BrotherhoodControl_"+key+"_Y",rect.anchoredPosition.y);}
 void LoadControlPosition(string key,RectTransform rect){string x="BrotherhoodControl_"+key+"_X",y="BrotherhoodControl_"+key+"_Y";if(PlayerPrefs.HasKey(x)&&PlayerPrefs.HasKey(y))rect.anchoredPosition=new Vector2(PlayerPrefs.GetFloat(x),PlayerPrefs.GetFloat(y));}
 void ShowPage(int page){menuPage=page;if(menuText==null||game==null)return;var p=game.progress;if(page==0)menuText.text="MAP · BROTHERHOOD OF THE SILENT SORROW\n\nAWAKENING  →  HALLWAY  →  WARDEN  →  PRIE DIEU\n\nCURRENT ROOM  "+game.Current.id+"\nRESTORED ROOMS  "+game.rooms.Length+" / 5";else if(page==1)menuText.text="INVENTORY\n\nHEALTH  "+Mathf.CeilToInt(game.player.health)+" / "+Mathf.CeilToInt(game.player.MaxHealth)+"\nFERVOUR  "+Mathf.CeilToInt(game.player.fervour)+" / "+Mathf.CeilToInt(game.player.MaxFervour)+"\nBILE FLASKS  "+game.player.flasks+" / "+game.player.MaxFlasks+"\nTEARS OF ATONEMENT  "+Mathf.FloorToInt(p.tears)+"\n\nSOURCE CATALOG  "+(inventory==null?0:inventory.items.Length)+" ITEMS";else if(page==2)menuText.text="RELICS  "+(inventory==null?0:inventory.Count("relic"))+"\n\n"+(inventory==null?"BLOOD PERPETUATED IN SAND":inventory.Caption("RE01"))+"\n"+(game.BloodRelicOwned?(game.BloodRelicEquipped?"EQUIPPED":"OWNED · READY TO EQUIP"):"NOT FOUND");else if(page==3)menuText.text="PRAYERS  "+(inventory==null?0:inventory.Count("prayer"))+"\n\n"+(p.hasPrayer?"PRAYER EQUIPPED\nConsumes Fervour and invokes its source effect.":"NO PRAYER FOUND");else menuText.text="MEA CULPA SKILLS\n\nCHARGED ATTACK  TIER "+p.chargedTier+"\nSACRED ONSLAUGHT  TIER "+p.lungeTier+"\nFERVOROUS BLOOD  TIER "+p.rangedTier+"\nSWORD HEARTS  "+(inventory==null?0:inventory.Count("sword"))+"\nROSARY BEADS  "+(inventory==null?0:inventory.Count("rosarybead"))+"\nPARRY  "+(p.canParry?"UNLOCKED":"LOCKED")+"\nDASH  "+(p.canDash?"UNLOCKED":"LOCKED");ShowIcons(page==1?"rosarybead":page==2?"relic":page==3?"prayer":page==4?"sword":null);}
 void ShowIcons(string category){int slot=0,seen=0;if(menuIcons==null)return;selectedItem=null;if(inventory!=null&&category!=null)foreach(var item in inventory.items)if(item.category==category){if(seen++<inventoryOffset)continue;if(slot>=menuIcons.Length)break;var sprite=InventorySprite(item.icon);shownItems[slot]=item;menuIcons[slot].sprite=sprite;menuIcons[slot].color=game.progress.Owns(item.id)?Color.white:new Color(.25f,.25f,.25f,.7f);menuIcons[slot].gameObject.SetActive(sprite!=null);if(selectedItem==null&&game.progress.Owns(item.id))selectedItem=item;slot++;}for(;slot<menuIcons.Length;slot++){shownItems[slot]=null;menuIcons[slot].gameObject.SetActive(false);}RefreshEquipText();}
 void TurnInventoryPage(int direction){string category=menuPage==1?"rosarybead":menuPage==2?"relic":menuPage==3?"prayer":menuPage==4?"sword":null;if(category==null||inventory==null)return;int count=inventory.Count(category),pages=Mathf.Max(1,Mathf.CeilToInt(count/7f)),page=Mathf.Clamp(inventoryOffset/7+direction,0,pages-1);inventoryOffset=page*7;ShowIcons(category);}
 void SelectShownItem(int slot){if(slot<0||slot>=shownItems.Length||shownItems[slot]==null)return;selectedItem=shownItems[slot];RefreshEquipText();ShowPageText();}
 void EquipSelected(){if(selectedItem!=null&&game.EquipInventoryItem(selectedItem)){RefreshEquipText();ShowPageText();}}
 void RefreshEquipText(){if(equipText==null)return;equipText.text=selectedItem==null?"SELECT ITEM":!game.progress.Owns(selectedItem.id)?"NOT OWNED":game.progress.IsEquipped(selectedItem.id)?"UNEQUIP":"EQUIP";}
 void ShowPageText(){if(selectedItem==null||menuText==null)return;string description=string.IsNullOrEmpty(selectedItem.description)?"":selectedItem.description+"\n\n";menuText.text=selectedItem.category.ToUpperInvariant()+" · "+selectedItem.id+"\n\n"+selectedItem.caption+"\n\n"+description+(game.progress.Owns(selectedItem.id)?(game.progress.IsEquipped(selectedItem.id)?"EQUIPPED":"OWNED") : "NOT FOUND");}
 Sprite InventorySprite(string path){if(string.IsNullOrEmpty(path))return null;if(inventorySprites.TryGetValue(path,out var sprite))return sprite;var tex=Resources.Load<Texture2D>(path);if(tex==null)return null;tex.filterMode=FilterMode.Point;sprite=Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),new Vector2(.5f,.5f),100);inventorySprites[path]=sprite;return sprite;}
 void Safe(){lastSafe=Screen.safeArea;Apply(hudSafe);Apply(controlSafe);}void Apply(RectTransform r){float w=Mathf.Max(1,Screen.width),h=Mathf.Max(1,Screen.height);Vector2 min=lastSafe.position/new Vector2(w,h),max=(lastSafe.position+lastSafe.size)/new Vector2(w,h);r.anchorMin=new Vector2(Mathf.Clamp01(min.x),Mathf.Clamp01(min.y));r.anchorMax=new Vector2(Mathf.Clamp01(max.x),Mathf.Clamp01(max.y));r.offsetMin=r.offsetMax=Vector2.zero;}
 void LateUpdate(){if(hudSafe==null||game.player==null)return;if(lastSafe!=Screen.safeArea)Safe();int h=Mathf.CeilToInt(game.player.health),f=Mathf.CeilToInt(game.player.fervour),fl=game.player.flasks;float target=h/game.player.MaxHealth;shownHealth=Mathf.Lerp(shownHealth,target,Mathf.Clamp01(Time.unscaledDeltaTime*8));shownLoss=Mathf.MoveTowards(shownLoss,target,Time.unscaledDeltaTime*.35f);if(shownLoss<shownHealth)shownLoss=shownHealth;hp.fillAmount=shownHealth;hpLoss.fillAmount=shownLoss;fervour.fillAmount=f/game.player.MaxFervour;if(fl!=oldFlask){for(int i=0;i<flaskIcons.Length;i++)flaskIcons[i].sprite=i<fl?flaskFull:flaskEmpty;oldFlask=fl;}var p=game.progress;tearsText.text=Mathf.FloorToInt(p.tears).ToString("0000");jumpButton.SetActive(Remapping||p.canJump);attackButton.SetActive(Remapping||p.hasMeaCulpa);dashButton.SetActive(Remapping||p.canDash);parryButton.SetActive(Remapping||p.canParry);flaskButton.SetActive(Remapping||(p.hasFlask&&fl>0));prayerButton.SetActive(Remapping||p.hasPrayer);specialButton.SetActive(true);// Always show RangeAttack - unlocked from map 1 like original mobile
mapButton.SetActive(Remapping||p.hasMap);bagButton.SetActive(Remapping||p.hasMeaCulpa||p.hasPrayer||p.hasSpecial||p.bloodOwned);useButton.SetActive(Remapping||game.CanInteract);string m=game.MessageText;if(!Remapping&&m!=oldMsg){msgText.text=m;oldMsg=m;}var b=game.Current.Boss;bool show=b!=null&&!b.Dead;if(bossRoot!=null)bossRoot.SetActive(show);else{boss.transform.parent.gameObject.SetActive(show);bossText.gameObject.SetActive(show);}if(show){float targetBoss=Mathf.Clamp01(b.health/b.maxHealth);boss.fillAmount=targetBoss;if(bossLoss!=null)bossLoss.fillAmount=Mathf.MoveTowards(bossLoss.fillAmount,targetBoss,Time.unscaledDeltaTime*.4f);}}
 Sprite LoadSprite(string path){var t=Resources.Load<Texture2D>(path);if(t==null)return Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),new Vector2(.5f,.5f));t.filterMode=FilterMode.Point;return Sprite.Create(t,new Rect(0,0,t.width,t.height),new Vector2(.5f,.5f),100);}
 Image AddArt(RectTransform p,string name,string path,Vector2 anchor,Vector2 pos,Vector2 size){var o=new GameObject(name,typeof(RectTransform),typeof(Image));var r=o.GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=anchor;r.pivot=new Vector2(anchor.x,anchor.y);r.anchoredPosition=pos;r.sizeDelta=size;var i=o.GetComponent<Image>();i.sprite=LoadSprite(path);i.preserveAspect=true;i.raycastTarget=false;return i;}
  Image OriginalGauge(RectTransform p,string name,Vector2 pos,float w,float h,string fillPath,bool thirds,out Image loss){
   var root=Panel(p,name+" source gauge",new Vector2(0,1),pos,new Vector2(w,h),Color.clear,false);
   var bgBox=Panel(root,name+" back",Vector2.zero,Vector2.zero,Vector2.zero,new Color(0.133f,0.094f,0.137f,0.98f),false);
   bgBox.anchorMin=Vector2.zero;bgBox.anchorMax=Vector2.one;bgBox.offsetMin=new Vector2(0,1);bgBox.offsetMax=new Vector2(-4,-1);
   var back=root.GetComponent<Image>();
   back.sprite=LoadSprite("HUD/BarMid");
   back.type=Image.Type.Tiled;
   back.pixelsPerUnitMultiplier=2;
   back.color=Color.white;
   back.raycastTarget=false;
   loss=GaugeLayer(root,name+" loss","HUD/HealthLoss",new Color(1,.72f,.18f),h);
   var fill=GaugeLayer(root,name+" fill",fillPath,Color.white,h);
   var endcap=AddArt(root,name+" end","HUD/BarEnd",new Vector2(1,.5f),new Vector2(-4,0),new Vector2(20,16));
   endcap.rectTransform.pivot=new Vector2(0,0.5f);
   if(thirds){
    for(int n=1;n<=2;n++){
     var tick=AddArt(root,name+" division "+n,"HUD/BarMid",new Vector2(0,.5f),new Vector2(w*n/3f,-h*.5f),new Vector2(3,h+2));
     tick.rectTransform.localRotation=Quaternion.Euler(0,0,90);
     tick.color=new Color(0.95f,0.85f,0.45f,0.95f);
    }
   }
   return fill;
  }
  Image GaugeLayer(RectTransform root,string name,string path,Color color,float h){
   var i=new GameObject(name,typeof(RectTransform),typeof(Image)).GetComponent<Image>();
   i.transform.SetParent(root,false);
   i.rectTransform.anchorMin=Vector2.zero;
   i.rectTransform.anchorMax=Vector2.one;
   i.rectTransform.offsetMin=new Vector2(0,Mathf.Max(1,(h-6)*.5f));
   i.rectTransform.offsetMax=new Vector2(-4,-Mathf.Max(1,(h-6)*.5f));
   i.sprite=LoadSprite(path);
   i.type=Image.Type.Filled;
   i.fillMethod=Image.FillMethod.Horizontal;
   i.fillOrigin=0;
   i.color=color;
   i.raycastTarget=false;
   return i;
  }
 Image Bar(RectTransform p,Vector2 pos,float w,float h,Color col){var back=Panel(p,"Gauge",new Vector2(0,1),pos,new Vector2(w,h),new Color(.09f,.06f,.07f,.9f),false);var i=new GameObject("Fill",typeof(RectTransform),typeof(Image)).GetComponent<Image>();i.transform.SetParent(back,false);i.rectTransform.anchorMin=Vector2.zero;i.rectTransform.anchorMax=Vector2.one;i.rectTransform.offsetMin=i.rectTransform.offsetMax=Vector2.zero;i.sprite=Sprite.Create(Texture2D.whiteTexture,new Rect(0,0,1,1),Vector2.zero);i.type=Image.Type.Filled;i.fillMethod=Image.FillMethod.Horizontal;i.color=col;i.raycastTarget=false;return i;}
 RectTransform Panel(RectTransform p,string n,Vector2 a,Vector2 pos,Vector2 size,Color col,bool circle){var o=new GameObject(n,typeof(RectTransform),typeof(Image));var r=o.GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=a;r.pivot=a==new Vector2(.5f,.5f)?new Vector2(.5f,.5f):new Vector2(0,a.y);r.anchoredPosition=pos;r.sizeDelta=size;var i=o.GetComponent<Image>();i.color=col;if(circle)i.sprite=disc;return r;}
 Text Label(RectTransform p,string value,Vector2 a,Vector2 pos,Vector2 size,int fs){var o=new GameObject("Label",typeof(RectTransform),typeof(Text));var r=o.GetComponent<RectTransform>();r.SetParent(p,false);r.anchorMin=r.anchorMax=a;r.pivot=new Vector2(0,a.y);r.anchoredPosition=pos;r.sizeDelta=size;var t=o.GetComponent<Text>();if(originalFont==null)originalFont=Resources.Load<Font>("Fonts/Caudex-Bold");if(originalFont==null)originalFont=Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");t.font=originalFont!=null?originalFont:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.text=value;t.fontSize=fs;t.color=new Color(.95f,.72f,.32f);t.raycastTarget=false;return t;}
 GameObject Button(RectTransform p,string value,Vector2 a,Vector2 pos,float size,System.Action press,System.Action<bool> held,bool circle)
 {
  string mobilePath=MobileControlPath(value);bool originalMobile=circle&&mobilePath!=null&&Resources.Load<Texture2D>(mobilePath)!=null;
  var r=Panel(p,value,a,pos,new Vector2(size,size),originalMobile?Color.white:new Color(.035f,.03f,.04f,.58f),circle&&!originalMobile);if(circle)r.pivot=new Vector2(.5f,.5f);if(originalMobile){var image=r.GetComponent<Image>();image.sprite=LoadSprite(mobilePath);image.preserveAspect=true;}else{var ol=r.gameObject.AddComponent<Outline>();ol.effectColor=new Color(.93f,.57f,.22f,.95f);ol.effectDistance=new Vector2(2,-2);}var h=r.gameObject.AddComponent<TouchButton>();h.press=press;h.held=held;h.owner=this;h.layoutKey=circle?value:null;if(circle)LoadControlPosition(value,r);
  var icon=new GameObject(value+" icon",typeof(RectTransform),typeof(Image)).GetComponent<Image>();icon.transform.SetParent(r,false);icon.rectTransform.anchorMin=new Vector2(.2f,.2f);icon.rectTransform.anchorMax=new Vector2(.8f,.8f);icon.rectTransform.offsetMin=icon.rectTransform.offsetMax=Vector2.zero;icon.sprite=MakeIcon(value);icon.preserveAspect=true;icon.raycastTarget=false;icon.gameObject.SetActive(circle&&!originalMobile);return r.gameObject;
 }
 string MobileControlPath(string value){switch(value){case "ATTACK":return "MobileControls/Attack";case "JUMP":return "MobileControls/Jump";case "DASH":return "MobileControls/Dash";case "PARRY":return "MobileControls/Parry";case "FLASK":return "MobileControls/Flask";case "PRAYER":return "MobileControls/Prayer";case "SPECIAL":return "MobileControls/RangeAttack";case "USE":return "MobileControls/Interact";case "MAP":return "MobileControls/Map";case "BAG":return "MobileControls/Inventory";default:return null;}}
 Sprite MakeIcon(string name)
 {
  const int s=64;var t=new Texture2D(s,s,TextureFormat.RGBA32,false);t.name="Mobile "+name;var clear=new Color32[s*s];t.SetPixels32(clear);Color gold=new Color(.95f,.65f,.25f);
  System.Action<int,int,int,int> line=(x0,y0,x1,y1)=>{int dx=Mathf.Abs(x1-x0),sx=x0<x1?1:-1,dy=-Mathf.Abs(y1-y0),sy=y0<y1?1:-1,err=dx+dy;while(true){for(int y=-2;y<=2;y++)for(int x=-2;x<=2;x++)if(x0+x>=0&&x0+x<s&&y0+y>=0&&y0+y<s)t.SetPixel(x0+x,y0+y,gold);if(x0==x1&&y0==y1)break;int e=2*err;if(e>=dy){err+=dy;x0+=sx;}if(e<=dx){err+=dx;y0+=sy;}}};
  if(name=="ATTACK"){line(17,14,46,49);line(12,10,23,17);line(42,48,49,55);}
  else if(name=="JUMP"){line(32,10,32,51);line(32,51,17,35);line(32,51,47,35);}
  else if(name=="DASH"){line(10,25,47,25);line(18,35,54,35);line(54,35,43,45);line(54,35,43,25);}
  else if(name=="PARRY"){line(18,49,18,20);line(18,49,32,56);line(32,56,46,49);line(46,49,46,20);line(18,20,32,13);line(32,13,46,20);}
  else if(name=="FLASK"){line(25,49,21,22);line(21,22,27,14);line(27,14,37,14);line(37,14,43,22);line(43,22,39,49);line(25,49,39,49);}
  else if(name=="MAP"){line(10,13,26,18);line(26,18,39,13);line(39,13,54,18);line(10,13,10,49);line(10,49,26,54);line(26,18,26,54);line(26,54,39,49);line(39,13,39,49);line(39,49,54,54);line(54,18,54,54);}
  else if(name=="BAG"){line(15,24,49,24);line(15,24,12,52);line(12,52,52,52);line(52,52,49,24);line(24,24,24,15);line(24,15,40,15);line(40,15,40,24);}
  else{line(32,10,32,54);line(10,32,54,32);}t.Apply();return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),64);
 }
 Sprite MakeDisc(){const int s=64;var t=new Texture2D(s,s,TextureFormat.RGBA32,false);var c=new Color32[s*s];for(int y=0;y<s;y++)for(int x=0;x<s;x++){float d=Vector2.Distance(new Vector2(x,y),new Vector2(31.5f,31.5f));c[y*s+x]=d<32?new Color32(255,255,255,(byte)Mathf.Clamp((33-d)*255,0,255)):new Color32(0,0,0,0);}t.SetPixels32(c);t.Apply();return Sprite.Create(t,new Rect(0,0,s,s),new Vector2(.5f,.5f),64);}
 void Clear(){tm=0;Vertical=0;ti=false;InteractHeld=false;tj=ta=td=false;qj=qa=qd=qp=qf=qpr=qsp=qi=false;}void OnApplicationFocus(bool x){if(!x)Clear();}void OnApplicationPause(bool x){if(x)Clear();}void OnDestroy(){Time.timeScale=1;}
 public sealed class TouchButton:MonoBehaviour,IPointerDownHandler,IPointerUpHandler,IPointerExitHandler,IDragHandler{public System.Action press;public System.Action<bool> held;public TouchControls owner;public string layoutKey;int id=int.MinValue;Vector3 rest;public void OnPointerDown(PointerEventData e){if(id!=int.MinValue)return;id=e.pointerId;rest=transform.localScale;if(owner!=null&&owner.Remapping&&!string.IsNullOrEmpty(layoutKey))return;transform.localScale=rest*.94f;if(Application.platform==RuntimePlatform.Android&&PlayerPrefs.GetInt("BrotherhoodHaptics",1)==1)Handheld.Vibrate();press?.Invoke();held?.Invoke(true);}public void OnDrag(PointerEventData e){if(id!=e.pointerId||owner==null||!owner.Remapping||string.IsNullOrEmpty(layoutKey))return;var rect=(RectTransform)transform;float scale=GetComponentInParent<Canvas>().scaleFactor;rect.anchoredPosition+=e.delta/Mathf.Max(.01f,scale);}public void OnPointerUp(PointerEventData e){if(id==e.pointerId){id=int.MinValue;transform.localScale=rest;if(owner!=null&&owner.Remapping&&!string.IsNullOrEmpty(layoutKey))owner.SaveControlPosition(layoutKey,(RectTransform)transform);else held?.Invoke(false);}}public void OnPointerExit(PointerEventData e){if(id==e.pointerId&&!(owner!=null&&owner.Remapping&&!string.IsNullOrEmpty(layoutKey))){id=int.MinValue;transform.localScale=rest;held?.Invoke(false);}}void OnDisable(){if(id!=int.MinValue){id=int.MinValue;transform.localScale=rest;held?.Invoke(false);}}}
 public sealed class Joystick:MonoBehaviour,IPointerDownHandler,IDragHandler,IPointerUpHandler
 {
   public TouchControls c;public RectTransform bas,knob;public bool fixedMode;int id=int.MinValue;Vector2 origin;
   public void OnPointerDown(PointerEventData e){if(id!=int.MinValue)return;id=e.pointerId;if(fixedMode)origin=bas.anchoredPosition;else{RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)bas.parent,e.position,e.pressEventCamera,out origin);bas.anchoredPosition=origin;}OnDrag(e);}
   public void OnDrag(PointerEventData e)
   {
    if(id!=e.pointerId)return;
    RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)bas.parent,e.position,e.pressEventCamera,out var p);
    var d=Vector2.ClampMagnitude(p-origin,80);knob.anchoredPosition=d;
    float x=d.x/80;c.tm=Mathf.Abs(x)<.15f?0:Mathf.Clamp(x,-1,1);c.Vertical=Mathf.Abs(d.y)<15?0:Mathf.Clamp(d.y/80,-1,1);c.td=d.y< -40;
   }
   public void OnPointerUp(PointerEventData e){if(id!=e.pointerId)return;id=int.MinValue;c.tm=0;c.Vertical=0;c.td=false;knob.anchoredPosition=Vector2.zero;if(!fixedMode)bas.anchoredPosition=new Vector2(320,-255);}
 }
}
}
