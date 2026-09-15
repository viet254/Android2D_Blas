using UnityEngine;
namespace Brotherhood
{
    public sealed class EnemyController : MonoBehaviour
    {
        public BrotherhoodGame game; public KinematicMotor motor; public SpriteActor actor;
        public bool boss; public string family="acolyte"; public float maxHealth=90,health=90;public int purgeReward;
        public float Radius=>boss?1.3f:.4f; public bool Dead=>health<=0; public bool Stunned=>stun>0;public bool ExecutionReady=>!boss&&!Dead&&stun>0;
        Vector2 origin;float timer,stun;int state;bool hit,rewardGranted;float facing=-1;float jumpTarget,executionClock,executionDuration,bossDeathClock;int executionAudioStage,lastBossAttack;bool bossDeathResolved;SpriteActor executionPrompt;
        public bool BossIntroComplete=>boss&&state!=5;
        public bool PhaseTwo=>boss&&health<=maxHealth*.5f;
        public bool BossCorpseShown=>boss&&bossDeathResolved;
        public void Initialize(){origin=transform.position;if(!boss){var p=new GameObject("Execution prompt");p.transform.SetParent(transform,false);p.transform.localPosition=new Vector3(0,2.05f,-.1f);executionPrompt=p.AddComponent<SpriteActor>();executionPrompt.catalog=actor.catalog;executionPrompt.visual=p.AddComponent<SpriteRenderer>();executionPrompt.visual.sharedMaterial=actor.visual.sharedMaterial;executionPrompt.visual.sortingOrder=32000;executionPrompt.Play("flagellant_execution_awareness_anim",true,true);}ResetEnemy();}
        public void ResetEnemy(){health=maxHealth;timer=boss?.65f:1;stun=0;state=boss?5:0;hit=rewardGranted=false;executionClock=executionDuration=bossDeathClock=0;executionAudioStage=lastBossAttack=0;bossDeathResolved=false;motor.Teleport(boss?origin+Vector2.up*6:origin);gameObject.SetActive(true);actor.visual.enabled=true;if(executionPrompt!=null)executionPrompt.gameObject.SetActive(false);Play(boss?"jump":"idle",true);}
        float attackClock;
        void Play(string action,bool loop=false)
        {
            string clip;
            if(boss)clip=action=="idle"?"ElderBrother_Idle":action=="attack"?"ElderBrother_SmashAttack":action=="windup"?"ElderBrother_SmashPreparation":action=="jump"?"ElderBrother_JumpLoop":action=="land"?"ElderBrother_JumpLanding":"ElderBrother_Death";
            else if(family=="flagellant")clip=action=="attack"?"NewFlagellant_attack":action=="hurt"?"NewFlagellant_hurt":action=="parry"?"NewFlagellant_parry_reaction_anim":action=="stun"?"Flagellant_stunt_anim":action=="death"?"NewFlagellant_death_gore":action=="walk"?"NewFlagellant_walk":"NewFlagellant_idle";
            else clip=action=="idle"?"acolyte_idle":action=="walk"?"acolyte_running":action=="attack"?"acolyte_attack":action=="hurt"?"acolyte_get_hit_low":action=="parry"?"acolyte_parry_reaction_anim":action=="stun"?"acolyte_stunned_anim":action=="death"?"acolyte_death_gore":"acolyte_idle";
            actor.Play(clip,loop,true);
        }
        public void Tick(float dt)
        {
            if(Dead){if(boss)BossDeathTick(dt);else ExecutionAudioTick(dt);return;}
            timer-=dt;stun-=dt;
            var player=game.player;
            if(executionPrompt!=null)executionPrompt.gameObject.SetActive(ExecutionReady&&player.motor.grounded&&motor.grounded&&Mathf.Abs(player.transform.position.y-transform.position.y)<=.45f&&Vector2.Distance(player.transform.position,transform.position)<1.8f);
            float dx=player.transform.position.x-transform.position.x;
            facing=Mathf.Abs(dx)>.01f?Mathf.Sign(dx):facing;actor.Face(facing);
            if(stun>0)
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
            if(player.Dead){motor.velocity.x=0;motor.Step(dt);return;}
            if(boss)BossTick(dt,dx);else NormalTick(dt,dx);
            motor.Step(dt);
            if(transform.position.y<game.Current.KillY)Damage(999);
        }
        void NormalTick(float dt,float dx)
        {
            if(state==0)
            {
                float direction=Mathf.Abs(dx)<7?facing:(transform.position.x>origin.x+2?-1:transform.position.x<origin.x-2?1:facing);
                bool floor=Physics2D.Raycast((Vector2)transform.position+new Vector2(direction*.65f,.4f),Vector2.down,1.2f,1<<8).collider!=null;
                motor.velocity.x=floor?direction*1.15f:0;
                if(actor.Current!=(family=="flagellant"?"NewFlagellant_walk":"acolyte_running"))Play("walk",true);
                float range=family=="flagellant"?2.8f:2.1f;
                if(Mathf.Abs(dx)<range && timer<=0){state=1;attackClock=0;timer=family=="flagellant"?1.3f:1.9f;motor.velocity.x=0;Play("attack");hit=false;}
            }
            else if(state==1)
            {
                attackClock+=dt;
                float hitTime=family=="flagellant"?SourceGameplayTuning.FlagellantAttackHitTime:SourceGameplayTuning.AcolyteAttackHitTime;
                if(!hit && attackClock>=hitTime)
                {
                    hit=true;
                    if(Strike(family=="flagellant"?3f:2.3f,family=="flagellant"?20:18))return;
                }
                if(timer<=0){state=2;timer=family=="flagellant"?.6f:.5f;}
            }
            else if(state==2 && timer<=0){state=0;timer=.3f;Play("idle",true);}
        }
        void BossTick(float dt,float dx)
        {
            motor.velocity.x=0;
            if(state==5&&timer<=0&&motor.grounded){state=0;timer=1f;Play("land");game.Shake(.28f);game.Sfx("ELDER_BROTHER_LANDING");game.Message("WARDEN OF THE SILENT SORROW");}
            else if(state==0 && timer<=0)
            {
                // Source D17 configuration disables repeated attacks. Alternate the
                // AREA and JUMP actions, using its serialized 1 s recovery windows.
                if(lastBossAttack==1){lastBossAttack=2;state=3;timer=.63f;jumpTarget=game.player.transform.position.x;Play("windup");game.Sfx("ELDER_BROTHER_JUMP_VOICE",.75f);}
                else {lastBossAttack=1;state=1;timer=.89f;Play("windup");game.Sfx("ELDER_BROTHER_PRE_ATTACK");game.Sfx("ELDER_BROTHER_ATTACK_VOICE",.7f);}
            }
            else if(state==1 && timer<=0){state=6;timer=.12f;Play("attack");game.Sfx("ELDER_BROTHER_ATTACK");}
            else if(state==6 && timer<=0){state=2;timer=.53f;Strike(3.7f,28);game.Sfx("ELDER_BROTHER_ATTACK_HIT");game.Shake(.12f);game.effects.Burst(transform.position+Vector3.right*facing*2,Color.gray);}
            else if(state==2 && timer<=0){state=0;timer=1f;Play("idle",true);}
            else if(state==3 && timer<=0){state=4;motor.velocity.y=13;timer=.15f;Play("jump",true);game.Sfx("ELDER_BROTHER_JUMP");}
            else if(state==4)
            {
                motor.velocity.x=Mathf.Clamp((jumpTarget-transform.position.x)*2,-7,7);
                if(motor.grounded && timer<=0)
                {state=7;timer=.07f;Play("land");}
            }
            else if(state==7&&timer<=0){state=2;timer=.93f;game.Sfx("ELDER_BROTHER_LANDING");Strike(3,30);game.Shake(.25f);if(health<maxHealth*.5f){game.Sfx("ELDER_BROTHER_CORPSE_WAVE");game.effects.Waves(transform.position,game);}}
        }
        bool Strike(float range,float damage)
        {
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
            stun=SourceGameplayTuning.ExecutionStunTime;
            state=8;
            timer=family=="flagellant"?SourceGameplayTuning.FlagellantParryReactionTime:SourceGameplayTuning.AcolyteParryReactionTime;
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
            motor.velocity.x=-facing*2f;
            Play("stun",true);
            if(executionPrompt!=null)executionPrompt.gameObject.SetActive(true);
            game.Sfx("ENEMY_STUNT");
        }
        public void Damage(float amount)
        {
            if(Dead)return;health=Mathf.Max(0,health-amount);
            if(Dead){motor.velocity=Vector2.zero;Play("death");GrantPurge();if(boss){bossDeathClock=0;bossDeathResolved=false;game.BossDefeated();}}
            else if(!boss)Play("hurt");
        }
        public float Execute()
        {
            if(Dead||boss)return 0;health=0;GrantPurge();motor.velocity=Vector2.zero;stun=0;state=9;executionClock=0;executionAudioStage=0;if(executionPrompt!=null)executionPrompt.gameObject.SetActive(false);
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
