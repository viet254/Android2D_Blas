using System.Collections.Generic;
using UnityEngine;

namespace Brotherhood
{
    // These prayers retain their individual source hit areas and schedules.
    // They run on the gameplay clock so pausing never spends their lifetimes.
    public sealed class LegacyPrayerEffects : MonoBehaviour
    {
        sealed class Visual
        {
            public SpriteActor actor;
            public float age, life;
        }

        sealed class Cherub
        {
            public SpriteActor actor;
            public Vector3 velocity, shootOrigin, shootDestination;
            public EnemyController target;
            public Vector2 offset;
            public float age, charge;
            public bool shooting;
        }

        sealed class Cloud
        {
            public SpriteActor actor;
            public Vector2 position;
            public float age, pulse, worldPulse, damage;
            public bool idle;
            public readonly HashSet<EnemyController> occupants = new HashSet<EnemyController>();
        }

        static readonly Vector2[] CherubOffsets =
        {
            new Vector2(0, 1.5f), new Vector2(-1, 2.5f), new Vector2(1, 2.5f),
            new Vector2(0, 3.5f), new Vector2(-1, 4.5f), new Vector2(1, 4.5f),
            new Vector2(0, 5.5f)
        };
        static readonly Vector3 BeamBodyOffset = new Vector3(.15625f, -.46875f, 0);

        BrotherhoodGame game;
        string prayer, room;
        float clock, duration, direction, scanClock, beamNextHit, castDamage, guardianAge;
        int deployed, jondoIndex, beamPhase;
        Vector3 origin, shieldCenter;
        SpriteActor beam, guardian;
        readonly List<SpriteActor> actors = new List<SpriteActor>();
        readonly List<Visual> visuals = new List<Visual>();
        readonly List<Cherub> cherubs = new List<Cherub>();
        readonly List<Cloud> clouds = new List<Cloud>();
        readonly List<EnemyController> jondoTargets = new List<EnemyController>();
        readonly HashSet<EnemyController>[] shieldPopulation =
        {
            new HashSet<EnemyController>(), new HashSet<EnemyController>()
        };
        readonly SpriteActor[] shields = new SpriteActor[2];

        public string RunningPrayer => prayer;
        public int ActiveActors => actors.Count;
        public int AvailableCherubs => cherubs.Count;
        public int ActiveClouds => clouds.Count;
        public Color PlayerTint => prayer == "PR03" && clock >= .4f && clock < 1.2f
            ? new Color(.45f, .7f, 1f, 1f) : Color.white;

        public void Initialize(BrotherhoodGame owner) { game = owner; }

