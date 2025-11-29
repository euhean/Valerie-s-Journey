using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// New Input System wrapper: exposes Move/Aim vectors and basic button (edge) with DSP timestamp.
/// Supports Gamepad and KB+Mouse (WASD + mouse-aim + left click).
/// </summary>
public class InputManager : BaseManager
{
    #region Inspector
    [Header("Input Actions")]
    public InputActionReference moveAction;   // Vector2
    public InputActionReference aimAction;    // Vector2 (gamepad stick)
    public InputActionReference basicAction;  // Button (south / left mouse)

    [Header("UI Navigation Actions")]
    public InputActionReference navigateAction;
    public InputActionReference submitAction;
    public InputActionReference cancelAction;
    #endregion

    #region Public State
    public Vector2 Move { get; private set; }
    public Vector2 Aim  { get; private set; }
    public Vector2 Navigate { get; private set; }
    public event Action<double> OnBasicPressedDSP;
    public event Action OnSubmitPressed;
    public event Action OnCancelPressed;
    #endregion

    #region Private
    bool actionsBound = false;
    bool runtimeActive = false;
    #endregion

    #region Lifecycle
    public override void Configure(GameManager gm)
    {
        base.Configure(gm);
        DebugHelper.LogManager("InputManager: Configured.");
    }

    public override void Initialize() { }

    public override void BindEvents()
    {
        if (actionsBound)
        {
            DebugHelper.LogWarning("InputManager: Events already bound, skipping.");
            return;
        }

        if (basicAction && basicAction.action != null)
            basicAction.action.performed += HandleBasicPerformed;
        else
            DebugHelper.LogWarning("InputManager: basicAction is null or invalid.");

        if (submitAction && submitAction.action != null)
            submitAction.action.performed += HandleSubmitPerformed;
        else
            DebugHelper.LogWarning("InputManager: submitAction is null or invalid.");

        if (cancelAction && cancelAction.action != null)
            cancelAction.action.performed += HandleCancelPerformed;
        else
            DebugHelper.LogWarning("InputManager: cancelAction is null or invalid.");

        actionsBound = true;
        DebugHelper.LogManager("InputManager: Events bound successfully.");
    }

    public override void StartRuntime()
    {
        if (runtimeActive)
        {
            DebugHelper.LogWarning("InputManager: Runtime already active, skipping.");
            return;
        }

        moveAction?.action?.Enable();
        aimAction?.action?.Enable();
        basicAction?.action?.Enable();
        navigateAction?.action?.Enable();
        submitAction?.action?.Enable();
        cancelAction?.action?.Enable();

        runtimeActive = true;
        DebugHelper.LogManager("InputManager: Runtime started, all actions enabled.");
    }

    public override void StopRuntime()
    {
        if (!runtimeActive)
        {
            DebugHelper.LogWarning("InputManager: Runtime not active, nothing to stop.");
            return;
        }
        DisableAll();
        runtimeActive = false;
        DebugHelper.LogManager("InputManager: Runtime stopped, all actions disabled.");
    }

    public override void UnbindEvents()
    {
        if (!actionsBound)
        {
            DebugHelper.LogWarning("InputManager: Events not bound, nothing to unbind.");
            return;
        }

        if (basicAction && basicAction.action != null)
            basicAction.action.performed -= HandleBasicPerformed;

        if (submitAction && submitAction.action != null)
            submitAction.action.performed -= HandleSubmitPerformed;

        if (cancelAction && cancelAction.action != null)
            cancelAction.action.performed -= HandleCancelPerformed;

        actionsBound = false;
        DebugHelper.LogManager("InputManager: Events unbound successfully.");
    }

    private void OnDisable()
    {
        // Safety net for domain reloads/scene changes
        DebugHelper.LogManager("InputManager: OnDisable called, cleaning up...");
        DisableAll();
        UnbindEvents();
    }
    #endregion

    #region Update Polling
    private void Update()
    {
        if (!runtimeActive) return;

        // Read movement input (gamepad stick or WASD)
        Move = moveAction && moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        // Read aim input (gamepad right stick)
        // Note: Mouse aiming should be handled in gameplay code (PlayerMover2D/Weapon)
        // where the player's world position is available for screen-to-world conversion
        Aim = aimAction && aimAction.action != null ? aimAction.action.ReadValue<Vector2>() : Vector2.zero;

        // UI navigate (optional)
        Navigate = navigateAction && navigateAction.action != null ? navigateAction.action.ReadValue<Vector2>() : Vector2.zero;
    }
    #endregion

    #region Handlers
    private void HandleBasicPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        double dspTime = AudioSettings.dspTime;
        DebugHelper.Log(() => $"InputManager: Basic action pressed at DSP {dspTime:F4}");
        OnBasicPressedDSP?.Invoke(dspTime);
    }

    private void HandleSubmitPerformed(InputAction.CallbackContext ctx)
    {
        DebugHelper.Log("InputManager: Submit action pressed");
        OnSubmitPressed?.Invoke();
    }

    private void HandleCancelPerformed(InputAction.CallbackContext ctx)
    {
        DebugHelper.Log("InputManager: Cancel action pressed");
        OnCancelPressed?.Invoke();
    }
    #endregion

    #region Helpers
    private void DisableAll()
    {
        moveAction?.action?.Disable();
        aimAction?.action?.Disable();
        basicAction?.action?.Disable();
        navigateAction?.action?.Disable();
        submitAction?.action?.Disable();
        cancelAction?.action?.Disable();
    }
    #endregion
}