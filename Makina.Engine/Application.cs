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
    private GameObject? _selectedGameObject = null; // <<< Added: Currently selected object for inspector

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

            // Create a shared Material instance
            var basicMaterial = new Material(shader, texture)
            {
                 Color = new Vector3(1.0f, 0.5f, 0.31f) // Set base color here (coral)
                 // Shininess is default 32.0f
            };

            // 2. Create First GameObject (Rotating)
            var triangleObject1 = new GameObject("RotatingTriangle");
            // Add MeshRenderer component and assign the material
            var meshRenderer1 = new MeshRenderer { Mesh = mesh, Material = basicMaterial }; // <<< Use Material
            triangleObject1.AddComponent(meshRenderer1);
            _gameObjects.Add(triangleObject1);
            Log.Info("First game object created with MeshRenderer.");
            
            // 3. Create Second GameObject (Static Offset, Child of First)
            var triangleObject2 = new GameObject("ChildTriangle"); // Renamed for clarity
            // Add MeshRenderer component, reusing the same material
            var meshRenderer2 = new MeshRenderer { Mesh = mesh, Material = basicMaterial }; // <<< Use Material
            triangleObject2.AddComponent(meshRenderer2);
            // Set local transform relative to parent
            triangleObject2.Transform.LocalPosition = new Vector3(1.5f, 0.0f, 0.0f); // Offset from parent
            triangleObject2.Transform.LocalScale = new Vector3(0.5f); // Smaller than parent
            // --- Set Parent --- 
            triangleObject2.Transform.SetParent(triangleObject1.Transform); // Make it a child
            _gameObjects.Add(triangleObject2);
            Log.Info("Second game object created as child of the first.");

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
            lightObject.Transform.LocalEulerAngles = new Vector3(45.0f, -30.0f, 0.0f); 
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
        // Example: Rotate the first game object (which is now the parent)
        if (_gameObjects.Count > 0)
        {
            // Find the parent triangle (assuming it's the first one for this example)
            var parentTriangle = _gameObjects.FirstOrDefault(go => go.Name == "RotatingTriangle"); 
            if (parentTriangle != null)
            {
                float angle = (float)_timer.Elapsed.TotalSeconds * 30.0f; // degrees per second
                parentTriangle.Transform.LocalEulerAngles = new Vector3(0, angle, 0); // Set local rotation
            }
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
        
        ImGui.Begin("Hierarchy"); // Renamed window for clarity
        
        ImGui.Text($"FPS: {_fps:F1}");
        ImGui.Separator();
        
        ImGui.Text("Scene Hierarchy:");
        ImGui.Separator();

        // Iterate through root GameObjects only (those with no parent)
        foreach (var go in _gameObjects.Where(g => g.Transform.Parent == null))
        {
             DrawGameObjectNode(go);
        }
        
        ImGui.End(); // End the Hierarchy window
        
        // --- Inspector Panel ---
        BuildInspectorPanel(); // Call the new inspector panel method
        
        // Example: Show ImGui Demo Window
        // ImGui.ShowDemoWindow(); 
    }

    /// <summary>
    /// Recursively draws a GameObject node and its children in the ImGui hierarchy.
    /// Handles selection.
    /// </summary>
    private void DrawGameObjectNode(GameObject go)
    {
        if (go == null) return;

        // Node Flags: DefaultOpen, OpenOnArrow, Selectable
        ImGuiTreeNodeFlags nodeFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (go.Transform.Children.Count == 0) 
        {
             nodeFlags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen; // No children
        }
        // Highlight selected node
        if (go == _selectedGameObject)
        {
             nodeFlags |= ImGuiTreeNodeFlags.Selected;
        }

        // Unique ID for the node
        string nodeLabel = $"{go.Name}##{go.GetHashCode()}";
        
        // Draw the node
        bool nodeOpen = ImGui.TreeNodeEx(nodeLabel, nodeFlags);

        // Handle selection
        if (ImGui.IsItemClicked())
        {
            _selectedGameObject = go;
            Log.Trace($"Selected GameObject: {go.Name}");
        }

        // If node is open and not a leaf, draw children and pop the tree node
        if (nodeOpen && (nodeFlags & ImGuiTreeNodeFlags.Leaf) == 0)
        {
            foreach (var child in go.Transform.Children)
            {
                 DrawGameObjectNode(child.GameObject!); // Recurse for children
            }
            ImGui.TreePop();
        }
        // Note: For leaf nodes (NoTreePushOnOpen), TreePop is not needed.
    }

    /// <summary>
    /// Builds the Inspector panel, showing details of the _selectedGameObject.
    /// </summary>
    private void BuildInspectorPanel()
    {
         ImGui.Begin("Inspector");
         
         if (_selectedGameObject == null)
         {
              ImGui.Text("Select a GameObject to inspect.");
         }
         else
         {
             ImGui.Text($"Inspecting: {_selectedGameObject.Name}");
             ImGui.Separator();
             
             // Display IsActive checkbox
             bool isActive = _selectedGameObject.IsActive;
             if (ImGui.Checkbox("Is Active", ref isActive))
             {
                 _selectedGameObject.IsActive = isActive;
             }
             ImGui.Separator();
             
             // Display components and their properties
             ImGui.Text("Components:");
             foreach (var component in _selectedGameObject.GetAllComponents())
             {
                  string componentName = component.GetType().Name;
                  if (ImGui.CollapsingHeader($"{componentName}##{component.GetHashCode()}", ImGuiTreeNodeFlags.DefaultOpen))
                  {
                       // --- Display Transform Component Details (Editable) ---
                       if (component is Transform transform)
                       {
                            // Use DragFloat3 for editable Vector3 fields
                            System.Numerics.Vector3 localPos = ToSystemVec3(transform.LocalPosition);
                            if (ImGui.DragFloat3("Local Position", ref localPos, 0.1f))
                            {
                                transform.LocalPosition = ToOpenTKVec3(localPos);
                            }

                            System.Numerics.Vector3 localEuler = ToSystemVec3(transform.LocalEulerAngles);
                            if (ImGui.DragFloat3("Local Rotation", ref localEuler, 1.0f))
                            {
                                transform.LocalEulerAngles = ToOpenTKVec3(localEuler);
                            }

                            System.Numerics.Vector3 localScl = ToSystemVec3(transform.LocalScale);
                            if (ImGui.DragFloat3("Local Scale", ref localScl, 0.05f))
                            {
                                // Prevent zero or negative scale if needed
                                localScl.X = Math.Max(localScl.X, 0.001f);
                                localScl.Y = Math.Max(localScl.Y, 0.001f);
                                localScl.Z = Math.Max(localScl.Z, 0.001f);
                                transform.LocalScale = ToOpenTKVec3(localScl);
                            }
                            
                            ImGui.Separator();
                            // Display read-only world info
                            ImGui.TextDisabled($"World Pos: {transform.Position:F2}");
                            ImGui.TextDisabled($"World Rot: {transform.EulerAngles:F1}");
                            ImGui.TextDisabled($"World Scale: {transform.LossyScale:F2}");
                       }
                       // --- Display Directional Light Component Details (Editable) ---
                       else if (component is DirectionalLight light)
                       {
                            System.Numerics.Vector3 color = ToSystemVec3(light.Color);
                            if (ImGui.ColorEdit3("Color", ref color))
                            {
                                light.Color = ToOpenTKVec3(color);
                            }
                            
                            float intensity = light.Intensity;
                            if (ImGui.DragFloat("Intensity", ref intensity, 0.05f, 0.0f, 100.0f)) // Min 0, Max 100
                            {
                                 light.Intensity = intensity;
                            }
                       }
                       // --- Display MeshRenderer Details (Read-only for now) ---
                       else if (component is MeshRenderer renderer)
                       {
                            // Get details from the Material
                            string meshName = renderer.Mesh?.GetHashCode().ToString() ?? "None"; // Placeholder ID
                            string textureName = renderer.Material?.Texture?.Handle.ToString() ?? "None";
                            string shaderName = renderer.Material?.Shader?.Handle.ToString() ?? "None";
                            ImGui.TextDisabled($"Mesh: {meshName}");
                            ImGui.TextDisabled($"Texture: {textureName}");
                            ImGui.TextDisabled($"Shader: {shaderName}");
                            // Add Material properties display
                            if (renderer.Material != null)
                            {
                                ImGui.Separator();
                                System.Numerics.Vector3 matColor = ToSystemVec3(renderer.Material.Color);
                                if (ImGui.ColorEdit3("Material Color", ref matColor))
                                {
                                    renderer.Material.Color = ToOpenTKVec3(matColor);
                                }
                                float shininess = renderer.Material.Shininess;
                                if (ImGui.DragFloat("Material Shininess", ref shininess, 0.5f, 1.0f, 256.0f))
                                {
                                     renderer.Material.Shininess = shininess;
                                }
                            }
                       }
                       // Add more component types here...
                       else 
                       {
                            ImGui.TextDisabled("(No editable properties)");
                       }
                  }
             }
         }
         
         ImGui.End(); // End the Inspector window
    }

    // Helper methods to convert between OpenTK and System.Numerics vectors
    private static System.Numerics.Vector3 ToSystemVec3(OpenTK.Mathematics.Vector3 v)
    {
        return new System.Numerics.Vector3(v.X, v.Y, v.Z);
    }

    private static OpenTK.Mathematics.Vector3 ToOpenTKVec3(System.Numerics.Vector3 v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
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
        // Removed hardcoded object color, now comes from Material
        // Vector3 objectBaseColor = new Vector3(1.0f, 0.5f, 0.31f); 

        // Loop through GameObjects and render them
        foreach (var gameObject in _gameObjects)
        {
            // Skip rendering if it doesn't have a MeshRenderer
            if (!gameObject.TryGetComponent<MeshRenderer>(out var renderer)) continue;
            
            if (!gameObject.IsActive) continue; // Skip inactive GameObjects

            // Check if MeshRenderer has valid Mesh and Material with Shader
            if (renderer.Mesh != null && renderer.Material != null && renderer.Material.Shader != null) 
            {
                Makina.Engine.Rendering.Shader shader = renderer.Material.Shader;
                Texture? texture = renderer.Material.Texture; // Can be null
                
                shader.Use();
                
                // Bind Texture if it exists
                if (texture != null) 
                {
                    texture.Bind(TextureUnit.Texture0);
                    shader.SetUniformInt("uTexture", 0); 
                }
                
                // Set Transformation Uniforms (using GameObject's Transform)
                shader.SetUniformMat4("uModel", gameObject.Transform.GetWorldMatrix());
                shader.SetUniformMat4("uView", _camera.ViewMatrix);
                shader.SetUniformMat4("uProjection", _camera.ProjectionMatrix);
                
                // --- Set Lighting & Material Uniforms ---
                shader.SetUniformVec3("objectColor", renderer.Material.Color); // Use color from Material
                shader.SetUniformFloat("shininess", renderer.Material.Shininess); // <<< Add Shininess uniform
                shader.SetUniformVec3("lightColor", currentLightColor); // Use color from scene light
                shader.SetUniformVec3("lightDir", currentLightDir);     // Use direction from scene light
                shader.SetUniformVec3("viewPos", cameraPosition);
                
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
        // <<< Get unique Textures and Shaders from Materials >>>
        var uniqueTextures = new HashSet<Texture>();
        var uniqueShaders = new HashSet<Rendering.Shader>();
        var uniqueMaterials = new HashSet<Material>(); // Keep track to avoid duplicate checks

        foreach (var go in _gameObjects)
        { 
            if(go.TryGetComponent<MeshRenderer>(out var renderer))
            {
                if (renderer.Mesh != null) uniqueMeshes.Add(renderer.Mesh);
                // Add Material components
                if (renderer.Material != null && uniqueMaterials.Add(renderer.Material))
                {
                     if (renderer.Material.Texture != null) uniqueTextures.Add(renderer.Material.Texture);
                     if (renderer.Material.Shader != null) uniqueShaders.Add(renderer.Material.Shader);
                     // Optionally dispose the material itself if it becomes disposable
                     // renderer.Material.Dispose(); 
                }
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