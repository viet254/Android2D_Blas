import re

with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\TouchControls.cs', 'r', encoding='utf-8') as f:
    tc_code = f.read()

# Exact HUD coordinates verified with PIL mockup:
# Health bar: (132, -62), 220, 12
# Fervour bar: (152, -86), 150, 8
# Flasks: (112 + i * 32, -104), size 28, 44
new_build_hud = ''' void BuildHud()
 {
  AddArt(hudSafe,"Penitent portrait","HUD/PortraitFrame",new Vector2(0,1),new Vector2(36,-42),new Vector2(176,116));
  hp=OriginalGauge(hudSafe,"Health",new Vector2(132,-62),220,12,"HUD/HealthFill",false,out hpLoss);
  Image unused;
  fervour=OriginalGauge(hudSafe,"Fervour",new Vector2(152,-86),150,8,"HUD/FervourFill",true,out unused);
  if(unused!=null)unused.gameObject.SetActive(false);
  flaskFull=LoadSprite("HUD/FlaskFull");flaskEmpty=LoadSprite("HUD/FlaskEmpty");flaskIcons=new Image[2];
  for(int i=0;i<flaskIcons.Length;i++){
   flaskIcons[i]=AddArt(hudSafe,"Bile flask "+(i+1),"HUD/FlaskFull",new Vector2(0,1),new Vector2(112+i*32,-104),new Vector2(28,44));
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
 }'''

tc_code = re.sub(r'void BuildHud\(\)\s*\{.*?(?=void BuildBossHud)', new_build_hud + '\n ', tc_code, flags=re.DOTALL)

with open(r'D:\game\Android2D_Blas\Assets\Brotherhood\Runtime\TouchControls.cs', 'w', encoding='utf-8') as f:
    f.write(tc_code)
print("Updated TouchControls.cs BuildHud to mathematically verified coordinates!")
