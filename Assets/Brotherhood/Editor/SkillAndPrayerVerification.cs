using System;
using System.IO;
using System.Linq;
using Brotherhood;
using UnityEditor;
using UnityEngine;

public static class SkillAndPrayerVerification
{
    [MenuItem("Brotherhood/Verify Skills, Prayers, Dash and UI")]
    public static void RunVerification()
    {
        var log = new System.Collections.Generic.List<string>();
        void AssertCheck(bool cond, string msg)
        {
            if (cond) log.Add("[PASS] " + msg);
            else log.Add("[FAIL] " + msg);
            Debug.Log((cond ? "[PASS] " : "[FAIL] ") + msg);
        }

        // 1. Catalog & Skills check
        var cat = InventoryCatalog.Load();
        AssertCheck(cat != null && cat.items.Length > 200, "Catalog loaded with " + (cat?.items?.Length ?? 0) + " items");
        int skillCount = cat.Count("ability");
        AssertCheck(skillCount == 15, "Catalog contains 15 authentic Mea Culpa skills (Found: " + skillCount + ")");

        var charged1 = cat.Find("CHARGED_1");
        AssertCheck(charged1 != null && charged1.caption == "SINFUL WRATH", "CHARGED_1 is SINFUL WRATH");
        var lunge3 = cat.Find("LUNGE_3");
        AssertCheck(lunge3 != null && lunge3.caption == "SACRED ONSLAUGHT", "LUNGE_3 is SACRED ONSLAUGHT");
        var ranged1 = cat.Find("RANGED_1");
        AssertCheck(ranged1 != null && ranged1.caption == "FERVOROUS BLOOD", "RANGED_1 is FERVOROUS BLOOD");
        var vert1 = cat.Find("VERTICAL_1");
        AssertCheck(vert1 != null && vert1.caption == "WEIGHT OF SIN", "VERTICAL_1 is WEIGHT OF SIN");

        // 2. Inventory UI Tab check
        AssertCheck(BlasInventoryUI.Categories.Length == 7, "Inventory has 7 tabs");
        AssertCheck(BlasInventoryUI.Categories[5] == "ability", "Tab 5 is 'ability'");
        AssertCheck(BlasInventoryUI.CategoryTitles[5] == "ABILITIES", "Tab 5 title is 'ABILITIES'");

        // 3. Textures and icons check
        var abilityIcon = Resources.Load<Sprite>("UI/Sprites/Item_Abilities");
        AssertCheck(abilityIcon != null, "Item_Abilities sprite exists in Resources");
        var skillIcon = Resources.Load<Texture2D>("Inventory/Skills/skill_generic_buyed");
        AssertCheck(skillIcon != null, "Authentic skill icon exists in Resources");
        var chargedPreview = Resources.Load<Texture2D>("Inventory/Skills/skill_preview_charged");
        AssertCheck(chargedPreview != null, "Authentic skill preview exists in Resources");

        // 4. Player progress check
        var prog = new PlayerProgress();
        prog.chargedTier = 2;
        AssertCheck(prog.IsEquipped("CHARGED_1") && prog.IsEquipped("CHARGED_2") && !prog.IsEquipped("CHARGED_3"), "PlayerProgress tracks ability tier progression correctly");

        // 5. Check PlayerController source for dash non-flickering and dust
        string pcSource = File.ReadAllText("Assets/Brotherhood/Runtime/PlayerController.cs");
        AssertCheck(!pcSource.Contains("invincible=SourceGameplayTuning.DashVulnerableEventTime;"), "Dash does not set invincible flicker state");
        AssertCheck(pcSource.Contains("penitent_pushback_grounded_dust_effect_anim"), "Dash plays authentic grounded pushback dust at start");
        AssertCheck(pcSource.Contains("Penitent_stop_running_dust"), "Dash plays stop running dust at end");
        AssertCheck(pcSource.Contains("PrayerCrawler"), "PlayerController invokes authentic PrayerCrawler for PR14");

        // Write results to file
        string outPath = "Documentation/skill_and_prayer_verification_results.txt";
        File.WriteAllText(outPath, string.Join("\n", log));
        Debug.Log("Verification completed. Results written to: " + outPath);
    }
}
