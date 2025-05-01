using Makina.Engine.Core.Logging;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Runtime.InteropServices; // Added for Marshal.SizeOf

namespace Makina.Engine.Rendering.Buffers;

public class VertexBuffer : IDisposable
{
    public readonly int Handle;
    private bool _disposed = false;

    /// <summary>
    /// Creates an uninitialized Vertex Buffer Object (VBO).
    /// </summary>
    /// <param name="size">Size of the buffer in bytes.</param>
    /// <param name="usage">Usage pattern hint (e.g., StaticDraw, DynamicDraw).</param>
    public VertexBuffer(int size, BufferUsageHint usage = BufferUsageHint.StaticDraw)
    {
        Handle = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, Handle);
        GL.BufferData(BufferTarget.ArrayBuffer, size, IntPtr.Zero, usage); // Allocate memory
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0); // Unbind
        Log.Trace($"Created Vertex Buffer (Handle: {Handle}, Size: {size} bytes, Usage: {usage})");
    }

    /// <summary>
    /// Static factory method to create a Vertex Buffer Object (VBO) and initialize it with data.
    /// </summary>
    /// <typeparam name="TData">Type of the data elements.</typeparam>
    /// <param name="data">The vertex data array.</param>
    /// <param name="usage">Usage pattern hint (e.g., StaticDraw, DynamicDraw).</param>
    /// <returns>A new VertexBuffer instance initialized with the provided data.</returns>
    public static VertexBuffer CreateWithData<TData>(TData[] data, BufferUsageHint usage = BufferUsageHint.StaticDraw) where TData : struct
    {
        int size = data.Length * Marshal.SizeOf<TData>();
        VertexBuffer vbo = new VertexBuffer(size, usage); // Use the size-only constructor
        vbo.SetData(data); // Populate with data
        // Log message moved to constructor and SetData
        Log.Trace($"Created and Initialized Vertex Buffer (Handle: {vbo.Handle}, Count: {data.Length}, Usage: {usage})");
        return vbo;
    }

    public void Bind()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VertexBuffer));
        GL.BindBuffer(BufferTarget.ArrayBuffer, Handle);
    }

    public void Unbind()
    {
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    /// <summary>
    /// Updates the buffer data (entire buffer).
    /// Consider adding an overload with offset/count for partial updates if needed.
    /// </summary>
    /// <typeparam name="TData">Type of the data elements.</typeparam>
    /// <param name="data">The new data array.</param>
    public void SetData<TData>(TData[] data) where TData : struct
    {
        if (_disposed) throw new ObjectDisposedException(nameof(VertexBuffer));
        Bind();
        // Note: Assuming the data size matches the buffer's allocated size.
        // Might add size checks or use BufferSubData for partial updates.
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * Marshal.SizeOf<TData>(), data, BufferUsageHint.StaticDraw); // Usage hint might need adjustment if buffer is dynamic
        Log.Trace($"Set Vertex Buffer Data (Handle: {Handle}, Count: {data.Length})");
        Unbind();
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
            GL.DeleteBuffer(Handle);
            Log.Trace($"Deleted Vertex Buffer (Handle: {Handle})");
            _disposed = true;
        }
    }

    ~VertexBuffer()
    {
        Log.Warn($"Vertex Buffer (Handle: {Handle}) not disposed explicitly. Cleaning up in finalizer.");
        Dispose(false);
    }
} 