using BepuPhysics.Collidables;
using BepuUtilities;
using System.Numerics;
using BepuPhysics;

namespace Makina.Engine.Physics
{
    /// <summary>
    /// Represents a sphere-shaped collider.
    /// </summary>
    public class SphereColliderComponent : ColliderComponent
    {
        /// <summary>
        /// Radius of the sphere.
        /// </summary>
        public float Radius { get; set; } = 0.5f;

        public override TypedIndex AddShapeToSimulation(PhysicsWorld world)
        {
            if (ShapeIndex.Exists) RemoveShapeFromSimulation(world);

            var sphereShape = new Sphere(Radius);
            ShapeIndex = world.Simulation.Shapes.Add(sphereShape);
            return ShapeIndex;
        }

        internal override BodyInertia ComputeInertia(float mass)
        {
            var sphereShape = new Sphere(Radius);
            return sphereShape.ComputeInertia(mass);
        }
    }
} 