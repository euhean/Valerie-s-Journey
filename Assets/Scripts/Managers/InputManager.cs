using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// New Input System wrapper: exposes Move/Aim and basic button (with DSP timestamp).
/// </summary>
public class InputManager : BaseManager
{
    #region Inspector
    [Header("Input Actions")]
    public InputActionReference moveAction;   // Vector2
    public InputActionReference aimAction;    // Vector2
    public InputActionReference basicAction;  // Button (South)
    
    [Header("UI Navigation Actions")]
    public InputActionReference navigateAction; // Vector2 (for UI navigation)
    public InputActionReference submitAction;   // Button (A/South)
    public InputActionReference cancelAction;   // Button (B/East)
    #endregion

    #region Public State
    public Vector2 Move { get; private set; }
    public Vector2 Aim  { get; private set; }
    public Vector2 Navigate { get; private set; }
    public event Action<double> OnBasicPressedDSP;
    public event System.Action OnSubmitPressed;
    public event System.Action OnCancelPressed;
    #endregion

    #region Private
    bool actionsBound = false;
    bool runtimeActive = false;
    #endregion

    #region Lifecycle
    public override void Configure(GameManager gm)
    {
        base.Configure(gm);
        DebugHelper.LogManager("InputManager.Configure()");
    }

    public override void Initialize()
    {
        DebugHelper.LogManager("InputManager.Initialize()");
        moveAction?.action.Disable();
        aimAction?.action.Disable();
        basicAction?.action.Disable();
        navigateAction?.action.Disable();
        submitAction?.action.Disable();
        cancelAction?.action.Disable();
    }

    public override void BindEvents()
    {
        DebugHelper.LogManager("InputManager.BindEvents()");
        if (actionsBound) return;
        
        if (basicAction != null)
            basicAction.action.performed += OnBasic;
        if (submitAction != null)
            submitAction.action.performed += OnSubmit;
        if (cancelAction != null)
            cancelAction.action.performed += OnCancel;
            
        actionsBound = true;
    }

    public override void StartRuntime()
    {
        DebugHelper.LogManager("InputManager.StartRuntime()");
        if (runtimeActive) return;
        moveAction?.action.Enable();
        aimAction?.action.Enable();
        basicAction?.action.Enable();
        navigateAction?.action.Enable();
        submitAction?.action.Enable();
        cancelAction?.action.Enable();
        runtimeActive = true;
    }

    public override void Pause(bool isPaused)
    {
        DebugHelper.LogManager($"InputManager.Pause({isPaused})");
        if (!runtimeActive) return;

        if (isPaused)
        {
            moveAction?.action.Disable();
            aimAction?.action.Disable();
            basicAction?.action.Disable();
            navigateAction?.action.Disable();
            submitAction?.action.Disable();
            cancelAction?.action.Disable();
        }
        else
        {
            moveAction?.action.Enable();
            aimAction?.action.Enable();
            basicAction?.action.Enable();
            navigateAction?.action.Enable();
            submitAction?.action.Enable();
            cancelAction?.action.Enable();
        }
    }

    public override void Teardown()
    {
        DebugHelper.LogManager("InputManager.Teardown()");
        if (actionsBound)
        {
            if (basicAction != null)
                basicAction.action.performed -= OnBasic;
            if (submitAction != null)
                submitAction.action.performed -= OnSubmit;
            if (cancelAction != null)
                cancelAction.action.performed -= OnCancel;
            actionsBound = false;
        }
        
        moveAction?.action.Disable();
        aimAction?.action.Disable();
        basicAction?.action.Disable();
        navigateAction?.action.Disable();
        submitAction?.action.Disable();
        cancelAction?.action.Disable();
        runtimeActive = false;
    }
    #endregion

    #region Unity
    private void Update()
    {
        Move = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Aim  = aimAction  != null ? aimAction.action.ReadValue<Vector2>()  : Vector2.zero;
        Navigate = navigateAction != null ? navigateAction.action.ReadValue<Vector2>() : Vector2.zero;
    }

    private void OnDisable()
    {
        if (actionsBound)
        {
            if (basicAction != null)
                basicAction.action.performed -= OnBasic;
            if (submitAction != null)
                submitAction.action.performed -= OnSubmit;
            if (cancelAction != null)
                cancelAction.action.performed -= OnCancel;
            actionsBound = false;
        }
    }
    #endregion

    #region Handlers
    private void OnBasic(InputAction.CallbackContext ctx)
        => OnBasicPressedDSP?.Invoke(AudioSettings.dspTime);
        
    private void OnSubmit(InputAction.CallbackContext ctx)
        => OnSubmitPressed?.Invoke();
        
    private void OnCancel(InputAction.CallbackContext ctx)
        => OnCancelPressed?.Invoke();
    #endregion
}