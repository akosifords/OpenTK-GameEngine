using Makina.Engine;
using Makina.Engine.Core;
using Makina.Engine.Core.Logging;
using Makina.Engine.Rendering;
using Makina.Engine.Rendering.Buffers;
using Makina.Engine.Scene;
using Makina.Engine.Scene.Components;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System;
using System.Linq;

namespace Sandbox
{
    public class SandboxApp : Application
    {
        // Keep references to resources if needed for UnloadContent or specific updates
        private Shader? _basicShader;
        private Texture? _containerTexture;
        private Mesh? _triangleMesh;
        private Material? _basicMaterial;

        protected override void LoadContent()
        {
            Log.Info("Sandbox: Loading content...");

            // 1. Load shared resources (formerly in Application.Initialize)
            _basicShader = new Shader("Assets/Shaders/basic.vert", "Assets/Shaders/basic.frag");
            ResourceManager.Track(_basicShader); // Track resources via the base engine's manager
            
            _containerTexture = new Texture("Assets/Textures/container.png");
            ResourceManager.Track(_containerTexture);
            
            float[] vertices = {
                // Positions          // Colors (unused)    // TexCoords  // Normals
                 0.0f,  0.5f, 0.0f,   1.0f, 0.0f, 0.0f,   0.5f, 1.0f,   0.0f, 0.0f, 1.0f, // Top
                -0.5f, -0.5f, 0.0f,   0.0f, 1.0f, 0.0f,   0.0f, 0.0f,   0.0f, 0.0f, 1.0f, // Bottom left
                 0.5f, -0.5f, 0.0f,   0.0f, 0.0f, 1.0f,   1.0f, 0.0f,   0.0f, 0.0f, 1.0f  // Bottom right
            };
            uint[] indices = { 0, 1, 2 };
            var layout = new VertexBufferLayout();
            layout.AddElement(0, 3, VertexAttribPointerType.Float, false); // Pos
            layout.AddElement(1, 3, VertexAttribPointerType.Float, false); // Color
            layout.AddElement(2, 2, VertexAttribPointerType.Float, false); // TexCoord
            layout.AddElement(3, 3, VertexAttribPointerType.Float, false); // Normal
            _triangleMesh = new Mesh(vertices, indices, layout);
            ResourceManager.Track(_triangleMesh);

            _basicMaterial = new Material(_basicShader, _containerTexture)
            {
                Color = new Vector3(1.0f, 0.5f, 0.31f), // Coral
                Shininess = 32.0f
            };
            // Note: Material itself is not tracked by ResourceManager yet

            // 2. Create GameObjects (formerly in Application.Initialize)
            var triangleObject1 = new GameObject("RotatingTriangle");
            triangleObject1.AddComponent(new MeshRenderer { Mesh = _triangleMesh, Material = _basicMaterial });
            _gameObjects.Add(triangleObject1); // Add to base class's list

            var triangleObject2 = new GameObject("ChildTriangle");
            triangleObject2.AddComponent(new MeshRenderer { Mesh = _triangleMesh, Material = _basicMaterial });
            triangleObject2.Transform.LocalPosition = new Vector3(1.5f, 0.0f, 0.0f);
            triangleObject2.Transform.LocalScale = new Vector3(0.5f);
            triangleObject2.Transform.SetParent(triangleObject1.Transform);
            _gameObjects.Add(triangleObject2);

            var lightObject = new GameObject("DirectionalLightSource");
            lightObject.AddComponent(new DirectionalLight 
            {
                Color = new Vector3(1.0f, 1.0f, 1.0f),
                Intensity = 1.0f
            });
            lightObject.Transform.LocalEulerAngles = new Vector3(45.0f, -30.0f, 0.0f); 
            _gameObjects.Add(lightObject);
            
            Log.Info("Sandbox: Scene created.");
        }

        protected override void UpdateScene(float deltaTime)
        {
            // Update specific Sandbox logic (formerly in Application.Update)
            var parentTriangle = _gameObjects.FirstOrDefault(go => go.Name == "RotatingTriangle"); 
            if (parentTriangle != null)
            { 
                 // Use the protected _timer from the base class
                float angle = (float)_timer.Elapsed.TotalSeconds * 30.0f; 
                parentTriangle.Transform.LocalEulerAngles = new Vector3(0, angle, 0);
            }
        }

        protected override void UnloadContent()
        {
            Log.Info("Sandbox: Unloading content...");
            // In this simple case, ResourceManager handles the tracked resources.
            // If we had loaded resources *not* tracked by ResourceManager,
            // we would dispose them here.
            
            // Example: If Material needed disposal and wasn't tracked:
            // _basicMaterial?.Dispose();
            
             // Nullify references
            _basicShader = null;
            _containerTexture = null;
            _triangleMesh = null;
            _basicMaterial = null;
            
            Log.Info("Sandbox: Content unloaded.");
        }

        // Optional: Override other virtual methods like Render, BuildDebugUI, 
        // DrawCustomComponentInspector if Sandbox needs specific behavior.
    }
} 