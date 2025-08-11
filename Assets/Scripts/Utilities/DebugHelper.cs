using UnityEngine;

/// <summary>
/// Simple debugging helper for development. Centralized debug logging with toggles.
/// Keeps debug noise controllable and categorized.
/// </summary>
public static class DebugHelper {
    
    // Debug toggles - can be set from inspector or code
    public static bool enableCombatLogs = true;
    public static bool enableStateLogs = true;
    public static bool enableManagerLogs = true;
    public static bool enableInputLogs = false; // Usually too noisy
    
    public static void LogCombat(string message) {
        if (enableCombatLogs) Debug.Log($"[COMBAT] {message}");
    }
    
    public static void LogState(string message) {
        if (enableStateLogs) Debug.Log($"[STATE] {message}");
    }
    
    public static void LogManager(string message) {
        if (enableManagerLogs) Debug.Log($"[MANAGER] {message}");
    }
    
    public static void LogInput(string message) {
        if (enableInputLogs) Debug.Log($"[INPUT] {message}");
    }
    
    public static void LogWarning(string message) {
        Debug.LogWarning($"[WARNING] {message}");
    }
    
    public static void LogError(string message) {
        Debug.LogError($"[ERROR] {message}");
    }
    
    // Quick helper for development - always logs regardless of flags
    public static void LogForce(string message) {
        Debug.Log($"[FORCE] {message}");
    }
}