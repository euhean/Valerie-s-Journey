using UnityEngine;

/// <summary>
/// Player entity: health, death and visuals. Movement, aiming and attacks live in
/// PlayerMover2D / PlayerAttackController / Weapon. GameManager.RegisterPlayer wires
/// those components to the managers and configs.
/// </summary>
[RequireComponent(typeof(PlayerMover2D), typeof(PlayerAttackController))]
public class Player : Entity
{
    #region Inspector: Visual Settings
    [Header("Player Visuals")]
    [SerializeField] private Color aliveColor = Color.white;
    [SerializeField] private Color deadColor = new Color(0.5f, 0.5f, 0.5f);
    #endregion

    #region Components
    private Weapon weapon;
    private PlayerAttackController attackController;
    #endregion

    #region Unity lifecycle
    protected override void Awake()
    {
        base.Awake();
        weapon = GetComponentInChildren<Weapon>();
        attackController = GetComponent<PlayerAttackController>();
    }

    protected override void Start()
    {
        base.Start(); // Auto-fit collider to sprite
        SetDutyState(DutyState.OnDuty);
        UpdateVisuals();
    }
    #endregion

    #region Duty
    public override void SetDutyState(DutyState newState)
    {
        base.SetDutyState(newState);
        weapon.SetOwnerDutyState(IsOnDuty);
    }
    #endregion

    #region Combat integration
    public override void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        base.TakeDamage(amount);

        // Damage feedback: flash sprite and reset combo
        if (IsAlive)
            AnimationHelper.ShowHitFlash(SpriteRenderer, GameConstants.PLAYER_DAMAGE_FLASH_COLOR,
                                         GameConstants.PLAYER_DAMAGE_FLASH_DURATION, this);

        attackController.ResetCombo("took damage");
    }

    /// <summary>
    /// Death flow: base.Die goes off duty and switches to Dead (which refreshes visuals),
    /// then the open attack is cancelled, physics is switched off and PlayerDiedEvent is published.
    /// </summary>
    protected override void Die()
    {
        if (!IsAlive) return;

        base.Die();
        attackController.AbortAttack();

        BoxCollider.enabled = false;
        Rigidbody.linearVelocity = Vector2.zero;
        Rigidbody.simulated = false;

        EventBus.Instance.Publish(new PlayerDiedEvent { player = this });
    }
    #endregion

    #region Visuals
    protected override void OnStateChanged(EntityState from, EntityState to)
    {
        UpdateVisuals();
    }

    // Dead = gray, alive = normal color. The sprite stays visible when dead as feedback.
    private void UpdateVisuals()
    {
        AnimationHelper.CancelFlash(SpriteRenderer); // a running hit flash must not restore the old colour
        SpriteRenderer.color = IsAlive ? aliveColor : deadColor;
    }
    #endregion
}
