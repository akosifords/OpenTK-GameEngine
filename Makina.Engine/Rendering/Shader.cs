using Makina.Engine.Core.Logging;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Collections.Generic;
using System.IO;
using OpenTK.Mathematics;

namespace Makina.Engine.Rendering;

public class Shader : IDisposable
{
    public readonly int Handle;
    private bool _disposed = false;

    // Simple uniform location caching
    private readonly Dictionary<string, int> _uniformLocationCache = new Dictionary<string, int>();

    public Shader(string vertexPath, string fragmentPath)
    {
        string vertexShaderSource;
        try
        {
            vertexShaderSource = File.ReadAllText(vertexPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to read vertex shader file: {vertexPath}");
            throw; // Re-throw for now, could handle more gracefully
        }

        string fragmentShaderSource;
        try
        {
            fragmentShaderSource = File.ReadAllText(fragmentPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, $"Failed to read fragment shader file: {fragmentPath}");
            throw;
        }

        int vertexShader = CompileShader(ShaderType.VertexShader, vertexShaderSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentShaderSource);

        // Create shader program
        Handle = GL.CreateProgram();
        Log.Trace($"Created shader program (Handle: {Handle})");

        // Attach shaders and link
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        LinkProgram(Handle);

        // Detach and delete shaders after linking (they are linked into the program)
        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(fragmentShader);
        GL.DeleteShader(vertexShader);
    }

    /// <summary>
    /// Creates a shader program directly from source code strings.
    /// Used for internally defined shaders like ImGui's.
    /// </summary>
    public Shader(string internalName, string vertexSource, string fragmentSource)
    {
        int vertexShader = CompileShader(ShaderType.VertexShader, vertexSource);
        int fragmentShader = CompileShader(ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        Log.Trace($"Created shader program '{internalName}' (Handle: {Handle}) from source");

        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        LinkProgram(Handle);

        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(fragmentShader);
        GL.DeleteShader(vertexShader);
    }

    private int CompileShader(ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        // Check for compile errors
        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            Log.Error($"Shader compilation failed ({type}):\n{infoLog}");
            GL.DeleteShader(shader); // Don't leak the shader
            throw new InvalidOperationException($"Shader compilation failed ({type}).");
        }
        Log.Trace($"Compiled shader (Handle: {shader}, Type: {type})");
        return shader;
    }

    private void LinkProgram(int program)
    {
        GL.LinkProgram(program);

        // Check for linking errors
        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(program);
            Log.Error($"Shader program linking failed:\n{infoLog}");
            throw new InvalidOperationException("Shader program linking failed.");
        }
        Log.Trace($"Linked shader program (Handle: {program})");
    }

    public void Use()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Shader));
        GL.UseProgram(Handle);
    }

    // --- Uniform Setting Methods ---

    public int GetUniformLocation(string name)
    {
        if (_uniformLocationCache.TryGetValue(name, out int location))
        {
            return location;
        }

        location = GL.GetUniformLocation(Handle, name);
        if (location == -1) 
        {
            Log.Warn($"Uniform '{name}' not found in shader program (Handle: {Handle}).");
        }
        _uniformLocationCache[name] = location;
        return location;
    }

    public void SetUniformMat4(string name, Matrix4 data)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Shader));
        int location = GetUniformLocation(name);
        if (location != -1)
        {
            // The 'transpose' parameter is set to false because OpenTK matrices are already column-major.
            GL.UniformMatrix4(location, false, ref data);
        }
    }
    
    public void SetUniformInt(string name, int value)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Shader));
        int location = GetUniformLocation(name);
        if (location != -1)
        {
            GL.Uniform1(location, value);
        }
    }

    public void SetUniformVec3(string name, Vector3 data)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Shader));
        int location = GetUniformLocation(name);
        if (location != -1)
        {
            GL.Uniform3(location, ref data); // Use GL.Uniform3 for Vector3
        }
    }

    public void SetUniformFloat(string name, float value)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(Shader));
        int location = GetUniformLocation(name);
        if (location != -1)
        {
            GL.Uniform1(location, value); // Use GL.Uniform1 for single float
        }
    }

    // TODO: Add SetUniformFloat, SetUniformVec3, etc.

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
                _uniformLocationCache.Clear();
            }

            // Always dispose unmanaged OpenGL resource
            GL.DeleteProgram(Handle);
            Log.Trace($"Deleted shader program (Handle: {Handle})");
            _disposed = true;
        }
    }

    ~Shader()
    {
        // Finalizer calls Dispose(false) to clean up unmanaged resources
        // Important: Should only be called by the GC
        Log.Warn($"Shader program (Handle: {Handle}) not disposed explicitly. Cleaning up in finalizer.");
        Dispose(false);
    }
} 