using UnityEngine;
using UnityEngine.UI;

namespace Brotherhood
{
    // Custom S09 encounter. EnemyController keeps normal damage, Hurt, parry,
    // Godmode and the once-only kill reward; this actor is never the Warden.
    public sealed class BrotherhoodTrialBossAI : MonoBehaviour
    {
        public enum BrainState { Dormant, Alert, Reposition, MeleeWindup, Melee, ComboGap, ChargeWindup, Charging, Recover, Defeated }
        public const float MeleeWindupDuration = .35f;
        public const float ChargeWindupDuration = .8f;
        public const float ChargeSpeed = 5.2f;

        public BrainState State { get; private set; }
        public Vector2 Home { get; private set; }
        public bool Enraged => enemy != null && !enemy.Dead && enemy.health <= enemy.maxHealth * .5f;
        public float ChargeDirection { get; private set; }
        public float ChargeRemaining { get; private set; }
        public float MeleeElapsed { get; private set; }
        public int ComboRemaining { get; private set; }
        public int MeleeAttacksStarted { get; private set; }
        public int ChargesStarted { get; private set; }
        public int ChargeHits { get; private set; }
        public int Interruptions { get; private set; }
        public int Engagements { get; private set; }
        public float DeathElapsed { get; private set; }
        public bool DefeatNotified { get; private set; }
        public bool DeathShown { get; private set; }
        public bool AllowsDamage => State == BrainState.Melee && enemy != null && enemy.AttackInProgress && MeleeElapsed >= .4999f && MeleeElapsed < .7f || State == BrainState.Charging && !chargeHit;

        BrotherhoodGame game;
        BrotherhoodTrialRoute route;
        EnemyController enemy;
        RoomState room;
        float left, right, stateClock, recoveryDuration, attackDirection, chargeEndX;
        bool configured, chargeHit;
        Color originalTint;
        Canvas hud;
        RectTransform safeArea;
        Image lifeFill;
        Text title;
        Rect lastSafeArea;
        Vector2 lastScreen;

        public void Configure(BrotherhoodGame owner, BrotherhoodTrialRoute encounter, EnemyController actor, float left = -886.5f, float right = -874f)
        {
            game = owner; route = encounter; enemy = actor;
            this.left = Mathf.Min(left, right); this.right = Mathf.Max(left, right);
            configured = game != null && route != null && enemy != null && !enemy.boss && enemy.family == "wheelcarrier" && enemy.motor != null && enemy.actor != null;
            if (!configured) return;
            room = enemy.GetComponentInParent<RoomState>(true);
            configured = room != null && room.id == "D17Z01S09";
            if (!configured) return;
            Home = enemy.transform.position; originalTint = enemy.actor.visual.color;
            enemy.trialBoss = this;
            BuildHud(); ResetBrain();
        }

        public bool AppliesTo(EnemyController candidate) => configured && candidate != null && candidate == enemy && isActiveAndEnabled && !candidate.boss;

        public void NotifyRoomEntered()
        {
            if (!configured || enemy == null) return;
            CancelPending(); stateClock = 0; enemy.actor.visual.color = originalTint;
            if (route.BossDefeated)
            {
                DefeatNotified = DeathShown = true; State = BrainState.Defeated;
                enemy.health = 0; enemy.motor.velocity = Vector2.zero;
                enemy.actor.visual.enabled = false; enemy.gameObject.SetActive(false);
                return;
            }
            // Loading an older save can remove the defeat flag while the runtime
            // actor still belongs to the newer, completed timeline. Live actors
            // keep their current Life when the player merely returns to S09.
            if (enemy.Dead || !enemy.gameObject.activeSelf) enemy.ResetEnemy();
            State = BrainState.Dormant;
            if (!enemy.Dead) enemy.actor.visual.enabled = true;
        }

