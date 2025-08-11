using UnityEngine;

/// <summary>
/// Helper for automatic component configuration and sizing.
/// Keeps component setup logic out of Entity classes.
/// </summary>
public static class ComponentHelper {
    
    /// <summary>
    /// Auto-configures BoxCollider2D to match SpriteRenderer bounds and centers everything.
    /// Call this after assigning a sprite to ensure proper alignment.
    /// </summary>
    public static void AutoConfigureColliderToSprite(SpriteRenderer spriteRenderer, BoxCollider2D boxCollider) {
        if (spriteRenderer == null || boxCollider == null) {
            DebugHelper.LogWarning("ComponentHelper: Cannot auto-configure - missing SpriteRenderer or BoxCollider2D");
            return;
        }
        
        if (spriteRenderer.sprite == null) {
            DebugHelper.LogWarning("ComponentHelper: Cannot auto-configure - SpriteRenderer has no sprite assigned");
            return;
        }
        
        // Get sprite bounds in local space (without transform scaling)
        Bounds spriteBounds = spriteRenderer.sprite.bounds;
        
        // Set collider size to match sprite bounds (in local space)
        boxCollider.size = spriteBounds.size;
        
        // Center the collider (offset from transform center)
        Vector3 spriteCenter = spriteRenderer.sprite.bounds.center;
        boxCollider.offset = spriteCenter;
        
        // Only log if debugging component setup
        if (DebugHelper.enableStateLogs) {
            DebugHelper.LogState($"Auto-configured {boxCollider.name}: size={boxCollider.size}, offset={boxCollider.offset}");
        }
    }
    
    /// <summary>
    /// Sets up Rigidbody2D with sensible defaults for entities.
    /// </summary>
    public static void ConfigureEntityRigidbody(Rigidbody2D rb2D, bool isStatic = false) {
        if (rb2D == null) {
            DebugHelper.LogWarning("ComponentHelper: Cannot configure - missing Rigidbody2D");
            return;
        }
        
        if (isStatic) {
            rb2D.bodyType = RigidbodyType2D.Kinematic;
            rb2D.linearVelocity = Vector2.zero;
            rb2D.angularVelocity = 0f;
            rb2D.gravityScale = 0f; // No gravity for top-down
        } else {
            rb2D.bodyType = RigidbodyType2D.Dynamic;
            rb2D.gravityScale = 0f; // Top-down game, no gravity
            rb2D.linearDamping = GameConstants.RIGIDBODY_LINEAR_DAMPING;
            rb2D.angularDamping = GameConstants.RIGIDBODY_ANGULAR_DAMPING;
            rb2D.freezeRotation = false; // Allow rotation for aiming
        }
        
        // Only log if debugging component setup
        if (DebugHelper.enableStateLogs) {
            DebugHelper.LogState($"Configured Rigidbody2D for {rb2D.name}: type={rb2D.bodyType}, gravity={rb2D.gravityScale}");
        }
    }
    
    /// <summary>
    /// Auto-configures all Entity components to work together properly.
    /// Call this in Entity.Awake() after all components are cached.
    /// </summary>
    public static void AutoConfigureEntity(Entity entity, bool isStaticEntity = false) {
        if (entity == null) {
            DebugHelper.LogWarning("ComponentHelper: Cannot configure - entity is null");
            return;
        }
        
        // Configure rigidbody
        if (entity.rb2D != null) {
            ConfigureEntityRigidbody(entity.rb2D, isStaticEntity);
        }
        
        // Auto-size collider to sprite
        if (entity.spriteRenderer != null && entity.boxCollider != null) {
            AutoConfigureColliderToSprite(entity.spriteRenderer, entity.boxCollider);
        }
        
        // Only log summary if debugging component setup
        if (DebugHelper.enableStateLogs) {
            DebugHelper.LogState($"Auto-configured all components for {entity.name}");
        }
    }
}