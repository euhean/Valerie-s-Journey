/// <summary>
/// Abstract base for all managers. Defines a lifecycle
/// that GameManager will call explicitly. Never instantiate directly.
/// </summary>
public abstract class BaseManager : MonoBehaviour {
    protected GameManager gameManager;

    public virtual void Configure(GameManager gm) => gameManager = gm;
    public virtual void Initialize() {}
    public virtual void BindEvents() {}
    public virtual void StartRuntime() {}
    public virtual void Pause(bool isPaused) {}
    public virtual void Teardown() {}
}