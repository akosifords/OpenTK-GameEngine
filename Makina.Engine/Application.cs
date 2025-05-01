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
using Makina.Engine.Debugging; // Added
using ImGuiNET; // Added
using Makina.Engine.Scene.Components; // <<< Added
using System.Linq;                  // <<< Added for LINQ in Shutdown

namespace Makina.Engine;

public class Application : IDisposable
{
    private Window? _window;
    private PerspectiveCamera? _camera;
    private ImGuiController? _imGuiController; // Added ImGui Controller
    
    // Scene Management (very basic)
    private List<GameObject> _gameObjects = new List<GameObject>();

    // Timing
    private readonly Stopwatch _timer = new Stopwatch();
    private float _lastFrameTime = 0.0f;
    private float _fps = 0.0f; // Added FPS counter

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
        if (_window == null || _camera == null || _imGuiController == null)
        {
            Log.Error("Window, Camera or ImGuiController failed to initialize.");
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
            _fps = 1.0f / deltaTime; // Simple FPS calculation

            // 1. Process native window events
            _window.ProcessEvents(); 
            
            // 2. Update ImGui Controller (Input, New Frame)
            _imGuiController.Update(_window.GetNativeWindow(), deltaTime); // Pass native window
            
            // 3. Dispatch queued engine events
            EventManager.DispatchQueuedEvents();

            // 4. Update application logic (incl. ImGui UI building)
            Update(deltaTime);
            
            // 5. Reset per-frame input state (and calculate mouse delta)
            InputManager.FrameReset();

            // 6. Render the scene
            Render();
            
            // 7. Render ImGui UI
            _imGuiController.Render();

            // 8. Swap buffers
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
            
            _window.CursorState = CursorState.Grabbed;
            Log.Info("Cursor state set to Grabbed.");
            
            float aspectRatio = (float)_window.Size.X / _window.Size.Y;
            _camera = new PerspectiveCamera(new Vector3(0.0f, 0.0f, 3.0f), aspectRatio);
            Log.Info("Camera created.");
            
            Renderer.Init();
            Log.Info("Renderer initialized.");

            _imGuiController = new ImGuiController(_window.Size.X, _window.Size.Y);
            Log.Info("ImGui Controller initialized.");

            // --- Create Game Objects with Components --- 
            Log.Info("Creating game objects and components...");

            // 1. Load shared resources
            var shader = new Makina.Engine.Rendering.Shader("Assets/Shaders/basic.vert", "Assets/Shaders/basic.frag");
            var texture = new Texture("Assets/Textures/container.png");
            // Updated vertices with normals (Pos(3) + Color(3) + TexCoord(2) + Normal(3) = 11 floats per vertex)
            float[] vertices = {
                 // Positions          // Colors (unused)    // TexCoords  // Normals
                 0.0f,  0.5f, 0.0f,   1.0f, 0.0f, 0.0f,   0.5f, 1.0f,   0.0f, 0.0f, 1.0f, // Top vertex
                -0.5f, -0.5f, 0.0f,   0.0f, 1.0f, 0.0f,   0.0f, 0.0f,   0.0f, 0.0f, 1.0f, // Bottom left vertex
                 0.5f, -0.5f, 0.0f,   0.0f, 0.0f, 1.0f,   1.0f, 0.0f,   0.0f, 0.0f, 1.0f  // Bottom right vertex
            };
            uint[] indices = { 0, 1, 2 };
            var layout = new VertexBufferLayout();
            layout.AddElement(0, 3, VertexAttribPointerType.Float, false); // Position (location 0)
            layout.AddElement(1, 3, VertexAttribPointerType.Float, false); // Color (location 1 - unused)
            layout.AddElement(2, 2, VertexAttribPointerType.Float, false); // TexCoord (location 2)
            layout.AddElement(3, 3, VertexAttribPointerType.Float, false); // Normal (location 3)
            var mesh = new Mesh(vertices, indices, layout);

            // 2. Create First GameObject (Rotating)
            var triangleObject1 = new GameObject("RotatingTriangle");
            // Add MeshRenderer component and assign resources
            var meshRenderer1 = new MeshRenderer { Mesh = mesh, Texture = texture, Shader = shader };
            triangleObject1.AddComponent(meshRenderer1);
            _gameObjects.Add(triangleObject1);
            Log.Info("First game object created with MeshRenderer.");
            
            // 3. Create Second GameObject (Static Offset)
            var triangleObject2 = new GameObject("StaticTriangle");
            // Add MeshRenderer component, reusing the same resources
            var meshRenderer2 = new MeshRenderer { Mesh = mesh, Texture = texture, Shader = shader };
            triangleObject2.AddComponent(meshRenderer2);
            // Modify Transform (already exists on GameObject)
            triangleObject2.Transform.Position = new Vector3(1.5f, 0.0f, 0.0f); 
            triangleObject2.Transform.Scale = new Vector3(0.75f); 
            _gameObjects.Add(triangleObject2);
            Log.Info("Second game object created with MeshRenderer at offset.");

            // 4. Create Directional Light GameObject
            var lightObject = new GameObject("DirectionalLightSource");
            var lightComponent = new DirectionalLight 
            {
                Color = new Vector3(1.0f, 1.0f, 1.0f), // White light
                Intensity = 1.0f
            };
            lightObject.AddComponent(lightComponent);
            // Set rotation so its Forward vector matches our desired light direction (0.5, -1.0, -0.5) normalized
            // This requires figuring out the Euler angles or Quaternion for that direction. 
            // Let's approximate with Euler angles for simplicity. Pointing down-right-ish.
            lightObject.Transform.EulerAngles = new Vector3(45.0f, -30.0f, 0.0f); 
            _gameObjects.Add(lightObject);
            Log.Info("Directional light game object created.");
            
            // --- End Game Object Setup ---

            // Subscribe AFTER all essential systems are created
            EventManager.Subscribe<WindowResizeEvent>(OnWindowResize);
            if (_window != null)
            {
                 _window.TextInput += OnTextInput; // Subscribe to C# event from Window
            }
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
            _imGuiController = null; // Null out controller
        }
    }

    private void SubscribeToEvents()
    {
        EventManager.Subscribe<WindowCloseEvent>(OnWindowClose); 
        // EventManager.Subscribe<WindowResizeEvent>(OnWindowResize); // Now handled in Initialize
        Log.Trace("Application subscribed to events.");
    }
    
    private void UnsubscribeFromEvents()
    {
        EventManager.Unsubscribe<WindowCloseEvent>(OnWindowClose);
        // EventManager.Unsubscribe<WindowResizeEvent>(OnWindowResize); // Now handled in Initialize
        if (_window != null) 
        {
            _window.TextInput -= OnTextInput; // Unsubscribe from C# event
        }
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
        if (_imGuiController != null) 
        {
             _imGuiController.WindowResized(e.Width, e.Height);
        }
    }

    // Handler for the C# TextInput event from Window
    private void OnTextInput(TextInputEventArgs args)
    {
        _imGuiController?.PressChar((char)args.Unicode);
    }

    private bool ShouldRun()
    { 
        // Loop condition: check if window is closing
        return _window != null && !_window.IsClosing;
    }

    private void Update(float deltaTime)
    { 
        // --- Build ImGui UI --- 
        BuildDebugUI();
        
        // Only process camera/game input if ImGui doesn't want capture
        ImGuiIOPtr io = ImGui.GetIO();
        if (!io.WantCaptureKeyboard || !io.WantCaptureMouse)
        {
             if (_camera != null && _window != null)
             {
                  // Camera Controls 
                  if (InputManager.IsKeyDown(Keys.W)) _camera.ProcessKeyboard(Keys.W, deltaTime);
                  if (InputManager.IsKeyDown(Keys.S)) _camera.ProcessKeyboard(Keys.S, deltaTime);
                  if (InputManager.IsKeyDown(Keys.A)) _camera.ProcessKeyboard(Keys.A, deltaTime);
                  if (InputManager.IsKeyDown(Keys.D)) _camera.ProcessKeyboard(Keys.D, deltaTime);

                  // Mouse Look (only if cursor is grabbed)
                  if (_window.CursorState == CursorState.Grabbed)
                  {
                       Vector2 mouseDelta = InputManager.GetMousePositionDelta();
                       if (mouseDelta.LengthSquared > 0.0001f) 
                           _camera.ProcessMouseMovement(mouseDelta.X, mouseDelta.Y);
                  }
             }
        }
        
        // --- Update GameObjects ---
        // Example: Rotate the first game object
        if (_gameObjects.Count > 0)
        {
            var triangleObject = _gameObjects[0];
            float angle = (float)_timer.Elapsed.TotalSeconds * 30.0f; // degrees per second
            triangleObject.Transform.EulerAngles = new Vector3(0, angle, 0); // Use EulerAngles setter
        }
        
        // --- Input Handling Example ---
        if (InputManager.IsKeyPressed(Keys.Escape))
        {
             _window.CursorState = _window.CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
             Log.Info($"Toggled cursor state to: {_window.CursorState}");
        }
    }

    private void BuildDebugUI()
    {
        // Potentially use ImGui Docking space here
        // ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());
        
        ImGui.Begin("Debug Info"); // Create a window
        
        ImGui.Text($"FPS: {_fps:F1}");
        ImGui.Separator();
        
        if (ImGui.CollapsingHeader("GameObjects"))
        {
            foreach (var go in _gameObjects)
            {
                if (ImGui.TreeNodeEx($"{go.Name}##{go.GetHashCode()}", ImGuiTreeNodeFlags.DefaultOpen)) // Use TreeNodeEx for better control and unique ID
                {
                    ImGui.TextDisabled($" Active: {go.IsActive}"); // Show active state
                    ImGui.Separator();
                    ImGui.Text("Components:");
                    ImGui.Indent(); // Indent component list
                    foreach (var component in go.GetAllComponents())
                    {
                        ImGui.Text($"- {component.GetType().Name}");
                        // Optional: Add specific component details here later
                        // if (component is MeshRenderer mr) { ... }

                        // Display Transform details
                        if (component is Transform transform)
                        {
                            ImGui.Indent();
                            // Use a smaller font or tighter spacing if needed
                            // ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(4, 1)); 
                            ImGui.Text($"  Pos: {transform.Position}");
                            ImGui.Text($"  Rot: {transform.EulerAngles}"); // Display Euler angles
                            ImGui.Text($"  Scl: {transform.Scale}");
                            // ImGui.PopStyleVar();
                            ImGui.Unindent();
                        }
                    }
                    ImGui.Unindent(); // Unindent component list
                    ImGui.TreePop();
                }
            }
        }
        
        // Add more debug sections as needed (Camera info, Renderer stats, etc.)

        ImGui.End(); // End the window
        
        // Example: Show ImGui Demo Window
        // ImGui.ShowDemoWindow(); 
    }

    private void Render()
    { 
        Renderer.Clear(); 
        
        if (_camera == null) return;

        // --- Find Directional Light in Scene ---
        DirectionalLight? sceneLight = null;
        Transform? lightTransform = null;
        foreach (var go in _gameObjects)
        {
            if (go.TryGetComponent<DirectionalLight>(out sceneLight))
            {
                lightTransform = go.Transform;
                break; // Found the first light, stop searching
            }
        }

        // --- Set Default Light if None Found (Optional Fallback) ---
        Vector3 currentLightDir = new Vector3(0, 0, -1); // Default towards -Z
        Vector3 currentLightColor = Vector3.Zero; // Default off
        bool lightWarningLogged = false; // Flag to log warning only once
        if (sceneLight != null && lightTransform != null)
        {
            // Direction *towards* light is negative of the transform's forward vector
            currentLightDir = -lightTransform.Forward; 
            currentLightColor = sceneLight.EffectiveColor;
        }
        else
        {
            if (!lightWarningLogged)
            {
                Log.Warn("RenderLoop: No DirectionalLight found in scene. Using default.");
                lightWarningLogged = true;
            }
        }

        // --- Get Camera Position --- 
        Vector3 cameraPosition = _camera.Position;
        // Remove hardcoded object color, maybe make it a component property later
        Vector3 objectBaseColor = new Vector3(1.0f, 0.5f, 0.31f); 

        // Loop through GameObjects and render them
        foreach (var gameObject in _gameObjects)
        {
            // Skip rendering the light source itself if it doesn't have a MeshRenderer
            if (!gameObject.TryGetComponent<MeshRenderer>(out _)) continue; // Use TryGetComponent instead of HasComponent
            
            if (!gameObject.IsActive) continue; // Skip inactive GameObjects

            // TryGetComponent is slightly more efficient if component might be missing
            if (gameObject.TryGetComponent<MeshRenderer>(out var renderer) && 
                renderer.Shader != null && renderer.Mesh != null) 
            {
                renderer.Shader.Use();
                
                // Bind Texture (Still useful if shader uses it, e.g., for modulation)
                if (renderer.Texture != null) 
                {
                    renderer.Texture.Bind(TextureUnit.Texture0);
                    renderer.Shader.SetUniformInt("uTexture", 0); 
                }
                
                // Set Transformation Uniforms (using GameObject's Transform)
                renderer.Shader.SetUniformMat4("uModel", gameObject.Transform.GetLocalMatrix());
                renderer.Shader.SetUniformMat4("uView", _camera.ViewMatrix);
                renderer.Shader.SetUniformMat4("uProjection", _camera.ProjectionMatrix);
                
                // --- Set Lighting Uniforms ---
                renderer.Shader.SetUniformVec3("objectColor", objectBaseColor); // Still hardcoded base color
                renderer.Shader.SetUniformVec3("lightColor", currentLightColor); // Use color from scene light
                renderer.Shader.SetUniformVec3("lightDir", currentLightDir);     // Use direction from scene light
                renderer.Shader.SetUniformVec3("viewPos", cameraPosition);
                
                // Draw Mesh
                renderer.Mesh.Bind(); 
                GL.DrawElements(PrimitiveType.Triangles, renderer.Mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                renderer.Mesh.Unbind();
            }
        }
    }

    private void Shutdown()
    {
        Log.Info("Shutting down subsystems and disposing resources...");
        
        _imGuiController?.Dispose();
        
        // Dispose unique resources used by MeshRenderers
        var uniqueMeshes = new HashSet<Mesh>();
        var uniqueTextures = new HashSet<Texture>();
        var uniqueShaders = new HashSet<Rendering.Shader>();

        foreach (var go in _gameObjects)
        { 
            if(go.TryGetComponent<MeshRenderer>(out var renderer))
            {
                if (renderer.Mesh != null) uniqueMeshes.Add(renderer.Mesh);
                if (renderer.Texture != null) uniqueTextures.Add(renderer.Texture);
                if (renderer.Shader != null) uniqueShaders.Add(renderer.Shader);
            }
        }
        
        Log.Info($"Disposing {uniqueMeshes.Count} unique Meshes...");
        foreach (var mesh in uniqueMeshes) mesh.Dispose();
        
        Log.Info($"Disposing {uniqueTextures.Count} unique Textures...");
        foreach (var texture in uniqueTextures) texture.Dispose();
        
        Log.Info($"Disposing {uniqueShaders.Count} unique Shaders...");
        foreach (var shader in uniqueShaders) shader.Dispose();

        _gameObjects.Clear();
        Log.Info("Game objects cleared.");

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