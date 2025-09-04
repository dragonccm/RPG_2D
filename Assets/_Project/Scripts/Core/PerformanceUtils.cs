using System.Text;
using UnityEngine;

/// <summary>
/// Performance optimization utilities
/// </summary>
public static class PerformanceUtils
{
    private static readonly StringBuilder stringBuilder = new StringBuilder();
    private static bool developmentLoggingEnabled = true;

    /// <summary>
    /// Enable or disable development logging
    /// </summary>
    public static void EnableDevelopmentLogging(bool enable)
    {
        developmentLoggingEnabled = enable;
    }

    /// <summary>
    /// Efficient string formatting without memory allocation
    /// </summary>
    public static string FormatString(string format, params object[] args)
    {
        stringBuilder.Length = 0;
        stringBuilder.AppendFormat(format, args);
        return stringBuilder.ToString();
    }

    /// <summary>
    /// Cached string for common debug messages
    /// </summary>
    public static class CachedStrings
    {
        public static readonly string PlayerNotFound = "Player not found!";
        public static readonly string ComponentMissing = "Component missing!";
        public static readonly string SystemInitialized = "System initialized successfully!";
        public static readonly string SystemDisabled = "System disabled";
        public static readonly string SystemEnabled = "System enabled";
        public static readonly string AnimationTriggered = "Animation triggered";
        public static readonly string SkillExecuted = "Skill executed";
        public static readonly string DamageDealt = "Damage dealt";
        public static readonly string TargetDestroyed = "Target destroyed";
    }

    /// <summary>
    /// Conditional logging - only logs in development builds
    /// </summary>
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        if (developmentLoggingEnabled)
        {
            Debug.Log(message);
        }
    }

    /// <summary>
    /// Conditional logging with format
    /// </summary>
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogFormat(string format, params object[] args)
    {
        if (developmentLoggingEnabled)
        {
            Debug.LogFormat(format, args);
        }
    }

    /// <summary>
    /// Conditional warning
    /// </summary>
    [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        if (developmentLoggingEnabled)
        {
            Debug.LogWarning(message);
        }
    }

    /// <summary>
    /// Conditional error - always logs
    /// </summary>
    public static void LogError(string message)
    {
        Debug.LogError(message);
    }
}
