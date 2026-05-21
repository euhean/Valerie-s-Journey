using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles attack timing, combo rules, aim lock during strong, and emits AttackResolved.
/// Minimal event pipeline: Weapon will emit DamageApplied per hit in the next step.
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
    #region Inspector
    [Header("Manager Refs")]
    public InputManager inputManager;
    public TimeManager timeManager;

    [Header("Configs")]
    public CombatConfig combatConfig; // basicDamage, strongDamage, comboStreak, attackWindow
    public BeatConfig beatConfig;     // onBeat window, reset policies

    [Header("Cooldowns")]
    [Tooltip("Seconds between button presses being accepted (prevents button mashing spam)")]
    public float attackCooldown = 0.15f;
    #endregion

    #region Private State
    private Weapon weapon;
    private bool isAimLocked = false;
    private bool attackInProgress = false;

    private bool isPaused = false;

    private double lastPressDSP = -9999.0;
    private int onBeatStreak = 0;
    private int beatsSinceLastOnBeatPress = 0;
    private Coroutine strongAimLockCoro;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        EventBus.Instance.Subscribe<BasicPressedEvent>(HandleBasicPressedDSP);
        EventBus.Instance.Subscribe<BeatTickEvent>(HandleBeat);
        EventBus.Instance.Subscribe<GameStateChangedEvent>(HandleStateChange);
    }

    private void OnDisable()
    {
        EventBus.Instance.Unsubscribe<BasicPressedEvent>(HandleBasicPressedDSP);
        EventBus.Instance.Unsubscribe<BeatTickEvent>(HandleBeat);
        EventBus.Instance.Unsubscribe<GameStateChangedEvent>(HandleStateChange);
    }

    private void HandleStateChange(GameStateChangedEvent e)
    {
        isPaused = (e.to != GameManager.GameState.Gameplay);
        if (isPaused) AbortAttack();
    }
    #endregion

    #region Subscription Management
    // <summary>
    // Subscribe to manager events. Idempotent - safe to call multiple times.
    // Called by OnEnable and by GameManager.RegisterPlayer after external wiring
    public void TrySubscribe()
    {
        if (subscribed) return;
        if (inputManager == null || timeManager == null) return;

        inputManager.OnBasicPressedDSP += HandleBasicPressedDSP;
        timeManager.OnBeat += HandleBeat;
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;

        if (inputManager != null) inputManager.OnBasicPressedDSP -= HandleBasicPressedDSP;
        if (timeManager != null)  timeManager.OnBeat -= HandleBeat;
        subscribed = false;
    }
    #endregion

    #region Input & Combo
    private void HandleBeat(BeatTickEvent _)
    {
        if (isPaused) return;
        if (beatConfig != null && beatConfig.resetOnInactivityBeats > 0)
        {
            beatsSinceLastOnBeatPress++;
            if (beatsSinceLastOnBeatPress >= beatConfig.resetOnInactivityBeats)
                ResetCombo("inactivity");
        }
    }

    private void HandleBasicPressedDSP(BasicPressedEvent e)
    {
        if (isPaused) return;
        double pressDSP = e.dspTime;
        
        if (attackInProgress) return; // respect the current attack window
        if (pressDSP - lastPressDSP < attackCooldown) return; // basic anti-spam
        lastPressDSP = pressDSP;

        bool onBeat = timeManager.IsOnBeat(pressDSP);
        bool isStrong = false;
        if (!onBeat)
            if (beatConfig == null || beatConfig.resetOnOffBeat) ResetCombo("off-beat");
        else
        {
            beatsSinceLastOnBeatPress = 0;
            onBeatStreak++;
            if (combatConfig != null && onBeatStreak >= combatConfig.comboStreak)
                isStrong = true;
        }

        StartCoroutine(DoAttackWindow(onBeat, isStrong));
    }
    #endregion

    #region Attack Window Coroutine
    private IEnumerator DoAttackWindow(bool onBeat, bool isStrong)
    {
        attackInProgress = true;

        float windowSec = combatConfig != null ? combatConfig.attackWindow : 0.20f;

        if (isStrong)
        {
            isAimLocked = true;
            if (strongAimLockCoro != null) StopCoroutine(strongAimLockCoro);
            strongAimLockCoro = StartCoroutine(UnlockAimAfter(windowSec));
        }

        // Tell weapon to open its active window; it will collect hits.
        // NOTE: Weapon will be replaced next to provide StartAttackWindow + ConsumeHitTargets.
        weapon.StartAttackWindow(isStrongAttack: isStrong, wasOnBeat: onBeat, windowSeconds: windowSec);

        yield return new WaitForSeconds(windowSec);

        // Consume hit targets from the weapon for this window
        IReadOnlyList<Entity> hits = weapon.ConsumeHitTargets();
        bool success = hits != null && hits.Count > 0;

        var evt = new AttackResolved(
            attacker: GetComponent<Entity>(),
            success: success,
            isStrong: isStrong,
            onBeat: onBeat,
            hitTargets: hits ?? (IReadOnlyList<Entity>)new List<Entity>(0),
            dspTime: AudioSettings.dspTime
        );
        EventBus.Instance.Publish(evt);

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

    /// <summary>Resets the combo streak (called when player takes damage).</summary>
    public void ResetCombo(string reason = "external")
    {
        if (onBeatStreak != 0) DebugHelper.LogCombat($"[PAC] Combo reset ({reason}).");
        onBeatStreak = 0;
        beatsSinceLastOnBeatPress = 0;
    }

    /// <summary>Aborts any ongoing attack and unlocks aim (called when player takes damage).</summary>
    public void AbortAttack()
    {
        if (attackInProgress)
        {
            attackInProgress = false;
            if (strongAimLockCoro != null)
            {
                StopCoroutine(strongAimLockCoro);
                strongAimLockCoro = null;
            }
            isAimLocked = false;
            DebugHelper.LogCombat("[PAC] Attack aborted due to damage.");
        }
    }
    #endregion
}
