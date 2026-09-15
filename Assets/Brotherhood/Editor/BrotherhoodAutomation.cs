using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class BrotherhoodAutomation
{
    static bool running;
    static double next;
    static BrotherhoodAutomation(){EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(running || EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.timeSinceStartup<next)return;
        next=EditorApplication.timeSinceStartup+1;
        string file=Path.Combine(Application.dataPath,"../Temp/brotherhood-command.txt");
        if(!File.Exists(file))return;
        var built=File.GetLastWriteTimeUtc(typeof(BrotherhoodBuilder).Assembly.Location);
        var runtimeAssembly=typeof(Brotherhood.BrotherhoodGame).Assembly.Location;
        if(File.Exists(runtimeAssembly) && File.GetLastWriteTimeUtc(runtimeAssembly)>built)built=File.GetLastWriteTimeUtc(runtimeAssembly);
        foreach(var source in Directory.GetFiles("Assets/Brotherhood","*.cs",SearchOption.AllDirectories))
            if(File.GetLastWriteTimeUtc(source)>built){AssetDatabase.Refresh();return;}
        string command=File.ReadAllText(file).Trim();File.Delete(file);running=true;
        try
        {
            if(command=="import")BrotherhoodBuilder.Build();
            else if(command=="android")BrotherhoodBuilder.BuildAndroid();
            else if(command=="verify" || command=="menuverify")
            {
                if(EditorApplication.isPlaying)
                {
                    EditorApplication.isPlaying=false;
                    File.WriteAllText(file,command);
                    return;
                }
                EditorSettings.enterPlayModeOptionsEnabled=false;
                string scenePath = command=="verify"?BrotherhoodBuilder.ScenePath:BrotherhoodBuilder.MenuScenePath;
                string flagPath = command=="verify"?"../Temp/brotherhood-verify-play":"../Temp/brotherhood-menu-verify";
                EditorSceneManager.OpenScene(scenePath);
                File.WriteAllText(Path.Combine(Application.dataPath,flagPath),"");
                EditorApplication.isPlaying=true;
            }
            else if(command=="stop")EditorApplication.isPlaying=false;
            File.WriteAllText("Documentation/automation-result.txt",command+" OK "+DateTime.Now);
        }
        catch(Exception e){Debug.LogException(e);File.WriteAllText("Documentation/automation-result.txt",command+" FAILED\n"+e);}
        finally{running=false;}
    }
}
