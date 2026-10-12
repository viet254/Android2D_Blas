using System;
using System.Collections.Generic;
using UnityEngine;

namespace Brotherhood
{
    /// <summary>
    /// Constructs and manages the authentic Candelaria Store room (D17Z01S06).
    /// Faithful recreation of the original Blasphemous merchant tent environment.
    /// </summary>
    [ExecuteAlways]
    public sealed class CandelariaMerchantRoom : MonoBehaviour
    {
        public static CandelariaMerchantRoom Instance { get; private set; }

        static readonly Rect[] LadronaFrames = new Rect[]
        {
            new Rect(0.0f, 0.0f, 47.0f, 97.0f),
            new Rect(0.0f, 97.0f, 47.0f, 97.0f),
            new Rect(0.0f, 194.0f, 47.0f, 97.0f),
            new Rect(0.0f, 291.0f, 47.0f, 97.0f),
            new Rect(0.0f, 388.0f, 47.0f, 97.0f),
            new Rect(94.0f, 291.0f, 46.0f, 97.0f),
            new Rect(94.0f, 388.0f, 46.0f, 97.0f),
            new Rect(187.0f, 97.0f, 45.0f, 97.0f),
            new Rect(232.0f, 97.0f, 45.0f, 97.0f),
            new Rect(277.0f, 97.0f, 45.0f, 97.0f),
            new Rect(322.0f, 97.0f, 45.0f, 97.0f),
            new Rect(141.0f, 97.0f, 46.0f, 97.0f),
            new Rect(188.0f, 0.0f, 47.0f, 97.0f),
            new Rect(235.0f, 0.0f, 47.0f, 97.0f),
            new Rect(282.0f, 0.0f, 47.0f, 97.0f),
            new Rect(329.0f, 0.0f, 47.0f, 97.0f),
            new Rect(376.0f, 0.0f, 47.0f, 97.0f),
            new Rect(423.0f, 0.0f, 47.0f, 97.0f),
            new Rect(94.0f, 97.0f, 47.0f, 97.0f),
            new Rect(94.0f, 194.0f, 47.0f, 97.0f),
            new Rect(47.0f, 0.0f, 47.0f, 97.0f),
            new Rect(47.0f, 97.0f, 47.0f, 97.0f),
            new Rect(141.0f, 291.0f, 46.0f, 97.0f),
            new Rect(141.0f, 388.0f, 46.0f, 97.0f),
            new Rect(412.0f, 97.0f, 45.0f, 97.0f),
            new Rect(457.0f, 97.0f, 45.0f, 97.0f),
            new Rect(187.0f, 194.0f, 45.0f, 97.0f),
            new Rect(367.0f, 97.0f, 45.0f, 97.0f),
            new Rect(141.0f, 194.0f, 46.0f, 97.0f),
            new Rect(47.0f, 194.0f, 47.0f, 97.0f),
            new Rect(47.0f, 291.0f, 47.0f, 97.0f),
            new Rect(47.0f, 388.0f, 47.0f, 97.0f),
            new Rect(94.0f, 0.0f, 47.0f, 97.0f),
            new Rect(141.0f, 0.0f, 47.0f, 97.0f)
        };

        SpriteRenderer candelariaRenderer;
        Sprite[] ladronaSprites;
        int candelariaFrame;
        float candelariaTimer;

        readonly List<ShelfItemVisual> shelfItems = new List<ShelfItemVisual>();

        sealed class ShelfItemVisual
        {
            public string itemId;
            public GameObject go;
            public Vector3 basePos;
            public float timeOffset;
        }

