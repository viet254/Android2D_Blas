using UnityEngine;
namespace Brotherhood
{
    public sealed class RestoredCatalog:ScriptableObject
    {
        public RestoredClip[] clips;
        public RestoredClip Find(string name){foreach(var c in clips)if(c.name==name)return c;return null;}
    }
}
