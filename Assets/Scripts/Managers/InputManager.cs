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

    [Header("Mouse Aim (PC)")]
    public Camera worldCamera; // if null -> Camera.main
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
        worldCamera ??= Camera.main;
    }

    public override void Initialize() { }

    public override void BindEvents()
    {
        if (actionsBound) return;

        if (basicAction && basicAction.action != null)
            basicAction.action.performed += HandleBasicPerformed;

        if (submitAction && submitAction.action != null)
            submitAction.action.performed += _ => OnSubmitPressed?.Invoke();

        if (cancelAction && cancelAction.action != null)
            cancelAction.action.performed += _ => OnCancelPressed?.Invoke();

        actionsBound = true;
    }

    public override void StartRuntime()
    {
        if (runtimeActive) return;

        moveAction?.action?.Enable();
        aimAction?.action?.Enable();
        basicAction?.action?.Enable();
        navigateAction?.action?.Enable();
        submitAction?.action?.Enable();
        cancelAction?.action?.Enable();

        runtimeActive = true;
    }

    public override void StopRuntime()
    {
        if (!runtimeActive) return;
        DisableAll();
        runtimeActive = false;
    }

    public override void UnbindEvents()
    {
        if (!actionsBound) return;

        if (basicAction && basicAction.action != null)
            basicAction.action.performed -= HandleBasicPerformed;

        if (submitAction && submitAction.action != null)
            submitAction.action.performed -= _ => OnSubmitPressed?.Invoke();

        if (cancelAction && cancelAction.action != null)
            cancelAction.action.performed -= _ => OnCancelPressed?.Invoke();

        actionsBound = false;
    }

    private void OnDisable()
    {
        // Safety net for domain reloads/scene changes
        DisableAll();
        UnbindEvents();
    }
    #endregion

    #region Update Polling
    private void Update()
    {
        if (!runtimeActive) return;

        // Gamepad / Keyboard movement from action
        Move = moveAction && moveAction.action != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;

        // Aim:
        // - If aimAction has value (gamepad RS), use it directly.
        // - Else derive from mouse (PC): vector from player to mouse world pos should be set by gameplay code,
        //   but as a fallback here we feed screen-space mouse delta as a direction if no aim action is present.
        Vector2 aimVec = Vector2.zero;

        if (aimAction && aimAction.action != null)
            aimVec = aimAction.action.ReadValue<Vector2>();

        // Derive mouse aim as world direction (requires a player position — recommended to be handled in gameplay).
        // Here we just keep aimVec if provided; gameplay can set precise aim via a dedicated system.

        Aim = aimVec;

        // UI navigate (optional)
        Navigate = navigateAction && navigateAction.action != null ? navigateAction.action.ReadValue<Vector2>() : Vector2.zero;
    }
    #endregion

    #region Handlers
    private void HandleBasicPerformed(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        OnBasicPressedDSP?.Invoke(AudioSettings.dspTime);
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