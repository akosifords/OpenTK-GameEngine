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
using BepuPhysics;
using Makina.Engine.Physics;
using BepuPhysics.Collidables;
using BepuUtilities;
using Mesh = Makina.Engine.Rendering.Mesh;

namespace Sandbox
{
    public class SandboxApp : Application
    {
        // Keep references to resources if needed for UnloadContent or specific updates
        private Shader? _basicShader;
        private Texture? _containerTexture;
        // private Mesh? _triangleMesh; // Removed triangle mesh
        private Mesh? _cubeMesh;     // Added cube mesh
        private Material? _basicMaterial;
        private Mesh? _planeMesh;

        protected override void LoadContent()
        {
            Log.Info("Sandbox: Loading content...");

            // 1. Load shared resources (formerly in Application.Initialize)
            _basicShader = new Shader("Assets/Shaders/basic.vert", "Assets/Shaders/basic.frag");
            ResourceManager.Track(_basicShader); // Track resources via the base engine's manager
            
            _containerTexture = new Texture("Assets/Textures/container.png");
            ResourceManager.Track(_containerTexture);
            
            /* Removed triangle mesh definition
            float[] vertices = {
                // Positions          // Colors (unused)    // TexCoords  // Normals
                 0.0f,  0.5f, 0.0f,   1.0f, 0.0f, 0.0f,   0.5f, 1.0f,   0.0f, 0.0f, 1.0f, // Top
                -0.5f, -0.5f, 0.0f,   0.0f, 1.0f, 0.0f,   0.0f, 0.0f,   0.0f, 0.0f, 1.0f, // Bottom left
                 0.5f, -0.5f, 0.0f,   0.0f, 0.0f, 1.0f,   1.0f, 0.0f,   0.0f, 0.0f, 1.0f  // Bottom right
            };
            uint[] indices = { 0, 1, 2 };
            */
            var layout = new VertexBufferLayout();
            layout.AddElement(0, 3, VertexAttribPointerType.Float, false); // Pos
            layout.AddElement(1, 3, VertexAttribPointerType.Float, false); // Color (Keep layout element even if unused in cube data)
            layout.AddElement(2, 2, VertexAttribPointerType.Float, false); // TexCoord
            layout.AddElement(3, 3, VertexAttribPointerType.Float, false); // Normal
            // _triangleMesh = new Mesh(vertices, indices, layout); // Removed triangle mesh creation
            // ResourceManager.Track(_triangleMesh); // Removed tracking

            // Create a cube mesh
            float[] cubeVertices = {
                // Positions          // Colors (unused)    // TexCoords // Normals
                // Back face
                -0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 0.0f,  0.0f,  0.0f, -1.0f,
                 0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f,  0.0f,  0.0f, -1.0f,
                 0.5f,  0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 1.0f,  0.0f,  0.0f, -1.0f,
                 0.5f,  0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 1.0f,  0.0f,  0.0f, -1.0f,
                -0.5f,  0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f,  0.0f,  0.0f, -1.0f,
                -0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 0.0f,  0.0f,  0.0f, -1.0f,

                // Front face
                -0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 0.0f,  0.0f,  0.0f,  1.0f,
                 0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f,  0.0f,  0.0f,  1.0f,
                 0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 1.0f,  0.0f,  0.0f,  1.0f,
                 0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 1.0f,  0.0f,  0.0f,  1.0f,
                -0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f,  0.0f,  0.0f,  1.0f,
                -0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 0.0f,  0.0f,  0.0f,  1.0f,

                // Left face (Corrected UVs)
                -0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f, -1.0f,  0.0f,  0.0f, // Top-Left
                -0.5f,  0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 1.0f, -1.0f,  0.0f,  0.0f, // Top-Right
                -0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f, -1.0f,  0.0f,  0.0f, // Bottom-Right
                -0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f, -1.0f,  0.0f,  0.0f, // Bottom-Right (Triangle 2)
                -0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 0.0f, -1.0f,  0.0f,  0.0f, // Bottom-Left
                -0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f, -1.0f,  0.0f,  0.0f, // Top-Left (Triangle 2)

                // Right face
                 0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f,  1.0f,  0.0f,  0.0f,
                 0.5f,  0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 1.0f,  1.0f,  0.0f,  0.0f,
                 0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f,  1.0f,  0.0f,  0.0f,
                 0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f,  1.0f,  0.0f,  0.0f,
                 0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 0.0f,  1.0f,  0.0f,  0.0f,
                 0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f,  1.0f,  0.0f,  0.0f,

                // Bottom face
                -0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f,  0.0f, -1.0f,  0.0f,
                 0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 1.0f,  0.0f, -1.0f,  0.0f,
                 0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f,  0.0f, -1.0f,  0.0f,
                 0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f,  0.0f, -1.0f,  0.0f,
                -0.5f, -0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 0.0f,  0.0f, -1.0f,  0.0f,
                -0.5f, -0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f,  0.0f, -1.0f,  0.0f,

                // Top face
                -0.5f,  0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f,  0.0f,  1.0f,  0.0f,
                 0.5f,  0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 1.0f,  0.0f,  1.0f,  0.0f,
                 0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f,  0.0f,  1.0f,  0.0f,
                 0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  1.0f, 0.0f,  0.0f,  1.0f,  0.0f,
                -0.5f,  0.5f,  0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 0.0f,  0.0f,  1.0f,  0.0f,
                -0.5f,  0.5f, -0.5f,  1.0f, 1.0f, 1.0f,  0.0f, 1.0f,  0.0f,  1.0f,  0.0f
            };
            // Cube vertices are defined per-face, requiring simple indices
            uint[] cubeIndices = Enumerable.Range(0, 36).Select(i => (uint)i).ToArray(); 

            // Cube doesn't need indices if vertices are defined per-triangle face
            _cubeMesh = new Mesh(cubeVertices, cubeIndices, layout); // Use generated indices
            ResourceManager.Track(_cubeMesh);

            // Create a simple plane mesh
            float[] planeVertices = {
                // Positions          // Colors (unused)    // TexCoords  // Normals (pointing up)
                 10.0f, 0.0f,  10.0f,   1.0f, 1.0f, 1.0f,   1.0f, 1.0f,   0.0f, 1.0f, 0.0f, // Top Right
                 10.0f, 0.0f, -10.0f,   1.0f, 1.0f, 1.0f,   1.0f, 0.0f,   0.0f, 1.0f, 0.0f, // Bottom Right
                -10.0f, 0.0f, -10.0f,   1.0f, 1.0f, 1.0f,   0.0f, 0.0f,   0.0f, 1.0f, 0.0f, // Bottom Left
                -10.0f, 0.0f,  10.0f,   1.0f, 1.0f, 1.0f,   0.0f, 1.0f,   0.0f, 1.0f, 0.0f  // Top Left
            };
            uint[] planeIndices = {
                0, 1, 3, // First Triangle
                1, 2, 3  // Second Triangle
            };
            // Reuse the same layout as the triangle for simplicity
            _planeMesh = new Mesh(planeVertices, planeIndices, layout);
            ResourceManager.Track(_planeMesh);

            _basicMaterial = new Material(_basicShader, _containerTexture)
            {
                Color = new Vector3(1.0f, 0.5f, 0.31f), // Coral
                Shininess = 32.0f
            };
            // Note: Material itself is not tracked by ResourceManager yet

            // 2. Create GameObjects (formerly in Application.Initialize)
            var ground = new GameObject("GroundPlane");
            ground.Transform.LocalPosition = new Vector3(0, -2f, 0);
            ground.AddComponent(new MeshRenderer { Mesh = _planeMesh, Material = _basicMaterial });
            var groundCollider = ground.AddComponent(new BoxColliderComponent
            {
                 Width = 20f,
                 Height = 1f,
                 Length = 20f
            });
            var groundRb = ground.AddComponent(new RigidbodyComponent
            {
                BodyType = BodyType.Static
            });
            _gameObjects.Add(ground);
            InitializePhysicsForGameObject(ground);

            var dynamicCube = new GameObject("DynamicCube"); // Renamed
            dynamicCube.Transform.LocalPosition = new Vector3(0, 2f, 0);
            dynamicCube.AddComponent(new MeshRenderer { Mesh = _cubeMesh, Material = _basicMaterial }); // Use cube mesh
            var cubeCollider = dynamicCube.AddComponent(new BoxColliderComponent // Renamed variable
            {
                 Width = 1f,   // Adjusted collider size
                 Height = 1f,  // Adjusted collider size
                 Length = 1f   // Adjusted collider size
            });
            var cubeRb = dynamicCube.AddComponent(new RigidbodyComponent // Renamed variable
            {
                 BodyType = BodyType.Dynamic,
                 Mass = 1.0f
            });
            _gameObjects.Add(dynamicCube);
            InitializePhysicsForGameObject(dynamicCube);

            var lightObject = new GameObject("DirectionalLightSource1");
            lightObject.AddComponent(new DirectionalLight 
            {
                Color = new Vector3(1.0f, 1.0f, 1.0f),
                Intensity = 1.0f
            });
            lightObject.Transform.LocalEulerAngles = new Vector3(45.0f, -30.0f, 0.0f);
            _gameObjects.Add(lightObject);

            Log.Info("Sandbox: Scene created with physics objects.");
        }

