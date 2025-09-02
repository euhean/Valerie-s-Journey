using System;
using System.Collections.Generic;

/// <summary>
/// Minimal global event bus (singleton). Generic typed pub/sub.
/// </summary>
public sealed class EventBus
{
    #region Singleton
    private static readonly Lazy<EventBus> _lazy = new(() => new EventBus());
    public static EventBus Instance => _lazy.Value;
    private EventBus() { }
    #endregion

    #region State
    // map event type -> list of delegates (Action<T>)
    private readonly Dictionary<Type, List<Delegate>> _subscribers = new();
    #endregion

    #region Subscriptions
    public void Subscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        var t = typeof(T);
        if (!_subscribers.TryGetValue(t, out var list))
        {
            list = new List<Delegate>();
            _subscribers[t] = list;
        }
        if (!list.Contains(handler)) list.Add(handler);
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null) return;
        var t = typeof(T);
        if (_subscribers.TryGetValue(t, out var list))
        {
            list.Remove(handler);
            if (list.Count == 0) _subscribers.Remove(t);
        }
    }
    #endregion

    #region Publish
    public void Publish<T>(T evt)
    {
        var t = typeof(T);
        if (_subscribers.TryGetValue(t, out var list))
        {
            var copy = list.ToArray(); // avoid mutation during iteration
            foreach (var d in copy)
            {
                try { ((Action<T>)d)?.Invoke(evt); }
                catch (Exception ex) { UnityEngine.Debug.LogError($"EventBus handler error for {t.Name}: {ex}"); }
            }
        }
    }
    #endregion
}