using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Brotherhood
{
    // An explicitly custom branch. Source room data, doors and quest flags stay
    // intact; these passages and objectives are attached to runtime instances.
    public sealed class BrotherhoodTrialRoute : MonoBehaviour
    {
        public const string ArenaFlag = "D17_TRIAL/ARENA_CLEARED";
        public const string EntranceRoom = "D17Z01S03";
        public const string BossFlag = "D17_TRIAL/BOSS_DEFEATED";
        // Kept for old saves; solving the retired bells never defeats the boss.
        public const string RelayFlag = "D17_TRIAL/RELAY_SOLVED";
        public const string AltarFlag = "D17_TRIAL/ALTAR_REACHED";
        public const string VisitedFlag = "D17_TRIAL/VISITED";

        [Serializable]
        public sealed class TrialPassage
        {
            public string room, target, label, requiredFlag;
            public Vector2 position, arrival;
            public string sourceKey, artNode;
            // Side gates are entered by walking toward the room edge. Doors
            // painted on the back wall keep their deliberate USE interaction.
            public int walkDirection;
            public float walkAtX;
            [NonSerialized] public RoomDoor sourceDoor;
            [NonSerialized] public Transform artAnchor;
        }

        public TrialPassage[] Passages { get; private set; } = Array.Empty<TrialPassage>();
        public EnemyController[] ArenaEnemies { get; private set; } = Array.Empty<EnemyController>();
        public EnemyController TrialBoss { get; private set; }
        public BrotherhoodTrialGateFx GateFx { get; private set; }
        public bool BossDefeated => game != null && Done(BossFlag);
        public int CompletedObjectives => game == null ? 0 : (Done(ArenaFlag) ? 1 : 0) + (Done(BossFlag) ? 1 : 0) + (Done(AltarFlag) ? 1 : 0);
        public bool CanInteract => NearPassage() != null;

        BrotherhoodGame game;
        TrialPassage pendingPassage;
        TrialPassage blockedWalkPassage;
        TrialPassage suppressedReturn;
        string expectedArrivalRoom;
        readonly List<LadderZone> trialLadders = new List<LadderZone>();
        readonly Dictionary<LadderZone, Vector2> ladderLandings = new Dictionary<LadderZone, Vector2>();
        Canvas hud;
        RectTransform safeArea;
        Text objectiveText;
        Rect oldSafeArea;
        Vector2 oldScreen;
        static Sprite pixel;

        bool Done(string flag) => game.progress.HasFlag(flag);

        public void Initialize(BrotherhoodGame owner)
        {
            game = owner;
            Passages = new[]
            {
                Passage(EntranceRoom, "D17Z01S04", "Vào hầm thử thách", -778, 8.025f, -858.5f, -4.975f, "relic", "LOGIC_95"),
                Passage("D17Z01S04", EntranceRoom, "Trở về tu viện", -860, -4.975f, -780, 8.025f, "FrontL", "LOGIC_140"),
                // Older saves may resume beyond the sealed middle wall. Use
                // the other authored doorway to leave that side safely.
                Passage("D17Z01S04", EntranceRoom, "Trở về tu viện", -840, -4.975f, -780, 8.025f, "FrontR", "LOGIC_141"),
                Passage("D17Z01S04", "D17Z01S07", "Sân gác dưới hầm", -869.2f, -4.975f, -866.8f, -24.975f, "W", null, null, -1, -869.4f),
                Passage("D17Z01S07", "D17Z01S04", "Trở lên lối hầm", -869.2f, -24.975f, -867.2f, -4.975f, "W", null, null, -1, -869.4f),
                Passage("D17Z01S07", "D17Z01S09", "Phòng Lavia", -850.8f, -32.975f, -874, -12.975f, "SE", "LOGIC_106", ArenaFlag, 1, -850.6f),
                Passage("D17Z01S09", "D17Z01S07", "Trở về sân gác", -871.7f, -12.975f, -852.5f, -32.975f, "E", null, null, 1, -871.6f),
                Passage("D17Z01S09", "D17Z01S08", "Bàn thờ Mea Culpa", -887.15f, -12.975f, -873.5f, -34.975f, null, "DECO_151", BossFlag, -1, -887.35f),
                Passage("D17Z01S08", "D17Z01S09", "Trở về phòng boss", -871.2f, -34.975f, -885.5f, -12.975f, "E", null, null, 1, -870.6f)
            };
            foreach (var passage in Passages)
            {
                var room = game.Find(passage.room);
                if (room != null) BindPassage(room, passage);
            }
            var arena = game.Find("D17Z01S07");
            if (arena != null)
            {
                var scout = SpawnTrialEnemy(arena, "acolyte", new Vector2(-864, -36.97f), BrotherhoodTrialEnemyAI.Kind.Scout, 54, 60);
                // The imported stair ramp rises under x=-860.2. Spawn on the
                // adjacent flat floor so the motor does not start inside it.
                var guard = SpawnTrialEnemy(arena, "flagellant", new Vector2(-862.4f, -36.975f), BrotherhoodTrialEnemyAI.Kind.Guard, 72, 90);
                var enemies = new List<EnemyController>(arena.enemies ?? Array.Empty<EnemyController>());
                var custom = new List<EnemyController>();
                if (scout != null) { enemies.Add(scout); custom.Add(scout); }
                if (guard != null) { enemies.Add(guard); custom.Add(guard); }
                arena.enemies = enemies.ToArray(); ArenaEnemies = custom.ToArray();
                AddLadder(arena, new Vector2(-857.5f, -17.5f), 15f);
                AddLadder(arena, new Vector2(-864.5f, -31), 12f);
                // The source upper platform ends at x=-858. Give the custom
                // ladder a short landing so either direction can reach it.
                var landing = Quad(arena.transform, "Trial upper ladder landing", new Vector2(-857.9f, -10.075f), new Vector2(1.2f, .15f), new Color(.53f, .38f, .2f));
                landing.gameObject.layer = 9;
                landing.gameObject.AddComponent<BoxCollider2D>();
            }
            var bossRoom = game.Find("D17Z01S09");
            if (bossRoom != null)
            {
                var exit = Array.Find(Passages, passage => passage.room == bossRoom.id && passage.target == "D17Z01S08");
                var rockArt = exit?.artAnchor != null ? exit.artAnchor.GetComponent<SpriteRenderer>() : null;
                if (rockArt == null) Debug.LogError("Missing S09 DECO_151 rubble renderer for the trial boss gate");
                else
                {
                    GateFx = bossRoom.gameObject.AddComponent<BrotherhoodTrialGateFx>();
                    GateFx.Configure(game, rockArt, BossDefeated);
                }
                SpawnBoss(bossRoom);
            }
            BuildHud();
        }

        static TrialPassage Passage(string room, string target, string label, float x, float y, float ax, float ay, string key, string art, string flag = null, int walkDirection = 0, float walkAtX = 0)
            => new TrialPassage { room = room, target = target, label = label, position = new Vector2(x, y), arrival = new Vector2(ax, ay), sourceKey = key, artNode = art, requiredFlag = flag, walkDirection = walkDirection, walkAtX = walkAtX };

        void SpawnBoss(RoomState room)
        {
            EnemyController template = null;
            foreach (var sourceRoom in game.rooms)
                foreach (var enemy in sourceRoom.enemies ?? Array.Empty<EnemyController>())
                    if (enemy != null && !enemy.boss && enemy.family == "wheelcarrier") { template = enemy; break; }
            if (template == null) { Debug.LogError("Lavia needs an EnemyController template for the source damage and reward pipeline"); return; }
            TrialBoss = Instantiate(template.gameObject, room.transform).GetComponent<EnemyController>();
            TrialBoss.name = "Trial Boss - Lavia"; TrialBoss.transform.position = new Vector2(-882, -12.975f);
            TrialBoss.game = game; TrialBoss.boss = false; TrialBoss.family = "lavia";
            TrialBoss.maxHealth = TrialBoss.health = 240; TrialBoss.purgeReward = 150;
            TrialBoss.motor.size = new Vector2(.8f, 1.4f);
            foreach (var id in TrialBoss.GetComponentsInChildren<SourceObjectId>(true)) Destroy(id);
            TrialBoss.actor.visual.enabled = false;
            var spriteNode = new GameObject("Lavia sprite");
            spriteNode.transform.SetParent(TrialBoss.transform, false);
            spriteNode.transform.localPosition = new Vector3(0, 1.1f, -.05f);
            spriteNode.transform.localScale = Vector3.one * 1.35f;
            var renderer = spriteNode.AddComponent<SpriteRenderer>();
            renderer.sortingLayerName = "Player"; renderer.sortingOrder = 2;
            var display = spriteNode.AddComponent<BrotherhoodTrialLaviaVisual>();
            display.Configure(renderer);
            TrialBoss.gameObject.AddComponent<BrotherhoodTrialLaviaAI>().Configure(game, this, TrialBoss, display);
            var enemies = new List<EnemyController>(room.enemies ?? Array.Empty<EnemyController>()) { TrialBoss };
            room.enemies = enemies.ToArray();
        }

        EnemyController SpawnTrialEnemy(RoomState room, string family, Vector2 at, BrotherhoodTrialEnemyAI.Kind kind, float health, int reward)
        {
            EnemyController template = null;
            foreach (var sourceRoom in game.rooms)
                foreach (var enemy in sourceRoom.enemies ?? Array.Empty<EnemyController>())
                    if (enemy != null && !enemy.boss && enemy.family == family && enemy.trialAI == null) { template = enemy; break; }
            if (template == null) { Debug.LogError("Trial branch needs source enemy family " + family); return null; }
            var copy = Instantiate(template.gameObject, room.transform).GetComponent<EnemyController>();
            copy.name = kind == BrotherhoodTrialEnemyAI.Kind.Scout ? "Trial Scout" : "Trial Guard";
            copy.transform.position = at; copy.game = game; copy.maxHealth = copy.health = health; copy.purgeReward = reward;
            foreach (var id in copy.GetComponentsInChildren<SourceObjectId>(true)) Destroy(id);
            copy.trialAI = copy.gameObject.AddComponent<BrotherhoodTrialEnemyAI>(); copy.trialAI.Configure(copy, kind, 2.5f, -1);
            copy.actor.visual.color = kind == BrotherhoodTrialEnemyAI.Kind.Scout ? new Color(.72f, .9f, .82f) : new Color(1f, .8f, .68f);
            return copy;
        }

        void AddLadder(RoomState room, Vector2 center, float height)
        {
            var node = new GameObject("Trial return ladder"); node.transform.SetParent(room.transform); node.transform.position = center;
            // The player checks feet + 0.6 against the sensor. Extend its top so
            // climbing reaches the floor with the feet, then end at that floor.
            var added = new LadderZone { sensor = node.transform, center = center + Vector2.up * .4f, size = new Vector2(.49f, height + .8f) };
            trialLadders.Add(added);
            ladderLandings[added] = new Vector2(center.y - height * .5f + .025f, center.y + height * .5f + .025f);
            var ladders = new List<LadderZone>(room.ladders ?? Array.Empty<LadderZone>()); ladders.Insert(0, added);
            room.ladders = ladders.ToArray();
            for (int side = -1; side <= 1; side += 2)
                Quad(node.transform, "Ladder rail", center + Vector2.right * side * .22f, new Vector2(.055f, height), new Color(.45f, .32f, .2f));
            for (float y = -height * .5f; y <= height * .5f; y += .42f)
                Quad(node.transform, "Ladder rung", center + Vector2.up * y, new Vector2(.49f, .055f), new Color(.65f, .48f, .27f));
        }

        void BindPassage(RoomState room, TrialPassage passage)
        {
            passage.sourceDoor = string.IsNullOrEmpty(passage.sourceKey) ? null : room.Door(passage.sourceKey);
            foreach (var node in room.GetComponentsInChildren<SourceObjectId>(true))
                if (node.node == passage.artNode) { passage.artAnchor = node.transform; break; }
            if (passage.artAnchor == null && passage.sourceDoor != null) passage.artAnchor = passage.sourceDoor.trigger;
            if (passage.artAnchor == null) Debug.LogError("Trial passage is missing its authored gate: " + room.id + "/" + passage.artNode);
        }

        public bool HandlesDoor(RoomState room, RoomDoor door)
        {
            foreach (var passage in Passages) if (passage.room == room.id && passage.sourceDoor == door) return true;
            return false;
        }

        public bool AllowSourceDoor(RoomState room, RoomDoor door)
        {
            if (room.id != "D17Z01S07") return true;
            if (door.key == "NW") return Done(ArenaFlag);
            if (door.key == "SW") return BossDefeated;
            return true;
        }

        static SpriteRenderer Quad(Transform parent, string name, Vector2 at, Vector2 size, Color color)
        {
            if (pixel == null)
            {
                var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point }; tex.SetPixel(0, 0, Color.white); tex.Apply();
                pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * .5f, 1);
            }
            var node = new GameObject(name); node.transform.SetParent(parent); node.transform.position = at; node.transform.localScale = new Vector3(size.x, size.y, 1);
            var visual = node.AddComponent<SpriteRenderer>(); visual.sprite = pixel; visual.color = color; visual.sortingLayerName = "Player"; visual.sortingOrder = -1;
            return visual;
        }


        TrialPassage NearPassage(bool includeWalkGates = false)
        {
            if (game == null || game.Current == null || game.player == null || game.player.Dead || game.InputBlocked || !game.player.motor.grounded) return null;
            foreach (var passage in Passages)
                if ((includeWalkGates || passage.walkDirection == 0) && game.Current.id == passage.room && Mathf.Abs(game.player.transform.position.x - passage.position.x) <= 1.3f && Mathf.Abs(game.player.transform.position.y - passage.position.y) < 1.8f) return passage;
            return null;
        }

        // Called after input sampling and before player physics: a closed gate
        // stops the player while there is still authored floor underfoot.
        public void HandleWalkPassage()
        {
            blockedWalkPassage = null;
            if (game == null || game.Current == null || game.player == null || game.player.Dead || game.InputBlocked) return;
            float move = game.controls.Move;
            float velocityX = game.player.motor.velocity.x;
            // Dash and lunge can keep moving toward an edge even after the
            // joystick is released or held the other way. Check both input
            // and committed high-speed travel so the gate cannot be skipped.
            // Ordinary walking stops on release in PlayerController.Tick;
            // its previous-frame velocity must not open a gate by itself.
            bool committedTravel = game.player.dashTime > 0 || Mathf.Abs(velocityX) >= 8f;
            if (suppressedReturn != null && (game.Current.id != suppressedReturn.room ||
                // Releasing normal walking input is enough even when the
                // previous frame's motor velocity has not settled yet.
                (Mathf.Abs(move) < .25f && (!committedTravel || Mathf.Abs(velocityX) < .25f)) ||
                (move * suppressedReturn.walkDirection < -.25f && velocityX * suppressedReturn.walkDirection < .25f)))
                suppressedReturn = null;
            foreach (var passage in Passages)
            {
                if (passage.walkDirection == 0 || passage.room != game.Current.id || Mathf.Abs(game.player.transform.position.y - passage.position.y) >= 1.8f) continue;
                if (move * passage.walkDirection < .25f && (!committedTravel || velocityX * passage.walkDirection < .25f)) continue;
                float x = game.player.transform.position.x;
                if (passage.walkDirection < 0 ? x > passage.walkAtX : x < passage.walkAtX) continue;
                if (suppressedReturn == passage)
                {
                    StopAtGate(passage);
                    game.Message("Thả hướng rồi đi vào cổng để quay lại.");
                    return;
                }
                if (!string.IsNullOrEmpty(passage.requiredFlag) && !Done(passage.requiredFlag))
                {
                    StopAtGate(passage);
                    game.Message(passage.requiredFlag == ArenaFlag ? "Hạ trinh sát và vệ binh để mở cổng phòng boss." : "Hạ Lavia để mở cổng tới Mea Culpa.");
                    return;
                }
                if (!StartPassage(passage, true))
                {
                    StopAtGate(passage);
                    game.Message("Chưa thể đi qua cổng này.");
                }
                return;
            }
        }

        void StopAtGate(TrialPassage passage)
        {
            blockedWalkPassage = passage;
            game.controls.Move = 0;
            var velocity = game.player.motor.velocity;
            velocity.x = 0;
            game.player.motor.velocity = velocity;
        }

        public void KeepPlayerAtWalkGate()
        {
            var passage = blockedWalkPassage;
            if (passage == null || game.Current == null || game.Current.id != passage.room || game.player.Dead) return;
            // A dash can keep its own velocity despite Move being cleared.
            // Hold it inside the authored floor until the gate can be opened.
            float safeX = passage.walkAtX - passage.walkDirection * .03f;
            float x = game.player.transform.position.x;
            if (passage.walkDirection < 0 ? x >= safeX : x <= safeX) return;
            var feet = game.player.transform.position;
            feet.x = safeX;
            game.player.transform.position = feet;
            Physics2D.SyncTransforms();
            var velocity = game.player.motor.velocity;
            velocity.x = 0;
            game.player.motor.velocity = velocity;
        }

        public bool TryInteract()
        {
            var passage = NearPassage(); if (passage == null) return false;
            if (!string.IsNullOrEmpty(passage.requiredFlag) && !Done(passage.requiredFlag))
            {
                game.Message(passage.requiredFlag == ArenaFlag ? "Hạ trinh sát và vệ binh để mở cổng phòng boss." : "Hạ Lavia để mở cổng tới Mea Culpa.");
                return true;
            }
            if (StartPassage(passage)) return true;
            // A real tap is a one-frame input. Keep it while the previous
            // room's short fade cooldown expires, provided the player remains
            // at this doorway. The use button is already visible in this state.
            if (game.TrialPassageCoolingDown)
            {
                pendingPassage = passage;
                game.Message("Cửa đang mở...");
            }
            else game.Message("Chưa thể đi qua cửa này.");
            return true;
        }

        bool StartPassage(TrialPassage passage, bool walking = false)
        {
            if (!game.TryTrialPassage(passage.target, passage.arrival, walking)) return false;
            pendingPassage = null;
            expectedArrivalRoom = passage.target;
            suppressedReturn = Array.Find(Passages, other => other.walkDirection != 0 && other.room == passage.target && other.target == passage.room);
            if (game.progress.SetFlag(VisitedFlag)) game.SaveGame();
            game.Sfx("SWITCH_ON");
            return true;
        }

        public void OnRoomEntered()
        {
            pendingPassage = null;
            if (expectedArrivalRoom != game.Current.id) suppressedReturn = null;
            expectedArrivalRoom = null;
            ApplySavedObjectives();
            if (GateFx != null) GateFx.Sync(BossDefeated);
            if (TrialBoss != null && TrialBoss.trialBoss != null) TrialBoss.trialBoss.NotifyRoomEntered();
            if (TrialBoss != null && TrialBoss.laviaBoss != null) TrialBoss.laviaBoss.NotifyRoomEntered();
        }

        public Vector2 ResolveSourceArrival(RoomState room, RoomDoor door, Vector2 arrival)
        {
            // The imported north spawn of S07 is embedded in its solid roof.
            // Keep that source door/ladder, but arrive on its clear upper landing.
            return room.id == "D17Z01S07" && door != null && door.key == "N" ? new Vector2(-861, -9.97f) : arrival;
        }

        public Vector2 ClampLadderStep(LadderZone ladder, Vector2 position, float vertical, out bool landed)
        {
            landed = false;
            if (!trialLadders.Contains(ladder)) return position;
            Vector2 ends = ladderLandings[ladder];
            position.y = Mathf.Clamp(position.y, ends.x, ends.y);
            landed = vertical < -.1f && position.y <= ends.x + .001f || vertical > .1f && position.y >= ends.y - .001f;
            return position;
        }

        public Vector2 LadderLandings(LadderZone ladder) => ladderLandings.TryGetValue(ladder, out var ends) ? ends : Vector2.zero;

        public void ApplySavedObjectives()
        {
            if (Done(ArenaFlag)) foreach (var enemy in ArenaEnemies) { enemy.health = 0; enemy.motor.velocity = Vector2.zero; enemy.gameObject.SetActive(false); }
            if (BossDefeated && TrialBoss != null) { TrialBoss.health = 0; TrialBoss.motor.velocity = Vector2.zero; TrialBoss.gameObject.SetActive(false); }
        }

        public void Tick(float dt)
        {
            if (pendingPassage != null)
            {
                if (NearPassage() != pendingPassage) pendingPassage = null;
                else if (!game.TrialPassageCoolingDown && !StartPassage(pendingPassage))
                {
                    pendingPassage = null;
                    game.Message("Chưa thể đi qua cửa này.");
                }
            }
            if (game.Current == null || game.player.Dead || game.InputBlocked) return;
            if (game.Current.id == "D17Z01S07" && !Done(ArenaFlag) && ArenaEnemies.Length == 2 && Array.TrueForAll(ArenaEnemies, enemy => enemy.Dead))
                Complete(ArenaFlag, 100, "Sân gác hoàn thành · cổng bên phải tới phòng boss đã mở");
        }

        public bool Strike(Func<Bounds, bool> intersects) => false;

        public void OnBossDefeated()
        {
            if (!game.progress.SetFlag(BossFlag)) return;
            // The ordinary kill pipeline already granted the boss's 150 Tears.
            // Keep its death animation visible instead of applying saved hiding.
            GateFx?.OpenWithCrumble();
            game.SaveGame(); game.Sfx("CHERUB_RESCUE"); game.Message("Lavia đã bị hạ · cổng Mea Culpa đã mở");
        }

        public void OnAltarActivated(string id)
        {
            if (game.Current.id == "D17Z01S08" && Done(ArenaFlag) && BossDefeated && !Done(AltarFlag))
                Complete(AltarFlag, 250, "Hầm thử thách hoàn thành · Mea Culpa đã được lưu");
        }

        void Complete(string flag, int tears, string message)
        {
            if (!game.progress.SetFlag(flag)) return;
            game.progress.tears += tears; ApplySavedObjectives(); game.SaveGame(); game.Sfx("CHERUB_RESCUE"); game.Message(message);
        }

        void BuildHud()
        {
            var root = new GameObject("Trial objective HUD", typeof(Canvas), typeof(CanvasScaler)); root.transform.SetParent(transform, false);
            hud = root.GetComponent<Canvas>(); hud.renderMode = RenderMode.ScreenSpaceOverlay; hud.sortingOrder = 2;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 1;
            safeArea = new GameObject("Trial HUD safe area", typeof(RectTransform)).GetComponent<RectTransform>(); safeArea.SetParent(root.transform, false);
            var label = new GameObject("Objective", typeof(RectTransform), typeof(Image)); label.transform.SetParent(safeArea, false);
            var rect = label.GetComponent<RectTransform>(); rect.anchorMin = rect.anchorMax = new Vector2(.5f, 1); rect.pivot = new Vector2(.5f, 1); rect.anchoredPosition = new Vector2(0, -96); rect.sizeDelta = new Vector2(600, 68);
            var image = label.GetComponent<Image>(); image.color = new Color(.025f, .02f, .035f, .78f); image.raycastTarget = false;
            var text = new GameObject("Text", typeof(RectTransform), typeof(Text)); text.transform.SetParent(label.transform, false);
            var tr = text.GetComponent<RectTransform>(); tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.offsetMin = new Vector2(12, 4); tr.offsetMax = new Vector2(-12, -4);
            objectiveText = text.GetComponent<Text>(); objectiveText.font = VietnameseSource.DynamicFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); objectiveText.fontSize = 18;
            objectiveText.alignment = TextAnchor.MiddleCenter; objectiveText.color = new Color(1, .88f, .6f); objectiveText.raycastTarget = false;
        }

        void LateUpdate()
        {
            if (hud == null || game.Current == null) return;
            bool branch = game.Current.id == "D17Z01S04" || game.Current.id == "D17Z01S07" || game.Current.id == "D17Z01S08" || game.Current.id == "D17Z01S09";
            var near = NearPassage(true);
            hud.enabled = (branch || near != null) && !game.player.Dead && !game.InputBlocked && game.controls.Visible;
            if (!hud.enabled) return;
            var screen = new Vector2(Screen.width, Screen.height);
            if (oldSafeArea != Screen.safeArea || oldScreen != screen)
            {
                oldSafeArea = Screen.safeArea; oldScreen = screen;
                safeArea.anchorMin = new Vector2(oldSafeArea.xMin / screen.x, oldSafeArea.yMin / screen.y);
                safeArea.anchorMax = new Vector2(oldSafeArea.xMax / screen.x, oldSafeArea.yMax / screen.y); safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            }
            string instruction = near != null ? (near.walkDirection == 0 ? "Tương tác: " : "Đi vào cổng: ") + near.label :
                game.Current.id == "D17Z01S07" ? (Done(ArenaFlag) ? "Đội gác đã bị hạ · lối đi bên phải đã mở" : "1/3 · Hạ trinh sát và vệ binh · chém sau lưng khi vệ binh phòng thủ") :
                game.Current.id == "D17Z01S09" ? (BossDefeated ? "Lavia đã bị hạ · cổng bên trái tới bàn thờ đã mở" : "2/3 · Hạ Lavia · né cú lao, cánh chém và cầu lửa") :
                game.Current.id == "D17Z01S08" ? (Done(AltarFlag) ? "Bàn Mea Culpa đã được lưu" : "3/3 · Tương tác tại bàn thờ Mea Culpa") : "Dùng cửa có sẵn để đi tới sân gác, phòng boss và bàn thờ";
            objectiveText.text = "HẦM THỬ THÁCH · " + CompletedObjectives + "/3 hoàn thành\n" + instruction;
        }
    }
}
