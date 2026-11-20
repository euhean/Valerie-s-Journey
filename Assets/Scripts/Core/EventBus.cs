using System;
using System.Collections.Generic;

/// <summary>
/// Global typed publish/subscribe bus. The single path every gameplay event travels on.
/// Subscriber lists are copy-on-write arrays: Subscribe/Unsubscribe build a new array,
/// Publish iterates the array it read. Handlers can therefore (un)subscribe while an
/// event is being published, and Publish allocates nothing.
/// </summary>
public sealed class EventBus
{
    public static EventBus Instance { get; } = new EventBus();
    private EventBus() { }

    private readonly Dictionary<Type, Delegate[]> subscribers = new();
    private readonly object gate = new();

    public void Subscribe<T>(Action<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        lock (gate)
        {
            if (!subscribers.TryGetValue(typeof(T), out var current))
            {
                subscribers[typeof(T)] = new Delegate[] { handler };
                return;
            }
            if (Array.IndexOf(current, handler) >= 0) return; // already subscribed

            var next = new Delegate[current.Length + 1];
            Array.Copy(current, next, current.Length);
            next[current.Length] = handler;
            subscribers[typeof(T)] = next;
        }
    }

    public void Unsubscribe<T>(Action<T> handler)
    {
        if (handler == null) throw new ArgumentNullException(nameof(handler));
        lock (gate)
        {
            if (!subscribers.TryGetValue(typeof(T), out var current)) return;
            int index = Array.IndexOf(current, handler);
            if (index < 0) return;

            if (current.Length == 1)
            {
                subscribers.Remove(typeof(T));
                return;
            }
            var next = new Delegate[current.Length - 1];
            Array.Copy(current, 0, next, 0, index);
            Array.Copy(current, index + 1, next, index, current.Length - index - 1);
            subscribers[typeof(T)] = next;
        }
    }

    /// <summary>
    /// Calls every subscriber of T. A handler that throws is logged and does not stop the others.
    /// </summary>
    public void Publish<T>(T evt)
    {
        Delegate[] handlers;
        lock (gate)
        {
            if (!subscribers.TryGetValue(typeof(T), out handlers)) return;
        }

        foreach (var handler in handlers)
        {
            try
            {
                ((Action<T>)handler).Invoke(evt);
            }
            catch (Exception ex)
            {
                DebugHelper.LogError($"EventBus handler error for {typeof(T).Name}: {ex.Message}");
                DebugHelper.LogException(ex);
            }
        }
    }
}
