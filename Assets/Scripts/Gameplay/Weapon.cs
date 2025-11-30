using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generic weapon: timed attack window, overlap-based hits, damage application,
/// and per-hit DamageApplied events. Works with PlayerAttackController which
/// opens the window and later consumes the hit list.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Weapon : MonoBehaviour
{
    #region Inspector

    [Header("Optional Config (overrides GameConstants damages if set)")]
    public CombatConfig combatConfig;

    [Header("Weapon Settings (fallback if no CombatConfig)")]
    public float basicDamage  = GameConstants.BASIC_DAMAGE;
    public float strongDamage = GameConstants.STRONG_DAMAGE;

    [Header("Visuals")]
    public Color onDutyColor   = GameConstants.WEAPON_ON_DUTY_COLOR;
    public Color offDutyColor  = GameConstants.WEAPON_OFF_DUTY_COLOR;
    public Color attackColor   = GameConstants.WEAPON_ATTACK_COLOR;
    [Tooltip("Local offset distance of the blade from player center when aiming.")]
    public float weaponDistance = 0.6f;

    [Header("References (auto-filled)")]
    [SerializeField] private BoxCollider2D weaponCollider;
    [SerializeField] private SpriteRenderer weaponRenderer;
    private Entity ownerEntity;

    #endregion

    #region Runtime State

    // Current window state
    private bool isAttacking = false;
    private bool currentAttackIsStrong = false;

    // Per-window hit tracking
    private readonly HashSet<Entity> hitSet = new HashSet<Entity>();
    private readonly List<Entity> hitList = new List<Entity>();

    #endregion

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!weaponCollider)  weaponCollider  = GetComponent<BoxCollider2D>();
        if (!weaponRenderer)  weaponRenderer  = GetComponent<SpriteRenderer>();
        if (weaponCollider)   weaponCollider.isTrigger = true;
    }
