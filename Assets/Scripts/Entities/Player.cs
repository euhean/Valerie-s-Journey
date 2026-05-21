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
        
        playerWeapon ??= GetComponentInChildren<Weapon>();
        mover = GetComponent<PlayerMover2D>();
        attackController = GetComponent<PlayerAttackController>();
        
        if (mover != null)
        {
            mover.weapon = playerWeapon;
            mover.attackController = attackController;
        }

        if (attackController != null)
            attackController?.SetWeapon(weaponComponent);
    }

    private void Start()
    {
        SetDutyState(true);
    }
    #endregion

    #region Duty / Aiming
    protected override void OnDutyStateChanged(bool fromDuty, bool toDuty)
    {
        base.OnDutyStateChanged(fromDuty, toDuty);
        playerWeapon?.SetOwnerDutyState(toDuty);
    }
    #endregion

    #region Damage Handling
    public override void TakeDamage(float amount)
    {
        if (currentState == EntityState.DEAD) return;

        base.TakeDamage(amount);

        // Show damage flash effect for better combat clarity
        if (currentState != EntityState.DEAD)
            AnimationHelper.ShowHitFlash(SpriteRenderer, GameConstants.PLAYER_DAMAGE_FLASH_COLOR, GameConstants.HIT_FLASH_DURATION);

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
