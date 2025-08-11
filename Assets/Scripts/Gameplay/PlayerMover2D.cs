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
    public float speed = GameConstants.PLAYER_SPEED;
    public float acceleration = GameConstants.PLAYER_ACCELERATION;
    [Tooltip("How fast we brake when no input")]
    public float deceleration = 18f;   // try 18–24
    [Tooltip("Stick deadzone for stop snap")]
    public float stopDeadzone = 0.08f; // 0.06–0.1
    
    private Rigidbody2D rb;
    private Vector2 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Vector2 move = (inputManager != null) ? inputManager.Move : Vector2.zero;
        Vector2 target = move * speed;

        bool hasInput = move.sqrMagnitude >= (stopDeadzone * stopDeadzone);

        // accelerate when there is input, decelerate hard when there isn't
        float rate = hasInput ? (acceleration) : (deceleration);
        velocity = Vector2.MoveTowards(velocity, target, rate * Time.fixedDeltaTime);

        // hard snap to zero to kill micro-drift
        if (!hasInput && velocity.sqrMagnitude < 0.001f) velocity = Vector2.zero;

        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        // rotation stays the same
        if (inputManager != null && inputManager.Aim.sqrMagnitude > GameConstants.AIM_THRESHOLD) {
        float angle = Mathf.Atan2(inputManager.Aim.y, inputManager.Aim.x) * Mathf.Rad2Deg;
        rb.rotation = angle + GameConstants.SPRITE_ROTATION_OFFSET;
        }

        if (inputManager != null && inputManager.Move.sqrMagnitude < 0.01f)
            velocity = Vector2.zero;
    }
}