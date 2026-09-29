using Content.Shared._Oxyd.NeoTheology.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Oxyd.NeoTheology.Effects;

/// <summary>
/// Eris <c>rituals/priest.dm:411-425</c>: set an existing cruciform's clearance to Clergy.
/// </summary>
public sealed partial class LitanyOrdinationEffect : LitanyEffect
{
    public override bool CanApply(
        LitanyEffectSystem system,
        LitanyEffectContext context,
        out LocId? failure)
    {
        if (context.Targets.Count == 0 || !system.TryGetActiveCruciform(context.Targets[0], out _))
        {
            failure = "oxyd-litany-no-cruciform";
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
        var rank = new LitanySetClearanceEvent(target, NeoTheologyClearance.Clergy, false);
        system.RaiseOn(target, ref rank);
        return rank.Handled;
    }
}
