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
        ExitOrContinue,
        Gameplay
    }

    [Header("Managers")]
    public InputManager inputManager;
    public TimeManager  timeManager;

    public GameState State { get; private set; } = GameState.Boot;

    private void Awake() {
        // Wire managers to this GameManager
        inputManager.Configure(this);
        timeManager.Configure(this);

        // Initialize and bind events
        inputManager.Initialize();
        timeManager.Initialize();
        inputManager.BindEvents();
        timeManager.BindEvents();

        // For now, go straight into gameplay
        ChangeState(GameState.Gameplay);
    }

    public void ChangeState(GameState next) {
        State = next;
        if (State == GameState.Gameplay) {
            inputManager.StartRuntime();
            timeManager.StartRuntime();
        }
        // Add other state transitions here as your flow expands
    }
}