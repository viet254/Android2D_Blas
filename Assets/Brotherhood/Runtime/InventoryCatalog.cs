using System;
using UnityEngine;
namespace Brotherhood
{
    [Serializable] public sealed class InventoryCatalog
    {
        [Serializable] public sealed class Effect
        {
            public string script,scriptGuid,sourceSettings; public int statType,effectMode,valueType,statValueType,limitTime;
            public bool usePrayerDurationAddition,useHitAsBaseValue; public float value,multiplier=1,effectTime;
            [NonSerialized] EffectSettings settings;
            public EffectSettings Settings=>settings??(settings=string.IsNullOrEmpty(sourceSettings)?new EffectSettings():JsonUtility.FromJson<EffectSettings>(sourceSettings)??new EffectSettings());
        }
        [Serializable] public sealed class EffectCondition { public int type; public float value; }
        [Serializable] public sealed class EffectSettings
        {
            public int effectType,percentToExecute=100,OnlyWhenUsingPrayer,TriggerOnlyOnce,UseWhenCastingPrayer=1;
            public float PingTime=.4f,TimeToWait=3;
            public EffectCondition[] Conditions=Array.Empty<EffectCondition>(),StoppingConditions=Array.Empty<EffectCondition>();
            public int effects,equip,addObject,DamageAmount,MaxUses;
            public float AnimatorSpeed=1,AuraTransformAnimationSpeed=1,AnimatorNormalizedSpeed=1;
            public string flagName;
            public ItemReference NewItem;
        }
        [Serializable] public sealed class ItemReference {public string id;public int type;}
        [Serializable] public sealed class Item { public string id,category,caption,description,pictureGuid,icon,subtitle,lore,parentSkill; public bool hasLore; public float fervourNeeded; public int prayerType,skillCost,skillTier; public Effect[] effects=Array.Empty<Effect>(); }
        [Serializable] sealed class SkillSource { public string id,parentSkill; public int cost,tier; }
        [Serializable] sealed class SkillSourceSet { public SkillSource[] skills; }
        public Item[] items=Array.Empty<Item>();
        static InventoryCatalog cached;
        public static InventoryCatalog Load()
        {
            if(cached!=null)return cached;
            var source=Resources.Load<TextAsset>("Inventory/catalog");
            var catalog=source==null?new InventoryCatalog():JsonUtility.FromJson<InventoryCatalog>(source.text);
            var skillAsset=Resources.Load<TextAsset>("Inventory/skill-source");
            var sourceSkills=skillAsset==null?null:JsonUtility.FromJson<SkillSourceSet>(skillAsset.text);
            if(catalog.items!=null)foreach(var item in catalog.items)
            {
                item.caption=VietnameseSource.Inventory(item.id,"CAPTION",item.caption);
                item.description=VietnameseSource.Inventory(item.id,"DESCRIPTION",item.description);
                item.subtitle=VietnameseSource.Inventory(item.id,"SUBTITLE",item.subtitle);
                item.lore=VietnameseSource.Inventory(item.id,"LORE",item.lore);
                if(item.category=="ability")
                {
                    if(sourceSkills?.skills!=null)
                    {
                        var sourceSkill=Array.Find(sourceSkills.skills,x=>x.id==item.id);
                        if(sourceSkill!=null){item.skillCost=sourceSkill.cost;item.skillTier=sourceSkill.tier;item.parentSkill=sourceSkill.parentSkill;}
                    }
                    string key="UnlockableSkill/"+item.id+"_";
                    item.caption=VietnameseSource.Term(key+"CAPTION",item.caption);
                    string description=VietnameseSource.Term(key+"DESCRIPTION",item.description);
                    string instructions=VietnameseSource.Term(key+"INSTRUCTIONS",item.lore);
                    item.description=string.IsNullOrEmpty(instructions)?description:description+"\n\n"+instructions;
                    item.lore="";
                }
            }
            cached=catalog;return catalog;
        }
        public int Count(string category){int n=0;foreach(var item in items)if(string.Equals(item.category,category,StringComparison.OrdinalIgnoreCase))n++;return n;}
        public string Caption(string id){foreach(var item in items)if(item.id==id)return item.caption;return id;}
        public Item Find(string id){foreach(var item in items)if(item.id==id)return item;return null;}
    }
}
