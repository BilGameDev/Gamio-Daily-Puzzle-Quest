using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildCommand
{
    public static void PerformBuild()
    {
        Debug.Log("Starting iOS build from GitHub Actions...");
        
        // Get command line arguments
        string[] args = System.Environment.GetCommandLineArgs();
        string buildPath = "";
        
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "-buildPath" && i + 1 < args.Length)
            {
                buildPath = args[i + 1];
                break;
            }
        }
        
        if (string.IsNullOrEmpty(buildPath))
        {
            buildPath = "build/ios";
        }
        
        Debug.Log($"Build path: {buildPath}");
        
        // Build options
        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = GetEnabledScenes();
        buildPlayerOptions.locationPathName = buildPath;
        buildPlayerOptions.target = BuildTarget.iOS;
        buildPlayerOptions.options = BuildOptions.None;
        
        // Perform build
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        
        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"✅ Build succeeded! Output: {buildPath}");
        }
        else
        {
            Debug.LogError($"❌ Build failed: {report.summary.result}");
            throw new System.Exception("Unity build failed");
        }
    }
    
    private static string[] GetEnabledScenes()
    {
        var scenes = new System.Collections.Generic.List<string>();
        foreach (var scene in EditorBuildSettings.scenes)
        {
            if (scene.enabled)
            {
                scenes.Add(scene.path);
                Debug.Log($"Including scene: {scene.path}");
            }
        }
        
        if (scenes.Count == 0)
        {
            Debug.LogError("No enabled scenes found in Build Settings!");
        }
        
        return scenes.ToArray();
    }
}