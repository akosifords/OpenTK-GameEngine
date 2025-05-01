using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Makina.Engine.Core.Logging;
using Makina.Engine.Rendering;

namespace Makina.Engine.Debugging;

/// <summary>
/// Controller class to manage Dear ImGui setup and rendering within an OpenTK GameWindow.
/// Adapted from ImGui.NET samples.
/// </summary>
public class ImGuiController : IDisposable
{
    private bool _frameBegun;

    private int _vertexArray;
    private int _vertexBuffer;
    private int _vertexBufferSize;
    private int _indexBuffer;
    private int _indexBufferSize;

    private Texture _fontTexture;

    private Rendering.Shader _shader;

    private int _windowWidth;
    private int _windowHeight;

    private System.Numerics.Vector2 _scaleFactor = System.Numerics.Vector2.One;

    // Cache Enum values to avoid allocation every frame
    private static readonly Keys[] _allKeys = (Keys[])Enum.GetValues(typeof(Keys));
    private static readonly Dictionary<Keys, ImGuiKey> _keyMapping = new Dictionary<Keys, ImGuiKey>();

    static ImGuiController()
    {
        // Populate the key mapping dictionary once
        PopulateKeyMapping();
    }

    /// <summary>
    /// Constructs a new ImGui controller.
    /// </summary>
    public ImGuiController(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);
        var io = ImGui.GetIO();
        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset; 

        // Enable docking if desired
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        CreateDeviceResources();

        SetPerFrameImGuiData(1f / 60f); // Initial delta time guess

