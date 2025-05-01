using Makina.Engine.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Makina.Engine.Core.Events;

public static class EventManager
{
    // Dictionary to hold subscribers. Key is the Event Type, Value is a list of delegates.
    private static readonly Dictionary<Type, List<Delegate>> s_subscribers = new();
    // Queue to hold events published during a frame.
    private static readonly Queue<Event> s_eventQueue = new();

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
    /// Queues an event to be dispatched later.
    /// </summary>
    /// <typeparam name="TEvent">The type of the event being published.</typeparam>
    /// <param name="eventArgs">The event object containing data.</param>
    public static void Publish<TEvent>(TEvent eventArgs) where TEvent : Event
    {
        // Log.Trace($"Queueing event: {eventArgs}");
        // NOTE: Consider thread safety if events can be published from multiple threads.
        // A ConcurrentQueue might be needed, or locking around enqueue/dequeue.
        s_eventQueue.Enqueue(eventArgs);
    }

    /// <summary>
    /// Dispatches all queued events to their respective subscribers.
    /// Should be called once per frame/update cycle.
    /// </summary>
    public static void DispatchQueuedEvents()
    {
        // Process events currently in the queue. New events published during dispatch
        // will be processed in the next cycle.
        int eventsToProcess = s_eventQueue.Count;
        if (eventsToProcess == 0) return;
        
        // Log.Trace($"Dispatching {eventsToProcess} queued events...");

        for (int i = 0; i < eventsToProcess; i++)
        {
            Event currentEvent = s_eventQueue.Dequeue();
            Type eventType = currentEvent.GetType(); // Get the actual runtime type

            if (s_subscribers.TryGetValue(eventType, out var handlers))
            {
                 // Iterate over a copy in case handlers modify the collection during iteration (e.g., unsubscribe)
                foreach (var handlerDelegate in handlers.ToList()) 
                {
                    // Check if the event has already been handled by a previous listener
                    if (currentEvent.Handled)
                    {
                        // Log.Trace($"Event {currentEvent.GetName()} already handled, skipping remaining listeners.");
                        break; // Stop propagation if handled
                    }

                    try
                    {
                        // Invoke the delegate with the specific event instance
                        // Note: This relies on the delegate signature matching the event type stored in the dictionary key
                        // which is enforced by the Subscribe<TEvent> generic constraint.
                        handlerDelegate.DynamicInvoke(currentEvent);
                    }
                    catch (Exception ex)
                    {
                        // Log potential exceptions during DynamicInvoke or within the handler itself
                        Log.Error(ex, $"Exception during event dispatch for {eventType.Name} with handler {handlerDelegate.Method.Name}");
                    }
                }
            }
        }
        
        // Check if new events were queued during dispatch (less common, but possible)
        if (s_eventQueue.Count > 0)
        {
            Log.Warn($"{s_eventQueue.Count} events were queued during event dispatch. They will be processed next cycle.");
        }
    }
} 