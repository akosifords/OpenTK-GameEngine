using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics; // For Vector2i
using OpenTK.Windowing.GraphicsLibraryFramework; // Added for IBindingsContext
using Makina.Engine.Core.Logging; // Added
using OpenTK.Graphics.OpenGL4; // Added for GL calls
using Makina.Engine.Core.Events; // Added
using Makina.Engine.Input; // Added
using System;

namespace Makina.Engine;

public class Window : IDisposable
{
    private readonly GameWindow _nativeWindow;

    public string Title
    {
        get => _nativeWindow.Title;
        set => _nativeWindow.Title = value;
    }

    public Vector2i Size
    {
        get => _nativeWindow.ClientSize;
        set => _nativeWindow.ClientSize = value;
    }

    public bool IsClosing { get; private set; } = false;

    // Expose CursorState from the underlying GameWindow
    public CursorState CursorState 
    {
        get => _nativeWindow.CursorState;
        set => _nativeWindow.CursorState = value;
    }

    public Window(string title = "Makina Engine", int width = 1280, int height = 720)
    {
        var gameWindowSettings = GameWindowSettings.Default;
        var nativeWindowSettings = new NativeWindowSettings()
        {
            ClientSize = new Vector2i(width, height),
            Title = title,
            // Optional: Add more settings like API version, profile, flags etc.
            // APIVersion = new Version(4, 6), // Example for OpenGL 4.6
            // Profile = ContextProfile.Core,
            // Flags = ContextFlags.ForwardCompatible,
        };

        _nativeWindow = new GameWindow(gameWindowSettings, nativeWindowSettings);
        Log.Info($"Native window created: Title='{Title}', Size={Size.X}x{Size.Y}");

        // Hook up OpenTK events to publish engine events
        _nativeWindow.Resize += OnResize;
        _nativeWindow.Closing += OnClosing;
        _nativeWindow.KeyDown += OnKeyDown;
        _nativeWindow.KeyUp += OnKeyUp;
        _nativeWindow.MouseDown += OnMouseDown;
        _nativeWindow.MouseUp += OnMouseUp;
        _nativeWindow.MouseMove += OnMouseMove;
        _nativeWindow.MouseWheel += OnMouseWheel;
        _nativeWindow.TextInput += OnTextInput; // Added TextInput event hook
    }

    public void ProcessEvents()
    {
        // This processes messages and dispatches events.
        // OpenTK recommends using a different approach involving `glfwPollEvents`.
        // However, `ProcessWindowEvents(false)` is often sufficient and simpler.
        // Passing 0 for timeout makes it non-blocking.
        _nativeWindow.ProcessEvents(0);
    }
    
    public void SwapBuffers()
    {
        _nativeWindow.SwapBuffers();
    }

    // --- Event Handlers --- 
    
    private void OnResize(ResizeEventArgs args)
    {
        Log.Trace($"Native window resize event: {args.Width}x{args.Height}");
        // Set OpenGL viewport directly (Renderer might also subscribe to this event)
        GL.Viewport(0, 0, args.Width, args.Height);
        
        // Publish the engine event
        var resizeEvent = new WindowResizeEvent(args.Width, args.Height);
        EventManager.Publish(resizeEvent);
    }

    private void OnClosing(System.ComponentModel.CancelEventArgs args)
    {
        Log.Trace("Native window closing event received.");
        IsClosing = true; // Set internal flag
        
        // Publish the engine event
        var closeEvent = new WindowCloseEvent();
        EventManager.Publish(closeEvent);
        
        // We could potentially allow a listener to cancel closing by setting closeEvent.Handled = true
        // if (closeEvent.Handled) { args.Cancel = true; IsClosing = false; }
    }

    private void OnKeyDown(KeyboardKeyEventArgs args)
    {
        // Don't process unknown keys or repeats from OS for KeyDown
        if (args.IsRepeat || args.Key == Keys.Unknown) return; 

        Log.Trace($"Native KeyDown: {args.Key}");
        InputManager.SetKeyDown(args.Key);
        EventManager.Publish(new KeyPressedEvent(args.Key));
    }

    private void OnKeyUp(KeyboardKeyEventArgs args)
    {
        if (args.Key == Keys.Unknown) return; 
        
        Log.Trace($"Native KeyUp: {args.Key}");
        InputManager.SetKeyUp(args.Key);
        EventManager.Publish(new KeyReleasedEvent(args.Key));
    }

    private void OnMouseDown(MouseButtonEventArgs args)
    {
        Log.Trace($"Native MouseDown: {args.Button}");
        InputManager.SetMouseButtonDown(args.Button);
        EventManager.Publish(new MouseButtonPressedEvent(args.Button));
    }

    private void OnMouseUp(MouseButtonEventArgs args)
    {
        Log.Trace($"Native MouseUp: {args.Button}");
        InputManager.SetMouseButtonUp(args.Button);
        EventManager.Publish(new MouseButtonReleasedEvent(args.Button));
    }

    private void OnMouseMove(MouseMoveEventArgs args)
    {
        // Log.Trace($"Native MouseMove: ({args.X}, {args.Y})"); // Can be very noisy
        Vector2 position = new Vector2(args.X, args.Y);
        InputManager.SetMousePosition(position);
        EventManager.Publish(new MouseMovedEvent(position));
    }

    private void OnMouseWheel(MouseWheelEventArgs args)
    {
        Log.Trace($"Native MouseWheel: ({args.OffsetX}, {args.OffsetY})");
        Vector2 offset = new Vector2(args.OffsetX, args.OffsetY);
        InputManager.SetMouseScroll(offset);
        EventManager.Publish(new MouseScrolledEvent(offset));
    }

    // Event publisher for TextInput - publish a custom event if needed later,
    // or directly call controller method.
    private void OnTextInput(TextInputEventArgs args)
    {
        // For now, Application will subscribe directly or handle this.
        // We could create a custom TextInputEvent if needed.
        // Log.Trace($"Native TextInput: {(char)args.Unicode}");
        // EventManager.Publish(new TextInputEvent((char)args.Unicode));
        
        // Alternatively, directly raise an event Application subscribes to
         TextInput?.Invoke(args); // Raise C# event
    }
    
    // C# event for Application to subscribe to
    public event Action<TextInputEventArgs>? TextInput;

    // Added method to access the underlying GameWindow
    public GameWindow GetNativeWindow() => _nativeWindow;

    public void Dispose()
    {
        // Dispose the native window resources
        Log.Info("Disposing native window.");
        _nativeWindow.Dispose();
        GC.SuppressFinalize(this);
    }

    // --- Internal Access for Graphics Context ---
    // Provide a way for the Renderer to access the underlying context if needed
    // internal IBindingsContext? GetBindingsContext() => _nativeWindow.Context; // Removed - Context is managed differently
}