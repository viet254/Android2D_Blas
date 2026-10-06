using UnityEngine;
namespace Brotherhood
{
    public sealed class RestoredCatalog:ScriptableObject
    {
        public RestoredClip[] clips;
        static RestoredCatalog supplement,roomSupplement,prayerSupplement;
        public RestoredClip Find(string name)
        {
            foreach(var c in clips)if(c.name==name)return c;
            if(prayerSupplement==null)prayerSupplement=Resources.Load<RestoredCatalog>("Effects/PrayerClips");
            if(prayerSupplement!=null&&prayerSupplement!=this)foreach(var c in prayerSupplement.clips)if(c.name==name)return c;
            if(supplement==null)supplement=Resources.Load<RestoredCatalog>("Effects/PriorityTwoClips");
            if(supplement!=null&&supplement!=this)foreach(var c in supplement.clips)if(c.name==name)return c;
            if(roomSupplement==null)roomSupplement=Resources.Load<RestoredCatalog>("Rooms/PriorityTwoClips");
            if(roomSupplement!=null&&roomSupplement!=this)foreach(var c in roomSupplement.clips)if(c.name==name)return c;
            return null;
        }
    }
}
