using Makina.Engine.Core.Logging;
using NLog;

namespace Makina.Engine;

public class Application : IDisposable
{
    private Window? _window;

    public Application()
    {
        // Constructor: Basic setup
        Log.Info("Makina Engine Initializing...");
    }

    public void Run()
    {
        // Main engine loop
        Initialize();
        
        // Ensure window was created
        if (_window == null)
        {
            Log.Error("Window failed to initialize.");
            return;
        }
        
        Log.Info("Entering main loop...");
        while (ShouldRun())
        {
            // Process window events first
            _window.ProcessEvents(); 

            Update();
            Render();
            
            // Swap buffers at the end of the frame
            _window.SwapBuffers();
        }
        Log.Info("Exited main loop.");
        
        Shutdown();
    }

    private void Initialize()
    { 
        // Initialize subsystems (Window, Input, Renderer, etc.)
        Log.Info("Initializing subsystems...");
        try
        {
            _window = new Window(); // Create the window
            Log.Info("Window created.");
            // TODO: Initialize other subsystems (Renderer, InputManager, etc.)
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception during window initialization");
            _window = null; // Ensure window is null if creation failed
        }
    }

    private bool ShouldRun()
    { 
        // Loop condition: check if window is closing
        return _window != null && !_window.IsClosing;
    }

    private void Update()
    { 
        // Update game state, handle input, run physics, etc.
        // Example: Check for Escape key press to close window (requires Input system later)
        // if (Input.IsKeyPressed(Keys.Escape)) { _window?.Close(); } 
    }

    private void Render()
    { 
        // Render the scene
        // Example: Clear the screen (requires Renderer setup)
        // Renderer.Clear(0.1f, 0.1f, 0.1f, 1.0f);
    }

    private void Shutdown()
    { 
        // Cleanup resources
        Log.Info("Shutting down subsystems...");
        // Dispose window last, as other systems might depend on it
        _window?.Dispose();
        Log.Info("Window disposed.");
        LogManager.Shutdown();
    }

    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }
}