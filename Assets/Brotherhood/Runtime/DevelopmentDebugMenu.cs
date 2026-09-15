using UnityEngine;
namespace Brotherhood
{
 public sealed class DevelopmentDebugMenu:MonoBehaviour
 {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
  public BrotherhoodGame game;bool open;Rect window=new Rect(18,55,330,470);
  void Update(){if(KeyboardShortcut())open=!open;}
  bool KeyboardShortcut(){var k=UnityEngine.InputSystem.Keyboard.current;return k!=null&&k.f1Key.wasPressedThisFrame;}
  void OnGUI()
  {
   if(!Debug.isDebugBuild&&!Application.isEditor)return;
   if(!open||game==null||game.player==null)return;
   if(GUI.Button(new Rect(12,12,88,38),"CLOSE"))open=false;
   window=GUI.Window(8172,window,Draw,"Brotherhood test tools");
  }
  void Draw(int id)
  {
   GUILayout.Label("HP "+Mathf.CeilToInt(game.player.health));game.player.health=GUILayout.HorizontalSlider(game.player.health,1,100);
   GUILayout.Label("Fervour "+Mathf.CeilToInt(game.player.fervour));game.player.fervour=GUILayout.HorizontalSlider(game.player.fervour,0,game.player.MaxFervour);
   game.progress.canDash=GUILayout.Toggle(game.progress.canDash,"Dash");game.progress.canParry=GUILayout.Toggle(game.progress.canParry,"Parry");game.progress.hasMeaCulpa=GUILayout.Toggle(game.progress.hasMeaCulpa,"Mea Culpa");game.progress.hasFlask=GUILayout.Toggle(game.progress.hasFlask,"Bile Flask");
   GUILayout.BeginHorizontal();GUILayout.Label("Charged tier "+game.progress.chargedTier);if(GUILayout.Button("+",GUILayout.Width(32)))game.progress.chargedTier=(game.progress.chargedTier+1)%4;GUILayout.EndHorizontal();
   GUILayout.BeginHorizontal();GUILayout.Label("Lunge tier "+game.progress.lungeTier);if(GUILayout.Button("+",GUILayout.Width(32)))game.progress.lungeTier=(game.progress.lungeTier+1)%4;GUILayout.EndHorizontal();
   GUILayout.BeginHorizontal();GUILayout.Label("Ranged tier "+game.progress.rangedTier);if(GUILayout.Button("+",GUILayout.Width(32)))game.progress.rangedTier=(game.progress.rangedTier+1)%4;GUILayout.EndHorizontal();
   if(GUILayout.Button("Special: "+(game.progress.specialMode==0?"Sacred Onslaught":"Fervorous Blood")))game.progress.specialMode=1-game.progress.specialMode;
   bool relic=GUILayout.Toggle(game.BloodRelicEquipped,"Blood Perpetuated in Sand");if(relic!=game.BloodRelicEquipped)game.SetBloodRelic(relic,relic);
   if(GUILayout.Button("Restore"))game.player.Restore();if(GUILayout.Button("Unlock core"))game.progress.UnlockCore();
   GUILayout.Label("Rooms");foreach(var room in game.rooms)if(GUILayout.Button(room.id))game.Enter(room.id,null,null);
   GUI.DragWindow();
  }
#endif
 }
}
