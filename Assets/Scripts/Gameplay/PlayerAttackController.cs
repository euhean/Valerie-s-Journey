using UnityEngine;

/// <summary>
/// Handles attack timing and combo logic. It subscribes to the basic attack
/// input and uses the TimeManager to decide if hits are on-beat.
/// </summary>
public class PlayerAttackController : MonoBehaviour {
    [Header("Attack Timing")]
    public float attackCooldown = 0.15f; // minimum time between presses

    private double lastAttackDSP;
    private int    onBeatStreak;

    private InputManager inputManager;
    private TimeManager  timeManager;

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
            BasicAttack(false);
            return;
        }

        // Valid on-beat press: increment streak and fire strong on every 4th
        onBeatStreak++;
        if (onBeatStreak >= 4) {
            onBeatStreak = 0;
            StrongAttack();
        } else {
            BasicAttack(true);
        }
    }

    private void BasicAttack(bool onBeat) {
        // TODO: spawn hitbox or animation
        Debug.Log(onBeat ? "Basic attack (on-beat)" : "Basic attack (off-beat)");
    }

    private void StrongAttack() {
        // TODO: spawn strong attack AOE, cone, etc.
        Debug.Log("Strong attack triggered!");
    }
}