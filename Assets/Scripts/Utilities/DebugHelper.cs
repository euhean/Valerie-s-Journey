using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple debugging helper for development. Centralized debug logging with toggles.
/// - Keeps API backward-compatible with the previous DebugHelper.
/// - Adds lazy-eval overloads, timestamp option, and "log once" helpers.
/// </summary>
public static class DebugHelper
{
    #region Flags (toggle at runtime from code or via tiny DebugSettings MonoBehaviour if you add one)
    public static bool enableCombatLogs = true;
    public static bool enableStateLogs = true;
    public static bool enableManagerLogs = true;
    public static bool enableInputLogs = false; // Usually too noisy

    /// <summary>Include a short timestamp prefix (realtime seconds) on logs when true.</summary>
    public static bool includeTimestamps = false;

    // ----- Internal "log once" tracking to avoid spam -----
    private static readonly HashSet<string> _loggedOnceKeys = new HashSet<string>();

    #endregion

    #region Utility

    private static string Format(string category, string message)
    {
        if (includeTimestamps)
            return $"[{Time.realtimeSinceStartup:0000.000}] [{category}] {message}";
        return $"[{category}] {message}";
    }

    private static void DoLog(string formatted) => Debug.Log(formatted);
    private static void DoWarning(string formatted) => Debug.LogWarning(formatted);
    private static void DoError(string formatted) => Debug.LogError(formatted);

    #endregion

    #region Public API (string)

    public static void LogCombat(string message)
    {
        if (!enableCombatLogs) return;
        DoLog(Format("COMBAT", message));
    }

    public static void LogState(string message)
    {
        if (!enableStateLogs) return;
        DoLog(Format("STATE", message));
    }

    public static void LogManager(string message)
    {
        if (!enableManagerLogs) return;
        DoLog(Format("MANAGER", message));
    }

    public static void LogInput(string message)
    {
        if (!enableInputLogs) return;
        DoLog(Format("INPUT", message));
    }

    public static void LogWarning(string message) => DoWarning(Format("WARNING", message));
    public static void LogError(string message) => DoError(Format("ERROR", message));
    public static void LogForce(string message) => DoLog(Format("FORCE", message));

    #endregion

    #region Lazy-eval overloads (avoid allocations when disabled)

    public static void LogCombat(Func<string> lazyMessage)
    {
        if (enableCombatLogs && lazyMessage != null) Debug.Log($"[COMBAT] {lazyMessage()}");
    }
    public static void LogState(Func<string> lazyMessage)
    {
        if (enableStateLogs && lazyMessage != null) Debug.Log($"[STATE] {lazyMessage()}");
    }
    public static void LogManager(Func<string> lazyMessage)
    {
        if (enableManagerLogs && lazyMessage != null) Debug.Log($"[MANAGER] {lazyMessage()}");
    }
    public static void LogInput(Func<string> lazyMessage)
    {
        if (enableInputLogs && lazyMessage != null) Debug.Log($"[INPUT] {lazyMessage()}");
    }

    #endregion

    #region Log-once helpers

    /// <summary>Logs a warning once for the given key.</summary>
    public static void LogWarningOnce(string key, string message)
    {
        if (string.IsNullOrEmpty(key)) { LogWarning(message); return; }
        if (_loggedOnceKeys.Add(key)) LogWarning(message);
    }

    /// <summary>Logs an error once for the given key.</summary>
    public static void LogErrorOnce(string key, string message)
    {
        if (string.IsNullOrEmpty(key)) { LogError(message); return; }
        if (_loggedOnceKeys.Add(key)) LogError(message);
    }

    /// <summary>Logs a normal message once for the given key.</summary>
    public static void LogOnce(string key, string message)
    {
        if (string.IsNullOrEmpty(key)) { LogForce(message); return; }
        if (_loggedOnceKeys.Add(key)) LogForce(message);
    }

    /// <summary>Clear the "log once" memory (useful between scenes/tests).</summary>
    public static void ClearLogOnce() => _loggedOnceKeys.Clear();

    #endregion

    #region Small convenience: toggle groups

    public static void SetAllDebug(bool on)
    {
        enableCombatLogs = on;
        enableStateLogs = on;
        enableManagerLogs = on;
        enableInputLogs = on;
    }
    #endregion
}