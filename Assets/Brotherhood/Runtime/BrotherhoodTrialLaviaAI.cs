using UnityEngine;
using UnityEngine.UI;

namespace Brotherhood
{
    // The D17 trial boss is a separate encounter. EnemyController remains the
    // authority for receiving damage, parry/hurt, reward, and death persistence.
    public sealed class BrotherhoodTrialLaviaAI : MonoBehaviour
    {
        public enum BrainState
        {
            Dormant, Alert, Hover, WingWindup, WingStrike,
            DiveWindup, Dive, CastWindup, CastRelease, Recover, Defeated
        }

        public const float WingWindupDuration = .55f;
        public const float DiveWindupDuration = .78f;
        public const float DiveSpeed = 5.1f;
        public const float WingDamage = 11f;
        public const float DiveDamage = 14f;
        public const float FireDamage = 9f;

        public BrainState State { get; private set; }
        public Vector2 Home { get; private set; }
        public bool Enraged => enemy != null && !enemy.Dead && enemy.health <= enemy.maxHealth * .5f;
        // Ordinary sword hits keep dealing damage, but only one in this window
        // can interrupt the boss. Otherwise a fast combo can prevent every attack.
        public bool CanStagger => !configured || Time.time >= nextStaggerAt;
        // Contact damage is disabled. Only the telegraphed attack frames below
        // call PlayerController.Damage, once per attack, with parry enabled.
        public bool AllowsDamage => false;
        public float DiveDirection { get; private set; }
        public float DiveRemaining { get; private set; }
        public int DiveAttacksStarted { get; private set; }
        public int WingAttacksStarted { get; private set; }
        public int DiveHits { get; private set; }
        public int WingHits { get; private set; }
        public int CastAttacksStarted { get; private set; }
        public int FireHits { get; private set; }
        public bool ProjectileActive => projectile != null;
        public int Interruptions { get; private set; }
        public int Engagements { get; private set; }
        public float DeathElapsed { get; private set; }
        public bool DefeatNotified { get; private set; }
        public bool DeathShown { get; private set; }

        BrotherhoodGame game;
        BrotherhoodTrialRoute route;
        EnemyController enemy;
        BrotherhoodTrialLaviaVisual display;
        RoomState room;
        float left, right, floorY, stateClock, recoveryDuration, attackDirection, diveEndX, nextStaggerAt;
        int attackSequence;
        bool configured, attackHit, recoveryReaction;
        GameObject projectile;
        SpriteRenderer projectileRenderer;
        Vector2 projectileDirection;
        float projectileClock;
        Color normalTint;
        Canvas hud;
        RectTransform safeArea;
        Image lifeFill;
        Image lifeLoss;
        Text title;
        Rect lastSafeArea;
        Vector2 lastScreen;

        public void Configure(BrotherhoodGame owner, BrotherhoodTrialRoute encounter,
            EnemyController actor, BrotherhoodTrialLaviaVisual visual,
            float left = -885.8f, float right = -874.4f)
        {
            game = owner;
            route = encounter;
            enemy = actor;
            display = visual;
            this.left = Mathf.Min(left, right);
            this.right = Mathf.Max(left, right);
            room = enemy == null ? null : enemy.GetComponentInParent<RoomState>(true);
            configured = game != null && route != null && enemy != null &&
                enemy.motor != null && enemy.actor != null && display != null &&
                display.IsReady && room != null && room.id == "D17Z01S09" && !enemy.boss;
            if (!configured)
            {
                Debug.LogError("Lavia trial boss needs the S09 room, visual atlas, and EnemyController");
                return;
            }

            // The enemy's transform represents its feet. Keep it in ordinary
            // sword reach while showing a broad winged silhouette above it.
            Home = enemy.transform.position;
            floorY = Home.y;
            normalTint = Color.white;
            enemy.laviaBoss = this;
            enemy.motor.gravity = 0;
            // Gravity is disabled for flight, but the motor's negative-speed
            // clamp must still permit controlled descent and the dive.
            enemy.motor.maxFall = 12f;
            BuildHud();
            ResetBrain();
        }

