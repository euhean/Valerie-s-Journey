using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles attack timing and combo logic. Subscribes to the basic-attack input
/// and uses TimeManager for on-beat checks. Works with Weapon to deal damage.
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
    #region Inspector

    [Header("Attack Timing")]
    public float attackCooldown = GameConstants.ATTACK_COOLDOWN; // min time between presses

    [Header("Strong Attack Options")]
    [Tooltip("If true, aiming will be locked for 'strongLockDuration' seconds when a strong attack triggers.")]
    public bool strongLocksAiming = false;

    [Tooltip("Seconds to lock aiming when a strong attack occurs (only if strongLocksAiming==true).")]
    public float strongLockDuration = 0.25f;

    [Header("Combo Decay (optional)")]
    [Tooltip("If > 0, the combo will auto-reset after this many beats without on-beat presses.")]
    public int comboDecayBeats = 0;

    #endregion

    #region Private State

    private double lastAttackDSP = -9999.0;
    private int onBeatStreak = 0;
    private int beatsSinceLastPress = 0;

    private InputManager registeredInput;
    private TimeManager registeredTime;
    private Weapon playerWeapon;

    private bool isRegistered = false;
    private bool isStrongActive = false;

    #endregion

    #region Events

    public event Action<int> OnComboProgress; // current streak (0..N)
    public event Action OnStrongAttackTriggered;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        playerWeapon = GetComponentInChildren<Weapon>(true);
        if (playerWeapon == null)
            DebugHelper.LogWarning("PlayerAttackController: No Weapon component found in children!");
    }

    #endregion

    #region Registration

    /// <summary>
    /// Safe, idempotent registration. If input/time are null it defers subscription until provided.
    /// </summary>
    public void Register(InputManager input, TimeManager time)
    {
        // if already registered to same pair -> noop
        if (isRegistered && registeredInput == input && registeredTime == time) return;

        // unsubscribe previous if any
        if (isRegistered && registeredInput != null)
        {
            try { registeredInput.OnBasicPressedDSP -= HandleAttackInput; } catch { }
            if (registeredTime != null) registeredTime.OnBeat -= HandleBeat;
            isRegistered = false;
        }

        registeredInput = input;
        registeredTime  = time;

        if (registeredInput != null && registeredTime != null)
        {
            registeredInput.OnBasicPressedDSP += HandleAttackInput;
            registeredTime.OnBeat += HandleBeat;
            isRegistered = true;
            DebugHelper.LogManager("PlayerAttackController: Registered to input/time.");
        }
        else
        {
            DebugHelper.LogManager("PlayerAttackController: Deferred registration (missing input/time).");
        }
    }

    /// <summary>
    /// Unregisters if the passed input matches what we subscribed to. Safe to call multiple times.
    /// </summary>
    public void Unregister(InputManager input)
    {
        if (!isRegistered || registeredInput == null) return;
        if (registeredInput != input) return;

        try { registeredInput.OnBasicPressedDSP -= HandleAttackInput; } catch { }
        if (registeredTime != null) registeredTime.OnBeat -= HandleBeat;

        registeredInput = null;
        registeredTime  = null;
        isRegistered    = false;

        DebugHelper.LogManager("PlayerAttackController: Unregistered.");
    }

    #endregion

    #region Beat Handling

    private void HandleBeat(int beatIndex)
    {
        if (comboDecayBeats <= 0) return;
        beatsSinceLastPress++;
        if (beatsSinceLastPress >= comboDecayBeats)
        {
            ResetCombo();
            beatsSinceLastPress = 0;
        }
    }

    #endregion

    #region Input Handling

    private void HandleAttackInput(double dspTime)
    {
        if (registeredTime == null) return;

        // cooldown
        if (dspTime - lastAttackDSP < attackCooldown) return;
        lastAttackDSP = dspTime;

        // ignore while strong lock active
        if (isStrongActive) return;

        beatsSinceLastPress = 0;

        bool onBeat = registeredTime.IsOnBeat(dspTime);
        if (!onBeat)
        {
            onBeatStreak = 0;
            OnComboProgress?.Invoke(onBeatStreak);
            DebugHelper.LogCombat("Off-beat attack - combo reset to 0");
            BasicAttack(false);
            return;
        }

        // on-beat
        onBeatStreak++;
        OnComboProgress?.Invoke(Mathf.Min(onBeatStreak, GameConstants.COMBO_STREAK_FOR_STRONG));

        if (onBeatStreak >= GameConstants.COMBO_STREAK_FOR_STRONG)
        {
            onBeatStreak = 0;
            OnComboProgress?.Invoke(onBeatStreak);
            DebugHelper.LogCombat(() => $"STRONG ATTACK triggered after {GameConstants.COMBO_STREAK_FOR_STRONG}-hit combo!");
            StrongAttack();
            OnStrongAttackTriggered?.Invoke();
            return;
        }

        BasicAttack(true);
    }

    #endregion

    #region Attacks

    private void BasicAttack(bool onBeat)
    {
        if (playerWeapon == null) return;
        playerWeapon.PerformAttack(false);
        AnimationHelper.ShowAttack(playerWeapon.transform.position);
        if (!onBeat) DebugHelper.LogCombat("Off-beat basic attack - no combo progression");
    }

    private void StrongAttack()
    {
        if (playerWeapon == null) return;

        if (strongLocksAiming && !isStrongActive)
        {
            isStrongActive = true;
            StartCoroutine(StrongLockCoroutine(strongLockDuration));
        }

        playerWeapon.PerformAttack(true);
        AnimationHelper.ShowStrongAttack(playerWeapon.transform.position);
    }

    private IEnumerator StrongLockCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        isStrongActive = false;
    }

    #endregion

    #region Public Controls

    public void ResetCombo()
    {
        onBeatStreak = 0;
        beatsSinceLastPress = 0;
        OnComboProgress?.Invoke(onBeatStreak);
    }

    public void AbortAttack()
    {
        isStrongActive = false;
        StopAllCoroutines();
    }

    public void CancelCurrentAttack() => AbortAttack();
    public int CurrentComboStreak => onBeatStreak;

    #endregion
}