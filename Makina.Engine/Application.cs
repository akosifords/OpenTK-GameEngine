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
using Makina.Engine.Core;           // <<< Added for ResourceManager
using Makina.Engine.Physics; // Added
using BepuPhysics; // Added for BodyReference, StaticReference etc.
using BepuUtilities; // Added for RigidPose

namespace Makina.Engine;

public class Application : IDisposable
{
    // Made protected for derived classes
    protected Window? _window;
    protected PerspectiveCamera? _camera;
    protected ImGuiController? _imGuiController;
    protected PhysicsWorld? _physicsWorld; // Added
    protected List<GameObject> _gameObjects = new List<GameObject>();
    protected GameObject? _selectedGameObject = null;
    protected readonly Stopwatch _timer = new Stopwatch();
    
    private float _lastFrameTime = 0.0f;
    private float _fps = 0.0f;

    public Application()
    {
        Log.Info("Makina Engine Initializing...");
    }

    public void Run()
    {
        Initialize();
        
        if (_window == null || _camera == null || _imGuiController == null || _physicsWorld == null)
        { 
             Log.Error("One or more essential engine systems (Window, Camera, ImGuiController, PhysicsWorld) failed to initialize.");
             return;
        }
        
        SubscribeToEvents();

        _timer.Start();
        _lastFrameTime = (float)_timer.Elapsed.TotalSeconds;

        Log.Info("Entering main loop...");
        while (ShouldRun())
        {
            float currentTime = (float)_timer.Elapsed.TotalSeconds;
            float deltaTime = currentTime - _lastFrameTime;
            _lastFrameTime = currentTime;
            _fps = 1.0f / deltaTime; 

            _window.ProcessEvents(); 
            _imGuiController.Update(_window.GetNativeWindow(), deltaTime);
            EventManager.DispatchQueuedEvents();
            Update(deltaTime);
            InputManager.FrameReset();
            Render();
            _imGuiController.Render();
            _window.SwapBuffers();
        }
        Log.Info("Exited main loop.");
        
        UnsubscribeFromEvents();
        Shutdown();
    }

    protected virtual void Initialize()
    {
        Log.Info("Initializing base engine subsystems...");
        bool initializationOk = false;
        try
        { 
            _window = new Window(); 
            _window.CursorState = CursorState.Grabbed;
            
            float aspectRatio = (float)_window.Size.X / _window.Size.Y;
            _camera = new PerspectiveCamera(new Vector3(0.0f, 0.0f, 3.0f), aspectRatio);
            
            Renderer.Init();
            GL.Enable(EnableCap.FramebufferSrgb);
            _imGuiController = new ImGuiController(_window.Size.X, _window.Size.Y);
            _physicsWorld = new PhysicsWorld(); // Added initialization

            Log.Info("Base engine subsystems initialized.");
            
            // --- Load application-specific content ---
            Log.Info("Loading application content...");
            LoadContent();
            Log.Info("Application content loaded.");

            // Subscribe AFTER all essential systems are created
            EventManager.Subscribe<WindowResizeEvent>(OnWindowResize);
            if (_window != null)
            {
                 _window.TextInput += OnTextInput;
            }
            initializationOk = true;
        }
        catch (Exception ex)
        { 
            Log.Error(ex, "Exception during engine initialization or content loading.");
        }
        finally
        { 
             if (!initializationOk) 
             { 
                Log.Error("Engine initialization failed. Shutting down partially initialized systems.");
                // Attempt cleanup even if initialization failed halfway
                Shutdown(); 
                // Nullify references to prevent Run() continuing with bad state
                _window = null; 
                _camera = null; 
                _imGuiController = null;
                _physicsWorld = null; // Added
            }
        }
    }

    /// <summary>
    /// Called during Initialize. Override to load resources and set up the initial scene.
    /// </summary>
    protected virtual void LoadContent()
    { 
        // Base implementation does nothing. Derived classes should override.
        Log.Info("Base LoadContent called. No application-specific content loaded by default.");
    }