        public bool AppliesTo(EnemyController candidate)
            => configured && candidate == enemy && isActiveAndEnabled && !candidate.boss;

        // EnemyController.Play forwards reactions here so its old template
        // animation never makes the WheelCarrier renderer visible again.
        public float PlayReaction(string action)
        {
            if (!configured || display == null) return .35f;
            string state = action == "death" ? "Death" :
                action == "hurt" || action == "stun" || action == "parry" ? "Hurt" : "Idle";
            if (enemy.actor != null && enemy.actor.visual != null)
                enemy.actor.visual.enabled = false;
            display.Show(true);
            display.Play(state);
            if (state == "Hurt") nextStaggerAt = Time.time + 2.4f;
            return state == "Death" ? Mathf.Max(.35f, display.DeathDuration) :
                state == "Hurt" ? .45f : .35f;
        }

        public void NotifyRoomEntered()
        {
            if (!configured || enemy == null) return;
            ClearAttack();
            stateClock = 0;
            recoveryReaction = false;
            display.SetTint(normalTint);
            if (route.BossDefeated)
            {
                HideDefeated();
                return;
            }
            if (enemy.Dead || !enemy.gameObject.activeSelf)
                enemy.ResetEnemy();
            else
            {
                enemy.motor.velocity = Vector2.zero;
                enemy.motor.Teleport(new Vector2(Mathf.Clamp(enemy.transform.position.x, left, right), floorY + 1.05f));
                State = BrainState.Dormant;
                display.Show(true);
                display.Play("Idle");
            }
            if (enemy.actor != null && enemy.actor.visual != null)
                enemy.actor.visual.enabled = false;
        }

        public void ResetBrain()
        {
            if (!configured || enemy == null) return;
            ClearAttack();
            stateClock = DeathElapsed = 0;
            recoveryReaction = false;
            DiveAttacksStarted = WingAttacksStarted = DiveHits = WingHits = 0;
            CastAttacksStarted = FireHits = 0;
            Interruptions = Engagements = attackSequence = 0;
            DefeatNotified = DeathShown = false;
            recoveryDuration = .8f;
            nextStaggerAt = 0;
            enemy.motor.gravity = 0;
            enemy.motor.maxFall = 12f;
            enemy.motor.velocity = Vector2.zero;
            if (enemy.actor != null && enemy.actor.visual != null)
                enemy.actor.visual.enabled = false;
            display.SetTint(normalTint);
            if (route.BossDefeated)
            {
                HideDefeated();
                return;
            }
            State = BrainState.Dormant;
            enemy.motor.Teleport(new Vector2(Home.x, floorY + 1.05f));
            display.Show(true);
            display.SetFacing(-1);
            display.Play("Idle");
        }

        public void NotifyInterrupted()
        {
            if (!configured || enemy == null) return;
            if (enemy.Dead) { NotifyDefeated(); return; }
            if (!enemy.ControlInterrupted)
            {
                ClearAttack();
                return;
            }
            // A cast can remain in flight during ordinary recovery. A later
            // hit cancels it and refreshes recovery only once, not every tick.
            if (State == BrainState.Recover)
            {
                DestroyProjectile();
                if (recoveryReaction) return;
                recoveryReaction = true;
                Interruptions++;
                stateClock = 0;
                recoveryDuration = Enraged ? .58f : .8f;
                display.SetTint(normalTint);
                display.Play("Hurt");
                return;
            }
            if (State == BrainState.Dormant) return;
            Interruptions++;
            ClearAttack();
            recoveryDuration = Enraged ? .58f : .8f;
            ChangeState(BrainState.Recover);
            recoveryReaction = true;
            enemy.motor.velocity = Vector2.zero;
            display.SetTint(normalTint);
            display.Play("Hurt");
        }

