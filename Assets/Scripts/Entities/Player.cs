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

        // Find weapon in children if not assigned
        playerWeapon ??= GetComponentInChildren<Weapon>(true);
    }

    private void Start()
    {
        // Acquire managers from GameManager if not set in inspector
        inputManager ??= GameManager.Instance?.inputManager;
        timeManager  ??= GameManager.Instance?.timeManager;

        // Give mover the inputManager so it can run in FixedUpdate
        if (mover != null) mover.inputManager = inputManager;

        // Self-register as main player
        GameManager.Instance?.RegisterPlayer(this);

        // Set duty state now that things are wired
        SetDutyState(true);

        // Idempotent registration
        attackController?.Register(inputManager, timeManager);
    }

    private void OnEnable()
    {
        // Safe: will noop if already registered or managers missing
        attackController?.Register(inputManager, timeManager);
    }

    private void OnDisable()
    {
        // Safe: only unsubscribes if it was subscribed to this input
        attackController?.Unregister(inputManager);
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
        base.TakeDamage(amount);

        // Reset combos and abort any ongoing strong-lock when hit
        attackController?.ResetCombo();
        attackController?.AbortAttack();
    }
    #endregion

    #region Public API
    // Method for GameManager to control player duty state
    public void SetPlayerDuty(bool isOnDuty) => SetDutyState(isOnDuty);
    #endregion
}