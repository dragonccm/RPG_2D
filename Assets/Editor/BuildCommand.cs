using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildCommand
{
    public static void Build()
    {
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/_Project/Scenes/Main.unity" },
            locationPathName = "Builds/Windows",
            options = BuildOptions.None,
            target = BuildTarget.StandaloneWindows64
        };

        var report = BuildPipeline.BuildPlayer(options);

        if (report.summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"Build successful - Build written to {options.locationPathName}");
            EditorApplication.Exit(0);
        }
        else if (report.summary.result == BuildResult.Failed)
        {
            Debug.LogError($"Build failed");
            EditorApplication.Exit(1);
        }
    }
}