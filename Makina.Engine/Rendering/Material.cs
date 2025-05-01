using OpenTK.Mathematics;

namespace Makina.Engine.Rendering;

/// <summary>
/// Represents the rendering properties applied to a surface, 
/// including shader, textures, and material-specific parameters.
/// </summary>
public class Material : IDisposable // Implement IDisposable if it holds disposable resources like shader/texture in the future
{
    public Shader? Shader { get; set; }
    public Texture? Texture { get; set; }

    /// <summary>
    /// Base color tint applied to the object. Often multiplied with the texture color.
    /// </summary>
    public Vector3 Color { get; set; } = Vector3.One; // Default to white (no tint)

    /// <summary>
    /// Controls the size and intensity of the specular highlight. Higher values mean smaller, sharper highlights.
    /// </summary>
    public float Shininess { get; set; } = 32.0f; // Default Phong shininess

    // TODO: Add support for multiple textures (e.g., specular map, normal map)
    // TODO: Consider if Material should own/dispose Shader/Texture or just reference them.
    //       For now, assuming they are managed externally (like in Application or an AssetManager).

    public Material(Shader? shader = null, Texture? texture = null)
    {
        Shader = shader;
        Texture = texture;
    }

    // Basic validation check
    public bool IsValidForRendering() => Shader != null; // Shader is the minimum requirement

    public void Dispose()
    {
        // If Material were to OWN the shader/texture, dispose them here.
        // Since they are currently assigned from external sources, we do nothing.
        GC.SuppressFinalize(this);
    }
    
    // Consider adding a finalizer if this class might hold unmanaged resources later
    // ~Material() { // Dispose unmanaged resources } 
} 