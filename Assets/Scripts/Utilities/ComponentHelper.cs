using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>
/// Helper for automatic component configuration and sizing.
/// Keeps component setup logic out of Entity classes.
/// </summary>
public static class ComponentHelper
{
    #region Weapon Fallback Assignment

    /// <summary>
    /// Centralized weapon finding logic to eliminate duplication.
    /// Uses tag-based search first, then scene-wide search.
    /// </summary>
    public static Weapon FindWeaponFallback(string debugContext = "")
    {
        // Strategy 1: Tag-based (fastest)
        try
        {
            GameObject weaponObj = GameObject.FindGameObjectWithTag("Weapon");
            if (weaponObj != null)
            {
                Weapon w = weaponObj.GetComponent<Weapon>();
                if (w != null) return w;
            }
        }
        catch (UnityException ex)
        {
            DebugHelper.LogWarning($"[{debugContext}] Tag 'Weapon' not defined: {ex.Message}. Falling back to scene search.");
        }

        // Strategy 2: Scene-wide search
        Weapon weapon = Object.FindFirstObjectByType<Weapon>();
        if (weapon != null) return weapon;

        // Strategy 3: Not found
        if (!string.IsNullOrEmpty(debugContext))
            DebugHelper.LogWarning($"[{debugContext}] No Weapon found. Create a separate Weapon GameObject in the scene or assign it in Inspector.");
        
        return null;
    }

    #endregion

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
        boxCollider.isTrigger = false; // Ensure it's a solid collider, not a trigger

        if (spriteRenderer.transform.lossyScale != Vector3.one && DebugHelper.StateLogsEnabled)
            DebugHelper.LogState(() => $"ComponentHelper: Non-uniform scale {spriteRenderer.transform.lossyScale} — collider matches sprite-local size.");

        if (DebugHelper.StateLogsEnabled)
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
            rb.bodyType         = RigidbodyType2D.Kinematic;
            rb.linearVelocity   = Vector2.zero;
            rb.angularVelocity  = 0f;
            rb.freezeRotation   = true;
        }
        else
        {
            rb.bodyType         = RigidbodyType2D.Dynamic;
            rb.linearDamping    = GameConstants.RIGIDBODY_LINEAR_DAMPING;
            rb.angularDamping   = GameConstants.RIGIDBODY_ANGULAR_DAMPING;
            rb.freezeRotation   = false;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // Prevent wall clipping
        }

        if (DebugHelper.StateLogsEnabled)
            DebugHelper.LogState(() => $"Configured Rigidbody2D for {rb.name}: type={rb.bodyType}, linearDamping={rb.linearDamping}, angularDamping={rb.angularDamping}");
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

        if (DebugHelper.StateLogsEnabled)
            DebugHelper.LogState(() => $"Auto-configured all components for {entity.name}");
    }

    #endregion

    #region UI Helpers

    /// <summary>
    /// Creates a full-screen Canvas with proper setup for overlay UI.
    /// </summary>
    public static GameObject CreateFullScreenCanvas(string name, int sortingOrder = 1000)
    {
        GameObject canvasGO = new GameObject(name);
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();
        
        // Ensure EventSystem exists for UI navigation
        if (EventSystem.current == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            EventSystem eventSystem = eventSystemGO.AddComponent<EventSystem>();
            InputSystemUIInputModule inputModule = eventSystemGO.AddComponent<InputSystemUIInputModule>();
            
            // Find InputManager and configure UI actions
            InputManager inputManager = Object.FindFirstObjectByType<InputManager>();
            if (inputManager != null)
            {
                if (inputManager.navigateAction != null)
                    inputModule.move = inputManager.navigateAction;
                if (inputManager.submitAction != null)
                    inputModule.submit = inputManager.submitAction;
                if (inputManager.cancelAction != null)
                    inputModule.cancel = inputManager.cancelAction;
            }
        }
        
        return canvasGO;
    }

    /// <summary>
    /// Creates a full-screen panel with background color.
    /// </summary>
    public static GameObject CreateFullScreenPanel(GameObject parent, string name, Color backgroundColor)
    {
        GameObject panelGO = new GameObject(name);
        panelGO.transform.SetParent(parent.transform, false);
        
        Image panelImage = panelGO.AddComponent<Image>();
        panelImage.color = backgroundColor;

        RectTransform panelRect = panelGO.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        return panelGO;
    }

    /// <summary>
    /// Creates a text element with specified properties and anchoring.
    /// </summary>
    public static GameObject CreateText(GameObject parent, string name, string text, int fontSize, Color color, 
        TextAnchor alignment, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent.transform, false);
        
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = text;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = fontSize;
        textComponent.color = color;
        textComponent.alignment = alignment;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return textGO;
    }

    /// <summary>
    /// Creates a button with text and click action.
    /// </summary>
    public static GameObject CreateButton(GameObject parent, string name, string buttonText, Color backgroundColor, 
        Vector2 anchorMin, Vector2 anchorMax, System.Action onClick = null)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent.transform, false);

        // Button background
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = backgroundColor;

        // Button component
        Button button = buttonGO.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        if (onClick != null)
            button.onClick.AddListener(() => onClick.Invoke());

        // Set up navigation colors
        var colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = new Color(0.5f, 0.5f, 0.8f, 0.9f);
        colors.selectedColor = new Color(0.5f, 0.5f, 0.8f, 0.9f);
        colors.pressedColor = new Color(0.3f, 0.3f, 0.6f, 0.9f);
        button.colors = colors;

        // Button positioning
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = Vector2.zero;
        buttonRect.offsetMax = Vector2.zero;

        // Button text
        GameObject textGO = new GameObject("ButtonText");
        textGO.transform.SetParent(buttonGO.transform, false);
        Text textComponent = textGO.AddComponent<Text>();
        textComponent.text = buttonText;
        textComponent.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        textComponent.fontSize = 24;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleCenter;

        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        return buttonGO;
    }

    #endregion

    #region UI Navigation Helpers

    /// <summary>
    /// Sets up Unity's built-in UI navigation for buttons and selects the first one.
    /// </summary>
    public static void SetupUINavigation(GameObject parent)
    {
        Button[] buttons = parent.GetComponentsInChildren<Button>();
        
        if (buttons.Length == 0) return;

        // Set up navigation between buttons
        for (int i = 0; i < buttons.Length; i++)
        {
            var nav = buttons[i].navigation;
            nav.mode = Navigation.Mode.Explicit;
            
            // Set left/right navigation for horizontal layout
            if (i > 0)
                nav.selectOnLeft = buttons[i - 1];
            if (i < buttons.Length - 1)
                nav.selectOnRight = buttons[i + 1];
                
            buttons[i].navigation = nav;
        }

        // Select the first button
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
        }
    }

    #endregion
}