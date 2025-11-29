using System;
using System.Diagnostics;
using UnityEngine;

/// <summary>
/// Lightweight debug helper:
/// - Calls are compile-stripped outside UNITY_EDITOR/DEVELOPMENT_BUILD (via Conditional).
/// - Runtime toggles read from DebugConfig (auto-loaded from Resources/DebugConfig).
/// - Always-available Error logging is not stripped so runtime errors still surface.
/// </summary>
public static class DebugHelper
{
    private static DebugConfig _config;

    /// <summary>Explicit assignment (optional).</summary>
    public static void AssignConfig(DebugConfig cfg) => _config = cfg;

    private static void EnsureConfig()
    {
        if (_config != null) return;
        // Try auto-load from Resources/DebugConfig (optional step)
        try
        {
            _config = Resources.Load<DebugConfig>("DebugConfig");
        }
        catch { _config = null; }
    }

    #region Compile-stripped logs (editor/dev only)

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(string message)
    {
        EnsureConfig();
        if (_config == null || _config.enableLogs) UnityEngine.Debug.Log(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Log(Func<string> messageProvider)
    {
        EnsureConfig();
        if (_config == null || _config.enableLogs) UnityEngine.Debug.Log(messageProvider());
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(string message)
    {
        EnsureConfig();
        if (_config == null || _config.enableLogs) UnityEngine.Debug.LogWarning(message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogWarning(Func<string> messageProvider)
    {
        EnsureConfig();
        if (_config == null || _config.enableLogs) UnityEngine.Debug.LogWarning(messageProvider());
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogManager(string message)
    {
        EnsureConfig();
        if (_config == null || _config.enableManagerLogs) UnityEngine.Debug.Log("[Manager] " + message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogManager(Func<string> provider)
    {
        EnsureConfig();
        if (_config == null || _config.enableManagerLogs) UnityEngine.Debug.Log("[Manager] " + provider());
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogCombat(string message)
    {
        EnsureConfig();
        if (_config == null || _config.enableCombatLogs) UnityEngine.Debug.Log("[Combat] " + message);
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogCombat(Func<string> provider)
    {
        EnsureConfig();
        if (_config == null || _config.enableCombatLogs) UnityEngine.Debug.Log("[Combat] " + provider());
    }

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void LogState(Func<string> provider)
    {
        EnsureConfig();
        if (StateLogsEnabled) UnityEngine.Debug.Log("[State] " + provider());
    }

    #endregion

    #region Always-on errors (not stripped)
    public static void LogError(string message)
    {
        UnityEngine.Debug.LogError(message);
    }

    public static void LogException(Exception ex)
    {
        UnityEngine.Debug.LogException(ex);
    }
    
    /// <summary>Check if state logs are enabled (for conditional logging).</summary>
    public static bool StateLogsEnabled
    {
        get
        {
            EnsureConfig();
            return _config == null || _config.enableStateLogs;
        }
    }
    #endregion
}