        public void NotifyDefeated()
        {
            if (!configured || enemy == null || !enemy.Dead || DefeatNotified) return;
            DefeatNotified = true;
            DeathShown = false;
            DeathElapsed = 0;
            ClearAttack();
            State = BrainState.Defeated;
            enemy.motor.velocity = Vector2.zero;
            display.SetTint(normalTint);
            display.Play("Death");
            route.OnBossDefeated();
        }

        public void TickDecision(float dt)
        {
            if (!configured || enemy == null || dt <= 0) return;
            if (enemy.Dead)
            {
                NotifyDefeated();
                DeathElapsed += dt;
                if (!DeathShown && DeathElapsed >= Mathf.Max(.5f, display.DeathDuration))
                {
                    DeathShown = true;
                    enemy.gameObject.SetActive(false);
                }
                return;
            }
            if (game.Current != room || !gameObject.activeInHierarchy)
            {
                ClearAttack();
                enemy.motor.velocity = Vector2.zero;
                return;
            }
            if (game.player == null || game.player.Dead || game.InputBlocked || enemy.ControlInterrupted)
            {
                if (game.player == null || game.player.Dead || game.InputBlocked) ClearAttack();
                if (enemy.ControlInterrupted) NotifyInterrupted();
                enemy.motor.velocity = Vector2.zero;
                return;
            }

            UpdateProjectile(dt);
            if (game.player.Dead)
            {
                ClearAttack();
                return;
            }
            stateClock += dt;
            Vector2 delta = game.player.transform.position - enemy.transform.position;
            float distance = Mathf.Abs(delta.x);
            float direction = Mathf.Abs(delta.x) > .08f ? Mathf.Sign(delta.x) : 1f;
            switch (State)
            {
                case BrainState.Dormant:
                    FloatAt(Home.x, 1.05f, direction, dt, false);
                    if (distance <= 9f && Mathf.Abs(delta.y) < 4f && LineToPlayer())
                    {
                        Engagements++;
                        ChangeState(BrainState.Alert);
                        game.Message("LAVIA · KẺ CANH HẦM");
                    }
                    return;
                case BrainState.Alert:
                    FloatAt(enemy.transform.position.x, 1.05f, direction, dt, false);
                    if (stateClock >= .75f) ChangeState(BrainState.Hover);
                    return;
                case BrainState.Recover:
                    FloatAt(enemy.transform.position.x, .7f, direction, dt, false);
                    if (stateClock >= recoveryDuration) ChangeState(BrainState.Hover);
                    return;
                case BrainState.WingWindup:
                    FloatAt(enemy.transform.position.x, 1.05f, attackDirection, dt, false);
                    Telegraph();
                    if (stateClock >= (Enraged ? .47f : WingWindupDuration))
                    {
                        attackHit = false;
                        WingAttacksStarted++;
                        ChangeState(BrainState.WingStrike);
                        display.SetTint(normalTint);
                        display.Play("Slash", true);
                        game.Sfx("GUARDIAN_ATTACK", .65f);
                    }
                    return;
                case BrainState.WingStrike:
                    FloatAt(enemy.transform.position.x, .7f, attackDirection, dt, false);
                    if (stateClock >= .17f && stateClock <= .46f)
                        TryWingHit();
                    if (stateClock >= .58f) BeginRecovery();
                    return;
                case BrainState.DiveWindup:
                    FloatAt(enemy.transform.position.x, 1.35f, DiveDirection, dt, false);
                    Telegraph();
                    if (stateClock >= (Enraged ? .67f : DiveWindupDuration))
                    {
                        if (Mathf.Abs(diveEndX - enemy.transform.position.x) < 1f)
                        {
                            BeginRecovery();
                            return;
                        }
                        attackHit = false;
                        DiveAttacksStarted++;
                        DiveRemaining = Mathf.Abs(diveEndX - enemy.transform.position.x) / (Enraged ? 5.7f : DiveSpeed);
                        ChangeState(BrainState.Dive);
                        display.SetTint(normalTint);
                        display.Play("Dive", true);
                        game.Sfx("GUARDIAN_MOVE_TO_ATTACK", .7f);
                    }
                    return;
                case BrainState.Dive:
                    AdvanceDive(dt);
                    return;
                case BrainState.CastWindup:
                    FloatAt(enemy.transform.position.x, 1.05f, attackDirection, dt, false);
                    Telegraph();
                    if (stateClock >= (Enraged ? .62f : .76f))
                    {
                        CastAttacksStarted++;
                        ChangeState(BrainState.CastRelease);
                        display.SetTint(normalTint);
                        display.Play("Cast", true);
                        SpawnProjectile();
                    }
                    return;
                case BrainState.CastRelease:
                    FloatAt(enemy.transform.position.x, .85f, attackDirection, dt, false);
                    if (stateClock >= .5f) BeginRecovery(true);
                    return;
                case BrainState.Defeated:
                    return;
            }

            if (!LineToPlayer())
            {
                FloatAt(Home.x, 1.05f, direction, dt, true);
                return;
            }
            if (stateClock > .45f && distance <= 3.15f)
            {
                attackDirection = direction;
                ChangeState(BrainState.WingWindup);
                display.Play("Windup", true);
                game.Sfx("GUARDIAN_MOVE_TO_ATTACK", .4f);
                return;
            }
            if (stateClock > .45f && distance > 3.15f && distance <= 8.5f &&
                display.ProjectileSprites != null && display.ProjectileSprites.Length > 0 &&
                attackSequence % 3 == 2)
            {
                attackDirection = direction;
                ChangeState(BrainState.CastWindup);
                display.Play("Cast", true);
                game.Sfx("ELM_FIRE_CHARGE", .45f);
                return;
            }
            if (stateClock > .45f && distance > 3.15f && distance <= 9f &&
                (attackSequence % 2 == 0 || distance >= 5f || attackSequence % 3 == 1))
            {
                DiveDirection = direction;
                diveEndX = Mathf.Clamp(enemy.transform.position.x + direction * Mathf.Min(5.5f, distance + .8f), left, right);
                DiveRemaining = 0;
                ChangeState(BrainState.DiveWindup);
                display.Play("Windup", true);
                game.Sfx("GUARDIAN_MOVE_TO_ATTACK", .4f);
                return;
            }
            // The player can always punish the hover and recovery phases with a
            // regular sword slash. Vertical variation stays within 1.5 units.
            float targetX = Mathf.Clamp(game.player.transform.position.x - direction * 2.4f, left, right);
            FloatAt(targetX, 1.05f, direction, dt, true);
        }

