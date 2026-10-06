using UnityEngine;
namespace Brotherhood
{
    public sealed class EnemyController : MonoBehaviour
    {
        static readonly AnimationCurve BossIntroEase=new AnimationCurve(
            new Keyframe(0f,0f,2f,2f),
            new Keyframe(.5583201f,.5695297f,.46220443f,.46220443f),
            new Keyframe(1f,1.0124999f,2.382353f,2.382353f));
        public BrotherhoodGame game; public KinematicMotor motor; public SpriteActor actor;
        public BrotherhoodTrialEnemyAI trialAI;
        public BrotherhoodTrialBossAI trialBoss;
        public BrotherhoodTrialLaviaAI laviaBoss;
        public bool boss; public string family="acolyte"; public float maxHealth=90,health=90;public int purgeReward;
        const int HurtState=13;
        public float Radius=>boss?1.3f:family=="lavia"?1f:.4f;
        // Lavia's broad wings and AI spacing must not extend the damageable
        // torso beyond the body motor when the Penitent swings a sword.
        public float SwordHurtRadius=>family=="lavia"&&motor!=null?motor.size.x*.5f+.05f:Radius;
        public bool Dead=>health<=0; public bool Stunned=>ExecutionReady;public bool ExecutionReady=>!boss&&(family=="acolyte"||family=="flagellant")&&!Dead&&stun>0;
        public bool Hurt=>!Dead&&state==HurtState;
        public bool CanDamagePlayer=>isActiveAndEnabled&&!Dead&&!Hurt&&stun<=0&&state!=7&&state!=8&&
            (boss?bossEncounterReady&&game.BossFightActive:family!="mudcrawler"||state==0)&&
            (trialBoss==null||!trialBoss.AppliesTo(this)||trialBoss.AllowsDamage)&&
            (laviaBoss==null||!laviaBoss.AppliesTo(this)||laviaBoss.AllowsDamage);
        Vector2 origin,introJumpStart,introJumpLanding,bossAreaOrigin;float timer,stun,lostClock,turnClock,wheelAttackLapse;int state;bool hit,hit2,rewardGranted,bossJumpTurned,bossAreaActive,bossMacePending;float facing=-1,bossAreaFacing;float jumpTarget,introJumpClock,executionClock,executionDuration,bossDeathClock;int executionAudioStage,lastBossAttack,bossAreaIndex;bool bossDeathResolved,bossEncounterReady;SpriteActor executionPrompt;
        public bool BossIntroComplete=>boss&&bossEncounterReady;
        public bool BossCorpseShown=>boss&&bossDeathResolved;
        public float Facing=>facing;
        public bool AttackInProgress=>!boss&&(state==1||state==2);
        public bool DecisionReady=>!boss&&!Dead&&state==0&&stun<=0&&timer<=0;
        public bool ControlInterrupted=>Hurt||stun>0||state==7||state==8;
        float flagellantDisplacement,flagellantDisplacementFacing;
        void OnAnimationEvent(RestoredAnimationEvent evt)
        {
            if(family!="flagellant"||state!=1||Dead||actor.Current!="NewFlagellant_attack")return;
            if(evt.functionName=="AnimationEvent_AttackDisplacement"){flagellantDisplacement=.25f;flagellantDisplacementFacing=facing;}
            else if(evt.functionName=="AnimationEvent_FastAttack")
            {
                if(!hit)hit=true;else if(!hit2)hit2=true;else return;
                Strike(3f,SourceGameplayTuning.FlagellantAttackDamage);
            }
        }
        public void Initialize(){origin=transform.position;actor.AnimationEvent-=OnAnimationEvent;actor.AnimationEvent+=OnAnimationEvent;if(!boss&&(family=="acolyte"||family=="flagellant")){var p=new GameObject("Execution prompt");p.transform.SetParent(transform,false);p.transform.localPosition=new Vector3(0,2.05f,-.1f);executionPrompt=p.AddComponent<SpriteActor>();executionPrompt.catalog=actor.catalog;executionPrompt.visual=p.AddComponent<SpriteRenderer>();executionPrompt.visual.sharedMaterial=actor.visual.sharedMaterial;executionPrompt.visual.sortingOrder=32000;executionPrompt.Play("flagellant_execution_awareness_anim",true,true);}ResetEnemy();}
        public void ResetEnemy(){health=maxHealth;timer=boss||family=="fool"||family=="wheelcarrier"||family=="mudcrawler"||family=="lavia"?0:1;stun=lostClock=turnClock=wheelAttackLapse=0;state=boss||family=="mudcrawler"?10:0;hit=hit2=rewardGranted=bossAreaActive=bossMacePending=bossJumpTurned=false;executionClock=executionDuration=bossDeathClock=0;executionAudioStage=lastBossAttack=bossAreaIndex=0;bossDeathResolved=bossEncounterReady=false;motor.Teleport(origin);gameObject.SetActive(true);actor.visual.enabled=!boss&&family!="mudcrawler"&&family!="lavia";if(executionPrompt!=null)executionPrompt.gameObject.SetActive(false);if(!boss)Play("idle",true);if(trialAI!=null)trialAI.ResetBrain();if(trialBoss!=null)trialBoss.ResetBrain();if(laviaBoss!=null)laviaBoss.ResetBrain();}
        public void PrepareBossEncounter()
        {
            if(!boss)return;
            health=maxHealth;state=10;timer=0;bossEncounterReady=bossDeathResolved=rewardGranted=bossAreaActive=bossMacePending=bossJumpTurned=false;lastBossAttack=bossAreaIndex=0;
            motor.velocity=Vector2.zero;motor.Teleport(origin);actor.visual.enabled=false;
        }
        public void BeginBossIntroJump(Vector2 landing)
        {
            if(!boss||Dead)return;
            var floor=Physics2D.Raycast(new Vector2(landing.x,origin.y+2f),Vector2.down,50f,1<<8);
            if(floor.collider!=null)landing.y=floor.point.y+.025f;
            jumpTarget=landing.x;introJumpStart=origin;introJumpLanding=landing;introJumpClock=0;state=11;timer=.65f;bossEncounterReady=false;motor.velocity=Vector2.zero;motor.Teleport(origin);actor.visual.enabled=true;
            FaceBossToPlayer();
            Play("jump_prepare");game.Sfx("ELDER_BROTHER_JUMP_VOICE",.75f);
        }
        public void ApplySavedBossDefeat()
        {
            if(!boss)return;
            health=0;state=10;bossEncounterReady=false;bossDeathResolved=true;motor.velocity=Vector2.zero;actor.visual.enabled=false;
        }
        public void ActivateBossCombat(){if(!boss||Dead)return;state=0;timer=1f;bossEncounterReady=true;motor.velocity=Vector2.zero;Play("idle",true);}
        float attackClock;
        float Play(string action,bool loop=false)
        {
            if(laviaBoss!=null&&laviaBoss.AppliesTo(this))return laviaBoss.PlayReaction(action);
            string clip;
            if(boss)clip=action=="idle"?"ElderBrother_Idle":action=="attack"?"ElderBrother_SmashAttack":action=="windup"?"ElderBrother_SmashPreparation":action=="jump_prepare"?"ElderBrother_JumpPreparation":action=="jump"?"ElderBrother_JumpLoop":action=="land"?"ElderBrother_JumpLanding":"ElderBrother_Death";
            else if(family=="fool")clip=action=="death"?"Fool_death":action=="hurt"?"Fool_Hurt":action=="walk"?"Fool_Walk":action=="turn"?"Fool_turnaround":"Fool_Idle";
            else if(family=="wheelcarrier")clip=action=="death"?"WheelCarrier_death":action=="hurt"||action=="stun"?"WheelCarrier_stun":action=="attack"?"WheelCarrier_attack":action=="walk"?"WheelCarrier_walk":"WheelCarrier_idle";
            else if(family=="mudcrawler")clip=action=="death"?"mudcrawler_death_anim":action=="hurt"?"mudcrawler_hurt_anim":action=="appear"?"mudcrawler_appearing_anim":action=="hide"?"mudcrawler_meltingdown_anim":"mudcrawler_crawling_anim";
            else if(family=="flagellant")clip=action=="attack"?"NewFlagellant_attack":action=="hurt"?"NewFlagellant_hurt":action=="parry"?"NewFlagellant_parry_reaction_anim":action=="stun"?"Flagellant_stunt_anim":action=="death"?"NewFlagellant_death_gore":action=="walk"?"NewFlagellant_walk":"NewFlagellant_idle";
            else clip=action=="idle"?"acolyte_idle":action=="walk"?"acolyte_running":action=="attack"?"acolyte_attack":action=="hurt"?"acolyte_get_hit_low":action=="parry"?"acolyte_parry_reaction_anim":action=="stun"?"acolyte_stunned_anim":action=="death"?"acolyte_death_gore":"acolyte_idle";
            return actor.Play(clip,loop,true);
        }
        public void Tick(float dt)
        {
            bool trial=trialAI!=null&&trialAI.AppliesTo(this);
            bool customBoss=trialBoss!=null&&trialBoss.AppliesTo(this);
            bool flyingBoss=laviaBoss!=null&&laviaBoss.AppliesTo(this);
            if(trial&&(Dead||Hurt||stun>0))trialAI.NotifyInterrupted();
            if(customBoss&&Dead)trialBoss.TickDecision(dt);
            else if(customBoss&&(Hurt||stun>0))trialBoss.NotifyInterrupted();
            if(flyingBoss&&Dead)laviaBoss.TickDecision(dt);
            else if(flyingBoss&&(Hurt||stun>0))laviaBoss.NotifyInterrupted();
            if(Dead){if(boss)BossDeathTick(dt);else ExecutionAudioTick(dt);return;}
            timer-=dt;bool wasStunned=stun>0;stun=Mathf.Max(0,stun-dt);
            var player=game.player;
            if(executionPrompt!=null)executionPrompt.gameObject.SetActive(ExecutionReady&&player.motor.grounded&&motor.grounded&&Mathf.Abs(player.transform.position.y-transform.position.y)<=.45f&&Vector2.Distance(player.transform.position,transform.position)<1.8f);
            float dx=player.transform.position.x-transform.position.x;
            float oldFacing=facing;
            if(!boss&&!trial&&!customBoss&&!flyingBoss){facing=Mathf.Abs(dx)>.01f?Mathf.Sign(dx):facing;actor.Face(facing);}
            if(family=="fool"&&oldFacing!=facing&&turnClock<=0&&state==0){turnClock=.37f;Play("turn");}
            if(wasStunned)
            {
                if(state==8)
                {
                    motor.velocity.x=Mathf.MoveTowards(motor.velocity.x,0,dt*8f);
                    if(timer<=0){state=7;Play("stun",true);}
                }
                else motor.velocity.x=0;
                motor.Step(dt);
                if(stun<=0){state=0;timer=.4f;Play("idle",true);if(executionPrompt!=null)executionPrompt.gameObject.SetActive(false);}
                return;
            }
            if(Hurt)
            {
                motor.velocity.x=0;motor.Step(dt);
                if(timer<=0){state=0;timer=.4f;Play("idle",true);}
                if(transform.position.y<game.Current.KillY)Damage(999);
                return;
            }
            if(player.Dead){if(trial)trialAI.NotifyInterrupted();if(customBoss)trialBoss.NotifyInterrupted();if(flyingBoss)laviaBoss.NotifyInterrupted();motor.velocity.x=0;motor.Step(dt);if(!flyingBoss)SyncStoppedLocomotion(0);return;}
            if(boss&&(state==10||state==11||state==12||state==4)){BossTick(dt,dx);return;}
            if(boss){BossTick(dt,dx);if(state==4)return;}
            else if(flyingBoss)laviaBoss.TickDecision(dt);
            else if(customBoss)
            {
                bool advancingAttack=AttackInProgress;
                trialBoss.TickDecision(dt);
                if(advancingAttack&&AttackInProgress)ForestTick(dt,dx);
            }
            else if(trial)
            {
                bool advancingAttack=AttackInProgress;
                trialAI.TickDecision(dt);
                if(advancingAttack)NormalTick(dt,dx);
            }
            else if(family=="fool"||family=="wheelcarrier"||family=="mudcrawler")ForestTick(dt,dx);
            else NormalTick(dt,dx);
            float beforeX=transform.position.x;
            motor.Step(dt);
            if(!flyingBoss)SyncStoppedLocomotion(transform.position.x-beforeX);
            if(transform.position.y<game.Current.KillY)Damage(999);
        }
        internal void TrialMove(float direction,float speed,float lookDirection=0)
        {
            if(boss||Dead||state!=0||stun>0)return;
            float look=Mathf.Abs(lookDirection)>.01f?lookDirection:direction;
            if(Mathf.Abs(look)>.01f){facing=Mathf.Sign(look);actor.Face(facing);}
            motor.velocity.x=Mathf.Sign(direction)*Mathf.Max(0,speed);
            string clip=family=="flagellant"?(speed>.01f?"NewFlagellant_walk":"NewFlagellant_idle"):(speed>.01f?"acolyte_running":"acolyte_idle");
            if(actor.Current!=clip)Play(speed>.01f?"walk":"idle",true);
        }
        internal bool TrialAttack(float direction)
        {
            if(!DecisionReady)return false;
            facing=Mathf.Abs(direction)>.01f?Mathf.Sign(direction):facing;actor.Face(facing);
            state=1;attackClock=0;flagellantDisplacement=0;timer=family=="flagellant"?1.3f:1.9f;motor.velocity.x=0;hit=hit2=false;Play("attack");
            return true;
        }
        internal void TrialBossMove(float direction,float speed)
        {
            if(trialBoss==null||boss||Dead||state!=0||stun>0)return;
            if(Mathf.Abs(direction)>.01f){facing=Mathf.Sign(direction);actor.Face(facing);}
            motor.velocity.x=Mathf.Sign(direction)*Mathf.Max(0,speed);
            string clip=speed>.01f?"WheelCarrier_walk":"WheelCarrier_idle";
            if(actor.Current!=clip)Play(speed>.01f?"walk":"idle",true);
        }
        internal bool TrialBossAttack(float direction)=>trialBoss!=null&&TrialAttack(direction);
        internal void TrialBossCancelAttack()
        {
            if(trialBoss==null||boss)return;
            if(state==1||state==2){state=0;timer=Mathf.Max(timer,.5f);attackClock=0;hit=hit2=true;}
            motor.velocity.x=0;
        }
        void SyncStoppedLocomotion(float movedX)
        {
            if(boss||state!=0)return;
            // The source controllers enter Idle when blocked. A requested velocity
            // is not proof of movement: KinematicMotor may hit a wall or gate.
            if(Mathf.Abs(movedX)>.001f)
            {
                if(family=="mudcrawler")actor.Paused=false;
                return;
            }
            string walk=family=="fool"?"Fool_Walk":family=="wheelcarrier"?"WheelCarrier_walk":
                family=="flagellant"?"NewFlagellant_walk":family=="acolyte"?"acolyte_running":"mudcrawler_crawling_anim";
            if(actor.Current!=walk)return;
            if(family=="mudcrawler")
            {
                if(!actor.Paused){actor.Play(walk,true,true);actor.Paused=true;}
            }
            else Play("idle",true);
        }
        void NormalTick(float dt,float dx)
        {
            if(state==0)
            {
                if(timer>0)
                {
                    motor.velocity.x=0;
                    if(actor.Current!=(family=="flagellant"?"NewFlagellant_idle":"acolyte_idle"))Play("idle",true);
                }
                else
                {
                    float direction=Mathf.Abs(dx)<7?facing:(transform.position.x>origin.x+2?-1:transform.position.x<origin.x-2?1:facing);
                    bool floor=Physics2D.Raycast((Vector2)transform.position+new Vector2(direction*.65f,.4f),Vector2.down,1.2f,(1<<8)|(1<<9)).collider!=null;
                    motor.velocity.x=floor?direction*1.15f:0;
                    if(Mathf.Abs(motor.velocity.x)>0.05f)
                    {
                        if(actor.Current!=(family=="flagellant"?"NewFlagellant_walk":"acolyte_running"))Play("walk",true);
                    }
                    else
                    {
                        if(actor.Current!=(family=="flagellant"?"NewFlagellant_idle":"acolyte_idle"))Play("idle",true);
                    }
                }
                float range=family=="flagellant"?2.8f:2.1f;
                if(Mathf.Abs(dx)<range && timer<=0){state=1;attackClock=0;flagellantDisplacement=0;timer=family=="flagellant"?1.3f:1.9f;motor.velocity.x=0;hit=hit2=false;Play("attack");}
            }
            else if(state==1)
            {
                attackClock+=dt;
                if(family=="flagellant")
                {
                    motor.velocity.x=0;
                    if(flagellantDisplacement>0)
                    {
                        float before=1-flagellantDisplacement/.25f;flagellantDisplacement=Mathf.Max(0,flagellantDisplacement-dt);float after=1-flagellantDisplacement/.25f;
                        motor.velocity.x=1.2f*flagellantDisplacementFacing*(Mathf.Pow(1-before,3)-Mathf.Pow(1-after,3))/Mathf.Max(.0001f,dt);
                    }
                    // Imported source events drive both hits. These constants
                    // only support catalogs generated before event import.
                    if(!actor.HasEvent("AnimationEvent_FastAttack")&&!hit && attackClock>=SourceGameplayTuning.FlagellantAttackHitTime)
                    {
                        hit=true;
                        if(Strike(3f,SourceGameplayTuning.FlagellantAttackDamage))return;
                    }
                    if(!actor.HasEvent("AnimationEvent_FastAttack")&&!hit2 && attackClock>=SourceGameplayTuning.FlagellantSecondAttackHitTime)
                    {
                        hit2=true;
                        if(Strike(3f,SourceGameplayTuning.FlagellantAttackDamage))return;
                    }
                }
                else
                {
                    float hitTime=SourceGameplayTuning.AcolyteAttackHitTime;
                    if(!hit && attackClock>=hitTime)
                    {
                        hit=true;
                        if(Strike(2.3f,18))return;
                    }
                }
                if(timer<=0){state=2;timer=family=="flagellant"?.6f:.5f;Play("idle",true);}
            }
            else if(state==2)
            {
                motor.velocity.x=0;
                if(actor.Current!=(family=="flagellant"?"NewFlagellant_idle":"acolyte_idle"))Play("idle",true);
                if(timer<=0){state=0;timer=.4f;Play("idle",true);}
            }
        }
        bool ForestPathClear(float direction)
        {
            float height=family=="mudcrawler"?.25f:.8f;
            Vector2 start=(Vector2)transform.position+new Vector2(direction*.55f,height);
            // MudCrawler.prefab uses 0.5 ground and 0.15 block sensors.
            bool floor=Physics2D.Raycast(start,Vector2.down,family=="mudcrawler"?.5f:1.5f,1<<8).collider!=null;
            bool wall=Physics2D.Raycast(start,Vector2.right*direction,family=="mudcrawler"?.15f:.35f,1<<8).collider!=null;
            if(!floor||wall)return false;
            foreach(var peer in game.Current.enemies)
            {
                if(peer==this||peer==null||peer.Dead||peer.boss||peer.actor==null||!peer.actor.visual.enabled)continue;
                Vector2 separation=peer.transform.position-transform.position;
                float minSpacing=Radius+peer.Radius+.15f;
                if(separation.x*direction>.01f&&separation.x*direction<minSpacing&&Mathf.Abs(separation.y)<.75f)return false;
            }
            return true;
        }
        bool WheelCanSeePlayer()
        {
            // WheelCarrier.prefab VisionCone: offset (0,1.13), 45 degrees,
            // distance 8, close radius 2, and backwards visibility enabled.
            Vector2 eye=(Vector2)transform.position+Vector2.up*1.13f;
            Vector2 target=(Vector2)game.player.transform.position+Vector2.up*.5f;
            float range=Vector2.Distance(eye,game.player.transform.position);
            if(range>8f)return false;
            if(range<2f)return true;
            Vector2 ray=target-eye;
            if(Mathf.Atan2(Mathf.Abs(ray.y),Mathf.Abs(ray.x))*Mathf.Rad2Deg>45f)return false;
            return Physics2D.Linecast(eye,target,1<<8).collider==null;
        }
        bool MudCanSeePlayer()
        {
            // MudCrawler.prefab VisionArea is a 6.6542 x 0.6486 box with
            // offset (2.7642, 0.1757), not a six-unit radial detector.
            Vector2 delta=game.player.transform.position-transform.position;
            float ahead=delta.x*facing;
            float playerHalfHeight=game.player.motor.size.y*.5f;
            return ahead>=-.56294f&&ahead<=6.09128f&&
                Mathf.Abs(delta.y+playerHalfHeight-.17571521f)<=.3242848f+playerHalfHeight;
        }
        void ForestTick(float dt,float dx)
        {
            float dy=Mathf.Abs(game.player.transform.position.y-transform.position.y);
            float distance=Vector2.Distance(game.player.transform.position,transform.position);
            if(family=="fool")
            {
                if(turnClock>0){turnClock-=dt;motor.velocity.x=0;return;}
                bool chase=distance<=10f&&dy<=2f&&Mathf.Abs(dx)>.8f&&ForestPathClear(facing);
                motor.velocity.x=chase?facing*.75f:0;
                string clip=chase?"Fool_Walk":"Fool_Idle";
                if(actor.Current!=clip)Play(chase?"walk":"idle",true);
                // FoolAttack inflicts contact damage; its source has no attack clip.
                if(distance<.9f&&dy<1.2f&&timer<=0){Strike(.9f,6f);timer=.1f;}
                return;
            }
            if(family=="wheelcarrier")
            {
                if(state==1)
                {
                    motor.velocity.x=0;attackClock+=dt;
                    if(!hit&&attackClock>=.5f){hit=true;Strike(2.8f,10f);}
                    if(attackClock>=1.09f){state=0;timer=.5f;Play("idle",true);}
                    return;
                }
                bool visible=distance<6f&&WheelCanSeePlayer();
                bool chase=visible&&Mathf.Abs(dx)>2.8f&&ForestPathClear(facing);
                motor.velocity.x=chase?facing:0;
                string clip=chase?"WheelCarrier_walk":"WheelCarrier_idle";
                if(actor.Current!=clip)Play(chase?"walk":"idle",true);
                if(visible&&Mathf.Abs(dx)<=2.8f&&timer<=0)
                {
                    // WheelCarrierBehaviour.Attack accumulates AttackLapse for
                    // 0.5 s before calling its attack animation.
                    wheelAttackLapse+=dt;
                    if(wheelAttackLapse>=.5f){wheelAttackLapse=0;state=1;attackClock=0;hit=false;Play("attack");}
                }
                return;
            }
            // MudCrawler is the source WaxCrawler prefab: initially invisible,
            // it appears, crawls, then melts and returns to its spawn when lost.
            bool mudSeesPlayer=MudCanSeePlayer();
            if(state==10)
            {
                motor.velocity.x=0;
                if(mudSeesPlayer){state=11;timer=.7f;actor.visual.enabled=true;Play("appear");}
                return;
            }
            if(state==11){motor.velocity.x=0;if(timer<=0){state=0;timer=0;Play("walk",true);}return;}
            if(state==12)
            {
                motor.velocity.x=0;
                if(timer<=0){motor.Teleport(origin);actor.visual.enabled=false;state=10;lostClock=0;}
                return;
            }
            if(mudSeesPlayer)lostClock=0;else lostClock+=dt;
            if(lostClock>=2f){state=12;timer=.78f;Play("hide");motor.velocity.x=0;return;}
            bool move=distance>.7f&&ForestPathClear(facing);
            motor.velocity.x=move?facing:0;
            if(actor.Current!="mudcrawler_crawling_anim")Play("walk",true);
            if(distance<.85f&&dy<.8f&&timer<=0){Strike(.85f,4f);timer=.75f;}
        }
        void BossTick(float dt,float dx)
        {
            motor.velocity.x=0;
            TickBossAreas(dt);
            if(state==10)return;
            if(state==11&&timer<=0){state=12;introJumpClock=0;introJumpStart=transform.position;motor.velocity=Vector2.zero;Play("jump",true);game.Sfx("ELDER_BROTHER_JUMP");}
            else if(state==12)
            {
                if(AdvanceBossJump(dt)){state=0;timer=1f;bossEncounterReady=true;Play("land");game.Shake(.3f);game.Sfx("ELDER_BROTHER_LANDING");}
            }
            else if(state==5)
            {
                motor.velocity.x=Mathf.Clamp((jumpTarget-transform.position.x)*2f,-7f,7f);
                if(timer<=0&&motor.grounded){state=0;timer=1f;bossEncounterReady=true;Play("land");game.Shake(.3f);game.Sfx("ELDER_BROTHER_LANDING");game.Message("WARDEN OF THE SILENT SORROW");}
            }
            else if(bossEncounterReady&&!game.BossFightActive){motor.velocity=Vector2.zero;}
            else if(state==0 && timer<=0)
            {
                // Source D17 configuration disables repeated attacks. Alternate the
                // AREA and JUMP actions, using its serialized 1 s recovery windows.
                if(lastBossAttack==1){lastBossAttack=2;state=3;timer=.65f;Play("jump_prepare");game.Sfx("ELDER_BROTHER_JUMP_VOICE",.75f);}
                else {lastBossAttack=1;state=1;timer=1f;FaceBossToPlayer();Play("windup");game.Sfx("ELDER_BROTHER_PRE_ATTACK");game.Sfx("ELDER_BROTHER_ATTACK_VOICE",.7f);}
            }
            else if(state==1 && timer<=0)
            {
                state=6;timer=1f;attackClock=0;bossAreaIndex=0;
                bossAreaOrigin=transform.position;bossAreaFacing=facing;bossAreaActive=bossMacePending=true;
                Play("attack");game.Sfx("ELDER_BROTHER_ATTACK");game.Sfx("ELDER_BROTHER_ATTACK_HIT");game.Shake(.3f);
            }
            else if(state==6)
            {
                // ElderBrotherBehaviour starts its one-second wait as soon as
                // AREA is summoned. The 1.4-second waves continue separately.
                if(timer<=0){state=0;timer=0;Play("idle",true);}
            }
            else if(state==2 && timer<=0){state=0;timer=0;Play("idle",true);}
            else if(state==3 && timer<=0)
            {
                state=4;introJumpClock=0;introJumpStart=transform.position;
                // GetTargetPredictedPos is evaluated after the 0.65-second
                // preparation, so a dash during windup changes the landing.
                jumpTarget=game.player.transform.position.x+game.player.motor.velocity.x;
                var floor=Physics2D.Raycast(new Vector2(jumpTarget,introJumpStart.y+3f),Vector2.down,50f,1<<8);
                introJumpLanding=new Vector2(jumpTarget,floor.collider!=null?floor.point.y+.025f:introJumpStart.y);
                motor.velocity=Vector2.zero;hit=false;bossJumpTurned=false;Play("jump",true);game.Sfx("ELDER_BROTHER_JUMP");
            }
            else if(state==4)
            {
                if(AdvanceBossJump(dt)){state=7;timer=.07f;Play("land");game.Sfx("ELDER_BROTHER_LANDING");game.Shake(.3f);}
            }
            else if(state==7&&timer<=0){state=2;timer=1f;Play("idle",true);}
        }
        bool AdvanceBossJump(float dt)
        {
            // D17Z01S11_LOGIC BossJumpAttack: 0.8 s along this four-point
            // Bezier. Point 2 stays 8.733559 above and .0637969 left of the
            // landing point when the original script retargets the spline.
            introJumpClock=Mathf.Min(.8f,introJumpClock+dt);
            // ElderBrotherBehaviour turns toward the current target at 90%
            // of the 0.8 s JUMP, never during preparation or the smash.
            if(state==4&&!bossJumpTurned&&introJumpClock>=.72f)
            {bossJumpTurned=true;FaceBossToPlayer();}
            float t=introJumpClock/.8f;
            float u=Mathf.Clamp01(BossIntroEase.Evaluate(t));
            Vector2 p0=introJumpStart+new Vector2(-.07652283f,.046804428f);
            Vector2 p1=introJumpStart+new Vector2(.23257446f,6.6636543f);
            Vector2 p2=introJumpLanding+new Vector2(-.0637969f,8.733559f);
            Vector2 p3=introJumpLanding;
            float v=1f-u;
            Vector2 position=v*v*v*p0+3f*v*v*u*p1+3f*v*u*u*p2+u*u*u*p3;
            transform.position=new Vector3(position.x,position.y,transform.position.z);
            if(state==4&&!hit&&Vector2.Distance(game.player.transform.position+Vector3.up*.6f,transform.position+Vector3.up*1f)<1.7f)
            {hit=true;game.player.Damage(10f,transform.position.x,false);}
            if(t<1f)return false;
            motor.Teleport(introJumpLanding);motor.grounded=true;
            return true;
        }
        void FaceBossToPlayer()
        {
            if(game==null||game.player==null)return;
            float dx=game.player.transform.position.x-transform.position.x;
            if(Mathf.Abs(dx)<.01f)return;
            facing=Mathf.Sign(dx);actor.Face(facing);
        }
        void TickBossAreas(float dt)
        {
            if(!bossAreaActive)return;
            attackClock+=dt;
            // The second source prefab, MaceImpact, is one 12-damage area at
            // 0.5 units, with a 0.1-second preparation and a 3x3 attack box.
            if(bossMacePending&&attackClock>=.1f)
            {
                bossMacePending=false;
                float center=bossAreaOrigin.x+bossAreaFacing*.5f;
                Vector2 feet=game.player.transform.position;
                if(Mathf.Abs(feet.x-center)<=1.5f&&feet.y>=bossAreaOrigin.y-.25f&&feet.y<=bossAreaOrigin.y+3f)
                    game.player.Damage(12f,center,false);
            }
            // CorpsesShockwave is spawned from the captured origin/direction;
            // its serialized curve is 2t-t² and emits at most once per frame.
            float t=Mathf.Clamp01(attackClock/ElderBrotherEncounter.AreaDuration);
            if(bossAreaIndex<ElderBrotherEncounter.AreaCount&&
                bossAreaIndex/(float)ElderBrotherEncounter.AreaCount<=2f*t-t*t)
            {BossAreaStrike(bossAreaIndex);bossAreaIndex++;}
            if(attackClock>=ElderBrotherEncounter.AreaDuration)bossAreaActive=false;
        }
        void BossAreaStrike(int index)
        {
            // D17 source: six CorpsesShockwave areas, offset 3.7 units and spaced
            // 1.5 units over 1.4 seconds. Its prefab deals 12 damage per area.
            float center=bossAreaOrigin.x+bossAreaFacing*(ElderBrotherEncounter.AreaOffset+index*ElderBrotherEncounter.AreaSpacing);
            Vector2 delta=game.player.transform.position-new Vector3(center,bossAreaOrigin.y,0);
            if(Mathf.Abs(delta.x)<=1f&&Mathf.Abs(delta.y)<1.6f)game.player.Damage(ElderBrotherEncounter.AreaDamage,center,false);
        }
        bool Strike(float range,float damage)
        {
            if(!CanDamagePlayer)return false;
            if(trialAI!=null&&trialAI.AppliesTo(this)&&!trialAI.CanStrikePlayer())return false;
            if(trialBoss!=null&&trialBoss.AppliesTo(this)&&!trialBoss.CanStrikePlayer())return false;
            Vector2 d=game.player.transform.position-transform.position;
            if(Mathf.Abs(d.x)<range && Mathf.Abs(d.y)<1.6f)
            {
                if(game.player.Damage(damage,transform.position.x,!boss))
                {
                    OnParried(game.player.facing);
                    return true;
                }
            }
            return false;
        }
        public void OnParried(float fromFacing)
        {
            if(Dead||boss)return;
            if(family=="fool"||family=="wheelcarrier"||family=="mudcrawler"||family=="lavia")
            {BeginHurt(.6f);game.Sfx("ENEMY_STUNT");return;}
            stun=SourceGameplayTuning.ExecutionStunTime;
            state=8;
            timer=family=="flagellant"?SourceGameplayTuning.FlagellantParryReactionTime:SourceGameplayTuning.AcolyteParryReactionTime;
            if(trialAI!=null)trialAI.NotifyInterrupted();
            if(trialBoss!=null)trialBoss.NotifyInterrupted();
            if(laviaBoss!=null)laviaBoss.NotifyInterrupted();
            motor.velocity.x=fromFacing*3f;
            Play("parry");
            if(executionPrompt!=null)executionPrompt.gameObject.SetActive(true);
            game.Sfx("ENEMY_STUNT");
            game.Message("PARRIED · COUNTERATTACK");
        }
        public void StunEnemy(float duration)
        {
            if(Dead||boss)return;
            stun=duration;
            state=7;
            timer=duration;
            if(trialAI!=null)trialAI.NotifyInterrupted();
            if(trialBoss!=null)trialBoss.NotifyInterrupted();
            if(laviaBoss!=null)laviaBoss.NotifyInterrupted();
            motor.velocity.x=-facing*2f;
            Play("stun",true);
            if(executionPrompt!=null)executionPrompt.gameObject.SetActive(true);
            game.Sfx("ENEMY_STUNT");
        }
        public void Damage(float amount){Damage(amount,false);}
        public void Damage(float amount,bool trialMelee)
        {
            if(Dead||(boss&&(!bossEncounterReady||!game.BossFightActive)))return;
            if(game?.controls?.debugUI!=null)amount=game.controls.debugUI.EnemyHitDamage(amount,health);
            if(trialMelee&&trialAI!=null&&trialAI.TryGuardHit(amount))return;
            health=Mathf.Max(0,health-amount);
            if(Dead){motor.velocity=Vector2.zero;stun=0;if(executionPrompt!=null)executionPrompt.gameObject.SetActive(false);Play("death");GrantPurge();if(boss){bossDeathClock=0;bossDeathResolved=false;game.BossDefeated();}}
            else if(!boss&&stun<=0&&(laviaBoss==null||laviaBoss.CanStagger))BeginHurt();
            if(Dead&&trialAI!=null)trialAI.NotifyInterrupted();
            if(Dead&&trialBoss!=null)trialBoss.NotifyDefeated();
            if(Dead&&laviaBoss!=null)laviaBoss.NotifyDefeated();
        }
        void BeginHurt(float minimumRecovery=0)
        {
            // Receiving a hit must interrupt the AI as well as its animation.
            // Discard pending melee hits/displacement before playing Hurt.
            state=HurtState;attackClock=flagellantDisplacement=wheelAttackLapse=turnClock=0;
            hit=hit2=false;motor.velocity.x=0;
            float duration=Play("hurt");
            timer=Mathf.Max(duration,minimumRecovery,family=="flagellant"?SourceGameplayTuning.FlagellantHurtRecoveryTime:0);
            if(trialAI!=null)trialAI.NotifyInterrupted();
            if(trialBoss!=null)trialBoss.NotifyInterrupted();
            if(laviaBoss!=null)laviaBoss.NotifyInterrupted();
        }
        public float Execute()
        {
            if(Dead||boss)return 0;health=0;GrantPurge();motor.velocity=Vector2.zero;stun=0;state=9;executionClock=0;executionAudioStage=0;if(executionPrompt!=null)executionPrompt.gameObject.SetActive(false);
            if(trialAI!=null)trialAI.NotifyInterrupted();
            if(trialBoss!=null)trialBoss.NotifyDefeated();
            if(laviaBoss!=null)laviaBoss.NotifyDefeated();
            float duration=actor.Play(family=="flagellant"?"Flagellant_execution_anim":"acolyte_execution_anim",false,true);executionDuration=duration;
            game.Sfx(family=="flagellant"?"EXECUTION_EFFECT":"ACOLYTE_EXECUTION_2");game.effects.Burst(transform.position+Vector3.up,Color.red);
            return duration;
        }
        void GrantPurge(){if(rewardGranted)return;rewardGranted=true;if(game!=null)game.EnemyDefeated(purgeReward);}
        void ExecutionAudioTick(float dt)
        {
            if(state!=9)return;executionClock+=dt;
            if(family=="flagellant")
            {
                // Exact Flagellant_execution_anim events from the Android source:
                // sound 0.88, shakes 1.02/1.72/2.44, sound params 1.54/2.22,
                // camera zoom-out 3.20, interaction/audio end 4.23 seconds.
                if(executionAudioStage==0&&executionClock>=.88f){game.Sfx("EXECUTION_FIRST_HIT");executionAudioStage++;}
                if(executionAudioStage==1&&executionClock>=1.02f){game.Shake(.1f);executionAudioStage++;}
                if(executionAudioStage==2&&executionClock>=1.54f){game.Sfx("EXECUTION_SECOND_HIT");executionAudioStage++;}
                if(executionAudioStage==3&&executionClock>=1.72f){game.Shake(.12f);executionAudioStage++;}
                if(executionAudioStage==4&&executionClock>=2.22f){game.Sfx("EXECUTION_THIRD_HIT");executionAudioStage++;}
                if(executionAudioStage==5&&executionClock>=2.44f){game.Shake(.15f);executionAudioStage++;}
                if(executionAudioStage==6&&executionClock>=3.2f){game.ZoomOutExecutionCamera();executionAudioStage++;}
                if(executionAudioStage==7&&executionClock>=4.23f)executionAudioStage++;
            }
            else
            {
                // acolyte_execution_anim has no staged FMOD parameters. Its source
                // clip drives three camera/rumble impacts at these exact times.
                if(executionAudioStage==0&&executionClock>=2.13f){game.Sfx("EXECUTION_FIRST_HIT");game.Shake(.1f);executionAudioStage++;}
                if(executionAudioStage==1&&executionClock>=2.99f){game.Sfx("EXECUTION_SECOND_HIT");game.Shake(.12f);executionAudioStage++;}
                if(executionAudioStage==2&&executionClock>=3.71f){game.Sfx("EXECUTION_THIRD_HIT");game.Shake(.15f);executionAudioStage++;}
            }
            // Execution clips are composite sprites containing both actors. Once
            // their last frame is reached the defeated entity is disposed in the
            // source game; keeping that frame visible duplicates the live Player.
            if(executionDuration>0&&executionClock>=executionDuration)actor.visual.enabled=false;
        }
        void BossDeathTick(float dt)
        {
            motor.velocity=Vector2.zero;bossDeathClock+=dt;
            if(!bossDeathResolved&&bossDeathClock>=Mathf.Max(.1f,actor.catalog.Find("ElderBrother_Death")?.duration??2f))
            {bossDeathResolved=true;actor.Play("ElderBrother_Corpse",false,true);game.effects.Burst(transform.position+Vector3.up*1.5f,new Color(.55f,.02f,.02f));game.Shake(.35f);game.Message("REQUIEM AETERNAM");}
        }
    }
}
