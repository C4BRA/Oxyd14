using System.Numerics;

namespace Content.Shared.Projectiles;

/// <summary>
/// Computes the static-surface ricochet direction for one incoming projectile.
/// </summary>
public static class PhysicalRicochetMath
{
    private const float EpsilonSquared = 0.000001f;

    public static bool IsIncoming(Vector2 incomingVelocity, Vector2 surfaceNormal)
    {
        var speedSquared = incomingVelocity.LengthSquared();
        var normalLengthSquared = surfaceNormal.LengthSquared();
        if (speedSquared <= EpsilonSquared || normalLengthSquared <= EpsilonSquared)
            return false;

        var normal = surfaceNormal / MathF.Sqrt(normalLengthSquared);
        return Vector2.Dot(incomingVelocity, normal) > 0f;
    }

    /// <summary>
    /// Returns the fraction of the incoming speed directed along the surface normal.
    /// Fails when the projectile is moving away or either input is degenerate.
    /// </summary>
    public static bool TryGetNormalSpeedRatio(
        Vector2 incomingVelocity,
        Vector2 surfaceNormal,
        out float normalSpeedRatio)
    {
        normalSpeedRatio = 0f;

        var speedSquared = incomingVelocity.LengthSquared();
        var normalLengthSquared = surfaceNormal.LengthSquared();
        if (speedSquared <= EpsilonSquared || normalLengthSquared <= EpsilonSquared)
            return false;

        var normal = surfaceNormal / MathF.Sqrt(normalLengthSquared);
        var normalSpeed = Vector2.Dot(incomingVelocity, normal);
        if (normalSpeed <= 0f)
            return false;

        normalSpeedRatio = normalSpeed / MathF.Sqrt(speedSquared);
        return true;
    }

    /// <summary>
    /// Deflects the velocity about the surface normal, keeping separate retention for
    /// the normal and tangential components of the outgoing speed.
    /// </summary>
    public static bool TryDeflect(
        Vector2 incomingVelocity,
        Vector2 surfaceNormal,
        float normalRetention,
        float tangentialRetention,
        out Vector2 outgoingVelocity)
    {
        outgoingVelocity = Vector2.Zero;

        var normalLengthSquared = surfaceNormal.LengthSquared();
        if (incomingVelocity.LengthSquared() <= EpsilonSquared || normalLengthSquared <= EpsilonSquared ||
            !float.IsFinite(normalRetention) || normalRetention is <= 0f or > 1f ||
            !float.IsFinite(tangentialRetention) || tangentialRetention is <= 0f or > 1f)
        {
            return false;
        }

        var normal = surfaceNormal / MathF.Sqrt(normalLengthSquared);
        var normalSpeed = Vector2.Dot(incomingVelocity, normal);
        if (normalSpeed <= 0f)
            return false;

        var tangential = incomingVelocity - normal * normalSpeed;
        outgoingVelocity = tangential * tangentialRetention - normal * (normalSpeed * normalRetention);
        return outgoingVelocity.LengthSquared() > EpsilonSquared;
    }

    /// <summary>
    /// The undamped specular direction fragments spray around when a projectile shatters.
    /// </summary>
    public static Vector2 GetSpecularDirection(Vector2 incomingVelocity, Vector2 surfaceNormal)
    {
        var normalLengthSquared = surfaceNormal.LengthSquared();
        if (incomingVelocity.LengthSquared() <= EpsilonSquared || normalLengthSquared <= EpsilonSquared)
            return Vector2.Zero;

        return Vector2.Reflect(incomingVelocity, surfaceNormal / MathF.Sqrt(normalLengthSquared));
    }
}
