using UnityEngine;

/// <summary>
/// Central place for game constants and states. Keeps magic numbers out of main classes.
/// </summary>
public static class GameConstants
{
    // Player Movement/Deadzone Tuning
    public const float PLAYER_DECELERATION = 18f;   // try 18–24
    public const float PLAYER_REVERSE_BRAKE = 30f;  // try 24–36
    public const float PLAYER_STOP_THRESHOLD = 0.02f; // 0.01–0.04
    public const float PLAYER_DEADZONE_ENTER = 0.08f; // 0.06–0.10
    public const float PLAYER_DEADZONE_EXIT = 0.12f;  // 0.10–0.14
    public const float PLAYER_SPEED = 2f;
    public const float PLAYER_ACCELERATION = 8f;
    public const float AIM_THRESHOLD = 0.01f;
    public const float WEAPON_AIM_THRESHOLD = 0.1f;

    // Combat Constants
    public const float BASIC_DAMAGE = 5f;
    public const float STRONG_DAMAGE = 20f;
    public const int COMBO_STREAK_FOR_STRONG = 4;
    public const float ATTACK_VISUAL_DURATION = 0.2f;
    public const float ATTACK_COOLDOWN = 0.15f;

    // Camera Constants
    public const float CAMERA_SMOOTH_TIME = 0.3f;
    public const float CAMERA_Z_OFFSET = -10f;

    // Visual Feedback Constants
    public const float HIT_FLASH_DURATION = 0.2f; // Increased from 0.1f for more noticeable flash
    public const float SPRITE_ROTATION_OFFSET = -90f; // For sprite forward direction
    public const float TEXT_OFFSET_Y = 0.5f;
    public const float TEXT_DURATION = 1f;
    public const float COMBO_TEXT_DURATION = 0.8f;
    public const float SHAKE_INTENSITY = 0.15f; // Increased from 0.1f for more noticeable shake
    public const float SHAKE_DURATION_MULTIPLIER = 2f; // Increased from 1.5f for longer shake

    // Entity Constants
    public const float DEFAULT_MAX_HEALTH = 100f; // Player health
    public const float ENEMY_MAX_HEALTH = 40f; // Balanced for perfect combo (4×5 + 20 = 40 damage)
    public const float RIGIDBODY_LINEAR_DAMPING = 6f;
    public const float RIGIDBODY_ANGULAR_DAMPING = 6f;

    // Colors for instant feedback
    public static readonly Color WEAPON_ON_DUTY_COLOR = Color.white;
    public static readonly Color WEAPON_OFF_DUTY_COLOR = Color.gray;
    public static readonly Color WEAPON_ATTACK_COLOR = Color.red;
    public static readonly Color ENEMY_ALIVE_COLOR = Color.red;
    public static readonly Color ENEMY_DEAD_COLOR = Color.gray;
    public static readonly Color HIT_FLASH_COLOR = Color.white;
    public static readonly Color PLAYER_DAMAGE_FLASH_COLOR = new(1f, 0.3f, 0.3f); // Bright red for player damage
    public static readonly Color ENEMY_DAMAGE_FLASH_COLOR = new(1f, 1f, 0.3f); // Bright yellow for enemy damage
    public static readonly Color ORANGE_COLOR = new(1f, 0.5f, 0f);
}