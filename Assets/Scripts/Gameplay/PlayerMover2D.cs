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

    private Rigidbody2D rb;
    private Vector2 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        // Calculate target velocity from the left stick
        Vector2 target = inputManager != null ? inputManager.Move * speed : Vector2.zero;
        velocity = Vector2.MoveTowards(
            velocity,
            target,
            acceleration * Time.fixedDeltaTime
        );
        // Apply movement
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);
        // Rotate to face the aim direction if the right stick is non-zero
        if (inputManager != null && inputManager.Aim.sqrMagnitude > GameConstants.AIM_THRESHOLD)
        {
            float angle = Mathf.Atan2(inputManager.Aim.y, inputManager.Aim.x) * Mathf.Rad2Deg;
            rb.rotation = angle + GameConstants.SPRITE_ROTATION_OFFSET; // adjust for sprite's forward direction
        }
    }
}