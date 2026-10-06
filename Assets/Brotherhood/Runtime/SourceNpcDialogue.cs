using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace Brotherhood
{
    // Bounded adapters for Tirso's delivery quest and Soledad's knot exchange.
    // Dialog terms, item order, rewards and flags come from their source FSMs.
    public sealed class SourceNpcDialogue:MonoBehaviour
    {
        public static readonly string[] Herbs={"QI19","QI20","QI37","QI63","QI64","QI65"};
        public static readonly string[] Knots={"QI44","QI52","QI53","QI54","QI55","QI56"};
        BrotherhoodGame game;GameObject root,choices;Text title,body;readonly Queue<string> lines=new Queue<string>();Action afterLines,confirm;string npc;float inputClock;
        public bool IsOpen=>root!=null&&root.activeSelf;
        public Canvas Canvas{get;private set;}
        public string NearNpc
        {
            get
            {
                if(game.Current==null||game.player.Dead||!game.player.motor.grounded)return null;
                string name=game.Current.id=="D01Z02S02"?"Tirso":game.Current.id=="D17Z01S09"&&!game.progress.HasFlag("ST21_JAILED_GHOST/JAILED_QUEST_COMPLETED")?"Jailed Ghost":null;
                if(name==null)return null;
                foreach(var node in game.Current.GetComponentsInChildren<SourceObjectId>(true))if(node.name==name&&node.gameObject.activeInHierarchy&&Mathf.Abs(node.transform.position.x-game.player.transform.position.x)<1.8f&&Mathf.Abs(node.transform.position.y-game.player.transform.position.y)<2)return name=="Tirso"?"Tirso":"Soledad";
                return null;
            }
        }
        public void Initialize(BrotherhoodGame owner)
        {
            game=owner;var obj=new GameObject("Source quest dialogue",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));obj.transform.SetParent(transform,false);var canvas=obj.GetComponent<Canvas>();Canvas=canvas;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=70;var scaler=obj.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
            root=Rect(obj.transform,"Dialogue",new Vector2(.06f,.03f),new Vector2(.94f,.40f)).gameObject;var image=root.AddComponent<Image>();image.sprite=Resources.Load<Sprite>("Dialogue/dialog_background");image.color=Color.white;
            title=Label(root.transform,"Speaker",new Vector2(.06f,.78f),new Vector2(.94f,.96f),23,new Color(.94f,.69f,.33f));body=Label(root.transform,"Source line",new Vector2(.06f,.22f),new Vector2(.94f,.77f),23,new Color(.93f,.89f,.78f));
            var next=Rect(root.transform,"Advance",new Vector2(.65f,.01f),new Vector2(.94f,.18f));next.gameObject.AddComponent<Image>().color=new Color(.25f,.13f,.05f,.65f);next.gameObject.AddComponent<Button>().onClick.AddListener(Advance);Label(next,"Advance label",Vector2.zero,Vector2.one,20,Color.white).text="Tiếp tục";
            choices=Rect(root.transform,"Quest choice",Vector2.zero,new Vector2(1,.20f)).gameObject;
            Choice("Đưa vật phẩm",new Vector2(.10f,.02f),new Vector2(.48f,.95f),()=>{var action=confirm;Close();action?.Invoke();});Choice("Giữ lại",new Vector2(.52f,.02f),new Vector2(.90f,.95f),Close);
            root.SetActive(false);
        }
        void Choice(string caption,Vector2 min,Vector2 max,UnityEngine.Events.UnityAction action){var rect=Rect(choices.transform,caption,min,max);rect.gameObject.AddComponent<Image>().color=new Color(.25f,.13f,.05f,.96f);rect.gameObject.AddComponent<Button>().onClick.AddListener(action);Label(rect,caption,Vector2.zero,Vector2.one,20,Color.white).text=caption;}
        Text Label(Transform parent,string name,Vector2 min,Vector2 max,int size,Color color){var t=Rect(parent,name,min,max).gameObject.AddComponent<Text>();t.font=VietnameseSource.DynamicFont??Resources.Load<Font>("Fonts/Caudex-Bold")??Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");t.fontSize=size;t.color=color;t.alignment=TextAnchor.MiddleLeft;t.raycastTarget=false;return t;}
        static RectTransform Rect(Transform parent,string name,Vector2 min,Vector2 max){var o=new GameObject(name,typeof(RectTransform));var r=(RectTransform)o.transform;r.SetParent(parent,false);r.anchorMin=min;r.anchorMax=max;r.offsetMin=r.offsetMax=Vector2.zero;return r;}
        public bool TryInteract()
        {
            if(IsOpen)return true;npc=NearNpc;if(npc==null)return false;title.text=npc;root.SetActive(true);choices.SetActive(false);game.SetEncounterInputBlocked(true);
            if(npc=="Tirso")
            {
                bool first=!game.progress.HasFlag("ST03_TIRSO/TIRSO_FIRSTCONVERSATION_DONE");
                Show(first?new[]{"DLG_0301","DLG_0311","DLG_0312"}:new[]{"DLG_0311","DLG_0302"},"ST03_TIRSO",()=>{game.progress.SetFlag("ST03_TIRSO/TIRSO_FIRSTCONVERSATION_DONE");game.SaveGame();OfferTirso();});
            }
            else Show(game.progress.HasFlag("ST21_JAILED_GHOST/JAILED_FIRSTCONVERSATION_DONE")?Array.Empty<string>():new[]{"DLG_2101"},"ST21_JAILED_GHOST",()=>{game.progress.SetFlag("ST21_JAILED_GHOST/JAILED_FIRSTCONVERSATION_DONE");game.SaveGame();OfferKnot();});
            return true;
        }
        void Show(string[] dialogs,string group,Action complete)
        {
            lines.Clear();foreach(string id in dialogs)for(int index=0;index<40;index++){string term=VietnameseSource.Term(group+"/"+id+"_"+index);if(string.IsNullOrEmpty(term))break;lines.Enqueue(term);}
            afterLines=complete;Advance();
        }
        public void Advance(){if(!IsOpen||choices.activeSelf)return;inputClock=.2f;if(lines.Count>0){body.text=lines.Dequeue();return;}var action=afterLines;afterLines=null;if(action!=null)action();else Close();}
        void Question(string group,string id,Action action){Show(new[]{id},group,()=>{confirm=action;choices.SetActive(true);});}
        void OfferTirso()
        {
            int index=Array.FindIndex(Herbs,id=>game.progress.Owns(id)&&!game.progress.HasFlag("ST03_TIRSO/TIRSO_"+id+"_DELIVERED"));
            if(index<0){Close();return;}string herb=Herbs[index];Question("ST03_TIRSO","DLG_QT_030"+(index+1),()=>DeliverHerb(herb));
        }
        void OfferKnot()
        {
            string knot=Array.Find(Knots,id=>game.progress.Owns(id)&&!game.progress.HasFlag("ST21_JAILED_GHOST/USED_"+id));
            if(knot==null||game.progress.rosarySlots>=8){Show(new[]{"DLG_2102"},"ST21_JAILED_GHOST",Close);return;}
            Show(new[]{"DLG_2103"},"ST21_JAILED_GHOST",()=>Question("ST21_JAILED_GHOST","DLG_QT_2101",()=>ExchangeKnot(knot)));
        }
        public bool DeliverHerb(string id)
        {
            if(NearNpc!="Tirso"||Array.IndexOf(Herbs,id)<0||!game.progress.Owns(id)||game.progress.HasFlag("ST03_TIRSO/TIRSO_"+id+"_DELIVERED"))return false;
            game.progress.SetOwned(id,false);game.progress.SetFlag("ST03_TIRSO/TIRSO_"+id+"_DELIVERED");int count=0;foreach(var herb in Herbs)if(game.progress.HasFlag("ST03_TIRSO/TIRSO_"+herb+"_DELIVERED"))count++;
            string reward=null;if(count==1&&!game.progress.Owns("QI66")&&!game.progress.HasFlag("ST03_TIRSO/TIRSO_FIRSTREWARD_DONE")){reward="QI66";game.progress.SetOwned(reward,true);game.progress.SetFlag("ST03_TIRSO/TIRSO_FIRSTREWARD_DONE");}
            else game.progress.tears+=new[]{200,500,1000,2000,5000,10000}[Mathf.Clamp(count-1,0,5)];
            if(count==6&&!game.progress.HasFlag("ST03_TIRSO/TIRSO_LASTREWARD_DONE")){game.progress.SetFlag("ST03_TIRSO/TIRSO_LASTREWARD_DONE");game.progress.SetOwned("QI56",true);reward="QI56";}
            game.events.Raise("QUEST_ITEM_DELIVERED",id);game.SaveGame();game.Sfx("ITEM_ADDED");game.Message(VietnameseSource.Term("ST03_TIRSO/DLG_0310_0","Tirso đã nhận dược liệu"));if(reward!=null)game.itemPopup.Show(InventoryCatalog.Load().Find(reward));return true;
        }
        public bool ExchangeKnot(string id)
        {
            if(NearNpc!="Soledad"||game.progress.rosarySlots>=8||Array.IndexOf(Knots,id)<0||!game.progress.Owns(id)||game.progress.HasFlag("ST21_JAILED_GHOST/USED_"+id))return false;
            game.progress.SetOwned(id,false);game.progress.rosarySlots++;game.progress.SetFlag("ST21_JAILED_GHOST/USED_"+id);if(id=="QI54")game.progress.SetFlag("ST02_REDENTO/REDENTO_QI54_USED");
            if(game.progress.rosarySlots==8){game.progress.SetFlag("ST21_JAILED_GHOST/JAILED_QUEST_COMPLETED");foreach(var actor in game.Current.GetComponentsInChildren<SpriteActor>())if(actor.initialClip=="soledad-ghost-anim")actor.PlayHoldLastFrame("soledad-ghost-vanishing");}
            game.events.Raise("BEAD_SLOT_UPGRADED",id);game.SaveGame();game.Sfx("GUILT_RECOVER");game.Message(VietnameseSource.Term("ST21_JAILED_GHOST/MSG_2101_0","Chuỗi mân côi đã được nâng cấp"));return true;
        }
        static string ActionCaption(string action)
        {
            switch(action){case "Attack":return "Tấn công";case "Interact":return "Tương tác";case "Dash":return "Lướt";case "Parry":return "Đỡ đòn";case "Flask":return "Bình máu";case "Range Attack":return "Kỹ năng";case "Inventory":return "Túi đồ";case "Pause":return "Tạm dừng";case "Move Vertical-":return "Xuống";case "Prayer":return "Niệm kinh";case "Jump":return "Nhảy";default:return action;}
        }
        public void ShowTutorial(string caption,string text){npc="Tutorial";title.text=caption;body.text=System.Text.RegularExpressions.Regex.Replace(text,@"\[ACT:([^\]]+)\]",m=>ActionCaption(m.Groups[1].Value)).Replace("[ICON:PLUS]","+").Replace("[ICON:TEARS]","Nước Mắt");lines.Clear();afterLines=Close;choices.SetActive(false);root.SetActive(true);game.SetEncounterInputBlocked(true);inputClock=.2f;}
        public void Close(){if(root!=null)root.SetActive(false);lines.Clear();afterLines=confirm=null;if(game!=null&&npc!=null){game.SetEncounterInputBlocked(false);npc=null;}}
        void Update(){if(!IsOpen)return;inputClock-=Time.unscaledDeltaTime;if(inputClock>0)return;var key=Keyboard.current;var pad=Gamepad.current;if(key!=null&&key.escapeKey.wasPressedThisFrame||pad!=null&&pad.buttonEast.wasPressedThisFrame){Close();return;}if(!choices.activeSelf&&(key!=null&&(key.enterKey.wasPressedThisFrame||key.spaceKey.wasPressedThisFrame)||pad!=null&&pad.buttonSouth.wasPressedThisFrame))Advance();}
    }
}
