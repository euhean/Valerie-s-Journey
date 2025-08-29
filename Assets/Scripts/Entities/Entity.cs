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
    public Rigidbody2D rb2D { get; private set; }
    public SpriteRenderer spriteRenderer { get; private set; }
    public BoxCollider2D boxCollider { get; private set; }

    public float health { get; protected set; }

    public bool IsActiveAndOnDuty => currentState == EntityState.ALIVE && onDuty;
    public bool CanTakeDamage => currentState == EntityState.ALIVE;

    protected virtual void Awake()
    {
        // Cache required components
        rb2D = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCollider = GetComponent<BoxCollider2D>();

        // Initialize health
        health = maxHealth;

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

        health -= amount;
        DebugHelper.LogCombat($"{gameObject.name} took {amount} damage ({health:F1}/{maxHealth:F1} HP)");

        if (health <= 0f) Die();
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
        if (boxCollider != null) boxCollider.enabled = false;

        // Stop physics
        if (rb2D != null)
        {
            rb2D.linearVelocity = Vector2.zero;
            rb2D.bodyType = RigidbodyType2D.Kinematic;
        }
    }
}