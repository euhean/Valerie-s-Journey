using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hit detection for a melee weapon. PlayerAttackController opens and closes the attack
/// window; while it is open, each living enemy the trigger touches is damaged once and a
/// DamageApplied event is published for it. The weapon is a child of the entity wielding it.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider2D), typeof(SpriteRenderer))]
public class Weapon : MonoBehaviour
{
    #region Inspector
    [Header("Optional Config (overrides the fallback damages below if set)")]
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
    #endregion

    #region Runtime State
    private Entity owner;
    private bool windowOpen = false;
    private bool currentAttackIsStrong = false;
    private bool currentAttackOnBeat = false;
    private readonly List<Entity> hitTargets = new List<Entity>(); // entities hit in the current window
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
        if (!weaponCollider) weaponCollider = GetComponent<BoxCollider2D>();
        if (!weaponRenderer) weaponRenderer = GetComponent<SpriteRenderer>();
        owner = GetComponentInParent<Entity>();

        // The trigger only exists while an attack window is open.
        weaponCollider.isTrigger = true;
        weaponCollider.enabled = false;
        SetOwnerDutyState(false); // hidden until the owner goes on duty
    }
    #endregion

    #region Public API
    /// <summary>
    /// Starts collecting hits. Enabling the collider here (instead of leaving it on)
    /// makes Unity report enemies that are already overlapping as new contacts.
    /// </summary>
    public void OpenAttackWindow(bool isStrongAttack, bool onBeat)
    {
        windowOpen = true;
        currentAttackIsStrong = isStrongAttack;
        currentAttackOnBeat = onBeat;
        hitTargets.Clear();

        weaponCollider.enabled = true;
        weaponRenderer.color = isStrongAttack ? GameConstants.ORANGE_COLOR : attackColor;
    }

    /// <summary>Stops collecting hits and returns the entities hit during the window.</summary>
    public IReadOnlyList<Entity> CloseAttackWindow()
    {
        windowOpen = false;
        weaponCollider.enabled = false;
        weaponRenderer.color = onDutyColor;

        var hits = new List<Entity>(hitTargets);
        hitTargets.Clear();
        return hits;
    }

    /// <summary>Rotates and offsets the weapon along the aim direction.</summary>
    public void UpdateAiming(Vector2 aimDirection)
    {
        if (!owner.IsAlive || !owner.IsOnDuty) return;

        float threshSq = GameConstants.WEAPON_AIM_THRESHOLD * GameConstants.WEAPON_AIM_THRESHOLD;
        if (aimDirection.sqrMagnitude <= threshSq) return;

        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        Vector2 normalized = aimDirection.normalized;
        transform.localPosition = new Vector3(normalized.x, normalized.y, 0f) * weaponDistance;
    }

    /// <summary>The owner calls this when its duty state changes: the weapon shows only while on duty.</summary>
    public void SetOwnerDutyState(bool isOnDuty)
    {
        weaponRenderer.enabled = isOnDuty;
        weaponRenderer.color = isOnDuty ? onDutyColor : offDutyColor;
    }
    #endregion

    #region Collision
    // Enter covers contacts that begin during the window; Stay covers a blade that is
    // steered onto a target that Unity already reported.
    private void OnTriggerEnter2D(Collider2D other) => TryHit(other);
    private void OnTriggerStay2D(Collider2D other) => TryHit(other);

    private void TryHit(Collider2D other)
    {
        if (!windowOpen) return;
        if (!other.CompareTag("Enemy")) return;
        if (!other.TryGetComponent<Entity>(out var target)) return;
        if (!target.IsAlive || hitTargets.Contains(target)) return;

        float damage = currentAttackIsStrong
            ? (combatConfig != null ? combatConfig.strongDamage : strongDamage)
            : (combatConfig != null ? combatConfig.basicDamage  : basicDamage);

        float before = target.Health;
        target.TakeDamage(damage);
        hitTargets.Add(target);

        EventBus.Instance.Publish(new DamageApplied(
            attacker:    owner,
            target:      target,
            amount:      damage,
            killingBlow: !target.IsAlive,
            isStrong:    currentAttackIsStrong,
            onBeat:      currentAttackOnBeat
        ));

        DebugHelper.LogCombat(() => $"[Weapon] Hit {target.name} for {damage} ({before:F1}->{target.Health:F1})");
    }
    #endregion
}