        void Awake()
        {
            Instance = this;
            BuildEnvironment();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void OnEnable()
        {
            var mat = GetRestoredMaterial();
            if (candelariaRenderer != null)
            {
                if (mat != null) candelariaRenderer.sharedMaterial = mat;
                candelariaRenderer.enabled = true;
                candelariaRenderer.color = Color.white;
                candelariaRenderer.sortingLayerName = "Player";
                candelariaRenderer.sortingOrder = 0;
                if (candelariaRenderer.sprite == null && ladronaSprites != null && ladronaSprites.Length > 0)
                {
                    candelariaRenderer.sprite = ladronaSprites[candelariaFrame % ladronaSprites.Length];
                }
            }
            RefreshDisplayItems();
        }

        public Material GetRestoredMaterial()
        {
            var mat = Resources.Load<Material>("Generated/RestoredSprite");
            if (mat == null)
            {
                var game = FindObjectOfType<BrotherhoodGame>();
                if (game != null && game.player != null && game.player.actor != null && game.player.actor.visual != null)
                    mat = game.player.actor.visual.sharedMaterial;
            }
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default") ?? Shader.Find("Sprites/Default");
                if (shader != null) mat = new Material(shader);
            }
            return mat;
        }

        void LoadLadronaSprites()
        {
            var list = new List<Sprite>();
            for (int i = 0; i < 34; i++)
            {
                string path = "Merchant/LadronaIdle/ladrona_" + (i < 10 ? ("0" + i) : i.ToString());
                var sp = Resources.Load<Sprite>(path);
                if (sp != null) list.Add(sp);
            }
            if (list.Count == 34)
            {
                ladronaSprites = list.ToArray();
                return;
            }

            var loaded = Resources.LoadAll<Sprite>("Merchant/ladrona_idle_anim");
            if (loaded != null && loaded.Length >= 34)
            {
                Array.Sort(loaded, (a, b) =>
                {
                    int ia = ParseFrameIndex(a.name);
                    int ib = ParseFrameIndex(b.name);
                    return ia.CompareTo(ib);
                });
                ladronaSprites = loaded;
                return;
            }

            var ladronaTex = Resources.Load<Texture2D>("Merchant/ladrona_idle_anim");
            if (ladronaTex != null)
            {
                ladronaTex.filterMode = FilterMode.Point;
                ladronaSprites = new Sprite[LadronaFrames.Length];
                for (int i = 0; i < LadronaFrames.Length; i++)
                {
                    float pivotX = (i >= 7 && i <= 10) || (i >= 24 && i <= 27) ? 0.5556f : (i == 5 || i == 6 || i == 11 || i == 22 || i == 23 || i == 28) ? 0.5435f : 0.5319f;
                    ladronaSprites[i] = Sprite.Create(ladronaTex, LadronaFrames[i], new Vector2(pivotX, 0.0825f), 32f);
                }
            }
        }

        static int ParseFrameIndex(string name)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            int lastUnderscore = name.LastIndexOf('_');
            if (lastUnderscore >= 0 && int.TryParse(name.Substring(lastUnderscore + 1), out int idx))
                return idx;
            return 0;
        }

