using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles input from the New Input System and exposes it
/// via events and properties. Uses Xbox button/axes by default.
/// </summary>
public class InputManager : BaseManager
{
    [Header("Input Actions")]
    public InputActionReference moveAction;  // left stick (Vector2)
    public InputActionReference aimAction;   // right stick (Vector2)
    public InputActionReference basicAction; // button A (South)

    public Vector2 Move { get; private set; }
    public Vector2 Aim  { get; private set; }

    /// <summary>
    /// Fired when the basic attack button is pressed. Supplies the DSP
    /// timestamp so TimeManager can do on‑beat checks.
    /// </summary>
    public event Action<double> OnBasicPressedDSP;

    public override void StartRuntime()
    {
        // Enable actions when entering Gameplay
        moveAction.action.Enable();
        aimAction.action.Enable();
        basicAction.action.Enable();

        basicAction.action.performed += OnBasic;
    }

    private void OnDisable()
    {
        // Clean up event subscription
        basicAction.action.performed -= OnBasic;
    }

    private void Update()
    {
        // Update movement and aim vectors every frame
        Move = moveAction.action.ReadValue<Vector2>();
        Aim  = aimAction.action.ReadValue<Vector2>();
    }

    private void OnBasic(InputAction.CallbackContext ctx)
    {
        // Use audio DSP time for rhythm checks
        OnBasicPressedDSP?.Invoke(AudioSettings.dspTime);
    }
}