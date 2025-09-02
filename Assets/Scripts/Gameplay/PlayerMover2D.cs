using UnityEngine;

/// <summary>
/// 2D top-down movement with accel/brake + aim-rotation.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMover2D : MonoBehaviour
{
    #region Inspector
    public InputManager inputManager;
    #endregion

    #region Private
    private Rigidbody2D rb;
    private Vector2 velocity;
    private bool inDeadzone;
    #endregion

    #region Unity
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
    }

    private void FixedUpdate()
    {
        // Input
        Vector2 raw = (inputManager != null) ? inputManager.Move : Vector2.zero;
        float mag = raw.magnitude;

        // Deadzone (hysteresis)
        if (inDeadzone)
        {
            if (mag > GameConstants.PLAYER_DEADZONE_EXIT) inDeadzone = false;
        }
        else
        {
            if (mag < GameConstants.PLAYER_DEADZONE_ENTER) inDeadzone = true;
        }

        bool hasInput = !inDeadzone;
        Vector2 dir = hasInput ? raw.normalized : Vector2.zero;
        Vector2 targetVel = dir * GameConstants.PLAYER_SPEED;

        // Accel/Brake
        if (!hasInput)
        {
            velocity = Vector2.MoveTowards(velocity, Vector2.zero, GameConstants.PLAYER_DECELERATION * Time.fixedDeltaTime);
        }
        else
        {
            float align = (velocity.sqrMagnitude > 0.0001f) ? Vector2.Dot(velocity.normalized, dir) : 1f;
            float rate = (align < 0f) ? GameConstants.PLAYER_REVERSE_BRAKE : GameConstants.PLAYER_ACCELERATION;
            velocity = Vector2.MoveTowards(velocity, targetVel, rate * Time.fixedDeltaTime);
        }

        // Snap near stop
        float stopSq = GameConstants.PLAYER_STOP_THRESHOLD * GameConstants.PLAYER_STOP_THRESHOLD;
        if (!hasInput && velocity.sqrMagnitude < stopSq) velocity = Vector2.zero;

        // Move
        rb.MovePosition(rb.position + velocity * Time.fixedDeltaTime);

        // Rotate to aim
        if (inputManager != null && inputManager.Aim.sqrMagnitude > GameConstants.AIM_THRESHOLD)
        {
            float angle = Mathf.Atan2(inputManager.Aim.y, inputManager.Aim.x) * Mathf.Rad2Deg;
            rb.rotation = angle + GameConstants.SPRITE_ROTATION_OFFSET;
        }
    }
    #endregion
}