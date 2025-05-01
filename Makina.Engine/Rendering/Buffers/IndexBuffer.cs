using Makina.Engine.Core.Logging;
using OpenTK.Graphics.OpenGL4;
using System;

namespace Makina.Engine.Rendering.Buffers;

public class IndexBuffer : IDisposable
{
    public readonly int Handle;
    public readonly int Count; // Number of indices
    private bool _disposed = false;

    /// <summary>
    /// Creates an Index Buffer Object (IBO / EBO).
    /// </summary>
    /// <param name="indices">Array of unsigned integer indices.</param>
    /// <param name="usage">Usage pattern hint (e.g., StaticDraw, DynamicDraw).</param>
    public IndexBuffer(uint[] indices, BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        Handle = GL.GenBuffer();
        Count = indices.Length;

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, Handle);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(uint), indices, usage);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0); // Unbind
        Log.Trace($"Created Index Buffer (Handle: {Handle}, Count: {Count}, Usage: {usage})");
    }

    public void Bind()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(IndexBuffer));
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, Handle);
    }

    public void Unbind()
    {
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
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
            // No managed resources to dispose currently

            // Always dispose unmanaged OpenGL resource
            GL.DeleteBuffer(Handle);
            Log.Trace($"Deleted Index Buffer (Handle: {Handle})");
            _disposed = true;
        }
    }

    ~IndexBuffer()
    {
        Log.Warn($"Index Buffer (Handle: {Handle}) not disposed explicitly. Cleaning up in finalizer.");
        Dispose(false);
    }
} 