        void TryWingHit()
        {
            if (attackHit || game.player == null || game.player.Dead || game.InputBlocked) return;
            Vector2 delta = game.player.transform.position - enemy.transform.position;
            if (delta.x * attackDirection < -.25f ||
                delta.x * attackDirection > 2.75f || Mathf.Abs(delta.y) > 1.6f || !LineToPlayer()) return;
            attackHit = true;
            WingHits++;
            if (game.player.Damage(WingDamage, enemy.transform.position.x, true))
                enemy.OnParried(game.player.facing);
        }

        void AdvanceDive(float dt)
        {
            float remaining = (diveEndX - enemy.transform.position.x) * DiveDirection;
            if (DiveRemaining <= 0 || remaining <= .08f)
            {
                BeginRecovery();
                return;
            }
            float speed = Enraged ? 5.7f : DiveSpeed;
            float vx = DiveDirection * Mathf.Min(speed, remaining / Mathf.Max(.001f, dt));
            // Descend visibly into the sword lane, then stay low during recovery.
            float desiredY = floorY + .55f;
            float vy = Mathf.Clamp((desiredY - enemy.transform.position.y) * 6f, -3.5f, 1.5f);
            SetBoundedVelocity(vx, vy, dt);
            if (!attackHit && game.player != null && !game.player.Dead && !game.InputBlocked)
            {
                Vector2 delta = game.player.transform.position - enemy.transform.position;
                float nextX = enemy.transform.position.x + enemy.motor.velocity.x * dt;
                float minX = Mathf.Min(enemy.transform.position.x, nextX) - .75f;
                float maxX = Mathf.Max(enemy.transform.position.x, nextX) + .75f;
                if (game.player.transform.position.x >= minX && game.player.transform.position.x <= maxX &&
                    delta.x * DiveDirection >= -.45f && Mathf.Abs(delta.y) <= 1.5f && LineToPlayer())
                {
                    attackHit = true;
                    DiveHits++;
                    if (game.player.Damage(DiveDamage, enemy.transform.position.x, true))
                        enemy.OnParried(game.player.facing);
                }
            }
            DiveRemaining = Mathf.Max(0, DiveRemaining - dt);
            if (stateClock >= 1.15f) BeginRecovery();
        }

