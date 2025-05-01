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
using System.Diagnostics; // Added for Stopwatch
using OpenTK.Windowing.Common; // Added for CursorState enum

namespace Makina.Engine;

public class Application : IDisposable
{
    private Window? _window;
    private PerspectiveCamera? _camera;
    
    // Triangle Rendering Resources
    private Shader? _shader;
    private VertexArray? _vertexArray;
    private VertexBuffer? _vertexBuffer;
    private IndexBuffer? _indexBuffer;

    // Timing
    private readonly Stopwatch _timer = new Stopwatch();
    private float _lastFrameTime = 0.0f;

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
        if (_window == null || _camera == null)
        {
            Log.Error("Window or Camera failed to initialize.");
            return;
        }
        
        // Subscribe to events AFTER subsystems are initialized
        SubscribeToEvents();

        // Start timer before main loop
        _timer.Start();
        _lastFrameTime = (float)_timer.Elapsed.TotalSeconds;

        Log.Info("Entering main loop...");
        while (ShouldRun())
        {
            // Calculate delta time
            float currentTime = (float)_timer.Elapsed.TotalSeconds;
            float deltaTime = currentTime - _lastFrameTime;
            _lastFrameTime = currentTime;
            // Log.Trace($"DeltaTime: {deltaTime * 1000:F2}ms"); // Can be noisy

            // 1. Process native window events
            _window.ProcessEvents(); 
            
            // 2. Dispatch queued engine events
            EventManager.DispatchQueuedEvents();

            // 3. Update application logic
            Update(deltaTime);
            
            // 4. Render the scene
            Render();
            
            // 5. Reset per-frame input state (and calculate mouse delta)
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
            
            // Capture mouse cursor for FPS controls
            _window.CursorState = CursorState.Grabbed;
            Log.Info("Cursor state set to Grabbed.");
            
            float aspectRatio = (float)_window.Size.X / _window.Size.Y;
            _camera = new PerspectiveCamera(new Vector3(0.0f, 0.0f, 3.0f), aspectRatio);
            Log.Info("Camera created.");
            
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
            _camera = null; // Ensure camera is also nulled on error
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
        if (e.Width > 0 && e.Height > 0 && _camera != null)
        {
            _camera.AspectRatio = (float)e.Width / e.Height;
        }
        // Viewport is handled in Window.cs
    }

    private bool ShouldRun()
    { 
        // Loop condition: check if window is closing
        return _window != null && !_window.IsClosing;
    }

    private void Update(float deltaTime)
    { 
        if (_camera == null || _window == null) return;

        // --- Camera Keyboard Movement ---
        if (InputManager.IsKeyDown(Keys.W)) _camera.ProcessKeyboard(Keys.W, deltaTime);
        if (InputManager.IsKeyDown(Keys.S)) _camera.ProcessKeyboard(Keys.S, deltaTime);
        if (InputManager.IsKeyDown(Keys.A)) _camera.ProcessKeyboard(Keys.A, deltaTime);
        if (InputManager.IsKeyDown(Keys.D)) _camera.ProcessKeyboard(Keys.D, deltaTime);
        // Optional Up/Down
        // if (InputManager.IsKeyDown(Keys.Space)) _camera.ProcessKeyboard(Keys.Space, deltaTime);
        // if (InputManager.IsKeyDown(Keys.LeftShift)) _camera.ProcessKeyboard(Keys.LeftShift, deltaTime);

        // --- Camera Mouse Look ---
        Vector2 mouseDelta = InputManager.GetMousePositionDelta();
        // Only process if there was actual movement (avoids small drift when not moving)
        if (mouseDelta.LengthSquared > 0.0001f) 
        {
             _camera.ProcessMouseMovement(mouseDelta.X, mouseDelta.Y);
        }

        // --- Input Handling Example ---
        if (InputManager.IsKeyPressed(Keys.Escape))
        {
            // Toggle cursor grab instead of closing immediately
             _window.CursorState = _window.CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
             Log.Info($"Toggled cursor state to: {_window.CursorState}");
             // EventManager.Publish(new WindowCloseEvent()); // Don't close on Escape now
        }
        
        // You can also check for continuous key hold:
        //if (InputManager.IsKeyDown(Keys.W)) { Log.Debug("W key is held down"); }
        
        // Check mouse position
        //Log.Trace($"Mouse Position: {InputManager.GetMousePosition()}");
    }

    private void Render()
    { 
        Renderer.Clear(); // Clear the screen
        
        if (_shader != null && _vertexArray != null && _indexBuffer != null && _camera != null)
        {
            _shader.Use();

            // Set Uniforms
            Matrix4 model = Matrix4.Identity; // No model transformation yet
            _shader.SetUniformMat4("uModel", model);
            _shader.SetUniformMat4("uView", _camera.ViewMatrix);
            _shader.SetUniformMat4("uProjection", _camera.ProjectionMatrix);
            
            // Draw
            _vertexArray.Bind();
            GL.DrawElements(PrimitiveType.Triangles, _indexBuffer.Count, DrawElementsType.UnsignedInt, 0);
            _vertexArray.Unbind();
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