        public bool Cast(string id, float seconds)
        {
            if (id != "PR03" && id != "PR05" && id != "PR08" && id != "PR11" &&
                id != "PR12" && id != "PR14" && id != "PR15") return false;
            Stop();
            prayer = id;
            room = game.Current.id;
            clock = scanClock = beamNextHit = 0;
            duration = Mathf.Max(0, seconds);
            string lifetimeEffect = id == "PR05" ? "PrayerAlliedCherubEffect" :
                id == "PR08" ? "PrayerShieldEffect" : id == "PR11" ? "ItemTemporalEffect" :
                id == "PR15" ? "ToxicCloudEffect" : null;
            if (lifetimeEffect != null)
            {
                var item = InventoryCatalog.Load().Find(id);
                if (item?.effects != null)
                    foreach (var effect in item.effects)
                        if (effect.script == lifetimeEffect && effect.limitTime != 0)
                            duration = new InventoryModifiers(game.progress, game.player).PrayerEffectDuration(effect);
            }
            direction = game.player.facing;
            origin = game.player.transform.position;
            deployed = jondoIndex = beamPhase = 0;
            castDamage = new InventoryModifiers(game.progress, game.player).PrayerDamage(id == "PR03" ? 55 : 210);

            if (id == "PR03")
            {
                var ground = Physics2D.Raycast(origin, Vector2.down, 8f, (1 << 8) | (1 << 9));
                if (ground.collider != null) origin.y = ground.point.y;
                beam = Create("Debla celestial beam", origin + BeamBodyOffset, "penitentBeam_startToWarning");
                game.Shake(.2f);
                game.Sfx("PENITENT_RAY_FIRE");
            }
            else if (id == "PR05") DeployCherub();
            else if (id == "PR08")
            {
                shieldCenter = origin + Vector3.up;
                for (int i = 0; i < 2; i++)
                    shields[i] = Create("Zarabanda shield " + i, shieldCenter, "PrayerShieldSprite", true);
            }
            else if (id == "PR12")
            {
                Show("Cante Jondo invocation", origin, "penitent_prayerPR12Action", 1.5f);
                game.Sfx("BELL_RECEIVER_ACTIVATE");
                game.Shake(.2f);
                foreach (var enemy in game.Current.enemies)
                {
                    if (enemy == null || enemy.Dead || !enemy.isActiveAndEnabled) continue;
                    var bounds = PrayerCombat.EnemyBounds(enemy);
                    Vector2 closest = bounds.ClosestPoint(origin);
                    if ((closest - (Vector2)origin).sqrMagnitude <= 20f * 20f)
                    {
                        jondoTargets.Add(enemy);
                        if (jondoTargets.Count == 20) break;
                    }
                }
                PrayerCombat.StrikeCircle(game, origin, 20f);
                HitJondoTarget();
            }
            else if (id == "PR14")
            {
                float damage = new InventoryModifiers(game.progress, game.player).PrayerDamage(18);
                game.effects.PrayerCrawler(origin + Vector3.right * .01f, 1, game, damage);
                game.effects.PrayerCrawler(origin + Vector3.left * .01f, -1, game, damage);
                game.Shake(.15f);
            }
            else if (id == "PR15") SpawnCloud();
            return true;
        }

        SpriteActor Create(string name, Vector3 at, string clip, bool loop = false)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(transform);
            obj.transform.position = at;
            var actor = obj.AddComponent<SpriteActor>();
            actor.enabled = false;
            actor.catalog = game.player.actor.catalog;
            if (clip == "AlliedCherub_flying")
            {
                var body = new GameObject("Sprite"); body.transform.SetParent(obj.transform, false);
                body.transform.localPosition = new Vector3(-.029659271f, -.011656761f, 0);
                actor.visual = body.AddComponent<SpriteRenderer>();
            }
            else actor.visual = obj.AddComponent<SpriteRenderer>();
            actor.visual.sharedMaterial = game.player.actor.visual.sharedMaterial;
            if (clip == "penitent_blueFireFull")
            {
                var palette = Resources.Load<Material>("Effects/recolor_FireToBlue");
                if (palette != null) actor.visual.sharedMaterial = palette;
            }
            actor.visual.sortingLayerName = "Player";
            actor.visual.sortingOrder = clip.StartsWith("penitentBeam_") ? -1 : 2;
            if (clip == "AlliedCherub_flying")
            { actor.visual.sortingLayerName = "Default"; actor.visual.sortingOrder = 0; }
            actor.Face(direction);
            actor.Play(clip, loop, true);
            actors.Add(actor);
            return actor;
        }

        void Show(string name, Vector3 at, string clip, float life)
        {
            visuals.Add(new Visual { actor = Create(name, at, clip), life = life });
        }

        void Dispose(SpriteActor actor)
        {
            if (actor == null) return;
            actors.Remove(actor);
            actor.gameObject.SetActive(false);
            Destroy(actor.gameObject);
        }

