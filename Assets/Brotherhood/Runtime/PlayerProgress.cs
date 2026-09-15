using System;
using System.Collections.Generic;
namespace Brotherhood
{
    [Serializable] public sealed class PlayerProgress
    {
        [Serializable] public struct MapPinSaveData { public int pinType; public float x, y; public string roomId; }
        public bool canJump=true,canDash=true,canParry=true,hasMeaCulpa=true;
        public bool hasFlask=true,hasMap=true,hasPrayer,hasSpecial=true,bloodOwned,bloodEquipped;
        public int chargedTier,lungeTier,rangedTier=1,specialMode=1;
        public int cherubsFreed=0;
        public float mapPercentage=1.0f;
        public List<MapPinSaveData> mapPins=new List<MapPinSaveData>();
        public float tears=9999;
        public string[] ownedItems=new string[]{"QI31","RE01"};
        public string[] equippedRelics=new string[]{"RE01"};
        public string[] equippedRosaryBeads=new string[]{"RB01"};
        public string equippedPrayer="PR01",equippedSwordHeart="HE01";
        public bool unlockAllItems=true;
        public void UnlockCore(){canJump=canDash=canParry=hasMeaCulpa=hasFlask=hasMap=hasSpecial=true;}
        public bool Owns(string id){return unlockAllItems || (!string.IsNullOrEmpty(id)&&ownedItems!=null&&Array.IndexOf(ownedItems,id)>=0);}
        public bool IsEquipped(string id)
        {
            if(string.IsNullOrEmpty(id)) return false;
            if(id.StartsWith("CHARGED_")) return chargedTier >= int.Parse(id.Substring(8));
            if(id.StartsWith("LUNGE_")) return lungeTier >= int.Parse(id.Substring(6));
            if(id.StartsWith("RANGED_")) return rangedTier >= int.Parse(id.Substring(7));
            if(id.StartsWith("VERTICAL_") || id.StartsWith("COMBO_")) return true;
            return id==equippedPrayer||id==equippedSwordHeart||(equippedRelics!=null&&Array.IndexOf(equippedRelics,id)>=0)||(equippedRosaryBeads!=null&&Array.IndexOf(equippedRosaryBeads,id)>=0);
        }
        public void SetOwned(string id,bool owned)
        {
            if(string.IsNullOrEmpty(id)||Owns(id)==owned)return;
            if(ownedItems==null)ownedItems=Array.Empty<string>();
            if(owned){var next=new string[ownedItems.Length+1];Array.Copy(ownedItems,next,ownedItems.Length);next[next.Length-1]=id;ownedItems=next;}
            else{var next=new string[Math.Max(0,ownedItems.Length-1)];int n=0;foreach(var item in ownedItems)if(item!=id&&n<next.Length)next[n++]=item;ownedItems=next;}
        }
    }
}
