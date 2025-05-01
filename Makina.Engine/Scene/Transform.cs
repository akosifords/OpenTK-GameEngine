using OpenTK.Mathematics;
using System;
using Makina.Engine.Scene.Components;

namespace Makina.Engine.Scene;

/// <summary>
/// Represents the position, rotation, and scale of a GameObject in 3D space.
/// </summary>
public class Transform : Component
{
    private Vector3 _position = Vector3.Zero;
    private Quaternion _rotation = Quaternion.Identity;
    private Vector3 _scale = Vector3.One;

    private Matrix4 _localMatrix = Matrix4.Identity;
    private bool _isDirty = true; // Flag to recalculate matrix only when needed

    public Vector3 Position
    {
        get => _position;
        set { _position = value; _isDirty = true; }
    }

    public Quaternion Rotation
    {
        get => _rotation;
        set { _rotation = value; _isDirty = true; }
    }

    public Vector3 Scale
    {
        get => _scale;
        set { _scale = value; _isDirty = true; }
    }

    // Convenience accessors/mutators for Euler angles (degrees)
    // Be cautious with Euler angles due to gimbal lock issues.
    public Vector3 EulerAngles
    {
        get => Rotation.ToEulerAngles() * Core.Math.MathUtil.RadToDeg;
        set => Rotation = Quaternion.FromEulerAngles(value * Core.Math.MathUtil.DegToRad);
    }

    // TODO: Add Parent/Child relationship support later for scene hierarchy
    // public Transform? Parent { get; set; }
    // public List<Transform> Children { get; } = new List<Transform>();
    // public Matrix4 WorldMatrix { get { ... calculate based on parent ... } }

    /// <summary>
    /// Gets the local transformation matrix (relative to parent, or world if no parent).
    /// Recalculates the matrix only if position, rotation, or scale has changed.
    /// </summary>
    public Matrix4 GetLocalMatrix()
    {
        if (_isDirty)
        {
            // TRS order: Scale -> Rotate -> Translate
            Matrix4 scaleMat = Matrix4.CreateScale(Scale);
            Matrix4 rotMat = Matrix4.CreateFromQuaternion(Rotation);
            Matrix4 transMat = Matrix4.CreateTranslation(Position);
            _localMatrix = scaleMat * rotMat * transMat;
            _isDirty = false;
        }
        return _localMatrix;
    }

    // Reset to default values
    public void Reset()
    {
        Position = Vector3.Zero;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
        // _isDirty will be set by setters
    }
} 