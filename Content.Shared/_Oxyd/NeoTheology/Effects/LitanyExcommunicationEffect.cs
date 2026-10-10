using Content.Shared.Database;
using Content.Shared._Oxyd.NeoTheology.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._Oxyd.NeoTheology.Effects;

/// <summary>
/// Eris <c>rituals/priest.dm:451-490</c>: drop the specialization modules, set clearance to
/// None, and tell the target. Priest and inquisitor modules stay. An Inquisitor or a Godblood
/// bearer is refused. The named follower is the single target the cast layer recorded.
/// </summary>
public sealed partial class LitanyExcommunicationEffect : LitanyEffect
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
        var strip = new LitanyRemoveSpecializationEvent(target, false);
        system.RaiseOn(target, ref strip);
        if (!strip.Handled)
            return false;

        var clearance = new LitanySetClearanceEvent(target, NeoTheologyClearance.None, false);
        system.RaiseOn(target, ref clearance);
        if (!clearance.Handled)
            return false;

        // Eris: to_chat(M, SPAN_DANGER("You have been spiritually separated...")) — delivered on
        // announcement only, exactly as the source does.
        system.AdminLog(LogType.Action, LogImpact.High, context.User,
            $"excommunicated via {context.Litany.ID} on", target);
        system.DeliverSocialNotice(target, Loc.GetString("oxyd-litany-excommunication-notice"));
        return true;
    }
}
