using System;
using UnityEngine;

namespace Brotherhood
{
    [Serializable] public class ImportData { public RoomData[] rooms; public SpriteData[] sprites; public ClipData[] animations; }
    [Serializable] public class RoomData { public string id; public NodeData[] nodes; public MarkerData[] markers; }
    [Serializable] public class NodeData { public string id,parent,name,section; public long gameObject; public bool active,hasRenderer; public int layer; public Vector3 position,scale; public Quaternion rotation; public RendererData renderer; public ColliderData[] colliders; }
    [Serializable] public class RendererData { public string sprite; public bool enabled,flipX,flipY; public Color color; public int order,drawMode,tileMode,mask;public float adaptive;public Vector2 size; public long sortingLayer; }
    [Serializable] public class ColliderData { public string kind; public bool enabled,trigger; public Vector3 offset,size; public float radius; public PathData[] paths; }
    [Serializable] public class PathData { public Vector3[] points; }
    [Serializable] public class MarkerData { public string kind,node,name,data,target,door,key,spawn,rendererNode,colliderNode; public bool first; public float delay; public Vector3 size,offset; public string[] targets; public float left,right,bottom,top; }
    [Serializable] public class SourceRect { public float x,y,width,height; public Rect Value=>new Rect(x,y,width,height); }
    [Serializable] public class SpriteData { public string id,name,texture; public SourceRect rect; public Vector2 pivot;public Vector4 border; public float ppu; public int packing; }
    [Serializable] public class ClipData { public string name; public FrameData[] frames; public float duration; public bool loop; }
    [Serializable] public class FrameData { public float time; public string sprite; }
    [Serializable] public class RestoredClip { public string name; public Sprite[] frames; public float[] times; public float duration; public bool loop; }
}
