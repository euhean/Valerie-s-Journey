using UnityEngine;

public static class GameConstants
{
    // Engine / physics tuning (not designer-facing)
    public const float RIGIDBODY_LINEAR_DAMPING = 6f;
    public const float RIGIDBODY_ANGULAR_DAMPING = 6f;

    public const float CAMERA_SMOOTH_TIME = 0.3f;
    public const float CAMERA_Z_OFFSET = -10f;

    // Minimal thresholds
    public const float AIM_THRESHOLD = 0.01f;
    public const float WEAPON_AIM_THRESHOLD = 0.1f;

    // Defaults used as safe fallbacks when no SO assigned
    public const float BASIC_DAMAGE = 5f;
    public const float STRONG_DAMAGE = 20f;
    public const int COMBO_STREAK_FOR_STRONG = 4;
    public const float ATTACK_VISUAL_DURATION = 0.2f;
    public const float ATTACK_COOLDOWN = 0.15f;

    // Player movement fallback defaults (used when MovementConfig is null)
    public const float PLAYER_SPEED = 2f;
    public const float PLAYER_ACCELERATION = 8f;
    public const float PLAYER_DECELERATION = 18f;
    public const float PLAYER_REVERSE_BRAKE = 30f;
    public const float PLAYER_DEADZONE_ENTER = 0.08f;
    public const float PLAYER_DEADZONE_EXIT = 0.12f;
    public const float PLAYER_STOP_THRESHOLD = 0.01f;

    // Text feedback animation defaults
    public const float TEXT_DURATION = 1.5f;
    public const float TEXT_OFFSET_Y = 0.5f;
    public const float COMBO_TEXT_DURATION = 2f;

    // Entity health defaults
    public const float DEFAULT_MAX_HEALTH = 100f;
    public const float ENEMY_MAX_HEALTH = 40f;

    // Hit flash animation
    public const float HIT_FLASH_DURATION = 0.2f;
    public const float SHAKE_DURATION_MULTIPLIER = 0.5f;
    public const float SHAKE_INTENSITY = 0.05f;

    // Visuals that are safe to keep here until moved to VisualConfig
    public static readonly Color WEAPON_ON_DUTY_COLOR = Color.white;
    public static readonly Color WEAPON_OFF_DUTY_COLOR = Color.gray;
    public static readonly Color WEAPON_ATTACK_COLOR = Color.red;
    public static readonly Color ORANGE_COLOR = new Color(1f, 0.5f, 0f);
    public static readonly Color ENEMY_ALIVE_COLOR = Color.red;
    public static readonly Color ENEMY_DEAD_COLOR = new Color(0.5f, 0.5f, 0.5f); // Gray
    public static readonly Color ENEMY_DAMAGE_FLASH_COLOR = Color.yellow;
    public static readonly Color PLAYER_DAMAGE_FLASH_COLOR = new Color(1f, 0.3f, 0.3f); // Light red
}