using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using Makina.Engine.Scene.Components;
using Makina.Engine.Core.Logging;

namespace Makina.Engine.Scene.Components;

/// <summary>
/// Represents the position, rotation, and scale of a GameObject in 3D space,
/// supporting parent-child hierarchical relationships.
/// </summary>
public class Transform : Component
{
    // --- Private Fields ---

    private Vector3 _localPosition = Vector3.Zero;
    private Quaternion _localRotation = Quaternion.Identity;
    private Vector3 _localScale = Vector3.One;

    private Transform? _parent = null;
    private readonly List<Transform> _children = new List<Transform>();

    private Matrix4 _localMatrix = Matrix4.Identity;
    private Matrix4 _worldMatrix = Matrix4.Identity;

    // Dirty flags to recalculate matrices only when needed
    private bool _isLocalDirty = true; 
    private bool _isWorldDirty = true; 

    // --- Public Properties (Local Space) ---

    /// <summary>
    /// Position relative to the parent Transform.
    /// </summary>
    public Vector3 LocalPosition
    {
        get => _localPosition;
        set { _localPosition = value; MarkDirty(); }
    }

    /// <summary>
    /// Rotation relative to the parent Transform.
    /// </summary>
    public Quaternion LocalRotation
    {
        get => _localRotation;
        set { _localRotation = value; MarkDirty(); }
    }

    /// <summary>
    /// Scale relative to the parent Transform.
    /// </summary>
    public Vector3 LocalScale
    {
        get => _localScale;
        set { _localScale = value; MarkDirty(); }
    }
    
    /// <summary>
    /// Euler angles (in degrees) relative to the parent Transform.
    /// Be cautious with Euler angles due to gimbal lock issues.
    /// </summary>
    public Vector3 LocalEulerAngles
    {
        get => LocalRotation.ToEulerAngles() * Core.Math.MathUtil.RadToDeg;
        set => LocalRotation = Quaternion.FromEulerAngles(value * Core.Math.MathUtil.DegToRad);
        // Setter automatically calls MarkDirty via LocalRotation setter
    }

    // --- Hierarchy Properties ---

    public Transform? Parent
    {
        get => _parent;
        private set // Use SetParent method to manage relationships
        {
            if (_parent == value) return; // No change

            // Remove from old parent's children list
            _parent?._children.Remove(this);

            _parent = value;

            // Add to new parent's children list
            _parent?._children.Add(this);

            MarkDirty(); // World transform is definitely changed
        }
    }

    public IReadOnlyList<Transform> Children => _children.AsReadOnly();
    
    public Transform Root => Parent == null ? this : Parent.Root;

    // --- Public Properties (World Space - Getters Only) ---

    /// <summary>
    /// Position in world space.
    /// </summary>
    public Vector3 Position => GetWorldMatrix().ExtractTranslation();

    /// <summary>
    /// Rotation in world space.
    /// </summary>
    public Quaternion Rotation => GetWorldMatrix().ExtractRotation();

    /// <summary>
    /// Scale in world space (lossy, represents combined scale).
    /// </summary>
    public Vector3 LossyScale => GetWorldMatrix().ExtractScale();
    
    /// <summary>
    /// Euler angles (in degrees) in world space.
    /// Note: Subject to gimbal lock and interpretation issues depending on rotation order.
    /// </summary>
    public Vector3 EulerAngles => Rotation.ToEulerAngles() * Core.Math.MathUtil.RadToDeg;
    
    /// <summary>
    /// Forward direction vector in world space.
    /// </summary>
    public Vector3 Forward => Vector3.Transform(-Vector3.UnitZ, Rotation);

    /// <summary>
    /// Right direction vector in world space.
    /// </summary>
    public Vector3 Right => Vector3.Transform(Vector3.UnitX, Rotation);

    /// <summary>
    /// Up direction vector in world space.
    /// </summary>
    public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);


    // --- Matrix Calculation ---

    /// <summary>
    /// Gets the local transformation matrix (relative to the parent).
    /// Recalculates only if local properties have changed.
    /// </summary>
    public Matrix4 GetLocalMatrix()
    {
        if (_isLocalDirty)
        {
            Matrix4 scaleMat = Matrix4.CreateScale(LocalScale);
            Matrix4 rotMat = Matrix4.CreateFromQuaternion(LocalRotation);
            Matrix4 transMat = Matrix4.CreateTranslation(LocalPosition);
            _localMatrix = scaleMat * rotMat * transMat; // TRS order
            _isLocalDirty = false;
        }
        return _localMatrix;
    }

    /// <summary>
    /// Gets the world transformation matrix (relative to the world origin).
    /// Recalculates only if local or parent's world transform has changed.
    /// </summary>
    public Matrix4 GetWorldMatrix()
    {
        if (_isWorldDirty)
        {
            if (Parent == null)
            {
                _worldMatrix = GetLocalMatrix();
            }
            else
            {
                // Combine parent's world matrix with this transform's local matrix
                _worldMatrix = GetLocalMatrix() * Parent.GetWorldMatrix();
            }
            _isWorldDirty = false;
        }
        return _worldMatrix;
    }

    // --- Public Methods ---

    /// <summary>
    /// Sets the parent of this transform. Pass null to make it a root object.
    /// </summary>
    public void SetParent(Transform? newParent, bool worldPositionStays = true)
    {
        var oldParent = Parent;
        if (oldParent == newParent) return; // No change

        // Cache world transform before changing parent
        Matrix4 worldMatrixCache = GetWorldMatrix();

        // Assign parent internally (handles adding/removing from children lists)
        Parent = newParent; 
        
        // If requested, adjust local transform to maintain original world position/rotation/scale
        if (worldPositionStays)
        {
             if (Parent == null)
             {
                  // New parent is root, so world becomes local
                  LocalPosition = worldMatrixCache.ExtractTranslation();
                  LocalRotation = worldMatrixCache.ExtractRotation();
                  LocalScale = worldMatrixCache.ExtractScale();
             }
             else
             {
                  // New parent exists, calculate required local transform
                  Matrix4 parentWorldInverse = Parent.GetWorldMatrix().Inverted();
                  Matrix4 newLocalMatrix = worldMatrixCache * parentWorldInverse;

                  LocalPosition = newLocalMatrix.ExtractTranslation();
                  LocalRotation = newLocalMatrix.ExtractRotation();
                  LocalScale = newLocalMatrix.ExtractScale(); 
             }
             // Setting local properties automatically calls MarkDirty()
        }
        else
        {
             // Just mark dirty if world position doesn't need preservation
             MarkDirty();
        }
    }

    /// <summary>
    /// Resets local position, rotation, and scale to defaults.
    /// </summary>
    public void ResetLocal()
    {
        LocalPosition = Vector3.Zero;
        LocalRotation = Quaternion.Identity;
        LocalScale = Vector3.One;
        // Setters call MarkDirty()
    }

    // --- Internal/Private Helpers ---

    /// <summary>
    /// Marks this transform and all its children as dirty for world matrix recalculation.
    /// Also marks local as dirty.
    /// </summary>
    private void MarkDirty()
    {
        if (!_isWorldDirty) // Avoid redundant marking if already dirty
        {
            _isWorldDirty = true;
            // Propagate dirty state down the hierarchy
            foreach (var child in _children)
            {
                child.MarkDirty();
            }
        }
        // Local transform always needs recalculation if a local property changes
        _isLocalDirty = true; 
    }
    
    // Override Reset method from Component if needed, maybe call ResetLocal?
    // public override void Reset() { ResetLocal(); } 
} 