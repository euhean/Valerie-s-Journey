using UnityEngine;

/// <summary>
/// Helper for automatic component configuration and sizing.
/// Keeps component setup logic out of Entity classes.
/// </summary>
public static class ComponentHelper
{
    #region Collider <-> Sprite

    /// <summary>
    /// Auto-configures BoxCollider2D to match SpriteRenderer bounds (sprite-local)
    /// and centers everything.
    /// </summary>
    public static void AutoConfigureColliderToSprite(SpriteRenderer spriteRenderer, BoxCollider2D boxCollider)
    {
        if (spriteRenderer == null || boxCollider == null)
        {
            DebugHelper.LogWarning("ComponentHelper: Cannot auto-configure - missing SpriteRenderer or BoxCollider2D");
            return;
        }
        if (spriteRenderer.sprite == null)
        {
            DebugHelper.LogWarning("ComponentHelper: Cannot auto-configure - SpriteRenderer has no sprite assigned");
            return;
        }

        // sprite.bounds is in sprite-local units
        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        boxCollider.size   = (Vector2)spriteBounds.size;
        boxCollider.offset = (Vector2)spriteBounds.center;

        if (spriteRenderer.transform.lossyScale != Vector3.one && DebugHelper.enableStateLogs)
            DebugHelper.LogState($"ComponentHelper: Non-uniform scale {spriteRenderer.transform.lossyScale} — collider matches sprite-local size.");

        if (DebugHelper.enableStateLogs)
            DebugHelper.LogState(() => $"Auto-configured {boxCollider.name}: size={boxCollider.size}, offset={boxCollider.offset}");
    }

    #endregion

    #region Rigidbody2D Presets

    /// <summary>
    /// Sets up Rigidbody2D with sensible defaults for top-down entities.
    /// </summary>
    public static void ConfigureEntityRigidbody(Rigidbody2D rb, bool isStatic = false)
    {
        if (rb == null)
        {
            DebugHelper.LogWarning("ComponentHelper: Cannot configure - missing Rigidbody2D");
            return;
        }

        rb.gravityScale = 0f;

        if (isStatic)
        {
            rb.bodyType       = RigidbodyType2D.Kinematic;
            rb.velocity       = Vector2.zero;
            rb.angularVelocity= 0f;
            rb.freezeRotation = true;
        }
        else
        {
            rb.bodyType       = RigidbodyType2D.Dynamic;
            rb.drag           = GameConstants.RIGIDBODY_LINEAR_DAMPING;
            rb.angularDrag    = GameConstants.RIGIDBODY_ANGULAR_DAMPING;
            rb.freezeRotation = false;
        }

        if (DebugHelper.enableStateLogs)
            DebugHelper.LogState(() => $"Configured Rigidbody2D for {rb.name}: type={rb.bodyType}, drag={rb.drag}, angDrag={rb.angularDrag}");
    }

    #endregion

    #region One-shot Entity Setup

    /// <summary>
    /// Auto-configures all Entity components to work together properly.
    /// Call from Entity.Awake() after caching components.
    /// </summary>
    public static void AutoConfigureEntity(Entity entity, bool isStaticEntity = false)
    {
        if (entity == null)
        {
            DebugHelper.LogWarning("ComponentHelper: Cannot configure - entity is null");
            return;
        }

        if (entity.Rb2D != null)
            ConfigureEntityRigidbody(entity.Rb2D, isStaticEntity);

        if (entity.SpriteRenderer != null && entity.BoxCollider != null)
            AutoConfigureColliderToSprite(entity.SpriteRenderer, entity.BoxCollider);

        if (DebugHelper.enableStateLogs)
            DebugHelper.LogState($"Auto-configured all components for {entity.name}");
    }

    #endregion
}