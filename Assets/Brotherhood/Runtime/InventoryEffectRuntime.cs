using System;
using System.Collections.Generic;
using UnityEngine;
namespace Brotherhood
{
    public sealed class InventoryEffectRuntime:MonoBehaviour
    {
        BrotherhoodGame game;InventoryCatalog catalog;SpriteActor familiar;Vector3 familiarVelocity;float familiarClock;string familiarRoom;
        public SpriteActor Familiar=>familiar;
        bool flaskRegeneration;
        readonly Dictionary<string,float> clocks=new Dictionary<string,float>();
        readonly Dictionary<string,int[]> temporalFlags=new Dictionary<string,int[]>();string temporalPrayer;
        readonly Dictionary<InventoryCatalog.Effect,float> prayerTimers=new Dictionary<InventoryCatalog.Effect,float>();
        readonly List<InventoryCatalog.Effect> timedPrayerEffects=new List<InventoryCatalog.Effect>();
        [Serializable] class FlagItem{public string item;public int[] effects;}
        [Serializable] class FlagData{public FlagItem[] items;}
        public void Initialize(BrotherhoodGame owner){game=owner;catalog=InventoryCatalog.Load();var source=Resources.Load<TextAsset>("Inventory/temporal-effects-source");if(source!=null)foreach(var item in JsonUtility.FromJson<FlagData>(source.text).items)temporalFlags[item.item]=item.effects;}
        public void BeginPrayer(string id)
        {
            StopPrayer();temporalPrayer=id;var item=catalog.Find(id);
            if(item?.effects==null)return;
            var mods=new InventoryModifiers(game.progress,game.player);
            foreach(var effect in item.effects)if(effect.Settings.effectType==1&&effect.limitTime!=0)
            {timedPrayerEffects.Add(effect);prayerTimers[effect]=mods.PrayerEffectDuration(effect);}
        }
        public void StopPrayer(){temporalPrayer=null;prayerTimers.Clear();timedPrayerEffects.Clear();}
        public bool TracksPrayer(string id){return temporalPrayer==id;}
        public bool PrayerEffectActive(InventoryCatalog.Effect effect){return prayerTimers.TryGetValue(effect,out float time)&&time>0;}
        public float PrayerEffectTimeRemaining(InventoryCatalog.Effect effect){return prayerTimers.TryGetValue(effect,out float time)?time:0;}
        public void ResetTransient(){StopPrayer();flaskRegeneration=false;clocks.Clear();}
        public void BeginFlaskRecovery(){flaskRegeneration=game.progress.IsEquipped("RB102");}
        public bool HasTemporalFlag(int flag)
        {
            foreach(var item in catalog.items)
            {
                bool prayer=item.category=="prayer";
                if(prayer)
                {
                    bool active=false;
                    if(temporalPrayer==item.id)
                    {foreach(var effect in item.effects)if(effect.script=="ItemTemporalEffect"&&PrayerEffectActive(effect)){active=true;break;}}
                    else if(game.player.ActivePrayer==item.id&&game.player.PrayerTimeRemaining>0)
                    {
                        var mods=new InventoryModifiers(game.progress,game.player);
                        float elapsed=Mathf.Max(0,mods.PrayerDurationFor(item.id)-game.player.PrayerTimeRemaining);
                        foreach(var effect in item.effects)if(effect.script=="ItemTemporalEffect"&&elapsed<mods.PrayerEffectDuration(effect)){active=true;break;}
                    }
                    if(!active)continue;
                }
                else if(!game.progress.IsEquipped(item.id))continue;
                if(temporalFlags.TryGetValue(item.id,out var flags)&&Array.IndexOf(flags,flag)>=0)return true;
            }
            return false;
        }
        public float AnimationSpeed(string script)
        {
            float speed=1;
            foreach(var item in catalog.items)if(game.progress.IsEquipped(item.id))foreach(var effect in item.effects)
                if(effect.script==script)speed*=script=="QuickAreaTransformBeadEffect"?effect.Settings.AuraTransformAnimationSpeed:script=="HardLandingBeadEffect"?effect.Settings.AnimatorNormalizedSpeed:effect.Settings.AnimatorSpeed;
            return Mathf.Max(.1f,speed);
        }
        public void Tick(float dt)
        {
            if(game.player.Dead||dt<=0)return;
            foreach(var effect in timedPrayerEffects)prayerTimers[effect]=Mathf.Max(0,prayerTimers[effect]-dt);
            if(flaskRegeneration)
            {
                if(!game.progress.IsEquipped("RB102")||game.player.health>=game.player.MaxHealth)flaskRegeneration=false;
                // PenitenceManager's ordinary-Life branch heals regenFactor *
                // FlaskUpgradeLevel per second. This route has the base flask,
                // level one; the separate PE02 stock-of-health mode is absent.
                else game.player.health=Mathf.Min(game.player.MaxHealth,game.player.health+dt);
            }
            TickFamiliar(dt);
            var mods=new InventoryModifiers(game.progress,game.player);
            foreach(var item in catalog.items)if(game.progress.IsEquipped(item.id)&&item.category!="prayer")
                for(int i=0;i<item.effects.Length;i++)
                {
                    var effect=item.effects[i];if(effect.script!="ObjectEffect_Stat"||effect.Settings.effectType!=4)continue;
                    string key=item.id+"/"+i;clocks.TryGetValue(key,out float time);time+=dt;
                    if(time>=Mathf.Max(.05f,effect.Settings.PingTime)){time=0;mods.ApplyEvent(4);}
                    clocks[key]=time;
                }
        }
        void TickFamiliar(float dt)
        {
            if(!game.progress.IsEquipped("RB42")){if(familiar!=null)Destroy(familiar.gameObject);familiar=null;return;}
            if(familiar==null)
            {
                var obj=new GameObject("RB42 source familiar");obj.transform.SetParent(transform);familiar=obj.AddComponent<SpriteActor>();familiar.enabled=false;familiar.catalog=game.player.actor.catalog;
                var visual=new GameObject("Body");visual.transform.SetParent(obj.transform,false);familiar.visual=visual.AddComponent<SpriteRenderer>();familiar.visual.sharedMaterial=game.player.actor.visual.sharedMaterial;familiar.visual.sortingLayerName="Player";familiar.visual.sortingOrder=2;
                familiar.Play("familiar_idle",true,true);familiarClock=0;familiarRoom=null;
            }
            familiarClock+=dt;familiar.Advance(dt);var cherub=familiarClock>=3&&game.world!=null?game.world.FreeCherub:null;
            Vector3 target=cherub!=null?cherub.position+new Vector3(.1f,-4,0):game.player.transform.position+new Vector3(-game.player.facing*1.2f,1.5f,0);
            if(familiarRoom!=game.Current.id){familiar.transform.position=target;familiarRoom=game.Current.id;familiarVelocity=Vector3.zero;familiarClock=0;}
            else familiar.transform.position=Vector3.SmoothDamp(familiar.transform.position,target,ref familiarVelocity,cherub==null?.1f:Vector3.Distance(familiar.transform.position,target)<5?.2f:1,20,dt);
            familiar.visual.transform.localPosition=new Vector3(0,Mathf.Cos(familiarClock*2)*.2f*Mathf.Clamp01(familiarClock),0);if(Mathf.Abs(familiarVelocity.x)>.01f)familiar.Face(Mathf.Sign(familiarVelocity.x));
        }
        public void Dispatch(int eventType,bool execution=false,bool heavy=false)
        {
            var mods=new InventoryModifiers(game.progress,game.player);bool changed=false;
            if(eventType==5||eventType==10)flaskRegeneration=false;
            // Snapshot equipment before a death effect replaces the bead so the
            // newly equipped next stage is not processed by the same event.
            var equipped=new List<InventoryCatalog.Item>();foreach(var item in catalog.items)if(game.progress.IsEquipped(item.id))equipped.Add(item);
            foreach(var item in equipped)foreach(var effect in item.effects)
            {
                if(effect.Settings.effectType!=eventType||!mods.ConditionsMet(effect,execution,heavy))continue;
                if(effect.script=="ObjectEffect_ChangeItem"&&!string.IsNullOrEmpty(effect.Settings.NewItem?.id))
                {
                    string next=effect.Settings.NewItem.id;game.progress.SetOwned(item.id,false);game.progress.SetOwned(next,true);
                    var beads=game.progress.equippedRosaryBeads;
                    for(int i=0;i<beads.Length;i++)if(beads[i]==item.id)beads[i]=effect.Settings.equip!=0?next:"";
                    game.progress.equippedRosaryBeads=Array.FindAll(beads,value=>!string.IsNullOrEmpty(value));changed=true;
                }
            }
            if(changed||eventType==10)game.SaveGame();
        }
    }
}
