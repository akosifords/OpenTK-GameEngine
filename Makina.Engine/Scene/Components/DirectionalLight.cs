using OpenTK.Mathematics;

namespace Makina.Engine.Scene.Components
{
    /// <summary>
    /// Represents a directional light source in the scene.
    /// The direction is determined by the forward vector of the GameObject's Transform.
    /// </summary>
    public class DirectionalLight : Component
    {
        /// <summary>
        /// The color of the light.
        /// </summary>
        public Vector3 Color { get; set; } = Vector3.One; // Default to white light

        /// <summary>
        /// The intensity of the light. Multiplies the color.
        /// </summary>
        public float Intensity { get; set; } = 1.0f;

        // Convenience getter for the final light color including intensity
        public Vector3 EffectiveColor => Color * Intensity;
    }
} 