        public void ResetBrain()
        {
            if (!configured || enemy == null) return;
            CancelPending(); stateClock = DeathElapsed = 0;
            MeleeAttacksStarted = ChargesStarted = ChargeHits = Interruptions = Engagements = 0;
            DefeatNotified = DeathShown = false;
            recoveryDuration = 1f; State = BrainState.Dormant;
            enemy.actor.visual.color = originalTint;
            if (route.BossDefeated)
            {
                DefeatNotified = DeathShown = true; State = BrainState.Defeated;
                enemy.health = 0; enemy.motor.velocity = Vector2.zero;
                enemy.actor.visual.enabled = false; enemy.gameObject.SetActive(false);
                return;
            }
            enemy.TrialBossMove(-1, 0);
        }

        public void NotifyInterrupted()
        {
            if (!configured || enemy == null) return;
            if (enemy.Dead) { NotifyDefeated(); return; }
            if (State != BrainState.Recover && State != BrainState.Dormant) Interruptions++;
            CancelPending(); recoveryDuration = Enraged ? .55f : .8f;
            ChangeState(BrainState.Recover);
            enemy.actor.visual.color = originalTint;
        }

        public void NotifyDefeated()
        {
            if (!configured || enemy == null || !enemy.Dead || DefeatNotified) return;
            DefeatNotified = true; DeathElapsed = 0; DeathShown = false;
            CancelPending(); ChangeState(BrainState.Defeated);
            enemy.motor.velocity = Vector2.zero; enemy.actor.visual.color = originalTint;
            route.OnBossDefeated();
        }

        public void TickDecision(float dt)
        {
            if (!configured || enemy == null || dt <= 0) return;
            if (enemy.Dead)
            {
                NotifyDefeated(); DeathElapsed += dt;
                float duration = enemy.actor.catalog.Find("WheelCarrier_death")?.duration ?? 1.84f;
                if (!DeathShown && DeathElapsed >= duration)
                { DeathShown = true; enemy.gameObject.SetActive(false); }
                return;
            }
            if (game.Current != room || !gameObject.activeInHierarchy) { CancelPending(); return; }
            if (game.player == null || game.player.Dead || game.InputBlocked || enemy.ControlInterrupted)
            { NotifyInterrupted(); return; }
            stateClock += dt;
            if (enemy.AttackInProgress)
            {
                if (State == BrainState.Melee) MeleeElapsed += dt;
                return;
            }
            Vector2 delta = game.player.transform.position - enemy.transform.position;
            float distance = Mathf.Abs(delta.x), direction = Mathf.Abs(delta.x) > .01f ? Mathf.Sign(delta.x) : enemy.Facing;
            bool visible = Mathf.Abs(delta.y) < 2.2f && LineToPlayer();
            if (State == BrainState.Melee)
            {
                MeleeElapsed = 0;
                if (ComboRemaining > 1) { ComboRemaining--; ChangeState(BrainState.ComboGap); }
                else BeginRecovery();
                return;
            }
            switch (State)
            {
                case BrainState.Dormant:
                    Stand(direction);
                    if (visible && distance <= 9f) { Engagements++; ChangeState(BrainState.Alert); game.Message("Káºº CANH Háº¦M"); }
                    return;
                case BrainState.Alert:
                    Stand(direction);
                    if (stateClock >= 1f) ChangeState(BrainState.Reposition);
                    return;
                case BrainState.Recover:
                    Stand(direction);
                    if (stateClock >= recoveryDuration && enemy.DecisionReady) ChangeState(BrainState.Reposition);
                    return;
                case BrainState.MeleeWindup:
                    Stand(attackDirection); Telegraph();
                    if (!visible) { BeginRecovery(); return; }
                    if (stateClock >= MeleeWindupDuration && enemy.DecisionReady) StartMelee(attackDirection);
                    return;
                case BrainState.ComboGap:
                    Stand(direction);
                    if (!visible || distance > 3.2f) { BeginRecovery(); return; }
                    if (stateClock >= .25f && enemy.DecisionReady) StartMelee(direction);
                    return;
                case BrainState.ChargeWindup:
                    Stand(ChargeDirection); Telegraph();
                    if (stateClock >= ChargeWindupDuration)
                    {
                        if (!PathClear(ChargeDirection) || Mathf.Abs(chargeEndX - enemy.transform.position.x) < .25f) { BeginRecovery(); return; }
                        chargeHit = false; ChargesStarted++; ChargeRemaining = Mathf.Abs(chargeEndX - enemy.transform.position.x) / ChargeSpeed;
                        ChangeState(BrainState.Charging); enemy.actor.visual.color = originalTint;
                    }
                    return;
                case BrainState.Charging:
                    AdvanceCharge(dt);
                    return;
                case BrainState.Defeated:
                    return;
            }
            enemy.actor.visual.color = originalTint;
            if (!visible) { Stand(direction); return; }
            if (distance > 4.1f && (MeleeAttacksStarted == 0 || (MeleeAttacksStarted + ChargesStarted) % 2 == 0))
            {
                ChargeDirection = direction;
                chargeEndX = Mathf.Clamp(enemy.transform.position.x + direction * Mathf.Min(5f, distance + .8f), left, right);
                ChargeRemaining = 0; ChangeState(BrainState.ChargeWindup); Stand(direction); return;
            }
            if (distance > 2.45f)
            {
                if (PathClear(direction)) enemy.TrialBossMove(direction, Enraged ? 1.65f : 1.3f);
                else Stand(direction);
                return;
            }
            Stand(direction);
            if (enemy.DecisionReady)
            {
                attackDirection = direction; ComboRemaining = Enraged ? 2 : 1;
                ChangeState(BrainState.MeleeWindup);
            }
        }

