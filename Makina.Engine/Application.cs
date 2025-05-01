using Makina.Engine.Core.Logging;
using NLog;
using Makina.Engine.Rendering;
using Makina.Engine.Core.Events;
using Makina.Engine.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
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
            // 1. Process native window events (pumps OS messages, triggers OpenTK callbacks like OnResize/OnClosing)
            _window.ProcessEvents(); 
            
            // 2. Dispatch queued engine events (allows systems to react to events published in step 1 or previous frame)
            EventManager.DispatchQueuedEvents();

            // 3. Update application logic / game state
            Update();
            
            // 4. Render the scene
            Render();
            
            // 5. Reset per-frame input state
            InputManager.FrameReset();

            // 6. Swap buffers
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
        // Example: Check for Escape key press to close window
        if (InputManager.IsKeyPressed(Keys.Escape))
        {
            Log.Info("Escape key pressed, publishing WindowCloseEvent.");
            EventManager.Publish(new WindowCloseEvent());
            // Note: The actual closing happens because Window.IsClosing gets set
            // when the event is published from OnClosing in Window.cs.
            // This just demonstrates using InputManager.
        }
        
        // You can also check for continuous key hold:
        //if (InputManager.IsKeyDown(Keys.W)) { Log.Debug("W key is held down"); }
        
        // Check mouse position
        Log.Trace($"Mouse Position: {InputManager.GetMousePosition()}");
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