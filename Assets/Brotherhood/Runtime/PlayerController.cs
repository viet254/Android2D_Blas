using UnityEngine;
namespace Brotherhood
{
    public sealed class PlayerController : MonoBehaviour
    {
        public KinematicMotor motor; public SpriteActor actor; public BrotherhoodGame game;
        public float health=88,fervour; public int flasks=2; public float facing=1;
        InventoryModifiers modifiers;InventoryModifiers Mods=>modifiers??(modifiers=new InventoryModifiers(game.progress));
        public float MaxHealth=>Mods.MaxLife;public float MaxFervour=>Mods.MaxFervour;public int MaxFlasks=>Mods.MaxFlasks;
        public bool Dead=>health<=0; public bool Parrying=>parryState==1||parryState==2;
        public bool Executing=>executionTime>0;
        public string ActivePrayer=>activePrayer;
        public float attackTime, dashTime;
        float invincible,hurtFlash,lockTime,jumpBuffer,coyote,comboBuffer,healTime,deathTime,charge,stepTime,dashCooldown,climbTime,executionTime,dashGhostClock,dashFxClock=-1,locomotionLock,ladderStep,fallPeak,lungeTime,chargedClock,throwbackTime,rangeAttackClock,prayerTime,prayerPulse;string activePrayer;
        int parryState; float parryTimer, slowMotionTimer; bool riposteHit, retributionTriggered; int prieDieuPhase, prieDieuMode;
        Vector2 climbStart,climbEnd,executionExitPosition;bool plunging,laddering,dashEndDust;float interactHold;bool interactionDone;
        bool prieDieuPrayer,prieDieuHoldRequired;float prieDieuTimer;
        int combo,ladderSoundIndex; bool hit,healPending,wasGrounded,lungeHit,chargePending,chargeLoaded,chargedAttack,chargedEvent,throwbackLanded; float attackDuration,previousMove;
        static readonly string[] AttackClips={"Player_Clamped_Attack_NoSlashes_1","Player_Clamped_Attack_NoSlashes_2","Player_Clamped_Attack_NoSlashes_3","Player_ComboFinisher_Down"};
        readonly EnemyController[] hitTargets=new EnemyController[32];int hitCount;
        public void Tick(float dt)
        {
            if(slowMotionTimer>0){slowMotionTimer-=Time.unscaledDeltaTime;if(slowMotionTimer<=0)Time.timeScale=1f;}
            if(executionTime>0)
            {
                // Mobile Execution drives the visible composite from the enemy's
                // interactable and keeps the real Penitent locked at its endpoint.
                executionTime-=dt;motor.velocity=Vector2.zero;
                if(executionTime<=0){motor.Teleport(executionExitPosition);motor.TrySetHeight(1.15f);lockTime=0;actor.visual.enabled=true;actor.Play("Player_Idle",true,true);game.EndExecutionCamera();}
                return;
            }
            if(Dead) {deathTime-=dt;if(deathTime<=0)game.Respawn();return;}
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
                    parryTimer=SourceGameplayTuning.ParryWindow;
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
            if(prayerTime>0){prayerTime=Mathf.Max(0,prayerTime-dt);if(prayerTime>0)TickPrayer(dt);else EndPrayer();}
            locomotionLock=Mathf.Max(0,locomotionLock-dt);
            var input=game.controls;
            float vertical=input.Vertical;if(UnityEngine.InputSystem.Keyboard.current!=null){if(UnityEngine.InputSystem.Keyboard.current.wKey.isPressed||UnityEngine.InputSystem.Keyboard.current.upArrowKey.isPressed)vertical=1;if(UnityEngine.InputSystem.Keyboard.current.sKey.isPressed||UnityEngine.InputSystem.Keyboard.current.downArrowKey.isPressed)vertical=-1;}if(UnityEngine.InputSystem.Gamepad.current!=null&&Mathf.Abs(UnityEngine.InputSystem.Gamepad.current.leftStick.y.ReadValue())>.25f)vertical=UnityEngine.InputSystem.Gamepad.current.leftStick.y.ReadValue();
            dashCooldown=Mathf.Max(0,dashCooldown-dt);
            // The restored dodge clip raises StopDust at 0.54 s. Keep this event on
            // the animation timeline even though the physical dash ends at 0.35 s.
            if(dashFxClock>=0){dashFxClock+=dt;if(!dashEndDust&&dashFxClock>=SourceGameplayTuning.DashStopDustEventTime){game.effects.Animation("Penitent_stop_running_dust",transform.position+Vector3.down*0.05f,facing);game.effects.Animation("Penitent_Landing_Dust",transform.position+Vector3.down*0.05f,facing);dashEndDust=true;dashFxClock=-1;}}
            if(climbTime>0){climbTime=Mathf.Max(0,climbTime-dt);motor.Teleport(Vector2.Lerp(climbStart,climbEnd,1-climbTime/.35f));return;}
            if(input.Jump)jumpBuffer=.14f;else jumpBuffer-=dt;
            coyote=motor.grounded?.12f:coyote-dt;
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
                if(input.Jump){laddering=false;motor.velocity.y=8;}
                else if(game.Current.OnLadder(transform.position,out ladder))
                {
                    float x=Mathf.MoveTowards(transform.position.x,ladder.center.x,dt*5);
                    motor.Teleport(new Vector2(x,transform.position.y+vertical*3.2f*dt));
                    actor.Play(vertical<-.1f?"penitent_ladder_going_down":"penitent_ladder_going_up",true);
                    ladderStep-=dt;if(Mathf.Abs(vertical)>.15f&&ladderStep<=0){game.Sfx("PENITENT_CLIMB_LADDER_"+(ladderSoundIndex++%3+1),.65f);ladderStep=.34f;}
                    return;
                }
                else laddering=false;
            }
            if(input.Flask && game.progress.hasFlask && flasks>0 && health<MaxHealth && lockTime<=0 && motor.grounded)
            {flasks--;healPending=true;healTime=.7f;lockTime=1;actor.Play("penitent_healthpotion_consuming_anim",false,true);game.Sfx("USE_FLASK");}
            if(healPending) {healTime-=dt;if(healTime<=0){health=Mathf.Min(MaxHealth,health+40+Mods.Bonus(16));healPending=false;game.Sfx("HEALING_EXPLOSION");game.effects.Burst(transform.position+Vector3.up,new Color(.9f,.75f,.25f));game.Message("Bile flask · restored");}}
            if(input.Parry && game.progress.canParry && lockTime<=0 && attackTime<=0 && parryState==0)
            {
                parryState=1;
                parryTimer=0.09f;
                lockTime=.44f;
                actor.Play("penitent_start_parry",false,true);
            }
            float prayerCost=Mods.PrayerCost;if(input.Prayer&&game.progress.hasPrayer&&lockTime<=0){if(fervour<prayerCost){game.Message("NOT ENOUGH FERVOUR ("+Mathf.CeilToInt(fervour)+"/"+Mathf.CeilToInt(prayerCost)+")");game.Sfx("PENITENT_PARRY_HIT",.5f);}else{fervour-=prayerCost;activePrayer=game.progress.equippedPrayer;prayerTime=Mods.PrayerDuration;prayerPulse=0;lockTime=.5f;actor.Play("penitent_fervour_penance_anim",false,true);game.effects.Burst(transform.position+Vector3.up,new Color(.35f,.65f,1));ActivatePrayer();game.Sfx("PENITENT_ACTIVATE_PRAYER");game.Sfx("FERVOR_START_PRAYER",.8f);game.Message("PRAYER · "+activePrayer+" · "+Mathf.CeilToInt(prayerTime)+"s");}}
            if(input.Special&&game.progress.hasSpecial&&lockTime<=0&&attackTime<=0){if(game.progress.specialMode==1&&game.progress.rangedTier>0&&fervour>=20)StartRangeAttack();else if(game.progress.lungeTier>0)StartLunge();}
            if(rangeAttackClock>0){rangeAttackClock-=dt;if(rangeAttackClock<=0)game.effects.RangeProjectile(transform.position+Vector3.right*facing*.7f+Vector3.up*.65f,facing,game,Mods.DamageDealt(32+game.progress.rangedTier*6),game.progress.rangedTier);}
            if(input.Dash && game.progress.canDash && lockTime<=0 && dashCooldown<=0 && attackTime<=0){dashTime=SourceGameplayTuning.DashRide*(1+Mods.Bonus(6));dashCooldown=SourceGameplayTuning.DashCooldown*Mods.DashCooldownMultiplier;dashGhostClock=0;dashFxClock=0;dashEndDust=false;motor.TrySetHeight(SourceGameplayTuning.DashCollisionSize.y);actor.Play("penitent_dodge_anim",false,true);game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",transform.position+Vector3.down*0.05f,-facing);game.effects.Animation("Penitent_Running_Dust",transform.position+Vector3.down*0.05f,facing);game.Sfx("PENITENT_DASH");}
            if(input.Attack && game.progress.hasMeaCulpa && !(input.Down&&!motor.grounded))
            {
                if(dashTime>0&&game.progress.lungeTier>0)StartLunge();
                else if(attackTime>0)comboBuffer=.5f;
                else if(lockTime<=0){if(game.progress.chargedTier>0&&input.AttackHeld){chargePending=true;chargeLoaded=false;charge=0;game.Sfx("LOADING_CHARGED_ATTACK",.65f);}else StartAttack(0,vertical);}
            }
            if(chargePending&&attackTime<=0){if(input.AttackHeld){charge+=dt;float need=game.progress.chargedTier>1?SourceGameplayTuning.ChargedTier2Time:SourceGameplayTuning.ChargedTier1Time;if(!chargeLoaded&&charge>=need){chargeLoaded=true;actor.Play("penitent_charged_attack_effect",true,true);game.Sfx("LOADED_CHARGED_ATTACK");}}else{chargePending=false;if(chargeLoaded)StartChargedAttack();else StartAttack(0,vertical);charge=0;chargeLoaded=false;}}
            if(input.Down && input.Attack && !motor.grounded){plunging=true;motor.velocity.y=-22;actor.Play("penitent_verticalattack_falling_anim",true,true);game.Sfx("VERTICAL_ATTACK_FALL");}
            if(attackTime>0)
            {
                attackTime-=dt;
                if(chargedAttack){chargedClock+=dt;if(!chargedEvent&&chargedClock>=SourceGameplayTuning.ChargedHitEventTime){chargedEvent=true;game.Sfx("CHARGED_ATTACK_PROJECTILE");game.effects.Animation("penitent_charged_attack_effect",transform.position+Vector3.right*facing,facing);HitCharged();game.Shake(.12f);}if(attackTime<=0)chargedAttack=false;}
                float progress=1-attackTime/attackDuration;
                if(!chargedAttack&&progress>.22f && progress<.72f)HitEnemies();
                if(attackTime<=0 && comboBuffer>0 && combo<3){StartAttack(combo+1,vertical);comboBuffer=0;}
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
                motor.velocity.x=facing*SourceGameplayTuning.DashSpeed;motor.velocity.y=0;
                if(!dashEndDust && (dashFxClock>=SourceGameplayTuning.DashStopDustEventTime || dashTime<=0))
                {
                    dashEndDust=true;
                    game.effects.Animation("Penitent_stop_running_dust",transform.position+Vector3.down*0.05f,facing);
                    game.effects.Animation("Penitent_Landing_Dust",transform.position+Vector3.down*0.05f,facing);
                }
            }
            else {bool canStand=motor.TrySetHeight(1.15f);motor.velocity.x=lockTime>0 || attackTime>0 ? 0:move*Mods.MoveSpeed;if(!canStand){motor.velocity.x=0;actor.Play("penitent_dodge_anim",false);locomotionLock=.08f;}}
            if(input.Jump&&input.Down&&motor.grounded){motor.dropThrough=.4f;motor.Teleport((Vector2)transform.position+Vector2.down*.08f);jumpBuffer=0;}
            if(jumpBuffer>0 && game.progress.canJump && coyote>0 && lockTime<=0 && attackTime<=0){motor.velocity.y=10.5f;jumpBuffer=0;coyote=0;motor.grounded=false;actor.Play(Mathf.Abs(move)>.1f?"Player_Jump_Forward":"Player_Jump",false,true);game.Sfx("PENITENT_JUMP");game.effects.Animation("Penitent_Running_Dust",transform.position,facing);locomotionLock=.12f;}
            if(input.Jump&&!motor.grounded&&lockTime<=0&&motor.Ledge(facing,out var ledge)){climbStart=transform.position;climbEnd=ledge;climbTime=.35f;actor.Play("Player_Climb_Edge_new",false,true);return;}
            if(!input.JumpHeld && motor.velocity.y>4)motor.velocity.y=Mathf.MoveTowards(motor.velocity.y,4,dt*35);
            bool groundedBefore=wasGrounded;float impactSpeed=motor.velocity.y;motor.Step(dt);
            if(!motor.grounded)fallPeak=Mathf.Min(fallPeak,motor.velocity.y);
            if(!groundedBefore&&motor.grounded){if(throwbackTime>0){throwbackLanded=true;throwbackTime=.45f;motor.velocity.x=0;actor.Play("penitent_throwback_ground_contact_anim",false,true);game.effects.Animation("penitent_throwback_ground_contact_dust_anim",transform.position,facing);game.Sfx("HARD_LANDING");game.Shake(.12f);locomotionLock=.45f;}else{bool hard=impactSpeed<-13||fallPeak<-13;actor.Play(hard?"penitent_hardlanding_rocks_anim":Mathf.Abs(move)>.1f?"Player_Landing_Running":"Player_Landing",false,true);game.Sfx(hard?"HARD_LANDING":Mathf.Abs(move)>.1f?"PENITENT_LANDING_RUNNING":"PENITENT_JUMP_FALL_STONE");game.effects.Animation("Penitent_Running_Dust",transform.position,facing);locomotionLock=hard?.45f:.12f;}fallPeak=0;}
            wasGrounded=motor.grounded;
            if(plunging&&motor.grounded){plunging=false;StartAttack(3,0);game.Sfx("VERTICAL_ATTACK_HIT");game.Shake(.15f);game.effects.Burst(transform.position,new Color(.7f,.65f,.5f));}
            stepTime-=dt;if(motor.grounded&&Mathf.Abs(motor.velocity.x)>1&&stepTime<=0){game.Sfx("PENITENT_RUN_STONE_3",.4f);stepTime=.28f;}
            if(lockTime<=0 && attackTime<=0 && dashTime<=0 && lungeTime<=0 && throwbackTime<=0 && !plunging && locomotionLock<=0)
            {
                if(!motor.grounded)actor.Play(motor.velocity.y>0?"Player_Jump":"Player_Fall",true);
                else if(Mathf.Abs(move)>.1f){if(Mathf.Abs(previousMove)<=.1f){actor.Play("Player_Run_Start",false,true);locomotionLock=.08f;}else actor.Play("Player_Run",true);}
                else if(Mathf.Abs(previousMove)>.1f){actor.Play("Player_Run_Stop",false,true);game.Sfx("PENITENT_RUNSTOP_STONE");game.effects.Animation("Penitent_Running_Dust",transform.position,facing);locomotionLock=.14f;}
                else if(actor.Current!="penitent_dodge_anim" || actor.Progress>=1f) actor.Play("Player_Idle",true);
            }
            previousMove=move;
            actor.visual.color=hurtFlash>0 && Mathf.FloorToInt(Time.time*20)%2==0?new Color(1,.5f,.5f,.4f):Color.white;
            if(lockTime<=0 && !Dead && invincible<=0 && throwbackTime<=0 && executionTime<=0 && (prayerTime<=0 || (activePrayer!="PR08"&&activePrayer!="PR11")))
            {
                if(game.Current!=null&&game.Current.enemies!=null)
                {
                    foreach(var enemy in game.Current.enemies)
                    {
                        if(enemy==null || enemy.Dead || enemy.Stunned) continue;
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
            if(transform.position.y<game.Current.KillY)Damage(200,transform.position.x,false);
        }
        void StartAttack(int index,float vertical)
        {
            combo=index;hit=false;hitCount=0;
            string clip=!motor.grounded?(index%2==0?"Player_Jump_Attack_noslashes":"Player_Jump_Attack_noslashes_2"):inputDirection(vertical,index);
            attackDuration=Mathf.Clamp(actor.Play(clip,false,true),.25f,.9f)/Mathf.Max(1,1+(prayerTime>0?Mods.PrayerBonus(activePrayer,0):0));attackTime=attackDuration;
            string slash=!motor.grounded?(index%2==0?"Player_Jump_Attack_slashes_lvl1":"Player_Jump_Attack_slashes_2_lvl1"):vertical<-.5f?"Player_crouch_attack_slashes_anim":vertical>.5f?"Player_Upward_Attack_Clamped_slash_lvl1_anim":"Slash_clamped_attack_"+Mathf.Clamp(index+1,1,3);game.effects.Animation(slash,transform.position,facing);
            game.Sfx(index==3?"PENITENT_COMBO_FINAL_DOWN":"PENITENT_SLASH_AIR_"+(index+1));
        }
        string inputDirection(float vertical,int index){if(vertical<-.5f)return "Player_crouch_attack_noslashes";if(vertical>.5f)return "Player_Upward_Attack_Clamped_anim";return AttackClips[index];}
        void StartChargedAttack(){chargedAttack=true;chargedEvent=false;chargedClock=0;combo=3;hit=false;hitCount=0;attackDuration=Mathf.Clamp(actor.Play("penitent_charged_attack",false,true),.4f,1.2f);attackTime=attackDuration;game.Sfx("RELEASE_CHARGED_ATTACK");}
        void StartLunge(){dashTime=0;lungeTime=SourceGameplayTuning.LungeDuration;lungeHit=false;motor.TrySetHeight(1.15f);int tier=game.progress.lungeTier;actor.Play(tier>=3?"penitent_dodge_attack_LVL3_anim":tier==2?"penitent_dodge_attack_LVL2_anim":"penitent_dodge_attack_anim",false,true);game.Sfx(tier>=3?"LUNGE_ATTACK_LV3":tier==2?"LUNGE_ATTACK_LV2":"LUNGE_ATTACK");}
        void StartRangeAttack(){fervour-=20;rangeAttackClock=.35f;lockTime=.69f;actor.Play(motor.grounded?"penitent_rangeAttack_shoot_anim":"penitent_rangeAttack_shoot_midair_anim",false,true);game.Sfx("RANGE_ATTACK");game.Message("FERVOROUS BLOOD");}
        void HitLunge(){HitBreakables(2.3f);foreach(var enemy in game.Current.enemies){if(enemy.Dead)continue;Vector2 d=enemy.transform.position-transform.position;if(d.x*facing>=-.2f&&d.x*facing<2.3f&&Mathf.Abs(d.y)<1.7f){enemy.Damage(Mods.DamageDealt(SourceGameplayTuning.LungeDamage(game.progress.lungeTier)));game.Sfx("LUNGE_ATTACK_HIT");game.effects.Burst(enemy.transform.position+Vector3.up,Color.red);lungeHit=true;break;}}}
        void HitCharged(){HitBreakables(SourceGameplayTuning.ChargedAreaWidth);float damage=Mods.DamageDealt(54+game.progress.chargedTier*9);foreach(var enemy in game.Current.enemies){if(enemy.Dead)continue;Vector2 d=enemy.transform.position-transform.position;if(d.x*facing>=-.3f&&d.x*facing<SourceGameplayTuning.ChargedAreaWidth+enemy.Radius&&Mathf.Abs(d.y)<2){enemy.Damage(damage);game.Sfx("CHARGED_ATTACK_PROJECTILE_HIT");game.effects.Burst(enemy.transform.position+Vector3.up,Color.red);}}if(game.progress.chargedTier>=3)game.effects.ChargedProjectile(transform.position+Vector3.right*facing*.8f+Vector3.up*.65f,facing,game,damage);}
        void HitBreakables(float radius)
        {
            if(game.Current==null||game.Current.breakables==null)return;
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
                    game.Message("TEARS OF ATONEMENT +15");
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
                if(delta.x*facing>=-.4f&&Mathf.Abs(delta.x)<=radius+enemy.Radius&&Mathf.Abs(delta.y)<2.2f)
                {
                    enemy.Damage(damage);
                    fervour=Mathf.Min(MaxFervour,fervour+18);
                    game.effects.Burst(enemy.transform.position+Vector3.up,Color.yellow);
                    game.effects.Animation("Penitent_Attack_Dust1",enemy.transform.position,facing);
                }
            }
        }
        void HitEnemies()
        {
            HitBreakables(1.8f);
            foreach(var enemy in game.Current.enemies)
            {
                if(enemy.Dead)continue;
                Vector2 delta=enemy.transform.position-transform.position;
                if(delta.x*facing<-.3f || Mathf.Abs(delta.x)>1.8f+enemy.Radius || Mathf.Abs(delta.y)>2.2f)continue;
                bool done=false;for(int i=0;i<hitCount;i++)if(hitTargets[i]==enemy)done=true;
                if(done)continue;if(hitCount<hitTargets.Length)hitTargets[hitCount++]=enemy;
                float dealt=Mods.DamageDealt(combo==3?32:18)+(activePrayer=="PR10"?18:0);enemy.Damage(dealt);if(activePrayer=="PR04")health=Mathf.Min(MaxHealth,health+dealt*.18f);fervour=Mathf.Min(MaxFervour,fervour+4+Mods.Bonus(10));game.effects.Burst(enemy.transform.position+Vector3.up, new Color(.7f,.08f,.05f));
                if(!hit){game.Sound(.8f);hit=true;}
            }
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
                executionTime=Mathf.Max(.5f,e.Execute());lockTime=executionTime;fervour=Mathf.Min(MaxFervour,fervour+20);actor.Play("penitent_ParryStab_anim",false,true);actor.visual.enabled=false;game.BeginExecutionCamera(e.transform,executionTime);game.effects.Burst(e.transform.position+Vector3.up,Color.red);return true;
            }
            return false;
        }
        public static Vector2 ExecutionEndpoint(Vector2 enemyPosition,float playerFacing){return enemyPosition+Vector2.right*Mathf.Sign(playerFacing);}
        public bool Damage(float amount,float fromX,bool canParry=true)
        {
            if(Dead||dashTime>0)return false;
            if(prayerTime>0&&(activePrayer=="PR08"||activePrayer=="PR11")){game.effects.Burst(transform.position+Vector3.up,new Color(.85f,.75f,.35f));game.Sfx("PRAYER_INVINCIBILITY",.8f);return false;}
            if(canParry && Parrying && (fromX-transform.position.x)*facing>=0)
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
            prieDieuPrayer=false;prieDieuPhase=0;prieDieuTimer=0;
            amount=Mods.DamageTaken(amount);health=Mathf.Max(0,health-amount);invincible=.95f;hurtFlash=.35f;lockTime=.3f;healPending=false;attackTime=0;dashTime=0;
            game.Sfx("PENITENT_SIMPLE_DAMAGE_DEFAULT");game.Sfx("PENITENT_PUSHBACK",.8f);game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",transform.position,facing);
            if(!Dead&&amount>=24){throwbackTime=.8f;throwbackLanded=false;float away=Mathf.Sign(transform.position.x-fromX);if(away==0)away=-facing;motor.velocity=new Vector2(away*6,7);actor.Play("penitent_throwback_transition_anim",false,true);}else actor.Play(Dead?"penitent_death_blood":"penitent_pushback_grounded",false,true);
            if(Dead){deathTime=1.8f;game.Message("YOU HAVE FALLEN");}return false;
        }
        public void DamageContact(float amount,float fromX)
        {
            if(Dead||invincible>0||dashTime>0)return;
            if(prayerTime>0&&(activePrayer=="PR08"||activePrayer=="PR11"))return;
            prieDieuPrayer=false;prieDieuPhase=0;prieDieuTimer=0;
            amount=Mods.DamageTaken(amount);health=Mathf.Max(0,health-amount);invincible=1.25f;hurtFlash=.35f;lockTime=.35f;healPending=false;attackTime=0;dashTime=0;lungeTime=0;parryState=0;
            float away=Mathf.Sign(transform.position.x-fromX);if(away==0)away=-facing;
            motor.velocity=new Vector2(away*5.5f,6.5f);
            throwbackTime=.75f;throwbackLanded=false;
            actor.Play("penitent_throwback_transition_anim",false,true);
            game.Sfx("PENITENT_SIMPLE_DAMAGE_DEFAULT");game.Sfx("PENITENT_PUSHBACK",.85f);
            game.effects.Animation("penitent_pushback_grounded_dust_effect_anim",transform.position,facing);
            game.Shake(.12f);
            if(Dead){deathTime=1.8f;game.Message("YOU HAVE FALLEN");}
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
                    enemy.Damage(damage);
                    enemy.StunEnemy(SourceGameplayTuning.ExecutionStunTime);
                    fervour=Mathf.Min(MaxFervour,fervour+25);
                    game.effects.Burst(enemy.transform.position+Vector3.up,Color.cyan);
                    game.effects.Burst(enemy.transform.position+Vector3.up*1.5f,Color.white);
                }
            }
        }
        public void Restore() {health=MaxHealth;flasks=MaxFlasks;fervour=MaxFervour;invincible=.5f;hurtFlash=0;lockTime=0;attackTime=0;dashTime=0;lungeTime=throwbackTime=rangeAttackClock=prayerTime=0;prieDieuPrayer=false;prieDieuPhase=0;prieDieuTimer=0;prieDieuMode=0;parryState=0;parryTimer=0;slowMotionTimer=0;Time.timeScale=1f;activePrayer="";healPending=false;comboBuffer=0;climbTime=0;plunging=false;dashCooldown=0;executionTime=0;locomotionLock=0;previousMove=0;chargePending=chargeLoaded=chargedAttack=false;wasGrounded=motor.grounded;actor.visual.enabled=true;actor.visual.color=Color.white;motor.TrySetHeight(1.15f);actor.Play("Player_Idle",true,true);}
        public void Kneel(){prieDieuMode=0;prieDieuPrayer=true;prieDieuHoldRequired=false;prieDieuPhase=0;prieDieuTimer=0;lockTime=99f;actor.Play("penitent_priedieu_kneeling_anim",false,true);game.Sfx("CHECKPOINT_KNEE_START");}
        public void Awaken(){lockTime=Mathf.Clamp(actor.Play("penitent_getting_up",false,true),.5f,4);}
        void ActivatePrayer()
        {
            switch(activePrayer)
            {
                case "PR01":
                    prayerTime=12f;
                    game.effects.Burst(transform.position+Vector3.up,Color.cyan);
                    game.effects.Ghost(actor.visual);
                    break;
                case "PR03":
                    game.effects.PrayerBeam(transform.position,game,Mods.DamageDealt(80f));
                    break;
                case "PR04":
                    prayerTime=14f;
                    game.effects.Burst(transform.position+Vector3.up,new Color(0.9f,0.1f,0.1f));
                    break;
                case "PR05":
                    game.effects.Animation("AlliedCherub_flying",transform.position+Vector3.up*2,1);
                    break;
                case "PR07":
                    PrayerHit(14f,65f,true);
                    game.effects.ChargedProjectile(transform.position+Vector3.up*0.7f,facing,game,Mods.DamageDealt(65f));
                    game.effects.Burst(transform.position+Vector3.right*facing*2+Vector3.up,Color.cyan);
                    break;
                case "PR08":
                    game.effects.PrayerShields(transform,game,Mods.DamageDealt(35f));
                    break;
                case "PR09":
                    PrayerHit(20f,75f);
                    game.effects.Burst(transform.position+Vector3.up*2,Color.white);
                    game.Shake(.3f);
                    break;
                case "PR10":
                    PrayerHit(8f,55f,true);
                    game.effects.ChargedProjectile(transform.position+Vector3.up*0.65f,facing,game,Mods.DamageDealt(55f));
                    break;
                case "PR11":
                    prayerTime=8f;
                    game.effects.Animation("penitent_guardian_lady_anim",transform.position+Vector3.up,1);
                    break;
                case "PR12":
                    game.effects.PrayerFlamePillars(transform.position,facing,game,Mods.DamageDealt(75f));
                    break;
                case "PR14": // Verdiales of the Forsaken Hamlet: launches two crawler orbs left and right along the floor
                    game.effects.PrayerCrawler(transform.position+Vector3.up*0.2f,1f,game,Mods.DamageDealt(50f));
                    game.effects.PrayerCrawler(transform.position+Vector3.up*0.2f,-1f,game,Mods.DamageDealt(50f));
                    game.effects.Burst(transform.position+Vector3.up,new Color(.35f,.75f,1));
                    game.Shake(.15f);
                    break;
                case "PR15":
                    game.effects.Animation("pontiffOldman_toxicFog_appear",transform.position,1);
                    prayerTime=10f;
                    break;
                default:
                    PrayerHit(8f,45f);
                    game.effects.Burst(transform.position+Vector3.up,Color.cyan);
                    break;
            }
        }
        void TickPrayer(float dt)
        {
            prayerPulse-=dt;if(prayerPulse>0)return;prayerPulse=activePrayer=="PR05"?.7f:activePrayer=="PR15"?.45f:1f;
            if(activePrayer=="PR05")PrayerHit(9,14);
            else if(activePrayer=="PR15")PrayerHit(2.6f,10);
            else if(activePrayer=="PR14")PrayerHit(5,8);
        }
        void EndPrayer(){if(string.IsNullOrEmpty(activePrayer))return;game.Sfx("FERVOR_END_PRAYER",.8f);game.effects.Burst(transform.position+Vector3.up,new Color(.35f,.45f,.7f));activePrayer="";}
        void PrayerHit(float radius,float damage,bool forwardOnly=false)
        {
            damage=Mods.DamageDealt(damage)*(1+Mods.Bonus(27));foreach(var enemy in game.Current.enemies){if(enemy==null||enemy.Dead)continue;Vector2 delta=enemy.transform.position-transform.position;if(delta.magnitude>radius||(forwardOnly&&delta.x*facing<0))continue;enemy.Damage(damage);game.effects.Burst(enemy.transform.position+Vector3.up,new Color(.55f,.75f,1));}
        }
    }
}