        /// <summary>
        /// Helper to initialize physics components for a GameObject.
        /// TODO: This logic should ideally live in a dedicated PhysicsSystem 
        /// that observes component additions/removals.
        /// </summary>
        private void InitializePhysicsForGameObject(GameObject go)
        {
            if (_physicsWorld == null) 
            {
                Log.Error("Cannot initialize physics, PhysicsWorld is null.");
                return;
            }

            var rb = go.GetComponent<RigidbodyComponent>();
            var collider = go.GetComponent<ColliderComponent>();

            if (rb != null && collider != null)
            {
                 if (rb.IsInitialized) 
                 {
                    Log.Warn($"Rigidbody on {go.Name} already initialized.");
                    return;
                 }

                // 1. Add collider shape to simulation
                TypedIndex shapeIndex = collider.AddShapeToSimulation(_physicsWorld);
                if (!shapeIndex.Exists)
                {
                    Log.Error($"Failed to add collider shape for {go.Name} to simulation.");
                    return;
                }

                // 2. Calculate pose (position/rotation)
                // Use collider offset relative to transform
                var worldPos = go.Transform.Position + Vector3.Transform((Vector3)collider.Offset, go.Transform.Rotation);
                var worldRot = go.Transform.Rotation;
                var pose = new RigidPose(Vec3Conversion.ToSystemNumerics(worldPos), QuatConversion.ToSystemNumerics(worldRot));
                
                // 3. Initialize the rigidbody (static, dynamic, kinematic)
                rb.InitializeBody(_physicsWorld, shapeIndex, pose); 
                 Log.Trace($"Initialized physics for {go.Name} (Type: {rb.BodyType})");
            }
            else if (rb != null && collider == null)
            {
                Log.Warn($"GameObject {go.Name} has RigidbodyComponent but no ColliderComponent. Physics body not created.");
            }
            // Collider without rigidbody is fine (e.g., for trigger volumes later)
        }

        protected override void UpdateScene(float deltaTime)
        {
            // Other non-physics scene updates can go here
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
            // _triangleMesh = null; // Removed
            _cubeMesh = null;     // Added
            _basicMaterial = null;
            _planeMesh = null;
            
            Log.Info("Sandbox: Content unloaded.");
        }

        // Optional: Override other virtual methods like Render, BuildDebugUI, 
        // DrawCustomComponentInspector if Sandbox needs specific behavior.
    }
} 