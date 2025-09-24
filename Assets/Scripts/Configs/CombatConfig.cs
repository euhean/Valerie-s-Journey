// Assets/Scripts/Config/CombatConfig.cs
using UnityEngine;

[CreateAssetMenu(fileName = "CombatConfig", menuName = "Config/Combat", order = 0)]
public class CombatConfig : ScriptableObject
{
    [Header("Damage")]
    public float basicDamage = 5f;
    public float strongDamage = 20f;

    [Header("Combo")]
    [Tooltip("On-beat streak needed to trigger strong attack")]
    public int comboStreak = 4;
    
    [Header("Attack Window (seconds)")]
    public float attackWindow = 0.20f;
}