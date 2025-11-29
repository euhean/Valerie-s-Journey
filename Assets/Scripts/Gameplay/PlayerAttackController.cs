using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles attack timing, combo rules, aim lock during strong, and communicates with weapon.
/// This is a controller script that goes on the Player - it finds and controls a separate Weapon object.
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
    #region Inspector
    [Header("Manager Refs (assign or left to be fetched from GameManager)")]
    public InputManager inputManager;
    public TimeManager timeManager;

    [Header("Configs (ScriptableObjects)")]
    public CombatConfig combatConfig; // basicDamage, strongDamage, comboStreak, attackWindow
    public BeatConfig beatConfig;     // onBeat window, reset policies

    [Header("Weapon Reference (found automatically if null)")]
    public Weapon weapon; // The weapon this controller commands

    [Header("Cooldowns")]
    [Tooltip("Seconds between button presses being accepted (prevents button mashing spam)")]
    public float attackCooldown = 0.15f;
    #endregion

    #region Private State
    private bool isAimLocked = false;
    private bool attackInProgress = false;

    private double lastPressDSP = -9999.0;
    private int onBeatStreak = 0;
    private int beatsSinceLastOnBeatPress = 0;
    private Coroutine strongAimLockCoro;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // Use centralized weapon fallback logic
        weapon ??= ComponentHelper.FindWeaponFallback("PAC");
        
        // Configs are optional - GameConstants provide fallback values
        if (combatConfig == null) DebugHelper.LogManager("[PAC] Using GameConstants for combat values (CombatConfig not assigned).");
        if (beatConfig == null) DebugHelper.LogManager("[PAC] Using GameConstants for beat values (BeatConfig not assigned).");
    }

    private void Start()
    {
        // Acquire managers from GameManager if not set in inspector
        inputManager ??= GameManager.Instance?.inputManager;
        timeManager  ??= GameManager.Instance?.timeManager;
        
        if (inputManager == null) DebugHelper.LogError("[PAC] InputManager not found.");
        if (timeManager == null)  DebugHelper.LogError("[PAC] TimeManager not found.");
    }

    private void OnEnable()
    {
        if (inputManager != null) inputManager.OnBasicPressedDSP += HandleBasicPressedDSP;
        if (timeManager  != null) timeManager.OnBeat += HandleBeat;
    }

    private void OnDisable()
    {
        if (inputManager != null) inputManager.OnBasicPressedDSP -= HandleBasicPressedDSP;
        if (timeManager  != null) timeManager.OnBeat -= HandleBeat;
    }
    #endregion

    #region Input & Combo
    private void HandleBeat(int _)
    {
        // Beat-based combo inactivity
        if (beatConfig != null && beatConfig.resetOnInactivityBeats > 0)
        {
            beatsSinceLastOnBeatPress++;
            if (beatsSinceLastOnBeatPress >= beatConfig.resetOnInactivityBeats)
            {
                ResetCombo("inactivity");
            }
        }
    }

    private void HandleBasicPressedDSP(double pressDSP)
    {
        if (attackInProgress) return; // respect the current attack window
        if (pressDSP - lastPressDSP < attackCooldown) return; // basic anti-spam
        lastPressDSP = pressDSP;

        bool onBeat = timeManager != null && timeManager.IsOnBeat(pressDSP);

        // Combo rules:
        // - Off-beat press: allowed, but resets streak immediately (never contributes to strong)
        // - On-beat press: increments streak; if reaches comboStreak -> strong attack and reset afterwards
        bool isStrong = false;

        if (!onBeat)
        {
            if (beatConfig == null || beatConfig.resetOnOffBeat) ResetCombo("off-beat");
        }
        else
        {
            beatsSinceLastOnBeatPress = 0;
            onBeatStreak++;
            if (combatConfig != null && onBeatStreak >= combatConfig.comboStreak)
            {
                isStrong = true;
                // Reset streak after strong resolves (done at the end of the window)
            }
        }

        // Run the attack window
        StartCoroutine(DoAttackWindow(onBeat, isStrong));
    }
    #endregion

    #region Attack Window Coroutine
    private IEnumerator DoAttackWindow(bool onBeat, bool isStrong)
    {
        attackInProgress = true;

        float windowSec = combatConfig != null ? combatConfig.attackWindow : 0.20f;

        // Lock aim for strong only during the window
        if (isStrong)
        {
            isAimLocked = true;
            if (strongAimLockCoro != null) StopCoroutine(strongAimLockCoro);
            strongAimLockCoro = StartCoroutine(UnlockAimAfter(windowSec));
        }

        // Tell weapon to open its active window; it will collect hits.
        if (weapon != null)
        {
            weapon.StartAttackWindow(isStrong, windowSec);
        }

        // Wait for the window to finish
        yield return new WaitForSeconds(windowSec);

        // Consume hit targets from the weapon for this window
        IReadOnlyList<Entity> hits = weapon?.ConsumeHitTargets();

        bool success = hits != null && hits.Count > 0;

        // Emit AttackResolved (DamageApplied will be emitted per-hit by Weapon during the window)
        var evt = new AttackResolved(
            attacker: GetComponent<Entity>(),
            success: success,
            isStrong: isStrong,
            onBeat: onBeat,
            hitTargets: hits ?? (IReadOnlyList<Entity>)new List<Entity>(0),
            dspTime: AudioSettings.dspTime
        );
        EventBus.Instance.Publish(evt);

        // Reset strong combo after a strong attack fires
        if (isStrong) onBeatStreak = 0;

        attackInProgress = false;
    }

    private IEnumerator UnlockAimAfter(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        isAimLocked = false;
    }
    #endregion

    #region Public API (for movement/aim systems)
    /// <summary>Called by Player to set the weapon reference.</summary>
    public void SetWeapon(Weapon w) => weapon = w;

    /// <summary>Call from your aiming code to know if aim updates should be ignored.</summary>
    public bool IsAimLocked() => isAimLocked;
    
    /// <summary>Public method to abort attack (unlock aim immediately).</summary>
    public void AbortAttack()
    {
        if (strongAimLockCoro != null)
        {
            StopCoroutine(strongAimLockCoro);
            strongAimLockCoro = null;
        }
        isAimLocked = false;
        attackInProgress = false;
    }

    public void ResetCombo(string reason)
    {
        if (onBeatStreak != 0) DebugHelper.LogCombat($"[PAC] Combo reset ({reason}).");
        onBeatStreak = 0;
        beatsSinceLastOnBeatPress = 0;
    }
    #endregion
}
