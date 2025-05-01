using BepuPhysics.Collidables;
using BepuUtilities;
using Makina.Engine.Scene.Components;
using System.Numerics;
using BepuPhysics;

namespace Makina.Engine.Physics
{
    /// <summary>
    /// Base class for collider shapes that can be attached to a GameObject.
    /// Responsible for creating the BEPUphysics shape and providing its index.
    /// </summary>
    public abstract class ColliderComponent : Component
    {
        /// <summary>
        /// The index of the shape created by this collider in the Simulation.Shapes collection.
        /// Will be TypedIndex.Invalid if the shape hasn't been created or added yet.
        /// </summary>
        public TypedIndex ShapeIndex { get; protected set; } = default;

        /// <summary>
        /// Offset of the collider shape relative to the GameObject's transform.
        /// </summary>
        public Vector3 Offset { get; set; } = Vector3.Zero;

        // TODO: Add properties for material (friction, restitution), IsTrigger, etc.

        /// <summary>
        /// Creates the specific BEPUphysics shape (e.g., Box, Sphere) and adds it to the simulation's shape collection.
        /// </summary>
        /// <param name="world">The PhysicsWorld containing the simulation.</param>
        /// <returns>The TypedIndex of the added shape.</returns>
        public abstract TypedIndex AddShapeToSimulation(PhysicsWorld world);

        /// <summary>
        /// Calculates the inertia tensor for this collider shape based on the given mass.
        /// </summary>
        /// <param name="mass">The mass of the rigidbody.</param>
        /// <returns>The BodyInertia for the shape.</returns>
        internal abstract BodyInertia ComputeInertia(float mass);

        /// <summary>
        /// Removes the shape associated with this collider from the simulation.
        /// </summary>
        /// <param name="world">The PhysicsWorld containing the simulation.</param>
        internal virtual void RemoveShapeFromSimulation(PhysicsWorld world)
        {
            if (ShapeIndex.Exists)
            {
                world.Simulation.Shapes.Remove(ShapeIndex);
                ShapeIndex = default;
            }
        }

        // Ensure shape is removed when the component is destroyed
        public override void OnDestroy()
        {
            // TODO: Need access to PhysicsWorld here, similar to RigidbodyComponent.
            // Cleanup should ideally be managed by a central PhysicsSystem.
            // Example: Application.Instance.PhysicsSystem.RemoveCollider(this);
            base.OnDestroy();
        }
    }
} 