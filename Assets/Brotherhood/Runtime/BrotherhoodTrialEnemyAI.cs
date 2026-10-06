using UnityEngine;

namespace Brotherhood
{
    // Custom trial behaviour, attached only to the actors spawned by the D17
    // branch. The source enemy controller still owns attacks, hurt and death.
    public sealed class BrotherhoodTrialEnemyAI : MonoBehaviour
    {
        public enum Kind { Scout, Guard }
        public enum BrainState { Patrol, Alert, Chase, Search, Return, Reposition, Guard, Retreat, Counter, Recover, Defeated }

        public Kind EnemyKind { get; private set; }
        public BrainState State { get; private set; }
        public Vector2 Home { get; private set; }
        public Vector2 LastSeen { get; private set; }
        public bool PlayerVisible { get; private set; }
        public float MemoryRemaining => memory;
        public int GuardedHits { get; private set; }
        public int AlertsRaised { get; private set; }

        const float MemoryDuration = 2.4f;
        const float SightRange = 9f;
        const float LeashRange = 12f;
        const int TerrainMask = 1 << 8;
        const int FloorMask = (1 << 8) | (1 << 9);
        const float MaxRise = .65f;
        const float MaxDrop = 1f;

        EnemyController enemy;
        readonly RaycastHit2D[] floorHits = new RaycastHit2D[12];
        readonly RaycastHit2D[] wallHits = new RaycastHit2D[12];
        float patrolRadius, initialFacing, patrolDirection;
        float memory, stateClock, actionCooldown, patrolPause, retreatStart;
        bool configured, attackObserved, guardConsumed;

        public void Configure(EnemyController owner, Kind kind, float patrolHalfWidth = 3f, float startFacing = -1f)
        {
            enemy = owner;
            EnemyKind = kind;
            patrolRadius = Mathf.Max(.5f, patrolHalfWidth);
            initialFacing = startFacing < 0 ? -1f : 1f;
            configured = enemy != null && enemy.motor != null && enemy.actor != null && !enemy.boss && enemy.family == (kind == Kind.Scout ? "acolyte" : "flagellant");
            if (!configured) return;
            Home = enemy.transform.position;
            enemy.trialAI = this;
            ResetBrain();
        }

        public bool AppliesTo(EnemyController candidate) => configured && candidate != null && enemy == candidate && isActiveAndEnabled && !candidate.boss;

        public void ResetBrain()
        {
            if (!configured || enemy == null) return;
            LastSeen = Home;
            memory = stateClock = actionCooldown = patrolPause = 0;
            retreatStart = Home.x;
            patrolDirection = initialFacing;
            PlayerVisible = attackObserved = guardConsumed = false;
            GuardedHits = AlertsRaised = 0;
            State = BrainState.Patrol;
            enemy.TrialMove(initialFacing, 0);
        }

        public void NotifyInterrupted()
        {
            if (!configured || enemy == null) return;
            attackObserved = guardConsumed = false;
            PlayerVisible = false;
            actionCooldown = Mathf.Max(actionCooldown, .55f);
            if (enemy.Dead) { ChangeState(BrainState.Defeated); memory = 0; return; }
            ChangeState(memory > 0 ? (EnemyKind == Kind.Scout ? BrainState.Chase : BrainState.Reposition) : BrainState.Return);
        }

