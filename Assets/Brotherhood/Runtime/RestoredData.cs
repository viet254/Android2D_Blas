using System;
using UnityEngine;

namespace Brotherhood
{
    [Serializable] public class ImportData { public RoomData[] rooms; public SpriteData[] sprites; public ClipData[] animations; }
    [Serializable] public class RoomData { public string id; public NodeData[] nodes; public MarkerData[] markers; }
    [Serializable] public class NodeData { public string id,parent,name,section,animation; public long gameObject; public bool active,hasRenderer; public int layer; public Vector3 position,scale; public Quaternion rotation; public RendererData renderer; public MeshData mesh; public ColliderData[] colliders; }
    [Serializable] public class MeshData {public string asset;public bool enabled;public Color color;public long sortingLayer;public int order;}
    [Serializable] public class RendererData { public string sprite; public bool enabled,flipX,flipY; public Color color; public int order,drawMode,tileMode,mask;public float adaptive;public Vector2 size; public long sortingLayer; }
    [Serializable] public class ColliderData { public string kind; public bool enabled,trigger; public Vector3 offset,size; public float radius; public PathData[] paths; }
    [Serializable] public class PathData { public Vector3[] points; }
    [Serializable] public class MarkerData { public string kind,node,name,data,item,target,door,key,spawn,rendererNode,colliderNode; public bool first; public float delay; public Vector3 size,offset; public string[] targets; public float left,right,bottom,top; }
    [Serializable] public class SourceRect { public float x,y,width,height; public Rect Value=>new Rect(x,y,width,height); }
    [Serializable] public class SpriteData { public string id,name,texture; public SourceRect rect; public Vector2 pivot;public Vector4 border; public float ppu; public int packing; }
    [Serializable] public class ClipData { public string name,spritePath; public FrameData[] frames; public float duration; public bool loop; public RestoredAnimationEvent[] events; public RestoredFloatTrack[] floatTracks; }
    [Serializable] public class FrameData { public float time; public string sprite; }
    [Serializable] public class RestoredAnimationEvent { public float time,floatParameter; public int intParameter,messageOptions; public string functionName,stringParameter,objectGuid; public long objectFileID; }
    [Serializable] public class RestoredFloatKey { public float time,value,inSlope,outSlope,inWeight,outWeight; public bool inStep,outStep; public int tangentMode,weightedMode; }
    [Serializable] public class RestoredFloatTrack
    {
        public string attribute,path; public int classID,preInfinity,postInfinity; public RestoredFloatKey[] keys;
        [NonSerialized] AnimationCurve curve;
        public float Evaluate(float time)
        {
            if(curve==null)
            {
                var source=keys??Array.Empty<RestoredFloatKey>();var frames=new Keyframe[source.Length];
                for(int i=0;i<source.Length;i++){var k=source[i];frames[i]=new Keyframe(k.time,k.value,k.inStep?float.PositiveInfinity:k.inSlope,k.outStep?float.PositiveInfinity:k.outSlope,k.inWeight,k.outWeight){weightedMode=(WeightedMode)k.weightedMode};}
                curve=new AnimationCurve(frames){preWrapMode=WrapMode.ClampForever,postWrapMode=WrapMode.ClampForever};
            }
            return curve.Evaluate(time);
        }
    }
    [Serializable] public class RestoredClip { public string name,spritePath; public Sprite[] frames; public float[] times; public float duration; public bool loop; public RestoredAnimationEvent[] events=Array.Empty<RestoredAnimationEvent>(); public RestoredFloatTrack[] floatTracks=Array.Empty<RestoredFloatTrack>(); }
}
