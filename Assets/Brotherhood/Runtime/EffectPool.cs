using UnityEngine;
namespace Brotherhood
{
    public sealed class EffectPool : MonoBehaviour
    {
        struct Effect
        {
            public Transform tr; public SpriteRenderer sr; public SpriteMask mask; public ParticleSystem particles; public Vector2 velocity; public Vector2 origin;
            public float life, total, clock, damage, phase, speed, hitTimer; public int tier;
            public bool wave, hit, fade, chargedProjectile, rangedProjectile, resolving, returning, crawlerProjectile, orbiting;
            public Transform orbitTarget; public float orbitAngle, orbitRadius;
            public RestoredClip clip;
        }
        public RestoredCatalog catalog; Effect[] pool; int cursor; BrotherhoodGame owner; Sprite crawlerMaskSprite;
        Material defaultSpriteMaterial,crawlerPalette;
        public int ActiveChargedProjectiles { get { int count = 0; if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].life > 0 && pool[i].chargedProjectile) count++; return count; } }
        public int ActiveRangeProjectiles { get { int count = 0; if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].life > 0 && pool[i].rangedProjectile) count++; return count; } }
        public int ActiveDashGhosts { get { int count = 0; if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].life > 0 && pool[i].fade) count++; return count; } }
        public bool HasActiveClip(string name) { if (pool != null) for (int i = 0; i < pool.Length; i++) if (pool[i].life > 0 && pool[i].clip != null && pool[i].clip.name == name) return true; return false; }
        public void StopClip(string name)
        {
            if (pool == null) return;
            for (int i = 0; i < pool.Length; i++)
            {
                ref var e = ref pool[i];
                if (e.life <= 0 || e.clip == null || e.clip.name != name) continue;
                e.life = 0; e.clip = null; e.tr.gameObject.SetActive(false);
            }
        }
        public string LastChargedProjectileResult { get; private set; }
        public string LastRangeProjectileResult { get; private set; }
        public int ActivePrayerCrawlers {get{int count=0;if(pool!=null)foreach(var e in pool)if(e.life>0&&e.crawlerProjectile)count++;return count;}}
        public void StopPrayerCrawlers()
        {
            if(pool==null)return;
            for(int i=0;i<pool.Length;i++)if(pool[i].crawlerProjectile){pool[i].life=0;pool[i].tr.gameObject.SetActive(false);if(pool[i].particles!=null)pool[i].particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);}
        }
        public bool InterceptProjectiles(Bounds area)
        {
            bool intercepted=false;if(pool==null)return false;
            for(int i=0;i<pool.Length;i++)
            {
                ref var e=ref pool[i];if(e.life<=0||!e.wave||!area.Contains(e.tr.position))continue;
                e.life=0;e.tr.gameObject.SetActive(false);intercepted=true;
            }
            return intercepted;
        }
        public bool InterceptProjectiles(Vector2 center,float radius)
        {
            bool intercepted=false;if(pool==null)return false;
            for(int i=0;i<pool.Length;i++)
            {
                ref var e=ref pool[i];if(e.life<=0||!e.wave||Vector2.SqrMagnitude((Vector2)e.tr.position-center)>radius*radius)continue;
                e.life=0;e.tr.gameObject.SetActive(false);intercepted=true;
            }
            return intercepted;
        }
        public bool InterceptProjectiles(System.Func<Bounds,bool> intersects)
        {
            if(pool==null||intersects==null)return false;bool intercepted=false;
            for(int i=0;i<pool.Length;i++)
            {
                ref var effect=ref pool[i];if(effect.life<=0||!effect.wave)continue;
                // Waves use the existing 0.55 x 0.4 footprint even when a
                // pooled renderer previously held other effect artwork.
                var bounds=new Bounds(effect.tr.position,new Vector3(.55f,.4f,1));
                if(!intersects(bounds))continue;
                effect.life=0;effect.tr.gameObject.SetActive(false);intercepted=true;
            }
            return intercepted;
        }
        static readonly Vector2[] CrawlerHitShape={new Vector2(-.2748413f,1.059494f),new Vector2(-.8829651f,.5363598f),new Vector2(-.95791626f,-.027106285f),new Vector2(.9223938f,-.03513241f),new Vector2(.6677246f,.82211876f)};

        void Awake()
        {
            pool = new Effect[100]; var sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(.5f, .5f), 1);
            for (int i = 0; i < pool.Length; i++)
            {
                var o = new GameObject("Pooled impact"); o.transform.SetParent(transform);
                var sr = o.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sortingOrder = 30000;
                if(i==0)defaultSpriteMaterial=sr.sharedMaterial;
                o.SetActive(false); pool[i] = new Effect { tr = o.transform, sr = sr };
            }
            crawlerPalette=Resources.Load<Material>("Effects/recolor_FireToBlue");
        }

        void Activate(ref Effect e)
        {
            bool masked=e.crawlerProjectile&&e.mask!=null;
            e.sr.sortingLayerName="Default";
            e.sr.sortingOrder=e.crawlerProjectile?30:30000;
            e.sr.maskInteraction=masked?SpriteMaskInteraction.VisibleInsideMask:SpriteMaskInteraction.None;
            e.sr.sharedMaterial=e.crawlerProjectile&&crawlerPalette!=null?crawlerPalette:defaultSpriteMaterial;
            e.sr.enabled=true;
            if(e.mask!=null)e.mask.enabled=masked;
            e.tr.gameObject.SetActive(true);
            if(e.particles!=null)
            {
                e.particles.Clear(true);
                if(e.crawlerProjectile)e.particles.Play(true);
                else e.particles.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        void EnsureCrawlerMask(ref Effect e)
        {
            if(e.mask!=null)return;
            var texture=Resources.Load<Texture2D>("Effects/CrawlerMaskSquare");
            if(texture==null){Debug.LogError("Missing original CrawlerBullet_Base Square_0 mask texture");return;}
            if(crawlerMaskSprite==null)crawlerMaskSprite=Sprite.Create(texture,new Rect(0,0,4,4),new Vector2(.5f,.5f),32f);
            var child=new GameObject("CrawlerBullet_Base SpriteMask");child.transform.SetParent(e.tr,false);
            child.transform.localPosition=new Vector3(0,.59375f,0);
            child.transform.localScale=new Vector3(23.041477f,11.411522f,1);
            e.mask=child.AddComponent<SpriteMask>();e.mask.sprite=crawlerMaskSprite;
            e.mask.alphaCutoff=.2f;e.mask.isCustomRangeActive=true;
            e.mask.frontSortingOrder=31;e.mask.backSortingOrder=29;
        }

        void EnsureCrawlerParticles(ref Effect e)
        {
            if(e.particles!=null)return;
            var material=Resources.Load<Material>("Effects/CrawlerOrbParticles");
            if(material==null){Debug.LogError("Missing original CrawlerOrbParticles source material");return;}
            var child=new GameObject("CrawlerBullet_Base Particles");
            child.transform.SetParent(e.tr,false);
            child.transform.localPosition=new Vector3(-.375f,0,0);
            var system=child.AddComponent<ParticleSystem>();
            var main=system.main;
            main.duration=5f;main.loop=true;main.playOnAwake=false;
            main.simulationSpace=ParticleSystemSimulationSpace.Local;
            main.startLifetime=new ParticleSystem.MinMaxCurve(.2f,.6f);
            main.startSpeed=.01f;
            main.startSize=new ParticleSystem.MinMaxCurve(.5f,1f);
            main.startColor=new Color(.25f,.9068966f,1f,1f);
            var emission=system.emission;
            emission.rateOverTime=0f;
            emission.rateOverDistance=new ParticleSystem.MinMaxCurve(2f,5f);
            var shape=system.shape;shape.enabled=true;
            shape.shapeType=(ParticleSystemShapeType)12;
            shape.angle=25f;shape.radius=.1f;shape.arc=14.598824f;
            shape.rotation=new Vector3(0,0,90);
            var lifetimeColor=system.colorOverLifetime;lifetimeColor.enabled=true;
            var gradient=new Gradient();
            gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},
                new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(1,.829f),new GradientAlphaKey(0,1)});
            lifetimeColor.color=gradient;
            var sheet=system.textureSheetAnimation;sheet.enabled=true;
            sheet.mode=ParticleSystemAnimationMode.Grid;sheet.numTilesX=3;sheet.numTilesY=3;
            sheet.animation=ParticleSystemAnimationType.WholeSheet;
            sheet.frameOverTime=new ParticleSystem.MinMaxCurve(0f,.9999f);
            var renderer=system.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial=material;renderer.sortingOrder=29998;
            e.particles=system;
        }

        public void Burst(Vector3 at, Color color)
        {
            for (int i = 0; i < 12; i++)
            {
                int n = cursor++ % pool.Length; ref var e = ref pool[n];
                e.tr.position = at; e.tr.localScale = Vector3.one * Random.Range(.025f, .085f);
                e.sr.color = color; e.velocity = Random.insideUnitCircle * 3;
                e.life = e.total = .35f; e.wave = e.hit = e.fade = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.crawlerProjectile = e.orbiting = false;
                e.clip = null; Activate(ref e);
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
                e.clip = null; Activate(ref e);
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
            if (clip != null && clip.frames.Length > 0) e.sr.sprite = (clip.times.Length > 0 && clip.times[0] > 0.001f) ? null : clip.frames[0];
        }

        public void Animation(string clipName, Vector3 at, float facing, float speed = 1f, bool blueFire = false)
        {
            var clip = FindClip(clipName); if (clip == null || clip.frames.Length == 0) return;
            int n = cursor++ % pool.Length; ref var e = ref pool[n];
            e.tr.position = at; e.tr.localScale = Vector3.one;
            e.sr.sprite = (clip.times.Length > 0 && clip.times[0] > 0.001f) ? null : clip.frames[0];
            e.sr.flipX = facing < 0; e.sr.sortingOrder = 30000; e.sr.color = Color.white;
            e.speed = Mathf.Max(0.01f, speed);
            e.velocity = Vector2.zero; e.life = e.total = Mathf.Max(.1f, clip.duration / e.speed); e.clock = 0;
            e.wave = e.hit = e.fade = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.crawlerProjectile = e.orbiting = false;
            e.clip = clip; Activate(ref e);
            if(blueFire)
            {
                // BlueFireExplosion_SimpleVFX: Player layer, order 2.
                e.sr.sortingLayerName="Player";e.sr.sortingOrder=2;
                if(crawlerPalette!=null)e.sr.sharedMaterial=crawlerPalette;
            }
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
            LastChargedProjectileResult = "Flying"; Activate(ref e);
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
            LastRangeProjectileResult = "Flying"; Activate(ref e); game.Sfx("RANGE_ATTACK_FLY", .75f);
        }

        public void PrayerCrawler(Vector3 at, float dir, BrotherhoodGame game, float damage)
        {
            // PR14's CrawlerBullet_Base uses BlueFireDisc.controller in the
            // original mobile prefab; the Pontiff toxic orb belongs to a boss.
            var clip = FindClip("penitent_blueFireDisc");
            if (clip == null || clip.frames.Length == 0) return;
            int n = cursor++ % pool.Length; ref var e = ref pool[n]; owner = game;
            var floor=Physics2D.Raycast((Vector2)at+Vector2.up,Vector2.down,10f,1<<8);
            if(floor.collider!=null)at.y=floor.point.y+.05f;
            e.tr.position = at; e.tr.rotation = Quaternion.identity; e.origin = at; e.tr.localScale = Vector3.one;
            e.sr.flipX = dir < 0; e.sr.sortingOrder = 30000;
            e.sr.color = Color.white;
            // Original CrawlerBullet_Base lives for 2 s and decelerates by 8
            // world units/s² from the Penitent prefab's projectileSpeed=16.
            e.velocity = Vector2.right * Mathf.Sign(dir) * 16f;
            e.life = e.total = 2.0f; e.clock = e.phase = e.speed = e.hitTimer = 0; e.damage = damage;
            e.wave = e.hit = e.fade = e.chargedProjectile = e.rangedProjectile = e.resolving = e.returning = e.orbiting = false;
            e.crawlerProjectile = true; e.clip = clip;
            if (clip != null && clip.frames.Length > 0) e.sr.sprite = clip.frames[0];
            EnsureCrawlerMask(ref e);EnsureCrawlerParticles(ref e);Activate(ref e);
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
                Activate(ref e);
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
            e.fade = true; e.clip = null; Activate(ref e);
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
            e.life=0;e.tr.gameObject.SetActive(false);
        }

        void Update(){Tick(Time.deltaTime);}
        public void Tick(float dt)
        {
            if (pool == null||dt<=0) return;
            for (int i = 0; i < pool.Length; i++)
            {
                ref var e = ref pool[i]; if (e.life <= 0) continue;
                e.life -= dt;
                // CrawlerProjectile.UpdateAnimSpeed: 0.6 at 4 units/s, 1 at 16.
                e.clock += dt * (e.crawlerProjectile ? Mathf.Lerp(.6f, 1f, Mathf.Clamp01((Mathf.Abs(e.velocity.x) - 4f) / 12f)) : e.speed > 0 ? e.speed : 1f);
                e.phase += dt;
                if (e.hitTimer > 0) e.hitTimer -= dt;
                if (e.life <= 0)
                {
                    // CrawlerBullet_Base.explosion is the original blue-fire
                    // explosion prefab (same effect as its onHitEffect).
                    if(e.crawlerProjectile&&!e.resolving)
                        Animation("processioner_fireBall_exploding",e.tr.position,1f,1f,true);
                    e.tr.gameObject.SetActive(false);continue;
                }
                if (e.clip != null)
                {
                    float frameTime=e.crawlerProjectile&&!e.resolving?e.clock%Mathf.Max(.01f,e.clip.duration):e.clock;
                    SpriteActor.SampleRenderer(e.clip,frameTime,e.sr);
                    if(e.clip.floatTracks!=null)foreach(var track in e.clip.floatTracks)if(track.classID==212&&track.attribute=="m_Enabled"&&track.keys!=null&&track.keys.Length>0)e.sr.enabled=track.Evaluate(frameTime)>=.5f;
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
                    // CrawlerProjectile.CheckGround uses two short sensors in
                    // local space, turning onto walls and around ledges. Its
                    // velocity is then translated in Space.Self, so a wall is
                    // a change of direction, not an impact/despawn.
                    float direction = Mathf.Sign(e.velocity.x);
                    Vector2 forward = e.tr.TransformDirection(Vector3.right * direction);
                    Vector2 up = e.tr.TransformDirection(Vector3.up);
                    Vector2 sensor = (Vector2)e.tr.position + forward * .07f + up * .1f;
                    bool wall = Physics2D.Linecast(sensor, sensor + forward * .15f, 1 << 8).collider != null;
                    bool floor = Physics2D.Linecast(sensor, sensor - up * .15f, 1 << 8).collider != null;
                    if (wall || !floor)
                    {
                        e.tr.rotation *= Quaternion.Euler(0, 0, (wall ? 90f : -90f) * direction);
                        forward = e.tr.TransformDirection(Vector3.right * direction);
                        up = e.tr.TransformDirection(Vector3.up);
                        sensor = (Vector2)e.tr.position + forward * .07f + up * .1f;
                        var surface = Physics2D.Raycast(sensor, -up, 2f, 1 << 8);
                        if (surface.collider != null)
                            e.tr.position += (Vector3)(surface.point - sensor + up * .05f);
                    }
                    e.tr.Translate(e.velocity * dt, Space.Self);
                    var crawlerPolygon=new Vector2[CrawlerHitShape.Length];
                    for(int v=0;v<crawlerPolygon.Length;v++)crawlerPolygon[v]=e.tr.TransformPoint(new Vector3(CrawlerHitShape[v].x*direction,CrawlerHitShape[v].y));
                    if(owner!=null)PrayerCombat.StrikePolygon(owner,crawlerPolygon);
                    if (owner != null && owner.Current != null)
                    {
                        foreach (var enemy in owner.Current.enemies)
                        {
                            if (enemy == null || enemy.Dead) continue;
                            Vector2 d = (Vector2)enemy.transform.position - (Vector2)e.tr.position;
                            if (e.hitTimer <= 0 && PrayerCombat.Polygon(crawlerPolygon,enemy))
                            {
                                PrayerCombat.Damage(owner,enemy,e.damage);
                                owner.Sfx("RANGE_ATTACK_HIT");
                                Animation("processioner_fireBall_exploding",e.tr.position,1f,1f,true);
                                e.hitTimer = .1f; // ProjectileWeapon.multiHit cooldown in the source.
                                break;
                            }
                        }
                    }
                    e.velocity.x=Mathf.MoveTowards(e.velocity.x,Mathf.Sign(e.velocity.x)*4f,8f*dt);
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
                                PrayerCombat.Damage(owner,enemy,e.damage,gainFervour:false); owner.effects.Burst(enemy.transform.position + Vector3.up, Color.red);
                                owner.Sfx("RANGE_ATTACK_HIT"); e.hit = true; LastRangeProjectileResult = "Enemy"; break;
                            }
                        }
                    if(owner!=null&&owner.world!=null)owner.world.Strike(new Bounds(next,new Vector3(1,.8f,2)));e.tr.position = next;
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
                                            PrayerCombat.Damage(owner,enemy,e.damage * .5f,gainFervour:false);
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
                                owner.player.HeavyWeaponHit(enemy,e.damage); e.tr.position = next;
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
