using System;
using UnityEngine;
namespace Brotherhood
{
    [Serializable] public sealed class InventoryCatalog
    {
        [Serializable] public sealed class Effect { public string script; public int statType,effectMode,valueType,statValueType,limitTime; public float value,multiplier=1,effectTime; }
        [Serializable] public sealed class Item { public string id,category,caption,description,pictureGuid,icon,subtitle,lore; public bool hasLore; public float fervourNeeded; public int prayerType; public Effect[] effects=Array.Empty<Effect>(); }
        public Item[] items=Array.Empty<Item>();
        public static InventoryCatalog Load(){var source=Resources.Load<TextAsset>("Inventory/catalog");return source==null?new InventoryCatalog():JsonUtility.FromJson<InventoryCatalog>(source.text);}
        public int Count(string category){int n=0;foreach(var item in items)if(string.Equals(item.category,category,StringComparison.OrdinalIgnoreCase))n++;return n;}
        public string Caption(string id){foreach(var item in items)if(item.id==id)return item.caption;return id;}
        public Item Find(string id){foreach(var item in items)if(item.id==id)return item;return null;}
    }
}
