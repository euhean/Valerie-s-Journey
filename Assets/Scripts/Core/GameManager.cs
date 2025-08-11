using UnityEngine;

/// <summary>
/// Orchestrates high‑level game flow. In this prototype it supports
/// a simple state machine and drives manager lifecycles. It can also
/// automatically configure the scene by finding managers and players
/// when running in a test environment.
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("Debug Tools")]
    public HitboxVisualizer hitboxVisualizer;
    /// <summary>
    /// Toggle hitbox visualization on or off via GameManager.
    /// </summary>
    public void SetHitboxVisualization(bool enabled)
    {
        if (hitboxVisualizer != null)
        {
            hitboxVisualizer.ToggleVisualization(enabled);
        }
    }

    public enum GameState
    {
        Boot,
        MainScreen,
        MenuSelector,
        LevelPreload,
        Cinematic,
        DialogueScene,
        LevelLoad,
        CompletionScene,
        ExitOrContinue,
        Gameplay
    }

    [Header("Managers")]
    public InputManager inputManager;
    public TimeManager timeManager;

    [Header("Auto-Configuration")]
    [Tooltip("If true, GameManager will automatically find managers and players in the scene")]
    public bool autoConfigureScene = true;

    public GameState State { get; private set; } = GameState.Boot;

    private void Awake()
    {
        DebugHelper.LogManager("GameManager initializing...");

        // Auto-find managers if not assigned and auto-config is enabled
        if (autoConfigureScene)
        {
            AutoConfigureScene();
        }

        // Wire managers to this GameManager
        if (inputManager != null)
        {
            inputManager.Configure(this);
        }
        if (timeManager != null)
        {
            timeManager.Configure(this);
        }

        // Initialize and bind events
        if (inputManager != null)
        {
            inputManager.Initialize();
            inputManager.BindEvents();
        }
        if (timeManager != null)
        {
            timeManager.Initialize();
            timeManager.BindEvents();
        }

        // For now, go straight into gameplay
        ChangeState(GameState.Gameplay);
    }

    private void AutoConfigureScene()
    {
        // Find managers if not assigned
        if (timeManager == null)
        {
            timeManager = FindFirstObjectByType<TimeManager>();
        }
        if (inputManager == null)
        {
            inputManager = FindFirstObjectByType<InputManager>();
        }

        // Ensure there's an AudioListener in the scene
        EnsureAudioListener();

        // Find and configure all players in the scene
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (Player player in players)
        {
            // Set player duty state to start combat-ready
            player.SetPlayerDuty(true);
        }
    }

    private void EnsureAudioListener()
    {
        // Check if there's already an AudioListener in the scene
        AudioListener existingListener = FindFirstObjectByType<AudioListener>();

        if (existingListener == null)
        {
            // Try to find the main camera and add AudioListener to it
            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                mainCamera = FindFirstObjectByType<Camera>();
            }
            if (mainCamera != null)
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
                DebugHelper.LogManager($"Added AudioListener to {mainCamera.name}");
            }
            else
            {
                // No camera found, add to GameManager as fallback
                gameObject.AddComponent<AudioListener>();
                DebugHelper.LogManager("Added AudioListener to GameManager (no camera found)");
            }
        }
        else
        {
            DebugHelper.LogManager($"AudioListener already exists on {existingListener.name}");
        }
    }

    public void ChangeState(GameState next)
    {
        DebugHelper.LogManager($"Game state changed: {State} → {next}");
        State = next;
        if (State == GameState.Gameplay)
        {
            if (inputManager != null)
            {
                inputManager.StartRuntime();
            }
            if (timeManager != null)
            {
                timeManager.StartRuntime();
            }
        }
        // Add other state transitions here as your flow expands
    }
}