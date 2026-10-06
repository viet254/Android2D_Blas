using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Brotherhood
{
    // The original PopUpWidget shows the item-acquired label, name and image.
    // The item's source-localized description is included so the player can inspect it.
    public sealed class SourceItemPopup : MonoBehaviour
    {
        BrotherhoodGame game;
        GameObject root;
        Image itemImage;
        Text nameText, descriptionText;
        Font font;
        public Canvas Canvas { get; private set; }
        public bool IsOpen => root != null && root.activeInHierarchy;
        public string ShownItem { get; private set; }
        public Sprite DisplayedIcon => itemImage == null ? null : itemImage.sprite;

        public void Initialize(BrotherhoodGame owner)
        {
            game=owner;
            font=Resources.Load<Font>("Fonts/Caudex-Regular");
            if(font==null)font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvasObject=new GameObject("Original item acquisition",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();Canvas=canvas;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=50;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=.5f;
            root=new GameObject("Item acquired popup",typeof(RectTransform),typeof(Image));root.transform.SetParent(canvasObject.transform,false);
            var full=(RectTransform)root.transform;full.anchorMin=Vector2.zero;full.anchorMax=Vector2.one;full.offsetMin=full.offsetMax=Vector2.zero;
            root.GetComponent<Image>().color=new Color(.02f,.012f,.015f,.82f);
            var panel=Rect(root.transform,"Source item panel",new Vector2(740,400),Vector2.zero);
            panel.gameObject.AddComponent<Image>().color=new Color(.09f,.045f,.05f,.98f);
            var outline=panel.gameObject.AddComponent<Outline>();outline.effectColor=new Color(.61f,.4f,.22f,1);outline.effectDistance=new Vector2(2,-2);
            Label(panel,"UI_Inventory/TEXT_ITEM_FOUND",new Vector2(0,140),new Vector2(680,42),29,new Color(.95f,.75f,.43f),TextAnchor.MiddleCenter).text=VietnameseSource.Term("UI_Inventory/TEXT_ITEM_FOUND","You have acquired:");
            var frame=Rect(panel,"Original item slot",new Vector2(164,164),new Vector2(-245,20));
            var frameImage=frame.gameObject.AddComponent<Image>();frameImage.sprite=SourceSprite("UI/Sprites/Slot_01");frameImage.preserveAspect=true;
            itemImage=Rect(frame,"Item source icon",new Vector2(115,115),Vector2.zero).gameObject.AddComponent<Image>();itemImage.preserveAspect=true;itemImage.raycastTarget=false;
            nameText=Label(panel,"Name",new Vector2(95,65),new Vector2(390,55),25,new Color(1f,.84f,.55f),TextAnchor.MiddleLeft);
            descriptionText=Label(panel,"Description",new Vector2(95,-62),new Vector2(405,175),17,new Color(.9f,.82f,.68f),TextAnchor.UpperLeft);
            descriptionText.horizontalOverflow=HorizontalWrapMode.Wrap;descriptionText.verticalOverflow=VerticalWrapMode.Truncate;
            var button=Rect(panel,"Close",new Vector2(190,52),new Vector2(0,-155));
            var buttonImage=button.gameObject.AddComponent<Image>();buttonImage.sprite=SourceSprite("UI/Sprites/Boton_04");buttonImage.preserveAspect=false;
            button.gameObject.AddComponent<Button>().onClick.AddListener(Hide);
            Label(button,"Close label",Vector2.zero,new Vector2(170,40),21,new Color(1f,.85f,.6f),TextAnchor.MiddleCenter).text=VietnameseSource.Display("CLOSE");
            root.SetActive(false);
        }

        public void Show(InventoryCatalog.Item item)
        {
            if(root==null||item==null)return;
            ShownItem=item.id;
            nameText.text=item.caption;
            descriptionText.text=item.description;
            itemImage.sprite=SourceSprite(item.icon);
            itemImage.enabled=itemImage.sprite!=null;
            root.SetActive(true);
            Canvas.ForceUpdateCanvases();
            game.SetItemPopupActive(true);
        }

        public void Hide()
        {
            if(!IsOpen)return;
            root.SetActive(false);
            game.SetItemPopupActive(false);
        }

        void Update()
        {
            if(!IsOpen)return;
            var key=Keyboard.current;var pad=Gamepad.current;
            if((key!=null&&(key.escapeKey.wasPressedThisFrame||key.enterKey.wasPressedThisFrame||key.spaceKey.wasPressedThisFrame))||
               (pad!=null&&(pad.buttonSouth.wasPressedThisFrame||pad.buttonEast.wasPressedThisFrame)))Hide();
        }

        static RectTransform Rect(Transform parent,string name,Vector2 size,Vector2 position)
        {
            var go=new GameObject(name,typeof(RectTransform));var rt=(RectTransform)go.transform;
            rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.sizeDelta=size;rt.anchoredPosition=position;return rt;
        }

        Text Label(Transform parent,string name,Vector2 position,Vector2 size,int fontSize,Color color,TextAnchor alignment)
        {
            var label=Rect(parent,name,size,position).gameObject.AddComponent<Text>();
            label.font=font;label.fontSize=fontSize;label.color=color;label.alignment=alignment;label.raycastTarget=false;
            return label;
        }

        static Sprite SourceSprite(string path)
        {
            if(string.IsNullOrEmpty(path))return null;
            var texture=Resources.Load<Texture2D>(path);if(texture==null)return null;
            texture.filterMode=FilterMode.Point;
            return Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
        }
    }
}
