using Makina.Engine.Core.Logging;
using System;
using System.Collections.Generic;

namespace Makina.Engine.Core.Events;

public static class EventManager
{
    // Dictionary to hold subscribers. Key is the Event Type, Value is a list of delegates (actions).
    private static readonly Dictionary<Type, List<Delegate>> s_subscribers = new();

    /// <summary>
    /// Subscribes a handler to a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to subscribe to (must inherit from Event).</typeparam>
    /// <param name="handler">The Action delegate to execute when the event is published.</param>
    public static void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : Event
    {
        Type eventType = typeof(TEvent);
        if (!s_subscribers.ContainsKey(eventType))
        {
            s_subscribers[eventType] = new List<Delegate>();
        }

        if (!s_subscribers[eventType].Contains(handler))
        {
            s_subscribers[eventType].Add(handler);
            // Log.Trace($"Subscribed handler {handler.Method.Name} to event {eventType.Name}");
        }
        else
        {
            Log.Warn($"Handler {handler.Method.Name} already subscribed to event {eventType.Name}");
        }
    }

    /// <summary>
    /// Unsubscribes a handler from a specific event type.
    /// </summary>
    /// <typeparam name="TEvent">The type of event to unsubscribe from.</typeparam>
    /// <param name="handler">The Action delegate to remove.</param>
    public static void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : Event
    {
        Type eventType = typeof(TEvent);
        if (s_subscribers.TryGetValue(eventType, out var handlers))
        {
            if (handlers.Remove(handler))
            {
                // Log.Trace($"Unsubscribed handler {handler.Method.Name} from event {eventType.Name}");
            }
            
            // Optional: Remove dictionary entry if list becomes empty
            if (handlers.Count == 0)
            {
                 s_subscribers.Remove(eventType);
            }
        }
    }

    /// <summary>
    /// Publishes an event to all subscribed handlers.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event being published.</typeparam>
    /// <param name="eventArgs">The event object containing data.</param>
    public static void Publish<TEvent>(TEvent eventArgs) where TEvent : Event
    {
        Type eventType = typeof(TEvent);
        if (s_subscribers.TryGetValue(eventType, out var handlers))
        {
            // Log.Trace($"Publishing event: {eventArgs}");
            // Iterate over a copy in case handlers modify the collection during iteration
            foreach (var handlerDelegate in handlers.ToList()) 
            {
                // Check if the event has already been handled by a previous listener
                if (eventArgs.Handled)
                {
                    break; // Stop propagation if handled
                }
                
                // Safely cast and invoke the handler
                if (handlerDelegate is Action<TEvent> specificHandler)
                {
                    try
                    {
                        specificHandler(eventArgs);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, $"Exception in event handler {specificHandler.Method.Name} for event {eventType.Name}");
                    }
                }
            }
        }
    }
} 