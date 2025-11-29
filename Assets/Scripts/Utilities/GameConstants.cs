using UnityEngine;

public static class GameConstants
{
    // Engine / physics tuning (not designer-facing)
    public const float RIGIDBODY_LINEAR_DAMPING = 0f;    // No damping - we handle velocity manually
    public const float RIGIDBODY_ANGULAR_DAMPING = 0.1f;  // Minimal angular damping to prevent spinning

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

    // Text display durations
    public const float TEXT_DURATION = 1f;
    public const float COMBO_TEXT_DURATION = 1.5f;

    // Enemy configuration
    public const float ENEMY_MAX_HEALTH = 40f;
    public const float DEFAULT_MAX_HEALTH = 100f;

    // Player movement constants
    public const float PLAYER_SPEED = 8f;
    public const float PLAYER_ACCELERATION = 15f;
    public const float PLAYER_DECELERATION = 12f;
    public const float PLAYER_REVERSE_BRAKE = 20f;
    public const float PLAYER_DEADZONE_ENTER = 0.1f;
    public const float PLAYER_DEADZONE_EXIT = 0.15f;
    public const float PLAYER_STOP_THRESHOLD = 0.1f;

    // Text display settings
    public const float TEXT_OFFSET_Y = 1f;

    // Animation/shake settings
    public const float SHAKE_DURATION_MULTIPLIER = 1f;
    public const float SHAKE_INTENSITY = 0.1f;

    // Visuals that are safe to keep here until moved to VisualConfig
    public static readonly Color WEAPON_ON_DUTY_COLOR = Color.white;
    public static readonly Color WEAPON_OFF_DUTY_COLOR = Color.gray;
    public static readonly Color WEAPON_ATTACK_COLOR = Color.red;
    public static readonly Color ORANGE_COLOR = new Color(1f, 0.5f, 0f);
    
    // Flash effect colors
    public static readonly Color ENEMY_HIT_FLASH_COLOR = new Color(1f, 0.3f, 0.3f); // Bright red
    public static readonly Color ENEMY_DAMAGE_FLASH_COLOR = new Color(1f, 0.8f, 0f); // Bright orange-yellow
    public static readonly Color PLAYER_DAMAGE_FLASH_COLOR = new Color(1f, 0.2f, 0.2f); // Bright red
    public static readonly Color ENEMY_ALIVE_COLOR = Color.white;
    public static readonly Color ENEMY_DEAD_COLOR = Color.gray;
    
    // Flash effect durations
    public const float ENEMY_HIT_FLASH_DURATION = 0.3f;
    public const float ENEMY_DAMAGE_FLASH_DURATION = 0.4f;
    public const float PLAYER_DAMAGE_FLASH_DURATION = 0.35f;
    public const float HIT_FLASH_DURATION = 0.2f;
}