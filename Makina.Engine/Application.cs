using Makina.Engine.Core.Logging;
using NLog;
using Makina.Engine.Rendering;
using Makina.Engine.Core.Events;
using Makina.Engine.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using Makina.Engine.Rendering.Buffers;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Makina.Engine;

public class Application : IDisposable
{
    private Window? _window;
    
    // Triangle Rendering Resources
    private Shader? _shader;
    private VertexArray? _vertexArray;
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;

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
            Log.Error("Window failed to initialize or required resources could not be created.");
            return;
        }
        
        // Subscribe to events AFTER subsystems are initialized
        SubscribeToEvents();

        Log.Info("Entering main loop...");
        while (ShouldRun())
        {
            // 1. Process native window events
            _window.ProcessEvents(); 
            
            // 2. Dispatch queued engine events
            EventManager.DispatchQueuedEvents();

            // 3. Update application logic
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
        Log.Info("Initializing subsystems...");
        try
        {
            _window = new Window(); 
            Log.Info("Window created.");
            
            Renderer.Init(); 
            Log.Info("Renderer initialized.");

            // --- Setup Triangle --- 
            Log.Info("Setting up triangle geometry...");

            // 1. Define Vertices (Position only)
            float[] vertices = {
                // Position       
                 0.0f,  0.5f, 0.0f, // Top center
                -0.5f, -0.5f, 0.0f, // Bottom left
                 0.5f, -0.5f, 0.0f  // Bottom right
            };

            // 2. Define Indices
            uint[] indices = {
                0, 1, 2
            };

            // 3. Create Shader
            _shader = new Shader("Assets/Shaders/basic.vert", "Assets/Shaders/basic.frag");

            // 4. Create Buffers
            _vertexBuffer = VertexBuffer.CreateWithData(vertices);
            _indexBuffer = new IndexBuffer(indices);

            // 5. Create Vertex Array and configure layout
            _vertexArray = new VertexArray();
            var layout = new VertexBufferLayout();
            layout.AddElement(0, 3, VertexAttribPointerType.Float, false); // layout(location = 0) = vec3 position
            
            _vertexArray.AddVertexBuffer(_vertexBuffer, layout);
            _vertexArray.SetIndexBuffer(_indexBuffer);
            
            // Unbind VAO after setup (good practice)
            _vertexArray.Unbind(); 
            _vertexBuffer.Unbind();
            _indexBuffer.Unbind();
            
            Log.Info("Triangle geometry setup complete.");
            // --- End Triangle Setup ---

            // Example: Renderer subscribing to resize events
            EventManager.Subscribe<WindowResizeEvent>(OnWindowResize); 

            Log.Info("Core systems initialized.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Exception during core system initialization or resource setup");
            // Cleanup partially created resources
            Shutdown(); // Call full shutdown to dispose anything created so far
            _window = null; // Ensure window is null so Run() exits
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
        Renderer.Clear(); // Clear the screen
        
        // Draw the triangle if resources are valid
        if (_shader != null && _vertexArray != null && _indexBuffer != null)
        {
            _shader.Use();
            _vertexArray.Bind();
            
            GL.DrawElements(PrimitiveType.Triangles, _indexBuffer.Count, DrawElementsType.UnsignedInt, 0);
            
            _vertexArray.Unbind(); // Unbind VAO after drawing
        }
    }

    private void Shutdown()
    { 
        Log.Info("Shutting down subsystems and disposing resources...");
        
        // Dispose rendering resources first (reverse order of creation is often safe)
        _indexBuffer?.Dispose();
        _vertexBuffer?.Dispose();
        _vertexArray?.Dispose(); // VAO doesn't own buffers, dispose it after
        _shader?.Dispose();
        Log.Info("Rendering resources disposed.");

        // Dispose window 
        _window?.Dispose();
        Log.Info("Window disposed.");
        
        // Shutdown logger last
        LogManager.Shutdown();
    }

    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }
}