        public bool CanStrikePlayer()
        {
            if (!configured || enemy == null || game.player == null || game.player.Dead || game.InputBlocked || State != BrainState.Melee || !AllowsDamage || !LineToPlayer()) return false;
            Vector2 delta = game.player.transform.position - enemy.transform.position;
            return delta.x * enemy.Facing >= -.15f && Mathf.Abs(delta.y) < 1.6f;
        }

        void StartMelee(float direction)
        {
            if (!enemy.TrialBossAttack(direction)) return;
            MeleeElapsed = 0; MeleeAttacksStarted++; enemy.actor.visual.color = originalTint;
            ChangeState(BrainState.Melee);
        }

        void AdvanceCharge(float dt)
        {
            float remainingDistance = (chargeEndX - enemy.transform.position.x) * ChargeDirection;
            if (ChargeRemaining <= 0 || remainingDistance <= .08f || !PathClear(ChargeDirection)) { BeginRecovery(); return; }
            float speed = Mathf.Min(ChargeSpeed, remainingDistance / Mathf.Max(.0001f, dt));
            enemy.TrialBossMove(ChargeDirection, speed);
            float nextX = enemy.transform.position.x + ChargeDirection * speed * dt;
            Vector2 player = game.player.transform.position;
            float from = Mathf.Min(enemy.transform.position.x, nextX) - 1.05f, to = Mathf.Max(enemy.transform.position.x, nextX) + 1.05f;
            if (!chargeHit && player.x >= from && player.x <= to && (player.x - enemy.transform.position.x) * ChargeDirection >= -.15f && Mathf.Abs(player.y - enemy.transform.position.y) < 1.3f && LineToPlayer())
            {
                chargeHit = true; ChargeHits++;
                bool parried = game.player.Damage(14f, enemy.transform.position.x, true);
                if (parried) enemy.OnParried(game.player.facing);
                if (!enemy.Hurt && !enemy.Dead) BeginRecovery();
                return;
            }
            ChargeRemaining = Mathf.Max(0, ChargeRemaining - dt);
        }

        bool LineToPlayer()
        {
            if (game == null || game.player == null) return false;
            Vector2 eye = (Vector2)enemy.transform.position + Vector2.up * enemy.motor.size.y * .7f;
            Vector2 target = (Vector2)game.player.transform.position + Vector2.up * game.player.motor.size.y * .65f;
            return Physics2D.Linecast(eye, target, 1 << 8).collider == null;
        }

        bool PathClear(float direction)
        {
            Vector2 feet = enemy.transform.position;
            if (feet.x + direction * .65f < left || feet.x + direction * .65f > right) return false;
            var floor = Physics2D.Raycast(feet + new Vector2(direction * .7f, .4f), Vector2.down, 1.25f, (1 << 8) | (1 << 9));
            return floor.collider != null && floor.normal.y > .6f && Physics2D.Raycast(feet + Vector2.up * .75f, Vector2.right * direction, .8f, 1 << 8).collider == null;
        }

