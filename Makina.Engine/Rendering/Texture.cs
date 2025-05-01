using Makina.Engine.Core.Logging;
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;
using System;
using System.IO;

namespace Makina.Engine.Rendering;

public class Texture : IDisposable
{
    public readonly int Handle;
    public readonly int Width;
    public readonly int Height;
    private bool _disposed = false;

    public Texture(string filePath)
    {
        if (!File.Exists(filePath)) 
        {
            throw new FileNotFoundException("Texture file not found.", filePath);
        }

        // Generate handle
        Handle = GL.GenTexture();
        Bind(); // Bind for configuration

        // Load image using StbImageSharp
        StbImage.stbi_set_flip_vertically_on_load(1); // Flip vertically for OpenGL
        ImageResult image;
        try
        {
            using (var stream = File.OpenRead(filePath))
            {
                image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha); // Load as RGBA
            }
            Width = image.Width;
            Height = image.Height;
            Log.Trace($"Loaded texture '{Path.GetFileName(filePath)}' ({Width}x{Height})");
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to load texture file: {filePath}");
            GL.DeleteTexture(Handle); // Clean up handle
            throw;
        }
        
        // Upload pixel data
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Srgb8Alpha8, // Use sRGB format for automatic gamma correction
                      Width, Height, 0, 
                      PixelFormat.Rgba, PixelType.UnsignedByte, image.Data);

        // Set texture parameters
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        // Generate Mipmaps
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        Log.Trace($"Generated mipmaps for texture (Handle: {Handle})");

        Unbind(); // Unbind after configuration
    }

    /// <summary>
    /// Creates a texture directly from pixel data in memory.
    /// Used primarily for ImGui font atlas.
    /// </summary>
    public Texture(string internalName, int width, int height, IntPtr data)
    {
        Width = width;
        Height = height;

        Handle = GL.GenTexture();
        Bind();

        // Use Rgba8 internal format, expect RGBA input data
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8,
                      Width, Height, 0,
                      PixelFormat.Rgba, PixelType.UnsignedByte, data);

        // Set basic parameters suitable for UI/font rendering
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);

        Log.Trace($"Created texture '{internalName}' from data (Handle: {Handle}, {Width}x{Height})");

        Unbind();
    }

    public void Bind(TextureUnit unit = TextureUnit.Texture0)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Texture));
        GL.ActiveTexture(unit); 
        GL.BindTexture(TextureTarget.Texture2D, Handle);
    }

    public void Unbind(TextureUnit unit = TextureUnit.Texture0)
    {
        // Usually not needed to unbind explicitly
        // GL.ActiveTexture(unit);
        // GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            GL.DeleteTexture(Handle);
            Log.Trace($"Disposed Texture (Handle: {Handle})");
            _disposed = true;
        }
    }

    ~Texture()
    {
        Log.Warn($"Texture (Handle: {Handle}) not disposed explicitly. Cleaning up in finalizer.");
        Dispose(false);
    }
} 