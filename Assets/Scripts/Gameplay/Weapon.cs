using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic weapon class that handles collision detection, visual representation,
/// and damage dealing. Should be attached to a child GameObject of the player.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
// Optional: ensure Weapon.Awake runs before Player.Awake if you ever touch it in Player.Awake
// [DefaultExecutionOrder(-50)]
public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float basicDamage = GameConstants.BASIC_DAMAGE;
    public float strongDamage = GameConstants.STRONG_DAMAGE;

    [Header("Visual Settings")]
    public Color onDutyColor  = GameConstants.WEAPON_ON_DUTY_COLOR;
    public Color offDutyColor = GameConstants.WEAPON_OFF_DUTY_COLOR;
    public Color attackColor  = GameConstants.WEAPON_ATTACK_COLOR;

    [Header("Components")]
    [SerializeField] private BoxCollider2D  weaponCollider;
    [SerializeField] private SpriteRenderer weaponRenderer;

    private Entity ownerEntity;
    private bool isAttacking = false;
    private bool currentAttackIsStrong = false;

    // Track entities we've already hit during current attack to prevent multiple hits
    private readonly HashSet<Entity> hitEntitiesThisAttack = new HashSet<Entity>();

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Keep references wired in the editor and enforce trigger
        if (!weaponCollider)  weaponCollider  = GetComponent<BoxCollider2D>();
        if (!weaponRenderer)  weaponRenderer  = GetComponent<SpriteRenderer>();
        if (weaponCollider)   weaponCollider.isTrigger = true;
    }
#endif

    private void Awake()
    {
        // Final runtime guarantees (no lazy init needed)
        weaponCollider = weaponCollider ? weaponCollider : GetComponent<BoxCollider2D>();
        weaponRenderer = weaponRenderer ? weaponRenderer : GetComponent<SpriteRenderer>();
        ownerEntity    = GetComponentInParent<Entity>();

        // Hard contract: these must exist because of RequireComponent
        Debug.Assert(weaponCollider && weaponRenderer, "Weapon requires BoxCollider2D + SpriteRenderer.");
        weaponCollider.isTrigger = true;

        // Auto-configure collider to match sprite size (when present)
        if (weaponRenderer.sprite != null)
            ComponentHelper.AutoConfigureColliderToSprite(weaponRenderer, weaponCollider);

        // Start hidden/off-duty — Player will enable in Start()
        SetVisualState(false);
        if (weaponCollider) weaponCollider.enabled = false;
    }

    public void SetOwnerDutyState(bool isOnDuty)
    {
        // Visual + collider gate
        SetVisualState(isOnDuty);
        if (weaponCollider) weaponCollider.enabled = isOnDuty;
    }

    public void PerformAttack(bool isStrongAttack)
    {
        if (ownerEntity == null || ownerEntity.currentState != Entity.EntityState.ALIVE || !ownerEntity.onDuty)
            return;

        isAttacking = true;
        currentAttackIsStrong = isStrongAttack;
        hitEntitiesThisAttack.Clear();

        // Quick flash on the weapon itself
        Color effectColor = isStrongAttack ? GameConstants.ORANGE_COLOR : attackColor;
        AnimationHelper.ShowHitFlash(weaponRenderer, effectColor, GameConstants.ATTACK_VISUAL_DURATION);

        // End the attack window shortly after
        Invoke(nameof(ResetAttackState), GameConstants.ATTACK_VISUAL_DURATION);
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        // Return to duty-appropriate visual state
        bool show = ownerEntity != null && ownerEntity.currentState == Entity.EntityState.ALIVE && ownerEntity.onDuty;
        SetVisualState(show);
        if (weaponCollider) weaponCollider.enabled = show;
    }

    private void SetVisualState(bool isOnDuty)
    {
        if (!weaponRenderer) return;
        weaponRenderer.enabled = isOnDuty;
        weaponRenderer.color   = isOnDuty ? onDutyColor : offDutyColor;
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isAttacking) return;
        if (!other.CompareTag("Enemy")) return;
        if (!other.TryGetComponent<Entity>(out var targetEntity)) return;
        if (hitEntitiesThisAttack.Contains(targetEntity)) return;

        float damage = currentAttackIsStrong ? strongDamage : basicDamage;
        string attackType = currentAttackIsStrong ? "STRONG" : "basic";
        DebugHelper.LogCombat(() => $"Weapon hit {targetEntity.name} with {attackType} attack ({damage} damage)");
        targetEntity.TakeDamage(damage);
        hitEntitiesThisAttack.Add(targetEntity);
    }

    public void UpdateAiming(Vector2 aimDirection)
    {
        // Rotate weapon to face aim direction
        if (ownerEntity == null || ownerEntity.currentState != Entity.EntityState.ALIVE || !ownerEntity.onDuty)
            return;

        if (aimDirection.sqrMagnitude > (GameConstants.WEAPON_AIM_THRESHOLD * GameConstants.WEAPON_AIM_THRESHOLD))
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}