        void BuildEnvironment()
        {
            var mat = GetRestoredMaterial();

            // Textures
            var shopTex = Resources.Load<Texture2D>("Merchant/merchant-shop-spritesheet");
            if (shopTex != null) shopTex.filterMode = FilterMode.Point;

            LoadLadronaSprites();

            // 1. Background Tent Backdrop (640x360 at 32 PPU = 20x11.25)
            var bgChild = transform.Find("Tent_Backdrop");
            var bgGO = bgChild != null ? bgChild.gameObject : new GameObject("Tent_Backdrop");
            bgGO.transform.SetParent(transform, false);
            bgGO.transform.position = new Vector3(-880f, -21.0f, 5f);
            var sr = bgGO.GetComponent<SpriteRenderer>();
            if (sr == null) sr = bgGO.AddComponent<SpriteRenderer>();
            if (shopTex != null && sr.sprite == null)
            {
                sr.sprite = Sprite.Create(shopTex, new Rect(2, 661, 640, 360), new Vector2(0.5f, 0.5f), 32f);
            }
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingLayerName = "Default";
            sr.sortingOrder = -10;
            sr.color = Color.white;
            sr.enabled = true;

            // 2. Candelaria Animated NPC (placed at her chair by the counter)
            var existing = transform.Find("Candelaria");
            var candelariaGO = existing != null ? existing.gameObject : new GameObject("Candelaria");
            candelariaGO.transform.SetParent(transform, false);
            candelariaGO.transform.position = new Vector3(-878.0f, -24.975f, 0f);
            candelariaGO.transform.localScale = new Vector3(-1f, 1f, 1f); // face left toward entrance

            candelariaRenderer = candelariaGO.GetComponent<SpriteRenderer>();
            if (candelariaRenderer == null) candelariaRenderer = candelariaGO.AddComponent<SpriteRenderer>();
            if (mat != null) candelariaRenderer.sharedMaterial = mat;
            candelariaRenderer.sortingLayerName = "Player";
            candelariaRenderer.sortingOrder = 0;
            candelariaRenderer.color = Color.white;
            candelariaRenderer.enabled = true;
            if (ladronaSprites != null && ladronaSprites.Length > 0)
            {
                candelariaRenderer.sprite = ladronaSprites[0];
            }

            var animComp = candelariaGO.GetComponent<CandelariaAnimator>();
            if (animComp == null) animComp = candelariaGO.AddComponent<CandelariaAnimator>();
            animComp.visual = candelariaRenderer;
            animComp.EnsureFramesLoaded();

            var idNode = candelariaGO.GetComponent<SourceObjectId>();
            if (idNode == null) idNode = candelariaGO.AddComponent<SourceObjectId>();
            idNode.room = "D17Z01S06";
            idNode.node = "Candelaria";

            // 3. Display Items on the Counter near Candelaria
            shelfItems.Clear();
            SetupShelfItem("RB02", new Vector3(-878.5f, -23.5f, 0f), 0f, mat);
            SetupShelfItem("QI58", new Vector3(-876.8f, -23.2f, 0f), 1f, mat);
            SetupShelfItem("RB05", new Vector3(-875.2f, -23.5f, 0f), 2f, mat);

            // 4. Solid Colliders (only built if not already authored in the prefab)
            BuildColliders();

            RefreshDisplayItems();
        }

        void SetupShelfItem(string itemId, Vector3 pos, float timeOffset, Material mat)
        {
            var iconTex = Resources.Load<Texture2D>("Inventory/Icons/" + itemId);
            if (iconTex == null) return;
            iconTex.filterMode = FilterMode.Point;

            var sp = Sprite.Create(iconTex, new Rect(0, 0, iconTex.width, iconTex.height), new Vector2(0.5f, 0.5f), 64f);
            var child = transform.Find("Shelf_" + itemId);
            var itemGO = child != null ? child.gameObject : new GameObject("Shelf_" + itemId);
            itemGO.transform.SetParent(transform, false);
            itemGO.transform.position = pos;

            var sr = itemGO.GetComponent<SpriteRenderer>();
            if (sr == null) sr = itemGO.AddComponent<SpriteRenderer>();
            sr.sprite = sp;
            if (mat != null) sr.sharedMaterial = mat;
            sr.sortingLayerName = "Player";
            sr.sortingOrder = 2;
            sr.color = Color.white;
            sr.enabled = true;

            // Soft glowing aura behind the item
            var auraChild = itemGO.transform.Find("Aura");
            var auraGO = auraChild != null ? auraChild.gameObject : new GameObject("Aura");
            auraGO.transform.SetParent(itemGO.transform, false);
            auraGO.transform.localPosition = new Vector3(0, 0, 0.1f);
            auraGO.transform.localScale = Vector3.one * 1.3f;
            var asr = auraGO.GetComponent<SpriteRenderer>();
            if (asr == null) asr = auraGO.AddComponent<SpriteRenderer>();
            asr.sprite = sp;
            if (mat != null) asr.sharedMaterial = mat;
            asr.color = new Color(1f, 0.85f, 0.45f, 0.35f);
            asr.sortingLayerName = "Player";
            asr.sortingOrder = 1;
            asr.enabled = true;

            shelfItems.Add(new ShelfItemVisual
            {
                itemId = itemId,
                go = itemGO,
                basePos = pos,
                timeOffset = timeOffset
            });
        }

