using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Mathematics; // For Vector2i
using OpenTK.Windowing.GraphicsLibraryFramework; // Added for IBindingsContext
using Makina.Engine.Core.Logging; // Added

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

    public bool IsClosing => _nativeWindow.IsExiting;

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

        // Hook up events if needed (e.g., Resize, KeyDown, etc.)
        _nativeWindow.Resize += args => OnResize(args);
        // Add more event handlers here
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

    // Example Event Handler
    private void OnResize(ResizeEventArgs args)
    {
        Log.Debug($"Window resized to: {args.Width}x{args.Height}");
        // Need to set OpenGL viewport here if using OpenGL
        // GL.Viewport(0, 0, args.Width, args.Height);
    }

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