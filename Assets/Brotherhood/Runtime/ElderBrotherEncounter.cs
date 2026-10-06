using System.Collections;
using UnityEngine;

namespace Brotherhood
{
    /// <summary>
    /// Restores the D17Z01S11 PlayMaker encounter flow and the exact timing from
    /// ElderBrotherIntroDummy / ElderBrotherBehaviour in the original sources.
    /// </summary>
    public sealed class ElderBrotherEncounter : MonoBehaviour
    {
        public const string RoomId="D17Z01S11";
        public const float BoundaryWidth=4f,BoundaryHeight=32f;
        public const float AreaOffset=3.7f,AreaSpacing=1.5f,AreaDuration=1.4f,AreaDamage=12f;
        public const int AreaCount=6;

        BrotherhoodGame game;RoomState room;EnemyController boss;Transform dummyRoot,cameraTarget,introJumpPoint,fightTrigger;
        SpriteActor dummyActor;Vector3 dummyStart;Coroutine sequence;WardenAchievementUI achievement;BossDefeatedUI victory;bool waitingForTrigger;

        public void Initialize(BrotherhoodGame owner)
        {
            game=owner;room=owner.Find(RoomId);if(room==null)return;boss=room.Boss;if(boss!=null)boss.purgeReward=0;
            dummyRoot=Find("ElderBrotherIntroDummy");cameraTarget=Find("CameraTargetIntroBoss");introJumpPoint=Find("IntroJumpPoint");fightTrigger=Find("BossFight");
            if(dummyRoot!=null)
            {
                dummyStart=dummyRoot.localPosition;
                var visual=dummyRoot.GetComponentInChildren<SpriteRenderer>(true);
                if(visual!=null&&boss!=null)
                {
                    dummyActor=visual.GetComponent<SpriteActor>();if(dummyActor==null)dummyActor=visual.gameObject.AddComponent<SpriteActor>();
                    dummyActor.catalog=boss.actor.catalog;dummyActor.visual=visual;
                    // elderBroDummy.mat in the source scene uses this exact opaque
                    // blue-black tint.  The restored dummy shares the normal boss
                    // sprite sheet, so its renderer tint supplies the silhouette.
                    visual.color=new Color(0.12941177f,0.12941177f,0.16470589f,1f);
                }
                dummyRoot.gameObject.SetActive(false);
            }
            EnsureSourceBoundary("CombatBoundariesLeft");EnsureSourceBoundary("CombatBoundariesRight");SetBoundaries(false);
            achievement=gameObject.AddComponent<WardenAchievementUI>();achievement.Initialize();
            victory=gameObject.AddComponent<BossDefeatedUI>();victory.Initialize(game);
        }

        Transform Find(string objectName)
        {
            if(room==null)return null;
            foreach(var t in room.GetComponentsInChildren<Transform>(true))if(t.name==objectName)return t;
            return null;
        }

        void EnsureSourceBoundary(string objectName)
        {
            var t=Find(objectName);if(t==null)return;t.gameObject.layer=8;
            var box=t.GetComponent<BoxCollider2D>();if(box==null)box=t.gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger=false;box.offset=Vector2.zero;box.size=new Vector2(BoundaryWidth,BoundaryHeight);
        }

        void SetBoundaryState(bool leftActive,bool rightActive)
        {
            var left=Find("CombatBoundariesLeft");var right=Find("CombatBoundariesRight");
            if(left!=null)left.gameObject.SetActive(leftActive);if(right!=null)right.gameObject.SetActive(rightActive);
        }
        void SetBoundaries(bool active){SetBoundaryState(active,active);}

        void Update()
        {
            if(!waitingForTrigger||game==null||game.Current!=room||game.player==null)return;
            Vector2 center=fightTrigger!=null?(Vector2)fightTrigger.position:new Vector2(room.transform.position.x-1.5625f,room.transform.position.y+4f);
            Vector2 delta=(Vector2)game.player.transform.position-center;
            // The original BossFight object has a 4x10 trigger collider.
            if(Mathf.Abs(delta.x)<=2f&&Mathf.Abs(delta.y)<=5f)
            {
                waitingForTrigger=false;sequence=StartCoroutine(SourceIntro());
            }
        }

