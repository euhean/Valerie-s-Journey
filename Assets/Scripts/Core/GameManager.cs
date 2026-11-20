using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central orchestrator. Singleton that persists across scenes.
/// Every scene load (the first one included) goes through OnSceneLoaded, which is the
/// single place where managers are discovered, run through
/// Configure → Initialize → BindEvents, and started.
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

    #region Managers (discovered in the loaded scene)
    [Header("Managers (found in the scene on every load)")]
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
    public Player MainPlayer { get; private set; }
    public GameState State { get; private set; } = GameState.Boot;

    // Lifecycle calls run in list order: input, time, level, dialog.
    private readonly List<BaseManager> managers = new();
    #endregion

    #region Unity lifecycle
    private void Awake()
    {
        // A reloaded scene brings its own GameManager; the persistent one wins.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StopManagers();
    }
    #endregion

    #region Scene bootstrap
    /// <summary>
    /// Unity raises sceneLoaded after every scene object's Awake/OnEnable and before any
    /// Start, so everything wired here is ready by the time gameplay scripts reach Start.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugHelper.LogManager($"Scene loaded: {scene.name} — wiring managers");

        StopManagers();
        DiscoverManagers();

        foreach (var m in managers) m.Configure(this);
        foreach (var m in managers) m.Initialize();
        foreach (var m in managers) m.BindEvents();

        MainPlayer = null;
        var scenePlayer = FindFirstObjectByType<Player>();
        if (scenePlayer != null) RegisterPlayer(scenePlayer);

        EnsureAudioListener();
        ChangeState(GameState.Gameplay);
    }

    private void DiscoverManagers()
    {
        inputManager  = FindFirstObjectByType<InputManager>();
        timeManager   = FindFirstObjectByType<TimeManager>();
        levelManager  = FindFirstObjectByType<LevelManager>();
        dialogManager = enableDialogManager ? FindFirstObjectByType<DialogManager>() : null;

        managers.Clear();
        if (inputManager != null)  managers.Add(inputManager);
        if (timeManager != null)   managers.Add(timeManager);
        if (levelManager != null)  managers.Add(levelManager);
        if (dialogManager != null) managers.Add(dialogManager);

        if (timeManager != null && beatConfig != null)
            timeManager.beatConfig = beatConfig;
    }

    /// <summary>
    /// Stops and unbinds the managers of the scene that is still alive.
    /// Managers destroyed by a scene unload have already done both in BaseManager.OnDisable.
    /// </summary>
    private void StopManagers()
    {
        foreach (var m in managers)
        {
            if (m == null) continue;
            m.StopRuntime();
            m.UnbindEvents();
        }
        managers.Clear();
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
            pac.timeManager = timeManager;
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

    /// <summary>
    /// Entering Gameplay starts every manager's runtime; leaving it stops them.
    /// Event bindings stay in place across state changes.
    /// </summary>
    public void ChangeState(GameState next)
    {
        DebugHelper.LogManager($"Game state changed: {State} → {next}");
        var previous = State;
        State = next;

        if (next == GameState.Gameplay)
            foreach (var m in managers) m.StartRuntime();
        else if (previous == GameState.Gameplay)
            foreach (var m in managers) m.StopRuntime();
    }
    #endregion
}