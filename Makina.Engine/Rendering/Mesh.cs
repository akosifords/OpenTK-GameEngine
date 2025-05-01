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

    /// <summary>
    /// Creates a new Mesh.
    /// </summary>
    /// <param name="vertices">Array of vertex data (floats).</param>
    /// <param name="indices">Array of indices defining triangles.</param>
    /// <param name="layout">The layout description for the vertex data.</param>
    public Mesh(float[] vertices, uint[] indices, VertexBufferLayout layout)
    {
        if (vertices == null || vertices.Length == 0) throw new ArgumentNullException(nameof(vertices));
        if (indices == null || indices.Length == 0) throw new ArgumentNullException(nameof(indices));
        if (layout == null || layout.Elements.Count == 0) throw new ArgumentNullException(nameof(layout));

        // Create Buffers
        _vertexBuffer = VertexBuffer.CreateWithData(vertices); 
        _indexBuffer = new IndexBuffer(indices);

        // Create Vertex Array and configure layout
        VertexArray = new VertexArray();
        VertexArray.AddVertexBuffer(_vertexBuffer, layout); // Use the provided layout
        VertexArray.SetIndexBuffer(_indexBuffer);
            
        // Unbind after setup
        VertexArray.Unbind(); 
        _vertexBuffer.Unbind();
        _indexBuffer.Unbind();
        
        Log.Info($"Created Mesh (VAO: {VertexArray.Handle}, VBO: {_vertexBuffer.Handle}, IBO: {_indexBuffer.Handle}, Vertices: {vertices.Length / (layout.Stride / sizeof(float))}, Indices: {indices.Length})"); // Adjusted vertex count log
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
            // Dispose managed resources first
            if (disposing)
            {
                // Nothing managed to dispose here yet
            }

            // Dispose unmanaged OpenGL resources owned by this Mesh
            _indexBuffer?.Dispose();
            _vertexBuffer?.Dispose();
            VertexArray?.Dispose(); 
            
            Log.Trace($"Disposed Mesh resources"); // Simplified log
            _disposed = true;
        }
    }

    ~Mesh()
    {
        Log.Warn($"Mesh not disposed explicitly. Cleaning up in finalizer.");
        Dispose(false);
    }
} 