        void SpawnProjectile()
        {
            DestroyProjectile();
            var sprites = display.ProjectileSprites;
            if (sprites == null || sprites.Length == 0) return;
            Vector2 start = (Vector2)enemy.transform.position + new Vector2(attackDirection * .9f, .55f);
            Vector2 target = (Vector2)game.player.transform.position + Vector2.up * .65f;
            projectileDirection = (target - start).normalized;
            if (Mathf.Abs(projectileDirection.x) < .2f)
                projectileDirection = new Vector2(attackDirection, 0);
            projectile = new GameObject("Lavia source fireball", typeof(SpriteRenderer));
            projectile.transform.SetParent(room.transform, true);
            projectile.transform.position = start;
            projectileRenderer = projectile.GetComponent<SpriteRenderer>();
            projectileRenderer.sprite = sprites[0];
            projectileRenderer.sortingLayerName = "Player";
            projectileRenderer.sortingOrder = 3;
            projectileRenderer.flipX = attackDirection > 0;
            projectileClock = 0;
            game.Sfx("ELM_FIRE_SHOT", .7f);
        }

        void UpdateProjectile(float dt)
        {
            if (projectile == null || projectileRenderer == null) return;
            projectileClock += dt;
            var sprites = display.ProjectileSprites;
            if (sprites != null && sprites.Length > 0)
                projectileRenderer.sprite = sprites[Mathf.FloorToInt(projectileClock * 12f) % sprites.Length];
            Vector2 from = projectile.transform.position;
            Vector2 to = from + projectileDirection * (Enraged ? 6.3f : 5.4f) * dt;
            if (Physics2D.Linecast(from, to, 1 << 8).collider != null ||
                to.x < left - 1f || to.x > right + 1f ||
                to.y < floorY - .2f || to.y > floorY + 3f || projectileClock > 2.4f)
            {
                DestroyProjectile();
                return;
            }
            if (game.player != null && !game.player.Dead && !game.InputBlocked)
            {
                Vector2 playerCenter = (Vector2)game.player.transform.position + Vector2.up * .6f;
                Vector2 nearest = Vector2.Lerp(from, to,
                    Mathf.Clamp01(Vector2.Dot(playerCenter - from, to - from) / Mathf.Max((to - from).sqrMagnitude, .0001f)));
                if (Vector2.Distance(playerCenter, nearest) < .65f)
                {
                    FireHits++;
                    if (game.player.Damage(FireDamage, from.x, true))
                        game.Sfx("PENITENT_PARRY_HIT", .55f);
                    game.effects.Burst(to, new Color(1f, .47f, .08f));
                    DestroyProjectile();
                    return;
                }
            }
            projectile.transform.position = to;
        }

        void DestroyProjectile()
        {
            if (projectile != null) Destroy(projectile);
            projectile = null;
            projectileRenderer = null;
            projectileClock = 0;
        }

        void FloatAt(float targetX, float height, float lookDirection, float dt, bool move)
        {
            display.SetFacing(lookDirection);
            if (enemy.actor != null) enemy.actor.Face(lookDirection);
            float x = enemy.transform.position.x;
            float y = enemy.transform.position.y;
            float desiredX = move ? Mathf.Clamp((targetX - x) * 2.2f, -(Enraged ? 3f : 2.35f), Enraged ? 3f : 2.35f) : 0;
            float bob = Mathf.Sin(Time.time * 2.5f) * .13f;
            float desiredY = Mathf.Clamp((floorY + height + bob - y) * 4f, -1.6f, 1.6f);
            float vx = Mathf.MoveTowards(enemy.motor.velocity.x, desiredX, 8f * dt);
            float vy = Mathf.MoveTowards(enemy.motor.velocity.y, desiredY, 8f * dt);
            SetBoundedVelocity(vx, vy, dt);
            if (State == BrainState.Hover || State == BrainState.Dormant || State == BrainState.Alert)
                display.Play("Idle");
        }

