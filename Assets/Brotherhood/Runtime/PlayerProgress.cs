using System;
using System.Collections.Generic;
namespace Brotherhood
{
    [Serializable] public sealed class PlayerProgress
    {
        [Serializable] public struct MapPinSaveData { public int pinType; public float x, y; public string roomId,cellKey; }
        [Serializable] public sealed class GuiltDrop {public string id,room,group;public float x,y;}
        public bool canJump=true,canDash=true,canParry=true,hasMeaCulpa=true;
        public bool hasFlask=true,hasMap=true,hasPrayer,hasSpecial,bloodOwned,bloodEquipped;
        public bool wardenDefeated,achievementAC01,deograciasMet,thornGranted,shockGateOpened;
        public bool campaignWon;
        public string[] completedRooms=Array.Empty<string>();
        public int chargedTier,lungeTier,rangedTier,comboTier,verticalTier,specialMode;
        public int meaCulpaLevel,rosarySlots=2;
        public string[] activatedSkillAltars=Array.Empty<string>();
        public int cherubsFreed=0;
        public string[] rescuedCherubIds=Array.Empty<string>(),sourceFlags=Array.Empty<string>();
        public int strengthUpgrades;
        public int fervourUpgrades;
        public int penanceUses;
        public float mapPercentage;
        public string[] discoveredMapCells=Array.Empty<string>();
        public List<GuiltDrop> guiltDrops=new List<GuiltDrop>();
        public List<MapPinSaveData> mapPins=new List<MapPinSaveData>();
        public float tears;
        public string[] ownedItems=Array.Empty<string>();
        public string[] equippedRelics=Array.Empty<string>();
        public string[] equippedRosaryBeads=Array.Empty<string>();
        public string equippedPrayer="",equippedSwordHeart="";
        public bool unlockAllItems;
        public int ComboLevel=>OwnedSkillTier("COMBO_",comboTier);
        public int VerticalLevel=>OwnedSkillTier("VERTICAL_",verticalTier);
        int OwnedSkillTier(string prefix,int stored){for(int tier=3;tier>stored;tier--)if(Owns(prefix+tier))return tier;return stored;}
        public void UnlockCore(){canJump=canDash=canParry=hasMeaCulpa=hasFlask=hasMap=true;}
        public bool ActivateSkillAltar(string id)
        {
            if(string.IsNullOrEmpty(id)||Array.IndexOf(activatedSkillAltars??Array.Empty<string>(),id)>=0)return false;
            var old=activatedSkillAltars??Array.Empty<string>();var next=new string[old.Length+1];Array.Copy(old,next,old.Length);next[old.Length]=id;activatedSkillAltars=next;
            meaCulpaLevel=Math.Min(7,meaCulpaLevel+1);strengthUpgrades=Math.Min(7,strengthUpgrades+1);return true;
        }
        public void Normalize(int saveVersion,InventoryCatalog catalog)
        {
            ownedItems=ownedItems??Array.Empty<string>();activatedSkillAltars=activatedSkillAltars??Array.Empty<string>();
            rescuedCherubIds=rescuedCherubIds??Array.Empty<string>();sourceFlags=sourceFlags??Array.Empty<string>();
            strengthUpgrades=Math.Min(7,Math.Max(strengthUpgrades,activatedSkillAltars.Length));
            if(HasFlag("D17Z01S04/BLUE_ALTAR_ACTIVATED")&&fervourUpgrades<1)fervourUpgrades=1;
            fervourUpgrades=Math.Max(0,Math.Min(10,fervourUpgrades));
            equippedRosaryBeads=equippedRosaryBeads??Array.Empty<string>();
            discoveredMapCells=discoveredMapCells??Array.Empty<string>();guiltDrops=guiltDrops??new List<GuiltDrop>();mapPins=mapPins??new List<MapPinSaveData>();
            if(saveVersion<6)
            {
                // v5 granted RANGED_1 automatically. Only an actual purchase
                // survives migration; higher legacy tiers remain earned.
                if(rangedTier==1&&Array.IndexOf(ownedItems,"RANGED_1")<0)rangedTier=0;
                foreach(var prefix in new[]{"CHARGED_","LUNGE_","RANGED_"})
                {
                    int tier=prefix=="CHARGED_"?chargedTier:prefix=="LUNGE_"?lungeTier:rangedTier;
                    for(int i=1;i<=tier;i++)SetOwned(prefix+i,true);
                }
                if(catalog?.items!=null)foreach(var item in catalog.items)if(item.category=="ability"&&Array.IndexOf(ownedItems,item.id)>=0)meaCulpaLevel=Math.Max(meaCulpaLevel,item.skillTier);
                // Preserve beads already equipped in a legacy save, without
                // granting eight slots to a new pilgrimage.
                rosarySlots=Math.Max(2,equippedRosaryBeads.Length);
            }
            meaCulpaLevel=Math.Max(0,Math.Min(7,meaCulpaLevel));rosarySlots=Math.Max(2,Math.Min(8,rosarySlots));
            // Added skills derive from ownership so existing v6 saves and the
            // test menu retain every skill they have actually unlocked.
            for(int tier=1;tier<=3;tier++)
            {
                if(Array.IndexOf(ownedItems,"COMBO_"+tier)>=0)comboTier=Math.Max(comboTier,tier);
                if(Array.IndexOf(ownedItems,"VERTICAL_"+tier)>=0)verticalTier=Math.Max(verticalTier,tier);
            }
            comboTier=Math.Max(0,Math.Min(3,comboTier));verticalTier=Math.Max(0,Math.Min(3,verticalTier));
            hasSpecial=rangedTier>0||lungeTier>0;specialMode=rangedTier>0?1:0;
            hasPrayer=!string.IsNullOrEmpty(equippedPrayer);
        }
        public bool Owns(string id){return unlockAllItems || (!string.IsNullOrEmpty(id)&&ownedItems!=null&&Array.IndexOf(ownedItems,id)>=0);}
        public bool HasFlag(string flag)=>Array.IndexOf(sourceFlags??Array.Empty<string>(),flag)>=0;
        public bool SetFlag(string flag){if(string.IsNullOrEmpty(flag)||HasFlag(flag))return false;var flags=new List<string>(sourceFlags??Array.Empty<string>()){flag};sourceFlags=flags.ToArray();return true;}
        public bool RescueCherub(string id){if(Array.IndexOf(rescuedCherubIds??Array.Empty<string>(),id)>=0)return false;var rescued=new List<string>(rescuedCherubIds??Array.Empty<string>()){id};rescuedCherubIds=rescued.ToArray();cherubsFreed=Math.Min(38,cherubsFreed+1);return true;}
        public bool RoomCompleted(string id){return !string.IsNullOrEmpty(id)&&completedRooms!=null&&Array.IndexOf(completedRooms,id)>=0;}
        public void CompleteRoom(string id)
        {
            if(string.IsNullOrEmpty(id)||RoomCompleted(id))return;
            var old=completedRooms??Array.Empty<string>();
            var next=new string[old.Length+1];Array.Copy(old,next,old.Length);next[old.Length]=id;completedRooms=next;
        }
        public bool IsEquipped(string id)
        {
            if(string.IsNullOrEmpty(id)) return false;
            if(id.StartsWith("CHARGED_")) return chargedTier >= int.Parse(id.Substring(8));
            if(id.StartsWith("LUNGE_")) return lungeTier >= int.Parse(id.Substring(6));
            if(id.StartsWith("RANGED_")) return rangedTier >= int.Parse(id.Substring(7));
            if(id.StartsWith("VERTICAL_")) return verticalTier>=int.Parse(id.Substring(9));
            if(id.StartsWith("COMBO_")) return comboTier>=int.Parse(id.Substring(6));
            return id==equippedPrayer||id==equippedSwordHeart||(equippedRelics!=null&&Array.IndexOf(equippedRelics,id)>=0)||(equippedRosaryBeads!=null&&Array.IndexOf(equippedRosaryBeads,id)>=0);
        }
        public void SetOwned(string id,bool owned)
        {
            if(string.IsNullOrEmpty(id))return;
            if(ownedItems==null)ownedItems=Array.Empty<string>();
            bool listed=Array.IndexOf(ownedItems,id)>=0;if(listed==owned)return;
            if(owned){var next=new string[ownedItems.Length+1];Array.Copy(ownedItems,next,ownedItems.Length);next[next.Length-1]=id;ownedItems=next;}
            else{var next=new string[Math.Max(0,ownedItems.Length-1)];int n=0;foreach(var item in ownedItems)if(item!=id&&n<next.Length)next[n++]=item;ownedItems=next;}
        }
    }
}
