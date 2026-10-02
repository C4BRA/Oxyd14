using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Projectiles;

/// <summary>
/// Allows selected static surfaces to ricochet or fragment configured physical
/// projectiles, and to throw spall behind themselves when penetrated.
/// </summary>
[RegisterComponent]
public sealed partial class PhysicalRicochetSurfaceComponent : Component
{
    /// <summary>
    /// Maximum speed fraction along the surface normal that still counts as a grazing hit.
    /// </summary>
    [DataField]
    public float MaxNormalSpeedRatio = 0.25f;

    /// <summary>
    /// Entity spawned as wall-material spall when a projectile penetrates. Null disables.
    /// </summary>
    [DataField]
    public EntProtoId? SpallProto;

    [DataField]
    public int MinSpall;

    [DataField]
    public int MaxSpall;

    /// <summary>
    /// Full cone width around the projectile's continued direction that spall sprays within.
    /// </summary>
    [DataField]
    public Angle SpallCone = Angle.FromDegrees(20);

    /// <summary>
    /// Spall speed as a fraction of the projectile's speed after penetration.
    /// </summary>
    [DataField]
    public float SpallSpeedFraction = 0.4f;

    /// <summary>
    /// Each spall fragment's damage as a fraction of the projectile's damage.
    /// </summary>
    [DataField]
    public float SpallDamageFraction = 0.1f;
}
