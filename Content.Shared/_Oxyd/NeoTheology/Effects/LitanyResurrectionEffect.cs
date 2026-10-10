using System.Linq;
using Content.Shared.Database;
using Content.Shared._Oxyd.NeoTheology.Events;

namespace Content.Shared._Oxyd.NeoTheology.Effects;

/// <summary>
/// Eris <c>rituals/machinery.dm:13-42</c> (resurrection): the cloner starts a soul-safe job for
/// the soul in the reader. The server checks the saved profile, mind, machines, and biomass before payment.
/// The effect raises <see cref="LitanyResurrectionEvent"/> on the cloner for validation and execution.
/// </summary>
public sealed partial class LitanyResurrectionEffect : LitanyEffect
{
    public override bool CanApply(
        LitanyEffectSystem system,
        LitanyEffectContext context,
        out LocId? failure)
    {
        if (!Execute(system, context, true))
        {
            failure = "oxyd-litany-no-target";
            return false;
        }

        failure = null;
        return true;
    }

    public override bool Apply(LitanyEffectSystem system, LitanyEffectContext context)
        => Execute(system, context, false);

    private bool Execute(LitanyEffectSystem system, LitanyEffectContext context, bool validateOnly)
    {
        // Eris utters the prayer over the cloner in front. The reader is the one linked
        // to that cloner, or the nearest reader beside it.
        foreach (var target in context.Targets)
        {
            if (!system.IsLitanyCloner(target) ||
                !system.TryResolveResurrectionReader(target, context.Targets, out var reader))
                continue;

            var start = new LitanyResurrectionEvent(target, reader, false, validateOnly, context.User);
            system.RaiseOn(target, ref start);
            if (start.Handled)
            {
                if (!validateOnly)
                    system.AdminLog(LogType.Action, LogImpact.High, context.User,
                        $"started resurrection via {context.Litany.ID} on", target);
                return true;
            }
        }

        return false;
    }
}
