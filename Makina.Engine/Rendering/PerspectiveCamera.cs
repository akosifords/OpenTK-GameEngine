using OpenTK.Mathematics;
using Makina.Engine.Core.Math; // For MathUtil if needed later
using System;
using Makina.Engine.Core.Logging; // Added
using OpenTK.Windowing.GraphicsLibraryFramework; // Added for Keys

namespace Makina.Engine.Rendering;

/// <summary>
/// Represents a camera using perspective projection, with support for first-person controls.
/// </summary>
public class PerspectiveCamera : Camera
{
    private Vector3 _front = -Vector3.UnitZ; // Initial direction
    private Vector3 _up = Vector3.UnitY;
    private Vector3 _right = Vector3.UnitX;
    private readonly Vector3 _worldUp = Vector3.UnitY; // Keep track of world up direction
    private float _fov = MathUtil.DegreesToRadians(45.0f); // Field of View in radians
    private float _nearClip = 0.1f;
    private float _farClip = 100.0f;

    // Euler Angles (in radians)
    private float _yaw = -MathUtil.Pi / 2.0f; // Initialize pointing along -Z
    private float _pitch = 0.0f;

    // Camera options
    private float _movementSpeed = 2.5f;
    private float _mouseSensitivity = 0.1f;

    public Vector3 Front => _front;
    public Vector3 Up => _up;
    public Vector3 Right => _right;

    public float FieldOfView
    {
        get => _fov;
        set
        {
            _fov = value;
            RecalculateProjectionMatrix();
        }
    }

    public float NearClipPlane
    {
        get => _nearClip;
        set
        {
            _nearClip = value;
            RecalculateProjectionMatrix();
        }
    }

    public float FarClipPlane
    {
        get => _farClip;
        set
        {
            _farClip = value;
            RecalculateProjectionMatrix();
        }
    }

    public float Yaw
    {
        get => MathUtil.RadiansToDegrees(_yaw);
        set
        {
            _yaw = MathUtil.DegreesToRadians(value);
            UpdateCameraVectors();
        }
    }

    public float Pitch
    {
        get => MathUtil.RadiansToDegrees(_pitch);
        set
        {
            // Constrain pitch to avoid flipping
            _pitch = MathUtil.DegreesToRadians(Math.Clamp(value, -89.0f, 89.0f));
            UpdateCameraVectors();
        }
    }

    public float MovementSpeed { get => _movementSpeed; set => _movementSpeed = value; }
    public float MouseSensitivity { get => _mouseSensitivity; set => _mouseSensitivity = value; }

    public PerspectiveCamera(Vector3 position, float aspectRatio, float fovDegrees = 45.0f)
        : base(position, aspectRatio)
    {
        FieldOfView = MathUtil.DegreesToRadians(fovDegrees);
        UpdateCameraVectors(); // Initial calculation of front/right/up from Yaw/Pitch
        // RecalculateViewMatrix(); // Called by UpdateCameraVectors -> Position setter
        RecalculateProjectionMatrix();
    }

    /// <summary>
    /// Processes input received from any keyboard-like input system.
    /// </summary>
    public void ProcessKeyboard(Keys key, float deltaTime)
    {
        float velocity = MovementSpeed * deltaTime;
        bool moved = true;
        switch (key)
        {
            case Keys.W: Position += Front * velocity; break;
            case Keys.S: Position -= Front * velocity; break;
            case Keys.A: Position -= Right * velocity; break;
            case Keys.D: Position += Right * velocity; break;
            // Optional: Add Up/Down movement
            // case Keys.Space: Position += _worldUp * velocity; break;
            // case Keys.LeftShift: Position -= _worldUp * velocity; break;
            default: moved = false; break;
        }
        // No need to call RecalculateViewMatrix here, the Position setter does it.
        // if (moved) Log.Trace($"Camera Pos: {Position}");
    }

    /// <summary>
    /// Processes input received from a mouse input system.
    /// Expects the offset value in both the x and y direction.
    /// </summary>
    public void ProcessMouseMovement(float xOffset, float yOffset, bool constrainPitch = true)
    {
        xOffset *= MouseSensitivity;
        yOffset *= MouseSensitivity;

        // Adjust Yaw and Pitch directly (values are expected in degrees)
        float newYaw = Yaw + xOffset;
        float newPitch = Pitch - yOffset; // Reversed since y-coordinates range from bottom to top

        // Directly setting properties handles clamping and calls UpdateCameraVectors
        Yaw = newYaw;
        Pitch = constrainPitch ? newPitch : Math.Clamp(newPitch, -89.0f, 89.0f); // Use property logic for clamping
        // No need to call UpdateCameraVectors() here, the Yaw/Pitch setters do it.
        
        // Log.Trace($"Yaw: {Yaw}, Pitch: {Pitch}");
    }

    /// <summary>
    /// Calculates the front vector from the Camera's (updated) Euler Angles.
    /// Also recalculates the Right and Up vector.
    /// </summary>
    private void UpdateCameraVectors()
    {
        // Calculate the new Front vector
        Vector3 front;
        front.X = (float)Math.Cos(_yaw) * (float)Math.Cos(_pitch);
        front.Y = (float)Math.Sin(_pitch);
        front.Z = (float)Math.Sin(_yaw) * (float)Math.Cos(_pitch);
        _front = Vector3.Normalize(front);
        
        // Also re-calculate the Right and Up vector
        _right = Vector3.Normalize(Vector3.Cross(_front, _worldUp));  // Normalize the vectors, because their length gets closer to 0 the more you look up or down which results in slower movement.
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        
        // View matrix update needs to happen whenever vectors change
        RecalculateViewMatrix(); 
    }

    protected override void RecalculateViewMatrix()
    {
        // View matrix depends on Position and the calculated direction vectors
        ViewMatrix = Matrix4.LookAt(Position, Position + _front, _up);
        UpdateViewProjectionMatrix();
    }

    protected override void RecalculateProjectionMatrix()
    {
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearClipPlane, FarClipPlane);
        UpdateViewProjectionMatrix();
    }
} 