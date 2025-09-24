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

    // Visuals that are safe to keep here until moved to VisualConfig
    public static readonly Color WEAPON_ON_DUTY_COLOR = Color.white;
    public static readonly Color WEAPON_OFF_DUTY_COLOR = Color.gray;
    public static readonly Color WEAPON_ATTACK_COLOR = Color.red;
    public static readonly Color ORANGE_COLOR = new Color(1f, 0.5f, 0f);
}