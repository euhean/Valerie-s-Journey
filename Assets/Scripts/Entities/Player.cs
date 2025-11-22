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
        
        // 1. Get weapon GameObject FIRST
        playerWeapon ??= GetComponentInChildren<Weapon>();
        GameObject weaponGO = playerWeapon?.gameObject;
        
        // 2. Get other components
        mover = GetComponent<PlayerMover2D>();
        attackController = GetComponent<PlayerAttackController>();
        
        // Acquire managers from GameManager if not set in inspector
        // IMPORTANT: Must be in Awake (not Start) so they're available for OnEnable subscriptions
        inputManager ??= GameManager.Instance?.inputManager;
        timeManager  ??= GameManager.Instance?.timeManager;
        
        // 3. Pass weapon GameObject and managers to components
        if (mover != null)
        {
            mover.weapon = weaponGO?.GetComponent<Weapon>();
            mover.attackController = attackController;
            if (inputManager != null) 
                mover.inputManager = inputManager;
        }
        
        if (attackController != null)
        {
            attackController.SetWeapon(weaponGO?.GetComponent<Weapon>());
        }
        
        // Log errors if managers are still null after acquisition
        if (GameManager.Instance == null)
            DebugHelper.LogError("[Player] GameManager.Instance is null during Awake!");
        if (inputManager == null)
            DebugHelper.LogError("[Player] InputManager not found. Input will not work.");
        if (timeManager == null)
            DebugHelper.LogError("[Player] TimeManager not found. Timing will not work.");
    }

    private void Start()
    {
        // Self-register as main player
        GameManager.Instance?.RegisterPlayer(this);

        // Set duty state now that things are wired
        SetDutyState(true);
    }

    private void OnEnable()
    {
        // PlayerAttackController manages its own event subscriptions in OnEnable
    }

    private void OnDisable()
    {
        // PlayerAttackController manages its own event unsubscriptions in OnDisable
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
            AnimationHelper.ShowHitFlash(SpriteRenderer, GameConstants.PLAYER_DAMAGE_FLASH_COLOR, GameConstants.HIT_FLASH_DURATION);
        }

        // Reset combos and abort any ongoing strong-lock when hit
        attackController?.ResetCombo();
        attackController?.AbortAttack();
    }

    protected override void Die()
    {
        DebugHelper.LogState($"{gameObject.name} died!");
        SetState(EntityState.DEAD);

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
    #endregion

    #region Public API
    // Method for GameManager to control player duty state
    public void SetPlayerDuty(bool isOnDuty) => SetDutyState(isOnDuty);
    #endregion
}