        void BeginRecovery()
        {
            CancelPending(); recoveryDuration = Enraged ? .6f : 1f;
            ChangeState(BrainState.Recover); enemy.actor.visual.color = originalTint;
            Stand(enemy.Facing);
        }

        void CancelPending()
        {
            ChargeRemaining = MeleeElapsed = 0; ChargeDirection = 0; ComboRemaining = 0; chargeHit = false;
            if (enemy == null) return;
            enemy.TrialBossCancelAttack(); enemy.motor.velocity.x = 0;
        }

        void Stand(float direction) => enemy.TrialBossMove(direction, 0);
        void Telegraph() => enemy.actor.visual.color = Color.Lerp(originalTint, new Color(1f, .72f, .28f), .45f + Mathf.Sin(stateClock * 22f) * .25f);
        void ChangeState(BrainState next) { if (State == next) return; State = next; stateClock = 0; }
        void OnDisable()
        {
            if (!configured) return;
            CancelPending();
            if (enemy != null && !enemy.Dead) { State = BrainState.Dormant; stateClock = 0; enemy.actor.visual.color = originalTint; }
        }

        void BuildHud()
        {
            if (hud != null) return;
            var root = new GameObject("Trial boss Life", typeof(Canvas), typeof(CanvasScaler)); root.transform.SetParent(transform, false);
            hud = root.GetComponent<Canvas>(); hud.renderMode = RenderMode.ScreenSpaceOverlay; hud.sortingOrder = 4;
            var scaler = root.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1280, 720); scaler.matchWidthOrHeight = 1;
            safeArea = Rect(root.transform, "Safe area", Vector2.zero, Vector2.zero);
            var panel = Rect(safeArea, "Boss health", new Vector2(0, 46), new Vector2(450, 46)); panel.anchorMin = panel.anchorMax = new Vector2(.5f, 0);
            var background = panel.gameObject.AddComponent<Image>(); background.color = new Color(.05f, .035f, .025f, .9f); background.raycastTarget = false;
            var bar = Rect(panel, "Life", new Vector2(0, -9), new Vector2(426, 12));
            var track = bar.gameObject.AddComponent<Image>(); track.color = new Color(.22f, .16f, .12f); track.raycastTarget = false;
            var fill = Rect(bar, "Fill", Vector2.zero, Vector2.zero); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one; fill.offsetMin = fill.offsetMax = Vector2.zero;
            lifeFill = fill.gameObject.AddComponent<Image>(); lifeFill.color = new Color(.72f, .12f, .08f); lifeFill.raycastTarget = false;
            var label = Rect(panel, "Name", new Vector2(0, 10), new Vector2(438, 24)); title = label.gameObject.AddComponent<Text>();
            title.font = VietnameseSource.DynamicFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); title.fontSize = 16; title.alignment = TextAnchor.MiddleCenter; title.color = new Color(1f, .83f, .46f); title.raycastTarget = false;
        }

        void LateUpdate()
        {
            if (!configured || hud == null || game.player == null) return;
            hud.enabled = game.Current == room && !enemy.Dead && !game.player.Dead && State != BrainState.Dormant && !game.InputBlocked && game.controls != null && game.controls.Visible;
            if (!hud.enabled) return;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (screen.x > 0 && screen.y > 0 && (lastSafeArea != Screen.safeArea || lastScreen != screen))
            {
                lastSafeArea = Screen.safeArea; lastScreen = screen;
                safeArea.anchorMin = new Vector2(lastSafeArea.xMin / screen.x, lastSafeArea.yMin / screen.y); safeArea.anchorMax = new Vector2(lastSafeArea.xMax / screen.x, lastSafeArea.yMax / screen.y); safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            }
            lifeFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(enemy.health / Mathf.Max(1, enemy.maxHealth)), 1);
            lifeFill.color = Enraged ? new Color(.9f, .32f, .07f) : new Color(.72f, .12f, .08f);
            title.text = Enraged ? "Káºº CANH Háº¦M Â· CUá»NG Ná»" : "Káºº CANH Háº¦M";
        }

        static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = position; rect.sizeDelta = size; return rect;
        }
    }
}