    private void SubscribeToEvents() // Keep private, internal engine detail
    {
        EventManager.Subscribe<WindowCloseEvent>(OnWindowClose);
        Log.Trace("Application subscribed to engine events.");
    }
    
    private void UnsubscribeFromEvents() // Keep private, internal engine detail
    {
        EventManager.Unsubscribe<WindowCloseEvent>(OnWindowClose);
        if (_window != null) 
        { 
            _window.TextInput -= OnTextInput;
        }
        Log.Trace("Application unsubscribed from engine events.");
    }

    // --- Event Handlers (Internal Engine Logic) ---

    private void OnWindowClose(WindowCloseEvent e)
    {
        Log.Info("WindowCloseEvent received. Preparing to exit.");
    }

    private void OnWindowResize(WindowResizeEvent e)
    {
        Log.Debug($"WindowResizeEvent: {e.Width}x{e.Height}");
        if (e.Width > 0 && e.Height > 0 && _camera != null)
        {
            _camera.AspectRatio = (float)e.Width / e.Height;
        }
        _imGuiController?.WindowResized(e.Width, e.Height);
    }

    private void OnTextInput(TextInputEventArgs args)
    {
        _imGuiController?.PressChar((char)args.Unicode);
    }

    protected virtual bool ShouldRun()
    { 
        return _window != null && !_window.IsClosing;
    }

    protected virtual void Update(float deltaTime)
    { 
        // --- Build Debug UI --- 
        BuildDebugUI();
        
        // --- Process Engine Input (Camera, etc.) ---
        ProcessEngineInput(deltaTime);

        // --- Update Physics Simulation ---
        UpdatePhysics(deltaTime); 

        // --- Update Application Scene Logic ---
        // This now runs AFTER physics updates the transforms of dynamic objects
        UpdateScene(deltaTime); 
    }

    /// <summary>
    /// Updates the physics simulation and synchronizes transforms.
    /// </summary>
    protected virtual void UpdatePhysics(float deltaTime)
    { 
        if (_physicsWorld == null) return;

        // --- Pre-step: Update Kinematic Bodies --- 
        // Apply GameObject transform changes to kinematic physics bodies
        foreach (var go in _gameObjects)
        {
            var rb = go.GetComponent<RigidbodyComponent>();
            if (rb != null && rb.IsInitialized && rb.BodyType == BodyType.Kinematic)
            {
                 if (_physicsWorld.Simulation.Bodies.GetBodyReference(rb.BodyHandle) is var bodyRef && bodyRef.Exists)
                 { 
                    // Convert GameObject transform to BEPU RigidPose
                    // Note: This assumes direct mapping. Adjust if Transform component uses different conventions.
                    var pose = new RigidPose(
                        Vec3Conversion.ToSystemNumerics(go.Transform.Position), 
                        QuatConversion.ToSystemNumerics(go.Transform.Rotation)
                    );
                    bodyRef.Pose = pose;
                    // TODO: Set kinematic velocity if needed (e.g., based on transform change)
                    // bodyRef.Velocity = ...
                 }
            }
        }

        // --- Step Simulation ---
        _physicsWorld.Update(deltaTime);

        // --- Post-step: Update GameObject Transforms --- 
        // Update GameObject transforms based on dynamic physics bodies
        foreach (var go in _gameObjects)
        {
            var rb = go.GetComponent<RigidbodyComponent>();
            if (rb != null && rb.IsInitialized && rb.BodyType == BodyType.Dynamic)
            {
                 if (_physicsWorld.Simulation.Bodies.GetBodyReference(rb.BodyHandle) is var bodyRef && bodyRef.Exists) 
                 {
                    // Apply physics world pose directly to local transform
                    // WARNING: This overrides parent influence for dynamic objects.
                    // More complex hierarchy synchronization might be needed for nested physics objects.
                    go.Transform.LocalPosition = Vec3Conversion.ToOpenTK(bodyRef.Pose.Position);
                    go.Transform.LocalRotation = QuatConversion.ToOpenTK(bodyRef.Pose.Orientation);
                 }
            }
            // Statics don't move, so no need to update their transforms from physics
        }
    }

