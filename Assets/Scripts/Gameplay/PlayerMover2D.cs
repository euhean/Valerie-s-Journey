using UnityEngine;

/// <summary>
/// Handles 2D movement and optional rotation based on the aim stick.
/// Requires a Rigidbody2D for smooth, physics-friendly motion.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMover2D : MonoBehaviour
{
    public InputManager inputManager;

    [Header("Movement Parameters")]
    // All movement/deadzone tuning values are now in GameConstants

    private Rigidbody2D rb;
    private Vector2 velocity;
    private bool inDeadzone;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        // helps visuals stay glued to physics without jitter
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        // 1) Read input
        Vector2 raw = (inputManager != null) ? inputManager.Move : Vector2.zero;
        float mag = raw.magnitude;

        // 2) Deadzone with hysteresis (stable center)
        if (inDeadzone)
            if (mag > GameConstants.PLAYER_DEADZONE_EXIT) inDeadzone = false;
            else if (mag < GameConstants.PLAYER_DEADZONE_ENTER) inDeadzone = true;

        bool hasInput = !inDeadzone;
        Vector2 moveDir = hasInput ? raw.normalized : Vector2.zero;
        Vector2 targetVel = moveDir * GameConstants.PLAYER_SPEED;

        // 3) Accel/Brake
        if (!hasInput) {
            // No input: brake hard toward zero
            velocity = Vector2.MoveTowards(
                velocity,
                Vector2.zero,
                GameConstants.PLAYER_DECELERATION * Time.fixedDeltaTime
            );
        } else {
            // If reversing direction, apply stronger brake before accelerating
            float align = (velocity.sqrMagnitude > 0.0001f)
                ? Vector2.Dot(velocity.normalized, moveDir)
                : 1f;
            float rate = (align < 0f)
                ? GameConstants.PLAYER_REVERSE_BRAKE
                : GameConstants.PLAYER_ACCELERATION;

            velocity = Vector2.MoveTowards(
                velocity,
                targetVel,
                rate * Time.fixedDeltaTime
            );
        }

        // 4) Hard snap near stop to kill micro-drift
        float stopSq = GameConstants.PLAYER_STOP_THRESHOLD * GameConstants.PLAYER_STOP_THRESHOLD;
        if (!hasInput && velocity.sqrMagnitude < stopSq) {
            velocity = Vector2.zero;
        }

        // 5) Move
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        // 6) Rotate to aim
        if (inputManager != null && inputManager.Aim.sqrMagnitude > GameConstants.AIM_THRESHOLD) {
            float angle = Mathf.Atan2(inputManager.Aim.y, inputManager.Aim.x) * Mathf.Rad2Deg;
            rb.rotation = angle + GameConstants.SPRITE_ROTATION_OFFSET;
        }
    }
}