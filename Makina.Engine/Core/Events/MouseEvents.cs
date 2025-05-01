using OpenTK.Windowing.GraphicsLibraryFramework; // For MouseButton enum
using OpenTK.Mathematics; // For Vector2

namespace Makina.Engine.Core.Events;

// --- Mouse Button Events ---

/// <summary>
/// Base class for mouse button events.
/// </summary>
public abstract class MouseButtonEvent : Event
{
    public MouseButton Button { get; protected set; }

    protected MouseButtonEvent(MouseButton button)
    {
        Button = button;
    }
}

/// <summary>
/// Event published when a mouse button is pressed.
/// </summary>
public class MouseButtonPressedEvent : MouseButtonEvent
{
    public MouseButtonPressedEvent(MouseButton button) : base(button) { }

    public override string ToString() => $"{GetName()}: {Button}";
}

/// <summary>
/// Event published when a mouse button is released.
/// </summary>
public class MouseButtonReleasedEvent : MouseButtonEvent
{
    public MouseButtonReleasedEvent(MouseButton button) : base(button) { }
    
    public override string ToString() => $"{GetName()}: {Button}";
}

// --- Mouse Move Event ---

/// <summary>
/// Event published when the mouse cursor moves.
/// </summary>
public class MouseMovedEvent : Event
{
    public Vector2 Position { get; }
    // Optional: public Vector2 Delta { get; } // Can be useful

    public MouseMovedEvent(Vector2 position)
    {
        Position = position;
    }

    public override string ToString() => $"{GetName()}: ({Position.X}, {Position.Y})";
}

// --- Mouse Scroll Event ---

/// <summary>
/// Event published when the mouse wheel is scrolled.
/// </summary>
public class MouseScrolledEvent : Event
{
    public Vector2 Offset { get; } // X is horizontal scroll, Y is vertical

    public MouseScrolledEvent(Vector2 offset)
    {
        Offset = offset;
    }

    public override string ToString() => $"{GetName()}: ({Offset.X}, {Offset.Y})";
} 