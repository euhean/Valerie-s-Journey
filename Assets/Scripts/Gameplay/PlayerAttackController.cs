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

    [Header("Attack Window Auto-Tune")]
    [Tooltip("Derive attack window from BPM instead of a fixed CombatConfig value.")]
    public bool scaleAttackWindowWithTempo = true;
    [Tooltip("Smallest allowed window once scaling is applied.")]
    public float minScaledWindow = 0.18f;
    [Tooltip("Largest allowed window once scaling is applied.")]
    public float maxScaledWindow = 0.28f;
    [Tooltip("Extra seconds removed from the beat spacing to give the player time to re-aim.")]
    public float reactionBuffer = 0.08f;
    #endregion

    #region Private State
    private bool isAimLocked = false;
    private bool attackInProgress = false;

    private double lastPressDSP = -9999.0;
    private int onBeatStreak = 0;
    private int beatsSinceLastOnBeatPress = 0;
    private Coroutine strongAimLockCoro;
    private Vector2 lastAimDirection = Vector2.down;
    private bool hasAimDirectionSample = false;
    private const float AIM_DIR_THRESHOLD_SQR = GameConstants.WEAPON_AIM_THRESHOLD * GameConstants.WEAPON_AIM_THRESHOLD;
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

        if (beatConfig != null)
        {
             DebugHelper.LogManager($"[PAC] BeatConfig loaded. ResetInactivity: {beatConfig.resetOnInactivityBeats}, ResetOffBeat: {beatConfig.resetOnOffBeat}");
        }
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
            // Use > instead of >= to be more forgiving of race conditions (late hits)
            // This effectively adds 1 beat of grace period.
            if (beatsSinceLastOnBeatPress > beatConfig.resetOnInactivityBeats)
            {
                ResetCombo($"inactivity: {beatsSinceLastOnBeatPress} > {beatConfig.resetOnInactivityBeats}");
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
            
            int requiredStreak = combatConfig != null ? combatConfig.comboStreak : GameConstants.COMBO_STREAK_FOR_STRONG;
            if (onBeatStreak >= requiredStreak)
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

        float windowSec = ResolveAttackWindowSeconds();

        // Lock aim for strong only during the window
        if (isStrong)
        {
            isAimLocked = true;
            if (strongAimLockCoro != null) StopCoroutine(strongAimLockCoro);
            strongAimLockCoro = StartCoroutine(UnlockAimAfter(windowSec));
            
            // Enlarge sprite for strong attack
            StartCoroutine(EnlargeSpriteRoutine(windowSec));
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

        if (isStrong) ResetCombo("strong attack complete");

        attackInProgress = false;
    }

    private float ResolveAttackWindowSeconds()
    {
        float fallback = combatConfig != null ? combatConfig.attackWindow : GameConstants.ATTACK_VISUAL_DURATION;
        if (!scaleAttackWindowWithTempo || timeManager == null)
        {
            return fallback;
        }

        float bpm = Mathf.Max(1f, timeManager.bpm);
        float secondsPerBeat = 60f / bpm;
        float onBeatTol = beatConfig != null ? Mathf.Abs(beatConfig.onBeatWindowSec) : 0.07f;
        float buffer = Mathf.Max(0f, reactionBuffer);

        float computed = secondsPerBeat - (2f * onBeatTol) - buffer;
        if (!float.IsFinite(computed))
        {
            return fallback;
        }

        float min = Mathf.Max(0.05f, minScaledWindow);
        float max = Mathf.Max(min, maxScaledWindow);
        return Mathf.Clamp(computed, min, max);
    }

    private IEnumerator EnlargeSpriteRoutine(float duration)
    {
        Transform visualTransform = transform; // Assuming script is on the root with the sprite
        Vector3 originalScale = visualTransform.localScale;
        Vector3 targetScale = originalScale * 2f;

        // Scale Up
        visualTransform.localScale = targetScale;

        yield return new WaitForSeconds(duration);

        // Scale Down
        visualTransform.localScale = originalScale;
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

        // Ensure weapon stops collecting hits immediately
        if (weapon != null)
        {
            weapon.AbortAttack();
        }
    }

    public void ResetCombo(string reason)
    {
        if (onBeatStreak != 0) DebugHelper.LogCombat($"[PAC] Combo reset ({reason}).");
        onBeatStreak = 0;
        beatsSinceLastOnBeatPress = 0;
    }

    /// <summary>
    /// Stores the latest aim direction so other systems can align visuals to weapon aim.
    /// </summary>
    public void RegisterAimDirection(Vector2 aimDir)
    {
        if (aimDir.sqrMagnitude < AIM_DIR_THRESHOLD_SQR)
        {
            return;
        }

        lastAimDirection = aimDir.normalized;
        hasAimDirectionSample = true;
    }

    /// <summary>
    /// Provides the cached aim direction, if the player has aimed at least once.
    /// </summary>
    public bool TryGetAimDirection(out Vector2 aimDir)
    {
        aimDir = lastAimDirection;
        return hasAimDirectionSample;
    }
    #endregion
}
