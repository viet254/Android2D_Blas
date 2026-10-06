using System;
using System.IO;
using System.Linq;
using Brotherhood;
using UnityEditor;
using UnityEngine;

public static class PrayerAssets
{
    const string Folder="Assets/Brotherhood/Resources/Effects/Prayers";
    const string CatalogPath="Assets/Brotherhood/Resources/Effects/PrayerClips.asset";
    public static void Prepare()
    {
        if(!File.Exists(Folder+"/clips.json"))return;
        var existing=AssetDatabase.LoadAssetAtPath<RestoredCatalog>(CatalogPath);
        if(existing!=null&&File.GetLastWriteTimeUtc(CatalogPath)>=File.GetLastWriteTimeUtc(Folder+"/clips.json"))return;
        foreach(string path in Directory.GetFiles(Folder,"*.png"))
        {
            var importer=AssetImporter.GetAtPath(path.Replace('\\','/')) as TextureImporter;if(importer==null)continue;
            importer.textureType=TextureImporterType.Default;importer.maxTextureSize=16384;
            importer.filterMode=FilterMode.Point;importer.mipmapEnabled=false;importer.alphaIsTransparency=true;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.SaveAndReimport();
        }
        var data=JsonUtility.FromJson<ImportData>(File.ReadAllText(Folder+"/clips.json"));
        var catalog=existing;
        if(catalog==null){catalog=ScriptableObject.CreateInstance<RestoredCatalog>();AssetDatabase.CreateAsset(catalog,CatalogPath);}
        else foreach(var sprite in AssetDatabase.LoadAllAssetsAtPath(CatalogPath).OfType<Sprite>())UnityEngine.Object.DestroyImmediate(sprite,true);
        var sprites=new System.Collections.Generic.Dictionary<string,Sprite>();
        foreach(var source in data.sprites)
        {
            var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Folder+"/"+source.texture+".png");
            if(texture==null)throw new InvalidDataException("Missing supplemental atlas "+source.texture);
            var sprite=Sprite.Create(texture,source.rect.Value,source.pivot,source.ppu,0,SpriteMeshType.FullRect,source.border);
            sprite.name=source.name;AssetDatabase.AddObjectToAsset(sprite,catalog);sprites.Add(source.id,sprite);
        }
        catalog.clips=data.animations.Select(c=>new RestoredClip{name=c.name,spritePath=c.spritePath,duration=c.duration,loop=c.loop,
            events=c.events??Array.Empty<RestoredAnimationEvent>(),floatTracks=c.floatTracks??Array.Empty<RestoredFloatTrack>(),
            times=c.frames.Select(f=>f.time).ToArray(),frames=c.frames.Select(f=>!string.IsNullOrEmpty(f.sprite)&&sprites.TryGetValue(f.sprite,out var sprite)?sprite:null).ToArray()}).ToArray();
        EditorUtility.SetDirty(catalog);AssetDatabase.SaveAssetIfDirty(catalog);
    }
}
