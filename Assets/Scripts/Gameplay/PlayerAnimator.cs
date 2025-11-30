using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator), typeof(Rigidbody2D))]
public class PlayerAnimator : MonoBehaviour
{
    [Header("Movement States (Animator State Names)")]
    [SerializeField] private string idleState = "Player_idle_";
    [SerializeField] private string moveRightState = "Player_right_";
    [SerializeField] private string moveLeftState = "Player_left_";
    [SerializeField] private string moveUpState = "Player_up_";
    [SerializeField] private string moveDownState = "Player_down_";

    [Header("Attack States (Animator State Names)")]
    [SerializeField] private string attackRightState = "Player_hit_right"; // Matches file list convention roughly
    [SerializeField] private string attackLeftState = "Player_hit_left_";
    [SerializeField] private string attackUpState = "Player_hit_up_";
    [SerializeField] private string attackDownState = "Player_hit_down_";

    [Header("Settings")]
    [SerializeField] private float velocityThreshold = 0.1f;
    [Tooltip("How long the attack animation overrides movement (Frame Buffer)")]
    [SerializeField] private float attackDuration = 0.25f; 

    private Animator animator;
    private Rigidbody2D rb;
    private InputManager inputManager;
    
    private string currentState;
    private bool isAttacking;
    private Vector2 lastDirection = Vector2.down; // Default facing

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        if (animator == null)
        {
            DebugHelper.LogWarning("[PlayerAnimator] Missing Animator component. Disabling animator logic.");
            enabled = false;
            return;
        }

        if (rb == null)
        {
            DebugHelper.LogWarning("[PlayerAnimator] Missing Rigidbody2D component. Disabling animator logic.");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // Subscribe to input via GameManager
        inputManager = GameManager.Instance?.inputManager;
        if (inputManager != null)
        {
            inputManager.OnBasicPressedDSP += HandleAttackInput;
        }
        else
        {
            DebugHelper.LogWarning("[PlayerAnimator] InputManager not found in GameManager.");
        }
    }

    private void OnDestroy()
    {
        if (inputManager != null)
        {
            inputManager.OnBasicPressedDSP -= HandleAttackInput;
        }
    }

    private void HandleAttackInput(double dspTime)
    {
        if (!isActiveAndEnabled || animator == null || rb == null)
            return;

        // Restart routine to allow "mashing" to reset the buffer window if desired
        if (isAttacking) StopCoroutine(nameof(AttackRoutine));
        StartCoroutine(nameof(AttackRoutine));
    }

    private IEnumerator AttackRoutine()
    {
        if (rb == null)
        {
            DebugHelper.LogWarning("[PlayerAnimator] AttackRoutine aborted — Rigidbody2D missing.");
            yield break;
        }

        isAttacking = true;

        // Determine direction for attack
        // If moving, use current velocity. If idle, use lastDirection.
        Vector2 velocity = rb.linearVelocity;
        Vector2 dir = velocity.sqrMagnitude > velocityThreshold * velocityThreshold 
            ? velocity.normalized 
            : lastDirection;

        // Determine state based on direction
        string attackState = attackDownState;
        
        // Priority: X axis > Y axis (matches movement logic)
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            attackState = dir.x > 0 ? attackRightState : attackLeftState;
        }
        else
        {
            attackState = dir.y > 0 ? attackUpState : attackDownState;
        }

        PlayState(attackState);

        yield return new WaitForSeconds(attackDuration);

        isAttacking = false;
        // Next Update() will resume movement animation
    }

    private void Update()
    {
        if (animator == null || rb == null) return;

        Vector2 velocity = rb.linearVelocity;

        // Always track last direction when moving, so we know where to attack when stopped
        if (velocity.sqrMagnitude > velocityThreshold * velocityThreshold)
        {
            lastDirection = velocity.normalized;
        }

        // If attacking, do not update movement state (lock animation)
        if (isAttacking) return;

        string newState = currentState;

        // Movement Logic
        if (velocity.sqrMagnitude < velocityThreshold * velocityThreshold)
        {
            newState = idleState;
        }
        else if (Mathf.Abs(velocity.x) > velocityThreshold) // X priority
        {
            newState = velocity.x > 0 ? moveRightState : moveLeftState;
        }
        else
        {
            newState = velocity.y > 0 ? moveUpState : moveDownState;
        }

        PlayState(newState);
    }

    private void PlayState(string newState)
    {
        if (animator == null) return;
        if (string.IsNullOrEmpty(newState)) return;
        if (newState == currentState) return;

        animator.Play(newState);
        currentState = newState;
    }
}
