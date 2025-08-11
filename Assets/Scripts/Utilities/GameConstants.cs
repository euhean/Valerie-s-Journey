using UnityEngine;

/// <summary>
/// Central place for game constants and states. Keeps magic numbers out of main classes.
/// </summary>
public static class GameConstants
{
    // Combat Constants
    public const float BASIC_DAMAGE = 5f;
    public const float STRONG_DAMAGE = 20f;
    public const int COMBO_STREAK_FOR_STRONG = 4;
    public const float ATTACK_VISUAL_DURATION = 0.2f;
    public const float ATTACK_COOLDOWN = 0.15f;

    // Movement Constants
    public const float PLAYER_SPEED = 4f;
    public const float PLAYER_ACCELERATION = 8f;
    public const float AIM_THRESHOLD = 0.01f;
    public const float WEAPON_AIM_THRESHOLD = 0.1f;

    // Camera Constants
    public const float CAMERA_SMOOTH_TIME = 0.3f;
    public const float CAMERA_Z_OFFSET = -10f;

    // Visual Feedback Constants
    public const float HIT_FLASH_DURATION = 0.1f;
    public const float SPRITE_ROTATION_OFFSET = -90f; // For sprite forward direction
    public const float TEXT_OFFSET_Y = 0.5f;
    public const float TEXT_DURATION = 1f;
    public const float COMBO_TEXT_DURATION = 0.8f;
    public const float SHAKE_INTENSITY = 0.1f;
    public const float SHAKE_DURATION_MULTIPLIER = 1.5f;

    // Entity Constants
    public const float DEFAULT_MAX_HEALTH = 100f;
    public const float RIGIDBODY_LINEAR_DAMPING = 5f;
    public const float RIGIDBODY_ANGULAR_DAMPING = 5f;

    // Colors for instant feedback
    public static readonly Color WEAPON_ON_DUTY_COLOR = Color.white;
    public static readonly Color WEAPON_OFF_DUTY_COLOR = Color.gray;
    public static readonly Color WEAPON_ATTACK_COLOR = Color.red;
    public static readonly Color ENEMY_ALIVE_COLOR = Color.red;
    public static readonly Color ENEMY_DEAD_COLOR = Color.gray;
    public static readonly Color HIT_FLASH_COLOR = Color.white;
    public static readonly Color ORANGE_COLOR = new Color(1f, 0.5f, 0f);
}