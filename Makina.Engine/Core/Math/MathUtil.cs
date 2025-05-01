using OpenTK.Mathematics;
using System;

namespace Makina.Engine.Core.Math;

/// <summary>
/// Provides utility mathematical functions specific to the Makina engine,
/// complementing System.Math and OpenTK.Mathematics.MathHelper.
/// </summary>
public static class MathUtil
{
    public const float Pi = MathF.PI;
    public const float DegToRad = Pi / 180.0f;
    public const float RadToDeg = 180.0f / Pi;

    /// <summary>
    /// Converts degrees to radians.
    /// </summary>
    /// <param name="degrees">Angle in degrees.</param>
    /// <returns>Angle in radians.</returns>
    public static float DegreesToRadians(float degrees)
    {
        return degrees * DegToRad;
    }

    /// <summary>
    /// Converts radians to degrees.
    /// </summary>
    /// <param name="radians">Angle in radians.</param>
    /// <returns>Angle in degrees.</returns>
    public static float RadiansToDegrees(float radians)
    {
        return radians * RadToDeg;
    }

    // Add other utility functions here as needed, e.g.:
    // - Lerp (linear interpolation) for floats, Vector3, etc.
    // - Clamp
    // - Custom easing functions
} 