using Makina.Engine.Rendering;
using Makina.Engine.Rendering.Buffers; 

namespace Makina.Engine.Scene.Components
{
    /// <summary>
    /// Component responsible for rendering a Mesh with a specific Material.
    /// </summary>
    public class MeshRenderer : Component
    {
        public Mesh? Mesh { get; set; }
        public Material? Material { get; set; }
        
        // Removed: public Texture? Texture { get; set; }
        // Removed: public Shader? Shader { get; set; } 
    }
} 