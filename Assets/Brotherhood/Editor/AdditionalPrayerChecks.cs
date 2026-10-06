using System;
using UnityEngine;

namespace Brotherhood
{
    // Exercises the restored entity prayers separately from the catalog smoke
    // tests, using a temporary flat floor so room slopes cannot mask collision.
    public static class AdditionalPrayerChecks
    {
        public static void Run(BrotherhoodGame game,Action<bool,string> check,Action<float> Step,Action<Vector2> Place)
        {
            game.progress=new PlayerProgress();game.Enter("D17Z01S02",null,null);
            foreach(var enemy in game.Current.enemies)enemy.health=0;
            var target=game.Current.enemies[0];var effects=game.prayerEffects;
            Vector2 floor=(Vector2)game.Current.start.position+Vector2.up*30;
            var ground=new GameObject("Prayer verification floor");ground.layer=8;
            ground.transform.position=floor-Vector2.up*.1f;
            ground.AddComponent<BoxCollider2D>().size=new Vector2(70,.2f);
            var wall=new GameObject("Prayer verification wall");wall.layer=8;
            wall.transform.position=floor+new Vector2(3,2);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.1f,4);wall.SetActive(false);
            Physics2D.SyncTransforms();
            Action<float> Advance=seconds=>
            {
                for(float remaining=seconds;remaining>.00001f;)
                {float dt=Mathf.Min(1f/120,remaining);effects.Tick(dt);remaining-=dt;}
            };
            Action<string> Reset=id=>
            {
                target.health=0;game.progress.equippedSwordHeart="";game.progress.equippedRosaryBeads=Array.Empty<string>();
                game.progress.equippedPrayer=id;game.progress.hasPrayer=true;
                Place(floor+Vector2.up*.025f);game.player.facing=1;
                floor=game.player.transform.position;Physics2D.SyncTransforms();
            };
            Action<Vector2> Target=at=>
            {target.ResetEnemy();target.health=1000;target.motor.Teleport(at);Physics2D.SyncTransforms();};
            Action CastInput=()=>
            {game.controls.Prayer=true;Step(.02f);game.controls.Prayer=false;};
            try
            {
                Reset("PR07");Target(floor+Vector2.right*7);game.player.fervour=60;CastInput();
                check(game.player.PrayerCasting&&target.health==1000&&game.player.fervour==60,"Lorquiana waits for the source cast event before consuming Fervour or firing");
                Step(.56f);check(target.health==910&&game.player.fervour==24,"Lorquiana's first source shot deals ninety and its successful hit restores four Fervour");
                Step(.14f);check(target.health==820,"Lorquiana fires its second independent shot after 0.15 seconds");
                Step(.16f);check(target.health==730,"Lorquiana fires three source shots without a generic extra attack");
                Step(1.1f);check(effects.ActiveActors==0&&string.IsNullOrEmpty(effects.RunningPrayer),"Lorquiana disposes beams and impact actors after its effect");
                Reset("PR07");Target(floor+Vector2.right*7);wall.SetActive(true);Physics2D.SyncTransforms();effects.Cast("PR07",1);
                check(target.health==910,"Lorquiana preserves its authored zero collision mask and reaches a target behind terrain");
                effects.Stop();wall.SetActive(false);Physics2D.SyncTransforms();
                Reset("PR07");game.player.facing=-1;Target(floor+Vector2.left*7);effects.Cast("PR07",1);
                check(target.health==910,"Lorquiana mirrors its source attack direction when facing left");effects.Stop();

                Reset("PR09");Target(floor+new Vector2(1.25f,14));game.player.fervour=60;CastInput();Step(.56f);
                check(target.health==980&&game.player.fervour==20,"Taranto's twenty-unit vertical collider hits above ground and its temporal flag stops Fervour collection");
                Step(.14f);check(target.health<=960,"Taranto repeats source twenty-damage hits every 0.12 seconds");
                Step(2);check(effects.ActiveActors==0,"Taranto releases all six column actors after completion");
                Reset("PR09");Target(floor+new Vector2(3.75f,4));wall.SetActive(true);Physics2D.SyncTransforms();effects.Cast("PR09",2);Advance(1);
                check(target.health==1000,"Taranto's summon line stops subsequent columns at the authored terrain collision");effects.Stop();wall.SetActive(false);Physics2D.SyncTransforms();

                Reset("PR101");effects.Cast("PR101",20);Advance(.55f);Vector2 guardian=effects.CompanionPosition;
                check(Vector2.Distance(guardian,floor)<2,"Guardian appears at its master and follows the authored two-unit offset");
                Target(guardian+new Vector2(3.25f,1));game.player.fervour=0;
                game.effects.InterceptProjectiles(bounds=>true);
                Vector2 guardianAttackCenter=guardian+new Vector2(2+1.340443f,2.0174785f);
                game.effects.Waves(guardianAttackCenter-Vector2.up*.2f,game);
                game.effects.Waves(guardianAttackCenter+new Vector2(4.25f,2.35f),game);
                effects.PlayerAttack();Advance(.5f);
                check(target.health==1000,"Guardian waits for its WeaponAttack animation event at 0.53 seconds");
                Advance(.06f);check(target.health==820,"Guardian applies its source Strength of 180 through the horizontal capsule");
                check(game.player.fervour==8,"Guardian's Heavy source hit gains eight Fervour");
                check(CountWaves(game)==2,"Guardian destroys projectiles inside its capsule while preserving projectiles in the enclosing rectangle's corner");
                game.effects.InterceptProjectiles(bounds=>true);
                Advance(.6f);guardian=effects.CompanionPosition;
                Vector2 capsuleCenter=guardian+new Vector2(2+1.340443f,2.0174785f);
                Target(capsuleCenter+new Vector2(4.5f,2.45f));effects.PlayerAttack();Advance(.6f);
                check(target.health==1000,"Guardian's rounded capsule excludes a target in the enclosing rectangle's corner");
                Advance(.6f);target.health=0;effects.PlayerParry();check(effects.GuardianGuarding,"The player's shield command starts Guardian's source guard action");
                float health=game.player.health;game.player.Damage(10,floor.x-3,false);game.player.DamageContact(10,floor.x+3);
                check(game.player.health==health,"Guardian's guard protects the master from attack and contact damage");
                Advance(.8f);check(!effects.GuardianGuarding&&!effects.BlocksAttack(floor.x+3,false),"Guardian releases protection after both guard animations finish");
                Advance(20);check(effects.ActiveActors==0,"Guardian's vanish animation releases its actor after the source duration");
                Reset("PR101");effects.Cast("PR101",20);game.progress.equippedPrayer="PR07";Advance(.02f);
                check(effects.ActiveActors==0,"Unequipping Guardian immediately releases its summoned actor");

                Reset("PR201");effects.Cast("PR201",20);Advance(.6f);Target(floor+new Vector2(7,1));Advance(1);
                check(target.health==1000&&effects.MiriamShardCount==0,"Miriam waits for the player's sword command instead of automatically selecting enemies");
                game.player.fervour=0;game.effects.InterceptProjectiles(bounds=>true);
                game.effects.Waves(floor+new Vector2(7,1),game);
                game.effects.Waves(floor+new Vector2(9.75f,2.25f),game);
                effects.PlayerAttack();Advance(.8f);
                check(target.health==900&&target.Stunned,"Miriam's source polygon deals one hundred damage and stuns a surviving ordinary enemy");
                check(game.player.fervour==4,"Miriam's OptionalStunt source hit gains four Fervour without Heavy context");
                check(CountWaves(game)==2,"Miriam destroys projectiles inside its source polygon while preserving the polygon rectangle's corner");
                game.effects.InterceptProjectiles(bounds=>true);
                check(effects.MiriamShardCount>0,"Miriam's grounded attack begins its source twelve-shard summon line");
                target.health=0;Advance(.5f);check(effects.MiriamShardCount==12,"Miriam completes all twelve authored shard summons");
                Advance(3);check(effects.ActiveActors==0,"Miriam keeps landing shards alive through their loops and fades, then releases all actors");
                Reset("PR201");effects.Cast("PR201",20);Advance(.6f);Target(floor+Vector2.right*25);
                game.player.fervour=0;game.effects.InterceptProjectiles(bounds=>true);effects.PlayerAttack();Advance(.42f);
                check(target.health==1000,"Miriam opens its hit window without damaging a target outside the current polygon");
                Target(effects.CompanionPosition+new Vector2(-1,1));game.effects.Waves(effects.CompanionPosition+new Vector2(-1,1),game);Advance(.03f);
                check(target.health==900&&target.Stunned,"Miriam's moving source polygon hits a target that enters after WeaponAttack");
                check(CountWaves(game)==0,"Miriam destroys a projectile entering after its hit window has already opened");
                Advance(.12f);target.motor.Teleport(floor+Vector2.right*25);Physics2D.SyncTransforms();Advance(.02f);
                target.motor.Teleport(effects.CompanionPosition+new Vector2(-1,1));Physics2D.SyncTransforms();Advance(.02f);
                check(target.health==900&&game.player.fervour==4,"Miriam's already-damaged set prevents repeated damage and Fervour even after leaving and re-entering the window");
                Advance(.18f);Target(effects.CompanionPosition+new Vector2(-1,1));game.effects.Waves(effects.CompanionPosition+new Vector2(-1,1),game);Advance(.05f);
                check(target.health==1000&&CountWaves(game)==2,"WeaponAttackFinished closes Miriam's hitbox for late enemies and projectiles");
                game.effects.InterceptProjectiles(bounds=>true);effects.Stop();
                Reset("PR201");effects.Cast("PR201",20);Advance(.6f);Target(floor+new Vector2(14.075f,.2f));effects.PlayerAttack();Advance(1.2f);
                check(target.health==960,"Miriam's independently summoned landing shard deals the source forty damage");effects.Stop();
                CompanionModifierChecks(game,check,Reset,Target,Advance,floor);

                Reset("PR203");Target(floor+new Vector2(1.25f,-.55f));effects.Cast("PR203",6);Advance(.7f);
                check(target.health==1000&&effects.LightningPulses==0,"San Telmo's beams cannot damage enemies before the first reveal-and-charge pulse");
                Advance(.12f);float afterFirst=target.health;
                check(afterFirst==990&&effects.LightningPulses==1,"San Telmo opens its ten-damage beams after the reveal and charge sequence");
                Advance(.09f);check(target.health==afterFirst,"One San Telmo beam damages an enemy only once during its 0.3-second attack window");
                Advance(.22f);check(target.health==afterFirst-10,"A later San Telmo link independently damages an enemy at a crossing of two source beams");
                Advance(2.6f);check(effects.LightningPulses==3,"San Telmo emits the source three base pulses at 1.2-second intervals");
                Advance(3);check(effects.ActiveActors==0,"San Telmo completes sequential core fades and releases all beam actors");
                Reset("PR203");game.progress.equippedSwordHeart="HE01";effects.Cast("PR203",6);Advance(7);
                check(effects.LightningPulses==6,"HE01's five-second addition grants floor(5 * 0.6) extra San Telmo pulses");
                Advance(3);check(effects.ActiveActors==0,"San Telmo also cleans up its longer sequence with HE01 equipped");
                Reset("PR203");effects.Cast("PR203",6);game.player.health=0;Advance(.02f);
                check(effects.ActiveActors==0,"Player death cancels San Telmo cores and beams immediately");

                Reset("PR202");game.player.motor.grounded=false;
                check(!effects.CanCast("PR202"),"The source return prayer refuses an airborne cast");
                game.player.motor.grounded=true;check(effects.CanCast("PR202")==game.HasPrayerCheckpoint,"The return prayer requires an activated source checkpoint");
                Reset("PR101");effects.Cast("PR101",20);game.Enter("D17Z01S05",null,null);Advance(.02f);
                check(effects.ActiveActors==0,"Changing rooms cancels a summoned prayer and its animation callbacks");
            }
            finally
            {
                effects.Stop();target.health=0;game.controls.ClearGameplayInput();game.player.Restore();
                wall.SetActive(false);ground.SetActive(false);UnityEngine.Object.Destroy(wall);UnityEngine.Object.Destroy(ground);
                game.progress=new PlayerProgress();game.Enter("D17Z01S02",null,null);foreach(var enemy in game.Current.enemies)enemy.health=0;
                Place(game.Current.start.position);
            }
        }
        static void CompanionModifierChecks(BrotherhoodGame game,Action<bool,string> check,Action<string> Reset,Action<Vector2> Target,Action<float> Advance,Vector2 floor)
        {
            // Keep HE03's real low-Life condition while varying the stat under
            // test. Restoring the catalog in finally prevents fixture leakage.
            var effect=InventoryCatalog.Load().Find("HE03").effects[0];
            int stat=effect.statType;float value=effect.value,multiplier=effect.multiplier;
            try
            {
                effect.statType=15;effect.value=0;effect.multiplier=2;
                Reset("PR201");game.progress.equippedSwordHeart="HE03";
                game.prayerEffects.Cast("PR201",20);Advance(.6f);game.player.health=8;Target(floor+new Vector2(7,1));game.prayerEffects.PlayerAttack();Advance(.8f);
                check(targetHealth(game)==900,"Miriam preserves its native cached one-hundred damage hit when Life changes after casting");
                Reset("PR201");game.progress.equippedSwordHeart="HE03";game.player.health=8;
                game.prayerEffects.Cast("PR201",20);Advance(.6f);Target(floor+new Vector2(7,1));game.prayerEffects.PlayerAttack();Advance(.8f);
                check(targetHealth(game)==800,"Miriam captures a conditionally active stat 15 multiplier when its entity spawns");
                Reset("PR201");game.progress.equippedSwordHeart="HE03";game.player.health=8;
                game.prayerEffects.Cast("PR201",20);Advance(.6f);Target(floor+new Vector2(14.075f,.2f));game.prayerEffects.PlayerAttack();Advance(1.2f);
                check(targetHealth(game)==920,"Miriam's forty-damage landing shards also scale with stat 15");
                Reset("PR101");game.progress.equippedSwordHeart="HE03";
                game.prayerEffects.Cast("PR101",20);Advance(.55f);game.player.health=8;Target(game.prayerEffects.CompanionPosition+new Vector2(3.25f,1));game.prayerEffects.PlayerAttack();Advance(.6f);
                check(targetHealth(game)==820,"Guardian preserves its native cached one-hundred-eighty damage hit when Life changes after casting");
                Reset("PR101");game.progress.equippedSwordHeart="HE03";game.player.health=8;
                game.prayerEffects.Cast("PR101",20);Advance(.55f);Target(game.prayerEffects.CompanionPosition+new Vector2(3.25f,1));game.prayerEffects.PlayerAttack();Advance(.6f);
                check(targetHealth(game)==640,"Guardian captures a conditionally active stat 15 multiplier when its entity spawns");
                effect.statType=27;
                Reset("PR201");game.progress.equippedSwordHeart="HE03";game.player.health=8;
                game.prayerEffects.Cast("PR201",20);Advance(.6f);Target(floor+new Vector2(7,1));game.prayerEffects.PlayerAttack();Advance(.8f);
                check(targetHealth(game)==900,"Miriam does not apply PrayerStrength stat 27 to its general-damage source hit");
                game.prayerEffects.Stop();
            }
            finally{effect.statType=stat;effect.value=value;effect.multiplier=multiplier;game.progress.equippedSwordHeart="";game.player.Restore();}
        }
        static int CountWaves(BrotherhoodGame game)
        {
            int count=0;game.effects.InterceptProjectiles(bounds=>{count++;return false;});return count;
        }
        static float targetHealth(BrotherhoodGame game)=>game.Current.enemies[0].health;
    }
}
