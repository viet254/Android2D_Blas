using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.Video;
namespace Brotherhood
{
    public sealed class SourceBloodyBaptism:MonoBehaviour
    {
        [Serializable] class Preview{public int fps,frames,columns,rows;public float duration;}
        BrotherhoodGame game;GameObject root;RawImage image;VideoPlayer video;AudioSource foley;RenderTexture texture;Preview preview;Texture2D atlas;bool ended,error;int loadedAtlas=-1;
        public bool IsPlaying{get;private set;}
        public bool SafePreview{get;private set;}
        public Canvas Canvas{get;private set;}
        public bool SkipForVerification;
        public void Initialize(BrotherhoodGame owner){game=owner;}
        public IEnumerator Play()
        {
            if(game.progress.HasFlag("CUTSCENE/CTS02_COMPLETED"))yield break;
            if(!SkipForVerification)
            {
                BuildUI();IsPlaying=true;root.SetActive(true);bool audioStarted=false;if(game.audioBank!=null)game.audioBank.SetCinematicAudio(true);
                SafePreview=Application.platform==RuntimePlatform.WindowsEditor&&SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Direct3D12;
                var clip=Resources.Load<VideoClip>("Cutscenes/CTS02");var data=Resources.Load<TextAsset>("Cutscenes/CTS02-preview");preview=data!=null?JsonUtility.FromJson<Preview>(data.text):new Preview{duration=23.45f};
                ended=error=false;float clock=0,prepare=0;
                if(!SafePreview&&clip!=null){video.clip=clip;video.Prepare();}else SafePreview=true;
                while(!ended&&clock<preview.duration+.5f)
                {
                    if(!IsPlaying||game.Current==null||game.Current.id!="D17Z01S11"){Finish();yield break;}
                    var key=Keyboard.current;var pad=Gamepad.current;
                    bool cancel=clock>.5f&&(key!=null&&(key.escapeKey.wasPressedThisFrame||key.enterKey.wasPressedThisFrame)||pad!=null&&pad.buttonSouth.wasPressedThisFrame||Pointer.current!=null&&Pointer.current.press.wasPressedThisFrame);
                    if(cancel)break;
                    if(!SafePreview)
                    {
                        if(error||!video.isPrepared&&(prepare+=Time.unscaledDeltaTime)>=8){video.Stop();SafePreview=true;clock=0;}
                        else if(video.isPrepared){if(!video.isPlaying)video.Play();clock=(float)video.time;image.texture=texture;image.uvRect=new Rect(0,0,1,1);if(!audioStarted){foley.Play();audioStarted=true;}}
                    }
                    if(SafePreview){ShowFrame(clock);clock+=Time.unscaledDeltaTime;if(!audioStarted){foley.Play();audioStarted=true;}}
                    foley.volume=PlayerPrefs.GetFloat("BrotherhoodSfxVol",1);foley.mute=game.audioBank!=null&&game.audioBank.SfxMuted;
                    yield return null;
                }
                Finish();
            }
            Complete();
        }
        public void Complete()
        {
            game.progress.SetFlag("CUTSCENE/CTS02_COMPLETED");game.progress.guiltDrops.Clear();if(game.guilt!=null)game.guilt.RefreshRoom();game.SaveGame();
        }
        public void Cancel(){Finish();}
        void ShowFrame(float time)
        {
            if(preview.frames<=0)return;int frame=Mathf.Clamp((int)(time*preview.fps),0,preview.frames-1),perAtlas=preview.columns*preview.rows,index=frame/perAtlas;
            if(loadedAtlas!=index){if(atlas!=null)Resources.UnloadAsset(atlas);atlas=Resources.Load<Texture2D>("Cutscenes/CTS02Preview_"+index.ToString("00"));loadedAtlas=index;image.texture=atlas;}
            int local=frame%perAtlas;image.uvRect=new Rect((local%preview.columns)/(float)preview.columns,1-(local/preview.columns+1)/(float)preview.rows,1f/preview.columns,1f/preview.rows);
        }
        void BuildUI()
        {
            if(root!=null)return;root=new GameObject("CTS02 Bloody Baptism",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));root.transform.SetParent(transform,false);var canvas=root.GetComponent<Canvas>();Canvas=canvas;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);
            var black=new GameObject("Video background",typeof(RectTransform),typeof(Image));black.transform.SetParent(root.transform,false);Stretch(black.GetComponent<RectTransform>());black.GetComponent<Image>().color=Color.black;
            var panel=new GameObject("Source CTS02 frame",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter));panel.transform.SetParent(root.transform,false);image=panel.GetComponent<RawImage>();Stretch(image.rectTransform);var fit=panel.GetComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=16f/9f;
            video=panel.AddComponent<VideoPlayer>();video.playOnAwake=false;video.waitForFirstFrame=true;video.skipOnDrop=true;video.isLooping=false;video.audioOutputMode=VideoAudioOutputMode.None;video.renderMode=VideoRenderMode.RenderTexture;texture=new RenderTexture(1280,720,0);video.targetTexture=texture;video.loopPointReached+=_=>ended=true;video.errorReceived+=(_,message)=>error=true;
            foley=panel.AddComponent<AudioSource>();foley.playOnAwake=false;foley.clip=Resources.Load<AudioClip>("Audio/CTS02_FOLEY");
        }
        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}
        void Finish(){if(video!=null){video.Stop();video.clip=null;}if(foley!=null)foley.Stop();if(root!=null)root.SetActive(false);if(atlas!=null)Resources.UnloadAsset(atlas);atlas=null;loadedAtlas=-1;IsPlaying=false;if(game!=null&&game.audioBank!=null)game.audioBank.SetCinematicAudio(false);}
        void OnDestroy(){Finish();if(texture!=null){texture.Release();Destroy(texture);}}
    }
}
