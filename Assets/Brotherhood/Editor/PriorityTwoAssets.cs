using System;
using System.IO;
using System.Linq;
using Brotherhood;
using UnityEditor;
using UnityEngine;

public static class PriorityTwoAssets
{
    const string Folder="Assets/Brotherhood/Resources/Effects/PriorityTwo";
    const string CatalogPath="Assets/Brotherhood/Resources/Effects/PriorityTwoClips.asset";
    public static void Prepare()
    {
        PriorityTwoRoomAssets.Prepare();
        foreach(string path in Directory.GetFiles("Assets/Brotherhood/Resources/Cutscenes","CTS02Preview_*.png"))
        {
            var ti=AssetImporter.GetAtPath(path.Replace('\\','/')) as TextureImporter;if(ti==null||ti.userData=="priority-two-cts02-v1")continue;
            ti.mipmapEnabled=false;ti.filterMode=FilterMode.Bilinear;ti.wrapMode=TextureWrapMode.Clamp;ti.npotScale=TextureImporterNPOTScale.None;ti.maxTextureSize=2048;ti.textureCompression=TextureImporterCompression.Compressed;ti.userData="priority-two-cts02-v1";ti.SaveAndReimport();
        }
        foreach(string path in Directory.GetFiles("Assets/Brotherhood/Resources/Audio","*.wav"))
        {
            // These two assets predate priority two. Preserve their import
            // settings just as we preserve the existing scene audio bank.
            if(Path.GetFileNameWithoutExtension(path)=="BELL_RECEIVER_ACTIVATE"||Path.GetFileNameWithoutExtension(path)=="GATE_OPEN")continue;
            var ai=AssetImporter.GetAtPath(path.Replace('\\','/')) as AudioImporter;if(ai==null||ai.userData=="priority-two-audio-v1")continue;
            var settings=ai.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;settings.loadType=new FileInfo(path).Length>2000000?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;ai.defaultSampleSettings=settings;ai.userData="priority-two-audio-v1";ai.SaveAndReimport();
        }
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
