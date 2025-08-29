using UnityEngine;

/// <summary>
/// The player entity. Holds references to movement and attack components,
/// wires managers (via GameManager) and ensures attacks reset on damage.
/// </summary>
[RequireComponent(typeof(PlayerMover2D), typeof(PlayerAttackController))]
public class Player : Entity
{
    [Header("Manager References")]
    public InputManager inputManager;
    public TimeManager timeManager;

    [Header("Player Components")]
    public Weapon playerWeapon;

    private PlayerMover2D mover;
    private PlayerAttackController attackController;

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

        // Self-register as main player with GameManager (if GameManager implements it)
        GameManager.Instance?.RegisterPlayer(this);

        // Set duty state now that things are wired
        SetDutyState(true);

        // Attempt to register attack controller; controller is idempotent and safe to call now
        attackController?.Register(inputManager, timeManager);
    }

    private void OnEnable()
    {
        // Safe: registration is idempotent in controller (will noop if already registered or if managers missing)
        attackController?.Register(inputManager, timeManager);
    }

    private void OnDisable()
    {
        // Safe: Unregister will only unsubscribe if it previously subscribed to that exact input manager.
        attackController?.Unregister(inputManager);
    }

    protected override void OnDutyStateChanged(bool fromDuty, bool toDuty)
    {
        base.OnDutyStateChanged(fromDuty, toDuty);

        // Update weapon visual/active state
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

    // Ensure the player's attack combo resets if they take damage
    public override void TakeDamage(float amount)
    {
        base.TakeDamage(amount);

        // Reset combos and abort any ongoing strong-lock when hit
        attackController?.ResetCombo();
        attackController?.AbortAttack();
    }

    // Method for GameManager to control player duty state
    public void SetPlayerDuty(bool isOnDuty)
    {
        SetDutyState(isOnDuty);
    }
}