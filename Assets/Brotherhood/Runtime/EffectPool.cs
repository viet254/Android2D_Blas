using UnityEngine;
namespace Brotherhood
{
    public sealed class EffectPool : MonoBehaviour
    {
        struct Effect
        {
            public Transform tr; public SpriteRenderer sr; public Vector2 velocity; public Vector2 origin;
            public float life, total, clock, damage, phase, speed, hitTimer; public int tier;
            public bool wave, hit, fade, chargedProjectile, rangedProjectile, resolving, returning, crawlerProjectile, orbiting;
            public Transform orbitTarget; public float orbitAngle, orbitRadius;
            public RestoredClip clip;
        }
        public RestoredCatalog catalog; Effect[] pool; int cursor; BrotherhoodGame owner;
        public int ActiveChargedProjectiles { get { int count = 0; if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].life > 0 && pool[i].chargedProjectile) count++; return count; } }
        public int ActiveRangeProjectiles { get { int count = 0; if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].life > 0 && pool[i].rangedProjectile) count++; return count; } }
        public int ActiveDashGhosts { get { int count = 0; if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].life > 0 && pool[i].fade) count++; return count; } }
        public bool HasActiveClip(string name) { if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].life > 0 && pool[i].clip != null && pool[i].clip.name == name) return true; return false; }
        public string LastChargedProjectileResult { get; private set; }
        public string LastRangeProjectileResult { get; private set; }

        void Awake()
        {
            pool = new Effect[100]; var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            for (int i = 0; i < pool.Length; i++)
            {
                var o = new GameObject("Pooled impact"); o.transform.SetParent(transform);
                var sr = o.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 30000;
                o.SetActive(false); pool[i] = new Effect { tr = o.transform, sr = sr };
            }
        }

        public void Burst(Vector3 at, Color color)
        {
            for (int i = 0; i < 12; i++)
            {
                int n = cursor++ % pool.Length; ref var e = ref pool[n];
                e.tr.position = at; e.tr.localScale = Vector3.one * Random.Range(.025f, .085f);
                e.sr.color = color; e.velocity = Random.insideUnitCircle * 3;
                e.life = e.total = .35f; e.wave = e.hit = e.fade = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.crawlerProjectile = e.orbiting = false;
                e.clip = null; e.tr.gameObject.SetActive(true);
            }
        }

        public void Waves(Vector3 at, BrotherhoodGame game)
        {
            owner = game;
            for (int d = -1; d <= 1; d += 2)
            {
                int n = cursor++ % pool.Length; ref var e = ref pool[n];
                e.tr.position = at + Vector3.up * .2f; e.tr.localScale = new Vector3(.55f, .4f, 1);
                e.sr.color = new Color(.8f, .65f, .45f); e.velocity = Vector2.right * d * 6;
                e.life = e.total = 2; e.wave = true;
                e.hit = e.fade = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.crawlerProjectile = e.orbiting = false;
                e.clip = null; e.tr.gameObject.SetActive(true);
            }
        }

        RestoredClip FindClip(string clipName)
        {
            if (catalog == null)
            {
                var source = FindAnyObjectByType<SpriteActor>();
                if (source != null) catalog = source.catalog;
            }
            return catalog == null ? null : catalog.Find(clipName);
        }

        void SetClip(ref Effect e, string clipName)
        {
            var clip = FindClip(clipName); e.clip = clip; e.clock = 0;
            if (clip != null && clip.frames.Length > 0) e.sr.sprite = clip.frames[0];
        }

        public void Animation(string clipName, Vector3 at, float facing)
        {
            var clip = FindClip(clipName); if (clip == null || clip.frames.Length == 0) return;
            int n = cursor++ % pool.Length; ref var e = ref pool[n];
            e.tr.position = at; e.tr.localScale = Vector3.one; e.sr.sprite = clip.frames[0];
            e.sr.flipX = facing < 0; e.sr.sortingOrder = 30000; e.sr.color = Color.white;
            e.velocity = Vector2.zero; e.life = e.total = Mathf.Max(.1f, clip.duration); e.clock = 0;
            e.wave = e.hit = e.fade = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.crawlerProjectile = e.orbiting = false;
            e.clip = clip; e.tr.gameObject.SetActive(true);
        }

        public void ChargedProjectile(Vector3 at, float facing, BrotherhoodGame game, float damage)
        {
            var clip = FindClip("ChargedAttackProjectile_anim"); if (clip == null || clip.frames.Length == 0) return;
            int n = cursor++ % pool.Length; ref var e = ref pool[n]; owner = game;
            e.tr.position = at; e.origin = at; e.tr.localScale = Vector3.one; e.sr.flipX = facing < 0;
            e.sr.sortingOrder = 30000; e.sr.color = Color.white; e.velocity = Vector2.right * Mathf.Sign(facing) * (4f / .3f);
            e.life = e.total = 3; e.clock = 0; e.damage = damage;
            e.wave = e.hit = e.fade = e.resolving = e.crawlerProjectile = e.orbiting = false;
            e.chargedProjectile = true; e.clip = clip; e.sr.sprite = clip.frames[0];
            LastChargedProjectileResult = "Flying"; e.tr.gameObject.SetActive(true);
        }

        public void RangeProjectile(Vector3 at, float facing, BrotherhoodGame game, float damage, int tier)
        {
            var clip = FindClip("penitent_rangeAttack_projectile_anim"); if (clip == null || clip.frames.Length == 0) return;
            int n = cursor++ % pool.Length; ref var e = ref pool[n]; owner = game;
            e.tr.position = at; e.origin = at; e.tr.localScale = Vector3.one; e.sr.flipX = facing < 0;
            e.sr.sortingOrder = 30000; e.sr.color = Color.white; e.velocity = Vector2.right * Mathf.Sign(facing);
            e.life = e.total = 2; e.clock = e.phase = e.speed = 0; e.damage = damage; e.tier = Mathf.Clamp(tier, 1, 3);
            e.wave = e.hit = e.fade = e.chargedProjectile = e.resolving = e.returning = e.crawlerProjectile = e.orbiting = false;
            e.rangedProjectile = true; e.clip = clip; e.sr.sprite = clip.frames[0];
            LastRangeProjectileResult = "Flying"; e.tr.gameObject.SetActive(true); game.Sfx("RANGE_ATTACK_FLY", .75f);
        }

        public void PrayerCrawler(Vector3 at, float dir, BrotherhoodGame game, float damage)
        {
            var clip = FindClip("pontiffOldman_toxicOrb_idle");
            int n = cursor++ % pool.Length; ref var e = ref pool[n]; owner = game;
            e.tr.position = at; e.origin = at; e.tr.localScale = Vector3.one * 1.25f;
            e.sr.flipX = dir < 0; e.sr.sortingOrder = 30000;
            e.sr.color = new Color(0.35f, 0.75f, 1f, 0.95f);
            e.velocity = Vector2.right * Mathf.Sign(dir) * 6.5f;
            e.life = e.total = 3.0f; e.clock = e.phase = e.speed = 0; e.damage = damage;
            e.wave = e.hit = e.fade = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.orbiting = false;
            e.crawlerProjectile = true; e.clip = clip;
            if (clip != null && clip.frames.Length > 0) e.sr.sprite = clip.frames[0];
            e.tr.gameObject.SetActive(true);
            game.Sfx("RANGE_ATTACK_FLY", .8f);
        }

        public void PrayerBeam(Vector3 at, BrotherhoodGame game, float damage)
        {
            owner = game;
            Animation("penitentBeam_startToWarning", at, 1);
            Animation("penitentBeam_attackLoop", at, 1);
            game.Shake(.35f);
            game.Sfx("PENITENT_ACTIVATE_PRAYER");
            game.Sfx("CHARGED_ATTACK_PROJECTILE");
            if (game.Current != null && game.Current.enemies != null)
            {
                foreach (var enemy in game.Current.enemies)
                {
                    if (enemy == null || enemy.Dead) continue;
                    Vector2 d = (Vector2)enemy.transform.position - (Vector2)at;
                    if (Mathf.Abs(d.x) < 2.5f && Mathf.Abs(d.y) < 7f)
                    {
                        enemy.Damage(damage);
                        Burst(enemy.transform.position + Vector3.up, Color.cyan);
                    }
                }
            }
        }

        public void PrayerFlamePillars(Vector3 at, float facing, BrotherhoodGame game, float damage)
        {
            owner = game;
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = at + Vector3.right * facing * (1.2f + i * 1.6f);
                Animation("flamePillar_warningToAttack", p, facing);
                Animation("flamePillar_attackLoop", p, facing);
            }
            game.Shake(.25f);
            game.Sfx("VERTICAL_ATTACK_HIT");
            if (game.Current != null && game.Current.enemies != null)
            {
                foreach (var enemy in game.Current.enemies)
                {
                    if (enemy == null || enemy.Dead) continue;
                    Vector2 d = (Vector2)enemy.transform.position - (Vector2)at;
                    if (d.x * facing >= 0.5f && d.x * facing <= 6.5f && Mathf.Abs(d.y) < 3.5f)
                    {
                        enemy.Damage(damage);
                        Burst(enemy.transform.position + Vector3.up, new Color(1f, 0.45f, 0.15f));
                    }
                }
            }
        }

        public void PrayerShields(Transform parent, BrotherhoodGame game, float damage)
        {
            owner = game;
            var clip = FindClip("penitent_blueFireDisc");
            for (int i = 0; i < 2; i++)
            {
                int n = cursor++ % pool.Length; ref var e = ref pool[n];
                e.tr.position = parent.position + Vector3.up;
                e.tr.localScale = Vector3.one * 0.7f;
                e.sr.sortingOrder = 30005;
                e.sr.color = new Color(0.4f, 0.75f, 1f, 0.85f);
                e.velocity = Vector2.zero;
                e.life = e.total = 8.0f; e.clock = e.phase = 0; e.damage = damage;
                e.wave = e.hit = e.fade = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.crawlerProjectile = false;
                e.orbiting = true; e.orbitTarget = parent; e.orbitAngle = i * Mathf.PI; e.orbitRadius = 1.35f;
                e.clip = clip;
                if (clip != null && clip.frames.Length > 0) e.sr.sprite = clip.frames[0];
                e.tr.gameObject.SetActive(true);
            }
        }

        public void Ghost(SpriteRenderer source)
        {
            int n = cursor++ % pool.Length; ref var e = ref pool[n];
            e.tr.position = source.transform.position; e.tr.localScale = source.transform.lossyScale;
            e.sr.sprite = source.sprite; e.sr.flipX = source.flipX; e.sr.color = new Color(.45f, .75f, .8f, .38f);
            e.sr.sortingOrder = source.sortingOrder - 1; e.velocity = Vector2.zero;
            e.life = e.total = .22f; e.clock = 0;
            e.wave = e.hit = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.crawlerProjectile = e.orbiting = false;
            e.fade = true; e.clip = null; e.tr.gameObject.SetActive(true);
        }

        void ResolveProjectile(ref Effect e, string result, string sound)
        {
            e.resolving = true; e.velocity = Vector2.zero;
            SetClip(ref e, result == "Expired" ? "ChargedAttackProjectile_vanish_anim" : "ChargedAttackProjectile_impact_anim");
            e.life = e.total = Mathf.Max(.1f, e.clip == null ? .18f : e.clip.duration);
            LastChargedProjectileResult = result; if (owner != null) owner.Sfx(sound);
        }

        void ResolveRange(ref Effect e, string result)
        {
            e.resolving = true; e.velocity = Vector2.zero;
            SetClip(ref e, "penitent_rangeAttack_projectile_vanish_anim");
            e.life = e.total = Mathf.Max(.1f, e.clip == null ? .3f : e.clip.duration);
            LastRangeProjectileResult = result; if (owner != null) owner.Sfx("RANGE_ATTACK_DISSAPEAR", .75f);
        }

        void ResolveCrawler(ref Effect e)
        {
            e.resolving = true; e.velocity = Vector2.zero;
            SetClip(ref e, "pontiffOldman_toxicOrb_cloudExplosion");
            e.life = e.total = Mathf.Max(.15f, e.clip == null ? .4f : e.clip.duration);
            if (owner != null)
            {
                owner.Sfx("PENITENT_ACTIVATE_PRAYER");
                owner.effects.Burst(e.tr.position, new Color(0.35f, 0.75f, 1f));
            }
        }

        void Update()
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                ref var e = ref pool[i]; if (e.life <= 0) continue;
                float dt = Time.deltaTime; e.life -= dt; e.clock += dt; e.phase += dt;
                if (e.hitTimer > 0) e.hitTimer -= dt;
                if (e.life <= 0) { e.tr.gameObject.SetActive(false); continue; }
                if (e.clip != null)
                {
                    int frame = 0;
                    while (frame + 1 < e.clip.times.Length && e.clip.times[frame + 1] <= e.clock) frame++;
                    e.sr.sprite = e.clip.frames[Mathf.Min(frame, e.clip.frames.Length - 1)];
                }
                if (e.fade)
                {
                    var c = e.sr.color; c.a = .38f * Mathf.Clamp01(e.life / e.total); e.sr.color = c;
                }
                if (e.orbiting)
                {
                    if (e.orbitTarget == null) { e.life = 0; continue; }
                    e.orbitAngle += dt * 3.8f;
                    Vector3 center = e.orbitTarget.position + Vector3.up * 0.75f;
                    e.tr.position = center + new Vector3(Mathf.Cos(e.orbitAngle) * e.orbitRadius, Mathf.Sin(e.orbitAngle) * 0.45f, 0);
                    if (e.hitTimer <= 0 && owner != null && owner.Current != null)
                    {
                        foreach (var enemy in owner.Current.enemies)
                        {
                            if (enemy == null || enemy.Dead) continue;
                            if (Vector2.Distance(enemy.transform.position + Vector3.up * 0.5f, e.tr.position) < 1.0f + enemy.Radius)
                            {
                                enemy.Damage(e.damage);
                                owner.effects.Burst(e.tr.position, Color.cyan);
                                owner.Sfx("PENITENT_PARRY_HIT");
                                e.hitTimer = 0.35f;
                                break;
                            }
                        }
                    }
                    continue;
                }
                if (e.crawlerProjectile && !e.resolving)
                {
                    Vector2 from = e.tr.position;
                    Vector2 next = from + e.velocity * dt;
                    var wall = Physics2D.Linecast(from, next, 1 << 8);
                    if (wall.collider != null)
                    {
                        e.tr.position = wall.point;
                        ResolveCrawler(ref e);
                        continue;
                    }
                    if (owner != null && owner.Current != null)
                    {
                        foreach (var enemy in owner.Current.enemies)
                        {
                            if (enemy == null || enemy.Dead) continue;
                            Vector2 d = (Vector2)enemy.transform.position - next;
                            if (Mathf.Abs(d.x) < 0.95f + enemy.Radius && Mathf.Abs(d.y) < 1.4f)
                            {
                                enemy.Damage(e.damage);
                                owner.effects.Burst(enemy.transform.position + Vector3.up * 0.5f, new Color(0.35f, 0.75f, 1f));
                                owner.Sfx("RANGE_ATTACK_HIT");
                                if (e.phase > 0.15f)
                                {
                                    ResolveCrawler(ref e);
                                    break;
                                }
                            }
                        }
                    }
                    if (e.resolving) continue;
                    e.tr.position = next;
                    if (Vector2.Distance(e.origin, next) >= 12.0f)
                    {
                        ResolveCrawler(ref e);
                        continue;
                    }
                    continue;
                }
                if (e.rangedProjectile && !e.resolving)
                {
                    Vector2 from = e.tr.position; e.speed = Mathf.Min(23, e.speed + 200 * dt);
                    Vector2 direction = e.returning && owner != null ? ((Vector2)owner.player.transform.position + Vector2.up * .65f - from).normalized : e.velocity.normalized;
                    Vector2 next = from + direction * e.speed * dt;
                    var wall = Physics2D.Linecast(from, next, 1 << 8);
                    if (wall.collider != null) { e.tr.position = wall.point; ResolveRange(ref e, "Wall"); continue; }
                    if (!e.hit && owner != null && owner.Current != null)
                        foreach (var enemy in owner.Current.enemies)
                        {
                            if (enemy == null || enemy.Dead) continue;
                            Vector2 d = (Vector2)enemy.transform.position - next;
                            if (Mathf.Abs(d.x) < 1 + enemy.Radius && Mathf.Abs(d.y) < .8f + enemy.Radius)
                            {
                                enemy.Damage(e.damage); owner.effects.Burst(enemy.transform.position + Vector3.up, Color.red);
                                owner.Sfx("RANGE_ATTACK_HIT"); e.hit = true; LastRangeProjectileResult = "Enemy"; break;
                            }
                        }
                    e.tr.position = next;
                    if (!e.returning && e.phase >= .3f)
                    {
                        if (e.tier == 1) { ResolveRange(ref e, "Expired"); continue; }
                        e.returning = true; e.phase = 0; e.speed = 0; e.hit = false;
                        if (e.tier >= 3)
                        {
                            Animation("penitent_rangeAttack_projectile_explode_anim", e.tr.position, 1);
                            if (owner != null)
                            {
                                owner.Sfx("RANGE_ATTACK_EXPLODE");
                                if (owner.Current != null)
                                    foreach (var enemy in owner.Current.enemies)
                                        if (!enemy.Dead && Vector2.Distance(enemy.transform.position, e.tr.position) < 2)
                                            enemy.Damage(e.damage * .5f);
                            }
                            LastRangeProjectileResult = "Explosion";
                        }
                    }
                    else if (e.returning && e.phase >= .3f) { ResolveRange(ref e, "Returned"); continue; }
                }
                else if (e.chargedProjectile && !e.resolving)
                {
                    Vector2 from = e.tr.position, next = from + e.velocity * dt;
                    var wall = Physics2D.Linecast(from, next, 1 << 8);
                    if (wall.collider != null) { e.tr.position = wall.point; ResolveProjectile(ref e, "Wall", "CHARGED_ATTACK_PROJECTILE_HIT_WALL"); continue; }
                    bool struck = false;
                    if (owner != null && owner.Current != null)
                        foreach (var enemy in owner.Current.enemies)
                        {
                            if (enemy == null || enemy.Dead) continue;
                            Vector2 d = (Vector2)enemy.transform.position - next;
                            if (Mathf.Abs(d.x) <= 1.205f + enemy.Radius && Mathf.Abs(d.y) <= 1.343f + enemy.Radius)
                            {
                                enemy.Damage(e.damage); e.tr.position = next;
                                owner.effects.Burst(enemy.transform.position + Vector3.up, Color.red);
                                ResolveProjectile(ref e, "Enemy", "CHARGED_ATTACK_PROJECTILE_HIT");
                                struck = true; break;
                            }
                        }
                    if (struck) continue;
                    e.tr.position = next;
                    if (Vector2.Distance(e.origin, next) >= 4f) { ResolveProjectile(ref e, "Expired", "CHARGED_ATTACK_PROJECTILE"); continue; }
                }
                else e.tr.position += (Vector3)(e.velocity * dt);

                if (!e.wave && e.clip == null && !e.fade && !e.chargedProjectile && !e.rangedProjectile && !e.crawlerProjectile && !e.orbiting)
                    e.velocity.y -= dt * 8;
                else if (e.wave && !e.hit && owner != null && Vector2.Distance(e.tr.position, owner.player.transform.position) < .65f)
                {
                    owner.player.Damage(20, e.tr.position.x, false); e.hit = true;
                }
            }
        }
        public void Clear()
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++) { pool[i].life = 0; pool[i].tr.gameObject.SetActive(false); }
        }
    }
}
