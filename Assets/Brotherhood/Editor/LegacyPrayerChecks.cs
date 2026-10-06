#if UNITY_EDITOR
using System;
using System.Linq;
using Brotherhood;
using UnityEngine;

public static class LegacyPrayerChecks
{
    public static void Run(BrotherhoodGame game, Action<bool, string> Check,
        Action<float> Step, Action<Vector2> Place)
    {
        EnemyController target = null;
        Vector2 floor = Vector2.zero;
        void Prepare(string id, Vector2 targetOffset)
        {
            game.progress = new PlayerProgress();
            game.Enter("D17Z01S02", null, null);
            foreach (var enemy in game.Current.enemies) enemy.health = 0;
            Place(game.Current.start.position);
            floor = game.player.transform.position;
            game.progress.hasPrayer = true;
            game.progress.equippedPrayer = id;
            game.player.fervour = 60;
            target = game.Current.enemies[0];
            target.ResetEnemy();
            target.health = 10000;
            target.motor.Teleport(floor + targetOffset);
        }
        void Cast()
        {
            game.controls.Prayer = true; Step(.01f);
            game.controls.Prayer = false; Step(.56f);
        }
        SpriteActor Actor(string name) => game.legacyPrayers.GetComponentsInChildren<SpriteActor>()
            .FirstOrDefault(a => a.gameObject.activeSelf && a.name == name);
        bool Near(float a, float b) => Mathf.Abs(a - b) < .01f;

        foreach (string clip in new[] { "penitentBeam_startToWarning", "penitentBeam_warningToAttack",
            "penitentBeam_attackLoop", "penitentBeam_fade", "prayerHealingEffect", "AlliedCherub_flying",
            "penitent_blueFireFull", "PrayerShieldSprite", "penitent_guardian_lady_anim",
            "penitent_prayerPR12Action", "prayerPR12EnemyImpact", "pontiffOldman_toxicBurstToFog",
            "pontiffOldman_fogToIdle" })
            Check(game.player.actor.catalog.Find(clip) != null, "Prayer source timeline loads: " + clip);

        Prepare("PR03", new Vector2(2, 0)); Cast();
        Check(Near(game.player.fervour, 20) && Near(target.health, 9945),
            "Debla charges forty and starts its fifty-five-damage column at the source cast event");
        Step(.23f);
        Check(Near(target.health, 9945), "Debla waits the source 0.28-second interval between damage ticks");
        Step(.07f);
        Check(Near(target.health, 9890), "Debla applies its second column tick instead of one broad instant hit");
        Step(.12f);
        Check(game.legacyPrayers.PlayerTint != Color.white,
            "Debla enables its player blue tint after the source 0.4-second delay");
        Step(.5f);
        Check(Near(target.health, 9780), "Debla deals exactly four source ticks during its one-second damage loop");
        Step(.35f);
        Check(game.legacyPrayers.PlayerTint == Color.white,
            "Debla restores player color after its source 0.8-second tint interval");
        Step(2);
        Check(game.legacyPrayers.ActiveActors == 0, "Debla beam and independent impact sprites expire completely");
        Prepare("PR03", new Vector2(2, 11)); Cast();
        Check(Near(target.health, 9945), "Debla uses the source twelve-unit upward column collider");
        Prepare("PR03", new Vector2(4.2f, 0)); Cast(); Step(1.1f);
        Check(Near(target.health, 10000), "Debla does not damage an enemy beyond its 6.8-unit column width");

        Prepare("PR05", new Vector2(4, 0)); Cast();
        Check(Near(game.player.fervour, 0) && Near(target.health, 10000) && game.legacyPrayers.AvailableCherubs == 1,
            "Campanillero charges sixty and deploys a finite court without instant area damage");
        Step(.5f);
        Check(game.legacyPrayers.AvailableCherubs == 2 && Near(target.health, 10000),
            "Campanillero deploys cherubs at half-second intervals and waits for target acquisition");
        Step(1.35f);
        Check(Near(target.health, 10000), "The newly deployed source cherub waits its one-second attack cooldown before charging");
        Step(.22f);
        var chargingCherub = Actor("Allied cherub 3");
        Check(chargingCherub != null && chargingCherub.visual.color.b > chargingCherub.visual.color.r && Near(target.health, 10000),
            "An allied cherub visibly charges its railgun before dealing damage");
        Step(.3f);
        Check(Near(target.health, 10000), "An allied cherub must finish its full 0.6-second railgun charge before dealing damage");
        Step(.25f);
        Check(Near(target.health, 9960), "An allied cherub fires once with its mobile forty-damage base strength");
        Step(5.3f);
        Check(Near(target.health, 9720) && game.legacyPrayers.AvailableCherubs == 0,
            "Exactly seven source cherubs are consumed after seven attacks, with no perpetual area pulses");
        Step(1);
        Check(game.legacyPrayers.ActiveActors == 0, "Spent cherubs and their departure effects release all actors");
        Prepare("PR05", new Vector2(15, 0)); Cast(); Step(4);
        Check(Near(target.health, 10000) && game.legacyPrayers.AvailableCherubs == 7,
            "Allied cherubs follow their owner while no enemy is inside the source ten-unit targeting range");
        Step(12);
        Check(game.legacyPrayers.AvailableCherubs == 0 && game.legacyPrayers.ActiveActors == 0,
            "Unused allied cherubs are stored when their fixed fifteen-second source duration ends");

        Prepare("PR08", new Vector2(2, 0)); Cast();
        Check(Near(game.player.fervour, 20), "Zarabanda charges forty at the cast event before its first physical shield hit");
        Step(2);
        Check(target.health < 10000 && game.legacyPrayers.ActiveActors == 2,
            "Zarabanda charges forty and its two orbiting circles damage enemies they physically touch");
        Check(!game.itemEffects.HasTemporalFlag(1), "Zarabanda does not grant the invulnerability flag");
        float health = game.player.health;
        game.player.Damage(10, game.player.transform.position.x + 2, false);
        Check(Near(game.player.health, health - 10), "Zarabanda leaves the player vulnerable to an unblocked direct enemy hit");
        var shield = Actor("Zarabanda shield 0");
        Check(shield != null && Vector2.Distance(shield.transform.position, game.player.transform.position + Vector3.up) > 1.8f,
            "Zarabanda expands to the original two-unit orbit radius");
        Step(8.25f);
        shield = Actor("Zarabanda shield 0");
        Check(game.legacyPrayers.ActiveActors == 2 && shield != null &&
            Vector2.Distance(shield.transform.position, game.player.transform.position + Vector3.up) < 1.8f,
            "Zarabanda contracts its two shields for one second after the ten-second buff ends");
        Step(1.3f);
        Check(game.legacyPrayers.ActiveActors == 0, "Zarabanda disposes both shields after contraction");

        Prepare("PR11", new Vector2(4, 0)); Cast();
        Check(Near(game.player.fervour, 20) && game.legacyPrayers.ActiveActors == 0,
            "Tiento charges forty and waits for an incoming hit before showing the thorned lady");
        health = game.player.health;
        game.player.Damage(10, game.player.transform.position.x + 2, false);
        Check(Near(game.player.health, health) && Actor("Thorned lady protection")?.Current == "penitent_guardian_lady_anim",
            "Tiento blocks an incoming strike and shows its original hurt-trigger protection animation");
        game.player.DamageContact(10, game.player.transform.position.x - 2);
        Check(Near(game.player.health, health) && game.legacyPrayers.ActiveActors == 1,
            "Tiento also blocks contact without duplicating an already playing protection animation");
        Step(.7f);
        Check(game.legacyPrayers.ActiveActors == 0, "The thorned lady fades after the source protection animation");
        Step(7.5f);
        game.player.Damage(10, game.player.transform.position.x + 2, false);
        Check(Near(game.player.health, health - 10) && !game.itemEffects.HasTemporalFlag(1),
            "Tiento removes invulnerability after eight seconds and allows subsequent damage");

        Prepare("PR12", new Vector2(5, 0));
        var second = game.Current.enemies[1]; second.ResetEnemy(); second.health = 10000;
        second.motor.Teleport(floor + new Vector2(-5, 0)); Cast();
        Check(Near(game.player.fervour, 4) && Near(target.health, 9790) && Near(second.health, 10000),
            "Cante Jondo charges sixty and hits the first enemy for its original two-hundred-ten damage");
        Step(.4f);
        Check(Near(second.health, 10000), "Cante Jondo preserves the mobile half-second delay between targets");
        Step(.14f);
        Check(Near(second.health, 9790), "Cante Jondo reaches an enemy behind the player inside its circular area");
        Step(1.5f);
        Check(game.legacyPrayers.ActiveActors == 0, "Cante Jondo releases invocation and enemy impact sprites");
        Prepare("PR12", new Vector2(0, 14)); Cast();
        Check(Near(target.health, 9790), "Cante Jondo reaches elevated targets within its original twenty-unit radius");
        Prepare("PR12", new Vector2(23, 0)); Cast(); Step(1);
        Check(Near(target.health, 10000), "Cante Jondo excludes enemies beyond its twenty-unit radius");

        Prepare("PR14", new Vector2(3, 0)); Cast();
        Check(Near(game.player.fervour, 40) && game.effects.ActivePrayerCrawlers == 2,
            "Verdiales charges twenty and launches exactly two ground-crawling source discs");
        Step(.4f);
        Check(target.health < 10000 && game.itemEffects.HasTemporalFlag(1),
            "Verdiales damages an intersecting enemy and retains its source two-second casting protection");
        float crawlerHealth = target.health;
        game.effects.Tick(0);
        Check(Near(target.health, crawlerHealth), "Paused Verdiales projectiles cannot move or deal extra damage");
        Step(2);
        Check(game.effects.ActivePrayerCrawlers == 0 && !game.itemEffects.HasTemporalFlag(1),
            "Verdiales removes both projectiles and casting protection after two seconds");

        Prepare("PR15", new Vector2(1.7f, 0)); Cast();
        Check(Near(game.player.fervour, 20) && game.legacyPrayers.ActiveClouds == 1 && Near(target.health, 10000),
            "Crimson Mist charges forty and creates one stationary cloud without an immediate generic hit");
        Step(.4f);
        Check(Near(target.health, 10000), "A blood cloud waits its source half-second area-effect interval");
        Step(.15f);
        Check(Near(target.health, 9975), "A blood cloud deals the source twenty-five damage at each area-effect tick");
        var cloud = Actor("Crimson mist"); Vector3 cloudOrigin = cloud.transform.position;
        game.player.motor.Teleport(floor + Vector2.right * 5); Step(.65f);
        Check(cloud != null && Vector3.Distance(cloud.transform.position, cloudOrigin) < .001f && target.health <= 9950,
            "An existing blood cloud stays behind and continues damaging its own occupants when the player moves");
        Check(game.legacyPrayers.ActiveClouds >= 2,
            "Crimson Mist creates another cloud at the player's new location on its one-second source interval");
        Step(9.3f);
        Check(game.legacyPrayers.ActiveClouds > 0,
            "Existing blood clouds finish their three-second lifetimes after the ten-second emitter stops");
        Step(3);
        Check(game.legacyPrayers.ActiveClouds == 0 && game.legacyPrayers.ActiveActors == 0,
            "Crimson Mist releases every cloud after emission and individual lifetimes finish");

        Transform PrepareCaptor(string id, Vector2 offset)
        {
            game.progress = new PlayerProgress(); game.Enter("D17Z01S01", null, null);
            foreach (var enemy in game.Current.enemies) enemy.health = 0;
            Place(game.Current.start.position); floor = game.player.transform.position;
            game.progress.hasPrayer = true; game.progress.equippedPrayer = id; game.player.fervour = 60;
            var captive = game.world.FreeCherub;
            Check(captive != null, id + " world fixture uses the actual restored source cherub captor");
            if (captive != null) captive.position = floor + offset;
            return captive;
        }
        PrepareCaptor("PR12", new Vector2(10, 1.5f)); Cast();
        Check(game.world.ActiveCherubs == 0 && game.progress.rescuedCherubIds.Contains("RESCUED_CHERUB_06"),
            "Actual Cante Jondo input rescues the source cherub and records its persistent original ID");
        PrepareCaptor("PR12", new Vector2(16, 18)); Cast();
        Check(game.world.ActiveCherubs == 1,
            "Cante Jondo excludes a captor in the corner outside its circle even inside a twenty-unit square");
        PrepareCaptor("PR14", new Vector2(3, 1.5f)); Cast(); Step(.5f);
        Check(game.world.ActiveCherubs == 0 && game.progress.rescuedCherubIds.Contains("RESCUED_CHERUB_06"),
            "Actual Verdiales input rescues a source cherub intersecting a moving crawler polygon");
        PrepareCaptor("PR14", new Vector2(3, 5)); Cast(); Step(.5f);
        Check(game.world.ActiveCherubs == 1,
            "Verdiales does not rescue a captor above the crawler's physical polygon");
        var worldCaptor = PrepareCaptor("PR14", new Vector2(5, 3.7f));
        PrayerCombat.StrikePolygon(game, new[] { floor + new Vector2(3, -1), floor + new Vector2(6, -1), floor + new Vector2(3, 2) });
        Check(game.world.ActiveCherubs == 1,
            "World polygon hits exclude a captor inside the bounding rectangle but beyond the actual sloping edge");
        worldCaptor = PrepareCaptor("PR05", new Vector2(15.9f, 1.5f));
        PrayerCombat.StrikeBeam(game, floor + Vector2.up * .55f, floor + new Vector2(15, .55f), 2);
        Check(game.world.ActiveCherubs == 1,
            "A cherub railgun cannot rescue a captor beyond the original fifteen-unit beam endpoint");
        if (worldCaptor != null) worldCaptor.position = floor + new Vector2(14.8f, 1.5f);
        PrayerCombat.StrikeBeam(game, floor + Vector2.up * .55f, floor + new Vector2(15, .55f), 2);
        Check(game.world.ActiveCherubs == 0, "A cherub railgun rescues a captor overlapping the actual oriented beam");
        worldCaptor = PrepareCaptor("PR08", new Vector2(.95f, 2.95f));
        PrayerCombat.StrikeCircle(game, floor, .75f);
        Check(game.world.ActiveCherubs == 1,
            "A Zarabanda circle excludes the captor at a square corner outside the source shield radius");
        if (worldCaptor != null) worldCaptor.position = floor + new Vector2(.7f, 2.7f);
        PrayerCombat.StrikeCircle(game, floor, .75f);
        Check(game.world.ActiveCherubs == 0, "A Zarabanda circle rescues a captor physically overlapping its source radius");

        foreach (string id in new[] { "PR03", "PR05", "PR08", "PR11", "PR12", "PR14", "PR15" })
        {
            Prepare(id, new Vector2(4, 0)); Cast();
            int actors = game.legacyPrayers.ActiveActors;
            game.legacyPrayers.Tick(0);
            Check(game.legacyPrayers.ActiveActors == actors, id + " keeps its visual lifetime paused when gameplay delta is zero");
            game.player.StopPrayers();
            Check(game.legacyPrayers.ActiveActors == 0 && game.effects.ActivePrayerCrawlers == 0,
                id + " releases its temporary effects when player prayer state is reset");
            Prepare(id, new Vector2(4, 0)); Cast();
            game.player.health = 0; game.legacyPrayers.Tick(.01f);
            Check(game.legacyPrayers.ActiveActors == 0 && game.effects.ActivePrayerCrawlers == 0,
                id + " releases all temporary prayer effects on player death");
        }
        game.progress = new PlayerProgress(); game.Enter("D17Z01S02", null, null);
        foreach (var enemy in game.Current.enemies) enemy.health = 0;
        Place(game.Current.start.position);
    }
}
#endif