        ImGui.NewFrame();
        _frameBegun = true;
        Log.Info("ImGuiController initialized.");
    }

    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    public void DestroyDeviceObjects()
    {
        Dispose();
    }

    public void CreateDeviceResources()
    {
        _vertexBufferSize = 10000;
        _indexBufferSize = 2000;

        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();

        string VertexSource = @"#version 330 core
            uniform mat4 projection_matrix;

            layout(location = 0) in vec2 in_position;
            layout(location = 1) in vec2 in_texCoord;
            layout(location = 2) in vec4 in_color;

            out vec4 color;
            out vec2 texCoord;

            void main()
            {
                gl_Position = projection_matrix * vec4(in_position, 0, 1);
                color = in_color;
                texCoord = in_texCoord;
            }";

        string FragmentSource = @"#version 330 core
            uniform sampler2D in_fontTexture;

            in vec4 color;
            in vec2 texCoord;

            out vec4 outputColor;

            void main()
            {
                outputColor = color * texture(in_fontTexture, texCoord);
            }";

        _shader = new Rendering.Shader("ImGui", VertexSource, FragmentSource);

        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), sizeof(float) * 2);
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, Unsafe.SizeOf<ImDrawVert>(), sizeof(float) * 4);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        // GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0); // VAO implicitly binds EBO
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);
        
        _fontTexture?.Dispose(); // Dispose previous texture if recreating
        _fontTexture = new Texture("ImGuiFont", width, height, pixels); // Use the new constructor
        // Parameters are set within the Texture constructor now

        io.Fonts.SetTexID((IntPtr)_fontTexture.Handle);

        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// </summary>
    public void Render()
    {
        if (_frameBegun)
        {
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }
    }

    /// <summary>
    /// Updates ImGui input and starts a new frame.
    /// </summary>
    public void Update(GameWindow wnd, float deltaSeconds)
    {
        if (_frameBegun)
        {
            ImGui.Render();
        }

        SetPerFrameImGuiData(deltaSeconds);
        UpdateImGuiInput(wnd);

        _frameBegun = true;
        ImGui.NewFrame();
    }

    /// <summary>
    /// Sets per-frame data based on the associated window. Assumes the ImGui context is current.
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        io.DisplayFramebufferScale = _scaleFactor;
        io.DeltaTime = deltaSeconds;
    }

    readonly List<char> PressedChars = new List<char>();

    private void UpdateImGuiInput(GameWindow wnd)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        MouseState MouseState = wnd.MouseState;
        KeyboardState KeyboardState = wnd.KeyboardState;

        io.MouseDown[0] = MouseState.IsButtonDown(MouseButton.Left);
        io.MouseDown[1] = MouseState.IsButtonDown(MouseButton.Right);
        io.MouseDown[2] = MouseState.IsButtonDown(MouseButton.Middle);
        // Others...

        var screenPoint = new Vector2i((int)MouseState.X, (int)MouseState.Y);
        var point = screenPoint;//wnd.PointToClient(screenPoint);
        io.MousePos = new System.Numerics.Vector2(point.X, point.Y);
        io.MouseWheel = MouseState.ScrollDelta.Y;
        io.MouseWheelH = MouseState.ScrollDelta.X;

        // Use cached key array and mapping dictionary
        foreach (Keys key in _allKeys) // Use cached array
        {   
            // Use AddKeyEvent instead of deprecated KeysDown
            if (key == Keys.Unknown) continue; 
            // ImGuiKey imguikey = ConvertToImGuiKey(key); // No longer needed
            if (_keyMapping.TryGetValue(key, out ImGuiKey imguikey) && imguikey != ImGuiKey.None) // Use dictionary lookup
            {
                 io.AddKeyEvent(imguikey, KeyboardState.IsKeyDown(key));
            }
        } 

        foreach (var c in PressedChars)
        {
             io.AddInputCharacter(c);
        }
        PressedChars.Clear();

        io.KeyCtrl = KeyboardState.IsKeyDown(Keys.LeftControl) || KeyboardState.IsKeyDown(Keys.RightControl);
        io.KeyAlt = KeyboardState.IsKeyDown(Keys.LeftAlt) || KeyboardState.IsKeyDown(Keys.RightAlt);
        io.KeyShift = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift);
        io.KeySuper = KeyboardState.IsKeyDown(Keys.LeftSuper) || KeyboardState.IsKeyDown(Keys.RightSuper);
    }

    internal void PressChar(char keyChar)
    {
        PressedChars.Add(keyChar);
    }

    private void RenderImDrawData(ImDrawDataPtr draw_data)
    {
        if (draw_data.CmdListsCount == 0) return;
        
        // Setup render state: alpha-blending enabled, no face culling, no depth testing, scissor enabled, polygon fill
        GL.Enable(EnableCap.Blend);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);
        GL.Enable(EnableCap.ScissorTest);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);

        // Setup viewport, orthographic projection matrix
        GL.Viewport(0, 0, _windowWidth, _windowHeight);
        float L = draw_data.DisplayPos.X;
        float R = draw_data.DisplayPos.X + draw_data.DisplaySize.X;
        float T = draw_data.DisplayPos.Y;
        float B = draw_data.DisplayPos.Y + draw_data.DisplaySize.Y;

        Matrix4 ortho_projection = Matrix4.CreateOrthographicOffCenter(L, R, B, T, -1.0f, 1.0f);

        _shader.Use();
        GL.UniformMatrix4(_shader.GetUniformLocation("projection_matrix"), false, ref ortho_projection);
        GL.Uniform1(_shader.GetUniformLocation("in_fontTexture"), 0); // Use texture unit 0 for font

        GL.BindVertexArray(_vertexArray);

        // Will project scissor/clipping rectangles into framebuffer space
        System.Numerics.Vector2 clip_off = draw_data.DisplayPos;         // (0,0) unless using multi-viewports
        System.Numerics.Vector2 clip_scale = draw_data.FramebufferScale; // (1,1) unless using retina display which are scaled

        // Render command lists
        for (int n = 0; n < draw_data.CmdListsCount; n++)
        {
            ImDrawListPtr cmd_list = draw_data.CmdLists[n];

            // Upload vertex/index buffers
            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferData(BufferTarget.ArrayBuffer, cmd_list.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>(), cmd_list.VtxBuffer.Data, BufferUsageHint.StreamDraw);
            
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
            GL.BufferData(BufferTarget.ElementArrayBuffer, cmd_list.IdxBuffer.Size * sizeof(ushort), cmd_list.IdxBuffer.Data, BufferUsageHint.StreamDraw);

            for (int cmd_i = 0; cmd_i < cmd_list.CmdBuffer.Size; cmd_i++)
            {
                ImDrawCmdPtr pcmd = cmd_list.CmdBuffer[cmd_i];
                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException(); // User callbacks not implemented
                }
                else
                {
                     // Project scissor/clipping rectangles into framebuffer space
                    System.Numerics.Vector4 clip_rect = pcmd.ClipRect;
                    clip_rect.X = (clip_rect.X - clip_off.X) * clip_scale.X;
                    clip_rect.Y = (clip_rect.Y - clip_off.Y) * clip_scale.Y;
                    clip_rect.Z = (clip_rect.Z - clip_off.X) * clip_scale.X;
                    clip_rect.W = (clip_rect.W - clip_off.Y) * clip_scale.Y;

                    if (clip_rect.X < _windowWidth && clip_rect.Y < _windowHeight && clip_rect.Z >= 0.0f && clip_rect.W >= 0.0f)
                    {
                        GL.Scissor((int)clip_rect.X, _windowHeight - (int)clip_rect.W, (int)(clip_rect.Z - clip_rect.X), (int)(clip_rect.W - clip_rect.Y));

                        GL.ActiveTexture(TextureUnit.Texture0);
                        GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                        GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
                    }
                }
            }
        }

        // Restore modified GL state
        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);
        // Reset texture binding
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, 0);
        // Unbind VAO (Important: Otherwise, the main render pass might accidently use ImGui's VAO state)
        GL.BindVertexArray(0); 
    }

    public void Dispose()
    {
        _fontTexture?.Dispose();
        _shader?.Dispose();
        
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);
        
        // Consider destroying the ImGui context if this controller is the sole owner
        // ImGui.DestroyContext();
        Log.Info("ImGuiController disposed.");
    }

    // Helper to populate the key mapping dictionary
    private static void PopulateKeyMapping()
    {
        // This covers most common keys, but might need expansion for less common ones
        // For non-mappable keys, it returns ImGuiKey.None
        _keyMapping.Clear(); // Ensure it's empty before populating

        // Letters
        _keyMapping[Keys.A] = ImGuiKey.A;
        _keyMapping[Keys.B] = ImGuiKey.B;
        _keyMapping[Keys.C] = ImGuiKey.C;
        _keyMapping[Keys.D] = ImGuiKey.D;
        _keyMapping[Keys.E] = ImGuiKey.E;
        _keyMapping[Keys.F] = ImGuiKey.F;
        _keyMapping[Keys.G] = ImGuiKey.G;
        _keyMapping[Keys.H] = ImGuiKey.H;
        _keyMapping[Keys.I] = ImGuiKey.I;
        _keyMapping[Keys.J] = ImGuiKey.J;
        _keyMapping[Keys.K] = ImGuiKey.K;
        _keyMapping[Keys.L] = ImGuiKey.L;
        _keyMapping[Keys.M] = ImGuiKey.M;
        _keyMapping[Keys.N] = ImGuiKey.N;
        _keyMapping[Keys.O] = ImGuiKey.O;
        _keyMapping[Keys.P] = ImGuiKey.P;
        _keyMapping[Keys.Q] = ImGuiKey.Q;
        _keyMapping[Keys.R] = ImGuiKey.R;
        _keyMapping[Keys.S] = ImGuiKey.S;
        _keyMapping[Keys.T] = ImGuiKey.T;
        _keyMapping[Keys.U] = ImGuiKey.U;
        _keyMapping[Keys.V] = ImGuiKey.V;
        _keyMapping[Keys.W] = ImGuiKey.W;
        _keyMapping[Keys.X] = ImGuiKey.X;
        _keyMapping[Keys.Y] = ImGuiKey.Y;
        _keyMapping[Keys.Z] = ImGuiKey.Z;
        // Numbers
        _keyMapping[Keys.D0] = ImGuiKey._0;
        _keyMapping[Keys.D1] = ImGuiKey._1;
        _keyMapping[Keys.D2] = ImGuiKey._2;
        _keyMapping[Keys.D3] = ImGuiKey._3;
        _keyMapping[Keys.D4] = ImGuiKey._4;
        _keyMapping[Keys.D5] = ImGuiKey._5;
        _keyMapping[Keys.D6] = ImGuiKey._6;
        _keyMapping[Keys.D7] = ImGuiKey._7;
        _keyMapping[Keys.D8] = ImGuiKey._8;
        _keyMapping[Keys.D9] = ImGuiKey._9;
        // Function Keys
        _keyMapping[Keys.F1] = ImGuiKey.F1;
        _keyMapping[Keys.F2] = ImGuiKey.F2;
        _keyMapping[Keys.F3] = ImGuiKey.F3;
        _keyMapping[Keys.F4] = ImGuiKey.F4;
        _keyMapping[Keys.F5] = ImGuiKey.F5;
        _keyMapping[Keys.F6] = ImGuiKey.F6;
        _keyMapping[Keys.F7] = ImGuiKey.F7;
        _keyMapping[Keys.F8] = ImGuiKey.F8;
        _keyMapping[Keys.F9] = ImGuiKey.F9;
        _keyMapping[Keys.F10] = ImGuiKey.F10;
        _keyMapping[Keys.F11] = ImGuiKey.F11;
        _keyMapping[Keys.F12] = ImGuiKey.F12;
        // Special Keys
        _keyMapping[Keys.Tab] = ImGuiKey.Tab;
        _keyMapping[Keys.Enter] = ImGuiKey.Enter;
        _keyMapping[Keys.Escape] = ImGuiKey.Escape;
        _keyMapping[Keys.Space] = ImGuiKey.Space;
        _keyMapping[Keys.Backspace] = ImGuiKey.Backspace;
        _keyMapping[Keys.Delete] = ImGuiKey.Delete;
        _keyMapping[Keys.Insert] = ImGuiKey.Insert;
        _keyMapping[Keys.Up] = ImGuiKey.UpArrow;
        _keyMapping[Keys.Down] = ImGuiKey.DownArrow;
        _keyMapping[Keys.Left] = ImGuiKey.LeftArrow;
        _keyMapping[Keys.Right] = ImGuiKey.RightArrow;
        _keyMapping[Keys.Home] = ImGuiKey.Home;
        _keyMapping[Keys.End] = ImGuiKey.End;
        _keyMapping[Keys.PageUp] = ImGuiKey.PageUp;
        _keyMapping[Keys.PageDown] = ImGuiKey.PageDown;
        _keyMapping[Keys.CapsLock] = ImGuiKey.CapsLock;
        _keyMapping[Keys.ScrollLock] = ImGuiKey.ScrollLock;
        _keyMapping[Keys.PrintScreen] = ImGuiKey.PrintScreen;
        _keyMapping[Keys.Pause] = ImGuiKey.Pause;
        // Modifiers (AddKeyEvent handles these implicitly via KeyCtrl, KeyShift etc flags)
        _keyMapping[Keys.LeftShift] = ImGuiKey.ModShift; 
        _keyMapping[Keys.RightShift] = ImGuiKey.ModShift;
        _keyMapping[Keys.LeftControl] = ImGuiKey.ModCtrl;
        _keyMapping[Keys.RightControl] = ImGuiKey.ModCtrl;
        _keyMapping[Keys.LeftAlt] = ImGuiKey.ModAlt;
        _keyMapping[Keys.RightAlt] = ImGuiKey.ModAlt;
        _keyMapping[Keys.LeftSuper] = ImGuiKey.ModSuper;
        _keyMapping[Keys.RightSuper] = ImGuiKey.ModSuper;
        // Keypad
        _keyMapping[Keys.KeyPad0] = ImGuiKey.Keypad0;
        _keyMapping[Keys.KeyPad1] = ImGuiKey.Keypad1;
        _keyMapping[Keys.KeyPad2] = ImGuiKey.Keypad2;
        _keyMapping[Keys.KeyPad3] = ImGuiKey.Keypad3;
        _keyMapping[Keys.KeyPad4] = ImGuiKey.Keypad4;
        _keyMapping[Keys.KeyPad5] = ImGuiKey.Keypad5;
        _keyMapping[Keys.KeyPad6] = ImGuiKey.Keypad6;
        _keyMapping[Keys.KeyPad7] = ImGuiKey.Keypad7;
        _keyMapping[Keys.KeyPad8] = ImGuiKey.Keypad8;
        _keyMapping[Keys.KeyPad9] = ImGuiKey.Keypad9;
        _keyMapping[Keys.KeyPadDecimal] = ImGuiKey.KeypadDecimal;
        _keyMapping[Keys.KeyPadDivide] = ImGuiKey.KeypadDivide;
        _keyMapping[Keys.KeyPadMultiply] = ImGuiKey.KeypadMultiply;
        _keyMapping[Keys.KeyPadSubtract] = ImGuiKey.KeypadSubtract;
        _keyMapping[Keys.KeyPadAdd] = ImGuiKey.KeypadAdd;
        _keyMapping[Keys.KeyPadEnter] = ImGuiKey.KeypadEnter;
        // Punctuation (Example, add more as needed)
        _keyMapping[Keys.Apostrophe] = ImGuiKey.Apostrophe;
        _keyMapping[Keys.Comma] = ImGuiKey.Comma;
        _keyMapping[Keys.Minus] = ImGuiKey.Minus;
        _keyMapping[Keys.Period] = ImGuiKey.Period;
        _keyMapping[Keys.Slash] = ImGuiKey.Slash;
        _keyMapping[Keys.Semicolon] = ImGuiKey.Semicolon;
        _keyMapping[Keys.Equal] = ImGuiKey.Equal;
        _keyMapping[Keys.LeftBracket] = ImGuiKey.LeftBracket;
        _keyMapping[Keys.Backslash] = ImGuiKey.Backslash;
        _keyMapping[Keys.RightBracket] = ImGuiKey.RightBracket;
        _keyMapping[Keys.GraveAccent] = ImGuiKey.GraveAccent;

        // Note: Some keys might not have direct mappings or might be handled differently (e.g., NumLock)
        // We default to ImGuiKey.None if a mapping isn't found later.
    }
}

