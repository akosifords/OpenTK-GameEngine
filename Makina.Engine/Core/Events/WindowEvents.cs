namespace Makina.Engine.Core.Events;

/// <summary>
/// Event published when the application window requests to close.
/// </summary>
public class WindowCloseEvent : Event
{
    // This event currently carries no specific data, but could if needed.
}

/// <summary>
/// Event published when the application window is resized.
/// </summary>
public class WindowResizeEvent : Event
{
    public int Width { get; }
    public int Height { get; }

    public WindowResizeEvent(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public override string ToString()
    {
        return $"{GetName()}: {Width}x{Height}";
    }
}

// TODO: Add other event types (Keyboard, Mouse, etc.) 