using System.Collections;
using UnityEngine;

/// <summary>
/// Base class for all living game objects. Provides health/damage system,
/// entity states, and ensures required 2D components are present.
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(BoxCollider2D))]
public abstract class Entity : MonoBehaviour
{
    #region Types
    public enum EntityState { ALIVE, DEAD }
    #endregion

    #region Inspector
    [Header("Entity Settings")]
    [SerializeField] protected float maxHealth = GameConstants.DEFAULT_MAX_HEALTH;
    public EntityState currentState = EntityState.ALIVE;
    public bool onDuty = false;

    [Header("Components")]
    public Rigidbody2D Rb2D { get; private set; }
    public SpriteRenderer SpriteRenderer { get; private set; }
    public BoxCollider2D BoxCollider { get; private set; }
    #endregion

    #region Properties
    public float MaxHealth => maxHealth;
    public float Health { get; protected set; }
    public bool IsActiveAndOnDuty => currentState == EntityState.ALIVE && onDuty;
    public bool CanTakeDamage    => currentState == EntityState.ALIVE;
    #endregion

    #region Unity Lifecycle
    protected virtual void Awake()
    {
        // Cache required components
        Rb2D         = GetComponent<Rigidbody2D>();
        SpriteRenderer = GetComponent<SpriteRenderer>();
        BoxCollider  = GetComponent<BoxCollider2D>();

        // Initialize health
        Health = maxHealth;

        // Auto-configure components to work together
        ComponentHelper.AutoConfigureEntity(this, IsStaticEntity());
    }

    protected virtual void Start()
    {
        // Reconfigure after Inspector values are guaranteed to be loaded
        if (SpriteRenderer != null && SpriteRenderer.sprite != null && BoxCollider != null)
        {
            ComponentHelper.AutoConfigureColliderToSprite(SpriteRenderer, BoxCollider);
        }
    }
    #endregion

    #region Configuration
    /// <summary>
    /// Override in derived classes to specify if this entity should be static.
    /// Static entities (like dummy enemies) use Kinematic rigidbodies.
    /// </summary>
    protected virtual bool IsStaticEntity() => false;
    #endregion

    #region Damage & Death
    public virtual void TakeDamage(float amount)
    {
        if (!CanTakeDamage) return;

        Health -= amount;
        DebugHelper.LogCombat(() => $"{gameObject.name} took {amount} damage ({Health:F1}/{maxHealth:F1} HP)");

        if (Health <= 0f) Die();
    }

    protected virtual void Die()
    {
        DebugHelper.LogState(() => $"{gameObject.name} died!");
        SetState(EntityState.DEAD);

        // Disable collider immediately to stop interactions
        if (BoxCollider != null) BoxCollider.enabled = false;

        // Stop physics
        if (Rb2D != null)
        {
            Rb2D.linearVelocity = Vector2.zero;
            Rb2D.bodyType = RigidbodyType2D.Kinematic;
        }

        // Start death sequence: play animation then destroy
        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        // TODO: Play death animation here
        // yield return new WaitForSeconds(deathAnimationDuration);
        
        // For now, use text duration as placeholder for death animation timing
        yield return new WaitForSeconds(GameConstants.TEXT_DURATION);

        // Destroy the game object after animation completes
        Destroy(gameObject);
    }
    #endregion

    #region State & Duty
    public virtual void SetState(EntityState newState)
    {
        var previous = currentState;
        currentState = newState;
        OnStateChanged(previous, newState);
    }

    public virtual void SetDutyState(bool isOnDuty)
    {
        bool previous = onDuty;
        onDuty = isOnDuty;
        OnDutyStateChanged(previous, isOnDuty);
    }

    protected virtual void OnStateChanged(EntityState from, EntityState to) { /* override in child */ }
    protected virtual void OnDutyStateChanged(bool fromDuty, bool toDuty)   { /* override in child */ }
    #endregion
}