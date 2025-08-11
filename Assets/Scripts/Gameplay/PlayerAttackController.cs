using UnityEngine;

/// <summary>
/// Handles attack timing and combo logic. It subscribes to the basic attack
/// input and uses the TimeManager to decide if hits are on-beat.
/// Works with the Weapon component to deal actual damage.
/// </summary>
public class PlayerAttackController : MonoBehaviour {
    [Header("Attack Timing")]
    public float attackCooldown = GameConstants.ATTACK_COOLDOWN; // minimum time between presses

    private double lastAttackDSP;
    private int    onBeatStreak;

    private InputManager inputManager;
    private TimeManager  timeManager;
    private Weapon playerWeapon;

    private void Awake() {
        // Find weapon component in the player hierarchy
        playerWeapon = GetComponentInChildren<Weapon>();
        if (playerWeapon == null) {
            DebugHelper.LogWarning("PlayerAttackController: No Weapon component found in children!");
        }
    }

    // Called by Player when enabling
    public void Register(InputManager input, TimeManager time) {
        inputManager = input;
        timeManager  = time;
        if (inputManager != null) {
            inputManager.OnBasicPressedDSP += HandleAttackInput;
        }
    }

    // Called by Player when disabling
    public void Unregister(InputManager input) {
        if (input != null) {
            input.OnBasicPressedDSP -= HandleAttackInput;
        }
    }

    private void HandleAttackInput(double dspTime) {
        // Ignore if no rhythm manager or double-tap too soon
        if (timeManager == null || dspTime - lastAttackDSP < attackCooldown) {
            return;
        }
        lastAttackDSP = dspTime;

        // Check on-beat timing
        bool onBeat = timeManager.IsOnBeat(dspTime);
        if (!onBeat) {
            onBeatStreak = 0;
            DebugHelper.LogCombat($"Off-beat attack - combo reset to 0");
            BasicAttack(false);
            return;
        }

        // Valid on-beat press: increment streak and fire strong on every 4th
        onBeatStreak++;
        
        // Show combo progress (only if we have a streak going)
        if (onBeatStreak > 1) {
            AnimationHelper.ShowCombo(transform.position, Mathf.Min(onBeatStreak, 4));
        }
        
        if (onBeatStreak >= 4) {
            DebugHelper.LogCombat($"STRONG ATTACK triggered after 4-hit combo!");
            onBeatStreak = 0;
            StrongAttack();
        } else {
            BasicAttack(true);
        }
    }

    private void BasicAttack(bool onBeat) {
        // Use weapon to perform actual attack
        if (playerWeapon != null) {
            playerWeapon.PerformAttack(false); // false = basic attack
            
            // Show different feedback based on timing accuracy
            if (onBeat) {
                AnimationHelper.ShowAttack(playerWeapon.transform.position);
            } else {
                // Off-beat attacks could have different visual feedback or reduced effectiveness
                AnimationHelper.ShowAttack(playerWeapon.transform.position);
                DebugHelper.LogCombat("Off-beat basic attack - no combo progression");
            }
        }
    }

    private void StrongAttack() {
        // Use weapon to perform strong attack
        if (playerWeapon != null) {
            playerWeapon.PerformAttack(true); // true = strong attack
            
            // Show strong attack feedback for alpha testing
            AnimationHelper.ShowStrongAttack(playerWeapon.transform.position);
        }
    }
}