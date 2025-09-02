using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles attack timing and combo logic. Subscribes to the basic-attack input
/// and uses TimeManager for on-beat checks. Works with Weapon to deal damage.
///
/// Improvements included:
/// - idempotent Register/Unregister (safe to call multiple times)
/// - events for combo progress and strong attack trigger
/// - optional strong-aim locking during strong attack
/// - optional combo decay based on beats without presses
/// - ResetCombo() and AbortAttack() public APIs
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
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

    // runtime state
    private double lastAttackDSP = -9999.0;
    private int onBeatStreak = 0;
    private int beatsSinceLastPress = 0;

    private InputManager registeredInput;
    private TimeManager registeredTime;
    private Weapon playerWeapon;

    private bool isRegistered = false;
    private bool isStrongActive = false;

    // Events for external UI/animation
    public event Action<int> OnComboProgress;    // current streak (0..N)
    public event Action OnStrongAttackTriggered;

    private void Awake()
    {
        playerWeapon = GetComponentInChildren<Weapon>(true);
        if (playerWeapon == null)
            DebugHelper.LogWarning("PlayerAttackController: No Weapon component found in children!");
    }

    /// <summary>
    /// Safe, idempotent registration. If input/time are null it will defer subscription until they're provided.
    /// </summary>
    public void Register(InputManager input, TimeManager time)
    {
        // If same pair already registered -> noop
        if (isRegistered && registeredInput == input && registeredTime == time) return;

        // Unsubscribe previous if any
        if (isRegistered && registeredInput != null)
        {
            try { registeredInput.OnBasicPressedDSP -= HandleAttackInput; }
            catch { }
            if (registeredTime != null) registeredTime.OnBeat -= HandleBeat;
            isRegistered = false;
        }

        // store refs (may be null)
        registeredInput = input;
        registeredTime = time;

        // Subscribe only when both are present
        if (registeredInput != null && registeredTime != null)
        {
            registeredInput.OnBasicPressedDSP += HandleAttackInput;
            registeredTime.OnBeat += HandleBeat;
            isRegistered = true;
            DebugHelper.LogManager("PlayerAttackController: Registered to input/time.");
        }
        else DebugHelper.LogManager("PlayerAttackController: Deferred registration (missing input/time).");
    }

    /// <summary>
    /// Unregisters the current subscription if the passed input matches what we subscribed to.
    /// Safe to call multiple times.
    /// </summary>
    public void Unregister(InputManager input)
    {
        if (!isRegistered) return;
        if (registeredInput == null) return;

        if (registeredInput == input)
        {
            try { registeredInput.OnBasicPressedDSP -= HandleAttackInput; }
            catch { }
            if (registeredTime != null) registeredTime.OnBeat -= HandleBeat;
            registeredInput = null;
            registeredTime = null;
            isRegistered = false;
            DebugHelper.LogManager("PlayerAttackController: Unregistered.");
        }
    }

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

    private void HandleAttackInput(double dspTime)
    {
        // Defensive checks
        if (registeredTime == null) return;

        // cooldown (DSP-based)
        if (dspTime - lastAttackDSP < attackCooldown) return;
        lastAttackDSP = dspTime;

        // ignore presses while strong attack locked
        if (isStrongActive) return;

        // reset beat decay counter
        beatsSinceLastPress = 0;

        bool onBeat = registeredTime.IsOnBeat(dspTime);
        if (!onBeat)
        {
            // off-beat: immediate combo reset, still perform basic attack
            onBeatStreak = 0;
            OnComboProgress?.Invoke(onBeatStreak);
            DebugHelper.LogCombat("Off-beat attack - combo reset to 0");
            BasicAttack(false);
            return;
        }

        // on-beat -> progress combo
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
        // Keep isStrongActive true during lock, preventing further presses
        yield return new WaitForSeconds(duration);
        isStrongActive = false;
    }

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
}