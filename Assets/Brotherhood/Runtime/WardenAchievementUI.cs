using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Brotherhood
{
    public sealed class WardenAchievementUI:MonoBehaviour
    {
        GameObject root;CanvasGroup group;Coroutine showing;

        public void Initialize()
        {
            var canvasObject=new GameObject("AC01 Achievement Canvas",typeof(Canvas),typeof(CanvasScaler));canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=35;canvas.pixelPerfect=true;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
            root=new GameObject("AC01 - A Long Path Ahead",typeof(RectTransform),typeof(Image),typeof(CanvasGroup));root.transform.SetParent(canvasObject.transform,false);
            var rect=root.GetComponent<RectTransform>();rect.anchorMin=rect.anchorMax=new Vector2(.5f,1);rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-36);rect.sizeDelta=new Vector2(726,114);
            var background=root.GetComponent<Image>();background.sprite=Resources.Load<Sprite>("Achievements/achievements-bg-unlocked");background.type=UnityEngine.UI.Image.Type.Simple;background.color=Color.white;background.raycastTarget=false;
            group=root.GetComponent<CanvasGroup>();group.interactable=group.blocksRaycasts=false;
            var icon=Image(root.transform,"AC01 source icon",new Vector2(24,-17),new Vector2(80,80));icon.sprite=Resources.Load<Sprite>("Achievements/achievements-AC01");icon.preserveAspect=true;
            var font=Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");
            Text(root.transform,"Achievement name","A LONG PATH AHEAD",font,new Vector2(122,-17),new Vector2(560,38),24,new Color(.93f,.74f,.35f));
            Text(root.transform,"Achievement description","Defeat the Warden of Silent Sorrow.",font,new Vector2(122,-55),new Vector2(560,34),18,new Color(.88f,.82f,.69f));
            root.SetActive(false);
        }

        Image Image(Transform parent,string name,Vector2 position,Vector2 size)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;
            var image=go.GetComponent<Image>();image.raycastTarget=false;return image;
        }

        void Text(Transform parent,string name,string value,Font font,Vector2 position,Vector2 size,int fontSize,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));var rect=go.GetComponent<RectTransform>();rect.SetParent(parent,false);
            rect.anchorMin=rect.anchorMax=new Vector2(0,1);rect.pivot=new Vector2(0,1);rect.anchoredPosition=position;rect.sizeDelta=size;
            var text=go.GetComponent<Text>();text.text=value;text.font=font;text.fontSize=fontSize;text.color=color;text.alignment=TextAnchor.MiddleLeft;text.horizontalOverflow=HorizontalWrapMode.Overflow;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;
        }

        public void Show()
        {
            if(root==null)return;if(showing!=null)StopCoroutine(showing);showing=StartCoroutine(Display());
        }

        IEnumerator Display()
        {
            root.SetActive(true);group.alpha=0;
            for(float t=0;t<.25f;t+=Time.unscaledDeltaTime){group.alpha=Mathf.Clamp01(t/.25f);yield return null;}
            group.alpha=1;yield return new WaitForSecondsRealtime(4f);
            for(float t=0;t<.35f;t+=Time.unscaledDeltaTime){group.alpha=1-Mathf.Clamp01(t/.35f);yield return null;}
            root.SetActive(false);showing=null;
        }
    }
}
