using BepuPhysics.Collidables;
using BepuUtilities;
using System.Numerics;
using BepuPhysics;

namespace Makina.Engine.Physics
{
    /// <summary>
    /// Represents a box-shaped collider.
    /// </summary>
    public class BoxColliderComponent : ColliderComponent
    {
        /// <summary>
        /// Width of the box (along X-axis).
        /// </summary>
        public float Width { get; set; } = 1.0f;

        /// <summary>
        /// Height of the box (along Y-axis).
        /// </summary>
        public float Height { get; set; } = 1.0f;

        /// <summary>
        /// Length of the box (along Z-axis).
        /// </summary>
        public float Length { get; set; } = 1.0f;

        public override TypedIndex AddShapeToSimulation(PhysicsWorld world)
        {
            if (ShapeIndex.Exists) RemoveShapeFromSimulation(world); // Remove old shape if exists

            var boxShape = new Box(Width, Height, Length);
            ShapeIndex = world.Simulation.Shapes.Add(boxShape);
            return ShapeIndex;
        }

        internal override BodyInertia ComputeInertia(float mass)
        {
            var boxShape = new Box(Width, Height, Length);
            return boxShape.ComputeInertia(mass);
        }
    }
} 