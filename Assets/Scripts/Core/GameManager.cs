using UnityEngine;

/// <summary>
/// Orchestrates high‑level game flow. For the prototype,
/// it only transitions from Boot to Gameplay and drives manager lifecycles.
/// </summary>
public class GameManager : MonoBehaviour {
    public enum GameState {
        Boot,
        MainScreen,
        MenuSelector,
        LevelPreload,
        Cinematic,
        DialogueScene,
        LevelLoad,
        CompletionScene,
        Gameplay
    }

    [Header("Managers")]
    public InputManager inputManager;
    public TimeManager  timeManager;
    
    [Header("Auto-Configuration")]
    [Tooltip("If true, GameManager will automatically find managers and players in the scene")]
    public bool autoConfigureScene = true;
    
    [Header("Debug Visualization")]
    [Tooltip("Show all colliders/hitboxes in the scene for debugging")]
    public bool showHitboxes = false;

    public GameState State { get; private set; } = GameState.Boot;
    
    private bool lastHitboxState = false;

    private void Awake() {
        DebugHelper.LogManager("GameManager initializing...");
        
        // Auto-find managers if not assigned and auto-config is enabled
        if (autoConfigureScene) {
            AutoConfigureScene();
        }
        
        // Wire managers to this GameManager
        if (inputManager != null) {
            inputManager.Configure(this);
        }
        if (timeManager != null) {
            timeManager.Configure(this);
        }

        // Initialize and bind events
        if (inputManager != null) {
            inputManager.Initialize();
            inputManager.BindEvents();
        }
        if (timeManager != null) {
            timeManager.Initialize();
            timeManager.BindEvents();
        }

        // For now, go straight into gameplay
        ChangeState(GameState.Gameplay);
    }
    
    private void Update() {
        // Check if hitbox visualization toggle has changed
        if (showHitboxes != lastHitboxState) {
            HitboxVisualizer.ToggleVisualization(showHitboxes);
            lastHitboxState = showHitboxes;
        }
    }
    
    private void AutoConfigureScene() {
        // Find managers if not assigned
        if (timeManager == null) {
            timeManager = FindFirstObjectByType<TimeManager>();
        }
        
        // Ensure there's an AudioListener in the scene
        EnsureAudioListener();
        
        // Find and configure all players in the scene
        Player[] players = FindObjectsByType<Player>(FindObjectsSortMode.None);
        foreach (Player player in players) {
            // Set player duty state to start combat-ready
            player.SetPlayerDuty(true);
        }
    }
    
    private void EnsureAudioListener() {
        // Check if there's already an AudioListener in the scene
        AudioListener existingListener = FindFirstObjectByType<AudioListener>();
        
        if (existingListener == null) {
            // Try to find the main camera and add AudioListener to it
            Camera mainCamera = Camera.main;
            if (mainCamera == null) {
                mainCamera = FindFirstObjectByType<Camera>();
            }
            
            if (mainCamera != null) {
                mainCamera.gameObject.AddComponent<AudioListener>();
                DebugHelper.LogManager($"Added AudioListener to {mainCamera.name}");
            } else {
                // No camera found, add to GameManager as fallback
                gameObject.AddComponent<AudioListener>();
                DebugHelper.LogManager("Added AudioListener to GameManager (no camera found)");
            }
        } else {
            DebugHelper.LogManager($"AudioListener already exists on {existingListener.name}");
        }
    }

    public void ChangeState(GameState next) {
        DebugHelper.LogManager($"Game state changed: {State} → {next}");
        State = next;
        if (State == GameState.Gameplay) {
            if (inputManager != null) {
                inputManager.StartRuntime();
            }
            if (timeManager != null) {
                timeManager.StartRuntime();
            }
        }
        // Add other state transitions here as your flow expands
    }
}