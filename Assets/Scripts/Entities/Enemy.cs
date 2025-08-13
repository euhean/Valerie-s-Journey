using UnityEngine;

/// <summary>
/// Basic enemy entity. Static target for testing combat mechanics and beat synchronization.
/// Takes damage and provides feedback when hit by player attacks.
/// </summary>
public class Enemy : Entity
{
    [Header("Enemy Visual Settings")]
    public Color aliveColor = GameConstants.ENEMY_ALIVE_COLOR;
    public Color deadColor = GameConstants.ENEMY_DEAD_COLOR;
    public Color hitFlashColor = GameConstants.HIT_FLASH_COLOR;
    public float hitFlashDuration = GameConstants.HIT_FLASH_DURATION;

    /// <summary>
    /// Enemies are static dummy targets - use kinematic rigidbodies
    /// </summary>
    protected override bool IsStaticEntity() => true;

    protected override void Awake()
    {
        base.Awake();
        // Set enemy tag for collision detection
        gameObject.tag = "Enemy";
        // Set initial visual state
        UpdateVisuals();
        // Start in off-duty state (static dummy)
        SetDutyState(false);
    }

    public override void TakeDamage(float amount)
    {
        if (currentState == EntityState.DEAD) return;
        base.TakeDamage(amount);
        // Determine if this was a strong attack based on damage amount
        bool isStrongAttack = amount >= GameConstants.STRONG_DAMAGE;
        // Delegate visual feedback to helper
        if (currentState != EntityState.DEAD)
        {
            if (isStrongAttack)
                AnimationHelper.ShowStrongHitShake(transform, spriteRenderer, hitFlashColor, hitFlashDuration);
            else AnimationHelper.ShowHitFlash(spriteRenderer, hitFlashColor, hitFlashDuration);
        }
    }

    protected override void OnStateChanged(EntityState from, EntityState to)
    {
        base.OnStateChanged(from, to);
        UpdateVisuals();
        switch (to)
        {
            case EntityState.DEAD:
                // Trigger death label
                ShowDeathLabel();
                break;
            case EntityState.ALIVE:
                // Reset to alive state
                break;
        }
    }

    protected override void OnDutyStateChanged(bool fromDuty, bool toDuty)
    {
        base.OnDutyStateChanged(fromDuty, toDuty);
        // Handle duty-specific behavior
        if (toDuty && currentState == EntityState.ALIVE)
            DebugHelper.LogState($"{gameObject.name} is now on patrol duty");
        else DebugHelper.LogState($"{gameObject.name} is now off duty (static)");
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        switch (currentState)
        {
            case EntityState.DEAD:
                spriteRenderer.color = deadColor;
                break;
            case EntityState.ALIVE:
                spriteRenderer.color = aliveColor;
                break;
        }
    }

    private void ShowDeathLabel()
    {
        // Show simple death feedback for alpha testing
        AnimationHelper.ShowDeath(transform.position);
    }
}