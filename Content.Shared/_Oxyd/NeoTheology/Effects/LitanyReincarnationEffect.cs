using Content.Shared._Oxyd.NeoTheology.Events;

namespace Content.Shared._Oxyd.NeoTheology.Effects;

/// <summary>Eris Reincarnation: transfer the saved soul into a matching, inactive implanted body.</summary>
public sealed partial class LitanyReincarnationEffect : LitanyEffect
{
    public override bool CanApply(LitanyEffectSystem system, LitanyEffectContext context, out LocId? failure)
    {
        failure = "oxyd-litany-soul-lost";
        if (context.Targets.Count == 0)
            return false;

        var check = new LitanyReincarnationEvent(context.Targets[0], false, ValidateOnly: true);
        system.RaiseOn(context.Targets[0], ref check);
        if (!check.Handled)
            return false;

        failure = null;
        return true;
    }

    public override bool Apply(LitanyEffectSystem system, LitanyEffectContext context)
    {
        if (context.Targets.Count == 0)
            return false;

        var transfer = new LitanyReincarnationEvent(context.Targets[0], false);
        system.RaiseOn(context.Targets[0], ref transfer);
        return transfer.Handled;
    }
}
