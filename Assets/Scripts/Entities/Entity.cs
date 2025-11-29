using UnityEngine;

/// <summary>
/// Base class for Player, Enemy, and all living entities.
/// CRITICAL: Manages health, death state, sprite rendering, and collider lifecycle.
/// Uses duty states to enable/disable entity activity.
/// </summary>
public abstract class Entity : MonoBehaviour
{
    #region Types
    public enum EntityState { Alive, Dead }
    public enum DutyState { OnDuty, OffDuty }
    #endregion

    #region Inspector
    [Header("Base Entity Settings")]
    [SerializeField] protected float maxHealth = GameConstants.ENEMY_MAX_HEALTH;
    #endregion

    #region State
    public float MaxHealth => maxHealth;
    public float Health { get; protected set; }
    public EntityState currentState = EntityState.Alive;
    public DutyState dutyState = DutyState.OffDuty;
    #endregion

    #region Components (cached on Awake)
    protected SpriteRenderer SpriteRenderer { get; private set; }
    protected BoxCollider2D BoxCollider { get; private set; }
    protected Rigidbody2D Rigidbody { get; private set; }
    #endregion

    #region State queries
    public bool IsAlive => currentState == EntityState.Alive;
    public bool IsOnDuty => dutyState == DutyState.OnDuty;
    #endregion

    #region Unity lifecycle
    protected virtual void Awake()
    {
        // Cache component refs to avoid repeated GetComponent calls
        SpriteRenderer = GetComponent<SpriteRenderer>();
        BoxCollider = GetComponent<BoxCollider2D>();
        Rigidbody = GetComponent<Rigidbody2D>();

        Health = maxHealth;
    }

    /// <summary>
    /// CRITICAL: Auto-fits collider to sprite bounds after inspector values load.
    /// Called after Awake, ensuring sprite assignments are ready.
    /// </summary>
    protected virtual void Start()
    {
        if (SpriteRenderer != null && SpriteRenderer.sprite != null && BoxCollider != null)
        {
            ComponentHelper.AutoConfigureColliderToSprite(SpriteRenderer, BoxCollider);
        }
    }
    #endregion

    #region Health & damage
    /// <summary>
    /// Apply damage. Clamps health to [0, maxHealth] and triggers death at zero.
    /// </summary>
    public virtual void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        Health = Mathf.Max(0, Health - amount);
        DebugHelper.LogState(() => $"{gameObject.name} took {amount} dmg, health: {Health}/{maxHealth}");

        if (Health <= 0)
            Die();
    }

    public virtual void Heal(float amount)
    {
        if (!IsAlive) return;
        Health = Mathf.Min(maxHealth, Health + amount);
    }

    /// <summary>
    /// Death handler. Override in subclasses for custom death behavior.
    /// Base implementation logs death and transitions to Dead state.
    /// </summary>
    protected virtual void Die()
    {
        if (!IsAlive) return;
        DebugHelper.LogState(() => $"{gameObject.name} died!");
        dutyState = DutyState.OffDuty;
        ChangeState(EntityState.Dead);
    }
    #endregion

    #region State management
    /// <summary>
    /// Duty state controls whether entity is active in gameplay.
    /// OffDuty = paused/disabled, OnDuty = active.
    /// </summary>
    public virtual void SetDutyState(DutyState newState)
    {
        if (dutyState == newState) return;
        DebugHelper.LogState(() => $"{gameObject.name} state changed from {dutyState} to {newState}");
        dutyState = newState;
    }

    protected void ChangeState(EntityState newState)
    {
        if (currentState == newState) return;
        var oldState = currentState;
        currentState = newState;
        DebugHelper.LogState(() => $"{gameObject.name} state changed from {oldState} to {newState}");
        OnStateChanged(oldState, newState);
    }

    protected virtual void OnStateChanged(EntityState from, EntityState to)
    {
        // Override in subclasses for custom behavior on state changes
    }
    #endregion
}