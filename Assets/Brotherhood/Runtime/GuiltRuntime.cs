using System;
using System.Collections.Generic;
using UnityEngine;
namespace Brotherhood
{
    [Serializable] public sealed class GuiltRules
    {
        [Serializable] public sealed class Key {public float time,value,inSlope,outSlope;}
        [Serializable] public sealed class Level {public string room;public bool enabled;}
        public int maxDrops=7;public float linkDistance=20;public Key[] fervourGain,fervourMax,tearsGain;public Level[] levels;
        static GuiltRules source;AnimationCurve gain,maximum,tears;
        public static GuiltRules Load()
        {
            if(source!=null)return source;var asset=Resources.Load<TextAsset>("Inventory/guilt-source");source=asset!=null?JsonUtility.FromJson<GuiltRules>(asset.text):new GuiltRules();
            source.gain=Curve(source.fervourGain);source.maximum=Curve(source.fervourMax);source.tears=Curve(source.tearsGain);return source;
        }
        static AnimationCurve Curve(Key[] keys)
        {
            if(keys==null||keys.Length==0)return AnimationCurve.Constant(0,7,1);var frames=new Keyframe[keys.Length];
            for(int i=0;i<keys.Length;i++)frames[i]=new Keyframe(keys[i].time,keys[i].value,keys[i].inSlope,keys[i].outSlope);
            return new AnimationCurve(frames){preWrapMode=WrapMode.ClampForever,postWrapMode=WrapMode.ClampForever};
        }
        public float Gain(PlayerProgress progress)=>gain.Evaluate(progress.guiltDrops?.Count??0);
        public float Maximum(PlayerProgress progress)=>maximum.Evaluate(progress.guiltDrops?.Count??0);
        public float Tears(PlayerProgress progress)=>tears.Evaluate(progress.guiltDrops?.Count??0);
        public bool Enabled(string room)
        {
            if(levels!=null)foreach(var level in levels)if(level.room==room)return level.enabled;
            return true; // LevelInitializer defaults to true when no override is exported.
        }
    }
    public sealed class GuiltRuntime:MonoBehaviour
    {
        BrotherhoodGame game;Vector2 safePosition;string safeRoom;readonly List<GameObject> visuals=new List<GameObject>();
        public void Initialize(BrotherhoodGame owner){game=owner;}
        public void RememberFloor()
        {
            if(game.Current==null||game.player.Dead||!game.player.motor.grounded)return;
            safeRoom=game.Current.id;safePosition=game.player.transform.position;
        }
        public void OnPlayerDeath()
        {
            var rules=GuiltRules.Load();var progress=game.progress;
            if(game.itemEffects!=null&&game.itemEffects.HasTemporalFlag(4))return;
            if(game.Current==null||!rules.Enabled(game.Current.id)||progress.guiltDrops.Count>=rules.maxDrops)return;
            var position=string.IsNullOrEmpty(safeRoom)?(Vector2)game.Current.start.position:safePosition;
            string room=string.IsNullOrEmpty(safeRoom)?game.Current.id:safeRoom;
            var drop=new PlayerProgress.GuiltDrop{id=Guid.NewGuid().ToString("N"),room=room,x=position.x,y=position.y};drop.group=drop.id;
            foreach(var previous in progress.guiltDrops)if(previous.room==room&&previous.group==previous.id&&Vector2.Distance(new Vector2(previous.x,previous.y),position)<=rules.linkDistance){drop.group=previous.id;break;}
            progress.guiltDrops.Add(drop);game.SaveGame();
        }
        public PlayerProgress.GuiltDrop NearDrop()
        {
            if(game.Current==null||game.player.Dead||!game.player.motor.grounded)return null;
            foreach(var drop in game.progress.guiltDrops)if(drop.room==game.Current.id&&drop.group==drop.id&&Vector2.Distance(new Vector2(drop.x,drop.y),game.player.transform.position)<1.7f)return drop;return null;
        }
        public bool TryRecover()
        {
            var drop=NearDrop();if(drop==null)return false;
            game.progress.guiltDrops.RemoveAll(value=>value.group==drop.id||value.id==drop.id);
            game.player.health=Mathf.Min(game.player.MaxHealth,game.player.health+game.player.MaxHealth*.33f);
            game.player.fervour=Mathf.Min(game.player.MaxFervour,game.player.fervour+game.player.MaxFervour*.5f);
            game.effects.Animation("guiltSystem_pickUpGuiltFx",game.player.transform.position,1);game.player.BeginCollect(false);
            RefreshRoom();game.SaveGame();game.Message("GUILT RECOVERED");game.Sfx("GUILT_RECOVER");return true;
        }
        public void RefreshRoom()
        {
            foreach(var visual in visuals)if(visual!=null)Destroy(visual);visuals.Clear();
            foreach(var drop in game.progress.guiltDrops)
            {
                if(game.Current==null||drop.room!=game.Current.id||drop.group!=drop.id)continue;
                var visual=new GameObject("Guilt fragment "+drop.id);visual.transform.SetParent(transform);visual.transform.position=new Vector3(drop.x,drop.y,0);
                var actor=visual.AddComponent<SpriteActor>();actor.catalog=game.player.actor.catalog;actor.visual=visual.AddComponent<SpriteRenderer>();
                actor.visual.sharedMaterial=game.player.actor.visual.sharedMaterial;actor.visual.sortingLayerName="Player";actor.visual.sortingOrder=0;actor.Play("GuiltSystem_idle",true,true);visuals.Add(visual);
            }
        }
    }
}