        void BuildColliders()
        {
            // If colliders are authored directly in the prefab, preserve them
            if (transform.Find("Ground_Floor") != null) return;

            // Floor Collider: ground layer 8
            var floorGO = new GameObject("Ground_Floor");
            floorGO.transform.SetParent(transform, false);
            floorGO.transform.position = new Vector3(-880f, -26.0f, 0f);
            floorGO.layer = 8;
            var floorCol = floorGO.AddComponent<BoxCollider2D>();
            floorCol.size = new Vector2(20f, 2f); // top surface is at -25.0f

            // Left Wall
            var leftGO = new GameObject("Wall_Left");
            leftGO.transform.SetParent(transform, false);
            leftGO.transform.position = new Vector3(-889.5f, -21.0f, 0f);
            leftGO.layer = 8;
            var leftCol = leftGO.AddComponent<BoxCollider2D>();
            leftCol.size = new Vector2(1f, 10f);

            // Right Wall
            var rightGO = new GameObject("Wall_Right");
            rightGO.transform.SetParent(transform, false);
            rightGO.transform.position = new Vector3(-870.5f, -21.0f, 0f);
            rightGO.layer = 8;
            var rightCol = rightGO.AddComponent<BoxCollider2D>();
            rightCol.size = new Vector2(1f, 10f);

            // Ceiling
            var ceilGO = new GameObject("Ceiling");
            ceilGO.transform.SetParent(transform, false);
            ceilGO.transform.position = new Vector3(-880f, -15.5f, 0f);
            ceilGO.layer = 8;
            var ceilCol = ceilGO.AddComponent<BoxCollider2D>();
            ceilCol.size = new Vector2(20f, 1f);
        }

        public void RefreshDisplayItems()
        {
            var game = FindObjectOfType<BrotherhoodGame>();
            if (game == null || game.progress == null) return;

            foreach (var item in shelfItems)
            {
                if (item.go == null) continue;
                bool owned = game.progress.Owns(item.itemId);
                item.go.SetActive(!owned);
            }
        }

        void Update()
        {
            // Lazy load if needed
            if (candelariaRenderer == null)
            {
                var existingCand = transform.Find("Candelaria");
                if (existingCand != null) candelariaRenderer = existingCand.GetComponent<SpriteRenderer>();
            }
            if (ladronaSprites == null || ladronaSprites.Length == 0)
            {
                LoadLadronaSprites();
            }

            // Animate Candelaria idle if no CandelariaAnimator on her
            if (candelariaRenderer != null)
            {
                var anim = candelariaRenderer.GetComponent<CandelariaAnimator>();
                if (anim == null && ladronaSprites != null && ladronaSprites.Length > 0)
                {
                    float dt = Application.isPlaying ? Time.deltaTime : 0.0833f;
                    candelariaTimer += dt;
                    if (candelariaTimer >= 0.0833f)
                    {
                        int steps = (int)(candelariaTimer / 0.0833f);
                        candelariaTimer -= steps * 0.0833f;
                        candelariaFrame = (candelariaFrame + steps) % ladronaSprites.Length;
                        candelariaRenderer.sprite = ladronaSprites[candelariaFrame];
                    }
                }
                if (!candelariaRenderer.enabled) candelariaRenderer.enabled = true;
            }

            // Animate floating items on shelf
            float time = Application.isPlaying ? Time.time : (float)Time.realtimeSinceStartup;
            foreach (var item in shelfItems)
            {
                if (item.go == null || !item.go.activeSelf) continue;
                float floatY = Mathf.Sin(time * 2.2f + item.timeOffset) * 0.08f;
                item.go.transform.position = item.basePos + new Vector3(0, floatY, 0);
            }
        }
    }
}
