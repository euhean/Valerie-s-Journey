using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles input from the New Input System and exposes it
/// via events and properties. Lifecycle-aware (Configure/Initialize/Bind/Start/Pause/Teardown).
/// </summary>
public class InputManager : BaseManager {
    [Header("Input Actions")]
    public InputActionReference moveAction;  // left stick (Vector2)
    public InputActionReference aimAction;   // right stick (Vector2)
    public InputActionReference basicAction; // button A (South)

    public Vector2 Move { get; private set; }
    public Vector2 Aim  { get; private set; }

    /// <summary>
    /// Fired when the basic attack button is pressed. Supplies the DSP
    /// timestamp so TimeManager can do on-beat checks.
    /// </summary>
    public event Action<double> OnBasicPressedDSP;

    // lifecycle state
    bool actionsBound = false;
    bool runtimeActive = false;

    public override void Configure(GameManager gm) {
        base.Configure(gm);
        DebugHelper.LogManager("InputManager.Configure()");
    }

    public override void Initialize() {
        DebugHelper.LogManager("InputManager.Initialize()");
        moveAction?.action.Disable();
        aimAction?.action.Disable();
        basicAction?.action.Disable();
    }

    public override void BindEvents() {
        DebugHelper.LogManager("InputManager.BindEvents()");
        if (actionsBound) return;

        if (basicAction != null) {
            basicAction.action.performed += OnBasic;
            actionsBound = true;
        }
    }

    public override void StartRuntime() {
        DebugHelper.LogManager("InputManager.StartRuntime()");
        if (runtimeActive) return;

        moveAction?.action.Enable();
        aimAction?.action.Enable();
        basicAction?.action.Enable();

        runtimeActive = true;
    }

    public override void Pause(bool isPaused) {
        DebugHelper.LogManager($"InputManager.Pause({isPaused})");
        if (!runtimeActive) return;

        if (isPaused) {
            moveAction?.action.Disable();
            aimAction?.action.Disable();
            basicAction?.action.Disable();
        } else {
            moveAction?.action.Enable();
            aimAction?.action.Enable();
            basicAction?.action.Enable();
        }
    }

    public override void Teardown() {
        DebugHelper.LogManager("InputManager.Teardown()");
        if (actionsBound && basicAction != null) {
            basicAction.action.performed -= OnBasic;
            actionsBound = false;
        }

        moveAction?.action.Disable();
        aimAction?.action.Disable();
        basicAction?.action.Disable();

        runtimeActive = false;
    }

    private void Update() {
        // update vectors every frame (only read values)
        Move = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Aim  = aimAction  != null ? aimAction.action.ReadValue<Vector2>()  : Vector2.zero;
    }

    private void OnDisable() {
        if (actionsBound && basicAction != null) {
            basicAction.action.performed -= OnBasic;
            actionsBound = false;
        }
    }

    private void OnBasic(InputAction.CallbackContext ctx) {
        OnBasicPressedDSP?.Invoke(AudioSettings.dspTime);
    }
}