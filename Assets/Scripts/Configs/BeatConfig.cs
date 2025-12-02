// Assets/Scripts/Configs/BeatConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "BeatConfig", menuName = "Config/Beat", order = 1)]
public class BeatConfig : ScriptableObject
{
    [Tooltip("Half-window in seconds considered 'on beat' (mirrors TimeManager)")]
    public float onBeatWindowSec = 0.07f;

    [Header("Combo Reset Rules")]
    [Tooltip("Reset combo if an attack is done off-beat")]
    public bool resetOnOffBeat = true;

    [Tooltip("Reset combo after this many beats without on-beat presses (0 = never). Set to 2 to allow 'hit every beat' without race conditions.")]
    public int resetOnInactivityBeats = 2;
}