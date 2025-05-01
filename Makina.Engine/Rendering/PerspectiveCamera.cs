using OpenTK.Mathematics;
using Makina.Engine.Core.Math; // For MathUtil if needed later
using System;

namespace Makina.Engine.Rendering;

/// <summary>
/// Represents a camera using perspective projection.
/// </summary>
public class PerspectiveCamera : Camera
{
    private Vector3 _front = -Vector3.UnitZ; // Initial direction
    private Vector3 _up = Vector3.UnitY;
    private Vector3 _right = Vector3.UnitX;
    private float _fov = MathUtil.DegreesToRadians(45.0f); // Field of View in radians
    private float _nearClip = 0.1f;
    private float _farClip = 100.0f;

    // TODO: Add properties for Yaw, Pitch if implementing free-look camera controls
    // private float _yaw = -90.0f;
    // private float _pitch = 0.0f;

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

    public PerspectiveCamera(Vector3 position, float aspectRatio, float fovDegrees = 45.0f)
        : base(position, aspectRatio)
    {
        FieldOfView = MathUtil.DegreesToRadians(fovDegrees);
        // Initial matrix calculations
        RecalculateViewMatrix(); 
        RecalculateProjectionMatrix();
    }

    // TODO: Implement method to update direction vectors based on Yaw/Pitch
    // private void UpdateCameraVectors() { ... }

    protected override void RecalculateViewMatrix()
    {
        // Update direction vectors if using Yaw/Pitch
        // UpdateCameraVectors(); 

        // Calculate the new Right and Up vectors based on the Front direction
        // This ensures the camera remains correctly oriented (prevents roll)
        _right = Vector3.Normalize(Vector3.Cross(_front, Vector3.UnitY)); 
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
        
        ViewMatrix = Matrix4.LookAt(Position, Position + _front, _up);
        UpdateViewProjectionMatrix();
    }

    protected override void RecalculateProjectionMatrix()
    {
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView(FieldOfView, AspectRatio, NearClipPlane, FarClipPlane);
        UpdateViewProjectionMatrix();
    }
} 