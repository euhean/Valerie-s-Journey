using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float moveThreshold = 0.1f;
    [SerializeField] private float attackDuration = 0.25f;

    [Header("Idle States")]
    [SerializeField] private string idleState = "Player_idle_"; // If you have directional idles, we can expand this

    [Header("Movement States")]
    [SerializeField] private string moveRight = "Player_right_";
    [SerializeField] private string moveLeft = "Player_left_";
    [SerializeField] private string moveUp = "Player_up_";
    [SerializeField] private string moveDown = "Player_down_";

    [Header("Attack States")]
    [SerializeField] private string attackRight = "Player_hit_right";
    [SerializeField] private string attackLeft = "Player_hit_left_";
    [SerializeField] private string attackUp = "Player_hit_up_";
    [SerializeField] private string attackDown = "Player_hit_down_";

    [Header("References")]
    [SerializeField] private PlayerAttackController attackController;

    private Animator _animator;
    private Rigidbody2D _rb;
    private string _currentState;
    private bool _isAttacking;
    private Vector2 _facingDir = Vector2.down; // Default facing

    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _rb = GetComponent<Rigidbody2D>();
        attackController ??= GetComponent<PlayerAttackController>();
    }

    private void Start()
    {
        var input = GameManager.Instance?.inputManager;
        if (input != null) input.OnBasicPressedDSP += OnAttack;
    }

    private void OnDestroy()
    {
        var input = GameManager.Instance?.inputManager;
        if (input != null) input.OnBasicPressedDSP -= OnAttack;
    }

    private void Update()
    {
        // 1. If attacking, lock movement animations
        if (_isAttacking) return;

        // 2. Calculate Physics
        Vector2 vel = _rb.linearVelocity;
        bool isMoving = vel.sqrMagnitude > moveThreshold * moveThreshold;

        // 3. Update Facing Direction (Isaac Style: Cardinal Priority)
        if (isMoving)
        {
            if (Mathf.Abs(vel.x) > Mathf.Abs(vel.y))
                _facingDir = vel.x > 0 ? Vector2.right : Vector2.left;
            else
                _facingDir = vel.y > 0 ? Vector2.up : Vector2.down;
        }

        // 4. Determine State
        string newState = DetermineState(isMoving);

        // 5. Apply State (The "Transition Logic")
        PlayAnimation(newState);
    }

    private string DetermineState(bool isMoving)
    {
        if (!isMoving) return idleState;

        if (_facingDir == Vector2.right) return moveRight;
        if (_facingDir == Vector2.left) return moveLeft;
        if (_facingDir == Vector2.up) return moveUp;
        return moveDown;
    }

    private Vector2 ResolveAttackDirection()
    {
        if (attackController != null && attackController.TryGetAimDirection(out var aimDir))
        {
            return aimDir;
        }

        return _facingDir;
    }

    private string DetermineAttackState(Vector2 attackDir)
    {
        if (Mathf.Abs(attackDir.x) > Mathf.Abs(attackDir.y))
            return attackDir.x >= 0f ? attackRight : attackLeft;

        return attackDir.y >= 0f ? attackUp : attackDown;
    }

    private void OnAttack(double dspTime)
    {
        if (_isAttacking) return;
        StartCoroutine(AttackRoutine());
    }

    private IEnumerator AttackRoutine()
    {
        _isAttacking = true;

        Vector2 attackDir = ResolveAttackDirection();
        string attackState = DetermineAttackState(attackDir);

        PlayAnimation(attackState);

        yield return new WaitForSeconds(attackDuration);

        _isAttacking = false;
        // Next Update() will automatically switch back to Idle/Move
    }

    private void PlayAnimation(string newState)
    {
        // Optimization: Don't tell Unity to play the same animation again
        if (_currentState == newState) return;

        _animator.Play(newState);
        _currentState = newState;
    }
}
