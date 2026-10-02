using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Projectiles;

/// <summary>
/// Limits physical ricochets and their remaining speed and damage, and describes how
/// the projectile fragments when it strikes a surface too steeply to deflect.
/// </summary>
[RegisterComponent]
public sealed partial class PhysicalRicochetProjectileComponent : Component
{
    [DataField]
    public int MaxBounces = 1;

    /// <summary>
    /// Fraction of the surface-normal speed kept after a ricochet.
    /// </summary>
    [DataField]
    public float NormalRetention = 0.4f;

    /// <summary>
    /// Fraction of the surface-tangent speed kept after a ricochet.
    /// </summary>
    [DataField]
    public float TangentialRetention = 0.75f;

    [DataField]
    public float DamageRetention = 0.5f;

    /// <summary>
    /// Random angular dispersion applied to the outgoing direction after a bounce.
    /// </summary>
    [DataField]
    public Angle Spread = Angle.FromDegrees(4);

    /// <summary>
    /// Upper bound of the breakup band, as a normal-speed ratio. First-impact hits
    /// steeper than the surface's ricochet cutoff but no steeper than this shatter
    /// into fragments. Steeper first impacts only fragment if <see cref="FragmentOnEmbed"/>
    /// is set. Zero disables band fragmentation entirely.
    /// </summary>
    [DataField]
    public float FragmentMaxRatio = 0.6f;

    [DataField]
    public EntProtoId? FragmentProto;

    [DataField]
    public int MinFragments = 2;

    [DataField]
    public int MaxFragments = 4;

    /// <summary>
    /// Full cone width around the specular direction that fragments spray within.
    /// </summary>
    [DataField]
    public Angle FragmentCone = Angle.FromDegrees(20);

    /// <summary>
    /// Fragment speed as a fraction of the incoming speed.
    /// </summary>
    [DataField]
    public float FragmentSpeedFraction = 0.5f;

    /// <summary>
    /// Each fragment's damage as a fraction of the projectile's current damage.
    /// </summary>
    [DataField]
    public float FragmentDamageFraction = 0.2f;

    /// <summary>
    /// Whether first-impact hits steeper than <see cref="FragmentMaxRatio"/> splash
    /// fragments instead of embedding. When set, every non-deflecting first impact
    /// fragments, so <see cref="FragmentMaxRatio"/> only matters while this is unset.
    /// </summary>
    [DataField]
    public bool FragmentOnEmbed;

    /// <summary>
    /// Surface this projectile was spawned inside of by a breakup or spall and should
    /// pass through while it exits.
    /// </summary>
    public EntityUid? IgnoreSurface;

    public int Bounces;
}