// --- Helper Texture class (Minimal version for ImGui font) ---
// We use our main Texture class now, so this can be removed if not needed elsewhere.
/*
internal class Texture : IDisposable
{
    public readonly int Handle;

    public Texture(string name, int width, int height, IntPtr data)
    {
        Handle = GL.GenTexture();
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, Handle);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            PixelFormat.Bgra, PixelType.UnsignedByte, data);
        
        // Set filtering parameters...

        GL.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Bind() => GL.BindTexture(TextureTarget.Texture2D, Handle);
    public void Unbind() => GL.BindTexture(TextureTarget.Texture2D, 0);

    public void Dispose() => GL.DeleteTexture(Handle);
}
*/

// --- Helper Shader Class (Minimal version for ImGui) ---
// We use our main Shader class now. This needs slight adaptation.
internal class Shader : IDisposable
{
    public readonly int Handle;
    private readonly Dictionary<string, int> _uniformLocations;

    // Creates shader from source strings
    public Shader(string name, string vertexSource, string fragmentSource)
    {
        int vertexShader = CompileShader(name, ShaderType.VertexShader, vertexSource);
        int fragmentShader = CompileShader(name, ShaderType.FragmentShader, fragmentSource);

        Handle = GL.CreateProgram();
        GL.AttachShader(Handle, vertexShader);
        GL.AttachShader(Handle, fragmentShader);
        GL.LinkProgram(Handle);

        GL.GetProgram(Handle, GetProgramParameterName.LinkStatus, out var success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(Handle);
            Log.Error($"Failed to link ImGui shader program '{name}': {infoLog}");
            throw new Exception($"Shader linking failed: {infoLog}");
        }

        GL.DetachShader(Handle, vertexShader);
        GL.DetachShader(Handle, fragmentShader);
        GL.DeleteShader(fragmentShader);
        GL.DeleteShader(vertexShader);

        // Cache uniform locations
        _uniformLocations = new Dictionary<string, int>();
        GL.GetProgram(Handle, GetProgramParameterName.ActiveUniforms, out int uniformCount);
        for (int i = 0; i < uniformCount; i++)
        {
            string key = GL.GetActiveUniform(Handle, i, out _, out _);
            int location = GL.GetUniformLocation(Handle, key);
            _uniformLocations.Add(key, location);
        }
    }

    private static int CompileShader(string name, ShaderType type, string source)
    {
        int handle = GL.CreateShader(type);
        GL.ShaderSource(handle, source);
        GL.CompileShader(handle);
        GL.GetShader(handle, ShaderParameter.CompileStatus, out var success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(handle);
            Log.Error($"Failed to compile ImGui {type} shader '{name}': {infoLog}");
             throw new Exception($"Shader compilation failed: {infoLog}");
        }
        return handle;
    }

    public void Use() => GL.UseProgram(Handle);

    public int GetUniformLocation(string name) => _uniformLocations.GetValueOrDefault(name, -1);

    public void Dispose()
    {
        GL.DeleteProgram(Handle);
    }
} 