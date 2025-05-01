using Makina.Engine.Rendering;
using Makina.Engine.Rendering.Buffers; 

namespace Makina.Engine.Scene.Components
{
    /// <summary>
    /// Component responsible for rendering a Mesh with a specific Shader and Texture.
    /// </summary>
    public class MeshRenderer : Component
    {
        public Mesh? Mesh { get; set; }
        public Texture? Texture { get; set; }
        public Shader? Shader { get; set; } 

        // We could add Material property here later to group Shader/Texture/etc.
    }
} 