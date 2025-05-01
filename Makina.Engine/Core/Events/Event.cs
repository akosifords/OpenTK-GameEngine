namespace Makina.Engine.Core.Events;

/// <summary>
/// Base class for all event types within the engine.
/// Allows for categorization and basic handling logic.
/// </summary>
public abstract class Event
{
    /// <summary>
    /// Indicates whether the event has been handled by a listener.
    /// A listener can set this to true to prevent further propagation if desired.
    /// </summary>
    public bool Handled { get; set; } = false;

    // Optional: Could add event categories or types here later
    // public virtual EventType Type => EventType.None;
    // public virtual EventCategory CategoryFlags => EventCategory.None;

    // Optional: A name for debugging/logging
    public virtual string GetName() => GetType().Name;

    public override string ToString() => GetName();
} 