using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace Brotherhood
{
    public sealed class BlasMapUI : MonoBehaviour
    {
        public BrotherhoodGame game;
        public TouchControls controls;
        public BlasOptionsUI optionsUI;

        private GameObject root;
        private RectTransform mapContent;
        private RectTransform mapViewport, safeAreaRect;
        private Rect lastSafeArea;
        private Vector2 lastScreenSize;
        private Text percentText;
        private Text cherubText;
        private Text zoneTitleText;
        private Text zoneSubText;
        private Text zoomText, pinHintText;

        private RectTransform playerMarker;
        private readonly Dictionary<string, Vector2> roomPositions = new Dictionary<string, Vector2>();
        private readonly Dictionary<string, Image> roomImages = new Dictionary<string, Image>();
        private bool hasMapSnapshot, hasPinSnapshot;
        private BrotherhoodGame displayedGame;
        private PlayerController displayedPlayer;
        private PlayerProgress displayedProgress, displayedPinProgress;
        private string displayedRoomId, displayedCellKey;
        private string[] displayedDiscovery = Array.Empty<string>();
        private PlayerProgress.MapPinSaveData[] displayedPins = Array.Empty<PlayerProgress.MapPinSaveData>();
        private float displayedPercentage;
        private int displayedCherubs;
        private readonly List<GameObject> pinObjects = new List<GameObject>();
        private readonly Image[] pinSelectorImages = new Image[8];
        private int selectedPinType = -1; // -1 means none selected

        private int zoomLevel = 3;
        private float viewZoom = 2f;
        private static readonly float[] ZoomScales = { .75f, 1f, 1.5f, 2f, 3f };
        private bool draggingMap, pinchingMap;
        private float suppressPinUntil, previousPinchDistance;
        private Vector2 previousPinchCenter;
        private int pinchFingerA=-1,pinchFingerB=-1;
        private const float CellSize = 42f;
        private static readonly Color WallColor = new Color(1f, .9f, .66f, 1f);
        private static readonly Color DoorColor = new Color(1f, .7f, .24f, 1f);
        private static readonly Color LocationColor = new Color(.3f, .95f, 1f, 1f);
        private static readonly string[] PinLabels = { "Thiên thần", "NPC", "Dấu xanh dương", "Rương", "Kẻ địch", "Dấu xanh lá", "Cần khám phá", "Dấu đỏ" };

        private Font titleFont;
        private Font bodyFont;
        private readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

        public static readonly string[] MarkerNames = new string[]
        {
            "map-marker-cherub", "map-marker-npc", "map-marker-blue", "map-marker-chest",
            "map-marker-enemy", "map-marker-green", "map-marker-question", "map-marker-red"
        };

        public bool IsOpen => root != null && root.activeSelf;
        public Canvas Canvas { get; private set; }

        void Awake()
        {
            LoadFonts();
        }

        void Update()
        {
            if (!IsOpen) return;
            UpdateSafeArea();
            UpdatePinch();
            if (MapStateChanged()) RefreshMap();
        }

        private void LoadFonts()
        {
            titleFont = Resources.Load<Font>("Fonts/Caudex-Bold");
            bodyFont  = Resources.Load<Font>("Fonts/Caudex-Regular");
            if (titleFont == null) titleFont = Resources.Load<Font>("Fonts/MajesticExtended_Pixel_Scroll");
            if (bodyFont  == null) bodyFont  = titleFont;
            if (titleFont == null) titleFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (bodyFont  == null) bodyFont  = titleFont;
        }

        public void Initialize(BrotherhoodGame g, TouchControls c, BlasOptionsUI opt)
        {
            game = g;
            controls = c;
            optionsUI = opt;
            BuildUI();
        }

        public void Show()
        {
            if (root == null) BuildUI();
            root.SetActive(true);
            controls?.SetControlsVisible(false);
            Time.timeScale = 0f;
            draggingMap = pinchingMap = false;
            RefreshMap();
            Recenter();
            game?.Sfx("INVENTORY_OPEN");
        }

        public void Hide()
        {
            if (root != null) root.SetActive(false);
            draggingMap = pinchingMap = false;
            Time.timeScale = 1f;
            controls?.SetControlsVisible(true);
            game?.Sfx("INVENTORY_CLOSE");
        }

        public void Toggle()
        {
            if (IsOpen) Hide();
            else Show();
        }

        private void BuildUI()
        {
            if (root != null) return;

            var canvasObj = new GameObject("BlasMapCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);
            Canvas = canvasObj.GetComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.overrideSorting = true;
            Canvas.sortingLayerName = "Canvas UI";
            Canvas.sortingOrder = 100;

            var scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1f;

            // Fullscreen root
            root = MakePanel(canvasObj.transform, "MapRoot", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.black);

            // FondoMapa background
            var bgObj = MakePanel(root.transform, "FondoMapa", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, Color.white);
            var bgImg = bgObj.GetComponent<Image>();
            bgImg.sprite = LoadSprite("UI/FondoMapa");
            bgImg.type = Image.Type.Simple;
            bgImg.color = new Color(.4f, .4f, .4f, 1f);

            var safeObj = new GameObject("MapSafeArea", typeof(RectTransform));
            safeObj.transform.SetParent(root.transform, false);
            safeAreaRect = safeObj.GetComponent<RectTransform>();
            safeAreaRect.anchorMin = Vector2.zero; safeAreaRect.anchorMax = Vector2.one;
            safeAreaRect.offsetMin = safeAreaRect.offsetMax = Vector2.zero;
            UpdateSafeArea();

            // Dark map viewport area
            var viewport = MakePanel(safeAreaRect, "MapViewport", Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, new Color(.018f, .025f, .04f, .92f));
            mapViewport = viewport.GetComponent<RectTransform>();
            mapViewport.offsetMin = new Vector2(20, 145);
            mapViewport.offsetMax = new Vector2(-112, -98);
            viewport.AddComponent<RectMask2D>();
            viewport.AddComponent<BlasMapViewportInput>().map = this;

            // Draggable Content root
            var contentObj = new GameObject("MapContent", typeof(RectTransform));
            contentObj.transform.SetParent(viewport.transform, false);
            mapContent = contentObj.GetComponent<RectTransform>();
            mapContent.anchorMin = mapContent.anchorMax = new Vector2(0.5f, 0.5f);
            mapContent.sizeDelta = new Vector2(3000, 2000);
            mapContent.anchoredPosition = Vector2.zero;
            mapContent.localScale = Vector3.one * viewZoom;

            // Build Room Cells
            BuildRoomGrid();

            // Standalone Map Header Panel (Top of map, flush with top)
            var header = MakePanel(safeAreaRect, "Header", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -38f), new Vector2(0, 76f), new Color(.035f, .025f, .035f, .98f));
            var headerOutline = header.AddComponent<Outline>();
            headerOutline.effectColor = new Color(0.7f, 0.5f, 0.2f, 0.8f);
            headerOutline.effectDistance = new Vector2(0, -2);

            // Authentic [X] Close Button (Top-Right inside Header)
            var closeBtn = MakeButton(header.transform, "CloseBtn", new Vector2(1, 0.5f), new Vector2(-54f, 0f), new Vector2(82f, 60f), Hide);
            var cbImg = closeBtn.GetComponent<Image>();
            cbImg.sprite = LoadSprite("UI/Sprites/X");
            cbImg.color = Color.white;
            cbImg.preserveAspect = true;

            // Discovery %
            var discIcon = MakePanel(header.transform, "DiscIcon", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(36, 0), new Vector2(32, 32), Color.white).GetComponent<Image>();
            discIcon.sprite = LoadSprite("UI/Sprites/map-discovery-icon");
            discIcon.preserveAspect = true;
            percentText = MakeLabel(header.transform, "0.00%", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(110, 0), new Vector2(108, 36), 26, TextAnchor.MiddleLeft, new Color(1f, .88f, .45f));

            // Cherubs Count
            var cherubIcon = MakePanel(header.transform, "CherubIcon", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(196, 0), new Vector2(30, 38), Color.white).GetComponent<Image>();
            cherubIcon.sprite = LoadSprite("UI/Sprites/map-marker-cherub");
            cherubIcon.preserveAspect = true;
            cherubText = MakeLabel(header.transform, "0 / 38", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(272, 0), new Vector2(110, 36), 24, TextAnchor.MiddleLeft, new Color(.95f, .85f, .65f));

            // Zone Title (Center)
            zoneTitleText = MakeLabel(header.transform, "BẢN ĐỒ", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 15), new Vector2(580, 32), 26, TextAnchor.MiddleCenter, new Color(1f, .85f, .4f));
            zoneSubText = MakeLabel(header.transform, "Đã khám phá 0 / 0 ô bản đồ", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, -16), new Vector2(560, 24), 18, TextAnchor.MiddleCenter, new Color(.84f, .81f, .75f));

            // Options Gear Button (Standalone at Top-Right under Header, aligned with zoom controls)
            MakeMapControl(safeAreaRect, "OptionsBtn", new Vector2(1, 1), new Vector2(-54, -124), "Boton_Config", OpenOptions);

            // Right-Side Controls (+, -, Recenter) anchored to bottom-right, vertically spaced per authentic reference
            var controlsPanel = MakePanel(safeAreaRect, "SideControls", new Vector2(1, .5f), new Vector2(1, .5f), new Vector2(-54, -32), new Vector2(88, 340), Color.clear);
            var cpRt = controlsPanel.GetComponent<RectTransform>();
            cpRt.pivot = new Vector2(.5f, .5f);
            controlsPanel.GetComponent<Image>().raycastTarget = false;

            // Recenter — uses Boton_Centrar.png (authentic y ~ 155 in 1080p)
            MakeMapControl(controlsPanel.transform, "Recenter", new Vector2(.5f, .5f), new Vector2(0, -112), "Boton_Centrar", Recenter);

            // Zoom Out (-) — uses Boton_Menos.png (authentic y ~ 295 in 1080p)
            MakeMapControl(controlsPanel.transform, "ZoomOut", new Vector2(.5f, .5f), Vector2.zero, "Boton_Menos", () => ChangeZoom(-1));

            // Zoom In (+) — uses Boton_Mas.png (authentic y ~ 445 in 1080p)
            MakeMapControl(controlsPanel.transform, "ZoomIn", new Vector2(.5f, .5f), new Vector2(0, 112), "Boton_Mas", () => ChangeZoom(1));
            zoomText = MakeLabel(controlsPanel.transform, "2×", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(0, 57), new Vector2(86, 24), 20, TextAnchor.MiddleCenter, new Color(.94f, .84f, .6f));

            // Bottom 8 Pin Bar — uses Canvas_08.png pill background (authentic y ~ 80 in 1080p)
            var pinBar = MakePanel(safeAreaRect, "PinBar", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 58), new Vector2(680, 72), Color.white);
            var pbRt = pinBar.GetComponent<RectTransform>();
            pbRt.pivot = new Vector2(0.5f, 0.5f);
            var pinBarImg = pinBar.GetComponent<Image>();
            var pillSp = LoadSprite("UI/Sprites/Canvas_08");
            if (pillSp != null) { pinBarImg.sprite = pillSp; pinBarImg.type = Image.Type.Simple; pinBarImg.preserveAspect = false; pinBarImg.color = new Color(1f, 1f, 1f, 0.95f); }
            else { pinBarImg.color = new Color(0.04f, 0.03f, 0.04f, 0.95f); }

            for (int i = 0; i < 8; i++)
            {
                int pinIdx = i;
                float x = -262.5f + i * 75f;
                var pBtn = MakeButton(pinBar.transform, "Pin_" + i, new Vector2(.5f, .5f), new Vector2(x, 0), new Vector2(62, 62), () => SelectPinTool(pinIdx));
                var pImg = pBtn.GetComponent<Image>();
                pImg.color = new Color(0.15f, 0.12f, 0.15f, 0f);
                var pBorder = pBtn.AddComponent<Outline>();
                pBorder.effectColor = new Color(0.9f, 0.7f, 0.3f, 0f);
                pBorder.effectDistance = new Vector2(1, -1);
                pinSelectorImages[i] = pImg;

                var icon = MakePanel(pBtn.transform, "Icon", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 40), Color.white).GetComponent<Image>();
                icon.sprite = LoadSprite("UI/Sprites/" + MarkerNames[i]);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            var legend = MakePanel(safeAreaRect, "MapLegend", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(-36, 116), new Vector2(1050, 28), Color.clear);
            legend.GetComponent<Image>().raycastTarget = false;
            MakeLegend(legend.transform, -445, "map-cursor", "Vị trí của bạn");
            MakeLegend(legend.transform, -215, "map-priedieu", "Điểm lưu");
            MakeLegend(legend.transform, -10, "map-mea-culpa-altar", "Bàn Mea Culpa");
            var dragHint = MakeLabel(legend.transform, "Kéo để xem · +/− để phóng", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(335, 0), new Vector2(380, 24), 17, TextAnchor.MiddleCenter, new Color(.8f, .78f, .73f));
            dragHint.font = VietnameseSource.DynamicFont ?? bodyFont;
            pinHintText = MakeLabel(safeAreaRect, "Chọn một dấu bên trên để đánh dấu ô đã khám phá", new Vector2(.5f, 0), new Vector2(.5f, 0), new Vector2(0, 11), new Vector2(1000, 22), 16, TextAnchor.MiddleCenter, new Color(.8f, .78f, .73f));
            pinHintText.font = VietnameseSource.DynamicFont ?? bodyFont;

            root.SetActive(false);
        }

        private void MakeMapControl(Transform parent, string name, Vector2 anchor, Vector2 position, string sprite, Action action)
        {
            var button = MakeButton(parent, name, anchor, position, new Vector2(80, 80), action);
            button.GetComponent<Image>().color = new Color(.07f, .055f, .05f, .95f);
            var border = button.AddComponent<Outline>(); border.effectColor = new Color(.55f, .39f, .19f, .8f); border.effectDistance = new Vector2(1, -1);
            var icon = MakePanel(button.transform, "Icon", new Vector2(.5f, .5f), new Vector2(.5f, .5f), Vector2.zero, new Vector2(44, 44), Color.white).GetComponent<Image>();
            icon.sprite = LoadSprite("UI/Sprites/" + sprite); icon.preserveAspect = true; icon.raycastTarget = false;
        }

        private void MakeLegend(Transform parent, float x, string sprite, string label)
        {
            var icon = MakePanel(parent, "LegendIcon", new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(x, 0), new Vector2(22, 22), Color.white).GetComponent<Image>();
            icon.sprite = LoadSprite("UI/Sprites/" + sprite); icon.preserveAspect = true; icon.raycastTarget = false;
            var text = MakeLabel(parent, label, new Vector2(.5f, .5f), new Vector2(.5f, .5f), new Vector2(x + 104, 0), new Vector2(170, 24), 18, TextAnchor.MiddleLeft, new Color(.9f, .86f, .77f));
            text.font = VietnameseSource.DynamicFont ?? bodyFont;
        }

        private void BuildRoomGrid()
        {
            roomPositions.Clear();roomImages.Clear();
            var source=SourceMap.Load();
            foreach(var room in game != null && game.rooms != null ? game.rooms : Array.Empty<RoomState>())
            {
                if (room == null) continue;
                Vector2 center=Vector2.zero;int count=0;
                foreach(var cell in source.RoomCells(room.id))
                {
                    if(cell.ngPlus)continue;Vector2 position=cell.MapPosition;center+=position;count++;
                    var box=MakePanel(mapContent,room.id+"/"+cell.key,new Vector2(.5f,.5f),new Vector2(.5f,.5f),position,new Vector2(CellSize,CellSize),ZoneColor(room.id));
                    var image=box.GetComponent<Image>();
                    BuildCellEdges(box.transform, cell);
                    roomImages[cell.key]=image;
                    string iconName = CellIcon(cell.type);
                    if(iconName != null)
                    {
                        var icon=MakePanel(box.transform,"MapPoint_"+cell.type,new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(20,20),Color.white).GetComponent<Image>();
                        icon.sprite=LoadSprite("UI/Sprites/"+iconName);icon.preserveAspect=true;icon.raycastTarget=false;
                        if(icon.sprite==null)icon.sprite=LoadSprite("UI/Sprites/map-marker-npc");
                        var shadow=icon.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(0,0,0,.95f);shadow.effectDistance=new Vector2(1,-1);
                    }
                    var button=box.AddComponent<Button>();string roomId=room.id;string key=cell.key;
                    button.onClick.AddListener(()=>OnClickRoom(roomId,position,key));
                }
                if(count>0)roomPositions[room.id]=center/count;
            }
            var marker=MakePanel(mapContent,"PlayerMarker",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(24,24),Color.white);
            playerMarker=marker.GetComponent<RectTransform>();var cursor=marker.GetComponent<Image>();cursor.sprite=LoadSprite("UI/Sprites/map-cursor");cursor.preserveAspect=true;cursor.raycastTarget=false;
            var markerShadow=marker.AddComponent<Shadow>();markerShadow.effectColor=Color.black;markerShadow.effectDistance=new Vector2(1.5f,-1.5f);
            var frame=MakePanel(marker.transform,"CurrentCellFrame",new Vector2(.5f,.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(CellSize,CellSize),Color.clear);
            frame.GetComponent<Image>().raycastTarget=false;
            foreach(float x in new[]{-1f,1f})foreach(float y in new[]{-1f,1f})
            {
                MakeEdge(frame.transform,"CornerH",new Vector2(x*17,y*20),new Vector2(8,2),LocationColor);
                MakeEdge(frame.transform,"CornerV",new Vector2(x*20,y*17),new Vector2(2,8),LocationColor);
            }
            hasMapSnapshot = hasPinSnapshot = false;
        }

        private static Color ZoneColor(string roomId)
        {
            // Preserve the source Level1 zone hue, with a brighter solid fill
            // because the exported UIMap shaders do not contain their source code.
            Color source = roomId.StartsWith("D17Z01", StringComparison.Ordinal) ? new Color(.31295955f,.30055147f,.375f) :
                roomId.StartsWith("D01Z01", StringComparison.Ordinal) ? new Color(.19357504f,.2647059f,.13429932f) :
                roomId.StartsWith("D01Z02", StringComparison.Ordinal) ? new Color(.25f,.22463237f,.12316176f) : new Color(.22f,.27f,.34f);
            return Color.Lerp(source,Color.white,.2f);
        }

        private static string CellIcon(int type)
        {
            switch(type)
            {
                case 1:return "map-priedieu";
                case 2:return "map-teleport";
                case 3:return "map-mea-culpa-altar";
                case 4:return "map-soledad";
                default:return null;
            }
        }

        private void MakeEdge(Transform parent,string name,Vector2 position,Vector2 size,Color color)
        {
            var edge=MakePanel(parent,name,new Vector2(.5f,.5f),new Vector2(.5f,.5f),position,size,color);
            edge.GetComponent<Image>().raycastTarget=false;
        }

        private void BuildCellEdges(Transform parent,SourceMap.Cell cell)
        {
            // Source side indices: north, south, west, east. Doors take priority
            // when the source marks both a door and a wall on the same side.
            for(int side=0;side<4;side++)
            {
                bool door=cell.doors!=null&&cell.doors.Length>side&&cell.doors[side]!=0;
                bool wall=cell.walls!=null&&cell.walls.Length>side&&cell.walls[side]!=0;
                if(!door&&!wall)continue;
                bool horizontal=side<2;
                Vector2 normal=side==0?Vector2.up:side==1?Vector2.down:side==2?Vector2.left:Vector2.right;
                Vector2 axis=horizontal?Vector2.right:Vector2.up;
                // Keep each edge inside its own cell. Some source neighbors
                // intentionally have a door facing a wall, so their marks must not overlap.
                Vector2 edge=normal*(CellSize*.5f-1f);
                if(!door)MakeEdge(parent,"Wall_"+side,edge,horizontal?new Vector2(CellSize,2):new Vector2(2,CellSize),WallColor);
                else
                {
                    float gap=18,length=(CellSize-gap)*.5f;
                    foreach(float direction in new[]{-1f,1f})
                    {
                        MakeEdge(parent,"Door_Wall_"+side,edge+axis*direction*(gap*.5f+length*.5f),horizontal?new Vector2(length,2):new Vector2(2,length),WallColor);
                        MakeEdge(parent,"Door_Post_"+side,edge+axis*direction*gap*.5f,horizontal?new Vector2(2,6):new Vector2(6,2),DoorColor);
                    }
                }
            }
        }

        private void RefreshMap()
        {
            var progress = game != null ? game.progress : null;
            var current = game != null ? game.Current : null;
            var source = SourceMap.Load();
            var playerCell = CurrentPlayerCell();
            if (zoneTitleText != null)
            {
                string id = current != null ? current.id ?? "" : "";
                string term = id.Length >= 6 ? "Map/" + id.Substring(0, 3) + "_" + id.Substring(3, 3) : "Map/" + id;
                zoneTitleText.text = id.Length > 0 ? VietnameseSource.Term(term, id) : "BẢN ĐỒ";
                zoneTitleText.font = VietnameseSource.DynamicFont ?? titleFont;
            }
            if (percentText != null)
                percentText.text = (Mathf.Clamp01(progress != null ? progress.mapPercentage : 0f) * 100f).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture) + "%";

            if (cherubText != null)
                cherubText.text = (progress != null ? progress.cherubsFreed : 0) + " / 38";

            int discoveredCount = 0;
            foreach(var cell in roomImages)
            {
                bool discovered=progress!=null&&source.Discovered(progress,cell.Key);
                cell.Value.gameObject.SetActive(discovered);
                var data=source.Find(cell.Key);Color fill=ZoneColor(data.room);
                if(current!=null&&data.room==current.id)fill=Color.Lerp(fill,new Color(.62f,.74f,.88f),.25f);
                if(playerCell!=null&&playerCell.key==cell.Key)fill=Color.Lerp(fill,new Color(.66f,.9f,.94f),.35f);
                cell.Value.color=fill;
                if (discovered) discoveredCount++;
            }
            if (zoneSubText != null)
            {
                zoneSubText.text = "Đã khám phá " + discoveredCount + " / " + roomImages.Count + " ô bản đồ";
                zoneSubText.font = VietnameseSource.DynamicFont ?? bodyFont;
            }
            // Update player marker using the actual imported room ID.
            if (playerMarker != null)
            {
                bool visible = current != null && game.player != null;
                if (playerCell != null) playerMarker.anchoredPosition = playerCell.MapPosition;
                else if (visible && roomPositions.TryGetValue(current.id ?? "", out var pos)) playerMarker.anchoredPosition = pos;
                else visible = false;
                playerMarker.gameObject.SetActive(visible);
            }

            if (PinsChanged(progress)) RefreshSavedPins();
            else RefreshPinVisibility(progress);
            BrotherhoodTrialMapOverlay.Refresh(mapContent,game);
            if(playerMarker!=null)playerMarker.SetAsLastSibling();
            displayedGame = game;
            displayedPlayer = game != null ? game.player : null;
            displayedProgress = progress;
            displayedRoomId = current != null ? current.id : null;
            displayedCellKey = playerCell != null ? playerCell.key : null;
            displayedPercentage = progress != null ? progress.mapPercentage : 0f;
            displayedCherubs = progress != null ? progress.cherubsFreed : 0;
            displayedDiscovery = progress != null && progress.discoveredMapCells != null ? (string[])progress.discoveredMapCells.Clone() : Array.Empty<string>();
            hasMapSnapshot = true;
        }

        private SourceMap.Cell CurrentPlayerCell()
        {
            if (game == null || game.Current == null || game.player == null || string.IsNullOrEmpty(game.Current.id)) return null;
            return SourceMap.Load().At(game.Current.id, (Vector2)game.player.transform.position + Vector2.up * .5f);
        }

        private bool MapStateChanged()
        {
            var progress = game != null ? game.progress : null;
            string roomId = game != null && game.Current != null ? game.Current.id : null;
            var cell = CurrentPlayerCell();
            if (!hasMapSnapshot || displayedGame != game || displayedPlayer != (game != null ? game.player : null) || !ReferenceEquals(displayedProgress, progress) || displayedRoomId != roomId || displayedCellKey != (cell != null ? cell.key : null)) return true;
            if (!displayedPercentage.Equals(progress != null ? progress.mapPercentage : 0f) || displayedCherubs != (progress != null ? progress.cherubsFreed : 0)) return true;
            var discovered = progress != null ? progress.discoveredMapCells : null;
            if (displayedDiscovery.Length != (discovered != null ? discovered.Length : 0)) return true;
            for (int i = 0; i < displayedDiscovery.Length; i++) if (displayedDiscovery[i] != discovered[i]) return true;
            return PinsChanged(progress);
        }

        private bool PinsChanged(PlayerProgress progress)
        {
            var pins = progress != null ? progress.mapPins : null;
            if (!hasPinSnapshot || !ReferenceEquals(displayedPinProgress, progress) || displayedPins.Length != (pins != null ? pins.Count : 0)) return true;
            for (int i = 0; i < displayedPins.Length; i++)
            {
                var before = displayedPins[i];
                var after = pins[i];
                if (before.pinType != after.pinType || before.cellKey != after.cellKey || before.roomId != after.roomId || !before.x.Equals(after.x) || !before.y.Equals(after.y)) return true;
            }
            return false;
        }

        private void RefreshSavedPins()
        {
            foreach (var go in pinObjects) Destroy(go);
            pinObjects.Clear();

            var progress = game != null ? game.progress : null;
            displayedPinProgress = progress;
            displayedPins = progress != null && progress.mapPins != null ? progress.mapPins.ToArray() : Array.Empty<PlayerProgress.MapPinSaveData>();
            hasPinSnapshot = true;
            if (progress == null || progress.mapPins == null) return;

            foreach (var pin in progress.mapPins)
            {
                if (pin.pinType >= 0 && pin.pinType < MarkerNames.Length)
                {
                    var pObj = MakePanel(mapContent, "Pin", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), PinPosition(pin)+new Vector2(11,11), new Vector2(18,18), Color.white);
                    var img = pObj.GetComponent<Image>();
                    img.sprite = LoadSprite("UI/Sprites/" + MarkerNames[pin.pinType]);
                    img.preserveAspect = true;
                    img.raycastTarget = false;
                    pinObjects.Add(pObj);
                }
            }
            RefreshPinVisibility(progress);
            if(playerMarker!=null)playerMarker.SetAsLastSibling();
        }

        private void RefreshPinVisibility(PlayerProgress progress)
        {
            var source = SourceMap.Load();
            int objectIndex = 0;
            foreach (var pin in displayedPins)
            {
                if (pin.pinType < 0 || pin.pinType >= MarkerNames.Length) continue;
                if (objectIndex >= pinObjects.Count) break;
                var pinObject = pinObjects[objectIndex++];
                bool visible = false;
                if (progress != null)
                {
                    if (!string.IsNullOrEmpty(pin.cellKey))
                        visible = roomImages.ContainsKey(pin.cellKey) && source.Discovered(progress, pin.cellKey);
                    else if (!string.IsNullOrEmpty(pin.roomId))
                        foreach (var cell in source.RoomCells(pin.roomId))
                            if (roomImages.ContainsKey(cell.key) && source.Discovered(progress, cell.key)) { visible = true; break; }
                }
                if (pinObject != null) pinObject.SetActive(visible);
            }
        }

        private Vector2 PinPosition(PlayerProgress.MapPinSaveData pin)
        {
            var cell=SourceMap.Load().Find(pin.cellKey);if(cell!=null)return cell.MapPosition;
            return roomPositions.TryGetValue(pin.roomId??"",out var center)?center:new Vector2(pin.x,pin.y);
        }

        private void SelectPinTool(int idx)
        {
            if (selectedPinType == idx) selectedPinType = -1; // toggle off
            else selectedPinType = idx;

            for (int i = 0; i < pinSelectorImages.Length; i++)
            {
                if (pinSelectorImages[i] != null)
                    pinSelectorImages[i].color = (i == selectedPinType) ? new Color(0.95f, 0.75f, 0.25f, 0.45f) : new Color(0.15f, 0.12f, 0.15f, 0f);
            }
            if(pinHintText!=null)pinHintText.text=selectedPinType>=0?"Đang chọn: "+PinLabels[selectedPinType]+" · Chạm ô đã khám phá để đặt hoặc xóa dấu":"Chọn một dấu bên trên để đánh dấu ô đã khám phá";
            game?.Sfx("INVENTORY_SCROLL");
        }

        private void OnClickRoom(string roomId, Vector2 roomPos,string cellKey)
        {
            if (draggingMap || pinchingMap || Time.unscaledTime<suppressPinUntil || selectedPinType < 0 || game == null || game.progress == null || game.progress.mapPins == null || !SourceMap.Load().Discovered(game.progress,cellKey)) return;

            // Check if a pin already exists here; if so, remove it
            int existIdx = game.progress.mapPins.FindIndex(p => p.cellKey==cellKey||string.IsNullOrEmpty(p.cellKey)&&p.roomId==roomId);
            if (existIdx >= 0)
            {
                game.progress.mapPins.RemoveAt(existIdx);
                game.Sfx("INVENTORY_EQUIP");
            }
            else
            {
                var newPin = new PlayerProgress.MapPinSaveData
                {
                    pinType = selectedPinType,
                    x = roomPos.x,
                    y = roomPos.y,
                    roomId = roomId,cellKey=cellKey
                };
                game.progress.mapPins.Add(newPin);
                game.Sfx("INVENTORY_EQUIP");
            }
            game.SaveGame();RefreshSavedPins();
        }

        private void ChangeZoom(int delta)
        {
            StepZoom(delta, Vector2.zero);
            game?.Sfx("INVENTORY_SCROLL");
        }

        private void StepZoom(int direction,Vector2 focus)
        {
            float next=viewZoom;
            if(direction>0){foreach(float scale in ZoomScales)if(scale>viewZoom+.001f){next=scale;break;}}
            else if(direction<0){for(int i=ZoomScales.Length-1;i>=0;i--)if(ZoomScales[i]<viewZoom-.001f){next=ZoomScales[i];break;}}
            ApplyZoom(next,focus);
        }

        private void ApplyZoom(float scale,Vector2 focus)
        {
            if(mapContent==null)return;
            scale=Mathf.Clamp(scale,ZoomScales[0],ZoomScales[ZoomScales.Length-1]);
            mapContent.anchoredPosition=focus-(focus-mapContent.anchoredPosition)*(scale/viewZoom);
            viewZoom=scale;mapContent.localScale=Vector3.one*viewZoom;
            zoomLevel=0;for(int i=1;i<ZoomScales.Length;i++)if(Mathf.Abs(ZoomScales[i]-viewZoom)<Mathf.Abs(ZoomScales[zoomLevel]-viewZoom))zoomLevel=i;
            if(zoomText!=null)zoomText.text=viewZoom.ToString("0.##",System.Globalization.CultureInfo.InvariantCulture)+"×";
            ClampMapPosition();
        }

        public void Recenter()
        {
            if (mapContent != null && playerMarker != null)
            {
                mapContent.localScale=Vector3.one*viewZoom;
                mapContent.anchoredPosition = -playerMarker.anchoredPosition * viewZoom;
            }
        }

        private void OpenOptions()
        {
            Hide();
            if (optionsUI != null) optionsUI.Show();
        }

        private void UpdateSafeArea()
        {
            if(safeAreaRect==null||Screen.width<=0||Screen.height<=0)return;
            Rect area=Screen.safeArea;var size=new Vector2(Screen.width,Screen.height);
            if(area==lastSafeArea&&size==lastScreenSize)return;
            lastSafeArea=area;lastScreenSize=size;
            safeAreaRect.anchorMin=new Vector2(area.xMin/size.x,area.yMin/size.y);
            safeAreaRect.anchorMax=new Vector2(area.xMax/size.x,area.yMax/size.y);
            safeAreaRect.offsetMin=safeAreaRect.offsetMax=Vector2.zero;
        }

        private void ClampMapPosition()
        {
            if(mapContent==null||mapViewport==null)return;
            bool any=false;Vector2 min=Vector2.zero,max=Vector2.zero;
            foreach(var image in roomImages.Values)
            {
                if(!image.gameObject.activeSelf)continue;
                Vector2 point=image.rectTransform.anchoredPosition;
                if(!any){min=max=point;any=true;}else{min=Vector2.Min(min,point);max=Vector2.Max(max,point);}
            }
            if(!any)return;
            min-=Vector2.one*CellSize*.5f;max+=Vector2.one*CellSize*.5f;
            Rect view=mapViewport.rect;float margin=Mathf.Min(CellSize*viewZoom*.5f,Mathf.Min(view.width,view.height)*.25f);
            Vector2 position=mapContent.anchoredPosition;
            position.x=Mathf.Clamp(position.x,view.xMin+margin-max.x*viewZoom,view.xMax-margin-min.x*viewZoom);
            position.y=Mathf.Clamp(position.y,view.yMin+margin-max.y*viewZoom,view.yMax-margin-min.y*viewZoom);
            // The explored shape may be an L rather than a rectangle. Keep an
            // actual cell visible when panning toward an empty corner of its bounds.
            Vector2 nearest=Vector2.zero;float distance=float.PositiveInfinity;
            foreach(var image in roomImages.Values)
            {
                if(!image.gameObject.activeSelf)continue;
                Vector2 center=position+image.rectTransform.anchoredPosition*viewZoom;
                Vector2 inside=new Vector2(Mathf.Clamp(center.x,view.xMin+margin,view.xMax-margin),Mathf.Clamp(center.y,view.yMin+margin,view.yMax-margin));
                Vector2 correction=inside-center;
                if(correction.sqrMagnitude<distance){distance=correction.sqrMagnitude;nearest=correction;}
                if(distance<.0001f)break;
            }
            position+=nearest;
            mapContent.anchoredPosition=position;
        }

        public void BeginViewportDrag(PointerEventData eventData)
        {
            if(!IsOpen||pinchingMap)return;
            draggingMap=true;eventData.eligibleForClick=false;
        }

        public void DragViewport(PointerEventData eventData)
        {
            if(!IsOpen||pinchingMap||mapContent==null)return;
            draggingMap=true;eventData.eligibleForClick=false;
            mapContent.anchoredPosition+=eventData.delta/Mathf.Max(.001f,Canvas.scaleFactor);
            ClampMapPosition();
        }

        public void EndViewportDrag(PointerEventData eventData)
        {
            draggingMap=false;eventData.eligibleForClick=false;
            suppressPinUntil=Time.unscaledTime+.15f;
        }

        public void ScrollViewport(PointerEventData eventData)
        {
            if(!IsOpen||mapViewport==null||Mathf.Abs(eventData.scrollDelta.y)<.01f)return;
            if(RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewport,eventData.position,eventData.enterEventCamera,out var focus))
                StepZoom(eventData.scrollDelta.y>0?1:-1,focus);
        }

        private void UpdatePinch()
        {
            var screen=Touchscreen.current;
            Camera inputCamera=Canvas!=null&&Canvas.renderMode!=RenderMode.ScreenSpaceOverlay?Canvas.worldCamera:null;
            int count=0,idA=-1,idB=-1;Vector2 a=Vector2.zero,b=Vector2.zero;
            if(screen!=null)foreach(var touch in screen.touches)
            {
                if(!touch.press.isPressed)continue;
                if(count==0){a=touch.position.ReadValue();idA=touch.touchId.ReadValue();}
                else{b=touch.position.ReadValue();idB=touch.touchId.ReadValue();}
                if(++count==2)break;
            }
            if(count<2)
            {
                if(pinchingMap)suppressPinUntil=Time.unscaledTime+.2f;
                pinchingMap=false;previousPinchDistance=0;return;
            }
            if(!pinchingMap||idA!=pinchFingerA||idB!=pinchFingerB)
            {
                if(mapViewport==null||!RectTransformUtility.RectangleContainsScreenPoint(mapViewport,a,inputCamera)||!RectTransformUtility.RectangleContainsScreenPoint(mapViewport,b,inputCamera)){pinchingMap=false;return;}
                pinchingMap=true;draggingMap=false;pinchFingerA=idA;pinchFingerB=idB;
                previousPinchDistance=Vector2.Distance(a,b);previousPinchCenter=(a+b)*.5f;
                suppressPinUntil=Time.unscaledTime+.2f;return;
            }
            float distance=Vector2.Distance(a,b);
            Vector2 center=(a+b)*.5f;
            if(previousPinchDistance>5f&&distance>5f&&RectTransformUtility.ScreenPointToLocalPointInRectangle(mapViewport,center,inputCamera,out var focus))
            {
                mapContent.anchoredPosition+=(center-previousPinchCenter)/Mathf.Max(.001f,Canvas.scaleFactor);
                ApplyZoom(viewZoom*distance/previousPinchDistance,focus);
            }
            previousPinchDistance=distance;previousPinchCenter=center;
            suppressPinUntil=Time.unscaledTime+.2f;
        }

        private Sprite LoadSprite(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            if (spriteCache.TryGetValue(path, out var s)) return s;

            if(path=="UI/Sprites/map-soledad")
            {
                var atlas=Resources.Load<Texture2D>("UI/map-source-icons");
                if(atlas!=null)
                {
                    atlas.filterMode=FilterMode.Point;
                    s=Sprite.Create(atlas,new Rect(501,153,10,10),new Vector2(.5f,.5f),100);
                    spriteCache[path]=s;return s;
                }
            }

            var sp = Resources.Load<Sprite>(path);
            if (sp != null)
            {
                spriteCache[path] = sp;
                return sp;
            }

            var subSprites = Resources.LoadAll<Sprite>(path);
            if (subSprites != null && subSprites.Length > 0)
            {
                spriteCache[path] = subSprites[0];
                return subSprites[0];
            }

            var tex = Resources.Load<Texture2D>(path);
            if (tex != null)
            {
                tex.filterMode = FilterMode.Point;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100);
                spriteCache[path] = s;
                return s;
            }
            return null;
        }

        private GameObject MakePanel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.GetComponent<Image>();
            img.color = color;
            return go;
        }

        private GameObject MakeButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, Action onClick)
        {
            var go = MakePanel(parent, name, anchor, anchor, pos, size, Color.white);
            var btn = go.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            return go;
        }

        private Text MakeLabel(Transform parent, string text, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size, int fontSize, TextAnchor alignment, Color color, bool isBold = true, bool wrap = false)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var txt = go.GetComponent<Text>();
            if (titleFont == null) LoadFonts();
            txt.font = isBold ? (titleFont ?? bodyFont) : (bodyFont ?? titleFont);
            txt.text = text;
            txt.fontSize = fontSize;
            txt.alignment = alignment;
            txt.color = color;
            txt.raycastTarget = false;
            txt.horizontalOverflow = wrap ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
            txt.verticalOverflow = wrap ? VerticalWrapMode.Overflow : VerticalWrapMode.Overflow;
            if (wrap) txt.lineSpacing = 1.2f;
            return txt;
        }
    }

    public sealed class BlasMapViewportInput : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IScrollHandler
    {
        public BlasMapUI map;
        public void OnBeginDrag(PointerEventData eventData)=>map?.BeginViewportDrag(eventData);
        public void OnDrag(PointerEventData eventData)=>map?.DragViewport(eventData);
        public void OnEndDrag(PointerEventData eventData)=>map?.EndViewportDrag(eventData);
        public void OnScroll(PointerEventData eventData)=>map?.ScrollViewport(eventData);
    }
}
