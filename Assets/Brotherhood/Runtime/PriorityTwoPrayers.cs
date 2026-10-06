using System.Collections.Generic;
using UnityEngine;
namespace Brotherhood
{
    // Source prayer entities own their animation/collision lifecycle. Sword,
    // Penance input and enemy hurt remain in their existing state machines.
    public sealed class PriorityTwoPrayers:MonoBehaviour
    {
        BrotherhoodGame game;string prayer,room;float clock,duration,phaseClock,cooldown;int phase,pulses;bool teleportDone;
        SpriteActor companion;Vector3 followVelocity,origin,actionStart,actionEnd;float direction,actionSeconds,descentSeconds,companionDamage,shardDamage,landingClock=-1;bool reachedGround,animationResumed;
        float shardClock=-1;int shardCount,totalLightningPulses;Vector2 shardOrigin;bool miriamHitWindow;
        readonly List<SpriteActor> actors=new List<SpriteActor>(),cores=new List<SpriteActor>(),beams=new List<SpriteActor>();
        readonly List<TimedVisual> timedVisuals=new List<TimedVisual>();
        readonly List<DivineArea> divineAreas=new List<DivineArea>();
        readonly List<Shard> shards=new List<Shard>();
        readonly List<HashSet<EnemyController>> beamHits=new List<HashSet<EnemyController>>();
        readonly Dictionary<EnemyController,float> coreHits=new Dictionary<EnemyController,float>();
        readonly HashSet<EnemyController> companionHits=new HashSet<EnemyController>();
        readonly float[] beamStarts=new float[6];readonly bool[] beamLaunched=new bool[6],blockedDivineDirections=new bool[2];
        sealed class TimedVisual {public SpriteActor actor;public float remaining;}
        sealed class DivineArea {public Vector2 position;public SpriteActor actor;public float age,nextHit;public bool ending;}
        sealed class Shard {public Vector2 position,offset,size;public string color;public SpriteActor actor;public float age;public bool hit,ending;}
        static readonly Vector2[] TrapOffsets={new Vector2(0,3.5f),new Vector2(-1.75f,1.75f),new Vector2(1.75f,-1.75f),new Vector2(0,-3.5f),new Vector2(-1.75f,-1.75f),new Vector2(1.75f,1.75f),new Vector2(0,3.5f)};
        static readonly Vector2[] MiriamPolygon={new Vector2(-.1509583f,-.4004815f),new Vector2(-2.1597846f,1.1236119f),new Vector2(-.5975878f,2.2377899f),new Vector2(.92251563f,2.2479367f),new Vector2(2.1997576f,1.5197543f),new Vector2(2.80674f,.3612504f),new Vector2(2.8489757f,-.77626216f)};
        public string RunningPrayer=>prayer;
        public int ActiveActors=>actors.Count;
        public int LightningPulses=>pulses;
        public bool GuardianGuarding=>prayer=="PR101"&&(phase==4||phase==5);
        public int MiriamShardCount=>shardCount;
        public Vector2 CompanionPosition=>companion!=null?(Vector2)companion.transform.position:Vector2.zero;
        public void Initialize(BrotherhoodGame owner){game=owner;}
        public bool CanCast(string id)=>id!="PR202"||game.HasPrayerCheckpoint&&game.player.motor.grounded;
        public bool Cast(string id,float seconds)
        {
            if(id!="PR07"&&id!="PR09"&&id!="PR101"&&id!="PR201"&&id!="PR202"&&id!="PR203")return false;
            Stop();prayer=id;room=game.Current.id;clock=phaseClock=cooldown=0;phase=pulses=0;duration=seconds;
            direction=game.player.facing;origin=game.player.transform.position;teleportDone=reachedGround=animationResumed=false;landingClock=-1;followVelocity=Vector3.zero;
            if(id=="PR07"){duration=1;FireLorquiana(0);}
            else if(id=="PR09"){duration=2;blockedDivineDirections[0]=blockedDivineDirections[1]=false;game.Shake(.3f);}
            else if(id=="PR101"||id=="PR201")
            {
                // Guardian spawns at the master; Miriam uses PrayerMiriam's
                // authored horizontal and vertical portal offsets.
                Vector3 offset=id=="PR101"?Vector3.zero:new Vector3(direction*2,3);
                companion=Create(id+" summoned ally",origin+offset,id=="PR101"?"auroraGuardia_appearing":"miriamPortal_appearing");
                companion.AnimationEvent+=OnCompanionEvent;game.Sfx(id=="PR101"?"GUARDIAN_APPEAR":"MIRIAM_APPEAR");
                // Their native OnStart caches CreateHit with the player's
                // general DamageMultiplier (stat 15) when the entity spawns.
                companionDamage=Mathf.Max(0,(id=="PR101"?180:100)*new InventoryModifiers(game.progress,game.player).Value(15,1));
            }
            else if(id=="PR202")game.player.actor.Play("penitent_pr202",false,true);
            else
            {
                origin+=Vector3.right*(direction*1.25f);
                totalLightningPulses=3+Mathf.Max(0,Mathf.FloorToInt(new InventoryModifiers(game.progress,game.player).Bonus(26)*.6f));
                duration=Mathf.Max(6,.75f+(totalLightningPulses-1)*1.2f+.35f+.7f+.65f);
                for(int i=0;i<TrapOffsets.Length;i++)
                {
                    var core=Create("San Telmo core "+i,TrapPoint(i),"SanTelmoCoreHidden");core.visual.enabled=false;cores.Add(core);
                    if(i+1<TrapOffsets.Length)
                    {
                        Vector3 a=TrapPoint(i),b=TrapPoint(i+1);var beam=Create("San Telmo lightning "+i,(a+b)*.5f,"SanTelmoLightning_anim");
                        beam.transform.rotation=Quaternion.Euler(0,0,Mathf.Atan2(b.y-a.y,b.x-a.x)*Mathf.Rad2Deg);
                        var sprite=beam.visual.sprite;if(sprite!=null)beam.transform.localScale=new Vector3(Vector3.Distance(a,b)/Mathf.Max(.01f,sprite.bounds.size.x),1,1);
                        beam.visual.enabled=false;beams.Add(beam);beamHits.Add(new HashSet<EnemyController>());beamStarts[i]=float.PositiveInfinity;beamLaunched[i]=true;
                    }
                }
            }
            return true;
        }
        Vector3 TrapPoint(int index)=>origin+new Vector3(TrapOffsets[index].x*direction,TrapOffsets[index].y,0);
        SpriteActor Create(string name,Vector3 at,string clip,bool loop=false)
        {
            var obj=new GameObject(name);obj.transform.SetParent(transform);obj.transform.position=at;
            var actor=obj.AddComponent<SpriteActor>();actor.enabled=false;actor.catalog=game.player.actor.catalog;actor.visual=obj.AddComponent<SpriteRenderer>();
            actor.visual.sharedMaterial=game.player.actor.visual.sharedMaterial;actor.visual.sortingLayerName="Player";actor.visual.sortingOrder=3;
            actor.Face(direction);actor.Play(clip,loop,true);actors.Add(actor);return actor;
        }
        void Visual(string name,Vector3 at,string clip,float seconds)=>timedVisuals.Add(new TimedVisual{actor=Create(name,at,clip),remaining=seconds});
        void Remove(SpriteActor actor){if(actor==null)return;actors.Remove(actor);actor.gameObject.SetActive(false);Destroy(actor.gameObject);}
        public void Tick(float dt)
        {
            if(string.IsNullOrEmpty(prayer)||dt<=0)return;
            if(game.player.Dead||game.Current==null||game.Current.id!=room){Stop();return;}
            clock+=dt;phaseClock+=dt;cooldown=Mathf.Max(0,cooldown-dt);
            // Animation events add VFX; advance actors present at step start.
            int count=actors.Count;for(int i=0;i<count;i++)if(actors[i]!=null)actors[i].Advance(dt);
            for(int i=timedVisuals.Count-1;i>=0;i--)
            {var effect=timedVisuals[i];effect.remaining-=dt;if(effect.remaining<=0){Remove(effect.actor);timedVisuals.RemoveAt(i);}}
            if(prayer=="PR07")
            {while(pulses<3&&clock>=pulses*.15f)FireLorquiana(UnityEngine.Random.Range(-1f,1f));if(clock>=duration)Stop();}
            else if(prayer=="PR09")TickDivineAreas(dt);
            else if(prayer=="PR101"||prayer=="PR201")TickCompanion(dt);
            else if(prayer=="PR202")
            {
                // RegresoAPuerto's source Animator behaviour has delay 0.3 s.
                if(!teleportDone&&clock>=.3f){teleportDone=true;game.TeleportToPrayerCheckpoint();return;}
                if(clock>=duration)Stop();
            }
            else TickLightning();
        }
        void FireLorquiana(float y)
        {
            pulses++;Vector2 start=(Vector2)origin+new Vector2(direction,y);const float range=15,width=2;
            // Source collisionMask is zero: terrain cannot shorten multishot.
            var beam=Create("Lorquiana beam "+pulses,start+Vector2.right*direction*range*.5f,"threeAnguish_spear_attackLine");
            if(beam.visual.sprite!=null)beam.transform.localScale=new Vector3(range/Mathf.Max(.01f,beam.visual.sprite.bounds.size.x),1,1);
            float damage=new InventoryModifiers(game.progress,game.player).PrayerDamage(90);
            Bounds area=new Bounds(start+Vector2.right*direction*range*.5f,new Vector3(range,width,2));
            foreach(var enemy in game.Current.enemies)if(PrayerCombat.Hit(area,enemy))
            {PrayerCombat.Damage(game,enemy,damage);Visual("Lorquiana impact",PrayerCombat.EnemyBounds(enemy).center,"lightning_projectileVanish",.8f);}
            PrayerCombat.Strike(game,area);game.Sfx("PRAYER_SHOT");
        }
        void TickDivineAreas(float dt)
        {
            float t=Mathf.Clamp01(clock),curve=1-(1-t)*(1-t);
            while(pulses<3&&pulses/3f<=curve)
            {
                pulses++;for(int side=0;side<2;side++)
                {
                    if(blockedDivineDirections[side])continue;float sign=side==0?-1:1;Vector2 at=(Vector2)origin+Vector2.right*(sign*1.25f*pulses);
                    if(Physics2D.Linecast(origin+Vector3.up*.05f,at+Vector2.up*.05f,1<<8).collider!=null){blockedDivineDirections[side]=true;continue;}
                    var ground=Physics2D.Raycast(at+Vector2.up*.02f,Vector2.down,10,(1<<8)|(1<<9));if(ground.collider!=null)at=ground.point;
                    var actor=Create("Taranto column "+side+"/"+pulses,at,"pontiff_lightningBolt_attack",true);actor.visual.flipX=UnityEngine.Random.value>.5f;
                    game.Sfx("TARANTO_THUNDER",.6f);divineAreas.Add(new DivineArea{position=at,actor=actor});
                }
            }
            float damage=new InventoryModifiers(game.progress,game.player).PrayerDamage(20);
            foreach(var area in divineAreas)
            {
                area.age+=dt;if(area.age>.5f)
                {if(!area.ending){area.ending=true;area.actor.Play("pontiff_lightningBolt_out",false,true);}if(area.actor.Progress>=1)area.actor.visual.enabled=false;continue;}
                if(area.age<area.nextHit)continue;area.nextHit+=.12f;Bounds box=new Bounds(area.position+Vector2.up*10,new Vector3(1.3f,20,2));
                foreach(var enemy in game.Current.enemies)if(PrayerCombat.Hit(box,enemy))PrayerCombat.Damage(game,enemy,damage);
                PrayerCombat.Strike(game,box);
            }
            if(clock>=duration)Stop();
        }
        void FollowCompanion(float dt,bool guardian)
        {
            Vector3 target=game.player.transform.position+new Vector3(game.player.facing*2,guardian?0:3,0);target.y+=Mathf.Sin(clock*1.5f)*.01f;
            companion.transform.position=Vector3.SmoothDamp(companion.transform.position,target,ref followVelocity,.34f,Mathf.Infinity,dt);
            if(companion.visual.flipX!=(game.player.facing<0)&&phase==1)
            {phase=6;phaseClock=0;direction=game.player.facing;companion.Face(direction);companion.Play(guardian?"auroraGuardia_quickTurn":"miriamPortal_quickTurn",false,true);game.Sfx(guardian?"GUARDIAN_TURN":"MIRIAM_TURN");}
        }
        void TickCompanion(float dt)
        {
            bool guardian=prayer=="PR101";if(game.progress.equippedPrayer!=prayer){Stop();return;}
            if(guardian&&clock>=duration&&phase!=3){BeginVanish(true);companion.Advance(clock-duration);}
            if(companion==null)return;
            if(phase==0&&companion.Progress>=.75f){phase=1;phaseClock=0;companion.Play(guardian?"auroraGuardia_idle":"miriamPortal_idle",true,true);}
            if(phase==1)FollowCompanion(dt,guardian);
            else if(phase==6&&companion.Progress>=1){phase=1;companion.Play(guardian?"auroraGuardia_idle":"miriamPortal_idle",true,true);}
            if(phase==2)
            {
                float t=Mathf.Clamp01(phaseClock/Mathf.Max(.01f,actionSeconds));
                if(guardian)companion.transform.position=Vector3.Lerp(actionStart,actionEnd,1-Mathf.Cos(t*Mathf.PI*.5f));
                else
                {
                    // Native DOTween choreography: rise one unit in 0.1 s
                    // (InQuad), descend independently (InQuad), and travel
                    // horizontally for descent + 0.1 s (OutQuad).
                    float vertical=phaseClock<.1f?Mathf.Lerp(actionStart.y,actionStart.y+1,Mathf.Pow(Mathf.Clamp01(phaseClock/.1f),2)):
                        Mathf.Lerp(actionStart.y+1,actionEnd.y,Mathf.Pow(Mathf.Clamp01((phaseClock-.1f)/descentSeconds),2));
                    companion.transform.position=new Vector3(Mathf.Lerp(actionStart.x,actionEnd.x,1-(1-t)*(1-t)),vertical,actionStart.z);
                    if(!animationResumed&&phaseClock>=descentSeconds){animationResumed=true;companion.Paused=false;}
                    if(t>=1&&!reachedGround)
                    {
                        reachedGround=true;companion.Paused=false;landingClock=clock;
                        if(Physics2D.Raycast((Vector2)actionEnd+Vector2.up*.05f,Vector2.down,.3f,(1<<8)|(1<<9)).collider!=null)
                        {
                            Visual("Miriam left landing",actionEnd+new Vector3(-.7f,-.6f),"landing_effects_anim",.8f);
                            Visual("Miriam right landing",actionEnd+new Vector3(.7f,-.6f),"landing_effects_anim",.8f);
                            game.Shake(.2f);shardOrigin=actionEnd;shardClock=0;shardCount=0;
                            shardDamage=Mathf.Max(0,40*new InventoryModifiers(game.progress,game.player).Value(15,1));
                        }
                    }
                }
                if(companion.Progress>=1){if(guardian){phase=1;companion.Play("auroraGuardia_idle",true,true);}else BeginVanish(false);}
            }
            else if(phase==4)
            {
                float t=Mathf.Clamp01(phaseClock/Mathf.Max(.01f,actionSeconds));companion.transform.position=Vector3.Lerp(actionStart,actionEnd,1-Mathf.Cos(t*Mathf.PI*.5f));
                if(companion.Progress>=1){phase=5;companion.Play("auroraGuardia_block2",false,true);}
            }
            else if(phase==5&&companion.Progress>=1){phase=1;companion.Play("auroraGuardia_idle",true,true);}
            if(!guardian&&landingClock>=0)
            {
                float finish=Mathf.Clamp01(clock-landingClock);Vector3 at=companion.transform.position;
                at.y=Mathf.Lerp(actionEnd.y,actionEnd.y-.3f,finish*finish);companion.transform.position=at;
            }
            // Miriam's weapon queries its moving polygon each Update between
            // the two animation events and remembers every struck target.
            if(!guardian&&miriamHitWindow&&phase==2)HitCompanion(false);
            TickShards(dt);
            if(phase==3&&companion.Progress>=1)
            {
                companion.visual.enabled=false;
                // Ground pillars finish after the portal disappears.
                if(guardian||shardClock<0||shardCount>=12&&shards.TrueForAll(s=>s.ending&&s.actor.Progress>=1))Stop();
            }
        }
        bool ReadyCompanion()=>companion!=null&&phase==1&&cooldown<=0&&companion.visual.flipX==(game.player.facing<0);
        public void PlayerAttack(){if((prayer=="PR101"||prayer=="PR201")&&ReadyCompanion())AttackCompanion();}
        public void PlayerParry()
        {
            if(prayer!="PR101"||!ReadyCompanion())return;phase=4;phaseClock=0;cooldown=.7f;direction=game.player.facing;
            actionStart=companion.transform.position;actionEnd=actionStart+Vector3.right*direction;actionSeconds=.1f;
            companion.Face(direction);companion.Play("auroraGuardia_block1",false,true);game.Sfx("GUARDIAN_PARRY");
        }
        public bool BlocksAttack(float fromX,bool contact)
        {
            if(!GuardianGuarding)return false;
            // GuardianPrayerGuardState temporarily protects its master during
            // the authored guard action, covering attacks and contact.
            game.Shake(.05f);return true;
        }
        void AttackCompanion()
        {
            phase=2;phaseClock=0;cooldown=.7f;direction=game.player.facing;companion.Face(direction);actionStart=companion.transform.position;
            actionEnd=actionStart+Vector3.right*(direction*(prayer=="PR101"?2:5));actionSeconds=.2f;reachedGround=animationResumed=false;
            if(prayer=="PR201")
            {
                actionEnd.y-=2;var ground=Physics2D.Raycast((Vector2)actionEnd,Vector2.down,9,(1<<8)|(1<<9));if(ground.collider!=null)actionEnd.y=ground.point.y+.2f;
                descentSeconds=.1f+Mathf.Min(Mathf.Abs(actionEnd.y-actionStart.y)/9,1)*.4f;actionSeconds=descentSeconds+.1f;
                Visual("Miriam portal shattering",actionStart,"miriamPortal_shatter",.69f);game.Sfx("MIRIAM_ATTACK");
            }
            else game.Sfx("GUARDIAN_ATTACK");
            companion.Play(prayer=="PR101"?"auroraGuardia_attack":"miriamPortal_attack",false,true);
        }
        void BeginVanish(bool guardian)
        {phase=3;phaseClock=0;companion.Paused=false;companion.Play(guardian?"auroraGuardia_vanish":"miriamPortal_vanish",false,true);game.Sfx(guardian?"GUARDIAN_VANISH":"MIRIAM_VANISH");}
        void OnCompanionEvent(RestoredAnimationEvent animation)
        {
            if(prayer=="PR201"&&phase==2&&animation.functionName=="StopAnimator"&&!animationResumed){companion.Paused=true;return;}
            if(prayer=="PR201"&&animation.functionName=="WeaponAttackFinished"){miriamHitWindow=false;return;}
            if(animation.functionName!="WeaponAttack"||phase!=2)return;
            if(prayer=="PR201"){companionHits.Clear();miriamHitWindow=true;return;}
            HitCompanion(true);
        }
        void HitCompanion(bool guardian)
        {
            // Native Miriam CreateHit halves Strength (200 * .5), whereas
            // Guardian uses its complete authored Strength of 180.
            float damage=companionDamage;
            Vector2 center=(Vector2)companion.transform.position+new Vector2(direction*1.340443f,2.0174785f);
            Vector2[] polygon=null;
            if(!guardian)
            {
                polygon=new Vector2[MiriamPolygon.Length];
                for(int i=0;i<polygon.Length;i++)polygon[i]=(Vector2)companion.transform.position+new Vector2(MiriamPolygon[i].x*direction,MiriamPolygon[i].y);
            }
            foreach(var enemy in game.Current.enemies)
            {
                if(!guardian&&companionHits.Contains(enemy))continue;
                bool hit=guardian?PrayerCombat.Capsule(center,new Vector2(8.332628f,5.034984f),true,enemy):PrayerCombat.Polygon(polygon,enemy);
                if(!hit)continue;if(!guardian)companionHits.Add(enemy);PrayerCombat.Damage(game,enemy,damage,guardian);
                if(!guardian&&!enemy.boss&&!enemy.Dead)enemy.StunEnemy(SourceGameplayTuning.ExecutionStunTime);
                Visual(guardian?"Guardian hit":"Miriam hit",PrayerCombat.EnemyBounds(enemy).center,"Penitent_Attack_Spark1",.3f);
            }
            if(guardian)
            {
                PrayerCombat.StrikeCapsule(game,center,new Vector2(8.332628f,5.034984f),true);
                game.effects.InterceptProjectiles(bounds=>PrayerCombat.Capsule(center,new Vector2(8.332628f,5.034984f),true,bounds));
            }
            else
            {
                PrayerCombat.StrikePolygon(game,polygon);
                game.effects.InterceptProjectiles(bounds=>PrayerCombat.Polygon(polygon,bounds));
            }
        }
        void TickShards(float dt)
        {
            if(shardClock<0)return;shardClock+=dt;
            while(shardCount<12&&shardCount/12f<=Mathf.Clamp01(shardClock))
            {
                int index=shardCount++;Vector2 at=shardOrigin+Vector2.right*direction*(index+1);var ground=Physics2D.Raycast(at+Vector2.up*5,Vector2.down,14,(1<<8)|(1<<9));if(ground.collider!=null)at.y=ground.point.y+.2f;
                int variant=UnityEngine.Random.Range(0,4);string color=variant==0?"Blue":variant==1?"Green":variant==2?"Purple":"Red";
                var shard=new Shard{position=at,color=color,actor=Create("Miriam "+color+" shard",at,"MiriamSpike"+color+"WarningToAttack"),offset=variant==1?new Vector2(-.05f,1.2f):variant==3?new Vector2(.15f,1):new Vector2(0,1.3f),size=variant==1?new Vector2(.9f,2.7f):variant==3?new Vector2(1,2.4f):new Vector2(.7f,3)};
                shards.Add(shard);game.Sfx("MIRIAM_SHARD_IN",.35f);
            }
            float damage=shardDamage;
            foreach(var shard in shards)
            {
                shard.age+=dt;if(!shard.hit)
                {
                    shard.hit=true;Bounds box=new Bounds(shard.position+shard.offset,new Vector3(shard.size.x,shard.size.y,2));
                    foreach(var enemy in game.Current.enemies)if(PrayerCombat.Hit(box,enemy)){PrayerCombat.Damage(game,enemy,damage);if(!enemy.boss&&!enemy.Dead)enemy.StunEnemy(SourceGameplayTuning.ExecutionStunTime);}
                    PrayerCombat.Strike(game,box);
                }
                if(shard.age>=.9f&&!shard.ending){shard.ending=true;shard.actor.Play("MiriamSpike"+shard.color+"AttackFade",false,true);game.Sfx("MIRIAM_SHARD_OUT",.25f);}
                else if(!shard.ending&&shard.actor.Progress>=1&&shard.actor.Current.EndsWith("WarningToAttack"))shard.actor.Play("MiriamSpike"+shard.color+"AttackLoop",true,true);
                if(shard.ending&&shard.actor.Progress>=1)shard.actor.visual.enabled=false;
            }
        }
        void TickLightning()
        {
            float first=.75f,hideStart=first+(totalLightningPulses-1)*1.2f+.35f;
            for(int i=0;i<cores.Count;i++)
            {
                var core=cores[i];float show=i*.1f,hide=hideStart+i*.1f;
                if(clock>=hide&&core.Current!="SanTelmoCoreOut_anim"&&core.Current!="SanTelmoCoreHidden")
                {core.Play("SanTelmoCoreOut_anim",false,true);game.Sfx("ELM_FIRE_OUT",.3f);}
                else if(clock>=show&&clock<hide&&core.Current=="SanTelmoCoreHidden")
                {core.visual.enabled=true;core.Play("SanTelmoCoreIn_anim",false,true);game.Sfx("ELM_FIRE_IN",.3f);}
                else if(core.Current=="SanTelmoCoreIn_anim"&&core.Progress>=1)core.Play("SanTelmoCore_anim",true,true);
                if(core.Current=="SanTelmoCoreOut_anim"&&core.Progress>=1)core.visual.enabled=false;
            }
            // Seven reveal steps, then each link charges 0.05 s apart.
            while(pulses<totalLightningPulses&&clock>=first+pulses*1.2f)
            {float start=first+pulses*1.2f;pulses++;for(int i=0;i<beams.Count;i++){beamStarts[i]=start+i*.05f;beamLaunched[i]=false;beamHits[i].Clear();}game.Sfx("ELM_FIRE_CHARGE",.6f);}
            float damage=new InventoryModifiers(game.progress,game.player).PrayerDamage(10);
            for(int i=0;i<beams.Count;i++)
            {
                var beam=beams[i];float elapsed=clock-beamStarts[i];if(elapsed<0)continue;
                if(!beamLaunched[i]&&elapsed<.3f){beamLaunched[i]=true;beam.visual.enabled=true;beam.Play("SanTelmoLightning_anim",false,true);game.Sfx("ELM_FIRE_SHOT",.35f);}
                if(elapsed<=.3f)
                {
                    Vector2 from=TrapPoint(i),to=TrapPoint(i+1),delta=to-from,edge=new Vector2(-delta.y,delta.x).normalized*.5f;
                    var polygon=new[]{from-edge,to-edge,to+edge,from+edge};
                    foreach(var enemy in game.Current.enemies)if(!beamHits[i].Contains(enemy)&&PrayerCombat.Polygon(polygon,enemy)){beamHits[i].Add(enemy);PrayerCombat.Damage(game,enemy,damage);}
                    PrayerCombat.StrikeBeam(game,TrapPoint(i),TrapPoint(i+1),1);
                }
                if(beam.Progress>=1)beam.visual.enabled=false;
            }
            foreach(var enemy in game.Current.enemies)
            {
                if(enemy==null||enemy.Dead||coreHits.TryGetValue(enemy,out float next)&&clock<next)continue;
                for(int i=0;i<cores.Count;i++)
                {
                    if(clock<i*.1f+.88f||clock>=hideStart+i*.1f+.3f)continue;
                    if(PrayerCombat.Circle(TrapPoint(i),.5f,enemy)){coreHits[enemy]=clock+.3f;PrayerCombat.Damage(game,enemy,damage);break;}
                }
            }
            if(clock>=duration)Stop();
        }
        public void Stop()
        {
            if(companion!=null)companion.AnimationEvent-=OnCompanionEvent;
            foreach(var actor in actors)if(actor!=null){actor.gameObject.SetActive(false);Destroy(actor.gameObject);}
            actors.Clear();cores.Clear();beams.Clear();divineAreas.Clear();timedVisuals.Clear();shards.Clear();beamHits.Clear();coreHits.Clear();companionHits.Clear();miriamHitWindow=false;companion=null;prayer="";shardClock=-1;shardCount=0;
        }
        void OnDestroy(){Stop();}
    }
}