    /// <summary>
    /// Handles engine-level input like camera controls.
    /// </summary>
    protected virtual void ProcessEngineInput(float deltaTime)
    {
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

        // Example: Toggle cursor grab (Engine-level convenience)
        if (InputManager.IsKeyPressed(Keys.Escape))
        {
            if (_window != null)
            { 
                _window.CursorState = _window.CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
                Log.Info($"Toggled cursor state to: {_window.CursorState}");
            }
        }
    }

    /// <summary>
    /// Called during Update. Override to implement application-specific update logic.
    /// </summary>
    protected virtual void UpdateScene(float deltaTime)
    {
        // Base implementation does nothing. Derived classes should override.
    }

    // --- Debug UI (Kept in Base Class as a Generic Feature) ---
    protected virtual void BuildDebugUI()
    {
        ImGui.Begin("Engine Debug");
        ImGui.Text($"FPS: {_fps:F1}");
        ImGui.End();
        
        ImGui.Begin("Hierarchy");
        ImGui.Text("Scene Hierarchy:");
        ImGui.Separator();
        foreach (var go in _gameObjects.Where(g => g.Transform.Parent == null))
        {
             DrawGameObjectNode(go);
        }
        ImGui.End(); 
        
        BuildInspectorPanel();
    }

    protected virtual void DrawGameObjectNode(GameObject go)
    {
        if (go == null) return;

        ImGuiTreeNodeFlags nodeFlags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
        if (go.Transform.Children.Count == 0) 
        {
             nodeFlags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
        }
        if (go == _selectedGameObject)
        {
             nodeFlags |= ImGuiTreeNodeFlags.Selected;
        }

        string nodeLabel = $"{go.Name}##{go.GetHashCode()}";
        bool nodeOpen = ImGui.TreeNodeEx(nodeLabel, nodeFlags);

        if (ImGui.IsItemClicked())
        {
            _selectedGameObject = go;
            Log.Trace($"Selected GameObject: {go.Name}");
        }

        if (nodeOpen && (nodeFlags & ImGuiTreeNodeFlags.Leaf) == 0)
        {
            foreach (var child in go.Transform.Children)
            {
                 DrawGameObjectNode(child.GameObject!); 
            }
            ImGui.TreePop();
        }
    }

