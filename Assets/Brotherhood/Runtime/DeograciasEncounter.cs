using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Brotherhood
{
    /// <summary>
    /// Restores Deogracias' source-authored first encounter in D01Z01S07.
    /// Dialogue ids and order follow the original PlayMaker FSM:
    /// DLG_0101 -> CTS07 -> DLG_0112 -> DLG_0113 -> DLG_0115 -> DLG_0114 -> QI31.
    /// </summary>
    public sealed class DeograciasEncounter:MonoBehaviour
    {
        public const string RoomId="D01Z01S07";
        const float InteractionRange=2.35f;
        const float CutsceneDuration=13.95f;
        const float VideoPrepareTimeout=8f;

        static string[] Introduction()=>new[]
        {
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0101_0","Regretful be the heart, Penitent One. The anguish of the Eldest Brother has now come to an end."),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0101_1","I am Deogracias, witness to and narrator of the acts of the Grievous Miracle. Such is my penance, as yours is silence.")
        };
        static string[] AfterCutscene()=>new[]
        {
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0112_0"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0113_0"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0113_1"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0115_0"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0115_1"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0115_2"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0114_0"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0114_1"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0114_2"),
            VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0114_3")
        };
        static string[] Repeated()=>new[]{VietnameseSource.Term("ST01_DEOSGRACIAS/DLG_0111_0","Sorrowful be the Heart, Penitent One.")};

        BrotherhoodGame game;RoomState room;Transform npc;SpriteActor actor;
        GameObject canvasRoot,dialogRoot,videoRoot,itemRoot;Text speaker,body,subtitle;RawImage videoImage;VideoPlayer videoPlayer;RenderTexture videoTexture;
        string[] lines;int lineIndex;bool active,afterVideo,repeatOnly,itemMessage,safeVideoFallback,finishVideoQueued,fallbackVideoQueued;float inputCooldown,cutsceneClock,prepareClock;

        public bool Active=>active;
        public Canvas Canvas{get;private set;}
        public bool SkipVideoPlaybackForTests{get;set;}
        public bool ForceSafeVideoFallbackForTests{get;set;}
        public bool UsingSafeVideoFallback=>safeVideoFallback;
        public bool NativeVideoGuardActive=>RequiresSafeVideoFallback();
        public bool CanInteract=>game!=null&&room!=null&&npc!=null&&game.Current==room&&!active&&game.IsBossDefeated&&Vector2.Distance(game.player.transform.position,npc.position)<=InteractionRange;

        public void Initialize(BrotherhoodGame owner)
        {
            game=owner;room=owner.Find(RoomId);if(room==null)return;
            foreach(var t in room.GetComponentsInChildren<Transform>(true))if(t.name=="Deosgracias"){npc=t;break;}
            if(npc!=null)
            {
                var visual=npc.GetComponentInChildren<SpriteRenderer>(true);
                if(visual!=null)
                {
                    actor=visual.GetComponent<SpriteActor>();if(actor==null)actor=visual.gameObject.AddComponent<SpriteActor>();
                    var playerActor=owner.player!=null?owner.player.actor:null;actor.catalog=playerActor!=null?playerActor.catalog:null;actor.visual=visual;
                    actor.Play("deosgracias_idle_anim",true,true);
                }
            }
            BuildUI();
        }

        public void OnRoomEntered(RoomState entered)
        {
            if(active&&entered!=room)Close();
            if(entered==room&&actor!=null)actor.Play("deosgracias_idle_anim",true,true);
        }

        public bool TryBegin()
        {
            if(!CanInteract)return false;
            active=true;afterVideo=false;itemMessage=false;repeatOnly=game.progress.deograciasMet;
            game.SetEncounterInputBlocked(true);game.BeginCinematicCamera(npc,5.625f);
            ShowLines(repeatOnly?Repeated():Introduction());return true;
        }

        void Update()
        {
            if(!active)return;
            if(fallbackVideoQueued){fallbackVideoQueued=false;StartSafeVideoFallback();}
            if(finishVideoQueued){finishVideoQueued=false;FinishVideo();}
            if(!active)return;
            inputCooldown-=Time.unscaledDeltaTime;
            if(videoRoot.activeSelf)
            {
                ApplyVoiceVolume();
                if(safeVideoFallback)
                {
                    cutsceneClock+=Time.unscaledDeltaTime;
                    if(cutsceneClock>=CutsceneDuration){FinishVideo();return;}
                }
                else if(videoPlayer!=null&&!videoPlayer.isPrepared)
                {
                    prepareClock+=Time.unscaledDeltaTime;
                    if(prepareClock>=VideoPrepareTimeout){StartSafeVideoFallback();}
                }
                double time=safeVideoFallback?cutsceneClock:(videoPlayer!=null?videoPlayer.time:0);
                if(time>=2&&time<=6.5)ShowSubtitle(VietnameseSource.Term("CUTSCENE/CTS07-Deosgracias_0"));
                else if(time>=7&&time<=11.5)ShowSubtitle(VietnameseSource.Term("CUTSCENE/CTS07-Deosgracias_1"));
                else ShowSubtitle("");
            }
            var keyboard=UnityEngine.InputSystem.Keyboard.current;var pad=UnityEngine.InputSystem.Gamepad.current;
            if(inputCooldown<=0&&((keyboard!=null&&(keyboard.eKey.wasPressedThisFrame||keyboard.spaceKey.wasPressedThisFrame))||(pad!=null&&pad.buttonSouth.wasPressedThisFrame)))Advance();
        }

        public void Advance()
        {
            if(!active||inputCooldown>0)return;inputCooldown=.16f;
            if(videoRoot.activeSelf){FinishVideo();return;}
            if(itemMessage){Close();return;}
            lineIndex++;
            if(lineIndex<lines.Length){body.text=lines[lineIndex];return;}
            if(repeatOnly){Close();return;}
            if(!afterVideo){BeginVideo();return;}
            GrantThorn();
        }

        void ShowLines(string[] sourceLines)
        {
            lines=sourceLines;lineIndex=0;canvasRoot.SetActive(true);dialogRoot.SetActive(true);videoRoot.SetActive(false);itemRoot.SetActive(false);
            speaker.text="DEOGRACIAS";body.text=lines[0];inputCooldown=.18f;
        }

        void BeginVideo()
        {
            dialogRoot.SetActive(false);videoRoot.SetActive(true);ShowSubtitle("");safeVideoFallback=false;finishVideoQueued=false;fallbackVideoQueued=false;cutsceneClock=0;prepareClock=0;
            if(SkipVideoPlaybackForTests){FinishVideo();return;}
            // Unity's Windows Media Foundation backend can crash natively while registering
            // a video texture on DX12. This cannot be caught from managed code, so never ask
            // that backend to open CTS07 in the affected Editor configuration.
            if(ForceSafeVideoFallbackForTests||RequiresSafeVideoFallback()){StartSafeVideoFallback();return;}
            var clip=Resources.Load<VideoClip>("Deogracias/CTS07");
            if(clip==null){StartSafeVideoFallback();return;}
            EnsureVideoPlayer();videoImage.enabled=true;
            videoPlayer.clip=clip;videoPlayer.Prepare();
        }

        static bool RequiresSafeVideoFallback()
        {
            return Application.platform==RuntimePlatform.WindowsEditor&&SystemInfo.graphicsDeviceType==GraphicsDeviceType.Direct3D12;
        }

        void StartSafeVideoFallback()
        {
            if(videoPlayer!=null){videoPlayer.Stop();videoPlayer.clip=null;}
            safeVideoFallback=true;finishVideoQueued=false;fallbackVideoQueued=false;cutsceneClock=0;prepareClock=0;
            if(videoImage!=null)videoImage.enabled=false;
            if(videoRoot!=null)videoRoot.SetActive(true);
            ShowSubtitle("");
        }

        void EnsureVideoPlayer()
        {
            if(videoPlayer!=null)return;
            videoTexture=new RenderTexture(1280,720,0,RenderTextureFormat.ARGB32){name="CTS07 runtime target",filterMode=FilterMode.Point};
            videoImage.texture=videoTexture;
            videoPlayer=videoRoot.AddComponent<VideoPlayer>();videoPlayer.playOnAwake=false;videoPlayer.waitForFirstFrame=true;videoPlayer.skipOnDrop=true;
            videoPlayer.renderMode=VideoRenderMode.RenderTexture;videoPlayer.targetTexture=videoTexture;videoPlayer.audioOutputMode=VideoAudioOutputMode.Direct;videoPlayer.isLooping=false;
            videoPlayer.prepareCompleted+=OnVideoPrepared;videoPlayer.loopPointReached+=OnVideoFinished;videoPlayer.errorReceived+=OnVideoError;
        }

        void ApplyVoiceVolume()
        {
            if(videoPlayer==null||!videoPlayer.isPrepared)return;
            for(ushort track=0;track<videoPlayer.audioTrackCount;track++)videoPlayer.SetDirectAudioVolume(track,Mathf.Clamp01(PlayerPrefs.GetFloat("BrotherhoodVoiceoverVol",1)));
        }
        void OnVideoPrepared(VideoPlayer prepared){if(active&&videoRoot.activeSelf&&!safeVideoFallback){ApplyVoiceVolume();prepared.Play();}}
        void OnVideoFinished(VideoPlayer _){finishVideoQueued=true;}
        void OnVideoError(VideoPlayer _,string message){Debug.LogWarning("CTS07 video playback failed; continuing with the source-subtitle fallback. "+message,this);fallbackVideoQueued=true;}

        void FinishVideo()
        {
            safeVideoFallback=false;finishVideoQueued=false;fallbackVideoQueued=false;cutsceneClock=0;prepareClock=0;
            if(videoPlayer!=null){videoPlayer.Stop();videoPlayer.clip=null;}
            if(videoImage!=null)videoImage.enabled=true;
            videoRoot.SetActive(false);afterVideo=true;ShowLines(AfterCutscene());
        }

        void GrantThorn()
        {
            game.progress.deograciasMet=true;game.progress.thornGranted=true;game.progress.SetOwned("QI31",true);game.SaveGame();
            dialogRoot.SetActive(false);videoRoot.SetActive(false);itemRoot.SetActive(true);itemMessage=true;
            inputCooldown=.2f;game.Sfx("ITEM_ADDED");
        }

        void Close()
        {
            active=false;afterVideo=false;repeatOnly=false;itemMessage=false;safeVideoFallback=false;finishVideoQueued=false;fallbackVideoQueued=false;
            if(videoPlayer!=null){videoPlayer.Stop();videoPlayer.clip=null;}
            if(canvasRoot!=null)canvasRoot.SetActive(false);if(game!=null){game.EndCinematicCamera();game.SetEncounterInputBlocked(false);}
        }

        void BuildUI()
        {
            var canvasObject=new GameObject("Deogracias source encounter",typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));canvasObject.transform.SetParent(transform,false);
            var canvas=canvasObject.GetComponent<Canvas>();Canvas=canvas;canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=60;canvas.pixelPerfect=true;
            var scaler=canvasObject.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;scaler.referenceResolution=new Vector2(1280,720);scaler.matchWidthOrHeight=1;
            canvasRoot=StretchPanel(canvasObject.transform,"Encounter root",new Color(0,0,0,0));

            videoRoot=StretchPanel(canvasRoot.transform,"CTS07-Deosgracias",Color.black);
            videoImage=new GameObject("Original CTS07 video",typeof(RectTransform),typeof(RawImage),typeof(AspectRatioFitter)).GetComponent<RawImage>();videoImage.transform.SetParent(videoRoot.transform,false);Stretch(videoImage.rectTransform);
            var fitter=videoImage.GetComponent<AspectRatioFitter>();fitter.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fitter.aspectRatio=16f/9f;
            subtitle=MakeText(videoRoot.transform,"CTS07 source subtitles",24,TextAnchor.MiddleCenter,Color.white);var subtitleRect=subtitle.rectTransform;subtitleRect.anchorMin=new Vector2(.12f,.04f);subtitleRect.anchorMax=new Vector2(.88f,.18f);subtitleRect.offsetMin=subtitleRect.offsetMax=Vector2.zero;

            dialogRoot=DialogPanel(canvasRoot.transform,"Source dialogue");
            speaker=MakeText(dialogRoot.transform,"Speaker",22,TextAnchor.UpperLeft,new Color(.94f,.69f,.33f));speaker.rectTransform.anchorMin=new Vector2(.12f,.67f);speaker.rectTransform.anchorMax=new Vector2(.88f,.9f);speaker.rectTransform.offsetMin=speaker.rectTransform.offsetMax=Vector2.zero;
            body=MakeText(dialogRoot.transform,"Localized source line",24,TextAnchor.MiddleLeft,new Color(.93f,.89f,.78f));body.rectTransform.anchorMin=new Vector2(.12f,.16f);body.rectTransform.anchorMax=new Vector2(.88f,.68f);body.rectTransform.offsetMin=body.rectTransform.offsetMax=Vector2.zero;

            itemRoot=DialogPanel(canvasRoot.transform,"QI31 source reward");
            var iconObject=new GameObject("QI31 Thorn icon",typeof(RectTransform),typeof(Image));iconObject.transform.SetParent(itemRoot.transform,false);var icon=iconObject.GetComponent<Image>();icon.preserveAspect=true;icon.raycastTarget=false;icon.sprite=LoadTextureSprite("Inventory/Icons/QI31");var iconRect=icon.rectTransform;iconRect.anchorMin=new Vector2(.12f,.2f);iconRect.anchorMax=new Vector2(.28f,.8f);iconRect.offsetMin=iconRect.offsetMax=Vector2.zero;
            var itemTitle=MakeText(itemRoot.transform,"QI31 title",VietnameseSource.Term("QuestItem/QI31_CAPTION","THORN"),25,TextAnchor.UpperLeft,new Color(.94f,.69f,.33f));itemTitle.rectTransform.anchorMin=new Vector2(.31f,.57f);itemTitle.rectTransform.anchorMax=new Vector2(.88f,.83f);itemTitle.rectTransform.offsetMin=itemTitle.rectTransform.offsetMax=Vector2.zero;
            var itemDescription=MakeText(itemRoot.transform,"QI31 description",VietnameseSource.Term("QuestItem/QI31_DESCRIPTION","Small gift from Deogracias, nailed into the effigy of the Twisted under the guard of your sword. The thorns arisen from the Miracle feed on sin and guilt, growing with the burden that its bearer carries."),20,TextAnchor.UpperLeft,new Color(.93f,.89f,.78f));itemDescription.rectTransform.anchorMin=new Vector2(.31f,.18f);itemDescription.rectTransform.anchorMax=new Vector2(.88f,.59f);itemDescription.rectTransform.offsetMin=itemDescription.rectTransform.offsetMax=Vector2.zero;

            var advanceObject=StretchPanel(canvasRoot.transform,"Advance source dialogue",new Color(0,0,0,0));var button=advanceObject.AddComponent<Button>();button.transition=Selectable.Transition.None;button.onClick.AddListener(Advance);
            canvasRoot.SetActive(false);
        }

        GameObject DialogPanel(Transform parent,string name)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);var rect=go.GetComponent<RectTransform>();rect.anchorMin=new Vector2(.05f,.02f);rect.anchorMax=new Vector2(.95f,.39f);rect.offsetMin=rect.offsetMax=Vector2.zero;
            var image=go.GetComponent<Image>();image.sprite=Resources.Load<Sprite>("Dialogue/dialog_background");image.type=Image.Type.Simple;image.color=Color.white;image.raycastTarget=false;return go;
        }

        GameObject StretchPanel(Transform parent,string name,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Image));go.transform.SetParent(parent,false);Stretch(go.GetComponent<RectTransform>());var image=go.GetComponent<Image>();image.color=color;return go;
        }

        static void Stretch(RectTransform rect){rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;}

        Text MakeText(Transform parent,string name,int size,TextAnchor alignment,Color color)=>MakeText(parent,name,"",size,alignment,color);
        Text MakeText(Transform parent,string name,string value,int size,TextAnchor alignment,Color color)
        {
            var go=new GameObject(name,typeof(RectTransform),typeof(Text));go.transform.SetParent(parent,false);var text=go.GetComponent<Text>();text.font=Resources.Load<Font>("Fonts/Caudex-Bold");if(text.font==null)text.font=Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");text.text=value;text.fontSize=size;text.alignment=alignment;text.color=color;text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Overflow;text.raycastTarget=false;return text;
        }

        Sprite LoadTextureSprite(string path)
        {
            var sprite=Resources.Load<Sprite>(path);if(sprite!=null)return sprite;var texture=Resources.Load<Texture2D>(path);return texture==null?null:Sprite.Create(texture,new Rect(0,0,texture.width,texture.height),new Vector2(.5f,.5f),100);
        }

        void ShowSubtitle(string value){if(subtitle!=null)subtitle.text=value;}
        void OnDestroy()
        {
            if(videoPlayer!=null){videoPlayer.prepareCompleted-=OnVideoPrepared;videoPlayer.loopPointReached-=OnVideoFinished;videoPlayer.errorReceived-=OnVideoError;videoPlayer.Stop();}
            if(videoTexture!=null){videoTexture.Release();Destroy(videoTexture);}
        }
    }
}
