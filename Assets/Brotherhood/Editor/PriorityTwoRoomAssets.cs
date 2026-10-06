using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Brotherhood;
using UnityEditor;
using UnityEngine;

// Adds four bounded source rooms as prefabs. Never rebuilds either saved scene
// or the main catalog, and never carries DEBUG progression commands into play.
public static class PriorityTwoRoomAssets
{
    const string Source="Assets/Brotherhood/PriorityTwo/SourceData";
    const string Output="Assets/Brotherhood/Resources/Rooms";
    public static void Prepare()
    {
        string json=Source+"/brotherhood.json";if(!File.Exists(json))return;
        Directory.CreateDirectory(Output+"/PriorityTwo");
        var data=JsonUtility.FromJson<ImportData>(File.ReadAllText(json));string path=Output+"/PriorityTwoClips.asset";
        if(File.Exists(path)&&File.GetLastWriteTimeUtc(path)>=File.GetLastWriteTimeUtc(json)&&data.rooms.All(r=>File.Exists(Output+"/PriorityTwo/"+r.id+".prefab")))return;
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach(string png in Directory.GetFiles(Source+"/Textures","*.png"))
        {
            var ti=AssetImporter.GetAtPath(png.Replace('\\','/')) as TextureImporter;if(ti==null)throw new InvalidDataException(png);
            ti.textureType=TextureImporterType.Default;ti.maxTextureSize=16384;ti.mipmapEnabled=false;ti.filterMode=FilterMode.Point;ti.npotScale=TextureImporterNPOTScale.None;
            ti.alphaIsTransparency=true;ti.textureCompression=TextureImporterCompression.Uncompressed;ti.SaveAndReimport();
        }
        var catalog=AssetDatabase.LoadAssetAtPath<RestoredCatalog>(path);
        if(catalog==null){catalog=ScriptableObject.CreateInstance<RestoredCatalog>();AssetDatabase.CreateAsset(catalog,path);}
        else foreach(var s in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())UnityEngine.Object.DestroyImmediate(s,true);
        var sprites=new Dictionary<string,Sprite>();
        foreach(var s in data.sprites)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Source+"/Textures/"+s.texture+".png");
            if(texture==null||s.rect.Value.xMax>texture.width+.1f||s.rect.Value.yMax>texture.height+.1f)throw new InvalidDataException("Supplemental sprite outside atlas: "+s.name);
            var sprite=Sprite.Create(texture,s.rect.Value,s.pivot,s.ppu,0,SpriteMeshType.FullRect,s.border);sprite.name=s.name;AssetDatabase.AddObjectToAsset(sprite,catalog);sprites.Add(s.id,sprite);
        }
        catalog.clips=data.animations.Select(c=>new RestoredClip{name=c.name,spritePath=c.spritePath,duration=c.duration,loop=c.loop,events=c.events??Array.Empty<RestoredAnimationEvent>(),floatTracks=c.floatTracks??Array.Empty<RestoredFloatTrack>(),times=c.frames.Select(f=>f.time).ToArray(),frames=c.frames.Select(f=>!string.IsNullOrEmpty(f.sprite)&&sprites.TryGetValue(f.sprite,out var s)?s:null).ToArray()}).ToArray();
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
        var material=AssetDatabase.LoadAssetAtPath<Material>("Assets/Brotherhood/Generated/RestoredSprite.mat");
        foreach(var source in data.rooms)
        {
            var room=new GameObject(source.id).AddComponent<RoomState>();room.id=source.id;room.sourceNodeCount=source.nodes.Length;room.sourceRendererCount=source.nodes.Count(n=>n.renderer!=null&&!string.IsNullOrEmpty(n.renderer.sprite));
            try
            {
                var nodes=source.nodes.ToDictionary(n=>n.id,n=>new GameObject(n.name));
                foreach(var n in source.nodes)
                {
                    var o=nodes[n.id];o.transform.SetParent(nodes.TryGetValue(n.parent,out var parent)?parent.transform:room.transform,false);o.transform.localPosition=n.position;o.transform.localRotation=n.rotation;o.transform.localScale=n.scale;o.layer=n.layer==13||n.layer==14?9:8;
                    var identity=o.AddComponent<SourceObjectId>();identity.room=source.id;identity.node=n.id;
                    o.SetActive(n.active&&!n.name.Contains("DEBUG")&&!n.name.Contains("Spawner Sprite"));
                    if(n.renderer!=null&&!string.IsNullOrEmpty(n.renderer.sprite)&&sprites.TryGetValue(n.renderer.sprite,out var sprite))
                    {
                        var sr=o.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.enabled=n.renderer.enabled;sr.color=n.renderer.color;sr.flipX=n.renderer.flipX;sr.flipY=n.renderer.flipY;sr.sortingLayerID=unchecked((int)n.renderer.sortingLayer);sr.sortingOrder=n.renderer.order;sr.drawMode=(SpriteDrawMode)n.renderer.drawMode;sr.size=n.renderer.size;sr.tileMode=(SpriteTileMode)n.renderer.tileMode;
                        if(!string.IsNullOrEmpty(n.animation)&&catalog.Find(n.animation)!=null){var actor=o.AddComponent<SpriteActor>();actor.catalog=catalog;actor.visual=sr;actor.initialClip=n.animation;actor.initialLoop=catalog.Find(n.animation).loop;}
                    }
                    if((n.section=="LAYOUT"||n.section=="LOGIC")&&(n.layer==19||n.layer==13||n.layer==14))foreach(var c in n.colliders)
                    {
                        if(!c.enabled||c.trigger)continue;Collider2D collider=null;
                        if(c.kind=="BoxCollider2D"){var b=o.AddComponent<BoxCollider2D>();b.size=c.size;collider=b;}
                        else if(c.kind=="PolygonCollider2D"&&c.paths.Length>0){var p=o.AddComponent<PolygonCollider2D>();p.pathCount=c.paths.Length;for(int i=0;i<c.paths.Length;i++)p.SetPath(i,c.paths[i].points.Select(v=>(Vector2)v).ToArray());collider=p;}
                        else if(c.kind=="EdgeCollider2D"&&c.paths.Length>0){var e=o.AddComponent<EdgeCollider2D>();e.points=c.paths[0].points.Select(v=>(Vector2)v).ToArray();collider=e;}
                        else if(c.kind=="CircleCollider2D"){var circle=o.AddComponent<CircleCollider2D>();circle.radius=c.radius;collider=circle;}
                        if(collider!=null)collider.offset=c.offset;
                    }
                }
                room.doors=dataFor("Door").Select(m=>new RoomDoor{key=m.key,target=m.target,targetDoor=m.door,trigger=nodes[m.node].transform,spawn=nodes.TryGetValue(m.spawn,out var o)?o.transform:nodes[m.node].transform}).ToArray();
                room.checkpoints=dataFor("PrieDieu").Select(m=>nodes[m.node].transform).ToArray();room.skillAltars=dataFor("MeaCulpaAltar").Select(m=>new SkillAltarZone{sensor=nodes[m.node].transform,offset=m.offset,size=m.size}).ToArray();
                room.start=nodes[dataFor("DebugSpawn").First().node].transform;
                room.cameraRegions=dataFor("CameraNumericBoundaries").Select(m=>new CameraRegion{left=m.left,right=m.right,bottom=m.bottom,top=m.top}).ToArray();
                if(room.cameraRegions.Length>0){var b=room.cameraRegions[0];room.left=b.left;room.right=b.right;room.bottom=b.bottom;room.top=b.top;}
                room.ladders=source.nodes.Where(n=>n.name.Contains("LadderTrigger")).SelectMany(n=>n.colliders.Where(c=>c.kind=="BoxCollider2D"&&c.enabled).Select(c=>{var t=nodes[n.id].transform;return new LadderZone{sensor=t,center=t.TransformPoint(c.offset),size=new Vector2(Mathf.Abs(c.size.x*t.lossyScale.x),Mathf.Abs(c.size.y*t.lossyScale.y))};})).ToArray();
                room.parallax=dataFor("ParallaxController").SelectMany(m=>
                {
                    var p=JsonUtility.FromJson<Parallax>(m.data);var origin=nodes[m.node].transform.position;room.parallaxOrigin=origin;
                    return (p.layers??Array.Empty<Layer>()).Select(l=>new{layer=l,node=source.nodes.FirstOrDefault(n=>n.section==m.node.Split('_')[0]&&n.gameObject==l.layer.fileID)}).Where(v=>v.node!=null).Select(v=>new ParallaxLayer{target=nodes[v.node.id].transform,speed=Mathf.Floor(v.layer.speed*32)/32f*p.influenceX,speedY=Mathf.Floor(v.layer.speed*32)/32f*p.influenceY,cameraOrigin=origin});
                }).ToArray();
                room.enemies=Array.Empty<EnemyController>();room.faithPlatforms=Array.Empty<FaithPlatform>();room.mudZones=Array.Empty<MudZone>();room.collectibles=Array.Empty<CollectibleZone>();room.breakables=Array.Empty<GameObject>();
                room.gameObject.SetActive(false);PrefabUtility.SaveAsPrefabAsset(room.gameObject,Output+"/PriorityTwo/"+room.id+".prefab");
                IEnumerable<MarkerData> dataFor(string kind)=>source.markers.Where(m=>m.kind==kind);
            }
            finally{UnityEngine.Object.DestroyImmediate(room.gameObject);}
        }
    }
    [Serializable] class Parallax{public Layer[] layers;public float influenceX,influenceY;}
    [Serializable] class Layer{public Ref layer;public float speed;}
    [Serializable] class Ref{public long fileID;}
}
