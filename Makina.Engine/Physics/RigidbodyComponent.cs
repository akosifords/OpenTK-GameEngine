using BepuPhysics;
using BepuPhysics.Collidables;
using BepuUtilities;
using Makina.Engine.Scene.Components;
using System.Numerics;
using Makina.Engine.Core.Logging;
using System.Linq; // Added for GetComponent

namespace Makina.Engine.Physics
{
    /// <summary>
    /// Represents a physical body in the simulation, associated with a GameObject.
    /// Links a GameObject's Transform to a BEPUphysics BodyHandle or StaticHandle.
    /// Requires a separate ColliderComponent on the same GameObject to define the shape.
    /// </summary>
    public class RigidbodyComponent : Component
    {
        public BodyHandle BodyHandle { get; internal set; } = default;
        public StaticHandle StaticHandle { get; internal set; } = default;
        public TypedIndex ShapeIndex { get; internal set; } = default;

        // Configuration properties (set before initialization)
        public BodyType BodyType { get; set; } = BodyType.Dynamic;
        public float Mass { get; set; } = 1.0f; // Used only for dynamic bodies
        public BodyActivityDescription ActivityDescription { get; set; } = new BodyActivityDescription(0.01f); // Default sleep threshold

        // Internal state
        public bool IsInitialized { get; set; } = false;

        // --- Initialization/Cleanup (Called by Physics System) ---

        public void InitializeBody(PhysicsWorld world, TypedIndex shapeIndex, RigidPose pose)
        {
            if (IsInitialized) throw new InvalidOperationException("Rigidbody already initialized.");
            if (!shapeIndex.Exists) throw new ArgumentException("Cannot initialize Rigidbody without a valid ShapeIndex.", nameof(shapeIndex));

            ShapeIndex = shapeIndex;
            var collidable = new CollidableDescription(ShapeIndex, 0.1f);

            switch (BodyType)
            {
                case BodyType.Dynamic:
                    if (Mass <= 0) 
                    {
                        Log.Warn($"Dynamic Rigidbody Mass should be positive ({Mass}). Using default 1.0f.");
                        Mass = 1.0f;
                    }
                    
                    // Get inertia from the ColliderComponent
                    var collider = GameObject.GetComponent<ColliderComponent>();
                    if (collider == null)
                    {
                        Log.Error($"Cannot initialize dynamic Rigidbody on GameObject '{GameObject.Name}' without a ColliderComponent.");
                        return; // Or throw exception
                    }
                    var inertia = collider.ComputeInertia(Mass);

                    var bodyDesc = BodyDescription.CreateDynamic(pose, inertia, collidable, ActivityDescription);
                    BodyHandle = world.Simulation.Bodies.Add(bodyDesc);
                    break;

                case BodyType.Kinematic:
                    Mass = 0; 
                    var kinematicDesc = BodyDescription.CreateKinematic(pose, collidable, ActivityDescription);
                    BodyHandle = world.Simulation.Bodies.Add(kinematicDesc);
                    break;

                case BodyType.Static:
                    Mass = float.PositiveInfinity;
                    var staticDesc = new StaticDescription(pose, ShapeIndex); 
                    StaticHandle = world.Simulation.Statics.Add(staticDesc);
                    break;
            }

            IsInitialized = true;
            // Register the component in the PhysicsWorld mapping
            world.RegisterComponent(this);
        }

        // Called when the component is removed or GameObject destroyed
        internal void Cleanup(PhysicsWorld world)
        {
             if (!IsInitialized) return;

            // Unregister before removing from simulation
            world.UnregisterComponent(this);

            if (BodyHandle.Value != default)
            {
                world.Simulation.Bodies.Remove(BodyHandle);
                BodyHandle = default;
            }
            if (StaticHandle.Value != default)
            {
                world.Simulation.Statics.Remove(StaticHandle);
                StaticHandle = default;
            }
            
            ShapeIndex = default;
            IsInitialized = false;
        }

        // Override OnDestroy to ensure cleanup (Requires access to PhysicsWorld)
        public override void OnDestroy()
        {
            // TODO: Need access to the PhysicsWorld instance here.
            // Example: Application.Instance.PhysicsSystem.RemoveRigidbody(this);
            // If we had access: _physicsWorld?.Cleanup(this);
            base.OnDestroy();
        }
    }

    public enum BodyType { Dynamic, Kinematic, Static }
} 