        public void TickDecision(float dt)
        {
            if (!AppliesTo(enemy) || enemy.game == null || enemy.game.player == null || dt <= 0) return;
            var player = enemy.game.player;
            stateClock += dt;
            actionCooldown = Mathf.Max(0, actionCooldown - dt);
            patrolPause = Mathf.Max(0, patrolPause - dt);
            PlayerVisible = SeePlayer();
            if (PlayerVisible) { LastSeen = player.transform.position; memory = MemoryDuration; }
            else memory = Mathf.Max(0, memory - dt);

            // Source state 1/2 advances its original attack and recovery. Never
            // replace its animation or motion while those states are running.
            if (enemy.AttackInProgress) { attackObserved = true; return; }
            if (attackObserved) { attackObserved = false; actionCooldown = Mathf.Max(actionCooldown, .65f); ChangeState(BrainState.Recover); }
            if (enemy.game.InputBlocked) { Stand(); return; }
            if (State == BrainState.Defeated) return;

            if ((State == BrainState.Patrol || State == BrainState.Return) && PlayerVisible && Mathf.Abs(enemy.transform.position.x - Home.x) < LeashRange - .5f)
                ChangeState(BrainState.Alert);
            if (State == BrainState.Alert)
            {
                Stand(FacingLastSeen());
                if (stateClock >= .45f)
                {
                    RaiseAlert();
                    ChangeState(EnemyKind == Kind.Scout ? BrainState.Chase : BrainState.Reposition);
                }
                return;
            }
            if (State == BrainState.Recover)
            {
                Stand(FacingLastSeen());
                if (stateClock >= .5f) ChangeState(memory > 0 ? (EnemyKind == Kind.Scout ? BrainState.Chase : BrainState.Reposition) : BrainState.Search);
                return;
            }
            if (State == BrainState.Patrol) { Patrol(); return; }
            if (State == BrainState.Return) { ReturnHome(); return; }
            if (State == BrainState.Search) { Search(); return; }

            if (!PlayerVisible)
            {
                MoveToLastSeen();
                return;
            }
            if (Mathf.Abs(enemy.transform.position.x - Home.x) > LeashRange) { ChangeState(BrainState.Return); Stand(); return; }
            if (EnemyKind == Kind.Scout) ScoutCombat();
            else GuardCombat();
        }

        bool SeePlayer()
        {
            Vector2 target = enemy.game.player.transform.position;
            Vector2 delta = target - (Vector2)enemy.transform.position;
            if (Mathf.Abs(target.x - Home.x) > LeashRange || Mathf.Abs(delta.y) > 3.5f || delta.sqrMagnitude > SightRange * SightRange) return false;
            if ((State == BrainState.Patrol || State == BrainState.Return) && delta.magnitude > 1.6f)
            {
                if (delta.x * enemy.Facing <= 0 || Mathf.Atan2(Mathf.Abs(delta.y), Mathf.Abs(delta.x)) * Mathf.Rad2Deg > 55f) return false;
            }
            return LineToPlayer();
        }

        Vector2 Eye(EnemyController actor) => (Vector2)actor.transform.position + Vector2.up * Mathf.Clamp(actor.motor.size.y * .75f, .6f, 1.4f);

        bool LineToPlayer()
        {
            var player = enemy.game.player;
            Vector2 target = (Vector2)player.transform.position + Vector2.up * player.motor.size.y * .65f;
            return Physics2D.Linecast(Eye(enemy), target, TerrainMask).collider == null;
        }

        public bool CanStrikePlayer()
        {
            if (!AppliesTo(enemy) || enemy.game == null || enemy.game.player == null || enemy.game.InputBlocked || enemy.game.player.Dead) return false;
            Vector2 delta = enemy.game.player.transform.position - enemy.transform.position;
            return delta.x * enemy.Facing >= -.1f && Mathf.Abs(delta.y) < 1.6f && LineToPlayer();
        }

        public bool TryGuardHit(float amount)
        {
            if (!AppliesTo(enemy) || EnemyKind != Kind.Guard || State != BrainState.Guard || guardConsumed || stateClock < .12f || amount <= 0 || !enemy.DecisionReady || enemy.game == null || enemy.game.player == null) return false;
            var player = enemy.game.player;
            if (enemy.game.controls != null && enemy.game.controls.debugUI != null && enemy.game.controls.debugUI.GodMode) return false;
            if (player.HeavyRewardContext || player.PrayerCasting || player.attackTime <= 0) return false;
            Vector2 delta = player.transform.position - enemy.transform.position;
            if (delta.x * enemy.Facing < 0 || Mathf.Abs(delta.x) > 2.5f || Mathf.Abs(delta.y) > 1.4f || !LineToPlayer()) return false;
            guardConsumed = true;
            GuardedHits++;
            ChangeState(BrainState.Counter);
            enemy.actor.Play("NewFlagellant_parry_reaction_anim", false, true);
            enemy.game.Sfx("PENITENT_PARRY_HIT", .7f);
            if (enemy.game.effects != null) enemy.game.effects.Burst(enemy.transform.position + Vector3.up, new Color(1f, .8f, .3f));
            return true;
        }

        void Patrol()
        {
            if (patrolPause > 0) { Stand(patrolDirection); return; }
            float dx = enemy.transform.position.x - Home.x;
            if (dx * patrolDirection >= patrolRadius || !PathClear(patrolDirection))
            {
                patrolDirection = -patrolDirection;
                patrolPause = .35f;
                Stand(patrolDirection);
                return;
            }
            Move(patrolDirection, EnemyKind == Kind.Scout ? .8f : .55f);
        }

