// Adapted from the supplied validator: source-aware, read-only, no global Z/layer rewrite.
using System;
using System.IO;
using System.Linq;
using System.Text;
using Brotherhood;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MapImportValidator
{
    [MenuItem("Brotherhood/Validate restored map against source")]
    public static void ValidateActiveScene()
    {
        string report=Validate(SceneManager.GetActiveScene(),out int errors);
        File.WriteAllText("Documentation/map-validation.txt",report);
        if(errors>0)throw new InvalidOperationException(report);Debug.Log(report);
        CheckDiagnosticSensitivity(SceneManager.GetActiveScene());
    }
    static void CheckDiagnosticSensitivity(Scene scene)
    {
        var identity=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SourceObjectId>(true)).First(i=>
        {
            var candidate=i.GetComponent<SpriteRenderer>();
            return candidate!=null&&candidate.sprite!=null;
        });
        var renderer=identity.GetComponent<SpriteRenderer>();var sprite=renderer.sprite;var position=identity.transform.localPosition;
        try
        {
            renderer.sprite=null;Validate(scene,out int missing);renderer.sprite=sprite;
            identity.transform.localPosition=position+Vector3.forward;Validate(scene,out int moved);
            if(missing==0||moved==0)throw new InvalidOperationException("Validator failed fault-injection checks");
            File.WriteAllText("Documentation/validator-selftest.txt","PASS deliberately removed sprite detected\nPASS deliberately changed Z detected\nAll mutations restored; scene not saved with test faults.");
        }
        finally{renderer.sprite=sprite;identity.transform.localPosition=position;}
    }
    public static string Validate(Scene scene,out int errors)
    {
        var data=JsonUtility.FromJson<ImportData>(File.ReadAllText("Assets/Brotherhood/SourceData/brotherhood.json"));
        var ids=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SourceObjectId>(true)).ToArray();
        var sprites=data.sprites.ToDictionary(s=>s.id);var text=new StringBuilder();errors=0;int checkedRenderers=0,sourceEmpty=0;
        text.AppendLine("Source-aware map validation · "+DateTime.Now.ToString("s"));
        foreach(var room in data.rooms)
        {
            var matches=ids.Where(i=>i.room==room.id).ToArray();
            if(matches.Length!=room.nodes.Length){text.AppendLine("ERROR node count "+room.id);errors++;}
            var groups=matches.GroupBy(i=>i.node).ToArray();
            if(groups.Any(g=>g.Count()!=1)){text.AppendLine("ERROR duplicate source identities "+room.id);errors++;continue;}
            var map=groups.ToDictionary(g=>g.Key,g=>g.First());
            foreach(var node in room.nodes)
            {
                string id=room.id+"/"+node.id+"/"+node.name;
                if(!map.TryGetValue(node.id,out var identity)){text.AppendLine("ERROR missing object "+id);errors++;continue;}
                var t=identity.transform;
                if((t.localPosition-node.position).sqrMagnitude>.000001f || (t.localScale-node.scale).sqrMagnitude>.000001f || Quaternion.Angle(t.localRotation,node.rotation)>.01f)
                {text.AppendLine("ERROR source transform differs "+id);errors++;}
                string parent=t.parent!=null?t.parent.GetComponent<SourceObjectId>()?.node:null;
                if(map.ContainsKey(node.parent)&&parent!=node.parent){text.AppendLine("ERROR hierarchy differs "+id);errors++;}
                var expected=node.renderer;if(!node.hasRenderer)continue;
                if(string.IsNullOrEmpty(expected.sprite)){sourceEmpty++;continue;}
                var sr=t.GetComponent<SpriteRenderer>();checkedRenderers++;
                if(sr==null||sr.sprite==null){text.AppendLine("ERROR missing referenced sprite "+id+" GUID="+expected.sprite);errors++;continue;}
                if(!sprites.TryGetValue(expected.sprite,out var original)){text.AppendLine("ERROR sprite absent from conversion manifest "+id);errors++;continue;}
                if(sr.sprite.name!=original.name || sr.sprite.rect!=original.rect.Value || Mathf.Abs(sr.sprite.pixelsPerUnit-original.ppu)>.001f || Vector2.Distance(sr.sprite.pivot,new Vector2(original.pivot.x*original.rect.width,original.pivot.y*original.rect.height))>.01f)
                {text.AppendLine("ERROR sprite identity/rect/pivot differs "+id);errors++;}
                if(sr.sortingLayerID!=unchecked((int)expected.sortingLayer)||sr.sortingOrder!=expected.order)
                {text.AppendLine("ERROR source sorting differs "+id);errors++;}
                if((int)sr.drawMode!=expected.drawMode || (expected.drawMode!=0 && (Vector2.Distance(sr.size,expected.size)>.001f || (int)sr.tileMode!=expected.tileMode || sr.sprite.border!=original.border)))
                {text.AppendLine("ERROR tiled/sliced geometry differs "+id);errors++;}
                if(!SortingLayer.IsValid(sr.sortingLayerID)){text.AppendLine("ERROR missing sorting layer "+id);errors++;}
                if(sr.enabled!=expected.enabled||sr.flipX!=expected.flipX||sr.flipY!=expected.flipY)
                {text.AppendLine("ERROR renderer flags differ "+id);errors++;}
            }
            text.AppendLine(room.id+": "+matches.Length+" source nodes; sections "+string.Join(",",room.nodes.Select(n=>n.section).Distinct()));
        }
        text.AppendLine($"Referenced renderers: {checkedRenderers}; intentionally empty source references: {sourceEmpty}; errors: {errors}");
        text.AppendLine("This checks source parity, not whether every apparent opening is an error. Door openings, masks, shader behavior and runtime state still require visual review.");
        return text.ToString();
    }
    [MenuItem("Brotherhood/Testing/Equip blood relic")]
    static void Equip(){var game=UnityEngine.Object.FindFirstObjectByType<BrotherhoodGame>();if(Application.isPlaying&&game!=null)game.SetBloodRelic(true,true);}
    [MenuItem("Brotherhood/Testing/Unequip blood relic")]
    static void Unequip(){var game=UnityEngine.Object.FindFirstObjectByType<BrotherhoodGame>();if(Application.isPlaying&&game!=null)game.SetBloodRelic(true,false);}
}
