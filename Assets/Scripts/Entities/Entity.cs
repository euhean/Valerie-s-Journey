using UnityEngine;

/// <summary>
/// Base class for all living game objects. Provides health/damage system,
/// entity states, and ensures required 2D components are present.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public abstract class Entity : MonoBehaviour
{
    public enum EntityState
    {
        ALIVE,
        DEAD
    }

    [Header("Entity Settings")]
    public float maxHealth = GameConstants.DEFAULT_MAX_HEALTH;
    public EntityState currentState = EntityState.ALIVE;
    public bool onDuty = false;

    [Header("Components")]
    public Rigidbody2D Rb2D { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    public BoxCollider2D BoxCollider { get; private set; }

    public float Health { get; protected set; }

    public bool IsActiveAndOnDuty => currentState == EntityState.ALIVE && onDuty;
    public bool CanTakeDamage => currentState == EntityState.ALIVE;

    protected virtual void Awake()
    {
        // Cache required components
        Rb2D = GetComponent<Rigidbody2D>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        BoxCollider = GetComponent<BoxCollider2D>();

        // Initialize health
        Health = maxHealth;

        // Auto-configure components to work together
        ComponentHelper.AutoConfigureEntity(this, IsStaticEntity());
    }

    /// <summary>
    /// Override in derived classes to specify if this entity should be static.
    /// Static entities (like dummy enemies) use Kinematic rigidbodies.
    /// </summary>
    protected virtual bool IsStaticEntity() => false;

    public virtual void TakeDamage(float amount)
    {
        if (!CanTakeDamage) return;

        Health -= amount;
        DebugHelper.LogCombat($"{gameObject.name} took {amount} damage ({Health:F1}/{maxHealth:F1} HP)");

        if (Health <= 0f) Die();
    }

    public virtual void SetState(EntityState newState)
    {
        EntityState previousState = currentState;
        currentState = newState;
        OnStateChanged(previousState, newState);
    }

    public virtual void SetDutyState(bool isOnDuty)
    {
        bool previousDuty = onDuty;
        onDuty = isOnDuty;
        OnDutyStateChanged(previousDuty, isOnDuty);
    }

    protected virtual void OnStateChanged(EntityState from, EntityState to)
    {
        // Override in derived classes for state-specific behaviour
    }

    protected virtual void OnDutyStateChanged(bool fromDuty, bool toDuty)
    {
        // Override in derived classes for duty-specific behaviour
    }

    protected virtual void Die()
    {
        DebugHelper.LogState($"{gameObject.name} died!");
        SetState(EntityState.DEAD);

        // Disable collider but keep the GameObject for death feedback
        if (BoxCollider != null) BoxCollider.enabled = false;

        // Stop physics
        if (Rb2D != null)
        {
            Rb2D.linearVelocity = Vector2.zero;
            Rb2D.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}