        void ScoutCombat()
        {
            float dx = LastSeen.x - enemy.transform.position.x;
            float direction = FacingLastSeen();
            if (Mathf.Abs(dx) <= 1.75f && Mathf.Abs(LastSeen.y - enemy.transform.position.y) <= 1.2f)
            {
                Stand(direction);
                StartAttack(direction);
            }
            else Move(direction, 2.1f);
        }

        void GuardCombat()
        {
            var player = enemy.game.player;
            float dx = LastSeen.x - enemy.transform.position.x;
            float distance = Mathf.Abs(dx);
            float direction = FacingLastSeen();
            switch (State)
            {
                case BrainState.Reposition:
                    if (distance < 1.3f || (enemy.health < enemy.maxHealth * .45f && distance < 3.2f))
                    { retreatStart = enemy.transform.position.x; ChangeState(BrainState.Retreat); break; }
                    if (distance > 2.65f) { Move(direction, 1.3f); return; }
                    if (distance < 1.85f) { retreatStart = enemy.transform.position.x; ChangeState(BrainState.Retreat); break; }
                    Stand(direction);
                    if (!enemy.DecisionReady || actionCooldown > 0) return;
                    guardConsumed = false;
                    ChangeState(BrainState.Guard);
                    enemy.actor.Play("NewFlagellant_parry_reaction_anim", false, true);
                    return;
                case BrainState.Guard:
                    enemy.motor.velocity.x = 0;
                    if (dx * enemy.Facing < -.2f || distance < 1.1f)
                    { retreatStart = enemy.transform.position.x; ChangeState(BrainState.Retreat); break; }
                    if (stateClock >= .7f)
                    {
                        if (distance <= 2.8f && !player.Parrying) ChangeState(BrainState.Counter);
                        else ChangeState(BrainState.Reposition);
                    }
                    return;
                case BrainState.Retreat:
                    if (stateClock >= .7f || Mathf.Abs(enemy.transform.position.x - retreatStart) >= 1.6f || distance >= 3.2f || !PathClear(-direction))
                    { Stand(direction); ChangeState(BrainState.Counter); return; }
                    Move(-direction, 1.7f, direction);
                    return;
                case BrainState.Counter:
                    Stand(direction);
                    if (player.Parrying)
                    {
                        if (stateClock >= .8f) { retreatStart = enemy.transform.position.x; ChangeState(BrainState.Retreat); }
                        return;
                    }
                    if (stateClock >= .22f && distance <= 2.65f && Mathf.Abs(LastSeen.y - enemy.transform.position.y) <= 1.2f && StartAttack(direction)) return;
                    if (distance > 2.65f) Move(direction, 1.6f);
                    if (stateClock >= 1f) ChangeState(BrainState.Reposition);
                    return;
            }
            Stand(direction);
        }

        bool StartAttack(float direction)
        {
            if (actionCooldown > 0 || !enemy.DecisionReady || !PlayerVisible) return false;
            if (!enemy.TrialAttack(direction)) return false;
            attackObserved = true;
            return true;
        }

        void MoveToLastSeen()
        {
            if (memory <= 0 || Mathf.Abs(LastSeen.x - enemy.transform.position.x) <= .35f) { ChangeState(BrainState.Search); Stand(); return; }
            if (!Move(FacingLastSeen(), EnemyKind == Kind.Scout ? 1.5f : 1f) && stateClock >= .6f) { ChangeState(BrainState.Search); }
        }

        void Search()
        {
            if (PlayerVisible) { ChangeState(EnemyKind == Kind.Scout ? BrainState.Chase : BrainState.Reposition); return; }
            if (stateClock >= 1.8f) { ChangeState(BrainState.Return); Stand(); return; }
            float direction = Mathf.FloorToInt(stateClock / .45f) % 2 == 0 ? 1f : -1f;
            if ((enemy.transform.position.x - LastSeen.x) * direction > .8f) direction = -direction;
            Move(direction, .5f);
        }

        void ReturnHome()
        {
            float dx = Home.x - enemy.transform.position.x;
            if (Mathf.Abs(dx) <= .15f) { Stand(initialFacing); ChangeState(BrainState.Patrol); return; }
            Move(Mathf.Sign(dx), .95f);
        }

