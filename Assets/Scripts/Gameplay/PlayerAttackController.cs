using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns attack timing: judges each press against the beat, applies the combo rules,
/// and opens/closes the weapon's attack window. The Weapon only detects hits.
/// </summary>
public class PlayerAttackController : MonoBehaviour
{
    #region Inspector
    [Header("Manager Refs (wired by GameManager.RegisterPlayer)")]
    public TimeManager timeManager; // queried for on-beat judgement

    [Header("Configs (ScriptableObjects)")]
    public CombatConfig combatConfig; // comboStreak, attackWindow
    public BeatConfig beatConfig;     // reset policies

    [Header("Weapon (child of the player)")]
    public Weapon weapon;

    [Header("Cooldowns")]
    [Tooltip("Seconds between button presses being accepted (prevents button mashing spam)")]
    public float attackCooldown = 0.15f;
    #endregion

    #region Private State
    private Entity owner;
    private Coroutine attackRoutine; // non-null while an attack window is open
    private bool isAimLocked = false;

    private double lastPressDSP = -9999.0;
    private int onBeatStreak = 0;
    private int lastOnBeatIndex = -1; // beat index of the latest on-beat press

    private float AttackWindow => combatConfig != null ? combatConfig.attackWindow : GameConstants.ATTACK_VISUAL_DURATION;
    private int ComboStreak => combatConfig != null ? combatConfig.comboStreak : GameConstants.COMBO_STREAK_FOR_STRONG;
    private int InactivityBeats => beatConfig != null ? beatConfig.resetOnInactivityBeats : 0;
    private bool ResetOnOffBeat => beatConfig == null || beatConfig.resetOnOffBeat;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        owner = GetComponent<Entity>();
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
        AbortAttack(); // Unity stops our coroutines on disable; don't leave a window open
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
        if (!owner.IsAlive || !owner.IsOnDuty) return;
        if (attackRoutine != null) return; // respect the current attack window
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

        attackRoutine = StartCoroutine(DoAttackWindow(onBeat, isStrong));
    }

    private void SetStreak(int value)
    {
        if (onBeatStreak == value) return;
        onBeatStreak = value;
        EventBus.Instance.Publish(new ComboChangedEvent { streak = value, streakForStrong = ComboStreak });
    }
    #endregion

    #region Attack Window
    private IEnumerator DoAttackWindow(bool onBeat, bool isStrong)
    {
        // GDD: a strong attack cannot be redirected after it triggers; a basic one can.
        isAimLocked = isStrong;
        weapon.OpenAttackWindow(isStrong, onBeat);
        EventBus.Instance.Publish(new AttackStartedEvent { attacker = owner, isStrong = isStrong, onBeat = onBeat });

        yield return new WaitForSeconds(AttackWindow);

        IReadOnlyList<Entity> hits = weapon.CloseAttackWindow();
        isAimLocked = false;
        attackRoutine = null;

        EventBus.Instance.Publish(new AttackResolved(
            attacker: owner,
            success: hits.Count > 0,
            isStrong: isStrong,
            onBeat: onBeat,
            hitTargets: hits,
            dspTime: AudioSettings.dspTime
        ));
    }
    #endregion

    #region Public API
    /// <summary>Movement/aim code asks this before steering the weapon.</summary>
    public bool IsAimLocked() => isAimLocked;

    /// <summary>Cancels the open attack window, if any. No AttackResolved is published for it.</summary>
    public void AbortAttack()
    {
        if (attackRoutine == null) return;

        StopCoroutine(attackRoutine);
        attackRoutine = null;
        isAimLocked = false;
        if (weapon != null) weapon.CloseAttackWindow(); // the weapon may already be destroyed on scene unload
    }

    public void ResetCombo(string reason)
    {
        if (onBeatStreak != 0) DebugHelper.LogCombat($"[PAC] Combo reset ({reason}).");
        SetStreak(0);
    }
    #endregion
}
