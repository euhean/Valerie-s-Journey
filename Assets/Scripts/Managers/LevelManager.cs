using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns player/enemies at inspector-defined points; publishes spawn events.
/// Delegates death-screen UI to ComponentHelper (uses CreateFullScreenCanvas / CreateFullScreenPanel / CreateText / CreateButton / SetupUINavigation).
/// </summary>
public class LevelManager : BaseManager
{
    #region Inspector
    [Header("Prefabs (assign in inspector)")]
    public GameObject playerPrefab;
    public GameObject[] enemyPrefabs;

    [Header("Spawn points (assign transforms in inspector)")]
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;

    [Header("Runtime Controls")]
    [SerializeField]
    private bool autoSpawnEnemies = false;
    #endregion

    #region Private
    bool isRunning = false;
    private GameObject deathScreenCanvas;
    #endregion

    #region Lifecycle
    public override void Configure(GameManager gm)
    {
        base.Configure(gm);
        DebugHelper.LogManager("LevelManager.Configure()");
    }

    public override void Initialize()
    {
        DebugHelper.LogManager("LevelManager.Initialize()");
        if (playerPrefab == null) DebugHelper.LogWarning("LevelManager: playerPrefab not assigned.");
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) DebugHelper.LogWarning("LevelManager: no enemyPrefabs assigned.");
        if (playerSpawnPoints == null || playerSpawnPoints.Length == 0) DebugHelper.LogWarning("LevelManager: no player spawn points assigned.");
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) DebugHelper.LogWarning("LevelManager: no enemy spawn points assigned.");
    }

    public override void BindEvents()
    {
        DebugHelper.LogManager("LevelManager.BindEvents()");
        EventBus.Instance?.Subscribe<GameplayEvent>(OnGameplayEvent);
        EventBus.Instance?.Subscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    public override void StartRuntime()
    {
        DebugHelper.LogManager("LevelManager.StartRuntime()");
        if (isRunning) return;
        isRunning = true;

        // Spawn player if none exists
        if (gameManager != null && gameManager.MainPlayer == null)
        {
            var sp = FirstValid(playerSpawnPoints);
            if (!playerPrefab) DebugHelper.LogError("LevelManager: Cannot spawn player (no prefab).");
            else if (!sp) DebugHelper.LogError("LevelManager: Cannot spawn player (no valid spawn point).");
            else SpawnPlayer(sp.position);
        }

        if (autoSpawnEnemies)
        {
            // Spawn identical enemy minions at every valid spawn point using prefab[0]
            if (enemyPrefabs == null || enemyPrefabs.Length == 0 || enemyPrefabs[0] == null)
            {
                DebugHelper.LogWarning("LevelManager: enemyPrefabs[0] not assigned — minions not spawned.");
            }
            else if (enemySpawnPoints != null && enemySpawnPoints.Length > 0)
            {
                for (int i = 0; i < enemySpawnPoints.Length; i++)
                {
                    var spawnPoint = enemySpawnPoints[i];
                    if (!spawnPoint) continue;
                    SpawnEnemy(0, spawnPoint.position);
                }
            }
        }
    }

    public override void Pause(bool isPaused)
    {
        DebugHelper.LogManager($"LevelManager.Pause({isPaused})");
    }

    public override void StopRuntime()
    {
        DebugHelper.LogManager("LevelManager.StopRuntime()");
        isRunning = false;
    }

    public override void UnbindEvents()
    {
        DebugHelper.LogManager("LevelManager.UnbindEvents()");
        EventBus.Instance?.Unsubscribe<GameplayEvent>(OnGameplayEvent);
        EventBus.Instance?.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    public override void Teardown()
    {
        DebugHelper.LogManager("LevelManager.Teardown()");
        StopRuntime();
        UnbindEvents();
    }
    #endregion

    #region Events
    private void OnGameplayEvent(GameplayEvent e)
        => DebugHelper.LogManager($"LevelManager received GameplayEvent: inCombat={e.inCombat}");

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        if (e?.player != null)
            DebugHelper.LogManager($"Player {e.player.name} died - showing death screen via ComponentHelper");
        else
            DebugHelper.LogWarning("LevelManager received PlayerDiedEvent without player reference.");

        gameManager?.HandlePlayerDeath();
        ShowDeathScreen();
    }
    #endregion

    #region Spawning
    public Player SpawnPlayer(Vector3 worldPos)
    {
        if (!playerPrefab)
        {
            DebugHelper.LogWarning("LevelManager: playerPrefab not assigned.");
            return null;
        }

        var go = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        var player = go.GetComponent<Player>();
        if (!player)
        {
            Destroy(go); // Clean up the instantiated GameObject to prevent memory leak
            DebugHelper.LogWarning("Spawned player prefab does not contain Player component.");
            return null;
        }

        GameManager.Instance?.RegisterPlayer(player);

        // Publish your existing event type
        EventBus.Instance?.Publish(new PlayerSpawnedEvent { player = player });

        DebugHelper.LogManager("LevelManager: Player spawned and registered.");
        return player;
    }

    public Enemy SpawnEnemy(int prefabIndex, Vector3 worldPos)
    {
        if (enemyPrefabs == null || enemyPrefabs.Length == 0)
        {
            DebugHelper.LogWarning("LevelManager: no enemyPrefabs assigned.");
            return null;
        }

        if (prefabIndex < 0 || prefabIndex >= enemyPrefabs.Length)
        {
            DebugHelper.LogWarning($"LevelManager: invalid enemy prefab index {prefabIndex}.");
            return null;
        }

        var prefab = enemyPrefabs[prefabIndex];
        if (!prefab)
        {
            DebugHelper.LogWarning($"LevelManager: enemy prefab at index {prefabIndex} is null.");
            return null;
        }

        var go = Instantiate(prefab, worldPos, Quaternion.identity);
        var enemy = go.GetComponent<Enemy>();
        if (!enemy)
        {
            DebugHelper.LogWarning("Spawned enemy prefab missing Enemy component.");
            Destroy(go);
            return null;
        }

        EventBus.Instance?.Publish(new EnemySpawnedEvent { enemy = enemy });
        DebugHelper.LogManager("LevelManager: Enemy spawned.");
        return enemy;
    }

    public void SpawnEnemyAtIndexToFirstPoint(int prefabIndex)
    {
        var sp = FirstValid(enemySpawnPoints);
        if (!sp) return;
        SpawnEnemy(prefabIndex, sp.position);
    }

    private Transform FirstValid(Transform[] points)
    {
        if (points == null) return null;
        foreach (var t in points) if (t) return t;
        return null;
    }
    #endregion

    #region Death Screen (delegates to ComponentHelper)
    private void ShowDeathScreen()
    {
        if (deathScreenCanvas != null)
        {
            DebugHelper.LogManager("[LevelManager] ShowDeathScreen called but canvas already exists (ignoring)");
            return; // Already showing
        }

        DebugHelper.LogManager("[LevelManager] ShowDeathScreen invoked - constructing overlay UI");

        // Use ComponentHelper to create the UI (your helper defines these exact APIs)
        GameObject canvasGO = ComponentHelper.CreateFullScreenCanvas("DeathScreenCanvas");

        Color backgroundColor = new Color(0, 0, 0, 0.8f); // Semi-transparent black
        GameObject panelGO = ComponentHelper.CreateFullScreenPanel(canvasGO, "DeathPanel", backgroundColor);

        ComponentHelper.CreateText(panelGO, "DeathTitle", "GAME OVER", 48, Color.red,
            TextAnchor.MiddleCenter, new Vector2(0, 0.6f), new Vector2(1, 0.8f));

        Color buttonColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        ComponentHelper.CreateButton(panelGO, "RestartButton", "RESTART", buttonColor,
            new Vector2(0.2f, 0.3f), new Vector2(0.4f, 0.4f), RestartGame);

        ComponentHelper.CreateButton(panelGO, "ExitButton", "EXIT", buttonColor,
            new Vector2(0.6f, 0.3f), new Vector2(0.8f, 0.4f), ExitGame);

        ComponentHelper.SetupUINavigation(panelGO);

        deathScreenCanvas = canvasGO;
    }

    private void RestartGame()
    {
        DebugHelper.LogManager("Restarting game...");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void ExitGame()
    {
        DebugHelper.LogManager("Exiting game...");
    #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
    #endregion
}