using UnityEngine;

/// <summary>
/// The player entity. It holds references to its movement and attack components
/// and registers them with the input and rhythm managers.
/// </summary>
public class Player : Entity {
    [Header("Manager References")]
    public InputManager inputManager;
    public TimeManager  timeManager;

    private PlayerMover2D        mover;
    private PlayerAttackController attackController;

    private void Awake() {
        mover = GetComponent<PlayerMover2D>();
        attackController = GetComponent<PlayerAttackController>();
    }

    private void OnEnable() {
        // Register attack input when enabling the player
        if (attackController != null) {
            attackController.Register(inputManager, timeManager);
        }
    }

    private void OnDisable() {
        // Unregister attack input when disabling the player
        if (attackController != null) {
            attackController.Unregister(inputManager);
        }
    }
}