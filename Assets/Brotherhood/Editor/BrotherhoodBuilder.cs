using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Brotherhood;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BrotherhoodBuilder
{
    // Rebuilds source-derived scenes and mobile runtime wiring.
    const string Root="Assets/Brotherhood";
    public const string ScenePath=Root+"/Scenes/Brotherhood.unity";
    public const string MenuScenePath=Root+"/Scenes/MainMenu.unity";
    static string Project=>Path.GetFullPath(Path.Combine(Application.dataPath,".."));
    [MenuItem("Brotherhood/Rebuild restored campaign")]
    public static void Build()
    {
        var data=JsonUtility.FromJson<ImportData>(File.ReadAllText(Root+"/SourceData/brotherhood.json"));
        Directory.CreateDirectory(Root+"/Generated");Directory.CreateDirectory(Root+"/Scenes");
        var textures=new Dictionary<string,Texture2D>();
        // The extraction tool can add source PNGs while Unity is open. Register
        // them before locking the database for batched importer changes.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.StartAssetEditing();
        try{
            foreach(var s in data.sprites)
            {
                if(textures.ContainsKey(s.texture))continue;
                string p=Root+"/SourceData/Textures/"+s.texture+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(p);
                if(importer==null){Debug.LogError("Missing extracted texture importer: "+p);textures[s.texture]=null;continue;}
                if(importer.userData=="brotherhood-mobile-v1"){textures[s.texture]=null;continue;}
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;
                importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;importer.maxTextureSize=8192;
                importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
                var android=importer.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=4096;android.format=TextureImporterFormat.ETC2_RGBA8;android.compressionQuality=100;importer.SetPlatformTextureSettings(android);
                importer.userData="brotherhood-mobile-v1";AssetDatabase.WriteImportSettingsIfDirty(p);textures[s.texture]=null;
            }
            foreach(var p in Directory.GetFiles(Root+"/Resources/Inventory/Icons","*.png").Concat(Directory.GetFiles(Root+"/Resources/MobileControls","*.png")))
            {
                string path=p.Replace('\\','/');var importer=(TextureImporter)AssetImporter.GetAtPath(path);if(importer==null||importer.userData=="brotherhood-inventory-v1")continue;
                importer.textureType=TextureImporterType.Default;importer.mipmapEnabled=false;importer.filterMode=FilterMode.Point;importer.wrapMode=TextureWrapMode.Clamp;importer.npotScale=TextureImporterNPOTScale.None;importer.textureCompression=TextureImporterCompression.Uncompressed;importer.alphaIsTransparency=true;
                var android=importer.GetPlatformTextureSettings("Android");android.name="Android";android.overridden=true;android.maxTextureSize=512;android.format=TextureImporterFormat.ETC2_RGBA8;android.compressionQuality=100;importer.SetPlatformTextureSettings(android);
                importer.userData="brotherhood-inventory-v1";AssetDatabase.WriteImportSettingsIfDirty(path);
            }
        }finally{AssetDatabase.StopAssetEditing();}
        foreach(string id in textures.Keys.ToArray())textures[id]=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/SourceData/Textures/"+id+".png");
        string catalogPath=Root+"/Generated/RestoredCatalog.asset";
        var old=AssetDatabase.LoadAssetAtPath<RestoredCatalog>(catalogPath);
        if(old!=null)AssetDatabase.DeleteAsset(catalogPath);
        var catalog=ScriptableObject.CreateInstance<RestoredCatalog>();AssetDatabase.CreateAsset(catalog,catalogPath);
        var sprites=new Dictionary<string,Sprite>();
        var buildNotes=new List<string>();
        foreach(var s in data.sprites)
        {
            var tex=textures[s.texture];var rect=s.rect.Value;float ppu=s.ppu;var border=s.border;
            if(rect.width<=0||rect.height<=0)continue;
            if(rect.x<0||rect.y<0)throw new Exception("Invalid negative source sprite rect: "+s.name);
            if(rect.xMax>tex.width+.1f||rect.yMax>tex.height+.1f)
            {
                // Unity can downscale a large non-power-of-two Android atlas at
                // import even when AssetRipper reports rects in original pixels.
                // Scale the rect from the PNG's real dimensions into the loaded
                // texture instead of clipping or silently dropping the sprite.
                string png=Root+"/SourceData/Textures/"+s.texture+".png";var raw=new Texture2D(2,2);
                if(!ImageConversion.LoadImage(raw,File.ReadAllBytes(png),false))throw new Exception("Unreadable source atlas: "+png);
                float sx=tex.width/(float)raw.width,sy=tex.height/(float)raw.height;UnityEngine.Object.DestroyImmediate(raw);
                rect=new Rect(rect.x*sx,rect.y*sy,rect.width*sx,rect.height*sy);ppu*=sx;border=new Vector4(border.x*sx,border.y*sy,border.z*sx,border.w*sy);
                if(rect.xMax>tex.width+.1f||rect.yMax>tex.height+.1f)throw new Exception("Invalid scaled source sprite rect: "+s.name);
                buildNotes.Add("Scaled mobile atlas rect "+s.name+" by "+sx.ToString("0.###")+"x"+sy.ToString("0.###"));
            }
            var sprite=Sprite.Create(tex,rect,s.pivot,ppu,0,SpriteMeshType.FullRect,border);sprite.name=s.name;
            AssetDatabase.AddObjectToAsset(sprite,catalog);sprites[s.id]=sprite;
        }
        catalog.clips=data.animations.Select(c=>new RestoredClip{name=c.name,duration=c.duration,loop=c.loop,times=c.frames.Select(f=>f.time).ToArray(),frames=c.frames.Select(f=>sprites.TryGetValue(f.sprite,out var sp)?sp:null).ToArray()}).ToArray();
        EditorUtility.SetDirty(catalog);
        var shader=Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");if(shader==null)shader=Shader.Find("Sprites/Default");
        string matPath=Root+"/Generated/RestoredSprite.mat";
        var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,matPath);}else material.shader=shader;
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var game=new GameObject("Brotherhood · campaign").AddComponent<BrotherhoodGame>();
        var developmentTools=game.gameObject.AddComponent<DevelopmentDebugMenu>();developmentTools.game=game;
        game.controls=new GameObject("Touch + keyboard + gamepad").AddComponent<TouchControls>();
        game.effects=new GameObject("Impact pool").AddComponent<EffectPool>();game.effects.catalog=catalog;
        game.audioBank=new GameObject("Original audio · voice pool").AddComponent<RestoredAudio>();
        var audioPaths=Directory.GetFiles(Root+"/Audio","*.wav");
        foreach(var path in audioPaths){var ai=AssetImporter.GetAtPath(path.Replace('\\','/')) as AudioImporter;if(ai==null)continue;var settings=ai.defaultSampleSettings;settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.7f;settings.loadType=new FileInfo(path).Length>2000000?AudioClipLoadType.Streaming:AudioClipLoadType.DecompressOnLoad;ai.defaultSampleSettings=settings;ai.SaveAndReimport();}
        game.audioBank.clips=audioPaths.Select(p=>AssetDatabase.LoadAssetAtPath<AudioClip>(p.Replace('\\','/'))).Where(c=>c!=null).ToArray();
        game.view=new GameObject("Main Camera").AddComponent<Camera>();game.view.tag="MainCamera";game.view.orthographic=true;game.view.orthographicSize=5.625f;game.view.backgroundColor=new Color(.07f,.075f,.09f);game.view.clearFlags=CameraClearFlags.SolidColor;game.view.nearClipPlane=.01f;game.view.farClipPlane=500;game.view.gameObject.AddComponent<AudioListener>();
        var player=new GameObject("The Penitent One").AddComponent<PlayerController>();game.player=player;
        player.motor=player.gameObject.AddComponent<KinematicMotor>();player.actor=CreateActor(player.transform,catalog,material,"Player_Idle");
        var rooms=new List<RoomState>();
        foreach(var source in data.rooms)
        {
            var room=new GameObject(source.id).AddComponent<RoomState>();room.id=source.id;room.sourceNodeCount=source.nodes.Length;room.sourceRendererCount=source.nodes.Count(n=>n.renderer!=null&&!string.IsNullOrEmpty(n.renderer.sprite));var nodes=new Dictionary<string,GameObject>();
            foreach(var n in source.nodes){var o=new GameObject(n.name);nodes[n.id]=o;var identity=o.AddComponent<SourceObjectId>();identity.room=source.id;identity.node=n.id;}
            foreach(var n in source.nodes)
            {
                var o=nodes[n.id];o.transform.SetParent(nodes.TryGetValue(n.parent,out var parent)?parent.transform:room.transform,false);
                o.transform.localPosition=n.position;o.transform.localRotation=n.rotation;o.transform.localScale=n.scale;o.SetActive(n.active);
                o.layer=(n.layer==13||n.layer==14)?9:8;
                bool debug=n.name.IndexOf("DEBUG",StringComparison.OrdinalIgnoreCase)>=0 || n.name.IndexOf("Spawner Sprite",StringComparison.OrdinalIgnoreCase)>=0 || n.name=="FakePenitent" || n.name.StartsWith("ElderBrother");
                if(debug)o.SetActive(false);
                if(n.renderer!=null && !string.IsNullOrEmpty(n.renderer.sprite) && sprites.TryGetValue(n.renderer.sprite,out var sprite))
                {var sr=o.AddComponent<SpriteRenderer>();sr.sprite=sprite;sr.sharedMaterial=material;sr.color=n.renderer.color;sr.flipX=n.renderer.flipX;sr.flipY=n.renderer.flipY;sr.enabled=n.renderer.enabled;sr.sortingLayerID=unchecked((int)n.renderer.sortingLayer);sr.sortingOrder=n.renderer.order;sr.drawMode=(SpriteDrawMode)n.renderer.drawMode;sr.size=n.renderer.size;sr.tileMode=(SpriteTileMode)n.renderer.tileMode;sr.adaptiveModeThreshold=n.renderer.adaptive;sr.maskInteraction=(SpriteMaskInteraction)n.renderer.mask;}
                // Only terrain geometry is carried into player physics. Old sensors are replaced below.
                if(n.section=="LAYOUT"&&(n.layer==19||n.layer==13||n.layer==14))
                foreach(var c in n.colliders)
                {
                    if(c.trigger || !c.enabled)continue;Collider2D collider=null;
                    if(c.kind=="BoxCollider2D"){var box=o.AddComponent<BoxCollider2D>();box.size=c.size;collider=box;}
                    else if(c.kind=="PolygonCollider2D" && c.paths.Length>0){var poly=o.AddComponent<PolygonCollider2D>();poly.pathCount=c.paths.Length;for(int i=0;i<c.paths.Length;i++)poly.SetPath(i,c.paths[i].points.Select(p=>(Vector2)p).ToArray());collider=poly;}
                    else if(c.kind=="EdgeCollider2D" && c.paths.Length>0){var edge=o.AddComponent<EdgeCollider2D>();edge.points=c.paths[0].points.Select(p=>(Vector2)p).ToArray();collider=edge;}
                    else if(c.kind=="CircleCollider2D"){var circle=o.AddComponent<CircleCollider2D>();circle.radius=c.radius;collider=circle;}
                    if(collider!=null)collider.offset=c.offset;
                }
            }
            var doors=new List<RoomDoor>();var shrines=new List<Transform>();var enemies=new List<EnemyController>();
            foreach(var marker in source.markers)
            {
                var o=nodes[marker.node];
                if(marker.kind=="DebugSpawn")room.start=o.transform;
                if(marker.kind=="Door")doors.Add(new RoomDoor{key=marker.key,target=marker.target,targetDoor=marker.door,trigger=o.transform,spawn=nodes.TryGetValue(marker.spawn,out var sp)?sp.transform:o.transform});
                if(marker.kind=="PrieDieu")
                {
                    shrines.Add(o.transform);
                    var lvl2=o.transform.Find("Interactable Animation_level2");
                    if(lvl2!=null)lvl2.gameObject.SetActive(false);
                    var lvl3=o.transform.Find("Interactable Animation_level3");
                    if(lvl3!=null)lvl3.gameObject.SetActive(false);
                    var animChild=o.transform.Find("Interactable Animation_level1");
                    if(animChild!=null)
                    {
                        animChild.gameObject.SetActive(true);
                        var sa=animChild.gameObject.AddComponent<SpriteActor>();
                        sa.catalog=catalog;
                        sa.visual=animChild.GetComponent<SpriteRenderer>();
                        sa.Play("Priedieu_shrine_off",true);
                    }
                }
                if(marker.kind=="CameraNumericBoundaries"){room.left=marker.left;room.right=marker.right;room.bottom=marker.bottom;room.top=marker.top;}
                if(marker.kind=="ElderBrother")
                {var e=CreateEnemy(room.transform,catalog,material,true,"elderbrother",o.transform.position);enemies.Add(e);}
            }
            if(room.start==null){var start=new GameObject("Arrival");start.transform.SetParent(room.transform);start.transform.position=Vector3.zero;room.start=start.transform;}
            // Authored encounters requested by the plan; not claimed to be original spawn data.
            if(source.id=="D17Z01S02"||source.id=="D17Z01S05")
            {
                float x=room.start.position.x+5;
                Physics2D.SyncTransforms();var ground=Physics2D.Raycast(new Vector2(x,room.start.position.y+2),Vector2.down,8,1<<8);
                if(ground.collider!=null){var e=CreateEnemy(room.transform,catalog,material,false,source.id=="D17Z01S02"?"acolyte":"flagellant",ground.point+Vector2.up*.03f);enemies.Add(e);buildNotes.Add(source.id+": authored "+e.family+" encounter at "+e.transform.position);}
            }
            var breakableList=new List<GameObject>();
            foreach(var n in source.nodes)
            {
                if(n.name.IndexOf("TwistedSymbol_breakable",StringComparison.OrdinalIgnoreCase)>=0||n.name.IndexOf("BreakableLantern",StringComparison.OrdinalIgnoreCase)>=0)
                {if(nodes.TryGetValue(n.id,out var bgo))breakableList.Add(bgo);}
            }
            room.breakables=breakableList.ToArray();
            room.doors=doors.ToArray();room.checkpoints=shrines.ToArray();room.enemies=enemies.ToArray();room.parallax=Array.Empty<ParallaxLayer>();
            var ladders=new List<LadderZone>();
            foreach(var n in source.nodes)if(n.name.IndexOf("LadderTrigger",StringComparison.OrdinalIgnoreCase)>=0)
            foreach(var c in n.colliders)if(c.kind=="BoxCollider2D"&&c.enabled)
            {var t=nodes[n.id].transform;var scale=t.lossyScale;ladders.Add(new LadderZone{center=t.TransformPoint(c.offset),size=new Vector2(Mathf.Abs(c.size.x*scale.x),Mathf.Abs(c.size.y*scale.y))});}
            room.ladders=ladders.ToArray();
            var faith=new Dictionary<string,FaithPlatform>();
            foreach(var marker in source.markers)if(marker.kind=="FaithPlatform")
            {
                var p=nodes[marker.node].AddComponent<FaithPlatform>();p.first=marker.first;p.deactivationDelay=marker.delay;
                var body=nodes[marker.colliderNode];p.collision=body.GetComponent<BoxCollider2D>();if(p.collision==null)p.collision=body.AddComponent<BoxCollider2D>();
                p.collision.size=marker.size;p.collision.offset=marker.offset;p.collision.enabled=false;body.layer=9;
                p.actor=p.gameObject.AddComponent<SpriteActor>();p.actor.catalog=catalog;p.actor.visual=nodes[marker.rendererNode].GetComponent<SpriteRenderer>();faith[marker.node]=p;
            }
            foreach(var marker in source.markers)if(marker.kind=="FaithPlatform")faith[marker.node].targets=marker.targets.Where(t=>faith.ContainsKey(t)).Select(t=>faith[t]).ToArray();
            room.faithPlatforms=faith.Values.ToArray();
            // Read the serialized source parallax references instead of inventing four layers.
            foreach(var marker in source.markers)if(marker.kind=="ParallaxController")
            {var o=nodes[marker.node];room.parallaxOrigin=o.transform.position;var pd=JsonUtility.FromJson<SourceParallax>(marker.data);var layers=new List<ParallaxLayer>();if(pd.layers!=null)foreach(var p in pd.layers){var n=source.nodes.FirstOrDefault(v=>v.section=="DECO"&&v.gameObject==p.layer.fileID);if(n!=null)layers.Add(new ParallaxLayer{target=nodes[n.id].transform,speed=p.speed*pd.influenceX,speedY=p.speed*pd.influenceY});}room.parallax=layers.ToArray();}
            rooms.Add(room);room.gameObject.SetActive(false);
        }
        game.rooms=rooms.ToArray();rooms[0].gameObject.SetActive(true);
        player.transform.position=rooms[0].start.position;game.view.transform.position=player.transform.position+new Vector3(0,2.4f,-10);
        PlayerSettings.companyName="Independent";PlayerSettings.productName="Brotherhood";
        // The shipped Android source is Gamma and landscape-right. Preserve
        // those presentation choices while keeping the independent package id.
        PlayerSettings.colorSpace=ColorSpace.Gamma;PlayerSettings.defaultInterfaceOrientation=UIOrientation.LandscapeRight;
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android,"com.independent.brotherhood");
        PlayerSettings.Android.targetArchitectures=AndroidArchitecture.ARM64;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android,ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.minSdkVersion=AndroidSdkVersions.AndroidApiLevel26;
        EditorSceneManager.SaveScene(scene,ScenePath);
        var menuScene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        new GameObject("Brotherhood · main menu").AddComponent<MainMenuController>();
        EditorSceneManager.SaveScene(menuScene,MenuScenePath);
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(MenuScenePath,true),new EditorBuildSettingsScene(ScenePath,true)};
        scene=EditorSceneManager.OpenScene(ScenePath);AssetDatabase.SaveAssets();
        MapImportValidator.ValidateActiveScene();
        File.WriteAllText(Path.Combine(Project,"Documentation/build-import.txt"),$"Imported {rooms.Count} rooms, {sprites.Count} sprites, {catalog.clips.Length} clips.\n"+string.Join("\n",buildNotes));
        // Leave the project at the real entry scene so Editor Play follows the
        // same new/continue flow as an installed build instead of loading a
        // stale gameplay save directly.
        EditorSceneManager.OpenScene(MenuScenePath);
        Debug.Log("BROTHERHOOD_BUILD_OK");
    }
    [Serializable] class SourceParallax {public SourceLayer[] layers;public float influenceX,influenceY;}
    [Serializable] class SourceLayer {public Ref layer;public float speed;}
    [Serializable] class Ref {public long fileID;}
    static SpriteActor CreateActor(Transform parent,RestoredCatalog catalog,Material material,string clip)
    {var actor=parent.gameObject.AddComponent<SpriteActor>();actor.catalog=catalog;var visual=new GameObject("Visual");visual.transform.SetParent(parent,false);visual.transform.localPosition=Vector3.zero;actor.visual=visual.AddComponent<SpriteRenderer>();actor.visual.sharedMaterial=material;actor.visual.sortingLayerName="Default";actor.visual.sortingOrder=100;actor.visual.sprite=catalog.Find(clip)?.frames.FirstOrDefault();return actor;}
    static EnemyController CreateEnemy(Transform parent,RestoredCatalog catalog,Material material,bool boss,string family,Vector2 position)
    {var e=new GameObject(boss?"Warden":"Encounter "+family).AddComponent<EnemyController>();e.transform.SetParent(parent);e.transform.position=position;e.boss=boss;e.family=family;e.maxHealth=e.health=boss?400:family=="acolyte"?60:100;e.purgeReward=boss?300:family=="acolyte"?15:10;e.motor=e.gameObject.AddComponent<KinematicMotor>();e.motor.size=boss?new Vector2(1.8f,3):new Vector2(.6f,1.5f);e.actor=CreateActor(e.transform,catalog,material,boss?"ElderBrother_Idle":family=="acolyte"?"acolyte_idle":"NewFlagellant_idle");return e;}
    public static void BuildAndroid()
    {
        Directory.CreateDirectory("Builds/Android");
        if(EditorUserBuildSettings.activeBuildTarget!=BuildTarget.Android)EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android,BuildTarget.Android);
        var result=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{MenuScenePath,ScenePath},locationPathName="Builds/Android/Brotherhood.apk",target=BuildTarget.Android,options=BuildOptions.Development});
        File.WriteAllText("Documentation/android-build.txt",result.summary.result+" · "+result.summary.totalErrors+" errors · "+result.summary.totalSize+" bytes");
        if(result.summary.result!=UnityEditor.Build.Reporting.BuildResult.Succeeded)throw new Exception("Android build failed");
    }
}