        void SetBoundedVelocity(float vx, float vy, float dt)
        {
            Vector2 pos = enemy.transform.position;
            float nextX = Mathf.Clamp(pos.x + vx * dt, left, right);
            float nextY = Mathf.Clamp(pos.y + vy * dt, floorY + .42f, floorY + 1.75f);
            enemy.motor.velocity = new Vector2((nextX - pos.x) / Mathf.Max(dt, .001f),
                (nextY - pos.y) / Mathf.Max(dt, .001f));
        }

        bool LineToPlayer()
        {
            if (enemy == null || game == null || game.player == null) return false;
            Vector2 eye = (Vector2)enemy.transform.position + Vector2.up * .55f;
            Vector2 target = (Vector2)game.player.transform.position + Vector2.up * game.player.motor.size.y * .6f;
            return Physics2D.Linecast(eye, target, 1 << 8).collider == null;
        }

        void Telegraph()
        {
            float flash = .45f + Mathf.Sin(stateClock * 19f) * .2f;
            display.SetTint(Color.Lerp(normalTint, new Color(1f, .46f, .19f), flash));
        }

        void BeginRecovery(bool keepProjectile = false)
        {
            if (keepProjectile)
            {
                DiveRemaining = 0;
                DiveDirection = 0;
                attackHit = false;
                enemy.motor.velocity = Vector2.zero;
            }
            else ClearAttack();
            recoveryDuration = Enraged ? .62f : .9f;
            attackSequence++;
            ChangeState(BrainState.Recover);
            display.SetTint(normalTint);
            display.Play("Idle");
        }

        void ClearAttack()
        {
            DestroyProjectile();
            DiveRemaining = 0;
            DiveDirection = 0;
            attackHit = false;
            if (enemy != null && enemy.motor != null)
                enemy.motor.velocity = Vector2.zero;
        }

        void ChangeState(BrainState next)
        {
            if (State == next) return;
            State = next;
            stateClock = 0;
        }

        void HideDefeated()
        {
            DefeatNotified = DeathShown = true;
            State = BrainState.Defeated;
            enemy.health = 0;
            enemy.motor.velocity = Vector2.zero;
            if (enemy.actor != null && enemy.actor.visual != null)
                enemy.actor.visual.enabled = false;
            display.Show(false);
            enemy.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            if (!configured || enemy == null) return;
            ClearAttack();
            if (!enemy.Dead)
            {
                State = BrainState.Dormant;
                stateClock = 0;
                display.SetTint(normalTint);
            }
        }

