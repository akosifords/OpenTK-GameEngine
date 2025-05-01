using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using System.Collections.Generic;

namespace Makina.Engine.Input;

/// <summary>
/// Static class to manage and query input states (keyboard, mouse).
/// </summary>
public static class InputManager
{
    // Keyboard State
    // Using HashSet for efficient checking (O(1) average)
    private static readonly HashSet<Keys> s_keysDown = new();
    private static readonly HashSet<Keys> s_keysPressedThisFrame = new(); // Keys pressed in the current frame
    private static readonly HashSet<Keys> s_keysReleasedThisFrame = new(); // Keys released in the current frame

    // Mouse State
    private static readonly HashSet<MouseButton> s_mouseButtonsDown = new();
    private static readonly HashSet<MouseButton> s_mouseButtonsPressedThisFrame = new();
    private static readonly HashSet<MouseButton> s_mouseButtonsReleasedThisFrame = new();
    private static Vector2 s_mousePosition;
    private static Vector2 s_lastMousePosition; // Added for delta calculation
    private static Vector2 s_mousePositionDelta; // Added
    private static Vector2 s_mouseScrollDelta;
    private static bool s_firstMouseMovement = true; // Prevent large jump on first focus

    // --- Public Accessors ---

    public static bool IsKeyDown(Keys key) => s_keysDown.Contains(key);
    public static bool IsKeyPressed(Keys key) => s_keysPressedThisFrame.Contains(key);
    public static bool IsKeyReleased(Keys key) => s_keysReleasedThisFrame.Contains(key);
    
    public static bool IsMouseButtonDown(MouseButton button) => s_mouseButtonsDown.Contains(button);
    public static bool IsMouseButtonPressed(MouseButton button) => s_mouseButtonsPressedThisFrame.Contains(button);
    public static bool IsMouseButtonReleased(MouseButton button) => s_mouseButtonsReleasedThisFrame.Contains(button);
    
    public static Vector2 GetMousePosition() => s_mousePosition;
    public static float GetMouseX() => s_mousePosition.X;
    public static float GetMouseY() => s_mousePosition.Y;
    public static Vector2 GetMousePositionDelta() => s_mousePositionDelta; // Added accessor
    public static Vector2 GetMouseScrollDelta() => s_mouseScrollDelta;

    // --- Internal Update Methods (Called by Window event handlers) ---

    internal static void SetKeyDown(Keys key)
    {
        if (s_keysDown.Add(key)) // Add returns true if the element was added (wasn't already present)
        {
            s_keysPressedThisFrame.Add(key);
        }
    }

    internal static void SetKeyUp(Keys key)
    {
        if (s_keysDown.Remove(key)) // Remove returns true if the element was removed (was present)
        {
            s_keysReleasedThisFrame.Add(key);
        }
    }

    internal static void SetMouseButtonDown(MouseButton button)
    {
        if (s_mouseButtonsDown.Add(button))
        {
            s_mouseButtonsPressedThisFrame.Add(button);
        }
    }

    internal static void SetMouseButtonUp(MouseButton button)
    {
        if (s_mouseButtonsDown.Remove(button))
        {
            s_mouseButtonsReleasedThisFrame.Add(button);
        }
    }

    internal static void SetMousePosition(Vector2 position)
    {
        s_lastMousePosition = s_mousePosition; // Store previous position before updating
        s_mousePosition = position;
        
        // Prevent large delta jump the first time the window receives focus
        if (s_firstMouseMovement)
        {
            s_lastMousePosition = s_mousePosition;
            s_firstMouseMovement = false;
        }
    }

    internal static void SetMouseScroll(Vector2 offset)
    {
        // Accumulate scroll during frame? Or just set? Let's just set for now.
        s_mouseScrollDelta = offset;
    }

    /// <summary>
    /// Clears the per-frame state (pressed/released keys/buttons, scroll delta).
    /// Should be called at the beginning or end of each frame/update cycle.
    /// </summary>
    internal static void FrameReset()
    {
        s_keysPressedThisFrame.Clear();
        s_keysReleasedThisFrame.Clear();
        s_mouseButtonsPressedThisFrame.Clear();
        s_mouseButtonsReleasedThisFrame.Clear();
        s_mouseScrollDelta = Vector2.Zero; // Reset scroll delta each frame
        
        // Calculate mouse delta AFTER processing events for the frame
        s_mousePositionDelta = s_mousePosition - s_lastMousePosition;
        // Important: Reset last position for the next frame's calculation AFTER calculating delta
        s_lastMousePosition = s_mousePosition; 
    }

    // Optional: Method to reset the 'first movement' flag if window focus changes
    internal static void OnFocusChanged(bool focused)
    {
        if (focused) s_firstMouseMovement = true;
    }
} 