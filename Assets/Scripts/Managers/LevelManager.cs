using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Spawns player/enemies at inspector-defined points; publishes spawn events.
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

        if (gameManager != null && gameManager.MainPlayer == null && playerPrefab != null && playerSpawnPoints != null && playerSpawnPoints.Length > 0)
        {
            SpawnPlayer(playerSpawnPoints[0].position);
        }
    }

    public override void Pause(bool isPaused)
    {
        DebugHelper.LogManager($"LevelManager.Pause({isPaused})");
    }

    public override void Teardown()
    {
        DebugHelper.LogManager("LevelManager.Teardown()");
        EventBus.Instance?.Unsubscribe<GameplayEvent>(OnGameplayEvent);
        EventBus.Instance?.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
        isRunning = false;
    }
    #endregion

    #region Events
    private void OnGameplayEvent(GameplayEvent e)
        => DebugHelper.LogManager($"LevelManager received GameplayEvent: inCombat={e.inCombat}");

    private void OnPlayerDied(PlayerDiedEvent e)
    {
        DebugHelper.LogManager($"Player {e.player.name} died - showing death screen");
        ShowDeathScreen();
    }
    #endregion

    #region Spawning
    public Player SpawnPlayer(Vector3 worldPos)
    {
        if (playerPrefab == null) { DebugHelper.LogWarning("LevelManager: playerPrefab not assigned."); return null; }
        var go = Object.Instantiate(playerPrefab, worldPos, Quaternion.identity);
        var player = go.GetComponent<Player>();
        if (player == null) { DebugHelper.LogWarning("Spawned player prefab does not contain Player component."); return null; }
        GameManager.Instance?.RegisterPlayer(player);
        return player;
    }

    public Enemy SpawnEnemy(int prefabIndex, Vector3 worldPos)
    {
        if (enemyPrefabs == null || prefabIndex < 0 || prefabIndex >= enemyPrefabs.Length)
        { DebugHelper.LogWarning("LevelManager: invalid enemy prefab index."); return null; }

        var go = Object.Instantiate(enemyPrefabs[prefabIndex], worldPos, Quaternion.identity);
        var enemy = go.GetComponent<Enemy>();
        if (enemy == null) { DebugHelper.LogWarning("Spawned enemy prefab missing Enemy component."); return null; }
        EventBus.Instance?.Publish(new EnemySpawnedEvent { enemy = enemy });
        return enemy;
    }

    public void SpawnEnemyAtIndexToFirstPoint(int prefabIndex)
    {
        if (enemySpawnPoints == null || enemySpawnPoints.Length == 0) return;
        SpawnEnemy(prefabIndex, enemySpawnPoints[0].position);
    }
    #endregion

    #region Death Screen UI
    private void ShowDeathScreen()
    {
        if (deathScreenCanvas != null) return; // Already showing

        // Create Canvas using ComponentHelper
        GameObject canvasGO = ComponentHelper.CreateFullScreenCanvas("DeathScreenCanvas");

        // Create background panel
        Color backgroundColor = new Color(0, 0, 0, 0.8f); // Semi-transparent black
        GameObject panelGO = ComponentHelper.CreateFullScreenPanel(canvasGO, "DeathPanel", backgroundColor);

        // Create title text
        ComponentHelper.CreateText(panelGO, "DeathTitle", "GAME OVER", 48, Color.red, 
            TextAnchor.MiddleCenter, new Vector2(0, 0.6f), new Vector2(1, 0.8f));

        // Create Restart button
        Color buttonColor = new Color(0.3f, 0.3f, 0.3f, 0.9f);
        ComponentHelper.CreateButton(panelGO, "RestartButton", "RESTART", buttonColor,
            new Vector2(0.2f, 0.3f), new Vector2(0.4f, 0.4f), RestartGame);

        // Create Exit button
        ComponentHelper.CreateButton(panelGO, "ExitButton", "EXIT", buttonColor,
            new Vector2(0.6f, 0.3f), new Vector2(0.8f, 0.4f), ExitGame);

        // Set up gamepad navigation using Unity's built-in system
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