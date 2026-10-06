using System;
using UnityEngine;

namespace Brotherhood
{
    // These trails are independent from the dodge trail. Original ItemGhostTrail
    // components can expire before PrayerUse's duration when a heart adds time.
    public sealed class PrayerBuffVisuals : MonoBehaviour
    {
        [Serializable] sealed class GhostSettings { public Color color; }
        [Serializable] sealed class HarvestSettings { public float Duration=2; }
        sealed class Ghost
        {
            public SpriteRenderer renderer;
            public float alpha;
            public Color color;
        }
        const float TrailInterval=.075f;
        const float AlphaPerSecond=.05f*60f;
        readonly Ghost[] ghosts=new Ghost[24];
        BrotherhoodGame game;
        InventoryCatalog catalog;
        SpriteActor harvest;
        Color trailColor;
        float trailTime,trailClock,harvestTime;
        int cursor;
        string room;
        public float TrailTimeRemaining=>trailTime;
        public float HarvestTimeRemaining=>harvestTime;
        public Color TrailColor=>trailColor;
        public int ActiveActors
        {
            get
            {
                int count=harvest!=null&&harvestTime>0?1:0;
                foreach(var ghost in ghosts)if(ghost!=null&&ghost.alpha>0)count++;
                return count;
            }
        }
        public void Initialize(BrotherhoodGame owner){game=owner;catalog=InventoryCatalog.Load();}
        public void Begin(string id)
        {
            trailTime=trailClock=0;room=game.Current?.id;
            var item=catalog.Find(id);
            if(item?.effects==null)return;
            foreach(var effect in item.effects)
            {
                if(effect.script!="ItemGhostTrail"||effect.Settings.effectType!=1)continue;
                trailColor=JsonUtility.FromJson<GhostSettings>(effect.sourceSettings).color;
                trailTime=new InventoryModifiers(game.progress,game.player).PrayerEffectDuration(effect);
                trailClock=0;
                break;
            }
        }
        public void Tick(float dt)
        {
            if(dt<=0||game==null)return;
            if(game.player.Dead||room!=null&&room!=game.Current?.id){Stop();return;}
            foreach(var ghost in ghosts)
            {
                if(ghost==null||ghost.alpha<=0)continue;
                ghost.alpha=Mathf.Max(0,ghost.alpha-AlphaPerSecond*dt);
                if(ghost.alpha<=0)ghost.renderer.gameObject.SetActive(false);
                else{var color=ghost.color;color.a=ghost.alpha;ghost.renderer.color=color;}
            }
            if(trailTime>0)
            {
                float activeStep=Mathf.Min(dt,trailTime);
                trailClock-=activeStep;
                while(trailClock<=0){SpawnGhost();trailClock+=TrailInterval;}
                trailTime=Mathf.Max(0,trailTime-dt);
            }
            if(harvestTime>0)
            {
                harvestTime=Mathf.Max(0,harvestTime-dt);
                if(harvestTime<=0)harvest.gameObject.SetActive(false);
                else
                {
                    harvest.transform.position=game.player.transform.position+Vector3.up*1.2f;
                    harvest.Advance(dt);
                }
            }
        }
        void SpawnGhost()
        {
            var source=game.player.actor.visual;
            if(!source.enabled||source.sprite==null)return;
            int index=cursor++%ghosts.Length;
            var ghost=ghosts[index];
            if(ghost==null)
            {
                var obj=new GameObject("Prayer source ghost trail");obj.transform.SetParent(transform,false);
                ghost=new Ghost{renderer=obj.AddComponent<SpriteRenderer>()};ghosts[index]=ghost;
                var material=Resources.Load<Material>("Effects/Prayers/PenitentGhostTrailMaterial");
                ghost.renderer.sharedMaterial=material!=null?material:source.sharedMaterial;
            }
            var renderer=ghost.renderer;
            renderer.transform.position=source.transform.position;
            renderer.transform.rotation=Quaternion.identity;
            renderer.transform.localScale=source.transform.lossyScale;
            renderer.sprite=source.sprite;renderer.flipX=source.flipX;renderer.flipY=source.flipY;
            renderer.sortingLayerID=source.sortingLayerID;renderer.sortingOrder=source.sortingOrder-1;
            // Penitent.prefab's InitialGhostTrailAlpha is 1. Its frame-based
            // AlphaStep .05 is normalized to seconds for manual simulation.
            ghost.alpha=1;ghost.color=trailColor;var color=trailColor;color.a=1;renderer.color=color;
            renderer.gameObject.SetActive(true);
        }
        public void EnemyKilled()
        {
            if(game==null||game.player.Dead||game.player.ActivePrayer!="PR16"||game.player.PrayerTimeRemaining<=0||harvestTime>0)return;
            var item=catalog.Find("PR16");float duration=2;
            foreach(var effect in item.effects)if(effect.script=="ZambraTearsHarvestEffect")
                duration=JsonUtility.FromJson<HarvestSettings>(effect.sourceSettings).Duration;
            if(harvest==null)
            {
                var obj=new GameObject("PR16 Zambra source Tears harvest");obj.transform.SetParent(transform,false);
                harvest=obj.AddComponent<SpriteActor>();harvest.enabled=false;harvest.catalog=game.player.actor.catalog;
                var visual=new GameObject("Body");visual.transform.SetParent(obj.transform,false);
                harvest.visual=visual.AddComponent<SpriteRenderer>();harvest.visual.sharedMaterial=game.player.actor.visual.sharedMaterial;
                harvest.visual.sortingLayerName="Player";harvest.visual.sortingOrder=10;
            }
            room=game.Current?.id;
            harvest.gameObject.SetActive(true);harvest.transform.position=game.player.transform.position+Vector3.up*1.2f;
            harvestTime=duration;harvest.Play("TearsUp_Effect",true,true);
            game.Sfx("GUARDIAN_TURN");
        }
        public void Stop()
        {
            trailTime=trailClock=harvestTime=0;room=null;
            foreach(var ghost in ghosts)if(ghost!=null){ghost.alpha=0;ghost.renderer.gameObject.SetActive(false);}
            if(harvest!=null)harvest.gameObject.SetActive(false);
        }
    }
}