        void RaiseAlert()
        {
            AlertsRaised++;
            var room = enemy.game.Current;
            if (room == null || room.enemies == null) return;
            foreach (var peer in room.enemies)
            {
                if (peer == null || peer == enemy || peer.Dead || peer.trialAI == null || !peer.trialAI.AppliesTo(peer)) continue;
                if (Vector2.Distance(peer.transform.position, enemy.transform.position) > 8f || Physics2D.Linecast(Eye(enemy), Eye(peer), TerrainMask).collider != null) continue;
                peer.trialAI.ReceiveAlert(LastSeen);
            }
            if (enemy.game.effects != null) enemy.game.effects.Burst(enemy.transform.position + Vector3.up * 1.7f, new Color(1f, .65f, .12f));
        }

        void ReceiveAlert(Vector2 seenPosition)
        {
            if (!configured || enemy == null || enemy.Dead || enemy.Hurt || enemy.AttackInProgress) return;
            if (!PlayerVisible) LastSeen = seenPosition;
            memory = MemoryDuration;
            if (State == BrainState.Patrol || State == BrainState.Return || State == BrainState.Search)
                ChangeState(EnemyKind == Kind.Scout ? BrainState.Chase : BrainState.Reposition);
        }

        float FacingLastSeen()
        {
            float dx = LastSeen.x - enemy.transform.position.x;
            return Mathf.Abs(dx) > .02f ? Mathf.Sign(dx) : enemy.Facing;
        }

        void Stand(float direction = 0) => enemy.TrialMove(Mathf.Abs(direction) > .01f ? direction : enemy.Facing, 0);

        bool Move(float direction, float speed, float lookDirection = 0)
        {
            if (!PathClear(direction)) { Stand(Mathf.Abs(lookDirection) > .01f ? lookDirection : direction); return false; }
            enemy.TrialMove(direction, speed, lookDirection);
            return true;
        }

        bool PathClear(float direction)
        {
            if (Mathf.Abs(direction) < .01f || enemy.motor == null) return false;
            direction = Mathf.Sign(direction);
            Vector2 feet = enemy.transform.position;
            // The S07 stairs are authored as 1:2 polygon ramps. Start the
            // support probe above the *next* point on the ramp: the old .4f
            // start was almost level with an uphill surface and could report
            // an inside-collider hit on alternating frames.
            float ahead = enemy.Radius + .3f;
            Vector2 supportOrigin = feet + new Vector2(direction * ahead, .9f);
            var floorFilter = new ContactFilter2D { useLayerMask = true, layerMask = FloorMask, useTriggers = false };
            int floorCount = Physics2D.Raycast(supportOrigin, Vector2.down, floorFilter, floorHits, .9f + MaxDrop);
            bool supported = false;
            for (int i = 0; i < floorCount; i++)
            {
                var hit = floorHits[i];
                float rise = hit.point.y - feet.y;
                if (hit.collider != null && hit.normal.y > .6f && rise <= MaxRise && rise >= -MaxDrop)
                { supported = true; break; }
            }
            if (!supported) return false;

            // A horizontal ray can strike the walkable face of an uphill
            // ramp. Only a near-vertical normal is a blocking wall.
            var wallFilter = new ContactFilter2D { useLayerMask = true, layerMask = TerrainMask, useTriggers = false };
            int wallCount = Physics2D.Raycast(feet + Vector2.up * .75f, Vector2.right * direction,
                wallFilter, wallHits, enemy.Radius + .4f);
            for (int i = 0; i < wallCount; i++)
                if (wallHits[i].collider != null && wallHits[i].normal.y <= .6f) return false;
            var room = enemy.game.Current;
            if (room != null && room.enemies != null)
                foreach (var peer in room.enemies)
                {
                    if (peer == null || peer == enemy || peer.Dead || peer.boss || !peer.gameObject.activeInHierarchy) continue;
                    Vector2 delta = peer.transform.position - enemy.transform.position;
                    if (delta.x * direction > 0 && delta.x * direction < enemy.Radius + peer.Radius + .25f && Mathf.Abs(delta.y) < 1f) return false;
                }
            return true;
        }

        void ChangeState(BrainState next)
        {
            if (State == next) return;
            State = next;
            stateClock = 0;
        }
    }
}