        public void OnRoomEntered(RoomState entered)
        {
            waitingForTrigger=false;if(sequence!=null){StopCoroutine(sequence);sequence=null;}
            if(victory!=null)victory.Hide();
            if(dummyRoot!=null){dummyRoot.localPosition=dummyStart;dummyRoot.gameObject.SetActive(false);}
            if(entered!=room){SetBoundaries(false);game.SetBossFightActive(false);game.EndCinematicCamera();game.SetEncounterInputBlocked(false);return;}
            if(game.IsBossDefeated)
            {
                SetBoundaries(false);if(boss!=null)boss.ApplySavedBossDefeat();return;
            }
            if(boss!=null){boss.PrepareBossEncounter();SetBoundaryState(false,true);waitingForTrigger=true;}
        }

        IEnumerator SourceIntro()
        {
            game.player.EnterBossCinematicIdle();
            game.SetEncounterInputBlocked(true);game.SetBossFightActive(false);SetBoundaries(true);
            game.BeginCinematicCamera(cameraTarget!=null?cameraTarget:dummyRoot,5.625f);
            if(dummyRoot!=null&&dummyActor!=null)
            {
                dummyRoot.localPosition=dummyStart;dummyRoot.gameObject.SetActive(true);
                yield return SmashBeat();
                yield return new WaitForSeconds(1f);
                yield return SmashBeat();
                yield return new WaitForSeconds(1.6f);
                dummyActor.Play("ElderBrother_JumpLoop",true,true);game.Sfx("ELDER_BROTHER_JUMP");
                // ElderBrotherIntroDummy.SetMidAir waits 0.7 s before its 0.8 s
                // OutCubic exit tween in the original game.
                yield return new WaitForSeconds(.7f);
                Vector3 from=dummyRoot.localPosition;float elapsed=0;
                while(elapsed<.8f)
                {
                    elapsed+=Time.deltaTime;float t=Mathf.Clamp01(elapsed/.8f);float eased=1f-Mathf.Pow(1f-t,3f);
                    dummyRoot.localPosition=from+Vector3.up*(12f*eased);yield return null;
                }
                yield return new WaitForSeconds(.1f);dummyRoot.gameObject.SetActive(false);
            }
            boss.BeginBossIntroJump(introJumpPoint!=null?(Vector2)introJumpPoint.position:(Vector2)boss.transform.position);
            game.BeginCinematicCamera(boss.transform,5.625f);
            while(game.Current==room&&!boss.BossIntroComplete)yield return null;
            if(game.Current!=room)yield break;
            yield return new WaitForSeconds(1f);
            game.SetBossFightActive(true);boss.ActivateBossCombat();game.EndCinematicCamera();game.SetEncounterInputBlocked(false);
            sequence=null;
        }

        IEnumerator SmashBeat()
        {
            dummyActor.Play("ElderBrother_SmashPreparation",false,true);game.Sfx("ELDER_BROTHER_PRE_ATTACK");
            yield return new WaitForSeconds(1f);
            dummyActor.Play("ElderBrother_SmashAttack",false,true);game.Sfx("ELDER_BROTHER_ATTACK");
            yield return new WaitForSeconds(.3f);game.Shake(.3f);
        }

        public void OnBossDefeated(bool firstGrant)
        {
            if(sequence!=null)StopCoroutine(sequence);sequence=StartCoroutine(SourceDeath(firstGrant));
        }

        IEnumerator SourceDeath(bool firstGrant)
        {
            game.SetBossFightActive(false);game.SetEncounterInputBlocked(true);game.BeginCinematicCamera(boss.transform,5.2f);
            while(game.Current==room&&!boss.BossCorpseShown)yield return null;
            if(game.Current!=room)yield break;
            yield return new WaitForSeconds(.6f);SetBoundaries(false);game.EndCinematicCamera();
            if(game.bloodyBaptism!=null)yield return game.bloodyBaptism.Play();
            if(game.Current!=room)yield break;
            if(victory!=null)
            {
                victory.Show();
                while(game.Current==room&&victory.IsShowing)yield return null;
            }
            if(game.Current!=room)yield break;
            game.SetEncounterInputBlocked(false);
            if(firstGrant&&achievement!=null&&GameSettings.AchievementPopups)achievement.Show();
            sequence=null;
        }
    }
}