        void BuildHud()
        {
            if (hud != null) return;
            var root = new GameObject("Lavia boss health", typeof(Canvas), typeof(CanvasScaler));
            root.transform.SetParent(transform, false);
            hud = root.GetComponent<Canvas>();
            hud.renderMode = RenderMode.ScreenSpaceOverlay;
            hud.sortingOrder = 4;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 1;
            safeArea = Rect(root.transform, "Safe area", Vector2.zero, Vector2.zero);
            var panel = Rect(safeArea, "Boss health", new Vector2(0, 55), new Vector2(538, 68));
            panel.anchorMin = panel.anchorMax = new Vector2(.5f, 0);
            var background = panel.gameObject.AddComponent<Image>();
            background.color = new Color(.025f, .018f, .025f, .67f);
            background.raycastTarget = false;
            var bar = Rect(panel, "Life", new Vector2(0, -10), new Vector2(432, 10));
            var track = bar.gameObject.AddComponent<Image>();
            track.color = new Color(.15f, .075f, .085f);
            track.raycastTarget = false;
            var loss = Rect(bar, "Recent damage", Vector2.zero, Vector2.zero);
            loss.anchorMin = Vector2.zero;
            loss.anchorMax = Vector2.one;
            loss.offsetMin = loss.offsetMax = Vector2.zero;
            lifeLoss = loss.gameObject.AddComponent<Image>();
            lifeLoss.color = new Color(.88f, .65f, .32f);
            lifeLoss.raycastTarget = false;
            var fill = Rect(bar, "Fill", Vector2.zero, Vector2.zero);
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            lifeFill = fill.gameObject.AddComponent<Image>();
            lifeFill.color = new Color(.76f, .105f, .095f);
            lifeFill.raycastTarget = false;
            // Reuse the game's boss frame so the trial boss matches the other
            // encounters. The track sits behind its transparent centre.
            var frame = Rect(panel, "Source boss frame", new Vector2(0, -10), new Vector2(520, 45));
            var frameImage = frame.gameObject.AddComponent<Image>();
            // This imported sheet is SpriteMode.Multiple, so load its full
            // texture as the original HUD does instead of requesting a Sprite.
            var frameTexture = Resources.Load<Texture2D>("UI/Sprites/inventory-spritesheet_72");
            if (frameTexture != null)
            {
                frameTexture.filterMode = FilterMode.Point;
                frameImage.sprite = Sprite.Create(frameTexture,
                    new Rect(0, 0, frameTexture.width, frameTexture.height),
                    new Vector2(.5f, .5f), 100);
            }
            frameImage.preserveAspect = true;
            frameImage.color = Color.white;
            frameImage.raycastTarget = false;
            var label = Rect(panel, "Name and health", new Vector2(0, 23), new Vector2(520, 28));
            title = label.gameObject.AddComponent<Text>();
            title.font = VietnameseSource.DynamicFont ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.fontSize = 17;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = new Color(1f, .84f, .49f);
            title.raycastTarget = false;
            var outline = label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(.02f, .01f, .015f, .95f);
            outline.effectDistance = new Vector2(1, -1);
        }

        void LateUpdate()
        {
            if (!configured || hud == null || game.player == null) return;
            hud.enabled = game.Current == room && !enemy.Dead && !game.player.Dead &&
                State != BrainState.Dormant && !game.InputBlocked &&
                game.controls != null && game.controls.Visible;
            if (!hud.enabled) return;
            Vector2 screen = new Vector2(Screen.width, Screen.height);
            if (screen.x > 0 && screen.y > 0 && (lastSafeArea != Screen.safeArea || lastScreen != screen))
            {
                lastSafeArea = Screen.safeArea;
                lastScreen = screen;
                safeArea.anchorMin = new Vector2(lastSafeArea.xMin / screen.x, lastSafeArea.yMin / screen.y);
                safeArea.anchorMax = new Vector2(lastSafeArea.xMax / screen.x, lastSafeArea.yMax / screen.y);
                safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
            }
            float fraction = Mathf.Clamp01(enemy.health / Mathf.Max(1f, enemy.maxHealth));
            lifeFill.rectTransform.anchorMax = new Vector2(fraction, 1);
            float recent = Mathf.Max(fraction, Mathf.MoveTowards(lifeLoss.rectTransform.anchorMax.x,
                fraction, Time.unscaledDeltaTime * .35f));
            lifeLoss.rectTransform.anchorMax = new Vector2(recent, 1);
            lifeFill.color = Enraged ? new Color(.95f, .31f, .09f) : new Color(.76f, .105f, .095f);
            int remaining = Mathf.Clamp(Mathf.CeilToInt(enemy.health), 0, Mathf.CeilToInt(enemy.maxHealth));
            int maximum = Mathf.CeilToInt(enemy.maxHealth);
            title.text = Enraged
                ? $"LAVIA · CUỒNG NỘ    {remaining} / {maximum}"
                : $"LAVIA    {remaining} / {maximum}";
        }

        static RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            return rect;
        }
    }
}