        public void Tick(float dt)
        {
            if (string.IsNullOrEmpty(prayer) || dt <= 0) return;
            if (game.player.Dead || game.Current == null || game.Current.id != room) { Stop(); return; }
            clock += dt;
            // Creation and disposal happen after this pass, never during it.
            foreach (var actor in actors) if (actor != null) actor.Advance(dt);
            for (int i = visuals.Count - 1; i >= 0; i--)
            {
                var visual = visuals[i]; visual.age += dt;
                if (visual.age < visual.life) continue;
                Dispose(visual.actor); visuals.RemoveAt(i);
            }
            if (prayer == "PR03") TickBeam();
            else if (prayer == "PR05") TickCherubs(dt);
            else if (prayer == "PR08") TickShields(dt);
            else if (prayer == "PR11")
            {
                if (guardian != null)
                {
                    guardianAge += dt;
                    float t = Mathf.Clamp01(guardianAge / .1f);
                    Color color = guardian.visual.color; color.a = 1f - (1f - t) * (1f - t);
                    guardian.visual.color = color;
                }
                if (guardian != null && guardian.Progress >= 1) { Dispose(guardian); guardian = null; }
                if (clock >= duration && guardian == null) Stop();
            }
            else if (prayer == "PR12")
            {
                while (jondoIndex < jondoTargets.Count && clock >= jondoIndex * .5f) HitJondoTarget();
                if (jondoIndex >= jondoTargets.Count && visuals.Count == 0) Stop();
            }
            else if (prayer == "PR14")
            {
                // The projectiles own their two-second expiration explosion.
                // Natural prayer completion must not cancel that pool event.
                if (clock >= 2f) { prayer = null; room = null; clock = duration = 0; }
            }
            else if (prayer == "PR15") TickClouds(dt);
        }

        void TickBeam()
        {
            if (beam == null) { if (visuals.Count == 0) Stop(); return; }
            if (clock >= .01f && beamPhase == 0)
            { beamPhase = 1; beam.Play("penitentBeam_warningToAttack", false, true); }
            if (clock >= .35f && beamPhase == 1)
            { beamPhase = 2; beam.Play("penitentBeam_attackLoop", true, true); }
            // BossSpawnedAreaAttack has preparationSeconds=0, loopSeconds=1,
            // firstTickDelay=0 and timeBetweenTicks=.28 in the mobile prefab.
            while (beamNextHit < 1f && clock >= beamNextHit)
            {
                beamNextHit += .28f;
                var area = new Bounds(origin + BeamBodyOffset + Vector3.up * 6, new Vector3(6.8f, 12, 2));
                foreach (var enemy in game.Current.enemies)
                {
                    if (!PrayerCombat.Hit(area, enemy)) continue;
                    PrayerCombat.Damage(game, enemy, castDamage);
                    Show("Debla impact", PrayerCombat.EnemyBounds(enemy).center,
                        "prayerHealingEffect", 2f);
                    game.Sfx("PENITENT_SIMPLE_ENEMY_HIT");
                }
                PrayerCombat.Strike(game, area);
            }
            if (clock >= 1f && beamPhase != 3)
            { beamPhase = 3; beam.Play("penitentBeam_fade", false, true); }
            if (clock >= 1.39f) { Dispose(beam); beam = null; }
        }

        void DeployCherub()
        {
            if (deployed >= CherubOffsets.Length) return;
            var offset = CherubOffsets[deployed++];
            var at = game.player.transform.position;
            var cherub = new Cherub { actor = Create("Allied cherub " + deployed, at, "AlliedCherub_flying", true), offset = offset };
            cherubs.Add(cherub);
            Show("Cherub arrival", at + (Vector3)offset, "penitent_blueFireFull", 1f);
        }

        bool EnemyVisible(EnemyController enemy)
        {
            if (enemy == null || enemy.Dead || !enemy.isActiveAndEnabled || enemy.actor == null || !enemy.actor.visual.enabled) return false;
            if (game.view == null) return true;
            Vector3 at = game.view.WorldToViewportPoint(PrayerCombat.EnemyBounds(enemy).center);
            return at.z > 0 && at.x >= 0 && at.x <= 1 && at.y >= 0 && at.y <= 1;
        }

