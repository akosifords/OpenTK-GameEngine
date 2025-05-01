using OpenTK.Mathematics;

namespace Makina.Engine.Rendering;

/// <summary>
/// Abstract base class for different camera types.
/// </summary>
public abstract class Camera
{
    private Vector3 _position = Vector3.Zero;
    private float _aspectRatio = 1.0f; // Width / Height

    public Vector3 Position
    {
        get => _position;
        set
        {
            _position = value;
            RecalculateViewMatrix();
        }
    }

    // TODO: Add Rotation property (e.g., Euler angles or Quaternion)
    // Changing rotation should also call RecalculateViewMatrix()

    public float AspectRatio
    {
        get => _aspectRatio;
        set
        {
            _aspectRatio = value;
            RecalculateProjectionMatrix();
        }
    }

    public Matrix4 ViewMatrix { get; protected set; } = Matrix4.Identity;
    public Matrix4 ProjectionMatrix { get; protected set; } = Matrix4.Identity;
    public Matrix4 ViewProjectionMatrix { get; protected set; } = Matrix4.Identity;

    protected Camera(Vector3 position, float aspectRatio)
    {
        _position = position;
        _aspectRatio = aspectRatio;
        // Initial calculation will be done by derived class constructor potentially
    }
    
    protected abstract void RecalculateViewMatrix();
    protected abstract void RecalculateProjectionMatrix();

    protected void UpdateViewProjectionMatrix()
    {
        // OpenGL uses column-major matrices, OpenTK follows this.
        // Matrix multiplication order is Projection * View
        ViewProjectionMatrix = ViewMatrix * ProjectionMatrix;
    }
} 