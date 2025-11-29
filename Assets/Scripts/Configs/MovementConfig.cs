// Assets/Scripts/Configs/MovementConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "MovementConfig", menuName = "Config/Movement", order = 2)]
public class MovementConfig : ScriptableObject
{
    [Header("Movement Tuning")]
    public float playerSpeed = 2f;
    public float playerAcceleration = 8f;
    public float playerDeceleration = 18f;
    public float playerReverseBrake = 30f;

    [Header("Deadzone (hysteresis)")]
    public float deadzoneEnter = 0.08f;
    public float deadzoneExit  = 0.12f;

    [Header("Stop Threshold")]
    [Tooltip("Velocity magnitude below which the player snaps to a complete stop")]
    public float stopThreshold = 0.01f;
}