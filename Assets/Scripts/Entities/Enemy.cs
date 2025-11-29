using System.Collections;
using UnityEngine;

/// <summary>
/// Basic enemy entity. Static target for testing combat mechanics and beat synchronization.
/// Patrols the player when on-duty and deals melee damage at range.
/// </summary>
public class Enemy : Entity
{
    private static WaitForSeconds _waitForSeconds0_25 = new(0.25f);
    private static WaitForSeconds _waitForSeconds1_0 = new(1.0f);
    
    #region Inspector: Visuals
    [Header("Enemy Visual Settings")]
    public Color aliveColor = GameConstants.ENEMY_ALIVE_COLOR;
    public Color deadColor  = GameConstants.ENEMY_DEAD_COLOR;
    public Color hitFlashColor = GameConstants.ENEMY_DAMAGE_FLASH_COLOR; // More noticeable bright yellow
    public float hitFlashDuration = GameConstants.HIT_FLASH_DURATION; // Now 0.2f for longer visibility
    #endregion

    #region Inspector: Patrol / Combat
    [Header("Patrol / Combat")]
    public Player playerTarget;
    public float patrolSpeed   = 1.5f;
    public float attackRange   = 0.6f;
    public float attackDamage  = 8f;
    public float attackCooldown = 1.0f;
    private Coroutine patrolCoroutine;
    private float lastAttackTime = -999f;
    #endregion

    #region Unity Lifecycle
    protected override void Awake()
    {  
        base.Awake();
        gameObject.tag = "Enemy";
        UpdateVisuals();
        SetDutyState(DutyState.OnDuty); // Start enemies on patrol duty
    }

    protected override void Start()
    {
        // CRITICAL: Call base.Start() for proper collider configuration
        base.Start();
        playerTarget ??= GameManager.Instance?.MainPlayer ?? FindFirstObjectByType<Player>();
        StartPatrol();
        EventBus.Instance.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
    }

    private void OnDestroy()
    {
        EventBus.Instance.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
    }
    #endregion

    #region Event Handlers
    private void OnPlayerSpawned(PlayerSpawnedEvent e) => playerTarget = e.player;
    #endregion

    #region Combat / Damage
    public override void TakeDamage(float amount)
    {
        if (CurrentState == EntityState.Dead) return;

        base.TakeDamage(amount);

        bool isStrongAttack = amount >= GameConstants.STRONG_DAMAGE;
        if (CurrentState != EntityState.Dead)
        {
            if (isStrongAttack)
                AnimationHelper.ShowStrongHitShake(transform, SpriteRenderer, hitFlashColor, hitFlashDuration, this);
            else
                AnimationHelper.ShowHitFlash(SpriteRenderer, hitFlashColor, hitFlashDuration, this);
        }
    }
    #endregion

    #region Duty / Patrol Toggle
    protected override void OnStateChanged(EntityState from, EntityState to)
    {
        UpdateVisuals();
        if (CurrentState == EntityState.Dead)
        {
            StopPatrol();
            DebugHelper.LogState(() => $"{gameObject.name} is now off duty (static)");
            ShowDeathLabel();
        }
    }
    #endregion

    #region Visuals
    private void UpdateVisuals()
    {
        if (SpriteRenderer == null) return;
        SpriteRenderer.color = (CurrentState == EntityState.Alive) ? aliveColor : deadColor;
    }

    private void ShowDeathLabel()
    {
        AnimationHelper.ShowDeath(transform.position);
    }
    #endregion

    #region Patrol / Melee
    public void StartPatrol()
    {
        if (patrolCoroutine != null) return;
        patrolCoroutine = StartCoroutine(PatrolRoutine());
    }

    public void StopPatrol()
    {
        if (patrolCoroutine == null) return;
        StopCoroutine(patrolCoroutine);
        patrolCoroutine = null;
    }

    private IEnumerator PatrolRoutine()
    {
        if (Rigidbody == null) yield break;

        while (CurrentDutyState == DutyState.OnDuty && CurrentState == EntityState.Alive)
        {
            // Check if target is missing or dead
            if (playerTarget == null || !playerTarget.IsAlive)
            {
                // Try to refresh target from GameManager
                playerTarget = GameManager.Instance?.MainPlayer;
                
                // If still null or dead, try search
                if (playerTarget == null || !playerTarget.IsAlive)
                {
                    playerTarget = FindFirstObjectByType<Player>();
                }

                // If still invalid, wait longer to avoid performance spam
                if (playerTarget == null || !playerTarget.IsAlive)
                {
                    yield return _waitForSeconds1_0;
                    continue;
                }
            }

            Vector2 toTarget = playerTarget.transform.position - transform.position;
            float distance = toTarget.magnitude;

            if (distance > attackRange)
            {
                Vector2 dir = toTarget.normalized;
                Vector2 next = Rigidbody.position + dir * (patrolSpeed * Time.deltaTime);
                Rigidbody.MovePosition(next);
            }
            else
            {
                TryAttack();
            }

            yield return null;
        }

        patrolCoroutine = null;
    }

    private void TryAttack()
    {
        if (playerTarget == null) return;
        if (Time.time - lastAttackTime < attackCooldown) return;

        lastAttackTime = Time.time;

        playerTarget.TakeDamage(attackDamage);
        DebugHelper.LogCombat($"{gameObject.name} hit {playerTarget.name} for {attackDamage} damage");

        AnimationHelper.ShowHitFlash(SpriteRenderer, hitFlashColor, hitFlashDuration, this);
    }
    #endregion
}