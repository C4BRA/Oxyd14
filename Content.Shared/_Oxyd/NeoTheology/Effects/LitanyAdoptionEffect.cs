using Content.Shared.Database;
using Content.Shared._Oxyd.NeoTheology.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Oxyd.NeoTheology.Effects;

/// <summary>
/// Eris <c>rituals/priest.dm:395-409</c>: set an existing cruciform's clearance to Common.
/// </summary>
public sealed partial class LitanyAdoptionEffect : LitanyEffect
{
    public override bool CanApply(
        LitanyEffectSystem system,
        LitanyEffectContext context,
        out LocId? failure)
    {
        if (context.Targets.Count == 0 ||
            !system.TryGetActiveCruciform(context.Targets[0], out _))
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

        var grant = new LitanySetClearanceEvent(context.Targets[0], NeoTheologyClearance.Common, false);
        system.RaiseOn(context.Targets[0], ref grant);
        if (grant.Handled)
            system.AdminLog(LogType.Action, LogImpact.Medium, context.User,
                $"converted via {context.Litany.ID} to Common clearance on", context.Targets[0]);
        return grant.Handled;
    }
}
