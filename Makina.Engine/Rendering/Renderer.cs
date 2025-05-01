using Makina.Engine.Core.Logging;
using OpenTK.Graphics.OpenGL4;
using System.Drawing;

namespace Makina.Engine.Rendering;

public static class Renderer
{
    public static void Init()
    {
        Log.Info("Initializing Renderer...");
        
        // GameWindow handles OpenGL context creation and makes it current.
        // OpenTK's static GL class uses the current context automatically.
        // We just need to set initial state.

        // Basic OpenGL Setup
        try
        {
            GL.ClearColor(Color.FromArgb(255, 74, 78, 105)); // Set clear color to dark grey
            Log.Info($"Default clear color set to dark grey.");

            // TODO: Add other initial GL setup (depth testing, blending, etc.)
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.CullFace);
            GL.CullFace(CullFaceMode.Back);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during OpenGL initialization (ClearColor).");
            // Handle error - maybe rethrow or set a flag?
        }
    }

    public static void Clear()
    {
        // Clear the color buffer
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
    }

    // Optional: Add a method to be called on window resize
    public static void OnWindowResize(int width, int height)
    {
        GL.Viewport(0, 0, width, height);
        Log.Debug($"Renderer viewport updated to: {width}x{height}");
    }
} 