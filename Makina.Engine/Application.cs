using Makina.Engine.Core.Logging;
using NLog;
using Makina.Engine.Rendering;
using Makina.Engine.Core.Events;
using Makina.Engine.Input;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics; // Added for Stopwatch
using OpenTK.Windowing.Common; // Added for CursorState enum
using Makina.Engine.Core.Math; // Added
using Makina.Engine.Rendering.Buffers; // Keep for VertexBufferLayout
using Makina.Engine.Scene; // Added
using System.Collections.Generic; // Added for List

namespace Makina.Engine;

public class Application : IDisposable
{
    private Window? _window;
    private PerspectiveCamera? _camera;
    
    // Scene Management (very basic)
    private List<GameObject> _gameObjects = new List<GameObject>();

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
        
        // Ensure window, camera, and rendering resources were created
        // Check _triangleMesh for now, since we are using Mesh now
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

            // --- Create Game Object --- 
            Log.Info("Creating game objects...");

            // 1. Load shared resources
            var shader = new Shader("Assets/Shaders/basic.vert", "Assets/Shaders/basic.frag");
            var texture = new Texture("Assets/Textures/container.png");
            float[] vertices = { 0.0f,  0.5f, 0.0f,  1.0f, 0.0f, 0.0f,  0.5f, 1.0f, -0.5f, -0.5f, 0.0f,  0.0f, 1.0f, 0.0f,  0.0f, 0.0f, 0.5f, -0.5f, 0.0f,  0.0f, 0.0f, 1.0f,  1.0f, 0.0f };
            uint[] indices = { 0, 1, 2 };
            var layout = new VertexBufferLayout();
            layout.AddElement(0, 3, VertexAttribPointerType.Float, false); // Position
            layout.AddElement(1, 3, VertexAttribPointerType.Float, false); // Color (unused by shader)
            layout.AddElement(2, 2, VertexAttribPointerType.Float, false); // TexCoord
            var mesh = new Mesh(vertices, indices, layout);

            // 2. Create First GameObject (Rotating)
            var triangleObject1 = new GameObject("RotatingTriangle");
            triangleObject1.Mesh = mesh;
            triangleObject1.Texture = texture;
            triangleObject1.Shader = shader;
            _gameObjects.Add(triangleObject1);
            Log.Info("First game object created.");
            
            // 3. Create Second GameObject (Static Offset)
            var triangleObject2 = new GameObject("StaticTriangle");
            triangleObject2.Mesh = mesh;     // Reuse same mesh
            triangleObject2.Texture = texture; // Reuse same texture
            triangleObject2.Shader = shader;  // Reuse same shader
            triangleObject2.Transform.Position = new Vector3(1.5f, 0.0f, 0.0f); // Offset to the right
            triangleObject2.Transform.Scale = new Vector3(0.75f); // Make it slightly smaller
            _gameObjects.Add(triangleObject2);
            Log.Info("Second game object created at offset.");
            
            // --- End Game Object Setup ---

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
            _gameObjects.Clear(); // Clear potentially partially created objects
        }
    }

    private void SubscribeToEvents()
    {
        // Example: Application subscribing to the window close event
        EventManager.Subscribe<WindowCloseEvent>(OnWindowClose); 
        EventManager.Subscribe<WindowResizeEvent>(OnWindowResize);
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
        
        // --- Update GameObjects ---
        // Example: Rotate the first game object
        if (_gameObjects.Count > 0)
        {
            var triangleObject = _gameObjects[0];
            float angle = (float)_timer.Elapsed.TotalSeconds * 30.0f; // degrees per second
            triangleObject.Transform.EulerAngles = new Vector3(0, angle, 0); // Use EulerAngles setter
        }
        
        // --- Camera Controls ---
        if (InputManager.IsKeyDown(Keys.W)) _camera.ProcessKeyboard(Keys.W, deltaTime);
        if (InputManager.IsKeyDown(Keys.S)) _camera.ProcessKeyboard(Keys.S, deltaTime);
        if (InputManager.IsKeyDown(Keys.A)) _camera.ProcessKeyboard(Keys.A, deltaTime);
        if (InputManager.IsKeyDown(Keys.D)) _camera.ProcessKeyboard(Keys.D, deltaTime);
        Vector2 mouseDelta = InputManager.GetMousePositionDelta();
        if (mouseDelta.LengthSquared > 0.0001f) 
        {
             _camera.ProcessMouseMovement(mouseDelta.X, mouseDelta.Y);
        }
        if (InputManager.IsKeyPressed(Keys.Escape))
        {
             _window.CursorState = _window.CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
             Log.Info($"Toggled cursor state to: {_window.CursorState}");
        }
    }

    private void Render()
    { 
        Renderer.Clear(); 
        
        if (_camera == null) return;

        // Loop through GameObjects and render them
        foreach (var gameObject in _gameObjects)
        {
            // Temporary direct component access
            if (gameObject.Shader != null && gameObject.Mesh != null && gameObject.Texture != null)
            {
                gameObject.Shader.Use();
                
                // Bind Texture
                gameObject.Texture.Bind(TextureUnit.Texture0);
                gameObject.Shader.SetUniformInt("uTexture", 0); 
                
                // Set Uniforms (using GameObject's Transform)
                gameObject.Shader.SetUniformMat4("uModel", gameObject.Transform.GetLocalMatrix());
                gameObject.Shader.SetUniformMat4("uView", _camera.ViewMatrix);
                gameObject.Shader.SetUniformMat4("uProjection", _camera.ProjectionMatrix);
                
                // Draw Mesh
                gameObject.Mesh.Bind(); 
                GL.DrawElements(PrimitiveType.Triangles, gameObject.Mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                gameObject.Mesh.Unbind();
            }
        }
    }

    private void Shutdown()
    { 
        Log.Info("Shutting down subsystems and disposing resources...");
        
        // Dispose resources held by GameObjects (assuming they are owned here for now)
        // In a real scenario, resource management would be more sophisticated.
        foreach (var go in _gameObjects)
        {
            go.Mesh?.Dispose();
            go.Texture?.Dispose();
            go.Shader?.Dispose();
        }
        _gameObjects.Clear();
        Log.Info("Game object resources disposed.");

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