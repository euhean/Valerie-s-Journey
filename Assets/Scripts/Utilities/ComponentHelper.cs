using UnityEngine;

/// <summary>
/// Helper for automatic component configuration and sizing.
/// Keeps component setup logic out of Entity classes.
/// </summary>
public static class ComponentHelper
{
    /// <summary>
    /// Auto-configures BoxCollider2D to match SpriteRenderer bounds and centers everything.
    /// Call this after assigning a sprite to ensure proper alignment.
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

        // Get sprite bounds in local space (sprite.bounds is in sprite-local units)
        Bounds spriteBounds = spriteRenderer.sprite.bounds;

        // BoxCollider2D.size/offset are Vector2
        Vector2 size = (Vector2)spriteBounds.size;
        Vector2 center = (Vector2)spriteBounds.center;

        boxCollider.size = size;
        boxCollider.offset = center;

        // Warn if transform is scaled (lossyScale != 1) since collider may not visually match in world space
        if (spriteRenderer.transform.lossyScale != Vector3.one && DebugHelper.enableStateLogs)
        {
            DebugHelper.LogState($"ComponentHelper: Sprite has non-1 scale ({spriteRenderer.transform.lossyScale}). Collider set to sprite-local size; consider adjusting for scale if visuals mismatch.");
        }

        if (DebugHelper.enableStateLogs)
            DebugHelper.LogState($"Auto-configured {boxCollider.name}: size={boxCollider.size}, offset={boxCollider.offset}");
    }

    /// <summary>
    /// Sets up Rigidbody2D with sensible defaults for entities.
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
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.velocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.freezeRotation = true;
        }
        else
        {
            rb.bodyType = RigidbodyType2D.Dynamic;

            rb.drag = GameConstants.RIGIDBODY_LINEAR_DAMPING;
            rb.angularDrag = GameConstants.RIGIDBODY_ANGULAR_DAMPING;

            rb.freezeRotation = false;
        }

        if (DebugHelper.enableStateLogs)
            DebugHelper.LogState($"Configured Rigidbody2D for {rb.name}: type={rb.bodyType}, gravity={rb.gravityScale}, drag={rb.drag}");
    }

    /// <summary>
    /// Auto-configures all Entity components to work together properly.
    /// Call this in Entity.Awake() after all components are cached.
    /// </summary>
    public static void AutoConfigureEntity(Entity entity, bool isStaticEntity = false)
    {
        if (entity == null)
        {
            DebugHelper.LogWarning("ComponentHelper: Cannot configure - entity is null");
            return;
        }

        // Use the property names from your Entity class (Rb2D, SpriteRenderer, BoxCollider)
        if (entity.Rb2D != null) ConfigureEntityRigidbody(entity.Rb2D, isStaticEntity);

        if (entity.SpriteRenderer != null && entity.BoxCollider != null)
            AutoConfigureColliderToSprite(entity.SpriteRenderer, entity.BoxCollider);

        if (DebugHelper.enableStateLogs)
            DebugHelper.LogState($"Auto-configured all components for {entity.name}");
    }
}