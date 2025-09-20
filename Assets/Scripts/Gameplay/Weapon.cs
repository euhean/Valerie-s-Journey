using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic weapon class that handles collision detection, visual representation,
/// and damage dealing. Attach to a child GameObject of the player.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Weapon : MonoBehaviour
{
    #region Inspector

    [Header("Weapon Settings")]
    public float basicDamage = GameConstants.BASIC_DAMAGE;
    public float strongDamage = GameConstants.STRONG_DAMAGE;
    public float weaponDistance = 0.8f; // Distance from player center for precise area of effect

    [Header("Visual Settings")]
    public Color onDutyColor  = GameConstants.WEAPON_ON_DUTY_COLOR;
    public Color offDutyColor = GameConstants.WEAPON_OFF_DUTY_COLOR;
    public Color attackColor  = GameConstants.WEAPON_ATTACK_COLOR;

    [Header("Components")]
    [SerializeField] private BoxCollider2D  weaponCollider;
    [SerializeField] private SpriteRenderer weaponRenderer;

    #endregion

    #region Private State

    private Entity ownerEntity;
    private bool isAttacking = false;
    private bool currentAttackIsStrong = false;

    // prevent multi-hit per swing
    private readonly HashSet<Entity> hitEntitiesThisAttack = new HashSet<Entity>();

    #endregion

    #region Editor Wiring

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!weaponCollider)  weaponCollider  = GetComponent<BoxCollider2D>();
        if (!weaponRenderer)  weaponRenderer  = GetComponent<SpriteRenderer>();
        if (weaponCollider)   weaponCollider.isTrigger = true;
    }
#endif

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        weaponCollider = weaponCollider ? weaponCollider : GetComponent<BoxCollider2D>();
        weaponRenderer = weaponRenderer ? weaponRenderer : GetComponent<SpriteRenderer>();
        ownerEntity    = GetComponentInParent<Entity>();

        Debug.Assert(weaponCollider && weaponRenderer, "Weapon requires BoxCollider2D + SpriteRenderer.");
        weaponCollider.isTrigger = true;

        // auto-size collider to sprite on boot
        if (weaponRenderer.sprite != null)
            ComponentHelper.AutoConfigureColliderToSprite(weaponRenderer, weaponCollider);

        // start hidden/off-duty — Player enables in Start()
        SetVisualState(false);
        if (weaponCollider) weaponCollider.enabled = false;
    }

    #endregion

    #region Public API

    public void SetOwnerDutyState(bool isOnDuty)
    {
        SetVisualState(isOnDuty);
        if (weaponCollider) weaponCollider.enabled = isOnDuty;
    }

    public void PerformAttack(bool isStrongAttack)
    {
        if (ownerEntity == null ||
            ownerEntity.currentState != Entity.EntityState.ALIVE ||
            !ownerEntity.onDuty)
            return;

        isAttacking = true;
        currentAttackIsStrong = isStrongAttack;
        hitEntitiesThisAttack.Clear();

        // quick flash on weapon
        Color effectColor = isStrongAttack ? GameConstants.ORANGE_COLOR : attackColor;
        AnimationHelper.ShowHitFlash(weaponRenderer, effectColor, GameConstants.ATTACK_VISUAL_DURATION);

        // close the attack window shortly after
        Invoke(nameof(ResetAttackState), GameConstants.ATTACK_VISUAL_DURATION);
    }

    public void UpdateAiming(Vector2 aimDirection)
    {
        if (ownerEntity == null ||
            ownerEntity.currentState != Entity.EntityState.ALIVE ||
            !ownerEntity.onDuty)
            return;

        float threshSq = GameConstants.WEAPON_AIM_THRESHOLD * GameConstants.WEAPON_AIM_THRESHOLD;
        if (aimDirection.sqrMagnitude > threshSq)
        {
            // Rotate weapon to face aim direction
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            
            // Position weapon at a fixed distance in the aim direction for precise area of effect
            Vector2 normalizedDirection = aimDirection.normalized;
            Vector3 weaponOffset = new Vector3(normalizedDirection.x, normalizedDirection.y, 0f) * weaponDistance;
            transform.localPosition = weaponOffset;
        }
    }

    #endregion

    #region Collision

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

    #endregion

    #region Helpers

    private void ResetAttackState()
    {
        isAttacking = false;

        bool show = ownerEntity != null &&
                    ownerEntity.currentState == Entity.EntityState.ALIVE &&
                    ownerEntity.onDuty;

        SetVisualState(show);
        if (weaponCollider) weaponCollider.enabled = show;
    }

    private void SetVisualState(bool isOnDuty)
    {
        if (!weaponRenderer) return;
        weaponRenderer.enabled = isOnDuty;
        weaponRenderer.color   = isOnDuty ? onDutyColor : offDutyColor;
    }

    #endregion
}