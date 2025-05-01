using Makina.Engine.Core.Logging;
using Makina.Engine.Rendering.Buffers;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;

namespace Makina.Engine.Rendering;

/// <summary>
/// Represents a collection of vertices and indices, along with their 
/// corresponding GPU buffers (VAO, VBO, IBO), defining a piece of geometry.
/// </summary>
public class Mesh : IDisposable
{
    public VertexArray VertexArray { get; private set; }
    private VertexBuffer _vertexBuffer;
    private IndexBuffer _indexBuffer;
    private bool _disposed = false;

    // TODO: Extend constructor or add methods to handle more complex vertex layouts (e.g., positions, normals, texcoords)
    
    /// <summary>
    /// Creates a new Mesh with position-only vertex data.
    /// </summary>
    /// <param name="vertices">Array of vertex positions (floats, 3 per vertex).</param>
    /// <param name="indices">Array of indices defining triangles.</param>
    public Mesh(float[] vertices, uint[] indices)
    {
        if (vertices == null || vertices.Length == 0) throw new ArgumentNullException(nameof(vertices));
        if (indices == null || indices.Length == 0) throw new ArgumentNullException(nameof(indices));

        // Create Buffers
        _vertexBuffer = VertexBuffer.CreateWithData(vertices); 
        _indexBuffer = new IndexBuffer(indices);

        // Create Vertex Array and configure layout
        VertexArray = new VertexArray();
        var layout = new VertexBufferLayout();
        // Assuming layout(location = 0) is vec3 position
        layout.AddElement(0, 3, VertexAttribPointerType.Float, false); 
            
        VertexArray.AddVertexBuffer(_vertexBuffer, layout);
        VertexArray.SetIndexBuffer(_indexBuffer);
            
        // Unbind after setup
        VertexArray.Unbind(); 
        _vertexBuffer.Unbind();
        _indexBuffer.Unbind();
        
        Log.Info($"Created Mesh (VAO: {VertexArray.Handle}, VBO: {_vertexBuffer.Handle}, IBO: {_indexBuffer.Handle}, Vertices: {vertices.Length/3}, Indices: {indices.Length})");
    }

    public int IndexCount => _indexBuffer.Count;

    public void Bind()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Mesh));
        VertexArray.Bind();
    }

    public void Unbind()
    { 
        VertexArray.Unbind();
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
            if (disposing)
            {
                // Dispose managed resources if any were held directly by Mesh (none currently)
            }

            // Dispose unmanaged OpenGL resources owned by this Mesh
            // Order: VAO contains references, so dispose buffers first is often safer, then VAO.
            // Although in our current setup VAO doesn't strictly own them, it's good practice.
            _indexBuffer?.Dispose();
            _vertexBuffer?.Dispose();
            VertexArray?.Dispose(); 
            
            Log.Trace($"Disposed Mesh (VAO: {VertexArray?.Handle})");
            _disposed = true;
        }
    }

    ~Mesh()
    {
        // We expect Dispose to be called, so log a warning if the finalizer runs.
        Log.Warn($"Mesh (VAO: {VertexArray?.Handle}) not disposed explicitly. Cleaning up in finalizer.");
        Dispose(false);
    }
} 