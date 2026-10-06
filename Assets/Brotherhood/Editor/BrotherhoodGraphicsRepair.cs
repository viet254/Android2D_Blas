using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public static class BrotherhoodGraphicsRepair
{
    [MenuItem("Brotherhood/Use Direct3D 11 for Windows Editor")]
    public static void UseDirect3D11()
    {
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
            new[] { GraphicsDeviceType.Direct3D11 });
        AssetDatabase.SaveAssets();
        Debug.Log("Brotherhood Windows graphics API: " +
            string.Join(", ", PlayerSettings.GetGraphicsAPIs(BuildTarget.StandaloneWindows64)));
    }
}
