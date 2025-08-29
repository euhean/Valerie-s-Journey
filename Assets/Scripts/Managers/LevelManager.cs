using UnityEngine;

/// <summary>
/// Minimal LevelManager: responsible for spawning player & enemies at inspector-defined spawn points.
/// Keeps responsibilities small and publishes spawn events via EventBus.
/// Lifecycle-aware (Configure/Initialize/Bind/Start/Pause/Teardown).
/// </summary>
public class LevelManager : BaseManager {
    [Header("Prefabs (assign in inspector)")]
    public GameObject playerPrefab;
    public GameObject[] enemyPrefabs;

    [Header("Spawn points (assign transforms in inspector)")]
    public Transform[] playerSpawnPoints;
    public Transform[] enemySpawnPoints;

    bool isRunning = false;

    public override void Configure(GameManager gm) {
        base.Configure(gm);
        DebugHelper.LogManager("LevelManager.Configure()");
    }

    public override void Initialize() {
        DebugHelper.LogManager("LevelManager.Initialize()");
        // Basic validation of inspector configuration (non-fatal)
        if (playerPrefab == null) DebugHelper.LogWarning("LevelManager: playerPrefab not assigned.");
        if (enemyPrefabs == null || enemyPrefabs.Length == 0) DebugHelper.LogWarning("LevelManager: no enemyPrefabs assigned.");
    }

    public override void BindEvents() {
        DebugHelper.LogManager("LevelManager.BindEvents()");
        // Example subscription to gameplay events so level reacts (spawn waves, pause, etc.)
        EventBus.Instance?.Subscribe<GameplayEvent>(OnGameplayEvent);
    }

    public override void StartRuntime() {
        DebugHelper.LogManager("LevelManager.StartRuntime()");
        if (isRunning) return;
        isRunning = true;

        // If there's no player present and a prefab & spawn point exist, spawn one now
        if (gameManager != null && gameManager.MainPlayer == null && playerPrefab != null && playerSpawnPoints != null && playerSpawnPoints.Length > 0) {
            var spawnPos = playerSpawnPoints[0].position;
            SpawnPlayer(spawnPos);
        }
    }

    public override void Pause(bool isPaused) {
        DebugHelper.LogManager($"LevelManager.Pause({isPaused})");
        // Pause-level behavior (not used in simple prototype)
    }

    public override void Teardown() {
        DebugHelper.LogManager("LevelManager.Teardown()");
        EventBus.Instance?.Unsubscribe<GameplayEvent>(OnGameplayEvent);
        isRunning = false;
    }

    private void OnGameplayEvent(GameplayEvent e) {
        // react to gameplay state toggles if needed (e.g., spawn wave on inCombat)
        DebugHelper.LogManager($"LevelManager received GameplayEvent: inCombat={e.inCombat}");
    }

    public Player SpawnPlayer(Vector3 worldPos) {
        if (playerPrefab == null) {
            DebugHelper.LogWarning("LevelManager: playerPrefab not assigned.");
            return null;
        }
        var go = UnityEngine.Object.Instantiate(playerPrefab, worldPos, Quaternion.identity);
        var player = go.GetComponent<Player>();
        if (player == null) {
            DebugHelper.LogWarning("Spawned player prefab does not contain Player component.");
            return null;
        }
        GameManager.Instance?.RegisterPlayer(player);
        return player;
    }

    public Enemy SpawnEnemy(int prefabIndex, Vector3 worldPos) {
        if (enemyPrefabs == null || prefabIndex < 0 || prefabIndex >= enemyPrefabs.Length) {
            DebugHelper.LogWarning("LevelManager: invalid enemy prefab index.");
            return null;
        }
        var go = UnityEngine.Object.Instantiate(enemyPrefabs[prefabIndex], worldPos, Quaternion.identity);
        var enemy = go.GetComponent<Enemy>();
        if (enemy == null) {
            DebugHelper.LogWarning("Spawned enemy prefab missing Enemy component.");
            return null;
        }
        EventBus.Instance?.Publish(new EnemySpawnedEvent { enemy = enemy });
        return enemy;
    }

    // Placeholder method
    public void SpawnEnemyAtIndexToFirstPoint(int prefabIndex) {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return;
        SpawnEnemy(prefabIndex, enemySpawnPoints[0].position);
    }
}