    protected virtual void BuildInspectorPanel()
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
             bool isActive = _selectedGameObject.IsActive;
             if (ImGui.Checkbox("Is Active", ref isActive))
             {
                 _selectedGameObject.IsActive = isActive;
             }
             ImGui.Separator();
             ImGui.Text("Components:");
             foreach (var component in _selectedGameObject.GetAllComponents())
             {
                  string componentName = component.GetType().Name;
                  if (ImGui.CollapsingHeader($"{componentName}##{component.GetHashCode()}", ImGuiTreeNodeFlags.DefaultOpen))
                  {
                       // --- Standard Components Handled by Base Inspector --- 
                       if (TryDrawTransformInspector(component)) continue;
                       if (TryDrawDirectionalLightInspector(component)) continue;
                       if (TryDrawMeshRendererInspector(component)) continue;

                       // --- Allow Derived Classes to Add Custom Inspectors ---
                       if (!DrawCustomComponentInspector(component))
                       { 
                            ImGui.TextDisabled("(No specific inspector available)");
                       }
                  }
             }
         }
         ImGui.End();
    }
    
    /// <summary>Override to add drawing logic for custom components in the inspector.</summary>
    /// <returns>True if the component was handled, false otherwise.</returns>
    protected virtual bool DrawCustomComponentInspector(Component component)
    { 
        return false; // Base class doesn't handle custom components
    }

    // --- Standard Component Inspectors (Moved into Helper Methods) ---
    protected bool TryDrawTransformInspector(Component component)
    {
        if (component is not Transform transform) return false;

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
            localScl.X = Math.Max(localScl.X, 0.001f);
            localScl.Y = Math.Max(localScl.Y, 0.001f);
            localScl.Z = Math.Max(localScl.Z, 0.001f);
            transform.LocalScale = ToOpenTKVec3(localScl);
        }
        ImGui.Separator();
        ImGui.TextDisabled($"World Pos: {transform.Position:F2}");
        ImGui.TextDisabled($"World Rot: {transform.EulerAngles:F1}");
        ImGui.TextDisabled($"World Scale: {transform.LossyScale:F2}");
        return true;
    }

    protected bool TryDrawDirectionalLightInspector(Component component)
    {
         if (component is not DirectionalLight light) return false;

         System.Numerics.Vector3 color = ToSystemVec3(light.Color);
         if (ImGui.ColorEdit3("Color", ref color))
         {
             light.Color = ToOpenTKVec3(color);
         }
         float intensity = light.Intensity;
         if (ImGui.DragFloat("Intensity", ref intensity, 0.05f, 0.0f, 100.0f))
         {
             light.Intensity = intensity;
         }
         return true;
    }

    protected bool TryDrawMeshRendererInspector(Component component)
    {
        if (component is not MeshRenderer renderer) return false;

        string meshName = renderer.Mesh?.GetHashCode().ToString() ?? "None"; 
        string textureName = renderer.Material?.Texture?.Handle.ToString() ?? "None";
        string shaderName = renderer.Material?.Shader?.Handle.ToString() ?? "None";
        ImGui.TextDisabled($"Mesh: {meshName}");
        ImGui.TextDisabled($"Texture: {textureName}");
        ImGui.TextDisabled($"Shader: {shaderName}");
        if (renderer.Material != null)
        {
            ImGui.Separator();
            ImGui.Text("Material Properties:");
            System.Numerics.Vector3 matColor = ToSystemVec3(renderer.Material.Color);
            if (ImGui.ColorEdit3("Color##Mat", ref matColor)) // Unique ID for ColorEdit
            {
                renderer.Material.Color = ToOpenTKVec3(matColor);
            }
            float shininess = renderer.Material.Shininess;
            if (ImGui.DragFloat("Shininess##Mat", ref shininess, 0.5f, 1.0f, 256.0f)) // Unique ID for DragFloat
            {
                 renderer.Material.Shininess = shininess;
            }
        }
        return true;
    }

    // Helper methods to convert between OpenTK and System.Numerics vectors
    protected static System.Numerics.Vector3 ToSystemVec3(OpenTK.Mathematics.Vector3 v)
    {
        return new System.Numerics.Vector3(v.X, v.Y, v.Z);
    }

    protected static OpenTK.Mathematics.Vector3 ToOpenTKVec3(System.Numerics.Vector3 v)
    {
        return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
    }

    // Made protected virtual if derived classes need custom rendering logic
    protected virtual void Render()
    { 
        Renderer.Clear(); 
        if (_camera == null) return;

        // --- Standard Scene Rendering Logic --- 
        // (Find light, loop through game objects, set uniforms, draw)
        // This could be further refactored, but keep it here for now
        DirectionalLight? sceneLight = null;
        Transform? lightTransform = null;
        // Find first active directional light
        foreach (var go in _gameObjects)
        {
            if (go.IsActive && go.TryGetComponent<DirectionalLight>(out sceneLight))
            {
                lightTransform = go.Transform;
                break; 
            }
        }

        Vector3 currentLightDir = new Vector3(0, 0, -1); 
        Vector3 currentLightColor = Vector3.Zero; 
        // Removed warning log spam
        if (sceneLight != null && lightTransform != null)
        {
            currentLightDir = -lightTransform.Forward; 
            currentLightColor = sceneLight.EffectiveColor;
        }

        Vector3 cameraPosition = _camera.Position;

        foreach (var gameObject in _gameObjects)
        {
            if (!gameObject.IsActive) continue; 
            if (!gameObject.TryGetComponent<MeshRenderer>(out var meshRenderer)) continue;
            
            if (meshRenderer.Mesh != null && meshRenderer.Material != null && meshRenderer.Material.Shader != null) 
            {
                var shader = meshRenderer.Material.Shader;
                var texture = meshRenderer.Material.Texture;
                
                shader.Use();
                if (texture != null) 
                { 
                    texture.Bind(TextureUnit.Texture0);
                    shader.SetUniformInt("uTexture", 0); 
                }
                
                shader.SetUniformMat4("uModel", gameObject.Transform.GetWorldMatrix());
                shader.SetUniformMat4("uView", _camera.ViewMatrix);
                shader.SetUniformMat4("uProjection", _camera.ProjectionMatrix);
                shader.SetUniformVec3("objectColor", meshRenderer.Material.Color);
                shader.SetUniformFloat("shininess", meshRenderer.Material.Shininess);
                shader.SetUniformVec3("lightColor", currentLightColor);
                shader.SetUniformVec3("lightDir", currentLightDir);
                shader.SetUniformVec3("viewPos", cameraPosition);
                
                meshRenderer.Mesh.Bind(); 
                GL.DrawElements(PrimitiveType.Triangles, meshRenderer.Mesh.IndexCount, DrawElementsType.UnsignedInt, 0);
                meshRenderer.Mesh.Unbind();
            }
        }
    }

    protected virtual void Shutdown()
    {
        Log.Info("Starting engine shutdown...");

        // --- Unload application content first ---
        Log.Info("Unloading application content...");
        UnloadContent();
        Log.Info("Application content unloaded.");

        // Release engine-managed resources
        Log.Info("Releasing engine resources...");
        ResourceManager.ReleaseAll();

        // Dispose GameObjects created by the application
        Log.Info("Disposing application game objects...");
        foreach (var go in _gameObjects.ToList()) 
        {
            go.Dispose();
        }
        _gameObjects.Clear();
        _selectedGameObject = null;
        Log.Info("Game objects disposed.");

        // Dispose core engine systems
        Log.Info("Disposing core engine systems...");
        _physicsWorld?.Dispose(); // Added dispose
        _imGuiController?.Dispose();
        _window?.Dispose();
        Log.Info("Core engine systems disposed.");

        LogManager.Shutdown();
        Log.Info("Engine shutdown complete.");
    }
    
    /// <summary>
    /// Called during Shutdown before engine resources are released. 
    /// Override to dispose of application-specific resources not tracked by ResourceManager.
    /// </summary>
    protected virtual void UnloadContent()
    { 
        // Base implementation does nothing. Derived classes should override.
        Log.Info("Base UnloadContent called. No application-specific unloading by default.");
    }

    public void Dispose()
    {
        Shutdown();
        GC.SuppressFinalize(this);
    }

    // --- Helper Structs for Conversions --- 
    // (Place these at the end of the Application class or in a separate utility file)
    public static class Vec3Conversion
    {
        public static System.Numerics.Vector3 ToSystemNumerics(OpenTK.Mathematics.Vector3 v)
        {
            return new System.Numerics.Vector3(v.X, v.Y, v.Z);
        }

        public static OpenTK.Mathematics.Vector3 ToOpenTK(System.Numerics.Vector3 v)
        {
            return new OpenTK.Mathematics.Vector3(v.X, v.Y, v.Z);
        }
    }

    public static class QuatConversion
    {
        public static System.Numerics.Quaternion ToSystemNumerics(OpenTK.Mathematics.Quaternion v)
        {
            return new System.Numerics.Quaternion(v.X, v.Y, v.Z, v.W);
        }

        public static OpenTK.Mathematics.Quaternion ToOpenTK(System.Numerics.Quaternion v)
        {
            return new OpenTK.Mathematics.Quaternion(v.X, v.Y, v.Z, v.W);
        }
    }
}