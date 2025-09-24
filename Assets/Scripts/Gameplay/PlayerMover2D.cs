using UnityEngine;

/// <summary>
/// 2D movement with hysteresis dead-zone and smooth accel/decel.
/// Also forwards aim to Weapon when not aim-locked by PlayerAttackController.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMover2D : MonoBehaviour
{
    [Header("Manager Refs (optional; fetched from GameManager if null)")]
    public InputManager inputManager;

    [Header("Configs (optional)")]
    public MovementConfig movementConfig; // if null, falls back to GameConstants

    [Header("Components")]
    public Weapon weapon;                         // optional: found in children if null
    public PlayerAttackController attackController; // optional: found on self if null

    private Rigidbody2D rb;
    private Vector2 velocity;
    private bool inDeadzone = false;

    private float Speed         => movementConfig ? movementConfig.playerSpeed         : GameConstants.PLAYER_SPEED;
    private float Accel         => movementConfig ? movementConfig.playerAcceleration  : GameConstants.PLAYER_ACCELERATION;
    private float Decel         => movementConfig ? movementConfig.playerDeceleration  : GameConstants.PLAYER_DECELERATION;
    private float ReverseBrake  => movementConfig ? movementConfig.playerReverseBrake  : GameConstants.PLAYER_REVERSE_BRAKE;
    private float DeadEnter     => movementConfig ? movementConfig.deadzoneEnter       : GameConstants.PLAYER_DEADZONE_ENTER;
    private float DeadExit      => movementConfig ? movementConfig.deadzoneExit        : GameConstants.PLAYER_DEADZONE_EXIT;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        weapon ??= GetComponentInChildren<Weapon>(true);
        attackController ??= GetComponent<PlayerAttackController>();
    }

    private void Start()
    {
        inputManager ??= GameManager.Instance?.inputManager;
        if (!inputManager) DebugHelper.LogError("[PlayerMover2D] InputManager missing.");
    }

    private void FixedUpdate()
    {
        if (!inputManager) return;

        // ----- DEAD-ZONE HYSTERESIS (recompute every tick) -----
        Vector2 raw = inputManager.Move;
        float mag = raw.magnitude;
        inDeadzone = inDeadzone ? (mag < DeadExit) : (mag < DeadEnter);

        Vector2 move = inDeadzone ? Vector2.zero : raw.normalized;

        // ----- VELOCITY MODEL -----
        Vector2 targetVel = move * Speed;
        Vector2 delta = targetVel - velocity;

        // Heavier brake when reversing
        float accel = (Vector2.Dot(velocity, targetVel) < 0f) ? ReverseBrake : Accel;
        Vector2 step = Vector2.ClampMagnitude(delta, accel * Time.fixedDeltaTime);
        velocity += step;

        // Stop threshold
        if (velocity.sqrMagnitude < GameConstants.PLAYER_STOP_THRESHOLD * GameConstants.PLAYER_STOP_THRESHOLD)
            velocity = Vector2.zero;

        rb.velocity = velocity;

        // ----- AIM FORWARDING -----
        // Skip aim updates if strong attack is locking aim
        if (weapon)
        {
            bool aimLocked = attackController && attackController.IsAimLocked();
            if (!aimLocked)
            {
                Vector2 aim = inputManager.Aim; // for KB+Mouse this should be set by InputManager as world dir
                weapon.UpdateAiming(aim);
            }
        }
    }
}