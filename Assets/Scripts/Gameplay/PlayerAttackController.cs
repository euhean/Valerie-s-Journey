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
    [Header("Manager Refs (wired by GameManager.RegisterPlayer)")]
    public TimeManager timeManager; // queried for on-beat judgement

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
    private int lastOnBeatIndex = -1; // beat index of the latest on-beat press

    private int ComboStreak => combatConfig != null ? combatConfig.comboStreak : GameConstants.COMBO_STREAK_FOR_STRONG;
    private int InactivityBeats => beatConfig != null ? beatConfig.resetOnInactivityBeats : 0;
    private bool ResetOnOffBeat => beatConfig == null || beatConfig.resetOnOffBeat;
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

    // The EventBus always exists, so subscribing here needs no manager reference
    // and works the same for a player placed in the scene or spawned at runtime.
    private void OnEnable()
    {
        EventBus.Instance.Subscribe<BasicPressedEvent>(HandleBasicPressed);
        EventBus.Instance.Subscribe<BeatTickEvent>(HandleBeat);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<BasicPressedEvent>(HandleBasicPressed);
        EventBus.Instance.Unsubscribe<BeatTickEvent>(HandleBeat);
    }
    #endregion

    #region Input & Combo
    // Combo rules, counted in beat indices:
    // - An on-beat press extends the streak. Reaching ComboStreak makes that attack strong.
    // - Skipping InactivityBeats or more beats between on-beat presses restarts the streak.
    // - An off-beat press still attacks, and resets the streak when ResetOnOffBeat is set.

    private void HandleBeat(BeatTickEvent e)
    {
        if (onBeatStreak == 0 || InactivityBeats <= 0) return;

        // When beat N ticks, the window of beat N-1 has closed (the window of N is still open).
        int missedBeats = (e.beatIndex - 1) - lastOnBeatIndex;
        if (missedBeats >= InactivityBeats) ResetCombo("inactivity");
    }

    private void HandleBasicPressed(BasicPressedEvent e)
    {
        if (attackInProgress) return; // respect the current attack window
        if (e.dspTime - lastPressDSP < attackCooldown) return; // basic anti-spam
        lastPressDSP = e.dspTime;

        bool onBeat = timeManager.TryGetBeatAt(e.dspTime, out int beatIndex);
        bool isStrong = false;

        if (onBeat && beatIndex != lastOnBeatIndex) // a second press on the same beat adds nothing
        {
            int missedBeats = beatIndex - lastOnBeatIndex - 1;
            bool streakBroken = InactivityBeats > 0 && missedBeats >= InactivityBeats;

            lastOnBeatIndex = beatIndex;
            SetStreak(streakBroken ? 1 : onBeatStreak + 1);

            if (onBeatStreak >= ComboStreak)
            {
                isStrong = true;
                SetStreak(0); // the strong attack spends the combo
            }
        }
        else if (!onBeat && ResetOnOffBeat)
        {
            ResetCombo("off-beat");
        }

        StartCoroutine(DoAttackWindow(onBeat, isStrong));
    }

    private void SetStreak(int value)
    {
        if (onBeatStreak == value) return;
        onBeatStreak = value;
        EventBus.Instance.Publish(new ComboChangedEvent { streak = value, streakForStrong = ComboStreak });
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

        EventBus.Instance.Publish(new AttackStartedEvent
        {
            attacker = GetComponent<Entity>(),
            isStrong = isStrong,
            onBeat   = onBeat
        });

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

        // Ensure weapon stops collecting hits immediately
        if (weapon != null)
        {
            weapon.AbortAttack();
        }
    }

    public void ResetCombo(string reason)
    {
        if (onBeatStreak != 0) DebugHelper.LogCombat($"[PAC] Combo reset ({reason}).");
        SetStreak(0);
    }
    #endregion
}