        EnemyController ClosestTarget(Cherub cherub)
        {
            EnemyController result = null; float best = 10f;
            foreach (var enemy in game.Current.enemies)
            {
                if (!EnemyVisible(enemy)) continue;
                float distance = Vector2.Distance(game.player.transform.position, enemy.transform.position);
                if (distance >= best) continue;
                Vector2 target = PrayerCombat.EnemyBounds(enemy).center;
                if (Physics2D.Linecast(cherub.actor.transform.position, target, 1 << 8).collider != null) continue;
                best = distance; result = enemy;
            }
            return result;
        }

        void TickCherubs(float dt)
        {
            while (deployed < CherubOffsets.Length && clock >= deployed * .5f && clock < duration) DeployCherub();
            scanClock += dt;
            if (clock < duration && scanClock >= 1f)
            {
                scanClock -= 1f;
                for (int i = cherubs.Count - 1; i >= 0; i--)
                {
                    var cherub = cherubs[i];
                    if (cherub.target != null || cherub.shooting) continue;
                    var target = ClosestTarget(cherub);
                    if (target != null) { cherub.target = target; break; }
                }
            }
            for (int i = cherubs.Count - 1; i >= 0; i--)
            {
                var cherub = cherubs[i]; cherub.age += dt;
                if (clock >= duration && !cherub.shooting) { StoreCherub(i); continue; }
                if (cherub.target != null && cherub.target.Dead && !cherub.shooting) cherub.target = null;
                if (cherub.shooting)
                {
                    cherub.charge += dt;
                    float t = Mathf.Clamp01(cherub.charge / .6f);
                    float ease = 1f - Mathf.Pow(1f - t, 3);
                    cherub.actor.transform.position = Vector3.LerpUnclamped(cherub.shootOrigin, cherub.shootDestination, ease);
                    cherub.actor.visual.color = Color.Lerp(Color.white, new Color(.5f, .85f, 1), t);
                    if (cherub.charge >= .6f) { FireCherub(cherub); StoreCherub(i); }
                    continue;
                }
                if (cherub.target != null)
                {
                    Vector3 target = PrayerCombat.EnemyBounds(cherub.target).center;
                    if (cherub.age >= 1f && Vector2.Distance(cherub.actor.transform.position, target) < 7.5f)
                    {
                        cherub.shooting = true; cherub.charge = 0;
                        cherub.shootOrigin = cherub.actor.transform.position;
                        cherub.shootDestination = cherub.shootOrigin + (target - cherub.shootOrigin).normalized * 2f;
                    }
                    else cherub.actor.transform.position = Vector3.SmoothDamp(cherub.actor.transform.position,
                        target + Vector3.up, ref cherub.velocity, .25f, 12f, dt);
                }
                else
                {
                    Vector3 follow = game.player.transform.position + (Vector3)cherub.offset + Vector3.up;
                    follow += new Vector3(Mathf.Sin(clock) * .1f, Mathf.Sin(clock * 3) * .15f);
                    cherub.actor.transform.position = Vector3.SmoothDamp(cherub.actor.transform.position,
                        follow, ref cherub.velocity, game.player.motor.grounded ? .05f : .15f, 25f, dt);
                }
                if (Mathf.Abs(cherub.velocity.x) > .1f) cherub.actor.Face(cherub.velocity.x);
            }
            if (cherubs.Count == 0 && deployed >= CherubOffsets.Length && visuals.Count == 0) Stop();
        }

