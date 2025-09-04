using UnityEngine;
using System.Text.RegularExpressions;

/// <summary>
/// Debug Log Cleanup Utility
/// Automatically removes or converts debug logs for production builds
/// </summary>
public static class DebugLogCleanup
{
    /// <summary>
    /// Remove all Debug.Log statements from a script content
    /// </summary>
    public static string RemoveDebugLogs(string scriptContent)
    {
        // Pattern to match Debug.Log statements (including variations)
        string pattern = @"Debug\.Log(?:Warning|Error)?\s*\(\s*[""'][^""']*[""']\s*\)\s*;";

        // Replace with empty string or conditional compilation
        string replacement = "";

        return Regex.Replace(scriptContent, pattern, replacement, RegexOptions.Multiline);
    }

    /// <summary>
    /// Convert Debug.Log to conditional logging
    /// </summary>
    public static string ConvertToConditionalLogging(string scriptContent)
    {
        // Replace Debug.Log with PerformanceUtils.Log
        scriptContent = Regex.Replace(scriptContent,
            @"Debug\.Log\s*\(\s*\$?""([^$""]*)""\s*\)",
            "PerformanceUtils.Log(\"$1\")");

        // Replace Debug.LogWarning with PerformanceUtils.LogWarning
        scriptContent = Regex.Replace(scriptContent,
            @"Debug\.LogWarning\s*\(\s*\$?""([^$""]*)""\s*\)",
            "PerformanceUtils.LogWarning(\"$1\")");

        // Replace Debug.LogError with PerformanceUtils.LogError
        scriptContent = Regex.Replace(scriptContent,
            @"Debug\.LogError\s*\(\s*\$?""([^$""]*)""\s*\)",
            "PerformanceUtils.LogError(\"$1\")");

        return scriptContent;
    }

    /// <summary>
    /// Add conditional compilation blocks around debug code
    /// </summary>
    public static string AddConditionalCompilation(string scriptContent)
    {
        // Add #if DEVELOPMENT_BUILD around debug blocks
        string pattern = @"(Debug\.Log.*;)";
        string replacement = "#if DEVELOPMENT_BUILD\n        $1\n        #endif";

        return Regex.Replace(scriptContent, pattern, replacement, RegexOptions.Multiline);
    }

    /// <summary>
    /// Get statistics about debug logs in a script
    /// </summary>
    public static (int totalLogs, int logStatements, int logWarnings, int logErrors) GetDebugLogStats(string scriptContent)
    {
        int logStatements = Regex.Matches(scriptContent, @"Debug\.Log\s*\(").Count;
        int logWarnings = Regex.Matches(scriptContent, @"Debug\.LogWarning\s*\(").Count;
        int logErrors = Regex.Matches(scriptContent, @"Debug\.LogError\s*\(").Count;

        return (logStatements + logWarnings + logErrors, logStatements, logWarnings, logErrors);
    }
}

/// <summary>
/// Editor utility for batch debug log cleanup
/// </summary>
#if UNITY_EDITOR
public class DebugLogCleanupWindow : UnityEditor.EditorWindow
{
    [UnityEditor.MenuItem("Tools/Debug Log Cleanup")]
    static void ShowWindow()
    {
        GetWindow<DebugLogCleanupWindow>("Debug Log Cleanup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Debug Log Cleanup Utility", UnityEditor.EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("Scan Project for Debug Logs"))
        {
            ScanProjectForDebugLogs();
        }

        if (GUILayout.Button("Clean Debug Logs (Safe Mode)"))
        {
            CleanDebugLogsSafe();
        }

        if (GUILayout.Button("Convert to Conditional Logging"))
        {
            ConvertToConditionalLogging();
        }
    }

    private void ScanProjectForDebugLogs()
    {
        string[] scriptFiles = System.IO.Directory.GetFiles("Assets", "*.cs", System.IO.SearchOption.AllDirectories);
        int totalLogs = 0;

        foreach (string file in scriptFiles)
        {
            string content = System.IO.File.ReadAllText(file);
            var stats = DebugLogCleanup.GetDebugLogStats(content);

            if (stats.totalLogs > 0)
            {
                UnityEngine.Debug.Log($"📊 {System.IO.Path.GetFileName(file)}: {stats.totalLogs} debug logs " +
                    $"({stats.logStatements} Log, {stats.logWarnings} Warning, {stats.logErrors} Error)");
                totalLogs += stats.totalLogs;
            }
        }

        UnityEngine.Debug.Log($"🎯 Total debug logs found: {totalLogs}");
    }

    private void CleanDebugLogsSafe()
    {
        // Implementation for safe cleanup
        UnityEngine.Debug.Log("🔧 Safe cleanup would go here...");
    }

    private void ConvertToConditionalLogging()
    {
        // Implementation for conversion
        UnityEngine.Debug.Log("🔄 Conditional logging conversion would go here...");
    }
}
#endif
