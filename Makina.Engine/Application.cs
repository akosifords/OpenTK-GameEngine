using Makina.Engine.Core.Logging;
using NLog;
using Makina.Engine.Rendering;
using Makina.Engine.Core.Events;
using System;

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
        
        // Subscribe to events AFTER subsystems are initialized
        SubscribeToEvents();

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
        
        UnsubscribeFromEvents(); // Unsubscribe before shutdown
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
            
            Renderer.Init(); // Initialize Renderer AFTER window/context exists

            // Ensure window and renderer are valid before proceeding
            if (_window == null)
            {
                Log.Fatal("Cannot continue without a valid window.");
                // Potentially throw or handle more gracefully
                return; 
            }

            // Example: Renderer subscribing to resize events
            EventManager.Subscribe<WindowResizeEvent>(OnWindowResize); 

            // TODO: Initialize other subsystems (InputManager, etc.)

            Log.Info("Core systems initialized.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception during core system initialization");
            // Ensure window is disposed if renderer init failed after window creation
            _window?.Dispose(); 
            _window = null; 
        }
    }

    private void SubscribeToEvents()
    {
        // Example: Application subscribing to the window close event
        EventManager.Subscribe<WindowCloseEvent>(OnWindowClose); 
        Log.Trace("Application subscribed to events.");
    }
    
    private void UnsubscribeFromEvents()
    {
        EventManager.Unsubscribe<WindowCloseEvent>(OnWindowClose);
        EventManager.Unsubscribe<WindowResizeEvent>(OnWindowResize); // Ensure Renderer also unsubscribes if needed
        Log.Trace("Application unsubscribed from events.");
    }

    // --- Event Handlers ---

    private void OnWindowClose(WindowCloseEvent e)
    {
        Log.Info("WindowCloseEvent received by Application. Preparing to exit.");
        // The main loop condition (_window.IsClosing) will handle the actual exit.
        // We could set e.Handled = true here to prevent closing, if needed.
    }

    private void OnWindowResize(WindowResizeEvent e)
    {
        Log.Debug($"Application received WindowResizeEvent: {e.Width}x{e.Height}");
        // Potentially pause rendering or updates during resize if needed
        // Note: Viewport is already handled by Window.cs and potentially Renderer.cs
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
        Renderer.Clear(); // Clear the screen
        
        // TODO: Add scene rendering logic here
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