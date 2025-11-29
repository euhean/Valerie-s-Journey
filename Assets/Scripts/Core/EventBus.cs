using System;
using System.Collections.Generic;

/// <summary>
/// Global event bus (singleton). Thread-safe pub/sub with snapshot iteration.
/// CRITICAL: Uses snapshot copy during publish to prevent modification-during-iteration crashes.
/// </summary>
public sealed class EventBus
{
    #region Singleton
    private static readonly Lazy<EventBus> _lazy = new(() => new EventBus());
    public static EventBus Instance => _lazy.Value;
    private EventBus() { }
    #endregion

    #region State
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    private readonly object _lock = new();
    #endregion

    #region Subscriptions
    public void Subscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        var t = typeof(T);
        lock (_lock)
        {
            if (!_subscribers.TryGetValue(t, out var list))
            {
                list = new List<Delegate>();
                _subscribers[t] = list;
            }
            if (!list.Contains(handler)) list.Add(handler);
        }
    }

    /// <summary>
    /// One-shot subscription: auto-unsubscribes after first invocation
    /// </summary>
    public void SubscribeOnce<T>(Action<T> handler)
    {
        if (handler == null) return;
        Action<T> wrapper = null;
        wrapper = (evt) =>
        {
            try { handler(evt); }
            finally { Unsubscribe<T>(wrapper); }
        };
        Subscribe<T>(wrapper);
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        var t = typeof(T);
        lock (_lock)
        {
            if (_subscribers.TryGetValue(t, out var list))
            {
                list.Remove(handler);
                if (list.Count == 0) _subscribers.Remove(t);
            }
        }
    }

    /// <summary>
    /// DANGER: Clears all subscriptions. Only call during full game reset.
    /// </summary>
    public void ClearAll()
    {
        lock (_lock) { _subscribers.Clear(); }
    }
    #endregion

    #region Publish
    /// <summary>
    /// Publishes event to all subscribers. Uses snapshot to prevent modification-during-iteration.
    /// Exceptions in handlers are caught and logged (doesn't crash other handlers).
    /// </summary>
    public void Publish<T>(T evt)
    {
        var t = typeof(T);
        List<Delegate> copy = null;
        lock (_lock)
        {
            if (_subscribers.TryGetValue(t, out var list))
                copy = new List<Delegate>(list); // Snapshot prevents concurrent modification
        }

        if (copy == null) return;

        foreach (var d in copy)
        {
            try
            {
                ((Action<T>)d)?.Invoke(evt);
            }
            catch (Exception ex)
            {
                // Isolated failure: log but continue to other handlers
                DebugHelper.LogError($"EventBus handler error for {t.Name}: {ex.Message}");
                DebugHelper.LogException(ex);
            }
        }
    }
    #endregion
}