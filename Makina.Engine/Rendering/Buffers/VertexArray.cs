using Makina.Engine.Core.Logging;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace Makina.Engine.Rendering.Buffers;

public class VertexArray : IDisposable
{
    public readonly int Handle;
    private readonly List<VertexBuffer> _vertexBuffers = new List<VertexBuffer>();
    private IndexBuffer? _indexBuffer = null;
    private bool _disposed = false;

    public VertexArray()
    {
        Handle = GL.GenVertexArray();
        Log.Trace($"Created Vertex Array (Handle: {Handle})");
    }

    public void Bind()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VertexArray));
        GL.BindVertexArray(Handle);
    }

    public void Unbind()
    {
        GL.BindVertexArray(0);
    }

    /// <summary>
    /// Adds a Vertex Buffer and configures its layout.
    /// </summary>
    /// <param name="vertexBuffer">The VertexBuffer to add.</param>
    /// <param name="layout">The layout description (attribute index, size, type, stride, offset).</param>
    public void AddVertexBuffer(VertexBuffer vertexBuffer, VertexBufferLayout layout)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VertexArray));
        if (layout.Elements.Count == 0) 
        {
            Log.Warn("Attempted to add VertexBuffer with empty layout.");
            return;
        }

        Bind(); // Bind VAO
        vertexBuffer.Bind(); // Bind VBO

        foreach (var element in layout.Elements)
        {
            GL.EnableVertexAttribArray(element.Index);
            GL.VertexAttribPointer(
                element.Index,         // Attribute index (location in shader)
                element.Size,          // Number of components (e.g., 3 for vec3)
                element.Type,          // Data type (e.g., Float)
                element.Normalized,    // Normalize data? (usually false for floats)
                layout.Stride,         // Stride - Size of one vertex in bytes
                element.Offset         // Offset of this attribute within the vertex struct
            );
            Log.Trace($"Configured Vertex Attrib (Index: {element.Index}, Size: {element.Size}, Type: {element.Type}, Stride: {layout.Stride}, Offset: {element.Offset})");
        }
        
        _vertexBuffers.Add(vertexBuffer);
        
        // Unbind VBO but keep VAO bound for potential IndexBuffer binding
        vertexBuffer.Unbind(); 
    }

    public void SetIndexBuffer(IndexBuffer indexBuffer)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VertexArray));
        
        Bind(); // Bind VAO
        indexBuffer.Bind(); // Bind IBO

        _indexBuffer = indexBuffer;
        Log.Trace($"Set Index Buffer (Handle: {indexBuffer.Handle}) for VAO (Handle: {Handle})");
        
        // Keep VAO bound after setting IBO is fine, or unbind here
        // Unbind();
    }

    public IndexBuffer? GetIndexBuffer() => _indexBuffer;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            // No managed resources to dispose currently

            // Always dispose unmanaged OpenGL resource
            GL.DeleteVertexArray(Handle);
            Log.Trace($"Deleted Vertex Array (Handle: {Handle})");
            
            // Note: This class currently does NOT own the VBOs/IBO.
            // They must be disposed separately.
            _disposed = true;
        }
    }

    ~VertexArray()
    {
        Log.Warn($"Vertex Array (Handle: {Handle}) not disposed explicitly. Cleaning up in finalizer.");
        Dispose(false);
    }
}

// --- Helper classes for Layout ---

public struct BufferElement
{
    public uint Index; // Corresponds to layout(location=...) in shader
    public int Size; // Number of components (e.g., 1, 2, 3, 4)
    public VertexAttribPointerType Type; // Data type (Float, Int, etc.)
    public bool Normalized;
    public int Offset; // Offset within the vertex struct

    public BufferElement(uint index, int size, VertexAttribPointerType type, bool normalized, int offset)
    {
        Index = index;
        Size = size;
        Type = type;
        Normalized = normalized;
        Offset = offset;
    }

    // Helper to get size in bytes of the element type
    public static int GetSizeOfType(VertexAttribPointerType type)
    {
        switch (type)
        {
            case VertexAttribPointerType.Float: return 4;
            case VertexAttribPointerType.Int: return 4;
            case VertexAttribPointerType.UnsignedInt: return 4;
            case VertexAttribPointerType.Byte: return 1;
            case VertexAttribPointerType.UnsignedByte: return 1;
            // Add other types as needed
            default: Log.Error($"Unsupported VertexAttribPointerType: {type}"); return 0;
        }
    }
}

public class VertexBufferLayout
{
    public List<BufferElement> Elements { get; } = new List<BufferElement>();
    public int Stride { get; private set; } = 0;

    public void AddElement(uint index, int size, VertexAttribPointerType type, bool normalized = false)
    {
        Elements.Add(new BufferElement(index, size, type, normalized, Stride));
        Stride += size * BufferElement.GetSizeOfType(type);
    }
} 