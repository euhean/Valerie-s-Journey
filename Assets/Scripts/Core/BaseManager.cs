using UnityEngine;

/// <summary>
/// Abstract base for all managers. GameManager drives the lifecycle explicitly:
/// Configure → Initialize → BindEvents → StartRuntime → StopRuntime → UnbindEvents.
/// StopRuntime and UnbindEvents must be safe to call more than once.
/// </summary>
public abstract class BaseManager : MonoBehaviour
{
    protected GameManager gameManager;

    public virtual void Configure(GameManager gm) => gameManager = gm;
    public virtual void Initialize() {}
    public virtual void BindEvents() {}
    public virtual void StartRuntime() {}
    public virtual void StopRuntime() {}
    public virtual void UnbindEvents() {}

    /// <summary>
    /// A manager cleans up after itself when its scene unloads, so nobody has to call
    /// into an already-destroyed object.
    /// </summary>
    protected virtual void OnDisable()
    {
        StopRuntime();
        UnbindEvents();
    }
}
