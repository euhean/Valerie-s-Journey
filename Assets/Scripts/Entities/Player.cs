using UnityEngine;

/// <summary>
/// Player entity. Integrates InputManager, TimeManager, and handles visual feedback.
/// CRITICAL: Wired by GameManager during RegisterPlayer(). Death publishes PlayerDiedEvent.
/// </summary>
public class Player : Entity
{
    #region Inspector: Visual Settings
    [Header("Player Visuals")]
    [SerializeField] private Color aliveColor = Color.white;
    [SerializeField] private Color deadColor = new Color(0.5f, 0.5f, 0.5f);
    #endregion

    #region Dependencies (injected by GameManager)
    [HideInInspector] public InputManager inputManager;
    [HideInInspector] public TimeManager timeManager;
    #endregion

    #region Components (auto-discovered or assigned)
    private Weapon weapon;
    private PlayerMover2D mover;
    private PlayerAttackController attackController;
    #endregion

    #region Unity lifecycle
    protected override void Awake()
    {
        base.Awake();
        // Component discovery with fallback
        weapon = ComponentHelper.FindWeaponFallback("Player");
        mover = GetComponent<PlayerMover2D>();
        attackController = GetComponent<PlayerAttackController>();
    }

    protected override void Start()
    {
        base.Start(); // Auto-fit collider to sprite

        // CRITICAL: Wire managers into components (fallback if GameManager hasn't run yet)
        if (inputManager == null || timeManager == null)
        {
            var gm = GameManager.Instance;
            inputManager = gm?.inputManager;
            timeManager = gm?.timeManager;
        }

        if (mover != null)
            mover.inputManager = inputManager;

        // Initialize duty state after wiring complete
        SetDutyState(DutyState.OnDuty);
        UpdateVisuals(); // Apply initial visual state
    }

    private void Update()
    {
        // Weapon aiming: track cursor/stick direction when alive and on-duty
        if (!IsAlive || !IsOnDuty) return;

        if (weapon != null && inputManager != null)
        {
            Vector2 aimDir = inputManager.Aim;
            if (aimDir.sqrMagnitude > 0.01f)
                weapon.UpdateAiming(aimDir);
        }
    }
    #endregion

    #region Combat integration
    public override void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        base.TakeDamage(amount);

        // Damage feedback: flash sprite
        if (IsAlive && SpriteRenderer != null)
            AnimationHelper.ShowHitFlash(SpriteRenderer, GameConstants.PLAYER_DAMAGE_FLASH_COLOR, 
                                         GameConstants.PLAYER_DAMAGE_FLASH_DURATION, this);

        // Combo is NO LONGER reset on damage (User Request)
        // if (attackController != null)
        //    attackController.ResetCombo("took damage");
    }

    /// <summary>
    /// CRITICAL: Death flow: disable physics, toggle duty, publish death event, update visuals.
    /// </summary>
    protected override void Die()
    {
        if (!IsAlive) return;

        DebugHelper.LogState(() => $"Player {gameObject.name} died");
        DebugHelper.LogManager($"[Player] Die() invoked for {gameObject.name} at t={Time.time:F2}");
        base.Die();

        // Disable interaction: turn off collider and physics
        if (BoxCollider != null) BoxCollider.enabled = false;
        if (Rigidbody != null)
        {
            Rigidbody.linearVelocity = Vector2.zero;
            Rigidbody.simulated = false;
        }

        SetDutyState(DutyState.OffDuty);

        // Notify game systems of death
        EventBus.Instance?.Publish(new PlayerDiedEvent { player = this });

        UpdateVisuals(); // Apply death visuals
    }
    #endregion

    #region Visuals
    /// <summary>
    /// State change callback: refresh visuals on transitions
    /// </summary>
    protected override void OnStateChanged(EntityState from, EntityState to)
    {
        base.OnStateChanged(from, to);
        UpdateVisuals();
    }

    /// <summary>
    /// Enforces visual state: sprite visibility, color based on alive/dead state.
    /// </summary>
    private void UpdateVisuals()
    {
        if (SpriteRenderer == null) return;

        // Dead = gray and semi-transparent, Alive = normal color
        SpriteRenderer.color = IsAlive ? aliveColor : deadColor;
        SpriteRenderer.enabled = true; // Keep visible even when dead (for feedback)
    }
    #endregion
}