using System;
using System.Collections.Generic;
using UnityEngine;
namespace Brotherhood
{
    [Serializable] public sealed class SourceMap
    {
        [Serializable] public sealed class Cell
        {
            public string key,room;public int gridX,gridY,type;public bool ignored,ngPlus;
            public float left,bottom,width,height;public int[] walls,doors;
            public bool Contains(Vector2 point)=>point.x>=left&&point.x<left+width&&point.y>=bottom&&point.y<bottom+height;
            // MapRenderer derives both cell dimensions from the 16x16 source
            // sprite rect, independently of the 20x11 world-space bounds.
            public Vector2 MapPosition=>new Vector2(gridX*42,gridY*42);
            public string SpriteKey
            {
                get{string key="";foreach(int side in new[]{2,0,3,1})key+=doors!=null&&doors[side]!=0?"D":walls!=null&&walls[side]!=0?"W":"_";return key;}
            }
        }
        public Cell[] cells=Array.Empty<Cell>();
        static SourceMap source;
        Dictionary<string,Cell> byKey;Dictionary<string,List<Cell>> byRoom;int counted;
        public static SourceMap Load()
        {
            if(source!=null)return source;
            var asset=Resources.Load<TextAsset>("Inventory/map-source");source=asset!=null?JsonUtility.FromJson<SourceMap>(asset.text):new SourceMap();source.Index();return source;
        }
        void Index()
        {
            byKey=new Dictionary<string,Cell>();byRoom=new Dictionary<string,List<Cell>>();
            foreach(var cell in cells)
            {
                byKey[cell.key]=cell;if(!byRoom.TryGetValue(cell.room,out var list))byRoom[cell.room]=list=new List<Cell>();list.Add(cell);
                if(!cell.ignored&&!cell.ngPlus)counted++;
            }
        }
        public IReadOnlyList<Cell> RoomCells(string room)=>byRoom.TryGetValue(room,out var list)?list:Array.Empty<Cell>();
        public Cell Find(string key)=>key!=null&&byKey.TryGetValue(key,out var cell)?cell:null;
        public Cell At(string room,Vector2 position)
        {
            if(!byRoom.TryGetValue(room,out var list))return null;
            foreach(var cell in list)if(cell.Contains(position))return cell;return null;
        }
        public bool Discovered(PlayerProgress progress,string key)=>Array.IndexOf(progress.discoveredMapCells??Array.Empty<string>(),key)>=0;
        public bool Discover(PlayerProgress progress,string room,Vector2 position)
        {
            var cell=At(room,position);if(cell==null||cell.ngPlus||Discovered(progress,cell.key))return false;
            var old=progress.discoveredMapCells??Array.Empty<string>();var next=new string[old.Length+1];Array.Copy(old,next,old.Length);next[old.Length]=cell.key;progress.discoveredMapCells=next;
            Recalculate(progress);return true;
        }
        public void Recalculate(PlayerProgress progress)
        {
            int visited=0;var unique=new HashSet<string>(progress.discoveredMapCells??Array.Empty<string>());
            foreach(string key in unique)if(byKey.TryGetValue(key,out var cell)&&!cell.ignored&&!cell.ngPlus)visited++;
            progress.mapPercentage=counted>0?(float)visited/counted:0;
        }
        public bool MigrateLegacy(PlayerProgress progress,bool recoverRooms=true)
        {
            int previousCount=progress.discoveredMapCells?.Length??0;float previousPercentage=progress.mapPercentage;
            if(recoverRooms&&previousCount==0&&(progress.completedRooms?.Length??0)>0)
            {
                var recovered=new List<string>();foreach(string room in progress.completedRooms)foreach(var cell in RoomCells(room))if(!cell.ngPlus)recovered.Add(cell.key);
                progress.discoveredMapCells=recovered.ToArray();
            }
            Recalculate(progress);
            return previousCount!=(progress.discoveredMapCells?.Length??0)||previousPercentage!=progress.mapPercentage;
        }
    }
}
