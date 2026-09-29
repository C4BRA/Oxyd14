using Content.Shared._Oxyd.NeoTheology.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Oxyd.NeoTheology.Effects;

/// <summary>
/// Eris <c>rituals/priest.dm:427-449</c>: set clearance to None. Rank modules stay.
/// An Inquisitor or a Godblood bearer is refused.
/// </summary>
public sealed partial class LitanyOmissionEffect : LitanyEffect
{
    private static readonly ProtoId<NeoTheologyProfilePrototype> InquisitorProfile = "OxydNtInquisitor";

    public override bool CanApply(
        LitanyEffectSystem system,
        LitanyEffectContext context,
        out LocId? failure)
    {
        if (context.Targets.Count == 0 || !system.TryGetActiveCruciform(context.Targets[0], out var cruciform))
        {
            failure = "oxyd-litany-no-cruciform";
            return false;
        }

        // Eris: `if(CI.get_module(CRUCIFORM_INQUISITOR)) fail("You don't have the authority for this.")`.
        if (cruciform.Profile == InquisitorProfile)
        {
            failure = "oxyd-litany-no-authority";
            return false;
        }

        if (system.HasGodblood(context.Targets[0]))
        {
            failure = "oxyd-litany-godblood";
            return false;
        }

        failure = null;
        return true;
    }

    public override bool Apply(LitanyEffectSystem system, LitanyEffectContext context)
    {
        if (context.Targets.Count == 0)
            return false;

        var target = context.Targets[0];
        var clearance = new LitanySetClearanceEvent(target, NeoTheologyClearance.None, false);
        system.RaiseOn(target, ref clearance);
        return clearance.Handled;
    }
}
