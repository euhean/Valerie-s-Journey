using System;
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
    public static GameManager Instance { get; private set; } = GameState.Menu;
    #endregion

    #region Types
    public enum GameState { Gameplay, Dialog, Menu }
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
    private readonly List<BaseManager> _managers = new();
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

        AutoConfigureScene();
        RunManagerLifecycle();
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

    private void Start()
    {
        ChangeState(GameState.Gameplay);
    }
    #endregion

    #region Scene handling & lifecycle wiring
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        DebugHelper.LogManager($"Scene loaded: {scene.name} — rewire managers");

        StopManagers();

        inputManager = null;
        timeManager = null;
        levelManager = null;
        dialogManager = null;
        MainPlayer = null;

        AutoConfigureScene();
        RunManagerLifecycle();
        StartAllManagers();
        ChangeState(GameState.Gameplay);
    }

    private void AutoConfigureScene()
    {
        _managers.clear();
        _managers.AddRange(FindObjectsByType<BaseManager>(FindObjectsSortMode.None));
        
        foreach (var m in _managers)
        {
          switch (m)
          {
            case InputManager im: inputManager ??= im; break;
            case TimeManager  tm: timeManager  ??= tm; break;
            case LevelManager lm: levelManager ??= lm; break;
            case DialogManager dm: dialogManager ??= dm; break;
          }
        }
        
        if (timeManager != null && beatConfig != null)
          timeManager.beatConfig = beatConfig;

        var existingPlayer = FindFirstObjectByType<Player>();
        if (existingPlayer != null && MainPlayer == null)
          RegisterPlayer(existingPlayer);

        EnsureAudioListener();
    }

    /// <summary>
    /// Configure -> Initialize -> Bind (does NOT call StartRuntime).
    /// </summary>
    private void RunManagerLifecycle()
    {
        foreach (var m in _managers) m.Configure(this);
        foreach (var m in _managers) m.Initialize();
        foreach (var m in _managers) m.BindEvents();

        DebugHelper.LogManager("RunManagerLifecycle completed (Configure/Initialize/Bind).");
        managersStarted = true;
    }

    private void StartAllManagers()
    {
      foreach (var m in _managers) m.StartRuntime();
    }
    #endregion

    #region Manager stop/unbind helpers
    public void StopManagers()
    {
        if (!managersStarted) return;

        try
        {
          foreach (var m in _managers) m.StopRuntime();
          foreach (var m in _managers) m.UnbindEvents();
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
        if (MainPlayer == p) return;
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
        if (State == next) return;

        var prev = State;
        DebugHelper.LogManager(&"Game state changed: {prev} -> {next}");
        State = next;

        Time.timeScale = (next == GameState.Gameplay) ? 1f : 0f;

        EventBus.Instance.Publish(new GameStateChangedEvent
            {
                from = prev,
                to = next;
            })
    }
    #endregion
}
