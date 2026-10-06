using System;
using UnityEngine;
namespace Brotherhood
{
    // StatsTypes numbers are retained from the original EntityStats enum so the
    // imported prefab values remain authoritative across the migration.
    public sealed class InventoryModifiers
    {
        readonly PlayerProgress progress; readonly InventoryCatalog catalog; readonly PlayerController owner;
        public PlayerProgress Progress=>progress;
        public InventoryModifiers(PlayerProgress progress,PlayerController owner=null){this.progress=progress;this.owner=owner;catalog=InventoryCatalog.Load();}
        public bool ConditionsMet(InventoryCatalog.Effect effect,bool execution=false,bool heavy=false)
        {
            foreach(var condition in effect.Settings.Conditions??Array.Empty<InventoryCatalog.EffectCondition>())
            {
                switch(condition.type)
                {
                    case 0: if(owner==null||owner.health>=MaxLife*condition.value/100f)return false;break;
                    case 1: if(!execution)return false;break;
                    case 2: if(!heavy)return false;break;
                    // Mobile ObjectEffect adds the empty-flask condition. The
                    // source RB104/RB201 values use thresholds two and zero.
                    case 4: if(owner==null||owner.flasks>condition.value)return false;break;
                    default:return false;
                }
            }
            return true;
        }
        public float Value(int statType,float baseValue)
        {
            float addition=0,multiplier=1;
            foreach(var item in catalog.items)
            {
                bool prayer=item.category=="prayer";
                if(prayer?owner==null||owner.ActivePrayer!=item.id||owner.PrayerTimeRemaining<=0:!progress.IsEquipped(item.id))continue;
                if(item.effects==null)continue;
                foreach(var effect in item.effects)
                    if((effect.script=="ObjectEffect_Stat"||effect.script=="HeavyAttackPrayerEffect")&&effect.statType==statType&&effect.effectMode==0&&effect.Settings.effectType==(prayer?1:0)&&(!prayer||PrayerEffectActive(item.id,effect))&&ConditionsMet(effect))
                    {
                        float amount=effect.value;
                        if(effect.valueType!=0)amount+=ReferencedStat(effect.statValueType,effect.valueType==1);
                        addition+=amount;multiplier*=effect.multiplier;
                    }
            }
            // Attribute.ApplyRawBonuses adds all bases before multiplying. A
            // zero-value effect can therefore still change a stat (HE201, HE11).
            return (baseValue+addition)*multiplier;
        }
        float ReferencedStat(int type,bool current)
        {
            if(type==5)return current&&owner!=null?owner.health:MaxLife;
            if(type==4)return current&&owner!=null?owner.fervour:MaxFervour;
            if(type==11)return current&&owner!=null?owner.flasks:MaxFlasks;
            return 0;
        }
        public float Bonus(int statType){return Value(statType,0);}
        public float DamageTaken(float raw,int reductionStat=32){return Mathf.Max(0,raw*(1-Mathf.Clamp(Bonus(reductionStat),-2,1))-Value(2,0));}
        public float DamageDealt(float raw){return Mathf.Max(0,Value(8,raw+progress.strengthUpgrades*4)*Value(15,1));}
        public float RangedDamage(float raw){return DamageDealt(raw)*Mathf.Max(0,Value(25,1));}
        public float PrayerDamage(float raw){return Mathf.Max(0,raw*Value(27,1));}
        public float FervourGain(float raw){return Mathf.Max(0,Value(10,raw))*GuiltRules.Load().Gain(progress);}
        public float TearsMultiplier=>Mathf.Max(0,Value(19,1));
        public float MaxLife=>Mathf.Clamp(Value(5,88),1,280);
        public float MaxFervour=>Mathf.Clamp(Value(4,60+progress.fervourUpgrades*20),1,210)*GuiltRules.Load().Maximum(progress);
        public int MaxFlasks=>Mathf.Max(0,Mathf.RoundToInt(Value(11,2)));
        public float FlaskHealing=>Mathf.Max(0,Value(16,40));
        public float MoveSpeed=>Mathf.Max(1,Value(9,progress.IsEquipped("RB203")?6.25f:SourceGameplayTuning.WalkSpeed));
        public int RosarySlots=>Mathf.Clamp(progress.rosarySlots+Mathf.RoundToInt(Bonus(12)),2,8);
        public int MeaCulpaLevel=>Mathf.Clamp(progress.meaCulpaLevel+Mathf.RoundToInt(Bonus(18)),0,7);
        public float ParryWindow=>Mathf.Max(.01f,Value(20,SourceGameplayTuning.ParryWindow));
        public float DashCooldownMultiplier=>Mathf.Max(.2f,Value(3,1));
        public float RangedCost=>SourceGameplayTuning.RangedFervourCost*(progress.IsEquipped("HE01")?SourceGameplayTuning.RangedHeartCostMultiplier:1f);
        public int AirImpulses=>Mathf.Max(0,Mathf.RoundToInt(Value(31,2)));
        public void ApplyEvent(int eventType,float hitDamage=0,bool execution=false,bool heavy=false)
        {
            if(owner==null)return;
            foreach(var item in catalog.items)
            {
                bool prayer=item.category=="prayer";
                if(prayer?item.id!=owner.ActivePrayer||owner.PrayerTimeRemaining<=0:!progress.IsEquipped(item.id))continue;
                foreach(var effect in item.effects??Array.Empty<InventoryCatalog.Effect>())
                {
                    var settings=effect.Settings;
                    if(effect.script!="ObjectEffect_Stat"||settings.effectType!=eventType||effect.effectMode!=1||!ConditionsMet(effect,execution,heavy))continue;
                    if(settings.OnlyWhenUsingPrayer!=0&&owner.PrayerTimeRemaining<=0)continue;
                    if(settings.percentToExecute<100&&UnityEngine.Random.Range(0,100)>=settings.percentToExecute)continue;
                    float amount=effect.value+(effect.useHitAsBaseValue?hitDamage:0);
                    if(effect.valueType!=0)amount+=ReferencedStat(effect.statValueType,effect.valueType==1);
                    amount*=effect.multiplier;
                    switch(effect.statType)
                    {
                        case 5:owner.health=Mathf.Clamp(owner.health+amount,0,owner.MaxHealth);break;
                        case 4:owner.fervour=Mathf.Clamp(owner.fervour+amount,0,owner.MaxFervour);break;
                        case 11:owner.flasks=Mathf.Clamp(owner.flasks+Mathf.RoundToInt(amount),0,owner.MaxFlasks);break;
                        case 17:progress.tears=Mathf.Max(0,progress.tears+amount);break;
                    }
                }
            }
        }
        public float PrayerCost
        {
            get{var item=catalog.Find(progress.equippedPrayer);return item==null?30:Mathf.Max(0,item.fervourNeeded+Bonus(28));}
        }
        public float PrayerDuration
        {
            get{return PrayerDurationFor(progress.equippedPrayer);}
        }
        public float PrayerDurationFor(string id)
        {
            var item=catalog.Find(id);float duration=0;
            if(item?.effects!=null)foreach(var effect in item.effects)
                if(effect.Settings.effectType==1&&effect.limitTime!=0)duration=Mathf.Max(duration,effect.effectTime);
            // PrayerUse reserves the longest OnUse effect plus the duration
            // stat; individual ObjectEffects opt into that addition separately.
            return duration>0?Mathf.Max(0,duration+Bonus(26)):0;
        }
        public float PrayerEffectDuration(InventoryCatalog.Effect effect)
        {
            return Mathf.Max(0,effect.effectTime+(effect.usePrayerDurationAddition?Bonus(26):0));
        }
        bool PrayerEffectActive(string id,InventoryCatalog.Effect effect)
        {
            if(effect.limitTime==0)return true;
            var runtime=owner?.game?.itemEffects;
            if(runtime!=null&&runtime.TracksPrayer(id))return runtime.PrayerEffectActive(effect);
            // Also supports restored state and the explicit state used by
            // verification; normal casts use the captured component timers.
            float elapsed=Mathf.Max(0,PrayerDurationFor(id)-owner.PrayerTimeRemaining);
            return elapsed<PrayerEffectDuration(effect);
        }
        public float PrayerBonus(string prayerId,int statType)
        {
            var item=catalog.Find(prayerId);float value=0;if(item!=null&&item.effects!=null)foreach(var effect in item.effects)if(effect.statType==statType&&effect.effectMode==0&&effect.valueType==0)value+=effect.value*effect.multiplier;return value;
        }
        public float PrayerHitHealing(string prayerId,float damage)
        {
            var item=catalog.Find(prayerId);float result=0;
            if(item?.effects!=null)foreach(var effect in item.effects)if(effect.statType==5&&effect.useHitAsBaseValue&&effect.effectMode==1)result+=damage*effect.multiplier;
            return result;
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
