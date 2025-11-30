using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central orchestrator. Singleton that persists across scenes and wires manager lifecycles.
/// CRITICAL: Manages full lifecycle: Configure → Initialize → BindEvents → StartRuntime → StopRuntime → UnbindEvents
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

    [Header("Feature Toggles")]
    [SerializeField] private bool enableDialogManager = false;
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
        // Singleton enforcement - destroy duplicates
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
        // Safety: cleanup on domain reload/playmode exit
        StopManagers();
    }

    private IEnumerator Start()
    {
        yield return null; // Let scene Awake/OnEnable complete first

        AutoConfigureScene();
        RunManagerLifecycle();

        // CRITICAL: Entering Gameplay triggers manager StartRuntime()
        ChangeState(GameState.Gameplay);
    }
    #endregion

    #region Scene handling & lifecycle wiring
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugHelper.LogManager($"Scene loaded: {scene.name} — rewire managers");

        // CRITICAL: Stop/unbind before reconfiguring to prevent ghost subscriptions
        StopManagers();

        // Clear scene-local refs so AutoConfigureScene finds new instances
        inputManager = null;
        timeManager = null;
        levelManager = null;
        dialogManager = null;
        MainPlayer = null;

        AutoConfigureScene();

        // Restart runtime if we're already in gameplay
        if (State == GameState.Gameplay)
        {
            inputManager?.StartRuntime();
            timeManager?.StartRuntime();
            levelManager?.StartRuntime();
            if (enableDialogManager)
                dialogManager?.StartRuntime();
        }
    }

    private void AutoConfigureScene()
    {
        // Fallback discovery: use inspector refs or find in scene
        inputManager ??= FindFirstObjectByType<InputManager>();
        timeManager  ??= FindFirstObjectByType<TimeManager>();
        levelManager ??= FindFirstObjectByType<LevelManager>();
        if (enableDialogManager)
            dialogManager ??= FindFirstObjectByType<DialogManager>();
        else
            dialogManager = null;
        MainPlayer   ??= FindFirstObjectByType<Player>();

        // Wire BeatConfig directly (no reflection)
        if (timeManager != null && beatConfig != null)
            timeManager.beatConfig = beatConfig;

        EnsureAudioListener();
    }

    /// <summary>
    /// Full lifecycle: Configure → Initialize → BindEvents (does NOT start runtime yet)
    /// </summary>
    private void RunManagerLifecycle()
    {
        // Phase 1: Configure (inject dependencies)
        inputManager?.Configure(this);
        timeManager?.Configure(this);
        levelManager?.Configure(this);
        if (enableDialogManager)
            dialogManager?.Configure(this);

        // Phase 2: Initialize (setup internal state)
        inputManager?.Initialize();
        timeManager?.Initialize();
        levelManager?.Initialize();
        if (enableDialogManager)
            dialogManager?.Initialize();

        // Phase 3: BindEvents (subscribe to event bus)
        inputManager?.BindEvents();
        timeManager?.BindEvents();
        levelManager?.BindEvents();
        if (enableDialogManager)
            dialogManager?.BindEvents();

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
            // Phase 1: Stop runtime loops (idempotent)
            inputManager?.StopRuntime();
            timeManager?.StopRuntime();
            levelManager?.StopRuntime();
            if (enableDialogManager)
                dialogManager?.StopRuntime();

            // Phase 2: Unbind events (each manager unsubscribes itself)
            inputManager?.UnbindEvents();
            timeManager?.UnbindEvents();
            levelManager?.UnbindEvents();
            if (enableDialogManager)
                dialogManager?.UnbindEvents();

            // NOTE: Do NOT call EventBus.ClearAll() here!
            // It would wipe entity subscriptions (Enemy, Player) breaking gameplay.
            // Managers must clean up their own subscriptions in UnbindEvents().
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
    /// Register spawned player and wire all dependencies (managers + configs).
    /// IDEMPOTENT: skips if same player already registered.
    /// </summary>
    public void RegisterPlayer(Player p)
    {
        if (p == null) return;
        
        // Idempotency: prevent duplicate registration
        if (MainPlayer == p)
        {
            DebugHelper.LogManager($"[GameManager] Player {p.name} already registered. Skipping.");
            return;
        }

        MainPlayer = p;
        DebugHelper.LogManager(() => $"MainPlayer registered: {p.name}");

        Weapon sceneWeapon = FindFirstObjectByType<Weapon>();
        WirePlayerManagers(p);
        WirePlayerComponents(p, sceneWeapon);
        WireWeaponSystem(p, sceneWeapon);
        PublishPlayerSpawnEvent(p);
    }

    private void WirePlayerManagers(Player p)
    {
        // Inject manager refs into player
        p.inputManager ??= inputManager;
        p.timeManager  ??= timeManager;
    }

    private void WirePlayerComponents(Player p, Weapon sceneWeapon)
    {
        // Wire movement: inject InputManager + MovementConfig
        var mover = p.GetComponent<PlayerMover2D>();
        if (mover != null)
        {
            mover.inputManager ??= inputManager;
            mover.movementConfig ??= movementConfig;
        }

        // Wire attack controller: inject managers + configs
        var pac = p.GetComponent<PlayerAttackController>();
        if (pac != null)
        {
            pac.inputManager ??= inputManager;
            pac.timeManager  ??= timeManager;
            pac.combatConfig ??= combatConfig;
            pac.beatConfig   ??= beatConfig;
            
            // CRITICAL: Auto-assign weapon if missing (prevents null access)
            pac.weapon ??= sceneWeapon;
            if (pac.weapon != null)
                DebugHelper.LogManager($"[GameManager] Auto-assigned weapon {pac.weapon.name} to PlayerAttackController");
        }
    }

    private void WireWeaponSystem(Player p, Weapon playerWeapon)
    {
        if (playerWeapon != null)
        {
            playerWeapon.combatConfig ??= combatConfig;
            playerWeapon.SetOwner(p); // Weapon needs owner for damage events
            DebugHelper.LogManager($"[GameManager] Set {p.name} as owner of {playerWeapon.name}");
        }
    }

    private void PublishPlayerSpawnEvent(Player p)
    {
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

    /// <summary>
    /// State transition: Gameplay state triggers StartRuntime() on all managers
    /// </summary>
    public void ChangeState(GameState next)
    {
        DebugHelper.LogManager($"Game state changed: {State} → {next}");
        State = next;
        if (State == GameState.Gameplay)
        {
            // CRITICAL: Gameplay entry starts manager runtime loops
            DebugHelper.LogManager("Starting manager runtimes for gameplay...");
            inputManager?.StartRuntime();
            timeManager?.StartRuntime();
            levelManager?.StartRuntime();
            DebugHelper.LogManager("All manager runtimes started.");
        }
        else
            StopManagers();
    }

    /// <summary>
    /// Called when the player dies to halt runtime systems that should not continue looping.
    /// Leaves InputManager and LevelManager running so UI and restart flows still work.
    /// </summary>
    public void HandlePlayerDeath()
    {
        if (!managersStarted) return;

        DebugHelper.LogManager("GameManager.HandlePlayerDeath(): stopping non-essential managers.");

        // Keep input + level alive for menu navigation while pausing everything else.
        timeManager?.StopRuntime();
        if (enableDialogManager)
            dialogManager?.StopRuntime();

        // Also pause beat visualizers so they stop logging on-beat checks post-mortem.
        var visualizers = FindObjectsByType<BeatVisualizer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var visualizer in visualizers)
            visualizer.SetPaused(true);
    }
    #endregion
}