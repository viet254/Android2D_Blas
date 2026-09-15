using System;
using UnityEngine;
namespace Brotherhood
{
    // StatsTypes numbers are retained from the original EntityStats enum so the
    // imported prefab values remain authoritative across the migration.
    public sealed class InventoryModifiers
    {
        readonly PlayerProgress progress; readonly InventoryCatalog catalog;
        public InventoryModifiers(PlayerProgress progress){this.progress=progress;catalog=InventoryCatalog.Load();}
        public float Bonus(int statType)
        {
            float value=0;
            foreach(var item in catalog.items)
            {
                if(item.category=="prayer"||!progress.IsEquipped(item.id)||item.effects==null)continue;
                foreach(var effect in item.effects)if(effect.statType==statType&&effect.effectMode==0&&effect.valueType==0)value+=effect.value*effect.multiplier;
            }
            return value;
        }
        public float DamageTaken(float raw){return Mathf.Max(0,raw*(1-Mathf.Clamp(Bonus(32),0,.9f)));}
        public float DamageDealt(float raw){return Mathf.Max(0,raw+Bonus(8));}
        public float MaxLife=>Mathf.Clamp(88+Bonus(5),1,280);
        public float MaxFervour=>Mathf.Clamp(60+Bonus(4),1,210);
        public int MaxFlasks=>Mathf.Max(0,2+Mathf.RoundToInt(Bonus(11)));
        public float MoveSpeed=>Mathf.Max(1,4.5f+Bonus(9));
        public float DashCooldownMultiplier=>Mathf.Max(.2f,1+Bonus(3));
        public float PrayerCost
        {
            get{var item=catalog.Find(progress.equippedPrayer);return item==null?30:Mathf.Max(0,item.fervourNeeded+Bonus(28));}
        }
        public float PrayerDuration
        {
            get{var item=catalog.Find(progress.equippedPrayer);float duration=0;if(item!=null&&item.effects!=null)foreach(var effect in item.effects)duration=Mathf.Max(duration,effect.effectTime);return Mathf.Max(.5f,duration+Bonus(26));}
        }
        public float PrayerBonus(string prayerId,int statType)
        {
            var item=catalog.Find(prayerId);float value=0;if(item!=null&&item.effects!=null)foreach(var effect in item.effects)if(effect.statType==statType&&effect.effectMode==0&&effect.valueType==0)value+=effect.value*effect.multiplier;return value;
        }
        public string[] SpecialEffects()
        {
            var result=new System.Collections.Generic.List<string>();
            foreach(var item in catalog.items)if(progress.IsEquipped(item.id)&&item.effects!=null)
                foreach(var effect in item.effects)if(!string.IsNullOrEmpty(effect.script)&&effect.script!="ObjectEffect_Stat"&&!result.Contains(effect.script))result.Add(effect.script);
            return result.ToArray();
        }
    }
}