        void FireCherub(Cherub cherub)
        {
            Vector2 from = cherub.actor.transform.position;
            Vector2 target = cherub.target != null ? (Vector2)PrayerCombat.EnemyBounds(cherub.target).center : (Vector2)cherub.shootDestination;
            Vector2 forward = (target - from).normalized;
            if (forward.sqrMagnitude < .1f) forward = Vector2.right * direction;
            Vector2 normal = new Vector2(-forward.y, forward.x);
            var beamActor = Create("Cherub railgun", from + forward * 7.5f, "threeAnguish_spear_attackLine");
            beamActor.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(forward.y, forward.x) * Mathf.Rad2Deg);
            beamActor.visual.flipX = false;
            if (beamActor.visual.sprite != null)
                beamActor.transform.localScale = new Vector3(15f / Mathf.Max(.01f, beamActor.visual.sprite.bounds.size.x), 1, 1);
            visuals.Add(new Visual { actor = beamActor, life = .5f });
            float multiplier = new InventoryModifiers(game.progress, game.player).Value(27, 1);
            float damage = Mathf.Max(0, 40f * (1f + .125f * (multiplier - 1f)));
            foreach (var enemy in game.Current.enemies)
            {
                if (enemy == null || enemy.Dead || !enemy.isActiveAndEnabled) continue;
                var bounds = PrayerCombat.EnemyBounds(enemy);
                Vector2 delta = (Vector2)bounds.center - from;
                float along = Vector2.Dot(delta, forward), perpendicular = Mathf.Abs(Vector2.Dot(delta, normal));
                float alongExtent = Mathf.Abs(forward.x) * bounds.extents.x + Mathf.Abs(forward.y) * bounds.extents.y;
                float normalExtent = Mathf.Abs(normal.x) * bounds.extents.x + Mathf.Abs(normal.y) * bounds.extents.y;
                if (along + alongExtent < 0 || along - alongExtent > 15 || perpendicular > 1 + normalExtent) continue;
                PrayerCombat.Damage(game, enemy, damage);
            }
            PrayerCombat.StrikeBeam(game, from, from + forward * 15, 2);
            game.Sfx("PRAYER_SHOT");
        }

        void StoreCherub(int index)
        {
            var cherub = cherubs[index];
            Show("Cherub departure", cherub.actor.transform.position, "penitent_blueFireFull", 1f);
            Dispose(cherub.actor); cherubs.RemoveAt(index);
        }

        void TickShields(float dt)
        {
            shieldCenter = Vector3.Slerp(shieldCenter, game.player.transform.position + Vector3.up, Mathf.Clamp01(dt * 15));
            float t = Mathf.Clamp01(clock / .5f);
            float radius = 2f * (1f - (1f - t) * (1f - t));
            if (clock >= duration)
            {
                float retreat = Mathf.Clamp01(clock - duration);
                radius = 2f * (1f - retreat) * (1f - retreat);
            }
            float damage = new InventoryModifiers(game.progress, game.player).PrayerDamage(18);
            for (int i = 0; i < 2; i++)
            {
                float angle = clock * (Mathf.PI * 2f / 1.15f) + i * Mathf.PI;
                Vector2 center = shieldCenter + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                shields[i].transform.position = center;
                var area = new Bounds(center, new Vector3(1.5f, 1.5f, 2));
                game.effects.InterceptProjectiles(center, .75f);
                foreach (var enemy in game.Current.enemies)
                {
                    bool hit = enemy != null && !enemy.Dead && enemy.isActiveAndEnabled &&
                        ((Vector2)PrayerCombat.EnemyBounds(enemy).ClosestPoint(center) - center).sqrMagnitude <= .75f * .75f;
                    if (!hit) { shieldPopulation[i].Remove(enemy); continue; }
                    if (!shieldPopulation[i].Add(enemy)) continue;
                    PrayerCombat.Damage(game, enemy, damage);
                    game.Sfx("RANGE_ATTACK_HIT");
                }
                PrayerCombat.StrikeCircle(game, center, .75f);
            }
            if (clock >= duration + 1f) Stop();
        }

        public void PlayerDamaged() { PlayerDamaged(game.player.transform.position.x - game.player.facing); }
        public void PlayerDamaged(float attackerX)
        {
            if (prayer != "PR11" || clock >= duration || guardian != null) return;
            guardian = Create("Thorned lady protection", game.player.transform.position + Vector3.up * 1.5f, "penitent_guardian_lady_anim");
            guardian.visual.flipX = attackerX <= game.player.transform.position.x;
            guardianAge = 0; guardian.visual.color = new Color(1, 1, 1, 0);
            game.Sfx("PRAYER_INVINCIBILITY");
        }

        void HitJondoTarget()
        {
            if (jondoIndex >= jondoTargets.Count) return;
            var enemy = jondoTargets[jondoIndex++];
            if (enemy == null || enemy.Dead || !enemy.isActiveAndEnabled) return;
            Vector3 at = PrayerCombat.EnemyBounds(enemy).center + Vector3.up * .5f;
            PrayerCombat.Damage(game, enemy, castDamage);
            Show("Cante Jondo enemy impact", at, "prayerPR12EnemyImpact", 1);
            game.Sfx("PENITENT_PARRY_HIT");
            game.Sfx("BELL_RECEIVER_ACTIVATE");
        }

        void SpawnCloud()
        {
            Vector2 at = game.player.transform.position + Vector3.up;
            var actor = Create("Crimson mist", at + new Vector2(-.0307064f, -.1751716f), "pontiffOldman_toxicBurstToFog");
            clouds.Add(new Cloud { actor = actor, position = at,
                damage = new InventoryModifiers(game.progress, game.player).PrayerDamage(25) });
            game.Sfx("BLOOD_CLOUDS");
        }

        void TickClouds(float dt)
        {
            while (deployed + 1 < duration && clock > deployed + 1)
            { deployed++; SpawnCloud(); }
            for (int i = clouds.Count - 1; i >= 0; i--)
            {
                var cloud = clouds[i]; cloud.age += dt; cloud.worldPulse += dt;
                if (!cloud.idle && cloud.age >= 1.11f)
                { cloud.idle = true; cloud.actor.Play("pontiffOldman_fogToIdle", true, true); }
                if (cloud.age >= 3f) { Dispose(cloud.actor); clouds.RemoveAt(i); continue; }
                if (cloud.age >= 1f)
                {
                    Color color = cloud.actor.visual.color;
                    float fade = Mathf.Clamp01((3f - cloud.age) / 2f);
                    color.a = fade * fade; cloud.actor.visual.color = color;
                }
                var area = new Bounds(cloud.position, new Vector3(2.9425964f, 1.72f, 2));
                foreach (var enemy in game.Current.enemies)
                {
                    if (PrayerCombat.Hit(area, enemy)) cloud.occupants.Add(enemy);
                    else cloud.occupants.Remove(enemy);
                }
                if (cloud.occupants.Count == 0) cloud.pulse = 0;
                else cloud.pulse += dt;
                while (cloud.pulse >= .5f)
                {
                    cloud.pulse -= .5f;
                    foreach (var enemy in cloud.occupants)
                    {
                        if (!PrayerCombat.Hit(area, enemy)) continue;
                        PrayerCombat.Damage(game, enemy, cloud.damage);
                        game.Sfx("PENITENT_SIMPLE_ENEMY_HIT");
                    }
                }
                while (cloud.worldPulse >= .5f)
                {
                    cloud.worldPulse -= .5f;
                    PrayerCombat.Strike(game, area);
                }
            }
            if (clock >= duration && clouds.Count == 0) Stop();
        }

        public void Stop()
        {
            // Explicit reset also covers the frame after the PR14 controller
            // finishes, before the independent pool consumes its final tick.
            if (game != null && game.effects != null) game.effects.StopPrayerCrawlers();
            foreach (var actor in actors)
                if (actor != null) { actor.gameObject.SetActive(false); Destroy(actor.gameObject); }
            actors.Clear(); visuals.Clear(); cherubs.Clear(); clouds.Clear(); jondoTargets.Clear();
            foreach (var population in shieldPopulation) population.Clear();
            shields[0] = shields[1] = null; beam = guardian = null;
            prayer = null; room = null; clock = duration = 0;
        }
    }
}
