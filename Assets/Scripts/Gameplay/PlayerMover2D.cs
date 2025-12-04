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

    private float Speed         => movementConfig?.playerSpeed         ?? GameConstants.PLAYER_SPEED;
    private float Accel         => movementConfig?.playerAcceleration  ?? GameConstants.PLAYER_ACCELERATION;
    private float Decel         => movementConfig?.playerDeceleration  ?? GameConstants.PLAYER_DECELERATION;
    private float ReverseBrake  => movementConfig?.playerReverseBrake  ?? GameConstants.PLAYER_REVERSE_BRAKE;
    private float DeadEnter     => movementConfig?.deadzoneEnter       ?? GameConstants.PLAYER_DEADZONE_ENTER;
    private float DeadExit      => movementConfig?.deadzoneExit        ?? GameConstants.PLAYER_DEADZONE_EXIT;
    private float StopThreshold => movementConfig?.stopThreshold       ?? GameConstants.PLAYER_STOP_THRESHOLD;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Use centralized weapon fallback logic
        weapon ??= ComponentHelper.FindWeaponFallback("PlayerMover2D");
        
        // Find attack controller on same GameObject
        attackController ??= GetComponent<PlayerAttackController>();
        if (attackController == null)
            DebugHelper.LogWarning("[PlayerMover2D] No PlayerAttackController found. Add one to this GameObject or assign it in Inspector.");
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

        // Determine which acceleration rate to use:
        // 1. Reversing (moving opposite to target) → use ReverseBrake (hardest)
        // 2. Decelerating (slowing down) → use Decel
        // 3. Accelerating (speeding up) → use Accel
        float accel;
        if (Vector2.Dot(velocity, targetVel) < 0f)
        {
            // Moving opposite direction from input → hard brake
            accel = ReverseBrake;
        }
        else if (velocity.sqrMagnitude > targetVel.sqrMagnitude)
        {
            // Current speed is higher than target → slowing down → deceleration
            accel = Decel;
        }
        else
        {
            // Target speed is higher than current → speeding up → acceleration
            accel = Accel;
        }

        Vector2 step = Vector2.ClampMagnitude(delta, accel * Time.fixedDeltaTime);
        velocity += step;

        // Stop threshold: snap to zero when velocity is very small to prevent drift
        if (velocity.sqrMagnitude < StopThreshold * StopThreshold)
            velocity = Vector2.zero;

        rb.linearVelocity = velocity;

        // ----- AIM FORWARDING -----
        // Skip aim updates if strong attack is locking aim
        Vector2 aim = inputManager.Aim; // for KB+Mouse this should be set by InputManager as world dir
        bool aimLocked = attackController && attackController.IsAimLocked();

        if (!aimLocked)
        {
            attackController?.RegisterAimDirection(aim);
            if (weapon)
            {
                weapon.UpdateAiming(aim);
            }
        }
    }
}