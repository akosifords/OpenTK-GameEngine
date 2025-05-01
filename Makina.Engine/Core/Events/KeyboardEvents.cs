using OpenTK.Windowing.GraphicsLibraryFramework; // For Keys enum

namespace Makina.Engine.Core.Events;

/// <summary>
/// Base class for keyboard-related events.
/// </summary>
public abstract class KeyEvent : Event
{
    public Keys KeyCode { get; protected set; }

    protected KeyEvent(Keys key)
    {
        KeyCode = key;
    }
}

/// <summary>
/// Event published when a keyboard key is pressed.
/// </summary>
public class KeyPressedEvent : KeyEvent
{
    // TODO: Add repeat count if needed?
    public KeyPressedEvent(Keys key) : base(key) { }

    public override string ToString() => $"{GetName()}: {KeyCode}";
}

/// <summary>
/// Event published when a keyboard key is released.
/// </summary>
public class KeyReleasedEvent : KeyEvent
{
    public KeyReleasedEvent(Keys key) : base(key) { }
    
    public override string ToString() => $"{GetName()}: {KeyCode}";
}

// Optional: Add KeyTypedEvent for character input later 