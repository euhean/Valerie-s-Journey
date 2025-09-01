using System.Collections;
using UnityEngine;

/// <summary>
/// Basic enemy entity. Static target for testing combat mechanics and beat synchronization.
/// Patrols the player when on-duty and deals melee damage at range.
/// </summary>
public class Enemy : Entity
{
    [Header("Enemy Visual Settings")]
    public Color aliveColor = GameConstants.ENEMY_ALIVE_COLOR;
    public Color deadColor = GameConstants.ENEMY_DEAD_COLOR;
    public Color hitFlashColor = GameConstants.HIT_FLASH_COLOR;
    public float hitFlashDuration = GameConstants.HIT_FLASH_DURATION;

    [Header("Patrol / Combat")]
    public Player playerTarget;
    public float patrolSpeed = 1.5f;    
    public float attackRange = 0.6f;          
    public float attackDamage = 8f;           
    public float attackCooldown = 1.0f;       

    private Coroutine patrolCoroutine;
    private float lastAttackTime = -999f;
    protected override bool IsStaticEntity() => true;

    #region Unity lifecycle
    protected override void Awake()
    {
        base.Awake();
        gameObject.tag = "Enemy";
        UpdateVisuals();
        SetDutyState(false);
    }

    private void Start()
    {
        playerTarget ??= GameManager.Instance?.MainPlayer ?? FindObjectOfType<Player>();
        EventBus.Instance.Subscribe<PlayerSpawnedEvent>(OnPlayerSpawned);
    }

    private void OnPlayerSpawned(PlayerSpawnedEvent e) => playerTarget = e.player;
    private void OnDestroy() => EventBus.Instance.Unsubscribe<PlayerSpawnedEvent>(OnPlayerSpawned);

    public override void TakeDamage(float amount)
    {
        if (currentState == EntityState.DEAD) return;
        base.TakeDamage(amount);

        bool isStrongAttack = amount >= GameConstants.STRONG_DAMAGE;
        if (currentState != EntityState.DEAD)
        {
            if (isStrongAttack)
                AnimationHelper.ShowStrongHitShake(transform, spriteRenderer, hitFlashColor, hitFlashDuration);
            else
                AnimationHelper.ShowHitFlash(spriteRenderer, hitFlashColor, hitFlashDuration);
        }
    }

    protected override void OnStateChanged(EntityState from, EntityState to)
    {
        base.OnStateChanged(from, to);
        UpdateVisuals();
        if (to == EntityState.DEAD) ShowDeathLabel();
    }

    protected override void OnDutyStateChanged(bool fromDuty, bool toDuty)
    {
        base.OnDutyStateChanged(fromDuty, toDuty);

        if (toDuty && currentState == EntityState.ALIVE)
        {
            StartPatrol();
            DebugHelper.LogState($"{gameObject.name} is now on patrol duty");
        }
        else
        {
            StopPatrol();
            DebugHelper.LogState($"{gameObject.name} is now off duty (static)");
        }
    }

    private void UpdateVisuals()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.color = (currentState == EntityState.ALIVE) ? aliveColor : deadColor;
    }

    private void ShowDeathLabel()
    {
        AnimationHelper.ShowDeath(transform.position);
    }
    #endregion

    #region Patrol / Combat behavior
    public void StartPatrol()
    {
        if (patrolCoroutine != null) return;
        playerTarget ??= GameManager.Instance?.MainPlayer ?? FindObjectOfType<Player>();
        patrolCoroutine = StartCoroutine(PatrolRoutine());
    }

    public void StopPatrol()
    {
        if (patrolCoroutine != null)
        {
            StopCoroutine(patrolCoroutine);
            patrolCoroutine = null;
        }
    }

    private IEnumerator PatrolRoutine()
    {
        if (rb2D == null) yield break;

        while (onDuty && currentState == EntityState.ALIVE)
        {
            if (playerTarget == null)
            {
                playerTarget = GameManager.Instance?.MainPlayer ?? FindObjectOfType<Player>();
                yield return new WaitForSeconds(0.25f);
                continue;
            }

            Vector2 toTarget = (playerTarget.transform.position - transform.position);
            float distance = toTarget.magnitude;

            if (distance > attackRange)
            {
                Vector2 dir = toTarget.normalized;
                Vector2 move = (Vector2)rb2D.position + dir * (patrolSpeed * Time.deltaTime);
                rb2D.MovePosition(move);
            }
            else TryAttack();

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

        AnimationHelper.ShowHitFlash(spriteRenderer, hitFlashColor, hitFlashDuration);
    }
}
#endregion