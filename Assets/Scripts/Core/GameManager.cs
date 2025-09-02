using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central orchestrator. Singleton that persists across scenes and
/// wires manager lifecycles. Re-runs auto-configuration when new
/// scenes are loaded so manager refs don't become stale.
/// 
/// Minimal changes only: factor lifecycle wiring into RunManagerLifecycle()
/// and call it from both initial Start() and scene-loaded handler.
/// </summary>
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Inspector: Debug Tools
    [Header("Debug Tools")]
    public HitboxVisualizer hitboxVisualizer;
    public void SetHitboxVisualization(bool enabled) => hitboxVisualizer?.ToggleVisualization(enabled);
    #endregion

    #region Types
    public enum GameState
    {
        Boot, MainScreen, MenuSelector, LevelPreload,
        Cinematic, DialogueScene, LevelLoad,
        CompletionScene, ExitOrContinue, Gameplay
    }
    #endregion

    #region Inspector: Managers & Level
    [Header("Managers")]
    public InputManager inputManager;
    public TimeManager timeManager;

    [Header("Level")]
    [Tooltip("Assign LevelManager here (inspector)")]
    public LevelManager levelManager;
    #endregion

    #region Runtime State
    [Header("Auto Assigned")]
    public Player MainPlayer { get; private set; }

    public GameState State { get; private set; } = GameState.Boot;
    #endregion

    #region Inspector: Auto-Configuration
    [Header("Auto-Configuration")]
    [Tooltip("If true, GameManager will automatically find managers and players in the scene")]
    public bool autoConfigureScene = true;
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        // singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        DebugHelper.LogManager("GameManager initializing...");
    }

    private void OnEnable()
    {
        // watch future scene loads so we can re-wire scene managers
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// First-time bootstrap runs as a coroutine to allow scene objects to initialize.
    /// </summary>
    private IEnumerator Start()
    {
        // wait a frame so scene-initialized objects have run their Awake/OnEnable
        yield return null;

        if (autoConfigureScene) AutoConfigureScene();

        // Configure / Initialize / Bind managers (do not start runtime here)
        RunManagerLifecycle();

        // enter gameplay state (this will start runtimes)
        ChangeState(GameState.Gameplay);
    }
    #endregion

    #region Scene Handling & Lifecycle Wiring
    /// <summary>
    /// Clears cached scene-local refs and re-finds managers on scene load,
    /// then runs the same lifecycle wiring as Start. If the game is already
    /// in Gameplay state, it re-starts the managers' runtime.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugHelper.LogManager($"Scene loaded: {scene.name} — rewire managers");

        // clear stale refs so AutoConfigureScene will find new ones
        inputManager = null;
        timeManager  = null;
        levelManager = null;
        MainPlayer   = null;

        if (autoConfigureScene) AutoConfigureScene();

        // Re-run configure/initialize/bind for new scene instances
        RunManagerLifecycle();

        // If we are currently in Gameplay, restart manager runtime
        if (State == GameState.Gameplay)
        {
            inputManager?.StartRuntime();
            timeManager?.StartRuntime();
            levelManager?.StartRuntime();
        }
    }

    /// <summary>
    /// Auto-locate managers and the main player if not already assigned.
    /// Uses FindFirstObjectByType so it's safe in editor/runtime.
    /// </summary>
    private void AutoConfigureScene()
    {
        timeManager ??= FindFirstObjectByType<TimeManager>();
        inputManager ??= FindFirstObjectByType<InputManager>();
        levelManager ??= FindFirstObjectByType<LevelManager>();
        MainPlayer   ??= FindFirstObjectByType<Player>();

        if (MainPlayer != null)
            DebugHelper.LogManager($"Auto-assigned MainPlayer: {MainPlayer.name}");

        EnsureAudioListener();
    }

    /// <summary>
    /// The common lifecycle steps used by Start() and OnSceneLoaded to avoid duplication:
    /// - Configure(managers, level)
    /// - Initialize()
    /// - BindEvents()
    /// Note: this method intentionally does NOT call StartRuntime() — state transitions control that.
    /// </summary>
    private void RunManagerLifecycle()
    {
        // Configure
        inputManager?.Configure(this);
        timeManager?.Configure(this);
        levelManager?.Configure(this);

        // Initialize
        inputManager?.Initialize();
        timeManager?.Initialize();
        levelManager?.Initialize();

        // Bind events / subscriptions
        inputManager?.BindEvents();
        timeManager?.BindEvents();
        levelManager?.BindEvents();

        DebugHelper.LogManager("RunManagerLifecycle completed (Configure/Initialize/Bind).");
    }
    #endregion

    #region Utilities & State Transitions
    private void EnsureAudioListener()
    {
        AudioListener existingListener = FindFirstObjectByType<AudioListener>();
        if (existingListener == null)
        {
            Camera mainCamera = Camera.main;
            mainCamera ??= FindFirstObjectByType<Camera>();
            if (mainCamera != null)
            {
                mainCamera.gameObject.AddComponent<AudioListener>();
                DebugHelper.LogManager($"Added AudioListener to {mainCamera.name}");
            }
            else
            {
                gameObject.AddComponent<AudioListener>();
                DebugHelper.LogManager("Added AudioListener to GameManager (no camera found)");
            }
        }
        else DebugHelper.LogManager($"AudioListener already exists on {existingListener.name}");
    }

    public void RegisterPlayer(Player p)
    {
        if (p == null) return;
        MainPlayer = p;
        DebugHelper.LogManager(() => $"MainPlayer registered: {p.name}");
        EventBus.Instance.Publish(new PlayerSpawnedEvent { player = p });
    }

    public void ChangeState(GameState next)
    {
        DebugHelper.LogManager($"Game state changed: {State} → {next}");
        State = next;
        if (State == GameState.Gameplay)
        {
            inputManager?.StartRuntime();
            timeManager?.StartRuntime();
            levelManager?.StartRuntime();
        }
    }
    #endregion
}