#endif

    #region Unity Lifecycle

    private void Awake()
    {
        gameObject.tag = "Weapon";
        weaponCollider = weaponCollider ? weaponCollider : GetComponent<BoxCollider2D>();
        weaponRenderer = weaponRenderer ? weaponRenderer : GetComponent<SpriteRenderer>();    
        
        // Auto-assign owner entity if not set
        if (ownerEntity == null)
        {
            // Try centralized reference first, then fallback to search
            ownerEntity = GameManager.Instance?.MainPlayer;
            if (ownerEntity == null)
                ownerEntity = FindFirstObjectByType<Player>();

            if (ownerEntity != null)
                DebugHelper.LogManager($"[Weapon] Auto-assigned owner: {ownerEntity.name}");
            else
                DebugHelper.LogWarning("[Weapon] No owner Entity found. Use weapon.SetOwner() or assign a Player to the scene.");
        }
        
        Debug.Assert(weaponCollider && weaponRenderer, "Weapon requires BoxCollider2D + SpriteRenderer.");
        weaponCollider.isTrigger = true;

        // start hidden/off-duty — controlled by PlayerAttackController
        SetVisualState(false);
        if (weaponCollider) weaponCollider.enabled = false;
    }

    #endregion

    #region Public API (called by PlayerAttackController)

    /// <summary>
    /// Sets the owner entity for this weapon. Call this after instantiating the weapon.
    /// </summary>
    public void SetOwner(Entity owner)
    {
        ownerEntity = owner;
    }

    /// <summary>
    /// Opens a timed attack window. During the window, overlapping entities are damaged once.
    /// </summary>
    public void StartAttackWindow(bool isStrongAttack, float windowSeconds)
    {
        // Require owner entity to be alive and on duty to prevent null attacker in events
        bool canAttack = ownerEntity != null && 
                        ownerEntity.CurrentState == Entity.EntityState.Alive && 
                        ownerEntity.CurrentDutyState == Entity.DutyState.OnDuty;
        
        if (!canAttack)
            return;

        // Validate windowSeconds to prevent NaN, negative, or zero values from breaking the system
        if (float.IsNaN(windowSeconds) || float.IsInfinity(windowSeconds) || windowSeconds <= 0f)
        {
            DebugHelper.LogWarning($"[Weapon] Invalid windowSeconds={windowSeconds}, clamping to 0.01f");
            windowSeconds = 0.01f; // Minimum safe duration
        }

        // Begin window
        isAttacking = true;
        currentAttackIsStrong = isStrongAttack;
        hitSet.Clear();
        hitList.Clear();

        // Visual flash to indicate attack
        var effectColor = isStrongAttack ? GameConstants.ORANGE_COLOR : attackColor;
        if (weaponRenderer != null)
            AnimationHelper.ShowHitFlash(weaponRenderer, effectColor, windowSeconds);
        else
            DebugHelper.LogWarning("[Weapon] weaponRenderer missing — skipping attack flash.");

        // Weapon should be visible & colliding while on duty
        SetVisualState(true);
        if (weaponCollider) weaponCollider.enabled = true;

        // Close window after duration
        CancelInvoke(nameof(ResetAttackState));
        Invoke(nameof(ResetAttackState), windowSeconds);
    }

    /// <summary>
    /// Returns the list of entities hit during the last window and clears the internal list.
    /// Safe to call after the window completes.
    /// </summary>
    public IReadOnlyList<Entity> ConsumeHitTargets()
    {
        // Return a copy and clear internal list to avoid accidental reuse
        var copy = new List<Entity>(hitList);
        hitList.Clear();
        hitSet.Clear();
        return copy;
    }

    /// <summary>
    /// Aiming helper. Rotate and offset the weapon along aim direction.
    /// Call from movement/aim system when not aim-locked.
    /// </summary>
    public void UpdateAiming(Vector2 aimDirection)
    {
        if (ownerEntity == null ||
            ownerEntity.CurrentState != Entity.EntityState.Alive ||
            ownerEntity.CurrentDutyState != Entity.DutyState.OnDuty)
            return;

        float threshSq = GameConstants.WEAPON_AIM_THRESHOLD * GameConstants.WEAPON_AIM_THRESHOLD;
        if (aimDirection.sqrMagnitude > threshSq)
        {
            // Rotate weapon to face aim direction
            float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            // Position weapon at a fixed distance in the aim direction
            Vector2 normalized = aimDirection.normalized;
            Vector3 offset = new Vector3(normalized.x, normalized.y, 0f) * weaponDistance;
            transform.localPosition = offset;
        }
    }

    /// <summary>
    /// Called by Player/Entity when their duty state changes (show/hide the weapon).
    /// </summary>
    public void SetOwnerDutyState(bool isOnDuty)
    {
        SetVisualState(isOnDuty);
        if (weaponCollider) weaponCollider.enabled = isOnDuty;
    }

    /// <summary>
    /// Immediately cancels any active attack window, resets state, and clears hit lists.
    /// Call this when the attack is interrupted (e.g. player takes damage).
    /// </summary>
    public void AbortAttack()
    {
        CancelInvoke(nameof(ResetAttackState));
        isAttacking = false;
        hitSet.Clear();
        hitList.Clear();
        ResetAttackState(); // Ensure visuals/collider return to normal state
    }

    #endregion

    #region Collision

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!isAttacking) return;
        if (!other.CompareTag("Enemy")) return;
        if (!other.TryGetComponent<Entity>(out var targetEntity)) return;
        if (hitSet.Contains(targetEntity)) return;

        // Determine damage
        float dmg = currentAttackIsStrong
            ? (combatConfig != null ? combatConfig.strongDamage : strongDamage)
            : (combatConfig != null ? combatConfig.basicDamage  : basicDamage);

        // Apply damage
        float before = targetEntity.Health;
        targetEntity.TakeDamage(dmg);
        bool killingBlow = targetEntity.Health <= 0f;

        // Track hit
        hitSet.Add(targetEntity);
        hitList.Add(targetEntity);

        // Notify
        if (EventBus.Instance != null)
        {
            EventBus.Instance.Publish(new DamageApplied(
                attacker: ownerEntity,
                target:   targetEntity,
                amount:   dmg,
                killingBlow: killingBlow,
                isStrong: currentAttackIsStrong,
                onBeat:   false // PlayerAttackController knows onBeat; emit there in AttackResolved
            ));
        }
        else
        {
            DebugHelper.LogWarning("[Weapon] EventBus missing — DamageApplied event skipped.");
        }

        DebugHelper.LogCombat(() => $"[Weapon] Hit {targetEntity.name} for {dmg} ({before:F1}->{targetEntity.Health:F1})");
    }

    #endregion

    #region Helpers

    private void ResetAttackState()
    {
        isAttacking = false;

        bool show = ownerEntity != null &&
                    ownerEntity.CurrentState == Entity.EntityState.Alive &&
                    ownerEntity.CurrentDutyState == Entity.DutyState.OnDuty;

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