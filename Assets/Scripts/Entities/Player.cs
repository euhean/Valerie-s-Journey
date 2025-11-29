using UnityEngine;

/// <summary>
/// The player entity. Holds references to movement and attack components,
/// wires managers (via GameManager) and ensures attacks reset on damage.
/// </summary>
[RequireComponent(typeof(PlayerMover2D), typeof(PlayerAttackController))]
public class Player : Entity
{
    #region Inspector: Manager Refs
    [Header("Manager References")]
    public InputManager inputManager;
    public TimeManager timeManager;
    #endregion

    #region Inspector: Components
    [Header("Player Components")]
    public Weapon playerWeapon;
    #endregion

    #region Cached Components
    private PlayerMover2D mover;
    private PlayerAttackController attackController;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {
        base.Awake();
        mover = GetComponent<PlayerMover2D>();
        attackController = GetComponent<PlayerAttackController>();

        // Use centralized weapon fallback logic
        playerWeapon ??= ComponentHelper.FindWeaponFallback("Player");
    }

    protected override void Start()
    {
        // CRITICAL: Call base.Start() for proper collider configuration
        base.Start();
        
        // Acquire managers from GameManager if not set in inspector
        inputManager ??= GameManager.Instance?.inputManager;
        timeManager  ??= GameManager.Instance?.timeManager;

        // Give mover the inputManager so it can run in FixedUpdate
        if (mover != null) mover.inputManager = inputManager;

        // Self-register as main player
        GameManager.Instance?.RegisterPlayer(this);

        // Set duty state now that things are wired
        SetDutyState(true);

        // Ensure sprite is visible
        UpdateVisuals();

        // Idempotent registration - handled automatically by PlayerAttackController in OnEnable
        // attackController subscribes to inputManager and timeManager events in its OnEnable
    }

    private void OnEnable()
    {
        // Event subscription handled automatically by PlayerAttackController in OnEnable
        // attackController subscribes to inputManager and timeManager events
    }

    private void OnDisable()
    {
        // Event unsubscription handled automatically by PlayerAttackController in OnDisable
        // attackController unsubscribes from inputManager and timeManager events
    }
    #endregion

    #region Visuals
    private void UpdateVisuals()
    {
        if (SpriteRenderer == null) 
        {
            DebugHelper.LogWarning("[Player] SpriteRenderer is null!");
            return;
        }
        
        SpriteRenderer.enabled = true;  // Make sure SpriteRenderer is enabled
        SpriteRenderer.color = Color.white; // Player is always visible and white
        
        // Debug sprite state
        DebugHelper.LogState(() => $"[Player] Sprite: {(SpriteRenderer.sprite ? SpriteRenderer.sprite.name : "NULL")}, Color: {SpriteRenderer.color}, Enabled: {SpriteRenderer.enabled}");
    }
    #endregion

    #region Duty / Aiming
    protected override void OnDutyStateChanged(bool fromDuty, bool toDuty)
    {
        base.OnDutyStateChanged(fromDuty, toDuty);
        playerWeapon?.SetOwnerDutyState(toDuty);
    }

    private void Update()
    {
        // Update weapon aiming with right stick input
        if (playerWeapon != null && inputManager != null && onDuty && currentState == EntityState.ALIVE)
        {
            playerWeapon.UpdateAiming(inputManager.Aim);
        }
    }
    #endregion

    #region Damage Handling
    public override void TakeDamage(float amount)
    {
        if (currentState == EntityState.DEAD) return;

        base.TakeDamage(amount);

        // Show damage flash effect for better combat clarity
        if (currentState != EntityState.DEAD)
        {
            AnimationHelper.ShowHitFlash(SpriteRenderer, GameConstants.PLAYER_DAMAGE_FLASH_COLOR, GameConstants.HIT_FLASH_DURATION, this);
        }

        // Reset combos and abort any ongoing strong-lock when hit
        attackController?.ResetCombo("took damage");
        attackController?.AbortAttack();
    }

    protected override void Die()
    {
        DebugHelper.LogState(() => $"{gameObject.name} died!");
        SetState(EntityState.DEAD);

        // Update visuals for death state
        UpdateVisuals();

        // Disable collider and stop physics immediately
        if (BoxCollider != null) BoxCollider.enabled = false;
        if (Rb2D != null)
        {
            Rb2D.linearVelocity = Vector2.zero;
            Rb2D.bodyType = RigidbodyType2D.Kinematic;
        }

        // Set player off duty to stop all interactions
        SetDutyState(false);

        // Trigger death event instead of destroying the player
        EventBus.Instance?.Publish(new PlayerDiedEvent { player = this });
    }

    protected override void OnStateChanged(EntityState from, EntityState to)
    {
        base.OnStateChanged(from, to);
        UpdateVisuals(); // Ensure sprite visibility is maintained
    }
    #endregion

    #region Public API
    // Method for GameManager to control player duty state
    public void SetPlayerDuty(bool isOnDuty) => SetDutyState(isOnDuty);
    #endregion
}