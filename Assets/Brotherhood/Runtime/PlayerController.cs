using UnityEngine;
namespace Brotherhood
{
    public sealed class PlayerController : MonoBehaviour
    {
        public KinematicMotor motor; public SpriteActor actor; public BrotherhoodGame game;
        public float health=88,fervour; public int flasks=2; public float facing=1;
        InventoryModifiers modifiers;InventoryModifiers Mods=>modifiers!=null&&ReferenceEquals(modifiers.Progress,game.progress)?modifiers:(modifiers=new InventoryModifiers(game.progress,this));
        public float MaxHealth=>Mods.MaxLife;public float MaxFervour=>Mods.MaxFervour;public int MaxFlasks=>Mods.MaxFlasks;
        public bool Dead=>health<=0; public bool Parrying=>parryState==1||parryState==2;
        public bool Executing=>executionTime>0;
        public bool ExecutionRewardContext{get;private set;}
        public bool HeavyRewardContext{get;private set;}
        public bool PenanceCasting=>penanceTime>0;
        public bool VerticalCasting=>verticalStartTime>0||plunging;
        public string ActivePrayer=>activePrayer;
        public float PrayerTimeRemaining=>Mathf.Max(0,prayerTime);
        public static bool PrayerImplemented(string id)=>id=="PR01"||id=="PR03"||id=="PR04"||id=="PR05"||id=="PR07"||id=="PR08"||id=="PR09"||id=="PR10"||id=="PR11"||id=="PR12"||id=="PR14"||id=="PR15"||id=="PR16"||id=="PR101"||id=="PR201"||id=="PR202"||id=="PR203";
        string pendingPrayer;float prayerCastClock,prayerCastSpeed;bool prayerLaunched;
        public bool PrayerCasting=>!string.IsNullOrEmpty(pendingPrayer);
        public float attackTime, dashTime;
        float collectClock,altarClock;int altarPhase;
        float invincible,hurtFlash,lockTime,jumpBuffer,coyote,comboBuffer,healTime,deathTime,charge,attackHoldClock,stepTime,dashCooldown,climbTime,executionTime,dashGhostClock,dashFxClock=-1,locomotionLock,ladderStep,fallPeak,lungeTime,chargedClock,throwbackTime,rangeAttackClock,prayerTime,prayerPulse;string activePrayer;
        int parryState; float parryTimer, slowMotionTimer; bool riposteHit, retributionTriggered; int prieDieuPhase, prieDieuMode;
        Vector2 climbStart,climbEnd,executionExitPosition;bool plunging,laddering,dashEndDust;float interactHold;bool interactionDone;
        bool prieDieuPrayer,prieDieuHoldRequired;float prieDieuTimer;
        int combo,ladderSoundIndex,airImpulses; bool hit,healPending,wasGrounded,lungeHit,attackHoldArmed,chargePending,chargeLoaded,chargedAttack,chargedEvent,throwbackLanded; float attackDuration,previousMove,attackVertical;
        float specialHoldClock,penanceTime;bool specialHoldConsumed;
        float verticalHoldClock,verticalStartTime,verticalHeight,healingProtectionTime;int verticalAttackTier;
        static readonly string[] AttackClips={"Player_Clamped_Attack_NoSlashes_1","Player_Clamped_Attack_NoSlashes_2","Player_Clamped_Attack_NoSlashes_3","Player_ComboFinisher_Down"};
        readonly EnemyController[] hitTargets=new EnemyController[32];int hitCount;
        public void Tick(float dt)
        {
            bool penanceRequested=ReadPenanceInput(dt,out bool specialTap);
            healingProtectionTime=Mathf.Max(0,healingProtectionTime-dt);
            if(game.itemEffects!=null)game.itemEffects.Tick(dt);
            if(game.prayerEffects!=null)game.prayerEffects.Tick(dt);
            if(game.legacyPrayers!=null)game.legacyPrayers.Tick(dt);
            if(game.prayerBuffs!=null)game.prayerBuffs.Tick(dt);
            if(prayerTime>0){prayerTime=Mathf.Max(0,prayerTime-dt);if(prayerTime<=0)EndPrayer();}
            if(PrayerCasting)TickPrayerCast(dt);
            if(slowMotionTimer>0){slowMotionTimer-=Time.unscaledDeltaTime;if(slowMotionTimer<=0)Time.timeScale=1f;}
            if(collectClock>0){collectClock-=dt;motor.velocity=Vector2.zero;if(collectClock<=0)actor.Play("Player_Idle",true,true);return;}
            if(altarPhase!=0)
            {
                motor.velocity=Vector2.zero;
                if(altarPhase==1&& (altarClock-=dt)<=0){altarPhase=2;actor.Play("penitent_kneeled",true,true);game.controls.inventoryUI.Show(5);}
                else if(altarPhase==3&&(altarClock-=dt)<=0){altarPhase=0;actor.Play("Player_Idle",true,true);}
                return;
            }
            if(executionTime>0)
            {
                // Mobile Execution drives the visible composite from the enemy's
                // interactable and keeps the real Penitent locked at its endpoint.
                executionTime-=dt;motor.velocity=Vector2.zero;
                if(executionTime<=0){motor.Teleport(executionExitPosition);motor.TrySetHeight(1.15f);lockTime=0;actor.visual.enabled=true;actor.Play("Player_Idle",true,true);game.EndExecutionCamera();}
                return;
            }
            if(Dead) {deathTime-=dt;if(deathTime<=0)game.ShowGameOver();return;}
            if(verticalStartTime>0)
            {
                verticalStartTime=Mathf.Max(0,verticalStartTime-dt);motor.velocity=Vector2.zero;
                if(verticalStartTime<=0){plunging=true;motor.velocity.y=-22;actor.Play("penitent_verticalattack_falling_anim",true,true);}
                return;
            }
            if(penanceTime>0)
            {
                penanceTime=Mathf.Max(0,penanceTime-dt);lockTime=penanceTime;
                invincible=Mathf.Max(0,invincible-dt);hurtFlash=Mathf.Max(0,hurtFlash-dt);
                motor.velocity=Vector2.zero;motor.Step(dt);
                if(penanceTime<=0)actor.Play("Player_Idle",true,true);
                return;
            }
            if(prieDieuPrayer)
            {
                var inpt=game.controls;
                bool cancelInput=inpt.Parry||inpt.Attack||inpt.Dash||inpt.Jump||inpt.Flask||Mathf.Abs(inpt.Move)>.15f;
                bool releaseHold=prieDieuHoldRequired&&!inpt.InteractHeld&&!inpt.Interact;
                if((prieDieuPhase==0&&cancelInput)||releaseHold)
                {
                    prieDieuPrayer=false;
                    prieDieuPhase=0;
                    prieDieuTimer=0;
                    lockTime=0;
                    actor.Play("Player_Idle",true,true);
                }
                else
                {
                    motor.velocity=Vector2.zero;
                    if(prieDieuPhase==0)
                    {
                        prieDieuTimer+=dt;
                        if(prieDieuTimer>=.70f)
                        {
                            game.Interact(false,true);
                            game.Sfx("HEALING_EXPLOSION");
                            game.effects.Burst(transform.position+Vector3.up,new Color(1f,.85f,.35f));
                            if(prieDieuMode==1)
                            {
                                prieDieuPhase=10;
                            }
                            else
                            {
                                prieDieuPhase=1;
                                prieDieuTimer=0;
                                actor.Play("penitent_priedieu_bended_knee_with_aura_anim",true,true);
                            }
                        }
                    }
                    else if(prieDieuPhase==10)
                    {
                        prieDieuTimer+=dt;
                        if(prieDieuTimer>=2.16f)
                        {
                            prieDieuPrayer=false;
                            prieDieuPhase=0;
                            prieDieuTimer=0;
                            lockTime=0;
                            actor.Play("Player_Idle",true,true);
                        }
                    }
                    else if(prieDieuPhase==1)
                    {
                        prieDieuTimer+=dt;
                        if(prieDieuTimer>=.30f&&(cancelInput||inpt.Interact))
                        {
                            prieDieuPhase=2;
                            prieDieuTimer=0;
                            actor.Play("penitent_priedieu_bended_knee_aura_turnoff_anim",false,true);
                            game.Sfx("CHECKPOINT_KNEE_END");
                        }
                    }
                    else if(prieDieuPhase==2)
                    {
                        prieDieuTimer+=dt;
                        if(prieDieuTimer>=.24f)
                        {
                            prieDieuPhase=3;
                            prieDieuTimer=0;
                            actor.Play("penitent_priedieu_stand_up_anim",false,true);
                        }
                    }
                    else if(prieDieuPhase==3)
                    {
                        prieDieuTimer+=dt;
                        if(prieDieuTimer>=.40f)
                        {
                            prieDieuPrayer=false;
                            prieDieuPhase=0;
                            prieDieuTimer=0;
                            lockTime=0;
                            actor.Play("Player_Idle",true,true);
                        }
                    }
                    motor.Step(dt);
                    return;
                }
            }
            if(parryState>0)
            {
                parryTimer-=dt;
                var inpt=game.controls;
                if((parryState==4||parryState==5)&&(inpt.Attack||inpt.AttackHeld))
                {
                    retributionTriggered=true;
                }
                if(parryState==1&&parryTimer<=0)
                {
                    parryState=2;
                    parryTimer=Mods.ParryWindow;
                    actor.Play("penitent_parry_chance_time",true,true);
                }
                else if(parryState==2&&parryTimer<=0)
                {
                    parryState=3;
                    parryTimer=.16f;
                    actor.Play("penitent_parry_failed",false,true);
                }
                else if(parryState==3&&parryTimer<=0)
                {
                    parryState=0;
                    lockTime=0;
                }
                else if(parryState==4&&parryTimer<=0)
                {
                    parryState=5;
                    parryTimer=.72f;
                    riposteHit=false;
                    motor.velocity.x=facing*4.5f;
                    actor.Play("penitent_ParryStab_anim",false,true);
                    game.Sfx("PENITENT_DASH",.7f);
                }
                else if(parryState==5)
                {
                    if(!riposteHit&&parryTimer<=.45f)
                    {
                        riposteHit=true;
                        if(retributionTriggered)
                        {
                            HitRetribution(3.2f,Mods.DamageDealt(98f));
                            game.Shake(.35f);
                            game.Sfx("PENITENT_ACTIVATE_PRAYER");
                            game.Sfx("CHARGED_ATTACK_PROJECTILE");
                            game.Sfx("PENITENT_PARRY_HIT");
                            game.effects.Animation("penitent_verticalattack_landing_effects_LVL3_anim",transform.position+Vector3.right*facing*1.6f,facing);
                            game.effects.Burst(transform.position+Vector3.right*facing*1.6f+Vector3.up,Color.cyan);
                            game.effects.Burst(transform.position+Vector3.right*facing*1.6f+Vector3.up*1.5f,Color.white);
                            game.effects.Animation("penitent_charged_attack_effect",transform.position+Vector3.right*facing*1.4f,facing);
                            game.Message("RETRIBUTION!");
                        }
                        else
                        {
                            HitRiposte(2.5f,Mods.DamageDealt(52f));
                            game.Shake(.18f);
                            game.Sfx("PENITENT_PARRY_HIT");
                            game.effects.Burst(transform.position+Vector3.right*facing*1.4f+Vector3.up,Color.yellow);
                        }
                    }
                    if(parryTimer<=0)
                    {
                        parryState=0;
                        lockTime=0;
                        retributionTriggered=false;
                    }
                }
            }
            invincible=Mathf.Max(0,invincible-dt);hurtFlash=Mathf.Max(0,hurtFlash-dt);lockTime=Mathf.Max(0,lockTime-dt);
            locomotionLock=Mathf.Max(0,locomotionLock-dt);
            var input=game.controls;
            var mud=game.CurrentMud;
            float vertical=input.Vertical;
            dashCooldown=Mathf.Max(0,dashCooldown-dt);
            // The restored dodge clip raises StopDust at 0.54 s. Keep this event on
            // the animation timeline even though the physical dash ends at 0.35 s.
            if(dashFxClock>=0){dashFxClock+=dt;if(!dashEndDust&&dashFxClock>=SourceGameplayTuning.DashStopDustEventTime){game.effects.Animation("Penitent_stop_running_dust",transform.position+Vector3.down*0.05f,facing);game.effects.Animation("Penitent_Landing_Dust",transform.position+Vector3.down*0.05f,facing);dashEndDust=true;dashFxClock=-1;}}
            if(climbTime>0)
            {
                climbTime=Mathf.Max(0,climbTime-dt);
                // The source ledge clip lasts 0.34 s. Raise the feet beside the
                // wall before crossing its top; a diagonal lerp passes the body
                // through the foreground soil for most of the climb.
                float t=1f-climbTime/.34f;
                Vector2 at=t<.65f
                    ?new Vector2(climbStart.x,Mathf.Lerp(climbStart.y,climbEnd.y,Mathf.Clamp01(t/.65f)))
                    :new Vector2(Mathf.Lerp(climbStart.x,climbEnd.x,Mathf.Clamp01((t-.65f)/.35f)),climbEnd.y);
                motor.Teleport(at);
                if(climbTime<=0){motor.grounded=true;wasGrounded=true;}
                return;
            }
            if(input.Jump)jumpBuffer=.14f;else jumpBuffer-=dt;
            coyote=motor.grounded?.12f:coyote-dt;
            if(input.Interact&&game.deograciasEncounter!=null&&game.deograciasEncounter.CanInteract){game.Interact(false,false);return;}
            if(input.Interact&&game.guilt!=null&&game.guilt.TryRecover())return;
            if(input.Interact&&game.world!=null&&game.world.TryInteract())return;
            if(input.Interact&&game.TryUseSkillAltar())return;
            if(input.Interact&&game.TryCollectItem())return;
            if(input.Interact&&TryExecution())return;
            if((input.Interact||input.InteractHeld)&&game.CanPray&&lockTime<=0&&!prieDieuPrayer)
            {
                var nearShrine=game.GetNearShrine();
                bool isLit=game.IsShrineLit(nearShrine);
                prieDieuMode=isLit?0:1;
                prieDieuPrayer=true;prieDieuHoldRequired=input.InteractHeld;prieDieuPhase=0;prieDieuTimer=0;lockTime=99f;
                if(nearShrine!=null)
                {
                    float dir=Mathf.Sign(nearShrine.position.x-transform.position.x);
                    if(dir!=0){facing=dir;actor.Face(facing);}
                }
                actor.Play(prieDieuMode==1?"penitent_priedieu_action_lightning_shrine_anim":"penitent_priedieu_kneeling_anim",false,true);
                game.Sfx(prieDieuMode==1?"CHECKPOINT_ACTIVATION":"CHECKPOINT_KNEE_START");
                return;
            }
            if(game.Current.OnLadder(transform.position,out var ladder)&&(laddering||Mathf.Abs(vertical)>.15f))laddering=true;
            if(laddering)
            {
                if(input.Jump){laddering=false;actor.Paused=false;motor.velocity.y=8;}
                else if(game.Current.OnLadder(transform.position,out ladder))
                {
                    float x=Mathf.MoveTowards(transform.position.x,ladder.center.x,dt*5);
                    Vector2 ladderPosition=new Vector2(x,transform.position.y+vertical*3.2f*dt);bool trialLanded=false;
                    if(game.trials!=null)ladderPosition=game.trials.ClampLadderStep(ladder,ladderPosition,vertical,out trialLanded);
                    motor.Teleport(ladderPosition);
                    if(trialLanded){laddering=false;actor.Paused=false;motor.Step(dt);return;}
                    if(Mathf.Abs(vertical)>.1f)
                    {
                        actor.Paused=false;
                        actor.Play(vertical<-.1f?"penitent_ladder_going_down":"penitent_ladder_going_up",true);
                    }
                    else
                    {
                        actor.Paused=true;
                    }
                    ladderStep-=dt;if(Mathf.Abs(vertical)>.15f&&ladderStep<=0){game.Sfx("PENITENT_CLIMB_LADDER_"+(ladderSoundIndex++%3+1),.65f);ladderStep=.34f;}
                    return;
                }
                else {laddering=false;actor.Paused=false;}
            }
            if(penanceRequested&&!game.InputBlocked&&motor.grounded&&!laddering&&lockTime<=0&&attackTime<=0&&dashTime<=0&&lungeTime<=0&&parryState==0&&!chargePending)
            {
                if(TryStartFervourPenance())return;
            }
            if(input.Flask && !game.progress.IsEquipped("HE06") && game.progress.hasFlask && flasks>0 && (game.progress.IsEquipped("RB103")?fervour<MaxFervour:health<MaxHealth) && lockTime<=0 && motor.grounded && !chargePending)
            {flasks--;healPending=true;float speed=game.itemEffects!=null?game.itemEffects.AnimationSpeed("QuickHealingBeadEffect"):1;healTime=.7f/speed;lockTime=1/speed;healingProtectionTime=game.progress.IsEquipped("RB28")?lockTime:0;actor.Play("penitent_healthpotion_consuming_anim",false,true,speed);ItemEvent(11);game.effects.Animation("penitent_healthpotion_consuming_aura_anim",transform.position,facing);game.Sfx("USE_FLASK");}
            if(healPending) {healTime-=dt;if(healTime<=0){if(game.progress.IsEquipped("RB103"))fervour=Mathf.Min(MaxFervour,fervour+Mods.FlaskHealing);else{health=Mathf.Min(MaxHealth,health+Mods.FlaskHealing);if(game.itemEffects!=null)game.itemEffects.BeginFlaskRecovery();}healPending=false;game.Sfx("HEALING_EXPLOSION");game.effects.Burst(transform.position+Vector3.up,new Color(.9f,.75f,.25f));game.Message("Bile flask · restored");}}
            if(input.Parry && game.progress.canParry && lockTime<=0 && attackTime<=0 && !chargePending && parryState==0)
            {
                parryState=1;
                if(game.prayerEffects!=null)game.prayerEffects.PlayerParry();
                parryTimer=0.09f;
                lockTime=.44f;
                actor.Play("penitent_start_parry",false,true);
            }
            if(input.Prayer&&game.progress.hasPrayer&&lockTime<=0&&!chargePending&&!PrayerCasting&&prayerTime<=0&&motor.grounded)
            {
                if(!PrayerImplemented(game.progress.equippedPrayer))game.Message("Chưa chọn kinh cầu nguyện");
                else if(game.prayerEffects!=null&&!game.prayerEffects.CanCast(game.progress.equippedPrayer))game.Message("Cần cầu nguyện tại Prie Dieu trước");
                else if(fervour<Mods.PrayerCost){game.Message("NOT ENOUGH FERVOUR ("+Mathf.CeilToInt(fervour)+"/"+Mathf.CeilToInt(Mods.PrayerCost)+")");game.Sfx("PENITENT_PARRY_HIT",.5f);}
                else StartPrayerCast();
            }
            if(specialTap&&lockTime<=0&&attackTime<=0&&!chargePending)
            {
                var progress=game.progress;
                if(!progress.hasSpecial||(progress.rangedTier<=0&&progress.lungeTier<=0))
                    game.Message("Giữ nút để đổi máu lấy Fervour · Phóng kiếm cần Mea Culpa cấp 2 và 2.000 Tears");
                else if(progress.specialMode==1&&progress.rangedTier>0)
                {
                    if(fervour>=Mods.RangedCost)StartRangeAttack();
                    else game.Message("NOT ENOUGH FERVOUR ("+Mathf.CeilToInt(fervour)+"/"+Mathf.CeilToInt(Mods.RangedCost)+")");
                }
                else if(progress.lungeTier>0)StartLunge();
            }
            if(rangeAttackClock>0){rangeAttackClock-=dt;if(rangeAttackClock<=0)game.effects.RangeProjectile(transform.position+Vector3.right*facing*.7f+Vector3.up*.65f,facing,game,Mods.RangedDamage(32+game.progress.rangedTier*6),game.progress.rangedTier);}
            if(input.Dash && game.progress.canDash && lockTime<=0 && dashCooldown<=0 && attackTime<=0 && !chargePending){dashTime=SourceGameplayTuning.DashRide*(1+Mods.Bonus(6));dashCooldown=SourceGameplayTuning.DashCooldown*Mods.DashCooldownMultiplier;dashGhostClock=0;dashFxClock=0;dashEndDust=false;motor.TrySetHeight(SourceGameplayTuning.DashCollisionSize.y);actor.Play("penitent_dodge_anim",false,true);game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",transform.position+Vector3.down*0.05f,-facing);game.effects.Animation("Penitent_Running_Dust",transform.position+Vector3.down*0.05f,facing);game.Sfx("PENITENT_DASH");}
            // Resolve a simultaneous Jump+Attack as an airborne slash. Before
            // this, Attack set attackTime first and prevented the jump below.
            if(input.Jump&&input.Attack&&!input.Down)TryStartJump(mud,input.Move);
            if(input.Attack && game.progress.hasMeaCulpa && !(input.Down&&!motor.grounded))
            {
                if(dashTime>0&&game.progress.lungeTier>0)StartLunge();
                else if(attackTime>0)comboBuffer=.5f;
                else if(lockTime<=0&&!chargePending)
                {
                    StartAttack(0,vertical);
                    attackHoldArmed=game.progress.chargedTier>0&&input.AttackHeld;
                    attackHoldClock=0;
                }
            }
            // The source input only sets IsAttackButtonHold after 0.5 seconds.
            // A tap must play the ordinary slash without even one charging frame.
            if(attackHoldArmed)
            {
                if(!input.AttackHeld){attackHoldArmed=false;attackHoldClock=0;}
                else if(!chargePending)
                {
                    attackHoldClock+=dt;
                    if(attackHoldClock>=SourceGameplayTuning.AttackHoldThreshold&&attackTime<=0&&lockTime<=0&&dashTime<=0&&lungeTime<=0&&parryState==0)
                    {
                        attackHoldArmed=false;chargePending=true;chargeLoaded=false;charge=0;comboBuffer=0;
                        actor.Play("penitent_start_charging",false,true);
                        game.Sfx("LOADING_CHARGED_ATTACK",.65f);
                    }
                }
            }
            if(chargePending&&attackTime<=0)
            {
                if(input.AttackHeld)
                {
                    charge+=dt;
                    var startCharging=actor.catalog.Find("penitent_start_charging");
                    if(actor.Current=="penitent_start_charging"&&startCharging!=null&&charge>=startCharging.duration)
                        actor.Play("penitent_charging",true,true);
                    float need=game.progress.chargedTier>1?SourceGameplayTuning.ChargedTier2Time:SourceGameplayTuning.ChargedTier1Time;
                    if(!chargeLoaded&&charge>=need)
                    {
                        chargeLoaded=true;
                        // This is an overlay effect, not a replacement for the Penitent's charging frames.
                        game.effects.Animation("penitent_charged_attack_effect",transform.position,facing);
                        game.Sfx("LOADED_CHARGED_ATTACK");
                    }
                }
                else
                {
                    chargePending=false;
                    game.effects.StopClip("penitent_charged_attack_effect");
                    if(chargeLoaded)StartChargedAttack();else actor.Play("Player_Idle",true,true);
                    charge=0;chargeLoaded=false;
                }
            }
            if(input.Down&&input.AttackHeld&&!motor.grounded&&motor.velocity.y<=.1f&&game.progress.VerticalLevel>0&&!plunging&&lockTime<=0&&attackTime<=0&&!chargePending)
            {
                verticalHoldClock+=dt;
                if(verticalHoldClock>=SourceGameplayTuning.VerticalHoldThreshold&&TryStartVerticalAttack())return;
            }
            else verticalHoldClock=0;
            if(attackTime>0)
            {
                attackTime-=dt;
                if(chargedAttack){chargedClock+=dt;if(!chargedEvent&&chargedClock>=SourceGameplayTuning.ChargedHitEventTime){chargedEvent=true;game.Sfx("CHARGED_ATTACK_PROJECTILE");HitCharged();game.Shake(.12f);}if(attackTime<=0)chargedAttack=false;}
                float progress=1-attackTime/attackDuration;
                if(!chargedAttack&&progress>.22f && progress<.72f)HitEnemies();
                if(attackTime<=0 && comboBuffer>0 && combo<(game.progress.ComboLevel>0?3:2)){StartAttack(combo+1,vertical);comboBuffer=0;}
            }
            comboBuffer-=dt;
            float move=input.Move;
            if(Mathf.Abs(move)>.1f && attackTime<=0 && lockTime<=0){facing=Mathf.Sign(move);actor.Face(facing);}
            if(throwbackTime>0){throwbackTime-=dt;if(throwbackLanded)motor.velocity.x=0;}
            else if(lungeTime>0){lungeTime-=dt;motor.velocity.x=facing*SourceGameplayTuning.LungeSpeed(game.progress.lungeTier);motor.velocity.y=0;if(!lungeHit)HitLunge();}
            else if(dashTime>0)
            {
                dashTime-=dt;dashFxClock+=dt;dashGhostClock-=dt;
                if(dashGhostClock<=0){game.effects.Ghost(actor.visual);dashGhostClock=.055f;}
                motor.velocity.x=facing*(mud!=null?mud.dashSpeed:SourceGameplayTuning.DashSpeed);motor.velocity.y=0;
                if(!dashEndDust && (dashFxClock>=SourceGameplayTuning.DashStopDustEventTime || dashTime<=0))
                {
                    dashEndDust=true;
                    game.effects.Animation("Penitent_stop_running_dust",transform.position+Vector3.down*0.05f,facing);
                    game.effects.Animation("Penitent_Landing_Dust",transform.position+Vector3.down*0.05f,facing);
                }
            }
            else {bool canStand=motor.TrySetHeight(1.15f);float target=lockTime>0 || attackTime>0 || chargePending ? 0:move*(mud!=null?Mathf.Min(Mods.MoveSpeed,mud.maxWalkSpeed):Mods.MoveSpeed);motor.velocity.x=mud!=null&&target!=0?Mathf.MoveTowards(motor.velocity.x,target,mud.walkAcceleration*dt):mud!=null?Mathf.MoveTowards(motor.velocity.x,0,mud.walkDrag*dt):target;if(!canStand){/* Stay in the source dash-height capsule, but allow moving out from under a ledge. */if(!chargePending)actor.Play("penitent_dodge_anim",false);locomotionLock=.08f;}}
            if(input.Jump&&input.Down&&motor.grounded&&!chargePending){motor.dropThrough=.4f;motor.Teleport((Vector2)transform.position+Vector2.down*.08f);jumpBuffer=0;}
            TryStartJump(mud,move);
            if(input.Jump&&!motor.grounded&&lockTime<=0&&attackTime<=0&&!chargePending&&motor.Ledge(facing,out var ledge)){climbStart=transform.position;climbEnd=ledge;climbTime=.34f;actor.Play("Player_Climb_Edge_new",false,true);return;}
            if(!input.JumpHeld && motor.velocity.y>4)motor.velocity.y=Mathf.MoveTowards(motor.velocity.y,4,dt*35);
            bool groundedBefore=wasGrounded;float impactSpeed=motor.velocity.y;
            float beforeStepX=transform.position.x;motor.Step(dt);
            // A held direction may still point into a wall or the arrival door.
            // Animate footsteps only when the motor actually changes position.
            bool movedHorizontally=Mathf.Abs(transform.position.x-beforeStepX)>.001f;
            if(!motor.grounded)fallPeak=Mathf.Min(fallPeak,motor.velocity.y);
            if(!groundedBefore&&motor.grounded){if(throwbackTime>0){throwbackLanded=true;throwbackTime=.45f;motor.velocity.x=0;actor.Play("penitent_throwback_ground_contact_anim",false,true);game.effects.Animation("penitent_throwback_ground_contact_dust_anim",transform.position,facing);game.Sfx("HARD_LANDING");game.Shake(.12f);locomotionLock=.45f;}else{bool hard=impactSpeed<-13||fallPeak<-13;float recoverySpeed=hard&&game.itemEffects!=null?game.itemEffects.AnimationSpeed("HardLandingBeadEffect"):1;actor.Play(hard?"penitent_hardlanding_rocks_anim":Mathf.Abs(move)>.1f?"Player_Landing_Running":"Player_Landing",false,true,recoverySpeed);game.Sfx(hard?"HARD_LANDING":Mathf.Abs(move)>.1f?"PENITENT_LANDING_RUNNING":"PENITENT_JUMP_FALL_STONE");game.effects.Animation("Penitent_Running_Dust",transform.position,facing);locomotionLock=hard?.45f/recoverySpeed:.12f;}fallPeak=0;}
            wasGrounded=motor.grounded;if(motor.grounded)airImpulses=0;
            if(plunging&&motor.grounded){plunging=false;LandVerticalAttack();}
            stepTime-=dt;if(motor.grounded&&movedHorizontally&&Mathf.Abs(motor.velocity.x)>1&&stepTime<=0){game.Sfx("PENITENT_RUN_STONE_3",.4f);stepTime=.28f;}
            if(lockTime<=0 && attackTime<=0 && !chargePending && dashTime<=0 && lungeTime<=0 && throwbackTime<=0 && !plunging && locomotionLock<=0)
            {
                if(!motor.grounded)actor.Play(motor.velocity.y>0?"Player_Jump":"Player_Fall",true);
                else if(input.Down && Mathf.Abs(move)<=.1f)
                {
                    motor.TrySetHeight(.85f);
                    if(actor.Current=="Player_crouch_attack_noslashes")
                    {
                        actor.PlayHoldLastFrame("Player_crouch_down");
                    }
                    else
                    {
                        actor.Play("Player_crouch_down",false,false);
                    }
                }
                else if(Mathf.Abs(move)>.1f && !movedHorizontally){motor.TrySetHeight(1.15f);actor.Play("Player_Idle",true,true);}
                else if(Mathf.Abs(move)>.1f){motor.TrySetHeight(1.15f);if(Mathf.Abs(previousMove)<=.1f){actor.Play("Player_Run_Start",false,true);locomotionLock=.08f;}else actor.Play("Player_Run",true);}
                else if(Mathf.Abs(previousMove)>.1f){motor.TrySetHeight(1.15f);actor.Play("Player_Run_Stop",false,true);game.Sfx("PENITENT_RUNSTOP_STONE");game.effects.Animation("Penitent_Running_Dust",transform.position,facing);locomotionLock=.14f;}
                else if(actor.Current=="Player_crouch_down")
                {
                    motor.TrySetHeight(1.15f);
                    actor.Play("Player_crouch_up_anim",false,true);
                    locomotionLock=.18f;
                }
                else if(actor.Current!="penitent_dodge_anim" || actor.Progress>=1f) {motor.TrySetHeight(1.15f);actor.Play("Player_Idle",true);}
            }
            previousMove=movedHorizontally?move:0f;
            if(actor.Current=="Player_Run")actor.Speed=mud!=null?.7f:1f;
            actor.visual.color=hurtFlash>0 && Mathf.FloorToInt(Time.time*20)%2==0?new Color(1,.5f,.5f,.4f):(game.legacyPrayers!=null?game.legacyPrayers.PlayerTint:Color.white);
            if(lockTime<=0 && !Dead && invincible<=0 && throwbackTime<=0 && executionTime<=0 && true)
            {
                if(game.Current!=null&&game.Current.enemies!=null)
                {
                    foreach(var enemy in game.Current.enemies)
                    {
                        if(enemy==null || !enemy.CanDamagePlayer) continue;
                        Vector2 delta=(Vector2)transform.position - (Vector2)enemy.transform.position;
                        float contactWidth = 0.55f + enemy.Radius * 0.45f;
                        if(Mathf.Abs(delta.x) < contactWidth && Mathf.Abs(delta.y) < 1.35f)
                        {
                            DamageContact(14f, enemy.transform.position.x);
                            break;
                        }
                    }
                }
            }
            if(transform.position.y<game.Current.PlayerFallY(transform.position))Damage(200,transform.position.x,false);
        }
        public float BeginCollect(bool halfHeight){collectClock=actor.Play(halfHeight?"penitent_collecting_object_from_halfheight_anim":"penitent_collecting_object_from_floor_anim",false,true);motor.velocity=Vector2.zero;return collectClock;}
        public void BeginAltarInteraction(){if(altarPhase!=0)return;altarPhase=1;altarClock=actor.Play("penitent_kneeling_down",false,true);motor.velocity=Vector2.zero;}
        public void EndAltarInteraction(){if(altarPhase!=2)return;altarPhase=3;altarClock=actor.Play("penitent_altar_stand_up_anim",false,true);}
        bool TryStartJump(MudZone mud,float move)
        {
            if(jumpBuffer<=0||!game.progress.canJump||coyote<=0||lockTime>0||attackTime>0||chargePending)return false;
            motor.velocity.y=mud!=null?mud.jumpSpeed:SourceGameplayTuning.JumpSpeed;jumpBuffer=0;coyote=0;motor.grounded=false;
            actor.Play(Mathf.Abs(move)>.1f?"Player_Jump_Forward":"Player_Jump",false,true);
            game.Sfx("PENITENT_JUMP");game.effects.Animation("Penitent_Running_Dust",transform.position,facing);locomotionLock=.12f;
            return true;
        }
        void StartAttack(int index,float vertical)
        {
            if(index==3){if(vertical>.5f&&game.progress.ComboLevel<2||vertical<-.5f&&game.progress.ComboLevel<3)vertical=0;}
            combo=index;hit=false;hitCount=0;attackVertical=vertical;
            bool isCrouch=index!=3&&motor.grounded && vertical<-.5f;
            bool upward=vertical>.5f;
            string clip=!motor.grounded?(upward?"penitent_upward_attack_jump":index%2==0?"Player_Jump_Attack_noslashes":"Player_Jump_Attack_noslashes_2"):inputDirection(vertical,index);
            float animSpeed=(isCrouch?0.65f:1f)*Mathf.Clamp(Mods.Value(0,1),1,1.5f);
            float sourceDuration=actor.Play(clip,false,true,animSpeed);
            // Player_ComboFinisher_Down is a 1.70 s source clip.  The old 1.10 s
            // clamp cut off its recovery frames and immediately replaced them
            // with locomotion, leaving only the planted sword/blue slash visible.
            float maximum=index==3?sourceDuration:1.1f;
            attackDuration=Mathf.Clamp(sourceDuration,.25f,maximum);attackTime=attackDuration;
            string slash=!motor.grounded?(upward?"penitent_upward_attack_slash_lvl1_jump":index%2==0?"Player_Jump_Attack_slashes_lvl1":"Player_Jump_Attack_slashes_2_lvl1"):vertical<-.5f?"Player_crouch_attack_slashes_anim":upward?"Player_Upward_Attack_Clamped_slash_lvl1_anim":"Slash_clamped_attack_"+Mathf.Clamp(index+1,1,3);if((activePrayer=="PR10"&&prayerTime>0)||game.progress.IsEquipped("HE201")){slash=slash.Replace("lvl1","lvl2");if(slash.StartsWith("Slash_clamped_attack_"))slash=slash.Replace("Slash_clamped_attack_","Slash_clamped_attack_lvl2_");else if(slash=="Player_crouch_attack_slashes_anim")slash="Player_crouch_attack_slashes_lvl2_anim";}game.effects.Animation(slash,transform.position,facing,animSpeed);
            if(game.prayerEffects!=null)game.prayerEffects.PlayerAttack();
            game.Sfx(index==3?"PENITENT_COMBO_FINAL_DOWN":"PENITENT_SLASH_AIR_"+(index+1));
        }
        string inputDirection(float vertical,int index){if(index==3)return vertical>.5f?"Player_ComboFinisher_Up":vertical<-.5f?"Player_ComboFinisher_Down":"penitent_ParryStab_old_anim";if(vertical<-.5f)return "Player_crouch_attack_noslashes";if(vertical>.5f)return "Player_Upward_Attack_Clamped_anim";return AttackClips[index];}
        bool TryStartVerticalAttack()
        {
            var floor=Physics2D.Raycast((Vector2)transform.position+Vector2.up*.1f,Vector2.down,10,(1<<8)|(1<<9));
            if(floor.collider==null||floor.distance-.1f<SourceGameplayTuning.VerticalMinimumHeight)return false;
            verticalHeight=floor.distance-.1f;verticalAttackTier=game.progress.VerticalLevel;verticalHoldClock=0;
            verticalStartTime=actor.Play("penitent_verticalattack_start_anim",false,true);motor.velocity=Vector2.zero;
            game.Sfx("VERTICAL_ATTACK_FALL");return true;
        }
        void LandVerticalAttack()
        {
            bool beam=verticalAttackTier>=3&&verticalHeight>=SourceGameplayTuning.VerticalBeamHeight;
            Vector2 size=beam?new Vector2(6,12):new Vector2(verticalAttackTier>=2?6:2,.5f);
            Vector2 center=(Vector2)transform.position+Vector2.up*(beam?6:.25f);
            foreach(var enemy in game.Current.enemies)
            {
                if(enemy==null||enemy.Dead)continue;
                float height=enemy.motor!=null?enemy.motor.size.y:1.15f;
                Vector2 delta=(Vector2)enemy.transform.position+Vector2.up*height*.5f-center;
                if(Mathf.Abs(delta.x)>size.x*.5f+enemy.SwordHurtRadius||Mathf.Abs(delta.y)>size.y*.5f+height*.5f)continue;
                float damage=Mods.DamageDealt(18)*SourceGameplayTuning.VerticalDamageFactor;HeavyWeaponHit(enemy,damage);
            }
            HitBreakables(size.x*.5f,size.y);lockTime=actor.Play("penitent_verticalattack_landing_anim",false,true);locomotionLock=lockTime;
            game.effects.Animation(beam?"penitent_verticalattack_landing_effects_LVL3_anim":"penitent_verticalattack_landing_effects_anim",transform.position,facing);
            if(beam)game.effects.Animation("penitent_verticalattack_landing_effects_LVL3_tileable_anim",transform.position+Vector3.up*6,facing);
            game.Sfx("VERTICAL_ATTACK_HIT");game.Shake(.15f);
        }
        void StartChargedAttack(){chargedAttack=true;chargedEvent=false;chargedClock=0;combo=3;hit=false;hitCount=0;attackDuration=Mathf.Clamp(actor.Play("penitent_charged_attack",false,true),.4f,1.2f);attackTime=attackDuration;game.Sfx("RELEASE_CHARGED_ATTACK");}
        void StartLunge(){dashTime=0;lungeTime=SourceGameplayTuning.LungeDuration;lungeHit=false;motor.TrySetHeight(1.15f);int tier=game.progress.lungeTier;actor.Play(tier>=3?"penitent_dodge_attack_LVL3_anim":tier==2?"penitent_dodge_attack_LVL2_anim":"penitent_dodge_attack_anim",false,true);game.Sfx(tier>=3?"LUNGE_ATTACK_LV3":tier==2?"LUNGE_ATTACK_LV2":"LUNGE_ATTACK");}
        bool ReadPenanceInput(float dt,out bool specialTap)
        {
            var input=game.controls;specialTap=input.Special&&!specialHoldConsumed;
            if(!input.SpecialHeld){specialHoldClock=0;specialHoldConsumed=false;return false;}
            specialTap=false;specialHoldClock+=dt;
            if(specialHoldConsumed||specialHoldClock<SourceGameplayTuning.PenanceHoldThreshold)return false;
            specialHoldConsumed=true;return true;
        }
        bool TryStartFervourPenance()
        {
            float tearsCost=SourceGameplayTuning.PenanceTearsCost(Mods.MeaCulpaLevel);
            if(health<=SourceGameplayTuning.PenanceLifeCost){game.Message("FERVOUR PENANCE · Cần trên 15 máu");return false;}
            if(game.progress.tears<=tearsCost){game.Message("FERVOUR PENANCE · Cần trên "+Mathf.CeilToInt(tearsCost)+" Tears");return false;}
            // Self-inflicted penance bypasses combat damage, armour and invulnerability.
            health-=SourceGameplayTuning.PenanceLifeCost;game.progress.tears-=tearsCost;
            fervour=Mathf.Min(MaxFervour,fervour+SourceGameplayTuning.PenanceFervourRestored);
            motor.velocity=Vector2.zero;penanceTime=actor.Play("penitent_fervour_penance_anim",false,true);lockTime=penanceTime;
            if(game.events!=null)game.events.Raise("PENANCE");game.SaveGame();game.Sfx("FERVOR_SELF_DAMAGE");game.Message("FERVOUR PENANCE · +25 Fervour");return true;
        }
        void StartRangeAttack(){fervour-=Mods.RangedCost;rangeAttackClock=.35f;lockTime=.69f;actor.Play(motor.grounded?"penitent_rangeAttack_shoot_anim":"penitent_rangeAttack_shoot_midair_anim",false,true);game.Sfx("RANGE_ATTACK");game.Message("FERVOROUS BLOOD");}
        void HitLunge(){HitBreakables(2.3f);foreach(var enemy in game.Current.enemies){if(enemy.Dead)continue;Vector2 d=enemy.transform.position-transform.position;float edge=enemy.family=="lavia"?enemy.SwordHurtRadius:0;if(d.x*facing>=-.2f&&d.x*facing<2.3f+edge&&Mathf.Abs(d.y)<1.7f){PrayerCombat.Damage(game,enemy,Mods.DamageDealt(SourceGameplayTuning.LungeDamage(game.progress.lungeTier)),false,false,true);game.Sfx("LUNGE_ATTACK_HIT");game.effects.Burst(enemy.transform.position+Vector3.up,Color.red);lungeHit=true;break;}}}
        void HitCharged(){HitBreakables(SourceGameplayTuning.ChargedAreaWidth);float damage=Mods.DamageDealt(54+game.progress.chargedTier*9);foreach(var enemy in game.Current.enemies){if(enemy.Dead)continue;Vector2 d=enemy.transform.position-transform.position;if(d.x*facing>=-.3f&&d.x*facing<SourceGameplayTuning.ChargedAreaWidth+enemy.SwordHurtRadius&&Mathf.Abs(d.y)<2){HeavyWeaponHit(enemy,damage);game.Sfx("CHARGED_ATTACK_PROJECTILE_HIT");game.effects.Burst(enemy.transform.position+Vector3.up,Color.red);}}if(game.progress.chargedTier>=3)game.effects.ChargedProjectile(transform.position+Vector3.right*facing*.8f+Vector3.up*.65f,facing,game,damage);}
        void HitBreakables(float radius,float receiverHeight=2.4f)
        {
            if(game.Current==null)return;
            game.Current.TryStrikeShockReceiver(transform.position,facing,radius,receiverHeight,game);
            if(game.world!=null)game.world.Strike(new Bounds(transform.position+new Vector3(facing*radius*.5f,receiverHeight*.5f,0),new Vector3(radius+1,receiverHeight,2)));
            if(game.Current.breakables==null)return;
            foreach(var b in game.Current.breakables)
            {
                if(b==null||!b.activeSelf)continue;
                Vector2 delta=(Vector2)b.transform.position-(Vector2)transform.position;
                if(Mathf.Abs(delta.x)<radius&&Mathf.Abs(delta.y)<2.4f&&(delta.x*facing>=-.6f||radius>2.5f))
                {
                    b.SetActive(false);
                    game.Sfx("CHARGED_ATTACK_PROJECTILE_HIT_WALL");
                    game.effects.Burst(b.transform.position+Vector3.up*.8f,new Color(.75f,.7f,.65f));
                    game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",b.transform.position,facing);
                    game.progress.tears+=15;
                    fervour=Mathf.Min(MaxFervour,fervour+10);
                    ItemEvent(7);game.Message("TEARS OF ATONEMENT +15");
                }
            }
        }
        void HitRiposte(float radius,float damage)
        {
            HitBreakables(radius);
            foreach(var enemy in game.Current.enemies)
            {
                if(enemy==null||enemy.Dead)continue;
                Vector2 delta=enemy.transform.position-transform.position;
                if(delta.x*facing>=-.4f&&Mathf.Abs(delta.x)<=radius+enemy.SwordHurtRadius&&Mathf.Abs(delta.y)<2.2f)
                {
                    PrayerCombat.Damage(game,enemy,damage,gainFervour:false);
                    fervour=Mathf.Min(MaxFervour,fervour+18);
                    game.effects.Burst(enemy.transform.position+Vector3.up,Color.yellow);
                    game.effects.Animation("Penitent_Attack_Dust1",enemy.transform.position,facing);
                }
            }
        }
        void HitEnemies()
        {
            // The original upward slash sheet rises about four source units
            // from the feet; the horizontal/air slashes do not reach that high.
            HitBreakables(1.8f,attackVertical>.5f?4.2f:2.4f);
            foreach(var enemy in game.Current.enemies)
            {
                if(enemy.Dead)continue;
                Vector2 delta=enemy.transform.position-transform.position;
                bool upward=attackVertical>.5f;
                if(upward)
                {if(Mathf.Abs(delta.x)>1.5f+enemy.SwordHurtRadius||delta.y<0||delta.y>4.2f)continue;}
                else if(delta.x*facing<-.3f || Mathf.Abs(delta.x)>1.8f+enemy.SwordHurtRadius ||
                    (enemy.family=="lavia" ? delta.y>2.4f||delta.y+enemy.motor.size.y<0 : Mathf.Abs(delta.y)>2.2f))continue;
                bool done=false;for(int i=0;i<hitCount;i++)if(hitTargets[i]==enemy)done=true;
                if(done)continue;if(hitCount<hitTargets.Length)hitTargets[hitCount++]=enemy;
                float dealt=Mods.DamageDealt(combo==3?32:18);PrayerCombat.Damage(game,enemy,dealt,activePrayer=="PR10"&&prayerTime>0,false,true);if(game.itemEffects==null||!game.itemEffects.HasTemporalFlag(0))fervour=Mathf.Min(MaxFervour,fervour+Mods.FervourGain(activePrayer=="PR10"&&prayerTime>0?8:4));game.effects.Burst(enemy.transform.position+Vector3.up, new Color(.7f,.08f,.05f));
                if(!hit){if(!motor.grounded&&game.controls.Down&&airImpulses<Mods.AirImpulses){motor.velocity=new Vector2(Mathf.Clamp(motor.velocity.x,-1,1)*6,6);airImpulses++;}game.Sound(.8f);hit=true;}
            }
        }
        public void ItemEvent(int eventType,float damage=0,bool execution=false,bool heavy=false){Mods.ApplyEvent(eventType,damage,execution,heavy);if(game.itemEffects!=null)game.itemEffects.Dispatch(eventType,execution,heavy);}
        public void HeavyWeaponHit(EnemyController enemy,float damage)
        {
            if(enemy==null||enemy.Dead)return;float before=enemy.health;
            HeavyRewardContext=true;try{enemy.Damage(damage);if(enemy.health<before)ItemEvent(2,damage,false,true);}finally{HeavyRewardContext=false;}
        }
        public bool TryExecution()
        {
            foreach(var e in game.Current.enemies)if(!e.boss && !e.Dead && e.Stunned && Vector2.Distance(transform.position,e.transform.position)<1.8f)
            {
                if(!motor.grounded||!e.motor.grounded||Mathf.Abs(transform.position.y-e.transform.position.y)>.45f)continue;
                facing=transform.position.x<=e.transform.position.x?1:-1;actor.Face(facing);
                // ACT_AcolyteExecution and ACT_FlagellantExecution select separate
                // left/right interactor animators. The composite crosses Penitent
                // through the victim, so it finishes one unit on the far side.
                executionExitPosition=ExecutionEndpoint(e.transform.position,facing);
                motor.Teleport(executionExitPosition);
                ExecutionRewardContext=true;try{executionTime=Mathf.Max(.5f,e.Execute());}finally{ExecutionRewardContext=false;}lockTime=executionTime;fervour=Mathf.Min(MaxFervour,fervour+20);actor.Play("penitent_ParryStab_anim",false,true);actor.visual.enabled=false;game.BeginExecutionCamera(e.transform,executionTime);game.effects.Burst(e.transform.position+Vector3.up,Color.red);return true;
            }
            return false;
        }
        public static Vector2 ExecutionEndpoint(Vector2 enemyPosition,float playerFacing){return enemyPosition+Vector2.right*Mathf.Sign(playerFacing);}
        public bool Damage(float amount,float fromX,bool canParry=true)
        {
            if(Dead||dashTime>0||healingProtectionTime>0)return false;
            if(game.prayerEffects!=null&&game.prayerEffects.BlocksAttack(fromX,false))return false;
            if(canParry && Parrying && ((fromX-transform.position.x)*facing>=0||game.progress.IsEquipped("RB202")))
            {
                parryState=4;
                parryTimer=.28f;
                invincible=.8f;
                lockTime=1.2f;
                slowMotionTimer=.18f;
                Time.timeScale=.2f;
                retributionTriggered=false;
                actor.Play("penitent_parry_success",false,true);
                game.Sfx("PENITENT_PARRY_HIT");
                game.effects.Animation("penitent_parrysuccess_dust_effect_anim",transform.position+Vector3.up*.9f,facing);
                game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",transform.position,facing);
                game.effects.Burst(transform.position+Vector3.up,Color.yellow);
                return true;
            }
            if(invincible>0)return false;
            if(game.itemEffects!=null&&game.itemEffects.HasTemporalFlag(1)){if(game.legacyPrayers!=null)game.legacyPrayers.PlayerDamaged(fromX);return false;}
            CancelPrayerCast();penanceTime=verticalStartTime=verticalHoldClock=0;plunging=false;prieDieuPrayer=false;prieDieuPhase=0;prieDieuTimer=0;
            amount=Mods.DamageTaken(amount);health=Mathf.Max(0,health-amount);invincible=.40f;hurtFlash=.35f;lockTime=.3f;healPending=false;attackTime=0;dashTime=0;attackHoldArmed=chargePending=chargeLoaded=chargedAttack=false;attackHoldClock=charge=0;game.effects.StopClip("penitent_charged_attack_effect");
            game.Sfx("PENITENT_SIMPLE_DAMAGE_DEFAULT");game.Sfx("PENITENT_PUSHBACK",.8f);game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",transform.position,facing);
            ItemEvent(5,amount);if(!Dead&&amount>=24){throwbackTime=.8f;throwbackLanded=false;float away=Mathf.Sign(transform.position.x-fromX);if(away==0)away=-facing;motor.velocity=new Vector2(away*6,7);actor.Play("penitent_throwback_transition_anim",false,true);}else actor.Play(Dead?"penitent_death_blood":"penitent_pushback_grounded",false,true);
            if(Dead){deathTime=1.8f;if(game.guilt!=null)game.guilt.OnPlayerDeath();StopPrayers();ItemEvent(10);game.Message("YOU HAVE FALLEN");}return false;
        }
        public void DamageContact(float amount,float fromX)
        {
            if(Dead||invincible>0||dashTime>0||healingProtectionTime>0)return;
            if(game.itemEffects!=null&&game.itemEffects.HasTemporalFlag(1)){if(game.legacyPrayers!=null)game.legacyPrayers.PlayerDamaged(fromX);return;}
            if(game.prayerEffects!=null&&game.prayerEffects.BlocksAttack(fromX,true))return;
            CancelPrayerCast();penanceTime=verticalStartTime=verticalHoldClock=0;plunging=false;prieDieuPrayer=false;prieDieuPhase=0;prieDieuTimer=0;
            amount=Mods.DamageTaken(amount,29);health=Mathf.Max(0,health-amount);invincible=1.25f;hurtFlash=.35f;lockTime=.35f;healPending=false;attackTime=0;dashTime=0;lungeTime=0;parryState=0;attackHoldArmed=chargePending=chargeLoaded=chargedAttack=false;attackHoldClock=charge=0;game.effects.StopClip("penitent_charged_attack_effect");
            float away=Mathf.Sign(transform.position.x-fromX);if(away==0)away=-facing;
            motor.velocity=new Vector2(away*5.5f,6.5f);
            throwbackTime=.75f;throwbackLanded=false;
            actor.Play("penitent_throwback_transition_anim",false,true);
            game.Sfx("PENITENT_SIMPLE_DAMAGE_DEFAULT");game.Sfx("PENITENT_PUSHBACK",.85f);
            game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",transform.position,facing);
            game.Shake(.12f);ItemEvent(5,amount);
            if(Dead){deathTime=1.8f;if(game.guilt!=null)game.guilt.OnPlayerDeath();StopPrayers();ItemEvent(10);game.Message("YOU HAVE FALLEN");}
        }
        void HitRetribution(float radius,float damage)
        {
            HitBreakables(radius);
            foreach(var enemy in game.Current.enemies)
            {
                if(enemy==null||enemy.Dead)continue;
                Vector2 delta=enemy.transform.position-transform.position;
                if(delta.x*facing>=-.4f&&Mathf.Abs(delta.x)<=radius+enemy.Radius&&Mathf.Abs(delta.y)<2.5f)
                {
                    PrayerCombat.Damage(game,enemy,damage,gainFervour:false);
                    enemy.StunEnemy(SourceGameplayTuning.ExecutionStunTime);
                    fervour=Mathf.Min(MaxFervour,fervour+25);
                    game.effects.Burst(enemy.transform.position+Vector3.up,Color.cyan);
                    game.effects.Burst(enemy.transform.position+Vector3.up*1.5f,Color.white);
                }
            }
        }
        public void EnterBossCinematicIdle()
        {
            // The trigger can fire while Player_Run or dodge is active. Finish
            // that locomotion state before encounter input is blocked.
            dashTime=lungeTime=attackTime=rangeAttackClock=climbTime=0f;
            chargePending=chargeLoaded=attackHoldArmed=chargedAttack=false;
            attackHoldClock=charge=comboBuffer=locomotionLock=0f;
            verticalStartTime=verticalHoldClock=0;plunging=false;previousMove=0f;motor.velocity=Vector2.zero;motor.TrySetHeight(1.15f);
            actor.Paused=false;actor.Play("Player_Idle",true,true);
        }
        public void EnterRoomTransitionIdle()
        {
            // The outgoing Run clip must not remain visible through the fade.
            EnterBossCinematicIdle();
        }
        public void Restore() {StopPrayers();healingProtectionTime=0;if(game.itemEffects!=null)game.itemEffects.ResetTransient();airImpulses=0;collectClock=altarClock=0;altarPhase=0;if(game.prayerEffects!=null)game.prayerEffects.Stop();health=MaxHealth;flasks=MaxFlasks;fervour=MaxFervour;invincible=.5f;hurtFlash=0;lockTime=0;attackTime=0;dashTime=0;lungeTime=throwbackTime=rangeAttackClock=prayerTime=penanceTime=0;specialHoldClock=0;specialHoldConsumed=game.controls!=null&&game.controls.SpecialHeld;prieDieuPrayer=false;prieDieuPhase=0;prieDieuTimer=0;prieDieuMode=0;parryState=0;parryTimer=0;slowMotionTimer=0;Time.timeScale=1f;activePrayer="";healPending=false;comboBuffer=0;climbTime=0;plunging=false;dashCooldown=0;executionTime=0;locomotionLock=0;previousMove=0;attackHoldArmed=chargePending=chargeLoaded=chargedAttack=false;attackHoldClock=charge=0;verticalStartTime=verticalHoldClock=0;wasGrounded=motor.grounded;actor.visual.enabled=true;actor.visual.color=Color.white;motor.TrySetHeight(1.15f);actor.Play("Player_Idle",true,true);}
        public void Kneel(){prieDieuMode=0;prieDieuPrayer=true;prieDieuHoldRequired=false;prieDieuPhase=0;prieDieuTimer=0;lockTime=99f;actor.Play("penitent_priedieu_kneeling_anim",false,true);game.Sfx("CHECKPOINT_KNEE_START");}
        public void Awaken(){lockTime=Mathf.Clamp(actor.Play("penitent_getting_up",false,true),.5f,4);}
        void StartPrayerCast()
        {
            pendingPrayer=game.progress.equippedPrayer;prayerCastClock=0;prayerLaunched=false;
            prayerCastSpeed=game.itemEffects!=null?game.itemEffects.AnimationSpeed("QuickAreaTransformBeadEffect"):1;
            lockTime=actor.Play("penitent_aura_transform",false,true,prayerCastSpeed);motor.velocity=Vector2.zero;
            game.Sfx("PENITENT_ACTIVATE_PRAYER");
        }
        void TickPrayerCast(float dt)
        {
            if(Dead||game.progress.equippedPrayer!=pendingPrayer){CancelPrayerCast();return;}
            prayerCastClock+=dt*prayerCastSpeed;
            if(!prayerLaunched&&prayerCastClock>=.55f)
            {
                prayerLaunched=true;
                var area=new Bounds(transform.position+Vector3.up*.25f,new Vector3(2,.5f,2));
                foreach(var enemy in game.Current.enemies)if(PrayerCombat.Hit(area,enemy))PrayerCombat.Damage(game,enemy,Mods.DamageDealt(10)*SourceGameplayTuning.VerticalDamageFactor,true,false);
                activePrayer=pendingPrayer;fervour=Mathf.Max(0,fervour-Mods.PrayerCost);prayerTime=Mods.PrayerDurationFor(activePrayer);
                if(game.itemEffects!=null)game.itemEffects.BeginPrayer(activePrayer);
                if(game.prayerBuffs!=null)game.prayerBuffs.Begin(activePrayer);
                if(game.legacyPrayers!=null)game.legacyPrayers.Cast(activePrayer,prayerTime);
                if(game.prayerEffects!=null)game.prayerEffects.Cast(activePrayer,prayerTime);
                // The source AuraTransform event triggers the hard-landing hit
                // before casting the selected prayer, without an explosion.
                game.Sfx("FERVOR_START_PRAYER",.8f);game.Message("PRAYER · "+(InventoryCatalog.Load().Find(activePrayer)?.caption??activePrayer));
                if(prayerTime<=0)EndPrayer();
                if(pendingPrayer=="PR202")lockTime=Mathf.Max(lockTime,2.0166667f);
            }
            if(prayerCastClock>=1.3299994f)pendingPrayer="";
        }
        void CancelPrayerCast(){pendingPrayer="";prayerCastClock=0;prayerLaunched=false;}
        public void StopPrayers()
        {
            CancelPrayerCast();activePrayer="";prayerTime=0;
            if(game.prayerEffects!=null)game.prayerEffects.Stop();
            if(game.legacyPrayers!=null)game.legacyPrayers.Stop();
            if(game.prayerBuffs!=null)game.prayerBuffs.Stop();
            if(game.itemEffects!=null)game.itemEffects.StopPrayer();
        }
        void EndPrayer(){if(string.IsNullOrEmpty(activePrayer))return;game.Sfx("FERVOR_END_PRAYER",.8f);activePrayer="";}
    }
}
