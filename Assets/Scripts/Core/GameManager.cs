using System;
using System.Reflection;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central orchestrator. Singleton that persists across scenes and
/// wires manager lifecycles. This variant:
/// - exposes global ScriptableObject configs (assign in inspector),
/// - safely stops/unbinds managers before scene reconfigure,
/// - wires configs into player components on RegisterPlayer,
/// - and does tidy shutdown on OnDisable.
/// </summary>
[DefaultExecutionOrder(-100)]
public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
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
    [Header("Managers (assign in inspector if you want fixed refs)")]
    [SerializeField] public InputManager inputManager;
    [SerializeField] public TimeManager timeManager;
    [SerializeField] public LevelManager levelManager;
    [SerializeField] private DialogManager dialogManager;

    #endregion

    #region Inspector: Global Configs (ScriptableObjects)
    [Header("Global Config Assets (assign)")]
    [SerializeField] public CombatConfig combatConfig;
    [SerializeField] public BeatConfig beatConfig;
    [SerializeField] public MovementConfig movementConfig;
    #endregion

    #region Runtime State
    [Header("Auto Assigned")]
    public Player MainPlayer { get; private set; }
    public GameState State { get; private set; } = GameState.Boot;
    bool managersStarted = false;
    #endregion

    #region Unity lifecycle
    private void Awake()
    {
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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        // Ensure managers stop when manager goes away (domain reload / playmode stop)
        StopManagers();
    }

    private IEnumerator Start()
    {
        // allow scene Awake/OnEnable to run
        yield return null;

        AutoConfigureScene();
        RunManagerLifecycle();

        // enter gameplay state (this will start runtimes)
        ChangeState(GameState.Gameplay);
    }
    #endregion

    #region Scene handling & lifecycle wiring
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugHelper.LogManager($"Scene loaded: {scene.name} — rewire managers");

        // stop and unbind old managers to avoid stale subscriptions/ghost calls
        StopManagers();

        // clear cached scene-local refs so AutoConfigureScene will find new ones
        inputManager = null;
        timeManager = null;
        levelManager = null;
        MainPlayer = null;

        AutoConfigureScene();
        RunManagerLifecycle();

        // If we are currently in Gameplay, restart manager runtime
        if (State == GameState.Gameplay)
        {
            inputManager?.StartRuntime();
            timeManager?.StartRuntime();
            levelManager?.StartRuntime();
        }
    }

    private void AutoConfigureScene()
    {
        // Find managers in scene if not set in inspector
        inputManager ??= FindFirstObjectByType<InputManager>();
        timeManager  ??= FindFirstObjectByType<TimeManager>();
        levelManager ??= FindFirstObjectByType<LevelManager>();
        dialogManager ??= FindFirstObjectByType<DialogManager>();
        MainPlayer   ??= FindFirstObjectByType<Player>();

        // Optionally push beatConfig into TimeManager if the field/property exists
        if (timeManager != null && beatConfig != null)
        {
            var tmType = timeManager.GetType();
            var field = tmType.GetField("beatConfig", BindingFlags.Public | BindingFlags.Instance);
            if (field != null && field.FieldType == typeof(BeatConfig))
                field.SetValue(timeManager, beatConfig);
            else
            {
                var prop = tmType.GetProperty("BeatConfig", BindingFlags.Public | BindingFlags.Instance);
                if (prop != null && prop.PropertyType == typeof(BeatConfig) && prop.CanWrite)
                    prop.SetValue(timeManager, beatConfig);
            }
        }

        EnsureAudioListener();
    }

    /// <summary>
    /// Configure -> Initialize -> Bind (does NOT call StartRuntime).
    /// </summary>
    private void RunManagerLifecycle()
    {
        // Configure
        inputManager?.Configure(this);
        timeManager?.Configure(this);
        //levelManager?.Configure(this);
        dialogManager?.Configure(this);


        // Initialize
        inputManager?.Initialize();
        timeManager?.Initialize();
        //levelManager?.Initialize();
        dialogManager?.Initialize();

        // Bind events
        inputManager?.BindEvents();
        timeManager?.BindEvents();
        //levelManager?.BindEvents();
        dialogManager?.BindEvents()



        DebugHelper.LogManager("RunManagerLifecycle completed (Configure/Initialize/Bind).");
        managersStarted = true;
    }
    #endregion

    #region Manager stop/unbind helpers
    public void StopManagers()
    {
        if (!managersStarted) return;

        try
        {
            // Stop runtime first (idempotent)
            inputManager?.StopRuntime();
            timeManager?.StopRuntime();
            levelManager?.StopRuntime();

            // Then unbind events
            inputManager?.UnbindEvents();
            timeManager?.UnbindEvents();
            levelManager?.UnbindEvents();

            // NOTE: Do NOT call EventBus.Instance.ClearAll() here!
            // ClearAll() removes ALL subscriptions globally, breaking other systems
            // (Player, Enemy, UI, combat) that also use EventBus.
            // Each manager is responsible for cleaning up its own EventBus subscriptions
            // in its UnbindEvents() method.
        }
        catch (Exception ex)
        {
            DebugHelper.LogWarning($"GameManager.StopManagers() encountered: {ex.Message}");
        }

        managersStarted = false;
        DebugHelper.LogManager("GameManager: Managers stopped and unbound.");
    }
    #endregion

    #region Utilities & state transitions
    private void EnsureAudioListener()
    {
        AudioListener existingListener = FindFirstObjectByType<AudioListener>();
        if (existingListener == null)
        {
            Camera mainCamera = Camera.main ?? FindFirstObjectByType<Camera>();
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

    /// <summary>
    /// Register a spawned player and wire manager refs + config assets into its components.
    /// </summary>
    public void RegisterPlayer(Player p)
    {
        if (p == null) return;
        MainPlayer = p;
        DebugHelper.LogManager(() => $"MainPlayer registered: {p.name}");

        // wire managers
        p.inputManager ??= inputManager;
        p.timeManager  ??= timeManager;

        // movement
        var mover = p.GetComponent<PlayerMover2D>();
        if (mover != null)
        {
            mover.inputManager ??= inputManager;
            mover.movementConfig ??= movementConfig;
        }

        // attack controller
        var pac = p.GetComponent<PlayerAttackController>();
        if (pac != null)
        {
            pac.inputManager ??= inputManager;
            pac.timeManager  ??= timeManager;
            pac.combatConfig ??= combatConfig;
            pac.beatConfig   ??= beatConfig;
        }

        // weapon
        var weapon = p.GetComponentInChildren<Weapon>(true);
        if (weapon != null)
            weapon.combatConfig ??= combatConfig;

        // Publish spawn event safely
        try
        {
            EventBus.Instance?.Publish(new PlayerSpawnedEvent { player = p });
        }
        catch (Exception ex)
        {
            DebugHelper.LogWarning($"GameManager.RegisterPlayer: EventBus publish failed: {ex.Message}");
        }
    }

    public void UnregisterPlayer(Player p)
    {
        if (MainPlayer == p) MainPlayer = null;
        DebugHelper.LogManager("GameManager: Player unregistered.");
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
        else
            StopManagers();
    }
    #endregion
}