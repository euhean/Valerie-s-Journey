using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic weapon class that handles collision detection, visual representation,
/// and damage dealing. Should be attached to a child GameObject of the player.
/// </summary>
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Weapon : MonoBehaviour
{
    [Header("Weapon Settings")]
    public float basicDamage = GameConstants.BASIC_DAMAGE;
    public float strongDamage = GameConstants.STRONG_DAMAGE;

    [Header("Visual Settings")]
    public Color onDutyColor = GameConstants.WEAPON_ON_DUTY_COLOR;
    public Color offDutyColor = GameConstants.WEAPON_OFF_DUTY_COLOR;
    public Color attackColor = GameConstants.WEAPON_ATTACK_COLOR;

    private BoxCollider2D weaponCollider;
    private SpriteRenderer weaponRenderer;
    private Entity ownerEntity;
    private bool isAttacking = false;
    private bool currentAttackIsStrong = false;

    // Track entities we've already hit during current attack to prevent multiple hits
    private readonly HashSet<Entity> hitEntitiesThisAttack = new HashSet<Entity>();

    private void Awake()
    {
        weaponCollider = GetComponent<BoxCollider2D>();
        weaponRenderer = GetComponent<SpriteRenderer>();

        // Set as trigger for collision detection
        weaponCollider.isTrigger = true;

        // Auto-configure collider to match sprite size
        if (weaponRenderer != null && weaponCollider != null)
        {
            ComponentHelper.AutoConfigureColliderToSprite(weaponRenderer, weaponCollider);
        }

        // Find owner entity in parent
        ownerEntity = GetComponentInParent<Entity>();

        // Start in off-duty state
        SetVisualState(false);
    }

    public void SetOwnerDutyState(bool isOnDuty)
    {
        SetVisualState(isOnDuty);
        // Enable/disable collider based on duty state
        weaponCollider.enabled = isOnDuty;
    }

    public void PerformAttack(bool isStrongAttack)
    {
        if (ownerEntity == null || ownerEntity.currentState != Entity.EntityState.ALIVE || !ownerEntity.onDuty)
        {
            return;
        }
        isAttacking = true;
        currentAttackIsStrong = isStrongAttack;
        hitEntitiesThisAttack.Clear();

        // Delegate visual feedback to helper
        Color effectColor = isStrongAttack ? GameConstants.ORANGE_COLOR : attackColor;
        AnimationHelper.ShowHitFlash(weaponRenderer, effectColor, GameConstants.ATTACK_VISUAL_DURATION);

        // Reset attack state after brief delay
        Invoke(nameof(ResetAttackState), GameConstants.ATTACK_VISUAL_DURATION);
    }

    private void ResetAttackState()
    {
        isAttacking = false;
        // Return to duty-appropriate visual state
        SetVisualState(ownerEntity != null && ownerEntity.currentState == Entity.EntityState.ALIVE && ownerEntity.onDuty);
    }

    private void SetVisualState(bool isOnDuty)
    {
        if (weaponRenderer != null)
        {
            weaponRenderer.color = isOnDuty ? onDutyColor : offDutyColor;
            weaponRenderer.enabled = isOnDuty; // Only show weapon when on duty
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        // Only deal damage during active attacks
        if (!isAttacking) return;

        // Check if target has "Enemy" tag
        if (!other.CompareTag("Enemy")) return;

        // Get entity component
        Entity targetEntity = other.GetComponent<Entity>();
        if (targetEntity == null) return;

        // Prevent hitting the same entity multiple times per attack
        if (hitEntitiesThisAttack.Contains(targetEntity)) return;

        // Deal damage
        float damage = currentAttackIsStrong ? strongDamage : basicDamage;
        string attackType = currentAttackIsStrong ? "STRONG" : "basic";
        DebugHelper.LogCombat($"Weapon hit {targetEntity.name} with {attackType} attack ({damage} damage)");
        targetEntity.TakeDamage(damage);
        hitEntitiesThisAttack.Add(targetEntity);
    }

    public void UpdateAiming(Vector2 aimDirection)
    {
        if (ownerEntity == null || ownerEntity.currentState != Entity.EntityState.ALIVE || !ownerEntity.onDuty)
        {
            return;
        }
        // Rotate weapon to face aim direction
        if (aimDirection.magnitude > GameConstants.WEAPON_AIM_THRESHOLD)
        {
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
}