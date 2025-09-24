using UnityEngine;

[CreateAssetMenu(fileName = "DebugConfig", menuName = "Config/Debug", order = 99)]
public class DebugConfig : ScriptableObject
{
    [Header("Global toggles (for dev/editor builds)")]
    public bool enableLogs = true;
    public bool enableStateLogs = false;
    public bool enableCombatLogs = true;
    public bool enableManagerLogs = true;
}