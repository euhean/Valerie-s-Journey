using UnityEngine;

/// <summary>
/// The player entity. It holds references to its movement and attack components
/// and registers them with the input and rhythm managers. Also manages weapon
/// and duty state.
/// </summary>
[RequireComponent(typeof(PlayerMover2D), typeof(PlayerAttackController))]
public class Player : Entity
{
    [Header("Manager References")]
    public InputManager inputManager;
    public TimeManager  timeManager;

    [Header("Player Components")]
    public Weapon playerWeapon; // Assign in inspector or find in children

    private PlayerMover2D mover;
    private PlayerAttackController attackController;

    protected override void Awake()
    {
        base.Awake();

        mover = GetComponent<PlayerMover2D>();
        attackController = GetComponent<PlayerAttackController>();

        // Find weapon in children if not assigned
        if (playerWeapon == null)
            playerWeapon = GetComponentInChildren<Weapon>(true);
    }

    private void Start()
    {
        // Now it’s safe to arm the player (Weapon.Awake has completed)
        SetDutyState(true);
    }

    private void OnEnable()
    {
        // Register attack input when enabling the player
        if (attackController != null)
            attackController.Register(inputManager, timeManager);
        // Assign input manager to the mover (so movement works)
        if (mover != null)
            mover.inputManager = inputManager;
    }

    private void OnDisable()
    {
        // Unregister attack input when disabling the player
        if (attackController != null)
            attackController.Unregister(inputManager);
    }

    protected override void OnDutyStateChanged(bool fromDuty, bool toDuty)
    {
        base.OnDutyStateChanged(fromDuty, toDuty);
        // Update weapon state
        if (playerWeapon != null)
            playerWeapon.SetOwnerDutyState(toDuty);
    }

    private void Update()
    {
        // Update weapon aiming with right stick input
        if (playerWeapon != null && inputManager != null && onDuty && currentState == EntityState.ALIVE)
            playerWeapon.UpdateAiming(inputManager.Aim);
    }

    // Method for GameManager to control player duty state
    public void SetPlayerDuty(bool isOnDuty)
    {
        SetDutyState(isOnDuty);
    }
}