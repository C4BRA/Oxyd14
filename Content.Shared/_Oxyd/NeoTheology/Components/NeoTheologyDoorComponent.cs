using Content.Shared.FixedPoint;
namespace Content.Shared._Oxyd.NeoTheology.Components;

/// <summary>
/// Marks a physical door as eligible for the foundation door litanies.
/// Access remains supplied dynamically by the current bearer.
/// </summary>
[RegisterComponent]
public sealed partial class NeoTheologyDoorComponent : Component
{
    /// <summary>Eris holy-door health depleted: the airlock's breakage threshold.</summary>
    public static readonly FixedPoint2 BrokenAt = FixedPoint2.New(200);

    [DataField]
    public bool LitanyLocked;

    /// <summary>Set when damage reaches <see cref="BrokenAt"/>. Repair Door clears it.</summary>
    [DataField]
    public bool Broken;

}
